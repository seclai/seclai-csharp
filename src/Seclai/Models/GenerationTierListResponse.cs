using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>``GET /models/generation-tiers`` legacy/default shape; 2026-07-27+ clients get the canonical ``{data, pagination}`` envelope.</summary>
public sealed class GenerationTierListResponse
{
    [JsonPropertyName("tiers")]
    public List<GenerationTierResponse> Tiers { get; set; } = new();
}
