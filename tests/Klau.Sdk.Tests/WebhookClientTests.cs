using System.Net;
using System.Text.Json;
using Klau.Sdk.Tests.Helpers;
using Klau.Sdk.Webhooks;

namespace Klau.Sdk.Tests;

public class WebhookClientTests
{
    private static (KlauClient client, MockHttpHandler handler) CreateClient()
    {
        var handler = new MockHttpHandler();
        var httpClient = new HttpClient(handler);
        var client = new KlauClient("kl_live_test", "https://api.test.com", httpClient);
        return (client, handler);
    }

    [Fact]
    public async Task CreateAsync_SendsCorrectBody()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK,
            new { webhookId = "wh-1", secret = "whsec_abc" });

        var result = await client.Webhooks.CreateAsync(new CreateWebhookRequest
        {
            Url = "https://example.com/webhook",
            Events = ["job.assigned", "job.completed"],
            Description = "My integration"
        });

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.Contains("settings/developer/webhooks", req.RequestUri!.ToString());

        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("https://example.com/webhook", doc.RootElement.GetProperty("url").GetString());
        Assert.Equal(2, doc.RootElement.GetProperty("events").GetArrayLength());

        Assert.Equal("wh-1", result.WebhookId);
        Assert.Equal("whsec_abc", result.Secret);
    }

    [Fact]
    public async Task DeleteAsync_SendsDeleteToCorrectPath()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK);

        await client.Webhooks.DeleteAsync("wh-1");

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Delete, req.Method);
        Assert.EndsWith("settings/developer/webhooks/wh-1", req.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task TestAsync_ReturnsResult()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK,
            new { success = true, statusCode = 200, responseTime = 42 });

        var result = await client.Webhooks.TestAsync("wh-1");

        Assert.True(result.Success);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal(42, result.ResponseTime);
    }

    [Fact]
    public async Task GetSettingsAsync_ReturnsSettings()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            developerAccountId = "dev-1",
            apiKeys = new[] { new { id = "key-1", name = "prod", prefix = "kl_live", lastFour = "abcd", scopes = new[] { "*" }, status = "ACTIVE", createdAt = "2026-01-01" } },
            webhookEndpoints = new[] { new { id = "wh-1", url = "https://example.com/wh", events = new[] { "*" }, status = "ACTIVE", createdAt = "2026-01-01", updatedAt = "2026-01-01" } }
        });

        var settings = await client.Webhooks.GetSettingsAsync();

        Assert.Equal("dev-1", settings.DeveloperAccountId);
        Assert.Single(settings.ApiKeys);
        Assert.Single(settings.WebhookEndpoints);
        Assert.Equal("wh-1", settings.WebhookEndpoints[0].Id);
    }

    [Fact]
    public async Task SetEnabledAsync_SendsPatchWithEnabled()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new { success = true });

        await client.Webhooks.SetEnabledAsync("wh-1", false);

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Patch, req.Method);
        Assert.Contains("wh-1", req.RequestUri!.ToString());

        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        Assert.False(doc.RootElement.GetProperty("enabled").GetBoolean());
    }
}
