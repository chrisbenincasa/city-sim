using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Tests.Space;

namespace Borough.Tests.Movement;

/// <summary>
/// A Building's Access Point: <b>where somebody arriving on foot or by Vehicle actually arrives</b>.
/// </summary>
/// <remarks>
/// <b>There is no column under any of this, and that is the claim these tests exist to hold.</b>
/// <c>adr/0078</c> makes frontage <c>(derived AND rebuilt)</c> on the Segment's Epoch, so a Building's
/// front door is read through the Lot it stands on rather than copied onto the Building when it is
/// raised. The failure a copy would produce is the quiet one — a road edit gives the Lot a new Address
/// and the Building keeps the old one, with nothing invalidated and nothing to notice — which is why
/// <see cref="A_bulldozed_street_takes_the_buildings_access_point_with_it"/> asserts the change
/// happens with no code having touched <see cref="BuildingTable"/> at all.
/// </remarks>
public sealed class AccessPointTests
{
    private const int Population = 64;

    /// <summary>
    /// A Building's Access Point is its Lot's Address, and not a second answer that agrees with it.
    /// </summary>
    [Fact]
    public void A_buildings_access_point_is_the_address_of_the_lot_it_stands_on()
    {
        (Simulation simulation, int lot) = Standing();
        World world = simulation.World;
        int building = world.Lots.BuildingOn(lot);

        Address expected = world.Lots.AddressOf(lot);

        Assert.True(expected.Exists);
        Assert.Equal(expected, world.PedestrianAccessPoint(building));
    }

    /// <summary>
    /// The pedestrian and the vehicle Access Point are equal, and today that is built behaviour rather
    /// than an interim simplification.
    /// </summary>
    /// <remarks>
    /// <b><c>adr/0074</c> gives a Building two Access Points; <see cref="LotSubdivider"/> derives
    /// one.</b> A second would need a second <em>saved</em> fact — the Lot's position and side is what
    /// is stored — and inventing one for a consumer that does not exist is the position
    /// <c>adr/0070</c> forbids. The two are kept as two accessors rather than collapsed into one so
    /// that whatever separates them is a change at one call site each. ⚠ <b>Parking was the named
    /// candidate and it did not separate them</b> — milestone <b>7</b>, which this sentence called 8
    /// before the renumber, gave a car somewhere else to be by having a Trip's flanking Legs name a
    /// <b>Car Park's Address</b> rather than by giving a Building a second door. <c>03 §6.6</c>'s
    /// freight is the remaining candidate.
    /// <para>
    /// <b>This test is expected to be deleted rather than to fail.</b> The day something makes them
    /// diverge it stops describing the city, and a test asserting equality that somebody weakens to
    /// keep it green is worse than no test.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_two_access_points_are_equal_by_construction()
    {
        (Simulation simulation, int lot) = Standing();
        World world = simulation.World;
        int building = world.Lots.BuildingOn(lot);

        Assert.Equal(world.PedestrianAccessPoint(building), world.VehicleAccessPoint(building));
    }

    /// <summary>
    /// <b>Bulldozing a Building's Street leaves it standing with no Access Point</b> — the state
    /// <c>adr/0079</c> names, reached through the ordinary verb rather than by constructing it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Nothing here writes to <see cref="BuildingTable"/>, and that is the assertion.</b> The
    /// <c>Connect</c> command re-subdivides the Lots on the face it touched; the Building is untouched
    /// by construction, because <c>02 §2.2</c> keys re-subdivision on <em>occupancy</em>. If the
    /// Access Point were a column it would still hold the Segment that has just been removed, and the
    /// three assertions below would pass while the Building answered with a road that is gone.
    /// </para>
    /// <para>
    /// <b><see cref="Address.None"/> is an answer, not an error.</b> A Trip to it ends <i>no route
    /// found</i> (<c>adr/0079</c>), which is why the Building stays live and the read does not throw.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_bulldozed_street_takes_the_buildings_access_point_with_it()
    {
        (Simulation simulation, int lot) = Standing();
        World world = simulation.World;
        int building = world.Lots.BuildingOn(lot);
        int block = world.Roads.Streets.BlockTiles;

        Assert.True(world.PedestrianAccessPoint(building).Exists);

        simulation.Step(new TickInput(
            [Connect(east: block, north: block, StreetAxis.East, ConnectAction.Bulldoze)],
            rulesetHash: 0));

        Assert.True(world.Buildings.Rows.IsLive(building), "the Building was demolished by a road edit");
        Assert.False(world.PedestrianAccessPoint(building).Exists);
        Assert.Equal(Address.None, world.VehicleAccessPoint(building));
    }

    /// <summary>
    /// A world with a Street lattice, one zoned block, and a Building standing on a Lot of that
    /// block's south face — the face <see cref="Connect"/> can remove in one edit.
    /// </summary>
    private static (Simulation Simulation, int Lot) Standing()
    {
        var key = WorldKey.FromSeed(0xD0D0_CACA_0000_0001UL);
        World world = new(Population, RoadFixtures.With(RoadFixtures.Lattice()));

        RoadGenerator.LayInto(world.Roads, key, CellGrid.WorldTiles);

        var simulation = new Simulation(world, key);
        int block = world.Roads.Streets.BlockTiles;

        simulation.Step(new TickInput(
            [Paint(east: block + (block / 2), north: block + (block / 2), permissions: 1)],
            rulesetHash: 0));

        // Named through the lattice rather than through Frontage.Locate: (block, block) is an
        // intersection and an intersection fronts nothing by design.
        int lot = OnFace(world, world.Roads.Streets.Horizontal(1, 1));

        Handle<Building> building =
            world.Buildings.Create(world.Lots, world.Lots.Rows.At(lot), kind: 1);
        world.Lots.Occupy(lot, world.Buildings.Rows.Resolve(building));

        return (simulation, lot);
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

    private static Command Paint(int east, int north, ushort permissions) =>
        new(CommandKind.Zone, new Tiles(east), new Tiles(north), permissions);

    private static Command Connect(
        int east, int north, StreetAxis axis, ConnectAction action = ConnectAction.Lay) =>
        new(
            CommandKind.Connect,
            new Tiles(east),
            new Tiles(north),
            new ConnectPayload(axis, action, RoadKind.Street).Encode());
}
