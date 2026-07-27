using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AddEmailDomainRequest
{
    [JsonPropertyName("delegated")]
    public bool? Delegated { get; set; }

    /// <summary>'vanity' or 'custom'</summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}
