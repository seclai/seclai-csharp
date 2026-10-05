using System.Text.Json.Serialization;

namespace Seclai.Models;

/// <summary>Response model for one content item's indexing status.</summary>
public sealed class SourceContentStatusResponse
{
    /// <summary>
    /// True when the item is linked and reports completed but its content is not yet embedded
    /// under the index the source connection currently uses, because it still sits under the
    /// index that connection used before an embedding migration switched it. Anything ingested
    /// while a migration ran can land in this state. Semantic and content search will not match
    /// it until it is re-embedded; a title keyword match can still return it, so the item may
    /// appear in results while its body is unsearchable. It clears on its own — a
    /// reconciliation pass re-embeds the item under the current index, typically within minutes
    /// of the migration finishing, and a daily sweep retries whatever is still outstanding, so
    /// a large backlog can take more than one sweep to drain. The re-embedding is not charged
    /// to your account: nothing you did caused it, so Seclai absorbs the cost. Never true for
    /// an item that is simply still indexing; content_status covers that.
    /// </summary>
    [JsonPropertyName("awaiting_reindex")]
    public bool AwaitingReindex { get; set; }

    /// <summary>Indexing status: pending, fetching, transcribing, scanning, indexing, completed, or failed.</summary>
    [JsonPropertyName("content_status")]
    public string ContentStatus { get; set; } = string.Empty;

    /// <summary>Extracted token count.</summary>
    [JsonPropertyName("content_token_count")]
    public int? ContentTokenCount { get; set; }

    /// <summary>Content type group: text, audio, video, image, or document.</summary>
    [JsonPropertyName("content_type")]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Internal URL identifying the item. Uploaded files use a <c>file-upload://</c> URL.</summary>
    [JsonPropertyName("content_url")]
    public string? ContentUrl { get; set; }

    /// <summary>
    /// ID of the content version. This is the <c>content_version_id</c> returned by the upload
    /// endpoints, so it is what you match an upload against.
    /// </summary>
    [JsonPropertyName("content_version_id")]
    public string ContentVersionId { get; set; } = string.Empty;

    /// <summary>Extracted word count.</summary>
    [JsonPropertyName("content_word_count")]
    public int? ContentWordCount { get; set; }

    /// <summary>Why the item failed, when <c>content_status</c> is <c>failed</c>.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// True when extraction stopped with media still unread, so the item references more media
    /// than was indexed and media search will not match anything past the cut. Two causes: a
    /// web page that ran out of the budget for fetching remote assets, or a container that
    /// could not be read to the end (a truncated or hostile archive). An uploaded document that
    /// reads cleanly is never capped, however much media it holds — there is no limit on that.
    /// </summary>
    [JsonPropertyName("extracted_media_capped")]
    public bool ExtractedMediaCapped { get; set; }

    /// <summary>
    /// Number of embedded images / videos extracted from inside this item and indexed as their
    /// own chunks. There is no limit on this — a document contributes as many as it holds. Null
    /// when there is no media record for the item: the extraction pass has not run, does not
    /// apply to this container, or found nothing. Treat null as 'unknown', never as zero.
    /// </summary>
    [JsonPropertyName("extracted_media_count")]
    public int? ExtractedMediaCount { get; set; }

    /// <summary>
    /// The bound that was reached, when extracted_media_capped is true and the stop was a bound
    /// — a number of fetch attempts, or a number of seconds. Null when extraction was not
    /// capped, or when it stopped because the container could not be read rather than because a
    /// bound fired.
    /// </summary>
    [JsonPropertyName("extracted_media_limit")]
    public int? ExtractedMediaLimit { get; set; }

    /// <summary>Timestamp when the item finished indexing and became retrievable. <c>null</c> until then.</summary>
    [JsonPropertyName("indexed_at")]
    public string? IndexedAt { get; set; }

    /// <summary>MIME type the item was ingested as, when known.</summary>
    [JsonPropertyName("mime_type")]
    public string? MimeType { get; set; }

    /// <summary>Publication timestamp of the item, when known.</summary>
    [JsonPropertyName("published_at")]
    public string? PublishedAt { get; set; }

    /// <summary>Timestamp when the item was uploaded or pulled.</summary>
    [JsonPropertyName("pulled_at")]
    public string PulledAt { get; set; } = string.Empty;

    /// <summary>
    /// ID to pass to <c>GET /contents/{id}</c>. <c>null</c> until the item has finished
    /// indexing — an item that is still processing, or that failed, has no retrievable content
    /// and keeps this <c>null</c>.
    /// </summary>
    [JsonPropertyName("source_connection_content_version_id")]
    public string? SourceConnectionContentVersionId { get; set; }

    /// <summary>Title of the content item.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }
}
