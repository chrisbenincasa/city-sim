using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Formats;

namespace Borough.Tests.Rules;

public sealed class PopulatorBandTests
{
    [Fact]
    public void The_populator_raises_no_dwelling_on_a_band_that_refuses_housing()
    {
        var loaded = RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "rowhouses.toml"));
        Assert.True(loaded.Ok, loaded.Describe());
        var key = WorldKey.FromSeed(0);
        var world = new World(1000, loaded.Ruleset!, key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        int raised = 0;
        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (!world.Buildings.Rows.IsLive(slot) || !world.Lots.Rows.TryResolve(world.Buildings.Lot[slot], out int lot)) continue;
            raised++;
            Assert.NotEqual(0, world.BandAdmitting(lot) & LotTable.Housing);
        }

        Assert.True(raised > 0);
    }
}
