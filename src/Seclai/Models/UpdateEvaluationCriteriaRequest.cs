using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class UpdateEvaluationCriteriaRequest
{
    [JsonPropertyName("step_id")]
    public string? StepId { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("evaluation_prompt")]
    public string? EvaluationPrompt { get; set; }

    [JsonPropertyName("evaluation_tier")]
    public string? EvaluationTier { get; set; }

    [JsonPropertyName("expectation_config")]
    public Dictionary<string, JsonElement>? ExpectationConfig { get; set; }

    [JsonPropertyName("pass_threshold")]
    public float? PassThreshold { get; set; }

    [JsonPropertyName("enabled")]
    public bool? Enabled { get; set; }
}
