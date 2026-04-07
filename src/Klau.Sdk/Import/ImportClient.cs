using Klau.Sdk.Common;

namespace Klau.Sdk.Import;

public interface IImportClient
{
    Task<ImportJobsResult> JobsAsync(ImportJobsRequest request, CancellationToken ct = default);
    Task<BatchReadiness> GetReadinessAsync(string batchId, CancellationToken ct = default);
    Task<ImportJobsResult> ImportAndWaitAsync(ImportJobsRequest request, TimeSpan? timeout = null, TimeSpan? pollInterval = null, CancellationToken ct = default);
    Task<AsyncImportSubmitResult> SubmitJobsAsync(ImportJobsRequest request, CancellationToken ct = default);
    Task<AsyncImportSubmitResult> SubmitJobsAsync(ImportJobsRequest request, KlauRequestOptions options, CancellationToken ct = default);
    Task<AsyncImportBatchStatus> GetBatchStatusAsync(string batchId, CancellationToken ct = default);
    Task<AsyncImportBatchStatus> SubmitAndWaitAsync(ImportJobsRequest request, TimeSpan? timeout = null, TimeSpan? pollInterval = null, CancellationToken ct = default);
}

/// <summary>
/// Client for the Klau import API. Provides bulk import using customer/site names
/// and addresses instead of pre-created IDs — you do NOT need to create customers
/// or sites before importing jobs.
///
/// When <see cref="ImportJobsRequest.CreateMissing"/> is true (the default),
/// customers and sites are auto-created by matching names case-insensitively.
/// New sites are geocoded and drive-time cached automatically.
///
/// Two import modes:
/// <list type="bullet">
///   <item><b>Async</b> (<see cref="SubmitJobsAsync"/> / <see cref="SubmitAndWaitAsync"/>):
///   Up to 1,000 jobs. Returns immediately; processing happens in the background.
///   Recommended for production integrations.</item>
///   <item><b>Sync</b> (<see cref="JobsAsync"/> / <see cref="ImportAndWaitAsync"/>):
///   Processes inline. Best for small batches and quick scripts.</item>
/// </list>
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
        return await _http.GetAsync<BatchReadiness>($"api/v1/import/batches/{QueryBuilder.PathEncode(batchId)}/readiness", _tenantId, ct);
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

    /// <summary>
    /// Submit jobs for asynchronous import. Returns immediately with a batch ID.
    /// Up to 1,000 jobs per request. No geocoding in the request path — all
    /// heavy processing happens in the background.
    ///
    /// Poll <see cref="GetBatchStatusAsync"/> (every 2 seconds) to track progress,
    /// or use <see cref="SubmitAndWaitAsync"/> for a one-call experience.
    /// </summary>
    public async Task<AsyncImportSubmitResult> SubmitJobsAsync(ImportJobsRequest request, CancellationToken ct = default)
    {
        return await _http.PostAsync<AsyncImportSubmitResult>("api/v1/import/jobs/async", request, _tenantId, ct);
    }

    /// <inheritdoc cref="SubmitJobsAsync(ImportJobsRequest, CancellationToken)"/>
    /// <param name="request">The import request containing job records and options.</param>
    /// <param name="options">Per-request options (idempotency key, timeout, tenant override).</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<AsyncImportSubmitResult> SubmitJobsAsync(ImportJobsRequest request, KlauRequestOptions options, CancellationToken ct = default)
    {
        return await _http.PostAsync<AsyncImportSubmitResult>("api/v1/import/jobs/async", request, _tenantId, options, ct);
    }

    /// <summary>
    /// Poll the processing status of an async import batch.
    /// Done when <see cref="AsyncImportBatchStatus.IsReadyForOptimization"/> is true.
    /// </summary>
    public async Task<AsyncImportBatchStatus> GetBatchStatusAsync(string batchId, CancellationToken ct = default)
    {
        return await _http.GetAsync<AsyncImportBatchStatus>(
            $"api/v1/import/batches/{QueryBuilder.PathEncode(batchId)}/status", _tenantId, ct);
    }

    /// <summary>
    /// Submit jobs for async import and poll until the batch is fully processed
    /// and the drive-time cache is warm. This is the golden-path convenience method
    /// that chains: submit → poll status → return when ready (or timeout).
    ///
    /// Polls every 2 seconds by default. Safe to run optimization after this returns
    /// without throwing.
    ///
    /// Note: each poll may trigger up to 3 retries with exponential backoff for
    /// transient errors. A degraded API can cause individual polls to take longer
    /// than the poll interval, reducing the effective number of status checks
    /// within the timeout window.
    /// </summary>
    /// <param name="request">The import request containing job records and options.</param>
    /// <param name="timeout">Max time to wait. Defaults to 120 seconds.</param>
    /// <param name="pollInterval">How often to poll. Defaults to 2 seconds.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Final batch status — safe to optimize when this returns without throwing.</returns>
    /// <exception cref="KlauImportException">
    /// Thrown when the batch reaches a terminal failure state (FAILED with no importable jobs),
    /// or when polling fails after a successful submission. The exception's
    /// <see cref="KlauImportException.BatchId"/> lets you resume polling manually.
    /// </exception>
    /// <exception cref="TimeoutException">Thrown when the batch doesn't reach a ready state within the timeout.</exception>
    public async Task<AsyncImportBatchStatus> SubmitAndWaitAsync(
        ImportJobsRequest request,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken ct = default)
    {
        var submitResult = await SubmitJobsAsync(request, ct);
        var batchId = submitResult.BatchId;

        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(120);
        var interval = pollInterval ?? TimeSpan.FromSeconds(2);
        var deadline = DateTime.UtcNow + effectiveTimeout;

        AsyncImportBatchStatus? lastStatus = null;
        try
        {
            while (DateTime.UtcNow < deadline)
            {
                lastStatus = await GetBatchStatusAsync(batchId, ct);

                if (lastStatus.IsReadyForOptimization)
                    return lastStatus;

                // Batch completely failed — no point waiting for cache warm-up.
                // COMPLETED/PARTIAL_FAILURE with WARMING cache keep polling because
                // the cache will eventually reach READY for the jobs that were imported.
                if (lastStatus.Status == ImportBatchStatus.FAILED)
                    return lastStatus;

                await Task.Delay(interval, ct);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not TimeoutException)
        {
            throw new KlauImportException(
                batchId,
                $"Polling failed for batch '{batchId}' after successful submission. " +
                "Use BatchId to resume polling via GetBatchStatusAsync.",
                lastStatus,
                ex);
        }

        throw new TimeoutException(
            $"Async import batch '{batchId}' did not complete within {effectiveTimeout.TotalSeconds}s. " +
            "Poll GetBatchStatusAsync to check progress.");
    }
}
