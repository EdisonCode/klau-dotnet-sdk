using Klau.Sdk.Common;

namespace Klau.Sdk.Import;

public interface IImportClient
{
    Task<ImportJobsResult> JobsAsync(ImportJobsRequest request, CancellationToken ct = default);
    Task<BatchReadiness> GetReadinessAsync(string batchId, CancellationToken ct = default);
    Task<ImportJobsResult> ImportAndWaitAsync(ImportJobsRequest request, TimeSpan? timeout = null, TimeSpan? pollInterval = null, CancellationToken ct = default);
}

/// <summary>
/// Client for the Klau import API. Provides bulk import using customer/site names
/// instead of pre-created IDs — the golden path for enterprise integrations.
/// </summary>
public sealed class ImportClient : IImportClient
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
    /// <returns>Import result with counts, per-row errors, and a batch ID for readiness polling.</returns>
    public async Task<ImportJobsResult> JobsAsync(ImportJobsRequest request, CancellationToken ct = default)
    {
        return await _http.PostAsync<ImportJobsResult>("api/v1/import/jobs", request, _tenantId, ct);
    }

    /// <summary>
    /// Check drive-time cache readiness for an import batch.
    /// After importing jobs with new site addresses, drive-time calculations run
    /// asynchronously in the background. Poll this endpoint until
    /// <see cref="BatchReadiness.Status"/> is <c>"ready"</c> before running
    /// optimization to ensure accurate commercial truck routing times.
    /// </summary>
    /// <param name="batchId">The batch ID returned from <see cref="JobsAsync"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<BatchReadiness> GetReadinessAsync(string batchId, CancellationToken ct = default)
    {
        return await _http.GetAsync<BatchReadiness>($"api/v1/import/batches/{batchId}/readiness", _tenantId, ct);
    }

    /// <summary>
    /// Import jobs and wait for drive-time cache warm-up to complete.
    /// This is the golden-path convenience method that chains:
    /// import → poll readiness → return when ready (or timeout).
    ///
    /// Polls every 2 seconds by default. If the import result has no batch ID
    /// (e.g. all sites already had cached drive times), returns immediately.
    /// </summary>
    /// <param name="request">The import request containing job records and options.</param>
    /// <param name="timeout">Max time to wait for readiness. Defaults to 60 seconds.</param>
    /// <param name="pollInterval">How often to poll. Defaults to 2 seconds.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The import result (drive-time cache is warm when this returns successfully).</returns>
    /// <exception cref="TimeoutException">Thrown when the cache doesn't reach "ready" within the timeout.</exception>
    public async Task<ImportJobsResult> ImportAndWaitAsync(
        ImportJobsRequest request,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken ct = default)
    {
        var result = await JobsAsync(request, ct);

        // No batch ID means no new sites to warm — nothing to wait for
        if (string.IsNullOrEmpty(result.BatchId))
            return result;

        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(60);
        var interval = pollInterval ?? TimeSpan.FromSeconds(2);
        var deadline = DateTime.UtcNow + effectiveTimeout;

        while (DateTime.UtcNow < deadline)
        {
            var readiness = await GetReadinessAsync(result.BatchId, ct);

            if (readiness.Status is "ready" or "not_applicable")
                return result;

            await Task.Delay(interval, ct);
        }

        throw new TimeoutException(
            $"Drive-time cache warm-up for batch '{result.BatchId}' did not complete within {effectiveTimeout.TotalSeconds}s. " +
            "You can still run optimization, but drive times may use Haversine estimates for new sites.");
    }
}
