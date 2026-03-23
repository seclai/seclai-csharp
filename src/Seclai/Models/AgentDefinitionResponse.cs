using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AgentDefinitionResponse
{
    [JsonPropertyName("change_id")]
    public string? ChangeId { get; set; }

    [JsonPropertyName("definition")]
    public Dictionary<string, JsonElement>? Definition { get; set; }

    [JsonPropertyName("schema_version")]
    public string? SchemaVersion { get; set; }

    [JsonPropertyName("warnings")]
    public List<Dictionary<string, string>>? Warnings { get; set; }
}
