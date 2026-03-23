using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class UpdateAgentDefinitionRequest
{
    [JsonPropertyName("definition")]
    public Dictionary<string, JsonElement>? Definition { get; set; }

    [JsonPropertyName("expected_change_id")]
    public string? ExpectedChangeId { get; set; }
}
