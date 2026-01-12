using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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
            Assert.Equal("/api/sources/", req.RequestUri!.AbsolutePath);
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
            Assert.Equal("/api/agents/a/runs", req.RequestUri!.AbsolutePath);

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
}
