# Klau Enterprise Integration Example

A complete, runnable example of integrating an existing hauler dispatch system with the [Klau platform](https://getklau.com) using the [Klau .NET SDK](https://github.com/EdisonCode/klau-dotnet-sdk).

This project takes a CSV work-order export — the kind most haulers already produce from their current dispatch software — and runs it through the full Klau pipeline: import, optimize, and export an optimized dispatch plan with driver assignments, route sequences, and ETAs.

## What this demonstrates

| Step | What happens | SDK method |
|------|-------------|------------|
| **0. Connect** | Initialize the SDK and scope to your division | `new KlauClient()`, `ForTenant()` |
| **1. Parse** | Read your daily work-order CSV export | _(your code)_ |
| **2. Master data** | Verify yard, dump sites, trucks, and drivers exist | `Yards.ListAsync()`, `Trucks.CreateAsync()`, etc. |
| **3. Container sizes** | Detect non-standard sizes (e.g. 35-yard compactors) and add them | `Company.GetAsync()`, `Company.UpdateAsync()` |
| **4. Import** | Push jobs to Klau with auto-created customers and sites | `Import.ImportAndWaitAsync()` |
| **5. Optimize** | Run the dispatch optimizer with real commercial truck drive times | `Dispatches.OptimizeAndWaitAsync()` |
| **6. Read plan** | Retrieve the optimized dispatch board with sequences and ETAs | `Dispatches.GetBoardAsync()` |
| **7. Export** | Write driver assignments to JSON and CSV for import back into your system | _(your code)_ |

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- A Klau account with an API key ([sign up](https://getklau.com))

## Quick start

```bash
# 1. Clone and navigate to the example
git clone https://github.com/EdisonCode/klau-dotnet-sdk.git
cd klau-dotnet-sdk/examples/EnterpriseSimulator

# 2. Configure your credentials
cp .env.example .env
# Edit .env — add your API key (and division ID if multi-location)

# 3. Place your CSV export in the project directory
cp /path/to/your/daily-export.csv .

# 4. Run
dotnet run -- your-daily-export.csv
```

Output files are written to `output/`:
- `dispatch-plan-YYYY-MM-DD.json` — full dispatch plan with drive-time details
- `dispatch-plan-YYYY-MM-DD.csv` — driver assignments keyed by your work-order numbers

## Configuration

Copy `.env.example` to `.env` and fill in your values:

```env
# Your API key (generate at Settings > Developer in the Klau dashboard)
KLAU_API_KEY=kl_live_YOUR_API_KEY_HERE

# API base URL (production is the default)
KLAU_BASE_URL=https://api.getklau.com

# Division ID — only needed for enterprise (multi-division) accounts
KLAU_DIVISION_ID=
```

### Finding your division ID

Enterprise accounts that manage multiple divisions need to specify which division to operate on. Run this snippet to list your divisions:

```csharp
var klau = new KlauClient("kl_live_YOUR_API_KEY");
var divisions = await klau.Divisions.ListAsync();
foreach (var div in divisions)
    Console.WriteLine($"{div.Name}: {div.Id}");
```

Single-location companies can leave `KLAU_DIVISION_ID` blank.

## Integration lifecycle

This section walks through each step of the integration in detail.

### Step 0: Initialize the SDK

```csharp
using var klau = new KlauClient(apiKey, baseUrl);

// Enterprise: scope all calls to a specific division
var api = klau.ForTenant(divisionId);

// Single-location: use the client directly
var api = klau;
```

The `ForTenant()` scope is thread-safe. You can hold multiple scopes targeting different divisions simultaneously.

### Step 1: Parse your CSV

This example includes a parser for a common hauler CSV format. Adapt the `CsvParser` class and the service-type mapping to match your system's export format.

The key mapping decisions:
- **Service type** — map your codes to Klau job types: `DELIVERY`, `PICKUP`, `DUMP_RETURN`, `SWAP`
- **Container size** — extract the yard size from your container description (e.g. `"30 YARD ROLL OFF"` -> `30`)
- **Destination** — the dump site name, if your system tracks it per-job

### Step 2: Master data

Your operation configuration — yards, dump sites, trucks, drivers — is typically set up once through the Klau dashboard, then kept in sync via the API as your fleet changes.

This example checks for existing data and creates a minimal starter set if nothing exists. In production, you would configure this through the dashboard and use the API to keep it in sync when trucks or drivers change in your system.

### Step 3: Container sizes

Klau ships with standard roll-off sizes (10, 15, 20, 30, 40 yard). If your fleet uses non-standard sizes — 35-yard compactors are common — this step detects them in your CSV and adds them to your company configuration automatically.

```csharp
var company = await api.Company.GetAsync();
var configuredSizes = company.ContainerSizes.ToHashSet();

// Detect sizes in the CSV that aren't configured yet
var missing = csvSizes.Except(configuredSizes).ToList();
if (missing.Count > 0)
{
    var updated = configuredSizes.Union(missing).OrderBy(s => s).ToList();
    await api.Company.UpdateAsync(
        new UpdateCompanyRequest { ContainerSizes = updated });
}
```

Without this step, jobs with non-standard sizes are skipped during import.

### Step 4: Import jobs

`ImportAndWaitAsync` is the recommended way to push jobs from your system into Klau. It handles three things in one call:

1. **Creates jobs** — with `createMissing: true`, it auto-creates customer and site records from the names/addresses in your CSV. No need to pre-create IDs.
2. **Warms the drive-time cache** — Klau uses HERE Maps commercial truck routing. New sites need their drive-time matrix cached before optimization can use real routes.
3. **Returns when ready** — the method polls until geocoding and caching are complete, so the optimizer has real drive times for all sites.

```csharp
var result = await api.Import.ImportAndWaitAsync(
    new ImportJobsRequest
    {
        Jobs = importRecords,
        CreateMissing = true,      // auto-create customers + sites
    },
    timeout: TimeSpan.FromSeconds(90));
```

The `ExternalId` field is your work-order number. It survives the full round trip (import -> optimize -> read back) so you can map assignments back to your system.

### Step 5: Optimize

The optimizer assigns jobs to drivers and sequences each route to minimize total drive time while respecting constraints (truck compatibility, time windows, dump site hours).

```csharp
var optimization = await api.Dispatches.OptimizeAndWaitAsync(
    new OptimizeRequest
    {
        Date = "2026-03-18",
        OptimizationMode = OptimizationMode.FULL_DAY,
    });
```

The `DriveTimeSource` on the result tells you whether the optimizer used:
- `routing_engine` — real commercial truck routing via HERE Maps
- `cached` — from the pre-warmed cache
- `haversine` — straight-line estimate (new sites not yet cached)

### Step 6: Read the dispatch plan

The dispatch board contains each driver's optimized route:

```csharp
var board = await api.Dispatches.GetBoardAsync("2026-03-18");

foreach (var driver in board.Drivers)
{
    Console.WriteLine($"Driver: {driver.Name} (Truck {driver.TruckNumber})");

    foreach (var job in driver.Jobs.OrderBy(j => j.Sequence))
    {
        // Map back to your system using ExternalId
        Console.WriteLine($"  #{job.Sequence} {job.Type} {job.CustomerName}");
        Console.WriteLine($"    Your order: {job.ExternalId}");
        Console.WriteLine($"    ETA: {job.EstimatedStartTime}");
        Console.WriteLine($"    Drive: {job.DriveToMinutes} min, {job.DriveToMiles} mi ({job.DriveTimeSource})");
    }
}
```

Each job on the board includes:

| Field | Description |
|-------|-------------|
| `Sequence` | Position in the driver's route (1-based) |
| `ExternalId` | Your work-order number — the sync key back to your system |
| `EstimatedStartTime` | Projected arrival time (ISO 8601) |
| `DriveToMinutes` | Drive time from the previous stop |
| `DriveToMiles` | Distance from the previous stop |
| `DriveTimeSource` | How the drive time was calculated |

### Step 7: Export

The example writes two output files:

- **JSON** — full dispatch plan with all fields, for archival or debugging
- **CSV** — flat file mapping your order numbers to driver assignments, suitable for import back into your dispatch system

## CSV format

The included CSV parser expects this column layout:

```
Order #, Acct #, Customer Name, Street, Street Number, City, State, Zip,
Container, Service, Destination, Longitude, Latitude, FullAddress
```

Adapt the `CsvParser` class and the `CsvWorkOrder` record to match your system's export format. The key fields the integration needs are:

| Your field | Maps to | Required |
|-----------|---------|----------|
| Customer name | `ImportJobRecord.CustomerName` | Yes |
| Site address | `ImportJobRecord.SiteAddress` | Yes |
| Service type | `ImportJobRecord.JobType` | Yes |
| Container size | `ImportJobRecord.ContainerSize` | Yes |
| Work order number | `ImportJobRecord.ExternalId` | Recommended |
| Destination / dump site | `ImportJobRecord.Notes` | Optional |

## Extending this example

### Webhooks for real-time sync

Instead of polling, register webhooks to react to changes in real time:

```csharp
await klau.Webhooks.CreateAsync(new CreateWebhookRequest
{
    Url = "https://your-system.com/klau/webhook",
    Events = ["job.completed", "job.assigned", "dispatch.optimized"],
});
```

Events fire when jobs are completed in the field, assignments change, or a new optimization runs.

### Publishing dispatches to drivers

After reviewing the optimized plan, publish it so drivers see their routes in the Klau mobile app:

```csharp
await api.Dispatches.PublishAsync("2026-03-18");
```

### Mid-day re-optimization

When rush jobs come in during the day, import the new job and re-optimize. The optimizer incorporates in-progress work and fits the new job into the best available route.

## Project structure

```
EnterpriseSimulator/
  .env.example        # Configuration template (copy to .env)
  Program.cs           # The full integration — one file, heavily commented
  output/              # Generated dispatch plans (git-ignored)
```

## SDK reference

- **NuGet**: `dotnet add package Klau.Sdk`
- **Source**: [github.com/EdisonCode/klau-dotnet-sdk](https://github.com/EdisonCode/klau-dotnet-sdk)
- **API docs**: [api.getklau.com/docs](https://api.getklau.com/docs)
