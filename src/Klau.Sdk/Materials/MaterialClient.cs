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
    public async Task<List<Material>> ListAsync(
        bool? activeOnly = null,
        bool? storefrontOnly = null,
        CancellationToken ct = default)
    {
        var path = QueryBuilder.Build("api/v1/materials",
            ("activeOnly", activeOnly),
            ("storefrontOnly", storefrontOnly));

        return await _http.GetAsync<List<Material>>(path, _tenantId, ct);
    }

    /// <summary>
    /// Get a single material by ID.
    /// </summary>
    public async Task<Material> GetAsync(string id, CancellationToken ct = default)
    {
        return await _http.GetAsync<Material>($"api/v1/materials/{id}", _tenantId, ct);
    }

    /// <summary>
    /// Create a custom material.
    /// </summary>
    public async Task<Material> CreateAsync(CreateMaterialRequest request, CancellationToken ct = default)
    {
        return await _http.PostAsync<Material>("api/v1/materials", request, _tenantId, ct);
    }

    /// <summary>
    /// Update a material.
    /// </summary>
    public async Task<Material> UpdateAsync(string id, UpdateMaterialRequest request, CancellationToken ct = default)
    {
        return await _http.PatchAsync<Material>($"api/v1/materials/{id}", request, _tenantId, ct);
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
        return await _http.GetAsync<List<MaterialTemplate>>("api/v1/materials/templates", _tenantId, ct);
    }

    /// <summary>
    /// Clone industry templates into the tenant's material library.
    /// </summary>
    public async Task<List<Material>> SeedFromTemplateAsync(
        List<string> templateCodes,
        CancellationToken ct = default)
    {
        return await _http.PostAsync<List<Material>>(
            "api/v1/materials/seed-from-template",
            new { templateCodes },
            _tenantId,
            ct);
    }
}
