using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;

namespace Borough.Core.Rules;

public enum HousingSearchReason : byte { None, CoverageLimit, Capacity, Affordability, Suitable, Preference }

/// <summary>A read-only current comparison and the saved observation interval.</summary>
public readonly record struct HousingSearchReading(HousingSearchReason Current, bool Observed,
    bool Fresh, bool Persistent, ulong ElapsedTicks, ulong RequiredTicks, ulong LastObserved);

/// <summary>Bounded observations of current homes, separate from read-only construction matching.</summary>
public static class HousingSearchEvidence
{
    public static HousingSearchReading Read(World world, int position)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(position);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(position, world.UnplacedPool.Count);
        var pool = world.UnplacedPool;
        ulong first = pool.MismatchSince[position], last = pool.MismatchObserved[position];
        bool observed = pool.SearchReason[position] == (byte)HousingSearchReason.Preference;
        bool fresh = observed && first <= last && last <= world.Tick.Raw
            && world.Rules.HousingConstruction is { } rules
            && world.Tick.Raw - last <= (ulong)rules.PreferenceFreshnessTicks;
        var current = Current(world, world.Key, position);
        return new(current, observed, fresh, current == HousingSearchReason.Preference && Persistent(world, position),
            observed && first <= last ? last - first : 0,
            (ulong)(world.Rules.HousingConstruction?.PreferencePersistenceTicks ?? 0), last);
    }

    internal static HousingSearchReason Current(World world, WorldKey key, int position)
    {
        if (world.Rules.HousingConstruction is not { } rules) { return HousingSearchReason.None; }
        if (world.Buildings.Rows.SlotCount > rules.MaxBuildingSlots || world.Lots.Rows.SlotCount > rules.MaxLotSlots)
        { return HousingSearchReason.CoverageLimit; }
        int household = world.Households.Rows.Resolve(world.UnplacedPool.At(position));
        byte stage = world.Households.LifeStage[household];
        long taste = HousingUtility.Taste(world.Rules, key, world.Households.TasteIdentity(household), stage);
        int rentWeight = world.Rules.RentWeight(stage);
        int outside = 0;
        bool compares = world.Rules.Placement.Chooses
            && HousingUtility.TryOutside(world, position, taste, rentWeight, out outside);
        bool vacancy = false, affordable = false;
        int best = int.MinValue;
        for (int row = 0; row < world.Buildings.Rows.SlotCount; row++)
        {
            if (!world.Buildings.Rows.IsLive(row) || world.Buildings.IsAbandoned(row)
                || !world.HasRoomForHousehold(row)) { continue; }
            vacancy = true;
            byte kind = world.Buildings.Kind[row];
            if (!HousingUtility.Affordable(world, position, kind)) { continue; }
            affordable = true;
            if (!compares) { return HousingSearchReason.Suitable; }
            int lot = world.Lots.Rows.Resolve(world.Buildings.Lot[row]);
            int worth = HousingUtility.Worth(world.Rules.Placement, HousingUtility.Distance(world, lot),
                taste, world.Rules.Kind(kind).Rent, rentWeight);
            if (worth > best) { best = worth; }
        }
        if (!vacancy) { return HousingSearchReason.Capacity; }
        if (!affordable) { return HousingSearchReason.Affordability; }
        return (long)outside - best >= rules.PreferenceMargin
            ? HousingSearchReason.Preference : HousingSearchReason.Suitable;
    }

    internal static void Observe(World world, WorldKey key, int position, Ticks tick)
    {
        HousingSearchReason reason = Current(world, key, position);
        UnplacedTable pool = world.UnplacedPool;
        if (reason != HousingSearchReason.Preference)
        {
            pool.ClearSearch(position);
            pool.SearchReason[position] = (byte)reason;
            return;
        }
        ulong now = tick.Raw;
        if (pool.SearchReason[position] != (byte)reason || now < pool.MismatchObserved[position]
            || now - pool.MismatchObserved[position] > (ulong)world.Rules.HousingConstruction!.PreferenceFreshnessTicks)
        { pool.MismatchSince[position] = now; }
        pool.SearchReason[position] = (byte)reason;
        pool.MismatchObserved[position] = now;
    }

    internal static bool Persistent(World world, int position)
    {
        if (world.Rules.HousingConstruction is not { } rules) { return false; }
        UnplacedTable pool = world.UnplacedPool;
        ulong first = pool.MismatchSince[position], last = pool.MismatchObserved[position], now = world.Tick.Raw;
        return pool.SearchReason[position] == (byte)HousingSearchReason.Preference
            && first <= last && last <= now && now - last <= (ulong)rules.PreferenceFreshnessTicks
            && last - first >= (ulong)rules.PreferencePersistenceTicks;
    }
}
