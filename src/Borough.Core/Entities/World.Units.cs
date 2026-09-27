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

    private void RaiseUnits(int buildingSlot)
    {
        if (TryDeclaredOccupancy(Buildings.Kind[buildingSlot], buildingSlot, out int tenancies))
        {
            ShapeEqualUnits(buildingSlot, tenancies);
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
    private void ReshapeUnits()
    {
        for (int slot = 0; slot < Buildings.Rows.SlotCount; slot++)
        {
            if (Buildings.Rows.IsLive(slot)
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
