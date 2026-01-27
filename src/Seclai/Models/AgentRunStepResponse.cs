using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AgentRunStepResponse
{
    [JsonPropertyName("agent_step_id")]
    public string? AgentStepId { get; set; }

    [JsonPropertyName("step_type")]
    public string? StepType { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("output")]
    public string? Output { get; set; }

    [JsonPropertyName("output_content_type")]
    public string? OutputContentType { get; set; }

    [JsonPropertyName("started_at")]
    public string? StartedAt { get; set; }

    [JsonPropertyName("ended_at")]
    public string? EndedAt { get; set; }

    [JsonPropertyName("duration_seconds")]
    public float? DurationSeconds { get; set; }

    [JsonPropertyName("credits_used")]
    public float CreditsUsed { get; set; }
}
