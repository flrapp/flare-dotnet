using System;
using System.Text.Json.Serialization;

namespace Flare.HttpClient.Models;

public sealed class FlagMetadata
{
    [JsonPropertyName("scopeAlias")]
    public string? ScopeAlias { get; set; }

    [JsonPropertyName("scopeId")]
    public Guid? ScopeId { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; set; }
}
