using Microsoft.Extensions.Configuration;

namespace Flare.Extensions.Configuration;

public class FlareConfigurationSource : IConfigurationSource
{
    public FlareConfigurationOptions Options { get; set; } = new();

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new FlareConfigurationProvider(this);
    }
}