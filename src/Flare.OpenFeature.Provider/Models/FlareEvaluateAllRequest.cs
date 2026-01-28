using System.Text.Json.Serialization;

namespace OpenFeature.Contrib.Providers.Flare.Models;

internal sealed class FlareEvaluateAllRequest
{
    [JsonPropertyName("context")]
    public FlareEvaluationContext? Context { get; set; }
}
