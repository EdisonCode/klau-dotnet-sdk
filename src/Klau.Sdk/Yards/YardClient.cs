using Klau.Sdk.Common;

namespace Klau.Sdk.Yards;

public sealed class YardClient
{
    private readonly KlauHttpClient _http;
    private readonly string? _tenantId;

    internal YardClient(KlauHttpClient http, string? tenantId = null)
    {
        _http = http;
        _tenantId = tenantId;
    }

    public async Task<PagedResult<Yard>> ListAsync(
        int page = 1,
        int pageSize = 100,
        CancellationToken ct = default)
    {
        var path = QueryBuilder.Build("api/v1/yards",
            ("page", page),
            ("pageSize", pageSize));

        var response = await _http.GetResponseAsync<List<Yard>>(path, _tenantId, ct);
        return new PagedResult<Yard>(response.Data, response.Meta);
    }

    public async Task<Yard> GetAsync(string id, CancellationToken ct = default)
    {
        return await _http.GetAsync<Yard>($"api/v1/yards/{id}", _tenantId, ct);
    }

    public async Task<Yard> CreateAsync(CreateYardRequest request, CancellationToken ct = default)
    {
        return await _http.PostAsync<Yard>("api/v1/yards", request, _tenantId, ct);
    }

    public async Task<Yard> UpdateAsync(string id, UpdateYardRequest request, CancellationToken ct = default)
    {
        return await _http.PatchAsync<Yard>($"api/v1/yards/{id}", request, _tenantId, ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await _http.DeleteAsync($"api/v1/yards/{id}", _tenantId, ct);
    }
}
