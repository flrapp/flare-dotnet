using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Flare.Extensions.Configuration.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Flare.Extensions.Configuration;

public class FlareBackgroundService : BackgroundService
{
    private readonly ILogger<FlareBackgroundService> _logger;
    private readonly FlareConfigurationObserver _observer;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FlareConfigurationOptions _options;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    
    public FlareBackgroundService(ILogger<FlareBackgroundService> logger, FlareConfigurationObserver observer,
        IOptions<FlareConfigurationOptions> options, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _observer = observer;
        _httpClientFactory = httpClientFactory;
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

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("FlareFeatureToggle");
        var request = new
        {
            Context = new
            {
                Scope = _options.ScopeAlias
            }
        };

        var json = JsonSerializer.Serialize(request, JsonOptions);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/sdk/v1/flags/evaluate-all");
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
    
        using var httpResponse = await client.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        httpResponse.EnsureSuccessStatusCode();

        var responseContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var result = JsonSerializer.Deserialize<BulkEvaluationResponseModel>(responseContent, JsonOptions);
    
        if (result?.Flags == null) return;

        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    
        foreach (var flag in result.Flags)
        {
            data[$"{_options.FeatureFlagSection}:{flag.FlagKey}"] = flag.Value.ToString().ToLowerInvariant();
        }

        _observer.SetAll(data);
    }
}