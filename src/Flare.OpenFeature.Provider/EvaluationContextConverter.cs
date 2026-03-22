using System.Linq;
using Flare.HttpClient.Models;
using OpenFeature.Model;

namespace Flare.OpenFeature.Provider;

/// <summary>
/// Converts OpenFeature <see cref="EvaluationContext"/> to Flare's <see cref="FlareEvaluationContext"/>.
/// </summary>
internal static class EvaluationContextConverter
{
    public static FlareEvaluationContext ToFlareContext(EvaluationContext? context, string scope)
    {
        return new FlareEvaluationContext
        {
            Scope = scope,
            TargetingKey = context?.TargetingKey,
            Attributes = context?.AsDictionary()
                .ToDictionary(x => x.Key, y => y.Value?.AsString)
        };
    }
}
