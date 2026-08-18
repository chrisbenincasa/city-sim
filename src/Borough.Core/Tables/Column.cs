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

    /// <summary>
    /// Folds slots <c>[0, slotCount)</c> into <paramref name="hash"/>, from
    /// <paramref name="storage"/> rather than from this column's own array.
    /// </summary>
    /// <param name="hash">The running hash.</param>
    /// <param name="storage">
    /// The bytes to fold — this column's live storage, or the same column's bytes inside a save.
    /// Its length is the slot count times <see cref="BytesPerRow"/>.
    /// </param>
    /// <param name="targets">
    /// Where <see cref="HandleColumn{TTarget}"/> resolves a handle to the monotonic id it folds.
    /// <c>default</c> for every column that holds no handle, which ignores it.
    /// </param>
    /// <remarks>
    /// <para>
    /// <b>⚠ It takes its bytes rather than reading its own array, and that is the whole of milestone 8
    /// task 10.</b> The State Hash and the save are the same values in the same order — <c>Rows.Fold</c>
    /// and <c>SaveFile.WriteBody</c> both walk <see cref="Rows.SavedColumns"/> over the same slots — so
    /// a fold that takes a span can be run against a <c>WorldSnapshot</c> as easily as against the live
    /// world, and a save can carry a verified hash that <b>costs the simulation thread nothing</b>
    /// (<c>adr/0112</c>).
    /// </para>
    /// <para>
    /// <b>One implementation against two sources, deliberately, rather than a second fold beside this
    /// one.</b> A snapshot-folding routine written separately would be two copies of one rule that must
    /// agree for ever, which is <c>plans/0012</c> <em>Cause 1</em> built on purpose. This signature is
    /// what makes the live path and the save path the same code.
    /// </para>
    /// </remarks>
    internal abstract void Fold(ref ulong hash, ReadOnlySpan<byte> storage, in TargetIds targets);

    /// <summary>Folds this column's own storage, resolving handles against the live target table.</summary>
    /// <remarks>
    /// <b>The live world's half of the pair, and not a second implementation of anything.</b> It fills
    /// in the two arguments a live fold always has the same answers for — this column's storage, and
    /// its target table — and hands them to the one <see cref="Fold(ref ulong, ReadOnlySpan{byte}, in TargetIds)"/>
    /// that <c>SaveHash</c> also calls.
    /// </remarks>
    internal void Fold(ref ulong hash, int slotCount) =>
        Fold(ref hash, StorageBytes(slotCount), LiveTargets);

    /// <summary>Where this column's handles resolve when the world holding it is the live one.</summary>
    private TargetIds LiveTargets =>
        HandleTarget is { } target ? TargetIds.Live(target) : default;

    /// <summary>
    /// The table this column's handles address, or <c>null</c> if it holds no handles.
    /// </summary>
    /// <remarks>
    /// <b>Non-generic on purpose.</b> A save folder has to find a handle column's target table without
    /// knowing its element type, in order to locate that table's <c>id</c> and <c>generation</c> bytes
    /// in the file. <see cref="HandleColumn{TTarget}.Target"/> is the typed answer to the same question
    /// and is the one to use where the type is in hand.
    /// </remarks>
    internal virtual Rows? HandleTarget => null;

    /// <summary>
    /// Slots <c>[0, slotCount)</c> as raw bytes, readable and writable. The save's half of
    /// <see cref="Fold"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A sibling to <see cref="Fold"/> rather than a reuse of it, because the two diverge in
    /// exactly one place and by design</b> (adr/0086). <see cref="Fold"/> is overridden by
    /// <see cref="HandleColumn{TTarget}"/> to fold the target's monotonic id, so the hash is blind to
    /// slot recycling; the file must store the handle itself, because a load has to restore the same
    /// slots. ***A save round-trip must preserve the hash and need not preserve the bytes.***
    /// </para>
    /// <para>
    /// <b>⚠ The bytes are the host's, and so is the hash's reading of them.</b> <see cref="FoldBytes"/>
    /// assembles <c>ulong</c>s little-endian, which fixes the *combination* step and not the *layout*:
    /// the bytes it combines come from <see cref="MemoryMarshal.AsBytes"/> over a struct whose field
    /// order in memory is the machine's. So a big-endian host would produce a different State Hash for
    /// the same city today, before any save existed — see this file's note on
    /// <see cref="FoldBytes"/>. The save inherits that exposure exactly and adds none of its own, which
    /// is the reason to copy rather than to invent a second byte order here: one representation, one
    /// place to fix if a port ever happens.
    /// </para>
    /// <para>
    /// <b>⚠ It hands out the storage itself rather than copying into or out of a buffer</b> (task 5).
    /// Task 2 built a <c>WriteBytes</c>/<c>ReadBytes</c> pair and both are gone: a column's slots are
    /// already contiguous, so a save writes them by handing this span to a sink and a load fills them
    /// by handing it to a source. ***The file needs no intermediate copy at either end, and a buffer
    /// the size of the save would have doubled the peak the copy in adr/0087 already costs.***
    /// </para>
    /// <para>
    /// <b>The write half is a real span into live storage, so the caller may fill it.</b> A load that
    /// fails partway leaves a column half-filled, which is safe only because a failed load discards
    /// the world it was building — stated here because nothing structural enforces it.
    /// </para>
    /// </remarks>
    internal abstract Span<byte> StorageBytes(int slotCount);

    /// <summary>Copies slots <c>[0, slotCount)</c> of the live half over the write half, if there is one.</summary>
    /// <remarks>
    /// <b>Called after a load fills <see cref="StorageBytes"/>, and it cannot be skipped.</b>
    /// <see cref="Clear"/> says why: a <c>_back</c> left holding an old row would resurrect it on the
    /// next swap, and a load is the one moment the front half changes without a swap having produced
    /// it. It is a separate call rather than part of the fill because the fill is the caller's write
    /// and this has to happen after it.
    /// </remarks>
    internal abstract void MirrorToBack(int slotCount);

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

    /// <inheritdoc/>
    /// <remarks>
    /// <b>The bytes are the whole of it, which is why a save needs no second fold for this case.</b>
    /// A column's file bytes <em>are</em> its storage bytes (<see cref="StorageBytes"/> hands out the
    /// same span), so folding one is folding the other. <see cref="HandleColumn{TTarget}"/> is the
    /// single exception in the whole table layer.
    /// </remarks>
    internal override void Fold(ref ulong hash, ReadOnlySpan<byte> storage, in TargetIds targets) =>
        FoldBytes(ref hash, storage);

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Not overridden by <see cref="HandleColumn{TTarget}"/>, which is the interesting half.</b>
    /// That type overrides <see cref="Fold"/> and inherits this, so a handle reaches the file as its
    /// stored <c>{index, generation}</c> — which <see cref="HandleColumn{TTarget}"/>'s own remarks
    /// already note *"would be correct for both uses the hash has today"* and is required rather than
    /// merely adequate here, since a load must restore the slots the handles address.
    /// </remarks>
    internal sealed override Span<byte> StorageBytes(int slotCount) =>
        MemoryMarshal.AsBytes(_values.AsSpan(0, slotCount));

    /// <inheritdoc/>
    internal sealed override void MirrorToBack(int slotCount)
    {
        if (_back is null)
        {
            return;
        }

        MemoryMarshal.AsBytes(_values.AsSpan(0, slotCount))
            .CopyTo(MemoryMarshal.AsBytes(_back.AsSpan(0, slotCount)));
    }

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
    private readonly Reference _reference;

    internal HandleColumn(
        Rows owner, string name, Disposition disposition, Touch touch, int capacity,
        Rows<TTarget> target, Reference reference)
        : base(owner, name, disposition, touch, capacity)
    {
        _target = target;
        _reference = reference;
    }

    /// <summary>The table this column's handles address.</summary>
    public Rows<TTarget> Target => _target;

    /// <summary>Whether the target must outlive the handle, or may be freed underneath it.</summary>
    public Reference Reference => _reference;

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// The unset handle is not dangling. A column is allowed to point at nothing — a Citizen with no
    /// Workplace is unemployed rather than corrupt — and a walk that could not tell those apart would
    /// report every empty field in the city.
    /// </para>
    /// <para>
    /// <b>Nor is a stale handle in a <see cref="Reference.Severable"/> column.</b> There the target
    /// being gone is the state being modelled rather than a break in it, and the two are
    /// indistinguishable to this method — which is why the difference is declared once, beside the
    /// field, instead of being decided here.
    /// </para>
    /// </remarks>
    internal override bool IsDangling(int slot)
    {
        if (_reference == Reference.Severable)
        {
            return false;
        }

        Handle<TTarget> handle = this[slot];

        return !handle.IsNone && !_target.IsValid(handle);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <b>⚠ Everything this needs is in the save, which is the finding milestone 8 task 10 rests on.</b>
    /// The id it folds lives in the target table rather than in the handle — so a fold over
    /// <em>these</em> bytes alone is not the State Hash, and <c>plans/0030</c> task 6 concluded from
    /// that the hash could not come from a copy at all. It can: <c>Rows</c> declares <c>id</c> and
    /// <c>generation</c> as <see cref="Disposition.Saved"/> columns, so both arrays are in the file
    /// too. ***The value is not in these bytes and it is in the copy.*** <see cref="TargetIds"/> is the
    /// one line of difference between reading it from the live table and reading it from the save.
    /// </remarks>
    internal override void Fold(ref ulong hash, ReadOnlySpan<byte> storage, in TargetIds targets)
    {
        ulong h = hash;
        ReadOnlySpan<Handle<TTarget>> handles = MemoryMarshal.Cast<byte, Handle<TTarget>>(storage);

        for (int i = 0; i < handles.Length; i++)
        {
            Handle<TTarget> handle = handles[i];

            ulong value = handle.IsNone
                ? 0
                : targets.TryIdOf(handle.Index, handle.Generation, out ulong id) ? id : Dangling;

            h = Randomness.Mix(h + value);
        }

        hash = h;
    }

    /// <inheritdoc/>
    internal override Rows HandleTarget => _target;
}
