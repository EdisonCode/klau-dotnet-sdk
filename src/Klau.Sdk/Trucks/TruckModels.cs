using System.Text.Json.Serialization;

namespace Klau.Sdk.Trucks;

public sealed record Truck
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("number")]
    public string Number { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("licensePlate")]
    public string? LicensePlate { get; init; }

    [JsonPropertyName("compatibleSizes")]
    public IReadOnlyList<int> CompatibleSizes { get; init; } = [];

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("homeYardId")]
    public string? HomeYardId { get; init; }

    [JsonPropertyName("maxContainers")]
    public int? MaxContainers { get; init; }

    [JsonPropertyName("preferredDumpSiteId")]
    public string? PreferredDumpSiteId { get; init; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; init; }
}

public sealed record CreateTruckRequest
{
    [JsonPropertyName("number")]
    public required string Number { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("licensePlate")]
    public string? LicensePlate { get; init; }

    /// <summary>Container sizes this truck can carry (e.g. [20, 30, 40]).</summary>
    [JsonPropertyName("compatibleSizes")]
    public IReadOnlyList<int>? CompatibleSizes { get; init; }

    [JsonPropertyName("homeYardId")]
    public string? HomeYardId { get; init; }

    [JsonPropertyName("maxContainers")]
    public int? MaxContainers { get; init; }

    [JsonPropertyName("preferredDumpSiteId")]
    public string? PreferredDumpSiteId { get; init; }
}

public sealed record UpdateTruckRequest
{
    [JsonPropertyName("number")]
    public string? Number { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("licensePlate")]
    public string? LicensePlate { get; init; }

    [JsonPropertyName("compatibleSizes")]
    public IReadOnlyList<int>? CompatibleSizes { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("homeYardId")]
    public string? HomeYardId { get; init; }

    [JsonPropertyName("maxContainers")]
    public int? MaxContainers { get; init; }

    [JsonPropertyName("preferredDumpSiteId")]
    public string? PreferredDumpSiteId { get; init; }
}
