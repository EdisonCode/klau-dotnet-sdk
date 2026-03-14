using Klau.Sdk.Authentication;
using Klau.Sdk.Common;
using Klau.Sdk.Customers;
using Klau.Sdk.Dispatches;
using Klau.Sdk.DumpTickets;
using Klau.Sdk.Jobs;
using Klau.Sdk.Materials;
using Klau.Sdk.Orders;
using Klau.Sdk.Divisions;
using Klau.Sdk.Storefronts;
using Microsoft.Extensions.Logging;

namespace Klau.Sdk;

/// <summary>
/// Entry point for the Klau .NET SDK.
///
/// Usage:
///   var klau = new KlauClient("kl_live_your_api_key_here");
///   var board = await klau.Dispatches.GetBoardAsync("2026-03-13");
///
/// Enterprise (division-scoped):
///   using var div = klau.ForTenant("child-company-id");
///   var board = await div.Dispatches.GetBoardAsync("2026-03-13");
/// </summary>
public sealed class KlauClient : IDisposable
{
    internal readonly KlauHttpClient Http;

    public AuthClient Auth { get; }
    public JobClient Jobs { get; }
    public CustomerClient Customers { get; }
    public DispatchClient Dispatches { get; }
    public StorefrontClient Storefronts { get; }
    public MaterialClient Materials { get; }
    public DumpTicketClient DumpTickets { get; }
    public OrderClient Orders { get; }
    public DivisionClient Divisions { get; }

    /// <summary>
    /// Create a new Klau API client authenticated with an API key.
    /// Generate an API key in Settings > Developer in your Klau dashboard.
    /// </summary>
    /// <param name="apiKey">Your API key (starts with kl_live_).</param>
    /// <param name="httpClient">Optional HttpClient for custom configuration (e.g. proxies, timeouts). The SDK will NOT dispose a caller-provided HttpClient.</param>
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
        Http = new KlauHttpClient(baseUrl, httpClient, logger);

        Auth = new AuthClient(Http);
        Jobs = new JobClient(Http);
        Customers = new CustomerClient(Http);
        Dispatches = new DispatchClient(Http);
        Storefronts = new StorefrontClient(Http);
        Materials = new MaterialClient(Http);
        DumpTickets = new DumpTicketClient(Http);
        Orders = new OrderClient(Http);
        Divisions = new DivisionClient(Http);

        Http.SetToken(apiKey);
    }

    /// <summary>
    /// Set the default tenant context for all requests made through this client.
    /// Enterprise API keys (parent company) can operate on any child tenant.
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

    internal TenantScope(KlauClient client, string tenantId)
    {
        _tenantId = tenantId;
        var http = client.Http;

        // Each sub-client gets the tenant override passed through to per-request headers
        Jobs = new JobClient(http, tenantId);
        Customers = new CustomerClient(http, tenantId);
        Dispatches = new DispatchClient(http, tenantId);
        Storefronts = new StorefrontClient(http, tenantId);
        Materials = new MaterialClient(http, tenantId);
        DumpTickets = new DumpTicketClient(http, tenantId);
        Orders = new OrderClient(http, tenantId);
    }
}
