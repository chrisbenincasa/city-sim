using System.Diagnostics;
using Borough.Core.Arithmetic;
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
/// What the catchment pass costs, and how much of a map it actually resolves. <b>An instrument.</b>
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>The third whole-map sweep of this milestone, and it is measured on the day it is written</b>
/// — <c>plans/0042</c> <b>F7</b> is this milestone getting burned by an unmeasured one already.
/// ⚠ <b>It is NOT a Tick cost and does not belong in <c>plans/0013</c></b>: the pass runs once, at
/// world creation, beside the noise field it reads. What it prices is how long a new world takes to
/// exist, which is a different budget with no ceiling written down.
/// </para>
/// <para>
/// ⚠ <b>A Cell on a filled flat is NOT a pit and the column saying so is not a defect count.</b>
/// Filling a depression makes it level, so a Cell in one has no STRICTLY lower neighbour — it has an
/// equal one that drains where it does, which is what <c>CatchmentTests.No_dry_Cell_is_a_pit</c>
/// asserts. The number is how much of the map is filled basin, and nothing is wrong when it is large.
/// </para>
/// <para>
/// ⚠ <b>It checks its own reconstruction before it trusts its clock.</b> The three arguments are
/// rebuilt from a generated world rather than taken from the generator, so the fingerprint assertion
/// is what says the thing being timed is the thing that ran. It asserts nothing about the clock and
/// cannot fail on a noisy machine — the figure names the reference machine or it is not a figure
/// (<c>adr/0106</c>), and a runner may report that this broke but may never supply a number a
/// document quotes (<c>adr/0121</c>).
/// </para>
/// </remarks>
[Trait(Tier.Key, Tier.Instrument)]
public sealed class CatchmentCostTests(ITestOutputHelper output)
{
    private const int Citizens = 1_000;

    private static readonly ulong[] Seeds = [1UL, 24_006UL, 770_413UL, 8_675_309UL, ulong.MaxValue];

    [Fact]
    public void One_catchment_pass_over_a_whole_map()
    {
        RulesetLoadResult result = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "coastal.toml"));

        Ruleset ruleset = result.Ruleset
            ?? throw new InvalidOperationException($"coastal.toml was refused:\n{result.Describe()}");

        output.WriteLine(
            $"WaterGenerator.Catchments over {CellGrid.WorldCells}x{CellGrid.WorldCells} = "
            + $"{CellGrid.WorldCellCount} Cells, rulesets/coastal.toml, {Seeds.Length} keys.");
        output.WriteLine(string.Empty);

        foreach (ulong seed in Seeds)
        {
            WorldKey key = WorldKey.FromSeed(seed);
            World world = new(Citizens, ruleset, key);
            SyntheticCity.PopulateInto(world, key, Ticks.Zero);

            // The generator's own three arguments, rebuilt. label carries a body number plus one and
            // 0 means dry, which is the encoding LayInto uses and the reason a zeroed array is
            // already correct for every dry Cell.
            int[] height = ValueNoise.Field(key, PurposeTag.TerrainType);
            var label = new int[CellGrid.WorldCellCount];
            var handles = new Handle<WaterBody>[world.Water.Rows.SlotCount];

            for (int slot = 0; slot < world.Water.Rows.SlotCount; slot++)
            {
                handles[slot] = world.Water.Rows.At(slot);
            }

            for (int slot = 0; slot < world.WaterCells.Rows.SlotCount; slot++)
            {
                if (!world.WaterCells.Rows.IsLive(slot))
                {
                    continue;
                }

                int cell = CellGrid.Index(world.WaterCells.East[slot], world.WaterCells.North[slot]);
                label[cell] = world.Water.Rows.Resolve(world.WaterCells.Body[slot]) + 1;
            }

            var into = new CatchmentCellTable(world.Water);

            var clock = Stopwatch.StartNew();
            int[] filled = WaterGenerator.Catchments(into, height, label, handles);
            clock.Stop();

            Assert.Equal(world.Catchment.Fingerprint(), into.Fingerprint());

            int wet = 0;
            int drains = 0;
            int nowhere = 0;
            int flats = 0;

            for (int cell = 0; cell < CellGrid.WorldCellCount; cell++)
            {
                int eastRaw = cell % CellGrid.WorldCells;
                int northRaw = IntegerMath.FloorDiv(cell, CellGrid.WorldCells);
                var east = new Cells(eastRaw);
                var north = new Cells(northRaw);

                if (world.WaterInCells.IsWet(east, north))
                {
                    wet++;

                    continue;
                }

                if (world.Catchment.At(east, north) != default) { drains++; }
                else { nowhere++; }

                bool lower = false;

                for (int step = 0; step < 4 && !lower; step++)
                {
                    int nextEast = step switch { 0 => eastRaw + 1, 2 => eastRaw - 1, _ => eastRaw };
                    int nextNorth = step switch { 1 => northRaw + 1, 3 => northRaw - 1, _ => northRaw };

                    if (nextEast < 0
                        || nextNorth < 0
                        || nextEast >= CellGrid.WorldCells
                        || nextNorth >= CellGrid.WorldCells)
                    {
                        continue;
                    }

                    lower = filled[(nextNorth * CellGrid.WorldCells) + nextEast] < filled[cell];
                }

                if (!lower) { flats++; }
            }

            output.WriteLine(
                $"seed {seed,20}: {clock.Elapsed.TotalMilliseconds,7:F3} ms; "
                + $"{wet,7} wet, {drains,7} dry Cells reach a body "
                + $"({drains * 100 / CellGrid.WorldCellCount,2}% of the map), "
                + $"{nowhere,7} leave the map, {flats,6} sit on a filled flat.");
        }

        output.WriteLine(string.Empty);
        output.WriteLine(
            "ONCE PER WORLD, NOT ONCE PER TICK. A share here is what these keys realised at "
            + "coastal.toml's sea level; another key differs. Quote the sentence, not the digits.");
    }
}
