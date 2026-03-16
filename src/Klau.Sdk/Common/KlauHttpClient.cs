using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Klau.Sdk.Common;

/// <summary>
/// Shared HTTP client for all Klau API calls. Handles authentication,
/// tenant context, serialization, retry logic, and error mapping.
/// </summary>
public sealed class KlauHttpClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly bool _ownsHttpClient;
    private readonly ILogger _logger;
    private string? _token;
    private string? _defaultTenantId;

    private const string TenantHeader = "Klau-Tenant-Id";

    /// <summary>
    /// ActivitySource for OpenTelemetry distributed tracing.
    /// Consumers opt in with: <c>builder.Services.AddOpenTelemetry().WithTracing(b =&gt; b.AddSource("Klau.Sdk"));</c>
    /// </summary>
    public static readonly ActivitySource ActivitySource = new("Klau.Sdk", SdkVersion);

    private static readonly string SdkVersion =
        typeof(KlauHttpClient).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
        ?? typeof(KlauHttpClient).Assembly.GetName().Version?.ToString()
        ?? "0.0.0";

    /// <summary>
    /// Default retry configuration for transient errors.
    /// Retries on 429 (rate limit), 502, 503, 504, and network errors.
    /// </summary>
    private const int MaxRetries = 3;
    private static readonly TimeSpan[] RetryDelays = [
        TimeSpan.FromMilliseconds(500),
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
    ];

    private static readonly HashSet<HttpStatusCode> RetryableStatusCodes =
    [
        HttpStatusCode.TooManyRequests,      // 429
        HttpStatusCode.BadGateway,           // 502
        HttpStatusCode.ServiceUnavailable,   // 503
        HttpStatusCode.GatewayTimeout,       // 504
    ];

    /// <summary>
    /// JSON serializer options used by the SDK. Exposed for webhook event
    /// deserialization and other scenarios where you need matching serialization.
    /// </summary>
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper) }
    };

    public KlauHttpClient(string baseUrl, HttpClient? httpClient = null, ILogger? logger = null)
    {
        _ownsHttpClient = httpClient is null;
        _http = httpClient ?? new HttpClient();
        _logger = logger ?? NullLogger.Instance;
        _http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        _http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        _http.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("Klau-DotNet-SDK", SdkVersion));

        // Default timeout — overridden by SetTimeout or caller-provided HttpClient
        if (_ownsHttpClient)
            _http.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Set the request timeout. Only applies to SDK-created HttpClients.
    /// If you provided your own HttpClient, configure its Timeout directly.
    /// </summary>
    internal void SetTimeout(TimeSpan timeout)
    {
        if (_ownsHttpClient)
            _http.Timeout = timeout;
    }

    internal void SetToken(string token)
    {
        _token = token;
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    internal void ClearToken()
    {
        _token = null;
        _http.DefaultRequestHeaders.Authorization = null;
    }

    internal string? Token => _token;

    /// <summary>
    /// Set the default tenant context for all requests.
    /// </summary>
    internal void SetDefaultTenantId(string? tenantId)
    {
        _defaultTenantId = tenantId;
        // Update default headers for non-scoped requests
        _http.DefaultRequestHeaders.Remove(TenantHeader);
        if (tenantId is not null)
        {
            _http.DefaultRequestHeaders.Add(TenantHeader, tenantId);
        }
    }

    internal async Task<T> GetAsync<T>(string path, string? tenantOverride = null, CancellationToken ct = default)
    {
        var response = await SendWithRetry(() =>
        {
            var req = new HttpRequestMessage(HttpMethod.Get, path);
            ApplyTenantHeader(req, tenantOverride);
            return req;
        }, ct);
        return await HandleResponse<T>(response, ct);
    }

    internal async Task<ApiResponse<T>> GetResponseAsync<T>(string path, string? tenantOverride = null, CancellationToken ct = default)
    {
        var response = await SendWithRetry(() =>
        {
            var req = new HttpRequestMessage(HttpMethod.Get, path);
            ApplyTenantHeader(req, tenantOverride);
            return req;
        }, ct);
        return await HandleWrappedResponse<T>(response, ct);
    }

    /// <summary>
    /// Get a list endpoint that returns { data: { [collectionName]: [...], total: N, ... } }.
    /// Extracts the named array from the nested object and returns it with pagination info.
    /// </summary>
    internal async Task<ListResponse<T>> GetListAsync<T>(string path, string collectionName, string? tenantOverride = null, CancellationToken ct = default)
    {
        var response = await SendWithRetry(() =>
        {
            var req = new HttpRequestMessage(HttpMethod.Get, path);
            ApplyTenantHeader(req, tenantOverride);
            return req;
        }, ct);
        return await HandleListResponse<T>(response, collectionName, ct);
    }

    internal async Task<T> PostAsync<T>(string path, object? body = null, string? tenantOverride = null, CancellationToken ct = default)
    {
        var response = await SendWithRetry(() =>
        {
            var req = new HttpRequestMessage(HttpMethod.Post, path);
            if (body is not null) req.Content = JsonContent.Create(body, options: JsonOptions);
            ApplyTenantHeader(req, tenantOverride);
            return req;
        }, ct);
        return await HandleResponse<T>(response, ct);
    }

    internal async Task<T> PostAsync<T>(string path, object? body, string? tenantOverride, KlauRequestOptions? options, CancellationToken ct)
    {
        var effectiveCt = WithTimeout(options?.Timeout, ct, out var cts);
        try
        {
            var response = await SendWithRetry(() =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, path);
                if (body is not null) req.Content = JsonContent.Create(body, options: JsonOptions);
                ApplyRequestOptions(req, tenantOverride, options);
                return req;
            }, effectiveCt);
            return await HandleResponse<T>(response, effectiveCt);
        }
        finally { cts?.Dispose(); }
    }

    /// <summary>
    /// Post to a create endpoint that returns { data: { [idFieldName]: "..." } }.
    /// Returns the created entity ID.
    /// </summary>
    internal async Task<string> PostCreateAsync(string path, object? body, string idFieldName, string? tenantOverride = null, CancellationToken ct = default)
    {
        var response = await SendWithRetry(() =>
        {
            var req = new HttpRequestMessage(HttpMethod.Post, path);
            if (body is not null) req.Content = JsonContent.Create(body, options: JsonOptions);
            ApplyTenantHeader(req, tenantOverride);
            return req;
        }, ct);

        if (!response.IsSuccessStatusCode)
        {
            await ThrowApiException(response, ct);
        }

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseBody);

        if (!doc.RootElement.TryGetProperty("data", out var dataElement))
            throw new KlauApiException("DESERIALIZATION_ERROR", "Response missing 'data' property", (int)response.StatusCode);

        if (!dataElement.TryGetProperty(idFieldName, out var idElement))
            throw new KlauApiException("DESERIALIZATION_ERROR", $"Response data missing '{idFieldName}' property", (int)response.StatusCode);

        return idElement.GetString()
            ?? throw new KlauApiException("DESERIALIZATION_ERROR", $"'{idFieldName}' was null", (int)response.StatusCode);
    }

    internal async Task PostAsync(string path, object? body = null, string? tenantOverride = null, CancellationToken ct = default)
    {
        var response = await SendWithRetry(() =>
        {
            var req = new HttpRequestMessage(HttpMethod.Post, path);
            if (body is not null) req.Content = JsonContent.Create(body, options: JsonOptions);
            ApplyTenantHeader(req, tenantOverride);
            return req;
        }, ct);
        await EnsureSuccess(response, ct);
    }

    internal async Task<string> PostCreateAsync(string path, object? body, string idFieldName, string? tenantOverride, KlauRequestOptions? options, CancellationToken ct)
    {
        var effectiveCt = WithTimeout(options?.Timeout, ct, out var cts);
        try
        {
            var response = await SendWithRetry(() =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, path);
                if (body is not null) req.Content = JsonContent.Create(body, options: JsonOptions);
                ApplyRequestOptions(req, tenantOverride, options);
                return req;
            }, effectiveCt);

            if (!response.IsSuccessStatusCode)
                await ThrowApiException(response, effectiveCt);

            var responseBody = await response.Content.ReadAsStringAsync(effectiveCt);
            using var doc = JsonDocument.Parse(responseBody);

            if (!doc.RootElement.TryGetProperty("data", out var dataElement))
                throw new KlauApiException("DESERIALIZATION_ERROR", "Response missing 'data' property", (int)response.StatusCode);
            if (!dataElement.TryGetProperty(idFieldName, out var idElement))
                throw new KlauApiException("DESERIALIZATION_ERROR", $"Response data missing '{idFieldName}' property", (int)response.StatusCode);

            return idElement.GetString()
                ?? throw new KlauApiException("DESERIALIZATION_ERROR", $"'{idFieldName}' was null", (int)response.StatusCode);
        }
        finally { cts?.Dispose(); }
    }

    internal async Task<T> PatchAsync<T>(string path, object body, string? tenantOverride = null, CancellationToken ct = default)
    {
        var response = await SendWithRetry(() =>
        {
            var req = new HttpRequestMessage(HttpMethod.Patch, path)
            {
                Content = JsonContent.Create(body, options: JsonOptions)
            };
            ApplyTenantHeader(req, tenantOverride);
            return req;
        }, ct);
        return await HandleResponse<T>(response, ct);
    }

    internal async Task<T> PutAsync<T>(string path, object body, string? tenantOverride = null, CancellationToken ct = default)
    {
        var response = await SendWithRetry(() =>
        {
            var req = new HttpRequestMessage(HttpMethod.Put, path)
            {
                Content = JsonContent.Create(body, options: JsonOptions)
            };
            ApplyTenantHeader(req, tenantOverride);
            return req;
        }, ct);
        return await HandleResponse<T>(response, ct);
    }

    internal async Task DeleteAsync(string path, string? tenantOverride = null, CancellationToken ct = default)
    {
        var response = await SendWithRetry(() =>
        {
            var req = new HttpRequestMessage(HttpMethod.Delete, path);
            ApplyTenantHeader(req, tenantOverride);
            return req;
        }, ct);
        await EnsureSuccess(response, ct);
    }

    internal async Task<T> DeleteAsync<T>(string path, string? tenantOverride = null, CancellationToken ct = default)
    {
        var response = await SendWithRetry(() =>
        {
            var req = new HttpRequestMessage(HttpMethod.Delete, path);
            ApplyTenantHeader(req, tenantOverride);
            return req;
        }, ct);
        return await HandleResponse<T>(response, ct);
    }

    /// <summary>
    /// Apply tenant header to a per-request message. Scoped tenant overrides the default.
    /// </summary>
    private void ApplyTenantHeader(HttpRequestMessage request, string? tenantOverride)
    {
        var tenantId = tenantOverride ?? _defaultTenantId;
        if (tenantId is not null)
        {
            request.Headers.Remove(TenantHeader);
            request.Headers.Add(TenantHeader, tenantId);
        }
    }

    private void ApplyRequestOptions(HttpRequestMessage request, string? tenantOverride, KlauRequestOptions? options)
    {
        ApplyTenantHeader(request, options?.TenantId ?? tenantOverride);
        if (options?.IdempotencyKey is { } key)
            request.Headers.Add("Idempotency-Key", key);
    }

    private static CancellationToken WithTimeout(TimeSpan? timeout, CancellationToken ct, out CancellationTokenSource? cts)
    {
        if (timeout is not { } t)
        {
            cts = null;
            return ct;
        }
        cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(t);
        return cts.Token;
    }

    /// <summary>
    /// Send an HTTP request with automatic retry for transient errors.
    /// Emits OpenTelemetry spans via <see cref="ActivitySource"/>.
    /// </summary>
    private async Task<HttpResponseMessage> SendWithRetry(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken ct)
    {
        using var activity = ActivitySource.StartActivity("Klau.Api.Request");
        activity?.SetTag("server.address", _http.BaseAddress?.Host);

        var tenantId = _defaultTenantId;
        if (tenantId is not null)
            activity?.SetTag("klau.tenant.id", tenantId);

        HttpResponseMessage? lastResponse = null;
        int totalAttempts = 0;
        bool spanTagged = false;

        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            totalAttempts = attempt + 1;

            if (attempt > 0)
            {
                var delay = RetryDelays[Math.Min(attempt - 1, RetryDelays.Length - 1)];

                // Respect Retry-After header from 429 responses
                if (lastResponse?.Headers.RetryAfter?.Delta is { } retryAfter)
                {
                    delay = retryAfter > delay ? retryAfter : delay;
                }

                _logger.LogWarning(
                    "Klau API request failed (attempt {Attempt}/{Max}), retrying in {Delay}ms",
                    attempt, MaxRetries + 1, delay.TotalMilliseconds);

                await Task.Delay(delay, ct);
            }

            try
            {
                var request = requestFactory();

                // Tag span with method/path from the first actual request
                if (!spanTagged && activity is not null)
                {
                    var reqPath = request.RequestUri?.AbsolutePath ?? request.RequestUri?.OriginalString ?? "unknown";
                    activity.DisplayName = $"Klau {request.Method.Method} {reqPath}";
                    activity.SetTag("http.request.method", request.Method.Method);
                    activity.SetTag("url.path", reqPath);
                    spanTagged = true;
                }

                lastResponse = await _http.SendAsync(request, ct);

                if (!RetryableStatusCodes.Contains(lastResponse.StatusCode))
                {
                    activity?.SetTag("http.response.status_code", (int)lastResponse.StatusCode);
                    if (totalAttempts > 1)
                        activity?.SetTag("klau.retry.count", totalAttempts - 1);
                    if (!lastResponse.IsSuccessStatusCode)
                        activity?.SetStatus(ActivityStatusCode.Error);
                    return lastResponse;
                }
            }
            catch (HttpRequestException ex) when (attempt < MaxRetries)
            {
                _logger.LogWarning(ex,
                    "Klau API network error (attempt {Attempt}/{Max})",
                    attempt + 1, MaxRetries + 1);
                lastResponse = null;
            }
            catch (TaskCanceledException ex) when (!ct.IsCancellationRequested && attempt < MaxRetries)
            {
                // Timeout (not user cancellation)
                _logger.LogWarning(ex,
                    "Klau API timeout (attempt {Attempt}/{Max})",
                    attempt + 1, MaxRetries + 1);
                lastResponse = null;
            }
        }

        activity?.SetTag("klau.retry.count", MaxRetries);
        activity?.SetStatus(ActivityStatusCode.Error, "All retries exhausted");

        // All retries exhausted
        if (lastResponse is not null)
        {
            activity?.SetTag("http.response.status_code", (int)lastResponse.StatusCode);
            _logger.LogError(
                "Klau API request failed after {MaxRetries} retries (final status: {StatusCode})",
                MaxRetries, (int)lastResponse.StatusCode);
            return lastResponse;
        }

        _logger.LogError("Klau API request failed after {MaxRetries} retries (no response)", MaxRetries);
        throw new HttpRequestException("All retry attempts exhausted with no response from the Klau API");
    }

    private async Task<T> HandleResponse<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            await ThrowApiException(response, ct);
        }

        var wrapper = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions, ct);
        if (wrapper is null)
            throw new KlauApiException("DESERIALIZATION_ERROR", "Failed to deserialize response", (int)response.StatusCode);

        return wrapper.Data;
    }

    private async Task<ApiResponse<T>> HandleWrappedResponse<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            await ThrowApiException(response, ct);
        }

        var wrapper = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions, ct);
        if (wrapper is null)
            throw new KlauApiException("DESERIALIZATION_ERROR", "Failed to deserialize response", (int)response.StatusCode);

        return wrapper;
    }

    /// <summary>
    /// Handle API list responses shaped as { data: { [collectionName]: [...], total: N, page: N, ... } }.
    /// Extracts the named collection and pagination metadata from the nested object.
    /// </summary>
    private async Task<ListResponse<T>> HandleListResponse<T>(HttpResponseMessage response, string collectionName, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            await ThrowApiException(response, ct);
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(body);

        if (!doc.RootElement.TryGetProperty("data", out var dataElement))
            throw new KlauApiException("DESERIALIZATION_ERROR", "Response missing 'data' property", (int)response.StatusCode);

        // Extract the named collection array
        if (!dataElement.TryGetProperty(collectionName, out var collectionElement))
            throw new KlauApiException("DESERIALIZATION_ERROR", $"Response data missing '{collectionName}' property", (int)response.StatusCode);

        var items = JsonSerializer.Deserialize<List<T>>(collectionElement.GetRawText(), JsonOptions)
            ?? [];

        // Extract pagination fields from the data object
        int? total = dataElement.TryGetProperty("total", out var totalEl) ? totalEl.GetInt32() : null;
        int? page = dataElement.TryGetProperty("page", out var pageEl) ? pageEl.GetInt32() : null;
        int? pageSize = dataElement.TryGetProperty("pageSize", out var pageSizeEl) ? pageSizeEl.GetInt32() : null;
        bool hasMore = dataElement.TryGetProperty("hasMore", out var hasMoreEl) && hasMoreEl.GetBoolean();

        return new ListResponse<T>(items, total, page, pageSize, hasMore);
    }

    private async Task EnsureSuccess(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            await ThrowApiException(response, ct);
        }
    }

    private static async Task ThrowApiException(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);

        // Extract Retry-After for rate limit responses
        TimeSpan? retryAfter = response.Headers.RetryAfter?.Delta
            ?? (response.Headers.RetryAfter?.Date is { } date
                ? date - DateTimeOffset.UtcNow
                : null);

        // Try standard Klau API error shape: { error: { code, message, details } }
        try
        {
            var error = JsonSerializer.Deserialize<ApiErrorResponse>(body, JsonOptions);
            if (error?.Error is not null)
            {
                throw new KlauApiException(
                    error.Error.Code,
                    error.Error.Message,
                    (int)response.StatusCode,
                    error.Error.Details)
                { RetryAfter = retryAfter };
            }
        }
        catch (JsonException) { }

        // Fallback: try Fastify's native error shape { statusCode, error, message }
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var message = root.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : null;
            var errorName = root.TryGetProperty("error", out var errProp) ? errProp.GetString() : null;
            if (message is not null)
            {
                throw new KlauApiException(
                    errorName ?? "HTTP_ERROR",
                    message,
                    (int)response.StatusCode)
                { RetryAfter = retryAfter };
            }
        }
        catch (JsonException) { }

        throw new KlauApiException(
            "HTTP_ERROR",
            $"Request failed with status {(int)response.StatusCode}: {body}",
            (int)response.StatusCode)
        { RetryAfter = retryAfter };
    }

    public void Dispose()
    {
        if (_ownsHttpClient) _http.Dispose();
    }
}
