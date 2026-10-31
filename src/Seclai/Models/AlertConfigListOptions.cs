namespace Seclai.Models;

/// <summary>Filters and paging for <see cref="SeclaiClient.ListAlertConfigsAsync(AlertConfigListOptions, System.Threading.CancellationToken)"/>.</summary>
public sealed class AlertConfigListOptions
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
