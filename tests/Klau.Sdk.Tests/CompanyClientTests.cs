using System.Net;
using System.Text.Json;
using Klau.Sdk.Companies;
using Klau.Sdk.Tests.Helpers;

namespace Klau.Sdk.Tests;

public class CompanyClientTests
{
    private static (KlauClient client, MockHttpHandler handler) CreateClient()
    {
        var handler = new MockHttpHandler();
        var httpClient = new HttpClient(handler);
        var client = new KlauClient("kl_live_test", "https://api.test.com", httpClient);
        return (client, handler);
    }

    // --- GetAsync - Request ---

    [Fact]
    public async Task GetAsync_SendsGetToCorrectPath()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            id = "comp-1",
            name = "Acme Hauling",
            timezone = "America/Los_Angeles",
            workdayStart = "07:00",
            workdayEnd = "17:00",
            workdays = new[] { "MON", "TUE", "WED", "THU", "FRI" },
            containerSizes = new[] { 10, 20, 30, 40 },
            autoPublishDispatches = false,
            dispatchApprovalThreshold = 80,
            isFoundingMember = false
        });

        await client.Company.GetAsync();

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Get, req.Method);
        Assert.EndsWith("api/v1/companies/me", req.RequestUri!.AbsolutePath);
    }

    // --- GetAsync - Full deserialization ---

    [Fact]
    public async Task GetAsync_DeserializesFullResponse()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            id = "comp-abc",
            name = "Pacific Waste Services",
            address = "100 Harbor Blvd",
            city = "San Luis Obispo",
            state = "CA",
            zip = "93401",
            phone = "805-555-1234",
            smsPhoneNumber = "805-555-5678",
            serviceAreaNorth = 35.5,
            serviceAreaSouth = 34.8,
            serviceAreaEast = -120.0,
            serviceAreaWest = -121.0,
            timezone = "America/Los_Angeles",
            workdayStart = "06:30",
            workdayEnd = "18:00",
            workdays = new[] { "MON", "TUE", "WED", "THU", "FRI", "SAT" },
            containerSizes = new[] { 10, 15, 20, 30, 40 },
            jobBufferPercentage = 10.5,
            jobBufferFlatMinutes = 5.0,
            subscriptionStatus = "ACTIVE",
            subscriptionTier = "FULL",
            trialEndsAt = "2026-04-01T00:00:00Z",
            importServiceCodeMappings = new[]
            {
                new { externalCode = "DEL", klauJobType = "DELIVERY" },
                new { externalCode = "PU", klauJobType = "PICKUP" }
            },
            importContainerPatterns = new[]
            {
                new { pattern = "^20", size = (int?)20, skip = (bool?)false },
                new { pattern = "^SKIP", size = (int?)null, skip = (bool?)true }
            },
            autoPublishDispatches = true,
            dispatchApprovalThreshold = 85,
            isFoundingMember = true
        });

        var company = await client.Company.GetAsync();

        // Core Identity
        Assert.Equal("comp-abc", company.Id);
        Assert.Equal("Pacific Waste Services", company.Name);

        // Location & Contact
        Assert.Equal("100 Harbor Blvd", company.Address);
        Assert.Equal("San Luis Obispo", company.City);
        Assert.Equal("CA", company.State);
        Assert.Equal("93401", company.Zip);
        Assert.Equal("805-555-1234", company.Phone);
        Assert.Equal("805-555-5678", company.SmsPhoneNumber);

        // Service Area
        Assert.Equal(35.5, company.ServiceAreaNorth);
        Assert.Equal(34.8, company.ServiceAreaSouth);
        Assert.Equal(-120.0, company.ServiceAreaEast);
        Assert.Equal(-121.0, company.ServiceAreaWest);

        // Operating Hours
        Assert.Equal("America/Los_Angeles", company.Timezone);
        Assert.Equal("06:30", company.WorkdayStart);
        Assert.Equal("18:00", company.WorkdayEnd);
        Assert.Equal(6, company.Workdays.Count);
        Assert.Equal("MON", company.Workdays[0]);
        Assert.Equal("SAT", company.Workdays[5]);

        // Container Configuration
        Assert.Equal(5, company.ContainerSizes.Count);
        Assert.Equal(new[] { 10, 15, 20, 30, 40 }, company.ContainerSizes);

        // Job Buffering
        Assert.Equal(10.5, company.JobBufferPercentage);
        Assert.Equal(5.0, company.JobBufferFlatMinutes);

        // Subscription
        Assert.Equal("ACTIVE", company.SubscriptionStatus);
        Assert.Equal("FULL", company.SubscriptionTier);
        Assert.Equal("2026-04-01T00:00:00Z", company.TrialEndsAt);

        // Import Configuration
        Assert.NotNull(company.ImportServiceCodeMappings);
        Assert.Equal(2, company.ImportServiceCodeMappings!.Count);
        Assert.Equal("DEL", company.ImportServiceCodeMappings[0].ExternalCode);
        Assert.Equal("DELIVERY", company.ImportServiceCodeMappings[0].KlauJobType);
        Assert.Equal("PU", company.ImportServiceCodeMappings[1].ExternalCode);
        Assert.Equal("PICKUP", company.ImportServiceCodeMappings[1].KlauJobType);

        Assert.NotNull(company.ImportContainerPatterns);
        Assert.Equal(2, company.ImportContainerPatterns!.Count);
        Assert.Equal("^20", company.ImportContainerPatterns[0].Pattern);
        Assert.Equal(20, company.ImportContainerPatterns[0].Size);
        Assert.False(company.ImportContainerPatterns[0].Skip);
        Assert.Equal("^SKIP", company.ImportContainerPatterns[1].Pattern);
        Assert.Null(company.ImportContainerPatterns[1].Size);
        Assert.True(company.ImportContainerPatterns[1].Skip);

        // Dispatch Automation
        Assert.True(company.AutoPublishDispatches);
        Assert.Equal(85, company.DispatchApprovalThreshold);

        // Metadata
        Assert.True(company.IsFoundingMember);
    }

    // --- GetAsync - containerSizes deserialization ---

    [Fact]
    public async Task GetAsync_DeserializesContainerSizesCorrectly()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            id = "comp-sizes",
            name = "Size Test Co",
            timezone = "America/New_York",
            workdayStart = "08:00",
            workdayEnd = "16:00",
            workdays = new[] { "MON", "TUE", "WED" },
            containerSizes = new[] { 10, 15, 20, 30, 40 },
            autoPublishDispatches = false,
            dispatchApprovalThreshold = 70,
            isFoundingMember = false
        });

        var company = await client.Company.GetAsync();

        Assert.Equal(5, company.ContainerSizes.Count);
        Assert.Equal(10, company.ContainerSizes[0]);
        Assert.Equal(15, company.ContainerSizes[1]);
        Assert.Equal(20, company.ContainerSizes[2]);
        Assert.Equal(30, company.ContainerSizes[3]);
        Assert.Equal(40, company.ContainerSizes[4]);
    }

    // --- UpdateAsync - Request ---

    [Fact]
    public async Task UpdateAsync_SendsPatchToCorrectPath()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            id = "comp-1",
            name = "Acme Hauling",
            timezone = "America/Chicago",
            workdayStart = "07:00",
            workdayEnd = "17:00",
            workdays = new[] { "MON", "TUE", "WED", "THU", "FRI" },
            containerSizes = new[] { 10, 20, 30, 40 },
            autoPublishDispatches = false,
            dispatchApprovalThreshold = 80,
            isFoundingMember = false
        });

        await client.Company.UpdateAsync(new UpdateCompanyRequest
        {
            Timezone = "America/Chicago"
        });

        var req = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Patch, req.Method);
        Assert.EndsWith("api/v1/companies/me", req.RequestUri!.AbsolutePath);
    }

    // --- UpdateAsync - containerSizes serialization ---

    [Fact]
    public async Task UpdateAsync_SerializesContainerSizesInRequestBody()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            id = "comp-1",
            name = "Acme Hauling",
            timezone = "America/Los_Angeles",
            workdayStart = "07:00",
            workdayEnd = "17:00",
            workdays = new[] { "MON", "TUE", "WED", "THU", "FRI" },
            containerSizes = new[] { 10, 20, 30, 40, 50 },
            autoPublishDispatches = false,
            dispatchApprovalThreshold = 80,
            isFoundingMember = false
        });

        await client.Company.UpdateAsync(new UpdateCompanyRequest
        {
            ContainerSizes = [10, 20, 30, 40, 50]
        });

        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var sizes = root.GetProperty("containerSizes");
        Assert.Equal(5, sizes.GetArrayLength());
        Assert.Equal(10, sizes[0].GetInt32());
        Assert.Equal(20, sizes[1].GetInt32());
        Assert.Equal(30, sizes[2].GetInt32());
        Assert.Equal(40, sizes[3].GetInt32());
        Assert.Equal(50, sizes[4].GetInt32());
    }

    // --- UpdateAsync - omits null fields ---

    [Fact]
    public async Task UpdateAsync_OmitsNullFields()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            id = "comp-1",
            name = "Acme Hauling",
            timezone = "America/Denver",
            workdayStart = "07:00",
            workdayEnd = "17:00",
            workdays = new[] { "MON", "TUE", "WED", "THU", "FRI" },
            containerSizes = new[] { 10, 20, 30, 40 },
            autoPublishDispatches = false,
            dispatchApprovalThreshold = 80,
            isFoundingMember = false
        });

        // Only set timezone — all other fields should be omitted
        await client.Company.UpdateAsync(new UpdateCompanyRequest
        {
            Timezone = "America/Denver"
        });

        var body = handler.SentBodies[0]!;
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        // timezone should be present
        Assert.Equal("America/Denver", root.GetProperty("timezone").GetString());

        // All other optional fields should not be present
        Assert.False(root.TryGetProperty("containerSizes", out _));
        Assert.False(root.TryGetProperty("workdayStart", out _));
        Assert.False(root.TryGetProperty("workdayEnd", out _));
        Assert.False(root.TryGetProperty("workdays", out _));
        Assert.False(root.TryGetProperty("jobBufferPercentage", out _));
        Assert.False(root.TryGetProperty("jobBufferFlatMinutes", out _));
        Assert.False(root.TryGetProperty("serviceAreaNorth", out _));
        Assert.False(root.TryGetProperty("serviceAreaSouth", out _));
        Assert.False(root.TryGetProperty("serviceAreaEast", out _));
        Assert.False(root.TryGetProperty("serviceAreaWest", out _));
        Assert.False(root.TryGetProperty("importServiceCodeMappings", out _));
        Assert.False(root.TryGetProperty("importContainerPatterns", out _));
        Assert.False(root.TryGetProperty("autoPublishDispatches", out _));
        Assert.False(root.TryGetProperty("dispatchApprovalThreshold", out _));
    }

    // --- TenantScope ---

    [Fact]
    public async Task GetAsync_TenantScope_SendsTenantHeader()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            id = "comp-tenant",
            name = "Tenant Co",
            timezone = "America/Los_Angeles",
            workdayStart = "07:00",
            workdayEnd = "17:00",
            workdays = new[] { "MON", "TUE", "WED", "THU", "FRI" },
            containerSizes = new[] { 20, 30 },
            autoPublishDispatches = false,
            dispatchApprovalThreshold = 75,
            isFoundingMember = false
        });

        var scope = client.ForTenant("tenant-abc");
        await scope.Company.GetAsync();

        var req = Assert.Single(handler.SentRequests);
        Assert.True(req.Headers.Contains("Klau-Tenant-Id"));
        Assert.Equal("tenant-abc", req.Headers.GetValues("Klau-Tenant-Id").First());
    }

    [Fact]
    public async Task UpdateAsync_TenantScope_SendsTenantHeader()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.OK, new
        {
            id = "comp-tenant",
            name = "Tenant Co",
            timezone = "America/Chicago",
            workdayStart = "07:00",
            workdayEnd = "17:00",
            workdays = new[] { "MON", "TUE", "WED", "THU", "FRI" },
            containerSizes = new[] { 20, 30 },
            autoPublishDispatches = true,
            dispatchApprovalThreshold = 90,
            isFoundingMember = false
        });

        var scope = client.ForTenant("tenant-xyz");
        await scope.Company.UpdateAsync(new UpdateCompanyRequest
        {
            Timezone = "America/Chicago"
        });

        var req = Assert.Single(handler.SentRequests);
        Assert.True(req.Headers.Contains("Klau-Tenant-Id"));
        Assert.Equal("tenant-xyz", req.Headers.GetValues("Klau-Tenant-Id").First());
    }
}
