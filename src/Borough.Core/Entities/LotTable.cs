namespace Borough.Core.Entities;

using Borough.Core.Quantities;
using Borough.Core.Tables;

/// <summary>
/// Parcels of land. The first table, and the only one holding no handles.
/// </summary>
/// <remarks>
/// <para>
/// <b>Thin on purpose.</b> Slice 4's job is the table layer, not the schema — enough columns to hash
/// something and to prove create, free and reuse. A wide table now is a wide table to migrate later,
/// and the save format that would make a migration necessary is milestone 10.
/// </para>
/// <para>
/// <b>A Lot does not point back at its Building.</b> The handle runs one way, Building to Lot, which
/// keeps the four tables a strict DAG and lets them be constructed in one order with no wiring pass.
/// The reverse lookup, when something needs it, is a derived index rebuilt from the forward handle —
/// the same treatment as the occupant lists.
/// </para>
/// </remarks>
[Table]
public sealed class LotTable
{
    private readonly Rows<Lot> _rows;

    /// <param name="capacity">Initial slot count. ~225 Lots per 1,000 Citizens, per S4 task 2.</param>
    public LotTable(int capacity)
    {
        _rows = new Rows<Lot>("lot", capacity, Buffering.OneCopy);

        East = _rows.Saved<Tiles>("east");
        North = _rows.Saved<Tiles>("north");
        Zone = _rows.Saved<ushort>("zone");

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<Lot> Rows => _rows;

    /// <summary>Position along the east axis, in whole Tiles.</summary>
    public Column<Tiles> East { get; }

    /// <summary>Position along the north axis, in whole Tiles.</summary>
    public Column<Tiles> North { get; }

    /// <summary>
    /// The Lot's Zone: <b>a permission set, one bit per kind admitted here</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A set rather than a kind, and the distinction is the design's rather than a convenience.</b>
    /// <c>CONTEXT</c> → Zone is <em>"a permission set over land: it lists the uses allowed there and
    /// forbids every other"</em>, and says mixed use needs no machinery because it is a set with more
    /// than one entry. A single enum here would re-introduce the *zone type* that framing exists to
    /// refuse, and would make mixed use something somebody has to add later.
    /// </para>
    /// <para>
    /// <b>It is permission and never instruction</b> (<c>adr/0025</c>). Zoning admits a kind; it does
    /// not summon one, and a bit set over land nothing wants to build on grows nothing. Density is the
    /// intensity cap <em>within</em> a permission rather than a second concept.
    /// </para>
    /// <para>
    /// <b>Sixteen bits, matching <see cref="Input.Command.Zone"/> at full width</b>, which discharges
    /// the narrowing that verb has carried since slice 5 — it authored a set and this column kept a
    /// byte of it. Sixteen is therefore how many kinds can ever be zoned for, against a <c>kind</c>
    /// that is a <see cref="byte"/> everywhere else. The two are deliberately not the same width: a
    /// kind nothing zones for is an ordinary thing — a service Building is *placed* (<c>adr/0032</c>)
    /// — and a seventeenth zonable kind is a widening that should be argued rather than absorbed.
    /// </para>
    /// </remarks>
    public Column<ushort> Zone { get; }

    /// <summary>Allocates a Lot at a position.</summary>
    public Handle<Lot> Create(Tiles east, Tiles north, ushort zone)
    {
        Handle<Lot> handle = _rows.Allocate();
        int slot = _rows.Resolve(handle);

        East[slot] = east;
        North[slot] = north;
        Zone[slot] = zone;

        return handle;
    }
}
