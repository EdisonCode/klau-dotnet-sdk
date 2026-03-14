# Klau .NET SDK

Official .NET SDK for the [Klau](https://getklau.com) API. Built for dev teams integrating roll-off waste hauling operations into .NET applications.

```bash
dotnet add package Klau.Sdk
```

Requires .NET 9.0+. No external dependencies.

## Quick Start

Generate an API key in **Settings > Developer** in your Klau dashboard. API keys start with `kl_live_`, are scoped to specific permissions, and can be revoked without affecting user credentials.

```csharp
using Klau.Sdk;

using var klau = new KlauClient("kl_live_your_api_key_here");

// Get today's dispatch board
var board = await klau.Dispatches.GetBoardAsync("2026-03-15");

foreach (var dispatch in board.Dispatches)
    Console.WriteLine($"{dispatch.DriverName}: {dispatch.Jobs.Count} jobs");
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

After optimization, read the dispatch board to get driver routes with job sequences:

```csharp
var board = await klau.Dispatches.GetBoardAsync("2026-03-15");

foreach (var dispatch in board.Dispatches)
{
    Console.WriteLine($"\n{dispatch.DriverName} ({dispatch.DriverId}):");

    foreach (var job in dispatch.Jobs.OrderBy(j => j.Sequence))
    {
        Console.WriteLine($"  #{job.Sequence} {job.Type} - {job.CustomerName} " +
            $"({job.ContainerSize}yd) [external: {job.ExternalId}]");
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

## Error Handling

All API errors throw `KlauApiException` with structured error information:

```csharp
try
{
    await klau.Jobs.GetAsync("nonexistent-id");
}
catch (KlauApiException ex)
{
    Console.WriteLine($"Error: {ex.ErrorCode} - {ex.Message}");
    Console.WriteLine($"HTTP Status: {ex.StatusCode}");
    // ex.Details contains field-level validation errors when applicable
}
```

Common error codes: `VALIDATION_ERROR`, `UNAUTHORIZED`, `NOT_FOUND`, `INSUFFICIENT_SCOPE`, `COMMAND_FAILED`.

The SDK automatically retries transient errors (429, 502, 503, 504) with exponential backoff up to 3 times, and respects `Retry-After` headers from rate limiting.

## Pagination

List endpoints return `PagedResult<T>` with pagination metadata:

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

## License

MIT
