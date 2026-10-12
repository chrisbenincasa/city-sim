using System.Text;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Instruments;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>rulesets/base/</c>, founded from Ground by the commands in its <c>founding.borough</c>.
/// </summary>
/// <remarks>
/// The log is what <c>Borough.Headless --ruleset rulesets/base/ruleset.toml --log
/// rulesets/base/founding.borough</c> replays. <see cref="Founding"/> scouts the Lots from a real
/// world and writes the same commands, so a package edit that moves a Lot or the bundle hash fails
/// here with the replacement text in the message.
/// </remarks>
public sealed class BaseFoundingPackageTests
{
    private const ulong Seed = 20_260_924;
    private const int Citizens = 2_000;
    private const int Blocks = 4;
    private const int Days = 12;
    private const int SaveDay = 6;
    private const byte HousingZone = 1;
    private const byte TradeZone = 2;

    [Fact]
    public void The_committed_founding_log_is_the_one_the_package_produces()
    {
        (Ruleset rules, ulong hash) = Package();
        string expected = InputLogCodec.ToText(Founding(rules, hash));

        Assert.True(
            expected == Committed(),
            $"rulesets/base/founding.borough is stale. Replace it with:\n{expected}");
    }

    [Fact]
    public void A_city_founded_from_ground_by_command_houses_arrivals_and_funds_its_school()
    {
        (Ruleset rules, _) = Package();
        InputLog log = InputLogCodec.FromText(Committed());
        Simulation simulation = Replay.Start(log, rules);

        Replay.Trace(simulation, log, new Ticks((ulong)Days * Ticks.PerDay), hashEvery: Ticks.PerDay, []);

        World world = simulation.World;
        byte school = KindServing(rules, Need.Education);
        byte teaching = rules.Kind(school).Business;
        long opening = rules.Treasury!.OpeningBalance.Raw;
        long price = rules.Kind(school).PlacementCost.Raw;

        Assert.Equal(1, Standing(world, school));
        Assert.True(world.Households.Rows.LiveCount > 0, "nobody arrived through the gate.");
        Assert.True(Trading(world, teaching) == 1, "the school stands without its teaching trade.");
        Assert.True(
            Trading(world, Trades(rules).Mill) > 0,
            "no dwelling houses a mill, so the package's mill never opened.");
        Assert.True(
            world.TreasuryBalance()!.Value.Raw < opening - price,
            "the treasury paid for the school and nothing after, so no grant reached it.");

        world.Invariants.RunEndOfRun(world);
    }

    [Fact]
    public void Households_buy_sundries_that_grocers_bake_from_flour_milled_in_the_city()
    {
        (Ruleset rules, _) = Package();
        InputLog log = InputLogCodec.FromText(Committed());
        Simulation simulation = Replay.Start(log, rules);
        (byte mill, byte grocer) = Trades(rules);
        long millRevenue = 0;
        long grocerRevenue = 0;
        List<ulong> hashes = [];

        for (int day = 0; day < Days; day++)
        {
            Replay.Trace(simulation, log, new Ticks(Ticks.PerDay), hashEvery: Ticks.PerDay, hashes);
            millRevenue += Revenue(simulation.World, mill);
            grocerRevenue += Revenue(simulation.World, grocer);
        }

        World world = simulation.World;

        Assert.Equal(DailyHashes(rules, log), hashes);
        Assert.True(Trading(world, grocer) > 0, "the trade zone raised no grocer.");
        Assert.True(millRevenue > 0, "no grocer bought flour from a mill.");
        Assert.True(grocerRevenue > 0, "no Household bought sundries from a grocer.");

        world.Invariants.RunEndOfRun(world);
    }

    [Fact]
    public void The_treasury_grants_new_trades_collects_taxes_and_pays_road_upkeep()
    {
        (Ruleset rules, _) = Package();
        InputLog log = InputLogCodec.FromText(Committed());
        Simulation simulation = Replay.Start(log, rules);
        TreasuryFlows flows = default;

        for (int day = 0; day < Days; day++)
        {
            Replay.Trace(simulation, log, new Ticks(Ticks.PerDay), hashEvery: Ticks.PerDay, []);
            flows = flows.Add(simulation.DrainTreasuryFlows());
        }

        Assert.True(flows.Grant > 0, "no zone-raised trade received an opening grant.");
        Assert.True(flows.Withheld > 0, "no wage paid income tax.");
        Assert.True(flows.ProfitTax > 0, "no Business paid business tax.");
        Assert.True(flows.Upkeep > 0, "the treasury paid no road upkeep.");

        simulation.World.Invariants.RunEndOfRun(simulation.World);
    }

    [Fact]
    public void Replaying_the_founding_log_twice_yields_the_same_city()
    {
        (Ruleset rules, _) = Package();
        InputLog log = InputLogCodec.FromText(Committed());

        Assert.Equal(DailyHashes(rules, log), DailyHashes(rules, log));
    }

    [Fact]
    public void A_founded_city_saved_mid_run_continues_as_the_uninterrupted_one()
    {
        RulesetCapture capture = RulesetCapture.Read(PackagePath()).Capture!;
        Ruleset rules = RulesetSource.Resolve(capture).Ruleset!;
        InputLog log = InputLogCodec.FromText(Committed());
        List<ulong> uninterrupted = DailyHashes(rules, log);

        Simulation first = Replay.Start(log, rules);
        List<ulong> resumed = [];
        Replay.Trace(first, log, new Ticks((ulong)SaveDay * Ticks.PerDay), hashEvery: Ticks.PerDay, resumed);

        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".borough-city");

        try
        {
            CitySave.Write(path, first.World, capture, Seed);
            SavedCity loaded = CitySave.Read(path);
            var second = new Simulation(loaded.World, loaded.Header.Key);

            Assert.Equal(first.World.HashState(), second.World.HashState());

            Replay.Trace(
                second, log, new Ticks((ulong)(Days - SaveDay) * Ticks.PerDay), hashEvery: Ticks.PerDay, resumed);

            Assert.Equal(uninterrupted, resumed);
            second.World.Invariants.RunEndOfRun(second.World);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static List<ulong> DailyHashes(Ruleset rules, InputLog log)
    {
        Simulation simulation = Replay.Start(log, rules);
        List<ulong> hashes = [];

        Replay.Trace(simulation, log, new Ticks((ulong)Days * Ticks.PerDay), hashEvery: Ticks.PerDay, hashes);

        return hashes;
    }

    /// <summary>
    /// Ground, a <see cref="Blocks"/>-square Street grid at the origin corner, trade zoned on three blocks and housing on the rest,
    /// then a gate on the first vacant edge Lot and a school on the first vacant inner one.
    /// </summary>
    /// <remarks>
    /// Zoning follows the Streets by a Tick because the subdivider carves against standing faces.
    /// The gate and school follow the zoning by a Tick, before the housing rule's first pass.
    /// </remarks>
    private static InputLog Founding(Ruleset rules, ulong hash)
    {
        InputLogBuilder builder = Opening(hash);
        Simulation scout = Replay.Start(builder.Build(), rules);

        Replay.Trace(scout, builder.Build(), new Ticks(3), hashEvery: 1, []);

        World world = scout.World;
        int gate = FirstVacant(world, onEdge: true, avoid: -1);
        int school = FirstVacant(world, onEdge: false, avoid: gate);

        builder.Append(
            new Ticks(3),
            Command.Gate(world.Lots.East[gate], world.Lots.North[gate], KindArriving(rules)));
        builder.Append(
            new Ticks(3),
            Command.Service(world.Lots.East[school], world.Lots.North[school], KindServing(rules, Need.Education)));

        return builder.Build();
    }

    private static InputLogBuilder Opening(ulong hash)
    {
        InputLogBuilder builder = new(Seed, new WorldConfiguration(Citizens), hash);
        int block = 32;

        builder.Append(Ticks.Zero, new Command(CommandKind.Ground, default, default));

        for (int row = 0; row <= Blocks; row++)
        {
            for (int column = 0; column < Blocks; column++)
            {
                builder.Append(new Ticks(1), Lay(block, column, row, StreetAxis.East));
            }
        }

        for (int column = 0; column <= Blocks; column++)
        {
            for (int row = 0; row < Blocks; row++)
            {
                builder.Append(new Ticks(1), Lay(block, column, row, StreetAxis.North));
            }
        }

        for (int column = 0; column < Blocks; column++)
        {
            for (int row = 0; row < Blocks; row++)
            {
                builder.Append(
                    new Ticks(2),
                    new Command(
                        CommandKind.Zone,
                        new Tiles((column * block) + (block / 2)),
                        new Tiles((row * block) + (block / 2)),
                        zone: IsTrade(column, row) ? TradeZone : HousingZone));
            }
        }

        return builder;
    }

    private static bool IsTrade(int column, int row) =>
        (column, row) is (1, 1) or (2, 2) or (3, 0);

    private static Command Lay(int block, int column, int row, StreetAxis axis) => new(
        CommandKind.Connect,
        new Tiles(column * block),
        new Tiles(row * block),
        new ConnectPayload(axis, ConnectAction.Lay, RoadKind.Street).Encode());

    private static int FirstVacant(World world, bool onEdge, int avoid)
    {
        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (slot == avoid
                || !world.Lots.Rows.IsLive(slot)
                || !world.Lots.IsVacant(slot)
                || !world.Lots.HasFrontage(slot))
            {
                continue;
            }

            MapEdge edge = world.EdgeOf(slot);

            if (onEdge ? edge != MapEdge.None && world.Rules.TryHinterland(edge, out _) : edge == MapEdge.None)
            {
                return slot;
            }
        }

        throw new InvalidOperationException(
            $"the founded grid has no vacant {(onEdge ? "edge" : "inner")} Lot with frontage.");
    }

    private static byte KindArriving(Ruleset rules)
    {
        for (int kind = 1; kind <= rules.KindCount; kind++)
        {
            if (rules.Kind((byte)kind).ArrivalsPerDay > 0)
            {
                return (byte)kind;
            }
        }

        throw new InvalidOperationException("the package declares no gate kind.");
    }

    private static byte KindServing(Ruleset rules, Need need)
    {
        for (int kind = 1; kind <= rules.KindCount; kind++)
        {
            if (rules.Kind((byte)kind).Serves == need)
            {
                return (byte)kind;
            }
        }

        throw new InvalidOperationException($"the package declares no kind serving {need}.");
    }

    private static (byte Mill, byte Grocer) Trades(Ruleset rules) => (Trade(rules, "mill"), Trade(rules, "grocer"));

    private static byte Trade(Ruleset rules, string id)
    {
        ulong key = ContentHash.Of(Encoding.UTF8.GetBytes(id));

        for (int kind = 1; kind <= rules.BusinessKindCount; kind++)
        {
            if (rules.BusinessKindKey((byte)kind) == key)
            {
                return (byte)kind;
            }
        }

        throw new InvalidOperationException($"the package declares no {id} trade.");
    }

    private static long Revenue(World world, byte trade)
    {
        long total = 0;

        for (int slot = 0; slot < world.Businesses.Rows.SlotCount; slot++)
        {
            if (world.Businesses.Rows.IsLive(slot) && world.Businesses.Kind[slot] == trade)
            {
                total += world.Businesses.DayRevenue[slot];
            }
        }

        return total;
    }

    private static int Standing(World world, byte kind)
    {
        int count = 0;

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot) && world.Buildings.Kind[slot] == kind)
            {
                count++;
            }
        }

        return count;
    }

    private static int Trading(World world, byte trade)
    {
        int count = 0;

        for (int slot = 0; slot < world.Businesses.Rows.SlotCount; slot++)
        {
            if (world.Businesses.Rows.IsLive(slot) && world.Businesses.Kind[slot] == trade)
            {
                count++;
            }
        }

        return count;
    }

    private static (Ruleset Rules, ulong Hash) Package()
    {
        RulesetSourceResult result = RulesetSource.Load(PackagePath());

        Assert.True(
            result.Ok,
            result.Diagnostics.Count > 0 ? result.Diagnostics[0].ToString() : "the package was refused.");

        return (result.Ruleset!, result.Capture!.ContentHash);
    }

    private static string PackagePath() =>
        Path.Combine(AppContext.BaseDirectory, "Rulesets", "base", "ruleset.toml");

    private static string Committed() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "base", "founding.borough"));
}
