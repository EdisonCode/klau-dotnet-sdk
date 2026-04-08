using System.Net;
using System.Text.Json;
using Klau.Sdk.Common;
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

    // --- JobsAsync - batchId deserialization ---

    [Fact]
    public async Task JobsAsync_ReturnsBatchId()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            success = true,
            batchId = "batch-abc-123",
            imported = 2,
            skipped = 0,
            errors = Array.Empty<object>(),
            customersCreated = 1,
            sitesCreated = 1
        });

        var request = new ImportJobsRequest
        {
            Jobs =
            [
                new ImportJobRecord { CustomerName = "A", SiteName = "S1", SiteAddress = "1 St", JobType = "DELIVERY", ContainerSize = "20" },
                new ImportJobRecord { CustomerName = "A", SiteName = "S2", SiteAddress = "2 St", JobType = "PICKUP", ContainerSize = "30" }
            ]
        };

        var result = await client.Import.JobsAsync(request);

        Assert.Equal("batch-abc-123", result.BatchId);
    }

    [Fact]
    public async Task JobsAsync_BatchIdNullWhenNotReturned()
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
        };

        var result = await client.Import.JobsAsync(request);

        Assert.Null(result.BatchId);
    }

    // --- GetReadinessAsync ---

    [Fact]
    public async Task GetReadinessAsync_SendsGetToCorrectPath()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-abc",
            sitesTotal = 5,
            sitesCached = 5,
            status = "ready",
            message = "All sites have cached drive times"
        });

        await client.Import.GetReadinessAsync("batch-abc");

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Get, req.Method);
        Assert.EndsWith("api/v1/import/batches/batch-abc/readiness", req.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetReadinessAsync_DeserializesReadyResponse()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-xyz",
            sitesTotal = 3,
            sitesCached = 3,
            status = "ready",
            message = "All 3 sites have cached drive times"
        });

        var result = await client.Import.GetReadinessAsync("batch-xyz");

        Assert.Equal("batch-xyz", result.BatchId);
        Assert.Equal(3, result.SitesTotal);
        Assert.Equal(3, result.SitesCached);
        Assert.Equal("ready", result.Status);
        Assert.Contains("3 sites", result.Message);
    }

    [Fact]
    public async Task GetReadinessAsync_DeserializesWarmingResponse()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-new",
            sitesTotal = 10,
            sitesCached = 4,
            status = "warming",
            message = "Cache warming in progress: 4/10 sites ready"
        });

        var result = await client.Import.GetReadinessAsync("batch-new");

        Assert.Equal("warming", result.Status);
        Assert.Equal(10, result.SitesTotal);
        Assert.Equal(4, result.SitesCached);
    }

    // --- ImportAndWaitAsync ---

    [Fact]
    public async Task ImportAndWaitAsync_ReturnsImmediatelyWhenNoBatchId()
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
        };

        var result = await client.Import.ImportAndWaitAsync(request);

        Assert.True(result.Success);
        Assert.Single(handler.SentRequests); // Only the import request, no readiness poll
    }

    [Fact]
    public async Task ImportAndWaitAsync_PollsUntilReady()
    {
        var (client, handler) = CreateClient();

        // 1. Import response with batchId
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            success = true,
            batchId = "batch-poll",
            imported = 2,
            skipped = 0,
            errors = Array.Empty<object>(),
            customersCreated = 1,
            sitesCreated = 2
        });

        // 2. First readiness poll — warming
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-poll",
            sitesTotal = 2,
            sitesCached = 0,
            status = "warming",
            message = "Cache warming in progress"
        });

        // 3. Second readiness poll — ready
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-poll",
            sitesTotal = 2,
            sitesCached = 2,
            status = "ready",
            message = "All sites ready"
        });

        var request = new ImportJobsRequest
        {
            Jobs =
            [
                new ImportJobRecord { CustomerName = "A", SiteName = "S1", SiteAddress = "1 St", JobType = "DELIVERY", ContainerSize = "20" },
                new ImportJobRecord { CustomerName = "A", SiteName = "S2", SiteAddress = "2 St", JobType = "PICKUP", ContainerSize = "30" }
            ]
        };

        var result = await client.Import.ImportAndWaitAsync(request, pollInterval: TimeSpan.FromMilliseconds(10));

        Assert.True(result.Success);
        Assert.Equal("batch-poll", result.BatchId);
        Assert.Equal(3, handler.SentRequests.Count); // import + 2 readiness polls
        Assert.EndsWith("api/v1/import/batches/batch-poll/readiness", handler.SentRequests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ImportAndWaitAsync_ReturnsImmediatelyForNotApplicable()
    {
        var (client, handler) = CreateClient();

        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            success = true,
            batchId = "batch-na",
            imported = 1,
            skipped = 0,
            errors = Array.Empty<object>(),
            customersCreated = 0,
            sitesCreated = 0
        });

        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-na",
            sitesTotal = 0,
            sitesCached = 0,
            status = "not_applicable",
            message = "No new sites to warm"
        });

        var request = new ImportJobsRequest
        {
            Jobs =
            [
                new ImportJobRecord { CustomerName = "A", SiteName = "S", SiteAddress = "1 St", JobType = "DELIVERY", ContainerSize = "20" }
            ]
        };

        var result = await client.Import.ImportAndWaitAsync(request, pollInterval: TimeSpan.FromMilliseconds(10));

        Assert.True(result.Success);
        Assert.Equal(2, handler.SentRequests.Count); // import + 1 readiness check
    }

    [Fact]
    public async Task ImportAndWaitAsync_ThrowsOnTimeout()
    {
        var (client, handler) = CreateClient();

        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            success = true,
            batchId = "batch-slow",
            imported = 1,
            skipped = 0,
            errors = Array.Empty<object>(),
            customersCreated = 0,
            sitesCreated = 1
        });

        // Always return warming
        for (var i = 0; i < 20; i++)
        {
            handler.EnqueueResponse(HttpStatusCode.OK, new
            {
                batchId = "batch-slow",
                sitesTotal = 1,
                sitesCached = 0,
                status = "warming",
                message = "Still warming"
            });
        }

        var request = new ImportJobsRequest
        {
            Jobs =
            [
                new ImportJobRecord { CustomerName = "A", SiteName = "S", SiteAddress = "1 St", JobType = "DELIVERY", ContainerSize = "20" }
            ]
        };

        var ex = await Assert.ThrowsAsync<TimeoutException>(() =>
            client.Import.ImportAndWaitAsync(
                request,
                timeout: TimeSpan.FromMilliseconds(100),
                pollInterval: TimeSpan.FromMilliseconds(10)));

        Assert.Contains("batch-slow", ex.Message);
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

    [Fact]
    public async Task GetReadinessAsync_TenantScope_SendsTenantHeader()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-t",
            sitesTotal = 1,
            sitesCached = 1,
            status = "ready",
            message = "Ready"
        });

        var scope = client.ForTenant("tenant-456");
        await scope.Import.GetReadinessAsync("batch-t");

        var req = Assert.Single(handler.SentRequests);
        Assert.True(req.Headers.Contains("Klau-Tenant-Id"));
        Assert.Equal("tenant-456", req.Headers.GetValues("Klau-Tenant-Id").First());
    }

    // --- SubmitJobsAsync ---

    [Fact]
    public async Task SubmitJobsAsync_SendsPostToAsyncPath()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-async-1",
            jobCount = 3,
            status = "ACCEPTED"
        });

        var request = new ImportJobsRequest
        {
            Jobs =
            [
                new ImportJobRecord { CustomerName = "A", SiteName = "S", SiteAddress = "1 St", JobType = "DELIVERY", ContainerSize = "20" },
                new ImportJobRecord { CustomerName = "B", SiteName = "S2", SiteAddress = "2 St", JobType = "PICKUP", ContainerSize = "30" },
                new ImportJobRecord { CustomerName = "C", SiteName = "S3", SiteAddress = "3 St", JobType = "SWAP", ContainerSize = "40" }
            ]
        };

        var result = await client.Import.SubmitJobsAsync(request);

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.EndsWith("api/v1/import/jobs/async", req.RequestUri!.AbsolutePath);
        Assert.Equal("batch-async-1", result.BatchId);
        Assert.Equal(3, result.JobCount);
        Assert.Equal(ImportBatchStatus.ACCEPTED, result.Status);
    }

    [Fact]
    public async Task SubmitJobsAsync_SerializesRequestBody()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-body",
            jobCount = 1,
            status = "ACCEPTED"
        });

        var request = new ImportJobsRequest
        {
            Jobs =
            [
                new ImportJobRecord
                {
                    CustomerName = "Acme Corp",
                    SiteName = "HQ",
                    SiteAddress = "100 Main St",
                    SiteCity = "Portland",
                    SiteState = "OR",
                    SiteZip = "97201",
                    JobType = "DELIVERY",
                    ContainerSize = "20",
                    TimeWindow = "MORNING",
                    Priority = "HIGH",
                    Notes = "Ring bell",
                    RequestedDate = "2026-04-10",
                    ExternalId = "ERP-001"
                }
            ],
            CreateMissing = false
        };

        await client.Import.SubmitJobsAsync(request);

        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        Assert.False(root.GetProperty("createMissing").GetBoolean());

        var job = root.GetProperty("jobs")[0];
        Assert.Equal("Acme Corp", job.GetProperty("customerName").GetString());
        Assert.Equal("HQ", job.GetProperty("siteName").GetString());
        Assert.Equal("100 Main St", job.GetProperty("siteAddress").GetString());
        Assert.Equal("Portland", job.GetProperty("siteCity").GetString());
        Assert.Equal("OR", job.GetProperty("siteState").GetString());
        Assert.Equal("97201", job.GetProperty("siteZip").GetString());
        Assert.Equal("DELIVERY", job.GetProperty("jobType").GetString());
        Assert.Equal("20", job.GetProperty("containerSize").GetString());
        Assert.Equal("MORNING", job.GetProperty("timeWindow").GetString());
        Assert.Equal("HIGH", job.GetProperty("priority").GetString());
        Assert.Equal("Ring bell", job.GetProperty("notes").GetString());
        Assert.Equal("2026-04-10", job.GetProperty("requestedDate").GetString());
        Assert.Equal("ERP-001", job.GetProperty("externalId").GetString());
    }

    // --- GetBatchStatusAsync ---

    [Fact]
    public async Task GetBatchStatusAsync_SendsGetToCorrectPath()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-s1",
            status = "PROCESSING",
            total = 10,
            processed = 4,
            imported = 3,
            skipped = 1,
            customersCreated = 1,
            sitesCreated = 2,
            errors = Array.Empty<object>(),
            driveTimeCacheStatus = "NOT_STARTED"
        });

        await client.Import.GetBatchStatusAsync("batch-s1");

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Get, req.Method);
        Assert.EndsWith("api/v1/import/batches/batch-s1/status", req.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetBatchStatusAsync_DeserializesProcessingResponse()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-p",
            status = "PROCESSING",
            total = 100,
            processed = 42,
            imported = 40,
            skipped = 2,
            customersCreated = 5,
            sitesCreated = 8,
            errors = new[] { new { row = 3, field = "containerSize", message = "Invalid size" } },
            driveTimeCacheStatus = "NOT_STARTED"
        });

        var result = await client.Import.GetBatchStatusAsync("batch-p");

        Assert.Equal("batch-p", result.BatchId);
        Assert.Equal(ImportBatchStatus.PROCESSING, result.Status);
        Assert.Equal(100, result.Total);
        Assert.Equal(42, result.Processed);
        Assert.Equal(40, result.Imported);
        Assert.Equal(2, result.Skipped);
        Assert.Equal(5, result.CustomersCreated);
        Assert.Equal(8, result.SitesCreated);
        Assert.Single(result.Errors);
        Assert.Equal(3, result.Errors[0].Row);
        Assert.Equal(DriveTimeCacheStatus.NOT_STARTED, result.DriveTimeCacheStatus);
        Assert.False(result.IsTerminal);
        Assert.False(result.IsReadyForOptimization);
    }

    [Fact]
    public async Task GetBatchStatusAsync_DeserializesCompletedResponse()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-c",
            status = "COMPLETED",
            total = 50,
            processed = 50,
            imported = 50,
            skipped = 0,
            customersCreated = 3,
            sitesCreated = 10,
            errors = Array.Empty<object>(),
            driveTimeCacheStatus = "READY"
        });

        var result = await client.Import.GetBatchStatusAsync("batch-c");

        Assert.Equal(ImportBatchStatus.COMPLETED, result.Status);
        Assert.Equal(DriveTimeCacheStatus.READY, result.DriveTimeCacheStatus);
        Assert.True(result.IsTerminal);
        Assert.True(result.IsReadyForOptimization);
    }

    [Fact]
    public async Task GetBatchStatusAsync_PartialFailureIsTerminal()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-pf",
            status = "PARTIAL_FAILURE",
            total = 10,
            processed = 10,
            imported = 7,
            skipped = 3,
            customersCreated = 0,
            sitesCreated = 0,
            errors = new[]
            {
                new { row = 2, field = "customerName", message = "Required" },
                new { row = 5, field = "jobType", message = "Invalid type" },
                new { row = 9, field = "externalId", message = "Duplicate" }
            },
            driveTimeCacheStatus = "NOT_APPLICABLE"
        });

        var result = await client.Import.GetBatchStatusAsync("batch-pf");

        Assert.Equal(ImportBatchStatus.PARTIAL_FAILURE, result.Status);
        Assert.True(result.IsTerminal);
        Assert.True(result.IsReadyForOptimization);
        Assert.Equal(3, result.Errors.Count);
    }

    [Fact]
    public async Task GetBatchStatusAsync_CompletedButCacheWarmingNotReady()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-w",
            status = "COMPLETED",
            total = 5,
            processed = 5,
            imported = 5,
            skipped = 0,
            customersCreated = 2,
            sitesCreated = 3,
            errors = Array.Empty<object>(),
            driveTimeCacheStatus = "WARMING"
        });

        var result = await client.Import.GetBatchStatusAsync("batch-w");

        Assert.True(result.IsTerminal);
        Assert.False(result.IsReadyForOptimization);
    }

    // --- SubmitAndWaitAsync ---

    [Fact]
    public async Task SubmitAndWaitAsync_PollsUntilReadyForOptimization()
    {
        var (client, handler) = CreateClient();

        // 1. Submit response
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-sw",
            jobCount = 5,
            status = "ACCEPTED"
        });

        // 2. First poll — processing, cache not started
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-sw",
            status = "PROCESSING",
            total = 5, processed = 2, imported = 2, skipped = 0,
            customersCreated = 1, sitesCreated = 1,
            errors = Array.Empty<object>(),
            driveTimeCacheStatus = "NOT_STARTED"
        });

        // 3. Second poll — completed, cache warming
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-sw",
            status = "COMPLETED",
            total = 5, processed = 5, imported = 5, skipped = 0,
            customersCreated = 1, sitesCreated = 2,
            errors = Array.Empty<object>(),
            driveTimeCacheStatus = "WARMING"
        });

        // 4. Third poll — completed, cache ready
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-sw",
            status = "COMPLETED",
            total = 5, processed = 5, imported = 5, skipped = 0,
            customersCreated = 1, sitesCreated = 2,
            errors = Array.Empty<object>(),
            driveTimeCacheStatus = "READY"
        });

        var request = new ImportJobsRequest
        {
            Jobs =
            [
                new ImportJobRecord { CustomerName = "A", SiteName = "S1", SiteAddress = "1 St", JobType = "DELIVERY", ContainerSize = "20" }
            ]
        };

        var result = await client.Import.SubmitAndWaitAsync(request, pollInterval: TimeSpan.FromMilliseconds(10));

        Assert.Equal(ImportBatchStatus.COMPLETED, result.Status);
        Assert.Equal(DriveTimeCacheStatus.READY, result.DriveTimeCacheStatus);
        Assert.True(result.IsReadyForOptimization);
        Assert.Equal(4, handler.SentRequests.Count); // submit + 3 polls
        Assert.EndsWith("api/v1/import/jobs/async", handler.SentRequests[0].RequestUri!.AbsolutePath);
        Assert.EndsWith("api/v1/import/batches/batch-sw/status", handler.SentRequests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task SubmitAndWaitAsync_ReturnsImmediatelyWhenAlreadyReady()
    {
        var (client, handler) = CreateClient();

        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-fast",
            jobCount = 2,
            status = "ACCEPTED"
        });

        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-fast",
            status = "COMPLETED",
            total = 2, processed = 2, imported = 2, skipped = 0,
            customersCreated = 0, sitesCreated = 0,
            errors = Array.Empty<object>(),
            driveTimeCacheStatus = "NOT_APPLICABLE"
        });

        var request = new ImportJobsRequest
        {
            Jobs =
            [
                new ImportJobRecord { CustomerName = "A", SiteName = "S", SiteAddress = "1 St", JobType = "DELIVERY", ContainerSize = "20" }
            ]
        };

        var result = await client.Import.SubmitAndWaitAsync(request, pollInterval: TimeSpan.FromMilliseconds(10));

        Assert.True(result.IsReadyForOptimization);
        Assert.Equal(2, handler.SentRequests.Count); // submit + 1 poll
    }

    [Fact]
    public async Task SubmitAndWaitAsync_ThrowsOnTimeout()
    {
        var (client, handler) = CreateClient();

        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-slow-async",
            jobCount = 100,
            status = "ACCEPTED"
        });

        for (var i = 0; i < 20; i++)
        {
            handler.EnqueueResponse(HttpStatusCode.OK, new
            {
                batchId = "batch-slow-async",
                status = "PROCESSING",
                total = 100, processed = 10, imported = 10, skipped = 0,
                customersCreated = 0, sitesCreated = 0,
                errors = Array.Empty<object>(),
                driveTimeCacheStatus = "NOT_STARTED"
            });
        }

        var request = new ImportJobsRequest
        {
            Jobs =
            [
                new ImportJobRecord { CustomerName = "A", SiteName = "S", SiteAddress = "1 St", JobType = "DELIVERY", ContainerSize = "20" }
            ]
        };

        var ex = await Assert.ThrowsAsync<TimeoutException>(() =>
            client.Import.SubmitAndWaitAsync(
                request,
                timeout: TimeSpan.FromMilliseconds(100),
                pollInterval: TimeSpan.FromMilliseconds(10)));

        Assert.Contains("batch-slow-async", ex.Message);
    }

    // --- Async import - TenantScope ---

    [Fact]
    public async Task SubmitJobsAsync_TenantScope_SendsTenantHeader()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-t-async",
            jobCount = 1,
            status = "ACCEPTED"
        });

        var scope = client.ForTenant("tenant-789");
        var request = new ImportJobsRequest
        {
            Jobs =
            [
                new ImportJobRecord { CustomerName = "A", SiteName = "S", SiteAddress = "1 St", JobType = "DELIVERY", ContainerSize = "20" }
            ]
        };

        await scope.Import.SubmitJobsAsync(request);

        var req = Assert.Single(handler.SentRequests);
        Assert.True(req.Headers.Contains("Klau-Tenant-Id"));
        Assert.Equal("tenant-789", req.Headers.GetValues("Klau-Tenant-Id").First());
    }

    [Fact]
    public async Task GetBatchStatusAsync_TenantScope_SendsTenantHeader()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-t-status",
            status = "COMPLETED",
            total = 1, processed = 1, imported = 1, skipped = 0,
            customersCreated = 0, sitesCreated = 0,
            errors = Array.Empty<object>(),
            driveTimeCacheStatus = "NOT_APPLICABLE"
        });

        var scope = client.ForTenant("tenant-abc");
        await scope.Import.GetBatchStatusAsync("batch-t-status");

        var req = Assert.Single(handler.SentRequests);
        Assert.True(req.Headers.Contains("Klau-Tenant-Id"));
        Assert.Equal("tenant-abc", req.Headers.GetValues("Klau-Tenant-Id").First());
    }

    // --- SubmitAndWaitAsync - failure modes ---

    [Fact]
    public async Task SubmitAndWaitAsync_ReturnsImmediatelyOnFailedBatch()
    {
        var (client, handler) = CreateClient();

        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-fail",
            jobCount = 5,
            status = "ACCEPTED"
        });

        // First poll returns FAILED with cache NOT_STARTED
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-fail",
            status = "FAILED",
            total = 5, processed = 5, imported = 0, skipped = 5,
            customersCreated = 0, sitesCreated = 0,
            errors = new[]
            {
                new { row = 1, field = "jobType", message = "Invalid job type" }
            },
            driveTimeCacheStatus = "NOT_STARTED"
        });

        var request = new ImportJobsRequest
        {
            Jobs =
            [
                new ImportJobRecord { CustomerName = "A", SiteName = "S", SiteAddress = "1 St", JobType = "INVALID", ContainerSize = "20" }
            ]
        };

        var result = await client.Import.SubmitAndWaitAsync(request, pollInterval: TimeSpan.FromMilliseconds(10));

        // Returns immediately — does not hang for 120s
        Assert.Equal(ImportBatchStatus.FAILED, result.Status);
        Assert.True(result.IsTerminal);
        Assert.False(result.IsReadyForOptimization);
        Assert.Single(result.Errors);
        Assert.Equal(2, handler.SentRequests.Count); // submit + 1 poll (no more)
    }

    [Fact]
    public async Task SubmitAndWaitAsync_ReturnsOnPartialFailureWithCacheNotApplicable()
    {
        var (client, handler) = CreateClient();

        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-pf-na",
            jobCount = 3,
            status = "ACCEPTED"
        });

        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-pf-na",
            status = "PARTIAL_FAILURE",
            total = 3, processed = 3, imported = 1, skipped = 2,
            customersCreated = 0, sitesCreated = 0,
            errors = new[]
            {
                new { row = 2, field = "containerSize", message = "Invalid" },
                new { row = 3, field = "customerName", message = "Required" }
            },
            driveTimeCacheStatus = "NOT_APPLICABLE"
        });

        var request = new ImportJobsRequest
        {
            Jobs =
            [
                new ImportJobRecord { CustomerName = "A", SiteName = "S", SiteAddress = "1 St", JobType = "DELIVERY", ContainerSize = "20" }
            ]
        };

        var result = await client.Import.SubmitAndWaitAsync(request, pollInterval: TimeSpan.FromMilliseconds(10));

        // PARTIAL_FAILURE + NOT_APPLICABLE exits via IsReadyForOptimization
        Assert.Equal(ImportBatchStatus.PARTIAL_FAILURE, result.Status);
        Assert.True(result.IsReadyForOptimization);
    }

    [Fact]
    public async Task SubmitAndWaitAsync_PollingError_ThrowsKlauImportExceptionWithBatchId()
    {
        var (client, handler) = CreateClient();

        // Submit succeeds
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-err",
            jobCount = 1,
            status = "ACCEPTED"
        });

        // First poll returns 500
        handler.EnqueueResponse(HttpStatusCode.InternalServerError,
            new ApiErrorBody("INTERNAL_ERROR", "Something went wrong"));

        var request = new ImportJobsRequest
        {
            Jobs =
            [
                new ImportJobRecord { CustomerName = "A", SiteName = "S", SiteAddress = "1 St", JobType = "DELIVERY", ContainerSize = "20" }
            ]
        };

        var ex = await Assert.ThrowsAsync<KlauImportException>(() =>
            client.Import.SubmitAndWaitAsync(
                request,
                pollInterval: TimeSpan.FromMilliseconds(10)));

        Assert.Equal("batch-err", ex.BatchId);
        Assert.Null(ex.LastStatus); // failed on first poll
        Assert.Contains("batch-err", ex.Message);
        Assert.IsType<KlauApiException>(ex.InnerException);
    }

    [Fact]
    public async Task SubmitAndWaitAsync_PollingError_PreservesLastStatus()
    {
        var (client, handler) = CreateClient();

        // Submit succeeds
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-err2",
            jobCount = 5,
            status = "ACCEPTED"
        });

        // First poll succeeds (PROCESSING)
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-err2",
            status = "PROCESSING",
            total = 5, processed = 2, imported = 2, skipped = 0,
            customersCreated = 1, sitesCreated = 1,
            errors = Array.Empty<object>(),
            driveTimeCacheStatus = "NOT_STARTED"
        });

        // Second poll returns 500
        handler.EnqueueResponse(HttpStatusCode.InternalServerError,
            new ApiErrorBody("INTERNAL_ERROR", "Transient failure"));

        var request = new ImportJobsRequest
        {
            Jobs =
            [
                new ImportJobRecord { CustomerName = "A", SiteName = "S", SiteAddress = "1 St", JobType = "DELIVERY", ContainerSize = "20" }
            ]
        };

        var ex = await Assert.ThrowsAsync<KlauImportException>(() =>
            client.Import.SubmitAndWaitAsync(
                request,
                pollInterval: TimeSpan.FromMilliseconds(10)));

        Assert.Equal("batch-err2", ex.BatchId);
        Assert.NotNull(ex.LastStatus);
        Assert.Equal(ImportBatchStatus.PROCESSING, ex.LastStatus!.Status);
        Assert.Equal(2, ex.LastStatus.Processed);
    }

    [Fact]
    public async Task SubmitAndWaitAsync_CancellationToken_ExitsPromptly()
    {
        var (client, handler) = CreateClient();

        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-cancel",
            jobCount = 1,
            status = "ACCEPTED"
        });

        // Enqueue many PROCESSING responses (we should never reach them all)
        for (var i = 0; i < 50; i++)
        {
            handler.EnqueueResponse(HttpStatusCode.OK, new
            {
                batchId = "batch-cancel",
                status = "PROCESSING",
                total = 1, processed = 0, imported = 0, skipped = 0,
                customersCreated = 0, sitesCreated = 0,
                errors = Array.Empty<object>(),
                driveTimeCacheStatus = "NOT_STARTED"
            });
        }

        var request = new ImportJobsRequest
        {
            Jobs =
            [
                new ImportJobRecord { CustomerName = "A", SiteName = "S", SiteAddress = "1 St", JobType = "DELIVERY", ContainerSize = "20" }
            ]
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.Import.SubmitAndWaitAsync(
                request,
                timeout: TimeSpan.FromSeconds(60),
                pollInterval: TimeSpan.FromMilliseconds(10),
                ct: cts.Token));

        // Should have exited well before consuming all 50 responses
        Assert.True(handler.SentRequests.Count < 50);
    }

    // --- SubmitJobsAsync - KlauRequestOptions ---

    [Fact]
    public async Task SubmitJobsAsync_WithIdempotencyKey_SendsHeader()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-idem",
            jobCount = 1,
            status = "ACCEPTED"
        });

        var request = new ImportJobsRequest
        {
            Jobs =
            [
                new ImportJobRecord { CustomerName = "A", SiteName = "S", SiteAddress = "1 St", JobType = "DELIVERY", ContainerSize = "20" }
            ]
        };

        await client.Import.SubmitJobsAsync(request, new KlauRequestOptions
        {
            IdempotencyKey = "erp-batch-2026-04-06"
        });

        var req = Assert.Single(handler.SentRequests);
        Assert.True(req.Headers.Contains("Idempotency-Key"));
        Assert.Equal("erp-batch-2026-04-06", req.Headers.GetValues("Idempotency-Key").First());
    }

    // --- Pre-routing fields (CLI data pipeline) ---

    [Fact]
    public async Task SubmitJobsAsync_SerializesPreRoutingFields()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-pr",
            jobCount = 1,
            status = "ACCEPTED"
        });

        var request = new ImportJobsRequest
        {
            Jobs =
            [
                new ImportJobRecord
                {
                    CustomerName = "Acme Corp",
                    SiteName = "Site 1",
                    SiteAddress = "1 Main St",
                    JobType = "DELIVERY",
                    ContainerSize = "20",
                    AssignedDriverExternalId = "463",
                    AssignedTruckNumber = "T-100",
                    Sequence = 1,
                    EstimatedStartTime = "2026-04-07T07:15:00"
                }
            ]
        };

        await client.Import.SubmitJobsAsync(request);

        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        var job = doc.RootElement.GetProperty("jobs")[0];

        Assert.Equal("463", job.GetProperty("assignedDriverExternalId").GetString());
        Assert.Equal("T-100", job.GetProperty("assignedTruckNumber").GetString());
        Assert.Equal(1, job.GetProperty("sequence").GetInt32());
        Assert.Equal("2026-04-07T07:15:00", job.GetProperty("estimatedStartTime").GetString());
    }

    [Fact]
    public async Task SubmitJobsAsync_OmitsPreRoutingFieldsWhenNull()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-nopr",
            jobCount = 1,
            status = "ACCEPTED"
        });

        var request = new ImportJobsRequest
        {
            Jobs =
            [
                new ImportJobRecord
                {
                    CustomerName = "Acme",
                    SiteName = "S",
                    SiteAddress = "1 St",
                    JobType = "DELIVERY",
                    ContainerSize = "20"
                }
            ]
        };

        await client.Import.SubmitJobsAsync(request);

        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        var job = doc.RootElement.GetProperty("jobs")[0];

        // Pre-routing fields must not appear when unset — the worker uses their
        // absence to stay on the non-pre-routed code path.
        Assert.False(job.TryGetProperty("assignedDriverExternalId", out _));
        Assert.False(job.TryGetProperty("assignedTruckNumber", out _));
        Assert.False(job.TryGetProperty("sequence", out _));
        Assert.False(job.TryGetProperty("estimatedStartTime", out _));
    }

    [Fact]
    public async Task GetBatchStatusAsync_DeserializesPreRoutingMatchFailures()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-pr-status",
            status = "COMPLETED",
            total = 142,
            processed = 142,
            imported = 142,
            skipped = 2,
            customersCreated = 0,
            sitesCreated = 0,
            errors = new object[]
            {
                new
                {
                    row = 4,
                    field = "assignedDriverExternalId",
                    message = "Unknown driver externalId \"463\" — job imported unassigned",
                    code = "DRIVER_MATCH_FAILED",
                    meta = new { externalId = "463" }
                },
                new
                {
                    row = 7,
                    field = "assignedTruckNumber",
                    message = "Unknown truck \"T-999\"",
                    code = "TRUCK_MATCH_FAILED",
                    meta = new { truckNumber = "T-999" }
                }
            },
            driveTimeCacheStatus = "READY",
            preRouted = true,
            driverMatchFailures = new[]
            {
                new { externalId = "463", rowCount = 12 }
            },
            truckMatchFailures = new[]
            {
                new { truckNumber = "T-999", rowCount = 1 }
            }
        });

        var result = await client.Import.GetBatchStatusAsync("batch-pr-status");

        Assert.True(result.PreRouted);
        Assert.True(result.IsReadyForOptimization);

        var driverFailure = Assert.Single(result.DriverMatchFailures);
        Assert.Equal("463", driverFailure.ExternalId);
        Assert.Equal(12, driverFailure.RowCount);

        var truckFailure = Assert.Single(result.TruckMatchFailures);
        Assert.Equal("T-999", truckFailure.TruckNumber);
        Assert.Equal(1, truckFailure.RowCount);

        Assert.Equal(2, result.Errors.Count);

        var driverErr = result.Errors[0];
        Assert.Equal("DRIVER_MATCH_FAILED", driverErr.Code);
        Assert.NotNull(driverErr.Meta);
        Assert.Equal("463", driverErr.Meta!["externalId"]);

        var truckErr = result.Errors[1];
        Assert.Equal("TRUCK_MATCH_FAILED", truckErr.Code);
        Assert.Equal("T-999", truckErr.Meta!["truckNumber"]);
    }

    [Fact]
    public async Task GetBatchStatusAsync_DefaultsPreRoutingFieldsWhenAbsent()
    {
        // Legacy (non-pre-routed) batches must still deserialize cleanly.
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            batchId = "batch-legacy",
            status = "COMPLETED",
            total = 5,
            processed = 5,
            imported = 5,
            skipped = 0,
            customersCreated = 1,
            sitesCreated = 2,
            errors = Array.Empty<object>(),
            driveTimeCacheStatus = "READY"
        });

        var result = await client.Import.GetBatchStatusAsync("batch-legacy");

        Assert.False(result.PreRouted);
        Assert.Empty(result.DriverMatchFailures);
        Assert.Empty(result.TruckMatchFailures);
    }
}
