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
/// How much of a map <c>coastal.toml</c>'s flood level makes Hazard Region. <b>An instrument.</b>
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>This is the figure <c>adr/0156</c>'s storage decision rests on, and that ADR names it as its
/// own revisit trigger</b>: *floodplain depth turns out not to be sparse — if a shipped world's
/// floodplain covers enough of the map, the sparse store stops being cheaper than a dense one and the
/// storage question reopens on cost rather than on principle.* So this instrument is not decoration;
/// it is the thing that would tell somebody the ADR needs reopening.
/// </para>
/// <para>
/// ⚠ <b>Every figure it prints is a fact about the worlds it measured.</b> <c>flood_level_percent</c>
/// is a LEVEL, and what share of a map ends up under it falls out of the key's own height field as
/// well — so two worlds at one flood level differ, exactly as
/// <see cref="WaterMeasurementTests"/> says of the sea. <c>plans/0012</c> <b>Cause 5</b> is what
/// happens when the share travels without that clause.
/// </para>
/// <para>
/// <c>plans/0032</c>: re-running this re-derives a constant that did not move, so it must never gate
/// a commit, and a runner may report that it broke but may never supply a number a document quotes
/// (<c>adr/0121</c>).
/// </para>
/// </remarks>
[Trait(Tier.Key, Tier.Instrument)]
public sealed class FloodplainMeasurementTests(ITestOutputHelper output)
{
    private const int Citizens = 1_000;

    private static readonly ulong[] Seeds = [1UL, 24_006UL, 770_413UL, 8_675_309UL, ulong.MaxValue];

    private static int Live<T>(Rows<T> rows)
        where T : unmanaged
    {
        int live = 0;

        for (int slot = 0; slot < rows.SlotCount; slot++)
        {
            if (rows.IsLive(slot)) { live++; }
        }

        return live;
    }

    [Fact]
    public void How_much_of_a_map_the_shipped_flood_level_puts_at_risk()
    {
        RulesetLoadResult result = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "coastal.toml"));

        Ruleset ruleset = result.Ruleset
            ?? throw new InvalidOperationException($"coastal.toml was refused:\n{result.Describe()}");

        output.WriteLine(
            $"rulesets/coastal.toml, [water] sea_level_percent = {ruleset.Water.SeaLevelPercent}, "
            + $"flood_level_percent = {ruleset.Water.FloodLevelPercent}, {Seeds.Length} keys, "
            + $"{CellGrid.WorldCells}x{CellGrid.WorldCells} = {CellGrid.WorldCellCount} Cells.");
        output.WriteLine(string.Empty);

        foreach (ulong seed in Seeds)
        {
            WorldKey key = WorldKey.FromSeed(seed);
            World world = new(Citizens, ruleset, key);
            SyntheticCity.PopulateInto(world, key, Ticks.Zero);

            int wet = Live(world.WaterCells.Rows);
            int rows = Live(world.Flood.Rows);
            int deepest = 0;
            long total = 0;

            for (int slot = 0; slot < world.Flood.Rows.SlotCount; slot++)
            {
                if (!world.Flood.Rows.IsLive(slot))
                {
                    continue;
                }

                int depth = world.Flood.Depth[slot];
                total += depth;

                if (depth > deepest) { deepest = depth; }
            }

            output.WriteLine(
                $"seed {seed,20}: {wet,7} wet, {rows,7} Cells of Hazard Region "
                + $"({rows * 100 / CellGrid.WorldCellCount,2}% of the map), deepest {deepest,6}, "
                + $"mean depth {(rows == 0 ? 0 : total / rows),6}.");
        }

        output.WriteLine(string.Empty);
        output.WriteLine(
            "A LEVEL, NOT A COVERAGE. The share above is what these keys realised at this flood "
            + "level; another key at the same level differs. Quote the sentence, not the digits.");
        output.WriteLine(
            "adr/0156 REOPENS if this stops being sparse -- the sparse store has to stay cheaper "
            + "than a dense column of 262,144 ints for its storage choice to hold.");
    }
}
