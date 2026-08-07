using Borough.Core.Arithmetic;
using S2.Routing.Graph;
using S2.Routing.Matrix;
using S2.Routing.Traffic;

namespace S2.Routing.Loop;

/// <summary>
/// R8's fleet: <c>adr/0046</c>'s three layers, closed on the volume column they are writing.
/// </summary>
/// <remarks>
/// <para>
/// <b>A separate class adapted from <see cref="Fleet"/> rather than a modification of it, and the
/// reason is a rule this spike has already learned the hard way.</b> <see cref="Fleet"/>'s figures are
/// published — R2a's attribution cost, R2b's lag and peak, R5's path-source storm — and they are in
/// <c>docs/spike-results.md</c> where the corpus reads them. A behavioural switch inside that class
/// would put R8's decision code on the path R2 and R5 timed, and the next capture of either would
/// move for a reason nobody wrote down. Duplicating three hundred lines is the cheaper mistake, and
/// it is the same argument <see cref="Congestion.LiveRatioUnclamped"/> makes about not sharing an
/// implementation with the method R1 published through.
/// </para>
/// <para>
/// <b>Habit is the free-flow next-hop table, built once and never refreshed.</b> That is
/// <c>adr/0046</c>'s explicit null hypothesis — <i>"Static per world is the null hypothesis and it
/// must be measured false before anything that maintains itself is built"</i> — and it carries a
/// known granularity error: R4 and R5.5 measured a District-granular next-hop route as structurally
/// wrong by 16.58% on the uniform draw and 149.73% on the local one. It is used anyway because
/// <b>diversion under it is free</b>: a Traveller that leaves the Habit Route resumes by reading the
/// table from wherever it now is, with no search. R8.6 prices what the same diversion costs under a
/// stored route, and the consequence for reading R8 is that <b>its stability conclusions carry to
/// either path source and its cost column does not.</b>
/// </para>
/// <para>
/// <b>The loop is closed on both sides, and this is the difference between R8 and every earlier
/// task in the spike.</b> The live cost array feeds route choice <i>and</i> travel speed: a
/// Traveller entering an arc is charged that arc's live traversal time (<see cref="Enter"/>), so a
/// jam slows the Travellers sitting in it, which raises their residence time, which raises the
/// volume, which raises the cost. <c>03 §3.4</c>'s self-correction needs both arrows and the first
/// draft of this class had only one — it consumed free-flow residuals, which made <i>volume</i> a
/// count of concurrent users rather than an accumulation, and left the VDF a display quantity that
/// routing happened to read.
/// </para>
/// <para>
/// <b>The consequence is that the Horizon 0 control is no longer inert and must not be expected to
/// be.</b> It has identical physics and no ability to <i>respond</i>, which is exactly what makes it
/// the right control: the difference between it and a Sight rung is routing and nothing else. The
/// tripwire that asked for a quiet control was written for the open-loop model and is restated in
/// the report rather than amended away.
/// </para>
/// <para>
/// <b>Temperament applies at the decision, per Traveller, per crossing.</b> <c>adr/0012</c> keys the
/// route cache by origin-destination pair and never by agent, so a threshold baked into a stored
/// route would violate it — <c>adr/0046</c> names that as <i>"the constraint most likely to be
/// violated by accident during implementation"</i>, which is why it is drawn here and stored nowhere.
/// </para>
/// </remarks>
internal sealed class SightFleet
{
    /// <summary>
    /// Crossings one Traveller may make in one Tick before the advance loop gives up.
    /// <see cref="Fleet"/>'s bound, unchanged, for <see cref="Fleet"/>'s reason.
    /// </summary>
    private const int CrossingsPerTickBound = 64;

    /// <summary>
    /// The score of a branch that runs into an arc no car may use.
    /// </summary>
    /// <remarks>
    /// Four orders above any real score and four orders below where <c>threshold × score</c> would
    /// overflow a <c>long</c>. Both ends matter: a sentinel small enough to lose to a real branch
    /// silently admits an impassable route, and one large enough to overflow the diversion margin
    /// turns the comparison's sign over, which is the same bug wearing the opposite result.
    /// </remarks>
    private const long Poisoned = 1_000_000_000_000L;

    /// <summary>
    /// Buckets in the relative-improvement histogram — <c>(habit − best) / habit</c>, Q16.16 in
    /// <c>[0, 1]</c>, recorded at every crossing where at least one alternative survived the filters.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Because a threshold swept across a guess is a threshold swept somewhere the decisions do
    /// not live.</b> R8.4's first attempt held the base at 10% and swept the spread around it, and
    /// the diversion rate moved by under 3% across the entire sweep — which says nothing about
    /// Temperament and everything about where 10% sits relative to the improvements drivers are
    /// actually offered.
    /// </para>
    /// <para>
    /// <b>The buckets are octaves, not equal widths, and that is the second thing this had to
    /// learn.</b> A linear histogram of 1,024 equal bins put <i>every</i> quantile from p10 to p90 in
    /// the first bin and reported the distribution as five zeroes — which is true, and useless. The
    /// improvements on offer span orders of magnitude and cluster far below one part in a thousand,
    /// so bucket <c>k</c> holds <c>[2^(k−1), 2^k)</c> in Q16.16 units and the whole range from one
    /// ulp to unity fits in eighteen of them.
    /// </para>
    /// </remarks>
    public const int ImprovementBuckets = 18;

    private readonly RoadGraph _graph;
    private readonly Districts _districts;
    private readonly NextHopTable _nextHop;
    private readonly int[] _freeFlowArcTicks;

    // The origin-destination pool. Drawn from rather than sampled into, for RouteStore's reason: a
    // rejection sampler at the tightest rung spends ~167 draws per pair, and paying that per
    // respawn per Tick would put the sampler's cost inside every figure in this section.
    private readonly int[] _poolOriginNode;
    private readonly int[] _poolTarget;

    private readonly int _horizon;
    private readonly int _alpha;
    private readonly int _baseThreshold;
    private readonly int _spread;
    private readonly int _blendWeight;
    private readonly ulong _seed;

    // Travellers, structure of arrays. The same layout the core's tables use.
    private readonly int[] _node;
    private readonly int[] _target;
    private readonly int[] _arc;
    private readonly int[] _arrivedBy;
    private readonly int[] _residual;
    private readonly int[] _spawnTick;

    private readonly int[] _liveTicks;
    private readonly long[] _improvement = new long[ImprovementBuckets];

    // Where this Tick's diversions went, so synchrony can be measured rather than inferred from an
    // amplitude. See DiversionTopShare.
    private readonly int[] _diversionOnArc;
    private readonly int[] _diversionTouched;
    private int _diversionTouchedCount;

    // R8.5's sustained surge. A bias on the RESPAWN pool rather than a one-off retarget, because an
    // impulse into a fleet that respawns on arrival is a pulse with a half-life of one journey, and
    // any system at all recovers from a pulse it stops receiving.
    private int _surgeDestination = -1;
    private int _surgeShare;

    private long _improvementSamples;
    private int _tick;
    private bool _refreshed;

    public SightFleet(
        RoadGraph graph,
        Districts districts,
        int[] freeFlowArcTicks,
        NextHopTable nextHop,
        int[] poolOriginNode,
        int[] poolTarget,
        int size,
        int horizon,
        int alpha,
        int baseThreshold,
        int spread,
        int blendWeight,
        ulong seed)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(districts);
        ArgumentNullException.ThrowIfNull(nextHop);
        ArgumentNullException.ThrowIfNull(poolOriginNode);
        ArgumentNullException.ThrowIfNull(poolTarget);

        _graph = graph;
        _districts = districts;
        _freeFlowArcTicks = freeFlowArcTicks;
        _nextHop = nextHop;
        _poolOriginNode = poolOriginNode;
        _poolTarget = poolTarget;
        _horizon = horizon;
        _alpha = alpha;
        _baseThreshold = baseThreshold;
        _spread = spread;
        _blendWeight = blendWeight;
        _seed = seed;

        _node = new int[size];
        _target = new int[size];
        _arc = new int[size];
        _arrivedBy = new int[size];
        _residual = new int[size];
        _spawnTick = new int[size];

        Volume = new int[graph.Volume.Length];
        _liveTicks = new int[graph.Arcs];
        _diversionOnArc = new int[graph.Arcs];
        _diversionTouched = new int[graph.Arcs];

        // Seeded free-flow, so the very first decision reads a cost basis rather than a zero field.
        // A zero field is not neutral: every arc would score identically and the first Tick's
        // diversions would be an artefact of arc ordering.
        Array.Copy(graph.ArcCarTicks, _liveTicks, graph.Arcs);

        for (int traveller = 0; traveller < size; traveller++)
        {
            _arc[traveller] = -1;
            Spawn(traveller);
        }
    }

    /// <summary>Travellers in flight. Constant by construction, exactly as <see cref="Fleet"/>'s is.</summary>
    public int Size => _node.Length;

    /// <summary>Direct attribution's volume column, Q16.16 Travellers present, indexed as the graph's own is.</summary>
    public int[] Volume { get; }

    /// <summary>The live cost array the Sight pass reads. Rewritten every <see cref="Refresh"/>.</summary>
    public int[] LiveTicks => _liveTicks;

    /// <summary>Segment boundaries crossed by the whole fleet during the last <see cref="Move"/>.</summary>
    public long Crossings { get; private set; }

    /// <summary>Travellers that reached <see cref="CrossingsPerTickBound"/> in one Tick. Expected zero.</summary>
    public long Bounded { get; private set; }

    /// <summary>Travellers that arrived and were replaced during the last <see cref="Move"/>.</summary>
    public long Arrivals { get; private set; }

    /// <summary>Crossings at which the Sight pass changed the arc taken, during the last <see cref="Move"/>.</summary>
    public long Diversions { get; private set; }

    /// <summary>
    /// Crossings at which <b>no</b> alternative survived the filters, during the last
    /// <see cref="Move"/>. R8.1's finding, traffic-weighted.
    /// </summary>
    public long NoAlternative { get; private set; }

    /// <summary>Crossings at which the Sight pass ran at all. Zero at Horizon 0, which is the point.</summary>
    public long Decisions { get; private set; }

    /// <summary>Journeys completed since construction. Cumulative — the harness diffs it per window.</summary>
    public long CompletedJourneys { get; private set; }

    /// <summary>Ticks those journeys took, summed. Cumulative.</summary>
    public long CompletedJourneyTicks { get; private set; }

    /// <summary>Spawns that found no usable origin in eight draws. Expected zero; never swallowed.</summary>
    public long SpawnFailures { get; private set; }

    /// <summary>
    /// Optional capture of <c>(node, target District)</c> pairs at each diversion, so R8.6 can price
    /// a re-search over the sites diversions actually happen at rather than over drawn ones.
    /// </summary>
    public int[]? DiversionLog { get; set; }

    /// <summary>Entries written to <see cref="DiversionLog"/> by the last <see cref="Move"/>.</summary>
    public int DiversionLogCount { get; private set; }

    /// <summary>Ticks advanced.</summary>
    public int Tick => _tick;

    /// <summary>
    /// The relative-improvement histogram, <see cref="ImprovementBuckets"/> buckets spanning
    /// <c>[0, 1]</c>. Cumulative; see <see cref="ClearImprovement"/>.
    /// </summary>
    public long[] ImprovementHistogram => _improvement;

    /// <summary>Decisions folded into <see cref="ImprovementHistogram"/>.</summary>
    public long ImprovementSamples => _improvementSamples;

    /// <summary>
    /// The share of this Tick's diversions that took the <b>same arc</b>, Q16.16. Zero where nobody
    /// diverted.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Amplitude and synchrony are different quantities, and <c>adr/0046</c>'s claim is about the
    /// second.</b> The ADR says an identical rule over an identical input <i>"produces a herd: the
    /// whole flow switches to the alternative together, the alternative jams, the whole flow
    /// switches back."</i> That is many drivers making the <b>same</b> move at the <b>same</b> time,
    /// and a mean absolute change in <c>v/c</c> does not reliably see it — a network can thrash with
    /// perfectly uncorrelated diversions and can herd with a small amplitude if the herd is small.
    /// </para>
    /// <para>
    /// Reported alongside <see cref="DiversionEffectiveArcs"/>, which is the same distribution's
    /// inverse Herfindahl: the number of arcs the Tick's diversions would be spread over if they
    /// were spread evenly. One means a perfect herd.
    /// </para>
    /// </remarks>
    public long DiversionTopShare { get; private set; }

    /// <summary>
    /// <c>total² / Σ(per-arc²)</c> for this Tick's diversions, Q16.16 — the effective number of
    /// distinct arcs they went to. <c>1.00</c> is a perfect herd.
    /// </summary>
    public long DiversionEffectiveArcs { get; private set; }

    /// <summary>
    /// Forgets the histogram, so a measurement window is not contaminated by the warm-up that
    /// preceded it — during warm-up the fleet is still spreading and the improvements on offer are
    /// not the ones a settled network offers.
    /// </summary>
    public void ClearImprovement()
    {
        Array.Clear(_improvement);
        _improvementSamples = 0;
    }

    /// <summary>
    /// One Tick: <see cref="Refresh"/> then <see cref="Move"/>. The two are separately callable so the
    /// harness can time them as their own columns — see <see cref="Refresh"/>.
    /// </summary>
    public void Advance()
    {
        Refresh();
        Move();
    }

    /// <summary>
    /// Recomputes the live cost array from the volume column the fleet is writing. <c>O(arcs)</c> per
    /// Tick and <b>independent of fleet size</b>.
    /// </summary>
    /// <remarks>
    /// <b>Public, and separate from <see cref="Move"/>, so that it can be timed as its own column.</b>
    /// It is a real cost a real implementation would pay every Tick, it does not scale with the
    /// thing everything else in this section scales with, and folding it into the traveller loop
    /// would charge the Sight sweep for 66,036 arcs it never looked at. R3's rule about inverting a
    /// derivation until what is published is measured applies to attribution as much as to
    /// tripwires.
    /// </remarks>
    public void Refresh()
    {
        for (int arc = 0; arc < _graph.Arcs; arc++)
        {
            _liveTicks[arc] = Congestion.LiveCarTicks(_graph, arc, Volume, _alpha);
        }

        _refreshed = true;
    }

    /// <summary>
    /// Advances every Traveller one Tick over the cost basis <see cref="Refresh"/> just wrote.
    /// </summary>
    public void Move()
    {
        if (!_refreshed)
        {
            // A Move over a stale cost array is a silently open loop, which is the one failure mode
            // this whole section exists to detect. Better a thrown exception than a published zero.
            throw new InvalidOperationException("Refresh() must precede Move()");
        }

        _tick++;
        Crossings = 0;
        Arrivals = 0;
        Bounded = 0;
        Diversions = 0;
        NoAlternative = 0;
        Decisions = 0;
        DiversionLogCount = 0;

        // Cleared through the touched list rather than with Array.Clear: diversions are a small
        // fraction of 66,036 arcs, and a full clear per Tick would put an O(arcs) cost inside the
        // traveller loop that the Sight column would then be charged for.
        for (int i = 0; i < _diversionTouchedCount; i++)
        {
            _diversionOnArc[_diversionTouched[i]] = 0;
        }

        _diversionTouchedCount = 0;

        for (int traveller = 0; traveller < _node.Length; traveller++)
        {
            int budget = Fixed.One;
            int crossings = 0;

            while (budget > 0)
            {
                if (_residual[traveller] > budget)
                {
                    _residual[traveller] -= budget;
                    break;
                }

                budget -= _residual[traveller];

                if (++crossings > CrossingsPerTickBound)
                {
                    Bounded++;
                    _residual[traveller] = Fixed.One;
                    break;
                }

                Leave(traveller);

                if (!Step(traveller))
                {
                    Arrivals++;
                    CompletedJourneys++;
                    CompletedJourneyTicks += _tick - _spawnTick[traveller];
                    Spawn(traveller);
                }

                Crossings++;
            }
        }

        long total = 0;
        long squares = 0;
        long top = 0;

        for (int i = 0; i < _diversionTouchedCount; i++)
        {
            long count = _diversionOnArc[_diversionTouched[i]];
            total += count;
            squares += count * count;

            if (count > top)
            {
                top = count;
            }
        }

        DiversionTopShare = total == 0 ? 0 : IntegerMath.FloorDiv(top * Fixed.One, total);
        DiversionEffectiveArcs = squares == 0
            ? 0
            : IntegerMath.FloorDiv(total * total * Fixed.One, squares);

        _refreshed = false;
    }

    /// <summary>
    /// Replaces <paramref name="count"/> Travellers with ones bound for <paramref name="destination"/>.
    /// R8.5's morning peak, and it is <see cref="Fleet.Surge"/>'s shape for <see cref="Fleet.Surge"/>'s
    /// reason: it replaces rather than adds, so the surge is a change in <i>where</i> the fleet is
    /// going and not in how large it is.
    /// </summary>
    public void Surge(int count, int destination)
    {
        int bound = count < _node.Length ? count : _node.Length;

        for (int traveller = 0; traveller < bound; traveller++)
        {
            Leave(traveller);
            Spawn(traveller, destination);
        }
    }

    /// <summary>
    /// Weights every subsequent respawn toward <paramref name="destination"/> with probability
    /// <paramref name="share"/> (Q16.16), until called again with a negative destination.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="Surge"/> alone cannot test what R8.5 exists to test, and the first capture of
    /// R8.5 proved it.</b> This fleet respawns a Traveller against the pool the moment it arrives, so
    /// a one-off retarget is a <i>pulse</i>: the surged Travellers reach their District, are replaced
    /// by ordinary ones, and the disturbance decays with a half-life of one journey whatever the
    /// routing does. Both the Horizon-0 control and every Sight rung recovered from it five times out
    /// of five, which is not a null result — it is no result.
    /// </para>
    /// <para>
    /// <c>adr/0046</c>'s exposed claim is about <c>03 §3.4</c>'s self-correction under a
    /// <i>sustained</i> asymmetry of demand, which is also the honest shape of R1's monocentric
    /// morning peak: people keep leaving for the centre for hours. That is a property of where Trips
    /// are drawn from, so it belongs on the respawn path and not on a one-shot method.
    /// </para>
    /// </remarks>
    public void SustainSurge(int destination, int share)
    {
        _surgeDestination = destination;
        _surgeShare = share;
    }

    /// <summary>Total volume, which must equal the fleet size in Q16.16 — <c>adr/0041</c>'s invariant.</summary>
    public long TotalVolume()
    {
        long total = 0;
        for (int i = 0; i < Volume.Length; i++)
        {
            total += Volume[i];
        }

        return total;
    }

    /// <summary>Travellers currently on no arc at all. The invariant's slack, and it should be zero.</summary>
    public int Unplaced()
    {
        int count = 0;
        for (int traveller = 0; traveller < _arc.Length; traveller++)
        {
            if (_arc[traveller] < 0)
            {
                count++;
            }
        }

        return count;
    }

    private void Leave(int traveller)
    {
        if (_arc[traveller] >= 0)
        {
            Volume[_graph.VolumeIndex(_arc[traveller])] -= Fixed.One;
            _arrivedBy[traveller] = _arc[traveller];
            _arc[traveller] = -1;
        }
    }

    /// <summary>
    /// Places a Traveller on an arc and charges it the arc's <b>live</b> traversal time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This one line is the physical half of the loop, and the first draft of this class did not
    /// have it.</b> With a free-flow residual the VDF is computed, the router reads it, and
    /// <i>nothing in the world slows down</i>: `03 §3.4`'s middle arrow — volume → travel time —
    /// simply does not exist, so "volume" degenerates into a count of concurrent users rather than
    /// an accumulation. The amplification that makes a jam a jam is <b>slower → longer residence →
    /// higher volume → slower still</b>, and it lives here.
    /// </para>
    /// <para>
    /// <b>The cost is locked in at entry and not re-read while the Traveller is on the arc.</b> A
    /// jam that clears does not release the vehicles already committed to it, which is the
    /// conservative direction and the cheap one; re-reading every Tick would be a second sweep over
    /// the fleet and a different model of what a queue is.
    /// </para>
    /// </remarks>
    private void Enter(int traveller, int arc)
    {
        _arc[traveller] = arc;

        int live = _liveTicks[arc];
        if (live == RoadGraph.Impassable || live <= 0)
        {
            // Only reachable for an arc no car may use, which the next-hop table never returns. The
            // free-flow array is the fallback rather than a constant so that the degenerate case is
            // still the graph's own answer.
            live = _freeFlowArcTicks[arc] == RoadGraph.Impassable
                ? Fixed.One
                : _freeFlowArcTicks[arc];
        }

        _residual[traveller] = live;
        Volume[_graph.VolumeIndex(arc)] += Fixed.One;
    }

    /// <summary>Moves onto the next arc. False when the Traveller has arrived.</summary>
    private bool Step(int traveller)
    {
        int node = _node[traveller];

        // A District with no car-reachable node has no representative, so a Traveller drawn into one
        // has nowhere to start.
        if (node < 0)
        {
            return false;
        }

        // Arrival is tested BEFORE entering, and the order is not cosmetic. Entering the last arc and
        // then reporting arrival in the same call leaves that arc incremented and never decremented —
        // the Traveller is respawned and its volume stays on the road forever, which is exactly the
        // adr/0006-class defect adr/0041's invariant exists to catch. Fleet.Step was written the
        // other way round first and the reading it produced was a v/c of 883.
        int target = _target[traveller];
        if (node == _districts.Representative[target])
        {
            return false;
        }

        int habit = _nextHop.Of(node, target);
        if (habit < 0)
        {
            return false;
        }

        // Horizon 0 is the control and it must be genuinely inert — no scoring, no draws, no branch
        // taken other than Habit's. Anything measured against it is measured against Habit alone.
        int chosen = _horizon > 0 ? Decide(traveller, node, target, habit) : habit;

        _node[traveller] = _graph.ArcTarget[chosen];
        Enter(traveller, chosen);
        return true;
    }

    /// <summary>
    /// The Sight decision: score Habit's arc and every alternative leaving this node, and take the
    /// best only if it beats Habit by more than this Traveller's Temperament.
    /// </summary>
    private int Decide(int traveller, int node, int target, int habit)
    {
        Decisions++;

        int arrivedBy = _arrivedBy[traveller];
        int arrivedSegment = arrivedBy < 0 ? -1 : _graph.ArcSegment[arrivedBy];

        long habitScore = Score(habit, target);
        long bestScore = habitScore;
        int best = habit;
        int alternatives = 0;

        for (int arc = _graph.ArcStart[node]; arc < _graph.ArcStart[node + 1]; arc++)
        {
            if (arc == habit || _graph.ArcCarTicks[arc] == RoadGraph.Impassable)
            {
                continue;
            }

            // No U-turns. Without this the cheapest escape from any jam is the road just travelled,
            // which is free by construction — the Traveller has just vacated it — and the fleet
            // would oscillate on its own wake rather than on the network.
            if (arrivedSegment >= 0 && _graph.ArcSegment[arc] == arrivedSegment)
            {
                continue;
            }

            // DistanceOf and not Of, because Of is -1 at the destination's own representative and
            // the branch that lands exactly on the destination is the one alternative that cannot be
            // wrong.
            if (_nextHop.DistanceOf(_graph.ArcTarget[arc], target) == RoadGraph.Impassable)
            {
                continue;
            }

            alternatives++;

            long score = Score(arc, target);
            if (score < bestScore)
            {
                bestScore = score;
                best = arc;
            }
        }

        if (alternatives == 0)
        {
            // R8.1's structural finding, weighted by where drivers actually are. A driver here
            // receives a signal it cannot act on however far it can see.
            NoAlternative++;
            return habit;
        }

        // The improvement on offer, recorded before any threshold is applied to it. A poisoned Habit
        // is excluded: its ratio would read as a near-total improvement and would say only that an
        // impassable arc is expensive.
        if (habitScore > 0 && habitScore < Poisoned)
        {
            _improvement[BucketOf(
                IntegerMath.FloorDiv((habitScore - bestScore) * Fixed.One, habitScore))]++;

            _improvementSamples++;
        }

        // Substantially better, never merely better. adr/0017's word, and Temperament is the number
        // it has been waiting for. Multiplied in long: the margin is a share of a score that already
        // carries a whole-network remainder.
        long margin = IntegerMath.ShiftRight(
            (long)Threshold(traveller) * habitScore, Fixed.FractionalBits);

        if (habitScore - bestScore > margin && best != habit)
        {
            Diversions++;

            if (_diversionOnArc[best]++ == 0)
            {
                _diversionTouched[_diversionTouchedCount++] = best;
            }

            if (DiversionLog is not null && DiversionLogCount + 2 <= DiversionLog.Length)
            {
                DiversionLog[DiversionLogCount++] = node;
                DiversionLog[DiversionLogCount++] = target;
            }

            return best;
        }

        return habit;
    }

    /// <summary>
    /// A branch's score: the live cost of the next <c>N</c> arcs following the next-hop table from
    /// <paramref name="firstArc"/>, plus the <b>free-flow</b> next-hop distance from wherever that
    /// lookahead ends to <paramref name="target"/>.
    /// </summary>
    /// <remarks>
    /// <b>The remainder is not optional and the reason is arithmetic rather than modelling.</b> Two
    /// branches walked <c>N</c> arcs deep have travelled different <i>distances</i>, so comparing
    /// their raw sums compares two different journeys and systematically prefers whichever branch
    /// happens to have shorter Segments. Adding what remains makes the two comparable — and it is
    /// also exactly <c>adr/0046</c>'s model, <i>a live view of what is in front plus a lagged
    /// expectation of the rest</i>, so the fix and the design are the same line of code.
    /// </remarks>
    private long Score(int firstArc, int target)
    {
        int representative = _districts.Representative[target];
        long total = 0;
        int arc = firstArc;
        int at = _graph.ArcTarget[firstArc];

        for (int step = 0; step < _horizon; step++)
        {
            if (arc < 0)
            {
                return total;
            }

            int live = _liveTicks[arc];
            if (live == RoadGraph.Impassable)
            {
                return Poisoned;
            }

            total += live;
            at = _graph.ArcTarget[arc];

            if (at == representative)
            {
                return total;
            }

            arc = _nextHop.Of(at, target);
        }

        int remainder = _nextHop.DistanceOf(at, target);
        return remainder == RoadGraph.Impassable ? Poisoned : total + remainder;
    }

    /// <summary>
    /// This Traveller's diversion threshold for this decision, Q16.16 as a share of the Habit
    /// branch's score.
    /// </summary>
    /// <remarks>
    /// <b>The base's counter is fixed at 0 and the jitter's is the Tick, and that split is the
    /// entire reason there are two tags.</b> <c>adr/0046</c>: <i>"Folding them into one would
    /// correlate a Citizen's character with its mood, which is the correlation the whole layer split
    /// exists to avoid."</i> A purely per-decision threshold makes each driver a fresh coin flip, so
    /// nobody is ever <i>the sort of person who takes the back roads</i>; a purely stable one is
    /// deterministic per Citizen, so the flow re-synchronises into a smaller permanent herd. R8.4
    /// sweeps the blend and both endpoints are predicted to fail.
    /// <para>
    /// <b>Both draws are taken unconditionally, including at spread 0 where neither can change the
    /// answer.</b> Skipping them would take the Temperament layer's per-decision cost out of exactly
    /// the rung R8.3 publishes a cost column for, and the layer would look free because it was not
    /// measured.
    /// </para>
    /// </remarks>
    private int Threshold(int traveller)
    {
        int character = Signed(
            CounterHash.Of(_seed, (ulong)traveller, 0, CounterHash.Purpose.TemperamentBase));

        int jitter = Signed(CounterHash.Of(
            _seed, (ulong)traveller, (ulong)(uint)_tick, CounterHash.Purpose.TemperamentJitter));

        int blend = Fixed.Mul(_blendWeight, character)
            + Fixed.Mul(Fixed.One - _blendWeight, jitter);

        int threshold = _baseThreshold + Fixed.Mul(_spread, blend);
        return threshold < 0 ? 0 : threshold;
    }

    /// <summary>A signed Q16.16 draw in <c>[-1, +1]</c>.</summary>
    private static int Signed(ulong hash) =>
        CounterHash.Below(hash, (2 * Fixed.One) + 1) - Fixed.One;

    /// <summary>The smallest improvement a bucket holds, Q16.16. Bucket 0 holds exactly zero.</summary>
    public static int ImprovementLowerBound(int bucket) =>
        bucket <= 0 ? 0 : (int)IntegerMath.ShiftLeft(1L, bucket - 1);

    private static int BucketOf(long improvement)
    {
        if (improvement <= 0)
        {
            return 0;
        }

        int bucket = 1;
        while (bucket < ImprovementBuckets - 1
            && improvement >= IntegerMath.ShiftLeft(1L, bucket))
        {
            bucket++;
        }

        return bucket;
    }

    private void Spawn(int traveller) => Spawn(traveller, SurgeDraw(traveller));

    /// <summary>
    /// The surged District if this respawn is drawn into it, and −1 otherwise. Its own purpose tag,
    /// because sharing <see cref="CounterHash.Purpose.LoopPair"/> would make the two draws perfectly
    /// correlated and the surge would capture exactly the same pool indices every Tick.
    /// </summary>
    private int SurgeDraw(int traveller)
    {
        if (_surgeDestination < 0 || _surgeShare <= 0)
        {
            return -1;
        }

        int roll = CounterHash.Below(
            CounterHash.Of(
                _seed, (ulong)traveller, (ulong)(uint)_tick, CounterHash.Purpose.SurgeDraw),
            Fixed.One);

        return roll < _surgeShare ? _surgeDestination : -1;
    }

    private void Spawn(int traveller, int destination)
    {
        _residual[traveller] = 0;
        _arrivedBy[traveller] = -1;
        _spawnTick[traveller] = _tick;
        _node[traveller] = -1;
        _target[traveller] = 0;

        for (int attempt = 0; attempt < 8; attempt++)
        {
            int pair = CounterHash.Below(
                CounterHash.Of(
                    _seed,
                    (ulong)traveller,
                    ((ulong)(uint)_tick << 8) | (uint)attempt,
                    CounterHash.Purpose.LoopPair),
                _poolOriginNode.Length);

            int origin = _poolOriginNode[pair];
            int target = destination >= 0 ? destination : _poolTarget[pair];

            // The origin comes from the rung's pool and the destination may have been forced by a
            // surge, so the pair being usable is a property of neither on its own. Tested here rather
            // than assumed: an unreachable pair leaves a Traveller on no arc at all, and Unplaced()
            // is checked every Tick precisely because that is how a conservation hole starts.
            if (origin >= 0
                && origin != _districts.Representative[target]
                && _nextHop.DistanceOf(origin, target) != RoadGraph.Impassable)
            {
                _node[traveller] = origin;
                _target[traveller] = target;
                break;
            }
        }

        if (_node[traveller] < 0)
        {
            SpawnFailures++;
        }

        // Placed onto its first arc immediately, so the volume invariant holds at every Tick boundary
        // rather than only after the first advance.
        if (!Step(traveller))
        {
            _arc[traveller] = -1;
            _residual[traveller] = Fixed.One;
        }
    }
}
