using Klau.Sdk.Companies;
using Klau.Sdk.Customers;
using Klau.Sdk.Dispatches;
using Klau.Sdk.Drivers;
using Klau.Sdk.DumpSites;
using Klau.Sdk.Jobs;
using Klau.Sdk.Trucks;
using Klau.Sdk.Yards;

namespace Klau.Sdk.Common;

/// <summary>
/// Factory for constructing SDK model instances in tests. Every parameter has a sensible
/// default, so you only specify what your test cares about:
/// <code>
/// var job = KlauModelFactory.Job(status: JobStatus.COMPLETED, driverId: "drv-1");
/// var page = KlauModelFactory.PagedResult([job1, job2], total: 50, hasMore: true);
/// </code>
/// </summary>
public static class KlauModelFactory
{
    public static Job Job(
        string? id = null,
        JobType type = JobType.DELIVERY,
        JobStatus status = JobStatus.UNASSIGNED,
        string? customerName = null,
        string? customerId = null,
        string? siteId = null,
        string? siteAddress = null,
        int? containerSize = 20,
        string? containerNumber = null,
        string? scheduledDate = null,
        string? requestedDate = null,
        TimeWindow? timeWindow = null,
        JobPriority? priority = null,
        string? driverId = null,
        string? driverName = null,
        string? truckId = null,
        int? sequence = null,
        string? estimatedStartTime = null,
        int? estimatedMinutes = 30,
        string? notes = null,
        string? externalId = null,
        string? orderId = null,
        string? dumpSiteId = null,
        DateTime? createdAt = null,
        DateTime? updatedAt = null) => new()
    {
        Id = id ?? Guid.NewGuid().ToString(),
        Type = type,
        Status = status,
        CustomerName = customerName ?? "Test Customer",
        CustomerId = customerId,
        SiteId = siteId,
        SiteAddress = siteAddress ?? "123 Test St",
        ContainerSize = containerSize,
        ContainerNumber = containerNumber,
        ScheduledDate = scheduledDate,
        RequestedDate = requestedDate ?? DateTime.Today.ToString("yyyy-MM-dd"),
        TimeWindow = timeWindow,
        Priority = priority,
        DriverId = driverId,
        DriverName = driverName,
        TruckId = truckId,
        Sequence = sequence,
        EstimatedStartTime = estimatedStartTime,
        EstimatedMinutes = estimatedMinutes,
        Notes = notes,
        ExternalId = externalId,
        OrderId = orderId,
        DumpSiteId = dumpSiteId,
        CreatedAt = createdAt ?? DateTime.UtcNow,
        UpdatedAt = updatedAt ?? DateTime.UtcNow,
    };

    public static DispatchBoardJob DispatchBoardJob(
        string? id = null,
        JobType type = JobType.DELIVERY,
        JobStatus status = JobStatus.ASSIGNED,
        string? customerName = null,
        int? containerSize = 20,
        int? sequence = null,
        string? estimatedStartTime = null,
        int? estimatedMinutes = 30,
        int? baselineMinutes = null,
        double? driveToMinutes = null,
        double? driveToMiles = null,
        string? driveTimeSource = null,
        string? externalId = null,
        string? driverId = null,
        DateTime? createdAt = null,
        DateTime? updatedAt = null) => new()
    {
        Id = id ?? Guid.NewGuid().ToString(),
        Type = type,
        Status = status,
        CustomerName = customerName ?? "Test Customer",
        ContainerSize = containerSize,
        Sequence = sequence,
        EstimatedStartTime = estimatedStartTime,
        EstimatedMinutes = estimatedMinutes,
        BaselineMinutes = baselineMinutes,
        DriveToMinutes = driveToMinutes,
        DriveToMiles = driveToMiles,
        DriveTimeSource = driveTimeSource,
        ExternalId = externalId,
        DriverId = driverId,
        CreatedAt = createdAt ?? DateTime.UtcNow,
        UpdatedAt = updatedAt ?? DateTime.UtcNow,
    };

    public static Customer Customer(
        string? id = null,
        string? name = null,
        string? contactName = null,
        string? contactPhone = null,
        string? contactEmail = null,
        DateTime? createdAt = null) => new()
    {
        Id = id ?? Guid.NewGuid().ToString(),
        Name = name ?? "Test Customer",
        ContactName = contactName,
        ContactPhone = contactPhone,
        ContactEmail = contactEmail,
        CreatedAt = createdAt ?? DateTime.UtcNow,
    };

    public static Driver Driver(
        string? id = null,
        string? name = null,
        string? driverType = null,
        bool isActive = true,
        string? defaultTruckId = null) => new()
    {
        Id = id ?? Guid.NewGuid().ToString(),
        Name = name ?? "Test Driver",
        DriverType = driverType ?? "FULL_TIME",
        IsActive = isActive,
        DefaultTruckId = defaultTruckId,
    };

    public static Truck Truck(
        string? id = null,
        string? number = null,
        string? type = null,
        IReadOnlyList<int>? compatibleSizes = null) => new()
    {
        Id = id ?? Guid.NewGuid().ToString(),
        Number = number ?? "T-001",
        Type = type ?? "ROLL_OFF",
        CompatibleSizes = compatibleSizes ?? [10, 15, 20, 30, 40],
    };

    public static Yard Yard(
        string? id = null,
        string? name = null,
        string? address = null,
        double? latitude = null,
        double? longitude = null) => new()
    {
        Id = id ?? Guid.NewGuid().ToString(),
        Name = name ?? "Main Yard",
        Address = address ?? "100 Yard Rd",
        Latitude = latitude ?? 40.0,
        Longitude = longitude ?? -77.0,
    };

    public static DumpSite DumpSite(
        string? id = null,
        string? name = null,
        string? address = null) => new()
    {
        Id = id ?? Guid.NewGuid().ToString(),
        Name = name ?? "Test Landfill",
        Address = address ?? "200 Dump Rd",
    };

    public static PagedResult<T> PagedResult<T>(
        IReadOnlyList<T> items,
        int? total = null,
        int? page = null,
        int? pageSize = null,
        bool hasMore = false) => new(
        items,
        total ?? items.Count,
        page ?? 1,
        pageSize ?? items.Count,
        hasMore);
}
