using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class CloudDriveScopeResponse
{
    /// <summary>What the scope allows.</summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>The OAuth scope string sent to the provider.</summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>Short human-readable name.</summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>Whether this scope is requested by default on connect.</summary>
    [JsonPropertyName("recommended")]
    public bool Recommended { get; set; }
}
