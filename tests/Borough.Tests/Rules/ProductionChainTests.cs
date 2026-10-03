using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Rules;

public sealed class ProductionChainTests
{
    private static string Text()
    {
        string text = LabourTests.Text();

        text = Swap(text, """
            [[resource]]
            name = "money"
            """, """
            [[resource]]
            name = "flour"
            family = "good"

            [[resource]]
            name = "money"
            """);

        text = Swap(text, """
                { resource = "repairs",  capacity = 4 },
            ]
            houses   = true
            premises = true
            parked = true
            """, """
                { resource = "repairs",  capacity = 4 },
                { resource = "flour",    capacity = 256, owner = "business" },
                { resource = "money",    owner = "business" },
                { resource = "labour",   owner = "business" },
            ]
            houses   = true
            premises = true
            parked = true
            """);

        text = Swap(text, """
                { resource = "sundries", capacity = 1024, owner = "business" },
            """, """
                { resource = "sundries", capacity = 1024, owner = "business" },
                { resource = "flour",    capacity = 64, owner = "business" },
            """);

        text = Swap(text, """
            inputs  = [ { scope = "local", resource = "labour", amount = 1 } ]
            outputs = [ { scope = "local", resource = "sundries", amount = 8 } ]
            """, """
            inputs  = [ { scope = "local", resource = "labour", amount = 1 }, { scope = "pool", resource = "flour", amount = 1 } ]
            outputs = [ { scope = "local", resource = "sundries", amount = 8 } ]

            [[rule]]
            name    = "mill"
            kind    = "dwelling"
            rate    = 8
            apply   = { min = 1, max = 4 }
            inputs  = [ { scope = "local", resource = "labour", amount = 1 } ]
            outputs = [ { scope = "local", resource = "flour", amount = 4 } ]
            """);

        text = text.Replace(
            "  { resource = \"repairs\",  price = 250 },\n",
            "  { resource = \"repairs\",  price = 250 },\n  { resource = \"flour\", price = 20 },\n");

        return text.Replace(
            "  { resource = \"repairs\",  price = 200 },\n",
            "  { resource = \"repairs\",  price = 200 },\n  { resource = \"flour\", price = 20 },\n");
    }

    private static string Swap(string text, string from, string to)
    {
        Assert.Contains(from, text);

        return text.Replace(from, to);
    }

    private static readonly ResourceId Sundries = new(1);
    private static readonly ResourceId Flour = new(3);
    private const byte Mill = 1;
    private const byte Grocer = 2;
    private const long Capital = 1_000_000;

    [Fact]
    public void A_grocer_buys_milled_flour_sells_to_households_pays_staff_loses_them_and_recovers()
    {
        RulesetLoadResult loaded = RulesetLoader.Parse(Text(), "chain.toml");
        Assert.True(loaded.Ok, loaded.Describe());
        var key = WorldKey.FromSeed(0);
        var world = new World(400, loaded.Ruleset!, key);
        var sim = new Simulation(world, key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        long millRevenue = 0;
        long grocerRevenue = 0;
        long paid = 0;

        for (int day = 0; day < 8; day++)
        {
            Endow(world);
            paid += RunDay(sim);
            millRevenue += Revenue(world, Mill);
            grocerRevenue += Revenue(world, Grocer);
        }

        Assert.True(millRevenue > 0, "no grocer paid a mill for flour");
        Assert.True(grocerRevenue > 0, "no Household bought from a grocer");
        Assert.True(paid > 0, "no payroll was paid");

        int grocer = Busiest(world);
        Handle<Business> business = world.Businesses.Rows.At(grocer);
        int labour = world.Bins.Rows.Resolve(world.LabourBinOf(grocer));
        int stock = StockOf(world, grocer, Sundries);
        ushort lossDay = BusinessAccounts.DayOf(world.Tick);

        foreach (int citizen in StaffOf(world, business))
        {
            world.Dismiss(world.Citizens.Rows.At(citizen));
        }

        Assert.Equal(0, world.Workers.Length(grocer));

        bool idleSeen = false;
        bool rehired = false;
        bool producedAfter = false;

        for (int day = 0; day < 16 && !(rehired && producedAfter && PaidSince(world, business, lossDay)); day++)
        {
            Endow(world);

            for (int t = 0; t < Ticks.PerDay; t++)
            {
                bool unstaffed = world.Workers.Length(grocer) == 0;
                long labourBefore = world.Bins.LevelAt(labour);
                long stockBefore = world.Bins.LevelAt(stock);

                sim.Step(default);

                if (unstaffed)
                {
                    idleSeen = true;
                    Assert.True(world.Bins.LevelAt(labour) <= labourBefore, "labour arrived at a grocer with no staff");
                }
                else
                {
                    rehired = true;
                    producedAfter |= world.Bins.LevelAt(stock) > stockBefore;
                }
            }
        }

        Assert.True(idleSeen);
        Assert.True(rehired, "the grocer never rehired");
        Assert.True(producedAfter, "the rehired grocer never produced");
        Assert.True(PaidSince(world, business, lossDay), "no rehired worker was paid");
        sim.CheckEndOfRun();
    }

    private static long RunDay(Simulation sim)
    {
        long paid = 0;

        for (int t = 0; t < Ticks.PerDay; t++)
        {
            sim.Step(default);
            paid = Math.Max(paid, sim.LastPayroll.Paid);
        }

        return paid;
    }

    private static void Endow(World world)
    {
        for (int b = 0; b < world.Businesses.Rows.SlotCount; b++)
        {
            if (!world.Businesses.Rows.IsLive(b)) { continue; }

            int balance = world.Bins.Rows.Resolve(world.Businesses.Balance[b]);

            if (world.Bins.LevelAt(balance) == 0)
            {
                world.Deposit(world.Bins.Rows.At(balance), Capital, world.Tick);
                world.MoneySupply.Issued[MoneySupplyTable.Slot] += new Money(Capital);
            }
        }
    }

    private static long Revenue(World world, byte kind)
    {
        long total = 0;

        for (int b = 0; b < world.Businesses.Rows.SlotCount; b++)
        {
            if (world.Businesses.Rows.IsLive(b) && world.Businesses.Kind[b] == kind) { total += world.Businesses.DayRevenue[b]; }
        }

        return total;
    }

    private static int Busiest(World world)
    {
        int busiest = Rows.NoSlot;

        for (int b = 0; b < world.Businesses.Rows.SlotCount; b++)
        {
            if (world.Businesses.Rows.IsLive(b) && world.Businesses.Kind[b] == Grocer
                && (busiest == Rows.NoSlot || world.Workers.Length(b) > world.Workers.Length(busiest)))
            {
                busiest = b;
            }
        }

        Assert.NotEqual(Rows.NoSlot, busiest);
        Assert.True(world.Workers.Length(busiest) > 0);

        return busiest;
    }

    private static int StockOf(World world, int business, ResourceId resource)
    {
        for (Handle<Bin> at = world.Businesses.BinHead[business]; !at.IsNone;)
        {
            int slot = world.Bins.Rows.Resolve(at);

            if (world.Bins.Resource[slot] == resource) { return slot; }

            at = world.Bins.OwnerNext[slot];
        }

        throw new InvalidOperationException($"business holds no Bin of resource {resource.Raw}");
    }

    private static int[] StaffOf(World world, Handle<Business> business) =>
        Enumerable.Range(0, world.Citizens.Rows.SlotCount)
            .Where(c => world.Citizens.Rows.IsLive(c) && world.Citizens.Workplace[c] == business)
            .ToArray();

    private static bool PaidSince(World world, Handle<Business> business, ushort day) =>
        StaffOf(world, business).Any(c => world.Citizens.LastPaidDay[c] > day);
}
