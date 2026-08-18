namespace Borough.Core.Persistence;

/// <summary>
/// The copy <c>adr/0087</c> requires: the whole save, in memory, taken at a phase boundary so that
/// whatever writes it never reads a live table.
/// </summary>
/// <remarks>
/// <para>
/// <b>It is an <see cref="ISaveSink"/>, which is what makes the copy and the write one mechanism rather
/// than two.</b> <c>adr/0087</c> asks for <em>a copy of the saved columns at the end of Tick phase 7</em>
/// and then a write off the simulation thread. Writing the world into a memory sink <b>is</b> that copy —
/// so there is one writer (<see cref="SaveFile.WriteBody"/>), used twice against different sinks, rather
/// than a serialiser and a separate cloner that could disagree about what the file contains.
/// </para>
/// <para>
/// ⚠ <b>What it holds is the body and not the file</b>, which changed with task 10. A header carries a
/// number folded from the body, so it cannot be written ahead of one — and a header is a statement about
/// the <em>build</em> where this is a copy of the <em>world</em>. The two were only ever adjacent.
/// </para>
/// <para>
/// <b>The buffer is reused across saves and grows only when a city does.</b> A save is
/// <b>131.33 MiB</b> at 1,000,000 Citizens, so allocating one per autosave would put a fresh body of
/// that size on the heap every in-world Day. It is allocated on the first save rather than with the
/// <c>Simulation</c>, because a session that never saves must not pay for one.
/// </para>
/// <para>
/// ⚠ <b>This is the one body of that size the design accepts, and task 5's streaming is what keeps it
/// to one.</b> The writer hands over a column at a time and this concatenates, so the peak is the
/// snapshot and not the snapshot plus a staging buffer. <see cref="DrainTo"/> then hands the whole
/// thing over in a single call, which is correct precisely because it is already assembled — there is
/// nothing left to stream.
/// </para>
/// <para>
/// ⚠ <b>It carries the State Hash after all, and the claim that it could not was too wide</b>
/// (<c>adr/0112</c>, task 10). Task 6 recorded that a copy could not produce one, because
/// <c>HandleColumn.Fold</c> folds the target row's monotonic id and a handle's bytes do not contain it.
/// The id is in <em>another table's block of this same buffer</em>: <c>Rows</c> declares <c>id</c> and
/// <c>generation</c> as saved columns, so both arrays are here. ***A value absent from a column's own
/// bytes can still be present in the copy.*** <see cref="SaveHash.Of"/> folds this buffer into the
/// number <c>World.HashState</c> returned at the instant it was taken, so a save verifies itself on
/// load and the simulation thread pays nothing for it.
/// </para>
/// </remarks>
public sealed class WorldSnapshot : ISaveSink
{
    private byte[] _bytes = [];
    private int _length;

    /// <summary>How many bytes the last copy produced.</summary>
    public int Length => _length;

    /// <summary>How much the buffer currently holds. Never shrinks.</summary>
    public int Capacity => _bytes.Length;

    /// <summary>The copy, as a span. Valid until the next one.</summary>
    public ReadOnlySpan<byte> Bytes => _bytes.AsSpan(0, _length);

    /// <summary>Discards the previous copy, keeping the buffer.</summary>
    public void Reset() => _length = 0;

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Appends, because it is the writer's sink.</b> The writer hands over each table's scalars and
    /// each of its saved columns, so this is called once per column and the concatenation is the body.
    /// </remarks>
    public void Write(ReadOnlySpan<byte> bytes)
    {
        if (_length + bytes.Length > _bytes.Length)
        {
            Grow(_length + bytes.Length);
        }

        bytes.CopyTo(_bytes.AsSpan(_length));
        _length += bytes.Length;
    }

    /// <summary>
    /// Hands the copy to wherever it is going — the unbounded half, and the half a thread would take.
    /// </summary>
    /// <remarks>
    /// <b>One call rather than a stream, and the difference from <see cref="SaveFile.Write"/> is
    /// deliberate.</b> Streaming exists to avoid assembling a body of bytes; this one is already
    /// assembled, so handing it over in pieces would buy nothing and would only give the destination a
    /// reason to reassemble it.
    /// </remarks>
    public void DrainTo(ISaveSink destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        destination.Write(Bytes);
    }

    private void Grow(int needed)
    {
        int capacity = _bytes.Length == 0 ? 1 : _bytes.Length;

        while (capacity < needed)
        {
            capacity *= 2;
        }

        Array.Resize(ref _bytes, capacity);
    }
}
