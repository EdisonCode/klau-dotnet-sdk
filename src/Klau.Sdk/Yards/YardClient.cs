using System.Runtime.CompilerServices;
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

        var response = await _http.GetListAsync<Yard>(path, "yards", _tenantId, ct);
        return new PagedResult<Yard>(response.Items, response.Total, response.Page, response.PageSize, response.HasMore);
    }

    /// <summary>
    /// Iterate all yards, automatically paging through results.
    /// </summary>
    public async IAsyncEnumerable<Yard> ListAllAsync(
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

    public async Task<Yard> GetAsync(string id, CancellationToken ct = default)
    {
        return await _http.GetAsync<Yard>($"api/v1/yards/{id}", _tenantId, ct);
    }

    /// <summary>
    /// Create a new yard. Returns the created yard ID.
    /// Use <see cref="GetAsync"/> to fetch the full yard after creation.
    /// </summary>
    public async Task<string> CreateAsync(CreateYardRequest request, CancellationToken ct = default)
    {
        return await _http.PostCreateAsync("api/v1/yards", request, "yardId", _tenantId, ct);
    }

    public async Task UpdateAsync(string id, UpdateYardRequest request, CancellationToken ct = default)
    {
        await _http.PatchAsync<SuccessResponse>($"api/v1/yards/{id}", request, _tenantId, ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await _http.DeleteAsync($"api/v1/yards/{id}", _tenantId, ct);
    }
}
