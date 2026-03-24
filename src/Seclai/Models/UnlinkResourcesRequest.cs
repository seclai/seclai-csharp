using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class UnlinkResourcesRequest
{
    [JsonPropertyName("ids")]
    public List<string>? Ids { get; set; }
}
