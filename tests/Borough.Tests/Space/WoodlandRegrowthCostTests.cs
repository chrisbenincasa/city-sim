using System.Diagnostics;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Xunit.Abstractions;

namespace Borough.Tests.Space;

/// <summary>
/// What one regrowth pass costs, because it is a whole-map sweep and this milestone has been here.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b><c>plans/0042</c> F7 is this milestone getting burned by an unmeasured whole-map sweep
/// already</b>, and <c>02 §10</c> names the shape as the wrong one. <c>MapLayers.RegrowWoodland</c>
/// walks all <see cref="CellGrid.WorldCellCount"/> Cells anyway, because ***forest grows where the
/// city is not*** — the sparse residency index holds exactly the Cells something happened to, which is
/// the complement of the set this pass cares about. ***A sweep taken deliberately is measured on the
/// day it is written***, which is <c>adr/0073</c>: the cost goes to <c>plans/0013</c> rather than
/// staying here as a hope.
/// </para>
/// <para>
/// ⚠ <b>It asserts nothing about the clock and cannot fail on a noisy machine.</b> The figure it
/// prints names the reference machine or it is not a figure (<c>adr/0106</c>), and a runner may report
/// that this broke but may never supply a number a document quotes (<c>adr/0121</c>). Its one
/// assertion is that the pass did something, which is the property that would have been false if the
/// step floored to zero.
/// </para>
/// </remarks>
[Trait(Tier.Key, Tier.Instrument)]
public sealed class WoodlandRegrowthCostTests(ITestOutputHelper output)
{
    private const int Passes = 64;

    [Fact]
    public void One_regrowth_pass_over_a_whole_map()
    {
        WorldKey key = WorldKey.FromSeed(0x5EA1U);
        World world = new(4_000, Ruleset.Empty.WithLayers(new LayerRuleset(
            LayerSchedule.Default,
            LayerRates.From(8, LayerRates.DefaultPollutionDecayTicks, 64, woodlandRegrowthDays: 512))),
            key);

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        long laid = 0;

        for (int cell = 0; cell < CellGrid.WorldCellCount; cell++)
        {
            laid += world.Layers.Woodland.Potential[cell];
            world.Layers.Woodland.Tiles[cell] = 0;
        }

        // Warm, so the figure is the loop and not the first touch of a 1 MB pair of arrays.
        world.Layers.RegrowWoodland();

        var clock = Stopwatch.StartNew();

        for (int pass = 0; pass < Passes; pass++)
        {
            world.Layers.RegrowWoodland();
        }

        clock.Stop();

        long standing = 0;

        for (int cell = 0; cell < CellGrid.WorldCellCount; cell++)
        {
            standing += world.Layers.Woodland.Tiles[cell];
        }

        double perPass = clock.Elapsed.TotalMilliseconds / Passes;

        output.WriteLine($"{CellGrid.WorldCellCount:N0} Cells, {laid:N0} Tiles of forest laid.");
        output.WriteLine($"one pass: {perPass:F3} ms");
        output.WriteLine($"amortised over a Day of {Ticks.PerDay} Ticks: {perPass / Ticks.PerDay:F6} ms a Tick");
        output.WriteLine($"after {Passes + 1} passes: {standing:N0} Tiles back, {100.0 * standing / laid:F1}%");

        Assert.True(standing > 0, "the pass put nothing back.");
    }
}
