using Borough.Core.Determinism;
using System.Globalization;
using Borough.Core.Entities;
using Borough.Core.Parking;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Parking;

/// <summary>
/// Milestone 7 task 1: the Car Park — <c>adr/0009</c>'s supply, <c>adr/0120</c>'s table.
/// </summary>
/// <remarks>
/// <para>
/// <b>The two claims under test are the two the decision turns on.</b> A Car Park's <b>capacity</b> is
/// a property of the <em>Ruleset in force</em>, so a retuned <c>[[building]] parking</c> reaches every
/// Building already standing — <c>adr/0068</c>'s rule on a sixth axis. Its <b>occupancy</b> is saved
/// state that nothing recomputes, because <c>adr/0084</c> calls a leak here an <c>adr/0006</c>-class
/// <em>permanent</em> capacity loss, and a reload that re-derived it would launder exactly the defect
/// the invariants exist to catch.
/// </para>
/// <para>
/// <b>Nothing here acquires or releases a space, and that is task 4's rather than an omission.</b>
/// These tests reach <see cref="CarParkTable.Occupied"/> through
/// <see cref="CarParkTable.Move(int, int)"/> directly, which is the one thing a production caller may
/// never do — <c>World</c> pairs every move with the holder's column. What is testable today is that
/// the supply is <em>created</em>, <em>ceilinged</em>, <em>located</em> and <em>freed</em> correctly,
/// and the last of those is the one with a demolition under it.
/// </para>
/// <para>
/// ⚠ <b>The over-full case is asserted and not resolved.</b> A lowered provision leaves
/// <see cref="CarParkTable.SpaceAt"/> negative until the dismissal writes to the holders, and the
/// dismissal is task 4's because it cannot be written without the acquire it has to stay paired with.
/// The test below therefore records a state the build passes through rather than one it rests in.
/// </para>
/// </remarks>
public sealed class CarParkTests
{
    private const ulong HashA = 0x1111_1111_1111_1111UL;
    private const ulong HashB = 0x2222_2222_2222_2222UL;

    private const byte Dwelling = 1;
    private const byte Depot = 2;

    private static readonly WorldKey Key = WorldKey.FromSeed(0x7000_0001UL);

    /// <summary>
    /// A kind that parks and a kind that does not, with the provision left as a token.
    /// </summary>
    /// <remarks>
    /// <b>Two kinds rather than one, because the plus-one encoding is only falsifiable against a
    /// second Building.</b> A world in which everything parks cannot tell <em>owns Car Park slot
    /// 0</em> from <em>owns none</em>, which is the failure
    /// <see cref="BuildingTable.CarPark"/> is encoded against.
    /// </remarks>
    private const string Template = """
        [[resource]]
        name = "sundries"
        family = "good"

        [[building]]
        name = "dwelling"
        occupants = 3
        parking = PARKS
        bins = [
            { resource = "sundries", capacity = 12 },
        ]

        [[building]]
        name = "depot"
        occupants = 1
        """;

    /// <summary>The same file with the <c>parking</c> key removed rather than zeroed.</summary>
    private const string Unstated = """
        [[resource]]
        name = "sundries"
        family = "good"

        [[building]]
        name = "dwelling"
        occupants = 3
        bins = [
            { resource = "sundries", capacity = 12 },
        ]

        [[building]]
        name = "depot"
        occupants = 1
        """;

    /// <summary>The same file with no <c>[[building]]</c> at all: every Building is derelict.</summary>
    private const string NoKinds = """
        [[resource]]
        name = "sundries"
        family = "good"
        """;

    /// <summary>A dwelling parking <paramref name="spaces"/> Vehicles.</summary>
    private static string Parking(int spaces) => Template.Replace(
        "PARKS",
        spaces.ToString(CultureInfo.InvariantCulture),
        StringComparison.Ordinal);

    /// <summary>
    /// <c>rulesets/minimal.toml</c>'s <c>[roads]</c> values, for the two tests that need a Street.
    /// </summary>
    /// <remarks>
    /// <b>Present for its speeds rather than for its geometry.</b> Nothing here runs the generator —
    /// the Segment below is built by hand — but a Ruleset stating no <c>[roads]</c> leaves every
    /// speed at zero, and <c>RoadGraph.RebuildDerived</c> divides by one to price an Arc.
    /// </remarks>
    private const string Streets = """
        [roads]
        block_tiles = 32
        arterial_count = 0
        arterial_junction_tiles = 512
        foot_crossing_every = 4
        foot_paths_per_thousand_blocks = 40
        street_speed_kph = 50
        arterial_speed_kph = 90
        walk_speed_kph = 5
        street_capacity_per_hour = 3600
        arterial_capacity_per_hour = 12000
        foot_path_capacity_per_hour = 1000
        """;

    /// <summary>A dwelling parking <paramref name="spaces"/> Vehicles, in a world with Streets.</summary>
    private static string ParkingOnAStreet(int spaces) => Parking(spaces) + "\n\n" + Streets;

    private static Ruleset Load(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    /// <summary>One dwelling standing under a Ruleset that parks <paramref name="spaces"/>.</summary>
    private static World City(int spaces) => Housing(Parking(spaces));

    /// <summary>One dwelling standing under <paramref name="toml"/>.</summary>
    private static World Housing(string toml)
    {
        var world = new World(1_000, Load(toml));

        Handle<Lot> lot = world.Lots.Create(new Tiles(0), new Tiles(0), zone: 1);
        world.CreateBuilding(lot, Dwelling, Ticks.Zero, Key);

        return world;
    }

    private static int OnlyCarPark(World world)
    {
        Assert.Equal(1, world.CarParks.Rows.LiveCount);

        return world.Buildings.CarParkOf(0);
    }

    /// <summary>Gives <paramref name="lotSlot"/> a Street to front, and returns its Segment.</summary>
    /// <remarks>
    /// Two nodes and one Segment, built by hand rather than generated, for
    /// <c>RoadFixtures.Chain</c>'s reason: the answer is known in advance, so the Address asserted
    /// below is an exact one rather than a number compared against itself.
    /// </remarks>
    private static Handle<RoadSegment> GiveFrontage(World world, int lotSlot)
    {
        Handle<RoadNode> a = world.Roads.Nodes.Create(Tiles.Zero, Tiles.Zero);
        Handle<RoadNode> b = world.Roads.Nodes.Create(new Tiles(32), Tiles.Zero);

        Handle<RoadSegment> segment = world.Roads.Segments.Create(
            a, b, new Tiles(32), RoadKind.Street, TravelMode.Any, TravelMode.Any);

        world.Roads.RebuildDerived();

        world.Lots.FrontageSlot[lotSlot] = world.Roads.Segments.Rows.Resolve(segment) + 1;
        world.Lots.FrontageOffset[lotSlot] = new Tiles(16);
        world.Lots.Side[lotSlot] = (byte)StreetSide.Right;

        return segment;
    }

    // ---- the provision ---------------------------------------------------------------------------

    /// <summary>
    /// <b>A Building of a kind that declares parking is raised with a Car Park at its kind's
    /// capacity, empty.</b>
    /// </summary>
    /// <remarks>
    /// The acceptance test for the task, and it runs through <see cref="World.CreateBuilding"/>
    /// rather than through <see cref="CarParkTable.Create"/> — because <c>CONTEXT.md</c> → Bin's
    /// <em>"a Building is given exactly its kind's Bins when it is built"</em> is the sentence being
    /// extended, and a test that called the allocator by hand would pass with the door unwired.
    /// </remarks>
    [Fact]
    public void A_building_that_declares_parking_is_raised_with_a_car_park()
    {
        World world = City(spaces: 6);
        int carPark = OnlyCarPark(world);

        Assert.True(world.Buildings.HasCarPark(0));
        Assert.Equal(6, world.CarParks.Capacity[carPark]);
        Assert.Equal(0, world.CarParks.Occupied[carPark]);
        Assert.Equal(6, world.CarParks.SpaceAt(carPark));
    }

    /// <summary>The Car Park names its Building, and the Building names it back.</summary>
    [Fact]
    public void The_car_park_and_its_building_name_each_other()
    {
        World world = City(spaces: 6);
        int carPark = OnlyCarPark(world);

        Assert.True(world.Buildings.Rows.TryResolve(world.CarParks.Owner[carPark], out int owner));
        Assert.Equal(0, owner);
        Assert.Equal(carPark, world.Buildings.CarParkOf(0));
    }

    /// <summary>
    /// <b>A kind that states no parking gets no row at all, and that is not the same as a row of
    /// capacity zero.</b>
    /// </summary>
    /// <remarks>
    /// The Parking Shed walks rows, so an empty row would be a Car Park that is <em>permanently
    /// full</em> where a Building that provides none has nothing to find — and the two read
    /// differently in every diagnosis <c>adr/0009</c> exists to give. Asserted for both spellings,
    /// because <em>absent</em> and <em>declared zero</em> mean the same thing here and that is itself
    /// a decision (<c>Ruleset.KindDefinition.Parking</c>): a kind saying nothing about parking
    /// provides none, where <c>occupants</c> and <c>jobs</c> have to keep the two apart.
    /// </remarks>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_kind_that_parks_nothing_gets_no_row(bool stated)
    {
        World world = stated ? City(spaces: 0) : Housing(Unstated);

        Assert.Equal(0, world.CarParks.Rows.LiveCount);
        Assert.False(world.Buildings.HasCarPark(0));
    }

    /// <summary>
    /// <b>A Building with no Car Park does not read as owning Car Park slot 0.</b>
    /// </summary>
    /// <remarks>
    /// <b>The plus-one encoding, tested against the world that falsifies it.</b> A freed or freshly
    /// allocated row is zero-filled, so a <see cref="Rows.NoSlot"/> sentinel would make every
    /// Building with no parking claim the first real Car Park in the city — one Car Park owned by
    /// everybody, with every hash moving and every test passing. The depot is raised
    /// <em>after</em> the dwelling precisely so that slot 0 is occupied when the question is asked.
    /// </remarks>
    [Fact]
    public void A_building_with_no_car_park_does_not_claim_slot_zero()
    {
        World world = City(spaces: 6);

        Handle<Lot> lot = world.Lots.Create(new Tiles(64), new Tiles(0), zone: 1);
        world.CreateBuilding(lot, Depot, Ticks.Zero, Key);

        Assert.Equal(0, world.Buildings.CarParkOf(0));
        Assert.False(world.Buildings.HasCarPark(1));
        Assert.Equal(1, world.CarParks.Rows.LiveCount);
    }

    /// <summary>
    /// A refit meets a Building that already has a Car Park and does not give it a second.
    /// </summary>
    /// <remarks>
    /// <b>The Bins' case, and it is reachable by the ordinary verb rather than by contrivance</b>:
    /// <c>World.Fit</c> runs at construction and again at every Ruleset swap, which is a hundred
    /// times in a designer's sitting under <c>adr/0015</c>.
    /// </remarks>
    [Fact]
    public void A_refit_does_not_give_a_building_a_second_car_park()
    {
        World world = City(spaces: 6);

        world.Adopt(Load(Parking(9)), HashB, new Ticks(64), Key);
        world.Adopt(Load(Parking(9)), HashB, new Ticks(65), Key);

        Assert.Equal(1, world.CarParks.Rows.LiveCount);
    }

    // ---- where it is -----------------------------------------------------------------------------

    /// <summary>
    /// <b>A Car Park sits at its Building's vehicle Access Point.</b>
    /// </summary>
    /// <remarks>
    /// <c>adr/0074</c> predicted this row — <em>"a parking Bin will have one"</em> — and only the
    /// type's name changed (<c>adr/0120</c>). The Address is <b>saved</b> rather than derived from the
    /// owner, because a Segment-held Car Park's Address is where the player put it; deriving would
    /// have forced street parking to bring a second column.
    /// </remarks>
    [Fact]
    public void A_car_park_sits_at_its_buildings_vehicle_access_point()
    {
        var world = new World(1_000, Load(ParkingOnAStreet(6)));

        Handle<Lot> lot = world.Lots.Create(new Tiles(0), new Tiles(0), zone: 1);
        int lotSlot = world.Lots.Rows.Resolve(lot);

        GiveFrontage(world, lotSlot);
        world.CreateBuilding(lot, Dwelling, Ticks.Zero, Key);

        Address expected = world.VehicleAccessPoint(0);
        int carPark = OnlyCarPark(world);

        Assert.True(expected.Exists);
        Assert.Equal(expected, world.CarParks.AddressAt(world.Roads.Segments, carPark));
    }

    /// <summary>
    /// ⚠ <b>A Building raised before its Street exists gets a Car Park with no Address, and nothing
    /// re-points it today.</b>
    /// </summary>
    /// <remarks>
    /// <b>The cost the saved disposition buys, asserted rather than left to be discovered.</b> It is
    /// bounded to Buildings raised before their frontage, and the repair belongs to task 3: the
    /// Parking Shed rebuilds on the per-Segment Epoch, which is the one pass that already runs when
    /// frontage changes. This test is expected to be <em>inverted</em> by that task rather than
    /// deleted — a silent no-Address Car Park is invisible supply, so the state has to stay named
    /// while it exists.
    /// </remarks>
    [Fact]
    public void A_building_with_no_frontage_gets_a_car_park_with_no_address()
    {
        World world = City(spaces: 6);
        int carPark = OnlyCarPark(world);

        Assert.Equal(Address.None, world.VehicleAccessPoint(0));
        Assert.Equal(Address.None, world.CarParks.AddressAt(world.Roads.Segments, carPark));
    }

    /// <summary>
    /// <b>Bulldozing the Street under a Car Park leaves it with no Address rather than with a stale
    /// one.</b>
    /// </summary>
    /// <remarks>
    /// <see cref="CarParkTable.WhereSegment"/> is <c>Reference.Severable</c> for
    /// <c>LegTable</c>'s reason, and the read boundary is where the severed handle becomes
    /// <c>adr/0079</c>'s named absence. The Car Park itself stays live: the parking did not stop
    /// existing, its road did.
    /// </remarks>
    [Fact]
    public void A_bulldozed_street_leaves_the_car_park_with_no_address()
    {
        var world = new World(1_000, Load(ParkingOnAStreet(6)));

        Handle<Lot> lot = world.Lots.Create(new Tiles(0), new Tiles(0), zone: 1);
        Handle<RoadSegment> segment = GiveFrontage(world, world.Lots.Rows.Resolve(lot));

        world.CreateBuilding(lot, Dwelling, Ticks.Zero, Key);

        int carPark = OnlyCarPark(world);
        Assert.True(world.CarParks.AddressAt(world.Roads.Segments, carPark).Exists);

        world.Roads.Segments.Rows.Free(segment);

        Assert.True(world.CarParks.Rows.IsLive(carPark));
        Assert.Equal(Address.None, world.CarParks.AddressAt(world.Roads.Segments, carPark));
    }

    // ---- the ceiling is the Ruleset in force -----------------------------------------------------

    /// <summary>
    /// <b>Retuning a kind's provision reaches every Building already standing, in both
    /// directions.</b>
    /// </summary>
    /// <remarks>
    /// <c>adr/0068</c> and <c>adr/0064</c> applied to parking: the alternative is a capacity frozen
    /// when the Building was raised, which makes a designer's edit true of the next Building and of
    /// no existing one — a city whose parking provision depends on when each garage happened to be
    /// built and which no reading of the file can predict.
    /// </remarks>
    [Theory]
    [InlineData(2)]
    [InlineData(11)]
    public void Retuning_the_provision_reaches_buildings_already_standing(int spaces)
    {
        World world = City(spaces: 6);
        int carPark = OnlyCarPark(world);

        world.Adopt(Load(Parking(spaces)), HashB, new Ticks(64), Key);

        Assert.Equal(spaces, world.CarParks.Capacity[carPark]);
    }

    /// <summary>
    /// <b>A Building whose kind the incoming Ruleset dropped keeps its parking.</b>
    /// </summary>
    /// <remarks>
    /// <c>World.TryDeclaredParking</c> keeps <em>declares no parking</em> and <em>is not declared at
    /// all</em> apart, which is <c>TryDeclaredJobs</c>' shape and its reason: a derelict Building
    /// <em>still stands and still occupies its Lot</em> (<c>CONTEXT</c> → Derelict Building), and
    /// dereliction must not evict a city's cars any more than it may sack a District. The failure
    /// collapsing them would produce is the loudest possible consequence for the quietest possible
    /// edit — deleting a <c>[[building]]</c> paragraph strands every car in the District.
    /// </remarks>
    [Fact]
    public void A_kind_the_ruleset_dropped_keeps_its_parking()
    {
        World world = City(spaces: 6);
        int carPark = OnlyCarPark(world);

        world.Adopt(Load(NoKinds), HashB, new Ticks(64), Key);

        Assert.True(world.CarParks.Rows.IsLive(carPark));
        Assert.Equal(6, world.CarParks.Capacity[carPark]);
    }

    /// <summary>
    /// ⚠ <b>A lowered provision can leave a Car Park over-full, and <see cref="CarParkTable.SpaceAt"/>
    /// reads negative until somebody dismisses the overflow.</b>
    /// </summary>
    /// <remarks>
    /// <b>The state the build passes through, asserted so that task 4 changes a test rather than
    /// discovering a case.</b> A Bin over its ceiling is left to <em>drain</em> because it has a
    /// consumer; a parked car has a <em>holder</em>, so nothing would ever spend the surplus down and
    /// the resolution has to be a <em>dismissal</em> — a write to the holders as well as to this
    /// column, which is why it does not belong in a capacity rebuild.
    /// </remarks>
    [Fact]
    public void A_lowered_provision_leaves_the_car_park_over_full()
    {
        World world = City(spaces: 6);
        int carPark = OnlyCarPark(world);

        world.CarParks.Move(carPark, 5);
        world.Adopt(Load(Parking(2)), HashB, new Ticks(64), Key);

        Assert.Equal(2, world.CarParks.Capacity[carPark]);
        Assert.Equal(5, world.CarParks.Occupied[carPark]);
        Assert.Equal(-3, world.CarParks.SpaceAt(carPark));
    }

    // ---- demolition ------------------------------------------------------------------------------

    /// <summary>
    /// <b>Demolishing a Building frees its Car Park, because the parking a garage provides stops
    /// existing when the garage does.</b>
    /// </summary>
    [Fact]
    public void Demolishing_a_building_frees_its_car_park()
    {
        World world = City(spaces: 6);
        int carPark = OnlyCarPark(world);

        world.DestroyBuilding(world.Buildings.Rows.At(0), new Ticks(64));

        Assert.False(world.CarParks.Rows.IsLive(carPark));
        Assert.Equal(0, world.CarParks.Rows.LiveCount);
    }

    /// <summary>
    /// <b>A demolished Car Park stops resolving for the Citizen parked in it, in the same act.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>CitizenTable.ParkedIn</c> is <c>Reference.Severable</c>, so demolition removes
    /// <em>both</em> sides of <c>adr/0084</c>'s conservation sum together and the loss cannot read as
    /// a leak. A car park that no longer resolves is the garage no longer existing, which is the fact
    /// rather than a break in it.
    /// </para>
    /// <para>
    /// ⚠ <b>The car is not re-parked anywhere, and that is <c>adr/0084</c>'s named second mutation
    /// site.</b> Whether a displaced car re-queries a shed, and where, is task 4's — it is the
    /// acquire/release pairing that decides it. What is settled here is only the conservation half.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_demolished_car_park_severs_the_handle_of_the_citizen_parked_in_it()
    {
        World world = City(spaces: 6);
        int carPark = OnlyCarPark(world);

        Handle<Building> building = world.Buildings.Rows.At(0);
        Handle<Household> household = world.CreateHousehold(building, lifeStage: 0);
        Handle<Citizen> citizen = world.CreateCitizen(household, Ticks.Zero);

        int citizenSlot = world.Citizens.Rows.Resolve(citizen);

        world.Citizens.ParkedIn[citizenSlot] = world.CarParks.Rows.At(carPark);
        world.CarParks.Move(carPark, 1);

        Assert.True(world.CarParks.Rows.TryResolve(world.Citizens.ParkedIn[citizenSlot], out _));

        world.DestroyBuilding(building, new Ticks(64));

        Assert.True(world.Citizens.Rows.IsLive(citizenSlot));
        Assert.False(world.CarParks.Rows.TryResolve(world.Citizens.ParkedIn[citizenSlot], out _));
    }

    /// <summary>
    /// <b>A recycled Car Park slot opens empty rather than carrying its predecessor's cars.</b>
    /// </summary>
    /// <remarks>
    /// <see cref="Rows{T}.Allocate"/> hands back a free slot without clearing any column, and
    /// demolition is what makes that reachable: the next Building raised on a cleared Lot would open
    /// with the condemned one's cars still parked in it — capacity destroyed from nothing, which
    /// reads as a busy city rather than as a defect.
    /// </remarks>
    [Fact]
    public void A_recycled_car_park_slot_opens_empty()
    {
        World world = City(spaces: 6);
        int carPark = OnlyCarPark(world);

        world.CarParks.Move(carPark, 4);
        world.DestroyBuilding(world.Buildings.Rows.At(0), new Ticks(64));

        Handle<Lot> lot = world.Lots.Create(new Tiles(64), new Tiles(0), zone: 1);
        Handle<Building> raised = world.CreateBuilding(lot, Dwelling, new Ticks(65), Key);

        int rebuilt = world.Buildings.CarParkOf(world.Buildings.Rows.Resolve(raised));

        Assert.Equal(carPark, rebuilt);
        Assert.Equal(0, world.CarParks.Occupied[rebuilt]);
    }

    // ---- the rebuild -----------------------------------------------------------------------------

    /// <summary>
    /// <b>The reverse index is rebuilt from the Car Parks' saved owners, and a Building with none
    /// still has none afterwards.</b>
    /// </summary>
    /// <remarks>
    /// <c>BuildingTable.CarPark</c> is <c>(derived AND rebuilt)</c> for <c>BinHead</c>'s reason: it is
    /// reproducible from <c>CarParkTable.Owner</c>, which is saved, so storing it twice would let the
    /// two disagree with nothing able to report it. The depot is in the assertion because a rebuild
    /// that attached everything to everything would pass the first half alone.
    /// </remarks>
    [Fact]
    public void A_rebuild_re_attaches_every_car_park_to_its_building()
    {
        World world = City(spaces: 6);

        Handle<Lot> lot = world.Lots.Create(new Tiles(64), new Tiles(0), zone: 1);
        world.CreateBuilding(lot, Depot, Ticks.Zero, Key);

        int carPark = world.Buildings.CarParkOf(0);

        world.RebuildDerived();

        Assert.True(world.Buildings.HasCarPark(0));
        Assert.Equal(carPark, world.Buildings.CarParkOf(0));
        Assert.False(world.Buildings.HasCarPark(1));
    }

    /// <summary>
    /// <b>A rebuild does not disturb a Car Park's occupancy</b>, which is saved state and not a
    /// derivation.
    /// </summary>
    /// <remarks>
    /// The half <c>adr/0084</c> is about: occupancy re-derived on load is a leak laundered into a
    /// clean number, so the check that it survives a rebuild is the one that would catch somebody
    /// moving it to the derived side for tidiness.
    /// </remarks>
    [Fact]
    public void A_rebuild_leaves_occupancy_alone()
    {
        World world = City(spaces: 6);
        int carPark = OnlyCarPark(world);

        world.CarParks.Move(carPark, 3);
        world.RebuildDerived();

        Assert.Equal(3, world.CarParks.Occupied[carPark]);
        Assert.Equal(6, world.CarParks.Capacity[carPark]);
    }
}
