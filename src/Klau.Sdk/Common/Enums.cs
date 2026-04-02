namespace Klau.Sdk.Common;

public enum JobType
{
    DELIVERY,
    PICKUP,
    DUMP_RETURN,
    SWAP,
    INTERNAL_DUMP,
    SERVICE_VISIT
}

public enum JobStatus
{
    UNASSIGNED,
    ASSIGNED,
    IN_PROGRESS,
    COMPLETED,
    CANCELLED
}

public enum ContainerSize
{
    Yard10 = 10,
    Yard15 = 15,
    Yard20 = 20,
    Yard30 = 30,
    Yard40 = 40
}

public enum TimeWindow
{
    MORNING,
    AFTERNOON,
    ANYTIME
}

public enum JobPriority
{
    NORMAL,
    HIGH,
    URGENT
}

public enum DispatchStatus
{
    DRAFT,
    PUBLISHED,
    IN_PROGRESS,
    COMPLETED
}

public enum OrderStatus
{
    SUBMITTED,
    CONFIRMED,
    SCHEDULED,
    ACTIVE,
    PICKUP_REQUESTED,
    COMPLETED,
    CANCELLED
}

public enum MaterialEconomicDirection
{
    DISPOSAL_COST,
    REDEMPTION_VALUE,
    NEUTRAL
}

public enum StorefrontStatus
{
    DRAFT,
    ACTIVE,
    PAUSED
}

public enum DumpTicketSource
{
    MANUAL,
    PHOTO_OCR,
    SCALE_API
}

public enum OptimizationMode
{
    FULL_DAY,
    NEW_JOB,
    REBALANCE
}

public enum OptimizationJobStatus
{
    PENDING,
    RUNNING,
    COMPLETED,
    FAILED,
    SKIPPED
}

/// <summary>
/// Container slot assignment for dual-container trucks (null for standard trucks).
/// </summary>
public enum ContainerSlot
{
    PRIMARY,
    SECONDARY
}
