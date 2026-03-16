using System.Text.Json.Serialization;

namespace Klau.Sdk.Import;

/// <summary>
/// A single job record for import. Uses customer/site names and addresses
/// instead of requiring pre-created IDs. When <see cref="ImportJobsRequest.CreateMissing"/>
/// is true (the default), Klau auto-creates customer and site stubs.
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
/// Request body for POST /api/v1/import/jobs.
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
