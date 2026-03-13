using System.Text.Json.Serialization;
using Klau.Sdk.Common;

namespace Klau.Sdk.Authentication;

public sealed class AuthClient
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
            ct);

        _http.SetToken(result.Token);
        return result;
    }

    /// <summary>
    /// Get a guest token for the demo company.
    /// </summary>
    public async Task<string> DemoLoginAsync(CancellationToken ct = default)
    {
        var result = await _http.PostAsync<DemoLoginResult>("api/v1/demo/login", new { }, ct);
        _http.SetToken(result.Token);
        return result.Token;
    }

    /// <summary>
    /// Register a new account.
    /// </summary>
    public async Task<LoginResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var result = await _http.PostAsync<LoginResult>("api/v1/auth/register", request, ct);
        _http.SetToken(result.Token);
        return result;
    }

    /// <summary>
    /// Request a password reset email.
    /// </summary>
    public async Task ForgotPasswordAsync(string email, CancellationToken ct = default)
    {
        await _http.PostAsync("api/v1/auth/forgot-password", new { email }, ct);
    }

    /// <summary>
    /// Reset password using a token from the reset email.
    /// </summary>
    public async Task ResetPasswordAsync(string token, string password, CancellationToken ct = default)
    {
        await _http.PostAsync("api/v1/auth/reset-password", new { token, password }, ct);
    }

    /// <summary>
    /// Change password for the currently authenticated user.
    /// </summary>
    public async Task ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct = default)
    {
        await _http.PostAsync("api/v1/auth/change-password", new { currentPassword, newPassword }, ct);
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

public sealed class LoginResult
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("user")]
    public UserInfo User { get; set; } = default!;

    [JsonPropertyName("company")]
    public CompanyInfo Company { get; set; } = default!;
}

public sealed class DemoLoginResult
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}

public sealed class UserInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
}

public sealed class CompanyInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = string.Empty;
}

public sealed class RegisterRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("companyName")]
    public string CompanyName { get; set; } = string.Empty;

    [JsonPropertyName("subscriptionTier")]
    public string? SubscriptionTier { get; set; }
}
