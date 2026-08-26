namespace Borough.Core.Rules;

using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;

/// <summary>
/// What one Rule evaluation decided. <b>Not table state</b>, which is what lets Phase 2 produce it.
/// </summary>
/// <remarks>
/// <c>adr/0037</c> makes Phase 2 read-only so that every entity table can be single-buffered, and
/// <c>Simulation.VerifyDecideWritesNothing</c> folds every column to prove it. An intent is a
/// <em>proposal Phase 3 may reject</em>, so it lives outside the tables the guard folds — a phase that
/// could not write its own proposals could not propose anything.
/// </remarks>
public readonly record struct RuleVerdict(
    int Instance,
    RuleId Rule,
    long Applications,
    int Bin,
    Blocking Blocking,
    ConditionId Reported)
{
    /// <summary>
    /// Whether this evaluation succeeded — <b>which is not the same as applying anything.</b>
    /// </summary>
    /// <remarks>
    /// <b>A derived apply count of zero is a success</b> (<c>02 §4.1</c>): it re-arms on its rate,
    /// waits on nothing, and moves nothing, because a Readout is not subscribable and there is no Bin
    /// that could ever wake it. So success is <em>no Bin stopped this</em> rather than
    /// <c>Applications &gt; 0</c>, and reading it the other way would put a zero-count Rule on a wait
    /// list against <see cref="Rows.NoSlot"/> — a Rule asleep on a Bin that does not exist.
    /// </remarks>
    public bool Succeeded => Bin == Rows.NoSlot;

    /// <summary>An evaluation that found every term satisfiable.</summary>
    public static RuleVerdict Fire(int instance, RuleId rule, long applications) =>
        new(instance, rule, applications, Rows.NoSlot, Blocking.Nothing, ConditionId.None);

    /// <summary>
    /// An evaluation stopped by one Bin, naming which Bin and in which direction.
    /// </summary>
    /// <remarks>
    /// <b>It carries no quantity</b> (<c>adr/0063</c>). A subscription used to record the deficit the
    /// waiter computed while failing, and the drain compared it against a single arrival; both halves
    /// were wrong, and the amount was the half nothing downstream ever read as an entitlement. What
    /// stops a Rule is <em>which</em> Bin and <em>why</em>; <em>how much</em> is a question the Bin and
    /// the Ruleset in force can answer at the moment it is asked, which is
    /// <see cref="RuleEngine.Requirement"/>.
    /// </remarks>
    public static RuleVerdict Stopped(int instance, RuleId rule, int bin, Blocking blocking) =>
        new(instance, rule, 0, bin, blocking, ConditionId.None);
}

/// <summary>
/// One flow counter's readings over an interval: what it totalled, and its largest single Tick.
/// </summary>
/// <remarks>
/// <b>Both, because a budget is per Tick and a bill is per run.</b> <c>02 §4</c> makes burstiness
/// <em>authored</em> rather than incurred — a greedy Rule drains to the floor and crosses the
/// supplied/short boundary more often than a fixed quantum of the same throughput — so an interval
/// holding a spike and an interval holding a plateau can carry the same total. Only
/// <see cref="Peak"/> can be held against the Tick budget, and only <see cref="Sum"/> says what the
/// run cost.
/// </remarks>
/// <param name="Sum">The total over the interval. Divided by the reading cadence, a mean rate.</param>
/// <param name="Peak">The largest single Tick in that interval.</param>
public readonly record struct RuleFlow(long Sum, int Peak)
{
    /// <summary>This flow with one Tick's count rolled into it.</summary>
    /// <remarks>
    /// <b>Shared by both Rule families rather than written once per engine</b>, because the peak is
    /// the half that drifts silently: a sum accumulated wrongly is visible in the first reading, and
    /// a peak that is never raised reads as a perfectly flat load — which is the shape both engines'
    /// tripwires are stated over.
    /// </remarks>
    public RuleFlow Fold(int tick) => new(Sum + tick, tick > Peak ? tick : Peak);
}

/// <summary>
/// What the Rule engine did since the last reading: <c>02 §4</c>'s two counters, and the scheduled
/// load they are read against.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>Evaluations − Due</c> is the quantity task 9's tripwire is actually about.</b> Due rows are
/// what the Event Wheel handed over and evaluations are what was spent on them, so the difference is
/// the entire cost of chain walking and Phase 3's re-check. Neither number alone separates a bigger
/// city from a less stable one, which is the same reason the Census carries a table's slots beside
/// its live rows.
/// </para>
/// <para>
/// <b>Read by draining, so the counters cannot become the defect they are watching for.</b> A
/// monotonically rising total trends upward for the life of every run by construction, which is
/// exactly the shape <c>adr/0006</c>'s instrument exists to flag — an instrument that always reads
/// <em>leak</em> reports nothing. The reading is the sink.
/// </para>
/// </remarks>
/// <param name="Due">Rule Instances taken off the Event Wheel.</param>
/// <param name="Evaluations">Evaluations performed — a head, each non-terminal link, each re-check.</param>
/// <param name="ChainRungs">Chain rungs descended, the reporting terminal included.</param>
public readonly record struct RuleActivity(RuleFlow Due, RuleFlow Evaluations, RuleFlow ChainRungs);

/// <summary>
/// Bin Rule evaluation and application: Phase 2 decides, Phase 3 applies, and nothing is partial.
/// </summary>
/// <remarks>
/// <para>
/// <b>Atomicity is the core semantic</b> (<c>02 §4.1</c>). If any input is insufficient or any output
/// would exceed capacity, <em>nothing happens</em> and the Rule fails. That is what makes the economy
/// conserved and failure reportable: a half-applied Rule would consume Goods that became nothing, and
/// there would be no single Bin to name as the cause.
/// </para>
/// <para>
/// <b>The check is over net deltas per Bin, not term by term, and that is not a refinement.</b> A Rule
/// naming one Bin on both sides — drawing from a Bin it also produces into — is checked term by term
/// as though the withdrawal had not happened, so it can be refused headroom in a Bin it was about to
/// empty. Nothing else would ever drain that Bin, so the Rule subscribes on a rescue that cannot
/// arrive: a deadlock, not a conservatism. Netting first removes the case rather than documenting it.
/// </para>
/// <para>
/// <b>A greedy Rule is raised to what its Bins allow, and the raise is the same walk as the check.</b>
/// A net delta is linear in the apply count, so the deltas are accumulated once per application and
/// each Bin then states a count it can carry — <c>level ÷ −delta</c> for a draw, <c>space ÷ delta</c>
/// for a deposit. The count is the least of those and the Rule's own <c>max</c>. Applying at
/// <em>n</em> and checking at <em>n</em> are therefore one arithmetic rather than a search: there is no
/// rung at which the Rule is tried and rejected.
/// </para>
/// <para>
/// <b>The engine holds no simulation state.</b> Its buffers are scratch, cleared at the head of every
/// Tick, so <c>adr/0006</c>'s question — <em>what is the sink?</em> — is answered by the clear rather
/// than by a cap. They grow to the largest single Tick's due count and stop, which is bounded by the
/// Rule Instance table and does not move with elapsed time.
/// </para>
/// </remarks>
public sealed class RuleEngine
{
    /// <summary>
    /// The ceiling Phase 2 evaluates under: none of its own, so the Rule's <c>max</c> is the bound.
    /// </summary>
    /// <remarks>
    /// Not a tuning number and not a cap — it is the absence of one, which is why it is a
    /// <c>const</c> here rather than a Ruleset value under <c>adr/0015</c>. The Rule's own <c>max</c>
    /// is the number a designer changes.
    /// </remarks>
    private const long Unbounded = long.MaxValue;

    /// <summary>
    /// What <see cref="Buy"/> answers when the premises stand in no District at all.
    /// </summary>
    /// <remarks>
    /// <b>Distinct from <see cref="Rows.NoSlot"/>, which that method uses for *settled*, and from a
    /// Bin slot, which it uses for *blocked on this*.</b> The three answers are three different
    /// cities: a purchase happened, a purchase is waiting on a market, and there is no market to wait
    /// on. The last re-arms rather than subscribing, and the reason is at the call site.
    /// </remarks>
    private const int Marketless = -2;

    private readonly World _world;
    private readonly WorldKey _key;

    private int[] _due = new int[64];
    private int _dueCount;

    private RuleVerdict[] _verdicts = new RuleVerdict[64];
    private ulong[] _order = new ulong[64];
    private int _verdictCount;

    // The net delta per distinct Bin of the Rule currently being checked. Filled by Check and read
    // straight afterwards by Apply, which is why the two are never interleaved across Rules.
    private int[] _touchedBin = new int[8];
    private long[] _touchedDelta = new long[8];
    private int _touchedCount;

    // Which Bin to NAME when a touch cannot carry its delta, which is the touched Bin itself for
    // every term but one. A purchase draws on a seller's own Bin and must blame the MARKET ROW
    // (adr/0139): a buyer parked on one shop's Bin is woken by that shop alone and sleeps through
    // every other seller in the District restocking. See Buy.
    private int[] _touchedBlame = new int[8];

    // The pool draws of the Rule currently being checked, per application: which market row and how
    // much. Read by Fire to post DistrictPoolTable.Consumed, which is the tatonnement's numerator
    // and had no writer at all before this. Term structure, which the netted deltas above have lost.
    private int[] _boughtRow = new int[4];
    private long[] _boughtAmount = new long[4];
    private int _boughtCount;

    // 02 §4's counters. The first three are the Tick in flight; CloseTick folds them into the second
    // three, which are the interval a Census reading drains.
    private int _tickDue;
    private int _tickEvaluations;
    private int _tickRungs;

    private RuleFlow _dueFlow;
    private RuleFlow _evaluationFlow;
    private RuleFlow _rungFlow;

    /// <param name="world">The tables this evaluates against. Not copied.</param>
    /// <param name="key">The world seed, for Phase 3's settle order.</param>
    public RuleEngine(World world, WorldKey key)
    {
        ArgumentNullException.ThrowIfNull(world);

        _world = world;
        _key = key;
    }

    /// <summary>
    /// Reads <c>02 §4</c>'s counters for the interval since the last call, and resets them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Read-and-reset rather than a running total</b>, so that a series of readings is a series of
    /// <em>intervals</em>. A cumulative counter would rise for the life of every run, and a census
    /// exists to answer <em>is this trending upward</em> — a metric that always says yes says nothing.
    /// It also puts the sink where <c>adr/0006</c> asks for one: the reading is what bounds these.
    /// </para>
    /// <para>
    /// <b>The interval is Ticks, not calls.</b> The accumulator behind this sees every Tick even when
    /// it is read every sixty-fourth, so a slow reading loses only the shape of the interval and never
    /// its magnitude — which is why there is one census cadence and not a second, finer one for
    /// flows.
    /// </para>
    /// <para>
    /// <b>Undrained, these are bounded by the run rather than by the city.</b> A caller that never
    /// takes a reading accumulates until the run ends, which is why the totals are 64-bit; they are
    /// counters rather than collections, so <c>adr/0006</c> is not what they are about, and
    /// <c>adr/0003</c>'s extension to magnitudes is answered by the width.
    /// </para>
    /// </remarks>
    public RuleActivity Drain()
    {
        var activity = new RuleActivity(_dueFlow, _evaluationFlow, _rungFlow);

        _dueFlow = default;
        _evaluationFlow = default;
        _rungFlow = default;

        return activity;
    }

    /// <summary>
    /// Phase 1 — take everything due off the Event Wheel.
    /// </summary>
    /// <remarks>
    /// <b>Between here and the end of Phase 3 a due row is on no queue at all</b>, which is the one
    /// window in which <see cref="Invariants.WorldInvariants.RuleInstancesAreQueuedExactlyOnce"/>
    /// would fail. It runs at the end of a run rather than mid-Tick, and every row taken here is put
    /// back — re-armed or subscribed — before <see cref="Apply"/> returns.
    /// </remarks>
    public void CollectDue(Ticks tick)
    {
        _dueCount = 0;

        int slot = _world.Wheel.PopDue(tick);

        while (slot != Rows.NoSlot)
        {
            Grow(ref _due, _dueCount + 1);
            _due[_dueCount++] = slot;
            slot = _world.Wheel.PopDue(tick);
        }

        _tickDue += _dueCount;
    }

    /// <summary>
    /// Phase 2 — evaluate every due Rule against the Past, writing nothing.
    /// </summary>
    /// <remarks>
    /// <b>The settle order is drawn here rather than in Phase 3, and it is drawn per (instance,
    /// Tick).</b> <c>02 §8</c> rule 5: contested outcomes are settled by a counter-based shuffle, never
    /// by arrival and never by entity id, because ordering by id is <em>biased</em> — the same Building
    /// would win every contested draw for the life of the city. Drawing it in the read-only phase is
    /// free: <see cref="Randomness.Draw"/> is a pure function of its coordinates, so it needs no stream
    /// and no ordering of its own.
    /// </remarks>
    public void Evaluate(Ticks tick)
    {
        _verdictCount = 0;

        for (int i = 0; i < _dueCount; i++)
        {
            int instance = _due[i];

            Grow(ref _verdicts, _verdictCount + 1);
            Grow(ref _order, _verdictCount + 1);

            _verdicts[_verdictCount] = Walk(
                instance, _world.RuleInstances.Rule[instance], Unbounded);
            _order[_verdictCount] = Randomness.Draw(
                _key, _world.RuleInstances.Rows.IdAt(instance), tick, PurposeTag.RuleSettleOrder);

            _verdictCount++;
        }
    }

    /// <summary>
    /// Phase 3 — apply the intents in shuffle order, re-checking atomicity as each is reached.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The re-check is not belt and braces.</b> Phase 2 evaluated every due Rule against one state;
    /// by the time a given intent is reached, earlier intents have already moved Bins. Two Rules on one
    /// Building both offered six flour where six exists is the whole reason the shuffle exists, and
    /// without the re-check both would take it.
    /// </para>
    /// <para>
    /// <b>A re-check that now fails subscribes, exactly as a Phase 2 failure does.</b> There is no
    /// third outcome and no retry: the loser waits on the Bin that beat it, and what wakes it is the
    /// next deposit large enough to cover its shortfall.
    /// </para>
    /// <para>
    /// <b>The re-check may lower a greedy Rule's count and may never raise it</b>, which is why it
    /// passes the Phase 2 count as a ceiling rather than re-deriving one. Lowering is forced — a Rule
    /// cannot spend what an earlier intent has already taken. Raising is not: an earlier intent may
    /// equally have <em>deposited</em>, and a Rule that helped itself to that would be consuming a
    /// quantity that did not exist when it decided. Phase 2 is where a Rule decides how much it wants
    /// (<c>adr/0037</c>); Phase 3 may refuse it or serve it short, and the surplus is simply there next
    /// time the Rule is due.
    /// </para>
    /// </remarks>
    public void Apply(Ticks tick)
    {
        // Allocation-free: the key overload sorts the payload span alongside the keys. Ties are
        // possible in principle and harmless in practice — the sort is deterministic given the same
        // input, and the input order is the wheel bucket's, which is itself deterministic.
        _order.AsSpan(0, _verdictCount).Sort(_verdicts.AsSpan(0, _verdictCount));

        for (int i = 0; i < _verdictCount; i++)
        {
            RuleVerdict verdict = _verdicts[i];

            if (verdict.Succeeded)
            {
                // Re-checked against the Future at the count Phase 2 decided against the Past, which
                // this may reduce and may never raise (adr/0049). A loser then resumes the walk from
                // the rung below the one that just failed: 02 §4.1's "losers take their fallback".
                RuleVerdict rechecked = Check(verdict.Instance, verdict.Rule, verdict.Applications);

                verdict = rechecked.Succeeded
                    ? rechecked
                    : Descend(verdict.Instance, _world.Rules.Rule(verdict.Rule).OnFail, rechecked);
            }

            if (verdict.Succeeded)
            {
                Fire(verdict, tick);
            }
            else
            {
                Stop(verdict, tick);
            }
        }

        _dueCount = 0;

        CloseTick();
    }

    /// <summary>
    /// Rolls what this Tick spent into the interval a reading will drain, and starts the next one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Called by <see cref="Apply"/>, which is a Tick's last engine phase, and public because a
    /// Tick is not always all three.</b> An instrument timing Phase 2 on its own has run something
    /// the engine can price and nothing that would close it, so without this the only way to read
    /// what a phase cost would be to derive it from what a whole Tick cost — and a benchmark dividing
    /// by a number nobody measured is the shape S2 hit three times.
    /// </para>
    /// <para>
    /// <b>Closing twice with nothing in between is closing once</b> — the second call folds a Tick of
    /// zero, which adds nothing to a sum and cannot lower a peak. Closing part-way <em>through</em> a
    /// Tick is a different thing and is wrong in one direction: the sums stay right and the peaks
    /// read low, because one busy Tick has been folded as two quiet ones.
    /// </para>
    /// </remarks>
    public void CloseTick()
    {
        _dueFlow = _dueFlow.Fold(_tickDue);
        _evaluationFlow = _evaluationFlow.Fold(_tickEvaluations);
        _rungFlow = _rungFlow.Fold(_tickRungs);

        _tickDue = 0;
        _tickEvaluations = 0;
        _tickRungs = 0;
    }

    /// <summary>Evaluates a Rule and, if it fails, its <c>on_fail</c> chain.</summary>
    /// <remarks>
    /// A link does not do the head's work by another route — it <b>relieves the Bin the head failed
    /// on</b>, refilling it if it was short (<c>adr/0045</c>). So the head does not fire this Tick;
    /// the link's deposit wakes it through the Bin's wait list, which is <c>02 §7</c>'s
    /// <em>mutators wake observers</em> rather than a retry.
    /// </remarks>
    private RuleVerdict Walk(int instance, RuleId head, long ceiling)
    {
        RuleVerdict verdict = Check(instance, head, ceiling);

        return verdict.Succeeded
            ? verdict
            : Descend(instance, _world.Rules.Rule(head).OnFail, verdict);
    }

    /// <summary>Walks the rungs below a head that has already failed.</summary>
    /// <remarks>
    /// <b>The verdict returned on failure is the head's, not the last link's</b>, so the subscription
    /// lands on the Bin the head was short of — <c>adr/0045</c>'s <em>a failed chain subscribes once,
    /// at its head</em>. Every link relieves that same Bin, so one subscription wakes on every rescue
    /// path and depth costs no subscriptions.
    /// </remarks>
    private RuleVerdict Descend(int instance, RuleId at, RuleVerdict head)
    {
        while (!at.IsNone)
        {
            // 02 §4's second counter, and it is a depth rather than a cost: the terminal below is
            // reached and counted but never evaluated, so a chain ending in a report is one rung
            // deeper than it is evaluations expensive.
            _tickRungs++;

            RuleDefinition definition = _world.Rules.Rule(at);

            // A terminal is never evaluated. It has no term that could be short, so under ordinary
            // Rule semantics it would succeed, fire, and re-arm the head on `rate` — walking the
            // whole chain again every `rate` Ticks for as long as the shortage lasts. That is the
            // polling defect subscription exists to remove, and the corpus's own worked example
            // contained it (adr/0045). It records, and the chain stays failed.
            if (definition.IsTerminal)
            {
                return head with { Reported = definition.Reports };
            }

            RuleVerdict link = Check(instance, at, Unbounded);

            if (link.Succeeded)
            {
                return link;
            }

            at = definition.OnFail;
        }

        return head;
    }

    /// <summary>
    /// Evaluates one Rule Instance at the largest count its Bins allow, leaving its net Bin deltas in
    /// the scratch buffers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The failure test is against the floor and the raise is against the ceiling, in one walk.</b>
    /// A Bin affording fewer than <c>min</c> applications is the failure <c>02 §4.1</c> describes, and
    /// its shortfall is <c>(min × amount) − available</c> — the floor's shortfall, not the ceiling's,
    /// because what the Rule waits on is the least that would let it fire at all. A Bin affording more
    /// than <c>min</c> lowers the ceiling and blames nobody.
    /// </para>
    /// <para>
    /// <b><paramref name="ceiling"/> is how Phase 3 refuses to enlarge a Phase 2 decision</b>; see
    /// <see cref="Apply"/>.
    /// </para>
    /// <para>
    /// <b>The Bin blamed on failure is the first one that cannot carry its net delta</b>, in
    /// declaration order with inputs before outputs. <c>02 §4.1</c> says a failing Rule subscribes to
    /// <em>the</em> Bin that was short — singular — so where several are, the first is the minimal
    /// reading. When it arrives the Rule re-evaluates, fails on the next, and subscribes there; the
    /// chain of waits is as long as the number of short Bins and no subscription is ever held twice.
    /// </para>
    /// </remarks>
    /// <param name="instance">The Rule Instance slot to evaluate.</param>
    /// <param name="ceiling">An upper bound on the count, over and above the Rule's own <c>max</c>.</param>
    private RuleVerdict Check(int instance, RuleId rule, long ceiling)
    {
        // 02 §4's first counter, and this is the only place it can be taken honestly: a head, a link
        // below one, and Phase 3's re-check all arrive here and all cost the same walk. Counting due
        // rows instead — which is what this counter was before task 9 — cannot see a chain link at
        // all, so chain walking would have been invisible in the number stated to price it.
        _tickEvaluations++;

        RuleDefinition definition = _world.Rules.Rule(rule);
        int building = _world.Buildings.Rows.Resolve(_world.RuleInstances.Building[instance]);

        (long floor, long band) = Band(_world, definition, building);

        if (band < ceiling)
        {
            ceiling = band;
        }

        _touchedCount = 0;
        _boughtCount = 0;

        foreach (Term term in _world.Rules.Inputs(rule))
        {
            if (term.Bin.Scope == Scope.Pool)
            {
                // A purchase is one term and three deltas, so it cannot go through Touch(Bin(...)).
                // It also fails BEFORE the affordability walk when no seller can cover a batch, which
                // is a district-wide shortage rather than one Bin being short.
                int unsupplied = Buy(instance, rule, term, floor);

                if (unsupplied == Marketless)
                {
                    // ⚠ A SUCCESS AT ZERO APPLICATIONS, and RuleVerdict.Succeeded's own remark is
                    // the argument: "it re-arms on its rate, waits on nothing, and moves nothing,
                    // because there is no Bin that could ever wake it." A buyer whose premises stand
                    // in no District is that sentence exactly -- the market row it would wait on has
                    // not been opened, so subscribing is not expressible and the honest answer is to
                    // try again on the rate. The window is bounded by [districts] revisit_ticks.
                    return RuleVerdict.Fire(instance, rule, 0);
                }

                if (unsupplied != Rows.NoSlot)
                {
                    return RuleVerdict.Stopped(instance, rule, unsupplied, Blocking.Supply);
                }

                continue;
            }

            Touch(Bin(_world, instance, term.Bin, rule), -term.Amount);
        }

        foreach (Term term in _world.Rules.Outputs(rule))
        {
            Touch(Bin(_world, instance, term.Bin, rule), term.Amount);
        }

        long applications = ceiling;

        for (int i = 0; i < _touchedCount; i++)
        {
            int bin = _touchedBin[i];
            long delta = _touchedDelta[i];
            long level = _world.Bins.LevelAt(bin);

            long affordable;

            if (delta < 0)
            {
                affordable = IntegerMath.FloorDiv(level, -delta);

                if (affordable < floor)
                {
                    return RuleVerdict.Stopped(instance, rule, _touchedBlame[i], Blocking.Supply);
                }
            }
            else if (delta > 0)
            {
                long headroom = _world.Bins.Capacity[bin] - level;
                affordable = IntegerMath.FloorDiv(headroom, delta);

                if (affordable < floor)
                {
                    return RuleVerdict.Stopped(instance, rule, _touchedBlame[i], Blocking.Space);
                }
            }
            else
            {
                // A Bin drawn from and returned to in equal measure bounds nothing, at any count.
                continue;
            }

            if (affordable < applications)
            {
                applications = affordable;
            }
        }

        for (int i = 0; i < _touchedCount; i++)
        {
            _touchedDelta[i] *= applications;
        }

        return RuleVerdict.Fire(instance, rule, applications);
    }

    /// <summary>
    /// Resolves one <c>pool</c> input into a seller, a payment and three Bin deltas, or names the
    /// market row nobody could supply from.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>adr/0050</c>'s whole sentence, and it is the one term in the design a designer does not
    /// author in full.</b> The Good moves one way and money the other at the prevailing price, settled
    /// atomically with the Rule: the seller's stock falls by the term's <c>amount</c>, the buyer's
    /// balance falls by <c>amount × price</c>, and the seller's balance rises by the same. ***Money is
    /// conserved across a purchase because the two money legs are one number with two signs*** — there
    /// is no leg to the treasury and none to nowhere.
    /// </para>
    /// <para>
    /// <b>The counterparty is one seller, chosen here</b> (<c>adr/0139</c>): the Pool is a market and
    /// not a store, so the stock is in the selling Business's own Bin and a <c>pool</c> term names a
    /// counterparty rather than a container. The candidates are the market row's sellers, which is the
    /// District's connectivity already applied.
    /// </para>
    /// <para>
    /// <b>⚠ The draw picks a START and the walk is first-fit from it, which is <c>02 §8</c> rule 5.</b>
    /// Every seller in a District charges the market row's price, so *cheapest* is not a discriminator
    /// and first-fit from the head would give the shop nearest the head every sale for the life of the
    /// city — the rule's own worked failure with list position standing in for entity id.
    /// <see cref="PurposeTag.SellerChoice"/> carries the rest.
    /// </para>
    /// <para>
    /// <b>⚠ A seller must hold a whole batch — <c>floor × amount</c> — and one below it cannot sell.</b>
    /// That is <c>adr/0139</c>'s surviving consequence stated in code: a Rule fails only when *no*
    /// seller in the District holds a batch, which is a genuine district-wide shortage and the correct
    /// failure. It is not the atomicity objection that record withdrew, because the band a shipped
    /// production Rule declares is one batch rather than a day's appetite.
    /// </para>
    /// <para>
    /// <b>⚠ It returns the MARKET ROW's Bin on failure and never the seller's</b>, and that is open
    /// decision 2 of <c>plans/0044</c> resolved for the supply half. A buyer parked on one shop's Bin
    /// is woken by that shop alone; parked on the market it is woken by any seller's deposit — which
    /// is what <c>World.RingMarket</c> exists to do. <b>Short of money is a different failure and
    /// blames the buyer's own balance</b>, reached through the affordability walk in the ordinary way,
    /// because the Good leg is touched first and <see cref="Check"/> blames the first short Bin.
    /// </para>
    /// </remarks>
    /// <returns>
    /// <see cref="Rows.NoSlot"/> when a seller was found, or the market row's Bin when none was.
    /// </returns>
    private int Buy(int instance, RuleId rule, in Term term, long floor)
    {
        int market = Bin(_world, instance, term.Bin, rule);

        if (market == Rows.NoSlot)
        {
            return Marketless;
        }

        int row = MarketRow(_world, instance, term.Bin.Resource);

        long batch = floor * term.Amount;
        int sellers = _world.Markets.SellerCount(_world, row);

        if (sellers == 0)
        {
            return market;
        }

        // Keyed on the buying Rule Instance's monotonic id, so it is a lottery number the buyer holds
        // rather than a rotation the whole District performs in step.
        ulong draw = Randomness.Draw(
            _key, _world.RuleInstances.Rows.IdAt(instance), _world.Tick, PurposeTag.SellerChoice);

        int start = (int)(draw % (ulong)sellers);
        var seller = new Space.Offer(Rows.NoSlot, Rows.NoSlot);

        for (int i = 0; i < sellers; i++)
        {
            Space.Offer candidate = _world.Markets.Seller(_world, row, (start + i) % sellers);

            if (_world.Bins.LevelAt(candidate.Bin) >= batch)
            {
                seller = candidate;
                break;
            }
        }

        if (seller.Bin == Rows.NoSlot)
        {
            return market;
        }

        if (!_world.TryMoneyResource(out ResourceId money))
        {
            throw new InvalidOperationException(
                $"rule {rule.Raw} buys from a District Pool and this Ruleset declares no Resource "
                + "whose family is money. A purchase crosses an ownership boundary, so the Good moves "
                + "one way and money the other at the prevailing price (adr/0050) -- there is no "
                + "spelling of a pool term that moves only the Good. A file with a pool term and no "
                + "money Resource is refusable at load and no refusal does so yet.");
        }

        int purse = _world.FindLocalBin(instance, money);

        if (purse == Rows.NoSlot)
        {
            throw new InvalidOperationException(
                $"rule {rule.Raw} buys from a District Pool and the subject running it holds no "
                + "balance to pay from. A balance is a Bin belonging to a Household or a Business "
                + "(adr/0113, adr/0114) and a Building never holds money, so this is a PREMISES Rule "
                + "with a pool term -- the landlord shopping. Give the Rule a local term whose Bin is "
                + "owner = \"occupant\" or owner = \"business\" and RulesetLoader.ApplyTenancies will "
                + "derive the payer.");
        }

        long payment = term.Amount * _world.DistrictPools.Price[row].Raw;
        int till = _world.Bins.Rows.Resolve(_world.Businesses.Balance[seller.Business]);

        Touch(seller.Bin, -term.Amount, market);
        Touch(purse, -payment);
        Touch(till, payment);

        Grow(ref _boughtRow, _boughtCount + 1);
        Grow(ref _boughtAmount, _boughtCount + 1);

        _boughtRow[_boughtCount] = row;
        _boughtAmount[_boughtCount] = term.Amount;
        _boughtCount++;

        return Rows.NoSlot;
    }

    /// <summary>
    /// The <c>DistrictPoolTable</c> row marketing a Resource where a Rule Instance stands.
    /// </summary>
    /// <remarks>
    /// <b>Premises, then Lot, then Cell, then the residency index</b> — the same chain
    /// <see cref="Emit"/> walks for a Map emission, and the one
    /// <c>Space.DistrictResidency.Of</c>'s own remark says a Building's District goes through. ⚠ <b>A
    /// Building carries no District handle and must not</b>: it would be a copy that goes stale the
    /// first time the watershed moves a boundary.
    /// </remarks>
    internal static int MarketRow(World world, int instance, ResourceId resource)
    {
        int building = world.Buildings.Rows.Resolve(world.RuleInstances.Building[instance]);

        if (!world.Lots.Rows.TryResolve(world.Buildings.Lot[building], out int lotSlot))
        {
            return Rows.NoSlot;
        }

        Handle<District> district = world.DistrictsInCells.Of(
            world.DistrictCells,
            CellGrid.ToCells(world.Lots.East[lotSlot]),
            CellGrid.ToCells(world.Lots.North[lotSlot]));

        return world.Districts.Rows.TryResolve(district, out int districtSlot)
            ? world.Markets.Row(world, districtSlot, resource)
            : Rows.NoSlot;
    }

    /// <summary>Applies a Rule: its net Bin deltas, its Map emissions, and its re-arm.</summary>
    /// <remarks>
    /// <b>Net deltas rather than terms, for the reason <see cref="RuleEngine"/> gives</b>, and it is
    /// load-bearing here for a second reason: <see cref="World.Deposit"/> asserts against the Bin's
    /// headroom, so a term-by-term application of a Rule holding one Bin on both sides could trip an
    /// invariant on a Rule the check had just passed.
    /// </remarks>
    private void Fire(RuleVerdict verdict, Ticks tick)
    {
        int instance = verdict.Instance;

        // The rule that fired, which is the head only when the chain was not walked. A rescued
        // Building runs the link's terms, emits the link's emissions and re-arms on the link's rate.
        RuleId rule = verdict.Rule;
        RuleDefinition definition = _world.Rules.Rule(rule);

        // Recovery is total (adr/0053): a Building whose supply returns is indistinguishable from one
        // that never failed. Firing is the only thing that clears the clock, which is why the
        // duration means continuous starvation rather than time since the last complaint.
        _world.RuleInstances.Reported[instance] = ConditionId.None;
        _world.RuleInstances.StarvedSince[instance] = default;

        for (int i = 0; i < _touchedCount; i++)
        {
            Handle<Bin> bin = _world.Bins.Rows.At(_touchedBin[i]);
            long delta = _touchedDelta[i];

            if (delta < 0)
            {
                _world.Withdraw(bin, -delta, tick);
            }
            else if (delta > 0)
            {
                _world.Deposit(bin, delta, tick);
            }
        }

        // DistrictPoolTable.Consumed's FIRST WRITER, and the column shipped at milestone 12 task 6
        // with none -- so every Day's bucket read zero, MarketRuleset.Reprice read that as NO TRADES,
        // and the price sat at its ceiling on every world. It is the tatonnement's numerator: what
        // the District drew since the last recompute, which the reprice zeroes.
        //
        // ⚠ Posted from the term list rather than from the netted deltas, because the deltas have
        // lost which Bin was a purchase -- a seller's stock Bin is an ordinary Bin by the time Fire
        // sees it. And posted here rather than in Check because Phase 2 writes nothing (adr/0037).
        for (int i = 0; i < _boughtCount; i++)
        {
            _world.DistrictPools.Consumed[_boughtRow[i]] += _boughtAmount[i] * verdict.Applications;
        }

        int building = _world.Buildings.Rows.Resolve(_world.RuleInstances.Building[instance]);

        foreach (MapEmission emission in _world.Rules.Emissions(rule))
        {
            Emit(building, emission, verdict.Applications);
        }

        _world.Wheel.Arm(instance, tick, definition.Rate);
    }

    /// <summary>Puts a failed Rule to sleep on the Bin that stopped it.</summary>
    /// <remarks>
    /// <para>
    /// <b>It does not re-arm</b> (<c>02 §4.1</c>), which is the entire economics of the design: a
    /// starved District costs nothing at all until supply arrives, where a retry timer would cost the
    /// same as a firing Rule for as long as the shortage lasted.
    /// </para>
    /// <para>
    /// <b>This is also where failure pressure starts, and only for one of the two failures</b>
    /// (<c>adr/0053</c>, as amended). Short of an <em>input</em> is <c>02 §5.9</c>'s starvation and
    /// starts the clock; out of <em>space</em> is a full Bin, which is what a well-supplied
    /// Building looks like, and stops it. Starting it only when it is not already running is what
    /// makes the duration continuous: a Rule woken by an arrival that turns out not to cover its
    /// shortfall comes back through here without having fired, and must not have its clock reset by
    /// the visit.
    /// </para>
    /// <para>
    /// <b>It drains the Bin it has just joined, and that is not a second wake path.</b>
    /// <see cref="Apply"/> settles intents in shuffle order, so a deposit large enough to cover this
    /// waiter may already have run this Tick — and <see cref="World.Drain"/> walked the queue before
    /// the waiter was on it, spending the wake it was owed on nobody. The rescue is the same drain,
    /// re-run against the queue it should have seen: one predicate, not two, and no timer, so
    /// <c>02 §4.1</c>'s <em>does not re-arm</em> still holds.
    /// </para>
    /// <para>
    /// <b>It cannot rescue a Rule into firing on the Tick it failed.</b>
    /// <see cref="World.Wake"/> arms for <c>tick + 1</c>, so a Phase 2 failure covered by a Phase 3
    /// deposit still waits a Tick. Re-checking it here instead would let a Rule spend a quantity that
    /// did not exist when it decided, which is the <em>may never raise</em> half of <c>adr/0049</c>.
    /// </para>
    /// </remarks>
    private void Stop(RuleVerdict verdict, Ticks tick)
    {
        _world.RuleInstances.Reported[verdict.Instance] = verdict.Reported;

        if (verdict.Blocking != Blocking.Supply)
        {
            _world.RuleInstances.StarvedSince[verdict.Instance] = default;
        }
        else if (!_world.RuleInstances.IsStarving(verdict.Instance))
        {
            _world.RuleInstances.StarvedSince[verdict.Instance] = tick;
        }

        _world.Subscribe(
            _world.RuleInstances.Rows.At(verdict.Instance),
            _world.Bins.Rows.At(verdict.Bin),
            verdict.Blocking);

        _world.Drain(verdict.Bin, verdict.Blocking, tick);
    }

    /// <summary>
    /// What this Rule needs from one named Bin, in one direction, under the Ruleset in force.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Derived on demand rather than recorded when the waiter failed</b> (<c>adr/0063</c>). The
    /// stored column this replaces was computed under the Ruleset in force at the moment of failure and
    /// nothing ever re-derived it, so halving a Rule's input on a hot reload left the starving Building
    /// as the one Building the edit never reached. Deriving it here makes a waiter's requirement a fact
    /// about the world now, which is what <c>adr/0015</c>'s acceptance test needs on the shortage path.
    /// </para>
    /// <para>
    /// <b>This is the engine's own arithmetic, exposed rather than restated.</b> It reuses
    /// <see cref="Band"/> and <see cref="Bin"/>, and it yields <c>floor × |net delta|</c> — the
    /// identical product <see cref="Check"/> compares against a level. Reimplementing affordability in
    /// the caller would leave two spellings of atomicity that have to agree for ever, and the one that
    /// drifts would be the one nothing exercises.
    /// </para>
    /// <para>
    /// <b>It does not count.</b> <see cref="Check"/> increments <c>02 §4</c>'s evaluation counter,
    /// which reaches the Census as a <em>flow</em> — read as a sum and a peak over the interval, and
    /// drained by the reading. Calling <see cref="Check"/> from the drain or from a whole-world walk
    /// would inflate that number with work no Rule evaluation did, so this derives the one Bin it was
    /// asked about instead of evaluating the Rule.
    /// </para>
    /// <para>
    /// <b>Net, because a Bin drawn from and returned to in equal measure bounds nothing</b> at any
    /// apply count — the same reason <see cref="Check"/> skips a zero delta rather than treating it as
    /// two constraints.
    /// </para>
    /// <para>
    /// <b>Zero means <em>this Bin cannot block that way</em>, and both callers want that reading.</b> A
    /// band of zero applications asks nothing of anything; a Bin the Rule only fills cannot be short of
    /// level for it. A waiter in either state is on a list its own terms contradict — a mislabelled
    /// subscription rather than a missed wake — and the honest response is to wake it and let Phase 2
    /// resubscribe it correctly, which a requirement of zero produces at both call sites.
    /// </para>
    /// </remarks>
    internal static long Requirement(World world, int instance, int binSlot, Blocking blocking)
    {
        RuleId rule = world.RuleInstances.Rule[instance];
        RuleDefinition definition = world.Rules.Rule(rule);
        int building = world.Buildings.Rows.Resolve(world.RuleInstances.Building[instance]);

        (long floor, _) = Band(world, definition, building);

        // Check reaches the same answer by `affordable < floor` being false for every non-negative
        // affordable, and it is spelled out here because this method has no affordable to compare.
        if (floor <= 0)
        {
            return 0;
        }

        long net = 0;

        foreach (Term term in world.Rules.Inputs(rule))
        {
            if (Bin(world, instance, term.Bin, rule) == binSlot)
            {
                net -= term.Amount;
            }
        }

        foreach (Term term in world.Rules.Outputs(rule))
        {
            if (Bin(world, instance, term.Bin, rule) == binSlot)
            {
                net += term.Amount;
            }
        }

        if (blocking == Blocking.Supply)
        {
            return net < 0 ? floor * -net : 0;
        }

        return net > 0 ? floor * net : 0;
    }

    /// <summary>
    /// The Bin one of a Rule's terms names, by position across its inputs and then its outputs.
    /// </summary>
    private static int BinAt(
        World world,
        int instance,
        RuleId rule,
        ReadOnlySpan<Term> inputs,
        ReadOnlySpan<Term> outputs,
        int position)
        => position < inputs.Length
            ? Bin(world, instance, inputs[position].Bin, rule)
            : Bin(world, instance, outputs[position - inputs.Length].Bin, rule);

    /// <summary>
    /// Adds to <paramref name="claims"/>, per Bin slot, what every armed Rule Instance will draw when
    /// it runs — the part of a Bin's level that is already spoken for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A woken waiter records no claim anywhere, and this derives the claim it would have made.</b>
    /// <c>World.Drain</c> spends a budget down — it wakes from the front while the arriving quantity
    /// covers the requirement, subtracting as it goes — but <c>World.Wake</c> only clears
    /// <see cref="RuleInstanceTable.Blocked"/> and arms the row for <c>tick + 1</c>. Nothing is drawn
    /// until that row runs, so between the drain and the end of the Tick the level reads as though
    /// none of it were owed. <b>The drain's guarantee is true of an instant</b>, and a check that
    /// compares a parked waiter against the whole level is asking after the budget has gone.
    /// </para>
    /// <para>
    /// <b>Derived rather than stored, so no State Hash moves</b> (<c>plans/0003</c> hash-moving queue
    /// item 14). The alternative was a reserved column on the Bin, incremented by the drain and
    /// released on apply — exact and cheap to read, and a new saved field that drifts silently the
    /// first time a release is missed. This costs one pass over the Rule Instances, at end of run,
    /// where <c>02 §10</c> already puts a whole-world walk.
    /// </para>
    /// <para>
    /// <b>Armed is <see cref="EventWheel.IsArmed"/>'s sense rather than <c>Blocked == Nothing</c>.</b>
    /// The latter reads the same for a row Phase 1 has already popped, which is in flight rather than
    /// owed — <c>adr/0056</c>'s third state, and the reason that predicate exists.
    /// </para>
    /// </remarks>
    internal static void AccumulateClaims(World world, Blocking blocking, Span<long> claims)
    {
        ArgumentNullException.ThrowIfNull(world);

        RuleInstanceTable instances = world.RuleInstances;

        for (int instance = 0; instance < instances.Rows.SlotCount; instance++)
        {
            if (!instances.Rows.IsLive(instance) || !world.Wheel.IsArmed(instance, world.Tick))
            {
                continue;
            }

            RuleId rule = instances.Rule[instance];

            ReadOnlySpan<Term> inputs = world.Rules.Inputs(rule);
            ReadOnlySpan<Term> outputs = world.Rules.Outputs(rule);

            int terms = inputs.Length + outputs.Length;

            for (int position = 0; position < terms; position++)
            {
                int bin = BinAt(world, instance, rule, inputs, outputs, position);

                // A pool term whose District does not exist yet resolves to nothing, and there is no
                // Bin for its claim to be recorded against. It claims nothing because it cannot fire
                // -- Check answers zero applications for exactly this Rule Instance.
                if (bin == Rows.NoSlot)
                {
                    continue;
                }

                // Requirement nets every term naming this Bin, so a Bin that two terms name must be
                // counted once. The rescan is over a Rule's own term list, which is a handful.
                bool counted = false;

                for (int earlier = 0; earlier < position; earlier++)
                {
                    if (BinAt(world, instance, rule, inputs, outputs, earlier) == bin)
                    {
                        counted = true;
                        break;
                    }
                }

                if (!counted)
                {
                    claims[bin] += Requirement(world, instance, bin, blocking);
                }
            }
        }
    }

    /// <summary>
    /// The band one evaluation of this Rule may apply within.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Greedy and fixed are one form, and this is where that is cashed in</b> (<c>02 §4.1</c>):
    /// <c>min = max</c> pins the ceiling to the floor, so the raise below cannot move and the fixed case
    /// needs no branch of its own. Which of the two a Rule uses is a modelling decision fixed at design
    /// time — <em>greedy when the actor works through its stock, fixed when the actor owes a
    /// quantum</em> — and never a performance one, because the two spend different amounts of the same
    /// Goods and are therefore different cities under <c>05 §4</c>.
    /// </para>
    /// <para>
    /// <b>A derived count is a band of one</b>, floor and ceiling together, because a Readout states
    /// how many times the Rule applies rather than bounding it. So a derived Rule cannot be served
    /// short: it applies <c>n</c> times or it fails, which is the fixed case's semantics arrived at
    /// from the other side. <em>Greed handles what is consumed; derived handles what is consulted.</em>
    /// </para>
    /// <para>
    /// <b>The Readout is consulted at evaluation, so <c>adr/0049</c> governs it too.</b> Phase 3
    /// re-reads it and then clamps to the Phase 2 count — a Readout that grew mid-Tick cannot enlarge
    /// what the Rule decided, for the same reason a Bin that grew cannot.
    /// </para>
    /// </remarks>
    private static (long Floor, long Ceiling) Band(
        World world, in RuleDefinition definition, int building)
    {
        if (!definition.Apply.IsDerived)
        {
            return (definition.Apply.Min, definition.Apply.Max);
        }

        long readout = Readouts.Read(world, building, definition.Apply.Derived);

        // The percentage is 02 §4.1's own spelling: "one unit of money applied income × 15 / 100
        // times". Floor division, because a fraction of an application is not an application.
        long count = IntegerMath.FloorDiv(readout * definition.Apply.Percent, 100);

        return (count, count);
    }

    /// <summary>Which Bin a term addresses, or a named hole.</summary>
    /// <remarks>
    /// <para>
    /// <b><c>pool</c> and <c>global</c> throw rather than resolving to nothing</b>, which is slice 6's
    /// pattern and exists because a placeholder returning zero is a value somebody reads and tunes
    /// around. <c>pool</c> needs road connectivity, Districts and a Pool, none of which exist before
    /// Phase 2 of the roadmap; <c>global</c> needs a home for a Bin no Building owns, which is an
    /// entity decision this slice has no content to justify making.
    /// </para>
    /// <para>
    /// <b>Neither is refused at load, deliberately.</b> <c>adr/0015</c>'s error surface argues for
    /// catching a bad Ruleset with a file and a line — but <c>02 §4.3</c>'s own worked example rescues
    /// its bakery from the District Pool, and a loader that refuses the corpus's example is not a
    /// loader. A Ruleset naming <c>pool</c> loads, and fails the first time a Rule actually reaches
    /// for it.
    /// </para>
    /// </remarks>
    private static int Bin(World world, int instance, in BinRef reference, RuleId rule)
    {
        switch (reference.Scope)
        {
            case Scope.Local:
                // ⚠ THE SUBJECT AND NOT THE BUILDING (adr/0141). A local term is free because the Bin
                // already belongs to whoever runs the Rule, and since a tenant runs its own Rules
                // that is no longer always the premises. World.FindLocalBin is where the two cases
                // are one lookup; the Rule's tenancy was settled at load, so nothing branches here.
                int slot = world.FindLocalBin(instance, reference.Resource);

                if (slot == Rows.NoSlot)
                {
                    throw new InvalidOperationException(
                        $"rule {rule.Raw} names local Resource {reference.Resource.Raw}, and the "
                        + "subject running it holds no Bin for it. The kind's Bin set and the Rules "
                        + "attached to it are stated in one file and could be checked against each "
                        + "other at load; no refusal does so yet.");
                }

                return slot;

            case Scope.Pool:
                // ⚠ THE MARKET ROW'S BIN AND NEVER A SELLER'S (adr/0139). This is the Bin a pool term
                // NAMES -- the wake target a blocked buyer parks on, and the address every caller but
                // Check wants: BinAt, AccumulateClaims and Requirement all reason about where a
                // waiter sleeps rather than about who supplied it. The three deltas a purchase
                // actually settles are Check's alone, through Buy, because a term is 1:1 here and a
                // purchase is 1:3. Its level is always zero and that is not a stub: the Pool is a
                // market and not a store, so the stock is in the selling Business's own Bin and this
                // row is the price, the wake target and the reachable sellers.
                // 🔴 A RULESET WITH A POOL TERM AND NO [districts] TABLE HAS NO MARKET AND NEVER
                // WILL, which is a different thing from not having one YET and must not share its
                // answer. Firing at zero for ever is the "loads clean and misbehaves in silence"
                // shape adr/0048 refuses outright, and it would be silent in exactly the file whose
                // whole point is the purchase. ⚠ THE REFUSAL BELONGS AT LOAD, with a file and a
                // line; it is here because the loader has no such check yet, and that debt is filed
                // rather than assumed.
                if (!world.Rules.Districts.Runs)
                {
                    throw new NotSupportedException(
                        $"rule {rule.Raw} names pool Resource {reference.Resource.Raw} and this "
                        + "Ruleset states no [districts] table, so the city has no Districts, no "
                        + "Pools and no markets -- a pool term in it can never resolve. NOTE "
                        + "(adr/0050, adr/0139): the Pool is a MARKET, not a wider Bin lookup. A "
                        + "pool term crosses an ownership boundary, so the Good moves one way and "
                        + "money the other at the prevailing price, settled atomically with the "
                        + "Rule. rulesets/provisioned.toml is the smallest shipped file that "
                        + "carries one.");
                }

                int row = MarketRow(world, instance, reference.Resource);

                // ⚠ NoSlot RATHER THAN A THROW, and the case is ordinary rather than exceptional:
                // the watershed runs on [districts] revisit_ticks, so a Building raised between two
                // evaluations stands on ground no District has claimed for up to a Day. Every caller
                // handles it -- Check fires at zero, Requirement nets nothing and the claim walk
                // skips it -- and none of them may throw, because AccumulateClaims walks every armed
                // Rule Instance at end of run whether or not it ever fired (plans/0044 F14).
                return row != Rows.NoSlot
                    && world.Bins.Rows.TryResolve(world.DistrictPools.Bin[row], out int pool)
                        ? pool
                        : Rows.NoSlot;

            case Scope.Global:
                // The entity decision this case waited on is adr/0114's: a Bin's owner is
                // discriminated, and one of the four kinds is the treasury. The building parameter is
                // unused here and that is the point of the scope -- global names one Bin per Resource
                // for the whole city, so there is nothing to resolve it against.
                int treasury = world.FindTreasuryBin(reference.Resource);

                if (treasury == Rows.NoSlot)
                {
                    throw new InvalidOperationException(
                        $"rule {rule.Raw} names global Resource {reference.Resource.Raw}, and the "
                        + "treasury holds no Bin for it. World.FitTreasury gives the treasury one Bin "
                        + "per CONSERVED Resource, so this is a money term naming a Resource whose "
                        + "[[resource]] family is not money -- which 02 §4.3 does not describe: "
                        + "global is the far end of an explicit transfer, local money out and global "
                        + "money in. A city-wide larder of Food is a different mechanism and nothing "
                        + "has designed it.");
                }

                return treasury;

            case Scope.Map:
            default:
                throw new InvalidOperationException(
                    "a map term is a MapEmission and never a Bin term. Reaching here means the loader "
                    + "put one in the wrong array.");
        }
    }

    /// <summary>Writes a Rule's Map Layer emission into the Cell under its Building.</summary>
    /// <remarks>
    /// <b>Pollution only, and the other two Layers are named holes rather than omissions.</b> Land
    /// value is not emitted at all — <c>SetLandValueTarget</c> sets a target the momentum operator
    /// chases, which is a different verb from <em>add this much</em> — and Sealing is a footprint
    /// property of a Building rather than a per-application output of a Rule. Accepting either as an
    /// emission would mean inventing a semantic for it here, which is a design change wearing a
    /// switch statement.
    /// </remarks>
    private void Emit(int building, in MapEmission emission, long applications)
    {
        Handle<Lot> lot = _world.Buildings.Lot[building];
        int lotSlot = _world.Lots.Rows.Resolve(lot);

        Cells east = CellGrid.ToCells(_world.Lots.East[lotSlot]);
        Cells north = CellGrid.ToCells(_world.Lots.North[lotSlot]);

        // A backstop, not the refusal. RulesetLoader.ReadEmission rejects this at the parse site
        // (adr/0048), so reaching here means a Ruleset got in by some other door. Both sides ask
        // MapEmission.IsEmittable so they cannot drift apart.
        if (!MapEmission.IsEmittable(emission.Layer))
        {
            throw new NotSupportedException(
                $"layer {(int)emission.Layer} is not an emittable Map Layer. Only pollution accumulates "
                + "from a source; land value is chased towards a target and Sealing is a property of a "
                + "footprint, so neither is a quantity a Rule adds per application.");
        }

        // The one place a Bin-side quantity meets a Map Layer, and the two widths differ on purpose.
        // adr/0065 widened what a Bin holds; a Layer Cell is an int and has its own magnitude bound
        // (Invariant.LayerMagnitudesAreBounded), so the narrowing happens here or nowhere. It is
        // loud rather than silent for IntegerMath.ShiftLeft's reason: there is no correct answer, so
        // the wrong answer that throws beats the one that wraps and reads as a clean field for ever.
        // Unreachable while every declared Readout is a count -- see adr/0065's third product.
        long emitted = emission.Amount * applications;

        if (emitted > int.MaxValue)
        {
            throw new InvalidOperationException(
                $"a Rule emitted {emitted} into a Map Layer, which is more than a Cell can hold. An "
                + "emission is per application and an application count is derived from a Readout, so "
                + "this is a Readout returning a stock where the Layer expects a count.");
        }

        _world.Layers.EmitPollution(east, north, (int)emitted);
    }

    /// <summary>Accumulates a delta against a Bin, merging a Bin already named by this Rule.</summary>
    /// <remarks>
    /// A linear scan because a Rule's term list is a handful — <c>02 §4.3</c>'s bakery has two — so
    /// anything cleverer would cost more to set up than the scan costs to run.
    /// </remarks>
    private void Touch(int bin, long delta) => Touch(bin, delta, bin);

    /// <summary>Accumulates a delta against a Bin, merging a Bin already named by this Rule.</summary>
    /// <remarks>
    /// <para>
    /// A linear scan because a Rule's term list is a handful — <c>02 §4.3</c>'s bakery has two — so
    /// anything cleverer would cost more to set up than the scan costs to run.
    /// </para>
    /// <para>
    /// <b><paramref name="blame"/> is the Bin a failure NAMES, which is the Bin itself except for a
    /// purchase.</b> The first writer wins where two terms merge, so a Bin a Rule both buys from and
    /// touches directly keeps the market's blame — the reading that sends the waiter somewhere a
    /// restock can reach it.
    /// </para>
    /// </remarks>
    private void Touch(int bin, long delta, int blame)
    {
        for (int i = 0; i < _touchedCount; i++)
        {
            if (_touchedBin[i] == bin)
            {
                _touchedDelta[i] += delta;
                return;
            }
        }

        Grow(ref _touchedBin, _touchedCount + 1);
        Grow(ref _touchedDelta, _touchedCount + 1);
        Grow(ref _touchedBlame, _touchedCount + 1);

        _touchedBin[_touchedCount] = bin;
        _touchedDelta[_touchedCount] = delta;
        _touchedBlame[_touchedCount] = blame;
        _touchedCount++;
    }

    private static void Grow<T>(ref T[] buffer, int needed)
    {
        if (buffer.Length >= needed)
        {
            return;
        }

        int size = buffer.Length;

        while (size < needed)
        {
            size *= 2;
        }

        Array.Resize(ref buffer, size);
    }
}
