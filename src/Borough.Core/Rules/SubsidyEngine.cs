// SPDX-License-Identifier: MIT
namespace Borough.Core.Rules;

using Borough.Core.Arithmetic;
using Borough.Core.Entities;
using Borough.Core.Quantities;

/// <summary>
/// What one Day's subsidies claimed, what the treasury could fund, and how many recipients were cut.
/// </summary>
/// <param name="Claimed">What every eligible recipient asked for, before any ceiling was applied.</param>
/// <param name="Paid">What actually left the treasury.</param>
/// <param name="Claimants">Recipients who asked for something.</param>
/// <param name="Rationed">
/// Recipients who received less than they claimed. ⚠ <b>The shortfall creates no debt</b>
/// (<c>plans/0072</c> D12), so this is the only trace a short pot leaves.
/// </param>
public readonly record struct SubsidyReading(long Claimed, long Paid, int Claimants, int Rationed);

/// <summary>
/// <b>Pays the city's subsidies out of the treasury, once a Day, rationed by each one's ceiling.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>A subsidy cannot be written as a <c>[[policy]]</c> transfer and that is why this type
/// exists</b> (<c>plans/0072</c> D33). <c>PolicyEngine.Move</c> pays each member in full or not at
/// all, and abandons the rest of the sweep the moment the treasury cannot cover somebody — so who
/// gets paid is decided by where the rotating scan happened to start. ***D12 refuses exactly that***:
/// when claims exceed the funding, every claimant is cut in proportion.
/// </para>
/// <para>
/// ⚠ <b>Two passes, because the cut cannot be known until every claim is in.</b> The first pass
/// gathers claims and the second pays them, and no Money moves between the two. A single pass would
/// have to guess the total, which is the all-or-nothing behaviour arriving again wearing a
/// proportion.
/// </para>
/// <para>
/// ⚠ <b>The ceiling is not a reservation.</b> It bounds what this subsidy may pay on a Day; whether
/// the Money is in the treasury is asked separately, and the pot is the smaller of the two. ***A
/// fully funded subsidy can still pay nothing***, which is what D12 means by support being explicitly
/// subject to funding.
/// </para>
/// </remarks>
/// <param name="world">The world being swept.</param>
public sealed class SubsidyEngine(World world)
{
    private readonly World _world = world;

    private long[] _claims = new long[world.Businesses.Rows.Capacity];
    private long[] _awards = new long[world.Businesses.Rows.Capacity];
    private int[] _claimant = new int[world.Businesses.Rows.Capacity];

    private MoneyFlow _paidFlow;

    /// <summary>What has left the treasury as subsidy since the last reading.</summary>
    public MoneyFlow DrainPaid()
    {
        MoneyFlow flow = _paidFlow;

        _paidFlow = default;

        return flow;
    }

    /// <summary>Pays every subsidy due on this Tick.</summary>
    public SubsidyReading Sweep(Ticks tick)
    {
        long claimed = 0;
        long paid = 0;
        int claimants = 0;
        int rationed = 0;

        for (int policy = 0; policy < _world.Rules.Policies.Length; policy++)
        {
            ref readonly PolicyDefinition definition = ref _world.Rules.Policies[policy];

            if (definition.Tool != PolicyTool.Subsidy)
            {
                continue;
            }

            if (definition.Interval == 0 || tick.Raw % definition.Interval != 0UL)
            {
                continue;
            }

            SubsidyReading one = Pay(policy, definition, tick);

            claimed += one.Claimed;
            paid += one.Paid;
            claimants += one.Claimants;
            rationed += one.Rationed;
        }

        _paidFlow = _paidFlow.Fold(paid);

        return new SubsidyReading(claimed, paid, claimants, rationed);
    }

    private SubsidyReading Pay(int policy, in PolicyDefinition definition, Ticks tick)
    {
        long rate = _world.Policies.AmountOf(policy, definition);

        if (rate <= 0)
        {
            return default;
        }

        int treasury = _world.FindTreasuryBin(definition.Resource);

        if (treasury == Tables.Rows.NoSlot)
        {
            return default;
        }

        int count = Gather(definition, rate, out long claimed);

        if (count == 0)
        {
            return default;
        }

        long ceiling = _world.Policies.CeilingOf(policy, definition);
        long held = _world.Bins.LevelAt(treasury);
        long pot = ceiling < held ? ceiling : held;

        if (pot <= 0)
        {
            return new SubsidyReading(claimed, 0, count, count);
        }

        Apportionment.Apportion(
            _claims.AsSpan(0, count), pot, _awards.AsSpan(0, count));

        long paid = 0;
        int rationed = 0;

        for (int at = 0; at < count; at++)
        {
            long award = _awards[at];

            if (award < _claims[at])
            {
                rationed++;
            }

            if (award <= 0)
            {
                continue;
            }

            int till = _claimant[at];

            _world.Withdraw(_world.Bins.Rows.At(treasury), award, tick);
            _world.Deposit(_world.Bins.Rows.At(till), award, tick);

            paid += award;
        }

        return new SubsidyReading(claimed, paid, count, rationed);
    }

    /// <summary>Makes room for every Business the world now holds.</summary>
    /// <remarks>
    /// 🔴 <b>The row count outgrows the capacity this engine was built at, and a save is how.</b> The
    /// allocator grows on demand, so a <c>Simulation</c> rebuilt on a reloaded world is sized to the
    /// rows that world had when it was written and then watches it grow — which is an index past the
    /// end of these three arrays on the first Day a claim list is longer than the world used to be.
    /// ***Sizing once at construction was a defect and not an optimisation***: the scratch is
    /// per-sweep, and a sweep runs once a Day.
    /// </remarks>
    private void MakeRoom(int slots)
    {
        if (_claims.Length >= slots)
        {
            return;
        }

        _claims = new long[slots];
        _awards = new long[slots];
        _claimant = new int[slots];
    }

    private int Gather(in PolicyDefinition definition, long rate, out long claimed)
    {
        MakeRoom(_world.Businesses.Rows.SlotCount);

        int count = 0;

        claimed = 0;

        for (int slot = 0; slot < _world.Businesses.Rows.SlotCount; slot++)
        {
            if (!_world.Businesses.Rows.IsLive(slot))
            {
                continue;
            }

            if (definition.Trade != TradeKind.Any && _world.Businesses.Kind[slot] != definition.Trade)
            {
                continue;
            }

            int workers = 0;

            foreach (int _ in _world.Workers.Walk(slot))
            {
                workers++;
            }

            if (workers == 0)
            {
                continue;
            }

            // A trade with nowhere to receive Money is not a claimant. Counting it would ration the
            // pot against a claim nobody could ever be paid, quietly shrinking what every real
            // claimant received -- BusinessTaxEngine's tilless case seen from the paying side.
            if (!_world.Bins.Rows.TryResolve(_world.Businesses.Balance[slot], out int till))
            {
                continue;
            }

            _claims[count] = rate * workers;
            _claimant[count] = till;

            claimed += _claims[count];
            count++;
        }

        return count;
    }
}
