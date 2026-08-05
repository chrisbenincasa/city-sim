using System.Numerics;
using Borough.Core.Determinism;
using Borough.Core.Quantities;

namespace Borough.Tests.Determinism;

/// <summary>plans/0005 task 5. The hash is a format, so these tests are format conformance.</summary>
public class RandomnessTests
{
    private static ulong Draw(ulong seed, ulong entity, ulong tick, ulong purpose) =>
        Randomness.Draw(WorldKey.FromSeed(seed), entity, new Ticks(tick), (PurposeTag)purpose);

    // ---------------------------------------------------------------------------------------------
    // Known-answer vectors
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Committed outputs for fixed coordinates. <b>These are the format.</b> A change here is a change
    /// to every stored Input Log and State Hash baseline, so a diff touching this table should be
    /// treated as a save-format change and never as a test fix.
    /// </summary>
    /// <remarks>
    /// Computed independently of the implementation, in arbitrary-precision arithmetic, from adr/0003's
    /// pseudocode. The last row is the all-ones corner, which is where a wrong integer width shows.
    /// </remarks>
    public static TheoryData<ulong, ulong, ulong, ulong, ulong> Vectors => new()
    {
        { 0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL, 0x2130748AAAC80268UL },
        { 0x0000000000000001UL, 0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL, 0xE28195DDD9EE4956UL },
        { 0x0000000000000000UL, 0x0000000000000001UL, 0x0000000000000000UL, 0x0000000000000000UL, 0xD100827AA323C935UL },
        { 0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000001UL, 0x0000000000000000UL, 0x62A4ED4055D278DBUL },
        { 0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000001UL, 0x698DFBA9F1FC1B53UL },
        { 0xDEADBEEFCAFEF00DUL, 0x00000000000F4240UL, 0x0000000000002000UL, 0x0000000000000007UL, 0x991DE0ECF139CA05UL },
        { 0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0xA4A73720451A0C00UL },
    };

    [Theory]
    [MemberData(nameof(Vectors))]
    public void Draw_matches_its_committed_vectors(ulong seed, ulong entity, ulong tick, ulong purpose, ulong expected) =>
        Assert.Equal(expected, Draw(seed, entity, tick, purpose));

    /// <summary>The seed round is committed too, since it is half the function and is paid separately.</summary>
    [Theory]
    [InlineData(0x0000000000000000UL, 0xE220A8397B1DCDAFUL)]
    [InlineData(0x0000000000000001UL, 0x910A2DEC89025CC1UL)]
    [InlineData(0xDEADBEEFCAFEF00DUL, 0x901D4F652FB472CBUL)]
    [InlineData(0xFFFFFFFFFFFFFFFFUL, 0xE4D971771B652C20UL)]
    public void WorldKey_matches_its_committed_vectors(ulong seed, ulong expected) =>
        Assert.Equal(expected, WorldKey.FromSeed(seed).Raw);

    // ---------------------------------------------------------------------------------------------
    // The second implementation
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// A second implementation, transliterated from adr/0003 in <see cref="BigInteger"/> with the
    /// modulus written out explicitly, asserted to agree with the core's over a swept domain.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This does not test the property plans/0005 names, and saying so matters more than the test
    /// does.</b> The stated property is that <em>two people</em> can implement this from the document
    /// and get the same city; both implementations here were written by one author in one sitting, so a
    /// misreading of the ADR would simply appear in both. Only a genuinely separate reader can close
    /// that, and it stays owed.
    /// </para>
    /// <para>
    /// <b>What it does close is narrower and still worth having.</b> The core relies on C#'s
    /// <c>unchecked</c> <c>ulong</c> arithmetic being exactly mod 2⁶⁴, and on <c>&gt;&gt;</c> being a
    /// logical shift on an unsigned type. This version assumes neither: every reduction is an explicit
    /// <c>% 2⁶⁴</c> on an arbitrary-precision integer. If the core's wrapping were subtly wrong — the
    /// most likely way to get this function wrong at all — the two would part company here.
    /// </para>
    /// </remarks>
    [Fact]
    public void An_independent_implementation_agrees_across_a_swept_domain()
    {
        ulong[] coordinates = [0, 1, 2, 7, 8191, 8192, 1_000_000, ulong.MaxValue - 1, ulong.MaxValue];

        foreach (ulong seed in coordinates)
        {
            foreach (ulong entity in coordinates)
            {
                foreach (ulong tick in coordinates)
                {
                    Assert.Equal(ReferenceDraw(seed, entity, tick, 3), Draw(seed, entity, tick, 3));
                }
            }
        }
    }

    private static readonly BigInteger TwoPow64 = BigInteger.One << 64;

    private static BigInteger ReferenceMix(BigInteger z)
    {
        z %= TwoPow64;
        z ^= z >> 30;
        z = z * 0xBF58476D1CE4E5B9UL % TwoPow64;
        z ^= z >> 27;
        z = z * 0x94D049BB133111EBUL % TwoPow64;
        z ^= z >> 31;
        return z % TwoPow64;
    }

    private static ulong ReferenceDraw(ulong seed, ulong entity, ulong tick, ulong purpose)
    {
        BigInteger golden = 0x9E3779B97F4A7C15UL;
        BigInteger h = ReferenceMix(seed + golden);
        h = ReferenceMix(h + golden + entity);
        h = ReferenceMix(h + golden + tick);
        h = ReferenceMix(h + golden + purpose);
        return (ulong)(h % TwoPow64);
    }

    // ---------------------------------------------------------------------------------------------
    // The defect this function was amended to remove
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// adr/0003's original first round was <c>mix(seed + GOLDEN + entity)</c>, which added two
    /// externally chosen coordinates so that only their sum reached the hash. Rerolling the world seed
    /// by one produced the same world shifted by one entity.
    /// </summary>
    [Fact]
    public void Neighbouring_world_seeds_are_not_the_same_world_shifted_by_one_entity()
    {
        for (ulong entity = 0; entity < 16; entity++)
        {
            Assert.NotEqual(Draw(1000, entity + 1, 77, 5), Draw(1001, entity, 77, 5));
        }

        // The general statement: the seed and the entity id do not commute.
        Assert.NotEqual(Draw(100, 50, 42, 3), Draw(50, 100, 42, 3));
        Assert.NotEqual(Draw(1, 0, 0, 0), Draw(0, 1, 0, 0));
    }

    // ---------------------------------------------------------------------------------------------
    // Bijection over the counter domain
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Each coordinate is injective with the others held fixed, which is what "no collisions over the
    /// counter domain" means in practice: two entities never share a draw at the same Tick and purpose.
    /// </summary>
    [Fact]
    public void Each_coordinate_is_injective_with_the_others_held_fixed()
    {
        const int Samples = 100_000;

        HashSet<ulong> byEntity = [];
        HashSet<ulong> byTick = [];
        for (ulong i = 0; i < Samples; i++)
        {
            Assert.True(byEntity.Add(Draw(12345, i, 999, 3)), $"entity {i} collided");
            Assert.True(byTick.Add(Draw(12345, 7, i, 3)), $"tick {i} collided");
        }

        HashSet<ulong> byPurpose = [];
        for (ulong p = 0; p < 2000; p++)
        {
            Assert.True(byPurpose.Add(Draw(12345, 7, 999, p)), $"purpose {p} collided");
        }
    }

    /// <summary>
    /// The reason <see cref="PurposeTag"/> exists: two uses at the same coordinates must not correlate.
    /// A draw that ignored its purpose would pass every other test in this file.
    /// </summary>
    [Fact]
    public void Distinct_purposes_at_identical_coordinates_draw_differently() =>
        Assert.NotEqual(Draw(7, 11, 42, 1), Draw(7, 11, 42, 2));

    /// <summary>
    /// A draw is a pure function of its coordinates — the property that lets the Decide phase be
    /// parallelised later with zero coordination, and lets a single decision be reproduced in a test
    /// without replaying the run that produced it.
    /// </summary>
    [Fact]
    public void Draw_is_a_pure_function_of_its_coordinates()
    {
        for (int repeat = 0; repeat < 3; repeat++)
        {
            Assert.Equal(0x991DE0ECF139CA05UL, Draw(0xDEADBEEFCAFEF00DUL, 1_000_000, 8192, 7));
        }
    }

    /// <summary>
    /// A weak distribution check. It would not detect a poor hash, but it does detect a broken one —
    /// a lost mixing round or a shift in the wrong direction leaves a visible bias in the bit balance.
    /// </summary>
    [Fact]
    public void Output_bits_are_balanced()
    {
        const int Samples = 20_000;
        int ones = 0;
        for (ulong i = 0; i < Samples; i++)
        {
            ones += BitOperations.PopCount(Draw(7, i, 11, 2));
        }

        double mean = (double)ones / Samples;
        Assert.InRange(mean, 31.5, 32.5);
    }

    // ---------------------------------------------------------------------------------------------
    // WorldKey
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void A_world_key_is_derivable_only_from_a_seed_and_is_stable()
    {
        Assert.Equal(WorldKey.FromSeed(42), WorldKey.FromSeed(42));
        Assert.NotEqual(WorldKey.FromSeed(42), WorldKey.FromSeed(43));

        // The type carries a derivation invariant, so unlike the Quantities types it exposes no
        // constructor. A raw ulong is a valid Ticks; only a mixed seed is a valid WorldKey.
        Assert.DoesNotContain(
            typeof(WorldKey).GetConstructors(),
            constructor => constructor.IsPublic);
    }
}
