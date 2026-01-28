using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Flare.Extensions.Configuration;

public class FlareConfigurationProvider : ConfigurationProvider
{
    private Dictionary<string, string?> _rawData = new();

    public FlareConfigurationProvider(FlareConfigurationObserver observer)
    {
        observer.AddListener(data => {
            _rawData = data;
            Load();
        });
    }

    public override void Load()
    {
        Data = _rawData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString());
        OnReload();
    }
}