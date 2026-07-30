using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>Response model for a prompt tool.</summary>
public sealed class PromptToolResponse
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("documentation_url")]
    public string? DocumentationUrl { get; set; }

    [JsonPropertyName("example")]
    public string? Example { get; set; }

    [JsonPropertyName("headers")]
    public Dictionary<string, string>? Headers { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("tool_name")]
    public string? ToolName { get; set; }

    [JsonPropertyName("tool_type")]
    public string ToolType { get; set; } = string.Empty;

    [JsonPropertyName("tool_type_pattern")]
    public string? ToolTypePattern { get; set; }
}
