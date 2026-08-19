using System.Diagnostics;
using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Parking;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Golden;
using Xunit.Abstractions;

namespace Borough.Tests.Parking;

/// <summary>
/// <b>Where a Parking Shed query's microseconds go, and how much of the answer is the order the
/// queries arrive in.</b>
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ParkingShedSizeTests"/> prices a shed at <b>one number</b> and
/// <see cref="ParkingArrivalStreamTests"/> multiplies that number by a per-Tick arrival count to get a
/// millisecond bill. Both are silent about two things a bill needs: <b>which part of the query the
/// microseconds are in</b>, and <b>whether a shed costs the same when the city arrives at 3,438 scattered
/// destinations at once as it does in a loop over the same few thousand</b>. This file measures both,
/// and it exists because the second one is an assumption the peak figure rests on entirely.
/// </para>
/// <para>
/// <b>The decomposition is differential rather than instrumented</b> — four runs of the real
/// <see cref="ParkingShed.Nearest"/> over the same doors, each with one term of its work suppressed by
/// an argument, and the parts read off the differences. A counter inside the query would price the
/// counter as well as the work, and this query is small enough that it would matter: the whole thing is
/// a few thousand nanoseconds.
/// </para>
/// <list type="bullet">
/// <item><b>frame</b> — radius 0 and an empty index: <see cref="ShedScratch.Begin"/> and the write out.</item>
/// <item><b>ball</b> — the shipped radius and an empty index: the frame, the Dijkstra ball, and the
/// touched-Segment loop finding nothing on every Segment it looks at.</item>
/// <item><b>own</b> — radius 0 and the real index: the frame and the door's own Street, which is walked
/// along rather than around and is therefore outside the ball by construction.</item>
/// <item><b>reach</b> — the shipped radius, the real index, and a kept set of <b>zero</b>: the residency
/// walk over every touched Segment and the two-endpoint distance for each Car Park on it, with the
/// ordering suppressed because <see cref="ShedScratch.Offer"/> returns on its first line when nothing is
/// kept.</item>
/// <item><b>offer</b> — what is left of the full query: the insertion sort that holds the nearest few.</item>
/// </list>
/// <para>
/// <b><c>reach</c> and <c>offer</c> are split because the repair differs.</b> If the microseconds are in
/// <c>reach</c> the query is walking Car Parks it will never keep, and the fix is to stop the ball at the
/// distance of the last one it would keep; if they are in <c>offer</c> the query is <em>ordering</em> Car
/// Parks it will never keep, and the fix is a cheaper rejection. The two are indistinguishable in a
/// single <c>supply</c> column, which is what the first cut of this file printed.
/// </para>
/// <para>
/// ⚠ <b>The empty index suppresses the supply term without changing the ball</b>, because
/// <see cref="CarParkResidency.Any"/> is the first thing both residency loops ask and a
/// <see cref="CarParkResidency"/> rebuilt against a Car Park table with no live rows answers <c>false</c>
/// for every Segment. The graph, the radius, the door set and the scratch are the ones the real query
/// uses. Nothing here re-implements any part of <see cref="ParkingShed"/>.
/// </para>
/// <para>
/// ⚠⚠ <b>THE DECOMPOSITION IS OF THE EXHAUSTIVE QUERY, AND IT HAS TO BE — the differential technique
/// stopped being valid the moment the ball learned to stop early, and it broke in the act of proving
/// itself.</b> Suppressing a term by an argument is sound only while the terms are <em>independent</em>.
/// <see cref="ParkingShed.Expand"/>'s exit fires on <see cref="ShedScratch.KeptFull"/>, so the two
/// suppressions each disable it as a side effect: an empty index keeps nothing, and a zero-length
/// <c>into</c> keeps nothing. Both variants therefore walk the <em>whole</em> radius while the full
/// query stops short, and subtracting one algorithm from another is meaningless. Measured, the columns
/// came back at <b>reach −3.84 and offer −4.40 µs</b>.
/// <para>
/// <b>Negative was the lucky outcome.</b> A weaker coupling would have left them positive and
/// plausible and nobody would have looked. The repair is to decompose with a kept set that never fills
/// — <see cref="Wide"/> — so every variant walks the same ball, and to print the capped total beside it
/// as a separate column rather than as a fifth part of the same sum. <b>An optimisation that makes one
/// term depend on another retires a differential instrument silently</b>, and the instrument goes on
/// printing.
/// </para>
/// </para>
/// <para>
/// ⚠ <b>The order sweep is the point of the file, not a coda.</b> A sweep in Building slot order is
/// spatially coherent — consecutive Buildings share Segments — so it reads a shed's neighbourhood out of
/// cache that the Building before it loaded. A rush hour is the opposite: every arrival is somewhere
/// else. <c>ParkingArrivalStreamTests</c>' <c>TimedBuilds</c> loop repeats its destination set until
/// 20,000 builds have happened, so a small city's figure is warm after dozens of passes and a
/// 1,000,000-Citizen city's is a <em>single</em> pass, because it has more distinct destinations than
/// that. <b>The populations in that table are therefore not measured the same way</b>, and this prints
/// all three orders side by side so the difference is a column rather than a hazard.
/// </para>
/// </remarks>
[Trait(Tier.Key, Tier.Instrument)]
public sealed class ParkingShedCostTests
{
    /// <summary>What the caller keeps in production. <see cref="ParkingShedSizeTests"/>' cap.</summary>
    private const int Keep = 8;

    /// <summary>
    /// A kept set too large to fill, so the ball never exits early and the four parts are comparable.
    /// </summary>
    /// <remarks>
    /// See the ⚠⚠ paragraph above. This is what makes the decomposition a decomposition of one
    /// algorithm rather than a subtraction across two.
    /// </remarks>
    private const int Wide = 4_096;

    /// <summary>Long enough for the Zone Rules to raise a city. <see cref="ParkingShedSizeTests"/>' number.</summary>
    private const int Raised = 1_024;

    /// <summary>The population every memory figure in this corpus is denominated in.</summary>
    private const int Target = 1_000_000;

    /// <summary>How many doors the repeated pass cycles over, and how many times.</summary>
    /// <remarks>
    /// <b>A prefix rather than a sample</b>, so the repeated pass is coherent <em>and</em> small: the
    /// figure it is standing in for is "the same query asked again", and a set spread across the city
    /// would be measuring the city's size instead.
    /// </remarks>
    private const int Repeated = 2_000;

    private const int Repeats = 20;

    /// <summary>
    /// The floor on how many queries a timed decomposition pass runs, whatever the city's size.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The first cut of this file read 20.88 µs at 4,000 Citizens against 3–5 at every larger
    /// one, and that row was not a measurement.</b> A 4,000-Citizen city has 338 doors, so a pass over
    /// it is ~14 ms of work — <em>below the tiered-compilation delay</em> — so the row was timed
    /// against unoptimised code however many warm-up passes preceded it. This is
    /// <c>ParkingArrivalStreamTests</c>' <b>DEFECT 3</b> arriving a third time, in a file written by
    /// somebody who had just read the other two. <b>A warm-up whose length is a count of passes is not
    /// a warm-up</b>: what the runtime counts is calls, so the floor is denominated in queries.
    /// <para>
    /// The consequence is stated rather than hidden — every column of the decomposition is a
    /// <em>warm</em> figure, and the order table below is where the single-pass reading lives.
    /// </para>
    /// </remarks>
    private const int TimedQueries = 20_000;

    private readonly ITestOutputHelper _out;

    public ParkingShedCostTests(ITestOutputHelper output) => _out = output;

    /// <summary>
    /// <b>The decomposition and the order sweep, on one world per population.</b>
    /// </summary>
    /// <remarks>
    /// One <see cref="Fact"/> rather than two because a 1,000,000-Citizen world costs minutes to raise
    /// and both readings are taken off the same one.
    /// </remarks>
    [Fact]
    public void Where_a_shed_querys_microseconds_go()
    {
        List<string> parts = [];
        List<string> exits = [];
        List<string> orders = [];

        foreach (int population in new[] { GoldenFixtures.Population, 16_000, 64_000, Target })
        {
            World world = Build(population);
            Address[] doors = Doors(world);

            Tiles radius = world.Rules.Parking.Radius;
            Tiles none = new(0);

            CarParkResidency real = new();
            real.Rebuild(world.CarParks, world.Roads.Segments);

            CarParkResidency empty = new();
            empty.Rebuild(new CarParkTable(1, world.Buildings, world.Roads.Segments), world.Roads.Segments);

            ShedScratch scratch = new();

            int[] into = new int[Keep];
            int[] wide = new int[Wide];
            int[] nothing = [];

            // Once through everything before anything is timed: ShedScratch grows to the largest graph
            // it has seen and the JIT has not seen Nearest at all, so the first pass in a world would
            // otherwise be charged to whichever variant ran first. ParkingArrivalStreamTests' DEFECT 3.
            Pass(world, real, scratch, wide, radius, doors);
            Pass(world, empty, scratch, wide, radius, doors);
            Pass(world, real, scratch, into, radius, doors);

            Shape whole = Shape.Of(world, real, scratch, wide, radius, doors);
            Shape stopped = Shape.Of(world, real, scratch, into, radius, doors);

            // Every variant here keeps more than any radius holds, so none of them ever satisfies
            // KeptFull and all of them walk the same ball. That is the whole point -- see the second
            // warning on the class.
            double frame = Pass(world, empty, scratch, wide, none, doors);
            double ball = Pass(world, empty, scratch, wide, radius, doors);
            double own = Pass(world, real, scratch, wide, none, doors);
            double reach = Pass(world, real, scratch, nothing, radius, doors);
            double exhaustive = Pass(world, real, scratch, wide, radius, doors);
            double capped = Pass(world, real, scratch, into, radius, doors);

            parts.Add(
                $"{population,-11} {doors.Length,-8} {whole.Settled,-8:F1} {whole.Touched,-8:F1} "
                + $"{whole.Walked,-7:F1} {whole.Found,-7:F1} "
                + $"{frame,-7:F2} {ball - frame,-7:F2} {own - frame,-7:F2} "
                + $"{reach - ball - (own - frame),-7:F2} {exhaustive - reach,-7:F2} {exhaustive,-7:F2}");

            exits.Add(
                $"{population,-11} {doors.Length,-8} {whole.Settled,-8:F1} {stopped.Settled,-8:F1} "
                + $"{whole.Walked,-7:F1} {stopped.Walked,-7:F1} "
                + $"x{whole.Walked / Math.Max(0.1, stopped.Walked),-9:F2} {capped,-7:F2}");

            orders.Add(Order(world, real, scratch, into, radius, doors, population));
        }

        _out.WriteLine("THE EXHAUSTIVE QUERY, DECOMPOSED — every variant keeps more than the radius holds,");
        _out.WriteLine("so no variant exits early and the five parts sum to the whole.");
        _out.WriteLine("population  doors    settled  touched  walked  found   "
            + "frame   ball    own     reach   offer   total   (us/shed)");

        foreach (string line in parts)
        {
            _out.WriteLine(line);
        }

        _out.WriteLine(string.Empty);
        _out.WriteLine("WHAT THE EARLY EXIT BUYS — the same doors, keeping everything against keeping 8.");
        _out.WriteLine("⚠ WORK, NOT PRICE. There is no honest 'before' microsecond here and the column is");
        _out.WriteLine("  omitted rather than printed wrong: an exhaustive ball needs an uncapped kept set,");
        _out.WriteLine("  Offer sorts by insertion, so the exhaustive total carries O(found^2) bookkeeping");
        _out.WriteLine("  no shipped query pays -- 7.98 of 13.99 us at 1M. One `keep` argument drives both");
        _out.WriteLine("  the cap and the exit, so the two cannot be separated from outside this API.");
        _out.WriteLine("  THE END-TO-END GAIN IS x3.4 (4.30 -> 1.28 us at 1M), measured across two builds");
        _out.WriteLine("  at the SAME cap of 8, differing only in whether the ball could stop.");
        _out.WriteLine("population  doors    settled(all/8)    walked(all/8)    work gain  us(capped)");

        foreach (string line in exits)
        {
            _out.WriteLine(line);
        }

        _out.WriteLine(string.Empty);
        _out.WriteLine("ORDER — one pass each, capped, because a rush hour is scattered and asks once.");
        _out.WriteLine("population  doors    coherent  scattered  repeated  scattered/repeated (us/shed)");

        foreach (string line in orders)
        {
            _out.WriteLine(line);
        }
    }

    /// <summary>The same doors in slot order, shuffled, and a small prefix asked over and over.</summary>
    private static string Order(
        World world,
        CarParkResidency residency,
        ShedScratch scratch,
        int[] into,
        Tiles radius,
        Address[] doors,
        int population)
    {
        Address[] scattered = [.. doors];

        Scatter(scattered);

        double tight = OnePass(world, residency, scratch, into, radius, doors);
        double loose = OnePass(world, residency, scratch, into, radius, scattered);

        Address[] few = doors.Length <= Repeated ? doors : doors[..Repeated];
        var clock = Stopwatch.StartNew();

        for (int i = 0; i < Repeats; i++)
        {
            Sweep(world, residency, scratch, into, radius, few);
        }

        clock.Stop();

        double warm = clock.Elapsed.TotalMilliseconds * 1000 / ((long)few.Length * Repeats);

        return $"{population,-11} {doors.Length,-8} {tight,-9:F2} {loose,-10:F2} {warm,-9:F2} "
            + $"x{loose / Math.Max(0.001, warm):F2}";
    }

    /// <summary>One timed pass over every door, in the order given.</summary>
    private static double Pass(
        World world,
        CarParkResidency residency,
        ShedScratch scratch,
        int[] into,
        Tiles radius,
        Address[] doors)
    {
        if (doors.Length == 0)
        {
            return 0;
        }

        var clock = Stopwatch.StartNew();
        long asked = 0;

        while (asked < TimedQueries)
        {
            Sweep(world, residency, scratch, into, radius, doors);

            asked += doors.Length;
        }

        clock.Stop();

        return clock.Elapsed.TotalMilliseconds * 1000 / asked;
    }

    /// <summary>One pass and one only, so the order it walks the doors in is what is being read.</summary>
    private static double OnePass(
        World world,
        CarParkResidency residency,
        ShedScratch scratch,
        int[] into,
        Tiles radius,
        Address[] doors)
    {
        var clock = Stopwatch.StartNew();

        Sweep(world, residency, scratch, into, radius, doors);

        clock.Stop();

        return doors.Length == 0 ? 0 : clock.Elapsed.TotalMilliseconds * 1000 / doors.Length;
    }

    private static void Sweep(
        World world,
        CarParkResidency residency,
        ShedScratch scratch,
        int[] into,
        Tiles radius,
        Address[] doors)
    {
        foreach (Address door in doors)
        {
            ParkingShed.Nearest(
                world.Roads, world.CarParks, residency, door, radius, scratch, into, out _);
        }
    }

    /// <summary>Every live Building's pedestrian Access Point, in slot order.</summary>
    private static Address[] Doors(World world)
    {
        List<Address> doors = [];

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot))
            {
                doors.Add(world.AccessPoint(slot, TravelMode.Foot));
            }
        }

        return [.. doors];
    }

    /// <summary>A deterministic Fisher-Yates, so the scattered order is the same on every machine.</summary>
    /// <remarks>
    /// <b>xorshift rather than <c>System.Random</c></b>, whose sequence is not contracted across
    /// runtimes — a shuffle that differs between machines makes the column below unrepeatable, which is
    /// the one property a cache-order measurement needs.
    /// </remarks>
    private static void Scatter(Address[] doors)
    {
        ulong state = 0x9E37_79B9_7F4A_7C15UL;

        for (int i = doors.Length - 1; i > 0; i--)
        {
            state ^= state << 13;
            state ^= state >> 7;
            state ^= state << 17;

            int j = (int)(state % (ulong)(i + 1));

            (doors[i], doors[j]) = (doors[j], doors[i]);
        }
    }

    private static World Build(int population)
    {
        InputLogBuilder builder = new(
            GoldenFixtures.Seed,
            new WorldConfiguration(population),
            GoldenFixtures.RulesetHash);

        builder.Append(new Ticks(0), new Command(CommandKind.Populate, default, default));

        InputLog log = builder.Build();
        Simulation simulation = Replay.Start(log, GoldenFixtures.Rules());

        // Stepped rather than started: a Building is a Zone Rule's output and a Car Park is provisioned
        // at CreateBuilding, so a world at Tick 0 has neither. ParkingShedSizeTests says so at length.
        Replay.Trace(simulation, log, new Ticks(Raised), Raised, []);

        return simulation.World;
    }

    /// <summary>How much work one shed does, counted rather than timed.</summary>
    private readonly record struct Shape(double Settled, double Touched, double Walked, double Found)
    {
        /// <summary>
        /// Counted on a pass of its own, after the query rather than inside it.
        /// </summary>
        /// <remarks>
        /// <b><c>walked</c> is the count the query does not report and the one the supply term is
        /// proportional to</b> — <c>found</c> counts the Car Parks inside the radius and the walk visits
        /// every Car Park on every touched Segment, including the ones past the edge. The gap between
        /// the two is work done to reject.
        /// </remarks>
        public static Shape Of(
            World world,
            CarParkResidency residency,
            ShedScratch scratch,
            int[] into,
            Tiles radius,
            Address[] doors)
        {
            long settled = 0;
            long touched = 0;
            long walked = 0;
            long found = 0;

            foreach (Address door in doors)
            {
                ParkingShed.Nearest(
                    world.Roads, world.CarParks, residency, door, radius, scratch, into, out int inRange);

                settled += scratch.Settled;
                touched += scratch.Touched.Length;
                found += inRange;

                foreach (int segment in scratch.Touched)
                {
                    if (segment == door.Segment || !residency.Any(segment))
                    {
                        continue;
                    }

                    IndexListWalk walk = residency.On(segment, world.CarParks);

                    while (walk.MoveNext())
                    {
                        walked++;
                    }
                }
            }

            int n = Math.Max(1, doors.Length);

            return new Shape((double)settled / n, (double)touched / n, (double)walked / n, (double)found / n);
        }
    }
}
