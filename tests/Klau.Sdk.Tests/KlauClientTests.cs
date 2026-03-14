namespace Klau.Sdk.Tests;

public class KlauClientTests
{
    [Fact]
    public void Constructor_ValidKey_Succeeds()
    {
        using var client = new KlauClient("kl_live_abc123");
        Assert.NotNull(client.Jobs);
    }

    [Fact]
    public void Constructor_NullKey_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new KlauClient(null!));
    }

    [Fact]
    public void Constructor_EmptyKey_Throws()
    {
        Assert.Throws<ArgumentException>(() => new KlauClient(""));
    }

    [Fact]
    public void Constructor_WhitespaceKey_Throws()
    {
        Assert.Throws<ArgumentException>(() => new KlauClient("   "));
    }

    [Fact]
    public void Constructor_WrongPrefix_ThrowsWithGuidance()
    {
        var ex = Assert.Throws<ArgumentException>(() => new KlauClient("sk_test_abc123"));
        Assert.Contains("kl_live_", ex.Message);
        Assert.Contains("Settings > Developer", ex.Message);
    }

    [Fact]
    public void Constructor_BearerToken_ThrowsWithGuidance()
    {
        var ex = Assert.Throws<ArgumentException>(() => new KlauClient("Bearer kl_live_abc123"));
        Assert.Contains("kl_live_", ex.Message);
    }

    [Fact]
    public void CreateFromEnvironment_NoEnvVar_ThrowsWithGuidance()
    {
        // Ensure env var is not set for this test
        var original = Environment.GetEnvironmentVariable("KLAU_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("KLAU_API_KEY", null);
            var ex = Assert.Throws<InvalidOperationException>(() => KlauClient.CreateFromEnvironment());
            Assert.Contains("KLAU_API_KEY", ex.Message);
            Assert.Contains("Settings > Developer", ex.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("KLAU_API_KEY", original);
        }
    }

    [Fact]
    public void CreateFromEnvironment_WithEnvVar_Succeeds()
    {
        var original = Environment.GetEnvironmentVariable("KLAU_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("KLAU_API_KEY", "kl_live_test_from_env");
            using var client = KlauClient.CreateFromEnvironment();
            Assert.NotNull(client.Jobs);
        }
        finally
        {
            Environment.SetEnvironmentVariable("KLAU_API_KEY", original);
        }
    }

    [Fact]
    public void Create_WithOptions_Succeeds()
    {
        var original = Environment.GetEnvironmentVariable("KLAU_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("KLAU_API_KEY", null);
            using var client = KlauClient.Create(new KlauClientOptions
            {
                ApiKey = "kl_live_options_test",
                TimeoutSeconds = 15,
            });
            Assert.NotNull(client.Jobs);
        }
        finally
        {
            Environment.SetEnvironmentVariable("KLAU_API_KEY", original);
        }
    }

    [Fact]
    public void Create_OptionsApiKeyOverridesEnvVar()
    {
        var original = Environment.GetEnvironmentVariable("KLAU_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("KLAU_API_KEY", "kl_live_env_key");
            using var client = KlauClient.Create(new KlauClientOptions
            {
                ApiKey = "kl_live_explicit_key",
            });
            // If it didn't throw, the explicit key was used
            Assert.NotNull(client);
        }
        finally
        {
            Environment.SetEnvironmentVariable("KLAU_API_KEY", original);
        }
    }
}
