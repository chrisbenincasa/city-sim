using System.Globalization;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;

namespace Borough.Headless;

/// <summary>
/// Prints the Lot grid by permission and occupancy, before and after a run, with what the sweep did
/// in between.
/// </summary>
/// <remarks>
/// <para>
/// <b>Slice 10's <em>something to look at</em></b>, and the second thing this project prints that is
/// not a number. The Map Layer dump showed a field; this shows a <em>city changing</em>, which is the
/// first defect class whose symptom is temporal rather than spatial. A Zone Rule that condemns
/// everything, or nothing, or the wrong Lot, produces a State Hash that moves and a census that looks
/// plausible — and is obvious the moment two grids sit next to each other.
/// </para>
/// <para>
/// <b>It requires a Ruleset and says so rather than degrading.</b> Sweeping is a Ruleset's
/// behaviour: with no <c>[[zone_rule]]</c> there is nothing to demonstrate, and a dump that printed
/// an unchanging grid would read as a broken mechanism instead of as an absent one. That is
/// <c>Mode.Layer</c>'s refusal in the other direction — it builds its own sources because no session
/// can place any; this one cannot invent its own Rules, because Rules are content.
/// </para>
/// <para>
/// <b>Every string here belongs to the shell</b> (<c>adr/0002</c>). <c>Borough.Core</c> hands over
/// Lot coordinates, permission bits and counters; the glyphs, the legend and the headings are this
/// file's.
/// </para>
/// </remarks>
internal static class ZoneDump
{
    /// <summary>
    /// Occupied, vacant-and-paintable, vacant-and-unzoned. Three states, because those are the three
    /// a reader has to be able to tell apart to judge a sweep.
    /// </summary>
    /// <remarks>
    /// A glyph rather than a permission digit: the question a picture answers is <em>is the city
    /// filling in or thinning out</em>, and a grid of bit patterns answers a different one. The CSV
    /// form beside it carries the permission set for anybody who wants it.
    /// </remarks>
    private const char Built = '#';
    private const char Vacant = '.';

    /// <summary>
    /// A Lot whose Building has been abandoned and is standing empty.
    /// </summary>
    /// <remarks>
    /// <b>A shell is not a vacant Lot and not a standing Building</b>, and until milestone 17 the
    /// dump had no way to say so -- <c>LotTable.IsVacant</c> asks whether a Building stands here,
    /// which a shell does. The distinction is the whole of what abandonment added: the Lot is
    /// occupied, nobody lives there, and no Household can be placed into it.
    /// </remarks>
    private const char Shell = '~';
    private const char Unzoned = ' ';

    /// <summary>Runs the demonstration and writes it to <paramref name="output"/>.</summary>
    internal static int Run(Options options, TextWriter output)
    {
        if (!Session.TryRules(options.RulesetPath, out Ruleset rules))
        {
            return 2;
        }

        var key = WorldKey.FromSeed(options.Seed);
        World world = new(options.Citizens, rules);
        Simulation simulation = new(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        output.WriteLine("# Borough Zone dump");
        output.WriteLine(
            $"# {world.Lots.Rows.LiveCount} Lots, {world.Buildings.Rows.LiveCount} Buildings, "
            + $"{world.Households.Rows.LiveCount} Households, {rules.ZoneRules.Length} Zone Rules.");

        for (int i = 0; i < rules.ZoneRules.Length; i++)
        {
            ZoneRuleDefinition rule = rules.ZoneRules[i];

            // Both numbers, because only one of them is in the file. adr/0059 states the revisit
            // period and derives the sample from the city, so a dump that printed the authored number
            // alone would leave the reader to do the arithmetic that is the whole subject of the ADR.
            output.WriteLine(
                $"#   rule {i}: kind {rule.Kind}, admits bit {rule.Zone}, every {rule.Interval} "
                + $"Ticks, {rule.SampleFor(world.Lots.Rows.SlotCount)} Lots a trigger — one visit per "
                + $"Lot every {rule.RevisitTicks} Ticks.");
        }

        output.WriteLine();
        output.WriteLine("## Before — the populator's city, one Building on every Lot");
        Write(output, world, options.Csv);

        simulation.Zoning.Drain();

        for (ulong tick = 0; tick < options.Ticks; tick++)
        {
            simulation.Step(default);
        }

        ZoneActivity activity = simulation.Zoning.Drain();

        output.WriteLine();
        output.WriteLine($"## After — {options.Ticks} Ticks of sweeping");
        Write(output, world, options.Csv);

        output.WriteLine();
        output.WriteLine("## What the sweep did");
        output.WriteLine(
            $"{activity.Triggers.Sum} triggers, {activity.Vacant.Sum} vacant Lots evaluated, "
            + $"{activity.Occupied.Sum} occupied. {activity.Created.Sum} Buildings raised, "
            + $"{activity.Demolished.Sum} condemned, {activity.Ended.Sum} tenancies ended.");
        output.WriteLine(
            $"{world.UnplacedPool.Count} Households are in the Unplaced Pool, waiting for somewhere "
            + "to live. A demolition evicts a Building's whole occupancy; a Zone Rule rehouses one "
            + "Household per Building it raises, because a Building has no declared occupancy yet "
            + "(plans/0014 task 10).");

        return 0;
    }

    /// <summary>How much a glyph outranks another when scaling collapses several Lots onto one.</summary>
    private static int Rank(char glyph) => glyph switch
    {
        Built => 3,
        Shell => 2,
        Vacant => 1,
        _ => 0,
    };

    /// <summary>
    /// One row per North coordinate, one glyph per Lot.
    /// </summary>
    /// <remarks>
    /// <b>The grid is the Lots that exist rather than a window on the map</b>, which is the opposite
    /// choice from the Layer dump and is forced by the same reasoning. A Layer is dense — every Cell
    /// has a value, so a window is the only way to fit one on a terminal. Lots are sparse and painted
    /// wherever the populator put them, so a fixed window would print mostly nothing and would hide
    /// exactly the Lots a sweep had been busy on.
    /// </remarks>
    private static void Write(TextWriter output, World world, bool csv)
    {
        LotTable lots = world.Lots;
        int slots = lots.Rows.SlotCount;

        if (csv)
        {
            output.WriteLine("east,north,zone,building");

            for (int slot = 0; slot < slots; slot++)
            {
                if (!lots.Rows.IsLive(slot))
                {
                    continue;
                }

                int building = lots.BuildingOn(slot);

                output.WriteLine(string.Create(
                    CultureInfo.InvariantCulture,
                    $"{lots.East[slot].Raw},{lots.North[slot].Raw},{lots.Zone[slot]},{building}"));
            }

            return;
        }

        // One character per Tile until 5a-bis, and it stopped being able to show the thing the dump
        // is now most worth having. A Lot hangs on a Segment (adr/0078), so at the shipped 32-Tile
        // block the Lots sit on the block's border and the whole interior is empty -- and at one
        // character per Tile that interior is 30 blank columns nobody can tell from unzoned land off
        // the edge of the city. Scaled to eight characters a block it is a hollow square, which is
        // 02 §2.2's "dead block interiors" as a picture rather than as a sentence.
        int scale = Scale(world);

        int east = 0;
        int north = 0;

        for (int slot = 0; slot < slots; slot++)
        {
            if (!lots.Rows.IsLive(slot))
            {
                continue;
            }

            int column = lots.East[slot].Raw / scale;
            int row = lots.North[slot].Raw / scale;

            if (column > east)
            {
                east = column;
            }

            if (row > north)
            {
                north = row;
            }
        }

        // Clipped, and the clip is announced rather than silent. A subdivider carves whole blocks
        // wherever the lattice reaches, so the populator's city is a strip 120 blocks long -- at
        // eight characters a block that is a thousand columns, which is a picture nobody can read
        // and therefore not a picture. The window is the corner nearest the origin, which is where
        // the populator starts.
        int clippedEast = east;
        int clippedNorth = north;

        if (east >= Window)
        {
            east = Window - 1;
        }

        if (north >= Window)
        {
            north = Window - 1;
        }

        // Indexed by position rather than walked per row, because the Lot table is in creation order
        // and nothing guarantees that is scan order -- CONTEXT's Lot is a parcel, not a grid cell.
        char[] grid = new char[(east + 1) * (north + 1)];
        Array.Fill(grid, Unzoned);

        int built = 0;
        int vacant = 0;
        int shells = 0;

        for (int slot = 0; slot < slots; slot++)
        {
            if (!lots.Rows.IsLive(slot))
            {
                continue;
            }

            bool occupied = !lots.IsVacant(slot);
            bool shell = occupied && world.Buildings.IsAbandoned(lots.BuildingOn(slot));
            int column = lots.East[slot].Raw / scale;
            int row = lots.North[slot].Raw / scale;

            // Tallied before the window check, because the tallies are about the city and only the
            // picture is clipped -- a legend saying "877 built" over a grid holding 200 of them
            // would be worse than either number on its own.
            if (shell)
            {
                shells++;
            }
            else if (occupied)
            {
                built++;
            }
            else
            {
                vacant++;
            }

            if (column > east || row > north)
            {
                continue;
            }

            int cell = (row * (east + 1)) + column;

            // Built beats Shell beats Vacant beats Unzoned when scaling collapses several Lots onto
            // one character. Scaling can only ever collapse Lots together, never split them, so the
            // honest reading of a cell is "the most built thing here" -- a picture that reported the
            // last writer would make occupancy depend on table order. A shell outranks a vacant Lot
            // because a Building stands on it, and loses to a standing one because a character that
            // said "blighted" over a block with a working dwelling in it would overstate the decline.
            char glyph = occupied ? (shell ? Shell : Built) : Vacant;

            if (Rank(glyph) > Rank(grid[cell]))
            {
                grid[cell] = glyph;
            }
        }

        output.WriteLine(
            $"{built} built, {shells} abandoned, {vacant} vacant. '{Built}' holds a Building "
            + $"somebody lives in, '{Shell}' holds an abandoned shell nobody can move into, "
            + $"'{Vacant}' is a Lot with no Building, blank is no Lot at all."
            + (scale == 1 ? string.Empty : Legend(scale, world.Roads.Streets.BlockTiles / scale))
            + (clippedEast > east || clippedNorth > north
                ? Clipped(east + 1, north + 1, clippedEast + 1, clippedNorth + 1)
                : string.Empty));

        for (int row = north; row >= 0; row--)
        {
            output.WriteLine(new string(grid, row * (east + 1), east + 1).TrimEnd());
        }
    }

    /// <summary>
    /// How wide and tall the picture may get, in characters.
    /// </summary>
    /// <remarks>
    /// <b>A property of a terminal, not of the city</b>, which is why it is a shell constant and not
    /// Ruleset data. At eight characters a block it is sixteen blocks each way — enough of a city to
    /// read a sweep off, and the tallies above the picture are over the whole world regardless.
    /// </remarks>
    private const int Window = 128;

    /// <summary>The third sentence of the legend, when the picture is clipped.</summary>
    private static string Clipped(int shownEast, int shownNorth, int east, int north) => string.Create(
        CultureInfo.InvariantCulture,
        $" Showing the {shownEast}×{shownNorth} characters nearest the origin, of {east}×{north}; the counts above are over the whole city.");

    /// <summary>The second sentence of the legend, when the picture is scaled.</summary>
    private static string Legend(int scale, int blockCharacters) => string.Create(
        CultureInfo.InvariantCulture,
        $" One character is {scale}×{scale} Tiles, so a block is {blockCharacters} characters across and hollow, because Lots hang on Segments.");

    /// <summary>
    /// How many Tiles a character stands for.
    /// </summary>
    /// <remarks>
    /// <b>Eight characters to a block, and the number is derived from the lattice rather than
    /// chosen.</b> The five Lots on a block face sit at midpoints of equal shares
    /// (<see cref="Frontage.OffsetOf"/>) — 3, 9, 16, 22 and 28 on a 32-Tile face — and eighths are
    /// the coarsest scale on which no two of them collapse into one character. Coarser and the
    /// picture starts under-reporting a face; finer and the interior it exists to show stops fitting
    /// on a terminal. A world with no lattice keeps one character per Tile, because there are no
    /// block faces to scale to.
    /// </remarks>
    private static int Scale(World world)
    {
        int block = world.Roads.Streets.BlockTiles;

        return block >= 8 ? block / 8 : 1;
    }
}
