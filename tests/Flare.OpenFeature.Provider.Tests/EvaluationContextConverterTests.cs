using Flare.OpenFeature.Provider;
using FluentAssertions;
using OpenFeature.Model;

namespace Flare.OpenFeature.Provider.Tests;

public class EvaluationContextConverterTests
{
    [Fact]
    public void ToFlareContext_NullContext_ReturnsScopeOnly()
    {
        var result = EvaluationContextConverter.ToFlareContext(null, "prod");

        result.Scope.Should().Be("prod");
        result.TargetingKey.Should().BeNull();
        result.Attributes.Should().BeNull();
    }

    [Fact]
    public void ToFlareContext_WithTargetingKey_MapsTargetingKey()
    {
        var context = EvaluationContext.Builder()
            .SetTargetingKey("user-123")
            .Build();

        var result = EvaluationContextConverter.ToFlareContext(context, "staging");

        result.Scope.Should().Be("staging");
        result.TargetingKey.Should().Be("user-123");
    }

    [Fact]
    public void ToFlareContext_WithAttributes_MapsAttributesToStringDictionary()
    {
        var context = EvaluationContext.Builder()
            .SetTargetingKey("user-456")
            .Set("plan", "enterprise")
            .Set("region", "us-east")
            .Build();

        var result = EvaluationContextConverter.ToFlareContext(context, "prod");

        result.Attributes.Should().ContainKey("plan").WhoseValue.Should().Be("enterprise");
        result.Attributes.Should().ContainKey("region").WhoseValue.Should().Be("us-east");
    }

    [Fact]
    public void ToFlareContext_AlwaysSetsScope()
    {
        var context = EvaluationContext.Builder().Build();

        var result = EvaluationContextConverter.ToFlareContext(context, "my-scope");

        result.Scope.Should().Be("my-scope");
    }
}
