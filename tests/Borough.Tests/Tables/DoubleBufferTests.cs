using Borough.Core.Entities;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Tests.Tables;

/// <summary>
/// <c>adr/0037</c>'s per-table buffering property, which slice 4 declared and slice 6 implements.
/// </summary>
/// <remarks>
/// <para>
/// <b>A table is double-buffered if and only if a parallel phase both reads and writes it</b>
/// (<c>05 §3</c>). That is two tables in the whole design, not all of them — Lane dynamics, which does
/// not exist yet, and Map Layer cells, which is the first. Everything else is written only by the
/// serial phases Settle and Growth, and a serial writer has no peer to race.
/// </para>
/// <para>
/// <b>The rule's polarity is what these tests protect.</b> It would be easy and wrong to give every
/// table a second copy for safety: that is the ~150 MB-per-Tick full-world buffer <c>adr/0037</c>
/// deleted, arriving one table at a time. A table that does not declare the hazard must not have the
/// storage, so asking for its write half is an error rather than a silent allocation.
/// </para>
/// </remarks>
public class DoubleBufferTests
{
    [Fact]
    public void Layer_Cells_declare_two_copies_and_the_entity_tables_declare_one()
    {
        World world = new(1_000);

        Assert.Equal(Buffering.TwoCopies, world.Layers.Cells.Rows.Buffering);

        Assert.Equal(Buffering.OneCopy, world.Lots.Rows.Buffering);
        Assert.Equal(Buffering.OneCopy, world.Buildings.Rows.Buffering);
        Assert.Equal(Buffering.OneCopy, world.Households.Rows.Buffering);
        Assert.Equal(Buffering.OneCopy, world.Citizens.Rows.Buffering);
    }

    /// <summary>A single-copy table has no write half, and asking for one says so.</summary>
    [Fact]
    public void A_single_copy_table_refuses_to_hand_out_a_write_half()
    {
        World world = new(1_000);

        Assert.Throws<InvalidOperationException>(() => world.Lots.Rows.PrepareBack());
        Assert.Throws<InvalidOperationException>(() => world.Lots.Rows.SwapBuffers());
        Assert.Throws<InvalidOperationException>(() => world.Lots.East.BackSpan.Length);
    }

    /// <summary>Writing the write half leaves the live half alone until the swap.</summary>
    [Fact]
    public void A_write_to_the_write_half_is_invisible_until_the_swap()
    {
        LayerCellTable cells = new(8);
        int slot = cells.Rows.Resolve(cells.Create(new Cells(3), new Cells(4)));

        cells.Pollution[slot] = 100;
        cells.Rows.PrepareBack();
        cells.Pollution.AtBack(slot) = 250;

        Assert.Equal(100, cells.Pollution[slot]);

        cells.Rows.SwapBuffers();

        Assert.Equal(250, cells.Pollution[slot]);
    }

    /// <summary>
    /// A partial write keeps every Cell it did not touch, which is what incremental diffusion needs.
    /// </summary>
    /// <remarks>
    /// <b>This is the reason <see cref="Rows.PrepareBack"/> seeds rather than clears.</b> Diffusion
    /// recomputes a halo and leaves the rest alone; an unseeded write half would swap in values from
    /// two cycles ago everywhere the halo did not reach. The field would then flicker between two
    /// states on the diffusion cadence, which reads as an art problem rather than as a buffering bug.
    /// </remarks>
    [Fact]
    public void A_partial_write_leaves_untouched_rows_at_their_live_values()
    {
        LayerCellTable cells = new(8);
        int kept = cells.Rows.Resolve(cells.Create(new Cells(1), new Cells(1)));
        int written = cells.Rows.Resolve(cells.Create(new Cells(2), new Cells(2)));

        cells.Pollution[kept] = 7;
        cells.Pollution[written] = 11;

        // Two cycles, because a single one cannot tell a seeded write half from a zeroed one.
        for (int cycle = 0; cycle < 2; cycle++)
        {
            cells.Rows.PrepareBack();
            cells.Pollution.AtBack(written) = 20 + cycle;
            cells.Rows.SwapBuffers();
        }

        Assert.Equal(7, cells.Pollution[kept]);
        Assert.Equal(21, cells.Pollution[written]);
    }

    /// <summary>
    /// Freeing a row clears both halves, so a recycled slot cannot swap its previous occupant back in.
    /// </summary>
    [Fact]
    public void Freeing_a_row_clears_the_write_half_too()
    {
        LayerCellTable cells = new(8);
        Handle<LayerCell> handle = cells.Create(new Cells(5), new Cells(5));
        int slot = cells.Rows.Resolve(handle);

        cells.Rows.PrepareBack();
        cells.Pollution.AtBack(slot) = 999;
        cells.Rows.Free(handle);
        cells.Rows.SwapBuffers();

        int recycled = cells.Rows.Resolve(cells.Create(new Cells(6), new Cells(6)));

        Assert.Equal(slot, recycled);
        Assert.Equal(0, cells.Pollution[recycled]);
    }

    /// <summary>
    /// A swap does not move the allocator's own state, because the seed made both halves agree.
    /// </summary>
    /// <remarks>
    /// The failure this rules out is the loudest one available: <c>id</c> and <c>generation</c> are
    /// columns like any other, so a swap that moved a stale copy of them would resurrect freed rows
    /// and hand out handles that resolve to the wrong entity — in a column the State Hash folds.
    /// </remarks>
    [Fact]
    public void A_swap_does_not_move_the_allocator_state()
    {
        LayerCellTable cells = new(8);
        Handle<LayerCell> handle = cells.Create(new Cells(9), new Cells(9));

        cells.Rows.PrepareBack();
        cells.Rows.SwapBuffers();

        Assert.True(cells.Rows.IsValid(handle));
        Assert.Equal(new Cells(9), cells.East[cells.Rows.Resolve(handle)]);
    }

    /// <summary>The State Hash folds the live half, and moves when a swap makes a write live.</summary>
    [Fact]
    public void The_State_Hash_follows_the_swap()
    {
        World world = new(1_000);
        LayerCellTable cells = world.Layers.Cells;
        int slot = cells.Rows.Resolve(cells.Create(new Cells(2), new Cells(3)));

        ulong before = world.HashState();

        cells.Rows.PrepareBack();
        cells.Pollution.AtBack(slot) = 4_242;

        Assert.Equal(before, world.HashState());

        cells.Rows.SwapBuffers();

        Assert.NotEqual(before, world.HashState());
    }
}
