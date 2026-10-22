using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>A page of the files attached to a content item.</summary>
public sealed class ContentAttachmentsListResponse
{
    [JsonPropertyName("data")]
    public List<ContentAttachmentResponse> Data { get; set; } = new();

    [JsonPropertyName("pagination")]
    public PaginationResponse Pagination { get; set; } = new();
}
