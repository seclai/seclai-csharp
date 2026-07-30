using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>A page of blocked senders + the account's auto-block mode.</summary>
public sealed class BlockedEmailSenderListResponse
{
    [JsonPropertyName("auto_block_mode")]
    public string AutoBlockMode { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public List<BlockedEmailSenderResponse> Items { get; set; } = new();

    [JsonPropertyName("total")]
    public int Total { get; set; }
}
