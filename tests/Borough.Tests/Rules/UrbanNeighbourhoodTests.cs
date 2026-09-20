using Borough.Core;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Examples;
using Borough.Formats;
using Borough.Shell;

namespace Borough.Tests.Rules;

public sealed class UrbanNeighbourhoodTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Permission_then_sustained_mismatch_builds_one_home_and_save_continues_identically(bool earlyPermission)
    {
        string source = Path.Combine(AppContext.BaseDirectory, "Rulesets", "urban-neighbourhood.toml");
        RulesetCapture capture = RulesetCapture.Read(source).Capture!;
        var world = UrbanNeighbourhood.Create(File.ReadAllText(source));
        var simulation = new Simulation(world, world.Key) { VerifyDecideWritesNothing = true };
        Assert.Equal(6, world.Citizens.Rows.LiveCount);
        Assert.Equal(2, world.UnplacedPool.Count);
        while (world.Tick.Raw < 640) simulation.Step(TickInput.Empty);
        Command[] commands = [new(CommandKind.Zone, new Tiles(16), new Tiles(10), 1)];
        if (earlyPermission) simulation.Step(new TickInput(commands, 0));
        Assert.Equal(3, world.Buildings.Rows.LiveCount);
        ulong before = world.HashState();
        var reading = HousingSearchEvidence.Read(world, 0);
        Assert.Equal(HousingSearchReason.Preference, reading.Current);
        Assert.True(reading.Observed);
        Assert.True(reading.Fresh);
        Assert.False(reading.Persistent);
        Assert.Equal(before, world.HashState());
        string path = Path.Combine(Path.GetTempPath(), $"urban-neighbourhood-{Guid.NewGuid():N}.borough-city");
        try
        {
            CitySave.Write(path, world, capture, UrbanNeighbourhood.Seed);
            var restored = CitySave.Read(path).World;
            var resumed = new Simulation(restored, restored.Key) { VerifyDecideWritesNothing = true, RouteWorkerCount = 2 };
            Assert.Equal(reading, HousingSearchEvidence.Read(restored, 0));
            while (world.Tick.Raw < (earlyPermission ? 760UL : 900UL))
            {
                simulation.Step(TickInput.Empty);
                resumed.Step(TickInput.Empty);
            }
            Assert.Equal(3, world.Buildings.Rows.LiveCount);
            Assert.Equal(2, world.UnplacedPool.Count);
            Assert.Equal(!earlyPermission, HousingSearchEvidence.Read(world, 0).Persistent);
            simulation.Step(new TickInput(commands, 0));
            resumed.Step(new TickInput(commands, 0));
            while (world.Tick.Raw < 1400)
            {
                simulation.Step(TickInput.Empty);
                resumed.Step(TickInput.Empty);
                Assert.Equal(world.HashState(), restored.HashState());
            }
            Assert.Equal(4, world.Buildings.Rows.LiveCount);
            Assert.Equal(0, world.UnplacedPool.Count);
            Assert.Equal(6, world.Citizens.Rows.LiveCount);
            for (int row = 0; row < world.Buildings.Rows.SlotCount; row++)
            {
                if (!world.Buildings.Rows.IsLive(row)) continue;
                byte kind = world.Buildings.Kind[row];
                if (kind == 2) Assert.Equal(1, world.Tenants(row));
                if (kind == 1) Assert.Equal(2, world.Tenants(row));
            }
            simulation.CheckEndOfRun();
            resumed.CheckEndOfRun();
        }
        finally { File.Delete(path); }
    }
}
