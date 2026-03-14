using System.Net;
using Klau.Sdk.Tests.Helpers;

namespace Klau.Sdk.Tests;

public class TenantScopeTests
{
    private static (KlauClient client, MockHttpHandler handler) CreateClient()
    {
        var handler = new MockHttpHandler();
        var httpClient = new HttpClient(handler);
        var client = new KlauClient("kl_live_test", "https://api.test.com", httpClient);
        return (client, handler);
    }

    private static readonly object SuccessJob = new
    {
        id = "j-1", type = "DELIVERY", status = "UNASSIGNED",
        customerName = "Test",
        createdAt = "2026-01-01T00:00:00Z", updatedAt = "2026-01-01T00:00:00Z"
    };

    [Fact]
    public async Task ForTenant_SendsTenantIdHeader()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, SuccessJob);

        var scope = client.ForTenant("tenant-abc");
        await scope.Jobs.GetAsync("j-1");

        var req = Assert.Single(handler.SentRequests);
        Assert.True(req.Headers.Contains("Klau-Tenant-Id"));
        Assert.Equal("tenant-abc", req.Headers.GetValues("Klau-Tenant-Id").First());
    }

    [Fact]
    public async Task TwoTenantScopes_DoNotInterfere()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, SuccessJob);
        handler.EnqueueResponse(HttpStatusCode.OK, SuccessJob);

        var scope1 = client.ForTenant("tenant-a");
        var scope2 = client.ForTenant("tenant-b");

        await scope1.Jobs.GetAsync("j-1");
        await scope2.Jobs.GetAsync("j-1");

        Assert.Equal(2, handler.SentRequests.Count);

        var req1 = handler.SentRequests[0];
        var req2 = handler.SentRequests[1];

        Assert.Equal("tenant-a", req1.Headers.GetValues("Klau-Tenant-Id").First());
        Assert.Equal("tenant-b", req2.Headers.GetValues("Klau-Tenant-Id").First());
    }

    [Fact]
    public async Task SetTenant_SetsDefaultHeaderOnRequests()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, SuccessJob);

        client.SetTenant("tenant-default");
        await client.Jobs.GetAsync("j-1");

        var req = Assert.Single(handler.SentRequests);
        // The default tenant header is set on the HttpClient's DefaultRequestHeaders,
        // which gets merged into the request.
        // We verify via the request URI that it was sent (the header may be on DefaultRequestHeaders).
        // Since KlauHttpClient also applies via ApplyTenantHeader when _defaultTenantId is set,
        // the per-request header should be present.
        Assert.True(
            req.Headers.Contains("Klau-Tenant-Id"),
            "Request should contain Klau-Tenant-Id header after SetTenant");
    }

    [Fact]
    public async Task ClearTenant_RemovesHeader()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, SuccessJob);

        client.SetTenant("tenant-temp");
        client.ClearTenant();
        await client.Jobs.GetAsync("j-1");

        var req = Assert.Single(handler.SentRequests);
        // After ClearTenant, no tenant header should be applied per-request
        Assert.False(req.Headers.Contains("Klau-Tenant-Id"));
    }

    [Fact]
    public async Task ForTenant_OverridesDefaultTenant()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, SuccessJob);

        client.SetTenant("tenant-default");
        var scope = client.ForTenant("tenant-override");
        await scope.Jobs.GetAsync("j-1");

        var req = Assert.Single(handler.SentRequests);
        // The per-request override should win
        var tenantValues = req.Headers.GetValues("Klau-Tenant-Id").ToList();
        Assert.Contains("tenant-override", tenantValues);
    }

    [Fact]
    public async Task TenantScope_WorksWithCustomerClient()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new { id = "cust-1", name = "Acme",
            isActive = true, createdAt = "2026-01-01T00:00:00Z", updatedAt = "2026-01-01T00:00:00Z" });

        var scope = client.ForTenant("tenant-xyz");
        var customer = await scope.Customers.GetAsync("cust-1");

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal("tenant-xyz", req.Headers.GetValues("Klau-Tenant-Id").First());
        Assert.Equal("Acme", customer.Name);
    }

    [Fact]
    public async Task DefaultClient_NoTenantHeader_WhenNoneSet()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, SuccessJob);

        await client.Jobs.GetAsync("j-1");

        var req = Assert.Single(handler.SentRequests);
        Assert.False(req.Headers.Contains("Klau-Tenant-Id"));
    }
}
