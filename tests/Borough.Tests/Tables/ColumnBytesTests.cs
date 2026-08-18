using System.Text;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Tables;
using Borough.Tests.Golden;
using Xunit.Abstractions;

namespace Borough.Tests.Tables;

/// <summary>
/// Milestone 8 task 2 — a column's bytes, out and in, and the saved set totalled.
/// </summary>
/// <remarks>
/// <para>
/// <b>The save's half of <c>Column.Fold</c>.</b> <c>Fold</c> was the only type-erased traversal of
/// column storage in the project; <c>WriteBytes</c>/<c>ReadBytes</c> are its sibling, and they diverge
/// from it in exactly one place by design — a <c>HandleColumn</c> overrides <c>Fold</c> to fold the
/// target's monotonic id and inherits these, so the file stores the handle. ***A save round-trip must
/// preserve the hash and need not preserve the bytes*** (<c>adr/0086</c>).
/// </para>
/// <para>
/// <b>This is not the save.</b> There is no header, no version, no allocator restore and no file —
/// tasks 3 to 5 are those. What is here is the one capability the table layer did not have, tested on
/// its own so that when the format arrives it is composing something already known to work.
/// </para>
/// </remarks>
public sealed class ColumnBytesTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    /// <summary>
    /// Every saved column in a stepped world survives a write-out, a scribble and a read-back, and
    /// the State Hash comes back with them.
    /// </summary>
    /// <remarks>
    /// <b>The strongest claim task 2 can make without the allocator restore.</b> The hash folds the
    /// four allocator scalars and then the saved columns; this leaves the scalars alone and destroys
    /// every saved byte, so a restored hash is exactly the statement <em>the column half round-trips</em>.
    /// The scalars are task 3, and they are the half with no precedent in the tree.
    /// </remarks>
    [Fact]
    public void Every_saved_column_round_trips_through_bytes()
    {
        World world = Stepped(512);

        ulong before = world.HashState();
        byte[][] saved = WriteSavedColumns(world);

        Scribble(world);
        Assert.NotEqual(before, world.HashState());

        ReadSavedColumns(world, saved);

        Assert.Equal(before, world.HashState());
    }

    /// <summary>
    /// A handle column's bytes are the handle, where its fold is the target's id — the one place the
    /// file and the hash disagree, and they disagree on purpose.
    /// </summary>
    /// <remarks>
    /// <b>A stale <see cref="Reference.Severable"/> handle survives the round trip unchanged</b>,
    /// which is the property a load must not "repair": the stale handle <em>is</em> the state, and a
    /// Citizen whose Workplace no longer resolves is exactly the fact that the job no longer exists.
    /// A loader that normalised it to <c>default</c> would delete a fact and move no hash while doing
    /// it — the fold of a dangling handle is a sentinel either way.
    /// </remarks>
    [Fact]
    public void A_severable_handle_survives_the_round_trip_stale()
    {
        World world = Stepped(512);

        int citizen = FindEmployedCitizen(world);
        Handle<Building> workplace = world.Citizens.Workplace[citizen];

        world.DestroyBuilding(workplace, new Ticks(512));

        Assert.False(world.Buildings.Rows.TryResolve(workplace, out _));
        Assert.Equal(workplace, world.Citizens.Workplace[citizen]);

        ulong before = world.HashState();
        byte[][] saved = WriteSavedColumns(world);

        Scribble(world);
        ReadSavedColumns(world, saved);

        // The same dangling handle, not a repaired one and not a zeroed one.
        Assert.Equal(workplace, world.Citizens.Workplace[citizen]);
        Assert.False(world.Buildings.Rows.TryResolve(world.Citizens.Workplace[citizen], out _));
        Assert.Equal(before, world.HashState());
    }

    /// <summary>
    /// A column's width is its declaration's, so a byte count that disagrees is refused rather than
    /// read.
    /// </summary>
    /// <remarks>
    /// The refusal exists because this is the layer a format-version mismatch reaches if the header
    /// let one through: a short buffer would otherwise leave the tail of a column holding whatever it
    /// held before, which is state from another city with no mark on it.
    /// </remarks>
    [Fact]
    public void A_byte_count_that_disagrees_with_the_declaration_is_refused()
    {
        World world = Stepped(64);
        Rows table = world.Citizens.Rows;
        Column column = table.SavedColumns[0];

        byte[] correct = new byte[column.BytesPerRow * table.SlotCount];
        column.WriteBytes(correct, table.SlotCount);

        InvalidOperationException refused = Assert.Throws<InvalidOperationException>(
            () => column.ReadBytes(correct.AsSpan(0, correct.Length - 1), table.SlotCount));

        Assert.Contains("expects", refused.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// The saved set in bytes, per table and totalled, at the golden fixture and at 1M — the figure
    /// <c>adr/0087</c> names as owed and forbids guessing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>⚠ S0a's 85.98 MiB at 1M is the *memory* figure and is not this.</b> That number counts every
    /// column, derived and scratch included, because it sizes the world; this counts the
    /// <see cref="Disposition.Saved"/> ones, because they are what reaches the file. Quoting the one
    /// for the other is <c>plans/0012</c> <em>Cause 5</em> with a number that looks right.
    /// </para>
    /// <para>
    /// <b>It asserts a relation rather than a total.</b> A committed byte count would be a baseline
    /// that every future column moves, and moving it is not evidence of anything — the number is a
    /// *report*, and what is worth holding is that the saved set is a strict subset of the world's
    /// storage and a large fraction of it. The figures themselves are printed and are carried in
    /// <c>plans/0030</c>.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_saved_set_is_reported_per_table_and_totalled()
    {
        Report("golden fixture", GoldenFixtures.Build());
        Report("stepped, 4,000 Citizens, 512 Ticks", Stepped(512));
        Report("1,000,000 Citizens, allocated capacity", new World(1_000_000));
    }

    private void Report(string label, World world)
    {
        var text = new StringBuilder();
        text.AppendLine($"--- saved set: {label} ---");
        text.AppendLine("table                 rows   saved B/row      saved bytes       all bytes");

        long savedTotal = 0;
        long allTotal = 0;

        foreach (Rows table in world.Tables)
        {
            int rows = table.SlotCount > 0 ? table.SlotCount : table.Capacity;

            int allPerRow = table.BytesPerRow(Touch.PerTick)
                          + table.BytesPerRow(Touch.Wake)
                          + table.BytesPerRow(Touch.Cold);

            long saved = (long)table.SavedBytesPerRow * rows;
            long all = (long)allPerRow * rows;

            savedTotal += saved;
            allTotal += all;

            text.AppendLine(
                $"{table.Name,-18} {rows,7:N0} {table.SavedBytesPerRow,12} {saved,16:N0} {all,15:N0}");

            // Every table's saved width is a subset of its storage, by declaration.
            Assert.True(table.SavedBytesPerRow <= allPerRow, table.Name);
        }

        text.AppendLine(
            $"{"TOTAL",-18} {"",7} {"",12} {savedTotal,16:N0} {allTotal,15:N0}");
        text.AppendLine(
            $"saved is {100.0 * savedTotal / allTotal:F1}% of storage, "
            + $"{savedTotal / 1024.0 / 1024.0:F2} MiB against {allTotal / 1024.0 / 1024.0:F2} MiB");

        _output.WriteLine(text.ToString());

        Assert.True(savedTotal > 0);
        Assert.True(savedTotal < allTotal, "the saved set cannot exceed the storage it is drawn from");
    }

    private static int FindEmployedCitizen(World world)
    {
        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (world.Citizens.Rows.IsLive(slot)
                && world.Buildings.Rows.TryResolve(world.Citizens.Workplace[slot], out _))
            {
                return slot;
            }
        }

        throw new InvalidOperationException(
            "no employed Citizen, so the severable-handle case cannot be constructed.");
    }

    /// <summary>Every saved column's storage, table by table, column by column.</summary>
    private static byte[][] WriteSavedColumns(World world)
    {
        List<byte[]> buffers = [];

        foreach (Rows table in world.Tables)
        {
            foreach (Column column in table.SavedColumns)
            {
                byte[] bytes = new byte[column.BytesPerRow * table.SlotCount];
                column.WriteBytes(bytes, table.SlotCount);
                buffers.Add(bytes);
            }
        }

        return [.. buffers];
    }

    private static void ReadSavedColumns(World world, byte[][] buffers)
    {
        int index = 0;

        foreach (Rows table in world.Tables)
        {
            foreach (Column column in table.SavedColumns)
            {
                column.ReadBytes(buffers[index++], table.SlotCount);
            }
        }

        Assert.Equal(buffers.Length, index);
    }

    /// <summary>Fills every saved column with a pattern, so a restored hash means something.</summary>
    private static void Scribble(World world)
    {
        foreach (Rows table in world.Tables)
        {
            foreach (Column column in table.SavedColumns)
            {
                byte[] rubbish = new byte[column.BytesPerRow * table.SlotCount];
                Array.Fill(rubbish, (byte)0xC3);
                column.ReadBytes(rubbish, table.SlotCount);
            }
        }
    }

    private static World Stepped(int ticks)
    {
        var key = WorldKey.FromSeed(GoldenFixtures.Seed);
        var world = new World(GoldenFixtures.Population, GoldenFixtures.Rules());

        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        for (int tick = 0; tick < ticks; tick++)
        {
            simulation.Step(default);
        }

        return world;
    }
}
