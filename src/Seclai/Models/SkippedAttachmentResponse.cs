using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>A part of an email that was not stored as an attachment.</summary>
public sealed class SkippedAttachmentResponse
{
    /// <summary>The file name the part is listed under; null when it had none.</summary>
    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    /// <summary>The part's declared media type.</summary>
    [JsonPropertyName("content_type")]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Decoded size in bytes; null for a part that was never decoded.</summary>
    [JsonPropertyName("size_bytes")]
    public long? SizeBytes { get; set; }

    /// <summary>Why the part was not stored: <c>displayed_in_body</c>, <c>over_cap</c>, <c>no_filename</c> or <c>undecodable</c>.</summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}
