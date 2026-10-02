using Borough.Core;
using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Rules;

public sealed class LabourTests
{
    private const long LabourPerDay = 3000;

    internal const string Labour = """
        [[resource]]
        name = "labour"
        family = "labour"
        shelf_life_cycles = 4
        shelf_life_cycle_minutes = 15

        """;

    internal const string LabourBin = """
            { resource = "labour",   owner = "business" },
        """;

    private const string LabourJobs = """
        labour_per_day = 3000
        labour_tier_percent = [100, 125, 150]
        labour_experience_premium_percent = 10
        """;

    internal static string Text()
    {
        string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "shopping.toml"));

        return text
            .Replace("[[business]]\nname = \"shop\"", Labour + "[[business]]\nname = \"shop\"")
            .Replace("    { resource = \"money\",    owner = \"business\" },\n",
                "    { resource = \"money\",    owner = \"business\" },\n" + LabourBin)
            .Replace("arrive_early_max_minutes = 15\n", "arrive_early_max_minutes = 15\n" + LabourJobs);
    }

    private static RulesetLoadResult Parse(string text) => RulesetLoader.Parse(text, "labour.toml");

    private static (World World, Simulation Sim) Start()
    {
        RulesetLoadResult loaded = Parse(Text());
        Assert.True(loaded.Ok, loaded.Describe());
        var key = WorldKey.FromSeed(0);
        var world = new World(400, loaded.Ruleset!, key);
        var sim = new Simulation(world, key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        return (world, sim);
    }

    private static int LabourBinOf(World world, Handle<Business> business)
    {
        Handle<Bin> at = world.Businesses.BinHead[world.Businesses.Rows.Resolve(business)];

        while (!at.IsNone)
        {
            int slot = world.Bins.Rows.Resolve(at);

            if (world.Rules.Family(world.Bins.Resource[slot]) == ResourceFamily.Labour)
            {
                return slot;
            }

            at = world.Bins.OwnerNext[slot];
        }

        return Rows.NoSlot;
    }

    private static int[] Workers(World world) =>
        Enumerable.Range(0, world.Citizens.Rows.SlotCount)
            .Where(c => world.Citizens.Rows.IsLive(c) && !world.Citizens.Workplace[c].IsNone)
            .ToArray();

    [Fact]
    public void A_labour_resource_and_its_grading_load()
    {
        RulesetLoadResult loaded = Parse(Text());

        Assert.True(loaded.Ok, loaded.Describe());
        Ruleset rules = loaded.Ruleset!;
        Assert.Equal(ResourceFamily.Labour, rules.Family(new ResourceId(4)));
        Assert.False(rules.IsConserved(new ResourceId(4)));
        Assert.Equal(LabourPerDay, rules.Jobs.LabourPerDay);
        Assert.Equal(125, rules.Jobs.LabourPercentOf(2));
        Assert.Equal(150, rules.Jobs.LabourPercentOf(SchoolingRuleset.TopTier));
        Assert.Equal(10, rules.Jobs.LabourExperiencePremiumPercent);
    }

    [Theory]
    [InlineData("shelf_life_cycles = 4\nshelf_life_cycle_minutes = 15\n", "", "shelf life")]
    [InlineData("{ resource = \"labour\",   owner = \"business\" }", "{ resource = \"labour\", capacity = 8, owner = \"business\" }", "capacity")]
    [InlineData("{ resource = \"labour\",   owner = \"business\" }", "{ resource = \"labour\" }", "owner = \"business\"")]
    [InlineData("inputs  = []\noutputs = [ { scope = \"local\", resource = \"sundries\", amount = 8 } ]", "inputs  = [ { scope = \"pool\", resource = \"labour\", amount = 1 } ]\noutputs = [ { scope = \"local\", resource = \"sundries\", amount = 8 } ]", "local")]
    [InlineData("inputs  = []\noutputs = [ { scope = \"local\", resource = \"sundries\", amount = 8 } ]", "inputs  = []\noutputs = [ { scope = \"local\", resource = \"labour\", amount = 8 } ]", "output")]
    [InlineData("labour_per_day = 3000\n", "", "labour_per_day")]
    [InlineData("shelf_life_cycle_minutes = 15", "shelf_life_cycle_minutes = 120", "shift_hours_min")]
    [InlineData("labour_tier_percent = [100, 125, 150]", "labour_tier_percent = [90, 125, 150]", "labour_tier_percent")]
    [InlineData("labour_experience_premium_percent = 10", "labour_experience_premium_percent = -1", "labour_experience_premium_percent")]
    public void A_labour_declaration_that_cannot_work_is_refused(string from, string to, string named)
    {
        string text = Text();
        Assert.Contains(from, text);

        RulesetLoadResult loaded = Parse(text.Replace(from, to));

        Assert.False(loaded.Ok);
        Assert.Contains(named, loaded.Describe());
    }

    [Fact]
    public void A_kind_with_two_labour_bins_is_refused()
    {
        string text = Text()
            .Replace(Labour, Labour + Labour.Replace("name = \"labour\"", "name = \"overtime\""))
            .Replace(LabourBin, LabourBin + LabourBin.Replace("\"labour\",  ", "\"overtime\","));

        RulesetLoadResult loaded = Parse(text);

        Assert.False(loaded.Ok);
        Assert.Contains("two labour Bins", loaded.Describe());
    }

    [Fact]
    public void Labour_per_day_without_a_labour_resource_is_refused()
    {
        string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "shopping.toml"))
            .Replace("arrive_early_max_minutes = 15\n", "arrive_early_max_minutes = 15\n" + LabourJobs);

        RulesetLoadResult loaded = Parse(text);

        Assert.False(loaded.Ok);
        Assert.Contains("labour_per_day", loaded.Describe());
    }

    [Fact]
    public void Present_workers_deposit_graded_labour_into_their_business_each_tick()
    {
        var (world, sim) = Start();
        for (int t = 0; t < Ticks.PerDay; t++) { sim.Step(default); }

        int[] workers = Workers(world);
        Assert.NotEmpty(workers);
        foreach (int c in workers)
        {
            world.Citizens.Activity[c] = (byte)CitizenActivity.AtWork;
            world.Citizens.LabourRemainder[c] = 0;
        }

        var expected = new Dictionary<int, long>();
        var remainders = new long[workers.Length];
        foreach (int c in workers)
        {
            int bin = LabourBinOf(world, world.Citizens.Workplace[c]);
            if (bin != Rows.NoSlot) { expected[bin] = world.Bins.LevelAt(bin); }
        }
        Assert.NotEmpty(expected);

        long deposited = 0;
        for (int t = Ticks.PerDay; t < 8 * Ticks.PerDay; t++)
        {
            Ticks tick = new((ulong)t);
            for (int i = 0; i < workers.Length; i++)
            {
                int c = workers[i];
                int bin = LabourBinOf(world, world.Citizens.Workplace[c]);
                if (bin == Rows.NoSlot || CivicEngine.TooIllToWork(world, c)
                    || !WorkSchedule.OnDuty(world, c, tick)) { continue; }

                long rate = IntegerMath.FloorDiv(
                    LabourPerDay * world.Rules.Jobs.LabourPercentOf(world.Citizens.SkillTier[c]), 100);
                long scaled = remainders[i] + rate;
                long whole = IntegerMath.FloorDiv(scaled, Ticks.PerDay);
                remainders[i] = scaled - whole * Ticks.PerDay;
                expected[bin] += whole;
                deposited += whole;
            }

            WorkSchedule.Accrue(world, tick);

            for (int i = 0; i < workers.Length; i++)
            {
                Assert.Equal(remainders[i], world.Citizens.LabourRemainder[workers[i]]);
            }
        }

        Assert.True(deposited > 0);
        foreach ((int bin, long level) in expected)
        {
            Assert.Equal(level, world.Bins.LevelAt(bin));
        }
    }

    [Fact]
    public void Absent_workers_deposit_nothing()
    {
        var (world, sim) = Start();
        for (int t = 0; t < Ticks.PerDay; t++) { sim.Step(default); }

        int[] workers = Workers(world);
        foreach (int c in workers) { world.Citizens.Activity[c] = (byte)CitizenActivity.AtHome; }
        var before = workers
            .Select(c => LabourBinOf(world, world.Citizens.Workplace[c]))
            .Where(bin => bin != Rows.NoSlot)
            .Distinct()
            .ToDictionary(bin => bin, bin => world.Bins.LevelAt(bin));
        Assert.NotEmpty(before);

        for (int t = Ticks.PerDay; t < 8 * Ticks.PerDay; t++)
        {
            WorkSchedule.Accrue(world, new Ticks((ulong)t));
        }

        foreach ((int bin, long level) in before)
        {
            Assert.Equal(level, world.Bins.LevelAt(bin));
        }
    }
}
