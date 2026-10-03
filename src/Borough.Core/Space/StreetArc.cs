using Borough.Core.Arithmetic;

namespace Borough.Core.Space;

/// <summary>A circular Street arc of at most a quarter turn. Coordinates and distances are Q16.16 Tiles.</summary>
public readonly record struct StreetArc
{
    // At radii above 2^33 Tiles, every int-length chord bulges less than 1/64 Tile.
    // Flattening there avoids unbounded centers; Q56 rotations retain precision below this bound.
    private const long MaximumRadius = 1L << 49;
    private readonly long _startAngle;
    private readonly long _sweep;

    private StreetArc(
        (long East, long North) a, (long East, long North) b,
        (long East, long North) center, long radius, long startAngle, long sweep, int length)
    {
        A = a;
        B = b;
        Center = center;
        Radius = radius;
        _startAngle = startAngle;
        _sweep = sweep;
        Length = length;
    }

    public (long East, long North) A { get; }
    public (long East, long North) B { get; }
    public (long East, long North) Center { get; }
    public long Radius { get; }
    public int StartAngle => NarrowAngle(_startAngle);
    public int Sweep => NarrowAngle(_sweep);
    public int Length { get; }
    public bool IsStraight => Radius == 0;

    /// <summary>Creates an arc from integer Tile endpoints and signed Q16.16 sagitta. Left bulges are positive.</summary>
    /// <remarks>Lengths must fit an int in Q16.16; the 16,384-Tile world fits this range.</remarks>
    public static bool TryCreate(int aEast, int aNorth, int bEast, int bNorth, int sagitta, out StreetArc arc)
    {
        arc = default;
        long dx = (long)bEast - aEast;
        long dy = (long)bNorth - aNorth;
        if (dx < -32767 || dx > 32767 || dy < -32767 || dy > 32767)
        {
            return false;
        }

        long squareTiles = (dx * dx) + (dy * dy);
        if (squareTiles == 0 || squareTiles > 1073741823)
        {
            return false;
        }

        long square = squareTiles << 32;
        int chord = IntegerMath.SqrtFloor(square);
        long s = sagitta < 0 ? -(long)sagitta : sagitta;
        if (s * 2 > chord)
        {
            return false;
        }

        // A quarter turn requires h >= chord/2, where h = (chord² - 4s²)/(8s).
        Int128 difference = square - (4 * (Int128)s * s);
        if ((difference * difference) < (16 * (Int128)s * s * square))
        {
            return false;
        }

        var a = ((long)aEast * Fixed.One, (long)aNorth * Fixed.One);
        var b = ((long)bEast * Fixed.One, (long)bNorth * Fixed.One);
        var midpoint = (a.Item1 + ((b.Item1 - a.Item1) >> 1), a.Item2 + ((b.Item2 - a.Item2) >> 1));
        long radius = s == 0 ? 0 : IntegerMath.FloorDiv(square + (4 * s * s), 8 * s);
        if (s == 0 || radius > MaximumRadius)
        {
            arc = new(a, b, midpoint, 0, Transcendental.Atan2Wide(dy, dx), 0, chord);
            return true;
        }

        // One Newton correction supplies the chord in Q32. A Q16 normal would move a distant center.
        long chordFine = ((long)chord << 16) + IntegerMath.FloorDiv((square - ((long)chord * chord)) << 16, 2L * chord);
        long h = sagitta > 0 ? radius - s : s - radius;
        var center = (
            midpoint.Item1 + Scale(h, dy << 32, chordFine),
            midpoint.Item2 - Scale(h, dx << 32, chordFine));
        long start = Transcendental.Atan2Wide(a.Item2 - center.Item2, a.Item1 - center.Item1);
        long sweep = 4 * Transcendental.Atan2Wide(2 * s, chord);
        sweep = sagitta > 0 ? -sweep : sweep;
        long length = ArcLength(radius, sweep);
        if (length > int.MaxValue)
        {
            return false;
        }

        arc = new(a, b, center, radius, start, sweep, (int)length);
        return true;
    }

    /// <summary>Returns the point at a clamped arc-length offset. Both endpoints are exact.</summary>
    public (long East, long North) PointAt(int offset)
    {
        offset = ClampOffset(offset);
        if (offset == 0)
        {
            return A;
        }

        if (offset == Length)
        {
            return B;
        }

        if (IsStraight)
        {
            return (A.East + Scale(B.East - A.East, offset, Length),
                A.North + Scale(B.North - A.North, offset, Length));
        }

        return CirclePoint(Radius, _startAngle + Scale(_sweep, offset, Length));
    }

    /// <summary>Returns a Q16.16 unit tangent in the A-to-B direction of travel.</summary>
    public (int East, int North) TangentAt(int offset)
    {
        if (IsStraight)
        {
            return ((int)Scale(B.East - A.East, Fixed.One, Length),
                (int)Scale(B.North - A.North, Fixed.One, Length));
        }

        var (sin, cos) = Transcendental.SinCosWide(_startAngle + Scale(_sweep, ClampOffset(offset), Length));
        return _sweep > 0
            ? (-NarrowVector(sin), NarrowVector(cos))
            : (NarrowVector(sin), -NarrowVector(cos));
    }

    /// <summary>Returns the shortest Q16.16 distance to the finite arc.</summary>
    /// <exception cref="OverflowException">The distance does not fit Q16.16 in an int.</exception>
    public int DistanceTo(long east, long north)
    {
        var point = PointAt(OffsetAlong(east, north));
        long distance = Hypot(east - point.East, north - point.North);
        if (distance > int.MaxValue)
        {
            throw new OverflowException("Distance exceeds Q16.16.");
        }

        return (int)distance;
    }

    /// <summary>Returns the arc-length offset of the closest point, clamped to the endpoints.</summary>
    public int OffsetAlong(long east, long north)
    {
        if (IsStraight)
        {
            long dx = B.East - A.East;
            long dy = B.North - A.North;
            long projection = Scale(east - A.East, dx, Length) + Scale(north - A.North, dy, Length);
            return projection <= 0 ? 0 : projection >= Length ? Length : (int)projection;
        }

        if (east == Center.East && north == Center.North)
        {
            return 0;
        }

        long delta = Transcendental.Atan2Wide(north - Center.North, east - Center.East) - _startAngle;
        delta = ((delta + (Transcendental.WideOne >> 1)) & (Transcendental.WideOne - 1)) - (Transcendental.WideOne >> 1);
        delta = _sweep < 0 ? -delta : delta;
        long sweep = _sweep < 0 ? -_sweep : _sweep;
        if (delta < 0 || delta > sweep)
        {
            return Hypot(east - A.East, north - A.North) <= Hypot(east - B.East, north - B.North) ? 0 : Length;
        }

        return (int)Scale(delta, Length, sweep);
    }

    /// <summary>Returns the offset curve without rounding endpoints to Tiles. Left distances are positive.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The radius is nonpositive, exceeds the bound, or gives a sub-raw-unit length.</exception>
    /// <exception cref="OverflowException">The offset arc length does not fit Q16.16 in an int.</exception>
    public StreetArc Parallel(int distance)
    {
        if (IsStraight)
        {
            var (sin, cos) = Transcendental.SinCosWide(_startAngle);
            long east = -Scale(distance, sin, Transcendental.WideOne);
            long north = Scale(distance, cos, Transcendental.WideOne);
            return new((A.East + east, A.North + north), (B.East + east, B.North + north),
                (Center.East + east, Center.North + north), 0, _startAngle, 0, Length);
        }

        long radius = Radius + (_sweep < 0 ? (long)distance : -(long)distance);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radius, nameof(distance));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(radius, MaximumRadius, nameof(distance));
        long length = ArcLength(radius, _sweep);
        ArgumentOutOfRangeException.ThrowIfZero(length, nameof(distance));
        if (length > int.MaxValue)
        {
            throw new OverflowException("Parallel arc length exceeds Q16.16.");
        }

        return new(CirclePoint(radius, _startAngle), CirclePoint(radius, _startAngle + _sweep),
            Center, radius, _startAngle, _sweep, (int)length);
    }

    /// <summary>Returns the sagitta of the arc through three integer Tile points; collinear points give zero.</summary>
    /// <exception cref="OverflowException">The arc's sagitta does not fit Q16.16 in an int.</exception>
    public static int SagittaThrough(int aE, int aN, int mE, int mN, int bE, int bN)
    {
        if (!CircleThrough(aE, aN, mE, mN, bE, bN, out var center, out long radius, out int sign))
        {
            return 0;
        }

        long dx = ((long)bE - aE) * Fixed.One;
        long dy = ((long)bN - aN) * Fixed.One;
        long hE = center.East - (((long)aE + bE) * (Fixed.One >> 1));
        long hN = center.North - (((long)aN + bN) * (Fixed.One >> 1));
        long h = Hypot(hE, hN);
        long centerSide = Scale(hN, bE - (long)aE, Fixed.One) - Scale(hE, bN - (long)aN, Fixed.One);
        long sagitta = (centerSide > 0) == (sign > 0)
            ? radius + h
            : IntegerMath.FloorDiv((dx * dx) + (dy * dy), 4 * (radius + h));
        if (sagitta > int.MaxValue)
        {
            throw new OverflowException("Sagitta exceeds Q16.16.");
        }

        return (int)sagitta * sign;
    }

    /// <summary>Rounds a split point to Tiles and fits both halves to the circle through A, node and B.</summary>
    public bool TrySplitAt(int offset, out int nodeEast, out int nodeNorth, out int firstSagitta, out int secondSagitta)
    {
        var point = PointAt(offset);
        nodeEast = RoundTile(point.East);
        nodeNorth = RoundTile(point.North);
        firstSagitta = 0;
        secondSagitta = 0;
        int aE = RoundTile(A.East);
        int aN = RoundTile(A.North);
        int bE = RoundTile(B.East);
        int bN = RoundTile(B.North);
        if ((nodeEast == aE && nodeNorth == aN) || (nodeEast == bE && nodeNorth == bN))
        {
            return false;
        }

        if (CircleThrough(aE, aN, nodeEast, nodeNorth, bE, bN, out var center, out long radius, out int sign))
        {
            firstSagitta = HalfSagitta(aE, aN, nodeEast, nodeNorth, center, radius) * sign;
            secondSagitta = HalfSagitta(nodeEast, nodeNorth, bE, bN, center, radius) * sign;
        }

        return true;
    }

    private (long East, long North) CirclePoint(long radius, long angle)
    {
        var (sin, cos) = Transcendental.SinCosWide(angle);
        return (Center.East + Scale(radius, cos, Transcendental.WideOne),
            Center.North + Scale(radius, sin, Transcendental.WideOne));
    }

    private int ClampOffset(int offset) => offset < 0 ? 0 : offset > Length ? Length : offset;
    private static int NarrowAngle(long angle) => (int)((angle + (1L << 39)) >> 40);
    private static int NarrowVector(long value) => (int)((value + (1L << 39)) >> 40);
    private static int RoundTile(long value) => (int)IntegerMath.RoundDiv(value, Fixed.One);

    // MulDivFloor widens products to Int128. Geometry ratios truncate toward zero for signed inputs.
    private static long Scale(long value, long numerator, long denominator)
    {
        bool negative = (value < 0) ^ (numerator < 0) ^ (denominator < 0);
        long result = IntegerMath.MulDivFloor(value < 0 ? -value : value,
            numerator < 0 ? -numerator : numerator, denominator < 0 ? -denominator : denominator);
        return negative ? -result : result;
    }

    private static long ArcLength(long radius, long sweep) =>
        IntegerMath.MulDivFloor(radius, Transcendental.RadiansWide(sweep < 0 ? -sweep : sweep), Transcendental.WideOne);

    private static long Hypot(long x, long y)
    {
        x = x < 0 ? -x : x;
        y = y < 0 ? -y : y;
        int shift = 0;
        while (x > (1L << 30) || y > (1L << 30))
        {
            x >>= 1;
            y >>= 1;
            shift++;
        }

        return IntegerMath.ShiftLeft((long)IntegerMath.SqrtFloor((x * x) + (y * y)), shift);
    }

    private static bool CircleThrough(int aE, int aN, int mE, int mN, int bE, int bN,
        out (long East, long North) center, out long radius, out int sign)
    {
        long mx = (long)mE - aE;
        long my = (long)mN - aN;
        long bx = (long)bE - aE;
        long by = (long)bN - aN;
        long cross = (bx * my) - (by * mx);
        center = default;
        radius = 0;
        sign = cross > 0 ? 1 : -1;
        if (cross == 0)
        {
            return false;
        }

        long mSquare = (mx * mx) + (my * my);
        long bSquare = (bx * bx) + (by * by);
        long cx = Scale((mSquare * by) - (bSquare * my), Fixed.One, -2 * cross);
        long cy = Scale((bSquare * mx) - (mSquare * bx), Fixed.One, -2 * cross);
        center = (((long)aE * Fixed.One) + cx, ((long)aN * Fixed.One) + cy);
        radius = Hypot(cx, cy);
        return true;
    }

    private static int HalfSagitta(int aE, int aN, int bE, int bN, (long East, long North) center, long radius)
    {
        long dx = ((long)bE - aE) * Fixed.One;
        long dy = ((long)bN - aN) * Fixed.One;
        long h = Hypot(center.East - (((long)aE + bE) * (Fixed.One >> 1)),
            center.North - (((long)aN + bN) * (Fixed.One >> 1)));
        return (int)IntegerMath.FloorDiv((dx * dx) + (dy * dy), 4 * (radius + h));
    }
}
