using Klau.Sdk.Common;

namespace Klau.Sdk.Storefronts;

public sealed class StorefrontClient
{
    private readonly KlauHttpClient _http;
    private readonly string? _tenantId;

    internal StorefrontClient(KlauHttpClient http, string? tenantId = null)
    {
        _http = http;
        _tenantId = tenantId;
    }

    /// <summary>
    /// Get the public storefront configuration and catalog by slug (no auth required).
    /// </summary>
    public async Task<StorefrontConfig> GetConfigAsync(string slug, CancellationToken ct = default)
    {
        return await _http.GetAsync<StorefrontConfig>($"api/v1/storefronts/{slug}/config", _tenantId, ct);
    }

    /// <summary>
    /// Submit an order through the public storefront (no auth required).
    /// </summary>
    public async Task<OrderConfirmation> SubmitOrderAsync(
        string slug,
        SubmitOrderRequest request,
        CancellationToken ct = default)
    {
        return await _http.PostAsync<OrderConfirmation>($"api/v1/storefronts/{slug}/orders", request, _tenantId, ct);
    }

    /// <summary>
    /// Check available delivery dates for the storefront (no auth required).
    /// </summary>
    public async Task<AvailabilityResult> CheckAvailabilityAsync(
        string slug,
        CheckAvailabilityRequest request,
        CancellationToken ct = default)
    {
        return await _http.PostAsync<AvailabilityResult>(
            $"api/v1/storefronts/{slug}/check-availability", request, _tenantId, ct);
    }

    /// <summary>
    /// Get the authenticated tenant's own storefront (admin).
    /// </summary>
    public async Task<Storefront> GetOwnAsync(CancellationToken ct = default)
    {
        return await _http.GetAsync<Storefront>("api/v1/storefronts/", _tenantId, ct);
    }

    /// <summary>
    /// Create a new storefront via the setup wizard (admin).
    /// </summary>
    public async Task<Storefront> SetupAsync(SetupStorefrontRequest request, CancellationToken ct = default)
    {
        return await _http.PostAsync<Storefront>("api/v1/storefronts/setup-wizard", request, _tenantId, ct);
    }

    /// <summary>
    /// Update storefront settings (admin).
    /// </summary>
    public async Task<Storefront> UpdateAsync(UpdateStorefrontRequest request, CancellationToken ct = default)
    {
        return await _http.PutAsync<Storefront>("api/v1/storefronts/", request, _tenantId, ct);
    }

    /// <summary>
    /// Activate the storefront (admin).
    /// </summary>
    public async Task ActivateAsync(CancellationToken ct = default)
    {
        await _http.PostAsync("api/v1/storefronts/activate", null, _tenantId, ct);
    }

    /// <summary>
    /// Pause the storefront (admin).
    /// </summary>
    public async Task PauseAsync(CancellationToken ct = default)
    {
        await _http.PostAsync("api/v1/storefronts/pause", null, _tenantId, ct);
    }

    /// <summary>
    /// List orders for the storefront (admin).
    /// </summary>
    public async Task<PagedResult<StorefrontOrder>> ListOrdersAsync(
        int page = 1,
        int pageSize = 100,
        CancellationToken ct = default)
    {
        var path = QueryBuilder.Build("api/v1/storefronts/orders",
            ("page", page),
            ("pageSize", pageSize));

        var response = await _http.GetResponseAsync<List<StorefrontOrder>>(path, _tenantId, ct);
        return new PagedResult<StorefrontOrder>(response.Data, response.Meta);
    }
}
