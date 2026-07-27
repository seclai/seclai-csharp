using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class InboundEmailStatusResponse
{
    [JsonPropertyName("paused")]
    public bool Paused { get; set; }

    [JsonPropertyName("queued_backlog")]
    public int QueuedBacklog { get; set; }
}
