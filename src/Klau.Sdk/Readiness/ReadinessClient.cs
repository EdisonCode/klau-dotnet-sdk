using Klau.Sdk.Common;
using Microsoft.Extensions.Logging;

namespace Klau.Sdk.Readiness;

public interface IReadinessClient
{
    Task<ReadinessReport> CheckAsync(CancellationToken ct = default);
    Task<ReadinessReport> CheckAndLogAsync(ILogger logger, CancellationToken ct = default);
}

/// <summary>
/// Check whether a tenant is properly configured for dispatch optimization.
///
/// Call <see cref="CheckAsync"/> before your first optimization run to surface
/// missing configuration (drivers, trucks, yards, dump sites, materials) before
/// it causes silent failures or suboptimal results.
/// </summary>
public sealed class ReadinessClient : IReadinessClient
{
    private readonly KlauHttpClient _http;
    private readonly string? _tenantId;

    internal ReadinessClient(KlauHttpClient http, string? tenantId = null)
    {
        _http = http;
        _tenantId = tenantId;
    }

    /// <summary>
    /// Run the dispatch readiness check. Returns a report of all configuration
    /// items with their completion status and remediation guidance.
    /// </summary>
    public async Task<ReadinessReport> CheckAsync(CancellationToken ct = default)
    {
        return await _http.GetAsync<ReadinessReport>("api/v1/companies/go-live-readiness", _tenantId, ct);
    }

    /// <summary>
    /// Run the readiness check and log all incomplete items with actionable guidance.
    /// Returns the report so you can inspect it programmatically.
    ///
    /// Use this before your first optimization run to surface issues early.
    /// In production, call it on startup or on a schedule to detect drift.
    ///
    /// <code>
    /// var report = await klau.Readiness.CheckAndLogAsync(logger);
    /// if (!report.CanGoLive)
    ///     Console.WriteLine("Fix the issues above before optimizing.");
    /// </code>
    /// </summary>
    public async Task<ReadinessReport> CheckAndLogAsync(ILogger logger, CancellationToken ct = default)
    {
        var report = await CheckAsync(ct);

        if (report.CanGoLive)
        {
            logger.LogInformation(
                "Klau dispatch readiness: READY ({Percentage}% complete)",
                report.ReadyPercentage);
            return report;
        }

        logger.LogWarning(
            "Klau dispatch readiness: NOT READY ({Percentage}% complete). " +
            "The following issues will prevent or degrade dispatch optimization:",
            report.ReadyPercentage);

        foreach (var section in report.Sections)
        {
            foreach (var item in section.Items)
            {
                if (item.IsComplete) continue;

                var severity = item.Required ? "BLOCKING" : "RECOMMENDED";
                var sdkHint = GetSdkRemediation(item.Key);

                if (sdkHint is not null)
                {
                    logger.LogWarning(
                        "  [{Severity}] {Label}: {Detail}\n" +
                        "    Fix via SDK: {SdkHint}",
                        severity, item.Label, item.Detail ?? "Not configured", sdkHint);
                }
                else
                {
                    logger.LogWarning(
                        "  [{Severity}] {Label}: {Detail}" +
                        (item.Route is not null ? "\n    Fix in dashboard: {Route}" : ""),
                        severity, item.Label, item.Detail ?? "Not configured", item.Route);
                }
            }
        }

        return report;
    }

    /// <summary>
    /// Map readiness check keys to SDK code examples for remediation.
    /// </summary>
    private static string? GetSdkRemediation(string key) => key switch
    {
        "drivers" =>
            "await klau.Drivers.CreateAsync(new CreateDriverRequest { Name = \"Driver Name\" })",
        "trucks" =>
            "await klau.Trucks.CreateAsync(new CreateTruckRequest { Number = \"T-001\", CompatibleSizes = [20, 30, 40] })",
        "yards" =>
            "await klau.Yards.CreateAsync(new CreateYardRequest { Name = \"Main Yard\", Address = \"...\", IsDefault = true })",
        "dumpSites" =>
            "await klau.DumpSites.CreateAsync(new CreateDumpSiteRequest { Name = \"Central Landfill\", Address = \"...\" })",
        "dumpSiteMaterials" =>
            "await klau.DumpSites.AddMaterialPricingAsync(dumpSiteId, new AddMaterialPricingRequest { ... })",
        "companyInfo" =>
            "Set timezone and workdays in Settings > Company in the Klau dashboard",
        _ => null,
    };
}
