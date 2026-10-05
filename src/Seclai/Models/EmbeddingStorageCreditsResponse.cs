using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>Monthly storage credits per stored record at a dimension count.</summary>
public sealed class EmbeddingStorageCreditsResponse
{
    /// <summary>Credits per record per month</summary>
    [JsonPropertyName("credits")]
    public int Credits { get; set; }

    /// <summary>Number of embedding dimensions</summary>
    [JsonPropertyName("dimensions")]
    public int Dimensions { get; set; }
}
