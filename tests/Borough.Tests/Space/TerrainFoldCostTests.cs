using System.Diagnostics;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;
using Xunit.Abstractions;

namespace Borough.Tests.Space;

/// <summary>
/// What a dense terrain table costs a Tick, and <b>which of the two costs is the one that bites.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>This exists because the assertion tier moved and the obvious suspects were the wrong ones.</b>
/// Milestone 24 task 2 added <see cref="TerrainCellTable"/>, and the two costs anybody would predict
/// — allocating 262,144 rows in every <c>new World</c>, and running the generator on every
/// <c>PopulateInto</c> — are **1.9 ms** and **9.9 ms**, which cannot add up to the observed minute.
/// <c>adr/0043</c>: the claim was settled by measuring rather than by picking the likelier story.
/// </para>
/// <para>
/// 🔴 <b>The cost is the fold, and it is paid per TICK rather than per world.</b>
/// <c>Simulation.VerifyDecideWritesNothing</c> is <b>on by default</b> and folds the whole world
/// <b>twice a Tick</b>, and terrain is now most of what a fold walks. ⚠ <b>This is the hazard
/// <c>CLAUDE.md</c> already records for <c>bordered.toml</c>, generalised to every world</b> — that
/// file paved the lattice to the boundary and made the *Layer* table dense, where this makes a
/// *terrain* table dense everywhere. ***The same wire, reached from a third direction.***
/// </para>
/// <para>
/// ⚠ <b>It is an instrument and asserts nothing.</b> Re-running it re-derives constants; what it is
/// for is producing the figures <c>plans/0042</c> <b>F8</b> and <c>plans/0013</c> quote. The numbers
/// move the day the fold, the guard or the table's width does.
/// </para>
/// </remarks>
[Trait(Tier.Key, Tier.Instrument)]
public sealed class TerrainFoldCostTests(ITestOutputHelper output)
{
    private const int Citizens = 1_000;

    private const int Samples = 200;

    private static readonly WorldKey Key = WorldKey.FromSeed(1U);

    private static Ruleset Load(string file)
    {
        RulesetLoadResult result = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

        return result.Ruleset ?? throw new InvalidOperationException(result.Describe());
    }

    private static World Populated()
    {
        World world = new(Citizens, Load("minimal.toml"), Key);
        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        return world;
    }

    [Fact]
    public void What_a_dense_terrain_table_costs()
    {
        World world = Populated();
        ulong sink = 0;

        Rows terrain = world.Layers.Terrain.Rows;
        terrain.Fold(ref sink);

        var watch = Stopwatch.StartNew();

        for (int sample = 0; sample < Samples; sample++)
        {
            ulong hash = 0;
            terrain.Fold(ref hash);
            sink ^= hash;
        }

        watch.Stop();
        output.WriteLine($"PROBE terrain table fold      {Each(watch)} ms");

        sink ^= world.HashState();
        watch.Restart();

        for (int sample = 0; sample < Samples; sample++)
        {
            sink ^= world.HashState();
        }

        watch.Stop();
        output.WriteLine($"PROBE whole-world fold        {Each(watch)} ms");

        // The pair that is the finding. The guard folds twice a Tick, so it prices the table twice.
        foreach (bool guard in new[] { true, false })
        {
            Simulation simulation = new(Populated(), Key) { VerifyDecideWritesNothing = guard };

            for (int warm = 0; warm < 10; warm++)
            {
                simulation.Step(default);
            }

            watch.Restart();

            for (int sample = 0; sample < Samples; sample++)
            {
                simulation.Step(default);
            }

            watch.Stop();
            output.WriteLine($"PROBE Tick, decide-guard {(guard ? "ON " : "OFF")}  {Each(watch)} ms");
        }

        // Read and discarded so that nothing above can be optimised away as dead.
        output.WriteLine($"PROBE (fold sink {sink})");
    }

    private static string Each(Stopwatch watch) =>
        ((double)watch.ElapsedTicks * 1_000 / Stopwatch.Frequency / Samples).ToString("F2");
}
