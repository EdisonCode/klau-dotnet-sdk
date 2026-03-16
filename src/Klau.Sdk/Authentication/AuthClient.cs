using System.Text.Json.Serialization;
using Klau.Sdk.Common;

namespace Klau.Sdk.Authentication;

public interface IAuthClient
{
    Task<LoginResult> LoginAsync(string email, string password, CancellationToken ct = default);
    Task<string> DemoLoginAsync(CancellationToken ct = default);
    Task<LoginResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task ForgotPasswordAsync(string email, CancellationToken ct = default);
    Task ResetPasswordAsync(string token, string password, CancellationToken ct = default);
    Task ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct = default);
    void SetToken(string token);
    void ClearToken();
}

public sealed class AuthClient : IAuthClient
{
    private readonly KlauHttpClient _http;

    internal AuthClient(KlauHttpClient http) => _http = http;

    /// <summary>
    /// Log in with email and password. Stores the JWT token for subsequent requests.
    /// </summary>
    public async Task<LoginResult> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var result = await _http.PostAsync<LoginResult>(
            "api/v1/auth/login",
            new { email, password },
            tenantOverride: null,
            ct);

        _http.SetToken(result.Token);
        return result;
    }

    /// <summary>
    /// Get a guest token for the demo company.
    /// </summary>
    public async Task<string> DemoLoginAsync(CancellationToken ct = default)
    {
        var result = await _http.PostAsync<DemoLoginResult>("api/v1/demo/login", new { }, tenantOverride: null, ct);
        _http.SetToken(result.Token);
        return result.Token;
    }

    /// <summary>
    /// Register a new account.
    /// </summary>
    public async Task<LoginResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var result = await _http.PostAsync<LoginResult>("api/v1/auth/register", request, tenantOverride: null, ct);
        _http.SetToken(result.Token);
        return result;
    }

    /// <summary>
    /// Request a password reset email.
    /// </summary>
    public async Task ForgotPasswordAsync(string email, CancellationToken ct = default)
    {
        await _http.PostAsync("api/v1/auth/forgot-password", new { email }, tenantOverride: null, ct);
    }

    /// <summary>
    /// Reset password using a token from the reset email.
    /// </summary>
    public async Task ResetPasswordAsync(string token, string password, CancellationToken ct = default)
    {
        await _http.PostAsync("api/v1/auth/reset-password", new { token, password }, tenantOverride: null, ct);
    }

    /// <summary>
    /// Change password for the currently authenticated user.
    /// </summary>
    public async Task ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct = default)
    {
        await _http.PostAsync("api/v1/auth/change-password", new { currentPassword, newPassword }, tenantOverride: null, ct);
    }

    /// <summary>
    /// Set a pre-existing JWT token (e.g. from a prior session).
    /// </summary>
    public void SetToken(string token) => _http.SetToken(token);

    /// <summary>
    /// Clear the current authentication token.
    /// </summary>
    public void ClearToken() => _http.ClearToken();
}

public sealed record LoginResult
{
    [JsonPropertyName("token")]
    public string Token { get; init; } = string.Empty;

    [JsonPropertyName("user")]
    public UserInfo User { get; init; } = default!;

    [JsonPropertyName("company")]
    public CompanyInfo Company { get; init; } = default!;
}

public sealed record DemoLoginResult
{
    [JsonPropertyName("token")]
    public string Token { get; init; } = string.Empty;
}

public sealed record UserInfo
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; init; } = string.Empty;
}

public sealed record CompanyInfo
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("timezone")]
    public string Timezone { get; init; } = string.Empty;
}

public sealed record RegisterRequest
{
    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("password")]
    public required string Password { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("companyName")]
    public required string CompanyName { get; init; }

    [JsonPropertyName("subscriptionTier")]
    public string? SubscriptionTier { get; init; }
}
