using Borough.Core.Arithmetic;

namespace S5.Lanes.Lanes;

/// <summary>
/// Materialising Lane queues from in-flight Trips, and converting them back to arrival Ticks.
/// <c>adr/0016</c>: <i>"Promotion therefore has to materialise Lane queues from in-flight Trips,
/// and demotion has to convert them back to arrival Ticks."</i>
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the half of <c>adr/0016</c> that carries its own named revisit trigger</b> —
/// <i>"promotion cost dominating the traffic budget"</i> — and the ADR names no machine that could
/// evaluate it. The trigger is a ratio, so the answer is a ratio: promotion plus demotion per
/// Vehicle, against the cost of running that Vehicle for one Tick. Their quotient is a
/// <b>break-even residency</b> in Ticks, and it is the number the hysteresis window in
/// <c>adr/0007</c> has to clear.
/// </para>
/// <para>
/// <b>The Travellers are found through an intrusive index list, because that is the structure the
/// project mandates</b> (<c>CLAUDE.md</c>: <i>"Every variable-length collection in
/// <c>Borough.Core</c> is an intrusive index list — a head index on the owner, a <c>next</c> index
/// on the element"</i>). A promotion therefore begins with a pointer chase through Traveller rows
/// in whatever order they joined the Segment, which is not the order they sit in memory. Modelling
/// it as a contiguous scan would have made promotion look free and would have been a fixture
/// choice, not a measurement.
/// </para>
/// <para>
/// <b>Sorting is the second half and it is unavoidable.</b> A Lane queue is sorted by position, and
/// the Statistical tier holds no order at all — a Traveller there is an origin, a destination and
/// an arrival Tick. Insertion sort is what a real implementation writes at these lengths: a Lane
/// holds 18 Vehicles at a standstill, so the quadratic term is small and the constant is what
/// matters.
/// </para>
/// </remarks>
internal sealed class PromotionFixture
{
    public required int Segments { get; init; }

    public required int LanesPerSegment { get; init; }

    public required int TravellersPerSegment { get; init; }

    /// <summary>Head of each Segment's in-flight Traveller list. <c>-1</c> when empty.</summary>
    public required int[] ListHead { get; init; }

    /// <summary>The intrusive <c>next</c> index. <c>-1</c> terminates.</summary>
    public required int[] Next { get; init; }

    /// <summary>Q16.16 Tiles along the Segment.</summary>
    public required int[] Progress { get; init; }

    /// <summary>Q16.16 Tiles per Tick.</summary>
    public required int[] Speed { get; init; }

    /// <summary>The Citizen this Traveller is.</summary>
    public required int[] TravellerId { get; init; }

    /// <summary>Arrival Tick written back by demotion.</summary>
    public required int[] ArrivalTick { get; init; }

    public required LaneNetwork Network { get; init; }

    /// <summary>
    /// Vehicles demotion could not give an arrival Tick because they were stopped.
    /// </summary>
    /// <remarks>
    /// Not an implementation detail. <c>03</c> invariant 3 requires that what is discarded on
    /// demotion be enumerated, and a Vehicle at rest in a jam has no <c>distance / speed</c> to
    /// convert to. Counting them is how the spike reports that the conversion is not total.
    /// </remarks>
    public int Stalled { get; set; }

    public static PromotionFixture Build(
        int segments, int lanesPerSegment, int travellersPerSegment, ulong seed)
    {
        int travellers = segments * travellersPerSegment;

        var fixture = new PromotionFixture
        {
            Segments = segments,
            LanesPerSegment = lanesPerSegment,
            TravellersPerSegment = travellersPerSegment,
            ListHead = new int[segments],
            Next = new int[travellers],
            Progress = new int[travellers],
            Speed = new int[travellers],
            TravellerId = new int[travellers],
            ArrivalTick = new int[travellers],
            Network = LaneNetwork.Build(
                segments * lanesPerSegment,
                // A Lane must have room for every Traveller promotion could give it. The draw is
                // uniform over the Segment's Lanes, so the block is sized for the worst case
                // rather than the mean — a real implementation refuses at the Cap instead
                // (adr/0062), which is a decision this spike must not take.
                travellersPerSegment,
                100,
                0,
                seed),
        };

        // The list is threaded in a shuffled order so that walking it chases pointers, which is
        // what walking an intrusive list over rows that joined at different times actually does.
        var order = new int[travellers];
        for (int i = 0; i < travellers; i++)
        {
            order[i] = i;
        }

        for (int i = travellers - 1; i > 0; i--)
        {
            int j = Draw.Below(seed, (ulong)i, 11, i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }

        for (int segment = 0; segment < segments; segment++)
        {
            fixture.ListHead[segment] = -1;
        }

        // A Lane is a Lane of a 128 m Segment whatever the block is sized for. LaneNetwork.Build
        // derives a length from the block, which is what the sweeps want and is wrong here: the
        // block is sized for the worst-case Lane assignment and the road is not.
        int laneLength = Fixed.FromInt(Units.SegmentLengthTiles);
        for (int lane = 0; lane < fixture.Network.Lanes; lane++)
        {
            fixture.Network.LaneLength[lane] = laneLength;
        }

        for (int k = 0; k < travellers; k++)
        {
            int traveller = order[k];
            int segment = IntegerMath.FloorDiv(k, travellersPerSegment);

            fixture.Next[traveller] = fixture.ListHead[segment];
            fixture.ListHead[segment] = traveller;

            fixture.Progress[traveller] = Draw.Below(seed, (ulong)traveller, 12, laneLength);
            fixture.Speed[traveller] = Units.FreeFlowSpeed;
            fixture.TravellerId[traveller] = traveller;
        }

        return fixture;
    }

    public int Travellers => Segments * TravellersPerSegment;
}

/// <summary>The two conversions, timed separately because they are not symmetric.</summary>
internal static class Fidelity
{
    /// <summary>
    /// Materialises every Segment's Lane queues from its in-flight Traveller list.
    /// </summary>
    public static void Promote(PromotionFixture fixture)
    {
        LaneNetwork n = fixture.Network;
        int lanesPer = fixture.LanesPerSegment;

        for (int segment = 0; segment < fixture.Segments; segment++)
        {
            int firstLane = segment * lanesPer;

            for (int lane = firstLane; lane < firstLane + lanesPer; lane++)
            {
                n.Count[lane] = 0;
                n.Head[lane] = 0;
            }

            for (int traveller = fixture.ListHead[segment];
                 traveller >= 0;
                 traveller = fixture.Next[traveller])
            {
                // Which Lane a Traveller lands in is a design question S5 does not own — turn
                // intent decides it. A draw off the Traveller's own id is the neutral placeholder
                // and it costs the same as the real rule would.
                int lane = firstLane + (traveller & (lanesPer - 1));
                int block = n.BlockStart[lane];
                int count = n.Count[lane];

                int position = fixture.Progress[traveller];
                int speed = fixture.Speed[traveller];
                int id = fixture.TravellerId[traveller];

                // Insertion into a queue sorted descending by position. The Statistical tier holds
                // no order, so this sort is the conversion rather than an optimisation of it.
                int i = count - 1;
                while (i >= 0 && n.Position[block + i] < position)
                {
                    n.Position[block + i + 1] = n.Position[block + i];
                    n.Velocity[block + i + 1] = n.Velocity[block + i];
                    n.DesiredSpeed[block + i + 1] = n.DesiredSpeed[block + i];
                    n.VehicleId[block + i + 1] = n.VehicleId[block + i];
                    i--;
                }

                n.Position[block + i + 1] = position;
                n.Velocity[block + i + 1] = speed;
                n.DesiredSpeed[block + i + 1] = Units.FreeFlowSpeed;
                n.VehicleId[block + i + 1] = id;
                n.Count[lane] = count + 1;
            }
        }
    }

    /// <summary>
    /// Converts every Lane queue back to arrival Ticks and rebuilds the Segment's Traveller list.
    /// </summary>
    public static void Demote(PromotionFixture fixture, int tick)
    {
        LaneNetwork n = fixture.Network;
        int lanesPer = fixture.LanesPerSegment;
        int stalled = 0;

        for (int segment = 0; segment < fixture.Segments; segment++)
        {
            int firstLane = segment * lanesPer;
            int head = -1;

            for (int lane = firstLane; lane < firstLane + lanesPer; lane++)
            {
                int block = n.BlockStart[lane];
                int count = n.Count[lane];
                int length = n.LaneLength[lane];

                for (int k = 0; k < count; k++)
                {
                    int slot = block + k;
                    int traveller = n.VehicleId[slot];
                    int remaining = length - n.Position[slot];
                    int speed = n.Velocity[slot];

                    if (speed < Units.GapFloor)
                    {
                        // A Vehicle at rest has no distance/speed to convert to. It is counted
                        // rather than given an invented arrival, because 03 invariant 3 requires
                        // what demotion discards to be enumerated.
                        stalled++;
                        speed = Units.GapFloor;
                    }

                    fixture.Progress[traveller] = n.Position[slot];
                    fixture.Speed[traveller] = n.Velocity[slot];
                    fixture.ArrivalTick[traveller] =
                        tick + Fixed.ToIntFloor(Fixed.Div(remaining, speed));

                    fixture.Next[traveller] = head;
                    head = traveller;
                }

                n.Count[lane] = 0;
                n.Head[lane] = 0;
            }

            fixture.ListHead[segment] = head;
        }

        fixture.Stalled = stalled;
    }
}
