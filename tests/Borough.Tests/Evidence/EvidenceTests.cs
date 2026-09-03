using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Evidence;
using Borough.Core.Input;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Golden;

namespace Borough.Tests.Evidence;

/// <summary>
/// Milestone 6 task 4: <c>02 §9</c>'s question surface, assembled on a click from live state and one
/// trail.
/// </summary>
/// <remarks>
/// <para>
/// <b>What is under test is agreement, not output.</b> An assembler has no behaviour of its own — every
/// number it returns already exists somewhere in the world — so a test that reads it and checks the
/// value is plausible is testing nothing. Every test here reads the <em>same fact by a second route</em>
/// and demands the two match: the intrusive lists the simulation itself walks, the trail table's own
/// columns, the Traveller table, and in one case the behaviour of the Zone Rule whose predicate the
/// assembler copies.
/// </para>
/// <para>
/// <b>Two of them are the ones worth having</b>, because they are the two places the assembler does
/// something rather than forwarding something.
/// <see cref="Pressure_is_the_longest_of_its_rules_and_not_their_sum"/> holds the maximum
/// <c>adr/0053</c> specifies and nothing has ever stored, and it runs the fixture in both declaration
/// orders for <c>CondemnationCauseTests</c>' reason — a mechanism picking the first Rule it met would
/// pass one of them. <see cref="A_lot_the_assembler_calls_unzoned_is_a_lot_nothing_builds_on"/> holds
/// the copied predicate against the engine it was copied from, which is the only guard against
/// <c>plans/0012</c> <em>Cause 1</em> here: <b>a copied clause is a second copy of a fact</b>, and the
/// two copies drift silently because both compile.
/// </para>
/// <para>
/// ⚠ <b>The last test is structural rather than about any one answer.</b>
/// <see cref="Assembling_every_answer_in_the_world_moves_no_state"/> is
/// <see cref="ColdPathAttribute"/>'s claim made mechanical — <em>no code path from <c>step()</c>
/// reaches it</em> has a sibling that a test can actually check, which is that nothing reaches
/// <c>step()</c> <em>from</em> it. It is the one test here that would survive every other file being
/// rewritten.
/// </para>
/// </remarks>
public sealed class EvidenceTests
{
    /// <summary>Half a Day. Long enough to employ, commute, build and condemn.</summary>
    private const int RunTicks = 1_024;

    private const int HashEvery = 1_024;

    /// <summary>
    /// One in sixteen, where a test walks Citizens and calls the assembler on each.
    /// </summary>
    /// <remarks>
    /// <see cref="Evidence.OfCitizen"/> scans the Travellers, deliberately and for the reason that
    /// method gives, so calling it four thousand times is quadratic in a way nothing in production is.
    /// A stride rather than a prefix, because the population is laid out in creation order and a prefix
    /// is the oldest sixteenth of it.
    /// </remarks>
    private const int Stride = 16;

    // ---- Building -------------------------------------------------------------------------------

    /// <summary>
    /// <b>A Building's answer is the four intrusive lists, read back.</b>
    /// </summary>
    /// <remarks>
    /// The second route is <c>World</c>'s own list walkers, which is what every mechanism in the
    /// simulation uses. Order is asserted as well as membership: the lists are intrusive and
    /// <em>head-first</em>, so a copy that reversed them would still hold the right set and would put
    /// the wrong Household at the top of a panel.
    /// </remarks>
    [Fact]
    public void A_buildings_answer_matches_the_lists_the_simulation_itself_walks()
    {
        World world = Golden();

        int seen = 0;

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (!world.Buildings.Rows.IsLive(slot))
            {
                continue;
            }

            Handle<Building> handle = world.Buildings.Rows.At(slot);
            BuildingEvidence evidence = Core.Evidence.Evidence.OfBuilding(world, handle);

            Assert.Equal(handle, evidence.Building);
            Assert.Equal(world.Buildings.Kind[slot], evidence.Kind);
            Assert.Equal(world.Buildings.Lot[slot], evidence.Lot);

            int at = 0;

            foreach (int household in world.Occupants.Walk(slot))
            {
                Assert.Equal(world.Households.Rows.At(household), evidence.Occupants.Span[at++]);
            }

            Assert.Equal(at, evidence.Occupants.Length);

            at = 0;

            // 🔴 The tenants' worker lists, NOT the Building's, matching Evidence.OfBuilding.
            //
            // ⚠ THIS PREDATES MILESTONE 17 AND IS FIXED HERE BECAUSE 17 IS WHAT MADE IT FAIL.
            // `Workers` has been indexed by BUSINESS slot since milestone 27 task 7, and this test
            // walked it at a BUILDING slot — so it asserted production was wrong and passed anyway,
            // because adr/0148 instantiates a Business with every dwelling and the two tables were
            // the same width. Abandonment made Buildings outlive their Businesses, the widths
            // diverged, and the stale walk read a different list.
            //
            // ***A second route that happens to agree is not a second route.*** The whole point of
            // this test is that the assembler and the simulation walk the same lists; keying them
            // differently and getting away with it is the failure it exists to catch, arriving in
            // the test itself.
            foreach (int tenant in world.BuildingBusinesses.Walk(slot))
            {
                foreach (int worker in world.Workers.Walk(tenant))
                {
                    Assert.Equal(world.Citizens.Rows.At(worker), evidence.Workers.Span[at++]);
                }
            }

            Assert.Equal(at, evidence.Workers.Length);

            at = 0;

            // The premises' Bins come first, in the Building's own list order, and each carries the
            // unset tenant handle.
            foreach (int bin in world.BuildingBins.Walk(slot))
            {
                BinEvidence found = evidence.Bins.Span[at++];

                Assert.Equal(world.Bins.Resource[bin], found.Resource);
                Assert.Equal(world.Bins.LevelAt(bin), found.Level);
                Assert.Equal(world.Bins.Capacity[bin], found.Capacity);
                Assert.True(found.Tenant.IsNone, "a premises Bin named a tenant.");
            }

            // ⚠ THEN EVERY TENANT'S, in occupant order (adr/0141, milestone 25 task 4). A tenant's Bin
            // is in no Building's list, so a panel assembled from BuildingBins alone printed Rules
            // drawing from Bins it did not show. The BALANCE is skipped -- it is money, it is
            // unbounded, and the Household finances panel is where a reader meets it.
            foreach (int household in world.Occupants.Walk(slot))
            {
                Handle<Bin> owned = world.Households.BinHead[household];

                while (!owned.IsNone)
                {
                    int bin = world.Bins.Rows.Resolve(owned);

                    if (!world.Rules.IsConserved(world.Bins.Resource[bin]))
                    {
                        BinEvidence found = evidence.Bins.Span[at++];

                        Assert.Equal(world.Bins.Resource[bin], found.Resource);
                        Assert.Equal(world.Bins.LevelAt(bin), found.Level);
                        Assert.Equal(world.Bins.Capacity[bin], found.Capacity);
                        Assert.Equal(world.Households.Rows.At(household), found.Tenant);
                    }

                    owned = world.Bins.OwnerNext[bin];
                }
            }

            Assert.Equal(at, evidence.Bins.Length);

            at = 0;

            foreach (int instance in world.BuildingRules.Walk(slot))
            {
                RuleEvidence found = evidence.Rules.Span[at++];

                Assert.Equal(world.RuleInstances.Rule[instance], found.Rule);
                Assert.Equal(world.RuleInstances.Household[instance], found.Tenant);
                Assert.Equal(world.RuleInstances.StarvedSince[instance], found.StarvedSince);
                Assert.Equal(world.RuleInstances.Reported[instance], found.Reported);
            }

            Assert.Equal(at, evidence.Rules.Length);

            seen++;
        }

        Assert.True(seen > 0, "the fixture holds no Buildings, so nothing was compared.");
    }

    /// <summary>
    /// <b>A Building's pressure is the longest of its Rules', and this is the only thing that computes
    /// it.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>adr/0053</c> as amended, and milestone 6 task 2's finding: <c>ZoneRuleEngine</c> says the
    /// maximum <em>"is never stored anywhere"</em>, so there is no column to compare against and the
    /// fixture has to make the three wrong answers distinguishable. Two Rules starving at rates 4 and
    /// 16 do that — after <c>T</c> Ticks of silence the fast one has missed four times the slow one's
    /// firings, so the <b>maximum</b>, the <b>sum</b>, the <b>minimum</b> and the <b>first declared</b>
    /// are four different numbers.
    /// </para>
    /// <para>
    /// <b>Both declaration orders, for <c>CondemnationCauseTests</c>' reason.</b> A Building's Rule
    /// list is built in the kind's declared order, so an implementation that took the first Rule it met
    /// would pass exactly one of the two runs — which is the failure a single-order test cannot see.
    /// </para>
    /// <para>
    /// <b>Only a <em>Supply</em> failure starts the clock</b> (<c>RuleEngine.Stop</c>): a Rule out of
    /// <em>space</em> is a well-supplied Building and has its clock cleared. So both Bins here are
    /// filled by nothing, which is the one shape that starves for ever — a failed Rule sleeps on the
    /// Bin that stopped it, and a Bin no Rule writes is a Rule that never wakes.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Pressure_is_the_longest_of_its_rules_and_not_their_sum(bool fastFirst)
    {
        (World world, Simulation simulation, Handle<Building> building) = Starving(fastFirst);

        for (int i = 0; i < 256; i++)
        {
            simulation.Step(TickInput.Empty);
        }

        BuildingEvidence evidence = Core.Evidence.Evidence.OfBuilding(world, building);
        RuleEvidence[] rules = evidence.Rules.ToArray();

        Assert.Equal(2, rules.Length);
        Assert.All(rules, rule => Assert.False(rule.Succeeded));
        Assert.All(rules, rule => Assert.NotEqual(default, rule.StarvedSince));

        long fast = rules.Single(rule => rule.Rate == FastRate).MissedFirings;
        long slow = rules.Single(rule => rule.Rate == SlowRate).MissedFirings;

        Assert.True(
            fast > slow && slow > 0,
            $"the fixture stopped separating its two Rules — fast {fast}, slow {slow} — so the "
            + "maximum and the minimum are no longer different answers and this test discriminates "
            + "nothing.");

        Assert.Equal(fast, evidence.Pressure);

        // Stated as its own assertion rather than left implied by the one above, because it is the
        // wrong answer with the strongest pull: summing is what a walk accumulating into one variable
        // does by default, and it is right whenever exactly one Rule starves -- which is every
        // Building in every shipped Ruleset.
        Assert.NotEqual(fast + slow, evidence.Pressure);
    }

    /// <summary>
    /// <b>A Rule's last firing is recovered from which side of the Wheel it is on, and it is never in
    /// the future.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>02 §9</c> asks which Rule a Building last ran and whether it succeeded, and no column holds
    /// either. <see cref="RuleEvidence.LastRan"/> derives both from the invariant that a Rule Instance
    /// is armed <em>or</em> asleep and never both: an armed one re-armed at <c>+rate</c> after a
    /// firing that worked, and a sleeping one left <c>NextTick</c> at the Tick it failed on.
    /// </para>
    /// <para>
    /// <b>The assertion is the discriminating half.</b> Returning <c>NextTick</c> bare — the obvious
    /// implementation, and the one a reader of the column name would write — puts every healthy Rule's
    /// last firing <em>after</em> the current Tick, which is the one thing a timestamp cannot be. The
    /// weaker half is asserted too: an armed Rule must be exactly <c>rate</c> Ticks behind its due
    /// Tick, so the derivation is checked rather than merely bounded.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_rules_last_firing_is_recovered_and_is_never_in_the_future()
    {
        World world = Golden();
        Ticks now = world.Tick;

        int armed = 0;
        int asleep = 0;

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (!world.Buildings.Rows.IsLive(slot))
            {
                continue;
            }

            BuildingEvidence evidence =
                Core.Evidence.Evidence.OfBuilding(world, world.Buildings.Rows.At(slot));

            foreach (RuleEvidence rule in evidence.Rules.ToArray())
            {
                Assert.True(
                    rule.LastRan.Raw <= now.Raw,
                    $"a Rule reports last running at {rule.LastRan.Raw} on Tick {now.Raw}. That is "
                    + "NextTick being returned bare: an armed Rule's next firing is in the future and "
                    + "its last one cannot be.");

                if (rule.Succeeded)
                {
                    armed++;
                }
                else
                {
                    asleep++;
                }
            }
        }

        Assert.True(armed > 0, "no Rule in the fixture is armed, so the healthy branch is untested.");
        Assert.True(asleep > 0, "no Rule in the fixture is asleep, so the failed branch is untested.");
    }

    // ---- Lot ------------------------------------------------------------------------------------

    /// <summary>
    /// <b>The copied zone predicate agrees with the engine it was copied from.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>This is the only test here that guards a second copy of a fact.</b>
    /// <c>ZoneRuleEngine.Create</c> is private and mutates, so there is nothing for the assembler to
    /// call and its admission clause is re-expressed rather than shared — which is <c>plans/0012</c>
    /// <em>Cause 1</em> written on purpose, with a test standing in for the shared symbol. Both copies
    /// compile whatever they say, so a test asserting <em>behaviour</em> is the only instrument that
    /// can separate them.
    /// </para>
    /// <para>
    /// <b>The claim is behavioural and runs both ways here</b>: a Lot the assembler calls unzoned is
    /// one the engine never builds on, <em>and</em> a Lot it does not is one the engine does. The
    /// second half is only assertable because the fixture is hand-built with a fast survey — on a
    /// generated city a zoned vacant Lot that stays vacant is the ordinary state of most of the map,
    /// since the Zone Rule samples rather than sweeps (<c>adr/0059</c>), so <em>not yet looked at</em>
    /// would explain it and prove nothing.
    /// </para>
    /// <para>
    /// ⚠ <b>It has to be hand-built for a second reason, and that reason is a finding.</b> The golden
    /// fixture cannot exercise this clause at all: <c>SyntheticCity</c> paints bit 0 on every Lot and
    /// the shipped <c>[[zone_rule]]</c> admits bit 0, so <b>every vacant Lot in every world this
    /// project generates is admitted</b> — measured, 2026-08-17, across the whole run. The flag is
    /// reachable only in a city somebody zoned, which is to say only under <c>CommandKind.Zone</c>.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_lot_the_assembler_calls_unzoned_is_a_lot_nothing_builds_on()
    {
        (World world, Simulation simulation, Handle<Lot>[] admitted, Handle<Lot>[] refused) =
            Zoning();

        foreach (Handle<Lot> lot in refused)
        {
            Assert.True(
                Core.Evidence.Evidence.OfLot(world, lot).Reason.HasFlag(VacancyReason.NotZoned),
                "a Lot carrying a bit no Zone Rule admits is not being reported as unzoned.");
        }

        foreach (Handle<Lot> lot in admitted)
        {
            Assert.False(
                Core.Evidence.Evidence.OfLot(world, lot).Reason.HasFlag(VacancyReason.NotZoned),
                "a Lot carrying the admitted bit is being reported as unzoned, so the copied clause "
                + "has the sense of ZoneRuleEngine.Create's test inverted.");
        }

        for (int i = 0; i < 512; i++)
        {
            simulation.Step(TickInput.Empty);
        }

        Assert.Contains(
            admitted,
            lot => !world.Lots.IsVacant(world.Lots.Rows.Resolve(lot)));

        foreach (Handle<Lot> lot in refused)
        {
            Assert.True(
                world.Lots.IsVacant(world.Lots.Rows.Resolve(lot)),
                "the assembler called this Lot unzoned and the Zone Rule then built on it, so the "
                + "clause copied out of ZoneRuleEngine.Create no longer says what that method says.");
        }
    }

    /// <summary>
    /// <b>A Lot with no frontage says so, and has no Address to give.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The two are one fact rather than two — an Address is <c>(Segment, offset, side)</c> and a Lot
    /// touching no Street has no Segment — so asserting them together is what would catch a frontage
    /// flag read off one column and an Address read off another.
    /// </para>
    /// <para>
    /// ⚠ <b>Two worlds, because no one world holds both branches.</b> Every Lot in a generated city
    /// has frontage and always will: <c>RoadGenerator</c> lays the lattice the subdivider then carves
    /// the Lots out of, so frontage is not a property the generated world varies — measured across the
    /// whole golden run, <b>0 of 150 vacant Lots</b> lack it. A Lot with none is one
    /// <c>LotTable.Create</c> made directly, which is what a hand-built fixture is. The generated
    /// world is kept for the negative half rather than dropped, because the flag being <em>absent</em>
    /// everywhere it should be absent is the half a one-sided test would lose.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_lot_with_no_frontage_says_so_and_has_no_address()
    {
        World generated = Golden();
        int fronted = 0;

        for (int slot = 0; slot < generated.Lots.Rows.SlotCount; slot++)
        {
            if (!generated.Lots.Rows.IsLive(slot) || !generated.Lots.IsVacant(slot))
            {
                continue;
            }

            Assert.True(generated.Lots.HasFrontage(slot));

            LotEvidence evidence =
                Core.Evidence.Evidence.OfLot(generated, generated.Lots.Rows.At(slot));

            Assert.False(evidence.Reason.HasFlag(VacancyReason.NoFrontage));
            Assert.NotEqual(Address.None, evidence.Address);

            fronted++;
        }

        Assert.True(fronted > 0, "the generated fixture holds no vacant Lots, so nothing was checked.");

        (World bare, _, Handle<Lot>[] admitted, _) = Zoning();

        foreach (Handle<Lot> lot in admitted)
        {
            LotEvidence evidence = Core.Evidence.Evidence.OfLot(bare, lot);

            Assert.True(evidence.Reason.HasFlag(VacancyReason.NoFrontage));
            Assert.Equal(Address.None, evidence.Address);
        }
    }

    /// <summary>
    /// <b>A Lot the trail remembers a demolition on reports it, with the condition that condemned it.</b>
    /// </summary>
    /// <remarks>
    /// The second route is the trail's own columns, walked directly. What this catches that a value
    /// check would not is the <em>direction</em> of the walk: the trail is dense and chronological, so
    /// a Lot demolished twice has two entries and the assembler must return the later one. Asserting
    /// against the last matching index rather than the first is the whole of it.
    /// </remarks>
    [Fact]
    public void A_demolished_lot_carries_the_condition_that_condemned_it()
    {
        World world = Golden();
        CondemnationTrailTable trail = world.CondemnationTrail;

        Assert.True(trail.Count > 0, "the fixture demolished nothing, so the trail is empty.");

        int checkedLots = 0;

        for (int index = 0; index < trail.Count; index++)
        {
            int entry = trail.EntrySlot(index);
            Handle<Lot> lot = trail.Lot[entry];

            if (!world.Lots.Rows.TryResolve(lot, out _))
            {
                continue;
            }

            CondemnationEvidence? found = Core.Evidence.Evidence.OfLot(world, lot).Condemnation;

            Assert.NotNull(found);

            int latest = Latest(trail, lot);

            Assert.Equal(trail.Tick[latest], found!.Value.Tick);
            Assert.Equal(trail.Kind[latest], found.Value.Kind);
            Assert.Equal(trail.Condition[latest], found.Value.Condition);

            checkedLots++;
        }

        Assert.True(checkedLots > 0, "every trail entry names a Lot that no longer exists.");
    }

    // ---- Citizen --------------------------------------------------------------------------------

    /// <summary>
    /// <b>A Citizen's workplace is the one the simulation believes in, which is not the one the column
    /// holds.</b>
    /// </summary>
    /// <remarks>
    /// <c>CitizenTable.Workplace</c> is <c>Reference.Severable</c>: a handle to a demolished Building
    /// stays in the column and stops resolving, and that <em>is</em> the job having ceased to exist.
    /// The assembler reports the unset handle for it, so the second route here is
    /// <c>Rows.IsValid</c> — and the test is worth having because the naive passthrough returns a
    /// handle a caller would then fail to resolve.
    /// </remarks>
    [Fact]
    public void A_citizens_answer_reports_the_workplace_the_simulation_believes_in()
    {
        World world = Golden();

        int employed = 0;
        int severed = 0;

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot += Stride)
        {
            if (!world.Citizens.Rows.IsLive(slot))
            {
                continue;
            }

            Handle<Business> held = world.Citizens.Workplace[slot];
            CitizenEvidence evidence =
                Core.Evidence.Evidence.OfCitizen(world, world.Citizens.Rows.At(slot));

            Assert.Equal(world.Citizens.ReachFailures[slot], evidence.ReachFailures);
            Assert.Equal(world.Citizens.HouseholdOf[slot], evidence.Household);

            if (world.Businesses.Rows.IsValid(held))
            {
                Assert.Equal(held, evidence.Workplace);
                employed++;
            }
            else
            {
                Assert.Equal(default, evidence.Workplace);

                if (!held.Equals(default))
                {
                    severed++;
                }
            }
        }

        Assert.True(employed > 0, "nobody in the sample works, so the resolving branch is untested.");
        Assert.True(
            severed > 0,
            "nobody in the sample holds a workplace handle that has stopped resolving, so the "
            + "severable branch -- the only one with a mechanism in it -- was never reached.");
    }

    /// <summary>
    /// <b>A Citizen in flight is found and one at rest is not.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The second route is the Traveller table, scanned by the test rather than by the assembler. What
    /// this holds is the pairing: the assembler must return a Trip for exactly the Citizens the
    /// Travellers name, so both a missed Traveller and a Trip attributed to the wrong person fail it.
    /// </para>
    /// <para>
    /// ⚠ <b>The moment is found rather than chosen, and that is forced.</b> Being in flight is a
    /// spike and not a level — measured over a whole Day at Tick 32 intervals, the in-flight count runs
    /// <c>0, 0, 1, 101, 2, 0, 18, 64, 0, 0</c>, so <b>most Ticks have nobody travelling at all</b> and
    /// the commonest sample is empty. That is <c>adr/0101</c>'s Day working: a Shift start band puts
    /// the departures in a narrow window and a commute is 1.39% of a Day, so a fixed Tick would be a
    /// test that passed or failed on where the window happened to be. Stepping until somebody is in
    /// flight makes it a statement about the pairing rather than about the Day's shape, which is what
    /// it is supposed to be about.
    /// </para>
    /// <para>
    /// <b>The travelling half iterates the Travellers and the resting half samples the population</b>,
    /// rather than one stride over both. A hundred people in flight out of four thousand means a
    /// one-in-sixteen sample catches about six of them on a good draw and none on a bad one, so the
    /// stride would decide whether the branch ran.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_traveller_in_flight_is_found_and_a_citizen_at_rest_is_not()
    {
        Simulation simulation = Start(GoldenFixtures.Rules(), out InputLog log);

        Replay.Trace(simulation, log, new Ticks(RunTicks), HashEvery, []);

        World world = simulation.World;

        // The Ruleset in force, restated every Tick, because a TickInput naming a different one is a
        // RELOAD -- and TickInput.Empty names hash zero, so stepping a replayed session with it asks
        // for a Ruleset no catalogue holds. The hand-built fixtures elsewhere in this file get away
        // with TickInput.Empty only because zero is also the hash they opened on.
        var carrying = new TickInput([], GoldenFixtures.RulesetHash);

        for (int i = 0; i < 2 * RunTicks && world.Travellers.Rows.LiveCount == 0; i++)
        {
            simulation.Step(carrying);
        }

        HashSet<Handle<Citizen>> travelling = [];

        for (int slot = 0; slot < world.Travellers.Rows.SlotCount; slot++)
        {
            if (world.Travellers.Rows.IsLive(slot))
            {
                travelling.Add(world.Travellers.Citizen[slot]);
            }
        }

        Assert.True(
            travelling.Count > 0,
            "a whole Day of stepping put nobody on the road, so the Trip branch is untested. The "
            + "commute generator or the Shift band has stopped producing departures.");

        foreach (Handle<Citizen> citizen in travelling)
        {
            TripEvidence? trip = Core.Evidence.Evidence.OfCitizen(world, citizen).Trip;

            Assert.NotNull(trip);
            Assert.Equal(TripPurpose.Commute, trip!.Value.Purpose);
        }

        int resting = 0;

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot += Stride)
        {
            if (!world.Citizens.Rows.IsLive(slot))
            {
                continue;
            }

            Handle<Citizen> citizen = world.Citizens.Rows.At(slot);

            if (travelling.Contains(citizen))
            {
                continue;
            }

            Assert.Null(Core.Evidence.Evidence.OfCitizen(world, citizen).Trip);

            resting++;
        }

        Assert.True(resting > 0, "the sample caught nobody at rest.");
    }

    // ---- Structural -----------------------------------------------------------------------------

    /// <summary>
    /// <b>Assembling every answer in the world changes nothing about the world.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b><see cref="ColdPathAttribute"/>'s claim, made mechanical from the other side.</b> That
    /// attribute records <em>no code path from <c>step()</c> reaches it</em>, which nothing can check
    /// without a call graph. Its sibling can be checked exactly: <b>no path from the assembler reaches
    /// state</b>. The State Hash is the instrument the project already has for that, and it is the
    /// strictest one available — it folds every saved column of every table, so any write anywhere
    /// fails this.
    /// </para>
    /// <para>
    /// <b>It is not a hypothetical.</b> The assembler re-runs predicates and walks lists, and both of
    /// those things live next door to methods that mutate: <c>ZoneRuleEngine.Create</c> raises a
    /// Building, and the Bin level is behind <c>LevelAt</c> precisely so a read cannot forget to wake
    /// anybody. An assembler that reached for the convenient method rather than the pure one would be
    /// a determinism defect that no other test in this file could see.
    /// </para>
    /// </remarks>
    [Fact]
    public void Assembling_every_answer_in_the_world_moves_no_state()
    {
        World world = Golden();
        ulong before = world.HashState();

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot))
            {
                _ = Core.Evidence.Evidence.OfBuilding(world, world.Buildings.Rows.At(slot));
            }
        }

        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (world.Lots.Rows.IsLive(slot))
            {
                _ = Core.Evidence.Evidence.OfLot(world, world.Lots.Rows.At(slot));
            }
        }

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot += Stride)
        {
            if (world.Citizens.Rows.IsLive(slot))
            {
                _ = Core.Evidence.Evidence.OfCitizen(world, world.Citizens.Rows.At(slot));
            }
        }

        Assert.Equal(before, world.HashState());
    }

    // ---- Fixtures -------------------------------------------------------------------------------

    /// <summary>The last trail slot naming <paramref name="lot"/>, which is the most recent entry.</summary>
    private static int Latest(CondemnationTrailTable trail, Handle<Lot> lot)
    {
        for (int index = trail.Count - 1; index >= 0; index--)
        {
            if (trail.Lot[trail.EntrySlot(index)].Equals(lot))
            {
                return trail.EntrySlot(index);
            }
        }

        throw new InvalidOperationException("the trail holds no entry for a Lot it was asked about.");
    }

    /// <summary>The golden fixture with decline in it, run four Days.</summary>
    /// <remarks>
    /// 🔴 <b><c>minimal.toml</c> for half a Day until milestone 17, and both callers need a Building
    /// to have FALLEN DOWN.</b> One reads the condemnation trail and one wants a Citizen whose
    /// workplace handle has stopped resolving; neither exists in a city where nothing is ever
    /// demolished, and <c>adr/0164</c> made <c>minimal.toml</c> that city.
    /// <para>
    /// ⚠ <b>A local fixture rather than a change to <c>RunTicks</c>.</b> That constant is shared with
    /// <c>A_traveller_in_flight_is_found_and_a_citizen_at_rest_is_not</c>, which needs somebody in
    /// flight and not a demolition — raising it there would multiply a passing test's cost eightfold
    /// to buy it nothing.
    /// </para>
    /// <para>
    /// ⚠ <b>8,192 Ticks is derived</b>: <c>declining.toml</c> condemns on a 2-Day threshold and
    /// collapses a Day later, so nothing is demolished before 6,144 Ticks. The guard is off because
    /// it, and not the length, is what a long replay costs — see <c>SeriesReportTests.Report</c>.
    /// </para>
    /// </remarks>
    private static World Golden()
    {
        InputLogBuilder builder = new(
            GoldenFixtures.Seed,
            new WorldConfiguration(GoldenFixtures.Population),
            RulesetFile.HashOf(GoldenFixtures.DecliningRulesetPath));

        builder.Append(new Ticks(0), new Command(CommandKind.Populate, default, default));

        InputLog log = builder.Build();
        Simulation simulation = Replay.Start(log, GoldenFixtures.DecliningRules());

        simulation.VerifyDecideWritesNothing = false;

        Replay.Trace(simulation, log, new Ticks(DeclineTicks), HashEvery, []);

        return simulation.World;
    }

    /// <summary>How far <see cref="Golden"/> runs: past a condemnation and its collapse.</summary>
    private const int DeclineTicks = 8_192;

    private static Simulation Start(Ruleset rules, out InputLog log)
    {
        InputLogBuilder builder = new(
            GoldenFixtures.Seed,
            new WorldConfiguration(GoldenFixtures.Population),
            GoldenFixtures.RulesetHash);

        builder.Append(new Ticks(0), new Command(CommandKind.Populate, default, default));

        log = builder.Build();

        return Replay.Start(log, rules);
    }

    private const byte House = 1;
    private const ushort Housing = 1;

    /// <summary>A permission set naming a kind no Zone Rule in <see cref="Zoning"/> builds.</summary>
    private const ushort Elsewhere = 1 << 1;

    /// <summary>
    /// A world of Lots painted two ways, with one Zone Rule that admits only the first.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>ZoneRuleCreateTests.Built</c>'s fixture with the zone bit split. Households are put in the
    /// Pool through Buildings that stay standing, so the Pool is non-empty throughout and
    /// <see cref="VacancyReason.NobodySeeking"/> never fires — which is what leaves
    /// <see cref="VacancyReason.NotZoned"/> as the only flag the zoning half of the test can be
    /// reading.
    /// </para>
    /// <para>
    /// <b>The survey is the fastest a Ruleset can legally author</b> (<c>adr/0059</c> refuses shorter),
    /// so over 512 Ticks the sampler reaches every Lot many times and <em>not yet looked at</em> stops
    /// being an available explanation for a Lot staying vacant. That is what makes the positive half —
    /// an admitted Lot actually being built on — assertable at all.
    /// </para>
    /// </remarks>
    private static (
        World World,
        Simulation Simulation,
        Handle<Lot>[] Admitted,
        Handle<Lot>[] Refused) Zoning()
    {
        Ruleset ruleset = new(
            resources: [],
            rules: [],
            kinds: [new KindDefinition(0, 0, 0, 0) { Houses = 1 > 0 , Premises = 1 > 0 }],
            inputs: [],
            outputs: [],
            emissions: [],
            bins: [],
            kindRules: [],
            zoneRules: [new ZoneRuleDefinition(House, 0, 4, 4)]);

        var world = new World(1_000, ruleset);
        var simulation = new Simulation(world, WorldKey.FromSeed(0xE71D_E0CE_0000_0005UL));

        for (int i = 0; i < 4; i++)
        {
            Handle<Lot> lot = world.Lots.Create(new Tiles(i), new Tiles(0), Housing);
            Handle<Building> building = world.CreateBuilding(lot, House, Ticks.Zero, simulation.Key);

            world.Unplace(world.CreateHousehold(building, lifeStage: 0));
        }

        Handle<Lot>[] admitted = new Handle<Lot>[8];
        Handle<Lot>[] refused = new Handle<Lot>[8];

        for (int i = 0; i < 8; i++)
        {
            admitted[i] = world.Lots.Create(new Tiles(i), new Tiles(1), Housing);
            refused[i] = world.Lots.Create(new Tiles(i), new Tiles(2), Elsewhere);
        }

        return (world, simulation, admitted, refused);
    }

    private const uint SlowRate = 16;
    private const uint FastRate = 4;
    private static readonly ResourceId Repairs = new(1);
    private static readonly ResourceId Parts = new(2);

    /// <summary>
    /// One Building whose two Rules both starve, at rates four Ticks apart, and which nothing condemns.
    /// </summary>
    /// <remarks>
    /// <c>CondemnationCauseTests.Failing</c>'s fixture with the Zone Rule removed and no <c>on_fail</c>
    /// chains, because what is under test here is the pressure rather than what a demolition records —
    /// and a Building that falls down is one there is nothing left to ask about.
    /// </remarks>
    private static (World World, Simulation Simulation, Handle<Building> Building) Starving(
        bool fastFirst)
    {
        Ruleset ruleset = new(
            resources: [ResourceFamily.Good, ResourceFamily.Good],
            rules:
            [
                new RuleDefinition(House, SlowRate, ApplyCount.Band(1, 1), RuleId.None,
                    false, default, ConditionId.None, 0, 1, 0, 0, 0, 0),
                new RuleDefinition(House, FastRate, ApplyCount.Band(1, 1), RuleId.None,
                    false, default, ConditionId.None, 1, 1, 0, 0, 0, 0),
            ],
            kinds: [new KindDefinition(0, 2, 0, 2) { Houses = 1 > 0 , Premises = 1 > 0 }],
            inputs:
            [
                new Term(new BinRef(Scope.Local, Repairs), 1),
                new Term(new BinRef(Scope.Local, Parts), 1),
            ],
            outputs: [],
            emissions: [],
            bins:
            [
                new BinDeclaration(Repairs, BinCapacity.Of(4)),
                new BinDeclaration(Parts, BinCapacity.Of(4)),
            ],
            kindRules: fastFirst ? [new RuleId(2), new RuleId(1)] : [new RuleId(1), new RuleId(2)],
            zoneRules: []);

        var world = new World(1_000, ruleset);
        var simulation = new Simulation(world, WorldKey.FromSeed(0xE71D_E0CE_0000_0004UL));

        Handle<Lot> lot = world.Lots.Create(new Tiles(0), new Tiles(0), Housing);
        Handle<Building> building = world.CreateBuilding(lot, House, Ticks.Zero, simulation.Key);

        world.CreateHousehold(building, lifeStage: 0);

        return (world, simulation, building);
    }
}
