using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenFeature.Contrib.Providers.Flare.Models;

internal sealed class FlareEvaluateAllResponse
{
    [JsonPropertyName("flags")]
    public List<FlagEvaluationResponse> Flags { get; set; } = new();
}
