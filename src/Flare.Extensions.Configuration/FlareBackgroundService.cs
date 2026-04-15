using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Flare.HttpClient;
using Flare.HttpClient.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Flare.Extensions.Configuration;

public class FlareBackgroundService : BackgroundService
{
    private readonly ILogger<FlareBackgroundService> _logger;
    private readonly FlareConfigurationObserver _observer;
    private readonly IFlareApiClient _apiClient;
    private readonly FlareConfigurationOptions _options;

    public FlareBackgroundService(
        ILogger<FlareBackgroundService> logger,
        FlareConfigurationObserver observer,
        IOptions<FlareConfigurationOptions> options,
        IFlareApiClient apiClient)
    {
        _logger = logger;
        _observer = observer;
        _apiClient = apiClient;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.ReloadInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await LoadAsync(stoppingToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occured while loading configuration");
            }
        }
    }

    private static string? FlagValueToString(object? value)
    {
        if (value is null)
            return null;

        if (value is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.GetRawText(),
                JsonValueKind.Object or JsonValueKind.Array => element.GetRawText(),
                JsonValueKind.Null => null,
                _ => null
            };
        }

        return value.ToString()?.ToLowerInvariant();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var context = new FlareEvaluationContext
        {
            Scope = _options.ScopeAlias
        };

        var result = await _apiClient.EvaluateAllAsync(context, cancellationToken).ConfigureAwait(false);

        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var flag in result.Flags)
        {
            data[$"{_options.FeatureFlagSection}:{flag.FlagKey}"] = FlagValueToString(flag.Value);
        }

        _observer.SetAll(data);
    }
}
