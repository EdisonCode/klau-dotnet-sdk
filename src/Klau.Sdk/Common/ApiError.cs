using System.Text.Json;
using System.Text.Json.Serialization;

namespace Klau.Sdk.Common;

/// <summary>
/// Standard Klau API error response.
/// </summary>
public sealed record ApiErrorResponse
{
    [JsonPropertyName("error")]
    public ApiError Error { get; init; } = default!;
}

public sealed record ApiError
{
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("details")]
    public object? Details { get; init; }
}

/// <summary>
/// A single field-level validation error from the API.
/// </summary>
public sealed record ValidationDetail
{
    /// <summary>The field that failed validation (e.g. "containerSize", "siteAddress").</summary>
    public string Field { get; init; } = string.Empty;

    /// <summary>Human-readable description of the validation failure.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Machine-readable constraint code (e.g. "REQUIRED", "INVALID_FORMAT").</summary>
    public string? Constraint { get; init; }
}

/// <summary>
/// Thrown when the Klau API returns an error response.
/// Use convenience properties (<see cref="IsRateLimit"/>, <see cref="IsNotFound"/>, etc.)
/// for programmatic error routing in integration code.
/// </summary>
public class KlauApiException : Exception
{
    /// <summary>Machine-readable error code from the API (e.g. "VALIDATION_ERROR", "NOT_FOUND").</summary>
    public string ErrorCode { get; }

    /// <summary>HTTP status code of the failed response.</summary>
    public int StatusCode { get; }

    /// <summary>Raw error details from the API response. May be a JsonElement, list, or null.</summary>
    public object? Details { get; }

    /// <summary>
    /// Typed validation errors extracted from <see cref="Details"/>.
    /// Non-empty when <see cref="IsValidation"/> is true.
    /// </summary>
    public IReadOnlyList<ValidationDetail> ValidationErrors { get; }

    /// <summary>
    /// Retry-After duration from the API response. Non-null when <see cref="IsRateLimit"/> is true.
    /// Use this to wait before retrying: <c>await Task.Delay(ex.RetryAfter.Value)</c>.
    /// </summary>
    public TimeSpan? RetryAfter { get; init; }

    // ── Convenience booleans for programmatic routing ──

    /// <summary>True when the API returned 429 Too Many Requests.</summary>
    public bool IsRateLimit => StatusCode == 429;

    /// <summary>True when the resource was not found (404).</summary>
    public bool IsNotFound => StatusCode == 404;

    /// <summary>True when the request failed field-level validation (400 + VALIDATION_ERROR).</summary>
    public bool IsValidation => StatusCode == 400 &&
        ErrorCode.Equals("VALIDATION_ERROR", StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the API key is missing or invalid (401).</summary>
    public bool IsUnauthorized => StatusCode == 401;

    /// <summary>True when the API key lacks the required scope (403).</summary>
    public bool IsInsufficientScope => StatusCode == 403;

    /// <summary>True when a concurrent update conflict occurred (409).</summary>
    public bool IsConflict => StatusCode == 409;

    public KlauApiException(string errorCode, string message, int statusCode, object? details = null)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
        Details = details;
        ValidationErrors = ExtractValidationDetails(details);
    }

    private static IReadOnlyList<ValidationDetail> ExtractValidationDetails(object? details)
    {
        if (details is not JsonElement element)
            return [];

        var results = new List<ValidationDetail>();

        // Array of { field, message, constraint? }
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var field = item.TryGetProperty("field", out var f) ? f.GetString() : null;
                var msg = item.TryGetProperty("message", out var m) ? m.GetString() : null;
                var constraint = item.TryGetProperty("constraint", out var c) ? c.GetString() : null;

                if (field is not null || msg is not null)
                {
                    results.Add(new ValidationDetail
                    {
                        Field = field ?? "",
                        Message = msg ?? "",
                        Constraint = constraint
                    });
                }
            }
        }

        return results;
    }
}
