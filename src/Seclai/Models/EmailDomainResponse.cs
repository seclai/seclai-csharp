using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class EmailDomainResponse
{
    [JsonPropertyName("delegated")]
    public bool? Delegated { get; set; }

    [JsonPropertyName("dns_records")]
    public List<DnsRecordResponse>? DnsRecords { get; set; }

    [JsonPropertyName("domain")]
    public string Domain { get; set; } = string.Empty;

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("is_primary")]
    public bool IsPrimary { get; set; }

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("last_checked_at")]
    public string? LastCheckedAt { get; set; }

    [JsonPropertyName("provider")]
    public DnsProviderResponse? Provider { get; set; }

    [JsonPropertyName("regressing")]
    public bool? Regressing { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("verified")]
    public bool? Verified { get; set; }

    [JsonPropertyName("verified_at")]
    public string? VerifiedAt { get; set; }

    [JsonPropertyName("zone_apex")]
    public string? ZoneApex { get; set; }
}
