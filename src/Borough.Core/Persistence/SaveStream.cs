namespace Borough.Core.Persistence;

/// <summary>
/// Where a save's bytes go. Implemented by whoever owns the file; <c>Core</c> never opens one.
/// </summary>
/// <remarks>
/// <para>
/// <b>An interface rather than a <c>Stream</c>, because <c>Borough.Core</c> holds no
/// <c>System.IO</c></b>, and rather than a whole buffer, because a whole buffer is the thing this
/// shape exists to avoid. `adr/0039` puts the Input Log's file handling in the shells for the
/// neighbouring reason: <c>Core</c> decides what the bytes are and never where they land.
/// </para>
/// <para>
/// <b>⚠ The writer hands over one span per column and never assembles the file.</b> A save at
/// 1,000,000 Citizens is <b>131.33 MiB</b>, and <c>adr/0087</c> already spends a copy of the world at
/// that scale — so materialising the file as well would put a second body of the same order beside the
/// first, at the one moment memory is already at its highest. Because a column's slots are contiguous
/// (<c>Column.StorageBytes</c>), nothing has to be assembled for that to be avoided: the largest single
/// write is the largest single column, not the file. ***A format with no authored layout can be
/// streamed, because there is nothing to lay out.***
/// </para>
/// <para>
/// <b>The implementation must consume the span before returning.</b> It is a window onto live column
/// storage rather than a copy the sink may keep, which is what makes the whole arrangement free —
/// and on the background write <c>adr/0087</c> foresees, that storage is the <em>copy</em>'s and not
/// the running world's.
/// </para>
/// </remarks>
public interface ISaveSink
{
    /// <summary>Takes the next run of bytes. Must consume them before returning.</summary>
    void Write(ReadOnlySpan<byte> bytes);
}

/// <summary>
/// Where a load's bytes come from. The mirror of <see cref="ISaveSink"/>.
/// </summary>
/// <remarks>
/// <b>It fills a span rather than returning one, and that is the whole reason a load allocates
/// nothing.</b> The span it is handed is the destination column's own storage, so the file's bytes land
/// where they are going to live with no staging buffer in between. A source that cannot fill the span
/// completely must throw rather than return short: a partial read is a truncated save, and returning a
/// count would put the refusal in every caller instead of in one place.
/// </remarks>
public interface ISaveSource
{
    /// <summary>Fills <paramref name="into"/> completely, or throws.</summary>
    void Read(Span<byte> into);
}
