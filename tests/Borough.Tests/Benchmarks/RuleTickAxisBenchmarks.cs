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
/// <b>Why a whole engine Tick costs 3.4× in a real city what it costs in a benchmark.</b>
/// </summary>
/// <remarks>
/// <para>
/// <c>RuleTickBenchmarks</c> measures **121.6 → 198.3 ns** per due Rule and
/// [`0013`](../../../plans/0013-tick-budget.md) prices the engine off it. The first in-situ capture —
/// 1M Citizens, <c>rulesets/minimal.toml</c> in force, per-Tick wall-clock slope — reads about
/// **626 ns** per due Rule. That gap is not noise and it is not the sort, which the synthetic figure
/// already contains.
/// </para>
/// <para>
/// <b>The synthetic fixture is best case on three independent axes at once, and none of them was
/// chosen to flatter it.</b> Each was the obvious way to make a Tick repeatable or a fixture cheap:
/// </para>
/// <list type="number">
/// <item><b>No terms.</b> The Rule touches no Bin, which is what leaves the world bit-identical and
/// lets one arrangement be stepped thousands of times. A real Rule resolves a Bin per term through
/// <c>World.FindBin</c>, which is a search down the Building's intrusive Bin list.</item>
/// <item><b>Everything due at once, in slot order.</b> Every Rule Instance is armed at
/// <c>delay: 1</c>, so one Wheel bucket holds the whole city and Phase 2 walks Building and Bin rows
/// in ascending slot order — a sequential scan the prefetcher can follow. A real city is staggered
/// over <c>[1, rate]</c>, so a Tick's due set is a scattered <c>1/rate</c> sample of the table.</item>
/// <item><b>No Citizens and no Households.</b> The fixture sizes the world for them and creates
/// none, so the tables the engine walks have the cache to themselves. At 1M the real run holds
/// 1,000,000 Citizen rows and 360,000 Household rows in the same 177 MB working set.</item>
/// </list>
/// <para>
/// <b>Every row below holds the due count fixed at <see cref="Due"/>.</b> That is the point of the
/// design: the sort is <c>O(n log n)</c> in the due count and is the one part of the engine that is
/// not linear, so a comparison that let the due count move would attribute the sort's curvature to
/// whichever axis happened to change it. Holding it fixed means each row's ratio to
/// <see cref="Baseline"/> is that axis and nothing else.
/// </para>
/// <para>
/// <b>What the terms row measures is the term <em>walk</em> and not the affordability arithmetic.</b>
/// Its Rule draws one and returns one from each of two Bins, so the net delta per Bin is zero — four
/// terms resolved, four <c>FindBin</c> searches, four <c>Touch</c> merges, and then nothing applied,
/// which is what keeps the Tick repeatable. <c>RuleEngine.Check</c> skips the level and headroom read
/// for a Bin whose net delta is zero, so that half is missing and this row is a floor on the term
/// axis. Recorded rather than worked around: making it exact costs the repeatability that makes the
/// whole class measurable, which is the trade <c>RuleTickBenchmarks</c> already had to make.
/// </para>
/// </remarks>
[MemoryDiagnoser]
public class RuleTickAxisBenchmarks
{
    /// <summary>The due count every row holds fixed. Near the 11,585 the 1M in-situ run measured.</summary>
    private const int Due = 10_000;

    /// <summary>
    /// The stagger the scattered row uses, and therefore how much bigger its table is.
    /// </summary>
    /// <remarks>
    /// 16 Buildings exist per Building due, which is the arrangement <c>rulesets/minimal.toml</c>
    /// produces at 1M: 120,001 Buildings, 240,002 Rule Instances, 11,585 due per Tick.
    /// </remarks>
    private const int Stagger = 16;

    private const byte Kind = 1;

    private static readonly ResourceId First = new(1);
    private static readonly ResourceId Second = new(2);

    private Simulation _baseline = null!;
    private Simulation _terms = null!;
    private Simulation _scattered = null!;
    private Simulation _populated = null!;

    [GlobalSetup]
    public void Setup()
    {
        _baseline = Arrange(NoTerms(rate: 1), Due, stagger: 1, citizens: 0);
        _terms = Arrange(BalancedTerms(rate: 1), Due, stagger: 1, citizens: 0);
        _scattered = Arrange(NoTerms(rate: Stagger), Due * Stagger, Stagger, citizens: 0);
        _populated = Arrange(NoTerms(rate: 1), Due, stagger: 1, citizens: 1_000_000);
    }

    /// <summary>Every Rule due, in slot order, no terms, no Citizens — the fixture as it stands.</summary>
    [Benchmark(Baseline = true, Description = "tick: baseline")]
    public void Baseline() => _baseline.Step(TickInput.Empty);

    /// <summary>The same Tick, with four terms to resolve and nothing to apply.</summary>
    [Benchmark(Description = "tick: + terms")]
    public void Terms() => _terms.Step(TickInput.Empty);

    /// <summary>The same due count, drawn from a table sixteen times larger by a stagger.</summary>
    [Benchmark(Description = "tick: + scattered")]
    public void Scattered() => _scattered.Step(TickInput.Empty);

    /// <summary>The same Tick, with a million Citizens and their Households in the working set.</summary>
    [Benchmark(Description = "tick: + population")]
    public void Populated() => _populated.Step(TickInput.Empty);

    /// <summary>
    /// Builds a world of <paramref name="buildings"/> Buildings with their Rules armed across
    /// <paramref name="stagger"/> Ticks, and steps it until the arrangement is warm and cyclic.
    /// </summary>
    /// <remarks>
    /// <b>The warm-up length is the stagger, not a constant.</b> A staggered arrangement's Tick is not
    /// the same Tick every time — it is the same Tick every <c>stagger</c> Ticks — so the arrangement
    /// is only in steady state once every bucket has been drained at least once. BenchmarkDotNet then
    /// averages over the cycle, which is the mean per-Tick cost and is exactly what the ledger wants.
    /// </remarks>
    private static Simulation Arrange(Ruleset ruleset, int buildings, int stagger, int citizens)
    {
        // Sized per Citizen at 150 Buildings per 1,000, so the tables are not grown mid-arrange.
        int population = Math.Max(citizens, Math.Max(1_000, buildings * 1_000 / 150));

        var world = new World(population, ruleset);
        var simulation = new Simulation(world, WorldKey.FromSeed(1))
        {
            // O(world) twice per Tick, and 490% of a Tick at 1M on its own. Leaving it on would
            // measure the guard rather than the engine, which is RuleTickBenchmarks' own note.
            VerifyDecideWritesNothing = false,
        };

        var made = new Handle<Building>[buildings];

        for (int i = 0; i < buildings; i++)
        {
            Handle<Lot> lot = world.Lots.Create(new Tiles(i % 2_048), new Tiles(i / 2_048), zone: 1);
            made[i] = world.Buildings.Create(world.Lots, lot, Kind);

            foreach (BinDeclaration bin in ruleset.BinsOf(Kind))
            {
                Handle<Bin> slot = world.CreateBin(made[i], bin.Resource, bin.Capacity);

                // Stocked, so a balanced Rule's draw is affordable at every count. A Bin at zero
                // would make the terms row measure a failing Rule, which stops at the first Bin and
                // is the cheaper evaluation — the same trap the 82.84 ns figure had to avoid.
                world.Deposit(slot, 8, Ticks.Zero);
            }

            foreach (RuleId rule in ruleset.RulesOf(Kind))
            {
                world.CreateRuleInstance(made[i], rule, Ticks.Zero, (uint)(1 + (i % stagger)));
            }
        }

        // The Citizens and Households the real Tick carries in cache and this fixture otherwise does
        // not. They are attached to real Buildings so the world stays coherent; nothing reads them.
        for (int i = 0; i < citizens; i++)
        {
            if (i % 3 == 0)
            {
                world.CreateHousehold(made[i % buildings], lifeStage: (byte)(i % 5));
            }

            world.CreateCitizen(world.Households.Rows.At(i / 3), new Ticks((ulong)i % 8192));
        }

        for (int i = 0; i < stagger + 4; i++)
        {
            simulation.Step(TickInput.Empty);
        }

        return simulation;
    }

    /// <summary>The fixture's own shape: one Rule, no terms, no Bins.</summary>
    private static Ruleset NoTerms(uint rate) => new(
        resources: [],
        rules:
        [
            new RuleDefinition(Kind, rate, ApplyCount.Band(1, 1), RuleId.None,
                false, default, ConditionId.None, 0, 0, 0, 0, 0, 0),
        ],
        kinds: [new KindDefinition(0, 0, 0, 1)],
        inputs: [],
        outputs: [],
        emissions: [],
        bins: [],
        kindRules: [new RuleId(1)],
        zoneRules: []);

    /// <summary>
    /// Four terms whose net delta per Bin is zero: the bakery's term count, applied to nothing.
    /// </summary>
    private static Ruleset BalancedTerms(uint rate) => new(
        resources: [ResourceFamily.Good, ResourceFamily.Good],
        rules:
        [
            new RuleDefinition(Kind, rate, ApplyCount.Band(1, 1), RuleId.None,
                false, default, ConditionId.None, 0, 2, 0, 2, 0, 0),
        ],
        kinds: [new KindDefinition(0, 2, 0, 1)],
        inputs: [new Term(new BinRef(Scope.Local, First), 1), new Term(new BinRef(Scope.Local, Second), 1)],
        outputs: [new Term(new BinRef(Scope.Local, First), 1), new Term(new BinRef(Scope.Local, Second), 1)],
        emissions: [],
        bins: [new BinDeclaration(First, BinCapacity.Of(60)), new BinDeclaration(Second, BinCapacity.Of(60))],
        kindRules: [new RuleId(1)],
        zoneRules: []);
}
