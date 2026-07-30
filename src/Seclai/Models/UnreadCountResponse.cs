using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class UnreadCountResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }
}
