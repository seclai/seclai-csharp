using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class CloudDriveAccessLevelResponse
{
    /// <summary>Whether connecting without a choice uses it.</summary>
    [JsonPropertyName("default")]
    public bool Default { get; set; }

    /// <summary>What agents can do at this level.</summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>Level identifier, e.g. <c>read_write</c>/<c>read_only</c>.</summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>Short human-readable name.</summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;
}
