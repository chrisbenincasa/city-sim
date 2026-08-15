using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
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

    /// <summary>How long a Vehicle takes to cross this Segment with nothing in its way.</summary>
    public TravelTime FreeFlowOver(int slot) => TravelTime.Over(LengthTiles[slot], FreeFlow[slot]);

    /// <summary>
    /// A Segment direction's volume/capacity ratio at its current volume — <b>the volume-delay
    /// function's one input</b>.
    /// </summary>
    public Ratio LoadAt(int slot, bool forward, TravelTime freeFlow) =>
        LoadOf(slot, forward ? VolumeForward[slot] : VolumeBackward[slot], freeFlow);

    /// <summary>The same ratio at a volume the caller names, rather than at the current one.</summary>
    /// <remarks>
    /// <para>
    /// <b>It lives on the table rather than in the engine that prices with it, because it has two
    /// readers.</b> <c>TripEngine</c> asks about <em>now</em>; the runner's <c>--traffic</c> picture
    /// asks about the <em>peak</em> a Segment reached over a run, which is not a state any column
    /// holds. A second copy of this arithmetic in the shell would be one fact stored twice with the
    /// two constants below duplicated beside it (<c>plans/0012</c> <b>Cause 1</b>), and the copy that
    /// drifts is always the one nothing prices with.
    /// </para>
    /// <para>
    /// ⚠ <b>Volume is a <em>stock</em> and capacity is a <em>flow</em>, and the conversion between
    /// them is the Segment's own free-flow crossing time.</b> <see cref="VolumeForward"/> counts
    /// Vehicles <em>present</em>; <see cref="CapacityPerDay"/> counts Vehicles <em>passing</em>.
    /// Little's Law relates them exactly — <c>present = passing × time spent</c> — so the Vehicles a
    /// Segment holds when it is running at capacity is <c>capacity per Tick × free-flow dwell in
    /// Ticks</c>, and that is the denominator. <b>No new number is introduced</b>: both terms are
    /// already derived from the Ruleset in force.
    /// </para>
    /// <para>
    /// ⚠ <b>The obvious alternative is wrong, it was built first, and a measurement caught it.</b>
    /// <c>adr/0041</c> says <i>a vehicle crosses about one Segment per Tick</i>, which would make the
    /// stock and the per-Tick flow the same number and the dwell term unnecessary. <b>That sentence was
    /// true when it was written and <c>adr/0094</c> made it false</b>: under the old 8192-Tick Day a
    /// 32-Tile Street at 50 km/h took <b>0.87</b> Ticks — near enough to one — and under the 2048-Tick
    /// Day it takes <b>0.22</b>, which is <c>adr/0071</c>'s own illustration restated. Dividing by the
    /// flow alone therefore overstated the denominator 4.5× and the function came back <b>inert at
    /// every population this project can build</b>: a peak of 6 Vehicles on one Segment against 42, a
    /// delay of ×1.0001, unchanged from 4,000 Citizens to 160,000. ***A premise licensing one quantity
    /// to stand in for another is a measurement, and a constant moved in another document can retire
    /// it silently.***
    /// </para>
    /// <para>
    /// <b>The corrected denominator is physically checkable, which the old one was not.</b> A Street
    /// carries 3,600 Vehicles an hour and a 128 m Segment is crossed in 9.2 seconds, so it holds
    /// <b>9.2 Vehicles</b> at capacity — a 14 m spacing, or a one-second headway, which is what a road
    /// at capacity looks like. Forty-two Vehicles on 128 m is a 3 m spacing, which is a car park.
    /// </para>
    /// <para>
    /// <b>A capacity below one Vehicle a Tick reads as no delay rather than as infinite delay.</b>
    /// Nothing in either shipped Ruleset produces one — the slowest kind carries 1,000 an hour, which
    /// is 11 a Tick — so this is the absent case, and ***absent is not zero and it is not
    /// impassable***.
    /// </para>
    /// </remarks>
    public Ratio LoadOf(int slot, int volume, TravelTime freeFlow)
    {
        int capacity = CapacityPerDay[slot];

        if (volume <= 0 || capacity <= 0 || freeFlow.Raw <= 0)
        {
            return Ratio.Zero;
        }

        // Both operands are scaled down by CapacityScale so that Fixed.FromInt keeps them inside
        // Q16.16's whole range: a Day is 2,048 Ticks and an Arterial carries 288,000 Vehicles a Day,
        // and 288,000 does not fit. Scaling both sides by the same factor leaves the quotient exact
        // wherever the two divide evenly, which they do at every capacity either shipped Ruleset
        // states.
        Ratio perTickShare = Ratio.FromFraction(
            (volume > MaxVolume ? MaxVolume : volume)
                * IntegerMath.FloorDiv(Ticks.PerDay, CapacityScale),
            IntegerMath.FloorDiv(capacity, CapacityScale));

        return new Ratio(Fixed.Div(perTickShare.Raw, freeFlow.Raw));
    }

    /// <summary>
    /// The divisor that keeps <see cref="LoadOf"/>'s two operands inside <see cref="Fixed"/>'s whole
    /// range.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The obvious alternative — floor the capacity to whole Vehicles per Tick first — was built
    /// and is wrong.</b> A Day is 2,048 Ticks, so anything under 2,048 Vehicles a Day floors to
    /// <b>zero</b> and the guard against a zero capacity then reports <em>no delay</em> for what is
    /// really a very narrow road. A measured sweep caught it: at 200 Vehicles an hour the function bit
    /// at ×2.48, and at <b>60</b> an hour — a narrower road still — it went back to ×1.0000.
    /// ***A guard written for an absent quantity will fire on a small one, and small is the direction
    /// the interesting cases lie in.***
    /// </remarks>
    private const int CapacityScale = 16;

    /// <summary>
    /// The largest volume <see cref="LoadOf"/> will read, so its numerator cannot leave Q16.16.
    /// </summary>
    /// <remarks>
    /// <b>Far past any authored clamp</b> — 255 Vehicles on one 128 m Segment direction is a 0.5 m
    /// spacing — so this bounds the arithmetic and never the model. The clamp that bounds the model is
    /// <c>[traffic] clamp_percent</c>, which is authored and refused outside 100–1000.
    /// </remarks>
    private const int MaxVolume = 255;
}
