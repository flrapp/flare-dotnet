using System.Text.Json.Serialization;

namespace Flare.HttpClient.Models;

internal sealed class FlareEvaluationRequest
{
    [JsonPropertyName("flagKey")]
    public string FlagKey { get; set; } = string.Empty;

    [JsonPropertyName("context")]
    public FlareEvaluationContext? Context { get; set; }
}
