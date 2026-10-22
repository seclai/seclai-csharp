using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>Mail an email content source refused: who sent it and why, never what it said.</summary>
public sealed class EmailSourceRejectionResponse
{
    /// <summary>Unique identifier of the refusal.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>When the mail was refused.</summary>
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    /// <summary>The source address it was sent to.</summary>
    [JsonPropertyName("recipient")]
    public string Recipient { get; set; } = string.Empty;

    /// <summary>The envelope sender. For bulk mail this is the delivery provider's bounce address, not the From address.</summary>
    [JsonPropertyName("sender")]
    public string Sender { get; set; } = string.Empty;

    /// <summary>The address in the message's From line, which the source's allowed senders are matched against. Null when no single From address could be read unambiguously.</summary>
    [JsonPropertyName("from_address")]
    public string? FromAddress { get; set; }

    /// <summary>IP address the mail came from.</summary>
    [JsonPropertyName("sender_ip")]
    public string? SenderIp { get; set; }

    /// <summary>
    /// Why it was refused: <c>sender_not_allowed</c>, <c>unauthenticated_sender</c>, <c>unreadable_from</c>,
    /// <c>sender_blocked</c>, <c>spam</c>, <c>virus</c>, <c>unsafe_content</c>, <c>rate_limited</c>,
    /// <c>account_not_chargeable</c> or <c>empty_message</c>; or, for a message the source received before,
    /// <c>repeat_stored</c>, <c>repeat_held</c>, <c>repeat_deleted</c> or <c>repeat_in_progress</c>.
    /// </summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>Whether the message passed DMARC. Adding an unauthenticated sender to the allowed senders does not let its mail in.</summary>
    [JsonPropertyName("authenticated")]
    public bool Authenticated { get; set; }

    /// <summary>The SPF, DKIM and DMARC verdicts reported for the message.</summary>
    [JsonPropertyName("auth_verdicts")]
    public Dictionary<string, string>? AuthVerdicts { get; set; }
}
