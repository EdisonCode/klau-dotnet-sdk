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
/// A validation error for a specific row in the import batch.
/// </summary>
public sealed record ImportError
{
    /// <summary>
    /// 1-based row number in the import batch.
    /// </summary>
    [JsonPropertyName("row")]
    public int Row { get; init; }

    /// <summary>
    /// The field that failed validation (e.g. "customerName", "containerSize", "externalId").
    /// </summary>
    [JsonPropertyName("field")]
    public string Field { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable error description.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
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
