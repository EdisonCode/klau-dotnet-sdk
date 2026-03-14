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

        var response = await _http.GetResponseAsync<List<Driver>>(path, _tenantId, ct);
        return new PagedResult<Driver>(response.Data, response.Meta);
    }

    public async Task<Driver> GetAsync(string id, CancellationToken ct = default)
    {
        return await _http.GetAsync<Driver>($"api/v1/drivers/{id}", _tenantId, ct);
    }

    public async Task<Driver> CreateAsync(CreateDriverRequest request, CancellationToken ct = default)
    {
        return await _http.PostAsync<Driver>("api/v1/drivers", request, _tenantId, ct);
    }

    public async Task<Driver> UpdateAsync(string id, UpdateDriverRequest request, CancellationToken ct = default)
    {
        return await _http.PatchAsync<Driver>($"api/v1/drivers/{id}", request, _tenantId, ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await _http.DeleteAsync($"api/v1/drivers/{id}", _tenantId, ct);
    }
}
