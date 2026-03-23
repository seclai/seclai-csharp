using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class UpdateOrganizationAlertPreferenceRequest
{
    [JsonPropertyName("subscribed")]
    public bool Subscribed { get; set; }
}
