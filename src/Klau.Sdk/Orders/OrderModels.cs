using System.Text.Json.Serialization;
using Klau.Sdk.Common;

namespace Klau.Sdk.Orders;

public sealed record OrderTracking
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public OrderStatus Status { get; init; }

    [JsonPropertyName("containerSize")]
    public int? ContainerSize { get; init; }

    [JsonPropertyName("deliveryAddress")]
    public string? DeliveryAddress { get; init; }

    [JsonPropertyName("requestedDeliveryDate")]
    public string? RequestedDeliveryDate { get; init; }

    [JsonPropertyName("businessName")]
    public string? BusinessName { get; init; }
}

public sealed record CustomerOrder
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public OrderStatus Status { get; init; }

    [JsonPropertyName("containerSize")]
    public int? ContainerSize { get; init; }

    [JsonPropertyName("deliveryAddress")]
    public string? DeliveryAddress { get; init; }

    [JsonPropertyName("totalCents")]
    public int? TotalCents { get; init; }

    [JsonPropertyName("requestedDeliveryDate")]
    public string? RequestedDeliveryDate { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }
}

public sealed record PendingSettlement
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; init; } = string.Empty;

    [JsonPropertyName("contactName")]
    public string ContactName { get; init; } = string.Empty;

    [JsonPropertyName("containerSize")]
    public int? ContainerSize { get; init; }

    [JsonPropertyName("basePriceCents")]
    public int? BasePriceCents { get; init; }

    [JsonPropertyName("actualWeightLbs")]
    public int? ActualWeightLbs { get; init; }
}

public sealed record SettleRequest
{
    [JsonPropertyName("weightOverrideLbs")]
    public int? WeightOverrideLbs { get; init; }
}

public sealed record SettlementResult
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; init; } = string.Empty;

    [JsonPropertyName("finalTotalCents")]
    public int FinalTotalCents { get; init; }

    [JsonPropertyName("weightOverageCents")]
    public int? WeightOverageCents { get; init; }

    [JsonPropertyName("rentalOverageCents")]
    public int? RentalOverageCents { get; init; }

    [JsonPropertyName("feesTotalCents")]
    public int? FeesTotalCents { get; init; }

    [JsonPropertyName("redemptionCreditCents")]
    public int? RedemptionCreditCents { get; init; }

    [JsonPropertyName("settledAt")]
    public DateTime SettledAt { get; init; }
}
