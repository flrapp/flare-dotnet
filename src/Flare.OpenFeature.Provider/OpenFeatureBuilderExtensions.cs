using System;
using Flare.HttpClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenFeature;
using OpenFeature.Hosting;

namespace Flare.OpenFeature.Provider;

/// <summary>
/// Extension methods for registering the Flare provider with OpenFeature.
/// </summary>
/// <example>
/// <code>
/// services.AddOpenFeature(builder =>
/// {
///     builder.AddFlareProvider(new FlareApiClientOptions
///     {
///         BaseUrl = "https://api.flare.example.com",
///         ApiKey = "your-api-key",
///         Scope = "production"
///     });
/// });
/// </code>
///
/// Or with configuration:
/// <code>
/// services.AddOpenFeature(builder =>
/// {
///     builder.AddFlareProvider(configuration);
/// });
/// </code>
/// </example>
public static class OpenFeatureBuilderExtensions
{
    /// <summary>
    /// Adds the Flare provider to the OpenFeature builder using explicit options.
    /// </summary>
    /// <param name="builder">The OpenFeature builder.</param>
    /// <param name="options">The Flare API client options.</param>
    /// <param name="providerOptions">Optional provider-specific options (caching, polling).</param>
    /// <returns>The builder for chaining.</returns>
    public static OpenFeatureBuilder AddFlareProvider(
        this OpenFeatureBuilder builder,
        FlareApiClientOptions options,
        FlareProviderOptions? providerOptions = null)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        return builder.AddFlareProviderCore(options, providerOptions ?? new FlareProviderOptions());
    }

    /// <summary>
    /// Adds the Flare provider to the OpenFeature builder using configuration binding.
    /// </summary>
    /// <param name="builder">The OpenFeature builder.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="sectionName">The configuration section name. Defaults to "Flare".</param>
    /// <returns>The builder for chaining.</returns>
    public static OpenFeatureBuilder AddFlareProvider(
        this OpenFeatureBuilder builder,
        IConfiguration configuration,
        string sectionName = "Flare")
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var section = configuration.GetSection(sectionName);
        var options = new FlareApiClientOptions
        {
            BaseUrl = section["BaseUrl"] ?? string.Empty,
            ApiKey = section["ApiKey"] ?? string.Empty,
            Scope = section["Scope"] ?? string.Empty,
        };

        return builder.AddFlareProviderCore(options, new FlareProviderOptions());
    }

    private static OpenFeatureBuilder AddFlareProviderCore(
        this OpenFeatureBuilder builder,
        FlareApiClientOptions options,
        FlareProviderOptions providerOptions)
    {
        var services = builder.Services;

        services.AddFlareHttpClient(options);

        services.Configure<FlareProviderOptions>(o =>
        {
            o.PollingInterval = providerOptions.PollingInterval;
            o.StaleThreshold = providerOptions.StaleThreshold;
            o.CachingEnabled = providerOptions.CachingEnabled;
        });

        services.AddSingleton<FlareProvider>();

        builder.AddProvider(sp => sp.GetRequiredService<FlareProvider>());
        builder.AddContext(contextBuilder => contextBuilder
            .Set("scope", options.Scope)
            .Build());

        return builder;
    }
}
