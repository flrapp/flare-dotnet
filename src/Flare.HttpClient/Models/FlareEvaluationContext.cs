using System.Text.Json.Serialization;

namespace Flare.HttpClient.Models;

public sealed class FlareEvaluationContext
{
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("targetingKey")]
    public string? TargetingKey { get; set; }
}
