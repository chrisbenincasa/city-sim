using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;
using Borough.Tests.Golden;
using Xunit.Abstractions;

namespace Borough.Tests.Parking;

/// <summary>
/// <b>The acceptance run for parking: over whole Days, nothing grows and no space is lost.</b>
/// </summary>
/// <remarks>
/// <para>
/// <c>adr/0006</c>'s obligation and <c>CLAUDE.md</c>'s <i>no collection and no magnitude trending
/// upward at steady state</i>, aimed at the two things this milestone added — <c>CarParkTable</c>, and
/// the occupancy on it. ⚠ <b>The magnitude is the one that matters and it is the one a reader would
/// skip</b>: a leaked space is <em>capacity destroyed for ever</em>, and it presents as a
/// well-provisioned city becoming a crowded one with nobody having built or demolished anything.
/// </para>
/// <para>
/// <b>Whole Days rather than a round 100,000 Ticks, which is task 8's own instruction.</b> 5c task 8
/// found a figure that was an artefact of stopping mid-ramp; parking is worse, because
/// <c>adr/0101</c> gives the Day a shape and Tick 0 is midnight — so a run that stops on a number
/// chosen for its roundness stops at a different hour every time the population or the Ruleset moves,
/// and every occupancy reading is a statement about that hour. ***A city has a time of day, so a
/// reading has one too.*** <see cref="Days"/> Days at <see cref="Ticks.PerDay"/> is 491,520 Ticks,
/// and it lands at midnight.
/// </para>
/// <para>
/// 🔴 <b>And the paragraph above did not save this test from the failure it names.</b> It was 48
/// Days settling for 8, the ramp is about eighty Days long, and the whole tail therefore sat inside
/// it — <i>an artefact of stopping mid-ramp</i>, which is the exact phrase, one milestone later, on
/// the quantity it was written to protect. ***Choosing a round number is one way to stop mid-ramp
/// and measuring the ramp is the other***, and only the first was guarded against. See
/// <see cref="Days"/>.
/// </para>
/// <para>
/// 🔴 ⚠ <b>THE LEAK THIS RUN IS WRITTEN TO CATCH IS BARELY REACHABLE ON THE WORLD IT RUNS ON, and
/// that is the strongest thing measured here.</b> Counting how every parking holding <em>ended</em>
/// over 240 Days at <see cref="Population"/>: <b>56,969</b> ended because the driver drove away —
/// the designed release — and <b>79,273</b> ended because <em>the garage was demolished under the
/// parked car</em>. ***Demolition is the majority sink for parking on this Ruleset, at 58% of all
/// endings.*** <c>minimal.toml</c> decays, condemnation runs continuously, and a Car Park's row is
/// freed with the Building that carried it, so a held space is reaped long before it could
/// accumulate into anything. It is why no holder in a 640-Day run is ever more than <b>four</b> Days
/// stale, and why an injected leak that strands Citizens while parked makes <c>held</c> go
/// <em>down</em> rather than up.
/// </para>
/// <para>
/// ⚠ <b>So the summary at the top of this file — <i>a leaked space is capacity destroyed for
/// ever</i> — describes a trigger this world does not reach</b>, which is <c>adr/0093</c>'s failure
/// mode rather than a false claim: the sentence is right about the mechanism and silent about
/// whether anything here exercises it. The assertions below are kept because they cost seconds and
/// because the world will stop decaying; ***what must not happen is anybody reading a green run here
/// as evidence that parking does not leak.*** A world that holds its Buildings up is what would make
/// this run mean what it says.
/// </para>
/// <para>
/// <b>It runs on a Ruleset with cars, and every shipped file but one has none.</b>
/// <c>rulesets/minimal.toml</c> states no <c>car_ownership_percent</c> by design, so on it this whole
/// file would be vacuous — every Car Park empty for every Tick, every assertion below satisfied
/// perfectly. The vacuity guard is therefore first and is not a formality.
/// </para>
/// </remarks>
public sealed class ParkingLongRunTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    /// <summary>Whole Days, and enough of them that the tail is longer than the ramp.</summary>
    /// <remarks>
    /// <para>
    /// The city builds out over the first several Days — placement runs on a
    /// <c>revisit_ticks = 1024</c> cadence and jobs on another — so a run has to be long enough that
    /// what is left after the ramp is still a run.
    /// </para>
    /// <para>
    /// 🔴 <b>It was 48 Days against a <see cref="SettleDays"/> of 8, and the ramp it was settling for
    /// is about eighty Days long — so the whole of its tail sat inside the ramp.</b> A run that stops
    /// there is not measuring a steady state; it is comparing one part of a rising curve against a
    /// later part of the same rising curve, and the growth it reports is the curve rather than the
    /// city. That is what <see cref="AssertFlat"/>'s band was widened to 12.5% to accommodate on
    /// 2026-08-27, and the widening bought a longer ramp rather than a real drift.
    /// </para>
    /// <para>
    /// <b>Measured over 640 Days at <see cref="Population"/> on this Ruleset</b>, spaces held settle
    /// at about <b>275</b> from Day 80 onward and every eighty-Day block mean thereafter reads
    /// 269–281 with no trend: 264, 276, 273, 273, 277, 281, 269, 277. The structural quantities
    /// underneath are flat across the same span — Buildings 124, placed Households 294, employed 648,
    /// spaces 997 — so what ramps is the <em>share</em> of commutable Citizens holding a space at
    /// midnight, and it saturates at 67–68%. ***The drift filed as <c>plans/0045</c> queue item 5 is
    /// this ramp seen through too short a window, and there is no leak under it.***
    /// </para>
    /// </remarks>
    private const int Days = 240;

    /// <summary>
    /// How many Days at the front are the city being built rather than the city running.
    /// </summary>
    /// <remarks>
    /// <b>Eighty, because that is where the measured ramp ends</b>, and the number is a property of
    /// the city rather than a margin somebody liked the look of. At 8 the tail began inside the ramp
    /// and the test could only pass by widening its band; at 80 the tail is post-ramp and the band
    /// goes back to the 6.25% every sibling long-run test uses. ⚠ <b>It is a settle window and not a
    /// warm-up</b> — the Ticks are simulated either way, and all that changes is which readings are
    /// allowed to answer the question.
    /// </remarks>
    private const int SettleDays = 80;

    private const int Population = 2_000;

    /// <summary>
    /// <b>Nothing grows, no space is lost, and the conservation sum holds at the end.</b>
    /// </summary>
    /// <remarks>
    /// It prints its numbers before it asserts anything — 5c task 8's rule, because an acceptance run
    /// that speaks only on success is one you cannot use on the day it fails.
    /// </remarks>
    [Fact]
    public void The_two_hundred_and_forty_Day_parking_run()
    {
        (Reading[] readings, World world) = Run();
        Reading[] tail = readings[SettleDays..];

        _output.WriteLine($"{Days} Days at {Population} Citizens on minimal.toml with cars.");
        _output.WriteLine("");
        _output.WriteLine("   day   car park slots   spaces   held   holders");
        _output.WriteLine("   ---   --------------   ------   ----   -------");

        for (int day = 0; day < readings.Length; day++)
        {
            Reading reading = readings[day];

            _output.WriteLine(
                $"  {day + 1,4}   {reading.Slots,14}   {reading.Spaces,6}   {reading.Held,4}   "
                + $"{reading.Holders,7}");
        }

        // Vacuity first, and it is not a formality: on a Ruleset with no cars every assertion below
        // passes over a city in which nothing ever parked.
        Assert.True(Mean(tail, r => r.Held) > 0, "no car was ever parked, so this run asserts nothing.");

        // The collection half. First against last of the tail rather than a mean, because a slot
        // count is monotone by construction -- a mean says nothing and the only question is whether
        // it stopped climbing.
        Assert.Equal(tail[0].Slots, tail[^1].Slots);

        // The magnitude half, and adr/0003's extension of adr/0006 to quantities. Occupancy is
        // bounded above by capacity, so this cannot run away the way an unbounded accumulator can --
        // what it catches is the leak, which climbs toward the ceiling and stays there.
        AssertFlat(tail, r => r.Held, "spaces held");

        // And the invariant, which says the same thing in one number and from the other side: the
        // sum is against the Citizens holding, so a space held by nobody fails here whether or not
        // the trend above noticed it.
        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>One reading a Day, taken at midnight.</summary>
    private static (Reading[] Readings, World World) Run()
    {
        Ruleset rules = Driving();
        var key = WorldKey.FromSeed(GoldenFixtures.Seed);
        var world = new World(Population, rules, key);

        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        List<Reading> readings = [];

        for (int tick = 0; tick < Days * Ticks.PerDay; tick++)
        {
            simulation.Step(default);

            if ((tick + 1) % Ticks.PerDay != 0)
            {
                continue;
            }

            int spaces = 0;
            int held = 0;

            for (int slot = 0; slot < world.CarParks.Rows.SlotCount; slot++)
            {
                if (!world.CarParks.Rows.IsLive(slot))
                {
                    continue;
                }

                spaces += world.CarParks.Capacity[slot];
                held += world.CarParks.Occupied[slot];
            }

            int holders = 0;

            for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
            {
                if (world.Citizens.Rows.IsLive(slot)
                    && world.CarParks.Rows.TryResolve(world.Citizens.ParkedIn[slot], out _))
                {
                    holders++;
                }
            }

            readings.Add(new Reading(world.CarParks.Rows.SlotCount, spaces, held, holders));
        }

        return ([.. readings], world);
    }

    /// <summary><c>minimal.toml</c> with the one table it omits by design.</summary>
    /// <remarks>
    /// <b>The shipped file plus a table, rather than a fixture's own Ruleset</b>, so the lattice, the
    /// shed radius, the Commute Budget and the per-kind parking are the ones the project ships. The
    /// alternative — <c>congested.toml</c> — also states <c>[traffic]</c>, which would put the
    /// volume-delay loop inside an <c>adr/0006</c> run and make a trend in occupancy indistinguishable
    /// from a trend in congestion.
    /// </remarks>
    private static Ruleset Driving()
    {
        RulesetLoadResult parsed = RulesetLoader.Parse(
            File.ReadAllText(GoldenFixtures.RulesetPath)
            + "\n[households]\ncar_ownership_percent = 100\n",
            "driving.toml");

        Assert.True(parsed.Ok, parsed.Describe());

        return parsed.Ruleset!;
    }

    /// <summary>
    /// <b>The band is 6.25%, and the run beneath it is long enough to deserve it.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A PROPORTIONAL BAND GETS STRICTER AS THE QUANTITY SHRINKS, and that is what fired on
    /// 2026-08-27.</b> <c>CommuteEngine</c>'s direction guard cut held spaces by about 27% — a
    /// quarter of this city's commuting was journeys to where the Citizen already stood — and the
    /// growth underneath, unchanged in absolute size, then breached a band that had shrunk with the
    /// base. ***That observation was right and is kept.***
    /// </para>
    /// <para>
    /// 🔴 <b>The diagnosis attached to it was WRONG, and the band was widened to 12.5% on the
    /// strength of it.</b> The growth was read as a real pre-existing drift and filed as
    /// <c>plans/0045</c> queue item 5: twenty-Day means over 160 Days at <b>251 → 283</b> with the
    /// guard and <b>370 → 388</b> without. Both figures reproduce exactly. ***Neither of them is a
    /// drift.*** Extended to 640 Days the same series settles at about 275 from Day 80 and never
    /// rises again, and every quantity that generates it — Buildings, placed Households, employed
    /// Citizens, spaces — is flat across the whole span. <see cref="Days"/> carries the block means.
    /// </para>
    /// <para>
    /// ⚠ <b>The failing window was the shipped one and nothing else.</b> Sweeping the settle window
    /// against the run length on one 240-Day series, the original 6.25% band refuses exactly one
    /// combination — settle 8 over 48 Days, at +6% — and accepts every other, with the growth
    /// reaching 0% by a settle of 40 and turning negative by 60. ***A band is not the thing to move
    /// when the window is what is wrong***, and widening it here would have made this test blind to
    /// the leak it exists to catch at precisely the size that leak would first appear.
    /// </para>
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

    /// <summary>One Day's end.</summary>
    private readonly record struct Reading(int Slots, int Spaces, int Held, int Holders);
}
