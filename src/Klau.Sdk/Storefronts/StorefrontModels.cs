using System.Text.Json.Serialization;
using Klau.Sdk.Common;

namespace Klau.Sdk.Storefronts;

public sealed class StorefrontConfig
{
    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("businessName")]
    public string BusinessName { get; set; } = string.Empty;

    [JsonPropertyName("primaryColor")]
    public string? PrimaryColor { get; set; }

    [JsonPropertyName("logoUrl")]
    public string? LogoUrl { get; set; }

    [JsonPropertyName("tagline")]
    public string? Tagline { get; set; }

    [JsonPropertyName("serviceOfferings")]
    public List<ServiceOffering> ServiceOfferings { get; set; } = [];

    [JsonPropertyName("serviceAreaDescription")]
    public string? ServiceAreaDescription { get; set; }
}

public sealed class ServiceOffering
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("containerSize")]
    public int? ContainerSize { get; set; }

    [JsonPropertyName("basePriceCents")]
    public int BasePriceCents { get; set; }

    [JsonPropertyName("rentalPeriodDays")]
    public int? RentalPeriodDays { get; set; }

    [JsonPropertyName("dailyOverageCents")]
    public int? DailyOverageCents { get; set; }

    [JsonPropertyName("materialPricings")]
    public List<MaterialPricing>? MaterialPricings { get; set; }
}

public sealed class MaterialPricing
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("materialId")]
    public string MaterialId { get; set; } = string.Empty;

    [JsonPropertyName("materialName")]
    public string? MaterialName { get; set; }

    [JsonPropertyName("priceCents")]
    public int PriceCents { get; set; }

    [JsonPropertyName("includedWeightLbs")]
    public int? IncludedWeightLbs { get; set; }

    [JsonPropertyName("weightOverageRateCents")]
    public int? WeightOverageRateCents { get; set; }

    [JsonPropertyName("weightOverageUnitLbs")]
    public int? WeightOverageUnitLbs { get; set; }
}

public sealed class Storefront
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("businessName")]
    public string BusinessName { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public StorefrontStatus Status { get; set; }

    [JsonPropertyName("primaryColor")]
    public string? PrimaryColor { get; set; }

    [JsonPropertyName("logoUrl")]
    public string? LogoUrl { get; set; }
}

public sealed class StorefrontOrder
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public OrderStatus Status { get; set; }

    [JsonPropertyName("contactName")]
    public string ContactName { get; set; } = string.Empty;

    [JsonPropertyName("contactEmail")]
    public string? ContactEmail { get; set; }

    [JsonPropertyName("contactPhone")]
    public string? ContactPhone { get; set; }

    [JsonPropertyName("deliveryAddress")]
    public string? DeliveryAddress { get; set; }

    [JsonPropertyName("containerSize")]
    public int? ContainerSize { get; set; }

    [JsonPropertyName("totalCents")]
    public int? TotalCents { get; set; }

    [JsonPropertyName("requestedDeliveryDate")]
    public string? RequestedDeliveryDate { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}

public sealed class SubmitOrderRequest
{
    [JsonPropertyName("serviceOfferingId")]
    public string ServiceOfferingId { get; set; } = string.Empty;

    [JsonPropertyName("materialPricingId")]
    public string? MaterialPricingId { get; set; }

    [JsonPropertyName("contact")]
    public OrderContact Contact { get; set; } = default!;

    [JsonPropertyName("deliveryAddress")]
    public DeliveryAddress DeliveryAddress { get; set; } = default!;

    [JsonPropertyName("requestedDeliveryDate")]
    public string RequestedDeliveryDate { get; set; } = string.Empty;

    [JsonPropertyName("timeWindow")]
    public TimeWindow? TimeWindow { get; set; }

    [JsonPropertyName("stripePaymentIntentId")]
    public string? StripePaymentIntentId { get; set; }

    [JsonPropertyName("smsConsent")]
    public bool? SmsConsent { get; set; }
}

public sealed class OrderContact
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}

public sealed class DeliveryAddress
{
    [JsonPropertyName("street")]
    public string Street { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("zip")]
    public string Zip { get; set; } = string.Empty;

    [JsonPropertyName("lat")]
    public double? Lat { get; set; }

    [JsonPropertyName("lng")]
    public double? Lng { get; set; }

    [JsonPropertyName("accessNotes")]
    public string? AccessNotes { get; set; }
}

public sealed class OrderConfirmation
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public OrderStatus Status { get; set; }

    [JsonPropertyName("trackingUrl")]
    public string? TrackingUrl { get; set; }
}

public sealed class CheckAvailabilityRequest
{
    [JsonPropertyName("serviceOfferingId")]
    public string? ServiceOfferingId { get; set; }

    [JsonPropertyName("zip")]
    public string? Zip { get; set; }
}

public sealed class AvailabilityResult
{
    [JsonPropertyName("availableDates")]
    public List<AvailableDate> AvailableDates { get; set; } = [];
}

public sealed class AvailableDate
{
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("available")]
    public bool Available { get; set; }

    [JsonPropertyName("priceCents")]
    public int? PriceCents { get; set; }

    [JsonPropertyName("discountLabel")]
    public string? DiscountLabel { get; set; }
}

public sealed class SetupStorefrontRequest
{
    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("businessName")]
    public string BusinessName { get; set; } = string.Empty;

    [JsonPropertyName("primaryColor")]
    public string? PrimaryColor { get; set; }

    [JsonPropertyName("logoUrl")]
    public string? LogoUrl { get; set; }

    [JsonPropertyName("tagline")]
    public string? Tagline { get; set; }

    [JsonPropertyName("serviceAreaZipCodes")]
    public List<string>? ServiceAreaZipCodes { get; set; }

    [JsonPropertyName("serviceAreaDescription")]
    public string? ServiceAreaDescription { get; set; }

    [JsonPropertyName("maxDeliveryRadiusMiles")]
    public int? MaxDeliveryRadiusMiles { get; set; }

    [JsonPropertyName("serviceOfferings")]
    public List<SetupServiceOffering>? ServiceOfferings { get; set; }

    [JsonPropertyName("leadTimeDays")]
    public int? LeadTimeDays { get; set; }

    [JsonPropertyName("maxAdvanceBookingDays")]
    public int? MaxAdvanceBookingDays { get; set; }

    [JsonPropertyName("requiresPayment")]
    public bool? RequiresPayment { get; set; }
}

public sealed class SetupServiceOffering
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("containerSize")]
    public int ContainerSize { get; set; }

    [JsonPropertyName("basePriceCents")]
    public int BasePriceCents { get; set; }

    [JsonPropertyName("rentalPeriodDays")]
    public int? RentalPeriodDays { get; set; }

    [JsonPropertyName("dailyOverageCents")]
    public int? DailyOverageCents { get; set; }
}

public sealed class UpdateStorefrontRequest
{
    [JsonPropertyName("businessName")]
    public string? BusinessName { get; set; }

    [JsonPropertyName("primaryColor")]
    public string? PrimaryColor { get; set; }

    [JsonPropertyName("logoUrl")]
    public string? LogoUrl { get; set; }

    [JsonPropertyName("tagline")]
    public string? Tagline { get; set; }

    [JsonPropertyName("serviceAreaDescription")]
    public string? ServiceAreaDescription { get; set; }

    [JsonPropertyName("leadTimeDays")]
    public int? LeadTimeDays { get; set; }

    [JsonPropertyName("maxAdvanceBookingDays")]
    public int? MaxAdvanceBookingDays { get; set; }
}
