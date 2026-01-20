using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenFeature.Contrib.Providers.Flare.Models;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.Flare;

public class FlareApiClient : IFlareApiClient
{
    private const string EvaluateEndpoint = "/sdk/v1/flags/evaluate";
    private const string EvaluateAllEndpoint = "/sdk/v1/flags/evaluate-all";

    private readonly HttpClient _httpClient;
    private readonly ILogger<FlareApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public FlareApiClient(
        HttpClient httpClient,
        ILogger<FlareApiClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<FlagEvaluationResponse>> EvaluateAllAsync(
        EvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        var request = new FlareEvaluateAllRequest
        {
            Context = BuildEvaluationContext(context)
        };

        _logger.LogDebug("Evaluating all flags for scope {Scope}", request.Context?.Scope);

        var json = JsonSerializer.Serialize(request, JsonOptions);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, EvaluateAllEndpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);

        await EnsureSuccessAsync(httpResponse, cancellationToken).ConfigureAwait(false);

        var responseContent = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        var response = JsonSerializer.Deserialize<FlareEvaluateAllResponse>(responseContent, JsonOptions);

        if (response?.Flags == null)
        {
            throw new JsonException("Failed to deserialize evaluate-all response");
        }

        _logger.LogDebug("Received {Count} flag evaluations", response.Flags.Count);

        return response.Flags;
    }

    public async Task<FlagEvaluationResponse> EvaluateAsync(
        string flagKey,
        EvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(flagKey))
        {
            throw new ArgumentException("Flag key cannot be null or empty.", nameof(flagKey));
        }

        var request = new FlareEvaluationRequest
        {
            FlagKey = flagKey,
            Context = BuildEvaluationContext(context)
        };

        _logger.LogDebug("Evaluating flag {FlagKey} for scope {Scope}", flagKey, request.Context?.Scope);

        var json = JsonSerializer.Serialize(request, JsonOptions);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, EvaluateEndpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);

        await EnsureSuccessAsync(httpResponse, cancellationToken).ConfigureAwait(false);

        var responseContent = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        var response = JsonSerializer.Deserialize<FlagEvaluationResponse>(responseContent, JsonOptions);

        if (response == null)
        {
            throw new JsonException("Failed to deserialize evaluate response");
        }

        _logger.LogDebug("Flag {FlagKey} evaluated to {Value} with reason {Reason}",
            flagKey, response.Value, response.Reason);

        return response;
    }

    private FlareEvaluationContext BuildEvaluationContext(EvaluationContext? context)
    {
        var flareContext = new FlareEvaluationContext
        {
            Scope = "dev"
        };

        if (context != null)
        {
            flareContext.TargetingKey = context.TargetingKey;

            var scopeValue = context.GetValue("scope");
            if (scopeValue != null && !string.IsNullOrEmpty(scopeValue.AsString))
            {
                flareContext.Scope = scopeValue.AsString;
            }
        }

        return flareContext;
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var statusCode = response.StatusCode;
        string? responseBody = null;

        try
        {
            responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
        catch
        {
            // Ignore errors reading response body
        }

        var message = statusCode switch
        {
            HttpStatusCode.BadRequest => $"Bad request: {responseBody ?? "Invalid request format"}",
            HttpStatusCode.Unauthorized => "Unauthorized: Invalid or missing API key",
            HttpStatusCode.NotFound => $"Not found: {responseBody ?? "Resource not found"}",
            _ => $"API error ({(int)statusCode}): {responseBody ?? response.ReasonPhrase}"
        };

        _logger.LogError("Flare API error: {StatusCode} - {Message}", (int)statusCode, message);

        throw new FlareApiException(statusCode, message);
    }
}
