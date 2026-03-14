using System.Text.Json.Serialization;

namespace Klau.Sdk.Webhooks;

public sealed record WebhookEndpoint
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("events")]
    public IReadOnlyList<string> Events { get; init; } = [];

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; init; } = string.Empty;

    [JsonPropertyName("updatedAt")]
    public string UpdatedAt { get; init; } = string.Empty;
}

public sealed record CreateWebhookRequest
{
    /// <summary>HTTPS URL to receive webhook POSTs.</summary>
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    /// <summary>
    /// Event types to subscribe to. Use <c>"*"</c> for all events.
    /// Common events: <c>job.created</c>, <c>job.assigned</c>, <c>job.completed</c>,
    /// <c>job.status_changed</c>, <c>dispatch.optimized</c>.
    /// </summary>
    [JsonPropertyName("events")]
    public required IReadOnlyList<string> Events { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }
}

public sealed record CreateWebhookResult
{
    [JsonPropertyName("webhookId")]
    public string WebhookId { get; init; } = string.Empty;

    /// <summary>
    /// The signing secret. Starts with <c>whsec_</c>.
    /// Only returned once at creation time — store securely.
    /// </summary>
    [JsonPropertyName("secret")]
    public string Secret { get; init; } = string.Empty;
}

public sealed record WebhookTestResult
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("statusCode")]
    public int? StatusCode { get; init; }

    [JsonPropertyName("responseTime")]
    public int? ResponseTime { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }
}

public sealed record DeveloperSettings
{
    [JsonPropertyName("developerAccountId")]
    public string? DeveloperAccountId { get; init; }

    [JsonPropertyName("apiKeys")]
    public IReadOnlyList<ApiKeyInfo> ApiKeys { get; init; } = [];

    [JsonPropertyName("webhookEndpoints")]
    public IReadOnlyList<WebhookEndpoint> WebhookEndpoints { get; init; } = [];
}

public sealed record ApiKeyInfo
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("prefix")]
    public string Prefix { get; init; } = string.Empty;

    [JsonPropertyName("lastFour")]
    public string LastFour { get; init; } = string.Empty;

    [JsonPropertyName("scopes")]
    public IReadOnlyList<string> Scopes { get; init; } = [];

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; init; } = string.Empty;
}
