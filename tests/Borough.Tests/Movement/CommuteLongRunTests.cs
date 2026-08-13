using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Instruments;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;
using Borough.Tests.Golden;

namespace Borough.Tests.Movement;

/// <summary>
/// The 100,000-Tick acceptance run for employment and the commute: nothing grows, and the morning
/// peak is the one the Ruleset states.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>adr/0006</c>'s collection half aimed at the three tables that have never met a long run.</b>
/// <c>TripTable</c>, <c>LegTable</c> and <c>TravellerTable</c> joined <c>World._tables</c> in
/// milestone 5b, and for the whole of that milestone the only Trips in existence were the ones a
/// human typed — a handful, all resolved within Ticks. A generator is the first thing that creates
/// them in bulk and for ever, so this is the first run that can say whether they recycle.
/// </para>
/// <para>
/// <b>Slots rather than live rows, which is the distinction the assertion turns on.</b> A live count
/// that stays flat proves nothing — it is flat in a city that leaks, because a leaked row is live.
/// What <c>adr/0006</c> is about is <see cref="Core.Instruments.CensusCounter.Slots"/>: the high-water
/// mark of simultaneous demand, which rises only when a row is created and the free list is empty. A
/// city that recycles has a slot count that stops climbing; a city that leaks has one that never does.
/// </para>
/// <para>
/// <b>1,000 Citizens, and the Commute Budget is inert at that size on purpose.</b> This is the
/// <em>structural</em> half of the acceptance — collections, trends, the peak's derivation — all of
/// which are true at any population. The <em>ratifying</em> half needs a city where the Budget binds
/// and is a headless run rather than a unit test, because it is a measurement somebody reads and not
/// an assertion anybody can write in advance (<c>plans/0023</c> task 8).
/// </para>
/// </remarks>
public sealed class CommuteLongRunTests
{
    private const int TickCount = 100_000;
    private const int Population = 1_000;

    /// <summary>
    /// Fine relative to the departure window, which is what makes the peak measurable.
    /// </summary>
    /// <remarks>
    /// At <c>commute_peak_factor = 3</c> the window is 2,731 Ticks, so a reading every 256 puts
    /// <b>eleven of a Day's thirty-two samples inside it</b>. <c>PlacementLongRunTests</c>' 2,048
    /// would put one sample in the window and would measure the peak as whatever it happened to land
    /// on. <b>Fineness is necessary and was not sufficient</b> — see <see cref="PeakPopulation"/>: a
    /// sample every 256 Ticks of a count whose mean is 1.34 resolves the noise perfectly and the
    /// window not at all.
    /// </remarks>
    private const int ReadEvery = 256;

    /// <summary>Readings discarded as the transient: nobody is employed until the pass has run.</summary>
    private const int SettleReadings = 32;

    /// <summary>
    /// Percentage points either side of the quiet fraction the window implies.
    /// </summary>
    /// <remarks>
    /// <b>Wide, and both ends are named rather than padded.</b> A commute that outlives the end of the
    /// window carries walkers past it and pushes the measured quiet fraction <em>down</em>; a Tick
    /// inside the window on which nobody happened to be assigned to leave pushes it <em>up</em>. Both
    /// shrink as the population grows and neither is zero here, so the band is symmetric and generous.
    /// A tolerance tight enough to distinguish a factor of 3 from one of 4 would be measuring the
    /// commute duration, which this test does not control.
    /// </remarks>
    private const int QuietTolerance = 10;

    /// <summary>
    /// The population the peak is measured at, and it is not <see cref="Population"/>.
    /// </summary>
    /// <remarks>
    /// <b>1,000 Citizens cannot carry a measurement of pedestrian density, and finding that out was
    /// most of what task 8 cost.</b> Roughly 440 of them hold a job; at a factor of 1 they leave over
    /// a whole Day and a commute lasts tens of Ticks, so <b>about one and a third people are walking
    /// at any instant</b> — and a count with a mean of 1.34 is zero 26% of the time by arithmetic, not
    /// by anything the departure window did. The control therefore read a quiet fraction of 24% where
    /// the window says 0%, and would have been "fixed" by widening the band until it admitted the
    /// noise it was made of.
    /// </remarks>
    private const int PeakPopulation = 4_000;

    /// <summary>Six Days, which is enough Days and a quarter of the run above.</summary>
    /// <remarks>
    /// The trend assertions need a long run because a leak is slow; a peak needs <em>samples inside a
    /// window</em>, which is a property of the reading interval and the Day, and more Days past the
    /// first few buy nothing. Four times the population at a fifth of the length costs less than the
    /// run it sits beside.
    /// </remarks>
    private const int PeakTickCount = 50_000;

    /// <summary>
    /// <b>Nothing grows: the three Movement tables recycle, and employment settles rather than
    /// climbing.</b>
    /// </summary>
    /// <remarks>
    /// <c>06</c>'s definition of done — <i>no collection and no magnitude trending upward at steady
    /// state</i>. The vacuity guards come first because every assertion below is satisfied perfectly
    /// by a city in which no Trip was ever made.
    /// </remarks>
    [Fact]
    public void The_hundred_thousand_Tick_commute_run()
    {
        Reading[] readings = Run(GoldenFixtures.Rules(), Population, TickCount);
        Reading[] tail = readings[SettleReadings..];

        Assert.True(Mean(tail, r => r.Made) > 0, "no Trip was ever made.");
        Assert.True(Mean(tail, r => r.Employed) > 0, "nobody ever held a job.");

        // The collection half. Compared between the first and last reading of the tail rather than
        // averaged: a slot count is monotone by construction, so a mean says nothing and the only
        // question is whether it stopped.
        Assert.Equal(tail[0].TripSlots, tail[^1].TripSlots);
        Assert.Equal(tail[0].LegSlots, tail[^1].LegSlots);
        Assert.Equal(tail[0].TravellerSlots, tail[^1].TravellerSlots);

        // The magnitude half, on the two quantities a generator could make climb without bound: how
        // many people hold a job, and how many Trips a reading interval produces.
        AssertFlat(tail, r => r.Employed, "the employed population");
        AssertFlat(tail, r => r.Made, "Trips made per interval");
    }

    /// <summary>
    /// <b>The quiet part of the Day is the part the peak factor states, and it is the one statement
    /// of that number a small city can carry.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>5b's transferred task 4 — peak pedestrian density — and it is the ratifier for
    /// <c>commute_peak_factor</c> read as a <em>derivation</em> rather than as a value.</b> Under a
    /// uniform departure window of <c>W</c> Ticks the instantaneous departure rate is
    /// <c>TICKS_PER_DAY / W</c> times the daily average. The obvious assertion is therefore
    /// <c>peak ÷ mean in-flight = the stated factor</c> — and it was written that way first, and it
    /// <b>reads 10 against a stated 3</b> on this fixture.
    /// </para>
    /// <para>
    /// <b>That reading is counting noise, and the decomposition is exact enough to be worth writing
    /// down.</b> <c>peak ÷ day-mean</c> is the product of two terms: the structural one, which is the
    /// stated factor, and <c>max-of-N ÷ in-window mean</c>, which is <b>1 only in the limit of large
    /// counts</b>. At 1,000 Citizens roughly 440 hold a job, they leave over 2,731 Ticks, and a commute
    /// lasts tens of Ticks — so about <b>four</b> people are walking at any instant inside the window,
    /// and the largest of 116 samples of a mean-4 count is about 10 by arithmetic that has nothing to
    /// do with cities. The first form of this test was measuring its own sample size.
    /// </para>
    /// <para>
    /// <b>The quiet fraction says the same thing and has no such term.</b> If departures are uniform
    /// over <c>TICKS_PER_DAY ÷ factor</c> Ticks and a commute is short against that window, then
    /// nobody is in flight for <c>1 − 1 ÷ factor</c> of the Day — a proportion over every sample
    /// rather than a maximum over them, so it converges rather than growing with the run. It is the
    /// same claim about the same number, stated in the one currency this population can pay in.
    /// </para>
    /// <para>
    /// <b>Two runs, because one is a fixture and two are a relationship.</b> A factor of 1 is a Day
    /// with no peak at all — the window is the whole Day and the quiet fraction should vanish — which
    /// is what makes this a test of <c>JobRuleset.CommuteWindow</c> rather than of the shipped file.
    /// </para>
    /// <para>
    /// <b>The peak itself is asserted one-sided, and on purpose.</b> Every term above inflates a
    /// measured maximum and none deflates it, so <c>peak ≥ factor × mean</c> is safe at any population
    /// while its converse is not — and it is the half that would fail if the departures ever stopped
    /// being concentrated at all.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData(3)]
    [InlineData(1)]
    public void The_quiet_part_of_the_Day_is_the_part_the_peak_factor_states(int factor)
    {
        Ruleset rules = WithPeak(factor);
        Reading[] tail = Run(rules, PeakPopulation, PeakTickCount)[SettleReadings..];

        long mean = Mean(tail, r => r.InFlight);
        long peak = 0;
        long quiet = 0;

        foreach (Reading reading in tail)
        {
            peak = reading.InFlight > peak ? reading.InFlight : peak;
            quiet += reading.InFlight == 0 ? 1 : 0;
        }

        Assert.True(mean > 0, "no Traveller was ever in flight, so there is no peak to measure.");

        long expected = 100 - (100 / factor);
        long measured = quiet * 100 / tail.Length;

        Assert.InRange(measured, expected - QuietTolerance, expected + QuietTolerance);
        Assert.True(
            peak >= factor * mean,
            $"in-flight Travellers peaked at {peak} against a daily mean of {mean}, which is flatter"
            + $" than the {factor}x the Ruleset states.");
    }

    /// <summary>
    /// The second half of the tail is not above the first, within a sixteenth.
    /// </summary>
    /// <remarks>
    /// <c>PlacementLongRunTests.The_queue_does_not_grow</c>'s shape, and a band rather than an
    /// equality because these are sampled flows over a city that demolishes and rebuilds: a run whose
    /// two halves matched exactly would be a run in which nothing happened.
    /// </remarks>
    private static void AssertFlat(Reading[] tail, Func<Reading, long> of, string what)
    {
        long early = Mean(tail[..(tail.Length / 2)], of);
        long late = Mean(tail[(tail.Length / 2)..], of);

        Assert.True(
            late <= early + (early / 16) + 1,
            $"{what} read {early} over the first half of the tail and {late} over the second.");
    }

    private static long Mean(Reading[] readings, Func<Reading, long> of)
    {
        long total = 0;

        foreach (Reading reading in readings)
        {
            total += of(reading);
        }

        return total / readings.Length;
    }

    /// <summary>One interval's worth of the city, in the quantities this test is about.</summary>
    private readonly record struct Reading(
        int TripSlots,
        int LegSlots,
        int TravellerSlots,
        int InFlight,
        int Employed,
        long Made);

    private static Reading[] Run(Ruleset rules, int population, int ticks)
    {
        var key = WorldKey.FromSeed(GoldenFixtures.Seed);
        var world = new World(population, rules, key);

        var simulation = new Simulation(world, key)
        {
            // O(world) twice per Tick against a phase meant to be O(woken). --no-decide-guard's
            // reason, and the guard's own correctness is covered by the tests written for it.
            VerifyDecideWritesNothing = false,
        };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        List<Reading> readings = [];

        for (int tick = 0; tick < ticks; tick++)
        {
            simulation.Step(default);

            if ((tick + 1) % ReadEvery != 0)
            {
                continue;
            }

            TripActivity trips = simulation.Trips.Drain();
            long made = 0;

            foreach (TripCostBucket bucket in Buckets)
            {
                made += trips.Costs[bucket].Sum;
            }

            readings.Add(new Reading(
                world.Trips.Rows.SlotCount,
                world.Legs.Rows.SlotCount,
                world.Travellers.Rows.SlotCount,
                world.Travellers.Rows.LiveCount,
                Employed(world),
                made));
        }

        return [.. readings];
    }

    private static int Employed(World world)
    {
        int total = 0;

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (world.Citizens.Rows.IsLive(slot)
                && world.Buildings.Rows.IsValid(world.Citizens.Workplace[slot]))
            {
                total++;
            }
        }

        return total;
    }

    /// <summary>The shipped Ruleset with its stated morning peak replaced.</summary>
    /// <remarks>
    /// Parsed from the committed file rather than built here, for <c>GoldenFixtures.Rules</c>' own
    /// reason: a Ruleset built in C# agrees with the loader by construction, so a run over one proves
    /// nothing about the file the city actually ships with.
    /// </remarks>
    private static Ruleset WithPeak(int factor)
    {
        string toml = File.ReadAllText(GoldenFixtures.RulesetPath);
        const string Key = "commute_peak_factor = 3";

        Assert.Contains(Key, toml, StringComparison.Ordinal);

        RulesetLoadResult result = RulesetLoader.Parse(
            toml.Replace(Key, $"commute_peak_factor = {factor}", StringComparison.Ordinal),
            "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    private static TripCostBucket[] Buckets =>
    [
        TripCostBucket.UnderOneMinute,
        TripCostBucket.UnderTwoMinutes,
        TripCostBucket.UnderFourMinutes,
        TripCostBucket.UnderEightMinutes,
        TripCostBucket.UnderSixteenMinutes,
        TripCostBucket.UnderThirtyTwoMinutes,
        TripCostBucket.ThirtyTwoMinutesOrMore,
    ];
}
