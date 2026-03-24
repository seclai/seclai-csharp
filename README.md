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
Console.WriteLine($"Run {run.Id}: {run.Status}");
```

## Configuration

| Option | Default | Description |
|--------|---------|-------------|
| `ApiKey` | `SECLAI_API_KEY` env var | API key for authentication. Falls back to `SECLAI_API_KEY` env var. |
| `BaseUri` | `https://seclai.com` | API base URL. Falls back to `SECLAI_API_URL` env var. |
| `HttpClient` | internal | Bring your own `HttpClient` (useful for DI or testing) |
| `Timeout` | 120 s | HTTP request timeout. Ignored when a custom `HttpClient` is provided. |
| `DefaultHeaders` | none | Extra headers appended to every request (e.g. tracing / tenant IDs). |

`SeclaiClient` implements `IDisposable`. When you let it create its own `HttpClient`, wrap it in a `using` statement so the client is disposed properly:

```csharp
using var client = new SeclaiClient(new SeclaiClientOptions { ApiKey = "sk_..." });
```

If you supply your own `HttpClient`, the client does **not** dispose it — you manage its lifetime.

## API Coverage

### Agents

```csharp
// CRUD
var agents = await client.ListAgentsAsync(page: 1, limit: 20);
var agent = await client.CreateAgentAsync(new CreateAgentRequest { Name = "Bot" });
var fetched = await client.GetAgentAsync(agent.Id);
var updated = await client.UpdateAgentAsync(agent.Id, new UpdateAgentRequest { Name = "Updated" });
await client.DeleteAgentAsync(agent.Id);

// Agent definitions
var def = await client.GetAgentDefinitionAsync(agent.Id);
await client.UpdateAgentDefinitionAsync(agent.Id, new UpdateAgentDefinitionRequest
{
    ExpectedChangeId = def.ChangeId
});
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
var criteria = await client.ListEvaluationCriteriaAsync("ag1");
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
await client.LinkSourcesToSolutionAsync(sol.Id, new LinkResourcesRequest { Ids = new List<string> { "s1" } });
await client.LinkKnowledgeBasesToSolutionAsync(sol.Id, new LinkResourcesRequest { Ids = new List<string> { "kb1" } });

// AI-assisted solution planning
var plan = await client.GenerateSolutionAiPlanAsync(sol.Id,
    new AiAssistantGenerateRequest { UserInput = "add a FAQ bot" });
var accepted = await client.AcceptSolutionAiPlanAsync(sol.Id, plan.ConversationId);
```

### Governance

```csharp
// AI-assisted governance
var gov = await client.GenerateGovernanceAiPlanAsync(
    new GovernanceAiAssistantRequest { UserInput = "create a content safety policy" });
var result = await client.AcceptGovernanceAiPlanAsync(gov.ConversationId!);
var conversations = await client.ListGovernanceAiConversationsAsync();
```

### Alerts

```csharp
var alerts = await client.ListAlertsAsync(status: "active");           // JsonElement
var alert = await client.GetAlertAsync("al1");                         // JsonElement
await client.ChangeAlertStatusAsync("al1", new ChangeStatusRequest { Status = "resolved" });
await client.AddAlertCommentAsync("al1", new AddCommentRequest { Comment = "Fixed" });

// Alert configs
var configs = await client.ListAlertConfigsAsync();                    // JsonElement
await client.DeleteAlertConfigAsync("ac1");

// Organization preferences
var prefs = await client.ListOrganizationAlertPreferencesAsync();
```

### Models & Model Alerts

```csharp
var alerts = await client.ListModelAlertsAsync();                      // JsonElement
await client.MarkModelAlertReadAsync("ma1");
await client.MarkAllModelAlertsReadAsync();
var recs = await client.GetModelRecommendationsAsync("model1");        // JsonElement
```

### Search

```csharp
var results = await client.SearchAsync(query: "my bot", entityType: "agent");  // JsonElement
```

### AI Assistant (Top-Level)

```csharp
// Knowledge base assistant
var plan = await client.AiAssistantKnowledgeBaseAsync(
    new AiAssistantGenerateRequest { UserInput = "create a docs knowledge base" });
await client.AcceptAiAssistantPlanAsync(plan.ConversationId);

// Source, memory bank, and agent assistants
await client.AiAssistantSourceAsync(new AiAssistantGenerateRequest { UserInput = "plan" });
await client.AiAssistantMemoryBankAsync(new AiAssistantGenerateRequest { UserInput = "plan" });
await client.AiAssistantAgentAsync(new AiAssistantGenerateRequest { UserInput = "plan" });

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
