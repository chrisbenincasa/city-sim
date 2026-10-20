using Borough.Core.Arithmetic;
using Borough.Core.Invariants;
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

    // A reserve Utility's scratch, per District slot: what its tower Bins held when the settle
    // opened, what they can hold, the room they had left after the Day's drain, what the Day drew
    // out of them for consumers here, and what the refill put back.
    private long[] _utilityReserveLevel = [];
    private long[] _utilityReserveCapacity = [];
    private long[] _utilityReserveSpace = [];
    private long[] _utilityDrained = [];
    private long[] _utilityFilled = [];
    private long[] _utilityFromReserve = [];

    // What a pass opened a District's source and sink edges at, so the pass that follows can read
    // back what each one carried, and what pass 1 left of each District's own production.
    private long[] _utilitySurplusLeft = [];
    private long[] _utilityOffered = [];
    private long[] _utilityWanted = [];

    // One District's proportional split, per District slot. The basis total is what its tower Bins
    // hold or can hold, exactly rather than saturated at UtilityFlowTable.Bound, because it divides
    // every share and a ceiling on it would skew the split instead of understating a counter. Int128
    // because enough Bins at adr/0065's permitted ceiling overflow a long between them.
    private Int128[] _towerBasisWhole = [];
    private long[] _towerLeftToPlace = [];

    // The tower Bins of the Utility being settled, in ascending Bin slot order, with the District
    // each one stands in. Recomputed at each settle: membership follows the Bins and the watershed,
    // and nothing outside the settle reads it.
    private int[] _reserveBins = [];
    private int[] _reserveBinDistricts = [];
    private int _reserveBinCount;

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

    /// <summary>Totals a District's current reserve from Building-owned Bins without changing it.</summary>
    /// <remarks>Totals saturate at UtilityFlowTable.Bound, like the settle's reserve totals.</remarks>
    public (long Level, long Capacity) ReadUtilityReserve(int districtSlot, ResourceId utility)
    {
        long level = 0;
        long capacity = 0;

        for (int bin = 0; bin < Bins.Rows.SlotCount; bin++)
        {
            if (!Bins.Rows.IsLive(bin)
                || Bins.Resource[bin] != utility
                || Bins.OwnerKind[bin] != BinOwnerKind.Building
                || !Buildings.Rows.TryResolve(Bins.Owner[bin], out int building)
                || !Lots.Rows.TryResolve(Buildings.Lot[building], out int lot)
                || !Districts.Rows.TryResolve(
                    DistrictsInCells.Of(
                        DistrictCells,
                        CellGrid.ToCells(Lots.East[lot]),
                        CellGrid.ToCells(Lots.North[lot])),
                    out int district)
                || district != districtSlot)
            {
                continue;
            }

            level = UtilityFlowTable.Total(level, Bins.LevelAt(bin), 1);
            capacity = UtilityFlowTable.Total(capacity, Bins.Capacity[bin], 1);
        }

        return (level, capacity);
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
    /// a delivered Utility's Pool holds none — <c>adr/0139</c> makes a Pool a market rather than a
    /// store, and a reserve Utility's carry-over stands in Building Bins, which this does not touch.
    /// Left behind, the row is an undelivered Resource that
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
    /// There is nothing to hand an heir. The network row holds no stock — a reserve Utility's stock
    /// stands in Building Bins and stays there as a plain local Bin — and what this row holds is one
    /// Day's flow through a network that no longer exists.
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
    /// <b>A Utility declaring <c>storage = "reserve"</c> has three more steps, and
    /// <see cref="TransmitReserves"/> is where they are.</b> Its carry-over stands in the Bins of the
    /// Buildings holding it rather than in any row here, so the settle drains those Bins to cover
    /// what production could not and refills them with what the Day had left over.
    /// </para>
    /// <para>
    /// Nothing on a network row is woken. A delivered-Utility term never blocks —
    /// <c>RuleEngine.Check</c> serves it short instead — so no Rule Instance is ever asleep on one of
    /// these rows, and a Rule that fired at zero applications re-armed on its own rate. ⚠ <b>A tower
    /// Bin is an ordinary Bin and its wait lists are drained</b>, because a Rule may hold a
    /// <c>local</c> term on a reserve Utility; <see cref="Deposit"/> and <see cref="Withdraw"/> are
    /// what the reserve moves through.
    /// </para>
    /// <para>
    /// Supply no reserve absorbs is lost and the fraction is recomputed from scratch, so a Day that
    /// produced nothing delivers nothing beyond what its reserves hold, however large yesterday's
    /// surplus was.
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
        Grow(ref _utilityOwed, districts);
        Grow(ref _utilitySurplus, districts);
        Grow(ref _utilityDeficit, districts);
        Grow(ref _utilityServed, districts);

        bool reserve = Rules.HasReserve(utility);

        if (reserve)
        {
            CollectReserves(utility, districts);
        }

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
            // is served. Standing reserve capacity counts as running one: stored water leaks along
            // the roads whether or not anybody is drawing on it.
            bool running = demanded > 0
                || produced > 0
                || (reserve && _utilityReserveCapacity[district] > 0);

            long tiles = running ? _utilityAdjacency.RoadTilesOf(district) : 0;
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

        if (reserve)
        {
            // The reserve passes are worth opening a residual for even with nothing to transmit: a
            // District's own towers can cover its own deficit, and its own production can refill them.
            if (anySurplus || anyDeficit || _reserveBinCount > 0)
            {
                TransmitReserves(utility, districts);
            }
        }
        else if (anySurplus && anyDeficit)
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

            // Written on every settle rather than only on a reserve Utility's, so a reload that drops
            // the storage mode leaves no figure standing from the Rules that are gone.
            UtilityFlows.FromReserve[flow] = reserve
                ? UtilityFlowTable.Total(0, _utilityFromReserve[district], 1)
                : 0;
            UtilityFlows.ToReserve[flow] = reserve
                ? UtilityFlowTable.Total(0, _utilityFilled[district], 1)
                : 0;

            UtilityFlows.Produced[flow] = 0;
            UtilityFlows.Demanded[flow] = 0;
        }
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
        int nodes = OpenResidual(utility, districts);
        int source = districts;
        int sink = districts + 1;

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
    /// Clears the residual network, lays the links in it, and answers how many nodes it has. The
    /// source and the sink are the last two, with no capacity on either until a pass gives them one.
    /// </summary>
    private int OpenResidual(ResourceId utility, int districts)
    {
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

        return nodes;
    }

    /// <summary>
    /// Settles a Utility that keeps a reserve: production, then the reserves, then the refill, on one
    /// residual network.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Five steps in the order <c>plans/utilities.md</c> sets, three of them passes of
    /// <see cref="Augment"/>.</b> Production covers local need and then crosses the links; each
    /// District's own reserve covers what is left of its own deficit, before any link; a reserve with
    /// anything left over exports it over whatever link capacity production did not use; the Day's
    /// leftover production refills the District's own towers; and what is still left reaches a
    /// neighbor's towers over the links. Anything after that is lost.
    /// </para>
    /// <para>
    /// <b>⚠ No pass can take back what an earlier one delivered.</b> An augmenting path leaves the
    /// source on a source edge and enters the sink on a sink edge, both of them forward, because the
    /// search expands neither node — so flow on those edges only ever grows, whatever a later pass
    /// reroutes between them.
    /// </para>
    /// <para>
    /// <b>The source and sink capacities are rewritten between passes and the link capacities are
    /// not.</b> Each pass asks a different question of the same network, and what the links have left
    /// is the previous pass's answer. Rewriting a source or sink edge is safe for the reason above:
    /// the flow already on it stands in the reverse entry, which no path can reach.
    /// </para>
    /// <para>
    /// <b>No District is both a source and a sink in the same pass</b>, because the local step before
    /// each one takes the smaller of the two and leaves at most one standing.
    /// </para>
    /// </remarks>
    private void TransmitReserves(ResourceId utility, int districts)
    {
        int nodes = OpenResidual(utility, districts);
        int source = districts;
        int sink = districts + 1;

        Grow(ref _utilitySurplusLeft, districts);
        Grow(ref _utilityOffered, districts);
        Grow(ref _utilityWanted, districts);

        // Pass 1: production to deficit, which is the whole of what a Utility with no reserve does.
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
            _utilityServed[district] +=
                _utilityDeficit[district] - _utilityResidual[(district * nodes) + sink];
            _utilitySurplusLeft[district] = _utilityResidual[(source * nodes) + district];
        }

        // A District's own reserve covers what is left of its own deficit, before any link is used.
        for (int district = 0; district < districts; district++)
        {
            long want = _utilityResidual[(district * nodes) + sink];
            long held = _utilityReserveLevel[district];
            long drawn = held < want ? held : want;

            if (drawn <= 0)
            {
                continue;
            }

            _utilityResidual[(district * nodes) + sink] = want - drawn;
            _utilityDrained[district] += drawn;
            _utilityServed[district] += drawn;
            _utilityFromReserve[district] += drawn;
        }

        // Pass 2: what the reserves still hold, over whatever link capacity pass 1 left free. The
        // source edges carry reserve now rather than production, so what each one gives up is
        // unambiguously an export from that District's towers.
        for (int district = 0; district < districts; district++)
        {
            _utilityOffered[district] =
                _utilityReserveLevel[district] - _utilityDrained[district];
            _utilityWanted[district] = _utilityResidual[(district * nodes) + sink];
            _utilityResidual[(source * nodes) + district] = _utilityOffered[district];
        }

        while (Augment(nodes, source, sink))
        {
        }

        for (int district = 0; district < districts; district++)
        {
            long taken = _utilityWanted[district] - _utilityResidual[(district * nodes) + sink];

            _utilityDrained[district] +=
                _utilityOffered[district] - _utilityResidual[(source * nodes) + district];
            _utilityServed[district] += taken;
            _utilityFromReserve[district] += taken;
        }

        DrainReserves(districts);
        MeasureReserveSpace(districts);

        // The Day's leftover production refills its own District's towers first.
        for (int district = 0; district < districts; district++)
        {
            long room = _utilityReserveSpace[district];
            long left = _utilitySurplusLeft[district];
            long filled = left < room ? left : room;

            if (filled <= 0)
            {
                continue;
            }

            _utilitySurplusLeft[district] = left - filled;
            _utilityFilled[district] += filled;
        }

        // Pass 3: the rest of the production into the rest of the reserve space. The sink edges carry
        // room now rather than unserved demand — a consumer this Day could not reach gets nothing,
        // and what would have gone to it fills a tower instead.
        for (int district = 0; district < districts; district++)
        {
            _utilityWanted[district] =
                _utilityReserveSpace[district] - _utilityFilled[district];
            _utilityResidual[(source * nodes) + district] = _utilitySurplusLeft[district];
            _utilityResidual[(district * nodes) + sink] = _utilityWanted[district];
        }

        while (Augment(nodes, source, sink))
        {
        }

        for (int district = 0; district < districts; district++)
        {
            _utilityFilled[district] +=
                _utilityWanted[district] - _utilityResidual[(district * nodes) + sink];
        }

        FillReserves(districts);
    }

    /// <summary>
    /// Finds the tower Bins of one reserve Utility and totals what each District's towers hold and
    /// could hold.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A tower is a Building-owned Bin of the Utility, and its District is its Lot's</b> — the
    /// chain <c>RuleEngine.MarketRow</c> walks, because a Building carries no District handle and must
    /// not. The list comes out in ascending Bin slot order, which is what makes the splits below
    /// reproducible across a save and a reload.
    /// </para>
    /// <para>
    /// <b>⚠ The totals saturate at <see cref="UtilityFlowTable.Bound"/></b>, because they become
    /// source and sink capacities in the residual matrix and that is the headroom it is sized for. A
    /// saturated total is below the true one, so the settle offers less than the towers hold and the
    /// splits stay exact.
    /// </para>
    /// </remarks>
    private void CollectReserves(ResourceId utility, int districts)
    {
        Grow(ref _utilityReserveLevel, districts);
        Grow(ref _utilityReserveCapacity, districts);
        Grow(ref _utilityReserveSpace, districts);
        Grow(ref _utilityDrained, districts);
        Grow(ref _utilityFilled, districts);
        Grow(ref _utilityFromReserve, districts);

        Array.Clear(_utilityReserveLevel, 0, districts);
        Array.Clear(_utilityReserveCapacity, 0, districts);
        Array.Clear(_utilityReserveSpace, 0, districts);
        Array.Clear(_utilityDrained, 0, districts);
        Array.Clear(_utilityFilled, 0, districts);
        Array.Clear(_utilityFromReserve, 0, districts);

        _reserveBinCount = 0;

        for (int bin = 0; bin < Bins.Rows.SlotCount; bin++)
        {
            if (!Bins.Rows.IsLive(bin)
                || Bins.Resource[bin] != utility
                || Bins.OwnerKind[bin] != BinOwnerKind.Building
                || !Buildings.Rows.TryResolve(Bins.Owner[bin], out int building)
                || !Lots.Rows.TryResolve(Buildings.Lot[building], out int lot))
            {
                continue;
            }

            if (!Districts.Rows.TryResolve(
                    DistrictsInCells.Of(
                        DistrictCells,
                        CellGrid.ToCells(Lots.East[lot]),
                        CellGrid.ToCells(Lots.North[lot])),
                    out int district))
            {
                continue;
            }

            Grow(ref _reserveBins, _reserveBinCount + 1);
            Grow(ref _reserveBinDistricts, _reserveBinCount + 1);

            _reserveBins[_reserveBinCount] = bin;
            _reserveBinDistricts[_reserveBinCount] = district;
            _reserveBinCount++;

            _utilityReserveLevel[district] = UtilityFlowTable.Total(
                _utilityReserveLevel[district], Bins.LevelAt(bin), 1);
            _utilityReserveCapacity[district] = UtilityFlowTable.Total(
                _utilityReserveCapacity[district], Bins.Capacity[bin], 1);
        }
    }

    /// <summary>Totals the room each District's towers have left, after the Day's drain.</summary>
    /// <remarks>
    /// ⚠ <b>Taken after the drain rather than with the levels</b>, because draining a tower is what
    /// makes room in it, and a refill measured before the drain would leave the Day's own production
    /// outside a reserve it had just emptied.
    /// </remarks>
    private void MeasureReserveSpace(int districts)
    {
        Array.Clear(_utilityReserveSpace, 0, districts);

        for (int at = 0; at < _reserveBinCount; at++)
        {
            int district = _reserveBinDistricts[at];

            _utilityReserveSpace[district] = UtilityFlowTable.Total(
                _utilityReserveSpace[district], BasisOf(_reserveBins[at], draining: false), 1);
        }
    }

    /// <summary>
    /// Takes each District's drain out of its tower Bins, in proportion to what each one holds.
    /// </summary>
    private void DrainReserves(int districts) =>
        SpreadAcrossTowers(districts, _utilityDrained, draining: true);

    /// <summary>Puts each District's refill into its tower Bins, in proportion to their room.</summary>
    private void FillReserves(int districts) =>
        SpreadAcrossTowers(districts, _utilityFilled, draining: false);

    /// <summary>
    /// Moves one total per District through that District's tower Bins, in proportion to what each
    /// Bin holds when draining or to the room it has when filling.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A floor share per Bin, then one unit each in ascending Bin slot order.</b> That places the
    /// remainder without a second division and without depending on anything but the slot numbering.
    /// The remainder is below the count of Bins with a basis, because a sum of floors is above the sum
    /// less that count; and every one of those Bins keeps at least one unit of headroom unless the
    /// District moves its whole basis, because <c>floor(total × basis / whole)</c> reaches
    /// <c>basis</c> only when the total reaches the whole. So the unit the second sweep takes is
    /// always there.
    /// </para>
    /// <para>
    /// ⚠ <b>The divisor is the District's exact basis total and not
    /// <see cref="UtilityFlowTable.Total"/>'s saturated one.</b> A Bin's ceiling may be any positive
    /// <see cref="long"/> (<c>adr/0065</c>), so a District's towers can hold more than
    /// <see cref="UtilityFlowTable.Bound"/> between them — and dividing by the bound rather than the
    /// truth makes every share the whole total, which hands the lowest slot everything and the rest
    /// nothing. <see cref="Int128"/> carries the sum, which <see cref="long"/> cannot for enough Bins
    /// at that ceiling, and <see cref="IntegerMath.MulDivFloor(long, long, Int128)"/> is the floor
    /// division that takes it.
    /// </para>
    /// <para>
    /// <b>Through <see cref="Withdraw"/> and <see cref="Deposit"/> rather than into the level</b>,
    /// which is what keeps <see cref="Invariant.BinLevelIsWithinCapacity"/> checked at the write and
    /// drains the Bin's wait list. A Rule may hold a <c>local</c> term on a reserve Utility, so
    /// neither list is provably empty.
    /// </para>
    /// </remarks>
    private void SpreadAcrossTowers(int districts, long[] totals, bool draining)
    {
        Grow(ref _towerBasisWhole, districts);
        Grow(ref _towerLeftToPlace, districts);

        Array.Clear(_towerBasisWhole, 0, districts);
        Array.Copy(totals, _towerLeftToPlace, districts);

        // Read before anything moves. Withdrawing from a Bin changes both its level and its room, and
        // the divisor has to be the basis the settle decided its totals against.
        for (int at = 0; at < _reserveBinCount; at++)
        {
            _towerBasisWhole[_reserveBinDistricts[at]] += BasisOf(_reserveBins[at], draining);
        }

        for (int at = 0; at < _reserveBinCount; at++)
        {
            int district = _reserveBinDistricts[at];
            int bin = _reserveBins[at];
            long basis = BasisOf(bin, draining);

            if (totals[district] <= 0 || basis <= 0)
            {
                continue;
            }

            long share = IntegerMath.MulDivFloor(
                totals[district], basis, _towerBasisWhole[district]);

            if (share > _towerLeftToPlace[district])
            {
                share = _towerLeftToPlace[district];
            }

            if (share <= 0)
            {
                continue;
            }

            MoveTowerStock(bin, share, draining);
            _towerLeftToPlace[district] -= share;
        }

        for (int at = 0; at < _reserveBinCount; at++)
        {
            int district = _reserveBinDistricts[at];
            int bin = _reserveBins[at];

            if (_towerLeftToPlace[district] <= 0 || BasisOf(bin, draining) <= 0)
            {
                continue;
            }

            MoveTowerStock(bin, 1, draining);
            _towerLeftToPlace[district]--;
        }

        for (int district = 0; district < districts; district++)
        {
            Invariants.Require(
                _towerLeftToPlace[district] == 0,
                Invariant.UtilityReserveIsConserved,
                district,
                _towerLeftToPlace[district]);
        }
    }

    /// <summary>Moves one share out of a tower Bin, or into it.</summary>
    private void MoveTowerStock(int bin, long share, bool draining)
    {
        if (draining)
        {
            Withdraw(Bins.Rows.At(bin), share, Tick);
        }
        else
        {
            Deposit(Bins.Rows.At(bin), share, Tick);
        }
    }

    /// <summary>What one tower Bin's share is proportional to — its level, or the room it has left.</summary>
    /// <remarks>
    /// A retuned ceiling can land under a Bin's level, which <c>BinTable.SpaceAt</c> answers as a
    /// negative. That Bin bleeds down on its own and takes no part in a refill.
    /// </remarks>
    private long BasisOf(int bin, bool draining)
    {
        if (draining)
        {
            return Bins.LevelAt(bin);
        }

        long room = Bins.SpaceAt(bin);

        return room > 0 ? room : 0;
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
