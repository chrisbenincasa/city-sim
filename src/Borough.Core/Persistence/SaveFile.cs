namespace Borough.Core.Persistence;

using System.Buffers.Binary;
using Borough.Core.Entities;
using Borough.Core.Rules;
using Borough.Core.Tables;

/// <summary>
/// A <c>World</c> out to bytes and back. The save, and the whole of it.
/// </summary>
/// <remarks>
/// <para>
/// <b>There is no layout in this file and that is the point</b> (<c>adr/0086</c>). The format is the
/// per-field declaration read out in the order <c>World.HashState</c> already folds it — for each table
/// in <c>World.Tables</c> order, the allocator's four scalars, then every <c>Disposition.Saved</c>
/// column over <c>[0, slotCount)</c>. Nothing here decides what goes in the file; it decides only that
/// the file is written in one pass and read in one pass.
/// </para>
/// <para>
/// <b>⚠ Neither direction allocates a buffer proportional to the save.</b> The writer hands each column's
/// own storage to the sink and the reader fills each column's own storage from the source, so the
/// largest run of bytes in play at any instant is the largest single column rather than the
/// <b>131.33 MiB</b> the file totals at 1,000,000 Citizens. That matters because <c>adr/0087</c> already
/// spends a copy of the world at save time: a staged file would have put a second body of the same order
/// beside it, at the one moment memory is highest. ***A format with no authored layout can be streamed,
/// because there is nothing to lay out.***
/// </para>
/// <para>
/// <b>The only fixed-size buffer is 60 bytes of header and 20 of table scalars</b>, both stack-allocated.
/// </para>
/// </remarks>
public static class SaveFile
{
    /// <summary>
    /// The four allocator scalars, per table: slot count, live count, free head, next id.
    /// </summary>
    /// <remarks>
    /// <b>Internal because it is the format, and the format has readers other than the writer.</b>
    /// <see cref="SaveHash"/> walks the same layout to find a column's bytes, and a test locating a byte
    /// inside a named column walks it too. It was restated in each; ***a layout constant copied per
    /// reader is one fact in three files***, which is <c>plans/0012</c> Cause 1 in code.
    /// </remarks>
    internal const int ScalarBytes = 4 + 4 + 4 + 8;

    /// <summary>Writes a world. The sink owns the file; this owns the order.</summary>
    /// <param name="world">The world to write. Read, never modified.</param>
    /// <param name="rulesetInForce">
    /// The content hash of the Ruleset in force — <c>Simulation.RulesetInForce</c>. It goes in the
    /// header, which is what dissolves the field (<c>adr/0111</c>).
    /// </param>
    /// <param name="sink">Where the bytes go.</param>
    /// <remarks>
    /// <para>
    /// <b>⚠ It does not take the copy <c>adr/0087</c> requires, and must not be handed a live world on a
    /// thread that is stepping it.</b> The copy is task 6's, and
    /// <see cref="Write(World, ulong, WorldSnapshot, ISaveSink)"/> is the overload a Tick uses —
    /// <c>Simulation.SaveAtEndOfTick</c> goes through that one, so every save a session actually takes
    /// is copied first.
    /// </para>
    /// <para>
    /// <b>⚠ This overload folds the <em>live</em> world for the header's hash, at 32.47 ms at 1M</b>
    /// (<c>adr/0112</c>), because it has no copy to fold instead. That is the cost the copy path exists
    /// to avoid, so this is for a caller that already has the world in hand and is not on a Tick's
    /// critical path — a test, or a one-off dump. ***Having both is deliberate: the two paths must agree,
    /// and <c>SaveHashTests</c> is what says they do.***
    /// </para>
    /// </remarks>
    public static void Write(World world, ulong rulesetInForce, ISaveSink sink)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(sink);

        Span<byte> header = stackalloc byte[SaveHeader.Bytes];
        SaveHeader.Of(world, rulesetInForce, world.HashState()).Write(header);
        sink.Write(header);

        WriteBody(world, sink);
    }

    /// <summary>
    /// Writes a world through a copy, taking the hash from the copy — the save path proper.
    /// </summary>
    /// <param name="world">The world to write. Read, never modified.</param>
    /// <param name="rulesetInForce">The content hash of the Ruleset in force.</param>
    /// <param name="copy">
    /// The buffer <c>adr/0087</c> requires. Reset and refilled; the caller keeps it across saves so an
    /// autosave costs no allocation.
    /// </param>
    /// <param name="sink">Where the bytes go.</param>
    /// <remarks>
    /// <para>
    /// <b>⚠ The seam is between the first line and the rest, and it moved outwards</b>
    /// (<c>adr/0112</c>). Only <see cref="WriteBody"/> into <paramref name="copy"/> has to happen on the
    /// simulation thread — ~10 ms at 1M, once per autosave. <see cref="SaveHash.Of"/>, the header and
    /// the drain all read the copy and the world's <em>schema</em>, so all three are a thread's to take.
    /// Task 6 drew the seam at <c>copy | write</c>; the hash sits on the far side of it, which is why
    /// carrying one costs the simulation thread nothing.
    /// </para>
    /// <para>
    /// <b>The header is written after the body is copied and before the body is drained</b>, because it
    /// carries a number that is a function of the body. Nothing seeks backwards: the copy is in memory,
    /// so the hash is known before the first byte reaches <paramref name="sink"/>.
    /// </para>
    /// <para>
    /// <b>⚠ A copy is now part of the format rather than an optimisation.</b> Before this, a save could
    /// have been streamed straight out of the live world and the copy was there to keep the write off
    /// the simulation thread. The hash is computed from the copy, so there is no longer a way to write a
    /// version-1 save without taking one.
    /// </para>
    /// </remarks>
    public static void Write(World world, ulong rulesetInForce, WorldSnapshot copy, ISaveSink sink)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(copy);
        ArgumentNullException.ThrowIfNull(sink);

        copy.Reset();
        WriteBody(world, copy);

        Span<byte> header = stackalloc byte[SaveHeader.Bytes];
        SaveHeader.Of(world, rulesetInForce, SaveHash.Of(world, copy.Bytes)).Write(header);
        sink.Write(header);

        copy.DrainTo(sink);
    }

    /// <summary>
    /// Writes the body — every table, no header. The copy, and the second half of a file.
    /// </summary>
    /// <param name="world">The world to write. Read, never modified.</param>
    /// <param name="sink">Where the bytes go.</param>
    /// <remarks>
    /// <para>
    /// <b>⚠ The header is not part of a copy of the world, and separating them is what lets a save carry
    /// a hash.</b> A header states what the <em>build</em> was — the format version, four world-creation
    /// constants, the Ruleset in force — and the ninth field states a fact about the body, so it cannot
    /// be written until the body exists. Task 6 had one writer producing header-and-body into both a
    /// file and a snapshot; splitting it means the copy is the hash's input exactly, with nothing in
    /// front of it to skip.
    /// </para>
    /// <para>
    /// <b>This is the walk <c>World.HashState</c> makes</b>, which is the property <see cref="SaveHash"/>
    /// rests on: the same tables in the same order, the same four scalars, the same saved columns over
    /// the same slots. Changing the order here without changing it there is caught by
    /// <c>SaveHashTests</c> on the first run.
    /// </para>
    /// </remarks>
    public static void WriteBody(World world, ISaveSink sink)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(sink);

        Span<byte> scalars = stackalloc byte[ScalarBytes];

        foreach (Rows table in world.Tables)
        {
            BinaryPrimitives.WriteInt32LittleEndian(scalars, table.SlotCount);
            BinaryPrimitives.WriteInt32LittleEndian(scalars[4..], table.LiveCount);
            BinaryPrimitives.WriteInt32LittleEndian(scalars[8..], table.FreeHead);
            BinaryPrimitives.WriteUInt64LittleEndian(scalars[12..], table.NextId);
            sink.Write(scalars);

            foreach (Column column in table.SavedColumns)
            {
                sink.Write(column.StorageBytes(table.SlotCount));
            }
        }
    }

    /// <summary>Reads a world back, and rebuilds everything the file does not carry.</summary>
    /// <param name="source">The file, positioned at the header.</param>
    /// <param name="rules">
    /// The Ruleset to put in force. <c>Core</c> cannot turn a content hash into Rules, so the caller
    /// resolves it and this reports what the file expected in <paramref name="header"/>.
    /// </param>
    /// <param name="header">What the file was written under, for the caller's Ruleset policy.</param>
    /// <remarks>
    /// <para>
    /// <b>It ends by calling <c>World.RebuildDerived</c>, which is the load's real work.</b> The file
    /// holds saved columns only; every derived list, index and cached Ruleset value is recomputed here
    /// — and a load is the one moment a derived structure is read in its pre-rebuild state, which is
    /// the failure the milestone is named for and what task 1's audit exists to catch.
    /// </para>
    /// <para>
    /// <b>⚠ It does not enforce the Ruleset policy and deliberately reports instead.</b> <c>05 §7</c>
    /// gives cross-Ruleset loading two answers — lenient in play, refused on an unaccounted mismatch in
    /// replay — and which one applies is a property of the shell doing the loading rather than of the
    /// file. Deciding here would give <c>Borough.Godot</c> the headless runner's policy.
    /// </para>
    /// <para>
    /// <b>The world is built at zero capacity and every table grown to exactly what the file says.</b>
    /// Nothing in the header records how big the world was, because nothing needs to: each table
    /// carries its own slot count, which is a better answer than one number for the whole world.
    /// </para>
    /// </remarks>
    public static World Read(ISaveSource source, Ruleset rules, out SaveHeader header)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(rules);

        Span<byte> headerBytes = stackalloc byte[SaveHeader.Bytes];
        source.Read(headerBytes);
        header = SaveHeader.Read(headerBytes);

        var world = new World(0, rules, header.Key);

        Span<byte> scalars = stackalloc byte[ScalarBytes];

        foreach (Rows table in world.Tables)
        {
            source.Read(scalars);

            table.Restore(
                BinaryPrimitives.ReadInt32LittleEndian(scalars),
                BinaryPrimitives.ReadInt32LittleEndian(scalars[4..]),
                BinaryPrimitives.ReadInt32LittleEndian(scalars[8..]),
                BinaryPrimitives.ReadUInt64LittleEndian(scalars[12..]),
                source);
        }

        world.RebuildDerived();

        ulong reloaded = world.HashState();

        if (reloaded != header.StateHash)
        {
            throw new InvalidOperationException(
                $"this save says its world hashes to 0x{header.StateHash:X16} and the world it loaded "
                + $"into hashes to 0x{reloaded:X16}. The file's columns were restored and the world "
                + "they describe is not the world that was saved — a corrupt file, or a build whose "
                + "declaration matches the writer's in shape and not in meaning (05 §4 invariant 6).");
        }

        return world;
    }
}
