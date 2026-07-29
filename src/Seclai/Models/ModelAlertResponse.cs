using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class ModelAlertResponse
{
    [JsonPropertyName("account_id")]
    public string AccountId { get; set; } = string.Empty;

    [JsonPropertyName("agent_id")]
    public string? AgentId { get; set; }

    [JsonPropertyName("alert_type")]
    public string AlertType { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("model_name")]
    public string ModelName { get; set; } = string.Empty;

    [JsonPropertyName("prompt_model_id")]
    public string PromptModelId { get; set; } = string.Empty;

    [JsonPropertyName("read_at")]
    public string? ReadAt { get; set; }

    [JsonPropertyName("successor_model_name")]
    public string? SuccessorModelName { get; set; }
}
