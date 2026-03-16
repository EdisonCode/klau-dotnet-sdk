using System.Net;
using System.Text.Json;
using Klau.Sdk.Import;
using Klau.Sdk.Tests.Helpers;

namespace Klau.Sdk.Tests;

public class ImportClientTests
{
    private static (KlauClient client, MockHttpHandler handler) CreateClient()
    {
        var handler = new MockHttpHandler();
        var httpClient = new HttpClient(handler);
        var client = new KlauClient("kl_live_test", "https://api.test.com", httpClient);
        return (client, handler);
    }

    // --- JobsAsync - Basic request ---

    [Fact]
    public async Task JobsAsync_SendsPostToCorrectPath()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            success = true,
            imported = 1,
            skipped = 0,
            errors = Array.Empty<object>(),
            customersCreated = 0,
            sitesCreated = 0
        });

        var request = new ImportJobsRequest
        {
            Jobs =
            [
                new ImportJobRecord
                {
                    CustomerName = "Acme Corp",
                    SiteName = "Main Site",
                    SiteAddress = "123 Main St",
                    JobType = "DELIVERY",
                    ContainerSize = "20"
                }
            ]
        };

        await client.Import.JobsAsync(request);

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.EndsWith("api/v1/import/jobs", req.RequestUri!.AbsolutePath);
    }

    // --- JobsAsync - Request body serialization ---

    [Fact]
    public async Task JobsAsync_SerializesAllFields()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            success = true,
            imported = 1,
            skipped = 0,
            errors = Array.Empty<object>(),
            customersCreated = 1,
            sitesCreated = 1
        });

        var request = new ImportJobsRequest
        {
            Jobs =
            [
                new ImportJobRecord
                {
                    CustomerName = "Acme Corp",
                    SiteName = "Warehouse",
                    SiteAddress = "456 Industrial Way",
                    SiteCity = "San Luis Obispo",
                    SiteState = "CA",
                    SiteZip = "93401",
                    JobType = "PICKUP",
                    ContainerSize = "30",
                    TimeWindow = "MORNING",
                    Priority = "HIGH",
                    Notes = "Gate code: 1234",
                    RequestedDate = "2026-03-20",
                    ExternalId = "WO-99001"
                }
            ],
            CreateMissing = true
        };

        await client.Import.JobsAsync(request);

        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        // Verify createMissing
        Assert.True(root.GetProperty("createMissing").GetBoolean());

        // Verify jobs array
        var jobs = root.GetProperty("jobs");
        Assert.Equal(1, jobs.GetArrayLength());

        var job = jobs[0];
        Assert.Equal("Acme Corp", job.GetProperty("customerName").GetString());
        Assert.Equal("Warehouse", job.GetProperty("siteName").GetString());
        Assert.Equal("456 Industrial Way", job.GetProperty("siteAddress").GetString());
        Assert.Equal("San Luis Obispo", job.GetProperty("siteCity").GetString());
        Assert.Equal("CA", job.GetProperty("siteState").GetString());
        Assert.Equal("93401", job.GetProperty("siteZip").GetString());
        Assert.Equal("PICKUP", job.GetProperty("jobType").GetString());
        Assert.Equal("30", job.GetProperty("containerSize").GetString());
        Assert.Equal("MORNING", job.GetProperty("timeWindow").GetString());
        Assert.Equal("HIGH", job.GetProperty("priority").GetString());
        Assert.Equal("Gate code: 1234", job.GetProperty("notes").GetString());
        Assert.Equal("2026-03-20", job.GetProperty("requestedDate").GetString());
        Assert.Equal("WO-99001", job.GetProperty("externalId").GetString());
    }

    [Fact]
    public async Task JobsAsync_OmitsNullOptionalFields()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            success = true,
            imported = 1,
            skipped = 0,
            errors = Array.Empty<object>(),
            customersCreated = 0,
            sitesCreated = 0
        });

        var request = new ImportJobsRequest
        {
            Jobs =
            [
                new ImportJobRecord
                {
                    CustomerName = "Acme Corp",
                    SiteName = "Main Site",
                    SiteAddress = "123 Main St",
                    JobType = "DELIVERY",
                    ContainerSize = "20"
                }
            ]
        };

        await client.Import.JobsAsync(request);

        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        var job = doc.RootElement.GetProperty("jobs")[0];

        // Optional fields should not be present when null
        Assert.False(job.TryGetProperty("siteCity", out _));
        Assert.False(job.TryGetProperty("siteState", out _));
        Assert.False(job.TryGetProperty("siteZip", out _));
        Assert.False(job.TryGetProperty("timeWindow", out _));
        Assert.False(job.TryGetProperty("priority", out _));
        Assert.False(job.TryGetProperty("notes", out _));
        Assert.False(job.TryGetProperty("requestedDate", out _));
        Assert.False(job.TryGetProperty("externalId", out _));
    }

    // --- JobsAsync - Response deserialization ---

    [Fact]
    public async Task JobsAsync_ReturnsSuccessResult()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            success = true,
            imported = 3,
            skipped = 0,
            errors = Array.Empty<object>(),
            customersCreated = 1,
            sitesCreated = 2
        });

        var request = new ImportJobsRequest
        {
            Jobs =
            [
                new ImportJobRecord { CustomerName = "A", SiteName = "S1", SiteAddress = "1 St", JobType = "DELIVERY", ContainerSize = "20" },
                new ImportJobRecord { CustomerName = "A", SiteName = "S2", SiteAddress = "2 St", JobType = "PICKUP", ContainerSize = "30" },
                new ImportJobRecord { CustomerName = "B", SiteName = "S3", SiteAddress = "3 St", JobType = "SWAP", ContainerSize = "40" }
            ]
        };

        var result = await client.Import.JobsAsync(request);

        Assert.True(result.Success);
        Assert.Equal(3, result.Imported);
        Assert.Equal(0, result.Skipped);
        Assert.Empty(result.Errors);
        Assert.Equal(1, result.CustomersCreated);
        Assert.Equal(2, result.SitesCreated);
    }

    [Fact]
    public async Task JobsAsync_ReturnsPartialErrors()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            success = false,
            imported = 1,
            skipped = 2,
            errors = new[]
            {
                new { row = 2, field = "containerSize", message = "Invalid container size \"99\". Must be one of: 10, 15, 20, 30, 40" },
                new { row = 3, field = "customerName", message = "Customer name is required" }
            },
            customersCreated = 1,
            sitesCreated = 1
        });

        var request = new ImportJobsRequest
        {
            Jobs =
            [
                new ImportJobRecord { CustomerName = "Good", SiteName = "S1", SiteAddress = "1 St", JobType = "DELIVERY", ContainerSize = "20" },
                new ImportJobRecord { CustomerName = "Bad Size", SiteName = "S2", SiteAddress = "2 St", JobType = "DELIVERY", ContainerSize = "99" },
                new ImportJobRecord { CustomerName = "", SiteName = "S3", SiteAddress = "3 St", JobType = "DELIVERY", ContainerSize = "20" }
            ]
        };

        var result = await client.Import.JobsAsync(request);

        Assert.False(result.Success);
        Assert.Equal(1, result.Imported);
        Assert.Equal(2, result.Skipped);
        Assert.Equal(2, result.Errors.Count);

        Assert.Equal(2, result.Errors[0].Row);
        Assert.Equal("containerSize", result.Errors[0].Field);
        Assert.Contains("99", result.Errors[0].Message);

        Assert.Equal(3, result.Errors[1].Row);
        Assert.Equal("customerName", result.Errors[1].Field);
    }

    // --- JobsAsync - CreateMissing flag ---

    [Fact]
    public async Task JobsAsync_CreateMissingDefaultsToTrue()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            success = true,
            imported = 1,
            skipped = 0,
            errors = Array.Empty<object>(),
            customersCreated = 0,
            sitesCreated = 0
        });

        var request = new ImportJobsRequest
        {
            Jobs =
            [
                new ImportJobRecord { CustomerName = "A", SiteName = "S", SiteAddress = "1 St", JobType = "DELIVERY", ContainerSize = "20" }
            ]
            // CreateMissing not specified — should default to true
        };

        await client.Import.JobsAsync(request);

        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.GetProperty("createMissing").GetBoolean());
    }

    [Fact]
    public async Task JobsAsync_CreateMissingFalse()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            success = false,
            imported = 0,
            skipped = 1,
            errors = new[]
            {
                new { row = 1, field = "customerName", message = "Customer \"Unknown\" not found. Enable createMissing to auto-create." }
            },
            customersCreated = 0,
            sitesCreated = 0
        });

        var request = new ImportJobsRequest
        {
            Jobs =
            [
                new ImportJobRecord { CustomerName = "Unknown", SiteName = "S", SiteAddress = "1 St", JobType = "DELIVERY", ContainerSize = "20" }
            ],
            CreateMissing = false
        };

        var result = await client.Import.JobsAsync(request);

        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        Assert.False(doc.RootElement.GetProperty("createMissing").GetBoolean());

        Assert.False(result.Success);
        Assert.Equal(0, result.Imported);
        Assert.Equal(1, result.Skipped);
    }

    // --- JobsAsync - Multiple jobs in a single batch ---

    [Fact]
    public async Task JobsAsync_SerializesMultipleJobs()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            success = true,
            imported = 2,
            skipped = 0,
            errors = Array.Empty<object>(),
            customersCreated = 0,
            sitesCreated = 0
        });

        var request = new ImportJobsRequest
        {
            Jobs =
            [
                new ImportJobRecord { CustomerName = "A", SiteName = "S1", SiteAddress = "1 St", JobType = "DELIVERY", ContainerSize = "20", ExternalId = "ext-1" },
                new ImportJobRecord { CustomerName = "B", SiteName = "S2", SiteAddress = "2 St", JobType = "SWAP", ContainerSize = "40", ExternalId = "ext-2" }
            ]
        };

        await client.Import.JobsAsync(request);

        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        var jobs = doc.RootElement.GetProperty("jobs");
        Assert.Equal(2, jobs.GetArrayLength());
        Assert.Equal("ext-1", jobs[0].GetProperty("externalId").GetString());
        Assert.Equal("ext-2", jobs[1].GetProperty("externalId").GetString());
    }

    // --- TenantScope ---

    [Fact]
    public async Task JobsAsync_TenantScope_SendsTenantHeader()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            success = true,
            imported = 1,
            skipped = 0,
            errors = Array.Empty<object>(),
            customersCreated = 0,
            sitesCreated = 0
        });

        var scope = client.ForTenant("tenant-123");
        var request = new ImportJobsRequest
        {
            Jobs =
            [
                new ImportJobRecord { CustomerName = "A", SiteName = "S", SiteAddress = "1 St", JobType = "DELIVERY", ContainerSize = "20" }
            ]
        };

        await scope.Import.JobsAsync(request);

        var req = Assert.Single(handler.SentRequests);
        Assert.True(req.Headers.Contains("Klau-Tenant-Id"));
        Assert.Equal("tenant-123", req.Headers.GetValues("Klau-Tenant-Id").First());
    }
}
