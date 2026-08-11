using Borough.Core.Quantities;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// One node of the Road Graph. Empty for the reason <c>Entities.Citizen</c> is empty.
/// </summary>
public readonly struct RoadNode;

/// <summary>
/// The Road Graph's nodes — <b>Street intersections and authored Junction pieces, stored as one
/// kind of thing</b> (<c>CONTEXT.md</c> → Road Graph: <i>"uniform regardless of how a road was
/// drawn"</i>).
/// </summary>
/// <remarks>
/// <para>
/// <b>A node holds a Tile coordinate and nothing else that is saved.</b> That is the whole of
/// <c>06</c> milestone 5a's stated risk being retired: <i>geometry leaks into the simulation and the
/// routing graph stops being uniform</i>. A Street's node falls out of the Tile grid and an
/// Arterial's is where a Junction piece was placed, and after construction nothing downstream can
/// tell which — there is no spline here, no control point, no curve parameter and no metre.
/// </para>
/// <para>
/// <b>The two derived columns are the CSR slice, and they are why an Arc is addressed from a node
/// rather than found by searching Segments.</b> <see cref="ArcStart"/> and <see cref="ArcCount"/>
/// name a contiguous run of <see cref="RoadArcTable"/> rows leaving this node. They are
/// <c>(derived AND rebuilt)</c> because the adjacency is a function of the Segments — which is
/// <c>adr/0040</c>'s posture that the whole abstract routing structure is free to change forever.
/// </para>
/// <para>
/// <b>Stored as a start and a count rather than as CSR offsets of length <c>Nodes + 1</c>.</b> The
/// spike carried the classic form and it does not fit the table discipline: a column has exactly one
/// element per row, so the terminating offset has nowhere to live and would have to become a bare
/// array beside the columns — which <c>BOR0901</c> is an error for, and rightly, since it is storage
/// nothing declared. A count carries the same information, needs no sentinel row and cannot be read
/// off the end.
/// </para>
/// <para>
/// <b>The two component columns are per mode, and that is Severance made queryable.</b> A city can be
/// one component for cars and several for pedestrians — <c>CONTEXT.md</c> → Severance: <i>"a city can
/// be perfectly well connected for cars and broken for people"</i> — so a single component label
/// would answer the question for whichever mode happened to be unioned and silently mislead about the
/// other. See <see cref="RoadConnectivity"/>.
/// </para>
/// </remarks>
[Table]
public sealed class RoadNodeTable
{
    private readonly Rows<RoadNode> _rows;

    /// <param name="capacity">Initial slot count. Nodes are added as the player lays road.</param>
    public RoadNodeTable(int capacity)
    {
        _rows = new Rows<RoadNode>("road_node", capacity, Buffering.OneCopy);

        East = _rows.Saved<Tiles>("east");
        North = _rows.Saved<Tiles>("north");

        ArcStart = _rows.Derived<int>("arc_start");
        ArcCount = _rows.Derived<int>("arc_count");

        CarComponent = _rows.Derived<int>("car_component");
        FootComponent = _rows.Derived<int>("foot_component");

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<RoadNode> Rows => _rows;

    /// <summary>The node's east coordinate, in whole Tiles.</summary>
    public Column<Tiles> East { get; }

    /// <summary>The node's north coordinate, in whole Tiles.</summary>
    public Column<Tiles> North { get; }

    /// <summary>First row in <see cref="RoadArcTable"/> leaving this node.</summary>
    public Column<int> ArcStart { get; }

    /// <summary>How many Arcs leave this node. Zero for a node no Segment reaches.</summary>
    public Column<int> ArcCount { get; }

    /// <summary>
    /// Which connected component this node is in over the <see cref="TravelMode.Car"/> subgraph.
    /// </summary>
    public Column<int> CarComponent { get; }

    /// <summary>
    /// Which connected component this node is in over the <see cref="TravelMode.Foot"/> subgraph.
    /// <b>The one that disagrees with <see cref="CarComponent"/> when an Arterial has severed a
    /// neighbourhood.</b>
    /// </summary>
    public Column<int> FootComponent { get; }

    /// <summary>Allocates a node at a Tile coordinate.</summary>
    public Handle<RoadNode> Create(Tiles east, Tiles north)
    {
        Handle<RoadNode> handle = _rows.Allocate();
        int slot = _rows.Resolve(handle);

        East[slot] = east;
        North[slot] = north;

        return handle;
    }
}
