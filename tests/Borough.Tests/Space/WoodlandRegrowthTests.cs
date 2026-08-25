using Borough.Core;
using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;

namespace Borough.Tests.Space;

/// <summary>
/// Forest coming back — <c>adr/0022</c>'s <em>"regrows on unsealed, unoccupied land — slowly"</em>.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>The rate these exercise is the one <c>adr/0022</c> calls load-bearing by name and left with
/// no owner for the life of the project.</b> Milestone 24 task 8b gave it a key, a cadence, a
/// <c>plans/0002</c> §D1 row and a named ratifier; these are what say it does what the key claims.
/// </para>
/// <para>
/// ⚠ <b>They state their own rate rather than reading the shipped one</b>, on
/// <c>FertilityTests</c>' rule: 512 Days is unratified and expected to move, and a test that fails
/// when a designer retunes a number is a test that has confused the mechanism with its setting. The
/// shipped value has one test of its own that says what it is for.
/// </para>
/// </remarks>
public sealed class WoodlandRegrowthTests
{
    /// <summary>The generator lays the trees and the ceiling together, and they start equal.</summary>
    [Fact]
    public void What_the_seed_laid_is_recorded_beside_what_is_standing()
    {
        World world = Built();
        int compared = 0;

        for (int cell = 0; cell < CellGrid.WorldCellCount; cell += 997)
        {
            Assert.Equal(world.Layers.Woodland.Potential[cell], world.Layers.Woodland.Tiles[cell]);
            compared++;
        }

        Assert.True(compared > 100, "the sample walked almost nothing.");
    }

    /// <summary>Regrowth climbs back to what the seed laid and stops there.</summary>
    /// <remarks>
    /// <b>The ceiling is the seed's and not the bare Cell's, which is the decision this asserts.</b>
    /// Growing toward <see cref="CellGrid.TilesInCell"/> would turn every unbuilt Cell into full
    /// forest given time and erase the property <c>adr/0022</c> put Woodland in for — <em>"a heavily
    /// forested seed is a Materials-rich, farmland-poor start"</em> is about the seed.
    /// </remarks>
    [Fact]
    public void Forest_grows_back_to_what_the_seed_laid_and_no_further()
    {
        MapLayers layers = Regrowing(days: 256);
        Cells east = new(3);
        Cells north = new(4);

        layers.Woodland.Lay(east, north, 400);
        layers.Woodland.Set(east, north, 0);

        for (int pass = 0; pass < 1_000; pass++)
        {
            layers.RegrowWoodland();
        }

        Assert.Equal(400, layers.Woodland.At(east, north));
    }

    /// <summary>And it takes about as long as the file says it will.</summary>
    /// <remarks>
    /// <b>A whole Cell in a whole duration, which is what the key means.</b> The rate is linear and
    /// derived from the duration, so the authored number and the felt number are the same number —
    /// the property task 4's exponential decay does <em>not</em> have, and the reason this curve was
    /// chosen (<c>plans/0042</c> <b>F12</b>).
    /// </remarks>
    [Fact]
    public void A_wholly_cleared_Cell_returns_in_about_the_stated_duration()
    {
        MapLayers layers = Regrowing(days: 512);
        Cells east = new(3);
        Cells north = new(4);

        layers.Woodland.Lay(east, north, CellGrid.TilesInCell);
        layers.Woodland.Set(east, north, 0);

        int passes = 0;

        while (layers.Woodland.At(east, north) < CellGrid.TilesInCell)
        {
            layers.RegrowWoodland();
            passes++;

            Assert.True(passes < 10_000, "the Cell never came back.");
        }

        Assert.Equal(512, passes);
    }

    /// <summary>
    /// ⚠ 🔴 And a thinly wooded Cell comes back sooner, because the step is absolute.
    /// </summary>
    /// <remarks>
    /// <b>The authored duration is a FULL Cell's recovery time and not every Cell's.</b> Forest
    /// advances at so many Tiles a pass wherever it advances, so a Cell the seed left a quarter wooded
    /// returns in a quarter of the stated Days. ***This was found by measuring and not by reading***
    /// — <c>WoodlandRegrowthCostTests</c> put 26.6% of a map's forest back in 65 passes of a 512-Day
    /// rate where the duration alone predicts 12.7% — and it is asserted here so the clause cannot
    /// quietly stop being true.
    /// </remarks>
    [Fact]
    public void A_thinly_wooded_Cell_returns_in_proportionally_less_than_the_stated_duration()
    {
        MapLayers layers = Regrowing(days: 512);
        Cells east = new(3);
        Cells north = new(4);

        layers.Woodland.Lay(east, north, CellGrid.TilesInCell / 4);
        layers.Woodland.Set(east, north, 0);

        int passes = 0;

        while (layers.Woodland.At(east, north) < CellGrid.TilesInCell / 4)
        {
            layers.RegrowWoodland();
            passes++;

            Assert.True(passes < 10_000, "the Cell never came back.");
        }

        Assert.Equal(128, passes);
    }

    /// <summary>Sealing bounds it, because the ground has one budget and not two.</summary>
    /// <remarks>
    /// <c>adr/0159</c>: <c>Woodland + Sealing ≤ TilesInCell</c> is what the two counts <em>mean</em>.
    /// <b>This is the only writer that raises Woodland</b>, so it is the only one that could break the
    /// bound from that side — <see cref="MapLayers.Seal"/> guards the other.
    /// </remarks>
    [Fact]
    public void Regrowth_does_not_take_ground_that_has_been_built_on()
    {
        MapLayers layers = Regrowing(days: 64);
        Cells east = new(3);
        Cells north = new(4);

        layers.Woodland.Lay(east, north, CellGrid.TilesInCell);
        layers.Seal(east, north, 900);

        Assert.Equal(CellGrid.TilesInCell - 900, layers.Woodland.At(east, north));

        for (int pass = 0; pass < 1_000; pass++)
        {
            layers.RegrowWoodland();
        }

        Assert.Equal(CellGrid.TilesInCell - 900, layers.Woodland.At(east, north));
        Assert.Equal(
            CellGrid.TilesInCell,
            layers.Woodland.At(east, north) + layers.Sealing(east, north));
    }

    /// <summary>
    /// Ground unsealed by <see cref="MapLayers.DecaySealing"/> is ground forest takes back.
    /// </summary>
    /// <remarks>
    /// <b>This is the loop the two halves of milestone 24 make together, and neither task closes it
    /// alone.</b> Task 4 lets Sealing fall; 8b lets Woodland rise into the room it leaves. ⚠ <b>The
    /// compound duration is the ratchet <c>adr/0022</c> is protecting</b> — a paved Cell takes its
    /// ground back first and its trees back after — so the two rates are read together or not at all.
    /// </remarks>
    [Fact]
    public void Forest_follows_Sealing_back_down()
    {
        MapLayers layers = Regrowing(days: 64);
        Cells east = new(3);
        Cells north = new(4);
        TerrainRuleset terrain = Recovering(tau: 8);

        layers.Woodland.Lay(east, north, CellGrid.TilesInCell);
        layers.Seal(east, north, CellGrid.TilesInCell);

        Assert.Equal(0, layers.Woodland.At(east, north));

        for (int day = 0; day < 2_000; day++)
        {
            layers.DecaySealing(terrain);
            layers.RegrowWoodland();
        }

        Assert.Equal(0, layers.Sealing(east, north));
        Assert.Equal(CellGrid.TilesInCell, layers.Woodland.At(east, north));
    }

    /// <summary>A Ruleset that says nothing about regrowth is a world where forest never returns.</summary>
    /// <remarks>
    /// <b>Absent means never, and it stays a legitimate world.</b> Every shipped Ruleset but
    /// <c>varied.toml</c> is this world, so a state nothing can reach is not what is being described —
    /// it is what fifteen files currently say.
    /// </remarks>
    [Fact]
    public void A_Ruleset_that_states_no_regrowth_never_puts_forest_back()
    {
        MapLayers layers = new(LayerRuleset.Default);
        Cells east = new(3);
        Cells north = new(4);

        layers.Woodland.Lay(east, north, 500);
        layers.Woodland.Set(east, north, 0);

        for (int pass = 0; pass < 5_000; pass++)
        {
            layers.RegrowWoodland();
        }

        Assert.Equal(0, LayerRates.Default.WoodlandRegrowthDays);
        Assert.Equal(0, layers.Woodland.At(east, north));
    }

    /// <summary>
    /// 🔴 The mirror of task 4's stall: a long duration must not round its step to nothing.
    /// </summary>
    /// <remarks>
    /// <c>RoundDiv(1024, days)</c> is zero for any duration past <see cref="CellGrid.TilesInCell"/>,
    /// so an unfloored rate would put back <em>nothing, for ever</em> while reading as a very slow
    /// one. That is exactly the defect <c>plans/0042</c> <b>F12</b> found in Sealing's decay, wearing
    /// the other sign — and it is guarded twice, here and by a loader refusal, because the first line
    /// of defence for the identical bug turned out to be nobody at all.
    /// </remarks>
    [Theory]
    [InlineData(1)]
    [InlineData(512)]
    [InlineData(1_024)]
    public void Every_admissible_duration_puts_back_at_least_one_Tile(int days)
    {
        LayerRates rates = LayerRates.From(
            landValueTau: 8,
            pollutionDecayTicks: LayerRates.DefaultPollutionDecayTicks,
            pollutionPeriod: 64,
            woodlandRegrowthDays: days);

        Assert.True(rates.WoodlandTilesPerPass >= 1, $"{days} Days puts back nothing.");
    }

    /// <summary>Regrowth has a cadence of its own and does not share Sealing's Tick.</summary>
    /// <remarks>
    /// <b>The offset must differ, and not only for the budget.</b> Sealing's decay opens the room
    /// regrowth fills, so one Tick carrying both would run the two halves of that loop with nothing
    /// able to read the map in between.
    /// </remarks>
    [Fact]
    public void Regrowth_runs_on_its_own_Tick_and_not_on_Sealings()
    {
        LayerSchedule schedule = LayerSchedule.Default;

        Assert.Equal(Ticks.PerDay, schedule.Woodland.Period);
        Assert.NotEqual(schedule.Sealing.Offset, schedule.Woodland.Offset);
        Assert.True(schedule.IsDue(Layer.Woodland, new Ticks((ulong)schedule.Woodland.Offset)));
        Assert.False(schedule.IsDue(Layer.Woodland, new Ticks((ulong)schedule.Sealing.Offset)));
    }

    /// <summary>And Phase 5 runs it, which is what makes the key do anything.</summary>
    [Fact]
    public void The_Layers_phase_is_what_puts_forest_back()
    {
        var world = new World(1_000, Ruleset.Empty.WithLayers(new LayerRuleset(
            LayerSchedule.Default,
            LayerRates.From(8, LayerRates.DefaultPollutionDecayTicks, 64, woodlandRegrowthDays: 64))));

        Cells east = new(3);
        Cells north = new(4);
        int offset = LayerSchedule.Default.Woodland.Offset;

        world.Layers.Woodland.Lay(east, north, 500);
        world.Layers.Woodland.Set(east, north, 0);

        world.Layers.Step(new Ticks((ulong)offset + 1), world.Roads, TerrainRuleset.None);

        Assert.Equal(0, world.Layers.Woodland.At(east, north));

        world.Layers.Step(new Ticks((ulong)offset), world.Roads, TerrainRuleset.None);

        Assert.Equal(IntegerMath.RoundDiv(CellGrid.TilesInCell, 64), world.Layers.Woodland.At(east, north));
    }

    private static World Built()
    {
        WorldKey key = WorldKey.FromSeed(0x5EA1U);
        World world = new(1_000, Ruleset.Empty, key);

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        return world;
    }

    private static MapLayers Regrowing(int days) => new(new LayerRuleset(
        LayerSchedule.Default,
        LayerRates.From(8, LayerRates.DefaultPollutionDecayTicks, 64, woodlandRegrowthDays: days)));

    /// <summary>A <c>[[terrain]]</c> table whose only interesting key is <c>ordinary</c>'s tau.</summary>
    private static TerrainRuleset Recovering(int tau) => TerrainRuleset.From(
        Fixed.One, Fixed.One, Fixed.One, Fixed.One, Fixed.One,
        ordinaryDecayTau: tau,
        rockDecayTau: 0,
        floodplainDecayTau: 0,
        marshDecayTau: 0,
        thinSoilDecayTau: 0);
}
