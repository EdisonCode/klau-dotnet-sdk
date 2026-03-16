using System.Runtime.CompilerServices;
using Klau.Sdk.Common;

namespace Klau.Sdk.DumpTickets;

public interface IDumpTicketClient
{
    Task<DumpTicket> CreateAsync(CreateDumpTicketRequest request, CancellationToken ct = default);
    Task<PagedResult<DumpTicket>> ListAsync(string? jobId = null, bool? isVerified = null, bool? settlementApplied = null, string? startDate = null, string? endDate = null, int page = 1, int pageSize = 100, CancellationToken ct = default);
    IAsyncEnumerable<DumpTicket> ListAllAsync(string? jobId = null, bool? isVerified = null, bool? settlementApplied = null, string? startDate = null, string? endDate = null, int pageSize = 100, CancellationToken ct = default);
    Task<DumpTicket> GetAsync(string id, CancellationToken ct = default);
    Task<DumpTicket> VerifyAsync(string id, VerifyDumpTicketRequest? request = null, CancellationToken ct = default);
    Task<DumpTicket> GetForJobAsync(string jobId, CancellationToken ct = default);
}

public sealed class DumpTicketClient : IDumpTicketClient
{
    private readonly KlauHttpClient _http;
    private readonly string? _tenantId;

    internal DumpTicketClient(KlauHttpClient http, string? tenantId = null)
    {
        _http = http;
        _tenantId = tenantId;
    }

    /// <summary>
    /// Create a dump ticket from manual entry.
    /// </summary>
    public async Task<DumpTicket> CreateAsync(CreateDumpTicketRequest request, CancellationToken ct = default)
    {
        return await _http.PostAsync<DumpTicket>("api/v1/dump-tickets", request, _tenantId, ct);
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

        var response = await _http.GetResponseAsync<List<DumpTicket>>(path, _tenantId, ct);
        return new PagedResult<DumpTicket>(response.Data, response.Meta);
    }

    /// <summary>
    /// Iterate all dump tickets matching the filters, automatically paging through results.
    /// </summary>
    public async IAsyncEnumerable<DumpTicket> ListAllAsync(
        string? jobId = null,
        bool? isVerified = null,
        bool? settlementApplied = null,
        string? startDate = null,
        string? endDate = null,
        int pageSize = 100,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        int page = 1;
        while (true)
        {
            var result = await ListAsync(jobId, isVerified, settlementApplied, startDate, endDate, page, pageSize, ct);
            foreach (var item in result.Items)
                yield return item;
            if (!result.HasMore) break;
            page++;
        }
    }

    /// <summary>
    /// Get a single dump ticket by ID.
    /// </summary>
    public async Task<DumpTicket> GetAsync(string id, CancellationToken ct = default)
    {
        return await _http.GetAsync<DumpTicket>($"api/v1/dump-tickets/{id}", _tenantId, ct);
    }

    /// <summary>
    /// Verify a dump ticket with optional corrections.
    /// </summary>
    public async Task<DumpTicket> VerifyAsync(string id, VerifyDumpTicketRequest? request = null, CancellationToken ct = default)
    {
        return await _http.PatchAsync<DumpTicket>($"api/v1/dump-tickets/{id}/verify", request ?? new(), _tenantId, ct);
    }

    /// <summary>
    /// Get the dump ticket associated with a specific job.
    /// </summary>
    public async Task<DumpTicket> GetForJobAsync(string jobId, CancellationToken ct = default)
    {
        return await _http.GetAsync<DumpTicket>($"api/v1/jobs/{jobId}/dump-ticket", _tenantId, ct);
    }
}
