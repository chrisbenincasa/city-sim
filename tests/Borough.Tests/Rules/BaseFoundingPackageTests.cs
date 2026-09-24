using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Input;
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
            Trading(world, ShopTrade(rules)) > 0,
            "no dwelling houses a shop, so the package's shop never opened.");
        Assert.True(
            world.TreasuryBalance()!.Value.Raw < opening - price,
            "the treasury paid for the school and nothing after, so no grant reached it.");

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// Ground, a <see cref="Blocks"/>-square Street grid at the origin corner, housing zoned on every
    /// block, then a gate on the first vacant edge Lot and a school on the first vacant inner one.
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
                        zone: 1));
            }
        }

        return builder;
    }

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

    private static byte ShopTrade(Ruleset rules)
    {
        for (int kind = 1; kind <= rules.KindCount; kind++)
        {
            if (rules.Kind((byte)kind).Houses)
            {
                return rules.Kind((byte)kind).Business;
            }
        }

        throw new InvalidOperationException("the package declares no housing kind.");
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
        RulesetSourceResult result = RulesetSource.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "base", "ruleset.toml"));

        Assert.True(
            result.Ok,
            result.Diagnostics.Count > 0 ? result.Diagnostics[0].ToString() : "the package was refused.");

        return (result.Ruleset!, result.Capture!.ContentHash);
    }

    private static string Committed() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "base", "founding.borough"));
}
