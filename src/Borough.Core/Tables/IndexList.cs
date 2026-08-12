namespace Borough.Core.Tables;

/// <summary>
/// A singly-linked list threaded through two flat columns: a head and tail index on the owner, a
/// <c>next</c> index on the element.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every variable-length collection in <c>Borough.Core</c> is one of these.</b> Never a
/// per-entity collection object. It allocates nothing, traces nothing, gives adr/0033's round-robin
/// drain its deterministic order for free, and survives a port unchanged. S4's K6 measured the
/// difference the alternative makes at the 1M target — <b>6.984 ms against 100.200 ms</b> — which is
/// what turned this from a preference into <c>BOR0701</c>.
/// </para>
/// <para>
/// <b>It is a <c>ref struct</c> over spans rather than a type holding columns</b>, because a struct
/// holding three <see cref="Column{T}"/> references is exactly the managed simulation state lint 7
/// forbids. Binding the spans once and walking them is also the hot-path shape: three streams, index
/// order, no indirection through a class per access.
/// </para>
/// <para>
/// <b>Zero means empty, and that is load-bearing rather than a micro-optimisation.</b> Slots are
/// zero-filled when a table grows and zeroed again when a row is freed, so a list whose terminator
/// were <c>-1</c> would read a freshly allocated owner's head as <em>slot 0</em> — a new Building
/// silently owning the first Household in the city. The stored form is therefore the slot index plus
/// one, and the encoding never leaves this type.
/// </para>
/// <para>
/// <b>Appending at the tail, not the head.</b> adr/0033's round-robin drain wants the order things
/// arrived in; a push-front list hands back the reverse, which is a different city rather than a
/// different implementation. The three consumers — Bin wait lists, Parking Sheds, Event Wheel buckets
/// — arrive in later slices and must not invent their own.
/// </para>
/// </remarks>
public readonly ref struct IndexList
{
    private readonly Span<int> _head;
    private readonly Span<int> _tail;
    private readonly Span<int> _next;

    /// <param name="head">A column on the owner table: the first element, encoded.</param>
    /// <param name="tail">A column on the owner table: the last element, encoded.</param>
    /// <param name="next">A column on the element table: the following element, encoded.</param>
    public IndexList(Column<int> head, Column<int> tail, Column<int> next)
    {
        ArgumentNullException.ThrowIfNull(head);
        ArgumentNullException.ThrowIfNull(tail);
        ArgumentNullException.ThrowIfNull(next);

        _head = head.Span;
        _tail = tail.Span;
        _next = next.Span;
    }

    /// <summary>
    /// The same list over an owner that is not a table row.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>For an owner indexed by <em>coordinate</em> rather than by slot.</b>
    /// <see cref="Space.BuildingResidency"/> is the case: its owner is a Cell, of which there are
    /// 16,384 whether or not any of them holds anything, so the head and tail live in flat arrays
    /// beside the table. A column would have to invent 16,384 rows to avoid two arrays, and
    /// <c>BOR0901</c> would reject them in a <c>[Table]</c> type anyway — the same argument
    /// <see cref="Space.CellResidency"/> makes for the sparse Layer index.
    /// </para>
    /// <para>
    /// <b>The element side is still a column</b>, because an element <em>is</em> a row and its
    /// <c>next</c> is per-row storage. The asymmetry is the point rather than a compromise: what
    /// makes this an index list is the threading, not where the head happens to be kept.
    /// </para>
    /// </remarks>
    /// <param name="head">The owner's first element, encoded. Indexed by whatever the owner is.</param>
    /// <param name="tail">The owner's last element, encoded.</param>
    /// <param name="next">A column on the element table: the following element, encoded.</param>
    public IndexList(Span<int> head, Span<int> tail, Column<int> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        _head = head;
        _tail = tail;
        _next = next.Span;
    }

    /// <summary>True when the owner has no elements.</summary>
    public bool IsEmpty(int owner) => _head[owner] == 0;

    /// <summary>How many elements the owner has.</summary>
    /// <remarks>
    /// <b>A walk rather than a stored counter, and the reason is <c>adr/0006</c>'s question rather
    /// than a cost argument.</b> A count column would be a second place for a fact the list already
    /// holds, and the question *what keeps it true* has no answer a walk needs. The lists this is
    /// asked of — a Building's Occupants, a Building's Bins — are a handful by construction, and the
    /// one caller on a per-Tick path (<see cref="Rules.Readouts"/>) is reading exactly this.
    /// </remarks>
    public int Length(int owner)
    {
        int count = 0;

        foreach (int _ in Walk(owner))
        {
            count++;
        }

        return count;
    }

    /// <summary>Adds an element at the end, which is where a queue's order comes from.</summary>
    public void Append(int owner, int node)
    {
        _next[node] = 0;

        if (_head[owner] == 0)
        {
            _head[owner] = node + 1;
        }
        else
        {
            _next[_tail[owner] - 1] = node + 1;
        }

        _tail[owner] = node + 1;
    }

    /// <summary>
    /// Adds an element at the position that keeps the list in ascending slot order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the insert a <see cref="Disposition.Derived"/> list must use, and the distinction
    /// is not stylistic.</b> A derived list claims to be a pure function of saved state, which means
    /// a rebuild has to reproduce it exactly — and a rebuild walks the owner table in index order,
    /// because that is the only order a rebuild has. <see cref="Append"/> reproduces <em>creation</em>
    /// order instead, and the two diverge the moment the free list recycles a slot: create A and B,
    /// free A, create C, and appending gives <c>B, C</c> where a rebuild gives <c>C, B</c>. Neither
    /// is folded into the hash, so nothing would report it; the drain order of the list would simply
    /// differ between a saved-and-reloaded city and a continuously-run one.
    /// </para>
    /// <para>
    /// <b>The corollary is the important half: a list whose order carries meaning cannot be
    /// derived.</b> A Bin's wait list is drained round-robin in <em>arrival</em> order, and arrival
    /// order is not recoverable from anything else — so it is state, declared
    /// <see cref="Disposition.Saved"/>, and it uses <see cref="Append"/>. Slice 7 owns that
    /// declaration; this is where the reasoning behind it is written down.
    /// </para>
    /// </remarks>
    public void InsertOrdered(int owner, int node)
    {
        int encoded = _head[owner];
        int previous = Rows.NoSlot;

        while (encoded != 0 && encoded - 1 < node)
        {
            previous = encoded - 1;
            encoded = _next[previous];
        }

        _next[node] = encoded;

        if (previous == Rows.NoSlot)
        {
            _head[owner] = node + 1;
        }
        else
        {
            _next[previous] = node + 1;
        }

        if (encoded == 0)
        {
            _tail[owner] = node + 1;
        }
    }

    /// <summary>
    /// The first element without removing it, or <see cref="Rows.NoSlot"/> when the list is empty.
    /// </summary>
    /// <remarks>
    /// <b>What a Bin's drain needs, and the reason it is separate from <see cref="PopFront"/>.</b>
    /// The drain stops at the first waiter the arriving quantity cannot cover and <em>leaves it at the
    /// head</em> — skipping it would starve a large waiter behind small ones, and popping it to look
    /// at it would lose its place. So the decision has to be made before the removal.
    /// </remarks>
    public int PeekFront(int owner)
    {
        int encoded = _head[owner];
        return encoded == 0 ? Rows.NoSlot : encoded - 1;
    }

    /// <summary>
    /// Removes and returns the first element, or <see cref="Rows.NoSlot"/> when the list is empty.
    /// </summary>
    public int PopFront(int owner)
    {
        int encoded = _head[owner];
        if (encoded == 0)
        {
            return Rows.NoSlot;
        }

        int node = encoded - 1;
        _head[owner] = _next[node];

        if (_head[owner] == 0)
        {
            _tail[owner] = 0;
        }

        _next[node] = 0;
        return node;
    }

    /// <summary>
    /// Unlinks one element, walking from the head to find its predecessor.
    /// </summary>
    /// <remarks>
    /// <b>O(n) in the list's length, and that is the accepted cost of a singly-linked list.</b> The
    /// alternative is a <c>prev</c> column on every element — four bytes per row across every table
    /// that owns a list — bought for a case none of the three named consumers has: a wait list drains
    /// from the front, a Wheel bucket drains whole, and a Parking Shed is rebuilt rather than edited.
    /// If a consumer appears that removes from the middle of a long list, that is when the column is
    /// worth adding, and it is a local change.
    /// </remarks>
    /// <returns><see langword="true"/> if the element was in the owner's list.</returns>
    public bool Remove(int owner, int node)
    {
        int encoded = _head[owner];
        int previous = Rows.NoSlot;

        while (encoded != 0)
        {
            int current = encoded - 1;

            if (current == node)
            {
                if (previous == Rows.NoSlot)
                {
                    _head[owner] = _next[node];
                }
                else
                {
                    _next[previous] = _next[node];
                }

                if (_tail[owner] == node + 1)
                {
                    _tail[owner] = previous == Rows.NoSlot ? 0 : previous + 1;
                }

                _next[node] = 0;
                return true;
            }

            previous = current;
            encoded = _next[current];
        }

        return false;
    }

    /// <summary>Drops every element, without touching their <c>next</c> links.</summary>
    /// <remarks>
    /// Correct only when the elements are about to be freed or re-linked, which is the rebuild case
    /// a derived list has. A live element left holding a stale <c>next</c> is a list that rejoins
    /// itself the next time it is appended to.
    /// </remarks>
    public void Clear(int owner)
    {
        _head[owner] = 0;
        _tail[owner] = 0;
    }

    /// <summary>Walks the owner's elements, front to back, in the order they were appended.</summary>
    public IndexListWalk Walk(int owner) => new(_next, _head[owner]);
}

/// <summary>The enumerator half of <see cref="IndexList.Walk"/>.</summary>
public ref struct IndexListWalk
{
    private readonly Span<int> _next;
    private int _encoded;

    internal IndexListWalk(Span<int> next, int firstEncoded)
    {
        _next = next;
        _encoded = firstEncoded;
        Current = Rows.NoSlot;
    }

    /// <summary>The slot the walk is standing on.</summary>
    public int Current { get; private set; }

    /// <summary>Present so that the walk can be the subject of a <c>foreach</c>.</summary>
    public readonly IndexListWalk GetEnumerator() => this;

    /// <summary>Advances to the next element.</summary>
    public bool MoveNext()
    {
        if (_encoded == 0)
        {
            return false;
        }

        Current = _encoded - 1;
        _encoded = _next[Current];
        return true;
    }
}
