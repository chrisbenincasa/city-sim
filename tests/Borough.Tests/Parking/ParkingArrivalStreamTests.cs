using Borough.Core;
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
/// <b>The stream of Parking Shed queries a real city produces, and what size of cache it wants.</b>
/// </summary>
/// <remarks>
/// <para>
/// A shed's use is <b>arrival</b> (<c>adr/0083</c>), so the workload is not a rate to be assumed — it
/// is a <em>sequence of destinations in time order</em>, which the commute already produces. This
/// captures it and sizes a cache from it, in that order.
/// </para>
/// <para>
/// ⚠ <b>Every number here was measured wrongly at least once, and the errors all pointed the same
/// way — toward a more alarming answer.</b> The six the sweep of 2026-08-18 found are recorded at
/// their sites below: the settle, the world, the warm-up, the dropped arrivals, the single-sample
/// peak, and the ring's own eviction rule. Two published conclusions reversed when they were fixed.
/// <b>A measurement that has never been re-derived is a measurement nobody has checked.</b>
/// </para>
/// </remarks>
[Trait(Tier.Key, Tier.Instrument)]
public sealed class ParkingArrivalStreamTests
{
    /// <summary>Four Days of capture, so a destination recurs and reuse distance is measurable.</summary>
    private const int Captured = 8_192;

    /// <summary>
    /// Four Days run and thrown away before anything is recorded.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>DEFECT 1, and it reversed two conclusions.</b> At Tick 0 nobody is employed, and
    /// <c>EmploymentEngine</c> hires on a <c>revisit_ticks</c> of 1,024 — so a city started from Tick 0
    /// hires in <em>synchronised waves</em> whose members then commute together, to destinations none
    /// of them has visited before. That burst is a fake peak made of compulsory misses, which
    /// simultaneously flatters the peak and defeats any cache. Measured from Tick 0 the cache removed
    /// <b>8%</b> of the worst Tick and a second employing kind made the peak <em>worse</em>; measured
    /// settled the figures are <b>35%</b> and <em>better</em>. This is <c>plans/0026</c> task 8's
    /// finding, which the corpus already had in writing.
    /// <b>Settledness is reported per Day rather than assumed.</b>
    /// </remarks>
    private const int Settle = 8_192;

    /// <summary>Arena sizes in <c>int</c> members. Four bytes each, so 262,144 ints is 1 MiB.</summary>
    private static int[] Arenas => [65_536, 262_144, 1_048_576, 4_194_304];

    /// <summary>The same ladder four rungs further, because the knee moves with the city.</summary>
    private static int[] TargetArenas => [1_048_576, 4_194_304, 16_777_216, 67_108_864];

    /// <summary>
    /// Shed builds to time before dividing, so a small city's reading is not its own start-up.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>DEFECT 3.</b> Timed cold-first, one city read <b>19.77 µs</b> against <b>5.91</b> for the
    /// same work a minute later — and the peak-millisecond column, the one the budget is denominated
    /// in, was comparing a cold run against a warm one. A warm-up pass fixed most of it and not all:
    /// at 4,000 Citizens there are only ~468 distinct destinations, so even the warm pass was
    /// dominated by first-call overhead. The timed pass now repeats until this many builds have
    /// happened, which makes every population's figure comparable.
    /// </remarks>
    private const int TimedBuilds = 20_000;

    private readonly ITestOutputHelper _out;

    public ParkingArrivalStreamTests(ITestOutputHelper output) => _out = output;

    /// <summary>
    /// <b>What the shed query costs a Tick, with the cache and without it.</b>
    /// </summary>
    /// <remarks>
    /// <b>Latency and not hit rate, because latency is the variable a cache exists to move.</b> A hit
    /// rate hides the two things a budget cares about: when the misses land, and how many land
    /// together.
    /// </remarks>
    [Fact]
    public void What_the_shed_query_costs_a_tick_with_the_cache_and_without()
    {
        foreach (int population in new[] { GoldenFixtures.Population, 16_000, 64_000 })
        {
            Report(Capture(population, Driving()), Arenas);
        }
    }

    /// <summary>
    /// <b>What the arena is worth at the population the budget is denominated in.</b>
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A 4 MiB arena comfortably exceeds a 64,000-Citizen working set, so that reading measured a
    /// cache bigger than its city.</b> The ladder here runs to 256 MiB and the whole working set is
    /// printed beside it, because the question is whether the knee sits below what caching everything
    /// would cost. <b>No LRU column</b>: the stack-distance pass is <c>O(arrivals × distinct)</c> and
    /// does not finish at this size, so rotation's near-ideal behaviour is inferred from the 64,000
    /// reading rather than measured here.
    /// </remarks>
    [Fact]
    public void What_the_arena_is_worth_at_a_million_citizens() =>
        Report(Capture(1_000_000, Driving()), TargetArenas);

    /// <summary>
    /// <b>How much of the arrival peak is the city and how much is the Ruleset's land use.</b>
    /// </summary>
    /// <remarks>
    /// <c>adr/0101</c> anchors a commute on the <em>Workplace's</em> Shift start, and the shipped file
    /// declares <b>one</b> employing kind — so every Workplace keeps identical hours and the city
    /// arrives together. The second kind is <em>earlier</em> rather than later:
    /// <c>CommuteLongRunTests</c> measured that direction backwards first, because <b>a Day's quiet end
    /// is bounded by <c>latest start + longest Shift</c></b>, so only an earlier band adds midday
    /// traffic without waking the night. That file's fixture is reused rather than restated.
    /// <para>
    /// <b>Two normalisations, because peak/mean is confounded and share is not.</b> A second kind
    /// brings its own <c>[[zone_rule]]</c> on the same zone and takes Lots from dwellings, so the city
    /// loses commuters — which moves the mean for a reason that has nothing to do with hours.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_arrival_peak_is_land_use_as_much_as_it_is_the_city()
    {
        Stream single = Capture(64_000, Driving());
        Stream mixed = Capture(64_000, WithASecondEmployingKind());

        _out.WriteLine("kinds  arrivals  mean/Tick  p95  p99  peak  peak/mean  peak share  us/shed");

        double singleShare = Peak("one", single);
        double mixedShare = Peak("two", mixed);

        _out.WriteLine(string.Empty);
        _out.WriteLine(
            $"the busiest Tick holds {singleShare:F3}% of a Day's arrivals on one employing kind "
            + $"and {mixedShare:F3}% on two");

        Assert.True(
            single.Arrivals.Count > 0 && mixed.Arrivals.Count > 0,
            "one of the two cities produced no arrivals, so nothing was compared.");
    }

    /// <summary>
    /// <b>What moving the shed query from arrival to Trip creation does to the per-Tick peak.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Milestone 7 task 4 has to query the shed at Trip creation, and this measures what that
    /// costs.</b> <c>adr/0083</c> calls the shed's one caller <em>arrival</em>, and it is right about
    /// the <em>occasion</em> — one query per car journey, at the destination. It is silent about the
    /// <em>instant</em>, and <c>adr/0075</c> settles that instead: every Leg is created at Trip
    /// creation, and the drive Leg's second Address is the Car Park it is driving to, so the Car Park
    /// has to be chosen before the car sets off. The alternative prices the Commute Budget on a
    /// journey missing its last walk, which <c>TripEngine.Start</c> refuses by name — <i>"a person who
    /// can see the journey is too long does not make two thirds of it and stop"</i>.
    /// </para>
    /// <para>
    /// <b>The two streams hold the same queries and differ only in when they fire</b>, so this is not
    /// a cost comparison between two designs — it is the same work, moved. The working set, the shed
    /// sizes and every cache figure in this file are untouched by the move. What moves is the
    /// per-Tick peak, and therefore the Tick budget, which is priced against the peak and not the
    /// mean.
    /// </para>
    /// <para>
    /// ⚠ <b>This does not ratify anything and no number here is hash-bearing.</b> It reports a shape.
    /// The disqualifier is the edge truncation named on <see cref="Departure"/> — a Trip spanning
    /// either boundary appears in one stream and not the other — so the two counts are printed and a
    /// reading is only worth quoting while they are close.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_query_stream_is_smoother_at_creation_than_at_arrival()
    {
        Stream stream = Capture(64_000, Driving());

        int[] atArrival = new int[Captured];
        int[] atCreation = new int[Captured];

        foreach (Arrival arrival in stream.Arrivals)
        {
            atArrival[arrival.Tick]++;
        }

        foreach (Departure departure in stream.Departures)
        {
            atCreation[departure.Tick]++;
        }

        _out.WriteLine($"{stream.Population:N0} Citizens, {stream.MicrosecondsPerShed:F2} us a shed");
        _out.WriteLine(string.Empty);
        _out.WriteLine("fired at   queries   mean/Tick   p95   p99   peak   peak/mean   ms at peak");

        int arrivalPeak = Spread("arrival", atArrival, stream);
        int creationPeak = Spread("creation", atCreation, stream);

        _out.WriteLine(string.Empty);
        _out.WriteLine(
            $"moving the query to Trip creation multiplies the peak by "
            + $"{(double)creationPeak / Math.Max(1, arrivalPeak):F2}x");

        Assert.True(
            stream.Arrivals.Count > 0 && stream.Departures.Count > 0,
            "one of the two streams was empty, so nothing was compared.");
    }

    /// <summary>One row of the comparison: a per-Tick count, described and priced.</summary>
    private int Spread(string when, int[] perTick, Stream stream)
    {
        double mean = Mean(perTick);
        int peak = perTick.Max();

        _out.WriteLine(
            $"{when,-10} {perTick.Sum(),-9} {mean,-11:F2} {Quantile(perTick, 0.95),-5} "
            + $"{Quantile(perTick, 0.99),-5} {peak,-6} "
            + $"{peak / Math.Max(0.01, mean),-11:F1} "
            + $"{peak * stream.MicrosecondsPerShed / 1000.0:F3}");

        return peak;
    }

    /// <summary>
    /// <b>How the ideal cache compares</b>, so the rotation's own cost is separable from the workload's.
    /// </summary>
    [Fact]
    public void Rotation_costs_little_against_an_ideal_cache_of_the_same_memory()
    {
        Stream stream = Capture(64_000, Driving());
        long[] ideal = StackDistances(stream);

        _out.WriteLine("arena(MiB)  ring hit%  LRU hit%  gap");

        foreach (int arena in Arenas)
        {
            double rotation = Ring(stream, arena, new int[Captured]);
            double best = Share(ideal, arena);

            _out.WriteLine(
                $"{arena * 4 / 1024.0 / 1024.0,-11:F1} {100.0 * rotation,-10:F1} "
                + $"{100.0 * best,-9:F1} {100.0 * (best - rotation):F1}");
        }
    }

    /// <summary>Prints one city's latency table across an arena ladder.</summary>
    private void Report(Stream stream, int[] arenas)
    {
        int[] perTick = new int[Captured];

        foreach (Arrival arrival in stream.Arrivals)
        {
            perTick[arrival.Tick]++;
        }

        double workingSet = (double)stream.Sheds.Values.Sum() * 4 / 1024 / 1024;

        _out.WriteLine(
            $"population {stream.Population}: {stream.Arrivals.Count} arrivals over {Captured} Ticks "
            + $"after {Settle} settling, {stream.Sheds.Count} distinct destinations, "
            + $"{stream.MicrosecondsPerShed:F2} us/shed, working set {workingSet:F0} MiB");

        _out.WriteLine($"  arrivals by captured Day: {PerDay(stream)}   dropped: {stream.Dropped}");
        _out.WriteLine(
            $"  arrivals/Tick  mean {Mean(perTick):F2}  p95 {Quantile(perTick, 0.95)}  "
            + $"p99 {Quantile(perTick, 0.99)}  p999 {Quantile(perTick, 0.999)}  peak {perTick.Max()}");
        _out.WriteLine("  arena(MiB)   no cache mean/p99/peak ms     cached mean/p99/peak ms    hit%");

        foreach (int arena in arenas)
        {
            int[] misses = new int[Captured];
            double hits = Ring(stream, arena, misses);

            _out.WriteLine(
                $"  {arena * 4 / 1024.0 / 1024.0,-12:F1} {Milliseconds(perTick, stream),-27} "
                + $"{Milliseconds(misses, stream),-26} {100.0 * hits:F1}");
        }

        _out.WriteLine(string.Empty);
    }

    /// <summary>Prints one city's arrival distribution and returns its peak share.</summary>
    private double Peak(string kinds, Stream stream)
    {
        int[] perTick = new int[Captured];

        foreach (Arrival arrival in stream.Arrivals)
        {
            perTick[arrival.Tick]++;
        }

        double mean = Mean(perTick);
        int peak = perTick.Max();

        _out.WriteLine(
            $"{kinds,-6} {stream.Arrivals.Count,-9} {mean,-10:F2} {Quantile(perTick, 0.95),-4} "
            + $"{Quantile(perTick, 0.99),-4} {peak,-5} {peak / Math.Max(0.01, mean),-10:F1} "
            + $"{100.0 * peak / Math.Max(1, stream.Arrivals.Count),-11:F3} "
            + $"{stream.MicrosecondsPerShed:F2}");

        return 100.0 * peak / Math.Max(1, stream.Arrivals.Count);
    }

    /// <summary>
    /// The <b>rotating arena</b>: members appended at a wrapping cursor, an entry dead the moment the
    /// cursor laps its first member. Returns the hit rate and fills <paramref name="misses"/> per Tick.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Denominated in members and not in entries</b>, which is why it is simulated rather than
    /// reasoned about: a fixed-way cache would need a per-entry cap, and a shed runs 35 to 166 members
    /// at the shipped radius.
    /// </para>
    /// <para>
    /// ⚠ <b>DEFECT 6, twice over.</b> The first cut derived one age array and read every arena size off
    /// it, which advanced the cursor on <em>hits</em> as well as misses — the behaviour of an arena too
    /// small to hit at all, so every size was under-reported. And its validity test asked
    /// <c>written − at ≤ arena − size</c>, demanding a whole shed of headroom that the ring does not
    /// need: an entry at <c>at</c> dies when the cursor laps its <em>first</em> member, which is
    /// <c>written − at ≤ arena</c>. Simulated per size now, exactly.
    /// </para>
    /// </remarks>
    private static double Ring(Stream stream, int arena, int[] misses)
    {
        Dictionary<Destination, long> writtenAt = [];
        long written = 0;
        int hits = 0;

        foreach ((int tick, Destination where) in stream.Arrivals)
        {
            if (writtenAt.TryGetValue(where, out long at) && written - at <= arena)
            {
                hits++;
                continue;
            }

            misses[tick]++;
            writtenAt[where] = written;
            written += stream.Sheds[where];
        }

        return stream.Arrivals.Count == 0 ? 0 : (double)hits / stream.Arrivals.Count;
    }

    /// <summary>
    /// For each arrival, <b>the members of the distinct destinations touched since it was last
    /// used</b> — the textbook stack distance, with the stack denominated in members.
    /// </summary>
    private static long[] StackDistances(Stream stream)
    {
        List<Destination> recency = [];
        long[] distances = new long[stream.Arrivals.Count];

        for (int i = 0; i < stream.Arrivals.Count; i++)
        {
            Destination arrival = stream.Arrivals[i].Where;
            int at = recency.IndexOf(arrival);

            if (at < 0)
            {
                distances[i] = long.MaxValue;
            }
            else
            {
                long ahead = stream.Sheds[arrival];

                for (int j = 0; j < at; j++)
                {
                    ahead += stream.Sheds[recency[j]];
                }

                distances[i] = ahead;
                recency.RemoveAt(at);
            }

            recency.Insert(0, arrival);
        }

        return distances;
    }

    /// <summary>What share of the stream falls inside an arena of <paramref name="arena"/> members.</summary>
    private static double Share(long[] distances, int arena)
    {
        int within = 0;

        foreach (long distance in distances)
        {
            if (distance <= arena)
            {
                within++;
            }
        }

        return distances.Length == 0 ? 0 : (double)within / distances.Length;
    }

    /// <summary>Runs a city and records every Trip destination in the order the Trips <b>ended</b>.</summary>
    /// <remarks>
    /// <para>
    /// <b>Ended rather than created.</b> A Trip is made at departure and queries the shed at arrival,
    /// so a capture keyed on creation would hold the right destinations in the wrong order — and cache
    /// locality is a property of order alone.
    /// </para>
    /// <para>
    /// ⚠ <b>DEFECT 4 was a silent filter.</b> A Trip whose destination Segment no longer resolves was
    /// skipped without trace, so an unknown number of arrivals left the measurement quietly. They are
    /// still skipped — a severed Address has no shed — but they are <b>counted and printed</b>, because
    /// a drop nobody can see is indistinguishable from a workload that is genuinely smaller.
    /// </para>
    /// <para>
    /// ⚠ <b>DEFECT 2 turned the Decide guard on for every Tick.</b> Built through <c>Replay.Start</c>,
    /// <c>VerifyDecideWritesNothing</c> defaults on, and it is <c>O(world)</c> — 76.4 ms a Tick at 1M,
    /// which is ~95% of a run. It corrupts nothing (the shed cost is timed separately) and it cost a
    /// 1M capture twenty minutes it did not need. <c>CommuteLongRunTests.RunToSettled</c> turns it off
    /// and this now follows it.
    /// </para>
    /// </remarks>
    private static Stream Capture(int population, Ruleset rules)
    {
        var key = WorldKey.FromSeed(GoldenFixtures.Seed);
        var world = new World(population, rules, key);
        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        Dictionary<ulong, Destination> inFlight = [];
        List<Arrival> arrivals = [];
        List<Departure> departures = [];
        HashSet<ulong> live = [];
        List<ulong> ended = [];
        int dropped = 0;

        for (int tick = 0; tick < Settle + Captured; tick++)
        {
            simulation.Step(default);

            live.Clear();

            TripTable trips = world.Trips;

            for (int slot = 0; slot < trips.Rows.SlotCount; slot++)
            {
                if (!trips.Rows.IsLive(slot))
                {
                    continue;
                }

                ulong id = trips.Rows.IdAt(slot);

                live.Add(id);

                if (inFlight.ContainsKey(id))
                {
                    continue;
                }

                if (world.Roads.Segments.Rows.TryResolve(
                    trips.DestinationSegment[slot], out int segment))
                {
                    Destination where = new(
                        segment, trips.DestinationOffset[slot].Raw, trips.DestinationSide[slot]);

                    inFlight[id] = where;

                    // The Tick a Trip is first seen is the Tick it was created on, which this loop
                    // has always known and thrown away. It is the other candidate query stream --
                    // see Departure -- and capturing it costs one list.
                    if (tick >= Settle)
                    {
                        departures.Add(new Departure(tick - Settle, where));
                    }
                }
                else if (tick >= Settle)
                {
                    dropped++;
                }
            }

            ended.Clear();

            foreach ((ulong id, Destination where) in inFlight)
            {
                if (!live.Contains(id))
                {
                    if (tick >= Settle)
                    {
                        arrivals.Add(new Arrival(tick - Settle, where));
                    }

                    ended.Add(id);
                }
            }

            foreach (ulong id in ended)
            {
                inFlight.Remove(id);
            }
        }

        Dictionary<Destination, int> sheds = ShedSizes(world, arrivals, out double microseconds);

        return new Stream(population, arrivals, departures, sheds, microseconds, dropped);
    }

    /// <summary>How big each distinct destination's shed is, and what one costs to build.</summary>
    private static Dictionary<Destination, int> ShedSizes(
        World world, List<Arrival> arrivals, out double microseconds)
    {
        CarParkResidency residency = new();
        residency.Rebuild(world.CarParks, world.Roads.Segments);

        ShedScratch scratch = new();
        int[] into = new int[8];
        Tiles radius = world.Rules.Parking.Radius;

        Dictionary<Destination, int> sizes = [];

        foreach ((_, Destination arrival) in arrivals)
        {
            if (!sizes.ContainsKey(arrival))
            {
                sizes[arrival] = Fill(world, residency, scratch, into, radius, arrival);
            }
        }

        var clock = System.Diagnostics.Stopwatch.StartNew();
        long built = 0;

        while (built < TimedBuilds && sizes.Count > 0)
        {
            foreach ((Destination arrival, _) in sizes)
            {
                Fill(world, residency, scratch, into, radius, arrival);
            }

            built += sizes.Count;
        }

        clock.Stop();

        microseconds = built == 0 ? 0 : clock.Elapsed.TotalMilliseconds * 1000 / built;

        return sizes;
    }

    /// <summary>One shed query, shared by the warm-up pass and the timed one.</summary>
    private static int Fill(
        World world,
        CarParkResidency residency,
        ShedScratch scratch,
        int[] into,
        Tiles radius,
        Destination arrival)
    {
        Address door = Address.On(
            arrival.Segment, new Tiles(arrival.Offset), (StreetSide)arrival.Side);

        ParkingShed.Nearest(
            world.Roads, world.CarParks, residency, door, radius, scratch, into, out int found);

        // A destination with no parking in reach still occupies a cache entry: the answer "none" is
        // as worth keeping as any other, and an entry of zero members would never be evicted.
        return Math.Max(1, found);
    }

    /// <summary>
    /// The shipped Ruleset with everybody driving.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>DEFECT 5, and it is the one that would have invalidated the whole file.</b>
    /// <c>minimal.toml</c> states no <c>[households]</c> table, so <c>car_ownership_percent</c> is
    /// absent and <b>nobody drives</b> (<c>adr/0098</c>) — which means the shipped fixture produces
    /// <em>zero</em> shed queries and every arrival measured on it is a pedestrian who never parks.
    /// One key rather than adopting <c>congested.toml</c>, whose <c>street_capacity_per_hour = 400</c>
    /// makes the Streets absurd on purpose and would have changed the city as well as the mode. This
    /// is <c>adr/0052</c>'s amendment applied to an instrument: <b>a measurement names a machine, a
    /// world and a quantity</b>.
    /// </remarks>
    private static Ruleset Driving() => Load(
        File.ReadAllText(GoldenFixtures.RulesetPath)
        + "\n\n[households]\ncar_ownership_percent = 100\n");

    /// <summary>
    /// <c>CommuteLongRunTests</c>' two-kind city, driving, with the second kind parking as well.
    /// </summary>
    private static Ruleset WithASecondEmployingKind()
    {
        // 🔴 ANCHORED ON THE BUILDING'S NAME LINE, AND IT WAS THE SHIFT BAND UNTIL 2026-08-26. The band
        // moved out of [[building]] and into [[business]] with milestone 26 task 2's land-use split
        // (adr/0149), so a replace-all put `parking` into the TRADE table as well -- where it is not a
        // key, and the loader refuses it. Parking is a property of PREMISES, so the building's own name
        // is the line to hang it off, and `name = "workshop_trade"` does not contain it: the closing
        // quote is part of the anchor.
        //
        // ⚠ THE OLD CODE ASSERTED Contains AND THAT IS WHY THIS SURVIVED THE SPLIT. Contains passes on
        // two occurrences exactly as it passes on one, so the guard was blind to the only thing that
        // could go wrong with a replace-all. It counts now.
        const string anchor = "name = \"workshop\"";

        string toml = Movement.CommuteLongRunTests.SecondKindToml(3, 6);

        Assert.Equal(
            1,
            toml.Split(anchor, StringSplitOptions.None).Length - 1);

        return Load(
            toml.Replace(anchor, anchor + "\nparking = 8", StringComparison.Ordinal)
            + "\n\n[households]\ncar_ownership_percent = 100\n");
    }

    private static Ruleset Load(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    /// <summary>Arrivals in each captured Day — <b>the evidence that the settle was long enough</b>.</summary>
    private static string PerDay(Stream stream)
    {
        int[] counts = new int[Math.Max(1, Captured / Ticks.PerDay)];

        foreach (Arrival arrival in stream.Arrivals)
        {
            counts[Math.Min(counts.Length - 1, arrival.Tick / Ticks.PerDay)]++;
        }

        return string.Join(" / ", counts);
    }

    /// <summary>The mean, p99 and peak of a per-Tick count, priced at this run's own shed cost.</summary>
    private static string Milliseconds(int[] counts, Stream stream)
    {
        double us = stream.MicrosecondsPerShed / 1000.0;

        return $"{Mean(counts) * us:F3} / {Quantile(counts, 0.99) * us:F3} / {counts.Max() * us:F3}";
    }

    private static double Mean(int[] counts) =>
        counts.Length == 0 ? 0 : (double)counts.Sum() / counts.Length;

    /// <summary>
    /// A quantile of a per-Tick count.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>DEFECT 5's sibling: a maximum is a statistic that grows with the sample.</b>
    /// <c>plans/0023</c> task 8 found a peak test measuring its own sample size, so p99 and p999 are
    /// printed beside the peak — <b>a peak that is far above p999 is one Tick and not a regime</b>.
    /// </remarks>
    private static int Quantile(int[] counts, double quantile)
    {
        int[] sorted = [.. counts];

        Array.Sort(sorted);

        return sorted[Math.Min(sorted.Length - 1, (int)(sorted.Length * quantile))];
    }

    /// <summary>A shed query's key: the Address a driver is arriving at.</summary>
    private readonly record struct Destination(int Segment, int Offset, byte Side);

    /// <summary>One shed query: where, and <b>on which Tick</b>.</summary>
    private readonly record struct Arrival(int Tick, Destination Where);

    /// <summary>
    /// The same shed query, timed at Trip <b>creation</b> instead of at arrival.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The destination set is identical to <see cref="Arrival"/>'s and only the Tick differs</b>,
    /// which is the whole question. Milestone 7 task 4 queries the shed when the Trip is built rather
    /// than when the car lands, because <c>adr/0075</c> creates every Leg at Trip creation and the
    /// drive Leg has to be routed to the Car Park it is heading for — so the working set, the shed
    /// sizes and the cache's hit rate are unchanged by the choice and <em>only the per-Tick peak
    /// moves</em>.
    /// </para>
    /// <para>
    /// <b>The two streams are predicted to differ, and the mechanism is written down rather than
    /// guessed.</b> <c>adr/0101</c> anchors a commute on the <em>Workplace's</em> Shift start, and
    /// <c>CommuteRoster.TryTimes</c> derives the outbound Tick as
    /// <c>start − planned − early</c> — so a city's arrivals share one clock while its departures are
    /// spread by each Citizen's own journey length. ***A stream anchored on a shared instant is
    /// peakier than the same stream anchored on that instant minus a distribution.***
    /// </para>
    /// <para>
    /// ⚠ <b>Both streams are truncated at the window's edges and in opposite directions.</b> A Trip
    /// departing before <see cref="Settle"/> and arriving after it is an arrival with no departure;
    /// one departing near the end of <see cref="Captured"/> is a departure with no arrival. The bias
    /// is bounded by the longest journey against a four-Day window, and the counts are printed so it
    /// is visible rather than assumed.
    /// </para>
    /// </remarks>
    private readonly record struct Departure(int Tick, Destination Where);

    /// <summary>The captured stream, the size of every shed in it, and what one cost to build.</summary>
    private sealed record Stream(
        int Population,
        List<Arrival> Arrivals,
        List<Departure> Departures,
        Dictionary<Destination, int> Sheds,
        double MicrosecondsPerShed,
        int Dropped);
}
