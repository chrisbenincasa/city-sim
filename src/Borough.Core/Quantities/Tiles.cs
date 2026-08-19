using Borough.Core.Arithmetic;

namespace Borough.Core.Quantities;

/// <summary>
/// Whole-Tile distance or extent, as a signed 32-bit count. Space in the core is Tiles and nothing else.
/// </summary>
/// <remarks>
/// The map is 16384² Tiles at roughly 4 m each, so i32 carries five orders of magnitude of headroom
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
    /// <summary>
    /// A Tile's edge in metres. <b>The one place this simulation says how big a Tile is.</b>
    /// </summary>
    /// <remarks>
    /// <b>Here rather than on <c>CellGrid</c>, because the Tile is the unit and the Cell is a
    /// multiple of it.</b> <c>CellGrid.MetresPerCell</c> derives from this; so does
    /// <see cref="Speed"/>'s conversion from km/h, which needs to know what a Tile is worth on the
    /// ground before it can say how many of them a Tick covers. ~4 m is <c>CONTEXT.md</c> → Cell's
    /// figure and <c>05 §26</c>'s.
    /// </remarks>
    public const int Metres = 4;

    /// <summary>No distance.</summary>
    public static Tiles Zero => new(0);

    /// <summary>
    /// The Tile extent that covers a range stated in metres, rounded <b>up</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>CellGrid.FromMetres</c>' rule at the finer unit, and it rounds up for that method's
    /// reason.</b> A range authored in metres is a <em>reach</em>, so rounding down would silently
    /// shorten something defended from reality — and the truncation is invisible in whatever the
    /// reach produced. The two conversions differ only in what they land on: a Cell is 128 m, so a
    /// range rounded to one can be out by up to 127 m; a Tile is <see cref="Metres"/>, so the same
    /// range is out by at most three.
    /// </para>
    /// <para>
    /// <b>Metres are an authoring unit and never a stored one.</b> Nothing in <c>Borough.Core</c>
    /// holds a distance in metres — the Ruleset states one, this converts it once at load, and every
    /// consumer sees <see cref="Tiles"/>. That is what keeps a metre out of the State Hash, where a
    /// second unit for one quantity would be two widths for one number (<c>adr/0065</c>).
    /// </para>
    /// </remarks>
    public static Tiles FromMetres(int metres) => new(IntegerMath.CeilDiv(metres, Metres));

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

    /// <summary>
    /// The Tile count, for a human reading a diagnostic.
    /// </summary>
    /// <remarks>
    /// <b>Declared because the compiler-generated one recurses for ever, and the recursion is
    /// invisible until the worst possible moment.</b> A record's <c>PrintMembers</c> prints every
    /// public property, <see cref="Magnitude"/> is a public property whose type is
    /// <see cref="Tiles"/>, so the generated <c>ToString</c> printed a <see cref="Tiles"/> that
    /// printed a <see cref="Tiles"/> — a stack overflow, reached only when something actually
    /// formatted one.
    /// <para>
    /// <b>Nothing formats a quantity on a passing path</b>, so the only trigger is a <em>failing</em>
    /// <c>Assert.Equal&lt;Tiles&gt;</c>, where xUnit formats both values to build the message. The
    /// result was that a one-line assertion failure took down the whole test host with a stack
    /// overflow and no failing test name — <b>the defect destroyed the report of the defect that
    /// woke it</b>. Found in 5a-bis, fixed here rather than worked around, per <c>adr/0073</c>: the
    /// finding belongs to the arithmetic substrate and not to the slice that tripped over it.
    /// </para>
    /// <para>
    /// The general form is worth more than the fix: <b>a record with a computed property of its own
    /// type has an infinitely recursive <c>ToString</c></b>. <see cref="Magnitude"/> is currently the
    /// only one in <c>Borough.Core.Quantities</c>; adding a second to any quantity reintroduces this
    /// unless that quantity also declares a <c>ToString</c>.
    /// </para>
    /// </remarks>
    public override string ToString() =>
        Raw.ToString(System.Globalization.CultureInfo.InvariantCulture) + " Tiles";
}
