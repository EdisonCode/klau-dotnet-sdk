using System.Text.Json.Serialization;
using Klau.Sdk.Common;

namespace Klau.Sdk.DumpTickets;

public sealed record DumpTicket
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("jobId")]
    public string JobId { get; init; } = string.Empty;

    [JsonPropertyName("ticketNumber")]
    public string TicketNumber { get; init; } = string.Empty;

    [JsonPropertyName("grossWeightLbs")]
    public int GrossWeightLbs { get; init; }

    [JsonPropertyName("tareWeightLbs")]
    public int TareWeightLbs { get; init; }

    [JsonPropertyName("netWeightLbs")]
    public int? NetWeightLbs { get; init; }

    [JsonPropertyName("dumpSiteId")]
    public string? DumpSiteId { get; init; }

    [JsonPropertyName("materialId")]
    public string? MaterialId { get; init; }

    [JsonPropertyName("tippingFeeCents")]
    public int? TippingFeeCents { get; init; }

    [JsonPropertyName("source")]
    public DumpTicketSource Source { get; init; }

    [JsonPropertyName("isVerified")]
    public bool IsVerified { get; init; }

    [JsonPropertyName("settlementApplied")]
    public bool SettlementApplied { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; init; }
}

public sealed record CreateDumpTicketRequest
{
    [JsonPropertyName("jobId")]
    public required string JobId { get; init; }

    [JsonPropertyName("ticketNumber")]
    public required string TicketNumber { get; init; }

    [JsonPropertyName("grossWeightLbs")]
    public required int GrossWeightLbs { get; init; }

    [JsonPropertyName("tareWeightLbs")]
    public required int TareWeightLbs { get; init; }

    [JsonPropertyName("dumpSiteId")]
    public string? DumpSiteId { get; init; }

    [JsonPropertyName("materialId")]
    public string? MaterialId { get; init; }

    [JsonPropertyName("tippingFeeCents")]
    public int? TippingFeeCents { get; init; }
}

public sealed record VerifyDumpTicketRequest
{
    [JsonPropertyName("ticketNumber")]
    public string? TicketNumber { get; init; }

    [JsonPropertyName("grossWeightLbs")]
    public int? GrossWeightLbs { get; init; }

    [JsonPropertyName("tareWeightLbs")]
    public int? TareWeightLbs { get; init; }

    [JsonPropertyName("dumpSiteId")]
    public string? DumpSiteId { get; init; }

    [JsonPropertyName("materialId")]
    public string? MaterialId { get; init; }

    [JsonPropertyName("tippingFeeCents")]
    public int? TippingFeeCents { get; init; }
}
