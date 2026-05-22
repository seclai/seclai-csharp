using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>
/// 422 response body for invalid <c>agent_definition</c> payloads on
/// <c>POST /agents</c>, <c>PUT /agents/{id}</c>, and <c>POST /agents/preview-import</c>.
///
/// Errors carry 1-indexed line/column references into the canonical
/// <see cref="Source"/> echo.
/// </summary>
public sealed class AgentDefinitionImportErrorResponse
{
    /// <summary>Discriminator (defaults to <c>invalid_agent_definition</c>).</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>Human-readable summary of the failure.</summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>Per-field errors with source positions.</summary>
    [JsonPropertyName("errors")]
    public List<ImportFieldErrorModel> Errors { get; set; } = new();

    /// <summary>
    /// Canonical pretty-printed echo of the supplied payload.
    /// Each error's <see cref="ImportFieldErrorModel.Line"/> and
    /// <see cref="ImportFieldErrorModel.Column"/> refer to this string.
    /// </summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }
}
