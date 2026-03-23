using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class EstimateExportResponse
{
    [JsonPropertyName("estimated_size_bytes")]
    public int EstimatedSizeBytes { get; set; }

    [JsonPropertyName("source_connection_id")]
    public string? SourceConnectionId { get; set; }
}
