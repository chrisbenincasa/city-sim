using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Golden;

namespace Borough.Tests.Movement;

/// <summary>
/// Milestone 5c task 5: <b>who drives, and what changes when they do</b> — <c>01 §8</c> ledger #3's
/// exogenous half, and the vehicular Leg it exists to make possible.
/// </summary>
/// <remarks>
/// <para>
/// <b>Task 5's brief called for a drive Leg and the thing that actually blocked it was that nobody
/// decides who drives.</b> Mode choice appears in no milestone in <c>06</c> <em>and</em> in none of
/// its <i>Mechanisms with no milestone</i> rows, because that inventory's own opening line is
/// <i>every row below is settled by an ADR</i> — so it is <c>adr/0070</c>'s <b>undesigned</b> class
/// rather than its <em>unbuilt</em> one. ⚠ <b>An inventory of unplaced mechanisms structurally cannot
/// list a mechanism nobody designed</b>, which is why this reached a task before anybody noticed, and
/// it is the fourth consecutive milestone to find a precondition it had not finished counting.
/// </para>
/// <para>
/// <b>What is built here is <em>ownership</em>, not choice.</b> <c>01 §8</c> already names the simple
/// assumption in its own words and defers the interesting one to a mechanism that has no milestone
/// either: <i>every Household owning a car is the simple assumption… only becomes interesting once
/// transit exists</i>. So this follows the design rather than compensating for its absence.
/// </para>
/// </remarks>
public sealed class CarOwnershipTests
{
    private const int Population = 4_000;

    /// <summary>A population at which the Commute Budget actually refuses somebody a job.</summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>64,000, AND THE READING THAT SETS IT IS <c>Beyond</c> AND NOT EMPLOYMENT.</b>
    /// <see cref="EmploymentActivity.Beyond"/> counts candidate vacancies refused for exceeding the
    /// ceiling, so it <em>is</em> the Budget binding, stated directly. Measured on
    /// <c>minimal.toml</c> over <see cref="Ticks"/>, a walker's <c>Beyond</c>: <b>0 at 16,000, 0 at
    /// 24,000, 36 at 32,000, 1,124 at 48,000, 4,920 at 64,000</b>. A driver's: <b>0 at every one of
    /// them.</b>
    /// </para>
    /// <para>
    /// ⚠ <b>16,000 REFUSED NOBODY, AND THIS CLASS ALREADY KNEW THAT SHAPE OF MISTAKE.</b> It moved
    /// off 4,000 on 2026-09-01 because the mechanism was inert there, and landed on a population
    /// where the Budget also refuses nobody — not one walker even reaches the unsavoury rung at
    /// 16,000. ***The escape from an inert fixture was itself inert***, and it stayed invisible for
    /// three days because the reading it escaped to saturates. <c>plans/0060</c>.
    /// </para>
    /// <para>
    /// ⚠ <b>32,000 is where it first binds and 36 refusals is not a demonstration</b>, which is why
    /// this is not the cheapest rung that is nonzero. The pair of runs costs about <b>6 s</b> at
    /// 64,000, so the cheap rung buys nothing worth the ambiguity.
    /// </para>
    /// </remarks>
    private const int BudgetBinds = 64_000;
    private const int Ticks = 2_048;
    private const int HashEvery = 64;

    /// <summary>The shipped Ruleset with a <c>[households]</c> table appended.</summary>
    /// <remarks>
    /// Appended rather than substituted, because the shipped file states no such table — which is
    /// itself one of the things under test, since the absence has to keep meaning <em>nobody
    /// drives</em>.
    /// </remarks>
    private static Ruleset WithOwnership(int percent)
    {
        string toml = File.ReadAllText(GoldenFixtures.RulesetPath);

        Assert.DoesNotContain("[households]", toml, StringComparison.Ordinal);

        RulesetLoadResult result = RulesetLoader.Parse(
            $"{toml}\n[households]\ncar_ownership_percent = {percent}\n", "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    private static Simulation Run(Ruleset rules, int population = Population, int ticks = Ticks)
    {
        InputLogBuilder builder = new(
            GoldenFixtures.Seed, new WorldConfiguration(population), GoldenFixtures.RulesetHash);

        builder.Append(new Core.Quantities.Ticks(0), new Command(CommandKind.Populate, default, default));

        InputLog log = builder.Build();
        Simulation simulation = Replay.Start(log, rules);

        Replay.Trace(simulation, log, new Core.Quantities.Ticks((ulong)ticks), HashEvery, []);

        return simulation;
    }

    /// <summary>Every live Household's mode, indexed by its never-reused id.</summary>
    private static Dictionary<ulong, bool> Ownership(World world)
    {
        Dictionary<ulong, bool> owners = [];
        HouseholdRuleset rules = world.Rules.Households;

        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (!world.Households.Rows.IsLive(slot))
            {
                continue;
            }

            ulong id = world.Households.Rows.IdAt(slot);
            owners[id] = rules.OwnsCar(world.Key, id);
        }

        return owners;
    }

    /// <summary>How many Legs of each mode a run <em>created</em>.</summary>
    /// <remarks>
    /// ⚠ <b>Created, not resident, and scanning the table would have measured almost nothing.</b> A
    /// Trip that ends is released and its Legs with it, and on this fixture a commute is sub-Tick —
    /// it is created and completed inside one call to phase 4 — so a walk over
    /// <see cref="World.Legs"/> at the end of a run finds the handful still in flight and reports zero
    /// for a city that made thousands of journeys. ***A table scan counts what survives, and a flow is
    /// the thing that did not.***
    /// </remarks>
    private static (long Foot, long Car) LegModes(Simulation simulation)
    {
        TripActivity trips = simulation.Trips.Drain();

        return (trips.WalkLegs.Sum, trips.DriveLegs.Sum);
    }

    // ---- who drives -----------------------------------------------------------------------------

    /// <summary>
    /// <b>A Ruleset with no <c>[households]</c> puts nobody in a car, and that is what keeps this
    /// whole task off the golden baselines.</b>
    /// </summary>
    /// <remarks>
    /// The shipped Rulesets state no such table, so under <c>05 §4</c> everything 5c task 5 changed is
    /// an <em>optimisation</em> on the committed session however it was motivated — a mode threaded
    /// through six signatures, a new subgraph selector, a new Ruleset table, and not one State Hash
    /// moved. <b>That is a safety net rather than a result</b>: it says the walk path is untouched, and
    /// it says nothing at all about whether the drive path is right.
    /// </remarks>
    [Fact]
    public void A_city_with_no_households_table_travels_entirely_on_foot()
    {
        Simulation simulation = Run(GoldenFixtures.Rules());

        Assert.False(simulation.World.Rules.Households.Runs);

        (long foot, long car) = LegModes(simulation);

        Assert.Equal(0, car);
        Assert.True(foot > 0, "the run produced no Legs at all, so it tested nothing.");

        foreach (bool owns in Ownership(simulation.World).Values)
        {
            Assert.False(owns);
        }
    }

    /// <summary>
    /// ⚠ <b>At 100% ownership there are exactly <em>twice</em> as many walk Legs as drive Legs, and at
    /// 0% there are none at all.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>adr/0008</c>: <b>a car commute is never one Leg, it is at minimum
    /// <c>walk → drive → walk</c></b>. The two flanking walks run from the pedestrian Access Point to
    /// the vehicle one, which are equal by construction — so they cost <b>zero</b> and cross no
    /// Segment, and they exist anyway. ⚠ <b>Milestone 7 task 5 gave a car somewhere else to be</b>
    /// (this sentence said <em>milestone 8</em> before the renumber), so a flanking walk now runs to
    /// the Car Park the driver holds and may cost something. <b>What this test asserts is the Leg
    /// <em>count</em> and not its cost</b>, which is why it is unaffected either way.
    /// </para>
    /// <para>
    /// ⚠ <b>This assertion is the one that catches the mistake this task nearly shipped.</b> The first
    /// cut built a car commute as one door-to-door Leg, reasoning from
    /// <see cref="World.VehicleAccessPoint"/>'s doc-comment — which forbids a <em>fallback</em> from an
    /// exhausted Parking Shed, and says nothing about the Leg count. ***A doc-comment forbidding one
    /// shape is not a decision permitting the others.*** A ratio is asserted rather than a mere
    /// presence, because <em>some walks exist</em> would have passed on the broken build too.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void The_ends_of_the_rate_put_the_whole_city_in_one_mode(int percent)
    {
        Simulation simulation = Run(WithOwnership(percent));

        (long foot, long car) = LegModes(simulation);

        Assert.True(foot + car > 0, "the run produced no Legs at all, so it tested nothing.");

        if (percent == 0)
        {
            Assert.Equal(0, car);
            return;
        }

        Assert.True(car > 0, "nobody drove in a city where every Household keeps a car.");
        Assert.Equal(car * 2, foot);
    }

    /// <summary>
    /// <b>A middling rate produces both modes, in roughly the share the file states.</b>
    /// </summary>
    /// <remarks>
    /// The tolerance is on the <em>Households</em>, not on the Legs. Leg counts are a share of
    /// <em>employed</em> Citizens and employment is not independent of the mode — a driver reaches
    /// more vacancies — so a Leg-share assertion would be measuring the labour market and reporting
    /// it as a draw.
    /// </remarks>
    [Fact]
    public void A_middling_rate_draws_about_the_share_the_file_states()
    {
        Simulation simulation = Run(WithOwnership(60));

        Dictionary<ulong, bool> owners = Ownership(simulation.World);
        int drivers = owners.Values.Count(owns => owns);

        Assert.True(owners.Count > 100, $"only {owners.Count} Households; the draw is not measurable.");

        double share = (double)drivers / owners.Count;

        Assert.InRange(share, 0.55, 0.65);

        (long foot, long car) = LegModes(simulation);

        Assert.True(foot > 0 && car > 0, $"the city produced {foot} walk Legs and {car} drive Legs.");

        // Two flanking walks per drive plus one per walker, so walk Legs exceed twice the drives by
        // exactly the number of foot commutes -- which is the arithmetic that says both kinds of Trip
        // are being made and neither is silently taking the other's shape.
        Assert.True(
            foot > car * 2,
            $"{foot} walk Legs against {car} drives leaves no room for a foot commute.");
    }

    // ---- why it is derived rather than stored ---------------------------------------------------

    /// <summary>
    /// ⚠ <b>Lowering the rate never gives a Household a car, and this is the whole argument for
    /// deriving ownership instead of storing it.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Ownership is <c>draw % 100 &lt; rate</c> over a draw that does not move, so the set of owners
    /// at a lower rate is a <b>subset</b> of the set at a higher one. That is what makes the key
    /// genuinely hot-reloadable: a designer nudging 60 to 55 takes cars from about a sixteenth of the
    /// owners and disturbs nobody else.
    /// </para>
    /// <para>
    /// <b>Both alternatives are worse and they fail in opposite directions.</b> A saved column
    /// re-rolled on reload would churn the entire city for a one-point change. A saved column
    /// <em>not</em> re-rolled would leave every standing Household carrying the old file's opinion,
    /// which is <c>adr/0064</c>'s frozen-at-construction defect and would make a key in a
    /// hot-reloadable file silently world-creation-fixed.
    /// </para>
    /// </remarks>
    [Fact]
    public void Lowering_the_rate_only_ever_takes_a_car_away()
    {
        World world = Run(WithOwnership(100)).World;
        int[] rungs = [100, 80, 60, 40, 20, 0];

        Dictionary<ulong, bool>? above = null;

        foreach (int percent in rungs)
        {
            var rules = new HouseholdRuleset(percent, Money.Zero, Money.Zero);
            Dictionary<ulong, bool> here = [];

            for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
            {
                if (!world.Households.Rows.IsLive(slot))
                {
                    continue;
                }

                ulong id = world.Households.Rows.IdAt(slot);
                here[id] = rules.OwnsCar(world.Key, id);
            }

            if (above is not null)
            {
                foreach ((ulong id, bool owns) in here)
                {
                    Assert.False(
                        owns && !above[id],
                        $"Household {id} acquired a car as the rate fell to {percent}%.");
                }
            }

            above = here;
        }

        Assert.NotNull(above);
        Assert.DoesNotContain(above!.Values, owns => owns);
    }

    /// <summary>
    /// <b>A Household's car does not come and go with the clock.</b>
    /// </summary>
    /// <remarks>
    /// The draw's Tick coordinate is <see cref="Core.Quantities.Ticks.Zero"/> — the second such tag,
    /// after <c>CommuteDeparture</c> — because this answers <i>what sort of Household is this</i>
    /// rather than <i>what happens now</i>. A car that re-rolled every Tick would be a household
    /// selling and repurchasing a vehicle continuously, which is neither the simple assumption nor
    /// the endogenous mechanism <c>01 §8</c> defers.
    /// </remarks>
    [Fact]
    public void Ownership_is_the_same_answer_early_and_late_in_a_run()
    {
        Ruleset rules = WithOwnership(60);

        Dictionary<ulong, bool> early = Ownership(Run(rules, ticks: 64).World);
        Dictionary<ulong, bool> late = Ownership(Run(rules, ticks: Ticks).World);

        int shared = 0;

        foreach ((ulong id, bool owns) in early)
        {
            if (!late.TryGetValue(id, out bool then))
            {
                continue;
            }

            shared++;
            Assert.Equal(owns, then);
        }

        Assert.True(shared > 100, $"only {shared} Households survived both runs; nothing was compared.");
    }

    // ---- what a drive is ------------------------------------------------------------------------

    /// <summary>
    /// <b>A drive Leg is priced on the car subgraph and is quicker than the walk it replaces.</b>
    /// </summary>
    /// <remarks>
    /// ⚠ <b>It is quicker by roughly the speed ratio and no more, because nothing slows it down.</b>
    /// A drive is free-flow door to door: no parking, no junction delay, and — until 5c task 6 — no
    /// congestion. <c>03 §3.7</c> says free-flow <em>is</em> the exact answer for a walk because
    /// pedestrian networks do not saturate; for a car it is an underestimate, in the one mode where
    /// the error grows with the city.
    /// </remarks>
    [Fact]
    public void The_same_journey_costs_less_by_car_than_on_foot()
    {
        World world = Run(WithOwnership(100)).World;
        var scratch = new WalkScratch();
        TripRuleset trips = world.Rules.Trips;

        int compared = 0;
        int quicker = 0;

        foreach ((Address from, Address to) in Commutes(world, TravelMode.Car).Take(400))
        {
            TravelTime drive = WalkRouting.Cost(
                world.Roads, TravelMode.Car, from, to, trips.CrossingCost, scratch);
            TravelTime walk = WalkRouting.Cost(
                world.Roads, TravelMode.Foot, from, to, trips.CrossingCost, scratch);

            if (drive.IsImpassable || walk.IsImpassable)
            {
                continue;
            }

            compared++;

            if (drive < walk)
            {
                quicker++;
            }
        }

        Assert.True(compared > 50, $"only {compared} journeys were comparable in both modes.");

        // Not every one: a journey short enough to be closed form on a single Segment is priced at
        // the same free-flow in both modes wherever the road is slower than a walker, and the
        // crossing cost is charged to the pedestrian only -- so a handful come out equal.
        Assert.True(
            quicker * 10 > compared * 9,
            $"only {quicker} of {compared} journeys were quicker by car.");
    }

    /// <summary>
    /// ⚠ <b>A driver is judged for a job on the clock they actually travel on</b> — so the Commute
    /// Budget refuses a walker jobs it never refuses a driver.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>adr/0008</c>, not a refinement. Session F refused a per-mode weight on the Commute Budget
    /// precisely so a walk and a drive are compared on <em>one</em> clock — which only works if the
    /// clock is read in the mode the journey is made in. <b>A driver judged on walking time would
    /// refuse jobs they can reach in ten minutes, and the shortfall would read as a labour-market
    /// finding.</b>
    /// </para>
    /// <para>
    /// 🔴 <b>THIS ASSERTED ON THE EMPLOYMENT TOTAL UNTIL 2026-09-04, AND THAT NUMBER SATURATES.</b>
    /// A walker the ceiling refuses does not go unemployed — they take a nearer job. So the total is
    /// bounded by the vacancies the city holds rather than by how far anybody reaches, and the whole
    /// effect lands in <em>which</em> job and not in <em>how many</em>. Measured at
    /// <see cref="BudgetBinds"/>: employment <b>44,188 walking against 44,211 driving</b>, a margin
    /// of <b>0.05%</b> — while the refusals behind it are <b>4,920 against 0</b> and the rung split
    /// is <b>26,307 / 15,991 / 1,890</b> against <b>44,211 / 0 / 0</b>.
    /// ***A saturating proxy passes as loudly on a margin of one job as on a margin of a thousand.***
    /// </para>
    /// <para>
    /// ⚠ <b>THE SWEEP THIS TEST WAS SITED BY HAD GONE STALE AND NOTHING COULD SAY SO.</b> It
    /// recorded drivers minus walkers as <b>−1, +41, +1,070</b> at 4,000, 16,000 and 64,000 on
    /// 2026-09-01. Re-measured on <c>2620f50</c>, the same three populations give <b>+1, +1,
    /// +19</b> — two orders of magnitude down at the top rung — and the assertion went on passing
    /// on a margin of <b>one job</b>, because <c>&gt;</c> reports a sign and never a size.
    /// <c>plans/0060</c> row 24 is what tipped it, by four jobs, having not caused it.
    /// </para>
    /// <para>
    /// ⚠ <b>A driver's <c>Beyond</c> is 0 at every population measured, and that is a finding rather
    /// than a tautology.</b> Fifty clock minutes at 90 km/h reaches across more city than this
    /// lattice holds at 64,000, so the Budget does not bind on a driver at any size this suite runs.
    /// The day it does, this is the assertion that says so.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_budget_refuses_a_walker_jobs_it_never_refuses_a_driver()
    {
        EmploymentActivity walking = Run(GoldenFixtures.Rules(), BudgetBinds).Employment.Drain();
        EmploymentActivity driving = Run(WithOwnership(100), BudgetBinds).Employment.Drain();

        Assert.True(
            walking.Employed.Sum > 0, "nobody was employed at all, so the comparison is empty.");

        Assert.True(
            walking.Beyond.Sum > 0,
            $"the Commute Budget refused a walker nothing at {BudgetBinds} Citizens, so the "
            + "mechanism under comparison is inert here and the run measures the city instead. "
            + "Re-sweep the population -- and read Beyond rather than the employment total, which "
            + "saturates and hid exactly this twice.");

        Assert.True(
            driving.Beyond.Sum == 0,
            $"the Commute Budget refused a driver {driving.Beyond.Sum} vacancies at {BudgetBinds} "
            + $"Citizens, against a walker's {walking.Beyond.Sum}. A driver being refused at all is "
            + "new: either the city has outgrown the ceiling in both modes, or the clock a driver "
            + "is judged on has stopped being the one they travel on.");

        // The same reach seen from the other side. A driver's commute is Fast or it does not happen;
        // a walker fills all three rungs. ⚠ This is the half that would survive a city large enough
        // to refuse a driver too, so it is asserted beside the refusal rather than instead of it.
        Assert.True(
            driving.Moderate.Sum + driving.Unsavoury.Sum == 0,
            $"{driving.Moderate.Sum + driving.Unsavoury.Sum} drivers commuted beyond the fast rung.");

        Assert.True(
            walking.Moderate.Sum + walking.Unsavoury.Sum > walking.Employed.Sum / 4,
            $"only {walking.Moderate.Sum + walking.Unsavoury.Sum} of {walking.Employed.Sum} walkers "
            + "commute beyond the fast rung, so the two modes are no longer distinguishable by the "
            + "rung they land on.");
    }

    /// <summary>Every employed Citizen's home and workplace Addresses, in the given mode.</summary>
    /// <remarks>
    /// <b>Drawn from the standing city rather than from the Legs it made</b>, for the reason
    /// <see cref="LegModes"/> gives: the Legs are gone. These are the same pairs
    /// <see cref="Core.Movement.CommuteEngine"/> routes every Day, so a measurement over them is a
    /// measurement of the city's actual commute and not of a synthetic draw — which is the whole
    /// distinction S2 R4 measured and called <em>a different city</em>.
    /// </remarks>
    internal static IEnumerable<(Address From, Address To)> Commutes(World world, TravelMode mode)
    {
        CitizenTable citizens = world.Citizens;

        for (int slot = 0; slot < citizens.Rows.SlotCount; slot++)
        {
            // Two hops to the workplace since adr/0141: the Workplace is a Business and a Business
            // borrows its premises' location, so an unpremised employer is skipped here rather than
            // routed to — there is no Address to walk to.
            if (!citizens.Rows.IsLive(slot)
                || !world.Businesses.Rows.TryResolve(citizens.Workplace[slot], out int employer)
                || !world.Buildings.Rows.TryResolve(
                    world.Businesses.Building[employer], out int workplace)
                || !world.Households.Rows.TryResolve(citizens.HouseholdOf[slot], out int household)
                || !world.Buildings.Rows.TryResolve(
                    world.Households.Dwelling[household], out int home))
            {
                continue;
            }

            Address from = world.AccessPoint(home, mode);
            Address to = world.AccessPoint(workplace, mode);

            if (from.Exists && to.Exists)
            {
                yield return (from, to);
            }
        }
    }
}
