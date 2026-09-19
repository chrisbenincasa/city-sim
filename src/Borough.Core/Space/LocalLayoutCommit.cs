using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>The simulation's one-Building transition; all ordinary refusals precede retirement.</summary>
internal static class LocalLayoutCommit
{
    internal static LocalLayoutCheck Apply(World world, LocalLayoutProposal proposal, WorldKey key,
        out Handle<Building> building)
    {
        building = default;
        LocalLayoutCheck check = LocalLayout.Revalidate(world, proposal);
        if (!check.Accepted) { return check; }
        if (!Prepare(world, proposal)) { return new(LocalLayoutRefusal.Storage); }

        LocalLot door = proposal.Sources[0];
        foreach (LocalLot source in proposal.Sources) { world.Lots.Rows.Free(source.Handle); }
        // Zone is a transitional discovery summary. Geographic permissions remain unchanged.
        Handle<Lot> lot = world.Lots.Create(new Tiles(door.East), new Tiles(door.North), LotTable.Housing,
            (StreetSide)door.Side);
        int row = world.Lots.Rows.Resolve(lot);
        LandRectangle site = proposal.Site;
        LandRectangle footprint = proposal.Building.Footprint;
        world.Lots.ParcelEast[row] = new Tiles(site.X);
        world.Lots.ParcelNorth[row] = new Tiles(site.Y);
        world.Lots.ParcelWide[row] = new Tiles(site.Width);
        world.Lots.ParcelDeep[row] = new Tiles(site.Height);
        world.Lots.FootprintEast[row] = new Tiles(footprint.X);
        world.Lots.FootprintNorth[row] = new Tiles(footprint.Y);
        world.Lots.FootprintWide[row] = new Tiles(footprint.Width);
        world.Lots.FootprintDeep[row] = new Tiles(footprint.Height);
        world.Lots.Storeys[row] = proposal.Building.Storeys;
        world.Lots.Pattern[row] = (byte)((int)proposal.Building.Form + 1);
        world.Frontage.Rebuild(world.Lots, world.Roads.Streets);
        world.LotsAdmitting.Invalidate();
        building = world.CreateBuilding(lot, proposal.Building.Kind, world.Tick, key);
        return default;
    }

    private static bool Prepare(World world, LocalLayoutProposal proposal)
    {
        byte kind = proposal.Building.Kind;
        bool business = world.Rules.Kind(kind).Business != 0 && proposal.Occupancy > 1;
        int bins = business && world.TryMoneyResource(out _) ? 1 : 0;
        int rules = 0;
        foreach (BinDeclaration bin in world.Rules.BinsOf(kind))
        {
            if (bin.Tenancy == BinTenancy.Premises
                || (business && bin.Tenancy == BinTenancy.Business && !world.Rules.IsConserved(bin.Resource))) { bins++; }
        }
        foreach (RuleId rule in world.Rules.RulesOf(kind))
        {
            BinTenancy tenancy = world.Rules.Rule(rule).Tenancy;
            if (tenancy == BinTenancy.Premises || (business && tenancy == BinTenancy.Business)) { rules++; }
        }
        int parking = world.Rules.Kind(kind).Parked
            && CapacityRuleset.Holds(proposal.FloorTiles, world.Rules.Capacity.FloorTilesPerParkingSpace) > 0 ? 1 : 0;
        LandRectangle f = proposal.Building.Footprint;
        int cells = 0;
        int x0 = CellGrid.ToCells(new Tiles(f.X)).Raw, x1 = CellGrid.ToCells(new Tiles(f.X + f.Width - 1)).Raw;
        int y0 = CellGrid.ToCells(new Tiles(f.Y)).Raw, y1 = CellGrid.ToCells(new Tiles(f.Y + f.Height - 1)).Raw;
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                if (world.Layers.Residency.Slot(new Cells(x), new Cells(y)) == CellResidency.NotResident) { cells++; }
            }
        }
        // Fit creates only these tables. Check every allocator before reserving any column capacity.
        if (!world.Lots.Rows.AllocationSlots(1, proposal.SourceCount, out int lots)
            || !world.Buildings.Rows.AllocationSlots(1, 0, out int buildings)
            || !world.Bins.Rows.AllocationSlots(bins, 0, out int binSlots)
            || !world.Businesses.Rows.AllocationSlots(business ? 1 : 0, 0, out int businesses)
            || !world.RuleInstances.Rows.AllocationSlots(rules, 0, out int instances)
            || !world.CarParks.Rows.AllocationSlots(parking, 0, out int parks)
            || !world.Layers.Cells.Rows.AllocationSlots(cells, 0, out int layerCells)) { return false; }
        world.Lots.Rows.PrepareCapacity(lots, int.MaxValue);
        world.Buildings.Rows.PrepareCapacity(buildings, int.MaxValue);
        world.Bins.Rows.PrepareCapacity(binSlots, int.MaxValue);
        world.Businesses.Rows.PrepareCapacity(businesses, int.MaxValue);
        world.RuleInstances.Rows.PrepareCapacity(instances, int.MaxValue);
        world.CarParks.Rows.PrepareCapacity(parks, int.MaxValue);
        world.Layers.Cells.Rows.PrepareCapacity(layerCells, int.MaxValue);
        return true;
    }
}
