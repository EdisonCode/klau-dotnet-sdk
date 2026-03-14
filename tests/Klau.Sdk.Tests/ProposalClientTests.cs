using System.Net;
using System.Text.Json;
using Klau.Sdk.Proposals;
using Klau.Sdk.Tests.Helpers;

namespace Klau.Sdk.Tests;

public class ProposalClientTests
{
    private static (KlauClient client, MockHttpHandler handler) CreateClient()
    {
        var handler = new MockHttpHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.test.com") };
        var client = new KlauClient("kl_live_test", "https://api.test.com", http);
        return (client, handler);
    }

    [Fact]
    public async Task CreateAsync_SendsPostToCorrectPath()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.Created, new
        {
            id = "prop-1",
            status = "SENT",
            customerName = "John Doe",
            customerPhone = "555-0100",
            containerSize = 30,
            lockedPriceCents = 45000,
            expiresAt = "2026-03-20T00:00:00Z",
            proposalLink = "https://klau.rolloff.app/proposals/abc123",
            createdAt = "2026-03-13T00:00:00Z"
        });

        var result = await client.Proposals.CreateAsync(new CreateProposalRequest
        {
            CustomerName = "John Doe",
            CustomerPhone = "555-0100",
            CustomerEmail = "john@example.com",
            ServiceAddress = new ProposalAddress
            {
                Street = "123 Main St",
                City = "Portland",
                State = "OR",
                Zip = "97201"
            },
            ContainerSize = 30,
            ProposedDate = "2026-03-14",
            OfferingId = "offer-1",
            Notes = "Driveway delivery"
        });

        Assert.Equal("prop-1", result.Id);
        Assert.Equal(ProposalStatus.SENT, result.Status);
        Assert.Equal(45000, result.LockedPriceCents);

        var req = handler.SentRequests[0];
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.Equal("/api/v1/proposals", req.RequestUri!.AbsolutePath);

        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("John Doe", doc.RootElement.GetProperty("customerName").GetString());
        Assert.Equal("john@example.com", doc.RootElement.GetProperty("customerEmail").GetString());
        Assert.Equal("offer-1", doc.RootElement.GetProperty("offeringId").GetString());
    }

    [Fact]
    public async Task ListAsync_IncludesStatusFilter()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            proposals = new[] { new { id = "prop-1", status = "SENT", customerName = "Test",
                customerPhone = "555", containerSize = 20, lockedPriceCents = 30000,
                expiresAt = "2026-03-20T00:00:00Z", createdAt = "2026-03-13T00:00:00Z" } },
            meta = new { total = 1, limit = 50, offset = 0 }
        });

        await client.Proposals.ListAsync(status: ProposalStatus.SENT);

        var req = handler.SentRequests[0];
        Assert.Contains("status=SENT", req.RequestUri!.Query);
    }

    [Fact]
    public async Task RemindAsync_SendsPostToCorrectPath()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK);

        await client.Proposals.RemindAsync("prop-1", new RemindRequest { Note = "Following up" });

        var req = handler.SentRequests[0];
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.Equal("/api/v1/proposals/prop-1/remind", req.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task UpdateOfferAsync_SendsNewPrice()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK);

        await client.Proposals.UpdateOfferAsync("prop-1", new UpdateOfferRequest
        {
            NewPriceCents = 35000,
            Note = "Adjusted for market"
        });

        var req = handler.SentRequests[0];
        Assert.Equal("/api/v1/proposals/prop-1/update-offer", req.RequestUri!.AbsolutePath);

        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(35000, doc.RootElement.GetProperty("newPriceCents").GetInt32());
    }

    [Fact]
    public async Task ExpireAsync_SendsPostToCorrectPath()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK);

        await client.Proposals.ExpireAsync("prop-1");

        var req = handler.SentRequests[0];
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.Equal("/api/v1/proposals/prop-1/expire", req.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetPricingCalendarAsync_IncludesQueryParams()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            calendar = new[] { new { date = "2026-03-14", priceCents = 45000,
                basePriceCents = 50000, discountCents = 5000, isOptimal = true,
                tier = "OPTIMAL", available = true } }
        });

        var result = await client.Proposals.GetPricingCalendarAsync(
            offeringId: "offer-1",
            containerSize: 30,
            lat: 45.5,
            lng: -122.6,
            days: 7);

        var req = handler.SentRequests[0];
        Assert.Contains("offeringId=offer-1", req.RequestUri!.Query);
        Assert.Contains("containerSize=30", req.RequestUri!.Query);
        Assert.Contains("days=7", req.RequestUri!.Query);

        Assert.Single(result.Calendar);
        Assert.True(result.Calendar[0].IsOptimal);
        Assert.Equal(5000, result.Calendar[0].DiscountCents);
    }

    [Fact]
    public async Task TenantScope_ProposalsSendTenantHeader()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            proposals = Array.Empty<object>(),
            meta = new { total = 0, limit = 50, offset = 0 }
        });

        var scope = client.ForTenant("div-portland");
        await scope.Proposals.ListAsync();

        var req = handler.SentRequests[0];
        Assert.True(req.Headers.Contains("Klau-Tenant-Id"));
        Assert.Equal("div-portland", req.Headers.GetValues("Klau-Tenant-Id").First());
    }

    [Fact]
    public async Task GetRecommendationsAsync_ReturnsRecommendations()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            recommendations = new[] { new { proposalId = "prop-1", type = "follow_up",
                message = "No response in 3 days", priority = "HIGH" } }
        });

        var result = await client.Proposals.GetRecommendationsAsync();

        Assert.Single(result.Recommendations);
        Assert.Equal("follow_up", result.Recommendations[0].Type);
    }

    [Fact]
    public async Task DismissRecommendationAsync_SendsCorrectBody()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK);

        await client.Proposals.DismissRecommendationAsync("prop-1", "follow_up");

        var req = handler.SentRequests[0];
        Assert.Equal("/api/v1/proposals/prop-1/dismiss-recommendation", req.RequestUri!.AbsolutePath);

        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("follow_up", doc.RootElement.GetProperty("type").GetString());
    }
}
