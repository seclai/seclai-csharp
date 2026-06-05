using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>A single LLM tool call made during a <c>prompt_call</c> step.</summary>
public sealed class AgentRunToolCallResponse
{
    /// <summary>Tool call identifier.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>Name of the tool/function invoked.</summary>
    [JsonPropertyName("function_name")]
    public string? FunctionName { get; set; }

    /// <summary>JSON arguments the LLM passed to the tool, if persisted.</summary>
    [JsonPropertyName("input")]
    public string? Input { get; set; }

    /// <summary>JSON result the tool returned to the LLM, if persisted.</summary>
    [JsonPropertyName("output")]
    public string? Output { get; set; }

    /// <summary>Whether the tool call completed without error.</summary>
    [JsonPropertyName("succeeded")]
    public bool Succeeded { get; set; }

    /// <summary>Error message when the tool call failed.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>Credits consumed by this tool call (0 for tools that don't bill).</summary>
    [JsonPropertyName("credits_used")]
    public float CreditsUsed { get; set; }

    /// <summary>0-based tool-loop round this call belonged to.</summary>
    [JsonPropertyName("round_index")]
    public int RoundIndex { get; set; }

    /// <summary>0-based ordinal of this call within its step run.</summary>
    [JsonPropertyName("sequence")]
    public int Sequence { get; set; }

    /// <summary>Timestamp when the tool call started.</summary>
    [JsonPropertyName("started_at")]
    public string? StartedAt { get; set; }

    /// <summary>Timestamp when the tool call ended.</summary>
    [JsonPropertyName("ended_at")]
    public string? EndedAt { get; set; }

    /// <summary>Duration of the tool call in seconds.</summary>
    [JsonPropertyName("duration_seconds")]
    public float? DurationSeconds { get; set; }
}
