using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AlertResponse
{
    [JsonPropertyName("account_id")]
    public string AccountId { get; set; } = string.Empty;

    [JsonPropertyName("agent_id")]
    public string? AgentId { get; set; }

    [JsonPropertyName("agent_run_id")]
    public string? AgentRunId { get; set; }

    [JsonPropertyName("alert_config_id")]
    public string? AlertConfigId { get; set; }

    [JsonPropertyName("alert_type")]
    public string AlertType { get; set; } = string.Empty;

    [JsonPropertyName("comment_count")]
    public int CommentCount { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("details")]
    public string? Details { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("is_subscribed")]
    public bool IsSubscribed { get; set; }

    [JsonPropertyName("mcp_client_id")]
    public string? McpClientId { get; set; }

    [JsonPropertyName("source_connection_id")]
    public string? SourceConnectionId { get; set; }

    [JsonPropertyName("source_connection_pull_id")]
    public string? SourceConnectionPullId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("subscriber_count")]
    public int SubscriberCount { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }
}
