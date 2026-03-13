using Klau.Sdk.Authentication;
using Klau.Sdk.Common;
using Klau.Sdk.Customers;
using Klau.Sdk.Dispatches;
using Klau.Sdk.DumpTickets;
using Klau.Sdk.Jobs;
using Klau.Sdk.Materials;
using Klau.Sdk.Orders;
using Klau.Sdk.Storefronts;

namespace Klau.Sdk;

/// <summary>
/// Entry point for the Klau .NET SDK.
///
/// Usage:
///   var klau = new KlauClient("https://api.getklau.com");
///   await klau.Auth.LoginAsync("user@example.com", "password");
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

    /// <summary>
    /// Create a new Klau API client.
    /// </summary>
    /// <param name="baseUrl">The base URL of your Klau API instance.</param>
    /// <param name="httpClient">Optional HttpClient for custom configuration (e.g. proxies, timeouts).</param>
    public KlauClient(string baseUrl, HttpClient? httpClient = null)
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
    }

    /// <summary>
    /// Create a client with a pre-existing JWT token.
    /// </summary>
    public KlauClient(string baseUrl, string token, HttpClient? httpClient = null)
        : this(baseUrl, httpClient)
    {
        Auth.SetToken(token);
    }

    public void Dispose() => _http.Dispose();
}
