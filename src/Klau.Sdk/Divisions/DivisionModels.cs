using System.Text.Json.Serialization;

namespace Klau.Sdk.Divisions;

public sealed class Division
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("subscriptionStatus")]
    public string? SubscriptionStatus { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("userCount")]
    public int UserCount { get; set; }

    [JsonPropertyName("jobCount")]
    public int JobCount { get; set; }

    [JsonPropertyName("driverCount")]
    public int DriverCount { get; set; }

    [JsonPropertyName("truckCount")]
    public int TruckCount { get; set; }
}

public sealed class DivisionDetail
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("zip")]
    public string? Zip { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("subscriptionStatus")]
    public string? SubscriptionStatus { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("userCount")]
    public int UserCount { get; set; }

    [JsonPropertyName("jobCount")]
    public int JobCount { get; set; }

    [JsonPropertyName("driverCount")]
    public int DriverCount { get; set; }

    [JsonPropertyName("truckCount")]
    public int TruckCount { get; set; }

    [JsonPropertyName("customerCount")]
    public int CustomerCount { get; set; }
}

public sealed class CreateDivisionRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }
}

public sealed class UpdateDivisionRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }
}

public sealed class UsageSummary
{
    [JsonPropertyName("totalJobs")]
    public int TotalJobs { get; set; }

    [JsonPropertyName("totalDrivers")]
    public int TotalDrivers { get; set; }

    [JsonPropertyName("totalTrucks")]
    public int TotalTrucks { get; set; }

    [JsonPropertyName("totalCustomers")]
    public int TotalCustomers { get; set; }

    [JsonPropertyName("divisions")]
    public List<Division> Divisions { get; set; } = [];
}

public sealed class DivisionUsage
{
    [JsonPropertyName("totalJobs")]
    public int TotalJobs { get; set; }

    [JsonPropertyName("recentJobs")]
    public int RecentJobs { get; set; }

    [JsonPropertyName("completedJobs")]
    public int CompletedJobs { get; set; }

    [JsonPropertyName("drivers")]
    public int Drivers { get; set; }

    [JsonPropertyName("trucks")]
    public int Trucks { get; set; }

    [JsonPropertyName("customers")]
    public int Customers { get; set; }

    [JsonPropertyName("billedJobs")]
    public int BilledJobs { get; set; }
}

public sealed class InviteUserRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
}

public sealed class Invitation
{
    [JsonPropertyName("invitationId")]
    public string InvitationId { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("divisionName")]
    public string? DivisionName { get; set; }

    [JsonPropertyName("expiresAt")]
    public DateTime ExpiresAt { get; set; }
}
