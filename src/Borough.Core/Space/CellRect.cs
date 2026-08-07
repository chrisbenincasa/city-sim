namespace Borough.Core.Space;

/// <summary>
/// A half-open rectangle of Cells. The unit a diffusion pass and a Layer query are both scoped by.
/// </summary>
/// <remarks>
/// <b>Half-open, so that dilating and clamping compose without an off-by-one at every call site.</b>
/// The incremental scheme dilates a dirty region by the kernel radius twice and clamps the result to
/// the map; an inclusive rectangle makes each of those a separate ±1 decision, and the failure mode is
/// a halo one Cell short, which is a field that is <em>almost</em> bit-identical to a full recompute.
/// That is precisely the failure <c>plans/0009</c> refuses to accept: <em>not close; identical.</em>
/// </remarks>
public readonly record struct CellRect
{
    /// <param name="east">The low east edge, inclusive.</param>
    /// <param name="north">The low north edge, inclusive.</param>
    /// <param name="width">Extent along east. Zero is an empty rectangle.</param>
    /// <param name="height">Extent along north. Zero is an empty rectangle.</param>
    public CellRect(Cells east, Cells north, Cells width, Cells height)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(width.Raw, nameof(width));
        ArgumentOutOfRangeException.ThrowIfNegative(height.Raw, nameof(height));

        East = east;
        North = north;
        Width = width;
        Height = height;
    }

    /// <summary>The whole map.</summary>
    public static CellRect World { get; } = new(
        Cells.Zero, Cells.Zero, new Cells(CellGrid.WorldCells), new Cells(CellGrid.WorldCells));

    /// <summary>Nothing. What an empty dirty set dilates to.</summary>
    public static CellRect Empty => default;

    /// <summary>The low east edge, inclusive.</summary>
    public Cells East { get; }

    /// <summary>The low north edge, inclusive.</summary>
    public Cells North { get; }

    /// <summary>Extent along east.</summary>
    public Cells Width { get; }

    /// <summary>Extent along north.</summary>
    public Cells Height { get; }

    /// <summary>The high east edge, exclusive.</summary>
    public Cells EastEnd => East + Width;

    /// <summary>The high north edge, exclusive.</summary>
    public Cells NorthEnd => North + Height;

    /// <summary>Whether the rectangle covers no Cell at all.</summary>
    public bool IsEmpty => Width.Raw <= 0 || Height.Raw <= 0;

    /// <summary>How many Cells it covers.</summary>
    public int Count => IsEmpty ? 0 : Width.Raw * Height.Raw;

    /// <summary>The single Cell at a coordinate.</summary>
    public static CellRect At(Cells east, Cells north) => new(east, north, Cells.One, Cells.One);

    /// <summary>Grown by <paramref name="margin"/> on every side. The halo of the incremental scheme.</summary>
    public CellRect Dilate(Cells margin) =>
        IsEmpty
            ? Empty
            : new CellRect(
                East - margin,
                North - margin,
                Width + (margin * 2),
                Height + (margin * 2));

    /// <summary>Trimmed to the map. What a dilated halo becomes at the world edge.</summary>
    public CellRect Clamp()
    {
        int east = East.Raw < 0 ? 0 : East.Raw;
        int north = North.Raw < 0 ? 0 : North.Raw;
        int eastEnd = EastEnd.Raw > CellGrid.WorldCells ? CellGrid.WorldCells : EastEnd.Raw;
        int northEnd = NorthEnd.Raw > CellGrid.WorldCells ? CellGrid.WorldCells : NorthEnd.Raw;

        return eastEnd <= east || northEnd <= north
            ? Empty
            : new CellRect(
                new Cells(east), new Cells(north),
                new Cells(eastEnd - east), new Cells(northEnd - north));
    }

    /// <summary>The smallest rectangle covering both. How a dirty set accumulates.</summary>
    public CellRect Union(CellRect other)
    {
        if (IsEmpty)
        {
            return other;
        }

        if (other.IsEmpty)
        {
            return this;
        }

        int east = East.Raw < other.East.Raw ? East.Raw : other.East.Raw;
        int north = North.Raw < other.North.Raw ? North.Raw : other.North.Raw;
        int eastEnd = EastEnd.Raw > other.EastEnd.Raw ? EastEnd.Raw : other.EastEnd.Raw;
        int northEnd = NorthEnd.Raw > other.NorthEnd.Raw ? NorthEnd.Raw : other.NorthEnd.Raw;

        return new CellRect(
            new Cells(east), new Cells(north),
            new Cells(eastEnd - east), new Cells(northEnd - north));
    }

    /// <summary>Whether a Cell is inside.</summary>
    public bool Contains(Cells east, Cells north) =>
        east >= East && east < EastEnd && north >= North && north < NorthEnd;
}
