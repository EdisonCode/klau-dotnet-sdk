using System.Text.Json.Serialization;
using Klau.Sdk.Common;

namespace Klau.Sdk.Jobs;

public sealed record Job
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("companyId")]
    public string CompanyId { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public JobType Type { get; init; }

    [JsonPropertyName("status")]
    public JobStatus Status { get; init; }

    [JsonPropertyName("customerName")]
    public string CustomerName { get; init; } = string.Empty;

    [JsonPropertyName("customerId")]
    public string? CustomerId { get; init; }

    [JsonPropertyName("siteId")]
    public string? SiteId { get; init; }

    [JsonPropertyName("siteAddress")]
    public string? SiteAddress { get; init; }

    [JsonPropertyName("containerSize")]
    public int? ContainerSize { get; init; }

    [JsonPropertyName("containerNumber")]
    public string? ContainerNumber { get; init; }

    [JsonPropertyName("containerSlot")]
    public ContainerSlot? ContainerSlot { get; init; }

    [JsonPropertyName("scheduledDate")]
    public string? ScheduledDate { get; init; }

    [JsonPropertyName("requestedDate")]
    public string? RequestedDate { get; init; }

    [JsonPropertyName("timeWindow")]
    public TimeWindow? TimeWindow { get; init; }

    [JsonPropertyName("priority")]
    public JobPriority? Priority { get; init; }

    [JsonPropertyName("driverId")]
    public string? DriverId { get; init; }

    [JsonPropertyName("driverName")]
    public string? DriverName { get; init; }

    [JsonPropertyName("truckId")]
    public string? TruckId { get; init; }

    [JsonPropertyName("dispatchId")]
    public string? DispatchId { get; init; }

    [JsonPropertyName("sequence")]
    public int? Sequence { get; init; }

    [JsonPropertyName("estimatedStartTime")]
    public string? EstimatedStartTime { get; init; }

    [JsonPropertyName("estimatedMinutes")]
    public int? EstimatedMinutes { get; init; }

    [JsonPropertyName("notes")]
    public string? Notes { get; init; }

    [JsonPropertyName("externalId")]
    public string? ExternalId { get; init; }

    [JsonPropertyName("orderId")]
    public string? OrderId { get; init; }

    [JsonPropertyName("dumpSiteId")]
    public string? DumpSiteId { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; init; }
}

public sealed record CreateJobRequest
{
    [JsonPropertyName("customerId")]
    public required string CustomerId { get; init; }

    [JsonPropertyName("siteId")]
    public required string SiteId { get; init; }

    [JsonPropertyName("siteAddress")]
    public string? SiteAddress { get; init; }

    [JsonPropertyName("type")]
    public required JobType Type { get; init; }

    [JsonPropertyName("containerSize")]
    public int? ContainerSize { get; init; }

    [JsonPropertyName("requestedDate")]
    public required string RequestedDate { get; init; }

    [JsonPropertyName("timeWindow")]
    public TimeWindow? TimeWindow { get; init; }

    [JsonPropertyName("priority")]
    public JobPriority? Priority { get; init; }

    [JsonPropertyName("notes")]
    public string? Notes { get; init; }

    [JsonPropertyName("estimatedMinutes")]
    public int? EstimatedMinutes { get; init; }

    [JsonPropertyName("orderId")]
    public string? OrderId { get; init; }

    [JsonPropertyName("dumpSiteId")]
    public string? DumpSiteId { get; init; }

    [JsonPropertyName("containerNumber")]
    public string? ContainerNumber { get; init; }

    /// <summary>
    /// External system ID for bidirectional sync (e.g. your ERP work order ID).
    /// </summary>
    [JsonPropertyName("externalId")]
    public string? ExternalId { get; init; }
}

public sealed record UpdateJobRequest
{
    [JsonPropertyName("customerId")]
    public string? CustomerId { get; init; }

    [JsonPropertyName("siteId")]
    public string? SiteId { get; init; }

    [JsonPropertyName("type")]
    public JobType? Type { get; init; }

    [JsonPropertyName("containerSize")]
    public int? ContainerSize { get; init; }

    [JsonPropertyName("requestedDate")]
    public string? RequestedDate { get; init; }

    [JsonPropertyName("timeWindow")]
    public TimeWindow? TimeWindow { get; init; }

    [JsonPropertyName("priority")]
    public JobPriority? Priority { get; init; }

    [JsonPropertyName("notes")]
    public string? Notes { get; init; }

    [JsonPropertyName("estimatedMinutes")]
    public int? EstimatedMinutes { get; init; }

    [JsonPropertyName("dumpSiteId")]
    public string? DumpSiteId { get; init; }

    [JsonPropertyName("preferredYardId")]
    public string? PreferredYardId { get; init; }

    [JsonPropertyName("containerNumber")]
    public string? ContainerNumber { get; init; }

    [JsonPropertyName("containerSlot")]
    public ContainerSlot? ContainerSlot { get; init; }

    /// <summary>Actual start time (ISO 8601). Set to null to clear.</summary>
    [JsonPropertyName("actualStartTime")]
    public string? ActualStartTime { get; init; }

    /// <summary>Actual end time (ISO 8601). Set to null to clear.</summary>
    [JsonPropertyName("actualEndTime")]
    public string? ActualEndTime { get; init; }
}

public sealed record BatchCreateResult
{
    [JsonPropertyName("created")]
    public IReadOnlyList<BatchJobResult> Created { get; init; } = [];

    [JsonPropertyName("errors")]
    public IReadOnlyList<BatchJobError> Errors { get; init; } = [];
}

public sealed record BatchJobResult
{
    [JsonPropertyName("jobId")]
    public string JobId { get; init; } = string.Empty;

    [JsonPropertyName("externalId")]
    public string? ExternalId { get; init; }
}

public sealed record BatchJobError
{
    [JsonPropertyName("index")]
    public int Index { get; init; }

    [JsonPropertyName("externalId")]
    public string? ExternalId { get; init; }

    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
}

public sealed record AssignJobRequest
{
    [JsonPropertyName("driverId")]
    public required string DriverId { get; init; }

    [JsonPropertyName("truckId")]
    public required string TruckId { get; init; }

    [JsonPropertyName("sequence")]
    public required int Sequence { get; init; }

    [JsonPropertyName("scheduledDate")]
    public required string ScheduledDate { get; init; }

    [JsonPropertyName("estimatedStartTime")]
    public string? EstimatedStartTime { get; init; }
}

/// <summary>
/// Result from the assign job endpoint.
/// </summary>
public sealed record AssignJobResult
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }
}

/// <summary>
/// Result from the unassign job endpoint.
/// </summary>
public sealed record UnassignJobResult
{
    [JsonPropertyName("jobId")]
    public string JobId { get; init; } = string.Empty;

    [JsonPropertyName("previousDriverId")]
    public string? PreviousDriverId { get; init; }

    [JsonPropertyName("previousDispatchId")]
    public string? PreviousDispatchId { get; init; }
}

/// <summary>
/// A single telemetry entry pushing actual start/end times for a job.
/// Closes the data flywheel so Klau learns real service times.
/// Provide at least one of <see cref="JobId"/> or <see cref="ExternalId"/>.
/// </summary>
public sealed record TelemetryEntry
{
    /// <summary>Klau job ID. Optional if <see cref="ExternalId"/> is provided.</summary>
    [JsonPropertyName("jobId")]
    public string? JobId { get; init; }

    /// <summary>External system ID. Optional if <see cref="JobId"/> is provided.</summary>
    [JsonPropertyName("externalId")]
    public string? ExternalId { get; init; }

    /// <summary>Actual start time (ISO 8601). Nullable.</summary>
    [JsonPropertyName("actualStartTime")]
    public string? ActualStartTime { get; init; }

    /// <summary>Actual end time (ISO 8601). Nullable.</summary>
    [JsonPropertyName("actualEndTime")]
    public string? ActualEndTime { get; init; }
}

/// <summary>
/// Result from the telemetry batch endpoint.
/// </summary>
public sealed record BatchTelemetryResult
{
    [JsonPropertyName("processed")]
    public int Processed { get; init; }

    [JsonPropertyName("updated")]
    public int Updated { get; init; }

    [JsonPropertyName("notFound")]
    public IReadOnlyList<string> NotFound { get; init; } = [];

    [JsonPropertyName("errors")]
    public IReadOnlyList<TelemetryError> Errors { get; init; } = [];
}

/// <summary>
/// A per-entry error from the telemetry batch endpoint.
/// </summary>
public sealed record TelemetryError
{
    /// <summary>The jobId or externalId that caused the error.</summary>
    [JsonPropertyName("ref")]
    public string Ref { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
}
