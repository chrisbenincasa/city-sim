using Borough.Core.Arithmetic;
using Borough.Core.Quantities;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// Which blocks have a Vehicle-carrying Segment within one <see cref="LineSource.Range"/> of them.
/// <b>A conservative superset, and the only thing it is allowed to be.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>It exists because a line source query does volume-independent work to reach a
/// volume-dependent answer.</b> <see cref="LineSourceQueries"/>' first pass finds the nearest
/// Street by measuring a distance to every Segment in the window — full handle resolution and a
/// projection each — and only then discovers, in <c>Contribution</c>, that the Segment carries no
/// Vehicles and radiates nothing. Where <em>nothing within range</em> carries Vehicles the whole
/// query is provably zero, and the measured cost of finding that out was <b>7.2 seconds per land
/// value pass</b> on <c>rulesets/bordered.toml</c>, flat across every hour of the day.
/// </para>
/// <para>
/// <b>Over-marking costs time and never costs an answer; under-marking is a wrong field.</b> So a
/// Segment is stamped over the bounding box of its two endpoints dilated by
/// <c>ceil(range / block) + 1</c> blocks, which is a superset of every block from which that
/// Segment could be within range. The <c>+1</c> is the floor-division fencepost and is not slack.
/// </para>
/// <para>
/// ⚠ <b>It is scratch, rebuilt from the Segment table at the top of every pass that uses it, and
/// that is what keeps it out of <c>CLAUDE.md</c>'s <em>a structure that lives outside the world is
/// not derived state</em> hazard.</b> Nothing here survives a Tick, so there is no state a load
/// could fail to rebuild and no column whose rebuild could go missing. A cache keyed on a Tick
/// would have both problems and buys nothing: the rebuild is one linear scan of the Segment table
/// against a pass that queries it a million times.
/// </para>
/// <para>
/// <b>It is keyed on the range it was built for and refuses to answer for any other</b>
/// (<see cref="Covers"/>). Noise and near-road pollution are the same query at different ranges, so
/// a presence map built for one is not a superset for the other — and the failure would be silent
/// zeroes in a field rather than an exception. Answering <c>false</c> to <c>Covers</c> falls the
/// caller back to the full scan, which is slow and right.
/// </para>
/// </remarks>
public sealed class TrafficPresence
{
    private bool[] _near = [];

    private int _span;

    private int _range = -1;

    /// <summary>Whether any Segment in the world carries a Vehicle. False skips the map wholesale.</summary>
    public bool AnyTraffic { get; private set; }

    /// <summary>Segments found carrying Vehicles on the last <see cref="Rebuild"/>.</summary>
    public int MovingSegments { get; private set; }

    /// <summary>
    /// Whether this map was built for <paramref name="range"/> and may be consulted for it.
    /// </summary>
    public bool Covers(Tiles range) => _range == range.Raw && _span > 0;

    /// <summary>
    /// Whether block <c>(column, row)</c> may have a Vehicle-carrying Segment within range.
    /// <b>False is a proof; true is a maybe.</b>
    /// </summary>
    public bool Near(int column, int row) =>
        (uint)column < (uint)_span
        && (uint)row < (uint)_span
        && _near[(row * _span) + column];

    /// <summary>
    /// Restamps the map from the Segment table for one range. <b>One linear scan, no allocation
    /// after the first call at a given size.</b>
    /// </summary>
    public void Rebuild(RoadGraph graph, Tiles range)
    {
        ArgumentNullException.ThrowIfNull(graph);

        StreetGrid streets = graph.Streets;
        RoadSegmentTable segments = graph.Segments;
        RoadNodeTable nodes = graph.Nodes;

        int block = streets.BlockTiles;

        _span = streets.Span;
        _range = range.Raw;
        AnyTraffic = false;
        MovingSegments = 0;

        if (_span <= 0 || block <= 0 || range.Raw <= 0)
        {
            // A map that cannot be indexed refuses to answer rather than answering "nothing here".
            _range = -1;
            return;
        }

        int cells = _span * _span;

        if (_near.Length < cells)
        {
            _near = new bool[cells];
        }
        else
        {
            Array.Clear(_near, 0, cells);
        }

        // The dilation: ceil(range / block) blocks of reach, plus one for the fencepost between a
        // Tile's position and the block it floors into.
        int window = IntegerMath.CeilDiv(range.Raw, block) + 1;

        for (int slot = 0; slot < segments.Rows.SlotCount; slot++)
        {
            if (!segments.Rows.IsLive(slot))
            {
                continue;
            }

            long volume = (long)segments.VolumeForward[slot] + segments.VolumeBackward[slot];

            if (volume <= 0)
            {
                continue;
            }

            if (!nodes.Rows.TryResolve(segments.NodeA[slot], out int a)
                || !nodes.Rows.TryResolve(segments.NodeB[slot], out int b))
            {
                continue;
            }

            MovingSegments++;
            AnyTraffic = true;

            int columnA = IntegerMath.FloorDiv(nodes.East[a].Raw, block);
            int columnB = IntegerMath.FloorDiv(nodes.East[b].Raw, block);
            int rowA = IntegerMath.FloorDiv(nodes.North[a].Raw, block);
            int rowB = IntegerMath.FloorDiv(nodes.North[b].Raw, block);

            int fromColumn = (columnA < columnB ? columnA : columnB) - window;
            int toColumn = (columnA > columnB ? columnA : columnB) + window;
            int fromRow = (rowA < rowB ? rowA : rowB) - window;
            int toRow = (rowA > rowB ? rowA : rowB) + window;

            if (fromColumn < 0) { fromColumn = 0; }
            if (fromRow < 0) { fromRow = 0; }
            if (toColumn > _span - 1) { toColumn = _span - 1; }
            if (toRow > _span - 1) { toRow = _span - 1; }

            for (int row = fromRow; row <= toRow; row++)
            {
                int start = row * _span;

                for (int column = fromColumn; column <= toColumn; column++)
                {
                    _near[start + column] = true;
                }
            }
        }
    }
}
