using System.Text.Json.Serialization;
using Klau.Sdk.Common;

namespace Klau.Sdk.Storefronts;

public sealed record StorefrontConfig
{
    [JsonPropertyName("slug")]
    public string Slug { get; init; } = string.Empty;

    [JsonPropertyName("businessName")]
    public string BusinessName { get; init; } = string.Empty;

    [JsonPropertyName("primaryColor")]
    public string? PrimaryColor { get; init; }

    [JsonPropertyName("logoUrl")]
    public string? LogoUrl { get; init; }

    [JsonPropertyName("tagline")]
    public string? Tagline { get; init; }

    [JsonPropertyName("serviceOfferings")]
    public IReadOnlyList<ServiceOffering> ServiceOfferings { get; init; } = [];

    [JsonPropertyName("serviceAreaDescription")]
    public string? ServiceAreaDescription { get; init; }
}

public sealed record ServiceOffering
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("containerSize")]
    public int? ContainerSize { get; init; }

    [JsonPropertyName("basePriceCents")]
    public int BasePriceCents { get; init; }

    [JsonPropertyName("rentalPeriodDays")]
    public int? RentalPeriodDays { get; init; }

    [JsonPropertyName("dailyOverageCents")]
    public int? DailyOverageCents { get; init; }

    [JsonPropertyName("materialPricings")]
    public IReadOnlyList<MaterialPricing>? MaterialPricings { get; init; }
}

public sealed record MaterialPricing
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("materialId")]
    public string MaterialId { get; init; } = string.Empty;

    [JsonPropertyName("materialName")]
    public string? MaterialName { get; init; }

    [JsonPropertyName("priceCents")]
    public int PriceCents { get; init; }

    [JsonPropertyName("includedWeightLbs")]
    public int? IncludedWeightLbs { get; init; }

    [JsonPropertyName("weightOverageRateCents")]
    public int? WeightOverageRateCents { get; init; }

    [JsonPropertyName("weightOverageUnitLbs")]
    public int? WeightOverageUnitLbs { get; init; }
}

public sealed record Storefront
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; init; } = string.Empty;

    [JsonPropertyName("businessName")]
    public string BusinessName { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public StorefrontStatus Status { get; init; }

    [JsonPropertyName("primaryColor")]
    public string? PrimaryColor { get; init; }

    [JsonPropertyName("logoUrl")]
    public string? LogoUrl { get; init; }
}

public sealed record StorefrontOrder
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public OrderStatus Status { get; init; }

    [JsonPropertyName("contactName")]
    public string ContactName { get; init; } = string.Empty;

    [JsonPropertyName("contactEmail")]
    public string? ContactEmail { get; init; }

    [JsonPropertyName("contactPhone")]
    public string? ContactPhone { get; init; }

    [JsonPropertyName("deliveryAddress")]
    public string? DeliveryAddress { get; init; }

    [JsonPropertyName("containerSize")]
    public int? ContainerSize { get; init; }

    [JsonPropertyName("totalCents")]
    public int? TotalCents { get; init; }

    [JsonPropertyName("requestedDeliveryDate")]
    public string? RequestedDeliveryDate { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }
}

public sealed record SubmitOrderRequest
{
    [JsonPropertyName("serviceOfferingId")]
    public required string ServiceOfferingId { get; init; }

    [JsonPropertyName("materialPricingId")]
    public string? MaterialPricingId { get; init; }

    [JsonPropertyName("contact")]
    public required OrderContact Contact { get; init; }

    [JsonPropertyName("deliveryAddress")]
    public required DeliveryAddress DeliveryAddress { get; init; }

    [JsonPropertyName("requestedDeliveryDate")]
    public required string RequestedDeliveryDate { get; init; }

    [JsonPropertyName("timeWindow")]
    public TimeWindow? TimeWindow { get; init; }

    [JsonPropertyName("stripePaymentIntentId")]
    public string? StripePaymentIntentId { get; init; }

    [JsonPropertyName("smsConsent")]
    public bool? SmsConsent { get; init; }
}

public sealed record OrderContact
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("phone")]
    public required string Phone { get; init; }

    [JsonPropertyName("email")]
    public required string Email { get; init; }
}

public sealed record DeliveryAddress
{
    [JsonPropertyName("street")]
    public required string Street { get; init; }

    [JsonPropertyName("city")]
    public required string City { get; init; }

    [JsonPropertyName("state")]
    public required string State { get; init; }

    [JsonPropertyName("zip")]
    public required string Zip { get; init; }

    [JsonPropertyName("lat")]
    public double? Lat { get; init; }

    [JsonPropertyName("lng")]
    public double? Lng { get; init; }

    [JsonPropertyName("accessNotes")]
    public string? AccessNotes { get; init; }
}

public sealed record OrderConfirmation
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public OrderStatus Status { get; init; }

    [JsonPropertyName("trackingUrl")]
    public string? TrackingUrl { get; init; }
}

public sealed record CheckAvailabilityRequest
{
    [JsonPropertyName("serviceOfferingId")]
    public string? ServiceOfferingId { get; init; }

    [JsonPropertyName("zip")]
    public string? Zip { get; init; }
}

public sealed record AvailabilityResult
{
    [JsonPropertyName("availableDates")]
    public IReadOnlyList<AvailableDate> AvailableDates { get; init; } = [];
}

public sealed record AvailableDate
{
    [JsonPropertyName("date")]
    public string Date { get; init; } = string.Empty;

    [JsonPropertyName("available")]
    public bool Available { get; init; }

    [JsonPropertyName("priceCents")]
    public int? PriceCents { get; init; }

    [JsonPropertyName("discountLabel")]
    public string? DiscountLabel { get; init; }
}

public sealed record SetupStorefrontRequest
{
    [JsonPropertyName("slug")]
    public required string Slug { get; init; }

    [JsonPropertyName("businessName")]
    public required string BusinessName { get; init; }

    [JsonPropertyName("primaryColor")]
    public string? PrimaryColor { get; init; }

    [JsonPropertyName("logoUrl")]
    public string? LogoUrl { get; init; }

    [JsonPropertyName("tagline")]
    public string? Tagline { get; init; }

    [JsonPropertyName("serviceAreaZipCodes")]
    public IReadOnlyList<string>? ServiceAreaZipCodes { get; init; }

    [JsonPropertyName("serviceAreaDescription")]
    public string? ServiceAreaDescription { get; init; }

    [JsonPropertyName("maxDeliveryRadiusMiles")]
    public int? MaxDeliveryRadiusMiles { get; init; }

    [JsonPropertyName("serviceOfferings")]
    public IReadOnlyList<SetupServiceOffering>? ServiceOfferings { get; init; }

    [JsonPropertyName("leadTimeDays")]
    public int? LeadTimeDays { get; init; }

    [JsonPropertyName("maxAdvanceBookingDays")]
    public int? MaxAdvanceBookingDays { get; init; }

    [JsonPropertyName("requiresPayment")]
    public bool? RequiresPayment { get; init; }
}

public sealed record SetupServiceOffering
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("containerSize")]
    public required int ContainerSize { get; init; }

    [JsonPropertyName("basePriceCents")]
    public required int BasePriceCents { get; init; }

    [JsonPropertyName("rentalPeriodDays")]
    public int? RentalPeriodDays { get; init; }

    [JsonPropertyName("dailyOverageCents")]
    public int? DailyOverageCents { get; init; }
}

public sealed record UpdateStorefrontRequest
{
    [JsonPropertyName("businessName")]
    public string? BusinessName { get; init; }

    [JsonPropertyName("primaryColor")]
    public string? PrimaryColor { get; init; }

    [JsonPropertyName("logoUrl")]
    public string? LogoUrl { get; init; }

    [JsonPropertyName("tagline")]
    public string? Tagline { get; init; }

    [JsonPropertyName("serviceAreaDescription")]
    public string? ServiceAreaDescription { get; init; }

    [JsonPropertyName("leadTimeDays")]
    public int? LeadTimeDays { get; init; }

    [JsonPropertyName("maxAdvanceBookingDays")]
    public int? MaxAdvanceBookingDays { get; init; }
}
