using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Klau.Sdk.Common;

namespace Klau.Sdk.Webhooks;

/// <summary>
/// Validates incoming Klau webhook requests using HMAC-SHA256 signatures.
///
/// Usage in an ASP.NET Core controller:
/// <code>
/// var validator = new KlauWebhookValidator("whsec_your_secret");
/// var (isValid, evt) = validator.ValidateAndParse(
///     Request.Headers["Klau-Signature"]!,
///     await new StreamReader(Request.Body).ReadToEndAsync());
/// </code>
/// </summary>
public sealed class KlauWebhookValidator
{
    private readonly byte[] _secretBytes;

    /// <summary>
    /// Default tolerance for timestamp validation (5 minutes).
    /// Events older than this are rejected to prevent replay attacks.
    /// </summary>
    public static readonly TimeSpan DefaultTolerance = TimeSpan.FromMinutes(5);

    /// <param name="secret">Your webhook signing secret (starts with <c>whsec_</c>).</param>
    public KlauWebhookValidator(string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        _secretBytes = Encoding.UTF8.GetBytes(secret);
    }

    /// <summary>
    /// Verify the HMAC signature of a webhook request.
    /// </summary>
    /// <param name="signatureHeader">The <c>Klau-Signature</c> header value (e.g. <c>t=1234,v1=abcd...</c>).</param>
    /// <param name="body">The raw request body as a string.</param>
    /// <param name="tolerance">Max age for the timestamp. Defaults to 5 minutes.</param>
    /// <returns><c>true</c> if the signature and timestamp are valid.</returns>
    public bool Validate(string signatureHeader, string body, TimeSpan? tolerance = null)
    {
        if (!TryParseHeader(signatureHeader, out var timestamp, out var signature))
            return false;

        // Check replay window
        var age = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
        if (age.Duration() > (tolerance ?? DefaultTolerance))
            return false;

        var expected = ComputeSignature(timestamp, body);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature));
    }

    /// <summary>
    /// Validate the signature and parse the event in one call.
    /// Throws <see cref="KlauWebhookException"/> on invalid signature.
    /// </summary>
    public WebhookEvent ValidateAndParse(string signatureHeader, string body, TimeSpan? tolerance = null)
    {
        if (!Validate(signatureHeader, body, tolerance))
            throw new KlauWebhookException("Invalid webhook signature or expired timestamp.");

        return JsonSerializer.Deserialize<WebhookEvent>(body, KlauHttpClient.JsonOptions)
            ?? throw new KlauWebhookException("Failed to deserialize webhook event.");
    }

    /// <summary>
    /// Validate the signature and parse into a typed event.
    /// </summary>
    public WebhookEvent<T> ValidateAndParse<T>(string signatureHeader, string body, TimeSpan? tolerance = null)
        where T : class
    {
        if (!Validate(signatureHeader, body, tolerance))
            throw new KlauWebhookException("Invalid webhook signature or expired timestamp.");

        return JsonSerializer.Deserialize<WebhookEvent<T>>(body, KlauHttpClient.JsonOptions)
            ?? throw new KlauWebhookException("Failed to deserialize webhook event.");
    }

    private string ComputeSignature(long timestamp, string body)
    {
        var message = $"{timestamp}.{body}";
        var hash = HMACSHA256.HashData(_secretBytes, Encoding.UTF8.GetBytes(message));
        return Convert.ToHexStringLower(hash);
    }

    private static bool TryParseHeader(string header, out long timestamp, out string signature)
    {
        timestamp = 0;
        signature = string.Empty;

        if (string.IsNullOrWhiteSpace(header)) return false;

        // Format: t=1234567890,v1=hex...
        foreach (var part in header.Split(','))
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith("t=", StringComparison.Ordinal))
            {
                if (!long.TryParse(trimmed.AsSpan(2), out timestamp))
                    return false;
            }
            else if (trimmed.StartsWith("v1=", StringComparison.Ordinal))
            {
                signature = trimmed[3..];
            }
        }

        return timestamp > 0 && signature.Length > 0;
    }
}

/// <summary>
/// Thrown when webhook signature validation fails.
/// </summary>
public sealed class KlauWebhookException : Exception
{
    public KlauWebhookException(string message) : base(message) { }
}
