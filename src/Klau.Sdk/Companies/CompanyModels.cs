using System.Text.Json.Serialization;

namespace Klau.Sdk.Companies;

/// <summary>
/// A service code mapping that maps external system job codes to Klau job types.
/// Used during import to automatically assign the correct job type.
/// </summary>
public sealed record ServiceCodeMapping
{
    /// <summary>
    /// The external system's service/job code (e.g. "DEL", "PU", "SW").
    /// </summary>
    [JsonPropertyName("externalCode")]
    public required string ExternalCode { get; init; }

    /// <summary>
    /// The Klau job type this code maps to: DELIVERY, PICKUP, DUMP_RETURN, SWAP, or SKIP.
    /// </summary>
    [JsonPropertyName("klauJobType")]
    public required string KlauJobType { get; init; }
}

/// <summary>
/// A pattern for matching container size strings during import.
/// </summary>
public sealed record ContainerPattern
{
    /// <summary>
    /// Regex or glob pattern to match against container size strings from external systems.
    /// </summary>
    [JsonPropertyName("pattern")]
    public required string Pattern { get; init; }

    /// <summary>
    /// The container size in yards to assign when the pattern matches.
    /// </summary>
    [JsonPropertyName("size")]
    public int? Size { get; init; }

    /// <summary>
    /// When true, jobs matching this pattern are skipped during import.
    /// </summary>
    [JsonPropertyName("skip")]
    public bool? Skip { get; init; }
}

/// <summary>
/// Company profile returned by GET /api/v1/companies/me.
/// Contains identity, location, operating hours, container config,
/// subscription info, and dispatch automation settings.
/// </summary>
public sealed record Company
{
    // --- Core Identity ---

    /// <summary>Company identifier.</summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>Company display name.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    // --- Location & Contact ---

    /// <summary>Street address.</summary>
    [JsonPropertyName("address")]
    public string? Address { get; init; }

    /// <summary>City.</summary>
    [JsonPropertyName("city")]
    public string? City { get; init; }

    /// <summary>State or province abbreviation.</summary>
    [JsonPropertyName("state")]
    public string? State { get; init; }

    /// <summary>ZIP or postal code.</summary>
    [JsonPropertyName("zip")]
    public string? Zip { get; init; }

    /// <summary>Primary phone number.</summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; init; }

    /// <summary>SMS-enabled phone number for driver notifications.</summary>
    [JsonPropertyName("smsPhoneNumber")]
    public string? SmsPhoneNumber { get; init; }

    // --- Service Area ---

    /// <summary>Northern boundary of the service area (latitude).</summary>
    [JsonPropertyName("serviceAreaNorth")]
    public double? ServiceAreaNorth { get; init; }

    /// <summary>Southern boundary of the service area (latitude).</summary>
    [JsonPropertyName("serviceAreaSouth")]
    public double? ServiceAreaSouth { get; init; }

    /// <summary>Eastern boundary of the service area (longitude).</summary>
    [JsonPropertyName("serviceAreaEast")]
    public double? ServiceAreaEast { get; init; }

    /// <summary>Western boundary of the service area (longitude).</summary>
    [JsonPropertyName("serviceAreaWest")]
    public double? ServiceAreaWest { get; init; }

    // --- Operating Hours ---

    /// <summary>IANA timezone identifier (e.g. "America/Los_Angeles").</summary>
    [JsonPropertyName("timezone")]
    public required string Timezone { get; init; }

    /// <summary>Workday start time in HH:mm format (e.g. "07:00").</summary>
    [JsonPropertyName("workdayStart")]
    public required string WorkdayStart { get; init; }

    /// <summary>Workday end time in HH:mm format (e.g. "17:00").</summary>
    [JsonPropertyName("workdayEnd")]
    public required string WorkdayEnd { get; init; }

    /// <summary>Active workdays as abbreviated day names (e.g. ["MON","TUE","WED","THU","FRI"]).</summary>
    [JsonPropertyName("workdays")]
    public required IReadOnlyList<string> Workdays { get; init; }

    // --- Container Configuration ---

    /// <summary>Available container sizes in yards (e.g. [10, 15, 20, 30, 40]).</summary>
    [JsonPropertyName("containerSizes")]
    public required IReadOnlyList<int> ContainerSizes { get; init; }

    // --- Job Buffering ---

    /// <summary>Percentage buffer added to estimated job duration.</summary>
    [JsonPropertyName("jobBufferPercentage")]
    public double? JobBufferPercentage { get; init; }

    /// <summary>Flat minutes added to estimated job duration.</summary>
    [JsonPropertyName("jobBufferFlatMinutes")]
    public double? JobBufferFlatMinutes { get; init; }

    // --- Subscription ---

    /// <summary>Subscription status: TRIAL, ACTIVE, PAST_DUE, CANCELLED, or PROSPECT.</summary>
    [JsonPropertyName("subscriptionStatus")]
    public string? SubscriptionStatus { get; init; }

    /// <summary>Subscription tier: KLAU_ONE, FULL, or SCORECARD_ONLY.</summary>
    [JsonPropertyName("subscriptionTier")]
    public string? SubscriptionTier { get; init; }

    /// <summary>ISO 8601 timestamp when the trial period ends.</summary>
    [JsonPropertyName("trialEndsAt")]
    public string? TrialEndsAt { get; init; }

    // --- Import Configuration ---

    /// <summary>Service code mappings for import — maps external codes to Klau job types.</summary>
    [JsonPropertyName("importServiceCodeMappings")]
    public IReadOnlyList<ServiceCodeMapping>? ImportServiceCodeMappings { get; init; }

    /// <summary>Container size patterns for import — matches external size strings to container sizes.</summary>
    [JsonPropertyName("importContainerPatterns")]
    public IReadOnlyList<ContainerPattern>? ImportContainerPatterns { get; init; }

    // --- Dispatch Automation ---

    /// <summary>When true, dispatches are automatically published after optimization.</summary>
    [JsonPropertyName("autoPublishDispatches")]
    public bool AutoPublishDispatches { get; init; }

    /// <summary>Score threshold (0–100) above which dispatches are auto-approved.</summary>
    [JsonPropertyName("dispatchApprovalThreshold")]
    public int DispatchApprovalThreshold { get; init; }

    // --- Metadata ---

    /// <summary>Whether this company is a founding member of the Klau platform.</summary>
    [JsonPropertyName("isFoundingMember")]
    public bool IsFoundingMember { get; init; }
}

/// <summary>
/// Request body for PATCH /api/v1/companies/me.
/// Only operational settings an integrator would change. All fields are optional;
/// only non-null fields are sent to the API.
/// </summary>
public sealed record UpdateCompanyRequest
{
    /// <summary>Available container sizes in yards.</summary>
    [JsonPropertyName("containerSizes")]
    public IReadOnlyList<int>? ContainerSizes { get; init; }

    /// <summary>IANA timezone identifier.</summary>
    [JsonPropertyName("timezone")]
    public string? Timezone { get; init; }

    /// <summary>Workday start time in HH:mm format.</summary>
    [JsonPropertyName("workdayStart")]
    public string? WorkdayStart { get; init; }

    /// <summary>Workday end time in HH:mm format.</summary>
    [JsonPropertyName("workdayEnd")]
    public string? WorkdayEnd { get; init; }

    /// <summary>Active workdays as abbreviated day names.</summary>
    [JsonPropertyName("workdays")]
    public IReadOnlyList<string>? Workdays { get; init; }

    /// <summary>Percentage buffer added to estimated job duration.</summary>
    [JsonPropertyName("jobBufferPercentage")]
    public double? JobBufferPercentage { get; init; }

    /// <summary>Flat minutes added to estimated job duration.</summary>
    [JsonPropertyName("jobBufferFlatMinutes")]
    public double? JobBufferFlatMinutes { get; init; }

    /// <summary>Northern boundary of the service area (latitude).</summary>
    [JsonPropertyName("serviceAreaNorth")]
    public double? ServiceAreaNorth { get; init; }

    /// <summary>Southern boundary of the service area (latitude).</summary>
    [JsonPropertyName("serviceAreaSouth")]
    public double? ServiceAreaSouth { get; init; }

    /// <summary>Eastern boundary of the service area (longitude).</summary>
    [JsonPropertyName("serviceAreaEast")]
    public double? ServiceAreaEast { get; init; }

    /// <summary>Western boundary of the service area (longitude).</summary>
    [JsonPropertyName("serviceAreaWest")]
    public double? ServiceAreaWest { get; init; }

    /// <summary>Service code mappings for import.</summary>
    [JsonPropertyName("importServiceCodeMappings")]
    public IReadOnlyList<ServiceCodeMapping>? ImportServiceCodeMappings { get; init; }

    /// <summary>Container size patterns for import.</summary>
    [JsonPropertyName("importContainerPatterns")]
    public IReadOnlyList<ContainerPattern>? ImportContainerPatterns { get; init; }

    /// <summary>When true, dispatches are automatically published after optimization.</summary>
    [JsonPropertyName("autoPublishDispatches")]
    public bool? AutoPublishDispatches { get; init; }

    /// <summary>Score threshold (0–100) above which dispatches are auto-approved.</summary>
    [JsonPropertyName("dispatchApprovalThreshold")]
    public int? DispatchApprovalThreshold { get; init; }
}
