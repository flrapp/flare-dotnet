using Microsoft.Extensions.Configuration;

namespace Flare.Extensions.Configuration;

public class FlareConfigurationSource(FlareConfigurationObserver observer) : IConfigurationSource
{
    
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new FlareConfigurationProvider(observer);
    }
}