using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Tables;
using Borough.Tests.Golden;
using Xunit.Abstractions;

namespace Borough.Tests.Persistence;

/// <summary>
/// Milestone 8 task 5 — a world out to bytes and back, in one pass each way.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is not yet the Factorio test.</b> That is task 7 — run N, save, reload, run M, against run
/// N+M — and it is the one that can catch a derived column rebuilding to the wrong value, because only
/// running on carries a wrong derived value into saved state. What is here is the round trip: the same
/// hash comes back, and the file is written and read without either side holding a buffer the size of
/// it.
/// </para>
/// <para>
/// ⚠ <b>The streaming property needs its own assertion and a round trip cannot supply one.</b> A writer
/// that assembled the whole file and handed it over in one call would pass every equality test here.
/// <see cref="MemorySave.LargestWrite"/> is what separates them, and the bound it is held to is *the
/// largest single column*, which is a fact about the declaration rather than about the writer.
/// </para>
/// </remarks>
public sealed class SaveFileTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    private const ulong InForce = 0x0BAD_F00D_0BAD_F00DUL;

    [Fact]
    public void A_world_comes_back_at_the_hash_it_was_saved_at()
    {
        World saved = Stepped(512);
        ulong hash = saved.HashState();

        var file = new MemorySave();
        SaveFile.Write(saved, InForce, file);

        World loaded = SaveFile.Read(file, GoldenFixtures.Rules(), out SaveHeader header);

        Assert.Equal(hash, loaded.HashState());
        Assert.Equal(InForce, header.RulesetInForce);
        Assert.Equal(saved.Key, loaded.Key);
        Assert.Equal(0, file.Unread);
    }

    /// <summary>
    /// ⚠ <b>The assertion the task exists for.</b> Neither direction may hold the file: the largest
    /// hand-over is the largest single column, and at the golden fixture that is a small fraction of
    /// the whole.
    /// </summary>
    [Fact]
    public void Neither_direction_buffers_the_file()
    {
        World saved = Stepped(512);

        var file = new MemorySave();
        SaveFile.Write(saved, InForce, file);

        int widest = WidestColumn(saved);
        int total = file.Bytes.Length;

        _output.WriteLine($"file {total:N0} B in {file.Writes:N0} writes");
        _output.WriteLine($"largest write {file.LargestWrite:N0} B, widest column {widest:N0} B");
        _output.WriteLine($"peak as a share of the file: {100.0 * file.LargestWrite / total:F2}%");

        Assert.Equal(widest, file.LargestWrite);
        Assert.True(
            file.LargestWrite < total,
            $"the largest single write was {file.LargestWrite} of {total} bytes, which is the whole "
            + "file: the writer is assembling it rather than streaming it.");
    }

    /// <summary>
    /// The header is written before anything else and read before anything else, so a file that is not
    /// a save is refused before a single column has been touched.
    /// </summary>
    [Fact]
    public void A_file_that_is_not_a_save_is_refused_before_any_table_is_read()
    {
        var file = new MemorySave();
        SaveFile.Write(Stepped(64), InForce, file);

        byte[] bytes = file.Bytes;
        bytes[0] = (byte)'X';

        var corrupt = new MemorySave();
        corrupt.Write(bytes);

        InvalidOperationException refused = Assert.Throws<InvalidOperationException>(
            () => SaveFile.Read(corrupt, GoldenFixtures.Rules(), out _));

        Assert.Contains("not a borough save", refused.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// ⚠ <b>A truncated save is refused by the source running out, which is where the width check went
    /// when task 5 removed <c>Column.ReadBytes</c>.</b> The refusal is the same one and it fires in one
    /// place now instead of in every column.
    /// </summary>
    [Fact]
    public void A_truncated_save_is_refused()
    {
        var file = new MemorySave();
        SaveFile.Write(Stepped(64), InForce, file);
        file.TruncateTo(file.Bytes.Length - 1);
        file.Rewind();

        InvalidOperationException refused = Assert.Throws<InvalidOperationException>(
            () => SaveFile.Read(file, GoldenFixtures.Rules(), out _));

        Assert.Contains("truncated save", refused.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// The load is slot-exact: the next allocation lands where the saved world's would have, which is
    /// what the free list and the id counter being saved state buys (<c>adr/0086</c>).
    /// </summary>
    [Fact]
    public void The_load_is_slot_exact()
    {
        World saved = SteppedWithAGap(512);

        var file = new MemorySave();
        SaveFile.Write(saved, InForce, file);

        World loaded = SaveFile.Read(file, GoldenFixtures.Rules(), out _);

        Rows before = saved.Households.Rows;
        Rows after = loaded.Households.Rows;

        Assert.Equal(before.SlotCount, after.SlotCount);
        Assert.Equal(before.LiveCount, after.LiveCount);
        Assert.NotEqual(Rows.NoSlot, after.FreeHead);
        Assert.Equal(before.FreeHead, after.FreeHead);
    }

    /// <summary>
    /// ⚠ <b>The load builds its world at zero capacity, which is the path that found two growth
    /// defects.</b> <c>Rows.GrowTo</c> doubled from the declared capacity and **did not terminate** from
    /// zero; the allocator's own <c>Grow</c> shared the premise and returned a capacity of zero. Both
    /// are reachable only through a load, because every table is sized per thousand Citizens and no
    /// other caller builds a world with none.
    /// </summary>
    [Fact]
    public void A_loaded_world_can_be_allocated_into()
    {
        var file = new MemorySave();
        SaveFile.Write(Stepped(64), InForce, file);

        World loaded = SaveFile.Read(file, GoldenFixtures.Rules(), out _);
        int before = loaded.Households.Rows.SlotCount;

        for (int i = 0; i < 64; i++)
        {
            loaded.UnplacedPool.Rows.Allocate();
        }

        Assert.Equal(before, loaded.Households.Rows.SlotCount);
        Assert.True(loaded.UnplacedPool.Rows.LiveCount >= 64);
    }

    /// <summary>
    /// The file's column set is the hash's <c>Saved</c> set, table by table — <c>adr/0086</c>'s owed
    /// structural test in its cheapest form. ⚠ <b>Task 7 owes the full one</b>; this asserts the size
    /// identity, which is what a reader can check without a second traversal.
    /// </summary>
    [Fact]
    public void The_files_size_is_the_saved_sets_size_and_nothing_else()
    {
        World saved = Stepped(512);

        var file = new MemorySave();
        SaveFile.Write(saved, InForce, file);

        int expected = SaveHeader.Bytes;

        foreach (Rows table in saved.Tables)
        {
            expected += 20;

            foreach (Column column in table.SavedColumns)
            {
                expected += column.BytesPerRow * table.SlotCount;
            }
        }

        Assert.Equal(expected, file.Bytes.Length);
    }

    private static int WidestColumn(World world)
    {
        int widest = 0;

        foreach (Rows table in world.Tables)
        {
            foreach (Column column in table.SavedColumns)
            {
                int width = column.BytesPerRow * table.SlotCount;

                if (width > widest)
                {
                    widest = width;
                }
            }
        }

        return widest;
    }

    private static World Stepped(int ticks)
    {
        var key = WorldKey.FromSeed(GoldenFixtures.Seed);
        var world = new World(GoldenFixtures.Population, GoldenFixtures.Rules(), key);
        var simulation = new Simulation(world, key);

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        for (int tick = 0; tick < ticks; tick++)
        {
            simulation.Step(default);
        }

        return world;
    }

    private static World SteppedWithAGap(int ticks)
    {
        World world = Stepped(ticks);

        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (world.Households.Rows.IsLive(slot))
            {
                world.DestroyHousehold(world.Households.Rows.At(slot));
                break;
            }
        }

        return world;
    }
}
