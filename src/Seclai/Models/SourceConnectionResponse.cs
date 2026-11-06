using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>A source connection linked to a knowledge base, as listed in <see cref="KnowledgeBaseResponse.Sources"/>.</summary>
public sealed class SourceConnectionResponse
{
    /// <summary>Source connection identifier.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>Source connection name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Source URL.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>Type of source (<c>rss</c>, <c>website</c>, and so on).</summary>
    [JsonPropertyName("source_type")]
    public string? SourceType { get; set; }

    /// <summary>Polling configuration.</summary>
    [JsonPropertyName("polling")]
    public string? Polling { get; set; }
}
