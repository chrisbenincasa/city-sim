using System;
using System.Linq;
using Borough.Core.Arithmetic;

namespace Borough.Tests.Arithmetic;

/// <summary>
/// The point of these is not that division works. It is that the pot is conserved to the unit, that
/// nobody is paid more than they asked for, and — the one most likely to be quietly false — that the
/// order the claimants were handed over in did not decide who got paid.
/// </summary>
/// <remarks>
/// <para>
/// <b>The oracle sorts and the implementation bisects, which is what makes the comparison worth
/// making.</b> <see cref="LargestRemainder"/> deals the leftover units by ranking every claimant and
/// taking the top few, which is the textbook statement of the rule and is allowed to allocate because
/// it runs in a test. <c>Apportionment.Apportion</c> instead binary-searches for the remainder value
/// at which the deal runs out. Two implementations agreeing over an exhaustive small domain is a
/// stronger statement than either agreeing with a hand-written expectation.
/// </para>
/// <para>
/// <b>The permutation tests are exhaustive rather than sampled.</b> A permutation defect is a defect
/// in a tie-break, and a tie needs a particular arrangement to surface — sampling would find it on
/// most days and pass on the day it mattered. Six claimants is 720 orderings, which is cheap enough
/// to take all of them.
/// </para>
/// </remarks>
public class ApportionmentTests
{
    /// <summary>The example the design states: 200 and 100 against 150 are 100 and 50.</summary>
    [Fact]
    public void Claims_in_proportion_are_paid_in_proportion()
    {
        long[] awards = new long[2];
        long paid = Apportionment.Apportion([200, 100], 150, awards);

        Assert.Equal(150, paid);
        Assert.Equal(new long[] { 100, 50 }, awards);
    }

    /// <summary>
    /// Requirement 1 and 2 together, over every claim vector of up to four claimants drawn from a
    /// small alphabet and every pot that could interact with them. Conservation and the no-overpay
    /// bound are asserted on each, and the whole vector is compared against the oracle, so
    /// proportionality (requirement 3) is covered here too.
    /// </summary>
    [Fact]
    public void Every_small_case_conserves_the_pot_and_matches_largest_remainder()
    {
        long[] alphabet = [0, 1, 2, 3, 5];

        foreach (long[] claims in Vectors(alphabet, maxLength: 4))
        {
            long total = claims.Sum();

            for (long pot = 0; pot <= 16; pot++)
            {
                long[] awards = new long[claims.Length];
                long paid = Apportionment.Apportion(claims, pot, awards);

                Assert.Equal(pot < total ? pot : total, paid);
                Assert.Equal(paid, awards.Sum());

                for (int i = 0; i < claims.Length; i++)
                {
                    Assert.InRange(awards[i], 0, claims[i]);
                }

                Assert.Equal(LargestRemainder(claims, pot), awards);
            }
        }
    }

    /// <summary>
    /// Requirement 2's other half. A pot wider than the claims is not spent down to nothing: every
    /// claim is met in full and the return value says how much of the ceiling was used.
    /// </summary>
    [Fact]
    public void A_pot_larger_than_the_claims_pays_every_claim_in_full()
    {
        long[] awards = new long[3];
        long paid = Apportionment.Apportion([7, 11, 2], 1_000, awards);

        Assert.Equal(20, paid);
        Assert.Equal(new long[] { 7, 11, 2 }, awards);
    }

    /// <summary>
    /// Requirement 4. Claims that are equal in every respect are separated by position, because
    /// something has to separate them and nothing else distinguishes them.
    /// </summary>
    [Theory]
    [InlineData(1, new long[] { 1, 0, 0 })]
    [InlineData(2, new long[] { 1, 1, 0 })]
    [InlineData(4, new long[] { 2, 1, 1 })]
    public void Identical_claims_are_separated_by_index(long pot, long[] expected)
    {
        long[] awards = new long[3];
        Apportionment.Apportion([10, 10, 10], pot, awards);

        Assert.Equal(expected, awards);
    }

    /// <summary>
    /// Requirement 4's first clause, and the case that shows why index alone is not enough. Claims of
    /// 1 and 3 against a pot of 2 are cut by exactly the same fraction — both want a half — and there
    /// is one unit. It goes to the larger claim, and it goes to the larger claim from either
    /// direction. Breaking the tie by index would hand it to whichever of them was scanned first,
    /// which is the whole defect.
    /// </summary>
    [Fact]
    public void An_equal_cut_is_broken_by_the_larger_claim_and_not_by_position()
    {
        long[] ascending = new long[2];
        Apportionment.Apportion([1, 3], 2, ascending);
        Assert.Equal(new long[] { 0, 2 }, ascending);

        long[] descending = new long[2];
        Apportionment.Apportion([3, 1], 2, descending);
        Assert.Equal(new long[] { 2, 0 }, descending);
    }

    /// <summary>
    /// Requirement 5, and the reason the whole tie-break is layered the way it is. Every ordering of
    /// six distinct claims is apportioned, un-permuted, and compared claimant by claimant against the
    /// unpermuted answer. A leftover loop that deals in scan order passes every other test in this
    /// file and fails this one.
    /// </summary>
    [Fact]
    public void Permuting_the_claims_moves_no_payment()
    {
        long[] claims = [1, 3, 5, 7, 11, 13];

        for (long pot = 0; pot <= 40; pot++)
        {
            long[] expected = new long[claims.Length];
            long expectedPaid = Apportionment.Apportion(claims, pot, expected);

            foreach (int[] order in Permutations(claims.Length))
            {
                long[] permutedClaims = order.Select(i => claims[i]).ToArray();
                long[] permutedAwards = new long[claims.Length];
                long paid = Apportionment.Apportion(permutedClaims, pot, permutedAwards);

                Assert.Equal(expectedPaid, paid);

                for (int slot = 0; slot < order.Length; slot++)
                {
                    Assert.Equal(expected[order[slot]], permutedAwards[slot]);
                }
            }
        }
    }

    /// <summary>
    /// What requirement 5 can and cannot promise once two claimants are identical. One last unit
    /// cannot be given to both, so which of the two holds it follows position — but the set of
    /// payments handed out does not move, and that is the property a Money conservation argument
    /// actually rests on.
    /// </summary>
    [Fact]
    public void Permuting_repeated_claims_moves_no_payment_out_of_the_multiset()
    {
        long[] claims = [4, 4, 7, 1, 7];

        for (long pot = 0; pot <= 30; pot++)
        {
            long[] expected = new long[claims.Length];
            Apportionment.Apportion(claims, pot, expected);
            long[] expectedSorted = expected.Order().ToArray();

            foreach (int[] order in Permutations(claims.Length))
            {
                long[] awards = new long[claims.Length];
                Apportionment.Apportion(order.Select(i => claims[i]).ToArray(), pot, awards);

                Assert.Equal(expectedSorted, awards.Order().ToArray());
            }
        }
    }

    /// <summary>
    /// Requirement 6. None of these throws, and the two that could quietly invent a payment — a claim
    /// of zero standing beside real claims, and a pot too small to go round — do not.
    /// </summary>
    [Fact]
    public void No_claimant_is_paid_and_nothing_throws_when_there_is_nothing_to_share()
    {
        Assert.Equal(0, Apportionment.Apportion([], 100, []));

        long[] threeAwards = new long[3];
        Assert.Equal(0, Apportionment.Apportion([5, 5, 5], 0, threeAwards));
        Assert.Equal(new long[] { 0, 0, 0 }, threeAwards);

        Assert.Equal(0, Apportionment.Apportion([0, 0, 0], 100, threeAwards));
        Assert.Equal(new long[] { 0, 0, 0 }, threeAwards);

        long[] oneAward = new long[1];
        Assert.Equal(3, Apportionment.Apportion([7], 3, oneAward));
        Assert.Equal(new long[] { 3 }, oneAward);
    }

    /// <summary>
    /// A zero claim is not a small claim. It is cut by nothing, so it can never be among the most
    /// cut, and no leftover unit can reach it however tight the pot is.
    /// </summary>
    [Fact]
    public void A_claim_of_zero_is_never_paid()
    {
        for (long pot = 0; pot <= 8; pot++)
        {
            long[] awards = new long[4];
            Apportionment.Apportion([0, 1, 0, 1], pot, awards);

            Assert.Equal(0, awards[0]);
            Assert.Equal(0, awards[2]);
        }
    }

    /// <summary>
    /// One unit across many identical claims. The unit is not divided, not dropped, and not handed
    /// out twice.
    /// </summary>
    [Fact]
    public void A_single_unit_across_equal_claims_goes_to_exactly_one_claimant()
    {
        long[] awards = new long[64];
        long paid = Apportionment.Apportion(Enumerable.Repeat(9L, 64).ToArray(), 1, awards);

        Assert.Equal(1, paid);
        Assert.Equal(1, awards.Count(a => a == 1));
        Assert.Equal(1, awards.Sum());
    }

    /// <summary>
    /// Requirement 7, under the guard. A total of 3,037,000,499 is the largest that is safe whatever
    /// the shape of the claims, because the square of it is the last one to fit in 64 bits.
    /// </summary>
    [Fact]
    public void The_stated_safe_total_apportions_without_refusing_or_wrapping()
    {
        long[] claims = [1_012_333_500, 1_012_333_500, 1_012_333_499];
        long total = claims.Sum();
        Assert.Equal(3_037_000_499, total);

        long[] awards = new long[3];
        long paid = Apportionment.Apportion(claims, total - 1, awards);

        Assert.Equal(total - 1, paid);
        Assert.Equal(paid, awards.Sum());
        Assert.Equal(LargestRemainder(claims, total - 1), awards);

        for (int i = 0; i < claims.Length; i++)
        {
            Assert.InRange(awards[i], 0, claims[i]);
        }
    }

    /// <summary>
    /// Requirement 7, past the guard. The product the exact share needs does not fit, and the answer
    /// is a refusal rather than a wrapped negative payment that every replay would agree on.
    /// </summary>
    [Fact]
    public void A_claim_whose_product_with_the_pot_overflows_is_refused()
    {
        long[] claims = [4_000_000_000_000_000_000, 4_000_000_000_000_000_000];

        Assert.Throws<ArgumentOutOfRangeException>(
            () => Apportionment.Apportion(claims, 3, new long[2]));
    }

    /// <summary>The claims themselves may not sum past the range they are counted in.</summary>
    [Fact]
    public void Claims_summing_past_the_range_are_refused()
    {
        long[] claims = [long.MaxValue, 1];

        Assert.Throws<ArgumentOutOfRangeException>(
            () => Apportionment.Apportion(claims, 10, new long[2]));
    }

    /// <summary>
    /// A negative claim or a negative pot is a caller's defect, and reading it as zero would pay
    /// everyone else slightly more for ever without saying so.
    /// </summary>
    [Fact]
    public void Negative_inputs_are_refused_rather_than_read_as_zero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Apportionment.Apportion([5, -1, 5], 6, new long[3]));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => Apportionment.Apportion([5, 5], -1, new long[2]));
    }

    /// <summary>
    /// The output span has to be the caller's and has to be separate, because the claims are read
    /// again after the first payment is written.
    /// </summary>
    [Fact]
    public void Mismatched_or_aliasing_spans_are_refused()
    {
        Assert.Throws<ArgumentException>(
            () => Apportionment.Apportion([1, 2, 3], 4, new long[2]));

        long[] shared = [1, 2, 3];
        Assert.Throws<ArgumentException>(() => Apportionment.Apportion(shared, 4, shared));
    }

    /// <summary>
    /// Requirement 8. The caller supplies the output, so an apportionment run every Day out of a
    /// funding ceiling must not put anything on the heap.
    /// </summary>
    [Fact]
    public void Apportioning_allocates_nothing()
    {
        Span<long> claims = stackalloc long[64];
        Span<long> awards = stackalloc long[64];
        for (int i = 0; i < claims.Length; i++)
        {
            claims[i] = (i * 7) + 1;
        }

        // Once first, so that nothing being measured is first-call JIT.
        Apportionment.Apportion(claims, 100, awards);

        long jitMethods = System.Runtime.JitInfo.GetCompiledMethodCount(currentThread: true);
        long jitIl = System.Runtime.JitInfo.GetCompiledILBytes(currentThread: true);
        int gen0 = GC.CollectionCount(0);
        int gen1 = GC.CollectionCount(1);
        int gen2 = GC.CollectionCount(2);
        long before = GC.GetAllocatedBytesForCurrentThread();

        for (long pot = 1; pot <= 1_000; pot++)
        {
            Apportionment.Apportion(claims, pot, awards);
        }

        long after = GC.GetAllocatedBytesForCurrentThread();

        AllocationProbe.Check(
            "ApportionmentTests.Apportioning_allocates_nothing",
            before,
            after,
            gen0,
            gen1,
            gen2,
            jitMethods,
            jitIl);
    }

    /// <summary>
    /// The oracle: largest remainder stated the way a textbook states it, by ranking the claimants
    /// and taking the top few. It allocates and sorts, which is exactly what the implementation under
    /// test is not allowed to do, and that is the point of having it.
    /// </summary>
    private static long[] LargestRemainder(long[] claims, long pot)
    {
        long total = claims.Sum();
        if (pot >= total)
        {
            return [.. claims];
        }

        long[] awards = new long[claims.Length];
        long floored = 0;
        for (int i = 0; i < claims.Length; i++)
        {
            awards[i] = claims[i] * pot / total;
            floored += awards[i];
        }

        int[] mostCutFirst = [.. Enumerable.Range(0, claims.Length)
            .OrderByDescending(i => claims[i] * pot % total)
            .ThenByDescending(i => claims[i])
            .ThenBy(i => i)];

        for (long unit = 0; unit < pot - floored; unit++)
        {
            awards[mostCutFirst[unit]]++;
        }

        return awards;
    }

    /// <summary>Every claim vector of length 1 to <paramref name="maxLength"/> over an alphabet.</summary>
    private static IEnumerable<long[]> Vectors(long[] alphabet, int maxLength)
    {
        for (int length = 1; length <= maxLength; length++)
        {
            int[] digits = new int[length];
            while (true)
            {
                yield return [.. digits.Select(d => alphabet[d])];

                int place = length - 1;
                while (place >= 0 && digits[place] == alphabet.Length - 1)
                {
                    digits[place] = 0;
                    place--;
                }

                if (place < 0)
                {
                    break;
                }

                digits[place]++;
            }
        }
    }

    /// <summary>Every ordering of <paramref name="count"/> positions, in a fixed sequence.</summary>
    private static IEnumerable<int[]> Permutations(int count)
    {
        int[] order = [.. Enumerable.Range(0, count)];
        yield return [.. order];

        while (true)
        {
            int pivot = count - 2;
            while (pivot >= 0 && order[pivot] >= order[pivot + 1])
            {
                pivot--;
            }

            if (pivot < 0)
            {
                yield break;
            }

            int swap = count - 1;
            while (order[swap] <= order[pivot])
            {
                swap--;
            }

            (order[pivot], order[swap]) = (order[swap], order[pivot]);
            Array.Reverse(order, pivot + 1, count - pivot - 1);

            yield return [.. order];
        }
    }
}
