using Klau.Sdk.Webhooks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Klau.Sdk;

/// <summary>
/// Extension methods for registering Klau SDK services in an ASP.NET Core DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register a singleton <see cref="KlauClient"/> configured from <see cref="KlauClientOptions"/>.
    ///
    /// If <see cref="KlauClientOptions.ApiKey"/> is not set, the SDK reads from
    /// the <c>KLAU_API_KEY</c> environment variable automatically.
    ///
    /// If <see cref="KlauClientOptions.WebhookSecret"/> is set (or <c>KLAU_WEBHOOK_SECRET</c>
    /// env var is present), a <see cref="KlauWebhookValidator"/> is also registered.
    ///
    /// <code>
    /// // Minimal — reads API key from KLAU_API_KEY env var
    /// builder.Services.AddKlauClient();
    ///
    /// // From configuration section
    /// builder.Services.AddKlauClient(options =>
    ///     builder.Configuration.GetSection("Klau").Bind(options));
    ///
    /// // Explicit
    /// builder.Services.AddKlauClient(options =>
    /// {
    ///     options.ApiKey = "kl_live_...";
    ///     options.WebhookSecret = "whsec_...";
    ///     options.TimeoutSeconds = 15;
    /// });
    /// </code>
    /// </summary>
    public static IServiceCollection AddKlauClient(
        this IServiceCollection services,
        Action<KlauClientOptions>? configure = null)
    {
        // Resolve options once to check webhook secret availability
        var options = new KlauClientOptions();
        configure?.Invoke(options);

        services.AddSingleton(sp =>
        {
            var opts = new KlauClientOptions();
            configure?.Invoke(opts);

            var logger = sp.GetService<ILoggerFactory>()?.CreateLogger<KlauClient>();
            return KlauClient.Create(opts, logger: logger);
        });

        // Register webhook validator only if a secret is configured
        var secret = options.ResolveWebhookSecret();
        if (secret is not null)
        {
            services.AddSingleton(new KlauWebhookValidator(secret));
        }

        return services;
    }
}
