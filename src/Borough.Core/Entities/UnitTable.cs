using Borough.Core.Quantities;
using Borough.Core.Tables;

namespace Borough.Core.Entities;

/// <summary>Row type of <see cref="UnitTable"/>.</summary>
public readonly struct Unit;

/// <summary>
/// The city's Units — <b>one tenancy's space inside a Building</b>.
/// </summary>
/// <remarks>
/// <para>
/// A Unit is a rectangle within its Building's footprint, a first storey, a storey span, a facing side
/// and an anchor flag. The rectangle is in Tiles, measured from the footprint's east and north edges.
/// A Business's premises name a Building and one of its Units, and a Unit holds at most one Business.
/// </para>
/// <para>
/// A Building of a kind without a trade form holds one equal Unit per tenancy. Each covers the whole
/// footprint and carries an equal share of the floor, so its storey and side mean nothing. Households
/// still take tenancies by count and name no Unit, so they and Businesses compete for the same
/// ceiling.
/// </para>
/// <para>
/// <see cref="Floor"/> is saved rather than computed from the rectangle, because an equal Unit's
/// share is not a rectangle. A Business's posts are this floor over <c>[capacity]
/// floor_tiles_per_job</c>.
/// </para>
/// </remarks>
[Table]
public sealed class UnitTable
{
    private readonly Rows<Unit> _rows;

    /// <summary>Allocates the table.</summary>
    public UnitTable(int capacity, BuildingTable buildings)
    {
        ArgumentNullException.ThrowIfNull(buildings);

        _rows = new Rows<Unit>("unit", capacity, Buffering.OneCopy);

        Building = _rows.SavedHandle("building", buildings.Rows);
        East = _rows.Saved<Tiles>("east");
        North = _rows.Saved<Tiles>("north");
        Wide = _rows.Saved<Tiles>("wide");
        Deep = _rows.Saved<Tiles>("deep");
        FirstStorey = _rows.Saved<byte>("first_storey");
        Storeys = _rows.Saved<byte>("storeys");
        Side = _rows.Saved<byte>("side");
        Anchor = _rows.Saved<byte>("anchor");
        Floor = _rows.Saved<int>("floor");

        Tenant = _rows.Derived<int>("tenant");
        BuildingNext = _rows.Derived<int>("building_next");

        _rows.Seal();
    }

    /// <summary>The table's rows.</summary>
    public Rows<Unit> Rows => _rows;

    /// <summary>The Building this Unit is part of.</summary>
    public HandleColumn<Building> Building { get; }

    /// <summary>West edge of the Unit, in Tiles east of the footprint's west edge.</summary>
    public Column<Tiles> East { get; }

    /// <summary>South edge of the Unit, in Tiles north of the footprint's south edge.</summary>
    public Column<Tiles> North { get; }

    /// <summary>East–west extent, in Tiles.</summary>
    public Column<Tiles> Wide { get; }

    /// <summary>North–south extent, in Tiles.</summary>
    public Column<Tiles> Deep { get; }

    /// <summary>The lowest storey the Unit occupies, counted from zero at the ground.</summary>
    public Column<byte> FirstStorey { get; }

    /// <summary>How many storeys the Unit spans.</summary>
    public Column<byte> Storeys { get; }

    /// <summary>The side of the footprint the Unit's door faces.</summary>
    public Column<byte> Side { get; }

    /// <summary>One where the Unit is its Building's anchor, else zero.</summary>
    public Column<byte> Anchor { get; }

    /// <summary>The Unit's floor, in Tiles.</summary>
    public Column<int> Floor { get; }

    /// <summary>The slot of the Business holding the Unit, plus one; zero when vacant.</summary>
    public Column<int> Tenant { get; }

    /// <summary>Next Unit of the same Building.</summary>
    public Column<int> BuildingNext { get; }

    /// <summary>The slot of the Business holding the Unit, or <see cref="Tables.Rows.NoSlot"/>.</summary>
    public int TenantSlot(int slot) => Tenant[slot] - 1;

    /// <summary>Whether no Business holds the Unit.</summary>
    public bool IsVacant(int slot) => Tenant[slot] == 0;

    internal void Let(int slot, int business) => Tenant[slot] = business + 1;

    internal void Vacate(int slot) => Tenant[slot] = 0;

    internal Handle<Unit> Create(
        Handle<Building> building, Tiles east, Tiles north, Tiles wide, Tiles deep,
        byte firstStorey, byte storeys, byte side, bool anchor, int floor)
    {
        Handle<Unit> handle = _rows.Allocate();
        int slot = _rows.Resolve(handle);

        Building[slot] = building;
        East[slot] = east;
        North[slot] = north;
        Wide[slot] = wide;
        Deep[slot] = deep;
        FirstStorey[slot] = firstStorey;
        Storeys[slot] = storeys;
        Side[slot] = side;
        Anchor[slot] = anchor ? (byte)1 : (byte)0;
        Floor[slot] = floor;
        Tenant[slot] = 0;

        return handle;
    }
}
