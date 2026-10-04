using Borough.Core.Arithmetic;
using Borough.Core.Quantities;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>A conservative, Cell-keyed traffic mask derived for one field pass.</summary>
/// <remarks>Rebuild before each pass. SegmentResidency owns geometry; this mask only caches volume reach.</remarks>
public sealed class TrafficPresence
{
    private readonly bool[] _near = new bool[CellGrid.WorldCellCount];
    private int _range = -1;

    internal Span<bool> Mask => _near;

    /// <summary>Whether any Segment in the world carries a Vehicle.</summary>
    public bool AnyTraffic { get; private set; }

    /// <summary>Segments found carrying Vehicles on the last rebuild.</summary>
    public int MovingSegments { get; private set; }

    /// <summary>Whether the rebuilt mask conservatively covers this range.</summary>
    public bool Covers(Tiles range) => range.Raw >= 0 && range.Raw <= _range;

    /// <summary>False proves silence; true requires the exact query. Outside-map points fall back.</summary>
    public bool Near(Tiles east, Tiles north) =>
        east.Raw < 0 || north.Raw < 0 || east.Raw > CellGrid.WorldTiles || north.Raw > CellGrid.WorldTiles
        || _near[CellGrid.Index(CellGrid.ToCellsClamped(east), CellGrid.ToCellsClamped(north))];

    /// <summary>Restamps volume reach from the graph's rebuilt Segment bounds.</summary>
    public void Rebuild(RoadGraph graph, Tiles range)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentOutOfRangeException.ThrowIfNegative(range.Raw, nameof(range));
        Array.Clear(_near);
        _range = range.Raw;
        MovingSegments = 0;

        // Floored contribution distance reaches one Tile farther. The extra Cell covers the
        // floor-division fencepost between the query point and the source; it is not slack.
        long reach = (long)range.Raw + 1;
        var margin = new Cells((int)IntegerMath.CeilDiv(reach, CellGrid.TilesPerCell) + 1);
        RoadSegmentTable segments = graph.Segments;
        for (int slot = 0; slot < segments.Rows.SlotCount; slot++)
        {
            if (!segments.Rows.IsLive(slot)
                || (long)segments.VolumeForward[slot] + segments.VolumeBackward[slot] <= 0)
            {
                continue;
            }
            MovingSegments++;
            CellRect box = graph.Residency.BoxOf(slot).Dilate(margin).Clamp();
            for (int north = box.North.Raw; north < box.NorthEnd.Raw; north++)
            {
                for (int east = box.East.Raw; east < box.EastEnd.Raw; east++)
                {
                    _near[CellGrid.Index(new Cells(east), new Cells(north))] = true;
                }
            }
        }
        AnyTraffic = MovingSegments > 0;
    }
}
