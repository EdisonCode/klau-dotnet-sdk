namespace Klau.Sdk.Common;

/// <summary>
/// A page of results from a list endpoint.
/// </summary>
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int? Total { get; }
    public int? Page { get; }
    public int? PageSize { get; }
    public bool HasMore { get; }

    public PagedResult(IReadOnlyList<T> items, ResponseMeta? meta)
    {
        Items = items;
        Total = meta?.Total;
        Page = meta?.Page;
        PageSize = meta?.PageSize;
        HasMore = meta?.HasMore ?? false;
    }
}
