using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class CancelQueuedRunsResponse
{
    [JsonPropertyName("cancelled")]
    public int Cancelled { get; set; }
}
