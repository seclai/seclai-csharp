using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class MeResponse
{
    [JsonPropertyName("account_id")]
    public string AccountId { get; set; } = string.Empty;

    [JsonPropertyName("organizations")]
    public List<OrganizationInfoResponse> Organizations { get; set; } = new();
}
