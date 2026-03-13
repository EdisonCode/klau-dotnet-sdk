using Klau.Sdk.Common;

namespace Klau.Sdk.Jobs;

public sealed class JobClient
{
    private readonly KlauHttpClient _http;

    internal JobClient(KlauHttpClient http) => _http = http;

    /// <summary>
    /// List jobs with optional filters.
    /// </summary>
    public async Task<PagedResult<Job>> ListAsync(
        string? date = null,
        JobStatus? status = null,
        string? driverId = null,
        int page = 1,
        int pageSize = 100,
        CancellationToken ct = default)
    {
        var path = QueryBuilder.Build("api/v1/jobs",
            ("date", date),
            ("status", status),
            ("driverId", driverId),
            ("page", page),
            ("pageSize", pageSize));

        var response = await _http.GetResponseAsync<List<Job>>(path, ct);
        return new PagedResult<Job>(response.Data, response.Meta);
    }

    /// <summary>
    /// Get a single job by ID.
    /// </summary>
    public async Task<Job> GetAsync(string id, CancellationToken ct = default)
    {
        return await _http.GetAsync<Job>($"api/v1/jobs/{id}", ct);
    }

    /// <summary>
    /// Create a new job.
    /// </summary>
    public async Task<Job> CreateAsync(CreateJobRequest request, CancellationToken ct = default)
    {
        return await _http.PostAsync<Job>("api/v1/jobs", request, ct);
    }

    /// <summary>
    /// Update an existing job.
    /// </summary>
    public async Task<Job> UpdateAsync(string id, UpdateJobRequest request, CancellationToken ct = default)
    {
        return await _http.PatchAsync<Job>($"api/v1/jobs/{id}", request, ct);
    }

    /// <summary>
    /// Assign a job to a driver and truck.
    /// </summary>
    public async Task<Job> AssignAsync(string id, AssignJobRequest request, CancellationToken ct = default)
    {
        return await _http.PostAsync<Job>($"api/v1/jobs/{id}/assign", request, ct);
    }

    /// <summary>
    /// Unassign a job from its current driver.
    /// </summary>
    public async Task<Job> UnassignAsync(string id, CancellationToken ct = default)
    {
        return await _http.PostAsync<Job>($"api/v1/jobs/{id}/unassign", ct);
    }

    /// <summary>
    /// Cancel a job.
    /// </summary>
    public async Task CancelAsync(string id, CancellationToken ct = default)
    {
        await _http.PostAsync($"api/v1/jobs/{id}/cancel", ct: ct);
    }

    /// <summary>
    /// Start a job (ASSIGNED -> IN_PROGRESS).
    /// </summary>
    public async Task<Job> StartAsync(string id, CancellationToken ct = default)
    {
        return await _http.PostAsync<Job>($"api/v1/jobs/{id}/start", ct: ct);
    }

    /// <summary>
    /// Complete a job (IN_PROGRESS -> COMPLETED).
    /// </summary>
    public async Task<Job> CompleteAsync(string id, CancellationToken ct = default)
    {
        return await _http.PostAsync<Job>($"api/v1/jobs/{id}/complete", ct: ct);
    }

    /// <summary>
    /// Delete a job (only UNASSIGNED, ASSIGNED, or CANCELLED).
    /// </summary>
    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await _http.DeleteAsync($"api/v1/jobs/{id}", ct);
    }
}
