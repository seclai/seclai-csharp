using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class ExportResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("download_url")]
    public string? DownloadUrl { get; set; }

    [JsonPropertyName("expires_at")]
    public string? ExpiresAt { get; set; }

    /// <summary>Source connection ID.</summary>
    [JsonPropertyName("source_connection_id")]
    public string? SourceConnectionId { get; set; }

    /// <summary>Owning account ID.</summary>
    [JsonPropertyName("account_id")]
    public string? AccountId { get; set; }

    /// <summary>Storage destination.</summary>
    [JsonPropertyName("destination")]
    public string? Destination { get; set; }

    /// <summary>Date-from filter applied to this export.</summary>
    [JsonPropertyName("date_from")]
    public string? DateFrom { get; set; }

    /// <summary>Date-to filter applied to this export.</summary>
    [JsonPropertyName("date_to")]
    public string? DateTo { get; set; }

    /// <summary>Metadata filter applied to this export.</summary>
    [JsonPropertyName("metadata_filter")]
    public Dictionary<string, JsonElement>? MetadataFilter { get; set; }

    /// <summary>Query filter applied to this export.</summary>
    [JsonPropertyName("query_filter")]
    public string? QueryFilter { get; set; }

    /// <summary>Pre-run size estimate.</summary>
    [JsonPropertyName("estimated_size_bytes")]
    public long? EstimatedSizeBytes { get; set; }

    /// <summary>File size in bytes.</summary>
    [JsonPropertyName("file_size_bytes")]
    public long? FileSizeBytes { get; set; }

    /// <summary>Items exported.</summary>
    [JsonPropertyName("item_count")]
    public int? ItemCount { get; set; }

    /// <summary>Items processed so far.</summary>
    [JsonPropertyName("progress_current")]
    public int? ProgressCurrent { get; set; }

    /// <summary>Total items to process.</summary>
    [JsonPropertyName("progress_total")]
    public int? ProgressTotal { get; set; }

    /// <summary>Error message if the export failed.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>Storage key of the exported file.</summary>
    [JsonPropertyName("storage_key")]
    public string? StorageKey { get; set; }

    /// <summary>ID of the user who requested this export.</summary>
    [JsonPropertyName("requested_by_user_id")]
    public string? RequestedByUserId { get; set; }

    /// <summary>Name of the user who requested this export.</summary>
    [JsonPropertyName("requested_by_user_name")]
    public string? RequestedByUserName { get; set; }

    /// <summary>Processing start time.</summary>
    [JsonPropertyName("started_at")]
    public string? StartedAt { get; set; }

    /// <summary>Completion time.</summary>
    [JsonPropertyName("completed_at")]
    public string? CompletedAt { get; set; }

    /// <summary>Last update time.</summary>
    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }
}
