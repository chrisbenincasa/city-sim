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
    int Applications,
    int Bin,
    Blocking Blocking,
    int Shortfall,
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
    public static RuleVerdict Fire(int instance, RuleId rule, int applications) =>
        new(instance, rule, applications, Rows.NoSlot, Blocking.Nothing, 0, ConditionId.None);

    /// <summary>An evaluation stopped by one Bin, carrying what it will subscribe with.</summary>
    public static RuleVerdict Stopped(
        int instance, RuleId rule, int bin, Blocking blocking, int shortfall) =>
        new(instance, rule, 0, bin, blocking, shortfall, ConditionId.None);
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
/// each Bin then states a count it can carry — <c>level ÷ −delta</c> for a draw, <c>headroom ÷ delta</c>
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
    private const int Unbounded = int.MaxValue;

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
    private int[] _touchedDelta = new int[8];
    private int _touchedCount;

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
                Stop(verdict);
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
    /// <summary>Evaluates a Rule and, if it fails, its <c>on_fail</c> chain.</summary>
    /// <remarks>
    /// A link does not do the head's work by another route — it <b>relieves the Bin the head failed
    /// on</b>, refilling it if it was short (<c>adr/0045</c>). So the head does not fire this Tick;
    /// the link's deposit wakes it through the Bin's wait list, which is <c>02 §7</c>'s
    /// <em>mutators wake observers</em> rather than a retry.
    /// </remarks>
    private RuleVerdict Walk(int instance, RuleId head, int ceiling)
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

    private RuleVerdict Check(int instance, RuleId rule, int ceiling)
    {
        // 02 §4's first counter, and this is the only place it can be taken honestly: a head, a link
        // below one, and Phase 3's re-check all arrive here and all cost the same walk. Counting due
        // rows instead — which is what this counter was before task 9 — cannot see a chain link at
        // all, so chain walking would have been invisible in the number stated to price it.
        _tickEvaluations++;

        RuleDefinition definition = _world.Rules.Rule(rule);
        int building = _world.Buildings.Rows.Resolve(_world.RuleInstances.Building[instance]);

        (int floor, int band) = Band(definition, building);

        if (band < ceiling)
        {
            ceiling = band;
        }

        _touchedCount = 0;

        foreach (Term term in _world.Rules.Inputs(rule))
        {
            Touch(Bin(building, term.Bin, rule), -term.Amount);
        }

        foreach (Term term in _world.Rules.Outputs(rule))
        {
            Touch(Bin(building, term.Bin, rule), term.Amount);
        }

        int applications = ceiling;

        for (int i = 0; i < _touchedCount; i++)
        {
            int bin = _touchedBin[i];
            int delta = _touchedDelta[i];
            int level = _world.Bins.LevelAt(bin);

            int affordable;

            if (delta < 0)
            {
                affordable = IntegerMath.FloorDiv(level, -delta);

                if (affordable < floor)
                {
                    return RuleVerdict.Stopped(
                        instance, rule, bin, Blocking.Level, (floor * -delta) - level);
                }
            }
            else if (delta > 0)
            {
                int headroom = _world.Bins.Capacity[bin] - level;
                affordable = IntegerMath.FloorDiv(headroom, delta);

                if (affordable < floor)
                {
                    return RuleVerdict.Stopped(
                        instance, rule, bin, Blocking.Headroom, (floor * delta) - headroom);
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

        _world.RuleInstances.Reported[instance] = ConditionId.None;

        for (int i = 0; i < _touchedCount; i++)
        {
            Handle<Bin> bin = _world.Bins.Rows.At(_touchedBin[i]);
            int delta = _touchedDelta[i];

            if (delta < 0)
            {
                _world.Withdraw(bin, -delta, tick);
            }
            else if (delta > 0)
            {
                _world.Deposit(bin, delta, tick);
            }
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
    /// <b>It does not re-arm</b> (<c>02 §4.1</c>), which is the entire economics of the design: a
    /// starved District costs nothing at all until supply arrives, where a retry timer would cost the
    /// same as a firing Rule for as long as the shortage lasted.
    /// </remarks>
    private void Stop(RuleVerdict verdict)
    {
        _world.RuleInstances.Reported[verdict.Instance] = verdict.Reported;

        _world.Subscribe(
            _world.RuleInstances.Rows.At(verdict.Instance),
            _world.Bins.Rows.At(verdict.Bin),
            verdict.Blocking,
            verdict.Shortfall);
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
    private (int Floor, int Ceiling) Band(in RuleDefinition definition, int building)
    {
        if (!definition.Apply.IsDerived)
        {
            return (definition.Apply.Min, definition.Apply.Max);
        }

        int readout = Readouts.Read(_world, building, definition.Apply.Derived);

        // The percentage is 02 §4.1's own spelling: "one unit of money applied income × 15 / 100
        // times". Floor division, because a fraction of an application is not an application.
        int count = IntegerMath.FloorDiv(readout * definition.Apply.Percent, 100);

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
    private int Bin(int building, in BinRef reference, RuleId rule)
    {
        switch (reference.Scope)
        {
            case Scope.Local:
                int slot = _world.FindBin(building, reference.Resource);

                if (slot == Rows.NoSlot)
                {
                    throw new InvalidOperationException(
                        $"rule {rule.Raw} names local Resource {reference.Resource.Raw}, and Building "
                        + $"kind {_world.Buildings.Kind[building]} declares no Bin for it. The kind's "
                        + "Bin set and the Rules attached to it are stated in one file and could be "
                        + "checked against each other at load; no refusal does so yet.");
                }

                return slot;

            case Scope.Pool:
                throw new NotSupportedException(
                    "the District Pool does not exist. The pool scope requires road connectivity, "
                    + "which arrives with Phase 2 of the roadmap; until then a pool term is a named "
                    + "hole rather than a Bin that is always empty. NOTE (adr/0050): the Pool is a "
                    + "MARKET, not a wider Bin lookup. A pool term crosses an ownership boundary, so "
                    + "the Good moves one way and money the other at the prevailing price, settled "
                    + "atomically with the Rule. Implementing this as a Bin lookup ships an "
                    + "unconserved economy, and no refusal can catch that.");

            case Scope.Global:
                throw new NotSupportedException(
                    "there are no global Bins. A city-wide Bin is one no Building owns, and where it "
                    + "would live is an entity decision — the treasury is the only content named for "
                    + "it and Money is conserved in Phase 2 of the roadmap.");

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
    private void Emit(int building, in MapEmission emission, int applications)
    {
        Handle<Lot> lot = _world.Buildings.Lot[building];
        int lotSlot = _world.Lots.Rows.Resolve(lot);

        Cells east = CellGrid.ToCells(_world.Lots.East[lotSlot]);
        Cells north = CellGrid.ToCells(_world.Lots.North[lotSlot]);

        if (emission.Layer != Layer.IndustrialPollution)
        {
            throw new NotSupportedException(
                $"layer {(int)emission.Layer} is not an emittable Map Layer. Only pollution accumulates "
                + "from a source; land value is chased towards a target and Sealing is a property of a "
                + "footprint, so neither is a quantity a Rule adds per application.");
        }

        _world.Layers.EmitPollution(east, north, emission.Amount * applications);
    }

    /// <summary>Accumulates a delta against a Bin, merging a Bin already named by this Rule.</summary>
    /// <remarks>
    /// A linear scan because a Rule's term list is a handful — <c>02 §4.3</c>'s bakery has two — so
    /// anything cleverer would cost more to set up than the scan costs to run.
    /// </remarks>
    private void Touch(int bin, int delta)
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

        _touchedBin[_touchedCount] = bin;
        _touchedDelta[_touchedCount] = delta;
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
