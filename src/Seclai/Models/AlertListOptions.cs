using System;

namespace Seclai.Models;

/// <summary>Filters and paging for <see cref="SeclaiClient.ListAlertsAsync(AlertListOptions, System.Threading.CancellationToken)"/>.</summary>
public sealed class AlertListOptions
{
    /// <summary>1-indexed page number.</summary>
    public int? Page { get; set; }

    /// <summary>Items per page (server default 20).</summary>
    public int? Limit { get; set; }

    /// <summary>Only alerts in this status.</summary>
    public string? Status { get; set; }

    /// <summary>Only alerts for this agent.</summary>
    public string? AgentId { get; set; }

    /// <summary>Only alerts for this source connection.</summary>
    public string? SourceConnectionId { get; set; }

    /// <summary>Only alerts from this time on.</summary>
    public DateTimeOffset? TimeFrom { get; set; }

    /// <summary>Only alerts up to this time.</summary>
    public DateTimeOffset? TimeTo { get; set; }
}
