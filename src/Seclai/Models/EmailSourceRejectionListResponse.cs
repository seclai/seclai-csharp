using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>A page of mail an email content source refused.</summary>
public sealed class EmailSourceRejectionListResponse
{
    [JsonPropertyName("data")]
    public List<EmailSourceRejectionResponse> Data { get; set; } = new();

    [JsonPropertyName("pagination")]
    public PaginationResponse Pagination { get; set; } = new();
}
