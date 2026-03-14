namespace WebhookIntegration.Models;

/// <summary>
/// A job/work order as it exists in your source system.
/// Map these fields to match your actual database schema.
/// </summary>
public sealed record SourceJob
{
    /// <summary>Your system's unique order ID. Becomes the ExternalId in Klau.</summary>
    public required string OrderId { get; init; }

    public required string CustomerName { get; init; }
    public required string Address { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required string Zip { get; init; }

    /// <summary>Your system's service code (e.g. "DUMP_RETURN", "DELIVERY").</summary>
    public required string ServiceType { get; init; }

    /// <summary>Container size in yards (10, 15, 20, 30, 40). Null for non-container jobs.</summary>
    public int? ContainerSize { get; init; }

    public required string RequestedDate { get; init; }
    public string? DumpSite { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// A status change reported by a driver's tablet in your source system.
/// The sync service picks these up and pushes them to Klau so the
/// dispatch board reflects real-time progress.
/// </summary>
public sealed record StatusChange
{
    /// <summary>Your system's order ID.</summary>
    public required string SourceOrderId { get; init; }

    /// <summary>The corresponding Klau job ID (stored when the job was first synced).</summary>
    public required string KlauJobId { get; init; }

    /// <summary>The new status from the driver's tablet.</summary>
    public required SourceJobStatus NewStatus { get; init; }

    public required DateTime Timestamp { get; init; }
}

/// <summary>
/// Driver assignment data written back from Klau after optimization.
/// </summary>
public sealed record DriverAssignment
{
    public required string KlauJobId { get; init; }
    public required string DriverId { get; init; }
    public string? DriverName { get; init; }
    public int? Sequence { get; init; }
    public string? EstimatedStartTime { get; init; }
    public string? AssignmentSource { get; init; }
}

/// <summary>
/// Job statuses as they exist in your source system.
/// Map these to your actual status values.
/// </summary>
public enum SourceJobStatus
{
    New,
    SyncedToKlau,
    Assigned,
    InProgress,
    Completed
}
