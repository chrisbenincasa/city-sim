using Borough.Core.Arithmetic;

namespace S5.Lanes.Lanes;

/// <summary>
/// A network of Lanes, each holding its Vehicles as a sorted one-dimensional queue.
/// <c>adr/0016</c>'s structure and nothing else.
/// </summary>
/// <remarks>
/// <para>
/// <b>Struct of arrays, one arena, no per-Lane collection object.</b> A Lane owns a contiguous
/// block of slots; the queue inside that block is a ring, so a Vehicle leaving the far end becomes
/// the tail without anything being moved. <c>adr/0016</c> says the queue is compacted as Vehicles
/// leave and that anything holding a bare Vehicle reference across a Tick is a defect — the ring is
/// the cheapest structure with that property, and it means the measured pass contains the
/// bookkeeping rather than excluding it.
/// </para>
/// <para>
/// <b>Each Lane is a ring road, and that is a deliberate fixture choice with a cost.</b> A ring
/// keeps the queue sorted forever with no source and no sink, which is what lets a kernel
/// measurement run for thousands of Ticks without an entry model that S5 has no business inventing
/// — Trip generation is milestone 5b's. It also produces real stop-and-go waves, so the kernel is
/// exercised in the regime the Microscopic tier exists for rather than in free flow only. What it
/// does not measure is Lane-to-Lane handover at a node, which is named in the plan as out of scope
/// and would be a second spike.
/// </para>
/// <para>
/// The Vehicle row is four <c>int</c> columns — position, velocity, desired speed, id — so
/// <b>16 bytes per Vehicle</b>. Desired speed is per Vehicle rather than per Ruleset because
/// <c>UNIQUE INDIVIDUALS</c> means drivers differ, and because leaving it out would understate the
/// row by a quarter and flatter every bandwidth ratio in the report.
/// </para>
/// </remarks>
internal sealed class LaneNetwork
{
    public const int BytesPerVehicleRow = 4 * sizeof(int);

    /// <summary>Q16.16 Tiles along the Lane. Descending from the head of the ring.</summary>
    public required int[] Position { get; init; }

    /// <summary>Q16.16 Tiles per Tick.</summary>
    public required int[] Velocity { get; init; }

    /// <summary>Q16.16 Tiles per Tick. The driver's own <c>v0</c>.</summary>
    public required int[] DesiredSpeed { get; init; }

    /// <summary>The Traveller this Vehicle carries. Written back on demotion; never dereferenced.</summary>
    public required int[] VehicleId { get; init; }

    /// <summary>
    /// Q16.16. <c>1 / v0</c> for this driver — a fifth column that exists only for the reciprocal
    /// variant of the kernel, and the reason that variant's row is 20 bytes rather than 16.
    /// </summary>
    public required int[] InverseDesiredSpeed { get; init; }

    /// <summary>Index of each Lane's slot block in the arena.</summary>
    public required int[] BlockStart { get; init; }

    /// <summary>Vehicles in each Lane.</summary>
    public required int[] Count { get; init; }

    /// <summary>Offset within the block of the Lane's leading Vehicle.</summary>
    public required int[] Head { get; init; }

    /// <summary>Q16.16 Tiles. The ring's circumference.</summary>
    public required int[] LaneLength { get; init; }

    /// <summary>Q16.16 Tiles. Where each Lane's Overlap window sits, one per Overlap slot.</summary>
    public required int[] OverlapWindow { get; init; }

    /// <summary>The partner Lane of each Overlap slot.</summary>
    public required int[] OverlapPartner { get; init; }

    /// <summary>Cached queue index of the last Vehicle found in an Overlap window.</summary>
    public required int[] OverlapCursor { get; init; }

    /// <summary>Q16.16 Tiles. Obstacle positions injected into a Lane by its Overlaps this Tick.</summary>
    public required int[] ObstaclePosition { get; init; }

    /// <summary>Q16.16 Tiles per Tick, matched to <see cref="ObstaclePosition"/>.</summary>
    public required int[] ObstacleVelocity { get; init; }

    /// <summary>Obstacles currently declared for each Lane.</summary>
    public required int[] ObstacleCount { get; init; }

    public required int Lanes { get; init; }

    public required int OverlapsPerLane { get; init; }

    public required int Vehicles { get; init; }

    /// <summary>
    /// Builds a network of <paramref name="lanes"/> Lanes each holding
    /// <paramref name="vehiclesPerLane"/> Vehicles at the stated occupancy.
    /// </summary>
    /// <param name="occupancyPercent">
    /// Vehicles as a percentage of what the Lane holds at a standstill. 100 is a solid jam; the
    /// realistic promoted rung is high but not solid, because a Segment is promoted when it is
    /// stressed rather than when it has stopped.
    /// </param>
    public static LaneNetwork Build(
        int lanes, int vehiclesPerLane, int occupancyPercent, int overlapsPerLane, ulong seed)
    {
        int capacity = lanes * vehiclesPerLane;

        // Lane length follows from the occupancy rather than the other way round, so that sweeping
        // the queue length sweeps the queue length and not the density with it. At 100% the Lane is
        // exactly jam spacing per Vehicle.
        int lengthPerVehicle = Fixed.Div(
            Fixed.Mul(Units.JamSpacing, Fixed.FromInt(100)), Fixed.FromInt(occupancyPercent));
        int laneLength = lengthPerVehicle * vehiclesPerLane;

        var network = new LaneNetwork
        {
            Position = new int[capacity],
            Velocity = new int[capacity],
            DesiredSpeed = new int[capacity],
            VehicleId = new int[capacity],
            InverseDesiredSpeed = new int[capacity],
            BlockStart = new int[lanes],
            Count = new int[lanes],
            Head = new int[lanes],
            LaneLength = new int[lanes],
            OverlapWindow = new int[lanes * overlapsPerLane],
            OverlapPartner = new int[lanes * overlapsPerLane],
            OverlapCursor = new int[lanes * overlapsPerLane],
            ObstaclePosition = new int[lanes * overlapsPerLane],
            ObstacleVelocity = new int[lanes * overlapsPerLane],
            ObstacleCount = new int[lanes],
            Lanes = lanes,
            OverlapsPerLane = overlapsPerLane,
            Vehicles = capacity,
        };

        for (int lane = 0; lane < lanes; lane++)
        {
            int block = lane * vehiclesPerLane;
            network.BlockStart[lane] = block;
            network.Count[lane] = vehiclesPerLane;
            network.Head[lane] = 0;
            network.LaneLength[lane] = laneLength;

            for (int k = 0; k < vehiclesPerLane; k++)
            {
                int slot = block + k;

                // Descending from the head, evenly spaced, with a small deterministic jitter so the
                // queue is not a lattice. A perfectly even ring is a fixed point of the IDM and
                // would measure a kernel that never brakes.
                int jitter = Draw.Below(seed, (ulong)slot, 1, 8192) - 4096;
                network.Position[slot] =
                    (lengthPerVehicle * (vehiclesPerLane - 1 - k)) + jitter;

                // Start at free flow. The first few hundred Ticks are the wave forming, which is
                // why the harness warms before it times.
                network.Velocity[slot] = Units.FreeFlowSpeed;

                // ±10% desired speed, per driver.
                int spread = Draw.Below(seed, (ulong)slot, 2, 20) - 10;
                network.DesiredSpeed[slot] = Units.FreeFlowSpeed
                    + Fixed.Div(Fixed.Mul(Units.FreeFlowSpeed, Fixed.FromInt(spread)), Fixed.FromInt(100));

                network.InverseDesiredSpeed[slot] =
                    Fixed.Div(Fixed.One, network.DesiredSpeed[slot]);

                network.VehicleId[slot] = slot;
            }
        }

        for (int i = 0; i < lanes * overlapsPerLane; i++)
        {
            // A partner drawn from the whole network rather than from a neighbourhood. That is the
            // pessimistic placement and it is the honest one for this question: an Overlap is
            // declared from geometry, and Lane ids are not laid out by geometry in any structure
            // this project has committed to.
            network.OverlapPartner[i] = Draw.Below(seed, (ulong)i, 3, lanes);
            network.OverlapWindow[i] = Draw.Below(seed, (ulong)i, 4, laneLength);
            network.OverlapCursor[i] = 0;
        }

        // Each Lane's Overlaps are ordered by where they sit along it, descending, so that the
        // obstacles the exchange emits arrive at the queue pass already sorted. This is geometry
        // and it is fixed when the Road Graph is edited — adr/0016: "Overlaps are declared, not
        // discovered." Sorting them per Tick would be pricing a defect rather than the design.
        for (int lane = 0; lane < lanes; lane++)
        {
            int baseIndex = lane * overlapsPerLane;
            for (int a = 1; a < overlapsPerLane; a++)
            {
                int window = network.OverlapWindow[baseIndex + a];
                int partner = network.OverlapPartner[baseIndex + a];
                int b = a - 1;
                while (b >= 0 && network.OverlapWindow[baseIndex + b] < window)
                {
                    network.OverlapWindow[baseIndex + b + 1] = network.OverlapWindow[baseIndex + b];
                    network.OverlapPartner[baseIndex + b + 1] = network.OverlapPartner[baseIndex + b];
                    b--;
                }

                network.OverlapWindow[baseIndex + b + 1] = window;
                network.OverlapPartner[baseIndex + b + 1] = partner;
            }
        }

        return network;
    }

    /// <summary>Bytes of Vehicle row this network holds, which is the denominator's numerator.</summary>
    public long VehicleBytes => (long)Vehicles * BytesPerVehicleRow;
}
