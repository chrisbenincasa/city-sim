namespace Borough.Core.Rules;

using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Tables;

/// <summary>
/// What the Zone Rules did since the last reading: how often they triggered, and what their samples
/// landed on.
/// </summary>
/// <remarks>
/// <para>
/// <b>Flows, not levels</b> — the same distinction slice 7 task 9 drew for <see cref="RuleActivity"/>.
/// A trigger has no value at an instant, so each is accumulated between readings and a reading drains
/// it, which is where <c>adr/0006</c>'s sink is.
/// </para>
/// <para>
/// <b><see cref="Vacant"/> and <see cref="Occupied"/> are the two things a sample can find, and they
/// are separated because they are two different mechanisms.</b> A vacant Lot is a candidate for
/// creation; an occupied one is a Building whose failure pressure is read (<c>02 §5.9</c>:
/// <em>"sampling reads that duration; it never produces it"</em>). Their sum is the quantity
/// <c>02 §5.7</c> claims is independent of Zone size, and the tripwire holds fixed.
/// </para>
/// </remarks>
/// <param name="Triggers">Zone Rule firings — one per Rule per Tick its interval divides.</param>
/// <param name="Vacant">Sampled Lots with no Building on them.</param>
/// <param name="Occupied">Sampled Lots with one.</param>
/// <param name="Created">Buildings built — the subset of <paramref name="Vacant"/> that qualified.</param>
/// <param name="Demolished">
/// Buildings condemned — the subset of <paramref name="Occupied"/> whose failure pressure had crossed
/// their kind's threshold.
/// </param>
public readonly record struct ZoneActivity(
    RuleFlow Triggers, RuleFlow Vacant, RuleFlow Occupied, RuleFlow Created, RuleFlow Demolished)
{
    /// <summary>Lots evaluated over the interval, which is what a trigger is charged for.</summary>
    /// <remarks>
    /// <b>A sum, and there is deliberately no peak equivalent.</b> The busiest Tick of one flow and
    /// the busiest Tick of the other need not be the same Tick, so adding the two peaks would report
    /// a burst that never happened. Only sums compose.
    /// </remarks>
    public long Evaluated => Vacant.Sum + Occupied.Sum;
}

/// <summary>
/// The Sweep family's first member: Zone Rules trigger on an interval, sample Lots, and act where
/// they run.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is <c>adr/0033</c>'s observable difference rather than a second copy of
/// <see cref="RuleEngine"/>.</b> A Bin Rule is scheduled — the Event Wheel hands it over in Tick
/// phase 1, it proposes in phase 2, and phase 3 settles contested proposals by a counter-based
/// shuffle, because two Rules may reach for one Bin. A Zone Rule is swept: it has no wheel entry, no
/// subscription and no proposal, and its effect is visible the moment it runs. The two families
/// therefore differ in <em>when their effect becomes visible within a single Tick</em>, which is
/// exactly the class of difference the ADR says makes moving a mechanism between them a change to the
/// city.
/// </para>
/// <para>
/// <b>The trigger is <c>tick % interval</c> and there is no stagger, which is a departure from
/// <c>02 §4.2</c> and a deliberate one.</b> That section's three cheapness mechanisms — low
/// frequency, stagger, Chunk partition — are argued for a <em>Policy</em>, which sweeps an entire
/// population and whose per-trigger cost therefore grows with the city. A Zone Rule <em>samples</em>,
/// so its per-trigger cost is the sample size in the Ruleset and nothing else. Staggering would be
/// armour against a cost this slice's own tripwire exists to prove does not exist, and it would put a
/// second hash-bearing coordinate into the trigger for it.
/// </para>
/// <para>
/// <b>Contention over one Lot is resolved by declaration order</b>, because <c>02 §5.5</c>'s
/// bid-price contest needs prices this build has none of. <c>02 §4.2</c>'s <em>rotate the scan
/// start</em> is not the mitigation here and could not be: a sampler has no scan to start. Nor is the
/// bias the one that mitigation addresses — a Policy rotates because a treasury <em>runs dry</em>, so
/// a fixed order permanently excludes the tail of the population. Nothing a Zone Rule contends for is
/// exhausted: the Rule that loses a Lot this trigger samples elsewhere on the next one, and two Rules
/// overlap at all only about <c>sample² ÷ Lots</c> of the time.
/// </para>
/// <para>
/// <b>The engine holds no simulation state</b>, for <see cref="RuleEngine"/>'s reason. Its one buffer
/// is scratch, grown to the largest sample any Zone Rule declares and never again, so it is bounded
/// by the Ruleset rather than by elapsed time.
/// </para>
/// </remarks>
public sealed class ZoneRuleEngine
{
    private readonly World _world;
    private readonly WorldKey _key;

    /// <summary>Where <see cref="ZoneSample.Draw"/> writes. Grown to the widest sample, then reused.</summary>
    private int[] _sample = [];

    private int _tickTriggers;
    private int _tickVacant;
    private int _tickOccupied;
    private int _tickCreated;
    private int _tickDemolished;

    private RuleFlow _triggerFlow;
    private RuleFlow _vacantFlow;
    private RuleFlow _occupiedFlow;
    private RuleFlow _createdFlow;
    private RuleFlow _demolishedFlow;

    /// <param name="world">The tables this sweeps, and the Ruleset it sweeps under. Not copied.</param>
    /// <param name="key">The world seed, as the sample's first coordinate.</param>
    public ZoneRuleEngine(World world, WorldKey key)
    {
        ArgumentNullException.ThrowIfNull(world);

        _world = world;
        _key = key;
    }

    /// <summary>
    /// Reads what the Zone Rules did since the last call, and resets the counters.
    /// </summary>
    /// <remarks>
    /// Read-and-reset for <see cref="RuleEngine.Drain"/>'s reason: a series exists to answer
    /// <em>is this trending upward</em>, and a cumulative total answers yes for the life of every run.
    /// </remarks>
    public ZoneActivity Drain()
    {
        var activity = new ZoneActivity(
            _triggerFlow, _vacantFlow, _occupiedFlow, _createdFlow, _demolishedFlow);

        _triggerFlow = default;
        _vacantFlow = default;
        _occupiedFlow = default;
        _createdFlow = default;
        _demolishedFlow = default;

        return activity;
    }

    /// <summary>
    /// Tick phase 6: fires every Zone Rule this Tick's interval divides, and samples for each.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Declaration order, and it is load-bearing twice.</b> It decides which of two Rules
    /// contending for one Lot acts first, and the index is a coordinate of the draw — so reordering
    /// <c>[[zone_rule]]</c> tables in a file changes which Lots are sampled and moves the State Hash.
    /// That is the Ruleset being content rather than configuration, and it is the same property
    /// <c>[[rule]]</c> order already has.
    /// </para>
    /// <para>
    /// <b>The sweep closes its own Tick</b>, unlike <see cref="RuleEngine.CloseTick"/>, which is
    /// public because a Bin Rule's Tick is three phases and an instrument may run one of them alone.
    /// A Zone Rule's Tick is this method, so there is nothing a caller could close that this has not.
    /// </para>
    /// </remarks>
    /// <param name="tick">The Tick being run, which is both the trigger test and the sample's key.</param>
    public void Sweep(Ticks tick)
    {
        ReadOnlySpan<ZoneRuleDefinition> rules = _world.Rules.ZoneRules;

        for (int rule = 0; rule < rules.Length; rule++)
        {
            ZoneRuleDefinition definition = rules[rule];

            // adr/0048's two-sided check, in the direction the loader cannot cover: a Ruleset built
            // in code has never been through RulesetLoader, and an interval of zero would divide by
            // it rather than say so.
            if (definition.Interval == 0)
            {
                throw new InvalidOperationException(
                    $"Zone Rule {rule} has an interval of 0. An interval is a reschedule in Ticks and "
                    + "the loader refuses anything below 1; this Ruleset was not built by it.");
            }

            if (tick.Raw % definition.Interval != 0)
            {
                continue;
            }

            _tickTriggers++;

            // adr/0059: the sample is derived from the revisit period and the city, per trigger, so
            // it moves as Lots are painted. SlotCount rather than LiveCount for the reason
            // ZoneSample.Draw already draws against it -- the draw is over slots and discards the
            // ones that are not live, so a denominator of live rows would systematically over-sample.
            Span<int> into = Scratch(definition.SampleFor(_world.Lots.Rows.SlotCount));
            int drawn = ZoneSample.Draw(_world.Lots, into, _key, tick, rule);

            for (int i = 0; i < drawn; i++)
            {
                // The fork the rest of the slice hangs off. A vacant Lot is a candidate for creation;
                // an occupied one is a Building whose failure pressure is read, which is task 7's.
                if (_world.Lots.IsVacant(into[i]))
                {
                    _tickVacant++;
                    Create(definition, into[i], tick);
                }
                else
                {
                    _tickOccupied++;
                    Condemn(into[i], tick);
                }
            }
        }

        CloseTick();
    }

    /// <summary>
    /// The create predicate: <b>vacant AND permitted AND somebody in the Pool would take it</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Three terms, and the third is a documented vacancy reason rather than a stand-in for the
    /// pro-forma.</b> <c>02 §5.6</c>'s developer test needs prices, capital and a bid contest, none of
    /// which exist. But <c>CONTEXT</c> → Frontage lists the four answers to <em>why is this Lot
    /// vacant</em>, and <em>"no Household in the Unplaced Pool that would accept it"</em> is one of
    /// them — <b>beside</b> <em>no capital</em>, not downstream of it. So consulting the Pool is the
    /// design's own reason, and what is missing is missing rather than approximated.
    /// </para>
    /// <para>
    /// <b>The permission bit is a term here and nowhere else</b> (<c>adr/0055</c>). Filtering the
    /// sample by it instead would let a player repaint a Lot and put the Building on it beyond every
    /// Rule's reach, which is immortality by paintbrush.
    /// </para>
    /// <para>
    /// <b>The Pool is read as <em>non-empty</em> and drained blind</b> (<c>adr/0054</c>). There is no
    /// acceptance test, because acceptance needs rent, a commute and a tolerance; a Household that
    /// would refuse this dwelling is a thing this build cannot express, and pretending otherwise would
    /// put a number in a file that nothing had measured.
    /// </para>
    /// <para>
    /// <b>Construction time is deliberately absent</b> (<c>02 §5.7</c>'s second pacing mechanism). A
    /// Building under construction occupies its Lot and produces nothing, which needs a state a
    /// Building does not have. Growth is therefore instantaneous, and the only thing pacing it is the
    /// trigger interval and the sample.
    /// </para>
    /// </remarks>
    private void Create(ZoneRuleDefinition definition, int lot, Ticks tick)
    {
        if ((_world.Lots.Zone[lot] & definition.Admits) == 0 || _world.UnplacedPool.Count == 0)
        {
            return;
        }

        Handle<Building> building = _world.CreateBuilding(
            _world.Lots.Rows.At(lot), definition.Kind, tick, _key);

        // Keyed on the Lot's monotonic id rather than its slot. 02 §8 rule 5's own footnote: a slot
        // is recycled when a row is freed, so drawing against one would make demolishing an unrelated
        // Lot change who moves in downtown.
        ulong value = Randomness.Draw(
            _key, _world.Lots.Rows.IdAt(lot), tick, PurposeTag.PoolDraw);

        int position = (int)(value % (ulong)(uint)_world.UnplacedPool.Count);

        _world.Place(_world.UnplacedPool.At(position), building);

        _tickCreated++;
    }

    /// <summary>
    /// The condemn predicate: <b>a Building that has been starved for longer than its kind allows</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The permission bit is not a term here, and that is <c>adr/0055</c> stated the other way
    /// round.</b> A Zone Rule's permission set scopes what it <em>builds</em>, never which Lots it
    /// looks at — so repainting a Lot cannot put the Building on it beyond every Rule's reach, which
    /// would be immortality by paintbrush. A Zone Rule that could not have built this Building may
    /// still notice that it has fallen down.
    /// </para>
    /// <para>
    /// <b>Sampling reads the duration; it never produces it</b> (<c>02 §5.9</c>). The pressure was
    /// already true before the sample arrived, so what a sample costs a condemned Building is a lag in
    /// being noticed and not a random lifetime. That distinction is what makes sampled decline
    /// legitimate at all, since <c>CONTEXT</c> → Zone Rule justifies sampling by <em>developers do not
    /// evaluate every Lot</em> — an argument about an actor choosing among alternatives — and
    /// abandonment has no actor.
    /// </para>
    /// <para>
    /// <b>The Building's pressure is the longest of its Rules', measured in missed firings</b>
    /// (<c>adr/0053</c>, as amended). Two Rules that went short at different moments are two
    /// durations, and a <c>rate</c> belongs to a Rule rather than to a kind — so the comparison is
    /// made per Rule Instance and the maximum is never stored anywhere. The walk is over a Building's
    /// own Rule list, which is its kind's declared set and a handful long.
    /// </para>
    /// <para>
    /// <b>The threshold multiplies rather than the duration dividing.</b> <c>elapsed ≥ condemn × rate</c>
    /// and not <c>elapsed ÷ rate ≥ condemn</c>, because the second is a division this project would
    /// have to spell through <see cref="Arithmetic.IntegerMath"/> to say the same thing, once per
    /// sampled Rule, for an answer it then throws away.
    /// </para>
    /// <para>
    /// <b>The condition behind the demolition is available and is not kept.</b>
    /// <see cref="RuleInstanceTable.Reported"/> holds it wherever an author has written an
    /// <c>on_fail</c> chain, which is <c>02 §5.9</c>'s refusal of the sad-face icon — but nothing
    /// consumes it: <c>01 §5</c>'s notification surface does not exist, and the row it would be copied
    /// off is freed on the next line. Storing it now would be a column nothing reads, which is the
    /// shape <c>0011</c> finding 39 already has one of.
    /// </para>
    /// </remarks>
    private void Condemn(int lot, Ticks tick)
    {
        // Through BuildingOn rather than off the column, because BuildingSlot is plus-one encoded so
        // that a zero-filled row reads as vacant rather than as holding the first Building in the
        // city. Read raw it is off by one, which condemns the Building on the next slot — and that
        // failure is invisible from outside: Buildings decline, Lots clear, the counts are plausible,
        // and the only wrong thing is which house fell down.
        int building = _world.Lots.BuildingOn(lot);
        byte kind = _world.Buildings.Kind[building];

        // A kind the Ruleset does not declare, which World.CreateBuilding permits by name: such a
        // Building is given no Bins and no Rules, so it has nothing that could starve and no threshold
        // to be measured against. 02 §4.3 calls this state derelict, and there is no flag for it yet —
        // asking the Ruleset about the kind would throw rather than answer.
        if (!_world.Rules.Declares(kind))
        {
            return;
        }

        int threshold = _world.Rules.Kind(kind).CondemnAfter;

        if (threshold == 0)
        {
            return;
        }

        bool condemned = false;

        foreach (int instance in _world.BuildingRules.Walk(building))
        {
            if (!_world.RuleInstances.IsStarving(instance))
            {
                continue;
            }

            ulong elapsed = tick.Raw - _world.RuleInstances.StarvedSince[instance].Raw;
            ulong allowed = (ulong)threshold * _world.Rules.Rule(
                _world.RuleInstances.Rule[instance]).Rate;

            if (elapsed >= allowed)
            {
                condemned = true;
                break;
            }
        }

        // Outside the walk, because demolition empties the list being walked. Breaking first and
        // acting after costs a bool and removes the question of whether an enumerator survives having
        // its collection dismantled underneath it.
        if (condemned)
        {
            _world.DestroyBuilding(_world.Buildings.Rows.At(building), tick);
            _tickDemolished++;
        }
    }

    /// <summary>A span of at least <paramref name="size"/>, growing the buffer once if it must.</summary>
    /// <remarks>
    /// <para>
    /// <b>The only allocation this class can make.</b> Sizing from the Ruleset instead would need the
    /// widest sample computed at construction and recomputed on every hot reload; growing on demand is
    /// the same bound reached lazily, and it survives a Ruleset swap without being told one happened.
    /// </para>
    /// <para>
    /// <b>It is bounded by the Lot count, hence by the map — and it did not used to be</b> (task 11d
    /// of <c>plans/0014</c>). The remark here said <em>bounded by the Ruleset rather than by elapsed
    /// time</em>, which was true when a sample was an absolute number a file stated.
    /// <c>adr/0059</c> derives the sample from the city, so the Ruleset no longer bounds anything: the
    /// buffer grows to <c>lots × interval ÷ revisit_ticks</c>, which is at most one <c>int</c> per Lot
    /// and reaches that only where the revisit period equals the interval.
    /// </para>
    /// <para>
    /// <b>Still <c>adr/0006</c>-safe, and for a different reason than before.</b> A map does not grow
    /// with elapsed time, so neither does this — but the bound is now a property of the world rather
    /// than of the file, and the day a map extends procedurally is the day this needs a ceiling. A
    /// remark that is true by accident is how the next person gets it wrong, which is why this says so
    /// rather than being left to be re-derived.
    /// </para>
    /// </remarks>
    private Span<int> Scratch(int size)
    {
        if (_sample.Length < size)
        {
            _sample = new int[size];
        }

        return _sample.AsSpan(0, size);
    }

    /// <summary>Rolls this Tick's counts into the interval a reading will drain.</summary>
    private void CloseTick()
    {
        _triggerFlow = _triggerFlow.Fold(_tickTriggers);
        _vacantFlow = _vacantFlow.Fold(_tickVacant);
        _occupiedFlow = _occupiedFlow.Fold(_tickOccupied);
        _createdFlow = _createdFlow.Fold(_tickCreated);
        _demolishedFlow = _demolishedFlow.Fold(_tickDemolished);

        _tickTriggers = 0;
        _tickVacant = 0;
        _tickOccupied = 0;
        _tickCreated = 0;
        _tickDemolished = 0;
    }
}
