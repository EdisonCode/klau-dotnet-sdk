using Klau.Sdk.Common;

namespace Klau.Sdk.Dispatches;

public sealed class DispatchClient
{
    private readonly KlauHttpClient _http;
    private readonly string? _tenantId;

    internal DispatchClient(KlauHttpClient http, string? tenantId = null)
    {
        _http = http;
        _tenantId = tenantId;
    }

    /// <summary>
    /// Get the dispatch board for a given date.
    /// </summary>
    public async Task<DispatchBoard> GetBoardAsync(string date, CancellationToken ct = default)
    {
        return await _http.GetAsync<DispatchBoard>($"api/v1/dispatches/board?date={date}", _tenantId, ct);
    }

    /// <summary>
    /// Start an async optimization job. Returns immediately with a job ID for polling.
    /// </summary>
    public async Task<OptimizationJob> StartOptimizationAsync(
        OptimizeRequest request,
        CancellationToken ct = default)
    {
        return await _http.PostAsync<OptimizationJob>("api/v1/dispatches/optimize", request, _tenantId, ct);
    }

    /// <summary>
    /// Poll the status of an optimization job.
    /// </summary>
    public async Task<OptimizationJob> GetOptimizationStatusAsync(
        string jobId,
        CancellationToken ct = default)
    {
        return await _http.GetAsync<OptimizationJob>($"api/v1/dispatches/optimize/{jobId}", _tenantId, ct);
    }

    /// <summary>
    /// Start optimization and poll until complete (or cancelled).
    /// Polls every 2 seconds by default.
    /// </summary>
    public async Task<OptimizationJob> OptimizeAndWaitAsync(
        OptimizeRequest request,
        TimeSpan? pollInterval = null,
        CancellationToken ct = default)
    {
        var interval = pollInterval ?? TimeSpan.FromSeconds(2);
        var job = await StartOptimizationAsync(request, ct);

        while (job.Status is OptimizationJobStatus.PENDING or OptimizationJobStatus.RUNNING)
        {
            await Task.Delay(interval, ct);
            job = await GetOptimizationStatusAsync(job.JobId, ct);
        }

        return job;
    }

    /// <summary>
    /// Publish all DRAFT dispatches for a date.
    /// </summary>
    public async Task PublishAsync(string date, CancellationToken ct = default)
    {
        await _http.PostAsync("api/v1/dispatches/publish", new { date }, _tenantId, ct);
    }

    /// <summary>
    /// Reorder jobs within a dispatch.
    /// </summary>
    public async Task ReorderAsync(string dispatchId, ReorderRequest request, CancellationToken ct = default)
    {
        await _http.PostAsync($"api/v1/dispatches/{dispatchId}/reorder", request, _tenantId, ct);
    }

    /// <summary>
    /// Run a what-if simulation without persisting changes.
    /// </summary>
    public async Task<WhatIfResult> WhatIfAsync(WhatIfRequest request, CancellationToken ct = default)
    {
        return await _http.PostAsync<WhatIfResult>("api/v1/dispatches/what-if", request, _tenantId, ct);
    }
}
