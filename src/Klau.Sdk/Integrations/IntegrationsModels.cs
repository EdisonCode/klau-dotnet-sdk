using System.Text.Json.Serialization;

namespace Klau.Sdk.Integrations;

// --- AI token broker ---
//
// POST /api/v1/integrations/ai-token mints a short-lived xAI (Grok) API key
// so the CLI can call inference directly from the customer's machine. Raw
// notes and PII never leave the customer's environment — Klau only issues
// the credential and tracks spend.
//
// Tokens are single-model, TPM/QPM capped, and live 15 minutes by default.
// The CLI is expected to re-request a fresh token every quarter hour during
// long imports and report usage back via /ai-usage after every inference.

/// <summary>
/// Hint that maps to a concrete Grok model on the server. Passed in the
/// <see cref="IssueAiTokenRequest"/> body; the broker decides the final
/// model string and returns it on <see cref="AiTokenResult.Model"/>.
/// </summary>
public static class AiTokenModelHint
{
    /// <summary>Reasoning-optimized model (default for notes extraction).</summary>
    public const string Reasoning = "reasoning";
    /// <summary>Latency-optimized model for short prompts.</summary>
    public const string Fast = "fast";
    /// <summary>Server default for the requested purpose.</summary>
    public const string Default = "default";
}

/// <summary>
/// Known purposes accepted by the AI token broker. v1 only accepts
/// <see cref="NotesExtraction"/>.
/// </summary>
public static class AiTokenPurpose
{
    /// <summary>CLI-side notes extraction from source-system free-text fields.</summary>
    public const string NotesExtraction = "notes-extraction";
}

/// <summary>
/// Request body for <see cref="IntegrationsClient.IssueAiTokenAsync"/>.
/// </summary>
public sealed record IssueAiTokenRequest
{
    /// <summary>
    /// What the CLI intends to do with the token. v1 accepts
    /// <see cref="AiTokenPurpose.NotesExtraction"/>. The purpose is persisted
    /// on the audit record and used as the suffix for <c>serviceId</c>
    /// on platform cost entries (e.g. <c>cli-notes-extraction</c>).
    /// </summary>
    [JsonPropertyName("purpose")]
    public required string Purpose { get; init; }

    /// <summary>
    /// Optional model family hint. See <see cref="AiTokenModelHint"/> for
    /// known values. The server maps the hint to a concrete model string
    /// and returns it on <see cref="AiTokenResult.Model"/>.
    /// </summary>
    [JsonPropertyName("modelHint")]
    public string? ModelHint { get; init; }

    /// <summary>
    /// Optional informational label for audit logging (e.g. the CLI
    /// user's email). The CLI can lie — the authoritative holder is the
    /// developer account behind the Klau API key. Do not use this field
    /// for authorization decisions.
    /// </summary>
    [JsonPropertyName("issuedForUserLabel")]
    public string? IssuedForUserLabel { get; init; }
}

/// <summary>
/// Response from <see cref="IntegrationsClient.IssueAiTokenAsync"/>. The raw
/// <see cref="Token"/> is returned <b>exactly once</b> and is never persisted
/// server-side — the CLI must hold it in memory for the lease window and
/// discard it on expiry. Only the <see cref="ProviderKeyId"/> (the xAI
/// apiKeyId) is recorded for usage reporting and reconciliation.
/// </summary>
public sealed record AiTokenResult
{
    /// <summary>Inference provider identifier (always <c>"xai"</c> in v1).</summary>
    [JsonPropertyName("provider")]
    public string Provider { get; init; } = string.Empty;

    /// <summary>Concrete model string the token is restricted to (e.g. <c>grok-4-1-fast-reasoning</c>).</summary>
    [JsonPropertyName("model")]
    public string Model { get; init; } = string.Empty;

    /// <summary>Base URL to POST inference requests to (e.g. <c>https://api.x.ai/v1</c>).</summary>
    [JsonPropertyName("baseUrl")]
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>
    /// The bearer token to pass to the inference API. Returned ONCE — the
    /// server does not store or log this value. Callers must not log, cache
    /// to disk, or ship to error-tracking systems.
    /// </summary>
    [JsonPropertyName("token")]
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// xAI-assigned stable key ID. Pass this as
    /// <see cref="ReportAiUsageRequest.ProviderKeyId"/> when reporting inference
    /// usage back to Klau — it's how the broker links usage to the issued key
    /// for per-tenant cost attribution and nightly reconciliation.
    /// <para>
    /// Nullable because the broker may omit it on older deployments. Callers
    /// should null-check before reporting usage and skip the report if absent
    /// (the spend simply won't be attributed to the CLI-direct channel).
    /// </para>
    /// </summary>
    [JsonPropertyName("providerKeyId")]
    public string? ProviderKeyId { get; init; }

    /// <summary>UTC timestamp when the token stops being accepted by xAI.</summary>
    [JsonPropertyName("expiresAt")]
    public DateTime ExpiresAt { get; init; }

    /// <summary>
    /// TTL in seconds as configured on the broker (default 900 = 15 minutes).
    /// The CLI can compare <c>ttlSeconds</c> against its poll cadence to decide
    /// when to preemptively re-issue before expiry.
    /// </summary>
    [JsonPropertyName("ttlSeconds")]
    public int TtlSeconds { get; init; }
}

// --- AI usage reporting ---

/// <summary>
/// Request body for <see cref="IntegrationsClient.ReportAiUsageAsync"/>.
/// The CLI calls this after every successful Grok inference so Klau can
/// attribute the spend to the right tenant — xAI's billing API is team-level
/// only, so self-report is the only signal Klau has for CLI-direct usage.
/// </summary>
public sealed record ReportAiUsageRequest
{
    /// <summary>
    /// xAI apiKeyId returned by <see cref="AiTokenResult.ProviderKeyId"/>.
    /// The handler verifies the key belongs to the authenticated tenant —
    /// cross-tenant reports return <c>401 UNAUTHORIZED_KEY</c>.
    /// </summary>
    [JsonPropertyName("providerKeyId")]
    public required string ProviderKeyId { get; init; }

    /// <summary>
    /// Client-generated unique ID for idempotency. Replaying the same
    /// <c>requestId</c> returns <c>DUPLICATE_IGNORED</c> without re-charging.
    /// Callers should persist this alongside the CLI's retry queue so
    /// crash-recovery reports don't double-bill.
    /// </summary>
    [JsonPropertyName("requestId")]
    public required string RequestId { get; init; }

    /// <summary>Concrete model string that handled the inference (matches the rate-table key).</summary>
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    /// <summary>Input (prompt) token count as reported by xAI.</summary>
    [JsonPropertyName("inputTokens")]
    public required int InputTokens { get; init; }

    /// <summary>Output (completion) token count as reported by xAI.</summary>
    [JsonPropertyName("outputTokens")]
    public required int OutputTokens { get; init; }

    /// <summary>Purpose suffix used for <c>serviceId</c> (e.g. <c>notes-extraction</c>).</summary>
    [JsonPropertyName("purpose")]
    public required string Purpose { get; init; }

    /// <summary>UTC timestamp when the inference call completed.</summary>
    [JsonPropertyName("occurredAt")]
    public required DateTime OccurredAt { get; init; }

    /// <summary>Wall-clock duration of the inference call, in milliseconds.</summary>
    [JsonPropertyName("durationMs")]
    public required int DurationMs { get; init; }
}

/// <summary>
/// Outcome of <see cref="IntegrationsClient.ReportAiUsageAsync"/>.
/// </summary>
public enum AiUsageReportStatus
{
    /// <summary>First time this <c>requestId</c> was seen — cost row written.</summary>
    RECORDED,
    /// <summary>
    /// <c>requestId</c> was already recorded. The original <c>costCents</c> is
    /// returned unchanged — the CLI can treat this as success.
    /// </summary>
    DUPLICATE_IGNORED
}

/// <summary>
/// Response from <see cref="IntegrationsClient.ReportAiUsageAsync"/>.
/// </summary>
public sealed record AiUsageReportResult
{
    /// <summary>
    /// Whether the report was newly recorded or ignored as a duplicate.
    /// Both outcomes are successful from the CLI's perspective — the retry
    /// queue can safely drop the entry in either case.
    /// </summary>
    [JsonPropertyName("status")]
    public AiUsageReportStatus Status { get; init; }

    /// <summary>
    /// Cost of the reported usage in integer cents, computed server-side from
    /// the shared <c>calculateLlmCostCents()</c> domain function. On
    /// <see cref="AiUsageReportStatus.DUPLICATE_IGNORED"/> this is the original
    /// cost, unchanged.
    /// </summary>
    [JsonPropertyName("costCents")]
    public int CostCents { get; init; }
}
