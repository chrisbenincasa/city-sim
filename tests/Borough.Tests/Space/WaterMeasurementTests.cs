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
/// What <c>rulesets/coastal.toml</c>'s sea level actually produces. <b>An instrument.</b>
/// </summary>
/// <remarks>
/// <para>
/// <c>plans/0032</c>: this produces a figure for a document to quote, and re-running it re-derives a
/// constant that did not move. It is not asking whether the city is correct — <see cref="WaterTests"/>
/// does that — so it must never gate a commit.
/// </para>
/// <para>
/// ⚠ <b>Every figure it prints is a fact about the worlds it measured</b>, and a document quoting one
/// names the file, the seeds and the key count with it. <c>[water] sea_level_percent</c> is a LEVEL;
/// what share of a map ends up wet falls out of the key's own field as well, so two worlds at one sea
/// level differ. <c>plans/0012</c> <b>Cause 5</b> is what happens when the share travels without that
/// clause.
/// </para>
/// </remarks>
[Trait(Tier.Key, Tier.Instrument)]
public sealed class WaterMeasurementTests(ITestOutputHelper output)
{
    private const int Citizens = 1_000;

    private static readonly ulong[] Seeds = [1UL, 24_006UL, 770_413UL, 8_675_309UL, ulong.MaxValue];

    private static Ruleset Load(string file)
    {
        RulesetLoadResult result = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

        return result.Ruleset
            ?? throw new InvalidOperationException($"{file} was refused:\n{result.Describe()}");
    }

    private static int Live<T>(Rows<T> rows)
        where T : unmanaged
    {
        int live = 0;

        for (int slot = 0; slot < rows.SlotCount; slot++)
        {
            if (rows.IsLive(slot))
            {
                live++;
            }
        }

        return live;
    }

    /// <summary>Prints the water share, the body count and the drainage split per key.</summary>
    [Fact]
    public void How_much_water_the_shipped_sea_level_lays()
    {
        Ruleset ruleset = Load("coastal.toml");

        output.WriteLine(
            $"rulesets/coastal.toml, [water] sea_level_percent = "
            + $"{ruleset.Water.SeaLevelPercent}, {Seeds.Length} keys, "
            + $"{CellGrid.WorldCells}x{CellGrid.WorldCells} Cells.");
        output.WriteLine(string.Empty);

        foreach (ulong seed in Seeds)
        {
            WorldKey key = WorldKey.FromSeed(seed);
            World world = new(Citizens, ruleset, key);
            SyntheticCity.PopulateInto(world, key, Ticks.Zero);

            int wet = Live(world.WaterCells.Rows);
            int bodies = Live(world.Water.Rows);
            int largest = 0;
            var size = new int[world.Water.Rows.SlotCount];
            var touchesEdge = new bool[world.Water.Rows.SlotCount];

            for (int slot = 0; slot < world.WaterCells.Rows.SlotCount; slot++)
            {
                if (!world.WaterCells.Rows.IsLive(slot))
                {
                    continue;
                }

                int body = world.Water.Rows.Resolve(world.WaterCells.Body[slot]);
                size[body]++;

                if (size[body] > largest)
                {
                    largest = size[body];
                }

                int east = world.WaterCells.East[slot].Raw;
                int north = world.WaterCells.North[slot].Raw;

                if (east == 0
                    || north == 0
                    || east == CellGrid.WorldCells - 1
                    || north == CellGrid.WorldCells - 1)
                {
                    touchesEdge[body] = true;
                }
            }

            // The two reasons a Downstream is unset, counted APART. Reaching the map's edge is the
            // designed terminus; an endorheic body is the generator's stated coarseness -- it spills
            // into a hollow that holds no water, and where that water then goes needs a volume, which
            // is a Bin, which is task 6b. One number covering both would hide the second.
            int toSea = 0;
            int endorheic = 0;
            int toBody = 0;

            for (int slot = 0; slot < world.Water.Rows.SlotCount; slot++)
            {
                if (!world.Water.Rows.IsLive(slot))
                {
                    continue;
                }

                if (!world.Water.Downstream[slot].IsNone) { toBody++; }
                else if (touchesEdge[slot]) { toSea++; }
                else { endorheic++; }
            }

            output.WriteLine(
                $"seed {seed,20}: {wet,7} wet Cells "
                + $"({wet * 100 / CellGrid.WorldCellCount,2}% of the map), {bodies,4} bodies "
                + $"[{toSea,4} reach the map edge, {toBody,3} drain into another body, "
                + $"{endorheic,4} endorheic], largest body {largest,6} Cells.");
        }

        output.WriteLine(string.Empty);
        output.WriteLine(
            "A LEVEL, NOT A COVERAGE. The share above is what these keys realised at this sea "
            + "level; another key at the same level differs. Quote the sentence, not the digits.");
    }
}
