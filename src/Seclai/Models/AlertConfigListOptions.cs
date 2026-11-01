namespace Seclai.Models;

/// <summary>Filters and paging for <see cref="SeclaiClient.ListAlertConfigsAsync(AlertConfigListOptions, System.Threading.CancellationToken)"/>.</summary>
/// <remarks>A struct rather than a class so that a bare <c>null</c> argument cannot convert to it and binds to the method's original overload.</remarks>
public struct AlertConfigListOptions
{
    /// <summary>1-indexed page number.</summary>
    public int? Page { get; set; }

    /// <summary>Items per page (server default 50).</summary>
    public int? Limit { get; set; }

    /// <summary>Only configurations for this agent.</summary>
    public string? AgentId { get; set; }

    /// <summary>Only configurations for this source connection.</summary>
    public string? SourceConnectionId { get; set; }

    /// <summary>Set to <c>source</c> to list account-level source alert configurations.</summary>
    public string? Scope { get; set; }
}
