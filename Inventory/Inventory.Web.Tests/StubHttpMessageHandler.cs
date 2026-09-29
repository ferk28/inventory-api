using System.Net;
using System.Text;
namespace Inventory.Web.Tests;
// Answers every request with a fixed response and remembers what was sent, so a test can
// assert both on how the client reads the API and on what it asked for.
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string? _body;
    private readonly string _mediaType;
    public StubHttpMessageHandler(HttpStatusCode statusCode, string? body = null, string mediaType = "application/json")
    {
        _statusCode = statusCode;
        _body = body;
        _mediaType = mediaType;
    }
    public List<HttpRequestMessage> Requests { get; } = [];
    public List<string?> RequestBodies { get; } = [];
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        RequestBodies.Add(request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken));
        HttpResponseMessage response = new(_statusCode);
        if (_body is not null)
        {
            response.Content = new StringContent(_body, Encoding.UTF8, _mediaType);
        }

        return response;
    }
}
