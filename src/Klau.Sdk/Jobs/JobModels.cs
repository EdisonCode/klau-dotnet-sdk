using System.Text.Json.Serialization;
using Klau.Sdk.Common;

namespace Klau.Sdk.Jobs;

public sealed record Job
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

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
    public string? SiteId { get; init; }

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
    /// External system ID for bidirectional sync (e.g. your RMO order ID).
    /// </summary>
    [JsonPropertyName("externalId")]
    public string? ExternalId { get; init; }
}

public sealed record UpdateJobRequest
{
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

    [JsonPropertyName("containerNumber")]
    public string? ContainerNumber { get; init; }
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
