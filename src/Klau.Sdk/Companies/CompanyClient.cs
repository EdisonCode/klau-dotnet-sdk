using Klau.Sdk.Common;

namespace Klau.Sdk.Companies;

public interface ICompanyClient
{
    Task<Company> GetAsync(CancellationToken ct = default);
    Task<Company> UpdateAsync(UpdateCompanyRequest request, CancellationToken ct = default);
}

/// <summary>
/// Client for the Klau company API. Provides access to the authenticated
/// company's profile and operational settings.
/// </summary>
public sealed class CompanyClient : ICompanyClient
{
    private readonly KlauHttpClient _http;
    private readonly string? _tenantId;

    internal CompanyClient(KlauHttpClient http, string? tenantId = null)
    {
        _http = http;
        _tenantId = tenantId;
    }

    /// <summary>
    /// Get the authenticated company's profile.
    /// Returns identity, location, operating hours, container configuration,
    /// subscription info, and dispatch automation settings.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The company profile.</returns>
    public async Task<Company> GetAsync(CancellationToken ct = default)
    {
        return await _http.GetAsync<Company>("api/v1/companies/me", _tenantId, ct);
    }

    /// <summary>
    /// Update the authenticated company's operational settings.
    /// Only non-null fields in the request are sent to the API.
    /// </summary>
    /// <param name="request">The fields to update.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated company profile.</returns>
    public async Task<Company> UpdateAsync(UpdateCompanyRequest request, CancellationToken ct = default)
    {
        return await _http.PatchAsync<Company>("api/v1/companies/me", request, _tenantId, ct);
    }
}
