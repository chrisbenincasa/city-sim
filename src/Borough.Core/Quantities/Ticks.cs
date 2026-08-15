using Borough.Core.Arithmetic;

namespace Borough.Core.Quantities;

/// <summary>
/// Simulation time, as an unsigned 64-bit count of Ticks. The only time base in the core.
/// </summary>
/// <remarks>
/// <para>
/// There is no <c>DateTime</c>, no <c>Stopwatch</c> and no wall clock in <c>Borough.Core</c>; those
/// are banned outright, because a simulation that can read the host's clock is one whose replay
/// depends on when it was run. <c>TICKS_PER_DAY</c> is <see cref="PerDay"/> and is a world-creation
/// constant baked into the save. ⚠ <b>It said <em>8192</em> here until <c>adr/0101</c> came past</b>,
/// which <c>adr/0094</c> made false on 2026-08-13 without this line following — the same drift that
/// ADR found in <c>Speed.PerKilometrePerHour</c>, in the file that owns the constant. <b>The repair is
/// the cross-reference rather than the corrected digit</b>: a number spelled out in prose beside its
/// own declaration is a second copy, and <c>plans/0012</c> Cause 1 is what a second copy does.
/// </para>
/// <para>
/// <b>Subtraction is not an operator, and the reason is the same one that kept Money signed.</b>
/// This type is unsigned, so <c>earlier - later</c> would wrap to roughly 1.8×10¹⁹ rather than going
/// visibly negative — a Tick count so large that every downstream comparison silently succeeds.
/// adr/0003's justification for leaving ambient arithmetic unchecked is that <em>the width already
/// closes the question</em>; for unsigned subtraction the width closes nothing, so the gap is covered
/// here in the same shape as <see cref="Money.TryDebit"/> rather than by widening the
/// <c>checked</c> scope beyond the fixed-point library.
/// </para>
/// <para>
/// <b>Instants and durations are one type, deliberately for now.</b> The design says only "the
/// clock", and splitting them would double the surface before anything needs it. The cost is that
/// <c>now + now</c> compiles and means nothing; see plans/0002 for the recorded trade.
/// </para>
/// </remarks>
public readonly record struct Ticks(ulong Raw) : IComparable<Ticks>
{
    /// <summary>Tick zero — world creation, and the additive identity for a duration.</summary>
    public static Ticks Zero => new(0);

    /// <summary>
    /// <c>TICKS_PER_DAY</c>. A world-creation constant baked into the save (<c>adr/0019</c>).
    /// </summary>
    /// <remarks>
    /// <b>Named here because a derivation cannot cite a number that has no name.</b> It appeared in
    /// prose in three documents and as a bare <c>8192</c> in one populator, and slice 8 needed to
    /// write <em>a plume fades over about a Day</em> as arithmetic rather than as a comment.
    /// <para>
    /// <b>It is a <c>const</c> and <c>adr/0015</c> says it should be Ruleset data</b> — <em>"these
    /// live in the Ruleset like everything else and are read from it"</em> — and it is not, because no
    /// Ruleset key states it. That is recorded rather than fixed here: naming it is what makes the gap
    /// visible, and closing it means giving a designer a knob <c>adr/0019</c> spends a whole ADR
    /// arguing they should not turn casually. See <c>plans/0012</c>.
    /// </para>
    /// </remarks>
    public const int PerDay = 2048;

    /// <summary>
    /// Seconds of in-world time in one Day. <b>The other half of what a Tick is worth</b>, and the
    /// only place this figure is stated.
    /// </summary>
    /// <remarks>
    /// <b>A Day is 24 in-world hours by definition, so this is fixed while <see cref="PerDay"/>
    /// moves</b> — which is what makes a Tick 42.1875 s at 2048 and was 10.546875 s at 8192
    /// (<c>adr/0094</c>). Both <see cref="Speed"/> and <see cref="TravelTime"/> convert through it,
    /// and they held two copies of it until the constant above moved and one of them did not follow.
    /// </remarks>
    public const int SecondsPerDay = 86_400;

    /// <summary>Minutes of in-world time in one Day. The same figure one unit up.</summary>
    public const int MinutesPerDay = 1_440;

    /// <summary>Hours of in-world time in one Day. The same figure one unit further up.</summary>
    public const int HoursPerDay = 24;

    /// <summary>
    /// The Tick of the Day a given in-world hour falls on, <b>with Tick 0 as midnight</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Midnight is <c>adr/0101</c>'s convention and not a number anybody may tune.</b> A Day has to
    /// begin somewhere; until that decision nothing in the simulation asked what time it was, so the
    /// choice was unobservable and was deliberately left unspent. A Day with a shape — two commute
    /// peaks and a quiet night — distinguishes its own ends, so the freedom is spent here and every
    /// later reader of the clock inherits it rather than choosing again.
    /// </para>
    /// <para>
    /// ⚠ <b>An hour does not divide the Day, and this rounds rather than pretending otherwise.</b>
    /// <see cref="PerDay"/> is 2048, which is 2¹¹, and 24 does not divide it: an hour is <b>85.33</b>
    /// Ticks. So the twenty-four hour marks are twenty-four anchor Ticks computed once — 0, 85, 171,
    /// 256, … — unevenly spaced by at most one Tick, which is <b>42 seconds</b>. Nothing here repeats
    /// an hourly period, which is the arrangement that matters: a mechanism stepping by 85 Ticks an
    /// hour would accumulate a third of a Tick each time and be eight Ticks adrift by the end of the
    /// Day, for ever.
    /// </para>
    /// </remarks>
    /// <param name="hour">An in-world hour. 0 is midnight; 24 is the following midnight.</param>
    public static int AtHour(int hour) => AtMinute(hour * 60);

    /// <summary>
    /// The Tick of the Day a given in-world minute falls on, with Tick 0 as midnight.
    /// </summary>
    /// <remarks>
    /// <b>A minute is 1.42 Ticks, so this rounds harder than <see cref="AtHour"/> does and is the
    /// finer of the two for that reason rather than in spite of it.</b> It exists for quantities that
    /// are genuinely sub-hour — a punctuality margin, a Shift length — where quantising to the hour
    /// would pile a whole city onto a handful of Ticks. ⚠ <b>Not everything wants it</b>: a
    /// Workplace's start hour is on the hour because workplaces really do open on the hour, and
    /// spreading that would delete the texture rather than smooth it.
    /// </remarks>
    /// <param name="minute">An in-world minute. 0 is midnight; 1440 is the following midnight.</param>
    public static int AtMinute(int minute)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minute);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minute, MinutesPerDay);

        // The widening is BOR0207's, and the lint is right in general even though this cannot
        // overflow at 1,440 minutes: the rule is about the shape of a scale-then-divide, not about
        // the range of one call site's argument.
        return (int)IntegerMath.RoundDiv((long)minute * PerDay, MinutesPerDay);
    }


    /// <summary>
    /// Subtracts, refusing rather than wrapping. <paramref name="earlier"/> must not be after this.
    /// </summary>
    /// <returns><see langword="true"/> if <paramref name="earlier"/> is at or before this Tick.</returns>
    public bool TrySubtract(Ticks earlier, out Ticks elapsed)
    {
        if (earlier.Raw > Raw)
        {
            elapsed = Zero;
            return false;
        }

        elapsed = new Ticks(Raw - earlier.Raw);
        return true;
    }

    /// <inheritdoc/>
    public int CompareTo(Ticks other) => Raw.CompareTo(other.Raw);

    public static Ticks operator +(Ticks left, Ticks right) => new(left.Raw + right.Raw);

    /// <summary>Scales a duration by a whole count. Never Ticks times Ticks.</summary>
    public static Ticks operator *(Ticks value, uint count) => new(value.Raw * count);

    /// <inheritdoc cref="op_Multiply(Ticks,uint)"/>
    public static Ticks operator *(uint count, Ticks value) => value * count;

    public static bool operator <(Ticks left, Ticks right) => left.Raw < right.Raw;

    public static bool operator >(Ticks left, Ticks right) => left.Raw > right.Raw;

    public static bool operator <=(Ticks left, Ticks right) => left.Raw <= right.Raw;

    public static bool operator >=(Ticks left, Ticks right) => left.Raw >= right.Raw;
}
