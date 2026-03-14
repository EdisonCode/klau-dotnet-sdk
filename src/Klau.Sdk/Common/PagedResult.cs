namespace Klau.Sdk.Common;

/// <summary>
/// A page of results from a list endpoint.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int? Total,
    int? Page,
    int? PageSize,
    bool HasMore)
{
    public PagedResult(IReadOnlyList<T> items, ResponseMeta? meta)
        : this(
            items,
            meta?.Total,
            meta?.Page,
            meta?.PageSize,
            meta?.HasMore ?? false)
    {
    }
}
