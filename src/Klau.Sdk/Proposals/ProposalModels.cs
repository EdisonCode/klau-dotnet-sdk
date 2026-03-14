using System.Text.Json.Serialization;

namespace Klau.Sdk.Proposals;

public sealed record Proposal
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public ProposalStatus Status { get; init; }

    [JsonPropertyName("customerName")]
    public string CustomerName { get; init; } = string.Empty;

    [JsonPropertyName("customerPhone")]
    public string CustomerPhone { get; init; } = string.Empty;

    [JsonPropertyName("customerEmail")]
    public string? CustomerEmail { get; init; }

    [JsonPropertyName("containerSize")]
    public int ContainerSize { get; init; }

    [JsonPropertyName("proposedDate")]
    public string? ProposedDate { get; init; }

    [JsonPropertyName("lockedPriceCents")]
    public int LockedPriceCents { get; init; }

    [JsonPropertyName("viewedAt")]
    public DateTime? ViewedAt { get; init; }

    [JsonPropertyName("reminderSentAt")]
    public DateTime? ReminderSentAt { get; init; }

    [JsonPropertyName("convertedAt")]
    public DateTime? ConvertedAt { get; init; }

    [JsonPropertyName("orderId")]
    public string? OrderId { get; init; }

    [JsonPropertyName("expiresAt")]
    public DateTime ExpiresAt { get; init; }

    [JsonPropertyName("proposalLink")]
    public string? ProposalLink { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }
}

public sealed record CreateProposalRequest
{
    /// <summary>Customer's full name.</summary>
    [JsonPropertyName("customerName")]
    public required string CustomerName { get; init; }

    /// <summary>Customer's phone number (SMS notification sent here).</summary>
    [JsonPropertyName("customerPhone")]
    public required string CustomerPhone { get; init; }

    /// <summary>Customer's email (optional, email notification sent if provided).</summary>
    [JsonPropertyName("customerEmail")]
    public string? CustomerEmail { get; init; }

    /// <summary>Service delivery address.</summary>
    [JsonPropertyName("serviceAddress")]
    public required ProposalAddress ServiceAddress { get; init; }

    /// <summary>Container size in yards.</summary>
    [JsonPropertyName("containerSize")]
    public required int ContainerSize { get; init; }

    /// <summary>Material ID (optional).</summary>
    [JsonPropertyName("materialId")]
    public string? MaterialId { get; init; }

    /// <summary>Proposed delivery date (YYYY-MM-DD).</summary>
    [JsonPropertyName("proposedDate")]
    public required string ProposedDate { get; init; }

    /// <summary>Service offering ID to snapshot pricing from.</summary>
    [JsonPropertyName("offeringId")]
    public required string OfferingId { get; init; }

    /// <summary>Internal notes (not shown to customer).</summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; init; }
}

public sealed record ProposalAddress
{
    [JsonPropertyName("street")]
    public required string Street { get; init; }

    [JsonPropertyName("city")]
    public required string City { get; init; }

    [JsonPropertyName("state")]
    public required string State { get; init; }

    [JsonPropertyName("zip")]
    public required string Zip { get; init; }

    [JsonPropertyName("lat")]
    public double? Lat { get; init; }

    [JsonPropertyName("lng")]
    public double? Lng { get; init; }
}

public sealed record RemindRequest
{
    /// <summary>Optional note to include in the reminder SMS/email.</summary>
    [JsonPropertyName("note")]
    public string? Note { get; init; }
}

public sealed record UpdateOfferRequest
{
    /// <summary>New locked price in cents.</summary>
    [JsonPropertyName("newPriceCents")]
    public required int NewPriceCents { get; init; }

    /// <summary>Optional note sent to the customer about the price change.</summary>
    [JsonPropertyName("note")]
    public string? Note { get; init; }
}

public sealed record ProposalListResult
{
    [JsonPropertyName("proposals")]
    public IReadOnlyList<Proposal> Proposals { get; init; } = [];

    [JsonPropertyName("meta")]
    public ProposalListMeta? Meta { get; init; }
}

public sealed record ProposalListMeta
{
    [JsonPropertyName("total")]
    public int Total { get; init; }

    [JsonPropertyName("limit")]
    public int Limit { get; init; }

    [JsonPropertyName("offset")]
    public int Offset { get; init; }

    [JsonPropertyName("statusCounts")]
    public IReadOnlyDictionary<string, int>? StatusCounts { get; init; }
}

public sealed record PricingCalendarDay
{
    [JsonPropertyName("date")]
    public string Date { get; init; } = string.Empty;

    [JsonPropertyName("priceCents")]
    public int PriceCents { get; init; }

    [JsonPropertyName("basePriceCents")]
    public int BasePriceCents { get; init; }

    [JsonPropertyName("discountCents")]
    public int DiscountCents { get; init; }

    [JsonPropertyName("isOptimal")]
    public bool IsOptimal { get; init; }

    [JsonPropertyName("tier")]
    public string Tier { get; init; } = string.Empty;

    [JsonPropertyName("available")]
    public bool Available { get; init; }
}

public sealed record PricingCalendarResult
{
    [JsonPropertyName("calendar")]
    public IReadOnlyList<PricingCalendarDay> Calendar { get; init; } = [];
}

public sealed record ProposalRecommendation
{
    [JsonPropertyName("proposalId")]
    public string ProposalId { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("priority")]
    public string? Priority { get; init; }
}

public sealed record RecommendationsResult
{
    [JsonPropertyName("recommendations")]
    public IReadOnlyList<ProposalRecommendation> Recommendations { get; init; } = [];
}

public enum ProposalStatus
{
    SENT,
    VIEWED,
    SLOT_FILLED,
    CONVERTED,
    EXPIRED
}
