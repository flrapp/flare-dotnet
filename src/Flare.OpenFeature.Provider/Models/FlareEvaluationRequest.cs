using System.Text.Json.Serialization;

namespace OpenFeature.Contrib.Providers.Flare.Models;

internal sealed class FlareEvaluationRequest
{
    [JsonPropertyName("flagKey")]
    public string FlagKey { get; set; } = string.Empty;

    [JsonPropertyName("context")]
    public FlareEvaluationContext? Context { get; set; }
}

internal sealed class FlareEvaluationContext
{
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("targetingKey")]
    public string? TargetingKey { get; set; }
}