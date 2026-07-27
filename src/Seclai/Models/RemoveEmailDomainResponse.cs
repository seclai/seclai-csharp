using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class RemoveEmailDomainResponse
{
    [JsonPropertyName("cleanup_note")]
    public string? CleanupNote { get; set; }

    [JsonPropertyName("removed")]
    public bool? Removed { get; set; }
}
