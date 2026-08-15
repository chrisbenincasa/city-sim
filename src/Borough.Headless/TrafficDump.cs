using System.Globalization;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Headless;

/// <summary>
/// Prints where the traffic is, and what the volume-delay function does to it — <b>the same city
/// stepped twice, once with <c>[traffic]</c> and once without</b>.
/// </summary>
/// <remarks>
/// <para>
/// <b>The control is the function, not the clock, and that is the choice this picture turns on.</b>
/// <c>--commute</c> and <c>--zones</c> take Tick 0 as their <em>before</em>, because a city at Tick 0
/// has nobody employed and no Building raised. Volume at Tick 0 is <b>zero on every Segment of every
/// world</b>, so a Tick 0 panel here would be blank on every input — it would show that the run did
/// something and never whether it did it right. Two runs differing in exactly one Ruleset table answer
/// the question the milestone actually has: <b>does the volume-delay function do anything, and is it
/// doing it in the right direction?</b>
/// </para>
/// <para>
/// <b>Printing free-flow beside loaded is <c>--commute</c>'s lesson applied on purpose.</b> That mode
/// exposed a lossy minute formatter only because it printed a duration beside a number the reader
/// already knew. A function that is inert and a function that is wired backwards both move the State
/// Hash and both produce a plausible Census; side by side they are one glance apart.
/// </para>
/// <para>
/// ⚠ <b>It refuses a Ruleset that states no <c>[traffic]</c> and one that states no
/// <c>[households]</c>, and no shipped Ruleset states either.</b> That is <c>--zones</c>' polarity — a
/// grid of quiet roads would read as a broken mechanism rather than as a file that declares no
/// congestion and no cars — and here it carries a second job: the refusal is the only place in the
/// runner that tells an operator the shipped files cannot reach this mechanism at all. 5c task 8 is
/// where both tables get stated.
/// </para>
/// <para>
/// <b>Every string here belongs to the shell</b> (<c>adr/0002</c>). <c>Borough.Core</c> hands over
/// coordinates, counts and quantities; the glyphs and the sentences are this file's.
/// </para>
/// </remarks>
internal static class TrafficDump
{
    /// <summary>
    /// Four bands of volume over capacity, because those are the four a reader has to tell apart.
    /// </summary>
    /// <remarks>
    /// <b>The bands are absolute and are deliberately not quantiles of this run.</b> A ladder that
    /// re-normalised to whatever the busiest Segment happened to reach would draw an identically dark
    /// picture for an empty city and a gridlocked one — ***a ruler must not move with the thing it
    /// measures***, which is 5b-bis task 6's rule for the Trip cost histogram and holds here for the
    /// same reason. <c>@</c> is over capacity, which is a real threshold rather than a chosen one.
    /// </remarks>
    private const char Quiet = '.';
    private const char Busy = ':';
    private const char Heavy = '#';
    private const char Over = '@';
    private const char NoRoad = ' ';

    /// <summary>The band edges in Q16.16: a quarter of capacity, three quarters, and capacity.</summary>
    private const int Quarter = 16_384;
    private const int ThreeQuarters = 49_152;
    private const int Saturated = 65_536;

    /// <summary>How many Segments the busiest-Segments table names.</summary>
    /// <remarks>
    /// <b>A cap stated in the output rather than applied silently.</b> A listing that shows the worst
    /// ten of ten thousand and does not say so reads as a complete one, which is slice 10 task 11's
    /// baseline finding arriving in an instrument.
    /// </remarks>
    private const int Busiest = 10;

    /// <summary>How wide and tall each panel may get, in blocks. A terminal's property.</summary>
    private const int Window = 48;

    internal static int Run(Options options, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(output);

        if (options.RulesetPath is null || !Session.TryRules(options.RulesetPath, out Ruleset rules))
        {
            return 2;
        }

        if (!rules.Traffic.Runs)
        {
            output.WriteLine(
                "This Ruleset declares no [traffic] table, so its roads never slow down and both "
                + "halves of this picture would be the same picture. Congestion is content, not "
                + "engine: a grid of quiet roads would read as a broken volume-delay function rather "
                + "than as a file that declares none.");
            output.WriteLine(
                "rulesets/congested.toml is the one shipped file that states both tables, and it was "
                + "written for this mode: minimal.toml structurally cannot congest itself, which is "
                + "measured rather than assumed (adr/0099, plans/0002 SSC). Try");
            output.WriteLine();
            output.WriteLine("  --traffic --ruleset rulesets/congested.toml --citizens 16000");
            output.WriteLine();
            output.WriteLine("or add both tables to this file:");
            output.WriteLine();
            output.WriteLine("  [households]");
            output.WriteLine("  car_ownership_percent = 100");
            output.WriteLine();
            output.WriteLine("  [traffic]");
            output.WriteLine("  alpha_percent = 15");
            output.WriteLine("  beta          = 4");
            output.WriteLine("  clamp_percent = 400");

            return 3;
        }

        if (!rules.Households.Runs)
        {
            output.WriteLine(
                "This Ruleset states [traffic] and no [households], so no Household keeps a car, so "
                + "nobody drives and no Segment ever carries a Vehicle. Volume is vehicular by "
                + "decision (adr/0041; 03 §3.7 keeps pedestrians out of Stress entirely), so this "
                + "would be an empty picture of a working mechanism.");

            return 3;
        }

        if (rules.Roads.BlockTiles <= 0)
        {
            output.WriteLine(
                "This Ruleset declares no [roads] table, so there is no lattice to bin Segments into "
                + "and no network to drive on.");

            return 3;
        }

        if (!TryFreeFlow(options.RulesetPath, out Ruleset control))
        {
            output.WriteLine(
                "The free-flow control could not be built: stripping [traffic] from this file left a "
                + "Ruleset that still states one, or one the loader refuses. The control is checked "
                + "rather than assumed, because a strip that silently fails produces two identical "
                + "runs and a delay of x1.0000 that looks like a measurement.");

            return 3;
        }

        int block = rules.Roads.BlockTiles;

        output.WriteLine("# Borough traffic dump");
        output.WriteLine(
            $"# {options.Citizens} Citizens, {options.Ticks} Ticks, blocks {block} Tiles, "
            + $"{rules.Households.CarOwnershipPercent}% of Households keep a car.");
        output.WriteLine(
            $"# Volume-delay: free-flow x (1 + {Fraction(rules.Traffic.Alpha.Raw)} x (v/c)^"
            + $"{rules.Traffic.Beta}), clamped at v/c {Fraction(rules.Traffic.Clamp.Raw)} "
            + "(adr/0099).");
        output.WriteLine(
            "# v/c divides Vehicles PRESENT by Vehicles a Segment HOLDS at capacity, which is its "
            + "capacity per Tick times its free-flow crossing time -- Little's Law.");

        Reading free = Step(options, control);
        Reading busy = Step(options, rules);

        output.WriteLine();
        output.WriteLine("## Where the traffic is — the peak v/c each block reached over the run");
        Grid(output, free, busy, block, options.Csv);

        if (options.Csv)
        {
            return 0;
        }

        output.WriteLine();
        output.WriteLine("## The busiest Segments, free-flow beside loaded");
        Table(output, busy, block);

        output.WriteLine();
        output.WriteLine("## What the function did to the city");
        Summary(output, free, busy, options.Ticks);

        return 0;
    }

    // ---- the control ----------------------------------------------------------------------------

    /// <summary>
    /// The same Ruleset with its <c>[traffic]</c> table removed, or <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Text surgery, and then the result is <em>checked</em>.</b> A <c>Ruleset</c> is a sealed class
    /// rather than a record, so there is no <c>with</c>; and building one field by field would mean
    /// enumerating every table, which drifts the moment a table is added. Removing the section from the
    /// file's own text and re-parsing goes back through the loader, and therefore through every refusal
    /// the loader states.
    /// </para>
    /// <para>
    /// ⚠ <b>The check is the point, not the surgery.</b> A text edit that fails to match does nothing
    /// and says nothing — which, twice on 2026-08-14, produced a clean-looking table of identical rows
    /// that was very nearly written down as a measurement. Here the failure mode is worse than a wrong
    /// number: two identical runs report <b>×1.0000</b>, which is exactly what an inert volume-delay
    /// function reports on a generated city. ***A control that silently equals its treatment is
    /// indistinguishable from a null result.***
    /// </para>
    /// </remarks>
    private static bool TryFreeFlow(string path, out Ruleset control)
    {
        control = Ruleset.Empty;

        var kept = new List<string>();
        bool inTraffic = false;

        foreach (string line in File.ReadAllLines(path))
        {
            string trimmed = line.TrimStart();

            if (trimmed.StartsWith('['))
            {
                inTraffic = trimmed.StartsWith("[traffic]", StringComparison.Ordinal);
            }

            if (!inTraffic)
            {
                kept.Add(line);
            }
        }

        RulesetLoadResult result = RulesetLoader.Parse(string.Join('\n', kept), path);

        if (result.Ruleset is null || result.Ruleset.Traffic.Runs)
        {
            return false;
        }

        control = result.Ruleset;

        return true;
    }

    // ---- the run --------------------------------------------------------------------------------

    /// <summary>What one stepped world did to its roads.</summary>
    /// <remarks>
    /// <b>Peak and mean are both carried, and the mean is the one that decides what the picture
    /// means.</b> A peak of ten Vehicles on one Segment for one Tick is a coincidence of arrival times;
    /// a mean of 0.03 says the road is empty 97% of the time. ***A peak that a mean does not follow is
    /// a coincidence, and a volume-delay function prices means*** — so a report carrying only the peak
    /// invites a reader to see a jam that is not there.
    /// </remarks>
    private sealed record Reading(
        int[] Peak,
        long Occupied,
        long SegmentTicks,
        long VehicleTicks,
        World World);

    private static Reading Step(Options options, Ruleset rules)
    {
        var key = WorldKey.FromSeed(options.Seed);
        World world = new(options.Citizens, rules, key);
        Simulation simulation = new(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        RoadSegmentTable segments = world.Roads.Segments;

        int[] peak = new int[segments.Rows.SlotCount];
        long occupied = 0;
        long segmentTicks = 0;
        long vehicleTicks = 0;

        for (ulong tick = 0; tick < options.Ticks; tick++)
        {
            simulation.Step(default);

            for (int slot = 0; slot < peak.Length; slot++)
            {
                if (!segments.Rows.IsLive(slot))
                {
                    continue;
                }

                // The busier DIRECTION, never the sum. Capacity is per direction, so adding the two
                // would put a two-way total over a one-way ceiling and read a balanced Street as twice
                // as loaded as it is.
                int forward = segments.VolumeForward[slot];
                int backward = segments.VolumeBackward[slot];
                int busier = forward > backward ? forward : backward;

                if (busier > peak[slot])
                {
                    peak[slot] = busier;
                }

                vehicleTicks += forward + backward;
                segmentTicks++;

                if (forward + backward > 0)
                {
                    occupied++;
                }
            }
        }

        return new Reading(peak, occupied, segmentTicks, vehicleTicks, world);
    }

    // ---- the picture ----------------------------------------------------------------------------

    /// <summary>Peak load per block, for both runs, side by side.</summary>
    private static void Grid(TextWriter output, Reading free, Reading busy, int block, bool csv)
    {
        Extent(busy, block, out int width, out int height);

        if (width == 0 || height == 0)
        {
            output.WriteLine();
            output.WriteLine("(no Segment stands anywhere, so there is nothing to draw.)");

            return;
        }

        char[] left = Bands(free, block, width, height);
        char[] right = Bands(busy, block, width, height);

        if (csv)
        {
            output.WriteLine("block_east,block_north,free_flow,loaded");

            for (int cell = 0; cell < left.Length; cell++)
            {
                if (left[cell] == NoRoad && right[cell] == NoRoad)
                {
                    continue;
                }

                output.WriteLine(string.Create(
                    CultureInfo.InvariantCulture,
                    $"{cell % width},{cell / width},{left[cell]},{right[cell]}"));
            }

            return;
        }

        int shownWidth = width > Window ? Window : width;
        int shownHeight = height > Window ? Window : height;

        output.WriteLine();
        output.WriteLine(
            $"   {Quiet}  under 25%    {Busy}  25-75%    {Heavy}  75-100%    {Over}  over capacity"
            + "    (blank: no road)");

        if (shownWidth < width || shownHeight < height)
        {
            output.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"   Showing the {shownWidth}×{shownHeight} blocks nearest the origin, of {width}×{height}; the totals below are over the whole city."));
        }

        output.WriteLine();
        output.WriteLine($"   free-flow{new string(' ', shownWidth > 9 ? shownWidth - 6 : 3)}with [traffic]");

        // North up, as --zones and --commute draw it.
        for (int row = shownHeight - 1; row >= 0; row--)
        {
            string a = new string(left, row * width, shownWidth);
            string b = new string(right, row * width, shownWidth).TrimEnd();

            output.WriteLine($"   {a}   {b}".TrimEnd());
        }
    }

    /// <summary>How many blocks of lattice hold a Segment.</summary>
    /// <remarks>
    /// <b>Taken from the loaded run and used for both panels</b>, so the two grids are the same shape
    /// and a reader can compare them cell by cell. The two worlds share a seed, a population and a
    /// command stream, so their road networks are identical by construction — the only thing
    /// <c>[traffic]</c> changes is how long a Vehicle stays on one.
    /// </remarks>
    private static void Extent(Reading reading, int block, out int width, out int height)
    {
        width = 0;
        height = 0;

        for (int slot = 0; slot < reading.Peak.Length; slot++)
        {
            if (!Block(reading.World, slot, block, out int column, out int row))
            {
                continue;
            }

            width = column + 1 > width ? column + 1 : width;
            height = row + 1 > height ? row + 1 : height;
        }
    }

    /// <summary>One glyph per block, taken from the busiest Segment that block holds.</summary>
    private static char[] Bands(Reading reading, int block, int width, int height)
    {
        RoadSegmentTable segments = reading.World.Roads.Segments;

        char[] grid = new char[width * height];

        Array.Fill(grid, NoRoad);

        for (int slot = 0; slot < reading.Peak.Length; slot++)
        {
            if (!Block(reading.World, slot, block, out int column, out int row)
                || column >= width
                || row >= height)
            {
                continue;
            }

            char band = Band(Load(segments, slot, reading.Peak[slot]));
            int cell = (row * width) + column;

            // The worst Segment the block holds, never an average over it: a block containing one
            // saturated Street and nine empty ones is a block with a problem in it, and averaging is
            // exactly what would hide it.
            if (Rank(band) > Rank(grid[cell]))
            {
                grid[cell] = band;
            }
        }

        return grid;
    }

    private static int Rank(char band) => band switch
    {
        Over => 4,
        Heavy => 3,
        Busy => 2,
        Quiet => 1,
        _ => 0,
    };

    private static char Band(Ratio load) => load.Raw switch
    {
        >= Saturated => Over,
        >= ThreeQuarters => Heavy,
        >= Quarter => Busy,
        _ => Quiet,
    };

    /// <summary>
    /// Vehicles present over Vehicles held at capacity, <b>through the table the engine prices
    /// with</b>.
    /// </summary>
    /// <remarks>
    /// <b>Not restated here.</b> <see cref="RoadSegmentTable.LoadOf"/> is where that arithmetic and its
    /// two guard constants live, precisely so this picture and <c>TripEngine</c> cannot disagree about
    /// what <em>loaded</em> means — one fact, one copy (<c>plans/0012</c> <b>Cause 1</b>). The volume
    /// passed in is the peak this run reached, which is not a state any column holds, which is why the
    /// table takes one rather than reading its own.
    /// </remarks>
    private static Ratio Load(RoadSegmentTable segments, int slot, int volume) =>
        segments.LoadOf(slot, volume, segments.FreeFlowOver(slot));

    /// <summary>Which block of the lattice a Segment starts in.</summary>
    private static bool Block(World world, int slot, int block, out int column, out int row)
    {
        column = 0;
        row = 0;

        RoadSegmentTable segments = world.Roads.Segments;

        if (!segments.Rows.IsLive(slot)
            || !world.Roads.Nodes.Rows.TryResolve(segments.NodeA[slot], out int node))
        {
            return false;
        }

        column = world.Roads.Nodes.East[node].Raw / block;
        row = world.Roads.Nodes.North[node].Raw / block;

        return column >= 0 && row >= 0;
    }

    // ---- the numbers ----------------------------------------------------------------------------

    /// <summary>The busiest Segments, with what each one costs free-flow and at its peak.</summary>
    private static void Table(TextWriter output, Reading busy, int block)
    {
        RoadSegmentTable segments = busy.World.Roads.Segments;

        int[] order = [.. Enumerable.Range(0, busy.Peak.Length)
            .Where(slot => segments.Rows.IsLive(slot) && busy.Peak[slot] > 0)
            .OrderByDescending(slot => busy.Peak[slot])
            .ThenBy(slot => slot)];

        if (order.Length == 0)
        {
            output.WriteLine();
            output.WriteLine(
                "(no Segment carried a Vehicle at any point in the run. Departures spread over the "
                + "commute window, so a run shorter than one reaches only the Citizens whose "
                + "departure phase falls inside it.)");

            return;
        }

        output.WriteLine();
        output.WriteLine("   block     peak   holds     v/c   free-flow     at peak     delay");
        output.WriteLine("   -------   ----   -----   -----   ---------   ---------   -------");

        foreach (int slot in order.Take(Busiest))
        {
            Block(busy.World, slot, block, out int column, out int row);

            TravelTime freeFlow = segments.FreeFlowOver(slot);
            Ratio load = Load(segments, slot, busy.Peak[slot]);
            TravelTime atPeak = busy.World.Rules.Traffic.Apply(freeFlow, load);

            // Vehicles held at capacity, restated for the reader out of the same two terms LoadOf
            // divides by: capacity per Tick x the free-flow dwell. It is the denominator of the v/c
            // column beside it, and printing it is what makes that column checkable by hand.
            long holds = (long)segments.CapacityPerDay[slot] * freeFlow.Raw / Ticks.PerDay;

            output.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"   {column,3},{row,-3}   {busy.Peak[slot],4}   {Fraction(holds),5}   "
                + $"{Fraction(load.Raw),5}   {Fraction(freeFlow.Raw),7} T   "
                + $"{Fraction(atPeak.Raw),7} T   {Times(freeFlow, atPeak),7}"));
        }

        if (order.Length > Busiest)
        {
            output.WriteLine();
            output.WriteLine(
                $"   ({order.Length - Busiest} more Segment(s) carried traffic and are not listed. "
                + "The cap is stated because a truncated list reads as a complete one.)");
        }
    }

    /// <summary>The two runs' totals, and the mean the peak has to be read against.</summary>
    private static void Summary(TextWriter output, Reading free, Reading busy, ulong ticks)
    {
        output.WriteLine();
        output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"   free-flow   {free.VehicleTicks,12:N0} vehicle-Ticks over {ticks} Ticks"));
        output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"   loaded      {busy.VehicleTicks,12:N0} vehicle-Ticks   "
            + $"{Times(free.VehicleTicks, busy.VehicleTicks)}"));
        output.WriteLine();
        output.WriteLine(
            $"   mean load   {Per(busy.VehicleTicks, busy.SegmentTicks),12} Vehicles per Segment "
            + "per Tick");
        output.WriteLine(
            $"   empty       {Percent(busy.SegmentTicks - busy.Occupied, busy.SegmentTicks),12} of "
            + "Segment-Ticks carried nothing at all");

        output.WriteLine();

        if (busy.VehicleTicks == free.VehicleTicks)
        {
            // Two causes, and the report names both rather than picking one, because they are
            // distinguished by the v/c column above and not by anything down here. A generated city
            // meets the first at its shipped capacity and the second at every capacity down to about
            // 600 Vehicles an hour, which is a rung nobody would author.
            output.WriteLine(
                "   The two runs came out identical. On a GENERATED city that is a reading rather "
                + "than a defect, and there are");
            output.WriteLine(
                "   TWO reasons for it — read the v/c column above to tell which one you are "
                + "looking at.");
            output.WriteLine();
            output.WriteLine(
                "     v/c is small everywhere. The paved extent scales with the SQUARE ROOT of the "
                + "population, so the network");
            output.WriteLine(
                "     grows with the traffic and the same number sizes both the demand and the "
                + "supply. Congestion is something");
            output.WriteLine(
                "     a PLAYER makes by laying too little road (adr/0090), and CommandKind.Populate "
                + "cannot reach it.");
            output.WriteLine();
            output.WriteLine(
                "     v/c is large and the delay column is not. The extra dwell is real and it is "
                + "SUB-TICK, so it changes what");
            output.WriteLine(
                "     a crossing costs without changing how many Vehicles stand on a road at any "
                + "Tick boundary. A per-Tick");
            output.WriteLine(
                "     snapshot cannot see a sub-Tick delay: the bill moves and this instrument "
                + "cannot.");
            output.WriteLine();
            output.WriteLine(
                "   rulesets/congested.toml is the rung where both halves move — plans/0002 §C, and "
                + "that file's own header");
            output.WriteLine(
                "   carries the sweep that chose it.");
        }
        else
        {
            output.WriteLine(
                "   The volume-delay function is changing what this city costs to cross. Read the "
                + "mean beside the peak: BPR");
            output.WriteLine(
                "   prices means, so a high peak over a low mean is a coincidence of arrival times "
                + "rather than a jam.");
        }
    }

    // ---- formatting -----------------------------------------------------------------------------

    /// <summary>A Q16.16 value to two decimal places, in integers.</summary>
    /// <remarks>
    /// <b>The shell may format and it is <c>Core</c> that may not hold a <c>double</c></b> —
    /// <c>TripDump.Minutes</c>' remark, and integers here anyway, because the alternative is a second
    /// arithmetic running beside the one the engine used and a reader with no way to tell which
    /// produced a disagreeing digit.
    /// </remarks>
    private static string Fraction(long raw) => string.Create(
        CultureInfo.InvariantCulture,
        $"{raw >> 16}.{(((raw & 0xFFFF) * 100) + 32_768) >> 16:D2}");

    /// <summary>How much a loaded crossing costs against a free-flow one, as <c>x1.2345</c>.</summary>
    private static string Times(TravelTime freeFlow, TravelTime loaded) =>
        Times(freeFlow.Raw, loaded.Raw);

    private static string Times(long freeFlow, long loaded)
    {
        if (freeFlow <= 0)
        {
            return "     --";
        }

        long scaled = ((loaded * 10_000) + (freeFlow / 2)) / freeFlow;

        return string.Create(CultureInfo.InvariantCulture, $"x{scaled / 10_000}.{scaled % 10_000:D4}");
    }

    /// <summary>A quotient below one, to four decimal places, in integers.</summary>
    private static string Per(long part, long whole)
    {
        if (whole <= 0)
        {
            return "0.0000";
        }

        long scaled = ((part * 10_000) + (whole / 2)) / whole;

        return string.Create(CultureInfo.InvariantCulture, $"{scaled / 10_000}.{scaled % 10_000:D4}");
    }

    /// <summary>A share as a percentage to one decimal place, in integers.</summary>
    private static string Percent(long part, long whole)
    {
        if (whole <= 0)
        {
            return "0.0%";
        }

        long scaled = ((part * 1_000) + (whole / 2)) / whole;

        return string.Create(CultureInfo.InvariantCulture, $"{scaled / 10}.{scaled % 10}%");
    }
}
