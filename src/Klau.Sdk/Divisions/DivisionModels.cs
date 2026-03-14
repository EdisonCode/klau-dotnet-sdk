using System.Text.Json.Serialization;

namespace Klau.Sdk.Divisions;

public sealed record Division
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("timezone")]
    public string? Timezone { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("subscriptionStatus")]
    public string? SubscriptionStatus { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("userCount")]
    public int UserCount { get; init; }

    [JsonPropertyName("jobCount")]
    public int JobCount { get; init; }

    [JsonPropertyName("driverCount")]
    public int DriverCount { get; init; }

    [JsonPropertyName("truckCount")]
    public int TruckCount { get; init; }
}

public sealed record DivisionDetail
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("timezone")]
    public string? Timezone { get; init; }

    [JsonPropertyName("address")]
    public string? Address { get; init; }

    [JsonPropertyName("city")]
    public string? City { get; init; }

    [JsonPropertyName("state")]
    public string? State { get; init; }

    [JsonPropertyName("zip")]
    public string? Zip { get; init; }

    [JsonPropertyName("phone")]
    public string? Phone { get; init; }

    [JsonPropertyName("subscriptionStatus")]
    public string? SubscriptionStatus { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("userCount")]
    public int UserCount { get; init; }

    [JsonPropertyName("jobCount")]
    public int JobCount { get; init; }

    [JsonPropertyName("driverCount")]
    public int DriverCount { get; init; }

    [JsonPropertyName("truckCount")]
    public int TruckCount { get; init; }

    [JsonPropertyName("customerCount")]
    public int CustomerCount { get; init; }
}

public sealed record CreateDivisionRequest
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("timezone")]
    public string? Timezone { get; init; }
}

public sealed record UpdateDivisionRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("timezone")]
    public string? Timezone { get; init; }
}

public sealed record UsageSummary
{
    [JsonPropertyName("totalJobs")]
    public int TotalJobs { get; init; }

    [JsonPropertyName("totalDrivers")]
    public int TotalDrivers { get; init; }

    [JsonPropertyName("totalTrucks")]
    public int TotalTrucks { get; init; }

    [JsonPropertyName("totalCustomers")]
    public int TotalCustomers { get; init; }

    [JsonPropertyName("divisions")]
    public IReadOnlyList<Division> Divisions { get; init; } = [];
}

public sealed record DivisionUsage
{
    [JsonPropertyName("totalJobs")]
    public int TotalJobs { get; init; }

    [JsonPropertyName("recentJobs")]
    public int RecentJobs { get; init; }

    [JsonPropertyName("completedJobs")]
    public int CompletedJobs { get; init; }

    [JsonPropertyName("drivers")]
    public int Drivers { get; init; }

    [JsonPropertyName("trucks")]
    public int Trucks { get; init; }

    [JsonPropertyName("customers")]
    public int Customers { get; init; }

    [JsonPropertyName("billedJobs")]
    public int BilledJobs { get; init; }
}

public sealed record InviteUserRequest
{
    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("role")]
    public required string Role { get; init; }
}

public sealed record Invitation
{
    [JsonPropertyName("invitationId")]
    public string InvitationId { get; init; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; init; } = string.Empty;

    [JsonPropertyName("divisionName")]
    public string? DivisionName { get; init; }

    [JsonPropertyName("expiresAt")]
    public DateTime ExpiresAt { get; init; }
}
