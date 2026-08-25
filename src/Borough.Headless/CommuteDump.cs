using System.Globalization;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Instruments;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Headless;

/// <summary>
/// Prints where people work against where they live, before and after the jobs are taken.
/// </summary>
/// <remarks>
/// <para>
/// <b>5b-bis's <em>something to look at</em>, and the picture worth printing is not a Trip count.</b>
/// A count says the mechanism ran; what it cannot say is whether the city it produced makes sense.
/// The defect class this catches is the one a hash and a census are both blind to — everybody
/// employed in the block they sleep in, or one block hoovering up every worker in the city — and both
/// of those produce a moving hash, a plausible Census and a picture that is obviously wrong.
/// </para>
/// <para>
/// <b>Two grids, because employment is a thing that happens over time.</b> That is <c>--zones</c>'
/// reasoning and not <c>--roads</c>': a Road Graph is laid at world creation and has no <em>before</em>,
/// where a city at Tick 0 has nobody employed at all. The first grid is what the populator built and
/// is therefore the control — every block exports its whole population and imports nobody.
/// </para>
/// <para>
/// <b>It refuses a Ruleset with no <c>[jobs]</c> rather than printing an empty city.</b> Employment is
/// content: <c>rulesets/minimal.toml</c> declared none for six slices and was a legitimate file
/// throughout, so a grid of <c>o</c> everywhere would read as a broken assignment pass instead of as a
/// file that grants no jobs. Same polarity as <c>--zones</c> refusing without a <c>[[zone_rule]]</c>,
/// and for the same reason.
/// </para>
/// <para>
/// <b>Every string here belongs to the shell</b> (<c>adr/0002</c>). <c>Borough.Core</c> hands over
/// coordinates, handles and counters; the glyphs and the sentences are this file's.
/// </para>
/// </remarks>
internal static class CommuteDump
{
    /// <summary>
    /// Four states, because those are the four a reader has to tell apart to judge an assignment
    /// pass.
    /// </summary>
    /// <remarks>
    /// <b>The quantity is a <em>balance</em>, not a count, and that is the choice the picture turns
    /// on.</b> A grid of worker counts is a grid of population, which the Zone dump already shows —
    /// what is new here is the <em>direction</em> people move in the morning, so a block reads as
    /// exporting workers, importing them, or neither. A city where nobody moves and a city where
    /// everybody moves are then two different pictures rather than two similar ones.
    /// </remarks>
    private const char Exports = 'o';
    private const char Balanced = '=';
    private const char Imports = '#';
    private const char Empty = ' ';

    /// <summary>
    /// How far from parity a block has to be before it reads as exporting or importing.
    /// </summary>
    /// <remarks>
    /// <b>A quarter, and it is a property of the picture rather than of the city.</b> Nothing here
    /// folds into a State Hash, so this is an instrument's resolution and not an <c>adr/0052</c>
    /// number — the same standing as the Census cost histogram's bucket edges. Exact parity as the
    /// only <c>=</c> would make the middle glyph vanishingly rare and the picture a two-colour one.
    /// </remarks>
    private const int BalanceNumerator = 1;
    private const int BalanceDenominator = 4;

    /// <summary>Runs the demonstration and writes it to <paramref name="output"/>.</summary>
    internal static int Run(Options options, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(output);

        if (!Session.TryRules(options.RulesetPath, out Ruleset rules))
        {
            return 2;
        }

        if (!rules.Jobs.Runs)
        {
            output.WriteLine(
                "This Ruleset declares no [jobs] table, so no Citizen is ever assigned work and "
                + "there is nothing to picture. Employment is content, not engine: a grid showing "
                + "every block exporting its whole population would read as a broken assignment "
                + "pass rather than as a file that grants no jobs.");

            return 3;
        }

        if (rules.Roads.BlockTiles <= 0)
        {
            output.WriteLine(
                "This Ruleset declares no [roads] table, so there is no lattice to bin Buildings "
                + "into and no network to walk. A commute is a journey between two Addresses, and "
                + "an Address is a Segment.");

            return 3;
        }

        var key = WorldKey.FromSeed(options.Seed);
        World world = new(options.Citizens, rules, key);
        Simulation simulation = new(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        int block = rules.Roads.BlockTiles;

        output.WriteLine("# Borough commute dump");
        output.WriteLine(
            $"# {world.Citizens.Rows.LiveCount} Citizens, {world.Buildings.Rows.LiveCount} Buildings, "
            + $"{Jobs(world)} job(s) declared. Blocks are {block} Tiles.");
        output.WriteLine(
            $"# Commute Budget: fast to {TripDump.Minutes(rules.Trips.Fast.Raw)} min, moderate to "
            + $"{TripDump.Minutes(rules.Trips.Moderate.Raw)}, unsavoury to "
            + $"{TripDump.Minutes(rules.Trips.CommuteBudget.Raw)}, refused beyond it. Only the "
            + "ceiling refuses.");
        output.WriteLine(
            $"# Assignment looks at {rules.Jobs.SampleFor(world.Citizens.Rows.SlotCount)} Citizen(s) "
            + $"every {rules.Jobs.Interval} Ticks, {rules.Jobs.Candidates} candidate(s) each, and "
            + "takes the best rung it draws.");
        output.WriteLine(
            $"# A Shift runs {rules.Jobs.ShiftHoursMin}-{rules.Jobs.ShiftHoursMax} in-world hours; "
            + "each Workplace starts its own hour inside its kind's band, and a Citizen leaves home "
            + "by the commute they were quoted plus up to "
            + $"{rules.Jobs.ArriveEarlyMaxMinutes} min of being early. The {Ticks.PerDay}-Tick Day's "
            + "shape is produced by those spreads and is authored nowhere.");

        output.WriteLine();
        output.WriteLine("## Before — the populator's city, in which nobody has a job");
        Write(output, world, block, options.Csv);

        simulation.Trips.Drain();

        for (ulong tick = 0; tick < options.Ticks; tick++)
        {
            simulation.Step(default);
        }

        TripActivity trips = simulation.Trips.Drain();

        output.WriteLine();
        output.WriteLine($"## After — {options.Ticks} Ticks of assignment and commuting");
        Write(output, world, block, options.Csv);

        output.WriteLine();
        output.WriteLine("## What the commute costs");
        Costs(output, world, trips, options.Ticks, rules);

        return 0;
    }

    /// <summary>The city's employment, by block, as a grid of balances.</summary>
    private static void Write(TextWriter output, World world, int block, bool csv)
    {
        CitizenTable citizens = world.Citizens;

        int east = 0;
        int north = 0;

        // Two passes, because the extent is not known until every Building has been placed and a grid
        // sized from the lattice would be the whole map rather than the developed part of it.
        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (!Block(world, slot, block, out int column, out int row))
            {
                continue;
            }

            east = column > east ? column : east;
            north = row > north ? row : north;
        }

        int width = east + 1;
        int height = north + 1;

        int[] residents = new int[width * height];
        int[] workers = new int[width * height];

        for (int slot = 0; slot < citizens.Rows.SlotCount; slot++)
        {
            if (!citizens.Rows.IsLive(slot))
            {
                continue;
            }

            if (Home(world, slot, out int home) && Block(world, home, block, out int hc, out int hr))
            {
                residents[(hr * width) + hc]++;
            }

            // Two hops: a Workplace is a Business and a Business borrows its premises' location.
            if (world.Businesses.Rows.TryResolve(citizens.Workplace[slot], out int employer)
                && world.Buildings.Rows.TryResolve(
                    world.Businesses.Building[employer], out int workplace)
                && Block(world, workplace, block, out int wc, out int wr))
            {
                workers[(wr * width) + wc]++;
            }
        }

        if (csv)
        {
            output.WriteLine("block_east,block_north,residents,workers");

            for (int cell = 0; cell < residents.Length; cell++)
            {
                if (residents[cell] == 0 && workers[cell] == 0)
                {
                    continue;
                }

                output.WriteLine(string.Create(
                    CultureInfo.InvariantCulture,
                    $"{cell % width},{cell / width},{residents[cell]},{workers[cell]}"));
            }

            return;
        }

        char[] grid = new char[width * height];
        int exporting = 0;
        int importing = 0;
        int balanced = 0;

        for (int cell = 0; cell < grid.Length; cell++)
        {
            int here = residents[cell];
            int there = workers[cell];

            if (here == 0 && there == 0)
            {
                grid[cell] = Empty;
                continue;
            }

            // The band is symmetric about parity and is measured against the larger side, so a block
            // of 100 residents and 80 workers reads the same as one of 5 and 4.
            int larger = here > there ? here : there;
            int gap = here > there ? here - there : there - here;

            if (gap * BalanceDenominator <= larger * BalanceNumerator)
            {
                grid[cell] = Balanced;
                balanced++;
            }
            else if (here > there)
            {
                grid[cell] = Exports;
                exporting++;
            }
            else
            {
                grid[cell] = Imports;
                importing++;
            }
        }

        int clippedWidth = width;
        int clippedHeight = height;

        width = width > Window ? Window : width;
        height = height > Window ? Window : height;

        output.WriteLine(
            $"{exporting} block(s) send workers out, {importing} take them in, {balanced} are within "
            + $"a {BalanceNumerator}/{BalanceDenominator} of parity. '{Exports}' has more residents "
            + $"than workers, '{Imports}' more workers than residents, '{Balanced}' neither, blank is "
            + "no Building at all."
            + (clippedWidth > width || clippedHeight > height
                ? Clipped(width, height, clippedWidth, clippedHeight)
                : string.Empty));

        for (int row = height - 1; row >= 0; row--)
        {
            output.WriteLine(new string(grid, row * clippedWidth, width).TrimEnd());
        }
    }

    /// <summary>What the run's Trips cost, as the Census histogram plus the two counts that frame it.</summary>
    /// <remarks>
    /// <para>
    /// <b>Read off the Census family rather than re-walked here</b>, which is 5b-bis task 6's whole
    /// point: the distribution is a property of the run, and an instrument that recomputed it would be
    /// measuring a second city that happened to share a seed.
    /// </para>
    /// <para>
    /// ⚠ <b>The <em>works at home</em> line is here because task 6 saw it and refused to change it
    /// mid-instrumentation.</b> <c>[[building]] jobs</c> sits on the <c>dwelling</c> kind, so a Citizen
    /// can be employed in the Building they sleep in and the generator makes that a Trip of no length.
    /// It is not obviously wrong — living above the shop is the arrangement task 4 chose on purpose —
    /// but it is invisible in every other reading, so this is where it gets counted.
    /// </para>
    /// </remarks>
    private static void Costs(
        TextWriter output, World world, TripActivity trips, ulong ticks, Ruleset rules)
    {
        CitizenTable citizens = world.Citizens;

        int employed = 0;
        int atHome = 0;

        for (int slot = 0; slot < citizens.Rows.SlotCount; slot++)
        {
            if (!citizens.Rows.IsLive(slot)
                || !world.Businesses.Rows.TryResolve(citizens.Workplace[slot], out int employer)
                || !world.Buildings.Rows.TryResolve(
                    world.Businesses.Building[employer], out int workplace))
            {
                continue;
            }

            employed++;

            if (Home(world, slot, out int home) && home == workplace)
            {
                atHome++;
            }
        }

        output.WriteLine(
            $"{employed} of {citizens.Rows.LiveCount} Citizens hold a job, of whom {atHome} work in "
            + "the Building they live in — the dwelling kind declares a trade and construction "
            + "instantiates it, so living above the shop is a legitimate arrangement and a commute "
            + "of no length.");

        long made = 0;

        foreach ((TripCostBucket bucket, string _) in Bands)
        {
            made += trips.Costs[bucket].Sum;
        }

        output.WriteLine(
            $"{made} Trip(s) were made over {ticks} Ticks — {trips.Completed.Sum} completed, "
            + $"{trips.NoRouteFound.Sum} found no route, {trips.ExceededCommuteBudget.Sum} exceeded "
            + $"the Commute Budget, {trips.Stranded.Sum} were stranded.");

        if (made == 0)
        {
            output.WriteLine(
                "No Trip was made, so there is no distribution. Departures follow each Workplace's "
                + "own Shift start, so they are spread across the whole Day rather than bunched at "
                + "its beginning; a run shorter than a Day reaches only the hours it covers.");

            return;
        }

        output.WriteLine();

        foreach ((TripCostBucket bucket, string label) in Bands)
        {
            long count = trips.Costs[bucket].Sum;

            output.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"  {label,-16} {count,7:N0}  {new string('#', (int)(count * 40 / made))}"));
        }

        output.WriteLine();
        output.WriteLine(
            "⚠ This distribution is NOT the one the Commute Budget is a percentile of. A commute "
            + "exists only because the assignment pass already accepted the job at the other end of "
            + "it, inside the Budget, so the ceiling is upstream and this is censored by the number "
            + "it would be used to ratify. The uncensored reading is --trips, which walks every "
            + "Building pair and had to be taken before any Budget was set.");
    }

    /// <summary>The Census cost histogram's bands, with the labels this shell gives them.</summary>
    private static (TripCostBucket Bucket, string Name)[] Bands =>
    [
        (TripCostBucket.UnderOneMinute, "under 1 min"),
        (TripCostBucket.UnderTwoMinutes, "1-2 min"),
        (TripCostBucket.UnderFourMinutes, "2-4 min"),
        (TripCostBucket.UnderEightMinutes, "4-8 min"),
        (TripCostBucket.UnderSixteenMinutes, "8-16 min"),
        (TripCostBucket.UnderThirtyTwoMinutes, "16-32 min"),
        (TripCostBucket.ThirtyTwoMinutesOrMore, "32 min or none"),
    ];

    /// <summary>The Building a Citizen lives in.</summary>
    private static bool Home(World world, int citizen, out int building)
    {
        building = Rows.NoSlot;

        return world.Households.Rows.TryResolve(
                   world.Citizens.HouseholdOf[citizen], out int household)
            && world.Buildings.Rows.TryResolve(
                   world.Households.Dwelling[household], out building);
    }

    /// <summary>Which block of the lattice a Building stands in.</summary>
    private static bool Block(World world, int building, int block, out int column, out int row)
    {
        column = 0;
        row = 0;

        if (!world.Buildings.Rows.IsLive(building)
            || !world.Lots.Rows.TryResolve(world.Buildings.Lot[building], out int lot))
        {
            return false;
        }

        column = world.Lots.East[lot].Raw / block;
        row = world.Lots.North[lot].Raw / block;

        return true;
    }

    /// <summary>How wide and tall the picture may get, in characters. A terminal's property.</summary>
    private const int Window = 128;

    private static string Clipped(int shownEast, int shownNorth, int east, int north) => string.Create(
        CultureInfo.InvariantCulture,
        $" Showing the {shownEast}×{shownNorth} blocks nearest the origin, of {east}×{north}; the counts above are over the whole city.");

    /// <summary>Jobs the Ruleset grants, summed over the Buildings that stand.</summary>
    private static long Jobs(World world)
    {
        long total = 0;

        // Businesses rather than Buildings as of milestone 27 task 7: a trade declares the jobs and
        // premises declare none. A world with no Business in it therefore reports ZERO, which is the
        // honest number rather than a broken one -- there is nobody to work for.
        for (int slot = 0; slot < world.Businesses.Rows.SlotCount; slot++)
        {
            if (world.Businesses.Rows.IsLive(slot))
            {
                total += world.Rules.BusinessKind(world.Businesses.Kind[slot]).Jobs;
            }
        }

        return total;
    }
}
