using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class SourceEmbeddingMigrationResponse
{
    [JsonPropertyName("migration_id")]
    public string? MigrationId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>The migration's ID.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>The source being migrated.</summary>
    [JsonPropertyName("source_connection_id")]
    public string? SourceConnectionId { get; set; }

    /// <summary>The index the source is migrating from.</summary>
    [JsonPropertyName("source_id_old")]
    public string? SourceIdOld { get; set; }

    /// <summary>The index the source is migrating to.</summary>
    [JsonPropertyName("source_id_new")]
    public string? SourceIdNew { get; set; }

    /// <summary>Embedding model the source is migrating to.</summary>
    [JsonPropertyName("target_embedding_model")]
    public string? TargetEmbeddingModel { get; set; }

    /// <summary>Embedding dimensions the source is migrating to.</summary>
    [JsonPropertyName("target_dimensions")]
    public int TargetDimensions { get; set; }

    /// <summary>Current phase of the migration.</summary>
    [JsonPropertyName("phase")]
    public string? Phase { get; set; }

    /// <summary>Items processed so far.</summary>
    [JsonPropertyName("progress_current")]
    public int ProgressCurrent { get; set; }

    /// <summary>Items to process.</summary>
    [JsonPropertyName("progress_total")]
    public int ProgressTotal { get; set; }

    /// <summary>Human-readable progress note.</summary>
    [JsonPropertyName("progress_message")]
    public string? ProgressMessage { get; set; }

    /// <summary>Why the migration failed, when it did.</summary>
    [JsonPropertyName("failure_message")]
    public string? FailureMessage { get; set; }

    /// <summary>Who is notified when the migration ends.</summary>
    [JsonPropertyName("notification_recipients")]
    public List<string>? NotificationRecipients { get; set; }

    /// <summary>The background task running the migration.</summary>
    [JsonPropertyName("task_execution_id")]
    public string? TaskExecutionId { get; set; }

    [JsonPropertyName("started_at")]
    public string? StartedAt { get; set; }

    [JsonPropertyName("completed_at")]
    public string? CompletedAt { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }
}
