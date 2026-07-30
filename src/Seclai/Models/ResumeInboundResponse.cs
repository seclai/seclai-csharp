using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class ResumeInboundResponse
{
    [JsonPropertyName("resumed")]
    public bool Resumed { get; set; }
}
