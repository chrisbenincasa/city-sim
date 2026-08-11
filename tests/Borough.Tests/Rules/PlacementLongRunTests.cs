using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Tests.Golden;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>adr/0069</c>'s acceptance: the standing housing stock is <b>used</b>, so what is left in the
/// Unplaced Pool is a housing shortage rather than a missing mechanism.
/// </summary>
/// <remarks>
/// <para>
/// <b>The number this replaces.</b> Slice 10's 100,000-Tick run settled <em>five-sixths homeless</em>
/// and the finding was filed as a question about how many Occupants a Building should hold. It was
/// not: the populator put three Households in a Building and a Zone Rule put one, so every
/// demolish-and-rebuild cycle netted two into the Pool, and <em>nothing in the simulation could ever
/// move a Household into a Building that already stood</em>. <c>adr/0068</c> closed the first half by
/// making occupancy declarable; this closes the second.
/// </para>
/// <para>
/// <b>What is asserted is vacancy and not homelessness, and that distinction is the whole test.</b>
/// The shipped Ruleset demolishes every dwelling it raises — its own header says so, at length, and
/// says why — so a city whose Buildings house 190 of its 360 Households is behaving exactly as its
/// content states. Asserting *everybody is housed* would be asserting a property of
/// <c>rulesets/minimal.toml</c>'s balance, which that file explicitly declines to have. Asserting
/// *the places that exist are lived in* is a property of the <em>mechanism</em>, and it is the one
/// that was false: before this pass, 45% of the housing stock stood empty while 70% of the population
/// queued.
/// </para>
/// <para>
/// <b>Vacancy cannot reach zero and should not be asserted to.</b> A Building is raised empty and
/// fills over the following triggers, so a city that is continuously building carries a floor of
/// vacancy proportional to its construction rate. The bound here is a quarter of capacity, which the
/// run clears by a factor of two and a mechanism that had stopped would miss by a factor of two the
/// other way.
/// </para>
/// </remarks>
public sealed class PlacementLongRunTests
{
    private const int TickCount = 100_000;
    private const int Population = 1_000;
    private const int ReadEvery = 2_048;

    /// <summary>Readings discarded as the transient: the city opens fully built and fully housed.</summary>
    private const int SettleReadings = 4;

    [Fact]
    public void The_hundred_thousand_Tick_occupancy_run()
    {
        Reading[] readings = Run(out World world);
        Reading[] tail = readings[SettleReadings..];

        // Vacuity, and it is not a formality here: every assertion below is satisfied perfectly by a
        // city in which nothing was ever demolished and nobody ever needed housing.
        Assert.True(
            Mean(tail, r => r.Placed) > 0,
            "nothing was ever placed, so every assertion below is about a mechanism that did not run.");

        Assert.True(
            Mean(tail, r => r.Pool) > 0,
            "nobody was ever in the Unplaced Pool, so there was no queue to clear.");

        // The claim. Averaged over the tail rather than asserted per reading, because vacancy is a
        // stock that a burst of demolition raises for a few triggers and the pass then works off.
        long capacity = Mean(tail, r => r.Capacity);
        long vacant = Mean(tail, r => r.Capacity - r.Housed);

        Assert.True(
            vacant <= capacity / 4,
            $"{vacant} of {capacity} declared places stood empty on average while "
            + $"{Mean(tail, r => r.Pool)} Households queued. The housing stock is not being used, "
            + "which is what adr/0069's pass exists to do -- and is what the five-sixths-homeless "
            + "equilibrium of slice 10 actually was.");

        // And the residue, stated the other way round: what is left in the Pool is explained by
        // there being nowhere to put them. Without this, a pass that housed one family and left the
        // rest would satisfy the vacancy bound by keeping capacity tiny.
        long pool = Mean(tail, r => r.Pool);
        long unhousable = Mean(tail, r => r.Households - r.Capacity);

        Assert.True(
            pool <= unhousable + (capacity / 4),
            $"{pool} Households queued on average against {unhousable} more people than places. The "
            + "gap is families the city had room for and did not house.");
    }

    /// <summary>
    /// The pass keeps up: what it houses over the tail matches what demolition turns out.
    /// </summary>
    /// <remarks>
    /// <b>A rate rather than a level, and it catches what the level cannot.</b> A pass that houses
    /// steadily but a little slower than the city evicts produces a Pool that climbs for the whole
    /// run, which no single reading of vacancy would show — the stock stays used, and the queue behind
    /// it grows without bound. This is <c>adr/0006</c>'s magnitude half aimed at the one collection
    /// whose sink is a mechanism rather than a table.
    /// </remarks>
    [Fact]
    public void The_queue_does_not_grow()
    {
        Reading[] tail = Run(out _)[SettleReadings..];

        long early = Mean(tail[..(tail.Length / 2)], r => r.Pool);
        long late = Mean(tail[(tail.Length / 2)..], r => r.Pool);

        Assert.True(
            late <= early + (early / 16),
            $"the Unplaced Pool held {early} Households on average over the first half of the tail "
            + $"and {late} over the second. The city is evicting faster than it houses.");
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

    /// <summary>One interval's worth of the city, in the four quantities this test is about.</summary>
    private readonly record struct Reading(
        int Households, int Pool, int Capacity, int Housed, long Considered, long Placed);

    private static Reading Read(World world, PlacementActivity activity)
    {
        int capacity = 0;
        int housed = 0;

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (!world.Buildings.Rows.IsLive(slot))
            {
                continue;
            }

            capacity += world.Rules.Kind(world.Buildings.Kind[slot]).Occupants;
            housed += world.Occupants.Length(slot);
        }

        return new Reading(
            world.Households.Rows.LiveCount,
            world.UnplacedPool.Count,
            capacity,
            housed,
            activity.Considered.Sum,
            activity.Placed.Sum);
    }

    private static Reading[] Run(out World world)
    {
        var key = WorldKey.FromSeed(GoldenFixtures.Seed);

        world = new World(Population, GoldenFixtures.Rules());

        var simulation = new Simulation(world, key)
        {
            // O(world) twice per Tick against a phase meant to be O(woken). --no-decide-guard's
            // reason, and the guard's own correctness is covered by the tests written for it.
            VerifyDecideWritesNothing = false,
        };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        List<Reading> readings = [];

        for (int tick = 0; tick < TickCount; tick++)
        {
            simulation.Step(default);

            if ((tick + 1) % ReadEvery == 0)
            {
                readings.Add(Read(world, simulation.Placement.Drain()));
            }
        }

        return [.. readings];
    }
}
