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

    internal Column(Rows owner, string name, Disposition disposition, Touch touch, int capacity)
        : base(owner, name, disposition, touch) =>
        _values = new T[capacity];

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

    private protected Span<T> Raw => _values;

    internal sealed override void Grow(int capacity) => Array.Resize(ref _values, capacity);

    internal sealed override void Clear(int slot) => _values[slot] = default;

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
