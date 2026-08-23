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
/// both would deadlock in one direction: a deposit can never satisfy a waiter that needs <em>space</em>,
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
    private readonly Column<long> _level;

    /// <param name="capacity">Initial slot count.</param>
    /// <param name="buildings">The table this one's <see cref="Owner"/> handles address.</param>
    public BinTable(int capacity, BuildingTable buildings)
    {
        ArgumentNullException.ThrowIfNull(buildings);

        _rows = new Rows<Bin>("bin", capacity, Buffering.OneCopy);

        OwnerKind = _rows.Saved<BinOwnerKind>("owner_kind");
        Owner = _rows.SavedHandle("owner", buildings.Rows);
        Resource = _rows.Saved<ResourceId>("resource");
        _level = _rows.Saved<long>("level", Touch.PerTick);
        Capacity = _rows.Derived<long>("capacity");
        SupplyHead = _rows.Saved<int>("supply_wait_head", Touch.PerTick);
        SupplyTail = _rows.Saved<int>("supply_wait_tail", Touch.PerTick);
        SpaceHead = _rows.Saved<int>("space_wait_head", Touch.PerTick);
        SpaceTail = _rows.Saved<int>("space_wait_tail", Touch.PerTick);
        BinNext = _rows.Derived<int>("bin_next");
        OwnerNext = _rows.SavedHandle("owner_next", _rows);

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<Bin> Rows => _rows;

    /// <summary>
    /// What owns this Bin — a Building, an actor, or the treasury (<c>adr/0114</c>).
    /// </summary>
    /// <remarks>
    /// <b>It is folded, and it has to be.</b> <see cref="Owner"/> folds the target row's monotonic
    /// never-reused id, so two owners of different kinds sharing an id would fold identically; the
    /// kind is what keeps two distinct Bins distinct in the State Hash. It also makes an unset
    /// <see cref="Owner"/> legible instead of ambiguous — see <see cref="BinOwnerKind"/>.
    /// </remarks>
    public Column<BinOwnerKind> OwnerKind { get; }

    /// <summary>
    /// The Building this Bin sits on. <b>Unset unless <see cref="OwnerKind"/> is
    /// <see cref="BinOwnerKind.Building"/></b>, and read only through that.
    /// </summary>
    /// <remarks>
    /// <b>It stayed a <see cref="HandleColumn{TTarget}"/> rather than becoming a polymorphic one</b>,
    /// which is what <c>adr/0114</c>'s <em>gains an owner kind</em> asks for and is the cheaper half
    /// of a real fork. A single column addressing four tables would need a new column type whose fold
    /// dispatches over the kind to four different <c>Rows</c>, and whose dangling check does the same
    /// — machinery paid by every Bin in the world so that one singleton need not be spelled
    /// separately. The treasury has no owner row to point at in any case, so the handle would be
    /// unset there under either shape.
    /// </remarks>
    public HandleColumn<Building> Owner { get; }

    /// <summary>Which Resource this Bin stores. One Bin, one Resource.</summary>
    public Column<ResourceId> Resource { get; }

    /// <summary>
    /// The ceiling. An output that would exceed it fails the whole Rule.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Derived and rebuilt, never saved and hashed</b> (<c>adr/0064</c>). It is a pure function of
    /// <c>(Building kind, Resource)</c> against the Ruleset in force, so it is rebuilt at world load
    /// and at every Ruleset swap — and a retuned ceiling therefore reaches every Building standing
    /// rather than only the next one raised.
    /// </para>
    /// <para>
    /// <b>It was saved, and the alternative to deriving it was a sweep that writes the same numbers at
    /// the same moments.</b> The difference is not cost — <see cref="RuleEngine"/> makes the same array
    /// access either way, and both walk every Bin off the Tick. It is that a saved column periodically
    /// overwritten by a reload is <em>a cache of a derived fact</em>, so <em>the stored ceiling
    /// disagrees with the Ruleset</em> stays a representable state that merely happens to be false.
    /// Here it is not representable, which is the whole purpose of <c>adr/0003</c>'s per-field
    /// declaration and exactly what <c>BOR0901</c> exists to stop somebody writing.
    /// </para>
    /// <para>
    /// <b>The argument is the patch rather than the reload.</b> A reload is design-time and rare; a
    /// shipped Ruleset change under a live city is not. Under <em>as built</em> a retuned capacity
    /// would leave the city permanently divided into Buildings raised before and after the patch,
    /// identical in kind and in every declared respect, different in their ceilings — with the cause
    /// recoverable from no state the world holds. That is `LEGIBLE CAUSE` failing in the one way no
    /// Evidence panel can rescue, because the fact needed is not there to find.
    /// </para>
    /// </remarks>
    public Column<long> Capacity { get; }

    /// <summary>Head of the Rule Instances asleep waiting for this Bin's <see cref="LevelAt"/>.</summary>
    public Column<int> SupplyHead { get; }

    /// <summary>Tail of the level list, so a subscriber appends rather than push-fronts.</summary>
    public Column<int> SupplyTail { get; }

    /// <summary>Head of the Rule Instances asleep waiting for this Bin's <see cref="SpaceAt"/>.</summary>
    public Column<int> SpaceHead { get; }

    /// <summary>Tail of the space list.</summary>
    public Column<int> SpaceTail { get; }

    /// <summary>Link through the owning Building's list of its Bins.</summary>
    public Column<int> BinNext { get; }

    /// <summary>
    /// Link through an <em>Occupant's</em> list of its Bins — a Household's or a Business's.
    /// <b>Unset unless <see cref="OwnerKind"/> is <see cref="BinOwnerKind.Household"/> or
    /// <see cref="BinOwnerKind.Business"/></b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A second link column rather than a second use of <see cref="BinNext"/></b>, because the two
    /// lists differ in disposition and a column is declared once as one or the other. The Building's
    /// list is <see cref="Disposition.Derived"/> and re-threaded by <c>World.RebuildDerived</c> from
    /// <see cref="Owner"/>; this one is <see cref="Disposition.Saved"/> and comes out of the file
    /// already made. ⚠ <b>A Bin is on exactly one of them</b>, which <see cref="OwnerKind"/> decides.
    /// </para>
    /// <para>
    /// <b>Saved because a tenant-owned Bin names nothing</b> (<c>adr/0143</c>). <c>DistrictPoolTable</c>
    /// states the rule this follows: <em>a derived list is only derivable when the element names its
    /// owner</em>. A Building-owned Bin names its Building through <see cref="Owner"/>, so that list
    /// rebuilds; an Occupant-owned Bin names no owner at all, so this relation is in the save or it does
    /// not come back.
    /// </para>
    /// <para>
    /// <b>It is a handle and not a slot index, and that is deliberate.</b>
    /// <see cref="SupplyHead"/> and <see cref="SupplyTail"/> are saved <c>int</c>s holding slot
    /// indices, which fold <em>identity</em> where <c>05 §4</c> asks for values — a
    /// <see cref="HandleColumn{TTarget}"/> folds the target row's monotonic never-reused id instead and
    /// costs nothing extra to declare. ⚠ <b>That precedent is not extended here</b>; whether the wait-list
    /// heads should follow is filed in <c>plans/0012</c> and is not decided by this column.
    /// </para>
    /// </remarks>
    public HandleColumn<Bin> OwnerNext { get; }

    /// <summary>How much is in the Bin. Read freely; a read cannot forget to wake anybody.</summary>
    public long LevelAt(int slot) => _level[slot];

    /// <summary>
    /// How much more this Bin can take before its capacity refuses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>capacity − level</c> is the form <c>CONTEXT</c> → Resource prescribes, and the reason is
    /// this method.</b> An unbounded Bin — every Money Bin and nothing else — carries
    /// <see cref="long.MaxValue"/> underneath, so the space computed as a subtraction from a
    /// non-negative level is always in range, where <c>level + delta &gt; capacity</c> would overflow.
    /// Nothing else in the codebase may reconstruct the comparison the other way round. That is
    /// <c>adr/0031</c>'s named determinism hazard, and <c>adr/0065</c> records that it was never in
    /// danger: the code has always subtracted, so nothing overflowed and nothing inverted.
    /// </para>
    /// <para>
    /// <b>This may return a negative, and a caller that assumes otherwise is wrong rather than
    /// unlucky</b> (<c>adr/0064</c>). Capacity is derived from the Ruleset in force, so a retuned
    /// ceiling can land under a Bin's current level — and the design answer is that the Bin bleeds
    /// down and heals itself. Negative space stops every producer at
    /// <see cref="RuleEngine"/>'s own affordability test and leaves every consumer untouched, which
    /// works because <see cref="Borough.Core.Arithmetic.IntegerMath.FloorDiv"/> rounds toward
    /// negative infinity: any negative space yields <c>affordable ≤ −1</c>, below even a derived
    /// floor of zero. Under C#'s truncating <c>/</c> that case would have passed the guard, so
    /// <c>BOR0203</c> is load-bearing here, in a place nobody aimed it.
    /// </para>
    /// </remarks>
    public long SpaceAt(int slot) => Capacity[slot] - _level[slot];

    /// <summary>Allocates a Bin on a Building, empty. Linking it in is <see cref="World"/>'s.</summary>
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
    internal Handle<Bin> Create(Handle<Building> owner, ResourceId resource, long capacity)
    {
        Handle<Bin> handle = _rows.Allocate();
        int slot = _rows.Resolve(handle);

        OwnerKind[slot] = BinOwnerKind.Building;
        Owner[slot] = owner;
        Resource[slot] = resource;
        Capacity[slot] = capacity;
        _level[slot] = 0;

        return handle;
    }

    /// <summary>Allocates a Bin on an owner that is not a Building, empty.</summary>
    /// <remarks>
    /// <b><see cref="Owner"/> is left unset on purpose and <see cref="OwnerKind"/> is what says so.</b>
    /// The treasury is a singleton with no row in <see cref="BuildingTable"/> to address, which is the
    /// entity decision <c>RuleEngine</c>'s <c>Scope.Global</c> throw was waiting on: <em>a city-wide
    /// Bin is one no Building owns.</em> The level is written for
    /// <see cref="Create(Handle{Building}, ResourceId, long)"/>'s reason — a recycled slot carries its
    /// predecessor's contents.
    /// </remarks>
    internal Handle<Bin> Create(BinOwnerKind kind, ResourceId resource, long capacity)
    {
        Handle<Bin> handle = _rows.Allocate();
        int slot = _rows.Resolve(handle);

        OwnerKind[slot] = kind;
        Owner[slot] = default;
        Resource[slot] = resource;
        Capacity[slot] = capacity;
        _level[slot] = 0;

        return handle;
    }

    /// <summary>
    /// Writes a Bin's derived ceiling. <see cref="World.RebuildCapacities"/> is the only caller.
    /// </summary>
    /// <remarks>
    /// <b><see cref="Capacity"/> is a public <see cref="Column{T}"/> and this method still exists</b>,
    /// which looks redundant and is not: it is where the *one writer* lives, so that the derivation
    /// has a single spelling and a search for it finds one site rather than an assignment anybody
    /// could have made. That is <see cref="Move"/>'s argument applied to the other column — the level
    /// hides behind a method because a write must drain a wait list, and the ceiling hides behind one
    /// because a write must come from the Ruleset (<c>adr/0064</c>).
    /// </remarks>
    internal void SetCapacity(int slot, long units) => Capacity[slot] = units;

    /// <summary>
    /// Moves the level. The one writer, and it is <see langword="internal"/> so that the drain cannot
    /// be skipped by anything outside <see cref="World"/>.
    /// </summary>
    internal void Move(int slot, long delta) => _level[slot] += delta;
}
