using Klau.Sdk.Common;

namespace Klau.Sdk.Webhooks;

public interface IWebhookClient
{
    Task<DeveloperSettings> GetSettingsAsync(CancellationToken ct = default);
    Task<CreateWebhookResult> CreateAsync(CreateWebhookRequest request, CancellationToken ct = default);
    Task SetEnabledAsync(string webhookId, bool enabled, CancellationToken ct = default);
    Task DeleteAsync(string webhookId, CancellationToken ct = default);
    Task<WebhookTestResult> TestAsync(string webhookId, CancellationToken ct = default);
}

/// <summary>
/// Manage webhook endpoints via the Klau Developer Settings API.
/// Register endpoints to receive real-time events for job lifecycle,
/// dispatch optimization, and more.
/// </summary>
public sealed class WebhookClient : IWebhookClient
{
    private readonly KlauHttpClient _http;

    internal WebhookClient(KlauHttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// List all webhook endpoints and API keys for your account.
    /// </summary>
    public async Task<DeveloperSettings> GetSettingsAsync(CancellationToken ct = default)
    {
        return await _http.GetAsync<DeveloperSettings>("api/v1/settings/developer", ct: ct);
    }

    /// <summary>
    /// Register a new webhook endpoint. The signing secret is returned only once.
    /// </summary>
    public async Task<CreateWebhookResult> CreateAsync(CreateWebhookRequest request, CancellationToken ct = default)
    {
        return await _http.PostAsync<CreateWebhookResult>("api/v1/settings/developer/webhooks", request, ct: ct);
    }

    /// <summary>
    /// Enable or disable a webhook endpoint.
    /// </summary>
    public async Task SetEnabledAsync(string webhookId, bool enabled, CancellationToken ct = default)
    {
        await _http.PatchAsync<object>($"api/v1/settings/developer/webhooks/{webhookId}", new { enabled }, ct: ct);
    }

    /// <summary>
    /// Delete a webhook endpoint.
    /// </summary>
    public async Task DeleteAsync(string webhookId, CancellationToken ct = default)
    {
        await _http.DeleteAsync($"api/v1/settings/developer/webhooks/{webhookId}", ct: ct);
    }

    /// <summary>
    /// Send a test event to a webhook endpoint to verify connectivity.
    /// </summary>
    public async Task<WebhookTestResult> TestAsync(string webhookId, CancellationToken ct = default)
    {
        return await _http.PostAsync<WebhookTestResult>(
            $"api/v1/settings/developer/webhooks/{webhookId}/test", ct: ct);
    }
}
