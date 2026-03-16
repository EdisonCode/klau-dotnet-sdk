using System.Text.Json.Serialization;

namespace Klau.Sdk.Webhooks;

/// <summary>
/// A webhook event delivered by Klau. All event types share this envelope;
/// the <see cref="Data"/> property contains event-specific fields.
/// </summary>
public sealed record WebhookEvent<T> where T : class
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("companyId")]
    public string CompanyId { get; init; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public T Data { get; init; } = default!;
}

/// <summary>
/// Untyped webhook event for routing before you know the event type.
/// Parse with <see cref="KlauWebhookParser"/> to get a typed event.
/// </summary>
public sealed record WebhookEvent
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("companyId")]
    public string CompanyId { get; init; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public System.Text.Json.JsonElement Data { get; init; }
}

// ── Job events ──────────────────────────────────────────────

/// <summary>Payload for <c>job.created</c>.</summary>
public sealed record JobCreatedEvent
{
    [JsonPropertyName("jobId")]
    public string JobId { get; init; } = string.Empty;

    [JsonPropertyName("jobType")]
    public string JobType { get; init; } = string.Empty;

    [JsonPropertyName("containerSize")]
    public int? ContainerSize { get; init; }

    [JsonPropertyName("siteId")]
    public string? SiteId { get; init; }
}

/// <summary>
/// Payload for <c>job.assigned</c>.
/// Fired after manual assignment or dispatch optimization.
/// </summary>
public sealed record JobAssignedEvent
{
    [JsonPropertyName("jobId")]
    public string JobId { get; init; } = string.Empty;

    [JsonPropertyName("driverId")]
    public string DriverId { get; init; } = string.Empty;

    [JsonPropertyName("dispatchId")]
    public string DispatchId { get; init; } = string.Empty;

    /// <summary><c>MANUAL</c> or <c>OPTIMIZATION</c>.</summary>
    [JsonPropertyName("assignmentSource")]
    public string? AssignmentSource { get; init; }
}

/// <summary>Payload for <c>job.unassigned</c>.</summary>
public sealed record JobUnassignedEvent
{
    [JsonPropertyName("jobId")]
    public string JobId { get; init; } = string.Empty;

    [JsonPropertyName("previousDriverId")]
    public string PreviousDriverId { get; init; } = string.Empty;
}

/// <summary>Payload for <c>job.status_changed</c>.</summary>
public sealed record JobStatusChangedEvent
{
    [JsonPropertyName("jobId")]
    public string JobId { get; init; } = string.Empty;

    [JsonPropertyName("previousStatus")]
    public string PreviousStatus { get; init; } = string.Empty;

    [JsonPropertyName("newStatus")]
    public string NewStatus { get; init; } = string.Empty;

    [JsonPropertyName("jobType")]
    public string JobType { get; init; } = string.Empty;

    [JsonPropertyName("driverId")]
    public string? DriverId { get; init; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = string.Empty;
}

/// <summary>Payload for <c>job.completed</c>.</summary>
public sealed record JobCompletedEvent
{
    [JsonPropertyName("jobId")]
    public string JobId { get; init; } = string.Empty;

    [JsonPropertyName("jobType")]
    public string JobType { get; init; } = string.Empty;

    [JsonPropertyName("driverId")]
    public string? DriverId { get; init; }

    [JsonPropertyName("completedAt")]
    public string CompletedAt { get; init; } = string.Empty;
}

// ── Dispatch events ─────────────────────────────────────────

/// <summary>Payload for <c>dispatch.optimized</c>.</summary>
public sealed record DispatchOptimizedEvent
{
    [JsonPropertyName("dispatchId")]
    public string DispatchId { get; init; } = string.Empty;

    [JsonPropertyName("date")]
    public string Date { get; init; } = string.Empty;

    [JsonPropertyName("mode")]
    public string Mode { get; init; } = string.Empty;

    [JsonPropertyName("metrics")]
    public OptimizationMetrics Metrics { get; init; } = new();
}

public sealed record OptimizationMetrics
{
    [JsonPropertyName("totalJobs")]
    public int TotalJobs { get; init; }

    [JsonPropertyName("assignedJobs")]
    public int AssignedJobs { get; init; }

    [JsonPropertyName("unassignedJobs")]
    public int UnassignedJobs { get; init; }

    [JsonPropertyName("chainsFormed")]
    public int ChainsFormed { get; init; }

    [JsonPropertyName("yardReturnsEliminated")]
    public int YardReturnsEliminated { get; init; }

    [JsonPropertyName("estimatedMinutesSaved")]
    public int EstimatedMinutesSaved { get; init; }

    [JsonPropertyName("strongBondsPreserved")]
    public int StrongBondsPreserved { get; init; }

    /// <summary>
    /// Whether drive times came from the routing engine or were estimated.
    /// <c>"API"</c> or <c>"ESTIMATED"</c>.
    /// </summary>
    [JsonPropertyName("driveTimeSource")]
    public string? DriveTimeSource { get; init; }
}
