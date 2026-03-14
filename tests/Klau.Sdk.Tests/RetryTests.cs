using System.Net;
using Klau.Sdk.Common;
using Klau.Sdk.Jobs;
using Klau.Sdk.Tests.Helpers;

namespace Klau.Sdk.Tests;

public class RetryTests
{
    private static (KlauClient client, MockHttpHandler handler) CreateClient()
    {
        var handler = new MockHttpHandler();
        var httpClient = new HttpClient(handler);
        var client = new KlauClient("kl_live_test", "https://api.test.com", httpClient);
        return (client, handler);
    }

    private static readonly object SuccessJob = new
    {
        id = "j-1", type = "DELIVERY", status = "UNASSIGNED",
        customerName = "Test",
        createdAt = "2026-01-01T00:00:00Z", updatedAt = "2026-01-01T00:00:00Z"
    };

    [Fact]
    public async Task TooManyRequests429_TriggersRetry()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.TooManyRequests);
        handler.EnqueueResponse(HttpStatusCode.OK, SuccessJob);

        var job = await client.Jobs.GetAsync("j-1");

        Assert.Equal(2, handler.SentRequests.Count);
        Assert.Equal("j-1", job.Id);
    }

    [Fact]
    public async Task ServiceUnavailable503_TriggersRetry()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.ServiceUnavailable);
        handler.EnqueueResponse(HttpStatusCode.OK, SuccessJob);

        var job = await client.Jobs.GetAsync("j-1");

        Assert.Equal(2, handler.SentRequests.Count);
        Assert.Equal("j-1", job.Id);
    }

    [Fact]
    public async Task BadGateway502_TriggersRetry()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.BadGateway);
        handler.EnqueueResponse(HttpStatusCode.OK, SuccessJob);

        var job = await client.Jobs.GetAsync("j-1");

        Assert.Equal(2, handler.SentRequests.Count);
    }

    [Fact]
    public async Task GatewayTimeout504_TriggersRetry()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.GatewayTimeout);
        handler.EnqueueResponse(HttpStatusCode.OK, SuccessJob);

        var job = await client.Jobs.GetAsync("j-1");

        Assert.Equal(2, handler.SentRequests.Count);
    }

    [Fact]
    public async Task SucceedsAfterTransientFailureThenSuccess()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.ServiceUnavailable);
        handler.EnqueueResponse(HttpStatusCode.ServiceUnavailable);
        handler.EnqueueResponse(HttpStatusCode.OK, SuccessJob);

        var job = await client.Jobs.GetAsync("j-1");

        Assert.Equal(3, handler.SentRequests.Count);
        Assert.Equal("j-1", job.Id);
    }

    [Fact]
    public async Task GivesUpAfterMaxRetries()
    {
        var (client, handler) = CreateClient();
        // MaxRetries is 3, so total attempts = 4 (initial + 3 retries)
        handler.EnqueueResponse(HttpStatusCode.ServiceUnavailable);
        handler.EnqueueResponse(HttpStatusCode.ServiceUnavailable);
        handler.EnqueueResponse(HttpStatusCode.ServiceUnavailable);
        handler.EnqueueResponse(HttpStatusCode.ServiceUnavailable);

        // After exhausting retries, the last 503 response should cause a deserialization
        // failure or error since it has no valid body — it should throw
        await Assert.ThrowsAsync<KlauApiException>(
            () => client.Jobs.GetAsync("j-1"));

        Assert.Equal(4, handler.SentRequests.Count);
    }

    [Fact]
    public async Task BadRequest400_DoesNotRetry()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.BadRequest,
            new ApiErrorBody("VALIDATION_ERROR", "Invalid input"));

        await Assert.ThrowsAsync<KlauApiException>(
            () => client.Jobs.GetAsync("j-1"));

        // Should only have sent 1 request — no retries for 400
        Assert.Single(handler.SentRequests);
    }

    [Fact]
    public async Task Unauthorized401_DoesNotRetry()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.Unauthorized,
            new ApiErrorBody("UNAUTHORIZED", "Bad token"));

        await Assert.ThrowsAsync<KlauApiException>(
            () => client.Jobs.GetAsync("j-1"));

        Assert.Single(handler.SentRequests);
    }

    [Fact]
    public async Task NotFound404_DoesNotRetry()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.NotFound,
            new ApiErrorBody("NOT_FOUND", "Not found"));

        await Assert.ThrowsAsync<KlauApiException>(
            () => client.Jobs.GetAsync("j-1"));

        Assert.Single(handler.SentRequests);
    }

    [Fact]
    public async Task Forbidden403_DoesNotRetry()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.Forbidden,
            new ApiErrorBody("FORBIDDEN", "Access denied"));

        await Assert.ThrowsAsync<KlauApiException>(
            () => client.Jobs.GetAsync("j-1"));

        Assert.Single(handler.SentRequests);
    }

    [Fact]
    public async Task VoidEndpoint_RetriesOnTransientError()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.ServiceUnavailable);
        handler.EnqueueResponse(HttpStatusCode.OK);

        await client.Jobs.CancelAsync("j-1");

        Assert.Equal(2, handler.SentRequests.Count);
    }
}
