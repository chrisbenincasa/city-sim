using Borough.Core.Entities;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Core.Rules;

/// <summary>Rebuilt qualified vacancies within a Lot's walking Commute Budget.</summary>
/// <remarks>
/// Input snapshots invalidate the counts when vacancies, credentials, doors, roads or Rules change.
/// Arrival choice prepares only its sampled Lots before scoring. Storage is bounded by current
/// Lot and Business slot counts and carries no saved state.
/// </remarks>
public sealed class JobAccessibility
{
    private readonly WalkScratch _walk = new();
    private Address[] _doors = [];
    private Vacancy[] _employers = [];
    private long[] _counts = [];
    private bool[] _prepared = [];
    private int[] _buildings = [];
    private Ruleset? _rules;
    private uint _roadVersion;
    private int _lots;
    private int _businesses;
    private bool _hasJobs;

    private readonly record struct Vacancy(Address Door, int Posts, byte Tier);

    /// <summary>Work in the most recent preparation or full refresh; zero on a cache hit.</summary>
    public int Searches { get; private set; }
    public long Settled { get; private set; }
    public long TotalSearches { get; private set; }
    public long TotalSettled { get; private set; }

    /// <summary>Reads a prepared Lot's count; a tier includes every lower credential.</summary>
    public long Count(int lot, byte tier) => tier == 0 || lot < 0 || lot >= _lots
        ? 0 : _counts[(lot * 3) + (tier > 3 ? 2 : tier - 1)];

    /// <summary>Discards snapshots and reconstructs all counts after restoring authoritative state.</summary>
    public void Rebuild(World world)
    {
        _rules = null;
        Refresh(world);
    }

    /// <summary>Reconstructs the whole field for cold rebuilds and reference checks.</summary>
    public void Refresh(World world)
    {
        if (!Snapshot(world)) { return; }
        for (int lot = 0; lot < _lots; lot++) { PrepareLot(world, lot); }
    }

    /// <summary>Prepares only the sampled origins against the current authoritative inputs.</summary>
    /// <remarks>Repeated Lots and unchanged snapshots reuse exact counts without walking again.</remarks>
    public void Prepare(World world, ReadOnlySpan<int> lots)
    {
        if (!Snapshot(world)) { return; }
        foreach (int lot in lots) { PrepareLot(world, lot); }
    }

    private bool Snapshot(World world)
    {
        Searches = 0;
        Settled = 0;
        if (world.Rules.Placement.JobsRent == 0) { return false; }

        int lots = world.Lots.Rows.SlotCount;
        int businesses = world.Businesses.Rows.SlotCount;
        bool changed = _rules != world.Rules || _roadVersion != world.Roads.Version
            || lots != _lots || businesses != _businesses;
        if (_doors.Length != lots)
        {
            _doors = new Address[lots];
            _counts = new long[checked(lots * 3)];
            _prepared = new bool[lots];
        }
        if (_employers.Length != businesses) { _employers = new Vacancy[businesses]; }

        for (int lot = 0; lot < lots; lot++)
        {
            Address door = world.Lots.Rows.IsLive(lot) ? world.Lots.AddressOf(lot) : Address.None;
            changed |= _doors[lot] != door;
            _doors[lot] = door;
        }
        for (int business = 0; business < businesses; business++)
        {
            Vacancy vacancy = default;
            if (world.Businesses.Rows.IsLive(business) && world.HasJob(business)
                && world.Buildings.Rows.TryResolve(world.Businesses.Building[business], out int building))
            {
                vacancy = new Vacancy(world.PedestrianAccessPoint(building),
                    world.DeclaredJobs(business) - world.Workers.Length(business),
                    world.Rules.BusinessKind(world.Businesses.Kind[business]).RequiresTier);
            }
            changed |= _employers[business] != vacancy;
            _employers[business] = vacancy;
        }
        if (!changed) { return _hasJobs; }

        _rules = world.Rules;
        _roadVersion = world.Roads.Version;
        _lots = lots;
        _businesses = businesses;
        _counts.AsSpan().Clear();
        _prepared.AsSpan().Clear();
        _hasJobs = false;
        for (int business = 0; business < businesses; business++)
        { _hasJobs |= _employers[business].Posts > 0; }
        return _hasJobs;
    }

    private void PrepareLot(World world, int lot)
    {
        if ((uint)lot >= (uint)_lots || _prepared[lot]) { return; }
        _prepared[lot] = true;

        TripRuleset trips = world.Rules.Trips;
        Address door = _doors[lot];
        if (!WalkRouting.Flood(world.Roads, TravelMode.Foot, door, trips.CommuteBudget, _walk))
        { return; }
        Searches++;
        Settled += _walk.Relaxed;
        TotalSearches++;
        TotalSettled += _walk.Relaxed;

        Cells radius = EmploymentEngine.Radius(trips.CommuteBudget, world.Rules.Roads.WalkSpeed);
        if (_buildings.Length < world.Buildings.Rows.LiveCount)
        { _buildings = new int[world.Buildings.Rows.LiveCount]; }
        CellRect box = CellRect.At(CellGrid.ToCells(world.Lots.East[lot]),
            CellGrid.ToCells(world.Lots.North[lot])).Dilate(radius).Clamp();
        int nearby = world.BuildingsInCells.In(box, world.Buildings, _buildings);
        for (int candidate = 0; candidate < nearby; candidate++)
            foreach (int business in world.BuildingBusinesses.Walk(_buildings[candidate]))
            {
                Vacancy vacancy = _employers[business];
                if (vacancy.Posts == 0 || !trips.WithinBudget(WalkRouting.CostFromFlood(
                        world.Roads, TravelMode.Foot, door, vacancy.Door, trips.CrossingCost, _walk)))
                { continue; }
                for (int tier = vacancy.Tier > 1 ? vacancy.Tier : 1; tier <= 3; tier++)
                { _counts[(lot * 3) + tier - 1] += vacancy.Posts; }
            }
    }
}
