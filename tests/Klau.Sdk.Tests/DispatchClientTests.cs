using System.Net;
using System.Text.Json;
using Klau.Sdk.Common;
using Klau.Sdk.Dispatches;
using Klau.Sdk.Tests.Helpers;

namespace Klau.Sdk.Tests;

public class DispatchClientTests
{
    private static (KlauClient client, MockHttpHandler handler) CreateClient()
    {
        var handler = new MockHttpHandler();
        var httpClient = new HttpClient(handler);
        var client = new KlauClient("kl_live_test", "https://api.test.com", httpClient);
        return (client, handler);
    }

    // --- GetBoardAsync - DispatchBoardJob drive-time fields ---

    [Fact]
    public async Task GetBoardAsync_DeserializesDriveTimeFields()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            date = "2026-03-16",
            drivers = new[]
            {
                new
                {
                    id = "drv-1",
                    name = "John",
                    jobs = new[]
                    {
                        new
                        {
                            id = "job-1",
                            type = "DELIVERY",
                            status = "ASSIGNED",
                            customerName = "Acme",
                            estimatedMinutes = 18,
                            baselineMinutes = 45,
                            driveToMinutes = 12.5,
                            driveToMiles = 8.3,
                            driveTimeSource = "routing_engine",
                            estimatedStartTime = "2026-03-16T08:30:00Z",
                            createdAt = "2026-03-15T10:00:00Z",
                            updatedAt = "2026-03-16T06:00:00Z"
                        }
                    },
                    totalDriveMinutes = 45,
                    totalServiceMinutes = 120,
                    totalBufferMinutes = 15,
                    score = 85
                }
            },
            unassignedJobs = Array.Empty<object>(),
            metrics = new
            {
                totalJobs = 1,
                assignedJobs = 1,
                unassignedJobs = 0,
                completedJobs = 0
            }
        });

        var board = await client.Dispatches.GetBoardAsync("2026-03-16");

        var driver = Assert.Single(board.Drivers);
        var job = Assert.Single(driver.Jobs);

        Assert.Equal("job-1", job.Id);
        Assert.Equal(18, job.EstimatedMinutes);
        Assert.Equal(45, job.BaselineMinutes);
        Assert.Equal(12.5, job.DriveToMinutes);
        Assert.Equal(8.3, job.DriveToMiles);
        Assert.Equal("routing_engine", job.DriveTimeSource);
        Assert.Equal("2026-03-16T08:30:00Z", job.EstimatedStartTime);
    }

    [Fact]
    public async Task GetBoardAsync_DriveTimeFieldsNullBeforeOptimization()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            date = "2026-03-16",
            drivers = Array.Empty<object>(),
            unassignedJobs = new[]
            {
                new
                {
                    id = "job-unassigned",
                    type = "PICKUP",
                    status = "UNASSIGNED",
                    customerName = "Widget Co",
                    estimatedMinutes = 25,
                    baselineMinutes = 60,
                    // driveToMinutes, driveToMiles, driveTimeSource are absent (null)
                    createdAt = "2026-03-15T10:00:00Z",
                    updatedAt = "2026-03-15T10:00:00Z"
                }
            },
            metrics = new
            {
                totalJobs = 1,
                assignedJobs = 0,
                unassignedJobs = 1,
                completedJobs = 0
            }
        });

        var board = await client.Dispatches.GetBoardAsync("2026-03-16");

        var unassigned = Assert.Single(board.UnassignedJobs);
        Assert.Equal("job-unassigned", unassigned.Id);
        Assert.Equal(25, unassigned.EstimatedMinutes);
        Assert.Equal(60, unassigned.BaselineMinutes);
        Assert.Null(unassigned.DriveToMinutes);
        Assert.Null(unassigned.DriveToMiles);
        Assert.Null(unassigned.DriveTimeSource);
    }

    [Fact]
    public async Task GetBoardAsync_DriveTimeSourceHaversine()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            date = "2026-03-16",
            drivers = new[]
            {
                new
                {
                    id = "drv-1",
                    name = "Jane",
                    jobs = new[]
                    {
                        new
                        {
                            id = "job-est",
                            type = "DELIVERY",
                            status = "ASSIGNED",
                            customerName = "New Site Co",
                            estimatedMinutes = 18,
                            driveToMinutes = 15.0,
                            driveToMiles = 10.2,
                            driveTimeSource = "haversine",
                            createdAt = "2026-03-16T06:00:00Z",
                            updatedAt = "2026-03-16T06:00:00Z"
                        }
                    },
                    totalDriveMinutes = 15,
                    totalServiceMinutes = 18,
                    totalBufferMinutes = 5,
                    score = 70
                }
            },
            unassignedJobs = Array.Empty<object>()
        });

        var board = await client.Dispatches.GetBoardAsync("2026-03-16");

        var job = board.Drivers[0].Jobs[0];
        Assert.Equal("haversine", job.DriveTimeSource);
        Assert.Equal(15.0, job.DriveToMinutes);
    }

    [Fact]
    public async Task GetBoardAsync_DriveTimeSourceCached()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            date = "2026-03-16",
            drivers = new[]
            {
                new
                {
                    id = "drv-1",
                    name = "Bob",
                    jobs = new[]
                    {
                        new
                        {
                            id = "job-cached",
                            type = "PICKUP",
                            status = "ASSIGNED",
                            customerName = "Repeat Customer",
                            estimatedMinutes = 25,
                            driveToMinutes = 18.0,
                            driveToMiles = 12.1,
                            driveTimeSource = "cached",
                            createdAt = "2026-03-16T06:00:00Z",
                            updatedAt = "2026-03-16T06:00:00Z"
                        }
                    },
                    totalDriveMinutes = 18,
                    totalServiceMinutes = 25,
                    totalBufferMinutes = 5,
                    score = 80
                }
            },
            unassignedJobs = Array.Empty<object>()
        });

        var board = await client.Dispatches.GetBoardAsync("2026-03-16");

        var job = board.Drivers[0].Jobs[0];
        Assert.Equal("cached", job.DriveTimeSource);
    }

    // --- OptimizationResult - driveTimeSource ---

    [Fact]
    public async Task GetOptimizationStatusAsync_DeserializesDriveTimeSource()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            jobId = "opt-123",
            status = "COMPLETED",
            result = new
            {
                flowScore = 85,
                totalJobs = 10,
                assignedJobs = 9,
                unassignedJobs = 1,
                planQuality = 78,
                planGrade = "B+",
                driveTimeSource = "ESTIMATED"
            }
        });

        var job = await client.Dispatches.GetOptimizationStatusAsync("opt-123");

        Assert.NotNull(job.Result);
        Assert.Equal("ESTIMATED", job.Result!.DriveTimeSource);
        Assert.Equal(85, job.Result.FlowScore);
    }

    // --- ScorePlanAsync (CLI data pipeline) ---

    [Fact]
    public async Task ScorePlanAsync_SendsPostToDateScopedPath()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            date = "2026-04-07",
            planGrade = "B",
            planQuality = 78,
            flowScore = 72,
            assignedJobs = 138,
            unassignedJobs = 4,
            driveTimeSource = "CACHED",
            recommendation = "KEEP_AS_IS",
            recommendationReason = "Plan quality 78 meets threshold (70)."
        });

        await client.Dispatches.ScorePlanAsync("2026-04-07");

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.EndsWith("api/v1/dispatches/2026-04-07/score", req.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ScorePlanAsync_SerializesIncludeDriverBreakdownFlag()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            date = "2026-04-07",
            planGrade = "A",
            planQuality = 91,
            flowScore = 88,
            assignedJobs = 100,
            unassignedJobs = 0,
            recommendation = "KEEP_AS_IS"
        });

        await client.Dispatches.ScorePlanAsync("2026-04-07", includeDriverBreakdown: true);

        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.GetProperty("includeDriverBreakdown").GetBoolean());
    }

    [Fact]
    public async Task ScorePlanAsync_DeserializesKeepAsIsRecommendation()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            date = "2026-04-07",
            planGrade = "B",
            planQuality = 78,
            flowScore = 72,
            assignedJobs = 138,
            unassignedJobs = 4,
            driveTimeSource = "CACHED",
            recommendation = "KEEP_AS_IS",
            recommendationReason = "Plan quality 78 meets threshold (70). Re-optimization is unlikely to improve the plan significantly."
        });

        var result = await client.Dispatches.ScorePlanAsync("2026-04-07");

        Assert.Equal("2026-04-07", result.Date);
        Assert.Equal("B", result.PlanGrade);
        Assert.Equal(78, result.PlanQuality);
        Assert.Equal(72, result.FlowScore);
        Assert.Equal(138, result.AssignedJobs);
        Assert.Equal(4, result.UnassignedJobs);
        Assert.Equal("CACHED", result.DriveTimeSource);
        Assert.Equal(PlanScoreRecommendation.KEEP_AS_IS, result.Recommendation);
        Assert.Contains("meets threshold", result.RecommendationReason);
        Assert.Null(result.DriverBreakdown);
    }

    [Fact]
    public async Task ScorePlanAsync_DeserializesReOptimizeRecommendation()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            date = "2026-04-07",
            planGrade = "D",
            planQuality = 55,
            flowScore = 48,
            assignedJobs = 90,
            unassignedJobs = 52,
            driveTimeSource = "HAVERSINE",
            recommendation = "RE_OPTIMIZE",
            recommendationReason = "Plan quality 55 below threshold (70)."
        });

        var result = await client.Dispatches.ScorePlanAsync("2026-04-07");

        Assert.Equal(PlanScoreRecommendation.RE_OPTIMIZE, result.Recommendation);
        Assert.Equal("HAVERSINE", result.DriveTimeSource);
    }

    [Fact]
    public async Task ScorePlanAsync_DeserializesDriverBreakdown()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            date = "2026-04-07",
            planGrade = "B",
            planQuality = 78,
            flowScore = 72,
            assignedJobs = 24,
            unassignedJobs = 0,
            recommendation = "KEEP_AS_IS",
            driverBreakdown = new[]
            {
                new
                {
                    driverName = "Driver A",
                    jobCount = 12,
                    score = 84,
                    chainRate = 0.84,
                    utilizationPercent = 92.5
                },
                new
                {
                    driverName = "Driver B",
                    jobCount = 12,
                    score = 68,
                    chainRate = 0.55,
                    utilizationPercent = 71.0
                }
            }
        });

        var result = await client.Dispatches.ScorePlanAsync("2026-04-07", includeDriverBreakdown: true);

        Assert.NotNull(result.DriverBreakdown);
        Assert.Equal(2, result.DriverBreakdown!.Count);
        Assert.Equal("Driver A", result.DriverBreakdown[0].DriverName);
        Assert.Equal(12, result.DriverBreakdown[0].JobCount);
        Assert.Equal(84, result.DriverBreakdown[0].Score);
        Assert.Equal(0.84, result.DriverBreakdown[0].ChainRate);
        Assert.Equal(92.5, result.DriverBreakdown[0].UtilizationPercent);
    }

    [Fact]
    public async Task ScorePlanAsync_ThrowsOnDispatchNotFound()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(
            HttpStatusCode.NotFound,
            new ApiErrorBody("DISPATCH_NOT_FOUND", "No jobs scheduled for the date"));

        var ex = await Assert.ThrowsAsync<KlauApiException>(
            () => client.Dispatches.ScorePlanAsync("2026-04-07"));

        Assert.Equal("DISPATCH_NOT_FOUND", ex.ErrorCode);
        Assert.Equal(404, ex.StatusCode);
        Assert.True(ex.IsNotFound);
    }

    [Fact]
    public async Task ScorePlanAsync_IncludeDriverBreakdownDefaultsFalse()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            date = "2026-04-07",
            planGrade = "A",
            planQuality = 90,
            flowScore = 85,
            assignedJobs = 50,
            unassignedJobs = 0,
            recommendation = "KEEP_AS_IS"
        });

        await client.Dispatches.ScorePlanAsync("2026-04-07");

        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        Assert.False(doc.RootElement.GetProperty("includeDriverBreakdown").GetBoolean());
    }
}
