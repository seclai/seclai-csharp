using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>
/// One run metadata key a trigger declares. The agent's steps reference it as
/// <c>{{metadata.&lt;key&gt;}}</c>.
/// </summary>
public sealed class MetadataKeyDeclaration
{
    /// <summary>The key, 1–255 characters. Steps reference it as <c>{{metadata.&lt;key&gt;}}</c>.</summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Who writes the key: <c>system</c> or <c>source</c> keys are written by the platform; <c>custom</c> keys
    /// are sent by the caller or set on the trigger.
    /// </summary>
    [JsonPropertyName("origin")]
    public string Origin { get; set; } = string.Empty;

    /// <summary>
    /// Whether a caller-started run without this key is refused. Only a <c>custom</c> key can be required.
    /// A null here is omitted from the request, and the server treats it as <c>false</c>.
    /// </summary>
    [JsonPropertyName("required")]
    public bool? Required { get; set; }
}
