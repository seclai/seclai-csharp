using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AgentRunRequest
{
    [JsonPropertyName("input")]
    public string? Input { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, JsonElement>? Metadata { get; set; }

    [JsonPropertyName("priority")]
    public bool? Priority { get; set; }

    /// <summary>
    /// ID of a file uploaded with <see cref="SeclaiClient.UploadAgentInputAsync"/> to use as the run input for a dynamic-input trigger.
    /// Mutually exclusive with <see cref="InputUploadIds"/>, which attaches several files. May be combined with <see cref="Input"/>:
    /// the prompt text leads and the file follows under a <c># {filename}</c> heading (before <c>Seclai-Version: 2026-10-16</c>, followed by its extracted text).
    /// The batch must fit the trigger's <c>accepted_attachments</c>, or the run is refused with HTTP 400.
    /// </summary>
    [JsonPropertyName("input_upload_id")]
    public string? InputUploadId { get; set; }

    /// <summary>
    /// IDs of several previously uploaded files, at most 20. Mutually exclusive with <see cref="InputUploadId"/> (two spellings of the
    /// same batch), but may be combined with <see cref="Input"/>: the prompt text leads and the per-file sections follow. Each upload reaches
    /// steps as a separate file. The batch must fit the trigger's <c>accepted_attachments</c>; a mismatch returns HTTP 400 listing what does not fit.
    /// </summary>
    [JsonPropertyName("input_upload_ids")]
    public List<string>? InputUploadIds { get; set; }

    /// <summary>
    /// Re-run this agent reusing a prior run's uploaded input files, re-resolved server-side from that run (which must belong to this account
    /// and agent), so they need not be uploaded again. Combine with <see cref="Input"/> to change the text while keeping the files. A fresh
    /// upload batch (<see cref="InputUploadId"/> or <see cref="InputUploadIds"/>) takes precedence and disables replay. Binaries swept by
    /// retention fall back to their extracted text; files the trigger's <c>accepted_attachments</c> no longer accepts are left out.
    /// </summary>
    [JsonPropertyName("replay_of_run_id")]
    public string? ReplayOfRunId { get; set; }
}
