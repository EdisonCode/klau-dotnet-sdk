using System.Net;
using Klau.Sdk.Tests.Helpers;

namespace Klau.Sdk.Tests;

public class ListAllAsyncTests
{
    private static (KlauClient client, MockHttpHandler handler) CreateClient()
    {
        var handler = new MockHttpHandler();
        var httpClient = new HttpClient(handler);
        var client = new KlauClient("kl_live_test", "https://api.test.com", httpClient);
        return (client, handler);
    }

    [Fact]
    public async Task ListAllAsync_SinglePage_ReturnsAllItems()
    {
        var (client, handler) = CreateClient();
        var jobs = new[]
        {
            new { id = "j-1", type = "DELIVERY", status = "UNASSIGNED", customerName = "Acme",
                  createdAt = "2026-01-01T00:00:00Z", updatedAt = "2026-01-01T00:00:00Z" },
            new { id = "j-2", type = "PICKUP", status = "ASSIGNED", customerName = "Beta",
                  createdAt = "2026-01-02T00:00:00Z", updatedAt = "2026-01-02T00:00:00Z" }
        };
        handler.EnqueueResponse(HttpStatusCode.OK,
            new { jobs, total = 2, page = 1, pageSize = 100, hasMore = false });

        var items = new List<string>();
        await foreach (var job in client.Jobs.ListAllAsync())
        {
            items.Add(job.Id);
        }

        Assert.Equal(2, items.Count);
        Assert.Equal("j-1", items[0]);
        Assert.Equal("j-2", items[1]);
        Assert.Single(handler.SentRequests);
    }

    [Fact]
    public async Task ListAllAsync_MultiplePages_PagesAutomatically()
    {
        var (client, handler) = CreateClient();

        // Page 1: hasMore = true
        var page1Jobs = new[]
        {
            new { id = "j-1", type = "DELIVERY", status = "UNASSIGNED", customerName = "Acme",
                  createdAt = "2026-01-01T00:00:00Z", updatedAt = "2026-01-01T00:00:00Z" },
            new { id = "j-2", type = "PICKUP", status = "ASSIGNED", customerName = "Beta",
                  createdAt = "2026-01-02T00:00:00Z", updatedAt = "2026-01-02T00:00:00Z" }
        };
        handler.EnqueueResponse(HttpStatusCode.OK,
            new { jobs = page1Jobs, total = 3, page = 1, pageSize = 2, hasMore = true });

        // Page 2: hasMore = false
        var page2Jobs = new[]
        {
            new { id = "j-3", type = "SWAP", status = "COMPLETED", customerName = "Gamma",
                  createdAt = "2026-01-03T00:00:00Z", updatedAt = "2026-01-03T00:00:00Z" }
        };
        handler.EnqueueResponse(HttpStatusCode.OK,
            new { jobs = page2Jobs, total = 3, page = 2, pageSize = 2, hasMore = false });

        var items = new List<string>();
        await foreach (var job in client.Jobs.ListAllAsync(pageSize: 2))
        {
            items.Add(job.Id);
        }

        Assert.Equal(3, items.Count);
        Assert.Equal("j-1", items[0]);
        Assert.Equal("j-2", items[1]);
        Assert.Equal("j-3", items[2]);

        // Verify two requests were made with correct page params
        Assert.Equal(2, handler.SentRequests.Count);
        var req1Query = handler.SentRequests[0].RequestUri!.Query;
        Assert.Contains("page=1", req1Query);
        Assert.Contains("pageSize=2", req1Query);
        var req2Query = handler.SentRequests[1].RequestUri!.Query;
        Assert.Contains("page=2", req2Query);
        Assert.Contains("pageSize=2", req2Query);
    }

    [Fact]
    public async Task ListAllAsync_EmptyResult_YieldsNothing()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK,
            new { jobs = Array.Empty<object>(), total = 0, page = 1, pageSize = 100, hasMore = false });

        var items = new List<string>();
        await foreach (var job in client.Jobs.ListAllAsync())
        {
            items.Add(job.Id);
        }

        Assert.Empty(items);
        Assert.Single(handler.SentRequests);
    }
}
