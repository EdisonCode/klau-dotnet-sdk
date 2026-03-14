using Klau.Sdk.Authentication;
using Klau.Sdk.Common;
using Klau.Sdk.Customers;
using Klau.Sdk.Dispatches;
using Klau.Sdk.DumpTickets;
using Klau.Sdk.Jobs;
using Klau.Sdk.Materials;
using Klau.Sdk.Orders;
using Klau.Sdk.Divisions;
using Klau.Sdk.Proposals;
using Klau.Sdk.Storefronts;
using Klau.Sdk.Webhooks;
using Microsoft.Extensions.Logging;

namespace Klau.Sdk;

/// <summary>
/// Entry point for the Klau .NET SDK.
///
/// Usage:
///   var klau = new KlauClient("kl_live_your_api_key_here");
///   var board = await klau.Dispatches.GetBoardAsync("2026-03-13");
///
/// From environment variable:
///   var klau = KlauClient.CreateFromEnvironment();
///
/// Enterprise (division-scoped):
///   var div = klau.ForTenant("child-company-id");
///   var board = await div.Dispatches.GetBoardAsync("2026-03-13");
/// </summary>
public sealed class KlauClient : IDisposable
{
    internal readonly KlauHttpClient Http;

    /// <summary>Expected prefix for Klau API keys.</summary>
    internal const string ApiKeyPrefix = "kl_live_";

    public AuthClient Auth { get; }
    public JobClient Jobs { get; }
    public CustomerClient Customers { get; }
    public DispatchClient Dispatches { get; }
    public StorefrontClient Storefronts { get; }
    public MaterialClient Materials { get; }
    public DumpTicketClient DumpTickets { get; }
    public OrderClient Orders { get; }
    public ProposalClient Proposals { get; }
    public DivisionClient Divisions { get; }
    public WebhookClient Webhooks { get; }

    /// <summary>
    /// Create a new Klau API client authenticated with an API key.
    /// Generate an API key in Settings &gt; Developer in your Klau dashboard.
    /// </summary>
    /// <param name="apiKey">Your API key (starts with kl_live_).</param>
    /// <param name="httpClient">Optional HttpClient for custom configuration (e.g. proxies). The SDK will NOT dispose a caller-provided HttpClient.</param>
    /// <param name="logger">Optional logger for retry/error diagnostics.</param>
    public KlauClient(string apiKey, HttpClient? httpClient = null, ILogger? logger = null)
        : this(apiKey, "https://api.getklau.com", httpClient, logger)
    {
    }

    /// <summary>
    /// Create a new Klau API client with a custom base URL.
    /// </summary>
    /// <param name="apiKey">Your API key (starts with kl_live_).</param>
    /// <param name="baseUrl">The base URL of the Klau API.</param>
    /// <param name="httpClient">Optional HttpClient for custom configuration. The SDK will NOT dispose a caller-provided HttpClient.</param>
    /// <param name="logger">Optional logger for retry/error diagnostics.</param>
    public KlauClient(string apiKey, string baseUrl, HttpClient? httpClient = null, ILogger? logger = null)
    {
        ValidateApiKey(apiKey);

        Http = new KlauHttpClient(baseUrl, httpClient, logger);

        Auth = new AuthClient(Http);
        Jobs = new JobClient(Http);
        Customers = new CustomerClient(Http);
        Dispatches = new DispatchClient(Http);
        Storefronts = new StorefrontClient(Http);
        Materials = new MaterialClient(Http);
        DumpTickets = new DumpTicketClient(Http);
        Orders = new OrderClient(Http);
        Proposals = new ProposalClient(Http);
        Divisions = new DivisionClient(Http);
        Webhooks = new WebhookClient(Http);

        Http.SetToken(apiKey);
    }

    /// <summary>
    /// Create a client from <see cref="KlauClientOptions"/>.
    /// The API key is resolved from <see cref="KlauClientOptions.ApiKey"/>
    /// or the <c>KLAU_API_KEY</c> environment variable.
    /// </summary>
    public static KlauClient Create(KlauClientOptions options, HttpClient? httpClient = null, ILogger? logger = null)
    {
        var apiKey = options.ResolveApiKey();
        var client = new KlauClient(apiKey, options.BaseUrl, httpClient, logger);
        client.Http.SetTimeout(TimeSpan.FromSeconds(options.TimeoutSeconds));
        return client;
    }

    /// <summary>
    /// Create a client using the <c>KLAU_API_KEY</c> environment variable.
    /// This is the recommended pattern for production deployments where
    /// credentials are injected via environment variables or secrets managers.
    /// </summary>
    /// <param name="httpClient">Optional HttpClient for custom configuration.</param>
    /// <param name="logger">Optional logger for retry/error diagnostics.</param>
    public static KlauClient CreateFromEnvironment(HttpClient? httpClient = null, ILogger? logger = null)
    {
        return Create(new KlauClientOptions(), httpClient, logger);
    }

    /// <summary>
    /// Validate that an API key has the expected format.
    /// Fails fast so integrators catch configuration errors at startup, not at first request.
    /// </summary>
    internal static void ValidateApiKey(string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));

        if (!apiKey.StartsWith(ApiKeyPrefix, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Invalid Klau API key format. Keys must start with '{ApiKeyPrefix}'. " +
                "Generate a key at Settings > Developer in your Klau dashboard.",
                nameof(apiKey));
        }
    }

    /// <summary>
    /// Set the default tenant context for all requests made through this client.
    /// Enterprise API keys (parent company) can operate on any child tenant.
    ///
    /// For concurrent multi-tenant use, prefer <see cref="ForTenant"/> which
    /// creates an isolated scope without mutating this client's state.
    /// </summary>
    /// <param name="tenantId">The company ID of the child tenant to operate on.</param>
    public void SetTenant(string tenantId) => Http.SetDefaultTenantId(tenantId);

    /// <summary>
    /// Clear the tenant context, reverting to the API key's own company.
    /// </summary>
    public void ClearTenant() => Http.SetDefaultTenantId(null);

    /// <summary>
    /// Create an isolated scope that targets a specific child tenant.
    /// All requests through the returned scope use per-request headers
    /// and do NOT mutate this client's state. Safe for concurrent use.
    ///
    /// Usage:
    ///   var div = klau.ForTenant("child-company-id");
    ///   var board = await div.Dispatches.GetBoardAsync("2026-03-13");
    /// </summary>
    public TenantScope ForTenant(string tenantId) => new(this, tenantId);

    public void Dispose() => Http.Dispose();
}

/// <summary>
/// An isolated scope that targets a specific child tenant.
/// Does NOT mutate the parent KlauClient. Safe for concurrent use across divisions.
///
/// Each sub-client returned by this scope passes the tenant ID as a per-request header,
/// so you can hold multiple TenantScope instances pointing at different divisions simultaneously.
/// </summary>
public sealed class TenantScope
{
    private readonly string _tenantId;

    public JobClient Jobs { get; }
    public CustomerClient Customers { get; }
    public DispatchClient Dispatches { get; }
    public StorefrontClient Storefronts { get; }
    public MaterialClient Materials { get; }
    public DumpTicketClient DumpTickets { get; }
    public OrderClient Orders { get; }
    public ProposalClient Proposals { get; }

    internal TenantScope(KlauClient client, string tenantId)
    {
        _tenantId = tenantId;
        var http = client.Http;

        Jobs = new JobClient(http, tenantId);
        Customers = new CustomerClient(http, tenantId);
        Dispatches = new DispatchClient(http, tenantId);
        Storefronts = new StorefrontClient(http, tenantId);
        Materials = new MaterialClient(http, tenantId);
        DumpTickets = new DumpTicketClient(http, tenantId);
        Orders = new OrderClient(http, tenantId);
        Proposals = new ProposalClient(http, tenantId);
    }
}
