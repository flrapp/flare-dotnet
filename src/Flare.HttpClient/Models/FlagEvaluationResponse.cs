using System.Text.Json.Serialization;

namespace Flare.HttpClient.Models;

public sealed class FlagEvaluationResponse
{
    [JsonPropertyName("flagKey")]
    public string FlagKey { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public object? Value { get; set; }

    [JsonPropertyName("variant")]
    public string? Variant { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// The flag type: "boolean", "string", "number", or "json".
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("flagMetadata")]
    public FlagMetadata? Metadata { get; set; }
}
