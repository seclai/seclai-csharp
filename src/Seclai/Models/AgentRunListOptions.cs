namespace Seclai.Models;

/// <summary>Filters and paging for <see cref="SeclaiClient.ListAgentRunsAsync(string, AgentRunListOptions, System.Threading.CancellationToken)"/>.</summary>
/// <remarks>A struct rather than a class so that a bare <c>null</c> argument cannot convert to it and binds to the method's original overload.</remarks>
public struct AgentRunListOptions
{
    /// <summary>1-indexed page number.</summary>
    public int? Page { get; set; }

    /// <summary>Items per page (server default 50).</summary>
    public int? Limit { get; set; }

    /// <summary>Only runs in this status: <c>pending</c>, <c>processing</c>, <c>completed</c> or <c>failed</c>.</summary>
    public string? Status { get; set; }
}
