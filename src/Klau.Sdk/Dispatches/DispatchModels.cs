using System.Text.Json.Serialization;
using Klau.Sdk.Common;
using Klau.Sdk.Jobs;

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
    public IReadOnlyList<Job> UnassignedJobs { get; init; } = [];

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
    public IReadOnlyList<Job> Jobs { get; init; } = [];

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
