using System.Text.Json.Serialization;

namespace Klau.Sdk.Common;

/// <summary>
/// Standard Klau API success response wrapper.
/// </summary>
public sealed class ApiResponse<T>
{
    [JsonPropertyName("data")]
    public T Data { get; set; } = default!;

    [JsonPropertyName("meta")]
    public ResponseMeta? Meta { get; set; }
}

public sealed class ResponseMeta
{
    [JsonPropertyName("total")]
    public int? Total { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int? PageSize { get; set; }

    [JsonPropertyName("hasMore")]
    public bool? HasMore { get; set; }
}
