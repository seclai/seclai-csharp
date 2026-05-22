using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>
/// Single <c>agent_definition</c> validation error with source position.
/// Carried as elements of <see cref="AgentDefinitionImportErrorResponse.Errors"/>.
/// </summary>
public sealed class ImportFieldErrorModel
{
    /// <summary>1-indexed line in <see cref="AgentDefinitionImportErrorResponse.Source"/>.</summary>
    [JsonPropertyName("line")]
    public int Line { get; set; }

    /// <summary>1-indexed column in <see cref="AgentDefinitionImportErrorResponse.Source"/>.</summary>
    [JsonPropertyName("column")]
    public int Column { get; set; }

    /// <summary>Dotted path of the offending field (e.g. <c>agent.definition.child_steps[0].step_type</c>).</summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>Human-readable description of the problem.</summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
