using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Tests.Golden;

namespace Borough.Tests.Rules;

/// <summary>
/// Slice 10 task 10: the <b><c>slots</c></b> half of slice 5 task 7's trend assertion, over 100,000
/// Ticks of a city that is demolishing and rebuilding itself.
/// </summary>
/// <remarks>
/// <para>
/// <b>The claim is <c>adr/0006</c> reaching the one place nothing has ever been able to check it.</b>
/// The assertion was written in slice 5 as <em><c>slots</c> trending upward against a flat
/// <c>live</c></em> — a table growing to the high-water mark of a <em>cycle</em> rather than to the
/// size of the city, because freed rows are appended beside rather than handed back out. Slice 7
/// could not carry it: a Rule Instance's life is its Building's and subscription allocates nothing,
/// so no Ruleset could make a slot count move. **What churns rows is Buildings arriving and being
/// demolished, and that is this slice.**
/// </para>
/// <para>
/// <b>The reading interval is derived from the Zone Rule's trigger period rather than chosen</b>,
/// which is what makes the trigger assertion exact instead of a trend line. An interval that cut a
/// period in half would put a different number of triggers in each reading and the whole thing would
/// have to be softened to a band — and a band would then hide the very drift it was softened to
/// tolerate. Reading it out of the Ruleset rather than writing 2,048 here is the difference between
/// an assertion that follows the file and one that silently desynchronises when somebody retunes it.
/// </para>
/// <para>
/// <b>Every table is read, not just the ones that were expected to churn.</b> The failure being
/// hunted is a row leak, and a leak arrives in whichever table the author was not thinking about —
/// slice 10 has already produced one recycled-row defect that had been latent since slice 4 and one
/// index-encoding defect that produced a plausible city while condemning the wrong Building. Both
/// were invisible in aggregate. Naming each table separately is what makes the failure message say
/// where.
/// </para>
/// <para>
/// <b>The Unplaced Pool is the only table here that is asserted differently, and the difference is a
/// property of the table rather than a concession.</b> Every other one holds one row per Lot, so its
/// high-water mark is reached at world creation and flatness is testable from the first reading. The
/// Pool's slot count is a running <em>maximum</em> over a stochastic quantity — how many Households
/// happen to be homeless at once — so it creeps upward while the run is still finding the largest
/// eviction cohort it will ever see, and it did: 300, 304, 305, 309, 311, 312, then 312 for the last
/// 37,000 Ticks. Two things are asserted instead. The <b>ceiling</b>, which is the population, because
/// a Household is in the Pool at most once — a structural bound rather than an observed one, and the
/// reason <c>adr/0006</c> holds here for a reason that has nothing to do with a sink. And
/// <b>convergence as a rate</b>, because the plateau arrives just past the midpoint of the tail, so
/// any <em>flat after reading N</em> would be an N picked because the data has that shape.
/// </para>
/// <para>
/// <b>This is not slice 7's acceptance run and does not replace it.</b> That one owns the
/// <em>flow</em> half — evaluations rising against a flat <c>due</c> — and states so. The two runs
/// cost the same and assert disjoint things; merging them would produce a test whose failure message
/// could mean either.
/// </para>
/// </remarks>
public class ZoneRuleLongRunTests
{
    private const int TickCount = 100_000;
    private const int Population = 1_000;

    /// <summary>
    /// Trigger periods per reading. <b>The multiplier, not the interval</b> — the interval itself is
    /// the Ruleset's, multiplied by this.
    /// </summary>
    private const int PeriodsPerReading = 64;

    /// <summary>
    /// How much of the run is discarded as the transient, in readings. The city opens fully built and
    /// fully housed, so what settles is the arming stagger and the first cohort of condemnations.
    /// </summary>
    private const int SettleReadings = 2;

    [Fact]
    public void The_hundred_thousand_Tick_slots_run()
    {
        Reading[] readings = Run(out World world, out Reading opening);
        Reading[] tail = readings[SettleReadings..];

        // Vacuity first, and it is the whole reason this test can be trusted. Every assertion below
        // is of the form "this did not grow", which a run in which nothing happened passes perfectly.
        Assert.True(
            Moved(tail, r => r.Buildings.Live),
            "the live Building count never moved across the tail, so nothing was demolished or built "
            + "and every slot-count assertion below is vacuous.");

        Assert.True(
            tail[^1].Pool.Live > 0 || Moved(tail, r => r.Pool.Live),
            "no Household was ever in the Unplaced Pool, so eviction and rehousing never ran.");

        // Exactly this many triggers in every reading, which is what the derived interval buys. A
        // Zone Rule fires on a fixed period and nothing can stop it firing, so a reading covering a
        // whole number of periods covers a fixed number of triggers — and a drift here means the
        // sweep started skipping, which no assertion about what it *did* would notice.
        foreach (Reading reading in tail)
        {
            Assert.Equal(PeriodsPerReading * world.Rules.ZoneRules.Length, (int)reading.Triggers);
        }

        // The claim itself, one table at a time. SlotCount never falls — a table does not shrink —
        // so equality against the first tail reading is flatness proved rather than sampled.
        Flat(tail, "Lots", r => r.Lots.Slots);
        Flat(tail, "Buildings", r => r.Buildings.Slots);
        Flat(tail, "Households", r => r.Households.Slots);
        Flat(tail, "Bins", r => r.Bins.Slots);
        Flat(tail, "Rule Instances", r => r.Instances.Slots);

        // And the transient, which the tail cannot speak for: a table that grew once while the first
        // cohort was being condemned and then held would pass every line above.
        NoLargerThanOpening(readings, opening, "Buildings", r => r.Buildings.Slots);
        NoLargerThanOpening(readings, opening, "Bins", r => r.Bins.Slots);
        NoLargerThanOpening(readings, opening, "Rule Instances", r => r.Instances.Slots);

        // The Pool is asserted differently, and the difference is a property of the table rather
        // than a concession. Every other table above holds one row per Lot, so its high-water mark
        // is reached at world creation and flatness is testable from the first reading. The Pool's
        // slot count is a running *maximum* over a stochastic quantity — how many Households happen
        // to be homeless at once — so it legitimately creeps upward while the run is still finding
        // the largest eviction cohort it will ever see. Demanding flatness from the first reading
        // would be demanding that a maximum be reached before it has been sampled.
        foreach (Reading reading in readings)
        {
            Assert.True(
                reading.Pool.Slots <= reading.Households.Live,
                $"the Unplaced Pool holds {reading.Pool.Slots} rows against {reading.Households.Live} "
                + "live Households. A Household is in the Pool at most once, so the population is the "
                + "Pool's ceiling — and that is why adr/0006 holds here for a reason that has nothing "
                + "to do with the sink, and why it expires the day immigration arrives.");
        }

        // The convergence, stated as a rate rather than as a window. The plateau in this run arrives
        // at tail reading 26 of 45 — just past the midpoint — so any "flat after reading N" would be
        // an N chosen because the data has that shape, which is the thing a tripwire must not be.
        // A rate has no such freedom: a running maximum that has converged grows by a fraction of a
        // percent across the back half of a run, and a table that is leaking rows grows by orders of
        // magnitude, because a hundred thousand Ticks of demolition would allocate thousands.
        int firstHalf = Max(tail[..(tail.Length / 2)], r => r.Pool.Slots);
        int secondHalf = Max(tail[(tail.Length / 2)..], r => r.Pool.Slots);

        Assert.True(
            secondHalf <= firstHalf + (firstHalf / 32),
            $"the Unplaced Pool's high-water mark went from {firstHalf} rows over the first half of "
            + $"the tail to {secondHalf} over the second. A running maximum over a bounded quantity "
            + "converges; one that is still climbing after 50,000 Ticks is freed rows not being "
            + "handed back out.");

        // And the Pool as a *level*, which is the one thing here that is about the city rather than
        // about the tables. A Household leaves the Pool only when a Zone Rule builds somewhere it
        // can go, so a Pool that fills monotonically is rehousing having quietly stopped — visible
        // in no slot count, because the rows were already allocated.
        long earlyPool = Mean(tail[..(tail.Length / 2)], r => r.Pool.Live);
        long latePool = Mean(tail[(tail.Length / 2)..], r => r.Pool.Live);

        Assert.True(
            latePool <= earlyPool + (earlyPool / 32),
            $"the Unplaced Pool held {earlyPool} Households on average over the first half of the "
            + $"tail and {latePool} over the second. The city is evicting faster than it rehouses.");

        new Simulation(world, WorldKey.FromSeed(GoldenFixtures.Seed)).CheckEndOfRun();
    }

    /// <summary>Every reading's value equals the first one's.</summary>
    private static void Flat(Reading[] tail, string table, Func<Reading, int> of)
    {
        int opening = of(tail[0]);

        for (int i = 1; i < tail.Length; i++)
        {
            Assert.True(
                of(tail[i]) == opening,
                $"the slot count for {table} went from {opening} to {of(tail[i])} by reading {i} of "
                + $"{tail.Length - 1}. A table whose slot count grows against a city of bounded size "
                + "is freed rows not being handed back out, which is adr/0006's collection half.");
        }
    }

    /// <summary>No reading, transient included, exceeds the size the city opened at.</summary>
    private static void NoLargerThanOpening(
        Reading[] readings, Reading opening, string table, Func<Reading, int> of)
    {
        int allowed = of(opening);

        foreach (Reading reading in readings)
        {
            Assert.True(
                of(reading) <= allowed,
                $"the slot count for {table} reached {of(reading)} against {allowed} at the opening. "
                + "The populator builds one Building per Lot, so the opening count is the size of "
                + "the city and nothing the run does may need more rows than that.");
        }
    }

    private static bool Moved(Reading[] readings, Func<Reading, int> of)
    {
        foreach (Reading reading in readings)
        {
            if (of(reading) != of(readings[0]))
            {
                return true;
            }
        }

        return false;
    }

    private static int Max(Reading[] readings, Func<Reading, int> of)
    {
        int most = 0;

        foreach (Reading reading in readings)
        {
            if (of(reading) > most)
            {
                most = of(reading);
            }
        }

        return most;
    }

    private static long Mean(Reading[] readings, Func<Reading, int> of)
    {
        long total = 0;

        foreach (Reading reading in readings)
        {
            total += of(reading);
        }

        return total / readings.Length;
    }

    /// <summary>One table's two numbers: how many rows exist, and how many are in use.</summary>
    private readonly record struct Size(int Slots, int Live);

    /// <summary>The whole world's row counts at one instant, plus what the sweep did to get there.</summary>
    private readonly record struct Reading(
        Size Lots, Size Buildings, Size Households, Size Bins, Size Instances, Size Pool,
        long Triggers);

    private static Reading Read(World world, long triggers) => new(
        new Size(world.Lots.Rows.SlotCount, world.Lots.Rows.LiveCount),
        new Size(world.Buildings.Rows.SlotCount, world.Buildings.Rows.LiveCount),
        new Size(world.Households.Rows.SlotCount, world.Households.Rows.LiveCount),
        new Size(world.Bins.Rows.SlotCount, world.Bins.Rows.LiveCount),
        new Size(world.RuleInstances.Rows.SlotCount, world.RuleInstances.Rows.LiveCount),
        new Size(world.UnplacedPool.Rows.SlotCount, world.UnplacedPool.Count),
        triggers);

    private static Reading[] Run(out World world, out Reading opening)
    {
        var key = WorldKey.FromSeed(GoldenFixtures.Seed);

        world = new World(Population, LayerRuleset.Default, GoldenFixtures.Rules());

        Simulation simulation = new(world, key)
        {
            // O(world) twice per Tick against a phase meant to be O(woken). --no-decide-guard's
            // reason, and the guard's own correctness is covered by the tests written for it.
            VerifyDecideWritesNothing = false,
        };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        // Derived from the file, so retuning the Ruleset moves the reading interval with it rather
        // than leaving the exact trigger assertion quietly measuring a sampling phase.
        int readEvery = (int)world.Rules.ZoneRules[0].Interval * PeriodsPerReading;

        opening = Read(world, 0);

        List<Reading> readings = [];

        for (int tick = 0; tick < TickCount; tick++)
        {
            simulation.Step(default);

            if ((tick + 1) % readEvery == 0)
            {
                readings.Add(Read(world, simulation.Zoning.Drain().Triggers.Sum));
            }
        }

        return [.. readings];
    }
}
