using System.Numerics;
using Borough.Core.Arithmetic;

namespace S2.Routing.Graph;

/// <summary>
/// The integer geometry this spike needs and the substrate does not have.
/// </summary>
/// <remarks>
/// <para>
/// <b>Nothing here enters the substrate.</b> <c>plans/0010</c> R0 is explicit about why: the spike
/// needs a distance, <c>Borough.Core.Arithmetic</c> has <c>Abs</c>, <c>FloorDiv</c>, <c>CeilDiv</c>,
/// <c>RoundDiv</c>, the guarded shifts, <c>Fixed.Mul</c>/<c>Div</c>/<c>Lerp</c> and the tabulated
/// <c>exp</c>/<c>log</c>, and <b>no <c>Sqrt</c> anywhere</b> — and it does not need one, because
/// A*'s admissibility requires only a <i>lower bound</i> on the true distance. This file exists so
/// the generator can measure a freeform Arterial's arc length, and it dies with the spike.
/// </para>
/// <para>
/// <b><see cref="SqrtFloor"/> uses no division and no <c>Math</c>.</b> Newton's method would need
/// both a division per iteration and a seed; the restoring bit-by-bit algorithm below needs neither,
/// uses only constant shifts, comparisons and subtraction, and is exact — it returns the true floor
/// of the square root rather than an approximation that would have to be argued about.
/// </para>
/// </remarks>
internal static class IntegerGeometry
{
    /// <summary>
    /// The exact floor of the square root of a non-negative value.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is negative.</exception>
    public static int SqrtFloor(long value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);

        if (value == 0)
        {
            return 0;
        }

        long remainder = value;
        long result = 0;

        // The highest power of four not exceeding the value, found directly.
        //
        // This line was `long bit = 1L << 62; while (bit > remainder) bit >>= 2;` and that cost the
        // first denominator capture most of its time: the search calls this twice per pushed node,
        // and the search-down loop ran ~30 iterations before doing any work on the small distances
        // that dominate. A denominator that spends its time in a helper's warm-up is measuring the
        // helper. Log2 gives the exponent outright; masking the low bit makes it even, which is the
        // invariant the restoring loop below needs.
        long bit = IntegerMath.ShiftLeft(1L, BitOperations.Log2((ulong)value) & ~1);

        while (bit != 0)
        {
            if (remainder >= result + bit)
            {
                remainder -= result + bit;
                result = (result >> 1) + bit;
            }
            else
            {
                result >>= 1;
            }

            bit >>= 2;
        }

        return (int)result;
    }

    /// <summary>
    /// The straight-line distance between two Tile coordinates, floored to whole Tiles.
    /// </summary>
    /// <remarks>
    /// Flooring is the safe direction everywhere this is used as a bound: it underestimates, and an
    /// underestimate of distance is what admissibility requires. Where it is used as a
    /// <i>length</i> — an Arterial's arc length in the generator — flooring costs at most one Tile
    /// per polyline vertex against a Segment tens of Tiles long.
    /// </remarks>
    public static int Distance(int ax, int ay, int bx, int by)
    {
        long dx = (long)ax - bx;
        long dy = (long)ay - by;
        return SqrtFloor((dx * dx) + (dy * dy));
    }
}
