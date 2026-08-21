using System.Globalization;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Invariants;
using Borough.Core.Parking;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Parking;

/// <summary>
/// <b>Taking a parking space and giving it back — the two writes that must happen together.</b>
/// </summary>
/// <remarks>
/// <para>
/// Milestone 7 task 4. <c>adr/0119</c> puts the space on the <b>Citizen</b>, so the pair being tested
/// is <c>CitizenTable.ParkedIn</c> against <c>CarParkTable.Occupied</c> — a handle and a count that
/// are only ever correct together. <c>CarParkTable.Move</c> is <see langword="internal"/> precisely so
/// that no caller can move one without the other, and these tests go through
/// <see cref="World.TryTakeParking"/> and <see cref="World.ReleaseParking"/> rather than through the
/// columns for that reason: a test that wrote the columns by hand would pass with the pairing undone.
/// </para>
/// <para>
/// ⚠ <b>The interesting cases are the ones where nothing is a violation.</b> A Citizen who holds no
/// space, a Citizen whose garage was demolished under them, and a shed with no room are all ordinary
/// answers rather than failures, and each has a test here saying so — because the cheap mistake is to
/// make the invariant fire on them and then discover, in a city, that it fires constantly.
/// </para>
/// </remarks>
public sealed class ParkingHoldTests
{
    private const byte Dwelling = 1;

    private static readonly WorldKey Key = WorldKey.FromSeed(0x7000_0004UL);

    /// <summary>
    /// One dwelling kind parking <c>PARKS</c> Vehicles, on a Street, with a shed that reaches.
    /// </summary>
    /// <remarks>
    /// <b>The <c>[parking]</c> table is present and that is load-bearing</b> — <c>ParkingRuleset.Runs</c>
    /// is <c>radius_metres &gt; 0</c>, and a file omitting the table would make every acquire below
    /// answer <see langword="false"/> for a reason that has nothing to do with what is being tested.
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

        [parking]
        radius_metres = 400
        shed_keeps = 24

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

    // ---- taking one ------------------------------------------------------------------------------

    /// <summary>
    /// <b>A driver takes the space and both writes land.</b>
    /// </summary>
    /// <remarks>
    /// The acceptance test for the task. It asserts the <em>pair</em> rather than either half, because
    /// either half alone is a defect with a name: an occupancy without a holder is capacity conjured
    /// from nothing, and a holder without an occupancy is a space two Citizens can take.
    /// </remarks>
    [Fact]
    public void A_driver_takes_a_space_and_the_two_writes_happen_together()
    {
        Fixture city = Fixture.WithParking(spaces: 4);

        Assert.True(city.Take(out int carPark));

        Assert.Equal(1, city.World.CarParks.Occupied[carPark]);
        Assert.True(
            city.World.CarParks.Rows.TryResolve(
                city.World.Citizens.ParkedIn[city.Citizen], out int held));
        Assert.Equal(carPark, held);
    }

    /// <summary>
    /// <b>A release gives the space back and clears the holding.</b>
    /// </summary>
    [Fact]
    public void A_release_gives_the_space_back_and_clears_the_holding()
    {
        Fixture city = Fixture.WithParking(spaces: 4);

        Assert.True(city.Take(out int carPark));
        Assert.True(city.World.ReleaseParking(city.Citizen));

        Assert.Equal(0, city.World.CarParks.Occupied[carPark]);
        Assert.Equal(default, city.World.Citizens.ParkedIn[city.Citizen]);
    }

    /// <summary>
    /// 🔴 <b>Retiring a parked Citizen gives its space back.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>plans/0035</c> <b>F30</b>, and it is <b>F29</b>'s shape on a second structure.</b>
    /// <see cref="World.ReleaseParking"/> had exactly one caller — <c>TripEngine</c>, when a driver
    /// leaves — so a Citizen whose row was freed while it held a space left
    /// <c>CarParkTable.Occupied</c> counting a car nobody was in. The space was then occupied for the
    /// rest of the run and no Vehicle could ever take it.
    /// </para>
    /// <para>
    /// ⚠ <b>It presents as a slow leak and not as a failure.</b>
    /// <see cref="Invariant.ParkingOccupancyIsConserved"/> sums both sides and catches it, but only
    /// at the whole-world tier — so the run has to reach its end before anything says a word. Found
    /// by milestone 11 task 9's acceptance run at Tick 65,664, reading <b>234</b> occupied spaces
    /// against <b>233</b> holders.
    /// </para>
    /// <para>
    /// ⚠ <b>Nothing destroyed a Citizen mid-run until this milestone</b>, which is why eleven
    /// milestones of long runs never saw it: the only production caller of
    /// <see cref="World.DestroyCitizen"/> arrived with <c>adr/0130</c>'s give-up bound.
    /// ***A path exercised only by fixtures is a path with no long run behind it.***
    /// </para>
    /// </remarks>
    [Fact]
    public void Retiring_a_parked_citizen_gives_the_space_back()
    {
        Fixture city = Fixture.WithParking(spaces: 4);

        Assert.True(city.Take(out int carPark));
        Assert.Equal(1, city.World.CarParks.Occupied[carPark]);

        city.World.DestroyCitizen(city.World.Citizens.Rows.At(city.Citizen));

        Assert.Equal(0, city.World.CarParks.Occupied[carPark]);
    }

    /// <summary>
    /// 🔴 <b>And retiring the Household its members park under gives every space back.</b>
    /// </summary>
    /// <remarks>
    /// <b>The production path, which is <see cref="World.Depart"/>'s.</b> A Household gives up and
    /// leaves through the gate; nothing walks its Citizens individually. This is the case the
    /// acceptance run actually hit, and it passes only because
    /// <see cref="World.DestroyHousehold"/> now retires its members through
    /// <see cref="World.DestroyCitizen"/> rather than by hand — <c>plans/0035</c> <b>F29</b>.
    /// ***One repair closing two leaks is the evidence that consolidating the path was the fix and
    /// the missing call was not.***
    /// </remarks>
    [Fact]
    public void Retiring_a_household_gives_back_every_space_its_members_held()
    {
        Fixture city = Fixture.WithParking(spaces: 4);

        Assert.True(city.Take(out int carPark));
        Assert.True(city.Take(city.AddCitizen(), out int second));

        Assert.Equal(carPark, second);
        Assert.Equal(2, city.World.CarParks.Occupied[carPark]);

        city.World.DestroyHousehold(city.TheHousehold);

        Assert.Equal(0, city.World.CarParks.Occupied[carPark]);
    }

    /// <summary>
    /// <b>Two drivers take two of the same Car Park's spaces, and the count is two.</b>
    /// </summary>
    /// <remarks>
    /// <c>adr/0119</c>'s <em>a Household holds as many cars as it has drivers</em> reaching the
    /// occupancy: the space is per Citizen, so two Citizens of one Household park two cars and the
    /// Car Park knows about both. A per-Household column would have recorded one.
    /// </remarks>
    [Fact]
    public void Two_drivers_of_one_household_take_two_spaces()
    {
        Fixture city = Fixture.WithParking(spaces: 4);
        int second = city.AddCitizen();

        Assert.True(city.Take(out int carPark));
        Assert.True(city.Take(second, out int alsoCarPark));

        Assert.Equal(carPark, alsoCarPark);
        Assert.Equal(2, city.World.CarParks.Occupied[carPark]);
    }

    // ---- refusing --------------------------------------------------------------------------------

    /// <summary>
    /// ⚠ <b>An exhausted shed refuses, and does not park at the kerb.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>adr/0008</c> forbids the fallback <em>by name</em>, and this is the first milestone at which
    /// the prohibition is enforceable — until task 3 there was no Shed to be exhausted. The failure it
    /// forbids reads as generosity rather than as a bug: a kerbside space at zero cost makes a full
    /// car park <em>cheaper</em> than an empty one, so under-building parking would pay the player.
    /// </para>
    /// <para>
    /// <b>The assertion is on the occupancy as well as on the answer</b>, because the shape being
    /// refused is not only <em>returns true</em> — it is <em>returns true having incremented
    /// something</em>, which is how a capacity gets exceeded quietly.
    /// </para>
    /// </remarks>
    [Fact]
    public void An_exhausted_shed_refuses_rather_than_parking_at_the_kerb()
    {
        Fixture city = Fixture.WithParking(spaces: 1);

        Assert.True(city.Take(out int carPark));

        int crowd = city.AddCitizen();

        Assert.False(city.Take(crowd, out int none));
        Assert.Equal(Rows.NoSlot, none);
        Assert.Equal(default, city.World.Citizens.ParkedIn[crowd]);
        Assert.Equal(1, city.World.CarParks.Occupied[carPark]);
        Assert.Equal(
            city.World.CarParks.Capacity[carPark], city.World.CarParks.Occupied[carPark]);
    }

    /// <summary>
    /// <b>A Citizen holding nothing releases nothing, and that is not a violation.</b>
    /// </summary>
    /// <remarks>
    /// A walker, a Citizen who has never driven and a driver already in motion are all <i>parked
    /// nowhere</i>. If this fired, it would fire on most of the city most of the time.
    /// </remarks>
    [Fact]
    public void Releasing_a_space_a_citizen_never_held_is_not_a_violation()
    {
        Fixture city = Fixture.WithParking(spaces: 4);

        Assert.False(city.World.ReleaseParking(city.Citizen));
        Assert.Equal(default, city.World.Citizens.ParkedIn[city.Citizen]);
    }

    /// <summary>
    /// ⚠ <b>A Car Park demolished under a parked car clears the holding and decrements nothing.</b>
    /// </summary>
    /// <remarks>
    /// <c>CitizenTable.ParkedIn</c> is <c>Reference.Severable</c> so that this is representable rather
    /// than a break. Both sides of <c>adr/0084</c>'s conservation sum lose the row together — the
    /// occupancy went when the row was freed — so decrementing here would remove one side twice, which
    /// is the leak the invariant exists to catch rather than a repair of it.
    /// </remarks>
    [Fact]
    public void A_demolished_car_park_clears_the_holding_without_decrementing()
    {
        Fixture city = Fixture.WithParking(spaces: 4);

        Assert.True(city.Take(out int carPark));

        city.World.DestroyBuilding(city.Building, Ticks.Zero);

        Assert.False(city.World.CarParks.Rows.IsLive(carPark));
        Assert.False(city.World.ReleaseParking(city.Citizen));
        Assert.Equal(default, city.World.Citizens.ParkedIn[city.Citizen]);
    }

    // ---- the diagnostic --------------------------------------------------------------------------

    /// <summary>
    /// <b>Releasing a space whose occupancy is already zero fires
    /// <see cref="Invariant.ParkingSpaceIsReleasedOnce"/>.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The violation is written by hand because no path through the public API reaches it</b>, which
    /// is the point of a write-site check and is <c>TripEngine.Release</c>'s own argument: a test
    /// confined to the legitimate callers could only assert that the guard never fires, and that claim
    /// is equally true of a guard that is not there.
    /// </para>
    /// <para>
    /// <c>CarParkTable.Move</c> is <see langword="internal"/> rather than private for exactly this —
    /// the corruption has to be expressible from the test assembly or the diagnostic ships untested.
    /// </para>
    /// </remarks>
    [Fact]
    public void Releasing_a_space_that_was_already_given_back_fires_the_invariant()
    {
        Fixture city = Fixture.WithParking(spaces: 4);

        Assert.True(city.Take(out int carPark));

        // The corruption: the occupancy is dropped without the holding, so the Citizen still names a
        // Car Park that no longer believes anybody is in it. That is the state a second release site
        // would produce, and the one this guard exists for.
        city.World.CarParks.Move(carPark, -1);

        InvariantViolationException violation =
            Assert.Throws<InvariantViolationException>(() => city.World.ReleaseParking(city.Citizen));

        Assert.Equal(Invariant.ParkingSpaceIsReleasedOnce, violation.Violation.Invariant);
        Assert.Equal(city.Citizen, violation.Violation.Slot);
    }

    // ---- the fixture -----------------------------------------------------------------------------

    /// <summary>One dwelling with a Car Park on a Street, and a Citizen living in it.</summary>
    private sealed class Fixture
    {
        private readonly ShedScratch _scratch = new();

        private Fixture(World world, Handle<Building> building, int citizen)
        {
            World = world;
            Building = building;
            Citizen = citizen;
        }

        public World World { get; }

        public Handle<Building> Building { get; }

        /// <summary>The driver every test without a second one uses.</summary>
        public int Citizen { get; }

        private Handle<Household> Household { get; init; }

        /// <summary>The Household every Citizen in this fixture belongs to.</summary>
        public Handle<Household> TheHousehold => Household;

        public static Fixture WithParking(int spaces)
        {
            var world = new World(1_000, Load(spaces));

            Handle<Lot> lot = world.Lots.Create(new Tiles(0), new Tiles(0), zone: 1);

            GiveFrontage(world, world.Lots.Rows.Resolve(lot));

            Handle<Building> building = world.CreateBuilding(lot, Dwelling, Ticks.Zero, Key);
            Handle<Household> household = world.CreateHousehold(building, lifeStage: 0);
            Handle<Citizen> citizen = world.CreateCitizen(household, Ticks.Zero);

            return new Fixture(world, building, world.Citizens.Rows.Resolve(citizen))
            {
                Household = household,
            };
        }

        /// <summary>A second driver in the same Household.</summary>
        public int AddCitizen() =>
            World.Citizens.Rows.Resolve(World.CreateCitizen(Household, Ticks.Zero));

        public bool Take(out int carPark) => Take(Citizen, out carPark);

        /// <summary>Parks <paramref name="citizen"/> at the dwelling's own door.</summary>
        public bool Take(int citizen, out int carPark) =>
            World.TryTakeParking(
                citizen,
                World.VehicleAccessPoint(World.Buildings.Rows.Resolve(Building)),
                _scratch,
                out carPark);

        /// <summary>
        /// Two nodes and one Segment, built by hand so the Address is known in advance.
        /// </summary>
        private static void GiveFrontage(World world, int lotSlot)
        {
            Handle<RoadNode> a = world.Roads.Nodes.Create(Tiles.Zero, Tiles.Zero);
            Handle<RoadNode> b = world.Roads.Nodes.Create(new Tiles(32), Tiles.Zero);

            Handle<RoadSegment> segment = world.Roads.Segments.Create(
                a, b, new Tiles(32), RoadKind.Street, TravelMode.Any, TravelMode.Any);

            world.Roads.RebuildDerived();

            world.Lots.FrontageSlot[lotSlot] = world.Roads.Segments.Rows.Resolve(segment) + 1;
            world.Lots.FrontageOffset[lotSlot] = new Tiles(16);
            world.Lots.Side[lotSlot] = (byte)StreetSide.Right;
        }

        private static Ruleset Load(int spaces)
        {
            RulesetLoadResult result = RulesetLoader.Parse(
                Template.Replace(
                    "PARKS",
                    spaces.ToString(CultureInfo.InvariantCulture),
                    StringComparison.Ordinal),
                "test.toml");

            Assert.True(result.Ok, result.Describe());

            return result.Ruleset!;
        }
    }
}
