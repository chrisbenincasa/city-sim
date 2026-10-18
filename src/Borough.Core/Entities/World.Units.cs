using Borough.Core.Quantities;
using Borough.Core.Tables;

namespace Borough.Core.Entities;

public sealed partial class World
{
    /// <summary>Whether a Building has a Unit no Business holds.</summary>
    public bool HasVacantUnit(int buildingSlot) => VacantUnit(buildingSlot) != Rows.NoSlot;

    /// <summary>
    /// The floor of the Unit a Business holds, in Tiles, or zero while it holds none.
    /// </summary>
    public int UnitFloorOf(int businessSlot) =>
        businessSlot >= 0
        && Businesses.Rows.IsLive(businessSlot)
        && Units.Rows.TryResolve(Businesses.Unit[businessSlot], out int unit)
            ? Units.Floor[unit]
            : 0;

    /// <summary>
    /// What a tenancy of <paramref name="floorTiles"/> Tiles in a Building of
    /// <paramref name="kind"/> on <paramref name="lotSlot"/> pays a Day: the kind's
    /// <c>rent_per_floor_tile</c> times that floor, times one plus the land value at the Lot.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The one place rent is priced</b> (<c>plans/money-circulation.md</c> L7). Placement, the
    /// choice model and housing-need matching all come through here, so a dwelling's rent is the same
    /// number whoever asks.
    /// </para>
    /// <para>
    /// <b>Land value adjusts the rate rather than being it</b>, which is
    /// <see cref="DemolitionPrice"/>'s arithmetic and its reasoning. Amenity is not built, so inland
    /// ground sits at or just below zero and land value can only discount rent today. Land value of
    /// minus one or below prices the tenancy at zero.
    /// </para>
    /// <para>
    /// <b>It is a price and not a charge.</b> Placement and the choice model read it to compare
    /// dwellings; what a standing tenant actually pays each Day is <see cref="ResidentRent"/> and
    /// <see cref="BusinessRent"/> through <c>RentEngine</c>, which charge it only where the Ruleset
    /// states <c>[rent]</c>.
    /// </para>
    /// </remarks>
    public Money TenancyRent(byte kind, int lotSlot, int floorTiles)
    {
        // A derelict Building keeps its tenants after a reload that dropped its kind, and
        // reconsideration still prices what they live in. An undeclared kind has no rate.
        if (!Rules.Declares(kind))
        {
            return Money.Zero;
        }

        long rate = Rules.Kind(kind).RentPerFloorTile.Raw;

        if (rate <= 0 || floorTiles <= 0 || lotSlot < 0 || !Lots.Rows.IsLive(lotSlot))
        {
            return Money.Zero;
        }

        long factor = (long)Arithmetic.Fixed.One
            + Layers.LandValue(
                Space.CellGrid.ToCells(Lots.East[lotSlot]), Space.CellGrid.ToCells(Lots.North[lotSlot]));

        return factor <= 0
            ? Money.Zero
            : new Money(Arithmetic.IntegerMath.MulDivFloor(factor * floorTiles, rate, Arithmetic.Fixed.One));
    }

    /// <summary>
    /// <inheritdoc cref="TenancyRent(byte, int, int)" path="/summary"/>
    /// </summary>
    /// <remarks>
    /// Zero where the Building's Lot is gone, as <see cref="FloorTilesOf"/> is — a Building with no
    /// ground under it stands on no land value either.
    /// </remarks>
    public Money TenancyRent(int buildingSlot, int floorTiles) =>
        buildingSlot >= 0
        && Buildings.Rows.IsLive(buildingSlot)
        && Lots.Rows.TryResolve(Buildings.Lot[buildingSlot], out int lotSlot)
            ? TenancyRent(Buildings.Kind[buildingSlot], lotSlot, floorTiles)
            : Money.Zero;

    /// <summary>
    /// A Household tenancy's floor in <paramref name="buildingSlot"/>, in Tiles: the Building's floor
    /// over the tenancy count its kind declares.
    /// </summary>
    /// <remarks>
    /// <b>The declared count rather than the tenants standing there now</b>, so a neighbour moving out
    /// does not reprice the floor a family lives on — and so a Household and a Business holding the
    /// same floor in the same Building pay the same, which is what
    /// <c>ShapeEqualUnits</c> gives a Unit from the same division.
    /// </remarks>
    public int HouseholdFloorOf(int buildingSlot) =>
        buildingSlot >= 0
        && Buildings.Rows.IsLive(buildingSlot)
        && TryDeclaredOccupancy(Buildings.Kind[buildingSlot], buildingSlot, out int tenancies)
        && tenancies > 0
            ? Arithmetic.IntegerMath.FloorDiv(FloorTilesOf(buildingSlot), tenancies)
            : 0;

    /// <summary>
    /// A Household tenancy's floor, in Tiles, in the dwelling a Zone Rule would raise on
    /// <paramref name="lotSlot"/> — the Lot's floor over the tenancies that floor divides into.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Somebody weighing vacant land has no Building to measure</b>, so this measures the ground.
    /// It takes the ordinary equal division and not a trade form's geometry, which is what
    /// <c>HousingKindFor</c> admits anyway.
    /// </para>
    /// <para>
    /// ⚠ <b>An estimate, not the price the dwelling will charge.</b> It divides the Lot's carve
    /// geometry, which is the footprint and storeys the subdivider sealed. A <c>[[housing_form]]</c>
    /// raised here can measure differently, and then the standing Building's floor is what
    /// <see cref="HouseholdFloorOf"/> reads.
    /// </para>
    /// </remarks>
    public int HouseholdFloorOnLot(int lotSlot)
    {
        if (lotSlot < 0 || !Lots.Rows.IsLive(lotSlot))
        {
            return 0;
        }

        int floor = Lots.FloorTiles(lotSlot);
        int tenancies = Core.Rules.CapacityRuleset.Holds(floor, Rules.Capacity.FloorTilesPerOccupant);

        return tenancies > 0 ? Arithmetic.IntegerMath.FloorDiv(floor, tenancies) : 0;
    }

    /// <summary>What a Household tenancy in <paramref name="buildingSlot"/> pays a Day.</summary>
    /// <remarks>
    /// <b>The same figure for a resident and for somebody only considering the dwelling</b>, because
    /// the share divides by the declared count and not by who is in it. What placement compares is
    /// therefore what the tenancy costs after the move.
    /// </remarks>
    public Money HouseholdRent(int buildingSlot) =>
        TenancyRent(buildingSlot, HouseholdFloorOf(buildingSlot));

    /// <summary>
    /// What the Household in <paramref name="householdSlot"/> is charged a Day for its home, which is
    /// zero where it holds the ground under it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A resident Landowner pays none</b> (<c>plans/money-circulation.md</c> L7). Rent is a
    /// payment from a tenant to the Lot's Landowner, and a Household paying itself would move money
    /// out of one Bin and back into the same one — an entry that reads as a market in land while
    /// being nothing of the kind.
    /// </para>
    /// <para>
    /// ⚠ <b>The exemption is per tenancy and not per Building.</b> A Landowner living in one tenancy
    /// of its own block pays nothing for that one and collects from every other Household in it, so
    /// <see cref="HouseholdRent"/> stays the <em>price</em> of a tenancy there and this is what one
    /// named resident is charged.
    /// </para>
    /// </remarks>
    public Money ResidentRent(int householdSlot)
    {
        if (householdSlot < 0
            || !Households.Rows.IsLive(householdSlot)
            || !Buildings.Rows.TryResolve(Households.Dwelling[householdSlot], out int buildingSlot)
            || !Lots.Rows.TryResolve(Buildings.Lot[buildingSlot], out int lotSlot))
        {
            return Money.Zero;
        }

        return Lots.LandownerOn(lotSlot) == householdSlot
            ? Money.Zero
            : HouseholdRent(buildingSlot);
    }

    /// <summary>What the Business in <paramref name="businessSlot"/> pays a Day for its Unit.</summary>
    public Money BusinessRent(int businessSlot) =>
        businessSlot >= 0
        && Businesses.Rows.IsLive(businessSlot)
        && Buildings.Rows.TryResolve(Businesses.Building[businessSlot], out int buildingSlot)
            ? TenancyRent(buildingSlot, UnitFloorOf(businessSlot))
            : Money.Zero;

    private int VacantUnit(int buildingSlot)
    {
        foreach (int unit in BuildingUnits.Walk(buildingSlot))
        {
            if (Units.IsVacant(unit))
            {
                return unit;
            }
        }

        return Rows.NoSlot;
    }

    /// <summary>Whether a Building stands on a car-park centre's Lot.</summary>
    public bool IsTradeCentre(int buildingSlot) =>
        buildingSlot >= 0
        && Buildings.Rows.IsLive(buildingSlot)
        && Lots.Rows.TryResolve(Buildings.Lot[buildingSlot], out int lotSlot)
        && Lots.PatternOf(lotSlot) == Space.BlockPattern.CarParkCentre;

    /// <summary>
    /// Whether a Building is a shop-house, on a shop-house parade or off a high-street block's south face.
    /// </summary>
    public bool IsShopHouse(int buildingSlot) =>
        buildingSlot >= 0
        && Buildings.Rows.IsLive(buildingSlot)
        && Lots.Rows.TryResolve(Buildings.Lot[buildingSlot], out int lotSlot)
        && (Lots.PatternOf(lotSlot) == Space.BlockPattern.ShopHouseParade
            || Lots.PatternOf(lotSlot) == Space.BlockPattern.HighStreetBlock && FaceOf(lotSlot) != Space.BlockFace.South);

    /// <summary>Whether a Building is a high-street block's department store, on its south face.</summary>
    public bool IsDepartmentStore(int buildingSlot) =>
        buildingSlot >= 0
        && Buildings.Rows.IsLive(buildingSlot)
        && Lots.Rows.TryResolve(Buildings.Lot[buildingSlot], out int lotSlot)
        && Lots.PatternOf(lotSlot) == Space.BlockPattern.HighStreetBlock
        && FaceOf(lotSlot) == Space.BlockFace.South;

    private Space.BlockFace FaceOf(int lotSlot)
    {
        Space.Frontage.BlockOf(
            Roads.Streets, Lots.East[lotSlot], Lots.North[lotSlot], (Space.StreetSide)Lots.Side[lotSlot],
            out _, out _, out Space.BlockFace face);

        return face;
    }

    /// <summary>Whether a Building stands on a town supermarket's Lot, with either parking.</summary>
    public bool IsSupermarket(int buildingSlot) =>
        buildingSlot >= 0
        && Buildings.Rows.IsLive(buildingSlot)
        && Lots.Rows.TryResolve(Buildings.Lot[buildingSlot], out int lotSlot)
        && Space.BlockPatterns.IsSupermarket(Lots.PatternOf(lotSlot));

    /// <summary>Whether a Building stands on a pad beside a car-park centre.</summary>
    public bool IsPadSite(int buildingSlot) =>
        buildingSlot >= 0
        && Buildings.Rows.IsLive(buildingSlot)
        && Lots.Rows.TryResolve(Buildings.Lot[buildingSlot], out int lotSlot)
        && Lots.PatternOf(lotSlot) == Space.BlockPattern.PadSite;

    /// <summary>Whether a Building is a sales yard's shed.</summary>
    public bool IsSalesYard(int buildingSlot) =>
        buildingSlot >= 0
        && Buildings.Rows.IsLive(buildingSlot)
        && Lots.Rows.TryResolve(Buildings.Lot[buildingSlot], out int lotSlot)
        && Lots.PatternOf(lotSlot) == Space.BlockPattern.SalesYard;

    /// <summary>Whether a Building is a precinct, of either height.</summary>
    public bool IsPrecinct(int buildingSlot) =>
        buildingSlot >= 0
        && Buildings.Rows.IsLive(buildingSlot)
        && Lots.Rows.TryResolve(Buildings.Lot[buildingSlot], out int lotSlot)
        && Space.BlockPatterns.IsPrecinct(Lots.PatternOf(lotSlot));

    /// <summary>Whether a Building is a market hall.</summary>
    public bool IsMarketHall(int buildingSlot) =>
        buildingSlot >= 0
        && Buildings.Rows.IsLive(buildingSlot)
        && Lots.Rows.TryResolve(Buildings.Lot[buildingSlot], out int lotSlot)
        && Lots.PatternOf(lotSlot) == Space.BlockPattern.MarketHall;

    /// <summary>Whether the Building is a commercial form that only Businesses occupy.</summary>
    public bool IsShopOnly(int buildingSlot) =>
        IsTradeCentre(buildingSlot) || IsSupermarket(buildingSlot) || IsDepartmentStore(buildingSlot)
        || IsPadSite(buildingSlot) || IsSalesYard(buildingSlot) || IsPrecinct(buildingSlot)
        || IsMarketHall(buildingSlot);

    private int MarketHallUnitCount(int buildingSlot)
    {
        int lotSlot = Lots.Rows.Resolve(Buildings.Lot[buildingSlot]);

        return Space.MarketHall.Stalls(Lots.FootprintWide[lotSlot].Raw, Lots.FootprintDeep[lotSlot].Raw);
    }

    private int PrecinctUnitCount(int buildingSlot)
    {
        int lotSlot = Lots.Rows.Resolve(Buildings.Lot[buildingSlot]);

        return Space.Precinct.Units(Lots.FootprintWide[lotSlot].Raw, Lots.FootprintDeep[lotSlot].Raw, Lots.Storeys[lotSlot]);
    }

    /// <summary>How many stalls a precinct's deck holds, over every level.</summary>
    private int PrecinctStalls(int buildingSlot)
    {
        int lotSlot = Lots.Rows.Resolve(Buildings.Lot[buildingSlot]);
        int level = Space.TownSupermarket.Stalls(
            Lots.ParcelBounds(lotSlot).Y, Lots.ParcelDeep[lotSlot].Raw, Lots.FootprintBounds(lotSlot).Y,
            Lots.FootprintWide[lotSlot].Raw, Lots.FootprintDeep[lotSlot].Raw,
            Rules.Lots.StreetHalfWidthTiles, Rules.Parking.Stalls).Stalls;

        return level * Space.TownSupermarket.DeckLevels;
    }

    private int PadStalls(int buildingSlot) =>
        Space.PadSite.Stalls(
            Lots.FootprintWide[Lots.Rows.Resolve(Buildings.Lot[buildingSlot])].Raw, Rules.Parking.Stalls).Stalls;

    /// <summary>The Lot's parcel, and the block ground it was carved from.</summary>
    public (Space.Parcel Parcel, Space.BlockGround Ground) ParcelGround(int lotSlot)
    {
        Space.Frontage.BlockOf(
            Roads.Streets, Lots.East[lotSlot], Lots.North[lotSlot], (Space.StreetSide)Lots.Side[lotSlot],
            out int column, out int row, out Space.BlockFace face);

        Space.Parcel parcel = new(face, (Space.StreetSide)Lots.Side[lotSlot], Quantities.Tiles.Zero, Lots.Parcel(lotSlot));

        return (parcel, Roads.Streets.Block(column, row));
    }

    private int SalesYardStalls(int buildingSlot)
    {
        (Space.Parcel parcel, Space.BlockGround ground) = ParcelGround(Lots.Rows.Resolve(Buildings.Lot[buildingSlot]));

        return Space.SalesYard.Stalls(parcel, ground, Rules.Lots.StreetHalfWidthTiles, Rules.Parking.Stalls).Stalls;
    }

    /// <summary>
    /// How many stalls a supermarket's car park holds, over every level of a deck. The kind's
    /// <c>parked</c> does not apply, because the car park is part of the form.
    /// </summary>
    private int SupermarketStalls(int buildingSlot)
    {
        int lotSlot = Lots.Rows.Resolve(Buildings.Lot[buildingSlot]);
        int level = Space.TownSupermarket.Stalls(
            Lots.ParcelBounds(lotSlot).Y, Lots.ParcelDeep[lotSlot].Raw, Lots.FootprintBounds(lotSlot).Y,
            Lots.FootprintWide[lotSlot].Raw, Lots.FootprintDeep[lotSlot].Raw,
            Rules.Lots.StreetHalfWidthTiles, Rules.Parking.Stalls).Stalls;

        return level * Space.TownSupermarket.Levels(Lots.PatternOf(lotSlot));
    }

    /// <summary>
    /// How many Households a shop-house's upper storeys hold: the floor above its ground-floor Unit
    /// over <c>[capacity] floor_tiles_per_occupant</c>.
    /// </summary>
    private int ShopHouseHomes(int buildingSlot)
    {
        int lotSlot = Lots.Rows.Resolve(Buildings.Lot[buildingSlot]);
        int ground = Lots.FootprintWide[lotSlot].Raw * Lots.FootprintDeep[lotSlot].Raw;

        return Core.Rules.CapacityRuleset.Holds(FloorTilesOf(buildingSlot) - ground, Rules.Capacity.FloorTilesPerOccupant);
    }

    private int DepartmentStoreUnitCount(int buildingSlot)
    {
        Span<Space.DepartmentStore.Bay> bays = stackalloc Space.DepartmentStore.Bay[Space.DepartmentStore.MaxUnits];

        return Space.DepartmentStore.Units(Lots.FootprintWide[Lots.Rows.Resolve(Buildings.Lot[buildingSlot])].Raw, bays);
    }

    private int CentreUnitCount(int buildingSlot) =>
        Space.CarParkCentre.UnitCount(Lots.FootprintWide[Lots.Rows.Resolve(Buildings.Lot[buildingSlot])].Raw);

    /// <summary>
    /// How many stalls a car-park centre's surface car park holds. The kind's <c>parked</c> does not
    /// apply, because the car park is part of the form.
    /// </summary>
    private int CentreStalls(int buildingSlot)
    {
        int lotSlot = Lots.Rows.Resolve(Buildings.Lot[buildingSlot]);

        return Space.CarParkCentre.Stalls(
            Lots.ParcelBounds(lotSlot).Y, Lots.FootprintBounds(lotSlot).Y, Lots.FootprintWide[lotSlot].Raw,
            Rules.Lots.StreetHalfWidthTiles, Rules.Parking.Stalls).Stalls;
    }

    private void RaiseUnits(int buildingSlot)
    {
        if (IsTradeCentre(buildingSlot))
        {
            RaiseCentreUnits(buildingSlot);
        }
        else if (IsShopHouse(buildingSlot))
        {
            RaiseShopHouseUnit(buildingSlot);
        }
        else if (IsSupermarket(buildingSlot))
        {
            RaiseSingleUnit(buildingSlot, anchor: true, Space.BlockFace.South);
        }
        else if (IsPadSite(buildingSlot) || IsSalesYard(buildingSlot))
        {
            RaiseSingleUnit(buildingSlot, anchor: false, FaceOf(Lots.Rows.Resolve(Buildings.Lot[buildingSlot])));
        }
        else if (IsDepartmentStore(buildingSlot))
        {
            RaiseDepartmentStoreUnits(buildingSlot);
        }
        else if (IsPrecinct(buildingSlot))
        {
            RaisePrecinctUnits(buildingSlot);
        }
        else if (IsMarketHall(buildingSlot))
        {
            RaiseMarketHallUnits(buildingSlot);
        }
        else if (TryDeclaredOccupancy(Buildings.Kind[buildingSlot], buildingSlot, out int tenancies))
        {
            ShapeEqualUnits(buildingSlot, tenancies);
        }
    }

    /// <summary>
    /// Lays a car-park centre's row of Units along its footprint, west to east, doors facing south.
    /// </summary>
    /// <remarks>
    /// The draw is keyed on the parcel's corner, so the same ground raises the same row.
    /// </remarks>
    private void RaiseCentreUnits(int buildingSlot)
    {
        int lotSlot = Lots.Rows.Resolve(Buildings.Lot[buildingSlot]);
        int wide = Lots.FootprintWide[lotSlot].Raw;
        Tiles deep = Lots.FootprintDeep[lotSlot];
        byte storeys = Lots.Storeys[lotSlot];

        ulong patch = ((ulong)(uint)Lots.ParcelBounds(lotSlot).X << 32) | (uint)Lots.ParcelBounds(lotSlot).Y;
        ulong draw = Determinism.Randomness.Draw(Key, patch, Ticks.Zero, Determinism.PurposeTag.CentreUnits);

        int count = Space.CarParkCentre.UnitCount(wide);
        Span<int> widths = count <= 64 ? stackalloc int[64] : new int[count];

        count = Space.CarParkCentre.UnitWidths(wide, draw, widths);

        int anchor = Space.CarParkCentre.AnchorIndex(count, draw);
        int east = 0;

        for (int i = 0; i < count; i++)
        {
            Handle<Unit> added = Units.Create(
                Buildings.Rows.At(buildingSlot), new Tiles(east), new Tiles(0), new Tiles(widths[i]), deep,
                0, storeys, (byte)Space.BlockFace.South, anchor: i == anchor,
                floor: widths[i] * deep.Raw * storeys);

            BuildingUnits.InsertOrdered(buildingSlot, Units.Rows.Resolve(added));
            east += widths[i];
        }
    }

    /// <summary>
    /// Lays a shop-house's one ground-floor Unit over its whole footprint, its door facing the Street
    /// the Lot fronts.
    /// </summary>
    private void RaiseShopHouseUnit(int buildingSlot)
    {
        int lotSlot = Lots.Rows.Resolve(Buildings.Lot[buildingSlot]);
        Tiles wide = Lots.FootprintWide[lotSlot];
        Tiles deep = Lots.FootprintDeep[lotSlot];
        Handle<Unit> added = Units.Create(
            Buildings.Rows.At(buildingSlot), Tiles.Zero, Tiles.Zero, wide, deep,
            0, 1, (byte)FaceOf(lotSlot), anchor: false, floor: wide.Raw * deep.Raw);

        BuildingUnits.InsertOrdered(buildingSlot, Units.Rows.Resolve(added));
    }

    /// <summary>
    /// Lays a supermarket's one anchor Unit over its whole store, its door facing south onto the Street.
    /// </summary>
    private void RaiseSingleUnit(int buildingSlot, bool anchor, Space.BlockFace side)
    {
        int lotSlot = Lots.Rows.Resolve(Buildings.Lot[buildingSlot]);
        Tiles wide = Lots.FootprintWide[lotSlot];
        Tiles deep = Lots.FootprintDeep[lotSlot];

        Handle<Unit> added = Units.Create(
            Buildings.Rows.At(buildingSlot), Tiles.Zero, Tiles.Zero, wide, deep,
            0, 1, (byte)side, anchor, floor: wide.Raw * deep.Raw);

        BuildingUnits.InsertOrdered(buildingSlot, Units.Rows.Resolve(added));
    }

    /// <summary>
    /// Lays a department store's anchor Unit between its two corner Units, each over every storey,
    /// doors facing south onto the high street.
    /// </summary>
    private void RaiseDepartmentStoreUnits(int buildingSlot)
    {
        int lotSlot = Lots.Rows.Resolve(Buildings.Lot[buildingSlot]);
        int wide = Lots.FootprintWide[lotSlot].Raw;
        Tiles deep = Lots.FootprintDeep[lotSlot];
        byte storeys = Lots.Storeys[lotSlot];
        Span<Space.DepartmentStore.Bay> bays = stackalloc Space.DepartmentStore.Bay[Space.DepartmentStore.MaxUnits];
        int count = Space.DepartmentStore.Units(wide, bays);

        for (int i = 0; i < count; i++)
        {
            Space.DepartmentStore.Bay bay = bays[i];
            Handle<Unit> added = Units.Create(
                Buildings.Rows.At(buildingSlot), new Tiles(bay.East), Tiles.Zero, new Tiles(bay.Wide), deep,
                0, storeys, (byte)Space.BlockFace.South, anchor: bay.Anchor,
                floor: bay.Wide * deep.Raw * storeys);

            BuildingUnits.InsertOrdered(buildingSlot, Units.Rows.Resolve(added));
        }
    }

    private void RaisePrecinctUnits(int buildingSlot)
    {
        int lotSlot = Lots.Rows.Resolve(Buildings.Lot[buildingSlot]);
        int wide = Lots.FootprintWide[lotSlot].Raw;
        int deep = Lots.FootprintDeep[lotSlot].Raw;
        byte storeys = Lots.Storeys[lotSlot];
        Span<Space.Precinct.Row> rows = stackalloc Space.Precinct.Row[Space.Precinct.RowCount(wide)];
        int count = Space.Precinct.Rows(wide, rows);
        int perRow = Space.Precinct.UnitsPerRow(deep);

        for (byte storey = 0; storey < storeys; storey++)
        {
            for (int row = 0; row < count; row++)
            {
                for (int j = 0; j < perRow; j++)
                {
                    int north = j * Space.Precinct.UnitTiles;
                    int along = j == perRow - 1 ? deep - north : Space.Precinct.UnitTiles;
                    Handle<Unit> added = Units.Create(
                        Buildings.Rows.At(buildingSlot), new Tiles(rows[row].East), new Tiles(north),
                        new Tiles(rows[row].Wide), new Tiles(along), storey, 1, (byte)rows[row].Face,
                        anchor: false, floor: rows[row].Wide * along);

                    BuildingUnits.InsertOrdered(buildingSlot, Units.Rows.Resolve(added));
                }
            }
        }
    }

    private void RaiseMarketHallUnits(int buildingSlot)
    {
        int lotSlot = Lots.Rows.Resolve(Buildings.Lot[buildingSlot]);
        int wide = Lots.FootprintWide[lotSlot].Raw;
        int pairs = Space.MarketHall.Pairs(wide);
        int perColumn = Space.MarketHall.StallsPerColumn(Lots.FootprintDeep[lotSlot].Raw);

        for (int pair = 0; pair < pairs; pair++)
        {
            int west = Space.MarketHall.PairEast(wide, pair);

            for (int column = 0; column < 2; column++)
            {
                Space.BlockFace face = column == 0 ? Space.BlockFace.West : Space.BlockFace.East;

                for (int stall = 0; stall < perColumn; stall++)
                {
                    Handle<Unit> added = Units.Create(
                        Buildings.Rows.At(buildingSlot), new Tiles(west + column), new Tiles(1 + stall),
                        new Tiles(1), new Tiles(1), 0, 1, (byte)face, anchor: false, floor: 1);

                    BuildingUnits.InsertOrdered(buildingSlot, Units.Rows.Resolve(added));
                }
            }
        }
    }

    /// <summary>
    /// Gives a Building exactly <paramref name="count"/> equal Units, each an equal share of its floor.
    /// </summary>
    /// <remarks>
    /// A surplus Unit is removed only while vacant, so the caller brings the tenant count within
    /// <paramref name="count"/> first. Every surviving Business keeps the Unit it holds.
    /// </remarks>
    private void ShapeEqualUnits(int buildingSlot, int count)
    {
        IndexList units = BuildingUnits;
        int held = units.Length(buildingSlot);

        while (held > count)
        {
            int surplus = Rows.NoSlot;

            foreach (int unit in units.Walk(buildingSlot))
            {
                if (Units.IsVacant(unit))
                {
                    surplus = unit;
                }
            }

            if (surplus == Rows.NoSlot)
            {
                break;
            }

            units.Remove(buildingSlot, surplus);
            Units.Rows.Free(Units.Rows.At(surplus));
            held--;
        }

        int lotSlot = Lots.Rows.TryResolve(Buildings.Lot[buildingSlot], out int resolved)
            ? resolved
            : Rows.NoSlot;
        Tiles wide = lotSlot >= 0 ? Lots.FootprintWide[lotSlot] : new Tiles(1);
        Tiles deep = lotSlot >= 0 ? Lots.FootprintDeep[lotSlot] : new Tiles(1);
        byte storeys = lotSlot >= 0 ? Lots.Storeys[lotSlot] : (byte)1;

        for (; held < count; held++)
        {
            Handle<Unit> added = Units.Create(
                Buildings.Rows.At(buildingSlot), new Tiles(0), new Tiles(0), wide, deep,
                0, storeys, 0, anchor: false, floor: 0);

            BuildingUnits.InsertOrdered(buildingSlot, Units.Rows.Resolve(added));
        }

        units = BuildingUnits;

        int share = count > 0
            ? Arithmetic.IntegerMath.FloorDiv(FloorTilesOf(buildingSlot), count)
            : 0;

        foreach (int unit in units.Walk(buildingSlot))
        {
            Units.Floor[unit] = share;
        }
    }

    /// <summary>
    /// Lets every vacant Unit of a Building to a new Business of its kind's trade.
    /// </summary>
    /// <remarks>
    /// Each Business names the Building as its origin, as the one that comes with a Building does.
    /// </remarks>
    /// <returns>How many Businesses were founded.</returns>
    public int FillUnits(int buildingSlot)
    {
        int founded = 0;

        while (OpenInVacantUnit(buildingSlot))
        {
            founded++;
        }

        return founded;
    }

    /// <summary>
    /// Opens one Business of the Building's trade in its first vacant Unit.
    /// </summary>
    /// <param name="buildingSlot">The Building's slot.</param>
    /// <param name="granted">
    /// Whether the treasury pays the trade's <c>opening_grant</c>, as <see cref="CreateBusiness"/>
    /// takes it. Otherwise the Business opens at zero balance.
    /// </param>
    /// <returns>
    /// Whether a Business opened. False when the kind has no trade, no Unit is vacant or the treasury
    /// cannot pay the grant.
    /// </returns>
    public bool OpenInVacantUnit(int buildingSlot, bool granted = false)
    {
        byte trade = Rules.Kind(Buildings.Kind[buildingSlot]).Business;

        if (trade == 0 || VacantUnit(buildingSlot) == Rows.NoSlot)
        {
            return false;
        }

        Handle<Building> building = Buildings.Rows.At(buildingSlot);
        Handle<Business> came = CreateBusiness(building, trade, granted);

        if (came.IsNone)
        {
            return false;
        }

        Businesses.Origin[Businesses.Rows.Resolve(came)] = building;
        FitBusiness(came);

        return true;
    }

    private void LetUnit(int buildingSlot, int businessSlot)
    {
        int unit = VacantUnit(buildingSlot);

        if (unit == Rows.NoSlot)
        {
            Businesses.Unit[businessSlot] = default;
            return;
        }

        Units.Let(unit, businessSlot);
        Businesses.Unit[businessSlot] = Units.Rows.At(unit);
        Changes?.Building(buildingSlot);
    }

    private void VacateUnit(int businessSlot)
    {
        if (Units.Rows.TryResolve(Businesses.Unit[businessSlot], out int unit))
        {
            Units.Vacate(unit);
            Changes?.Building(Buildings.Rows.Resolve(Units.Building[unit]));
        }

        Businesses.Unit[businessSlot] = default;
    }

    private void FreeUnits(int buildingSlot)
    {
        IndexList units = BuildingUnits;
        int unit = units.PopFront(buildingSlot);

        while (unit != Rows.NoSlot)
        {
            int tenant = Units.TenantSlot(unit);

            if (tenant != Rows.NoSlot)
            {
                Businesses.Unit[tenant] = default;
            }

            Units.Rows.Free(Units.Rows.At(unit));
            unit = units.PopFront(buildingSlot);
        }
    }

    /// <summary>
    /// Brings every equal-Unit Building's Units to its current tenancy count after a Ruleset change.
    /// </summary>
    /// <remarks>A trade form's Units are fixed by its geometry and are left alone.</remarks>
    private void ReshapeUnits()
    {
        for (int slot = 0; slot < Buildings.Rows.SlotCount; slot++)
        {
            if (Buildings.Rows.IsLive(slot)
                && !IsShopOnly(slot)
                && !IsShopHouse(slot)
                && TryDeclaredOccupancy(Buildings.Kind[slot], slot, out int tenancies))
            {
                ShapeEqualUnits(slot, tenancies);
            }
        }
    }

    private void RebuildUnits()
    {
        Buildings.UnitHead.Span.Clear();
        Buildings.UnitTail.Span.Clear();
        Units.BuildingNext.Span.Clear();
        Units.Tenant.Span.Clear();

        IndexList units = BuildingUnits;

        for (int slot = 0; slot < Units.Rows.SlotCount; slot++)
        {
            if (Units.Rows.IsLive(slot)
                && Buildings.Rows.TryResolve(Units.Building[slot], out int buildingSlot))
            {
                units.Append(buildingSlot, slot);
            }
        }

        for (int slot = 0; slot < Businesses.Rows.SlotCount; slot++)
        {
            if (Businesses.Rows.IsLive(slot)
                && Units.Rows.TryResolve(Businesses.Unit[slot], out int unit))
            {
                Units.Let(unit, slot);
            }
        }
    }
}
