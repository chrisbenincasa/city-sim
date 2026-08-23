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
/// What a generated city actually Seals, per Cell, split between roads and Buildings.
/// </summary>
/// <remarks>
/// <para>
/// <b>This exists because the question it answers was being settled by argument.</b> How much ground
/// a Building covers was decided twice from prose — once from <c>CONTEXT.md</c> → Sealing's <em>"one
/// house seals 1/1024 of its Cell"</em> and once against <c>adr/0022</c>'s <em>"ground sealed 12%"</em>
/// — while the quantity itself had never been observed, because nothing in the build wrote the column
/// at all. <c>adr/0043</c> says a claim a measurement could settle must not be settled by argument,
/// and this is the measurement.
/// </para>
/// <para>
/// ⚠ <b>It asserts almost nothing and it is not a regression test.</b> Its two assertions are that
/// roads seal and that Buildings seal, which is the pair that was identically zero before. The
/// numbers it prints are what a document may quote, and they will move the moment
/// <c>footprint_tiles</c> is retuned — which is the point of the key.
/// </para>
/// </remarks>
[Trait(Tier.Key, Tier.Instrument)]
public sealed class SealingMeasurementTests(ITestOutputHelper output)
{
    private const int Citizens = 4_000;

    private const byte DwellingKind = 1;

    private static readonly WorldKey Key = WorldKey.FromSeed(0x5EA1U);

    private static string PathTo(string file) =>
        System.IO.Path.Combine(AppContext.BaseDirectory, "Rulesets", file);

    private static Ruleset Load(string file)
    {
        RulesetLoadResult result = RulesetLoader.Load(PathTo(file));

        return result.Ruleset
            ?? throw new InvalidOperationException($"{file} was refused:\n{result.Describe()}");
    }

    /// <summary>What one generated city's ground looks like once it is built.</summary>
    private readonly record struct Reading(
        int Cells, long Total, int Peak, int Buildings, int FootprintTiles);

    private static Reading Measure(string file)
    {
        Ruleset ruleset = Load(file);
        World world = new(Citizens, ruleset, Key);

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        int cells = 0;
        long total = 0;
        int peak = 0;

        for (int slot = 0; slot < world.Layers.Cells.Rows.SlotCount; slot++)
        {
            if (!world.Layers.Cells.Rows.IsLive(slot))
            {
                continue;
            }

            int sealing = world.Layers.Cells.Sealing[slot];

            if (sealing <= 0)
            {
                continue;
            }

            cells++;
            total += sealing;
            peak = sealing > peak ? sealing : peak;
        }

        // The Buildings' share is counted rather than differenced, because every Building seals its
        // kind's footprint exactly once at World.CreateBuilding. The populator raises one kind, so
        // one lookup covers the city. Roads are what is left.
        int footprint = world.Rules.Kind(DwellingKind).FootprintTiles;

        return new Reading(
            cells, total, peak, world.Buildings.Rows.LiveCount, footprint);
    }

    private static string Percent(long tiles, long cells)
    {
        if (cells <= 0)
        {
            return "n/a";
        }

        long thousandths = (tiles * 1000L) / (cells * CellGrid.TilesInCell);

        return $"{thousandths / 10}.{thousandths % 10}%";
    }

    [Theory]
    [InlineData("minimal.toml")]
    [InlineData("severance.toml")]
    [InlineData("twinned.toml")]
    public void What_a_generated_city_seals(string file)
    {
        Reading reading = Measure(file);

        long buildingTiles = (long)reading.Buildings * reading.FootprintTiles;
        long roadTiles = reading.Total - buildingTiles;

        output.WriteLine($"# {file} — {Citizens} Citizens, Cell = {CellGrid.TilesInCell} Tiles");
        output.WriteLine($"Buildings                {reading.Buildings} at "
            + $"footprint_tiles = {reading.FootprintTiles}");
        output.WriteLine($"cells with any Sealing   {reading.Cells}");
        output.WriteLine($"total Tiles sealed       {reading.Total}");
        output.WriteLine($"  of which Buildings     {buildingTiles} "
            + $"({(reading.Total > 0 ? buildingTiles * 100 / reading.Total : 0)}%)");
        output.WriteLine($"  of which roads         {roadTiles} "
            + $"({(reading.Total > 0 ? roadTiles * 100 / reading.Total : 0)}%)");
        output.WriteLine($"mean over sealed Cells   {Percent(reading.Total, reading.Cells)}");
        output.WriteLine($"PEAK Cell                {reading.Peak} Tiles = "
            + $"{Percent(reading.Peak, 1)}");

        // The two facts that were false before this milestone, and nothing else. A number here would
        // be a regression test over a figure that is deliberately a Ruleset's to move.
        Assert.True(roadTiles > 0, "roads sealed nothing");
        Assert.True(buildingTiles > 0, "Buildings sealed nothing");
    }
}
