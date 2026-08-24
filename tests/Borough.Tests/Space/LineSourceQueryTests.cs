using Borough.Core.Arithmetic;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Tests.Space;

/// <summary>
/// Milestone 9 task 1 — noise and near-road pollution as point-of-use line-source queries.
/// </summary>
/// <remarks>
/// <b>Every test here pins a sentence that was written before the implementation existed.</b>
/// <c>LineSourceQueries</c> shipped in slice 3c as a documentation-only stub carrying two constraints —
/// <em>the query sums</em> and <em>it enumerates by loudness</em> — precisely so that whoever built it
/// inherited them. These assert the built thing has them, which is the only way that intention becomes
/// a property of the build rather than of a comment.
/// </remarks>
public sealed class LineSourceQueryTests
{
    private static readonly LineSource Noise = new(new Tiles(75), Fixed.One);

    /// <summary>
    /// A chain whose Streets sit ON the declared lattice, so <see cref="StreetGrid"/> holds them.
    /// </summary>
    /// <remarks>
    /// ⚠ <b><see cref="RoadFixtures.Chain"/> does not do this, and it reads as though it does.</b> Its
    /// nodes are 32 Tiles apart and its Ruleset declares <c>block_tiles = 512</c>, so every Segment it
    /// makes is <em>off</em> the lattice and lands in <see cref="StreetGrid.OffLatticeCount"/>. Every
    /// other test in this file therefore exercises the linear scan; without this fixture the lattice
    /// window — the half the query exists to be fast in — would have no coverage at all while the file
    /// looked thorough. ***A fixture named for a shape is not a fixture of that shape.***
    /// </remarks>
    private static RoadGraph OnTheLattice(int nodes)
    {
        RoadGraph graph = new(RoadFixtures.Roads(blockTiles: 32, arterials: 0));
        Handle<RoadNode> previous = graph.Nodes.Create(Tiles.Zero, Tiles.Zero);

        for (int i = 1; i < nodes; i++)
        {
            Handle<RoadNode> next = graph.Nodes.Create(new Tiles(i * 32), Tiles.Zero);

            graph.Segments.Create(previous, next, new Tiles(32), RoadKind.Street, TravelMode.Any, TravelMode.Any);
            previous = next;
        }

        graph.RebuildDerived();

        return graph;
    }

    /// <summary>
    /// <b>The lattice window finds what the linear scan would.</b> The two halves of the source set
    /// agree, which is the property that makes the split an optimisation rather than a second model.
    /// </summary>
    [Fact]
    public void A_street_on_the_lattice_is_found_through_the_window_and_not_the_scan()
    {
        RoadGraph graph = OnTheLattice(4);

        Assert.Equal(0, graph.Streets.OffLatticeCount);

        graph.Segments.VolumeForward[0] = 40;

        int through = LineSourceQueries.Noise(graph, Noise, new Tiles(16), new Tiles(6));

        Assert.True(through > 0, "found through the StreetGrid window, with nothing in the scan to find");

        // The same geometry off the lattice, reached by the other half of the source set.
        RoadGraph scanned = RoadFixtures.Chain(4);

        Assert.True(scanned.Streets.OffLatticeCount > 0);

        scanned.Segments.VolumeForward[0] = 40;

        Assert.Equal(through, LineSourceQueries.Noise(scanned, Noise, new Tiles(16), new Tiles(6)));
    }

    /// <summary>
    /// <b>Silence is zero, and the zero is true rather than a placeholder.</b>
    /// </summary>
    /// <remarks>
    /// A Cell nobody drives past is silent, and <c>adr/0123</c> turns on this being an honest reading:
    /// six of the eight shipped Rulesets grant nobody a car, so noise is identically zero in them and
    /// that is a correct statement about a city before the motor car, not a hole returning zero.
    /// ⚠ <b><c>Log1P</c> rather than <c>Log</c> is what makes it representable at all</b> — a plain
    /// logarithm of no intensity is not a small number, it is undefined.
    /// </remarks>
    [Fact]
    public void A_road_nobody_drives_on_is_silent()
    {
        RoadGraph graph = RoadFixtures.Chain(4);

        Assert.Equal(0, LineSourceQueries.Noise(graph, Noise, new Tiles(48), new Tiles(8)));
    }

    /// <summary>Nearer is louder, and it falls away with distance rather than at a cliff.</summary>
    [Fact]
    public void The_same_traffic_is_quieter_further_away()
    {
        RoadGraph graph = RoadFixtures.Chain(4);

        graph.Segments.VolumeForward[0] = 40;

        int near = LineSourceQueries.Noise(graph, Noise, new Tiles(16), new Tiles(2));
        int middle = LineSourceQueries.Noise(graph, Noise, new Tiles(16), new Tiles(20));
        int far = LineSourceQueries.Noise(graph, Noise, new Tiles(16), new Tiles(60));

        Assert.True(near > middle, $"near {near} should exceed middle {middle}");
        Assert.True(middle > far, $"middle {middle} should exceed far {far}");
        Assert.True(far > 0, "still inside the range, so still audible");
    }

    /// <summary>Outside the range a source contributes nothing at all.</summary>
    /// <remarks>
    /// The range is the field's <b>cutoff</b> and the only thing separating this query from
    /// near-road pollution beyond the weights. ⚠ It is authored in metres by the Ruleset and is
    /// <b>unratified</b>: <c>02 §2.4</c>'s <em>50–300 m</em> is a band six times wide, the same defect
    /// <c>plans/0012</c> already records against the industrial kernel's 1–10 km.
    /// </remarks>
    [Fact]
    public void Past_the_range_a_source_is_not_heard()
    {
        RoadGraph graph = RoadFixtures.Chain(4);

        graph.Segments.VolumeForward[0] = 400;

        LineSource shortRange = new(new Tiles(10), Fixed.One);

        Assert.Equal(0, LineSourceQueries.Noise(graph, shortRange, new Tiles(16), new Tiles(40)));
        Assert.True(LineSourceQueries.Noise(graph, shortRange, new Tiles(16), new Tiles(4)) > 0);
    }

    /// <summary>
    /// <b>The query sums, and it sums INTENSITIES rather than levels.</b> The first constraint the stub
    /// was written to carry, and the arithmetic trap underneath it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>02 §2.4</c> says the falloff is logarithmic <em>and</em> that the query sums, and never says
    /// in which domain. Both readings are available from the prose and only one is right: two equal
    /// sources are <b>half a bel</b> louder, not twice as loud. This test fails under a log-domain sum,
    /// which is the implementation somebody reading those two sentences would naturally write.
    /// </para>
    /// <para>
    /// It also fails under a <em>nearest-source</em> query, which is <c>02 §2.5</c> question 3 and the
    /// property that decides whether a query is admissible at all — a Lot caught between two busy roads
    /// is the Lot a player asks about.
    /// </para>
    /// </remarks>
    [Fact]
    public void Two_roads_are_louder_than_one_and_not_twice_as_loud()
    {
        RoadGraph graph = RoadFixtures.Chain(4);

        // (48, 8) fronts the middle Segment, which is left SILENT so the background is zero and the
        // crossover admits both neighbours. The two are then equidistant by construction — the closest
        // point of each is an end of the middle block — so this measures the sum and nothing else.
        Tiles east = new(48);
        Tiles north = new(8);

        graph.Segments.VolumeForward[0] = 40;

        int one = LineSourceQueries.Noise(graph, Noise, east, north);

        graph.Segments.VolumeForward[2] = 40;

        int two = LineSourceQueries.Noise(graph, Noise, east, north);

        Assert.True(one > 0, "the first source is audible at all");
        Assert.True(two > one, $"summing, so {two} must exceed {one} — a nearest-source query returns one");
        Assert.True(two < 2 * one, $"a level, so {two} must be under twice {one} — summing levels doubles it");
    }

    /// <summary>
    /// <b>A source quieter than the road you are standing on does not join the sum.</b> The second
    /// constraint the stub was written to carry.
    /// </summary>
    /// <remarks>
    /// <c>02 §2.4</c> enumerates <em>by loudness rather than by road class</em>: a linear source counts
    /// only where its contribution here exceeds the ambient background, which is the level the Tile's
    /// own frontage Street already puts there. <b>Nobody authors that threshold</b> — it is a crossover,
    /// and it is what catches <c>adr/0029</c>'s Reserved band, which puts Arterial-scale volume on an
    /// ordinary grid Street and which enumeration by class would miss.
    /// </remarks>
    [Fact]
    public void A_distant_road_quieter_than_the_frontage_does_not_change_the_answer()
    {
        RoadGraph graph = RoadFixtures.Chain(4);

        Tiles east = new(48);
        Tiles north = new(2);

        graph.Segments.VolumeForward[1] = 200;

        int alone = LineSourceQueries.Noise(graph, Noise, east, north);

        // Far away and lightly used: below the background, so it is not enumerated.
        graph.Segments.VolumeForward[0] = 1;

        Assert.Equal(alone, LineSourceQueries.Noise(graph, Noise, east, north));
    }

    /// <summary>
    /// <b>An Arterial off the lattice is heard.</b> The test for the hybrid source set, and a
    /// lattice-only query returns zero here.
    /// </summary>
    /// <remarks>
    /// <c>StreetGrid</c> admits a Segment by geometry — both endpoints on the lattice, one step apart,
    /// kind <c>Street</c> — so an Arterial is in no lattice cell and would be invisible to a query that
    /// walked the index alone. ⚠ <b><c>02 §2.4</c> names <em>Arterials within ~300 m</em> as a source</b>,
    /// so that gap would have silenced the loudest roads in the model while every other test still
    /// passed. It is found through <see cref="StreetGrid.OffLatticeCount"/>, whose whole purpose is this.
    /// </remarks>
    [Fact]
    public void An_arterial_that_is_on_no_lattice_edge_is_still_a_source()
    {
        RoadGraph graph = RoadFixtures.Chain(4);

        Handle<RoadNode> a = graph.Nodes.Create(new Tiles(18), new Tiles(30));
        Handle<RoadNode> b = graph.Nodes.Create(new Tiles(18), new Tiles(94));

        Handle<RoadSegment> arterial = graph.Segments.Create(
            a, b, new Tiles(64), RoadKind.Arterial, TravelMode.Car, TravelMode.Car);

        graph.RebuildDerived();

        Assert.True(graph.Segments.Rows.TryResolve(arterial, out int slot));
        Assert.True(graph.Streets.OffLatticeCount > 0, "the Arterial is on no lattice edge");

        graph.Segments.VolumeForward[slot] = 60;

        Assert.True(
            LineSourceQueries.Noise(graph, Noise, new Tiles(24), new Tiles(60)) > 0,
            "a lattice-only query returns 0 here, and 02 §2.4 names Arterials as a source");
    }

    /// <summary>
    /// <b>Near-road pollution is the same query with different weights, and the two do not share a
    /// range.</b>
    /// </summary>
    /// <remarks>
    /// One implementation, two parameter sets. ⚠ <b>One <em>kernel</em> for two geometries is the defect
    /// <c>adr/0034</c> undid</b> — the old Pollution row was fed by industry at kilometres and traffic at
    /// 150 m through a single kernel, so one of them was always wrong. Sharing an implementation while
    /// keeping the parameters apart is the opposite of that, and this pins the difference.
    /// </remarks>
    [Fact]
    public void Near_road_pollution_reaches_a_different_distance_from_noise()
    {
        RoadGraph graph = RoadFixtures.Chain(4);

        graph.Segments.VolumeForward[0] = 200;

        LineSource shorter = new(new Tiles(12), Fixed.One);

        Tiles east = new(16);
        Tiles north = new(30);

        Assert.True(LineSourceQueries.Noise(graph, Noise, east, north) > 0);
        Assert.Equal(0, LineSourceQueries.NearRoadPollution(graph, shorter, east, north));
    }

    /// <summary>
    /// <b>The composition is bounded above by zero, and that is the milestone's honest shortfall
    /// rather than a bug.</b>
    /// </summary>
    /// <remarks>
    /// Amenity is desirability's only positive term and it needs a <b>kind</b> on a Business, at
    /// milestone 15 (<c>adr/0123</c>). Until then every Cell rests at zero or below and the most
    /// valuable land in the city is clean, quiet and empty. ⚠ <b>This test is the shortfall stated as a
    /// property.</b> When it starts failing, that is amenity arriving — not a regression — and this
    /// assertion and <c>DesirabilityShortfallTests</c> go together.
    /// </remarks>
    [Fact]
    public void Desirability_is_never_positive_while_amenity_is_absent()
    {
        RoadGraph graph = RoadFixtures.Chain(4);
        MapLayers layers = new(LayerRuleset.Default);

        graph.Segments.VolumeForward[0] = 80;
        layers.EmitPollution(new Cells(0), new Cells(0), 400);
        layers.Step(Ticks.Zero, graph, TerrainRuleset.None);

        DesirabilityWeights weights = new(Fixed.One, Fixed.One, Noise);

        for (int tile = 0; tile <= 96; tile += 8)
        {
            int value = layers.Desirability(graph, weights, new Tiles(tile), new Tiles(4));

            Assert.True(value <= 0, $"tile {tile} composed to {value}; there is no positive term");
        }
    }

    /// <summary>Quiet, clean ground is exactly zero — the field's maximum, and it is reachable.</summary>
    [Fact]
    public void Clean_and_silent_ground_composes_to_exactly_zero()
    {
        RoadGraph graph = RoadFixtures.Chain(4);
        MapLayers layers = new(LayerRuleset.Default);

        DesirabilityWeights weights = new(Fixed.One, Fixed.One, Noise);

        Assert.Equal(0, layers.Desirability(graph, weights, new Tiles(16), new Tiles(8)));
    }

    /// <summary>Each term subtracts, and each one on its own.</summary>
    /// <remarks>
    /// Two assertions rather than one, because a composition that read the same input twice — or
    /// dropped a term — would still be monotone in the other.
    /// </remarks>
    [Fact]
    public void Both_terms_subtract_and_neither_is_the_other()
    {
        RoadGraph graph = RoadFixtures.Chain(4);
        MapLayers layers = new(LayerRuleset.Default);

        DesirabilityWeights weights = new(Fixed.One, Fixed.One, Noise);

        Tiles east = new(16);
        Tiles north = new(6);

        int clean = layers.Desirability(graph, weights, east, north);

        graph.Segments.VolumeForward[0] = 80;

        int noisy = layers.Desirability(graph, weights, east, north);

        Assert.True(noisy < clean, $"noise subtracts: {noisy} should be under {clean}");

        // Emitting fills the SOURCE column; the field a query reads is the convolution of it, so the
        // cadence has to run before pollution exists anywhere. Tick 0 is due for it.
        layers.EmitPollution(CellGrid.ToCells(east), CellGrid.ToCells(north), 400);
        layers.Step(Ticks.Zero, graph, TerrainRuleset.None);

        int fouled = layers.Desirability(graph, weights, east, north);

        Assert.True(fouled < noisy, $"pollution subtracts too: {fouled} should be under {noisy}");
    }
}
