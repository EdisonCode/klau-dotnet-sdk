using System.Collections.Concurrent;
using WebhookIntegration.Models;

namespace WebhookIntegration.Database;

/// <summary>
/// In-memory mock of your source database.
/// Replace this with your real implementation (EF Core, Dapper, ADO.NET, etc.).
///
/// This mock seeds 6 sample jobs and simulates driver tablet status changes
/// after jobs are assigned by Klau's optimizer.
/// </summary>
public sealed class MockSourceDatabase : ISourceDatabase
{
    private readonly ConcurrentDictionary<string, MockJobRecord> _jobs = new();
    private readonly ConcurrentDictionary<string, StatusChange> _pendingStatusChanges = new();
    private readonly ILogger<MockSourceDatabase> _logger;

    public MockSourceDatabase(ILogger<MockSourceDatabase> logger)
    {
        _logger = logger;
        SeedData();
    }

    private void SeedData()
    {
        var today = DateTime.Today.ToString("yyyy-MM-dd");

        var seed = new[]
        {
            new SourceJob
            {
                OrderId = "WO-10001",
                CustomerName = "Summit Demolition LLC",
                Address = "1450 Main St",
                City = "Harrisburg",
                State = "PA",
                Zip = "17101",
                ServiceType = "DUMP_RETURN",
                ContainerSize = 30,
                RequestedDate = today,
                DumpSite = "Central Landfill",
            },
            new SourceJob
            {
                OrderId = "WO-10002",
                CustomerName = "Keystone Roofing Co",
                Address = "320 Market St",
                City = "York",
                State = "PA",
                Zip = "17401",
                ServiceType = "DUMP_RETURN",
                ContainerSize = 20,
                RequestedDate = today,
                DumpSite = "Central Landfill",
            },
            new SourceJob
            {
                OrderId = "WO-10003",
                CustomerName = "Penn State Facilities",
                Address = "100 College Ave",
                City = "State College",
                State = "PA",
                Zip = "16801",
                ServiceType = "DELIVERY",
                ContainerSize = 40,
                RequestedDate = today,
            },
            new SourceJob
            {
                OrderId = "WO-10004",
                CustomerName = "Liberty Bell Builders",
                Address = "2200 Broad St",
                City = "Philadelphia",
                State = "PA",
                Zip = "19102",
                ServiceType = "PICKUP",
                ContainerSize = 30,
                RequestedDate = today,
                Notes = "Gate code: 4455",
            },
            new SourceJob
            {
                OrderId = "WO-10005",
                CustomerName = "Valley Forge Concrete",
                Address = "875 Industrial Blvd",
                City = "King of Prussia",
                State = "PA",
                Zip = "19406",
                ServiceType = "DUMP_RETURN",
                ContainerSize = 30,
                RequestedDate = today,
                DumpSite = "Central Landfill",
            },
            new SourceJob
            {
                OrderId = "WO-10006",
                CustomerName = "Bethlehem Steel Redevelopment",
                Address = "1800 3rd St",
                City = "Bethlehem",
                State = "PA",
                Zip = "18015",
                ServiceType = "DELIVERY",
                ContainerSize = 40,
                RequestedDate = today,
                Notes = "Construction entrance on south side",
            },
        };

        foreach (var job in seed)
        {
            _jobs[job.OrderId] = new MockJobRecord
            {
                Job = job,
                Status = SourceJobStatus.New,
            };
        }

        _logger.LogInformation("Mock database seeded with {Count} jobs", seed.Length);
    }

    // ─── Outbound ──────────────────────────────────────────────────────────

    public Task<IReadOnlyList<SourceJob>> GetPendingJobsAsync()
    {
        var pending = _jobs.Values
            .Where(r => r.Status == SourceJobStatus.New)
            .Select(r => r.Job)
            .ToList();

        return Task.FromResult<IReadOnlyList<SourceJob>>(pending);
    }

    public Task RecordKlauJobIdsAsync(IReadOnlyList<(string SourceOrderId, string KlauJobId)> syncedJobs)
    {
        foreach (var (sourceId, klauId) in syncedJobs)
        {
            if (_jobs.TryGetValue(sourceId, out var record))
            {
                record.KlauJobId = klauId;
                record.Status = SourceJobStatus.SyncedToKlau;
                _logger.LogInformation(
                    "Recorded Klau job {KlauJobId} for order {OrderId}",
                    klauId, sourceId);
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<StatusChange>> GetPendingStatusChangesAsync()
    {
        var changes = _pendingStatusChanges.Values.ToList();
        return Task.FromResult<IReadOnlyList<StatusChange>>(changes);
    }

    public Task MarkStatusSyncedAsync(string sourceOrderId)
    {
        _pendingStatusChanges.TryRemove(sourceOrderId, out _);
        return Task.CompletedTask;
    }

    // ─── Inbound ───────────────────────────────────────────────────────────

    public Task RecordAssignmentAsync(string sourceOrderId, DriverAssignment assignment)
    {
        if (_jobs.TryGetValue(sourceOrderId, out var record))
        {
            record.Status = SourceJobStatus.Assigned;
            record.Assignment = assignment;
            _logger.LogInformation(
                "Recorded assignment for {OrderId}: driver={DriverName} seq={Sequence}",
                sourceOrderId, assignment.DriverName, assignment.Sequence);
        }

        return Task.CompletedTask;
    }

    public Task<string?> FindSourceOrderIdAsync(string klauJobId)
    {
        var match = _jobs.Values.FirstOrDefault(r => r.KlauJobId == klauJobId);
        return Task.FromResult(match?.Job.OrderId);
    }

    // ─── Mock helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Simulate a driver starting a job on their tablet.
    /// Call this manually (or via a test endpoint) to create a status change
    /// that the sync service will pick up and push to Klau.
    /// </summary>
    public void SimulateDriverStart(string sourceOrderId)
    {
        if (!_jobs.TryGetValue(sourceOrderId, out var record) || record.KlauJobId is null)
            return;

        record.Status = SourceJobStatus.InProgress;
        _pendingStatusChanges[sourceOrderId] = new StatusChange
        {
            SourceOrderId = sourceOrderId,
            KlauJobId = record.KlauJobId,
            NewStatus = SourceJobStatus.InProgress,
            Timestamp = DateTime.UtcNow,
        };

        _logger.LogInformation("Simulated driver START for {OrderId}", sourceOrderId);
    }

    /// <summary>
    /// Simulate a driver completing a job on their tablet.
    /// </summary>
    public void SimulateDriverComplete(string sourceOrderId)
    {
        if (!_jobs.TryGetValue(sourceOrderId, out var record) || record.KlauJobId is null)
            return;

        record.Status = SourceJobStatus.Completed;
        _pendingStatusChanges[sourceOrderId] = new StatusChange
        {
            SourceOrderId = sourceOrderId,
            KlauJobId = record.KlauJobId,
            NewStatus = SourceJobStatus.Completed,
            Timestamp = DateTime.UtcNow,
        };

        _logger.LogInformation("Simulated driver COMPLETE for {OrderId}", sourceOrderId);
    }

    /// <summary>Dump current state for the status endpoint.</summary>
    public IReadOnlyList<object> GetAllJobStatus()
    {
        return _jobs.Values
            .OrderBy(r => r.Job.OrderId)
            .Select(r => new
            {
                orderId = r.Job.OrderId,
                customer = r.Job.CustomerName,
                status = r.Status.ToString(),
                klauJobId = r.KlauJobId,
                driver = r.Assignment?.DriverName,
                sequence = r.Assignment?.Sequence,
            })
            .ToList<object>();
    }

    private sealed class MockJobRecord
    {
        public required SourceJob Job { get; init; }
        public SourceJobStatus Status { get; set; }
        public string? KlauJobId { get; set; }
        public DriverAssignment? Assignment { get; set; }
    }
}
