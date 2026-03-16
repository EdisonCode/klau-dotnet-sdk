using System.Text.Json.Serialization;

namespace Klau.Sdk.Common;

/// <summary>
/// Response model for API endpoints that return { "success": true }.
/// Used by update and delete endpoints.
/// </summary>
public sealed record SuccessResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }
}

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

/// <summary>
/// Response from a list endpoint where the API returns
/// { data: { [collectionName]: [...], total: N, page: N, ... } }.
/// </summary>
internal sealed record ListResponse<T>(
    List<T> Items,
    int? Total,
    int? Page,
    int? PageSize,
    bool HasMore);
