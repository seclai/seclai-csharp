namespace Seclai.Models;

/// <summary>Options for <see cref="SeclaiClient.DownloadAgentRunAttachmentWithOptionsAsync(string, string, DownloadAgentRunAttachmentOptions, System.Threading.CancellationToken)"/>.</summary>
/// <remarks>
/// A <c>default</c> value downloads exactly as <see cref="SeclaiClient.DownloadAgentRunAttachmentAsync(string, string, string?, System.Threading.CancellationToken)"/>
/// does with no <c>downloadName</c>: the whole file, adding no <c>Range</c> header.
/// <para>
/// The range fields build one <c>Range</c> header. Set none of them, <see cref="RangeStart"/> alone
/// (<c>bytes=start-</c>), <see cref="RangeStart"/> and <see cref="RangeEnd"/> (<c>bytes=start-end</c>), or
/// <see cref="RangeSuffix"/> alone (<c>bytes=-suffix</c>). Any other combination throws
/// <see cref="System.ArgumentException"/> before a request is sent.
/// </para>
/// </remarks>
public struct DownloadAgentRunAttachmentOptions
{
    /// <summary>Optional filename hint for the download disposition, sent as <c>download_name</c>.</summary>
    public string? DownloadName { get; set; }

    /// <summary>
    /// First byte to return, zero-based. Must not be negative. Alone it asks for the rest of the file
    /// from this offset; with <see cref="RangeEnd"/>, for the bytes from this offset to that one.
    /// </summary>
    public long? RangeStart { get; set; }

    /// <summary>
    /// Last byte to return, zero-based and inclusive. Requires <see cref="RangeStart"/>, and must not be
    /// less than it.
    /// </summary>
    public long? RangeEnd { get; set; }

    /// <summary>
    /// Number of bytes to return from the end of the file. Must be greater than zero, and cannot be
    /// combined with <see cref="RangeStart"/> or <see cref="RangeEnd"/>.
    /// </summary>
    public long? RangeSuffix { get; set; }
}
