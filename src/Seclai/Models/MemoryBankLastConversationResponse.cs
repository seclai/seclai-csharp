using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class MemoryBankLastConversationResponse
{
    [JsonPropertyName("user_input")]
    public string? UserInput { get; set; }

    [JsonPropertyName("ai_note")]
    public string? AiNote { get; set; }

    [JsonPropertyName("accepted")]
    public bool? Accepted { get; set; }

    [JsonPropertyName("total")]
    public int? Total { get; set; }

    [JsonPropertyName("turns")]
    public List<MemoryBankConversationTurnResponse>? Turns { get; set; }
}
