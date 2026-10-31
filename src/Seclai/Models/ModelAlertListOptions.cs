namespace Seclai.Models;

/// <summary>Filters and paging for <see cref="SeclaiClient.ListModelAlertsAsync(ModelAlertListOptions, System.Threading.CancellationToken)"/>.</summary>
public sealed class ModelAlertListOptions
{
    /// <summary>1-indexed page number, sent as the <c>offset</c> the endpoint declares.</summary>
    public int? Page { get; set; }

    /// <summary>Most alerts to return, 1-100 (server default 50).</summary>
    public int? Limit { get; set; }

    /// <summary>Only alerts for this agent.</summary>
    public string? AgentId { get; set; }

    /// <summary>When true, only unread alerts.</summary>
    public bool? UnreadOnly { get; set; }
}
