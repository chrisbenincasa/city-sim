using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Space;

public sealed class RoadFacesTests
{
    [Fact]
    public void A_two_by_two_grid_has_four_closed_blocks_and_one_outer_face()
    {
        RoadGraph graph = Grid(3, 64);
        RoadFaces faces = graph.Faces;

        Assert.Equal(5, faces.Count);
        int[] closed = Closed(faces);
        Assert.Equal(4, closed.Length);
        Assert.All(closed, face => Assert.Equal(4, faces.Boundary(face).Length));
        Assert.Single(closed.Select(face => faces.ScaledArea(face)).Distinct());
        Assert.Equal(Six(64 * 64), faces.ScaledArea(closed[0]));
        Assert.Equal(4, closed.Select(faces.Anchor).Distinct().Count());
    }

    [Fact]
    public void Every_arc_borders_exactly_one_face_and_twins_border_opposite_sides()
    {
        RoadGraph graph = Grid(3, 64);
        RoadFaces faces = graph.Faces;

        int total = 0;
        for (int face = 0; face < faces.Count; face++)
        {
            foreach (int arc in faces.Boundary(face))
            {
                Assert.Equal(face, faces.FaceOf(arc));
                Assert.NotEqual(faces.SideOf(arc), faces.SideOf(faces.Twin(arc)));
                total++;
            }
        }

        Assert.Equal(graph.Arcs.Count, total);
    }

    [Fact]
    public void Find_returns_the_block_holding_a_point_and_nothing_outside()
    {
        RoadGraph graph = Grid(3, 64);
        RoadFaces faces = graph.Faces;

        int southWest = faces.Find(Q(32), Q(32));
        int northEast = faces.Find(Q(96), Q(96));
        Assert.True(southWest >= 0);
        Assert.True(northEast >= 0);
        Assert.NotEqual(southWest, northEast);
        Assert.Equal(southWest, faces.Find(Q(1), Q(63)));
        Assert.Equal(-1, faces.Find(Q(200), Q(32)));
        Assert.Equal(-1, faces.Find(Q(-5), Q(32)));
    }

    [Fact]
    public void A_dead_end_spur_joins_its_block_on_both_sides()
    {
        RoadGraph graph = Grid(2, 64);
        int corner = Node(graph, 0, 0);
        Handle<RoadNode> tip = graph.Nodes.Create(new Tiles(20), new Tiles(20));
        int spur = Segment(graph, graph.Nodes.Rows.At(corner), tip);
        graph.RebuildDerived();
        RoadFaces faces = graph.Faces;

        int block = Assert.Single(Closed(faces));
        Assert.Equal(6, faces.Boundary(block).Length);
        Assert.Equal(Six(64 * 64), faces.ScaledArea(block));
        int[] spurArcs = faces.Boundary(block).ToArray().Where(arc => graph.Arcs.Segment[arc] == spur).ToArray();
        Assert.Equal(2, spurArcs.Length);
    }

    [Fact]
    public void A_foot_path_across_a_block_does_not_split_it()
    {
        RoadGraph graph = Grid(2, 64);
        graph.Segments.Create(graph.Nodes.Rows.At(Node(graph, 0, 0)), graph.Nodes.Rows.At(Node(graph, 64, 64)),
            new Tiles(90), RoadKind.FootPath, TravelMode.Any, TravelMode.Any);
        graph.RebuildDerived();

        int block = Assert.Single(Closed(graph.Faces));
        Assert.Equal(4, graph.Faces.Boundary(block).Length);
    }

    [Fact]
    public void A_curved_street_and_its_chord_enclose_a_lens()
    {
        RoadGraph graph = Fresh();
        Handle<RoadNode> a = graph.Nodes.Create(new Tiles(0), new Tiles(0));
        Handle<RoadNode> b = graph.Nodes.Create(new Tiles(64), new Tiles(0));
        Segment(graph, a, b);
        int curve = Segment(graph, a, b);
        graph.Segments.Sagitta[curve] = SubTiles.FromTiles(new Tiles(-8));
        graph.RebuildDerived();
        RoadFaces faces = graph.Faces;

        int lens = Assert.Single(Closed(faces));
        Assert.Equal(2, faces.Boundary(lens).Length);
        Assert.Equal(lens, faces.Find(Q(32), Q(-4)));
        Assert.Equal(-1, faces.Find(Q(32), Q(8)));
        Assert.Equal(-1, faces.Find(Q(32), Q(-9)));
        Assert.Equal(-1, faces.Find(Q(2), Q(-7)));
    }

    [Fact]
    public void A_curved_side_moves_the_block_boundary()
    {
        RoadGraph graph = Grid(2, 64);
        int south = SegmentBetween(graph, 0, 0, 64, 0);
        graph.Segments.Sagitta[south] = SubTiles.FromTiles(new Tiles(8));
        graph.RebuildDerived();
        RoadFaces faces = graph.Faces;

        int block = Assert.Single(Closed(faces));
        Assert.True(faces.ScaledArea(block) < Six(64 * 64));
        Assert.Equal(-1, faces.Find(Q(32), Q(4)));
        Assert.Equal(block, faces.Find(Q(32), Q(10)));
    }

    [Fact]
    public void Rebuilding_preserves_faces_and_anchors()
    {
        RoadGraph graph = Grid(3, 64);
        var before = Snapshot(graph.Faces);
        graph.RebuildDerived();
        Assert.Equal(before, Snapshot(graph.Faces));
    }

    [Fact]
    public void Removing_a_street_merges_two_blocks_under_the_lower_anchor()
    {
        RoadGraph graph = Grid(3, 64);
        var anchors = Closed(graph.Faces).Select(graph.Faces.Anchor).ToHashSet();
        graph.Segments.Rows.Free(graph.Segments.Rows.At(SegmentBetween(graph, 64, 0, 64, 64)));
        graph.RebuildDerived();
        RoadFaces faces = graph.Faces;

        int[] closed = Closed(faces);
        Assert.Equal(3, closed.Length);
        Assert.Contains(closed, face => faces.ScaledArea(face) == Six(2 * 64 * 64));
        Assert.All(closed, face => Assert.Contains(faces.Anchor(face), anchors));
    }

    [Fact]
    public void A_generated_city_satisfies_euler_and_every_closed_face_holds_lattice_ground()
    {
        WorldKey key = WorldKey.FromSeed(0x5EA1U);
        RulesetLoadResult loaded = RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "varied.toml"));
        World world = new(1_000, loaded.Ruleset!, key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);
        RoadGraph graph = world.Roads;
        RoadFaces faces = graph.Faces;

        var parent = Enumerable.Range(0, graph.Nodes.Rows.SlotCount).ToArray();
        int Root(int node) => parent[node] == node ? node : parent[node] = Root(parent[node]);
        var used = new bool[parent.Length];
        int edges = 0;
        for (int segment = 0; segment < graph.Segments.Rows.SlotCount; segment++)
        {
            if (!graph.Segments.Rows.IsLive(segment) || graph.Segments.Centerline[segment].Length == 0
                || (RoadKind)graph.Segments.Kind[segment] != RoadKind.Street)
            {
                continue;
            }

            int a = graph.Nodes.Rows.Resolve(graph.Segments.NodeA[segment]);
            int b = graph.Nodes.Rows.Resolve(graph.Segments.NodeB[segment]);
            used[a] = used[b] = true;
            parent[Root(a)] = Root(b);
            edges++;
        }

        int vertices = used.Count(x => x);
        int components = Enumerable.Range(0, parent.Length).Count(node => used[node] && Root(node) == node);
        Assert.True(edges > 50, $"edges {edges} vertices {vertices} faces {faces.Count} blocks {graph.Streets.Blocks}");
        Assert.Equal(1 + components, vertices - edges + faces.Count);

        var hit = new HashSet<int>();
        for (int column = 0; column < graph.Streets.Blocks; column++)
        {
            for (int row = 0; row < graph.Streets.Blocks; row++)
            {
                LandRectangle ground = world.BlockGroundRectangle(column, row);
                for (int quarter = 0; quarter < 9; quarter++)
                {
                    int face = faces.Find(Q(ground.X) + (Q(ground.Width) * (1 + (quarter % 3)) / 4),
                        Q(ground.Y) + (Q(ground.Height) * (1 + (quarter / 3)) / 4));
                    if (face >= 0)
                    {
                        hit.Add(face);
                    }
                }
            }
        }

        string Describe(int face) => $"face {face} area/6Q32 {faces.ScaledArea(face) / (6 * (Int128)Fixed.One * Fixed.One)}: " + string.Join(" ",
            faces.Boundary(face).ToArray().Select(arc => { var l = graph.Segments.Centerline[graph.Arcs.Segment[arc]]; var p = faces.SideOf(arc) == 0 ? l.A : l.B; return $"({p.East >> 16},{p.North >> 16})k{graph.Segments.Kind[graph.Arcs.Segment[arc]]}s{graph.Segments.Sagitta[graph.Arcs.Segment[arc]].Raw}"; }));
        Assert.True(Closed(faces).SequenceEqual(hit.Order()), string.Join("\n", Closed(faces).Except(hit).Select(Describe)));
    }

    private static RoadGraph Fresh() => new(RoadFixtures.Roads(blockTiles: 0, arterials: 0));

    private static RoadGraph Grid(int nodesPerSide, int spacing)
    {
        RoadGraph graph = Fresh();
        var nodes = new Handle<RoadNode>[nodesPerSide, nodesPerSide];
        for (int x = 0; x < nodesPerSide; x++)
        {
            for (int y = 0; y < nodesPerSide; y++)
            {
                nodes[x, y] = graph.Nodes.Create(new Tiles(x * spacing), new Tiles(y * spacing));
            }
        }

        for (int x = 0; x < nodesPerSide; x++)
        {
            for (int y = 0; y < nodesPerSide; y++)
            {
                if (x + 1 < nodesPerSide)
                {
                    Segment(graph, nodes[x, y], nodes[x + 1, y]);
                }

                if (y + 1 < nodesPerSide)
                {
                    Segment(graph, nodes[x, y], nodes[x, y + 1]);
                }
            }
        }

        graph.RebuildDerived();
        return graph;
    }

    private static int Segment(RoadGraph graph, Handle<RoadNode> a, Handle<RoadNode> b) =>
        graph.Segments.Rows.Resolve(graph.Segments.Create(a, b, new Tiles(64), RoadKind.Street,
            TravelMode.Any, TravelMode.Any));

    private static int Node(RoadGraph graph, int east, int north)
    {
        for (int slot = 0; slot < graph.Nodes.Rows.SlotCount; slot++)
        {
            if (graph.Nodes.Rows.IsLive(slot) && graph.Nodes.East[slot].Raw == east && graph.Nodes.North[slot].Raw == north)
            {
                return slot;
            }
        }

        throw new InvalidOperationException("No such node.");
    }

    private static int SegmentBetween(RoadGraph graph, int aEast, int aNorth, int bEast, int bNorth)
    {
        int a = Node(graph, aEast, aNorth);
        int b = Node(graph, bEast, bNorth);
        for (int slot = 0; slot < graph.Segments.Rows.SlotCount; slot++)
        {
            if (graph.Segments.Rows.IsLive(slot)
                && graph.Nodes.Rows.Resolve(graph.Segments.NodeA[slot]) == a
                && graph.Nodes.Rows.Resolve(graph.Segments.NodeB[slot]) == b)
            {
                return slot;
            }
        }

        throw new InvalidOperationException("No such segment.");
    }

    private static int[] Closed(RoadFaces faces) =>
        Enumerable.Range(0, faces.Count).Where(faces.IsClosed).ToArray();

    private static string Snapshot(RoadFaces faces) =>
        string.Join(";", Enumerable.Range(0, faces.Count).Select(face =>
            $"{faces.Anchor(face)}:{faces.ScaledArea(face)}:{string.Join(",", faces.Boundary(face).ToArray())}"));

    private static long Q(int tiles) => (long)tiles * Fixed.One;

    private static Int128 Six(long squareTiles) => 6 * (Int128)squareTiles * Fixed.One * Fixed.One;
}
