using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class SolutionConversationResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("user_input")]
    public string? UserInput { get; set; }

    [JsonPropertyName("ai_response")]
    public string? AiResponse { get; set; }

    [JsonPropertyName("accepted")]
    public bool? Accepted { get; set; }

    [JsonPropertyName("actions_taken")]
    public Dictionary<string, JsonElement>? ActionsTaken { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }
}
