using Borough.Core.Arithmetic;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Core.Entities;

public sealed partial class World
{
    // Which UtilityFlowTable row belongs to each District Pool row, slot-plus-one encoded so a
    // zeroed entry reads as absent. Derived, and outside the table for BOR0901's reason: it is
    // indexed by another table's slot rather than by its own row. Rebuilt whole by
    // RebuildUtilityFlows, which a load calls and which the fitting below calls after it allocates.
    private int[] _flowOfPool = [];

    // Recomputed at each settle and read nowhere else, so there is no stale state to rebuild.
    private readonly DistrictAdjacency _utilityAdjacency = new();

    // The settle's scratch, per District slot: its flow row, what it can export, what it is short
    // of, and what it ends up with. Grown to the District table's slot count and no further.
    private int[] _flowOfDistrict = [];
    private long[] _utilitySurplus = [];
    private long[] _utilityDeficit = [];
    private long[] _utilityServed = [];

    // The max-flow residual network, (Districts + 2)² entries with the source and the sink last.
    // A matrix rather than an edge list because the District count is small and a dense residual
    // needs no incidence structure to walk in slot order.
    private long[] _utilityResidual = [];
    private int[] _utilityParent = [];
    private int[] _utilityQueue = [];

    /// <summary>
    /// The <see cref="UtilityFlowTable"/> row for a Pool row, or <see cref="Rows.NoSlot"/>.
    /// </summary>
    public int UtilityFlowOfPool(int poolSlot) =>
        (uint)poolSlot < (uint)_flowOfPool.Length ? _flowOfPool[poolSlot] - 1 : Rows.NoSlot;

    /// <summary>
    /// The network a Rule Instance's <c>pool</c> term in <paramref name="utility"/> draws on, or
    /// <see cref="Rows.NoSlot"/> while its premises stand in no District.
    /// </summary>
    public int UtilityFlowOf(int instanceSlot, ResourceId utility)
    {
        int pool = RuleEngine.MarketRow(this, instanceSlot, utility);

        return pool == Rows.NoSlot ? Rows.NoSlot : UtilityFlowOfPool(pool);
    }

    /// <summary>
    /// What a District's network delivers of <paramref name="utility"/> for the current Day, or
    /// <see cref="Ratio.Zero"/> where it has no network for it.
    /// </summary>
    public Ratio DeliveredIn(int districtSlot, ResourceId utility)
    {
        int pool = Markets.Row(this, districtSlot, utility);

        if (pool == DistrictMarkets.NoRow)
        {
            return Ratio.Zero;
        }

        int flow = UtilityFlowOfPool(pool);

        return flow == Rows.NoSlot ? Ratio.Zero : UtilityFlows.Delivered[flow];
    }

    /// <summary>Rebuilds the Pool-to-network index from the rows' own saved handles.</summary>
    internal void RebuildUtilityFlows()
    {
        Grow(ref _flowOfPool, DistrictPools.Rows.SlotCount);
        Array.Clear(_flowOfPool);

        for (int slot = 0; slot < UtilityFlows.Rows.SlotCount; slot++)
        {
            if (UtilityFlows.Rows.IsLive(slot)
                && DistrictPools.Rows.TryResolve(UtilityFlows.Pool[slot], out int pool))
            {
                _flowOfPool[pool] = slot + 1;
            }
        }
    }

    /// <summary>
    /// Gives every delivered-Utility Pool row the network row it needs and does not already have.
    /// </summary>
    /// <remarks>
    /// Called from <see cref="FitDistrictPools"/>, after it has opened the Pool rows themselves, and
    /// it adds and never removes on that method's rule. A network closes when its District does, in
    /// <see cref="RetirePool"/>.
    /// </remarks>
    internal void FitUtilityFlows()
    {
        RebuildUtilityFlows();

        for (int pool = 0; pool < DistrictPools.Rows.SlotCount; pool++)
        {
            if (IsDeliveredUtilityPool(pool) && UtilityFlowOfPool(pool) == Rows.NoSlot)
            {
                Handle<UtilityFlow> opened = UtilityFlows.Create(DistrictPools.Rows.At(pool));

                _flowOfPool[pool] = UtilityFlows.Rows.Resolve(opened) + 1;
            }
        }
    }

    /// <summary>Whether a Pool row holds a delivered Utility rather than a traded Resource.</summary>
    internal bool IsDeliveredUtilityPool(int poolSlot) =>
        DistrictPools.Rows.IsLive(poolSlot)
        && Bins.Rows.TryResolve(DistrictPools.Bin[poolSlot], out int bin)
        && Rules.IsDeliveredUtility(Bins.Resource[bin]);

    /// <summary>Closes the network a dying District's Pool row carried, if it had one.</summary>
    /// <remarks>
    /// There is nothing to hand an heir. A delivered Utility has no stock in this slice, and what
    /// these rows hold is one Day's flow through a network that no longer exists.
    /// </remarks>
    internal void RetireUtilityFlow(int poolSlot)
    {
        int flow = UtilityFlowOfPool(poolSlot);

        if (flow == Rows.NoSlot)
        {
            return;
        }

        UtilityFlows.Rows.Free(UtilityFlows.Rows.At(flow));
        _flowOfPool[poolSlot] = 0;
    }

    /// <summary>
    /// Settles every delivered Utility's network for the Day: what each District made, what it
    /// needed, what crossed its boundaries, and the fraction its consumers get next.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Once a Day, at the head of the Tick beside <see cref="RollPopulationDayFlows"/>, so a Day's
    /// firings all run against one fraction. Per District: production less the loss its road length
    /// incurs, local use first, then an integer max-flow of the remaining surplus to the remaining
    /// deficits over the District adjacency graph, with each link carrying
    /// <c>crossings × link_per_crossing</c>. Multi-hop, because a relay District's links are edges
    /// of the same network.
    /// </para>
    /// <para>
    /// Nothing is woken. A delivered-Utility term never blocks — <c>RuleEngine.Check</c> serves it
    /// short instead — so no Rule Instance is ever asleep on one of these rows, and a Rule that
    /// fired at zero applications re-armed on its own rate.
    /// </para>
    /// <para>
    /// Unused supply is lost and the fraction is recomputed from scratch, so a Day that produced
    /// nothing delivers nothing however large yesterday's surplus was.
    /// </para>
    /// </remarks>
    internal void SettleUtilityNetworks()
    {
        if (UtilityFlows.Rows.LiveCount == 0)
        {
            return;
        }

        _utilityAdjacency.Compute(this);

        for (int raw = 1; raw <= Rules.ResourceCount; raw++)
        {
            var resource = new ResourceId((ushort)raw);

            if (Rules.IsDeliveredUtility(resource))
            {
                SettleUtility(resource);
            }
        }
    }

    private void SettleUtility(ResourceId utility)
    {
        int districts = Districts.Rows.SlotCount;

        Grow(ref _flowOfDistrict, districts);
        Grow(ref _utilitySurplus, districts);
        Grow(ref _utilityDeficit, districts);
        Grow(ref _utilityServed, districts);

        long loss = Rules.LossPerRoadTile(utility);
        bool anySurplus = false;
        bool anyDeficit = false;

        for (int district = 0; district < districts; district++)
        {
            _flowOfDistrict[district] = Rows.NoSlot;
            _utilitySurplus[district] = 0;
            _utilityDeficit[district] = 0;
            _utilityServed[district] = 0;

            if (!Districts.Rows.IsLive(district))
            {
                continue;
            }

            int pool = Markets.Row(this, district, utility);
            int flow = pool == DistrictMarkets.NoRow ? Rows.NoSlot : UtilityFlowOfPool(pool);

            if (flow == Rows.NoSlot)
            {
                // A District the fitting has not reached yet. It still relays, because a link is a
                // property of the road graph rather than of the Pool.
                continue;
            }

            _flowOfDistrict[district] = flow;

            long demanded = UtilityFlows.Demanded[flow];
            long net = UtilityFlows.Produced[flow] - (loss * _utilityAdjacency.RoadTilesOf(district));

            if (net < 0)
            {
                net = 0;
            }

            long local = net < demanded ? net : demanded;

            _utilityServed[district] = local;
            _utilitySurplus[district] = net - local;
            _utilityDeficit[district] = demanded - local;

            anySurplus |= _utilitySurplus[district] > 0;
            anyDeficit |= _utilityDeficit[district] > 0;
        }

        if (anySurplus && anyDeficit)
        {
            Transmit(utility, districts);
        }

        for (int district = 0; district < districts; district++)
        {
            int flow = _flowOfDistrict[district];

            if (flow == Rows.NoSlot)
            {
                continue;
            }

            long demanded = UtilityFlows.Demanded[flow];
            long served = _utilityServed[district];

            // A District nobody draws in is fully supplied: there is no shortfall to report, and a
            // fraction of zero there would read as an outage on every consumer raised in it.
            UtilityFlows.Delivered[flow] = demanded <= 0
                ? Ratio.One
                : new Ratio((int)IntegerMath.MulDivFloor(Fixed.One, served, demanded));

            UtilityFlows.Supplied[flow] = UtilityFlows.Produced[flow];
            UtilityFlows.Asked[flow] = demanded;
            UtilityFlows.Received[flow] = served;
            UtilityFlows.Produced[flow] = 0;
            UtilityFlows.Demanded[flow] = 0;
        }
    }

    /// <summary>
    /// Moves surplus to deficit over the District adjacency graph — integer Edmonds–Karp from a
    /// source joined to every surplus District to a sink joined from every deficit one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The breadth-first search visits neighbors in ascending slot order and the source and the sink
    /// are the last two nodes, so the augmenting path chosen among equal-length candidates is a
    /// function of the slot numbering alone.
    /// </para>
    /// <para>
    /// ⚠ It picks one optimal split when total supply is short, with no fairness across Districts.
    /// A deficit that happens to lie on a shorter augmenting path is served first.
    /// </para>
    /// </remarks>
    private void Transmit(ResourceId utility, int districts)
    {
        int source = districts;
        int sink = districts + 1;
        int nodes = districts + 2;

        Grow(ref _utilityResidual, nodes * nodes);
        Grow(ref _utilityParent, nodes);
        Grow(ref _utilityQueue, nodes);

        // The whole indexed region, because a previous settle may have used a different stride.
        Array.Clear(_utilityResidual, 0, nodes * nodes);

        long perCrossing = Rules.LinkPerCrossing(utility);

        foreach (DistrictAdjacency.Link link in _utilityAdjacency.Links)
        {
            // Both directions at full capacity, which is how an undirected edge is spelled in a
            // residual network: pushing f one way leaves c − f forward and c + f back.
            long capacity = link.Crossings * perCrossing;

            _utilityResidual[(link.Lower * nodes) + link.Higher] = capacity;
            _utilityResidual[(link.Higher * nodes) + link.Lower] = capacity;
        }

        for (int district = 0; district < districts; district++)
        {
            _utilityResidual[(source * nodes) + district] = _utilitySurplus[district];
            _utilityResidual[(district * nodes) + sink] = _utilityDeficit[district];
        }

        while (Augment(nodes, source, sink))
        {
        }

        for (int district = 0; district < districts; district++)
        {
            // What the sink edge carried is what this District took off the network.
            _utilityServed[district] +=
                _utilityDeficit[district] - _utilityResidual[(district * nodes) + sink];
        }
    }

    /// <summary>Pushes one bottleneck along the shortest augmenting path, or answers that none is left.</summary>
    private bool Augment(int nodes, int source, int sink)
    {
        for (int node = 0; node < nodes; node++)
        {
            _utilityParent[node] = Rows.NoSlot;
        }

        _utilityParent[source] = source;

        int head = 0;
        int tail = 0;
        _utilityQueue[tail++] = source;
        bool reached = false;

        while (head < tail && !reached)
        {
            int at = _utilityQueue[head++];

            for (int next = 0; next < nodes; next++)
            {
                if (_utilityParent[next] != Rows.NoSlot
                    || _utilityResidual[(at * nodes) + next] <= 0)
                {
                    continue;
                }

                _utilityParent[next] = at;

                if (next == sink)
                {
                    reached = true;
                    break;
                }

                _utilityQueue[tail++] = next;
            }
        }

        if (!reached)
        {
            return false;
        }

        long pushed = long.MaxValue;

        for (int at = sink; at != source; at = _utilityParent[at])
        {
            long capacity = _utilityResidual[(_utilityParent[at] * nodes) + at];

            if (capacity < pushed)
            {
                pushed = capacity;
            }
        }

        for (int at = sink; at != source; at = _utilityParent[at])
        {
            int from = _utilityParent[at];

            _utilityResidual[(from * nodes) + at] -= pushed;
            _utilityResidual[(at * nodes) + from] += pushed;
        }

        return true;
    }

    private static void Grow<T>(ref T[] buffer, int count)
    {
        if (buffer.Length < count)
        {
            int doubled = buffer.Length * 2;

            Array.Resize(ref buffer, doubled > count ? doubled : count);
        }
    }
}
