using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>One agent that calls another (blocks disabling the callee while live).</summary>
public sealed class AgentCallerApiResponse
{
    [JsonPropertyName("disabled")]
    public bool Disabled { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
