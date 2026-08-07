using S2.Routing.Graph;

namespace S2.Routing.Storm;

/// <summary>
/// Segment → its arcs, as a CSR index.
/// </summary>
/// <remarks>
/// <para>
/// <b>R3 and R4 did without this and R5 cannot.</b> Both earlier tasks apply an edit by scanning
/// every arc in the graph and comparing <c>ArcSegment</c> against the one being deleted — O(arcs)
/// per edit, which is ~66,000 comparisons. That is invisible when the thing being timed is a repair
/// costing milliseconds and the edit is one Segment applied outside the clock. <b>R5 deletes
/// hundreds of Segments in one gesture and the gesture is the thing being timed</b>, so the same
/// spelling would put ~17 million comparisons inside the measured span and publish them as the cost
/// of the edit.
/// </para>
/// <para>
/// The index is a property of the Road Graph rather than of any rung, so it is built once and its
/// footprint is stated once, outside every resident-size column below — the handling R3 gave the
/// reverse-arc index for the same reason.
/// </para>
/// </remarks>
internal sealed class SegmentArcs
{
    private readonly int[] _start;
    private readonly int[] _arc;

    private SegmentArcs(int[] start, int[] arc)
    {
        _start = start;
        _arc = arc;
    }

    public static SegmentArcs Of(RoadGraph graph)
    {
        var start = new int[graph.Segments + 1];

        for (int arc = 0; arc < graph.Arcs; arc++)
        {
            start[graph.ArcSegment[arc] + 1]++;
        }

        for (int segment = 0; segment < graph.Segments; segment++)
        {
            start[segment + 1] += start[segment];
        }

        var arcs = new int[graph.Arcs];
        var cursor = (int[])start.Clone();

        for (int arc = 0; arc < graph.Arcs; arc++)
        {
            arcs[cursor[graph.ArcSegment[arc]]++] = arc;
        }

        return new SegmentArcs(start, arcs);
    }

    /// <summary>The arcs carrying one Segment. Both directions, so a deletion is symmetric.</summary>
    public ReadOnlySpan<int> For(int segment) =>
        _arc.AsSpan(_start[segment], _start[segment + 1] - _start[segment]);

    public long ResidentBytes => ((long)_start.Length + _arc.Length) * sizeof(int);
}
