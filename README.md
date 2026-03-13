# Klau .NET SDK

Official .NET SDK for the [Klau](https://getklau.com) API. Built for dev teams integrating roll-off waste hauling operations into .NET applications.

## Installation

```bash
dotnet add package Klau.Sdk
```

## Quick Start

Generate an API key in **Settings > Developer** in your Klau dashboard. API keys start with `kl_live_`, are scoped to specific permissions, and can be revoked without affecting user credentials.

```csharp
using Klau.Sdk;

using var klau = new KlauClient("kl_live_your_api_key_here");

// Get today's dispatch board
var board = await klau.Dispatches.GetBoardAsync("2026-03-13");

foreach (var dispatch in board.Dispatches)
{
    Console.WriteLine($"{dispatch.DriverName}: {dispatch.Jobs.Count} jobs");
}
```

The API key is the only credential you need. It authenticates as your company with the scopes you configured at creation time.

## Enterprise: Multi-Division Operations

Parent company API keys can list all divisions and operate on any child tenant. Integrate once, manage everything through a single API key.

```csharp
using var klau = new KlauClient("kl_live_corporate_api_key");

// List all divisions under your account
var divisions = await klau.Divisions.ListAsync();
foreach (var div in divisions)
{
    Console.WriteLine($"{div.Name}: {div.DriverCount} drivers, {div.JobCount} jobs");
}

// Get aggregate usage across all divisions
var usage = await klau.Divisions.GetUsageSummaryAsync();
Console.WriteLine($"Total jobs: {usage.TotalJobs} across {usage.Divisions.Count} divisions");

// Operate on a specific division
var region = klau.ForTenant(divisions[0].Id);
var board = await region.Dispatches.GetBoardAsync("2026-03-13");
var jobs = await region.Jobs.ListAsync(date: "2026-03-13");

// Or set/clear tenant context directly
klau.SetTenant(divisions[0].Id);
var customers = await klau.Customers.ListAsync();
klau.ClearTenant(); // revert to parent company context

// Get detailed usage for a single division
var divUsage = await klau.Divisions.GetUsageAsync(divisions[0].Id);
Console.WriteLine($"Recent jobs: {divUsage.RecentJobs}, Billed: {divUsage.BilledJobs}");
```

## Jobs

```csharp
// List unassigned jobs for a date
var jobs = await klau.Jobs.ListAsync(date: "2026-03-13", status: JobStatus.UNASSIGNED);

foreach (var job in jobs.Items)
{
    Console.WriteLine($"{job.Type} - {job.CustomerName} - {job.ContainerSize}yd");
}

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

## Dispatch Optimization

```csharp
// Start optimization and wait for results (polls automatically)
var result = await klau.Dispatches.OptimizeAndWaitAsync(new OptimizeRequest
{
    Date = "2026-03-13",
    OptimizationMode = OptimizationMode.FULL_DAY
});

Console.WriteLine($"Chain Score: {result.Result?.ChainScore}/100");
Console.WriteLine($"Assigned: {result.Result?.AssignedJobs}/{result.Result?.TotalJobs}");

// Or manage polling yourself
var job = await klau.Dispatches.StartOptimizationAsync(new OptimizeRequest
{
    Date = "2026-03-13"
});

while (job.Status is OptimizationJobStatus.PENDING or OptimizationJobStatus.RUNNING)
{
    await Task.Delay(2000);
    job = await klau.Dispatches.GetOptimizationStatusAsync(job.JobId);
}

// Publish optimized routes to drivers
await klau.Dispatches.PublishAsync("2026-03-13");
```

## Storefront (Online Ordering)

Storefronts are configured through the API and live at `{slug}.rolloff.app`. Public endpoints (catalog, order submission, availability) do not require authentication.

```csharp
// Get storefront catalog (public, no auth required)
var config = await klau.Storefronts.GetConfigAsync("my-company");
foreach (var offering in config.ServiceOfferings)
{
    Console.WriteLine($"{offering.Name}: ${offering.BasePriceCents / 100.0}");
}

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

## Requirements

- .NET 9.0+
- No external dependencies

## License

MIT
