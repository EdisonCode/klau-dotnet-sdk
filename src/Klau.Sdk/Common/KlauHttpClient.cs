using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
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

    /// <summary>
    /// Send an HTTP request with automatic retry for transient errors.
    /// </summary>
    private async Task<HttpResponseMessage> SendWithRetry(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken ct)
    {
        HttpResponseMessage? lastResponse = null;

        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
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
                lastResponse = await _http.SendAsync(request, ct);

                if (!RetryableStatusCodes.Contains(lastResponse.StatusCode))
                {
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

        // All retries exhausted
        if (lastResponse is not null)
        {
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

    private async Task EnsureSuccess(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            await ThrowApiException(response, ct);
        }
    }

    private static async Task ThrowApiException(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions, ct);
            if (error?.Error is not null)
            {
                throw new KlauApiException(
                    error.Error.Code,
                    error.Error.Message,
                    (int)response.StatusCode,
                    error.Error.Details);
            }
        }
        catch (JsonException) { }

        var body = await response.Content.ReadAsStringAsync(ct);
        throw new KlauApiException(
            "HTTP_ERROR",
            $"Request failed with status {(int)response.StatusCode}: {body}",
            (int)response.StatusCode);
    }

    public void Dispose()
    {
        if (_ownsHttpClient) _http.Dispose();
    }
}
