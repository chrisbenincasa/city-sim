using Borough.Core.Arithmetic;

namespace Borough.Core.Quantities;

/// <summary>
/// A duration of travel, in Q16.16 Ticks. <b>The quantity routing minimises, trip failure is judged
/// on, and the player is shown</b> — one quantity, which is <c>02 §5.9</c>'s whole requirement.
/// </summary>
/// <remarks>
/// <para>
/// <b>It is sub-Tick, and that is <c>adr/0071</c>'s load-bearing half.</b> A vehicle covers roughly
/// one Segment per Tick, so at whole-Tick resolution a 4-Tile stub and a 60-Tile run both cost 1 —
/// and A* over that graph minimises <b>hop count</b> while every column and every panel says Ticks.
/// That is SC4's failure with better manners: the quantity optimised and the quantity shown have
/// diverged and nothing in the code marks the seam.
/// </para>
/// <para>
/// <b>Distinct from <see cref="Ticks"/> rather than a scaled version of it.</b> <see cref="Ticks"/>
/// counts whole Ticks in a <c>ulong</c> and is what the clock, the Event Wheel and every rate are
/// denominated in; this is a cost accumulated along a path. Neither converts to the other implicitly,
/// so a traversal cost can never be armed into the Wheel by accident, and a Wheel period can never be
/// summed into a route.
/// </para>
/// <para>
/// <b>Addition is exact, which is the property A* actually depends on.</b> A path's cost is the exact
/// sum of its Arcs' costs with no accumulation error, so two routes compared are compared on what they
/// are rather than on where the rounding fell. The range is ±32,768 Ticks — four Days, against a
/// Commute Budget of order a hundred — and the resolution is 1/65,536 of a Tick, about 0.16 ms of
/// in-world time.
/// </para>
/// <para>
/// <b>The overflow policy is to throw, and that is deliberate.</b> <see cref="Fixed.Div"/> narrows
/// <c>checked</c>, so a cost that leaves the format raises rather than wrapping negative. The live
/// risk is a volume-delay function driving a jammed Arc's cost without bound; <c>adr/0071</c> records
/// that the answer then is a saturating ceiling with a stated meaning — an impassable Arc and a
/// four-Day Arc are the same routing decision — and that the choice belongs to the slice where the
/// VDF exists.
/// </para>
/// </remarks>
public readonly record struct TravelTime(int Raw) : IComparable<TravelTime>
{
    /// <summary>No time at all. The additive identity, and the cost of a path with no Arcs.</summary>
    public static TravelTime Zero => new(0);

    /// <summary>
    /// <b>The cost of an Arc no mode may traverse.</b> Not a large number — a sentinel, and a search
    /// must refuse it rather than relax through it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It is <see cref="Fixed.MaxValue"/> so that any real cost compares smaller, and it is named so
    /// that <c>if (cost == TravelTime.Impassable)</c> is what a reader sees rather than a magic
    /// comparison against the format's ceiling.
    /// </para>
    /// <para>
    /// <b>It absorbs addition</b>: <c>Impassable + anything</c> is <c>Impassable</c>, and so is any
    /// sum that would reach the ceiling from below. That is the sentinel's whole contract — a path
    /// through an impassable Arc is not a long path, it is not a path, and a cost that says so
    /// survives being summed by a relaxation step that did not think to check.
    /// </para>
    /// <para>
    /// <b>This remark previously claimed the addition would overflow instead, and it did not.</b>
    /// <c>Fixed</c> is the one place in <c>Borough.Core</c> where <c>checked</c> appears, nothing sets
    /// <c>CheckForOverflowUnderflow</c>, and <c>operator +</c> was a bare <c>int</c> addition — so
    /// <c>Impassable + FromTicks(1)</c> wrapped to a large <em>negative</em> cost, which a search
    /// preferring the cheapest route would have taken in preference to every real one. A documented
    /// failure mode with no implementation is the shape <c>plans/0012</c> <i>Cause 1</i> warns about,
    /// and it was found by a test written against the prose rather than against the code.
    /// </para>
    /// </remarks>
    public static TravelTime Impassable => new(Fixed.MaxValue);

    /// <summary>Whether this is the <see cref="Impassable"/> sentinel rather than a duration.</summary>
    public bool IsImpassable => Raw == Fixed.MaxValue;

    /// <summary>Lifts a whole count of Ticks. <b>For authored budgets, not for a traversal.</b></summary>
    /// <exception cref="OverflowException">The duration exceeds ±32,768 Ticks.</exception>
    public static TravelTime FromTicks(int ticks) => new(Fixed.FromInt(ticks));

    /// <summary>
    /// The time to cover a distance at a speed. <b>The one construction a traversal cost has.</b>
    /// </summary>
    /// <remarks>
    /// <c>Tiles ÷ Tiles-per-Tick → Ticks</c>: the dimensions cancel, which is why this is offered as
    /// a named operation rather than as an operator on two raw <c>int</c>s. Rounding is floor,
    /// following <see cref="Fixed"/> throughout, so a cost underestimates by at most 1/65,536 Tick —
    /// the safe direction for a heuristic and immaterial for a sum.
    /// </remarks>
    /// <exception cref="DivideByZeroException"><paramref name="speed"/> is <see cref="Speed.Zero"/>.</exception>
    public static TravelTime Over(Tiles distance, Speed speed) =>
        new(Fixed.Div(Fixed.FromInt(distance.Raw), speed.Raw));

    /// <summary>Truncates to whole Ticks toward negative infinity — for a panel, never for a sum.</summary>
    public Ticks ToTicksFloor() => new((ulong)Fixed.ToIntFloor(Raw));

    /// <inheritdoc/>
    public int CompareTo(TravelTime other) => Raw.CompareTo(other.Raw);

    /// <summary>
    /// Accumulating a path. Exact, which is the whole reason for the format, and <b>saturating at
    /// <see cref="Impassable"/></b>.
    /// </summary>
    /// <remarks>
    /// Saturation rather than a throw, because the caller is a relaxation loop over Arcs and an
    /// exception there would make an impassable Arc a crash rather than a route nobody takes.
    /// Saturation is also monotone, which is what a search needs: a partial path that has reached
    /// <see cref="Impassable"/> can never come back down below a real one.
    /// </remarks>
    public static TravelTime operator +(TravelTime left, TravelTime right)
    {
        long sum = (long)left.Raw + right.Raw;

        return sum >= Fixed.MaxValue ? Impassable : new TravelTime((int)sum);
    }

    public static TravelTime operator -(TravelTime left, TravelTime right) =>
        new(left.Raw - right.Raw);

    /// <summary>Scales by a whole count. Never TravelTime times TravelTime.</summary>
    public static TravelTime operator *(TravelTime value, int count) => new(value.Raw * count);

    /// <inheritdoc cref="op_Multiply(TravelTime,int)"/>
    public static TravelTime operator *(int count, TravelTime value) => value * count;

    /// <summary>Scales by a dimensionless ratio — what a volume-delay function will do to an Arc.</summary>
    public static TravelTime operator *(TravelTime value, Ratio ratio) =>
        new(Fixed.Mul(value.Raw, ratio.Raw));

    /// <inheritdoc cref="op_Multiply(TravelTime,Ratio)"/>
    public static TravelTime operator *(Ratio ratio, TravelTime value) => value * ratio;

    public static bool operator <(TravelTime left, TravelTime right) => left.Raw < right.Raw;

    public static bool operator >(TravelTime left, TravelTime right) => left.Raw > right.Raw;

    public static bool operator <=(TravelTime left, TravelTime right) => left.Raw <= right.Raw;

    public static bool operator >=(TravelTime left, TravelTime right) => left.Raw >= right.Raw;
}
