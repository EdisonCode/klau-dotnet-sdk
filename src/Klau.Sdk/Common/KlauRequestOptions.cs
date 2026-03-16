namespace Klau.Sdk.Common;

/// <summary>
/// Per-request options for Klau API calls.
/// Pass to mutation methods like <c>CreateAsync</c> to set idempotency keys,
/// per-request timeouts, or tenant overrides.
///
/// <example>
/// <code>
/// await klau.Jobs.CreateAsync(request, new KlauRequestOptions
/// {
///     IdempotencyKey = $"erp-order-{order.ExternalId}",
///     Timeout = TimeSpan.FromSeconds(60),
/// });
/// </code>
/// </example>
/// </summary>
public sealed record KlauRequestOptions
{
    /// <summary>
    /// Idempotency key for safe retries. When set, the API guarantees that
    /// repeated requests with the same key produce the same result without
    /// creating duplicate resources. Keys are scoped to your API key and
    /// expire after 24 hours.
    ///
    /// Recommended for any operation triggered by an external queue or webhook
    /// where the message might be delivered more than once.
    /// </summary>
    public string? IdempotencyKey { get; init; }

    /// <summary>
    /// Per-request timeout override. When set, this request uses its own
    /// cancellation deadline instead of the client-level default (30s).
    /// Useful for bulk imports or optimization calls that take longer.
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Per-request tenant override. When set, this single request targets
    /// the specified tenant without mutating the client or requiring a
    /// <c>ForTenant</c> scope.
    /// </summary>
    public string? TenantId { get; init; }
}
