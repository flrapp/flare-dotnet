using System;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;

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

        services.AddHttpClient<IFlareApiClient, FlareApiClient>()
            .ConfigureHttpClient((_, client) =>
            {
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
