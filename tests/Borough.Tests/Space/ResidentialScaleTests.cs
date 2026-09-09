using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;
using Borough.Tests.Persistence;

namespace Borough.Tests.Space;

public sealed class ResidentialScaleTests
{
    [Theory]
    [InlineData(16, 32)]
    [InlineData(32, 32)]
    [InlineData(64, 48)]
    [InlineData(256, 256)]
    public void Longer_streets_add_houses_without_enlarging_them(int wide, int deep)
    {
        var rules = Load().Lots;
        var key = WorldKey.FromSeed(0);
        var ground = new BlockGround(2, 3, 100, 200, wide, deep);
        var parcels = new Parcel[rules.ParcelCeiling(ground)];
        int count = rules.Carve(key, BlockPattern.Detached, ground, parcels);
        Assert.True(count >= 8);
        var tiles = new HashSet<(int, int)>();
        for (int i = 0; i < count; i++)
        {
            var p = parcels[i];
            var f = rules.Footprint(key, p, ground, BlockPattern.Detached);
            Assert.Equal(6, f.Wide.Raw * f.Deep.Raw);
            Assert.Equal(2, rules.Height(key, p, BlockPattern.Detached, wide));
            Assert.Equal(1, CapacityRuleset.Holds(12, 25));
            for (int x = p.East.Raw; x < p.East.Raw + p.Wide.Raw; x++)
                for (int y = p.North.Raw; y < p.North.Raw + p.Deep.Raw; y++)
                {
                    Assert.InRange(x, ground.East + 1, ground.East + wide - 2);
                    Assert.InRange(y, ground.North + 1, ground.North + deep - 2);
                    Assert.True(tiles.Add((x, y)), "Parcels overlap");
                }
        }
        var changedRoadCount = rules with { LotsPerSegment = 10 };
        var other = new Parcel[changedRoadCount.ParcelCeiling(ground)];
        Assert.Equal(count, changedRoadCount.Carve(key, BlockPattern.Detached, ground, other));
        Assert.Equal(parcels[..count], other[..count]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    public void Parcel_paint_changes_one_permission_and_survives_rebuild_replay_and_save(int spread)
    {
        var key = WorldKey.FromSeed(0);
        string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "neighbourhood.toml"));
        var rules = RulesetLoader.Parse(text.Replace("[roads]", spread == 0 ? "[roads]" : $"[roads]\nblock_spread_tiles = {spread}",
            StringComparison.Ordinal), "test.toml").Ruleset!;
        var world = new World(64, rules, key);
        var control = new World(64, rules, key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);
        SyntheticCity.PopulateInto(control, key, Ticks.Zero);
        int target = Enumerable.Range(0, world.Lots.Rows.SlotCount)
            .First(i => world.Lots.Rows.IsLive(i) && !world.Lots.IsVacant(i));
        var lots = world.Lots;
        var zones = Enumerable.Range(0, lots.Rows.SlotCount).Select(i => lots.Zone[i]).ToArray();
        var building = lots.BuildingOn(target);
        var commands = new[] { new Command(CommandKind.ZoneParcel,
            new Tiles(lots.ParcelEast[target].Raw + 1), new Tiles(lots.ParcelNorth[target].Raw + 1), LotTable.Trade) };
        var sim = new Simulation(world, key);
        var replay = new Simulation(control, key);
        sim.Step(new TickInput(commands, 0));
        replay.Step(new TickInput(commands, 0));
        Assert.Equal(world.HashState(), control.HashState());
        for (int i = 0; i < zones.Length; i++) Assert.Equal(i == target ? LotTable.Trade : zones[i], lots.Zone[i]);
        Assert.Equal(building, lots.BuildingOn(target));
        var geometry = Geometry(world);
        ulong hash = world.HashState();
        world.RebuildDerived();
        Assert.Equal(geometry, Geometry(world));
        Assert.Equal(hash, world.HashState());
        var save = new MemorySave();
        sim.SaveAtEndOfTick(save);
        sim.Step(default);
        World loaded = SaveFile.Read(save, rules, out SaveHeader header);
        Assert.Equal(world.HashState(), loaded.HashState());
        Assert.Equal(geometry, Geometry(loaded));
        var resumed = new Simulation(loaded, header.Key);
        for (int i = 0; i < 16; i++)
        {
            sim.Step(default); resumed.Step(default);
            Assert.Equal(world.HashState(), loaded.HashState());
        }
    }

    [Theory]
    [InlineData("residential_frontage_tiles = 4", "")]
    [InlineData("residential_frontage_tiles = 4", "residential_frontage_tiles = 1")]
    [InlineData("residential_depth_tiles = 6", "residential_depth_tiles = 0")]
    [InlineData("house_width_tiles = 2", "house_width_tiles = 5")]
    [InlineData("house_depth_tiles = 3", "house_depth_tiles = 7")]
    [InlineData("house_storeys = 2", "house_storeys = 0")]
    [InlineData("house_storeys = 2", "house_storeys = 256")]
    [InlineData("house_storeys = 2", "")]
    public void Incomplete_or_impossible_house_dimensions_are_refused(string before, string after)
    {
        string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "neighbourhood.toml"));
        Assert.False(RulesetLoader.Parse(text.Replace(before, after, StringComparison.Ordinal), "test.toml").Ok);
    }

    [Fact]
    public void Painting_bare_frontage_leaves_the_other_parcels_unzoned()
    {
        var key = WorldKey.FromSeed(0);
        var world = new World(64, Load(), key);
        var sim = new Simulation(world, key);
        sim.Step(new TickInput([new Command(CommandKind.Ground, default, default)], 0));
        var road = new ConnectPayload(StreetAxis.East, ConnectAction.Lay, RoadKind.Street);
        sim.Step(new TickInput([new Command(CommandKind.Connect, new Tiles(32), new Tiles(32), road.Encode())], 0));
        var paint = new Command(CommandKind.ZoneParcel, new Tiles(35), new Tiles(35), LotTable.Housing);
        var builder = new InputLogBuilder(0, new WorldConfiguration(64), 0);
        builder.Append(new Ticks(2), paint);
        var decoded = InputLogCodec.FromText(InputLogCodec.ToText(builder.Build())).Entry(0).Command;
        Assert.Equal(paint.Kind, decoded.Kind);
        Assert.Equal(paint.East, decoded.East);
        Assert.Equal(paint.North, decoded.North);
        Assert.Equal(paint.Zone, decoded.Zone);
        sim.Step(new TickInput([decoded], 0));
        int painted = Enumerable.Range(0, world.Lots.Rows.SlotCount)
            .Count(i => world.Lots.Rows.IsLive(i) && world.Lots.Zone[i] == LotTable.Housing);
        Assert.Equal(1, painted);
        Assert.True(world.Lots.Rows.LiveCount > 1);
        ulong hash = world.HashState();
        Assert.Equal(0, LotSubdivider.PaintParcelAt(world, new Tiles(35), new Tiles(35), LotTable.Housing));
        Assert.Equal(0, LotSubdivider.PaintParcelAt(world, new Tiles(48), new Tiles(48), LotTable.Housing));
        Assert.Equal(hash, world.HashState());
        Assert.Equal(1, LotSubdivider.PaintParcelAt(world, new Tiles(35), new Tiles(35), 0));
        Assert.Equal(0, world.LotsAdmitting.Count(world.Lots, LotTable.Housing));
    }

    [Fact]
    public void House_dimensions_cannot_change_under_existing_addresses()
    {
        string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "neighbourhood.toml"));
        var original = RulesetLoader.Parse(text, "original").Ruleset!;
        var changed = RulesetLoader.Parse(text.Replace("house_storeys = 2", "house_storeys = 1",
            StringComparison.Ordinal), "changed").Ruleset!;
        var world = new World(64, original);
        Assert.Throws<NotSupportedException>(() => world.Adopt(changed, 2, Ticks.Zero, WorldKey.FromSeed(0)));
        Assert.Same(original, world.Rules);
    }

    [Fact]
    public void Demonstration_parcels_address_the_actual_street_on_a_varied_lattice()
    {
        var rules = RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "gridded.toml")).Ruleset!;
        var key = WorldKey.FromSeed(0);
        var world = new World(64, rules, key);
        var simulation = new Simulation(world, key);
        simulation.Step(new TickInput([new Command(CommandKind.Ground, default, default)], 0));
        var ground = BlockGround.At(world.Roads.Streets.Lattice, 3, 4);
        var road = new ConnectPayload(StreetAxis.East, ConnectAction.Lay, RoadKind.Street);
        simulation.Step(new TickInput([new Command(CommandKind.Connect,
            new Tiles(ground.East), new Tiles(ground.North), road.Encode())], 0));
        Assert.True(LotSubdivider.SubdivideBlock(world, 3, 4, LotTable.Housing) > 0);
        var geometry = Geometry(world);
        for (int i = 0; i < world.Lots.Rows.SlotCount; i++)
        {
            if (!world.Lots.Rows.IsLive(i)) continue;
            Assert.Equal(world.Lots.FrontageSlot[i] - 1, Frontage.Locate(world.Roads.Streets,
                world.Lots.East[i], world.Lots.North[i], out _));
        }
        world.RebuildDerived();
        Assert.Equal(geometry, Geometry(world));
    }

    private static (int, int, int, int, byte)[] Geometry(World world) =>
        Enumerable.Range(0, world.Lots.Rows.SlotCount).Where(i => world.Lots.Rows.IsLive(i))
            .Select(i => (world.Lots.FootprintEast[i].Raw, world.Lots.FootprintNorth[i].Raw,
                world.Lots.FootprintWide[i].Raw, world.Lots.FootprintDeep[i].Raw, world.Lots.Storeys[i])).ToArray();

    private static Ruleset Load() => RulesetLoader.Load(
        Path.Combine(AppContext.BaseDirectory, "Rulesets", "neighbourhood.toml")).Ruleset!;
}
