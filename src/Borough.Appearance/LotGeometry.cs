using System.Numerics;
using Borough.Core.Arithmetic;
using Borough.Core.Entities;
using Borough.Core.Space;

namespace Borough.Appearance;

/// <summary>Rectangle-local geometry for the drawing. Coordinates are in Tiles, with north positive.</summary>
public static class LotGeometry
{
    /// <summary>The rectangle edge facing the Street, as a local outward unit normal.</summary>
    public static Vector2 Front(World world, int lot)
    {
        Address address = world.Lots.AddressOf(lot);
        if (!address.Exists) return -Vector2.UnitY;
        var tangent = world.Roads.Segments.Centerline[address.Segment].TangentAt(Fixed.FromInt(address.Offset.Raw));
        return Front(world.Lots.Parcel(lot), tangent.East, tangent.North, address.Side);
    }

    public static Vector2 Front(OrientedRectangle rectangle, int tangentEastQ16, int tangentNorthQ16, StreetSide side)
    {
        long east = side == StreetSide.Left ? tangentNorthQ16 : -(long)tangentNorthQ16;
        long north = side == StreetSide.Left ? -(long)tangentEastQ16 : tangentEastQ16;
        long a = east * rectangle.AxisEastQ16 + north * rectangle.AxisNorthQ16;
        long b = -east * rectangle.AxisNorthQ16 + north * rectangle.AxisEastQ16;
        return Math.Abs(a) > Math.Abs(b)
            ? new Vector2(a < 0 ? -1 : 1, 0)
            : new Vector2(0, b < 0 ? -1 : 1);
    }

    /// <summary>Candidate buckets include the maximum boundary, which can be an included near edge on a rotated axis.</summary>
    public static (int FirstEast, int FirstNorth, int LastEast, int LastNorth) Buckets(OrientedRectangle rectangle, int squareTiles)
    {
        LandRectangle bounds = rectangle.Bounds;
        return (IntegerMath.FloorDiv(bounds.X, squareTiles), IntegerMath.FloorDiv(bounds.Y, squareTiles),
            IntegerMath.FloorDiv(bounds.X + bounds.Width, squareTiles), IntegerMath.FloorDiv(bounds.Y + bounds.Height, squareTiles));
    }

    /// <summary>Re-expresses a trade layout on the same ground, with frontage along its expected local axis.</summary>
    public static OrientedRectangle LayoutFrame(OrientedRectangle rectangle, Vector2 front, bool frontAlongA, bool eitherSide = false)
    {
        for (int turn = 0; turn < 4; turn++)
        {
            if (frontAlongA ? front.X != 0 : front.Y != 0 && (eitherSide || front.Y < 0)) return rectangle;
            rectangle = new OrientedRectangle(
                rectangle.EastQ16 + rectangle.Wide * rectangle.AxisEastQ16,
                rectangle.NorthQ16 + rectangle.Wide * rectangle.AxisNorthQ16,
                -rectangle.AxisNorthQ16, rectangle.AxisEastQ16, rectangle.Deep, rectangle.Wide);
            front = new Vector2(front.Y, -front.X);
        }
        return rectangle;
    }

    public static Vector2 Direction(OrientedRectangle rectangle, Vector2 local) => new(
        (rectangle.AxisEastQ16 * local.X - rectangle.AxisNorthQ16 * local.Y) / Fixed.One,
        (rectangle.AxisNorthQ16 * local.X + rectangle.AxisEastQ16 * local.Y) / Fixed.One);

    public static Vector2 Point(OrientedRectangle rectangle, Vector2 local) =>
        new Vector2(rectangle.EastQ16 / (float)Fixed.One, rectangle.NorthQ16 / (float)Fixed.One)
        + Direction(rectangle, local);

    public static Vector2 Local(OrientedRectangle rectangle, Vector2 point)
    {
        Vector2 relative = point - new Vector2(rectangle.EastQ16 / (float)Fixed.One, rectangle.NorthQ16 / (float)Fixed.One);
        var axis = new Vector2(rectangle.AxisEastQ16 / (float)Fixed.One, rectangle.AxisNorthQ16 / (float)Fixed.One);
        return new(Vector2.Dot(relative, axis) / axis.LengthSquared(),
            Vector2.Dot(relative, new Vector2(-axis.Y, axis.X)) / axis.LengthSquared());
    }

    /// <summary>The garden depth behind the footprint along its frontage normal.</summary>
    public static float BackSpace(OrientedRectangle parcel, OrientedRectangle footprint, Vector2 front)
    {
        Vector2 corner = Local(parcel, Point(footprint, Vector2.Zero));
        if (front.X < 0) return parcel.Wide - corner.X - footprint.Wide;
        if (front.X > 0) return corner.X;
        return front.Y < 0 ? parcel.Deep - corner.Y - footprint.Deep : corner.Y;
    }

    /// <summary>Whether the neighbour spans the same depth interval, projected on the own rectangle's axis.</summary>
    public static bool SameDepth(OrientedRectangle own, OrientedRectangle other, bool alongA)
    {
        int east = alongA ? -own.AxisNorthQ16 : own.AxisEastQ16;
        int north = alongA ? own.AxisEastQ16 : own.AxisNorthQ16;
        return Span(own, east, north) == Span(other, east, north);
    }

    private static (long Low, long High) Span(OrientedRectangle rectangle, int east, int north)
    {
        long corner = (long)rectangle.EastQ16 * east + (long)rectangle.NorthQ16 * north;
        long a = ((long)rectangle.AxisEastQ16 * east + (long)rectangle.AxisNorthQ16 * north) * rectangle.Wide;
        long b = (-(long)rectangle.AxisNorthQ16 * east + (long)rectangle.AxisEastQ16 * north) * rectangle.Deep;
        return (corner + Math.Min(0, a) + Math.Min(0, b), corner + Math.Max(0, a) + Math.Max(0, b));
    }
}
