using Klau.Sdk.Common;

namespace Klau.Sdk.Proposals;

public interface IProposalClient
{
    Task<Proposal> CreateAsync(CreateProposalRequest request, CancellationToken ct = default);
    Task<ProposalListResult> ListAsync(ProposalStatus? status = null, int limit = 50, int offset = 0, CancellationToken ct = default);
    Task RemindAsync(string proposalId, RemindRequest? request = null, CancellationToken ct = default);
    Task UpdateOfferAsync(string proposalId, UpdateOfferRequest request, CancellationToken ct = default);
    Task ExpireAsync(string proposalId, CancellationToken ct = default);
    Task<PricingCalendarResult> GetPricingCalendarAsync(string offeringId, int containerSize, string? materialId = null, double? lat = null, double? lng = null, int days = 14, CancellationToken ct = default);
    Task<RecommendationsResult> GetRecommendationsAsync(string? type = null, CancellationToken ct = default);
    Task DismissRecommendationAsync(string proposalId, string type, CancellationToken ct = default);
}

/// <summary>
/// Proposals are the primary way to send customers a quote via SMS/email.
/// The customer receives a link to view pricing, pick a date, and book.
/// All endpoints require authentication (admin).
/// </summary>
public sealed class ProposalClient : IProposalClient
{
    private readonly KlauHttpClient _http;
    private readonly string? _tenantId;

    internal ProposalClient(KlauHttpClient http, string? tenantId = null)
    {
        _http = http;
        _tenantId = tenantId;
    }

    /// <summary>
    /// Create and send a proposal. The customer receives an SMS (and email if provided)
    /// with a link to view the quote and book online.
    /// </summary>
    public async Task<Proposal> CreateAsync(CreateProposalRequest request, CancellationToken ct = default)
    {
        return await _http.PostAsync<Proposal>("api/v1/proposals", request, _tenantId, ct);
    }

    /// <summary>
    /// List proposals with optional status filter.
    /// </summary>
    public async Task<ProposalListResult> ListAsync(
        ProposalStatus? status = null,
        int limit = 50,
        int offset = 0,
        CancellationToken ct = default)
    {
        var path = QueryBuilder.Build("api/v1/proposals",
            ("status", status),
            ("limit", limit),
            ("offset", offset));

        return await _http.GetAsync<ProposalListResult>(path, _tenantId, ct);
    }

    /// <summary>
    /// Send a reminder notification for a proposal that hasn't been booked yet.
    /// </summary>
    public async Task RemindAsync(string proposalId, RemindRequest? request = null, CancellationToken ct = default)
    {
        await _http.PostAsync($"api/v1/proposals/{proposalId}/remind", request ?? new RemindRequest(), _tenantId, ct);
    }

    /// <summary>
    /// Update the locked price on a proposal and notify the customer of the new pricing.
    /// </summary>
    public async Task UpdateOfferAsync(string proposalId, UpdateOfferRequest request, CancellationToken ct = default)
    {
        await _http.PostAsync($"api/v1/proposals/{proposalId}/update-offer", request, _tenantId, ct);
    }

    /// <summary>
    /// Manually expire a proposal (e.g. customer went with a competitor).
    /// </summary>
    public async Task ExpireAsync(string proposalId, CancellationToken ct = default)
    {
        await _http.PostAsync($"api/v1/proposals/{proposalId}/expire", null, _tenantId, ct);
    }

    /// <summary>
    /// Get the demand-shaped pricing calendar for a container size and offering.
    /// Shows optimal dates with discounts to help pick the best date for the proposal.
    /// </summary>
    public async Task<PricingCalendarResult> GetPricingCalendarAsync(
        string offeringId,
        int containerSize,
        string? materialId = null,
        double? lat = null,
        double? lng = null,
        int days = 14,
        CancellationToken ct = default)
    {
        var path = QueryBuilder.Build("api/v1/proposals/pricing-calendar",
            ("offeringId", offeringId),
            ("containerSize", containerSize),
            ("materialId", materialId),
            ("lat", lat),
            ("lng", lng),
            ("days", days));

        return await _http.GetAsync<PricingCalendarResult>(path, _tenantId, ct);
    }

    /// <summary>
    /// Get shepherd AI recommendations for proposals that need attention
    /// (follow-ups, expiring soon, price adjustments).
    /// </summary>
    public async Task<RecommendationsResult> GetRecommendationsAsync(
        string? type = null,
        CancellationToken ct = default)
    {
        var path = QueryBuilder.Build("api/v1/proposals/recommendations",
            ("type", type));

        return await _http.GetAsync<RecommendationsResult>(path, _tenantId, ct);
    }

    /// <summary>
    /// Dismiss a shepherd recommendation for a proposal.
    /// </summary>
    public async Task DismissRecommendationAsync(
        string proposalId,
        string type,
        CancellationToken ct = default)
    {
        await _http.PostAsync(
            $"api/v1/proposals/{proposalId}/dismiss-recommendation",
            new { type },
            _tenantId,
            ct);
    }
}
