namespace Borough.Headless;

using System.Globalization;
using System.Text;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;

/// <summary>
/// Frames of the city as ASCII — the standing Buildings under the Travellers moving over them.
/// </summary>
/// <remarks>
/// ⚠ <b>The frame is scaled to the Lots that exist</b>: 1,000 Citizens occupy under a kilometre of
/// a map 65 km across. ⚠ <b>A frame every 32 Ticks reports an empty city</b> — the job cadence,
/// aliased against itself. Ask for enough frames that the interval is coprime with it.
/// </remarks>
internal static class WatchDump
{
    /// <summary>The widest frame, in characters. Sized for a terminal.</summary>
    private const int Columns = 96;

    /// <summary>The tallest frame. A character is about twice as tall as it is wide.</summary>
    private const int RowsHigh = 40;

    /// <summary>Standing Buildings per character, densest last.</summary>
    private const string Ramp = ".:-=+*#%@";

    /// <summary>A character the Road Graph passes through and nothing stands on.</summary>
    private const char Road = ',';

    /// <summary>Travellers on one character before it prints the crowd mark.</summary>
    private const int Crowd = 9;

    internal static int Run(Options options, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(output);

        if (!Session.TryRules(options.RulesetPath, out Ruleset rules))
        {
            return 2;
        }

        var key = WorldKey.FromSeed(options.Seed);
        World world = new(options.Citizens, rules, key);
        Simulation simulation = new(world, key) { VerifyDecideWritesNothing = false };
        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        View view = View.Of(world);
        var agents = new VisibleAgent[Math.Max(1024, options.Citizens)];
        var buildings = new int[view.Width * view.Height];
        var crowd = new int[view.Width * view.Height];
        var roads = new bool[view.Width * view.Height];

        Pave(world, view, roads);

        Header(options, view, output);

        ulong interval = Math.Max(1UL, options.Ticks / (ulong)options.Frames);

        for (ulong tick = 0; tick < options.Ticks; tick++)
        {
            simulation.Step(default);

            if ((simulation.Tick.Raw - 1UL) % interval != 0UL)
            {
                continue;
            }

            int found = VisibleAgents.In(world, view.Cells, Ratio.Zero, agents);

            Paint(world, view, buildings, crowd, agents.AsSpan(0, found));
            Frame(simulation, world, view, buildings, crowd, roads, found, output);
        }

        return 0;
    }

    /// <summary>Marks every character the Road Graph runs through. Done once: nothing is built here.</summary>
    private static void Pave(World world, View view, bool[] roads)
    {
        RoadSegmentTable segments = world.Roads.Segments;
        RoadNodeTable nodes = world.Roads.Nodes;

        for (int slot = 0; slot < segments.Rows.SlotCount; slot++)
        {
            if (!segments.Rows.IsLive(slot)
                || !nodes.Rows.TryResolve(segments.NodeA[slot], out int a)
                || !nodes.Rows.TryResolve(segments.NodeB[slot], out int b))
            {
                continue;
            }

            int steps = Math.Max(
                Math.Abs((nodes.East[b] - nodes.East[a]).Raw),
                Math.Abs((nodes.North[b] - nodes.North[a]).Raw));

            for (int step = 0; step <= steps; step++)
            {
                int index = view.Index(
                    new Tiles(nodes.East[a].Raw
                        + (((nodes.East[b] - nodes.East[a]).Raw * step) / Math.Max(1, steps))),
                    new Tiles(nodes.North[a].Raw
                        + (((nodes.North[b] - nodes.North[a]).Raw * step) / Math.Max(1, steps))));

                if (index >= 0)
                {
                    roads[index] = true;
                }
            }
        }
    }

    /// <summary>Fills the two grids a frame is composed from.</summary>
    private static void Paint(
        World world, View view, int[] buildings, int[] crowd, ReadOnlySpan<VisibleAgent> agents)
    {
        Array.Clear(buildings);
        Array.Clear(crowd);

        BuildingTable table = world.Buildings;
        LotTable lots = world.Lots;

        for (int slot = 0; slot < table.Rows.SlotCount; slot++)
        {
            if (!table.Rows.IsLive(slot) || !lots.Rows.TryResolve(table.Lot[slot], out int lot))
            {
                continue;
            }

            int index = view.Index(lots.East[lot], lots.North[lot]);

            if (index >= 0)
            {
                buildings[index]++;
            }
        }

        foreach (VisibleAgent agent in agents)
        {
            int index = view.Index(agent.East.ToTilesFloor(), agent.North.ToTilesFloor());

            if (index >= 0)
            {
                crowd[index]++;
            }
        }
    }

    /// <summary>Writes one frame, north at the top.</summary>
    private static void Frame(
        Simulation simulation,
        World world,
        View view,
        int[] buildings,
        int[] crowd,
        bool[] roads,
        int found,
        TextWriter output)
    {
        ulong tick = simulation.Tick.Raw - 1UL;
        ulong ofDay = tick % (ulong)Ticks.PerDay;
        StringBuilder line = new(view.Width);

        output.WriteLine();
        output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"-- Tick {tick}  Day {tick / (ulong)Ticks.PerDay} "
            + $"{Ticks.MinuteOfDay(tick) / 60:00}:{Ticks.MinuteOfDay(tick) % 60:00}  "
            + $"travelling {world.Travellers.Rows.LiveCount}  placed {found}  "
            + $"buildings {world.Buildings.Rows.LiveCount}"));

        for (int y = view.Height - 1; y >= 0; y--)
        {
            line.Clear();

            for (int x = 0; x < view.Width; x++)
            {
                int index = (y * view.Width) + x;

                line.Append(crowd[index] > 0
                    ? crowd[index] >= Crowd ? '@' : (char)('0' + crowd[index])
                    : buildings[index] > 0
                        ? Ramp[Math.Min(buildings[index] - 1, Ramp.Length - 1)]
                        : roads[index] ? Road : ' ');
            }

            output.WriteLine(line.ToString().TrimEnd());
        }
    }

    /// <summary>What the frames are of, and what the marks mean.</summary>
    private static void Header(Options options, View view, TextWriter output)
    {
        output.WriteLine("# Borough watch — the city as a place");
        output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"# {options.RulesetPath}, {options.Citizens} Citizens, {options.Ticks} Ticks, "
            + $"{options.Frames} frames"));
        output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"# {view.Width}x{view.Height} characters, 1 character = "
            + $"{view.ScaleEast.Raw * Tiles.Metres}x{view.ScaleNorth.Raw * Tiles.Metres} m"));
        output.WriteLine(
            $"# '{Road}' = Road, '{Ramp}' = Buildings, digits = Travellers, '@' = {Crowd}+");
    }

    /// <summary>The frame's geometry: where the city is, and how many Tiles a character covers.</summary>
    private readonly record struct View(
        Tiles East, Tiles North, Tiles ScaleEast, Tiles ScaleNorth, int Width, int Height)
    {
        /// <summary>The box <see cref="VisibleAgents.In"/> is asked for.</summary>
        public CellRect Cells =>
            new(CellGrid.ToCells(East),
                CellGrid.ToCells(North),
                new Cells(CellGrid.ToCells(ScaleEast * Width).Raw + 2),
                new Cells(CellGrid.ToCells(ScaleNorth * Height).Raw + 2));

        /// <summary>A place's character in the frame, or -1 where it falls outside.</summary>
        public int Index(Tiles east, Tiles north)
        {
            int x = (east - East).Raw / ScaleEast.Raw;
            int y = (north - North).Raw / ScaleNorth.Raw;

            return (uint)x < (uint)Width && (uint)y < (uint)Height ? (y * Width) + x : -1;
        }

        /// <summary>A frame around the Lots the city actually laid, at a legible scale.</summary>
        internal static View Of(World world)
        {
            LotTable lots = world.Lots;
            int east = int.MaxValue;
            int north = int.MaxValue;
            int eastEnd = int.MinValue;
            int northEnd = int.MinValue;

            for (int slot = 0; slot < lots.Rows.SlotCount; slot++)
            {
                if (!lots.Rows.IsLive(slot))
                {
                    continue;
                }

                east = Math.Min(east, lots.East[slot].Raw);
                north = Math.Min(north, lots.North[slot].Raw);
                eastEnd = Math.Max(eastEnd, lots.East[slot].Raw);
                northEnd = Math.Max(northEnd, lots.North[slot].Raw);
            }

            if (east > eastEnd)
            {
                return new View(Tiles.Zero, Tiles.Zero, new Tiles(1), new Tiles(2), Columns, RowsHigh);
            }

            int wide = eastEnd - east + 1;
            int tall = northEnd - north + 1;

            // A character is twice as tall as it is wide, so north is scaled twice as hard.
            int scale = Math.Max(1, Math.Max(
                (wide + Columns - 1) / Columns, (tall + (2 * RowsHigh) - 1) / (2 * RowsHigh)));

            return new View(
                new Tiles(east),
                new Tiles(north),
                new Tiles(scale),
                new Tiles(2 * scale),
                Math.Min(Columns, ((wide + scale - 1) / scale) + 1),
                Math.Min(RowsHigh, ((tall + (2 * scale) - 1) / (2 * scale)) + 1));
        }
    }
}
