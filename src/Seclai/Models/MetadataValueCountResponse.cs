using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>One value of an item-metadata key, and how many items carry it.</summary>
public sealed class MetadataValueCountResponse
{
    /// <summary>The value as text, cut to 100 characters; an object is shown as <c>{…}</c> and a list as <c>[…]</c>.</summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    /// <summary>Items carrying this value.</summary>
    [JsonPropertyName("items")]
    public int Items { get; set; }
}
