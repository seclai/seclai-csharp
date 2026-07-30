using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>Set the account's governance auto-block mode (shared REST request).</summary>
public sealed class SetAutoBlockModeRequest
{
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = string.Empty;
}
