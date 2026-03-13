using System.Text.Json.Serialization;
using Klau.Sdk.Common;

namespace Klau.Sdk.DumpTickets;

public sealed class DumpTicket
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("ticketNumber")]
    public string TicketNumber { get; set; } = string.Empty;

    [JsonPropertyName("grossWeightLbs")]
    public int GrossWeightLbs { get; set; }

    [JsonPropertyName("tareWeightLbs")]
    public int TareWeightLbs { get; set; }

    [JsonPropertyName("netWeightLbs")]
    public int? NetWeightLbs { get; set; }

    [JsonPropertyName("dumpSiteId")]
    public string? DumpSiteId { get; set; }

    [JsonPropertyName("materialId")]
    public string? MaterialId { get; set; }

    [JsonPropertyName("tippingFeeCents")]
    public int? TippingFeeCents { get; set; }

    [JsonPropertyName("source")]
    public DumpTicketSource Source { get; set; }

    [JsonPropertyName("isVerified")]
    public bool IsVerified { get; set; }

    [JsonPropertyName("settlementApplied")]
    public bool SettlementApplied { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

public sealed class CreateDumpTicketRequest
{
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("ticketNumber")]
    public string TicketNumber { get; set; } = string.Empty;

    [JsonPropertyName("grossWeightLbs")]
    public int GrossWeightLbs { get; set; }

    [JsonPropertyName("tareWeightLbs")]
    public int TareWeightLbs { get; set; }

    [JsonPropertyName("dumpSiteId")]
    public string? DumpSiteId { get; set; }

    [JsonPropertyName("materialId")]
    public string? MaterialId { get; set; }

    [JsonPropertyName("tippingFeeCents")]
    public int? TippingFeeCents { get; set; }
}

public sealed class VerifyDumpTicketRequest
{
    [JsonPropertyName("ticketNumber")]
    public string? TicketNumber { get; set; }

    [JsonPropertyName("grossWeightLbs")]
    public int? GrossWeightLbs { get; set; }

    [JsonPropertyName("tareWeightLbs")]
    public int? TareWeightLbs { get; set; }

    [JsonPropertyName("dumpSiteId")]
    public string? DumpSiteId { get; set; }

    [JsonPropertyName("materialId")]
    public string? MaterialId { get; set; }

    [JsonPropertyName("tippingFeeCents")]
    public int? TippingFeeCents { get; set; }
}
