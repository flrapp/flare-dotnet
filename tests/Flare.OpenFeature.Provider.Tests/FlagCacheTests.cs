using Flare.HttpClient.Models;
using Flare.OpenFeature.Provider;
using FluentAssertions;

namespace Flare.OpenFeature.Provider.Tests;

public class FlagCacheTests
{
    private readonly FlagCache _cache = new();

    [Fact]
    public void IsEmpty_NewCache_ReturnsTrue()
    {
        _cache.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void IsEmpty_AfterUpdate_ReturnsFalse()
    {
        var flags = new List<FlagEvaluationResponse>
        {
            new() { FlagKey = "flag1", Value = true, Variant = "on" }
        };

        _cache.Update(flags, "scope1");

        _cache.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void TryGetFlag_FlagExists_ReturnsTrueAndFlag()
    {
        var flags = new List<FlagEvaluationResponse>
        {
            new() { FlagKey = "my-flag", Value = true, Variant = "enabled" }
        };
        _cache.Update(flags, "prod");

        var found = _cache.TryGetFlag("prod:my-flag", out var result);

        found.Should().BeTrue();
        result.Value.Should().Be(true);
        result.Variant.Should().Be("enabled");
        result.FlagKey.Should().Be("my-flag");
    }

    [Fact]
    public void TryGetFlag_FlagDoesNotExist_ReturnsFalse()
    {
        var found = _cache.TryGetFlag("scope:nonexistent", out _);

        found.Should().BeFalse();
    }

    [Fact]
    public void BuildKey_CombinesScopeAndFlagKey()
    {
        var key = FlagCache.BuildKey("production", "feature-x");

        key.Should().Be("production:feature-x");
    }

    [Fact]
    public void Update_InitialPopulation_ReturnsAllKeysAsChanged()
    {
        var flags = new List<FlagEvaluationResponse>
        {
            new() { FlagKey = "flag1", Value = true, Variant = "on" },
            new() { FlagKey = "flag2", Value = false, Variant = "off" }
        };

        var changed = _cache.Update(flags, "scope");

        changed.Should().BeEquivalentTo(new[] { "flag1", "flag2" });
    }

    [Fact]
    public void Update_NoChanges_ReturnsEmptyList()
    {
        var flags = new List<FlagEvaluationResponse>
        {
            new() { FlagKey = "flag1", Value = true, Variant = "on" }
        };
        _cache.Update(flags, "scope");

        var changed = _cache.Update(flags, "scope");

        changed.Should().BeEmpty();
    }

    [Fact]
    public void Update_ValueChanged_ReturnsChangedKey()
    {
        var original = new List<FlagEvaluationResponse>
        {
            new() { FlagKey = "flag1", Value = true, Variant = "on" }
        };
        _cache.Update(original, "scope");

        var updated = new List<FlagEvaluationResponse>
        {
            new() { FlagKey = "flag1", Value = false, Variant = "on" }
        };
        var changed = _cache.Update(updated, "scope");

        changed.Should().ContainSingle().Which.Should().Be("flag1");
    }

    [Fact]
    public void Update_VariantChanged_ReturnsChangedKey()
    {
        var original = new List<FlagEvaluationResponse>
        {
            new() { FlagKey = "flag1", Value = true, Variant = "on" }
        };
        _cache.Update(original, "scope");

        var updated = new List<FlagEvaluationResponse>
        {
            new() { FlagKey = "flag1", Value = true, Variant = "variant-b" }
        };
        var changed = _cache.Update(updated, "scope");

        changed.Should().ContainSingle().Which.Should().Be("flag1");
    }

    [Fact]
    public void Update_FlagRemoved_ReturnsRemovedKeyAsChanged()
    {
        var original = new List<FlagEvaluationResponse>
        {
            new() { FlagKey = "flag1", Value = true, Variant = "on" },
            new() { FlagKey = "flag2", Value = false, Variant = "off" }
        };
        _cache.Update(original, "scope");

        var updated = new List<FlagEvaluationResponse>
        {
            new() { FlagKey = "flag1", Value = true, Variant = "on" }
        };
        var changed = _cache.Update(updated, "scope");

        changed.Should().ContainSingle().Which.Should().Be("flag2");
    }

    [Fact]
    public void Update_NewFlagAdded_ReturnsNewKeyAsChanged()
    {
        var original = new List<FlagEvaluationResponse>
        {
            new() { FlagKey = "flag1", Value = true, Variant = "on" }
        };
        _cache.Update(original, "scope");

        var updated = new List<FlagEvaluationResponse>
        {
            new() { FlagKey = "flag1", Value = true, Variant = "on" },
            new() { FlagKey = "flag2", Value = false, Variant = "off" }
        };
        var changed = _cache.Update(updated, "scope");

        changed.Should().ContainSingle().Which.Should().Be("flag2");
    }

    [Fact]
    public void Update_ReplacesEntireCache()
    {
        var first = new List<FlagEvaluationResponse>
        {
            new() { FlagKey = "old-flag", Value = true, Variant = "on" }
        };
        _cache.Update(first, "scope");

        var second = new List<FlagEvaluationResponse>
        {
            new() { FlagKey = "new-flag", Value = false, Variant = "off" }
        };
        _cache.Update(second, "scope");

        _cache.TryGetFlag("scope:old-flag", out _).Should().BeFalse();
        _cache.TryGetFlag("scope:new-flag", out _).Should().BeTrue();
    }
}
