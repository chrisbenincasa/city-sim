using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Persistence;

namespace Borough.Tests.Space;

/// <summary>
/// Milestone 24 task 6b: a Water Body's Bin, its capacity, and the water leaving it.
/// </summary>
/// <remarks>
/// <para>
/// <c>adr/0160</c>, <c>CONTEXT.md</c> → Water Body. The claims: <b>one Bin per body holding one
/// <c>Utility</c>-family Resource</b>; <b>capacity is the body's size</b>, which is what makes
/// pollution a debt in a small body and a rent in a large one; <b>outflow is the body's exits</b>, so
/// a pond fills and a sea flushes with no taxonomy of water types; and <b>a full body downstream backs
/// the water up rather than destroying it.</b>
/// </para>
/// <para>
/// 🔴 <b>Nothing in the build puts anything in a Water Body's Bin, so every test here deposits by
/// hand.</b> No <c>Scope</c> reaches a Water Body — <c>adr/0160</c> names that as <c>adr/0070</c>'s
/// *unbuilt* — which means the outflow mechanism is exercised only from this file, and on every
/// shipped world it moves nothing because every level is zero. ⚠ <b>That is stated rather than hidden
/// behind a passing suite</b>: a green run here is not evidence that any city's water does anything.
/// </para>
/// </remarks>
public sealed class WaterBinTests
{
    private const int Citizens = 1_000;

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

    /// <summary>
    /// Puts up to <paramref name="want"/> into a body's Bin and answers what actually went in.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Capacity is the body's SIZE, so a fixed amount overfills a small body</b> — the first
    /// version of these tests deposited 500,000 into every body and tripped
    /// <c>BinLevelIsWithinCapacity</c> on a body of a few Cells. That is the debt-versus-rent gradient
    /// working, met from the test side.
    /// </remarks>
    private static long Fill(World world, int body, long want)
    {
        int bin = world.Bins.Rows.Resolve(world.Water.Bin[body]);
        long space = world.Bins.SpaceAt(bin);
        long amount = want < space ? want : space;

        if (amount > 0)
        {
            world.Deposit(world.Water.Bin[body], amount, Ticks.Zero);
        }

        return amount;
    }

    private static int LiveBodies(World world)
    {
        int live = 0;

        for (int slot = 0; slot < world.Water.Rows.SlotCount; slot++)
        {
            if (world.Water.Rows.IsLive(slot)) { live++; }
        }

        return live;
    }

    /// <summary>Every live Water Body owns exactly one Bin, and it holds the declared Resource.</summary>
    [Fact]
    public void Every_body_owns_one_Bin_holding_the_declared_Resource()
    {
        World world = Generated(Key);

        Assert.True(LiveBodies(world) > 0, "coastal.toml laid no Water Bodies");
        Assert.True(world.Rules.Water.HasBin, "coastal.toml states no water Bin");

        for (int slot = 0; slot < world.Water.Rows.SlotCount; slot++)
        {
            if (!world.Water.Rows.IsLive(slot))
            {
                continue;
            }

            Assert.True(world.Bins.Rows.TryResolve(world.Water.Bin[slot], out int bin));
            Assert.Equal(BinOwnerKind.WaterBody, world.Bins.OwnerKind[bin]);
            Assert.Equal(world.Rules.Water.Carries, world.Bins.Resource[bin]);
        }
    }

    /// <summary>
    /// ⚠ <b>The Resource is <c>Utility</c> family, and that is <c>adr/0160</c> rather than a
    /// convention.</b> A Good moving downstream would move with no Vehicle.
    /// </summary>
    [Fact]
    public void What_a_body_holds_is_a_Utility()
    {
        World world = Generated(Key);

        Assert.Equal(ResourceFamily.Utility, world.Rules.Family(world.Rules.Water.Carries));
    }

    /// <summary>
    /// <b>Capacity is the body's size times the authored per-Cell figure</b> — the gradient that makes
    /// pollution a debt in a small body and a rent in a large one.
    /// </summary>
    [Fact]
    public void Capacity_is_the_bodys_own_size()
    {
        World world = Generated(Key);

        for (int slot = 0; slot < world.Water.Rows.SlotCount; slot++)
        {
            if (!world.Water.Rows.IsLive(slot))
            {
                continue;
            }

            Assert.True(world.Bins.Rows.TryResolve(world.Water.Bin[slot], out int bin));
            Assert.Equal(
                (long)world.Water.CellCount[slot] * world.Rules.Water.CapacityPerCell,
                world.Bins.Capacity[bin]);
        }
    }

    /// <summary>
    /// The recorded Cell count is the number of rows that name the body. <b>The saved copy is right.</b>
    /// </summary>
    /// <remarks>
    /// ⚠ <b>It is saved rather than derived, which is only safe because <c>adr/0021</c> makes water
    /// immutable</b> — so this test is what says the copy and the rows agree on the day it was
    /// written, and nothing can move them apart afterwards.
    /// </remarks>
    [Fact]
    public void The_recorded_size_is_the_number_of_Cells()
    {
        World world = Generated(Key);
        var counted = new int[world.Water.Rows.SlotCount];

        for (int slot = 0; slot < world.WaterCells.Rows.SlotCount; slot++)
        {
            if (world.WaterCells.Rows.IsLive(slot))
            {
                counted[world.Water.Rows.Resolve(world.WaterCells.Body[slot])]++;
            }
        }

        for (int slot = 0; slot < world.Water.Rows.SlotCount; slot++)
        {
            if (world.Water.Rows.IsLive(slot))
            {
                Assert.Equal(counted[slot], world.Water.CellCount[slot]);
            }
        }
    }

    /// <summary>
    /// ⚠ <b>An endorheic body has no exits, which is what makes <em>a pond fills</em> true by
    /// construction rather than by a rule.</b> A body with a downstream has exactly one — its spill
    /// point — and a body on the map's edge has one per boundary Cell.
    /// </summary>
    [Fact]
    public void Exits_are_zero_only_where_the_water_goes_nowhere()
    {
        World world = Generated(Key);
        int endorheic = 0;

        for (int slot = 0; slot < world.Water.Rows.SlotCount; slot++)
        {
            if (!world.Water.Rows.IsLive(slot))
            {
                continue;
            }

            bool onEdge = false;

            for (int cell = 0; cell < world.WaterCells.Rows.SlotCount; cell++)
            {
                if (!world.WaterCells.Rows.IsLive(cell)
                    || world.Water.Rows.Resolve(world.WaterCells.Body[cell]) != slot)
                {
                    continue;
                }

                int east = world.WaterCells.East[cell].Raw;
                int north = world.WaterCells.North[cell].Raw;

                if (east == 0
                    || north == 0
                    || east == CellGrid.WorldCells - 1
                    || north == CellGrid.WorldCells - 1)
                {
                    onEdge = true;

                    break;
                }
            }

            bool spills = !world.Water.Downstream[slot].IsNone;

            if (world.Water.Exits[slot] == 0)
            {
                endorheic++;
                Assert.False(onEdge, $"body {slot} touches the map edge and has no exit");
                Assert.False(spills, $"body {slot} has a downstream and no exit");
            }
            else
            {
                Assert.True(onEdge || spills, $"body {slot} has exits and nowhere to send them");
            }
        }

        Assert.True(endorheic > 0, "no endorheic body on this world, so that branch never ran");
    }

    /// <summary>
    /// A body with exits sheds <c>exits × rate</c> in a Day, and a body without sheds nothing.
    /// </summary>
    [Fact]
    public void A_Day_moves_a_bodys_outflow_and_a_pond_keeps_everything()
    {
        World world = Generated(Key);

        int pond = -1;
        int draining = -1;

        for (int slot = 0; slot < world.Water.Rows.SlotCount && (pond < 0 || draining < 0); slot++)
        {
            if (!world.Water.Rows.IsLive(slot))
            {
                continue;
            }

            // A body draining OFF THE MAP, so the deposit has nowhere to land and the test reads one
            // level rather than two. A spilling body is the next test's subject.
            if (world.Water.Exits[slot] == 0 && pond < 0) { pond = slot; }
            else if (world.Water.Exits[slot] > 0
                && world.Water.Downstream[slot].IsNone
                && draining < 0)
            {
                draining = slot;
            }
        }

        Assert.True(pond >= 0, "no endorheic body to test");
        Assert.True(draining >= 0, "no body draining off the map to test");

        long inPond = Fill(world, pond, 500_000);
        long inDraining = Fill(world, draining, 500_000);

        Assert.True(inPond > 0 && inDraining > 0, "one of the two bodies had no room at all");

        world.DrainWaterBodies(Ticks.Zero);

        long rate = (long)world.Water.Exits[draining] * world.Rules.Water.OutflowPerExitPerDay;

        Assert.Equal(inPond, world.Bins.LevelAt(world.Bins.Rows.Resolve(world.Water.Bin[pond])));
        Assert.Equal(
            inDraining - Math.Min(inDraining, rate),
            world.Bins.LevelAt(world.Bins.Rows.Resolve(world.Water.Bin[draining])));
    }

    /// <summary>
    /// ⚠ <b>Water that leaves a body arrives in the one below it</b>, which is the asymmetry
    /// <c>CONTEXT.md</c> calls the only one in the design — a city exports its consequence downstream.
    /// </summary>
    [Fact]
    public void What_leaves_one_body_arrives_in_the_next()
    {
        World world = Generated(Key);
        int upstream = -1;

        for (int slot = 0; slot < world.Water.Rows.SlotCount; slot++)
        {
            if (world.Water.Rows.IsLive(slot)
                && world.Water.Exits[slot] > 0
                && !world.Water.Downstream[slot].IsNone)
            {
                upstream = slot;

                break;
            }
        }

        Assert.True(upstream >= 0, "no body spills into another on this world");

        int below = world.Water.Rows.Resolve(world.Water.Downstream[upstream]);
        long held = Fill(world, upstream, 500_000);

        Assert.True(held > 0, "the upstream body had no room at all");

        long before = world.Bins.LevelAt(world.Bins.Rows.Resolve(world.Water.Bin[below]));

        world.DrainWaterBodies(Ticks.Zero);

        long moved = held - world.Bins.LevelAt(world.Bins.Rows.Resolve(world.Water.Bin[upstream]));

        Assert.True(moved > 0, "the upstream body shed nothing");
        Assert.Equal(before + moved, world.Bins.LevelAt(world.Bins.Rows.Resolve(world.Water.Bin[below])));
    }

    /// <summary>
    /// 🔴 <b>Nothing is created and nothing is destroyed on the way down.</b> The conservation check,
    /// and the one that would catch a two-phase pass that double-counted.
    /// </summary>
    [Fact]
    public void A_Day_of_drainage_conserves_what_stays_in_the_world()
    {
        World world = Generated(Key);
        long placed = 0;

        for (int slot = 0; slot < world.Water.Rows.SlotCount; slot++)
        {
            if (world.Water.Rows.IsLive(slot))
            {
                placed += Fill(world, slot, 1_000);
            }
        }

        // What leaves the world is what the off-map bodies shed, and it is subtracted rather than
        // ignored -- CONTEXT.md -> Water Body's Hinterland terminus is a real sink and the only one.
        long offMap = 0;

        for (int slot = 0; slot < world.Water.Rows.SlotCount; slot++)
        {
            if (world.Water.Rows.IsLive(slot)
                && world.Water.Exits[slot] > 0
                && world.Water.Downstream[slot].IsNone)
            {
                long rate = (long)world.Water.Exits[slot] * world.Rules.Water.OutflowPerExitPerDay;
                long level = world.Bins.LevelAt(world.Bins.Rows.Resolve(world.Water.Bin[slot]));
                offMap += Math.Min(level, rate);
            }
        }

        world.DrainWaterBodies(Ticks.Zero);

        long remaining = 0;

        for (int slot = 0; slot < world.Water.Rows.SlotCount; slot++)
        {
            if (world.Water.Rows.IsLive(slot))
            {
                remaining += world.Bins.LevelAt(world.Bins.Rows.Resolve(world.Water.Bin[slot]));
            }
        }

        Assert.Equal(placed - offMap, remaining);
    }

    /// <summary>A world whose Ruleset states no water Bin has bodies with no Bin at all.</summary>
    /// <remarks>
    /// <c>adr/0123</c>: no Bin rather than a Bin whose level is permanently zero.
    /// </remarks>
    [Fact]
    public void A_Ruleset_with_no_water_Bin_gives_the_bodies_none()
    {
        World world = Generated(Key, "minimal.toml");

        Assert.False(world.Rules.Water.HasBin);

        for (int slot = 0; slot < world.Water.Rows.SlotCount; slot++)
        {
            if (world.Water.Rows.IsLive(slot))
            {
                Assert.True(world.Water.Bin[slot].IsNone);
            }
        }
    }

    /// <summary>The Bins and their capacities survive a save, and capacity is derived on the way in.</summary>
    [Fact]
    public void The_Bins_survive_a_save_and_their_capacities_are_rebuilt()
    {
        World world = Generated(Key);
        Ruleset rules = Load("coastal.toml");

        var file = new MemorySave();
        SaveFile.Write(world, 0x0BAD_F00D_0BAD_F00DUL, file);

        World loaded = SaveFile.Read(file, rules, out _);

        for (int slot = 0; slot < world.Water.Rows.SlotCount; slot++)
        {
            if (!world.Water.Rows.IsLive(slot))
            {
                continue;
            }

            Assert.True(loaded.Bins.Rows.TryResolve(loaded.Water.Bin[slot], out int bin));
            Assert.Equal(
                world.Bins.Capacity[world.Bins.Rows.Resolve(world.Water.Bin[slot])],
                loaded.Bins.Capacity[bin]);
        }
    }
}
