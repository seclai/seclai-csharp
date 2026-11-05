using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AgentRunStreamRequest
{
    [JsonPropertyName("input")]
    public string? Input { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, JsonElement>? Metadata { get; set; }

    /// <summary>
    /// Receive <c>stream_reasoning</c> events carrying the model's reasoning while it thinks. They are sent
    /// only for a <c>streaming_result</c> step whose author enabled <c>show_reasoning</c>, and never when a
    /// governance policy redacts that step. Null omits the field (server default false). It applies to
    /// <see cref="SeclaiClient.RunStreamingAgentAsync"/>. <see cref="SeclaiClient.RunStreamingAgentAndWaitAsync"/>
    /// reads only the <c>init</c> and <c>done</c> events and does not send this field, so reasoning events never
    /// count against its timeout.
    /// </summary>
    [JsonPropertyName("include_reasoning")]
    public bool? IncludeReasoning { get; set; }

    /// <summary>ID of a previously uploaded file to use as the run input for a dynamic-input trigger. Mutually exclusive with <see cref="InputUploadIds"/>; may be combined with <see cref="Input"/>. Must fit the trigger's <c>accepted_attachments</c>, as on <see cref="AgentRunRequest.InputUploadId"/>.</summary>
    [JsonPropertyName("input_upload_id")]
    public string? InputUploadId { get; set; }

    /// <summary>IDs of several previously uploaded files, at most 20. See <see cref="AgentRunRequest.InputUploadIds"/>: the batch must fit the trigger's <c>accepted_attachments</c> (else HTTP 400) and may be combined with <see cref="Input"/>.</summary>
    [JsonPropertyName("input_upload_ids")]
    public List<string>? InputUploadIds { get; set; }

    /// <summary>Re-run reusing a prior run's uploaded input files, re-resolved server-side from that run (which must belong to this account and agent). A fresh upload batch takes precedence. Files the trigger's <c>accepted_attachments</c> no longer accepts are left out.</summary>
    [JsonPropertyName("replay_of_run_id")]
    public string? ReplayOfRunId { get; set; }
}
