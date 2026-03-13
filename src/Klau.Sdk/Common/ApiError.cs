using System.Text.Json.Serialization;

namespace Klau.Sdk.Common;

/// <summary>
/// Standard Klau API error response.
/// </summary>
public sealed class ApiErrorResponse
{
    [JsonPropertyName("error")]
    public ApiError Error { get; set; } = default!;
}

public sealed class ApiError
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("details")]
    public object? Details { get; set; }
}

/// <summary>
/// Thrown when the Klau API returns an error response.
/// </summary>
public class KlauApiException : Exception
{
    public string ErrorCode { get; }
    public int StatusCode { get; }
    public object? Details { get; }

    public KlauApiException(string errorCode, string message, int statusCode, object? details = null)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
        Details = details;
    }
}
