namespace Borough.Core.Tables;

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Borough.Core.Determinism;

/// <summary>
/// One field of one table, as a contiguous array — the non-generic half, so that
/// <see cref="Rows{T}"/> can hold its columns in declaration order without knowing their types.
/// </summary>
/// <remarks>
/// <b>A column can only come into existence through <see cref="Rows{T}"/>'s declaration methods</b>,
/// because those are what allocate it. That is the whole mechanism behind adr/0003's field rule: a
/// column that was never declared has no storage, so it cannot be forgotten by the hash — it cannot
/// exist. The only remaining way to add undeclared state to a table is to put a bare array beside the
/// columns, and <c>BOR0901</c> is an error for exactly that.
/// </remarks>
public abstract class Column
{
    private protected Column(Rows owner, string name, Disposition disposition, Touch touch)
    {
        Owner = owner;
        Name = name;
        Disposition = disposition;
        Touch = touch;
    }

    /// <summary>
    /// The column's name in the declaration.
    /// </summary>
    /// <remarks>
    /// <b>Not a Readout, and adr/0002 is not in play.</b> That rule is about strings a panel shows a
    /// player, resolved through the Ruleset. This one names a field in a save header and in a
    /// footprint report, and is addressed to whoever is auditing the declaration — the same category
    /// as <c>ColdPathAttribute.Reason</c>, and exempt for the same reason.
    /// </remarks>
    public string Name { get; }

    /// <summary>Saved and hashed, or derived and rebuilt. Never neither, never both.</summary>
    public Disposition Disposition { get; }

    /// <summary>How often the column is read. Declared, never branched on.</summary>
    public Touch Touch { get; }

    /// <summary>Width of one element, in bytes. Structure-of-arrays has no per-row padding.</summary>
    public abstract int BytesPerRow { get; }

    private protected Rows Owner { get; }

    internal abstract void Grow(int capacity);

    internal abstract void Clear(int slot);

    internal abstract void Fold(ref ulong hash, int slotCount);

    /// <summary>Copies the live half over the write half, before a partial parallel write.</summary>
    /// <remarks>
    /// <b>Seeding rather than clearing, because a parallel phase is entitled to write only part of
    /// the table.</b> Map Layer diffusion is incremental by design — it recomputes a halo and leaves
    /// the rest alone (<c>02 §2.4</c>) — so an unseeded write half would swap in values from two
    /// cycles ago everywhere the halo did not reach. That defect is invisible in one Tick and arrives
    /// as a field flickering between two states, which reads as an art problem.
    /// </remarks>
    internal abstract void PrepareBack();

    /// <summary>Makes the write half live. See <see cref="Rows.SwapBuffers"/>.</summary>
    internal abstract void SwapBuffers();

    /// <summary>
    /// Whether this column holds a handle at <paramref name="slot"/> whose target row is gone.
    /// </summary>
    /// <remarks>
    /// <b>Asked of every column so that the invariant walk needs no schema.</b> Referential integrity
    /// is ours to maintain (<c>adr/0004</c>) and the check that maintains it must find every handle
    /// column, including ones added after it was written. A walk driven by a list of columns somebody
    /// remembered has the same blind spot as the bug it looks for. Columns that hold no handle answer
    /// false and cost a virtual call once per column per walk, which happens once a run.
    /// </remarks>
    internal virtual bool IsDangling(int slot) => false;
}

/// <summary>
/// One field of one table: a contiguous <typeparamref name="T"/> array, one element per slot.
/// </summary>
/// <typeparam name="T">
/// The field's type. <b>It must have no padding bytes.</b> A struct with padding — <c>{ byte, int }</c>
/// — leaves three bytes whose contents the runtime does not define, and the fold below reads every
/// byte. Every field width the design states is a single primitive or a wrapper over one, so this
/// holds today; it is recorded in plans/0002 as owed rather than enforced.
/// </typeparam>
/// <remarks>
/// <para>
/// <b>Structure-of-arrays, which is most of the reason for the layout.</b> A hot loop touching three
/// fields touches three streams and not a stride, and <see cref="Span"/> stays useful over each one.
/// The named hot loops are Lane queues, Event Wheel buckets, layer diffusion and choice scoring.
/// </para>
/// <para>
/// <b>Iteration is in index order, always.</b> That is not a convention here — it is what makes
/// adr/0003's ban on walking a hash map practical rather than merely aspirational. Index order is
/// identical across runs and across machines, and it is also the fastest option.
/// </para>
/// </remarks>
public class Column<T> : Column
    where T : unmanaged
{
    private T[] _values;

    /// <summary>
    /// The write half, allocated only for a <see cref="Buffering.TwoCopies"/> table.
    /// </summary>
    /// <remarks>
    /// <b>Null is the declaration, not a lazy allocation.</b> <c>adr/0037</c>'s rule is per table — a
    /// table is double-buffered <em>if and only if</em> a parallel phase both reads and writes it — so
    /// whether this array exists is settled at construction by the table's stated
    /// <see cref="Rows.Buffering"/> and never by a call site deciding it needs one. A column that
    /// allocated its second half on first use would let a phase quietly acquire a hazard the table
    /// never declared.
    /// </remarks>
    private T[]? _back;

    internal Column(Rows owner, string name, Disposition disposition, Touch touch, int capacity)
        : base(owner, name, disposition, touch)
    {
        _values = new T[capacity];

        if (owner.Buffering == Buffering.TwoCopies)
        {
            _back = new T[capacity];
        }
    }

    /// <inheritdoc/>
    public sealed override int BytesPerRow => Unsafe.SizeOf<T>();

    /// <summary>The live prefix of the column — slot 0 up to the table's high-water slot.</summary>
    /// <remarks>
    /// Freed slots are inside this range and hold zeroes; see <see cref="Rows{T}.Free"/> for why
    /// they are zeroed rather than left as the previous occupant's residue.
    /// </remarks>
    public Span<T> Span => _values.AsSpan(0, Owner.SlotCount);

    /// <summary>The value at one slot. The slot is a resolved index, never a raw handle.</summary>
    public ref T this[int slot] => ref _values[slot];

    /// <summary>
    /// The write half of a double-buffered column, over the same live prefix as <see cref="Span"/>.
    /// </summary>
    /// <remarks>
    /// <b>A parallel phase writes here and reads <see cref="Span"/>, then the table swaps.</b> That is
    /// what removes the order dependence <c>02 §2.4</c> names: an in-place field lets a Cell's new
    /// value depend on whether its neighbour has been visited yet, which is simultaneously a
    /// determinism hazard and a directional smear in the picture.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The table is <see cref="Buffering.OneCopy"/>.</exception>
    public Span<T> BackSpan => Back().AsSpan(0, Owner.SlotCount);

    /// <inheritdoc cref="BackSpan"/>
    /// <summary>The write half's value at one slot.</summary>
    public ref T AtBack(int slot) => ref Back()[slot];

    private protected Span<T> Raw => _values;

    internal sealed override void Grow(int capacity)
    {
        Array.Resize(ref _values, capacity);

        if (_back is not null)
        {
            Array.Resize(ref _back, capacity);
        }
    }

    internal sealed override void Clear(int slot)
    {
        _values[slot] = default;

        // Both halves, so that a recycled slot cannot swap its previous occupant back in. The free
        // list hands slots out again and the swap is blind to liveness; a write half left holding the
        // old row would resurrect it on the next cycle, in a column the hash folds.
        if (_back is not null)
        {
            _back[slot] = default;
        }
    }

    internal sealed override void PrepareBack()
    {
        if (_back is not null)
        {
            _values.AsSpan(0, Owner.SlotCount).CopyTo(_back);
        }
    }

    internal sealed override void SwapBuffers()
    {
        if (_back is not null)
        {
            (_values, _back) = (_back, _values);
        }
    }

    private T[] Back() =>
        _back ?? throw new InvalidOperationException(
            $"column '{Name}' has one copy; table '{Owner.Name}' did not declare Buffering.TwoCopies. "
            + "adr/0037: a table is double-buffered if and only if a parallel phase reads and writes it.");

    internal override void Fold(ref ulong hash, int slotCount) =>
        FoldBytes(ref hash, MemoryMarshal.AsBytes(_values.AsSpan(0, slotCount)));

    /// <summary>
    /// Folds a column's bytes through slice 2's <c>mix</c>, eight at a time, little-endian.
    /// </summary>
    /// <remarks>
    /// <b>The endianness is stated rather than inherited.</b> Every machine this has run on is
    /// little-endian and reinterpreting the array directly would be faster, but a State Hash whose
    /// value depends on the host's byte order is a hash that reports a divergence on a port. adr/0003
    /// puts the draw function in the same category as a save format; the same argument applies to the
    /// hash the draws are checked with.
    /// </remarks>
    private protected static void FoldBytes(ref ulong hash, ReadOnlySpan<byte> bytes)
    {
        ulong h = hash;
        int i = 0;

        for (; i + sizeof(ulong) <= bytes.Length; i += sizeof(ulong))
        {
            h = Randomness.Mix(h + BinaryPrimitives.ReadUInt64LittleEndian(bytes[i..]));
        }

        if (i < bytes.Length)
        {
            ulong tail = 0;
            for (int b = bytes.Length - 1; b >= i; b--)
            {
                tail = (tail << 8) | bytes[b];
            }

            h = Randomness.Mix(h + tail);
        }

        hash = h;
    }
}

/// <summary>
/// A column of handles into another table, which is the one case where the hash cannot fold the
/// stored bytes.
/// </summary>
/// <remarks>
/// <para>
/// <b>Fold values, never identity.</b> A handle's index is identity — a slot address, assigned by a
/// free list whose state is a function of the entire demolition history of the city. The thing the
/// field <em>means</em> is which entity it points at, and the stable name for that is the target row's
/// monotonic never-reused id. So this column resolves each handle and folds that.
/// </para>
/// <para>
/// <b>The cost is one random access per handle per hash, and it is worth it.</b> It makes the hash
/// invariant to table layout: two runs that build the same city with different allocation histories
/// hash the same, and a save that ever compacts rows cannot move the hash by doing so. Folding the raw
/// <c>{index, generation}</c> would be cheaper and would be correct for both uses the hash has today —
/// in replay and in save/reload the allocation history is identical, so indices agree — but it buys
/// that correctness from a coincidence rather than from the definition.
/// </para>
/// <para>
/// <b>A dangling handle folds as a sentinel rather than throwing.</b> Referential integrity is ours to
/// maintain (adr/0004) and is checked by invariants that walk every cross-table handle; making the
/// hash itself fatal on one would turn a diagnostic into a crash, and the hash is the tool you reach
/// for <em>while</em> diagnosing. It still moves the hash, so the divergence is visible.
/// </para>
/// </remarks>
public sealed class HandleColumn<TTarget> : Column<Handle<TTarget>>
    where TTarget : unmanaged
{
    /// <summary>Folded in place of a handle whose target row has been freed.</summary>
    private const ulong Dangling = ulong.MaxValue;

    private readonly Rows<TTarget> _target;

    internal HandleColumn(
        Rows owner, string name, Disposition disposition, Touch touch, int capacity,
        Rows<TTarget> target)
        : base(owner, name, disposition, touch, capacity) =>
        _target = target;

    /// <summary>The table this column's handles address.</summary>
    public Rows<TTarget> Target => _target;

    /// <inheritdoc />
    /// <remarks>
    /// The unset handle is not dangling. A column is allowed to point at nothing — a Citizen with no
    /// Workplace is unemployed rather than corrupt — and a walk that could not tell those apart would
    /// report every empty field in the city.
    /// </remarks>
    internal override bool IsDangling(int slot)
    {
        Handle<TTarget> handle = this[slot];

        return !handle.IsNone && !_target.IsValid(handle);
    }

    internal override void Fold(ref ulong hash, int slotCount)
    {
        ulong h = hash;
        Span<Handle<TTarget>> handles = Raw[..slotCount];

        for (int i = 0; i < handles.Length; i++)
        {
            Handle<TTarget> handle = handles[i];

            ulong value = handle.IsNone
                ? 0
                : _target.TryIdOf(handle, out ulong id) ? id : Dangling;

            h = Randomness.Mix(h + value);
        }

        hash = h;
    }
}
