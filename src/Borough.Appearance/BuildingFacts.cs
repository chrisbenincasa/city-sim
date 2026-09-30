using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Appearance;

/// <summary>
/// The facts about one Building that choose its Appearance Family. Each is fixed when the Building
/// is raised, so a Building keeps its architecture for life.
/// </summary>
/// <param name="Id">The Building's monotonic id.</param>
/// <param name="Kind">The kind's name in the Ruleset in force.</param>
/// <param name="FrontageMetres">Footprint length along the Street.</param>
/// <param name="DepthMetres">Footprint length away from the Street.</param>
/// <param name="Face">
/// The monotonic id of the Segment the Lot fronts, or zero where the Lot has lost its Street.
/// </param>
/// <param name="Zone">The Lot's zone permission bits (<see cref="LotTable.Housing"/>, <see cref="LotTable.Trade"/>).</param>
/// <param name="RaisedDay">The game Day the Building was raised on.</param>
/// <param name="Units">How many Units the Building holds.</param>
/// <param name="Anchored">Whether one of its Units is an anchor.</param>
/// <param name="Parking">The car park laid on the Building's own ground.</param>
/// <param name="Corner">The Lot's parcel reaches both an east–west and a north–south edge of its block.</param>
public readonly record struct BuildingFacts(
    ulong Id,
    string Kind,
    int FrontageMetres,
    int DepthMetres,
    int Storeys,
    BlockPattern Pattern,
    ushort Zone,
    long RaisedDay,
    ulong Face,
    StreetSide Side,
    int Units = 0,
    bool Anchored = false,
    ParkingForm Parking = ParkingForm.None,
    bool Corner = false)
{
    /// <summary>Reads the facts of the live Building in <paramref name="slot"/>.</summary>
    /// <returns><c>false</c> where the slot is dead or the Building has no Lot or no footprint.</returns>
    public static bool TryOf(World world, RulesetNames names, int slot, out BuildingFacts facts)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(names);
        facts = default;

        BuildingTable buildings = world.Buildings;
        LotTable lots = world.Lots;
        if (!buildings.Rows.IsLive(slot) || !lots.Rows.TryResolve(buildings.Lot[slot], out int lot))
        {
            return false;
        }

        int wide = lots.FootprintWide[lot].Raw * Tiles.Metres;
        int deep = lots.FootprintDeep[lot].Raw * Tiles.Metres;
        if (wide <= 0 || deep <= 0)
        {
            return false;
        }

        bool eastWest = RunsEastWest(world.Roads.Streets.Lattice, lots, lot);
        Address address = lots.AddressOf(lot);
        byte kind = buildings.Kind[slot];
        BlockPattern pattern = lots.PatternOf(lot);
        int units = 0;
        bool anchored = false;

        foreach (int unit in world.BuildingUnits.Walk(slot))
        {
            units++;
            anchored |= world.Units.Anchor[unit] != 0;
        }

        facts = new BuildingFacts(
            buildings.Rows.IdAt(slot),
            names.Kind(kind) ?? $"#{kind}",
            eastWest ? wide : deep,
            eastWest ? deep : wide,
            Math.Max(1, (int)lots.Storeys[lot]),
            pattern,
            lots.Zone[lot],
            (long)(buildings.RaisedAt[slot].Raw / Ticks.PerDay),
            address.Exists ? world.Roads.Segments.Rows.IdAt(address.Segment) : 0UL,
            address.Side,
            units,
            anchored,
            pattern switch
            {
                BlockPattern.CarParkCentre or BlockPattern.Supermarket or BlockPattern.PadSite => ParkingForm.Surface,
                BlockPattern.DeckedSupermarket => ParkingForm.Deck,
                _ => ParkingForm.None,
            },
            IsCorner(world.Roads.Streets.Lattice, lots, lot));
        return true;
    }

    /// <summary>
    /// Whether the Lot's parcel reaches both an east–west and a north–south edge of its block, so its
    /// Building meets two street faces.
    /// </summary>
    public static bool IsCorner(BlockLattice lattice, LotTable lots, int lot)
    {
        ArgumentNullException.ThrowIfNull(lattice);
        ArgumentNullException.ThrowIfNull(lots);

        int west = lots.ParcelEast[lot].Raw;
        int south = lots.ParcelNorth[lot].Raw;
        int wide = lots.ParcelWide[lot].Raw;
        int deep = lots.ParcelDeep[lot].Raw;
        if (lattice.Nominal <= 0 || wide <= 0 || deep <= 0)
        {
            return false;
        }

        BlockGround block = BlockGround.At(lattice, lattice.LineAt(west), lattice.LineAt(south));
        bool eastOrWest = west == block.East || west + wide == block.East + block.Wide;
        bool northOrSouth = south == block.North || south + deep == block.North + block.Deep;
        return eastOrWest && northOrSouth;
    }

    /// <summary>Whether the Lot's Street runs east–west, read off the lattice line the Lot sits on.</summary>
    public static bool RunsEastWest(BlockLattice lattice, LotTable lots, int lot)
    {
        ArgumentNullException.ThrowIfNull(lattice);
        ArgumentNullException.ThrowIfNull(lots);
        return lattice.Nominal > 0 && lattice.EdgeOf(lattice.LineAt(lots.North[lot].Raw)) == lots.North[lot].Raw;
    }
}

/// <summary>
/// The facts about one Unit that change while its Building stands. They dress a Building and never
/// pick its Appearance Family.
/// </summary>
/// <param name="Id">The Unit's monotonic id.</param>
/// <param name="Let">A Business holds the Unit.</param>
/// <param name="Open">The Unit's Business keeps its shop hours at the world's current Tick.</param>
public readonly record struct UnitLiveFacts(ulong Id, bool Let, bool Open)
{
    /// <summary>Reads the live facts of the Unit in <paramref name="slot"/>.</summary>
    /// <returns><c>false</c> where the slot is dead.</returns>
    public static bool TryOf(World world, int slot, out UnitLiveFacts facts)
    {
        ArgumentNullException.ThrowIfNull(world);
        facts = default;

        UnitTable units = world.Units;
        if (!units.Rows.IsLive(slot))
        {
            return false;
        }

        int business = units.TenantSlot(slot);
        bool let = business >= 0;
        bool open = let
            && world.Rules.BusinessKind(world.Businesses.Kind[business]).ShopHours.IsOpen(world.Tick);
        facts = new UnitLiveFacts(units.Rows.IdAt(slot), let, open);
        return true;
    }
}

/// <summary>The car park a Building lays on its own ground.</summary>
public enum ParkingForm : byte
{
    None,
    Surface,
    Deck,
}
