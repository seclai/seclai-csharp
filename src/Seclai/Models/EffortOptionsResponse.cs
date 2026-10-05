using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>The reasoning-effort values a model accepts.</summary>
public sealed class EffortOptionsResponse
{
    /// <summary>The vendor's default level, when known.</summary>
    [JsonPropertyName("default")]
    public string? Default { get; set; }

    /// <summary><c>levels</c> today: <c>values</c> lists them.</summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    /// <summary>Accepted values, weakest first.</summary>
    [JsonPropertyName("values")]
    public List<string>? Values { get; set; }
}
