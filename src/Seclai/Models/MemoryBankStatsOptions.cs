namespace Seclai.Models;

/// <summary>Period for <see cref="SeclaiClient.GetMemoryBankStatsAsync(string, MemoryBankStatsOptions, System.Threading.CancellationToken)"/>.</summary>
public sealed class MemoryBankStatsOptions
{
    /// <summary>Days back to cover (server default 30).</summary>
    public int? Days { get; set; }

    /// <summary>Start of the period, as an ISO 8601 date (<c>yyyy-MM-dd</c>).</summary>
    public string? StartDate { get; set; }

    /// <summary>End of the period, as an ISO 8601 date (<c>yyyy-MM-dd</c>).</summary>
    public string? EndDate { get; set; }
}
