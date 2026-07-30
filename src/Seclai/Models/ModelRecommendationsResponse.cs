using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class ModelRecommendationsResponse
{
    [JsonPropertyName("alternatives")]
    public List<ModelRecommendationResponse> Alternatives { get; set; } = new();

    [JsonPropertyName("current_model_id")]
    public string CurrentModelId { get; set; } = string.Empty;

    [JsonPropertyName("current_model_name")]
    public string CurrentModelName { get; set; } = string.Empty;

    [JsonPropertyName("same_provider")]
    public List<ModelRecommendationResponse> SameProvider { get; set; } = new();

    [JsonPropertyName("successor")]
    public ModelRecommendationResponse? Successor { get; set; }

    [JsonPropertyName("upgrades")]
    public List<ModelRecommendationResponse> Upgrades { get; set; } = new();
}
