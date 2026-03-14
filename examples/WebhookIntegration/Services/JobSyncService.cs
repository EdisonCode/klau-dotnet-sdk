using Klau.Sdk;
using Klau.Sdk.Common;
using Klau.Sdk.Jobs;
using WebhookIntegration.Database;
using WebhookIntegration.Models;

namespace WebhookIntegration.Services;

/// <summary>
/// Background service that polls your source database on a regular interval
/// and syncs changes to Klau in both directions:
///
///   Source → Klau:
///     - New jobs that need to be dispatched
///     - Status changes from driver tablets (started, completed)
///
/// The reverse direction (Klau → Source) is handled by the webhook endpoint
/// in Program.cs, not by this polling service.
/// </summary>
public sealed class JobSyncService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<JobSyncService> _logger;
    private readonly TimeSpan _pollInterval;

    public JobSyncService(
        IServiceProvider services,
        ILogger<JobSyncService> logger,
        IConfiguration config)
    {
        _services = services;
        _logger = logger;
        _pollInterval = TimeSpan.FromMinutes(
            config.GetValue("Klau:PollIntervalMinutes", 5));
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation(
            "Job sync service started. Polling every {Interval} minutes", _pollInterval.TotalMinutes);

        // Check dispatch readiness on startup — surfaces missing configuration
        // (no drivers, trucks, yards, dump sites) before the first sync cycle.
        await CheckReadinessAsync(ct);

        // Run immediately on startup, then on interval
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await RunSyncCycleAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Sync cycle failed — will retry next interval");
            }

            await Task.Delay(_pollInterval, ct);
        }
    }

    /// <summary>
    /// Run the dispatch readiness check and log any missing configuration.
    /// This helps integration teams identify setup issues at startup rather
    /// than discovering them when optimization silently produces poor results.
    /// </summary>
    private async Task CheckReadinessAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _services.CreateScope();
            var klau = scope.ServiceProvider.GetRequiredService<KlauClient>();
            await klau.Readiness.CheckAndLogAsync(_logger, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Readiness check failed — continuing with sync");
        }
    }

    private async Task RunSyncCycleAsync(CancellationToken ct)
    {
        // Create a scope so we get fresh instances of scoped services
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISourceDatabase>();
        var klau = scope.ServiceProvider.GetRequiredService<KlauClient>();

        await SyncNewJobsAsync(db, klau, ct);
        await SyncStatusChangesAsync(db, klau, ct);
    }

    /// <summary>
    /// Find new jobs in your source DB and batch-create them in Klau.
    /// </summary>
    private async Task SyncNewJobsAsync(ISourceDatabase db, KlauClient klau, CancellationToken ct)
    {
        var pendingJobs = await db.GetPendingJobsAsync();
        if (pendingJobs.Count == 0) return;

        _logger.LogInformation("Found {Count} new jobs to sync to Klau", pendingJobs.Count);

        // Map source jobs to Klau create requests
        var requests = pendingJobs.Select(MapToCreateRequest).ToList();

        var result = await klau.Jobs.CreateBatchAsync(requests, ct);

        // Record successful creates
        if (result.Created.Count > 0)
        {
            var synced = result.Created
                .Where(c => c.ExternalId is not null)
                .Select(c => (SourceOrderId: c.ExternalId!, KlauJobId: c.JobId))
                .ToList();

            await db.RecordKlauJobIdsAsync(synced);

            _logger.LogInformation(
                "Created {Count} jobs in Klau", result.Created.Count);
        }

        // Log failures
        foreach (var error in result.Errors)
        {
            var sourceId = error.Index < pendingJobs.Count
                ? pendingJobs[error.Index].OrderId : "?";

            _logger.LogWarning(
                "Failed to create job {OrderId} in Klau: {Code} - {Message}",
                sourceId, error.Code, error.Message);
        }
    }

    /// <summary>
    /// Find status changes from driver tablets and push them to Klau.
    /// This keeps the Klau dispatch board in sync with real-world progress
    /// so the optimizer can adjust the day's plan (drivers ahead/behind schedule).
    /// </summary>
    private async Task SyncStatusChangesAsync(ISourceDatabase db, KlauClient klau, CancellationToken ct)
    {
        var changes = await db.GetPendingStatusChangesAsync();
        if (changes.Count == 0) return;

        _logger.LogInformation("Found {Count} status changes to sync to Klau", changes.Count);

        foreach (var change in changes)
        {
            try
            {
                switch (change.NewStatus)
                {
                    case SourceJobStatus.InProgress:
                        await klau.Jobs.StartAsync(change.KlauJobId, ct);
                        _logger.LogInformation(
                            "Pushed START to Klau for {OrderId} (Klau: {KlauJobId})",
                            change.SourceOrderId, change.KlauJobId);
                        break;

                    case SourceJobStatus.Completed:
                        await klau.Jobs.CompleteAsync(change.KlauJobId, ct);
                        _logger.LogInformation(
                            "Pushed COMPLETE to Klau for {OrderId} (Klau: {KlauJobId})",
                            change.SourceOrderId, change.KlauJobId);
                        break;

                    default:
                        _logger.LogDebug(
                            "Skipping status {Status} for {OrderId} — not a Klau transition",
                            change.NewStatus, change.SourceOrderId);
                        continue;
                }

                await db.MarkStatusSyncedAsync(change.SourceOrderId);
            }
            catch (KlauApiException ex)
            {
                // Don't fail the whole batch — log and continue.
                // Common: job already in target status (idempotent retry)
                _logger.LogWarning(
                    "Failed to sync status for {OrderId}: {Code} - {Message}",
                    change.SourceOrderId, ex.ErrorCode, ex.Message);
            }
        }
    }

    /// <summary>
    /// Map a source job to a Klau CreateJobRequest.
    /// Adapt this mapping to match your system's data model.
    /// </summary>
    private static CreateJobRequest MapToCreateRequest(SourceJob job) => new()
    {
        // In production, look up or auto-create the customer in Klau.
        // For now we use a placeholder — Klau's createMissing option
        // can auto-create customer stubs from the job data.
        CustomerId = "default",
        Type = MapJobType(job.ServiceType),
        ContainerSize = job.ContainerSize,
        RequestedDate = job.RequestedDate,
        TimeWindow = TimeWindow.ANYTIME,
        Notes = job.Notes,
        ExternalId = job.OrderId,
    };

    private static JobType MapJobType(string serviceType) => serviceType.ToUpperInvariant() switch
    {
        "DUMP_RETURN" => JobType.DUMP_RETURN,
        "DELIVERY" => JobType.DELIVERY,
        "PICKUP" => JobType.PICKUP,
        "SWAP" => JobType.SWAP,
        "SERVICE_VISIT" => JobType.SERVICE_VISIT,
        _ => JobType.SERVICE_VISIT,
    };
}
