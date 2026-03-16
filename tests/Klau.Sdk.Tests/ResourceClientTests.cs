using System.Net;
using System.Text.Json;
using Klau.Sdk.Drivers;
using Klau.Sdk.DumpSites;
using Klau.Sdk.Trucks;
using Klau.Sdk.Yards;
using Klau.Sdk.Tests.Helpers;

namespace Klau.Sdk.Tests;

/// <summary>
/// Tests for Driver, Truck, Yard, and DumpSite resource clients.
/// These follow the same pattern as existing client tests.
/// </summary>
public class ResourceClientTests
{
    private static (KlauClient client, MockHttpHandler handler) CreateClient()
    {
        var handler = new MockHttpHandler();
        var httpClient = new HttpClient(handler);
        var client = new KlauClient("kl_live_test", "https://api.test.com", httpClient);
        return (client, handler);
    }

    // ─── Drivers ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Drivers_CreateAsync_ReturnsDriverId()
    {
        var (client, handler) = CreateClient();
        // API returns { data: { driverId: "drv-1" } }
        handler.EnqueueResponse(HttpStatusCode.OK, new { driverId = "drv-1" });

        var driverId = await client.Drivers.CreateAsync(new CreateDriverRequest
        {
            Name = "John Smith",
            DefaultTruckId = "trk-1",
            HomeYardId = "yard-1",
        });

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.EndsWith("api/v1/drivers", req.RequestUri!.AbsolutePath);

        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("John Smith", doc.RootElement.GetProperty("name").GetString());
        Assert.Equal("trk-1", doc.RootElement.GetProperty("defaultTruckId").GetString());
        Assert.Equal("yard-1", doc.RootElement.GetProperty("homeYardId").GetString());

        Assert.Equal("drv-1", driverId);
    }

    [Fact]
    public async Task Drivers_ListAsync_ReturnsPagedResult()
    {
        var (client, handler) = CreateClient();
        // API returns { data: { drivers: [...], total: 1 } }
        handler.EnqueueResponse(HttpStatusCode.OK,
            new { drivers = new[] { new { id = "drv-1", name = "John", isActive = true,
                createdAt = "2026-01-01T00:00:00Z", updatedAt = "2026-01-01T00:00:00Z" } }, total = 1 });

        var result = await client.Drivers.ListAsync();

        Assert.Single(result.Items);
        Assert.Equal("John", result.Items[0].Name);
        Assert.Equal(1, result.Total);
    }

    // ─── Trucks ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Trucks_CreateAsync_ReturnsTruckId()
    {
        var (client, handler) = CreateClient();
        // API returns { data: { truckId: "trk-1" } }
        handler.EnqueueResponse(HttpStatusCode.OK, new { truckId = "trk-1" });

        var truckId = await client.Trucks.CreateAsync(new CreateTruckRequest
        {
            Number = "T-001",
            CompatibleSizes = [20, 30, 40],
            HomeYardId = "yard-1",
        });

        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("T-001", doc.RootElement.GetProperty("number").GetString());
        var sizes = doc.RootElement.GetProperty("compatibleSizes");
        Assert.Equal(3, sizes.GetArrayLength());

        Assert.Equal("trk-1", truckId);
    }

    [Fact]
    public async Task Trucks_ListAsync_ReturnsPagedResult()
    {
        var (client, handler) = CreateClient();
        // API returns { data: { trucks: [...], total: 1 } }
        handler.EnqueueResponse(HttpStatusCode.OK,
            new { trucks = new[] { new { id = "trk-1", number = "T-001", compatibleSizes = new[] { 20, 30, 40 }, isActive = true,
                createdAt = "2026-01-01T00:00:00Z", updatedAt = "2026-01-01T00:00:00Z" } }, total = 1 });

        var result = await client.Trucks.ListAsync();

        Assert.Single(result.Items);
        Assert.Equal("T-001", result.Items[0].Number);
        Assert.Equal(3, result.Items[0].CompatibleSizes.Count);
    }

    // ─── Yards ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Yards_CreateAsync_ReturnsYardId()
    {
        var (client, handler) = CreateClient();
        // API returns { data: { yardId: "yard-1" } }
        handler.EnqueueResponse(HttpStatusCode.OK, new { yardId = "yard-1" });

        var yardId = await client.Yards.CreateAsync(new CreateYardRequest
        {
            Name = "Main Yard",
            Address = "100 Industrial Blvd",
            City = "Harrisburg",
            State = "PA",
            Zip = "17101",
            IsDefault = true,
        });

        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.GetProperty("isDefault").GetBoolean());

        Assert.Equal("yard-1", yardId);
    }

    [Fact]
    public async Task Yards_GetAsync_DeserializesLatLng()
    {
        var (client, handler) = CreateClient();
        // API returns latitude/longitude (not lat/lng)
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            id = "yard-1", name = "Main Yard", address = "100 Industrial Blvd",
            latitude = 40.2732, longitude = -76.8867,
            isDefault = true, isActive = true,
            createdAt = "2026-01-01T00:00:00Z", updatedAt = "2026-01-01T00:00:00Z"
        });

        var yard = await client.Yards.GetAsync("yard-1");

        Assert.Equal(40.2732, yard.Latitude);
        Assert.Equal(-76.8867, yard.Longitude);
    }

    [Fact]
    public async Task Yards_ListAsync_ReturnsPagedResult()
    {
        var (client, handler) = CreateClient();
        // API returns { data: { yards: [...], total: 1 } }
        handler.EnqueueResponse(HttpStatusCode.OK,
            new { yards = new[] { new { id = "yard-1", name = "Main Yard", address = "100 Industrial Blvd",
                latitude = 40.2732, longitude = -76.8867,
                isDefault = true, isActive = true,
                createdAt = "2026-01-01T00:00:00Z", updatedAt = "2026-01-01T00:00:00Z" } }, total = 1 });

        var result = await client.Yards.ListAsync();

        Assert.Single(result.Items);
        Assert.Equal("Main Yard", result.Items[0].Name);
        Assert.Equal(40.2732, result.Items[0].Latitude);
    }

    // ─── Dump Sites ────────────────────────────────────────────────────────

    [Fact]
    public async Task DumpSites_CreateAsync_ReturnsDumpSiteId()
    {
        var (client, handler) = CreateClient();
        // API returns { data: { dumpSiteId: "ds-1", geocoded: true } }
        handler.EnqueueResponse(HttpStatusCode.OK, new { dumpSiteId = "ds-1", geocoded = true });

        var dumpSiteId = await client.DumpSites.CreateAsync(new CreateDumpSiteRequest
        {
            Name = "Central Landfill",
            Address = "500 Dump Rd",
            OpenTime = "06:00",
            CloseTime = "18:00",
            AvgWaitMinutes = 15,
            AcceptedSizes = [20, 30, 40],
            SiteType = "LANDFILL",
        });

        var req = Assert.Single(handler.SentRequests);
        Assert.EndsWith("api/v1/dump-sites", req.RequestUri!.AbsolutePath);
        Assert.Equal("ds-1", dumpSiteId);
    }

    [Fact]
    public async Task DumpSites_ListAsync_ReturnsPagedResult()
    {
        var (client, handler) = CreateClient();
        // API returns { data: { dumpSites: [...], total: 1 } }
        handler.EnqueueResponse(HttpStatusCode.OK,
            new { dumpSites = new[] { new { id = "ds-1", name = "Central Landfill", address = "500 Dump Rd",
                acceptedSizes = new[] { 20, 30, 40 }, materialPricing = Array.Empty<object>(),
                isActive = true, createdAt = "2026-01-01T00:00:00Z", updatedAt = "2026-01-01T00:00:00Z" } }, total = 1 });

        var result = await client.DumpSites.ListAsync();

        Assert.Single(result.Items);
        Assert.Equal("Central Landfill", result.Items[0].Name);
    }

    [Fact]
    public async Task DumpSites_AddMaterialPricingAsync_SendsCorrectPath()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            id = "mp-1", dumpSiteId = "ds-1", materialId = "mat-1",
            pricePerUnitCents = 4500, unit = "TON",
            createdAt = "2026-01-01T00:00:00Z", updatedAt = "2026-01-01T00:00:00Z"
        });

        var pricing = await client.DumpSites.AddMaterialPricingAsync("ds-1", new AddMaterialPricingRequest
        {
            MaterialId = "mat-1",
            PricePerUnitCents = 4500,
            Unit = "TON",
        });

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.Contains("dump-sites/ds-1/material-pricing", req.RequestUri!.ToString());
        Assert.Equal(4500, pricing.PricePerUnitCents);
    }

    // ─── Tenant Scope ──────────────────────────────────────────────────────

    [Fact]
    public async Task TenantScope_ExposesAllResourceClients()
    {
        var (client, handler) = CreateClient();
        // API returns { data: { drivers: [...], total: 1 } }
        handler.EnqueueResponse(HttpStatusCode.OK,
            new { drivers = new[] { new { id = "drv-1", name = "Driver", isActive = true,
                createdAt = "2026-01-01T00:00:00Z", updatedAt = "2026-01-01T00:00:00Z" } }, total = 1 });

        var scope = client.ForTenant("tenant-1");
        var drivers = await scope.Drivers.ListAsync();

        Assert.Single(drivers.Items);
        var req = Assert.Single(handler.SentRequests);
        Assert.Equal("tenant-1", req.Headers.GetValues("Klau-Tenant-Id").Single());
    }
}
