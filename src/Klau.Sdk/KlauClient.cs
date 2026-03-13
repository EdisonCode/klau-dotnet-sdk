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

namespace Klau.Sdk;

/// <summary>
/// Entry point for the Klau .NET SDK.
///
/// Usage:
///   var klau = new KlauClient("kl_live_your_api_key_here");
///   var board = await klau.Dispatches.GetBoardAsync("2026-03-13");
///
/// Sub-tenant (enterprise):
///   var klau = new KlauClient("kl_live_your_api_key_here");
///   klau.SetTenant("child-company-id");
///   var board = await klau.Dispatches.GetBoardAsync("2026-03-13");
/// </summary>
public sealed class KlauClient : IDisposable
{
    private readonly KlauHttpClient _http;

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
    /// <param name="httpClient">Optional HttpClient for custom configuration (e.g. proxies, timeouts).</param>
    public KlauClient(string apiKey, HttpClient? httpClient = null)
        : this(apiKey, "https://api.getklau.com", httpClient)
    {
    }

    /// <summary>
    /// Create a new Klau API client with a custom base URL.
    /// </summary>
    /// <param name="apiKey">Your API key (starts with kl_live_).</param>
    /// <param name="baseUrl">The base URL of the Klau API.</param>
    /// <param name="httpClient">Optional HttpClient for custom configuration (e.g. proxies, timeouts).</param>
    public KlauClient(string apiKey, string baseUrl, HttpClient? httpClient = null)
    {
        _http = new KlauHttpClient(baseUrl, httpClient);

        Auth = new AuthClient(_http);
        Jobs = new JobClient(_http);
        Customers = new CustomerClient(_http);
        Dispatches = new DispatchClient(_http);
        Storefronts = new StorefrontClient(_http);
        Materials = new MaterialClient(_http);
        DumpTickets = new DumpTicketClient(_http);
        Orders = new OrderClient(_http);
        Divisions = new DivisionClient(_http);

        _http.SetToken(apiKey);
    }

    /// <summary>
    /// Set the tenant context for sub-tenant operations.
    /// Enterprise API keys (parent company) can operate on any child tenant
    /// by setting this before making requests.
    /// </summary>
    /// <param name="tenantId">The company ID of the child tenant to operate on.</param>
    public void SetTenant(string tenantId) => _http.SetTenantId(tenantId);

    /// <summary>
    /// Clear the tenant context, reverting to the API key's own company.
    /// </summary>
    public void ClearTenant() => _http.SetTenantId(null);

    /// <summary>
    /// Create a scoped client that operates on a specific child tenant.
    /// All requests made through the returned client target the specified tenant.
    /// The returned object is a lightweight handle (not a new HTTP connection).
    /// Dispose the parent KlauClient when done, not the tenant scope.
    /// </summary>
    public TenantScope ForTenant(string tenantId) => new(this, tenantId);

    public void Dispose() => _http.Dispose();
}

/// <summary>
/// A scoped handle that sets/clears the tenant context around operations.
/// Use via KlauClient.ForTenant("child-company-id").
/// </summary>
public sealed class TenantScope
{
    private readonly KlauClient _client;
    private readonly string _tenantId;

    internal TenantScope(KlauClient client, string tenantId)
    {
        _client = client;
        _tenantId = tenantId;
    }

    public JobClient Jobs => WithTenant().Jobs;
    public CustomerClient Customers => WithTenant().Customers;
    public DispatchClient Dispatches => WithTenant().Dispatches;
    public StorefrontClient Storefronts => WithTenant().Storefronts;
    public MaterialClient Materials => WithTenant().Materials;
    public DumpTicketClient DumpTickets => WithTenant().DumpTickets;
    public OrderClient Orders => WithTenant().Orders;

    private KlauClient WithTenant()
    {
        _client.SetTenant(_tenantId);
        return _client;
    }
}
