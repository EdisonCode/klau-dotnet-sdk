using WebhookIntegration.Models;

namespace WebhookIntegration.Database;

/// <summary>
/// Interface to your source system's database.
///
/// Replace <see cref="MockSourceDatabase"/> with your own implementation
/// backed by SQL Server, PostgreSQL, or whatever your backend uses.
/// Each method maps to a specific integration concern — implement them
/// against your schema and you have a working Klau integration.
/// </summary>
public interface ISourceDatabase
{
    // ─── Outbound: Your System → Klau ──────────────────────────────────────

    /// <summary>
    /// Return jobs that need to be created in Klau.
    /// These are new work orders that haven't been synced yet.
    /// Typically: SELECT * FROM orders WHERE klau_job_id IS NULL AND status = 'new'
    /// </summary>
    Task<IReadOnlyList<SourceJob>> GetPendingJobsAsync();

    /// <summary>
    /// After batch-creating jobs in Klau, store the Klau job IDs
    /// so we can push status changes and correlate webhook events.
    /// Typically: UPDATE orders SET klau_job_id = @klauId WHERE order_id = @orderId
    /// </summary>
    /// <param name="syncedJobs">Pairs of (sourceOrderId, klauJobId).</param>
    Task RecordKlauJobIdsAsync(IReadOnlyList<(string SourceOrderId, string KlauJobId)> syncedJobs);

    /// <summary>
    /// Return jobs where the driver's tablet reported a status change
    /// that hasn't been pushed to Klau yet.
    /// Typically: SELECT * FROM order_status_log WHERE synced_to_klau = false
    /// </summary>
    Task<IReadOnlyList<StatusChange>> GetPendingStatusChangesAsync();

    /// <summary>
    /// Mark a status change as synced to Klau.
    /// Typically: UPDATE order_status_log SET synced_to_klau = true WHERE order_id = @id
    /// </summary>
    Task MarkStatusSyncedAsync(string sourceOrderId);

    // ─── Inbound: Klau → Your System ──────────────────────────────────────

    /// <summary>
    /// Write a driver assignment from Klau back into your system.
    /// Called when Klau's optimizer assigns a job to a driver.
    /// Typically: UPDATE orders SET driver_id = @driverId, sequence = @seq WHERE order_id = @id
    /// </summary>
    Task RecordAssignmentAsync(string sourceOrderId, DriverAssignment assignment);

    /// <summary>
    /// Look up which source order corresponds to a Klau job ID.
    /// Used when processing inbound webhooks (Klau sends klauJobId,
    /// we need to find the matching record in your system).
    /// Typically: SELECT order_id FROM orders WHERE klau_job_id = @klauJobId
    /// </summary>
    Task<string?> FindSourceOrderIdAsync(string klauJobId);
}
