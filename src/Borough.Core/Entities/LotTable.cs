namespace Borough.Core.Entities;

using Borough.Core.Arithmetic;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;

/// <summary>
/// Parcels of land.
/// </summary>
/// <remarks>
/// <para>
/// <b>A Lot does not point back at its Building.</b> The handle runs one way, Building to Lot, which
/// keeps the four tables a strict DAG and lets them be constructed in one order with no wiring pass.
/// The reverse lookup, when something needs it, is a derived index rebuilt from the forward handle —
/// the same treatment as the occupant lists.
/// </para>
/// <para>
/// <b>A Lot's frontage is saved</b> (<c>adr/0174</c>) — the Segment it fronts, how far along it sits
/// and which side of it. <see cref="Space.Frontage"/> writes it at creation and migrates it across
/// Street edits, and the handle is <see cref="Reference.Severable"/> because a bulldozed Street
/// leaves the Lot standing with no Address (<c>adr/0079</c>).
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
    /// <param name="segments">The table this one's <see cref="FrontageSegment"/> handles address.</param>
    public LotTable(int capacity, RoadSegmentTable segments)
    {
        ArgumentNullException.ThrowIfNull(segments);

        _rows = new Rows<Lot>("lot", capacity, Buffering.OneCopy);

        East = _rows.Saved<Tiles>("east");
        North = _rows.Saved<Tiles>("north");
        Zone = _rows.Derived<ushort>("zone");
        Side = _rows.Saved<byte>("side");
        BuildingSlot = _rows.Derived<int>("building_slot");

        // CarParkTable.WhereSegment's three-column pattern. An Address holds a Segment *slot*, and a
        // saved slot index folds the city's whole demolition history into the State Hash, so two runs
        // building the same city would disagree; Address.cs says so at length.
        FrontageSegment = _rows.SavedHandle(
            "frontage_segment", segments.Rows, Touch.Wake, Reference.Severable);
        FrontageOffset = _rows.Saved<Tiles>("frontage_offset");
        ParcelEastQ16 = _rows.Saved<int>("parcel_east_q16");
        ParcelNorthQ16 = _rows.Saved<int>("parcel_north_q16");
        AxisEastQ16 = _rows.Saved<int>("axis_east_q16");
        AxisNorthQ16 = _rows.Saved<int>("axis_north_q16");
        ParcelWide = _rows.Saved<Tiles>("parcel_wide");
        ParcelDeep = _rows.Saved<Tiles>("parcel_deep");
        FootprintEastQ16 = _rows.Saved<int>("footprint_east_q16");
        FootprintNorthQ16 = _rows.Saved<int>("footprint_north_q16");
        FootprintWide = _rows.Saved<Tiles>("footprint_wide");
        FootprintDeep = _rows.Saved<Tiles>("footprint_deep");
        Storeys = _rows.Saved<byte>("storeys");
        Pattern = _rows.Saved<byte>("block_pattern", Touch.Cold);
        PodiumStoreys = _rows.Derived<byte>("podium_storeys", Touch.Cold);

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<Lot> Rows => _rows;

    /// <summary>The saved parcel corner in Q16.16 Tiles. It survives loss of frontage.</summary>
    public Column<int> ParcelEastQ16 { get; }

    /// <inheritdoc cref="ParcelEastQ16"/>
    public Column<int> ParcelNorthQ16 { get; }

    /// <summary>The saved first unit axis in Q16.16, shared by parcel and footprint.</summary>
    public Column<int> AxisEastQ16 { get; }

    /// <inheritdoc cref="AxisEastQ16"/>
    public Column<int> AxisNorthQ16 { get; }

    /// <summary>The parcel's extent along its first axis, in Tiles.</summary>
    public Column<Tiles> ParcelWide { get; }

    /// <summary>The parcel's extent along its second axis, in Tiles.</summary>
    public Column<Tiles> ParcelDeep { get; }

    /// <summary>The saved footprint corner in Q16.16 Tiles, inset by the carve's setbacks.</summary>
    public Column<int> FootprintEastQ16 { get; }

    /// <inheritdoc cref="FootprintEastQ16"/>
    public Column<int> FootprintNorthQ16 { get; }

    /// <summary>The footprint's extent along the parcel's first axis, in Tiles.</summary>
    public Column<Tiles> FootprintWide { get; }

    /// <summary>The footprint's extent along the parcel's second axis, in Tiles.</summary>
    public Column<Tiles> FootprintDeep { get; }

    public OrientedRectangle Parcel(int slot) => new(ParcelEastQ16[slot], ParcelNorthQ16[slot],
        AxisEastQ16[slot], AxisNorthQ16[slot], ParcelWide[slot].Raw, ParcelDeep[slot].Raw);

    public OrientedRectangle Footprint(int slot) => new(FootprintEastQ16[slot], FootprintNorthQ16[slot],
        AxisEastQ16[slot], AxisNorthQ16[slot], FootprintWide[slot].Raw, FootprintDeep[slot].Raw);

    public LandRectangle ParcelBounds(int slot) => Parcel(slot).Bounds;

    public LandRectangle FootprintBounds(int slot) => Footprint(slot).Bounds;

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

    /// <summary>The storeys a Tower's podium stands on this Lot, of its <see cref="Storeys"/>.</summary>
    /// <remarks>
    /// <b>Derived from the saved parcel corner</b> by <see cref="Rules.LotRuleset.PodiumOn"/>, so it
    /// needs no saved bytes and a Ruleset without a podium range hashes as before. Every Lot holds
    /// one and only a Tower reads it. <see cref="FloorTiles"/> reads it, so it is rebuilt before
    /// anything that reads floor.
    /// </remarks>
    public Column<byte> PodiumStoreys { get; }

    /// <summary>Draw every live Lot's podium from its saved parcel corner.</summary>
    public void RebuildPodiums(Rules.LotRuleset rules, Determinism.WorldKey key)
    {
        for (int slot = 0; slot < _rows.SlotCount; slot++)
        {
            PodiumStoreys[slot] = _rows.IsLive(slot) ? rules.PodiumOn(key, new Tiles(ParcelBounds(slot).X), new Tiles(ParcelBounds(slot).Y)) : (byte)0;
        }
    }

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
        PatternOf(slot), FootprintWide[slot].Raw, FootprintDeep[slot].Raw, Storeys[slot], PodiumStoreys[slot]);

    /// <summary>Position along the east axis, in whole Tiles.</summary>
    public Column<Tiles> East { get; }

    /// <summary>Position along the north axis, in whole Tiles.</summary>
    public Column<Tiles> North { get; }

    /// <summary>
    /// Intersection of geographic use permissions over the saved parcel, rebuilt from land paint.
    /// A discovery summary, not construction authority or standing-Building eligibility.
    /// </summary>
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
    /// <b>Saved, with the rest of the Lot's Address</b> — <c>adr/0074</c>'s <i>"one saved bit on that
    /// place"</i>. A point on a line is on both sides of it, so no geometry recovers this one even
    /// where the Segment and the offset could be found again.
    /// </para>
    /// <para>
    /// <b>Left or right of the Segment's A→B direction</b> (<see cref="Space.StreetSide"/>), which the
    /// endpoint columns already fix, so no geometry is needed to interpret it and the simulation still
    /// never sees a spline.
    /// </para>
    /// </remarks>
    public Column<byte> Side { get; }

    /// <summary>
    /// The Segment this Lot fronts. Unset where it has no frontage, and severed where its Street has
    /// been bulldozed.
    /// </summary>
    /// <remarks>
    /// <b>Saved</b> (<c>adr/0174</c>). A freeform Street has no lattice line to run the derivation
    /// backwards from, and a nearest-Segment search is ambiguous at corners and on curves — so the
    /// Lot holds the contact and every Street edit migrates the Lots it touches. Read it through
    /// <see cref="FrontageOn"/> or <see cref="AddressOf"/>, which resolve the handle; write it
    /// through <see cref="Front"/> and <see cref="Unfront"/>.
    /// </remarks>
    public HandleColumn<RoadSegment> FrontageSegment { get; }

    /// <summary>How far along <see cref="FrontageSegment"/> this Lot sits, from its A endpoint.</summary>
    public Column<Tiles> FrontageOffset { get; }

    /// <summary>Whether nothing stands here — <c>02 §2.2</c>'s other state.</summary>
    public bool IsVacant(int slot) => BuildingSlot[slot] == 0;

    /// <summary>
    /// The slot of the Segment this Lot fronts, or <see cref="Rows.NoSlot"/> where it has none.
    /// </summary>
    /// <remarks>
    /// <b>A severed handle reads as no frontage</b>, which is the one place a bulldozed Street becomes
    /// <c>adr/0079</c>'s named absence rather than a stale reference.
    /// </remarks>
    public int FrontageOn(int slot) =>
        FrontageSegment.Target.TryResolve(FrontageSegment[slot], out int segment) ? segment : Tables.Rows.NoSlot;

    /// <summary>Whether this Lot touches a Street it can take access from.</summary>
    public bool HasFrontage(int slot) => FrontageOn(slot) != Tables.Rows.NoSlot;

    /// <summary>Records which Segment this Lot fronts and where along it.</summary>
    /// <remarks>
    /// <b>The side is set at <see cref="Create"/> and is not part of this.</b> A Lot does not change
    /// which side of a Street it stands on, not even when a split moves it to another Segment, because
    /// both parts of a split run in the original's A→B direction.
    /// </remarks>
    public void Front(int slot, Handle<RoadSegment> segment, Tiles offset)
    {
        FrontageSegment[slot] = segment;
        FrontageOffset[slot] = offset;
    }

    /// <summary>Records that this Lot fronts nothing. It keeps its ground and its Building.</summary>
    public void Unfront(int slot)
    {
        FrontageSegment[slot] = default;
        FrontageOffset[slot] = Tiles.Zero;
    }

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
    public Address AddressOf(int slot)
    {
        int segment = FrontageOn(slot);

        return segment == Tables.Rows.NoSlot
            ? Address.None
            : Address.On(segment, FrontageOffset[slot], (StreetSide)Side[slot]);
    }

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
    /// <i>Lots are generated, not painted</i>). ⚠ <b>It leaves the Lot unfronted</b>, because it does
    /// not know the Segment — the caller that does calls <see cref="Front"/> at the same site, and a
    /// Lot that never gets one stands on ground no Street reaches.
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

        ParcelEastQ16[slot] = Fixed.FromInt(east.Raw);
        ParcelNorthQ16[slot] = Fixed.FromInt(north.Raw);
        AxisEastQ16[slot] = Fixed.One;
        AxisNorthQ16[slot] = 0;
        ParcelWide[slot] = across;
        ParcelDeep[slot] = along;

        FootprintEastQ16[slot] = Fixed.FromInt(east.Raw);
        FootprintNorthQ16[slot] = Fixed.FromInt(north.Raw);
        FootprintWide[slot] = across;
        FootprintDeep[slot] = along;

        Storeys[slot] = storeys < 1 ? (byte)1 : storeys;

        return handle;
    }
}
