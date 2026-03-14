using Klau.Sdk.Common;

namespace Klau.Sdk.DumpSites;

public sealed class DumpSiteClient
{
    private readonly KlauHttpClient _http;
    private readonly string? _tenantId;

    internal DumpSiteClient(KlauHttpClient http, string? tenantId = null)
    {
        _http = http;
        _tenantId = tenantId;
    }

    // ─── Dump Sites ────────────────────────────────────────────────────────

    public async Task<PagedResult<DumpSite>> ListAsync(
        int page = 1,
        int pageSize = 100,
        CancellationToken ct = default)
    {
        var path = QueryBuilder.Build("api/v1/dump-sites",
            ("page", page),
            ("pageSize", pageSize));

        var response = await _http.GetResponseAsync<List<DumpSite>>(path, _tenantId, ct);
        return new PagedResult<DumpSite>(response.Data, response.Meta);
    }

    public async Task<DumpSite> GetAsync(string id, CancellationToken ct = default)
    {
        return await _http.GetAsync<DumpSite>($"api/v1/dump-sites/{id}", _tenantId, ct);
    }

    public async Task<DumpSite> CreateAsync(CreateDumpSiteRequest request, CancellationToken ct = default)
    {
        return await _http.PostAsync<DumpSite>("api/v1/dump-sites", request, _tenantId, ct);
    }

    public async Task<DumpSite> UpdateAsync(string id, UpdateDumpSiteRequest request, CancellationToken ct = default)
    {
        return await _http.PatchAsync<DumpSite>($"api/v1/dump-sites/{id}", request, _tenantId, ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await _http.DeleteAsync($"api/v1/dump-sites/{id}", _tenantId, ct);
    }

    // ─── Material Pricing ──────────────────────────────────────────────────

    /// <summary>
    /// List all material pricing for a dump site.
    /// </summary>
    public async Task<IReadOnlyList<MaterialPricing>> ListMaterialPricingAsync(
        string dumpSiteId, CancellationToken ct = default)
    {
        return await _http.GetAsync<IReadOnlyList<MaterialPricing>>(
            $"api/v1/dump-sites/{dumpSiteId}/material-pricing", _tenantId, ct);
    }

    /// <summary>
    /// Add a material with pricing to a dump site.
    /// This tells the optimizer which materials each dump site accepts.
    /// </summary>
    public async Task<MaterialPricing> AddMaterialPricingAsync(
        string dumpSiteId, AddMaterialPricingRequest request, CancellationToken ct = default)
    {
        return await _http.PostAsync<MaterialPricing>(
            $"api/v1/dump-sites/{dumpSiteId}/material-pricing", request, _tenantId, ct);
    }

    /// <summary>
    /// Update material pricing for a dump site.
    /// </summary>
    public async Task<MaterialPricing> UpdateMaterialPricingAsync(
        string dumpSiteId, string materialId, UpdateMaterialPricingRequest request, CancellationToken ct = default)
    {
        return await _http.PatchAsync<MaterialPricing>(
            $"api/v1/dump-sites/{dumpSiteId}/material-pricing/{materialId}", request, _tenantId, ct);
    }

    /// <summary>
    /// Remove a material from a dump site.
    /// </summary>
    public async Task RemoveMaterialPricingAsync(
        string dumpSiteId, string materialId, CancellationToken ct = default)
    {
        await _http.DeleteAsync($"api/v1/dump-sites/{dumpSiteId}/material-pricing/{materialId}", _tenantId, ct);
    }
}
