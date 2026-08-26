namespace Borough.Core.Rules;

using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Space;
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
    RuleFlow Ended,
    RuleFlow Unpremised)
{
    /// <summary>Lots evaluated over the interval, which is what a trigger is charged for.</summary>
    /// <remarks>
    /// <b>A sum, and there is deliberately no peak equivalent.</b> The busiest Tick of one flow and
    /// the busiest Tick of the other need not be the same Tick, so adding the two peaks would report
    /// a burst that never happened. Only sums compose.
    /// </remarks>
    public long Evaluated => Vacant.Sum + Occupied.Sum;

    /// <summary>
    /// <b>Businesses turned out of premises that stay standing</b> — the trade half of
    /// <see cref="Ended"/>.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>A SEPARATE FLOW RATHER THAN A WIDENING OF <see cref="Ended"/>, and that is the whole
    /// point of it.</b> <c>Ended</c> counts Household tenancies. Folding trade evictions into it would
    /// make a counter that already answers <em>how many tenancies ended</em> answer a broader question
    /// under the same name — and milestone 26 task 7 shipped an assertion that read <c>Ended.Sum</c>
    /// as <em>a broke shop was turned out</em> when every one it counted was a dwelling.
    /// ***A counter that aggregates over the whole world, read as though it were scoped to the subject
    /// the claim names***, is a shape with four sightings in <c>plans/0012</c>; this is the one place
    /// the repair was cheap, so it was taken.
    /// </remarks>
    public long TurnedOut => Unpremised.Sum;
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
    private int _tickUnpremised;

    private RuleFlow _triggerFlow;
    private RuleFlow _vacantFlow;
    private RuleFlow _occupiedFlow;
    private RuleFlow _createdFlow;
    private RuleFlow _demolishedFlow;
    private RuleFlow _endedFlow;
    private RuleFlow _unpremisedFlow;

    /// <summary>Elapsed unserved need per District market row, recomputed each trigger.</summary>
    private long[] _demand = [];

    /// <summary>What this sweep has already answered per row, so two Lots cannot answer one hunger.</summary>
    private long[] _claimed = [];

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
            _triggerFlow,
            _vacantFlow,
            _occupiedFlow,
            _createdFlow,
            _demolishedFlow,
            _endedFlow,
            _unpremisedFlow);

        _triggerFlow = default;
        _vacantFlow = default;
        _occupiedFlow = default;
        _createdFlow = default;
        _demolishedFlow = default;
        _endedFlow = default;
        _unpremisedFlow = default;

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

        // Lazily, and at most once a Tick however many Rules read it -- the pass walks every Rule
        // Instance in the world and its answer does not depend on who asked. A world whose Rules are
        // all tier 0 never pays for it at all.
        bool demanded = false;

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
            if (definition.ReadsDemand && !demanded)
            {
                // Once a Tick and not once a Rule: the pass is over every Rule Instance in the world
                // and its answer does not depend on which Zone Rule asked.
                RecomputeDemand(tick);
                demanded = true;
            }

            Span<int> into = Scratch(definition.SampleFor(_world.Lots.Rows.SlotCount));
            int drawn = ZoneSample.Draw(_world.Lots, into, _key, tick, rule);

            // adr/0170: the SAMPLE IS THE CANDIDATE LIST, and no extra draws are taken. A tier-1
            // Rule scores every vacant Lot it drew and builds on the best one; a tier-0 Rule builds
            // on the first that passes, exactly as before. ⚠ Best-of-N over a sample the Rule was
            // already going to take is what makes siting non-random for no extra work -- the shape
            // EmploymentEngine.TryEmploy uses, one subsystem over.
            int best = Rows.NoSlot;
            int bestScore = int.MinValue;

            for (int i = 0; i < drawn; i++)
            {
                // The fork the rest of the slice hangs off. A vacant Lot is a candidate for creation;
                // an occupied one is a Building whose failure pressure is read, which is task 7's.
                if (_world.Lots.IsVacant(into[i]))
                {
                    _tickVacant++;

                    if (!definition.ReadsDemand)
                    {
                        Create(definition, into[i], tick);
                        continue;
                    }

                    if ((_world.Lots.Zone[into[i]] & definition.Admits) == 0)
                    {
                        continue;
                    }

                    int score = Score(into[i]);

                    // ⚠ STRICTLY better keeps the EARLIER draw, which is EmploymentEngine's rule and
                    // its reason: the result is then a function of draw ORDER and not of scan order,
                    // so it does not move when the Lot table is compacted.
                    if (best == Rows.NoSlot || score > bestScore)
                    {
                        best = into[i];
                        bestScore = score;
                    }
                }
                else
                {
                    _tickOccupied++;
                    Condemn(into[i], tick);
                }
            }

            if (best != Rows.NoSlot)
            {
                Create(definition, best, tick);
            }
        }

        CloseTick();
    }



    /// <summary>
    /// Whether this District's unserved need for <paramref name="definition"/>'s Good justifies
    /// raising one — and, if it does, <b>claims it</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Three terms and they fail in the cheapest order</b>: is there a market here at all, is the
    /// cooldown clear, and has enough hunger gone unanswered.
    /// </para>
    /// <para>
    /// <b>THE CLAIM IS THE SUBTRACTION AND IT IS DELIBERATELY NOT STORED.</b> <c>adr/0163</c> wants
    /// demand to be *a stock that answering depletes* because several Lots are sampled in one pass and
    /// each would otherwise read the same starving Households — ***an undifferentiated read overshoots
    /// in proportion to how many Lots happen to be sampled together***, which is a number belonging to
    /// the cadence rather than to the city. That failure is entirely <em>within</em> one sweep, so the
    /// claim lives exactly as long as one sweep: <see cref="_claimed"/> is zeroed by every recompute.
    /// ***A stored claim would be a magnitude that only grows, owing a decay rate no measurement could
    /// ratify*** — <c>adr/0006</c> avoided by not creating the collection.
    /// </para>
    /// <para>
    /// <b>The claim's AMOUNT is the threshold, derived rather than chosen.</b> Raising a Building
    /// claims exactly the hunger that justified it, so a District with twice the threshold raises two
    /// and one with a fraction over raises one. ⚠ <b>That is one fewer <c>plans/0002</c> §D row than
    /// <c>adr/0163</c> anticipated</b>, which expected the threshold and the claim to be a chosen pair
    /// ratified together; making the second follow the first removes the question of whether they
    /// agree. ***A pair that must be ratified together is better replaced by one number and a rule.***
    /// </para>
    /// <para>
    /// <b>The cooldown is the across-sweep half</b> (<c>adr/0170</c>). A shop raised this trigger has
    /// bought nothing, sold nothing and relieved nobody, so its District's hunger reads exactly as high
    /// on the next trigger; without this the same demand is answered again and again until it finally
    /// falls. ⚠ <b>Per District rather than globally, which is what keeps it legal under
    /// <c>adr/0163</c></b> — that record refuses a build-rate throttle for being unable to tell *five
    /// shops for one hungry neighbourhood* from *five shops for five*, and a per-market-row gate tells
    /// them apart exactly.
    /// </para>
    /// </remarks>
    private bool Demanded(ZoneRuleDefinition definition, int lot, Ticks tick)
    {
        int row = MarketFor(definition.Kind, lot);

        // No market: either the kind sells nothing, or this Lot stands on ground no District has
        // claimed yet. Both are ordinary rather than exceptional -- the watershed runs on a cadence
        // (RuleEngine.Bin's remark says the same from the purchase's side) -- and both mean there is
        // no demand here to read.
        if (row == DistrictMarkets.NoRow || row >= _demand.Length)
        {
            return false;
        }

        if (definition.CooldownDays > 0)
        {
            ulong since = tick.Raw - _world.DistrictPools.LastRaised[row].Raw;

            if (_world.DistrictPools.LastRaised[row].Raw != 0
                && since < (ulong)definition.CooldownDays * Ticks.PerDay)
            {
                    return false;
            }
        }

        long threshold = (long)definition.BuildThresholdDays * Ticks.PerDay;

        if (_demand[row] - _claimed[row] < threshold)
        {
            return false;
        }


        _claimed[row] += threshold;
        _world.DistrictPools.LastRaised[row] = tick;

        return true;
    }

    /// <summary>
    /// A Lot's site quality, as a small ordinal — <b>higher is better</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>An ORDINAL LADDER and never a weighted sum, and that is the whole reason this costs no
    /// ratifier.</b> A weighted score needs weights, every weight is hash-bearing, and each one would
    /// owe <c>adr/0052</c> a machine, a world and a quantity — for a <em>siting preference</em>, which
    /// no measurement settles. A ladder needs none: it says <em>nearer the houses beats further</em>,
    /// which is an ordering rather than a magnitude. ***<c>EmploymentEngine.TryEmploy</c> reaches the
    /// same shape from the other side***, scoring on <c>CommuteRung</c> rather than on minutes.
    /// </para>
    /// <para>
    /// <b>Two terms, in priority order.</b> <b>Density</b> — the Buildings in the Lot's Cell, which is
    /// <c>BuildingResidency.Density</c>: maintained at the write site, exact at every Tick, free to
    /// read, and non-zero in every shipped world. It is the only <em>customer</em> proxy the build has.
    /// ⚠ <b>It is capacity and not population</b> — there is no per-Cell Household or Citizen count
    /// anywhere in the core — so it says <em>how much building is here</em>, which is the honest
    /// reading. <b>Centrality</b> breaks its ties, on the District's own centre, which the watershed
    /// already computed because <c>adr/0134</c> makes a District <em>a centre and its basin</em>.
    /// ***Nothing new is derived for either.***
    /// </para>
    /// <para>
    /// ⚠ <b>Chebyshev distance rather than Euclidean</b>, because a square ring is what a Cell grid
    /// has and a hypotenuse would need a root this project does not take in integers. It is a
    /// tie-break over a ladder, so its exact shape is not load-bearing.
    /// </para>
    /// <para>
    /// ⚠ <b>Three signals were considered and refused as NOT CHEAP.</b> <em>Near other shops</em> and
    /// <em>not too near other shops</em> both need a per-Cell count filtered by Building kind, and no
    /// such index exists. <em>Footfall</em> does not exist and never will — <c>03 §3.7</c> makes a
    /// pedestrian network permanently non-saturating, so a foot Leg is priced once and attributed to
    /// no Segment. <em>Land value</em> is readable in O(1) and is ≤ 0 everywhere, because every term in
    /// it subtracts and amenity is unbuilt — so preferring a high one means preferring the quietest
    /// street, which is backwards for a shop.
    /// </para>
    /// </remarks>
    private int Score(int lot)
    {
        Cells east = CellGrid.ToCells(_world.Lots.East[lot]);
        Cells north = CellGrid.ToCells(_world.Lots.North[lot]);

        int density = _world.BuildingsInCells.Density(east, north);

        Handle<District> district = _world.DistrictsInCells.Of(_world.DistrictCells, east, north);

        // No District yet -- the watershed runs on a cadence and a Lot may stand on unclaimed ground
        // for up to [districts] revisit_ticks. Density alone still orders it.
        if (!_world.Districts.Rows.TryResolve(district, out int districtSlot))
        {
            return density * CentralityRange;
        }

        int acrossEast = (_world.Districts.CentreEast[districtSlot] - east).Magnitude.Raw;
        int acrossNorth = (_world.Districts.CentreNorth[districtSlot] - north).Magnitude.Raw;

        // Chebyshev: the larger of the two, and no Math.Max because there is none in this project.
        int reach = acrossEast > acrossNorth ? acrossEast : acrossNorth;

        if (reach > CentralityRange - 1)
        {
            reach = CentralityRange - 1;
        }

        // Density leads and centrality breaks its ties, which the multiply expresses without a weight:
        // one more Building in the Cell outranks any centrality difference, and centrality decides
        // only between Lots that tied on density.
        return (density * CentralityRange) + (CentralityRange - reach);
    }

    /// <summary>How finely centrality separates two Lots that tied on density.</summary>
    /// <remarks>
    /// <b>The District's working extent</b> — <c>CONTEXT.md</c> → District's 128 Cells, which is a
    /// curve's scale rather than a ceiling (<c>adr/0134</c>). ⚠ <b>It is a tie-break's resolution and
    /// not a distance anything is measured against</b>, so it carries no <c>plans/0002</c> §D row:
    /// changing it reorders Lots that were already equal on the term that leads.
    /// </remarks>
    private const int CentralityRange = 128;

    /// <summary>
    /// Sums elapsed unserved need onto every District market row, for this trigger.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>adr/0163</c>'s signal, and the wait list is what makes it one pass instead of a search.</b>
    /// A buyer short of a Good sleeps on the <b>market row's Bin</b> and never on a seller's
    /// (<c>adr/0167</c>) — so the Bin a starving Rule Instance waits on <em>is</em> the
    /// <c>(District, Good)</c> address its hunger belongs to, and <c>DistrictMarkets.MarketOf</c> turns
    /// it into the row in one lookup. ***No District walk, no reach query, no spatial index.***
    /// </para>
    /// <para>
    /// <b>Elapsed rather than counted</b>, which is <c>adr/0163</c>'s own fork: <c>StarvedSince</c> is
    /// a timestamp, recovery is total, and a Household starving intermittently is invisible to
    /// whichever samples catch it fed. ***A signal that flickers under sampling reports a sampling
    /// artefact.***
    /// </para>
    /// <para>
    /// ⚠ <b>Recomputed whole and stored nowhere.</b> One pass over live Rule Instances, which is the
    /// cost <c>adr/0163</c> owes <c>plans/0013</c> a measured row for. Keeping a running total instead
    /// would be a magnitude that only grows, needing a decay rate nobody could ratify —
    /// <c>adr/0006</c> avoided by not creating the collection.
    /// </para>
    /// </remarks>
    private void RecomputeDemand(Ticks tick)
    {
        int rows = _world.DistrictPools.Rows.SlotCount;

        if (_demand.Length < rows)
        {
            _demand = new long[rows];
            _claimed = new long[rows];
        }

        Array.Clear(_demand, 0, rows);
        Array.Clear(_claimed, 0, rows);

        int instances = _world.RuleInstances.Rows.SlotCount;

        for (int slot = 0; slot < instances; slot++)
        {
            if (!_world.RuleInstances.Rows.IsLive(slot)
                || !_world.RuleInstances.IsStarving(slot)
                || !_world.Bins.Rows.TryResolve(_world.RuleInstances.WaitingOn[slot], out int bin))
            {
                continue;
            }

            int row = _world.Markets.PoolRowOf(_world, bin);

            if (row == DistrictMarkets.NoRow)
            {
                continue;
            }

            _demand[row] += (long)(tick.Raw - _world.RuleInstances.StarvedSince[slot].Raw);
        }
    }

    /// <summary>
    /// The market row a Lot's District would sell <paramref name="kind"/>'s Good in, or
    /// <see cref="DistrictMarkets.NoRow"/>.
    /// </summary>
    /// <remarks>
    /// <b>A kind sells what it stocks AS A BUSINESS, and that needs no new Ruleset key.</b> A
    /// <c>[[building]]</c> declaring <c>{ resource = "sundries", owner = "business" }</c> is a shop;
    /// one declaring the same Bin <c>owner = "occupant"</c> is a larder. ***That is the discriminator
    /// <c>DistrictMarkets</c> already uses to decide who is a seller*** — a Business's own Bin, offered
    /// in its District's row — so the Zone Rule and the market agree by construction rather than by two
    /// definitions kept in step. ⚠ <b>Money is skipped</b>: a till is a Business-owned Bin and is not
    /// stock.
    /// </remarks>
    private int MarketFor(byte kind, int lot)
    {
        Handle<District> district = _world.DistrictsInCells.Of(
            _world.DistrictCells,
            CellGrid.ToCells(_world.Lots.East[lot]),
            CellGrid.ToCells(_world.Lots.North[lot]));

        if (!_world.Districts.Rows.TryResolve(district, out int districtSlot))
        {
            return DistrictMarkets.NoRow;
        }

        foreach (BinDeclaration declaration in _world.Rules.BinsOf(kind))
        {
            if (declaration.Tenancy != BinTenancy.Business
                || _world.Rules.IsConserved(declaration.Resource))
            {
                continue;
            }

            return _world.Markets.Row(_world, districtSlot, declaration.Resource);
        }

        return DistrictMarkets.NoRow;
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
        if ((_world.Lots.Zone[lot] & definition.Admits) == 0)
        {
            return;
        }

        if (definition.ReadsDemand)
        {
            if (!Demanded(definition, lot, tick))
            {
                return;
            }
        }
        else if (_world.UnplacedPool.Count == 0)
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

        int threshold = _world.Rules.Kind(kind).CondemnAfter;

        if (threshold == 0)
        {
            return;
        }

        // THE PREMISES' OWN RULES ONLY, which is adr/0141 and the whole of what changed here. The
        // unset handle is the premises (RuleInstanceTable.Household), so the same walk answers both
        // verdicts and the discriminator is a parameter rather than a branch.
        int worst = Worst(building, household: default, business: default, threshold, tick);

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

            _world.DestroyBuilding(_world.Buildings.Rows.At(building), tick);
            _tickDemolished++;

            // A demolition ends every tenancy in the Building, through DestroyBuilding's own walk of
            // the occupant list. There is nothing left to end, and the Building row is gone.
            return;
        }

        // The tenancies, each judged on ITS OWN Rules. ⚠ This is what adr/0141 means by *what changes
        // is what dies*: a Building whose tenant starves used to fall down, so ONE STARVING TENANT
        // CONDEMNED THE OTHER'S SHOP. The pressure mechanism is untouched -- same durations, same
        // threshold, same cross-multiplied comparison -- and only the subject of the verdict moved.
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

                if (Worst(building, household, business: default, threshold, tick) < 0)
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

        // THE TRADES, and this walk did not exist until 2026-08-26 (adr/0170). A Business occupies
        // through BuildingBusinesses -- "the second Occupant list" (adr/0113) -- and nothing walked
        // it, so its Failure Pressure reached no threshold AS A TENANCY while reaching the premises'
        // by mistake through Worst's missing filter. ***Both halves ship together on purpose***: the
        // filter alone would leave a broke shop immortal, and this walk alone would leave it
        // demolishing its building AND losing its tenancy for the same starvation.
        //
        // ⚠ Unpremise rather than DestroyBuilding, and that is adr/0141's "what changes is what
        // dies": the trade failed, the premises did not, and the building stands for another tenant.
        // The Business keeps its balance (adr/0144), joins the bounded UnpremisedPool, and leaves the
        // city through Depart with its money EXPORTED rather than destroyed (adr/0142) -- so the sink
        // adr/0006 requires is the one already built, and destroying the row instead would take its
        // balance with it, which is adr/0024's leak with extra steps.
        bool turned = true;

        while (turned)
        {
            turned = false;

            // Restarted from the front for the Household loop's reason: Unpremise removes the
            // Business from the list being walked.
            foreach (int occupant in _world.BuildingBusinesses.Walk(building))
            {
                Handle<Business> business = _world.Businesses.Rows.At(occupant);

                if (Worst(building, household: default, business, threshold, tick) < 0)
                {
                    continue;
                }

                // ⚠ NOTHING RECORDS *WHY*, for the Household loop's reason exactly: the condemnation
                // trail is a LOT's, and a trade turned out of a building still standing has moved no
                // Lot, no kind and no Building.
                _world.Unpremise(business, tick);
                _tickUnpremised++;
                turned = true;
                break;
            }
        }
    }

    /// <summary>
    /// The worst-starved Rule Instance one subject runs on this Building past
    /// <paramref name="threshold"/> missed firings, or <c>-1</c> when none of them is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One walk answers both verdicts, and the two subject handles are the whole difference</b>
    /// (<c>adr/0141</c>). Both unset selects the Rules the premises run themselves; a Household or a
    /// Business selects that tenant's. ***Spelling it as one function is what keeps the two verdicts
    /// from drifting into two pressure models***, which is the failure <c>adr/0053</c>'s
    /// <em>missed firings rather than Ticks</em> was already guarding against on the other axis.
    /// </para>
    /// <para>
    /// 🔴 <b>IT FILTERED ON <paramref name="household"/> ALONE UNTIL 2026-08-26 AND THAT WAS A DEFECT,
    /// NOT A SIMPLIFICATION</b> (<c>adr/0170</c>, <c>plans/0044</c> <b>F44</b>). A Business's Rule
    /// Instance leaves <c>Household</c> unset, so it matched the *premises* call and its failure
    /// pressure was counted as the building's — ***a broke shop demolished its own premises instead of
    /// ending its tenancy.*** Measured at 20 shops raised and 2 demolished, which is exactly the number
    /// that go broke. ⚠ <b>The sentence this paragraph replaced was TRUE WHEN IT WAS WRITTEN</b>,
    /// because no Business ran a Rule until milestone 26 task 1.
    /// </para>
    /// <para>
    /// <b>The walk does not stop at the first Rule past its threshold, and the reason is attribution
    /// rather than the predicate</b>: the verdict is an <em>or</em> and any exceedance settles it, but
    /// the trail records ONE condition and <see cref="Condemn"/>'s remarks say which one the design
    /// means — the subject's pressure is the LONGEST of its Rules'. That maximum is stored nowhere.
    /// </para>
    /// <para>
    /// <b>Missed firings, compared by cross-multiplying</b> — <c>elapsed/rate</c> against
    /// <c>worstElapsed/worstRate</c>, for the reason <see cref="Condemn"/> gives for multiplying the
    /// threshold: the division would be spelled through <see cref="Arithmetic.IntegerMath"/> for an
    /// answer nothing keeps. Strictly greater, so a tie leaves the earlier Rule in place and the
    /// choice is a function of the Building's own Rule list rather than of the order two equal
    /// pressures were met in.
    /// </para>
    /// <para>
    /// ⚠ <b>Both subjects are measured against the SAME <c>condemn_after</c>, which is the premises'
    /// kind's.</b> A tenant has no kind to declare its own — a Household never will, and a Business
    /// gets one at milestone 27 — so a second threshold would be a number with nowhere to be authored.
    /// <c>adr/0052</c> is not owed a ratifier here because no number is chosen; what is chosen is that
    /// there is only one.
    /// </para>
    /// </remarks>
    private int Worst(
        int building,
        Handle<Household> household,
        Handle<Business> business,
        int threshold,
        Ticks tick)
    {
        int worst = -1;
        ulong worstElapsed = 0;
        uint worstRate = 0;

        foreach (int instance in _world.BuildingRules.Walk(building))
        {
            // 🔴 BOTH SUBJECTS, AND THE SECOND ONE WAS MISSING UNTIL 2026-08-26 (adr/0170). A
            // Business's Rule Instance leaves Household UNSET, so while this filtered on Household
            // alone every trade's Rule matched the PREMISES call -- and a broke shop's failure
            // pressure condemned the building it stood in instead of ending its tenancy. Measured on
            // rulesets/provisioned.toml at 2,000 Citizens over 24,576 Ticks: 20 shops raised, 2
            // demolished, and 2 is exactly the number that go broke. The attribution is airtight
            // because a shopfront runs two Rules and `stock` has inputs = [], so a Rule with no
            // inputs can never be Blocking.Supply and can never starve; the levy is the only thing on
            // that kind able to set StarvedSince.
            //
            // ⚠ That is adr/0141's "ONE STARVING TENANT CONDEMNED THE OTHER'S SHOP" arriving one
            // subject late -- the record fixed it for Households and no Business ran a Rule until
            // milestone 26 task 1, so the gap opened after the repair rather than surviving it.
            if (_world.RuleInstances.Household[instance] != household
                || _world.RuleInstances.Business[instance] != business)
            {
                continue;
            }

            if (!_world.RuleInstances.IsStarving(instance))
            {
                continue;
            }

            ulong elapsed = tick.Raw - _world.RuleInstances.StarvedSince[instance].Raw;
            uint rate = _world.Rules.Rule(_world.RuleInstances.Rule[instance]).Rate;

            if (elapsed < (ulong)threshold * rate)
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
        _unpremisedFlow = _unpremisedFlow.Fold(_tickUnpremised);

        _tickTriggers = 0;
        _tickVacant = 0;
        _tickOccupied = 0;
        _tickCreated = 0;
        _tickDemolished = 0;
        _tickEnded = 0;
        _tickUnpremised = 0;
    }
}
