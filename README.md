# Seclai C# SDK

Official Seclai C# SDK.

## Install (NuGet)

Once published, install via:

```bash
dotnet add package Seclai.Sdk
```

## Usage

```csharp
using System;
using System.IO;
using System.Threading.Tasks;
using Seclai;
using Seclai.Models;

class Program
{
	static async Task Main()
	{
		var client = new SeclaiClient(new SeclaiClientOptions
		{
			ApiKey = Environment.GetEnvironmentVariable("SECLAI_API_KEY"),
			// BaseUri optional; defaults to https://seclai.com
		});

		var sources = await client.ListSourcesAsync(page: 1, limit: 20);
		Console.WriteLine($"Sources: {sources.Data.Count}");

		// Upload a file to a source connection
		var bytes = await File.ReadAllBytesAsync("./example.pdf");
		var upload = await client.UploadFileToSourceAsync(
			sourceConnectionId: "sc_scn_123",
			fileBytes: bytes,
			fileName: "example.pdf",
			title: "Example PDF"
		);
		Console.WriteLine($"Uploaded: {upload.SourceConnectionContentVersion}");

		// Run an agent
		var run = await client.RunAgentAsync(
			agentId: "sc_ag_123",
			body: new AgentRunRequest { Input = "hello" }
		);
		Console.WriteLine($"Run: {run.Id} status={run.Status}");

		// Run an agent via SSE streaming and wait for the final result
		// Completes when the stream emits the final `done` event; throws on timeout or early termination.
		var finalRun = await client.RunStreamingAgentAndWaitAsync(
			agentId: "sc_ag_123",
			body: new AgentRunStreamRequest { Input = "hello", Metadata = new() { ["app"] = "My App" } },
			timeout: TimeSpan.FromSeconds(60)
		);
		Console.WriteLine($"Final run: {finalRun.Id} status={finalRun.Status}");

		// Fetch content details + embeddings
		var content = await client.GetContentDetailAsync(upload.SourceConnectionContentVersion);
		var embeddings = await client.ListContentEmbeddingsAsync(upload.SourceConnectionContentVersion, page: 1, limit: 20);
		Console.WriteLine($"Content title: {content.Title} embeddings: {embeddings.Data.Count}");

		// Delete content
		await client.DeleteContentAsync(upload.SourceConnectionContentVersion);
	}
}
```

## Configuration

- `SECLAI_API_KEY`: API key (used if `SeclaiClientOptions.ApiKey` is not set)
- `SECLAI_API_URL`: Override base URL (used if `SeclaiClientOptions.BaseUri` is not set)

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
dotnet tool run docfx docfx.json
open build/docs/index.html
```
