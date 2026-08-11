using Borough.Core.Quantities;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// One Segment of the Road Graph. Empty for the reason <c>Entities.Citizen</c> is empty.
/// </summary>
public readonly struct RoadSegment;

/// <summary>
/// The Road Graph's Segments — <b>one run of road between two adjacent nodes</b>
/// (<c>CONTEXT.md</c> → Segment), carrying the attributes every other system reads off it.
/// </summary>
/// <remarks>
/// <para>
/// <b>A Segment is not a Tile and it is not a whole road.</b> A Tile-length edge would put millions
/// on a 4096² map and a run between authored Junctions would put almost none on a city that is mostly
/// Streets — both wrong by more than an order of magnitude. What sets the length here is
/// <c>[roads] block_tiles</c> rather than a constant, so the working figure is a Ruleset's claim and
/// not the binary's.
/// </para>
/// <para>
/// <b><see cref="LengthTiles"/> is arc length, not the distance between the endpoints</b>, and the
/// gap is deliberate. A freeform Arterial curves between its Junction pieces, so its Segment is
/// longer than the straight line joining them — which is the safe direction for a distance heuristic
/// (it underestimates) and is the exact quantity a traversal cost must divide. A Street's two are
/// equal because a Street is straight.
/// </para>
/// <para>
/// <b>Free-flow speed and capacity are <c>(derived AND rebuilt)</c> from the Segment's
/// <see cref="Kind"/> and the Ruleset in force</b>, which is <c>adr/0064</c> and <c>adr/0068</c>
/// applied to a road rather than to a Bin or a Building. Freezing either into a saved column at
/// construction is the defect both ADRs corrected: a designer could then not retune a speed limit
/// without demolishing the city holding the old copy, and <c>adr/0015</c>'s acceptance test would
/// fail in the place a player would most notice it.
/// </para>
/// <para>
/// <b>The mode masks are per direction and saved; the union is derived.</b> See
/// <see cref="TravelMode"/> and <c>adr/0072</c> — a one-way street carries cars one way and
/// pedestrians both, which a single Segment-level mask cannot express without either a second Arc set
/// for foot or a street nobody may walk down.
/// </para>
/// <para>
/// <b>Volume is per direction, and that is settled rather than assumed.</b> <c>adr/0041</c> attributes
/// volume when a Traveller <em>enters</em> a Segment, and a one-way pair is not the same road in both
/// directions; a Segment carries about four Lanes and Lanes are directional queues, so a Segment
/// jammed inbound at the morning peak would read half-loaded if the two were summed — making Stress
/// understate exactly when it matters. S2 R0 priced the finer scope at ~5% of graph footprint. Two
/// columns rather than one array of <c>2 × Segments</c>, because per-direction storage <em>is</em>
/// per-row storage once the row is the Segment.
/// </para>
/// <para>
/// <b><see cref="Epoch"/> is the invalidation contract, built now so every later consumer inherits it
/// rather than inventing one</b> (<c>adr/0012</c>). The rung is per-Segment and it is measured: 96%
/// route retention under a sustained edit storm against a single counter's 9%, and cheaper. A single
/// counter for the whole graph carries no location, so a route cannot tell whether an edit touched it
/// — which makes <i>never a global flush</i> true about <em>when you pay</em> and false about
/// <em>what survives</em>.
/// </para>
/// </remarks>
[Table]
public sealed class RoadSegmentTable
{
    private readonly Rows<RoadSegment> _rows;

    /// <param name="capacity">Initial slot count. Segments are added as the player lays road.</param>
    /// <param name="nodes">The table this one's endpoint handles address.</param>
    public RoadSegmentTable(int capacity, RoadNodeTable nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        _rows = new Rows<RoadSegment>("road_segment", capacity, Buffering.OneCopy);

        NodeA = _rows.SavedHandle("node_a", nodes.Rows);
        NodeB = _rows.SavedHandle("node_b", nodes.Rows);

        LengthTiles = _rows.Saved<Tiles>("length_tiles");
        Kind = _rows.Saved<byte>("kind");

        ModesForward = _rows.Saved<byte>("modes_forward");
        ModesBackward = _rows.Saved<byte>("modes_backward");

        VolumeForward = _rows.Saved<int>("volume_forward", Touch.PerTick);
        VolumeBackward = _rows.Saved<int>("volume_backward", Touch.PerTick);

        Epoch = _rows.Saved<uint>("epoch");

        FreeFlow = _rows.Derived<Speed>("free_flow");
        CapacityPerDay = _rows.Derived<int>("capacity_per_day");
        Modes = _rows.Derived<byte>("modes");
        Fidelity = _rows.Derived<byte>("fidelity");

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<RoadSegment> Rows => _rows;

    /// <summary>The Segment's first node. The forward direction runs A→B.</summary>
    public HandleColumn<RoadNode> NodeA { get; }

    /// <summary>The Segment's second node.</summary>
    public HandleColumn<RoadNode> NodeB { get; }

    /// <summary>Run length in whole Tiles. <b>Arc length</b> — see the type's remarks.</summary>
    public Column<Tiles> LengthTiles { get; }

    /// <summary>Which <see cref="RoadKind"/> this is. What free-flow and capacity are derived from.</summary>
    public Column<byte> Kind { get; }

    /// <summary>The <see cref="TravelMode"/> mask valid travelling A→B.</summary>
    public Column<byte> ModesForward { get; }

    /// <summary>The <see cref="TravelMode"/> mask valid travelling B→A.</summary>
    public Column<byte> ModesBackward { get; }

    /// <summary>Vehicles currently on the Segment travelling A→B.</summary>
    public Column<int> VolumeForward { get; }

    /// <summary>Vehicles currently on the Segment travelling B→A.</summary>
    public Column<int> VolumeBackward { get; }

    /// <summary>
    /// The Segment's Epoch — a monotone counter bumped on any edit to it.
    /// </summary>
    /// <remarks>
    /// <b>Saved and hashed, because a stored route compares against it.</b> A consumer records the
    /// Epoch it computed under and revalidates lazily on next use; the test is containment — <i>does
    /// this route name that Segment</i> — which is exact for a removal and has nothing to match for
    /// an addition, since a new Segment is on no existing route. <c>adr/0012</c> settles that
    /// asymmetry with a bound checked at use and a proximity wake over it, and <b>both live with the
    /// consumer</b>: no route, Habit or Traveller exists before 5b, and a stale bit with nothing to
    /// mark stale is a mechanism with no consumer (<c>adr/0070</c> read forwards).
    /// </remarks>
    public Column<uint> Epoch { get; }

    /// <summary>
    /// Free-flow speed, Q16.16 Tiles per Tick. <b>The road's ceiling, not a mode's</b> — a walk is
    /// held to walking pace on a boulevard, and the minimum is taken per Arc.
    /// </summary>
    public Column<Speed> FreeFlow { get; }

    /// <summary>
    /// Flow capacity, in whole Vehicles per Day.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Vehicles per Day rather than a fourth Q16.16 quantity, and that is a decision.</b> The spike
    /// stored capacity as Q16.16 Vehicles/Tick, which <c>adr/0071</c> does not enumerate — it carries
    /// exactly three quantities at that scale (sub-Tile position, speed in Tiles/Tick, travel time in
    /// Ticks) and names <i>a fourth quantity wanting the scale</i> as a trigger to reopen it. A Day is
    /// <c>CONTEXT.md</c>'s only time unit above the Tick, the conversion from an authored
    /// Vehicles/hour is exact, and a count of Vehicles needs no fixed point at all.
    /// </para>
    /// <para>
    /// <b>Nothing reads it yet, which is why the unit could be chosen freely.</b> The volume-delay
    /// function is milestone 6 and <c>adr/0035</c>'s Upkeep pricing is Phase 3; under <c>adr/0070</c>
    /// neither unbuilt consumer may dictate a representation now, and whichever of them arrives first
    /// may pick its own denominator without a migration, because this column is derived.
    /// </para>
    /// </remarks>
    public Column<int> CapacityPerDay { get; }

    /// <summary>
    /// The union of the Segment's two direction masks. <b>Derived — the Arcs own the truth.</b>
    /// </summary>
    public Column<byte> Modes { get; }

    /// <summary>
    /// Statistical or Microscopic. <b>A named hole: nothing writes anything but zero.</b>
    /// </summary>
    /// <remarks>
    /// <c>adr/0007</c> makes Fidelity a property of place driven by <b>Stress</b>, which is
    /// <c>volume / capacity</c> times a junction complexity factor — and volume is written by Trips
    /// (5b), while the complexity factor's derivation is unowned (<c>03 §3.3</c>, needed by 7a). It is
    /// declared now with a constant writer rather than added later, because <em>where the number
    /// lands</em> is this slice's decision and <em>what the number is</em> is not; declaring it later
    /// would be a schema change applied to a table that already has rows.
    /// </remarks>
    public Column<byte> Fidelity { get; }

    /// <summary>
    /// Allocates a Segment between two nodes, opening its Epoch at one.
    /// </summary>
    /// <remarks>
    /// <b>The Epoch opens at 1 rather than 0 so that <c>0</c> stays available as <em>never
    /// computed</em>.</b> A consumer holding a default-initialised Epoch must not compare equal to a
    /// Segment nobody has edited, or a route that was never computed would validate. The derived
    /// columns are left to <see cref="RoadGraph.RebuildDerived"/> rather than written here, because a
    /// value written at construction is exactly the copy <c>adr/0064</c> forbids.
    /// </remarks>
    public Handle<RoadSegment> Create(
        Handle<RoadNode> nodeA,
        Handle<RoadNode> nodeB,
        Tiles lengthTiles,
        RoadKind kind,
        TravelMode forward,
        TravelMode backward)
    {
        Handle<RoadSegment> handle = _rows.Allocate();
        int slot = _rows.Resolve(handle);

        NodeA[slot] = nodeA;
        NodeB[slot] = nodeB;
        LengthTiles[slot] = lengthTiles;
        Kind[slot] = (byte)kind;
        ModesForward[slot] = (byte)forward;
        ModesBackward[slot] = (byte)backward;
        Epoch[slot] = 1;

        return handle;
    }

    /// <summary>
    /// Bumps a Segment's Epoch. <b>The one door an edit goes through.</b>
    /// </summary>
    /// <remarks>
    /// Monotone and never reset, so a consumer's stored value can only ever be equal or behind. It
    /// saturates rather than wrapping: a <c>uint</c> that wrapped would make a stale route compare
    /// valid, which is the one failure this counter exists to prevent, and 4.29 billion edits to one
    /// Segment is a number no session reaches.
    /// </remarks>
    public void Edited(int slot)
    {
        if (Epoch[slot] != uint.MaxValue)
        {
            Epoch[slot]++;
        }
    }
}
