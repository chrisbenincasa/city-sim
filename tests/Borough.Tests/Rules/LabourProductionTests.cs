using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Evidence;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Persistence;

namespace Borough.Tests.Rules;

public sealed class LabourProductionTests
{
    private static readonly ResourceId Crumbs = new(5);

    private const string Goods = """
        [[resource]]
        name = "crumbs"
        family = "good"

        [[resource]]
        name = "flour"
        family = "good"

        """;

    private const string GoodBins = """
            { resource = "crumbs",   capacity = 1000000, owner = "business" },
            { resource = "flour",    capacity = 64, owner = "business" },
        """;

    private static string Text(long labourPerDay = 3000, bool needsFlour = false)
    {
        string flour = needsFlour ? ", { scope = \"local\", resource = \"flour\", amount = 1 }" : "";

        return LabourTests.Text()
            .Replace("[[business]]\nname = \"shop\"", Goods + "[[business]]\nname = \"shop\"")
            .Replace(LabourTests.LabourBin, LabourTests.LabourBin + GoodBins)
            .Replace("labour_per_day = 3000", $"labour_per_day = {labourPerDay}")
            .Replace("  { resource = \"repairs\",  price = 250 },\n",
                "  { resource = \"repairs\",  price = 250 },\n  { resource = \"crumbs\", price = 10 },\n  { resource = \"flour\", price = 10 },\n")
            .Replace("  { resource = \"repairs\",  price = 200 },\n",
                "  { resource = \"repairs\",  price = 200 },\n  { resource = \"crumbs\", price = 10 },\n  { resource = \"flour\", price = 10 },\n")
            + $$"""

            [[rule]]
            name    = "bake"
            kind    = "shopfront"
            rate    = 8
            apply   = { min = 1, max = 1000 }
            inputs  = [ { scope = "local", resource = "labour", amount = 4 }{{flour}} ]
            outputs = [ { scope = "local", resource = "crumbs", amount = 1 } ]
            """;
    }

    private static (World World, Simulation Sim) Start(string text)
    {
        RulesetLoadResult loaded = RulesetLoader.Parse(text, "labour-production.toml");
        Assert.True(loaded.Ok, loaded.Describe());
        var key = WorldKey.FromSeed(0);
        var world = new World(400, loaded.Ruleset!, key);
        var sim = new Simulation(world, key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        return (world, sim);
    }

    private static RuleId Bake(Ruleset rules)
    {
        for (int i = 1; i <= rules.RuleCount; i++)
        {
            var id = new RuleId((ushort)i);
            foreach (Term term in rules.Outputs(id))
            {
                if (term.Bin.Resource == Crumbs) { return id; }
            }
        }

        throw new InvalidOperationException("no Rule makes crumbs");
    }

    private static int[] Bakers(World world)
    {
        RuleId bake = Bake(world.Rules);

        return Enumerable.Range(0, world.RuleInstances.Rows.SlotCount)
            .Where(i => world.RuleInstances.Rows.IsLive(i) && world.RuleInstances.Rule[i] == bake)
            .ToArray();
    }

    private static long Produced(World world)
    {
        long total = 0;
        for (int bin = 0; bin < world.Bins.Rows.SlotCount; bin++)
        {
            if (world.Bins.Rows.IsLive(bin) && world.Bins.Resource[bin] == Crumbs) { total += world.Bins.LevelAt(bin); }
        }

        return total;
    }

    private static Handle<Bin> LabourOf(World world, int instance) =>
        world.LabourBinOf(world.Businesses.Rows.Resolve(world.RuleInstances.Business[instance]));

    [Fact]
    public void A_rule_firing_slower_than_its_labour_keeps_is_refused()
    {
        string text = Text();
        string from = "rate    = 8\napply   = { min = 1, max = 1000 }";
        Assert.Contains(from, text);

        RulesetLoadResult loaded = RulesetLoader.Parse(
            text.Replace(from, "rate    = 128\napply   = { min = 1, max = 1000 }"), "slow.toml");

        Assert.False(loaded.Ok);
        Assert.Contains("fires every 128 Ticks", loaded.Describe());
    }

    [Fact]
    public void Before_any_shift_a_production_rule_waits_on_labour_without_starving()
    {
        var (world, sim) = Start(Text());
        for (int t = 0; t < 300; t++) { sim.Step(default); }

        int[] bakers = Bakers(world);
        Assert.NotEmpty(bakers);
        Assert.Equal(0, Produced(world));
        foreach (int i in bakers)
        {
            Assert.Equal(LabourOf(world, i), world.RuleInstances.WaitingOn[i]);
            Assert.Equal(Blocking.Supply, world.RuleInstances.Blocked[i]);
            Assert.False(world.RuleInstances.IsStarving(i));
        }
    }

    [Fact]
    public void A_real_input_shortage_starts_the_clock_even_when_labour_is_short_too()
    {
        var (world, sim) = Start(Text(needsFlour: true));
        for (int t = 0; t < 300; t++) { sim.Step(default); }

        int[] bakers = Bakers(world);
        Assert.NotEmpty(bakers);
        foreach (int i in bakers)
        {
            Assert.NotEqual(LabourOf(world, i), world.RuleInstances.WaitingOn[i]);
            Assert.True(world.RuleInstances.IsStarving(i));
        }
    }

    [Fact]
    public void Output_follows_the_labour_supplied_and_a_closed_weekend_accrues_no_pressure()
    {
        var (world, sim) = Start(Text());
        var producedByDay = new long[7];
        for (int day = 0; day < 7; day++)
        {
            for (int t = 0; t < Ticks.PerDay; t++)
            {
                sim.Step(default);
                foreach (int i in Bakers(world)) { Assert.False(world.RuleInstances.IsStarving(i)); }
            }

            producedByDay[day] = Produced(world);
        }

        var (halfWorld, halfSim) = Start(Text(labourPerDay: 1500));
        for (int t = 0; t < 7 * Ticks.PerDay; t++) { halfSim.Step(default); }

        long full = producedByDay[^1];
        long half = Produced(halfWorld);
        Assert.True(full > 0);
        Assert.InRange(half * 100 / full, 40, 60);
        Assert.Contains(producedByDay.Zip(producedByDay.Skip(1)), pair => pair.First == pair.Second);
        sim.CheckEndOfRun();
    }

    [Fact]
    public void A_city_saved_mid_shift_resumes_with_its_labour_and_hashes_identically()
    {
        string text = Text();
        var (world, sim) = Start(text);
        Ruleset rules = world.Rules;
        for (int t = 0; t < 7 * Ticks.PerDay
            && !(Produced(world) > 0 && Bakers(world).Any(i => Level(world, LabourOf(world, i)) > 0)
                && world.Tick.Raw % 7 != 0); t++)
        {
            sim.Step(default);
        }

        Assert.Contains(Bakers(world), i => Level(world, LabourOf(world, i)) > 0);
        var save = new MemorySave();
        SaveFile.Write(world, 0, save);
        World restored = SaveFile.Read(save, rules, out _);
        var resumed = new Simulation(restored, WorldKey.FromSeed(0));
        long before = Produced(world);

        for (int t = 0; t < 1024; t++)
        {
            sim.Step(default);
            resumed.Step(default);
            Assert.Equal(world.HashState(), restored.HashState());
        }

        Assert.True(Produced(restored) > before);
        sim.CheckEndOfRun();
        resumed.CheckEndOfRun();
    }

    [Fact]
    public void Labour_a_shift_leaves_unspent_is_reported_as_the_business_s_waste()
    {
        var (world, sim) = Start(Text(needsFlour: true));
        for (int t = 0; t < 7 * Ticks.PerDay; t++) { sim.Step(default); }

        int baker = Bakers(world)[0];
        Handle<Business> business = world.RuleInstances.Business[baker];
        Handle<Building> premises = world.RuleInstances.Building[baker];
        ResourceId labour = world.Bins.Resource[world.Bins.Rows.Resolve(LabourOf(world, baker))];

        BuildingEvidence evidence = Core.Evidence.Evidence.OfBuilding(world, premises);

        Assert.Contains(evidence.Waste.ToArray(),
            w => w.Business == business && w.Resource == labour && w.Today + w.Yesterday > 0);
        sim.CheckEndOfRun();
    }

    private static long Level(World world, Handle<Bin> bin) =>
        bin.IsNone ? 0 : world.Bins.LevelAt(world.Bins.Rows.Resolve(bin));
}
