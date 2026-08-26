using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Tests.Rules;

/// <summary>
/// Where the line runs between a reload that is a tuning change and one that is a migration.
/// </summary>
/// <remarks>
/// <para>
/// <b>The test that decides each case is <em>what live state points at</em>, and it is worth stating
/// because it is not the obvious one.</b> A Bin row holds a Resource id; a Rule Instance holds a Rule
/// id; a Building holds a kind. Anything a row can point at is <b>structure</b>, and a Ruleset that
/// moves it leaves rows pointing at something that has changed meaning underneath them. Anything only
/// ever read <em>through</em> a row — a rate, a capacity, a quantity, an apply band — is a
/// <b>number</b>, and moving it is exactly what <c>adr/0015</c>'s acceptance test is about.
/// </para>
/// <para>
/// <b>Both halves are tested, and the safe half is the one that would rot.</b> A missing refusal is
/// caught the first time somebody reloads a genuinely different Ruleset and the world corrupts. A
/// refusal that is too <em>broad</em> has no symptom at all: it just means a designer cannot tune a
/// number and is told the file is a migration, and nobody ever finds out that it was not. So every
/// field that may move has a case here saying so by name.
/// </para>
/// <para>
/// <b>This is the list to re-run when the Ruleset grows a field.</b> A new field is on one side or
/// the other, and deciding which is a design act rather than a formality — the whole reason
/// <see cref="RulesetShape"/> is a named thing rather than an equality operator.
/// </para>
/// </remarks>
public sealed class RulesetShapeTests
{
    private const byte House = 1;
    private const byte Shop = 2;

    private static readonly ResourceId Flour = new(1);
    private static readonly ResourceId Bread = new(2);

    /// <summary>
    /// Two kinds, two Resources, two Rules and a Zone Rule — every axis populated, so a change to any
    /// one of them has somewhere to show.
    /// </summary>
    private static Ruleset Reference(
        uint rate = 8,
        int capacity = 100,
        int quantity = 1,
        ApplyCount? apply = null,
        int condemnAfter = 4,
        uint interval = 32,
        int sample = 4,
        ResourceFamily breadFamily = ResourceFamily.Good,
        ResourceId? bakes = null,
        RuleId? onFail = null,
        Layer emits = Layer.IndustrialPollution) =>
        new(
            resources: [ResourceFamily.Good, breadFamily],
            rules:
            [
                new RuleDefinition(
                    House, rate, apply ?? ApplyCount.Band(1, 1), onFail ?? RuleId.None, false,
                    default, ConditionId.None, 0, 1, 0, 1, 0, 1),
                new RuleDefinition(
                    Shop, rate, ApplyCount.Band(1, 1), RuleId.None, false, default,
                    ConditionId.None, 1, 1, 1, 0, 1, 0),
            ],
            kinds:
            [
                new KindDefinition(0, 1, 0, 1) { CondemnAfterTicks = condemnAfter },
                new KindDefinition(1, 1, 1, 1),
            ],
            inputs:
            [
                new Term(new BinRef(Scope.Local, Flour), quantity),
                new Term(new BinRef(Scope.Local, bakes ?? Bread), 1),
            ],
            outputs: [new Term(new BinRef(Scope.Local, bakes ?? Bread), quantity)],
            emissions: [new MapEmission(emits, quantity)],
            bins:
            [
                new BinDeclaration(Flour, BinCapacity.Of(capacity)),
                new BinDeclaration(Bread, BinCapacity.Of(capacity)),
            ],
            kindRules: [new RuleId(1), new RuleId(2)],
            zoneRules: [new ZoneRuleDefinition(House, 0, interval, sample)]);

    private static RulesetChange Compare(Ruleset replacement) =>
        RulesetShape.Compare(Reference(), replacement);

    // ---- Numbers, which may move ----

    /// <summary>A Ruleset is structurally identical to itself.</summary>
    [Fact]
    public void A_ruleset_matches_itself()
    {
        Assert.Equal(RulesetChange.None, Compare(Reference()));
    }

    /// <summary>
    /// <b>A Rule's rate may move, and this is the case <c>adr/0015</c>'s acceptance test is made
    /// of.</b>
    /// </summary>
    [Fact]
    public void A_rate_may_move()
    {
        Assert.Equal(RulesetChange.None, Compare(Reference(rate: 64)));
    }

    /// <summary>
    /// A Bin's capacity may move, even though a Bin over its new capacity is a real situation.
    /// </summary>
    /// <remarks>
    /// <b>That situation is task 5's and not a reason to refuse here.</b> Nothing points at a
    /// capacity: a Bin row holds a Resource and a level, and the capacity is read through the Ruleset
    /// at every deposit. A city that shrinks its warehouses is tuning, and a Bin above the new
    /// ceiling stops accepting until it drains — which is the mechanism already behaving correctly
    /// rather than a migration.
    /// </remarks>
    [Fact]
    public void A_bin_capacity_may_move()
    {
        Assert.Equal(RulesetChange.None, Compare(Reference(capacity: 4)));
    }

    /// <summary>A term's quantity may move — this is *the* production ratio, in the ADR's phrase.</summary>
    [Fact]
    public void A_terms_quantity_may_move()
    {
        Assert.Equal(RulesetChange.None, Compare(Reference(quantity: 9)));
    }

    /// <summary>An apply band may move, including from a band to a Readout-derived count.</summary>
    /// <remarks>
    /// <b>Derived-ness is a number here and not a shape, which is the one entry on this list worth
    /// arguing about.</b> The engine branches on <c>ApplyCount.IsDerived</c>, so it looks structural —
    /// but no row holds it. A Rule Instance is armed and evaluated identically either way; what
    /// changes is how many times the same evaluation applies, which is a quantity.
    /// </remarks>
    [Fact]
    public void An_apply_band_may_move()
    {
        Assert.Equal(RulesetChange.None, Compare(Reference(apply: ApplyCount.Band(1, 7))));
    }

    /// <summary>A kind's condemnation threshold may move.</summary>
    [Fact]
    public void A_condemnation_threshold_may_move()
    {
        Assert.Equal(RulesetChange.None, Compare(Reference(condemnAfter: 40)));
    }

    /// <summary>A Zone Rule's interval and sample may move — they are pacing.</summary>
    [Fact]
    public void A_zone_rules_pacing_may_move()
    {
        Assert.Equal(RulesetChange.None, Compare(Reference(interval: 8, sample: 64)));
    }

    // ---- Structure, which may not ----

    /// <summary>A Resource added or removed changes what every Bin id means.</summary>
    [Fact]
    public void A_resource_may_not_be_added_or_removed()
    {
        Ruleset fewer = new(
            resources: [ResourceFamily.Good],
            rules: [], kinds: [], inputs: [], outputs: [], emissions: [], bins: [],
            kindRules: [], zoneRules: []);

        Assert.Equal(RulesetChange.ResourceCount, Compare(fewer));
    }

    /// <summary>
    /// A Resource changing family is not a tuning change, because conservation is enforced engine-wide.
    /// </summary>
    /// <remarks>
    /// <c>adr/0024</c> makes Money conserved and every other family not, and the loader enforces it on
    /// every Rule. A Good quietly becoming Money would make the same Bin start refusing what it had
    /// been accepting, with no row having changed.
    /// </remarks>
    [Fact]
    public void A_resource_may_not_change_family()
    {
        Assert.Equal(
            RulesetChange.ResourceFamily, Compare(Reference(breadFamily: ResourceFamily.Money)));
    }

    /// <summary>A Rule's <c>on_fail</c> is the chain, and a Rule Instance walks it.</summary>
    [Fact]
    public void A_chain_may_not_be_rewired()
    {
        Assert.Equal(RulesetChange.RuleShape, Compare(Reference(onFail: new RuleId(2))));
    }

    /// <summary>A term naming a different Resource is a different Rule wearing the same id.</summary>
    [Fact]
    public void A_term_may_not_name_a_different_resource()
    {
        Assert.Equal(RulesetChange.RuleShape, Compare(Reference(bakes: Flour)));
    }

    /// <summary>An emission writing to a different Layer is likewise a different Rule.</summary>
    [Fact]
    public void An_emission_may_not_move_to_a_different_layer()
    {
        Assert.Equal(RulesetChange.RuleShape, Compare(Reference(emits: Layer.LandValue)));
    }

    /// <summary>A kind added or removed is what the derelict flag exists for, and it is task 4's.</summary>
    [Fact]
    public void A_kind_may_not_be_added_or_removed()
    {
        Ruleset fewer = new(
            resources: [ResourceFamily.Good, ResourceFamily.Good],
            rules:
            [
                new RuleDefinition(
                    House, 8, ApplyCount.Band(1, 1), RuleId.None, false, default,
                    ConditionId.None, 0, 1, 0, 1, 0, 1),
                new RuleDefinition(
                    Shop, 8, ApplyCount.Band(1, 1), RuleId.None, false, default,
                    ConditionId.None, 1, 1, 1, 0, 1, 0),
            ],
            kinds: [new KindDefinition(0, 1, 0, 1)],
            inputs:
            [
                new Term(new BinRef(Scope.Local, Flour), 1),
                new Term(new BinRef(Scope.Local, Bread), 1),
            ],
            outputs: [new Term(new BinRef(Scope.Local, Bread), 1)],
            emissions: [new MapEmission(Layer.IndustrialPollution, 1)],
            bins:
            [
                new BinDeclaration(Flour, BinCapacity.Of(100)),
                new BinDeclaration(Bread, BinCapacity.Of(100)),
            ],
            kindRules: [new RuleId(1)],
            zoneRules: [new ZoneRuleDefinition(House, 0, 32, 4)]);

        Assert.Equal(RulesetChange.KindCount, Compare(fewer));
    }

    /// <summary>
    /// A kind's Bin naming a different Resource is task 5's dropped Bin, and every live Building of
    /// that kind already holds the old one.
    /// </summary>
    [Fact]
    public void A_kinds_bin_may_not_name_a_different_resource()
    {
        Ruleset swapped = new(
            resources: [ResourceFamily.Good, ResourceFamily.Good],
            rules:
            [
                new RuleDefinition(
                    House, 8, ApplyCount.Band(1, 1), RuleId.None, false, default,
                    ConditionId.None, 0, 1, 0, 1, 0, 1),
                new RuleDefinition(
                    Shop, 8, ApplyCount.Band(1, 1), RuleId.None, false, default,
                    ConditionId.None, 1, 1, 1, 0, 1, 0),
            ],
            kinds: [new KindDefinition(0, 1, 0, 1) { CondemnAfterTicks = 4 }, new KindDefinition(1, 1, 1, 1)],
            inputs:
            [
                new Term(new BinRef(Scope.Local, Flour), 1),
                new Term(new BinRef(Scope.Local, Bread), 1),
            ],
            outputs: [new Term(new BinRef(Scope.Local, Bread), 1)],
            emissions: [new MapEmission(Layer.IndustrialPollution, 1)],
            bins:
            [
                // Flour becomes Bread. Every Building of this kind already holds a Flour Bin.
                new BinDeclaration(Bread, BinCapacity.Of(100)),
                new BinDeclaration(Bread, BinCapacity.Of(100)),
            ],
            kindRules: [new RuleId(1), new RuleId(2)],
            zoneRules: [new ZoneRuleDefinition(House, 0, 32, 4)]);

        Assert.Equal(RulesetChange.KindBins, Compare(swapped));
    }

    /// <summary>
    /// A Zone Rule's kind or permission bit may not move, even though nothing in the world points at
    /// one.
    /// </summary>
    /// <remarks>
    /// <b>The exception that proves the rule, and it is here on a different argument.</b> No row
    /// holds a Zone Rule id — a Zone Rule is only ever iterated, which is why it has no id type at
    /// all. But its <em>identity is its position in declaration order</em>, which is `02 §4.2`'s
    /// tie-break between two Rules contending for one Lot; silently repurposing entry 3 would move
    /// the tie-break and change which city gets built, with no row having changed either.
    /// </remarks>
    [Fact]
    public void A_zone_rules_kind_may_not_move()
    {
        Ruleset repurposed = new(
            resources: [ResourceFamily.Good, ResourceFamily.Good],
            rules:
            [
                new RuleDefinition(
                    House, 8, ApplyCount.Band(1, 1), RuleId.None, false, default,
                    ConditionId.None, 0, 1, 0, 1, 0, 1),
                new RuleDefinition(
                    Shop, 8, ApplyCount.Band(1, 1), RuleId.None, false, default,
                    ConditionId.None, 1, 1, 1, 0, 1, 0),
            ],
            kinds: [new KindDefinition(0, 1, 0, 1) { CondemnAfterTicks = 4 }, new KindDefinition(1, 1, 1, 1)],
            inputs:
            [
                new Term(new BinRef(Scope.Local, Flour), 1),
                new Term(new BinRef(Scope.Local, Bread), 1),
            ],
            outputs: [new Term(new BinRef(Scope.Local, Bread), 1)],
            emissions: [new MapEmission(Layer.IndustrialPollution, 1)],
            bins:
            [
                new BinDeclaration(Flour, BinCapacity.Of(100)),
                new BinDeclaration(Bread, BinCapacity.Of(100)),
            ],
            kindRules: [new RuleId(1), new RuleId(2)],
            zoneRules: [new ZoneRuleDefinition(Shop, 0, 32, 4)]);

        Assert.Equal(RulesetChange.ZoneRuleShape, Compare(repurposed));
    }

    /// <summary>The comparison is symmetric — adding is refused the same way removing is.</summary>
    /// <remarks>
    /// Worth asserting because the implementation compares counts before contents, and a one-sided
    /// count check reads as correct while only catching shrinkage.
    /// </remarks>
    [Fact]
    public void The_comparison_is_symmetric()
    {
        Ruleset wider = new(
            resources: [ResourceFamily.Good, ResourceFamily.Good, ResourceFamily.Good],
            rules: [], kinds: [], inputs: [], outputs: [], emissions: [], bins: [],
            kindRules: [], zoneRules: []);

        Assert.Equal(RulesetChange.ResourceCount, RulesetShape.Compare(Reference(), wider));
        Assert.Equal(RulesetChange.ResourceCount, RulesetShape.Compare(wider, Reference()));
    }
}
