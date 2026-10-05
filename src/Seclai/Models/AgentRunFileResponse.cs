using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>A file in a run's or a step's output.</summary>
public sealed class AgentRunFileResponse
{
    /// <summary>Size of the file in bytes, when known.</summary>
    [JsonPropertyName("bytes")]
    public long? Bytes { get; set; }

    /// <summary><c>GET</c> URL that streams the file; accepts an API key or OAuth token.</summary>
    [JsonPropertyName("download_url")]
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>File identifier, used to download it.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>MIME type of the file.</summary>
    [JsonPropertyName("mime")]
    public string Mime { get; set; } = string.Empty;

    /// <summary>
    /// The file's name in this run, as sent to email recipients and webhooks and matched by
    /// <c>{{attachments[...]}}</c> selectors.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
