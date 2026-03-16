using System.Net;
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
}
