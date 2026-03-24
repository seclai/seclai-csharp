using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class MemoryBankListResponse
{
    [JsonPropertyName("data")]
    public List<MemoryBankResponse> Data { get; set; } = new();

    [JsonPropertyName("total")]
    public int Total { get; set; }
}
