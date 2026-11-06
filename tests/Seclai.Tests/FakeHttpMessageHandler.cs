using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Seclai.Tests;

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = (req, _) => Task.FromResult(handler(req));
    }

    /// <summary>A handler that can await, and observe the request's cancellation token.</summary>
    public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _handler(request, cancellationToken);
    }
}
