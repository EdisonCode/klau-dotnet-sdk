# WebhookIntegration Example

Deployable Kestrel web app that keeps your backend database and Klau in bidirectional sync. This is the starting point for enterprise teams integrating their existing dispatch system with Klau.

## What it does

```
┌─────────────────┐         ┌──────────────┐         ┌───────────┐
│  Your Database   │ ──────→ │  This App    │ ──────→ │   Klau    │
│                  │         │  (Kestrel)   │         │           │
│  - New jobs      │  poll   │              │  batch  │  Optimize │
│  - Driver starts │  5 min  │  JobSync     │  create │  Assign   │
│  - Driver done   │         │  Service     │  start  │  Publish  │
│                  │         │              │  done   │           │
│                  │ ←────── │              │ ←────── │           │
│  - Assignments   │  write  │  Webhook     │  event  │  Webhook  │
│  - Sequences     │  back   │  Endpoint    │  POST   │  Delivery │
└─────────────────┘         └──────────────┘         └───────────┘
```

**Outbound (Your DB → Klau):** Background service polls every 5 minutes for new jobs and driver status changes, pushes them to Klau via the SDK.

**Inbound (Klau → Your DB):** Webhook endpoint receives `job.assigned` and `dispatch.optimized` events, fetches full job details, writes driver assignments back to your database.

## Quick start

```bash
# Set your credentials
export KLAU_API_KEY="kl_live_your_key"
export KLAU_WEBHOOK_SECRET="whsec_your_secret"  # optional for local dev

# Run it
dotnet run
```

The app starts on `http://localhost:5000` with:
- `POST /webhook/klau` — Webhook receiver (register this URL in Klau)
- `GET /status` — View current job sync state
- `POST /simulate/start/{orderId}` — Simulate a driver starting a job on their tablet
- `POST /simulate/complete/{orderId}` — Simulate a driver completing a job

## Integrate with your database

The entire database interface is in `Database/ISourceDatabase.cs`. Replace `MockSourceDatabase` with your implementation:

```csharp
// In Program.cs, change this:
builder.Services.AddSingleton<ISourceDatabase>(sp => sp.GetRequiredService<MockSourceDatabase>());

// To this:
builder.Services.AddScoped<ISourceDatabase, YourSqlDatabase>();
```

Each method in the interface has SQL-level comments explaining what query it maps to. The six methods you need to implement:

| Method | Direction | What it does |
|--------|-----------|-------------|
| `GetPendingJobsAsync` | → Klau | Return new jobs not yet synced |
| `RecordKlauJobIdsAsync` | → Klau | Store Klau job IDs after batch create |
| `GetPendingStatusChangesAsync` | → Klau | Return driver tablet status changes |
| `MarkStatusSyncedAsync` | → Klau | Mark a status change as synced |
| `RecordAssignmentAsync` | ← Klau | Write driver assignment from webhook |
| `FindSourceOrderIdAsync` | ← Klau | Map Klau job ID → your order ID |

## Testing the full loop

1. Start the app — the sync service immediately finds 6 seeded jobs and creates them in Klau
2. In Klau, run optimization for today's date — jobs get assigned to drivers
3. Klau sends `job.assigned` webhooks → the app writes assignments back to your DB
4. Hit `GET /status` to see assignments
5. Simulate a driver starting a job: `POST /simulate/start/WO-10001`
6. Wait for the next poll cycle (or restart) — the sync service pushes the status to Klau
7. Klau's dispatch board now shows the job as IN_PROGRESS

## Configuration

Set via `appsettings.json` or environment variables:

| Setting | Env var | Default | Description |
|---------|---------|---------|-------------|
| `Klau:ApiKey` | `KLAU_API_KEY` | — | Your Klau API key |
| `Klau:WebhookSecret` | `KLAU_WEBHOOK_SECRET` | — | Webhook signing secret (from Klau) |
| `Klau:PollIntervalMinutes` | — | `5` | How often to poll your source DB |
