using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AiAssistantFeedbackResponse
{
    [JsonPropertyName("feedback_id")]
    public string? FeedbackId { get; set; }

    [JsonPropertyName("flagged")]
    public bool Flagged { get; set; }

    [JsonPropertyName("flag_reason")]
    public string? FlagReason { get; set; }
}
