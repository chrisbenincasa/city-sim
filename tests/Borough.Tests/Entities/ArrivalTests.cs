using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Invariants;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Entities;

/// <summary>
/// Milestone 11 task 4: the arrival door, the Pool's gate column, and the ceiling that binds.
/// </summary>
/// <remarks>
/// <para>
/// <b>What is under test is that a Household can exist here having never lived here.</b>
/// <c>World.CreateHousehold</c> demands a dwelling and <c>World.Unplace</c> reports
/// <see cref="Invariant.OnlyAHousedHouseholdIsUnplaced"/>, so between them the build could not hold
/// one — while <c>CONTEXT.md</c> → Unplaced Pool says the Pool's four entry routes *"all enter on
/// equal terms"*. <c>World.TryArrive</c> is which one moved
/// (<see href="../../../docs/adr/0129-the-pool-waits-at-the-gate-and-an-arrivals-trip-is-the-move-in.md">adr/0129</see>).
/// </para>
/// <para>
/// ⚠ <b>Nobody makes a Trip here and that is the ADR's finding rather than a gap.</b>
/// <c>adr/0023</c> reads *arrive as Trips, enter the Pool, house themselves*, and that order cannot
/// be built: a Household the Pool has not placed has no destination Address. ***A journey described
/// in prose can name an endpoint the mechanism has to compute.*** The move-in is <b>task 6</b>'s.
/// </para>
/// </remarks>
public sealed class ArrivalTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(1);

    private static Ruleset Shipped(string file)
    {
        RulesetLoadResult result =
            RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

        return result.Ruleset
            ?? throw new InvalidOperationException(
                $"the shipped Ruleset {file} was refused, so this test cannot run:\n{result.Describe()}");
    }

    private static World Bordered(int citizens = 1_000)
    {
        var world = new World(citizens, Shipped("bordered.toml"));

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        return world;
    }

    /// <summary>Every standing Outside Connection, by Building slot.</summary>
    private static List<int> Gates(World world)
    {
        var found = new List<int>();

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot)
                && world.IsOutsideConnection(world.Buildings.Kind[slot]))
            {
                found.Add(slot);
            }
        }

        return found;
    }

    /// <summary>A Household that has never lived here now exists, in the Pool, at a named gate.</summary>
    [Fact]
    public void An_arrival_joins_the_pool_at_the_gate_it_came_through()
    {
        World world = Bordered();
        int gate = Gates(world)[0];
        int before = world.UnplacedPool.Count;

        Assert.True(
            world.TryArrive(
                world.Buildings.Rows.At(gate), lifeStage: 0, Ticks.Zero, out var household));

        Assert.Equal(before + 1, world.UnplacedPool.Count);

        int slot = world.Households.Rows.Resolve(household);

        Assert.True(world.Households.IsUnplaced(slot));
        Assert.Equal(
            world.Buildings.Rows.At(gate),
            world.UnplacedPool.GateAt(world.Households.PoolPosition(slot)));
    }

    /// <summary>
    /// <b>Nobody is housed by arriving</b>, which is the line between the Pool and a dwelling.
    /// </summary>
    /// <remarks>
    /// A Household with a dwelling handle would be in an occupant list as well as in the Pool, which
    /// is <see cref="Invariant.HouseholdIsHousedOrInThePool"/>'s exact violation — and it would be
    /// drawn for a second dwelling with both occupancies reading as consistent.
    /// </remarks>
    [Fact]
    public void An_arrival_is_housed_nowhere()
    {
        World world = Bordered();

        Assert.True(
            world.TryArrive(
                world.Buildings.Rows.At(Gates(world)[0]), lifeStage: 0, Ticks.Zero, out var household));

        int slot = world.Households.Rows.Resolve(household);

        Assert.False(world.Buildings.Rows.TryResolve(world.Households.Dwelling[slot], out _));

        foreach (int gate in Gates(world))
        {
            Assert.Equal(0, world.Occupants.Length(gate));
        }
    }

    /// <summary>
    /// 🔴 <b>The gate's declared ceiling binds, which is what makes the number ratifiable at all.</b>
    /// </summary>
    /// <remarks>
    /// <c>bordered.toml</c> states <c>arrivals_per_day = 12</c>, and <c>plans/0002</c> §D1's row asks
    /// whether the Pool's depth is bounded by <em>this</em> number or by placement. It cannot be
    /// asked of a door that admits everybody, so this is the assertion that turns the row from a
    /// number in a file into a number a run can refute.
    /// </remarks>
    [Fact]
    public void A_gate_admits_no_more_than_its_declared_ceiling_in_a_day()
    {
        World world = Bordered();
        int gate = Gates(world)[0];
        int ceiling = world.Rules.Kind(world.Buildings.Kind[gate]).ArrivalsPerDay;

        Assert.True(ceiling > 0);

        int admitted = 0;

        for (int i = 0; i < ceiling * 3; i++)
        {
            if (world.TryArrive(world.Buildings.Rows.At(gate), 0, Ticks.Zero, out _))
            {
                admitted++;
            }
        }

        Assert.Equal(ceiling, admitted);
    }

    /// <summary>The ceiling is a rate, so the next Day admits a fresh quota.</summary>
    /// <remarks>
    /// <b>This is the difference between a ceiling and a lifetime cap</b>, and it is the half a
    /// per-call bound could not express. <c>BuildingTable.ArrivalDay</c> is what carries it, and the
    /// reset is lazy — nothing sweeps the Buildings at midnight.
    /// </remarks>
    [Fact]
    public void The_quota_refills_on_the_next_day()
    {
        World world = Bordered();
        int gate = Gates(world)[0];
        int ceiling = world.Rules.Kind(world.Buildings.Kind[gate]).ArrivalsPerDay;

        for (int i = 0; i < ceiling; i++)
        {
            Assert.True(world.TryArrive(world.Buildings.Rows.At(gate), 0, Ticks.Zero, out _));
        }

        Assert.False(world.TryArrive(world.Buildings.Rows.At(gate), 0, Ticks.Zero, out _));

        Assert.True(
            world.TryArrive(
                world.Buildings.Rows.At(gate), 0, new Ticks(Ticks.PerDay), out _));
    }

    /// <summary>Each gate meters its own Day, so a full one does not close the others.</summary>
    [Fact]
    public void One_gates_ceiling_does_not_bind_another()
    {
        World world = Bordered();
        List<int> gates = Gates(world);

        Assert.True(gates.Count > 1);

        int ceiling = world.Rules.Kind(world.Buildings.Kind[gates[0]]).ArrivalsPerDay;

        for (int i = 0; i < ceiling; i++)
        {
            Assert.True(world.TryArrive(world.Buildings.Rows.At(gates[0]), 0, Ticks.Zero, out _));
        }

        Assert.False(world.TryArrive(world.Buildings.Rows.At(gates[0]), 0, Ticks.Zero, out _));
        Assert.True(world.TryArrive(world.Buildings.Rows.At(gates[1]), 0, Ticks.Zero, out _));
    }

    /// <summary>
    /// A Building that is not a gate admits nobody, and says so as an invariant.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>This is the one refusal that reports, and the ceiling's is deliberately not.</b> A full
    /// gate is the mechanism working; a Building that cannot admit anybody at all is a caller naming
    /// the wrong place, and the arrival's gate becomes its move-in Trip's origin — so the mistake
    /// would surface at placement, long after the call that was wrong.
    /// </remarks>
    [Fact]
    public void A_building_that_is_not_a_gate_admits_nobody()
    {
        World world = Bordered();
        int dwelling = -1;

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot)
                && !world.IsOutsideConnection(world.Buildings.Kind[slot]))
            {
                dwelling = slot;
                break;
            }
        }

        Assert.True(dwelling >= 0);

        int before = world.UnplacedPool.Count;

        world.Invariants.Collect = true;

        Assert.False(world.TryArrive(world.Buildings.Rows.At(dwelling), 0, Ticks.Zero, out _));
        Assert.Equal(before, world.UnplacedPool.Count);
        Assert.Contains(
            world.Invariants.Collected,
            violation => violation.Invariant == Invariant.AnArrivalCrossesAnOutsideConnection);
    }

    /// <summary>
    /// 🔴 <b>The Pool's gate column survives the swap <c>Leave</c> does to keep the Pool dense.</b>
    /// </summary>
    /// <remarks>
    /// <b>The regression this exists for is silent by construction.</b>
    /// <c>UnplacedTable.Leave</c> moves the last member into the vacated position; a swap that
    /// carried the Household handle and left the gate behind would give the moved family the
    /// leaver's origin, and its move-in Trip would run between two real Addresses from a gate it
    /// never came through. ***A wrong answer that is a legitimate journey is not a wrong answer
    /// anything downstream can see.***
    /// </remarks>
    [Fact]
    public void A_pool_member_keeps_its_gate_when_another_leaves()
    {
        World world = Bordered();
        List<int> gates = Gates(world);

        Assert.True(gates.Count > 1);

        Assert.True(world.TryArrive(world.Buildings.Rows.At(gates[0]), 0, Ticks.Zero, out var first));
        Assert.True(world.TryArrive(world.Buildings.Rows.At(gates[1]), 0, Ticks.Zero, out var second));

        int firstPosition = world.Households.PoolPosition(world.Households.Rows.Resolve(first));

        // The leaver is at the front, so the second arrival is the member Leave swaps down into it.
        world.UnplacedPool.Leave(world.Households, firstPosition);

        int moved = world.Households.PoolPosition(world.Households.Rows.Resolve(second));

        Assert.Equal(world.Buildings.Rows.At(gates[1]), world.UnplacedPool.GateAt(moved));
    }

    /// <summary>An evicted Household is in the Pool with no gate, which is the ordinary reading.</summary>
    /// <remarks>
    /// Three of the Pool's four entry routes have no gate at all, so <c>default</c> has to be a legal
    /// value here rather than a hole — ***a column that is meaningless for half its rows is a column
    /// describing something else***, which is why the gate is on the membership and not on the
    /// Household.
    /// </remarks>
    [Fact]
    public void An_eviction_enters_the_pool_with_no_gate()
    {
        World world = Bordered();
        int housed = -1;

        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (world.Households.Rows.IsLive(slot) && !world.Households.IsUnplaced(slot))
            {
                housed = slot;
                break;
            }
        }

        Assert.True(housed >= 0);

        world.Unplace(world.Households.Rows.At(housed));

        Assert.Equal(
            default,
            world.UnplacedPool.GateAt(world.Households.PoolPosition(housed)));
    }

    /// <summary>The verb admits Households through the gate the command names.</summary>
    [Fact]
    public void The_arrive_verb_admits_through_the_named_gate()
    {
        Ruleset rules = Shipped("bordered.toml");
        InputLogBuilder builder = new(1, new WorldConfiguration(1_000), rulesetHash: 0);
        Simulation simulation = Replay.Start(builder.Build(), rules);

        simulation.Step(new TickInput([new Command(CommandKind.Populate, default, default)], 0));

        World world = simulation.World;
        int gate = Gates(world)[0];
        int lot = world.Lots.Rows.Resolve(world.Buildings.Lot[gate]);
        int before = world.UnplacedPool.Count;

        simulation.Step(
            new TickInput(
                [
                    new Command(
                        CommandKind.Arrive,
                        world.Lots.East[lot],
                        world.Lots.North[lot],
                        new ArrivePayload(Households: 5, LifeStage: 2).Encode()),
                ],
                0));

        Assert.Equal(before + 5, world.UnplacedPool.Count);
    }

    /// <summary>
    /// 🔴 <b>Asking for more than the gate admits is an ordinary outcome, not a refusal.</b>
    /// </summary>
    /// <remarks>
    /// ***A command that asks for a hundred and gets twelve is the ceiling being observable***, which
    /// is the whole of what <c>plans/0002</c> §D1 needs of <c>arrivals_per_day</c>. A verb that threw
    /// here would make the bound unreadable from a session.
    /// </remarks>
    [Fact]
    public void Asking_for_more_than_the_gate_admits_delivers_the_ceiling()
    {
        Ruleset rules = Shipped("bordered.toml");
        InputLogBuilder builder = new(1, new WorldConfiguration(1_000), rulesetHash: 0);
        Simulation simulation = Replay.Start(builder.Build(), rules);

        simulation.Step(new TickInput([new Command(CommandKind.Populate, default, default)], 0));

        World world = simulation.World;
        int gate = Gates(world)[0];
        int lot = world.Lots.Rows.Resolve(world.Buildings.Lot[gate]);
        int ceiling = world.Rules.Kind(world.Buildings.Kind[gate]).ArrivalsPerDay;
        int before = world.UnplacedPool.Count;

        simulation.Step(
            new TickInput(
                [
                    new Command(
                        CommandKind.Arrive,
                        world.Lots.East[lot],
                        world.Lots.North[lot],
                        new ArrivePayload(Households: 200, LifeStage: 0).Encode()),
                ],
                0));

        Assert.Equal(before + ceiling, world.UnplacedPool.Count);
    }

    /// <summary>
    /// A Tile with no gate on it is refused by name rather than resolved to the nearest one.
    /// </summary>
    /// <remarks>
    /// <b>The edge a Household entered by selects its Hinterland</b> (<c>adr/0088</c>), so a
    /// substituted gate does not misplace an arrival — it changes which market it came from. That is
    /// <c>ApplyTrip</c>'s refusal for <c>ApplyTrip</c>'s reason: ***a substituted endpoint makes a
    /// mistyped command indistinguishable from the one somebody meant.***
    /// </remarks>
    [Fact]
    public void The_arrive_verb_refuses_a_tile_with_no_gate_on_it()
    {
        Ruleset rules = Shipped("bordered.toml");
        InputLogBuilder builder = new(1, new WorldConfiguration(1_000), rulesetHash: 0);
        Simulation simulation = Replay.Start(builder.Build(), rules);

        simulation.Step(new TickInput([new Command(CommandKind.Populate, default, default)], 0));

        Assert.Throws<InvalidOperationException>(
            () => simulation.Step(
                new TickInput(
                    [
                        new Command(
                            CommandKind.Arrive,
                            new Tiles(7_777),
                            new Tiles(7_777),
                            new ArrivePayload(1, 0).Encode()),
                    ],
                    0)));
    }

    /// <summary>
    /// 🔴 <b>A Ruleset reload that un-declares the gate kind is caught, and the write site cannot
    /// see it.</b>
    /// </summary>
    /// <remarks>
    /// ***A guard at the write site checks the kind a Building was born with, and a hot-reloadable
    /// kind is not a property a Building was born with*** — <c>plans/0035</c> <b>F14</b>, one
    /// milestone's task later and on a different column. Removing <c>arrivals_per_day</c> converts
    /// every standing gate back into an ordinary Building with no call made, leaving the Pool's
    /// members waiting at a door that is no longer one.
    /// </remarks>
    [Fact]
    public void A_pool_member_waiting_at_a_kind_that_stopped_being_a_gate_is_caught()
    {
        World world = Bordered();

        Assert.True(
            world.TryArrive(
                world.Buildings.Rows.At(Gates(world)[0]), 0, Ticks.Zero, out _));

        world.Invariants.Collect = true;
        WorldInvariants.ThePoolWaitsAtRealGates(world, world.Invariants);

        Assert.DoesNotContain(
            world.Invariants.Collected,
            violation => violation.Invariant == Invariant.ThePoolsGateIsAnOutsideConnection);

        // The reload is the whole point: it converts a standing gate back into an ordinary Building
        // with no call made, which is the case World.TryArrive's O(1) guard structurally cannot see.
        world.Adopt(Shipped("minimal.toml"), contentHash: 0, Ticks.Zero, Key);

        WorldInvariants.ThePoolWaitsAtRealGates(world, world.Invariants);

        Assert.Contains(
            world.Invariants.Collected,
            violation => violation.Invariant == Invariant.ThePoolsGateIsAnOutsideConnection);
    }
}
