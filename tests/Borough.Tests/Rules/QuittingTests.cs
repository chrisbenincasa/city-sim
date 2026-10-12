using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Golden;
using Xunit.Abstractions;

namespace Borough.Tests.Rules;

/// <summary>
/// Workers who go unpaid for <c>quits_after_days_unpaid</c> Days leave, and a trade that sets the key
/// hires only what its till can pay.
/// </summary>
/// <remarks>
/// <c>insolvent.toml</c> pays its grocers weekly, so a threshold of 14 Days is two short paydays.
/// The fixture's grocers are too poor to hire under the hiring rule, so each test funds two of them.
/// </remarks>
public sealed class QuittingTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    private const int Population = 2_000;
    private const int Threshold = 14;

    [Fact]
    public void An_unpaid_worker_quits_and_a_paid_one_does_not()
    {
        (World world, Simulation simulation, Handle<Business> paying, Handle<Business> broke) =
            Staffed(quits: true);

        int[] paid = WorkersOf(world, paying);
        int[] unpaid = WorkersOf(world, broke);
        int quit = 0;

        for (int tick = 0; tick < (Threshold + 7) * Ticks.PerDay; tick++)
        {
            Starve(world, broke, paying, simulation.Tick);
            simulation.Step(default);
            quit += simulation.LastPayroll.Quit;
        }

        _output.WriteLine($"{paid.Length} paid, {unpaid.Length} unpaid, {quit} quit across the city.");

        foreach (int citizen in paid)
        {
            Assert.Equal(paying, world.Citizens.Workplace[citizen]);
            Assert.Equal(0, world.Citizens.DaysUnpaid[citizen]);
        }

        foreach (int citizen in unpaid)
        {
            Assert.NotEqual(broke, world.Citizens.Workplace[citizen]);
        }

        Assert.True(world.Businesses.Rows.IsValid(broke), "the starved grocer went bankrupt first.");
        Assert.True(quit >= unpaid.Length, $"{quit} quit of the {unpaid.Length} never paid.");
    }

    [Fact]
    public void Without_the_key_an_unpaid_worker_stays()
    {
        (World world, Simulation simulation, Handle<Business> paying, Handle<Business> broke) =
            Staffed(quits: false);

        int[] unpaid = WorkersOf(world, broke);
        long shortfall = 0;
        int quit = 0;

        for (int tick = 0; tick < (Threshold + 7) * Ticks.PerDay; tick++)
        {
            Starve(world, broke, paying, simulation.Tick);
            simulation.Step(default);
            shortfall += simulation.LastPayroll.Shortfall;
            quit += simulation.LastPayroll.Quit;
        }

        Assert.True(shortfall > 0, "nobody went unpaid, so this test asserts nothing.");
        Assert.Equal(0, quit);

        foreach (int citizen in unpaid)
        {
            Assert.Equal(broke, world.Citizens.Workplace[citizen]);
            Assert.Equal(0, world.Citizens.DaysUnpaid[citizen]);
        }
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void An_empty_till_stops_hiring_only_where_workers_quit(bool quits, bool hiresWhenBroke)
    {
        (World world, Simulation simulation) = Start(quits);

        (int employer, _) = TwoGrocers(world);
        Handle<Bin> till = world.Businesses.Balance[employer];

        foreach (int worker in world.Workers.Walk(employer))
        {
            world.Dismiss(world.Citizens.Rows.At(worker));
        }

        world.Deposit(till, 1_000_000, simulation.Tick);
        Assert.True(world.HasJob(employer));

        world.Withdraw(till, world.Bins.LevelAt(world.Bins.Rows.Resolve(till)), simulation.Tick);
        Assert.Equal(hiresWhenBroke, world.HasJob(employer));
    }

    /// <summary>Moves everything in one till to another, so the first can pay nobody.</summary>
    private static void Starve(World world, Handle<Business> from, Handle<Business> to, Ticks tick)
    {
        if (!world.Businesses.Rows.TryResolve(from, out int fromSlot)
            || !world.Businesses.Rows.TryResolve(to, out int toSlot))
        {
            return;
        }

        Handle<Bin> source = world.Businesses.Balance[fromSlot];
        long level = world.Bins.LevelAt(world.Bins.Rows.Resolve(source));

        if (level > 0)
        {
            world.Withdraw(source, level, tick);
            world.Deposit(world.Businesses.Balance[toSlot], level, tick);
        }
    }

    /// <summary>Two premised grocers that hold a till.</summary>
    private static (int First, int Second) TwoGrocers(World world)
    {
        int first = -1;

        for (int slot = 0; slot < world.Businesses.Rows.SlotCount; slot++)
        {
            if (!world.Businesses.Rows.IsLive(slot)
                || world.Businesses.Kind[slot] == 0
                || world.Rules.BusinessKind(world.Businesses.Kind[slot]).WagePerDay <= 0
                || world.Businesses.Balance[slot].IsNone)
            {
                continue;
            }

            if (first < 0)
            {
                first = slot;
                continue;
            }

            return (first, slot);
        }

        Assert.Fail("the fixture has fewer than two grocers with room to hire.");
        return (-1, -1);
    }

    private static int[] WorkersOf(World world, Handle<Business> employer)
    {
        var workers = new List<int>();

        foreach (int worker in world.Workers.Walk(world.Businesses.Rows.Resolve(employer)))
        {
            workers.Add(worker);
        }

        Assert.NotEmpty(workers);
        return [.. workers];
    }

    /// <summary>
    /// Two grocers given enough money to hire, after three Days of the job search filling them.
    /// </summary>
    /// <remarks>The deposits create money, so these worlds do not run end-of-run invariants.</remarks>
    private static (World, Simulation, Handle<Business>, Handle<Business>) Staffed(bool quits)
    {
        (World world, Simulation simulation) = Start(quits);
        (int first, int second) = TwoGrocers(world);

        world.Deposit(world.Businesses.Balance[first], 1_000_000, simulation.Tick);
        world.Deposit(world.Businesses.Balance[second], 1_000_000, simulation.Tick);

        for (int tick = 0; tick < 3 * Ticks.PerDay; tick++)
        {
            simulation.Step(default);
        }

        return (world, simulation, world.Businesses.Rows.At(first), world.Businesses.Rows.At(second));
    }

    private static (World World, Simulation Simulation) Start(bool quits)
    {
        string text = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "insolvent.toml"));

        if (quits)
        {
            text = text.Replace(
                "goes_bankrupt_after_days_in_arrears = 21\n",
                $"goes_bankrupt_after_days_in_arrears = 21\nquits_after_days_unpaid = {Threshold}\n",
                StringComparison.Ordinal);
        }

        RulesetLoadResult parsed = RulesetLoader.Parse(text, "insolvent.toml");

        Assert.True(parsed.Ok, parsed.Describe());
        Assert.Equal(quits ? Threshold : 0, parsed.Ruleset!.BusinessKind(2).QuitsAfterDaysUnpaid);

        var key = WorldKey.FromSeed(GoldenFixtures.Seed);
        var world = new World(Population, parsed.Ruleset!, key);
        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        for (int tick = 0; tick < Ticks.PerDay; tick++)
        {
            simulation.Step(default);
        }

        return (world, simulation);
    }
}
