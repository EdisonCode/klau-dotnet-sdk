using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Klau.Sdk.Common;
using Klau.Sdk.Jobs;
using Klau.Sdk.Tests.Helpers;

namespace Klau.Sdk.Tests;

public class SerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper) }
    };

    private static (KlauClient client, MockHttpHandler handler) CreateClient()
    {
        var handler = new MockHttpHandler();
        var httpClient = new HttpClient(handler);
        var client = new KlauClient("kl_live_test", "https://api.test.com", httpClient);
        return (client, handler);
    }

    [Fact]
    public async Task Job_DeserializesAllFields()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            id = "j-123",
            type = "DUMP_RETURN",
            status = "IN_PROGRESS",
            customerName = "Acme Corp",
            customerId = "cust-1",
            siteId = "site-1",
            siteAddress = "123 Main St",
            containerSize = 30,
            containerNumber = "C-42",
            scheduledDate = "2026-03-13",
            requestedDate = "2026-03-12",
            timeWindow = "MORNING",
            priority = "HIGH",
            driverId = "drv-1",
            driverName = "Mike",
            truckId = "trk-1",
            sequence = 3,
            estimatedStartTime = "09:15",
            estimatedMinutes = 60,
            notes = "Use side gate",
            externalId = "ext-456",
            orderId = "ord-789",
            dumpSiteId = "dump-1",
            createdAt = "2026-01-15T10:30:00Z",
            updatedAt = "2026-03-13T08:00:00Z"
        });

        var job = await client.Jobs.GetAsync("j-123");

        Assert.Equal("j-123", job.Id);
        Assert.Equal(JobType.DUMP_RETURN, job.Type);
        Assert.Equal(JobStatus.IN_PROGRESS, job.Status);
        Assert.Equal("Acme Corp", job.CustomerName);
        Assert.Equal("cust-1", job.CustomerId);
        Assert.Equal("site-1", job.SiteId);
        Assert.Equal("123 Main St", job.SiteAddress);
        Assert.Equal(30, job.ContainerSize);
        Assert.Equal("C-42", job.ContainerNumber);
        Assert.Equal("2026-03-13", job.ScheduledDate);
        Assert.Equal("2026-03-12", job.RequestedDate);
        Assert.Equal(TimeWindow.MORNING, job.TimeWindow);
        Assert.Equal(JobPriority.HIGH, job.Priority);
        Assert.Equal("drv-1", job.DriverId);
        Assert.Equal("Mike", job.DriverName);
        Assert.Equal("trk-1", job.TruckId);
        Assert.Equal(3, job.Sequence);
        Assert.Equal("09:15", job.EstimatedStartTime);
        Assert.Equal(60, job.EstimatedMinutes);
        Assert.Equal("Use side gate", job.Notes);
        Assert.Equal("ext-456", job.ExternalId);
        Assert.Equal("ord-789", job.OrderId);
        Assert.Equal("dump-1", job.DumpSiteId);
    }

    [Fact]
    public async Task CreateJobRequest_SerializesWithCamelCase()
    {
        var (client, handler) = CreateClient();
        // API returns { data: { jobId: "j-1" } }
        handler.EnqueueResponse(HttpStatusCode.OK, new { jobId = "j-1" });

        await client.Jobs.CreateAsync(new CreateJobRequest
        {
            CustomerId = "cust-1",
            SiteId = "site-1",
            SiteAddress = "456 Oak Ave",
            Type = JobType.DELIVERY,
            ContainerSize = 20,
            RequestedDate = "2026-03-15",
            DumpSiteId = "dump-1",
            ContainerNumber = "C-100"
        });

        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        // Verify camelCase property names
        Assert.True(root.TryGetProperty("customerId", out _));
        Assert.True(root.TryGetProperty("siteAddress", out _));
        Assert.True(root.TryGetProperty("containerSize", out _));
        Assert.True(root.TryGetProperty("requestedDate", out _));
        Assert.True(root.TryGetProperty("dumpSiteId", out _));
        Assert.True(root.TryGetProperty("containerNumber", out _));

        // Verify PascalCase is NOT used
        Assert.False(root.TryGetProperty("CustomerId", out _));
        Assert.False(root.TryGetProperty("SiteAddress", out _));
        Assert.False(root.TryGetProperty("ContainerSize", out _));
    }

    [Fact]
    public async Task NullOptionalFields_OmittedFromJson()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new { jobId = "j-1" });

        await client.Jobs.CreateAsync(new CreateJobRequest
        {
            CustomerId = "cust-1",
            SiteId = "site-1",
            Type = JobType.DELIVERY,
            RequestedDate = "2026-03-14"
            // All other optional fields left null
        });

        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        // Required fields should be present
        Assert.True(root.TryGetProperty("customerId", out _));
        Assert.True(root.TryGetProperty("siteId", out _));
        Assert.True(root.TryGetProperty("type", out _));

        // Null optional fields should be omitted
        Assert.False(root.TryGetProperty("notes", out _));
        Assert.False(root.TryGetProperty("externalId", out _));
        Assert.False(root.TryGetProperty("timeWindow", out _));
        Assert.False(root.TryGetProperty("priority", out _));
        Assert.False(root.TryGetProperty("dumpSiteId", out _));
        Assert.False(root.TryGetProperty("containerNumber", out _));
        Assert.False(root.TryGetProperty("orderId", out _));
        Assert.False(root.TryGetProperty("estimatedMinutes", out _));
    }

    [Fact]
    public async Task Enums_SerializeAsSnakeCaseUpperStrings()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new { jobId = "j-1" });

        await client.Jobs.CreateAsync(new CreateJobRequest
        {
            CustomerId = "cust-1",
            SiteId = "site-1",
            Type = JobType.DUMP_RETURN,
            RequestedDate = "2026-03-14",
            Priority = JobPriority.URGENT,
            TimeWindow = TimeWindow.AFTERNOON
        });

        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        Assert.Equal("DUMP_RETURN", root.GetProperty("type").GetString());
        Assert.Equal("URGENT", root.GetProperty("priority").GetString());
        Assert.Equal("AFTERNOON", root.GetProperty("timeWindow").GetString());
    }

    [Fact]
    public async Task ExternalId_SerializedInCreateRequest()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new { jobId = "j-1" });

        await client.Jobs.CreateAsync(new CreateJobRequest
        {
            CustomerId = "cust-1",
            SiteId = "site-1",
            Type = JobType.DELIVERY,
            RequestedDate = "2026-03-14",
            ExternalId = "erp-wo-7890"
        });

        // Verify it was sent in the request
        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("erp-wo-7890", doc.RootElement.GetProperty("externalId").GetString());
    }

    [Fact]
    public async Task ExternalId_DeserializedInGetResponse()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new { id = "j-1", type = "DELIVERY", status = "UNASSIGNED",
            customerName = "Test", externalId = "erp-wo-7890",
            createdAt = "2026-01-01T00:00:00Z", updatedAt = "2026-01-01T00:00:00Z" });

        var job = await client.Jobs.GetAsync("j-1");
        Assert.Equal("erp-wo-7890", job.ExternalId);
    }

    [Fact]
    public void JobType_AllValues_SerializeCorrectly()
    {
        // Verify all JobType enum values serialize to expected strings
        var expectedMappings = new Dictionary<JobType, string>
        {
            { JobType.DELIVERY, "DELIVERY" },
            { JobType.PICKUP, "PICKUP" },
            { JobType.DUMP_RETURN, "DUMP_RETURN" },
            { JobType.SWAP, "SWAP" },
            { JobType.INTERNAL_DUMP, "INTERNAL_DUMP" },
            { JobType.SERVICE_VISIT, "SERVICE_VISIT" }
        };

        foreach (var (enumVal, expected) in expectedMappings)
        {
            var json = JsonSerializer.Serialize(enumVal, JsonOptions);
            Assert.Equal($"\"{expected}\"", json);
        }
    }

    [Fact]
    public void JobStatus_AllValues_SerializeCorrectly()
    {
        var expectedMappings = new Dictionary<JobStatus, string>
        {
            { JobStatus.UNASSIGNED, "UNASSIGNED" },
            { JobStatus.ASSIGNED, "ASSIGNED" },
            { JobStatus.IN_PROGRESS, "IN_PROGRESS" },
            { JobStatus.COMPLETED, "COMPLETED" },
            { JobStatus.CANCELLED, "CANCELLED" }
        };

        foreach (var (enumVal, expected) in expectedMappings)
        {
            var json = JsonSerializer.Serialize(enumVal, JsonOptions);
            Assert.Equal($"\"{expected}\"", json);
        }
    }

    [Fact]
    public void ContainerSlot_AllValues_SerializeCorrectly()
    {
        var expectedMappings = new Dictionary<ContainerSlot, string>
        {
            { ContainerSlot.PRIMARY, "PRIMARY" },
            { ContainerSlot.SECONDARY, "SECONDARY" }
        };

        foreach (var (enumVal, expected) in expectedMappings)
        {
            var json = JsonSerializer.Serialize(enumVal, JsonOptions);
            Assert.Equal($"\"{expected}\"", json);
        }
    }

    [Fact]
    public async Task UpdateJobRequest_OnlySerializesSetFields()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new { id = "j-1", type = "DELIVERY", status = "UNASSIGNED",
            customerName = "Test", containerSize = 30,
            createdAt = "2026-01-01T00:00:00Z", updatedAt = "2026-01-01T00:00:00Z" });

        await client.Jobs.UpdateAsync("j-1", new UpdateJobRequest
        {
            ContainerSize = 30
        });

        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("containerSize", out _));
        // Null fields should be omitted
        Assert.False(root.TryGetProperty("notes", out _));
        Assert.False(root.TryGetProperty("priority", out _));
        Assert.False(root.TryGetProperty("requestedDate", out _));
    }
}
