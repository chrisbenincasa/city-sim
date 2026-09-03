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
/// numbers it prints are what a document may quote.
/// </para>
/// <para>
/// 🔴 <b>RE-POINTED 2026-09-02 BY <c>plans/0052</c> STAGE 1.</b> It read the Buildings' share off
/// <c>footprint_tiles</c>, one lookup for the whole city because the key was a property of the kind.
/// <b>The key is retired and the footprint is now the Lot's parcel</b>, so the share is <em>summed
/// per Building</em> — and it moves when the player draws different blocks rather than when a
/// designer retunes a constant, which is the whole of what stage 1 bought.
/// </para>
/// <para>
/// ⚠ <b>THE SATURATION CHECK IS WHY THE PEAK IS PRINTED.</b> A block is exactly one Cell at shipped
/// figures, so a block's parcels plus its carriageway may exceed 1,024 Tiles. <c>MapLayers.Seal</c>
/// clamps at the write site, so there is no correctness break — but <b>a saturated Cell has Fertility
/// 0 and stops telling two differently-built Cells apart</b>.
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
        int Cells, long Total, int Peak, int Buildings, long BuildingTiles, int Saturated);

    private static Reading Measure(string file)
    {
        Ruleset ruleset = Load(file);
        World world = new(Citizens, ruleset, Key);

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        int cells = 0;
        long total = 0;
        int peak = 0;
        int saturated = 0;

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

            if (sealing >= CellGrid.TilesInCell)
            {
                saturated++;
            }
        }

        // The Buildings' share is counted rather than differenced, because every Building seals its
        // Lot's FOOTPRINT exactly once at World.CreateBuilding. plans/0052 stage 1: SUMMED rather
        // than multiplied, because the footprint is a property of the ground and not of the kind, so
        // two Buildings of one kind on differently-shaped parcels seal different amounts. Roads are
        // what is left.
        //
        // 🔴 IT SUMMED `ParcelTiles` AND THE WORLD SEALS `FootprintTiles`, WHICH plans/0052 STAGE 1
        // MADE TWO DIFFERENT QUANTITIES -- a footprint is a fraction of its parcel. So the residual
        // below was a subtraction of two numbers that are not nested, and it stayed positive only
        // for as long as the roads sealed enough to cover the gap. plans/0055 made the city compact
        // and it went NEGATIVE: on minimal.toml at 4,000 Citizens, 38,064 Tiles of parcel against a
        // layer holding 29,943, so `roads sealed nothing` fired on a city whose roads seal plenty.
        // ***A residual is only a measurement while the thing subtracted is part of the thing it is
        // subtracted from.*** No Cell was clamped in that reading -- peak 509 of 1,024 -- so the
        // clamp at MapLayers.Seal is not what made the difference, and the two quantities simply
        // were not the same one.
        long buildingTiles = 0;

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (!world.Buildings.Rows.IsLive(slot))
            {
                continue;
            }

            if (world.Lots.Rows.TryResolve(world.Buildings.Lot[slot], out int lotSlot))
            {
                int footprint = world.Lots.FootprintTiles(lotSlot);

                buildingTiles += footprint < 1 ? 1 : footprint;
            }
        }

        return new Reading(
            cells, total, peak, world.Buildings.Rows.LiveCount, buildingTiles, saturated);
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

        long buildingTiles = reading.BuildingTiles;
        long roadTiles = reading.Total - buildingTiles;

        output.WriteLine($"# {file} — {Citizens} Citizens, Cell = {CellGrid.TilesInCell} Tiles");
        output.WriteLine($"Buildings                {reading.Buildings} on "
            + $"{buildingTiles} Tiles of footprint, mean "
            + $"{(reading.Buildings > 0 ? buildingTiles / reading.Buildings : 0)}");
        output.WriteLine($"cells with any Sealing   {reading.Cells}");
        output.WriteLine($"total Tiles sealed       {reading.Total}");
        output.WriteLine($"  of which Buildings     {buildingTiles} "
            + $"({(reading.Total > 0 ? buildingTiles * 100 / reading.Total : 0)}%)");
        output.WriteLine($"  of which roads         {roadTiles} "
            + $"({(reading.Total > 0 ? roadTiles * 100 / reading.Total : 0)}%)");
        output.WriteLine($"mean over sealed Cells   {Percent(reading.Total, reading.Cells)}");
        output.WriteLine($"PEAK Cell                {reading.Peak} Tiles = "
            + $"{Percent(reading.Peak, 1)}");
        output.WriteLine($"SATURATED Cells          {reading.Saturated} of {reading.Cells} — a "
            + "saturated Cell has Fertility 0 and no longer distinguishes two built Cells");

        // ⚠ AND THE ROAD FIGURE IS A FLOOR RATHER THAN A COUNT WHEREVER A CELL SATURATES.
        // MapLayers.Seal clamps at CellGrid.TilesInCell, so a Cell that filled up stopped recording
        // what was added to it -- and the residual carries that loss entirely on the road side,
        // since the footprint sum above is taken off the Lots and is not clamped by anything.
        // MEASURED: minimal.toml and twinned.toml saturate NO Cell at 4,000 Citizens and severance
        // .toml saturates 20 of 2,899, so this reads true on two of the three worlds and low on the
        // third. It is stated rather than corrected because the assertion below is a sign test.
        //
        // The two facts that were false before this milestone, and nothing else. A number here would
        // be a regression test over a figure that is deliberately a Ruleset's to move.
        Assert.True(roadTiles > 0, "roads sealed nothing");
        Assert.True(buildingTiles > 0, "Buildings sealed nothing");
    }
}
