using System.Text.Json.Serialization;
using Klau.Sdk.Common;
using Klau.Sdk.Jobs;

namespace Klau.Sdk.Dispatches;

public sealed class DispatchBoard
{
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("dispatches")]
    public List<Dispatch> Dispatches { get; set; } = [];

    [JsonPropertyName("unassignedJobs")]
    public List<Job> UnassignedJobs { get; set; } = [];
}

public sealed class Dispatch
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("driverId")]
    public string DriverId { get; set; } = string.Empty;

    [JsonPropertyName("driverName")]
    public string DriverName { get; set; } = string.Empty;

    [JsonPropertyName("truckId")]
    public string? TruckId { get; set; }

    [JsonPropertyName("status")]
    public DispatchStatus Status { get; set; }

    [JsonPropertyName("jobs")]
    public List<Job> Jobs { get; set; } = [];
}

public sealed class OptimizationJob
{
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public OptimizationJobStatus Status { get; set; }

    [JsonPropertyName("estimatedDurationSeconds")]
    public int? EstimatedDurationSeconds { get; set; }

    [JsonPropertyName("pollUrl")]
    public string? PollUrl { get; set; }

    [JsonPropertyName("result")]
    public OptimizationResult? Result { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

public sealed class OptimizationResult
{
    [JsonPropertyName("chainScore")]
    public int? ChainScore { get; set; }

    [JsonPropertyName("totalJobs")]
    public int? TotalJobs { get; set; }

    [JsonPropertyName("assignedJobs")]
    public int? AssignedJobs { get; set; }

    [JsonPropertyName("unassignedJobs")]
    public int? UnassignedJobs { get; set; }
}

public sealed class OptimizeRequest
{
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("optimizationMode")]
    public OptimizationMode? OptimizationMode { get; set; }

    [JsonPropertyName("regionId")]
    public string? RegionId { get; set; }

    [JsonPropertyName("yardId")]
    public string? YardId { get; set; }
}

public sealed class ReorderRequest
{
    [JsonPropertyName("jobIds")]
    public List<string> JobIds { get; set; } = [];
}

public sealed class WhatIfRequest
{
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("changes")]
    public object? Changes { get; set; }
}

public sealed class WhatIfResult
{
    [JsonPropertyName("chainScore")]
    public int? ChainScore { get; set; }

    [JsonPropertyName("dispatches")]
    public List<Dispatch>? Dispatches { get; set; }
}
