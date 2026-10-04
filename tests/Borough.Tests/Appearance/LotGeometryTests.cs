using System.Numerics;
using Borough.Appearance;
using Borough.Core.Arithmetic;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Tests.Space;

namespace Borough.Tests.Appearance;

public sealed class LotGeometryTests
{
    [Theory]
    [InlineData(65536, 0, StreetSide.Left, 0, -1)]
    [InlineData(65536, 0, StreetSide.Right, 0, 1)]
    [InlineData(0, 65536, StreetSide.Left, 1, 0)]
    [InlineData(0, 65536, StreetSide.Right, -1, 0)]
    public void Lattice_fronts_keep_their_original_normals(int east, int north, StreetSide side, int frontEast, int frontNorth)
    {
        var rectangle = OrientedRectangle.FromBounds(new(10, 10, 4, 6));
        Assert.Equal(new Vector2(frontEast, frontNorth), LotGeometry.Front(rectangle, east, north, side));
    }

    [Fact]
    public void Rotated_lot_faces_its_curved_frontage_at_the_saved_offset_without_writing_state()
    {
        var world = new World(16, RoadFixtures.With(RoadFixtures.Lattice()));
        var a = world.Roads.Nodes.Create(new Tiles(100), new Tiles(100));
        var b = world.Roads.Nodes.Create(new Tiles(200), new Tiles(100));
        var segment = world.Roads.Segments.Create(a, b, new Tiles(110), RoadKind.Street, TravelMode.Any, TravelMode.Any);
        int road = world.Roads.Segments.Rows.Resolve(segment);
        world.Roads.Segments.Sagitta[road] = SubTiles.FromTiles(new Tiles(20));
        world.Roads.RebuildDerived();
        var handle = world.Lots.Create(new Tiles(125), new Tiles(110), LotTable.Housing, StreetSide.Left);
        int lot = world.Lots.Rows.Resolve(handle);
        OrientedRectangle rectangle = Rotated();
        world.Lots.AxisEastQ16[lot] = rectangle.AxisEastQ16;
        world.Lots.AxisNorthQ16[lot] = rectangle.AxisNorthQ16;
        world.Lots.ParcelEastQ16[lot] = rectangle.EastQ16;
        world.Lots.ParcelNorthQ16[lot] = rectangle.NorthQ16;
        world.Lots.ParcelWide[lot] = new Tiles(rectangle.Wide);
        world.Lots.ParcelDeep[lot] = new Tiles(rectangle.Deep);
        world.Lots.Front(lot, segment, new Tiles(20));
        ulong hash = world.HashState();

        Vector2 front = LotGeometry.Front(world, lot);
        Assert.Equal(-Vector2.UnitY, front);
        Vector2 normal = LotGeometry.Direction(rectangle, front);
        var tangent = world.Roads.Segments.Centerline[road].TangentAt(Fixed.FromInt(20));
        Vector2 streetward = new(tangent.North / (float)Fixed.One, -tangent.East / (float)Fixed.One);
        Assert.True(Vector2.Dot(normal, streetward) > .9f);
        Assert.Equal(hash, world.HashState());

        // The far end's tangent selects another rectangle edge; a chord-only lookup cannot do this.
        world.Lots.Front(lot, segment, new Tiles(100));
        Assert.Equal(-Vector2.UnitX, LotGeometry.Front(world, lot));
        world.Lots.Unfront(lot);
        Assert.Equal(-Vector2.UnitY, LotGeometry.Front(world, lot));
        Assert.Equal(front, LotGeometry.Front(world, lot));
    }

    [Fact]
    public void Rotated_points_garden_space_and_neighbours_use_rectangle_axes()
    {
        OrientedRectangle rectangle = Rotated();
        Vector2 center = LotGeometry.Point(rectangle, new Vector2(4, 3));
        Assert.InRange(Math.Abs(center.X - (100 + 4 * MathF.Cos(MathF.PI / 6) - 1.5f)), 0, .0002f);
        Assert.InRange(Math.Abs(center.Y - (100 + 2 + 3 * MathF.Cos(MathF.PI / 6))), 0, .0002f);
        Assert.InRange(Vector2.Distance(new Vector2(4, 3), LotGeometry.Local(rectangle, center)), 0, .00002f);
        Assert.True(rectangle.Contains(ToQ16(center.X), ToQ16(center.Y)));
        Assert.False(rectangle.Contains(Fixed.FromInt(rectangle.Bounds.X), Fixed.FromInt(rectangle.Bounds.Y)));

        var neighbour = rectangle with
        {
            EastQ16 = rectangle.EastQ16 + rectangle.Wide * rectangle.AxisEastQ16,
            NorthQ16 = rectangle.NorthQ16 + rectangle.Wide * rectangle.AxisNorthQ16,
        };
        Assert.True(LotGeometry.SameDepth(rectangle, neighbour, alongA: true));
        Assert.False(LotGeometry.SameDepth(rectangle, neighbour with { Deep = 5 }, alongA: true));
        Assert.False(rectangle.Overlaps(neighbour));
        Vector2 probe = LotGeometry.Point(rectangle, new Vector2(8.125f, 3));
        Assert.True(neighbour.Contains(ToQ16(probe.X), ToQ16(probe.Y)));
        var foot = rectangle with { Wide = 4, Deep = 2 };
        Assert.Equal(4, LotGeometry.BackSpace(rectangle, foot, -Vector2.UnitY));
        Assert.Equal(4, LotGeometry.BackSpace(rectangle, foot, -Vector2.UnitX));
        Assert.Equal(0, LotGeometry.BackSpace(rectangle, foot, Vector2.UnitY));
        Assert.Equal(0, LotGeometry.BackSpace(rectangle, foot, Vector2.UnitX));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(1, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 1)]
    public void Trade_layout_frames_preserve_rotated_ground_and_align_the_front(int east, int north)
    {
        OrientedRectangle rectangle = Rotated();
        Vector2 front = new(east, north);
        OrientedRectangle pad = LotGeometry.LayoutFrame(rectangle, front, frontAlongA: true);
        OrientedRectangle south = LotGeometry.LayoutFrame(rectangle, front, frontAlongA: false);
        OrientedRectangle yard = LotGeometry.LayoutFrame(rectangle, front, frontAlongA: false, eitherSide: true);
        Vector2 normal = LotGeometry.Direction(rectangle, front);
        foreach (OrientedRectangle frame in new[] { pad, south, yard })
        {
            Assert.Equal(rectangle.Bounds, frame.Bounds);
            Assert.Equal(rectangle.Center, frame.Center);
            Assert.True(rectangle.Contains(frame));
            Assert.True(frame.Contains(rectangle));
        }
        Assert.InRange(Math.Abs(Vector2.Dot(normal, LotGeometry.Direction(pad, Vector2.UnitX))), .999f, 1.001f);
        Assert.InRange(Vector2.Dot(normal, LotGeometry.Direction(south, -Vector2.UnitY)), .999f, 1.001f);
        Assert.InRange(Math.Abs(Vector2.Dot(normal, LotGeometry.Direction(yard, Vector2.UnitY))), .999f, 1.001f);
        Assert.Equal(rectangle, LotGeometry.LayoutFrame(rectangle, -Vector2.UnitY, frontAlongA: false));
    }

    [Fact]
    public void Included_near_corner_at_the_world_maximum_keeps_its_hash_bucket()
    {
        var rectangle = new OrientedRectangle(Fixed.FromInt(64), Fixed.FromInt(64), -Fixed.One, 0, 4, 4);
        Assert.Equal(new LandRectangle(60, 60, 4, 4), rectangle.Bounds);
        Assert.True(rectangle.Contains(Fixed.FromInt(64), Fixed.FromInt(64)));
        var buckets = LotGeometry.Buckets(rectangle, 64);
        Assert.Equal((0, 0, 1, 1), buckets);
    }

    private static OrientedRectangle Rotated() => new(Fixed.FromInt(100), Fixed.FromInt(100),
        ToQ16(MathF.Cos(MathF.PI / 6)), ToQ16(MathF.Sin(MathF.PI / 6)), 8, 6);

    private static int ToQ16(float tiles) => (int)MathF.Round(tiles * Fixed.One);
}
