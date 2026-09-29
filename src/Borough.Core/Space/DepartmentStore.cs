namespace Borough.Core.Space;

/// <summary>
/// The Unit layout of a <see cref="BlockPattern.HighStreetBlock"/>'s department store: an anchor Unit
/// through the middle of the south face, and a corner Unit at each end. Every Unit runs the full depth
/// and every storey.
/// </summary>
/// <remarks>
/// The layout is fixed and uses no draw. The corner width is a shape constant of the form.
/// </remarks>
public static class DepartmentStore
{
    /// <summary>How wide each corner Unit is, in Tiles (8 m).</summary>
    public const int CornerTiles = 2;

    /// <summary>The most Units a store holds.</summary>
    public const int MaxUnits = 3;

    /// <summary>One Unit's span along the store, in Tiles east of its west wall.</summary>
    public readonly record struct Bay(int East, int Wide, bool Anchor);

    /// <summary>
    /// Writes the store's Units west to east and returns how many. A store too narrow for two corners
    /// and an anchor between them is one anchor Unit.
    /// </summary>
    public static int Units(int wide, Span<Bay> into)
    {
        if (wide <= 0)
        {
            return 0;
        }

        if (wide <= 2 * CornerTiles)
        {
            into[0] = new Bay(0, wide, Anchor: true);
            return 1;
        }

        into[0] = new Bay(0, CornerTiles, Anchor: false);
        into[1] = new Bay(CornerTiles, wide - 2 * CornerTiles, Anchor: true);
        into[2] = new Bay(wide - CornerTiles, CornerTiles, Anchor: false);
        return MaxUnits;
    }
}
