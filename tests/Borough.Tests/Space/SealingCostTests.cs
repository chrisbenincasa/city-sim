using System.Diagnostics;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;
using Xunit.Abstractions;

namespace Borough.Tests.Space;

/// <summary>
/// What writing Sealing costs, in Cell rows and in Tick time.
/// </summary>
/// <remarks>
/// <para>
/// <b>Written the day the assertion tier went from 50 seconds to over an hour.</b> Before Sealing had
/// a write path, <c>LayerCellTable</c> was empty on every world that did not emit pollution — which is
/// eight of the ten shipped Rulesets. Sealing at construction materialises a Cell row for every Cell a
/// road touches, which on a lattice paved to the boundary is the whole 512×512 map.
/// </para>
/// <para>
/// ⚠ <b>The suspicion this exists to test is that the cost is the HASH and not the write.</b>
/// <c>Simulation.VerifyDecideWritesNothing</c> defaults to <c>true</c> and folds the world twice a
/// Tick, so a table going from zero rows to a quarter of a million is paid for on every Tick of every
/// test rather than once at generation.
/// </para>
/// </remarks>
[Trait(Tier.Key, Tier.Instrument)]
public sealed class SealingCostTests(ITestOutputHelper output)
{
    private const int Citizens = 4_000;

    private const int TimedTicks = 64;

    private static readonly WorldKey Key = WorldKey.FromSeed(0x5EA1U);

    private static readonly List<(int Tick, long Ms)> Slow = [];

    private static int OffLattice;

    private static int Segments;

    private static Ruleset Load(string file)
    {
        RulesetLoadResult result = RulesetLoader.Load(
            System.IO.Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

        return result.Ruleset
            ?? throw new InvalidOperationException($"{file} was refused:\n{result.Describe()}");
    }

    private static long TimeTicks(string file, bool guard, out int cellRows, out long buildMs)
    {
        World world = new(Citizens, Load(file), Key);
        Simulation simulation = new(world, Key) { VerifyDecideWritesNothing = guard };

        var building = Stopwatch.StartNew();

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        building.Stop();
        buildMs = building.ElapsedMilliseconds;
        cellRows = world.Layers.Cells.Rows.LiveCount;
        OffLattice = world.Roads.Streets.OffLatticeCount;
        Segments = world.Roads.Segments.Rows.LiveCount;

        var ticking = Stopwatch.StartNew();
        long previous = 0;

        for (int tick = 0; tick < TimedTicks; tick++)
        {
            simulation.Step(default);

            long now = ticking.ElapsedMilliseconds;

            if (now - previous > 50)
            {
                Slow.Add((tick, now - previous));
            }

            previous = now;
        }

        ticking.Stop();

        return ticking.ElapsedMilliseconds;
    }

    [Theory]
    [InlineData("minimal.toml")]
    [InlineData("bordered.toml")]
    public void What_sealing_costs(string file)
    {
        Slow.Clear();

        long guarded = TimeTicks(file, guard: true, out int rows, out long buildMs);
        List<(int Tick, long Ms)> slowGuarded = [.. Slow];
        long bare = TimeTicks(file, guard: false, out _, out _);

        output.WriteLine($"# {file} — {Citizens} Citizens, {TimedTicks} Ticks");
        output.WriteLine($"LayerCell rows after build   {rows}");
        output.WriteLine($"Segments                     {Segments}");
        output.WriteLine($"  of which OFF-LATTICE       {OffLattice} "
            + "(scanned twice per noise query)");
        output.WriteLine($"noise queries per pass       {rows * 4} "
            + $"= {(long)rows * 4 * OffLattice * 2:N0} off-lattice visits");
        output.WriteLine($"PopulateInto                 {buildMs} ms");
        output.WriteLine($"{TimedTicks} Ticks, guard ON          {guarded} ms "
            + $"({guarded / (double)TimedTicks:F2} ms/Tick)");
        output.WriteLine($"{TimedTicks} Ticks, guard OFF         {bare} ms "
            + $"({bare / (double)TimedTicks:F2} ms/Tick)");
        output.WriteLine($"guard multiplier             "
            + $"{(bare > 0 ? guarded / (double)bare : 0):F1}x");

        foreach ((int tick, long ms) in slowGuarded)
        {
            output.WriteLine($"  SLOW Tick {tick}: {ms} ms");
        }
    }
}
