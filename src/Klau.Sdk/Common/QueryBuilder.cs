using System.Web;

namespace Klau.Sdk.Common;

/// <summary>
/// Builds URL query strings from optional parameters.
/// </summary>
internal static class QueryBuilder
{
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
