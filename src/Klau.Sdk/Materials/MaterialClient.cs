using Klau.Sdk.Common;

namespace Klau.Sdk.Materials;

public sealed class MaterialClient
{
    private readonly KlauHttpClient _http;
    private readonly string? _tenantId;

    internal MaterialClient(KlauHttpClient http, string? tenantId = null)
    {
        _http = http;
        _tenantId = tenantId;
    }

    /// <summary>
    /// List materials with optional filters.
    /// </summary>
    public async Task<PagedResult<Material>> ListAsync(
        bool? activeOnly = null,
        bool? storefrontOnly = null,
        CancellationToken ct = default)
    {
        var path = QueryBuilder.Build("api/v1/materials",
            ("activeOnly", activeOnly),
            ("storefrontOnly", storefrontOnly));

        var response = await _http.GetListAsync<Material>(path, "materials", _tenantId, ct);
        return new PagedResult<Material>(response.Items, response.Total, response.Page, response.PageSize, response.HasMore);
    }

    /// <summary>
    /// Get a single material by ID.
    /// </summary>
    public async Task<Material> GetAsync(string id, CancellationToken ct = default)
    {
        return await _http.GetAsync<Material>($"api/v1/materials/{id}", _tenantId, ct);
    }

    /// <summary>
    /// Create a custom material. Returns the created material ID.
    /// Use <see cref="GetAsync"/> to fetch the full material after creation.
    /// </summary>
    public async Task<string> CreateAsync(CreateMaterialRequest request, CancellationToken ct = default)
    {
        return await _http.PostCreateAsync("api/v1/materials", request, "materialId", _tenantId, ct);
    }

    /// <summary>
    /// Update a material.
    /// </summary>
    public async Task UpdateAsync(string id, UpdateMaterialRequest request, CancellationToken ct = default)
    {
        // API uses PUT for material updates and returns { success: true }
        await _http.PutAsync<SuccessResponse>($"api/v1/materials/{id}", request, _tenantId, ct);
    }

    /// <summary>
    /// Soft-delete a material (sets isActive to false).
    /// </summary>
    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await _http.DeleteAsync($"api/v1/materials/{id}", _tenantId, ct);
    }

    /// <summary>
    /// List available industry templates for seeding.
    /// </summary>
    public async Task<List<MaterialTemplate>> ListTemplatesAsync(CancellationToken ct = default)
    {
        // API returns { data: { templates: [...] } }
        var response = await _http.GetListAsync<MaterialTemplate>("api/v1/materials/templates", "templates", _tenantId, ct);
        return response.Items;
    }

    /// <summary>
    /// Clone industry templates into the tenant's material library.
    /// Returns a result with the number of created and skipped materials.
    /// </summary>
    public async Task<SeedFromTemplateResult> SeedFromTemplateAsync(
        List<string> templateCodes,
        CancellationToken ct = default)
    {
        return await _http.PostAsync<SeedFromTemplateResult>(
            "api/v1/materials/seed-from-template",
            new { templateCodes },
            _tenantId,
            ct);
    }
}
