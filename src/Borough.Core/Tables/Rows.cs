namespace Borough.Core.Tables;

using Borough.Core.Determinism;

/// <summary>
/// The shared half of a hand-written table: a generation array, a free list, a monotonic id, a count,
/// and the columns in declaration order.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is a handful of helpers held by a table, not a base class a table inherits.</b> adr/0004
/// is explicit that each table is hand-written and that a general table framework of our own is the
/// yak-shave version of the decision — <em>how ten dead libraries get written instead of one game</em>.
/// Eight to fifteen entity types, all known at compile time, is few enough that hand-writing each is
/// cheaper than the abstraction that would generalise them, and the abstraction is how an ECS gets in
/// through the back door. What is shared here is storage bookkeeping that would otherwise be copied
/// verbatim a dozen times; the schema, the invariants and the behaviour stay in the table's own file.
/// </para>
/// <para>
/// <b>Declaring a column is what allocates it</b>, which is what makes adr/0003's field rule
/// structural. There is no way to obtain storage without stating a <see cref="Disposition"/>, so the
/// State Hash cannot have a coverage hole — a field that is saved but not hashed is invisible to every
/// tool in the project, and this is the only construction under which that state is unreachable rather
/// than merely discouraged.
/// </para>
/// <para>
/// <b>Composition order, stated because it is a hash input and not an implementation detail:</b>
/// <em>tables in declaration order, arrays in index order</em>, folded through slice 2's <c>mix</c>.
/// </para>
/// </remarks>
public abstract class Rows
{
    /// <summary>The free-list and intrusive-list terminator. Never a valid slot.</summary>
    /// <remarks>
    /// Public because it is what <see cref="IndexList.PopFront"/> hands back on an empty list, so a
    /// caller outside this assembly needs to be able to name it. It is <em>not</em> what an intrusive
    /// list stores — the stored form is the slot index plus one, so that a zeroed row reads as an
    /// empty list rather than as one containing slot 0.
    /// </remarks>
    public const int NoSlot = -1;

    private const int MinimumCapacity = 8;

    private List<Column>? _declaring = [];
    private Column[] _columns = [];

    private Column[] _savedColumns = [];

    private readonly Column<ulong> _id;
    private readonly Column<uint> _generation;
    private readonly Column<int> _freeNext;

    private int _capacity;
    private int _slotCount;
    private int _liveCount;
    private int _freeHead = NoSlot;
    private ulong _nextId = 1;

    private protected Rows(string name, int capacity, Buffering buffering)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        Name = name;
        Buffering = buffering;
        _capacity = capacity < MinimumCapacity ? MinimumCapacity : capacity;

        // The three intrinsic columns are declared first, so they fold first. They go through the
        // same declaration as everything else deliberately: the allocator's own state decides which
        // slot the next entity lands in, so a divergence in it is a divergence in the city, and the
        // hash should catch it at the Tick it happens rather than ten thousand Ticks later.
        _id = Saved<ulong>("id", Touch.Wake);
        _generation = Saved<uint>("generation", Touch.Wake);
        _freeNext = Saved<int>("free_next", Touch.Cold);
    }

    /// <summary>
    /// The table's name in the declaration — a schema identifier for the save header and the
    /// footprint report, never a Readout. See <see cref="Column.Name"/>.
    /// </summary>
    public string Name { get; }

    /// <summary>adr/0037's per-table property. Every table is <see cref="Buffering.OneCopy"/> in Phase 1.</summary>
    public Buffering Buffering { get; }

    /// <summary>The high-water slot count — the length every column is iterated to.</summary>
    /// <remarks>
    /// Not the live count. Freed slots stay inside this range holding zeroes, because the free list
    /// will hand them out again and because a table that compacted would relocate rows, which
    /// <see cref="Handle{T}"/>'s generation counter cannot detect.
    /// </remarks>
    public int SlotCount => _slotCount;

    /// <summary>How many rows are live. The number the footprint report and the invariants use.</summary>
    public int LiveCount => _liveCount;

    /// <summary>Allocated slots, live or free. A sizing figure; it never reaches the hash.</summary>
    public int Capacity => _capacity;

    /// <summary>Every column, in declaration order — which is the order the hash folds them in.</summary>
    public ReadOnlySpan<Column> Columns => _columns;

    /// <summary>
    /// The <see cref="Disposition.Saved"/> columns, in declaration order. What the hash folds and
    /// what the save writes, which are the same set by construction.
    /// </summary>
    /// <remarks>
    /// <b>One accessor rather than the fourth hand-written filter.</b> Three call sites wrote the
    /// <c>if</c> themselves before milestone 8 — the fold here, the footprint report, and a test — and
    /// the save would have been a fourth. The set is the same question each time and the answers had
    /// no way to disagree loudly, which is the shape <c>plans/0012</c> <em>Cause 1</em> describes:
    /// several copies of one fact, none of them able to notice the others. Computed once at
    /// <see cref="Seal"/>, because the declaration is closed there and cannot change afterwards.
    /// </remarks>
    public ReadOnlySpan<Column> SavedColumns => _savedColumns;

    /// <summary>Declares a column of state: written to the save and folded into the hash.</summary>
    public Column<TField> Saved<TField>(string name, Touch touch = Touch.Wake)
        where TField : unmanaged =>
        Declare(new Column<TField>(this, name, Disposition.Saved, touch, _capacity));

    /// <summary>Declares a cache: neither saved nor hashed, and rebuilt from saved state on load.</summary>
    public Column<TField> Derived<TField>(string name, Touch touch = Touch.Wake)
        where TField : unmanaged =>
        Declare(new Column<TField>(this, name, Disposition.Derived, touch, _capacity));

    /// <summary>
    /// Declares scratch: neither saved, hashed, nor rebuilt, and read only inside the phase that
    /// writes it.
    /// </summary>
    /// <remarks>
    /// Read <see cref="Disposition.Scratch"/> before choosing this. It carries an obligation the
    /// other two do not — nothing may read the column outside the phase that wrote it — and the
    /// rebuild audit skips it on the strength of that obligation.
    /// </remarks>
    public Column<TField> Scratch<TField>(string name, Touch touch = Touch.Wake)
        where TField : unmanaged =>
        Declare(new Column<TField>(this, name, Disposition.Scratch, touch, _capacity));

    /// <summary>
    /// Declares a column of handles into <paramref name="target"/>. Saved, and hashed by the target's
    /// monotonic id rather than by the slot the handle happens to address.
    /// </summary>
    public HandleColumn<TTarget> SavedHandle<TTarget>(
        string name, Rows<TTarget> target, Touch touch = Touch.Wake,
        Reference reference = Reference.Required)
        where TTarget : unmanaged =>
        Declare(new HandleColumn<TTarget>(
            this, name, Disposition.Saved, touch, _capacity, target, reference));

    /// <inheritdoc cref="SavedHandle{TTarget}(string, Rows{TTarget}, Touch, Reference)"/>
    /// <summary>Declares a rebuilt column of handles — an index, not state.</summary>
    public HandleColumn<TTarget> DerivedHandle<TTarget>(
        string name, Rows<TTarget> target, Touch touch = Touch.Wake,
        Reference reference = Reference.Required)
        where TTarget : unmanaged =>
        Declare(new HandleColumn<TTarget>(
            this, name, Disposition.Derived, touch, _capacity, target, reference));

    /// <summary>
    /// Closes the declaration. Called at the end of a table's constructor; nothing can be allocated
    /// before it and no column can be declared after it.
    /// </summary>
    /// <remarks>
    /// The sealing is not ceremony. Declaring a column after rows exist would leave the new column
    /// zeroed for every existing row while the rest of the table believes they are populated — a
    /// schema change applied to half a table, which is the one failure the single declaration cannot
    /// see because the declaration would be complete.
    /// </remarks>
    public void Seal()
    {
        if (_declaring is null)
        {
            throw new InvalidOperationException($"table '{Name}' is already sealed.");
        }

        _columns = [.. _declaring];
        _savedColumns = [.. _declaring.FindAll(column => column.Disposition == Disposition.Saved)];
        _declaring = null;
    }

    /// <summary>Bytes of column storage per slot, for one <see cref="Touch"/> tier.</summary>
    /// <remarks>
    /// Every column counts, derived ones included: a rebuilt cache costs the same memory as state
    /// does. K0's footprint table is this figure times <see cref="LiveCount"/>, per tier, per table.
    /// </remarks>
    public int BytesPerRow(Touch touch)
    {
        int total = 0;
        foreach (Column column in _columns)
        {
            if (column.Touch == touch)
            {
                total += column.BytesPerRow;
            }
        }

        return total;
    }

    /// <summary>
    /// Bytes of <see cref="Disposition.Saved"/> column storage per slot — the width of one row in the
    /// save.
    /// </summary>
    /// <remarks>
    /// <b>The figure <c>adr/0087</c> names as owed and forbids guessing.</b> It is deliberately not
    /// <see cref="BytesPerRow(Touch)"/> summed over the three tiers: that figure counts every column,
    /// derived and scratch included, because it sizes <em>memory</em>. This sizes the <em>file</em>,
    /// and the two differ by whatever the declaration says is rebuildable. ⚠ <b>S0a's 85.98 MiB at 1M
    /// is the memory figure and must never be quoted as a save's size</b> (<c>plans/0012</c>
    /// <em>Cause 5</em>).
    /// </remarks>
    public int SavedBytesPerRow
    {
        get
        {
            int total = 0;

            foreach (Column column in _savedColumns)
            {
                total += column.BytesPerRow;
            }

            return total;
        }
    }

    /// <summary>Folds this table's saved state into <paramref name="hash"/>, in index order.</summary>
    public void Fold(ref ulong hash)
    {
        ulong h = hash;

        // The allocator's scalars first. _freeHead is -1 when the list is empty and sign-extends to
        // a value no slot index can collide with, which is the intent.
        h = Randomness.Mix(h + (ulong)(long)_slotCount);
        h = Randomness.Mix(h + (ulong)(long)_liveCount);
        h = Randomness.Mix(h + (ulong)(long)_freeHead);
        h = Randomness.Mix(h + _nextId);

        foreach (Column column in _savedColumns)
        {
            column.Fold(ref h, _slotCount);
        }

        hash = h;
    }

    /// <summary>
    /// Folds <em>every</em> column, derived ones included, plus the allocator's scalars.
    /// </summary>
    /// <remarks>
    /// <b>This is not the State Hash and must never be used as one.</b> <see cref="Fold"/> folds saved
    /// state, which is the declaration's whole point; this folds storage. It exists for the one
    /// question the State Hash cannot answer — <em>did anything at all change?</em> — which is what
    /// <c>Simulation.VerifyDecideWritesNothing</c> asks around Phase 2. A guard built on the State Hash
    /// would wave through a write to a derived column, and a derived column is precisely where a Rule
    /// evaluating in Decide would cache something.
    /// </remarks>
    internal void FoldAll(ref ulong hash)
    {
        ulong h = hash;

        h = Randomness.Mix(h + (ulong)(long)_slotCount);
        h = Randomness.Mix(h + (ulong)(long)_liveCount);
        h = Randomness.Mix(h + (ulong)(long)_freeHead);
        h = Randomness.Mix(h + _nextId);

        foreach (Column column in _columns)
        {
            column.Fold(ref h, _slotCount);
        }

        hash = h;
    }

    /// <summary>
    /// Seeds the write half from the live half, opening a double-buffered phase's write window.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Called once per parallel write, not once per Tick, and the distinction is the whole reason
    /// <c>adr/0037</c> deleted the full-world buffer.</b> The copy this makes is bounded by one table
    /// rather than by the world, and it happens on that table's cadence — Map Layer cells at
    /// <c>tick % 64</c>, not every Tick. The thing <c>05 §9</c> priced at ~150 MB against ~1 MB of
    /// writes was a copy of everything, every Tick, including every sleeping Citizen.
    /// </para>
    /// <para>
    /// <b>Every column, not only the ones the phase intends to write.</b> A phase that writes one
    /// column and swaps would otherwise swap the rest to whatever they held a cycle ago — including
    /// the allocator's own <c>id</c> and <c>generation</c>, which would resurrect freed rows. Seeding
    /// uniformly makes the swap a no-op for anything untouched, which is the only version of this that
    /// does not depend on a caller enumerating its writes correctly.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">The table declared <see cref="Buffering.OneCopy"/>.</exception>
    public void PrepareBack()
    {
        RequireTwoCopies();

        foreach (Column column in _columns)
        {
            column.PrepareBack();
        }
    }

    /// <summary>Makes the write half live, closing the window <see cref="PrepareBack"/> opened.</summary>
    /// <exception cref="InvalidOperationException">The table declared <see cref="Buffering.OneCopy"/>.</exception>
    public void SwapBuffers()
    {
        RequireTwoCopies();

        foreach (Column column in _columns)
        {
            column.SwapBuffers();
        }
    }

    /// <summary>True while the slot holds a live row. Live generations are odd; free ones are even.</summary>
    public bool IsLive(int slot) => (uint)slot < (uint)_slotCount && (_generation[slot] & 1u) == 1u;

    /// <summary>The monotonic never-reused id at a live slot. Zero at a free one.</summary>
    /// <remarks>
    /// <b>This is the stable per-entity key, and the reason a handle index must never be one.</b>
    /// Indices are recycled by the free list, so ordering by one means an unrelated demolition on the
    /// far side of the city can silently change who wins a contested draw downtown. This id is never
    /// recycled. It is also not, on its own, a licence to order by: <c>02 §8</c> rule 5 settles the
    /// case that prompted the rule with a counter-based random shuffle instead, because ordering by
    /// entity id is <em>biased</em> — the same Building wins every contested draw for the life of the
    /// city and no player can see why — and because <em>two bakeries reached for the same six flour
    /// and one got it</em> is a complete explanation where <em>it has a lower id</em> is not one at all.
    /// </remarks>
    public ulong IdAt(int slot) => _id[slot];

    private protected uint GenerationAt(int slot) => _generation[slot];

    private protected int AllocateSlot(out uint generation)
    {
        if (_declaring is not null)
        {
            throw new InvalidOperationException(
                $"table '{Name}' has not been sealed; a row was allocated while the schema was open.");
        }

        int slot;

        if (_freeHead != NoSlot)
        {
            slot = _freeHead;
            _freeHead = _freeNext[slot];
        }
        else
        {
            if (_slotCount == _capacity)
            {
                Grow();
            }

            slot = _slotCount++;
        }

        _freeNext[slot] = NoSlot;

        // Even to odd: the slot becomes live, and every handle ever issued against its previous
        // occupant now carries a generation one lower. Wrapping needs 2^31 allocate/free cycles on a
        // single slot; parity is preserved across the wrap, so generation 0 stays reserved for
        // Handle<T>.None.
        generation = unchecked(_generation[slot] + 1);
        _generation[slot] = generation;
        _id[slot] = _nextId++;
        _liveCount++;

        return slot;
    }

    private protected void FreeSlot(int slot)
    {
        // Read the generation before the clear, not after: the generation column is a column like
        // any other and the loop below zeroes it too. Bumping the zero would hand the slot an odd
        // generation — which is this file's encoding for *live* — so a freed row would read as
        // occupied and every handle to it would resolve. Slice 4's own lifecycle test caught this.
        uint generation = unchecked(_generation[slot] + 1);

        // Zeroing is what lets the hash fold every slot in index order with no liveness branch, and
        // it makes a freed row read as freed in a debugger rather than as its previous occupant.
        foreach (Column column in _columns)
        {
            column.Clear(slot);
        }

        _generation[slot] = generation;
        _freeNext[slot] = _freeHead;
        _freeHead = slot;
        _liveCount--;
    }

    private TColumn Declare<TColumn>(TColumn column)
        where TColumn : Column
    {
        if (_declaring is null)
        {
            throw new InvalidOperationException(
                $"table '{Name}' is sealed; column '{column.Name}' was declared after it.");
        }

        foreach (Column existing in _declaring)
        {
            if (existing.Name == column.Name)
            {
                throw new ArgumentException(
                    $"table '{Name}' already declares a column named '{column.Name}'.", nameof(column));
            }
        }

        _declaring.Add(column);
        return column;
    }

    private void RequireTwoCopies()
    {
        if (Buffering != Buffering.TwoCopies)
        {
            throw new InvalidOperationException(
                $"table '{Name}' declared Buffering.OneCopy and has no write half. adr/0037: a table "
                + "is double-buffered if and only if a parallel phase both reads and writes it.");
        }
    }

    private void Grow()
    {
        _capacity *= 2;

        foreach (Column column in _columns)
        {
            column.Grow(_capacity);
        }
    }
}

/// <summary>
/// The typed half: the only place <see cref="Handle{T}"/> values are minted, resolved and retired.
/// </summary>
/// <typeparam name="T">The entity tag this table stores rows of.</typeparam>
public sealed class Rows<T> : Rows
    where T : unmanaged
{
    /// <param name="name">The table's schema name.</param>
    /// <param name="capacity">Initial slot count. A sizing hint; the table grows as needed.</param>
    /// <param name="buffering">adr/0037's per-table property. Single unless a parallel phase writes it.</param>
    public Rows(string name, int capacity, Buffering buffering = Buffering.OneCopy)
        : base(name, capacity, buffering)
    {
    }

    /// <summary>Takes a free slot, or a fresh one, and returns a handle to it.</summary>
    public Handle<T> Allocate()
    {
        int slot = AllocateSlot(out uint generation);
        return new Handle<T>((uint)slot, generation);
    }

    /// <summary>
    /// Retires a row. Every handle to it becomes detectably stale, including any copy already stored
    /// in another table's column.
    /// </summary>
    public void Free(Handle<T> handle) => FreeSlot(Resolve(handle));

    /// <summary>True when the handle still addresses the row it was issued for.</summary>
    public bool IsValid(Handle<T> handle) =>
        handle.Generation != 0
        && handle.Index < (uint)SlotCount
        && GenerationAt((int)handle.Index) == handle.Generation;

    /// <summary>
    /// The slot a handle addresses, or a throw.
    /// </summary>
    /// <remarks>
    /// <b>There is no unchecked fast path, and the omission is deliberate.</b> The check is a load
    /// and a compare. What it prevents is a dangling read, and adr/0004 is explicit that in a
    /// deterministic simulation a dangling read is a <em>divergence</em>, not a crash — two runs
    /// disagree, the tools report success, and nothing points at the cause. Paying two instructions
    /// to keep the failure loud is not a trade worth revisiting without a profile that names it.
    /// </remarks>
    /// <exception cref="StaleHandleException">The row has been freed, or was never allocated.</exception>
    public int Resolve(Handle<T> handle)
    {
        if (!IsValid(handle))
        {
            throw new StaleHandleException(Name, handle.Index, handle.Generation);
        }

        return (int)handle.Index;
    }

    /// <inheritdoc cref="Resolve"/>
    /// <summary>The slot a handle addresses, without throwing.</summary>
    public bool TryResolve(Handle<T> handle, out int slot)
    {
        slot = (int)handle.Index;
        return IsValid(handle);
    }

    /// <summary>The handle currently addressing a live slot.</summary>
    /// <remarks>
    /// The route from an index back to a handle, needed by anything that iterates a table and then
    /// has to store what it found. Free slots return the unset handle, <c>default</c>.
    /// </remarks>
    public Handle<T> At(int slot) =>
        IsLive(slot) ? new Handle<T>((uint)slot, GenerationAt(slot)) : default;

    /// <summary>The target's monotonic id, for the hash and for anything needing a stable key.</summary>
    public bool TryIdOf(Handle<T> handle, out ulong id)
    {
        if (!IsValid(handle))
        {
            id = 0;
            return false;
        }

        id = IdAt((int)handle.Index);
        return true;
    }
}
