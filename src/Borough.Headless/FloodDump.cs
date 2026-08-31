namespace Borough.Headless;

using System.Globalization;
using Borough.Core;
using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

/// <summary>
/// The floods, printed: where each one started, how far it got, and what it took.
/// </summary>
/// <remarks>
/// <para>
/// <b>The first instrument that reads <c>World.Flood</c></b>, which has held the Hazard Region since
/// milestone 24 with nothing firing on it. <c>plans/0045</c> row 12.
/// </para>
/// <para>
/// <b>Four things per flood, and the fourth is the point.</b> Where the world seeded it, how deep
/// that ground was, how many Cells it reached at its peak, and how many Buildings it ruined and
/// swept. ⚠ <b>A flood that took nothing is printed exactly like one that took a district</b> —
/// <c>01 §5.3</c>: <em>"Uninteresting disasters still fire, and are still reported. 'Riverside
/// floodplain inundated — 0 Buildings affected' is the game telling a player that a zoning decision
/// made forty Days ago was correct. There is no other way to be told that."</em>
/// </para>
/// <para>
/// ⚠ <b>Sampled every Tick and not on a cadence.</b> A flood's reach is a level and would survive a
/// coarse sample, but <c>Ruined</c> and <c>Swept</c> are flows: they are per-Tick deltas on
/// <see cref="Simulation.LastDisasters"/> and read zero on every Tick but the one they happen on.
/// ***That is <c>StageDump</c>'s recorded mistake, avoided by not sampling at all.***
/// </para>
/// <para>
/// ⚠ <b>The peak reach is the instrument's high-water mark and not a column.</b> Nothing in the
/// world stores how large a flood ever got — the footprint rows are freed as the water leaves, which
/// is the sink <c>adr/0006</c> asks for — so a run is the only place the number exists.
/// </para>
/// </remarks>
internal static class FloodDump
{
    /// <summary>How many floods to print in full before summarising.</summary>
    private const int Named = 24;

    /// <summary>What one flood looked like from beginning to end.</summary>
    private sealed class Event
    {
        internal ulong Id;
        internal Handle<Disaster> Handle;
        internal ulong Began;
        internal int East;
        internal int North;
        internal int SeedDepth;
        internal int Peak;
        internal int Ruined;
        internal int Swept;
        internal ulong Ended;
    }

    internal static int Run(Options options, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(output);

        if (!Session.TryRules(options.RulesetPath, out Ruleset rules, out RulesetNames _))
        {
            return 2;
        }

        if (!rules.Disasters.Stated)
        {
            output.WriteLine(
                "This Ruleset declares no [disasters], so nothing is ever scheduled over the Hazard "
                + "Region and every row of this dump would be empty. A world with a floodplain and "
                + "no floods is a world -- coastal.toml is one -- and it is not a reading.");
            output.WriteLine();
            output.WriteLine(
                "  --flood --ruleset rulesets/flooded.toml --citizens 2000 --ticks 20480");

            return 2;
        }

        var key = WorldKey.FromSeed(options.Seed);
        World world = new(options.Citizens, rules, key);
        Simulation simulation = new(world, key) { VerifyDecideWritesNothing = false };
        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        int hazard = world.Flood.Rows.LiveCount;
        int lots = world.Lots.Rows.LiveCount;
        int atRisk = AtRisk(world);
        List<Event> events = [];
        long tickCells = 0;

        for (ulong tick = 0; tick < options.Ticks; tick++)
        {
            simulation.Step(default);

            tickCells += world.Inundations.Rows.LiveCount;

            Track(world, tick, events);

            // THE REACH IS A LEVEL, so a coarse sample carries it and a per-Tick walk of the
            // footprint would be the instrument costing more than the mechanism. ⚠ THE DAMAGE IS
            // NOT, and it is read off the saved columns above rather than sampled at all --
            // DisasterTable.Ruined and .Swept are running totals per flood, which is why they were
            // made columns instead of left to a readout. StageDump's recorded mistake, avoided by
            // sampling only the thing that survives sampling.
            //
            // ⚠ 65 AND NOT 64: the flood's own cadences are powers of two, and an instrument
            // sampling a paced mechanism has to pick an interval coprime with the pacing. That
            // sentence is already in plans/0045, written by --watch after four readings of an empty
            // city.
            if (tick % 65UL == 0UL)
            {
                Reach(world, events);
            }
        }

        Header(output, options, hazard, lots, atRisk, world);
        Table(output, events);
        Tail(output, events, tickCells, options.Ticks, world);

        return 0;
    }

    /// <summary>How many standing Buildings sit on Hazard Region ground at all.</summary>
    /// <remarks>
    /// ⚠ <b>A count of exposure and NOT a prediction.</b> A Building is in the Hazard Region when a
    /// flood could reach its Cell at the flood level; whether any flood ever does depends on where
    /// the world seeds one and on whether that Cell is connected to it below the surge. ***This is
    /// the number the overlay would show, and it is the price <c>01 §5.3</c> wants posted.***
    /// </remarks>
    private static int AtRisk(World world)
    {
        LotTable lots = world.Lots;
        int exposed = 0;

        for (int slot = 0; slot < lots.Rows.SlotCount; slot++)
        {
            if (!lots.Rows.IsLive(slot) || lots.BuildingOn(slot) < 0)
            {
                continue;
            }

            if (world.FloodInCells.DepthAt(
                    world.Flood,
                    CellGrid.ToCells(lots.East[slot]),
                    CellGrid.ToCells(lots.North[slot]))
                > 0)
            {
                exposed++;
            }
        }

        return exposed;
    }

    /// <summary>
    /// Opens an Event for each Disaster that has begun and closes each that has finished.
    /// </summary>
    /// <remarks>
    /// <b>Keyed on the row's MONOTONIC id and not on its slot</b>, which is the same rule the State
    /// Hash folds handles by. Slots are recycled, so a flood ending on the Tick the next one begins
    /// would reuse the row and this instrument would report one long flood — and the interval and
    /// the lifetime are two independent Ruleset durations, so an author can make that happen.
    /// </remarks>
    private static void Track(World world, ulong tick, List<Event> events)
    {
        DisasterTable floods = world.Disasters;

        for (int slot = 0; slot < floods.Rows.SlotCount; slot++)
        {
            if (!floods.Rows.IsLive(slot))
            {
                continue;
            }

            Event flood = Open(floods, slot, tick, events);

            flood.Ruined = floods.Ruined[slot];
            flood.Swept = floods.Swept[slot];
        }

        foreach (Event flood in events)
        {
            if (flood.Ended == 0 && !floods.Rows.IsValid(flood.Handle))
            {
                flood.Ended = tick;
            }
        }
    }

    /// <summary>Finds the Event for a Disaster row, or starts one.</summary>
    private static Event Open(DisasterTable floods, int slot, ulong tick, List<Event> events)
    {
        ulong id = floods.Rows.IdAt(slot);

        foreach (Event known in events)
        {
            if (known.Id == id)
            {
                return known;
            }
        }

        var opened = new Event
        {
            Id = id,
            Handle = floods.Rows.At(slot),
            Began = tick,
            East = floods.East[slot].Raw,
            North = floods.North[slot].Raw,
            SeedDepth = floods.SeedDepth[slot],
        };

        events.Add(opened);

        return opened;
    }

    /// <summary>Tallies each live flood's footprint and keeps the high-water mark.</summary>
    private static void Reach(World world, List<Event> events)
    {
        InundationTable wet = world.Inundations;
        DisasterTable floods = world.Disasters;

        foreach (Event flood in events)
        {
            if (flood.Ended != 0 || !floods.Rows.IsValid(flood.Handle))
            {
                continue;
            }

            int held = 0;

            for (int slot = 0; slot < wet.Rows.SlotCount; slot++)
            {
                if (wet.Rows.IsLive(slot) && wet.Cause[slot] == flood.Handle)
                {
                    held++;
                }
            }

            flood.Peak = Math.Max(flood.Peak, held);
        }
    }

    private static void Header(
        TextWriter output,
        Options options,
        int hazard,
        int lots,
        int atRisk,
        World world)
    {
        DisasterRuleset rules = world.Rules.Disasters;

        output.WriteLine($"flood — {options.RulesetPath}, {options.Citizens:N0} Citizens, "
            + $"{options.Ticks:N0} Ticks, seed {options.Seed}");
        output.WriteLine();
        output.WriteLine($"  a flood every {rules.FloodEveryDays} Days, rising over "
            + $"{rules.FloodRisesOverDays} and receding over {rules.FloodRecedesOverDays}");
        output.WriteLine($"  Hazard Region {hazard:N0} Cells of {CellGrid.WorldCellCount:N0} "
            + $"({Share(hazard, CellGrid.WorldCellCount)}% of the map)");
        output.WriteLine($"  Lots {lots:N0}, of which {atRisk:N0} stand on floodplain "
            + $"({Share(atRisk, lots)}% of the built city)");
        Probe(output, world);
        output.WriteLine();
        output.WriteLine("  ⚠ at-risk is EXPOSURE and not a forecast. A flood reaches the connected");
        output.WriteLine("    floodplain below its surge, so most of it is out of reach of most floods.");
        output.WriteLine();
    }

    /// <summary>
    /// Where the floodplain actually is — <b>the line that answers <em>why did nothing
    /// happen</em>.</b>
    /// </summary>
    /// <remarks>
    /// 🔴 <b>The first run of this dump reported four floods and zero Buildings touched, and the
    /// reason was not the mechanism.</b> The synthetic city is ~1.4 km across on a 65.5 km map
    /// (<c>adr/0089</c>: the map is sized by how many commutes fit across it), so it occupies about
    /// two ten-thousandths of the ground and the odds of it meeting a coast by accident are nil.
    /// ***A world where a flood cannot reach the city is not a demonstration of floods***, and this
    /// line is what says so at a glance instead of after an afternoon.
    /// </remarks>
    private static void Probe(TextWriter output, World world)
    {
        const int Block = 8;
        int across = CellGrid.WorldCells / Block;
        int[] band = new int[across * across];

        for (int slot = 0; slot < world.Flood.Rows.SlotCount; slot++)
        {
            if (world.Flood.Rows.IsLive(slot))
            {
                band[(IntegerMath.FloorDiv(world.Flood.North[slot].Raw, Block) * across)
                    + IntegerMath.FloorDiv(world.Flood.East[slot].Raw, Block)]++;
            }
        }

        int best = 0;

        for (int at = 1; at < band.Length; at++)
        {
            if (band[at] > band[best])
            {
                best = at;
            }
        }

        int east = (best % across) * Block;
        int north = IntegerMath.FloorDiv(best, across) * Block;

        output.WriteLine($"  densest floodplain: Cell ({east},{north}), tiles "
            + $"({east * CellGrid.TilesPerCell:N0},{north * CellGrid.TilesPerCell:N0}) — "
            + "a [[lattice]] origin near here is a city that can be flooded");
    }

    private static void Table(TextWriter output, List<Event> events)
    {
        output.WriteLine("  began      seed Cell     depth   peak Cells   ruined   swept   Ticks");
        output.WriteLine("  ---------- ------------- ------- ------------ -------- ------- ------");

        for (int at = 0; at < events.Count && at < Named; at++)
        {
            Event flood = events[at];
            string span = flood.Ended == 0 ? "  live" : (flood.Ended - flood.Began).ToString("N0", Culture);

            output.WriteLine(
                $"  {flood.Began,-10:N0} ({flood.East,3},{flood.North,3})     "
                + $"{flood.SeedDepth,6:N0}  {flood.Peak,11:N0}  {flood.Ruined,7:N0} "
                + $"{flood.Swept,7:N0} {span,6}");
        }

        if (events.Count > Named)
        {
            output.WriteLine($"  … and {events.Count - Named:N0} more");
        }

        output.WriteLine();
    }

    private static void Tail(
        TextWriter output, List<Event> events, long tickCells, ulong ticks, World world)
    {
        int ruined = 0;
        int swept = 0;
        int peak = 0;
        long reach = 0;
        int harmless = 0;

        foreach (Event flood in events)
        {
            ruined += flood.Ruined;
            swept += flood.Swept;
            peak = Math.Max(peak, flood.Peak);
            reach += flood.Peak;

            if (flood.Ruined + flood.Swept == 0)
            {
                harmless++;
            }
        }

        output.WriteLine($"  {events.Count:N0} floods, {ruined:N0} Buildings ruined, "
            + $"{swept:N0} swept");
        output.WriteLine($"  reach: largest {peak:N0} Cells, mean "
            + $"{(events.Count == 0 ? 0 : reach / events.Count):N0}");
        output.WriteLine($"  {harmless:N0} of {events.Count:N0} touched nothing at all "
            + $"({Share(harmless, events.Count)}%)");
        output.WriteLine($"  water standing: {(ticks == 0 ? 0 : tickCells / (long)ticks):N0} Cells "
            + "on the average Tick");
        output.WriteLine();
        output.WriteLine($"  Buildings standing at the end {world.Buildings.Rows.LiveCount:N0}, "
            + $"Cells still under water {world.Inundations.Rows.LiveCount:N0}");
        output.WriteLine();
        output.WriteLine("  🔴 THE NUMBERS THIS FILE PRODUCES RATIFY NOTHING. All three [disasters]");
        output.WriteLine("     keys are stopwatch settings — see the Ruleset's own header.");
    }

    private static string Share(long part, long whole) =>
        whole == 0 ? "0" : (part * 100 / whole).ToString("N0", Culture);

    private static CultureInfo Culture => CultureInfo.InvariantCulture;
}
