using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class ChangeStatusRequest
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }
}
