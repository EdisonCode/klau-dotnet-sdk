using System.Text.Json.Serialization;

namespace Klau.Sdk.Readiness;

/// <summary>
/// Dispatch readiness report for a tenant. Identifies missing configuration
/// that will prevent dispatch optimization from working correctly.
/// </summary>
public sealed record ReadinessReport
{
    [JsonPropertyName("canGoLive")]
    public bool CanGoLive { get; init; }

    [JsonPropertyName("readyPercentage")]
    public int ReadyPercentage { get; init; }

    [JsonPropertyName("sections")]
    public IReadOnlyList<ReadinessSection> Sections { get; init; } = [];
}

public sealed record ReadinessSection
{
    [JsonPropertyName("key")]
    public string Key { get; init; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    [JsonPropertyName("items")]
    public IReadOnlyList<ReadinessItem> Items { get; init; } = [];
}

/// <summary>
/// A single readiness check item. When <see cref="Status"/> is not <c>"complete"</c>,
/// the <see cref="Detail"/> field explains what's missing and <see cref="Route"/>
/// points to the Klau dashboard page where it can be fixed.
/// </summary>
public sealed record ReadinessItem
{
    [JsonPropertyName("key")]
    public string Key { get; init; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    /// <summary><c>complete</c>, <c>incomplete</c>, or <c>in_progress</c>.</summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    /// <summary>Current count for the resource (e.g. number of drivers).</summary>
    [JsonPropertyName("count")]
    public int? Count { get; init; }

    /// <summary>Human-readable explanation or remediation guidance.</summary>
    [JsonPropertyName("detail")]
    public string? Detail { get; init; }

    /// <summary>Dashboard route where this can be fixed (e.g. <c>/drivers</c>).</summary>
    [JsonPropertyName("route")]
    public string? Route { get; init; }

    /// <summary>Whether this item is required for dispatch optimization.</summary>
    [JsonPropertyName("required")]
    public bool Required { get; init; }

    public bool IsComplete => Status == "complete";
    public bool IsIncomplete => Status is "incomplete" or "in_progress";
}
