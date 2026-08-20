using System.Globalization;
using Borough.Core;
using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Movement;
using Borough.Core.Parking;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;
using Borough.Tests.Golden;
using Xunit.Abstractions;

namespace Borough.Tests.Parking;

/// <summary>
/// <b>What a Parking Shed does as occupancy climbs — milestone 7 task 8's ratifying instrument.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>Three numbers ratify on this one machine, and <c>plans/0002</c> says so itself.</b> The shed
/// <b>radius</b> (§D1) is ratified by <i>the walk-Leg length distribution as shed occupancy approaches
/// 1</i>; <i>does parking scarcity degrade as a gradient?</i> (§B) is refuted by <b>the same
/// distribution</b>, because <i>a jump is a cliff, whatever the mechanism intended</i>; and the shed
/// <b>cap</b> (§D1) is ratified by <b>exhaustion</b> and explicitly not by walk length. ***Three
/// questions on one instrument is the corpus's own arrangement and not a saving***, and it works only
/// because each reads a different column of the same sweep.
/// </para>
/// <para>
/// <b>The world is the half <c>adr/0052</c>'s second amendment added, and a generated city is not
/// it.</b> Capacity is per building <b>kind</b> and demand is per <b>Citizen</b>, both sized by
/// <see cref="SyntheticCity"/> from one population — ***the same number sizes both the demand and the
/// supply*** — so occupancy is flat at every population and nothing this project generates approaches
/// 1. What varies it is the one dial that is not derived from the population: <c>[[building]]
/// parking</c>. The sweep below cuts it, which is <c>rulesets/congested.toml</c>'s method exactly.
/// </para>
/// <para>
/// <b>The reading is a <em>probe</em> and not a harvest of Legs, and that is what makes the rungs
/// comparable.</b> A Leg exists only where a Trip happened, and cutting supply cuts the Trips — so a
/// distribution gathered from Legs would thin out at exactly the rungs it is meant to describe, and
/// the scarcest city would report the fewest walks. Instead every rung runs the same city for the
/// same whole number of Days and is then asked the same question at every door:
/// <c>World.TryChooseParking</c>, and the walk to whatever it answers. ***A distribution measured
/// over survivors is a distribution of survival.***
/// </para>
/// <para>
/// ⚠ <b>The probe is a query and not an acquire</b> — <c>TryChooseParking</c> writes nothing, which is
/// the reason it exists separately from <c>TryTakeParking</c>. So the sweep can ask every door in the
/// city what it would find without any door's answer changing the next one's.
/// </para>
/// </remarks>
[Trait(Tier.Key, Tier.Instrument)]
public sealed class ParkingScarcityTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    /// <summary>
    /// The dial, cut rung by rung. <c>8</c> is what every shipped Ruleset declares.
    /// </summary>
    /// <remarks>
    /// <b>Down to 1 rather than to 0, because 0 is a different question.</b> A kind declaring no
    /// parking has no Car Park at all, so every shed is empty for want of supply rather than for want
    /// of room — <c>adr/0070</c>'s *unbuilt* wearing the clothes of scarcity, and a rung that would
    /// report 100% exhaustion while measuring nothing about a shed.
    /// </remarks>
    private static readonly int[] Rungs = [8, 6, 5, 4, 3, 2, 1];

    /// <summary>
    /// Whole Days, past the employment ramp.
    /// </summary>
    /// <remarks>
    /// <b>Four Days rather than a round Tick count, which is task 8's own instruction and 5c task 8's
    /// finding.</b> Every parking figure taken from Tick 0 is taken while employment is still
    /// ramping — jobs are assigned on a <c>revisit_ticks = 1024</c> cadence — so a run that stops on a
    /// number chosen for its roundness stops somewhere different in the Day at every population.
    /// ***A city has a time of day, so a reading has one too.***
    /// </remarks>
    private const int Days = 4;

    private const int Population = 4_000;

    /// <summary>
    /// <b>The sweep. Occupancy against the walk it costs and the exhaustion it produces.</b>
    /// </summary>
    /// <remarks>
    /// It prints before it asserts anything, which is 5c task 8's rule: an acceptance run that speaks
    /// only on success is one you cannot use on the day it fails.
    /// </remarks>
    [Fact]
    public void The_walk_and_the_exhaustion_as_occupancy_climbs()
    {
        _output.WriteLine(
            $"Parking scarcity sweep — {Population} Citizens, {Days} whole Days, minimal.toml with "
            + "car_ownership_percent = 100 and [[building]] parking cut rung by rung.");
        _output.WriteLine("");
        _output.WriteLine(
            "  parking  spaces    held  occupancy   probes  exhausted  past cap   walk p50   walk p90   walk max");
        _output.WriteLine(
            "  -------  ------  ------  ---------   ------  ---------  --------   --------   --------   --------");

        var readings = new List<Reading>();

        foreach (int rung in Rungs)
        {
            Reading reading = Measure(rung);
            readings.Add(reading);

            _output.WriteLine(
                $"  {rung,7}  {reading.Spaces,6}  {reading.Occupied,6}  "
                + $"{Percent(reading.Occupied, reading.Spaces),9}   {reading.Probes,6}  "
                + $"{Percent(reading.Exhausted, reading.Probes),9}  {reading.BeyondCap,8}   "
                + $"{Minutes(reading.Percentile(50)),6} min  {Minutes(reading.Percentile(90)),6} min  "
                + $"{Minutes(reading.Longest),6} min");
        }

        _output.WriteLine("");
        _output.WriteLine(
            $"A walk across the whole shed is {Minutes(Ceiling().Raw)} min at "
            + $"{Ruleset(8).Parking.RadiusMetres} m — the radius bounds this walk and nothing else "
            + "does.");

        // The instrument's own control, and the only thing it asserts. If the dial does not move
        // occupancy, every column above is a reading of one world printed seven times -- which is the
        // failure decision 3 predicted for the POPULATION axis and would be far worse here, because
        // this is the axis chosen to escape it.
        Assert.True(
            Share(readings[^1]) > Share(readings[0]),
            "cutting [[building]] parking did not raise occupancy, so this sweep is one world "
            + "measured seven times and ratifies nothing.");
    }

    /// <summary>One rung: run the city, then ask every door what it would find.</summary>
    private static Reading Measure(int rung)
    {
        Ruleset rules = Ruleset(rung);
        var key = WorldKey.FromSeed(GoldenFixtures.Seed);
        World world = new(Population, rules);
        Simulation simulation = new(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        for (int tick = 0; tick < Days * Ticks.PerDay; tick++)
        {
            simulation.Step(default);
        }

        var reading = new Reading();

        for (int slot = 0; slot < world.CarParks.Rows.SlotCount; slot++)
        {
            if (!world.CarParks.Rows.IsLive(slot))
            {
                continue;
            }

            reading.Spaces += world.CarParks.Capacity[slot];
            reading.Occupied += world.CarParks.Occupied[slot];
        }

        var shed = new ShedScratch();
        var walk = new WalkScratch();

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (!world.Buildings.Rows.IsLive(slot))
            {
                continue;
            }

            Address door = world.PedestrianAccessPoint(slot);

            if (!door.Exists)
            {
                continue;
            }

            reading.Probes++;

            if (!world.TryChooseParking(door, shed, out int carPark))
            {
                // adr/0009's own failure: the shed widened as far as it goes and every Car Park in
                // it was full. It is the CAP's refuting reading and never the radius's, which is why
                // it is counted here rather than folded into the walk distribution as a large value.
                reading.Exhausted++;

                if (RoomBeyondTheCap(world, rules, door))
                {
                    reading.BeyondCap++;
                }

                continue;
            }

            TravelTime cost = WalkRouting.Cost(
                world.Roads,
                TravelMode.Foot,
                door,
                world.CarParks.AddressAt(world.Roads.Segments, carPark),
                rules.Trips.CrossingCost,
                walk);

            if (!cost.IsImpassable)
            {
                reading.Walks.Add(cost.Raw);
            }
        }

        reading.Walks.Sort();

        return reading;
    }

    /// <summary>
    /// Whether a Car Park with room sits inside the radius but past what the cap keeps.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the cap's refuting reading and the exhaustion count alone is not.</b>
    /// <c>plans/0002</c> §D1 names it in both directions: exhaustion of <b>zero</b> across a congested
    /// city means <c>shed_keeps</c> is larger than any driver needs, and <i>a driver running out of
    /// shed while Car Parks remain inside the radius means it is too small and the cap is refusing
    /// supply the radius admitted</i>. A count of exhausted doors cannot tell those apart —
    /// ***a shed that came back empty does not say which of its two bounds stopped it*** — so the
    /// query is run again with a keep wide enough that only the radius can bind.
    /// </para>
    /// <para>
    /// ⚠ <b>A wider keep also walks a wider ball, and that is the point rather than a distortion.</b>
    /// <c>ParkingShed.Expand</c> stops once nothing further out could displace a <em>full</em> kept
    /// set, so a keep this large never fills and the ball runs to the radius — which is exactly the
    /// question being asked.
    /// </para>
    /// </remarks>
    private static bool RoomBeyondTheCap(World world, Ruleset rules, Address door)
    {
        Span<int> wide = stackalloc int[UncappedKeep];
        var scratch = new ShedScratch();

        int kept = ParkingShed.Nearest(
            world.Roads,
            world.CarParks,
            world.CarParksOnSegments,
            door,
            rules.Parking.Radius,
            scratch,
            wide,
            out _);

        for (int i = 0; i < kept; i++)
        {
            if (world.CarParks.SpaceAt(wide[i]) > 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// A keep wide enough that the radius is the only bound left.
    /// </summary>
    /// <remarks>
    /// Milestone 7 task 3 measured the Car Parks a 400 m ball encounters on a generated city:
    /// <b>35</b> at 1,000,000 Citizens and <b>32</b> at 4,000. This is an order above both, so it
    /// cannot fill and cannot trigger the early exit. ***A bound chosen to be inert has to be checked
    /// against the measurement that says what would make it bind.***
    /// </remarks>
    private const int UncappedKeep = 512;

    /// <summary>
    /// <c>minimal.toml</c> with cars, and the parking dial at <paramref name="rung"/>.
    /// </summary>
    /// <remarks>
    /// <b>Built from the shipped file rather than written out here</b>, so the lattice, the Commute
    /// Budget, the walk speed and the shed radius are the ones the project actually ships and not a
    /// fixture's idea of them. Two edits: the <c>[households]</c> table <c>minimal.toml</c> omits by
    /// design, and the one dial being swept.
    /// </remarks>
    private static Ruleset Ruleset(int rung)
    {
        string toml = File.ReadAllText(GoldenFixtures.RulesetPath);

        Assert.Contains("\nparking = 8\n", toml, StringComparison.Ordinal);

        RulesetLoadResult parsed = RulesetLoader.Parse(
            toml.Replace(
                "\nparking = 8\n",
                $"\nparking = {rung.ToString(CultureInfo.InvariantCulture)}\n",
                StringComparison.Ordinal)
            + "\n[households]\ncar_ownership_percent = 100\n",
            "scarcity-sweep.toml");

        Assert.True(parsed.Ok, parsed.Describe());

        return parsed.Ruleset!;
    }

    /// <summary>A walk across the whole shed, which is the only ceiling the arrival walk has.</summary>
    private static TravelTime Ceiling()
    {
        Ruleset rules = Ruleset(8);

        return TravelTime.Over(rules.Parking.Radius, rules.Roads.WalkSpeed);
    }

    /// <summary>Occupancy in hundredths, for the control assertion.</summary>
    private static long Share(Reading reading) =>
        reading.Spaces == 0 ? 0 : IntegerMath.RoundDiv((long)reading.Occupied * 10_000, reading.Spaces);

    private static string Percent(int part, int whole)
    {
        if (whole == 0)
        {
            return "—";
        }

        long tenths = IntegerMath.RoundDiv((long)part * 1_000, whole);

        return $"{tenths / 10}.{tenths % 10}%";
    }

    private static string Minutes(long raw) => Borough.Headless.TripDump.Minutes(raw);

    /// <summary>One rung's answer.</summary>
    private sealed class Reading
    {
        internal int Spaces;

        internal int Occupied;

        internal int Probes;

        internal int Exhausted;

        /// <summary>Exhausted doors that had room inside the radius, past what the cap kept.</summary>
        internal int BeyondCap;

        internal List<long> Walks { get; } = [];

        internal long Longest => Walks.Count == 0 ? 0 : Walks[^1];

        internal long Percentile(int which) =>
            Walks.Count == 0
                ? 0
                : Walks[(int)IntegerMath.RoundDiv((long)(Walks.Count - 1) * which, 100)];
    }
}
