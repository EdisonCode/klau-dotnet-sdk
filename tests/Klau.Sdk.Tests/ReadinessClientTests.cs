using System.Net;
using Klau.Sdk.Readiness;
using Klau.Sdk.Tests.Helpers;
using Microsoft.Extensions.Logging;

namespace Klau.Sdk.Tests;

public class ReadinessClientTests
{
    private static (KlauClient client, MockHttpHandler handler) CreateClient()
    {
        var handler = new MockHttpHandler();
        var httpClient = new HttpClient(handler);
        var client = new KlauClient("kl_live_test", "https://api.test.com", httpClient);
        return (client, handler);
    }

    [Fact]
    public async Task CheckAsync_ReturnsReadinessReport()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            canGoLive = false,
            readyPercentage = 40,
            sections = new[]
            {
                new
                {
                    key = "operation",
                    label = "Operations",
                    items = new object[]
                    {
                        new { key = "drivers", label = "Drivers", status = "complete", count = 3, required = true },
                        new { key = "trucks", label = "Trucks", status = "incomplete", count = 0, detail = "Add at least one truck", route = "/trucks", required = true },
                        new { key = "yards", label = "Yards", status = "in_progress", count = 1, detail = "Set a default yard", route = "/yards", required = true },
                    }
                },
                new
                {
                    key = "dumpSites",
                    label = "Dump Sites",
                    items = new object[]
                    {
                        new { key = "dumpSites", label = "Dump Sites", status = "incomplete", count = 0, detail = "Add at least one dump site", route = "/dump-sites", required = true },
                        new { key = "dumpSiteMaterials", label = "Dump Site Materials", status = "incomplete", count = 0, detail = "Configure accepted materials", route = "/dump-sites", required = true },
                    }
                }
            }
        });

        var report = await client.Readiness.CheckAsync();

        Assert.False(report.CanGoLive);
        Assert.Equal(40, report.ReadyPercentage);
        Assert.Equal(2, report.Sections.Count);

        // Operations section
        var ops = report.Sections[0];
        Assert.Equal("operation", ops.Key);
        Assert.Equal(3, ops.Items.Count);

        // Drivers complete
        Assert.True(ops.Items[0].IsComplete);
        Assert.Equal(3, ops.Items[0].Count);

        // Trucks incomplete
        Assert.True(ops.Items[1].IsIncomplete);
        Assert.Equal("Add at least one truck", ops.Items[1].Detail);
        Assert.True(ops.Items[1].Required);

        // Yards in progress
        Assert.True(ops.Items[2].IsIncomplete);
        Assert.Equal("in_progress", ops.Items[2].Status);
    }

    [Fact]
    public async Task CheckAsync_AllReady_ReturnsTrue()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            canGoLive = true,
            readyPercentage = 100,
            sections = new[]
            {
                new
                {
                    key = "operation",
                    label = "Operations",
                    items = new object[]
                    {
                        new { key = "drivers", label = "Drivers", status = "complete", count = 5, required = true },
                    }
                }
            }
        });

        var report = await client.Readiness.CheckAsync();

        Assert.True(report.CanGoLive);
        Assert.Equal(100, report.ReadyPercentage);
    }

    [Fact]
    public async Task CheckAsync_SendsCorrectPath()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            canGoLive = true,
            readyPercentage = 100,
            sections = Array.Empty<object>()
        });

        await client.Readiness.CheckAsync();

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Get, req.Method);
        Assert.Contains("api/v1/companies/go-live-readiness", req.RequestUri!.ToString());
    }
}
