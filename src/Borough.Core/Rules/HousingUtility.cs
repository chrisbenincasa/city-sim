using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Quantities;

namespace Borough.Core.Rules;

/// <summary>
/// What one home is worth to one family, in <c>02 §5.4</c>'s utility units.
/// </summary>
/// <remarks>
/// <para>
/// <b>One kernel with four callers, and the fourth is why it exists.</b> A Household looking from
/// the Pool, a housed one weighing somewhere else, the Outside row in either comparison, and now a
/// prospect standing at a gate all score a place by the same two terms. ***A comparison whose two
/// sides are computed by different code is not a comparison*** — <c>plans/0073</c> D4 — and the
/// prospect path is where that stopped being theoretical: it weighed rent alone, against a taste of
/// zero, and would have made an arriving family's preferences appear for the first time after it had
/// already chosen.
/// </para>
/// <para>
/// <b>Two terms, both authored in domain units per utility unit.</b> A Ruleset states how many Tiles
/// and how much daily rent are worth one unit; nothing states a coefficient, and <c>adr/0023</c>'s
/// rule is why — a constant that cannot be read off a panel is a balance hazard, and utility is not a
/// thing anybody sees.
/// </para>
/// <para>
/// ⚠ <b><see cref="LifeStageDefinition.RentWeightPercent"/> scales rent against centrality and is not
/// an affordability discount.</b> What a family can pay is the filter in
/// <c>PlacementEngine.Consider</c> and it is untouched here: a stage weighing rent at zero still
/// cannot move into what it cannot afford, it simply stops trading distance away to save money.
/// </para>
/// </remarks>
public static class HousingUtility
{
    private const int Neutral = Ruleset.RentNeutralPercent;

    /// <summary>
    /// Where a family in <paramref name="stage"/> sits on <c>adr/0027</c>'s centrality axis, as a
    /// direction and a strength in one number.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The position runs 0 (wants room) to <see cref="Fixed.One"/> (wants the middle). <c>2T − One</c>
    /// re-centres that on zero, so the SIGN says which way this family leans and the MAGNITUDE says
    /// how hard. Scoring <c>distance × weight</c> and taking the smallest does both jobs at once.
    /// </para>
    /// <para>
    /// 🔴 <b>A neutral family weighs exactly zero, and that is load-bearing rather than neat.</b>
    /// Every candidate scores the same on this term, so the mechanism is continuous with the
    /// behaviour it replaces at the midpoint of the axis — <c>centrality_base_percent = 50</c> is not
    /// a special case anybody had to write.
    /// </para>
    /// <para>
    /// ⚠ <b>The gate on <see cref="Ruleset.CentralityVaries"/> is about the DRAW and not the
    /// value.</b> A world whose stages all sit at the midpoint would get zero either way; what it
    /// would not get is the hash it has, because the scored branch looks at every candidate in the
    /// budget where the old accept stops at the first with room.
    /// </para>
    /// </remarks>
    /// <param name="rules">The Rules in force.</param>
    /// <param name="key">The world's key.</param>
    /// <param name="identity">
    /// Whose taste this is. A Household's monotonic id, or a prospect's choice identity — which is
    /// the whole of what makes the family that compared equal the family that arrives.
    /// </param>
    /// <param name="stage">The Life Stage that identity is in now.</param>
    public static long Taste(Ruleset rules, WorldKey key, ulong identity, byte stage)
    {
        ArgumentNullException.ThrowIfNull(rules);

        return rules.CentralityVaries
            ? (2L * rules.CentralityTaste(key, identity, stage)) - Fixed.One
            : 0L;
    }

    /// <summary>
    /// What a place costing <paramref name="rent"/> a Day, <paramref name="centralityTiles"/> from
    /// the middle, is worth to somebody whose taste is <paramref name="taste"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>Rent appears here as well as in the affordability filter, and they are different
    /// questions.</b> <i>Can this family pay at all</i> eliminates a candidate before scoring, which
    /// is <c>02 §5.4</c>'s <i>hard constraints are filters</i>; <i>is it worth what it costs</i> is
    /// the trade-off against distance and belongs in the sum. Neither substitutes for the other.
    /// </para>
    /// <para>
    /// ⚠ <b>The rent weight applies to the whole rent term and not to the money.</b> Scaling the
    /// money would move which dwellings a family can afford, which is the filter's question; scaling
    /// the term moves only what the family will pay to be somewhere it likes.
    /// </para>
    /// </remarks>
    /// <param name="placement">The choice model's units. Both scales must be stated.</param>
    /// <param name="centralityTiles">How far the place is from the nearest centre, in Tiles.</param>
    /// <param name="taste">The looker's weight on the centrality axis, from <see cref="Taste"/>.</param>
    /// <param name="rent">What the place charges a Day.</param>
    /// <param name="rentWeightPercent">How heavily this Life Stage weighs rent, 100 being neutral.</param>
    public static int Worth(
        in PlacementRuleset placement,
        long centralityTiles,
        long taste,
        Money rent,
        int rentWeightPercent)
    {
        long centrality = -IntegerMath.FloorDiv(
            centralityTiles * taste, placement.CentralityTilesPerUnit);

        // Widened through the scale first and weighted afterwards, so the rounding happens in one
        // order for every caller. Weighting the money and dividing afterwards would put a second
        // floor between a stage and its rent, and two stages at 100 would then disagree with the
        // world that has no stage table at all.
        long charged = IntegerMath.FloorDiv(rent.Raw * Fixed.One, placement.RentPerUnit);

        long weighed = rentWeightPercent == Neutral
            ? charged
            : IntegerMath.FloorDiv(charged * rentWeightPercent, Neutral);

        return Saturate(centrality - weighed);
    }

    /// <summary>
    /// A utility sum held inside what Q16.16 represents.
    /// </summary>
    /// <remarks>
    /// <b>Clamping cannot change a choice</b> — <see cref="Choice"/> gives every candidate past
    /// <c>adr/0038</c>'s horizon a weight of exactly zero, and the clamp sits four orders beyond it.
    /// The alternative is an overflow exception standing in for a Ruleset nobody would write.
    /// </remarks>
    public static int Saturate(long utility) => utility > Fixed.MaxValue
        ? Fixed.MaxValue
        : utility < Fixed.MinValue ? Fixed.MinValue : (int)utility;
}
