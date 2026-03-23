using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class SourceEmbeddingMigrationResponse
{
    [JsonPropertyName("migration_id")]
    public string? MigrationId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
