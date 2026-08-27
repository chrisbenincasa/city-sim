using BenchmarkDotNet.Attributes;
using Borough.Core;
using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Tests.Benchmarks;

/// <summary>
/// Slice 10's tripwire: <b>a Zone Rule's per-trigger cost does not depend on the size of the Zone it
/// sweeps</b> (<c>02 §5.7</c>).
/// </summary>
/// <remarks>
/// <para>
/// <b>Published as a ratio whose denominator was measured, never as a multiple over a guessed one.</b>
/// The wire is <em>the per-trigger cost at the largest Zone divided by the cost at the smallest</em>.
/// Both ends of that division are timed here, on one machine in one run, so nothing in the published
/// figure came from somewhere else. That is S2 R3's lesson stated as a harness: a wire taken over a
/// guessed divisor fires on the guess.
/// </para>
/// <para>
/// <b>The sweep leaves the world exactly as it found it, which is what makes a trigger repeatable.</b>
/// The Zone Rule admits a permission bit no Lot carries, so <c>Create</c> always returns at its first
/// term; the kind's Rule is armed and never fires, so no Rule Instance is ever starving and
/// <c>Condemn</c> always walks the whole chain and finds nothing. Both branches run at full cost and
/// neither can act. A fixture that let it act would be measuring a city being rebuilt rather than a
/// Zone Rule being swept, and would stop being repeatable on its first trigger.
/// </para>
/// <para>
/// <b>The Tick advances on every invocation, and that is load-bearing.</b> Sampling is keyed on the
/// Tick, so a fixed Tick would draw the same handful of Lots for ever and hold them in cache — which
/// would read 1.00× at every rung by construction and prove nothing about a large Zone. Advancing it
/// makes each invocation touch rows it has not touched recently, which is what a real trigger does.
/// The interval is 1, so every Tick triggers, and the world is inert, so no Tick can change it.
/// </para>
/// <para>
/// <b><see cref="Scan"/> is the rung that is expected to move</b>, and it is here for S2 R3's detour
/// column: a ratio reading 1.00× everywhere is indistinguishable from an instrument that is not wired
/// up. It does a sample's per-Lot admission test over <em>every</em> Lot instead of over a sample, so
/// it is linear in Zone size by construction. If <see cref="Sweep"/> is flat and <see cref="Scan"/> is
/// also flat, the harness is broken and neither column means anything.
/// </para>
/// <para>
/// <b><see cref="Denominator"/> is the smallest Zone, timed once at every rung of the sweep.</b> S2 R3
/// found that a denominator measured once has no error bar and one measured first has a systematic
/// one — its flat search read 1,401,307 ns before the sweep and 477,609 ns after, a spread that would
/// have decorated every ratio it published. This case does not vary with its parameter, so its own
/// column across the rungs <em>is</em> this harness's error bar: if it drifts, the sweep column is
/// measuring the machine.
/// </para>
/// </remarks>
[MemoryDiagnoser]
public class ZoneRuleBenchmarks
{
    /// <summary>Lots in the Zone. Three orders of magnitude, against a fixed sample of 16.</summary>
    [Params(256, 2_560, 25_600, 256_000)]
    public int Lots { get; set; }

    private Simulation _swept = null!;
    private Simulation _smallest = null!;
    private ulong _sweptTick;
    private ulong _smallestTick;

    [GlobalSetup]
    public void Setup()
    {
        _swept = ZoneRuleFixture.Arrange(Lots);
        _smallest = ZoneRuleFixture.Arrange(ZoneRuleFixture.SmallestZone);
        _sweptTick = 0;
        _smallestTick = 0;
    }

    /// <summary>One trigger over a Zone of <see cref="Lots"/> Lots, at a fixed sample size.</summary>
    [Benchmark(Description = "sweep")]
    public void Sweep() => _swept.Zoning.Sweep(new Ticks(++_sweptTick));

    /// <summary>The same trigger over the smallest Zone, at every rung. The harness's error bar.</summary>
    [Benchmark(Description = "denominator")]
    public void Denominator() => _smallest.Zoning.Sweep(new Ticks(++_smallestTick));

    /// <summary>
    /// A sample's per-Lot admission test over every Lot rather than over a sample — the control rung,
    /// which must move.
    /// </summary>
    /// <remarks>
    /// <b>In the harness rather than in the engine, deliberately.</b> Nothing in the simulation scans
    /// a Zone and nothing should; this exists only so the flat column has a non-flat column beside it.
    /// </remarks>
    [Benchmark(Description = "scan (control)")]
    public int Scan()
    {
        LotTable lots = _swept.World.Lots;
        int admissible = 0;

        for (int slot = 0; slot < lots.Rows.SlotCount; slot++)
        {
            if (lots.Rows.IsLive(slot)
                && lots.IsVacant(slot)
                && (lots.Zone[slot] & ZoneRuleFixture.Admits) != 0)
            {
                admissible++;
            }
        }

        return admissible;
    }
}

/// <summary>
/// The arrangement <see cref="ZoneRuleBenchmarks"/> times, built where a test can also hold it to the
/// engine's own counters.
/// </summary>
/// <remarks>
/// <b>Shared so that the published denominator and the checked one are the same object</b>, for
/// <c>RuleEngineFixture</c>'s reason: a benchmark carrying a private copy of its fixture is a
/// benchmark whose divisor can drift away from the test that checks it, silently and in the
/// flattering direction. <c>ZoneRuleBenchmarkFixtureTests</c> is what does the holding.
/// </remarks>
internal static class ZoneRuleFixture
{
    /// <summary>The kind on every occupied Lot, and the kind the Zone Rule would raise if it could.</summary>
    internal const byte House = 1;

    /// <summary>The bit the Zone Rule admits — and the one no Lot in this fixture carries.</summary>
    internal const byte AdmittedBit = 1;

    /// <summary>The permission set the Rule admits.</summary>
    internal const ushort Admits = 1 << AdmittedBit;

    /// <summary>The permission set the Lots carry, which is deliberately not <see cref="Admits"/>.</summary>
    internal const ushort Painted = 1;

    /// <summary>
    /// Lots evaluated per trigger. Held fixed across the sweep — it is the controlled quantity.
    /// </summary>
    /// <remarks>
    /// <b>Holding it fixed now takes work, and that is <c>adr/0059</c> rather than an inconvenience.</b>
    /// A Ruleset states <c>revisit_ticks</c> and the engine derives the sample from the Lot count, so a
    /// fixture that stated one number would have a sample proportional to its rung and would be timing
    /// the very thing it means to control. <see cref="Arrange"/> therefore inverts the derivation per
    /// rung. The tripwire's question is unchanged — <em>is a trigger's cost <c>O(sample)</c> or
    /// <c>O(Zone)</c></em> — and it is worth being explicit that this is the *cost* half of
    /// <c>02 §5.7</c>'s bullet. The *pacing* half, which the same bullet used to carry, is what
    /// <c>adr/0059</c> found false, and no benchmark could have caught it: this fixture was measuring
    /// scale-freedom in cost and passing, while the mechanism's time constant was the city.
    /// </remarks>
    internal const int Sample = 16;

    /// <summary>The bottom rung, and the denominator every ratio is taken over.</summary>
    internal const int SmallestZone = 256;

    /// <summary>Lots per row before the strip wraps, so a Zone is a block rather than a line.</summary>
    private const int LotsPerRow = 512;

    /// <summary>A threshold no run reaches, so the condemn branch walks rather than returning early.</summary>
    private const int NeverCondemned = 1 << 20;

    /// <summary>
    /// A world of <paramref name="lots"/> Lots, half of them built on, under a Zone Rule that can
    /// neither raise a Building nor condemn one.
    /// </summary>
    /// <remarks>
    /// <b>Half built on, because both branches of the sample have to be exercised.</b> An all-vacant
    /// Zone measures the create predicate and an all-occupied one measures the condemn predicate, and
    /// a tripwire taken over either alone would be a statement about half the mechanism. The
    /// proportion is fixed across the sweep so that it is not a second variable.
    /// </remarks>
    internal static Simulation Arrange(int lots)
    {
        // Sizing is per Citizen at 225 Lots per 1,000, so ask for the population that carries the Lot
        // count rather than growing the table in the middle of the arrange.
        int citizens = lots * 1_000 / 225;
        var world = new World(citizens < 1_000 ? 1_000 : citizens, Inert(lots));
        var simulation = new Simulation(world, WorldKey.FromSeed(0x2011_0900_0000_0001UL));

        for (int i = 0; i < lots; i++)
        {
            Handle<Lot> lot = world.Lots.Create(
                new Tiles(i % LotsPerRow), new Tiles(i / LotsPerRow), Painted);

            if (i % 2 == 0)
            {
                world.CreateBuilding(lot, House, Ticks.Zero, simulation.Key);
            }
        }

        return simulation;
    }

    /// <summary>
    /// A Zone Rule that samples and does nothing, over a kind that is armed and never starves.
    /// </summary>
    /// <remarks>
    /// <b>Inert in both directions on purpose, and by two different mechanisms.</b> <c>adr/0055</c>
    /// makes the permission bit scope only what a Rule <em>builds</em>, so admitting a bit no Lot
    /// carries stops creation and leaves the condemn branch running at full cost. The kind therefore
    /// also has to decline to decline, which it does by carrying a threshold nothing reaches rather
    /// than none at all — a threshold of zero would return before the chain walk and quietly measure
    /// less than a trigger does.
    /// </remarks>
    /// <param name="lots">
    /// The rung, which the revisit period is derived backwards from so that <see cref="Sample"/> Lots
    /// are evaluated per trigger at every rung. The interval is 1, so the period is simply
    /// <c>lots ÷ Sample</c>, and every rung divides exactly.
    /// </param>
    private static Ruleset Inert(int lots) => new(
        resources: [ResourceFamily.Good],
        rules:
        [
            new RuleDefinition(
                House, 8, ApplyCount.Band(1, 1), RuleId.None, false, default,
                ConditionId.None, 0, 1, 0, 0, 0, 0),
        ],
        kinds: [new KindDefinition(0, 1, 0, 1) { CondemnAfterTicks = NeverCondemned }],
        inputs: [new Term(new BinRef(Scope.Local, new ResourceId(1)), 1)],
        outputs: [],
        emissions: [],
        bins: [new BinDeclaration(new ResourceId(1), BinCapacity.Of(4))],
        kindRules: [new RuleId(1)],
        zoneRules: [new ZoneRuleDefinition(House, AdmittedBit, 1, IntegerMath.FloorDiv(lots, Sample))]);
}
