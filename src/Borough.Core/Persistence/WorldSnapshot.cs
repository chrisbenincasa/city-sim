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
/// so there is one writer (<see cref="SaveFile.Write"/>), used twice against different sinks, rather
/// than a serialiser and a separate cloner that could disagree about what the file contains.
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
/// ⚠ <b>It cannot carry a State Hash, and that is a property of the hash rather than of this type.</b>
/// <c>adr/0087</c> says a verified hash would be <em>computed on the background thread from the copy</em>.
/// It cannot be: <c>HandleColumn.Fold</c> folds the <em>target row's monotonic id</em>, which lives in
/// another table and is not a function of the handle's bytes, so folding this buffer would produce a
/// number that is not the State Hash. ***A hash that folds a value the bytes do not contain cannot be
/// computed from the bytes.*** See <c>plans/0030</c>, task 6.
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
    /// <b>Appends, because it is the writer's sink.</b> The writer hands over the header, then each
    /// table's scalars and each of its saved columns, so this is called once per column and the
    /// concatenation is the file.
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
