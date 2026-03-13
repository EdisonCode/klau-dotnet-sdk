using Klau.Sdk.Common;

namespace Klau.Sdk.DumpTickets;

public sealed class DumpTicketClient
{
    private readonly KlauHttpClient _http;

    internal DumpTicketClient(KlauHttpClient http) => _http = http;

    /// <summary>
    /// Create a dump ticket from manual entry.
    /// </summary>
    public async Task<DumpTicket> CreateAsync(CreateDumpTicketRequest request, CancellationToken ct = default)
    {
        return await _http.PostAsync<DumpTicket>("api/v1/dump-tickets", request, ct);
    }

    /// <summary>
    /// List dump tickets with optional filters.
    /// </summary>
    public async Task<PagedResult<DumpTicket>> ListAsync(
        string? jobId = null,
        bool? isVerified = null,
        bool? settlementApplied = null,
        string? startDate = null,
        string? endDate = null,
        int page = 1,
        int pageSize = 100,
        CancellationToken ct = default)
    {
        var path = QueryBuilder.Build("api/v1/dump-tickets",
            ("jobId", jobId),
            ("isVerified", isVerified),
            ("settlementApplied", settlementApplied),
            ("startDate", startDate),
            ("endDate", endDate),
            ("page", page),
            ("pageSize", pageSize));

        var response = await _http.GetResponseAsync<List<DumpTicket>>(path, ct);
        return new PagedResult<DumpTicket>(response.Data, response.Meta);
    }

    /// <summary>
    /// Get a single dump ticket by ID.
    /// </summary>
    public async Task<DumpTicket> GetAsync(string id, CancellationToken ct = default)
    {
        return await _http.GetAsync<DumpTicket>($"api/v1/dump-tickets/{id}", ct);
    }

    /// <summary>
    /// Verify a dump ticket with optional corrections.
    /// </summary>
    public async Task<DumpTicket> VerifyAsync(string id, VerifyDumpTicketRequest? request = null, CancellationToken ct = default)
    {
        return await _http.PatchAsync<DumpTicket>($"api/v1/dump-tickets/{id}/verify", request ?? new(), ct);
    }

    /// <summary>
    /// Get the dump ticket associated with a specific job.
    /// </summary>
    public async Task<DumpTicket> GetForJobAsync(string jobId, CancellationToken ct = default)
    {
        return await _http.GetAsync<DumpTicket>($"api/v1/jobs/{jobId}/dump-ticket", ct);
    }
}
