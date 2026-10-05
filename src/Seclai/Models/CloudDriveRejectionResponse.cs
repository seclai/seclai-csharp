using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>One file the connection deliberately did not process.</summary>
public sealed class CloudDriveRejectionResponse
{
    /// <summary>When the file was skipped.</summary>
    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    /// <summary>Extra context, e.g. the cap that was hit.</summary>
    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    /// <summary>The provider's file id, when the file was known.</summary>
    [JsonPropertyName("file_id")]
    public string? FileId { get; set; }

    /// <summary>Path of the skipped file, when known.</summary>
    [JsonPropertyName("file_path")]
    public string? FilePath { get; set; }

    /// <summary>Rejection identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary><c>too_large</c>, <c>download_failed</c>, or <c>flood</c>.</summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}
