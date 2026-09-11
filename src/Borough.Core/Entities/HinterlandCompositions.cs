namespace Borough.Core.Entities;

using Borough.Core.Determinism;
using Borough.Core.Space;
using Borough.Core.Tables;

/// <summary>
/// Which row holds a given composition behind a given edge. The lookup a return needs.
/// </summary>
/// <remarks>
/// <para>
/// <b>A flat open-addressed table, because the alternative is banned and the walk is the wrong
/// shape.</b> <c>05 §4</c> lint 3 forbids a <c>Dictionary</c> in simulation code, and a linear scan of
/// an edge's list would be fine at four authored compositions and quadratic in a city that has been
/// sending families back for a hundred thousand Ticks — the reachable key space is every stage against
/// every tier split against every child count against every purse band.
/// </para>
/// <para>
/// <b>Lookup and insertion only, and a retirement rebuilds.</b> Linear probing cannot delete without
/// tombstones, and a tombstone is a key the index keeps after the row is gone — which is
/// <c>plans/0073</c> D1's stated refusal and an <c>adr/0006</c> leak besides. A group is retired when
/// it empties, which is rare, so paying a whole rebuild for it buys the guarantee that this structure
/// holds exactly the live rows and nothing else.
/// </para>
/// <para>
/// <b><c>(derived AND rebuilt)</c>, and it lives outside the table on <c>BOR0901</c>'s rule</b> —
/// <see cref="Parking.CarParkResidency"/>'s shape, for its reason. Every entry is a pure function of
/// the saved composition columns, so <c>World.RebuildDerived</c> reproduces it exactly.
/// </para>
/// </remarks>
public sealed class HinterlandCompositions
{
    private const int SmallestBuckets = 16;

    private int[] _buckets = [];

    /// <summary>How many rows the index currently holds.</summary>
    public int Count { get; private set; }

    /// <summary>Rebuilds the whole index from the live rows.</summary>
    public void Rebuild(HinterlandPopulationTable groups)
    {
        ArgumentNullException.ThrowIfNull(groups);

        Count = 0;

        int wanted = SmallestBuckets;

        while (wanted < groups.Rows.LiveCount * 2)
        {
            wanted *= 2;
        }

        if (_buckets.Length != wanted)
        {
            _buckets = new int[wanted];
        }
        else
        {
            Array.Clear(_buckets);
        }

        for (int slot = 0; slot < groups.Rows.SlotCount; slot++)
        {
            if (groups.Rows.IsLive(slot))
            {
                Insert(groups, slot);
            }
        }
    }

    /// <summary>Indexes one newly opened group.</summary>
    public void Add(HinterlandPopulationTable groups, int slot)
    {
        ArgumentNullException.ThrowIfNull(groups);

        if ((Count + 1) * 2 > _buckets.Length)
        {
            Rebuild(groups);
            return;
        }

        Insert(groups, slot);
    }

    /// <summary>Drops one retired group, by rebuilding around it.</summary>
    /// <remarks>
    /// ⚠ <b>Call it after the row is freed</b>, since what the rebuild walks is the live set.
    /// </remarks>
    public void Remove(HinterlandPopulationTable groups) => Rebuild(groups);

    /// <summary>The row holding this composition behind this edge, if there is one.</summary>
    public bool TryFind(
        HinterlandPopulationTable groups,
        MapEdge edge,
        in HinterlandComposition composition,
        out int slot)
    {
        ArgumentNullException.ThrowIfNull(groups);

        slot = Rows.NoSlot;

        if (_buckets.Length == 0)
        {
            return false;
        }

        int mask = _buckets.Length - 1;
        int bucket = (int)(KeyOf(edge, composition) & (ulong)(uint)mask);

        for (int probe = 0; probe <= mask; probe++)
        {
            int encoded = _buckets[(bucket + probe) & mask];

            if (encoded == 0)
            {
                return false;
            }

            if (groups.Holds(encoded - 1, edge, composition))
            {
                slot = encoded - 1;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The bucket a composition behind an edge starts probing at.
    /// </summary>
    /// <remarks>
    /// <b><see cref="Randomness.Mix"/> rather than a hand-rolled fold</b>, because it is SplitMix64's
    /// finaliser and already in the project for the property this needs: it spreads adjacent inputs,
    /// and a child count of two against a child count of three has to land in different buckets. It is
    /// not a draw and takes no <c>purpose_tag</c> — nothing here is random.
    /// </remarks>
    private static ulong KeyOf(MapEdge edge, in HinterlandComposition composition)
    {
        ulong key = Randomness.Mix((ulong)edge);

        key = Randomness.Mix(key ^ composition.Stage);
        key = Randomness.Mix(key ^ (ulong)(uint)composition.AdultsTier1);
        key = Randomness.Mix(key ^ (ulong)(uint)composition.AdultsTier2);
        key = Randomness.Mix(key ^ (ulong)(uint)composition.AdultsTier3);
        key = Randomness.Mix(key ^ (ulong)(uint)composition.Children);

        return Randomness.Mix(key ^ (ulong)(uint)composition.MoneyBand);
    }

    private void Insert(HinterlandPopulationTable groups, int slot)
    {
        int mask = _buckets.Length - 1;
        var edge = (MapEdge)groups.Edge[slot];
        int bucket = (int)(KeyOf(edge, groups.CompositionAt(slot)) & (ulong)(uint)mask);

        for (int probe = 0; probe <= mask; probe++)
        {
            int at = (bucket + probe) & mask;

            if (_buckets[at] == 0)
            {
                _buckets[at] = slot + 1;
                Count++;
                return;
            }

            if (_buckets[at] == slot + 1)
            {
                return;
            }
        }

        throw new InvalidOperationException(
            $"the composition index is full at {_buckets.Length} buckets holding {Count} groups. "
            + "Insertion keeps it under half full, so reaching this means a caller indexed a group "
            + "without growing the table.");
    }
}
