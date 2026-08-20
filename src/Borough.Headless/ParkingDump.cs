namespace Borough.Headless;

using System.Globalization;
using Borough.Core;
using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

/// <summary>
/// <c>--parking</c>: where people parked against where they were going, and the walk between.
/// </summary>
/// <remarks>
/// <para>
/// <b>Milestone 7 task 7, and the picture worth printing is not a grid of occupancy.</b> Capacity is
/// declared per building <b>kind</b>, so a grid of occupied spaces is a grid of land use redrawn, and
/// <c>--zones</c> already draws that. What is new here is the <b>gap</b> — the distance between the
/// space a driver took and the door they wanted — which is <c>--commute</c>'s reasoning about printing
/// a balance rather than a count, arriving on a distance instead of on a direction.
/// </para>
/// <para>
/// <b>The quantity is <c>adr/0009</c>'s own sentence.</b> That ADR argues for modelled supply over an
/// averaged one by naming what an average cannot say: not <i>"downtown averages eight minutes from
/// your car"</i> but <i>"you parked three blocks away because the two nearer garages were full."</i>
/// The walk is the half this mode can measure, and it is the half that costs the player something —
/// <c>adr/0008</c> makes the walk a simulated Leg, so it is priced into the Commute Budget and shows
/// up as scarcity long before a Trip fails.
/// </para>
/// <para>
/// <b>Two walks and not one, because <c>adr/0009</c> says the two sheds must be balanced
/// separately.</b> The walk <em>from</em> the car is the arrival — a driver parks as near the door as
/// the shed allows. The walk <em>to</em> the car is the departure, and it is the overnight case:
/// <i>"a household's car sits at home overnight"</i>, so a driver starts the day walking to wherever
/// yesterday left them. They are different distributions over different supply and printing one number
/// would average a residential shed against a commercial one.
/// </para>
/// <para>
/// <b>It steps the world, and it samples every Tick rather than reading the tables at the end.</b>
/// ***A table emptied by the mechanism under test cannot be read after the mechanism has run*** —
/// milestone 7 task 5's finding, paid for there by a test that walked <c>world.Legs</c> after the run
/// and found <b>zero</b> Legs of either mode, because <c>TripEngine.Release</c> frees them as Trips
/// resolve. A Leg is counted once, by its Handle, so a journey in flight across forty Ticks
/// contributes one walk and not forty.
/// </para>
/// <para>
/// <b>It refuses a Ruleset whose city keeps no cars.</b> <c>--traffic</c>'s polarity: with nobody
/// driving every panel here is empty, and an empty picture reads as a broken instrument rather than as
/// a file that grants nobody a car. ⚠ <b><c>rulesets/minimal.toml</c> is such a file</b> — it declares
/// <c>parking = 8</c> and <c>[parking] radius_metres = 400</c> and no <c>car_ownership_percent</c> at
/// all — which is why the golden session's State Hash never moved for any of this milestone.
/// </para>
/// <para>
/// <b>Every string here belongs to the shell</b> (<c>adr/0002</c>). <c>Borough.Core</c> hands over
/// costs, handles and counters; the sentences are this file's.
/// </para>
/// </remarks>
internal static class ParkingDump
{
    /// <summary>The upper edge of each walk band, in whole in-world minutes.</summary>
    /// <remarks>
    /// <b>Bands rather than percentiles alone, and the edges are the instrument's rather than the
    /// city's.</b> Nothing here folds into a State Hash, so these are a picture's resolution and not
    /// an <c>adr/0052</c> number — the Trip cost histogram's standing. They double because a shed
    /// degrades by widening, so the interesting structure is at the short end and a linear scale would
    /// spend every band on it.
    /// </remarks>
    private static readonly int[] Bands = [1, 2, 4, 8, 16];

    /// <summary>Runs the demonstration and writes it to <paramref name="output"/>.</summary>
    internal static int Run(Options options, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(output);

        if (!Session.TryRules(options.RulesetPath, out Ruleset rules))
        {
            return 2;
        }

        if (Refuse(rules, output) is int refusal)
        {
            return refusal;
        }

        var key = WorldKey.FromSeed(options.Seed);
        World world = new(options.Citizens, rules);
        Simulation simulation = new(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        var sampler = new Sampler();

        for (ulong tick = 0; tick < options.Ticks; tick++)
        {
            simulation.Step(default);
            sampler.Observe(world);
        }

        output.WriteLine("# Borough Parking dump");
        output.WriteLine(
            $"# {options.Citizens} Citizens, {options.Ticks} Ticks, "
            + $"{world.Buildings.Rows.LiveCount} Buildings standing, "
            + $"{world.CarParks.Rows.LiveCount} Car Parks.");
        output.WriteLine(
            $"# Shed radius {rules.Parking.RadiusMetres} m, keeping {rules.Parking.Keeps}; "
            + $"{rules.Households.CarOwnershipPercent}% of Households keep a car.");
        output.WriteLine();

        // The shed's radius as a WALK, which is the only thing that makes either panel falsifiable.
        // A space is chosen from a ball of this radius around the destination's door, so the arrival
        // walk cannot exceed it -- and the departure walk has no such ceiling, because the car is
        // wherever the last journey left it and the next journey need not start there.
        TravelTime ceiling = TravelTime.Over(rules.Parking.Radius, world.Roads.Ruleset.WalkSpeed);

        sampler.From.Write(
            output,
            "## The walk FROM the car — where you parked against where you were going",
            "The gap adr/0009 is about. A driver takes the nearest space with room, so this is how far",
            "the shed had to widen before it found one. Zero is a space at the destination's own door.",
            ceiling,
            "it is a CEILING here: a space is chosen from a ball of that radius around the door");

        output.WriteLine();

        sampler.To.Write(
            output,
            "## The walk TO the car — where you set off against where you left it",
            "The departure, and adr/0009's overnight case: a car sits where yesterday parked it, so",
            "this is the residential shed and not the commercial one. Zero is a car at your own door.",
            ceiling,
            "it is NOT a ceiling here — the same figure is printed under both panels to say so");

        output.WriteLine();

        Supply(output, world, sampler);

        return 0;
    }

    /// <summary>
    /// Supply against holding — the balance, and deliberately not a grid.
    /// </summary>
    /// <remarks>
    /// <b>Peak rather than the closing figure alone, because the closing figure is a time of day.</b>
    /// <c>adr/0101</c> anchors a commute on the Workplace's Shift start hour and Tick 0 is midnight,
    /// so a run ending mid-morning ends with the residential sheds empty and the commercial ones full.
    /// A single occupancy reading is therefore a statement about when the run stopped, and the peak is
    /// the one number that is about the city.
    /// </remarks>
    private static void Supply(TextWriter output, World world, Sampler sampler)
    {
        int capacity = 0;
        int occupied = 0;

        for (int slot = 0; slot < world.CarParks.Rows.SlotCount; slot++)
        {
            if (!world.CarParks.Rows.IsLive(slot))
            {
                continue;
            }

            capacity += world.CarParks.Capacity[slot];
            occupied += world.CarParks.Occupied[slot];
        }

        int drivers = 0;

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (world.Citizens.Rows.IsLive(slot) && world.ModeOf(slot) == TravelMode.Car)
            {
                drivers++;
            }
        }

        output.WriteLine("## Supply against holding");
        output.WriteLine();
        output.WriteLine(
            "  Not a grid. Capacity is declared per building KIND and demand is per Citizen, so a map");
        output.WriteLine(
            "  of occupied spaces is a map of land use and --zones already draws that. What is left");
        output.WriteLine(
            "  that a Ruleset does not already state is the balance, and how much of it a peak uses.");
        output.WriteLine();
        output.WriteLine($"  spaces built            {capacity,8}");
        output.WriteLine($"  held at the last Tick   {occupied,8}   {Share(occupied, capacity)}");
        output.WriteLine($"  held at the peak        {sampler.PeakHeld,8}   {Share(sampler.PeakHeld, capacity)}");
        output.WriteLine();
        output.WriteLine($"  Citizens who drive      {drivers,8}");
        output.WriteLine($"  spaces per driver       {Ratio(capacity, drivers),8}");
        output.WriteLine();

        if (sampler.PeakHeld * 2 < capacity)
        {
            // Printed rather than refused, and it is the milestone's own open question rather than a
            // defect in the run. plans/0002 D1's ratifier for the shed radius is the walk-Leg length
            // distribution AS OCCUPANCY APPROACHES 1, and a generated city cannot get there: capacity
            // is per kind and demand is per Citizen, both sized by the generator from one population.
            // An instrument that showed the gap without saying it cannot be closed would read as a
            // city with spare parking rather than as a world that cannot demonstrate scarcity.
            output.WriteLine(
                "  ⚠ THE PEAK IS UNDER HALF THE SUPPLY, and no --citizens will change that. Capacity");
            output.WriteLine(
                "  is per building kind and demand is per Citizen, so the generator sizes both from");
            output.WriteLine(
                "  one population and occupancy is FLAT at every city size. The shed radius's");
            output.WriteLine(
                "  ratifier is this distribution as occupancy approaches 1 (plans/0002 D1), and");
            output.WriteLine(
                "  nothing this project can generate approaches it — that needs a Ruleset whose");
            output.WriteLine(
                "  per-kind parking is cut until it does, on rulesets/congested.toml's precedent.");
        }
    }

    /// <summary>
    /// Refuses a Ruleset that parks nobody, because both walk panels would be empty for a reason
    /// that is not about parking.
    /// </summary>
    /// <remarks>
    /// <b>Two refusals and not one, because the two absences are different files and a reader meeting
    /// either needs a different sentence.</b> A file with no <c>[parking]</c> has supply nobody can
    /// find; a file with no <c>[households]</c> has drivers nobody has. <c>rulesets/minimal.toml</c> is
    /// the second, and it is the file an operator will reach for first.
    /// </remarks>
    private static int? Refuse(Ruleset rules, TextWriter output)
    {
        if (!rules.Parking.Runs)
        {
            output.WriteLine(
                "This Ruleset states no [parking] radius_metres, so no arrival ever queries a shed "
                + "and no space is ever taken, whatever the Buildings declare. Both walk panels "
                + "would be empty for a reason that has nothing to do with parking.");
            output.WriteLine();
            output.WriteLine("  --parking --ruleset rulesets/congested.toml --citizens 16000 --ticks 4096");

            return 3;
        }

        if (!rules.Households.Runs)
        {
            output.WriteLine(
                "No Household in this Ruleset keeps a car: it states no [households] "
                + "car_ownership_percent, and adr/0098 makes the absence of the table mean nobody "
                + "drives rather than a defaulted rate. Nobody drives, so nobody parks, and every "
                + "panel here would be empty — which reads as a broken instrument rather than as a "
                + "file that grants nobody a car.");
            output.WriteLine();
            output.WriteLine(
                "  rulesets/minimal.toml is such a file, deliberately: it declares parking = 8 and a "
                + "400 m shed and no ownership at all, which is why the golden session's State Hash "
                + "never moved for any of milestone 7.");
            output.WriteLine();
            output.WriteLine("  --parking --ruleset rulesets/congested.toml --citizens 16000 --ticks 4096");

            return 3;
        }

        return null;
    }

    /// <summary>A percentage, or a dash where the denominator is nothing.</summary>
    private static string Share(int part, int whole) =>
        whole == 0 ? "—" : $"{IntegerMath.RoundDiv((long)part * 100, whole)}% of supply";

    /// <summary>One decimal of <paramref name="part"/> over <paramref name="whole"/>.</summary>
    private static string Ratio(int part, int whole)
    {
        if (whole == 0)
        {
            return "—";
        }

        long tenths = IntegerMath.RoundDiv((long)part * 10, whole);

        return $"{tenths / 10}.{tenths % 10}";
    }

    /// <summary>
    /// Walks the Leg chain each Tick and keeps every foot Leg that flanks a drive, once.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A foot Leg is only a parking walk because of what is next to it</b>, which is why this walks
    /// adjacency rather than filtering on mode. Every Leg in the world is one of <c>adr/0008</c>'s
    /// three, and the outer two are foot Legs whose whole content is the parking decision.
    /// </para>
    /// <para>
    /// ⚠ <b>Counted by Handle and not by slot.</b> A Leg's slot is recycled the moment its Trip
    /// resolves, so a set of slots would collapse a city's whole day into at most one walk per row —
    /// and a set that never forgot would count one journey once per Tick it was in flight. The
    /// generation is what separates the two, and it is the reason <c>Rows.At</c> hands back a Handle
    /// rather than an index.
    /// </para>
    /// </remarks>
    private sealed class Sampler
    {
        private readonly HashSet<Handle<Leg>> _seen = [];

        internal Walks To { get; } = new();

        internal Walks From { get; } = new();

        /// <summary>The most spaces held at once across the run.</summary>
        internal int PeakHeld { get; private set; }

        internal void Observe(World world)
        {
            LegTable legs = world.Legs;
            RoadSegmentTable segments = world.Roads.Segments;

            int held = 0;

            for (int slot = 0; slot < world.CarParks.Rows.SlotCount; slot++)
            {
                if (world.CarParks.Rows.IsLive(slot))
                {
                    held += world.CarParks.Occupied[slot];
                }
            }

            if (held > PeakHeld)
            {
                PeakHeld = held;
            }

            for (int slot = 0; slot < legs.Rows.SlotCount; slot++)
            {
                if (!legs.Rows.IsLive(slot) || (TravelMode)legs.Mode[slot] != TravelMode.Car)
                {
                    continue;
                }

                Take(legs, segments, Preceding(legs, slot), To);
                Take(legs, segments, legs.Next[slot] - 1, From);
            }
        }

        /// <summary>Records <paramref name="walk"/> in <paramref name="into"/>, if it is a new one.</summary>
        private void Take(LegTable legs, RoadSegmentTable segments, int walk, Walks into)
        {
            if (walk < 0
                || !legs.Rows.IsLive(walk)
                || (TravelMode)legs.Mode[walk] != TravelMode.Foot
                || !_seen.Add(legs.Rows.At(walk)))
            {
                return;
            }

            // The COST and not the endpoints, because the cost is what the Commute Budget spends and
            // what a player feels. Two Addresses that differ is the weaker claim task 5's tests make;
            // this is the same walk denominated in the currency adr/0008 chose for it.
            into.Add(legs.Time[walk], legs.From(walk, segments) == legs.To(walk, segments));
        }

        /// <summary>The Leg whose <c>Next</c> is <paramref name="target"/>, or <c>-1</c>.</summary>
        /// <remarks>
        /// A Leg list is singly linked — <c>adr/0075</c>, and a back pointer would be a second copy of
        /// the order — so the Leg before one is found by looking. <c>TripDump</c> does the same.
        /// </remarks>
        private static int Preceding(LegTable legs, int target)
        {
            for (int slot = 0; slot < legs.Rows.SlotCount; slot++)
            {
                if (legs.Rows.IsLive(slot) && legs.Next[slot] - 1 == target)
                {
                    return slot;
                }
            }

            return -1;
        }
    }

    /// <summary>One walk distribution, and how it prints.</summary>
    private sealed class Walks
    {
        private readonly List<long> _costs = [];

        /// <summary>Walks whose two Addresses are the same one.</summary>
        private int _atTheDoor;

        internal void Add(TravelTime cost, bool sameAddress)
        {
            _costs.Add(cost.Raw);

            if (sameAddress)
            {
                _atTheDoor++;
            }
        }

        internal void Write(
            TextWriter output,
            string heading,
            string first,
            string second,
            TravelTime ceiling,
            string ceilingNote)
        {
            output.WriteLine(heading);
            output.WriteLine();
            output.WriteLine($"  {first}");
            output.WriteLine($"  {second}");
            output.WriteLine();

            if (_costs.Count == 0)
            {
                // Distinct from a city of zero-length walks, which is what a table of zeroes below
                // would have said. The Ruleset was not refused, so this is a run in which no car Trip
                // ever completed a three-Leg journey -- too few Ticks, or nowhere to drive to.
                output.WriteLine(
                    "  NOT ONE walk of this kind happened. The Ruleset parks people, so this is the "
                    + "run: no car journey got as far as a flanking Leg. Try more --ticks.");

                return;
            }

            _costs.Sort();

            output.WriteLine(
                $"  {_costs.Count} walks. {_atTheDoor} of them ({Percent(_atTheDoor, _costs.Count)}) "
                + "never left the Address they started on.");
            output.WriteLine();
            output.WriteLine("  p50        p90        max        longest band");
            output.WriteLine(
                $"  {TripDump.Minutes(Percentile(50)),5} min  {TripDump.Minutes(Percentile(90)),5} min  "
                + $"{TripDump.Minutes(_costs[^1]),5} min  {Longest()}");
            output.WriteLine();
            output.WriteLine("  up to        walks     share");

            int from = 0;

            foreach (int band in Bands)
            {
                int to = Upper(band);
                output.WriteLine(
                    $"  {band,2} min  {to - from,11}     {Percent(to - from, _costs.Count)}");
                from = to;
            }

            output.WriteLine(
                $"  longer  {_costs.Count - from,11}     {Percent(_costs.Count - from, _costs.Count)}");

            Ceiling(output, ceiling, ceilingNote);
        }

        /// <summary>
        /// How many of these walks are longer than a walk across the shed, which is the one claim
        /// either panel makes that a run can refute.
        /// </summary>
        /// <remarks>
        /// <b>Printed on both panels and load-bearing on neither alone.</b> On the arrival walk it is
        /// an <em>assertion</em> — <c>World.TryChooseParking</c> chooses from a ball of the shed's
        /// radius around the destination's door, so a walk past it is a space nothing could have
        /// picked. On the departure walk it is a <em>contrast</em>: nothing bounds that one, because
        /// the car is where the last journey left it and <c>TripEngine.Itinerary</c> relates
        /// <c>waypoints[1]</c> to the Citizen's holding and never to <c>waypoints[0]</c>. Printing the
        /// same number under both is what makes the asymmetry a reading rather than an impression.
        /// </remarks>
        private void Ceiling(TextWriter output, TravelTime ceiling, string note)
        {
            int over = 0;

            for (int i = _costs.Count - 1; i >= 0 && _costs[i] > ceiling.Raw; i--)
            {
                over++;
            }

            output.WriteLine();
            output.WriteLine($"  A walk across the whole shed is {TripDump.Minutes(ceiling.Raw)} min, and");
            output.WriteLine($"  {note}.");
            output.WriteLine(
                $"  {over} of these {_costs.Count} walks are longer than that "
                + $"({Percent(over, _costs.Count)}).");
        }

        /// <summary>How many walks are at most <paramref name="minutes"/> long.</summary>
        private int Upper(int minutes)
        {
            long edge = TravelTime.FromMinutes(minutes).Raw;
            int count = 0;

            while (count < _costs.Count && _costs[count] <= edge)
            {
                count++;
            }

            return count;
        }

        /// <summary>The label of the last band anything landed in.</summary>
        private string Longest()
        {
            foreach (int band in Bands)
            {
                if (_costs[^1] <= TravelTime.FromMinutes(band).Raw)
                {
                    return $"{band} min";
                }
            }

            return $"over {Bands[^1]} min";
        }

        private long Percentile(int which) =>
            _costs[(int)IntegerMath.RoundDiv((long)(_costs.Count - 1) * which, 100)];

        private static string Percent(int part, int whole) =>
            whole == 0
                ? "—"
                : IntegerMath.RoundDiv((long)part * 100, whole).ToString(CultureInfo.InvariantCulture) + "%";
    }
}
