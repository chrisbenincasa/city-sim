using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Entities;

/// <summary>
/// <c>plans/0045</c> row 31 task 5: the <c>Arrive</c> command asking a counted Outside for people
/// instead of inventing them.
/// </summary>
/// <remarks>
/// <para>
/// <b>The verb used to be a generator.</b> It was handed a Life Stage and a head count and made a
/// family on the spot, so a runner could type a city into existence out of an Outside that held
/// nobody. Where a Ruleset states <c>[immigration]</c> the command now selects among the families
/// standing behind that edge and puts them through the comparison, the quota and the queue the edge's
/// own occasions use (<c>plans/0073</c> D8).
/// </para>
/// <para>
/// ⚠ <b>Two silences mean different things and both are here.</b> A composition nobody declares is a
/// refusal, because the command is asking for people who could not exist; a composition that exists
/// and is spent admits nobody and is refused by nothing, because a depleted edge is the mechanism
/// working.
/// </para>
/// </remarks>
public sealed class StockArrivalCommandTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(7);

    private static string Text(string file) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

    private static Ruleset Parsed(string text, string name)
    {
        RulesetLoadResult result = RulesetLoader.Parse(text, name);

        return result.Ruleset
            ?? throw new InvalidOperationException(
                $"the Ruleset was refused, so this test cannot run:\n{result.Describe()}");
    }

    private static Ruleset Shipped() => Parsed(Text("attracted.toml"), "attracted.toml");

    /// <summary>The shipped file with three Households behind west and nobody anywhere else.</summary>
    /// <remarks>
    /// <b>Small enough to exhaust, which is what makes a request countable.</b> Six hundred families
    /// behind an edge cannot be asked for often enough in one test to show a bound.
    /// </remarks>
    private static Ruleset Thin()
    {
        string text = Text("attracted.toml");
        int first = text.IndexOf("households      = 600", StringComparison.Ordinal);

        text = text[..first] + "households      = 3" + text[(first + "households      = 600".Length)..];

        return Parsed(
            text.Replace("households      = 600", "households      = 0", StringComparison.Ordinal)
                .Replace("households      = 200", "households      = 0", StringComparison.Ordinal)
                .Replace("households      = 100", "households      = 0", StringComparison.Ordinal),
            "thin.toml");
    }

    private static (World World, Simulation Simulation) City(Ruleset rules)
    {
        var world = new World(1_000, rules, Key);

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        var simulation = new Simulation(world, Key) { VerifyDecideWritesNothing = false };

        return (world, simulation);
    }

    /// <summary>The Tile the first gate on <paramref name="edge"/> stands on.</summary>
    private static (Tiles East, Tiles North) GateTile(World world, MapEdge edge)
    {
        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (!world.Lots.Rows.IsLive(slot)
                || world.Lots.IsVacant(slot)
                || world.EdgeOf(slot) != edge)
            {
                continue;
            }

            int building = world.Lots.BuildingOn(slot);

            if (building >= 0 && world.IsOutsideConnection(world.Buildings.Kind[building]))
            {
                return (world.Lots.East[slot], world.Lots.North[slot]);
            }
        }

        throw new InvalidOperationException($"this world has no gate on its {edge} edge.");
    }

    private static Command Ask(World world, MapEdge edge, ArrivePayload payload)
    {
        (Tiles east, Tiles north) = GateTile(world, edge);

        return new Command(CommandKind.Arrive, east, north, payload.Encode());
    }

    /// <summary>The group behind <paramref name="edge"/> whose Households hold one adult and nobody else.</summary>
    private static int Single(World world, MapEdge edge)
    {
        foreach (int slot in world.HinterlandPopulation.Groups(world.Hinterlands)
            .Walk(HinterlandTable.SlotOf(edge)))
        {
            if (world.HinterlandPopulation.Members(slot) == 1)
            {
                return slot;
            }
        }

        throw new InvalidOperationException($"nobody behind {edge} lives alone.");
    }

    /// <summary>A request for nobody is not a refusal, and it asks nobody.</summary>
    [Fact]
    public void A_request_for_no_Households_moves_nothing()
    {
        (World world, Simulation simulation) = City(Shipped());

        int group = Single(world, MapEdge.West);
        int standing = world.HinterlandPopulation.Stock[group];

        Command ask = Ask(world, MapEdge.West, new ArrivePayload(0, world.HinterlandPopulation.Stage[group], 1));

        Assert.Equal(Refusal.None, simulation.Refuses(ask));

        simulation.Step(new TickInput([ask], 0));

        Assert.Equal(0, world.Hinterlands.RequestedToday[HinterlandTable.SlotOf(MapEdge.West)]);
        Assert.True(
            world.HinterlandPopulation.Stock[group] <= standing,
            "a request for nobody put people behind the edge.");

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>A family of a shape nobody out there has is refused rather than invented.</summary>
    [Fact]
    public void A_composition_nobody_declares_is_refused()
    {
        (World world, Simulation simulation) = City(Shipped());

        // Every composition attracted.toml declares holds one, two or four people.
        Command ask = Ask(world, MapEdge.West, new ArrivePayload(1, 0, 9));

        ulong before = world.HashState();

        Assert.Equal(Refusal.ArriveNoSuchFamilyOutside, simulation.Refuses(ask));

        // Asking costs the world nothing, which is what lets a shell ask before it sends.
        Assert.Equal(before, world.HashState());

        Assert.Throws<InvalidOperationException>(() => simulation.Step(new TickInput([ask], 0)));
    }

    /// <summary>A declared composition standing at nobody admits nobody, and is refused by nothing.</summary>
    /// <remarks>
    /// <b>The distinction the refusal exists to draw.</b> An authored group with no Households in it
    /// is somebody the Outside supplies and has none of today, which is depletion rather than a
    /// command asking for people who cannot exist.
    /// </remarks>
    [Fact]
    public void A_declared_composition_that_is_spent_is_a_no_op()
    {
        (World world, Simulation simulation) = City(Thin());

        int group = Single(world, MapEdge.East);

        Assert.Equal(0, world.HinterlandPopulation.Stock[group]);

        Command ask = Ask(world, MapEdge.East, new ArrivePayload(4, world.HinterlandPopulation.Stage[group], 1));

        Assert.Equal(Refusal.None, simulation.Refuses(ask));

        simulation.Step(new TickInput([ask], 0));

        Assert.Equal(0, world.HinterlandPopulation.Admitted[group]);
        Assert.Equal(0, world.Hinterlands.RequestedToday[HinterlandTable.SlotOf(MapEdge.East)]);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>Asking for more families than stand behind the edge cannot produce more of them.</summary>
    [Fact]
    public void A_request_larger_than_the_stock_cannot_mint_people()
    {
        (World world, Simulation simulation) = City(Thin());

        int group = Single(world, MapEdge.West);
        int standing = world.HinterlandPopulation.Stock[group];

        Assert.Equal(3, standing);

        Command ask = Ask(world, MapEdge.West, new ArrivePayload(12, world.HinterlandPopulation.Stage[group], 1));

        simulation.Step(new TickInput([ask], 0));

        HinterlandPopulationTable groups = world.HinterlandPopulation;

        Assert.True(groups.Stock[group] >= 0, "the command spent stock the edge did not hold.");
        Assert.True(
            groups.Admitted[group] <= standing,
            $"{groups.Admitted[group]} families crossed out of {standing}.");
        Assert.Equal(standing, groups.Stock[group] + groups.Admitted[group]);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>Households already promised to a gate are not offered to a command as well.</summary>
    /// <remarks>
    /// <b>Both draws come out of the same three Households</b> (D5). A reservation is the whole of
    /// what stops a family being admitted once by the queue it is standing in and once by a runner
    /// asking for somebody of its shape.
    /// </remarks>
    [Fact]
    public void A_request_cannot_draw_stock_that_is_already_promised()
    {
        (World world, Simulation simulation) = City(Thin());

        int group = Single(world, MapEdge.West);
        int standing = world.HinterlandPopulation.Stock[group];

        ShutTheDoors(world);

        Command ask = Ask(world, MapEdge.West, new ArrivePayload(12, world.HinterlandPopulation.Stage[group], 1));

        simulation.Step(new TickInput([ask], 0));

        HinterlandPopulationTable groups = world.HinterlandPopulation;

        Assert.True(
            groups.Reserved[group] <= groups.Stock[group],
            "more Households are promised than stand behind the edge.");
        Assert.Equal(standing, groups.Stock[group]);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>Two commands on one Tick ask two different families.</summary>
    [Fact]
    public void Two_requests_on_one_Tick_are_two_occasions()
    {
        (World world, Simulation simulation) = City(Shipped());

        int group = Single(world, MapEdge.West);
        int edge = HinterlandTable.SlotOf(MapEdge.West);

        Command ask = Ask(world, MapEdge.West, new ArrivePayload(1, world.HinterlandPopulation.Stage[group], 1));

        ulong sequence = world.Hinterlands.Sequence[edge];

        simulation.Step(new TickInput([ask, ask], 0));

        Assert.Equal(2, world.Hinterlands.RequestedToday[edge]);
        Assert.True(
            world.Hinterlands.Sequence[edge] >= sequence + 2,
            "two requests shared one identity, so they were the same family asked twice.");

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>A commanded arrival and an autonomous one spend the same daily ceiling.</summary>
    [Fact]
    public void A_command_and_the_Outside_share_one_quota()
    {
        (World world, Simulation simulation) = City(
            Parsed(
                Text("attracted.toml").Replace(
                    "arrivals_per_day = 96", "arrivals_per_day = 2", StringComparison.Ordinal),
                "narrow.toml"));

        int group = Single(world, MapEdge.West);

        Command ask = Ask(world, MapEdge.West, new ArrivePayload(8, world.HinterlandPopulation.Stage[group], 1));

        simulation.Step(new TickInput([ask], 0));

        foreach (int gate in world.Buildings.Gates(world.Hinterlands).Walk(HinterlandTable.SlotOf(MapEdge.West)))
        {
            Assert.True(
                world.Buildings.ArrivalsToday[gate] <= 2,
                $"a gate admitted {world.Buildings.ArrivalsToday[gate]} on a ceiling of two.");
        }

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>A world stating no Outside stock keeps the verb it always had.</summary>
    /// <remarks>
    /// <b>The old path is the fixture path</b>, and every Ruleset but <c>attracted.toml</c> is on it.
    /// A command there still invents the family it names, because there is no stock for it to come
    /// out of and no account that would notice.
    /// </remarks>
    [Fact]
    public void A_world_with_no_stock_still_invents_the_family_it_is_asked_for()
    {
        (World world, Simulation simulation) = City(Parsed(Text("bordered.toml"), "bordered.toml"));

        Assert.False(world.Rules.Immigration.Stated);

        int before = world.Households.Rows.LiveCount;

        (Tiles east, Tiles north) = GateTile(world, EdgeOfAnyGate(world));

        simulation.Step(
            new TickInput(
                [new Command(CommandKind.Arrive, east, north, new ArrivePayload(2, 0, 2).Encode())], 0));

        Assert.True(
            world.Households.Rows.LiveCount > before,
            "the command that used to make a family made none.");

        world.Invariants.RunEndOfRun(world);
    }

    private static MapEdge EdgeOfAnyGate(World world)
    {
        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (world.Lots.Rows.IsLive(slot)
                && !world.Lots.IsVacant(slot)
                && world.Lots.BuildingOn(slot) >= 0
                && world.IsOutsideConnection(world.Buildings.Kind[world.Lots.BuildingOn(slot)]))
            {
                return world.EdgeOf(slot);
            }
        }

        throw new InvalidOperationException("this world raised no gate.");
    }

    /// <summary>Spends every gate's quota for the Day, so nobody can cross today.</summary>
    private static void ShutTheDoors(World world)
    {
        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (!world.Buildings.Rows.IsLive(slot)
                || !world.IsOutsideConnection(world.Buildings.Kind[slot]))
            {
                continue;
            }

            world.Buildings.ArrivalDay[slot] = (int)(world.Tick.Raw / Ticks.PerDay);
            world.Buildings.ArrivalsToday[slot] =
                world.Rules.Kind(world.Buildings.Kind[slot]).ArrivalsPerDay;
        }
    }
}
