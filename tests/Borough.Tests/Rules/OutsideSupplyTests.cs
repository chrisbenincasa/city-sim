using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// A pool purchase no city seller can fill buys from the Outside at a gate, and its Money leaves
/// the city.
/// </summary>
public sealed class OutsideSupplyTests
{
    private const long Capital = 100_000;

    /// <summary>
    /// A world stepped until its first shopfront stands, with every shop given working capital.
    /// </summary>
    /// <remarks>
    /// A shop the trade Zone Rule raises opens with no Money and holds nothing to sell, so without
    /// capital it could never pay for its first import. The capital enters the supply of record as
    /// an endowment does.
    /// </remarks>
    private static (World World, Simulation Sim) Start(bool verify = false)
    {
        RulesetLoadResult loaded =
            RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "imported.toml"));
        Assert.True(loaded.Ok, loaded.Describe());

        var key = WorldKey.FromSeed(0);
        var world = new World(400, loaded.Ruleset!, key);
        var sim = new Simulation(world, key) { VerifyDecideWritesNothing = verify };
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        for (int t = 0; t < 2048 && Capitalise(world) == 0; t++)
        {
            sim.Step(default);
        }

        Assert.True(Capitalise(world) > 0, "the trade Zone Rule raised no shopfront");
        return (world, sim);
    }

    /// <summary>Endows every shop that holds no Money yet, and counts the shops.</summary>
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

    /// <summary>Every Good a Business holds, which in this Ruleset is shopfront sundries.</summary>
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

    private static bool WaitsOnMarket(World world)
    {
        for (int i = 0; i < world.RuleInstances.Rows.SlotCount; i++)
        {
            if (world.RuleInstances.Rows.IsLive(i)
                && world.Bins.Rows.TryResolve(world.RuleInstances.WaitingOn[i], out int bin)
                && world.Bins.OwnerKind[bin] == BinOwnerKind.District)
            {
                return true;
            }
        }

        return false;
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

    /// <summary>
    /// Steps until shop stock first rises, and returns the stock and Money supply moved in that Tick.
    /// </summary>
    private static (long Stocked, long Issued) FirstImport(World world, Simulation sim, int ticks = 4 * Ticks.PerDay)
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

    [Fact]
    public void A_shop_with_no_city_supplier_imports_at_the_cheapest_gated_price()
    {
        var (world, sim) = Start(verify: true);

        (long stocked, long issued) = FirstImport(world, sim);

        Assert.True(stocked > 0, "no shopfront ever restocked");
        Assert.Equal(-stocked * 100, issued);
        sim.CheckEndOfRun();
    }

    [Fact]
    public void An_edge_without_a_gate_sells_nothing()
    {
        var (world, sim) = Start();
        RazeGates(world, MapEdge.North);

        (long stocked, long issued) = FirstImport(world, sim);

        Assert.True(stocked > 0, "no shopfront ever restocked");
        Assert.Equal(-stocked * 120, issued);
        sim.CheckEndOfRun();
    }

    [Fact]
    public void A_city_with_no_gate_imports_nothing()
    {
        var (world, sim) = Start();
        RazeGates(world, MapEdge.North, MapEdge.East, MapEdge.South, MapEdge.West);

        (long stocked, _) = FirstImport(world, sim);

        Assert.Equal(0, stocked);
        sim.CheckEndOfRun();
    }

    [Fact]
    public void A_gate_raised_later_wakes_the_shops_waiting_on_their_market()
    {
        var (world, sim) = Start();
        var gates = RazeGates(world, MapEdge.North, MapEdge.East, MapEdge.South, MapEdge.West);

        for (int t = 0; t < 4 * Ticks.PerDay && !Staffed(world); t++) { sim.Step(default); }
        Assert.True(Staffed(world), "no shopfront ever held labour");
        Assert.Equal(0, ShopStock(world));
        Assert.True(WaitsOnMarket(world), "no shop was waiting on its market");

        (Handle<Lot> lot, byte kind) = gates.First(g =>
            world.EdgeOf(world.Lots.Rows.Resolve(g.Lot)) == MapEdge.East);
        world.CreateBuilding(lot, kind, world.Tick, WorldKey.FromSeed(0));

        for (int t = 0; t < 16; t++) { sim.Step(default); }
        Assert.False(WaitsOnMarket(world), "the new gate woke no shop");

        (long stocked, long issued) = FirstImport(world, sim, ticks: Ticks.PerDay);

        Assert.True(stocked > 0, "no woken shop imported");
        Assert.Equal(-stocked * 120, issued);
        sim.CheckEndOfRun();
    }
}
