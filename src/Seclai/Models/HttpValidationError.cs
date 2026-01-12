using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Seclai.Models;

public sealed class HttpValidationError
{
    [JsonPropertyName("detail")]
    public List<ValidationError>? Detail { get; set; }
}
