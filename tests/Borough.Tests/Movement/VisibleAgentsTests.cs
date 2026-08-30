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
