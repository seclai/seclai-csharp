using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class MemoryBankConversationTurnResponse
{
    [JsonPropertyName("user_input")]
    public string? UserInput { get; set; }

    [JsonPropertyName("ai_note")]
    public string? AiNote { get; set; }

    [JsonPropertyName("accepted")]
    public bool? Accepted { get; set; }
}
