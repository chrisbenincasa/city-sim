using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Evidence;
using Borough.Core.Instruments;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;
using Borough.Tests.Rules;

namespace Borough.Tests.Instruments;

public sealed class NeedsReadingTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(0);

    [Fact]
    public void Households_priced_out_of_every_vacant_home_are_counted_as_unable_to_afford()
    {
        string toml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "priced-out.toml"));
        RulesetLoadResult loaded = RulesetLoader.Parse(toml, "priced-out.toml");
        Assert.True(loaded.Ok, loaded.Describe());

        var world = new World(500, loaded.Ruleset!, Key);
        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);
        var simulation = new Simulation(world, Key) { VerifyDecideWritesNothing = false };

        for (int tick = 0; tick < Ticks.PerDay; tick++)
        {
            simulation.Step(TickInput.Empty);
        }

        HomeNeeds homes = NeedsReading.Homes(world);

        Assert.Equal(world.UnplacedPool.Count, homes.Looking);
        Assert.True(homes.Looking > 0, "nobody in priced-out.toml is looking for a home.");
        Assert.True(homes.CannotAfford > 0, "nobody in priced-out.toml is priced out.");
        Assert.Equal(homes.Looking, homes.NoRoom + homes.CannotAfford + homes.CanAfford);
        Assert.Equal(VacantHomes(world), homes.VacantHomes);
        Assert.Equal(homes.VacantHomes == 0 ? homes.Looking : 0, homes.NoRoom);
        simulation.CheckEndOfRun();
    }

    [Fact]
    public void Citizens_who_find_every_post_full_are_counted_with_the_posts_still_open()
    {
        (World world, Simulation simulation) = SchoolingTests.JobsCity(requiresTier: 0);

        simulation.Step(default);

        JobNeeds jobs = NeedsReading.Jobs(world);

        Assert.True(jobs.NoVacancy > 0, "nobody in the jobs city found every post full.");
        Assert.Equal(Citizens(world, EmploymentState.NoVacancy), jobs.NoVacancy);
        Assert.Equal(Citizens(world, EmploymentState.Employed), jobs.Employed);
        Assert.Equal(OpenPosts(world), jobs.OpenPosts);
        Assert.Equal(world.UnpremisedPool.Count, jobs.BusinessesLooking);
    }

    [Fact]
    public void Failing_errands_are_grouped_by_good_and_reason_with_the_largest_first()
    {
        (World world, Simulation simulation) = ShoppingTests.Start();

        for (int tick = 0; tick < Ticks.PerDay; tick++)
        {
            simulation.Step(default);
        }

        int empty = 0, unknown = 0, marked = 0;

        for (int row = 0; row < world.Shopping.Rows.SlotCount; row++)
        {
            if (!world.Shopping.Rows.IsLive(row))
            {
                continue;
            }

            world.Shopping.UnservedSince[row] = world.Tick;

            if (marked++ % 3 == 0)
            {
                world.Shopping.Reason[row] = (byte)ShoppingFailure.NoKnownShop;
                unknown++;
            }
            else
            {
                world.Shopping.Reason[row] = (byte)ShoppingFailure.Empty;
                empty++;
            }
        }

        Assert.True(unknown > 0 && empty > 0, "the shopping fixture has too few errands to split.");

        var groups = new ShoppingNeed[16];
        int written = NeedsReading.Shopping(world, groups);

        Assert.True(written > 0);
        Assert.Equal(empty, Sum(groups, written, ShoppingFailure.Empty));
        Assert.Equal(unknown, Sum(groups, written, ShoppingFailure.NoKnownShop));

        for (int at = 1; at < written; at++)
        {
            Assert.True(groups[at - 1].Households >= groups[at].Households);
        }
    }

    [Fact]
    public void An_errand_that_has_not_failed_is_not_a_need()
    {
        (World world, _) = ShoppingTests.Start();

        for (int row = 0; row < world.Shopping.Rows.SlotCount; row++)
        {
            if (world.Shopping.Rows.IsLive(row))
            {
                world.Shopping.UnservedSince[row] = Ticks.Zero;
            }
        }

        Assert.Equal(0, NeedsReading.Shopping(world, new ShoppingNeed[16]));
    }

    [Fact]
    public void Vacant_lots_are_counted_once_and_by_every_reason_they_carry()
    {
        (World world, _) = ShoppingTests.Start();

        LotNeeds lots = NeedsReading.Lots(world);
        LotEvidence[] evidence = Borough.Core.Evidence.Evidence.OfLots(world);

        Assert.Equal(evidence.Count(lot => lot.IsVacant), lots.Vacant);
        Assert.Equal(
            evidence.Count(lot => lot.IsVacant && (lot.Reason & VacancyReason.NoFrontage) != 0),
            lots.NoFrontage);
        Assert.Equal(evidence.Count(lot => lot.IsVacant && lot.Reason == VacancyReason.None), lots.Unexplained);
    }

    private static int VacantHomes(World world)
    {
        int count = 0;

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot) && !world.Buildings.IsAbandoned(slot)
                && world.HasRoomForHousehold(slot))
            {
                count++;
            }
        }

        return count;
    }

    private static int Citizens(World world, EmploymentState state)
    {
        int count = 0;

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (world.Citizens.Rows.IsLive(slot) && world.Citizens.Employment[slot] == (byte)state)
            {
                count++;
            }
        }

        return count;
    }

    private static int OpenPosts(World world)
    {
        int open = 0;

        for (int slot = 0; slot < world.Businesses.Rows.SlotCount; slot++)
        {
            if (world.Businesses.Rows.IsLive(slot) && world.HasJob(slot))
            {
                open += world.DeclaredJobs(slot) - world.Workers.Length(slot);
            }
        }

        return open;
    }

    private static int Sum(ShoppingNeed[] groups, int written, ShoppingFailure reason)
    {
        int sum = 0;

        for (int at = 0; at < written; at++)
        {
            if (groups[at].Reason == reason)
            {
                sum += groups[at].Households;
            }
        }

        return sum;
    }
}
