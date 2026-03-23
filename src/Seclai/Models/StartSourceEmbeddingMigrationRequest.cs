using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class StartSourceEmbeddingMigrationRequest
{
    [JsonPropertyName("target_embedding_model")]
    public string? TargetEmbeddingModel { get; set; }

    [JsonPropertyName("target_dimensions")]
    public int TargetDimensions { get; set; }

    [JsonPropertyName("chunk_size")]
    public int? ChunkSize { get; set; }

    [JsonPropertyName("chunk_overlap")]
    public int? ChunkOverlap { get; set; }

    [JsonPropertyName("chunk_language")]
    public string? ChunkLanguage { get; set; }

    [JsonPropertyName("chunk_separators")]
    public string? ChunkSeparators { get; set; }

    [JsonPropertyName("chunk_regex_separators")]
    public bool? ChunkRegexSeparators { get; set; }

    [JsonPropertyName("notification_recipients")]
    public List<string>? NotificationRecipients { get; set; }
}
