# Klau .NET SDK

Official .NET SDK for the [Klau](https://klau.app) API. Built for dev teams integrating roll-off waste hauling operations into .NET applications.

## Installation

```bash
dotnet add package Klau.Sdk
```

## Quick Start

```csharp
using Klau.Sdk;

// Create client and authenticate
using var klau = new KlauClient("https://your-instance.klau.app");
await klau.Auth.LoginAsync("user@example.com", "password");

// Get today's dispatch board
var board = await klau.Dispatches.GetBoardAsync("2026-03-13");

foreach (var dispatch in board.Dispatches)
{
    Console.WriteLine($"{dispatch.DriverName}: {dispatch.Jobs.Count} jobs");
}
```

## Authentication

```csharp
// Login with credentials (stores token automatically)
var result = await klau.Auth.LoginAsync("user@example.com", "password");
Console.WriteLine($"Logged in as {result.User.Name} at {result.Company.Name}");

// Or use a pre-existing token
using var klau = new KlauClient("https://your-instance.klau.app", "your-jwt-token");
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

```csharp
// Public endpoints (no auth required)

// Get storefront catalog
var config = await klau.Storefronts.GetConfigAsync("dcwaste");
foreach (var offering in config.ServiceOfferings)
{
    Console.WriteLine($"{offering.Name}: ${offering.BasePriceCents / 100.0}");
}

// Check available delivery dates
var availability = await klau.Storefronts.CheckAvailabilityAsync("dcwaste",
    new CheckAvailabilityRequest { Zip = "62049" });

// Submit an order
var order = await klau.Storefronts.SubmitOrderAsync("dcwaste", new SubmitOrderRequest
{
    ServiceOfferingId = config.ServiceOfferings[0].Id,
    Contact = new OrderContact
    {
        Name = "Jane Smith",
        Phone = "2175551234",
        Email = "jane@example.com"
    },
    DeliveryAddress = new DeliveryAddress
    {
        Street = "123 Main St",
        City = "Hillsboro",
        State = "IL",
        Zip = "62049"
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
var customers = await klau.Customers.ListAsync(search: "Smith");

// Create a customer
var customer = await klau.Customers.CreateAsync(new CreateCustomerRequest
{
    Name = "Smith Construction",
    ContactName = "John Smith",
    ContactPhone = "2175551234",
    ContactEmail = "john@smithconstruction.com"
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

## Requirements

- .NET 9.0+
- No external dependencies

## License

MIT
