using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class EmailDomainsListResponse
{
    [JsonPropertyName("can_add_custom")]
    public bool? CanAddCustom { get; set; }

    [JsonPropertyName("can_add_vanity")]
    public bool? CanAddVanity { get; set; }

    [JsonPropertyName("custom_plan_names")]
    public List<string>? CustomPlanNames { get; set; }

    [JsonPropertyName("domains")]
    public List<EmailDomainResponse>? Domains { get; set; }

    [JsonPropertyName("has_custom")]
    public bool? HasCustom { get; set; }

    [JsonPropertyName("has_vanity")]
    public bool? HasVanity { get; set; }

    [JsonPropertyName("vanity_plan_names")]
    public List<string>? VanityPlanNames { get; set; }
}
