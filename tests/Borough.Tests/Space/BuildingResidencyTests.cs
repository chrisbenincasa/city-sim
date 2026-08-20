using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Determinism;
using Borough.Core.Tables;

namespace Borough.Tests.Space;

/// <summary>
/// The query from a place to the Buildings on it — milestone 5b-bis task 1.
/// </summary>
/// <remarks>
/// <para>
/// <b>It exists because every candidate first Trip generator needed it and no document listed it</b>
/// (<c>adr/0081</c>). What is checked here is the two properties that make it safe to build things on
/// top of: that a rebuild reproduces it <em>exactly</em>, and that answering it costs nothing.
/// </para>
/// <para>
/// <b>Coordinates here are Tiles and the Cell is 32 Tiles wide</b>, which is a design constant and
/// never tuned (<c>CellGrid.TilesPerCell</c>). Tile 0 and Tile 31 share a Cell; Tile 32 does not.
/// </para>
/// </remarks>
public sealed class BuildingResidencyTests
{
    /// <summary>A Building is findable in the Cell its Lot stands in, and not in the next one.</summary>
    [Fact]
    public void A_building_is_in_the_cell_its_lot_stands_in()
    {
        var world = new World(1_000);
        int building = Build(world, east: 40, north: 8);

        Assert.Equal(new[] { building }, Query(world, CellRect.At(new Cells(1), new Cells(0))));
        Assert.Empty(Query(world, CellRect.At(new Cells(0), new Cells(0))));
    }

    /// <summary>
    /// Two Buildings sharing a Cell come back in ascending slot order.
    /// </summary>
    /// <remarks>
    /// Not a presentational preference. Slot order is the order a rebuild produces, and a maintained
    /// list that agreed with a rebuilt one on membership but not on order would be a derived structure
    /// whose value depended on whether the city had been saved — which no hash would report, because
    /// the list is derived and therefore not folded.
    /// </remarks>
    [Fact]
    public void Buildings_sharing_a_cell_come_back_in_slot_order()
    {
        var world = new World(1_000);
        int first = Build(world, east: 1, north: 1);
        int second = Build(world, east: 30, north: 2);

        Assert.Equal(
            new[] { first, second }, Query(world, CellRect.At(Cells.Zero, Cells.Zero)));
    }

    /// <summary>A box spanning several Cells walks them row-major, and each list in slot order.</summary>
    [Fact]
    public void A_box_walks_its_cells_in_a_stated_order()
    {
        var world = new World(1_000);
        int origin = Build(world, east: 1, north: 1);
        int eastwards = Build(world, east: 33, north: 1);
        int northwards = Build(world, east: 1, north: 33);

        Assert.Equal(
            new[] { origin, eastwards, northwards },
            Query(world, new CellRect(Cells.Zero, Cells.Zero, new Cells(2), new Cells(2))));
    }

    /// <summary>A demolished Building leaves the index.</summary>
    [Fact]
    public void A_demolished_building_is_no_longer_in_its_cell()
    {
        var world = new World(1_000);
        int building = Build(world, east: 1, north: 1);

        world.DestroyBuilding(world.Buildings.Rows.At(building), Ticks.Zero);

        Assert.Empty(Query(world, CellRect.At(Cells.Zero, Cells.Zero)));
    }

    /// <summary>
    /// <b>A rebuild reproduces the index exactly, across a recycled slot.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The recycled slot is the whole test.</b> <c>IndexList.InsertOrdered</c>'s own remarks give
    /// the failing sequence: <i>"create A and B, free A, create C, and appending gives <c>B, C</c>
    /// where a rebuild gives <c>C, B</c>."</i> A maintained list built with <c>Append</c> passes every
    /// membership assertion above and fails only here — and in production it would fail as a
    /// saved-and-reloaded city draining a list in a different order from a continuously-run one, with
    /// nothing to report it.
    /// </para>
    /// <para>
    /// <b>This is the test that licenses the <c>(derived AND rebuilt)</c> declaration.</b> <c>05 §3</c>
    /// requires that a derived structure's <em>order</em> be recoverable from saved state, not merely
    /// its membership, and *exactly rather than plausibly* is the phrase this asserts.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_rebuild_reproduces_the_index_exactly_across_a_recycled_slot()
    {
        var world = new World(1_000);

        int first = Build(world, east: 1, north: 1);
        _ = Build(world, east: 2, north: 2);

        world.DestroyBuilding(world.Buildings.Rows.At(first), Ticks.Zero);

        int recycled = Build(world, east: 3, north: 3);

        Assert.Equal(first, recycled);

        int[] maintained = Query(world, CellRect.At(Cells.Zero, Cells.Zero));

        world.RebuildDerived();

        Assert.Equal(maintained, Query(world, CellRect.At(Cells.Zero, Cells.Zero)));
        Assert.Equal(2, maintained.Length);
    }

    /// <summary>
    /// The query truncates to the caller's buffer and says how many it wrote.
    /// </summary>
    /// <remarks>
    /// <b>Truncation is the ordinary case here rather than the degraded one.</b> <c>adr/0081</c>'s
    /// assignment wants a <em>sample</em> of candidates — <c>adr/0017</c>, satisficing, never
    /// optimising — so a caller that fills a small buffer and stops is what this is for. Growing the
    /// buffer instead would mean allocating inside the hot path.
    /// </remarks>
    [Fact]
    public void The_query_truncates_rather_than_growing()
    {
        var world = new World(1_000);
        Build(world, east: 1, north: 1);
        Build(world, east: 2, north: 2);
        Build(world, east: 3, north: 3);

        Span<int> into = stackalloc int[2];
        int written = world.BuildingsInCells.In(
            CellRect.At(Cells.Zero, Cells.Zero), world.Buildings, into);

        Assert.Equal(2, written);
    }

    /// <summary>
    /// Answering the query allocates nothing. <c>adr/0002</c>'s hot flavour, checked rather than
    /// asserted.
    /// </summary>
    /// <remarks>
    /// Measured against the calling thread's allocated byte count, with the buffer allocated
    /// <em>before</em> the measurement: the claim is that answering costs nothing, not that the answer
    /// is free to store. <c>LayerQueryTests</c> sets this pattern for the project's first hot query and
    /// this is the second.
    /// </remarks>
    [Fact]
    public void Answering_the_query_allocates_nothing()
    {
        var world = new World(1_000);

        for (int i = 0; i < 16; i++)
        {
            Build(world, east: i * 32, north: 0);
        }

        var box = new CellRect(Cells.Zero, Cells.Zero, new Cells(16), new Cells(1));
        int[] into = new int[64];

        // Warm the path first, or the measurement is of the JIT rather than of the query.
        _ = world.BuildingsInCells.In(box, world.Buildings, into);

        int gen0 = GC.CollectionCount(0);
        int gen1 = GC.CollectionCount(1);
        int gen2 = GC.CollectionCount(2);
        long before = GC.GetAllocatedBytesForCurrentThread();

        for (int i = 0; i < 64; i++)
        {
            _ = world.BuildingsInCells.In(box, world.Buildings, into);
        }

        long after = GC.GetAllocatedBytesForCurrentThread();

        AllocationProbe.Record(
            "BuildingResidencyTests.Answering_the_query_allocates_nothing",
            after - before,
            GC.CollectionCount(0) - gen0,
            GC.CollectionCount(1) - gen1,
            GC.CollectionCount(2) - gen2);

        Assert.Equal(before, after);
    }

    /// <summary>
    /// Nothing on the query surface returns a string. <c>adr/0002</c>'s actual leak vector.
    /// </summary>
    /// <remarks>
    /// Not <c>using Godot;</c> but a method that returns a formatted string because a panel wanted
    /// one — and a candidate-Buildings query feeding a *choose where to work* panel is exactly the
    /// caller that would ask for a name.
    /// </remarks>
    [Fact]
    public void The_query_surface_returns_no_strings()
    {
        IEnumerable<string> offenders = typeof(BuildingResidency).GetMethods()
            .Where(method => method.DeclaringType == typeof(BuildingResidency))
            .Where(method => method.ReturnType == typeof(string))
            .Select(method => method.Name);

        Assert.Empty(offenders);
    }

    /// <summary>
    /// A box outside the map answers empty rather than throwing.
    /// </summary>
    /// <remarks>
    /// <see cref="CellRect.Clamp"/>'s policy, inherited rather than re-decided — the same boundary
    /// rule the convolution uses, where off-map is <em>absent</em> and contributes nothing.
    /// </remarks>
    [Fact]
    public void A_box_off_the_map_answers_empty()
    {
        var world = new World(1_000);
        Build(world, east: 1, north: 1);

        Assert.Empty(
            Query(world, new CellRect(new Cells(500), new Cells(500), new Cells(4), new Cells(4))));
    }

    private static int Build(World world, int east, int north)
    {
        Handle<Lot> lot = world.Lots.Create(new Tiles(east), new Tiles(north), zone: 1);

        // Through World.CreateBuilding rather than BuildingTable.Create, because the index is
        // maintained at the door and a test that went round it would be testing nothing.
        return world.Buildings.Rows.Resolve(
            world.CreateBuilding(lot, kind: 1, Ticks.Zero, WorldKey.FromSeed(1)));
    }

    // ---- the sampling pair ------------------------------------------------------------------------

    /// <summary>
    /// <b><c>CountIn</c> and <c>NthIn</c> address exactly what <c>In</c> enumerates, in the same
    /// order.</b>
    /// </summary>
    /// <remarks>
    /// The property that makes the pair a fair draw: <c>CountIn</c> is the denominator and
    /// <c>NthIn(n)</c> is the nth thing <c>In</c> would have written. Asserted against <c>In</c>
    /// rather than against a literal, because a literal here would be a second copy of the traversal
    /// order and the two would drift — which is the failure the ordered insert exists to prevent one
    /// level down.
    /// </remarks>
    [Fact]
    public void The_sampling_pair_addresses_what_the_enumeration_walks()
    {
        var world = new World(1_000);

        Build(world, east: 1, north: 1);
        Build(world, east: 30, north: 2);
        Build(world, east: 33, north: 1);
        Build(world, east: 1, north: 33);

        var box = new CellRect(Cells.Zero, Cells.Zero, new Cells(2), new Cells(2));
        int[] walked = Query(world, box);

        Assert.Equal(4, world.BuildingsInCells.CountIn(box));

        for (int i = 0; i < walked.Length; i++)
        {
            Assert.Equal(walked[i], world.BuildingsInCells.NthIn(box, world.Buildings, i));
        }
    }

    /// <summary>An ordinal past the end is a named absence rather than a wrong answer.</summary>
    /// <remarks>
    /// <b>The case a caller reaches by racing its own denominator</b>, which is ordinary here: a
    /// sampling caller counts once and looks several times, and a demolition between the two shrinks
    /// the box. <c>Rows.NoSlot</c> is what the caller already tests for, so this is the cheap outcome
    /// rather than an exception at a write site.
    /// </remarks>
    [Fact]
    public void An_ordinal_past_the_end_of_the_box_is_no_slot()
    {
        var world = new World(1_000);

        Build(world, east: 1, north: 1);

        var box = CellRect.At(Cells.Zero, Cells.Zero);

        Assert.Equal(1, world.BuildingsInCells.CountIn(box));
        Assert.Equal(Rows.NoSlot, world.BuildingsInCells.NthIn(box, world.Buildings, 1));
    }

    /// <summary>An empty box holds nothing, and a demolition takes its Building out of the count.</summary>
    /// <remarks>
    /// The count is a cached list length, so it is maintained in three places rather than derived on
    /// read — and a cache that only ever grows is the defect this asserts against. The rebuild
    /// agreeing with the maintained count is <c>A_rebuild_reproduces_the_maintained_index</c>'s
    /// business, which walks the same lists.
    /// </remarks>
    [Fact]
    public void A_demolished_building_leaves_the_count()
    {
        var world = new World(1_000);
        int building = Build(world, east: 1, north: 1);

        var box = CellRect.At(Cells.Zero, Cells.Zero);

        Assert.Equal(1, world.BuildingsInCells.CountIn(box));

        world.DestroyBuilding(world.Buildings.Rows.At(building), Ticks.Zero);

        Assert.Equal(0, world.BuildingsInCells.CountIn(box));
        Assert.Equal(Rows.NoSlot, world.BuildingsInCells.NthIn(box, world.Buildings, 0));
    }

    private static int[] Query(World world, CellRect area)
    {
        int[] into = new int[64];
        int written = world.BuildingsInCells.In(area, world.Buildings, into);

        return into[..written];
    }
}
