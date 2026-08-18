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
/// <b>The only fixed-size buffer is 52 bytes of header and 24 of table scalars</b>, both stack-allocated.
/// </para>
/// </remarks>
public static class SaveFile
{
    /// <summary>The four allocator scalars, per table: slot count, live count, free head, next id.</summary>
    private const int ScalarBytes = 4 + 4 + 4 + 8;

    /// <summary>Writes a world. The sink owns the file; this owns the order.</summary>
    /// <param name="world">The world to write. Read, never modified.</param>
    /// <param name="rulesetInForce">
    /// The content hash of the Ruleset in force — <c>Simulation.RulesetInForce</c>. It goes in the
    /// header, which is what dissolves the field (<c>adr/0111</c>).
    /// </param>
    /// <param name="sink">Where the bytes go.</param>
    /// <remarks>
    /// <b>⚠ It does not take the copy <c>adr/0087</c> requires, and must not be handed a live world on a
    /// thread that is stepping it.</b> The copy is task 6's and the seam is drawn around this call:
    /// what moves to a background thread later is the hash, this, and the write, with the copy staying
    /// on the simulation thread. Called synchronously in this milestone (<c>plans/0030</c> D4).
    /// </remarks>
    public static void Write(World world, ulong rulesetInForce, ISaveSink sink)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(sink);

        Span<byte> header = stackalloc byte[SaveHeader.Bytes];
        SaveHeader.Of(world, rulesetInForce).Write(header);
        sink.Write(header);

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

        return world;
    }
}
