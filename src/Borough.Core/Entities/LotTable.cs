namespace Borough.Core.Entities;

using Borough.Core.Quantities;
using Borough.Core.Space;
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
    /// <summary>
    /// How many kinds a Zone can ever admit — the width of <see cref="Zone"/> in bits.
    /// </summary>
    /// <remarks>
    /// <b>Declared here because this is where the width is decided</b>, and read by the Ruleset loader
    /// so that *a Zone Rule naming a permission bit no <c>zone</c> verb can paint* is refused against
    /// the column rather than against a number copied into the parser. A constant repeated in two
    /// projects is one edit away from a Ruleset that loads clean and paints nothing.
    /// </remarks>
    public const int ZoneBits = 16;

    private readonly Rows<Lot> _rows;

    /// <param name="capacity">Initial slot count. ~225 Lots per 1,000 Citizens, per S4 task 2.</param>
    public LotTable(int capacity)
    {
        _rows = new Rows<Lot>("lot", capacity, Buffering.OneCopy);

        East = _rows.Saved<Tiles>("east");
        North = _rows.Saved<Tiles>("north");
        Zone = _rows.Saved<ushort>("zone");
        Side = _rows.Saved<byte>("side");
        BuildingSlot = _rows.Derived<int>("building_slot");
        FrontageSlot = _rows.Derived<int>("frontage_slot");
        FrontageOffset = _rows.Derived<Tiles>("frontage_offset");

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

    /// <summary>
    /// Which side of its Street this Lot sits on — <b>the one bit <c>adr/0074</c> puts on the place
    /// rather than in the graph</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Saved, and it is the only part of the Lot's Address that is.</b> The Segment and the offset
    /// are recoverable from <see cref="East"/> and <see cref="North"/> against the Street lattice, and
    /// a side is not: a point on a line is on both sides of it. So this is exactly the residue —
    /// <c>adr/0074</c>'s <i>"one saved bit on that place"</i>, arrived at from the other direction.
    /// </para>
    /// <para>
    /// <b>Left or right of the Segment's A→B direction</b> (<see cref="Space.StreetSide"/>), which the
    /// endpoint columns already fix, so no geometry is needed to interpret it and the simulation still
    /// never sees a spline.
    /// </para>
    /// </remarks>
    public Column<byte> Side { get; }

    /// <summary>
    /// The Segment this Lot fronts, as a slot index <b>plus one</b> — zero meaning no frontage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>Derived</c>, on the Epoch</b> (<c>adr/0078</c>). A Lot no more stores its frontage than
    /// an Arc stores its cost, because both are functions of the Segments — and the edit that would
    /// not reach a stored copy is <b>the player bulldozing the Street</b>, which is the one edit this
    /// slice exists to make possible.
    /// </para>
    /// <para>
    /// <b>Plus one, for <see cref="BuildingSlot"/>'s reason exactly.</b> A freshly allocated or freed
    /// row is zero-filled, so a <c>-1</c> sentinel would make every unfronted Lot read as fronting
    /// <em>Segment slot 0</em> — the first Street in the city, silently claimed across the map, with
    /// every hash moving and every test passing. Read it through <see cref="AddressOf"/> rather than
    /// directly; the encoding is not meant to travel.
    /// </para>
    /// </remarks>
    public Column<int> FrontageSlot { get; }

    /// <summary>How far along its Segment this Lot sits, from the A endpoint.</summary>
    public Column<Tiles> FrontageOffset { get; }

    /// <summary>Whether nothing stands here — <c>02 §2.2</c>'s other state.</summary>
    public bool IsVacant(int slot) => BuildingSlot[slot] == 0;

    /// <summary>Whether this Lot touches a Street it can take access from.</summary>
    public bool HasFrontage(int slot) => FrontageSlot[slot] != 0;

    /// <summary>
    /// This Lot's Address — <b>and therefore its Building's Access Point</b>, which is what
    /// <c>CONTEXT.md</c> → Access Point means by <i>"where a Building meets a network"</i>.
    /// </summary>
    /// <remarks>
    /// <b><see cref="Address.None"/> where the Lot has no frontage</b>, which is <c>adr/0079</c>'s
    /// requirement and the state a Building reaches when its last Street is bulldozed. It is a value
    /// and not a null precisely so that milestone 5b reads it and reports <em>no route found</em>
    /// rather than dereferencing something.
    /// </remarks>
    public Address AddressOf(int slot) =>
        FrontageSlot[slot] == 0
            ? Address.None
            : Address.On(FrontageSlot[slot] - 1, FrontageOffset[slot], (StreetSide)Side[slot]);

    /// <summary>
    /// The slot of the Building on this Lot, or <see cref="Rows.NoSlot"/> when it is vacant.
    /// </summary>
    /// <remarks>Vacant decodes to <see cref="Rows.NoSlot"/> on its own: stored zero, minus one.</remarks>
    public int BuildingOn(int slot) => BuildingSlot[slot] - 1;

    /// <summary>Records that a Building now stands here.</summary>
    public void Occupy(int slot, int buildingSlot) => BuildingSlot[slot] = buildingSlot + 1;

    /// <summary>Records that the Lot is clear again.</summary>
    public void Vacate(int slot) => BuildingSlot[slot] = 0;

    /// <summary>
    /// Allocates a Lot at a position, on a given side of the Street it fronts.
    /// </summary>
    /// <remarks>
    /// <b>The subdivider is the intended caller and the tests are the other one</b> (<c>02 §2.2</c>:
    /// <i>Lots are generated, not painted</i>). It does not set the frontage columns, because it does
    /// not know the Segment — the subdivider does, and writes them at the same site, exactly as
    /// <see cref="World.CreateBuilding"/> calls <see cref="Occupy"/> for the reverse index that
    /// <see cref="World.RebuildDerived"/> also recomputes. <b>Two producers of a derived column is the
    /// established pattern here rather than a hazard</b>: the write site keeps it cheap, the rebuild
    /// keeps it recoverable, and a test that the two agree is what stops them drifting.
    /// </remarks>
    public Handle<Lot> Create(Tiles east, Tiles north, ushort zone, StreetSide side = StreetSide.Left)
    {
        Handle<Lot> handle = _rows.Allocate();
        int slot = _rows.Resolve(handle);

        East[slot] = east;
        North[slot] = north;
        Zone[slot] = zone;
        Side[slot] = (byte)side;

        return handle;
    }
}
