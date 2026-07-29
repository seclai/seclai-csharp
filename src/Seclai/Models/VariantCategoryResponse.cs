using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>Response model for a variant category</summary>
public sealed class VariantCategoryResponse
{
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("configurable")]
    public bool Configurable { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("options")]
    public List<VariantOptionResponse> Options { get; set; } = new();

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
}
