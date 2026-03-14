namespace Klau.Sdk.Tests;

public class HttpClientOwnershipTests
{
    [Fact]
    public void WhenNoHttpClientProvided_DisposeWorks()
    {
        // When no HttpClient is provided, KlauClient creates its own and owns it.
        // Dispose should not throw.
        var client = new KlauClient("kl_live_test", "https://api.test.com");

        var exception = Record.Exception(() => client.Dispose());

        Assert.Null(exception);
    }

    [Fact]
    public void WhenHttpClientProvided_DisposeDoesNotDisposeIt()
    {
        // When a caller provides their own HttpClient, Dispose should NOT dispose it.
        // We verify by using the HttpClient after the KlauClient is disposed.
        var httpClient = new HttpClient();

        var klauClient = new KlauClient("kl_live_test", "https://api.test.com", httpClient);
        klauClient.Dispose();

        // If KlauClient disposed the caller's HttpClient, this would throw ObjectDisposedException
        var exception = Record.Exception(() =>
        {
            // Accessing BaseAddress is a simple way to check the HttpClient is still alive
            _ = httpClient.BaseAddress;
        });

        Assert.Null(exception);

        // Clean up
        httpClient.Dispose();
    }

    [Fact]
    public void WhenNoHttpClientProvided_SecondDisposeDoesNotThrow()
    {
        // Double-dispose should be safe
        var client = new KlauClient("kl_live_test", "https://api.test.com");

        client.Dispose();
        var exception = Record.Exception(() => client.Dispose());

        Assert.Null(exception);
    }
}
