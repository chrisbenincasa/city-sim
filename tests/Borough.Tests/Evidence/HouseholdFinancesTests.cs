using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Evidence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Evidence;

/// <summary>
/// Milestone 10 task 8 — <c>02 §9</c>'s household finances, and the clause
/// <c>CitizenEvidence</c> shipped declining.
/// </summary>
/// <remarks>
/// <para>
/// <b>The load-bearing pair is <see cref="A_world_with_no_money_reports_absent"/> and
/// <see cref="A_household_that_holds_nothing_reports_zero"/>.</b> The clause was omitted because those
/// two read the same, so the whole of this task is that they no longer do — and a test that held only
/// one of them would pass over a member that returned the wrong one of the two everywhere.
/// </para>
/// <para>
/// ⚠ <b>The absent case is exercised here and by no shipped Ruleset.</b> Milestone 10 task 2 put a
/// money Resource in all seven, so every Household in every shipped world holds a money Bin. That
/// makes this file the only thing standing between the absent branch and being dead code — which is
/// milestone 6 task 7's finding on <c>TripFate.Stranded</c>, arriving before the branch shipped
/// rather than a milestone later. ***A branch no shipped content reaches is still a branch content
/// can reach, and the test is what says which.***
/// </para>
/// <para>
/// <b>Every test reads the same fact by a second route</b>, which is <c>EvidenceTests</c>' rule: an
/// assembler has no behaviour of its own, so an assertion that a number looks plausible tests nothing.
/// The second route here is <c>World.BalanceOf</c> and the Bin the Household names.
/// </para>
/// </remarks>
public sealed class HouseholdFinancesTests
{
    private const int Citizens = 500;

    /// <summary>
    /// A Ruleset naming no money opens no balance, and the row says so rather than saying zero.
    /// </summary>
    /// <remarks>
    /// <b>This is the half a writer arriving did not pay.</b> Task 4c made a balance a Bin and task 5
    /// gave it a production writer, which answers <em>nothing writes this column</em>; it does not
    /// answer <em>a Household with no money and a Household in a world with no money read the same</em>.
    /// The shape answers that, and this is the assertion.
    /// </remarks>
    [Fact]
    public void A_world_with_no_money_reports_absent()
    {
        World world = City(Bare);

        int seen = 0;

        foreach (CitizenEvidence evidence in Everybody(world))
        {
            Assert.Null(evidence.HouseholdBalance);
            seen++;
        }

        Assert.True(seen > 0, "the fixture built no Citizens, so nothing here was asserted.");
    }

    /// <summary>
    /// A Ruleset naming money but endowing nobody reports a present zero, which is destitution.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>This is what six of the seven shipped Rulesets produce</b>, and the fact it reports is a
    /// real state of the city rather than a gap in the build: money enters a world only through
    /// <c>[households] opening_balance_min/max</c>, `taxed.toml` alone states it, and `adr/0024` makes
    /// the Outside Connection the only other door. <c>adr/0024</c> takes no position on destitution,
    /// so a report that hid it behind an absence would be the simulation declining to say something
    /// it knows.
    /// </remarks>
    [Fact]
    public void A_household_that_holds_nothing_reports_zero()
    {
        World world = City(Bare + Currency);

        int seen = 0;

        foreach (CitizenEvidence evidence in Everybody(world))
        {
            Assert.Equal(Money.Zero, evidence.HouseholdBalance);
            seen++;
        }

        Assert.True(seen > 0, "the fixture built no Citizens, so nothing here was asserted.");
    }

    /// <summary>
    /// An endowed world reports the balance the Household's Bin actually holds.
    /// </summary>
    /// <remarks>
    /// <b>The second route is <c>World.BalanceOf</c></b>, which is the method the simulation itself
    /// spends through. The two must agree on every Citizen — and they are reached differently, one
    /// through the Citizen's Household handle and one through the Household's Bin handle, so an
    /// assembler reading the wrong row would show up here and nowhere else.
    /// </remarks>
    [Fact]
    public void A_balance_agrees_with_the_bin_the_household_names()
    {
        World world = City(Bare + Currency + Endowment);

        int held = 0;

        foreach (CitizenEvidence evidence in Everybody(world))
        {
            Money balance = Assert.IsType<Money>(evidence.HouseholdBalance);

            Assert.Equal(world.BalanceOf(evidence.Household), balance);

            if (balance.Raw > 0)
            {
                held++;
            }
        }

        Assert.True(held > 0, "the populator endowed nobody, so the reading proves nothing.");
    }

    /// <summary>
    /// <c>World.BalanceOf</c> conflates the two facts this member separates, and both are right.
    /// </summary>
    /// <remarks>
    /// <b>The one test here that is about a disagreement rather than an agreement.</b>
    /// <c>BalanceOf</c> returns <c>Money.Zero</c> for a Household with no money Bin and states why —
    /// <em>"a world with no currency and a Household with none behave identically at every call site
    /// money has"</em> — which is true of a call site that <b>spends</b> and false of a reader. If
    /// somebody ever tidies the two into agreement, this fails and points at the sentence that says
    /// they must not. ***Two facts a mechanism may treat as one are still two facts to somebody
    /// reading them.***
    /// </remarks>
    [Fact]
    public void The_spender_and_the_reader_disagree_on_a_world_with_no_money()
    {
        World world = City(Bare);

        foreach (CitizenEvidence evidence in Everybody(world))
        {
            Assert.Equal(Money.Zero, world.BalanceOf(evidence.Household));
            Assert.Null(evidence.HouseholdBalance);

            return;
        }

        Assert.Fail("the fixture built no Citizens.");
    }

    /// <summary>Money moving through a Policy moves the reading, which is what a report is for.</summary>
    /// <remarks>
    /// Task 5's circuit read through task 8's member. Without this the member could be a snapshot of
    /// the endowment that never refreshes, and every assertion above would still pass.
    /// </remarks>
    [Fact]
    public void A_levy_moves_what_the_row_reports()
    {
        RulesetLoadResult result =
            RulesetLoader.Parse(Bare + Currency + Endowment + Levy, "test.toml");

        Assert.True(result.Ok, result.Describe());

        var key = WorldKey.FromSeed(20_260_819);
        var world = new World(Citizens, result.Ruleset!, key);
        var simulation = new Simulation(world, key);

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        long before = Sum(world);

        for (int tick = 0; tick < Ticks.PerDay + 1; tick++)
        {
            simulation.Step(default);
        }

        long after = Sum(world);

        Assert.True(before > 0, "the populator endowed nobody.");
        Assert.True(after < before, $"the levy took nothing: {before} -> {after}.");
    }

    /// <summary>Every live Citizen's row, assembled.</summary>
    private static IEnumerable<CitizenEvidence> Everybody(World world)
    {
        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (world.Citizens.Rows.IsLive(slot))
            {
                yield return Core.Evidence.Evidence.OfCitizen(world, world.Citizens.Rows.At(slot));
            }
        }
    }

    /// <summary>What every Citizen's row reports, summed — households counted once per occupant.</summary>
    private static long Sum(World world)
    {
        long total = 0;

        foreach (CitizenEvidence evidence in Everybody(world))
        {
            total += evidence.HouseholdBalance?.Raw ?? 0;
        }

        return total;
    }

    private static World City(string source)
    {
        RulesetLoadResult result = RulesetLoader.Parse(source, "test.toml");

        Assert.True(result.Ok, result.Describe());

        var key = WorldKey.FromSeed(20_260_819);
        var world = new World(Citizens, result.Ruleset!, key);

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        return world;
    }

    /// <summary>A world with Buildings, Households and Citizens, and no currency at all.</summary>
    private const string Bare = """
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
        """;

    /// <summary>The currency, which opens a balance on every Household and puts nothing in it.</summary>
    private const string Currency = """

        [[resource]]
        name = "money"
        family = "money"
        """;

    /// <summary>The only door money enters a world by until milestone 11.</summary>
    private const string Endowment = """

        [households]
        car_ownership_percent = 0
        opening_balance_min = 0
        opening_balance_max = 1000
        """;

    private static readonly string Levy = $$"""

        [[policy]]
        name = "levy"
        sweeps = "household"
        interval = {{Ticks.PerDay}}
        apply = { derived = "balance", percent = 10 }
        transfer = { from = "local", to = "global", resource = "money", amount = 1 }
        """;
}
