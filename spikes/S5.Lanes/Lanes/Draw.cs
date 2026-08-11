namespace S5.Lanes.Lanes;

/// <summary>
/// Counter-based draws, because <c>System.Random</c> is banned (BOR0302) and because a spike whose
/// fixture is not reproducible cannot be re-run against its own recorded number.
/// </summary>
/// <remarks>
/// The same shape as <c>Borough.Core</c>'s <c>Randomness.Mix</c> — a hash of (seed, counter,
/// purpose) rather than a stream — and for the same reason: a stream makes every draw depend on
/// evaluation order, so the fixture would change when the generator's loop order changed. Here that
/// would silently move a measured number.
/// </remarks>
internal static class Draw
{
    /// <summary>SplitMix64's finaliser. Integer only, constant shifts only.</summary>
    public static ulong Mix(ulong seed, ulong counter, ulong purpose)
    {
        unchecked
        {
            ulong z = seed + (counter * 0x9E3779B97F4A7C15UL) + (purpose * 0xBF58476D1CE4E5B9UL);
            z ^= z >> 30;
            z *= 0xBF58476D1CE4E5B9UL;
            z ^= z >> 27;
            z *= 0x94D049BB133111EBUL;
            z ^= z >> 31;
            return z;
        }
    }

    /// <summary>A draw in <c>[0, bound)</c>. <paramref name="bound"/> must be positive.</summary>
    public static int Below(ulong seed, ulong counter, ulong purpose, int bound)
    {
        unchecked
        {
            ulong r = Mix(seed, counter, purpose);
            return (int)(((r >> 32) * (ulong)bound) >> 32);
        }
    }
}
