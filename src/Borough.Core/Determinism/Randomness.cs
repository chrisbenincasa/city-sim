namespace Borough.Core.Determinism;

using Borough.Core.Quantities;

/// <summary>
/// Counter-based randomness: <c>draw(world_key, entity, tick, purpose)</c>. Never a stream.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is a format, not an implementation detail.</b> An Input Log plus a world seed reproduces a
/// run only if this function is bit-identical, so changing it invalidates every stored log, every State
/// Hash baseline and every bug report in flight. adr/0003 puts it in the same category as a save-format
/// change, and 05 §7's version machinery applies to it. <b>If profiling ever demands fewer rounds, that
/// is a deliberate re-baseline and not a free optimisation.</b> The literal constants are written out in
/// adr/0003 so this is re-implementable from the document alone, because saves outlive binaries.
/// </para>
/// <para>
/// <b>Why hashing rather than a stream.</b> A shared mutable stream couples every draw to every draw
/// before it, so results depend on evaluation order and any reordering of updates changes the whole
/// city. Hashing makes a draw a pure function of its coordinates, which means the Decide phase can
/// later be parallelised in any order across any number of threads with bit-identical results and zero
/// coordination. <c>System.Random</c> is banned outright: its algorithm changed in .NET 6 and has never
/// been documented as stable across versions.
/// </para>
/// <para>
/// <b>The arithmetic wraps, deliberately.</b> This is one of the two exceptions adr/0003 names to the
/// overflow policy, and the <c>unchecked</c> blocks below are load-bearing documentation rather than
/// defensive habit — without them a future reader finding a multiply that overflows will "fix" it and
/// silently invalidate every replay in the project.
/// </para>
/// </remarks>
public static class Randomness
{
    /// <summary>
    /// The golden-ratio odd constant, 2⁶⁴ / φ. Its role is to make consecutive coordinate values land
    /// far apart before mixing, so that the low-entropy inputs this function actually gets — entity 0,
    /// entity 1, tick 0, tick 1 — do not enter the mixer as near-neighbours.
    /// </summary>
    internal const ulong Golden = 0x9E37_79B9_7F4A_7C15UL;

    /// <summary>
    /// Draws a 64-bit value for one entity, at one Tick, for one purpose.
    /// </summary>
    /// <remarks>
    /// Three rounds, one per varying coordinate. The seed's own round is not here: it is
    /// loop-invariant and lives in <see cref="WorldKey.FromSeed"/>, which is what keeps this fix free.
    /// </remarks>
    public static ulong Draw(WorldKey world, ulong entityId, Ticks tick, PurposeTag purpose)
    {
        unchecked
        {
            ulong h = Mix(world.Raw + Golden + entityId);
            h = Mix(h + Golden + tick.Raw);
            return Mix(h + Golden + (ulong)purpose);
        }
    }

    /// <summary>
    /// SplitMix64's finalizer. A bijection over the full 64-bit domain, so it has no collisions at all
    /// — which is the property that makes the counter domain safe to enumerate.
    /// </summary>
    /// <remarks>
    /// Chosen for four properties, in adr/0003's order: it is a bijection; it is published and stable,
    /// unlike <c>System.Random</c>; it has no dependency, which this project's own revisit trigger
    /// requires of anything in the core; and it is about fifteen instructions, cheap enough not to
    /// matter beside the decision it seeds. It is not a cryptographic hash and does not need to be.
    /// </remarks>
    internal static ulong Mix(ulong z)
    {
        unchecked
        {
            z ^= z >> 30;
            z *= 0xBF58_476D_1CE4_E5B9UL;
            z ^= z >> 27;
            z *= 0x94D0_49BB_1331_11EBUL;
            z ^= z >> 31;
            return z;
        }
    }
}
