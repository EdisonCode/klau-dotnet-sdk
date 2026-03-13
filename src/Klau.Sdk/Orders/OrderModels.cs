using System.Text.Json.Serialization;
using Klau.Sdk.Common;

namespace Klau.Sdk.Orders;

public sealed class OrderTracking
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public OrderStatus Status { get; set; }

    [JsonPropertyName("containerSize")]
    public int? ContainerSize { get; set; }

    [JsonPropertyName("deliveryAddress")]
    public string? DeliveryAddress { get; set; }

    [JsonPropertyName("requestedDeliveryDate")]
    public string? RequestedDeliveryDate { get; set; }

    [JsonPropertyName("businessName")]
    public string? BusinessName { get; set; }
}

public sealed class CustomerOrder
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public OrderStatus Status { get; set; }

    [JsonPropertyName("containerSize")]
    public int? ContainerSize { get; set; }

    [JsonPropertyName("deliveryAddress")]
    public string? DeliveryAddress { get; set; }

    [JsonPropertyName("totalCents")]
    public int? TotalCents { get; set; }

    [JsonPropertyName("requestedDeliveryDate")]
    public string? RequestedDeliveryDate { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}

public sealed class PendingSettlement
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("contactName")]
    public string ContactName { get; set; } = string.Empty;

    [JsonPropertyName("containerSize")]
    public int? ContainerSize { get; set; }

    [JsonPropertyName("basePriceCents")]
    public int? BasePriceCents { get; set; }

    [JsonPropertyName("actualWeightLbs")]
    public int? ActualWeightLbs { get; set; }
}

public sealed class SettleRequest
{
    [JsonPropertyName("weightOverrideLbs")]
    public int? WeightOverrideLbs { get; set; }
}

public sealed class SettlementResult
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("finalTotalCents")]
    public int FinalTotalCents { get; set; }

    [JsonPropertyName("weightOverageCents")]
    public int? WeightOverageCents { get; set; }

    [JsonPropertyName("rentalOverageCents")]
    public int? RentalOverageCents { get; set; }

    [JsonPropertyName("feesTotalCents")]
    public int? FeesTotalCents { get; set; }

    [JsonPropertyName("redemptionCreditCents")]
    public int? RedemptionCreditCents { get; set; }

    [JsonPropertyName("settledAt")]
    public DateTime SettledAt { get; set; }
}
