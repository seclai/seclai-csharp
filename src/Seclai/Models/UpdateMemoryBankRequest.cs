using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class UpdateMemoryBankRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("compaction_prompt")]
    public string? CompactionPrompt { get; set; }

    [JsonPropertyName("max_age_days")]
    public int? MaxAgeDays { get; set; }

    [JsonPropertyName("max_size_tokens")]
    public int? MaxSizeTokens { get; set; }

    [JsonPropertyName("max_turns")]
    public int? MaxTurns { get; set; }

    [JsonPropertyName("retention_days")]
    public int? RetentionDays { get; set; }
}
