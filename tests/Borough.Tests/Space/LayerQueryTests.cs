using Borough.Core.Space;

namespace Borough.Tests.Space;

/// <summary>
/// <c>layer_cells(aabb, layer)</c> — the project's first hot entry point.
/// </summary>
/// <remarks>
/// <para>
/// <b>It sets the pattern for the rest of <c>adr/0002</c>'s hot flavour</b>: a bounded box the host
/// supplies, answered every frame, allocation-free, value types only, no strings. Getting it wrong
/// here would be copied nineteen more times.
/// </para>
/// <para>
/// <b>The string rule is the one worth testing rather than trusting.</b> <c>adr/0002</c> is explicit
/// that the real leak vector is not <c>using Godot;</c> but a method that returns a formatted string
/// because a panel wanted one — and an overlay is exactly the caller that would ask for the Layer's
/// name alongside its values.
/// </para>
/// </remarks>
public class LayerQueryTests
{
    [Fact]
    public void It_returns_one_reading_per_Cell_in_the_box()
    {
        MapLayers layers = new(LayerRuleset.Default);
        CellRect box = new(new Cells(4), new Cells(5), new Cells(3), new Cells(2));

        Span<LayerReading> readings = new LayerReading[MapLayers.LayerCellCount(box)];
        int written = layers.LayerCells(box, Layer.IndustrialPollution, readings);

        Assert.Equal(6, written);
        Assert.Equal(new Cells(4), readings[0].East);
        Assert.Equal(new Cells(5), readings[0].North);
        Assert.Equal(new Cells(6), readings[2].East);
        Assert.Equal(new Cells(6), readings[5].North);
    }

    /// <summary>The values are normalised, so a caller never sees kernel units.</summary>
    /// <remarks>
    /// Pre-normalised storage is an internal consequence of exact superposition. A query handing out
    /// raw kernel units would make every consumer responsible for a rounding decision this slice
    /// already made once — and they would each make it differently.
    /// </remarks>
    [Fact]
    public void The_values_it_returns_are_normalised()
    {
        MapLayers layers = new(LayerRuleset.Default);
        layers.EmitPollution(new Cells(30), new Cells(30), 5_000);
        layers.RediffusePollution();

        CellRect box = CellRect.At(new Cells(30), new Cells(30));
        Span<LayerReading> readings = new LayerReading[1];

        Assert.Equal(1, layers.LayerCells(box, Layer.IndustrialPollution, readings));
        Assert.Equal(layers.Pollution(new Cells(30), new Cells(30)), readings[0].Value);
        Assert.NotEqual(layers.Cells.Pollution[layers.Residency.Slot(new Cells(30), new Cells(30))],
            readings[0].Value);
    }

    /// <summary>Every Layer answers, including the ones that are not convolutions.</summary>
    [Fact]
    public void It_reads_all_three_Layers()
    {
        MapLayers layers = new(LayerRuleset.Default);
        Cells east = new(8);
        Cells north = new(9);

        layers.EmitPollution(east, north, 2_000);
        layers.RediffusePollution();
        layers.Seal(east, north, 64);
        layers.SetLandValueTarget(east, north, 500);
        layers.DriftLandValue();

        CellRect box = CellRect.At(east, north);
        Span<LayerReading> readings = new LayerReading[1];

        foreach (Layer layer in Enum.GetValues<Layer>())
        {
            Assert.Equal(1, layers.LayerCells(box, layer, readings));
            Assert.Equal(layers.Value(layer, east, north), readings[0].Value);
        }
    }

    /// <summary>
    /// A box larger than the buffer is truncated and counted, never thrown and never grown.
    /// </summary>
    /// <remarks>
    /// <b>Truncating is the right behaviour for a per-frame query whose box follows a camera</b>, and
    /// growing would mean allocating inside the hot path. A host that wants the whole box sizes with
    /// <see cref="MapLayers.LayerCellCount"/> first, which is why that exists.
    /// </remarks>
    [Fact]
    public void A_buffer_too_small_is_filled_and_the_count_says_so()
    {
        MapLayers layers = new(LayerRuleset.Default);
        CellRect box = new(Cells.Zero, Cells.Zero, new Cells(10), new Cells(10));

        Span<LayerReading> readings = new LayerReading[7];
        int written = layers.LayerCells(box, Layer.IndustrialPollution, readings);

        Assert.Equal(7, written);
        Assert.Equal(100, MapLayers.LayerCellCount(box));
    }

    /// <summary>A box hanging off the map is clamped, not wrapped and not refused.</summary>
    [Fact]
    public void A_box_off_the_edge_is_clamped()
    {
        MapLayers layers = new(LayerRuleset.Default);
        CellRect box = new(new Cells(-3), new Cells(-3), new Cells(5), new Cells(5));

        Span<LayerReading> readings = new LayerReading[MapLayers.LayerCellCount(box)];
        int written = layers.LayerCells(box, Layer.IndustrialPollution, readings);

        Assert.Equal(4, written);
        Assert.Equal(new Cells(0), readings[0].East);
        Assert.Equal(new Cells(0), readings[0].North);
    }

    /// <summary>A box entirely off the map answers nothing rather than throwing.</summary>
    [Fact]
    public void A_box_entirely_off_the_map_returns_nothing()
    {
        MapLayers layers = new(LayerRuleset.Default);
        CellRect box = new(new Cells(-50), new Cells(-50), new Cells(10), new Cells(10));

        Assert.Equal(0, layers.LayerCells(box, Layer.IndustrialPollution, []));
        Assert.Equal(0, MapLayers.LayerCellCount(box));
    }

    /// <summary>
    /// The query allocates nothing. <c>adr/0002</c>'s hot flavour, checked rather than asserted.
    /// </summary>
    /// <remarks>
    /// Measured against the allocated byte count of the calling thread. The buffer is allocated
    /// <em>before</em> the measurement because owning it is the caller's job — the claim is that
    /// answering costs nothing, not that the answer is free to store.
    /// </remarks>
    [Fact]
    public void Answering_the_query_allocates_nothing()
    {
        MapLayers layers = new(LayerRuleset.Default);
        layers.EmitPollution(new Cells(20), new Cells(20), 3_000);
        layers.RediffusePollution();

        CellRect box = new(new Cells(10), new Cells(10), new Cells(20), new Cells(20));
        LayerReading[] readings = new LayerReading[MapLayers.LayerCellCount(box)];

        // Warm the path first: a first call would otherwise measure the JIT rather than the query.
        _ = layers.LayerCells(box, Layer.IndustrialPollution, readings);

        int gen0 = GC.CollectionCount(0);
        int gen1 = GC.CollectionCount(1);
        int gen2 = GC.CollectionCount(2);
        long before = GC.GetAllocatedBytesForCurrentThread();

        for (int i = 0; i < 64; i++)
        {
            _ = layers.LayerCells(box, Layer.IndustrialPollution, readings);
        }

        long after = GC.GetAllocatedBytesForCurrentThread();

        AllocationProbe.Record(
            "LayerQueryTests.Answering_the_query_allocates_nothing",
            after - before,
            GC.CollectionCount(0) - gen0,
            GC.CollectionCount(1) - gen1,
            GC.CollectionCount(2) - gen2);

        Assert.Equal(before, after);
    }

    /// <summary>
    /// Nothing on the query surface returns a string. <c>adr/0002</c>'s actual leak vector.
    /// </summary>
    [Fact]
    public void The_query_surface_returns_no_strings()
    {
        IEnumerable<string> offenders = typeof(MapLayers).GetMethods()
            .Where(method => method.DeclaringType == typeof(MapLayers))
            .Where(method => method.ReturnType == typeof(string))
            .Select(method => method.Name);

        Assert.Empty(offenders);
    }
}
