using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AgentRunAttemptResponse
{
    [JsonPropertyName("duration")]
    public float? Duration { get; set; }

    [JsonPropertyName("ended_at")]
    public string? EndedAt { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("started_at")]
    public string? StartedAt { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
