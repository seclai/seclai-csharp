using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>``GET /models/playground/experiments`` legacy/default shape; 2026-07-27+ clients get the canonical ``{data, pagination}`` envelope.</summary>
public sealed class ExperimentListResponse
{
    [JsonPropertyName("experiments")]
    public List<ExperimentSummaryResponse> Experiments { get; set; } = new();

    [JsonPropertyName("total")]
    public int Total { get; set; }
}
