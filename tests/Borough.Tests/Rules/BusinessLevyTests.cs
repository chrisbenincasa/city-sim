using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Instruments;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// Milestone 27 task 9: a Policy sweeps Businesses (<c>adr/0149</c>).
/// </summary>
/// <remarks>
/// <para>
/// <b>The circuit under test is <c>rulesets/levied.toml</c>'s</b>, and it is <c>founded.toml</c> plus
/// one <c>[[policy]]</c> — so <c>founded.toml</c> is the control in every assertion here, run at the
/// same seed for the same number of Ticks. ***What the pair isolates is the levy and nothing else.***
/// </para>
/// <para>
/// ⚠ <b>The sweep passes over a large part of what it considers, and that is the second thing
/// asserted.</b> <c>adr/0148</c> gives every dwelling an instantiated shop whose balance opens at
/// zero, and only a <em>founded</em> shop holds money — 302 live Businesses at this fixture's scale,
/// of which 125 hold nothing. A reader taking that gap for a defect has read <c>considered</c> as
/// <c>eligible</c>. ⚠ <b>The SHAPE is asserted and never the ratio</b>: founded shops accumulate
/// while instantiated ones come down with their premises, so the split is a property of how far the
/// run got rather than of the file.
/// </para>
/// </remarks>
public sealed class BusinessLevyTests(Xunit.Abstractions.ITestOutputHelper output)
{
    private readonly Xunit.Abstractions.ITestOutputHelper _out = output;

    private const int Citizens = 2_000;
    private const int TickCount = 6_144;
    private const ulong Seed = 0x1E71EDU;

    /// <summary>
    /// The levy takes money out of Businesses, and the control file with no levy keeps it.
    /// </summary>
    [Fact]
    public void A_levy_over_businesses_takes_money_a_business_holds()
    {
        (World levied, Money levyTreasury) = Run("levied.toml");
        (World control, Money controlTreasury) = Run("founded.toml");

        long levyBusinesses = TotalBusinessMoney(levied).Raw;
        long controlBusinesses = TotalBusinessMoney(control).Raw;

        _out.WriteLine(
            $"levied.toml:  businesses={levyBusinesses} treasury={levyTreasury.Raw} "
            + $"live={levied.Businesses.Rows.LiveCount} holding={BusinessesHoldingSomething(levied)}");
        _out.WriteLine(
            $"founded.toml: businesses={controlBusinesses} treasury={controlTreasury.Raw} "
            + $"live={control.Businesses.Rows.LiveCount} holding={BusinessesHoldingSomething(control)}");

        // Vacuity first: a run in which nothing was ever founded satisfies everything below by
        // holding zero on both sides, and every shop in these worlds that holds money was founded.
        Assert.True(
            controlBusinesses > 0,
            "no Business in the control world holds any money, so the levy has nothing to take and "
            + "the comparison below is between two zeroes. Either [founding]'s means test refused "
            + "every Household or the founding pass did not run.");

        Assert.True(
            levyBusinesses < controlBusinesses,
            $"Businesses hold {levyBusinesses} under a Ruleset that levies them and "
            + $"{controlBusinesses} under the same file without the levy. The levy took nothing -- "
            + "adr/0149's whole claim is that a Policy can read a Business's balance.");

        // And the other end of the transfer, so this is not a leak wearing a levy's name. The
        // treasuries are NOT compared to each other: founded.toml has two Household Policies of its
        // own, so both treasuries are non-zero and the difference is the levy's collection plus
        // whatever the rebate happened to pay back on the last trigger.
        Assert.True(
            levyTreasury.Raw > controlTreasury.Raw,
            $"the treasury holds {levyTreasury.Raw} with the trade levy and {controlTreasury.Raw} "
            + "without it. Money left the Businesses and did not arrive.");
    }

    /// <summary>
    /// ⚠ <b>The sweep considers every live Business and applies to the few that hold anything.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>PolicyEngine</c>'s own line about a Household with nothing, arriving on the other
    /// population: a shop holding zero owes zero applications and the sweep passes over it. <b>The
    /// existence of the gap is the assertion and its width is not</b> — a world where the two counts
    /// were equal would be one where every shop had somehow been capitalised, and a world where they
    /// diverge by any margin is the mechanism working.
    /// </para>
    /// <para>
    /// ⚠ <b>The shipped file cannot make this assertion and the fixture is why.</b> The three
    /// <c>PolicyCounter</c>s are one set for the whole engine, so a run of <c>levied.toml</c> reports
    /// <em>every</em> Policy's members together — and its two Household Policies drown the trade levy
    /// at a ratio of about ten to one. ***A counter that cannot be attributed to a Policy cannot
    /// assert a property of one***, so this loads <c>founded.toml</c> <b>truncated at its first
    /// <c>[[policy]]</c></b> and appends the levy alone. Editing the text and parsing it is
    /// <c>ReachFailureTests</c>' trick, and the thing under test is the mechanism rather than the
    /// file.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_business_holding_nothing_is_considered_and_owes_nothing()
    {
        (World world, Simulation simulation) = CityFrom(OnlyTheTradeLevy());
        var census = new Census(world);

        for (int tick = 0; tick < TickCount; tick++)
        {
            simulation.Step(TickInput.Empty);
        }

        census.Observe(simulation);

        long considered = Read(census, PolicyCounter.Considered);
        long applied = Read(census, PolicyCounter.Applied);
        int holding = BusinessesHoldingSomething(world);

        _out.WriteLine(
            $"considered={considered} applied={applied} live={world.Businesses.Rows.LiveCount} "
            + $"holding={holding}");

        Assert.True(considered > 0, "no Business was swept at all.");

        Assert.True(
            applied > 0,
            "the levy applied to nobody, so it is inert -- and every assertion about what it passes "
            + "over would hold for a Policy that did nothing.");

        // ⚠ `considered` is the RUN's total across every trigger and not one sweep's, so it is
        // compared against the live count as a lower bound rather than an equality. What it refuses
        // is a sweep reaching a subset -- a Policy is an entitlement (02 section 4.2) and a sampler
        // would show up here as a count below the population.
        Assert.True(
            considered >= world.Businesses.Rows.LiveCount,
            $"{considered} members swept in total against {world.Businesses.Rows.LiveCount} live "
            + "Businesses, and the sweep triggered more than once. A Policy sweeps its whole "
            + "population; a count below it means something is sampling.");

        // The claim: a Business holding nothing is reached and owes nothing.
        Assert.True(
            world.Businesses.Rows.LiveCount > holding,
            $"every one of the {holding} live Businesses holds money, so this world has no shop the "
            + "levy could pass over and the assertion below is vacuous. adr/0148's instantiated "
            + "shops are the ones that should hold nothing.");

        Assert.True(
            applied < considered,
            $"{applied} applications against {considered} members swept, and this world holds "
            + "Businesses with a zero balance. A levy derived from `balance` owes nothing on zero, "
            + "so it must pass over at least those.");
    }

    // ---- the fixture ---------------------------------------------------------------------------

    private static Ruleset Load(string file)
    {
        RulesetLoadResult loaded = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

        Assert.True(loaded.Ok, loaded.Describe());

        return loaded.Ruleset!;
    }

    private static (World World, Simulation Simulation) City(string file) => Build(Load(file));

    private static (World World, Simulation Simulation) CityFrom(string toml)
    {
        RulesetLoadResult loaded = RulesetLoader.Parse(toml, "levied-alone.toml");

        Assert.True(loaded.Ok, loaded.Describe());

        return Build(loaded.Ruleset!);
    }

    private static (World World, Simulation Simulation) Build(Ruleset rules)
    {
        var key = WorldKey.FromSeed(Seed);
        var world = new World(Citizens, rules, key);

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        return (world, new Simulation(world, key));
    }

    /// <summary>
    /// <c>founded.toml</c> cut at its first <c>[[policy]]</c>, with the trade levy appended.
    /// </summary>
    private static string OnlyTheTradeLevy()
    {
        string text = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "founded.toml"));

        // ⚠ AT THE START OF A LINE, and the first attempt was not: founded.toml's header says "two
        // [[policy]] tables" in prose four hundred lines above the tables themselves, so a bare
        // IndexOf cut the file at its own description of itself and produced a world with no
        // Buildings in it. A fixture built by text surgery has to cut on syntax.
        int at = text.IndexOf("\n[[policy]]", StringComparison.Ordinal);

        Assert.True(
            at > 0,
            "founded.toml states no [[policy]] table, so this fixture is cutting nothing and the "
            + "counters it reads belong to a Policy set that has changed shape.");

        return text[..(at + 1)] + """
            [[policy]]
            name = "trade_levy"
            sweeps = "business"
            interval = 2048
            apply = { derived = "balance", percent = 1 }
            transfer = { from = "local", to = "global", resource = "money", amount = 1 }
            """;
    }

    private static int BusinessesHoldingSomething(World world)
    {
        int holding = 0;

        for (int slot = 0; slot < world.Businesses.Rows.SlotCount; slot++)
        {
            if (world.Businesses.Rows.IsLive(slot)
                && world.BalanceOf(world.Businesses.Rows.At(slot)).Raw > 0)
            {
                holding++;
            }
        }

        return holding;
    }

    private static (World World, Money Treasury) Run(string file)
    {
        (World world, Simulation simulation) = City(file);

        for (int tick = 0; tick < TickCount; tick++)
        {
            simulation.Step(TickInput.Empty);
        }

        return (world, Treasury(world));
    }

    private static Money TotalBusinessMoney(World world)
    {
        long total = 0;

        for (int slot = 0; slot < world.Businesses.Rows.SlotCount; slot++)
        {
            if (world.Businesses.Rows.IsLive(slot))
            {
                total += world.BalanceOf(world.Businesses.Rows.At(slot)).Raw;
            }
        }

        return new Money(total);
    }

    private static Money Treasury(World world)
    {
        int[] bins = [.. world.TreasuryBins.Walk(TreasuryTable.Slot)];

        return new Money(world.Bins.LevelAt(bins[0]));
    }

    private static long Read(Census census, PolicyCounter counter)
    {
        ReadOnlySpan<CensusSample> samples =
            census.Series(Metric.Of(counter, Aggregate.Sum), new Ticks(1)).Samples.Span;

        return samples.IsEmpty ? 0 : samples[^1].Value;
    }
}
