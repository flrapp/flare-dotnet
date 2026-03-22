using System;
using Flare.HttpClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenFeature;

namespace Flare.OpenFeature.Provider;

/// <summary>
/// Extension methods for registering Flare OpenFeature provider in the DI container.
/// </summary>
/// <example>
/// Usage with OpenFeature:
/// <code>
/// services.AddFlareProvider(options =>
/// {
///     options.BaseUrl = "https://api.flare.example.com";
///     options.ApiKey = "your-api-key";
///     options.Scope = "production";
/// });
///
/// services.AddOpenFeature(builder =>
/// {
///     builder.AddProvider(sp => sp.GetRequiredService&lt;FlareProvider&gt;());
/// });
/// </code>
///
/// Or with configuration:
/// <code>
/// // appsettings.json:
/// // {
/// //   "Flare": {
/// //     "BaseUrl": "https://api.flare.example.com",
/// //     "ApiKey": "your-api-key",
/// //     "Scope": "production"
/// //   }
/// // }
///
/// services.AddFlareProvider(configuration);
///
/// services.AddOpenFeature(builder =>
/// {
///     builder.AddProvider(sp => sp.GetRequiredService&lt;FlareProvider&gt;());
/// });
/// </code>
/// </example>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Flare provider and related services using an options configuration action.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure FlareProviderOptions.</param>
    /// <param name="options"></param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFlareProvider(
        this IServiceCollection services,
        FlareApiClientOptions options,
        FlareProviderOptions? providerOptions = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        return services.AddFlareProviderCore(options, providerOptions ?? new FlareProviderOptions());
    }

    /// <summary>
    /// Registers the Flare provider and related services using configuration binding.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="sectionName">The configuration section name. Defaults to "Flare".</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFlareProvider(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "Flare")
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var section = configuration.GetSection(sectionName);
        var options = new FlareApiClientOptions()
        {
            BaseUrl = section["BaseUrl"] ?? string.Empty,
            ApiKey = section["ApiKey"] ?? string.Empty,
            Scope = section["Scope"] ?? string.Empty,
        };
        
        return services.AddFlareProviderCore(options, new FlareProviderOptions());
    }

    private static IServiceCollection AddFlareProviderCore(
        this IServiceCollection services,
        FlareApiClientOptions options,
        FlareProviderOptions providerOptions)
    {
        services.AddFlareHttpClient(options);

        services.Configure<FlareProviderOptions>(o =>
        {
            o.PollingInterval = providerOptions.PollingInterval;
            o.StaleThreshold = providerOptions.StaleThreshold;
            o.CachingEnabled = providerOptions.CachingEnabled;
        });

        services.AddSingleton<FlareProvider>();

        services.AddOpenFeature(builder =>
        {
            builder.AddProvider(sp => ServiceProviderServiceExtensions.GetRequiredService<FlareProvider>(sp));
            builder.AddContext(contextBuilder => contextBuilder
                .Set("scope", options.Scope)
                .Build());
        });

        return services;
    }
}
