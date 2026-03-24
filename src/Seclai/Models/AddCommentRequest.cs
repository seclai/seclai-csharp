using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AddCommentRequest
{
    [JsonPropertyName("body")]
    public string? Body { get; set; }
}
