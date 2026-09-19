using Borough.Core.Arithmetic;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>The realised geometry requested for one Building, not a future construction queue.</summary>
public readonly record struct LocalBuildingPlan(byte Kind, BlockPattern Form, LandRectangle Footprint, byte Storeys);

public enum LocalLayoutRefusal : byte
{
    None,
    SourceCount,
    StaleLot,
    DuplicateLot,
    Occupied,
    UnsupportedKind,
    InvalidGeometry,
    NoFrontage,
    IncompatibleFrontage,
    Noncontiguous,
    Overlap,
    AddressConflict,
    Permission,
    Intensity,
    NoHousingCapacity,
    StaleProposal,
    Storage,
    WrongPhase,
}

/// <summary>Structured refusal plus the geographic check's more specific reason.</summary>
public readonly record struct LocalLayoutCheck(LocalLayoutRefusal Refusal, PermissionRefusal Permission = PermissionRefusal.None)
{
    public bool Accepted => Refusal == LocalLayoutRefusal.None;
}

/// <summary>
/// Immutable, synchronous scratch. It holds exact source identities and geometry, not reservations.
/// The source array is bounded by the number of Tiles along one world edge.
/// </summary>
public sealed class LocalLayoutProposal
{
    internal LocalLayoutProposal(World world, LocalLot[] sources, LocalBuildingPlan building,
        LandRectangle site, Handle<RoadSegment> street, uint epoch, int floor, int occupancy, int housing, byte band)
    {
        World = world; Rules = world.Rules; Tick = world.Tick; Sources = sources;
        Building = building; Site = site; Street = street; StreetEpoch = epoch;
        FloorTiles = floor; Occupancy = occupancy; HousingCapacity = housing; Band = band;
    }

    internal World World { get; }
    internal Ruleset Rules { get; }
    internal Ticks Tick { get; }
    internal LocalLot[] Sources { get; }
    internal Handle<RoadSegment> Street { get; }
    internal uint StreetEpoch { get; }
    public LocalBuildingPlan Building { get; }
    public LandRectangle Site { get; }
    public int FloorTiles { get; }
    public int Occupancy { get; }
    public int HousingCapacity { get; }
    public byte Band { get; }
    public int SourceCount => Sources.Length;
}

internal readonly record struct LocalLot(Handle<Lot> Handle, ulong Id, int East, int North, byte Side,
    LandRectangle Parcel, LandRectangle Footprint, byte Storeys, byte Pattern)
{
    internal static LocalLot Read(LotTable lots, int row) => new(lots.Rows.At(row), lots.Rows.IdAt(row),
        lots.East[row].Raw, lots.North[row].Raw, lots.Side[row],
        new(lots.ParcelEast[row].Raw, lots.ParcelNorth[row].Raw, lots.ParcelWide[row].Raw, lots.ParcelDeep[row].Raw),
        new(lots.FootprintEast[row].Raw, lots.FootprintNorth[row].Raw, lots.FootprintWide[row].Raw, lots.FootprintDeep[row].Raw),
        lots.Storeys[row], lots.Pattern[row]);
}

/// <summary>Read-only whole-Lot assembly checks. Gameplay selection and evidence are separate.</summary>
public static class LocalLayout
{
    public static LocalLayoutCheck Evaluate(World world, ReadOnlySpan<Handle<Lot>> sources,
        LocalBuildingPlan building, out LocalLayoutProposal? proposal)
    {
        ArgumentNullException.ThrowIfNull(world);
        proposal = null;
        if (sources.Length == 0 || sources.Length > CellGrid.WorldTiles) { return new(LocalLayoutRefusal.SourceCount); }
        var captured = new LocalLot[sources.Length];
        for (int i = 0; i < sources.Length; i++)
        {
            if (!world.Lots.Rows.TryResolve(sources[i], out int row)) { return new(LocalLayoutRefusal.StaleLot); }
            captured[i] = LocalLot.Read(world.Lots, row);
        }
        return Evaluate(world, captured, building, out proposal);
    }

    private static LocalLayoutCheck Evaluate(World world, LocalLot[] sources,
        LocalBuildingPlan building, out LocalLayoutProposal? proposal)
    {
        proposal = null;
        if (!world.Rules.Declares(building.Kind) || !world.Rules.Kind(building.Kind).Houses
            || world.IsOutsideConnection(building.Kind)) { return new(LocalLayoutRefusal.UnsupportedKind); }
        if ((uint)building.Form >= BlockPatterns.Count || !building.Footprint.IsValid || building.Storeys == 0)
        {
            return new(LocalLayoutRefusal.InvalidGeometry);
        }
        int segment = Rows.NoSlot;
        byte side = sources[0].Side;
        bool horizontal = false;
        int blockColumn = 0, blockRow = 0;
        foreach (LocalLot source in sources)
        {
            int row = world.Lots.Rows.Resolve(source.Handle);
            if (!world.Lots.IsVacant(row)) { return new(LocalLayoutRefusal.Occupied); }
            if (!source.Parcel.IsValid || source.Side > (byte)StreetSide.Right) { return new(LocalLayoutRefusal.InvalidGeometry); }
            int at = Frontage.Locate(world.Roads.Streets, new Tiles(source.East), new Tiles(source.North), out _);
            if (at == Rows.NoSlot || !world.Roads.Segments.Rows.IsLive(at)
                || (RoadKind)world.Roads.Segments.Kind[at] != RoadKind.Street)
            {
                return new(LocalLayoutRefusal.NoFrontage);
            }
            if (!Frontage.BlockOf(world.Roads.Streets, new Tiles(source.East), new Tiles(source.North),
                (StreetSide)source.Side, out int column, out int on)) { return new(LocalLayoutRefusal.NoFrontage); }
            if (segment == Rows.NoSlot)
            {
                segment = at; blockColumn = column; blockRow = on;
                int a = world.Roads.Nodes.Rows.Resolve(world.Roads.Segments.NodeA[at]);
                int b = world.Roads.Nodes.Rows.Resolve(world.Roads.Segments.NodeB[at]);
                horizontal = world.Roads.Nodes.North[a] == world.Roads.Nodes.North[b];
            }
            if (at != segment || source.Side != side || column != blockColumn || on != blockRow)
            {
                return new(LocalLayoutRefusal.IncompatibleFrontage);
            }
            if (!Fronts(world, source, horizontal, column, on)) { return new(LocalLayoutRefusal.NoFrontage); }
        }

        Array.Sort(sources, horizontal
            ? static (a, b) => a.Parcel.X.CompareTo(b.Parcel.X)
            : static (a, b) => a.Parcel.Y.CompareTo(b.Parcel.Y));
        LandRectangle first = sources[0].Parcel;
        int extent = horizontal ? first.Width : first.Height;
        for (int i = 1; i < sources.Length; i++)
        {
            if (sources[i].Handle == sources[i - 1].Handle) { return new(LocalLayoutRefusal.DuplicateLot); }
            LandRectangle next = sources[i].Parcel;
            bool contiguous = horizontal
                ? next.Y == first.Y && next.Height == first.Height && next.X == first.X + extent
                : next.X == first.X && next.Width == first.Width && next.Y == first.Y + extent;
            if (!contiguous) { return new(LocalLayoutRefusal.Noncontiguous); }
            extent += horizontal ? next.Width : next.Height;
        }
        LandRectangle site = horizontal ? first with { Width = extent } : first with { Height = extent };
        if (!site.IsValid || !Contains(site, building.Footprint)) { return new(LocalLayoutRefusal.InvalidGeometry); }

        // Identity order determines retirement and which existing Address the new Lot retains.
        Array.Sort(sources, static (a, b) => a.Id.CompareTo(b.Id));
        LocalLot door = sources[0];
        for (int row = 0; row < world.Lots.Rows.SlotCount; row++)
        {
            if (!world.Lots.Rows.IsLive(row) || Includes(sources, world.Lots.Rows.At(row))) { continue; }
            LocalLot other = LocalLot.Read(world.Lots, row);
            if (Overlaps(site, other.Parcel)) { return new(LocalLayoutRefusal.Overlap); }
            if (other.East == door.East && other.North == door.North && other.Side == door.Side)
            {
                return new(LocalLayoutRefusal.AddressConflict);
            }
        }
        // Do not trust a stale reverse index when retiring source identities.
        for (int row = 0; row < world.Buildings.Rows.SlotCount; row++)
        {
            if (world.Buildings.Rows.IsLive(row) && Includes(sources, world.Buildings.Lot[row]))
            {
                return new(LocalLayoutRefusal.Occupied);
            }
        }

        ushort form = (ushort)IntegerMath.ShiftLeft(1, (int)building.Form);
        PermissionRefusal permission = world.LandPermissions.Check(site, LotTable.Housing, form, out byte band);
        if (permission != PermissionRefusal.None) { return new(LocalLayoutRefusal.Permission, permission); }
        if ((world.Rules.Band(band).Admits & LotTable.Housing) == 0) { return new(LocalLayoutRefusal.Intensity); }
        if (!BuildingPlan.TryFloorTiles(building.Form, building.Footprint.Width, building.Footprint.Height,
            building.Storeys, out int floor)) { return new(LocalLayoutRefusal.InvalidGeometry); }
        int occupancy = CapacityRuleset.Holds(floor, world.Rules.Capacity.FloorTilesPerOccupant);
        int housing = occupancy - (world.Rules.Kind(building.Kind).Business != 0 && occupancy > 1 ? 1 : 0);
        if (housing <= 0) { return new(LocalLayoutRefusal.NoHousingCapacity); }
        proposal = new LocalLayoutProposal(world, sources, building, site,
            world.Roads.Segments.Rows.At(segment), world.Roads.Segments.Epoch[segment], floor, occupancy, housing, band);
        return default;
    }

    internal static LocalLayoutCheck Revalidate(World world, LocalLayoutProposal proposal)
    {
        if (!ReferenceEquals(world, proposal.World) || !ReferenceEquals(world.Rules, proposal.Rules)
            || world.Tick != proposal.Tick || !world.Roads.Segments.Rows.TryResolve(proposal.Street, out int street)
            || world.Roads.Segments.Epoch[street] != proposal.StreetEpoch)
        {
            return new(LocalLayoutRefusal.StaleProposal);
        }
        foreach (LocalLot source in proposal.Sources)
        {
            if (!world.Lots.Rows.TryResolve(source.Handle, out int row)) { return new(LocalLayoutRefusal.StaleLot); }
            if (LocalLot.Read(world.Lots, row) != source) { return new(LocalLayoutRefusal.StaleProposal); }
        }
        LocalLayoutCheck check = Evaluate(world, (LocalLot[])proposal.Sources.Clone(), proposal.Building, out var current);
        if (!check.Accepted) { return check; }
        return current!.Site == proposal.Site && current.FloorTiles == proposal.FloorTiles
            && current.HousingCapacity == proposal.HousingCapacity && current.Band == proposal.Band
            ? default : new(LocalLayoutRefusal.StaleProposal);
    }

    private static bool Fronts(World world, LocalLot source, bool horizontal, int column, int row)
    {
        BlockGround block = BlockGround.At(world.Roads.Lattice, column, row);
        int gap = world.Rules.Lots.StreetHalfWidthTiles;
        LandRectangle p = source.Parcel;
        if (p.X < block.East + gap || p.Y < block.North + gap
            || p.X + p.Width > block.East + block.Wide - gap
            || p.Y + p.Height > block.North + block.Deep - gap) { return false; }
        bool left = source.Side == (byte)StreetSide.Left;
        return horizontal
            ? source.East >= p.X && source.East < p.X + p.Width
                && (left ? p.Y == source.North + gap : p.Y + p.Height == source.North - gap)
            : source.North >= p.Y && source.North < p.Y + p.Height
                && (left ? p.X + p.Width == source.East - gap : p.X == source.East + gap);
    }

    private static bool Includes(ReadOnlySpan<LocalLot> sources, Handle<Lot> lot)
    {
        foreach (LocalLot source in sources) { if (source.Handle == lot) { return true; } }
        return false;
    }

    private static bool Contains(LandRectangle outer, LandRectangle inner) => inner.X >= outer.X && inner.Y >= outer.Y
        && inner.X + inner.Width <= outer.X + outer.Width && inner.Y + inner.Height <= outer.Y + outer.Height;

    private static bool Overlaps(LandRectangle a, LandRectangle b) => b.IsValid && a.X < b.X + b.Width
        && b.X < a.X + a.Width && a.Y < b.Y + b.Height && b.Y < a.Y + a.Height;
}
