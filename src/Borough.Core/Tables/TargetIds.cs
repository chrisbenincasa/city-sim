namespace Borough.Core.Tables;

using System.Runtime.InteropServices;

/// <summary>
/// Where a <see cref="HandleColumn{TTarget}"/> reads the monotonic id it folds: the live target table,
/// or that same table's saved bytes inside a save.
/// </summary>
/// <remarks>
/// <para>
/// <b>It exists so that there is one fold rather than two</b> (<c>adr/0112</c>). The State Hash folds a
/// handle as the target row's never-reused id, which is the one value in the whole declaration that is
/// not in the bytes being folded. That one indirection is the entire difference between hashing the
/// live world and hashing a copy of it — so it is the only thing abstracted, and
/// <see cref="Column.Fold"/> is otherwise the same code down both paths.
/// </para>
/// <para>
/// <b>⚠ The saved side reads through <see cref="MemoryMarshal.Read{T}"/> rather than through a cast, and
/// both halves of that matter.</b> It is an <em>unaligned</em> read, because a table's block begins
/// wherever the previous table's columns ended and a one-byte column with an odd slot count leaves the
/// next one at any offset. And it is a <em>native-order</em> read, because the live path reads
/// <c>_id[slot]</c> in the machine's order: a little-endian reading here would give a different hash on
/// a big-endian host from the one that host computes live, which would turn a port into a divergence in
/// the one instrument used to detect divergences.
/// </para>
/// <para>
/// <b>The default value holds no target and must never be asked for one.</b> Every column that holds no
/// handle gets <c>default</c> and ignores it; <see cref="TryIdOf"/> answering <c>false</c> there would
/// be a plausible wrong answer, so it throws instead.
/// </para>
/// </remarks>
internal readonly ref struct TargetIds
{
    private readonly Rows? _live;
    private readonly ReadOnlySpan<byte> _generations;
    private readonly ReadOnlySpan<byte> _ids;
    private readonly int _slotCount;
    private readonly bool _saved;

    private TargetIds(
        Rows? live, ReadOnlySpan<byte> generations, ReadOnlySpan<byte> ids, int slotCount, bool saved)
    {
        _live = live;
        _generations = generations;
        _ids = ids;
        _slotCount = slotCount;
        _saved = saved;
    }

    /// <summary>Resolving against the live target table, which is what the State Hash does.</summary>
    internal static TargetIds Live(Rows target) => new(target, default, default, 0, false);

    /// <summary>
    /// Resolving against the target table's bytes in a save, which is what lets the hash be computed
    /// from a copy.
    /// </summary>
    /// <param name="generations">The target's <c>generation</c> column over <c>[0, slotCount)</c>.</param>
    /// <param name="ids">The target's <c>id</c> column over <c>[0, slotCount)</c>.</param>
    /// <param name="slotCount">The target's slot count, as the save records it.</param>
    internal static TargetIds Saved(
        ReadOnlySpan<byte> generations, ReadOnlySpan<byte> ids, int slotCount) =>
        new(null, generations, ids, slotCount, true);

    /// <summary>
    /// The target row's monotonic id, or false if the handle addresses no live row.
    /// </summary>
    /// <remarks>
    /// <b>The validity rule is <c>Rows.IsValidSlot</c>'s, stated once</b> — a generation of zero is the
    /// unset handle, an index past the slot count is out of range, and a generation that does not match
    /// the slot's is a handle whose row has been freed and reallocated. The saved branch repeats the
    /// <em>shape</em> of that check because it reads from bytes rather than from arrays; anything it
    /// could get wrong is caught by <c>SaveHashTests</c>, which asserts the two paths agree.
    /// </remarks>
    internal bool TryIdOf(uint index, uint generation, out ulong id)
    {
        if (_live is not null)
        {
            return _live.TryIdAt(index, generation, out id);
        }

        if (!_saved)
        {
            throw new InvalidOperationException(
                "a handle column was folded with no target id source. Column.Fold takes TargetIds and "
                + "only a column with no handles may be given default.");
        }

        if (generation == 0
            || index >= (uint)_slotCount
            || MemoryMarshal.Read<uint>(_generations[((int)index * sizeof(uint))..]) != generation)
        {
            id = 0;
            return false;
        }

        id = MemoryMarshal.Read<ulong>(_ids[((int)index * sizeof(ulong))..]);
        return true;
    }
}
