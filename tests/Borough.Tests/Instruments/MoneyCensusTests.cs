using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Instruments;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Instruments;

/// <summary>
/// Milestone 10 task 7 — the two money families, and the first magnitudes the Census has carried.
/// </summary>
/// <remarks>
/// <para>
/// <b>The load-bearing pair is <see cref="The_supply_and_the_treasury_move_independently"/> and
/// <see cref="Both_directions_are_recorded_and_a_net_would_have_hidden_the_circuit"/>.</b> Everything
/// else is a shape or a refusal. The first is <c>01 §5.1</c>'s requirement held as an assertion — the
/// money supply and the treasury are different bills, so a report carrying one is not carrying the
/// other — and the second is the reason a flow is two counters rather than one.
/// </para>
/// <para>
/// <b>The circuit is <c>rulesets/taxed.toml</c>'s, authored inline for <c>PolicyTests</c>' reason</b>:
/// a test that reads the shipped file measures the file, and what is under test here is the
/// instrument. Both halves — a levy into the treasury and a rebate out of it — are needed, because a
/// single-direction circuit cannot show that the two flow counters are not the same counter.
/// </para>
/// </remarks>
public sealed class MoneyCensusTests
{
    private const int Citizens = 2_000;

    /// <summary>
    /// <c>01 §5.1</c>'s separation, as an assertion: one aggregate moves and the other does not.
    /// </summary>
    /// <remarks>
    /// <b>Both halves are asserted and neither alone would do.</b> That the treasury moved would pass
    /// over a report whose supply row was a second copy of it; that the supply is flat would pass over
    /// a report in which nothing happened at all. The pair says the two rows are different numbers
    /// about a city where money moved — which is the claim <em>"a different bill — the money supply,
    /// not the treasury"</em> makes.
    /// </remarks>
    [Fact]
    public void The_supply_and_the_treasury_move_independently()
    {
        (Census census, _) = Run(Levy(percent: 10) + Rebate(amount: 100), Ticks.PerDay * 3);

        long[] supply = Levels(census, MoneyCounter.Supply);
        long[] treasury = Levels(census, MoneyCounter.Treasury);

        Assert.True(supply[0] > 0, "the populator endowed nobody, so nothing here means anything.");
        Assert.All(supply, reading => Assert.Equal(supply[0], reading));

        Assert.Equal(0, treasury[0]);
        Assert.Contains(treasury, reading => reading != 0);
    }

    /// <summary>
    /// The conservation identity, read off the instrument rather than off the invariant.
    /// </summary>
    /// <remarks>
    /// <b><c>held</c> and <c>supply</c> are arrived at differently and that is the whole point.</b>
    /// One is a walk over every conserved Bin and the other is what <c>World.Endow</c> issued, so
    /// their agreeing on every reading of a run is <c>adr/0031</c> holding continuously rather than at
    /// the one instant the end-of-run invariant looks.
    /// </remarks>
    [Fact]
    public void Held_equals_the_supply_on_every_reading()
    {
        (Census census, _) = Run(Levy(percent: 10) + Rebate(amount: 100), Ticks.PerDay * 3);

        long[] supply = Levels(census, MoneyCounter.Supply);
        long[] held = Levels(census, MoneyCounter.Held);

        Assert.Equal(supply.Length, held.Length);
        Assert.Equal(supply, held);
    }

    /// <summary>
    /// The three named holders sum to <c>held</c>, and the residue stays empty.
    /// </summary>
    /// <remarks>
    /// <b><c>elsewhere</c> reading zero is the assertion, not the decomposition summing.</b> The
    /// residue is defined as the difference, so it sums by construction; what a test can hold is that
    /// it is <em>empty</em> — <c>adr/0113</c>'s a Building never holds money, and no fifth owner kind
    /// exists. This is the test that fails on the day one of those stops being true, which is the one
    /// event a report of named holders cannot otherwise show.
    /// </remarks>
    [Fact]
    public void The_named_holders_account_for_all_of_it()
    {
        (Census census, _) = Run(Levy(percent: 10) + Rebate(amount: 100), Ticks.PerDay * 2);

        long[] held = Levels(census, MoneyCounter.Held);
        long[] treasury = Levels(census, MoneyCounter.Treasury);
        long[] households = Levels(census, MoneyCounter.Households);
        long[] businesses = Levels(census, MoneyCounter.Businesses);
        long[] elsewhere = Levels(census, MoneyCounter.Elsewhere);

        Assert.All(elsewhere, reading => Assert.Equal(0, reading));

        for (int i = 0; i < held.Length; i++)
        {
            Assert.Equal(held[i], treasury[i] + households[i] + businesses[i]);
        }
    }

    /// <summary>
    /// A levy and a rebate that nearly cancel are two large flows, not one small one.
    /// </summary>
    /// <remarks>
    /// <b>This is the reason the flow is two counters.</b> The circuit under test moves roughly
    /// 35,000 each way per sweep and leaves the treasury holding tens — so a netted counter would read
    /// as a city that taxes almost nothing, and the gross figures say it taxes heavily and pays it
    /// nearly all back. Those are different cities and the assertion is that the instrument can tell
    /// them apart.
    /// </remarks>
    [Fact]
    public void Both_directions_are_recorded_and_a_net_would_have_hidden_the_circuit()
    {
        (Census census, _) = Run(Levy(percent: 10) + Rebate(amount: 100), Ticks.PerDay * 2);

        long collected = Total(census, MoneyFlowCounter.ToTreasury);
        long paid = Total(census, MoneyFlowCounter.FromTreasury);
        long treasury = Levels(census, MoneyCounter.Treasury)[^1];

        Assert.True(collected > 0, "the levy moved nothing.");
        Assert.True(paid > 0, "the rebate moved nothing.");

        // The identity that makes the two columns trustworthy: the treasury holds exactly what came
        // in less what went out, and it opened empty (adr/0116).
        Assert.Equal(treasury, collected - paid);

        // And the gross is far larger than the net, which is the thing a netted counter would lose.
        Assert.True(
            collected > treasury * 10,
            $"the circuit moved {collected} to leave {treasury}; a net would have said as much.");
    }

    /// <summary>A one-directional circuit leaves the other counter at zero.</summary>
    /// <remarks>
    /// The counters are keyed on the transfer's <c>Scope</c> rather than on the Bins it resolved to,
    /// so a Ruleset with only a levy must show a flow one way and nothing the other. Without this a
    /// pair of counters both fed by every transfer would pass every assertion above.
    /// </remarks>
    [Fact]
    public void A_levy_with_no_rebate_moves_money_one_way_only()
    {
        (Census census, _) = Run(Levy(percent: 10), Ticks.PerDay * 2);

        Assert.True(Total(census, MoneyFlowCounter.ToTreasury) > 0);
        Assert.Equal(0, Total(census, MoneyFlowCounter.FromTreasury));
    }

    /// <summary>
    /// The peak is a Tick's worth and the sum is the interval's, which is what separates them.
    /// </summary>
    /// <remarks>
    /// <b>A sweep fires on one Tick of its interval</b>, so over a whole Day of readings the sum and
    /// the peak are equal — and that equality is the assertion, because it says the peak is being
    /// folded per Tick rather than per reading. ⚠ It also pins the width: the peak rides a
    /// <c>MoneyFlow</c>, whose <c>Peak</c> is a <c>long</c>, and an <c>int</c> one would have to
    /// overflow before this could be shown to matter — so what is held here is the shape and the
    /// <em>argument</em> for the width lives on the type.
    /// </remarks>
    [Fact]
    public void A_sweeps_whole_amount_lands_on_one_tick()
    {
        (Census census, _) = Run(Levy(percent: 10), Ticks.PerDay);

        long sum = Total(census, MoneyFlowCounter.ToTreasury);
        long peak = Peak(census, MoneyFlowCounter.ToTreasury);

        Assert.True(sum > 0, "the levy moved nothing.");
        Assert.Equal(sum, peak);
    }

    /// <summary>A Ruleset that endows nobody reports zero everywhere, and holds vacuously.</summary>
    /// <remarks>
    /// <b>Vacuous rather than wrong, and the distinction is why <c>--money</c> refuses such a file.</b>
    /// Every figure is zero and <c>held == supply</c> is true, so the instrument is right and a reader
    /// shown it under a heading saying money is conserved would learn nothing at all. ***A conservation
    /// identity that holds vacuously reads exactly like one that holds.***
    /// </remarks>
    [Fact]
    public void A_ruleset_that_endows_nobody_reports_zero_everywhere()
    {
        (Census census, _) = Run(string.Empty, Ticks.PerDay, endowed: false);

        foreach (MoneyCounter counter in Enum.GetValues<MoneyCounter>())
        {
            Assert.All(Levels(census, counter), reading => Assert.Equal(0, reading));
        }
    }

    /// <summary>A money level is not aggregated, and asking for a reduction of one says so.</summary>
    /// <remarks>
    /// <c>MetricSource.Table</c>'s property, and it is here because the money family is the first
    /// level family since it: a stock read at an instant is the same number at any cadence, so there
    /// is nothing for a <c>Sum</c> or a <c>Peak</c> to mean.
    /// </remarks>
    [Fact]
    public void A_money_level_has_no_aggregate()
    {
        Metric level = Metric.Of(MoneyCounter.Treasury);

        Assert.Throws<InvalidOperationException>(() => level.Aggregate);
        Assert.Throws<InvalidOperationException>(() => level.Counter);
        Assert.Equal(MoneyCounter.Treasury, level.MoneyCounter);
    }

    /// <summary>A money flow is a flow, and does not answer a level's question.</summary>
    [Fact]
    public void A_money_flow_is_aggregated_and_names_no_level()
    {
        Metric flow = Metric.Of(MoneyFlowCounter.ToTreasury, Aggregate.Peak);

        Assert.Equal(Aggregate.Peak, flow.Aggregate);
        Assert.Equal(MoneyFlowCounter.ToTreasury, flow.MoneyFlowCounter);
        Assert.Throws<InvalidOperationException>(() => flow.MoneyCounter);
    }

    /// <summary>Every reading of one money level, oldest first.</summary>
    private static long[] Levels(Census census, MoneyCounter counter) =>
        [.. Read(census, Metric.Of(counter))];

    /// <summary>
    /// Everything one money flow moved over the whole run.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Summed across readings rather than read off the last one.</b> A flow reading is what
    /// happened since the previous reading and the reading drains it, so the last one covers the last
    /// interval alone — where a level's last reading covers the whole run. Comparing the two
    /// <em>looks</em> right and is a category error, which is how this helper came to exist.
    /// </remarks>
    private static long Total(Census census, MoneyFlowCounter counter)
    {
        long total = 0;

        foreach (long reading in Read(census, Metric.Of(counter, Aggregate.Sum)))
        {
            total += reading;
        }

        return total;
    }

    /// <summary>The largest single Tick of one money flow over the whole run.</summary>
    private static long Peak(Census census, MoneyFlowCounter counter)
    {
        long peak = 0;

        foreach (long reading in Read(census, Metric.Of(counter, Aggregate.Peak)))
        {
            peak = reading > peak ? reading : peak;
        }

        return peak;
    }

    private static IEnumerable<long> Read(Census census, Metric metric)
    {
        // One reading per observation over the whole run: the window is every Tick the census could
        // hold, so nothing is a tail of itself.
        ReadOnlyMemory<CensusSample> samples =
            census.Series(metric, new Ticks(ulong.MaxValue / 2)).Samples;

        for (int i = 0; i < samples.Length; i++)
        {
            yield return samples.Span[i].Value;
        }
    }

    /// <summary>
    /// Builds a city on the given circuit, steps it, and observes once per sweep interval.
    /// </summary>
    private static (Census Census, World World) Run(string policies, int ticks, bool endowed = true)
    {
        string source = (endowed ? Endowed : Bare) + policies;
        RulesetLoadResult result = RulesetLoader.Parse(source, "test.toml");

        Assert.True(result.Ok, result.Describe());

        var key = WorldKey.FromSeed(20_260_819);
        var world = new World(Citizens, result.Ruleset!, key);
        var simulation = new Simulation(world, key);

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        Census census = new(world, (ticks / Ticks.PerDay) + 2);

        census.Observe(simulation);

        for (int tick = 0; tick < ticks; tick++)
        {
            simulation.Step(default);

            if (simulation.Tick.Raw % Ticks.PerDay == 0)
            {
                census.Observe(simulation);
            }
        }

        return (census, world);
    }

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

    /// <summary>
    /// <c>PolicyTests</c>' fixture, and it is the same file for the same reason: what is under test
    /// is the mechanism, so the Ruleset is authored here rather than read off a shipped one.
    /// </summary>
    private const string Endowed = Bare + """

        [households]
        car_ownership_percent = 0
        opening_balance_min = 0
        opening_balance_max = 1000
        """;

    /// <summary>The same file without the endowment, so a money Resource exists and no money does.</summary>
    private const string Bare = """
        [[resource]]
        name = "money"
        family = "money"

        [[resource]]
        name = "sundries"
        family = "good"

        [[building]]
        name = "dwelling"
        houses = true
        premises = true
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
        setback_tiles = 2

        [capacity]
        floor_tiles_per_occupant      = 6
        floor_tiles_per_job           = 1
        floor_tiles_per_parking_space = 6
        """;
}
