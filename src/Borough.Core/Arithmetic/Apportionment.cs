namespace Borough.Core.Arithmetic;

/// <summary>
/// Shares a capped pot across weighted claims, conserving every unit.
/// </summary>
/// <remarks>
/// <para>
/// <b>The problem is that proportion and integers disagree, and something has to absorb the
/// disagreement.</b> Claims of 200 and 100 against a pot of 150 want 100 and 50 and get them
/// exactly; claims of 1 and 2 against a pot of 2 want 0.667 and 1.333 and there is no such payment.
/// Flooring every share underspends the pot, rounding every share overspends it, and both are wrong
/// in a way that compounds: a subsidy paid every Day out of a ceiling that is never quite reached is
/// a slow leak of Money into or out of the city. The units that cannot be divided are therefore
/// <em>dealt</em> rather than rounded, and this is the rule by which they are dealt.
/// </para>
/// <para>
/// <b>Largest remainder, which is the Hare quota apportionment.</b> Each claimant first receives the
/// floor of its exact share. The floors are short of the pot by some whole number of units strictly
/// fewer than the number of claimants, and those units go one apiece to the claimants whose exact
/// shares were cut by the most. Nobody receives two of them. The alternative families — highest
/// averages, or simply paying in scan order until the pot runs dry — either need a division per unit
/// dealt or make the answer depend on who was looked at first, and the second of those is the defect
/// this exists to refuse.
/// </para>
/// <para>
/// <b>The tie-break is a rule of the design and not an implementation detail, because a replay has to
/// reproduce the payments.</b> Two claimants can be cut by exactly the same amount while only one
/// unit is left. The order is then: <em>the larger claim wins; if the claims are also equal, the
/// lower index wins.</em> Both halves are needed and they are not interchangeable. Deciding by index
/// alone would be deterministic within a single run and still wrong, because the caller scans its
/// members from a rotating start — the same two claimants arrive in a different order on a different
/// Tick, and the payment would follow the scan rather than the claim.
/// </para>
/// <para>
/// <b>What order independence therefore means here, stated exactly.</b> Permute the claims,
/// apportion, un-permute, and every claimant holds what it held before — <em>provided its claim
/// value is unique</em>. Two claimants holding the identical claim are indistinguishable to every
/// rule stated above, and one last unit cannot be given to both, so which of them receives it follows
/// position. That residue is not avoidable by a better rule: it is the pigeonhole. What is guaranteed
/// unconditionally is that the <em>multiset</em> of payments does not depend on order, and that
/// claimants with distinct claims are never reordered against each other.
/// </para>
/// <para>
/// <b>Negative inputs throw rather than being read as zero</b>, following
/// <see cref="IntegerMath.Abs(int)"/>: there is no correct answer, so the loud wrong answer beats the
/// quiet one. A negative claim is not a small claim — it is a caller that has confused a debt with a
/// demand, and reading it as zero would pay every other claimant slightly more for ever and never say
/// why. A negative pot is the same defect at the other end.
/// </para>
/// </remarks>
public static class Apportionment
{
    /// <summary>
    /// Deals <paramref name="pot"/> across <paramref name="claims"/> in proportion, writing each
    /// claimant's payment into <paramref name="awards"/> and returning the total paid.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Four properties hold for every accepted input</b>, and each is a test rather than a
    /// comment. The sum of <paramref name="awards"/> is exactly the lesser of <paramref name="pot"/>
    /// and the sum of the claims — no unit is lost to rounding and none is invented. No claimant is
    /// paid more than it claimed, so a pot larger than the total claimed pays every claim in full and
    /// returns less than the pot. A claim of zero is never paid. And the payments depend on the
    /// claims and not on their order, in the sense <see cref="Apportionment"/> states.
    /// </para>
    /// <para>
    /// <b>The dangerous product is <c>claim × pot</c> and it is guarded rather than widened.</b> An
    /// exact share needs that product before the division, and it is the only place 64 bits can run
    /// out. The pot is smaller than the total claimed in every case that reaches the multiply, so a
    /// sufficient condition is that the total claimed squared fits: any city whose claims sum to at
    /// most <b>3,037,000,499</b> — about 3.0 × 10⁹ of the smallest Money unit — is safe whatever the
    /// shape of the claims. Above that the bound is the exact one, the largest single claim times the
    /// pot, and only inputs that exceed <em>that</em> are refused. Refusing is deliberate: widening to
    /// 128 bits would buy a range no city reaches at the cost of making every apportionment slower,
    /// and a silent wrap here is the defect class the State Hash cannot find, since both runs wrap
    /// alike and the replay agrees.
    /// </para>
    /// <para>
    /// <b>Cost is <c>O(n log T)</c> in the claim count and the total claimed, and nothing is
    /// allocated.</b> The leftover units are placed by binary-searching for the remainder value at
    /// which the deal runs out, not by repeatedly scanning for the next largest, which would be
    /// quadratic in the claim count — a subsidy claimed by every Household in a District is not a
    /// small span.
    /// </para>
    /// </remarks>
    /// <param name="claims">What each claimant asked for. Every entry must be non-negative.</param>
    /// <param name="pot">The ceiling. Must be non-negative.</param>
    /// <param name="awards">
    /// Receives each claimant's payment. Must be the same length as <paramref name="claims"/> and
    /// must not overlap it.
    /// </param>
    /// <returns>The total paid, which is the lesser of the pot and the total claimed.</returns>
    /// <exception cref="ArgumentException">
    /// The spans differ in length, or they overlap in memory.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The pot is negative, a claim is negative, the claims sum past <see cref="long.MaxValue"/>, or
    /// the largest claim times the pot does.
    /// </exception>
    public static long Apportion(ReadOnlySpan<long> claims, long pot, Span<long> awards)
    {
        if (awards.Length != claims.Length)
        {
            throw new ArgumentException(
                "An award must be written for every claim.", nameof(awards));
        }

        if (claims.Overlaps(awards))
        {
            throw new ArgumentException(
                "The claims are read again after the awards are written, so the two spans must be "
                + "distinct.", nameof(awards));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(pot);

        long total = 0;
        long largestClaim = 0;
        for (int i = 0; i < claims.Length; i++)
        {
            long claim = claims[i];
            if (claim < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(claims), claim, "A claim is a demand and cannot be negative.");
            }

            if (claim > long.MaxValue - total)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(claims), claim, "The claims sum past the range of a 64-bit Money value.");
            }

            total += claim;
            if (claim > largestClaim)
            {
                largestClaim = claim;
            }
        }

        if (pot >= total)
        {
            claims.CopyTo(awards);
            return total;
        }

        if (pot == 0)
        {
            awards.Clear();
            return 0;
        }

        if (largestClaim > long.MaxValue / pot)
        {
            throw new ArgumentOutOfRangeException(
                nameof(claims),
                largestClaim,
                "The largest claim times the pot exceeds the range of a 64-bit Money value, so the "
                + "exact share cannot be formed.");
        }

        long floored = 0;
        for (int i = 0; i < claims.Length; i++)
        {
            long share = claims[i] * pot / total;
            awards[i] = share;
            floored += share;
        }

        long leftover = pot - floored;
        if (leftover == 0)
        {
            return pot;
        }

        long remainderFloor = SmallestRemainderDealt(claims, pot, total, leftover);
        long tiedUnits = leftover - CountRemainderAtLeast(claims, pot, total, remainderFloor + 1);

        long claimFloor = SmallestClaimDealt(claims, pot, total, remainderFloor, tiedUnits);
        long positionalUnits =
            tiedUnits - CountTiedClaimAtLeast(claims, pot, total, remainderFloor, claimFloor + 1);

        for (int i = 0; i < claims.Length; i++)
        {
            long remainder = claims[i] * pot % total;
            if (remainder > remainderFloor)
            {
                awards[i]++;
            }
            else if (remainder == remainderFloor)
            {
                if (claims[i] > claimFloor)
                {
                    awards[i]++;
                }
                else if (claims[i] == claimFloor && positionalUnits > 0)
                {
                    awards[i]++;
                    positionalUnits--;
                }
            }
        }

        return pot;
    }

    /// <summary>
    /// The smallest remainder that still receives a leftover unit — the <paramref name="leftover"/>th
    /// largest remainder, found by bisecting the value range rather than the claims.
    /// </summary>
    /// <remarks>
    /// <b>The search is well-founded at both ends and needs no guard.</b> Every remainder is below the
    /// total claimed, so nothing reaches the upper bound; and the leftover is strictly fewer than the
    /// number of claimants with a non-zero remainder, because a leftover unit is a sum of fractions
    /// each strictly below one. That second fact is also what keeps a claim of zero unpaid: its
    /// remainder is zero, the answer here is therefore at least one, and it never qualifies.
    /// </remarks>
    private static long SmallestRemainderDealt(
        ReadOnlySpan<long> claims, long pot, long total, long leftover)
    {
        long dealt = 0;
        long undealt = total;

        while (undealt - dealt > 1)
        {
            long probe = dealt + (undealt - dealt) / 2;
            if (CountRemainderAtLeast(claims, pot, total, probe) >= leftover)
            {
                dealt = probe;
            }
            else
            {
                undealt = probe;
            }
        }

        return dealt;
    }

    /// <summary>
    /// The smallest claim among those cut by exactly <paramref name="remainderFloor"/> that still
    /// receives one of the <paramref name="tiedUnits"/> units left for them to share.
    /// </summary>
    /// <remarks>
    /// The upper bound is the total claimed rather than the largest claim, which is sound because a
    /// claim equal to the total belongs to a sole claimant, who takes the whole pot exactly and
    /// leaves no remainder to break.
    /// </remarks>
    private static long SmallestClaimDealt(
        ReadOnlySpan<long> claims, long pot, long total, long remainderFloor, long tiedUnits)
    {
        long dealt = 0;
        long undealt = total;

        while (undealt - dealt > 1)
        {
            long probe = dealt + (undealt - dealt) / 2;
            if (CountTiedClaimAtLeast(claims, pot, total, remainderFloor, probe) >= tiedUnits)
            {
                dealt = probe;
            }
            else
            {
                undealt = probe;
            }
        }

        return dealt;
    }

    /// <summary>How many claimants were cut by at least <paramref name="threshold"/>.</summary>
    private static int CountRemainderAtLeast(
        ReadOnlySpan<long> claims, long pot, long total, long threshold)
    {
        int counted = 0;
        for (int i = 0; i < claims.Length; i++)
        {
            if (claims[i] * pot % total >= threshold)
            {
                counted++;
            }
        }

        return counted;
    }

    /// <summary>
    /// How many claimants cut by exactly <paramref name="remainderFloor"/> claimed at least
    /// <paramref name="threshold"/>.
    /// </summary>
    private static int CountTiedClaimAtLeast(
        ReadOnlySpan<long> claims, long pot, long total, long remainderFloor, long threshold)
    {
        int counted = 0;
        for (int i = 0; i < claims.Length; i++)
        {
            if (claims[i] * pot % total == remainderFloor && claims[i] >= threshold)
            {
                counted++;
            }
        }

        return counted;
    }
}
