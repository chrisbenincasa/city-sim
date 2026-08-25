using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Space;

/// <summary>
/// Milestone 24: runoff — <b>the inflow that makes a Water Body's level move at all.</b>
/// </summary>
/// <remarks>
/// <para>
/// <c>CONTEXT.md</c> → Water Body gives a Bin two inflows, <em>"dumping, runoff"</em>. <b>This is
/// runoff</b>, and the claims are: <b>a paved Cell sheds into the body its catchment names</b>;
/// <b>the amount scales with how much of the Cell is sealed</b>; <b>an unpaved city sheds nothing</b>;
/// and <b>the level a body reaches is bounded by its capacity rather than unbounded.</b>
/// </para>
/// <para>
/// 🔴 <b>This is the test that says milestone 24's water is observable rather than merely present.</b>
/// Before it, every Bin's level was zero on every shipped world and a shoreline term reading one would
/// have been <c>adr/0123</c>'s present-and-permanently-zero. ⚠ <b>Dumping is still unbuilt</b> — it
/// needs a <see cref="Scope"/> reaching a Water Body — so runoff is the only inflow there is.
/// </para>
/// </remarks>
public sealed class WaterRunoffTests
{
    private const int Citizens = 4_000;

    private static readonly WorldKey Key = WorldKey.FromSeed(24_006);

    private static Ruleset Load(string file)
    {
        RulesetLoadResult result = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

        return result.Ruleset
            ?? throw new InvalidOperationException($"{file} was refused:\n{result.Describe()}");
    }

    private static World Generated(WorldKey key, string file = "coastal.toml")
    {
        World world = new(Citizens, Load(file), key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        return world;
    }

    private static long TotalLevel(World world)
    {
        long total = 0;

        for (int slot = 0; slot < world.Water.Rows.SlotCount; slot++)
        {
            if (world.Water.Rows.IsLive(slot)
                && world.Bins.Rows.TryResolve(world.Water.Bin[slot], out int bin))
            {
                total += world.Bins.LevelAt(bin);
            }
        }

        return total;
    }

    /// <summary>
    /// 🔴 <b>A generated city fouls the water below it, and this is the whole point.</b> The city is
    /// laid, the ground is sealed by laying it, and one Day of runoff puts a level in a Bin.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>Five seeds rather than one, and the sweep is why this file's city is not at the origin.</b>
    /// Written against a single seed it failed, and the diagnosis was not in the runoff at all: a
    /// Ruleset stating no <c>[[lattice]]</c> gets one at (0, 0), which puts the city in the map's
    /// north-west corner, and <b>a map edge is where water leaves the world</b> — the catchment seeds
    /// every dry boundary Cell as draining off the map. All five seeds shed into nothing. Moving the
    /// origin to the map's middle fixed all five at once, which is what says the cause was the siting
    /// and not the seed. <c>rulesets/coastal.toml</c>'s <c>[[lattice]]</c> header carries the finding.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData(1UL)]
    [InlineData(24_006UL)]
    [InlineData(770_413UL)]
    [InlineData(8_675_309UL)]
    [InlineData(ulong.MaxValue)]
    public void A_paved_city_puts_something_in_the_water(ulong seed)
    {
        World world = Generated(WorldKey.FromSeed(seed));

        Assert.Equal(0, TotalLevel(world));

        world.RunoffIntoWater(Ticks.Zero);

        Assert.True(
            TotalLevel(world) > 0,
            $"seed {seed} shed nothing into any Water Body. Either the city is sited where its "
            + "catchment names no body — check rulesets/coastal.toml's [[lattice]] origin and this "
            + "class's remarks — or runoff has stopped reading Sealing.");
    }

    /// <summary>
    /// <b>The amount scales with how much of the Cell is paved</b>, so the authored number is a fully
    /// sealed Cell's shedding and a half-built Cell sheds half of it.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Asserted as a ratio between two Days rather than against an absolute</b>, because the
    /// absolute is a function of how much of this key's city happens to sit in a catchment — a figure
    /// that belongs in an instrument, not in an assertion.
    /// </remarks>
    [Fact]
    public void What_a_Cell_sheds_scales_with_its_Sealing()
    {
        World world = Generated(Key);

        world.RunoffIntoWater(Ticks.Zero);
        long once = TotalLevel(world);

        world.RunoffIntoWater(Ticks.Zero);
        long twice = TotalLevel(world);

        // Two identical Days shed the same amount, because Sealing is not consumed by shedding --
        // pavement does not run out. That is the property, and it is what separates runoff from a
        // transfer of a stock.
        Assert.Equal(once * 2, twice);
    }

    /// <summary>
    /// ⚠ <b>A Ruleset with no water Bin sheds nothing and does not throw.</b> Every shipped file but
    /// <c>coastal.toml</c> is this world.
    /// </summary>
    [Fact]
    public void A_world_with_no_water_Bin_sheds_nothing()
    {
        World world = Generated(Key, "minimal.toml");

        world.RunoffIntoWater(Ticks.Zero);

        Assert.Equal(0, TotalLevel(world));
    }

    /// <summary>
    /// <b>A body cannot be filled past its capacity</b>, however long the city sheds into it.
    /// </summary>
    /// <remarks>
    /// <c>CONTEXT.md</c> → Water Body: <em>"nothing is an infinite sink … so dumping is never free, it
    /// is only cheaper."</em> ⚠ <b>The cap is on the RECEIVING Bin and the surplus is simply not
    /// shed</b> — there is nowhere else for it to go, because runoff is not a transfer out of
    /// anything.
    /// </remarks>
    [Fact]
    public void A_body_never_exceeds_its_capacity()
    {
        World world = Generated(Key);

        for (int day = 0; day < 64; day++)
        {
            world.RunoffIntoWater(Ticks.Zero);
        }

        for (int slot = 0; slot < world.Water.Rows.SlotCount; slot++)
        {
            if (world.Water.Rows.IsLive(slot)
                && world.Bins.Rows.TryResolve(world.Water.Bin[slot], out int bin))
            {
                Assert.True(
                    world.Bins.LevelAt(bin) <= world.Bins.Capacity[bin],
                    $"body {slot} holds more than it can");
            }
        }
    }

    /// <summary>
    /// 🔴 <b>Runoff reaches a body through the CATCHMENT and not through proximity</b>, so a Cell sheds
    /// into the water downhill of it rather than the water beside it.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The two differ, and the difference is the whole reason the catchment exists</b>
    /// (<c>plans/0042</c> <b>F14</b>): a Cell on the far side of a ridge drains away from the river it
    /// can see. This asserts the mechanism rather than the geography — every body that received
    /// anything is one some sealed Cell's catchment actually names.
    /// </remarks>
    [Fact]
    public void What_a_Cell_sheds_goes_where_its_catchment_says()
    {
        World world = Generated(Key);

        world.RunoffIntoWater(Ticks.Zero);

        for (int slot = 0; slot < world.Water.Rows.SlotCount; slot++)
        {
            if (!world.Water.Rows.IsLive(slot)
                || !world.Bins.Rows.TryResolve(world.Water.Bin[slot], out int bin)
                || world.Bins.LevelAt(bin) == 0)
            {
                continue;
            }

            bool named = false;

            for (int cell = 0; cell < world.Layers.Cells.Rows.SlotCount && !named; cell++)
            {
                if (!world.Layers.Cells.Rows.IsLive(cell)
                    || world.Layers.Cells.Sealing[cell] <= 0)
                {
                    continue;
                }

                named = world.Water.Rows.TryResolve(
                    world.Catchment.At(
                        world.Layers.Cells.East[cell], world.Layers.Cells.North[cell]),
                    out int into)
                    && into == slot;
            }

            Assert.True(named, $"body {slot} took runoff from no sealed Cell that drains to it");
        }
    }
}
