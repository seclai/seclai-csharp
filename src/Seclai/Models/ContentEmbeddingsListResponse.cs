using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class ContentEmbeddingsListResponse
{
    [JsonPropertyName("data")]
    public List<ContentEmbeddingResponse> Data { get; set; } = new();

    [JsonPropertyName("pagination")]
    public PaginationResponse Pagination { get; set; } = new();
}
