using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class SendTestEmailResponse
{
    [JsonPropertyName("sent")]
    public bool? Sent { get; set; }
}
