using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Evidence;
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

        foreach (int citizen in Live(world, crossers))
        {
            Assert.Equal(
                Core.Evidence.Evidence.OfCitizen(world, world.Citizens.Rows.At(citizen)).LastCommuteEpisode,
                Core.Evidence.Evidence.OfCitizen(reloaded, world.Citizens.Rows.At(citizen)).LastCommuteEpisode);
        }
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
