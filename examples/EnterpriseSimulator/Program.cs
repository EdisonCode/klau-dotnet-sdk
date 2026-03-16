// ──────────────────────────────────────────────────────────────────────────────
// Klau SDK — Enterprise Integration Example
//
// Demonstrates the recommended integration pattern for haulers migrating
// daily dispatch from an existing system (CSV export) to Klau:
//
//   Step 0  Initialize the SDK and scope to your division
//   Step 1  Parse your daily work-order export
//   Step 2  Ensure master data exists (yard, dump sites, trucks, drivers)
//   Step 3  Ensure container sizes cover your fleet (add non-standard sizes)
//   Step 4  Import jobs with createMissing (auto-creates customers + sites)
//   Step 5  Optimize dispatch
//   Step 6  Read back the optimized plan
//   Step 7  Export assignments for import back into your system
//
// Getting started:
//   1. Copy .env.example → .env and fill in your API key + division ID
//   2. Place your CSV export in the project root (or pass the path as arg[0])
//   3. dotnet run -- your-export.csv
//
// SDK docs:  https://github.com/EdisonCode/klau-dotnet-sdk
// NuGet:     dotnet add package Klau.Sdk
// ──────────────────────────────────────────────────────────────────────────────

using System.Text;
using System.Text.Json;
using Klau.Sdk;
using Klau.Sdk.Common;
using Klau.Sdk.Customers;
using Klau.Sdk.Dispatches;
using Klau.Sdk.Drivers;
using Klau.Sdk.DumpSites;
using Klau.Sdk.Import;
using Klau.Sdk.Jobs;
using Klau.Sdk.Trucks;
using Klau.Sdk.Yards;
using Microsoft.Extensions.Logging;

// ═══════════════════════════════════════════════════════════════════════════════
// Configuration — loaded from .env file, environment variables, or defaults
// ═══════════════════════════════════════════════════════════════════════════════

LoadDotEnv(); // reads .env into Environment if the file exists

var apiKey   = RequireEnv("KLAU_API_KEY");
var baseUrl  = Env("KLAU_BASE_URL", "https://api.getklau.com");
var divId    = Env("KLAU_DIVISION_ID");
var csvPath  = args.Length > 0 ? args[0] : "work-orders.csv";
var today    = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"); // schedule for tomorrow
var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "output");
Directory.CreateDirectory(outputDir);

using var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
var log = loggerFactory.CreateLogger("KlauIntegration");

Header($"Klau SDK — Enterprise Integration", new Dictionary<string, string>
{
    ["API"]      = baseUrl,
    ["Division"] = string.IsNullOrEmpty(divId) ? "(single-location)" : divId,
    ["CSV"]      = Path.GetFileName(csvPath),
    ["Date"]     = today,
});

// ═══════════════════════════════════════════════════════════════════════════════
// Step 0: Initialize SDK
// ═══════════════════════════════════════════════════════════════════════════════
//
// For enterprise (multi-division) accounts, use ForTenant() to scope all
// subsequent operations to a specific division. Single-location accounts
// skip this — the API key already targets the right company.

Step("Step 0: Initialize SDK");

using var klau = new KlauClient(apiKey, baseUrl, logger: log);

// If a division ID is configured, scope all calls to that division.
// Otherwise, use the top-level client (single-location companies).
dynamic api = !string.IsNullOrEmpty(divId) ? klau.ForTenant(divId) : klau;
Ok("Client ready" + (string.IsNullOrEmpty(divId) ? "" : $" (division: {divId})"));

// ═══════════════════════════════════════════════════════════════════════════════
// Step 1: Parse CSV
// ═══════════════════════════════════════════════════════════════════════════════
//
// Your system exports work orders in its own format. This step maps them
// into a normalized shape. Adapt the CSV parser to match your export format.

Step("Step 1: Parse CSV export");

if (!File.Exists(csvPath))
{
    Fail($"CSV not found: {csvPath}");
    return 1;
}

var allOrders = CsvParser.Parse(csvPath);
Info($"Parsed {allOrders.Count} total work orders");

// Klau handles roll-off dispatch. Filter to RO- service types.
var orders = allOrders
    .Where(o => o.ServiceType.StartsWith("RO-", StringComparison.OrdinalIgnoreCase))
    .ToList();

var skipped = allOrders.Except(orders).ToList();
Info($"Roll-off jobs: {orders.Count}  |  Skipped: {skipped.Count} (non roll-off)");

if (skipped.Count > 0)
    foreach (var s in skipped)
        Info($"  skip #{s.OrderNumber} {s.ServiceType,-20} {s.CustomerName}");

Console.WriteLine();
Table(["Order", "Service", "Customer", "Container", "Destination"],
      [8, 22, 35, 18, 0],
      orders.Select(o => new[] { o.OrderNumber, o.ServiceType, o.CustomerName, o.Container, o.Destination }));

// ═══════════════════════════════════════════════════════════════════════════════
// Step 2: Ensure master data (yard, dump sites, trucks, drivers)
// ═══════════════════════════════════════════════════════════════════════════════
//
// Your operation configuration (yards, dump sites, trucks, drivers) is
// typically set up once through the Klau dashboard, then kept in sync via
// the API as your fleet changes. This step ensures the minimum config exists.

Step("Step 2: Verify master data");

// ── Yard (required — the home base for route optimization) ──────────────────
string yardId;
var existingYards = await ((TenantScope)api).Yards.ListAsync();
if (existingYards.Items.Count > 0)
{
    yardId = existingYards.Items[0].Id;
    Ok($"Yard: {existingYards.Items[0].Name}");
}
else
{
    // Create a yard at your main location. Replace with your actual address and coordinates.
    yardId = await ((TenantScope)api).Yards.CreateAsync(new CreateYardRequest
    {
        Name = "Main Yard",
        Address = "123 Industrial Blvd",
        City = "Anytown",
        State = "PA",
        Zip = "10001",
        Latitude = 40.0,
        Longitude = -77.0,
        IsDefault = true,
        ServiceRadiusMiles = 60,
    });
    Ok($"Created yard: {yardId}");
}

// ── Dump Sites (landfills / transfer stations your drivers haul to) ─────────
var dumpSiteMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
var existingSites = await ((TenantScope)api).DumpSites.ListAsync();
foreach (var s in existingSites.Items) dumpSiteMap[s.Name] = s.Id;
Ok($"Dump sites: {existingSites.Items.Count}");

// Extract dump site names from the CSV Destination column. Create any missing.
var csvDumpSites = orders
    .Where(o => !string.IsNullOrWhiteSpace(o.Destination))
    .Select(o => o.Destination.Trim())
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .Where(name => !dumpSiteMap.ContainsKey(name))
    .ToList();

foreach (var name in csvDumpSites)
{
    try
    {
        var id = await ((TenantScope)api).DumpSites.CreateAsync(new CreateDumpSiteRequest
        {
            Name = name,
            Address = name, // placeholder — update with real address in dashboard
            City = "Anytown", State = "PA", Zip = "10001",
            Latitude = 40.0, Longitude = -77.0,
            OpenTime = "06:00", CloseTime = "17:00",
            AcceptedSizes = [10, 15, 20, 30, 40],
            SiteType = "LANDFILL",
        });
        dumpSiteMap[name] = id;
        Ok($"Created dump site: {name}");
    }
    catch (Exception ex) { Warn($"Dump site '{name}': {ex.Message}"); }
}

// ── Trucks ──────────────────────────────────────────────────────────────────
var existingTrucks = await ((TenantScope)api).Trucks.ListAsync();
var truckIds = existingTrucks.Items.Select(t => t.Id).ToList();

if (truckIds.Count == 0)
{
    // Create starter fleet — adjust to match your actual trucks
    for (int i = 1; i <= 4; i++)
    {
        var id = await ((TenantScope)api).Trucks.CreateAsync(new CreateTruckRequest
        {
            Number = $"T-{600 + i}",
            Type = "ROLL_OFF",
            CompatibleSizes = [10, 15, 20, 30, 40],
            HomeYardId = yardId,
            MaxContainers = 1,
        });
        truckIds.Add(id);
    }
    Ok($"Created {truckIds.Count} trucks");
}
else
{
    Ok($"Trucks: {truckIds.Count}");
}

// ── Drivers ─────────────────────────────────────────────────────────────────
var existingDrivers = await ((TenantScope)api).Drivers.ListAsync();
var driverIds = existingDrivers.Items.Where(d => d.IsActive).Select(d => d.Id).ToList();

if (driverIds.Count == 0)
{
    var names = new[] { "Driver 1", "Driver 2", "Driver 3", "Driver 4" };
    for (int i = 0; i < names.Length && i < truckIds.Count; i++)
    {
        var id = await ((TenantScope)api).Drivers.CreateAsync(new CreateDriverRequest
        {
            Name = names[i],
            DriverType = "FULL_TIME",
            DefaultTruckId = truckIds[i],
            HomeYardId = yardId,
        });
        driverIds.Add(id);
    }
    Ok($"Created {driverIds.Count} drivers");
}
else
{
    Ok($"Drivers: {driverIds.Count}");
}

// ═══════════════════════════════════════════════════════════════════════════════
// Step 3: Ensure container sizes (add non-standard sizes from CSV)
// ═══════════════════════════════════════════════════════════════════════════════
//
// Klau ships with standard roll-off sizes (10, 15, 20, 30, 40 yard).
// If your fleet uses non-standard sizes (e.g., 35-yard compactors), add
// them here so the import doesn't reject those jobs.
//
Step("Step 3: Check container sizes");

// Get the company's currently configured sizes
var company = await ((TenantScope)api).Company.GetAsync();
var configuredSizes = company.ContainerSizes?.ToHashSet() ?? new HashSet<int> { 10, 15, 20, 30, 40 };
Ok($"Configured sizes: {string.Join(", ", configuredSizes.OrderBy(s => s))} yard");

// Detect non-standard sizes in the CSV that aren't configured yet
var csvSizes = orders
    .Select(o => ParseContainerSize(o.Container))
    .Where(s => s > 0)
    .Distinct()
    .ToHashSet();

var missing = csvSizes.Except(configuredSizes).OrderBy(s => s).ToList();

if (missing.Count > 0)
{
    Info($"CSV contains sizes not yet configured: {string.Join(", ", missing)} yard");
    Info("Adding to company container sizes...");

    var updatedSizes = configuredSizes.Union(missing).OrderBy(s => s).ToList();
    await ((TenantScope)api).Company.UpdateAsync(
        new Klau.Sdk.Companies.UpdateCompanyRequest { ContainerSizes = updatedSizes });

    Ok($"Updated sizes: {string.Join(", ", updatedSizes)} yard");
}
else
{
    Ok("All CSV sizes are already configured");
}

// ═══════════════════════════════════════════════════════════════════════════════
// Step 4: Import jobs — the golden path
// ═══════════════════════════════════════════════════════════════════════════════
//
// ImportAndWaitAsync is the recommended way to push jobs from your system
// into Klau. It does three things:
//   1. Creates jobs (auto-creating customers + sites with createMissing)
//   2. Waits for drive-time cache (HERE Maps commercial truck routing)
//   3. Returns when the optimizer has real drive times for all new sites
//
// Use ExternalId to carry your system's work-order number through the
// round trip. This is the key for syncing assignments back to your system.

Step("Step 4: Import jobs");

var importRecords = orders.Select(o =>
{
    var jobType = o.ServiceType.ToUpperInvariant() switch
    {
        var s when s.Contains("DUMP & RETURN") => "DUMP_RETURN",
        var s when s.Contains("DELIVERY") => "DELIVERY",
        var s when s.Contains("DISCONTINUE") => "PICKUP",
        var s when s.Contains("SWAP") => "SWAP",
        _ => "DELIVERY",
    };

    var containerSize = ParseContainerSize(o.Container);
    var siteName = !string.IsNullOrEmpty(o.Street)
        ? $"{o.StreetNumber} {o.Street}"
        : o.CustomerName;

    return new ImportJobRecord
    {
        CustomerName = o.CustomerName,
        SiteName     = siteName,
        SiteAddress  = $"{o.StreetNumber} {o.Street}",
        SiteCity     = o.City,
        SiteState    = o.State,
        SiteZip      = o.Zip,
        JobType      = jobType,
        ContainerSize = containerSize > 0 ? containerSize.ToString() : "30",
        TimeWindow   = "ANYTIME",
        Priority     = "NORMAL",
        RequestedDate = today,
        ExternalId   = o.OrderNumber, // ← your system's ID, key for two-way sync
        Notes        = !string.IsNullOrEmpty(o.Destination) ? $"Dest: {o.Destination}" : null,
    };
}).ToList();

Info($"Importing {importRecords.Count} jobs (createMissing=true)...");

ImportJobsResult importResult;
try
{
    importResult = await ((TenantScope)api).Import.ImportAndWaitAsync(
        new ImportJobsRequest { Jobs = importRecords, CreateMissing = true },
        timeout: TimeSpan.FromSeconds(90),
        pollInterval: TimeSpan.FromSeconds(2));
}
catch (TimeoutException)
{
    // Geocode cache didn't finish in time — optimization will use haversine
    // estimates. This is non-fatal; results are still good, just less precise.
    Warn("Drive-time cache warm-up timed out. Optimization will use straight-line estimates.");
    importResult = await ((TenantScope)api).Import.JobsAsync(
        new ImportJobsRequest { Jobs = importRecords, CreateMissing = true });
}

Ok($"Imported: {importResult.Imported}  |  Skipped: {importResult.Skipped}");
if (importResult.CustomersCreated > 0 || importResult.SitesCreated > 0)
    Ok($"Auto-created: {importResult.CustomersCreated} customers, {importResult.SitesCreated} sites");

foreach (var err in importResult.Errors)
    Warn($"Row {err}: skipped");

if (importResult.Imported == 0)
{
    Fail("No jobs imported. Check errors above.");
    return 1;
}

// ═══════════════════════════════════════════════════════════════════════════════
// Step 5: Optimize dispatch
// ═══════════════════════════════════════════════════════════════════════════════
//
// Runs the MDP-based dispatch optimizer. Because drive times were pre-warmed
// in Step 4, the optimizer uses real commercial truck routing (HERE Maps)
// instead of straight-line estimates.

Step("Step 5: Optimize");

Console.Write("  Running optimizer");
var optimization = await ((TenantScope)api).Dispatches.OptimizeAndWaitAsync(
    new OptimizeRequest { Date = today, OptimizationMode = OptimizationMode.FULL_DAY },
    pollInterval: TimeSpan.FromSeconds(3));
Console.WriteLine(" done!");

if (optimization.Status == OptimizationJobStatus.COMPLETED && optimization.Result is { } r)
{
    Ok($"Grade: {r.PlanGrade ?? "N/A"} ({r.PlanQuality ?? 0}/100)  |  Flow: {r.FlowScore ?? 0}/100");
    Ok($"Assigned: {r.AssignedJobs ?? 0}/{r.TotalJobs ?? 0}  |  " +
       $"Drive-time source: {r.DriveTimeSource ?? "N/A"}");
}
else
{
    Warn($"Optimization {optimization.Status}: {optimization.Reason}");
}

// ═══════════════════════════════════════════════════════════════════════════════
// Step 6: Read back the dispatch plan
// ═══════════════════════════════════════════════════════════════════════════════
//
// The board contains each driver's optimized route with sequenced jobs,
// ETAs, and drive-time details. Use ExternalId to map assignments back
// to your system's work orders.

Step("Step 6: Dispatch plan");

var board = await ((TenantScope)api).Dispatches.GetBoardAsync(today);
Info($"Drivers with jobs: {board.Drivers.Count(d => d.Jobs.Count > 0)}  |  " +
     $"Unassigned: {board.UnassignedJobs.Count}");

Console.WriteLine();
foreach (var driver in board.Drivers.Where(d => d.Jobs.Count > 0))
{
    Console.WriteLine($"  {driver.Name}  ({driver.Jobs.Count} jobs, truck {driver.TruckNumber ?? "—"})");
    foreach (var job in driver.Jobs.OrderBy(j => j.Sequence))
    {
        Console.WriteLine($"    #{job.Sequence,-3} {job.Type,-14} {Trunc(job.CustomerName, 25),-25} " +
            $"{job.ExternalId ?? "—",-10} {(job.ContainerSize.HasValue ? $"{job.ContainerSize}yd" : "—"),4}  " +
            $"{job.DriveTimeSource ?? ""}");
    }
    Console.WriteLine();
}

if (board.UnassignedJobs.Count > 0)
{
    Console.WriteLine("  Unassigned:");
    foreach (var j in board.UnassignedJobs)
        Console.WriteLine($"    {j.ExternalId ?? j.Id,-12} {j.Type,-14} {j.CustomerName}");
    Console.WriteLine();
}

// ═══════════════════════════════════════════════════════════════════════════════
// Step 7: Export for ERP sync
// ═══════════════════════════════════════════════════════════════════════════════
//
// Write the dispatch plan to files your system can ingest. The CSV maps
// each original work-order number to its assigned driver and sequence.

Step("Step 7: Export");

var jsonPath   = Path.Combine(outputDir, $"dispatch-plan-{today}.json");
var csvOutPath = Path.Combine(outputDir, $"dispatch-plan-{today}.csv");

// JSON — full detail for archival / debugging
var export = new
{
    generated = DateTime.UtcNow.ToString("O"),
    date = today,
    source = Path.GetFileName(csvPath),
    optimization = optimization.Result is { } res ? new
    {
        grade = res.PlanGrade, quality = res.PlanQuality,
        flow = res.FlowScore, driveTimeSource = res.DriveTimeSource,
        assigned = res.AssignedJobs, unassigned = res.UnassignedJobs,
    } : null,
    drivers = board.Drivers.Where(d => d.Jobs.Count > 0).Select(d => new
    {
        name = d.Name, truck = d.TruckNumber,
        jobs = d.Jobs.OrderBy(j => j.Sequence).Select(j => new
        {
            seq = j.Sequence, externalId = j.ExternalId,
            type = j.Type.ToString(), customer = j.CustomerName,
            size = j.ContainerSize, address = j.SiteAddress,
            eta = j.EstimatedStartTime, driveMinutes = j.DriveToMinutes,
            driveMiles = j.DriveToMiles, driveTimeSource = j.DriveTimeSource,
        }),
    }),
    unassigned = board.UnassignedJobs.Select(j => new
    {
        externalId = j.ExternalId, type = j.Type.ToString(), customer = j.CustomerName,
    }),
};

await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(export, new JsonSerializerOptions
{
    WriteIndented = true,
    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
}));
Ok($"JSON → {jsonPath}");

// CSV — for import back into your dispatch system
var csv = new StringBuilder();
csv.AppendLine("OrderNumber,Driver,Truck,Seq,Type,Customer,Size,ETA,DriveMinutes,DriveMiles,DriveTimeSource");
foreach (var d in board.Drivers)
    foreach (var j in d.Jobs.OrderBy(j2 => j2.Sequence))
        csv.AppendLine(string.Join(",",
            Esc(j.ExternalId ?? ""), Esc(d.Name), d.TruckNumber ?? "",
            j.Sequence, j.Type, Esc(j.CustomerName),
            j.ContainerSize, j.EstimatedStartTime ?? "",
            j.DriveToMinutes, j.DriveToMiles,
            j.DriveTimeSource ?? ""));
foreach (var j in board.UnassignedJobs)
    csv.AppendLine(string.Join(",",
        Esc(j.ExternalId ?? ""), "UNASSIGNED", "", "", j.Type,
        Esc(j.CustomerName), j.ContainerSize, "", "", "", ""));

await File.WriteAllTextAsync(csvOutPath, csv.ToString());
Ok($"CSV  → {csvOutPath}");

// ═══════════════════════════════════════════════════════════════════════════════
// Done
// ═══════════════════════════════════════════════════════════════════════════════

int assigned = board.Drivers.Sum(d => d.Jobs.Count);
int unassigned = board.UnassignedJobs.Count;
Console.WriteLine();
Console.WriteLine("══════════════════════════════════════════════════════════════");
Console.WriteLine($"  Imported: {importResult.Imported}  |  Assigned: {assigned}  |  Unassigned: {unassigned}");
Console.WriteLine($"  Output:   {outputDir}/");
Console.WriteLine("══════════════════════════════════════════════════════════════");

return 0;

// ═══════════════════════════════════════════════════════════════════════════════
// Helpers
// ═══════════════════════════════════════════════════════════════════════════════

static int ParseContainerSize(string container)
{
    var parts = container.Split(' ');
    return parts.Length >= 2 && int.TryParse(parts[0], out var size) && size >= 10 ? size : 0;
}

static string Esc(string v) =>
    v.Contains(',') || v.Contains('"') ? $"\"{v.Replace("\"", "\"\"")}\"" : v;

static string Trunc(string v, int max) =>
    v.Length <= max ? v : v[..(max - 1)] + "…";

static void Header(string title, Dictionary<string, string> details)
{
    var bar = new string('═', 62);
    Console.WriteLine($"╔{bar}╗");
    Console.WriteLine($"║  {title,-60}║");
    Console.WriteLine($"╚{bar}╝");
    foreach (var (k, v) in details) Console.WriteLine($"  {k + ":",-12}{v}");
    Console.WriteLine();
}

static void Step(string name) => Console.WriteLine($"\n── {name} {"─",60}");
static void Ok(string msg)   => Console.WriteLine($"  + {msg}");
static void Warn(string msg) => Console.WriteLine($"  ! {msg}");
static void Fail(string msg) => Console.Error.WriteLine($"  x {msg}");
static void Info(string msg) => Console.WriteLine($"  {msg}");

static void Table(string[] headers, int[] widths, IEnumerable<string[]> rows)
{
    var fmt = string.Join(" ", headers.Select((h, i) =>
        i < widths.Length && widths[i] > 0 ? $"{{{i},-{widths[i]}}}" : $"{{{i}}}"));
    Console.WriteLine("  " + string.Format(fmt, headers.Cast<object>().ToArray()));
    Console.WriteLine("  " + string.Join(" ", headers.Select((h, i) =>
        new string('─', i < widths.Length && widths[i] > 0 ? widths[i] : h.Length))));
    foreach (var row in rows)
        Console.WriteLine("  " + string.Format(fmt, row.Select((c, i) =>
            (object)(i < widths.Length && widths[i] > 0 ? Trunc(c, widths[i]) : c)).ToArray()));
}

// ── .env loader ─────────────────────────────────────────────────────────────

static void LoadDotEnv()
{
    // Walk up from current directory to find .env
    var dir = Directory.GetCurrentDirectory();
    for (int i = 0; i < 5; i++)
    {
        var envPath = Path.Combine(dir, ".env");
        if (File.Exists(envPath))
        {
            foreach (var line in File.ReadAllLines(envPath))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;
                var eq = trimmed.IndexOf('=');
                if (eq <= 0) continue;
                var key = trimmed[..eq].Trim();
                var val = trimmed[(eq + 1)..].Trim();
                if (Environment.GetEnvironmentVariable(key) is null)
                    Environment.SetEnvironmentVariable(key, val);
            }
            return;
        }
        var parent = Path.GetDirectoryName(dir);
        if (parent is null) break;
        dir = parent;
    }
}

static string RequireEnv(string key)
{
    var val = Environment.GetEnvironmentVariable(key);
    if (string.IsNullOrWhiteSpace(val))
    {
        Console.Error.WriteLine($"Missing required environment variable: {key}");
        Console.Error.WriteLine("Copy .env.example to .env and fill in your values.");
        Environment.Exit(1);
    }
    return val;
}

static string Env(string key, string fallback = "") =>
    Environment.GetEnvironmentVariable(key) ?? fallback;

// ═══════════════════════════════════════════════════════════════════════════════
// CSV Parser — adapt this to match your system's export format
// ═══════════════════════════════════════════════════════════════════════════════

record CsvWorkOrder
{
    public string OrderNumber   { get; init; } = "";
    public string AccountNumber { get; init; } = "";
    public string CustomerName  { get; init; } = "";
    public string Street        { get; init; } = "";
    public string StreetNumber  { get; init; } = "";
    public string City          { get; init; } = "";
    public string State         { get; init; } = "";
    public string Zip           { get; init; } = "";
    public string Container     { get; init; } = "";
    public string ServiceType   { get; init; } = "";
    public string Destination   { get; init; } = "";
    public double? Longitude    { get; init; }
    public double? Latitude     { get; init; }
    public string FullAddress   { get; init; } = "";
}

static class CsvParser
{
    public static List<CsvWorkOrder> Parse(string path)
    {
        var orders = new List<CsvWorkOrder>();
        foreach (var line in File.ReadLines(path).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var f = SplitCsvLine(line);
            if (f.Count < 14) continue;
            orders.Add(new CsvWorkOrder
            {
                OrderNumber   = f[0].Trim(),  AccountNumber = f[1].Trim(),
                CustomerName  = f[2].Trim(),  Street        = f[3].Trim(),
                StreetNumber  = f[4].Trim(),  City          = f[5].Trim(),
                State         = f[6].Trim(),  Zip           = f[7].Trim(),
                Container     = f[8].Trim(),  ServiceType   = f[9].Trim(),
                Destination   = f[10].Trim(),
                Longitude     = double.TryParse(f[11], out var lng) ? lng : null,
                Latitude      = double.TryParse(f[12], out var lat) ? lat : null,
                FullAddress   = f[13].Trim(),
            });
        }
        return orders;
    }

    static List<string> SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var cur = new StringBuilder();
        bool q = false;
        foreach (var c in line)
        {
            if (c == '"') q = !q;
            else if (c == ',' && !q) { fields.Add(cur.ToString()); cur.Clear(); }
            else cur.Append(c);
        }
        fields.Add(cur.ToString());
        return fields;
    }
}
