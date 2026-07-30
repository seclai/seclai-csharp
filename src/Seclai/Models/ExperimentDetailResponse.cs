using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class ExperimentDetailResponse
{
    [JsonPropertyName("completed_at")]
    public string? CompletedAt { get; set; }

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("evaluation_complexity")]
    public string EvaluationComplexity { get; set; } = string.Empty;

    [JsonPropertyName("evaluation_mode")]
    public string EvaluationMode { get; set; } = string.Empty;

    [JsonPropertyName("evaluator_model_id")]
    public string? EvaluatorModelId { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("include_step_output_in_evaluation")]
    public bool IncludeStepOutputInEvaluation { get; set; }

    [JsonPropertyName("json_template")]
    public string? JsonTemplate { get; set; }

    [JsonPropertyName("progress_current")]
    public int? ProgressCurrent { get; set; }

    [JsonPropertyName("progress_message")]
    public string? ProgressMessage { get; set; }

    [JsonPropertyName("progress_total")]
    public int? ProgressTotal { get; set; }

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("result_data")]
    public Dictionary<string, JsonElement>? ResultData { get; set; }

    [JsonPropertyName("selected_model_ids")]
    public List<string> SelectedModelIds { get; set; } = new();

    [JsonPropertyName("selected_step_output")]
    public string? SelectedStepOutput { get; set; }

    [JsonPropertyName("started_at")]
    public string? StartedAt { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("system_prompt")]
    public string SystemPrompt { get; set; } = string.Empty;
}
