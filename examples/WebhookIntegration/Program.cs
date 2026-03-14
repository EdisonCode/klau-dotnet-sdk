// ──────────────────────────────────────────────────────────────────────────────
// WebhookIntegration — Bidirectional Klau integration for enterprise backends
//
// This is a deployable Kestrel web app that keeps your source database and
// Klau in sync. It handles both directions:
//
//   Your DB → Klau:  Background service polls every 5 min for new jobs and
//                     driver status changes, pushes them to Klau.
//
//   Klau → Your DB:  Webhook endpoint receives real-time events (assignments,
//                     optimization results) and writes them back.
//
// To integrate with your backend:
//   1. Implement ISourceDatabase against your real database
//   2. Set your API key and webhook secret in appsettings.json (or env vars)
//   3. Deploy and register the webhook URL in Klau's Developer Settings
//
// The mock implementation seeds sample data so you can run this immediately
// and see the full flow in action.
// ──────────────────────────────────────────────────────────────────────────────

using System.Text.Json;
using Klau.Sdk;
using Klau.Sdk.Common;
using Klau.Sdk.Webhooks;
using WebhookIntegration.Database;
using WebhookIntegration.Models;
using WebhookIntegration.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Services ────────────────────────────────────────────────────────────────

// Register Klau SDK — reads API key from Klau:ApiKey config or KLAU_API_KEY env var.
// Also registers KlauWebhookValidator if a webhook secret is configured.
builder.Services.AddKlauClient(options =>
    builder.Configuration.GetSection("Klau").Bind(options));

// Register your database.
// ┌──────────────────────────────────────────────────────────────────────┐
// │  REPLACE THIS with your real ISourceDatabase implementation.        │
// │  e.g.: builder.Services.AddScoped<ISourceDatabase, SqlDatabase>();  │
// └──────────────────────────────────────────────────────────────────────┘
builder.Services.AddSingleton<MockSourceDatabase>();
builder.Services.AddSingleton<ISourceDatabase>(sp => sp.GetRequiredService<MockSourceDatabase>());

// Background service that polls your DB and syncs to Klau.
builder.Services.AddHostedService<JobSyncService>();

var app = builder.Build();

// ── Webhook Endpoint ────────────────────────────────────────────────────────
//
// Register this URL in Klau: Settings > Developer > Webhooks
// Subscribe to: job.assigned, job.status_changed, dispatch.optimized
//
// Klau delivers events with HMAC-SHA256 signatures. In production,
// always validate the signature. For local dev, you can skip it.

app.MapPost("/webhook/klau", async (HttpContext ctx, ISourceDatabase db, KlauClient klau, ILogger<Program> logger) =>
{
    // Read the raw body (must be read before any other processing)
    ctx.Request.EnableBuffering();
    var body = await new StreamReader(ctx.Request.Body).ReadToEndAsync();

    // Validate HMAC signature in production
    var validator = ctx.RequestServices.GetService<KlauWebhookValidator>();
    if (validator is not null)
    {
        var signature = ctx.Request.Headers["Klau-Signature"].ToString();
        if (!validator.Validate(signature, body))
        {
            logger.LogWarning("Webhook signature validation failed");
            return Results.Unauthorized();
        }
    }

    // Parse the event envelope
    WebhookEvent evt;
    try
    {
        evt = JsonSerializer.Deserialize<WebhookEvent>(body, KlauHttpClient.JsonOptions)
            ?? throw new JsonException("Null event");
    }
    catch (JsonException ex)
    {
        logger.LogWarning(ex, "Failed to parse webhook event");
        return Results.BadRequest();
    }

    logger.LogInformation("Received webhook: {EventType} (id: {EventId})", evt.Type, evt.Id);

    // Route by event type
    switch (evt.Type)
    {
        case "job.assigned":
            await HandleJobAssigned(evt, db, klau, logger);
            break;

        case "job.status_changed":
            await HandleJobStatusChanged(evt, db, logger);
            break;

        case "dispatch.optimized":
            HandleDispatchOptimized(evt, logger);
            break;

        default:
            logger.LogDebug("Ignoring unhandled event type: {Type}", evt.Type);
            break;
    }

    // Always return 200 quickly — Klau retries on non-2xx.
    return Results.Ok();
});

// ── Status Endpoint ─────────────────────────────────────────────────────────
// Quick way to see the current state of all jobs in the mock database.

app.MapGet("/status", (MockSourceDatabase db) => Results.Json(db.GetAllJobStatus()));

// ── Simulation Endpoints ────────────────────────────────────────────────────
// These simulate driver tablet actions so you can test the full loop
// without a real tablet. In production, these wouldn't exist — the
// status changes would come from your actual driver app / DB triggers.

app.MapPost("/simulate/start/{orderId}", (string orderId, MockSourceDatabase db) =>
{
    db.SimulateDriverStart(orderId);
    return Results.Ok(new { message = $"Simulated driver START for {orderId}" });
});

app.MapPost("/simulate/complete/{orderId}", (string orderId, MockSourceDatabase db) =>
{
    db.SimulateDriverComplete(orderId);
    return Results.Ok(new { message = $"Simulated driver COMPLETE for {orderId}" });
});

app.Run();

// ═══════════════════════════════════════════════════════════════════════════
// Webhook event handlers
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// When Klau assigns a job to a driver (via optimization or manual assignment),
/// fetch the full job details and write the assignment back to your source DB.
/// </summary>
static async Task HandleJobAssigned(
    WebhookEvent evt, ISourceDatabase db, KlauClient klau, ILogger logger)
{
    var data = evt.Data.Deserialize<JobAssignedEvent>(KlauHttpClient.JsonOptions);
    if (data is null) return;

    // Find the corresponding order in our source DB
    var sourceOrderId = await db.FindSourceOrderIdAsync(data.JobId);
    if (sourceOrderId is null)
    {
        logger.LogWarning(
            "Received job.assigned for unknown Klau job {KlauJobId} — skipping", data.JobId);
        return;
    }

    // The webhook only includes jobId + driverId.
    // Fetch the full job to get driver name, sequence, and estimated start time.
    try
    {
        var job = await klau.Jobs.GetAsync(data.JobId);

        await db.RecordAssignmentAsync(sourceOrderId, new DriverAssignment
        {
            KlauJobId = data.JobId,
            DriverId = data.DriverId,
            DriverName = job.DriverName,
            Sequence = job.Sequence,
            EstimatedStartTime = job.EstimatedStartTime,
            AssignmentSource = data.AssignmentSource,
        });

        logger.LogInformation(
            "Wrote assignment for {OrderId}: driver={DriverName} seq={Sequence} source={Source}",
            sourceOrderId, job.DriverName, job.Sequence, data.AssignmentSource);
    }
    catch (KlauApiException ex)
    {
        logger.LogError(
            "Failed to fetch job details for {KlauJobId}: {Code} - {Message}",
            data.JobId, ex.ErrorCode, ex.Message);
    }
}

/// <summary>
/// When Klau reports a status change (e.g. from another integration or
/// the Klau mobile app), update your source DB to stay in sync.
/// This prevents your system and Klau from diverging.
/// </summary>
static async Task HandleJobStatusChanged(WebhookEvent evt, ISourceDatabase db, ILogger logger)
{
    var data = evt.Data.Deserialize<JobStatusChangedEvent>(KlauHttpClient.JsonOptions);
    if (data is null) return;

    var sourceOrderId = await db.FindSourceOrderIdAsync(data.JobId);
    if (sourceOrderId is null)
    {
        logger.LogDebug("Ignoring status change for unknown Klau job {KlauJobId}", data.JobId);
        return;
    }

    logger.LogInformation(
        "Klau status change for {OrderId}: {From} → {To}",
        sourceOrderId, data.PreviousStatus, data.NewStatus);

    // In your implementation, update the source DB status here.
    // Be careful to avoid loops: if this status change originated from YOUR system
    // (pushed by the sync service), you may want to ignore it.
}

/// <summary>
/// When Klau finishes an optimization run, log the results.
/// You could use this to trigger a notification, update a dashboard,
/// or pull the full dispatch board for reporting.
/// </summary>
static void HandleDispatchOptimized(WebhookEvent evt, ILogger logger)
{
    var data = evt.Data.Deserialize<DispatchOptimizedEvent>(KlauHttpClient.JsonOptions);
    if (data is null) return;

    logger.LogInformation(
        "Dispatch optimized for {Date}: {Assigned}/{Total} jobs assigned, " +
        "{Chains} chains formed, {Minutes} minutes saved",
        data.Date,
        data.Metrics.AssignedJobs,
        data.Metrics.TotalJobs,
        data.Metrics.ChainsFormed,
        data.Metrics.EstimatedMinutesSaved);
}
