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
    StreetSide Side)
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

        facts = new BuildingFacts(
            buildings.Rows.IdAt(slot),
            names.Kind(kind) ?? $"#{kind}",
            eastWest ? wide : deep,
            eastWest ? deep : wide,
            Math.Max(1, (int)lots.Storeys[lot]),
            lots.PatternOf(lot),
            lots.Zone[lot],
            (long)(buildings.RaisedAt[slot].Raw / Ticks.PerDay),
            address.Exists ? world.Roads.Segments.Rows.IdAt(address.Segment) : 0UL,
            address.Side);
        return true;
    }

    /// <summary>Whether the Lot's Street runs east–west, read off the lattice line the Lot sits on.</summary>
    public static bool RunsEastWest(BlockLattice lattice, LotTable lots, int lot)
    {
        ArgumentNullException.ThrowIfNull(lattice);
        ArgumentNullException.ThrowIfNull(lots);
        return lattice.Nominal > 0 && lattice.EdgeOf(lattice.LineAt(lots.North[lot].Raw)) == lots.North[lot].Raw;
    }
}
