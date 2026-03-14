using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Klau.Sdk.Webhooks;

namespace Klau.Sdk.Tests;

public class WebhookValidatorTests
{
    private const string TestSecret = "whsec_test_secret_123";

    private static string MakeSignature(string secret, long timestamp, string body)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var message = Encoding.UTF8.GetBytes($"{timestamp}.{body}");
        var hash = HMACSHA256.HashData(key, message);
        return Convert.ToHexStringLower(hash);
    }

    private static (string header, string body) CreateSignedEvent(
        string secret, string eventType = "job.created", long? overrideTimestamp = null)
    {
        var body = JsonSerializer.Serialize(new
        {
            id = "evt_test_123",
            type = eventType,
            companyId = "comp_1",
            timestamp = "2026-03-14T10:00:00Z",
            data = new { jobId = "j-1", jobType = "DELIVERY" }
        });

        var ts = overrideTimestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var sig = MakeSignature(secret, ts, body);
        var header = $"t={ts},v1={sig}";

        return (header, body);
    }

    [Fact]
    public void Validate_ValidSignature_ReturnsTrue()
    {
        var validator = new KlauWebhookValidator(TestSecret);
        var (header, body) = CreateSignedEvent(TestSecret);

        Assert.True(validator.Validate(header, body));
    }

    [Fact]
    public void Validate_WrongSecret_ReturnsFalse()
    {
        var validator = new KlauWebhookValidator(TestSecret);
        var (header, body) = CreateSignedEvent("whsec_wrong_secret");

        Assert.False(validator.Validate(header, body));
    }

    [Fact]
    public void Validate_TamperedBody_ReturnsFalse()
    {
        var validator = new KlauWebhookValidator(TestSecret);
        var (header, _) = CreateSignedEvent(TestSecret);

        Assert.False(validator.Validate(header, "{\"tampered\":true}"));
    }

    [Fact]
    public void Validate_ExpiredTimestamp_ReturnsFalse()
    {
        var validator = new KlauWebhookValidator(TestSecret);
        var tenMinutesAgo = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeMilliseconds();
        var (header, body) = CreateSignedEvent(TestSecret, overrideTimestamp: tenMinutesAgo);

        Assert.False(validator.Validate(header, body));
    }

    [Fact]
    public void Validate_ExpiredTimestamp_PassesWithLargeTolerance()
    {
        var validator = new KlauWebhookValidator(TestSecret);
        var tenMinutesAgo = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeMilliseconds();
        var (header, body) = CreateSignedEvent(TestSecret, overrideTimestamp: tenMinutesAgo);

        Assert.True(validator.Validate(header, body, tolerance: TimeSpan.FromMinutes(15)));
    }

    [Fact]
    public void Validate_EmptyHeader_ReturnsFalse()
    {
        var validator = new KlauWebhookValidator(TestSecret);
        Assert.False(validator.Validate("", "{}"));
    }

    [Fact]
    public void Validate_MalformedHeader_ReturnsFalse()
    {
        var validator = new KlauWebhookValidator(TestSecret);
        Assert.False(validator.Validate("not-a-valid-header", "{}"));
    }

    [Fact]
    public void ValidateAndParse_ValidSignature_ReturnsEvent()
    {
        var validator = new KlauWebhookValidator(TestSecret);
        var (header, body) = CreateSignedEvent(TestSecret, "job.assigned");

        var evt = validator.ValidateAndParse(header, body);

        Assert.Equal("evt_test_123", evt.Id);
        Assert.Equal("job.assigned", evt.Type);
        Assert.Equal("comp_1", evt.CompanyId);
    }

    [Fact]
    public void ValidateAndParse_InvalidSignature_Throws()
    {
        var validator = new KlauWebhookValidator(TestSecret);
        var (header, body) = CreateSignedEvent("whsec_wrong");

        Assert.Throws<KlauWebhookException>(() => validator.ValidateAndParse(header, body));
    }

    [Fact]
    public void ValidateAndParse_Typed_DeserializesData()
    {
        var validator = new KlauWebhookValidator(TestSecret);
        var (header, body) = CreateSignedEvent(TestSecret, "job.created");

        var evt = validator.ValidateAndParse<JobCreatedEvent>(header, body);

        Assert.Equal("job.created", evt.Type);
        Assert.Equal("j-1", evt.Data.JobId);
        Assert.Equal("DELIVERY", evt.Data.JobType);
    }

    [Fact]
    public void Constructor_NullSecret_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new KlauWebhookValidator(null!));
    }

    [Fact]
    public void Constructor_EmptySecret_Throws()
    {
        Assert.Throws<ArgumentException>(() => new KlauWebhookValidator(""));
    }
}
