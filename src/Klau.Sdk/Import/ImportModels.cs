using System.Text.Json.Serialization;
using Klau.Sdk.Common;

namespace Klau.Sdk.Import;

/// <summary>
/// A single job record for import. Uses customer/site names and addresses
/// instead of requiring pre-created IDs — you do NOT need to call
/// <c>Customers.CreateAsync</c> or set up sites before importing.
///
/// When <see cref="ImportJobsRequest.CreateMissing"/> is true (the default),
/// customers and sites that don't already exist are auto-created. Existing records
/// are matched case-insensitively by name. New sites are geocoded from the address
/// and added to the drive-time cache automatically.
/// </summary>
public sealed record ImportJobRecord
{
    /// <summary>
    /// Customer name. Matched case-insensitively; created if missing and CreateMissing is true.
    /// </summary>
    [JsonPropertyName("customerName")]
    public required string CustomerName { get; init; }

    /// <summary>
    /// Site name within the customer. Matched case-insensitively; created if missing and CreateMissing is true.
    /// </summary>
    [JsonPropertyName("siteName")]
    public required string SiteName { get; init; }

    /// <summary>
    /// Street address for the site. Used for geocoding when creating a new site.
    /// </summary>
    [JsonPropertyName("siteAddress")]
    public required string SiteAddress { get; init; }

    /// <summary>
    /// City for the site address.
    /// </summary>
    [JsonPropertyName("siteCity")]
    public string? SiteCity { get; init; }

    /// <summary>
    /// State for the site address.
    /// </summary>
    [JsonPropertyName("siteState")]
    public string? SiteState { get; init; }

    /// <summary>
    /// ZIP code for the site address.
    /// </summary>
    [JsonPropertyName("siteZip")]
    public string? SiteZip { get; init; }

    /// <summary>
    /// Job type: DELIVERY, PICKUP, DUMP_RETURN, or SWAP.
    /// </summary>
    [JsonPropertyName("jobType")]
    public required string JobType { get; init; }

    /// <summary>
    /// Container size in yards (e.g. "10", "20", "30", "40").
    /// Passed as a string to match the CSV-oriented API contract.
    /// </summary>
    [JsonPropertyName("containerSize")]
    public required string ContainerSize { get; init; }

    /// <summary>
    /// Time window preference: MORNING, AFTERNOON, or ANYTIME. Defaults to ANYTIME if omitted.
    /// </summary>
    [JsonPropertyName("timeWindow")]
    public string? TimeWindow { get; init; }

    /// <summary>
    /// Job priority: NORMAL, HIGH, or URGENT. Defaults to NORMAL if omitted.
    /// </summary>
    [JsonPropertyName("priority")]
    public string? Priority { get; init; }

    /// <summary>
    /// Free-text notes for the job (gate codes, special instructions, etc.).
    /// </summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; init; }

    /// <summary>
    /// Requested date in YYYY-MM-DD format. Defaults to tenant's "today" if omitted.
    /// </summary>
    [JsonPropertyName("requestedDate")]
    public string? RequestedDate { get; init; }

    /// <summary>
    /// External system ID for bidirectional sync (e.g. your ERP work order ID).
    /// Must be unique per company; duplicate external IDs are rejected.
    /// </summary>
    [JsonPropertyName("externalId")]
    public string? ExternalId { get; init; }

    // --- Pre-routing fields (CLI data pipeline) ---
    //
    // Preserve source-system driver/truck/sequence assignments end-to-end
    // instead of dropping them on the floor. When any row in a batch carries
    // pre-routing fields, the worker resolves drivers (by externalId) and
    // trucks (by truckNumber, case-insensitive) in a single batch lookup and
    // assigns each job after creation.
    //
    // Pre-routed assignments use AssignmentSource CLI_PREROUTED, which does
    // NOT auto-pin jobs the way MANUAL does — the dispatcher can still score
    // the plan and re-optimize freely.
    //
    // Unknown driver/truck IDs produce warnings (DRIVER_MATCH_FAILED /
    // TRUCK_MATCH_FAILED), not hard failures — the job is still imported
    // unassigned. Check AsyncImportBatchStatus.DriverMatchFailures /
    // TruckMatchFailures for the per-id roll-up.

    /// <summary>
    /// External driver ID from the source system. Matched against <c>Driver.externalId</c>
    /// within the tenant. Unknown IDs produce a <c>DRIVER_MATCH_FAILED</c> warning
    /// and the job is imported UNASSIGNED.
    /// </summary>
    [JsonPropertyName("assignedDriverExternalId")]
    public string? AssignedDriverExternalId { get; init; }

    /// <summary>
    /// Truck number from the source system. Matched against <c>Truck.truckNumber</c>
    /// case-insensitively within the tenant. Unknown numbers produce a
    /// <c>TRUCK_MATCH_FAILED</c> warning and the job is imported UNASSIGNED.
    /// </summary>
    [JsonPropertyName("assignedTruckNumber")]
    public string? AssignedTruckNumber { get; init; }

    /// <summary>
    /// 1-based position within the driver's route. Ignored if the driver does not resolve.
    /// </summary>
    [JsonPropertyName("sequence")]
    public int? Sequence { get; init; }

    /// <summary>
    /// Estimated start time in ISO 8601 format. Naive timestamps (no offset)
    /// are interpreted as the tenant's local timezone. Required when
    /// <see cref="AssignedDriverExternalId"/> or <see cref="AssignedTruckNumber"/>
    /// is set — otherwise the worker emits an <c>ESTIMATED_START_TIME_MISSING</c> warning.
    /// </summary>
    [JsonPropertyName("estimatedStartTime")]
    public string? EstimatedStartTime { get; init; }
}

/// <summary>
/// Request body for the import endpoints (sync and async).
/// Used by <see cref="ImportClient.JobsAsync"/>, <see cref="ImportClient.SubmitJobsAsync"/>,
/// and their convenience wrappers.
/// </summary>
public sealed record ImportJobsRequest
{
    /// <summary>
    /// The job records to import.
    /// </summary>
    [JsonPropertyName("jobs")]
    public required IReadOnlyList<ImportJobRecord> Jobs { get; init; }

    /// <summary>
    /// When true (the default), auto-creates customer and site records that don't already exist.
    /// When false, rows referencing unknown customers or sites are skipped with an error.
    /// </summary>
    [JsonPropertyName("createMissing")]
    public bool CreateMissing { get; init; } = true;
}

/// <summary>
/// Result from the job import endpoint.
/// </summary>
public sealed record ImportJobsResult
{
    /// <summary>
    /// True when every row was imported without errors.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    /// <summary>
    /// Batch identifier for tracking drive-time cache readiness.
    /// Use with <see cref="ImportClient.GetReadinessAsync"/> to poll for cache warm-up
    /// before running optimization.
    /// </summary>
    [JsonPropertyName("batchId")]
    public string? BatchId { get; init; }

    /// <summary>
    /// Number of jobs successfully created.
    /// </summary>
    [JsonPropertyName("imported")]
    public int Imported { get; init; }

    /// <summary>
    /// Number of rows skipped due to validation errors.
    /// </summary>
    [JsonPropertyName("skipped")]
    public int Skipped { get; init; }

    /// <summary>
    /// Per-row validation errors. Empty when <see cref="Success"/> is true.
    /// </summary>
    [JsonPropertyName("errors")]
    public IReadOnlyList<ImportError> Errors { get; init; } = [];

    /// <summary>
    /// Number of new customer records created during import.
    /// </summary>
    [JsonPropertyName("customersCreated")]
    public int CustomersCreated { get; init; }

    /// <summary>
    /// Number of new site records created during import.
    /// </summary>
    [JsonPropertyName("sitesCreated")]
    public int SitesCreated { get; init; }
}

/// <summary>
/// A validation or processing warning for a specific row in the import batch.
/// Rows with an error are NOT rejected from the batch — the import continues
/// with the affected field(s) dropped. For pre-routing failures specifically,
/// the job is still imported UNASSIGNED.
/// </summary>
public sealed record ImportError
{
    /// <summary>
    /// 1-based row number in the import batch.
    /// </summary>
    [JsonPropertyName("row")]
    public int Row { get; init; }

    /// <summary>
    /// The field that failed validation (e.g. "customerName", "containerSize",
    /// "externalId", "assignedDriverExternalId").
    /// </summary>
    [JsonPropertyName("field")]
    public string Field { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable error description.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Structured error code for programmatic handling. Known values include:
    /// <list type="bullet">
    ///   <item><c>DRIVER_MATCH_FAILED</c> — <see cref="Meta"/> contains <c>externalId</c>.</item>
    ///   <item><c>TRUCK_MATCH_FAILED</c> — <see cref="Meta"/> contains <c>truckNumber</c>.</item>
    ///   <item><c>ESTIMATED_START_TIME_MISSING</c> — row had driver/truck but no start time.</item>
    ///   <item><c>ESTIMATED_START_TIME_INVALID</c> — start time was not parseable as ISO 8601.</item>
    ///   <item><c>PRE_ROUTE_ASSIGNMENT_FAILED</c> — <see cref="Meta"/> contains <c>driverExternalId</c> and <c>truckNumber</c>.</item>
    /// </list>
    /// Null for legacy per-row validation errors emitted by the sync import path.
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    /// <summary>
    /// Structured metadata keyed by field name. See <see cref="Code"/> for the
    /// fields carried by each error code. Null or empty for errors that don't
    /// need additional context beyond <see cref="Field"/> and <see cref="Message"/>.
    /// </summary>
    [JsonPropertyName("meta")]
    public IReadOnlyDictionary<string, string>? Meta { get; init; }
}

/// <summary>
/// Aggregated pre-routing match failures for a single identifier. The worker
/// rolls these up by ID so dispatchers can see at a glance which drivers or
/// trucks were missing (and how many rows referenced them) without scanning
/// every <see cref="ImportError"/>.
/// </summary>
public sealed record ImportMatchFailure
{
    /// <summary>
    /// Driver external ID that did not resolve. Present on
    /// <see cref="AsyncImportBatchStatus.DriverMatchFailures"/>.
    /// </summary>
    [JsonPropertyName("externalId")]
    public string? ExternalId { get; init; }

    /// <summary>
    /// Truck number that did not resolve. Present on
    /// <see cref="AsyncImportBatchStatus.TruckMatchFailures"/>.
    /// </summary>
    [JsonPropertyName("truckNumber")]
    public string? TruckNumber { get; init; }

    /// <summary>
    /// Number of rows in the batch that referenced this unresolved identifier.
    /// </summary>
    [JsonPropertyName("rowCount")]
    public int RowCount { get; init; }
}

/// <summary>
/// Drive-time cache readiness status for an import batch.
/// After importing jobs with new site addresses, drive-time calculations
/// run asynchronously. Poll this until <see cref="Status"/> is <c>"ready"</c>
/// before running optimization to ensure accurate routing.
/// </summary>
public sealed record BatchReadiness
{
    /// <summary>The import batch identifier.</summary>
    [JsonPropertyName("batchId")]
    public required string BatchId { get; init; }

    /// <summary>Total number of unique sites in the batch.</summary>
    [JsonPropertyName("sitesTotal")]
    public required int SitesTotal { get; init; }

    /// <summary>Number of sites with cached drive times.</summary>
    [JsonPropertyName("sitesCached")]
    public required int SitesCached { get; init; }

    /// <summary>
    /// Readiness status: <c>"ready"</c>, <c>"warming"</c>, <c>"partial"</c>, or <c>"not_applicable"</c>.
    /// </summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>Human-readable status message.</summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }
}

// --- Async import (POST /api/v1/import/jobs/async → poll /api/v1/import/batches/{batchId}/status) ---

/// <summary>
/// Result from submitting an async job import.
/// The batch is queued for background processing — poll
/// <see cref="ImportClient.GetBatchStatusAsync"/> to track progress.
/// </summary>
public sealed record AsyncImportSubmitResult
{
    /// <summary>Batch identifier for polling status.</summary>
    [JsonPropertyName("batchId")]
    public string BatchId { get; init; } = string.Empty;

    /// <summary>Number of jobs accepted into the batch.</summary>
    [JsonPropertyName("jobCount")]
    public int JobCount { get; init; }

    /// <summary>Initial status (always ACCEPTED on a successful submit).</summary>
    [JsonPropertyName("status")]
    public ImportBatchStatus Status { get; init; }
}

/// <summary>
/// Processing status for an async import batch.
/// Poll until <see cref="Status"/> is terminal (COMPLETED, PARTIAL_FAILURE, or FAILED)
/// and <see cref="DriveTimeCacheStatus"/> is READY or NOT_APPLICABLE before optimizing.
/// </summary>
public sealed record AsyncImportBatchStatus
{
    /// <summary>The import batch identifier.</summary>
    [JsonPropertyName("batchId")]
    public string BatchId { get; init; } = string.Empty;

    /// <summary>
    /// Processing status. Terminal values: COMPLETED, PARTIAL_FAILURE, FAILED.
    /// </summary>
    [JsonPropertyName("status")]
    public ImportBatchStatus Status { get; init; }

    /// <summary>Total jobs in the batch.</summary>
    [JsonPropertyName("total")]
    public int Total { get; init; }

    /// <summary>Jobs processed so far.</summary>
    [JsonPropertyName("processed")]
    public int Processed { get; init; }

    /// <summary>Jobs successfully imported.</summary>
    [JsonPropertyName("imported")]
    public int Imported { get; init; }

    /// <summary>Jobs skipped due to validation errors.</summary>
    [JsonPropertyName("skipped")]
    public int Skipped { get; init; }

    /// <summary>Customer records auto-created during import.</summary>
    [JsonPropertyName("customersCreated")]
    public int CustomersCreated { get; init; }

    /// <summary>Site records auto-created during import.</summary>
    [JsonPropertyName("sitesCreated")]
    public int SitesCreated { get; init; }

    /// <summary>Per-row validation errors. Empty when all jobs succeed.</summary>
    [JsonPropertyName("errors")]
    public IReadOnlyList<ImportError> Errors { get; init; } = [];

    /// <summary>
    /// Drive-time cache warm-up status. Wait for READY or NOT_APPLICABLE
    /// before running dispatch optimization.
    /// </summary>
    [JsonPropertyName("driveTimeCacheStatus")]
    public DriveTimeCacheStatus DriveTimeCacheStatus { get; init; }

    /// <summary>
    /// True when any row in the batch carried pre-routing fields
    /// (<see cref="ImportJobRecord.AssignedDriverExternalId"/>,
    /// <see cref="ImportJobRecord.AssignedTruckNumber"/>, etc.).
    /// The worker takes the pre-routing code path when this is true —
    /// drivers and trucks are resolved in a single batch lookup before
    /// any job is created.
    /// </summary>
    [JsonPropertyName("preRouted")]
    public bool PreRouted { get; init; }

    /// <summary>
    /// Driver external IDs that did not resolve during pre-routing, rolled up
    /// with the number of rows that referenced each one. Empty when every
    /// referenced driver was found (or when the batch was not pre-routed).
    /// </summary>
    [JsonPropertyName("driverMatchFailures")]
    public IReadOnlyList<ImportMatchFailure> DriverMatchFailures { get; init; } = [];

    /// <summary>
    /// Truck numbers that did not resolve during pre-routing, rolled up with
    /// the number of rows that referenced each one. Empty when every referenced
    /// truck was found (or when the batch was not pre-routed).
    /// </summary>
    [JsonPropertyName("truckMatchFailures")]
    public IReadOnlyList<ImportMatchFailure> TruckMatchFailures { get; init; } = [];

    /// <summary>
    /// True when <see cref="Status"/> is a terminal value (COMPLETED, PARTIAL_FAILURE, or FAILED).
    /// </summary>
    public bool IsTerminal => Status is ImportBatchStatus.COMPLETED
        or ImportBatchStatus.PARTIAL_FAILURE
        or ImportBatchStatus.FAILED;

    /// <summary>
    /// True when the batch is fully done — terminal status and drive-time cache is warm (or not needed).
    /// Safe to run optimization after this returns true.
    /// </summary>
    public bool IsReadyForOptimization => IsTerminal
        && DriveTimeCacheStatus is DriveTimeCacheStatus.READY or DriveTimeCacheStatus.NOT_APPLICABLE;
}

/// <summary>
/// Thrown by <see cref="ImportClient.SubmitAndWaitAsync"/> when polling fails after a
/// successful submission. The <see cref="BatchId"/> is preserved so the caller can
/// resume polling manually with <see cref="ImportClient.GetBatchStatusAsync"/>.
/// </summary>
public class KlauImportException : Exception
{
    /// <summary>
    /// The batch ID that was successfully submitted. Use this to resume polling
    /// via <see cref="ImportClient.GetBatchStatusAsync"/> after handling the error.
    /// </summary>
    public string BatchId { get; }

    /// <summary>
    /// The last known batch status, if a status poll succeeded before the failure.
    /// Null if the first poll failed.
    /// </summary>
    public AsyncImportBatchStatus? LastStatus { get; }

    public KlauImportException(string batchId, string message, AsyncImportBatchStatus? lastStatus = null, Exception? innerException = null)
        : base(message, innerException)
    {
        BatchId = batchId;
        LastStatus = lastStatus;
    }
}
