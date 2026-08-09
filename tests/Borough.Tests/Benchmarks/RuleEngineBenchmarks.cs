using BenchmarkDotNet.Attributes;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Tests.Benchmarks;

/// <summary>
/// What one Rule evaluation costs, which is the denominator slice 7's tripwire is stated over.
/// </summary>
/// <remarks>
/// <para>
/// <b>The tripwire is published inverted, and that is a rule rather than a preference.</b> S2 R3
/// drafted its budget row as <em>routing is 6.4× over budget</em>, which multiplies a measured
/// per-route cost by a Trip arrival rate nobody had measured — <em>a wire whose denominator is a
/// guess fires on the guess</em>. Stated the other way round — <em>chain walking fits while fewer
/// than N evaluations occur per Tick</em> — the published quantity is a measured cost over a world
/// constant, and it survives the arrival rate being measured elsewhere. <c>02 §4</c> asks for exactly
/// this shape and adds a second prohibition: never state it over a <em>depth</em>, because a depth
/// nobody can price is an argument wearing a measurement's clothes.
/// </para>
/// <para>
/// <b>Phase 2 is measured alone because Phase 2 is the phase that writes nothing.</b>
/// <c>adr/0037</c> makes Decide read-only and <c>Simulation.VerifyDecideWritesNothing</c> proves it,
/// so <see cref="RuleEngine.Evaluate"/> can be called a million times over one arrangement and the
/// millionth call does the same work as the first. Every alternative — stepping the world, or
/// rebuilding it per iteration — measures either a starving city or a constructor.
/// </para>
/// <para>
/// <b>The denominator is asserted rather than assumed.</b> How many evaluations one invocation
/// performs is a property of the fixture, and a benchmark dividing by a number nobody checked is the
/// shape S2 hit three times. <c>RuleEngineBenchmarkFixtureTests</c> holds the same three arrangements
/// against the engine's own counter, so the figure this is divided by is measured on the same code.
/// </para>
/// <para>
/// <b>Three populations because the claim is about the slope.</b> A per-evaluation cost that is
/// stable across two orders of magnitude extrapolates; one that is not is a cache effect being
/// reported as a unit cost.
/// </para>
/// </remarks>
[MemoryDiagnoser]
public class RuleEngineBenchmarks
{
    /// <summary>The shape of the Tick being evaluated.</summary>
    public enum Load
    {
        /// <summary>Every Bin full: each due Rule walks all its terms and succeeds. One evaluation.</summary>
        Fires,

        /// <summary>Every Bin empty and no <c>on_fail</c>: each due Rule stops at its first term. One evaluation.</summary>
        Starves,

        /// <summary>Every Bin empty, with a two-link ladder ending in a report. Three evaluations, three rungs.</summary>
        Walks,

        /// <summary>
        /// A Rule with no terms at all, armed at rate 1: the engine's fixed cost per due Rule, and
        /// nothing else.
        /// </summary>
        /// <remarks>
        /// <b>Degenerate on purpose, and it is the only shape that makes a whole Tick repeatable.</b>
        /// It touches no Bin, so it fires every Tick, re-arms every Tick, and leaves the world
        /// exactly as it found it — which means <c>Step</c> can be called a hundred thousand times
        /// over one arrangement. What it prices is the per-due-Rule overhead every Rule pays whatever
        /// its terms: the Wheel pop, the settle-order draw, **the sort**, the re-check's dispatch and
        /// the re-arm. The sort is the reason this shape exists, being the one part of the engine
        /// that is not linear in the due count.
        /// </remarks>
        Idle,
    }

    private Simulation _simulation = null!;
    private Ticks _due;

    [Params(1_000, 10_000, 100_000)]
    public int Buildings { get; set; }

    [Params(Load.Fires, Load.Starves, Load.Walks)]
    public Load Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        (_simulation, _due) = RuleEngineFixture.Arrange(Shape, Buildings);

        // Phase 1, once. Evaluate reads the collected list and never clears it — only Apply does,
        // and Apply is what would make this arrangement single-use.
        _simulation.Rules.CollectDue(_due);
    }

    /// <summary>One Tick's Phase 2 over the whole due set.</summary>
    [Benchmark(Description = "decide")]
    public void Evaluate() => _simulation.Rules.Evaluate(_due);
}

/// <summary>
/// The three arrangements <see cref="RuleEngineBenchmarks"/> times, built where a test can also
/// assert what they cost in evaluations.
/// </summary>
/// <remarks>
/// <b>Shared so that the published denominator and the measured one are the same object.</b> A
/// benchmark holding its own copy of the fixture is a benchmark whose divisor can drift away from the
/// test that checks it, silently and in the direction that makes the number look better.
/// </remarks>
internal static class RuleEngineFixture
{
    internal static readonly ResourceId Flour = new(1);
    internal static readonly ResourceId Bread = new(2);
    internal static readonly ResourceId Grain = new(3);

    private static readonly ConditionId InputStarved = new(1);

    private const byte Bakery = 1;
    private const uint Rate = 8;

    /// <summary>
    /// A world of bakeries, all armed for one Tick, and the Tick they are due on.
    /// </summary>
    /// <remarks>
    /// <b>Every Building is due on the same Tick deliberately.</b> A real city's Rules are spread
    /// across the Wheel by construction order, so a Tick like this one does not occur — which is
    /// precisely why it is the right thing to time. The tripwire is a statement about the worst Tick
    /// the engine can be handed, and staggering the arrangement would measure the Wheel's spreading
    /// rather than the engine's cost.
    /// </remarks>
    internal static (Simulation Simulation, Ticks Due) Arrange(
        RuleEngineBenchmarks.Load shape, int buildings)
    {
        Ruleset ruleset = shape switch
        {
            RuleEngineBenchmarks.Load.Walks => Laddered(),
            RuleEngineBenchmarks.Load.Idle => Idle(),
            _ => Flat(),
        };

        // World sizing is per Citizen and the ratio is 150 Buildings per 1,000, so this asks for the
        // population that would carry the Building count rather than growing the tables mid-arrange.
        var world = new World(Math.Max(1_000, buildings * 1_000 / 150), LayerRuleset.Default, ruleset);
        var simulation = new Simulation(world, WorldKey.FromSeed(1));

        for (int i = 0; i < buildings; i++)
        {
            Handle<Lot> lot = world.Lots.Create(new Tiles(i % 2_048), new Tiles(i / 2_048), zone: 1);
            Handle<Building> building = world.Buildings.Create(world.Lots, lot, Bakery);

            foreach (BinDeclaration bin in ruleset.BinsOf(Bakery))
            {
                Handle<Bin> slot = world.CreateBin(building, bin.Resource, bin.Capacity);

                if (shape is RuleEngineBenchmarks.Load.Fires)
                {
                    world.Deposit(slot, 6, Ticks.Zero);
                }
            }

            foreach (RuleId rule in ruleset.RulesOf(Bakery))
            {
                world.CreateRuleInstance(building, rule, Ticks.Zero, delay: 1);
            }
        }

        return (simulation, new Ticks(1));
    }

    /// <summary>
    /// A Rule with no terms and no Bins, armed at rate 1 so that every Tick is a busy one.
    /// </summary>
    /// <remarks>
    /// <b>The point of it is that a Tick leaves the world unchanged</b>, so <c>Step</c> is repeatable
    /// and a whole Tick can be measured without rebuilding anything. What survives is every fixed
    /// per-due-Rule cost — the Wheel pop and re-arm, the settle-order draw, the intent sort, and the
    /// dispatch of an atomicity check with nothing in it.
    /// </remarks>
    private static Ruleset Idle() => new(
        resources: [],
        rules:
        [
            new RuleDefinition(Bakery, 1, ApplyCount.Band(1, 1), RuleId.None,
                false, default, ConditionId.None, 0, 0, 0, 0, 0, 0),
        ],
        kinds: [new KindDefinition(0, 0, 0, 1)],
        inputs: [],
        outputs: [],
        emissions: [],
        bins: [],
        kindRules: [new RuleId(1)]);

    /// <summary><c>02 §4.3</c>'s bakery with no fallback: six flour in, four bread out.</summary>
    private static Ruleset Flat() => new(
        resources: [ResourceFamily.Good, ResourceFamily.Good],
        rules:
        [
            new RuleDefinition(Bakery, Rate, ApplyCount.Band(1, 1), RuleId.None,
                false, default, ConditionId.None, 0, 1, 0, 1, 0, 0),
        ],
        kinds: [new KindDefinition(0, 2, 0, 1)],
        inputs: [new Term(new BinRef(Scope.Local, Flour), 6)],
        outputs: [new Term(new BinRef(Scope.Local, Bread), 4)],
        emissions: [],
        bins: [new BinDeclaration(Flour, BinCapacity.Of(60)), new BinDeclaration(Bread, BinCapacity.Of(20))],
        kindRules: [new RuleId(1)]);

    /// <summary>The same bakery over a two-link ladder ending in a report.</summary>
    private static Ruleset Laddered() => new(
        resources: [ResourceFamily.Good, ResourceFamily.Good, ResourceFamily.Good],
        rules:
        [
            new RuleDefinition(Bakery, Rate, ApplyCount.Band(1, 1), new RuleId(2),
                false, default, ConditionId.None, 0, 1, 0, 1, 0, 0),
            new RuleDefinition(Bakery, Rate, ApplyCount.Band(1, 1), new RuleId(3),
                false, default, ConditionId.None, 1, 1, 1, 1, 0, 0),
            new RuleDefinition(Bakery, Rate, ApplyCount.Band(1, 1), new RuleId(4),
                false, default, ConditionId.None, 2, 1, 2, 1, 0, 0),
            new RuleDefinition(Bakery, Rate, ApplyCount.Band(1, 1), RuleId.None,
                false, default, InputStarved, 0, 0, 0, 0, 0, 0),
        ],
        kinds: [new KindDefinition(0, 3, 0, 1)],
        inputs:
        [
            new Term(new BinRef(Scope.Local, Flour), 6),
            new Term(new BinRef(Scope.Local, Grain), 6),
            new Term(new BinRef(Scope.Local, Grain), 12),
        ],
        outputs:
        [
            new Term(new BinRef(Scope.Local, Bread), 4),
            new Term(new BinRef(Scope.Local, Flour), 6),
            new Term(new BinRef(Scope.Local, Flour), 12),
        ],
        emissions: [],
        bins:
        [
            new BinDeclaration(Flour, BinCapacity.Of(60)),
            new BinDeclaration(Bread, BinCapacity.Of(20)),
            new BinDeclaration(Grain, BinCapacity.Of(60)),
        ],
        kindRules: [new RuleId(1)]);
}
