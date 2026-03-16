using System.Runtime.CompilerServices;
using Klau.Sdk.Common;

namespace Klau.Sdk.Customers;

public sealed class CustomerClient
{
    private readonly KlauHttpClient _http;
    private readonly string? _tenantId;

    internal CustomerClient(KlauHttpClient http, string? tenantId = null)
    {
        _http = http;
        _tenantId = tenantId;
    }

    /// <summary>
    /// List customers with optional search filter.
    /// </summary>
    public async Task<PagedResult<Customer>> ListAsync(
        string? search = null,
        bool? includeInactive = null,
        int page = 1,
        int pageSize = 100,
        CancellationToken ct = default)
    {
        var path = QueryBuilder.Build("api/v1/customers",
            ("search", search),
            ("includeInactive", includeInactive),
            ("page", page),
            ("pageSize", pageSize));

        var response = await _http.GetListAsync<Customer>(path, "customers", _tenantId, ct);
        return new PagedResult<Customer>(response.Items, response.Total, response.Page, response.PageSize, response.HasMore);
    }

    /// <summary>
    /// Iterate all customers matching the filters, automatically paging through results.
    /// </summary>
    public async IAsyncEnumerable<Customer> ListAllAsync(
        string? search = null,
        bool? includeInactive = null,
        int pageSize = 100,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        int page = 1;
        while (true)
        {
            var result = await ListAsync(search, includeInactive, page, pageSize, ct);
            foreach (var item in result.Items)
                yield return item;
            if (!result.HasMore) break;
            page++;
        }
    }

    /// <summary>
    /// Get a customer by ID.
    /// </summary>
    public async Task<Customer> GetAsync(string id, CancellationToken ct = default)
    {
        return await _http.GetAsync<Customer>($"api/v1/customers/{id}", _tenantId, ct);
    }

    /// <summary>
    /// Get the full 360 customer view (orders, interactions, lifecycle).
    /// </summary>
    public async Task<Customer360> Get360Async(string id, CancellationToken ct = default)
    {
        return await _http.GetAsync<Customer360>($"api/v1/customers/{id}/360", _tenantId, ct);
    }

    /// <summary>
    /// Create a new customer. Returns the created customer ID.
    /// Use <see cref="GetAsync"/> to fetch the full customer after creation.
    /// </summary>
    public async Task<string> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        return await _http.PostCreateAsync("api/v1/customers", request, "customerId", _tenantId, ct);
    }

    /// <summary>
    /// Update an existing customer. Returns the updated customer.
    /// </summary>
    public async Task<Customer> UpdateAsync(string id, UpdateCustomerRequest request, CancellationToken ct = default)
    {
        return await _http.PatchAsync<Customer>($"api/v1/customers/{id}", request, _tenantId, ct);
    }

    /// <summary>
    /// Delete a customer.
    /// </summary>
    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await _http.DeleteAsync($"api/v1/customers/{id}", _tenantId, ct);
    }

    /// <summary>
    /// List a customer's sites.
    /// </summary>
    public async Task<List<Site>> ListSitesAsync(string customerId, CancellationToken ct = default)
    {
        return await _http.GetAsync<List<Site>>($"api/v1/customers/{customerId}/sites", _tenantId, ct);
    }
}
