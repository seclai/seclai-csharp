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
| `HttpClient` | internal | Bring your own `HttpClient` (useful for DI or testing). |
| `Timeout` | 120 s | HTTP request timeout. Ignored when a custom `HttpClient` is provided. |
| `DefaultHeaders` | none | Extra headers appended to every request. |

`SeclaiClient` implements `IDisposable`. When you let it create its own `HttpClient`, wrap it in a `using` statement so the client is disposed properly:

```csharp
using var client = new SeclaiClient(new SeclaiClientOptions { ApiKey = "sk_..." });
```

If you supply your own `HttpClient`, the client does **not** dispose it — you manage its lifetime.

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

Known versions are on `SeclaiApiVersion` (`V2026_07_01`, `V2026_07_27`, plus
`Default`, `Latest` and `Known`). A version this release was **not** built
against is rejected at construction: a newer version can reshape responses, and
this client would decode them incorrectly rather than reject them. Upgrade the
package to adopt a new version, or set `AllowUnknownApiVersion` if you have to
move first and accept that risk.

The guard only covers the header. An account pinned server-side can still be
newer than this release — `GetApiVersionAsync().EffectiveVersion` is what the
request actually resolved to, and comparing it against `SeclaiApiVersion.Latest`
is how you detect the gap.

**What `2026-07-27` changes.** Undeclared query parameters become a 422 instead
of being ignored, and list endpoints move to the canonical
`{data, pagination}` envelope. The affected methods read both shapes, so they
keep working either way — but the metadata moves:

| Method | Before | From 2026-07-27 |
| --- | --- | --- |
| `ListEvaluationCriteriaPageAsync` | bare array | `Data` + `Pagination` |
| `ListRunEvaluationResultsAsync` | bare array | `Data` + `Pagination` |
| `Typed.ListAlertConfigsAsync` | `Configs` + `Total` | `Data` + `Pagination` |
| `Typed.ListModelAlertsAsync` | `Alerts` + `Total` | `Data` + `Pagination` |

Read the last two through `Items`, which returns whichever key arrived, and
prefer `Pagination` over the flat `Total`/`Page`/`Limit` properties. The flat
properties will be deprecated and then removed once the canonical envelope is
the default.

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

// Download a file attachment emitted by a step in a run (raw HttpResponseMessage).
// attachmentId is the URL-safe-base64 storage_key from run output manifests / webhooks.
using var attachment = await client.DownloadAgentRunAttachmentAsync("run1", "attachment_id");
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
```

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
var templates = await client.ListMemoryBankTemplatesAsync();         // JsonElement
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
