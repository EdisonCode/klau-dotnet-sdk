// ──────────────────────────────────────────────────────────────────────────────
// CsvJobImport — End-to-end example of the Klau SDK integration hot path
//
// This example shows how an enterprise .NET team would:
//   1. Parse a CSV export from their existing dispatch system
//   2. Batch-load those work orders into Klau as jobs
//   3. Run dispatch optimization
//   4. Read back driver assignments and optimized job sequences
//
// The CSV format matches a typical roll-off waste hauler's daily order export.
// ──────────────────────────────────────────────────────────────────────────────

using Klau.Sdk;
using Klau.Sdk.Common;
using Klau.Sdk.Jobs;
using Klau.Sdk.Dispatches;

// ── Configuration ───────────────────────────────────────────────────────────

var apiKey = Environment.GetEnvironmentVariable("KLAU_API_KEY")
    ?? throw new InvalidOperationException(
        "Set the KLAU_API_KEY environment variable. " +
        "Generate one at Settings > Developer in your Klau dashboard.");

var csvPath = args.Length > 0 ? args[0] : "sample-orders.csv";
var date = args.Length > 1 ? args[1] : DateTime.Today.ToString("yyyy-MM-dd");

// ── Step 1: Parse the CSV ───────────────────────────────────────────────────

Console.WriteLine($"Loading orders from {csvPath}...");

var orders = ParseCsv(csvPath);
Console.WriteLine($"Parsed {orders.Count} orders");

// Show what we found
foreach (var order in orders)
{
    Console.WriteLine($"  {order.OrderNumber,-8} {order.ServiceType,-22} " +
        $"{order.CustomerName,-30} {order.ContainerSize,2}yd  {order.City}, {order.State}");
}

// ── Step 2: Map to Klau jobs and batch-create ───────────────────────────────

Console.WriteLine($"\nCreating {orders.Count} jobs in Klau for {date}...");

using var klau = new KlauClient(apiKey);

var jobRequests = orders
    .Select(o => new CreateJobRequest
    {
        // Use a placeholder customer ID — in production you'd look these up
        // or use Klau's createMissing option to auto-create customer stubs
        CustomerId = "default",
        Type = MapServiceType(o.ServiceType),
        ContainerSize = o.ContainerSize > 0 ? o.ContainerSize : null,
        RequestedDate = date,
        TimeWindow = TimeWindow.ANYTIME,
        Notes = BuildNotes(o),
        ExternalId = o.OrderNumber,  // ← your system's ID, the key to two-way sync
    })
    .ToList();

var result = await klau.Jobs.CreateBatchAsync(jobRequests);

Console.WriteLine($"  Created: {result.Created.Count}");
if (result.Errors.Count > 0)
{
    Console.WriteLine($"  Errors:  {result.Errors.Count}");
    foreach (var err in result.Errors)
        Console.WriteLine($"    Order {orders[err.Index].OrderNumber}: {err.Code} - {err.Message}");
}

// Build a lookup from Klau job ID → original order number
var jobToOrder = result.Created.ToDictionary(c => c.JobId, c => c.ExternalId ?? "?");

// ── Step 3: Optimize dispatch ───────────────────────────────────────────────

Console.WriteLine($"\nOptimizing dispatch for {date}...");

var optimization = await klau.Dispatches.OptimizeAndWaitAsync(
    new OptimizeRequest
    {
        Date = date,
        OptimizationMode = OptimizationMode.FULL_DAY,
    },
    pollInterval: TimeSpan.FromSeconds(3));

switch (optimization.Status)
{
    case OptimizationJobStatus.COMPLETED:
        var r = optimization.Result!;
        Console.WriteLine($"  Plan grade: {r.PlanGrade} ({r.PlanQuality}/100)");
        Console.WriteLine($"  Flow score: {r.FlowScore}/100");
        Console.WriteLine($"  Assigned:   {r.AssignedJobs}/{r.TotalJobs} jobs");
        break;

    case OptimizationJobStatus.FAILED:
        Console.WriteLine($"  Optimization failed: {optimization.Reason}");
        return;

    case OptimizationJobStatus.SKIPPED:
        Console.WriteLine($"  Optimization skipped: {optimization.Reason}");
        break;
}

// ── Step 4: Read back driver assignments ────────────────────────────────────

Console.WriteLine($"\nDriver assignments for {date}:");
Console.WriteLine(new string('─', 90));

var board = await klau.Dispatches.GetBoardAsync(date);

foreach (var dispatch in board.Dispatches)
{
    Console.WriteLine($"\n  Driver: {dispatch.DriverName} ({dispatch.DriverId})");
    Console.WriteLine($"  Status: {dispatch.Status}");
    Console.WriteLine($"  {"Seq",-4} {"Type",-14} {"Customer",-28} {"Size",-6} {"Order #"}");
    Console.WriteLine($"  {"───",-4} {"──────────────",-14} {"────────────────────────────",-28} {"──────",-6} {"───────"}");

    foreach (var job in dispatch.Jobs.OrderBy(j => j.Sequence))
    {
        Console.WriteLine($"  {job.Sequence,-4} {job.Type,-14} {Truncate(job.CustomerName, 28),-28} " +
            $"{(job.ContainerSize.HasValue ? $"{job.ContainerSize}yd" : "—"),-6} {job.ExternalId}");
    }
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
// CSV parsing and mapping helpers
// ═══════════════════════════════════════════════════════════════════════════

static List<CsvOrder> ParseCsv(string path)
{
    var orders = new List<CsvOrder>();

    foreach (var line in File.ReadLines(path).Skip(1)) // skip header
    {
        var fields = ParseCsvLine(line);
        if (fields.Count < 14) continue;

        orders.Add(new CsvOrder
        {
            OrderNumber = fields[0].Trim(),
            AccountNumber = fields[1].Trim(),
            CustomerName = fields[2].Trim(),
            Street = fields[3].Trim(),
            StreetNumber = fields[4].Trim(),
            City = fields[5].Trim(),
            State = fields[6].Trim(),
            Zip = fields[7].Trim(),
            Container = fields[8].Trim(),
            ServiceType = fields[9].Trim(),
            Destination = fields[10].Trim(),
            Longitude = double.TryParse(fields[11], out var lng) ? lng : null,
            Latitude = double.TryParse(fields[12], out var lat) ? lat : null,
            FullAddress = fields[13].Trim(),
        });
    }

    return orders;
}

/// <summary>
/// Parse a CSV line respecting quoted fields that may contain commas.
/// </summary>
static List<string> ParseCsvLine(string line)
{
    var fields = new List<string>();
    var current = new System.Text.StringBuilder();
    var inQuotes = false;

    for (int i = 0; i < line.Length; i++)
    {
        var c = line[i];
        if (c == '"')
        {
            inQuotes = !inQuotes;
        }
        else if (c == ',' && !inQuotes)
        {
            fields.Add(current.ToString());
            current.Clear();
        }
        else
        {
            current.Append(c);
        }
    }
    fields.Add(current.ToString());
    return fields;
}

/// <summary>
/// Map hauler service codes to Klau job types.
/// Adapt this mapping to match your system's codes.
/// </summary>
static JobType MapServiceType(string service) => service.ToUpperInvariant() switch
{
    var s when s.Contains("DUMP & RETURN") => JobType.DUMP_RETURN,
    var s when s.Contains("DELIVERY") => JobType.DELIVERY,
    var s when s.Contains("DISCONTINUE") => JobType.PICKUP,        // pickup = end of service
    var s when s.Contains("SWAP") => JobType.SWAP,
    var s when s.Contains("MISSED") => JobType.SERVICE_VISIT,
    var s when s.Contains("NEW SERVICE") => JobType.DELIVERY,      // new service = first delivery
    var s when s.Contains("DRIVER NOTE") => JobType.SERVICE_VISIT,
    _ => JobType.SERVICE_VISIT,
};

static string BuildNotes(CsvOrder order)
{
    var parts = new List<string>();
    if (!string.IsNullOrEmpty(order.Destination))
        parts.Add($"Destination: {order.Destination}");
    if (!string.IsNullOrEmpty(order.FullAddress))
        parts.Add($"Address: {order.FullAddress}");
    return string.Join(" | ", parts);
}

static string Truncate(string value, int maxLength) =>
    value.Length <= maxLength ? value : value[..(maxLength - 1)] + "…";

// ═══════════════════════════════════════════════════════════════════════════

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

    /// <summary>
    /// Extract container size in yards from the Container column.
    /// Formats: "30 YARD ROLL OFF", "20 YARD ROLL OFF", "35 YARD COMPACTOR", etc.
    /// Returns 0 for non-roll-off containers (toters, frontload, etc.)
    /// </summary>
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
