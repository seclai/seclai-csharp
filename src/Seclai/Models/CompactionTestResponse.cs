using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class CompactionTestResponse
{
    [JsonPropertyName("generated")]
    public bool Generated { get; set; }

    [JsonPropertyName("original_entries")]
    public List<string>? OriginalEntries { get; set; }

    [JsonPropertyName("surviving_entries")]
    public List<string>? SurvivingEntries { get; set; }

    [JsonPropertyName("compaction_summary")]
    public string? CompactionSummary { get; set; }

    [JsonPropertyName("evaluation")]
    public CompactionEvaluationModel? Evaluation { get; set; }
}
