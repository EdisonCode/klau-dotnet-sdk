using System.Text.Json.Serialization;

namespace Klau.Sdk.Yards;

public sealed record Yard
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("address")]
    public string Address { get; init; } = string.Empty;

    [JsonPropertyName("city")]
    public string? City { get; init; }

    [JsonPropertyName("state")]
    public string? State { get; init; }

    [JsonPropertyName("zip")]
    public string? Zip { get; init; }

    [JsonPropertyName("lat")]
    public double? Lat { get; init; }

    [JsonPropertyName("lng")]
    public double? Lng { get; init; }

    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; init; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; }

    [JsonPropertyName("serviceRadiusMiles")]
    public double? ServiceRadiusMiles { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; init; }
}

public sealed record CreateYardRequest
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("address")]
    public required string Address { get; init; }

    [JsonPropertyName("city")]
    public string? City { get; init; }

    [JsonPropertyName("state")]
    public string? State { get; init; }

    [JsonPropertyName("zip")]
    public string? Zip { get; init; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; init; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; init; }

    /// <summary>Set to true to make this the default yard. Required for dispatch optimization.</summary>
    [JsonPropertyName("isDefault")]
    public bool? IsDefault { get; init; }

    [JsonPropertyName("serviceRadiusMiles")]
    public double? ServiceRadiusMiles { get; init; }
}

public sealed record UpdateYardRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("address")]
    public string? Address { get; init; }

    [JsonPropertyName("city")]
    public string? City { get; init; }

    [JsonPropertyName("state")]
    public string? State { get; init; }

    [JsonPropertyName("zip")]
    public string? Zip { get; init; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; init; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; init; }

    [JsonPropertyName("isDefault")]
    public bool? IsDefault { get; init; }

    [JsonPropertyName("isActive")]
    public bool? IsActive { get; init; }

    [JsonPropertyName("serviceRadiusMiles")]
    public double? ServiceRadiusMiles { get; init; }
}
