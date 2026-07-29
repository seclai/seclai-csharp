using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AlertDetailResponse
{
    [JsonPropertyName("alert")]
    public AlertResponse? Alert { get; set; }

    [JsonPropertyName("comments")]
    public List<AlertCommentResponse> Comments { get; set; } = new();

    [JsonPropertyName("history")]
    public List<AlertHistoryEntryResponse> History { get; set; } = new();

    [JsonPropertyName("subscribers")]
    public List<AlertSubscriberResponse> Subscribers { get; set; } = new();
}
