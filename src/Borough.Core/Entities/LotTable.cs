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
/// and the save format that would make a migration necessary arrived in milestone <b>8</b> — this
/// sentence said <em>milestone 10</em> before the renumber. ⚠ <b>It did not bring the migration cost
/// this paragraph anticipated</b>: <c>adr/0086</c> settles that a save has <em>no schema of its own
/// and the field declaration is the format</em>, so a column added here is a column the save learns
/// rather than one it has to be migrated across.
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

    /// <summary>Land where a dwelling may stand. Bit 0, and every Lot the generator ever painted.</summary>
    /// <remarks>
    /// <b>Named here rather than in <see cref="SyntheticCity"/> because two subsystems read it and
    /// only one paints it</b> (<c>adr/0165</c>). The generator assigns the bits; the District
    /// watershed has to know which vacant land is <em>deliberately</em> vacant. A bit index repeated
    /// in two files is one edit away from a watershed that reads commercial land as a hole.
    /// </remarks>
    public const ushort Housing = 1 << 0;

    /// <summary>
    /// Land where a trade's premises may stand, and <b>where a dwelling may not</b>.
    /// </summary>
    /// <remarks>
    /// <b>Exclusive with <see cref="Housing"/></b>, which is <c>CONTEXT.md</c> → Zone's own definition
    /// of a permission set: *"it lists the uses allowed there and forbids every other."* ⚠ <b>A Lot
    /// carrying this and standing vacant is not empty ground</b> — see
    /// <c>Space.DistrictWatershed</c>, which counts it toward settlement height for that reason.
    /// </remarks>
    public const ushort Trade = 1 << 1;

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
        ParcelEast = _rows.Saved<Tiles>("parcel_east");
        ParcelNorth = _rows.Saved<Tiles>("parcel_north");
        ParcelWide = _rows.Saved<Tiles>("parcel_wide");
        ParcelDeep = _rows.Saved<Tiles>("parcel_deep");
        FootprintEast = _rows.Saved<Tiles>("footprint_east");
        FootprintNorth = _rows.Saved<Tiles>("footprint_north");
        FootprintWide = _rows.Saved<Tiles>("footprint_wide");
        FootprintDeep = _rows.Saved<Tiles>("footprint_deep");
        Storeys = _rows.Saved<byte>("storeys");
        Pattern = _rows.Saved<byte>("block_pattern", Touch.Cold);

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<Lot> Rows => _rows;

    /// <summary>
    /// <b>The ground this Lot holds</b> — its parcel's south-west corner and extent, in Tiles.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>A LOT IS AN ADDRESS AND THIS DOES NOT CHANGE THAT.</b> <c>adr/0078</c> refused a
    /// <em>depth key</em>, and there still is not one: the parcel is <b>derived</b>, on the epoch,
    /// from the block's saved pattern and the lattice — the same standing as
    /// <see cref="FrontageSlot"/>, produced by the same carve, and rebuilt from the same saved state.
    /// </para>
    /// <para>
    /// <b><c>plans/0052</c> stage 1, which is <c>plans/0053</c>'s step 5.</b> Before this the ground
    /// under a Building was invented independently in six places — five in the shell and one in the
    /// core — and two of those inventions landed on the same patch, which is <c>plans/0049</c>
    /// <b>F21</b>. ***A partition of a block cannot overlap; five sizings can.***
    /// </para>
    /// <para>
    /// ⚠ <b>An unfronted Lot has no parcel</b> and reads zero on all four. <c>adr/0079</c> keeps such
    /// a Lot and its Building standing with no Address, and ground with no Address on it is ground
    /// this table cannot name — so a zero here means <em>ask the frontage</em> rather than
    /// <em>a Building covering nothing</em>.
    /// </para>
    /// </remarks>
    public Column<Tiles> ParcelEast { get; }

    /// <inheritdoc cref="ParcelEast"/>
    public Column<Tiles> ParcelNorth { get; }

    /// <inheritdoc cref="ParcelEast"/>
    public Column<Tiles> ParcelWide { get; }

    /// <inheritdoc cref="ParcelEast"/>
    public Column<Tiles> ParcelDeep { get; }

    /// <summary>
    /// <b>The ground the Building on this Lot actually covers</b> — its parcel inset by four
    /// setbacks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THIS IS WHAT MEETS THE MAP LAYERS, AND THE PARCEL IS NOT.</b> <c>CONTEXT.md</c> →
    /// Building: <i>"a Building has a footprint (the set of Tiles it covers)"</i> and <i>"interacts
    /// with Map Layers through that footprint"</i>. The parcel is the Lot's <em>holding</em>;
    /// this is the part with a wall on it. ⚠ <b>Sealing was the parcel and was therefore about
    /// TWICE the built ground</b>, while the shell drew the smaller figure — so the simulation and
    /// the picture disagreed about the same quantity, and the picture was the one that was right.
    /// </para>
    /// <para>
    /// <b>Derived on the epoch beside <see cref="ParcelEast"/> and by the same call.</b> The
    /// setbacks come from <c>[lots] setback_tiles</c> and a draw on the <em>parcel's corner</em>, so
    /// the footprint is a property of the ground rather than of the row — see
    /// <c>LotRuleset.Footprint</c>.
    /// </para>
    /// <para>
    /// ⚠ <b>Zero on all four where there is no parcel</b>, the same convention
    /// <see cref="ParcelEast"/> keeps, and it means the same thing: <em>ask the frontage</em>.
    /// </para>
    /// </remarks>
    public Column<Tiles> FootprintEast { get; }

    /// <inheritdoc cref="FootprintEast"/>
    public Column<Tiles> FootprintNorth { get; }

    /// <inheritdoc cref="FootprintEast"/>
    public Column<Tiles> FootprintWide { get; }

    /// <inheritdoc cref="FootprintEast"/>
    public Column<Tiles> FootprintDeep { get; }

    /// <summary>
    /// <b>How many floors a Building here stands</b>, derived from the block's pattern.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>A HEIGHT THE CITY OWNS, WHICH IT HAD NEVER HAD.</b> The shell drew a Building
    /// <c>occupants</c> storeys tall and jittered it — so height was a function of a Ruleset key
    /// about <em>tenancies</em>, invented in the renderer, and every kind that housed four
    /// Households was four storeys everywhere in the world. ***A city drawn from one number is a
    /// city with one skyline.***
    /// </para>
    /// <para>
    /// <b>It is the block's rung plus two, plus a draw of one.</b> The two is the floor — a building
    /// with no upper floor is a shed — and the rung is <c>BlockPatterns.Ladder</c>'s own ordering, so
    /// height rises with density <b>because it is the same quantity</b> and not because anybody
    /// tabulated a height per pattern. ⚠ <b>The draw is what stops a block being a wall</b>: one
    /// storey of variation, on the parcel's corner, so neighbours differ and the ladder still reads
    /// from the air.
    /// </para>
    /// <para>
    /// 🔴 <b>IT IS CAPACITY AND NOT DECORATION.</b> A Building's floor area is its footprint times
    /// this, and how many tenants, jobs and parking spaces it holds all divide that — see
    /// <c>World.FloorTiles</c>. Changing it is a change to the city and not to the picture.
    /// </para>
    /// </remarks>
    public Column<byte> Storeys { get; }

    /// <summary>The <see cref="Space.BlockPattern"/> that carved this Lot.</summary>
    /// <remarks>
    /// <b>Saved beside the parcel and rebuilt beside it on an epoch.</b> Parcel geometry survives a
    /// Ruleset change rather than being silently reinterpreted, and this is part of that geometry.
    /// A Building plan needs to know
    /// whether a broad footprint is a courtyard or a Tower; dimensions alone cannot carry that
    /// distinction, which is what made tall Towers hollow squares.
    /// </remarks>
    public Column<byte> Pattern { get; }

    /// <summary>The decoded pattern; the column stores one-based so zero means not rebuilt.</summary>
    public Space.BlockPattern PatternOf(int slot) =>
        Pattern[slot] == 0
            ? Space.BlockPattern.Detached
            : (Space.BlockPattern)(Pattern[slot] - 1);

    /// <summary>
    /// How much ground a Lot holds, in Tiles — <b>and therefore how much its Building Seals</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THIS REPLACED <c>[[building]] footprint_tiles</c>, WHICH IS DELETED.</b>
    /// <c>adr/0025</c> is unambiguous about which of the two belongs in the design: <em>block
    /// geometry determining parcel size is a physical consequence — it is not a rule at all; it is
    /// arithmetic over what the player drew</em>. ***An authored constant was standing exactly where
    /// the design says arithmetic belongs.***
    /// </para>
    /// <para>
    /// <b>The whole parcel is spent, garden included</b>, which reads
    /// <c>adr/0022</c>'s <em>"Land is a stock the city spends"</em> literally: <b>you cannot farm
    /// somebody's back garden</b>. ⚠ <b>A coverage fraction can arrive later as a multiplier
    /// defaulting to 1</b> — it would be <c>adr/0025</c>'s band, a lever the design already wants —
    /// without this having been wrong.
    /// </para>
    /// <para>
    /// ⚠ <b>It stretches one word in <c>CONTEXT.md</c> → Sealing</b> — <em>"the count of Tiles in a
    /// Cell ever built on"</em> — because a garden is developed rather than built on. That is a
    /// corpus correction and not a design problem, and it is filed.
    /// </para>
    /// </remarks>
    public int ParcelTiles(int slot) => ParcelWide[slot].Raw * ParcelDeep[slot].Raw;

    /// <summary>How many Tiles the Building on this Lot covers. Zero where there is no parcel.</summary>
    public int FootprintTiles(int slot) => FootprintWide[slot].Raw * FootprintDeep[slot].Raw;

    /// <summary>
    /// <b>How much floor a Building here has</b>, in Tiles — its <em>habitable</em> plan on every
    /// storey.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The one quantity every capacity divides.</b> Occupancy, employment and parking are all
    /// floor area over a rate, so they move together when the ground moves and none of them is
    /// authored per kind. ⚠ <b>Zero where there is no parcel</b>, which is a Lot with no Address
    /// (<c>adr/0079</c>) or one no subdivider carved.
    /// </para>
    /// <para>
    /// 🔴 <b>It is not the footprint any more</b> — see <see cref="BuildingPlan"/>. A plan deeper
    /// than daylight reaches keeps a perimeter and loses its middle, which is what stopped a
    /// 256-Tile block housing four hundred people in two Buildings.
    /// </para>
    /// </remarks>
    public int FloorTiles(int slot) => BuildingPlan.FloorTiles(
        PatternOf(slot), FootprintWide[slot].Raw, FootprintDeep[slot].Raw, Storeys[slot]);

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
    /// Allocates a Lot at a position, on a given side of the Street it fronts, standing on
    /// <paramref name="wide"/> × <paramref name="deep"/> Tiles of ground.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The subdivider is the intended caller and the tests are the other one</b> (<c>02 §2.2</c>:
    /// <i>Lots are generated, not painted</i>). It does not set the frontage columns, because it does
    /// not know the Segment — the subdivider does, and writes them at the same site, exactly as
    /// <see cref="World.CreateBuilding"/> calls <see cref="Occupy"/> for the reverse index that
    /// <see cref="World.RebuildDerived"/> also recomputes. <b>Two producers of a derived column is the
    /// established pattern here rather than a hazard</b>: the write site keeps it cheap, the rebuild
    /// keeps it recoverable, and a test that the two agree is what stops them drifting.
    /// </para>
    /// <para>
    /// 🔴 <b>The ground is a parameter because capacity divides it</b> (<c>plans/0053</c>). Until
    /// occupancy derived from floor area, a Lot made here could carry no parcel at all and nothing
    /// noticed — the count lived on the kind. It does not now, so ***a Lot with no ground is a
    /// Building that holds nobody***, and every hand-built fixture would have gone silently empty.
    /// ⚠ <b>The default is ONE Tile on ONE storey rather than none</b>, which is the smallest honest
    /// parcel rather than a convenient one: a Lot exists, so it stands somewhere. A fixture wanting a
    /// Building that holds four says how much ground four takes.
    /// </para>
    /// <para>
    /// ⚠ <b>The footprint is the whole parcel here</b>, where <c>LotRuleset.Footprint</c> would set
    /// it back from the boundary. A setback is a property of the Ruleset in force and this call
    /// takes none — so the honest thing is to seal what was asked for, and a caller wanting a
    /// setback is a caller who should be going through the subdivider.
    /// </para>
    /// </remarks>
    public Handle<Lot> Create(
        Tiles east,
        Tiles north,
        ushort zone,
        StreetSide side = StreetSide.Left,
        Tiles wide = default,
        Tiles deep = default,
        byte storeys = 1)
    {
        Handle<Lot> handle = _rows.Allocate();
        int slot = _rows.Resolve(handle);

        East[slot] = east;
        North[slot] = north;
        Zone[slot] = zone;
        Side[slot] = (byte)side;

        Tiles across = wide.Raw > 0 ? wide : new Tiles(1);
        Tiles along = deep.Raw > 0 ? deep : new Tiles(1);

        ParcelEast[slot] = east;
        ParcelNorth[slot] = north;
        ParcelWide[slot] = across;
        ParcelDeep[slot] = along;

        FootprintEast[slot] = east;
        FootprintNorth[slot] = north;
        FootprintWide[slot] = across;
        FootprintDeep[slot] = along;

        Storeys[slot] = storeys < 1 ? (byte)1 : storeys;

        return handle;
    }
}
