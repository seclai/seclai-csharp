using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class ExperimentSummaryResponse
{
    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("evaluation_complexity")]
    public string EvaluationComplexity { get; set; } = string.Empty;

    [JsonPropertyName("evaluation_mode")]
    public string EvaluationMode { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("selected_model_ids")]
    public List<string> SelectedModelIds { get; set; } = new();

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}
