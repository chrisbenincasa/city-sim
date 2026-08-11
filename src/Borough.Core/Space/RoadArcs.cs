using Borough.Core.Quantities;

namespace Borough.Core.Space;

/// <summary>
/// The Road Graph's Arcs — <b>one permitted direction of travel along a Segment</b>
/// (<c>CONTEXT.md</c> → Arc), grouped by the node they leave.
/// </summary>
/// <remarks>
/// <para>
/// <b>Arrays beside the graph rather than a <c>[Table]</c>, and the reason is the State Hash rather
/// than convenience.</b> <see cref="Tables.Rows.Fold"/> folds four allocator scalars —
/// <c>slot_count</c>, <c>live_count</c>, <c>free_head</c> and <b><c>next_id</c></b> — <em>before</em>
/// it consults any column's disposition, so they fold whether or not the table declares a single
/// saved column. A wholly derived table registered with <c>World</c> would therefore fold
/// <c>next_id</c>, which counts every row ever allocated: two worlds identical in every Segment would
/// hash differently because one of them had rebuilt its adjacency more often than the other. <b>The
/// State Hash would depend on the rebuild history of a structure declared free to change forever</b>,
/// which is precisely the coupling <c>(derived AND rebuilt)</c> exists to deny.
/// </para>
/// <para>
/// <b>The precedent is <see cref="CellResidency"/>, and the distinguishing test is disposition rather
/// than shape.</b> <c>WheelBucketTable</c> is the counter-precedent and reads the opposite way — <i>"a
/// table rather than a bare array, because a bare array beside the columns is exactly what
/// <c>BOR0901</c> is an error for"</i> — and it is right, because wheel buckets are <b>saved</b>
/// state the simulation reads every Tick. What <c>BOR0901</c> protects against is a field that is
/// saved but not hashed, a defect with no other detector. Storage that is never saved and never
/// hashed cannot have that defect, and <see cref="CellResidency"/> already lives outside a
/// <c>[Table]</c> for the same reason. So: <b>saved → a table; derived and rebuilt wholesale →
/// arrays beside the owner, which is not a <c>[Table]</c> either.</b>
/// </para>
/// <para>
/// <b>Rows are addressed by slot rather than by <c>Handle</c>, and that is safe <em>because</em> they
/// are derived.</b> A handle exists to detect a row that was freed and its slot reused; these arrays
/// are discarded and rebuilt in full whenever any Segment changes, so no entry outlives the rows it
/// names. A generation counter here would be checking against a staleness the rebuild has already
/// made impossible.
/// </para>
/// <para>
/// <b>Grouped by source node, which is what makes <see cref="RoadNodeTable.ArcStart"/> a slice rather
/// than a search.</b> <see cref="RoadGraph.RebuildDerived"/> reserves each node a run and fills it, so
/// a node's Arcs are contiguous — the classic CSR layout, and the same structure-of-arrays the core's
/// tables use.
/// </para>
/// </remarks>
public sealed class RoadArcs
{
    private int[] _target = [];
    private int[] _segment = [];
    private byte[] _modes = [];
    private TravelTime[] _carTime = [];
    private TravelTime[] _footTime = [];
    private int _count;

    /// <summary>How many Arcs the graph currently has. Exactly two per live Segment.</summary>
    public int Count => _count;

    /// <summary>The node slot this Arc leads to. Its source is whichever node's slice holds it.</summary>
    public ReadOnlySpan<int> Target => _target.AsSpan(0, _count);

    /// <summary>The Segment slot this Arc is a direction of.</summary>
    public ReadOnlySpan<int> Segment => _segment.AsSpan(0, _count);

    /// <summary>Which <see cref="TravelMode"/>s may traverse <em>in this direction</em>.</summary>
    public ReadOnlySpan<byte> Modes => _modes.AsSpan(0, _count);

    /// <summary>Free-flow traversal time by car, or <see cref="TravelTime.Impassable"/>.</summary>
    public ReadOnlySpan<TravelTime> CarTime => _carTime.AsSpan(0, _count);

    /// <summary>Free-flow traversal time on foot, or <see cref="TravelTime.Impassable"/>.</summary>
    public ReadOnlySpan<TravelTime> FootTime => _footTime.AsSpan(0, _count);

    /// <summary>Whether an Arc admits a mode at all.</summary>
    public bool Admits(int arc, TravelMode mode) => (_modes[arc] & (byte)mode) != 0;

    /// <summary>The traversal time of an Arc for a mode, without branching on the mode at the call site.</summary>
    public TravelTime TimeFor(int arc, TravelMode mode) =>
        mode == TravelMode.Foot ? _footTime[arc] : _carTime[arc];

    /// <summary>
    /// Discards every Arc and opens a run of <paramref name="count"/> of them, to be filled by
    /// <see cref="Set"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Grown to the largest graph seen and never shrunk</b>, which is the posture the Rule
    /// engine's scratch buffers take: bounded by the size of the world rather than by elapsed time,
    /// so <c>adr/0006</c> is satisfied structurally rather than by an invariant watching it.
    /// </para>
    /// <para>
    /// <b>The run opens filled rather than empty, because the CSR is written out of order.</b> A
    /// node's Arcs occupy a reserved slice and the two Arcs of one Segment land in two different
    /// slices, so the emit walks Segments and scatters — there is no order in which appending
    /// produces a graph grouped by source node. Every reserved position is written exactly once by
    /// construction: the slices partition the run, and each is filled by a cursor that advances once
    /// per incident Segment.
    /// </para>
    /// </remarks>
    internal void Reset(int count)
    {
        if (_target.Length < count)
        {
            _target = new int[count];
            _segment = new int[count];
            _modes = new byte[count];
            _carTime = new TravelTime[count];
            _footTime = new TravelTime[count];
        }

        _count = count;
    }

    /// <summary>Writes one Arc. Called only by <see cref="RoadGraph.RebuildDerived"/>.</summary>
    internal void Set(int arc, int target, int segment, TravelMode modes, TravelTime car, TravelTime foot)
    {
        _target[arc] = target;
        _segment[arc] = segment;
        _modes[arc] = (byte)modes;
        _carTime[arc] = car;
        _footTime[arc] = foot;
    }
}
