using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class TestCompactionRequest
{
    [JsonPropertyName("compaction_prompt")]
    public string? CompactionPrompt { get; set; }

    [JsonPropertyName("entry_count")]
    public int? EntryCount { get; set; }

    [JsonPropertyName("generate_direction")]
    public string? GenerateDirection { get; set; }

    [JsonPropertyName("sample_entries")]
    public List<string>? SampleEntries { get; set; }
}
