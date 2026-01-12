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
