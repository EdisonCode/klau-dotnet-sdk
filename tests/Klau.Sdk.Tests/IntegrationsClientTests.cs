using System.Net;
using System.Text.Json;
using Klau.Sdk.Common;
using Klau.Sdk.Integrations;
using Klau.Sdk.Tests.Helpers;

namespace Klau.Sdk.Tests;

public class IntegrationsClientTests
{
    private static (KlauClient client, MockHttpHandler handler) CreateClient()
    {
        var handler = new MockHttpHandler();
        var httpClient = new HttpClient(handler);
        var client = new KlauClient("kl_live_test", "https://api.test.com", httpClient);
        return (client, handler);
    }

    // --- IssueAiTokenAsync ---

    [Fact]
    public async Task IssueAiTokenAsync_SendsPostToCorrectPath()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            provider = "xai",
            model = "grok-4-1-fast-reasoning",
            baseUrl = "https://api.x.ai/v1",
            token = "xai-test-token",
            providerKeyId = "xai-key-uuid-abc",
            expiresAt = "2026-04-07T14:37:00Z",
            ttlSeconds = 900
        });

        await client.Integrations.IssueAiTokenAsync(new IssueAiTokenRequest
        {
            Purpose = AiTokenPurpose.NotesExtraction
        });

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.EndsWith("api/v1/integrations/ai-token", req.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task IssueAiTokenAsync_SerializesAllRequestFields()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            provider = "xai",
            model = "grok-4-1-fast-reasoning",
            baseUrl = "https://api.x.ai/v1",
            token = "xai-test",
            providerKeyId = "kid-1",
            expiresAt = "2026-04-07T14:37:00Z",
            ttlSeconds = 900
        });

        await client.Integrations.IssueAiTokenAsync(new IssueAiTokenRequest
        {
            Purpose = AiTokenPurpose.NotesExtraction,
            ModelHint = AiTokenModelHint.Reasoning,
            IssuedForUserLabel = "dispatch@example.com"
        });

        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        Assert.Equal("notes-extraction", root.GetProperty("purpose").GetString());
        Assert.Equal("reasoning", root.GetProperty("modelHint").GetString());
        Assert.Equal("dispatch@example.com", root.GetProperty("issuedForUserLabel").GetString());
    }

    [Fact]
    public async Task IssueAiTokenAsync_OmitsNullOptionalFields()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            provider = "xai",
            model = "grok-4-1-fast-reasoning",
            baseUrl = "https://api.x.ai/v1",
            token = "xai-test",
            providerKeyId = "kid-1",
            expiresAt = "2026-04-07T14:37:00Z",
            ttlSeconds = 900
        });

        await client.Integrations.IssueAiTokenAsync(new IssueAiTokenRequest
        {
            Purpose = AiTokenPurpose.NotesExtraction
        });

        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        Assert.Equal("notes-extraction", root.GetProperty("purpose").GetString());
        Assert.False(root.TryGetProperty("modelHint", out _));
        Assert.False(root.TryGetProperty("issuedForUserLabel", out _));
    }

    [Fact]
    public async Task IssueAiTokenAsync_DeserializesSuccessResponse()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            provider = "xai",
            model = "grok-4-1-fast-reasoning",
            baseUrl = "https://api.x.ai/v1",
            token = "xai-secret-token-value",
            providerKeyId = "xai-key-uuid-abc",
            expiresAt = "2026-04-07T14:37:00Z",
            ttlSeconds = 900
        });

        var result = await client.Integrations.IssueAiTokenAsync(new IssueAiTokenRequest
        {
            Purpose = AiTokenPurpose.NotesExtraction
        });

        Assert.Equal("xai", result.Provider);
        Assert.Equal("grok-4-1-fast-reasoning", result.Model);
        Assert.Equal("https://api.x.ai/v1", result.BaseUrl);
        Assert.Equal("xai-secret-token-value", result.Token);
        Assert.Equal("xai-key-uuid-abc", result.ProviderKeyId);
        Assert.Equal(900, result.TtlSeconds);
        Assert.Equal(new DateTime(2026, 4, 7, 14, 37, 0, DateTimeKind.Utc), result.ExpiresAt.ToUniversalTime());
    }

    [Fact]
    public async Task IssueAiTokenAsync_ThrowsOnDailyBudgetExceeded()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(
            HttpStatusCode.Forbidden,
            new ApiErrorBody("FEATURE_NOT_ENABLED", "Daily CLI LLM spend cap reached for this tenant"));

        var ex = await Assert.ThrowsAsync<KlauApiException>(() =>
            client.Integrations.IssueAiTokenAsync(new IssueAiTokenRequest
            {
                Purpose = AiTokenPurpose.NotesExtraction
            }));

        Assert.Equal("FEATURE_NOT_ENABLED", ex.ErrorCode);
        Assert.Equal(403, ex.StatusCode);
        Assert.True(ex.IsInsufficientScope);
    }

    [Fact]
    public async Task IssueAiTokenAsync_ThrowsOnProviderUnavailable()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(
            HttpStatusCode.ServiceUnavailable,
            new ApiErrorBody("PROVIDER_UNAVAILABLE", "xAI management API is down"));

        var ex = await Assert.ThrowsAsync<KlauApiException>(() =>
            client.Integrations.IssueAiTokenAsync(new IssueAiTokenRequest
            {
                Purpose = AiTokenPurpose.NotesExtraction
            }));

        Assert.Equal("PROVIDER_UNAVAILABLE", ex.ErrorCode);
        Assert.Equal(503, ex.StatusCode);
    }

    // --- ReportAiUsageAsync ---

    [Fact]
    public async Task ReportAiUsageAsync_SendsPostToCorrectPath()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            status = "RECORDED",
            costCents = 5
        });

        await client.Integrations.ReportAiUsageAsync(new ReportAiUsageRequest
        {
            ProviderKeyId = "xai-key-uuid-abc",
            RequestId = "11111111-2222-3333-4444-555555555555",
            Model = "grok-4-1-fast-reasoning",
            InputTokens = 1234,
            OutputTokens = 567,
            Purpose = AiTokenPurpose.NotesExtraction,
            OccurredAt = new DateTime(2026, 4, 7, 14, 22, 0, DateTimeKind.Utc),
            DurationMs = 4200
        });

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.EndsWith("api/v1/integrations/ai-usage", req.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ReportAiUsageAsync_SerializesAllFields()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            status = "RECORDED",
            costCents = 5
        });

        var occurredAt = new DateTime(2026, 4, 7, 14, 22, 0, DateTimeKind.Utc);

        await client.Integrations.ReportAiUsageAsync(new ReportAiUsageRequest
        {
            ProviderKeyId = "xai-key-uuid-abc",
            RequestId = "req-123",
            Model = "grok-4-1-fast-reasoning",
            InputTokens = 1234,
            OutputTokens = 567,
            Purpose = AiTokenPurpose.NotesExtraction,
            OccurredAt = occurredAt,
            DurationMs = 4200
        });

        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        Assert.Equal("xai-key-uuid-abc", root.GetProperty("providerKeyId").GetString());
        Assert.Equal("req-123", root.GetProperty("requestId").GetString());
        Assert.Equal("grok-4-1-fast-reasoning", root.GetProperty("model").GetString());
        Assert.Equal(1234, root.GetProperty("inputTokens").GetInt32());
        Assert.Equal(567, root.GetProperty("outputTokens").GetInt32());
        Assert.Equal("notes-extraction", root.GetProperty("purpose").GetString());
        Assert.Equal(4200, root.GetProperty("durationMs").GetInt32());
        Assert.True(root.TryGetProperty("occurredAt", out _));
    }

    [Fact]
    public async Task ReportAiUsageAsync_DeserializesRecordedStatus()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            status = "RECORDED",
            costCents = 5
        });

        var result = await client.Integrations.ReportAiUsageAsync(new ReportAiUsageRequest
        {
            ProviderKeyId = "xai-key-uuid-abc",
            RequestId = "req-1",
            Model = "grok-4-1-fast-reasoning",
            InputTokens = 100,
            OutputTokens = 50,
            Purpose = AiTokenPurpose.NotesExtraction,
            OccurredAt = DateTime.UtcNow,
            DurationMs = 1000
        });

        Assert.Equal(AiUsageReportStatus.RECORDED, result.Status);
        Assert.Equal(5, result.CostCents);
    }

    [Fact]
    public async Task ReportAiUsageAsync_DeserializesDuplicateIgnoredStatus()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            status = "DUPLICATE_IGNORED",
            costCents = 5
        });

        var result = await client.Integrations.ReportAiUsageAsync(new ReportAiUsageRequest
        {
            ProviderKeyId = "xai-key-uuid-abc",
            RequestId = "req-replay",
            Model = "grok-4-1-fast-reasoning",
            InputTokens = 100,
            OutputTokens = 50,
            Purpose = AiTokenPurpose.NotesExtraction,
            OccurredAt = DateTime.UtcNow,
            DurationMs = 1000
        });

        // Idempotent replay — CLI can drop the retry entry, original cost returned.
        Assert.Equal(AiUsageReportStatus.DUPLICATE_IGNORED, result.Status);
        Assert.Equal(5, result.CostCents);
    }

    [Fact]
    public async Task ReportAiUsageAsync_ThrowsOnUnauthorizedKey()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(
            HttpStatusCode.Unauthorized,
            new ApiErrorBody("UNAUTHORIZED_KEY", "providerKeyId belongs to a different tenant"));

        var ex = await Assert.ThrowsAsync<KlauApiException>(() =>
            client.Integrations.ReportAiUsageAsync(new ReportAiUsageRequest
            {
                ProviderKeyId = "xai-key-wrong-tenant",
                RequestId = "req-1",
                Model = "grok-4-1-fast-reasoning",
                InputTokens = 100,
                OutputTokens = 50,
                Purpose = AiTokenPurpose.NotesExtraction,
                OccurredAt = DateTime.UtcNow,
                DurationMs = 1000
            }));

        Assert.Equal("UNAUTHORIZED_KEY", ex.ErrorCode);
        Assert.Equal(401, ex.StatusCode);
        Assert.True(ex.IsUnauthorized);
    }

    [Fact]
    public async Task ReportAiUsageAsync_ThrowsOnPersistFailed()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(
            HttpStatusCode.ServiceUnavailable,
            new ApiErrorBody("PERSIST_FAILED", "DB write failed"));

        var ex = await Assert.ThrowsAsync<KlauApiException>(() =>
            client.Integrations.ReportAiUsageAsync(new ReportAiUsageRequest
            {
                ProviderKeyId = "xai-key-uuid-abc",
                RequestId = "req-1",
                Model = "grok-4-1-fast-reasoning",
                InputTokens = 100,
                OutputTokens = 50,
                Purpose = AiTokenPurpose.NotesExtraction,
                OccurredAt = DateTime.UtcNow,
                DurationMs = 1000
            }));

        Assert.Equal("PERSIST_FAILED", ex.ErrorCode);
        Assert.Equal(503, ex.StatusCode);
    }

    // --- Tenant scoping ---

    [Fact]
    public async Task IssueAiTokenAsync_TenantScope_SendsTenantHeader()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            provider = "xai",
            model = "grok-4-1-fast-reasoning",
            baseUrl = "https://api.x.ai/v1",
            token = "xai-t",
            providerKeyId = "kid",
            expiresAt = "2026-04-07T14:37:00Z",
            ttlSeconds = 900
        });

        var scope = client.ForTenant("child-tenant-id");
        await scope.Integrations.IssueAiTokenAsync(new IssueAiTokenRequest
        {
            Purpose = AiTokenPurpose.NotesExtraction
        });

        var req = Assert.Single(handler.SentRequests);
        Assert.True(req.Headers.Contains("Klau-Tenant-Id"));
        Assert.Equal("child-tenant-id", req.Headers.GetValues("Klau-Tenant-Id").First());
    }
}
