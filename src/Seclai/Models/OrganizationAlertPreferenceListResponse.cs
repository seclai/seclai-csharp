using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class OrganizationAlertPreferenceListResponse
{
    [JsonPropertyName("preferences")]
    public List<JsonElement>? Preferences { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
}
