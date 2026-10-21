namespace Seclai.Models;

/// <summary>
/// An evaluation-criteria field that <see cref="SeclaiClient.ClearEvaluationCriteriaFieldsAsync(string, EvaluationCriteriaField[])"/>
/// can clear by sending it as <c>null</c>.
/// </summary>
public enum EvaluationCriteriaField
{
    /// <summary><c>description</c>: removes the criterion's description.</summary>
    Description,

    /// <summary><c>evaluation_prompt</c>: removes the custom evaluation prompt.</summary>
    EvaluationPrompt,

    /// <summary><c>expectation_config</c>: removes the expectation configuration.</summary>
    ExpectationConfig,

    /// <summary>
    /// <c>evaluation_tier</c>: removes the tier pinned on the criterion, so it falls back to the agent's
    /// default evaluation tier. This is the only way to undo a pinned tier.
    /// </summary>
    EvaluationTier,
}
