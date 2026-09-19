using Borough.Core.Arithmetic;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Core.Entities;

public sealed partial class World
{
    public LandRectangle LotGround(int lot) => Lots.Rows.IsLive(lot)
        ? new(Lots.ParcelEast[lot].Raw, Lots.ParcelNorth[lot].Raw, Lots.ParcelWide[lot].Raw, Lots.ParcelDeep[lot].Raw)
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
        PermissionRefusal refusal = LandPermissions.Check(LotGround(lot), uses,
            (ushort)IntegerMath.ShiftLeft(1, form), out byte band);
        if (refusal != PermissionRefusal.None) { return refusal; }
        // Uniform band admission is checked with the same any-use semantics as ground permission.
        return LandPermissions.Check(LotGround(lot), (ushort)(uses & Rules.Band(band).Admits),
            (ushort)IntegerMath.ShiftLeft(1, form), out _) == PermissionRefusal.None
            ? PermissionRefusal.None : PermissionRefusal.Intensity;
    }

    public PermissionRefusal PaintPermissions(LandRectangle area, GroundPermissions permission) =>
        FinishPaint(area, LandPermissions.Paint(area, permission, Rules.PermissionRecordLimit));

    public PermissionRefusal PaintUsePermissions(LandRectangle area, ushort uses) =>
        FinishPaint(area, LandPermissions.PaintUses(area, uses, Rules.PermissionRecordLimit));

    public PermissionRefusal PaintBandPermissions(LandRectangle area, byte band) =>
        FinishPaint(area, LandPermissions.PaintBand(area, band, Rules.PermissionRecordLimit));

    public PermissionRefusal PaintFormPermissions(LandRectangle area, bool restricted, ushort forms) =>
        FinishPaint(area, LandPermissions.PaintForms(area, restricted, forms, Rules.PermissionRecordLimit));

    private PermissionRefusal FinishPaint(LandRectangle area, PermissionRefusal refusal)
    {
        if (refusal == PermissionRefusal.None) { RefreshPermissionSummaries(area); }
        return refusal;
    }

    internal void RefreshBlockPermissions(int slot)
    {
        if (slot == Rows.NoSlot) { return; }
        LandPermissionSummary summary = LandPermissions.Summary(BlockGroundRectangle(Blocks.LatticeColumn[slot], Blocks.LatticeRow[slot]));
        Blocks.Zone[slot] = summary.AnyUses;
        Blocks.Band[slot] = summary.MixedIntensity ? (byte)0 : summary.Band;
    }

    internal void RefreshPermissionSummaries(LandRectangle changed = default)
    {
        for (int row = 0; row < Lots.Rows.SlotCount; row++)
        {
            if (!Lots.Rows.IsLive(row)) { continue; }
            LandRectangle ground = LotGround(row);
            if (changed.IsValid && !Overlaps(changed, ground)) { continue; }
            Lots.Zone[row] = LandPermissions.Summary(ground).CommonUses;
        }
        for (int row = 0; row < Blocks.Rows.SlotCount; row++)
        {
            if (!Blocks.Rows.IsLive(row)) { continue; }
            if (changed.IsValid && !Overlaps(changed, BlockGroundRectangle(Blocks.LatticeColumn[row], Blocks.LatticeRow[row]))) { continue; }
            RefreshBlockPermissions(row);
        }
        LotsAdmitting.Invalidate();
    }

    internal static bool Overlaps(LandRectangle a, LandRectangle b) => a.IsValid && b.IsValid
        && a.X < b.X + b.Width && b.X < a.X + a.Width && a.Y < b.Y + b.Height && b.Y < a.Y + a.Height;
}
