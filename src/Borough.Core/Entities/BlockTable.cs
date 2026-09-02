namespace Borough.Core.Entities;

using Borough.Core.Tables;

/// <summary>
/// One row per lattice square the player has done something to — its Zone, its density band, and
/// <b>the pattern it was carved with</b>.
/// </summary>
/// <remarks>
/// <para>
/// <b>It exists because five different block subdivisions were proposed in one sitting and every one
/// of them turned into a world constant</b> (<c>plans/0053</c>). It was not the patterns. ***A pattern
/// with nowhere to live becomes a default*** — and before this table a block was a
/// <c>(column, row)</c> index into <see cref="Space.StreetGrid"/> with no state whatsoever, so there
/// was nowhere for a per-block decision to be recorded.
/// </para>
/// <para>
/// <b>It also discharges a limitation <c>LotSubdivider.Relot</c> names in its own remarks.</b> A
/// block's Zone was read back off <em>whichever Lots survived on it</em>, so a block that lost every
/// Lot had forgotten it was ever zoned and a Street run back through it yielded nothing until the
/// player zoned again. <see cref="Zone"/> here is where that fact belongs.
/// </para>
/// <para>
/// ⚠ <b><see cref="LotTable.Zone"/> is NOT removed and this is not yet its replacement.</b> The Lot's
/// Zone is what every Zone Rule reads (<c>ZoneRuleEngine</c>'s admission test), and moving that is a
/// separate change with its own hash movement. What this column does today is <b>remember</b>, so that
/// re-subdivision has a source that outlives the Lots.
/// </para>
/// <para>
/// <b>The index into this table is a <see cref="Space.BlockResidency"/> rather than a column</b>,
/// because the lattice has <c>Span²</c> squares and a city occupies a handful of them. See that type
/// for why it is the one residency in the project sized by a tuning key rather than a design constant.
/// </para>
/// </remarks>
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
        Zone = _rows.Saved<ushort>("zone", Touch.Cold);
        Band = _rows.Saved<byte>("band", Touch.Cold);
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

    /// <summary>The Zone the player painted here — <b>a permission set, one bit per kind admitted</b>.</summary>
    /// <remarks>
    /// <b>The same sixteen-bit permission set <see cref="LotTable.Zone"/> carries</b>, and deliberately
    /// the same width: a block whose Lots are gone must be able to hand back exactly what it was
    /// painted with, and a narrower field here would silently drop bits on the way through.
    /// </remarks>
    public Column<ushort> Zone { get; }

    /// <summary>
    /// The density band — <c>adr/0025</c>'s <b>cap</b>, which is permission and never instruction.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>adr/0025</c>: <em>"the player sets a ceiling, never a floor"</em></b>, and <em>"a high band
    /// on land nothing wants to build on grows nothing, and that is information rather than a bug"</em>.
    /// </para>
    /// <para>
    /// 🔴 ⚠ <b>NOTHING MAY DERIVE THIS FROM CONDITIONS.</b> <c>adr/0025</c> rejects the road-derived
    /// cap specifically, because <em>"a road-derived cap would pre-empt the lesson the engine exists to
    /// teach"</em> — and deriving it from land value instead of road tier does not change the
    /// objection. A generator painting an initial value is the same act as a generator zoning land and
    /// is not a rule reading conditions; a <em>Rule</em> writing this column would be the rejected
    /// design arriving by the back door.
    /// </para>
    /// </remarks>
    public Column<byte> Band { get; }

    /// <summary>
    /// The subdivision pattern this block was carved with.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>SAVED, AND IT IS THE ONE COLUMN HERE WHOSE DISPOSITION IS ARGUED RATHER THAN OBVIOUS.</b>
    /// <c>adr/0078</c>'s rule is that a fact computable from other saved facts must not be stored beside
    /// them — which is why a Lot's <em>parcel</em> is derived. ***A carving decision is not so
    /// computable: the conditions that produced it are gone.*** The block was carved when land value
    /// here was low; it is not low now; and the pattern cannot be recomputed from a world that has
    /// moved on.
    /// </para>
    /// <para>
    /// <b>That is the whole of how the city stops looking uniform</b>, and it needs no randomness:
    /// selection happens once, from local conditions, and <c>02 §2.2</c>'s existing preservation rule —
    /// <em>"only vacant land re-parcels"</em> — freezes it while the block is occupied. ***A city that
    /// grew over time into changing conditions looks like one.***
    /// </para>
    /// </remarks>
    public Column<byte> Pattern { get; }
}
