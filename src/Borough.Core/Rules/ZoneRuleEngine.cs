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
/// Buildings condemned — the subset of <paramref name="Occupied"/> whose <b>premises'</b> failure
/// pressure had crossed their kind's threshold.
/// </param>
/// <param name="Ended">
/// <b>Tenancies ended</b> — Households evicted because <em>their own</em> Rules crossed the same
/// threshold, leaving the premises standing (<c>adr/0141</c>). ⚠ <b>It is not a subset of
/// <paramref name="Demolished"/> and never overlaps it</b>: a demolition ends every tenancy in the
/// Building through <c>World.DestroyBuilding</c> and is counted once, as one Building. ***The two
/// counters are what makes the split visible at all***, since before this the second outcome did not
/// exist and a starving tenant was reported as a demolished Building.
/// </param>
public readonly record struct ZoneActivity(
    RuleFlow Triggers,
    RuleFlow Vacant,
    RuleFlow Occupied,
    RuleFlow Created,
    RuleFlow Demolished,
    RuleFlow Ended)
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
    private int _tickEnded;

    private RuleFlow _triggerFlow;
    private RuleFlow _vacantFlow;
    private RuleFlow _occupiedFlow;
    private RuleFlow _createdFlow;
    private RuleFlow _demolishedFlow;
    private RuleFlow _endedFlow;

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
            _triggerFlow, _vacantFlow, _occupiedFlow, _createdFlow, _demolishedFlow, _endedFlow);

        _triggerFlow = default;
        _vacantFlow = default;
        _occupiedFlow = default;
        _createdFlow = default;
        _demolishedFlow = default;
        _endedFlow = default;

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

        _world.CreateBuilding(_world.Lots.Rows.At(lot), definition.Kind, tick, _key);

        // adr/0069: construction houses NOBODY. This used to draw a Pool member and place them here,
        // which was placement's job done one Household deep by the only mechanism that existed --
        // World.Place had exactly one caller and it was this line. The new Building stands empty and
        // PlacementEngine fills it over the following Days, which is what makes its declared capacity
        // reachable at all and what balances the demolish-and-rebuild cycle: eviction and re-housing
        // now use the same door.
        //
        // The Pool is still read, one line above, and that reading is what changed character. It is no
        // longer a population count that this method then decrements; it is the RESIDUAL left after
        // placement ran earlier in the same phase, so a member of it is somebody the standing stock
        // could not house. A developer does not build while there are empty flats.
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
    /// <b>The condition behind the demolition is copied out before the demolition frees it</b>
    /// (milestone 6 task 2). <see cref="RuleInstanceTable.Reported"/> holds it wherever an author has
    /// written an <c>on_fail</c> chain, which is <c>02 §5.9</c>'s refusal of the sad-face icon, and
    /// <see cref="World.DestroyBuilding"/> frees the row holding it on the next line — so the answer
    /// has a lifetime of one line and <see cref="CondemnationTrailTable"/> is where it goes.
    /// ⚠ <b>The condition recorded is the worst-starved Rule's, not the first one found.</b> The
    /// verdict is an <em>or</em> and any exceedance settles it, but a trail entry names one cause, and
    /// the paragraph above already says which one the design means. Where an author wrote no chain the
    /// entry carries <see cref="ConditionId.None"/> and is kept anyway, because a Building that
    /// vanished with no entry at all is the worse answer.
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

        // THE SINK. A shell that has stood its kind's collapse duration comes down here, and its Lot
        // returns to vacant -- which is what lets the Zone Rule build on it again and what closes the
        // growth cycle abandonment opened.
        //
        // ⚠ An abandoned Building holds no Rules, so the pressure walk below would find nothing and
        // fall through to the occupant loop, which is also empty. Returning here rather than relying
        // on that is what keeps a second trail entry from ever being written for a Building that has
        // already been recorded as condemned once.
        //
        // Compared by ADDING the duration to the moment rather than subtracting, because Ticks
        // refuses a subtraction operator on purpose -- a clock difference that underflows is a
        // duration of about six hundred million years and reads as "not yet" for ever.
        //
        // ⚠ A shell whose kind the Ruleset has since STOPPED declaring never reaches this line: the
        // Declares guard above returns first. That is correct rather than missed -- such a Building
        // is derelict as well as abandoned, and CONTEXT.md is explicit that a derelict "stands until
        // the player clears it". A reload that describes the kind again restores it to a shell that
        // collapses on schedule.
        if (_world.Buildings.IsAbandoned(building))
        {
            int stands = _world.Rules.Kind(kind).CollapsesAfterDays;

            // ⚠ ZERO MEANS NEVER, and it is unreachable from any Ruleset a designer can write: the
            // loader requires the key wherever condemn_after is stated and refuses a non-positive
            // one. What reaches here with zero is a Ruleset built IN CODE -- every test fixture that
            // predates this milestone -- and the alternative reading cost an afternoon: zero as a
            // duration makes `due` the abandonment Tick itself, so the shell collapses on the sweep
            // that finds it and the abandoned state has no observable extent at all.
            //
            // ***So the engine's default is the old behaviour and the loader is what forbids it***,
            // which is adr/0048's division: the parse site refuses what a designer must not author,
            // and the engine stays defined for everything it can be handed.
            if (stands > 0)
            {
                Ticks due = _world.Buildings.AbandonedSince[building]
                    + new Ticks((ulong)Ticks.PerDay * (ulong)stands);

                if (tick >= due)
                {
                    _world.DestroyBuilding(_world.Buildings.Rows.At(building), tick);
                }
            }

            return;
        }

        // TWO THRESHOLDS, TWO SUBJECTS, TWO KEYS -- which is adr/0141's split finished. The ADR
        // separated the VERDICT in milestone 25 (a tenant's pressure ends the tenancy, the premises'
        // condemns the Building) and left both reading one number, so a world could not demonstrate
        // either mechanism without the other. Stripping decline from the shipped Rulesets is what
        // exposed it: evicted.toml, whose entire purpose is a tenancy that ends, stopped ending any.
        //
        // Both are DURATIONS IN TICKS as of milestone 17 and were counts of missed firings before it
        // (adr/0053). ⚠ The Ruleset authors DAYS -- `condemn_after_days`, `tenancy_ends_after_days` --
        // and RulesetLoader multiplies them up, so the designer's unit never reaches here. That is
        // adr/0048's division and it is also why the engine can be handed any Tick count: every test
        // fixture in the suite holds one too short to be authored in a file.
        KindDefinition definition = _world.Rules.Kind(kind);

        ulong condemns = (ulong)definition.CondemnAfterTicks;
        ulong endsTenancy = (ulong)definition.TenancyEndsAfterTicks;

        // ZERO MEANS NEVER for each, independently. A kind stating neither declines and evicts
        // nobody, which is what every Ruleset written before decline existed meant and what most of
        // the shipped files mean today.
        if (condemns == 0 && endsTenancy == 0)
        {
            return;
        }

        // THE PREMISES FIRST, and the order matters: abandonment empties the Building, so a premises
        // verdict makes every tenancy question below moot. Judging the tenants first would end a
        // tenancy on the same Tick the shell it sits in is abandoned, and the Household would be
        // unplaced twice.
        int worst = condemns == 0 ? -1 : Worst(building, tenant: default, condemns, tick);

        if (worst >= 0)
        {
            // Both writes are outside the walk, because demolition empties the list being walked, and
            // the trail is written before the demolition rather than after it: DestroyBuilding frees
            // the Rule Instance holding the condition and the Building row holding the kind, so a Tick
            // later there is nothing left to copy. That one-line lifetime is the whole reason the
            // trail is a table.
            _world.CondemnationTrail.Record(
                tick,
                _world.Lots.Rows.At(lot),
                kind,
                _world.RuleInstances.Reported[worst]);

            // ABANDONED RATHER THAN DEMOLISHED, and the shell stays standing on its Lot.
            // adr/0091 found 02 §5.9 contradicting itself twelve lines apart -- "its Lot returns to
            // vacant" against "the specific accumulated condition is retained on the Building" -- and
            // settled on the second reading, because the contagion, the sustained-detection duration
            // and the clearance verb all need a shell to act on. This line held the first reading
            // from before that ADR existed.
            //
            // ⚠ The city no longer removes anything. What clears an abandoned Building is adr/0091's
            // Demolish verb or its Govern clearance programme, and until one of those ships the stock
            // has no sink -- which is milestone 17's named risk arriving, deliberately, here.
            _world.AbandonBuilding(_world.Buildings.Rows.At(building), tick);
            _tickDemolished++;

            // Abandonment ends every tenancy in the Building, through EmptyPremises' own walk of the
            // occupant list. There is nothing left to end, and the shell holds nobody.
            return;
        }

        if (endsTenancy == 0)
        {
            return;
        }

        // The tenancies, each judged on ITS OWN Rules and against THEIR OWN threshold. ⚠ This is what
        // adr/0141 means by *what changes is what dies*: a Building whose tenant starves used to fall
        // down, so ONE STARVING TENANT CONDEMNED THE OTHER'S SHOP. Milestone 25 moved the subject of
        // the verdict; milestone 17 moved the number behind it, and until it did, a Ruleset could not
        // say `my tenants fail but my buildings stand` -- which is the whole of evicted.toml.
        //
        // Restarted from the front after every eviction, because World.Unplace removes the Household
        // from the list being walked. A Building holds `occupants` of them -- three in every shipped
        // Ruleset -- so the rescan is over single digits and is cheaper than a second decode API.
        bool ended = true;

        while (ended)
        {
            ended = false;

            foreach (int occupant in _world.Occupants.Walk(building))
            {
                Handle<Household> household = _world.Households.Rows.At(occupant);

                if (Worst(building, household, endsTenancy, tick) < 0)
                {
                    continue;
                }

                // ⚠ NOTHING RECORDS *WHY* THIS TENANCY ENDED, and that is deliberate rather than
                // missed. The condemnation trail is a LOT's -- it names the Lot, the kind and the
                // condition behind a demolition -- and a tenancy that ends leaves the Lot, the kind
                // and the Building exactly as they were, so an entry there would be a demolition
                // record for a Building still standing. The channel that carries `why is this
                // Household unhoused` is adr/0130's and ships with the Pool's give-up bound, which is
                // plans/0040 task 5. Filed rather than invented here.
                _world.Unplace(household);
                _tickEnded++;
                ended = true;
                break;
            }
        }
    }

    /// <summary>
    /// The starving Rule Instance of <paramref name="tenant"/>'s that has been starving longest
    /// relative to its own cadence, provided any of them has starved for <paramref name="endures"/>
    /// Ticks or more. <c>-1</c> when none has.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One walk answers both verdicts, and <paramref name="tenant"/> is the whole difference</b>
    /// (<c>adr/0141</c>). The unset handle selects the Rules the premises run themselves; a Household
    /// selects that tenant's. ***Spelling it as one function is what keeps the two verdicts from
    /// drifting into two pressure models*** — which matters more now that they are measured against
    /// two different authored numbers rather than one.
    /// </para>
    /// <para>
    /// <b>The walk does not stop at the first Rule past its threshold, and the reason is attribution
    /// rather than the predicate</b>: the verdict is an <em>or</em> and any exceedance settles it, but
    /// the trail records ONE condition and <see cref="Condemn"/>'s remarks say which one the design
    /// means — the subject's pressure is the LONGEST of its Rules'. That maximum is stored nowhere.
    /// </para>
    /// <para>
    /// 🔴 <b>THIS COMMENT ARGUED THE OPPOSITE UNTIL MILESTONE 17</b>, and the argument is worth
    /// keeping because it was nearly right: <i>"a tenant has no kind to declare its own — a Household
    /// never will — so a second threshold would be a number with nowhere to be authored."</i> The
    /// premise holds and the conclusion does not. A tenant threshold has nowhere to be authored
    /// <em>on the tenant</em>; it is authored on the <b>premises</b> kind, as
    /// <c>tenancy_ends_after_days</c>, which is <c>adr/0141</c>'s own answer to the identical problem
    /// about Bin capacity. ⚠ <b>The cost of the missing number was invisible</b> until decline was
    /// stripped from the shipped Rulesets and every tenancy in the repository stopped ending with it.
    /// </para>
    /// <para>
    /// ⚠ <b>THE THRESHOLD AND THE RANKING ARE DENOMINATED DIFFERENTLY, AND THAT IS THE DESIGN.</b>
    /// <paramref name="endures"/> is a <em>duration</em> in Ticks — milestone 17 moved it off
    /// <c>adr/0053</c>'s missed-firing count so a designer authors the felt quantity — but the
    /// comparison that picks the <em>worst</em> instance is still cross-multiplied against each
    /// Rule's <c>rate</c>, which ranks by <b>firings missed</b>.
    /// </para>
    /// <para>
    /// They answer different questions. <i>Should this be condemned</i> is about how long the thing
    /// has been broken, and a wall clock is the honest unit. <i>Which condition do we name in the
    /// trail</i> is about severity, and a Rule due every 8 Ticks that has been silent for 100 is
    /// more starved than one due every 32 that has been silent for the same 100. Collapsing both
    /// onto one unit would make the reported cause depend on cadence, which is
    /// <c>plans/0012</c> <b>Cause 5</b> waiting to happen in the Evidence panel.
    /// </para>
    /// </remarks>
    private int Worst(int building, Handle<Household> tenant, ulong endures, Ticks tick)
    {
        int worst = -1;
        ulong worstElapsed = 0;
        uint worstRate = 0;

        foreach (int instance in _world.BuildingRules.Walk(building))
        {
            if (_world.RuleInstances.Household[instance] != tenant)
            {
                continue;
            }

            if (!_world.RuleInstances.IsStarving(instance))
            {
                continue;
            }

            ulong elapsed = tick.Raw - _world.RuleInstances.StarvedSince[instance].Raw;
            uint rate = _world.Rules.Rule(_world.RuleInstances.Rule[instance]).Rate;

            if (elapsed < endures)
            {
                continue;
            }

            if (worst < 0 || elapsed * worstRate > worstElapsed * rate)
            {
                worst = instance;
                worstElapsed = elapsed;
                worstRate = rate;
            }
        }

        return worst;
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
        _endedFlow = _endedFlow.Fold(_tickEnded);

        _tickTriggers = 0;
        _tickVacant = 0;
        _tickOccupied = 0;
        _tickCreated = 0;
        _tickDemolished = 0;
        _tickEnded = 0;
    }
}
