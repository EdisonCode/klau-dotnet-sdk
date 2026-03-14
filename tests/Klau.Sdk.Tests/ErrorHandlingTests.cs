using System.Net;
using Klau.Sdk.Common;
using Klau.Sdk.Tests.Helpers;

namespace Klau.Sdk.Tests;

public class ErrorHandlingTests
{
    private static (KlauClient client, MockHttpHandler handler) CreateClient()
    {
        var handler = new MockHttpHandler();
        var httpClient = new HttpClient(handler);
        var client = new KlauClient("kl_live_test", "https://api.test.com", httpClient);
        return (client, handler);
    }

    [Fact]
    public async Task BadRequest400_MapsToKlauApiException()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.BadRequest,
            new ApiErrorBody("VALIDATION_ERROR", "containerSize must be one of [10, 15, 20, 30, 40]"));

        var ex = await Assert.ThrowsAsync<KlauApiException>(
            () => client.Jobs.GetAsync("j-1"));

        Assert.Equal("VALIDATION_ERROR", ex.ErrorCode);
        Assert.Equal("containerSize must be one of [10, 15, 20, 30, 40]", ex.Message);
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task Unauthorized401_MapsToKlauApiException()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.Unauthorized,
            new ApiErrorBody("UNAUTHORIZED", "Invalid or expired API key"));

        var ex = await Assert.ThrowsAsync<KlauApiException>(
            () => client.Jobs.ListAsync());

        Assert.Equal("UNAUTHORIZED", ex.ErrorCode);
        Assert.Equal("Invalid or expired API key", ex.Message);
        Assert.Equal(401, ex.StatusCode);
    }

    [Fact]
    public async Task NotFound404_MapsToKlauApiException()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.NotFound,
            new ApiErrorBody("NOT_FOUND", "Job not found"));

        var ex = await Assert.ThrowsAsync<KlauApiException>(
            () => client.Jobs.GetAsync("j-nonexistent"));

        Assert.Equal("NOT_FOUND", ex.ErrorCode);
        Assert.Equal("Job not found", ex.Message);
        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task NonJsonErrorBody_HandledGracefully()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueRawResponse(HttpStatusCode.InternalServerError,
            "<html>502 Bad Gateway</html>", "text/html");

        var ex = await Assert.ThrowsAsync<KlauApiException>(
            () => client.Jobs.GetAsync("j-1"));

        Assert.Equal("HTTP_ERROR", ex.ErrorCode);
        Assert.Equal(500, ex.StatusCode);
        Assert.Contains("500", ex.Message);
    }

    [Fact]
    public async Task ErrorCode_IsAccessibleOnException()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.Forbidden,
            new ApiErrorBody("TENANT_MISMATCH", "You do not have access to this tenant"));

        var ex = await Assert.ThrowsAsync<KlauApiException>(
            () => client.Customers.GetAsync("cust-1"));

        Assert.Equal("TENANT_MISMATCH", ex.ErrorCode);
        Assert.Equal(403, ex.StatusCode);
        Assert.IsType<KlauApiException>(ex);
        // Verify it's also a standard Exception
        Assert.IsAssignableFrom<Exception>(ex);
    }

    [Fact]
    public async Task VoidEndpoint_ThrowsOnError()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.BadRequest,
            new ApiErrorBody("INVALID_STATE", "Cannot cancel a completed job"));

        var ex = await Assert.ThrowsAsync<KlauApiException>(
            () => client.Jobs.CancelAsync("j-1"));

        Assert.Equal("INVALID_STATE", ex.ErrorCode);
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task DeleteEndpoint_ThrowsOnError()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueResponse(HttpStatusCode.NotFound,
            new ApiErrorBody("NOT_FOUND", "Job not found"));

        var ex = await Assert.ThrowsAsync<KlauApiException>(
            () => client.Jobs.DeleteAsync("j-gone"));

        Assert.Equal("NOT_FOUND", ex.ErrorCode);
        Assert.Equal(404, ex.StatusCode);
    }
}
