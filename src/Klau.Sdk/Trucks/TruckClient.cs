using System.Runtime.CompilerServices;
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

        var response = await _http.GetListAsync<Truck>(path, "trucks", _tenantId, ct);
        return new PagedResult<Truck>(response.Items, response.Total, response.Page, response.PageSize, response.HasMore);
    }

    /// <summary>
    /// Iterate all trucks, automatically paging through results.
    /// </summary>
    public async IAsyncEnumerable<Truck> ListAllAsync(
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

    public async Task<Truck> GetAsync(string id, CancellationToken ct = default)
    {
        return await _http.GetAsync<Truck>($"api/v1/trucks/{id}", _tenantId, ct);
    }

    /// <summary>
    /// Create a new truck. Returns the created truck ID.
    /// Use <see cref="GetAsync"/> to fetch the full truck after creation.
    /// </summary>
    public async Task<string> CreateAsync(CreateTruckRequest request, CancellationToken ct = default)
    {
        return await _http.PostCreateAsync("api/v1/trucks", request, "truckId", _tenantId, ct);
    }

    public async Task UpdateAsync(string id, UpdateTruckRequest request, CancellationToken ct = default)
    {
        await _http.PatchAsync<SuccessResponse>($"api/v1/trucks/{id}", request, _tenantId, ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await _http.DeleteAsync($"api/v1/trucks/{id}", _tenantId, ct);
    }
}
