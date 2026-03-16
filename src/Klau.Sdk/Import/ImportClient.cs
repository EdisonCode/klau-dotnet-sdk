using Klau.Sdk.Common;

namespace Klau.Sdk.Import;

/// <summary>
/// Client for the Klau import API. Provides bulk import using customer/site names
/// instead of pre-created IDs — the golden path for enterprise integrations.
/// </summary>
public sealed class ImportClient
{
    private readonly KlauHttpClient _http;
    private readonly string? _tenantId;

    internal ImportClient(KlauHttpClient http, string? tenantId = null)
    {
        _http = http;
        _tenantId = tenantId;
    }

    /// <summary>
    /// Import jobs using customer names and site addresses.
    /// When <see cref="ImportJobsRequest.CreateMissing"/> is true (the default),
    /// customers and sites that don't exist are auto-created.
    ///
    /// This is the recommended path for enterprise integrations that sync jobs
    /// from external systems (ERP, dispatch, CSV uploads) without needing to
    /// pre-create customer and site records.
    /// </summary>
    /// <param name="request">The import request containing job records and options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Import result with counts and per-row errors.</returns>
    public async Task<ImportJobsResult> JobsAsync(ImportJobsRequest request, CancellationToken ct = default)
    {
        return await _http.PostAsync<ImportJobsResult>("api/v1/import/jobs", request, _tenantId, ct);
    }
}
