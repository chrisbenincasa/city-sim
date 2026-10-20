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

    // The settle's scratch, per District slot: its flow row, what its roads lose, what it can
    // export, what it is short of, and what it ends up with. Grown to the District table's slot
    // count and no further.
    private int[] _flowOfDistrict = [];
    private long[] _utilityOwed = [];
    private long[] _utilitySurplus = [];
    private long[] _utilityDeficit = [];
    private long[] _utilityServed = [];

    // The import pass's scratch, per District slot: what the gates brought it, what the treasury
    // paid for that, and what its sink edge still carried when the previous gate was done.
    private long[] _utilityImported = [];
    private long[] _utilityImportPaid = [];
    private long[] _utilityShort = [];

    // The import sources, one entry per Outside Connection standing on an edge whose Hinterland
    // prices the Utility, sorted by ascending price and then by the gate's monotonic Building id.
    // Parallel arrays rather than a struct because the sort is an insertion sort over three
    // columns.
    private int[] _importGate = [];
    private int[] _importDistrict = [];
    private long[] _importPrice = [];

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

    /// <summary>
    /// Closes the networks of a Utility the Ruleset in force no longer delivers, and the Pool rows
    /// that addressed them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A reload is the only thing that can un-deliver a Utility</b>, so this runs from
    /// <see cref="Adopt"/> and nowhere else. The rows have to go rather than merely stop being read,
    /// because <see cref="SettleUtilityNetworks"/> walks the delivered Resources — a retired
    /// Utility's counters and its delivered fraction stand still for as long as the edit lasts and
    /// then serve a Day of production that predates it. ***A re-delivered Utility is a new network
    /// and opens at nothing delivered.***
    /// </para>
    /// <para>
    /// <b>The Pool row goes with it, which is the one place <see cref="FitDistrictPools"/>' "adds and
    /// never removes" does not hold.</b> That rule protects the stock standing in a Good's Pool, and
    /// a delivered Utility has none — <c>storage</c> is 0 and <c>adr/0139</c> makes a Pool a market
    /// rather than a store. Left behind, the row is an undelivered Resource that
    /// <see cref="RepriceDistrictPools"/> no longer skips, so it reprices against a zero import
    /// ceiling and runs away from below, which is what <c>adr/0050</c> names.
    /// </para>
    /// </remarks>
    internal void RetireUndeliveredNetworks()
    {
        RebuildUtilityFlows();

        bool retired = false;

        for (int pool = DistrictPools.Rows.SlotCount - 1; pool >= 0; pool--)
        {
            if (UtilityFlowOfPool(pool) == Rows.NoSlot || IsDeliveredUtilityPool(pool))
            {
                continue;
            }

            RetireUtilityFlow(pool);

            if (Bins.Rows.TryResolve(DistrictPools.Bin[pool], out int bin))
            {
                // Before the Free, on RetirePool's order, because a waiter left on a freed Bin is a
                // Rule Instance pointing at a recycled row.
                WakeAll(bin, Tick);
                Expiries.Close(Bins, bin);
                Bins.Rows.Free(Bins.Rows.At(bin));
            }

            DistrictPools.Rows.Free(DistrictPools.Rows.At(pool));
            retired = true;
        }

        if (retired)
        {
            Markets.Invalidate();
        }
    }

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
    /// firings all run against one fraction. Per District: the loss its road length incurs is added
    /// to what its consumers asked for, local production covers that total first, and then an
    /// integer max-flow carries the remaining surplus to the remaining shortfalls over the District
    /// adjacency graph, with each link carrying <c>crossings × link_per_crossing</c>. Multi-hop,
    /// because a relay District's links are edges of the same network.
    /// </para>
    /// <para>
    /// <b>The loss is a first claim on imports as much as on local production</b>, which is what
    /// makes road length cost a District that generates nothing. Charged against production alone
    /// it misses an importing District entirely, and stops growing inside a producing one the
    /// moment its roads outrun its plants.
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
    /// <para>
    /// <b>Imports come last, after the local cover and the transfer</b>, because a plant's output is
    /// already paid for and an imported unit is not. What each Outside Connection brings in is
    /// bounded by <c>import_per_connection</c> and by what the treasury can pay at the gate's price.
    /// </para>
    /// </remarks>
    /// <returns>What the treasury paid the Outside for imported units.</returns>
    internal Money SettleUtilityNetworks()
    {
        if (UtilityFlows.Rows.LiveCount == 0)
        {
            return Money.Zero;
        }

        _utilityAdjacency.Compute(this);

        long paid = 0;

        for (int raw = 1; raw <= Rules.ResourceCount; raw++)
        {
            var resource = new ResourceId((ushort)raw);

            if (Rules.IsDeliveredUtility(resource))
            {
                paid += SettleUtility(resource).Raw;
            }
        }

        return new Money(paid);
    }

    private Money SettleUtility(ResourceId utility)
    {
        int districts = Districts.Rows.SlotCount;

        Grow(ref _flowOfDistrict, districts);
        Grow(ref _utilityOwed, districts);
        Grow(ref _utilitySurplus, districts);
        Grow(ref _utilityDeficit, districts);
        Grow(ref _utilityServed, districts);
        Grow(ref _utilityImported, districts);
        Grow(ref _utilityImportPaid, districts);
        Grow(ref _utilityShort, districts);

        long loss = Rules.LossPerRoadTile(utility);
        bool anySurplus = false;
        bool anyDeficit = false;

        for (int district = 0; district < districts; district++)
        {
            _flowOfDistrict[district] = Rows.NoSlot;
            _utilityOwed[district] = 0;
            _utilitySurplus[district] = 0;
            _utilityDeficit[district] = 0;
            _utilityServed[district] = 0;
            _utilityImported[district] = 0;
            _utilityImportPaid[district] = 0;

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
            long produced = UtilityFlows.Produced[flow];

            // A District with neither a consumer nor a plant runs no network, so its roads lose
            // nothing. One with either owes its whole road length, and owes it before anybody in it
            // is served.
            long tiles = demanded > 0 || produced > 0 ? _utilityAdjacency.RoadTilesOf(district) : 0;
            long owed = UtilityFlowTable.Total(0, loss, tiles);
            long need = UtilityFlowTable.Total(demanded, loss, tiles);
            long local = produced < need ? produced : need;

            _utilityOwed[district] = owed;
            _utilityServed[district] = local;
            _utilitySurplus[district] = produced - local;
            _utilityDeficit[district] = need - local;

            anySurplus |= _utilitySurplus[district] > 0;
            anyDeficit |= _utilityDeficit[district] > 0;
        }

        // A deficit with no surplus anywhere still reaches the solver when a gate can import into
        // it, because the import pass runs over the residual network this leaves behind.
        int gates = anyDeficit ? ListImportGates(utility) : 0;
        long paid = 0;

        if (anyDeficit && (anySurplus || gates > 0))
        {
            Transmit(utility, districts);

            if (gates > 0)
            {
                paid = Import(utility, districts, gates);
            }
        }

        for (int district = 0; district < districts; district++)
        {
            int flow = _flowOfDistrict[district];

            if (flow == Rows.NoSlot)
            {
                continue;
            }

            long demanded = UtilityFlows.Demanded[flow];

            // What the consumers get is what the network covered less what the roads took, and the
            // roads are paid first — so a District covered to a tenth of its loss delivers nothing
            // rather than delivering a negative share.
            long served = _utilityServed[district] - _utilityOwed[district];

            if (served < 0)
            {
                served = 0;
            }

            // A District nobody draws in is fully supplied: there is no shortfall to report, and a
            // fraction of zero there would read as an outage on every consumer raised in it.
            UtilityFlows.Delivered[flow] = demanded <= 0
                ? Ratio.One
                : Share(served, demanded);

            UtilityFlows.Supplied[flow] = UtilityFlows.Produced[flow];
            UtilityFlows.Asked[flow] = demanded;
            UtilityFlows.Received[flow] = served;
            UtilityFlows.Imported[flow] = _utilityImported[district];
            UtilityFlows.ImportPaid[flow] = new Money(_utilityImportPaid[district]);
            UtilityFlows.Produced[flow] = 0;
            UtilityFlows.Demanded[flow] = 0;
        }

        return new Money(paid);
    }

    /// <summary>What a District was served as a fraction of what it asked for, capped at whole.</summary>
    private static Ratio Share(long served, long demanded)
    {
        long share = IntegerMath.MulDivFloor(Fixed.One, served, demanded);

        return share >= Fixed.One ? Ratio.One : new Ratio((int)share);
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
            // residual network: pushing f one way leaves c − f forward and c + f back. Saturated at
            // the counters' own bound, so the pair of residual entries cannot pass long.MaxValue.
            long capacity = UtilityFlowTable.Total(0, link.Crossings, perCrossing);

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

    /// <summary>
    /// Lists the Outside Connections that can import <paramref name="utility"/>, cheapest first.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A gate is a source attached to the District holding its Lot.</b> A gate standing in no
    /// District imports nothing — there is no node for the units to arrive at, and carriage inside
    /// the city is the links' business rather than the gate's.
    /// </para>
    /// <para>
    /// <b>A gate's price is the cheapest a Hinterland on its edge charges</b>, which is
    /// <c>Ruleset.ImportCeiling</c>'s rule narrowed to one edge. The loader admits one Hinterland
    /// per edge, so the minimum is over one in every file that loads; it is written as a minimum
    /// because the price is read off the edge rather than off a declaration order.
    /// </para>
    /// <para>
    /// Sorted by ascending price and then by the gate's monotonic Building id, so the cheapest
    /// units are bought first and equal-priced gates are bought in the order they were raised.
    /// ⚠ <b>The id and not the slot</b>: a slot is recycled, so a gate raised today can hold the
    /// slot a demolished one left and would otherwise be bought before an older gate.
    /// </para>
    /// </remarks>
    /// <returns>How many entries of the import arrays are live.</returns>
    private int ListImportGates(ResourceId utility)
    {
        if (Rules.ImportPerConnection(utility) <= 0)
        {
            return 0;
        }

        int count = 0;

        for (int edge = 0; edge < HinterlandTable.Edges; edge++)
        {
            if (!TryEdgeImportPrice(utility, HinterlandTable.EdgeAt(edge), out long price))
            {
                continue;
            }

            foreach (int gate in Buildings.Gates(Hinterlands).Walk(edge))
            {
                if (!Lots.Rows.TryResolve(Buildings.Lot[gate], out int lot))
                {
                    continue;
                }

                Handle<District> holding = DistrictsInCells.Of(
                    DistrictCells,
                    CellGrid.ToCells(Lots.East[lot]),
                    CellGrid.ToCells(Lots.North[lot]));

                if (!Districts.Rows.TryResolve(holding, out int district))
                {
                    continue;
                }

                Grow(ref _importGate, count + 1);
                Grow(ref _importDistrict, count + 1);
                Grow(ref _importPrice, count + 1);

                int at = count;
                ulong raised = Buildings.Rows.IdAt(gate);

                while (at > 0
                    && (_importPrice[at - 1] > price
                        || (_importPrice[at - 1] == price
                            && Buildings.Rows.IdAt(_importGate[at - 1]) > raised)))
                {
                    _importPrice[at] = _importPrice[at - 1];
                    _importGate[at] = _importGate[at - 1];
                    _importDistrict[at] = _importDistrict[at - 1];
                    at--;
                }

                _importPrice[at] = price;
                _importGate[at] = gate;
                _importDistrict[at] = district;
                count++;
            }
        }

        return count;
    }

    /// <summary>What the Outside charges for <paramref name="utility"/> behind one map edge.</summary>
    private bool TryEdgeImportPrice(ResourceId utility, MapEdge edge, out long price)
    {
        price = 0;
        bool priced = false;

        for (int hinterland = 0; hinterland < Rules.Hinterlands.Length; hinterland++)
        {
            if (Rules.Hinterlands[hinterland].Edge != edge)
            {
                continue;
            }

            long offered = Rules.ImportPrice(hinterland, utility).Raw;

            if (offered > 0 && (!priced || offered < price))
            {
                price = offered;
                priced = true;
            }
        }

        return priced;
    }

    /// <summary>
    /// Buys the shortfall the links could not cover at the Outside Connections, cheapest gate
    /// first, and pays for it out of the treasury.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A second pass over the residual network <see cref="Transmit"/> left.</b> The links carry
    /// what pass 1 did not use, each District's sink edge holds what it is still short of, and the
    /// source is cleared of the local surplus in both residual directions — so an import covers a
    /// shortfall rather than displacing a unit a plant had already covered, and what arrives is
    /// attributable to the gate that sold it.
    /// </para>
    /// <para>
    /// <b>A gate's flow is capped at what the treasury can still pay at that gate's price</b>, and
    /// the shortfall is not owed later: there is no debt until the borrowing row designs one, which
    /// is <c>World.SpendOnUpkeep</c>'s rule. ⚠ <b>An empty treasury imports nothing</b> rather than
    /// importing on credit, because the Outside gives nothing away and the alternative is a money
    /// supply that stops balancing.
    /// </para>
    /// <para>
    /// The withdrawal and <see cref="PayOutside"/> are paired here and never left to the caller, so
    /// the Bin and <c>MoneySupply.Issued</c> move by the same amount in the same call
    /// (<c>adr/0024</c>).
    /// </para>
    /// </remarks>
    /// <returns>What the treasury paid.</returns>
    private long Import(ResourceId utility, int districts, int gates)
    {
        int bin = TryMoneyResource(out ResourceId money) ? FindTreasuryBin(money) : Rows.NoSlot;
        long purse = bin == Rows.NoSlot ? 0 : Bins.LevelAt(bin);

        if (purse <= 0)
        {
            return 0;
        }

        int source = districts;
        int sink = districts + 1;
        int nodes = districts + 2;
        long capacity = Rules.ImportPerConnection(utility);

        for (int district = 0; district < districts; district++)
        {
            _utilityResidual[(source * nodes) + district] = 0;
            _utilityResidual[(district * nodes) + source] = 0;
            _utilityShort[district] = _utilityResidual[(district * nodes) + sink];
        }

        long paid = 0;

        for (int entry = 0; entry < gates; entry++)
        {
            long price = _importPrice[entry];
            long affordable = IntegerMath.FloorDiv(purse, price);
            long allowed = capacity < affordable ? capacity : affordable;

            if (allowed <= 0)
            {
                // The gates are in ascending price order, so no gate behind this one is cheaper
                // and none of them is affordable either.
                break;
            }

            int gateDistrict = _importDistrict[entry];

            _utilityResidual[(source * nodes) + gateDistrict] = allowed;

            while (Augment(nodes, source, sink))
            {
            }

            // Cleared in both directions before the next gate, so one gate's unused capacity and
            // the flow it did push cannot be spent again at another gate's price.
            _utilityResidual[(source * nodes) + gateDistrict] = 0;
            _utilityResidual[(gateDistrict * nodes) + source] = 0;

            for (int district = 0; district < districts; district++)
            {
                long left = _utilityResidual[(district * nodes) + sink];
                long taken = _utilityShort[district] - left;

                if (taken <= 0)
                {
                    continue;
                }

                long charge = taken * price;

                _utilityShort[district] = left;
                _utilityServed[district] += taken;
                _utilityImported[district] += taken;
                _utilityImportPaid[district] += charge;
                purse -= charge;
                paid += charge;
            }
        }

        if (paid > 0)
        {
            // Through Withdraw rather than out of the level, on SpendOnPlacement's reason: a Bin
            // written without draining its wait list strands whoever was waiting for the space.
            Withdraw(Bins.Rows.At(bin), paid, Tick);
            PayOutside(new Money(paid));
        }

        return paid;
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
