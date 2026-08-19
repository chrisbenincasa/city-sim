using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Parking;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Tests.Space;

namespace Borough.Tests.Parking;

/// <summary>
/// <b>The Parking Shed ball: what it finds, in what order, and what it refuses to walk to.</b>
/// </summary>
/// <remarks>
/// <para>
/// Hand-built graphs rather than a generated city, for <see cref="RoadFixtures.Chain"/>'s reason: the
/// answer is known in advance, so each assertion is against a number rather than against itself. The
/// distributions over a real world are <see cref="ParkingShedSizeTests"/>' job and cannot check
/// <em>correctness</em> — every reading there is consistent with a ball that walks the wrong way.
/// </para>
/// <para>
/// <b>A chain of 32-Tile Segments is 128 m a block</b>, which is the shipped <c>[roads]</c> geometry,
/// so a radius here reads in the same units the Ruleset authors.
/// </para>
/// </remarks>
public sealed class ParkingShedTests
{
    /// <summary>Three blocks, so a radius can sit between two Car Parks rather than beyond both.</summary>
    private static Tiles Block => new(32);

    /// <summary>
    /// <b>A Car Park on the door's own Street is the offset difference away, not the length of the
    /// block.</b>
    /// </summary>
    /// <remarks>
    /// <c>WalkRouting</c>'s same-Segment case, and the one a ball structurally cannot express: a ball
    /// starts at nodes, so routing a neighbour four doors down through an endpoint would price a 16 m
    /// walk at 240 m. It is checked first because every other reading here would still look sensible
    /// with it wrong.
    /// </remarks>
    [Fact]
    public void A_car_park_on_the_same_street_is_reached_along_it_and_not_around_it()
    {
        Shed shed = new(nodes: 4);

        int near = shed.Park(segment: 1, offset: new Tiles(20));

        Assert.Equal(new[] { near }, shed.Nearest(door: new Tiles(16), onSegment: 1, radius: new Tiles(8)));

        // Four Tiles apart along the Street. Round the block it is 16 + 20 = 36, so a radius of 8
        // admits it only if the same-Segment case is taken.
        Assert.Equal(new Tiles(4), shed.DistanceOf(0));
    }

    /// <summary>
    /// <b>The answer is nearest first, and a Car Park past the radius is not in it at all.</b>
    /// </summary>
    [Fact]
    public void A_shed_is_nearest_first_and_stops_at_the_radius()
    {
        Shed shed = new(nodes: 5);

        int oneBlock = shed.Park(segment: 1, offset: new Tiles(16));
        int twoBlocks = shed.Park(segment: 2, offset: new Tiles(16));
        int threeBlocks = shed.Park(segment: 3, offset: new Tiles(16));

        // The door is at the far end of Segment 0, so the three are 16, 48 and 80 Tiles away.
        Assert.Equal(
            new[] { oneBlock, twoBlocks },
            shed.Nearest(door: Block, onSegment: 0, radius: new Tiles(64)));

        Assert.Equal(
            new[] { oneBlock, twoBlocks, threeBlocks },
            shed.Nearest(door: Block, onSegment: 0, radius: new Tiles(96)));

        Assert.Equal(new Tiles(16), shed.DistanceOf(0));
        Assert.Equal(new Tiles(48), shed.DistanceOf(1));
        Assert.Equal(new Tiles(80), shed.DistanceOf(2));
    }

    /// <summary>
    /// <b>Two Car Parks at the same distance come back in slot order, whichever the ball met first.</b>
    /// </summary>
    /// <remarks>
    /// <b>Not a nicety.</b> A generated city is a grid of identical Streets with a Car Park on every
    /// Building, so most comparisons in a real shed <em>are</em> ties — and a tie resolved by the
    /// order the adjacency happens to list Arcs in would make an accumulated shed and a rebuilt one
    /// disagree, with nothing in the corpus able to report it.
    /// </remarks>
    [Fact]
    public void Two_car_parks_the_same_distance_away_come_back_in_slot_order()
    {
        Shed shed = new(nodes: 3);

        // Equidistant from a door in the middle: one 16 Tiles along Segment 0, one 16 along
        // Segment 1, and the door at the node between them.
        int first = shed.Park(segment: 1, offset: new Tiles(16));
        int second = shed.Park(segment: 0, offset: new Tiles(16));

        Assert.True(first < second);

        Assert.Equal(
            new[] { first, second },
            shed.Nearest(door: Block, onSegment: 0, radius: new Tiles(32)));

        Assert.Equal(shed.DistanceOf(0), shed.DistanceOf(1));
    }

    /// <summary>
    /// <b>A Car Park the ball cannot walk to is not in the shed however near it is in metres</b> —
    /// which is what makes <c>03 §3.7</c>'s Severance visible in parking.
    /// </summary>
    /// <remarks>
    /// <see cref="RoadFixtures.TwoIslands"/> puts the two chains 4,096 Tiles apart, so a radius
    /// covering the whole of one island reaches nothing on the other. <b>A box on the Cell grid would
    /// have found it and a ball does not</b>, which is <see cref="Space.BuildingResidency"/>'s stated
    /// non-goal arriving as a mechanism.
    /// </remarks>
    [Fact]
    public void A_car_park_in_another_component_is_in_nobodys_shed()
    {
        Shed shed = new(RoadFixtures.TwoIslands(each: 3));

        int mine = shed.Park(segment: 0, offset: new Tiles(16));
        shed.Park(segment: 2, offset: new Tiles(16));

        Assert.Equal(
            new[] { mine },
            shed.Nearest(door: Tiles.Zero, onSegment: 0, radius: new Tiles(8_192)));
    }

    /// <summary>
    /// <b>A Street that admits no pedestrian is not walked down, so the parking on it is unreachable
    /// from the far side.</b>
    /// </summary>
    /// <remarks>
    /// The Severance case one Segment wide rather than one component wide — a motorway with no
    /// pavement, which <c>adr/0072</c>'s mode mask is what makes expressible. <b>The Car Park on the
    /// motorway itself is still found</b>, because it is on the door's own Segment and reached along
    /// it; what the mask blocks is walking <em>through</em>.
    /// </remarks>
    [Fact]
    public void A_street_that_admits_no_pedestrian_is_not_walked_down()
    {
        Shed shed = new(RoadFixtures.Chain(4, TravelMode.Car, TravelMode.Car));

        shed.Park(segment: 1, offset: new Tiles(16));

        Assert.Empty(shed.Nearest(door: Tiles.Zero, onSegment: 0, radius: new Tiles(8_192)));
    }

    /// <summary>
    /// <b>What was kept and what was found are different numbers, and the second is what a storage
    /// decision is taken on.</b>
    /// </summary>
    /// <remarks>
    /// S2 R5.6's <c>ShedBuilder</c> keeps <c>KeptBins = 8</c> and counts the rest in a separate
    /// accumulator, so its published <b>110 at 400 m</b> is what its ball <em>encountered</em> — and
    /// <c>adr/0083</c>'s <i>doubling the radius is roughly 5× the shed</i> is true of the ball and
    /// false of the shed, which is constant at 8. <c>plans/0012</c> <b>Cause 5</b>: the digits
    /// travelled and the clause did not. This asserts the two are separable here.
    /// </remarks>
    [Fact]
    public void A_shed_reports_what_it_found_as_well_as_what_it_kept()
    {
        Shed shed = new(nodes: 5);

        int nearest = shed.Park(segment: 0, offset: new Tiles(16));
        shed.Park(segment: 1, offset: new Tiles(16));
        shed.Park(segment: 2, offset: new Tiles(16));

        // Uncapped, so the ball walks the whole radius and `found` is the shed's true size. A capped
        // query is the next test's subject and would stop before meeting the far two.
        int[] all = shed.Nearest(door: new Tiles(16), onSegment: 0, radius: new Tiles(96), keep: 64);

        Assert.Equal(nearest, all[0]);
        Assert.Equal(3, all.Length);
        Assert.Equal(3, shed.Found);
    }

    /// <summary>
    /// <b>A query that keeps one looks at fewer than a query that keeps everything, and comes back with
    /// the same one.</b>
    /// </summary>
    /// <remarks>
    /// The other half of the sentence above, and it is the contract the early exit changed. <c>found</c>
    /// is <em>what the query looked at</em>; it equals a shed's size only when nothing was capped. Here
    /// the door's own Car Park fills a kept set of one at distance zero, so the ball stops on its first
    /// pop and never meets the other two — <b>and the answer is unaffected</b>, which is the property
    /// worth asserting. Asserting the count alone would forbid the optimisation; asserting the answer
    /// alone would not notice if it stopped working.
    /// </remarks>
    [Fact]
    public void Capping_a_shed_stops_the_ball_early_without_moving_the_answer()
    {
        Shed shed = new(nodes: 5);

        int nearest = shed.Park(segment: 0, offset: new Tiles(16));
        shed.Park(segment: 1, offset: new Tiles(16));
        shed.Park(segment: 2, offset: new Tiles(16));

        int[] capped = shed.Nearest(door: new Tiles(16), onSegment: 0, radius: new Tiles(96), keep: 1);
        int looked = shed.Found;

        Assert.Equal(new[] { nearest }, capped);
        Assert.True(
            looked < 3,
            $"the ball looked at {looked} Car Parks with a kept set of one, so it did not stop early.");
    }

    /// <summary>
    /// <b>A Car Park whose Street was bulldozed is in nobody's shed, and a Building with no frontage
    /// has none.</b>
    /// </summary>
    /// <remarks>
    /// Both are <c>adr/0079</c>'s named absence rather than corruption — a Building outlives its
    /// frontage — and they are asserted together because they are the same hole seen from its two
    /// ends: the supply side has no Address to be indexed at, and the demand side has no door to
    /// search from.
    /// </remarks>
    [Fact]
    public void A_car_park_with_no_address_and_a_door_with_none_both_come_back_empty()
    {
        Shed shed = new(nodes: 4);

        shed.Park(segment: 1, offset: new Tiles(16));

        Assert.NotEmpty(shed.Nearest(door: Tiles.Zero, onSegment: 0, radius: new Tiles(96)));

        shed.Bulldoze(segment: 1);

        Assert.Empty(shed.Nearest(door: Tiles.Zero, onSegment: 0, radius: new Tiles(96)));
        Assert.Empty(shed.Nearest(Address.None, new Tiles(96), keep: 8));
    }

    /// <summary>
    /// <b>On a real lattice, a capped shed is the exhaustive shed's first few — every Car Park, every
    /// door, exactly.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the early exit's correctness, and nothing else in this file can check it.</b> The
    /// hand-built graphs above are chains, and a chain has no branch — the ball has one direction to go,
    /// so it cannot demonstrate that stopping short loses nothing. The property is that a query keeping
    /// <c>k</c> returns exactly what a query keeping everything would have returned, truncated to
    /// <c>k</c>: same Car Parks, same order, at every door.
    /// </para>
    /// <para>
    /// <b>Generated rather than simulated.</b> <see cref="RoadGenerator.LayInto"/> gives the shipped
    /// 32-Tile lattice without running a Tick, and the Car Parks are placed by hand at four offsets a
    /// Segment — near <c>[lots] lots_per_segment = 5</c> without depending on the Zone Rules having run.
    /// What is being tested is the ball, so the supply only has to be dense and branching.
    /// </para>
    /// <para>
    /// ⚠ <b>It sweeps every door rather than sampling.</b> An exit that is wrong is wrong on the
    /// geometry of one neighbourhood, so a sample is a test that passes until the day the sampled doors
    /// move — <c>plans/0014</c> task 11's lesson, that a run which stops reaching a branch reports
    /// nothing.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_capped_shed_is_the_exhaustive_sheds_first_few_at_every_door()
    {
        const int Extent = 256;
        const int Cap = 8;

        var key = WorldKey.FromSeed(0x0FA4_C0DE_0000_0001UL);
        World world = new(64, RoadFixtures.With(RoadFixtures.Lattice(32)));

        RoadGenerator.LayInto(world.Roads, key, Extent);

        RoadSegmentTable segments = world.Roads.Segments;
        CarParkTable carParks = world.CarParks;

        for (int slot = 0; slot < segments.Rows.SlotCount; slot++)
        {
            if (!segments.Rows.IsLive(slot))
            {
                continue;
            }

            foreach (int offset in new[] { 4, 12, 20, 28 })
            {
                carParks.Create(
                    default, segments.Rows.At(slot), new Tiles(offset), StreetSide.Right, capacity: 4);
            }
        }

        CarParkResidency residency = new();
        residency.Rebuild(carParks, segments);

        ShedScratch scratch = new();
        Tiles radius = new(100);

        int[] capped = new int[Cap];
        int[] exhaustive = new int[4096];
        int doors = 0;

        for (int slot = 0; slot < segments.Rows.SlotCount; slot++)
        {
            if (!segments.Rows.IsLive(slot))
            {
                continue;
            }

            Address door = Address.On(slot, new Tiles(8), StreetSide.Right);

            int few = ParkingShed.Nearest(
                world.Roads, carParks, residency, door, radius, scratch, capped, out _);

            int all = ParkingShed.Nearest(
                world.Roads, carParks, residency, door, radius, scratch, exhaustive, out int found);

            Assert.Equal(Math.Min(all, Cap), few);
            Assert.Equal(exhaustive[..few], capped[..few]);

            // The exhaustive query must genuinely be doing more work, or the two sides agree because
            // neither reached anything and the assertion above is vacuous.
            Assert.True(found > Cap, $"door on Segment {slot} found only {found}, so nothing was capped.");

            doors++;
        }

        Assert.True(doors > 100, $"only {doors} doors were swept, which is not a lattice.");
    }

    /// <summary>A graph, some Car Parks on it, and the index between them.</summary>
    private sealed class Shed
    {
        private readonly RoadGraph _graph;
        private readonly BuildingTable _buildings;
        private readonly CarParkTable _carParks;
        private readonly CarParkResidency _residency = new();
        private readonly ShedScratch _scratch = new();

        private Tiles[] _distances = [];

        public Shed(int nodes)
            : this(RoadFixtures.Chain(nodes))
        {
        }

        public Shed(RoadGraph graph)
        {
            _graph = graph;
            _buildings = new BuildingTable(16, new LotTable(16));
            _carParks = new CarParkTable(16, _buildings, graph.Segments);
        }

        /// <summary>How many Car Parks the last query found in range, kept or not.</summary>
        public int Found { get; private set; }

        /// <summary>Puts a Car Park on a Segment and returns its slot.</summary>
        public int Park(int segment, Tiles offset)
        {
            Handle<CarPark> handle = _carParks.Create(
                default, _graph.Segments.Rows.At(segment), offset, StreetSide.Right, capacity: 4);

            return _carParks.Rows.Resolve(handle);
        }

        /// <summary>Frees a Segment, severing the handles of the Car Parks on it.</summary>
        public void Bulldoze(int segment) =>
            _graph.Segments.Rows.Free(_graph.Segments.Rows.At(segment));

        public int[] Nearest(Tiles door, int onSegment, Tiles radius, int keep = 8) =>
            Nearest(Address.On(onSegment, door, StreetSide.Right), radius, keep);

        public int[] Nearest(Address door, Tiles radius, int keep)
        {
            _residency.Rebuild(_carParks, _graph.Segments);

            int[] into = new int[keep];

            int kept = ParkingShed.Nearest(
                _graph, _carParks, _residency, door, radius, _scratch, into, out int found);

            Found = found;
            _distances = new Tiles[kept];

            for (int i = 0; i < kept; i++)
            {
                _distances[i] = _scratch.KeptAt(i);
            }

            return into[..kept];
        }

        /// <summary>How far the <paramref name="index"/>th Car Park of the last answer was.</summary>
        public Tiles DistanceOf(int index) => _distances[index];
    }
}
