using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class NonManualEvaluationSummaryResponse
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("passed")]
    public int Passed { get; set; }

    [JsonPropertyName("failed")]
    public int Failed { get; set; }

    [JsonPropertyName("flagged")]
    public int Flagged { get; set; }

    [JsonPropertyName("pass_rate")]
    public float PassRate { get; set; }

    [JsonPropertyName("failure_rate")]
    public float FailureRate { get; set; }

    [JsonPropertyName("by_mode")]
    public List<JsonElement>? ByMode { get; set; }
}
