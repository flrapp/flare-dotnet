using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Flare.Extensions.Configuration;

public class FlareConfigurationSource : IConfigurationSource
{
    public FlareConfigurationOptions Options { get; set; } = new();
    public ILoggerFactory? LoggerFactory { get; set; }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new FlareConfigurationProvider(this);
    }
}