using System.Net;
using System.Text.Json;
using Klau.Sdk.Customers;
using Klau.Sdk.Tests.Helpers;

namespace Klau.Sdk.Tests;

public class CustomerClientTests
{
    private static (KlauClient client, MockHttpHandler handler) CreateClient()
    {
        var handler = new MockHttpHandler();
        var httpClient = new HttpClient(handler);
        var client = new KlauClient("kl_live_test", "https://api.test.com", httpClient);
        return (client, handler);
    }

    // --- ListAsync ---

    [Fact]
    public async Task ListAsync_SendsCorrectPath()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new List<object>(),
            new { total = 0, page = 1, pageSize = 100, hasMore = false });

        await client.Customers.ListAsync();

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Get, req.Method);
        Assert.Contains("api/v1/customers", req.RequestUri!.ToString());
    }

    [Fact]
    public async Task ListAsync_IncludesSearchParam()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new List<object>(),
            new { total = 0, page = 1, pageSize = 100, hasMore = false });

        await client.Customers.ListAsync(search: "Acme");

        var req = Assert.Single(handler.SentRequests);
        Assert.Contains("search=Acme", req.RequestUri!.Query);
    }

    [Fact]
    public async Task ListAsync_IncludesIncludeInactiveParam()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new List<object>(),
            new { total = 0, page = 1, pageSize = 100, hasMore = false });

        await client.Customers.ListAsync(includeInactive: true);

        var req = Assert.Single(handler.SentRequests);
        Assert.Contains("includeInactive=True", req.RequestUri!.Query);
    }

    // --- GetAsync ---

    [Fact]
    public async Task GetAsync_SendsCorrectPath()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new { id = "cust-1", name = "Acme Corp",
            isActive = true, createdAt = "2026-01-01T00:00:00Z", updatedAt = "2026-01-01T00:00:00Z" });

        var customer = await client.Customers.GetAsync("cust-1");

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Get, req.Method);
        Assert.EndsWith("api/v1/customers/cust-1", req.RequestUri!.AbsolutePath);
        Assert.Equal("Acme Corp", customer.Name);
    }

    // --- CreateAsync ---

    [Fact]
    public async Task CreateAsync_SendsPostWithRequiredName()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new { id = "cust-new", name = "New Customer",
            isActive = true, createdAt = "2026-01-01T00:00:00Z", updatedAt = "2026-01-01T00:00:00Z" });

        var request = new CreateCustomerRequest
        {
            Name = "New Customer",
            ContactName = "John Doe",
            ContactPhone = "555-1234",
            ContactEmail = "john@example.com"
        };
        var customer = await client.Customers.CreateAsync(request);

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.EndsWith("api/v1/customers", req.RequestUri!.AbsolutePath);

        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        Assert.Equal("New Customer", root.GetProperty("name").GetString());
        Assert.Equal("John Doe", root.GetProperty("contactName").GetString());
        Assert.Equal("555-1234", root.GetProperty("contactPhone").GetString());
        Assert.Equal("john@example.com", root.GetProperty("contactEmail").GetString());

        Assert.Equal("New Customer", customer.Name);
    }

    // --- ExternalId deserialization ---

    [Fact]
    public async Task CustomerModel_DeserializesExternalId()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new { id = "cust-1", name = "Acme",
            externalId = "ext-acme-42", isActive = true,
            createdAt = "2026-01-01T00:00:00Z", updatedAt = "2026-01-01T00:00:00Z" });

        var customer = await client.Customers.GetAsync("cust-1");

        Assert.Equal("ext-acme-42", customer.ExternalId);
    }

    [Fact]
    public async Task CustomerModel_NullExternalId()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new { id = "cust-1", name = "Acme",
            isActive = true, createdAt = "2026-01-01T00:00:00Z", updatedAt = "2026-01-01T00:00:00Z" });

        var customer = await client.Customers.GetAsync("cust-1");

        Assert.Null(customer.ExternalId);
    }

    // --- UpdateAsync ---

    [Fact]
    public async Task UpdateAsync_UsesPatchMethod()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new { id = "cust-1", name = "Updated Name",
            isActive = true, createdAt = "2026-01-01T00:00:00Z", updatedAt = "2026-01-02T00:00:00Z" });

        var request = new UpdateCustomerRequest { Name = "Updated Name" };
        var customer = await client.Customers.UpdateAsync("cust-1", request);

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Patch, req.Method);
        Assert.EndsWith("api/v1/customers/cust-1", req.RequestUri!.AbsolutePath);
        Assert.Equal("Updated Name", customer.Name);
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_SendsDeleteToCorrectPath()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK);

        await client.Customers.DeleteAsync("cust-1");

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Delete, req.Method);
        Assert.EndsWith("api/v1/customers/cust-1", req.RequestUri!.AbsolutePath);
    }

    // --- Get360Async ---

    [Fact]
    public async Task Get360Async_SendsCorrectPath()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new {
            customer = new { id = "cust-1", name = "Acme", isActive = true,
                createdAt = "2026-01-01T00:00:00Z", updatedAt = "2026-01-01T00:00:00Z" },
            healthScore = 85,
            lifecycleStage = "active",
            totalOrders = 12,
            totalRevenueCents = 450000
        });

        var c360 = await client.Customers.Get360Async("cust-1");

        var req = Assert.Single(handler.SentRequests);
        Assert.EndsWith("api/v1/customers/cust-1/360", req.RequestUri!.AbsolutePath);
        Assert.Equal(85, c360.HealthScore);
        Assert.Equal(12, c360.TotalOrders);
    }

    // --- ListSitesAsync ---

    [Fact]
    public async Task ListSitesAsync_SendsCorrectPath()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new[]
        {
            new { id = "site-1", address = "123 Main St", customerId = "cust-1" }
        });

        var sites = await client.Customers.ListSitesAsync("cust-1");

        var req = Assert.Single(handler.SentRequests);
        Assert.EndsWith("api/v1/customers/cust-1/sites", req.RequestUri!.AbsolutePath);
        Assert.Single(sites);
        Assert.Equal("123 Main St", sites[0].Address);
    }
}
