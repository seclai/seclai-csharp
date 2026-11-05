using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class EvaluationCriteriaResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>The agent the criterion belongs to.</summary>
    [JsonPropertyName("agent_id")]
    public string? AgentId { get; set; }

    /// <summary>The owning account.</summary>
    [JsonPropertyName("account_id")]
    public string? AccountId { get; set; }

    /// <summary>The step the criterion evaluates; null when it evaluates the run.</summary>
    [JsonPropertyName("step_id")]
    public string? StepId { get; set; }

    /// <summary>Whether the criterion is evaluated.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>Runtime behaviour: <c>output_expectation</c> (manual validation) or <c>sample_and_flag</c> (sampled monitoring).</summary>
    [JsonPropertyName("evaluation_mode")]
    public string? EvaluationMode { get; set; }

    /// <summary>Prompt the evaluator scores the output against.</summary>
    [JsonPropertyName("evaluation_prompt")]
    public string? EvaluationPrompt { get; set; }

    /// <summary>Evaluation tier pinned on the criterion; null to use the agent's default.</summary>
    [JsonPropertyName("evaluation_tier")]
    public string? EvaluationTier { get; set; }

    /// <summary>Expectation configuration for <c>output_expectation</c> mode.</summary>
    [JsonPropertyName("expectation_config")]
    public Dictionary<string, JsonElement>? ExpectationConfig { get; set; }

    /// <summary>Score cutoff for pass/fail, inclusive (0.0 to 1.0).</summary>
    [JsonPropertyName("pass_threshold")]
    public double PassThreshold { get; set; }

    /// <summary>Result counts keyed by outcome.</summary>
    [JsonPropertyName("result_summary")]
    public Dictionary<string, int>? ResultSummary { get; set; }

    /// <summary>Has no effect: the API reports it for compatibility only.</summary>
    [JsonPropertyName("max_retries")]
    public int MaxRetries { get; set; }

    /// <summary>Has no effect: the API reports it for compatibility only.</summary>
    [JsonPropertyName("retry_on_failure")]
    public bool RetryOnFailure { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }
}
