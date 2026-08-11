namespace S5.Lanes.Lanes;

/// <summary>
/// The denominator: the same walk over the same arrays in the same order, with the car-following
/// arithmetic removed.
/// </summary>
/// <remarks>
/// <para>
/// <b>This exists to test one sentence.</b> <c>adr/0016</c> says car-following down a sorted queue
/// is <i>"O(n) in the number of Vehicles with the constant of a <c>memcpy</c>"</i>. That is a claim
/// about a ratio, and a ratio needs a denominator taken on the same machine, in the same
/// configuration, over the same layout — which is why it is measured here rather than divided
/// against S4's recorded bandwidth. S4's figure was taken under a different governor on a different
/// day, and dividing across that is a ratio between two machines.
/// </para>
/// <para>
/// It is deliberately a <b>lower bound on the traffic</b> rather than an exact match: three loads
/// and two stores per Vehicle, the same ring bookkeeping, and nothing else. A kernel cannot beat
/// it, so <c>IDM ÷ this</c> is an upper bound on the true multiple of memory-bound cost.
/// </para>
/// </remarks>
internal static class Denominator
{
    /// <summary>Walks every Lane's queue, touching every Vehicle row. Returns a checksum.</summary>
    public static long Touch(LaneNetwork n)
    {
        long checksum = 0;
        int[] position = n.Position;
        int[] velocity = n.Velocity;
        int[] desired = n.DesiredSpeed;

        for (int lane = 0; lane < n.Lanes; lane++)
        {
            int count = n.Count[lane];
            if (count == 0)
            {
                continue;
            }

            int block = n.BlockStart[lane];
            int head = n.Head[lane];

            int i = head;
            for (int k = 0; k < count; k++)
            {
                int slot = block + i;
                int p = position[slot];
                int v = velocity[slot];
                int d = desired[slot];

                checksum += p + v + d;

                position[slot] = p;
                velocity[slot] = v;

                i++;
                if (i == count)
                {
                    i = 0;
                }
            }
        }

        return checksum;
    }
}
