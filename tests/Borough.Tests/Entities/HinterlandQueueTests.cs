using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Invariants;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Entities;

/// <summary>
/// <c>plans/0045</c> row 31 task 4: the family a full door turns away, and what becomes of it.
/// </summary>
/// <remarks>
/// <para>
/// <b>A queue row promises a Household and moves nobody</b> (<c>plans/0073</c> D5). The people are
/// still behind the edge and still counted there; the reservation only stops the same family being
/// drawn twice, once by the queue it stands in and once by a fresh occasion. Every test here is
/// about that promise being kept — served in order, released on a change of mind, released at the
/// authored wait, and released when the door it was waiting at stops existing.
/// </para>
/// <para>
/// ⚠ <b>The Ruleset is <c>attracted.toml</c> with one key rewritten.</b> A shipped gate takes 96
/// arrivals a Day and the stock presents nothing like that many willing families, so no queue would
/// ever form; at one arrival a Day the second willing family of the Day waits, which is the
/// condition under test rather than a tuning claim about the shipped file.
/// </para>
/// </remarks>
public sealed class HinterlandQueueTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(7);

    /// <summary>The shipped file's text, so one key can be rewritten without a second fixture file.</summary>
    private static string Text() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "attracted.toml"));

    private static Ruleset Parsed(string text)
    {
        RulesetLoadResult result = RulesetLoader.Parse(text, "attracted.toml");

        return result.Ruleset
            ?? throw new InvalidOperationException(
                $"the rewritten Ruleset was refused, so this test cannot run:\n{result.Describe()}");
    }

    /// <summary>One arrival a Day at each door, so a second willing family has to wait.</summary>
    private static Ruleset Narrow() =>
        Parsed(Text().Replace("arrivals_per_day = 96", "arrivals_per_day = 1", StringComparison.Ordinal));

    private static (World World, Simulation Simulation) City(Ruleset rules)
    {
        var world = new World(1_000, rules, Key);

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        var simulation = new Simulation(world, Key) { VerifyDecideWritesNothing = false };

        return (world, simulation);
    }

    /// <summary>Puts every door's meter at its Day's ceiling, so the queue cannot be served.</summary>
    /// <remarks>
    /// <b>The meter is written rather than spent through <c>TryArrive</c></b>, which would fill the
    /// Unplaced Pool with families this test never asked for and make demolishing the gate a
    /// different experiment.
    /// </remarks>
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

    /// <summary>Steps until somebody is waiting, holding every door shut while it runs.</summary>
    /// <returns>The edge they are waiting behind.</returns>
    private static int Waited(World world, Simulation simulation, int limit = 4 * Ticks.PerDay)
    {
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

    private static int Waiting(World world, int edge)
    {
        int rows = 0;

        foreach (int slot in world.HinterlandQueue.Admissions(world.Hinterlands).Walk(edge))
        {
            rows++;
        }

        return rows;
    }

    private static int GroupOf(World world, int slot) =>
        world.HinterlandPopulation.Rows.Resolve(world.HinterlandQueue.Group[slot]);

    /// <summary>A family standing outside is promised, not moved.</summary>
    /// <remarks>
    /// <b>The distinction the row exists for.</b> A queue that transferred its people would count
    /// them in the city while they stood outside it, and a change of mind would then have to invent a
    /// Household to send back.
    /// </remarks>
    [Fact]
    public void Waiting_reserves_a_Household_and_transfers_nobody()
    {
        (World world, Simulation simulation) = City(Narrow());

        int edge = Waited(world, simulation);
        int head = world.Hinterlands.AdmitHead[edge] - 1;
        int group = GroupOf(world, head);

        Assert.Equal(Waiting(world, edge), CountFor(world, group));
        Assert.Equal(CountFor(world, group), world.HinterlandPopulation.Reserved[group]);

        // The promise comes out of the free count and not out of the stock: the stock is still what
        // the account says it is, and the reservation is what nobody else may draw.
        Assert.Equal(
            world.HinterlandPopulation.Stock[group] - world.HinterlandPopulation.Reserved[group],
            world.HinterlandPopulation.Free(group));

        world.Invariants.RunEndOfRun(world);
    }

    private static int CountFor(World world, int group)
    {
        int rows = 0;

        for (int edge = 0; edge < HinterlandTable.Edges; edge++)
        {
            foreach (int slot in world.HinterlandQueue.Admissions(world.Hinterlands).Walk(edge))
            {
                if (GroupOf(world, slot) == group)
                {
                    rows++;
                }
            }
        }

        return rows;
    }

    /// <summary>The door serves the family that has waited longest.</summary>
    [Fact]
    public void Room_at_the_door_goes_to_the_oldest_wait()
    {
        (World world, Simulation simulation) = City(Narrow());

        int edge = Waited(world, simulation);

        while (Waiting(world, edge) < 2)
        {
            ShutTheDoors(world);
            simulation.Step(default);
        }

        LinkedIndexList admissions = world.HinterlandQueue.Admissions(world.Hinterlands);

        int first = admissions.PeekFront(edge);
        int second = admissions.After(first);

        Assert.True(
            world.HinterlandQueue.Since[first].Raw <= world.HinterlandQueue.Since[second].Raw,
            "the admission list is not in join order.");

        ulong front = world.HinterlandQueue.Identity[first];
        ulong behind = world.HinterlandQueue.Identity[second];

        // The doors are left open from here, so the Day's one arrival goes to somebody.
        for (int tick = 0; tick < Ticks.PerDay && StillWaiting(world, edge, front); tick++)
        {
            simulation.Step(default);

            Assert.True(
                !StillWaiting(world, edge, front) || StillWaiting(world, edge, behind),
                "the family behind left the queue while the family in front was still waiting.");
        }

        Assert.False(
            StillWaiting(world, edge, front), "no door opened, so the order was never tested.");
    }

    private static bool StillWaiting(World world, int edge, ulong identity)
    {
        foreach (int slot in world.HinterlandQueue.Admissions(world.Hinterlands).Walk(edge))
        {
            if (world.HinterlandQueue.Identity[slot] == identity)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Patience runs out at the authored wait and not a Tick either side of it.</summary>
    /// <remarks>
    /// <b>Measured from the join and never from the last review</b>, so reviewing a wait cannot
    /// extend it. The doors are held shut throughout, which is what makes the expiry the only way out.
    /// </remarks>
    [Fact]
    public void A_wait_ends_at_exactly_the_authored_duration()
    {
        (World world, Simulation simulation) = City(Narrow());

        int edge = Waited(world, simulation);
        int head = world.Hinterlands.AdmitHead[edge] - 1;

        ulong identity = world.HinterlandQueue.Identity[head];
        long since = (long)world.HinterlandQueue.Since[head].Raw;
        long wait = world.Rules.Immigration.QueueWaitTicks;

        // Step drives the Tick the world is standing on and then advances it, so the last Tick that
        // must still find the family waiting is the one before the wait is up.
        while ((long)world.Tick.Raw < since + wait)
        {
            ShutTheDoors(world);
            simulation.Step(default);

            Assert.True(
                StillWaiting(world, edge, identity),
                $"the family left the queue at Tick {world.Tick.Raw}, before its wait was up.");
        }

        ShutTheDoors(world);
        simulation.Step(default);

        Assert.False(
            StillWaiting(world, edge, identity), "the family outstayed the authored wait.");

        Assert.True(world.Hinterlands.ExpiredToday[edge] > 0);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>Losing the only door sends everybody waiting at it home.</summary>
    [Fact]
    public void Losing_the_last_gate_sends_the_queue_home()
    {
        (World world, Simulation simulation) = City(Narrow());

        int edge = Waited(world, simulation);
        int head = world.Hinterlands.AdmitHead[edge] - 1;
        int group = GroupOf(world, head);

        int stock = world.HinterlandPopulation.Stock[group];
        int waiting = Waiting(world, edge);

        Assert.True(waiting > 0);

        foreach (int gate in Gates(world, edge))
        {
            world.DestroyBuilding(world.Buildings.Rows.At(gate), world.Tick);
        }

        simulation.Step(default);

        Assert.Equal(0, Waiting(world, edge));
        Assert.Equal(waiting, world.Hinterlands.ConnectionLostToday[edge]);

        // The stock never moved, so the families are back where they always were.
        Assert.Equal(stock, world.HinterlandPopulation.Stock[group]);
        Assert.Equal(0, world.HinterlandPopulation.Reserved[group]);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>Losing one of two doors leaves the queue standing at the other.</summary>
    [Fact]
    public void Losing_one_of_two_gates_leaves_the_queue_standing()
    {
        (World world, Simulation simulation) = City(Narrow());

        int edge = Waited(world, simulation);

        RaiseGate(world, HinterlandTable.EdgeAt(edge));

        int waiting = Waiting(world, edge);
        int[] gates = Gates(world, edge);

        Assert.Equal(2, gates.Length);

        world.DestroyBuilding(world.Buildings.Rows.At(gates[0]), world.Tick);

        ShutTheDoors(world);
        simulation.Step(default);

        Assert.Equal(0, world.Hinterlands.ConnectionLostToday[edge]);
        Assert.True(Waiting(world, edge) >= waiting - 1, "the queue was sent home by a surviving door.");

        world.Invariants.RunEndOfRun(world);
    }

    private static int[] Gates(World world, int edge)
    {
        List<int> gates = [];

        foreach (int gate in world.Buildings.Gates(world.Hinterlands).Walk(edge))
        {
            gates.Add(gate);
        }

        return [.. gates];
    }

    /// <summary>Puts a second door on an edge, on the vacant ground the lattice leaves there.</summary>
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

    /// <summary>A reservation nobody is standing behind is caught.</summary>
    /// <remarks>
    /// <b>The count and the rows are separate state, so they can drift apart in silence.</b> A
    /// reservation left behind by a cancelled row takes a Household out of circulation for good, and
    /// nothing else in the suite compares the two.
    /// </remarks>
    [Fact]
    public void A_reservation_with_nobody_waiting_is_caught()
    {
        (World world, Simulation simulation) = City(Narrow());

        int edge = Waited(world, simulation);
        int group = GroupOf(world, world.Hinterlands.AdmitHead[edge] - 1);

        world.HinterlandPopulation.Reserved[group]++;

        Violation caught = Assert.Throws<InvariantViolationException>(
            () => world.Invariants.RunEndOfRun(world)).Violation;

        Assert.Equal(Invariant.TheQueueMatchesItsReservations, caught.Invariant);
        Assert.Equal(group, caught.Slot);
        Assert.Equal(1, caught.Other);
    }
}
