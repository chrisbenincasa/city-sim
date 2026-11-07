namespace Borough.Core.Rules;

using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Movement;
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
/// <param name="ToTreasury">
/// Money a fired Bin Rule <em>deposited</em> into the treasury over the interval — every output term
/// naming <c>scope = "global"</c> in a money-family Resource, summed at the count it actually fired
/// at.
/// <para>
/// 🔴 <b>It is here because it was nowhere, and the hole was 89% of a shipped world's treasury.</b>
/// On <c>rulesets/taxing.toml</c> over 24,576 Ticks the treasury closed at <b>1,621,850</b> against a
/// withheld column of <b>180,058</b>; the missing <b>1,441,792</b> was 88 firings of that file's
/// <c>rates</c> Rule, whose output names <c>global</c>. Every flow the instrument carried was folded
/// from the Policy sweeps and the paydays, so a budget's balance column could not be explained by the
/// flow columns beside it.
/// </para>
/// </param>
/// <param name="FromTreasury">
/// Money a fired Bin Rule <em>withdrew</em> from the treasury over the interval — an input term
/// naming <c>global</c>, on the same terms. Reported apart from <paramref name="ToTreasury"/> rather
/// than netted, for <c>MoneyFlowCounter.FromTreasury</c>'s reason: a net cannot distinguish a city
/// that taxed nothing and paid nothing from one that taxed heavily and paid it all back.
/// </param>
/// <param name="Tariff">Utility tariffs paid to the treasury at firing.</param>
public readonly record struct RuleActivity(
    RuleFlow Due,
    RuleFlow Evaluations,
    RuleFlow ChainRungs,
    MoneyFlow ToTreasury = default,
    MoneyFlow FromTreasury = default,
    MoneyFlow Tariff = default);

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

    // Whether the touched Bin is the TREASURY's and holds a money-family Resource, decided at the
    // call site from the term's Scope rather than here from the Bin's owner. PolicyEngine.Move gives
    // the argument for reading the Scope: a Bin is the treasury's by ownership and a transfer is the
    // treasury's by direction. The cheapness follows from the same choice -- a Rule that names no
    // global term pays one enum comparison per term and never touches a column.
    private bool[] _touchedTreasury = new bool[8];

    // The consumption remainders a per-Day input term would leave on its Bin, computed by Check and
    // banked by Fire. Held rather than written where they are computed because Phase 2 writes
    // nothing (adr/0037) and because a Rule that is checked and then blocked must accrue nothing:
    // forfeit while starved is the shape, not a case.
    private int[] _accruedBin = new int[4];
    private int[] _accruedRemainder = new int[4];
    private int _accruedCount;

    // The pool draws of the Rule currently being checked, per application: which market row and how
    // much. Read by Fire to post DistrictPoolTable.Consumed, which is the tatonnement's numerator
    // and had no writer at all before this. Term structure, which the netted deltas above have lost.
    private int[] _boughtRow = new int[4];
    private long[] _boughtAmount = new long[4];
    private int _boughtCount;

    // The same purchases again, from the two TRADERS' side rather than the market's -- who sold,
    // out of which Bin, how much and for what, plus the Bin the Good lands in on the buyer's side.
    // plans/0072 D15: one pool trade is a sale for one party and stock arriving for the other, and
    // the netted deltas cannot tell them apart -- by the time Fire sees them a seller's stock Bin
    // and a buyer's are both just Bins with a sign. Per application, like _boughtAmount, and scaled
    // by the verdict's count in Fire.
    private int[] _tradeSeller = new int[4];
    private int[] _tradeStock = new int[4];
    private int[] _tradeLanding = new int[4];
    private long[] _tradeAmount = new long[4];
    private long[] _tradePayment = new long[4];

    // The buying firm, for a purchase whose cost no Bin can carry: a service is used in the firing
    // that bought it, so Fire expenses it on the Day. Rows.NoSlot for a Good, whose cost lands on
    // the stock Bin in _tradeLanding and is expensed later, a share at a time.
    private int[] _tradeBuyer = new int[4];
    private int _tradeCount;

    // Contracts a Check chose and Fire records, for adr/0037's reason: Phase 2 writes nothing. A
    // provider of Rows.NoSlot means the Hinterland beside it is the one that supplies.
    private int[] _retainedBusiness = new int[4];
    private ResourceId[] _retainedService = new ResourceId[4];
    private int[] _retainedProvider = new int[4];
    private int[] _retainedHinterland = new int[4];
    private int _retainedCount;

    // The service units this firing buys from a city office and the allowance row they come out of,
    // per application. Fire spends them; Check only reads what is left.
    private int[] _soldAllowance = new int[4];
    private long[] _soldUnits = new long[4];
    private int _soldCount;

    // The service units this firing MAKES, per application, and the office making them. No Bin
    // carries a service, so these are the output terms the affordability walk below cannot see.
    private ResourceId[] _madeService = new ResourceId[4];
    private long[] _madeAmount = new long[4];
    private int _madeBusiness;
    private int _madeCount;

    // The delivered-Utility draws of the Rule currently being checked, per application: which
    // network row and how much. Held rather than written because Phase 2 writes nothing
    // (adr/0037). Fire posts them as DEMAND, which is a term fact the netted deltas never see --
    // a delivered Utility touches no Bin at all.
    private int[] _drawnNetwork = new int[2];
    private ResourceId[] _drawnUtility = new ResourceId[2];
    private long[] _drawnAmount = new long[2];
    private int _drawnCount;

    // The delivered Utility this firing FEEDS, per application, and which network. A plant's output
    // is bounded by nothing: there is no Bin to run out of headroom, so these are output terms the
    // affordability walk cannot see -- _madeService's position, one family over.
    private int[] _fedNetwork = new int[2];
    private long[] _fedAmount = new long[2];
    private int _fedCount;

    // The count the Rule would have applied at with its delivered Utilities whole. Demand is posted
    // at this rather than at what fired, so a brownout does not erase the demand that caused it.
    private long _drawnFull;

    // The least fraction any delivered Utility this Rule draws is delivering, One when it draws
    // none. The LEAST and not the product: a Rule short of two Utilities runs at the worse-supplied
    // one's rate and draws proportionately less of both, and multiplying would charge it twice.
    private Ratio _delivered;

    // What a provider's remaining allowance allows this firing, which Check clamps its ceiling to.
    // Read immediately after Retain returns, like Export's return value.
    private long _retainCeiling;

    // Reusable walk-search state for the reach test. One per engine, never shared across threads:
    // Phase 2 is a sequential loop over the due Rules (Evaluate), and a search writes scratch rather
    // than world state, so adr/0037's read-only phase is intact.
    private readonly WalkScratch _reachWalk = new();

    /// <summary>Counts the provider choice's reach walks, for the profiler.</summary>
    public RouteWork? ProviderRouteWork { get => _reachWalk.Work; set => _reachWalk.Work = value; }

    // What each allowance row's walk cost this buyer, so that the two passes of Office share one
    // search per candidate. Indexed by allowance row; Impassable means "not a candidate".
    private TravelTime[] _reachCost = new TravelTime[4];

    // One export per firing, which a Check chose and Fire pays. The payment is per application.
    private int _exportHinterland;
    private int _exportBusiness;
    private long _exportPayment;

    // 02 §4's counters. The first three are the Tick in flight; CloseTick folds them into the second
    // three, which are the interval a Census reading drains.
    private int _tickDue;
    private int _tickEvaluations;
    private int _tickRungs;

    private RuleFlow _dueFlow;
    private RuleFlow _evaluationFlow;
    private RuleFlow _rungFlow;

    // The same shape in Money's width, and PolicyEngine's fields exactly. Accumulated in Fire --
    // where the deposit and the withdrawal actually happen -- and never in Check, because a Rule that
    // is evaluated and then blocked has moved nothing and must appear in no flow.
    private long _tickTariff;
    private MoneyFlow _tariffFlow;
    private long _tickToTreasury;
    private long _tickFromTreasury;

    private MoneyFlow _toTreasuryFlow;
    private MoneyFlow _fromTreasuryFlow;

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
        var activity = new RuleActivity(
            _dueFlow, _evaluationFlow, _rungFlow, _toTreasuryFlow, _fromTreasuryFlow, _tariffFlow);

        _dueFlow = default;
        _evaluationFlow = default;
        _rungFlow = default;
        _toTreasuryFlow = default;
        _fromTreasuryFlow = default;
        _tariffFlow = default;

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

            if (ShoppingEngine.IsReplenishment(_world, instance)) { continue; }

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

        for (int i = 0; i < _dueCount; i++)
        {
            int instance = _due[i];
            if (ShoppingEngine.IsReplenishment(_world, instance))
            { _world.Wheel.Arm(instance, tick, (uint)_world.Rules.Shopping.Interval); }
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
        _toTreasuryFlow = _toTreasuryFlow.Fold(_tickToTreasury);
        _fromTreasuryFlow = _fromTreasuryFlow.Fold(_tickFromTreasury);

        _tariffFlow = _tariffFlow.Fold(_tickTariff);
        _tickTariff = 0;

        _tickDue = 0;
        _tickEvaluations = 0;
        _tickRungs = 0;
        _tickToTreasury = 0;
        _tickFromTreasury = 0;
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

        // The band bounds the count the Rule's inputs are measured against; `ceiling` bounds the
        // count that actually FIRES. The two were one number until a delivered Utility could serve
        // a Rule short -- a cap applied to a count that is already capped would discount twice,
        // because Phase 3 re-checks at the count Phase 2 settled on.
        (long floor, long band) = Band(_world, definition, building);

        _touchedCount = 0;
        _boughtCount = 0;
        _tradeCount = 0;
        _retainedCount = 0;
        _accruedCount = 0;
        _soldCount = 0;
        _madeCount = 0;
        _drawnCount = 0;
        _fedCount = 0;
        _drawnFull = 0;
        _delivered = Ratio.One;
        _exportHinterland = -1;

        // What the Rule's own max, its inputs and its providers allow. Lowered by every supply
        // bound below; the delivered-Utility cap and `ceiling` are applied to it at the end.
        long allowed = band;

        foreach (Term term in _world.Rules.Inputs(rule))
        {
            if (term.Bin.Scope == Scope.Pool)
            {
                if (term.PerDay)
                {
                    throw new InvalidOperationException(
                        $"rule {rule.Raw} states a per-Day quantity on a pool term for Resource "
                        + $"{term.Bin.Resource.Raw}. A purchase settles three deltas against a market "
                        + "row rather than drawing one Bin down, so there is no Bin to carry the "
                        + "remainder. The refusal belongs at load, with a file and a line; it is here "
                        + "because the loader has no such check yet.");
                }

                if (_world.Rules.IsDeliveredUtility(term.Bin.Resource))
                {
                    Draw(instance, term);
                    continue;
                }

                // A purchase is one term and three deltas, so it cannot go through Touch(Bin(...)).
                // It also fails BEFORE the affordability walk when no seller can cover a batch, which
                // is a district-wide shortage rather than one Bin being short.
                int unsupplied;

                if (_world.Rules.Family(term.Bin.Resource) == ResourceFamily.Service)
                {
                    unsupplied = Retain(instance, rule, term, floor);

                    // A provider's remaining allowance caps the count the way a Hinterland's budget
                    // caps an export's, and it comes down HERE rather than through the affordability
                    // walk because a service moves no Bin that could have carried the bound.
                    if (_retainCeiling < allowed)
                    {
                        allowed = _retainCeiling;
                    }
                }
                else
                {
                    unsupplied = Buy(instance, rule, term, floor);
                }

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

            int drawn = Bin(_world, instance, term.Bin, rule);

            Touch(drawn, -PerFiring(drawn, term, rule, definition), IsTreasuryMoney(term.Bin));
        }

        foreach (Term term in _world.Rules.Outputs(rule))
        {
            if (term.PerDay)
            {
                throw new InvalidOperationException(
                    $"rule {rule.Raw} states a per-Day quantity on an output term for Resource "
                    + $"{term.Bin.Resource.Raw}. The remainder column is consumption progress and a "
                    + "Bin carries one, so production cannot share it. The refusal belongs at load, "
                    + "with a file and a line; it is here because the loader has no such check yet.");
            }

            if (_world.Rules.IsDeliveredUtility(term.Bin.Resource))
            {
                Feed(instance, term);
                continue;
            }

            if (_world.Rules.Family(term.Bin.Resource) == ResourceFamily.Service)
            {
                // No Bin holds a service, so this term bounds nothing: it cannot run out of
                // headroom and it cannot be exported. What the units go into is the office's
                // allowance for the Day, which Fire credits. The loader allows a service output
                // only on a Business Rule that spends labour, and the labour is what bounds it.
                Handle<Business> maker = _world.RuleInstances.Business[instance];

                if (maker.IsNone)
                {
                    throw new InvalidOperationException(
                        $"rule {rule.Raw} makes a service and no Business runs it. "
                        + "RulesetLoader.RefuseServiceMadeWithoutAStaffedFirm refuses this at load.");
                }

                Grow(ref _madeService, _madeCount + 1);
                Grow(ref _madeAmount, _madeCount + 1);

                _madeService[_madeCount] = term.Bin.Resource;
                _madeAmount[_madeCount] = term.Amount;
                _madeBusiness = _world.Businesses.Rows.Resolve(maker);
                _madeCount++;

                continue;
            }

            Touch(Bin(_world, instance, term.Bin, rule), term.Amount, IsTreasuryMoney(term.Bin));
        }

        long applications = allowed;
        int shortOfLabour = Rows.NoSlot;

        for (int i = 0; i < _touchedCount; i++)
        {
            int bin = _touchedBin[i];
            long delta = _touchedDelta[i];
            long level = _world.Bins.LevelAt(bin);

            long affordable;

            if (delta < 0)
            {
                affordable = IntegerMath.FloorDiv(level, -delta);

                // Labour is reported only when nothing else is short, because a labour stop starts
                // no pressure clock and would otherwise hide a real shortage behind a closed shift.
                if (affordable < floor && IsLabour(bin))
                {
                    if (shortOfLabour == Rows.NoSlot)
                    {
                        shortOfLabour = _touchedBlame[i];
                    }

                    continue;
                }

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
                    affordable = Export(instance, rule, bin, delta, floor);

                    if (affordable < floor)
                    {
                        return RuleVerdict.Stopped(instance, rule, _touchedBlame[i], Blocking.Space);
                    }

                    _touchedDelta[i] = 0;
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

        if (shortOfLabour != Rows.NoSlot)
        {
            return RuleVerdict.Stopped(instance, rule, shortOfLabour, Blocking.Supply);
        }

        // The count the Rule would have run at with its delivered Utilities whole. Demand is posted
        // at this, which is what makes a brownout visible rather than self-concealing: a consumer
        // served a third asks for the same amount again tomorrow.
        _drawnFull = applications;

        // ⚠ The band's FLOOR is deliberately not re-applied below it. A Rule whose share rounds to
        // nothing fires at zero rather than stopping: the floor says what the Rule's own inputs must
        // cover, and a brownout is the District failing to deliver rather than the Building failing
        // to be viable. Firing at zero also keeps the demand above posted and re-arms the Rule on
        // its own rate, which is why nothing has to wake it when the network recovers.
        if (_delivered != Ratio.One)
        {
            applications = IntegerMath.MulDivFloor(applications, _delivered.Raw, Fixed.One);
        }

        // Phase 3 may lower the count Phase 2 decided and may never raise it (adr/0049). It bounds
        // the count that FIRES rather than the one measured above, so a re-check does not apply the
        // delivered fraction a second time.
        if (applications > ceiling)
        {
            applications = ceiling;
        }

        for (int i = 0; i < _touchedCount; i++)
        {
            _touchedDelta[i] *= applications;
        }

        return RuleVerdict.Fire(instance, rule, applications);
    }

    /// <summary>
    /// Records a delivered Utility this firing draws, and lowers the fraction the Rule will run at.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It never blocks and it is not a purchase.</b> A delivered Utility has no stock and no
    /// price in this slice: the Pool row is the network's address, the Bin behind it stays empty, and
    /// what the term asks of the District is <em>a share of a Day's throughput</em>. So there is no
    /// Bin that could be short and nothing a waiter could usefully sleep on — a consumer under a
    /// brownout succeeds at a reduced count, which is what makes the shortage a visible degradation
    /// rather than a stopped Building.
    /// </para>
    /// <para>
    /// ⚠ <b>Premises in no District deliver nothing.</b> That is the <see cref="Marketless"/> case
    /// arrived at from the other side — the network row the term addresses has not been opened — and
    /// the Rule fires at zero applications rather than subscribing, bounded by
    /// <c>[districts] revisit_ticks</c> exactly as a purchase is.
    /// </para>
    /// </remarks>
    private void Draw(int instance, in Term term)
    {
        int network = _world.UtilityFlowOf(instance, term.Bin.Resource);

        if (network == Rows.NoSlot)
        {
            _delivered = Ratio.Zero;
            return;
        }

        Grow(ref _drawnNetwork, _drawnCount + 1);
        Grow(ref _drawnAmount, _drawnCount + 1);
        Grow(ref _drawnUtility, _drawnCount + 1);

        _drawnNetwork[_drawnCount] = network;
        _drawnUtility[_drawnCount] = term.Bin.Resource;
        _drawnAmount[_drawnCount] = term.Amount;
        _drawnCount++;

        Ratio fraction = _world.UtilityFlows.Delivered[network];

        if (fraction < _delivered)
        {
            _delivered = fraction;
        }
    }

    /// <summary>Records a delivered Utility this firing feeds into its District's network.</summary>
    /// <remarks>
    /// A plant's output bounds nothing. There is no Bin to run out of headroom and no Hinterland to
    /// export to, so the count comes from the plant's own inputs and its <c>max</c>. A plant standing
    /// in no District feeds nothing and is not thereby stopped.
    /// </remarks>
    private void Feed(int instance, in Term term)
    {
        int network = _world.UtilityFlowOf(instance, term.Bin.Resource);

        if (network == Rows.NoSlot)
        {
            return;
        }

        Grow(ref _fedNetwork, _fedCount + 1);
        Grow(ref _fedAmount, _fedCount + 1);

        _fedNetwork[_fedCount] = network;
        _fedAmount[_fedCount] = term.Amount;
        _fedCount++;
    }

    /// <summary>
    /// Sells a firing's output to a Hinterland when the firm has no room to store it, and returns how
    /// many applications the Hinterland's budget covers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only a Good the firm makes is exported. A Rule that buys the same Good through a pool term is
    /// restocking, and exporting what it bought would spend the budget on Goods that only pass
    /// through the city.
    /// </para>
    /// <para>
    /// The output never reaches the Bin, so the Bin stays full and local buyers lose nothing. One
    /// output per firing is exported, because two would draw on one budget twice.
    /// </para>
    /// </remarks>
    /// <returns>Zero when nothing may be exported.</returns>
    private long Export(int instance, RuleId rule, int bin, long perApplication, long floor)
    {
        ResourceId resource = _world.Bins.Resource[bin];
        Handle<Business> firm = _world.RuleInstances.Business[instance];

        if (_exportHinterland >= 0
            || firm.IsNone
            || _world.Bins.OwnerKind[bin] != BinOwnerKind.Business
            || _world.Rules.Family(resource) != ResourceFamily.Good
            || Restocks(rule, resource)
            || !_world.TryExportTo(
                resource, floor * perApplication, out int hinterland, out Money price, out Money left))
        {
            return 0;
        }

        long payment = perApplication * price.Raw;
        long applications = IntegerMath.FloorDiv(left.Raw, payment);

        if (applications > 0)
        {
            _exportHinterland = hinterland;
            _exportBusiness = _world.Businesses.Rows.Resolve(firm);
            _exportPayment = payment;
        }

        return applications;
    }

    private bool Restocks(RuleId rule, ResourceId resource)
    {
        foreach (Term term in _world.Rules.Inputs(rule))
        {
            if (term.Bin.Scope == Scope.Pool && term.Bin.Resource == resource)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsLabour(int bin) =>
        _world.Rules.Family(_world.Bins.Resource[bin]) == ResourceFamily.Labour;

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
    /// <b>When no city seller holds a batch, the Outside sells it</b> at the cheapest gated edge's
    /// price (<see cref="World.TryImportPrice"/>). The buyer pays and no Bin in the city is paid, so
    /// <see cref="Fire"/> writes the Money supply down by the payment. The Good arrives from nowhere
    /// in the city. A city with no gate on a priced edge still fails on the market row.
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
    /// <see cref="Rows.NoSlot"/> when a city seller or the Outside will sell, or the market row's Bin
    /// when neither will.
    /// </returns>
    private int Buy(int instance, RuleId rule, in Term term, long floor)
    {
        int market = Bin(_world, instance, term.Bin, rule);

        if (market == Rows.NoSlot)
        {
            return Marketless;
        }

        int row = MarketRow(_world, instance, term.Bin.Resource);

        Space.Offer seller = Seller(instance, row, floor * term.Amount);

        if (seller.Bin == Rows.NoSlot && !_world.TryImportPrice(term.Bin.Resource, out _))
        {
            return market;
        }

        if (!_world.TryMoneyResource(out ResourceId money))
        {
            throw new InvalidOperationException(
                "a pool term reached the RuleEngine without a money Resource. RulesetLoader refuses "
                + "pool terms when the Ruleset declares no Resource whose family is money.");
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

        long price;

        if (seller.Bin != Rows.NoSlot)
        {
            price = _world.DistrictPools.Price[row].Raw;
        }
        else
        {
            _world.TryImportPrice(term.Bin.Resource, out Money imported);
            price = imported.Raw;

            // A buyer that can meet the city price but not the import price waits on the market row,
            // because a restock lowers what it owes without depositing into its purse.
            long held = _world.Bins.LevelAt(purse);

            if (_world.Markets.SellerCount(_world, row) > 0
                && held < floor * term.Amount * price
                && held >= floor * term.Amount * _world.DistrictPools.Price[row].Raw)
            {
                return market;
            }
        }

        long payment = term.Amount * price;

        if (seller.Bin != Rows.NoSlot)
        {
            Touch(seller.Bin, -term.Amount, market);
            Touch(purse, -payment);
            Touch(_world.Bins.Rows.Resolve(_world.Businesses.Balance[seller.Business]), payment);
        }
        else
        {
            Touch(purse, -payment);
        }

        Grow(ref _boughtRow, _boughtCount + 1);
        Grow(ref _boughtAmount, _boughtCount + 1);

        _boughtRow[_boughtCount] = row;
        _boughtAmount[_boughtCount] = term.Amount;
        _boughtCount++;

        // 🔴 THE DIRECTION, recorded here because this is the only place that still knows it.
        // adr/0050: the Good moves one way and money the other. The SELLER delivered Goods out of
        // seller.Bin and was paid, which is revenue and a cost of goods (plans/0072 D15). The BUYER
        // paid for stock, which is not an expense and not revenue -- it is cost carried on the Bin
        // the stock lands in, until it is sold on (D24). ***Reading the buyer's payment as revenue
        // would invert the sign of every profit figure in the city***, which is why the two legs
        // are posted through two differently named methods rather than one with a flag.
        Grow(ref _tradeSeller, _tradeCount + 1);
        Grow(ref _tradeStock, _tradeCount + 1);
        Grow(ref _tradeLanding, _tradeCount + 1);
        Grow(ref _tradeAmount, _tradeCount + 1);
        Grow(ref _tradePayment, _tradeCount + 1);
        Grow(ref _tradeBuyer, _tradeCount + 1);

        _tradeSeller[_tradeCount] = seller.Business;
        _tradeStock[_tradeCount] = seller.Bin;
        _tradeLanding[_tradeCount] = Landing(instance, rule, term.Bin.Resource);
        _tradeAmount[_tradeCount] = term.Amount;
        _tradePayment[_tradeCount] = payment;
        _tradeBuyer[_tradeCount] = Rows.NoSlot;
        _tradeCount++;

        return Rows.NoSlot;
    }

    /// <summary>
    /// Resolves one <c>pool</c> input for a Professional Service into a payment to the firm's provider,
    /// or names the market row the firm waits on when no provider will supply it (<c>adr/0179</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The firm keeps the provider its contract names while that provider can still serve it</b>,
    /// and picks again only when it cannot. For a city office that means it still exists, is still in
    /// reach, and still has units left today; for a Hinterland it means the edge still has a gate and
    /// still prices the service.
    /// </para>
    /// <para>
    /// <b>A city office beats an import, and that is the one place a standing provider is dropped
    /// while it can still supply.</b> A firm on a Hinterland contract looks for a local provider on
    /// every purchase, because the whole point of raising offices is that they displace imports. The
    /// reverse does not hold: a firm with a local provider does not shop for a nearer one.
    /// </para>
    /// <para>
    /// <b>A local sale is a city transfer and an import is not.</b> The office is paid at the buyer's
    /// own market row — the price every local seller of a Good charges — out of the buyer's purse into
    /// the office's balance, so the Money supply does not move. An import pays the Outside at its
    /// edge's price and <see cref="Fire"/> writes the supply down. Neither moves a Bin of stock: a
    /// service is never stocked.
    /// </para>
    /// <para>
    /// <b>What is left of the provider's allowance caps the count</b>, and <c>Fire</c> is where the
    /// units are actually spent (<c>adr/0037</c>). So Phase 3's re-check sees what earlier settlers
    /// took this Tick, and an office sold out mid-Tick sends its next client to another provider.
    /// </para>
    /// <para>
    /// The firm waits on its District's market row for the service, so a new gate wakes it through
    /// <see cref="World.WakeMarkets"/> and an office's first units through
    /// <see cref="World.WakeServiceMarkets"/>, as a seller's deposit wakes a buyer of Goods.
    /// </para>
    /// </remarks>
    private int Retain(int instance, RuleId rule, in Term term, long floor)
    {
        _retainCeiling = Unbounded;

        int market = Bin(_world, instance, term.Bin, rule);

        if (market == Rows.NoSlot)
        {
            return Marketless;
        }

        Handle<Business> trader = _world.RuleInstances.Business[instance];

        if (trader.IsNone)
        {
            throw new InvalidOperationException(
                $"rule {rule.Raw} buys a service and no Business runs it. "
                + "RulesetLoader.RefuseServiceBoughtWithoutAFirm refuses this at load.");
        }

        int business = _world.Businesses.Rows.Resolve(trader);
        ResourceId service = term.Bin.Resource;
        long batch = floor * term.Amount;

        int contract = _world.FindContract(business, service);
        int office = Rows.NoSlot;
        int hinterland = -1;

        if (contract != Rows.NoSlot)
        {
            if (_world.Businesses.Rows.TryResolve(
                _world.ServiceContracts.Provider[contract], out int kept))
            {
                // Units first: it is an array read where the reach test is a walk search, and a
                // provider with nothing left today is refused without asking the Road Graph.
                if (_world.UnitsLeft(_world.FindAllowance(kept, service)) >= batch
                    && InReach(business, kept, service, out _))
                {
                    office = kept;
                }
            }
            else
            {
                hinterland = _world.ServiceContracts.Hinterland[contract];
            }
        }

        if (office == Rows.NoSlot)
        {
            office = Office(instance, business, service, batch);
        }

        if (office == Rows.NoSlot
            && !_world.Supplies(hinterland, service)
            && !_world.TryImportFrom(service, out hinterland, out _))
        {
            return market;
        }

        if (!_world.TryMoneyResource(out ResourceId money))
        {
            throw new InvalidOperationException(
                "a pool term reached the RuleEngine without a money Resource. RulesetLoader refuses "
                + "pool terms when the Ruleset declares no Resource whose family is money.");
        }

        int purse = _world.FindLocalBin(instance, money);

        if (purse == Rows.NoSlot)
        {
            throw new InvalidOperationException(
                $"rule {rule.Raw} buys a service and its Business holds no balance to pay from.");
        }

        long payment;

        if (office == Rows.NoSlot)
        {
            payment = term.Amount * _world.Rules.ImportPrice(hinterland, service).Raw;
            Touch(purse, -payment);
        }
        else
        {
            int row = MarketRow(_world, instance, service);
            int allowance = _world.FindAllowance(office, service);

            payment = term.Amount * _world.DistrictPools.Price[row].Raw;

            Touch(purse, -payment);
            Touch(_world.Bins.Rows.Resolve(_world.Businesses.Balance[office]), payment);

            _retainCeiling = IntegerMath.FloorDiv(_world.UnitsLeft(allowance), term.Amount);

            // The row's own consumption, which the tatonnement reads as its numerator -- recorded
            // for a city sale and not for an import, exactly as a Good's purchase records the leg
            // that moved inside the District.
            Grow(ref _boughtRow, _boughtCount + 1);
            Grow(ref _boughtAmount, _boughtCount + 1);

            _boughtRow[_boughtCount] = row;
            _boughtAmount[_boughtCount] = term.Amount;
            _boughtCount++;

            Grow(ref _soldAllowance, _soldCount + 1);
            Grow(ref _soldUnits, _soldCount + 1);

            _soldAllowance[_soldCount] = allowance;
            _soldUnits[_soldCount] = term.Amount;
            _soldCount++;
        }

        // Seller NoSlot is Fire's reading of "the Outside sold this", which writes the Money supply
        // down. A stock Bin of NoSlot beside a real seller is a sale with no Goods behind it, which
        // is what a service is. The buyer is named so that Fire can expense what it paid: there is
        // no landing Bin to carry the cost, and the office's revenue is booked.
        //
        // ⚠ An IMPORT names no buyer, so the import leg stays unexpensed as it was before offices
        // existed. The asymmetry that mattered was a city sale with revenue on one side only;
        // pricing an import into a firm's profit is a separate change, filed in plans/export-base.md.
        Grow(ref _tradeSeller, _tradeCount + 1);
        Grow(ref _tradeStock, _tradeCount + 1);
        Grow(ref _tradeLanding, _tradeCount + 1);
        Grow(ref _tradeAmount, _tradeCount + 1);
        Grow(ref _tradePayment, _tradeCount + 1);
        Grow(ref _tradeBuyer, _tradeCount + 1);

        _tradeSeller[_tradeCount] = office;
        _tradeStock[_tradeCount] = Rows.NoSlot;
        _tradeLanding[_tradeCount] = Rows.NoSlot;
        _tradeAmount[_tradeCount] = term.Amount;
        _tradePayment[_tradeCount] = payment;
        _tradeBuyer[_tradeCount] = office == Rows.NoSlot ? Rows.NoSlot : business;
        _tradeCount++;

        if (Rebound(contract, office, hinterland))
        {
            Grow(ref _retainedBusiness, _retainedCount + 1);
            Grow(ref _retainedService, _retainedCount + 1);
            Grow(ref _retainedProvider, _retainedCount + 1);
            Grow(ref _retainedHinterland, _retainedCount + 1);

            _retainedBusiness[_retainedCount] = business;
            _retainedService[_retainedCount] = service;
            _retainedProvider[_retainedCount] = office;
            _retainedHinterland[_retainedCount] = hinterland;
            _retainedCount++;
        }

        return Rows.NoSlot;
    }

    /// <summary>Whether the provider just chosen is not the one the contract already names.</summary>
    /// <remarks>
    /// A contract whose <c>Provider</c> no longer resolves names nobody, so any provider is a change
    /// — which is how a client of a demolished office is re-contracted rather than left pointing at a
    /// freed row.
    /// </remarks>
    private bool Rebound(int contract, int office, int hinterland)
    {
        if (contract == Rows.NoSlot)
        {
            return true;
        }

        return _world.Businesses.Rows.TryResolve(
            _world.ServiceContracts.Provider[contract], out int named)
            ? named != office
            : office != Rows.NoSlot || _world.ServiceContracts.Hinterland[contract] != hinterland;
    }

    /// <summary>
    /// The city office a firm takes as its provider, or <see cref="Rows.NoSlot"/> when none in reach
    /// can serve a whole batch today.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The candidates are the allowance rows</b>, which is the set of offices that have actually
    /// produced something: an office whose Rule has never fired holds no row and is not a provider,
    /// however close it stands. The buyer itself is excluded — a firm cannot contract with itself.
    /// </para>
    /// <para>
    /// <b>Shortest walk wins and ties go to a draw</b> (<c>02 §8</c> rule 5). The enumeration is slot
    /// order and nothing is ever chosen <em>by</em> slot order, which is <see cref="Seller"/>'s
    /// arrangement with a different discriminator in front of it. Ties are the ordinary case for a
    /// remote service, where every provider reports the same distance.
    /// </para>
    /// <para>
    /// <b>A whole batch, like a seller of Goods</b> (<c>adr/0139</c>): an office with less than
    /// <c>floor × amount</c> left today cannot serve this firing at all, so the firm looks past it
    /// rather than being served short. A batch of nothing asks nobody — a derived apply count of
    /// zero buys nothing, and admitting every office on <c>UnitsLeft >= 0</c> would pay the whole
    /// fan-out to pick a provider that supplies no units.
    /// </para>
    /// <para>
    /// <b>The first pass keeps each candidate's walk so the second pass needs no search.</b> Choosing
    /// a winner needs the cheapest cost and how many share it, which one pass cannot know until it
    /// ends — and re-walking every candidate to find the drawn one again would double the only
    /// expensive thing this method does.
    /// </para>
    /// </remarks>
    private int Office(int instance, int buyer, ResourceId service, long batch)
    {
        if (batch <= 0)
        {
            return Rows.NoSlot;
        }

        int rows = _world.ServiceAllowances.Rows.SlotCount;
        TravelTime best = TravelTime.Impassable;
        int tied = 0;

        Grow(ref _reachCost, rows);

        for (int row = 0; row < rows; row++)
        {
            _reachCost[row] = Candidate(row, buyer, service, batch, out TravelTime cost)
                ? cost
                : TravelTime.Impassable;

            if (_reachCost[row].IsImpassable)
            {
                continue;
            }

            if (tied == 0 || cost < best)
            {
                best = cost;
                tied = 1;
            }
            else if (cost == best)
            {
                tied++;
            }
        }

        if (tied == 0)
        {
            return Rows.NoSlot;
        }

        // The service is a draw coordinate, not just the Rule Instance: a firm buying two services
        // in one firing would otherwise take the same ordinal for both whenever their tied counts
        // matched, and the two picks would be perfectly correlated. Shifted into the high half, which
        // is EmploymentEngine's and PlacementEngine's form -- a small id XORed into the low bits
        // collides with the instance ids just above it, and Mix cannot undo a collision in its input.
        ulong entity = Randomness.Mix(
            _world.RuleInstances.Rows.IdAt(instance) ^ ((ulong)(uint)service.Raw << 32));

        ulong draw = Randomness.Draw(_key, entity, _world.Tick, PurposeTag.ProviderChoice);

        int take = (int)(draw % (ulong)tied);

        for (int row = 0; row < rows; row++)
        {
            if (_reachCost[row] == best && take-- == 0)
            {
                return _world.Businesses.Rows.Resolve(_world.ServiceAllowances.Provider[row]);
            }
        }

        return Rows.NoSlot;
    }

    /// <summary>Whether one allowance row can serve this buyer, and how far away it is.</summary>
    /// <remarks>
    /// Cheapest test first: the units are an array read, the box is two Lot reads, and only what
    /// survives both is walked.
    /// </remarks>
    private bool Candidate(int row, int buyer, ResourceId service, long batch, out TravelTime cost)
    {
        cost = TravelTime.Impassable;

        return _world.ServiceAllowances.Rows.IsLive(row)
            && _world.ServiceAllowances.Service[row] == service
            && _world.UnitsLeft(row) >= batch
            && _world.Businesses.Rows.TryResolve(
                _world.ServiceAllowances.Provider[row], out int provider)
            && provider != buyer
            && InReach(buyer, provider, service, out cost);
    }

    /// <summary>
    /// Whether a provider is close enough to serve a buyer, and the free-flow walk from the buyer's
    /// door to the provider's.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One walk per candidate, which is <see cref="ServiceEngine"/>'s shape and its reason.</b>
    /// A Household judges a school by walking to it rather than by reading a structure laid over the
    /// graph, and a firm judges an office the same way: door to door, in the buyer's direction of
    /// travel, charged the pedestrian crossing. Foot is the mode until a firm's errands are Trips of
    /// their own.
    /// </para>
    /// <para>
    /// 🔴 <b>It was the all-pairs travel-time matrix, and the matrix is unaffordable here.</b> Its
    /// order is the Road Graph's <em>occupied</em> partition count, which <c>serviced.toml</c> and
    /// every fixture sharing its <c>[roads]</c> lattice drive to all 16,384 partitions on the map —
    /// measured 10/17/2026: one rebuild ran 16,384 one-to-all searches, settled 4,311,760,896 nodes
    /// and held 1.07 GB of entries. Those counts are deterministic; what it cost in seconds is a
    /// provisional figure in <c>plans/export-base.md</c> and not a quiet-machine one.
    /// <c>RoutingPartition.Count</c>'s own remark predicts exactly that and sizes a city at ~1,444
    /// partitions; the shipped generator does not keep to it. A walk per candidate is bounded by the
    /// offices inside the box instead, and it is <em>exact</em> where an access-node entry is wrong
    /// by up to 9.2 minutes at each end.
    /// </para>
    /// <para>
    /// <b>A straight-line box comes first and no walk is ever run outside it</b>, which is the half
    /// of <c>ServiceEngine.Candidates</c> that makes a walk per candidate affordable. The bound is
    /// <see cref="EmploymentEngine.Radius"/> over the reach, so it over-supplies candidates and never
    /// under-supplies them, and an office across the map is rejected by two Lot reads rather than by
    /// a search that settles the city.
    /// </para>
    /// <para>
    /// <b>A remote service costs nothing to judge and every provider of one is equally near.</b> It
    /// returns before the box and the search, so a Ruleset whose services are all remote never asks
    /// the Road Graph anything — and because every candidate then reports the same distance,
    /// <see cref="Office"/>'s tie-break draw is what chooses between them.
    /// </para>
    /// </remarks>
    private bool InReach(int buyer, int provider, ResourceId service, out TravelTime cost)
    {
        ServiceReach reach = _world.Rules.ReachOf(service);

        if (reach.IsUnlimited)
        {
            cost = TravelTime.Zero;

            return true;
        }

        cost = TravelTime.Impassable;

        if (!_world.Buildings.Rows.TryResolve(_world.Businesses.Building[buyer], out int from)
            || !_world.Buildings.Rows.TryResolve(_world.Businesses.Building[provider], out int to))
        {
            return false;
        }

        TravelTime budget = TravelTime.FromMinutes(reach.Minutes);

        if (!InBox(from, to, budget))
        {
            return false;
        }

        cost = WalkRouting.Cost(
            _world.Roads,
            TravelMode.Foot,
            _world.AccessPoint(from, TravelMode.Foot),
            _world.AccessPoint(to, TravelMode.Foot),
            _world.Rules.Trips.CrossingCost,
            _reachWalk);

        return !cost.IsImpassable && cost <= budget;
    }

    /// <summary>
    /// Whether a walk of <paramref name="budget"/> could reach <paramref name="to"/>'s Lot from
    /// <paramref name="from"/>'s at all, as the crow flies.
    /// </summary>
    /// <remarks>
    /// <c>ServiceEngine.Candidates</c>' box, in Cells, and the rounding argument is on
    /// <see cref="EmploymentEngine.Radius"/>: no route is shorter than the straight line, so every
    /// Building a walk within the budget could reach is inside the box. Some Buildings inside it are
    /// unreachable, and those are the ones the walk then rejects.
    /// </remarks>
    private bool InBox(int from, int to, TravelTime budget)
    {
        if (!_world.Lots.Rows.TryResolve(_world.Buildings.Lot[from], out int here)
            || !_world.Lots.Rows.TryResolve(_world.Buildings.Lot[to], out int there))
        {
            return false;
        }

        CellRect box = CellRect.At(
                CellGrid.ToCellsClamped(_world.Lots.East[here]),
                CellGrid.ToCellsClamped(_world.Lots.North[here]))
            .Dilate(EmploymentEngine.Radius(budget, _world.Rules.Roads.WalkSpeed))
            .Clamp();

        return box.Contains(
            CellGrid.ToCellsClamped(_world.Lots.East[there]),
            CellGrid.ToCellsClamped(_world.Lots.North[there]));
    }

    /// <summary>
    /// The city seller that will fill a batch, or no seller when none holds one.
    /// </summary>
    private Space.Offer Seller(int instance, int row, long batch)
    {
        var none = new Space.Offer(Rows.NoSlot, Rows.NoSlot);
        int sellers = _world.Markets.SellerCount(_world, row);

        if (sellers == 0 || _world.Markets.Largest(_world, row) < batch)
        {
            return none;
        }

        // Keyed on the buying Rule Instance's monotonic id, so it is a lottery number the buyer holds
        // rather than a rotation the whole District performs in step.
        ulong draw = Randomness.Draw(
            _key, _world.RuleInstances.Rows.IdAt(instance), _world.Tick, PurposeTag.SellerChoice);

        int start = (int)(draw % (ulong)sellers);

        for (int i = 0; i < sellers; i++)
        {
            Space.Offer candidate = _world.Markets.Seller(_world, row, (start + i) % sellers);

            if (_world.Bins.LevelAt(candidate.Bin) >= batch)
            {
                return candidate;
            }
        }

        return none;
    }

    /// <summary>
    /// The Bin a <c>pool</c> purchase's Good lands in on the buyer's side, or
    /// <see cref="Rows.NoSlot"/> when it lands nowhere.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Off the Rule's OUTPUT terms and not off the buyer's Bin list</b>, and the difference is
    /// the whole of what this method is for. <c>Buy</c> takes the Good out of the seller's Bin and
    /// puts it nowhere; a Rule that wants to <em>hold</em> what it bought says so with a matching
    /// local output — <c>rulesets/shopping.toml</c>'s <c>restock</c> is <c>pool</c> in and
    /// <c>local</c> out of the same Resource, which is a shop restocking. Asking the buyer's Bin
    /// list instead would find a Bin whether or not the Good ever reached it, and would put the
    /// cost of an input a Rule <em>consumed</em> onto stock it did not buy.
    /// </para>
    /// <para>
    /// ⚠ <b>Nowhere is a legitimate answer and its accounting is deliberately absent.</b> A Rule
    /// that buys a Good and consumes it in the same firing has incurred a production cost, and
    /// <c>plans/0072</c> D15 files production cost as still to be designed. Recording it as an
    /// expense here would be answering that question in a helper.
    /// </para>
    /// </remarks>
    private int Landing(int instance, RuleId rule, ResourceId resource)
    {
        foreach (Term term in _world.Rules.Outputs(rule))
        {
            if (term.Bin.Scope == Scope.Local && term.Bin.Resource == resource)
            {
                return Bin(_world, instance, term.Bin, rule);
            }
        }

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

        // 🔴 BEFORE the deltas and not after, because the cost of what a seller sold is a share of
        // what the seller was HOLDING (plans/0072 D24) and the loop below is what stops it holding
        // it. Posted here rather than in Check for DistrictPoolTable.Consumed's reason exactly: a
        // Rule that was evaluated and then blocked has sold nothing, and Phase 2 writes nothing
        // (adr/0037).
        //
        // ⚠ Zero applications is the Marketless success, and every call below is a no-op at zero.
        for (int i = 0; i < _tradeCount; i++)
        {
            long sold = _tradeAmount[i] * verdict.Applications;
            long paid = _tradePayment[i] * verdict.Applications;

            if (_tradeSeller[i] == Rows.NoSlot)
            {
                _world.PayOutside(new Money(paid));
            }
            else if (_tradeStock[i] == Rows.NoSlot)
            {
                // A city provider of a service: revenue with no stock Bin behind it, which is
                // BusinessAccounts.Serve's own case rather than a delivery of nothing. The money
                // leg is a transfer between two city Bins, so the supply of record does not move.
                BusinessAccounts.Serve(_world, _tradeSeller[i], paid, tick);
            }
            else
            {
                BusinessAccounts.Deliver(
                    _world,
                    _tradeSeller[i],
                    _tradeStock[i],
                    sold,
                    _world.Bins.LevelAt(_tradeStock[i]),
                    paid,
                    tick);
            }

            BusinessAccounts.Stock(_world, _tradeLanding[i], paid);
            BusinessAccounts.Consume(_world, _tradeBuyer[i], paid, tick);
        }

        if (verdict.Applications > 0 && _exportHinterland >= 0)
        {
            _world.PayForExport(
                _exportBusiness,
                _exportHinterland,
                new Money(_exportPayment * verdict.Applications),
                tick);
        }

        if (verdict.Applications > 0)
        {
            for (int i = 0; i < _retainedCount; i++)
            {
                _world.Contract(
                    _retainedBusiness[i],
                    _retainedService[i],
                    _retainedProvider[i],
                    _retainedHinterland[i],
                    tick);
            }

            // The provider's Day, both ways round: what its clients took out of its allowance, and
            // what an office's own firing put into it. Here rather than in Check because Phase 2
            // writes nothing (adr/0037), and scaled by the count Phase 3 settled on.
            for (int i = 0; i < _soldCount; i++)
            {
                _world.SellService(_soldAllowance[i], _soldUnits[i] * verdict.Applications);
            }

            for (int i = 0; i < _madeCount; i++)
            {
                _world.MakeService(
                    _madeBusiness, _madeService[i], _madeAmount[i] * verdict.Applications, tick);
            }
        }

        for (int i = 0; i < _touchedCount; i++)
        {
            Handle<Bin> bin = _world.Bins.Rows.At(_touchedBin[i]);
            long delta = _touchedDelta[i];

            if (delta < 0)
            {
                _world.Withdraw(bin, -delta, tick);

                // The treasury's flow, taken HERE and not in Check, and the two are different
                // numbers: Check runs for every due Rule and for every rung of a failed chain, and
                // a Rule that is evaluated and then blocked has moved nothing. Positive and
                // negative are accumulated apart rather than netted for
                // MoneyFlowCounter.FromTreasury's reason -- a net cannot distinguish a city that
                // taxed nothing and paid nothing from one that taxed heavily and paid it all back.
                if (_touchedTreasury[i])
                {
                    _tickFromTreasury += -delta;
                }

                // 04 section 6 step 6, the recovery half: a Household that DREW the Good had its
                // occasion met. A withdrawal is the shop counter of 04 section 2 -- "a Household
                // buys Food (an integer leaving a Bin) and its Sustenance moves toward zero".
                MoveNeed(instance, _touchedBin[i], met: true);
            }
            else if (delta > 0)
            {
                _world.Deposit(bin, delta, tick);

                if (_touchedTreasury[i])
                {
                    _tickToTreasury += delta;
                }
            }
        }

        for (int i = 0; i < _tradeCount; i++)
        {
            if (_tradeSeller[i] != Rows.NoSlot)
            {
                _world.Arrears.SettleBusiness(_tradeSeller[i], tick);
            }
        }

        SettleRent(instance, tick);

        // The part of a per-Day quantity that did not reach a whole unit, banked HERE and nowhere
        // else, so a Rule that was checked and then blocked accrues nothing. Check holds these
        // because Phase 2 writes nothing (adr/0037) and because the rung that fires is not always
        // the rung first evaluated -- a fallback chain re-checks on the way down, and the last check
        // to succeed is the one whose terms Fire is settling.
        //
        // ⚠ Zero applications moves no Goods, so it accrues nothing either. Unlike the deltas above,
        // a remainder is not scaled by the applications and would advance on its own.
        if (verdict.Applications > 0)
        {
            for (int i = 0; i < _accruedCount; i++)
            {
                _world.Bins.Progress[_accruedBin[i]] = _accruedRemainder[i];
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

        // A delivered Utility's two columns, and they are scaled by DIFFERENT counts. Demand is what
        // the Rule would have drawn with the network whole, so a Day of brownout still states the
        // load that caused it; production is what the plant's own inputs actually bought, which is
        // the count that fired. Posted here rather than in Check because Phase 2 writes nothing
        // (adr/0037), and from the term list because no Bin carries either figure.
        //
        // Both posts saturate at UtilityFlowTable.Bound. An amount and an apply count may each be
        // int.MaxValue, so an unbounded accumulator wraps negative on the third firing of a Ruleset
        // the loader accepts.
        for (int i = 0; i < _drawnCount; i++)
        {
            _world.UtilityFlows.Demand(_drawnNetwork[i], _drawnAmount[i], _drawnFull);
            BillUtility(instance, _drawnUtility[i], _drawnAmount[i], verdict.Applications, tick);
        }

        for (int i = 0; i < _fedCount; i++)
        {
            _world.UtilityFlows.Produce(_fedNetwork[i], _fedAmount[i], verdict.Applications);
        }

        int building = _world.Buildings.Rows.Resolve(_world.RuleInstances.Building[instance]);

        foreach (MapEmission emission in _world.Rules.Emissions(rule))
        {
            Emit(building, emission, verdict.Applications, tick);
        }

        _world.Wheel.Arm(instance, tick, definition.Rate);
    }

    private void BillUtility(int instance, ResourceId utility, long amount, long applications, Ticks tick)
    {
        int tariff = _world.Rules.TariffOf(utility);
        if (tariff < 0 || applications == 0) { return; }

        PolicyDefinition policy = _world.Rules.Policies[tariff];
        long price = long.Max(0, _world.Policies.AmountOf(tariff, policy));
        long units = (long)Int128.Min(long.MaxValue, (Int128)amount * applications);
        long bill = (long)Int128.Min(long.MaxValue, (Int128)units * price);
        int business = _world.Businesses.Rows.TryResolve(_world.RuleInstances.Business[instance], out int firm)
            ? firm : Rows.NoSlot;
        Handle<Bin> purse = _world.Households.Rows.TryResolve(_world.RuleInstances.Household[instance], out int household)
            ? _world.Households.Balance[household]
            : _world.Businesses.Balance[business];
        int payer = _world.Bins.Rows.Resolve(purse);
        long paid = long.Min(bill, _world.Bins.LevelAt(payer));
        if (paid > 0)
        {
            _world.Withdraw(purse, paid, tick);
            int treasury = _world.FindTreasuryBin(policy.Resource);
            _world.Deposit(_world.Bins.Rows.At(treasury), paid, tick);
            BusinessAccounts.Consume(_world, business, paid, tick);
            _tickTariff = checked(_tickTariff + paid);
        }

        int building = _world.Buildings.Rows.Resolve(_world.RuleInstances.Building[instance]);
        _world.UtilityBills.Record(_world.Buildings, building, _world.Rules.ResourceKey(utility),
            BusinessAccounts.DayOf(tick), paid, bill - paid);
    }

    /// <summary>
    /// Settles the running tenant's rent arrears where this Rule's own output credited its money
    /// balance (<c>plans/money-circulation.md</c> L16).
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>ANY credit to the balance, not only a sale.</b> A Bin Rule can state <c>global</c> money
    /// in and <c>local</c> money out — an ordinary balanced transfer with no Pool trade anywhere in
    /// it — and that is income to whoever runs the Rule. ***Settling only the trade sellers left that
    /// money spendable until the next Day boundary***, which is the case L16 exists to rule out.
    /// </para>
    /// <para>
    /// <b>Resolved from the INSTANCE and not from the Bin</b>, because a tenant-owned Bin names no
    /// owner (<c>adr/0143</c>) while a Rule Instance names its Household and its Business outright.
    /// The balance handle is then compared against the credited slot, so a Household's Goods Bin or
    /// another tenant's purse cannot be mistaken for this one.
    /// </para>
    /// <para>
    /// ⚠ <b>Money only.</b> A Rule that delivers Food credits a Bin too, and settling there would pay
    /// rent out of cash the tenant was already holding — a payment on a Tick nothing was earned.
    /// </para>
    /// </remarks>
    private void SettleRent(int instance, Ticks tick)
    {
        if (!_world.Rules.Rent.Collects || !_world.TryMoneyResource(out ResourceId money))
        {
            return;
        }

        int household = _world.Households.Rows.TryResolve(
            _world.RuleInstances.Household[instance], out int housed) ? housed : Rows.NoSlot;

        int business = _world.Businesses.Rows.TryResolve(
            _world.RuleInstances.Business[instance], out int premised) ? premised : Rows.NoSlot;

        if (household == Rows.NoSlot && business == Rows.NoSlot)
        {
            return;
        }

        for (int i = 0; i < _touchedCount; i++)
        {
            int bin = _touchedBin[i];

            if (_touchedDelta[i] <= 0 || _world.Bins.Resource[bin] != money)
            {
                continue;
            }

            if (household != Rows.NoSlot && Purse(_world.Households.Balance[household]) == bin)
            {
                _world.Arrears.SettleHousehold(household, tick);
            }
            else if (business != Rows.NoSlot && Purse(_world.Businesses.Balance[business]) == bin)
            {
                _world.Arrears.SettleBusiness(business, tick);
            }
        }
    }

    /// <summary>The Bin slot behind a balance handle, or <c>Rows.NoSlot</c>.</summary>
    private int Purse(Handle<Bin> balance) =>
        _world.Bins.Rows.TryResolve(balance, out int slot) ? slot : Rows.NoSlot;

    /// <summary>
    /// Recovers a Household's Need by one met occasion, on <c>04 §6</c> step 6.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The RECOVERY half only.</b> <c>04 §6</c> asks for a scalar that <em>"falls while unmet and
    /// recovers when met, so a dry afternoon and a dry month are one mechanism at two depths"</em> —
    /// and the two halves are written differently on purpose. <b>Recovery is per occasion</b>, because
    /// a Rule that fires is a Rule being visited and a meal really is an event.
    /// <see cref="RefreshNeed"/> is the fall, and it is a duration.
    /// </para>
    /// <para>
    /// 🔴 <b>The fall was per occasion too, and it was WRONG.</b> A blocked Rule has exactly one
    /// failed occasion — <see cref="Stop"/> sleeps it on its Bin — so a shortage nothing ends bought
    /// one step and then silence. On <c>rulesets/hungry.toml</c> before the repair: <b>7</b> steps a
    /// Household over 32,768 Ticks where <c>consume</c>'s rate of 32 gives 1,024, the 7 tracking
    /// <em>rehousings</em>. ***A success is an event and a failure is a state.***
    /// </para>
    /// <para>
    /// ⚠ <b>THE OCCASION IS A RULE FIRING, NOT YET A SHOPPING TRIP.</b> <c>04 §6</c> step 4 makes it a
    /// <b>Trip</b> to a Provider List (<c>adr/0067</c>), unbuilt. What is built has the same shape, so
    /// ***the Trip will replace this call site and the accumulator will not change.***
    /// </para>
    /// </remarks>
    /// <param name="instance">The Rule Instance whose occasion this was.</param>
    /// <param name="bin">The Bin the Good was drawn from.</param>
    /// <param name="met">Whether the occasion succeeded. Only <c>true</c> reaches here now.</param>
    private void MoveNeed(int instance, int bin, bool met)
    {
        NeedRuleset needs = _world.Rules.Needs;

        if (!needs.Runs)
        {
            return;
        }

        Handle<Household> household = _world.RuleInstances.Household[instance];

        if (household.IsNone)
        {
            return;
        }

        Need need = _world.Rules.NeedOf(_world.Bins.Resource[bin]);

        if (need == Need.None)
        {
            return;
        }

        int slot = _world.Households.Rows.Resolve(household);

        Column<int>? column = _world.Households.NeedColumn(need);

        if (column is null)
        {
            return;
        }

        int moved = met
            ? column[slot] + needs.RecoverOf(need)
            : column[slot] - needs.DegradeOf(need);

        Write(column, slot, moved, needs.Floor);
    }

    /// <summary>Deepens every starving Household's Need to the depth its shortage has lasted.</summary>
    /// <remarks>
    /// <para>
    /// <b>Once a Day, and the period is DERIVED.</b> The Ruleset states a degrade <em>per Day</em>, so
    /// a Day is where the depth is exact and no cadence number enters the design. A coarser period
    /// would move only staleness, since <see cref="RefreshNeed"/> recomputes rather than accumulates.
    /// </para>
    /// <para>
    /// 🔴 <b>Its own pass rather than a ride on the Zone Rule sweep.</b> <c>ZoneRuleEngine.Condemn</c>
    /// reaches a Building only through a <em>sample</em> and only when its kind declares
    /// <c>condemn_after_days</c> or <c>tenancy_ends_after_days</c> — so hunger would have been a silent
    /// function of two keys about Buildings. ***A Household mechanism must not depend on a Ruleset
    /// having opinions about Buildings.***
    /// </para>
    /// <para>
    /// ⚠ <b>An O(n) staggered pass</b> (<c>02 §10</c>'s shape, <c>RecomputeDemand</c>'s precedent),
    /// returning on line one where a Ruleset states no <c>[needs]</c>. <b><c>plans/0013</c> carries
    /// a row for it as of 2026-08-31</b> (<c>adr/0073</c>), and the row says <em>unmeasured</em> —
    /// which is the ledger doing its job rather than a figure arriving.
    /// </para>
    /// </remarks>
    /// <param name="tick">The Tick now being stepped.</param>
    public void SweepNeeds(Ticks tick)
    {
        if (!_world.Rules.Needs.Runs || tick.Raw % Ticks.PerDay != 0)
        {
            return;
        }

        for (int instance = 0; instance < _world.RuleInstances.Rows.SlotCount; instance++)
        {
            if (!_world.RuleInstances.Rows.IsLive(instance)
                || !_world.RuleInstances.IsStarving(instance))
            {
                continue;
            }

            RefreshNeed(
                _world, instance, tick.Raw - _world.RuleInstances.StarvedSince[instance].Raw);
        }
    }

    /// <summary>Deepens one Household's Need to the depth its shortage has now lasted.</summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>The degrade half, and it is a DURATION rather than a tally</b> (<c>adr/0053</c>, one
    /// subject along from the Building). ⚠ <b>It RECOMPUTES from <c>elapsed</c> rather than
    /// accumulating per visit, which is what makes a staggered pass sound</b>: the depth is a function
    /// of the duration, so a Household first visited ten Days in arrives at the right depth in one
    /// step and sampling costs staleness rather than accuracy.
    /// </para>
    /// <para>
    /// <b>Deeper only.</b> An earlier episode's residual is never lifted here — recovery is
    /// <see cref="MoveNeed"/>'s alone — so a Household back at −2 that starves again keeps the −2
    /// until this episode passes it.
    /// </para>
    /// </remarks>
    /// <param name="world">The world the Household lives in.</param>
    /// <param name="instance">A starving Rule Instance.</param>
    /// <param name="elapsed">Ticks it has been short for.</param>
    public static void RefreshNeed(World world, int instance, ulong elapsed)
    {
        NeedRuleset needs = world.Rules.Needs;

        if (!needs.Runs)
        {
            return;
        }

        Handle<Household> household = world.RuleInstances.Household[instance];

        if (household.IsNone)
        {
            return;
        }

        int slot = world.Households.Rows.Resolve(household);

        // The Rule's INPUT terms rather than the Bin it stopped on: a term carries its Resource, so
        // no bin slot has to be resolved, and a Rule drawing two Needs' Goods deepens both.
        foreach (Term term in world.Rules.Inputs(world.RuleInstances.Rule[instance]))
        {
            Need need = world.Rules.NeedOf(term.Bin.Resource);

            if (need == Need.None)
            {
                continue;
            }

            Column<int>? column = world.Households.NeedColumn(need);

            if (column is null)
            {
                continue;
            }

            // The Ruleset states steps per DAY (adr/0059's shape: the designer authors a duration),
            // so the cadence this is called on may be retuned without moving the felt quantity.
            long depth = IntegerMath.FloorDiv((long)needs.DegradeOf(need) * (long)elapsed, Ticks.PerDay);
            long target = depth > int.MaxValue ? int.MinValue : -depth;

            if (target < column[slot])
            {
                Write(column, slot, (int)target, needs.Floor);
            }
        }
    }

    /// <summary>Stores a Need, clamped at the ideal above and the Ruleset's floor below.</summary>
    /// <remarks>
    /// ⚠ <b>Written out rather than <c>Math.Clamp</c></b>, which <c>05 §4</c> bans. The floor is
    /// <c>adr/0006</c> as <c>adr/0003</c> extends it: an unbounded magnitude trends downward for ever.
    /// </remarks>
    internal static void Write(Column<int> column, int slot, int value, int floor)
    {
        if (value > 0)
        {
            value = 0;
        }
        else if (value < floor)
        {
            value = floor;
        }

        column[slot] = value;
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

        // Short of labour is a closed shift rather than a shortage: a five-day trade is unstaffed for
        // about 62 hours every weekend, longer than any decline threshold.
        if (verdict.Blocking != Blocking.Supply || IsLabour(verdict.Bin))
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

        ReleaseClaims(verdict, tick);
    }

    /// <summary>
    /// Drains every other Bin the stopped Rule names, in the direction the Rule would have moved it.
    /// </summary>
    /// <remarks>
    /// <see cref="World.Drain(int, Blocking, Ticks)"/> spends its budget on each waiter it wakes, so a
    /// woken Rule holds a claim on the Bin that woke it until it runs. A Rule that stops draws nothing,
    /// and the waiters its claim held back are owed a wake that no write to that Bin will bring.
    /// </remarks>
    private void ReleaseClaims(RuleVerdict verdict, Ticks tick)
    {
        int instance = verdict.Instance;
        RuleId rule = _world.RuleInstances.Rule[instance];
        ReadOnlySpan<Term> inputs = _world.Rules.Inputs(rule);
        ReadOnlySpan<Term> outputs = _world.Rules.Outputs(rule);
        int terms = inputs.Length + outputs.Length;

        for (int position = 0; position < terms; position++)
        {
            int bin = BinAt(_world, instance, rule, inputs, outputs, position);
            Blocking blocking = position < inputs.Length ? Blocking.Supply : Blocking.Space;

            if (bin == Rows.NoSlot || (bin == verdict.Bin && blocking == verdict.Blocking))
            {
                continue;
            }

            bool drained = false;

            for (int earlier = 0; earlier < position; earlier++)
            {
                if (BinAt(_world, instance, rule, inputs, outputs, earlier) == bin
                    && (earlier < inputs.Length ? Blocking.Supply : Blocking.Space) == blocking)
                {
                    drained = true;
                    break;
                }
            }

            if (!drained)
            {
                _world.Drain(bin, blocking, tick);
            }
        }
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
            if (term.Bin.Scope == Scope.Pool)
            {
                net += PoolDraw(world, instance, term, rule, binSlot);
                continue;
            }

            if (Bin(world, instance, term.Bin, rule) == binSlot)
            {
                net -= term.Amount;
            }
        }

        foreach (Term term in world.Rules.Outputs(rule))
        {
            // A service output is not a deposit into anything: the units go into the office's daily
            // allowance, which is not a Bin, so no Bin can be short of space for it. A delivered
            // Utility's output is the same shape one family over -- it feeds a network, and the Pool
            // row's Bin stays empty.
            if (world.Rules.Family(term.Bin.Resource) == ResourceFamily.Service
                || world.Rules.IsDeliveredUtility(term.Bin.Resource))
            {
                continue;
            }

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
    /// What one application of a <see cref="Scope.Pool"/> term draws from <paramref name="binSlot"/>,
    /// negative for a draw and zero when this Bin is not on either side of the purchase.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>This exists because a purchase's money leg has NO TERM, and a term walk therefore priced
    /// it at zero.</b> <c>adr/0050</c>: *"there is no Ruleset syntax for the payment"*, the price is
    /// emergent and the counterparty is implied by the scope. So a buyer stopped for want of funds
    /// subscribed to its own balance, <c>RuleEngine.Stop</c> drained that Bin immediately as it always
    /// does, <see cref="Requirement"/> answered **0**, and <c>World.Drain</c> woke it on the spot.
    /// ***A wait that is undone by the drain that follows it is indistinguishable from no wait at
    /// all***, so the buyer spun on its rate for ever, never appeared on any wait list, and reported
    /// itself as armed.
    /// </para>
    /// <para>
    /// ⚠ <b><c>adr/0137</c> predicted this failure and described it one layer up</b> — *"the cheapest
    /// implementation returns insufficient funds and subscribes to nothing"*. Milestone 26 task 4 did
    /// subscribe, which looked like the record's requirement being met; **the subscription was
    /// cancelled a line later by shared code the purchase does not own**. ***Found by building
    /// <c>adr/0137</c>'s own field and watching it read <c>—</c> on a world where every purse was
    /// empty***, which is what an instrument is for (<c>adr/0093</c>: the code said one thing and the
    /// run said another).
    /// </para>
    /// <para>
    /// <b>Derived rather than stored, so <c>adr/0063</c> is kept rather than excepted.</b> The payment
    /// is recomputable at drain time because <b>the price is a property of the market row and not of
    /// the seller</b> — <c>adr/0167</c>, and it is the same fact that made *buy from the cheapest*
    /// unavailable. ⚠ <b>So no seller has to be re-drawn here</b>, and this method deliberately does
    /// not: a draw at drain time would be a second lottery with a different <c>purpose_tag</c>'s worth
    /// of consequences, and it would answer a question nobody asked.
    /// </para>
    /// <para>
    /// ⚠ <b>The money guard is first because this runs inside <c>World.Drain</c>.</b> Every deposit and
    /// every withdrawal in the city reaches here once per waiter, so the ordinary case — a Good Bin,
    /// which is every Bin in nine of the shipped files — must cost one column read and a comparison.
    /// </para>
    /// </remarks>
    private static long PoolDraw(World world, int instance, in Term term, RuleId rule, int binSlot)
    {
        // A delivered Utility draws no stock Bin. Check serves it short instead of
        // stopping on it, so there is no Bin a waiter could sleep on and no requirement to price.
        if (world.Rules.IsDeliveredUtility(term.Bin.Resource))
        {
            return 0;
        }

        // The Good leg. Bin() resolves a pool term to the MARKET ROW's Bin for every caller but
        // Check, which is adr/0139's wake target and what a blocked buyer actually sleeps on.
        if (Bin(world, instance, term.Bin, rule) == binSlot)
        {
            return -term.Amount;
        }

        if (!world.Rules.IsConserved(world.Bins.Resource[binSlot]))
        {
            return 0;
        }

        if (!world.TryMoneyResource(out ResourceId money)
            || world.FindLocalBin(instance, money) != binSlot)
        {
            // A money Bin, but not this buyer's. The seller's till is the ordinary case, and it is
            // an output of the purchase rather than a draw on it.
            return 0;
        }

        int row = MarketRow(world, instance, term.Bin.Resource);

        if (row == Rows.NoSlot)
        {
            // Premises in no District yet. Check answers this with a fire at zero applications, so
            // there is nothing waiting here and nothing to price.
            return 0;
        }

        // A buyer no city seller can fill pays the Outside's price, which is never below the row's.
        if (world.Markets.Largest(world, row) < term.Amount
            && world.TryImportPrice(term.Bin.Resource, out Money imported))
        {
            return -(term.Amount * imported.Raw);
        }

        return -(term.Amount * world.DistrictPools.Price[row].Raw);
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
    {
        BinRef addressed = position < inputs.Length
            ? inputs[position].Bin
            : outputs[position - inputs.Length].Bin;

        // A service a Rule MAKES names no Bin at all -- the units go into the office's allowance for
        // the Day -- so there is nothing here to drain or to claim against. A service a Rule BUYS is
        // a pool term and still resolves to the market row, which is where its buyer sleeps.
        //
        // A delivered Utility names no Bin on EITHER side, and that is stronger than the service
        // case: it neither blocks nor stores, so a claim against the Pool row's empty Bin would be
        // a claim nothing can ever satisfy and a drain there would wake nobody.
        if (world.Rules.IsDeliveredUtility(addressed.Resource))
        {
            return Rows.NoSlot;
        }

        return addressed.Scope == Scope.Local
            && world.Rules.Family(addressed.Resource) == ResourceFamily.Service
                ? Rows.NoSlot
                : Bin(world, instance, addressed, rule);
    }

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
    /// <b>A claim belongs to every row due on <c>world.Tick</c> or later.</b> The check runs between
    /// Ticks, after <c>World.Advance</c>, so a waiter woken on the last Tick is due on
    /// <c>world.Tick</c> and still on the wheel. <see cref="EventWheel.IsArmed"/> reads that row as in
    /// flight, which is true only between Phase 1 and Phase 3, so it misses the very claim the drain
    /// just made. A row in flight also still draws this Tick, so it claims the same way.
    /// </para>
    /// </remarks>
    internal static void AccumulateClaims(World world, Blocking blocking, Span<long> claims)
    {
        ArgumentNullException.ThrowIfNull(world);

        RuleInstanceTable instances = world.RuleInstances;

        for (int instance = 0; instance < instances.Rows.SlotCount; instance++)
        {
            if (!instances.Rows.IsLive(instance)
                || instances.Blocked[instance] != Blocking.Nothing
                || instances.NextTick[instance] < world.Tick)
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

    /// <summary>
    /// Writes a Rule's Map Layer emission into the Cell under its Building, and against the Building.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Two destinations for one figure, and they answer different questions.</b> The Cell is the
    /// city-wide field a Household smells; <see cref="BuildingTable.DayEmitted"/> is what this
    /// Building put out, which the Cell stops being able to say the moment a second Lot emits into
    /// it or the operator decays it.
    /// </para>
    /// <b>Pollution only, and the other two Layers are named holes rather than omissions.</b> Land
    /// value is not emitted at all — <c>SetLandValueTarget</c> sets a target the momentum operator
    /// chases, which is a different verb from <em>add this much</em> — and Sealing is a footprint
    /// property of a Building rather than a per-application output of a Rule. Accepting either as an
    /// emission would mean inventing a semantic for it here, which is a design change wearing a
    /// switch statement.
    /// </remarks>
    private void Emit(int building, in MapEmission emission, long applications, Ticks tick)
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

        // 🔴 THE SAME FIGURE, KEPT WHERE IT CAN STILL BE ATTRIBUTED. The line above adds into a Cell
        // that many Lots share and that the diffusion operator decays every cadence, so from the
        // next Layer Tick onward nothing in the world can say which Building put what there. An
        // addition beside EmitPollution and never a replacement for it: the Map Layer is the
        // city-wide field and this is one Building's own output, and they are different quantities
        // that happen to agree at the instant of the firing.
        //
        // Written from Fire and not from Check, which is the same discipline the trade posting and
        // DistrictPools.Consumed above are under (adr/0037: phase 2 writes nothing). Check runs for
        // every due Rule, for every rung of a failed chain and again on the phase 3 re-check, and a
        // Rule that was evaluated and then blocked has emitted nothing -- recording there would
        // count evaluations rather than emissions.
        _world.Buildings.Emit(building, BusinessAccounts.DayOf(tick), emitted);
    }

    /// <summary>
    /// The whole units an input term takes on this firing, which is its amount unless the amount is
    /// stated per Day.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A Rule firing every <c>rate</c> Ticks covers <c>rate</c> Ticks of the Day, so a daily
    /// quantity earns <c>amount × rate</c> Tick-units per firing and <see cref="Ticks.PerDay"/> of
    /// them make a unit.</b> The part below a whole unit stays on the Bin, so three a Day over eight
    /// firings takes three rather than zero or eight. It is
    /// <c>CitizenTable.WageRemainder</c>'s arithmetic with the firing in the Tick's place.
    /// </para>
    /// <para>
    /// ⚠ <b>Nothing is written here.</b> Phase 2 writes nothing (<c>adr/0037</c>), the re-check in
    /// Phase 3 runs this again against the same Bin, and a Rule that never reaches
    /// <see cref="Fire"/> must leave the Bin's progress where it was. The remainder is held until
    /// the firing is settled.
    /// </para>
    /// <para>
    /// <b>A second per-Day term on one Bin continues from the first's remainder rather than from the
    /// column</b>, so two terms naming one Bin take the same total as one term for their sum.
    /// </para>
    /// <para>
    /// ⚠ <b>A fixed apply count of one is required, and refused rather than assumed.</b> Check
    /// multiplies every delta by the applications it settled on, which would take a multiple of a
    /// daily quantity while advancing the remainder once.
    /// </para>
    /// </remarks>
    private long PerFiring(int bin, in Term term, RuleId rule, in RuleDefinition definition)
    {
        if (!term.PerDay)
        {
            return term.Amount;
        }

        if (!definition.Apply.Derived.IsNone || definition.Apply.Min != 1 || definition.Apply.Max != 1)
        {
            throw new InvalidOperationException(
                $"rule {rule.Raw} states a per-Day quantity for Resource {term.Bin.Resource.Raw} "
                + "under an apply count that is not fixed at one. A daily quantity is spent once per "
                + "firing, so a Rule free to apply twice would take twice the Day's amount. The "
                + "refusal belongs at load, with a file and a line; it is here because the loader has "
                + "no such check yet.");
        }

        int at = -1;

        for (int i = 0; i < _accruedCount; i++)
        {
            if (_accruedBin[i] == bin)
            {
                at = i;
                break;
            }
        }

        long scaled = (at >= 0 ? _accruedRemainder[at] : _world.Bins.Progress[bin])
            + ((long)term.Amount * definition.Rate);
        int remainder = (int)(scaled % Ticks.PerDay);

        if (at >= 0)
        {
            _accruedRemainder[at] = remainder;
        }
        else
        {
            Grow(ref _accruedBin, _accruedCount + 1);
            Grow(ref _accruedRemainder, _accruedCount + 1);

            _accruedBin[_accruedCount] = bin;
            _accruedRemainder[_accruedCount] = remainder;
            _accruedCount++;
        }

        return IntegerMath.FloorDiv(scaled, Ticks.PerDay);
    }

    /// <summary>Accumulates a delta against a Bin, merging a Bin already named by this Rule.</summary>
    /// <remarks>
    /// A linear scan because a Rule's term list is a handful — <c>02 §4.3</c>'s bakery has two — so
    /// anything cleverer would cost more to set up than the scan costs to run.
    /// </remarks>
    private void Touch(int bin, long delta, bool treasury = false) =>
        Touch(bin, delta, bin, treasury);

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
    /// <para>
    /// <b><paramref name="treasury"/> is OR-ed across a merge where the blame is not</b>, and the
    /// asymmetry is not a nicety: blame is a choice between two Bins a reader could be sent to, so
    /// the first writer wins; treasury-ness is a property of the Bin itself, so two terms merging
    /// onto one Bin cannot disagree about it and an <c>|=</c> is the only spelling that stays right
    /// if a caller ever passes <see langword="false"/> first.
    /// </para>
    /// </remarks>
    private void Touch(int bin, long delta, int blame, bool treasury = false)
    {
        for (int i = 0; i < _touchedCount; i++)
        {
            if (_touchedBin[i] == bin)
            {
                _touchedDelta[i] += delta;
                _touchedTreasury[i] |= treasury;
                return;
            }
        }

        Grow(ref _touchedBin, _touchedCount + 1);
        Grow(ref _touchedDelta, _touchedCount + 1);
        Grow(ref _touchedBlame, _touchedCount + 1);
        Grow(ref _touchedTreasury, _touchedCount + 1);

        _touchedBin[_touchedCount] = bin;
        _touchedDelta[_touchedCount] = delta;
        _touchedBlame[_touchedCount] = blame;
        _touchedTreasury[_touchedCount] = treasury;
        _touchedCount++;
    }

    /// <summary>
    /// Whether a term names the treasury's Bin for a conserved Resource — the one movement in a Bin
    /// Rule that a city's budget has to be able to account for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Off the term's <see cref="Scope"/> and not off the Bin's owner</b>, which is
    /// <c>PolicyEngine.Move</c>'s decision restated: <em>a Bin is the treasury's by ownership and a
    /// transfer is the treasury's by direction.</em> The two agree today — <c>Bin</c>'s
    /// <see cref="Scope.Global"/> case resolves to <c>World.FindTreasuryBin</c> and nothing else does
    /// — and reading the Scope is what keeps them agreeing for a stated reason rather than by
    /// coincidence.
    /// </para>
    /// <para>
    /// <b>The conserved test is the second half and it is not redundant.</b>
    /// <c>World.FitTreasury</c> opens a Bin per <em>money-family</em> Resource and never removes one,
    /// so a hot reload that demotes a Resource out of the money family (<c>adr/0015</c>) leaves a
    /// treasury Bin standing for something a budget must not count as income. ⚠ It is behind the
    /// Scope test rather than beside it, so a Rule naming no global term never reaches the Ruleset
    /// at all: what a Rule that does not touch the treasury pays for this counter is one enum
    /// comparison per term.
    /// </para>
    /// </remarks>
    private bool IsTreasuryMoney(BinRef reference) =>
        reference.Scope == Scope.Global && _world.Rules.IsConserved(reference.Resource);

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
