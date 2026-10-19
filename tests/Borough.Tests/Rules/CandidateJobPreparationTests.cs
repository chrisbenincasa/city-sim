using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Tests.Entities;

namespace Borough.Tests.Rules;

public sealed class CandidateJobPreparationTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(7);

    [Fact]
    public void Sampled_counts_match_a_full_field_before_and_after_vacancy_changes()
    {
        World world = JobsAttractTests.City();
        var full = new JobAccessibility();
        full.Refresh(world);
        int first = -1;
        for (int lot = 0; lot < world.Lots.Rows.SlotCount; lot++)
        {
            if (full.Count(lot, 3) > 0) { first = lot; break; }
        }
        Assert.True(first >= 0);
        int[] origins = [first, first, (first + 1) % world.Lots.Rows.SlotCount];
        AssertMatches(world, origins);
        Assert.InRange(world.JobAccess.Searches, 1, 2);
        Assert.True(world.JobAccess.TotalSearches < full.TotalSearches);
        AssertMatches(world, origins);
        Assert.Equal(0, world.JobAccess.Searches);

        for (int business = 0; business < world.Businesses.Rows.SlotCount; business++)
        {
            if (!world.Businesses.Rows.IsLive(business) || !world.HasJob(business)) { continue; }
            var citizen = world.CreateCitizen(world.Households.Rows.At(0));
            world.Employ(citizen, world.Businesses.Rows.At(business), Ticks.Zero);
            break;
        }
        AssertMatches(world, origins);
        Assert.InRange(world.JobAccess.Searches, 1, 2);

        world.Adopt(JobsAttractTests.Rules(), 8, world.Tick, Key);
        AssertMatches(world, origins);
        Assert.InRange(world.JobAccess.Searches, 1, 2);
        world.Roads.RebuildDerived();
        AssertMatches(world, origins);
        Assert.InRange(world.JobAccess.Searches, 1, 2);
    }

    private static void AssertMatches(World world, int[] origins)
    {
        world.JobAccess.Prepare(world, origins);

        // A cold field each time, so an input missing from the snapshot cannot hide in both caches.
        var full = new JobAccessibility();
        full.Refresh(world);
        foreach (int lot in origins)
            for (byte tier = 1; tier <= 3; tier++)
            { Assert.Equal(full.Count(lot, tier), world.JobAccess.Count(lot, tier)); }
    }

    [Fact]
    public void Each_comparison_prepares_at_most_its_two_authored_candidate_budgets()
    {
        World world = JobsAttractTests.City();
        var simulation = new Simulation(world, Key);
        var gate = world.Buildings.Rows.At(0);
        for (int building = 0; building < world.Buildings.Rows.SlotCount; building++)
        {
            if (world.Buildings.Rows.IsLive(building) && world.IsOutsideConnection(world.Buildings.Kind[building]))
            { gate = world.Buildings.Rows.At(building); break; }
        }
        var composition = HinterlandComposition.Of(world.Rules.HinterlandPopulations[0]);
        for (ulong identity = 1; identity <= 16; identity++)
        {
            int business = (int)identity - 1;
            if (world.Businesses.Rows.IsLive(business))
            { world.DestroyBusiness(world.Businesses.Rows.At(business)); }
            long before = world.JobAccess.TotalSearches;
            var prospect = new ArrivalProspect(default, MapEdge.West, composition, new Money(20_000), identity);
            ProspectOutcome sampled = simulation.Placement.Compare(prospect, gate, Ticks.Zero);
            Assert.InRange(world.JobAccess.TotalSearches - before, 0, 2 * world.Rules.Placement.Candidates);
            world.JobAccess.Refresh(world);
            Assert.Equal(sampled, simulation.Placement.Compare(prospect, gate, Ticks.Zero));
        }
    }

    [Fact]
    public void Candidate_preparation_keeps_the_Decide_guard_green()
    {
        World world = JobsAttractTests.City();
        var simulation = new Simulation(world, Key) { VerifyDecideWritesNothing = true };
        for (int tick = 0; tick < 64; tick++) { simulation.Step(default); }
        Assert.True(world.JobAccess.TotalSearches > 0);
        simulation.CheckEndOfRun();
    }
}
