namespace Seclai.Models;

/// <summary>Filters, sorting and paging for <see cref="SeclaiClient.ListSolutionsAsync(SolutionListOptions, System.Threading.CancellationToken)"/>.</summary>
/// <remarks>A struct rather than a class so that a bare <c>null</c> argument cannot convert to it and binds to the method's original overload.</remarks>
public struct SolutionListOptions
{
    /// <summary>1-indexed page number.</summary>
    public int? Page { get; set; }

    /// <summary>Items per page (server default 20).</summary>
    public int? Limit { get; set; }

    /// <summary>Sort field (server default <c>created_at</c>).</summary>
    public string? Sort { get; set; }

    /// <summary>Sort direction: <c>asc</c> or <c>desc</c>.</summary>
    public string? Order { get; set; }

    /// <summary>Only solutions whose name contains this text, case-insensitively.</summary>
    public string? Search { get; set; }
}
