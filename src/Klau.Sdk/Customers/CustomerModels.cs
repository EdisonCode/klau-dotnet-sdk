using System.Text.Json.Serialization;

namespace Klau.Sdk.Customers;

public sealed record Customer
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("externalId")]
    public string? ExternalId { get; init; }

    [JsonPropertyName("contactName")]
    public string? ContactName { get; init; }

    [JsonPropertyName("contactPhone")]
    public string? ContactPhone { get; init; }

    [JsonPropertyName("contactEmail")]
    public string? ContactEmail { get; init; }

    [JsonPropertyName("billingAddress")]
    public string? BillingAddress { get; init; }

    [JsonPropertyName("notes")]
    public string? Notes { get; init; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; init; }
}

public sealed record Customer360
{
    [JsonPropertyName("customer")]
    public Customer Customer { get; init; } = default!;

    [JsonPropertyName("healthScore")]
    public int? HealthScore { get; init; }

    [JsonPropertyName("lifecycleStage")]
    public string? LifecycleStage { get; init; }

    [JsonPropertyName("totalOrders")]
    public int? TotalOrders { get; init; }

    [JsonPropertyName("totalRevenueCents")]
    public long? TotalRevenueCents { get; init; }
}

public sealed record Site
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; init; }

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

    [JsonPropertyName("accessNotes")]
    public string? AccessNotes { get; init; }

    [JsonPropertyName("externalId")]
    public string? ExternalId { get; init; }

    [JsonPropertyName("customerId")]
    public string CustomerId { get; init; } = string.Empty;
}

public sealed record CreateCustomerRequest
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("contactName")]
    public string? ContactName { get; init; }

    [JsonPropertyName("contactPhone")]
    public string? ContactPhone { get; init; }

    [JsonPropertyName("contactEmail")]
    public string? ContactEmail { get; init; }

    [JsonPropertyName("billingAddress")]
    public string? BillingAddress { get; init; }

    [JsonPropertyName("notes")]
    public string? Notes { get; init; }
}

public sealed record UpdateCustomerRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("contactName")]
    public string? ContactName { get; init; }

    [JsonPropertyName("contactPhone")]
    public string? ContactPhone { get; init; }

    [JsonPropertyName("contactEmail")]
    public string? ContactEmail { get; init; }

    [JsonPropertyName("billingAddress")]
    public string? BillingAddress { get; init; }

    [JsonPropertyName("notes")]
    public string? Notes { get; init; }
}
