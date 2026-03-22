using System;

namespace Flare.OpenFeature.Provider;

public class FlareProviderOptions
{
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(30);
    public int StaleThreshold { get; set; } = 3;
    public bool CachingEnabled { get; set; } = true;
}
