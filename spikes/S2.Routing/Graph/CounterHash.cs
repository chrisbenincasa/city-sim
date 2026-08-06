namespace S2.Routing.Graph;

/// <summary>
/// Counter-based hashing, never a stream — the same rule <c>CLAUDE.md</c> states for the simulation,
/// <c>hash(world_seed, entity_id, tick, purpose_tag)</c>, and the same implementation S4 used.
/// </summary>
/// <remarks>
/// <para>
/// <b>The reason is stronger here than discipline.</b> A generator that carries state between calls
/// makes the graph depend on the order its pieces were generated in, so adding one Arterial to the
/// sweep would change every Street that followed it and silently invalidate every figure already
/// recorded against the earlier graph. Counter-based, the graph at a given parameter set is the same
/// graph forever, and two rungs of the sweep differ only where the parameters differ.
/// </para>
/// <para>
/// Every distinct use gets a distinct <see cref="Purpose"/>. Reusing one correlates two decisions
/// invisibly — here that would show up as an Arterial whose curvature tracks its own origin, which
/// is a road pattern nobody would notice was wrong.
/// </para>
/// <para>
/// The mixer is SplitMix64's finaliser. Not cryptographic and it does not need to be: it needs to be
/// cheap, identical on every machine, and free of the low-bit structure that would turn a modulo
/// into a stride.
/// </para>
/// </remarks>
internal static class CounterHash
{
    private const ulong DefaultSeed = 0x5F34_9E37_79B9_7F4AUL;

    /// <summary>
    /// Appended to, never renumbered. A purpose tag is an input to the hash, so slotting a new one
    /// in beside its siblings changes which values every later use draws and quietly invalidates
    /// figures already recorded.
    /// </summary>
    public enum Purpose : ulong
    {
        ArterialOrigin = 1,
        ArterialHeading = 2,
        ArterialCurvature = 3,
        FootPathPlacement = 4,
        OriginSegment = 5,
        OriginOffset = 6,
        DestinationSegment = 7,
        DestinationOffset = 8,
        MatrixReadIndex = 9,
        MatrixSampleOrigin = 10,
        MatrixSampleDestination = 11,
    }

    public static ulong Of(ulong seed, ulong entity, ulong counter, Purpose purpose) =>
        Mix(Mix(Mix(seed ^ (entity * 0x9E37_79B9_7F4A_7C15UL)) ^ (counter + 0xBF58_476D_1CE4_E5B9UL))
            ^ ((ulong)purpose + 0xD6E8_FEB8_6659_FD93UL));

    /// <summary>The seed every sweep uses unless one is passed, so two rungs are comparable.</summary>
    public static ulong Seed => DefaultSeed;

    /// <summary>
    /// A draw in <c>[0, bound)</c>, by the multiply-high method rather than by modulo — no division,
    /// and no low-bit bias to reason about.
    /// </summary>
    public static int Below(ulong hash, int bound)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bound);

        // Multiply-high consumes the TOP bits of the word, which is what makes it free of the
        // low-bit bias a modulo would carry. It also makes the function quietly wrong for a caller
        // that pre-shifts to get a second draw out of one hash: `Below(h >> 32, 2001)` reads bits
        // that are now zero and returns 0 every single time.
        //
        // That is not hypothetical. It is the bug this line was written to close: every Arterial in
        // the first capture drew the same cross-heading, entered the map at a corner pointing
        // straight back off it, and died within one step — and the footprint table looked entirely
        // healthy while it happened, because a graph with no Arterials in it is still a graph. The
        // severance table is what caught it, which is an argument for reporting a quantity you
        // expect to be boring.
        //
        // Re-mixing makes the function total: a caller cannot destroy entropy it does not know the
        // function depends on. The correct idiom is still a fresh draw with a distinct counter, and
        // the call sites use it — this is the belt to that pair of braces.
        return (int)(((System.UInt128)Mix(hash) * (ulong)bound) >> 64);
    }

    private static ulong Mix(ulong z)
    {
        z ^= z >> 30;
        z *= 0xBF58_476D_1CE4_E5B9UL;
        z ^= z >> 27;
        z *= 0x94D0_49BB_1331_11EBUL;
        return z ^ (z >> 31);
    }
}
