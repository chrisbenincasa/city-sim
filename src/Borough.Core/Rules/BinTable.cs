namespace Borough.Core.Rules;

using Borough.Core.Entities;
using Borough.Core.Tables;

/// <summary>
/// The Bins: one integer store of one Resource on one Building, each with its wait list.
/// </summary>
/// <remarks>
/// <para>
/// <b>There is no public level column, and that is structural rather than stylistic.</b> <c>05 §9</c>
/// is explicit about the failure it prevents: <em>a Bin written without draining its wait list leaves
/// that Building asleep for ever, with no error and no timer to rescue it.</em> A writable
/// <see cref="Column{T}"/> hanging off this table would make that the easiest spelling available, so
/// the column is private and <see cref="World.Deposit"/> is the only door. Reading is free — see
/// <see cref="LevelAt"/> — because a read cannot forget to wake anybody.
/// </para>
/// <para>
/// <b>A Bin is a row rather than a slot on the Building, and the choice is the collection rule
/// applied.</b> Which Bins a Building has is declared per Building kind, so the count varies by kind
/// and a fixed width on the Building would be paid by every Building for the widest one. The Bins of
/// a Building are therefore an intrusive index list through <see cref="BinNext"/> — derived, because
/// membership is a pure function of <see cref="Owner"/> and the order carries no meaning, a lookup
/// being a search either way.
/// </para>
/// <para>
/// <b>The wait lists are the opposite call, and <see cref="IndexList.InsertOrdered"/> says why.</b>
/// Arrival order is what makes the round-robin drain fair and it is recoverable from nothing else, so
/// a wait list is state: its head and tail are <see cref="Disposition.Saved"/> and it is appended to,
/// never inserted into.
/// </para>
/// <para>
/// <b>There are two of them, because <c>adr/0045</c>'s <em>blocking</em> generalises over two failure
/// modes</b> — <em>refill if the Bin was short, drain if it was a full output.</em> One list holding
/// both would deadlock in one direction: a deposit can never satisfy a waiter that needs headroom,
/// and a drain that stops at the first waiter it cannot cover — which is what makes the queue fair —
/// would stop at that one for ever. Skipping past it instead is the other defect, the one that
/// starves a large waiter behind small ones. Two lists remove the choice.
/// </para>
/// </remarks>
[Table]
public sealed class BinTable
{
    private readonly Rows<Bin> _rows;

    /// <summary>
    /// The level. Private, so that <see cref="World.Deposit"/> is the only way to move it.
    /// </summary>
    private readonly Column<int> _level;

    /// <param name="capacity">Initial slot count.</param>
    /// <param name="buildings">The table this one's <see cref="Owner"/> handles address.</param>
    public BinTable(int capacity, BuildingTable buildings)
    {
        ArgumentNullException.ThrowIfNull(buildings);

        _rows = new Rows<Bin>("bin", capacity, Buffering.OneCopy);

        Owner = _rows.SavedHandle("owner", buildings.Rows);
        Resource = _rows.Saved<ResourceId>("resource");
        _level = _rows.Saved<int>("level", Touch.PerTick);
        Capacity = _rows.Saved<int>("capacity");
        LevelHead = _rows.Saved<int>("level_wait_head", Touch.PerTick);
        LevelTail = _rows.Saved<int>("level_wait_tail", Touch.PerTick);
        HeadroomHead = _rows.Saved<int>("headroom_wait_head", Touch.PerTick);
        HeadroomTail = _rows.Saved<int>("headroom_wait_tail", Touch.PerTick);
        BinNext = _rows.Derived<int>("bin_next");

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<Bin> Rows => _rows;

    /// <summary>The Building this Bin sits on.</summary>
    public HandleColumn<Building> Owner { get; }

    /// <summary>Which Resource this Bin stores. One Bin, one Resource.</summary>
    public Column<ResourceId> Resource { get; }

    /// <summary>The ceiling. An output that would exceed it fails the whole Rule.</summary>
    public Column<int> Capacity { get; }

    /// <summary>Head of the Rule Instances asleep waiting for this Bin's <see cref="LevelAt"/>.</summary>
    public Column<int> LevelHead { get; }

    /// <summary>Tail of the level list, so a subscriber appends rather than push-fronts.</summary>
    public Column<int> LevelTail { get; }

    /// <summary>Head of the Rule Instances asleep waiting for this Bin's <see cref="HeadroomAt"/>.</summary>
    public Column<int> HeadroomHead { get; }

    /// <summary>Tail of the headroom list.</summary>
    public Column<int> HeadroomTail { get; }

    /// <summary>Link through the owning Building's list of its Bins.</summary>
    public Column<int> BinNext { get; }

    /// <summary>How much is in the Bin. Read freely; a read cannot forget to wake anybody.</summary>
    public int LevelAt(int slot) => _level[slot];

    /// <summary>
    /// How much more this Bin can take before its capacity refuses.
    /// </summary>
    /// <remarks>
    /// <b><c>capacity − level</c> is the form <c>CONTEXT</c> → Resource prescribes, and the reason is
    /// this method.</b> An unbounded Bin — every Money Bin and nothing else — carries
    /// <see cref="int.MaxValue"/> underneath, so a headroom computed as a subtraction from a
    /// non-negative level is always in range, where <c>level + delta &gt; capacity</c> would overflow.
    /// Nothing else in the codebase may reconstruct the comparison the other way round.
    /// </remarks>
    public int HeadroomAt(int slot) => Capacity[slot] - _level[slot];

    /// <summary>Allocates an empty Bin on a Building. Linking it in is <see cref="World"/>'s.</summary>
    /// <summary>Allocates a Bin on a Building, empty.</summary>
    /// <remarks>
    /// <b>The level is written rather than assumed, because a recycled slot carries its predecessor's
    /// contents.</b> <see cref="Rows{T}.Allocate"/> hands back a free slot without clearing any
    /// column — it has never promised to — and until slice 10 nothing in a running simulation ever
    /// freed a Bin, so the omission cost nothing. Demolition is what makes it reachable: the next
    /// Building raised on a cleared Lot would open its doors with whatever the condemned one still had
    /// in store, which is goods created from nothing and would read as a generous city rather than as
    /// a defect. The wait-list columns need no such line — <see cref="IndexList"/> encodes an empty
    /// list as zero, so a fresh slot is already empty and a drained one is empty again.
    /// </remarks>
    internal Handle<Bin> Create(Handle<Building> owner, ResourceId resource, BinCapacity capacity)
    {
        Handle<Bin> handle = _rows.Allocate();
        int slot = _rows.Resolve(handle);

        Owner[slot] = owner;
        Resource[slot] = resource;
        Capacity[slot] = capacity.Units;
        _level[slot] = 0;

        return handle;
    }

    /// <summary>
    /// Moves the level. The one writer, and it is <see langword="internal"/> so that the drain cannot
    /// be skipped by anything outside <see cref="World"/>.
    /// </summary>
    internal void Move(int slot, int delta) => _level[slot] += delta;
}
