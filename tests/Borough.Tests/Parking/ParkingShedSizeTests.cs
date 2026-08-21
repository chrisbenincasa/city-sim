using System.Diagnostics;
using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Parking;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;
using Borough.Tests.Golden;
using Xunit.Abstractions;

namespace Borough.Tests.Parking;

/// <summary>
/// <b>How big a Parking Shed actually is, on the city this project generates.</b>
/// </summary>
/// <remarks>
/// <para>
/// Task 3 has to choose how a shed is stored, and the corpus offers one number to choose with —
/// S2 R5.6's <b>110 at 400 m against 596 at 800 m</b>, quoted by <c>adr/0083</c>, <c>plans/0002</c> §D2
/// and <c>rulesets/minimal.toml</c>'s own header. ⚠ <b>That number is not the size of a shed.</b> The
/// spike's <c>ShedBuilder</c> keeps <c>KeptBins = 8</c> and counts the rest in a separate accumulator,
/// so 110 is what its <em>ball encountered</em> and 8 is what it stored — and the sentence <i>doubling
/// the radius is roughly 5× the shed</i> is true of the ball and false of the shed, which is
/// <b>constant</b>. <c>plans/0012</c> <b>Cause 5</b>: the digits travelled and the clause did not.
/// </para>
/// <para>
/// <b>So the storage decision is taken on numbers taken here rather than on that one</b>
/// (<c>adr/0043</c>). Everything this file prints is measured on a real generated world at Tick 0 —
/// the supply is <c>[[building]] parking</c> against the Buildings <see cref="SyntheticCity"/> raised,
/// and the geometry is the shipped <c>[roads]</c> lattice.
/// </para>
/// <para>
/// ⚠ <b>A generated city cannot vary its parking density, and that bounds what this can settle.</b>
/// Capacity is per kind and every Building is the same kind, so the shed's <em>size</em> is a property
/// of the lattice and the radius alone. What is measured here is therefore how much a shed <b>costs to
/// store and to build</b>; how often the nearest few are all full is a different question needing
/// task 4's acquire and release and a world where parking is scarce — <c>adr/0070</c> makes it void
/// until then.
/// </para>
/// </remarks>
[Trait(Tier.Key, Tier.Instrument)]
public sealed class ParkingShedSizeTests
{
    /// <summary>How many the caller keeps in the capped shape. The spike's number, for comparison.</summary>
    private const int Capped = 8;

    /// <summary>
    /// What the distribution sweep asks the query to keep, which is <em>everything</em>.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>This was <see cref="Capped"/> and it had to change, because the query stopped being
    /// exhaustive.</b> The old comment here said the size of a shed is <c>found</c> and <c>found</c> is
    /// counted whether or not a Car Park fits — <b>true when it was written and false now</b>.
    /// <see cref="ParkingShed"/> stops its ball once nothing further out could displace a <em>full</em>
    /// kept set, so a sweep keeping 8 counts only what it looked at on the way to its eighth. A kept set
    /// this large never fills at any radius this file sweeps, never satisfies
    /// <see cref="ShedScratch.KeptFull"/>, and therefore walks the whole ball exactly as before.
    /// <para>
    /// The old comment's worry was real and is now paid somewhere harmless: <see cref="ShedScratch.Offer"/>
    /// sorts by insertion, so an uncapped kept set does <c>O(found²)</c> bookkeeping. That inflates the
    /// <em>cost</em> of this sweep and not its <em>counts</em>, which is why the cost column below is
    /// timed on a separate sweep at <see cref="Capped"/> rather than on this one.
    /// </para>
    /// </remarks>
    private const int Uncapped = 4_096;

    /// <summary>Long enough for the Zone Rules to raise a city. <c>JobSearchBoxTests</c>' number.</summary>
    private const int Ticks = 1_024;

    /// <summary>The population every memory figure in this corpus is denominated in.</summary>
    private const int Target = 1_000_000;

    /// <summary>
    /// The gradient is read across three radii and the bill is read at target scale, so only the
    /// shipped radius pays for a 1M world.
    /// </summary>
    private static int[] Populations(int metres) => metres == 400
        ? [GoldenFixtures.Population, 16_000, 64_000, Target]
        : [GoldenFixtures.Population, 16_000, 64_000];

    private readonly ITestOutputHelper _out;

    public ParkingShedSizeTests(ITestOutputHelper output) => _out = output;

    /// <summary>
    /// <b>How many Car Parks are within reach of a Building, across the radius and the population.</b>
    /// </summary>
    [Fact]
    public void A_sheds_size_is_a_property_of_the_radius_and_not_of_the_city()
    {
        _out.WriteLine("radius(m)  population  buildings  found min/med/mean/p95/max  "
            + "settled  touched  | capped settled  touched");

        double[] means = new double[3];
        int index = 0;

        foreach (int metres in new[] { 200, 400, 800 })
        {
            Ruleset rules = WithRadius(metres);

            foreach (int population in Populations(metres))
            {
                Reading reading = Measure(population, rules, Capped);

                _out.WriteLine(
                    $"{metres,-10} {population,-11} {reading.Buildings,-10} "
                    + $"{reading.Min}/{reading.Median}/{reading.Mean:F1}/{reading.P95}/{reading.Max,-12} "
                    + $"{reading.Settled:F1}      {reading.Touched:F1}      | "
                    + $"{reading.CappedSettled:F1}            {reading.CappedTouched:F1}");

                if (population == 64_000)
                {
                    means[index] = reading.Mean;
                }
            }

            index++;
            _out.WriteLine(string.Empty);
        }

        Assert.True(means[0] > 0, "no Building found any parking, so nothing here was measured.");

        _out.WriteLine($"200 -> 400: x{means[1] / means[0]:F2}   400 -> 800: x{means[2] / means[1]:F2}");
    }

    /// <summary>
    /// <b>What a shed costs to build, and what each storage shape costs to keep.</b>
    /// </summary>
    [Fact]
    public void What_a_shed_costs_to_build_and_to_keep()
    {
        Ruleset rules = GoldenFixtures.Rules();

        _out.WriteLine($"radius {rules.Parking.RadiusMetres} m, cap {Capped}");
        _out.WriteLine("population  buildings  build(us/shed)  full(KiB)  capped(KiB)  full/capped");

        foreach (int population in new[] { GoldenFixtures.Population, 16_000, 64_000, Target })
        {
            Reading reading = Measure(population, rules, Capped);

            // A flat CSR rather than an intrusive list, because a shed is many-to-many: one Car Park
            // is within reach of many Buildings, so no single `next` column on CarParkTable can thread
            // it. Offsets plus members, four bytes each.
            double full = ((reading.Buildings + 1L) + reading.Total) * 4 / 1024.0;
            double capped = ((reading.Buildings + 1L) + reading.Kept(Capped)) * 4 / 1024.0;

            _out.WriteLine(
                $"{population,-11} {reading.Buildings,-10} {reading.MicrosecondsPerShed,-15:F2} "
                + $"{full,-10:F0} {capped,-12:F0} x{full / capped:F1}");
        }
    }

    /// <summary>
    /// One reading: the shed-size distribution over every Building, and what a production-shaped query
    /// costs on the same world.
    /// </summary>
    /// <remarks>
    /// <b>Two sweeps of one world, and they cannot be one sweep.</b> The distribution needs an
    /// <em>uncapped</em> query, because a capped one stops its ball early and stops counting with it;
    /// the cost needs a <em>capped</em> one, because that is the shape production runs. Doing both off
    /// one world rather than two is not a convenience — a 1,000,000-Citizen world is ten minutes to
    /// raise, and two would double every reading in this file.
    /// </remarks>
    private static Reading Measure(int population, Ruleset rules, int cap)
    {
        InputLog log = Log(population);
        Simulation simulation = Replay.Start(log, rules);

        // Stepped rather than started, because SyntheticCity raises no Buildings: a Building is a
        // Zone Rule's output, so a world at Tick 0 has the population and none of the housing -- and
        // a Car Park is provisioned at CreateBuilding, so at Tick 0 there is no parking either.
        Replay.Trace(simulation, log, new Ticks(Ticks), Ticks, []);

        World world = simulation.World;

        CarParkResidency residency = new();
        residency.Rebuild(world.CarParks, world.Roads.Segments);

        ShedScratch scratch = new();
        int[] wide = new int[Uncapped];
        int[] into = new int[cap];
        Tiles radius = rules.Parking.Radius;

        List<int> found = [];
        long settled = 0;
        long touched = 0;

        // Once to warm the arrays, once to read: ShedScratch grows to the largest graph it has seen,
        // so the first query in a world pays for every one after it.
        Sweep(world, residency, scratch, wide, radius, null, ref settled, ref touched);

        settled = 0;
        touched = 0;

        Sweep(world, residency, scratch, wide, radius, found, ref settled, ref touched);

        long capSettled = 0;
        long capTouched = 0;

        Sweep(world, residency, scratch, into, radius, null, ref capSettled, ref capTouched);

        capSettled = 0;
        capTouched = 0;

        var clock = Stopwatch.StartNew();
        Sweep(world, residency, scratch, into, radius, null, ref capSettled, ref capTouched);
        clock.Stop();

        found.Sort();

        return new Reading(
            found, settled, touched, capSettled, capTouched, clock.Elapsed.TotalMilliseconds);
    }

    private static void Sweep(
        World world,
        CarParkResidency residency,
        ShedScratch scratch,
        int[] into,
        Tiles radius,
        List<int>? record,
        ref long settled,
        ref long touched)
    {
        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (!world.Buildings.Rows.IsLive(slot))
            {
                continue;
            }

            Address door = world.AccessPoint(slot, TravelMode.Foot);

            ParkingShed.Nearest(
                world.Roads, world.CarParks, residency, door, radius, scratch, into, out int found);

            record?.Add(found);
            settled += scratch.Settled;
            touched += scratch.Touched.Length;
        }
    }

    /// <summary>The shipped Ruleset with one number changed.</summary>
    private static Ruleset WithRadius(int metres)
    {
        string toml = File.ReadAllText(GoldenFixtures.RulesetPath)
            .Replace("radius_metres = 400", $"radius_metres = {metres}", StringComparison.Ordinal);

        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    private static InputLog Log(int population)
    {
        InputLogBuilder builder = new(
            GoldenFixtures.Seed,
            new WorldConfiguration(population),
            GoldenFixtures.RulesetHash);

        builder.Append(new Ticks(0), new Command(CommandKind.Populate, default, default));

        return builder.Build();
    }

    /// <summary>A sorted list of shed sizes and what the sweep that produced it cost.</summary>
    private sealed class Reading(
        List<int> sorted,
        long settled,
        long touched,
        long capSettled,
        long capTouched,
        double milliseconds)
    {
        public int Buildings => sorted.Count;

        public long Total => sorted.Sum(static size => (long)size);

        public int Min => sorted.Count == 0 ? 0 : sorted[0];

        public int Max => sorted.Count == 0 ? 0 : sorted[^1];

        public int Median => At(0.50);

        public int P95 => At(0.95);

        public double Mean => sorted.Count == 0 ? 0 : (double)Total / sorted.Count;

        /// <summary>Nodes the <em>uncapped</em> ball settled — the whole radius, every time.</summary>
        public double Settled => Per(settled);

        /// <summary>Segments the <em>uncapped</em> ball touched.</summary>
        public double Touched => Per(touched);

        /// <summary>Nodes a production-shaped ball settled before it stopped.</summary>
        public double CappedSettled => Per(capSettled);

        /// <summary>Segments a production-shaped ball touched before it stopped.</summary>
        public double CappedTouched => Per(capTouched);

        public double MicrosecondsPerShed =>
            sorted.Count == 0 ? 0 : milliseconds * 1000 / sorted.Count;

        /// <summary>How many members a shed capped at <paramref name="cap"/> would hold in total.</summary>
        public long Kept(int cap) => sorted.Sum(size => (long)Math.Min(size, cap));

        private double Per(long total) => sorted.Count == 0 ? 0 : (double)total / sorted.Count;

        private int At(double quantile) =>
            sorted.Count == 0 ? 0 : sorted[Math.Min(sorted.Count - 1, (int)(sorted.Count * quantile))];
    }
}
