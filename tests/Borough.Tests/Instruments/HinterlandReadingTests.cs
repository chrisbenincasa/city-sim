using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Instruments;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Instruments;

/// <summary>
/// <c>plans/0045</c> row 31 task 7: what an inspection may say about the Outside, and what reading it
/// is not allowed to cost.
/// </summary>
public sealed class HinterlandReadingTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(7);

    private static readonly MapEdge[] Edges =
        [MapEdge.West, MapEdge.East, MapEdge.South, MapEdge.North];

    private static Ruleset Shipped()
    {
        RulesetLoadResult result =
            RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "attracted.toml"));

        return result.Ruleset
            ?? throw new InvalidOperationException(
                $"the shipped attracted.toml was refused, so this test cannot run:\n{result.Describe()}");
    }

    private static (World World, Simulation Simulation) City()
    {
        var world = new World(1_000, Shipped(), Key);

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        var simulation = new Simulation(world, Key) { VerifyDecideWritesNothing = false };

        return (world, simulation);
    }

    private static void Step(Simulation simulation, int ticks)
    {
        for (int tick = 0; tick < ticks; tick++)
        {
            simulation.Step(default);
        }
    }

    /// <summary>Reads everything there is to read, so a caller can assert on what it cost.</summary>
    private static long ReadEverything(World world)
    {
        Span<HinterlandGateReading> gates = stackalloc HinterlandGateReading[32];
        Span<HinterlandGroupReading> groups = stackalloc HinterlandGroupReading[32];
        long seen = 0;

        foreach (MapEdge edge in Edges)
        {
            HinterlandReading reading = HinterlandReading.Of(world, edge);

            seen += reading.StockHouseholds + reading.QueueHouseholds + reading.AvailableHouseholds;
            seen += HinterlandGateReading.Of(world, edge, gates);
            seen += HinterlandGroupReading.Of(world, edge, groups);
        }

        return seen + PopulationReading.Of(world).People;
    }

    [Fact]
    public void A_reading_moves_no_state()
    {
        (World world, Simulation simulation) = City();

        Step(simulation, Ticks.PerDay + 64);

        ulong before = world.HashState();

        Assert.NotEqual(0, ReadEverything(world));
        Assert.Equal(before, world.HashState());
    }

    /// <summary>The four fresh outcomes are disjoint and exhaustive, so they add up to the occasions.</summary>
    [Fact]
    public void The_four_fresh_outcomes_add_up_to_the_occasions()
    {
        (World world, Simulation simulation) = City();

        Step(simulation, Ticks.PerDay);

        int occasions = 0;

        foreach (MapEdge edge in Edges)
        {
            HinterlandFlows today = HinterlandReading.Of(world, edge).Today;

            Assert.Equal(
                today.Occasions,
                today.NoConnection + today.NoSample + today.StayedOutside + today.Willing);

            occasions += today.Occasions;
        }

        Assert.True(occasions > 0, "no edge generated an occasion, so the identity is vacuous.");
    }

    /// <summary>An edge's stock is its compositions added up, and never a figure of its own.</summary>
    [Fact]
    public void An_edges_stock_is_its_compositions_added_up()
    {
        (World world, Simulation simulation) = City();

        Step(simulation, Ticks.PerDay);

        Span<HinterlandGroupReading> groups = stackalloc HinterlandGroupReading[32];

        foreach (MapEdge edge in Edges)
        {
            HinterlandReading reading = HinterlandReading.Of(world, edge);
            int written = HinterlandGroupReading.Of(world, edge, groups);

            Assert.Equal(reading.Compositions, written);

            int stock = 0;
            int reserved = 0;
            long people = 0;

            for (int group = 0; group < written; group++)
            {
                stock += groups[group].Stock;
                reserved += groups[group].Reserved;
                people += groups[group].People;

                Assert.Equal(edge, groups[group].Edge);
            }

            Assert.Equal(reading.StockHouseholds, stock);
            Assert.Equal(reading.ReservedHouseholds, reserved);
            Assert.Equal(reading.StockPeople, people);
            Assert.Equal(reading.AvailableHouseholds, stock - reserved);
        }
    }

    /// <summary>An edge's quota is its doors' quotas added up, and each door keeps its own meter.</summary>
    [Fact]
    public void An_edges_quota_is_its_doors_added_up()
    {
        (World world, Simulation simulation) = City();

        Step(simulation, Ticks.PerDay + 64);

        Span<HinterlandGateReading> gates = stackalloc HinterlandGateReading[32];
        int doors = 0;

        foreach (MapEdge edge in Edges)
        {
            HinterlandReading reading = HinterlandReading.Of(world, edge);
            int written = HinterlandGateReading.Of(world, edge, gates);

            Assert.Equal(reading.Gates, written);

            int admitted = 0;
            int remaining = 0;

            for (int gate = 0; gate < written; gate++)
            {
                Assert.Equal(edge, gates[gate].Edge);
                Assert.Equal(
                    gates[gate].Ceiling - gates[gate].AdmittedToday, gates[gate].RemainingToday);

                admitted += gates[gate].AdmittedToday;
                remaining += gates[gate].RemainingToday;
            }

            Assert.Equal(reading.AdmittedToday, admitted);
            Assert.Equal(reading.RemainingToday, remaining);

            doors += written;
        }

        Assert.True(doors > 0, "the generated city raised no gate, so this asserts nothing.");
    }

    /// <summary>
    /// The account and the live rows agree, which is a check on the ledger rather than on the city.
    /// </summary>
    [Fact]
    public void The_account_agrees_with_the_rows_it_is_kept_beside()
    {
        (World world, Simulation simulation) = City();

        Step(simulation, Ticks.PerDay + 64);

        PopulationReading reading = PopulationReading.Of(world);

        Assert.True(
            reading.Today.Admissions + reading.Yesterday.Admissions > 0,
            "nobody was admitted, so the residual is a check over a city that did nothing.");

        Assert.Equal(0, reading.Residual);
        Assert.Equal(0, reading.HouseholdResidual);
    }

    /// <summary>Yesterday's figures are exactly what today's were when the Day turned over.</summary>
    [Fact]
    public void Yesterday_is_what_today_was_when_the_Day_turned()
    {
        (World world, Simulation simulation) = City();

        Step(simulation, Ticks.PerDay);

        HinterlandReading westBefore = HinterlandReading.Of(world, MapEdge.West);
        PopulationReading accountBefore = PopulationReading.Of(world);

        Assert.Equal(0, westBefore.Day);
        Assert.Equal(0, accountBefore.Day);

        Step(simulation, 1);

        HinterlandReading westAfter = HinterlandReading.Of(world, MapEdge.West);
        PopulationReading accountAfter = PopulationReading.Of(world);

        Assert.Equal(1, westAfter.Day);
        Assert.Equal(1, accountAfter.Day);

        Assert.Equal(westBefore.Today, westAfter.Yesterday);
        Assert.Equal(accountBefore.Today, accountAfter.Yesterday);

        Assert.Equal(accountBefore.People, accountAfter.PeopleAtDayStart);
        Assert.Equal(
            accountAfter.People,
            accountAfter.PeopleAtDayStart + accountAfter.Today.PeopleIn
            - accountAfter.Today.PeopleOut);
    }
}
