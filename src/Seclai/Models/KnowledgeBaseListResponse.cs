using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class KnowledgeBaseListResponse
{
    [JsonPropertyName("data")]
    public List<KnowledgeBaseResponse> Data { get; set; } = new();

    [JsonPropertyName("total")]
    public int Total { get; set; }
}
