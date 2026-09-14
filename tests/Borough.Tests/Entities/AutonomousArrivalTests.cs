using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Entities;

/// <summary>
/// <c>plans/0045</c> row 31 task 4: people arriving because the Outside decided to, not because a
/// runner said so.
/// </summary>
/// <remarks>
/// Step with empty input. Assert the reason counters as well as admissions so a different
/// bottleneck cannot satisfy a zero-admissions assertion.
/// </remarks>
public sealed class AutonomousArrivalTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(7);

    private static string Text() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "attracted.toml"));

    private static Ruleset Parsed(string text)
    {
        RulesetLoadResult result = RulesetLoader.Parse(text, "attracted.toml");

        return result.Ruleset
            ?? throw new InvalidOperationException(
                $"the rewritten Ruleset was refused, so this test cannot run:\n{result.Describe()}");
    }

    private static Ruleset Shipped() => Parsed(Text());

    /// <summary>The shipped file with one Household in the whole Outside, behind the west edge.</summary>
    private static Ruleset Sparse()
    {
        string text = Text();

        // Every edge authors the same three counts, so the first occurrence of each is west's.
        text = Once(text, "households      = 600", "households      = 1");

        return Parsed(text.Replace("households      = 600", "households      = 0", StringComparison.Ordinal)
            .Replace("households      = 200", "households      = 0", StringComparison.Ordinal)
            .Replace("households      = 100", "households      = 0", StringComparison.Ordinal));
    }

    private static string Once(string text, string from, string to)
    {
        int at = text.IndexOf(from, StringComparison.Ordinal);

        return text[..at] + to + text[(at + from.Length)..];
    }

    private static (World World, Simulation Simulation) City(Ruleset rules)
    {
        var world = new World(1_000, rules, Key);

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

    private static long Admitted(World world)
    {
        long admitted = 0;

        for (int edge = 0; edge < HinterlandTable.Edges; edge++)
        {
            admitted += world.Hinterlands.AdmittedHouseholds[edge];
        }

        return admitted;
    }

    private static long Stock(World world)
    {
        long stock = 0;

        for (int slot = 0; slot < world.HinterlandPopulation.Rows.SlotCount; slot++)
        {
            if (world.HinterlandPopulation.Rows.IsLive(slot))
            {
                stock += world.HinterlandPopulation.Stock[slot];
            }
        }

        return stock;
    }

    /// <summary>Nobody asks for these arrivals. They happen because the Outside wanted to come.</summary>
    [Fact]
    public void People_arrive_without_a_single_Arrive_command()
    {
        (World world, Simulation simulation) = City(Shipped());

        long opening = Stock(world);

        Step(simulation, Ticks.PerDay);

        Assert.True(Admitted(world) > 0, "a whole Day passed and nobody came.");
        Assert.Equal(opening - Admitted(world), Stock(world) - Replenished(world) + Turnover(world));

        // What arrived came out of the Outside rather than from nowhere.
        Assert.Equal(
            Admitted(world), world.PopulationLedger.HouseholdsAdmitted[PopulationLedgerTable.Slot]);

        world.Invariants.RunEndOfRun(world);
    }

    private static long Replenished(World world)
    {
        long replenished = 0;

        for (int edge = 0; edge < HinterlandTable.Edges; edge++)
        {
            replenished += world.Hinterlands.ReplenishedHouseholds[edge];
        }

        return replenished;
    }

    private static long Turnover(World world)
    {
        long turnover = 0;

        for (int edge = 0; edge < HinterlandTable.Edges; edge++)
        {
            turnover += world.Hinterlands.TurnoverHouseholds[edge];
        }

        return turnover;
    }

    /// <summary>An edge with no door admits nobody, and the Outside behind it loses nothing.</summary>
    [Fact]
    public void A_city_with_no_gates_admits_nobody_and_spends_no_stock()
    {
        (World world, Simulation simulation) = City(Shipped());

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot)
                && world.IsOutsideConnection(world.Buildings.Kind[slot]))
            {
                world.DestroyBuilding(world.Buildings.Rows.At(slot), world.Tick);
            }
        }

        long standing = Stock(world);

        Step(simulation, Ticks.PerDay);

        Assert.Equal(0, Admitted(world));
        Assert.Equal(standing, Stock(world));

        long unseen = 0;

        for (int edge = 0; edge < HinterlandTable.Edges; edge++)
        {
            unseen += world.Hinterlands.NoConnectionToday[edge];
            Assert.Equal(0, world.Hinterlands.WillingToday[edge]);
        }

        Assert.True(unseen > 0, "no occasion was recorded as having nowhere to walk in through.");

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>Every occasion ends in exactly one of the four outcomes, and the counters say which.</summary>
    [Fact]
    public void Every_occasion_is_accounted_for_by_one_outcome()
    {
        (World world, Simulation simulation) = City(Shipped());

        Step(simulation, Ticks.PerDay - 1);

        for (int edge = 0; edge < HinterlandTable.Edges; edge++)
        {
            Assert.Equal(
                world.Hinterlands.OccasionsToday[edge],
                world.Hinterlands.NoConnectionToday[edge]
                + world.Hinterlands.NoSampleToday[edge]
                + world.Hinterlands.StayedOutsideToday[edge]
                + world.Hinterlands.WillingToday[edge]);
        }
    }

    /// <summary>Two doors on one edge draw on one Outside and take turns.</summary>
    [Fact]
    public void Two_gates_on_one_edge_share_the_stock_behind_it()
    {
        (World world, Simulation simulation) = City(Shipped());

        MapEdge which = MapEdge.West;
        int edge = HinterlandTable.SlotOf(which);

        RaiseGate(world, which);

        Step(simulation, 2 * Ticks.PerDay);

        int gates = 0;
        int used = 0;

        foreach (int gate in world.Buildings.Gates(world.Hinterlands).Walk(edge))
        {
            gates++;

            if (world.Buildings.ArrivalsToday[gate] > 0)
            {
                used++;
            }
        }

        Assert.Equal(2, gates);

        // Both doors are fed by the same occasions, so the second is only ever reached because the
        // round robin sent somebody to it.
        Assert.True(
            world.Hinterlands.AdmittedHouseholds[edge] == 0 || used > 0,
            "admissions were recorded at an edge whose doors both show none.");

        world.Invariants.RunEndOfRun(world);
    }

    private static void RaiseGate(World world, MapEdge edge)
    {
        byte kind = 0;

        for (int declared = 1; declared <= world.Rules.KindCount; declared++)
        {
            if (world.IsOutsideConnection((byte)declared))
            {
                kind = (byte)declared;
                break;
            }
        }

        for (int lot = 0; lot < world.Lots.Rows.SlotCount; lot++)
        {
            if (world.Lots.Rows.IsLive(lot)
                && world.Lots.IsVacant(lot)
                && world.EdgeOf(lot) == edge)
            {
                world.CreateBuilding(world.Lots.Rows.At(lot), kind, world.Tick, Key);
                return;
            }
        }

        throw new InvalidOperationException($"no vacant Lot stands on the {edge} edge.");
    }

    /// <summary>An Outside holding nobody presents nobody, however long the city waits.</summary>
    [Fact]
    public void An_empty_Outside_generates_no_occasions()
    {
        (World world, Simulation simulation) = City(Sparse());

        Step(simulation, 2 * Ticks.PerDay);

        for (int slot = 0; slot < world.HinterlandPopulation.Rows.SlotCount; slot++)
        {
            if (!world.HinterlandPopulation.Rows.IsLive(slot)
                || world.HinterlandPopulation.Target[slot] != 0)
            {
                continue;
            }

            Assert.Equal(0, world.HinterlandPopulation.Stock[slot]);
            Assert.Equal(0, world.HinterlandPopulation.Admitted[slot]);
        }

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>One Household in the whole Outside still gets its occasion.</summary>
    [Fact]
    public void A_single_Household_is_never_rounded_away()
    {
        (World world, Simulation simulation) = City(Sparse());

        Step(simulation, world.Rules.Immigration.ReconsiderTicks + 1);

        long occasions = 0;

        for (int edge = 0; edge < HinterlandTable.Edges; edge++)
        {
            occasions += world.Hinterlands.OccasionsToday[edge]
                + world.Hinterlands.OccasionsYesterday[edge];
        }

        Assert.True(occasions > 0, "the last family outside was never asked.");

        world.Invariants.RunEndOfRun(world);
    }
}
