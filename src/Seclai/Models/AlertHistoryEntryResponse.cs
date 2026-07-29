using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AlertHistoryEntryResponse
{
    [JsonPropertyName("changed_by_name")]
    public string? ChangedByName { get; set; }

    [JsonPropertyName("changed_by_user_id")]
    public string? ChangedByUserId { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("new_status")]
    public string NewStatus { get; set; } = string.Empty;

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("previous_status")]
    public string? PreviousStatus { get; set; }
}
