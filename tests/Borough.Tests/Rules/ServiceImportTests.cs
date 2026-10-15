using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Persistence;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>rulesets/serviced.toml</c>: a shop's restock buys accounting from a Hinterland under a contract
/// (<c>adr/0178</c>). North sells accounting at 90 and insurance at 30, east at 110 and 40; sundries
/// cost 100 north and 120 east.
/// </summary>
public sealed class ServiceImportTests
{
    private const long Capital = 100_000;
    private const int North = 0;
    private const int East = 1;

    private static Ruleset Rules()
    {
        RulesetLoadResult loaded =
            RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "serviced.toml"));
        Assert.True(loaded.Ok, loaded.Describe());
        return loaded.Ruleset!;
    }

    private static (World World, Simulation Sim) Start(Ruleset? rules = null)
    {
        var key = WorldKey.FromSeed(0);
        var world = new World(400, rules ?? Rules(), key);
        var sim = new Simulation(world, key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        for (int t = 0; t < 2048 && Capitalise(world) == 0; t++)
        {
            sim.Step(default);
        }

        Assert.True(Capitalise(world) > 0, "the trade Zone Rule raised no shopfront");

        for (int t = 0; t < 4 * Ticks.PerDay && !Staffed(world); t++)
        {
            sim.Step(default);
        }

        Assert.True(Staffed(world), "no shopfront ever held labour");
        return (world, sim);
    }

    private static ResourceId Accounting(World world) => new((ushort)Enumerable
        .Range(1, world.Rules.ResourceCount)
        .First(raw => world.Rules.Family(new ResourceId((ushort)raw)) == ResourceFamily.Service));

    private static int Capitalise(World world)
    {
        world.TryMoneyResource(out ResourceId money);
        int shops = 0;

        for (int business = 0; business < world.Businesses.Rows.SlotCount; business++)
        {
            if (!world.Businesses.Rows.IsLive(business) || !HoldsGoods(world, business, money))
            {
                continue;
            }

            shops++;
            int balance = world.Bins.Rows.Resolve(world.Businesses.Balance[business]);

            if (world.Bins.LevelAt(balance) == 0)
            {
                world.Deposit(world.Bins.Rows.At(balance), Capital, world.Tick);
                world.MoneySupply.Issued[MoneySupplyTable.Slot] += new Money(Capital);
            }
        }

        return shops;
    }

    private static bool HoldsGoods(World world, int business, ResourceId money)
    {
        for (Handle<Bin> at = world.Businesses.BinHead[business];
             world.Bins.Rows.TryResolve(at, out int bin);
             at = world.Bins.OwnerNext[bin])
        {
            if (world.Bins.Resource[bin] != money)
            {
                return true;
            }
        }

        return false;
    }

    private static long ShopStock(World world)
    {
        world.TryMoneyResource(out ResourceId money);
        long total = 0;

        for (int bin = 0; bin < world.Bins.Rows.SlotCount; bin++)
        {
            if (world.Bins.Rows.IsLive(bin)
                && world.Bins.OwnerKind[bin] == BinOwnerKind.Business
                && world.Bins.Resource[bin] != money
                && world.Rules.Family(world.Bins.Resource[bin]) != ResourceFamily.Labour)
            {
                total += world.Bins.LevelAt(bin);
            }
        }

        return total;
    }

    private static bool Staffed(World world)
    {
        for (int bin = 0; bin < world.Bins.Rows.SlotCount; bin++)
        {
            if (world.Bins.Rows.IsLive(bin)
                && world.Rules.Family(world.Bins.Resource[bin]) == ResourceFamily.Labour
                && world.Bins.LevelAt(bin) > 0)
            {
                return true;
            }
        }

        return false;
    }

    private static List<int> StarvedOfService(World world)
    {
        var starved = new List<int>();

        for (int i = 0; i < world.RuleInstances.Rows.SlotCount; i++)
        {
            if (world.RuleInstances.Rows.IsLive(i)
                && world.Bins.Rows.TryResolve(world.RuleInstances.WaitingOn[i], out int bin)
                && world.Bins.OwnerKind[bin] == BinOwnerKind.District
                && world.Bins.Resource[bin] == Accounting(world)
                && world.RuleInstances.IsStarving(i))
            {
                starved.Add(i);
            }
        }

        return starved;
    }

    private static List<int> Providers(World world)
    {
        var providers = new List<int>();

        for (int contract = 0; contract < world.ServiceContracts.Rows.SlotCount; contract++)
        {
            if (world.ServiceContracts.Rows.IsLive(contract))
            {
                providers.Add(world.ServiceContracts.Hinterland[contract]);
            }
        }

        return providers;
    }

    private static List<(Handle<Lot> Lot, byte Kind)> RazeGates(World world, params MapEdge[] edges)
    {
        var razed = new List<(Handle<Lot>, byte)>();

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot)
                && world.IsOutsideConnection(world.Buildings.Kind[slot])
                && edges.Contains(world.EdgeOf(world.Lots.Rows.Resolve(world.Buildings.Lot[slot]))))
            {
                razed.Add((world.Buildings.Lot[slot], world.Buildings.Kind[slot]));
                world.DestroyBuilding(world.Buildings.Rows.At(slot), world.Tick);
            }
        }

        return razed;
    }

    private static void Raise(World world, List<(Handle<Lot> Lot, byte Kind)> gates, MapEdge edge)
    {
        (Handle<Lot> lot, byte kind) = gates.First(g =>
            world.EdgeOf(world.Lots.Rows.Resolve(g.Lot)) == edge);
        world.CreateBuilding(lot, kind, world.Tick, WorldKey.FromSeed(0));
    }

    private static (long Stocked, long Issued) NextImport(World world, Simulation sim, int ticks = 4 * Ticks.PerDay)
    {
        for (int t = 0; t < ticks; t++)
        {
            long stock = ShopStock(world);
            long issued = world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw;

            sim.Step(default);

            long stocked = ShopStock(world) - stock;

            if (stocked > 0)
            {
                return (stocked, world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw - issued);
            }
        }

        return (0, 0);
    }

    private static long Paid(long stocked, long sundries, long accounting, long insurance) =>
        -(stocked * sundries) - (stocked / 8 * (accounting + insurance));

    /// <summary>A world in which firms hold service contracts, for the table audits.</summary>
    internal static World Contracted()
    {
        var (world, sim) = Start();
        NextImport(world, sim);
        Assert.NotEmpty(Providers(world));
        return world;
    }

    [Fact]
    public void A_firm_imports_a_service_from_the_cheapest_gated_hinterland()
    {
        var (world, sim) = Start();
        sim.VerifyDecideWritesNothing = true;

        (long stocked, long issued) = NextImport(world, sim);

        Assert.True(stocked > 0, "no shopfront ever restocked");
        Assert.Equal(Paid(stocked, 100, 90, 30), issued);
        Assert.Contains(North, Providers(world));
        Assert.All(Providers(world), provider => Assert.Equal(North, provider));
        sim.CheckEndOfRun();
    }

    [Fact]
    public void A_firm_keeps_its_provider_after_a_cheaper_one_opens()
    {
        var (world, sim) = Start();
        var gates = RazeGates(world, MapEdge.North);

        (long stocked, long issued) = NextImport(world, sim);
        Assert.True(stocked > 0, "no shopfront ever restocked");
        Assert.Equal(Paid(stocked, 120, 110, 40), issued);

        Raise(world, gates, MapEdge.North);
        int contracted = Providers(world).Count;

        (stocked, issued) = NextImport(world, sim);

        Assert.True(stocked > 0, "no shopfront restocked after the north gate returned");
        Assert.Equal(Paid(stocked, 100, 110, 40), issued);
        Assert.Equal(contracted, Providers(world).Count);
        Assert.All(Providers(world), provider => Assert.Equal(East, provider));
        sim.CheckEndOfRun();
    }

    [Fact]
    public void A_firm_stalls_when_its_hinterland_supply_is_cut_and_a_new_gate_wakes_it()
    {
        var (world, sim) = Start();
        (long stocked, _) = NextImport(world, sim);
        Assert.True(stocked > 0, "no shopfront ever restocked");

        var gates = RazeGates(world, MapEdge.North, MapEdge.East);

        (stocked, _) = NextImport(world, sim, ticks: 2 * Ticks.PerDay);

        Assert.Equal(0, stocked);
        Assert.NotEmpty(StarvedOfService(world));

        Capitalise(world);
        Raise(world, gates, MapEdge.East);

        for (int t = 0; t < 16; t++) { sim.Step(default); }
        Assert.Empty(StarvedOfService(world));

        (stocked, long issued) = NextImport(world, sim, ticks: Ticks.PerDay);

        Assert.True(stocked > 0, "no woken shop imported");
        Assert.True(issued < 0, "the woken import paid nobody outside the city");
        Assert.All(Providers(world), provider => Assert.Equal(East, provider));
        sim.CheckEndOfRun();
    }

    [Fact]
    public void Contracts_survive_save_and_reload()
    {
        Ruleset rules = Rules();
        var (world, sim) = Start(rules);
        RazeGates(world, MapEdge.North);
        NextImport(world, sim);
        Assert.NotEmpty(Providers(world));

        var file = new MemorySave();
        sim.SaveAtEndOfTick(file);
        sim.Step(default);

        World reloaded = SaveFile.Read(file, rules, out SaveHeader header);
        var resumed = new Simulation(reloaded, header.Key) { VerifyDecideWritesNothing = false };

        Assert.Equal(world.HashState(), reloaded.HashState());

        for (int contract = 0; contract < reloaded.ServiceContracts.Rows.SlotCount; contract++)
        {
            if (reloaded.ServiceContracts.Rows.IsLive(contract))
            {
                int buyer = reloaded.Businesses.Rows.Resolve(reloaded.ServiceContracts.Buyer[contract]);
                Assert.Equal(
                    contract, reloaded.FindContract(buyer, reloaded.ServiceContracts.Service[contract]));
            }
        }

        for (int t = 0; t < 2 * Ticks.PerDay; t++)
        {
            sim.Step(default);
            resumed.Step(default);
        }

        Assert.Equal(world.HashState(), reloaded.HashState());
        sim.CheckEndOfRun();
    }

    [Fact]
    public void A_replayed_run_reaches_the_same_state()
    {
        Ruleset rules = Rules();
        ulong Run()
        {
            var (world, sim) = Start(rules);
            var gates = RazeGates(world, MapEdge.North);
            NextImport(world, sim);
            Raise(world, gates, MapEdge.North);

            for (int t = 0; t < Ticks.PerDay; t++) { sim.Step(default); }

            Assert.NotEmpty(Providers(world));
            sim.CheckEndOfRun();
            return world.HashState();
        }

        Assert.Equal(Run(), Run());
    }

    [Fact]
    public void A_closed_firm_leaves_no_contract_behind()
    {
        var (world, sim) = Start();
        NextImport(world, sim);
        int contract = Enumerable.Range(0, world.ServiceContracts.Rows.SlotCount)
            .First(world.ServiceContracts.Rows.IsLive);
        Handle<Business> buyer = world.ServiceContracts.Buyer[contract];

        world.DestroyBusiness(buyer);

        Assert.False(world.ServiceContracts.Rows.IsLive(contract));
    }
}
