using Q16 = Borough.Core.Arithmetic.Fixed;
using Borough.Appearance;
using Borough.Core.Arithmetic;
using Borough.Core.Space;
using Godot;
using Vec2 = System.Numerics.Vector2;

namespace Borough.Shell;

public partial class Main
{
    private static Basis RectangleBasis(OrientedRectangle rectangle)
    {
        var a = new Vector3(rectangle.AxisEastQ16, 0f, -rectangle.AxisNorthQ16).Normalized();
        return new Basis(a, Vector3.Up, new Vector3(-a.Z, 0f, a.X));
    }

    private static Vector3 RectanglePoint(OrientedRectangle rectangle, float a, float up, float b)
    {
        Vec2 point = LotGeometry.Point(rectangle, new Vec2(a, b));
        return At(point.X, up, point.Y);
    }

    private OrientedRectangle DrawingRectangle(int lot, bool parcel, bool trade)
    {
        OrientedRectangle rectangle = parcel ? _world.Lots.Parcel(lot) : _world.Lots.Footprint(lot);
        if (!trade) return rectangle;
        BlockPattern pattern = _world.Lots.PatternOf(lot);
        return LotGeometry.LayoutFrame(rectangle, LotGeometry.Front(_world, lot),
            frontAlongA: pattern == BlockPattern.PadSite, eitherSide: pattern == BlockPattern.SalesYard);
    }

    // Virtual corners keep lattice arithmetic unchanged; positions rotate at the placement boundary.
    private LandRectangle FootprintFrame(int lot, bool trade = false)
    {
        OrientedRectangle foot = DrawingRectangle(lot, parcel: false, trade);
        return new(Q16.ToIntFloor(foot.EastQ16), Q16.ToIntFloor(foot.NorthQ16), foot.Wide, foot.Deep);
    }

    private LandRectangle ParcelFrame(int lot, bool trade = false)
    {
        OrientedRectangle foot = DrawingRectangle(lot, parcel: false, trade), parcel = DrawingRectangle(lot, parcel: true, trade);
        LandRectangle anchor = FootprintFrame(lot, trade);
        Vec2 offset = LotGeometry.Local(foot, LotGeometry.Point(parcel, Vec2.Zero));
        return new(anchor.X + Mathf.RoundToInt(offset.X), anchor.Y + Mathf.RoundToInt(offset.Y), parcel.Wide, parcel.Deep);
    }

    private Transform3D OnTradeLot(int lot, Transform3D local) => OnLot(lot, local, trade: true);

    private Transform3D OnLot(int lot, Transform3D local, bool trade = false)
    {
        OrientedRectangle foot = DrawingRectangle(lot, parcel: false, trade);
        LandRectangle anchor = FootprintFrame(lot, trade);
        if (foot.AxisEastQ16 == Q16.One && foot.AxisNorthQ16 == 0
            && foot.EastQ16 == Q16.FromInt(anchor.X) && foot.NorthQ16 == Q16.FromInt(anchor.Y)) return local;
        return new(RectangleBasis(foot) * local.Basis,
            RectanglePoint(foot, local.Origin.X / MetresPerTile - anchor.X, local.Origin.Y,
                -local.Origin.Z / MetresPerTile - anchor.Y));
    }

    private (Parcel Parcel, BlockGround Ground) TradeGround(int lot)
    {
        LandRectangle bounds = ParcelFrame(lot, trade: true);
        OrientedRectangle saved = _world.Lots.Parcel(lot);
        Vec2 worldFront = LotGeometry.Direction(saved, LotGeometry.Front(_world, lot));
        OrientedRectangle frame = DrawingRectangle(lot, parcel: true, trade: true);
        Vec2 a = LotGeometry.Direction(frame, Vec2.UnitX), b = LotGeometry.Direction(frame, Vec2.UnitY);
        Vec2 front = new(Vec2.Dot(worldFront, a), Vec2.Dot(worldFront, b));
        BlockFace face = front.X < -.5f ? BlockFace.West : front.X > .5f ? BlockFace.East
            : front.Y < 0 ? BlockFace.South : BlockFace.North;
        var parcel = new Parcel(face, (StreetSide)_world.Lots.Side[lot], default, OrientedRectangle.FromBounds(bounds));
        if (saved.AxisEastQ16 == Q16.One && saved.AxisNorthQ16 == 0
            && DrawingRectangle(lot, parcel: true, trade: true) == saved)
            return (parcel, _world.ParcelGround(lot).Ground);
        return (parcel, LotGeometry.TradeGround(bounds));
    }
}
