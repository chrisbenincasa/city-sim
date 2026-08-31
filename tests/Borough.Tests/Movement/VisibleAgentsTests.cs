using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Movement;

/// <summary>
/// The city can be asked where its Travellers are. <c>plans/0045</c>'s queue item 11a.
/// </summary>
/// <remarks>
/// <b>Coverage is the assertion that matters.</b> A query that dropped the walkers would look
/// correct on <c>congested.toml</c> and show an empty city on every world where nobody drives,
/// which is most of them.
/// </remarks>
public sealed class VisibleAgentsTests
{
    /// <summary><b>Every in-flight Traveller is somewhere.</b></summary>
    [Fact]
    public void Every_traveller_in_flight_is_placed()
    {
        (Simulation simulation, World world) = Rush();

        var into = new VisibleAgent[4_096];
        int sampled = 0;

        for (int tick = 0; tick < Ticks.PerDay; tick++)
        {
            simulation.Step(default);

            int live = world.Travellers.Rows.LiveCount;

            if (live == 0)
            {
                continue;
            }

            sampled++;

            Assert.Equal(live, VisibleAgents.In(world, CellRect.World, Ratio.Zero, into));
        }

        Assert.True(sampled > 0, "no Traveller was in flight all Day, so nothing was asked");
    }

    /// <summary>A Traveller is placed on the map rather than off it.</summary>
    [Fact]
    public void A_placed_traveller_is_inside_the_world()
    {
        (Simulation simulation, World world) = Rush();

        var into = new VisibleAgent[4_096];
        Tiles edge = CellGrid.ToTiles(new Cells(CellGrid.WorldCells));

        for (int tick = 0; tick < Ticks.PerDay; tick++)
        {
            simulation.Step(default);

            int found = VisibleAgents.In(world, CellRect.World, Ratio.Zero, into);

            for (int agent = 0; agent < found; agent++)
            {
                Assert.InRange(into[agent].East.ToTilesFloor().Raw, 0, edge.Raw);
                Assert.InRange(into[agent].North.ToTilesFloor().Raw, 0, edge.Raw);
            }
        }
    }

    /// <summary><b>A box excludes</b>, which is what makes the query a query.</summary>
    [Fact]
    public void A_box_with_no_city_in_it_is_empty()
    {
        (Simulation simulation, World world) = Rush();

        var into = new VisibleAgent[4_096];
        CellRect far = new(
            new Cells(CellGrid.WorldCells - 4), new Cells(CellGrid.WorldCells - 4),
            new Cells(2), new Cells(2));

        for (int tick = 0; tick < 512; tick++)
        {
            simulation.Step(default);

            Assert.Equal(0, VisibleAgents.In(world, far, Ratio.Zero, into));
        }
    }

    /// <summary><b>A short buffer truncates</b>, for <c>MapLayers.LayerCells</c>'s reason.</summary>
    [Fact]
    public void A_short_buffer_is_filled_and_no_more()
    {
        (Simulation simulation, World world) = Rush();

        var one = new VisibleAgent[1];

        for (int tick = 0; tick < Ticks.PerDay; tick++)
        {
            simulation.Step(default);

            if (world.Travellers.Rows.LiveCount > 0)
            {
                Assert.Equal(1, VisibleAgents.In(world, CellRect.World, Ratio.Zero, one));

                return;
            }
        }

        Assert.Fail("no Traveller was in flight all Day, so nothing was truncated");
    }

    /// <summary><b>Asking does not change the city.</b></summary>
    [Fact]
    public void Looking_at_the_city_does_not_move_it()
    {
        (Simulation simulation, World world) = Rush();

        var into = new VisibleAgent[4_096];

        for (int tick = 0; tick < 512; tick++)
        {
            simulation.Step(default);

            ulong before = world.HashState();
            VisibleAgents.In(world, CellRect.World, Ratio.Zero, into);

            Assert.Equal(before, world.HashState());
        }
    }

    /// <summary>
    /// 🔴 <b>A Traveller stands on the Road Graph and not in the middle of a block.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The walkers used to be drawn on a straight line between their Leg's two Addresses</b>, and
    /// the remark saying so was read by everybody and minded by nobody until the shell drew them
    /// floating over the blocks. A walk Leg now records its route like a drive, so a walker has a
    /// place rather than a pair of endpoints.
    /// </para>
    /// <para>
    /// ⚠ <b>The tolerance is a Segment's own half-width and not zero.</b> A position is placed on the
    /// line between two Nodes, and a Node's coordinates are whole Tiles, so the arithmetic lands
    /// within a Tile of the centreline rather than on it.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_traveller_stands_on_a_segment()
    {
        (Simulation simulation, World world) = Rush();

        var into = new VisibleAgent[4_096];
        int checked_ = 0;

        for (int tick = 0; tick < Ticks.PerDay; tick++)
        {
            simulation.Step(default);

            int found = VisibleAgents.In(world, CellRect.World, Ratio.Zero, into);

            for (int agent = 0; agent < found; agent++)
            {
                Assert.True(
                    OnAStreet(world, into[agent]),
                    $"a Traveller at ({into[agent].East.ToTilesFloor().Raw}, "
                    + $"{into[agent].North.ToTilesFloor().Raw}) is on no Segment — it was placed in "
                    + "the middle of a block, which is the straight-line placement returning.");

                checked_++;
            }
        }

        Assert.True(checked_ > 0, "no Traveller was in flight all Day, so nothing was checked");
    }

    /// <summary>Whether a placed agent lies within a Tile of some Segment's centreline.</summary>
    private static bool OnAStreet(World world, VisibleAgent agent)
    {
        RoadSegmentTable segments = world.Roads.Segments;
        RoadNodeTable nodes = world.Roads.Nodes;
        int east = agent.East.ToTilesFloor().Raw;
        int north = agent.North.ToTilesFloor().Raw;

        for (int slot = 0; slot < segments.Rows.SlotCount; slot++)
        {
            if (!segments.Rows.IsLive(slot)
                || !nodes.Rows.TryResolve(segments.NodeA[slot], out int a)
                || !nodes.Rows.TryResolve(segments.NodeB[slot], out int b))
            {
                continue;
            }

            int aEast = nodes.East[a].Raw;
            int aNorth = nodes.North[a].Raw;
            int bEast = nodes.East[b].Raw;
            int bNorth = nodes.North[b].Raw;

            if (east < Math.Min(aEast, bEast) - 1 || east > Math.Max(aEast, bEast) + 1
                || north < Math.Min(aNorth, bNorth) - 1 || north > Math.Max(aNorth, bNorth) + 1)
            {
                continue;
            }

            // Twice the triangle's area over the base is its height: the point's distance from the
            // line through the two Nodes, without a square root and without a divide by zero.
            long area = Math.Abs(
                ((long)(bEast - aEast) * (aNorth - north))
                - ((long)(aEast - east) * (bNorth - aNorth)));
            long baseSquared = ((long)(bEast - aEast) * (bEast - aEast))
                + ((long)(bNorth - aNorth) * (bNorth - aNorth));

            if (baseSquared > 0 && area * area <= 4 * baseSquared)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>A walking city, stepped to where its Citizens have jobs to walk to.</summary>
    private static (Simulation Simulation, World World) Rush()
    {
        string toml = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml"));

        RulesetLoadResult result = RulesetLoader.Parse(toml, "minimal.toml");

        Assert.True(result.Ok, result.Describe());

        var key = WorldKey.FromSeed(0);
        World world = new(1_000, result.Ruleset!, key);
        Simulation simulation = new(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        for (int tick = 0; tick < 4_096; tick++)
        {
            simulation.Step(default);
        }

        return (simulation, world);
    }
}
