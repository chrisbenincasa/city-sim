using S2.Routing.Graph;
using S2.Routing.Matrix;

namespace S2.Routing.Traffic;

/// <summary>
/// Whether the vector carries DSDV's sequence numbers, or Citybound's nothing.
/// </summary>
/// <remarks>
/// <c>references.md</c> is explicit — <i>"if we adopt distance-vector routing, we take DSDV's
/// version, not Citybound's"</i> — and gives the reason: Citybound's entries carry
/// <c>distance</c>, <c>distance_hops</c>, <c>outgoing_idx</c> and <c>learned_from</c> and no
/// sequence numbers, so link deletion count-to-infinities. <b>Under <c>adr/0043</c> that is a
/// measurable claim wearing an argued one's clothes</b>: it names a failure, a trigger and a
/// mechanism, so it has a refuting number and R4 is the machine. Both rungs are built, and the
/// unsequenced one is here to make the correctness column move — R3 established that a column which
/// cannot move is not evidence, and this spike has now caught two instruments that could not.
/// </remarks>
internal enum VectorProtocol
{
    /// <summary>DSDV. Entries carry a sequence number; a broken link is advertised with an odd one.</summary>
    Sequenced,

    /// <summary>Citybound's. Cost and next hop, nothing to order two claims about the same destination by.</summary>
    Unsequenced,
}

/// <summary>What one repair cost, and whether it arrived anywhere correct.</summary>
/// <param name="Rounds">Vector-exchange rounds, or Dijkstra settles for the repair rung.</param>
/// <param name="Relaxations">Arc relaxations performed. The work figure, independent of the clock.</param>
/// <param name="Converged">False if the round cap was hit — which is itself the finding.</param>
internal readonly record struct RepairCost(int Rounds, long Relaxations, bool Converged);

/// <summary>
/// A next-hop table under the four maintenance schemes, and the sequence numbers one of them needs.
/// </summary>
/// <remarks>
/// <para>
/// <b>The structure is R2's next-hop table and R4 is not proposing a different one.</b> R2 built it
/// from the attribution side — <c>adr/0041</c> needs a Traveller to know the next Segment it enters
/// every Tick, which is a next hop and not a path — and measured it at 0 ns to start a Trip and 32 ns
/// per crossing. R3 then found that no cluster size fits a per-Trip search into the Tick budget and
/// named this table as <i>"the rung this arithmetic does not touch… a structural advantage over both
/// hierarchies rather than a faster constant, and it is R4's to press."</i>
/// </para>
/// <para>
/// <b>So R4's subject is not which router. It is whether the one structure that fits the budget can
/// be kept current.</b> Its build costs 474.47 ms — thirty Ticks — and the corpus's core verb is
/// deleting a road. Distance-vector is one answer to that and the one <c>plans/0010</c> names; it is
/// not the only one, and pricing only the named candidate is how a spike produces a verdict it has
/// not earned. Four schemes are measured against the same edits:
/// </para>
/// <list type="bullet">
/// <item><b>Rebuild</b> — every column, from scratch. R2's figure, and the baseline every other rung divides by.</item>
/// <item><b>DSDV, sequenced</b> — the protocol <c>references.md</c> specifies.</item>
/// <item><b>DSDV, unsequenced</b> — Citybound's, present to move the correctness column.</item>
/// <item><b>Dynamic repair</b> — invalidate the affected subtree, re-derive it from its valid boundary. Not distance-vector at all, and the strongest competitor.</item>
/// </list>
/// <para>
/// A fifth, <b>rolling refresh</b>, needs none of this machinery — it is the rebuild amortised across
/// Ticks — and is measured in the report against staleness rather than against an edit.
/// </para>
/// <para>
/// <b>Layout is destination-major, matching R2's table and for R2's stated reason:</b> following one
/// route is then a strided walk rather than a random one. R1 measured that cliff on this machine.
/// </para>
/// </remarks>
internal sealed class DistanceVector
{
    /// <summary>No route known. Kept below <c>int.MaxValue</c> so a relaxation cannot overflow.</summary>
    public const int Unreachable = int.MaxValue >> 2;

    private readonly RoadGraph _graph;
    private readonly ReverseArcs _reverse;
    private readonly int _nodes;
    private readonly int _destinations;

    private readonly int[] _cost;
    private readonly int[] _nextArc;
    private readonly int[] _sequence;

    // Scratch, one entry per node, reused across columns. Generation-stamped rather than cleared:
    // clearing 16,697 ints per column per repair would put the clear inside the figure being measured.
    private readonly int[] _stamp;
    private readonly int[] _scratch;
    private readonly int[] _queue;
    private readonly bool[] _queued;

    // The flood's two frontier buffers, hoisted. Allocating them per call would put ~133 KB of
    // churn per column per edit inside the figures this task exists to measure, and a repair cost
    // that is partly a garbage collection is not a repair cost.
    private readonly int[] _frontierA;
    private readonly int[] _frontierB;

    // The audit's memo. Three states rather than a bool, because "not yet known" and "known not to
    // reach" are different answers and collapsing them would make the memo unusable.
    private const byte Unknown = 0;
    private const byte Reaches = 1;
    private const byte Stranded = 2;

    private readonly byte[] _reaches;
    private readonly int[] _onPath;
    private int _generation;

    private int[] _heapKey = new int[4096];
    private int[] _heapNode = new int[4096];
    private int _heapCount;

    public DistanceVector(RoadGraph graph, ReverseArcs reverse, int destinations)
    {
        _graph = graph;
        _reverse = reverse;
        _nodes = graph.Nodes;
        _destinations = destinations;

        long entries = (long)destinations * graph.Nodes;
        if (entries > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(destinations), "table exceeds one array");
        }

        _cost = new int[entries];
        _nextArc = new int[entries];
        _sequence = new int[entries];

        _stamp = new int[graph.Nodes];
        _scratch = new int[graph.Nodes];
        _queue = new int[graph.Nodes];
        _queued = new bool[graph.Nodes];
        _frontierA = new int[graph.Nodes];
        _frontierB = new int[graph.Nodes];
        _reaches = new byte[graph.Nodes];
        _onPath = new int[graph.Nodes];

        Array.Fill(_cost, Unreachable);
        Array.Fill(_nextArc, -1);
    }

    public int Destinations => _destinations;

    /// <summary>
    /// <c>destinations × nodes × (cost + next arc + sequence)</c>.
    /// </summary>
    /// <remarks>
    /// <b>This is the axis <c>plans/0010</c> calls "the frightening one" and the tripwire it names:</b>
    /// <i>"DSDV's routing tables exceed the whole world's 172.3 MiB footprint → distance-vector is out
    /// on memory alone."</i> The sequence numbers are a third of it, so the protocol
    /// <c>references.md</c> insists on costs 50% more than the table R2 measured — and what that
    /// third buys is measured here rather than assumed.
    /// </remarks>
    public long ResidentBytes => (long)_cost.Length * 3 * sizeof(int);

    /// <summary>The same figure without sequence numbers, for the rung that does not carry them.</summary>
    public long UnsequencedResidentBytes => (long)_cost.Length * 2 * sizeof(int);

    public int Cost(int node, int destination) => _cost[(destination * _nodes) + node];

    public int Of(int node, int destination) => _nextArc[(destination * _nodes) + node];

    /// <summary>
    /// Seeds one column to its converged state by backward Dijkstra, with an even sequence number.
    /// </summary>
    /// <remarks>
    /// <b>DSDV's claim is about repair, not about cold start, and conflating the two would flatter
    /// it.</b> A protocol converging from nothing by vector exchange takes rounds proportional to the
    /// graph's diameter; the same answer by Dijkstra is one settle per node. So every scheme starts
    /// from the same converged table, computed the same way, and what is measured is what each does
    /// to it afterwards. <see cref="ConvergeCold"/> measures the other thing separately, because
    /// "DSDV cannot cold-start economically" is a finding rather than a reason not to look.
    /// </remarks>
    public void Seed(int destination, int target, int[] arcTicks)
    {
        int column = destination * _nodes;

        for (int node = 0; node < _nodes; node++)
        {
            _cost[column + node] = Unreachable;
            _nextArc[column + node] = -1;
            _sequence[column + node] = 0;
        }

        if (target < 0)
        {
            return;
        }

        _generation++;
        _heapCount = 0;

        _cost[column + target] = 0;
        Push(0, target);

        while (_heapCount > 0)
        {
            (int key, int node) = Pop();

            if (_stamp[node] == _generation || key > _cost[column + node])
            {
                continue;
            }

            _stamp[node] = _generation;

            for (int slot = _reverse.Start[node]; slot < _reverse.Start[node + 1]; slot++)
            {
                int arc = _reverse.Arc[slot];
                int step = arcTicks[arc];
                if (step == RoadGraph.Impassable)
                {
                    continue;
                }

                int from = _reverse.Source[arc];
                if (_stamp[from] == _generation)
                {
                    continue;
                }

                int candidate = key + step;
                if (candidate >= _cost[column + from])
                {
                    continue;
                }

                _cost[column + from] = candidate;
                _nextArc[column + from] = arc;
                Push(candidate, from);
            }
        }
    }

    /// <summary>
    /// Converges one column from nothing by synchronous vector exchange, the way the protocol
    /// actually runs. Returns the rounds and relaxations it took.
    /// </summary>
    public RepairCost ConvergeCold(int destination, int target, int[] arcTicks, int roundCap)
    {
        int column = destination * _nodes;

        for (int node = 0; node < _nodes; node++)
        {
            _cost[column + node] = Unreachable;
            _nextArc[column + node] = -1;
        }

        if (target < 0)
        {
            return new RepairCost(0, 0, Converged: true);
        }

        _cost[column + target] = 0;

        Array.Clear(_queued);
        int[] current = _queue;
        int[] pending = _scratch;
        current[0] = target;
        _queued[target] = true;

        long relaxations = 0;
        int rounds = 0;
        int frontier = 1;

        // Frontier-at-a-time, so `rounds` is the protocol's round count rather than a queue-pop
        // count. The active set is what any implementation would keep; running the textbook
        // all-nodes-every-round version would measure a strawman and report distance-vector as far
        // worse than anyone would build it.
        while (frontier > 0 && rounds < roundCap)
        {
            rounds++;
            int next = 0;

            for (int i = 0; i < frontier; i++)
            {
                int node = current[i];
                _queued[node] = false;
                int here = _cost[column + node];

                for (int slot = _reverse.Start[node]; slot < _reverse.Start[node + 1]; slot++)
                {
                    int arc = _reverse.Arc[slot];
                    int step = arcTicks[arc];
                    if (step == RoadGraph.Impassable)
                    {
                        continue;
                    }

                    relaxations++;
                    int from = _reverse.Source[arc];
                    int candidate = here + step;

                    if (candidate >= _cost[column + from])
                    {
                        continue;
                    }

                    _cost[column + from] = candidate;
                    _nextArc[column + from] = arc;

                    if (!_queued[from])
                    {
                        _queued[from] = true;
                        pending[next++] = from;
                    }
                }
            }

            (current, pending) = (pending, current);
            frontier = next;
        }

        return new RepairCost(rounds, relaxations, frontier == 0);
    }

    /// <summary>
    /// Repairs one column after arc costs changed, by vector exchange.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The sequenced rung is what makes deletion safe, and the mechanism is worth stating because
    /// it is the whole of why <c>references.md</c> insists on it.</b> When a node's own route is
    /// broken it does not merely raise its cost — it advertises unreachability under an <b>odd</b>
    /// sequence number it originates itself. An odd number beats every finite route any neighbour
    /// still believes in, because those carry the destination's older even number. So the bad news
    /// cannot be overwritten by stale good news, which is exactly the loop count-to-infinity is.
    /// </para>
    /// <para>
    /// <b>The unsequenced rung has no such ordering</b>, so a node accepts any neighbour's cheaper
    /// claim — including one routed through itself — and the estimates creep upward by whatever the
    /// cheapest cycle costs. On a road graph in Q16.16 Ticks that increment is small and the ceiling
    /// is the alternative route, so the round count is not a constant of the protocol but a ratio of
    /// two costs. That is why the cap exists and why hitting it is reported rather than hidden.
    /// </para>
    /// </remarks>
    public RepairCost Repair(
        int destination,
        int target,
        int[] arcTicks,
        int[] changedNodes,
        int changedCount,
        VectorProtocol protocol,
        int roundCap)
    {
        int column = destination * _nodes;

        if (target < 0)
        {
            return new RepairCost(0, 0, Converged: true);
        }

        if (protocol == VectorProtocol.Unsequenced)
        {
            // Citybound's. Nothing to order two claims by, so a node simply believes the cheapest
            // one on offer — including one routed through itself.
            return Flood(column, target, arcTicks, changedNodes, changedCount, false, roundCap);
        }

        // Phase A — poison. A node whose own next hop is now impassable advertises unreachability
        // under an ODD sequence number it originates itself. Odd outranks the even number the
        // destination last issued, so the bad news cannot be overwritten by stale good news, which
        // is the loop count-to-infinity is.
        int poisoned = 0;
        for (int i = 0; i < changedCount; i++)
        {
            int node = changedNodes[i];
            if (node == target)
            {
                continue;
            }

            int arc = _nextArc[column + node];
            if (arc >= 0 && arcTicks[arc] != RoadGraph.Impassable)
            {
                continue;
            }

            _cost[column + node] = Unreachable;
            _nextArc[column + node] = -1;
            _sequence[column + node] = (_sequence[column + node] | 1) + 2;
            _scratch[poisoned++] = node;
        }

        // The flood is seeded with the poisoned nodes' PREDECESSORS, not the poisoned nodes.
        //
        // A poisoned node has already been changed by hand, and re-deriving it achieves nothing —
        // worse, it correctly rejects every neighbour's stale claim, so nothing "moves" and it never
        // notifies anybody. In DSDV the poisoning node *advertises* its new entry: the advertisement
        // is the event, not a change the node discovers by looking around. Seeding the detectors
        // themselves made this whole phase a silent no-op that converged in two rounds and
        // twenty-four relaxations while leaving 16,680 of 16,697 entries wrong — and the section
        // still read "converged: yes", because a phase that does nothing does it very quickly.
        int notified = 0;
        for (int i = 0; i < poisoned; i++)
        {
            int node = _scratch[i];
            for (int slot = _reverse.Start[node]; slot < _reverse.Start[node + 1]; slot++)
            {
                int from = _reverse.Source[_reverse.Arc[slot]];
                if (from != target && notified < _nodes)
                {
                    _queue[notified++] = from;
                }
            }
        }

        RepairCost poison = notified == 0
            ? new RepairCost(0, 0, Converged: true)
            : Flood(column, target, arcTicks, _queue, notified, true, roundCap);

        // Phase B — the destination re-originates.
        //
        // This phase is the price of sequence numbers and it is not an implementation choice. An odd
        // entry outranks every finite route in circulation by construction, so once the poison has
        // spread nothing any neighbour still believes can restore a route: only a NEWER EVEN number,
        // issued by the destination itself, outranks the poison. That is DSDV working exactly as
        // specified — and it means one broken link obliges the destination to re-flood its whole
        // tree, because every node must at minimum accept the new sequence number.
        //
        // So the sequence numbers that make deletion safe are the same thing that makes deletion
        // expensive, and R4 measures that trade rather than asserting it.
        int highest = 0;
        for (int node = 0; node < _nodes; node++)
        {
            if (_sequence[column + node] > highest)
            {
                highest = _sequence[column + node];
            }
        }

        _sequence[column + target] = (highest | 1) + 1;
        _cost[column + target] = 0;
        _nextArc[column + target] = -1;

        int frontier = 0;
        for (int slot = _reverse.Start[target]; slot < _reverse.Start[target + 1]; slot++)
        {
            int from = _reverse.Source[_reverse.Arc[slot]];
            if (from != target)
            {
                _queue[frontier++] = from;
            }
        }

        RepairCost restore = Flood(column, target, arcTicks, _queue, frontier, true, roundCap);

        return new RepairCost(
            poison.Rounds + restore.Rounds,
            poison.Relaxations + restore.Relaxations,
            poison.Converged && restore.Converged);
    }

    /// <summary>
    /// The vector-exchange round loop, from a given frontier.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The comparison is the protocol.</b> With <paramref name="sequenced"/> set, a higher
    /// sequence number wins outright and the metric only breaks ties within one — which is DSDV's
    /// defining rule and the reason an unreachable claim can propagate at all. Without it, only the
    /// metric is left, and a node will accept a cheaper claim whose route runs through the node
    /// making the comparison.
    /// </para>
    /// <para>
    /// Frontier-at-a-time, so <c>Rounds</c> is the protocol's round count rather than a queue-pop
    /// count. The active set is what any implementation would keep — running the textbook
    /// all-nodes-every-round version would measure a strawman and report distance-vector as far
    /// worse than anyone would build it.
    /// </para>
    /// </remarks>
    private RepairCost Flood(
        int column, int target, int[] arcTicks, int[] seed, int seedCount, bool sequenced, int roundCap)
    {
        Array.Clear(_queued);
        int[] current = _frontierA;
        int[] pending = _frontierB;

        int frontier = 0;
        for (int i = 0; i < seedCount; i++)
        {
            int node = seed[i];
            if (node == target || _queued[node])
            {
                continue;
            }

            _queued[node] = true;
            current[frontier++] = node;
        }

        long relaxations = 0;
        int rounds = 0;

        while (frontier > 0 && rounds < roundCap)
        {
            rounds++;
            int next = 0;

            for (int i = 0; i < frontier; i++)
            {
                int node = current[i];
                _queued[node] = false;

                // Re-derive this node's entry from its neighbours' current claims. A raise is as
                // legitimate an outcome as a drop, which is what a pure relaxation cannot express
                // and what a cost increase requires.
                int mine = _sequence[column + node];
                int best = Unreachable;
                int bestArc = -1;
                int bestSequence = -1;

                for (int arc = _graph.ArcStart[node]; arc < _graph.ArcStart[node + 1]; arc++)
                {
                    int step = arcTicks[arc];
                    if (step == RoadGraph.Impassable)
                    {
                        continue;
                    }

                    relaxations++;

                    int neighbour = _graph.ArcTarget[arc];
                    int through = _cost[column + neighbour];
                    int candidate = through >= Unreachable ? Unreachable : through + step;
                    int sequence = sequenced ? _sequence[column + neighbour] : 0;

                    // DSDV's acceptance rule, and the half of it that is easy to leave out. A node
                    // rejects any advertisement OLDER than what it already holds — not merely
                    // preferring newer ones, but refusing stale ones outright. Without this clause a
                    // poisoned node keeps its odd sequence number while adopting a neighbour's stale
                    // finite cost, and then advertises that stale cost under the high sequence its
                    // own poison earned. That is count-to-infinity with a sequence number attached,
                    // and it is indistinguishable from the unsequenced failure the protocol exists
                    // to prevent — which is exactly how it presented before this line existed.
                    if (sequenced && sequence < mine)
                    {
                        continue;
                    }

                    if (!sequenced && candidate >= Unreachable)
                    {
                        continue;
                    }

                    if (bestSequence < 0
                        || sequence > bestSequence
                        || (sequence == bestSequence && candidate < best))
                    {
                        best = candidate;
                        bestArc = candidate >= Unreachable ? -1 : arc;
                        bestSequence = sequence;
                    }
                }

                bool moved = best != _cost[column + node] || bestArc != _nextArc[column + node]
                    || (sequenced && bestSequence > mine);

                if (!moved)
                {
                    continue;
                }

                _cost[column + node] = best;
                _nextArc[column + node] = bestArc;

                if (sequenced && bestSequence > mine)
                {
                    _sequence[column + node] = bestSequence;
                }

                // A node whose entry moved makes every node that can reach it a candidate.
                for (int slot = _reverse.Start[node]; slot < _reverse.Start[node + 1]; slot++)
                {
                    int from = _reverse.Source[_reverse.Arc[slot]];
                    if (_queued[from] || from == target || next >= _nodes)
                    {
                        continue;
                    }

                    _queued[from] = true;
                    pending[next++] = from;
                }
            }

            (current, pending) = (pending, current);
            frontier = next;
        }

        return new RepairCost(rounds, relaxations, frontier == 0);
    }

    /// <summary>Copies one column's costs out, for use as ground truth in an audit.</summary>
    public void CopyCosts(int destination, int[] into) =>
        Array.Copy(_cost, destination * _nodes, into, 0, _nodes);

    /// <summary>
    /// Copies another table's whole state in, so every scheme starts one edit from the same place.
    /// </summary>
    /// <remarks>
    /// <b>R2 caught two rungs reporting byte-identical peaks because the experiment had quietly
    /// removed the difference it existed to measure.</b> The cheap defence is to make the shared
    /// starting state explicit and copied rather than re-derived: if two schemes disagree afterwards
    /// it is because they did different things, and if they agree it is not because one of them
    /// never ran.
    /// </remarks>
    public void CopyFrom(DistanceVector other)
    {
        Array.Copy(other._cost, _cost, _cost.Length);
        Array.Copy(other._nextArc, _nextArc, _nextArc.Length);
        Array.Copy(other._sequence, _sequence, _sequence.Length);
    }

    /// <summary>
    /// Repairs one column by invalidating the affected subtree and re-deriving it from its own valid
    /// boundary. Not distance-vector, and the rung distance-vector has to beat.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the scheme a person would write if nobody had said the words "distance vector",
    /// and leaving it out is how R4 would produce a verdict it had not earned.</b> The observation it
    /// rests on is that a shortest-path tree is mostly unaffected by one edit: only the nodes whose
    /// current route uses a changed arc can be wrong, every other node's cost is still exact, and
    /// those exact costs are a valid boundary condition to re-run a Dijkstra against. So the work is
    /// proportional to the affected subtree instead of to the graph.
    /// </para>
    /// <para>
    /// Increases are handled first and decreases second. An increase can only be repaired by
    /// re-deriving — a node cannot discover that its own route got worse by relaxing — while a
    /// decrease propagates by ordinary relaxation and can only lower costs, so running it afterwards
    /// cannot invalidate what the first pass established.
    /// </para>
    /// </remarks>
    public RepairCost RepairSubtree(
        int destination, int target, int[] arcTicks, int[] changedArcs, int changedCount)
    {
        int column = destination * _nodes;

        if (target < 0)
        {
            return new RepairCost(0, 0, Converged: true);
        }

        _generation++;
        long relaxations = 0;
        int settled = 0;

        // Phase 1 — the affected subtree. A node is affected if its own next arc changed, or if the
        // node it routes through is affected.
        int head = 0;
        int tail = 0;

        for (int i = 0; i < changedCount; i++)
        {
            int node = _reverse.Source[changedArcs[i]];
            if (node == target || _stamp[node] == _generation)
            {
                continue;
            }

            if (_nextArc[column + node] != changedArcs[i])
            {
                continue;
            }

            _stamp[node] = _generation;
            _queue[tail++] = node;
        }

        while (head < tail)
        {
            int node = _queue[head++];
            _cost[column + node] = Unreachable;
            _nextArc[column + node] = -1;

            for (int slot = _reverse.Start[node]; slot < _reverse.Start[node + 1]; slot++)
            {
                int arc = _reverse.Arc[slot];
                int from = _reverse.Source[arc];

                relaxations++;

                if (_stamp[from] == _generation || from == target
                    || _nextArc[column + from] != arc)
                {
                    continue;
                }

                _stamp[from] = _generation;
                _queue[tail++] = from;
            }
        }

        int affected = tail;

        // Phase 2 — re-derive the affected set from the unaffected boundary. Every arc out of an
        // affected node into an unaffected one carries a cost that is still exact.
        _heapCount = 0;
        _generation++;

        for (int i = 0; i < affected; i++)
        {
            int node = _queue[i];
            int best = Unreachable;
            int bestArc = -1;

            for (int arc = _graph.ArcStart[node]; arc < _graph.ArcStart[node + 1]; arc++)
            {
                int step = arcTicks[arc];
                if (step == RoadGraph.Impassable)
                {
                    continue;
                }

                relaxations++;
                int through = _cost[column + _graph.ArcTarget[arc]];
                if (through >= Unreachable || through + step >= best)
                {
                    continue;
                }

                best = through + step;
                bestArc = arc;
            }

            if (bestArc >= 0)
            {
                _cost[column + node] = best;
                _nextArc[column + node] = bestArc;
                Push(best, node);
            }
        }

        // An arc that got CHEAPER improves its own source whether or not that source was in the
        // affected subtree, and no amount of re-deriving the subtree will discover it. Increases and
        // decreases are different problems: an increase can only be repaired by re-derivation,
        // because a node cannot learn that its own route got worse by relaxing; a decrease
        // propagates by ordinary relaxation and can only lower costs, so seeding it here cannot
        // invalidate what the phase above established.
        for (int i = 0; i < changedCount; i++)
        {
            int arc = changedArcs[i];
            int step = arcTicks[arc];
            if (step == RoadGraph.Impassable)
            {
                continue;
            }

            int from = _reverse.Source[arc];
            if (from == target)
            {
                continue;
            }

            relaxations++;
            int through = _cost[column + _graph.ArcTarget[arc]];
            if (through >= Unreachable || through + step >= _cost[column + from])
            {
                continue;
            }

            _cost[column + from] = through + step;
            _nextArc[column + from] = arc;
            Push(through + step, from);
        }

        while (_heapCount > 0)
        {
            (int key, int node) = Pop();

            if (_stamp[node] == _generation || key > _cost[column + node])
            {
                continue;
            }

            _stamp[node] = _generation;
            settled++;

            for (int slot = _reverse.Start[node]; slot < _reverse.Start[node + 1]; slot++)
            {
                int arc = _reverse.Arc[slot];
                int step = arcTicks[arc];
                if (step == RoadGraph.Impassable)
                {
                    continue;
                }

                relaxations++;
                int from = _reverse.Source[arc];
                int candidate = key + step;

                if (candidate >= _cost[column + from])
                {
                    continue;
                }

                _cost[column + from] = candidate;
                _nextArc[column + from] = arc;
                Push(candidate, from);
            }
        }

        return new RepairCost(settled, relaxations, Converged: true);
    }

    /// <summary>
    /// Compares one column against ground truth, returning how many entries hold a cost that is
    /// wrong and how many hold a next hop that does not reach the destination at all.
    /// </summary>
    /// <remarks>
    /// <b>Two columns rather than one, because they are different failures.</b> A wrong cost is a
    /// Traveller taking a longer route, which under <c>05 §4</c> is a different city. A next hop that
    /// walks into a cycle is a Traveller that never arrives — the <c>adr/0006</c>-class defect
    /// <c>adr/0041</c> names as <i>"a road that looks busy forever"</i>, and the failure sequence
    /// numbers exist to prevent.
    /// </remarks>
    public void Audit(
        int destination, int target, int[] truth, out int wrongCost, out int noRoute)
    {
        int column = destination * _nodes;
        wrongCost = 0;
        noRoute = 0;

        if (target < 0)
        {
            return;
        }

        // The next-hop entries form a functional graph — one out-edge per node — so "does this node
        // reach the target" is answered for every node in one linear pass with memoisation, not by
        // walking a chain from each. The walk-from-each version is O(nodes × path length) and was
        // costing more than every repair it was auditing put together.
        _generation++;
        Array.Clear(_reaches);

        // The destination reaches itself in zero steps. Without this the walk below terminates at
        // the target with an empty path stack, never records a verdict for it, and the final pass
        // counts it as stranded — one phantom per column, which is exactly the shape a real defect
        // would wear and is why it was worth chasing rather than rounding away.
        _reaches[target] = Reaches;

        for (int start = 0; start < _nodes; start++)
        {
            if (_cost[column + start] != truth[start])
            {
                wrongCost++;
            }

            if (_reaches[start] != Unknown)
            {
                continue;
            }

            int depth = 0;
            int at = start;
            byte verdict;

            while (true)
            {
                if (at == target)
                {
                    verdict = Reaches;
                    break;
                }

                if (_reaches[at] != Unknown)
                {
                    verdict = _reaches[at];
                    break;
                }

                if (_onPath[at] == _generation)
                {
                    // Met ourselves. A cycle in the next hops is a Traveller that never arrives.
                    verdict = Stranded;
                    break;
                }

                int arc = _nextArc[column + at];
                if (arc < 0)
                {
                    verdict = Stranded;
                    break;
                }

                _onPath[at] = _generation;
                _scratch[depth++] = at;
                at = _graph.ArcTarget[arc];
            }

            for (int i = 0; i < depth; i++)
            {
                _reaches[_scratch[i]] = verdict;
            }
        }

        for (int node = 0; node < _nodes; node++)
        {
            // Only entries that are supposed to have a route can fail to have one. A node genuinely
            // cut off from the destination is not a defect.
            if (truth[node] < Unreachable && _reaches[node] != Reaches)
            {
                noRoute++;
            }
        }
    }

    private void Push(int key, int node)
    {
        if (_heapCount == _heapKey.Length)
        {
            Array.Resize(ref _heapKey, _heapKey.Length * 2);
            Array.Resize(ref _heapNode, _heapNode.Length * 2);
        }

        int i = _heapCount++;
        _heapKey[i] = key;
        _heapNode[i] = node;

        while (i > 0)
        {
            int parent = (i - 1) >> 1;
            if (_heapKey[parent] <= _heapKey[i])
            {
                break;
            }

            Swap(parent, i);
            i = parent;
        }
    }

    private (int Key, int Node) Pop()
    {
        int key = _heapKey[0];
        int node = _heapNode[0];

        _heapCount--;
        _heapKey[0] = _heapKey[_heapCount];
        _heapNode[0] = _heapNode[_heapCount];

        int i = 0;
        while (true)
        {
            int left = (i << 1) + 1;
            if (left >= _heapCount)
            {
                break;
            }

            int right = left + 1;
            int smaller = right < _heapCount && _heapKey[right] < _heapKey[left] ? right : left;

            if (_heapKey[i] <= _heapKey[smaller])
            {
                break;
            }

            Swap(i, smaller);
            i = smaller;
        }

        return (key, node);
    }

    private void Swap(int a, int b)
    {
        (_heapKey[a], _heapKey[b]) = (_heapKey[b], _heapKey[a]);
        (_heapNode[a], _heapNode[b]) = (_heapNode[b], _heapNode[a]);
    }
}
