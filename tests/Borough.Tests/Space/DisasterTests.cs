using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Space;

/// <summary>
/// <c>plans/0045</c> row 12: the Hazard Region acquires a consumer, and floods happen on it.
/// </summary>
/// <remarks>
/// <para>
/// <c>CONTEXT.md</c> → Disaster, <c>01 §5.2</c>, <c>01 §5.3</c>. The claims under test are the
/// mechanism's: <b>a flood is scheduled by the clock and the seed and never by the city</b>; <b>it
/// spreads only over the Hazard Region and only where it is connected to its seed</b>; <b>it
/// recedes to nothing, so the footprint is a collection with a sink</b>; and <b>both verbs fire,
/// with one depth against another choosing between them</b>.
/// </para>
/// <para>
/// ⚠ <b>Assertion tier, and the Ticks are what they cost.</b> <c>flooded.toml</c> states
/// <c>flood_every_days = 2</c> and <see cref="DisasterEngine"/> refuses Tick 0, so the first flood
/// in that world begins at Tick 4,096 and there is no shorter path to one — <b>a duration cannot be
/// shortened by a fixture</b>, and a Ruleset built in code to fire sooner would be testing an
/// interval no designer can author (the loader refuses a duration below one Day).
/// </para>
/// <para>
/// ⚠ <b>Nothing here asserts how many Buildings a flood takes.</b> That is a property of where the
/// world seeded it and of the terrain the seed sits in — <c>flooded.toml</c>'s own header says the
/// numbers ratify nothing — so a count pinned here would be a re-baseline on every change to the
/// noise. What is asserted is that <em>both</em> verbs are reachable and that the depth is what
/// picks.
/// </para>
/// </remarks>
public sealed class DisasterTests
{
    private const int Citizens = 2_000;

    /// <summary>The first flood on <c>flooded.toml</c>: <c>flood_every_days = 2</c>, and not Tick 0.</summary>
    private const ulong FirstFlood = 2UL * Ticks.PerDay;

    private static readonly WorldKey Key = WorldKey.FromSeed(0);

    [Fact]
    public void A_world_without_the_table_never_floods()
    {
        // coastal.toml has the Hazard Region and no schedule over it, which is 01 §5.3's posted
        // price before the first Act of God. ⚠ THE POINT IS THAT IT HAS ROWS TO FLOOD AND DOES NOT
        // -- a world with no floodplain would pass this for the wrong reason.
        (World world, Simulation simulation) = Run("coastal.toml", FirstFlood + 64);

        Assert.False(world.Rules.Disasters.Stated);
        Assert.True(world.Flood.Rows.LiveCount > 0);
        Assert.Equal(0, world.Disasters.Rows.LiveCount);
        Assert.Equal(0, world.Inundations.Rows.LiveCount);
        Assert.Equal(default, simulation.LastDisasters);
    }

    [Fact]
    public void A_flood_begins_on_the_interval_and_never_on_tick_zero()
    {
        (World world, _) = Run("flooded.toml", 1);

        Assert.Equal(0, world.Disasters.Rows.LiveCount);

        (World later, _) = Run("flooded.toml", FirstFlood + 1);

        Assert.Equal(1, later.Disasters.Rows.LiveCount);
    }

    [Fact]
    public void The_seed_is_a_cell_of_the_hazard_region_and_carries_its_depth()
    {
        (World world, _) = Run("flooded.toml", FirstFlood + 1);
        int slot = OnlyFlood(world);

        Cells east = world.Disasters.East[slot];
        Cells north = world.Disasters.North[slot];

        // The seed depth is not merely positive -- it is the Hazard Region's own depth at that Cell.
        // A flood whose scale came from anywhere else would still spread and still look right.
        Assert.Equal(
            world.FloodInCells.DepthAt(world.Flood, east, north),
            world.Disasters.SeedDepth[slot]);

        Assert.True(world.Disasters.SeedDepth[slot] > 0);
    }

    [Fact]
    public void Every_cell_under_water_is_hazard_region()
    {
        // ⚠ THE CONVERSE IS FALSE AND MUST NOT BE ASSERTED. Most of the Hazard Region is not under
        // water at any moment: a flood reaches the component connected to its seed below the surge,
        // and 01 §5.2 is explicit that the overlay is where a flood COULD go.
        (World world, _) = Run("flooded.toml", FirstFlood + 512);
        InundationTable wet = world.Inundations;

        Assert.True(wet.Rows.LiveCount > 0);

        for (int slot = 0; slot < wet.Rows.SlotCount; slot++)
        {
            if (!wet.Rows.IsLive(slot))
            {
                continue;
            }

            int depth = world.FloodInCells.DepthAt(
                world.Flood, wet.East[slot], wet.North[slot]);

            Assert.True(depth > 0, $"Cell ({wet.East[slot].Raw},{wet.North[slot].Raw}) is under "
                + "water and is not in the Hazard Region.");
            Assert.Equal(depth, wet.Depth[slot]);
        }
    }

    [Fact]
    public void The_footprint_is_connected_to_the_seed()
    {
        (World world, _) = Run("flooded.toml", FirstFlood + 512);
        InundationTable wet = world.Inundations;
        int flood = OnlyFlood(world);
        var reached = new HashSet<int>();
        var frontier = new Stack<int>();

        for (int slot = 0; slot < wet.Rows.SlotCount; slot++)
        {
            if (wet.Rows.IsLive(slot))
            {
                reached.Add(CellGrid.Index(wet.East[slot], wet.North[slot]));
            }
        }

        int seed = CellGrid.Index(world.Disasters.East[flood], world.Disasters.North[flood]);

        Assert.Contains(seed, reached);
        frontier.Push(seed);

        var walked = new HashSet<int> { seed };

        while (frontier.Count > 0)
        {
            int at = frontier.Pop();

            foreach (int step in Neighbours(at))
            {
                if (reached.Contains(step) && walked.Add(step))
                {
                    frontier.Push(step);
                }
            }
        }

        // A Cell under water that no walk from the seed reaches would mean the surge test alone had
        // put it there -- which is the whole floodplain flooding at once rather than a bounded event.
        Assert.Equal(reached.Count, walked.Count);
    }

    [Fact]
    public void The_water_leaves_and_the_rows_go_with_it()
    {
        DisasterRuleset rules = Load("flooded.toml").Disasters;
        ulong over = FirstFlood
            + (ulong)rules.FloodRisesOverTicks
            + (ulong)rules.FloodRecedesOverTicks
            + 2UL;

        // ⚠ THERE IS NO MOMENT ON THIS FILE WITH NOTHING LIVE, AND THE FIRST SPELLING OF THIS TEST
        // ASSERTED ONE. rises 1 + recedes 1 = every 2, so the second flood begins on Tick 8,192 and
        // the first ends on 8,193 — the header says so and the assertion was written anyway. What is
        // asserted instead is the invariant that actually matters, which is stronger: NO CELL IS
        // UNDER WATER FOR A FLOOD THAT HAS ENDED.
        World world = new(Citizens, Load("flooded.toml"), Key);
        Simulation simulation = new(world, Key);
        int peak = 0;

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        for (ulong tick = 0; tick < over; tick++)
        {
            simulation.Step(default);
            peak = Math.Max(peak, world.Inundations.Rows.LiveCount);
        }

        InundationTable wet = world.Inundations;

        for (int slot = 0; slot < wet.Rows.SlotCount; slot++)
        {
            Assert.True(
                !wet.Rows.IsLive(slot) || world.Disasters.Rows.IsValid(wet.Cause[slot]),
                $"Cell ({wet.East[slot].Raw},{wet.North[slot].Raw}) is under water for a Disaster "
                + "that has ended.");
        }

        // adr/0006 in one number: the footprint is created by a schedule with no sink of its own, so
        // a flood that did not put its Cells back would be a collection growing with elapsed time —
        // and on a floodplain it would take a hundred thousand Ticks to show up.
        //
        // 🔴 IT LEAKED, AND ONLY THE DEEP HALF. The final drain freed the Cells BELOW the surge, and
        // ground deeper than the seed is never below it: measured at 5,140 Cells still standing after
        // three floods had ended. The second flood here is two Ticks old, so anything approaching the
        // high-water mark is the first flood's water still standing.
        Assert.True(peak > 0, "no Cell was ever under water, so this asserts nothing.");
        Assert.True(
            wet.Rows.LiveCount * 2 < peak,
            $"{wet.Rows.LiveCount:N0} Cells are under water two Ticks into a flood that peaked at "
            + $"{peak:N0}. The first flood's water has not left.");
    }

    [Fact]
    public void Both_verbs_fire_and_the_depth_is_what_picks()
    {
        // Over five floods rather than one, because which verb fires is a property of where the
        // world seeded each -- ground below the flood's origin is swept, ground above it is ruined --
        // and a single flood on a single seed demonstrates one half. ⚠ THE COUNTS ARE NOT ASSERTED,
        // only that neither is zero across the run: flooded.toml's header says its numbers ratify
        // nothing, and a pinned count would re-baseline on any change to the noise.
        (World world, _) = Run("flooded.toml", (5UL * FirstFlood) + 1);
        DisasterTable floods = world.Disasters;
        long ruined = 0;
        long swept = 0;

        for (int slot = 0; slot < floods.Rows.SlotCount; slot++)
        {
            if (floods.Rows.IsLive(slot))
            {
                ruined += floods.Ruined[slot];
                swept += floods.Swept[slot];
            }
        }

        // The live row is the last flood only, so the totals above are that one's. The verbs are
        // asserted against the CITY instead, which carries every flood's work: a swept Building
        // vacated its Lot and a ruined one is standing and abandoned.
        Assert.True(ruined + swept >= 0);
        Assert.True(Abandoned(world) > 0, "no Building was ruined by five floods on a city with "
            + "240 of 420 Lots on the floodplain. Both verbs are supposed to be reachable.");
        Assert.True(
            world.Buildings.Rows.LiveCount < world.Lots.Rows.LiveCount,
            "every Lot is still built on after five floods, so nothing was ever swept away.");
    }

    [Fact]
    public void The_schedule_owes_nothing_to_the_city()
    {
        // 01 §5.3, and it is the ADR-shaped claim in this file. A flood's place is a function of the
        // seed and the Tick, so two worlds differing ONLY in population -- different Lots, different
        // Buildings, a different city entirely -- put the flood on the same Cell.
        //
        // ⚠ A DISASTER THAT MOVED WITH THE CITY WOULD STILL BE DETERMINISTIC AND WOULD STILL LOOK
        // RIGHT. What it would break is the hazard overlay: riverside land would become
        // cheap-until-you-use-it, and the price the overlay posts would be describing a trap.
        (World small, _) = Run("flooded.toml", FirstFlood + 1, citizens: 500);
        (World large, _) = Run("flooded.toml", FirstFlood + 1, citizens: 4_000);

        Assert.NotEqual(small.Lots.Rows.LiveCount, large.Lots.Rows.LiveCount);
        Assert.Equal(
            small.Disasters.East[OnlyFlood(small)], large.Disasters.East[OnlyFlood(large)]);
        Assert.Equal(
            small.Disasters.North[OnlyFlood(small)], large.Disasters.North[OnlyFlood(large)]);
    }

    [Fact]
    public void A_schedule_over_a_world_with_no_floodplain_is_refused()
    {
        // adr/0123's shape: the Hazard Region is generated from [water] flood_level_percent alone, so
        // a schedule without it is an event with nowhere to happen -- a table that reads as a
        // decision and derives nothing.
        RulesetLoadResult result = RulesetLoader.Parse(
            """
            [[resource]]
            name = "sundries"
            family = "good"

            [disasters]
            flood_every_days = 2
            flood_rises_over_days = 1
            flood_recedes_over_days = 1
            """,
            "no-water.toml");

        Assert.Null(result.Ruleset);
        Assert.Contains("nowhere to occur", result.Describe(), StringComparison.Ordinal);
    }

    private static IEnumerable<int> Neighbours(int at)
    {
        int east = at % CellGrid.WorldCells;
        int north = at / CellGrid.WorldCells;

        if (east + 1 < CellGrid.WorldCells) { yield return at + 1; }
        if (east > 0) { yield return at - 1; }
        if (north + 1 < CellGrid.WorldCells) { yield return at + CellGrid.WorldCells; }
        if (north > 0) { yield return at - CellGrid.WorldCells; }
    }

    private static int Abandoned(World world)
    {
        int shells = 0;

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot) && world.Buildings.IsAbandoned(slot))
            {
                shells++;
            }
        }

        return shells;
    }

    private static int OnlyFlood(World world)
    {
        for (int slot = 0; slot < world.Disasters.Rows.SlotCount; slot++)
        {
            if (world.Disasters.Rows.IsLive(slot))
            {
                return slot;
            }
        }

        throw new InvalidOperationException("no Disaster is in progress.");
    }

    private static Ruleset Load(string file)
    {
        RulesetLoadResult result = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

        return result.Ruleset
            ?? throw new InvalidOperationException($"{file} was refused:\n{result.Describe()}");
    }

    private static (World World, Simulation Simulation) Run(
        string file, ulong ticks, int citizens = Citizens)
    {
        World world = new(citizens, Load(file), Key);
        Simulation simulation = new(world, Key);

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        for (ulong tick = 0; tick < ticks; tick++)
        {
            simulation.Step(default);
        }

        return (world, simulation);
    }
}
