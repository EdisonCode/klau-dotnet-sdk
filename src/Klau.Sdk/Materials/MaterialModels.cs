using System.Text.Json.Serialization;
using Klau.Sdk.Common;

namespace Klau.Sdk.Materials;

public sealed record Material
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    [JsonPropertyName("economicDirection")]
    public MaterialEconomicDirection EconomicDirection { get; init; }

    [JsonPropertyName("templateCode")]
    public string? TemplateCode { get; init; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; }

    [JsonPropertyName("isStorefrontVisible")]
    public bool IsStorefrontVisible { get; init; }

    [JsonPropertyName("storefrontSortOrder")]
    public int StorefrontSortOrder { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; init; }
}

public sealed record MaterialTemplate
{
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    [JsonPropertyName("economicDirection")]
    public MaterialEconomicDirection EconomicDirection { get; init; }
}

public sealed record CreateMaterialRequest
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    [JsonPropertyName("economicDirection")]
    public required MaterialEconomicDirection EconomicDirection { get; init; }

    [JsonPropertyName("isStorefrontVisible")]
    public bool? IsStorefrontVisible { get; init; }

    [JsonPropertyName("storefrontSortOrder")]
    public int? StorefrontSortOrder { get; init; }
}

public sealed record UpdateMaterialRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    [JsonPropertyName("economicDirection")]
    public MaterialEconomicDirection? EconomicDirection { get; init; }

    [JsonPropertyName("isStorefrontVisible")]
    public bool? IsStorefrontVisible { get; init; }

    [JsonPropertyName("storefrontSortOrder")]
    public int? StorefrontSortOrder { get; init; }
}
