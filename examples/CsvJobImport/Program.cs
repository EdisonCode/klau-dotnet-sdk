// ──────────────────────────────────────────────────────────────────────────────
// CsvJobImport — Minimal example of the Klau SDK import-and-optimize flow
//
// This example shows the simplest integration path:
//   1. Parse a CSV export from your existing system
//   2. Import into Klau (auto-creates customers + sites by name)
//   3. Run dispatch optimization
//   4. Read back driver assignments
//
// For a full enterprise integration with master-data setup, container size
// detection, and dispatch plan export, see the EnterpriseSimulator example.
// ──────────────────────────────────────────────────────────────────────────────

using Klau.Sdk;
using Klau.Sdk.Common;
using Klau.Sdk.Dispatches;
using Klau.Sdk.Import;
using Microsoft.Extensions.Logging;

// ── Configuration ───────────────────────────────────────────────────────────

var csvPath = args.Length > 0 ? args[0] : "sample-orders.csv";
var date = args.Length > 1 ? args[1] : DateTime.Today.ToString("yyyy-MM-dd");

// ── Step 0: Initialize and check readiness ─────────────────────────────────

using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
var logger = loggerFactory.CreateLogger("CsvJobImport");

using var klau = KlauClient.CreateFromEnvironment(logger: logger);

Console.WriteLine("Checking dispatch readiness...\n");
var readiness = await klau.Readiness.CheckAndLogAsync(logger);
if (!readiness.CanGoLive)
{
    Console.Error.WriteLine(
        "\nDispatch readiness check failed. Fix the issues above before importing jobs.\n" +
        "See the EnterpriseSimulator example for how to push drivers, trucks, yards, and dump sites.");
    return;
}

// ── Step 1: Parse the CSV ───────────────────────────────────────────────────

Console.WriteLine($"Loading orders from {csvPath}...");
var orders = ParseCsv(csvPath);
Console.WriteLine($"Parsed {orders.Count} orders\n");

foreach (var order in orders)
    Console.WriteLine($"  {order.OrderNumber,-8} {order.ServiceType,-22} " +
        $"{order.CustomerName,-30} {order.ContainerSize,2}yd  {order.City}, {order.State}");

// ── Step 2: Import jobs ─────────────────────────────────────────────────────
// ImportAndWaitAsync resolves customers and sites by name, auto-creates
// missing records, and waits for drive-time cache warm-up so the optimizer
// has real truck routing times from HERE Maps.

Console.WriteLine($"\nImporting {orders.Count} jobs for {date}...");

var importRecords = orders.Select(o => new ImportJobRecord
{
    CustomerName  = o.CustomerName,
    SiteName      = !string.IsNullOrEmpty(o.Street) ? $"{o.StreetNumber} {o.Street}" : o.CustomerName,
    SiteAddress   = $"{o.StreetNumber} {o.Street}",
    SiteCity      = o.City,
    SiteState     = o.State,
    SiteZip       = o.Zip,
    JobType       = MapServiceType(o.ServiceType),
    ContainerSize = o.ContainerSize > 0 ? o.ContainerSize.ToString() : "30",
    TimeWindow    = "ANYTIME",
    RequestedDate = date,
    ExternalId    = o.OrderNumber,
    Notes         = !string.IsNullOrEmpty(o.Destination) ? $"Dest: {o.Destination}" : null,
}).ToList();

var import = await klau.Import.ImportAndWaitAsync(
    new ImportJobsRequest { Jobs = importRecords, CreateMissing = true },
    timeout: TimeSpan.FromSeconds(90),
    pollInterval: TimeSpan.FromSeconds(2));

Console.WriteLine($"  Imported: {import.Imported}  |  Skipped: {import.Skipped}");
if (import.CustomersCreated > 0 || import.SitesCreated > 0)
    Console.WriteLine($"  Auto-created: {import.CustomersCreated} customers, {import.SitesCreated} sites");

foreach (var err in import.Errors)
    Console.WriteLine($"  Error row {err.Row}: {err.Field} - {err.Message}");

if (import.Imported == 0)
{
    Console.Error.WriteLine("No jobs imported — nothing to optimize.");
    return;
}

// ── Step 3: Optimize dispatch ───────────────────────────────────────────────

Console.WriteLine($"\nOptimizing dispatch for {date}...");

var optimization = await klau.Dispatches.OptimizeAndWaitAsync(
    new OptimizeRequest { Date = date, OptimizationMode = OptimizationMode.FULL_DAY },
    pollInterval: TimeSpan.FromSeconds(3));

if (optimization.Status == OptimizationJobStatus.COMPLETED && optimization.Result is { } r)
{
    Console.WriteLine($"  Grade: {r.PlanGrade} ({r.PlanQuality}/100)  |  Flow: {r.FlowScore}/100");
    Console.WriteLine($"  Assigned: {r.AssignedJobs}/{r.TotalJobs}  |  Drive times: {r.DriveTimeSource}");
}
else
{
    Console.WriteLine($"  Optimization {optimization.Status}: {optimization.Reason}");
}

// ── Step 4: Read back driver assignments ────────────────────────────────────

Console.WriteLine($"\nDriver assignments for {date}:");
Console.WriteLine(new string('─', 90));

var board = await klau.Dispatches.GetBoardAsync(date);

foreach (var driver in board.Drivers.Where(d => d.Jobs.Count > 0))
{
    Console.WriteLine($"\n  {driver.Name} ({driver.Jobs.Count} jobs, truck {driver.TruckNumber ?? "—"})");
    foreach (var job in driver.Jobs.OrderBy(j => j.Sequence))
        Console.WriteLine($"    #{job.Sequence,-3} {job.Type,-14} {Truncate(job.CustomerName, 25),-25} " +
            $"{job.ExternalId,-10} {(job.ContainerSize.HasValue ? $"{job.ContainerSize}yd" : "—")}");
}

if (board.UnassignedJobs.Count > 0)
{
    Console.WriteLine($"\n  Unassigned ({board.UnassignedJobs.Count}):");
    foreach (var job in board.UnassignedJobs)
        Console.WriteLine($"    {job.ExternalId,-10} {job.CustomerName} - {job.Type}");
}

Console.WriteLine($"\n{new string('─', 90)}");
Console.WriteLine("Done. Review assignments in Klau, then publish when ready:");
Console.WriteLine($"  await klau.Dispatches.PublishAsync(\"{date}\");");

// ═══════════════════════════════════════════════════════════════════════════
// Helpers
// ═══════════════════════════════════════════════════════════════════════════

static List<CsvOrder> ParseCsv(string path)
{
    var orders = new List<CsvOrder>();
    foreach (var line in File.ReadLines(path).Skip(1))
    {
        var fields = ParseCsvLine(line);
        if (fields.Count < 14) continue;
        orders.Add(new CsvOrder
        {
            OrderNumber  = fields[0].Trim(),
            AccountNumber = fields[1].Trim(),
            CustomerName = fields[2].Trim(),
            Street       = fields[3].Trim(),
            StreetNumber = fields[4].Trim(),
            City         = fields[5].Trim(),
            State        = fields[6].Trim(),
            Zip          = fields[7].Trim(),
            Container    = fields[8].Trim(),
            ServiceType  = fields[9].Trim(),
            Destination  = fields[10].Trim(),
            Longitude    = double.TryParse(fields[11], out var lng) ? lng : null,
            Latitude     = double.TryParse(fields[12], out var lat) ? lat : null,
            FullAddress  = fields[13].Trim(),
        });
    }
    return orders;
}

static List<string> ParseCsvLine(string line)
{
    var fields = new List<string>();
    var current = new System.Text.StringBuilder();
    var inQuotes = false;
    for (int i = 0; i < line.Length; i++)
    {
        var c = line[i];
        if (c == '"') inQuotes = !inQuotes;
        else if (c == ',' && !inQuotes) { fields.Add(current.ToString()); current.Clear(); }
        else current.Append(c);
    }
    fields.Add(current.ToString());
    return fields;
}

static string MapServiceType(string service) => service.ToUpperInvariant() switch
{
    var s when s.Contains("DUMP & RETURN") => "DUMP_RETURN",
    var s when s.Contains("DELIVERY")      => "DELIVERY",
    var s when s.Contains("DISCONTINUE")   => "PICKUP",
    var s when s.Contains("SWAP")          => "SWAP",
    var s when s.Contains("NEW SERVICE")   => "DELIVERY",
    _                                      => "DELIVERY",
};

static string Truncate(string value, int maxLength) =>
    value.Length <= maxLength ? value : value[..(maxLength - 1)] + "…";

record CsvOrder
{
    public string OrderNumber { get; init; } = "";
    public string AccountNumber { get; init; } = "";
    public string CustomerName { get; init; } = "";
    public string Street { get; init; } = "";
    public string StreetNumber { get; init; } = "";
    public string City { get; init; } = "";
    public string State { get; init; } = "";
    public string Zip { get; init; } = "";
    public string Container { get; init; } = "";
    public string ServiceType { get; init; } = "";
    public string Destination { get; init; } = "";
    public double? Longitude { get; init; }
    public double? Latitude { get; init; }
    public string FullAddress { get; init; } = "";

    public int ContainerSize
    {
        get
        {
            var parts = Container.Split(' ');
            if (parts.Length >= 2 && int.TryParse(parts[0], out var size) && size >= 10)
                return size;
            return 0;
        }
    }
}
