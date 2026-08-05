using Borough.Core.Arithmetic;

namespace Borough.Core.Quantities;

/// <summary>
/// Whole-Tile distance or extent, as a signed 32-bit count. Space in the core is Tiles and nothing else.
/// </summary>
/// <remarks>
/// The map is 4096² Tiles at roughly 4 m each, so i32 carries six orders of magnitude of headroom
/// over any distance the world can hold. Signed because a difference of two coordinates is a
/// direction as well as a length.
/// <para>
/// <b><see cref="Tiles"/> times <see cref="Tiles"/> does not compile, and that is the point of the
/// type.</b> adr/0003's rule is that fixed-point multiplication operates on dimensionless ratios and
/// never on absolute quantities; an area is not a quantity this simulation has any use for, and a
/// call site that wants one is almost always a call site that meant to scale. Making the rule
/// structural is what stops it depending on discipline — a convention needs a type or it needs a
/// lint, and it never survives on either being remembered.
/// </para>
/// </remarks>
public readonly record struct Tiles(int Raw) : IComparable<Tiles>
{
    /// <summary>No distance.</summary>
    public static Tiles Zero => new(0);

    /// <summary>Distance ignoring direction.</summary>
    public Tiles Magnitude => new(IntegerMath.Abs(Raw));

    /// <inheritdoc/>
    public int CompareTo(Tiles other) => Raw.CompareTo(other.Raw);

    public static Tiles operator +(Tiles left, Tiles right) => new(left.Raw + right.Raw);

    public static Tiles operator -(Tiles left, Tiles right) => new(left.Raw - right.Raw);

    public static Tiles operator -(Tiles value) => new(-value.Raw);

    /// <summary>Scales by a whole count. Deliberately not defined for <see cref="Tiles"/>.</summary>
    public static Tiles operator *(Tiles value, int count) => new(value.Raw * count);

    /// <inheritdoc cref="op_Multiply(Tiles,int)"/>
    public static Tiles operator *(int count, Tiles value) => value * count;

    public static bool operator <(Tiles left, Tiles right) => left.Raw < right.Raw;

    public static bool operator >(Tiles left, Tiles right) => left.Raw > right.Raw;

    public static bool operator <=(Tiles left, Tiles right) => left.Raw <= right.Raw;

    public static bool operator >=(Tiles left, Tiles right) => left.Raw >= right.Raw;
}
