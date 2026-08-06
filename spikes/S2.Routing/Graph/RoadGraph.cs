using Borough.Core.Arithmetic;

namespace S2.Routing.Graph;

/// <summary>One column of the graph's storage, so the footprint is reported per column rather than as a total.</summary>
/// <param name="Name">The column, named as the design names it.</param>
/// <param name="Group">Nodes, Segments, Arcs or Volume — the axis its length scales on.</param>
/// <param name="Count">Elements.</param>
/// <param name="BytesEach">Width of one element.</param>
/// <param name="Derived">
/// True for <c>(derived AND rebuilt)</c> storage — never saved, free to change forever. Reported
/// separately because it is the part of the footprint a later optimisation may simply delete.
/// </param>
internal sealed record ColumnFootprint(string Name, string Group, int Count, int BytesEach, bool Derived)
{
    public long Bytes => (long)Count * BytesEach;
}

/// <summary>
/// The synthetic Road Graph. Nodes and directed arcs, uniform regardless of how a road was drawn
/// (<c>CONTEXT.md</c> → Road Graph: <i>"The simulation never sees a spline"</i>).
/// </summary>
/// <remarks>
/// <para>
/// <b>The graph is directed, and that is settled rather than chosen.</b> <c>plans/0010</c>'s gate
/// section derives it from the Segment definition: the VDF is evaluated on one Segment's own
/// <c>volume / capacity</c>, Lanes are directional queues, so <c>cost(A→B) ≠ cost(B→A)</c> on one
/// Segment. Three consequences follow and all three had to be true from R0's first line, because
/// none is a local retrofit — the route cache key is an <i>ordered</i> node pair, the travel-time
/// matrix is asymmetric and nothing may halve it by symmetry, and this structure stores two arcs
/// where an undirected graph would store one edge.
/// </para>
/// <para>
/// <b>Structure of arrays, CSR by source node.</b> The same layout the core's tables use and the
/// same layout S4's K1 and K2 measured, so the numbers R0 produces are comparable with the ones
/// already recorded rather than being about a different data structure.
/// </para>
/// <para>
/// <b>What is <c>(saved AND hashed)</c> and what is <c>(derived AND rebuilt)</c> is marked, and the
/// distinction is load-bearing for the footprint curve rather than decorative.</b> Under
/// <c>adr/0040</c> the whole of the abstract routing structure is derived and therefore free to
/// change forever; the same is true of the per-arc traversal times below, which are a cache of a
/// division. A footprint that does not separate them cannot tell the reader which half a later
/// optimisation is allowed to delete.
/// </para>
/// </remarks>
internal sealed class RoadGraph
{
    /// <summary>Sentinel traversal time for an arc a mode may not use. Never reached by a search.</summary>
    public const int Impassable = int.MaxValue;

    public required GraphParameters Parameters { get; init; }

    // --- Nodes. Intersections for Streets, authored Junction pieces for Arterials. -------------

    /// <summary>Node x, in Tiles. <c>(saved AND hashed)</c>.</summary>
    public required int[] NodeX { get; init; }

    /// <summary>Node y, in Tiles. <c>(saved AND hashed)</c>.</summary>
    public required int[] NodeY { get; init; }

    // --- Segments. The edge of the Road Graph, CONTEXT.md's definition exactly. ----------------

    /// <summary>The Segment's first node. <c>(saved AND hashed)</c>.</summary>
    public required int[] SegmentNodeA { get; init; }

    /// <summary>The Segment's second node. <c>(saved AND hashed)</c>.</summary>
    public required int[] SegmentNodeB { get; init; }

    /// <summary>
    /// Run length in Tiles. <b>Arc length, not the distance between the endpoints</b> — a freeform
    /// Arterial curves between its Junction pieces, and the difference between the two is precisely
    /// what a distance heuristic has to underestimate. <c>(saved AND hashed)</c>.
    /// </summary>
    public required int[] SegmentLengthTiles { get; init; }

    /// <summary>Capacity, Q16.16 vehicles per Tick. <c>(saved AND hashed)</c>.</summary>
    public required int[] SegmentCapacity { get; init; }

    /// <summary>Free-flow speed, Q16.16 Tiles per Tick. <c>(saved AND hashed)</c>.</summary>
    public required int[] SegmentFreeFlow { get; init; }

    /// <summary>The union of the Segment's arcs' masks. <c>(saved AND hashed)</c>.</summary>
    public required byte[] SegmentModes { get; init; }

    /// <summary>Statistical or Microscopic. Zero throughout R0 — S2 never simulates a vehicle.</summary>
    public required byte[] SegmentFidelity { get; init; }

    /// <summary>
    /// Volume. Length is <c>Segments</c> under <see cref="VolumeScope.PerSegment"/> and
    /// <c>2 × Segments</c> under <see cref="VolumeScope.PerDirection"/>, indexed by
    /// <see cref="VolumeIndex"/>. <c>(saved AND hashed)</c>.
    /// </summary>
    public required int[] Volume { get; init; }

    // --- Arcs. One per permitted direction of travel. CSR, grouped by source node. -------------

    /// <summary>CSR offsets, length <c>Nodes + 1</c>. <c>(derived AND rebuilt)</c>.</summary>
    public required int[] ArcStart { get; init; }

    /// <summary>The node this arc leads to. <c>(derived AND rebuilt)</c>.</summary>
    public required int[] ArcTarget { get; init; }

    /// <summary>The Segment this arc is a direction of. <c>(derived AND rebuilt)</c>.</summary>
    public required int[] ArcSegment { get; init; }

    /// <summary>Which modes may traverse <i>in this direction</i>. See <see cref="Modes"/>.</summary>
    public required byte[] ArcModes { get; init; }

    /// <summary>
    /// Free-flow traversal time by car, Q16.16 Ticks, or <see cref="Impassable"/>.
    /// <c>(derived AND rebuilt)</c> — a cached division, and reported as such so the curve shows
    /// what recomputing it on the fly would save.
    /// </summary>
    public required int[] ArcCarTicks { get; init; }

    /// <summary>Free-flow traversal time on foot, Q16.16 Ticks, or <see cref="Impassable"/>.</summary>
    public required int[] ArcFootTicks { get; init; }

    // --- What the Arterials did to the grid, for the report --------------------------------------

    /// <summary>
    /// Street Segments an Arterial ran over and deleted. The detour count, and the reason this
    /// graph has a routing problem in it at all.
    /// </summary>
    public required int SeveredStreets { get; init; }

    /// <summary>
    /// Severed Streets kept as foot-only crossings. Severance is measured against this: it is the
    /// only way across an Arterial on foot.
    /// </summary>
    public required int FootCrossings { get; init; }

    /// <summary>Foot-only block cut-throughs laid, distinct from the crossings above.</summary>
    public required int FootPaths { get; init; }

    /// <summary>
    /// Arterial Segments — runs of Arterial between two authored Junction pieces.
    /// </summary>
    /// <remarks>
    /// <b>Reported because its absence was invisible.</b> The first capture generated eight
    /// Arterials, every one of which left the map within a step, and nothing in the report said so:
    /// the footprint was healthy, the density curve was healthy, and the Arterials simply were not
    /// there. A count that should obviously be non-zero is worth printing precisely because nobody
    /// will read it — until the day it reads zero.
    /// </remarks>
    public required int ArterialRuns { get; init; }

    /// <summary>Total Arterial run length in Tiles. Zero with a non-zero Arterial count is a bug.</summary>
    public required long ArterialRunTiles { get; init; }

    /// <summary>Ramps from an authored Junction piece to the nearest Street intersection.</summary>
    public required int ArterialRamps { get; init; }

    // --- Counts and derived reporting ---------------------------------------------------------

    public int Nodes => NodeX.Length;

    public int Segments => SegmentNodeA.Length;

    public int Arcs => ArcTarget.Length;

    /// <summary>
    /// Where this arc's volume lives. Under <see cref="VolumeScope.PerDirection"/> the two arcs of a
    /// Segment index different counters; under <see cref="VolumeScope.PerSegment"/> they index the
    /// same one. This is the only place the scope is observable, which is the point of making it a
    /// parameter rather than a rewrite.
    /// </summary>
    public int VolumeIndex(int arc)
    {
        int segment = ArcSegment[arc];
        if (Parameters.VolumeScope == VolumeScope.PerSegment)
        {
            return segment;
        }

        // Direction 0 runs A→B. An arc whose source is not A is the reverse.
        int forward = ArcTarget[arc] == SegmentNodeB[segment] ? 0 : 1;
        return (segment * 2) + forward;
    }

    /// <summary>Total road length in Tiles, summed over Segments rather than over arcs.</summary>
    public long RoadLengthTiles()
    {
        long total = 0;
        for (int i = 0; i < Segments; i++)
        {
            total += SegmentLengthTiles[i];
        }

        return total;
    }

    /// <summary>How many Segments admit a mode at all, in either direction.</summary>
    public int SegmentsAdmitting(Modes mode)
    {
        int count = 0;
        for (int i = 0; i < Segments; i++)
        {
            if ((SegmentModes[i] & (byte)mode) != 0)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>Every column, for the footprint curve. Order is declaration order, as the core's is.</summary>
    public ColumnFootprint[] Columns() =>
    [
        new("NodeX", "Nodes", Nodes, sizeof(int), Derived: false),
        new("NodeY", "Nodes", Nodes, sizeof(int), Derived: false),

        new("SegmentNodeA", "Segments", Segments, sizeof(int), Derived: false),
        new("SegmentNodeB", "Segments", Segments, sizeof(int), Derived: false),
        new("SegmentLengthTiles", "Segments", Segments, sizeof(int), Derived: false),
        new("SegmentCapacity", "Segments", Segments, sizeof(int), Derived: false),
        new("SegmentFreeFlow", "Segments", Segments, sizeof(int), Derived: false),
        new("SegmentModes", "Segments", Segments, sizeof(byte), Derived: false),
        new("SegmentFidelity", "Segments", Segments, sizeof(byte), Derived: false),

        new("Volume", "Volume", Volume.Length, sizeof(int), Derived: false),

        new("ArcStart", "Arcs", ArcStart.Length, sizeof(int), Derived: true),
        new("ArcTarget", "Arcs", Arcs, sizeof(int), Derived: true),
        new("ArcSegment", "Arcs", Arcs, sizeof(int), Derived: true),
        new("ArcModes", "Arcs", Arcs, sizeof(byte), Derived: true),
        new("ArcCarTicks", "Arcs", Arcs, sizeof(int), Derived: true),
        new("ArcFootTicks", "Arcs", Arcs, sizeof(int), Derived: true),
    ];

    /// <summary>
    /// The per-mode traversal time of an arc, from the Segment's free-flow speed and the mode's own
    /// ceiling. Precomputed into <see cref="ArcCarTicks"/> and <see cref="ArcFootTicks"/> at build.
    /// </summary>
    public static int TraversalTicks(int lengthTiles, int segmentFreeFlow, Modes mode, byte arcModes)
    {
        if ((arcModes & (byte)mode) == 0)
        {
            return Impassable;
        }

        int ceiling = mode == Modes.Foot ? Units.WalkFreeFlow : int.MaxValue;
        return Units.TraversalTicks(lengthTiles, Units.SlowerOf(segmentFreeFlow, ceiling));
    }

    /// <summary>
    /// A sanity walk over the built graph. Not an invariant tier — this is a spike — but the same
    /// instinct: a generator that quietly emits a disconnected or self-looping graph would produce
    /// routing figures that look fine and mean nothing.
    /// </summary>
    public void AssertWellFormed()
    {
        if (ArcStart.Length != Nodes + 1 || ArcStart[Nodes] != Arcs)
        {
            throw new InvalidOperationException("CSR offsets do not span the arc array.");
        }

        for (int node = 0; node < Nodes; node++)
        {
            for (int arc = ArcStart[node]; arc < ArcStart[node + 1]; arc++)
            {
                int segment = ArcSegment[arc];
                int target = ArcTarget[arc];

                if (target == node)
                {
                    throw new InvalidOperationException($"Self-loop at node {node}.");
                }

                bool endpointsMatch =
                    (SegmentNodeA[segment] == node && SegmentNodeB[segment] == target)
                    || (SegmentNodeB[segment] == node && SegmentNodeA[segment] == target);

                if (!endpointsMatch)
                {
                    throw new InvalidOperationException($"Arc {arc} is not a direction of Segment {segment}.");
                }

                if (ArcModes[arc] == 0)
                {
                    throw new InvalidOperationException($"Arc {arc} admits no mode.");
                }

                if ((SegmentModes[segment] & ArcModes[arc]) != ArcModes[arc])
                {
                    throw new InvalidOperationException($"Segment {segment} mask does not cover arc {arc}.");
                }

                if (ArcCarTicks[arc] <= 0 || ArcFootTicks[arc] <= 0)
                {
                    throw new InvalidOperationException($"Arc {arc} has a non-positive traversal time.");
                }
            }
        }

        for (int segment = 0; segment < Segments; segment++)
        {
            if (SegmentLengthTiles[segment] <= 0)
            {
                throw new InvalidOperationException($"Segment {segment} has no length.");
            }

            if (Fixed.ToIntFloor(SegmentFreeFlow[segment]) <= 0)
            {
                throw new InvalidOperationException($"Segment {segment} has no free-flow speed.");
            }
        }
    }
}
