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
    /// The wall-clock hour Tick 0 of a Day stands at. <b>05:00, and it used to be midnight.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>A DAY IS A WAKING DAY AND NOT A CALENDAR ONE, which is a change to <c>adr/0101</c>'s
    /// convention rather than to anything it argued.</b> That record spent the freedom on midnight
    /// because nothing then asked what time it was; two things ask now. A run opened with no
    /// <c>--start-at</c> begins at Tick 0, and under the moving sun that meant ***every fresh run
    /// opened in the dark***. And a Day boundary at midnight cuts the night in half, so the quiet
    /// hours belong to two different Day numbers and no readout of "a Day" ever shows one.
    /// </para>
    /// <para>
    /// ⚠ <b>It is a PHASE and not a length.</b> A Day is still 2048 Ticks and 24 hours; what moved
    /// is where the count starts. Nothing that measures a duration is touched, which is exactly the
    /// distinction <see cref="AtClock"/> exists to keep — see its remarks, because
    /// <see cref="AtHour"/> was already serving both meanings and only one of them may move.
    /// </para>
    /// <para>
    /// ⚠ <b>Hash-bearing, and the movement is real rather than incidental.</b> A Shift starting at
    /// 06:00 now begins at Tick 85 of the Day where it began at Tick 512, so every commute in every
    /// world shifts within the Day. <c>05 §4</c> calls that a design change and not an optimisation.
    /// </para>
    /// <para>
    /// ⚠ <b>Five and not six or four, and there is nothing to ratify.</b> It wants to be before the
    /// earliest Shift a Ruleset may declare — <c>shift_start_earliest_hour</c> is 6 in every shipped
    /// file — so that no commute is cut by the Day boundary it belongs to, and after the middle of
    /// the night so the quiet hours stay in one Day. Any hour in that band is the same world; this
    /// one is also just before sunrise, which is what makes a fresh run open on a dawn.
    /// </para>
    /// </remarks>
    public const int DayBeginsAtHour = 5;

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
    /// <param name="hour">A number of in-world hours. 24 is a whole Day.</param>
    public static int AtHour(int hour) => AtMinute(hour * 60);

    /// <summary>
    /// The Tick of the Day a given <b>wall-clock</b> hour falls on, with Tick 0 at
    /// <see cref="DayBeginsAtHour"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b><see cref="AtHour"/> WAS SERVING TWO MEANINGS AND ONLY ONE OF THEM MAY BE PHASED.</b>
    /// It was reading <em>six hours</em> at three call sites — a Shift's length, a punctuality
    /// margin, a whole Day — and <em>six o'clock</em> at a fourth. While Tick 0 was midnight the two
    /// were the same arithmetic and the ambiguity cost nothing; the moment a Day starts at 05:00 they
    /// part company, and a Shift six hours long would have become a Shift one hour long. ***So the
    /// split is what makes the phase safe, and it is the whole of the risk in this change.***
    /// </para>
    /// <para>
    /// ⚠ <b>It wraps, and the wrap is the point.</b> 05:00 is Tick 0 and 04:00 is the last hour of
    /// the same Day, so a caller asking for an hour earlier than the Day's start gets the
    /// <em>late</em> end rather than a negative Tick.
    /// </para>
    /// </remarks>
    /// <param name="hour">A wall-clock hour, 0 to 23.</param>
    public static int AtClock(int hour)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(hour);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(hour, HoursPerDay);

        return AtMinute((hour - DayBeginsAtHour + HoursPerDay) % HoursPerDay * 60);
    }

    /// <summary>
    /// The wall-clock minute of the day a Tick stands at — <b>0 to 1,439, where 0 is midnight.</b>
    /// </summary>
    /// <remarks>
    /// <b><see cref="AtClock"/>'s inverse, and the one place a reading of the clock is derived.</b>
    /// Three separate dumps and the shell's readout each carried their own copy of
    /// <c>ofDay * 24 / PerDay</c>, which was right while the Day began at midnight and silently
    /// wrong the moment it did not. ⚠ <b>It is a number and never a string</b> — <c>05 §1</c>: the
    /// shell owns every string a human reads.
    /// </remarks>
    /// <param name="tick">Any Tick. The Day it falls in does not matter.</param>
    public static int MinuteOfDay(ulong tick) =>
        (int)((IntegerMath.FloorDiv(
                (long)(tick % PerDay) * MinutesPerDay, PerDay)
            + (DayBeginsAtHour * 60)) % MinutesPerDay);

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
