using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AiAssistantGenerateRequest
{
    [JsonPropertyName("user_input")]
    public string? UserInput { get; set; }
}
