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
    /// Fills <paramref name="into"/> with the live Lot slots one trigger evaluates.
    /// </summary>
    /// <param name="lots">The Lot table, whose slot count is the population.</param>
    /// <param name="into">Where to write the slots. Its length is the trigger's derived sample.</param>
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
    /// nothing and is <em>already the model</em>: a draw landing on a freed slot is a parcel the
    /// developer looked at and could not use.
    /// </para>
    /// <para>
    /// <b>So the return value can be less than the sample size, and that is not a degradation.</b> The
    /// sample size is how many Lots are *evaluated*, which is the quantity <c>02 §5.7</c> talks about
    /// and the one task 9's tripwire holds fixed. How many of them turn out to be usable is a property
    /// of the city.
    /// </para>
    /// <para>
    /// <b>Sampling is with replacement, and a repeated draw is evaluated twice</b> — task 11c of
    /// <c>plans/0014</c>, and it reverses what this method used to do. The scan that found a duplicate
    /// was linear in what had been drawn so far, so quadratic in the sample, and it was justified by
    /// <em>a sample is a handful of Lots</em>. <c>adr/0059</c> killed that premise: the sample is now
    /// <c>lots × interval ÷ revisit_ticks</c> and therefore proportional to the map — 469 draws at
    /// 120,001 Lots, and quadratic in a quantity that grows with the city is a defect whether or not
    /// today's profile can afford it.
    /// </para>
    /// <para>
    /// <b>What decided it was that the scan never bought coverage.</b> Deduplicating <em>within</em> a
    /// trigger does not make a trigger reach more Lots than drawing with replacement does — the same
    /// slots come up either way and the scan only skipped the second look — so the per-Lot revisit
    /// period is unchanged by removing it. What it bought was avoiding a doubled *evaluation*, and
    /// <c>02 §5.3</c>'s criticism of UrbanSim is about a doubled *weight*: that model samples with
    /// replacement and double-counts an alternative when scoring it. Here the create predicate is a
    /// boolean with no score, so a duplicate costs one wasted evaluation and biases nothing. <b>That
    /// stops being true the day <c>02 §5.4</c>'s choice model arrives</b>, and it is the trigger for
    /// putting a stamp array in rather than the scan back.
    /// </para>
    /// <para>
    /// <b>And the measured rate is negligible and scale-free</b>, which is the number the choice wanted
    /// rather than the argument above. The duplicate fraction depends on <c>sample ÷ lots</c>, which
    /// <c>adr/0059</c> makes exactly <c>interval ÷ revisit_ticks</c> — a property of the file and not of
    /// the city — so one measurement settles every city size at once.
    /// <c>Duplicates_are_negligible_at_the_shipped_revisit_period</c> is where it lives.
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

            if (!lots.Rows.IsLive(slot))
            {
                continue;
            }

            into[found++] = slot;
        }

        return found;
    }
}
