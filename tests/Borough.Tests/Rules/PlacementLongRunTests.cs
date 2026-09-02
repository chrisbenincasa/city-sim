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
/// <b>The number this replaces, and the number it replaces it with.</b> Slice 10's 100,000-Tick run was
/// filed as <em>five-sixths homeless</em>, and that was the wrong quantity to record. Homelessness in
/// this fixture is <c>1 − capacity ÷ population</c> over a Ruleset that condemns every dwelling 64
/// Ticks after raising it on purpose, so it would have read much the same at any occupancy and against
/// any placement mechanism. <b>The number that was evidence of a defect is the vacancy</b>: 45% of the
/// declared housing stock stood empty while 70% of the population queued outside it, which no balance
/// of rates produces. <c>adr/0068</c> made occupancy declarable and <c>adr/0069</c> built the door;
/// vacancy is now 10%.
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
        long tolerance = Tolerance(world, capacity);

        Assert.True(
            vacant <= tolerance,
            $"{vacant} of {capacity} declared places stood empty on average while "
            + $"{Mean(tail, r => r.Pool)} Households queued. The housing stock is not being used, "
            + "which is what adr/0069's pass exists to do -- and is what slice 10 recorded as a "
            + "homelessness figure when it was a vacancy.");

        // And the residue, stated the other way round: what is left in the Pool is explained by
        // there being nowhere to put them. Without this, a pass that housed one family and left the
        // rest would satisfy the vacancy bound by keeping capacity tiny.
        long pool = Mean(tail, r => r.Pool);
        long unhousable = Mean(tail, r => r.Households - r.Capacity);

        Assert.True(
            pool <= unhousable + tolerance,
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
        var drifts = new List<long>();
        var report = new List<string>();

        foreach (ulong seed in Seeds)
        {
            Reading[] tail = Run(seed, out _)[SettleReadings..];

            long early = Mean(tail[..(tail.Length / 2)], r => r.Pool);
            long late = Mean(tail[(tail.Length / 2)..], r => r.Pool);
            long drift = ((late - early) * 1_000) / early;

            drifts.Add(drift);
            report.Add($"seed {seed}: {early} then {late} ({drift / 10}.{Abs(drift) % 10}%)");
        }

        long mean = 0;

        foreach (long drift in drifts)
        {
            mean += drift;
        }

        mean /= drifts.Count;

        // 62 per mille rather than 63: the bound is a sixteenth, expressed where the arithmetic is.
        Assert.True(
            mean <= 62,
            $"the Unplaced Pool grew by {mean / 10}.{Abs(mean) % 10}% on average across "
            + $"{drifts.Count} world seeds. The city is evicting faster than it houses.\n"
            + string.Join("\n", report));
    }

    private static long Abs(long value) => value < 0 ? -value : value;

    /// <summary>
    /// The world seeds this claim is asserted over, and it is asserted over their <b>mean</b> rather
    /// than over each. The golden fixture's is first, so its reading is always in the report.
    /// </summary>
    /// <remarks>
    /// <b>One trajectory cannot carry a claim about a trend, and this test asserted one until
    /// 2026-08-13.</b> Scoping the generator to developed land moved the golden seed's drift from
    /// +0.5% to +8.9% and left ten other seeds between −3.4% and +1.5% — a <em>tighter</em> spread
    /// than the same ten give on the old geometry, so the city had not got worse and the test could
    /// not tell. <b>A single draw cannot distinguish a regression from a trajectory</b>, which is the
    /// corpus's own <c>--roads</c> finding — <i>a generator whose output cannot be varied cannot be
    /// characterised</i> — arriving at a test rather than at an instrument.
    /// <para>
    /// <b>The mean and not each, which is the whole repair.</b> Asserting the bound per seed puts the
    /// verdict back on the worst trajectory and fails for the same reason as before. The claim being
    /// made is about the <em>city</em> — does its queue grow — and the quantity that states it is the
    /// average drift over independent worlds. A city that genuinely evicts faster than it houses
    /// moves every seed and therefore the mean; one bad draw moves the mean by a fifth of itself.
    /// </para>
    /// <para>
    /// <b>Five rather than ten, and the cost is why.</b> Each is a 100,000-Tick run. Five is enough
    /// that no single trajectory carries the verdict, which is the property that was missing; it is
    /// not enough to characterise the distribution, and nothing here claims to. Every seed's reading
    /// is printed on failure, so a diverging city is diagnosable rather than merely reported.
    /// </para>
    /// </remarks>
    private static ulong[] Seeds => [GoldenFixtures.Seed, 1, 8, 34, 89];

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

            // 🔴 A SHELL IS A LIVE ROW AND NOT A PLACE, and this loop counted it as one until
            // milestone 17. adr/0091 leaves a condemned Building standing rather than freeing its
            // row, so `IsLive` stopped meaning "somebody could move in here" on the day clearing
            // became a purchase -- and the reading went unexamined because no shipped world
            // condemned anything once the fixture stopped declining.
            //
            // ***The vacancy this test exists to refuse is a place that COULD be filled and is not.***
            // A shell is a place that cannot be, so counting it makes the denominator a statement
            // about blight and the ratio a statement about nothing. The same sentence is already
            // written in PlacementEngine.TryHouse, which skips shells rather than spending a look on
            // one -- this is that fix arriving in the measurement, one layer up.
            if (world.Buildings.IsAbandoned(slot))
            {
                continue;
            }

            // HOUSING capacity and not TENANT capacity (adr/0148). One ceiling covers both kinds
            // of tenant, so a Building holding a shop houses that many families fewer -- and this
            // reading is compared against `housed`, which counts Households alone. Taking the live
            // count rather than the kind's declaration is deliberate: a Building may hold a second
            // shop that walked in through adr/0147's placement pass.
            //
            // ⚠ The ceiling is DERIVED as of plans/0053 -- floor area over [capacity]
            // floor_tiles_per_occupant -- so it is asked of the BUILDING and differs Building by
            // Building with the ground each stands on. A reading taken off the kind would have been
            // one number repeated, which is what made this column worth nothing to look at.
            capacity += world.DeclaredOccupancy(slot) - world.BuildingBusinesses.Length(slot);
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

    private static Reading[] Run(out World world) => Run(GoldenFixtures.Seed, out world);

    /// <summary>
    /// The vacancy this test tolerates: a quarter of capacity, widened by the share of the city's
    /// land that cannot hold a home.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A quarter was the whole of it until <c>adr/0165</c>, and the split is what made the
    /// denominator wrong rather than the fraction.</b> The old bound read <c>capacity / 4</c>, which
    /// is a tolerance on the vacancy floor a continuously-building city carries — and it was
    /// calibrated on a world where <em>every</em> Lot admitted a dwelling. Once one block in
    /// <c>SyntheticCity.TradeBlockStride</c> is permitted only to a trade, the same fraction is being
    /// asked of a city with less housing land, and it fails for a reason that is nothing to do with
    /// the mechanism the test is about.
    /// </para>
    /// <para>
    /// ⚠ <b>MEASURED, and it is the housing Lot count that vacancy tracks — not capacity, and not
    /// commercial share.</b> A sweep on this fixture: <b>134</b> housing Lots → <b>18.5%</b> vacancy,
    /// <b>126</b> → <b>21.9%</b>, <b>124</b> → <b>25.0%</b>. The city is 134 Lots and near-saturated,
    /// so ten Lots is a large fraction of it. ***An <c>adr/0055</c> reading — that commercial land
    /// dilutes the Zone Rule's sample — was proposed and is refuted by the same sweep***, because a
    /// stride of 4 carries four times the trade land and yields <em>lower</em> vacancy: it also
    /// yields a wider lattice and two more housing Lots. The cost is land, not wasted looks.
    /// </para>
    /// <para>
    /// <b>On a world with no split this is exactly the old number, which is the property worth
    /// having.</b> Every Lot admits a dwelling there, so the ratio is one and the bound is
    /// <c>capacity / 4</c> to the digit — verified against <c>main</c>'s city, which reads 35 of 189.
    /// It widens only in proportion to the housing land actually removed, so a future change to the
    /// land-use mix moves the bound <em>with</em> the city instead of against it.
    /// </para>
    /// <para>
    /// 🔴 ⚠ <b>CHOSEN AND NOT DERIVED, and the measurement above is what says so.</b> The observed
    /// rise is <em>steeper</em> than inversely proportional to housing land — scaling 18.5% by
    /// 134/124 predicts 20.0% where the run gives 25.0% — so this scaling under-corrects, and the
    /// margin it leaves is **3 places on the vacancy bound and 2 on the residue** rather than
    /// <c>main</c>'s 12. ***Nobody has derived why vacancy rises faster than the land it lost***, and
    /// until somebody does, a change that eats those three places will read as a placement regression
    /// when it may be a land one. That is the reading to distrust first.
    /// </para>
    /// </remarks>
    private static long Tolerance(World world, long capacity)
    {
        long lots = 0;
        long housing = 0;

        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (!world.Lots.Rows.IsLive(slot))
            {
                continue;
            }

            lots++;

            if ((world.Lots.Zone[slot] & LotTable.Housing) != 0)
            {
                housing++;
            }
        }

        // A city with no housing land at all is a different failure, and the caller's own vacuity
        // assertions catch it; falling back to the old bound keeps this from dividing by zero.
        return housing == 0 ? capacity / 4 : capacity * lots / (4 * housing);
    }

    private static Reading[] Run(ulong seed, out World world)
    {
        var key = WorldKey.FromSeed(seed);

        world = new World(Population, GoldenFixtures.DecliningRules());

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
