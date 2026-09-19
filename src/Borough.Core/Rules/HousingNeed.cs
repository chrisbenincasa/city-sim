using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Core.Rules;

public enum HousingNeedRefusal : byte { None, Disabled, InvalidSite, CoverageLimit, NoNeed, ExcessCapacity }

/// <summary>Distinct current seekers, never an extrapolation from the sample to the city.</summary>
public readonly record struct HousingNeed(HousingNeedRefusal Refusal, int Assessed, int Uncovered, int Served)
{
    public bool Accepted => Refusal == HousingNeedRefusal.None;
}

/// <summary>
/// Synchronous scratch matching. Each usable tenancy covers at most one sampled Household; an
/// augmenting path prevents a flexible seeker from stealing the only home a poorer seeker can use.
/// Nothing is placed or reserved. Every assessment reads actual capacity again.
/// </summary>
public static class HousingNeedAssessment
{
    public static HousingNeed Evaluate(World world, WorldKey key, LocalLayoutProposal proposal) =>
        Evaluate(world, key, proposal, null);

    internal static HousingNeed Evaluate(World world, WorldKey key, LocalLayoutProposal first, LocalLayoutProposal? second)
    {
        HousingConstructionRuleset? rules = world.Rules.HousingConstruction;
        if (rules is null) { return new(HousingNeedRefusal.Disabled, 0, 0, 0); }
        // Slot high-water marks bound complete coverage, including dead slots. A missed home is
        // never evidence that no home exists. Check before allocating or rebuilding any index.
        if (world.Buildings.Rows.SlotCount > rules.MaxBuildingSlots || world.Lots.Rows.SlotCount > rules.MaxLotSlots)
        { return new(HousingNeedRefusal.CoverageLimit, 0, 0, 0); }
        if (!LocalLayout.Revalidate(world, first).Accepted || second is not null && !LocalLayout.Revalidate(world, second).Accepted)
        { return new(HousingNeedRefusal.InvalidSite, 0, 0, 0); }
        int count = world.UnplacedPool.Count;
        if (count > rules.MaxSeekers) { count = rules.MaxSeekers; }
        if (count == 0) { return new(HousingNeedRefusal.NoNeed, 0, 0, 0); }
        var matching = new Assessment(world, key, count);
        return matching.Run(rules, first, second);
    }

    internal sealed class Assessment
    {
        private readonly World _world;
        private readonly WorldKey _key;
        private LocalLayoutProposal _first = null!;
        private LocalLayoutProposal? _second;
        private readonly int[] _positions, _homes, _capacity, _assigned;
        private readonly bool[] _eligible, _visited;
        private int _homeCount;

        internal Assessment(World world, WorldKey key, int count)
        {
            _world = world; _key = key;
            _positions = new int[count]; _assigned = new int[count]; _eligible = new bool[count];
            _homes = new int[world.Buildings.Rows.SlotCount]; _capacity = new int[_homes.Length];
            _visited = new bool[_homes.Length];
            ulong draw = Randomness.Draw(key, 0, world.Tick, PurposeTag.HousingConstructionSeekers);
            int start = (int)(draw % (ulong)(uint)world.UnplacedPool.Count);
            for (int i = 0; i < count; i++)
            {
                _positions[i] = (int)(((long)start + i) % world.UnplacedPool.Count);
            }
            for (int row = 0; row < world.Buildings.Rows.SlotCount; row++)
            {
                if (!world.Buildings.Rows.IsLive(row) || world.Buildings.IsAbandoned(row)
                    || !world.HasRoomForHousehold(row)) { continue; }
                world.TryDeclaredOccupancy(world.Buildings.Kind[row], row, out int ceiling);
                int free = ceiling - world.Tenants(row);
                if (free <= 0) { continue; }
                _homes[_homeCount] = row;
                _capacity[_homeCount++] = free < count ? free : count;
            }
        }

        internal HousingNeed Run(HousingConstructionRuleset rules, LocalLayoutProposal firstPlan, LocalLayoutProposal? secondPlan = null)
        {
            _first = firstPlan; _second = secondPlan;
            Array.Fill(_assigned, Rows.NoSlot);
            for (int seeker = 0; seeker < _positions.Length; seeker++)
            { _eligible[seeker] = Suitable(seeker, firstPlan) || secondPlan is not null && Suitable(seeker, secondPlan); }
            // Maximise existing coverage of those who would actually use this proposed site.
            // Other seekers cannot turn a usable vacancy into evidence for construction here.
            for (int seeker = 0; seeker < _positions.Length; seeker++)
            {
                if (!_eligible[seeker]) { continue; }
                Array.Clear(_visited);
                Match(seeker);
            }
            int uncovered = 0;
            for (int seeker = 0; seeker < _positions.Length; seeker++)
            { if (_eligible[seeker] && _assigned[seeker] == Rows.NoSlot) { uncovered++; } }
            int capacity = _first.HousingCapacity + (_second?.HousingCapacity ?? 0);
            if (uncovered == 0) { return new(HousingNeedRefusal.NoNeed, _positions.Length, 0, 0); }
            if (capacity > rules.Supported(uncovered))
            { return new(HousingNeedRefusal.ExcessCapacity, _positions.Length, uncovered, 0); }
            // Two proposed homes may appeal to different people. Match their remaining capacities
            // too, rather than counting one seeker once for each Building in an arrangement.
            int firstOnly = 0, secondOnly = 0, both = 0;
            for (int seeker = 0; seeker < _positions.Length; seeker++)
            {
                if (!_eligible[seeker] || _assigned[seeker] != Rows.NoSlot) { continue; }
                bool a = Suitable(seeker, _first), b = _second is not null && Suitable(seeker, _second);
                if (a && b) { both++; } else if (a) { firstOnly++; } else { secondOnly++; }
            }
            int first = Min(firstOnly, _first.HousingCapacity);
            int second = Min(secondOnly, _second?.HousingCapacity ?? 0);
            int served = first + second + Min(both, capacity - first - second);
            return new(HousingNeedRefusal.None, _positions.Length, uncovered, served);
        }

        private bool Suitable(int seeker, LocalLayoutProposal proposal) => HousingUtility.Suitable(_world, _key,
            _positions[seeker], _world.Lots.Rows.Resolve(proposal.Sources[0].Handle), proposal.Building.Kind, true);

        private bool Match(int seeker)
        {
            for (int home = 0; home < _homeCount; home++)
            {
                if (_visited[home]) { continue; }
                int building = _homes[home];
                int lot = _world.Lots.Rows.Resolve(_world.Buildings.Lot[building]);
                if (!HousingUtility.Suitable(_world, _key, _positions[seeker], lot, _world.Buildings.Kind[building], false)) { continue; }
                _visited[home] = true;
                int used = 0;
                for (int other = 0; other < _assigned.Length; other++) { if (_assigned[other] == home) { used++; } }
                if (used < _capacity[home]) { _assigned[seeker] = home; return true; }
                for (int other = 0; other < _assigned.Length; other++)
                {
                    if (_assigned[other] == home && Match(other)) { _assigned[seeker] = home; return true; }
                }
            }
            return false;
        }
        private static int Min(int a, int b) => a < b ? a : b;
    }
}
