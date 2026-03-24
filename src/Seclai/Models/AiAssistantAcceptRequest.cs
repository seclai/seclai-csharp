using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class AiAssistantAcceptRequest
{
    [JsonPropertyName("solution_name")]
    public string? SolutionName { get; set; }

    [JsonPropertyName("solution_description")]
    public string? SolutionDescription { get; set; }

    [JsonPropertyName("confirm_deletions")]
    public bool? ConfirmDeletions { get; set; }
}
