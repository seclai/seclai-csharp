using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class TestDraftEvaluationResponse
{
    /// <summary>
    /// <c>passed</c>, <c>failed</c>, or <c>error</c> when the evaluator returned no verdict.
    /// On <c>error</c>, <see cref="Passed"/> and <see cref="Score"/> are not a verdict.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>False when <see cref="Status"/> is <c>error</c>.</summary>
    [JsonPropertyName("passed")]
    [JsonConverter(typeof(NullAsDefaultConverter<bool>))]
    public bool Passed { get; set; }

    /// <summary>
    /// Not a score when <see cref="Status"/> is <c>error</c>: 0 from API version <c>2026-10-21</c>,
    /// which sends <c>null</c>, and 0.5 before it.
    /// </summary>
    [JsonPropertyName("score")]
    [JsonConverter(typeof(NullAsDefaultConverter<float>))]
    public float Score { get; set; }

    [JsonPropertyName("explanation")]
    public string? Explanation { get; set; }
}

// Reads JSON null as default(T), so the property keeps its released non-nullable type.
internal sealed class NullAsDefaultConverter<T> : JsonConverter<T>
    where T : struct
{
    public override bool HandleNull => true;

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? default : JsonSerializer.Deserialize<T>(ref reader, options);

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value, options);
}
