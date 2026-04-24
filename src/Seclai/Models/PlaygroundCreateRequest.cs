using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class PlaygroundCreateRequest
{
    [JsonPropertyName("prompt")]
    public string? Prompt { get; set; }

    [JsonPropertyName("model_ids")]
    public List<string> ModelIds { get; set; } = new();

    [JsonPropertyName("system_prompt")]
    public string? SystemPrompt { get; set; }

    [JsonPropertyName("evaluation_mode")]
    public string? EvaluationMode { get; set; }

    [JsonPropertyName("evaluation_complexity")]
    public string? EvaluationComplexity { get; set; }

    [JsonPropertyName("evaluator_model_id")]
    public string? EvaluatorModelId { get; set; }

    [JsonPropertyName("include_step_output_in_evaluation")]
    public bool? IncludeStepOutputInEvaluation { get; set; }

    [JsonPropertyName("selected_step_output")]
    public string? SelectedStepOutput { get; set; }

    [JsonPropertyName("json_template")]
    public string? JsonTemplate { get; set; }
}
