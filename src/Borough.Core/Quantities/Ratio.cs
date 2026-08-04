using Borough.Core.Arithmetic;

namespace Borough.Core.Quantities;

/// <summary>
/// A dimensionless Q16.16 ratio. <b>The only quantity that may be multiplied by another
/// fixed-point value.</b>
/// </summary>
/// <remarks>
/// <para>
/// adr/0003 permits <c>fixed × fixed</c> only on dimensionless ratios, and this type is how that
/// permission is expressed. Everything absolute — <see cref="Money"/>, <see cref="Tiles"/>,
/// <see cref="Ticks"/>, <see cref="SubTiles"/> — refuses to multiply by its own kind, so the rule
/// holds structurally rather than by review.
/// </para>
/// <para>
/// <b>Why this is not the same type as <see cref="SubTiles"/>, which it shares a representation
/// with.</b> They differ in what may be done to them, and that difference is the entire reason either
/// exists: <c>Ratio × Ratio</c> is legal and <c>SubTiles × SubTiles</c> is not. Merging them to save
/// a file would delete the distinction the slice was built to enforce. Recorded here because the
/// duplication is the first thing a later reader will want to remove.
/// </para>
/// </remarks>
public readonly record struct Ratio(int Raw) : IComparable<Ratio>
{
    /// <summary>Zero.</summary>
    public static Ratio Zero => new(0);

    /// <summary>Unity — the multiplicative identity.</summary>
    public static Ratio One => new(Fixed.One);

    /// <summary>Builds a ratio from a numerator and denominator, rounding toward negative infinity.</summary>
    public static Ratio FromFraction(int numerator, int denominator) =>
        new(Fixed.Div(Fixed.FromInt(numerator), Fixed.FromInt(denominator)));

    /// <summary>Truncates toward negative infinity to a whole number.</summary>
    public int ToIntFloor() => Fixed.ToIntFloor(Raw);

    /// <summary>
    /// Asserts this ratio lies within the magnitude the call site claims for it, and returns it.
    /// </summary>
    public Ratio AssertMagnitude(int minWhole, int maxWhole) =>
        new(Fixed.AssertMagnitude(Raw, minWhole, maxWhole));

    /// <inheritdoc/>
    public int CompareTo(Ratio other) => Raw.CompareTo(other.Raw);

    public static Ratio operator +(Ratio left, Ratio right) => new(left.Raw + right.Raw);

    public static Ratio operator -(Ratio left, Ratio right) => new(left.Raw - right.Raw);

    public static Ratio operator -(Ratio value) => new(-value.Raw);

    /// <summary>The one legal <c>fixed × fixed</c> in the design.</summary>
    public static Ratio operator *(Ratio left, Ratio right) => new(Fixed.Mul(left.Raw, right.Raw));

    /// <summary>The one legal <c>fixed ÷ fixed</c> in the design.</summary>
    public static Ratio operator /(Ratio left, Ratio right) => new(Fixed.Div(left.Raw, right.Raw));

    public static bool operator <(Ratio left, Ratio right) => left.Raw < right.Raw;

    public static bool operator >(Ratio left, Ratio right) => left.Raw > right.Raw;

    public static bool operator <=(Ratio left, Ratio right) => left.Raw <= right.Raw;

    public static bool operator >=(Ratio left, Ratio right) => left.Raw >= right.Raw;
}
