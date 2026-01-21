using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Flare.Extensions.Configuration.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Flare.Extensions.Configuration;

public class FlareConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly FlareConfigurationSource _source;
    private readonly HttpClient _httpClient;
    private Timer? _reloadTimer;
    private readonly ILogger _logger;
    private readonly ILoggerFactory? _loggerFactory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    
    public FlareConfigurationProvider(FlareConfigurationSource source)
    {
        _source = source;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_source.Options.ServerUrl)
        };
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_source.Options.ApiKey}");
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        
        _logger = _loggerFactory.CreateLogger<FlareConfigurationProvider>();
        
    }

    public override void Load()
    {
        try
        {
            LoadAsync().GetAwaiter().GetResult();
        
            if (_source.Options.ReloadInterval > TimeSpan.Zero)
            {
                _reloadTimer = new Timer(
                    _ => Load(),
                    null,
                    _source.Options.ReloadInterval,
                    _source.Options.ReloadInterval
                );
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to load FlareConfiguration");
            throw;
        }
        
    }

    private async Task LoadAsync()
    {
        var request = new
        {
            Context = new
            {
                Scope = _source.Options.ScopeAlias
            }
        };

        var json = JsonSerializer.Serialize(request, JsonOptions);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/sdk/v1/flags/evaluate-all");
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
        
        using var httpResponse = await _httpClient.SendAsync(httpRequest).ConfigureAwait(false);
        httpResponse.EnsureSuccessStatusCode();

        var responseContent = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        var result = JsonSerializer.Deserialize<BulkEvaluationResponseModel>(responseContent, JsonOptions);
        
        if (result?.Flags == null) return;

        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var flag in result.Flags)
        {
            data[$"{_source.Options.FeatureFlagSection}:{flag.FlagKey}"] = flag.Value.ToString().ToLowerInvariant();
        }

        Data = data;
    }

    public void Dispose()
    {
        _reloadTimer?.Dispose();
        _httpClient.Dispose();
        _loggerFactory?.Dispose();
    }
}