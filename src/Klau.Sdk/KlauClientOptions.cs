namespace Klau.Sdk;

/// <summary>
/// Configuration options for <see cref="KlauClient"/>.
/// </summary>
public sealed class KlauClientOptions
{
    /// <summary>
    /// Your Klau API key. Starts with <c>kl_live_</c>.
    /// If not set, falls back to the <c>KLAU_API_KEY</c> environment variable.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Base URL of the Klau API. Override for staging or local development.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.getklau.com";

    /// <summary>
    /// Request timeout in seconds. Default is 30 seconds.
    /// Applies to each individual HTTP request (not including retry delays).
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Webhook signing secret. Starts with <c>whsec_</c>.
    /// If not set, falls back to the <c>KLAU_WEBHOOK_SECRET</c> environment variable.
    /// Only needed if you receive webhooks.
    /// </summary>
    public string? WebhookSecret { get; set; }

    /// <summary>
    /// Resolve the API key from the configured value or environment variable.
    /// Throws if neither is set.
    /// </summary>
    internal string ResolveApiKey()
    {
        var key = ApiKey ?? Environment.GetEnvironmentVariable("KLAU_API_KEY");

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException(
                "Klau API key is required. Set KlauClientOptions.ApiKey, " +
                "the KLAU_API_KEY environment variable, or pass it to the KlauClient constructor. " +
                "Generate a key at Settings > Developer in your Klau dashboard.");
        }

        KlauClient.ValidateApiKey(key);
        return key;
    }

    /// <summary>
    /// Resolve the webhook secret from the configured value or environment variable.
    /// Returns null if neither is set.
    /// </summary>
    internal string? ResolveWebhookSecret()
    {
        return WebhookSecret ?? Environment.GetEnvironmentVariable("KLAU_WEBHOOK_SECRET");
    }
}
