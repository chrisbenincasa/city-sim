using Borough.Core.Persistence;

namespace Borough.Headless;

/// <summary>
/// A save going out to a file, one column at a time.
/// </summary>
/// <remarks>
/// <para>
/// <b>The adapter lives in the shell because <c>Borough.Core</c> has no <c>System.IO</c> and this
/// milestone did not give it any</b> (<c>plans/0030</c>, *What this milestone must not do*). D7 made
/// the boundary two interfaces precisely so that the side which knows about files is the side that
/// already owns them — so a save is written by handing <see cref="SaveFile.Write"/> somewhere to put
/// bytes, and the somewhere is here.
/// </para>
/// <para>
/// ⚠ <b>It is not in <c>Borough.Formats</c>, and the reason is that a save is not a format.</b> That
/// project holds the artefacts that spell things in words — the Input Log codec and the crash
/// artifact — and both shells must agree on those (<c>adr/0039</c>). A save has no schema of its own
/// (<c>adr/0086</c>): it is the field declaration, dumped. What is here is a <c>Stream</c> wearing an
/// interface, which is the shell's business and not a thing two shells could disagree about. The day
/// the Godot shell saves, it writes its own eight lines or this moves; **it does not become a
/// format because a second caller appeared.**
/// </para>
/// </remarks>
internal sealed class SaveSink(Stream stream) : ISaveSink
{
    private readonly Stream _stream = stream;

    public void Write(ReadOnlySpan<byte> bytes) => _stream.Write(bytes);
}

/// <summary>
/// A save coming back from a file, one column at a time.
/// </summary>
/// <remarks>
/// <b>A short read is a truncated save and is thrown as one</b>, which is where task 5 put the width
/// check when it deleted <c>Column.ReadBytes</c>: the refusal fires once, here, instead of in every
/// column. <c>Stream.ReadExactly</c> does the looping, because a <c>FileStream</c> is entitled to
/// return fewer bytes than asked for and a save read with a naive <c>Read</c> would corrupt itself
/// silently on exactly the largest files.
/// </remarks>
internal sealed class SaveSource(Stream stream) : ISaveSource
{
    private readonly Stream _stream = stream;

    public void Read(Span<byte> into)
    {
        try
        {
            _stream.ReadExactly(into);
        }
        catch (EndOfStreamException)
        {
            throw new InvalidOperationException(
                $"truncated save: {into.Length} more bytes were declared and the file has ended.");
        }
    }
}
