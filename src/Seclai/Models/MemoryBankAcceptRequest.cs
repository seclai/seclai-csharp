using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class MemoryBankAcceptRequest
{
    [JsonPropertyName("accepted")]
    public bool Accepted { get; set; }
}
