using Klau.Sdk.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Klau.Sdk;

/// <summary>
/// ASP.NET Core health check that validates the Klau API connection.
/// Calls <c>Auth.ValidateAsync()</c> to verify the API key is valid and
/// the service is reachable.
///
/// Register with:
/// <code>
/// builder.Services.AddHealthChecks()
///     .AddKlauCheck();
/// </code>
/// </summary>
public sealed class KlauHealthCheck : IHealthCheck
{
    private readonly KlauClient _client;

    public KlauHealthCheck(KlauClient client) => _client = client;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var company = await _client.Company.GetAsync(cancellationToken);
            return HealthCheckResult.Healthy($"Klau API connection is healthy (company: {company.Name})");
        }
        catch (KlauApiException ex)
        {
            return HealthCheckResult.Unhealthy(
                $"Klau API error: {ex.ErrorCode} — {ex.Message}",
                ex);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Klau API is unreachable",
                ex);
        }
    }
}

/// <summary>
/// Extension methods for registering Klau health checks.
/// </summary>
public static class KlauHealthCheckExtensions
{
    /// <summary>
    /// Add a health check that validates the Klau API connection.
    /// Requires <see cref="ServiceCollectionExtensions.AddKlauClient"/> to be registered first.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">Health check name. Defaults to "klau".</param>
    /// <param name="failureStatus">The <see cref="HealthStatus"/> to report on failure.</param>
    /// <param name="tags">Tags for filtering health checks.</param>
    public static IHealthChecksBuilder AddKlauCheck(
        this IHealthChecksBuilder builder,
        string name = "klau",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        return builder.AddCheck<KlauHealthCheck>(name, failureStatus, tags ?? []);
    }
}
