namespace Seclai.Models;

/// <summary>Filters for the agent AI assistant conversation history.</summary>
public sealed class AiConversationHistoryOptions
{
    /// <summary>Step type to look up. Required by the API.</summary>
    public string? StepType { get; set; }

    /// <summary>Filter to a single step.</summary>
    public string? StepId { get; set; }

    /// <summary>Max turns to return (1-50, default 10).</summary>
    public int? Limit { get; set; }

    /// <summary>Number of recent turns to skip.</summary>
    public int? Offset { get; set; }
}
