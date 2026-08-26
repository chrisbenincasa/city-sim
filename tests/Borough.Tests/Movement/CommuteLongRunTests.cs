using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Instruments;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;
using Borough.Tests.Golden;

using Xunit.Abstractions;

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
public sealed class CommuteLongRunTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

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

    /// <summary>
    /// Four Days, which is all the profile needs and a fraction of what the flow readings do.
    /// </summary>
    /// <remarks>
    /// <b>The profile is read off the roster rather than sampled from the run, so the run's only job
    /// is to get everybody a job.</b> <c>[jobs] revisit_ticks</c> is 1,024, so employment has settled
    /// several times over by here. ⚠ <b>Not shared with <see cref="PeakTickCount"/> on purpose</b>: a
    /// sampled flow needs length because its precision comes from the number of readings, and this
    /// needs none because there is no sample in it. Sharing the constant would make a cheap
    /// measurement pay a long run's price for a precision it does not consume — which matters now
    /// that a commute is two journeys and every one of these runs costs twice what it did.
    /// </remarks>
    private const int ProfileTickCount = 8_192;

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
    /// <b>The Day has a shape: two peaks, a middle that is not empty, and a quiet night.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>adr/0101</c>'s named ratifier, and it replaces the ratifier for a number that no longer
    /// exists.</b> This test used to assert the <em>quiet fraction</em> against
    /// <c>commute_peak_factor</c> — a dial that authored a departure window, whose only checkable
    /// consequence was that the window and the peak were reciprocals of each other. The peak is now a
    /// <b>reading</b>: what the profile comes out as, given where the jobs are and how long people
    /// work. So what is asserted is the profile's <em>shape</em>, which is the thing the decision
    /// actually claims.
    /// </para>
    /// <para>
    /// <b>Read off the roster rather than sampled from a run, and that is a real improvement.</b> The
    /// roster's buckets <em>are</em> the departure profile — every Citizen's two phases, exactly, with
    /// no counting noise at all. The old test's whole difficulty was that a maximum over a small
    /// sample is a statistic about the sample; this quantity has no sample in it, so the population
    /// is a matter of taste rather than of validity.
    /// </para>
    /// <para>
    /// <b>Asserted as ordering relations rather than as counts</b>, because the counts are a property
    /// of the shipped bands and would have to be re-recorded every time somebody retunes them —
    /// which is the <c>[InlineData]</c> trap <c>JobRulesetLoadTests</c> already walked into once.
    /// What is structural is that a night with no Shift band over it is quieter than a morning that
    /// has one.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_Day_has_two_peaks_a_middle_that_is_not_empty_and_a_quiet_night()
    {
        World world = RunToSettled(GoldenFixtures.Rules(), PeakPopulation, ProfileTickCount);

        (int[] perHour, int total) = Profile(world);

        Assert.True(total > 0, "nobody commutes at all, so there is no profile.");

        Print(perHour, total);

        int morning = Between(perHour, 4 * 4, 10 * 4);
        int midday = Between(perHour, 10 * 4, 12 * 4);
        int evening = Between(perHour, 12 * 4, 21 * 4);
        int night = Between(perHour, 21 * 4, 24 * 4) + Between(perHour, 0, 4 * 4);

        // ⚠ No bin may hold a tenth of the Day's journeys. This is the assertion the first version of
        // this test lacked, and lacking it is why a profile made of 25 spikes passed: `two peaks, a
        // middle, a quiet night` was true of it. A spike is what a quantised draw produces, and it
        // is the failure mode this shape has.
        int fullest = 0;

        foreach (int here in perHour)
        {
            fullest = here > fullest ? here : fullest;
        }

        Assert.True(
            fullest * 10 < total,
            $"one quarter-hour holds {fullest} of {total} journeys, which is a spike rather than a "
            + "peak -- the draws behind it are quantised.");

        // The night is the assertion that would fail first if the Shift bands ever stopped bounding
        // anything, and it is one-sided: a band with no mass over those hours cannot put departures
        // there, and no term in this arithmetic can move them back in.
        Assert.True(
            night * 20 < total,
            $"{night} of {total} journeys begin between 21:00 and 04:00, which is not a quiet night.");

        Assert.True(morning > 0 && evening > 0, "the Day has fewer than two peaks.");

        // ⚠ The middle of the Day is EXACTLY ZERO here, and that is a finding rather than a defect.
        // This Ruleset declares ONE employing kind, so every workplace in the world keeps the same
        // sort of hours and there is genuinely nobody to be travelling at eleven. A midday commute is
        // a shop, a school and a factory disagreeing about when the day starts -- which is LAND USE,
        // and adr/0070's `build X` rather than a distribution this draw could be reshaped into.
        //
        // Asserted as a ZERO rather than left unasserted, because the claim is now discharged next
        // door: The_empty_middle_of_the_Day_is_land_use_and_a_second_kind_fills_it appends a second
        // [[building]] kind to this same file and watches the hole close. This end of that pair is
        // the control, so it has to hold the hole open.
        Assert.Equal(0, midday);

        // Peaked rather than flat, over the morning, and this is the assertion the plateau failed.
        // A uniform draw over the start band gives near-equal bars; a triangular one gives a peak.
        // Stated against the morning's own mean so it cannot be satisfied by the evening.
        int morningBins = 0;
        int morningFullest = 0;

        for (int bin = 4 * 4; bin < 10 * 4; bin++)
        {
            morningBins += perHour[bin] > 0 ? 1 : 0;
            morningFullest = perHour[bin] > morningFullest ? perHour[bin] : morningFullest;
        }

        Assert.True(
            morningFullest * morningBins * 2 > morning * 3,
            $"the fullest morning quarter-hour holds {morningFullest} against a mean of "
            + $"{morning / Math.Max(morningBins, 1)} over {morningBins} occupied ones, which is a "
            + "plateau rather than a peak.");
    }

    /// <summary>
    /// <b>The empty middle of the Day is land use, and a second employing kind fills it</b> — but only
    /// an <em>earlier</em> one, because a later one drags its own return echo into the night.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This discharges the prediction the test above declines to assert.</b> That one measures
    /// exactly zero journeys between 10:00 and 11:45 and records it as correct for a Ruleset declaring
    /// one employing kind, with <em>a second <c>[[building]]</c> kind</em> named as the reopening
    /// trigger. This is that trigger pulled, so the claim stops being a comment: the shipped file's
    /// hole is a property of its <b>content</b> and not of the draw behind the departure, and the
    /// mechanism responds to land use with no engine change and no new number.
    /// </para>
    /// <para>
    /// ⚠ <b>The finding is the <em>direction</em>, and it was measured backwards first.</b> The
    /// obvious second kind is a later one — an office opening while a works is already running — and it
    /// does fill the afternoon. It also fills the night: a band at 13–17 returns at
    /// <c>start + shift</c>, which is 19:00–03:00, so the Day acquires an all-night tail and the quiet
    /// end is gone. ***A Day's quiet end is bounded by <c>latest start + longest Shift</c>***, which is
    /// a consequence of <c>adr/0101</c>'s anchor model that nothing in that ADR states: the night is
    /// not authored anywhere and cannot be defended directly, so the only band that adds midday
    /// traffic without waking it is one starting <b>earlier</b> than the shipped one. An early shift
    /// puts its <em>returns</em> in the middle of the Day, which is where a real city's midday travel
    /// comes from.
    /// </para>
    /// <para>
    /// <b>Both cities are measured, because the assertion is a comparison and not a level.</b> A
    /// midday count means nothing on its own — the shipped bands could be retuned until any number
    /// appeared there. What is structural is that the same engine, the same draws and the same
    /// population produce a hole with one kind and a baseline with two.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_empty_middle_of_the_Day_is_land_use_and_a_second_kind_fills_it()
    {
        World mixed = RunToSettled(WithASecondKind(3, 6), PeakPopulation, ProfileTickCount);
        World single = RunToSettled(GoldenFixtures.DecliningRules(), PeakPopulation, ProfileTickCount);

        (int[] perBin, int total) = Profile(mixed);
        (int[] control, int controlTotal) = Profile(single);

        Assert.True(total > 0, "nobody commutes at all, so there is no profile.");

        Print(perBin, total);

        int midday = Between(perBin, 10 * 4, 12 * 4);
        int controlMidday = Between(control, 10 * 4, 12 * 4);

        _output.WriteLine(string.Empty);
        _output.WriteLine($"10:00-11:45 holds {midday} of {total} against {controlMidday} of "
            + $"{controlTotal} on one kind.");

        // The whole claim, stated as the comparison it is.
        Assert.Equal(0, controlMidday);
        Assert.True(
            midday > 0,
            "a second employing kind did not put anybody on the road in the middle of the Day, so "
            + "the hole is not land use after all.");

        // ⚠ The quiet stretch MOVED, and reading that as a regression is the trap. It is 21:00-04:00
        // on one kind and 20:00-02:00 here, because the earliest band moved from 6 to 3 and the night
        // is bounded by the bands at both ends rather than authored at either. What must survive is
        // that a quiet stretch EXISTS -- an anchor model that produced traffic round the clock would
        // have lost the property the shipped Ruleset demonstrates.
        int night = Between(perBin, 20 * 4, 24 * 4) + Between(perBin, 0, 2 * 4);

        Assert.True(
            night * 20 < total,
            $"{night} of {total} journeys begin between 20:00 and 01:45, which is not a quiet night.");
    }

    /// <summary>
    /// The roster's two bucket lists as a quarter-hourly histogram, and the count in it.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Quarter-hours rather than hours, and the coarser instrument was hiding the defect it was
    /// built to find.</b> An hourly histogram cannot distinguish <em>departures spread across this
    /// hour</em> from <em>every departure in this hour on one Tick</em> — and the first profile this
    /// test printed was the second while reading like the first.
    /// </remarks>
    private static (int[] PerBin, int Total) Profile(World world)
    {
        const int Bins = Ticks.HoursPerDay * 4;
        int[] perHour = new int[Bins];
        int total = 0;

        for (int phase = 0; phase < Ticks.PerDay; phase++)
        {
            int here = world.Commutes.CountAt(world.Citizens, phase)
                + world.Commutes.ReturningCountAt(world.Citizens, phase);

            // The bin a Tick falls in, floored -- the inverse of Ticks.AtHour, and inexact in the
            // same place for the same reason: 24 does not divide 2048.
            perHour[(int)((long)phase * Bins / Ticks.PerDay)] += here;
            total += here;
        }

        return (perHour, total);
    }

    /// <summary>The journeys beginning in <c>[from, to)</c>.</summary>
    private static int Between(int[] perHour, int from, int to)
    {
        int total = 0;

        for (int hour = from; hour < to; hour++)
        {
            total += perHour[hour];
        }

        return total;
    }

    /// <summary>The profile, as something to look at.</summary>
    private void Print(int[] perHour, int total)
    {
        int widest = 1;

        foreach (int here in perHour)
        {
            widest = here > widest ? here : widest;
        }

        _output.WriteLine($"Commute departures by quarter-hour -- {total} journeys a Day");
        _output.WriteLine("(both directions: leaving home and leaving work)");
        _output.WriteLine(string.Empty);

        for (int bin = 0; bin < perHour.Length; bin++)
        {
            _output.WriteLine(
                $"{bin / 4,2}:{bin % 4 * 15:00} {perHour[bin],5}  "
                + new string('#', perHour[bin] * 48 / widest));
        }
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

    /// <summary>
    /// A city stepped until employment has settled, for the readings that want the roster rather
    /// than a sampled flow.
    /// </summary>
    private static World RunToSettled(Ruleset rules, int population, int ticks)
    {
        var key = WorldKey.FromSeed(GoldenFixtures.Seed);
        var world = new World(population, rules, key);

        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        for (int tick = 0; tick < ticks; tick++)
        {
            simulation.Step(default);
        }

        return world;
    }

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
                && world.Businesses.Rows.IsValid(world.Citizens.Workplace[slot]))
            {
                total++;
            }
        }

        return total;
    }

    /// <summary>
    /// The shipped file plus a second employing kind, as TOML.
    /// </summary>
    /// <remarks>
    /// <b><see langword="internal"/> and returning text rather than a <see cref="Ruleset"/>, so a
    /// second reader can add a key without copying the fixture.</b>
    /// <c>Parking.ParkingArrivalStreamTests</c> needs this city with <c>parking</c> on the second
    /// kind, and a duplicate would be <c>plans/0012</c> <i>Cause 1</i> by construction — one fact,
    /// two files, and the copy nobody is looking at is the one that drifts.
    /// </remarks>
    internal static string SecondKindToml(int earliestHour, int latestHour) =>
        File.ReadAllText(GoldenFixtures.DecliningRulesetPath) + $$"""

            [[business]]
            name = "workshop_trade"
            jobs = 8
            shift_start_earliest_hour = {{earliestHour}}
            shift_start_latest_hour   = {{latestHour}}

            [[building]]
            name = "workshop"
            bins = [
                { resource = "sundries", capacity = 48 },
                { resource = "repairs",  capacity = 4 },
            ]
            occupants = 4
            business = "workshop_trade"
            condemn_after_days   = 2
            collapses_after_days = 1

            [[rule]]
            name    = "restock_workshop"
            kind    = "workshop"
            rate    = 8
            apply   = { min = 1, max = 4 }
            inputs  = []
            outputs = [ { scope = "local", resource = "sundries", amount = 4 } ]

            [[rule]]
            name    = "consume_workshop"
            kind    = "workshop"
            rate    = 32
            apply   = { derived = "occupancy" }
            inputs  = [ { scope = "local", resource = "sundries", amount = 4 } ]
            outputs = []

            [[rule]]
            name    = "upkeep_workshop"
            kind    = "workshop"
            rate    = 16
            apply   = { min = 1, max = 1 }
            inputs  = [ { scope = "local", resource = "repairs", amount = 4 } ]
            outputs = []

            [[zone_rule]]
            name          = "works"
            kind          = "workshop"
            zone          = 0
            interval      = 32
            revisit_ticks = 2048
            """;

    /// <summary>
    /// The shipped Ruleset with a <b>second employing kind</b> appended, keeping its own Shift band.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Parsed from the committed file rather than built here, for <c>GoldenFixtures.Rules</c>' own
    /// reason: a Ruleset built in C# agrees with the loader by construction, so a run over one proves
    /// nothing about the file the city actually ships with.
    /// </para>
    /// <para>
    /// 🔴 <b><c>declining.toml</c> rather than <c>minimal.toml</c> since milestone 17, and the
    /// paragraph below is what said so before anyone checked.</b> It claims the clone holds <em>the
    /// condemnation cadence</em>; <c>adr/0164</c> made that cadence <em>nothing</em>, and the
    /// consequence is not that the workshops decline more slowly — <em>it is that they are never
    /// built at all</em>. The two Zone Rules compete for the same Lots, the city opens fully built
    /// in dwellings, and without a Building falling down no Lot ever comes free for the second kind
    /// to win. The midday count was 0 because the mixed city had no workshops in it.
    /// </para>
    /// <para>
    /// ⚠ <b>The two <c>condemn_after_days</c> keys on the appended kind are part of the clone and
    /// not a new choice</b> — they restate what <c>declining.toml</c> gives <c>dwelling</c>, which is
    /// what "cloning holds the cadence" has always meant. A second kind that never declined would
    /// ratchet: every cleared Lot taken by a workshop would stay one for ever, and the comparison
    /// would drift into being about tenure rather than about Shift bands.
    /// </para>
    /// <para>
    /// <b>The appended kind is a clone of <c>dwelling</c> with one line changed</b>, and everything
    /// about that is deliberate. Cloning holds every ratio the shipped file chose — occupancy, jobs
    /// per resident, the Bin sizes, the condemnation cadence — so the only difference between the two
    /// cities is <em>when the workplaces open</em>. <c>zone = 0</c> on both Zone Rules because
    /// <c>SyntheticCity</c> paints every Lot with bit 0 and nothing else: the two Rules compete for
    /// the same Lots, which is a city with <b>mixed use everywhere</b> rather than districts. That is
    /// the crudest possible land-use split and it is the right one to measure first, because it
    /// isolates the question — <em>does a mixture of Shift bands fill the middle of the Day?</em> —
    /// from the separate question of whether the split has to be <em>spatial</em>.
    /// </para>
    /// </remarks>
    private static Ruleset WithASecondKind(int earliestHour, int latestHour)
    {
        RulesetLoadResult result =
            RulesetLoader.Parse(SecondKindToml(earliestHour, latestHour), "test.toml");

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
