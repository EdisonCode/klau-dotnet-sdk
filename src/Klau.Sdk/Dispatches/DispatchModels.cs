using System.Text.Json.Serialization;
using Klau.Sdk.Common;

namespace Klau.Sdk.Dispatches;

/// <summary>
/// Dispatch board response from GET /dispatches/board.
/// Contains driver routes, unassigned jobs, yard info, and metrics.
/// </summary>
public sealed record DispatchBoard
{
    [JsonPropertyName("date")]
    public string Date { get; init; } = string.Empty;

    [JsonPropertyName("drivers")]
    public IReadOnlyList<DispatchBoardDriver> Drivers { get; init; } = [];

    [JsonPropertyName("unassignedJobs")]
    public IReadOnlyList<DispatchBoardJob> UnassignedJobs { get; init; } = [];

    [JsonPropertyName("yard")]
    public YardInfo? Yard { get; init; }

    [JsonPropertyName("dumpSites")]
    public IReadOnlyList<DumpSiteInfo> DumpSites { get; init; } = [];

    [JsonPropertyName("metrics")]
    public DispatchBoardMetrics? Metrics { get; init; }

    [JsonPropertyName("dispatchStatus")]
    public string? DispatchStatus { get; init; }
}

/// <summary>
/// A job as it appears on the dispatch board. Includes drive-time and routing
/// fields that are only available in the dispatch board context (not on the
/// regular <c>GET /api/v1/jobs/:id</c> response).
/// </summary>
public sealed record DispatchBoardJob
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

    /// <summary>On-site service time only (minutes).</summary>
    [JsonPropertyName("estimatedMinutes")]
    public int? EstimatedMinutes { get; init; }

    /// <summary>
    /// Total isolated job cost: yard → site → dump → yard round trip + service time (minutes).
    /// Null for jobs without geocoded sites.
    /// </summary>
    [JsonPropertyName("baselineMinutes")]
    public int? BaselineMinutes { get; init; }

    /// <summary>
    /// Drive time TO this job from the previous stop (minutes).
    /// Populated after optimization; null for unassigned jobs or before first optimization.
    /// </summary>
    [JsonPropertyName("driveToMinutes")]
    public double? DriveToMinutes { get; init; }

    /// <summary>
    /// Distance TO this job from the previous stop (miles).
    /// Populated after optimization; null for unassigned jobs or before first optimization.
    /// </summary>
    [JsonPropertyName("driveToMiles")]
    public double? DriveToMiles { get; init; }

    /// <summary>
    /// Source of the drive time estimate for this job.
    /// <c>"haversine"</c> — straight-line estimate; <c>"cached"</c> — pre-warmed cache;
    /// <c>"routing_engine"</c> — real commercial truck routing (HERE Maps).
    /// Null when no drive time data is available.
    /// </summary>
    [JsonPropertyName("driveTimeSource")]
    public string? DriveTimeSource { get; init; }

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

/// <summary>
/// A driver's route on the dispatch board, including assigned jobs.
/// </summary>
public sealed record DispatchBoardDriver
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("color")]
    public string? Color { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("dispatchId")]
    public string? DispatchId { get; init; }

    [JsonPropertyName("truckId")]
    public string? TruckId { get; init; }

    [JsonPropertyName("truckNumber")]
    public string? TruckNumber { get; init; }

    [JsonPropertyName("truck")]
    public TruckInfo? Truck { get; init; }

    [JsonPropertyName("startTime")]
    public string? StartTime { get; init; }

    [JsonPropertyName("driverType")]
    public string? DriverType { get; init; }

    [JsonPropertyName("jobs")]
    public IReadOnlyList<DispatchBoardJob> Jobs { get; init; } = [];

    [JsonPropertyName("totalDriveMinutes")]
    public int TotalDriveMinutes { get; init; }

    [JsonPropertyName("totalServiceMinutes")]
    public int TotalServiceMinutes { get; init; }

    [JsonPropertyName("totalBufferMinutes")]
    public int TotalBufferMinutes { get; init; }

    [JsonPropertyName("score")]
    public int Score { get; init; }

    [JsonPropertyName("estimatedShiftCompletion")]
    public string? EstimatedShiftCompletion { get; init; }
}

public sealed record TruckInfo
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("truckNumber")]
    public string TruckNumber { get; init; } = string.Empty;

    [JsonPropertyName("supportedSizes")]
    public IReadOnlyList<int> SupportedSizes { get; init; } = [];
}

public sealed record YardInfo
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("address")]
    public string? Address { get; init; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; init; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; init; }
}

public sealed record DumpSiteInfo
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("address")]
    public string? Address { get; init; }

    [JsonPropertyName("lat")]
    public double? Lat { get; init; }

    [JsonPropertyName("lng")]
    public double? Lng { get; init; }
}

public sealed record DispatchBoardMetrics
{
    [JsonPropertyName("totalJobs")]
    public int TotalJobs { get; init; }

    [JsonPropertyName("assignedJobs")]
    public int AssignedJobs { get; init; }

    [JsonPropertyName("unassignedJobs")]
    public int UnassignedJobs { get; init; }

    [JsonPropertyName("completedJobs")]
    public int CompletedJobs { get; init; }

    [JsonPropertyName("flowScore")]
    public int? FlowScore { get; init; }

    [JsonPropertyName("planQuality")]
    public int? PlanQuality { get; init; }

    [JsonPropertyName("planGrade")]
    public string? PlanGrade { get; init; }
}

public sealed record OptimizationJob
{
    [JsonPropertyName("jobId")]
    public string JobId { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public OptimizationJobStatus Status { get; init; }

    [JsonPropertyName("estimatedDurationSeconds")]
    public int? EstimatedDurationSeconds { get; init; }

    [JsonPropertyName("pollUrl")]
    public string? PollUrl { get; init; }

    [JsonPropertyName("result")]
    public OptimizationResult? Result { get; init; }

    [JsonPropertyName("reason")]
    public string? Reason { get; init; }
}

public sealed record OptimizationResult
{
    /// <summary>
    /// Transition efficiency score (0-100). Measures the percentage of job-to-job
    /// transitions that don't require a yard return. Higher is better.
    /// </summary>
    [JsonPropertyName("flowScore")]
    public int? FlowScore { get; init; }

    /// <summary>Legacy chain score. Use FlowScore instead.</summary>
    [JsonPropertyName("chainScore")]
    public int? ChainScore { get; init; }

    [JsonPropertyName("totalJobs")]
    public int? TotalJobs { get; init; }

    [JsonPropertyName("assignedJobs")]
    public int? AssignedJobs { get; init; }

    [JsonPropertyName("unassignedJobs")]
    public int? UnassignedJobs { get; init; }

    /// <summary>
    /// Overall plan quality score (0-100) combining flow efficiency,
    /// geographic compactness, utilization balance, and more.
    /// </summary>
    [JsonPropertyName("planQuality")]
    public int? PlanQuality { get; init; }

    /// <summary>Plan quality letter grade (A+ through F).</summary>
    [JsonPropertyName("planGrade")]
    public string? PlanGrade { get; init; }

    /// <summary>
    /// Indicates whether real routing-engine drive times or Haversine estimates were used.
    /// <c>"API"</c> or <c>"ESTIMATED"</c>.
    /// </summary>
    [JsonPropertyName("driveTimeSource")]
    public string? DriveTimeSource { get; init; }
}

public sealed record OptimizeRequest
{
    [JsonPropertyName("date")]
    public required string Date { get; init; }

    [JsonPropertyName("optimizationMode")]
    public OptimizationMode? OptimizationMode { get; init; }

    [JsonPropertyName("regionId")]
    public string? RegionId { get; init; }

    [JsonPropertyName("yardId")]
    public string? YardId { get; init; }
}

public sealed record ReorderRequest
{
    [JsonPropertyName("jobIds")]
    public required IReadOnlyList<string> JobIds { get; init; }
}

public sealed record WhatIfRequest
{
    [JsonPropertyName("date")]
    public required string Date { get; init; }

    [JsonPropertyName("changes")]
    public object? Changes { get; init; }
}

public sealed record WhatIfResult
{
    [JsonPropertyName("flowScore")]
    public int? FlowScore { get; init; }

    [JsonPropertyName("chainScore")]
    public int? ChainScore { get; init; }

    [JsonPropertyName("planQuality")]
    public int? PlanQuality { get; init; }

    [JsonPropertyName("dispatches")]
    public IReadOnlyList<DispatchBoardDriver>? Dispatches { get; init; }
}
