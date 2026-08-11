using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Tests.Golden;

namespace Borough.Tests.Space;

/// <summary>
/// <c>adr/0006</c>'s long run, aimed at the Road Graph: <b>100,000 Ticks with no collection and no
/// magnitude trending upward</b>, and an assertion that both branches under test were reached.
/// </summary>
/// <remarks>
/// <para>
/// <b>The honest shape of this test is unusual and worth stating before the assertions.</b> Nothing
/// in milestone 5a <em>edits</em> the Road Graph — the generator lays it at world creation, the
/// <c>build_road</c> command is 5a-bis, and no Tick phase writes a Segment. So <c>adr/0006</c>'s
/// question, <i>does anything grow with elapsed time</i>, has an answer known in advance: no, because
/// nothing changes at all. A test that only asserted that would be true, vacuous, and would go on
/// passing after somebody added the mechanism that could break it.
/// </para>
/// <para>
/// <b>So what is asserted is the stronger claim: the graph is bit-identical after 100,000 Ticks of a
/// city that is demolishing and rebuilding itself throughout.</b> That is a real property with a real
/// way to fail — a Tick phase that reached into the graph, a rebuild triggered from somewhere it
/// should not be, an Epoch bumped by a write nobody meant to make. Stated as a fold over the two
/// tables rather than as a count, so a Segment whose <em>contents</em> moved is caught as well as one
/// that appeared.
/// </para>
/// <para>
/// <b>5a-bis built the mechanism this paragraph was waiting for, and the run below is deliberately
/// unchanged.</b> <c>CommandKind.Connect</c> now edits the graph, so <i>nothing writes a Segment</i>
/// has stopped being a property of the code and become a property of <em>this session</em>, which
/// issues no commands. That is still the claim worth making here — a Tick phase reaching into the
/// graph unbidden is a different defect from an editor that leaks — and the editing run lives beside
/// it in <c>LotLongRunTests</c>. Splitting them keeps each failure attributable to one cause.
/// </para>
/// <para>
/// <b>And it carries a vacuity guard on the other side.</b> <i>The graph did not move</i> is a claim
/// about a static structure inside a churning city; if the city were not churning it would be a claim
/// about nothing. The run therefore asserts that Buildings really were demolished and Households
/// really were placed over the same 100,000 Ticks, which is what makes the graph's stillness evidence
/// rather than a tautology.
/// </para>
/// </remarks>
public sealed class RoadLongRunTests
{
    private const int TickCount = 100_000;
    private const int Population = 1_000;
    private const int ReadEvery = 2_048;

    /// <summary>
    /// The graph is bit-identical after 100,000 Ticks, in a city that never stopped changing.
    /// </summary>
    [Fact]
    public void The_hundred_thousand_Tick_road_run()
    {
        Reading[] readings = Run(out World world, out Reading opening);

        Assert.NotEmpty(readings);

        // Vacuity first, because everything below is a claim about a still object inside a moving one.
        Assert.True(
            readings[^1].Buildings != opening.Buildings
            || readings[^1].Households != opening.Households,
            "neither the Building count nor the Household count moved over 100,000 Ticks, so the "
            + "city was static and 'the Road Graph did not move' is a claim about nothing.");

        foreach (Reading reading in readings)
        {
            Assert.Equal(opening.Nodes, reading.Nodes);
            Assert.Equal(opening.Segments, reading.Segments);
            Assert.Equal(opening.Arcs, reading.Arcs);

            Assert.True(
                reading.Fold == opening.Fold,
                $"the Road Graph's fold moved from {opening.Fold:X16} to {reading.Fold:X16} by Tick "
                + $"{reading.Tick}. Nothing in milestone 5a may edit the graph, so some Tick phase is "
                + "writing to it -- the counts alone would not have shown this.");
        }

        // adr/0006's collection half, stated over the one structure that is rebuilt rather than
        // written: a rebuild that grew the Arc array each time would be invisible in the fold, which
        // covers only the saved tables.
        Assert.Equal(opening.Arcs, world.Roads.Arcs.Count);
        Assert.Equal(opening.Segments * 2, world.Roads.Arcs.Count);
    }

    /// <summary>
    /// Rebuilding the derived structures after the run reproduces them exactly.
    /// </summary>
    /// <remarks>
    /// <b><c>adr/0003</c>'s <c>(derived AND rebuilt)</c> declaration, made into an assertion.</b> A
    /// derived column is allowed to be absent from the save on the promise that it is a pure function
    /// of the saved state — so rebuilding it at any moment must be a no-op. This is where that promise
    /// is checked, and 100,000 Ticks in is the interesting moment to check it, because a rebuild that
    /// depends on the order things happened rather than on what they are will have accumulated its
    /// divergence by then.
    /// </remarks>
    [Fact]
    public void The_derived_structures_rebuild_to_themselves_after_a_long_run()
    {
        Run(out World world, out _);

        RoadGraph graph = world.Roads;

        int arcs = graph.Arcs.Count;
        int carComponents = graph.Connectivity.CarComponents;
        int footComponents = graph.Connectivity.FootComponents;
        int largestCar = graph.Connectivity.LargestCar;

        ulong before = FoldOf(graph);
        int[] labels = Labels(graph);

        graph.RebuildDerived();

        Assert.Equal(arcs, graph.Arcs.Count);
        Assert.Equal(carComponents, graph.Connectivity.CarComponents);
        Assert.Equal(footComponents, graph.Connectivity.FootComponents);
        Assert.Equal(largestCar, graph.Connectivity.LargestCar);
        Assert.Equal(labels, Labels(graph));
        Assert.Equal(before, FoldOf(graph));
    }

    /// <summary>
    /// Both branches of the generator were reached in the graph this run is about.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Slice 10 task 11's lesson, and the reason <c>plans/0020</c> task 7 asks for it by name.</b>
    /// The Zone Rule's derived sample fell to 1, the committed trace silently stopped covering the
    /// create branch, every hash moved and every test passed — because <b>a baseline records what a
    /// run <em>did</em>, so a change that narrows what the run <em>reaches</em> is invisible in it by
    /// construction.</b>
    /// </para>
    /// <para>
    /// The equivalent narrowing here is an Arterial that stops crossing any Street. Nothing would go
    /// red: the node count is unchanged, the Segment count goes <em>up</em> because nothing is being
    /// destroyed, the fold moves as it always does, and Severance quietly stops existing. So the run
    /// asserts that the graph it ran over contains both outcomes — a Street destroyed by an Arterial,
    /// and a Street kept as a crossing.
    /// </para>
    /// </remarks>
    [Fact]
    public void Both_generator_branches_were_reached_by_the_graph_this_run_uses()
    {
        Run(out World world, out _);

        RoadGraph graph = world.Roads;

        int crossings = 0;
        int streets = 0;
        int arterials = 0;

        for (int slot = 0; slot < graph.Segments.Rows.SlotCount; slot++)
        {
            if (!graph.Segments.Rows.IsLive(slot))
            {
                continue;
            }

            switch ((RoadKind)graph.Segments.Kind[slot])
            {
                case RoadKind.Street:
                    streets++;
                    break;

                case RoadKind.Arterial:
                    arterials++;
                    break;

                case RoadKind.FootPath:
                    crossings++;
                    break;

                default:
                    break;
            }
        }

        Assert.True(arterials > 0, "no Arterial was laid, so nothing could sever anything.");
        Assert.True(streets > 0, "no Street was laid, so the grid branch never ran.");
        Assert.True(
            crossings > 0,
            "no Street was kept as a crossing over the whole run. Either the Arterials crossed "
            + "nothing -- in which case Severance is uncovered and no count would have shown it -- "
            + "or the crossing branch is dead.");

        // The destroyed branch, stated against the count the same Ruleset produces with the Arterials
        // taken out. Without the control, 'streets > 0' passes on a generator that severs nothing.
        Assert.True(
            streets < IntactStreets(),
            $"{streets} Streets survived against {IntactStreets()} with no Arterial on the map, so "
            + "no Street was destroyed and the severance branch never ran.");
    }

    /// <summary>The Street count the same Ruleset lays with no Arterial to destroy any of them.</summary>
    private static int IntactStreets()
    {
        // Only the [roads] table matters to the generator, so this drops the rest of the golden
        // Ruleset rather than trying to clone a class that is deliberately not a record.
        var roads = GoldenFixtures.Rules().Roads with { ArterialCount = 0 };
        World world = new(Population, RoadFixtures.With(roads));

        SyntheticCity.PopulateInto(world, WorldKey.FromSeed(GoldenFixtures.Seed), Ticks.Zero);

        return world.Roads.Segments.Rows.LiveCount;
    }

    private static ulong FoldOf(RoadGraph graph)
    {
        ulong hash = 0;

        graph.Nodes.Rows.Fold(ref hash);
        graph.Segments.Rows.Fold(ref hash);

        return hash;
    }

    private static int[] Labels(RoadGraph graph)
    {
        var labels = new int[graph.Nodes.Rows.SlotCount];

        for (int slot = 0; slot < labels.Length; slot++)
        {
            labels[slot] = graph.Nodes.CarComponent[slot];
        }

        return labels;
    }

    /// <summary>One interval's worth of the graph, plus the two counts that prove the city moved.</summary>
    private readonly record struct Reading(
        int Tick, int Nodes, int Segments, int Arcs, ulong Fold, int Buildings, int Households);

    private static Reading Read(World world, int tick) =>
        new(
            tick,
            world.Roads.Nodes.Rows.LiveCount,
            world.Roads.Segments.Rows.LiveCount,
            world.Roads.Arcs.Count,
            FoldOf(world.Roads),
            world.Buildings.Rows.LiveCount,
            world.Households.Rows.LiveCount);

    private static Reading[] Run(out World world, out Reading opening)
    {
        var key = WorldKey.FromSeed(GoldenFixtures.Seed);

        world = new World(Population, GoldenFixtures.Rules());

        var simulation = new Simulation(world, key)
        {
            // O(world) twice per Tick against a phase meant to be O(woken). --no-decide-guard's
            // reason, and the guard's own correctness is covered by the tests written for it.
            VerifyDecideWritesNothing = false,
        };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        opening = Read(world, 0);

        List<Reading> readings = [];

        for (int tick = 0; tick < TickCount; tick++)
        {
            simulation.Step(default);

            if ((tick + 1) % ReadEvery == 0)
            {
                readings.Add(Read(world, tick + 1));
            }
        }

        return [.. readings];
    }
}
