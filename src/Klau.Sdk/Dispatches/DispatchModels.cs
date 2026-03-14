using System.Text.Json.Serialization;
using Klau.Sdk.Common;
using Klau.Sdk.Jobs;

namespace Klau.Sdk.Dispatches;

public sealed record DispatchBoard
{
    [JsonPropertyName("date")]
    public string Date { get; init; } = string.Empty;

    [JsonPropertyName("dispatches")]
    public IReadOnlyList<Dispatch> Dispatches { get; init; } = [];

    [JsonPropertyName("unassignedJobs")]
    public IReadOnlyList<Job> UnassignedJobs { get; init; } = [];
}

public sealed record Dispatch
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("driverId")]
    public string DriverId { get; init; } = string.Empty;

    [JsonPropertyName("driverName")]
    public string DriverName { get; init; } = string.Empty;

    [JsonPropertyName("truckId")]
    public string? TruckId { get; init; }

    [JsonPropertyName("status")]
    public DispatchStatus Status { get; init; }

    [JsonPropertyName("jobs")]
    public IReadOnlyList<Job> Jobs { get; init; } = [];
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
    [JsonPropertyName("chainScore")]
    public int? ChainScore { get; init; }

    [JsonPropertyName("totalJobs")]
    public int? TotalJobs { get; init; }

    [JsonPropertyName("assignedJobs")]
    public int? AssignedJobs { get; init; }

    [JsonPropertyName("unassignedJobs")]
    public int? UnassignedJobs { get; init; }
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
    [JsonPropertyName("chainScore")]
    public int? ChainScore { get; init; }

    [JsonPropertyName("dispatches")]
    public IReadOnlyList<Dispatch>? Dispatches { get; init; }
}
