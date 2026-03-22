using Flare.OpenFeature.Provider;
using FluentAssertions;

namespace Flare.OpenFeature.Provider.Tests;

public class FlareProviderOptionsTests
{
    [Fact]
    public void Defaults_PollingInterval_Is30Seconds()
    {
        var options = new FlareProviderOptions();

        options.PollingInterval.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Defaults_StaleThreshold_Is3()
    {
        var options = new FlareProviderOptions();

        options.StaleThreshold.Should().Be(3);
    }

    [Fact]
    public void Defaults_CachingEnabled_IsTrue()
    {
        var options = new FlareProviderOptions();

        options.CachingEnabled.Should().BeTrue();
    }
}
