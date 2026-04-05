using System.Web;

namespace Klau.Sdk.Common;

/// <summary>
/// Builds URL query strings and encodes path segments.
/// </summary>
internal static class QueryBuilder
{
    /// <summary>
    /// Percent-encode a value for safe use in a URL path segment.
    /// Prevents path traversal when interpolating caller-supplied IDs or slugs.
    /// </summary>
    public static string PathEncode(string value) => Uri.EscapeDataString(value);

    public static string Build(string path, params (string key, object? value)[] parameters)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);

        foreach (var (key, value) in parameters)
        {
            if (value is null) continue;
            query[key] = value.ToString();
        }

        var qs = query.ToString();
        return string.IsNullOrEmpty(qs) ? path : $"{path}?{qs}";
    }
}
