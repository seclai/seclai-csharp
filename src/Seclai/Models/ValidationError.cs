using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class ValidationError
{
    [JsonPropertyName("loc")]
    public List<JsonElement>? Loc { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}
