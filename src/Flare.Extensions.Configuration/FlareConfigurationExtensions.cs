using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Flare.Extensions.Configuration;

public static class FlareConfigurationExtensions
{
    public static IConfigurationBuilder AddFlareConfiguration(
        this IConfigurationBuilder builder)
    {
        var source = new FlareConfigurationSource(FlareConfigurationObserver.Instance);
        return builder.Add(source);
    }

    public static IServiceCollection AddFlareBackgroundService(this IServiceCollection services, IConfiguration configuration, string flareOptionsSection)
    {
        services.Configure<FlareConfigurationOptions>(configuration.GetSection(flareOptionsSection));

        services.AddSingleton(FlareConfigurationObserver.Instance);
        services.AddHttpClient("FlareFeatureToggle", (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<FlareConfigurationOptions>>().Value;
            client.BaseAddress = new Uri(options.ServerUrl);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ApiKey}");
        });
        
        services.AddHostedService<FlareBackgroundService>();

        return services;
    }
    
    public static IServiceCollection AddFlareBackgroundService(this IServiceCollection services, 
        Action<FlareConfigurationOptions> configure)
    {
        services.Configure(configure);
        
        services.AddSingleton(FlareConfigurationObserver.Instance);
        services.AddHttpClient("FlareFeatureToggle", (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<FlareConfigurationOptions>>().Value;
            client.BaseAddress = new Uri(options.ServerUrl);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ApiKey}");
        });
        
        services.AddHostedService<FlareBackgroundService>();

        return services;
    }
}