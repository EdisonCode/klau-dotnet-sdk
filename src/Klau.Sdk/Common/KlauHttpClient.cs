using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Klau.Sdk.Common;

/// <summary>
/// Shared HTTP client for all Klau API calls. Handles authentication,
/// tenant context, serialization, and error mapping.
/// </summary>
public sealed class KlauHttpClient : IDisposable
{
    private readonly HttpClient _http;
    private string? _token;

    private const string TenantHeader = "Klau-Tenant-Id";

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper) }
    };

    public KlauHttpClient(string baseUrl, HttpClient? httpClient = null)
    {
        _http = httpClient ?? new HttpClient();
        _http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        _http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
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
    /// Set the tenant context for sub-tenant operations.
    /// Parent company API keys can operate on child tenants by setting this header.
    /// </summary>
    internal void SetTenantId(string? tenantId)
    {
        _http.DefaultRequestHeaders.Remove(TenantHeader);
        if (tenantId is not null)
        {
            _http.DefaultRequestHeaders.Add(TenantHeader, tenantId);
        }
    }

    internal async Task<T> GetAsync<T>(string path, CancellationToken ct = default)
    {
        var response = await _http.GetAsync(path, ct);
        return await HandleResponse<T>(response, ct);
    }

    internal async Task<ApiResponse<T>> GetResponseAsync<T>(string path, CancellationToken ct = default)
    {
        var response = await _http.GetAsync(path, ct);
        return await HandleWrappedResponse<T>(response, ct);
    }

    internal async Task<T> PostAsync<T>(string path, object? body = null, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(path, body, JsonOptions, ct);
        return await HandleResponse<T>(response, ct);
    }

    internal async Task PostAsync(string path, object? body = null, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(path, body, JsonOptions, ct);
        await EnsureSuccess(response, ct);
    }

    internal async Task<T> PatchAsync<T>(string path, object body, CancellationToken ct = default)
    {
        var content = JsonContent.Create(body, options: JsonOptions);
        var response = await _http.PatchAsync(path, content, ct);
        return await HandleResponse<T>(response, ct);
    }

    internal async Task<T> PutAsync<T>(string path, object body, CancellationToken ct = default)
    {
        var content = JsonContent.Create(body, options: JsonOptions);
        var response = await _http.PutAsync(path, content, ct);
        return await HandleResponse<T>(response, ct);
    }

    internal async Task DeleteAsync(string path, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync(path, ct);
        await EnsureSuccess(response, ct);
    }

    internal async Task<T> DeleteAsync<T>(string path, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync(path, ct);
        return await HandleResponse<T>(response, ct);
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

    public void Dispose() => _http.Dispose();
}
