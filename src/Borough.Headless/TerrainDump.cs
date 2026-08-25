using System.Globalization;
using System.Text;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;

namespace Borough.Headless;

/// <summary>
/// Prints the ground a city stands on — terrain type, Fertility, Sealing and Woodland — before and
/// after a run, on a world that has actually been built.
/// </summary>
/// <remarks>
/// <para>
/// <b>It exists because <see cref="LayerDump"/> has no city in it.</b> That dump builds a bare
/// <see cref="World"/> and hand-emits two pollution sources to demonstrate a kernel, so it never goes
/// through <see cref="SyntheticCity"/> — which is the only caller of <c>LayWoodland</c> and the only
/// thing that lays roads and Buildings to seal ground. Three of the four <see cref="Layer"/> members
/// are structurally zero there, and <c>--layer sealing</c> printed <c>peak 0</c> and an empty map from
/// the day Sealing shipped. ***A field with no consumer is invisible whether the hole is in the city
/// or in the tooling.***
/// </para>
/// <para>
/// <b>Terrain type is printed as categories and never as a ramp</b>, and that is the one display
/// decision here worth arguing. The other three fields are magnitudes, so shading them low-to-high
/// says something true. A terrain type is a <em>name</em>: marsh is not more-than floodplain, and a
/// ramp over the five would invent an ordering that nothing in the design has. So each type gets a
/// letter and a legend, which is the only rendering that adds no claim.
/// </para>
/// <para>
/// <b>The ground is printed twice because two of the four fields move on a clock.</b> Terrain type is
/// written once from the <see cref="WorldKey"/> and never again; Sealing goes up on build and decays
/// on <c>LayerSchedule.Sealing</c>, Woodland regrows on its own cadence, and Fertility is composed
/// from both at the point of use. A single frame would show the ground and hide the loop.
/// </para>
/// <para>
/// <b>Every string here belongs to the shell</b> (<c>adr/0002</c>). <c>Borough.Core</c> hands over
/// Cell coordinates, a <see cref="TerrainKind"/> and integers; the letters, the ramp and the headings
/// are this file's.
/// </para>
/// </remarks>
internal static class TerrainDump
{
    /// <summary>
    /// One letter per terrain type, indexed by <see cref="TerrainKind"/>. <b>Not a ramp.</b>
    /// </summary>
    /// <remarks>
    /// Ordinary is a dot rather than an <c>O</c> because it is most of the map (<c>TerrainKind</c>),
    /// and a page of <c>O</c>s would hide the four types a reader is looking for.
    /// </remarks>
    private static readonly (TerrainKind Kind, char Mark, string Name)[] Marks =
    [
        (TerrainKind.Ordinary, '.', "ordinary"),
        (TerrainKind.Rock, 'R', "rock"),
        (TerrainKind.Floodplain, 'F', "floodplain"),
        (TerrainKind.Marsh, 'M', "marsh"),
        (TerrainKind.ThinSoil, 'T', "thin soil"),
    ];

    /// <summary>Runs the dump and writes it to <paramref name="output"/>.</summary>
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

        MapLayers layers = world.Layers;
        TerrainRuleset terrain = rules.Terrain;
        FertilityWeights weights = layers.Ruleset.Fertility;

        output.WriteLine("# Borough terrain dump");
        output.WriteLine(
            $"# Cell = {CellGrid.TilesPerCell}x{CellGrid.TilesPerCell} Tiles "
            + $"(~{CellGrid.MetresPerCell} m). Window {LayerDump.Window.Width.Raw}x"
            + $"{LayerDump.Window.Height.Raw} Cells from ({LayerDump.Window.East.Raw}, "
            + $"{LayerDump.Window.North.Raw}), of {CellGrid.WorldCellCount} on the map.");
        output.WriteLine(
            $"# {world.Lots.Rows.LiveCount} Lots, {world.Buildings.Rows.LiveCount} Buildings, "
            + $"{world.Roads.Segments.Rows.LiveCount} Segments — the ground under a built city.");

        WriteTerrain(output, layers, terrain, options.Csv);
        WriteVariety(output, layers);
        WriteSealingByType(output, layers, terrain, "at Tick 0");

        output.WriteLine();
        output.WriteLine("## Before — the populator's city, nothing decayed and nothing regrown");
        WriteFields(output, layers, terrain, weights, options.Csv);

        for (ulong tick = 0; tick < options.Ticks; tick++)
        {
            simulation.Step(default);
        }

        output.WriteLine();
        output.WriteLine(
            $"## After — {options.Ticks} Ticks ({options.Ticks / Ticks.PerDay} Days) of decay and "
            + "regrowth");
        WriteFields(output, layers, terrain, weights, options.Csv);
        WriteSealingByType(
            output,
            layers,
            terrain,
            $"after {options.Ticks / Ticks.PerDay} Days");

        return 0;
    }

    /// <summary>Writes the terrain type map and its legend. Categories, so no ramp and no peak.</summary>
    private static void WriteTerrain(
        TextWriter output, MapLayers layers, TerrainRuleset terrain, bool csv)
    {
        output.WriteLine();
        output.WriteLine("## Terrain type — written once from the WorldKey, and it never moves");

        Span<int> counts = stackalloc int[Marks.Length];

        if (csv)
        {
            output.WriteLine("east,north,kind");
        }

        StringBuilder line = new(LayerDump.Window.Width.Raw);

        for (int row = 0; row < LayerDump.Window.Height.Raw; row++)
        {
            line.Clear();

            for (int column = 0; column < LayerDump.Window.Width.Raw; column++)
            {
                Cells east = new(LayerDump.Window.East.Raw + column);
                Cells north = new(LayerDump.Window.North.Raw + row);
                TerrainKind kind = layers.Terrain.At(east, north);
                int index = Index(kind);

                counts[index]++;

                if (csv)
                {
                    output.WriteLine(string.Create(
                        CultureInfo.InvariantCulture,
                        $"{east.Raw},{north.Raw},{Marks[index].Name}"));
                }
                else
                {
                    line.Append(Marks[index].Mark);
                }
            }

            if (!csv)
            {
                output.WriteLine(line.ToString());
            }
        }

        if (csv)
        {
            return;
        }

        output.WriteLine();

        for (int i = 0; i < Marks.Length; i++)
        {
            // Base Fertility beside the count, because the letter is the whole reason the reader
            // needs a legend and the price of that ground is what the letter is standing in for.
            string worth = terrain.Stated
                ? $"base fertility {terrain.BaseFertility(Marks[i].Kind)}, decay tau "
                    + $"{terrain.SealingDecayTau(Marks[i].Kind)}"
                : "unpriced — this Ruleset states no [[terrain]]";

            output.WriteLine(
                $"  {Marks[i].Mark}  {Marks[i].Name,-11} {counts[i],6} Cells — {worth}");
        }
    }

    /// <summary>How many Cells on a side one square of the variety survey covers.</summary>
    /// <remarks>
    /// <b>Chosen to be about a city.</b> A city of 20,000 Citizens touches roughly 528 Cells, so a
    /// 32×32 block is the same order — which is what makes a square's reading mean <em>how many kinds
    /// of ground a city sited here would stand on</em> rather than an abstract measure of mixing.
    /// </remarks>
    private const int SurveyBlock = 32;

    /// <summary>
    /// Writes how many distinct terrain types each block of the map holds. <b>A siting tool.</b>
    /// </summary>
    /// <remarks>
    /// <b>It exists because a city stands where its lattice puts it and nothing steers it toward
    /// interesting ground.</b> Terrain is generated from the <c>WorldKey</c> and placement is blind to
    /// it, so whether a world exercises a rate keyed by terrain type is decided entirely by where the
    /// origin lands. A Ruleset stating no <c>[[lattice]]</c> gets one at (0, 0) — the corner — and on
    /// this map that corner is uniformly ordinary ground. ⚠ <b>This is the same failure
    /// <c>rulesets/coastal.toml</c> hit</b>, where a corner city's runoff drained off the map and
    /// every measurement read zero until the origin moved.
    /// </remarks>
    private static void WriteVariety(TextWriter output, MapLayers layers)
    {
        int blocks = CellGrid.WorldCells / SurveyBlock;

        output.WriteLine();
        output.WriteLine(
            $"## Where the ground varies — {blocks}x{blocks} blocks of {SurveyBlock}x{SurveyBlock} "
            + "Cells, each digit the count of distinct terrain types in that block");

        int best = 0;
        int bestEast = 0;
        int bestNorth = 0;
        StringBuilder line = new(blocks);

        for (int row = 0; row < blocks; row++)
        {
            line.Clear();

            for (int column = 0; column < blocks; column++)
            {
                int present = 0;

                for (int y = 0; y < SurveyBlock; y++)
                {
                    for (int x = 0; x < SurveyBlock; x++)
                    {
                        Cells east = new((column * SurveyBlock) + x);
                        Cells north = new((row * SurveyBlock) + y);

                        present |= 1 << Index(layers.Terrain.At(east, north));
                    }
                }

                int kinds = System.Numerics.BitOperations.PopCount((uint)present);

                line.Append((char)('0' + kinds));

                if (kinds > best)
                {
                    best = kinds;
                    bestEast = column;
                    bestNorth = row;
                }
            }

            output.WriteLine(line.ToString());
        }

        // The centre of the best block, in Tiles, because that is the unit [[lattice]] authors its
        // origin in and a reader converting Cells to Tiles by hand is a reader making an error.
        int centreEast = ((bestEast * SurveyBlock) + (SurveyBlock / 2)) * CellGrid.TilesPerCell;
        int centreNorth = ((bestNorth * SurveyBlock) + (SurveyBlock / 2)) * CellGrid.TilesPerCell;

        output.WriteLine();
        output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"  most varied block: ({bestEast}, {bestNorth}) with {best} types. Its centre is "
            + $"origin_east_tiles = {centreEast}, origin_north_tiles = {centreNorth}."));
    }

    /// <summary>
    /// Writes Sealing broken down by terrain type, over the <b>whole map</b> and not the window.
    /// </summary>
    /// <remarks>
    /// <b>This is the shape of decision 5's ratifier and not the ratifier itself.</b> That quantity is
    /// <em>Days from a Cell's last demolition to its Sealing reaching zero, per terrain type</em>, and
    /// it needs per-Cell event tracking over a long run. What this answers is the question that comes
    /// first and is far cheaper: ⚠ <b>does the city stand on more than one kind of ground at all?</b>
    /// A rate keyed by terrain type cannot be ratified by a world that only ever seals one type, and
    /// the decision refutes in exactly that direction — <em>every type recovering in the same time
    /// means the key is not keyed on anything</em>.
    /// </remarks>
    private static void WriteSealingByType(
        TextWriter output, MapLayers layers, TerrainRuleset terrain, string when)
    {
        Span<int> cells = stackalloc int[Marks.Length];
        Span<int> sealedCells = stackalloc int[Marks.Length];
        Span<int> recovered = stackalloc int[Marks.Length];
        Span<long> totals = stackalloc long[Marks.Length];
        Span<int> peaks = stackalloc int[Marks.Length];

        for (int slot = 0; slot < layers.Cells.Rows.SlotCount; slot++)
        {
            if (!layers.Cells.Rows.IsLive(slot))
            {
                continue;
            }

            int sealing = layers.Cells.Sealing[slot];
            int index = Index(layers.Terrain.At(layers.Cells.East[slot], layers.Cells.North[slot]));

            // A Layer Cell row exists because something touched this Cell, so a live row reading
            // zero is ground that WAS sealed and has come back -- which is the only direct evidence
            // that decision 5's quantity is reachable at all. A Cell that was never built on has no
            // row and is not counted here.
            if (sealing <= 0)
            {
                recovered[index]++;
                continue;
            }

            sealedCells[index]++;
            totals[index] += sealing;

            if (sealing > peaks[index])
            {
                peaks[index] = sealing;
            }
        }

        // The whole map, because the window above is 1,344 Cells of 262,144 and a distribution read
        // off 0.5% of the ground is not the map's distribution.
        for (int cell = 0; cell < CellGrid.WorldCellCount; cell++)
        {
            // The inverse of CellGrid.Index, which is north * WorldCells + east.
            Cells east = new(cell % CellGrid.WorldCells);
            Cells north = new(cell / CellGrid.WorldCells);

            cells[Index(layers.Terrain.At(east, north))]++;
        }

        output.WriteLine();
        output.WriteLine(
            $"## Sealing by terrain type, {when} — the whole map, "
            + $"{CellGrid.WorldCellCount} Cells");
        output.WriteLine(
            "  type          map Cells   sealed  recovered   total sealing   peak   tau");

        for (int i = 0; i < Marks.Length; i++)
        {
            string tau = terrain.Stated
                ? terrain.SealingDecayTau(Marks[i].Kind).ToString(CultureInfo.InvariantCulture)
                : "--";

            output.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"  {Marks[i].Name,-11} {cells[i],11} {sealedCells[i],8} {recovered[i],10} "
                + $"{totals[i],15} {peaks[i],6} {tau,5}"));
        }
    }

    /// <summary>Writes Fertility, Sealing and Woodland, each ramped against its own peak.</summary>
    private static void WriteFields(
        TextWriter output,
        MapLayers layers,
        TerrainRuleset terrain,
        FertilityWeights weights,
        bool csv)
    {
        WriteField(output, "sealing", layers, terrain, weights, Layer.Sealing, csv);
        WriteField(output, "woodland", layers, terrain, weights, Layer.Woodland, csv);

        if (!terrain.Stated)
        {
            // Not a crash and not a zero. TerrainRuleset.BaseFertility throws on an unstated file
            // BY DESIGN (adr/0158) -- every world has ground, and what that ground is WORTH is
            // Ruleset data the file has declined to state. Printing zeros would be a lie about a
            // decision; saying so names the key that would fix it.
            output.WriteLine();
            output.WriteLine(
                "### fertility — UNAVAILABLE. This Ruleset states no [[terrain]], so its ground has "
                + "no Base Fertility to compose from (adr/0155, adr/0158). The type map above is "
                + "still real: a world always has terrain, and a file may decline to price it. "
                + "rulesets/varied.toml is the shipped file that states it.");
            return;
        }

        WriteField(output, "fertility", layers, terrain, weights, null, csv);
    }

    /// <summary>
    /// Writes one magnitude field. <paramref name="layer"/> is <c>null</c> for Fertility, which is
    /// composed at the point of use and is therefore not a <see cref="Layer"/> at all.
    /// </summary>
    private static void WriteField(
        TextWriter output,
        string name,
        MapLayers layers,
        TerrainRuleset terrain,
        FertilityWeights weights,
        Layer? layer,
        bool csv)
    {
        int width = LayerDump.Window.Width.Raw;
        int height = LayerDump.Window.Height.Raw;
        Span<int> values = new int[width * height];

        int peak = 0;
        int low = int.MaxValue;
        long total = 0;

        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
            {
                Cells east = new(LayerDump.Window.East.Raw + column);
                Cells north = new(LayerDump.Window.North.Raw + row);

                int value = layer is Layer known
                    ? layers.Value(known, east, north)
                    : layers.Fertility(terrain, weights, east, north);

                values[(row * width) + column] = value;
                total += value;

                if (value > peak)
                {
                    peak = value;
                }

                if (value < low)
                {
                    low = value;
                }
            }
        }

        output.WriteLine();
        output.WriteLine($"### {name}");

        if (csv)
        {
            output.WriteLine("east,north,value");

            for (int row = 0; row < height; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    output.WriteLine(string.Create(
                        CultureInfo.InvariantCulture,
                        $"{LayerDump.Window.East.Raw + column},"
                        + $"{LayerDump.Window.North.Raw + row},"
                        + $"{values[(row * width) + column]}"));
                }
            }

            return;
        }

        // The total is the figure the long run's two ratifiers are read from -- Woodland against
        // Sealing (adr/0022's load-bearing constant) -- so it is printed as a number and never left
        // for the reader to infer from a shading.
        output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"low {low}, peak {peak}, total {total} over {width * height} Cells, "
            + $"ramp \"{LayerDump.Ramp}\" low to high"));

        StringBuilder line = new(width);

        for (int row = 0; row < height; row++)
        {
            line.Clear();

            for (int column = 0; column < width; column++)
            {
                line.Append(LayerDump.Step(values[(row * width) + column], peak));
            }

            output.WriteLine(line.ToString());
        }
    }

    /// <summary>The index of a terrain type in <see cref="Marks"/>.</summary>
    private static int Index(TerrainKind kind)
    {
        for (int i = 0; i < Marks.Length; i++)
        {
            if (Marks[i].Kind == kind)
            {
                return i;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(kind));
    }
}
