using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class CreateMemoryBankRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("solution_id")]
    public string? SolutionId { get; set; }

    /// <summary>Conversation banks only. When true, a conversation turn written to this bank has the quoted reply chain an email client prepends to a reply dropped from it. Omitted when null, which leaves it off.</summary>
    [JsonPropertyName("strip_quoted_reply_chains")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? StripQuotedReplyChains { get; set; }
}
