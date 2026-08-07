using Borough.Core.Arithmetic;

namespace Borough.Core.Space;

/// <summary>
/// Whole-Cell distance or extent along one axis — the unit every Map Layer is stored in.
/// </summary>
/// <remarks>
/// <para>
/// <b>Distinct from <see cref="Chunks"/> in the type system, and that is the whole point of
/// adr/0034.</b> The two were one number carrying two decisions, and <em>a constant welded to two
/// decisions is governed by whichever of them is louder</em> — the performance role had a profiler
/// and the design role had nobody. A comment saying "these are different now" would be governed by
/// whoever read it last. This does not compile if they are confused.
/// </para>
/// <para>
/// Signed, for the same reason <see cref="Quantities.Tiles"/> is: a difference of two coordinates is
/// a direction as well as a length, and a kernel offset is routinely negative.
/// </para>
/// </remarks>
public readonly record struct Cells(int Raw) : IComparable<Cells>
{
    /// <summary>No extent.</summary>
    public static Cells Zero => new(0);

    /// <summary>One Cell.</summary>
    public static Cells One => new(1);

    /// <summary>Extent ignoring direction.</summary>
    public Cells Magnitude => new(IntegerMath.Abs(Raw));

    /// <inheritdoc/>
    public int CompareTo(Cells other) => Raw.CompareTo(other.Raw);

    public static Cells operator +(Cells left, Cells right) => new(left.Raw + right.Raw);

    public static Cells operator -(Cells left, Cells right) => new(left.Raw - right.Raw);

    public static Cells operator -(Cells value) => new(-value.Raw);

    /// <summary>Scales by a whole count. Deliberately not defined for <see cref="Cells"/>.</summary>
    public static Cells operator *(Cells value, int count) => new(value.Raw * count);

    /// <inheritdoc cref="op_Multiply(Cells,int)"/>
    public static Cells operator *(int count, Cells value) => value * count;

    public static bool operator <(Cells left, Cells right) => left.Raw < right.Raw;

    public static bool operator >(Cells left, Cells right) => left.Raw > right.Raw;

    public static bool operator <=(Cells left, Cells right) => left.Raw <= right.Raw;

    public static bool operator >=(Cells left, Cells right) => left.Raw >= right.Raw;
}

/// <summary>
/// Whole-Chunk distance or extent along one axis — the technical partition, never a Layer's storage.
/// </summary>
/// <remarks>
/// <b>Hash-preserving, and therefore a profiler's to move</b> (<c>05 §4</c>). Everything the Chunk
/// carries — dirty tracking, save serialisation, parallel work, aggregate caching, the pathfinding
/// cluster, render streaming — leaves the State Hash where it was. That is exactly what
/// <see cref="Cells"/> is not, which is why they are two types.
/// </remarks>
public readonly record struct Chunks(int Raw) : IComparable<Chunks>
{
    /// <summary>No extent.</summary>
    public static Chunks Zero => new(0);

    /// <summary>One Chunk.</summary>
    public static Chunks One => new(1);

    /// <summary>Extent ignoring direction.</summary>
    public Chunks Magnitude => new(IntegerMath.Abs(Raw));

    /// <inheritdoc/>
    public int CompareTo(Chunks other) => Raw.CompareTo(other.Raw);

    public static Chunks operator +(Chunks left, Chunks right) => new(left.Raw + right.Raw);

    public static Chunks operator -(Chunks left, Chunks right) => new(left.Raw - right.Raw);

    public static Chunks operator -(Chunks value) => new(-value.Raw);

    /// <inheritdoc cref="op_Multiply(Cells,int)"/>
    public static Chunks operator *(Chunks value, int count) => new(value.Raw * count);

    /// <inheritdoc cref="op_Multiply(Cells,int)"/>
    public static Chunks operator *(int count, Chunks value) => value * count;

    public static bool operator <(Chunks left, Chunks right) => left.Raw < right.Raw;

    public static bool operator >(Chunks left, Chunks right) => left.Raw > right.Raw;

    public static bool operator <=(Chunks left, Chunks right) => left.Raw <= right.Raw;

    public static bool operator >=(Chunks left, Chunks right) => left.Raw >= right.Raw;
}
