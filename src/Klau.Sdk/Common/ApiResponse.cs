using System.Text.Json.Serialization;

namespace Klau.Sdk.Common;

/// <summary>
/// Standard Klau API success response wrapper.
/// </summary>
public sealed record ApiResponse<T>
{
    [JsonPropertyName("data")]
    public T Data { get; init; } = default!;

    [JsonPropertyName("meta")]
    public ResponseMeta? Meta { get; init; }
}

public sealed record ResponseMeta
{
    [JsonPropertyName("total")]
    public int? Total { get; init; }

    [JsonPropertyName("page")]
    public int? Page { get; init; }

    [JsonPropertyName("pageSize")]
    public int? PageSize { get; init; }

    [JsonPropertyName("hasMore")]
    public bool? HasMore { get; init; }
}
