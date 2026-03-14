using System.Text.Json.Serialization;

namespace Klau.Sdk.DumpSites;

public sealed record DumpSite
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

    [JsonPropertyName("openTime")]
    public string? OpenTime { get; init; }

    [JsonPropertyName("closeTime")]
    public string? CloseTime { get; init; }

    [JsonPropertyName("avgWaitMinutes")]
    public int? AvgWaitMinutes { get; init; }

    [JsonPropertyName("acceptedSizes")]
    public IReadOnlyList<int> AcceptedSizes { get; init; } = [];

    [JsonPropertyName("materialPricing")]
    public IReadOnlyList<MaterialPricing> MaterialPricing { get; init; } = [];

    [JsonPropertyName("siteType")]
    public string? SiteType { get; init; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; }

    [JsonPropertyName("notes")]
    public string? Notes { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; init; }
}

public sealed record MaterialPricing
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("dumpSiteId")]
    public string DumpSiteId { get; init; } = string.Empty;

    [JsonPropertyName("materialId")]
    public string MaterialId { get; init; } = string.Empty;

    [JsonPropertyName("pricePerUnitCents")]
    public int PricePerUnitCents { get; init; }

    [JsonPropertyName("unit")]
    public string Unit { get; init; } = string.Empty;

    [JsonPropertyName("effectiveFrom")]
    public string? EffectiveFrom { get; init; }

    [JsonPropertyName("effectiveUntil")]
    public string? EffectiveUntil { get; init; }
}

public sealed record CreateDumpSiteRequest
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

    [JsonPropertyName("openTime")]
    public string? OpenTime { get; init; }

    [JsonPropertyName("closeTime")]
    public string? CloseTime { get; init; }

    [JsonPropertyName("avgWaitMinutes")]
    public int? AvgWaitMinutes { get; init; }

    [JsonPropertyName("acceptedSizes")]
    public IReadOnlyList<int>? AcceptedSizes { get; init; }

    [JsonPropertyName("siteType")]
    public string? SiteType { get; init; }

    [JsonPropertyName("notes")]
    public string? Notes { get; init; }
}

public sealed record UpdateDumpSiteRequest
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

    [JsonPropertyName("openTime")]
    public string? OpenTime { get; init; }

    [JsonPropertyName("closeTime")]
    public string? CloseTime { get; init; }

    [JsonPropertyName("avgWaitMinutes")]
    public int? AvgWaitMinutes { get; init; }

    [JsonPropertyName("acceptedSizes")]
    public IReadOnlyList<int>? AcceptedSizes { get; init; }

    [JsonPropertyName("siteType")]
    public string? SiteType { get; init; }

    [JsonPropertyName("isActive")]
    public bool? IsActive { get; init; }

    [JsonPropertyName("notes")]
    public string? Notes { get; init; }
}

public sealed record AddMaterialPricingRequest
{
    [JsonPropertyName("materialId")]
    public required string MaterialId { get; init; }

    [JsonPropertyName("pricePerUnitCents")]
    public required int PricePerUnitCents { get; init; }

    /// <summary>Weight unit: <c>TON</c> or <c>LB</c>.</summary>
    [JsonPropertyName("unit")]
    public required string Unit { get; init; }

    [JsonPropertyName("effectiveFrom")]
    public string? EffectiveFrom { get; init; }

    [JsonPropertyName("effectiveUntil")]
    public string? EffectiveUntil { get; init; }
}

public sealed record UpdateMaterialPricingRequest
{
    [JsonPropertyName("pricePerUnitCents")]
    public int? PricePerUnitCents { get; init; }

    [JsonPropertyName("unit")]
    public string? Unit { get; init; }

    [JsonPropertyName("effectiveUntil")]
    public string? EffectiveUntil { get; init; }
}
