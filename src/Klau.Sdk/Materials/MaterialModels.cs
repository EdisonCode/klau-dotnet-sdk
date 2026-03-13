using System.Text.Json.Serialization;
using Klau.Sdk.Common;

namespace Klau.Sdk.Materials;

public sealed class Material
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("economicDirection")]
    public MaterialEconomicDirection EconomicDirection { get; set; }

    [JsonPropertyName("templateCode")]
    public string? TemplateCode { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("isStorefrontVisible")]
    public bool IsStorefrontVisible { get; set; }

    [JsonPropertyName("storefrontSortOrder")]
    public int StorefrontSortOrder { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

public sealed class MaterialTemplate
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("economicDirection")]
    public MaterialEconomicDirection EconomicDirection { get; set; }
}

public sealed class CreateMaterialRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("economicDirection")]
    public MaterialEconomicDirection EconomicDirection { get; set; }

    [JsonPropertyName("isStorefrontVisible")]
    public bool? IsStorefrontVisible { get; set; }

    [JsonPropertyName("storefrontSortOrder")]
    public int? StorefrontSortOrder { get; set; }
}

public sealed class UpdateMaterialRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("economicDirection")]
    public MaterialEconomicDirection? EconomicDirection { get; set; }

    [JsonPropertyName("isStorefrontVisible")]
    public bool? IsStorefrontVisible { get; set; }

    [JsonPropertyName("storefrontSortOrder")]
    public int? StorefrontSortOrder { get; set; }
}
