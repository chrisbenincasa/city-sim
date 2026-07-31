namespace S4.Kernels.Kernels;

/// <summary>
/// Counter-based hashing, never a stream. CLAUDE.md states the rule for the simulation —
/// <c>hash(world_seed, entity_id, tick, purpose_tag)</c> — and the kernels follow it for a reason
/// beyond discipline: a seeded generator carries state between invocations, and BenchmarkDotNet
/// invokes a method thousands of times. Anything stateful would make the tenth invocation measure a
/// different input distribution than the first.
///
/// Every distinct use gets a distinct <see cref="Purpose"/>. Reusing one correlates two decisions
/// invisibly, which in a benchmark shows up as an access pattern the prefetcher can learn.
///
/// The mixer is SplitMix64's finaliser. It is not a cryptographic hash and does not need to be; it
/// needs to be cheap, deterministic across both machines, and free of the low-bit structure that
/// would turn <c>% n</c> into a stride.
/// </summary>
internal static class CounterHash
{
    /// <summary>The world seed. Fixed, so every run on every machine sees the same inputs.</summary>
    private const ulong Seed = 0x5F34_9E37_79B9_7F4AUL;

    public enum Purpose : ulong
    {
        K1Rate = 1,
        K1Weight = 2,
        K2Permutation = 3,
        K2Generation = 4,
        K5InitialBucket = 5,
        K5RescheduleDelay = 6,

        // Appended rather than slotted in beside their siblings. A purpose tag is an input to the
        // hash, so renumbering an existing one changes which values that kernel draws and quietly
        // invalidates every figure already recorded against it.
        K4MapShape = 7,
        K4BinAmount = 8,
        K4Permutation = 9,
        K6ManagedLink = 10,
    }

    public static ulong Of(ulong entity, ulong tick, Purpose purpose) =>
        Mix(Mix(Mix(Seed ^ (entity * 0x9E37_79B9_7F4A_7C15UL)) ^ (tick + 0xBF58_476D_1CE4_E5B9UL))
            ^ ((ulong)purpose + 0xD6E8_FEB8_6659_FD93UL));

    private static ulong Mix(ulong z)
    {
        z ^= z >> 30;
        z *= 0xBF58_476D_1CE4_E5B9UL;
        z ^= z >> 27;
        z *= 0x94D0_49BB_1331_11EBUL;
        return z ^ (z >> 31);
    }
}
