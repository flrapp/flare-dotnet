using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Flare.HttpClient;
using Flare.HttpClient.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenFeature;
using OpenFeature.Constant;
using OpenFeature.Model;

namespace Flare.OpenFeature.Provider;

public sealed class FlareProvider : FeatureProvider
{
    private static readonly Metadata ProviderMetadata = new("Flare Provider");

    private readonly IFlareApiClient _apiClient;
    private readonly FlareApiClientOptions _options;
    private readonly ILogger<FlareProvider> _logger;

    public FlareProvider(IFlareApiClient apiClient, IOptions<FlareApiClientOptions> options,ILogger<FlareProvider>? logger = null)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _options = options.Value;
        _logger = logger ?? NullLogger<FlareProvider>.Instance;
    }
    
    public override Metadata? GetMetadata() => ProviderMetadata;

    public override async Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(
        string flagKey,
        bool defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var flareContext = new FlareEvaluationContext
            {
                Scope = _options.Scope,
                TargetingKey = context?.TargetingKey,
                Attributes = context?.AsDictionary()
                    .ToDictionary(x => x.Key, y => y.Value?.AsString)
            };

            var response = await _apiClient.EvaluateAsync(flagKey, flareContext, cancellationToken).ConfigureAwait(false);

            return new ResolutionDetails<bool>(
                flagKey: flagKey,
                value: response.Value,
                variant: response.Variant,
                reason: MapReason(response.Reason),
                errorType: ErrorType.None,
                errorMessage: null,
                flagMetadata: BuildFlagMetadata(response.Metadata)
            );
        }
        catch (FlareApiException ex)
        {
            _logger.LogError(ex, "API error evaluating flag {FlagKey}: {StatusCode}", flagKey, ex.StatusCode);
            return CreateErrorResult(flagKey, defaultValue, ErrorType.General, ex.Message);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error evaluating flag {FlagKey}", flagKey);
            return CreateErrorResult(flagKey, defaultValue, ErrorType.ProviderNotReady, ex.Message);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error for flag {FlagKey}", flagKey);
            return CreateErrorResult(flagKey, defaultValue, ErrorType.ParseError, ex.Message);
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            _logger.LogWarning("Flag evaluation cancelled for {FlagKey}", flagKey);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout evaluating flag {FlagKey}", flagKey);
            return CreateErrorResult(flagKey, defaultValue, ErrorType.ProviderNotReady, "Request timeout");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error evaluating flag {FlagKey}", flagKey);
            return CreateErrorResult(flagKey, defaultValue, ErrorType.General, ex.Message);
        }
    }

    public override Task<ResolutionDetails<string>> ResolveStringValueAsync(
        string flagKey,
        string defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateTypeMismatchResult(flagKey, defaultValue));
    }

    public override Task<ResolutionDetails<int>> ResolveIntegerValueAsync(
        string flagKey,
        int defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateTypeMismatchResult(flagKey, defaultValue));
    }

    public override Task<ResolutionDetails<double>> ResolveDoubleValueAsync(
        string flagKey,
        double defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateTypeMismatchResult(flagKey, defaultValue));
    }

    public override Task<ResolutionDetails<Value>> ResolveStructureValueAsync(
        string flagKey,
        Value defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateTypeMismatchResult(flagKey, defaultValue));
    }

    private static string MapReason(string? reason)
    {
        if (string.IsNullOrEmpty(reason))
            return Reason.Unknown;

        return reason!.ToUpperInvariant() switch
        {
            "STATIC" => Reason.Static,
            "DEFAULT" => Reason.Default,
            "TARGETING_MATCH" => Reason.TargetingMatch,
            "SPLIT" => Reason.Split,
            "CACHED" => Reason.Cached,
            "DISABLED" => Reason.Disabled,
            "ERROR" => Reason.Error,
            _ => reason
        };
    }

    private static ImmutableMetadata? BuildFlagMetadata(FlagMetadata? metadata)
    {
        if (metadata == null)
            return null;

        var dict = new Dictionary<string, object>();

        if (metadata.ScopeAlias != null)
            dict["scopeAlias"] = metadata.ScopeAlias;

        if (metadata.ScopeId.HasValue)
            dict["scopeId"] = metadata.ScopeId.Value.ToString();

        dict["updatedAt"] = metadata.UpdatedAt.ToString("O");

        return new ImmutableMetadata(dict);
    }

    private static ResolutionDetails<T> CreateErrorResult<T>(string flagKey, T defaultValue, ErrorType errorType, string? errorMessage)
    {
        return new ResolutionDetails<T>(
            flagKey: flagKey,
            value: defaultValue,
            reason: Reason.Error,
            errorType: errorType,
            errorMessage: errorMessage
        );
    }

    private static ResolutionDetails<T> CreateTypeMismatchResult<T>(string flagKey, T defaultValue)
    {
        return new ResolutionDetails<T>(
            flagKey: flagKey,
            value: defaultValue,
            reason: Reason.Error,
            errorType: ErrorType.TypeMismatch,
            errorMessage: "Flare provider only supports boolean flag evaluation for now"
        );
    }
}