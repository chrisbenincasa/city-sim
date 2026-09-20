namespace Borough.Core.Entities;

using Borough.Core.Tables;

/// <summary>Historical subdivision choice and derived geographic permission summaries per block.</summary>
[Table]
public sealed class BlockTable
{
    private readonly Rows<Block> _rows;

    /// <summary>Builds the table at a capacity, and seals its declaration.</summary>
    public BlockTable(int capacity)
    {
        _rows = new Rows<Block>("block", capacity, Buffering.OneCopy);

        LatticeColumn = _rows.Saved<int>("lattice_column", Touch.Cold);
        LatticeRow = _rows.Saved<int>("lattice_row", Touch.Cold);
        Zone = _rows.Derived<ushort>("zone", Touch.Cold);
        Band = _rows.Derived<byte>("band", Touch.Cold);
        Pattern = _rows.Saved<byte>("pattern", Touch.Cold);

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<Block> Rows => _rows;

    /// <summary>The square's column on the Street lattice.</summary>
    /// <remarks>
    /// <b>Saved, and it is what makes the residency rebuildable.</b> The index is a function from a
    /// lattice position to a slot; without the position on the row there would be nothing to rebuild
    /// it from, and the index would have to be saved instead — which is the second-copy shape
    /// <c>adr/0078</c> refuses.
    /// </remarks>
    public Column<int> LatticeColumn { get; }

    /// <summary>The square's row on the Street lattice.</summary>
    public Column<int> LatticeRow { get; }

    /// <summary>Union of geographic uses, for discovery only; never construction authority.</summary>
    public Column<ushort> Zone { get; }

    /// <summary>Uniform geographic intensity band; zero when absent or mixed.</summary>
    public Column<byte> Band { get; }

    /// <summary>
    /// Historical initial-carving pattern, one-based; zero means not yet carved.
    /// Individual Lots retain their own saved geometry after local redevelopment.
    /// </summary>
    public Column<byte> Pattern { get; }
}
