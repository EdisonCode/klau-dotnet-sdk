# Klau .NET SDK

Official .NET SDK for the [Klau](https://getklau.com) API. Built for dev teams integrating roll-off waste hauling operations into .NET applications.

```bash
dotnet add package Klau.Sdk
```

Requires .NET 9.0+. Dependencies: `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Diagnostics.HealthChecks`.

## Quick Start

Generate an API key in **Settings > Developer** in your Klau dashboard. API keys start with `kl_live_`, are scoped to specific permissions, and can be revoked without affecting user credentials.

```csharp
using Klau.Sdk;

using var klau = new KlauClient("kl_live_your_api_key_here");

// Get today's dispatch board
var board = await klau.Dispatches.GetBoardAsync("2026-03-15");

foreach (var driver in board.Drivers)
    Console.WriteLine($"{driver.Name}: {driver.Jobs.Count} jobs, {driver.TotalDriveMinutes} min drive");
```

## Integration Guide: Jobs In, Assignments Out

Most integrations follow the same pattern: push work orders from your backend into Klau, run dispatch optimization, then read back driver assignments. This section walks through the entire flow.

### Step 1: Push jobs into Klau

Use `ExternalId` on every job to correlate Klau records with your system's IDs. This is the key to reliable two-way sync.

```csharp
// Single job
var job = await klau.Jobs.CreateAsync(new CreateJobRequest
{
    CustomerId = "customer-id",
    SiteId = "site-id",
    Type = JobType.DELIVERY,
    ContainerSize = 20,
    RequestedDate = "2026-03-15",
    TimeWindow = TimeWindow.MORNING,
    ExternalId = "YOUR-WORK-ORDER-123"  // your system's ID
});

// Batch — send up to 100 jobs in one call
var result = await klau.Jobs.CreateBatchAsync(new List<CreateJobRequest>
{
    new()
    {
        CustomerId = "cust-1",
        SiteId = "site-1",
        Type = JobType.DELIVERY,
        ContainerSize = 20,
        RequestedDate = "2026-03-15",
        ExternalId = "WO-1001"
    },
    new()
    {
        CustomerId = "cust-2",
        SiteId = "site-2",
        Type = JobType.PICKUP,
        ContainerSize = 30,
        RequestedDate = "2026-03-15",
        ExternalId = "WO-1002"
    }
});

// Check for partial failures
foreach (var created in result.Created)
    Console.WriteLine($"Created {created.JobId} (external: {created.ExternalId})");

foreach (var error in result.Errors)
    Console.WriteLine($"Failed index {error.Index}: {error.Code} - {error.Message}");
```

### Alternative: Bulk import with auto-created customers/sites

If your external system doesn't map to Klau customer/site IDs, use the import API instead. It resolves by name, auto-creates missing records, and waits for drive-time cache warm-up so optimization uses accurate truck routing times:

```csharp
var import = await klau.Import.ImportAndWaitAsync(new ImportJobsRequest
{
    Jobs =
    [
        new ImportJobRecord
        {
            CustomerName = "Acme Construction",
            SiteName = "Main Office",
            SiteAddress = "456 Industrial Way",
            SiteCity = "San Luis Obispo",
            SiteState = "CA",
            SiteZip = "93401",
            JobType = "DELIVERY",
            ContainerSize = "20",
            TimeWindow = "MORNING",
            ExternalId = "WO-1001"
        },
        new ImportJobRecord
        {
            CustomerName = "Acme Construction",
            SiteName = "Warehouse",
            SiteAddress = "789 Commerce Dr",
            SiteCity = "San Luis Obispo",
            SiteState = "CA",
            SiteZip = "93401",
            JobType = "PICKUP",
            ContainerSize = "30",
            ExternalId = "WO-1002"
        }
    ]
});

Console.WriteLine($"Imported: {import.Imported}, Created: {import.CustomersCreated} customers, {import.SitesCreated} sites");

// Drive-time cache is warm — safe to optimize now
```

`ImportAndWaitAsync` chains three steps: import jobs, poll the batch readiness endpoint until the drive-time cache is warm for any new sites, then return. If you need more control, call `JobsAsync` and `GetReadinessAsync` separately:

```csharp
var result = await klau.Import.JobsAsync(request);

if (result.BatchId is not null)
{
    BatchReadiness readiness;
    do
    {
        await Task.Delay(2000);
        readiness = await klau.Import.GetReadinessAsync(result.BatchId);
        Console.WriteLine($"Cache: {readiness.SitesCached}/{readiness.SitesTotal} sites ready");
    }
    while (readiness.Status is "warming" or "partial");
}
```

### Step 2: Optimize dispatch

Optimization is async. `OptimizeAndWaitAsync` handles polling for you.

```csharp
var optimization = await klau.Dispatches.OptimizeAndWaitAsync(new OptimizeRequest
{
    Date = "2026-03-15",
    OptimizationMode = OptimizationMode.FULL_DAY
});

if (optimization.Status == OptimizationJobStatus.COMPLETED)
{
    var r = optimization.Result!;
    Console.WriteLine($"Plan grade: {r.PlanGrade} ({r.PlanQuality}/100)");
    Console.WriteLine($"Assigned: {r.AssignedJobs}/{r.TotalJobs}");
    Console.WriteLine($"Flow score: {r.FlowScore}/100");
    Console.WriteLine($"Drive times: {r.DriveTimeSource}"); // "API" (real) or "ESTIMATED" (haversine)
}
```

Or manage polling yourself for more control:

```csharp
var job = await klau.Dispatches.StartOptimizationAsync(new OptimizeRequest
{
    Date = "2026-03-15"
});

while (job.Status is OptimizationJobStatus.PENDING or OptimizationJobStatus.RUNNING)
{
    await Task.Delay(2000);
    job = await klau.Dispatches.GetOptimizationStatusAsync(job.JobId);
}
```

### Step 3: Read back assignments

After optimization, read the dispatch board to get driver routes with job sequences. Each job includes drive-time data showing how it will be reached:

```csharp
var board = await klau.Dispatches.GetBoardAsync("2026-03-15");

foreach (var driver in board.Drivers)
{
    Console.WriteLine($"\n{driver.Name} ({driver.Id}): " +
        $"{driver.TotalDriveMinutes} min drive, {driver.TotalServiceMinutes} min service");

    foreach (var job in driver.Jobs.OrderBy(j => j.Sequence))
    {
        Console.WriteLine($"  #{job.Sequence} {job.Type} - {job.CustomerName} " +
            $"({job.ContainerSize}yd) [external: {job.ExternalId}]");

        // Drive-time fields (populated after optimization)
        Console.WriteLine($"    Drive: {job.DriveToMinutes:F1} min / {job.DriveToMiles:F1} mi " +
            $"(source: {job.DriveTimeSource})");
        // DriveTimeSource: "routing_engine" (real truck routing), "cached", "haversine" (estimate), or null
    }
}

// Unassigned jobs that couldn't be fit
foreach (var unassigned in board.UnassignedJobs)
    Console.WriteLine($"Unassigned: {unassigned.ExternalId} - {unassigned.CustomerName}");
```

### Step 4: Publish to drivers

Once you're satisfied with the plan, publish it so drivers see their routes:

```csharp
await klau.Dispatches.PublishAsync("2026-03-15");
```

## Webhooks: Real-Time Event Delivery

Instead of polling for changes, register a webhook to receive events as they happen. Klau delivers events with HMAC-SHA256 signatures for verification.

### Register a webhook

```csharp
var webhook = await klau.Webhooks.CreateAsync(new CreateWebhookRequest
{
    Url = "https://your-app.com/api/klau-webhook",
    Events = ["job.assigned", "job.completed", "dispatch.optimized"],
    Description = "Production sync"
});

// Store this secret securely — it's only returned once
Console.WriteLine($"Webhook secret: {webhook.Secret}");
```

### Receive and verify events

Use `KlauWebhookValidator` in your webhook endpoint to verify signatures and parse events:

```csharp
// In your ASP.NET Core controller or minimal API
app.MapPost("/api/klau-webhook", async (HttpContext ctx) =>
{
    var validator = new KlauWebhookValidator("whsec_your_secret");

    var body = await new StreamReader(ctx.Request.Body).ReadToEndAsync();
    var signature = ctx.Request.Headers["Klau-Signature"].ToString();

    // Throws KlauWebhookException if signature is invalid or timestamp expired
    var evt = validator.ValidateAndParse(signature, body);

    switch (evt.Type)
    {
        case "job.assigned":
            var assigned = evt.Data.Deserialize<JobAssignedEvent>();
            // Sync assignment back to your system
            // assigned.JobId, assigned.DriverId, assigned.AssignmentSource
            break;

        case "job.completed":
            var completed = evt.Data.Deserialize<JobCompletedEvent>();
            // Close work order in your system
            break;

        case "dispatch.optimized":
            var optimized = evt.Data.Deserialize<DispatchOptimizedEvent>();
            // React to optimization: optimized.Metrics.AssignedJobs, etc.
            break;
    }

    return Results.Ok();
});
```

Or parse directly into a typed event if you know the type:

```csharp
var evt = validator.ValidateAndParse<JobAssignedEvent>(signature, body);
Console.WriteLine($"Job {evt.Data.JobId} assigned to driver {evt.Data.DriverId}");
```

### Available events

| Event | Fired when |
|-------|-----------|
| `job.created` | New job entered into Klau |
| `job.assigned` | Job assigned to a driver (manual or optimization) |
| `job.unassigned` | Job removed from a driver's route |
| `job.status_changed` | Job transitions (IN_PROGRESS, COMPLETED, etc.) |
| `job.completed` | Job finished (also fires `status_changed`) |
| `dispatch.optimized` | Optimization run completed with metrics |

Use `"*"` to subscribe to all events.

### Manage webhooks

```csharp
// List existing webhooks
var settings = await klau.Webhooks.GetSettingsAsync();
foreach (var endpoint in settings.WebhookEndpoints)
    Console.WriteLine($"{endpoint.Id}: {endpoint.Url} [{endpoint.Status}]");

// Disable/enable
await klau.Webhooks.SetEnabledAsync("webhook-id", false);

// Test connectivity
var test = await klau.Webhooks.TestAsync("webhook-id");
Console.WriteLine($"Test: {(test.Success ? "OK" : test.Error)} ({test.ResponseTime}ms)");

// Delete
await klau.Webhooks.DeleteAsync("webhook-id");
```

## Enterprise: Multi-Division Operations

Parent company API keys can list all divisions and operate on any child tenant. Integrate once, manage everything through a single API key.

```csharp
using var klau = new KlauClient("kl_live_corporate_api_key");

// List all divisions under your account
var divisions = await klau.Divisions.ListAsync();
foreach (var div in divisions)
    Console.WriteLine($"{div.Name}: {div.DriverCount} drivers, {div.JobCount} jobs");

// Operate on a specific division — thread-safe, no shared state
var region = klau.ForTenant(divisions[0].Id);
var board = await region.Dispatches.GetBoardAsync("2026-03-15");
var jobs = await region.Jobs.ListAsync(date: "2026-03-15");

// Or set/clear tenant context directly
klau.SetTenant(divisions[0].Id);
var customers = await klau.Customers.ListAsync();
klau.ClearTenant(); // revert to parent company context

// Aggregate usage across all divisions
var usage = await klau.Divisions.GetUsageSummaryAsync();
Console.WriteLine($"Total jobs: {usage.TotalJobs} across {usage.Divisions.Count} divisions");
```

## Jobs

```csharp
// List unassigned jobs for a date
var jobs = await klau.Jobs.ListAsync(date: "2026-03-15", status: JobStatus.UNASSIGNED);

foreach (var job in jobs.Items)
    Console.WriteLine($"{job.Type} - {job.CustomerName} - {job.ContainerSize}yd");

// Create a job
var newJob = await klau.Jobs.CreateAsync(new CreateJobRequest
{
    CustomerId = "customer-id",
    SiteId = "site-id",
    Type = JobType.DELIVERY,
    ContainerSize = 20,
    RequestedDate = "2026-03-15",
    TimeWindow = TimeWindow.MORNING
});

// Assign to a driver
await klau.Jobs.AssignAsync(newJob.Id, new AssignJobRequest
{
    DriverId = "driver-id",
    TruckId = "truck-id",
    Sequence = 1,
    ScheduledDate = "2026-03-15"
});

// Lifecycle transitions
await klau.Jobs.StartAsync(newJob.Id);    // ASSIGNED -> IN_PROGRESS
await klau.Jobs.CompleteAsync(newJob.Id);  // IN_PROGRESS -> COMPLETED
```

## Storefront (Online Ordering)

Storefronts are configured through the API and live at `{slug}.rolloff.app`. Public endpoints (catalog, order submission, availability) do not require authentication.

```csharp
// Get storefront catalog (public, no auth required)
var config = await klau.Storefronts.GetConfigAsync("my-company");
foreach (var offering in config.ServiceOfferings)
    Console.WriteLine($"{offering.Name}: ${offering.BasePriceCents / 100.0}");

// Check available delivery dates
var availability = await klau.Storefronts.CheckAvailabilityAsync("my-company",
    new CheckAvailabilityRequest { Zip = "90210" });

// Submit an order
var order = await klau.Storefronts.SubmitOrderAsync("my-company", new SubmitOrderRequest
{
    ServiceOfferingId = config.ServiceOfferings[0].Id,
    Contact = new OrderContact
    {
        Name = "Jane Doe",
        Phone = "5555551234",
        Email = "jane@example.com"
    },
    DeliveryAddress = new DeliveryAddress
    {
        Street = "123 Main St",
        City = "Anytown",
        State = "CA",
        Zip = "90210"
    },
    RequestedDeliveryDate = "2026-03-15",
    TimeWindow = TimeWindow.MORNING
});

// Track the order (public, no auth)
var tracking = await klau.Orders.GetStatusAsync(order.OrderId);
Console.WriteLine($"Order {tracking.OrderId}: {tracking.Status}");
```

## Dump Tickets and Settlement

```csharp
// Record a dump ticket
var ticket = await klau.DumpTickets.CreateAsync(new CreateDumpTicketRequest
{
    JobId = "job-id",
    TicketNumber = "DT-2026-0042",
    GrossWeightLbs = 12000,
    TareWeightLbs = 8000
});

// Verify with corrections
await klau.DumpTickets.VerifyAsync(ticket.Id, new VerifyDumpTicketRequest
{
    GrossWeightLbs = 12500  // corrected weight
});

// Manual settlement (auto-settlement happens via dump ticket capture)
var settlement = await klau.Orders.SettleAsync("order-id");
Console.WriteLine($"Settled: ${settlement.FinalTotalCents / 100.0}");
```

## Customers

```csharp
// Search customers
var customers = await klau.Customers.ListAsync(search: "Acme");

// Create a customer
var customer = await klau.Customers.CreateAsync(new CreateCustomerRequest
{
    Name = "Acme Construction",
    ContactName = "John Doe",
    ContactPhone = "5555551234",
    ContactEmail = "john@example.com"
});

// Get full customer 360 view
var view = await klau.Customers.Get360Async(customer.Id);
Console.WriteLine($"Health: {view.HealthScore} | Orders: {view.TotalOrders}");

// List customer's sites
var sites = await klau.Customers.ListSitesAsync(customer.Id);
```

## Materials

```csharp
// List active materials
var materials = await klau.Materials.ListAsync(activeOnly: true);

// Seed from industry templates
var templates = await klau.Materials.ListTemplatesAsync();
await klau.Materials.SeedFromTemplateAsync(
    templates.Select(t => t.Code).ToList());
```

## Company Settings

Read and update your company's operational configuration. Useful for setting up container sizes, operating hours, and import mappings before running bulk imports.

```csharp
// Read current settings
var company = await klau.Company.GetAsync();
Console.WriteLine($"Container sizes: {string.Join(", ", company.ContainerSizes)}");
Console.WriteLine($"Hours: {company.WorkdayStart}–{company.WorkdayEnd} {company.Timezone}");
Console.WriteLine($"Workdays: {string.Join(", ", company.Workdays)}");

// Add a nonstandard container size (e.g. 35-yard)
var updated = await klau.Company.UpdateAsync(new UpdateCompanyRequest
{
    ContainerSizes = [10, 15, 20, 30, 35, 40]
});

// Configure import mappings (map your ERP service codes to Klau job types)
await klau.Company.UpdateAsync(new UpdateCompanyRequest
{
    ImportServiceCodeMappings =
    [
        new ServiceCodeMapping { ExternalCode = "DEL", KlauJobType = "DELIVERY" },
        new ServiceCodeMapping { ExternalCode = "PU",  KlauJobType = "PICKUP" },
        new ServiceCodeMapping { ExternalCode = "SW",  KlauJobType = "SWAP" },
        new ServiceCodeMapping { ExternalCode = "RLO", KlauJobType = "SKIP" }  // skip relay-only codes
    ]
});

// Adjust dispatch automation
await klau.Company.UpdateAsync(new UpdateCompanyRequest
{
    AutoPublishDispatches = true,
    DispatchApprovalThreshold = 80  // auto-publish plans scoring 80+
});
```

## Error Handling

All API errors throw `KlauApiException` with structured properties for programmatic routing:

```csharp
try
{
    await klau.Jobs.GetAsync("nonexistent-id");
}
catch (KlauApiException ex) when (ex.IsRateLimit)
{
    // Wait and retry — RetryAfter is parsed from the Retry-After header
    await Task.Delay(ex.RetryAfter ?? TimeSpan.FromSeconds(5));
}
catch (KlauApiException ex) when (ex.IsValidation)
{
    // Field-level validation errors are parsed into typed objects
    foreach (var err in ex.ValidationErrors)
        Console.WriteLine($"  {err.Field}: {err.Message}");
}
catch (KlauApiException ex) when (ex.IsNotFound)
{
    Console.WriteLine($"Resource not found: {ex.Message}");
}
catch (KlauApiException ex)
{
    Console.WriteLine($"Error: {ex.ErrorCode} - {ex.Message} (HTTP {ex.StatusCode})");
}
```

Convenience properties: `IsRateLimit`, `IsNotFound`, `IsValidation`, `IsUnauthorized`, `IsInsufficientScope`, `IsConflict`. The `RetryAfter` property (`TimeSpan?`) is available on rate limit errors. `ValidationErrors` (`IReadOnlyList<ValidationDetail>`) provides typed field/message/constraint details.

Common error codes: `VALIDATION_ERROR`, `UNAUTHORIZED`, `NOT_FOUND`, `INSUFFICIENT_SCOPE`, `COMMAND_FAILED`.

The SDK automatically retries transient errors (429, 502, 503, 504) with exponential backoff up to 3 times, and respects `Retry-After` headers from rate limiting.

## Pagination

Use `ListAllAsync` to iterate all results without manual paging. The SDK handles page advancement automatically:

```csharp
// Recommended — zero boilerplate
await foreach (var job in klau.Jobs.ListAllAsync(date: "2026-03-15"))
    Console.WriteLine($"{job.Type} - {job.CustomerName}");

// With LINQ (requires System.Linq.Async NuGet package)
var urgent = await klau.Jobs.ListAllAsync(date: "2026-03-15")
    .Where(j => j.Priority == JobPriority.URGENT)
    .ToListAsync();
```

`ListAllAsync` is available on Jobs, Customers, Drivers, Trucks, Yards, DumpSites, and DumpTickets.

For page-level control, use `ListAsync` which returns `PagedResult<T>`:

```csharp
var page1 = await klau.Jobs.ListAsync(page: 1, pageSize: 50);
Console.WriteLine($"Showing {page1.Items.Count} of {page1.Total}");

if (page1.HasMore)
{
    var page2 = await klau.Jobs.ListAsync(page: 2, pageSize: 50);
}
```

## API Key Scopes

API keys can be scoped to specific permissions using `action:resource` format:

| Scope | Access |
|-------|--------|
| `read:all` | Read access to all resources |
| `write:all` | Write access to all resources |
| `read:jobs` | Read jobs only |
| `write:dispatches` | Write access to dispatches |
| `*` | Full access (read + write, all resources) |

Resources: `jobs`, `drivers`, `trucks`, `dispatches`, `customers`, `sites`, `yards`, `dump-sites`, `materials`, `storefronts`, `orders`, `dump-tickets`, `communications`, `intelligence`, `coaching`, `export`, and more.

If a request exceeds the key's scopes, the API returns `403 INSUFFICIENT_SCOPE`.

## ASP.NET Core Integration

### Dependency injection

```csharp
// Minimal — reads API key from KLAU_API_KEY env var
builder.Services.AddKlauClient();

// From configuration
builder.Services.AddKlauClient(opts =>
    builder.Configuration.GetSection("Klau").Bind(opts));

// Inject via interface
public class DispatchService(IKlauClient klau)
{
    public async Task SyncJobs() =>
        await foreach (var job in klau.Jobs.ListAllAsync(date: "2026-03-15"))
            // ...
}
```

### Health checks

```csharp
builder.Services.AddHealthChecks()
    .AddKlauCheck();  // calls Company.GetAsync to verify connectivity
```

### OpenTelemetry tracing

Every API call emits a span via `System.Diagnostics.ActivitySource`. Tags include `http.request.method`, `url.path`, `http.response.status_code`, `klau.tenant.id`, and `klau.retry.count`.

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(b => b.AddSource("Klau.Sdk"));
```

## Idempotency

Use `KlauRequestOptions` to pass idempotency keys on mutation methods. This prevents duplicate creates when ERP queue messages are delivered more than once:

```csharp
await klau.Jobs.CreateAsync(request, new KlauRequestOptions
{
    IdempotencyKey = $"erp-order-{workOrder.ExternalId}",
    Timeout = TimeSpan.FromSeconds(60),  // per-request timeout override
});
```

## Testing

All domain clients have interfaces (`IJobClient`, `IDispatchClient`, etc.) for business-level mocking:

```csharp
var mockJobs = Substitute.For<IJobClient>();
mockJobs.ListAsync(date: "2026-03-15").Returns(
    KlauModelFactory.PagedResult([
        KlauModelFactory.Job(status: JobStatus.ASSIGNED, driverId: "drv-1"),
        KlauModelFactory.Job(status: JobStatus.UNASSIGNED),
    ]));

var service = new YourService(mockJobs);
```

`KlauModelFactory` provides static factories with sensible defaults for `Job`, `DispatchBoardJob`, `Customer`, `Driver`, `Truck`, `Yard`, `DumpSite`, and `PagedResult<T>`.

For HTTP-level testing, the test helpers include `MockHttpHandler` — see [tests/](tests/Klau.Sdk.Tests/Helpers/).

## Raw API Access

Call endpoints the SDK doesn't cover yet through the full auth/retry infrastructure:

```csharp
var response = await klau.SendRawAsync(
    HttpMethod.Get, "api/v1/analytics/route-efficiency?date=2026-03-16");
var json = await response.Content.ReadAsStringAsync();
```

## Examples

| Example | Description |
|---------|-------------|
| [`CsvJobImport`](examples/CsvJobImport/) | Console app: CSV → batch create → optimize → read assignments |
| [`WebhookIntegration`](examples/WebhookIntegration/) | Kestrel web app: bidirectional sync with webhooks |
| [`EnterpriseSimulator`](examples/EnterpriseSimulator/) | Full enterprise pipeline: CSV export → import → optimize → export dispatch plan |

## License

MIT
