using System.Text.Json.Serialization;

namespace OpenFeature.Contrib.Providers.Flare.Models;

public sealed class FlagEvaluationResponse
{
    [JsonPropertyName("flagKey")]
    public string FlagKey { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public bool Value { get; set; }

    [JsonPropertyName("variant")]
    public string? Variant { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("flagMetadata")]
    public FlagMetadata? Metadata { get; set; }
}
