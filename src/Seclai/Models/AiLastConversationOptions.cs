namespace Seclai.Models;

/// <summary>Paging for the memory bank AI assistant's last conversation.</summary>
/// <remarks>A struct rather than a class so that a bare <c>null</c> argument cannot convert to it and binds to the method's original overload.</remarks>
public struct AiLastConversationOptions
{
    /// <summary>Most turns to return (server default 5).</summary>
    public int? Limit { get; set; }

    /// <summary>Number of most-recent turns to skip.</summary>
    public int? Offset { get; set; }
}
