using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Seclai.Exceptions;
using Seclai.Models;
using Xunit;

namespace Seclai.Tests;

/// <summary>
/// Every version-gated list endpoint, against the default body and the one the API
/// sends once the request resolves to <c>Seclai-Version</c> 2026-07-27 or later.
/// </summary>
[Collection("Sequential")]
public sealed class GatedListTests
{
    // One item carrying every identifying field the item models read, so "x1" survives in each.
    private const string Item = "{\"id\":\"x1\",\"key\":\"x1\",\"agent_id\":\"x1\",\"agent_name\":\"n\",\"domain\":\"x1\",\"provider\":\"x1\",\"model_id\":\"x1\",\"model_type\":\"x1\",\"name\":\"n\",\"reason\":\"r\",\"created_at\":\"t\"}";

    private const string Flat = "\"total\":7,\"page\":2,\"limit\":5";

    // versioned_list_response / versioned_offset_list_response: a real page of a larger set.
    private const string Paged = "{\"page\":2,\"limit\":5,\"total\":7,\"pages\":2,\"has_next\":false,\"has_prev\":true}";

    // versioned_complete_list_response: one page spanning the whole list.
    private const string Complete = "{\"page\":1,\"limit\":1,\"total\":1,\"pages\":1,\"has_next\":false,\"has_prev\":false}";

    private const string EmailCaps = "\"can_add_vanity\":true,\"can_add_custom\":false,\"has_vanity\":false,\"has_custom\":false,\"vanity_plan_names\":[\"Pro\"],\"custom_plan_names\":[]";
    private const string EmbedderExtras = "\"default_model_type\":\"x1\",\"default_dimension\":1024,\"storage_credits\":[],\"file_processing_credits_per_mb\":1.5";
    private const string RerankerExtras = "\"default_model_type\":\"x1\",\"search_processing_credits\":0.1";

    /// <summary>What one call exposed. A null counter means the return type has no such member.</summary>
    private sealed record Seen(int Count, string First, int? Total = null, int? Page = null, int? Limit = null, bool Paged = false, PaginationResponse? Pagination = null, string? Extra = null);

    /// <param name="Raw">A JsonElement method documented as the wire body: not reshaped, so not held to the list contract.</param>
    private sealed record Call(string Name, Func<SeclaiClient, Task<Seen>> Run, bool Raw = false);

    private sealed record Row(string Route, string LegacyKey, string Legacy, string Canonical, bool CompletePage, int? LegacyTotal, bool LegacyHasPage, string? Extra, Call[] Calls);

    private static Seen Of<T>(IEnumerable<T>? items, int? total = null, int? page = null, int? limit = null, bool paged = false, PaginationResponse? pagination = null, string? extra = null)
    {
        var list = (items ?? Enumerable.Empty<T>()).ToList();
        return new Seen(list.Count, list.Count > 0 ? JsonSerializer.Serialize(list[0]) : string.Empty, total, page, limit, paged, pagination, extra);
    }

    private static Seen OfWire(JsonElement body) => new(1, body.GetRawText());

    private static Row Bare(string route, bool complete, params Call[] calls)
        => new(route, "data", "[" + Item + "]", Envelope(complete, null), complete, null, false, null, calls);

    private static Row Keyed(string route, string key, string? legacyRest, bool complete, string? extraJson, string? extra, params Call[] calls)
    {
        var tail = string.Join(",", new[] { legacyRest, extraJson }.Where(s => s is not null));
        var legacy = "{\"" + key + "\":[" + Item + "]" + (tail.Length > 0 ? "," + tail : string.Empty) + "}";
        var hasTotal = legacyRest is not null && legacyRest.Contains("\"total\"");
        var hasPage = legacyRest is not null && legacyRest.Contains("\"page\"");
        return new Row(route, key, legacy, Envelope(complete, extraJson), complete, hasTotal ? 7 : null, hasPage, extra, calls);
    }

    // `provider` is an object on an email domain, so the shared item does not fit.
    private const string EmailDomain = "{\"id\":\"x1\",\"domain\":\"x1\",\"kind\":\"custom\",\"status\":\"verified\"}";

    private static Row WithItem(string item, Row row)
        => row with { Legacy = row.Legacy.Replace(Item, item), Canonical = row.Canonical.Replace(Item, item) };

    private static string Envelope(bool complete, string? extraJson)
        => "{\"data\":[" + Item + "],\"pagination\":" + (complete ? Complete : Paged) + (extraJson is null ? string.Empty : "," + extraJson) + "}";

    // Legacy bodies follow each call site's `legacy=` value; canonical bodies follow
    // `_canonical` in the backend's routers/list_response.py, with the same `**extra`.
    private static readonly Row[] Rows =
    {
        Keyed("GET /agents/agent-email-optouts", "items", "\"total\":7", false, null, null,
            new Call("ListAgentEmailOptOutsAsync", async c => { var r = await c.ListAgentEmailOptOutsAsync(); return Of(r.Items, r.Total, paged: true, pagination: r.Pagination); })),
        Keyed("GET /agents/blocked-email-senders", "items", "\"total\":7", false, "\"auto_block_mode\":\"input\"", "input",
            new Call("ListBlockedEmailSendersAsync", async c => { var r = await c.ListBlockedEmailSendersAsync(); return Of(r.Items, r.Total, paged: true, pagination: r.Pagination, extra: r.AutoBlockMode); })),
        Keyed("PUT /agents/blocked-email-senders/mode", "items", "\"total\":7", true, "\"auto_block_mode\":\"input\"", "input",
            new Call("SetAutoBlockModeAsync", async c => { var r = await c.SetAutoBlockModeAsync(new SetAutoBlockModeRequest { Mode = "input" }); return Of(r.Items, r.Total, paged: true, pagination: r.Pagination, extra: r.AutoBlockMode); })),
        Keyed("GET /agents/evaluation-criteria/{criteria_id}/compatible-runs", "data", Flat, false, null, null,
            new Call("ListCompatibleRunsAsync", async c => { var r = await c.ListCompatibleRunsAsync("c1"); return Of(r.Data, r.Total, r.Page, r.Limit, true, r.Pagination); })),
        Keyed("GET /agents/evaluation-criteria/{criteria_id}/results", "data", Flat, false, null, null,
            new Call("ListEvaluationResultsAsync", async c => { var r = await c.ListEvaluationResultsAsync("c1"); return Of(r.Data, r.Total, r.Page, r.Limit, true, r.Pagination); })),
        Bare("GET /agents/inbound-email-rejections", false,
            new Call("ListInboundEmailRejectionsAsync", async c => Of(await c.ListInboundEmailRejectionsAsync()))),
        Bare("GET /agents/{agent_id}/callers", true,
            new Call("GetAgentCallersAsync", async c => Of(await c.GetAgentCallersAsync("a1")))),
        Bare("GET /agents/{agent_id}/evaluation-criteria", false,
            new Call("ListEvaluationCriteriaAsync", async c => Of(await c.ListEvaluationCriteriaAsync("a1"))),
            new Call("ListEvaluationCriteriaPageAsync", async c => { var r = await c.ListEvaluationCriteriaPageAsync("a1"); return Of(r.Data, paged: true, pagination: r.Pagination); })),
        Keyed("GET /agents/{agent_id}/evaluation-results", "data", Flat, false, null, null,
            new Call("ListAgentEvaluationResultsAsync", async c => { var r = await c.ListAgentEvaluationResultsAsync("a1"); return Of(r.Data, r.Total, r.Page, r.Limit, true, r.Pagination); })),
        Keyed("GET /agents/{agent_id}/evaluation-runs", "data", Flat, false, null, null,
            new Call("ListEvaluationRunsAsync", async c => { var r = await c.ListEvaluationRunsAsync("a1"); return Of(r.Data, r.Total, r.Page, r.Limit, true, r.Pagination); })),
        Bare("GET /agents/{agent_id}/runs/{run_id}/evaluation-results", false,
            new Call("ListRunEvaluationResultsAsync", async c => { var r = await c.ListRunEvaluationResultsAsync("a1", "r1"); return Of(r.Data, r.Total, paged: true, pagination: r.Pagination); })),
        Keyed("GET /alerts/configs", "configs", "\"total\":7", false, null, null,
            new Call("ListAlertConfigsAsync", async c => OfWire(await c.ListAlertConfigsAsync()), Raw: true),
            new Call("Typed.ListAlertConfigsAsync", async c => { var r = await c.Typed.ListAlertConfigsAsync(); return Of(r.Items, r.Total, paged: true, pagination: r.Pagination); })),
        Keyed("GET /alerts/organization-preferences/list", "preferences", "\"total\":7", true, null, null,
            new Call("ListOrganizationAlertPreferencesAsync", async c => { var r = await c.ListOrganizationAlertPreferencesAsync(); return Of(r.Preferences, paged: true, pagination: r.Pagination); })),
        Bare("GET /cloud-drives", true,
            new Call("ListCloudDrivesAsync", async c => Of(await c.ListCloudDrivesAsync()))),
        Bare("GET /cloud-drives/providers", true,
            new Call("ListCloudDriveProvidersAsync", async c => Of(await c.ListCloudDriveProvidersAsync()))),
        Bare("GET /cloud-drives/{connection_id}/agents", true,
            new Call("GetAgentsUsingCloudDriveAsync", async c => Of(await c.GetAgentsUsingCloudDriveAsync("cd1")))),
        Bare("GET /cloud-drives/{connection_id}/rejections", false,
            new Call("ListCloudDriveRejectionsAsync", async c => Of(await c.ListCloudDriveRejectionsAsync("cd1")))),
        WithItem(EmailDomain, Keyed("GET /email-domains", "domains", null, true, EmailCaps, "True",
            new Call("ListEmailDomainsAsync", async c => { var r = await c.ListEmailDomainsAsync(); return Of(r.Domains, paged: true, pagination: r.Pagination, extra: r.CanAddVanity.ToString()); }))),
        Bare("GET /governance/ai-assistant/conversations", false,
            new Call("ListGovernanceAiConversationsAsync", async c => Of(await c.ListGovernanceAiConversationsAsync()))),
        Keyed("GET /knowledge_bases", "knowledge_bases", Flat, false, null, null,
            new Call("ListKnowledgeBasesAsync", async c => { var r = await c.ListKnowledgeBasesAsync(); return Of(r.Data, r.Total, r.Page, r.Limit, true, r.Pagination); })),
        Keyed("GET /memory_banks", "memory_banks", Flat, false, null, null,
            new Call("ListMemoryBanksAsync", async c => { var r = await c.ListMemoryBanksAsync(); return Of(r.Data, r.Total, r.Page, r.Limit, true, r.Pagination); })),
        Bare("GET /memory_banks/templates", true,
            new Call("ListMemoryBankTemplatesAsync", async c => OfWire(await c.ListMemoryBankTemplatesAsync()), Raw: true),
            new Call("Typed.ListMemoryBankTemplatesAsync", async c => Of(await c.Typed.ListMemoryBankTemplatesAsync()))),
        Bare("GET /memory_banks/{memory_bank_id}/agents", true,
            new Call("GetAgentsUsingMemoryBankAsync", async c => OfWire(await c.GetAgentsUsingMemoryBankAsync("mb1")), Raw: true),
            new Call("Typed.GetAgentsUsingMemoryBankAsync", async c => Of(await c.Typed.GetAgentsUsingMemoryBankAsync("mb1")))),
        Bare("GET /models", true,
            new Call("ListModelsAsync", async c => OfWire(await c.ListModelsAsync()), Raw: true),
            new Call("Typed.ListModelsAsync", async c => Of(await c.Typed.ListModelsAsync()))),
        Keyed("GET /models/alerts", "alerts", "\"total\":7", false, null, null,
            new Call("ListModelAlertsAsync", async c => OfWire(await c.ListModelAlertsAsync()), Raw: true),
            new Call("Typed.ListModelAlertsAsync", async c => { var r = await c.Typed.ListModelAlertsAsync(); return Of(r.Items, r.Total, paged: true, pagination: r.Pagination); })),
        Keyed("GET /models/embedders", "models", null, true, EmbedderExtras, "x1",
            new Call("ListEmbeddingModelsAsync", async c => { var r = await c.ListEmbeddingModelsAsync(); return Of(r.Items, paged: true, pagination: r.Pagination, extra: r.DefaultModelType); })),
        Keyed("GET /models/generation-tiers", "tiers", null, true, null, null,
            new Call("GetGenerationTiersAsync", async c => OfWire(await c.GetGenerationTiersAsync()), Raw: true),
            new Call("Typed.GetGenerationTiersAsync", async c => { var r = await c.Typed.GetGenerationTiersAsync(); return Of(r.Tiers, paged: true, pagination: r.Pagination); })),
        Keyed("GET /models/playground/experiments", "experiments", "\"total\":7", false, null, null,
            new Call("ListExperimentsAsync", async c => OfWire(await c.ListExperimentsAsync()), Raw: true),
            new Call("Typed.ListExperimentsAsync", async c => { var r = await c.Typed.ListExperimentsAsync(); return Of(r.Experiments, r.Total, paged: true, pagination: r.Pagination); })),
        Keyed("GET /models/rerankers", "models", null, true, RerankerExtras, "x1",
            new Call("ListRerankerModelsAsync", async c => { var r = await c.ListRerankerModelsAsync(); return Of(r.Items, paged: true, pagination: r.Pagination, extra: r.DefaultModelType); })),
        Bare("GET /solutions/{solution_id}/conversations", true,
            new Call("ListSolutionConversationsAsync", async c => Of(await c.ListSolutionConversationsAsync("s1")))),
    };

    public static IEnumerable<object[]> Cases()
        => Rows.SelectMany(row => row.Calls.Select(call => new object[] { row.Route, call.Name }));

    private static (Row Row, Call Call) Find(string route, string name)
    {
        var row = Rows.Single(r => r.Route == route);
        return (row, row.Calls.Single(c => c.Name == name));
    }

    private static SeclaiClient ClientReturning(string body, Action<HttpRequestMessage>? inspect = null)
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            inspect?.Invoke(req);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        });
        return new SeclaiClient(new SeclaiClientOptions { ApiKey = "k", BaseUri = new Uri("https://example.invalid"), HttpClient = new HttpClient(handler) });
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task ReturnsTheItemsOnBothShapes(string route, string name)
    {
        var (row, call) = Find(route, name);
        var verb = route.Split(' ')[0];

        var legacy = await call.Run(ClientReturning(row.Legacy, req => Assert.Equal(verb, req.Method.Method)));
        if (call.Raw)
        {
            Assert.Equal(row.Legacy, legacy.First);
            Assert.Equal(row.Canonical, (await call.Run(ClientReturning(row.Canonical))).First);
            return;
        }
        Assert.Equal(1, legacy.Count);
        Assert.Contains("x1", legacy.First);
        Assert.Null(legacy.Pagination);
        if (legacy.Total is not null)
        {
            // A bare array states no counts, so the counters keep their zero.
            Assert.Equal(row.LegacyTotal ?? 0, legacy.Total);
        }
        if (legacy.Page is not null && row.LegacyHasPage)
        {
            Assert.Equal(2, legacy.Page);
            Assert.Equal(5, legacy.Limit);
        }
        Assert.Equal(row.Extra, legacy.Extra);

        var canonical = await call.Run(ClientReturning(row.Canonical));
        Assert.Equal(1, canonical.Count);
        Assert.Contains("x1", canonical.First);
        var total = row.CompletePage ? 1 : 7;
        if (canonical.Total is not null) Assert.Equal(total, canonical.Total);
        if (canonical.Page is not null)
        {
            Assert.Equal(row.CompletePage ? 1 : 2, canonical.Page);
            Assert.Equal(row.CompletePage ? 1 : 5, canonical.Limit);
        }
        if (canonical.Paged)
        {
            Assert.NotNull(canonical.Pagination);
            Assert.Equal(total, canonical.Pagination!.Total);
            Assert.Equal(!row.CompletePage, canonical.Pagination.HasPrev);
        }
        Assert.Equal(row.Extra, canonical.Extra);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task ThrowsApiExceptionWhenTheBodyIsNotAList(string route, string name)
    {
        var (row, call) = Find(route, name);
        if (call.Raw) return;

        var bodies = new[]
        {
            "{\"detail\":\"Internal error\"}",
            "\"oops\"",
            "null",
            "not json",
            string.Empty,
            "{\"data\":\"oops\"}",
            "{\"" + row.LegacyKey + "\":{\"id\":\"x1\"}}",
        };
        foreach (var body in bodies)
        {
            var ex = await Assert.ThrowsAsync<ApiException>(() => call.Run(ClientReturning(body)));
            Assert.Equal(route.Split(' ')[0], ex.Method);
            Assert.Contains("expected a list", ex.Message);
            Assert.Equal(body, ex.ResponseBody);
            Assert.Equal("example.invalid", ex.Url.Host);
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task ReadsAnExplicitNullDataAsAnEmptyList(string route, string name)
    {
        var (row, call) = Find(route, name);
        if (call.Raw) return;

        var seen = await call.Run(ClientReturning("{\"data\":null,\"pagination\":" + (row.CompletePage ? Complete : Paged) + "}"));
        Assert.Equal(0, seen.Count);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task PrefersDataThenTheLegacyKeyThenANullData(string route, string name)
    {
        var (row, call) = Find(route, name);
        if (call.Raw || row.LegacyKey == "data") return;

        // `data` is not an array, so the endpoint's own key is read before a null `data` counts as empty.
        foreach (var data in new[] { "\"oops\"", "null" })
        {
            var seen = await call.Run(ClientReturning("{\"data\":" + data + "," + row.Legacy.Substring(1)));
            Assert.Equal(1, seen.Count);
        }

        // Both are arrays: `data` wins.
        var both = await call.Run(ClientReturning("{\"data\":[]," + row.Legacy.Substring(1)));
        Assert.Equal(0, both.Count);
    }

    [Fact]
    public async Task AFlatCounterSurvivesAPaginationObjectThatOmitsIt()
    {
        const string body = "{\"data\":[" + Item + "],\"total\":5,\"pagination\":{\"page\":1,\"limit\":20}}";

        var optOuts = await ClientReturning(body).ListAgentEmailOptOutsAsync();
        Assert.Equal(5, optOuts.Total);

        var results = await ClientReturning(body).ListEvaluationResultsAsync("c1");
        Assert.Equal(5, results.Total);
        Assert.Equal(1, results.Page);
        Assert.Equal(20, results.Limit);

        var banks = await ClientReturning(body).ListMemoryBanksAsync();
        Assert.Equal(5, banks.Total);
        Assert.Equal(20, banks.Limit);

        var configs = await ClientReturning(body).Typed.ListAlertConfigsAsync();
        Assert.Equal(5, configs.Total);
    }

    [Fact]
    public async Task DualKeyModelsFillOnlyTheKeyThatArrived()
    {
        const string paging = ",\"pagination\":" + Paged + "}";
        const string items = "[" + Item + "]";

        var configs = await ClientReturning("{\"configs\":" + items + ",\"total\":7}").Typed.ListAlertConfigsAsync();
        Assert.Single(configs.Configs);
        Assert.Empty(configs.Data);
        Assert.Single(configs.Items);
        Assert.Null(configs.Pagination);
        configs = await ClientReturning("{\"data\":" + items + paging).Typed.ListAlertConfigsAsync();
        Assert.Empty(configs.Configs);
        Assert.Single(configs.Data);
        Assert.Single(configs.Items);
        Assert.Equal(7, configs.Total);

        var alerts = await ClientReturning("{\"alerts\":" + items + ",\"total\":7}").Typed.ListModelAlertsAsync();
        Assert.Single(alerts.Alerts);
        Assert.Empty(alerts.Data);
        alerts = await ClientReturning("{\"data\":" + items + paging).Typed.ListModelAlertsAsync();
        Assert.Empty(alerts.Alerts);
        Assert.Single(alerts.Data);
        Assert.Equal(7, alerts.Total);

        var embedders = await ClientReturning("{\"models\":" + items + "," + EmbedderExtras + "}").ListEmbeddingModelsAsync();
        Assert.Single(embedders.Models);
        Assert.Empty(embedders.Data);
        embedders = await ClientReturning("{\"data\":" + items + "," + EmbedderExtras + paging).ListEmbeddingModelsAsync();
        Assert.Empty(embedders.Models);
        Assert.Single(embedders.Data);

        var rerankers = await ClientReturning("{\"models\":" + items + "," + RerankerExtras + "}").ListRerankerModelsAsync();
        Assert.Single(rerankers.Models);
        Assert.Empty(rerankers.Data);
        rerankers = await ClientReturning("{\"data\":" + items + "," + RerankerExtras + paging).ListRerankerModelsAsync();
        Assert.Empty(rerankers.Models);
        Assert.Single(rerankers.Data);
    }

    [Fact]
    public async Task RunEvaluationResults_DefaultShapeStatesNoCounters()
    {
        var res = await ClientReturning("[" + Item + "," + Item + "]").ListRunEvaluationResultsAsync("a1", "r1");
        Assert.Equal(2, res.Data!.Count);
        Assert.Equal(0, res.Total);
        Assert.Equal(0, res.Page);
        Assert.Equal(0, res.Limit);
    }

    [Fact]
    public async Task NotAListReportsTheRequestUrlWithItsQueryAndTheBody()
    {
        const string body = "{\"detail\":\"x\"}";
        var ex = await Assert.ThrowsAsync<ApiException>(() => ClientReturning(body).ListKnowledgeBasesAsync(page: 3, limit: 7));
        Assert.Equal("https://example.invalid/knowledge_bases?page=3&limit=7", ex.Url.ToString());
        Assert.Equal(body, ex.ResponseBody);

        ex = await Assert.ThrowsAsync<ApiException>(() => ClientReturning(body).Typed.ListAlertConfigsAsync(page: 3, limit: 7));
        Assert.Equal("https://example.invalid/alerts/configs?page=3&limit=7", ex.Url.ToString());
        Assert.Equal(body, ex.ResponseBody);
    }

    [Fact]
    public async Task TypedListMethods_ReportANonJsonBodyAsReceived()
    {
        const string body = "<html>Bad gateway</html>";
        var calls = new Func<SeclaiClient, Task>[]
        {
            c => c.Typed.ListAlertConfigsAsync(),
            c => c.Typed.ListModelAlertsAsync(),
            c => c.Typed.ListModelsAsync(provider: "openai"),
            c => c.Typed.ListExperimentsAsync(),
            c => c.Typed.GetGenerationTiersAsync(),
            c => c.Typed.ListMemoryBankTemplatesAsync(),
            c => c.Typed.GetAgentsUsingMemoryBankAsync("mb1"),
        };
        foreach (var call in calls)
        {
            var ex = await Assert.ThrowsAsync<ApiException>(() => call(ClientReturning(body)));
            Assert.Equal(body, ex.ResponseBody);
            Assert.Contains(body, ex.Message);
            Assert.DoesNotContain("was empty", ex.Message);
        }

        var models = await Assert.ThrowsAsync<ApiException>(() => ClientReturning(body).Typed.ListModelsAsync(provider: "openai"));
        Assert.Equal("https://example.invalid/models?provider=openai", models.Url.ToString());
    }

    [Fact]
    public async Task RawListMethods_ReturnAnEmptyBodyAsBefore()
    {
        // 1.5.0 returned an undefined JsonElement for an empty 200; the raw methods are untouched.
        Assert.Equal(JsonValueKind.Undefined, (await ClientReturning(string.Empty).ListMemoryBankTemplatesAsync()).ValueKind);
        Assert.Equal(JsonValueKind.Undefined, (await ClientReturning(string.Empty).GetAgentsUsingMemoryBankAsync("mb1")).ValueKind);
    }

    private sealed class YieldingHandler : HttpMessageHandler
    {
        private readonly string _body;

        public int Requests { get; private set; }

        public YieldingHandler(string body) => _body = body;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            Requests++;
            await Task.Delay(5, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(_body, Encoding.UTF8, "application/json") };
        }
    }

    private static SeclaiClient ClientWithTokenProvider(HttpMessageHandler handler, Func<Task<string>> token)
        => new(new SeclaiClientOptions
        {
            BaseUri = new Uri("https://example.invalid"),
            HttpClient = new HttpClient(handler),
            AccessTokenProvider = _ => token(),
        });

    // One of each kind: keyed page, bare list, Typed list, and a method that is not a list at all.
    private static readonly Func<SeclaiClient, Task>[] OneOfEachKind =
    {
        c => c.ListKnowledgeBasesAsync(page: 3, limit: 7),
        c => c.GetAgentCallersAsync("a1"),
        c => c.Typed.ListExperimentsAsync(),
        c => c.GetAgentAsync("a1"),
    };

    [Fact]
    public async Task AJsonExceptionFromTheTokenProviderPropagatesAndNothingIsSent()
    {
        foreach (var call in OneOfEachKind)
        {
            var handler = new YieldingHandler("[]");
            var thrown = new JsonException("token endpoint returned HTML");
            var client = ClientWithTokenProvider(handler, () => throw thrown);

            var ex = await Assert.ThrowsAsync<JsonException>(() => call(client));
            Assert.Same(thrown, ex);
            Assert.Equal(0, handler.Requests);
        }
    }

    [Fact]
    public async Task AJsonExceptionFromTheTokenProviderCarriesNothingFromAnotherCall()
    {
        foreach (var call in OneOfEachKind)
        {
            var other = ClientReturning("{\"detail\":\"from the other client\"}");
            var handler = new YieldingHandler("[]");
            var thrown = new JsonException("token endpoint returned HTML");
            var client = ClientWithTokenProvider(handler, async () =>
            {
                await other.GetApiVersionAsync();
                throw thrown;
            });

            var ex = await Assert.ThrowsAsync<JsonException>(() => call(client));
            Assert.Same(thrown, ex);
            Assert.DoesNotContain("other client", ex.ToString());
            Assert.Equal(0, handler.Requests);
        }
    }

    [Fact]
    public async Task NotAListReportsUrlAndBodyWhenExecutionContextFlowIsSuppressed()
    {
        const string body = "{\"detail\":\"x\"}";
        var client = new SeclaiClient(new SeclaiClientOptions { ApiKey = "k", BaseUri = new Uri("https://example.invalid"), HttpClient = new HttpClient(new YieldingHandler(body)) });

        Task plain;
        Task typed;
        using (System.Threading.ExecutionContext.SuppressFlow())
        {
            plain = client.ListKnowledgeBasesAsync(page: 3, limit: 7);
            typed = client.Typed.ListAlertConfigsAsync(page: 3, limit: 7);
        }

        var ex = await Assert.ThrowsAsync<ApiException>(() => plain);
        Assert.Equal("https://example.invalid/knowledge_bases?page=3&limit=7", ex.Url.ToString());
        Assert.Equal(body, ex.ResponseBody);

        ex = await Assert.ThrowsAsync<ApiException>(() => typed);
        Assert.Equal("https://example.invalid/alerts/configs?page=3&limit=7", ex.Url.ToString());
        Assert.Equal(body, ex.ResponseBody);
    }

    // The seven raw list methods and their Typed forms share one internal request definition each.
    private static readonly (string Path, Func<SeclaiClient, Task> Raw, Func<SeclaiClient, Task> Typed)[] RawAndTyped =
    {
        ("/alerts/configs", c => c.ListAlertConfigsAsync(page: 2, limit: 5), c => c.Typed.ListAlertConfigsAsync(page: 2, limit: 5)),
        ("/models/alerts", c => c.ListModelAlertsAsync(page: 2, limit: 5), c => c.Typed.ListModelAlertsAsync(page: 2, limit: 5)),
        ("/models", c => c.ListModelsAsync("openai", true, false), c => c.Typed.ListModelsAsync("openai", true, false)),
        ("/models/playground/experiments", c => c.ListExperimentsAsync(7, "2026-01-01", "2026-02-01", 5, 10), c => c.Typed.ListExperimentsAsync(7, "2026-01-01", "2026-02-01", 5, 10)),
        ("/models/generation-tiers", c => c.GetGenerationTiersAsync(), c => c.Typed.GetGenerationTiersAsync()),
        ("/memory_banks/templates", c => c.ListMemoryBankTemplatesAsync(), c => c.Typed.ListMemoryBankTemplatesAsync()),
        ("/memory_banks/{memory_bank_id}/agents", c => c.GetAgentsUsingMemoryBankAsync("mb1"), c => c.Typed.GetAgentsUsingMemoryBankAsync("mb1")),
    };

    [Fact]
    public async Task TypedListFormsIssueTheSameRequestAsTheirRawMethod()
    {
        foreach (var (_, raw, typed) in RawAndTyped)
        {
            var seen = new List<string>();
            var client = ClientReturning("[]", req => seen.Add($"{req.Method} {req.RequestUri!.PathAndQuery}"));
            await raw(client);
            await typed(client);
            Assert.Equal(2, seen.Count);
            Assert.Equal(seen[0], seen[1]);
        }
    }

    [Fact]
    public async Task RawListMethodsSendOnlyQueryParametersTheSpecDeclares()
    {
        // sdksync.py `params` reads a request only from a public method's own body, and these
        // seven now define theirs in an internal method. This is the same check for them.
        var specPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "..", "seclai-python", "openapi", "seclai.openapi.json");
        if (!File.Exists(specPath)) return;   // spec is not bundled in this repo
        using var spec = JsonDocument.Parse(File.ReadAllText(specPath));
        var paths = spec.RootElement.GetProperty("paths");

        foreach (var (path, raw, _) in RawAndTyped)
        {
            var declared = new List<string>();
            if (paths.GetProperty(path).GetProperty("get").TryGetProperty("parameters", out var parameters))
            {
                declared.AddRange(parameters.EnumerateArray()
                    .Where(p => p.TryGetProperty("in", out var where) && where.GetString() == "query")
                    .Select(p => p.GetProperty("name").GetString()!));
            }

            var sent = new List<string>();
            await raw(ClientReturning("[]", req => sent.AddRange(
                req.RequestUri!.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries).Select(pair => pair.Split('=')[0]))));
            Assert.All(sent, key => Assert.Contains(key, declared));
        }
    }

    [Fact]
    public void EveryGatedEndpointHasARow()
    {
        // GatedListEndpoints.txt is generated from the backend; see gated_list_endpoints.py.
        var path = Path.Combine(AppContext.BaseDirectory, "GatedListEndpoints.txt");
        var gated = File.ReadAllLines(path)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith("#", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(gated);
        Assert.Equal(gated.OrderBy(r => r, StringComparer.Ordinal), Rows.Select(r => r.Route).OrderBy(r => r, StringComparer.Ordinal));
    }
}
