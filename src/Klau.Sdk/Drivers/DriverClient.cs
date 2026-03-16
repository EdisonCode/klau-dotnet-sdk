using Klau.Sdk.Common;

namespace Klau.Sdk.Drivers;

public sealed class DriverClient
{
    private readonly KlauHttpClient _http;
    private readonly string? _tenantId;

    internal DriverClient(KlauHttpClient http, string? tenantId = null)
    {
        _http = http;
        _tenantId = tenantId;
    }

    public async Task<PagedResult<Driver>> ListAsync(
        bool? activeOnly = null,
        int page = 1,
        int pageSize = 100,
        CancellationToken ct = default)
    {
        var path = QueryBuilder.Build("api/v1/drivers",
            ("activeOnly", activeOnly),
            ("page", page),
            ("pageSize", pageSize));

        var response = await _http.GetListAsync<Driver>(path, "drivers", _tenantId, ct);
        return new PagedResult<Driver>(response.Items, response.Total, response.Page, response.PageSize, response.HasMore);
    }

    public async Task<Driver> GetAsync(string id, CancellationToken ct = default)
    {
        return await _http.GetAsync<Driver>($"api/v1/drivers/{id}", _tenantId, ct);
    }

    /// <summary>
    /// Create a new driver. Returns the created driver ID.
    /// Use <see cref="GetAsync"/> to fetch the full driver after creation.
    /// </summary>
    public async Task<string> CreateAsync(CreateDriverRequest request, CancellationToken ct = default)
    {
        return await _http.PostCreateAsync("api/v1/drivers", request, "driverId", _tenantId, ct);
    }

    /// <summary>
    /// Update an existing driver. Returns the updated driver.
    /// </summary>
    public async Task<Driver> UpdateAsync(string id, UpdateDriverRequest request, CancellationToken ct = default)
    {
        return await _http.PatchAsync<Driver>($"api/v1/drivers/{id}", request, _tenantId, ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await _http.DeleteAsync($"api/v1/drivers/{id}", _tenantId, ct);
    }
}
