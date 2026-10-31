namespace Seclai.Models;

/// <summary>Paging for the memory bank AI assistant's last conversation.</summary>
public sealed class AiLastConversationOptions
{
    /// <summary>Most turns to return (server default 5).</summary>
    public int? Limit { get; set; }

    /// <summary>Number of most-recent turns to skip.</summary>
    public int? Offset { get; set; }
}
