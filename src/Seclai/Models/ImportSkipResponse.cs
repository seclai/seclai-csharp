using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>
/// One item dropped or substituted during an agent import.
///
/// Used as the element type for <c>import_warnings</c> on every response model that
/// accepts an <c>agent_definition</c> payload.
/// </summary>
public sealed class ImportSkipResponse
{
    /// <summary>
    /// The kind of item that was skipped or substituted (e.g. <c>schedule</c>,
    /// <c>evaluation_criteria</c>, <c>alert_config</c>, <c>alert_recipient</c>,
    /// <c>governance_policy</c>, <c>governance_kb_link</c>, <c>solution_link</c>).
    /// </summary>
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    /// <summary>Human-readable explanation of what was skipped and why.</summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Category-specific identifiers for the skipped item
    /// (<c>step_id</c>, <c>alert_type</c>, <c>kb_name</c>, etc.).
    /// </summary>
    [JsonPropertyName("details")]
    public Dictionary<string, JsonElement>? Details { get; set; }
}
