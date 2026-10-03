using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Tests.Persistence;

namespace Borough.Tests.Space;

/// <summary>
/// A Lot's frontage is saved state: a handle to the Segment it fronts and an offset along it
/// (<c>adr/0174</c>).
/// </summary>
/// <remarks>
/// <b>What these three cover is the consequence rather than the column.</b> Frontage has two homes
/// now, the Lot and the Segment, so a Street edit that does not migrate the Lots it touches leaves
/// them wrong — and nothing rebuilds the contact from the lattice afterwards to cover the mistake up.
/// Bulldoze, re-lay and split are the three edits that have to migrate, and a reload is the fourth
/// thing that has to produce the frontage the world was saved with.
/// </remarks>
public sealed class SavedFrontageTests
{
    private const int Population = 64;

    /// <summary>
    /// The frontage in the file is the frontage that comes back, and the lattice is not consulted.
    /// </summary>
    /// <remarks>
    /// <b>The offset is moved off the carve before the save, which is what makes the claim testable
    /// at all.</b> A Lot carrying the offset <c>Frontage.Locate</c> would compute cannot tell a
    /// restored column from a re-derived one — the two agree by construction — so this one is put
    /// somewhere the lattice would never put it, and a load that still derived frontage would hand
    /// back the carve's number instead.
    /// </remarks>
    [Fact]
    public void A_lots_frontage_comes_back_from_the_save_and_not_from_the_lattice()
    {
        Simulation simulation = Zoned();
        World world = simulation.World;

        Carve(simulation);

        int segment = world.Roads.Streets.Horizontal(1, 1);
        int moved = Fronting(world, segment)[0];
        var unusual = new Tiles(world.Lots.FrontageOffset[moved].Raw + 1);

        world.Lots.Front(moved, world.Roads.Segments.Rows.At(segment), unusual);
        Assert.NotEqual(unusual, Offset(world, moved));

        var file = new MemorySave();
        SaveFile.Write(world, 0xF0A7, file);
        World restored = SaveFile.Read(file, world.Rules, out _);

        Assert.Equal(Addresses(world), Addresses(restored));
        Assert.Equal(unusual, restored.Lots.FrontageOffset[moved]);
        Assert.Equal(segment, restored.Lots.FrontageOn(moved));
        Assert.Equal(world.HashState(), restored.HashState());
    }

    /// <summary>
    /// Bulldozing a Street unfronts the Lots on it; laying it back gives their frontage back.
    /// </summary>
    /// <remarks>
    /// <b>The occupied Lot is the one worth following</b> (<c>adr/0079</c>): a vacant Lot with no
    /// Street is re-subdivided away, so only a standing one is still there to be fronted again. ⚠
    /// <b>The row is cleared rather than left pointing at a freed Segment</b> — a severed handle
    /// already reads as no frontage, and the state the city is in is <em>this Lot fronts nothing</em>.
    /// </remarks>
    [Fact]
    public void A_bulldozed_street_unfronts_its_lots_and_laying_it_back_fronts_them_again()
    {
        Simulation simulation = Zoned();
        World world = simulation.World;
        int block = world.Roads.Streets.BlockTiles;

        Carve(simulation);

        int segment = world.Roads.Streets.Horizontal(1, 1);
        int standing = Fronting(world, segment)[0];
        Tiles offset = world.Lots.FrontageOffset[standing];

        Handle<Building> building =
            world.Buildings.Create(world.Lots, world.Lots.Rows.At(standing), kind: 1);

        world.Lots.Occupy(standing, world.Buildings.Rows.Resolve(building));

        simulation.Step(new TickInput(
            [Connect(block, block, StreetAxis.East, ConnectAction.Bulldoze)], rulesetHash: 0));

        Assert.True(world.Lots.Rows.IsLive(standing), "an occupied Lot was freed by re-subdivision");
        Assert.False(world.Lots.HasFrontage(standing));
        Assert.True(
            world.Lots.FrontageSegment[standing].IsNone,
            "the Lot kept a handle to a bulldozed Segment");
        Assert.Equal(Tiles.Zero, world.Lots.FrontageOffset[standing]);

        simulation.Step(new TickInput([Connect(block, block, StreetAxis.East)], rulesetHash: 0));

        Assert.True(world.Lots.HasFrontage(standing));
        Assert.Equal(world.Roads.Streets.Horizontal(1, 1), world.Lots.FrontageOn(standing));
        Assert.Equal(offset, world.Lots.FrontageOffset[standing]);
    }

    /// <summary>
    /// A Street edit elsewhere does not front a Lot standing on ground the edit never named.
    /// </summary>
    /// <remarks>
    /// <b>The Lot is made by hand and never fronted</b>, which is the one way to stand unfronted on a
    /// lattice edge that still has a Street on it — <see cref="LotTable.Create"/> leaves frontage to
    /// its caller, and no carve skips it. ⚠ <b>The pass this replaced re-derived frontage for every
    /// Lot on every road edit and every rebuild</b>, so an edit anywhere used to front this Lot.
    /// Both halves of a connect command are covered: the bulldoze creates nothing to attach, and the
    /// re-lay creates a Segment on an edge this Lot does not sit on.
    /// </remarks>
    [Fact]
    public void A_street_edit_elsewhere_does_not_front_a_lot_that_was_never_fronted()
    {
        Simulation simulation = Zoned();
        World world = simulation.World;
        int block = world.Roads.Streets.BlockTiles;

        // On the Segment at lattice (1, 1), off the intersection, so the lattice has a Street here.
        Handle<Lot> lot = world.Lots.Create(
            new Tiles(block + 6), new Tiles(block), LotTable.Housing, StreetSide.Left);

        int row = world.Lots.Rows.Resolve(lot);
        Handle<Building> building = world.Buildings.Create(world.Lots, lot, kind: 1);

        world.Lots.Occupy(row, world.Buildings.Rows.Resolve(building));

        Assert.False(world.Lots.HasFrontage(row), "the fixture fronted the Lot before the edit");
        Assert.NotEqual(Rows.NoSlot, Frontage.Locate(
            world.Roads.Streets, world.Lots.East[row], world.Lots.North[row], out _));

        int elsewhere = 2 * block;

        simulation.Step(new TickInput(
            [Connect(elsewhere, elsewhere, StreetAxis.East, ConnectAction.Bulldoze)], rulesetHash: 0));

        Assert.False(world.Lots.HasFrontage(row), "a bulldoze elsewhere fronted an unfronted Lot");

        simulation.Step(new TickInput(
            [Connect(elsewhere, elsewhere, StreetAxis.East)], rulesetHash: 0));

        Assert.False(world.Lots.HasFrontage(row), "a lay elsewhere fronted an unfronted Lot");
        Assert.True(world.Lots.Rows.IsLive(row));
    }

    /// <summary>
    /// A Segment split moves the Lots past the cut to the new Segment, at their offsets minus the
    /// retained length.
    /// </summary>
    /// <remarks>
    /// <b>The cut is taken at the first Lot's own offset</b>, so the test covers the boundary as well
    /// as the migration: the rule is <em>beyond</em> the retained length and not <em>at</em> it, which
    /// is what keeps a Lot standing exactly where the A part ends on the Segment that still holds its
    /// ground. The side never moves, because both parts run in the original's A→B direction.
    /// </remarks>
    [Fact]
    public void A_segment_split_moves_the_lots_beyond_the_cut_and_leaves_the_rest()
    {
        Simulation simulation = Zoned();
        World world = simulation.World;
        int block = world.Roads.Streets.BlockTiles;

        Carve(simulation);

        int original = world.Roads.Streets.Horizontal(1, 1);
        List<int> on = Fronting(world, original);
        Assert.True(on.Count >= 2, "the fixture carved too few Lots on one face to split it");

        Tiles cut = world.Lots.FrontageOffset[on[0]];
        Tiles[] offsets = [.. on.Select(slot => world.Lots.FrontageOffset[slot])];
        byte[] sides = [.. on.Select(slot => world.Lots.Side[slot])];
        List<(ulong Id, Address Where)> elsewhere = Addresses(world, skip: on);

        Handle<RoadNode> cutNode =
            world.Roads.Nodes.Create(new Tiles(block + cut.Raw), new Tiles(block));

        Handle<RoadSegment> created = world.Roads.Segments.Create(
            cutNode,
            world.Roads.Segments.NodeB[original],
            new Tiles(block - cut.Raw),
            RoadKind.Street,
            TravelMode.Any,
            TravelMode.Any);

        int createdSlot = world.Roads.Segments.Rows.Resolve(created);
        int moved = Frontage.Split(world.Lots, world.Roads.Segments, original, createdSlot, cut);

        Assert.Equal(on.Count - 1, moved);
        Assert.Equal(original, world.Lots.FrontageOn(on[0]));
        Assert.Equal(cut, world.Lots.FrontageOffset[on[0]]);

        for (int i = 1; i < on.Count; i++)
        {
            Assert.Equal(createdSlot, world.Lots.FrontageOn(on[i]));
            Assert.Equal(new Tiles(offsets[i].Raw - cut.Raw), world.Lots.FrontageOffset[on[i]]);
            Assert.Equal(sides[i], world.Lots.Side[on[i]]);
        }

        Assert.Equal(elsewhere, Addresses(world, skip: on));
    }

    /// <summary>Paints one block, which is what carves Lots along its four faces.</summary>
    private static void Carve(Simulation simulation)
    {
        int block = simulation.World.Roads.Streets.BlockTiles;
        int middle = block + (block / 2);

        simulation.Step(new TickInput(
            [new Command(CommandKind.Zone, new Tiles(middle), new Tiles(middle), LotTable.Housing)],
            rulesetHash: 0));

        Assert.True(simulation.World.Lots.Rows.LiveCount > 0, "the fixture carved no Lots");
    }

    /// <summary>The live Lots fronting one Segment, in offset order.</summary>
    private static List<int> Fronting(World world, int segment)
    {
        List<int> found = [];

        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (world.Lots.Rows.IsLive(slot) && world.Lots.FrontageOn(slot) == segment)
            {
                found.Add(slot);
            }
        }

        found.Sort((one, other) =>
            world.Lots.FrontageOffset[one].Raw.CompareTo(world.Lots.FrontageOffset[other].Raw));

        Assert.NotEmpty(found);

        return found;
    }

    /// <summary>Every live Lot's Address against its row id, which survives a reload.</summary>
    private static List<(ulong Id, Address Where)> Addresses(World world, List<int>? skip = null)
    {
        List<(ulong Id, Address Where)> found = [];

        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (!world.Lots.Rows.IsLive(slot) || skip?.Contains(slot) == true)
            {
                continue;
            }

            found.Add((world.Lots.Rows.IdAt(slot), world.Lots.AddressOf(slot)));
        }

        return found;
    }

    /// <summary>What the lattice would give this Lot, which is the carve's own answer.</summary>
    private static Tiles Offset(World world, int slot)
    {
        Frontage.Locate(world.Roads.Streets, world.Lots.East[slot], world.Lots.North[slot], out Tiles offset);

        return offset;
    }

    private static Command Connect(
        int east, int north, StreetAxis axis, ConnectAction action = ConnectAction.Lay) =>
        new(
            CommandKind.Connect,
            new Tiles(east),
            new Tiles(north),
            new ConnectPayload(axis, action, RoadKind.Street).Encode());

    /// <summary>A Simulation over a laid Street lattice, which is what the <c>zone</c> verb needs.</summary>
    private static Simulation Zoned()
    {
        var key = WorldKey.FromSeed(0x0174_0174_0000_0003UL);
        World world = new(Population, RoadFixtures.With(RoadFixtures.Lattice()));

        RoadGenerator.LayInto(world.Roads, key, CellGrid.WorldTiles);

        return new Simulation(world, key);
    }
}
