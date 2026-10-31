namespace Seclai.Models;

/// <summary>Filters, sorting and paging for <see cref="SeclaiClient.ListMemoryBanksAsync(MemoryBankListOptions, System.Threading.CancellationToken)"/>.</summary>
public sealed class MemoryBankListOptions
{
    /// <summary>1-indexed page number.</summary>
    public int? Page { get; set; }

    /// <summary>Items per page (server default 20).</summary>
    public int? Limit { get; set; }

    /// <summary>Sort field: <c>created_at</c>, <c>updated_at</c> or <c>name</c>.</summary>
    public string? Sort { get; set; }

    /// <summary>Sort direction: <c>asc</c> or <c>desc</c>.</summary>
    public string? Order { get; set; }

    /// <summary>Only banks of this type: <c>conversation</c> or <c>general</c>.</summary>
    public string? Type { get; set; }
}
