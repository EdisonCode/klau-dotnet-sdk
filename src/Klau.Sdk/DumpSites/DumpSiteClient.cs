using System.Runtime.CompilerServices;
using Klau.Sdk.Common;

namespace Klau.Sdk.DumpSites;

public interface IDumpSiteClient
{
    Task<PagedResult<DumpSite>> ListAsync(int page = 1, int pageSize = 100, CancellationToken ct = default);
    IAsyncEnumerable<DumpSite> ListAllAsync(int pageSize = 100, CancellationToken ct = default);
    Task<DumpSite> GetAsync(string id, CancellationToken ct = default);
    Task<string> CreateAsync(CreateDumpSiteRequest request, CancellationToken ct = default);
    Task UpdateAsync(string id, UpdateDumpSiteRequest request, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<MaterialPricing>> ListMaterialPricingAsync(string dumpSiteId, CancellationToken ct = default);
    Task<MaterialPricing> AddMaterialPricingAsync(string dumpSiteId, AddMaterialPricingRequest request, CancellationToken ct = default);
    Task<MaterialPricing> UpdateMaterialPricingAsync(string dumpSiteId, string materialId, UpdateMaterialPricingRequest request, CancellationToken ct = default);
    Task RemoveMaterialPricingAsync(string dumpSiteId, string materialId, CancellationToken ct = default);
}

public sealed class DumpSiteClient : IDumpSiteClient
{
    private readonly KlauHttpClient _http;
    private readonly string? _tenantId;

    internal DumpSiteClient(KlauHttpClient http, string? tenantId = null)
    {
        _http = http;
        _tenantId = tenantId;
    }

    // --- Dump Sites ---

    public async Task<PagedResult<DumpSite>> ListAsync(
        int page = 1,
        int pageSize = 100,
        CancellationToken ct = default)
    {
        var path = QueryBuilder.Build("api/v1/dump-sites",
            ("page", page),
            ("pageSize", pageSize));

        var response = await _http.GetListAsync<DumpSite>(path, "dumpSites", _tenantId, ct);
        return new PagedResult<DumpSite>(response.Items, response.Total, response.Page, response.PageSize, response.HasMore);
    }

    /// <summary>
    /// Iterate all dump sites, automatically paging through results.
    /// </summary>
    public async IAsyncEnumerable<DumpSite> ListAllAsync(
        int pageSize = 100,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        int page = 1;
        while (true)
        {
            var result = await ListAsync(page, pageSize, ct);
            foreach (var item in result.Items)
                yield return item;
            if (!result.HasMore) break;
            page++;
        }
    }

    public async Task<DumpSite> GetAsync(string id, CancellationToken ct = default)
    {
        return await _http.GetAsync<DumpSite>($"api/v1/dump-sites/{id}", _tenantId, ct);
    }

    /// <summary>
    /// Create a new dump site. Returns the created dump site ID.
    /// Use <see cref="GetAsync"/> to fetch the full dump site after creation.
    /// </summary>
    public async Task<string> CreateAsync(CreateDumpSiteRequest request, CancellationToken ct = default)
    {
        return await _http.PostCreateAsync("api/v1/dump-sites", request, "dumpSiteId", _tenantId, ct);
    }

    public async Task UpdateAsync(string id, UpdateDumpSiteRequest request, CancellationToken ct = default)
    {
        await _http.PatchAsync<SuccessResponse>($"api/v1/dump-sites/{id}", request, _tenantId, ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await _http.DeleteAsync($"api/v1/dump-sites/{id}", _tenantId, ct);
    }

    // --- Material Pricing ---

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
