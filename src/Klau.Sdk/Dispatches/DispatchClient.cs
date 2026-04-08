using Klau.Sdk.Common;

namespace Klau.Sdk.Dispatches;

public interface IDispatchClient
{
    Task<DispatchBoard> GetBoardAsync(string date, CancellationToken ct = default);
    Task<OptimizationJob> StartOptimizationAsync(OptimizeRequest request, CancellationToken ct = default);
    Task<OptimizationJob> GetOptimizationStatusAsync(string jobId, CancellationToken ct = default);
    Task<OptimizationJob> OptimizeAndWaitAsync(OptimizeRequest request, TimeSpan? pollInterval = null, CancellationToken ct = default);
    Task PublishAsync(string date, CancellationToken ct = default);
    Task ReorderAsync(string dispatchId, ReorderRequest request, CancellationToken ct = default);
    Task<WhatIfResult> WhatIfAsync(WhatIfRequest request, CancellationToken ct = default);
    Task<PlanScoreResult> ScorePlanAsync(string date, bool includeDriverBreakdown = false, CancellationToken ct = default);
}

public sealed class DispatchClient : IDispatchClient
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
        var path = QueryBuilder.Build("api/v1/dispatches/board", ("date", date));
        return await _http.GetAsync<DispatchBoard>(path, _tenantId, ct);
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
        return await _http.GetAsync<OptimizationJob>($"api/v1/dispatches/optimize/{QueryBuilder.PathEncode(jobId)}", _tenantId, ct);
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
        await _http.PostAsync($"api/v1/dispatches/{QueryBuilder.PathEncode(dispatchId)}/reorder", request, _tenantId, ct);
    }

    /// <summary>
    /// Run a what-if simulation without persisting changes.
    /// </summary>
    public async Task<WhatIfResult> WhatIfAsync(WhatIfRequest request, CancellationToken ct = default)
    {
        return await _http.PostAsync<WhatIfResult>("api/v1/dispatches/what-if", request, _tenantId, ct);
    }

    /// <summary>
    /// Score an existing plan without running a fresh optimization. Pure read —
    /// no mutations, no concurrency lock. Use this after a pre-routed import to
    /// decide whether the inherited plan is good enough to keep (and skip the
    /// optimizer) or worth re-optimizing.
    ///
    /// The server returns a threshold-driven <see cref="PlanScoreRecommendation"/>
    /// (<c>KEEP_AS_IS</c> vs <c>RE_OPTIMIZE</c>) plus the underlying scores.
    ///
    /// <code>
    /// var score = await klau.Dispatches.ScorePlanAsync("2026-04-07", includeDriverBreakdown: true);
    /// if (score.Recommendation == PlanScoreRecommendation.RE_OPTIMIZE)
    ///     await klau.Dispatches.OptimizeAndWaitAsync(new OptimizeRequest { Date = score.Date });
    /// </code>
    /// </summary>
    /// <param name="date">Dispatch date in <c>YYYY-MM-DD</c> format.</param>
    /// <param name="includeDriverBreakdown">When true, include per-driver scores in the response.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="KlauApiException">
    /// Thrown with code <c>VALIDATION_ERROR</c> (400) for a malformed date,
    /// or <c>DISPATCH_NOT_FOUND</c> (404) when no jobs are scheduled for the date.
    /// </exception>
    public async Task<PlanScoreResult> ScorePlanAsync(
        string date,
        bool includeDriverBreakdown = false,
        CancellationToken ct = default)
    {
        var body = new PlanScoreRequest { IncludeDriverBreakdown = includeDriverBreakdown };
        return await _http.PostAsync<PlanScoreResult>(
            $"api/v1/dispatches/{QueryBuilder.PathEncode(date)}/score",
            body,
            _tenantId,
            ct);
    }
}
