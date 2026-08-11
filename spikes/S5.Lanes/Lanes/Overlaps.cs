namespace S5.Lanes.Lanes;

/// <summary>
/// The Overlap exchange: <c>adr/0016</c>'s <i>"Lanes that physically interact declare an Overlap
/// and exchange their Vehicles' projected positions once per Tick, mapped into each other's
/// coordinate space as ordinary obstacles."</i>
/// </summary>
/// <remarks>
/// <para>
/// <b>The exchange is where one dimension buys two, and it is also the only part of
/// <c>adr/0016</c> with no stated cost anywhere in the corpus.</b> The car-following claim is
/// O(n) with a memcpy constant and says so; the Overlap claim says only that the exchange happens.
/// What it costs depends entirely on how a Lane finds the Vehicle in its partner that is near the
/// conflict point, and that is a data-structure decision nobody has taken.
/// </para>
/// <para>
/// So both plausible answers are measured rather than one being assumed. <b>Scan</b> walks the
/// partner's queue from its head until it reaches the window, which is what a first implementation
/// writes and costs half a queue per Overlap on average. <b>Cursor</b> keeps the queue index found
/// last Tick and steps it, which is O(1) amortised because a Vehicle moves at most 1.05 Tiles in a
/// Tick against a jam spacing of 1.75. The gap between the two rows is the price of the
/// bookkeeping, and it is a design question rather than an optimisation: the cursor is state the
/// Overlap owns and promotion has to materialise.
/// </para>
/// <para>
/// The projected obstacle sits at the Overlap's own position in the receiving Lane, which is what a
/// crossing conflict is: something in front of you at a known distance. Its velocity is the
/// partner Vehicle's. A Lane with no Vehicle near the window emits no obstacle at all, which is the
/// common case and is why the exchange is cheaper than the queue pass despite touching two Lanes.
/// </para>
/// </remarks>
internal static class Overlaps
{
    /// <summary>
    /// Half-width of the conflict window, Q16.16 Tiles. 2 Tiles is 8 m — a junction mouth.
    /// </summary>
    public static readonly int WindowHalfWidth = Borough.Core.Arithmetic.Fixed.FromInt(2);

    /// <summary>Walks the partner queue from its head. The naive implementation, priced.</summary>
    public static void ExchangeByScan(LaneNetwork n)
    {
        int lanes = n.Lanes;
        int per = n.OverlapsPerLane;

        for (int lane = 0; lane < lanes; lane++)
        {
            int emitted = 0;
            int baseIndex = lane * per;

            for (int o = 0; o < per; o++)
            {
                int slot = baseIndex + o;
                int partner = n.OverlapPartner[slot];
                int partnerCount = n.Count[partner];
                if (partnerCount == 0)
                {
                    continue;
                }

                int partnerBlock = n.BlockStart[partner];
                int partnerHead = n.Head[partner];
                int window = n.OverlapWindow[slot];
                int high = window + WindowHalfWidth;
                int low = window - WindowHalfWidth;

                int j = partnerHead;
                int found = -1;
                for (int k = 0; k < partnerCount; k++)
                {
                    int p = n.Position[partnerBlock + j];
                    if (p <= high)
                    {
                        if (p >= low)
                        {
                            found = partnerBlock + j;
                        }

                        break;
                    }

                    j++;
                    if (j == partnerCount)
                    {
                        j = 0;
                    }
                }

                if (found >= 0)
                {
                    n.ObstaclePosition[baseIndex + emitted] = window;
                    n.ObstacleVelocity[baseIndex + emitted] = n.Velocity[found];
                    emitted++;
                }
            }

            n.ObstacleCount[lane] = emitted;
        }
    }

    /// <summary>
    /// Steps the cached queue index found last Tick. O(1) amortised, at the cost of one <c>int</c>
    /// per Overlap that promotion must materialise and demotion must discard.
    /// </summary>
    public static void ExchangeByCursor(LaneNetwork n)
    {
        int lanes = n.Lanes;
        int per = n.OverlapsPerLane;

        for (int lane = 0; lane < lanes; lane++)
        {
            int emitted = 0;
            int baseIndex = lane * per;

            for (int o = 0; o < per; o++)
            {
                int slot = baseIndex + o;
                int partner = n.OverlapPartner[slot];
                int partnerCount = n.Count[partner];
                if (partnerCount == 0)
                {
                    continue;
                }

                int partnerBlock = n.BlockStart[partner];
                int partnerHead = n.Head[partner];
                int window = n.OverlapWindow[slot];
                int high = window + WindowHalfWidth;
                int low = window - WindowHalfWidth;

                int j = n.OverlapCursor[slot];
                if (j >= partnerCount)
                {
                    j = partnerCount - 1;
                }

                // Back up while the Vehicle before this one is already at or below the window —
                // the head rotating past the start line shifts every index by one, so the cursor
                // can be stale in either direction by a bounded amount.
                while (j > 0 && Ring(n, partnerBlock, partnerHead, partnerCount, j - 1) <= high)
                {
                    j--;
                }

                while (j < partnerCount && Ring(n, partnerBlock, partnerHead, partnerCount, j) > high)
                {
                    j++;
                }

                n.OverlapCursor[slot] = j < partnerCount ? j : partnerCount - 1;

                if (j < partnerCount && Ring(n, partnerBlock, partnerHead, partnerCount, j) >= low)
                {
                    n.ObstaclePosition[baseIndex + emitted] = window;
                    n.ObstacleVelocity[baseIndex + emitted] =
                        n.Velocity[partnerBlock + RingIndex(partnerHead, partnerCount, j)];
                    emitted++;
                }
            }

            n.ObstacleCount[lane] = emitted;
        }
    }

    private static int RingIndex(int head, int count, int j)
    {
        int i = head + j;
        return i >= count ? i - count : i;
    }

    private static int Ring(LaneNetwork n, int block, int head, int count, int j) =>
        n.Position[block + RingIndex(head, count, j)];
}
