using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class MarkConversationTurnRequest
{
    [JsonPropertyName("accepted")]
    public bool Accepted { get; set; }
}
