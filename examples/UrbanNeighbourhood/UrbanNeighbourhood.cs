using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Examples;

public static class UrbanNeighbourhood
{
    public const ulong Seed = 62002;
    public const int StartTick = 512;

    public static World Create(string toml)
    {
        var loaded = RulesetLoader.Parse(toml, "urban-neighbourhood.toml");
        if (!loaded.Ok) throw new InvalidDataException(loaded.Describe());
        var key = WorldKey.FromSeed(Seed);
        var world = new World(0, loaded.Ruleset!, key);
        var simulation = new Simulation(world, key);
        while (world.Tick.Raw < StartTick) simulation.Step(TickInput.Empty);
        world.Roads.LayStreet(0, 0, StreetAxis.East);
        world.Roads.LayStreet(0, 0, StreetAxis.North);
        int gap = world.Rules.Lots.StreetHalfWidthTiles;
        for (int i = 0; i < 5; i++)
        {
            int x = gap + 12 * i;
            var lot = world.Lots.Create(new Tiles(x + 6), new Tiles(0), LotTable.Housing, StreetSide.Left);
            int row = world.Lots.Rows.Resolve(lot);
            world.Lots.ParcelEast[row] = world.Lots.FootprintEast[row] = new Tiles(x);
            world.Lots.ParcelNorth[row] = world.Lots.FootprintNorth[row] = new Tiles(gap);
            world.Lots.ParcelWide[row] = world.Lots.FootprintWide[row] = new Tiles(12);
            world.Lots.ParcelDeep[row] = world.Lots.FootprintDeep[row] = new Tiles(16);
            world.Lots.Storeys[row] = 2;
            if (i != 0 && i != 4) continue;
            var home = world.CreateBuilding(lot, 2, world.Tick, key);
            var household = world.CreateHousehold(home, 0);
            world.CreateCitizen(household);
            world.Endow(household, new Money(100));
        }
        var gateLot = world.Lots.Create(new Tiles(0), new Tiles(32), 0, StreetSide.Right);
        world.Frontage.Rebuild(world.Lots, world.Roads.Streets);
        var gate = world.CreateBuilding(gateLot, 3, world.Tick, key);
        for (int i = 0; i < 2; i++)
            if (!world.TryArrive(gate, 0, 2, world.Tick, out _))
                throw new InvalidOperationException("The neighbourhood's arriving Household was refused.");
        simulation.CheckEndOfRun();
        return world;
    }
}
