using System;

namespace Flare.Extensions.Configuration;

public class FlareConfigurationOptions
{
    public string ServerUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ScopeAlias { get; set; } = string.Empty;
    public TimeSpan ReloadInterval { get; set; } = TimeSpan.Zero;
    public string FeatureFlagSection { get; set; } = "FeatureFlags";
}