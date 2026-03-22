using System;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Flare.HttpClient;

public static class FlareHttpClientExtensions
{
    /// <summary>
    /// Registers <see cref="IFlareApiClient"/> and its underlying <see cref="System.Net.Http.HttpClient"/>
    /// in the DI container using the provided options.
    /// </summary>
    public static IServiceCollection AddFlareHttpClient(
        this IServiceCollection services,
        FlareApiClientOptions options)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
            throw new InvalidOperationException("FlareApiClientOptions.BaseUrl is required.");
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("FlareApiClientOptions.ApiKey is required.");

        services.AddOptions<FlareApiClientOptions>().Configure(o =>
        {
            o.BaseUrl = options.BaseUrl;
            o.ApiKey  = options.ApiKey;
            o.Scope   = options.Scope;
        });

        services.AddHttpClient<IFlareApiClient, FlareApiClient>()
            .ConfigureHttpClient((_, client) =>
            {
                client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/'));
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", options.ApiKey);
                client.Timeout = TimeSpan.FromSeconds(30);
            });

        return services;
    }

    /// <summary>
    /// Registers <see cref="IFlareApiClient"/> and its underlying <see cref="System.Net.Http.HttpClient"/>
    /// in the DI container using a factory that resolves options from the service provider at runtime.
    /// </summary>
    public static IServiceCollection AddFlareHttpClient(
        this IServiceCollection services,
        Func<IServiceProvider, FlareApiClientOptions> optionsFactory)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (optionsFactory == null)
            throw new ArgumentNullException(nameof(optionsFactory));

        services.AddOptions<FlareApiClientOptions>().Configure<IServiceProvider>((o, sp) =>
        {
            var resolved = optionsFactory(sp);
            o.BaseUrl = resolved.BaseUrl;
            o.ApiKey  = resolved.ApiKey;
            o.Scope   = resolved.Scope;
        });

        services.AddHttpClient<IFlareApiClient, FlareApiClient>()
            .ConfigureHttpClient((sp, client) =>
            {
                var options = optionsFactory(sp);

                if (string.IsNullOrWhiteSpace(options.BaseUrl))
                    throw new InvalidOperationException("FlareApiClientOptions.BaseUrl is required.");
                if (string.IsNullOrWhiteSpace(options.ApiKey))
                    throw new InvalidOperationException("FlareApiClientOptions.ApiKey is required.");

                client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/'));
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", options.ApiKey);
                client.Timeout = TimeSpan.FromSeconds(30);
            });

        return services;
    }
}
