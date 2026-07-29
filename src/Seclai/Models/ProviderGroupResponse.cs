using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>Response model for provider group with models</summary>
public sealed class ProviderGroupResponse
{
    [JsonPropertyName("models")]
    public List<PromptModelResponse> Models { get; set; } = new();

    [JsonPropertyName("provider")]
    public string Provider { get; set; } = string.Empty;
}
