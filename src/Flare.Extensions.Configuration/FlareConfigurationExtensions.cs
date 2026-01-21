using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Flare.Extensions.Configuration;

public static class FlareConfigurationExtensions
{
    public static IConfigurationBuilder AddFlare(
        this IConfigurationBuilder builder,
        Action<FlareConfigurationOptions> configure)
    {
        var source = new FlareConfigurationSource();
        configure(source.Options);
        
        if (builder is ConfigurationManager configManager)
        {
            var loggerFactory = configManager.GetType()
                .GetProperty("LoggerFactory")?
                .GetValue(configManager) as ILoggerFactory;
        
            source.LoggerFactory = loggerFactory;
        }
        
        if (string.IsNullOrWhiteSpace(source.Options.ServerUrl))
            throw new ArgumentException("ServerUrl is required", nameof(FlareConfigurationOptions.ServerUrl));
    
        if (string.IsNullOrWhiteSpace(source.Options.ApiKey))
            throw new ArgumentException("ApiKey is required", nameof(FlareConfigurationOptions.ApiKey));
    
        if (string.IsNullOrWhiteSpace(source.Options.ScopeAlias))
            throw new ArgumentException("ScopeAlias is required", nameof(FlareConfigurationOptions.ScopeAlias));
        
        return builder.Add(source);
    }
}