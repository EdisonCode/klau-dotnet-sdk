using Klau.Sdk.Common;

namespace Klau.Sdk.Trucks;

public sealed class TruckClient
{
    private readonly KlauHttpClient _http;
    private readonly string? _tenantId;

    internal TruckClient(KlauHttpClient http, string? tenantId = null)
    {
        _http = http;
        _tenantId = tenantId;
    }

    public async Task<PagedResult<Truck>> ListAsync(
        int page = 1,
        int pageSize = 100,
        CancellationToken ct = default)
    {
        var path = QueryBuilder.Build("api/v1/trucks",
            ("page", page),
            ("pageSize", pageSize));

        var response = await _http.GetResponseAsync<List<Truck>>(path, _tenantId, ct);
        return new PagedResult<Truck>(response.Data, response.Meta);
    }

    public async Task<Truck> GetAsync(string id, CancellationToken ct = default)
    {
        return await _http.GetAsync<Truck>($"api/v1/trucks/{id}", _tenantId, ct);
    }

    public async Task<Truck> CreateAsync(CreateTruckRequest request, CancellationToken ct = default)
    {
        return await _http.PostAsync<Truck>("api/v1/trucks", request, _tenantId, ct);
    }

    public async Task<Truck> UpdateAsync(string id, UpdateTruckRequest request, CancellationToken ct = default)
    {
        return await _http.PatchAsync<Truck>($"api/v1/trucks/{id}", request, _tenantId, ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await _http.DeleteAsync($"api/v1/trucks/{id}", _tenantId, ct);
    }
}
