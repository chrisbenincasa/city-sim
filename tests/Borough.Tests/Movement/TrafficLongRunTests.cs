using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;
using Borough.Tests.Golden;
using Xunit.Abstractions;

namespace Borough.Tests.Movement;

/// <summary>
/// Milestone 5c task 8: the long acceptance run for traffic — <b>nothing accumulates on the road, and
/// nothing accumulates because of it.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>The first mechanism in this project with a per-Tick write to a saved column on every moving
/// entity.</b> <c>adr/0041</c>'s <c>volume_forward</c> and <c>volume_backward</c> are incremented when a
/// vehicle enters a Segment and decremented when it leaves, so an unmatched pair is an
/// <c>adr/0006</c>-class capacity loss that grows with elapsed time and that no single reading can see.
/// <c>RouteHopTable</c> is the other new collection: one row per Segment of every in-flight drive, whose
/// only sink is <c>TripEngine.ReleaseEnded</c>.
/// </para>
/// <para>
/// ⚠ <b>It runs over whole Days, and that is a correction rather than a nicety.</b> Every congestion
/// figure this milestone published before task 8 was taken over <b>512 Ticks from Tick 0</b> on a
/// 2,048-Tick Day, with employment still ramping through the whole window — and one of them, the
/// rush-hour sweep's ×1.0000, was an artefact of exactly that. So the length here is
/// <c>49 × TICKS_PER_DAY</c> rather than a round 100,000, and every reading is one whole Day.
/// </para>
/// <para>
/// ⚠ <b>Levels are accumulated every Tick and never sampled.</b> Segment volume is a <em>level</em>: a
/// reading taken once a Day is one instant of 2,048, and the departure window makes those instants
/// wildly unrepresentative of each other. Every quantity below that could be sampled is instead summed
/// over every Tick of the Day and divided, which is what makes <em>mean load</em> mean anything —
/// <c>plans/0026</c>'s second precondition, and the one this milestone got wrong for two days by
/// quoting the busiest Segment.
/// </para>
/// <para>
/// ⚠ <b>It cannot ratify <c>[traffic]</c>'s three numbers and it is not written as though it can.</b>
/// A generated city never reaches a load at which the volume-delay curve is anything but nearly flat
/// (task 6's finding 3: the paved extent scales with √population, so the same number sizes the demand
/// and the supply). What is asserted here is that the mechanism is <em>sound</em> over time. What the
/// curve is <em>worth</em> is a measurement somebody reads, is taken by <c>--traffic</c>, and is
/// recorded in <c>plans/0026</c> with the reason it settles nothing.
/// </para>
/// </remarks>
public sealed class TrafficLongRunTests(ITestOutputHelper output)
{
    /// <summary>Forty-nine whole Days, which is the first multiple of the Day above 100,000 Ticks.</summary>
    private const int Days = 49;

    /// <summary>
    /// Four thousand, which is the golden fixture's population and the smallest that drives at all.
    /// </summary>
    /// <remarks>
    /// <b>The structural half of the acceptance is true at any population and this is the cheapest one
    /// that is not vacuous.</b> A leak is a property of the code rather than of the load, so paying
    /// four times over for a run that asserts the same thing buys nothing — and the reading that
    /// <em>does</em> want a big city is the mean load, which is not asserted here at all.
    /// </remarks>
    private const int Population = 4_000;

    /// <summary>Days discarded as the transient: nobody drives until the assignment pass has run.</summary>
    /// <remarks>
    /// <c>[jobs] revisit_ticks</c> is 1,024 and the sample is derived from it, so employment climbs for
    /// several Days before it settles. Eight is generous and leaves forty-one Days of tail.
    /// </remarks>
    private const int SettleDays = 8;

    /// <summary>How far above the first half of the tail the second may read, in standard errors.</summary>
    /// <remarks>
    /// <b>Three, which is a convention rather than a measurement, and the quantity it guards is
    /// nowhere near it.</b> See <see cref="AssertFlat"/>: the failure this exists to catch stands at
    /// about fifteen, and what sits under three is the run's own noise.
    /// </remarks>
    private const double Sigmas = 3;

    private readonly ITestOutputHelper _output = output;

    /// <summary>
    /// <b>Nothing grows: the road empties, the hop table recycles, and a Day costs what the Day before
    /// it cost.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>06</c>'s definition of done — <i>no collection and no magnitude trending upward at steady
    /// state</i> — on the two collections 5c added and the one magnitude it can leak.
    /// </para>
    /// <para>
    /// <b>The vacuity guards come first, because every assertion below is satisfied perfectly by a city
    /// in which nobody ever drove</b>, and that city is what every shipped Ruleset except
    /// <c>congested.toml</c> produces.
    /// </para>
    /// <para>
    /// ⚠ <b>The collection half is a <em>minimum</em> live count over each Day, and not the slot
    /// high-water mark its sibling uses.</b> <c>CommuteLongRunTests</c> argues for slots on the ground
    /// that <i>a live count that stays flat proves nothing — it is flat in a city that leaks, because a
    /// leaked row is live</i>. That is true of a <b>mean</b> and false of a <b>minimum</b>: the
    /// departure window empties this city completely once a Day, so the emptiest Tick of a Day reads
    /// exactly <b>0</b>, and one leaked row makes it read 1 for ever. ***A minimum taken over a period
    /// the city empties is an exact leak test where a high-water mark is a statistical one*** — and it
    /// needs the city to empty, which is a property of the commute rather than of the assertion.
    /// </para>
    /// <para>
    /// ⚠ <b>The slot counts were asserted first and the assertion was measuring its own sample size.</b>
    /// Over 49 Days the hop table saturates at <b>105</b> slots by Day 4 and never moves again, and
    /// <c>TripTable</c> steps <b>12 → 13</b> once, around Day 41 — a max over ~100,000 draws of a count
    /// whose peak is <b>11 vehicles in the whole city</b>, which creeps for ever and is not a leak. That
    /// is <c>CommuteLongRunTests.PeakPopulation</c>'s finding on a second axis: a high-water mark of a
    /// small count is a statistic about the run's length. Reported below rather than asserted, because
    /// the minimum already refuses every leak the maximum could have caught.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_hundred_thousand_Tick_traffic_run()
    {
        Day[] days = Run();
        Day[] tail = days[SettleDays..];

        // ⚠ Printed before anything is asserted, because a long run that fails on its own first
        // assertion otherwise reports a number and withholds the series it came out of -- and the
        // series is the whole diagnosis. Found by needing it: the flatness band below was measured
        // wrong, and reading that took a second run with the print moved.
        _output.WriteLine($"Days                 {days.Length} ({days.Length * Ticks.PerDay:N0} Ticks)");
        _output.WriteLine($"drive Legs a Day     {Mean(tail, d => d.DriveLegs):N0}");
        _output.WriteLine($"vehicle-Ticks a Day  {Mean(tail, d => d.VehicleTicks):N0}");
        _output.WriteLine($"employed             {Mean(tail, d => d.Employed):N0}");
        _output.WriteLine(
            $"slots, first -> last  hop {tail[0].HopSlots}->{tail[^1].HopSlots}"
            + $"  trip {tail[0].TripSlots}->{tail[^1].TripSlots}"
            + $"  leg {tail[0].LegSlots}->{tail[^1].LegSlots}"
            + $"  traveller {tail[0].TravellerSlots}->{tail[^1].TravellerSlots}");

        foreach (Day day in days)
        {
            _output.WriteLine(
                $"  vehicle-Ticks {day.VehicleTicks,6}  drive Legs {day.DriveLegs,5}  "
                + $"employed {day.Employed,5}  quiet hops {day.QuietHops,3}");
        }

        Assert.True(Mean(tail, d => d.VehicleTicks) > 0, "no vehicle was ever on a Segment.");
        Assert.True(Mean(tail, d => d.DriveLegs) > 0, "no drive Leg was ever made.");

        // The collection half, and it is a MINIMUM rather than a high-water mark. See the remark.
        foreach (Day day in tail)
        {
            Assert.Equal(0, day.QuietHops);
            Assert.Equal(0, day.QuietTrips);
            Assert.Equal(0, day.QuietLegs);
            Assert.Equal(0, day.QuietTravellers);
        }

        // The magnitude half. Vehicle-Ticks is the quantity a leaked increment inflates without bound
        // -- one phantom vehicle costs 2,048 vehicle-Ticks a Day, for ever -- and drive Legs is the
        // flow that would have to be climbing for that inflation to be the city rather than a defect.
        AssertFlat(tail, d => d.VehicleTicks, "vehicle-Ticks per Day");
        AssertFlat(tail, d => d.DriveLegs, "drive Legs per Day");
        AssertFlat(tail, d => d.Employed, "the employed population");
    }

    /// <summary>
    /// ⚠ <b>The road is empty at the end of every Day, checked on every Tick of the run.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>Invariant.SegmentVolumeIsConserved</c> held to a hundred thousand Ticks rather than to
    /// 512.</b> <c>SegmentVolumeTests</c> already checks the same equality per Tick over half a
    /// thousand, which catches a decrement that never happens; what it cannot catch is a decrement that
    /// happens <em>nearly</em> always. A leak of one vehicle every few thousand Ticks is invisible in a
    /// short run, is a permanent capacity loss, and is the exact shape <c>adr/0084</c> names for parking
    /// occupancy — the reason that milestone is specified with two checks rather than one.
    /// </para>
    /// <para>
    /// <b>Stated as a peak of the discrepancy rather than as an assertion per Tick</b>, so that a
    /// failure reports how far the two sides drifted and on which Tick rather than merely that they
    /// did. A leak is monotone, so the first divergence is the whole diagnosis.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_road_never_carries_a_vehicle_that_is_not_on_it()
    {
        Simulation simulation = Start();
        World world = simulation.World;

        long worst = 0;
        long worstTick = -1;
        long peak = 0;

        for (int tick = 0; tick < Days * Ticks.PerDay; tick++)
        {
            simulation.Step(default);

            long onTheRoad = TotalVolume(world);
            long driving = Driving(world);
            long gap = Math.Abs(onTheRoad - driving);

            peak = Math.Max(peak, onTheRoad);

            if (gap > worst)
            {
                worst = gap;
                worstTick = tick;
            }
        }

        _output.WriteLine($"peak vehicles on the road: {peak:N0}");

        Assert.True(peak > 0, "no vehicle was ever on a Segment, so the conservation check is vacuous.");
        Assert.True(
            worst == 0,
            $"Segment volume and the driving population first disagreed by {worst} on Tick "
            + $"{worstTick:N0}. An increment with no matching decrement is a permanent capacity loss.");
    }

    /// <summary>
    /// The emptiest each Movement collection got inside one Day.
    /// </summary>
    /// <remarks>
    /// <b>A mutable struct threaded through the Tick loop rather than four locals</b>, because the four
    /// are one fact — <i>did the city empty</i> — and splitting them made the reset four lines that had
    /// to agree.
    /// </remarks>
    private struct Quiet()
    {
        public int Hops { get; private set; } = int.MaxValue;

        public int Trips { get; private set; } = int.MaxValue;

        public int Legs { get; private set; } = int.MaxValue;

        public int Travellers { get; private set; } = int.MaxValue;

        public void Observe(World world)
        {
            ArgumentNullException.ThrowIfNull(world);

            Hops = Math.Min(Hops, world.RouteHops.Rows.LiveCount);
            Trips = Math.Min(Trips, world.Trips.Rows.LiveCount);
            Legs = Math.Min(Legs, world.Legs.Rows.LiveCount);
            Travellers = Math.Min(Travellers, world.Travellers.Rows.LiveCount);
        }
    }

    /// <summary>One whole Day of the city, in the quantities this run is about.</summary>
    /// <remarks>
    /// <see cref="VehicleTicks"/> is a <b>sum over every Tick of the Day</b> and the rest are levels or
    /// flows read once at the end of it. The distinction is the file's second precondition: dividing
    /// this by <c>TICKS_PER_DAY</c> and by the Segment count is the mean load, and no sampled level
    /// recovers it.
    /// </remarks>
    private readonly record struct Day(
        long VehicleTicks,
        long DriveLegs,
        int Employed,
        int QuietHops,
        int QuietTrips,
        int QuietLegs,
        int QuietTravellers,
        int HopSlots,
        int TripSlots,
        int LegSlots,
        int TravellerSlots);

    private static Day[] Run()
    {
        Simulation simulation = Start();
        World world = simulation.World;

        List<Day> days = [];
        long vehicleTicks = 0;
        long driveLegs = 0;
        var quiet = new Quiet();

        for (int tick = 0; tick < Days * Ticks.PerDay; tick++)
        {
            simulation.Step(default);

            vehicleTicks += TotalVolume(world);
            quiet.Observe(world);

            // Drained every Tick rather than every Day, because the flow's own reading drains it and a
            // Census interval this long would be read as a peak somewhere it is not.
            driveLegs += simulation.Trips.Drain().DriveLegs.Sum;

            if ((tick + 1) % Ticks.PerDay != 0)
            {
                continue;
            }

            days.Add(new Day(
                vehicleTicks,
                driveLegs,
                Employed(world),
                quiet.Hops,
                quiet.Trips,
                quiet.Legs,
                quiet.Travellers,
                world.RouteHops.Rows.SlotCount,
                world.Trips.Rows.SlotCount,
                world.Legs.Rows.SlotCount,
                world.Travellers.Rows.SlotCount));

            vehicleTicks = 0;
            driveLegs = 0;
            quiet = new Quiet();
        }

        return [.. days];
    }

    /// <summary>
    /// The shipped congestion Ruleset, everybody driving.
    /// </summary>
    /// <remarks>
    /// <b><c>rulesets/congested.toml</c> rather than <c>minimal.toml</c>, and it is the fourth
    /// precondition.</b> <c>minimal.toml</c> states neither <c>[traffic]</c> nor <c>[households]</c>, so
    /// a run over it makes no drive Leg at all and every assertion in this file is vacuously true.
    /// Parsed from the committed file rather than built here, for <c>GoldenFixtures.Rules</c>' own
    /// reason: a Ruleset built in C# agrees with the loader by construction.
    /// </remarks>
    private static Simulation Start()
    {
        RulesetLoadResult result = RulesetLoader.Parse(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "congested.toml")),
            "congested.toml");

        Assert.True(result.Ok, result.Describe());

        InputLogBuilder builder = new(
            GoldenFixtures.Seed, new WorldConfiguration(Population), GoldenFixtures.RulesetHash);

        Simulation simulation = Replay.Start(builder.Build(), result.Ruleset!);

        // O(world) twice per Tick against a phase meant to be O(woken). --no-decide-guard's reason,
        // and the guard's own correctness is covered by the tests written for it.
        simulation.VerifyDecideWritesNothing = false;

        simulation.Step(new TickInput(
            [new Command(CommandKind.Populate, default, default)], rulesetHash: 0));

        return simulation;
    }

    private static long TotalVolume(World world)
    {
        RoadSegmentTable segments = world.Roads.Segments;
        long total = 0;

        for (int slot = 0; slot < segments.Rows.SlotCount; slot++)
        {
            if (segments.Rows.IsLive(slot))
            {
                total += segments.VolumeForward[slot] + segments.VolumeBackward[slot];
            }
        }

        return total;
    }

    private static int Driving(World world)
    {
        int driving = 0;

        for (int slot = 0; slot < world.Travellers.Rows.SlotCount; slot++)
        {
            if (world.Travellers.Rows.IsLive(slot)
                && (TravelMode)world.Legs.Mode[world.Travellers.CurrentLeg[slot]] != TravelMode.Foot)
            {
                driving++;
            }
        }

        return driving;
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

    /// <summary>
    /// The second half of the tail is not above the first, within a sixteenth.
    /// </summary>
    /// <remarks>
    /// <c>CommuteLongRunTests.AssertFlat</c>'s band, and for its reason: these are sampled flows over a
    /// city that demolishes and rebuilds, so two halves that matched exactly would be a run in which
    /// nothing happened.
    /// </remarks>
    /// <summary>
    /// The second half of the tail is not above the first by more than the series' own noise.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>The band is derived from the tail's own spread, and a fixed fraction was measured wrong
    /// here on 2026-08-15.</b> This began as <c>CommuteLongRunTests.AssertFlat</c>'s <b>1/16</b>,
    /// copied — and that band was chosen for quantities three times quieter. Vehicle-Ticks a Day has a
    /// per-Day <b>CV of 15.7%</b> over this run, so the difference of the two half-means carries a
    /// standard error of about <b>133</b> against a 1/16 band of <b>164</b>: the assertion was
    /// <b>1.25σ wide</b> and failed roughly one run in ten with nothing wrong underneath. It failed on
    /// the shipped seed, by 166. ***A flatness band is an assertion about the quantity's own variance
    /// as much as about its trend, so it cannot be transplanted between quantities.***
    /// </para>
    /// <para>
    /// <b>The city was flat the whole time, and lengthening the run is what said so.</b> At 200 Days
    /// the 192-Day tail's slope is <b>+0.267 vehicle-Ticks a Day</b> — against +6.71 fitted over 41 —
    /// and the half-difference is <b>−23, or −0.32σ</b>, pointing the other way. ⚠ <b>The spread grows
    /// with the run</b> (CV 15.7% at 49 Days, <b>19.0%</b> at 200), which is the signature of a
    /// <em>tail</em> rather than of a trend: more Days is more chances to draw a big one. That is this
    /// file's own <em>a maximum is a statistic of the sample as well as of the thing sampled</em>
    /// arriving on a <b>mean</b>.
    /// </para>
    /// <para>
    /// <b>Three sigma, one-sided, and the sensitivity was measured rather than argued.</b> A single
    /// leaked increment costs <c>TICKS_PER_DAY</c> vehicle-Ticks a Day for ever, so <b>one</b> vehicle
    /// leaked once, on Day 30 of 49, reads a rise of <b>2,018.6 against a band of 601.3</b> — ten
    /// sigma, taken by injecting exactly that and watching this assertion fire. A leak of one vehicle
    /// every twenty Days reads 2,272.2 against 991.5. ***A band widened because it was failing has to
    /// be shown still catching what it was written for***, which the fixed 1/16 never was. What this
    /// can no longer see is a drift of a few tenths of a percent a Day — and neither could the fixed
    /// band, which merely reported one either way at random.
    /// </para>
    /// <para>
    /// <b>A leak that lands <em>before</em> the tail is invisible here and is not missed</b>, because a
    /// constant offset is flat: it is caught exactly, on the Tick it happens, by
    /// <see cref="The_road_never_carries_a_vehicle_that_is_not_on_it"/>. This assertion owns the
    /// <em>trend</em> and that one owns the <em>level</em>, which is why widening this one costs
    /// nothing the file was relying on.
    /// </para>
    /// <para>
    /// <b>The <c>+ 1</c> floor is kept from the original</b>, so that a quantity whose halves are
    /// exactly constant — a standard error of zero — is not failed by a single row of movement.
    /// </para>
    /// <para>
    /// ⚠ <b><c>CommuteLongRunTests</c> still carries the fixed band and is not repaired here.</b> Its
    /// drive-Legs sibling in this file sits at a CV of <b>6.3%</b> against the same 1/16, which is the
    /// same coin toss in the safe direction on this seed; whether its own quantities are as marginal
    /// is a reading nobody has taken.
    /// </para>
    /// </remarks>
    private static void AssertFlat(Day[] tail, Func<Day, long> of, string what)
    {
        Day[] earlyDays = tail[..(tail.Length / 2)];
        Day[] lateDays = tail[(tail.Length / 2)..];

        double early = Mean(earlyDays, of);
        double late = Mean(lateDays, of);

        // The standard error of the difference of two means, taken from each half's own spread. Two
        // variances rather than one pooled estimate, because a real trend inflates the spread of the
        // whole tail and would widen the band it is being tested against.
        double error = Math.Sqrt(
            (Variance(earlyDays, of) / earlyDays.Length) + (Variance(lateDays, of) / lateDays.Length));

        double band = (Sigmas * error) + 1;

        Assert.True(
            late - early <= band,
            $"{what} read {early:F1} over the first half of the tail and {late:F1} over the second -- "
            + $"a rise of {late - early:F1} against a {Sigmas}-sigma band of {band:F1}, from a "
            + $"standard error of {error:F1} on the difference.");
    }

    private static double Mean(Day[] days, Func<Day, long> of)
    {
        long total = 0;

        foreach (Day day in days)
        {
            total += of(day);
        }

        return (double)total / days.Length;
    }

    /// <summary>The spread of <paramref name="of"/> about its own mean over <paramref name="days"/>.</summary>
    private static double Variance(Day[] days, Func<Day, long> of)
    {
        double mean = Mean(days, of);
        double total = 0;

        foreach (Day day in days)
        {
            double error = of(day) - mean;

            total += error * error;
        }

        return total / days.Length;
    }
}
