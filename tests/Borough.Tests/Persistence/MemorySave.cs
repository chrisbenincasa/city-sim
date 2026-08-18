using System.Runtime.InteropServices;
using Borough.Core.Persistence;

namespace Borough.Tests.Persistence;

/// <summary>
/// A save in a <see cref="MemoryStream"/>-shaped buffer, plus what the writer did to get it there.
/// </summary>
/// <remarks>
/// <b>It records the shape of the traffic and not only the bytes</b>, because the property task 5 is
/// asserting is about the traffic: no single hand-over is proportional to the file. A sink that
/// concatenates is still a correct sink, so a round-trip test alone cannot tell a streaming writer from
/// a buffering one — <see cref="LargestWrite"/> is what can.
/// </remarks>
public sealed class MemorySave : ISaveSink, ISaveSource
{
    private readonly List<byte> _bytes = [];
    private int _read;

    /// <summary>How many times the writer handed bytes over.</summary>
    public int Writes { get; private set; }

    /// <summary>The largest single hand-over. The number the streaming claim rests on.</summary>
    public int LargestWrite { get; private set; }

    /// <summary>The whole file.</summary>
    public byte[] Bytes => [.. _bytes];

    /// <summary>Bytes not yet read. Zero after a complete load.</summary>
    public int Unread => _bytes.Count - _read;

    /// <inheritdoc/>
    public void Write(ReadOnlySpan<byte> bytes)
    {
        Writes++;

        if (bytes.Length > LargestWrite)
        {
            LargestWrite = bytes.Length;
        }

        _bytes.AddRange(bytes);
    }

    /// <inheritdoc/>
    public void Read(Span<byte> into)
    {
        if (_read + into.Length > _bytes.Count)
        {
            throw new InvalidOperationException(
                $"the save is {_bytes.Count} bytes and a reader asked for {into.Length} more at "
                + $"offset {_read}. A truncated save.");
        }

        CollectionsMarshal.AsSpan(_bytes).Slice(_read, into.Length).CopyTo(into);
        _read += into.Length;
    }

    /// <summary>Rewinds the read cursor, so one buffer can be loaded twice.</summary>
    public void Rewind() => _read = 0;

    /// <summary>Truncates the file to <paramref name="bytes"/>, for the reader's refusal tests.</summary>
    public void TruncateTo(int bytes) => _bytes.RemoveRange(bytes, _bytes.Count - bytes);
}
