using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Flare.HttpClient.Models;

public sealed class FlareEvaluateAllResponse
{
    [JsonPropertyName("flags")]
    public List<FlagEvaluationResponse> Flags { get; set; } = new();
}
