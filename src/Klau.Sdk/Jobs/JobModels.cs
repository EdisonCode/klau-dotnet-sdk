using System.Text.Json.Serialization;
using Klau.Sdk.Common;

namespace Klau.Sdk.Jobs;

public sealed class Job
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public JobType Type { get; set; }

    [JsonPropertyName("status")]
    public JobStatus Status { get; set; }

    [JsonPropertyName("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    [JsonPropertyName("customerId")]
    public string? CustomerId { get; set; }

    [JsonPropertyName("siteId")]
    public string? SiteId { get; set; }

    [JsonPropertyName("siteAddress")]
    public string? SiteAddress { get; set; }

    [JsonPropertyName("containerSize")]
    public int? ContainerSize { get; set; }

    [JsonPropertyName("containerNumber")]
    public string? ContainerNumber { get; set; }

    [JsonPropertyName("scheduledDate")]
    public string? ScheduledDate { get; set; }

    [JsonPropertyName("requestedDate")]
    public string? RequestedDate { get; set; }

    [JsonPropertyName("timeWindow")]
    public TimeWindow? TimeWindow { get; set; }

    [JsonPropertyName("priority")]
    public JobPriority? Priority { get; set; }

    [JsonPropertyName("driverId")]
    public string? DriverId { get; set; }

    [JsonPropertyName("driverName")]
    public string? DriverName { get; set; }

    [JsonPropertyName("truckId")]
    public string? TruckId { get; set; }

    [JsonPropertyName("sequence")]
    public int? Sequence { get; set; }

    [JsonPropertyName("estimatedStartTime")]
    public string? EstimatedStartTime { get; set; }

    [JsonPropertyName("estimatedMinutes")]
    public int? EstimatedMinutes { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("orderId")]
    public string? OrderId { get; set; }

    [JsonPropertyName("dumpSiteId")]
    public string? DumpSiteId { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

public sealed class CreateJobRequest
{
    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [JsonPropertyName("siteId")]
    public string? SiteId { get; set; }

    [JsonPropertyName("siteAddress")]
    public string? SiteAddress { get; set; }

    [JsonPropertyName("type")]
    public JobType Type { get; set; }

    [JsonPropertyName("containerSize")]
    public int? ContainerSize { get; set; }

    [JsonPropertyName("requestedDate")]
    public string? RequestedDate { get; set; }

    [JsonPropertyName("timeWindow")]
    public TimeWindow? TimeWindow { get; set; }

    [JsonPropertyName("priority")]
    public JobPriority? Priority { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("estimatedMinutes")]
    public int? EstimatedMinutes { get; set; }

    [JsonPropertyName("orderId")]
    public string? OrderId { get; set; }

    [JsonPropertyName("dumpSiteId")]
    public string? DumpSiteId { get; set; }

    [JsonPropertyName("containerNumber")]
    public string? ContainerNumber { get; set; }
}

public sealed class UpdateJobRequest
{
    [JsonPropertyName("containerSize")]
    public int? ContainerSize { get; set; }

    [JsonPropertyName("requestedDate")]
    public string? RequestedDate { get; set; }

    [JsonPropertyName("timeWindow")]
    public TimeWindow? TimeWindow { get; set; }

    [JsonPropertyName("priority")]
    public JobPriority? Priority { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("estimatedMinutes")]
    public int? EstimatedMinutes { get; set; }

    [JsonPropertyName("dumpSiteId")]
    public string? DumpSiteId { get; set; }

    [JsonPropertyName("containerNumber")]
    public string? ContainerNumber { get; set; }
}

public sealed class AssignJobRequest
{
    [JsonPropertyName("driverId")]
    public string DriverId { get; set; } = string.Empty;

    [JsonPropertyName("truckId")]
    public string TruckId { get; set; } = string.Empty;

    [JsonPropertyName("sequence")]
    public int Sequence { get; set; }

    [JsonPropertyName("scheduledDate")]
    public string ScheduledDate { get; set; } = string.Empty;

    [JsonPropertyName("estimatedStartTime")]
    public string? EstimatedStartTime { get; set; }
}
