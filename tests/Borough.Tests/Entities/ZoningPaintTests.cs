using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Persistence;

namespace Borough.Tests.Entities;

public sealed class ZoningPaintTests
{
    [Theory]
    [InlineData(LotTable.Trade)]
    [InlineData(0)]
    public void Repaint_updates_only_the_target_block_and_preserves_buildings(ushort permission)
    {
        var world = City();
        int target = Enumerable.Range(0, world.Lots.Rows.SlotCount)
            .First(i => world.Lots.Rows.IsLive(i) && !world.Lots.IsVacant(i));
        Assert.True(Frontage.BlockOf(world.Roads.Streets, world.Lots.East[target], world.Lots.North[target],
            (StreetSide)world.Lots.Side[target], out int column, out int row));
        ushort[] zones = Enumerable.Range(0, world.Lots.Rows.SlotCount).Select(i => world.Lots.Zone[i]).ToArray();
        var buildings = Enumerable.Range(0, world.Lots.Rows.SlotCount).Select(i => world.Lots.BuildingOn(i)).ToArray();
        _ = world.LotsAdmitting.Count(world.Lots, LotTable.Housing);
        var tile = world.Roads.Streets.IntersectionTile(column, row);
        LotSubdivider.PaintAt(world, tile.East, tile.North, permission);
        int housing = 0;
        for (int i = 0; i < zones.Length; i++)
        {
            if (!world.Lots.Rows.IsLive(i)) continue;
            bool same = Frontage.BlockOf(world.Roads.Streets, world.Lots.East[i], world.Lots.North[i],
                (StreetSide)world.Lots.Side[i], out int c, out int r) && c == column && r == row;
            Assert.Equal(same ? permission : zones[i], world.Lots.Zone[i]);
            Assert.Equal(buildings[i], world.Lots.BuildingOn(i));
            if ((world.Lots.Zone[i] & LotTable.Housing) != 0) housing++;
        }
        Assert.Equal(housing, world.LotsAdmitting.Count(world.Lots, LotTable.Housing));
        ulong hash = world.HashState();
        LotSubdivider.PaintAt(world, tile.East, tile.North, permission);
        Assert.Equal(hash, world.HashState());
    }

    [Fact]
    public void Repaint_commands_replay_and_continue_after_save()
    {
        var world = City();
        var control = City();
        var key = WorldKey.FromSeed(1);
        var simulation = new Simulation(world, key);
        var replay = new Simulation(control, key);
        var commands = new[] { new Command(CommandKind.Zone, new Tiles(36), new Tiles(36), LotTable.Trade),
            new Command(CommandKind.Zone, new Tiles(68), new Tiles(36), 0) };
        simulation.Step(new TickInput(commands, 0));
        replay.Step(new TickInput(commands, 0));
        Assert.Equal(control.HashState(), world.HashState());
        var file = new MemorySave();
        simulation.SaveAtEndOfTick(file);
        simulation.Step(default);
        replay.Step(default);
        World loaded = SaveFile.Read(file, world.Rules, out SaveHeader header);
        var resumed = new Simulation(loaded, header.Key);
        Assert.Equal(world.HashState(), loaded.HashState());
        for (int tick = 0; tick < 128; tick++)
        {
            simulation.Step(default); replay.Step(default); resumed.Step(default);
            Assert.Equal(control.HashState(), world.HashState());
            Assert.Equal(world.HashState(), loaded.HashState());
        }
    }

    [Fact]
    public void Zoning_ground_after_a_freeform_street_is_laid_cuts_lots_along_that_street()
    {
        var rules = RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml")).Ruleset!;
        var world = new World(1_000, rules, WorldKey.FromSeed(0));
        SyntheticCity.PopulateInto(world, WorldKey.FromSeed(0), Ticks.Zero);
        StreetGrid streets = world.Roads.Streets;
        (int column, int row) = Enumerable.Range(0, streets.Blocks * streets.Blocks)
            .Select(i => (i % streets.Blocks, i / streets.Blocks))
            .First(b => world.LandPermissions.Summary(world.BlockGroundRectangle(b.Item1, b.Item2)).AnyUses == 0);
        var south = streets.IntersectionTile(column, row);
        var north = streets.IntersectionTile(column + 1, row + 1);
        Assert.True(world.Roads.BulldozeStreet(column, row, StreetAxis.East));
        Assert.True(world.Roads.BulldozeStreet(column, row + 1, StreetAxis.East));
        Assert.True(world.Roads.BulldozeStreet(column, row, StreetAxis.North));
        Assert.True(world.Roads.BulldozeStreet(column + 1, row, StreetAxis.North));
        world.RebuildDerived();
        LotSubdivider.Resubdivide(world);

        Assert.Equal(StreetLayRefusal.None,
            world.LayStreet(south.East.Raw, south.North.Raw, north.East.Raw, north.North.Raw, 0));
        int[] diagonal = Enumerable.Range(0, world.Roads.Segments.Rows.SlotCount)
            .Where(s => world.Roads.Segments.Rows.IsLive(s) && (RoadKind)world.Roads.Segments.Kind[s] == RoadKind.Street
                && world.Roads.Segments.Centerline[s].A.East != world.Roads.Segments.Centerline[s].B.East
                && world.Roads.Segments.Centerline[s].A.North != world.Roads.Segments.Centerline[s].B.North)
            .ToArray();
        Assert.NotEmpty(diagonal);
        Assert.Equal(0, Fronting(world, diagonal));

        LotSubdivider.PaintAt(world, new Tiles(south.East.Raw + 4), new Tiles(south.North.Raw + 20), LotTable.Housing);

        Assert.True(Fronting(world, diagonal) > 0);
    }

    private static int Fronting(World world, int[] segments) =>
        Enumerable.Range(0, world.Lots.Rows.SlotCount)
            .Count(i => world.Lots.Rows.IsLive(i) && segments.Contains(world.Lots.FrontageOn(i)));

    private static World City()
    {
        var rules = RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml")).Ruleset!;
        var world = new World(64, rules, WorldKey.FromSeed(1));
        SyntheticCity.PopulateInto(world, WorldKey.FromSeed(1), Ticks.Zero);
        return world;
    }
}
