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

    /// <summary>Whether the Building is a commercial form that only Businesses occupy.</summary>
    public bool IsShopOnly(int buildingSlot) =>
        IsTradeCentre(buildingSlot) || IsSupermarket(buildingSlot) || IsDepartmentStore(buildingSlot)
        || IsPadSite(buildingSlot) || IsSalesYard(buildingSlot) || IsPrecinct(buildingSlot);

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
            Lots.ParcelNorth[lotSlot].Raw, Lots.ParcelDeep[lotSlot].Raw, Lots.FootprintNorth[lotSlot].Raw,
            Lots.FootprintWide[lotSlot].Raw, Lots.FootprintDeep[lotSlot].Raw,
            Rules.Lots.StreetHalfWidthTiles, Rules.Parking.Stalls).Stalls;

        return level * Space.TownSupermarket.DeckLevels;
    }

    private int PadStalls(int buildingSlot) =>
        Space.PadSite.Stalls(
            Lots.FootprintWide[Lots.Rows.Resolve(Buildings.Lot[buildingSlot])].Raw, Rules.Parking.Stalls).Stalls;

    /// <summary>The Lot's parcel, and the block ground it was carved from.</summary>
    public (Space.Parcel Parcel, Space.BlockGround Ground) SalesYardGround(int lotSlot)
    {
        Space.Frontage.BlockOf(
            Roads.Streets, Lots.East[lotSlot], Lots.North[lotSlot], (Space.StreetSide)Lots.Side[lotSlot],
            out int column, out int row, out Space.BlockFace face);

        Space.Parcel parcel = new(face, (Space.StreetSide)Lots.Side[lotSlot], Quantities.Tiles.Zero,
            Lots.ParcelEast[lotSlot], Lots.ParcelNorth[lotSlot], Lots.ParcelWide[lotSlot], Lots.ParcelDeep[lotSlot]);

        return (parcel, Space.BlockGround.At(Roads.Streets.Lattice, column, row));
    }

    private int SalesYardStalls(int buildingSlot)
    {
        (Space.Parcel parcel, Space.BlockGround ground) = SalesYardGround(Lots.Rows.Resolve(Buildings.Lot[buildingSlot]));

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
            Lots.ParcelNorth[lotSlot].Raw, Lots.ParcelDeep[lotSlot].Raw, Lots.FootprintNorth[lotSlot].Raw,
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
            Lots.ParcelNorth[lotSlot].Raw, Lots.FootprintNorth[lotSlot].Raw, Lots.FootprintWide[lotSlot].Raw,
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

        ulong patch = ((ulong)(uint)Lots.ParcelEast[lotSlot].Raw << 32) | (uint)Lots.ParcelNorth[lotSlot].Raw;
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
        byte trade = Rules.Kind(Buildings.Kind[buildingSlot]).Business;

        if (trade == 0)
        {
            return 0;
        }

        Handle<Building> building = Buildings.Rows.At(buildingSlot);
        int founded = 0;

        while (VacantUnit(buildingSlot) != Rows.NoSlot)
        {
            Handle<Business> came = CreateBusiness(building, trade);

            Businesses.Origin[Businesses.Rows.Resolve(came)] = building;
            FitBusiness(came);
            founded++;
        }

        return founded;
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
    }

    private void VacateUnit(int businessSlot)
    {
        if (Units.Rows.TryResolve(Businesses.Unit[businessSlot], out int unit))
        {
            Units.Vacate(unit);
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
