namespace Borough.Core.Entities;

using Borough.Core.Rules;
using Borough.Core.Tables;

/// <summary>
/// Where the money in a world is, summed once and split by the kind of thing holding it.
/// </summary>
/// <remarks>
/// <para>
/// <b>One walk with three readers, because the three must not be able to disagree about where money
/// lives.</b> <c>WorldInvariants.MoneyIsRepresentable</c> asks whether the sum can be represented,
/// <c>WorldInvariants.MoneyIsConserved</c> asks whether it equals the supply, and the
/// <c>Census</c> asks how it is distributed. Written separately they would each carry their own
/// notion of which Bins count, and a Bin one walked and another did not would surface as an overflow
/// reported under conservation's name or the reverse — one bug wearing another's.
/// </para>
/// <para>
/// <b><see cref="Total"/> is the walk and the four kinds are a decomposition of it</b>, rather than
/// the other way round. Keyed on <c>Ruleset.IsConserved</c> and never on the owner, which is
/// <c>adr/0114</c>'s point: a milestone adding a fifth owner kind is counted from the day it opens
/// its first Bin, where an enumeration of owners is a list somebody has to remember to extend.
/// </para>
/// <para>
/// <b><see cref="Elsewhere"/> is the residue, and it exists so that the decomposition cannot quietly
/// stop decomposing.</b> It is <see cref="Total"/> less the three named kinds, so it reads zero
/// today for a reason worth separating from the reasons a counter usually reads zero:
/// <c>adr/0113</c> says a Building never holds money, and no fifth owner kind exists. A nonzero
/// reading is either of those two facts having changed without this report knowing — which is the
/// one thing a report of named kinds cannot otherwise tell you, since the missing money would simply
/// not appear.
/// </para>
/// <para>
/// ⚠ <b>It saturates rather than throwing, and it does not report.</b> A sum that cannot be
/// represented is <c>Invariant.MoneyIsRepresentable</c>'s failure and that invariant is registered
/// to catch it; a second complaint from here would give one bug two names. <see cref="Representable"/>
/// is how a caller that must not read a saturated figure asks.
/// </para>
/// </remarks>
[ColdPath("a walk over every Bin: an invariant at end of run, the Census on an observation.")]
public readonly record struct MoneyLedger
{
    internal MoneyLedger(
        long total, long treasury, long households, long businesses, bool representable, int overflowed)
    {
        Total = total;
        Treasury = treasury;
        Households = households;
        Businesses = businesses;
        Representable = representable;
        Overflowed = overflowed;
    }

    /// <summary>Every live Bin holding a conserved Resource, whoever owns it. All the money there is.</summary>
    public long Total { get; }

    /// <summary>What the treasury holds. <c>01 §5.1</c>'s first money aggregate.</summary>
    public long Treasury { get; }

    /// <summary>What the Households hold, summed over every one of them.</summary>
    public long Households { get; }

    /// <summary>What the Businesses hold, summed over every one of them.</summary>
    public long Businesses { get; }

    /// <summary>
    /// <see cref="Total"/> less the three named kinds: money held by something this report does not
    /// name. Zero unless <c>adr/0113</c> has been reversed or a fifth owner kind exists.
    /// </summary>
    public long Elsewhere => Total - Treasury - Households - Businesses;

    /// <summary>
    /// Whether every figure above is a sum rather than a saturation.
    /// </summary>
    /// <remarks>
    /// False when adding a Bin's level would have left <see cref="long"/>. The figures are then the
    /// walk up to that Bin and are not the city's money.
    /// </remarks>
    public bool Representable { get; }

    /// <summary>The Bin the sum ran away on, or <see cref="Rows.NoSlot"/>.</summary>
    public int Overflowed { get; }

    /// <summary>
    /// Walks every live Bin once and splits the conserved ones by owner.
    /// </summary>
    /// <remarks>
    /// A Ruleset naming no money Resource conserves nothing, so every figure is zero and the readers
    /// above are vacuous rather than wrong.
    /// </remarks>
    /// <param name="world">The world to sum.</param>
    /// <returns>The ledger, whose <see cref="Representable"/> says whether to believe it.</returns>
    public static MoneyLedger Of(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        BinTable bins = world.Bins;

        long total = 0;
        long treasury = 0;
        long households = 0;
        long businesses = 0;

        for (int slot = 0; slot < bins.Rows.SlotCount; slot++)
        {
            if (!bins.Rows.IsLive(slot) || !world.Rules.IsConserved(bins.Resource[slot]))
            {
                continue;
            }

            long level = bins.LevelAt(slot);

            if (!TryAdd(ref total, level))
            {
                return new MoneyLedger(
                    total, treasury, households, businesses, representable: false, overflowed: slot);
            }

            // The per-kind sums are unguarded because each is a subset of a total that has just been
            // shown to fit, and a Bin's level is never negative -- World.Withdraw refuses a draw
            // larger than the level, so no subset can exceed the whole in magnitude.
            switch (bins.OwnerKind[slot])
            {
                case BinOwnerKind.Treasury:
                    treasury += level;
                    break;

                case BinOwnerKind.Household:
                    households += level;
                    break;

                case BinOwnerKind.Business:
                    businesses += level;
                    break;

                default:
                    // Falls into Elsewhere, which is the residue and is where it should show.
                    break;
            }
        }

        return new MoneyLedger(
            total, treasury, households, businesses, representable: true, overflowed: Rows.NoSlot);
    }

    /// <summary>
    /// Adds, or reports that it cannot.
    /// </summary>
    /// <remarks>
    /// Tested rather than caught. A <c>checked</c> block would express the same thing, but this check
    /// exists precisely because the sum is expected to be near its limit when it fires, and throwing
    /// to detect a condition you are deliberately looking for turns the diagnostic into the thing
    /// being diagnosed.
    /// </remarks>
    private static bool TryAdd(ref long total, long value)
    {
        if ((value > 0 && total > long.MaxValue - value)
            || (value < 0 && total < long.MinValue - value))
        {
            return false;
        }

        total += value;
        return true;
    }
}
