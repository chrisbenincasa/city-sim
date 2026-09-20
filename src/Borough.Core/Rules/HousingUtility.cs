using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Quantities;
using Borough.Core.Entities;
using Borough.Core.Space;

namespace Borough.Core.Rules;

/// <summary>
/// What one home is worth to one family, in <c>02 §5.4</c>'s utility units.
/// </summary>
/// <remarks>
/// Residents and prospects use the same rent and centrality terms. Rent weight affects
/// preference; affordability remains a separate hard filter.
/// </remarks>
public static class HousingUtility
{
    private const int Neutral = Ruleset.RentNeutralPercent;

    /// <summary>
    /// Where a family in <paramref name="stage"/> sits on <c>adr/0027</c>'s centrality axis, as a
    /// direction and a strength in one number.
    /// </summary>
    /// <remarks>
    /// Maps centrality taste from 0..Fixed.One to -Fixed.One..Fixed.One; midpoint is neutral.
    /// </remarks>
    /// <param name="rules">The Rules in force.</param>
    /// <param name="key">The world's key.</param>
    /// <param name="identity">
    /// Household monotonic id, or the retained prospect choice identity after admission.
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
    /// Scale rent into utility units before applying the stage weight, preserving one rounding
    /// order across every caller.
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

        // Scale first, then weight, to keep the same rounding order for every caller.
        long charged = IntegerMath.FloorDiv(rent.Raw * Fixed.One, placement.RentPerUnit);

        long weighed = rentWeightPercent == Neutral
            ? charged
            : IntegerMath.FloorDiv(charged * rentWeightPercent, Neutral);

        return Saturate(centrality - weighed);
    }

    /// <summary>
    /// A utility sum held inside what Q16.16 represents.
    /// </summary>
    public static int Saturate(long utility) => utility > Fixed.MaxValue
        ? Fixed.MaxValue
        : utility < Fixed.MinValue ? Fixed.MinValue : (int)utility;
    internal static long Distance(World world, int lot)
    {
        long east = world.Lots.East[lot].Raw, north = world.Lots.North[lot].Raw;
        if (world.Rules.Lattices.Length == 0) { return Abs(east) + Abs(north); }
        long nearest = long.MaxValue;
        foreach (LatticeDefinition lattice in world.Rules.Lattices)
        {
            long distance = Abs(east - lattice.OriginEastTiles) + Abs(north - lattice.OriginNorthTiles);
            if (distance < nearest) { nearest = distance; }
        }
        return nearest;
    }

    internal static bool TryOutside(World world, int position, long weight, int rentWeight, out int worth)
    {
        MapEdge edge;
        if (world.Buildings.Rows.TryResolve(world.UnplacedPool.GateAt(position), out int gate)
            && world.Lots.Rows.TryResolve(world.Buildings.Lot[gate], out int lot))
        {
            edge = world.EdgeOf(lot);
        }
        else
        {
            int household = world.Households.Rows.Resolve(world.UnplacedPool.At(position));
            edge = world.Households.Arrived[household] == 0 ? MapEdge.None
                : (MapEdge)world.Households.ArrivalEdge[household];
        }
        worth = 0;
        if (edge == MapEdge.None || !world.Rules.TryHinterland(edge, out HinterlandDefinition hinterland)) { return false; }
        worth = Worth(world.Rules.Placement, hinterland.CentralityTiles, weight, hinterland.Rent, rentWeight);
        return true;
    }

    internal static bool Affordable(World world, int position, byte kind)
    {
        Money rent = world.Rules.Kind(kind).Rent;
        return rent.Raw <= 0 || world.BalanceOf(world.UnplacedPool.At(position)).Raw >= rent.Raw;
    }

    // A tie with Outside is not positive evidence for new construction. Existing capacity at a tie
    // still suppresses construction, so a probabilistic placement loss cannot manufacture shortage.
    internal static bool Suitable(World world, WorldKey key, int position, int lot, byte kind, bool prospective)
    {
        int household = world.Households.Rows.Resolve(world.UnplacedPool.At(position));
        Money rent = world.Rules.Kind(kind).Rent;
        if (!Affordable(world, position, kind)) { return false; }
        if (!world.Rules.Placement.Chooses) { return true; }
        byte stage = world.Households.LifeStage[household];
        long taste = Taste(world.Rules, key, world.Households.TasteIdentity(household), stage);
        int rentWeight = world.Rules.RentWeight(stage);
        if (!TryOutside(world, position, taste, rentWeight, out int outside)) { return true; }
        int inside = Worth(world.Rules.Placement, Distance(world, lot), taste, rent, rentWeight);
        return prospective ? inside > outside : inside >= outside;
    }

    private static long Abs(long value) => value < 0 ? -value : value;
}
