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
        Zone = _rows.Saved<byte>("zone");

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<Lot> Rows => _rows;

    /// <summary>Position along the east axis, in whole Tiles.</summary>
    public Column<Tiles> East { get; }

    /// <summary>Position along the north axis, in whole Tiles.</summary>
    public Column<Tiles> North { get; }

    /// <summary>Which Zone family the Lot is zoned for. Resolved through the Ruleset.</summary>
    public Column<byte> Zone { get; }

    /// <summary>Allocates a Lot at a position.</summary>
    public Handle<Lot> Create(Tiles east, Tiles north, byte zone)
    {
        Handle<Lot> handle = _rows.Allocate();
        int slot = _rows.Resolve(handle);

        East[slot] = east;
        North[slot] = north;
        Zone[slot] = zone;

        return handle;
    }
}
