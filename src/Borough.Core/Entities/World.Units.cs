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

    /// <summary>Whether a Building stands on a shop-house parade's Lot.</summary>
    public bool IsShopHouse(int buildingSlot) =>
        buildingSlot >= 0
        && Buildings.Rows.IsLive(buildingSlot)
        && Lots.Rows.TryResolve(Buildings.Lot[buildingSlot], out int lotSlot)
        && Lots.PatternOf(lotSlot) == Space.BlockPattern.ShopHouseParade;

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
        Space.Frontage.BlockOf(
            Roads.Streets, Lots.East[lotSlot], Lots.North[lotSlot], (Space.StreetSide)Lots.Side[lotSlot],
            out _, out _, out Space.BlockFace face);

        Handle<Unit> added = Units.Create(
            Buildings.Rows.At(buildingSlot), Tiles.Zero, Tiles.Zero, wide, deep,
            0, 1, (byte)face, anchor: false, floor: wide.Raw * deep.Raw);

        BuildingUnits.InsertOrdered(buildingSlot, Units.Rows.Resolve(added));
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
                && !IsTradeCentre(slot)
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
