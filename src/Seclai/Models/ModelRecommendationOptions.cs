namespace Seclai.Models;

/// <summary>Requirements for <see cref="SeclaiClient.GetModelRecommendationsAsync(string, ModelRecommendationOptions, System.Threading.CancellationToken)"/>.</summary>
public sealed class ModelRecommendationOptions
{
    /// <summary>Only recommend models that support tool use.</summary>
    public bool? RequireToolUse { get; set; }

    /// <summary>Only recommend models that support structured output.</summary>
    public bool? RequireStructuredOutput { get; set; }

    /// <summary>Only recommend models that support thinking or reasoning.</summary>
    public bool? RequireThinking { get; set; }

    /// <summary>Minimum context window, in tokens.</summary>
    public int? MinContextTokens { get; set; }

    /// <summary>Minimum output token limit.</summary>
    public int? MinOutputTokens { get; set; }
}
