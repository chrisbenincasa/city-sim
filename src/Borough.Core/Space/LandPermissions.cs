using Borough.Core.Arithmetic;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>Half-open bounds in integer Tiles.</summary>
public readonly record struct LandRectangle(int X, int Y, int Width, int Height)
{
    public bool IsValid => X >= 0 && Y >= 0 && Width > 0 && Height > 0
        && Width <= CellGrid.WorldTiles - X && Height <= CellGrid.WorldTiles - Y;
}

/// <summary>Future use, intensity band and an optional form restriction. Zero means unzoned.</summary>
public readonly record struct GroundPermissions(ushort Uses, byte Band, bool RestrictsForms = false, ushort Forms = 0)
{
    internal ulong Packed => Uses | ((ulong)Band << 16)
        | (RestrictsForms ? (1UL << 24) | ((ulong)Forms << 25) : 0);

    public static GroundPermissions Unpack(ulong value) => new(
        (ushort)value, (byte)(value >> 16), (value & (1UL << 24)) != 0, (ushort)(value >> 25));
}

public enum PermissionRefusal : byte
{
    None,
    InvalidBounds,
    RecordLimit,
    Unzoned,
    Use,
    Form,
    MixedIntensity,
    InvalidForm,
    Intensity,
}

/// <summary>
/// Exact Tile painting with bounded Cell scratch and count-before-stage preflight. Call on the
/// simulation's single writer; successful painting is published only after the full commit.
/// </summary>
public sealed class LandPermissions
{
    private const int Side = CellGrid.TilesPerCell;
    private const int PageTiles = CellGrid.TilesInCell;
    private const int PagesAcross = CellGrid.WorldCells;
    private readonly LandPermissionTable _table;
    private readonly int[] _heads = new int[PagesAcross * PagesAcross];

    public LandPermissions(LandPermissionTable table)
    {
        ArgumentNullException.ThrowIfNull(table);
        _table = table;
    }

    public GroundPermissions At(int x, int y)
    {
        if ((uint)x >= CellGrid.WorldTiles || (uint)y >= CellGrid.WorldTiles)
        {
            return default;
        }

        for (int link = _heads[Page(x, y)]; link != 0; link = _table.Next[link - 1])
        {
            int row = link - 1;
            if (x >= _table.X[row] && x < _table.X[row] + _table.Width[row]
                && y >= _table.Y[row] && y < _table.Y[row] + _table.Height[row])
            {
                return GroundPermissions.Unpack(_table.Permission[row]);
            }
        }

        return default;
    }

    /// <summary>Checks all ground, including open ground in a proposed site. Uses retain any-bit admission.</summary>
    public PermissionRefusal Check(LandRectangle site, ushort uses, ushort form, out byte band)
    {
        band = 0;
        if (!site.IsValid) { return PermissionRefusal.InvalidBounds; }
        if (form == 0 || (form & (form - 1)) != 0) { return PermissionRefusal.InvalidForm; }
        int covered = 0;
        bool seen = false, wrongUse = false, wrongForm = false, mixed = false;
        for (int py = PageAxis(site.Y); py <= PageAxis(site.Y + site.Height - 1); py++)
        {
            for (int px = PageAxis(site.X); px <= PageAxis(site.X + site.Width - 1); px++)
            {
                for (int link = _heads[py * PagesAcross + px]; link != 0; link = _table.Next[link - 1])
                {
                    int row = link - 1;
                    int left = Max(site.X, _table.X[row]), top = Max(site.Y, _table.Y[row]);
                    int right = Min(site.X + site.Width, _table.X[row] + _table.Width[row]);
                    int bottom = Min(site.Y + site.Height, _table.Y[row] + _table.Height[row]);
                    if (right <= left || bottom <= top) { continue; }
                    GroundPermissions permission = GroundPermissions.Unpack(_table.Permission[row]);
                    if (permission.Uses == 0) { continue; }
                    covered += (right - left) * (bottom - top);
                    wrongUse |= (permission.Uses & uses) == 0;
                    wrongForm |= permission.RestrictsForms && (permission.Forms & form) == 0;
                    mixed |= seen && band != permission.Band;
                    if (!seen) { band = permission.Band; seen = true; }
                }
            }
        }

        if (covered != site.Width * site.Height) { return PermissionRefusal.Unzoned; }
        if (wrongUse) { return PermissionRefusal.Use; }
        if (wrongForm) { return PermissionRefusal.Form; }
        return mixed ? PermissionRefusal.MixedIntensity : PermissionRefusal.None;
    }

    public PermissionRefusal Paint(LandRectangle area, GroundPermissions permission, int recordLimit) =>
        Paint(area, permission, recordLimit, mask: ulong.MaxValue);

    /// <summary>Changes only form restrictions, including on otherwise unzoned ground.</summary>
    public PermissionRefusal PaintForms(LandRectangle area, bool restricted, ushort forms, int recordLimit) =>
        Paint(area, new GroundPermissions(0, 0, restricted, forms), recordLimit, mask: ~0xFFFFFFUL);

    public PermissionRefusal CanPaintUses(LandRectangle area, ushort uses, int recordLimit) =>
        Paint(area, new GroundPermissions(uses, 0), recordLimit, 0xFFFFUL, apply: false);

    public PermissionRefusal PaintUses(LandRectangle area, ushort uses, int recordLimit) =>
        Paint(area, new GroundPermissions(uses, 0), recordLimit, 0xFFFFUL);

    public PermissionRefusal PaintBand(LandRectangle area, byte band, int recordLimit) =>
        Paint(area, new GroundPermissions(0, band), recordLimit, 0xFF0000UL);

    /// <summary>Common uses are conservative; the union is for discovery/display only.</summary>
    public LandPermissionSummary Summary(LandRectangle site)
    {
        if (!site.IsValid) { return default; }
        int covered = 0;
        ushort common = ushort.MaxValue, any = 0;
        GroundPermissions first = default;
        bool seen = false, mixed = false, mixedBand = false;
        for (int py = PageAxis(site.Y); py <= PageAxis(site.Y + site.Height - 1); py++)
        {
            for (int px = PageAxis(site.X); px <= PageAxis(site.X + site.Width - 1); px++)
            {
                for (int link = _heads[py * PagesAcross + px]; link != 0; link = _table.Next[link - 1])
                {
                    int row = link - 1;
                    int left = Max(site.X, _table.X[row]), top = Max(site.Y, _table.Y[row]);
                    int right = Min(site.X + site.Width, _table.X[row] + _table.Width[row]);
                    int bottom = Min(site.Y + site.Height, _table.Y[row] + _table.Height[row]);
                    if (right <= left || bottom <= top) { continue; }
                    GroundPermissions permission = GroundPermissions.Unpack(_table.Permission[row]);
                    covered += (right - left) * (bottom - top);
                    common &= permission.Uses; any |= permission.Uses;
                    mixed |= seen && first != permission;
                    mixedBand |= seen && first.Band != permission.Band;
                    if (!seen) { first = permission; seen = true; }
                }
            }
        }
        if (covered != site.Width * site.Height)
        {
            common = 0;
            mixed |= seen;
            mixedBand |= seen && first.Band != 0;
        }
        return new(common, any, first.Band, mixedBand, mixed);
    }

    private PermissionRefusal Paint(LandRectangle area, GroundPermissions permission, int limit, ulong mask, bool apply = true)
    {
        if (!area.IsValid) { return PermissionRefusal.InvalidBounds; }
        if (limit < 8 || _table.Rows.SlotCount > limit) { return PermissionRefusal.RecordLimit; }
        int firstX = PageAxis(area.X), firstY = PageAxis(area.Y);
        int across = PageAxis(area.X + area.Width - 1) - firstX + 1;
        int down = PageAxis(area.Y + area.Height - 1) - firstY + 1;
        // One count per selected Cell. -1 distinguishes an unchanged page from a cleared page.
        int[] counts = new int[across * down];
        Span<ulong> tiles = stackalloc ulong[PageTiles];
        Span<Rectangle> rectangles = stackalloc Rectangle[PageTiles];
        int finalCount = _table.Rows.LiveCount, stagedCount = 0;
        for (int page = 0; page < counts.Length; page++)
        {
            int px = firstX + page % across, py = firstY + IntegerMath.FloorDiv(page, across);
            int oldCount = Decode(px, py, tiles);
            if (!Edit(px, py, tiles, area, permission, mask)) { counts[page] = -1; continue; }
            int count = Encode(px, py, tiles, rectangles);
            counts[page] = count;
            stagedCount += count;
            finalCount += count - oldCount;
        }

        // A later page's compaction may pay for an earlier page's growth.
        if (finalCount > limit) { return PermissionRefusal.RecordLimit; }
        if (!apply) { return PermissionRefusal.None; }
        if (stagedCount == 0 && finalCount == _table.Rows.LiveCount) { return PermissionRefusal.None; }
        var staged = new Rectangle[stagedCount];
        int offset = 0;
        for (int page = 0; page < counts.Length; page++)
        {
            if (counts[page] < 0) { continue; }
            int px = firstX + page % across, py = firstY + IntegerMath.FloorDiv(page, across);
            Decode(px, py, tiles);
            Edit(px, py, tiles, area, permission, mask);
            int count = Encode(px, py, tiles, rectangles);
            rectangles[..count].CopyTo(staged.AsSpan(offset));
            offset += count;
        }

        _table.Rows.PrepareCapacity(Max(_table.Rows.SlotCount, finalCount), limit);
        // Retire every changed page before allocating any replacement.
        for (int page = 0; page < counts.Length; page++)
        {
            if (counts[page] < 0) { continue; }
            int index = (firstY + IntegerMath.FloorDiv(page, across)) * PagesAcross + firstX + page % across;
            int link = _heads[index];
            while (link != 0)
            {
                int row = link - 1;
                link = _table.Next[row];
                _table.Rows.Free(_table.Rows.At(row));
            }
            _heads[index] = 0;
        }

        Span<int> slots = stackalloc int[PageTiles];
        offset = 0;
        for (int page = 0; page < counts.Length; page++)
        {
            int count = counts[page];
            if (count < 0) { continue; }
            int index = (firstY + IntegerMath.FloorDiv(page, across)) * PagesAcross + firstX + page % across;
            for (int r = 0; r < count; r++)
            {
                Rectangle rect = staged[offset++];
                int row = _table.Rows.Resolve(_table.Rows.Allocate());
                _table.X[row] = rect.X; _table.Y[row] = rect.Y;
                _table.Width[row] = rect.Width; _table.Height[row] = rect.Height;
                _table.Permission[row] = rect.Permission;
                slots[r] = row;
            }
            slots[..count].Sort();
            for (int r = count - 1; r >= 0; r--)
            {
                _table.Next[slots[r]] = _heads[index];
                _heads[index] = slots[r] + 1;
            }
        }
        return PermissionRefusal.None;
    }

    /// <summary>Reconstructs Cell lists in ascending slot order; validates saved geographic state.</summary>
    public void Rebuild()
    {
        Array.Clear(_heads);
        for (int row = _table.Rows.SlotCount - 1; row >= 0; row--)
        {
            _table.Next[row] = 0;
            if (!_table.Rows.IsLive(row)) { continue; }
            if (!ValidRectangle(row))
            {
                throw new InvalidOperationException("Invalid saved land permission rectangle.");
            }
            int page = Page(_table.X[row], _table.Y[row]);
            _table.Next[row] = _heads[page];
            _heads[page] = row + 1;
        }
        if (!IsValid(out _))
        {
            throw new InvalidOperationException("Overlapping saved land permissions.");
        }
    }

    /// <summary>Checks saved geometry and exact, disjoint index coverage without mutation.</summary>
    internal bool IsValid(out int invalidRow)
    {
        Span<byte> occupied = stackalloc byte[PageTiles];
        int indexed = 0;
        for (int page = 0; page < _heads.Length; page++)
        {
            if (_heads[page] == 0) { continue; }
            occupied.Clear();
            int previous = -1;
            for (int link = _heads[page]; link != 0; link = _table.Next[link - 1])
            {
                int row = link - 1;
                invalidRow = row;
                if (row <= previous || row >= _table.Rows.SlotCount || !_table.Rows.IsLive(row)
                    || !ValidRectangle(row) || Page(_table.X[row], _table.Y[row]) != page)
                {
                    return false;
                }
                previous = row;
                indexed++;
                int x = _table.X[row] % Side, y = _table.Y[row] % Side;
                for (int dy = y; dy < y + _table.Height[row]; dy++)
                {
                    for (int dx = x; dx < x + _table.Width[row]; dx++)
                    {
                        int at = dy * Side + dx;
                        if (occupied[at] != 0) { return false; }
                        occupied[at] = 1;
                    }
                }
            }
        }
        invalidRow = -1;
        return indexed == _table.Rows.LiveCount;
    }

    private bool ValidRectangle(int row)
    {
        var area = new LandRectangle(_table.X[row], _table.Y[row], _table.Width[row], _table.Height[row]);
        ulong packed = _table.Permission[row];
        return area.IsValid && Page(area.X, area.Y) == Page(area.X + area.Width - 1, area.Y + area.Height - 1)
            && packed != 0 && GroundPermissions.Unpack(packed).Packed == packed;
    }

    private int Decode(int px, int py, Span<ulong> tiles)
    {
        tiles.Clear();
        int count = 0;
        for (int link = _heads[py * PagesAcross + px]; link != 0; link = _table.Next[link - 1])
        {
            int row = link - 1;
            int x = _table.X[row] - px * Side, y = _table.Y[row] - py * Side;
            for (int dy = 0; dy < _table.Height[row]; dy++)
            {
                tiles.Slice((y + dy) * Side + x, _table.Width[row]).Fill(_table.Permission[row]);
            }
            count++;
        }
        return count;
    }

    private static bool Edit(int px, int py, Span<ulong> tiles, LandRectangle area, GroundPermissions permission, ulong mask)
    {
        bool changed = false;
        for (int y = Max(area.Y, py * Side); y < Min(area.Y + area.Height, (py + 1) * Side); y++)
        {
            for (int x = Max(area.X, px * Side); x < Min(area.X + area.Width, (px + 1) * Side); x++)
            {
                int at = (y - py * Side) * Side + x - px * Side;
                ulong value = permission.Packed;
                value = (tiles[at] & ~mask) | (value & mask);
                changed |= tiles[at] != value;
                tiles[at] = value;
            }
        }
        return changed;
    }

    private static int Encode(int px, int py, ReadOnlySpan<ulong> tiles, Span<Rectangle> output)
    {
        Span<int> previous = stackalloc int[Side];
        Span<int> current = stackalloc int[Side];
        previous.Fill(-1);
        int count = 0;
        for (int y = 0; y < Side; y++)
        {
            current.Fill(-1);
            for (int x = 0; x < Side;)
            {
                ulong value = tiles[y * Side + x];
                int end = x + 1;
                while (end < Side && tiles[y * Side + end] == value) { end++; }
                if (value != 0)
                {
                    int before = previous[x];
                    if (before >= 0 && output[before].Width == end - x && output[before].Permission == value)
                    {
                        output[before] = output[before] with { Height = output[before].Height + 1 };
                        current[x] = before;
                    }
                    else
                    {
                        output[count] = new Rectangle(px * Side + x, py * Side + y, end - x, 1, value);
                        current[x] = count++;
                    }
                }
                x = end;
            }
            current.CopyTo(previous);
        }
        return count;
    }

    private static int PageAxis(int tile) => IntegerMath.FloorDiv(tile, Side);
    private static int Page(int x, int y) => PageAxis(y) * PagesAcross + PageAxis(x);
    private static int Min(int a, int b) => a < b ? a : b;
    private static int Max(int a, int b) => a > b ? a : b;
    private readonly record struct Rectangle(int X, int Y, int Width, int Height, ulong Permission);
}

/// <summary>Spatial summary; only a complete Check can authorise construction.</summary>
public readonly record struct LandPermissionSummary(ushort CommonUses, ushort AnyUses, byte Band, bool MixedIntensity, bool MixedPermissions);
