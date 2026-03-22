using System.Text.Json.Serialization;

namespace Flare.HttpClient.Models;

internal sealed class FlareEvaluateAllRequest
{
    [JsonPropertyName("context")]
    public FlareEvaluationContext? Context { get; set; }
}
