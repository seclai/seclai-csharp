namespace Seclai.Models;

/// <summary>Filters for <see cref="SeclaiClient.ListModelsAsync(ModelListOptions, System.Threading.CancellationToken)"/>.</summary>
public sealed class ModelListOptions
{
    /// <summary>Only models from this provider.</summary>
    public string? Provider { get; set; }

    /// <summary>Only models that support tool use.</summary>
    public bool? SupportsToolUse { get; set; }

    /// <summary>Only models that support extended thinking.</summary>
    public bool? SupportsThinking { get; set; }

    /// <summary>Only models that accept this input modality: a coarse kind (<c>image</c>, <c>audio</c>, <c>video</c>, <c>pdf</c>) or a full MIME type.</summary>
    public string? SupportsInputMedia { get; set; }

    /// <summary>Only models that produce this output modality: a coarse kind (<c>image</c>, <c>audio</c>, <c>video</c>) or a full MIME type.</summary>
    public string? SupportsOutputMedia { get; set; }
}
