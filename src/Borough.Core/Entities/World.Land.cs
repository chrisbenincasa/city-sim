using Borough.Core.Arithmetic;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Core.Entities;

public sealed partial class World
{
    public LandRectangle LotGround(int lot) => Lots.Rows.IsLive(lot)
        ? Lots.ParcelBounds(lot)
        : default;

    public LandRectangle BlockGroundRectangle(int column, int row)
    {
        if (column < 0 || row < 0 || column >= Roads.Streets.Blocks || row >= Roads.Streets.Blocks) { return default; }
        BlockGround g = BlockGround.At(Roads.Lattice, column, row);
        return new(g.East, g.North, g.Wide, g.Deep);
    }

    public PermissionRefusal ConstructionPermission(int lot, ushort uses)
    {
        int form = Lots.Pattern[lot] == 0 ? 0 : Lots.Pattern[lot] - 1;
        if (form >= BlockPatterns.Count) { return PermissionRefusal.InvalidForm; }
        PermissionRefusal refusal = LandPermissions.CheckParcel(Lots.Parcel(lot), uses,
            (ushort)IntegerMath.ShiftLeft(1, form), out byte band);
        if (refusal != PermissionRefusal.None) { return refusal; }
        // Uniform band admission is checked with the same any-use semantics as ground permission.
        return LandPermissions.CheckParcel(Lots.Parcel(lot), (ushort)(uses & Rules.Band(band).Admits),
            (ushort)IntegerMath.ShiftLeft(1, form), out _) == PermissionRefusal.None
            ? PermissionRefusal.None : PermissionRefusal.Intensity;
    }

    public PermissionRefusal PaintPermissions(LandRectangle area, GroundPermissions permission) =>
        FinishPaint(area, LandPermissions.Paint(area, permission, Rules.PermissionRecordLimit));

    public PermissionRefusal PaintUsePermissions(LandRectangle area, ushort uses) =>
        FinishPaint(area, LandPermissions.PaintUses(area, uses, Rules.PermissionRecordLimit));

    public PermissionRefusal PaintUsePermissions(ZoneGround area, ushort uses) =>
        FinishPaint(area.Bounds, LandPermissions.PaintUses(area, uses, Rules.PermissionRecordLimit));

    /// <summary>The paint on a live Lot's own Tiles.</summary>
    public LandPermissionSummary LotPermissions(int lot) => Lots.Rows.IsLive(lot)
        ? LandPermissions.ParcelSummary(Lots.Parcel(lot))
        : default;

    /// <summary>
    /// The ground a <c>Zone</c> command at this Tile paints: the smallest closed face holding the
    /// Tile's center, else the nearest Street side within one plot depth of it.
    /// </summary>
    /// <returns>False when no closed face holds the Tile and no Street side reaches it.</returns>
    public bool TryZoneGround(Tiles east, Tiles north, out ZoneGround ground)
    {
        ground = default;
        if (east.Raw < 0 || north.Raw < 0 || east.Raw >= CellGrid.WorldTiles || north.Raw >= CellGrid.WorldTiles) { return false; }
        long x = Fixed.FromInt(east.Raw) + OrientedTiles.HalfTile, y = Fixed.FromInt(north.Raw) + OrientedTiles.HalfTile;
        int face = Roads.Faces.Find(x, y);
        if (face >= 0)
        {
            ground = ZoneGround.OfFace(Roads.Faces, face);
            return true;
        }

        int reachTiles = LotSubdivider.SideReachTiles(this);
        if (reachTiles <= 0) { return false; }
        int reach = Fixed.FromInt(reachTiles);
        int best = Rows.NoSlot, bestDistance = int.MaxValue;
        StreetSide bestSide = StreetSide.Left;
        foreach (int segment in Roads.Residency.Near(east, north, new Tiles(reachTiles + 1)))
        {
            if ((RoadKind)Roads.Segments.Kind[segment] != RoadKind.Street) { continue; }
            if (!ZoneGround.SideOf(Roads.Segments.Centerline[segment], x, y, reach, out StreetSide side, out int distance)) { continue; }
            if (distance < bestDistance || (distance == bestDistance
                && Roads.Segments.Rows.IdAt(segment) < Roads.Segments.Rows.IdAt(best)))
            {
                (best, bestDistance, bestSide) = (segment, distance, side);
            }
        }
        if (best == Rows.NoSlot) { return false; }

        CellRect box = Roads.Residency.BoxOf(best);
        int west = Clip(box.East.Raw * CellGrid.TilesPerCell - reachTiles - 1);
        int south = Clip(box.North.Raw * CellGrid.TilesPerCell - reachTiles - 1);
        int eastEnd = Clip(box.EastEnd.Raw * CellGrid.TilesPerCell + reachTiles + 1);
        int northEnd = Clip(box.NorthEnd.Raw * CellGrid.TilesPerCell + reachTiles + 1);
        ground = ZoneGround.OfSide(best, Roads.Segments.Centerline[best], bestSide, reach,
            new LandRectangle(west, south, eastEnd - west, northEnd - south));
        return true;

        static int Clip(int tile) => tile < 0 ? 0 : tile > CellGrid.WorldTiles ? CellGrid.WorldTiles : tile;
    }

    public PermissionRefusal PaintBandPermissions(LandRectangle area, byte band) =>
        FinishPaint(area, LandPermissions.PaintBand(area, band, Rules.PermissionRecordLimit));

    public PermissionRefusal PaintFormPermissions(LandRectangle area, bool restricted, ushort forms) =>
        FinishPaint(area, LandPermissions.PaintForms(area, restricted, forms, Rules.PermissionRecordLimit));

    private PermissionRefusal FinishPaint(LandRectangle area, PermissionRefusal refusal)
    {
        if (refusal == PermissionRefusal.None) { RefreshPermissionSummaries(area); }
        return refusal;
    }

    internal void RefreshPermissionSummaries(LandRectangle changed = default)
    {
        for (int row = 0; row < Lots.Rows.SlotCount; row++)
        {
            if (!Lots.Rows.IsLive(row)) { continue; }
            if (changed.IsValid && !Overlaps(changed, Lots.Parcel(row))) { continue; }
            Lots.Zone[row] = LandPermissions.ParcelSummary(Lots.Parcel(row)).CommonUses;
        }
        LotsAdmitting.Invalidate();
    }

    internal static bool Overlaps(LandRectangle a, LandRectangle b) => a.IsValid && b.IsValid
        && OrientedRectangle.FromBounds(a).Overlaps(OrientedRectangle.FromBounds(b));

    internal static bool Overlaps(LandRectangle a, OrientedRectangle b) => a.IsValid
        && OrientedRectangle.FromBounds(a).Overlaps(b);
}
