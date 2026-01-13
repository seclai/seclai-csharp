using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Seclai.Exceptions;
using Seclai.Models;
using Xunit;

namespace Seclai.Tests;

public sealed class SeclaiClientTests
{
    [Fact]
    public void Constructor_UsesEnvApiKey()
    {
        Environment.SetEnvironmentVariable("SECLAI_API_KEY", "k");
        try
        {
            var client = new SeclaiClient(new SeclaiClientOptions { HttpClient = new HttpClient(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))) });
            Assert.NotNull(client);
        }
        finally
        {
            Environment.SetEnvironmentVariable("SECLAI_API_KEY", null);
        }
    }

    [Fact]
    public async System.Threading.Tasks.Task ListSources_MatchesTrailingSlashPathAndSetsAuth()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal("/sources/", req.RequestUri!.AbsolutePath);
            Assert.True(req.Headers.TryGetValues("x-api-key", out var values));
            Assert.Contains("k", values);
            var body = "{\"data\":[],\"pagination\":{\"has_next\":false,\"has_prev\":false,\"limit\":20,\"page\":1,\"pages\":1,\"total\":0}}";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        });

        using var http = new HttpClient(handler);
        var client = new SeclaiClient(new SeclaiClientOptions { ApiKey = "k", BaseUri = new Uri("https://example.invalid"), HttpClient = http });
        var res = await client.ListSourcesAsync();
        Assert.NotNull(res);
        Assert.NotNull(res.Pagination);
    }

    [Fact]
    public async System.Threading.Tasks.Task ValidationError_ThrowsApiValidationException()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            var body = "{\"detail\":[{\"loc\":[\"query\",\"page\"],\"msg\":\"bad\",\"type\":\"value_error\"}]}";
            return new HttpResponseMessage((HttpStatusCode)422)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        });

        using var http = new HttpClient(handler);
        var client = new SeclaiClient(new SeclaiClientOptions { ApiKey = "k", BaseUri = new Uri("https://example.invalid"), HttpClient = http });

        var ex = await Assert.ThrowsAsync<ApiValidationException>(async () => await client.ListSourcesAsync(page: 0));
        Assert.NotNull(ex.Validation);
        Assert.NotNull(ex.Validation!.Detail);
        Assert.Single(ex.Validation.Detail!);
    }

    [Fact]
    public async System.Threading.Tasks.Task RunAgent_SerializesBody()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/agents/a/runs", req.RequestUri!.AbsolutePath);

            var json = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);
            Assert.True(doc.RootElement.TryGetProperty("priority", out _));

            var resp = new AgentRunResponse { RunId = "run_1", Status = "pending" };
            var body = JsonSerializer.Serialize(resp);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        });

        using var http = new HttpClient(handler);
        var client = new SeclaiClient(new SeclaiClientOptions { ApiKey = "k", BaseUri = new Uri("https://example.invalid"), HttpClient = http });

        var res = await client.RunAgentAsync("a", new AgentRunRequest { Priority = false });
        Assert.Equal("run_1", res.RunId);
    }

    [Fact]
    public async Task RunStreamingAgentAndWait_ParsesDoneEvent()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/agents/a/runs/stream", req.RequestUri!.AbsolutePath);

            Assert.Contains(req.Headers.Accept, h => h.MediaType == "text/event-stream");

            var sse =
                ": keepalive\n\n" +
                "event: init\n" +
                "data: {\"run_id\":\"run_1\",\"status\":\"processing\",\"attempts\":[],\"error_count\":0,\"priority\":false,\"credits\":null,\"input\":null,\"output\":null}\n\n" +
                "event: done\n" +
                "data: {\"run_id\":\"run_1\",\"status\":\"completed\",\"output\":\"ok\",\"attempts\":[],\"error_count\":0,\"priority\":false,\"credits\":null,\"input\":null}\n\n";

            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new System.IO.MemoryStream(Encoding.UTF8.GetBytes(sse)))
            };
            resp.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/event-stream");
            return resp;
        });

        using var http = new HttpClient(handler);
        var client = new SeclaiClient(new SeclaiClientOptions { ApiKey = "k", BaseUri = new Uri("https://example.invalid"), HttpClient = http });

        var res = await client.RunStreamingAgentAndWaitAsync("a", new AgentRunStreamRequest { Input = "hi", Metadata = new System.Collections.Generic.Dictionary<string, JsonElement>() });
        Assert.Equal("run_1", res.RunId);
        Assert.Equal("ok", res.Output);
    }

    [Fact]
    public async Task RunStreamingAgentAndWait_TimesOut()
    {
        var handler = new FakeHttpMessageHandler(_ =>
        {
            // Stream that never ends; cancellation should dispose it.
            var stream = new NeverEndingStream(Encoding.UTF8.GetBytes("event: init\n" + "data: {}\n\n"));
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(stream)
            };
            resp.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/event-stream");
            return resp;
        });

        using var http = new HttpClient(handler);
        var client = new SeclaiClient(new SeclaiClientOptions { ApiKey = "k", BaseUri = new Uri("https://example.invalid"), HttpClient = http });

        await Assert.ThrowsAsync<StreamingException>(async () =>
            await client.RunStreamingAgentAndWaitAsync(
                "a",
                new AgentRunStreamRequest { Input = "hi", Metadata = new System.Collections.Generic.Dictionary<string, JsonElement>() },
                timeout: TimeSpan.FromMilliseconds(25)
            ));
    }

    private sealed class NeverEndingStream : System.IO.Stream
    {
        private readonly byte[] _prefix;
        private int _offset;
        private volatile bool _disposed;
        private readonly ManualResetEventSlim _disposedEvent = new(false);

        public NeverEndingStream(byte[] prefix)
        {
            _prefix = prefix;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(NeverEndingStream));

            if (_offset < _prefix.Length)
            {
                var remaining = _prefix.Length - _offset;
                var toCopy = Math.Min(count, remaining);
                Buffer.BlockCopy(_prefix, _offset, buffer, offset, toCopy);
                _offset += toCopy;
                return toCopy;
            }

            // Block until disposed.
            while (!_disposed)
            {
                _disposedEvent.Wait(5);
            }
            throw new ObjectDisposedException(nameof(NeverEndingStream));
        }

        protected override void Dispose(bool disposing)
        {
            _disposed = true;
            _disposedEvent.Set();
            base.Dispose(disposing);
        }

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, System.IO.SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
