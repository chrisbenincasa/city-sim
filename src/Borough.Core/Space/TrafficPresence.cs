using Borough.Core.Quantities;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>Traffic totals for one field pass. Spatial candidates come from SegmentResidency.</summary>
public sealed class TrafficPresence
{
    private int _range = -1;

    /// <summary>Whether any Segment in the world carries a Vehicle.</summary>
    public bool AnyTraffic { get; private set; }

    /// <summary>Segments found carrying Vehicles on the last rebuild.</summary>
    public int MovingSegments { get; private set; }

    /// <summary>Whether these totals were built for the requested pass.</summary>
    public bool Covers(Tiles range) => _range == range.Raw && _range > 0;

    /// <summary>Whether a candidate Segment carries traffic. False proves the query is silent.</summary>
    public bool Near(RoadGraph graph, Tiles east, Tiles north, Tiles range)
    {
        ArgumentNullException.ThrowIfNull(graph);
        if (!Covers(range)) { return true; }
        if (!AnyTraffic) { return false; }
        Tiles reach = new(range.Raw < int.MaxValue ? range.Raw + 1 : range.Raw);
        foreach (int slot in graph.Residency.Near(east, north, reach))
        {
            if ((long)graph.Segments.VolumeForward[slot] + graph.Segments.VolumeBackward[slot] > 0)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Recounts live, Vehicle-carrying Segments at the start of a field pass.</summary>
    public void Rebuild(RoadGraph graph, Tiles range)
    {
        ArgumentNullException.ThrowIfNull(graph);
        _range = range.Raw;
        MovingSegments = 0;
        RoadSegmentTable segments = graph.Segments;
        for (int slot = 0; slot < segments.Rows.SlotCount; slot++)
        {
            if (segments.Rows.IsLive(slot)
                && (long)segments.VolumeForward[slot] + segments.VolumeBackward[slot] > 0)
            {
                MovingSegments++;
            }
        }
        AnyTraffic = MovingSegments > 0;
    }
}
