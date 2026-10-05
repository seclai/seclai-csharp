using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Seclai.Exceptions;
using Xunit;

namespace Seclai.Tests;

/// <summary>
/// What the client puts on the wire. Captured from a socket, because a handler sees the
/// request before <see cref="HttpClient"/> folds repeated values into one line.
/// </summary>
[Collection("Sequential")]
public sealed class RequestHeaderTests
{
    private const string Body = "{\"data\":[],\"pagination\":{\"page\":1,\"limit\":20,\"total\":0,\"pages\":0,\"has_next\":false,\"has_prev\":false}}";

    private static async Task<string> CaptureAsync(Func<Uri, SeclaiClient> make)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var server = Task.Run(async () =>
            {
                using var tcp = await listener.AcceptTcpClientAsync();
                using var stream = tcp.GetStream();
                var buffer = new byte[16384];
                var head = new StringBuilder();
                while (!head.ToString().Contains("\r\n\r\n"))
                {
                    var read = await stream.ReadAsync(buffer);
                    if (read == 0) break;
                    head.Append(Encoding.ASCII.GetString(buffer, 0, read));
                }
                var response = $"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: {Body.Length}\r\nConnection: close\r\n\r\n{Body}";
                await stream.WriteAsync(Encoding.ASCII.GetBytes(response));
                return head.ToString();
            });

            using var client = make(new Uri($"http://127.0.0.1:{port}"));
            await client.ListAgentsAsync();
            return await server;
        }
        finally
        {
            listener.Stop();
        }
    }

    /// <summary>Every value sent for a header: one entry per line, split on commas.</summary>
    private static string[] Sent(string request, string name)
        => request.Split(new[] { "\r\n" }, StringSplitOptions.None)
            .Where(line => line.StartsWith(name + ":", StringComparison.OrdinalIgnoreCase))
            .SelectMany(line => line.Substring(name.Length + 1).Split(','))
            .Select(value => value.Trim())
            .ToArray();

    [Fact]
    public async Task ApiKey_WinsOverDefaultHeadersAndTheHttpClient()
    {
        var request = await CaptureAsync(uri =>
        {
            var http = new HttpClient();
            http.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", "from-http-client");
            return new SeclaiClient(new SeclaiClientOptions
            {
                ApiKey = "k",
                BaseUri = uri,
                HttpClient = http,
                DefaultHeaders = new Dictionary<string, string> { ["X-API-Key"] = "from-default-headers" },
            });
        });
        Assert.Equal(new[] { "k" }, Sent(request, "x-api-key"));
    }

    [Fact]
    public async Task BearerToken_WinsOverDefaultHeadersAndTheHttpClient()
    {
        var request = await CaptureAsync(uri =>
        {
            var http = new HttpClient();
            http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer from-http-client");
            return new SeclaiClient(new SeclaiClientOptions
            {
                AccessToken = "tok",
                BaseUri = uri,
                HttpClient = http,
                DefaultHeaders = new Dictionary<string, string> { ["authorization"] = "Bearer from-default-headers" },
            });
        });
        Assert.Equal(new[] { "Bearer tok" }, Sent(request, "Authorization"));
    }

    [Fact]
    public async Task AccountId_WinsOverDefaultHeadersAndTheHttpClient()
    {
        var request = await CaptureAsync(uri =>
        {
            var http = new HttpClient();
            http.DefaultRequestHeaders.TryAddWithoutValidation("X-Account-Id", "from-http-client");
            return new SeclaiClient(new SeclaiClientOptions
            {
                ApiKey = "k",
                AccountId = "acct",
                BaseUri = uri,
                HttpClient = http,
                DefaultHeaders = new Dictionary<string, string> { ["x-account-id"] = "from-default-headers" },
            });
        });
        Assert.Equal(new[] { "acct" }, Sent(request, "X-Account-Id"));
    }

    [Fact]
    public async Task DefaultHeaders_ReplaceTheHttpClientsAndAreSentOnce()
    {
        var request = await CaptureAsync(uri =>
        {
            var http = new HttpClient();
            http.DefaultRequestHeaders.TryAddWithoutValidation("X-Trace", "from-http-client");
            http.DefaultRequestHeaders.TryAddWithoutValidation("X-Only-Http", "kept");
            return new SeclaiClient(new SeclaiClientOptions
            {
                ApiKey = "k",
                BaseUri = uri,
                HttpClient = http,
                DefaultHeaders = new Dictionary<string, string> { ["X-Trace"] = "first", ["x-trace"] = "second", ["Accept"] = "text/plain" },
            });
        });
        Assert.Contains(Assert.Single(Sent(request, "X-Trace")), new[] { "first", "second" });
        Assert.Equal(new[] { "kept" }, Sent(request, "X-Only-Http"));
        Assert.Equal(new[] { "application/json" }, Sent(request, "Accept"));
    }

    [Fact]
    public async Task SeclaiVersion_IsSentOnceWhicheverLayerSetsIt()
    {
        var fromOption = await CaptureAsync(uri =>
        {
            var http = new HttpClient();
            http.DefaultRequestHeaders.TryAddWithoutValidation("Seclai-Version", SeclaiApiVersion.V2026_07_01);
            return new SeclaiClient(new SeclaiClientOptions { ApiKey = "k", BaseUri = uri, HttpClient = http, ApiVersion = SeclaiApiVersion.V2026_07_27 });
        });
        Assert.Equal(new[] { SeclaiApiVersion.V2026_07_27 }, Sent(fromOption, "Seclai-Version"));

        var fromHttpClient = await CaptureAsync(uri =>
        {
            var http = new HttpClient();
            http.DefaultRequestHeaders.TryAddWithoutValidation("seclai-version", SeclaiApiVersion.V2026_07_27);
            return new SeclaiClient(new SeclaiClientOptions { ApiKey = "k", BaseUri = uri, HttpClient = http });
        });
        Assert.Equal(new[] { SeclaiApiVersion.V2026_07_27 }, Sent(fromHttpClient, "Seclai-Version"));

        var unset = await CaptureAsync(uri => new SeclaiClient(new SeclaiClientOptions { ApiKey = "k", BaseUri = uri, HttpClient = new HttpClient() }));
        Assert.Empty(Sent(unset, "Seclai-Version"));
    }

    [Fact]
    public async Task SeclaiVersion_UnknownOnTheHttpClientIsSentOnlyWhenAllowed()
    {
        var request = await CaptureAsync(uri =>
        {
            var http = new HttpClient();
            http.DefaultRequestHeaders.TryAddWithoutValidation("Seclai-Version", "2099-01-01");
            return new SeclaiClient(new SeclaiClientOptions { ApiKey = "k", BaseUri = uri, HttpClient = http, AllowUnknownApiVersion = true });
        });
        Assert.Equal(new[] { "2099-01-01" }, Sent(request, "Seclai-Version"));
    }

    private static (HttpClient Http, Func<int> Requests) CountingHttpClient()
    {
        var requests = 0;
        var http = new HttpClient(new FakeHttpMessageHandler(_ =>
        {
            requests++;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Body, Encoding.UTF8, "application/json") };
        }));
        return (http, () => requests);
    }

    [Theory]
    [InlineData("2099-01-01")]
    [InlineData("")]
    public void SeclaiVersion_OnTheHttpClientIsRejectedAtConstruction(string version)
    {
        var (http, _) = CountingHttpClient();
        http.DefaultRequestHeaders.TryAddWithoutValidation("Seclai-Version", version);

        var ex = Assert.Throws<ConfigurationException>(() => new SeclaiClient(new SeclaiClientOptions
        {
            ApiKey = "k",
            BaseUri = new Uri("https://example.invalid"),
            HttpClient = http,
        }));
        Assert.Contains("HttpClient.DefaultRequestHeaders", ex.Message);
        Assert.Contains("AllowUnknownApiVersion", ex.Message);
    }

    [Fact]
    public async Task SeclaiVersion_SetOnTheHttpClientAfterConstructionIsRejectedBeforeSending()
    {
        var (http, requests) = CountingHttpClient();
        using var client = new SeclaiClient(new SeclaiClientOptions { ApiKey = "k", BaseUri = new Uri("https://example.invalid"), HttpClient = http });
        await client.ListAgentsAsync();
        Assert.Equal(1, requests());

        http.DefaultRequestHeaders.TryAddWithoutValidation("Seclai-Version", "2099-01-01");
        await Assert.ThrowsAsync<ConfigurationException>(() => client.ListAgentsAsync());
        await Assert.ThrowsAsync<ConfigurationException>(() => client.DeleteAgentAsync("a1"));
        await Assert.ThrowsAsync<ConfigurationException>(() => client.SearchAsync("q"));
        Assert.Equal(1, requests());

        http.DefaultRequestHeaders.Remove("Seclai-Version");
        http.DefaultRequestHeaders.TryAddWithoutValidation("Seclai-Version", SeclaiApiVersion.Latest);
        await client.ListAgentsAsync();
        Assert.Equal(2, requests());
    }

    [Fact]
    public async Task SeclaiVersion_AddedWhileTheTokenIsFetchedIsStillChecked()
    {
        var (http, requests) = CountingHttpClient();
        using var client = new SeclaiClient(new SeclaiClientOptions
        {
            BaseUri = new Uri("https://example.invalid"),
            HttpClient = http,
            AccessTokenProvider = async _ =>
            {
                await Task.Yield();
                http.DefaultRequestHeaders.TryAddWithoutValidation("Seclai-Version", "2099-01-01");
                return "tok";
            },
        });

        await Assert.ThrowsAsync<ConfigurationException>(() => client.ListAgentsAsync());
        Assert.Equal(0, requests());
    }

    [Fact]
    public async Task SeclaiVersion_TheSameValueTwiceOnTheHttpClientIsSentOnce()
    {
        var request = await CaptureAsync(uri =>
        {
            var http = new HttpClient();
            http.DefaultRequestHeaders.TryAddWithoutValidation("Seclai-Version", SeclaiApiVersion.V2026_07_27);
            http.DefaultRequestHeaders.TryAddWithoutValidation("Seclai-Version", SeclaiApiVersion.V2026_07_27);
            return new SeclaiClient(new SeclaiClientOptions { ApiKey = "k", BaseUri = uri, HttpClient = http });
        });
        Assert.Equal(new[] { SeclaiApiVersion.V2026_07_27 }, Sent(request, "Seclai-Version"));
    }

    [Fact]
    public async Task SeclaiVersion_TwoValuesOnTheHttpClientAreRejected()
    {
        var (http, requests) = CountingHttpClient();
        using var client = new SeclaiClient(new SeclaiClientOptions { ApiKey = "k", BaseUri = new Uri("https://example.invalid"), HttpClient = http, AllowUnknownApiVersion = true });
        http.DefaultRequestHeaders.TryAddWithoutValidation("Seclai-Version", SeclaiApiVersion.V2026_07_01);
        http.DefaultRequestHeaders.TryAddWithoutValidation("Seclai-Version", SeclaiApiVersion.V2026_07_27);

        await Assert.ThrowsAsync<ConfigurationException>(() => client.ListAgentsAsync());
        Assert.Equal(0, requests());
    }

    [Fact]
    public async Task SeclaiVersion_OptionMakesTheHttpClientsValueIrrelevant()
    {
        // The request carries the option's value, so HttpClient drops its own default for that header.
        var (http, requests) = CountingHttpClient();
        http.DefaultRequestHeaders.TryAddWithoutValidation("Seclai-Version", "2099-01-01");
        using var client = new SeclaiClient(new SeclaiClientOptions
        {
            ApiKey = "k",
            BaseUri = new Uri("https://example.invalid"),
            HttpClient = http,
            ApiVersion = SeclaiApiVersion.Latest,
        });
        await client.ListAgentsAsync();
        Assert.Equal(1, requests());
    }
}
