using System.Net;
using Klau.Sdk.Common;
using Klau.Sdk.Jobs;
using Klau.Sdk.Tests.Helpers;
using KlauRequestOptions = Klau.Sdk.Common.KlauRequestOptions;

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
    public async Task PostEndpoint_DoesNotRetryOn503()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.ServiceUnavailable);

        // POST without Idempotency-Key should NOT retry on 503 — the server
        // may have processed the request before returning the error.
        await Assert.ThrowsAsync<KlauApiException>(
            () => client.Jobs.CancelAsync("j-1"));

        Assert.Single(handler.SentRequests);
    }

    [Fact]
    public async Task PostEndpoint_DoesRetryOn429()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.TooManyRequests);
        handler.EnqueueResponse(HttpStatusCode.OK);

        // POST should always retry 429 — the server guarantees it did not
        // process the request.
        await client.Jobs.CancelAsync("j-1");

        Assert.Equal(2, handler.SentRequests.Count);
    }

    [Fact]
    public async Task GetEndpoint_DoesRetryOn503()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.ServiceUnavailable);
        handler.EnqueueResponse(HttpStatusCode.OK, SuccessJob);

        // GET is idempotent and safe to retry on any transient error.
        var job = await client.Jobs.GetAsync("j-1");

        Assert.Equal(2, handler.SentRequests.Count);
        Assert.Equal("j-1", job.Id);
    }

    [Fact]
    public async Task PatchEndpoint_DoesNotRetryOn502()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.BadGateway);

        // PATCH without Idempotency-Key should NOT retry on 502 — the server
        // may have applied the partial update before the gateway failed.
        await Assert.ThrowsAsync<KlauApiException>(
            () => client.Jobs.UpdateAsync("j-1", new UpdateJobRequest { ContainerSize = 30 }));

        Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Patch, handler.SentRequests[0].Method);
    }

    [Fact]
    public async Task PostWithIdempotencyKey_DoesRetryOn503()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.ServiceUnavailable);
        handler.EnqueueResponse(HttpStatusCode.OK, new { jobId = "j-new" });

        // POST with Idempotency-Key IS safe to retry — the server deduplicates.
        var jobId = await client.Jobs.CreateAsync(
            new CreateJobRequest
            {
                CustomerId = "c-1",
                SiteId = "s-1",
                Type = JobType.DELIVERY,
                RequestedDate = "2026-04-05",
            },
            new KlauRequestOptions { IdempotencyKey = "idem-123" });

        Assert.Equal(2, handler.SentRequests.Count);
        Assert.True(handler.SentRequests[0].Headers.Contains("Idempotency-Key"));
        Assert.Equal("j-new", jobId);
    }

    [Fact]
    public async Task PostEndpoint_DoesNotRetryOnNetworkError()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueException(new HttpRequestException("Connection refused"));

        // POST without Idempotency-Key should NOT retry network errors —
        // the server may have received and processed the request.
        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.Jobs.CancelAsync("j-1"));

        Assert.Single(handler.SentRequests);
    }

    [Fact]
    public async Task PostEndpoint_DoesNotRetryOnTimeout()
    {
        var (client, handler) = CreateClient();
        // TaskCanceledException with a non-cancelled token simulates an HTTP timeout
        handler.EnqueueException(new TaskCanceledException("The request timed out"));

        // POST without Idempotency-Key should NOT retry timeouts —
        // the server may have started processing before the timeout.
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => client.Jobs.CancelAsync("j-1"));

        Assert.Single(handler.SentRequests);
    }

    [Fact]
    public async Task GetEndpoint_DoesRetryOnNetworkError()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueException(new HttpRequestException("Connection refused"));
        handler.EnqueueResponse(HttpStatusCode.OK, SuccessJob);

        // GET is idempotent — network errors should be retried.
        var job = await client.Jobs.GetAsync("j-1");

        Assert.Equal(2, handler.SentRequests.Count);
        Assert.Equal("j-1", job.Id);
    }

    [Fact]
    public async Task GetEndpoint_DoesRetryOnTimeout()
    {
        var (client, handler) = CreateClient();
        // TaskCanceledException with a non-cancelled token simulates an HTTP timeout
        handler.EnqueueException(new TaskCanceledException("The request timed out"));
        handler.EnqueueResponse(HttpStatusCode.OK, SuccessJob);

        // GET is idempotent — timeouts should be retried.
        var job = await client.Jobs.GetAsync("j-1");

        Assert.Equal(2, handler.SentRequests.Count);
        Assert.Equal("j-1", job.Id);
    }

    [Fact]
    public async Task PostEndpoint_ExhaustsAll429Retries()
    {
        var (client, handler) = CreateClient();
        // MaxRetries is 3, so total attempts = 4 (initial + 3 retries).
        // Even non-idempotent POST retries on 429 because the server
        // guarantees no processing — so all 4 attempts should fire.
        handler.EnqueueResponse(HttpStatusCode.TooManyRequests);
        handler.EnqueueResponse(HttpStatusCode.TooManyRequests);
        handler.EnqueueResponse(HttpStatusCode.TooManyRequests);
        handler.EnqueueResponse(HttpStatusCode.TooManyRequests);

        await Assert.ThrowsAsync<KlauApiException>(
            () => client.Jobs.CancelAsync("j-1"));

        Assert.Equal(4, handler.SentRequests.Count);
    }

    [Fact]
    public async Task DeleteEndpoint_DoesRetryOn503()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.ServiceUnavailable);
        handler.EnqueueResponse(HttpStatusCode.OK);

        // DELETE is idempotent — safe to retry on any transient error.
        await client.Jobs.DeleteAsync("j-1");

        Assert.Equal(2, handler.SentRequests.Count);
        Assert.Equal(HttpMethod.Delete, handler.SentRequests[0].Method);
    }
}
