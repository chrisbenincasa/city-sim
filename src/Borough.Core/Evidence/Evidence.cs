namespace Borough.Core.Evidence;

using Borough.Core.Arithmetic;
using Borough.Core.Entities;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

/// <summary>
/// <c>02 §9</c>'s question surface: one entry point per subject, assembled on a click.
/// </summary>
/// <remarks>
/// <para>
/// <b>An assembler rather than a store</b>, which is milestone 6's D2. Most of <c>02 §9</c> needs no
/// accumulator — a Building's occupants and Bin levels are live state and a Rule's pressure is two
/// columns and a subtraction — so the default is to read the world and re-derive, and a stored answer
/// needs an argument each time: <em>the entity holding it will not exist when the question is
/// asked</em>. Exactly one thing here meets that test today, and it is
/// <see cref="CondemnationTrailTable"/>.
/// </para>
/// <para>
/// <b>Ids and numbers, never strings</b> (<c>adr/0002</c>). Every kind, condition, Resource and
/// activity below leaves as the id the Ruleset gave it, and the host resolves it. The leak this guards
/// against is not <c>using Godot;</c> — it is a method that returns a formatted string because a panel
/// wanted one.
/// </para>
/// <para>
/// <b>Static and <c>World</c>-taking, on <c>Readouts.Read</c>'s precedent</b>, which is the other
/// pure per-entity read in the project and is documented as <em>the set a shell may enumerate to
/// build an inspector</em>. Nothing here is stored on the <c>World</c>, because none of it is state.
/// </para>
/// <para>
/// ⚠ <b>Detailed answers allocate; no method may be called from <c>step()</c>.</b> That is the
/// hot/cold axis <see cref="ColdPathAttribute"/> records, and it is what buys the arrays. The cost is
/// paid once by a human who is waiting.
/// </para>
/// </remarks>
public static class Evidence
{
    public static SupplyEvidence SupplyOfBuilding(World world, Handle<Building> building,
        Handle<Household> household = default, Handle<Business> business = default)
    {
        if (!world.Buildings.Rows.TryResolve(building, out int slot)) return default;
        int shortfalls = 0, routine = 0, unavailable = 0;
        RuleEvidence primary = default;
        UtilityEvidence shortest = default;
        foreach (int instance in world.BuildingRules.Walk(slot))
        {
            RuleEvidence rule = ReadRule(world, instance, world.Tick);
            if (!household.IsNone && rule.Tenant != household) continue;
            if (!business.IsNone && rule.Business != business) continue;
            // Before the blocked test, because a Rule under a brownout is not blocked at all. It
            // succeeds at a reduced count.
            NoteShortUtility(world, instance, rule.Rule, ref shortest);
            if (rule.Blocked == Blocking.Nothing) continue;
            if (rule.WaitingBin.IsNone || rule.Blocked is not (Blocking.Supply or Blocking.Space))
                unavailable++;
            if (rule.Blocked == Blocking.Space) routine++;
            if (rule.Blocked != Blocking.Supply) continue;
            shortfalls++;
            if (primary.InstanceId == 0 || rule.MissedFirings > primary.MissedFirings
                || rule.MissedFirings == primary.MissedFirings && rule.InstanceId < primary.InstanceId)
                primary = rule;
        }
        return new SupplyEvidence(world.Rules.Declares(world.Buildings.Kind[slot]),
            shortfalls, routine, unavailable, primary, shortest);
    }

    /// <summary>
    /// The first delivered Utility a Building's Rules draw, or false where they draw none.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>For a reader that paints every Building at once</b> — a city-wide wash cannot afford
    /// <see cref="OfBuilding"/> per Building, and one Utility is what a single colour can say. The
    /// order is the Rules' declaration order, so the answer is the same on every pass.
    /// </para>
    /// <para>
    /// The walk follows each Rule Instance's <c>on_fail</c> chain, because a link has no instance
    /// of its own and draws against the head's.
    /// </para>
    /// </remarks>
    /// <param name="world">The world to read.</param>
    /// <param name="building">Which Building.</param>
    /// <param name="drawn">The Utility and what its network delivers, when there is one.</param>
    /// <exception cref="ArgumentNullException"><paramref name="world"/> is null.</exception>
    public static bool TryUtilityOfBuilding(
        World world, Handle<Building> building, out UtilityEvidence drawn)
    {
        ArgumentNullException.ThrowIfNull(world);

        drawn = default;

        if (!world.Buildings.Rows.TryResolve(building, out int slot))
        {
            return false;
        }

        foreach (int instance in world.BuildingRules.Walk(slot))
        {
            for (RuleId at = world.RuleInstances.Rule[instance]; !at.IsNone;
                at = world.Rules.Rule(at).OnFail)
            {
                foreach (Term term in world.Rules.Inputs(at))
                {
                    if (IsDrawnUtility(world, term))
                    {
                        drawn = ReadUtility(world, instance, term.Bin.Resource);

                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Keeps the worst of the delivered Utilities one Rule Instance draws short, if any is.
    /// </summary>
    /// <remarks>
    /// The worst rather than the first, because a reader given one Utility's name wants the one
    /// hurting most. Ties keep the one already held, so the answer is the Rules' own order. The
    /// scan covers the Instance's <c>on_fail</c> chain, whose links draw against this Instance.
    /// </remarks>
    private static void NoteShortUtility(
        World world, int instance, RuleId rule, ref UtilityEvidence worst)
    {
        for (RuleId at = rule; !at.IsNone; at = world.Rules.Rule(at).OnFail)
        {
            foreach (Term term in world.Rules.Inputs(at))
            {
                if (!IsDrawnUtility(world, term))
                {
                    continue;
                }

                UtilityEvidence drawn = ReadUtility(world, instance, term.Bin.Resource);

                if (drawn.Delivered < Ratio.One
                    && (worst.Utility.Raw == 0 || drawn.Delivered < worst.Delivered))
                {
                    worst = drawn;
                }
            }
        }
    }

    /// <summary>Adds the delivered Utilities one Rule Instance draws, each one once.</summary>
    /// <remarks>
    /// The scan for a duplicate is linear over a list a Ruleset's Utility count long, which is what
    /// a set would be at these sizes. The Instance's <c>on_fail</c> chain is included, head first,
    /// because its links draw against this Instance rather than holding one of their own.
    /// </remarks>
    private static void AddUtilities(
        World world, List<UtilityEvidence> into, int instance, RuleId rule)
    {
        for (RuleId link = rule; !link.IsNone; link = world.Rules.Rule(link).OnFail)
        {
            foreach (Term term in world.Rules.Inputs(link))
            {
                if (!IsDrawnUtility(world, term))
                {
                    continue;
                }

                bool held = false;

                for (int at = 0; at < into.Count && !held; at++)
                {
                    held = into[at].Utility == term.Bin.Resource;
                }

                if (!held)
                {
                    into.Add(ReadUtility(world, instance, term.Bin.Resource, includeReserve: true));
                }
            }
        }
    }

    /// <summary>Whether a term is a draw on a District's Utility network.</summary>
    private static bool IsDrawnUtility(World world, in Term term) =>
        term.Bin.Scope == Scope.Pool && world.Rules.IsDeliveredUtility(term.Bin.Resource);

    /// <summary>
    /// What the network a Rule Instance's term addresses delivers, and what it carried yesterday.
    /// </summary>
    /// <remarks>
    /// Premises in no District have no network row and are served nothing, which is
    /// <c>RuleEngine.Draw</c>'s own answer rather than a missing reading.
    /// </remarks>
    private static UtilityEvidence ReadUtility(
        World world, int instance, ResourceId utility, bool includeReserve = false)
    {
        int flow = world.UtilityFlowOf(instance, utility);
        bool reserve = world.Rules.StorageOf(utility) == UtilityStorage.Reserve;

        if (flow == Rows.NoSlot)
        {
            return new UtilityEvidence(utility, Ratio.Zero, 0, 0, reserve, 0, 0, 0, 0);
        }

        long level = 0;
        long capacity = 0;

        if (reserve && includeReserve)
        {
            int pool = world.DistrictPools.Rows.Resolve(world.UtilityFlows.Pool[flow]);
            int district = world.Districts.Rows.Resolve(world.DistrictPools.District[pool]);
            (level, capacity) = world.ReadUtilityReserve(district, utility);
        }

        return new UtilityEvidence(
            utility,
            world.UtilityFlows.Delivered[flow],
            world.UtilityFlows.Received[flow],
            world.UtilityFlows.Asked[flow],
            reserve, level, capacity,
            world.UtilityFlows.FromReserve[flow],
            world.UtilityFlows.ToReserve[flow]);
    }

    /// <summary>
    /// Assembles the city's blocked set: everything that is stopped, grouped by why.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One walk of every live Building's Rule Instances, and no accumulator anywhere.</b> That is
    /// this class's own default holding at city scale — the entities holding the answer are all
    /// standing while the question is asked, so re-deriving beats shadowing (milestone 6's D2).
    /// </para>
    /// <para>
    /// ⚠ <b>The dedupe is per Building because it can be.</b> A tenant's Rule Instances hang off the
    /// Building it occupies, so every Rule belonging to one subject is reached inside one iteration
    /// of the outer loop — which is what lets a subject be counted once per cause without a set
    /// spanning the whole city.
    /// </para>
    /// <para>
    /// ⚠ <b>The order is total and derived, never incidental.</b> Groups sort trouble before routine
    /// waiting and then by how many subjects they stop; subjects sort worst first within their group
    /// and tie-break on the never-reused row id. A panel paging through this must get the same page
    /// twice.
    /// </para>
    /// </remarks>
    /// <param name="world">The world to read.</param>
    /// <exception cref="ArgumentNullException"><paramref name="world"/> is null.</exception>
    public static CityEvidence OfCity(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        Ticks now = world.Tick;
        var causes = new List<CityCause>();
        var counts = new List<int>();
        var worst = new List<long>();
        var subjects = new List<CitySubject>();
        int read = 0;

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (!world.Buildings.Rows.IsLive(slot))
            {
                continue;
            }

            read++;

            Handle<Building> building = world.Buildings.Rows.At(slot);
            int first = subjects.Count;

            foreach (int instance in world.BuildingRules.Walk(slot))
            {
                RuleEvidence rule = ReadRule(world, instance, now);

                if (rule.Blocked == Blocking.Nothing)
                {
                    continue;
                }

                SubjectKind kind = !rule.Business.IsNone
                    ? SubjectKind.Business
                    : rule.Tenant.IsNone ? SubjectKind.Premises : SubjectKind.Household;

                ulong id = kind switch
                {
                    SubjectKind.Business => world.Businesses.Rows.TryResolve(rule.Business, out int trade)
                        ? world.Businesses.Rows.IdAt(trade) : 0,
                    SubjectKind.Household => world.Households.Rows.TryResolve(rule.Tenant, out int family)
                        ? world.Households.Rows.IdAt(family) : 0,
                    _ => world.Buildings.Rows.IdAt(slot),
                };

                // A subject whose own row has already gone is not a subject a panel can open, and a
                // Rule Instance outliving its owner by a Tick is ordinary rather than exceptional.
                if (id == 0)
                {
                    continue;
                }

                var cause = new CityCause(
                    kind, rule.Blocked, rule.WaitingFor, rule.WaitingOn, !rule.WaitingBin.IsNone);

                int group = causes.IndexOf(cause);

                if (group < 0)
                {
                    causes.Add(cause);
                    counts.Add(0);
                    worst.Add(0);
                    group = causes.Count - 1;
                }

                int at = first;

                while (at < subjects.Count
                    && (subjects[at].Group != group || subjects[at].SubjectId != id))
                {
                    at++;
                }

                if (at < subjects.Count)
                {
                    if (rule.MissedFirings > subjects[at].MissedFirings)
                    {
                        subjects[at] = subjects[at] with { MissedFirings = rule.MissedFirings };
                    }

                    continue;
                }

                subjects.Add(new CitySubject(
                    group, kind, building, rule.Tenant, rule.Business, id, rule.MissedFirings));
                counts[group]++;
            }
        }

        foreach (CitySubject subject in subjects)
        {
            if (subject.MissedFirings > worst[subject.Group])
            {
                worst[subject.Group] = subject.MissedFirings;
            }
        }

        int[] order = new int[causes.Count];

        for (int i = 0; i < order.Length; i++)
        {
            order[i] = i;
        }

        Array.Sort(order, (left, right) => Rank(causes, counts, left, right));

        int[] moved = new int[causes.Count];
        CityGroup[] groups = new CityGroup[causes.Count];

        for (int i = 0; i < order.Length; i++)
        {
            moved[order[i]] = i;
            groups[i] = new CityGroup(causes[order[i]], counts[order[i]], worst[order[i]]);
        }

        CitySubject[] listed = new CitySubject[subjects.Count];

        for (int i = 0; i < listed.Length; i++)
        {
            listed[i] = subjects[i] with { Group = moved[subjects[i].Group] };
        }

        Array.Sort(listed, (left, right) => left.Group != right.Group
            ? left.Group.CompareTo(right.Group)
            : left.MissedFirings != right.MissedFirings
                ? right.MissedFirings.CompareTo(left.MissedFirings)
                : left.SubjectId.CompareTo(right.SubjectId));

        return new CityEvidence(now, read, groups, listed);
    }

    /// <summary>
    /// Orders two causes: trouble before routine waiting, then the one stopping more subjects.
    /// </summary>
    /// <remarks>
    /// <b>Waiting for output space sorts last on purpose.</b> A full store is not a fault
    /// (<c>plans/0064</c> row 7), so it may appear in the list and must never head it.
    /// </remarks>
    private static int Rank(List<CityCause> causes, List<int> counts, int left, int right)
    {
        bool routineLeft = causes[left].Blocked == Blocking.Space;
        bool routineRight = causes[right].Blocked == Blocking.Space;

        if (routineLeft != routineRight)
        {
            return routineLeft ? 1 : -1;
        }

        if (counts[left] != counts[right])
        {
            return counts[right].CompareTo(counts[left]);
        }

        if (causes[left].Kind != causes[right].Kind)
        {
            return causes[left].Kind.CompareTo(causes[right].Kind);
        }

        if (causes[left].WaitingFor.Raw != causes[right].WaitingFor.Raw)
        {
            return causes[left].WaitingFor.Raw.CompareTo(causes[right].WaitingFor.Raw);
        }

        return causes[left].WaitingOn != causes[right].WaitingOn
            ? causes[left].WaitingOn.CompareTo(causes[right].WaitingOn)
            : causes[right].Explained.CompareTo(causes[left].Explained);
    }

    /// <summary>
    /// Assembles <c>02 §9</c>'s Building answer.
    /// </summary>
    /// <param name="world">The world to read.</param>
    /// <param name="building">Which Building.</param>
    /// <exception cref="ArgumentNullException"><paramref name="world"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The handle does not resolve.</exception>
    public static BuildingEvidence OfBuilding(World world, Handle<Building> building)
    {
        ArgumentNullException.ThrowIfNull(world);

        int slot = world.Buildings.Rows.Resolve(building);
        byte kind = world.Buildings.Kind[slot];

        bool declared = world.Rules.Declares(kind);
        int occupancy = declared && world.TryDeclaredOccupancy(kind, slot, out int held) ? held : 0;

        // ⚠ SUMMED OVER THE TENANTS, because premises declare no jobs any more (adr/0141). A
        // Building's employment is whatever the trades inside it declare between them, so a Building
        // with no tenant employs nobody -- which is the honest answer rather than a missing one.
        int jobs = 0;

        foreach (int tenant in world.BuildingBusinesses.Walk(slot))
        {
            if (world.Rules.DeclaresBusiness(world.Businesses.Kind[tenant]))
            {
                jobs += world.DeclaredJobs(tenant);
            }
        }

        Handle<Household>[] occupants = new Handle<Household>[world.Occupants.Length(slot)];
        int at = 0;

        foreach (int household in world.Occupants.Walk(slot))
        {
            occupants[at++] = world.Households.Rows.At(household);
        }

        // 🔴 The tenants' worker lists, NOT the Building's -- `Workers` is indexed by Business slot
        // since milestone 27 task 7, so walking it at a Building slot read out of bounds the moment
        // the two tables differed in width. A Building's staff is the union of its tenants' staff.
        int staff = 0;

        foreach (int tenant in world.BuildingBusinesses.Walk(slot))
        {
            staff += world.Workers.Length(tenant);
        }

        Handle<Citizen>[] workers = new Handle<Citizen>[staff];
        at = 0;

        foreach (int tenant in world.BuildingBusinesses.Walk(slot))
        {
            foreach (int citizen in world.Workers.Walk(tenant))
            {
                workers[at++] = world.Citizens.Rows.At(citizen);
            }
        }

        // The premises' Bins and then every tenant's, in occupant order. ⚠ THE SECOND HALF IS WHY
        // THIS IS TWO WALKS: a tenant's Bin hangs off the Household (adr/0143) and is in no
        // Building's list, so the panel showed Rules drawing from Bins it did not print until
        // milestone 25 task 4. The tenant's balance is skipped -- it is money, it is unbounded, and
        // the Household finances panel is where a reader meets it.
        var bins = new List<BinEvidence>(world.BuildingBins.Length(slot));

        foreach (int bin in world.BuildingBins.Walk(slot))
        {
            bins.Add(new BinEvidence(
                world.Bins.Resource[bin],
                world.Bins.LevelAt(bin),
                world.Bins.Capacity[bin],
                default));
        }

        foreach (Handle<Household> tenant in occupants)
        {
            Handle<Bin> owned = world.Households.BinHead[world.Households.Rows.Resolve(tenant)];

            while (!owned.IsNone)
            {
                int bin = world.Bins.Rows.Resolve(owned);

                if (!world.Rules.IsConserved(world.Bins.Resource[bin]))
                {
                    bins.Add(new BinEvidence(
                        world.Bins.Resource[bin],
                        world.Bins.LevelAt(bin),
                        world.Bins.Capacity[bin],
                        tenant));
                }

                owned = world.Bins.OwnerNext[bin];
            }
        }

        RuleEvidence[] rules = new RuleEvidence[world.BuildingRules.Length(slot)];
        Ticks now = world.Tick;

        // Two maxima and not one, because milestone 25 task 4 made them two verdicts: the premises'
        // pressure condemns the BUILDING and a tenant's ends a TENANCY (adr/0141). Folding them
        // together would print one number against a condemn_after that governs only half of it.
        long pressure = 0;
        long tenantPressure = 0;
        at = 0;

        var utilities = new List<UtilityEvidence>();

        foreach (int instance in world.BuildingRules.Walk(slot))
        {
            RuleEvidence evidence = ReadRule(world, instance, now);

            AddUtilities(world, utilities, instance, evidence.Rule);

            if (evidence.Tenant.IsNone)
            {
                if (evidence.MissedFirings > pressure)
                {
                    pressure = evidence.MissedFirings;
                }
            }
            else if (evidence.MissedFirings > tenantPressure)
            {
                tenantPressure = evidence.MissedFirings;
            }

            rules[at++] = evidence;
        }

        var waste = new List<WasteEvidence>();
        ushort today = BusinessAccounts.DayOf(now);

        foreach (int bin in world.BuildingBins.Walk(slot))
        {
            AddWaste(world, waste, bin, default, default, today);
        }

        foreach (Handle<Household> tenant in occupants)
        {
            for (Handle<Bin> owned = world.Households.BinHead[world.Households.Rows.Resolve(tenant)];
                 !owned.IsNone;)
            {
                int bin = world.Bins.Rows.Resolve(owned);
                AddWaste(world, waste, bin, default, tenant, today);
                owned = world.Bins.OwnerNext[bin];
            }
        }

        foreach (int business in world.BuildingBusinesses.Walk(slot))
        {
            Handle<Business> owner = world.Businesses.Rows.At(business);

            for (Handle<Bin> owned = world.Businesses.BinHead[business]; !owned.IsNone;)
            {
                int bin = world.Bins.Rows.Resolve(owned);
                AddWaste(world, waste, bin, owner, default, today);
                owned = world.Bins.OwnerNext[bin];
            }
        }

        return new BuildingEvidence(
            building, kind, world.Buildings.Lot[slot], declared, occupancy, jobs,
            occupants, workers, [.. bins], rules, [.. waste], [.. utilities],
            pressure, tenantPressure);
    }

    private static void AddWaste(
        World world, List<WasteEvidence> waste, int bin, Handle<Business> business,
        Handle<Household> tenant, ushort today)
    {
        int row = world.Expiries.RowOf(world.Bins, bin);

        if (row == Rows.NoSlot)
        {
            return;
        }

        waste.Add(new WasteEvidence(
            world.Bins.Resource[bin], business, tenant,
            world.Expiries.SpoiledOn(row, today), world.Expiries.PriorSpoiledOn(row, today)));
    }

    /// <summary>
    /// Assembles <c>02 §9</c>'s Citizen answer.
    /// </summary>
    /// <param name="world">The world to read.</param>
    /// <param name="citizen">Which Citizen.</param>
    /// <exception cref="ArgumentNullException"><paramref name="world"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The handle does not resolve.</exception>
    public static CitizenEvidence OfCitizen(World world, Handle<Citizen> citizen)
    {
        ArgumentNullException.ThrowIfNull(world);

        int slot = world.Citizens.Rows.Resolve(citizen);
        Handle<Household> household = world.Citizens.HouseholdOf[slot];

        Handle<Building> home = world.Households.Rows.TryResolve(household, out int householdSlot)
            ? world.Households.Dwelling[householdSlot]
            : default;

        // Severable, so an unresolvable workplace is the job having stopped existing rather than a
        // broken handle -- CitizenTable.Workplace says so at the declaration. Reported as unset,
        // which is what the simulation itself believes.
        Handle<Business> workplace = world.Businesses.Rows.IsValid(world.Citizens.Workplace[slot])
            ? world.Citizens.Workplace[slot]
            : default;

        // InFlight stored means no journey of theirs has ever ended, so the answer is absent rather
        // than a Fate nobody can interpret. CitizenTable.LastTripFate: the sentinel is the one the
        // Fate set already reserved for `not an outcome`, so it costs nothing and cannot collide.
        var lastFate = (TripFate)world.Citizens.LastTripFate[slot];

        PastTripEvidence? last = lastFate == TripFate.InFlight
            ? null
            : new PastTripEvidence(lastFate, world.Citizens.LastTripEndedDay[slot]);

        // 02 section 9's household finances. Absent rather than zero where the Household holds no
        // money Bin, which is what a Ruleset naming no money Resource produces -- World.BalanceOf
        // conflates the two on purpose and says why, and a reader is the one caller that must not.
        Money? balance = world.Households.Rows.TryResolve(household, out int financesSlot)
            && world.Bins.Rows.TryResolve(world.Households.Balance[financesSlot], out int balanceBin)
                ? new Money(world.Bins.LevelAt(balanceBin))
                : null;

        // 02 section 9's third clause. Read off the same resolved slot as the balance and null on the
        // same condition: an unhoused Citizen has no Household row, and a zero would say "ideally fed"
        // about somebody nobody is feeding.
        int? sustenance = world.Households.Rows.TryResolve(household, out int needSlot)
            ? world.Households.Sustenance[needSlot]
            : null;

        // adr/0027's consequence: a Household's own preference, or an odd choice is inexplicable.
        // Null on the same condition as the two above -- an unhoused Citizen has no Household row to
        // ask, and the neutral value would say "wanted nothing in particular" about somebody nobody
        // can ask at all.
        int? taste = world.Households.Rows.TryResolve(household, out int tasteSlot)
            ? world.Rules.CentralityTaste(
                world.Key, world.Households.Rows.IdAt(tasteSlot), world.Households.LifeStage[tasteSlot])
            : null;

        return new CitizenEvidence(
            citizen,
            household,
            home,
            workplace,
            world.Citizens.Activity[slot],
            world.Citizens.PlannedCommute[slot],
            world.Citizens.ReachFailures[slot],
            balance,
            sustenance,
            taste,
            InFlightTrip(world, citizen),
            last,
            FailingCommuteOf(world, slot),
            LastCommuteEpisodeOf(world, slot));
    }

    /// <summary>
    /// The workers of <paramref name="business"/> whose outbound commutes are failing, in worker
    /// list order. Derived from each worker's clock and never saved.
    /// </summary>
    /// <param name="world">The world to read.</param>
    /// <param name="business">Which Business.</param>
    /// <exception cref="ArgumentNullException"><paramref name="world"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The handle does not resolve.</exception>
    public static ReadOnlyMemory<Handle<Citizen>> FailingCommutersOf(World world, Handle<Business> business)
    {
        ArgumentNullException.ThrowIfNull(world);

        int slot = world.Businesses.Rows.Resolve(business);
        List<Handle<Citizen>> failing = [];

        foreach (int worker in world.Workers.Walk(slot))
        {
            if (world.Citizens.CommuteFailedSince[worker] != Ticks.Zero)
            {
                failing.Add(world.Citizens.Rows.At(worker));
            }
        }

        return failing.ToArray();
    }

    /// <summary>
    /// The Building's running cut-off clock, or absent when it is reachable or its kind states no
    /// <c>abandoned_after_cut_off_days</c>.
    /// </summary>
    /// <param name="world">The world to read.</param>
    /// <param name="building">Which Building.</param>
    /// <exception cref="ArgumentNullException"><paramref name="world"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The handle does not resolve.</exception>
    public static CutOffEvidence? CutOffOf(World world, Handle<Building> building)
    {
        ArgumentNullException.ThrowIfNull(world);

        int slot = world.Buildings.Rows.Resolve(building);
        ulong since = world.Buildings.CutOffSince[slot].Raw;

        if (since == 0)
        {
            return null;
        }

        var first = new Ticks(since - 1);
        ulong deadline = (ulong)world.Rules.Kind(world.Buildings.Kind[slot]).AbandonedAfterCutOffTicks;

        return new CutOffEvidence(
            first, new Ticks(first.Raw + deadline), world.VehicleAccessPoint(slot).Exists);
    }

    private static FailingCommuteEvidence? FailingCommuteOf(World world, int slot)
    {
        ulong since = world.Citizens.CommuteFailedSince[slot].Raw;

        if (since == 0)
        {
            return null;
        }

        var first = new Ticks(since - 1);

        return new FailingCommuteEvidence(first, new Ticks(first.Raw + world.Rules.Jobs.FailedCommuteDeadline.Raw));
    }

    private static CommuteEpisodeEvidence? LastCommuteEpisodeOf(World world, int slot)
    {
        var outcome = (CommuteEpisodeOutcome)world.Citizens.CommuteEpisodeOutcome[slot];

        return outcome == CommuteEpisodeOutcome.None
            ? null
            : new CommuteEpisodeEvidence(
                world.Citizens.CommuteEpisodeStartDay[slot], world.Citizens.CommuteEpisodeEndDay[slot], outcome);
    }

    /// <summary>
    /// Assembles <c>02 §9</c>'s Lot answer — <em>why it is vacant</em>.
    /// </summary>
    /// <param name="world">The world to read.</param>
    /// <param name="lot">Which Lot.</param>
    /// <exception cref="ArgumentNullException"><paramref name="world"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The handle does not resolve.</exception>
    public static LotEvidence OfLot(World world, Handle<Lot> lot) => OfLot(world, lot, null);

    /// <summary>Reads every live Lot with one shared Household capital pass.</summary>
    public static LotEvidence[] OfLots(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        int richest = world.Rules.Lots.LandSales ? world.MaximumCapitalHousehold() : Rows.NoSlot;
        var evidence = new LotEvidence[world.Lots.Rows.LiveCount];
        int count = 0;
        for (int lot = 0; lot < world.Lots.Rows.SlotCount; lot++)
        {
            if (world.Lots.Rows.IsLive(lot))
            { evidence[count++] = OfLot(world, world.Lots.Rows.At(lot), richest); }
        }
        return evidence;
    }

    private static LotEvidence OfLot(World world, Handle<Lot> lot, int? capitalBuyer)
    {
        ArgumentNullException.ThrowIfNull(world);

        int slot = world.Lots.Rows.Resolve(lot);
        bool vacant = world.Lots.IsVacant(slot);

        VacancyReason reason = VacancyReason.None;

        if (vacant)
        {
            if (!world.Lots.HasFrontage(slot))
            {
                reason |= VacancyReason.NoFrontage;
            }

            if (world.UnplacedPool.Count == 0)
            {
                reason |= VacancyReason.NobodySeeking;
            }

            if (!AdmittedByAnyRule(world, slot))
            {
                reason |= VacancyReason.NotZoned;
            }

            if (NoConstructionCapital(world, slot, capitalBuyer))
            {
                reason |= VacancyReason.NoCapital;
            }
        }

        int building = world.Lots.BuildingOn(slot);

        // Resolved rather than copied: a severed handle means the Landowner died or left and the Lot
        // passed to the city, so it reads as the city here rather than as a Household that is gone.
        int landowner = world.Lots.LandownerOn(slot);

        return new LotEvidence(
            lot,
            world.LotPermissions(slot),
            ConstructionRefusal(world, slot),
            vacant,
            world.Lots.AddressOf(slot),
            reason,
            LastCondemnation(world, lot),
            building == Rows.NoSlot ? default : world.Buildings.Rows.At(building),
            landowner == Rows.NoSlot ? default : world.Households.Rows.At(landowner));
    }

    /// <summary>
    /// What a housed Household pays for its home and what it owes, or <see langword="null"/> where
    /// the city charges no rent.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Read here rather than off the columns, so the shell and a headless dump answer from one
    /// place</b> — <see cref="OfLot"/>'s Landowner clause and the same reason. The daily figure is
    /// <c>World.ResidentRent</c>, which is the charge and not the price, so a resident Landowner
    /// reads zero.
    /// </para>
    /// <para>
    /// ⚠ <b>Null where the Ruleset states no <c>[rent]</c>, and zero where it states one and this
    /// tenant owes nothing.</b> Those are different answers and <see cref="RentEvidence"/> carries
    /// why.
    /// </para>
    /// </remarks>
    /// <param name="world">The world to read.</param>
    /// <param name="household">Which Household.</param>
    /// <exception cref="ArgumentNullException"><paramref name="world"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The handle does not resolve.</exception>
    public static RentEvidence? RentOf(World world, Handle<Household> household)
    {
        ArgumentNullException.ThrowIfNull(world);

        int slot = world.Households.Rows.Resolve(household);

        return world.Rules.Rent.Collects
            ? new RentEvidence(
                world.ResidentRent(slot),
                world.Households.RentDebt[slot],
                world.Households.RentDebtSinceDay[slot])
            : null;
    }

    /// <inheritdoc cref="RentOf(World, Handle{Household})"/>
    /// <param name="world">The world to read.</param>
    /// <param name="business">Which Business.</param>
    public static RentEvidence? RentOf(World world, Handle<Business> business)
    {
        ArgumentNullException.ThrowIfNull(world);

        int slot = world.Businesses.Rows.Resolve(business);

        return world.Rules.Rent.Collects
            ? new RentEvidence(
                world.BusinessRent(slot),
                world.Businesses.RentDebt[slot],
                world.Businesses.RentDebtSinceDay[slot])
            : null;
    }

    /// <summary>
    /// One Rule Instance's state, with <c>02 §9</c>'s <em>last ran and whether it succeeded</em>
    /// derived from it.
    /// </summary>
    /// <remarks>
    /// <b>The derivation rests on a structural invariant rather than on a stored timestamp</b>: a Rule
    /// Instance is armed on the Wheel or asleep on a wait list, never both, and <c>EventWheel.Arm</c>
    /// is the only writer of <c>NextTick</c>. Armed means the last firing worked and re-armed at
    /// <c>+rate</c>; asleep means it did not, and left <c>NextTick</c> at the Tick it fired on. See
    /// <see cref="RuleEvidence"/> for the one case this cannot separate.
    /// </remarks>
    private static RuleEvidence ReadRule(World world, int instance, Ticks now)
    {
        RuleId id = world.RuleInstances.Rule[instance];
        uint rate = world.Rules.Rule(id).Rate;
        var blocked = (Blocking)world.RuleInstances.Blocked[instance];
        bool succeeded = blocked == Blocking.Nothing;
        Ticks due = world.RuleInstances.NextTick[instance];

        Ticks lastRan = succeeded && due.Raw >= rate
            ? new Ticks(due.Raw - rate)
            : due;

        Ticks starvedSince = world.RuleInstances.StarvedSince[instance];

        // adr/0053: pressure is in MISSED FIRINGS rather than in Ticks, so that a Ruleset which
        // retunes every rate does not silently retune every Building's lifespan. Zero rather than a
        // division by zero on a rate the loader should already have refused.
        //
        // ZoneRuleEngine.Condemn deliberately does NOT divide -- it cross-multiplies, because the
        // quotient is an answer it throws away. Here the quotient IS the answer, so the division is
        // spelled rather than avoided, and FloorDiv states the rounding (05 §4 lint 3). Both operands
        // are non-negative, so floor and truncation agree; naming the rounding is the point.
        long missed = starvedSince.Raw == 0 || rate == 0
            ? 0
            : IntegerMath.FloorDiv((long)(now.Raw - starvedSince.Raw), rate);

        // adr/0137: the wait list has always known which Bin, and this type did not. Resolved rather
        // than handed out as a handle, for BinEvidence.Resource's reason -- Evidence is the read the
        // shell may enumerate, and a handle it has to dereference against a live world is not one.
        //
        // TryResolve rather than Resolve, and the unset case is the ordinary one: a Rule that
        // SUCCEEDED is armed on the Event Wheel with WaitingOn cleared by World.Unlink, so most rows
        // in a healthy Building take this branch. A stale handle is the same answer for a different
        // reason and neither is worth a throw on a panel read.
        ResourceId waitingFor = default;
        BinOwnerKind waitingOn = BinOwnerKind.None;

        bool hasBin = world.Bins.Rows.TryResolve(world.RuleInstances.WaitingOn[instance], out int blocking);
        if (hasBin)
        {
            waitingFor = world.Bins.Resource[blocking];
            waitingOn = world.Bins.OwnerKind[blocking];
        }

        return new RuleEvidence(
            id,
            world.RuleInstances.Household[instance],
            lastRan,
            succeeded,
            blocked,
            world.RuleInstances.Reported[instance],
            starvedSince,
            rate,
            missed,
            waitingFor,
            waitingOn)
        {
            Business = world.RuleInstances.Business[instance],
            InstanceId = world.RuleInstances.Rows.IdAt(instance),
            WaitingBin = hasBin
                ? world.Bins.Rows.At(blocking) : default,
            WaitingLevel = hasBin ? world.Bins.LevelAt(blocking) : 0,
            WaitingCapacity = hasBin ? world.Bins.Capacity[blocking] : 0,
        };
    }

    private static bool NoConstructionCapital(World world, int lot, int? capitalBuyer)
    {
        if (!world.Rules.Lots.LandSales || world.HasStreets && !world.Lots.HasFrontage(lot)) { return false; }
        bool measured = false;
        foreach (ZoneRuleDefinition definition in world.Rules.ZoneRules)
        {
            if (!definition.ReadsDemand && world.UnplacedPool.Count == 0) { continue; }
            if (!definition.ReadsDemand && world.Rules.HousingConstruction is not null
                && world.Rules.Declares(definition.Kind) && world.Rules.Kind(definition.Kind).Houses)
            {
                if ((definition.Admits & LotTable.Housing) == 0) { continue; }
                Space.LocalLayoutProposal? proposal = HousingConstruction.Select(world, world.Key, world.Lots.Rows.At(lot), definition.Kind);
                if (proposal is null) { continue; }
                measured = true;
                Space.LocalLot door = proposal.Sources[0];
                int source = world.Lots.Rows.Resolve(door.Handle);
                Money land = world.LandPrice(new Tiles(door.East), new Tiles(door.North),
                    (long)proposal.Building.Footprint.Width * proposal.Building.Footprint.Height);
                if (world.ConstructionBuyer(source, world.Lots.LandownerOn(source), land,
                    world.ConstructionCharge(proposal.FloorTiles), world.Tick, draw: false, capitalBuyer) != Rows.NoSlot) { return false; }
            }
            else if (world.ConstructionPermission(lot, definition.Admits) == Space.PermissionRefusal.None)
            {
                measured = true;
                if (world.ConstructionBuyer(lot, world.Lots.LandownerOn(lot), world.LandPrice(lot),
                    world.ConstructionCharge(world.Lots.FloorTiles(lot)), world.Tick, draw: false, capitalBuyer) != Rows.NoSlot)
                { return false; }
            }
        }
        return measured;
    }

    private static bool AdmittedByAnyRule(World world, int lotSlot) =>
        ConstructionRefusal(world, lotSlot) == Space.PermissionRefusal.None;

    /// <summary>Uses the construction predicate, including band, form and complete ground coverage.</summary>
    private static Space.PermissionRefusal ConstructionRefusal(World world, int lotSlot)
    {
        Space.PermissionRefusal first = Space.PermissionRefusal.Use;
        bool seen = false;
        foreach (ZoneRuleDefinition definition in world.Rules.ZoneRules)
        {
            Space.PermissionRefusal refusal = world.ConstructionPermission(lotSlot, definition.Admits);
            if (refusal == Space.PermissionRefusal.None) { return refusal; }
            if (!seen) { first = refusal; seen = true; }
        }
        return first;
    }

    /// <summary>
    /// The most recent surviving trail entry for this Lot, if there is one.
    /// </summary>
    /// <remarks>
    /// <b>Walked backwards, because the trail is dense and chronological</b> — entries slide down
    /// rather than rotating through a cursor, so the last matching index is the most recent
    /// condemnation. Slot 0 is the aggregate and carries no identity, which is why the walk stops
    /// above it: ***attribution decays to magnitude***, and an entry that has aged out is
    /// indistinguishable from one that never happened.
    /// </remarks>
    private static CondemnationEvidence? LastCondemnation(World world, Handle<Lot> lot)
    {
        CondemnationTrailTable trail = world.CondemnationTrail;

        for (int index = trail.Count - 1; index >= 0; index--)
        {
            int slot = trail.EntrySlot(index);

            if (trail.Lot[slot].Equals(lot))
            {
                return new CondemnationEvidence(
                    trail.Tick[slot], trail.Kind[slot], trail.Condition[slot]);
            }
        }

        return null;
    }

    /// <summary>
    /// The Trip this Citizen is on, found by scanning the Travellers.
    /// </summary>
    /// <remarks>
    /// <b>A scan rather than an index, and deliberately.</b> A reverse Citizen-to-Traveller column
    /// would be state the simulation never reads, maintained every Tick for a question asked on a
    /// click — which is the hot path paying for the cold one, and the wrong way round. The scan is
    /// bounded by the Travellers in flight, not by the population.
    /// </remarks>
    private static TripEvidence? InFlightTrip(World world, Handle<Citizen> citizen)
    {
        TravellerTable travellers = world.Travellers;

        for (int slot = 0; slot < travellers.Rows.SlotCount; slot++)
        {
            if (!travellers.Rows.IsLive(slot) || !travellers.Citizen[slot].Equals(citizen))
            {
                continue;
            }

            if (!world.Trips.Rows.TryResolve(travellers.Trip[slot], out int trip))
            {
                continue;
            }

            return new TripEvidence(
                travellers.Trip[slot],
                (TripPurpose)world.Trips.Purpose[trip],
                (TripFate)world.Trips.Fate[trip],
                travellers.ArrivesAt[slot]);
        }

        return null;
    }
}
