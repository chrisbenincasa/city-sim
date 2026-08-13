using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Tests.Space;

namespace Borough.Tests.Session;

/// <summary>
/// <c>step(inputs)</c>: the Tick counter, Phase 0's command application, and the guard that keeps
/// Phase 2 honest.
/// </summary>
public sealed class SimulationTests
{
    private const int Population = 64;

    /// <summary>
    /// Lots one block yields on a lattice with all four faces — four faces at two or three each.
    /// </summary>
    /// <remarks>
    /// Arithmetic rather than a constant to keep in step: <c>lots_per_segment</c> is 5, split by
    /// parity between the two blocks sharing a Segment, so a block takes 3 from two of its faces and
    /// 2 from the other two (<c>adr/0078</c>).
    /// </remarks>
    private const int LotsPerBlock = 10;

    [Fact]
    public void A_new_simulation_starts_at_tick_zero() =>
        Assert.Equal(0UL, Build().Tick.Raw);

    [Fact]
    public void Each_step_advances_the_tick_by_exactly_one()
    {
        Simulation simulation = Build();

        for (ulong expected = 1; expected <= 16; expected++)
        {
            simulation.Step(TickInput.Empty);
            Assert.Equal(expected, simulation.Tick.Raw);
        }
    }

    /// <summary>
    /// A Tick with no commands changes nothing but the clock.
    /// </summary>
    /// <remarks>
    /// <b>The clock has to be excluded, and the exclusion is the honest half of the claim.</b> This read
    /// <em>changes nothing</em> and compared <see cref="World.HashState"/> directly, which worked only
    /// because the Tick lived outside the world's state — so the assertion was quietly true of a world
    /// that had, in fact, moved on in time. Now that the Tick is a saved and hashed column the canonical
    /// hash differs on every Tick by construction, and the claim worth making is the one this always
    /// meant: <em>the commands are what change the city.</em>
    /// <para>
    /// It folds every table but the clock rather than asking <c>Core</c> for a second hash, because a
    /// second published fold would be an API a test wanted and a thing to keep in step for ever. The
    /// clock's own movement is asserted too, so the test cannot pass by excluding everything.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_tick_with_no_commands_changes_nothing_but_the_clock()
    {
        Simulation simulation = Build();

        ulong before = HashExceptTheClock(simulation.World);
        Ticks was = simulation.World.Tick;

        simulation.Step(TickInput.Empty);

        Assert.Equal(before, HashExceptTheClock(simulation.World));
        Assert.NotEqual(was, simulation.World.Tick);
        Assert.NotEqual(before, simulation.World.HashState());
    }

    /// <summary>Every table's fold except the clock's, in declaration order.</summary>
    private static ulong HashExceptTheClock(World world)
    {
        ulong hash = 0;

        foreach (Rows table in world.Tables)
        {
            if (ReferenceEquals(table, world.Clock.Rows))
            {
                continue;
            }

            table.Fold(ref hash);
        }

        return hash;
    }

    /// <summary>
    /// The <c>zone</c> verb subdivides the block it names, rather than painting one Lot on it.
    /// </summary>
    /// <remarks>
    /// <b>The verb's meaning changed in 5a-bis and this is where that is asserted</b> —
    /// <c>02 §2.2</c>: <i>Lots are <b>generated, not painted</b></i>. It used to create exactly one
    /// Lot at the command's coordinates, which was honest while there was no Street network to carve
    /// against and became a fiction the moment there was.
    /// <para>
    /// <b>Ten Lots, and the number is arithmetic rather than a magic constant.</b> A block has four
    /// faces; a Segment carries <c>lots_per_segment = 5</c> split between the two blocks that share
    /// it by parity, so each block takes three from one face and two from the next, alternating —
    /// which is odd-and-even house numbering (<c>adr/0078</c>).
    /// </para>
    /// </remarks>
    [Fact]
    public void A_zone_command_subdivides_the_block_it_names()
    {
        Simulation simulation = Zoned();

        simulation.Step(new TickInput([Paint(east: 12, north: 8, permissions: 1)], rulesetHash: 0));

        LotTable lots = simulation.World.Lots;

        Assert.Equal(LotsPerBlock, lots.Rows.LiveCount);

        for (int slot = 0; slot < lots.Rows.SlotCount; slot++)
        {
            Assert.True(lots.Rows.IsLive(slot));
            Assert.True(lots.HasFrontage(slot), $"Lot {slot} was carved with no frontage.");
            Assert.True(lots.AddressOf(slot).Exists);
        }
    }

    /// <summary>
    /// Land with no Street on any face stays unlotted — <c>02 §2.2</c>'s third rule, and the one the
    /// whole slice exists for.
    /// </summary>
    /// <remarks>
    /// <b>This is <c>adr/0025</c>'s rejected road-derived cap, shown working the way that ADR said it
    /// would.</b> The player who zones land the streets do not reach is <em>not refused</em> — the
    /// command applies, nothing throws, and what they get is a dead block interior that explains
    /// itself. `LEGIBLE CAUSE`
    /// </remarks>
    [Fact]
    public void Land_with_no_street_gets_no_lots()
    {
        Simulation simulation = Zoned();
        StreetGrid streets = simulation.World.Roads.Streets;

        // Strip block (1,1) of all four faces, which is what a player bulldozing round a block does.
        foreach ((int column, int row, StreetAxis axis) in ((int, int, StreetAxis)[])[
            (1, 1, StreetAxis.East), (1, 2, StreetAxis.East),
            (1, 1, StreetAxis.North), (2, 1, StreetAxis.North)])
        {
            Assert.True(simulation.World.Roads.BulldozeStreet(column, row, axis));
        }

        int block = streets.BlockTiles;

        simulation.Step(new TickInput(
            [Paint(east: block + (block / 2), north: block + (block / 2), permissions: 1)],
            rulesetHash: 0));

        Assert.Equal(0, simulation.World.Lots.Rows.LiveCount);
    }

    /// <summary>
    /// The same interior fills once a Street is run through it — the other half of the picture.
    /// </summary>
    /// <remarks>
    /// <b>Neither half is testable alone, which is why <c>plans/0022</c> made the subdivider and the
    /// road editor one slice.</b> A subdivider that only ever runs once never exercises its hardest
    /// requirement; a road editor with nothing reading the graph proves only that a row changed.
    /// </remarks>
    [Fact]
    public void A_street_run_through_dead_land_makes_it_developable()
    {
        Simulation simulation = Zoned();

        foreach ((int column, int row, StreetAxis axis) in ((int, int, StreetAxis)[])[
            (1, 1, StreetAxis.East), (1, 2, StreetAxis.East),
            (1, 1, StreetAxis.North), (2, 1, StreetAxis.North)])
        {
            simulation.World.Roads.BulldozeStreet(column, row, axis);
        }

        int block = simulation.World.Roads.Streets.BlockTiles;
        Command zone = Paint(east: block + (block / 2), north: block + (block / 2), permissions: 1);

        simulation.Step(new TickInput([zone], rulesetHash: 0));
        Assert.Equal(0, simulation.World.Lots.Rows.LiveCount);

        // The player runs one Street back along the block's south face. That is one SIDE of one
        // Segment, so it is a quarter of the block's faces and not a quarter of its Lots: a block
        // takes the Left side of its south face, and Left is the even indices {0, 2, 4} of five --
        // three Lots. Its four faces are 3, 2, 2, 3 rather than 2.5 each, because the sides alternate
        // and five is odd. That asymmetry is odd-and-even house numbering and is not an artefact.
        simulation.Step(new TickInput(
            [Connect(east: block, north: block, StreetAxis.East), zone], rulesetHash: 0));

        Assert.Equal(3, simulation.World.Lots.Rows.LiveCount);
    }

    /// <summary>
    /// <b>A Building outlives its frontage: bulldozing its Street leaves it standing with no
    /// Address</b> (<c>adr/0079</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>02 §2.2</c>: <i>"re-subdivision must preserve existing Buildings — only vacant land
    /// re-parcels."</i></b> The rule is keyed on <b>occupancy</b> and not on frontage, and the
    /// difference is the whole test: both Lots below lose their Street in the same edit, and the
    /// occupied one stays while the vacant one goes.
    /// </para>
    /// <para>
    /// <b>The brief proposed letting such a Building decline through <c>adr/0053</c>'s existing
    /// machinery instead, and that is false in the code</b> — <c>ZoneRuleEngine.Condemn</c> is keyed
    /// on a starving Rule Instance, and a bulldozed Street starves nothing. *Citing a mechanism is
    /// not checking what it is keyed on.* What actually happens is 5b's: a Trip to an Address that
    /// does not exist ends <i>no route found</i>, which is why <see cref="Address.None"/> is a value
    /// the Lot can hold rather than a state that has to be refused.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_building_survives_losing_its_street_and_a_vacant_lot_beside_it_does_not()
    {
        Simulation simulation = Zoned();
        World world = simulation.World;
        int block = world.Roads.Streets.BlockTiles;

        simulation.Step(new TickInput(
            [Paint(east: block + (block / 2), north: block + (block / 2), permissions: 1)],
            rulesetHash: 0));

        // The south face of block (1,1) -- three Lots, on the Segment the edit below removes.
        // Named through the lattice rather than through Frontage.Locate, because the intersection
        // itself fronts nothing by design and (block, block) is an intersection.
        int occupied = OnFace(world, world.Roads.Streets.Horizontal(1, 1));

        Handle<Building> building = world.Buildings.Create(world.Lots, world.Lots.Rows.At(occupied), kind: 1);
        world.Lots.Occupy(occupied, world.Buildings.Rows.Resolve(building));

        int before = world.Lots.Rows.LiveCount;

        simulation.Step(new TickInput(
            [Connect(east: block, north: block, StreetAxis.East, ConnectAction.Bulldoze)], rulesetHash: 0));

        Assert.True(world.Lots.Rows.IsLive(occupied), "an occupied Lot was freed by re-subdivision");
        Assert.False(world.Lots.HasFrontage(occupied), "the Lot kept a Street that was bulldozed");
        Assert.False(world.Lots.AddressOf(occupied).Exists);
        Assert.True(world.Buildings.Rows.IsLive(world.Buildings.Rows.Resolve(building)));

        // Two of the face's three Lots were vacant, and they are land again.
        Assert.Equal(before - 2, world.Lots.Rows.LiveCount);
    }

    /// <summary>The slot of the first live Lot fronting <paramref name="segment"/>.</summary>
    private static int OnFace(World world, int segment)
    {
        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (world.Lots.Rows.IsLive(slot) && world.Lots.FrontageSlot[slot] == segment + 1)
            {
                return slot;
            }
        }

        throw new InvalidOperationException($"no Lot fronts Segment {segment}");
    }

    /// <summary>
    /// The permission set survives application at full width.
    /// </summary>
    /// <remarks>
    /// <b>The assertion nobody wrote, which is why the narrowing lived for five slices.</b>
    /// <c>A_zone_command_paints_a_lot</c> above checks that a Lot appears where the command said, and
    /// checks nothing about what was painted on it — so slice 5 casting the authored set down to a
    /// byte on the way in passed every test in this file. The bits chosen here are the ones that cast
    /// destroyed: bit 15 and bit 8 are both above a byte, and a Lot that came back holding
    /// <c>0b0000_0101</c> would be the old behaviour reporting success.
    /// <para>
    /// <c>CONTEXT</c> → Zone is a permission set and mixed use is a set with more than one entry, so
    /// four bits are set rather than one — a single bit could not tell a set apart from an enum that
    /// happens to fit.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_zone_command_paints_its_whole_permission_set()
    {
        Simulation simulation = Zoned();
        const ushort MixedUse = 0b1000_0001_0000_0101;

        simulation.Step(new TickInput(
            [Paint(east: 3, north: 4, permissions: MixedUse)], rulesetHash: 0));

        Assert.Equal(MixedUse, simulation.World.Lots.Zone[0]);
    }

    /// <summary>
    /// Two commands in one Tick land in the order they were issued.
    /// </summary>
    /// <remarks>
    /// <b>Asserted through the permission set rather than through position</b>, because a block's Lots
    /// are no longer at the coordinates the command named — the subdivider puts them on the block's
    /// faces. The set is the one payload that travels from the command onto every Lot it produces, so
    /// it is what distinguishes *which command made this row* now that one command makes many.
    /// </remarks>
    [Fact]
    public void Commands_in_one_tick_are_applied_in_issue_order()
    {
        Simulation simulation = Zoned();
        int block = simulation.World.Roads.Streets.BlockTiles;

        simulation.Step(new TickInput(
            [
                Paint(east: block / 2, north: block / 2, permissions: 0b0001),
                Paint(east: block + (block / 2), north: block / 2, permissions: 0b0010),
            ],
            rulesetHash: 0));

        Assert.Equal(0b0001, simulation.World.Lots.Zone[0]);
        Assert.Equal(0b0010, simulation.World.Lots.Zone[LotsPerBlock]);
    }

    /// <summary>
    /// A verb the slice has not built throws rather than being skipped: a run that silently drops a
    /// command diverges from the log describing it while reporting success.
    /// </summary>
    [Theory]
    [InlineData(CommandKind.None)]
    [InlineData(CommandKind.Service)]
    [InlineData(CommandKind.Govern)]
    public void An_unapplied_verb_throws_rather_than_being_skipped(CommandKind kind)
    {
        Simulation simulation = Build();
        var command = new Command(kind, new Tiles(0), new Tiles(0));

        Assert.Throws<InvalidOperationException>(() =>
            simulation.Step(new TickInput([command], rulesetHash: 0)));
    }

    /// <summary>
    /// <b>Why the Decide guard folds storage rather than the State Hash.</b> A write to a derived
    /// column is invisible to the State Hash <em>by declaration</em> — that is what
    /// <c>(derived AND rebuilt)</c> means — so a read-only check built on the State Hash would wave
    /// through exactly the write a Rule evaluating in Decide is most likely to make: caching something.
    /// This is slice 4's blind spot read from the other side.
    /// </summary>
    [Fact]
    public void The_decide_guard_sees_a_write_the_state_hash_cannot()
    {
        World world = Populate();

        ulong hashBefore = world.HashState();
        ulong storageBefore = FoldEverything(world);

        // A derived column: the intrusive link from a Citizen to the next member of its Household.
        world.Citizens.MemberNext[0] += 1;

        Assert.Equal(hashBefore, world.HashState());
        Assert.NotEqual(storageBefore, FoldEverything(world));
    }

    [Fact]
    public void The_decide_guard_is_on_by_default() =>
        Assert.True(Build().VerifyDecideWritesNothing);

    /// <summary>
    /// The guard is a runtime switch rather than a build configuration, so that a release long-run can
    /// turn it off for speed and a release correctness run can leave it on. <c>02 §10</c>'s rule is
    /// that invariants sort by frequency, never by build.
    /// </summary>
    [Fact]
    public void The_decide_guard_can_be_turned_off_without_changing_the_result()
    {
        Simulation guarded = Build();
        Simulation unguarded = Build();
        unguarded.VerifyDecideWritesNothing = false;

        for (int i = 0; i < 8; i++)
        {
            guarded.Step(TickInput.Empty);
            unguarded.Step(TickInput.Empty);
        }

        Assert.Equal(guarded.World.HashState(), unguarded.World.HashState());
    }

    [Fact]
    public void The_phase_after_a_step_is_the_last_one()
    {
        Simulation simulation = Build();
        simulation.Step(TickInput.Empty);

        Assert.Equal(TickPhase.Commit, simulation.Phase);
    }

    private static Command Paint(int east, int north, ushort permissions) =>
        new(CommandKind.Zone, new Tiles(east), new Tiles(north), permissions);

    private static Command Connect(
        int east, int north, StreetAxis axis, ConnectAction action = ConnectAction.Lay) =>
        new(
            CommandKind.Connect,
            new Tiles(east),
            new Tiles(north),
            new ConnectPayload(axis, action, RoadKind.Street).Encode());

    /// <summary>
    /// A Simulation over a laid Street lattice, which is what the <c>zone</c> verb now needs.
    /// </summary>
    /// <remarks>
    /// <b>The roads are laid directly rather than through <c>Populate</c></b>, because these tests are
    /// about the verb rather than about the populator and a synthetic city at this size would be most
    /// of their run time. The generator is the same one <c>Populate</c> calls.
    /// </remarks>
    private static Simulation Zoned()
    {
        var key = WorldKey.FromSeed(0xD0D0_CACA_0000_0001UL);
        World world = new(Population, RoadFixtures.With(RoadFixtures.Lattice()));

        RoadGenerator.LayInto(world.Roads, key, CellGrid.WorldTiles);

        return new Simulation(world, key);
    }

    private static Simulation Build() =>
        new(new World(Population), WorldKey.FromSeed(0xD0D0_CACA_0000_0001UL));

    private static ulong FoldEverything(World world)
    {
        ulong hash = 0;

        foreach (Rows table in world.Tables)
        {
            table.FoldAll(ref hash);
        }

        return hash;
    }

    /// <summary>A world with one of everything, so that every table has a row to disturb.</summary>
    private static World Populate()
    {
        var world = new World(Population);

        Handle<Lot> lot = world.Lots.Create(new Tiles(0), new Tiles(0), zone: 1);
        Handle<Building> building = world.Buildings.Create(world.Lots, lot, kind: 1);
        Handle<Household> household = world.CreateHousehold(building, lifeStage: 1);

        world.CreateCitizen(household, new Ticks(4));
        world.CreateCitizen(household, new Ticks(9));

        return world;
    }
}
