using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AiConversationHistoryResponse
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("turns")]
    public List<AiConversationTurnResponse>? Turns { get; set; }
}
