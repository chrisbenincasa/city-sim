using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Instruments;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;
using Xunit.Abstractions;

namespace Borough.Tests.Rules;

/// <summary>
/// Milestone 10 task 5 — the Sweep family's second member, and the first production writer the
/// Household balance sheet has ever had.
/// </summary>
/// <remarks>
/// <para>
/// <b>The circuit under test is <c>rulesets/taxed.toml</c>'s</b>: a levy sweeps 10% of every
/// Household's balance into the treasury, and a rebate pays a flat 100 back out. Both are
/// <c>local</c>↔<c>global</c> transfers, both are Sweep Rules, and between them they exercise every
/// branch the milestone owes — the transfer in both directions, the exhaustion branch
/// <c>02 §4.2</c> names, the rotation that makes exhaustion fair, and <c>adr/0115</c>'s
/// floor-to-zero instrument.
/// </para>
/// <para>
/// <b>Money is conserved across all of it and that is asserted as an exact equality</b>, which is
/// what <c>plans/0033</c> F5 says this milestone is the only one able to do: the Outside Connection
/// is money's only sink and it arrives in 11, so until then every movement is internal and the sum
/// cannot legitimately drift by one.
/// </para>
/// </remarks>
public sealed class PolicyTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    private const int Citizens = 4_000;

    /// <summary>The levy alone, so what the treasury holds is what the levy took.</summary>
    [Fact]
    public void A_levy_moves_money_from_every_household_to_the_treasury()
    {
        (World world, Simulation simulation) = City(Levy(percent: 10));

        Money before = TotalHouseholdMoney(world);

        Assert.Equal(Money.Zero, Treasury(world));
        Assert.True(before.Raw > 0, "the populator endowed nobody.");

        Step(simulation, Ticks.PerDay);

        Money after = TotalHouseholdMoney(world);

        _output.WriteLine($"households {before.Raw} -> {after.Raw}, treasury {Treasury(world).Raw}");

        Assert.True(after.Raw < before.Raw, "the levy took nothing.");
        Assert.Equal(before, after + Treasury(world));
    }

    /// <summary>
    /// A transfer runs in both directions inside one Tick, and the sum does not move.
    /// </summary>
    /// <remarks>
    /// <b>The levy is declared first and funds the rebate on the same trigger</b>, which is the
    /// declaration order being load-bearing: a Policy acts where it runs, so there is no proposal and
    /// no settle between them.
    /// </remarks>
    [Fact]
    public void Money_is_conserved_across_a_levy_and_a_rebate()
    {
        (World world, Simulation simulation) = City(Levy(percent: 10) + Rebate(amount: 100));

        Money supply = TotalHouseholdMoney(world);

        for (int day = 0; day < 8; day++)
        {
            Step(simulation, Ticks.PerDay);

            Assert.Equal(supply, TotalHouseholdMoney(world) + Treasury(world));
        }

        _output.WriteLine(
            $"after 8 days: households {TotalHouseholdMoney(world).Raw}, "
            + $"treasury {Treasury(world).Raw}");
    }

    /// <summary>
    /// ⚠ <b><c>02 §4.2</c>'s exhaustion branch: a rebate the levy cannot cover pays whom it reaches
    /// and stops.</b>
    /// </summary>
    /// <remarks>
    /// <b>Reachable on the first sweep rather than after a constructed run</b>, which is one of the
    /// two things <c>adr/0116</c>'s empty opening treasury bought. The other half is the file: the
    /// rebate owes 100 a Household against a levy that takes about 50, so the treasury runs dry about
    /// halfway through.
    /// </remarks>
    [Fact]
    public void A_rebate_the_treasury_cannot_cover_pays_whom_it_reaches_and_stops()
    {
        (World world, Simulation simulation) = City(Levy(percent: 10) + Rebate(amount: 100));
        var census = new Census(world);

        Step(simulation, Ticks.PerDay);
        census.Observe(simulation);

        long swept = Read(census, PolicyCounter.Considered);
        long applied = Read(census, PolicyCounter.Applied);
        long dry = Read(census, PolicyCounter.Exhausted);

        _output.WriteLine($"considered {swept}, applied {applied}, sweeps run dry {dry}");

        Assert.Equal(1, dry);
        Assert.True(applied > 0, "the rebate paid nobody.");
        Assert.True(applied < swept, "the rebate paid everybody, so nothing ran dry.");
    }

    /// <summary>
    /// ⚠ <b>The rotation is what makes exhaustion a gradient rather than a permanent boundary.</b>
    /// </summary>
    /// <remarks>
    /// <b>Asserted on the <em>set</em> reached rather than on the counter</b>, because a count is the
    /// same on every sweep whichever Households it fell on — which is exactly the failure a fixed scan
    /// order would produce and exactly what no aggregate can see. Two consecutive triggers reach two
    /// different sets, so the tail of the population is not permanently excluded.
    /// </remarks>
    [Fact]
    public void Two_triggers_of_an_exhausting_sweep_reach_different_households()
    {
        (World world, Simulation simulation) = City(Rebate(amount: 100));

        // The rebate alone, out of a treasury seeded before EACH trigger: nothing refills it in the
        // file, and a single seeding would leave the second sweep meeting an empty treasury, which
        // pays nobody and says nothing about the scan order. Seeded twice, both sweeps are SHORT --
        // each covers part of the population -- which is the condition the rotation is about.
        Handle<Bin> treasury =
            world.Bins.Rows.At(Assert.Single<int>([.. world.TreasuryBins.Walk(TreasuryTable.Slot)]));

        long[] first = Balances(
            world,
            () =>
            {
                world.Deposit(treasury, 20_000, world.Tick);
                Step(simulation, Ticks.PerDay);
            });

        long[] second = Balances(
            world,
            () =>
            {
                world.Deposit(treasury, 20_000, world.Tick);
                Step(simulation, Ticks.PerDay);
            });

        int paidFirst = first.Count(delta => delta > 0);
        int paidSecond = second.Count(delta => delta > 0);
        int both = first.Where((delta, i) => delta > 0 && second[i] > 0).Count();

        _output.WriteLine($"paid {paidFirst} then {paidSecond}, overlap {both}");

        Assert.True(paidFirst > 0 && paidSecond > 0, "one of the sweeps paid nobody.");
        Assert.True(
            both < paidFirst,
            "the second sweep paid exactly the households the first did, so the start did not rotate.");
    }

    /// <summary>
    /// ⚠ <b><c>adr/0115</c>'s instrument fires, and it counts a non-zero balance rounding away rather
    /// than a Household with nothing.</b>
    /// </summary>
    /// <remarks>
    /// <b>At 10% the threshold is ten smallest units, which is that ADR's own floor</b> — so this is
    /// the discipline being measured rather than a number chosen beside it. A Household holding 1..9
    /// pays nothing while everybody else pays the stated rate, which is a regressive outcome produced
    /// by rounding and chosen by nobody.
    /// </remarks>
    [Fact]
    public void A_percentage_that_floors_to_zero_on_a_real_balance_is_counted()
    {
        (World world, Simulation simulation) = City(Levy(percent: 10));
        var census = new Census(world);

        int poor = 0;
        int destitute = 0;

        // Read BEFORE the sweep, because the levy floors on the balance it was handed. Reading after
        // counts a Household that held 10 and paid 1 as one holding 9, which the levy did not floor
        // on -- the after-picture cannot distinguish a balance that rounded away from one the tax
        // produced.
        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (!world.Households.Rows.IsLive(slot))
            {
                continue;
            }

            long balance = world.BalanceOf(world.Households.Rows.At(slot)).Raw;

            if (balance == 0)
            {
                destitute++;
            }
            else if (balance < 10)
            {
                poor++;
            }
        }

        Step(simulation, Ticks.PerDay);
        census.Observe(simulation);

        long floored = Read(census, PolicyCounter.Floored);

        _output.WriteLine($"floored {floored}, holding 1..9 {poor}, holding nothing {destitute}");

        Assert.True(floored > 0, "no percentage floored to zero, so the instrument is unexercised.");
        Assert.Equal(poor, floored);
    }

    /// <summary>A Ruleset with no Policy sweeps nothing and costs nothing.</summary>
    /// <remarks>
    /// The negative half, and it is what makes every assertion above mean something: the counters
    /// above are non-zero because a Policy ran, not because the family reads non-zero by default.
    /// </remarks>
    [Fact]
    public void A_ruleset_with_no_policy_reports_no_sweeps()
    {
        (World world, Simulation simulation) = City(string.Empty);
        var census = new Census(world);

        Money before = TotalHouseholdMoney(world);

        Step(simulation, Ticks.PerDay);
        census.Observe(simulation);

        Assert.Equal(0, Read(census, PolicyCounter.Triggers));
        Assert.Equal(0, Read(census, PolicyCounter.Considered));
        Assert.Equal(before, TotalHouseholdMoney(world));
    }

    // ---- the fixture ---------------------------------------------------------------------------

    private const string Endowed = """
        [[resource]]
        name = "money"
        family = "money"

        [[resource]]
        name = "sundries"
        family = "good"

        [[building]]
        name = "dwelling"
        occupants = 3
        bins = [ { resource = "sundries", capacity = 48 } ]

        [roads]
        block_tiles = 32
        arterial_count = 0
        arterial_junction_tiles = 512
        foot_crossing_every = 4
        foot_paths_per_thousand_blocks = 40
        street_speed_kph = 50
        arterial_speed_kph = 90
        walk_speed_kph = 5
        street_capacity_per_hour = 3600
        arterial_capacity_per_hour = 12000
        foot_path_capacity_per_hour = 1000

        [lots]
        lots_per_segment = 5

        [households]
        car_ownership_percent = 0
        opening_balance_min = 0
        opening_balance_max = 1000
        """;

    private static string Levy(int percent) => $$"""

        [[policy]]
        name = "levy"
        sweeps = "household"
        interval = {{Ticks.PerDay}}
        apply = { derived = "balance", percent = {{percent}} }
        transfer = { from = "local", to = "global", resource = "money", amount = 1 }
        """;

    private static string Rebate(int amount) => $$"""

        [[policy]]
        name = "rebate"
        sweeps = "household"
        interval = {{Ticks.PerDay}}
        apply = { min = {{amount}}, max = {{amount}} }
        transfer = { from = "global", to = "local", resource = "money", amount = 1 }
        """;

    private static (World World, Simulation Simulation) City(string policies)
    {
        RulesetLoadResult result = RulesetLoader.Parse(Endowed + policies, "test.toml");

        Assert.True(result.Ok, result.Describe());

        var key = WorldKey.FromSeed(20_260_819);
        var world = new World(Citizens, result.Ruleset!, key);
        var simulation = new Simulation(world, key);

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        return (world, simulation);
    }

    private static void Step(Simulation simulation, int ticks)
    {
        for (int tick = 0; tick < ticks; tick++)
        {
            simulation.Step(default);
        }
    }

    private static Money TotalHouseholdMoney(World world)
    {
        long total = 0;

        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (world.Households.Rows.IsLive(slot))
            {
                total += world.BalanceOf(world.Households.Rows.At(slot)).Raw;
            }
        }

        return new Money(total);
    }

    private static Money Treasury(World world)
    {
        int[] bins = [.. world.TreasuryBins.Walk(TreasuryTable.Slot)];

        return new Money(world.Bins.LevelAt(bins[0]));
    }

    /// <summary>Every live Household's change in balance across <paramref name="run"/>.</summary>
    private static long[] Balances(World world, Action run)
    {
        int slots = world.Households.Rows.SlotCount;
        long[] before = new long[slots];

        for (int slot = 0; slot < slots; slot++)
        {
            before[slot] = world.Households.Rows.IsLive(slot)
                ? world.BalanceOf(world.Households.Rows.At(slot)).Raw
                : 0;
        }

        run();

        long[] delta = new long[slots];

        for (int slot = 0; slot < slots; slot++)
        {
            delta[slot] = world.Households.Rows.IsLive(slot)
                ? world.BalanceOf(world.Households.Rows.At(slot)).Raw - before[slot]
                : 0;
        }

        return delta;
    }

    private static long Read(Census census, PolicyCounter counter)
    {
        ReadOnlySpan<CensusSample> samples =
            census.Series(Metric.Of(counter, Aggregate.Sum), new Ticks(1)).Samples.Span;

        return samples.IsEmpty ? 0 : samples[^1].Value;
    }
}
