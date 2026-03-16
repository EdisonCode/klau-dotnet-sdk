using Klau.Sdk.Common;

namespace Klau.Sdk.Orders;

public interface IOrderClient
{
    Task<OrderTracking> GetStatusAsync(string orderId, CancellationToken ct = default);
    Task RequestPickupAsync(string orderId, CancellationToken ct = default);
    Task<List<CustomerOrder>> ListMineAsync(CancellationToken ct = default);
    Task<List<PendingSettlement>> ListPendingSettlementsAsync(CancellationToken ct = default);
    Task<SettlementResult> SettleAsync(string orderId, SettleRequest? request = null, CancellationToken ct = default);
}

public sealed class OrderClient : IOrderClient
{
    private readonly KlauHttpClient _http;
    private readonly string? _tenantId;

    internal OrderClient(KlauHttpClient http, string? tenantId = null)
    {
        _http = http;
        _tenantId = tenantId;
    }

    /// <summary>
    /// Get order tracking status (public, no auth required).
    /// </summary>
    public async Task<OrderTracking> GetStatusAsync(string orderId, CancellationToken ct = default)
    {
        return await _http.GetAsync<OrderTracking>($"api/v1/orders/{orderId}/status", _tenantId, ct);
    }

    /// <summary>
    /// Request dumpster pickup for an active order (customer auth).
    /// </summary>
    public async Task RequestPickupAsync(string orderId, CancellationToken ct = default)
    {
        await _http.PostAsync($"api/v1/orders/{orderId}/request-pickup", null, _tenantId, ct);
    }

    /// <summary>
    /// List the authenticated customer's own orders (customer auth).
    /// </summary>
    public async Task<List<CustomerOrder>> ListMineAsync(CancellationToken ct = default)
    {
        return await _http.GetAsync<List<CustomerOrder>>("api/v1/orders/mine", _tenantId, ct);
    }

    /// <summary>
    /// List orders pending settlement (admin).
    /// </summary>
    public async Task<List<PendingSettlement>> ListPendingSettlementsAsync(CancellationToken ct = default)
    {
        return await _http.GetAsync<List<PendingSettlement>>("api/v1/storefronts/settlements", _tenantId, ct);
    }

    /// <summary>
    /// Manually settle an order (admin).
    /// </summary>
    public async Task<SettlementResult> SettleAsync(
        string orderId,
        SettleRequest? request = null,
        CancellationToken ct = default)
    {
        return await _http.PostAsync<SettlementResult>(
            $"api/v1/storefronts/settlements/{orderId}/settle",
            request ?? new(),
            _tenantId,
            ct);
    }
}
