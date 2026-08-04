using Borough.Core.Arithmetic;

namespace Borough.Core.Quantities;

/// <summary>
/// A sub-Tile position, as Q16.16 Tiles. <b>Positions, and only positions.</b>
/// </summary>
/// <remarks>
/// <para>
/// adr/0003 confines Q16.16 to positions because that is the one place the design genuinely needs a
/// fraction of a Tile — a Vehicle part-way along a Lane. The headroom is provable rather than
/// estimated: ±32,768 against a 4096-Tile map is <b>8× margin, forever</b>, because the map is a
/// world-creation constant and cannot grow under a running save.
/// </para>
/// <para>
/// <b><see cref="SubTiles"/> times <see cref="SubTiles"/> does not compile.</b> The stated use is
/// <c>position += velocity × ticks</c>, which is fixed × <em>integer</em>. Scaling by a
/// <see cref="Ratio"/> is also legal, because a ratio is dimensionless and the product is still a
/// position. Multiplying two positions is not an operation this design has, and admitting it would
/// admit the overflow case the ±32,768 bound was chosen to exclude.
/// </para>
/// </remarks>
public readonly record struct SubTiles(int Raw) : IComparable<SubTiles>
{
    /// <summary>The origin.</summary>
    public static SubTiles Zero => new(0);

    /// <summary>Lifts a whole-Tile distance to sub-Tile precision.</summary>
    /// <exception cref="OverflowException">The distance exceeds ±32,768 Tiles.</exception>
    public static SubTiles FromTiles(Tiles tiles) => new(Fixed.FromInt(tiles.Raw));

    /// <summary>Drops to the containing whole Tile, rounding toward negative infinity.</summary>
    public Tiles ToTilesFloor() => new(Fixed.ToIntFloor(Raw));

    /// <inheritdoc/>
    public int CompareTo(SubTiles other) => Raw.CompareTo(other.Raw);

    public static SubTiles operator +(SubTiles left, SubTiles right) => new(left.Raw + right.Raw);

    public static SubTiles operator -(SubTiles left, SubTiles right) => new(left.Raw - right.Raw);

    public static SubTiles operator -(SubTiles value) => new(-value.Raw);

    /// <summary>Scales by a whole count — the <c>velocity × ticks</c> case.</summary>
    public static SubTiles operator *(SubTiles value, int count) => new(value.Raw * count);

    /// <inheritdoc cref="op_Multiply(SubTiles,int)"/>
    public static SubTiles operator *(int count, SubTiles value) => value * count;

    /// <summary>Scales by a dimensionless ratio. The product is still a position.</summary>
    public static SubTiles operator *(SubTiles value, Ratio ratio) => new(Fixed.Mul(value.Raw, ratio.Raw));

    /// <inheritdoc cref="op_Multiply(SubTiles,Ratio)"/>
    public static SubTiles operator *(Ratio ratio, SubTiles value) => value * ratio;

    public static bool operator <(SubTiles left, SubTiles right) => left.Raw < right.Raw;

    public static bool operator >(SubTiles left, SubTiles right) => left.Raw > right.Raw;

    public static bool operator <=(SubTiles left, SubTiles right) => left.Raw <= right.Raw;

    public static bool operator >=(SubTiles left, SubTiles right) => left.Raw >= right.Raw;
}
