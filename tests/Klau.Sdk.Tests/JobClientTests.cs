using System.Net;
using System.Text.Json;
using Klau.Sdk.Common;
using Klau.Sdk.Jobs;
using Klau.Sdk.Tests.Helpers;

namespace Klau.Sdk.Tests;

public class JobClientTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper) }
    };

    private static (KlauClient client, MockHttpHandler handler) CreateClient()
    {
        var handler = new MockHttpHandler();
        var httpClient = new HttpClient(handler);
        var client = new KlauClient("kl_live_test", "https://api.test.com", httpClient);
        return (client, handler);
    }

    // --- ListAsync ---

    [Fact]
    public async Task ListAsync_SendsCorrectPath()
    {
        var (client, handler) = CreateClient();
        // API returns { data: { jobs: [...], total: 0, page: 1, pageSize: 100, hasMore: false } }
        handler.EnqueueResponse(HttpStatusCode.OK,
            new { jobs = new List<object>(), total = 0, page = 1, pageSize = 100, hasMore = false });

        await client.Jobs.ListAsync();

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Get, req.Method);
        Assert.Contains("api/v1/jobs", req.RequestUri!.ToString());
    }

    [Fact]
    public async Task ListAsync_IncludesQueryParams()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK,
            new { jobs = new List<object>(), total = 0, page = 2, pageSize = 50, hasMore = false });

        await client.Jobs.ListAsync(date: "2026-03-13", status: JobStatus.ASSIGNED, driverId: "drv-1", page: 2, pageSize: 50);

        var req = Assert.Single(handler.SentRequests);
        var query = req.RequestUri!.Query;
        Assert.Contains("date=2026-03-13", query);
        Assert.Contains("status=ASSIGNED", query);
        Assert.Contains("driverId=drv-1", query);
        Assert.Contains("page=2", query);
        Assert.Contains("pageSize=50", query);
    }

    [Fact]
    public async Task ListAsync_ReturnsPagedResult()
    {
        var (client, handler) = CreateClient();
        var jobs = new[]
        {
            new { id = "j-1", type = "DELIVERY", status = "UNASSIGNED", customerName = "Acme",
                  createdAt = "2026-01-01T00:00:00Z", updatedAt = "2026-01-01T00:00:00Z" }
        };
        handler.EnqueueResponse(HttpStatusCode.OK,
            new { jobs, total = 42, page = 1, pageSize = 100, hasMore = true });

        var result = await client.Jobs.ListAsync();

        Assert.Single(result.Items);
        Assert.Equal(42, result.Total);
        Assert.True(result.HasMore);
    }

    // --- GetAsync ---

    [Fact]
    public async Task GetAsync_SendsCorrectPath()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new { id = "j-123", type = "PICKUP", status = "COMPLETED",
            customerName = "Test", createdAt = "2026-01-01T00:00:00Z", updatedAt = "2026-01-01T00:00:00Z" });

        var job = await client.Jobs.GetAsync("j-123");

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Get, req.Method);
        Assert.EndsWith("api/v1/jobs/j-123", req.RequestUri!.AbsolutePath);
        Assert.Equal("j-123", job.Id);
    }

    // --- CreateAsync ---

    [Fact]
    public async Task CreateAsync_ReturnsJobId()
    {
        var (client, handler) = CreateClient();
        // API returns { data: { jobId: "j-new" } }
        handler.EnqueueResponse(HttpStatusCode.OK, new { jobId = "j-new" });

        var request = new CreateJobRequest
        {
            CustomerId = "cust-1",
            SiteId = "site-1",
            Type = JobType.DELIVERY,
            ContainerSize = 20,
            RequestedDate = "2026-03-15",
            TimeWindow = TimeWindow.MORNING,
            Priority = JobPriority.HIGH,
            Notes = "Gate code: 1234",
            ExternalId = "ext-99",
            DumpSiteId = "dump-1",
            ContainerNumber = "C-100"
        };

        var jobId = await client.Jobs.CreateAsync(request);

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.EndsWith("api/v1/jobs", req.RequestUri!.AbsolutePath);

        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        Assert.Equal("cust-1", root.GetProperty("customerId").GetString());
        Assert.Equal("site-1", root.GetProperty("siteId").GetString());
        Assert.Equal("DELIVERY", root.GetProperty("type").GetString());
        Assert.Equal(20, root.GetProperty("containerSize").GetInt32());
        Assert.Equal("2026-03-15", root.GetProperty("requestedDate").GetString());
        Assert.Equal("MORNING", root.GetProperty("timeWindow").GetString());
        Assert.Equal("HIGH", root.GetProperty("priority").GetString());
        Assert.Equal("Gate code: 1234", root.GetProperty("notes").GetString());
        Assert.Equal("ext-99", root.GetProperty("externalId").GetString());
        Assert.Equal("dump-1", root.GetProperty("dumpSiteId").GetString());
        Assert.Equal("C-100", root.GetProperty("containerNumber").GetString());

        Assert.Equal("j-new", jobId);
    }

    // --- UpdateAsync ---

    [Fact]
    public async Task UpdateAsync_UsesPatchMethod()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new { id = "j-1", type = "DELIVERY", status = "UNASSIGNED",
            customerName = "Test", containerSize = 30, createdAt = "2026-01-01T00:00:00Z",
            updatedAt = "2026-01-01T00:00:00Z" });

        var request = new UpdateJobRequest { ContainerSize = 30 };
        await client.Jobs.UpdateAsync("j-1", request);

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Patch, req.Method);
        Assert.EndsWith("api/v1/jobs/j-1", req.RequestUri!.AbsolutePath);
    }

    // --- AssignAsync ---

    [Fact]
    public async Task AssignAsync_SendsCorrectPathAndBody()
    {
        var (client, handler) = CreateClient();
        // API returns { data: { success: true } }
        handler.EnqueueResponse(HttpStatusCode.OK, new { success = true });

        var request = new AssignJobRequest
        {
            DriverId = "drv-1",
            TruckId = "trk-1",
            Sequence = 3,
            ScheduledDate = "2026-03-15",
            EstimatedStartTime = "08:30"
        };
        var result = await client.Jobs.AssignAsync("j-1", request);

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.EndsWith("api/v1/jobs/j-1/assign", req.RequestUri!.AbsolutePath);

        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        Assert.Equal("drv-1", root.GetProperty("driverId").GetString());
        Assert.Equal("trk-1", root.GetProperty("truckId").GetString());
        Assert.Equal(3, root.GetProperty("sequence").GetInt32());
        Assert.Equal("2026-03-15", root.GetProperty("scheduledDate").GetString());
        Assert.Equal("08:30", root.GetProperty("estimatedStartTime").GetString());

        Assert.True(result.Success);
    }

    // --- UnassignAsync ---

    [Fact]
    public async Task UnassignAsync_SendsPostToCorrectPath()
    {
        var (client, handler) = CreateClient();
        // API returns { data: { jobId: "j-1", previousDriverId: "drv-1" } }
        handler.EnqueueResponse(HttpStatusCode.OK, new { jobId = "j-1", previousDriverId = "drv-1" });

        var result = await client.Jobs.UnassignAsync("j-1");

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.EndsWith("api/v1/jobs/j-1/unassign", req.RequestUri!.AbsolutePath);
        Assert.Equal("j-1", result.JobId);
        Assert.Equal("drv-1", result.PreviousDriverId);
    }

    // --- CancelAsync ---

    [Fact]
    public async Task CancelAsync_SendsPostToCorrectPath()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK);

        await client.Jobs.CancelAsync("j-1");

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.EndsWith("api/v1/jobs/j-1/cancel", req.RequestUri!.AbsolutePath);
    }

    // --- StartAsync ---

    [Fact]
    public async Task StartAsync_SendsPostToCorrectPath()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK);

        await client.Jobs.StartAsync("j-1");

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.EndsWith("api/v1/jobs/j-1/start", req.RequestUri!.AbsolutePath);
    }

    // --- CompleteAsync ---

    [Fact]
    public async Task CompleteAsync_SendsPostToCorrectPath()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK);

        await client.Jobs.CompleteAsync("j-1");

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.EndsWith("api/v1/jobs/j-1/complete", req.RequestUri!.AbsolutePath);
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_SendsDeleteToCorrectPath()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK);

        await client.Jobs.DeleteAsync("j-1");

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Delete, req.Method);
        Assert.EndsWith("api/v1/jobs/j-1", req.RequestUri!.AbsolutePath);
    }

    // --- ContainerSlot ---

    [Fact]
    public async Task UpdateAsync_SerializesContainerSlot()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new { id = "j-1", type = "DELIVERY", status = "UNASSIGNED",
            customerName = "Test", containerSlot = "SECONDARY", createdAt = "2026-01-01T00:00:00Z", updatedAt = "2026-01-01T00:00:00Z" });

        var request = new UpdateJobRequest { ContainerSlot = ContainerSlot.SECONDARY };
        var job = await client.Jobs.UpdateAsync("j-1", request);

        // Verify request serialization
        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("SECONDARY", doc.RootElement.GetProperty("containerSlot").GetString());

        // Verify response deserialization
        Assert.Equal(ContainerSlot.SECONDARY, job.ContainerSlot);
    }

    // --- RecordTelemetryBatchAsync ---

    [Fact]
    public async Task RecordTelemetryBatchAsync_SendsCorrectPathAndBody()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            processed = 2, updated = 2, notFound = new string[0], errors = new object[0]
        });

        var entries = new List<TelemetryEntry>
        {
            new() { JobId = "j-1", ActualStartTime = "2026-04-02T08:15:00Z", ActualEndTime = "2026-04-02T08:42:00Z" },
            new() { ExternalId = "WCN-ORD-1234", ActualStartTime = "2026-04-02T09:00:00Z" }
        };

        var result = await client.Jobs.RecordTelemetryBatchAsync(entries);

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.EndsWith("api/v1/jobs/telemetry/batch", req.RequestUri!.AbsolutePath);

        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        var arr = doc.RootElement.GetProperty("entries");
        Assert.Equal(2, arr.GetArrayLength());
        Assert.Equal("j-1", arr[0].GetProperty("jobId").GetString());
        Assert.Equal("2026-04-02T08:15:00Z", arr[0].GetProperty("actualStartTime").GetString());
        Assert.Equal("2026-04-02T08:42:00Z", arr[0].GetProperty("actualEndTime").GetString());
        Assert.Equal("WCN-ORD-1234", arr[1].GetProperty("externalId").GetString());

        Assert.Equal(2, result.Processed);
        Assert.Equal(2, result.Updated);
        Assert.Empty(result.NotFound);
    }

    [Fact]
    public async Task RecordTelemetryBatchAsync_ReturnsNotFoundAndErrors()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            processed = 3,
            updated = 1,
            notFound = new[] { "WCN-ORD-9999" },
            errors = new[] { new { @ref = "j-bad", message = "Invalid time range" } }
        });

        var entries = new List<TelemetryEntry>
        {
            new() { JobId = "j-1", ActualStartTime = "2026-04-02T08:15:00Z" },
            new() { ExternalId = "WCN-ORD-9999", ActualEndTime = "2026-04-02T09:00:00Z" },
            new() { JobId = "j-bad", ActualStartTime = "2026-04-02T10:00:00Z", ActualEndTime = "2026-04-02T09:00:00Z" }
        };

        var result = await client.Jobs.RecordTelemetryBatchAsync(entries);

        Assert.Equal(3, result.Processed);
        Assert.Equal(1, result.Updated);
        Assert.Single(result.NotFound);
        Assert.Equal("WCN-ORD-9999", result.NotFound[0]);
        var error = Assert.Single(result.Errors);
        Assert.Equal("j-bad", error.Ref);
        Assert.Equal("Invalid time range", error.Message);
    }
}
