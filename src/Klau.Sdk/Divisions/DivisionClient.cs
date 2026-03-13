using Klau.Sdk.Common;

namespace Klau.Sdk.Divisions;

public sealed class DivisionClient
{
    private readonly KlauHttpClient _http;

    internal DivisionClient(KlauHttpClient http) => _http = http;

    /// <summary>
    /// List all child divisions under the authenticated parent company.
    /// </summary>
    public async Task<List<Division>> ListAsync(CancellationToken ct = default)
    {
        return await _http.GetAsync<List<Division>>("api/v1/divisions", ct);
    }

    /// <summary>
    /// Get a single division with expanded detail.
    /// </summary>
    public async Task<DivisionDetail> GetAsync(string id, CancellationToken ct = default)
    {
        return await _http.GetAsync<DivisionDetail>($"api/v1/divisions/{id}", ct);
    }

    /// <summary>
    /// Create a new child division.
    /// </summary>
    public async Task<Division> CreateAsync(CreateDivisionRequest request, CancellationToken ct = default)
    {
        return await _http.PostAsync<Division>("api/v1/divisions", request, ct);
    }

    /// <summary>
    /// Update a division.
    /// </summary>
    public async Task<Division> UpdateAsync(string id, UpdateDivisionRequest request, CancellationToken ct = default)
    {
        return await _http.PatchAsync<Division>($"api/v1/divisions/{id}", request, ct);
    }

    /// <summary>
    /// Get aggregate usage across all divisions.
    /// </summary>
    public async Task<UsageSummary> GetUsageSummaryAsync(CancellationToken ct = default)
    {
        return await _http.GetAsync<UsageSummary>("api/v1/divisions/usage-summary", ct);
    }

    /// <summary>
    /// Get usage detail for a specific division (last 30 days).
    /// </summary>
    public async Task<DivisionUsage> GetUsageAsync(string id, CancellationToken ct = default)
    {
        return await _http.GetAsync<DivisionUsage>($"api/v1/divisions/{id}/usage", ct);
    }

    /// <summary>
    /// Invite a user to a division.
    /// </summary>
    public async Task<Invitation> InviteUserAsync(string id, InviteUserRequest request, CancellationToken ct = default)
    {
        return await _http.PostAsync<Invitation>($"api/v1/divisions/{id}/invite", request, ct);
    }

    /// <summary>
    /// List all managed divisions (vendor/corporate API key).
    /// Uses the /corporate/divisions endpoint for enterprise vendor accounts.
    /// </summary>
    public async Task<List<Division>> ListCorporateAsync(CancellationToken ct = default)
    {
        return await _http.GetAsync<List<Division>>("api/v1/corporate/divisions", ct);
    }
}
