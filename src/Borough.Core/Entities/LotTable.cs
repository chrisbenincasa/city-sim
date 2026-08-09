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
        BuildingSlot = _rows.Derived<int>("building_slot");

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

    /// <summary>
    /// Which Building stands on this Lot, as a slot index <b>plus one</b> — zero meaning vacant.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The relation exists on <see cref="BuildingTable.Lot"/> and this is its reverse index.</b>
    /// <em>Is this Lot vacant</em> is the first question a Zone Rule asks and the one it asks most, and
    /// without this column answering it means scanning Buildings — which would make the sweep
    /// <c>O(Buildings)</c> per sample and destroy the constant-cost property slice 10's tripwire exists
    /// to measure, before the tripwire could measure it.
    /// </para>
    /// <para>
    /// <b><c>Derived</c> rather than <c>Saved</c>, so it is outside the State Hash and outside the
    /// save.</b> It is recoverable from <see cref="BuildingTable.Lot"/> in one pass and is rebuilt by
    /// <see cref="World.RebuildDerived"/>. Storing it would give the same fact two homes that could
    /// disagree, and the hash would then fold a disagreement as though it were state.
    /// </para>
    /// <para>
    /// <b>Plus one, for the reason <see cref="IndexList"/> gives at length.</b> Slots are zero-filled
    /// when a table grows and zeroed again when a row is freed, so a sentinel of <c>-1</c> would make a
    /// freshly allocated Lot read as holding <em>Building slot 0</em> — the first Building in the city,
    /// silently claimed by every new Lot. Use <see cref="IsVacant"/> and <see cref="BuildingOn"/>
    /// rather than reading this directly; the encoding is not meant to travel.
    /// </para>
    /// <para>
    /// <b>A slot rather than a <c>DerivedHandle</c>, and the reason is construction order rather than
    /// preference.</b> <see cref="BuildingTable"/> already takes this table to address its
    /// <see cref="BuildingTable.Lot"/> handles, so a handle column pointing back would be a cycle. The
    /// four reverse indices that predate this one are all raw slots for the same reason, and a derived
    /// index carries no generation risk anyway: it is rebuilt from the truth it mirrors.
    /// </para>
    /// </remarks>
    public Column<int> BuildingSlot { get; }

    /// <summary>Whether nothing stands here — <c>02 §2.2</c>'s other state.</summary>
    public bool IsVacant(int slot) => BuildingSlot[slot] == 0;

    /// <summary>
    /// The slot of the Building on this Lot, or <see cref="Rows.NoSlot"/> when it is vacant.
    /// </summary>
    /// <remarks>Vacant decodes to <see cref="Rows.NoSlot"/> on its own: stored zero, minus one.</remarks>
    public int BuildingOn(int slot) => BuildingSlot[slot] - 1;

    /// <summary>Records that a Building now stands here.</summary>
    public void Occupy(int slot, int buildingSlot) => BuildingSlot[slot] = buildingSlot + 1;

    /// <summary>Records that the Lot is clear again.</summary>
    public void Vacate(int slot) => BuildingSlot[slot] = 0;

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
