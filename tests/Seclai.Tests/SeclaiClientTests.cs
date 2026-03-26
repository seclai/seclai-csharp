using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        var originalApiKey = Environment.GetEnvironmentVariable("SECLAI_API_KEY");
        var originalConfigDir = Environment.GetEnvironmentVariable("SECLAI_CONFIG_DIR");
        Environment.SetEnvironmentVariable("SECLAI_API_KEY", "k");
        try
        {
            var client = new SeclaiClient(new SeclaiClientOptions { HttpClient = new HttpClient(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))) });
            Assert.NotNull(client);
        }
        finally
        {
            Environment.SetEnvironmentVariable("SECLAI_API_KEY", originalApiKey);
            Environment.SetEnvironmentVariable("SECLAI_CONFIG_DIR", originalConfigDir);
        }
    }

    [Fact]
    public void Constructor_ThrowsWhenNoCredentials()
    {
        var originalApiKey = Environment.GetEnvironmentVariable("SECLAI_API_KEY");
        var originalConfigDir = Environment.GetEnvironmentVariable("SECLAI_CONFIG_DIR");
        Environment.SetEnvironmentVariable("SECLAI_API_KEY", null);
        Environment.SetEnvironmentVariable("SECLAI_CONFIG_DIR", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
        try
        {
            Assert.Throws<ConfigurationException>(() =>
                new SeclaiClient(new SeclaiClientOptions()));
        }
        finally
        {
            Environment.SetEnvironmentVariable("SECLAI_API_KEY", originalApiKey);
            Environment.SetEnvironmentVariable("SECLAI_CONFIG_DIR", originalConfigDir);
        }
    }

    [Fact]
    public void Constructor_ThrowsWhenBothApiKeyAndAccessToken()
    {
        Assert.Throws<ConfigurationException>(() =>
            new SeclaiClient(new SeclaiClientOptions { ApiKey = "k", AccessToken = "tok" }));
    }

    [Fact]
    public async Task BearerStaticToken_SetsAuthorizationHeader()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.True(req.Headers.Authorization is not null);
            Assert.Equal("Bearer", req.Headers.Authorization!.Scheme);
            Assert.Equal("my-jwt", req.Headers.Authorization.Parameter);
            // Should NOT have x-api-key
            Assert.False(req.Headers.Contains("x-api-key"));

            var body = "{\"data\":[],\"pagination\":{\"has_next\":false,\"has_prev\":false,\"limit\":20,\"page\":1,\"pages\":1,\"total\":0}}";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        });

        var client = new SeclaiClient(new SeclaiClientOptions
        {
            AccessToken = "my-jwt",
            BaseUri = new Uri("https://example.invalid"),
            HttpClient = new HttpClient(handler)
        });

        var res = await client.ListSourcesAsync();
        Assert.NotNull(res);
    }

    [Fact]
    public async Task BearerProvider_CalledPerRequest()
    {
        var callCount = 0;
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.True(req.Headers.Authorization is not null);
            Assert.Equal("Bearer", req.Headers.Authorization!.Scheme);

            var body = "{\"data\":[],\"pagination\":{\"has_next\":false,\"has_prev\":false,\"limit\":20,\"page\":1,\"pages\":1,\"total\":0}}";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        });

        var client = new SeclaiClient(new SeclaiClientOptions
        {
            AccessTokenProvider = ct =>
            {
                callCount++;
                return Task.FromResult("tok-" + callCount);
            },
            BaseUri = new Uri("https://example.invalid"),
            HttpClient = new HttpClient(handler)
        });

        await client.ListSourcesAsync();
        await client.ListSourcesAsync();
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task AccountId_SetsHeader()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.True(req.Headers.Contains("X-Account-Id"));
            Assert.Equal("acct-123", req.Headers.GetValues("X-Account-Id").First());

            var body = "{\"data\":[],\"pagination\":{\"has_next\":false,\"has_prev\":false,\"limit\":20,\"page\":1,\"pages\":1,\"total\":0}}";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        });

        var client = new SeclaiClient(new SeclaiClientOptions
        {
            AccessToken = "tok",
            AccountId = "acct-123",
            BaseUri = new Uri("https://example.invalid"),
            HttpClient = new HttpClient(handler)
        });

        var res = await client.ListSourcesAsync();
        Assert.NotNull(res);
    }

    [Fact]
    public void ParseIni_ParsesSections()
    {
        var input = "[default]\nsso_region = us-east-1\n\n[profile dev]\nsso_account_id = 123\n";
        using var reader = new System.IO.StringReader(input);
        var sections = SeclaiAuth.ParseIni(reader);

        Assert.Equal("us-east-1", sections["default"]["sso_region"]);
        Assert.Equal("123", sections["dev"]["sso_account_id"]);
    }

    [Fact]
    public void IsTokenValid_ChecksExpiry()
    {
        var future = new SsoCacheEntry { ExpiresAt = DateTimeOffset.UtcNow.AddHours(1).ToString("o") };
        var past = new SsoCacheEntry { ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1).ToString("o") };
        var nearExpiry = new SsoCacheEntry { ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(20).ToString("o") };

        Assert.True(SeclaiAuth.IsTokenValid(future));
        Assert.False(SeclaiAuth.IsTokenValid(past));
        Assert.False(SeclaiAuth.IsTokenValid(nearExpiry));
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

        var client = MakeClient(handler);
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

        var client = MakeClient(handler);

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

        var client = MakeClient(handler);

        var res = await client.RunAgentAsync("a", new AgentRunRequest { Priority = false });
        Assert.Equal("run_1", res.RunId);
    }

    [Fact]
    public async Task GetAgentRun_CanIncludeStepOutputs()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/agents/runs/run_1", req.RequestUri!.AbsolutePath);
            Assert.Contains("include_step_outputs=true", req.RequestUri!.Query);

            var body = "{" +
                       "\"run_id\":\"run_1\"," +
                       "\"status\":\"completed\"," +
                       "\"attempts\":[]," +
                       "\"error_count\":0," +
                       "\"priority\":false," +
                       "\"credits\":null," +
                       "\"input\":null," +
                       "\"output\":\"ok\"," +
                       "\"steps\":[{" +
                           "\"agent_step_id\":\"step_1\"," +
                           "\"step_type\":\"tool\"," +
                           "\"status\":\"completed\"," +
                           "\"output\":\"out\"," +
                           "\"output_content_type\":\"text/plain\"," +
                           "\"started_at\":null," +
                           "\"ended_at\":null," +
                           "\"duration_seconds\":null," +
                           "\"credits_used\":0" +
                       "}]}";

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        });

        var client = MakeClient(handler);

        var res = await client.GetAgentRunAsync("run_1", includeStepOutputs: true);
        Assert.Equal("run_1", res.RunId);
        Assert.NotNull(res.Steps);
        Assert.Single(res.Steps!);
        Assert.Equal("step_1", res.Steps![0].AgentStepId);
    }

    [Fact]
    public async Task GetAgentRunByRunId_CanIncludeStepOutputs()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/agents/runs/run_1", req.RequestUri!.AbsolutePath);
            Assert.Contains("include_step_outputs=true", req.RequestUri!.Query);

            var body = "{" +
                       "\"run_id\":\"run_1\"," +
                       "\"status\":\"completed\"," +
                       "\"attempts\":[]," +
                       "\"error_count\":0," +
                       "\"priority\":false," +
                       "\"credits\":null," +
                       "\"input\":null," +
                       "\"output\":\"ok\"," +
                       "\"steps\":[]}";

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        });

        var client = MakeClient(handler);

        var res = await client.GetAgentRunAsync("run_1", includeStepOutputs: true);
        Assert.Equal("run_1", res.RunId);
    }

    [Fact]
    public async Task DeleteAgentRun_UsesRunIdOnlyEndpoint()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Delete, req.Method);
            Assert.Equal("/agents/runs/run_1", req.RequestUri!.AbsolutePath);

            var resp = new AgentRunResponse { RunId = "run_1", Status = "processing" };
            var body = JsonSerializer.Serialize(resp);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        });

        var client = MakeClient(handler);

        var deleted = await client.DeleteAgentRunAsync("run_1");
        Assert.Equal("run_1", deleted.RunId);
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

        var client = MakeClient(handler);

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

        var client = MakeClient(handler);

        await Assert.ThrowsAsync<StreamingException>(async () =>
            await client.RunStreamingAgentAndWaitAsync(
                "a",
                new AgentRunStreamRequest { Input = "hi", Metadata = new System.Collections.Generic.Dictionary<string, JsonElement>() },
                timeout: TimeSpan.FromMilliseconds(25)
            ));
    }

    [Fact]
    public async Task UploadFileToSource_SendsMetadataMultipartField()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/sources/sc_1/upload", req.RequestUri!.AbsolutePath);
            Assert.Equal("multipart/form-data", req.Content!.Headers.ContentType!.MediaType);

            var multipart = req.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            Assert.True(multipart.Contains("name=\"metadata\"", StringComparison.Ordinal) || multipart.Contains("name=metadata", StringComparison.Ordinal));
            Assert.True(multipart.Contains("name=\"file\"", StringComparison.Ordinal) || multipart.Contains("name=file", StringComparison.Ordinal));
            Assert.True(multipart.Contains("filename=\"hello.txt\"", StringComparison.Ordinal) || multipart.Contains("filename=hello.txt", StringComparison.Ordinal));

            var metadataJson = ExtractMultipartPartValue(multipart, "metadata");
            using var doc = JsonDocument.Parse(metadataJson);
            Assert.Equal("docs", doc.RootElement.GetProperty("category").GetString());
            Assert.Equal("Ada", doc.RootElement.GetProperty("author").GetString());

            var body = "{\"filename\":\"hello.txt\",\"status\":\"success\",\"source_connection_content_version_id\":\"sc_cv_1\"}";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        });

        var client = MakeClient(handler);

        var res = await client.UploadFileToSourceAsync(
            sourceConnectionId: "sc_1",
            fileBytes: Encoding.UTF8.GetBytes("hello"),
            fileName: "hello.txt",
            metadata: new Dictionary<string, object?> { ["category"] = "docs", ["author"] = "Ada" });

        Assert.Equal("hello.txt", res.Filename);
        Assert.Equal("success", res.Status);
        Assert.Equal("sc_cv_1", res.SourceConnectionContentVersionId);
    }

    [Fact]
    public async Task UploadFileToContent_PostsToContentUploadEndpointAndSendsMetadata()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/contents/sc_cv_123/upload", req.RequestUri!.AbsolutePath);
            Assert.Equal("multipart/form-data", req.Content!.Headers.ContentType!.MediaType);

            var multipart = req.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            Assert.True(multipart.Contains("name=\"metadata\"", StringComparison.Ordinal) || multipart.Contains("name=metadata", StringComparison.Ordinal));
            Assert.True(multipart.Contains("name=\"file\"", StringComparison.Ordinal) || multipart.Contains("name=file", StringComparison.Ordinal));
            Assert.True(multipart.Contains("filename=\"updated.pdf\"", StringComparison.Ordinal) || multipart.Contains("filename=updated.pdf", StringComparison.Ordinal));

            var metadataJson = ExtractMultipartPartValue(multipart, "metadata");
            using var doc = JsonDocument.Parse(metadataJson);
            Assert.Equal(2, doc.RootElement.GetProperty("revision").GetInt32());

            var body = "{\"filename\":\"updated.pdf\",\"status\":\"success\",\"source_connection_content_version_id\":\"sc_cv_123\"}";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        });

        var client = MakeClient(handler);

        var res = await client.UploadFileToContentAsync(
            sourceConnectionContentVersionId: "sc_cv_123",
            fileBytes: Encoding.UTF8.GetBytes("%PDF-1.4"),
            fileName: "updated.pdf",
            metadata: new Dictionary<string, object?> { ["revision"] = 2 });

        Assert.Equal("updated.pdf", res.Filename);
        Assert.Equal("success", res.Status);
        Assert.Equal("sc_cv_123", res.SourceConnectionContentVersionId);
    }

    // ── Agents ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListAgents_SetsPathAndDeserializes()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/agents", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"data\":[{\"id\":\"a1\",\"name\":\"Bot\",\"description\":\"\",\"is_public\":false,\"created_at\":\"\",\"updated_at\":\"\"}],\"total\":1}");
        });
        var client = MakeClient(handler);
        var res = await client.ListAgentsAsync(page: 1, limit: 10);
        Assert.Single(res.Data);
        Assert.Equal("a1", res.Data[0].Id);
    }

    [Fact]
    public async Task CreateAgent_PostsBody()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/agents", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"id\":\"a1\",\"name\":\"Bot\",\"description\":\"\",\"is_public\":false,\"created_at\":\"\",\"updated_at\":\"\"}");
        });
        var client = MakeClient(handler);
        var res = await client.CreateAgentAsync(new CreateAgentRequest { Name = "Bot" });
        Assert.Equal("a1", res.Id);
    }

    [Fact]
    public async Task GetAgent_UsesAgentIdInPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/agents/a1", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"id\":\"a1\",\"name\":\"Bot\",\"description\":\"\",\"is_public\":false,\"created_at\":\"\",\"updated_at\":\"\"}");
        });
        var client = MakeClient(handler);
        var res = await client.GetAgentAsync("a1");
        Assert.Equal("a1", res.Id);
    }

    [Fact]
    public async Task UpdateAgent_PutsBody()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Put, req.Method);
            Assert.Equal("/agents/a1", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"id\":\"a1\",\"name\":\"Updated\",\"description\":\"\",\"is_public\":false,\"created_at\":\"\",\"updated_at\":\"\"}");
        });
        var client = MakeClient(handler);
        var res = await client.UpdateAgentAsync("a1", new UpdateAgentRequest { Name = "Updated" });
        Assert.Equal("Updated", res.Name);
    }

    [Fact]
    public async Task DeleteAgent_SendsDelete()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Delete, req.Method);
            Assert.Equal("/agents/a1", req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var client = MakeClient(handler);
        await client.DeleteAgentAsync("a1"); // should not throw
    }

    // ── Agent Definitions ───────────────────────────────────────────────────

    [Fact]
    public async Task GetAgentDefinition_GetsPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/agents/a1/definition", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"change_id\":\"c1\",\"schema_version\":\"1\",\"definition\":{}}");
        });
        var client = MakeClient(handler);
        var res = await client.GetAgentDefinitionAsync("a1");
        Assert.Equal("c1", res.ChangeId);
    }

    [Fact]
    public async Task UpdateAgentDefinition_PutsBody()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Put, req.Method);
            Assert.Equal("/agents/a1/definition", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"change_id\":\"c2\",\"schema_version\":\"1\",\"definition\":{}}");
        });
        var client = MakeClient(handler);
        var res = await client.UpdateAgentDefinitionAsync("a1", new UpdateAgentDefinitionRequest { ExpectedChangeId = "c1" });
        Assert.Equal("c2", res.ChangeId);
    }

    // ── Agent Runs (additions) ──────────────────────────────────────────────

    [Fact]
    public async Task SearchAgentRuns_PostsBody()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/agents/runs/search", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"results\":[],\"total\":0}");
        });
        var client = MakeClient(handler);
        var res = await client.SearchAgentRunsAsync(new AgentTraceSearchRequest { Query = "test" });
        Assert.Equal(0, res.Total);
    }

    [Fact]
    public async Task CancelAgentRun_PostsToCancel()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/agents/runs/r1/cancel", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"run_id\":\"r1\",\"status\":\"cancelled\",\"attempts\":[],\"error_count\":0,\"priority\":false}");
        });
        var client = MakeClient(handler);
        var res = await client.CancelAgentRunAsync("r1");
        Assert.Equal("cancelled", res.Status);
    }

    // ── Agent Input Uploads ─────────────────────────────────────────────────

    [Fact]
    public async Task UploadAgentInput_PostsMultipart()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/agents/a1/upload-input", req.RequestUri!.AbsolutePath);
            Assert.Equal("multipart/form-data", req.Content!.Headers.ContentType!.MediaType);
            return JsonResponse("{\"id\":\"u1\",\"filename\":\"f.txt\",\"content_type\":\"text/plain\",\"file_size\":5,\"status\":\"completed\"}");
        });
        var client = MakeClient(handler);
        var res = await client.UploadAgentInputAsync("a1", Encoding.UTF8.GetBytes("hello"), "f.txt");
        Assert.Equal("u1", res.Id);
    }

    [Fact]
    public async Task GetAgentInputUploadStatus_GetsPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/agents/a1/input-uploads/u1", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"id\":\"u1\",\"filename\":\"f.txt\",\"content_type\":\"text/plain\",\"file_size\":5,\"status\":\"completed\"}");
        });
        var client = MakeClient(handler);
        var res = await client.GetAgentInputUploadStatusAsync("a1", "u1");
        Assert.Equal("completed", res.Status);
    }

    // ── Agent AI Assistant ──────────────────────────────────────────────────

    [Fact]
    public async Task GenerateAgentSteps_PostsBody()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/agents/a1/ai-assistant/generate-steps", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"steps\":[]}");
        });
        var client = MakeClient(handler);
        var res = await client.GenerateAgentStepsAsync("a1", new GenerateAgentStepsRequest { UserInput = "build a bot" });
        Assert.NotNull(res.Steps);
    }

    [Fact]
    public async Task GenerateStepConfig_PostsBody()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/agents/a1/ai-assistant/step-config", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"conversation_id\":\"c1\",\"note\":\"ok\",\"step_type\":\"llm\",\"success\":true}");
        });
        var client = MakeClient(handler);
        var res = await client.GenerateStepConfigAsync("a1", new GenerateStepConfigRequest { StepType = "llm", UserInput = "configure" });
        Assert.True(res.Success);
    }

    [Fact]
    public async Task GetAgentAiConversationHistory_GetsPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/agents/a1/ai-assistant/conversations", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"total\":0,\"turns\":[]}");
        });
        var client = MakeClient(handler);
        var res = await client.GetAgentAiConversationHistoryAsync("a1");
        Assert.Equal(0, res.Total);
    }

    [Fact]
    public async Task MarkAgentAiSuggestion_PatchesPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal("PATCH", req.Method.Method);
            Assert.Equal("/agents/a1/ai-assistant/c1", req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var client = MakeClient(handler);
        await client.MarkAgentAiSuggestionAsync("a1", "c1", new MarkAiSuggestionRequest { Accepted = true });
    }

    // ── Agent Evaluations ───────────────────────────────────────────────────

    [Fact]
    public async Task ListEvaluationCriteria_GetsPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/agents/a1/evaluation-criteria", req.RequestUri!.AbsolutePath);
            return JsonResponse("[{\"id\":\"ec1\",\"name\":\"Accuracy\",\"description\":\"test\"}]");
        });
        var client = MakeClient(handler);
        var res = await client.ListEvaluationCriteriaAsync("a1");
        Assert.Single(res);
    }

    [Fact]
    public async Task CreateEvaluationCriteria_PostsBody()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/agents/a1/evaluation-criteria", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"id\":\"ec1\",\"name\":\"Accuracy\",\"description\":\"test\"}");
        });
        var client = MakeClient(handler);
        var res = await client.CreateEvaluationCriteriaAsync("a1", new CreateEvaluationCriteriaRequest { StepId = "s1" });
        Assert.Equal("ec1", res.Id);
    }

    [Fact]
    public async Task DeleteEvaluationCriteria_DeletesPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Delete, req.Method);
            Assert.Equal("/agents/evaluation-criteria/ec1", req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var client = MakeClient(handler);
        await client.DeleteEvaluationCriteriaAsync("ec1");
    }

    [Fact]
    public async Task GetEvaluationCriteriaSummary_GetsPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/agents/evaluation-criteria/ec1/summary", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"total\":10,\"passed\":8,\"failed\":1,\"flagged\":1,\"error\":0}");
        });
        var client = MakeClient(handler);
        var res = await client.GetEvaluationCriteriaSummaryAsync("ec1");
        Assert.Equal(10, res.Total);
    }

    [Fact]
    public async Task TestDraftEvaluation_PostsBody()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/agents/a1/evaluation-criteria/test-draft", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"passed\":true,\"score\":0.95}");
        });
        var client = MakeClient(handler);
        var res = await client.TestDraftEvaluationAsync("a1", new TestDraftEvaluationRequest { AgentInput = "hi" });
        Assert.True(res.Passed);
    }

    [Fact]
    public async Task ListAgentEvaluationResults_GetsPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/agents/a1/evaluation-results", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"data\":[],\"total\":0,\"page\":1,\"limit\":20}");
        });
        var client = MakeClient(handler);
        var res = await client.ListAgentEvaluationResultsAsync("a1");
        Assert.Equal(0, res.Total);
    }

    [Fact]
    public async Task GetNonManualEvaluationSummary_GetsPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/agents/evaluation-results/non-manual-summary", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"total\":0,\"passed\":0,\"failed\":0,\"flagged\":0,\"pass_rate\":0,\"failure_rate\":0,\"by_mode\":[]}");
        });
        var client = MakeClient(handler);
        var res = await client.GetNonManualEvaluationSummaryAsync();
        Assert.Equal(0, res.Total);
    }

    // ── Knowledge Bases ─────────────────────────────────────────────────────

    [Fact]
    public async Task ListKnowledgeBases_SetsQueryParams()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/knowledge_bases", req.RequestUri!.AbsolutePath);
            Assert.Contains("sort=created_at", req.RequestUri!.Query);
            return JsonResponse("{\"data\":[],\"total\":0}");
        });
        var client = MakeClient(handler);
        var res = await client.ListKnowledgeBasesAsync(sort: "created_at");
        Assert.Equal(0, res.Total);
    }

    [Fact]
    public async Task CreateKnowledgeBase_PostsBody()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/knowledge_bases", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"id\":\"kb1\",\"name\":\"KB\",\"description\":\"\",\"created_at\":\"\",\"updated_at\":\"\"}");
        });
        var client = MakeClient(handler);
        var res = await client.CreateKnowledgeBaseAsync(new CreateKnowledgeBaseRequest { Name = "KB" });
        Assert.Equal("kb1", res.Id);
    }

    [Fact]
    public async Task DeleteKnowledgeBase_DeletesPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Delete, req.Method);
            Assert.Equal("/knowledge_bases/kb1", req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var client = MakeClient(handler);
        await client.DeleteKnowledgeBaseAsync("kb1");
    }

    // ── Memory Banks ────────────────────────────────────────────────────────

    [Fact]
    public async Task ListMemoryBanks_GetsPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/memory_banks", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"data\":[],\"total\":0}");
        });
        var client = MakeClient(handler);
        var res = await client.ListMemoryBanksAsync();
        Assert.Equal(0, res.Total);
    }

    [Fact]
    public async Task CreateMemoryBank_PostsBody()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/memory_banks", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"id\":\"mb1\",\"name\":\"MB\",\"description\":\"\",\"type\":\"general\",\"created_at\":\"\",\"updated_at\":\"\"}");
        });
        var client = MakeClient(handler);
        var res = await client.CreateMemoryBankAsync(new CreateMemoryBankRequest { Name = "MB", Type = "general" });
        Assert.Equal("mb1", res.Id);
    }

    [Fact]
    public async Task DeleteMemoryBank_DeletesPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Delete, req.Method);
            Assert.Equal("/memory_banks/mb1", req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var client = MakeClient(handler);
        await client.DeleteMemoryBankAsync("mb1");
    }

    [Fact]
    public async Task GetMemoryBankStats_ReturnsJsonElement()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/memory_banks/mb1/stats", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"entry_count\":42}");
        });
        var client = MakeClient(handler);
        var res = await client.GetMemoryBankStatsAsync("mb1");
        Assert.Equal(42, res.GetProperty("entry_count").GetInt32());
    }

    [Fact]
    public async Task CompactMemoryBank_PostsPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/memory_banks/mb1/compact", req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var client = MakeClient(handler);
        await client.CompactMemoryBankAsync("mb1");
    }

    [Fact]
    public async Task ListMemoryBankTemplates_ReturnsJsonElement()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/memory_banks/templates", req.RequestUri!.AbsolutePath);
            return JsonResponse("[{\"name\":\"chat\"}]");
        });
        var client = MakeClient(handler);
        var res = await client.ListMemoryBankTemplatesAsync();
        Assert.Equal(JsonValueKind.Array, res.ValueKind);
    }

    [Fact]
    public async Task GenerateMemoryBankConfig_PostsBody()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/memory_banks/ai-assistant", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"conversation_id\":\"c1\",\"note\":\"ok\"}");
        });
        var client = MakeClient(handler);
        var res = await client.GenerateMemoryBankConfigAsync(new MemoryBankAiAssistantRequest { UserInput = "create a memory bank" });
        Assert.Equal("c1", res.ConversationId);
    }

    // ── Sources (additions) ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateSource_PostsBody()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/sources", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"id\":\"s1\",\"name\":\"S\",\"source_type\":\"upload\"}");
        });
        var client = MakeClient(handler);
        var res = await client.CreateSourceAsync(new CreateSourceRequest { Name = "S", SourceType = "upload" });
        Assert.Equal("s1", res.Id);
    }

    [Fact]
    public async Task GetSource_GetsPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/sources/s1", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"id\":\"s1\",\"name\":\"S\",\"source_type\":\"upload\"}");
        });
        var client = MakeClient(handler);
        var res = await client.GetSourceAsync("s1");
        Assert.Equal("s1", res.Id);
    }

    [Fact]
    public async Task DeleteSource_DeletesPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Delete, req.Method);
            Assert.Equal("/sources/s1", req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var client = MakeClient(handler);
        await client.DeleteSourceAsync("s1");
    }

    [Fact]
    public async Task UploadInlineTextToSource_PostsBody()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/sources/sc1", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"filename\":\"inline.txt\",\"status\":\"success\"}");
        });
        var client = MakeClient(handler);
        var res = await client.UploadInlineTextToSourceAsync("sc1", new InlineTextUploadRequest { Text = "hello", Title = "test" });
        Assert.Equal("success", res.Status);
    }

    // ── Source Exports ──────────────────────────────────────────────────────

    [Fact]
    public async Task ListSourceExports_GetsPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/sources/s1/exports", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"data\":[],\"total\":0}");
        });
        var client = MakeClient(handler);
        var res = await client.ListSourceExportsAsync("s1");
        Assert.Equal(0, res.Total);
    }

    [Fact]
    public async Task CreateSourceExport_PostsBody()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/sources/s1/exports", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"id\":\"e1\",\"format\":\"csv\",\"status\":\"pending\",\"created_at\":\"\"}");
        });
        var client = MakeClient(handler);
        var res = await client.CreateSourceExportAsync("s1", new CreateExportRequest { Format = "csv" });
        Assert.Equal("e1", res.Id);
    }

    [Fact]
    public async Task DeleteSourceExport_DeletesPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Delete, req.Method);
            Assert.Equal("/sources/s1/exports/e1", req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var client = MakeClient(handler);
        await client.DeleteSourceExportAsync("s1", "e1");
    }

    [Fact]
    public async Task DownloadSourceExport_ReturnsRawResponse()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/sources/s1/exports/e1/download", req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("csv,data") };
        });
        var client = MakeClient(handler);
        using var res = await client.DownloadSourceExportAsync("s1", "e1");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Equal("csv,data", body);
    }

    [Fact]
    public async Task EstimateSourceExport_PostsBody()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/sources/s1/exports/estimate", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"estimated_size_bytes\":1024,\"source_connection_id\":\"sc1\"}");
        });
        var client = MakeClient(handler);
        var res = await client.EstimateSourceExportAsync("s1", new EstimateExportRequest { Format = "csv" });
        Assert.Equal(1024, res.EstimatedSizeBytes);
    }

    // ── Source Embedding Migrations ─────────────────────────────────────────

    [Fact]
    public async Task GetSourceEmbeddingMigration_GetsPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/sources/s1/embedding-migration", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"migration_id\":\"m1\",\"status\":\"completed\"}");
        });
        var client = MakeClient(handler);
        var res = await client.GetSourceEmbeddingMigrationAsync("s1");
        Assert.Equal("m1", res.MigrationId);
    }

    [Fact]
    public async Task StartSourceEmbeddingMigration_PostsBody()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/sources/s1/embedding-migration", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"migration_id\":\"m1\",\"status\":\"pending\"}");
        });
        var client = MakeClient(handler);
        var res = await client.StartSourceEmbeddingMigrationAsync("s1", new StartSourceEmbeddingMigrationRequest { TargetEmbeddingModel = "text-embedding-3-small", TargetDimensions = 1536 });
        Assert.Equal("pending", res.Status);
    }

    [Fact]
    public async Task CancelSourceEmbeddingMigration_PostsToCancel()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/sources/s1/embedding-migration/cancel", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"migration_id\":\"m1\",\"status\":\"cancelled\"}");
        });
        var client = MakeClient(handler);
        var res = await client.CancelSourceEmbeddingMigrationAsync("s1");
        Assert.Equal("cancelled", res.Status);
    }

    // ── Content (additions) ─────────────────────────────────────────────────

    [Fact]
    public async Task ReplaceContentWithInlineText_PutsBody()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Put, req.Method);
            Assert.Equal("/contents/cv1", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"filename\":\"inline.txt\",\"status\":\"success\"}");
        });
        var client = MakeClient(handler);
        var res = await client.ReplaceContentWithInlineTextAsync("cv1", new InlineTextReplaceRequest { Text = "updated" });
        Assert.Equal("success", res.Status);
    }

    // ── Solutions ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ListSolutions_GetsPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/solutions", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"data\":[],\"total\":0}");
        });
        var client = MakeClient(handler);
        var res = await client.ListSolutionsAsync();
        Assert.Equal(0, res.Total);
    }

    [Fact]
    public async Task CreateSolution_PostsBody()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/solutions", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"id\":\"sol1\",\"name\":\"Sol\",\"description\":\"\",\"created_at\":\"\",\"updated_at\":\"\"}");
        });
        var client = MakeClient(handler);
        var res = await client.CreateSolutionAsync(new CreateSolutionRequest { Name = "Sol" });
        Assert.Equal("sol1", res.Id);
    }

    [Fact]
    public async Task DeleteSolution_DeletesPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Delete, req.Method);
            Assert.Equal("/solutions/sol1", req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var client = MakeClient(handler);
        await client.DeleteSolutionAsync("sol1");
    }

    [Fact]
    public async Task UpdateSolution_PatchesBody()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal("PATCH", req.Method.Method);
            Assert.Equal("/solutions/sol1", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"id\":\"sol1\",\"name\":\"Updated\",\"description\":\"\",\"created_at\":\"\",\"updated_at\":\"\"}");
        });
        var client = MakeClient(handler);
        var res = await client.UpdateSolutionAsync("sol1", new UpdateSolutionRequest { Name = "Updated" });
        Assert.Equal("Updated", res.Name);
    }

    [Fact]
    public async Task LinkAgentsToSolution_PostsBody()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/solutions/sol1/agents", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"id\":\"sol1\",\"name\":\"Sol\",\"description\":\"\",\"created_at\":\"\",\"updated_at\":\"\"}");
        });
        var client = MakeClient(handler);
        var res = await client.LinkAgentsToSolutionAsync("sol1", new LinkResourcesRequest { Ids = new List<string> { "a1" } });
        Assert.Equal("sol1", res.Id);
    }

    [Fact]
    public async Task UnlinkAgentsFromSolution_DeletesWithBody()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Delete, req.Method);
            Assert.Equal("/solutions/sol1/agents", req.RequestUri!.AbsolutePath);
            Assert.NotNull(req.Content); // DELETE with body
            return JsonResponse("{\"id\":\"sol1\",\"name\":\"Sol\",\"description\":\"\",\"created_at\":\"\",\"updated_at\":\"\"}");
        });
        var client = MakeClient(handler);
        var res = await client.UnlinkAgentsFromSolutionAsync("sol1", new UnlinkResourcesRequest { Ids = new List<string> { "a1" } });
        Assert.Equal("sol1", res.Id);
    }

    [Fact]
    public async Task ListSolutionConversations_GetsPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/solutions/sol1/conversations", req.RequestUri!.AbsolutePath);
            return JsonResponse("[{\"id\":\"c1\",\"user_input\":\"hi\",\"created_at\":\"\"}]");
        });
        var client = MakeClient(handler);
        var res = await client.ListSolutionConversationsAsync("sol1");
        Assert.Single(res);
    }

    [Fact]
    public async Task MarkSolutionConversationTurn_PatchesPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal("PATCH", req.Method.Method);
            Assert.Equal("/solutions/sol1/conversations/c1", req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var client = MakeClient(handler);
        await client.MarkSolutionConversationTurnAsync("sol1", "c1", new MarkConversationTurnRequest { Accepted = true });
    }

    // ── Solution AI Assistant ───────────────────────────────────────────────

    [Fact]
    public async Task GenerateSolutionAiPlan_PostsBody()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/solutions/sol1/ai-assistant/generate", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"conversation_id\":\"c1\",\"note\":\"ok\",\"proposed_actions\":[]}");
        });
        var client = MakeClient(handler);
        var res = await client.GenerateSolutionAiPlanAsync("sol1", new AiAssistantGenerateRequest { UserInput = "plan" });
        Assert.Equal("c1", res.ConversationId);
    }

    [Fact]
    public async Task DeclineSolutionAiPlan_PostsPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/solutions/sol1/ai-assistant/c1/decline", req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var client = MakeClient(handler);
        await client.DeclineSolutionAiPlanAsync("sol1", "c1");
    }

    // ── Governance AI ───────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateGovernanceAiPlan_PostsBody()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/governance/ai-assistant", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"assistant_response\":\"done\"}");
        });
        var client = MakeClient(handler);
        var res = await client.GenerateGovernanceAiPlanAsync(new GovernanceAiAssistantRequest { UserInput = "create policy" });
        Assert.Equal("done", res.AssistantResponse);
    }

    [Fact]
    public async Task ListGovernanceAiConversations_GetsPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/governance/ai-assistant/conversations", req.RequestUri!.AbsolutePath);
            return JsonResponse("[]");
        });
        var client = MakeClient(handler);
        var res = await client.ListGovernanceAiConversationsAsync();
        Assert.Empty(res);
    }

    [Fact]
    public async Task AcceptGovernanceAiPlan_PostsPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/governance/ai-assistant/c1/accept", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"conversation_id\":\"c1\",\"actions_applied\":[],\"success\":true}");
        });
        var client = MakeClient(handler);
        var res = await client.AcceptGovernanceAiPlanAsync("c1");
        Assert.True(res.Success);
    }

    [Fact]
    public async Task DeclineGovernanceAiPlan_PostsPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/governance/ai-assistant/c1/decline", req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var client = MakeClient(handler);
        await client.DeclineGovernanceAiPlanAsync("c1");
    }

    // ── Alerts ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListAlerts_ReturnsJsonElement()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/alerts", req.RequestUri!.AbsolutePath);
            Assert.Contains("status=active", req.RequestUri!.Query);
            return JsonResponse("{\"data\":[],\"total\":0}");
        });
        var client = MakeClient(handler);
        var res = await client.ListAlertsAsync(status: "active");
        Assert.Equal(JsonValueKind.Object, res.ValueKind);
    }

    [Fact]
    public async Task GetAlert_GetsPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/alerts/al1", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"id\":\"al1\",\"status\":\"active\"}");
        });
        var client = MakeClient(handler);
        var res = await client.GetAlertAsync("al1");
        Assert.Equal("al1", res.GetProperty("id").GetString());
    }

    [Fact]
    public async Task ChangeAlertStatus_PostsBody()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/alerts/al1/status", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"id\":\"al1\",\"status\":\"resolved\"}");
        });
        var client = MakeClient(handler);
        var res = await client.ChangeAlertStatusAsync("al1", new ChangeStatusRequest { Status = "resolved" });
        Assert.Equal("resolved", res.GetProperty("status").GetString());
    }

    [Fact]
    public async Task SubscribeToAlert_PostsPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/alerts/al1/subscribe", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"subscribed\":true}");
        });
        var client = MakeClient(handler);
        var res = await client.SubscribeToAlertAsync("al1");
        Assert.True(res.GetProperty("subscribed").GetBoolean());
    }

    // ── Alert Configs ───────────────────────────────────────────────────────

    [Fact]
    public async Task ListAlertConfigs_GetsPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/alerts/configs", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"data\":[],\"total\":0}");
        });
        var client = MakeClient(handler);
        var res = await client.ListAlertConfigsAsync();
        Assert.Equal(JsonValueKind.Object, res.ValueKind);
    }

    [Fact]
    public async Task DeleteAlertConfig_DeletesPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Delete, req.Method);
            Assert.Equal("/alerts/configs/ac1", req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var client = MakeClient(handler);
        await client.DeleteAlertConfigAsync("ac1");
    }

    // ── Alert Preferences ───────────────────────────────────────────────────

    [Fact]
    public async Task ListOrganizationAlertPreferences_GetsPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/alerts/organization-preferences/list", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"preferences\":[],\"total\":0}");
        });
        var client = MakeClient(handler);
        var res = await client.ListOrganizationAlertPreferencesAsync();
        Assert.Equal(0, res.Total);
    }

    // ── Models & Alerts ─────────────────────────────────────────────────────

    [Fact]
    public async Task ListModelAlerts_ReturnsJsonElement()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/models/alerts", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"data\":[],\"total\":0}");
        });
        var client = MakeClient(handler);
        var res = await client.ListModelAlertsAsync();
        Assert.Equal(JsonValueKind.Object, res.ValueKind);
    }

    [Fact]
    public async Task MarkAllModelAlertsRead_PostsPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/models/alerts/mark-all-read", req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var client = MakeClient(handler);
        await client.MarkAllModelAlertsReadAsync();
    }

    [Fact]
    public async Task MarkModelAlertRead_PatchesPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal("PATCH", req.Method.Method);
            Assert.Equal("/models/alerts/ma1/read", req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var client = MakeClient(handler);
        await client.MarkModelAlertReadAsync("ma1");
    }

    [Fact]
    public async Task GetModelRecommendations_GetsPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/models/m1/recommendations", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"recommendations\":[]}");
        });
        var client = MakeClient(handler);
        var res = await client.GetModelRecommendationsAsync("m1");
        Assert.Equal(JsonValueKind.Object, res.ValueKind);
    }

    // ── Search ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Search_SetsQueryParams()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/search", req.RequestUri!.AbsolutePath);
            Assert.Contains("query=test", req.RequestUri!.Query);
            Assert.Contains("entity_type=agent", req.RequestUri!.Query);
            return JsonResponse("{\"results\":[]}");
        });
        var client = MakeClient(handler);
        var res = await client.SearchAsync(query: "test", entityType: "agent");
        Assert.Equal(JsonValueKind.Object, res.ValueKind);
    }

    // ── Top-Level AI Assistant ──────────────────────────────────────────────

    [Fact]
    public async Task SubmitAiFeedback_PostsBody()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/ai-assistant/feedback", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"feedback_id\":\"f1\",\"flagged\":false}");
        });
        var client = MakeClient(handler);
        var res = await client.SubmitAiFeedbackAsync(new AiAssistantFeedbackRequest { Feature = "chat", Rating = "positive" });
        Assert.Equal("f1", res.FeedbackId);
    }

    [Fact]
    public async Task AiAssistantKnowledgeBase_PostsBody()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/ai-assistant/knowledge-base", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"conversation_id\":\"c1\",\"note\":\"ok\",\"proposed_actions\":[]}");
        });
        var client = MakeClient(handler);
        var res = await client.AiAssistantKnowledgeBaseAsync(new AiAssistantGenerateRequest { UserInput = "create kb" });
        Assert.Equal("c1", res.ConversationId);
    }

    [Fact]
    public async Task DeclineAiAssistantPlan_PostsPath()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/ai-assistant/c1/decline", req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var client = MakeClient(handler);
        await client.DeclineAiAssistantPlanAsync("c1");
    }

    // ── High-Level Abstractions ─────────────────────────────────────────────

    [Fact]
    public async Task RunStreamingAgent_YieldsEvents()
    {
        var sse =
            "event: init\n" +
            "data: {\"run_id\":\"r1\",\"status\":\"processing\",\"attempts\":[],\"error_count\":0,\"priority\":false}\n\n" +
            "event: done\n" +
            "data: {\"run_id\":\"r1\",\"status\":\"completed\",\"output\":\"ok\",\"attempts\":[],\"error_count\":0,\"priority\":false}\n\n";

        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/agents/a1/runs/stream", req.RequestUri!.AbsolutePath);
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new System.IO.MemoryStream(Encoding.UTF8.GetBytes(sse)))
            };
            resp.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/event-stream");
            return resp;
        });

        var client = MakeClient(handler);
        var events = new List<AgentRunEvent>();
        await foreach (var evt in client.RunStreamingAgentAsync("a1", new AgentRunStreamRequest { Input = "hi" }))
        {
            events.Add(evt);
        }
        Assert.Equal(2, events.Count);
        Assert.Equal("init", events[0].Event);
        Assert.Equal("done", events[1].Event);
        Assert.Equal("ok", events[1].Run?.Output);
    }

    [Fact]
    public async Task RunAgentAndPoll_PollsUntilCompleted()
    {
        var callCount = 0;
        var handler = new FakeHttpMessageHandler(req =>
        {
            callCount++;
            if (req.Method == HttpMethod.Post && req.RequestUri!.AbsolutePath == "/agents/a1/runs")
            {
                return JsonResponse("{\"run_id\":\"r1\",\"status\":\"running\",\"attempts\":[],\"error_count\":0,\"priority\":false}");
            }
            // GET poll
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/agents/runs/r1", req.RequestUri!.AbsolutePath);
            return JsonResponse("{\"run_id\":\"r1\",\"status\":\"completed\",\"output\":\"done\",\"attempts\":[],\"error_count\":0,\"priority\":false}");
        });
        var client = MakeClient(handler);
        var res = await client.RunAgentAndPollAsync("a1", new AgentRunRequest { Input = "go" }, pollInterval: TimeSpan.FromMilliseconds(10));
        Assert.Equal("completed", res.Status);
        Assert.Equal("done", res.Output);
        Assert.True(callCount >= 2); // at least the initial POST + 1 GET
    }

    // ── IDisposable ───────────────────────────────────────────────────────

    [Fact]
    public async Task Dispose_DoesNotThrow_WhenUsingExternalHttpClient()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var http = new HttpClient(handler);
        var client = new SeclaiClient(new SeclaiClientOptions { ApiKey = "k", BaseUri = new Uri("https://example.invalid"), HttpClient = http });
        client.Dispose(); // should NOT dispose the external HttpClient
        // Prove the external HttpClient is still usable by making a request
        var resp = await http.GetAsync("https://example.invalid");
        Assert.Equal(System.Net.HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public void Dispose_DisposesOwnedHttpClient()
    {
        var client = new SeclaiClient(new SeclaiClientOptions { ApiKey = "k", BaseUri = new Uri("https://example.invalid") });
        client.Dispose(); // should dispose its own HttpClient without throwing
    }

    // ── DefaultHeaders ──────────────────────────────────────────────────────

    [Fact]
    public async Task DefaultHeaders_AreSentWithEveryRequest()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.True(req.Headers.TryGetValues("X-Custom", out var values));
            Assert.Contains("val", values);
            return JsonResponse("{\"data\":[],\"total\":0}");
        });
        var http = new HttpClient(handler);
        var client = new SeclaiClient(new SeclaiClientOptions
        {
            ApiKey = "k",
            BaseUri = new Uri("https://example.invalid"),
            HttpClient = http,
            DefaultHeaders = new Dictionary<string, string> { ["X-Custom"] = "val" }
        });
        await client.ListAgentsAsync();
    }

    // ── Timeout option ──────────────────────────────────────────────────────

    [Fact]
    public void Timeout_SetsDefaultTo120Seconds()
    {
        var opts = new SeclaiClientOptions();
        Assert.Equal(TimeSpan.FromSeconds(120), opts.Timeout);
    }

    // ── Stream-based uploads ────────────────────────────────────────────────

    [Fact]
    public async Task UploadFileToSource_Stream_PostsMultipart()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Contains("/upload", req.RequestUri!.AbsolutePath);
            Assert.Equal("multipart/form-data", req.Content!.Headers.ContentType!.MediaType);
            return JsonResponse("{\"source_connection_content_version_id\":\"cv1\",\"filename\":\"test.txt\"}");
        });
        var client = MakeClient(handler);
        using var stream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes("hello"));
        var res = await client.UploadFileToSourceAsync("sc1", stream, "test.txt");
        Assert.Equal("cv1", res.SourceConnectionContentVersionId);
    }

    [Fact]
    public async Task UploadFileToContent_Stream_PostsMultipart()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Contains("/upload", req.RequestUri!.AbsolutePath);
            Assert.Equal("multipart/form-data", req.Content!.Headers.ContentType!.MediaType);
            return JsonResponse("{\"source_connection_content_version_id\":\"cv1\",\"filename\":\"test.txt\"}");
        });
        var client = MakeClient(handler);
        using var stream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes("hello"));
        var res = await client.UploadFileToContentAsync("cv1", stream, "test.txt");
        Assert.Equal("cv1", res.SourceConnectionContentVersionId);
    }

    // ── RunAgentAndPollAsync timeout ────────────────────────────────────────

    [Fact]
    public async Task RunAgentAndPoll_TimesOut()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            // Always return "running" so it never finishes
            return JsonResponse("{\"run_id\":\"r1\",\"status\":\"running\",\"attempts\":[],\"error_count\":0,\"priority\":false}");
        });
        var client = MakeClient(handler);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.RunAgentAndPollAsync("a1",
                new AgentRunRequest { Input = "go" },
                pollInterval: TimeSpan.FromMilliseconds(10),
                timeout: TimeSpan.FromMilliseconds(50)));
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static SeclaiClient MakeClient(FakeHttpMessageHandler handler)
    {
        var http = new HttpClient(handler);
        return new SeclaiClient(new SeclaiClientOptions { ApiKey = "k", BaseUri = new Uri("https://example.invalid"), HttpClient = http });
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static string ExtractMultipartPartValue(string multipart, string name)
    {
        var markerQuoted = $"name=\"{name}\"";
        var markerUnquoted = $"name={name}";

        var start = multipart.IndexOf(markerQuoted, StringComparison.Ordinal);
        if (start < 0) start = multipart.IndexOf(markerUnquoted, StringComparison.Ordinal);
        if (start < 0) throw new InvalidOperationException($"Missing multipart field '{name}'.");

        start = multipart.IndexOf("\r\n\r\n", start, StringComparison.Ordinal);
        if (start < 0) throw new InvalidOperationException("Invalid multipart format: missing header separator.");
        start += 4;

        var end = multipart.IndexOf("\r\n--", start, StringComparison.Ordinal);
        if (end < 0) end = multipart.Length;

        return multipart.Substring(start, end - start).TrimEnd('\r', '\n');
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
