using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>A system or source metadata key that a trigger's type can declare.</summary>
public sealed class DeclarableMetadataKey
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary><c>system</c>, <c>source</c> or <c>custom</c>.</summary>
    [JsonPropertyName("origin")]
    public string Origin { get; set; } = string.Empty;

    /// <summary>Whether every run carries this key.</summary>
    [JsonPropertyName("always_present")]
    public bool AlwaysPresent { get; set; }
}
