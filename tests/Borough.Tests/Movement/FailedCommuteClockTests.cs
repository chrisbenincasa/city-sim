using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Evidence;
using Borough.Core.Input;
using Borough.Core.Movement;
using Borough.Core.Persistence;
using Borough.Tests.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Tables;
using Borough.Formats;

using static Borough.Tests.Movement.BridgeCity;

namespace Borough.Tests.Movement;

/// <summary>
/// A run of failed outbound commutes is timed on the Citizen and costs the job at the
/// <c>[jobs] dismissed_after_failed_commute_days</c> deadline (<c>adr/0177</c>).
/// </summary>
public sealed class FailedCommuteClockTests
{
    private const int DeadlineDays = 2;

    [Fact]
    public void A_failed_outbound_commute_starts_the_clock_at_its_tick()
    {
        (Simulation simulation, List<Handle<Citizen>> crossers) = Severed(DeadlineDays);
        World world = simulation.World;

        StepTo(simulation, 3 * Ticks.PerDay);

        foreach (int citizen in Live(world, crossers))
        {
            Ticks since = world.Citizens.CommuteFailedSince[citizen];

            Assert.InRange((long)since.Raw - 1, 2 * Ticks.PerDay, (3 * Ticks.PerDay) - 1);
            Assert.True(Crosses(world, citizen), $"Citizen {citizen} lost the job before the deadline.");
        }
    }

    [Fact]
    public void The_deadline_dismisses_every_worker_the_bridge_cut_off()
    {
        (Simulation simulation, List<Handle<Citizen>> crossers) = Severed(DeadlineDays);
        World world = simulation.World;

        StepTo(simulation, (2 + DeadlineDays + 2) * Ticks.PerDay);

        foreach (int citizen in Live(world, crossers))
        {
            Assert.False(Crosses(world, citizen), $"Citizen {citizen} still works across a missing bridge.");

            if (!world.Businesses.Rows.IsValid(world.Citizens.Workplace[citizen]))
            {
                Assert.Equal(Ticks.Zero, world.Citizens.CommuteFailedSince[citizen]);
            }
        }
    }

    [Fact]
    public void A_completed_commute_before_the_deadline_clears_the_clock_and_keeps_the_job()
    {
        (Simulation simulation, List<Handle<Citizen>> crossers) = Severed(DeadlineDays);
        World world = simulation.World;

        StepTo(simulation, LateEvening(2));
        RebuildBridge(simulation);
        StepTo(simulation, (2 + DeadlineDays + 2) * Ticks.PerDay);

        foreach (int citizen in Live(world, crossers))
        {
            Assert.True(Crosses(world, citizen), $"Citizen {citizen} was dismissed after the repair.");
            Assert.Equal(Ticks.Zero, world.Citizens.CommuteFailedSince[citizen]);
        }
    }

    [Fact]
    public void A_failed_homeward_commute_starts_nothing()
    {
        Simulation simulation = Start(BridgeRules(DeadlineDays));
        World world = simulation.World;

        StepTo(simulation, LateMorning(1));
        List<Handle<Citizen>> crossers = Crossers(world);
        RemoveBridge(simulation);
        StepTo(simulation, (2 + DeadlineDays + 2) * Ticks.PerDay);

        Assert.NotEmpty(crossers);

        foreach (int citizen in Live(world, crossers))
        {
            Assert.Equal(Ticks.Zero, world.Citizens.CommuteFailedSince[citizen]);
            Assert.True(Crosses(world, citizen), $"Citizen {citizen} lost the job without an outbound failure.");
        }
    }

    [Fact]
    public void A_failed_trip_for_another_purpose_starts_nothing()
    {
        Simulation simulation = Start(BridgeRules(DeadlineDays));
        World world = simulation.World;

        StepTo(simulation, LateEvening(1));
        int citizen = world.Citizens.Rows.Resolve(Crossers(world)[0]);

        world.Citizens.Activity[citizen] = (byte)CitizenActivity.Travelling;
        world.RecordTripFate(citizen, TripFate.NoRouteFound);

        Assert.Equal(Ticks.Zero, world.Citizens.CommuteFailedSince[citizen]);

        world.Citizens.Activity[citizen] = (byte)CitizenActivity.TravellingToWork;
        world.RecordTripFate(citizen, TripFate.NoRouteFound);

        Assert.NotEqual(Ticks.Zero, world.Citizens.CommuteFailedSince[citizen]);
    }

    [Fact]
    public void Employment_clears_a_running_clock()
    {
        (Simulation simulation, List<Handle<Citizen>> crossers) = Severed(DeadlineDays);
        World world = simulation.World;

        StepTo(simulation, 3 * Ticks.PerDay);
        Handle<Citizen> crosser = crossers[0];
        int citizen = world.Citizens.Rows.Resolve(crosser);

        Assert.NotEqual(Ticks.Zero, world.Citizens.CommuteFailedSince[citizen]);

        world.Employ(crosser, world.Citizens.Workplace[citizen], world.Citizens.PlannedCommute[citizen]);

        Assert.Equal(Ticks.Zero, world.Citizens.CommuteFailedSince[citizen]);
    }

    [Fact]
    public void Without_the_key_the_clock_never_runs()
    {
        (Simulation simulation, List<Handle<Citizen>> crossers) = Severed(dismissAfterDays: null);
        World world = simulation.World;

        StepTo(simulation, (2 + DeadlineDays + 2) * Ticks.PerDay);

        foreach (int citizen in Live(world, crossers))
        {
            Assert.Equal(Ticks.Zero, world.Citizens.CommuteFailedSince[citizen]);
            Assert.True(Crosses(world, citizen), $"Citizen {citizen} was dismissed in a city with no deadline.");
        }
    }

    [Fact]
    public void A_running_clock_survives_save_and_reload()
    {
        var rules = BridgeRules(DeadlineDays);
        (Simulation simulation, _) = Severed(DeadlineDays, rules);
        World world = simulation.World;

        StepTo(simulation, (2 * Ticks.PerDay) + 1);

        var file = new MemorySave();
        simulation.SaveAtEndOfTick(file);
        simulation.Step(default);

        World reloaded = SaveFile.Read(file, rules, out SaveHeader header);
        var resumed = new Simulation(reloaded, header.Key) { VerifyDecideWritesNothing = false };

        Assert.Equal(world.HashState(), reloaded.HashState());

        long end = (2 + DeadlineDays + 2) * Ticks.PerDay;

        StepTo(simulation, end);
        StepTo(resumed, end);

        Assert.Equal(world.HashState(), reloaded.HashState());
    }

    [Fact]
    public void A_running_clock_reads_as_evidence_with_its_dismissal_due()
    {
        (Simulation simulation, List<Handle<Citizen>> crossers) = Severed(DeadlineDays);
        World world = simulation.World;

        StepTo(simulation, 3 * Ticks.PerDay);

        foreach (int citizen in Live(world, crossers))
        {
            CitizenEvidence who = Core.Evidence.Evidence.OfCitizen(world, world.Citizens.Rows.At(citizen));
            FailingCommuteEvidence failing = Assert.NotNull(who.FailingCommute);

            Assert.Equal(world.Citizens.CommuteFailedSince[citizen].Raw - 1, failing.Since.Raw);
            Assert.Equal(failing.Since.Raw + ((ulong)DeadlineDays * Ticks.PerDay), failing.DismissalDue.Raw);
            Assert.Null(who.LastCommuteEpisode);
        }
    }

    [Fact]
    public void A_workplace_lists_exactly_the_workers_whose_clocks_run()
    {
        (Simulation simulation, List<Handle<Citizen>> crossers) = Severed(DeadlineDays);
        World world = simulation.World;

        StepTo(simulation, 3 * Ticks.PerDay);

        int listed = 0;

        for (int business = 0; business < world.Businesses.Rows.SlotCount; business++)
        {
            if (!world.Businesses.Rows.IsLive(business))
            {
                continue;
            }

            foreach (Handle<Citizen> worker in Core.Evidence.Evidence.FailingCommutersOf(world, world.Businesses.Rows.At(business)).Span)
            {
                int citizen = world.Citizens.Rows.Resolve(worker);

                Assert.NotEqual(Ticks.Zero, world.Citizens.CommuteFailedSince[citizen]);
                Assert.Equal(world.Businesses.Rows.At(business), world.Citizens.Workplace[citizen]);
                listed++;
            }
        }

        int running = 0;

        for (int citizen = 0; citizen < world.Citizens.Rows.SlotCount; citizen++)
        {
            running += world.Citizens.Rows.IsLive(citizen) && world.Citizens.CommuteFailedSince[citizen] != Ticks.Zero ? 1 : 0;
        }

        Assert.Equal(running, listed);
        Assert.True(listed >= crossers.Count, "a severed crosser is missing from the workplace reading.");
    }

    [Fact]
    public void A_repair_before_the_deadline_records_a_recovered_episode()
    {
        (Simulation simulation, List<Handle<Citizen>> crossers) = Severed(DeadlineDays);
        World world = simulation.World;

        StepTo(simulation, LateEvening(2));
        RebuildBridge(simulation);
        StepTo(simulation, 5 * Ticks.PerDay);

        foreach (int citizen in Live(world, crossers))
        {
            CitizenEvidence who = Core.Evidence.Evidence.OfCitizen(world, world.Citizens.Rows.At(citizen));
            CommuteEpisodeEvidence episode = Assert.NotNull(who.LastCommuteEpisode);

            Assert.Null(who.FailingCommute);
            Assert.Equal(CommuteEpisodeOutcome.Recovered, episode.Outcome);
            Assert.Equal(2, episode.StartDay);
            Assert.Equal(3, episode.EndDay);
        }
    }

    [Fact]
    public void The_deadline_records_a_dismissed_episode_with_its_duration()
    {
        (Simulation simulation, List<Handle<Citizen>> crossers) = Severed(DeadlineDays);
        World world = simulation.World;

        StepTo(simulation, (2 + DeadlineDays + 2) * Ticks.PerDay);

        foreach (int citizen in Live(world, crossers))
        {
            CommuteEpisodeEvidence episode = Assert.NotNull(
                Core.Evidence.Evidence.OfCitizen(world, world.Citizens.Rows.At(citizen)).LastCommuteEpisode);

            Assert.Equal(CommuteEpisodeOutcome.Dismissed, episode.Outcome);
            Assert.Equal(2, episode.StartDay);
            Assert.InRange(episode.EndDay - episode.StartDay, DeadlineDays, DeadlineDays + 1);
        }
    }

    [Fact]
    public void A_run_cut_short_by_another_dismissal_or_a_new_job_leaves_the_last_record()
    {
        (Simulation simulation, List<Handle<Citizen>> crossers) = Severed(DeadlineDays);
        World world = simulation.World;

        StepTo(simulation, LateEvening(2));
        RebuildBridge(simulation);
        StepTo(simulation, LateEvening(3));
        RemoveBridge(simulation);
        StepTo(simulation, 5 * Ticks.PerDay);

        List<Handle<Citizen>> running = [];

        foreach (int citizen in Live(world, crossers))
        {
            if (world.Citizens.CommuteFailedSince[citizen] != Ticks.Zero)
            {
                running.Add(world.Citizens.Rows.At(citizen));
            }
        }

        Assert.True(running.Count >= 2, $"only {running.Count} crossers have a second run under way.");

        Handle<Citizen> dismissed = running[0];
        Handle<Citizen> rehired = running[1];
        int rehiredSlot = world.Citizens.Rows.Resolve(rehired);
        CommuteEpisodeEvidence? before = Core.Evidence.Evidence.OfCitizen(world, dismissed).LastCommuteEpisode;
        CommuteEpisodeEvidence? beforeRehired = Core.Evidence.Evidence.OfCitizen(world, rehired).LastCommuteEpisode;

        Assert.Equal(CommuteEpisodeOutcome.Recovered, Assert.NotNull(before).Outcome);
        Assert.Equal(CommuteEpisodeOutcome.Recovered, Assert.NotNull(beforeRehired).Outcome);

        world.Dismiss(dismissed);
        world.Employ(rehired, world.Citizens.Workplace[rehiredSlot], world.Citizens.PlannedCommute[rehiredSlot]);

        Assert.Equal(before, Core.Evidence.Evidence.OfCitizen(world, dismissed).LastCommuteEpisode);
        Assert.Equal(beforeRehired, Core.Evidence.Evidence.OfCitizen(world, rehired).LastCommuteEpisode);
    }

    [Fact]
    public void A_recorded_episode_survives_save_and_reload()
    {
        var rules = BridgeRules(DeadlineDays);
        (Simulation simulation, List<Handle<Citizen>> crossers) = Severed(DeadlineDays, rules);
        World world = simulation.World;

        StepTo(simulation, LateEvening(2));
        RebuildBridge(simulation);
        StepTo(simulation, 4 * Ticks.PerDay);

        var file = new MemorySave();
        simulation.SaveAtEndOfTick(file);
        simulation.Step(default);

        World reloaded = SaveFile.Read(file, rules, out _);

        Assert.Equal(world.HashState(), reloaded.HashState());

        foreach (Handle<Citizen> crosser in crossers)
        {
            if (!world.Citizens.Rows.TryResolve(crosser, out _))
            {
                continue;
            }

            CommuteEpisodeEvidence saved = Assert.NotNull(
                Core.Evidence.Evidence.OfCitizen(world, crosser).LastCommuteEpisode);

            Assert.Equal(saved, Core.Evidence.Evidence.OfCitizen(reloaded, crosser).LastCommuteEpisode);
        }
    }

    [Fact]
    public void An_intermittent_failure_restarts_the_clock_and_never_reaches_the_deadline()
    {
        (Simulation simulation, List<Handle<Citizen>> crossers) = Severed(DeadlineDays);
        World world = simulation.World;

        for (int failedDay = 2; failedDay <= 6; failedDay += 2)
        {
            StepTo(simulation, LateMorning(failedDay));

            foreach (int citizen in Live(world, crossers))
            {
                long since = (long)world.Citizens.CommuteFailedSince[citizen].Raw - 1;

                Assert.InRange(since, failedDay * Ticks.PerDay, LateMorning(failedDay));
            }

            StepTo(simulation, LateEvening(failedDay));
            RebuildBridge(simulation);
            StepTo(simulation, LateEvening(failedDay + 1));

            if (failedDay < 6)
            {
                RemoveBridge(simulation);
            }
        }

        foreach (int citizen in Live(world, crossers))
        {
            CommuteEpisodeEvidence episode = Assert.NotNull(
                Core.Evidence.Evidence.OfCitizen(world, world.Citizens.Rows.At(citizen)).LastCommuteEpisode);

            Assert.True(Crosses(world, citizen), $"Citizen {citizen} was dismissed by failures a commute interrupted.");
            Assert.Equal(Ticks.Zero, world.Citizens.CommuteFailedSince[citizen]);
            Assert.Equal(CommuteEpisodeOutcome.Recovered, episode.Outcome);
            Assert.Equal(6, episode.StartDay);
            Assert.Equal(7, episode.EndDay);
        }
    }

    [Fact]
    public void A_completed_trip_for_another_purpose_leaves_the_clock_running()
    {
        (Simulation simulation, List<Handle<Citizen>> crossers) = Severed(DeadlineDays);
        World world = simulation.World;

        StepTo(simulation, LateMorning(2));

        Dictionary<Handle<Citizen>, Ticks> before = [];

        foreach (int citizen in Live(world, crossers))
        {
            Assert.Equal(CitizenActivity.AtHome, (CitizenActivity)world.Citizens.Activity[citizen]);
            before[world.Citizens.Rows.At(citizen)] = world.Citizens.CommuteFailedSince[citizen];
        }

        int refused = simulation.CommandsRefused;

        simulation.Step(new TickInput(
        [
            NearTrip(0, 0, 1), NearTrip(1, 0, -1), NearTrip(0, 1, 1), NearTrip(1, 1, -1),
        ], rulesetHash: 0));

        Assert.True(simulation.CommandsRefused - refused < 4, "every commanded Trip was refused.");

        StepTo(simulation, LateEvening(2));

        List<int> travelled = [];

        foreach (int citizen in Live(world, crossers))
        {
            if ((TripFate)world.Citizens.LastTripFate[citizen] == TripFate.Completed)
            {
                travelled.Add(citizen);
                Assert.Equal(before[world.Citizens.Rows.At(citizen)], world.Citizens.CommuteFailedSince[citizen]);
            }
        }

        Assert.NotEmpty(travelled);

        StepTo(simulation, (2 + DeadlineDays + 2) * Ticks.PerDay);

        foreach (int citizen in travelled)
        {
            CommuteEpisodeEvidence episode = Assert.NotNull(
                Core.Evidence.Evidence.OfCitizen(world, world.Citizens.Rows.At(citizen)).LastCommuteEpisode);

            Assert.Equal(CommuteEpisodeOutcome.Dismissed, episode.Outcome);
            Assert.Equal(2, episode.StartDay);
        }
    }

    [Fact]
    public void The_clock_counts_days_off_and_nobody_retries_within_a_day()
    {
        const int deadline = 5;
        const int mondayToFriday = 0b001_1111;

        string toml = BridgeToml(deadline).Replace(
            "[[business]]\nname = \"shop\"\n",
            $"[[business]]\nname = \"shop\"\nwork_days = {mondayToFriday}\n",
            StringComparison.Ordinal);

        (Simulation simulation, List<Handle<Citizen>> crossers) = Severed(deadline, Load(toml));
        World world = simulation.World;

        for (int day = 2; day <= 6; day++)
        {
            (_, long noRoute, _) = StepTo(simulation, (day + 1) * Ticks.PerDay);
            bool working = day <= 4;

            Assert.Equal(working ? crossers.Count : 0, noRoute);

            foreach (int citizen in Live(world, crossers))
            {
                long since = (long)world.Citizens.CommuteFailedSince[citizen].Raw - 1;

                Assert.True(Crosses(world, citizen), $"Citizen {citizen} was dismissed on Day {day}.");
                Assert.InRange(since, 2 * Ticks.PerDay, (3 * Ticks.PerDay) - 1);
            }
        }

        StepTo(simulation, 8 * Ticks.PerDay);

        foreach (int citizen in Live(world, crossers))
        {
            CommuteEpisodeEvidence episode = Assert.NotNull(
                Core.Evidence.Evidence.OfCitizen(world, world.Citizens.Rows.At(citizen)).LastCommuteEpisode);

            Assert.Equal(CommuteEpisodeOutcome.Dismissed, episode.Outcome);
            Assert.Equal(2, episode.StartDay);
            Assert.Equal(7, episode.EndDay);
        }
    }

    [Fact]
    public void Tenants_of_one_dwelling_and_workers_of_one_business_keep_their_own_clocks()
    {
        (Simulation simulation, List<Handle<Citizen>> crossers) = Severed(DeadlineDays);
        World world = simulation.World;

        StepTo(simulation, LateMorning(2));

        Handle<Business> shared = default;
        List<int> sharedDwelling = [];

        for (int business = 0; business < world.Businesses.Rows.SlotCount; business++)
        {
            if (!world.Businesses.Rows.IsLive(business))
            {
                continue;
            }

            Handle<Business> handle = world.Businesses.Rows.At(business);
            ReadOnlySpan<Handle<Citizen>> failing = Core.Evidence.Evidence.FailingCommutersOf(world, handle).Span;

            Assert.Equal(failing.Length, failing.ToArray().Distinct().Count());

            if (shared.IsNone && failing.Length >= 2)
            {
                shared = handle;
            }
        }

        foreach (int citizen in Live(world, crossers))
        {
            int home = HomeOf(world, citizen);
            List<int> neighbours = [.. Live(world, crossers).Where(other => HomeOf(world, other) == home
                && world.Citizens.HouseholdOf[other] != world.Citizens.HouseholdOf[citizen])];

            if (neighbours.Count > 0)
            {
                sharedDwelling = [citizen, .. neighbours];
                break;
            }
        }

        Assert.False(shared.IsNone, "no Business has two failing workers.");
        Assert.NotEmpty(sharedDwelling);

        Handle<Citizen>[] workers = Core.Evidence.Evidence.FailingCommutersOf(world, shared).ToArray();
        Dictionary<Handle<Citizen>, Ticks> clocks = [];

        foreach (int citizen in Live(world, crossers))
        {
            clocks[world.Citizens.Rows.At(citizen)] = world.Citizens.CommuteFailedSince[citizen];
        }

        world.Dismiss(workers[0]);
        world.Dismiss(world.Citizens.Rows.At(sharedDwelling[0]));

        Assert.Equal(workers[1..], Core.Evidence.Evidence.FailingCommutersOf(world, shared).ToArray());

        foreach (int citizen in Live(world, crossers))
        {
            Handle<Citizen> handle = world.Citizens.Rows.At(citizen);

            if (handle != workers[0] && citizen != sharedDwelling[0])
            {
                Assert.Equal(clocks[handle], world.Citizens.CommuteFailedSince[citizen]);
            }
        }
    }

    [Fact]
    public void A_citizen_who_dies_mid_episode_leaves_a_clear_slot_for_the_next()
    {
        (Simulation simulation, List<Handle<Citizen>> crossers) = Severed(DeadlineDays);
        World world = simulation.World;

        StepTo(simulation, LateEvening(2));
        RebuildBridge(simulation);
        StepTo(simulation, LateEvening(3));
        RemoveBridge(simulation);
        StepTo(simulation, LateMorning(4));

        int dying = Live(world, crossers).First();
        Handle<Citizen> dead = world.Citizens.Rows.At(dying);
        Handle<Business> workplace = world.Citizens.Workplace[dying];
        Handle<Household> household = world.Citizens.HouseholdOf[dying];

        Assert.NotEqual(Ticks.Zero, world.Citizens.CommuteFailedSince[dying]);
        Assert.NotNull(Core.Evidence.Evidence.OfCitizen(world, dead).LastCommuteEpisode);
        Assert.Contains(dead, Core.Evidence.Evidence.FailingCommutersOf(world, workplace).ToArray());

        world.DieCitizen(dead);

        Assert.DoesNotContain(dead, Core.Evidence.Evidence.FailingCommutersOf(world, workplace).ToArray());

        Handle<Citizen> born = world.CreateCitizen(household);
        CitizenEvidence fresh = Core.Evidence.Evidence.OfCitizen(world, born);

        Assert.Equal(dying, world.Citizens.Rows.Resolve(born));
        Assert.Equal(Ticks.Zero, world.Citizens.CommuteFailedSince[dying]);
        Assert.Equal(0, world.Citizens.CommuteEpisodeStartDay[dying]);
        Assert.Equal(0, world.Citizens.CommuteEpisodeEndDay[dying]);
        Assert.Equal(0, world.Citizens.CommuteEpisodeOutcome[dying]);
        Assert.Null(fresh.FailingCommute);
        Assert.Null(fresh.LastCommuteEpisode);

        StepTo(simulation, (4 + DeadlineDays + 2) * Ticks.PerDay);

        Assert.False(world.Citizens.Rows.IsValid(dead));
    }

    [Fact]
    public void A_household_removed_mid_episode_takes_every_clock_with_it()
    {
        (Simulation simulation, List<Handle<Citizen>> crossers) = Severed(DeadlineDays);
        World world = simulation.World;

        StepTo(simulation, LateMorning(2));

        int first = Live(world, crossers).First();
        Handle<Household> household = world.Citizens.HouseholdOf[first];
        List<Handle<Citizen>> members = [.. Live(world, crossers)
            .Where(citizen => world.Citizens.HouseholdOf[citizen] == household)
            .Select(citizen => world.Citizens.Rows.At(citizen))];
        List<Handle<Business>> workplaces = [.. members.Select(member =>
            world.Citizens.Workplace[world.Citizens.Rows.Resolve(member)])];

        world.DestroyHousehold(household);

        foreach (Handle<Business> workplace in workplaces)
        {
            foreach (Handle<Citizen> member in members)
            {
                Assert.DoesNotContain(member, Core.Evidence.Evidence.FailingCommutersOf(world, workplace).ToArray());
            }
        }

        StepTo(simulation, (2 + DeadlineDays + 2) * Ticks.PerDay);

        foreach (Handle<Citizen> member in members)
        {
            Assert.False(world.Citizens.Rows.IsValid(member));
        }
    }

    [Fact]
    public void Observation_cadence_and_a_reload_mid_episode_change_nothing()
    {
        var rules = BridgeRules(DeadlineDays, 2 * DeadlineDays);
        long middle = LateMorning(3) + 7;
        long end = (2 + DeadlineDays + 3) * Ticks.PerDay;

        (Simulation everyTick, List<Handle<Citizen>> crossers) = Severed(DeadlineDays, rules);
        Dictionary<Handle<Citizen>, Handle<Business>> employer = crossers.ToDictionary(
            crosser => crosser, crosser => everyTick.World.Citizens.Workplace[everyTick.World.Citizens.Rows.Resolve(crosser)]);
        Dictionary<Handle<Citizen>, long> dismissedAt = [];

        while ((long)everyTick.World.Tick.Raw < end)
        {
            StepTo(everyTick, (long)everyTick.World.Tick.Raw + 1);

            foreach (int citizen in Live(everyTick.World, crossers))
            {
                Handle<Citizen> handle = everyTick.World.Citizens.Rows.At(citizen);

                if (!dismissedAt.ContainsKey(handle) && everyTick.World.Citizens.Workplace[citizen] != employer[handle])
                {
                    dismissedAt[handle] = (long)everyTick.World.Tick.Raw;
                }
            }
        }

        (Simulation strided, _) = Severed(DeadlineDays, rules);

        for (int stepped = 0; (long)strided.World.Tick.Raw < end; stepped++)
        {
            strided.Step(default);

            if (stepped % 37 == 0)
            {
                strided.World.HashState();
                strided.Trips.Drain();
                Core.Evidence.Evidence.OfCitizen(strided.World, crossers[0]);
            }
        }

        (Simulation saved, _) = Severed(DeadlineDays, rules);

        StepTo(saved, middle);

        var file = new MemorySave();
        saved.SaveAtEndOfTick(file);
        saved.Step(default);

        World reloaded = SaveFile.Read(file, rules, out SaveHeader header);
        var resumed = new Simulation(reloaded, header.Key) { VerifyDecideWritesNothing = false };

        StepTo(resumed, end);

        Assert.NotEmpty(dismissedAt);
        Assert.Equal(everyTick.World.HashState(), strided.World.HashState());
        Assert.Equal(everyTick.World.HashState(), resumed.World.HashState());

        int dismissals = 0;

        foreach (int citizen in Live(everyTick.World, crossers))
        {
            Handle<Citizen> handle = everyTick.World.Citizens.Rows.At(citizen);
            CommuteEpisodeEvidence episode = Assert.NotNull(
                Core.Evidence.Evidence.OfCitizen(everyTick.World, handle).LastCommuteEpisode);

            Assert.Equal(episode, Core.Evidence.Evidence.OfCitizen(strided.World, handle).LastCommuteEpisode);
            Assert.Equal(episode, Core.Evidence.Evidence.OfCitizen(resumed.World, handle).LastCommuteEpisode);

            if (episode.Outcome == CommuteEpisodeOutcome.Dismissed)
            {
                Assert.Equal(dismissedAt[handle] / Ticks.PerDay, episode.EndDay);
                dismissals++;
            }
        }

        Assert.True(dismissals > 0, "no crosser's last episode is the dismissal.");
    }

    [Fact]
    public void Running_clocks_hash_alike_at_every_route_worker_count()
    {
        var rules = BridgeRules(DeadlineDays, 2 * DeadlineDays);
        long end = (2 + (2 * DeadlineDays) + 1) * Ticks.PerDay;

        (Simulation serial, List<Handle<Citizen>> crossers) = Severed(DeadlineDays, rules);
        (Simulation parallel, _) = Severed(DeadlineDays, rules);

        parallel.RouteWorkerCount = 4;

        while ((long)serial.World.Tick.Raw < end)
        {
            long next = (long)serial.World.Tick.Raw + 16;

            StepTo(serial, next);
            StepTo(parallel, next);

            Assert.Equal(serial.World.HashState(), parallel.World.HashState());
        }

        World world = serial.World;

        Assert.Contains(Live(world, crossers), citizen => Core.Evidence.Evidence
            .OfCitizen(world, world.Citizens.Rows.At(citizen)).LastCommuteEpisode?.Outcome == CommuteEpisodeOutcome.Dismissed);
        Assert.Contains(Enumerable.Range(0, world.Buildings.Rows.SlotCount), building =>
            world.Buildings.Rows.IsLive(building) && world.Buildings.AbandonedCause[building] == (byte)AbandonmentCause.CutOff);

        serial.CheckEndOfRun();
        parallel.CheckEndOfRun();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65_536)]
    public void The_loader_refuses_a_deadline_outside_one_to_65535_days(int days)
    {
        string toml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml"))
            .Replace("\n[jobs]\n", $"\n[jobs]\ndismissed_after_failed_commute_days = {days}\n", StringComparison.Ordinal);

        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.False(result.Ok);
        Assert.Contains("dismissed_after_failed_commute_days", result.Describe(), StringComparison.Ordinal);
    }

    /// <summary>The bridge removed at 23:00 on Day 1, so the first failures come on Day 2.</summary>
    private static (Simulation Simulation, List<Handle<Citizen>> Crossers) Severed(
        int? dismissAfterDays, Core.Rules.Ruleset? rules = null)
    {
        Simulation simulation = Start(rules ?? BridgeRules(dismissAfterDays));

        StepTo(simulation, LateEvening(1));

        List<Handle<Citizen>> crossers = Crossers(simulation.World);

        Assert.NotEmpty(crossers);

        RemoveBridge(simulation);

        return (simulation, crossers);
    }

    /// <summary>A Trip between two blocks on the near side, which the missing bridge does not touch.</summary>
    private static Command NearTrip(int column, int row, int blocksEast) => new(
        CommandKind.Trip,
        new Tiles(column * BlockTiles),
        new Tiles(row * BlockTiles),
        new TripPayload((sbyte)blocksEast, 0).Encode());

    private static IEnumerable<int> Live(World world, List<Handle<Citizen>> crossers)
    {
        foreach (Handle<Citizen> crosser in crossers)
        {
            if (world.Citizens.Rows.TryResolve(crosser, out int citizen))
            {
                yield return citizen;
            }
        }
    }
}
