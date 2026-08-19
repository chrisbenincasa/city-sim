namespace Borough.Core.Persistence;

using System.Buffers.Binary;
using Borough.Core.Entities;
using Borough.Core.Tables;

/// <summary>
/// The State Hash of the world a save body describes, computed from the body and never from the world.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is what lets a save carry a verified hash for nothing</b> (<c>adr/0112</c>). <c>adr/0087</c>
/// asks for a hash <em>"computed on the background thread from the copy, never on the simulation thread
/// as part of taking it"</em>, and milestone 8 task 6 recorded that as unbuildable, because
/// <c>HandleColumn.Fold</c> folds the target row's monotonic id and a handle's bytes do not contain
/// one. ***That is true of a fold over a column's bytes and false of a fold over the copy.*** <c>Rows</c>
/// declares <c>id</c> and <c>generation</c> as <see cref="Disposition.Saved"/> columns, so the id the
/// handle resolves to is in the file — in another table's block, which is the whole of the difficulty
/// and none of the impossibility.
/// </para>
/// <para>
/// <b>It reproduces <c>World.HashState</c> exactly, and it can because the two walks are the same
/// walk.</b> The hash folds the seed, then per table in <c>World.Tables</c> order the allocator's four
/// scalars and every saved column over <c>[0, slotCount)</c>; <see cref="SaveFile.WriteBody"/> writes
/// the same four scalars and the same columns in the same order. The save is the hash's input, written
/// down. Nothing here re-implements the fold — <see cref="Rows.FoldScalars"/> and
/// <see cref="Column.Fold"/> are the same members the live path calls, handed bytes from a buffer
/// instead of from a column.
/// </para>
/// <para>
/// <b>⚠ The world is read for its <em>schema</em> and never for a value.</b> What this needs from it is
/// the table order, each table's saved column list, each column's width, and which table a handle
/// column points at — all of which are fixed at <c>Rows.Seal</c> and never move again. That is what
/// makes the call safe to hand to a thread while the simulation runs on: it touches no array a phase
/// could be writing. ***A schema read is not a state read***, and the distinction is the reason this
/// costs the simulation thread nothing.
/// </para>
/// <para>
/// <b>Two passes, because a handle can point forwards.</b> A column's own bytes are found by walking
/// the body once; the <em>target</em> table's <c>id</c> and <c>generation</c> bytes may be in a block
/// that has not been reached yet, so the offsets are collected first and the fold runs second.
/// </para>
/// </remarks>
public static class SaveHash
{
    /// <summary>The four allocator scalars, per table. <see cref="SaveFile.ScalarBytes"/>.</summary>
    private const int ScalarBytes = SaveFile.ScalarBytes;

    /// <summary>
    /// The State Hash of the world <paramref name="body"/> describes.
    /// </summary>
    /// <param name="world">
    /// A world of the same shape — the schema only. Its <em>values</em> are never read, so it need not
    /// be the world the body was taken from and may be being stepped on another thread.
    /// </param>
    /// <param name="body">
    /// A save body, as <see cref="SaveFile.WriteBody"/> writes it: no header, every table in order.
    /// </param>
    /// <returns>
    /// The number <c>world.HashState()</c> returned at the instant the body was copied.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// The body is not the right length for this build's declaration, which means it was written by a
    /// different one. The header's format version is what is supposed to catch that first.
    /// </exception>
    public static ulong Of(World world, ReadOnlySpan<byte> body)
    {
        ArgumentNullException.ThrowIfNull(world);

        ReadOnlySpan<Rows> tables = world.Tables;

        int[] blockStart = new int[tables.Length];
        int[] slotCounts = new int[tables.Length];

        int offset = 0;

        for (int i = 0; i < tables.Length; i++)
        {
            if (offset + ScalarBytes > body.Length)
            {
                throw new InvalidOperationException(
                    $"a save body of {body.Length} bytes ends inside table '{tables[i].Name}'s scalars. "
                    + "It was written by a build with a different declaration (adr/0086).");
            }

            blockStart[i] = offset;
            slotCounts[i] = BinaryPrimitives.ReadInt32LittleEndian(body[offset..]);
            offset += ScalarBytes + WidthOf(tables[i], slotCounts[i]);

            if (offset > body.Length)
            {
                throw new InvalidOperationException(
                    $"a save body of {body.Length} bytes ends inside table '{tables[i].Name}'s columns, "
                    + $"which this build lays out as {offset} bytes by that point.");
            }
        }

        if (offset != body.Length)
        {
            throw new InvalidOperationException(
                $"a save body of {body.Length} bytes has {body.Length - offset} left over after every "
                + "table this build declares. It was written by a build with more tables in it.");
        }

        ulong hash = World.HashSeed;

        for (int i = 0; i < tables.Length; i++)
        {
            Rows table = tables[i];
            int at = blockStart[i];
            int slotCount = slotCounts[i];

            Rows.FoldScalars(
                ref hash,
                slotCount,
                BinaryPrimitives.ReadInt32LittleEndian(body[(at + 4)..]),
                BinaryPrimitives.ReadInt32LittleEndian(body[(at + 8)..]),
                BinaryPrimitives.ReadUInt64LittleEndian(body[(at + 12)..]));

            int column = at + ScalarBytes;

            foreach (Column field in table.SavedColumns)
            {
                int width = field.BytesPerRow * slotCount;

                field.Fold(
                    ref hash,
                    body.Slice(column, width),
                    TargetsOf(field, tables, blockStart, slotCounts, body));

                column += width;
            }
        }

        return hash;
    }

    /// <summary>How many bytes this table's saved columns occupy at a given slot count.</summary>
    private static int WidthOf(Rows table, int slotCount)
    {
        int width = 0;

        foreach (Column column in table.SavedColumns)
        {
            width += column.BytesPerRow * slotCount;
        }

        return width;
    }

    /// <summary>
    /// The <c>id</c> and <c>generation</c> bytes of the table a handle column points at, located in the
    /// same body.
    /// </summary>
    /// <remarks>
    /// <b>The target is found by reference and its two columns by identity</b>, never by position, so
    /// declaring a new column above <c>id</c> or reordering <c>World.Tables</c> cannot silently point
    /// this at the wrong bytes. Both walks are O(tables) and O(columns) and run once per handle column
    /// per save, not once per row.
    /// </remarks>
    private static TargetIds TargetsOf(
        Column column,
        ReadOnlySpan<Rows> tables,
        int[] blockStart,
        int[] slotCounts,
        ReadOnlySpan<byte> body)
    {
        if (column.HandleTarget is not { } target)
        {
            return default;
        }

        int index = -1;

        for (int i = 0; i < tables.Length; i++)
        {
            if (ReferenceEquals(tables[i], target))
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            throw new InvalidOperationException(
                $"column '{column.Name}' holds handles into table '{target.Name}', which is not in "
                + "World.Tables — so its rows are not in the save and the hash cannot resolve them. A "
                + "handle column's target must be a table the world folds (adr/0112).");
        }

        int slotCount = slotCounts[index];
        int at = blockStart[index] + ScalarBytes;
        int ids = -1;
        int generations = -1;

        foreach (Column field in target.SavedColumns)
        {
            if (ReferenceEquals(field, target.IdColumn))
            {
                ids = at;
            }
            else if (ReferenceEquals(field, target.GenerationColumn))
            {
                generations = at;
            }

            at += field.BytesPerRow * slotCount;
        }

        if (ids < 0 || generations < 0)
        {
            throw new InvalidOperationException(
                $"table '{target.Name}' does not carry both its id and its generation column in the "
                + "save. Rows declares both Saved and the hash resolves handles through them.");
        }

        return TargetIds.Saved(
            body.Slice(generations, slotCount * sizeof(uint)),
            body.Slice(ids, slotCount * sizeof(ulong)),
            slotCount);
    }
}
