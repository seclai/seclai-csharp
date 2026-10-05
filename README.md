# Seclai C# SDK

Official Seclai C# SDK for .NET, targeting **netstandard2.0** (compatible with .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+).

Provides strongly-typed async methods for the entire Seclai API: agents, knowledge bases, memory banks, sources, content, evaluations, solutions, governance, alerts, search, AI assistants, and more.

## Install

```bash
dotnet add package Seclai.Sdk
```

## Quick Start

```csharp
using Seclai;
using Seclai.Models;

var client = new SeclaiClient(new SeclaiClientOptions
{
    ApiKey = Environment.GetEnvironmentVariable("SECLAI_API_KEY"),
    // BaseUri defaults to https://seclai.com
});

// Run an agent
var run = await client.RunAgentAsync("sc_ag_123", new AgentRunRequest { Input = "hello" });
Console.WriteLine($"Run {run.RunId}: {run.Status}");
```

## Configuration

| Option | Default | Description |
|--------|---------|-------------|
| `ApiKey` | `SECLAI_API_KEY` env var | API key for `x-api-key` header authentication. |
| `AccessToken` | — | Static OAuth2 bearer token. |
| `AccessTokenProvider` | — | Async function called per request: `Func<CancellationToken, Task<string>>`. |
| `Profile` | `SECLAI_PROFILE` / `"default"` | SSO profile name from `~/.seclai/config`. |
| `ConfigDir` | `SECLAI_CONFIG_DIR` / `~/.seclai` | Config/cache directory path. |
| `AutoRefresh` | `true` | Auto-refresh expired SSO tokens using the cached refresh token. |
| `AccountId` | — | Account ID sent as `X-Account-Id` header. |
| `BaseUri` | `https://seclai.com` | API base URL. Falls back to `SECLAI_API_URL` env var. |
| `HttpClient` | internal | Bring your own `HttpClient` (useful for DI or testing). Its `DefaultRequestHeaders` are the lowest header layer — see [Request headers](#request-headers). |
| `Timeout` | 120 s | HTTP request timeout. Ignored when a custom `HttpClient` is provided. |
| `DefaultHeaders` | none | Extra headers sent with every request. An entry replaces the supplied `HttpClient`'s default for the same header; the SDK's own headers replace both. |

`SeclaiClient` implements `IDisposable`. When you let it create its own `HttpClient`, wrap it in a `using` statement so the client is disposed properly:

```csharp
using var client = new SeclaiClient(new SeclaiClientOptions { ApiKey = "sk_..." });
```

If you supply your own `HttpClient`, the client does **not** dispose it — you manage its lifetime.

### Request headers

Exactly one value is sent for every header the SDK sets and for every request
header named in `DefaultHeaders`. A content header named there, such as
`Content-Type`, is not a request header in .NET and is ignored. A header that
only the supplied `HttpClient` carries is sent as that client holds it, with
every value it has. From lowest to highest, where a higher layer replaces a
lower one:

1. the supplied `HttpClient`'s `DefaultRequestHeaders`
2. `DefaultHeaders`
3. the headers the SDK sets from its own options: `Accept`, `Seclai-Version`
   (from `ApiVersion`), the API-key header (from `ApiKey`), `Authorization` (from
   `AccessToken`, `AccessTokenProvider` or SSO) and `X-Account-Id` (from
   `AccountId`)

So the credential and the account a request runs as always come from the options
that exist for them, never from a default header of the same name. A header the
SDK is not setting passes through from the lower layers: with `ApiKey`
authentication a default `Authorization` is still sent, and with `AccountId`
unset a default `X-Account-Id` is too. `Seclai-Version` is the one exception in
layer 3: a `DefaultHeaders` entry for it takes precedence over `ApiVersion`.

### Authentication

Credentials are resolved via a chain (first match wins):

1. Explicit `ApiKey` option
2. Explicit `AccessToken` option (static string)
3. Explicit `AccessTokenProvider` option (async delegate, called per request)
4. `SECLAI_API_KEY` environment variable
5. SSO — cached tokens from `~/.seclai/sso/cache/` (always available as fallback)

```csharp
// API key
using var client = new SeclaiClient(new SeclaiClientOptions { ApiKey = "sk-..." });

// Static bearer token
using var client = new SeclaiClient(new SeclaiClientOptions { AccessToken = "eyJhbGciOi..." });

// Dynamic bearer token provider (called per request)
using var client = new SeclaiClient(new SeclaiClientOptions
{
    AccessTokenProvider = async ct => await GetTokenFromVaultAsync(ct)
});

// SSO profile (uses cached tokens, auto-refreshes)
using var client = new SeclaiClient(new SeclaiClientOptions { Profile = "my-profile" });

// Environment variable
// set SECLAI_API_KEY=sk-...
using var client = new SeclaiClient(new SeclaiClientOptions());
```

#### SSO authentication

SSO is the default fallback when no explicit credentials are provided. The SDK
includes built-in production SSO defaults, so no configuration is needed:

```bash
npx @seclai/cli auth login    # authenticate via browser — works immediately
```

To customize SSO settings (e.g. for a staging environment), use `seclai configure sso`
or set environment variables:

| Variable | Description | Default |
|---|---|---|
| `SECLAI_SSO_DOMAIN` | Cognito domain | `auth.seclai.com` |
| `SECLAI_SSO_CLIENT_ID` | Cognito app client ID | `4bgf8v9qmc5puivbaqon9n5lmr` |
| `SECLAI_SSO_REGION` | AWS region | `us-west-2` |

## API versioning

The API dates its backward-incompatible changes. Nothing changes for you until
you opt in, either per client or by pinning the account:

```csharp
var client = new SeclaiClient(new SeclaiClientOptions
{
    ApiKey = "...",
    ApiVersion = SeclaiApiVersion.V2026_07_27,   // sent as the Seclai-Version header
});

var state = await client.GetApiVersionAsync();   // what this request resolved to
await client.UpdateApiVersionAsync(SeclaiApiVersion.V2026_07_27); // pin the whole account
```

Leave `ApiVersion` unset and the header is omitted, so the account's pinned
baseline applies and responses keep their current shapes. Upgrading this package
alone never changes the wire contract.

Known versions are on `SeclaiApiVersion` (`V2026_07_01` through `V2026_10_03`,
plus `Default`, `Latest` and `Known`). A version this release was **not** built
against is rejected with `ConfigurationException`: a newer version can reshape
responses, and this client would decode them incorrectly rather than reject
them. Upgrade the package to adopt a new version, or set
`AllowUnknownApiVersion` if you have to move first and accept that risk.

The guard covers every way a `Seclai-Version` can reach the wire: `ApiVersion`,
`DefaultHeaders`, and the `DefaultRequestHeaders` of an `HttpClient` you supply.
The first two are checked at construction. An `HttpClient`'s defaults can change
afterwards, so they are checked at construction and again on every request,
after the access token has been fetched and immediately before the request is
handed to the `HttpClient`: an unknown or empty value throws before anything is
sent. Two different values there throw even with `AllowUnknownApiVersion`,
since the client cannot tell which one the server would use; the same value
twice is sent once.

That check cannot be airtight. `HttpClient` adds its defaults when it sends, so
another thread that changes a shared `HttpClient`'s `Seclai-Version` between the
check and the send is not seen. Set the version through `ApiVersion`, which the
client writes onto each request itself, if the `HttpClient` is shared.

The guard only covers the header. An account pinned server-side can still be
newer than this release — `GetApiVersionAsync().EffectiveVersion` is what the
request actually resolved to, and comparing it against `SeclaiApiVersion.Latest`
is how you detect the gap.

**What `2026-07-27` changes.** Undeclared query parameters become a 422 instead
of being ignored, and 30 list endpoints change shape: by default each answers
with a bare array or an object keyed per resource (`items`, `domains`,
`configs`, …), and from `2026-07-27` with `{data, pagination}` plus the same
extra fields. Every method for those endpoints reads both shapes and returns the
items where it always has:

| Returns | Methods | Paging metadata |
| --- | --- | --- |
| `List<T>` | `ListEvaluationCriteriaAsync`, `GetAgentCallersAsync`, `ListInboundEmailRejectionsAsync`, `ListGovernanceAiConversationsAsync`, `ListSolutionConversationsAsync`, `Typed.ListModelsAsync`, `Typed.ListMemoryBankTemplatesAsync`, `Typed.GetAgentsUsingMemoryBankAsync`, the four cloud-drive listings | not exposed |
| A model with `Data` | `ListEvaluationCriteriaPageAsync`, `ListRunEvaluationResultsAsync`, `ListAgentEvaluationResultsAsync`, `ListEvaluationResultsAsync`, `ListEvaluationRunsAsync`, `ListCompatibleRunsAsync`, `ListKnowledgeBasesAsync`, `ListMemoryBanksAsync` | `Total`, `Page` and `Limit` where the model has them, filled from either shape that states them — the default bare array of `ListRunEvaluationResultsAsync` states none, so its counters are 0 there; `Pagination` once opted in |
| A model with its own list property | `ListAgentEmailOptOutsAsync`, `ListBlockedEmailSendersAsync` and `SetAutoBlockModeAsync` (`Items`), `ListOrganizationAlertPreferencesAsync` (`Preferences`), `ListEmailDomainsAsync` (`Domains`), `Typed.ListExperimentsAsync` (`Experiments`), `Typed.GetGenerationTiersAsync` (`Tiers`) | `Total` where the model has it, filled from either shape; `Pagination` once opted in |
| A model with two list properties | `Typed.ListAlertConfigsAsync` (`Configs`), `Typed.ListModelAlertsAsync` (`Alerts`), `ListEmbeddingModelsAsync` and `ListRerankerModelsAsync` (`Models`) | Read `Items`, which returns whichever arrived: the named property is filled on the default shape and `Data` from 2026-07-27, never both. `Total` where the model has it; `Pagination` once opted in |

`Pagination` is `null` on the default shape. The raw `JsonElement` methods —
`ListAlertConfigsAsync`, `ListModelAlertsAsync`, `ListExperimentsAsync`,
`GetGenerationTiersAsync`, `ListModelsAsync`, `ListMemoryBankTemplatesAsync` and
`GetAgentsUsingMemoryBankAsync` — return the response body exactly as sent, so
its top-level shape follows the version; use their `client.Typed` forms.

A successful response that is not a list in either shape — an error-shaped
object, text, `null` — throws `ApiException` from every method in the table
rather than returning an empty list, which would read as "no results". An
explicit `"data": null` is an empty list.

**Opting in turns paging on.** Some endpoints return everything by default and
one page once the request resolves to `2026-07-27` or later, so the same call
can return fewer rows:

| Method | Default | From 2026-07-27 |
| --- | --- | --- |
| `ListEvaluationCriteriaAsync`, `ListEvaluationCriteriaPageAsync` | every criterion; `page` and `limit` are ignored | one page, 20 items unless `limit` is passed |
| `ListRunEvaluationResultsAsync` | every result for the run; `page` and `limit` are ignored | one page, 20 items unless `limit` is passed |
| `ListAlertConfigsAsync`, `Typed.ListAlertConfigsAsync` | every config; `page` and `limit` are ignored | one page, 50 items unless `limit` is passed |
| `SetAutoBlockModeAsync` | `Total` is the account's full count of blocked senders | `Total` is the number of rows returned, which is at most 50 |

For the three listings, read `Pagination.HasNext` and request the next page, or
pass `limit`, before relying on the result being complete. `SetAutoBlockModeAsync`
cannot tell you: opted in, its `Total` is the row count and `HasNext` is always
false. To see every blocked sender, page through `ListBlockedEmailSendersAsync`.

**Later versions.** Each is cumulative, and none changes a response shape this
client decodes:

| Version | What it changes |
| --- | --- |
| `2026-08-03` | The API rejects `max_age_days` on a memory bank with a 400 — leave `UpdateMemoryBankRequest.MaxAgeDays` unset, or send 0 to clear a stored value — and a bank created without `retention_days` gets a default per bank type instead of 30 |
| `2026-08-21` | `CreateSourceAsync` rejects an embedding dimension its embedder does not support with a 400 — `ListEmbeddingModelsAsync` reports the supported ones |
| `2026-09-28` | Agent-definition writes use the current file-list grammar: an omitted `attachments` keeps the stored list and `[]` means no files |
| `2026-09-30` | `AgentRunResponse.Output` and `AgentRunStepResponse.Output` are the text rather than a JSON manifest; the files are in `Attachments` on every version |
| `2026-10-03` | A new LLM step written without `attachments` takes its parent's files, and a new retrieval step's matched media are its files |

## Typed responses

Some methods return `JsonElement` for historical reasons. The same endpoints are
available deserialized under `client.Typed`, which issues the identical request:

```csharp
var raw   = await client.SearchAsync("disk");        // JsonElement
var typed = await client.Typed.SearchAsync("disk");  // SearchResponse
foreach (var hit in typed.Results) Console.WriteLine(hit.Name);
```

**Prefer `client.Typed`.** The raw methods are kept only for source
compatibility; they will be deprecated and then removed in a future major.

## API Coverage

### Identity

```csharp
var me = await client.GetMeAsync();
foreach (var org in me.Organizations)
    Console.WriteLine($"{org.Name} {org.AccountId}");

// Act as an organization: new SeclaiClientOptions { AccountId = org.AccountId }
```

### Agents

```csharp
// CRUD
var agents = await client.ListAgentsAsync(page: 1, limit: 20);
var agent = await client.CreateAgentAsync(new CreateAgentRequest { Name = "Bot" });

// Pause / resume — a disabled agent stops firing from every trigger path
var callers = await client.GetAgentCallersAsync("a1");  // live agents calling this one
await client.DisableAgentAsync("a1");                   // 409 if a caller is still live
await client.EnableAgentAsync("a1");
var fetched = await client.GetAgentAsync(agent.Id);
var updated = await client.UpdateAgentAsync(agent.Id, new UpdateAgentRequest { Name = "Updated" });
await client.DeleteAgentAsync(agent.Id);

// Agent definitions
var def = await client.GetAgentDefinitionAsync(agent.Id);
await client.UpdateAgentDefinitionAsync(agent.Id, new UpdateAgentDefinitionRequest
{
    ExpectedChangeId = def.ChangeId
});

// Export / import an agent
var exported = await client.ExportAgentAsync(agent.Id, download: false);

// Round-trip the export through a Dictionary so the import endpoints can accept it.
var payloadJson = JsonSerializer.Serialize(exported);
var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadJson)!;

// Validate the payload first to surface unresolved entity refs in this account.
var preview = await client.PreviewImportAgentAsync(new AgentImportPreviewRequest { AgentDefinition = payload });
var entityRemap = new Dictionary<string, string>();
foreach (var refEntry in preview.UnresolvedRefs ?? new())
{
    if (refEntry.TryGetValue("ref_id", out var refId) && refId.ValueKind == JsonValueKind.String)
    {
        // Replace "<target-uuid>" with an id from refEntry["alternatives"]
        // before calling CreateAgentAsync; empty values are rejected.
        entityRemap[refId.GetString()!] = "<target-uuid>";
    }
}

// Commit — EntityRemap substitutes workflow refs before save.
var imported = await client.CreateAgentAsync(new CreateAgentRequest
{
    Name = "Imported",
    AgentDefinition = payload,
    EntityRemap = entityRemap,
});
// imported.ImportWarnings lists any items that couldn't be applied.
```

### Agent Runs

```csharp
// Start a run
var run = await client.RunAgentAsync("ag1", new AgentRunRequest { Input = "hello" });

// List runs, get details, cancel
var runs = await client.ListAgentRunsAsync("ag1", page: 1, limit: 20);
var detail = await client.GetAgentRunAsync("run1", includeStepOutputs: true);
await client.CancelAgentRunAsync("run1");
await client.DeleteAgentRunAsync("run1");

// Search across runs
var results = await client.SearchAgentRunsAsync(new AgentTraceSearchRequest { Query = "error" });

// Discover which files (if any) an agent expects before staging uploads
var refs = await client.GetAgentAttachmentReferencesAsync("ag1");
// refs.RequiresUploads reports whether the agent accepts files; refs.Agent lists the
// exact names / indexes / glob patterns a run-time upload batch must satisfy.

// Files a run produced are listed on the run and on each step, on every API version.
// Download one by its Id (raw HttpResponseMessage).
foreach (var file in detail.Attachments)
{
    using var attachment = await client.DownloadAgentRunAttachmentAsync("run1", file.Id, file.Name);
}
```

### Streaming

```csharp
// SSE streaming — yields AgentRunEvent items as they arrive
await foreach (var evt in client.RunStreamingAgentAsync("ag1",
    new AgentRunStreamRequest { Input = "hello" }))
{
    Console.WriteLine($"[{evt.Event}] {evt.Run?.Status}");
}

// Wait for the final result from the SSE stream
var final = await client.RunStreamingAgentAndWaitAsync("ag1",
    new AgentRunStreamRequest { Input = "hello" },
    timeout: TimeSpan.FromSeconds(120));

// Poll-based alternative
var polled = await client.RunAgentAndPollAsync("ag1",
    new AgentRunRequest { Input = "hello" },
    pollInterval: TimeSpan.FromSeconds(2),
    timeout: TimeSpan.FromMinutes(5));
```

### Sources & Content

```csharp
// CRUD
var sources = await client.ListSourcesAsync(page: 1, limit: 20);
var source = await client.CreateSourceAsync(new CreateSourceRequest { Name = "Docs", SourceType = "upload" });
await client.DeleteSourceAsync(source.Id);

// Upload file (max 200 MiB; MIME type auto-inferred from file extension)
var bytes = File.ReadAllBytes("./doc.pdf");
var upload = await client.UploadFileToSourceAsync("sc1", bytes, "doc.pdf",
    title: "My Doc", mimeType: "application/pdf");

// Stream-based upload — avoids loading the entire file into memory
await using var stream = File.OpenRead("./large.pdf");
var upload2 = await client.UploadFileToSourceAsync("sc1", stream, "large.pdf",
    title: "Large Doc");

// Inline text upload
await client.UploadInlineTextToSourceAsync("sc1", new InlineTextUploadRequest
{
    Text = "Hello world", Title = "greeting"
});

// Content management
var content = await client.GetContentDetailAsync("cv1");
await client.DeleteContentAsync("cv1");
var embeddings = await client.ListContentEmbeddingsAsync("cv1", page: 1, limit: 50);

// Indexing status of a source's content, keyed by the ContentVersionId an upload returns
var failed = await client.ListSourceContentsAsync("sc1", status: "failed");
var batch = await client.ListSourceContentsAsync("sc1",
    contentVersionIds: new[] { upload.ContentVersionId!, upload2.ContentVersionId! });
var one = await client.GetSourceContentStatusAsync("sc1", upload.ContentVersionId!);
Console.WriteLine($"{one.ContentStatus} {one.Error}");
```

### Cloud Drives

```csharp
var providers = await client.ListCloudDriveProvidersAsync();
var drives = await client.ListCloudDrivesAsync();
var drive = await client.GetCloudDriveAsync("cd1");

// Only the properties you set are sent; the rest are left unchanged
await client.UpdateCloudDriveAsync("cd1", new CloudDriveUpdateRequest { Name = "Contracts" });

// Which agents depend on it, and which files it skipped and why
var users = await client.GetAgentsUsingCloudDriveAsync("cd1");
var skipped = await client.ListCloudDriveRejectionsAsync("cd1", limit: 20);

await client.DisconnectCloudDriveAsync("cd1");  // keeps the connection
await client.DeleteCloudDriveAsync("cd1");
```

Connecting a drive happens in the app; the API manages connections that exist.

### Source Exports

```csharp
var estimate = await client.EstimateSourceExportAsync("s1",
    new EstimateExportRequest { Format = "csv" });
var export = await client.CreateSourceExportAsync("s1",
    new CreateExportRequest { Format = "csv" });
var status = await client.GetSourceExportAsync("s1", export.Id);
using var response = await client.DownloadSourceExportAsync("s1", export.Id);
await client.DeleteSourceExportAsync("s1", export.Id);
```

### Knowledge Bases

```csharp
var kbs = await client.ListKnowledgeBasesAsync(sort: "created_at");
var kb = await client.CreateKnowledgeBaseAsync(new CreateKnowledgeBaseRequest { Name = "KB" });
await client.UpdateKnowledgeBaseAsync(kb.Id, new UpdateKnowledgeBaseRequest { Name = "Renamed" });
await client.DeleteKnowledgeBaseAsync(kb.Id);
```

### Memory Banks

```csharp
var banks = await client.ListMemoryBanksAsync();
var bank = await client.CreateMemoryBankAsync(new CreateMemoryBankRequest
{
    Name = "Chat Memory", Type = "conversation"
});
var stats = await client.GetMemoryBankStatsAsync(bank.Id);          // JsonElement
var templates = await client.Typed.ListMemoryBankTemplatesAsync();   // List<Dictionary<string, JsonElement>>
var usedBy = await client.Typed.GetAgentsUsingMemoryBankAsync(bank.Id);
await client.CompactMemoryBankAsync(bank.Id);
await client.DeleteMemoryBankAsync(bank.Id);

// AI-assisted configuration
var config = await client.GenerateMemoryBankConfigAsync(
    new MemoryBankAiAssistantRequest { UserInput = "build a chat memory" });
```

### Evaluations

```csharp
// Criteria
var criteria = await client.ListEvaluationCriteriaAsync("ag1", page: 1, limit: 50);
// ListEvaluationCriteriaPageAsync returns the same criteria plus Total/Page/Limit.
var created = await client.CreateEvaluationCriteriaAsync("ag1",
    new CreateEvaluationCriteriaRequest { StepId = "s1" });
var summary = await client.GetEvaluationCriteriaSummaryAsync(created.Id);
await client.DeleteEvaluationCriteriaAsync(created.Id);

// Results
var results = await client.ListAgentEvaluationResultsAsync("ag1", page: 1);
var nonManual = await client.GetNonManualEvaluationSummaryAsync();

// Test before creating
var test = await client.TestDraftEvaluationAsync("ag1",
    new TestDraftEvaluationRequest { AgentInput = "test input" });
```

### Solutions

```csharp
var solutions = await client.ListSolutionsAsync();
var sol = await client.CreateSolutionAsync(new CreateSolutionRequest { Name = "My Solution" });

// Link/unlink resources
await client.LinkAgentsToSolutionAsync(sol.Id, new LinkResourcesRequest { Ids = new List<string> { "ag1" } });
await client.LinkSourceConnectionsToSolutionAsync(sol.Id, new LinkResourcesRequest { Ids = new List<string> { "s1" } });
await client.LinkKnowledgeBasesToSolutionAsync(sol.Id, new LinkResourcesRequest { Ids = new List<string> { "kb1" } });

// AI-assisted solution planning
var plan = await client.GenerateSolutionAiPlanAsync(sol.Id,
    new AiAssistantGenerateRequest { UserInput = "add a FAQ bot" });
var accepted = await client.AcceptSolutionAiPlanAsync(
    sol.Id, plan.ConversationId!, new AiAssistantAcceptRequest());
```

### Governance

```csharp
// AI-assisted governance
var gov = await client.GenerateGovernanceAiPlanAsync(
    new GovernanceAiAssistantRequest { UserInput = "create a content safety policy" });
var conversations = await client.ListGovernanceAiConversationsAsync();
```

### Alerts

```csharp
var alerts = await client.ListAlertsAsync(status: "active");           // JsonElement
var alert = await client.GetAlertAsync("al1");                         // JsonElement
await client.ChangeAlertStatusAsync("al1", new ChangeStatusRequest { Status = "resolved" });
await client.AddAlertCommentAsync("al1", new AddCommentRequest { Body = "Fixed" });

// Alert configs
var configs = await client.ListAlertConfigsAsync();                    // JsonElement
await client.DeleteAlertConfigAsync("ac1");

// Organization preferences
var prefs = await client.ListOrganizationAlertPreferencesAsync();
```

### Agent Email Triggers

A property left `null` is omitted from the request, so the server leaves that field
unchanged. To clear a field, send its **empty value** — `""` for `Alias`, an empty
list for `AllowedSenders`. Setting `null` does not clear anything.

```csharp
// IgnoreAutoGenerated and QueueOnQuota are left null, so they stay as they are.
var cfg = await client.SetEmailTriggerConfigAsync("a1", "t1",
    new SetEmailTriggerConfigRequest
    {
        Alias = "support",
        AllowedSenders = new List<string> { "example.com" },
        RequireSenderAuth = true,
    });
Console.WriteLine(string.Join(", ", cfg.EmailAddresses ?? new List<string>()));

// Clear the alias and open the inbox to any sender.
await client.SetEmailTriggerConfigAsync("a1", "t1",
    new SetEmailTriggerConfigRequest { Alias = "", AllowedSenders = new List<string>() });
```

### Agent Email Governance

```csharp
// Recipients who opted out of this account's agent emails
var optOuts = await client.ListAgentEmailOptOutsAsync(agentId: "a1", limit: 50);
await client.RemoveAgentEmailOptOutAsync("oo1");   // opt them back in

// Blocked inbound senders (owner/admin only); paginates by limit/offset
var blocked = await client.ListBlockedEmailSendersAsync(limit: 50, offset: 0);
await client.BlockEmailSenderAsync(
    new BlockEmailSenderRequest { SenderEmail = "spam.example.com", MatchType = "domain" });
await client.UnblockEmailSenderAsync("b1");

// "disabled" | "input" | "input_and_output"
await client.SetAutoBlockModeAsync(new SetAutoBlockModeRequest { Mode = "input_and_output" });

// Inbound mail discarded before running an agent
var rejections = await client.ListInboundEmailRejectionsAsync(agentId: "a1");

// Account-wide overload circuit breaker
var status = await client.GetInboundEmailStatusAsync();
await client.CancelQueuedEmailRunsAsync();   // fail all QUEUED (over-quota parked) runs
await client.ResumeInboundEmailAsync();      // one-shot; re-arms if still overloaded
```

### Email Domains

Send and receive agent email on your own domain instead of the shared
`agent.seclai.com`. Requires a user-bound credential; mutations require an
account owner/admin.

```csharp
var listing = await client.ListEmailDomainsAsync();

var vanity = await client.AddEmailDomainAsync(
    new AddEmailDomainRequest { Kind = "vanity", Value = "acme" });
var custom = await client.AddEmailDomainAsync(
    new AddEmailDomainRequest { Kind = "custom", Value = "agent.mycompany.com" });

// Publish custom.DnsRecords, then check without waiting for the background sweep
await client.VerifyEmailDomainAsync(custom.Id);

await client.SetPrimaryEmailDomainAsync(custom.Id);
await client.UseSharedEmailDomainAsync();  // revert; domains stay configured & verified

await client.SendEmailDomainTestEmailAsync(custom.Id);  // always to the account owner
var dmarc = await client.GetDmarcSummaryAsync(custom.Id, days: 30, topSources: 10);

var removed = await client.RemoveEmailDomainAsync(custom.Id);
// removed.CleanupNote is set when the domain was Seclai-managed
```

### Models & Model Alerts

```csharp
// Media-generation quality tiers (fast/balanced/thorough) and what each resolves to
var tiers = await client.GetGenerationTiersAsync();                    // JsonElement

// Embedding and reranker models, with their pricing
var embedders = await client.ListEmbeddingModelsAsync(supportsInputMedia: "image");
foreach (var m in embedders.Items)
    Console.WriteLine($"{m.ModelType} {string.Join(",", m.Dimensions)}");
var rerankers = await client.ListRerankerModelsAsync();
Console.WriteLine(rerankers.DefaultModelType);

var alerts = await client.ListModelAlertsAsync();                      // JsonElement
await client.MarkModelAlertReadAsync("ma1");
await client.MarkAllModelAlertsReadAsync();
var recs = await client.GetModelRecommendationsAsync("model1");        // JsonElement

// Model playground experiments
var experiment = await client.CreateExperimentAsync(
    new PlaygroundCreateRequest { Prompt = "...", ModelIds = new List<string> { "model1" } });
var experiments = await client.ListExperimentsAsync();                 // JsonElement
var detail = await client.GetExperimentAsync("exp1");                  // JsonElement
await client.CancelExperimentAsync("exp1");
await client.DeleteExperimentAsync("exp1");  // soft-delete, preserves audit history
```

### Search

```csharp
var results = await client.SearchAsync(query: "my bot", entityType: "agent");  // JsonElement
```

### Documentation Search

Results are global (not account-scoped); each carries a `doc_slug` plus an optional
`anchor` for building a `https://seclai.com/docs/<doc_slug>[#<anchor>]` link.

```csharp
var hits = await client.SearchDocsAsync("email triggers");                       // JsonElement
var deep = await client.SearchDocsAsync("how do I stop auto-reply loops",
                                        mode: "semantic", limit: 5);
```

### AI Assistant (Top-Level)

```csharp
// Knowledge base assistant
var plan = await client.AiAssistantKnowledgeBaseAsync(
    new AiAssistantGenerateRequest { UserInput = "create a docs knowledge base" });
await client.AcceptAiAssistantPlanAsync(
    plan.ConversationId!, new AiAssistantAcceptRequest());

// Source and memory bank assistants
await client.AiAssistantSourceAsync(new AiAssistantGenerateRequest { UserInput = "plan" });
await client.AiAssistantMemoryBankAsync(new MemoryBankAiAssistantRequest { UserInput = "plan" });

// Feedback
await client.SubmitAiFeedbackAsync(new AiAssistantFeedbackRequest
{
    Feature = "chat", Rating = "positive"
});
```

## Error Handling

```csharp
try
{
    await client.GetAgentAsync("nonexistent");
}
catch (ApiValidationException ex)
{
    // 422 — validation errors
    Console.WriteLine(ex.Message);
    foreach (var err in ex.Errors)
        Console.WriteLine($"  {err.Loc}: {err.Msg}");
}
catch (ApiException ex)
{
    // Other HTTP errors (401, 403, 404, 500, …)
    Console.WriteLine($"{ex.StatusCode}: {ex.Message}");
}
```

## Documentation

Full API reference is available [on the Seclai docs site](https://docs.seclai.com/sdks/csharp).

To generate docs locally:

```bash
make docs
```

## License

[Apache-2.0](LICENSE)

## Development

Tests target `net10.0` (requires the .NET 10 SDK).

```bash
dotnet test
```

## Docs

This repo uses DocFX to generate and publish API docs to GitHub Pages.

Pages structure:

- `/latest/` is updated on each release build from `main`
- `/<version>/` is published for each release tag (e.g. `1.2.3`)

Build docs locally:

```bash
dotnet tool restore
rm -rf build/docs build/api
dotnet tool run docfx docfx.json

# static site output (upload `build/docs` to any static host, incl. GitHub Pages)
open build/docs/index.html

# if your browser/preview blocks JS/CSS when using file://, serve it locally:
# cd build/docs && python3 -m http.server 8000
```

Or using the Makefile:

```bash
make docs
make docs-serve
```
