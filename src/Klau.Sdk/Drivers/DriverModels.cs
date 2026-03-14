using System.Text.Json.Serialization;

namespace Klau.Sdk.Drivers;

public sealed record Driver
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("phone")]
    public string? Phone { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("color")]
    public string? Color { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("driverType")]
    public string? DriverType { get; init; }

    [JsonPropertyName("defaultTruckId")]
    public string? DefaultTruckId { get; init; }

    [JsonPropertyName("defaultTruckNumber")]
    public string? DefaultTruckNumber { get; init; }

    [JsonPropertyName("homeYardId")]
    public string? HomeYardId { get; init; }

    [JsonPropertyName("homeYardName")]
    public string? HomeYardName { get; init; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; init; }
}

public sealed record CreateDriverRequest
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("phone")]
    public string? Phone { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("driverType")]
    public string? DriverType { get; init; }

    /// <summary>Link this driver to a truck for dispatch optimization.</summary>
    [JsonPropertyName("defaultTruckId")]
    public string? DefaultTruckId { get; init; }

    /// <summary>Assign the driver's home yard (starting point for routes).</summary>
    [JsonPropertyName("homeYardId")]
    public string? HomeYardId { get; init; }
}

public sealed record UpdateDriverRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("phone")]
    public string? Phone { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("driverType")]
    public string? DriverType { get; init; }

    [JsonPropertyName("defaultTruckId")]
    public string? DefaultTruckId { get; init; }

    [JsonPropertyName("homeYardId")]
    public string? HomeYardId { get; init; }

    [JsonPropertyName("isActive")]
    public bool? IsActive { get; init; }
}
