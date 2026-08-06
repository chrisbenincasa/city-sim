using S2.Routing.Graph;

namespace S2.Routing.Cluster;

/// <summary>
/// The Road Graph walked backwards: each arc's source node, and each node's incoming arcs.
/// </summary>
/// <remarks>
/// <para>
/// <b>R3 needs it because inserting the goal into the abstract graph is a backward query.</b> The
/// hierarchy answers <c>cost(portal → goal)</c> for every portal of the goal's cluster, and the
/// graph's own CSR is grouped by <i>source</i>, so a forward search cannot ask it. Nothing before
/// R3 needed this: the flat search terminates on a goal it walks toward, and the matrix's build
/// kernel is a forward one-to-all.
/// </para>
/// <para>
/// <b>It is a property of the graph, not of the cluster size</b>, so it is built once and shared
/// across the whole sweep — and its footprint is reported apart from the abstract graph's, or every
/// rung would carry a constant that has nothing to do with the axis being swept.
/// </para>
/// </remarks>
internal sealed class ReverseArcs
{
    private ReverseArcs(int[] source, int[] incomingStart, int[] incoming)
    {
        Source = source;
        IncomingStart = incomingStart;
        Incoming = incoming;
    }

    /// <summary>Arc index → the node it leaves.</summary>
    public int[] Source { get; }

    /// <summary>CSR offsets into <see cref="Incoming"/>, length <c>Nodes + 1</c>.</summary>
    public int[] IncomingStart { get; }

    /// <summary>Arc indices, grouped by the node they lead to.</summary>
    public int[] Incoming { get; }

    public long ResidentBytes =>
        ((long)Source.Length + IncomingStart.Length + Incoming.Length) * sizeof(int);

    public static ReverseArcs Of(RoadGraph graph)
    {
        var source = new int[graph.Arcs];
        var start = new int[graph.Nodes + 1];

        for (int node = 0; node < graph.Nodes; node++)
        {
            for (int arc = graph.ArcStart[node]; arc < graph.ArcStart[node + 1]; arc++)
            {
                source[arc] = node;
                start[graph.ArcTarget[arc] + 1]++;
            }
        }

        for (int node = 0; node < graph.Nodes; node++)
        {
            start[node + 1] += start[node];
        }

        var incoming = new int[graph.Arcs];
        var cursor = (int[])start.Clone();

        for (int arc = 0; arc < graph.Arcs; arc++)
        {
            incoming[cursor[graph.ArcTarget[arc]]++] = arc;
        }

        return new ReverseArcs(source, start, incoming);
    }
}
