using System.Threading;
using System.Threading.Tasks;
using OpenFeature.Contrib.Providers.Flare.Models;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.Flare;

public interface IFlareApiClient
{
    /// <summary>
    /// Evaluates a single flag for the given context.
    /// Calls POST /sdk/v1/flags/evaluate
    /// </summary>
    /// <param name="flagKey">The key of the flag to evaluate.</param>
    /// <param name="context">The evaluation context containing scope and targeting key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The flag evaluation response.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when scope does not provide</exception>
    /// <exception cref="System.Net.Http.HttpRequestException">Thrown for network errors.</exception>
    /// <exception cref="FlareApiException">Thrown for API errors (400, 401, 404).</exception>
    Task<FlagEvaluationResponse> EvaluateAsync(
        string flagKey,
        EvaluationContext context,
        CancellationToken cancellationToken = default);
}
