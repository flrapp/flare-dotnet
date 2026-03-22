using System;
using Flare.HttpClient;
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

    public static IServiceCollection AddFlareBackgroundService(
        this IServiceCollection services,
        IConfiguration configuration,
        string flareOptionsSection)
    {
        services.Configure<FlareConfigurationOptions>(configuration.GetSection(flareOptionsSection));

        services.AddSingleton(FlareConfigurationObserver.Instance);

        services.AddFlareHttpClientFromOptions();
        services.AddHostedService<FlareBackgroundService>();

        return services;
    }

    public static IServiceCollection AddFlareBackgroundService(
        this IServiceCollection services,
        Action<FlareConfigurationOptions> configure)
    {
        services.Configure(configure);

        services.AddSingleton(FlareConfigurationObserver.Instance);

        services.AddFlareHttpClientFromOptions();
        services.AddHostedService<FlareBackgroundService>();

        return services;
    }

    private static void AddFlareHttpClientFromOptions(this IServiceCollection services)
    {
        services.AddFlareHttpClient(sp =>
        {
            var options = sp.GetRequiredService<IOptions<FlareConfigurationOptions>>().Value;
            return new FlareApiClientOptions
            {
                BaseUrl = options.ServerUrl,
                ApiKey = options.ApiKey,
                Scope = options.ScopeAlias
            };
        });
    }
}
