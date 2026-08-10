using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// Slice 8, the merged degradation task: a declaration's identity is its name, never its id.
/// </summary>
/// <remarks>
/// <para>
/// <b>The hole these close was open for two tasks and is invisible from every angle but this one.</b>
/// A <see cref="ResourceId"/> is declaration order. Reorder two <c>[[resource]]</c> blocks and every
/// count, every family and every shape is identical — so <see cref="RulesetShape.Compare"/> called it
/// <em>numbers only</em>, the reload was admitted, and every live Bin row silently started naming the
/// other Good. Nothing in the world records that it changed; the city just begins converting flour
/// into flour.
/// </para>
/// <para>
/// <b>It matters more for the degradations than for the refusal.</b> A reload that removes a kind is
/// the case slice 8 exists for, and removing a declaration from the middle of a file renumbers
/// everything below it — so <em>every</em> real removal is a reordering, and a degradation that
/// mapped ids positionally would relabel the survivors while it was busy derelicting the casualty.
/// </para>
/// </remarks>
public sealed class RulesetIdentityTests
{
    private const string TwoGoods = """
        [[resource]]
        name = "flour"
        family = "good"

        [[resource]]
        name = "bread"
        family = "good"
        """;

    private const string TwoGoodsSwapped = """
        [[resource]]
        name = "bread"
        family = "good"

        [[resource]]
        name = "flour"
        family = "good"
        """;

    private static Ruleset Load(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    private static string WithKinds(string first, string second) => $$"""
        {{TwoGoods}}

        [[building]]
        name = "{{first}}"
        bins = [ { resource = "flour", capacity = 10 } ]

        [[building]]
        name = "{{second}}"
        bins = [ { resource = "bread", capacity = 10 } ]
        """;

    [Fact]
    public void A_declarations_key_is_its_name_and_not_its_position()
    {
        Ruleset original = Load(TwoGoods);
        Ruleset swapped = Load(TwoGoodsSwapped);

        Assert.Equal(original.ResourceKey(new ResourceId(1)), swapped.ResourceKey(new ResourceId(2)));
        Assert.Equal(original.ResourceKey(new ResourceId(2)), swapped.ResourceKey(new ResourceId(1)));
        Assert.NotEqual(original.ResourceKey(new ResourceId(1)), original.ResourceKey(new ResourceId(2)));
    }

    [Fact]
    public void Reordering_two_resources_is_refused_rather_than_read_as_a_tuning_change()
    {
        Assert.Equal(
            RulesetChange.ResourceIdentity,
            RulesetShape.Compare(Load(TwoGoods), Load(TwoGoodsSwapped)));
    }

    [Fact]
    public void Reordering_two_kinds_is_refused_rather_than_read_as_a_tuning_change()
    {
        // The two kinds differ in their Bins, so the pre-key comparison would have caught this one as
        // KindBins -- a true sentence about the wrong pair, and the reason identity is checked first.
        Assert.Equal(
            RulesetChange.KindIdentity,
            RulesetShape.Compare(Load(WithKinds("house", "shop")), Load(WithKinds("shop", "house"))));
    }

    [Fact]
    public void Renaming_a_declaration_is_a_different_declaration()
    {
        // There is no rename, and that is the honest answer rather than a limitation. A name is the
        // only identity a TOML file carries, so "flour became grain" and "flour went and grain
        // arrived" are the same file. Guessing between them is what an explicit id field would be for.
        Assert.Equal(
            RulesetChange.ResourceIdentity,
            RulesetShape.Compare(Load(TwoGoods), Load(TwoGoods.Replace("flour", "grain", StringComparison.Ordinal))));
    }

    [Fact]
    public void An_unchanged_file_still_compares_equal()
    {
        Assert.Equal(RulesetChange.None, RulesetShape.Compare(Load(TwoGoods), Load(TwoGoods)));
    }

    [Fact]
    public void Retuning_a_capacity_is_still_a_tuning_change()
    {
        string tuned = WithKinds("house", "shop").Replace("capacity = 10", "capacity = 40", StringComparison.Ordinal);

        Assert.Equal(RulesetChange.None, RulesetShape.Compare(Load(WithKinds("house", "shop")), Load(tuned)));
    }

    /// <summary>
    /// A Ruleset built in code has no names to hash, and positional identity is the right default.
    /// </summary>
    /// <remarks>
    /// Every fixture in this suite declares its Resources in an order it means, and there is no file
    /// for the loader to read them out of. Defaulting the key to the id keeps those Rulesets comparing
    /// exactly as they did — the check is inert where there is nothing to check, rather than
    /// refusing everything that was not parsed.
    /// </remarks>
    [Fact]
    public void A_Ruleset_built_in_code_keeps_positional_identity()
    {
        Assert.Equal(1UL, Ruleset.Empty.ResourceKey(new ResourceId(1)));
        Assert.Equal(3UL, Ruleset.Empty.KindKey(3));
    }

    [Fact]
    public void Changing_only_the_Layer_data_carries_the_keys_across()
    {
        Ruleset original = Load(TwoGoods);
        Ruleset relayered = original.WithLayers(Borough.Core.Space.LayerRuleset.Default);

        Assert.Equal(RulesetChange.None, RulesetShape.Compare(original, relayered));
    }
}
