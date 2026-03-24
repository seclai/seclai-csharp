using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class GovernanceAiAssistantResponse
{
    [JsonPropertyName("assistant_response")]
    public string? AssistantResponse { get; set; }
}
