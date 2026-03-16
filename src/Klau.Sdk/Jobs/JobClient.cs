using Klau.Sdk.Common;

namespace Klau.Sdk.Jobs;

public sealed class JobClient
{
    private readonly KlauHttpClient _http;
    private readonly string? _tenantId;

    internal JobClient(KlauHttpClient http, string? tenantId = null)
    {
        _http = http;
        _tenantId = tenantId;
    }

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

        var response = await _http.GetListAsync<Job>(path, "jobs", _tenantId, ct);
        return new PagedResult<Job>(response.Items, response.Total, response.Page, response.PageSize, response.HasMore);
    }

    /// <summary>
    /// Get a single job by ID.
    /// </summary>
    public async Task<Job> GetAsync(string id, CancellationToken ct = default)
    {
        return await _http.GetAsync<Job>($"api/v1/jobs/{id}", _tenantId, ct);
    }

    /// <summary>
    /// Create a new job. Returns the created job ID.
    /// Use <see cref="GetAsync"/> to fetch the full job after creation.
    /// </summary>
    public async Task<string> CreateAsync(CreateJobRequest request, CancellationToken ct = default)
    {
        return await _http.PostCreateAsync("api/v1/jobs", request, "jobId", _tenantId, ct);
    }

    /// <summary>
    /// Create multiple jobs in a single API call.
    /// Returns a batch result with the created job IDs and any per-record errors.
    /// </summary>
    public async Task<BatchCreateResult> CreateBatchAsync(
        IReadOnlyList<CreateJobRequest> jobs,
        CancellationToken ct = default)
    {
        return await _http.PostAsync<BatchCreateResult>(
            "api/v1/jobs/batch", new { jobs }, _tenantId, ct);
    }

    /// <summary>
    /// Update an existing job. Returns the updated job.
    /// </summary>
    public async Task<Job> UpdateAsync(string id, UpdateJobRequest request, CancellationToken ct = default)
    {
        return await _http.PatchAsync<Job>($"api/v1/jobs/{id}", request, _tenantId, ct);
    }

    /// <summary>
    /// Assign a job to a driver and truck. Returns assignment result.
    /// </summary>
    public async Task<AssignJobResult> AssignAsync(string id, AssignJobRequest request, CancellationToken ct = default)
    {
        return await _http.PostAsync<AssignJobResult>($"api/v1/jobs/{id}/assign", request, _tenantId, ct);
    }

    /// <summary>
    /// Unassign a job from its current driver. Returns unassignment result.
    /// </summary>
    public async Task<UnassignJobResult> UnassignAsync(string id, CancellationToken ct = default)
    {
        return await _http.PostAsync<UnassignJobResult>($"api/v1/jobs/{id}/unassign", null, _tenantId, ct);
    }

    /// <summary>
    /// Cancel a job.
    /// </summary>
    public async Task CancelAsync(string id, CancellationToken ct = default)
    {
        await _http.PostAsync($"api/v1/jobs/{id}/cancel", null, _tenantId, ct);
    }

    /// <summary>
    /// Start a job (ASSIGNED -> IN_PROGRESS).
    /// </summary>
    public async Task StartAsync(string id, CancellationToken ct = default)
    {
        await _http.PostAsync($"api/v1/jobs/{id}/start", null, _tenantId, ct);
    }

    /// <summary>
    /// Complete a job (IN_PROGRESS -> COMPLETED).
    /// </summary>
    public async Task CompleteAsync(string id, CancellationToken ct = default)
    {
        await _http.PostAsync($"api/v1/jobs/{id}/complete", null, _tenantId, ct);
    }

    /// <summary>
    /// Delete a job (only UNASSIGNED, ASSIGNED, or CANCELLED).
    /// </summary>
    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await _http.DeleteAsync($"api/v1/jobs/{id}", _tenantId, ct);
    }
}
