namespace Borough.Core.Quantities;

/// <summary>
/// Simulation time, as an unsigned 64-bit count of Ticks. The only time base in the core.
/// </summary>
/// <remarks>
/// <para>
/// There is no <c>DateTime</c>, no <c>Stopwatch</c> and no wall clock in <c>Borough.Core</c>; those
/// are banned outright, because a simulation that can read the host's clock is one whose replay
/// depends on when it was run. <c>TICKS_PER_DAY</c> is 8192 and is a world-creation constant baked
/// into the save.
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
    public const int PerDay = 8192;

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
