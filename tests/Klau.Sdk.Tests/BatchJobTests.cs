using System.Net;
using System.Text.Json;
using Klau.Sdk.Common;
using Klau.Sdk.Jobs;
using Klau.Sdk.Tests.Helpers;

namespace Klau.Sdk.Tests;

public class BatchJobTests
{
    private static (KlauClient client, MockHttpHandler handler) CreateClient()
    {
        var handler = new MockHttpHandler();
        var httpClient = new HttpClient(handler);
        var client = new KlauClient("kl_live_test", "https://api.test.com", httpClient);
        return (client, handler);
    }

    [Fact]
    public async Task CreateBatchAsync_SendsAllJobs()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            created = new[]
            {
                new { jobId = "j-1", externalId = "ext-1" },
                new { jobId = "j-2", externalId = "ext-2" }
            },
            errors = Array.Empty<object>()
        });

        var jobs = new List<CreateJobRequest>
        {
            new()
            {
                CustomerId = "cust-1",
                SiteId = "site-1",
                Type = JobType.DELIVERY,
                ContainerSize = 20,
                RequestedDate = "2026-03-15",
                ExternalId = "ext-1"
            },
            new()
            {
                CustomerId = "cust-2",
                SiteId = "site-2",
                Type = JobType.PICKUP,
                ContainerSize = 30,
                RequestedDate = "2026-03-15",
                ExternalId = "ext-2"
            }
        };

        var result = await client.Jobs.CreateBatchAsync(jobs);

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.EndsWith("api/v1/jobs/batch", req.RequestUri!.AbsolutePath);

        Assert.Equal(2, result.Created.Count);
        Assert.Equal("j-1", result.Created[0].JobId);
        Assert.Equal("ext-1", result.Created[0].ExternalId);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task CreateBatchAsync_ReturnsPartialErrors()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            created = new[]
            {
                new { jobId = "j-1", externalId = "ext-1" }
            },
            errors = new[]
            {
                new { index = 1, externalId = "ext-bad", code = "VALIDATION_ERROR", message = "Invalid site" }
            }
        });

        var jobs = new List<CreateJobRequest>
        {
            new() { CustomerId = "cust-1", SiteId = "site-1", Type = JobType.DELIVERY, RequestedDate = "2026-03-15", ExternalId = "ext-1" },
            new() { CustomerId = "cust-2", SiteId = "site-2", Type = JobType.DELIVERY, RequestedDate = "2026-03-15", ExternalId = "ext-bad" }
        };

        var result = await client.Jobs.CreateBatchAsync(jobs);

        Assert.Single(result.Created);
        Assert.Single(result.Errors);
        Assert.Equal(1, result.Errors[0].Index);
        Assert.Equal("VALIDATION_ERROR", result.Errors[0].Code);
        Assert.Equal("ext-bad", result.Errors[0].ExternalId);
    }

    [Fact]
    public async Task CreateBatchAsync_SerializesExternalId()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            created = new[] { new { jobId = "j-1", externalId = "MY-ORDER-99" } },
            errors = Array.Empty<object>()
        });

        var jobs = new List<CreateJobRequest>
        {
            new()
            {
                CustomerId = "cust-1",
                SiteId = "site-1",
                Type = JobType.SWAP,
                ContainerSize = 40,
                RequestedDate = "2026-03-20",
                ExternalId = "MY-ORDER-99",
                Notes = "Gate code: 5678"
            }
        };

        await client.Jobs.CreateBatchAsync(jobs);

        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        var jobsArr = doc.RootElement.GetProperty("jobs");
        Assert.Equal(1, jobsArr.GetArrayLength());
        Assert.Equal("MY-ORDER-99", jobsArr[0].GetProperty("externalId").GetString());
        Assert.Equal("SWAP", jobsArr[0].GetProperty("type").GetString());
    }
}
