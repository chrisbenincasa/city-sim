using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Instruments;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Entities;

/// <summary>
/// <c>plans/0073</c> D14: <b>the player's own door, raised and taken away through the verb.</b>
/// </summary>
/// <remarks>
/// <para>
/// <c>RefusalTests</c> owns every gate refusal reason — it enumerates <see cref="Refusal"/> and goes
/// red on a member with no case — so this class asserts the accepting half instead. What a door that
/// really lands does to the stock behind its edge, to the families waiting at it, and to a log that
/// replays it.
/// </para>
/// <para>
/// 🔴 <b>Every gate here is raised by <see cref="CommandKind.Gate"/>.</b> The three existing
/// second-gate fixtures call <c>World.CreateBuilding</c> directly, which runs no Phase 0 and cannot
/// fail the way a command can — so nothing yet asserted that the verb reaches the same mechanism.
/// </para>
/// </remarks>
public sealed class GateCommandTests
{
    private const int Citizens = 1_000;

    private static readonly WorldKey Key = WorldKey.FromSeed(7);

    /// <summary>A door stands on that plot, and the edge's list grew by one.</summary>
    [Fact]
    public void A_door_lands_on_the_plot_the_command_named()
    {
        (World world, Simulation simulation) = City(Attracted());

        int lot = Doorstep(world, out MapEdge edge);
        int before = HinterlandReading.Of(world, edge).Gates;

        Command gate = Command.Gate(world.Lots.East[lot], world.Lots.North[lot], GateKind(world));

        Assert.Equal(Refusal.None, simulation.Refuses(gate));

        simulation.Step(new TickInput([gate], 0));

        Assert.Equal(before + 1, HinterlandReading.Of(world, edge).Gates);

        int raised = world.Lots.BuildingOn(lot);

        Assert.True(raised >= 0 && world.IsOutsideConnection(world.Buildings.Kind[raised]));

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// A second door widens the way in and adds no second Outside behind the edge.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The stock is asserted across the placement, not after a run.</b> A door that created
    /// population would read as a working mechanism in every later figure — more admissions through
    /// two doors is what the player expects to see either way.
    /// </remarks>
    [Fact]
    public void A_second_door_on_one_edge_shares_the_stock_behind_it()
    {
        (World world, Simulation simulation) = City(Attracted());

        int lot = Doorstep(world, out MapEdge edge);
        HinterlandReading before = HinterlandReading.Of(world, edge);

        Assert.True(before.Gates >= 1, "the generated city left this edge with no door to stand beside.");

        simulation.Step(new TickInput(
            [Command.Gate(world.Lots.East[lot], world.Lots.North[lot], GateKind(world))], 0));

        HinterlandReading after = HinterlandReading.Of(world, edge);

        Assert.Equal(before.Gates + 1, after.Gates);
        Assert.Equal(before.StockHouseholds, after.StockHouseholds);
        Assert.Equal(before.StockPeople, after.StockPeople);
        Assert.Equal(before.Compositions, after.Compositions);
        Assert.Equal(before.RestingHouseholds, after.RestingHouseholds);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>A door placed by a command admits on the Tick it appears.</summary>
    /// <remarks>
    /// Phase 0 raises it and phase 6 walks the admission list, so the families already waiting at the
    /// shut doors reach this one without waiting a Tick for it to be noticed.
    /// </remarks>
    [Fact]
    public void A_door_placed_by_a_command_admits_on_the_Tick_it_appears()
    {
        (World world, Simulation simulation) = City(Attracted());

        int edge = Waited(world, simulation);
        MapEdge named = HinterlandTable.EdgeAt(edge);
        int lot = DoorstepOn(world, named);

        long before = world.Hinterlands.AdmittedHouseholds[edge];

        // The standing doors only; the new one is created inside the Step below and keeps its own
        // full quota for the Day, which is what lets it admit immediately.
        ShutTheDoors(world);

        simulation.Step(new TickInput(
            [Command.Gate(world.Lots.East[lot], world.Lots.North[lot], GateKind(world))], 0));

        // ⚠ The new door's OWN meter, not the edge's total. A Tick that happened to roll the Day
        // would reopen the standing doors and raise that total without this door admitting anybody.
        int opened = world.Lots.BuildingOn(lot);

        Assert.True(
            world.Buildings.ArrivalsToday[opened] > 0,
            "the door appeared and admitted nobody on the Tick it opened.");
        Assert.True(world.Hinterlands.AdmittedHouseholds[edge] > before);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// Removing the last door sends the waiting families home, and a new one lets them start again.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A cancelled reservation must come back as stock rather than vanish.</b> The queue holds
    /// Households that were never subtracted from the Outside, so a removal that dropped the rows
    /// without releasing the count would take those families out of circulation for good.
    /// </remarks>
    [Fact]
    public void Removing_the_last_door_sends_the_waiting_families_home()
    {
        (World world, Simulation simulation) = City(Attracted());

        int edge = Waited(world, simulation);
        MapEdge named = HinterlandTable.EdgeAt(edge);
        HinterlandReading waiting = HinterlandReading.Of(world, named);

        Assert.True(waiting.QueueHouseholds > 0 && waiting.ReservedHouseholds > 0);

        foreach (int gate in Doors(world, named))
        {
            simulation.Step(new TickInput([Removal(world, gate)], 0));
        }

        HinterlandReading sent = HinterlandReading.Of(world, named);

        Assert.Equal(0, sent.Gates);
        Assert.Equal(0, sent.QueueHouseholds);
        Assert.Equal(0, sent.ReservedHouseholds);
        Assert.Equal(waiting.StockHouseholds, sent.StockHouseholds);
        Assert.True(world.Hinterlands.ConnectionLostToday[edge] > 0);

        int lot = DoorstepOn(world, named);
        long admitted = world.Hinterlands.AdmittedHouseholds[edge];

        simulation.Step(new TickInput(
            [Command.Gate(world.Lots.East[lot], world.Lots.North[lot], GateKind(world))], 0));

        for (int tick = 0; tick < Ticks.PerDay; tick++)
        {
            simulation.Step(default);
        }

        Assert.True(
            world.Hinterlands.AdmittedHouseholds[edge] > admitted,
            "the replacement door resumed nothing, so the lost connection was not restored.");

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>A refused door leaves the city exactly as it was, including the second removal.</summary>
    /// <remarks>
    /// <b>Stated as a State Hash equality</b> rather than as a claim about <c>ApplyGate</c>'s write
    /// order. ⚠ The world is stepped once first: the founding seal and the Day rollover both write,
    /// and a refusal measured across Tick 0 would be measured across those instead.
    /// </remarks>
    [Fact]
    public void A_refused_door_changes_nothing()
    {
        (World world, Simulation simulation) = City(Attracted());

        simulation.Step(default);

        int standing = Doors(world, HinterlandTable.EdgeAt(0))[0];
        Command removal = Removal(world, standing);

        simulation.Step(new TickInput([removal], 0));

        ulong before = world.HashState();

        Command[] refused =
        [
            // Nowhere near a Lot, a kind id past the end of the byte, and a door already taken away.
            Command.Gate(new Tiles(9_000), new Tiles(9_000), GateKind(world)),
            new Command(CommandKind.Gate, removal.East, removal.North, zone: 300),
            removal,
        ];

        foreach (Command command in refused)
        {
            Assert.NotEqual(Refusal.None, simulation.Refuses(command));
            Assert.Throws<InvalidOperationException>(
                () => simulation.Step(new TickInput([command], 0)));
            Assert.Equal(before, world.HashState());
        }

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>The verb survives the log it is written to, removal included.</summary>
    /// <remarks>
    /// A removal is a kind of zero, so a codec that dropped an absent payload would write a line that
    /// reads back as a placement of kind zero — or as nothing at all.
    /// </remarks>
    [Fact]
    public void A_door_command_survives_the_log_it_is_written_to()
    {
        InputLogBuilder builder = new(seed: 7, new WorldConfiguration(Citizens), rulesetHash: 0);

        builder.Append(new Ticks(3), Command.Gate(new Tiles(64), new Tiles(-96), kind: 5));
        builder.Append(new Ticks(4), Command.Gate(new Tiles(64), new Tiles(-96), kind: 0));

        string text = InputLogCodec.ToText(builder.Build());

        Assert.Contains("gate", text, StringComparison.Ordinal);
        Assert.Equal(text, InputLogCodec.ToText(InputLogCodec.FromText(text)));
    }

    /// <summary>A log holding a door replays to the same city twice.</summary>
    /// <remarks>
    /// 🔴 <b>The Tile is scouted from a replay rather than chosen.</b> Gate positions are the
    /// subdivider's, so a hand-picked Tile would name a Lot that happens to be vacant in a world built
    /// the fixture's way and occupied in the one the log builds.
    /// </remarks>
    [Fact]
    public void A_log_holding_a_door_replays_the_same_city_twice()
    {
        Ruleset rules = Attracted();

        Simulation scout = Replay.Start(Populated(), rules);

        Replay.Trace(scout, Populated(), new Ticks(1), hashEvery: 1, []);

        int lot = Doorstep(scout.World, out MapEdge edge);
        InputLog log = Populated(
            Command.Gate(scout.World.Lots.East[lot], scout.World.Lots.North[lot], GateKind(scout.World)));

        (ulong[] first, World left) = Replayed(log, rules);
        (ulong[] second, World right) = Replayed(log, rules);

        Assert.Equal(first, second);
        Assert.Equal(
            HinterlandReading.Of(left, edge).Gates,
            HinterlandReading.Of(right, edge).Gates);
        Assert.True(
            HinterlandReading.Of(left, edge).Gates >= 2,
            "the replayed log raised no second door, so it asserts nothing about the verb.");

        left.Invariants.RunEndOfRun(left);
        right.Invariants.RunEndOfRun(right);
    }

    private static (ulong[] Trace, World World) Replayed(InputLog log, Ruleset rules)
    {
        Simulation simulation = Replay.Start(log, rules);
        List<ulong> trace = [];

        Replay.Trace(simulation, log, new Ticks(64), hashEvery: 1, trace);

        return ([.. trace], simulation.World);
    }

    /// <summary>A log that builds the city, and optionally does one thing to it afterwards.</summary>
    private static InputLog Populated(Command? then = null)
    {
        InputLogBuilder builder = new(seed: 7, new WorldConfiguration(Citizens), rulesetHash: 0);

        builder.Append(Ticks.Zero, new Command(CommandKind.Populate, default, default));

        if (then is Command command)
        {
            builder.Append(new Ticks(8), command);
        }

        return builder.Build();
    }

    private static Ruleset Attracted() =>
        Parsed(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "attracted.toml")));

    private static Ruleset Parsed(string text)
    {
        RulesetLoadResult result = RulesetLoader.Parse(text, "attracted.toml");

        return result.Ruleset
            ?? throw new InvalidOperationException(
                $"the Ruleset was refused, so this test cannot run:\n{result.Describe()}");
    }

    private static (World World, Simulation Simulation) City(Ruleset rules)
    {
        var world = new World(Citizens, rules, Key);

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        var simulation = new Simulation(world, Key) { VerifyDecideWritesNothing = false };

        return (world, simulation);
    }

    /// <summary>The first kind declaring a door's width.</summary>
    private static byte GateKind(World world)
    {
        for (int kind = 1; kind <= world.Rules.KindCount; kind++)
        {
            if (world.IsOutsideConnection((byte)kind))
            {
                return (byte)kind;
            }
        }

        throw new InvalidOperationException("this Ruleset declares no gate kind.");
    }

    /// <summary>A vacant Lot a door could stand on, and the edge it faces.</summary>
    private static int Doorstep(World world, out MapEdge edge)
    {
        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (!world.Lots.Rows.IsLive(slot)
                || !world.Lots.IsVacant(slot)
                || !world.Lots.HasFrontage(slot))
            {
                continue;
            }

            MapEdge facing = world.EdgeOf(slot);

            if (facing != MapEdge.None && world.Rules.TryHinterland(facing, out HinterlandDefinition _))
            {
                edge = facing;

                return slot;
            }
        }

        throw new InvalidOperationException(
            "this city has no vacant edge Lot with frontage, so no door can be placed on it.");
    }

    /// <summary>The same, on one named edge.</summary>
    private static int DoorstepOn(World world, MapEdge edge)
    {
        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (world.Lots.Rows.IsLive(slot)
                && world.Lots.IsVacant(slot)
                && world.Lots.HasFrontage(slot)
                && world.EdgeOf(slot) == edge)
            {
                return slot;
            }
        }

        throw new InvalidOperationException($"the {edge} edge has no vacant Lot with frontage.");
    }

    private static int[] Doors(World world, MapEdge edge)
    {
        List<int> gates = [];

        foreach (int gate in world.Buildings.Gates(world.Hinterlands).Walk(HinterlandTable.SlotOf(edge)))
        {
            gates.Add(gate);
        }

        return [.. gates];
    }

    /// <summary>The command that takes one standing door away. Kind zero is the instruction.</summary>
    private static Command Removal(World world, int gate)
    {
        Assert.True(world.Lots.Rows.TryResolve(world.Buildings.Lot[gate], out int lot));

        return Command.Gate(world.Lots.East[lot], world.Lots.North[lot], kind: 0);
    }

    /// <summary>Spend every door's quota for the Day, so willingness has to wait.</summary>
    private static void ShutTheDoors(World world)
    {
        int day = (int)(world.Tick.Raw / Ticks.PerDay);

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (!world.Buildings.Rows.IsLive(slot)
                || !world.IsOutsideConnection(world.Buildings.Kind[slot]))
            {
                continue;
            }

            world.Buildings.ArrivalDay[slot] = day;
            world.Buildings.ArrivalsToday[slot] =
                world.Rules.Kind(world.Buildings.Kind[slot]).ArrivalsPerDay;
        }
    }

    /// <returns>The slot of the edge somebody is waiting behind.</returns>
    private static int Waited(World world, Simulation simulation)
    {
        int limit = 4 * Ticks.PerDay;

        for (int tick = 0; tick < limit; tick++)
        {
            ShutTheDoors(world);
            simulation.Step(default);

            for (int edge = 0; edge < HinterlandTable.Edges; edge++)
            {
                if (world.Hinterlands.AdmitHead[edge] != 0)
                {
                    return edge;
                }
            }
        }

        throw new InvalidOperationException(
            $"nobody was waiting at a shut door after {limit} Ticks, so this test asserts nothing.");
    }
}
