using Klau.Sdk.Common;

namespace Klau.Sdk.Integrations;

public interface IIntegrationsClient
{
    Task<AiTokenResult> IssueAiTokenAsync(IssueAiTokenRequest request, CancellationToken ct = default);
    Task<AiUsageReportResult> ReportAiUsageAsync(ReportAiUsageRequest request, CancellationToken ct = default);
}

/// <summary>
/// Integrations API — CLI data-pipeline hooks that let the <c>klau-cli</c>
/// call third-party LLMs (Grok) directly from the customer's machine while
/// Klau keeps per-tenant cost attribution.
///
/// The two operations are paired:
/// <list type="number">
///   <item><see cref="IssueAiTokenAsync"/> mints a short-lived xAI key (15 min TTL,
///   restricted to a single model, TPM/QPM capped).</item>
///   <item><see cref="ReportAiUsageAsync"/> reports token counts back to Klau
///   after every successful inference so spend lands on the right tenant.</item>
/// </list>
///
/// The SDK is a thin wrapper — it does NOT call xAI directly and does NOT
/// persist tokens. Token lifetime and retry-queue semantics are the caller's
/// responsibility.
/// </summary>
public sealed class IntegrationsClient : IIntegrationsClient
{
    private readonly KlauHttpClient _http;
    private readonly string? _tenantId;

    internal IntegrationsClient(KlauHttpClient http, string? tenantId = null)
    {
        _http = http;
        _tenantId = tenantId;
    }

    /// <summary>
    /// Mint a short-lived xAI API key the CLI can use to call Grok inference
    /// directly. The returned <see cref="AiTokenResult.Token"/> is only
    /// usable until <see cref="AiTokenResult.ExpiresAt"/> — the CLI is
    /// expected to re-issue before expiry during long imports.
    ///
    /// Per-tenant daily budget is enforced server-side: once the tenant's
    /// CLI-direct LLM spend meets <c>CLI_AI_DAILY_BUDGET_CENTS</c>, this
    /// method throws a <see cref="KlauApiException"/> with code
    /// <c>FEATURE_NOT_ENABLED</c> (HTTP 403). The CLI should skip extraction
    /// for the rest of the tenant's local calendar day and warn once.
    ///
    /// <code>
    /// var token = await klau.Integrations.IssueAiTokenAsync(new IssueAiTokenRequest
    /// {
    ///     Purpose = AiTokenPurpose.NotesExtraction,
    ///     ModelHint = AiTokenModelHint.Reasoning,
    ///     IssuedForUserLabel = "dispatch@example.com",
    /// });
    /// // Use token.Token + token.BaseUrl + token.Model to call xAI directly.
    /// </code>
    /// </summary>
    /// <param name="request">Token request (purpose is required; modelHint/label optional).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="KlauApiException">
    /// <list type="bullet">
    ///   <item><c>401 UNAUTHORIZED</c> — API key is missing or expired.</item>
    ///   <item><c>403 FEATURE_NOT_ENABLED</c> — tenant has hit the daily LLM spend cap.</item>
    ///   <item><c>503 PROVIDER_UNAVAILABLE</c> — broker not configured on this deployment
    ///   or xAI management API is down. Skip extraction and continue the import.</item>
    /// </list>
    /// </exception>
    public async Task<AiTokenResult> IssueAiTokenAsync(
        IssueAiTokenRequest request,
        CancellationToken ct = default)
    {
        return await _http.PostAsync<AiTokenResult>(
            "api/v1/integrations/ai-token",
            request,
            _tenantId,
            ct);
    }

    /// <summary>
    /// Report token counts back to Klau after a successful Grok inference call.
    /// This is the only signal Klau has for CLI-direct LLM spend (xAI's billing
    /// API is team-level only), so the write is synchronous — the CLI should
    /// persist a retry queue on disk and replay safely on startup.
    ///
    /// The write is idempotent on <see cref="ReportAiUsageRequest.RequestId"/>:
    /// replays return <see cref="AiUsageReportStatus.DUPLICATE_IGNORED"/> with
    /// the original <see cref="AiUsageReportResult.CostCents"/> unchanged. Both
    /// statuses are "success" — the CLI can drop the retry entry either way.
    ///
    /// <code>
    /// // After a successful Grok call. Skip the report if the broker did not
    /// // return a providerKeyId — the spend simply won't be attributed to the
    /// // CLI-direct channel in that case.
    /// if (token.ProviderKeyId is { } providerKeyId)
    /// {
    ///     await klau.Integrations.ReportAiUsageAsync(new ReportAiUsageRequest
    ///     {
    ///         ProviderKeyId = providerKeyId,
    ///         RequestId = Guid.NewGuid().ToString(),
    ///         Model = token.Model,
    ///         InputTokens = response.Usage.InputTokens,
    ///         OutputTokens = response.Usage.OutputTokens,
    ///         Purpose = AiTokenPurpose.NotesExtraction,
    ///         OccurredAt = DateTime.UtcNow,
    ///         DurationMs = (int)stopwatch.ElapsedMilliseconds,
    ///     });
    /// }
    /// </code>
    /// </summary>
    /// <param name="request">Usage report payload. All fields required.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="KlauApiException">
    /// <list type="bullet">
    ///   <item><c>400 INVALID_USAGE</c> — missing <c>requestId</c> or negative token counts.</item>
    ///   <item><c>401 UNAUTHORIZED_KEY</c> — <c>providerKeyId</c> not found or belongs to a different tenant.</item>
    ///   <item><c>503 PERSIST_FAILED</c> — DB write failed. The CLI should re-queue the report.</item>
    /// </list>
    /// </exception>
    public async Task<AiUsageReportResult> ReportAiUsageAsync(
        ReportAiUsageRequest request,
        CancellationToken ct = default)
    {
        return await _http.PostAsync<AiUsageReportResult>(
            "api/v1/integrations/ai-usage",
            request,
            _tenantId,
            ct);
    }
}
