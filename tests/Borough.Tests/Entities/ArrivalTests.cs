using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Invariants;
using Borough.Core.Tables;
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

    /// <summary>
    /// <c>bordered.toml</c> with every <c>[[hinterland]]</c> struck, which no shipped file is.
    /// </summary>
    /// <remarks>
    /// <b>Built by editing the file's text rather than by hand-assembling a <c>Ruleset</c></b>, so
    /// what is under test is a Ruleset the loader actually accepts. A file declaring a gate kind and
    /// no Hinterland is <em>not refusable at load</em> — which edge a gate stands on is a property of
    /// where it was placed, and the loader cannot see a world — so this is exactly the configuration
    /// the arrival-site check exists for.
    /// </remarks>
    private static Ruleset WithGateButNoHinterland()
    {
        string[] lines =
            File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Rulesets", "bordered.toml"));

        var kept = new List<string>();
        bool inHinterland = false;

        foreach (string line in lines)
        {
            if (line.TrimStart().StartsWith('['))
            {
                inHinterland = line.TrimStart().StartsWith("[[hinterland]]", StringComparison.Ordinal);
            }

            if (!inHinterland)
            {
                kept.Add(line);
            }
        }

        RulesetLoadResult result = RulesetLoader.Parse(
            string.Join("\n", kept), "bordered-no-hinterland.toml");

        return result.Ruleset
            ?? throw new InvalidOperationException(
                $"a gate with no Hinterland must load cleanly, and did not:\n{result.Describe()}");
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
                world.Buildings.Rows.At(gate), lifeStage: 0, Ticks.Zero, Key, out var household));

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
                world.Buildings.Rows.At(Gates(world)[0]), lifeStage: 0, Ticks.Zero, Key, out var household));

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
            if (world.TryArrive(world.Buildings.Rows.At(gate), 0, Ticks.Zero, Key, out _))
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
            Assert.True(world.TryArrive(world.Buildings.Rows.At(gate), 0, Ticks.Zero, Key, out _));
        }

        Assert.False(world.TryArrive(world.Buildings.Rows.At(gate), 0, Ticks.Zero, Key, out _));

        Assert.True(
            world.TryArrive(
                world.Buildings.Rows.At(gate), 0, new Ticks(Ticks.PerDay), Key, out _));
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
            Assert.True(world.TryArrive(world.Buildings.Rows.At(gates[0]), 0, Ticks.Zero, Key, out _));
        }

        Assert.False(world.TryArrive(world.Buildings.Rows.At(gates[0]), 0, Ticks.Zero, Key, out _));
        Assert.True(world.TryArrive(world.Buildings.Rows.At(gates[1]), 0, Ticks.Zero, Key, out _));
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

        Assert.False(world.TryArrive(world.Buildings.Rows.At(dwelling), 0, Ticks.Zero, Key, out _));
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

        Assert.True(world.TryArrive(world.Buildings.Rows.At(gates[0]), 0, Ticks.Zero, Key, out var first));
        Assert.True(world.TryArrive(world.Buildings.Rows.At(gates[1]), 0, Ticks.Zero, Key, out var second));

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
    /// 🔴 <b>Money crosses the gate, and the world's supply is no longer a constant.</b>
    /// </summary>
    /// <remarks>
    /// <b><c>MoneySupplyTable.Issued</c>'s second writer, which that column has been predicting since
    /// milestone 10</b> — <em>"milestone 11 gives it its second writer … the gate moves this and the
    /// invariant is unchanged."</em> Until now the supply moved at the founding alone, so
    /// <see cref="Invariant.MoneyIsConserved"/> was an exact equality against a number that never
    /// changed. It is still an exact equality, because <c>Issued</c> is declared as money that has
    /// entered <em>net of anything that has left</em>.
    /// </remarks>
    [Fact]
    public void Money_crosses_the_gate_and_the_supply_moves()
    {
        World world = Bordered();
        long before = world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw;
        int gate = Gates(world)[0];

        Assert.True(world.TryArrive(world.Buildings.Rows.At(gate), 0, Ticks.Zero, Key, out var arrival));

        long after = world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw;

        Assert.True(after > before, "the supply did not move when a Household carried money in.");
        Assert.Equal(after - before, world.BalanceOf(arrival).Raw);
    }

    /// <summary>
    /// <b>What crosses is inside the band the gate's own Hinterland authored.</b>
    /// </summary>
    /// <remarks>
    /// <b>The pairing is the assertion, not the range.</b> The four edges carry deliberately
    /// different bands so that edge selection is not inert — <c>CONTEXT.md</c> → Hinterland's *four
    /// comparable markets are each other's referent* — so an arrival drawing against the wrong
    /// Hinterland is a Household from a market it never came from, and it would look entirely
    /// ordinary.
    /// </remarks>
    [Fact]
    public void An_arrival_carries_what_its_own_hinterland_authored()
    {
        World world = Bordered();
        int checkedGates = 0;

        foreach (int gate in Gates(world))
        {
            MapEdge edge = world.EdgeOf(world.Lots.Rows.Resolve(world.Buildings.Lot[gate]));

            Assert.True(world.Rules.TryHinterland(edge, out HinterlandDefinition hinterland));
            Assert.True(
                world.TryArrive(world.Buildings.Rows.At(gate), 0, Ticks.Zero, Key, out var arrival));

            Money carried = world.BalanceOf(arrival);

            Assert.InRange(
                carried.Raw, hinterland.EmigrantBalanceMin.Raw, hinterland.EmigrantBalanceMax.Raw);

            checkedGates++;
        }

        Assert.Equal(4, checkedGates);
    }

    /// <summary>
    /// <b>Conservation still holds once money has come in from outside.</b>
    /// </summary>
    /// <remarks>
    /// 🔴 ⚠ <b>This milestone's task list said <c>MoneyIsConserved</c> would be *rewritten as supply
    /// plus flow*, and it was not rewritten at all.</b> <c>MoneySupplyTable.Issued</c> is declared as
    /// money that has entered <em>net of anything that has left it</em>, and <c>World.Endow</c>
    /// writes it in the same call that deposits — so an arrival moves both sides together and the
    /// equality is exact without a flow term. ***A term is only owed where the two sides are arrived
    /// at on different schedules.*** The column's own doc-comment said so a milestone in advance.
    /// </remarks>
    [Fact]
    public void Conservation_holds_across_an_arrival()
    {
        World world = Bordered();
        int gate = Gates(world)[0];

        for (int i = 0; i < 5; i++)
        {
            Assert.True(world.TryArrive(world.Buildings.Rows.At(gate), 0, Ticks.Zero, Key, out _));
        }

        world.Invariants.Collect = true;
        WorldInvariants.MoneyIsConserved(world, world.Invariants);

        Assert.DoesNotContain(
            world.Invariants.Collected,
            violation => violation.Invariant == Invariant.MoneyIsConserved);

        MoneyLedger ledger = MoneyLedger.Of(world);

        Assert.Equal(world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw, ledger.Total);
    }

    /// <summary>
    /// Two arrivals through one gate do not carry the same amount, which is what a band is for.
    /// </summary>
    /// <remarks>
    /// <b>A single figure has no distribution</b>, so any instrument reading a spread reads 0% or
    /// 100% and measures nothing — <c>adr/0115</c>'s concern, and the reason
    /// <c>[[hinterland]]</c> authors a band rather than a number. The draw is on the Household's
    /// <b>monotonic id</b>, so two arrivals differ even when they share a slot's history.
    /// </remarks>
    [Fact]
    public void Two_arrivals_through_one_gate_do_not_carry_the_same_amount()
    {
        World world = Bordered();
        int gate = Gates(world)[0];
        var carried = new HashSet<long>();

        for (int i = 0; i < 8; i++)
        {
            Assert.True(
                world.TryArrive(world.Buildings.Rows.At(gate), 0, Ticks.Zero, Key, out var arrival));

            carried.Add(world.BalanceOf(arrival).Raw);
        }

        Assert.True(carried.Count > 1, "every arrival carried the same amount, so the band is inert.");
    }

    /// <summary>
    /// 🔴 <b>A gate whose edge has no Hinterland admits nobody, and says which refusal it is.</b>
    /// </summary>
    /// <remarks>
    /// <b>Admitting them with nothing was the alternative and it is milestone 9's F13.</b> Zero is a
    /// legitimate emigrant balance — a Hinterland whose people arrive penniless is a poor economy,
    /// not an unset field — so a Household admitted through a gate with no economy behind it would be
    /// indistinguishable from one that came from somewhere poor. ***A zero that is a real answer
    /// cannot double as the absence of an answer.***
    /// </remarks>
    [Fact]
    public void A_gate_with_no_hinterland_behind_it_admits_nobody()
    {
        World world = Bordered();
        int gate = Gates(world)[0];
        int before = world.UnplacedPool.Count;

        // minimal.toml declares the same Buildings and no [[hinterland]] at all, so the gate goes on
        // standing where it stands and the Outside behind it stops existing.
        world.Adopt(WithGateButNoHinterland(), contentHash: 0, Ticks.Zero, Key);

        world.Invariants.Collect = true;

        Assert.False(world.TryArrive(world.Buildings.Rows.At(gate), 0, Ticks.Zero, Key, out _));
        Assert.Equal(before, world.UnplacedPool.Count);
        Assert.Contains(
            world.Invariants.Collected,
            violation => violation.Invariant == Invariant.AGateOpensOntoAHinterland);
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
                world.Buildings.Rows.At(Gates(world)[0]), 0, Ticks.Zero, Key, out _));

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
