namespace Borough.Core.Rules;

using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Tables;

/// <summary>
/// Which Lots a Zone Rule looks at on one trigger.
/// </summary>
/// <remarks>
/// <para>
/// <b>Sampling is the behaviour model, not an optimisation</b> (<c>02 §5.3</c>, <c>CONTEXT</c> → Zone
/// Rule). A developer does not evaluate every parcel in the city, so a Zone Rule does not either. That
/// it also keeps growth cost constant regardless of Zone size is a second benefit and not the reason —
/// which matters, because the two justifications would be satisfied by different code if the first
/// were dropped.
/// </para>
/// <para>
/// <b>The population is every Lot</b> (<c>adr/0055</c>). The Rule's permission bit is a term in its
/// create predicate, never a filter on what it draws from — a Rule that only looked at Lots already
/// carrying its bit would let a player repaint a Lot and put the Building on it beyond the reach of
/// everything, which is immortality by paintbrush.
/// </para>
/// </remarks>
public static class ZoneSample
{
    /// <summary>
    /// Fills <paramref name="into"/> with the distinct live Lot slots one trigger evaluates.
    /// </summary>
    /// <param name="lots">The Lot table, whose slot count is the population.</param>
    /// <param name="into">Where to write the slots. Its length is the Ruleset's sample size.</param>
    /// <param name="key">The world key.</param>
    /// <param name="tick">The Tick this trigger fires on, which is what makes each one differ.</param>
    /// <param name="rule">The Zone Rule's index in declaration order.</param>
    /// <returns>How many slots were written, which may be fewer than <paramref name="into"/> holds.</returns>
    /// <remarks>
    /// <para>
    /// <b>Exactly <c>into.Length</c> draws, and a draw that lands badly is discarded rather than
    /// retried.</b> That is the whole of the algorithm, and it is a design decision rather than a
    /// simplification. A retry loop would need an attempt budget; an attempt budget is a number that
    /// changes which Lots get built on, so it is hash-bearing, so under <c>adr/0052</c> it would need a
    /// named ratifier — for a quantity nobody has ever wanted to reason about. Discarding instead costs
    /// nothing and is <em>already the model</em>: a draw landing on a freed slot or on a Lot this
    /// trigger has seen is a parcel the developer looked at and could not use.
    /// </para>
    /// <para>
    /// <b>So the return value can be less than the sample size, and that is not a degradation.</b> The
    /// sample size is how many Lots are *evaluated*, which is the quantity <c>02 §5.7</c> talks about
    /// and the one task 9's tripwire holds fixed. How many of them turn out to be usable is a property
    /// of the city.
    /// </para>
    /// <para>
    /// <b>Duplicates are discarded rather than counted twice, which is <c>02 §5.3</c>'s one criticism
    /// of UrbanSim.</b> It samples *with* replacement and double-counts an alternative's weight; the
    /// section calls that a real if minor defect. Copying it knowingly would be worse than copying it
    /// unknowingly. The scan that finds a duplicate is linear over what has been drawn so far, which is
    /// the right shape at these sizes — a sample is a handful of Lots, and a set would allocate,
    /// hash, and be walked in an order <c>05 §4</c> lint 3 bans.
    /// </para>
    /// <para>
    /// <b>Allocation-free and <c>O(sample)</c> exactly</b>, with no dependence on the Lot count in
    /// either. The tripwire in task 9 measures that claim rather than trusting this paragraph.
    /// </para>
    /// </remarks>
    public static int Draw(LotTable lots, Span<int> into, WorldKey key, Ticks tick, int rule)
    {
        ArgumentNullException.ThrowIfNull(lots);

        int slots = lots.Rows.SlotCount;

        if (slots == 0)
        {
            return 0;
        }

        int found = 0;

        for (int draw = 0; draw < into.Length; draw++)
        {
            // The Rule and the draw ordinal together, so that two Zone Rules triggering on one Tick
            // do not sample the same Lots, and the draws within one trigger do not repeat. The shift
            // count is constant, which is what keeps BOR0204 quiet: it is the *count* that may not
            // vary, because C# would mask it against the operand width.
            ulong entity = Randomness.Mix((ulong)(uint)rule ^ ((ulong)(uint)draw << 32));
            ulong value = Randomness.Draw(key, entity, tick, PurposeTag.ZoneRuleSample);

            int slot = (int)(value % (ulong)(uint)slots);

            if (!lots.Rows.IsLive(slot) || Contains(into[..found], slot))
            {
                continue;
            }

            into[found++] = slot;
        }

        return found;
    }

    /// <summary>Whether this trigger has already drawn that slot.</summary>
    private static bool Contains(ReadOnlySpan<int> drawn, int slot)
    {
        foreach (int seen in drawn)
        {
            if (seen == slot)
            {
                return true;
            }
        }

        return false;
    }
}
