using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Klau.Sdk.Tests.Helpers;

/// <summary>
/// A DelegatingHandler that intercepts HTTP requests and returns canned responses.
/// Standard .NET pattern for testing HttpClient consumers without Moq.
/// </summary>
public sealed class MockHttpHandler : DelegatingHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper) }
    };

    /// <summary>
    /// All requests that were sent through this handler, in order.
    /// </summary>
    public List<HttpRequestMessage> SentRequests { get; } = [];

    /// <summary>
    /// Captured request bodies (read and stored as strings) in order.
    /// </summary>
    public List<string?> SentBodies { get; } = [];

    /// <summary>
    /// Enqueue a success response. Body is wrapped in { "data": body } for the standard API envelope.
    /// </summary>
    public void EnqueueResponse(HttpStatusCode status, object? body = null, object? meta = null)
    {
        var envelope = new Dictionary<string, object?>();

        if ((int)status >= 200 && (int)status < 300)
        {
            envelope["data"] = body;
            if (meta is not null)
            {
                envelope["meta"] = meta;
            }
        }
        else
        {
            // Error response: body is expected to be an ApiErrorBody or null
            if (body is ApiErrorBody errorBody)
            {
                envelope["error"] = new { code = errorBody.Code, message = errorBody.Message };
            }
        }

        var json = JsonSerializer.Serialize(envelope, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _responses.Enqueue(new HttpResponseMessage(status) { Content = content });
    }

    /// <summary>
    /// Enqueue a raw response with a string body (not wrapped in the API envelope).
    /// Useful for testing non-JSON error handling.
    /// </summary>
    public void EnqueueRawResponse(HttpStatusCode status, string body, string contentType = "text/plain")
    {
        var content = new StringContent(body, Encoding.UTF8, contentType);
        _responses.Enqueue(new HttpResponseMessage(status) { Content = content });
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Capture the request body before it's consumed
        string? bodyStr = null;
        if (request.Content is not null)
        {
            bodyStr = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        SentRequests.Add(request);
        SentBodies.Add(bodyStr);

        if (_responses.Count == 0)
        {
            throw new InvalidOperationException(
                $"MockHttpHandler: No responses enqueued. Request was {request.Method} {request.RequestUri}");
        }

        return _responses.Dequeue();
    }
}

/// <summary>
/// Helper to construct error bodies for EnqueueResponse.
/// </summary>
public sealed class ApiErrorBody
{
    public string Code { get; }
    public string Message { get; }

    public ApiErrorBody(string code, string message)
    {
        Code = code;
        Message = message;
    }
}
