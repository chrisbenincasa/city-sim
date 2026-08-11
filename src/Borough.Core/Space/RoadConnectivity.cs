namespace Borough.Core.Space;

/// <summary>
/// Connected components of the Road Graph, <b>computed once per mode</b>, so that a city well
/// connected for cars and broken for people reads as two different numbers rather than one.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the deliverable the <c>pool</c> scope is waiting on.</b> <c>04 §6</c> requires that
/// <i>"a District whose internal Road Graph is broken must still fail to distribute"</i>, which is
/// prose until something can answer <em>are these two places on the same network</em>. A component
/// label makes it one integer comparison and turns <c>pool</c> from a Bin lookup into a real
/// membership question.
/// </para>
/// <para>
/// <b>Per mode, and that is Severance made queryable rather than merely emergent.</b>
/// <c>CONTEXT.md</c> → Severance: <i>"a city can be perfectly well connected for cars and broken for
/// people, and the game can say so."</i> A single component label would answer for whichever mode
/// happened to be unioned and mislead silently about the other — and the mode it would mislead about
/// is the one the design calls its flagship emergent behaviour.
/// </para>
/// <para>
/// <b>⚠ The measurement is <see cref="StrandedOnFoot"/>, and this paragraph originally named
/// <c>FootComponents &gt; CarComponents</c> instead, which is wrong.</b> A component count over a
/// subgraph counts every node the mode cannot use as an isolated singleton, so it rises with the
/// size of the <em>other</em> mode's network rather than with Severance. <c>RoadSeveranceTests</c>
/// found this on the day it was written and filtered it out with a <c>Walkable</c> predicate — and
/// the filter never reached the runner, so <c>--roads</c> printed a <b>SEVERANCE</b> banner over the
/// shipped Ruleset, which severs nothing, for as long as the instrument existed. <b>A predicate
/// discovered in a test and left there is a correction with a blast radius of one call site.</b>
/// </para>
/// <para>
/// <b>Union-find, so this is <em>weak</em> connectivity, and the gap is recorded rather than
/// glossed.</b> <c>adr/0020</c> computes a Settlement as <i>"a connected component … a union-find
/// over data already being maintained, at effectively no cost"</i>, while <c>CONTEXT.md</c> →
/// Settlement defines one as <i>"a maximal set of Districts <b>mutually</b> reachable within the
/// Commute Budget"</i> — and <b>mutually reachable is strong connectivity, which coincides with weak
/// only on a symmetric relation.</b> S2's <c>Matrix/Connectivity.cs</c> recorded that exposure before
/// its numbers arrived so it could not be reasoned around afterwards, and it is carried here for the
/// same reason. It does not bite yet: the generator emits no one-way Segment, so every Arc has its
/// reverse and the two readings agree. It will bite the moment one-way streets exist, which the mode
/// masks already permit — and it belongs to the slice that has a travel-time matrix to be asymmetric
/// <em>in</em>, which is 5c and not this one.
/// </para>
/// <para>
/// <b>Labels are assigned in ascending slot order, which is what makes them hashable if they ever
/// need to be.</b> Union-find's roots depend on the order unions happened; renumbering by first
/// appearance does not. They are <c>(derived AND rebuilt)</c> today and so fold into nothing, but a
/// derived value that is not a function of the saved state is a trap for whoever promotes it.
/// </para>
/// </remarks>
public sealed class RoadConnectivity
{
    private int[] _parent = [];
    private int[] _label = [];
    private int[] _size = [];
    private bool[] _touched = [];

    /// <summary>Components of the <see cref="TravelMode.Car"/> subgraph. Isolated nodes count.</summary>
    public int CarComponents { get; private set; }

    /// <summary>
    /// Components of the <see cref="TravelMode.Foot"/> subgraph. Isolated nodes count, and here that
    /// is most of them.
    /// </summary>
    /// <remarks>
    /// <b>⚠ This said "greater than <see cref="CarComponents"/> exactly when Severance has happened",
    /// and it is false in both directions.</b> It is not sufficient: at the shipped <c>[roads]</c>
    /// this reads <b>66</b> against the car network's <b>8</b> over a city whose pedestrian network
    /// is in one piece, because 65 of the 66 are Arterial junctions nobody can walk to. It is not
    /// necessary either: severing one node from a graph whose car network is already in eight pieces
    /// moves nothing. <b>Use <see cref="StrandedOnFoot"/></b>, which is a count of places rather than
    /// of labels.
    /// </remarks>
    public int FootComponents { get; private set; }

    /// <summary>Nodes in the largest <see cref="TravelMode.Car"/> component.</summary>
    public int LargestCar { get; private set; }

    /// <summary>
    /// Nodes in the largest <see cref="TravelMode.Foot"/> component.
    /// </summary>
    /// <remarks>
    /// <b>Reported beside the count because the count alone cannot be read.</b> Eight components is
    /// a city in eight pieces or a city in one piece with seven stranded corners, and those are
    /// opposite diagnoses — an isolated node is its own component, and a grid intersection all four
    /// of whose Streets an Arterial ran over becomes one. The pair is what makes the number an
    /// answer rather than a figure.
    /// </remarks>
    public int LargestFoot { get; private set; }

    /// <summary>Live nodes an admitting Segment touches — somewhere a Vehicle can actually be.</summary>
    public int DrivableNodes { get; private set; }

    /// <summary>
    /// Live nodes at least one <see cref="TravelMode.Foot"/> Segment touches — <b>somewhere a
    /// pedestrian can actually stand</b>.
    /// </summary>
    /// <remarks>
    /// <b>The denominator <see cref="FootComponents"/> needs, and its absence made the runner report
    /// Severance where there is none.</b> An Arterial lays its own junction nodes; those carry no
    /// foot Segment, so each is trivially its own foot component. At the shipped <c>[roads]</c> that
    /// is <b>~65 motorway junctions</b> — the count rises with the Arterial count and never with
    /// Severance — so <c>FootComponents &gt; CarComponents</c> is true of a city with a perfectly
    /// intact pedestrian network, and <c>--roads</c> announced exactly that for a slice.
    /// </remarks>
    public int WalkableNodes { get; private set; }

    /// <summary>
    /// Walkable nodes cut off from the largest pedestrian piece. <b>This is Severance, as a number.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Zero at both shipped Rulesets, and that is a finding rather than a reassurance.</b> A sweep
    /// of 240 <c>[roads]</c> configurations against an Arterial-free control found the shipped
    /// 32-Tile lattice severed by nothing at any <c>foot_crossing_every</c> in <c>1..16</c> and at any
    /// Arterial count up to 32 — see <c>plans/0020</c>.
    /// </para>
    /// <para>
    /// <b>It measures disconnection and not detour</b>, which is the smaller half of what Severance
    /// means. A pedestrian who must walk four hundred metres to the nearest crossing is severed in
    /// every sense a player would recognise and is fully connected here. That half needs a shortest
    /// path, and nothing in <c>Borough.Core</c> searches the graph yet.
    /// </para>
    /// </remarks>
    public int StrandedOnFoot => WalkableNodes - LargestFoot;

    /// <summary>Recomputes both labellings from the Segments.</summary>
    /// <summary>Recomputes both labellings from the Segments.</summary>
    internal void Rebuild(RoadNodeTable nodes, RoadSegmentTable segments)
    {
        CarComponents = Label(
            nodes, segments, TravelMode.Car, nodes.CarComponent, out int car, out int drivable);
        FootComponents = Label(
            nodes, segments, TravelMode.Foot, nodes.FootComponent, out int foot, out int walkable);

        LargestCar = car;
        LargestFoot = foot;
        DrivableNodes = drivable;
        WalkableNodes = walkable;
    }

    private int Label(
        RoadNodeTable nodes,
        RoadSegmentTable segments,
        TravelMode mode,
        Tables.Column<int> into,
        out int largest,
        out int usable)
    {
        int slots = nodes.Rows.SlotCount;

        if (_parent.Length < slots)
        {
            _parent = new int[slots];
            _label = new int[slots];
            _size = new int[slots];
            _touched = new bool[slots];
        }

        for (int slot = 0; slot < slots; slot++)
        {
            _parent[slot] = slot;
            _label[slot] = Unlabelled;
            _touched[slot] = false;
        }

        for (int segment = 0; segment < segments.Rows.SlotCount; segment++)
        {
            if (!segments.Rows.IsLive(segment))
            {
                continue;
            }

            // The union is over the Segment rather than over each Arc, because union-find cannot
            // represent a direction. That is the weak-connectivity approximation, stated once here
            // and argued in this type's remarks.
            byte admits = (byte)(segments.ModesForward[segment] | segments.ModesBackward[segment]);

            if ((admits & (byte)mode) == 0)
            {
                continue;
            }

            if (nodes.Rows.TryResolve(segments.NodeA[segment], out int a)
                && nodes.Rows.TryResolve(segments.NodeB[segment], out int b))
            {
                _touched[a] = true;
                _touched[b] = true;

                Union(a, b);
            }
        }

        int components = 0;

        for (int slot = 0; slot < slots; slot++)
        {
            if (!nodes.Rows.IsLive(slot))
            {
                into[slot] = Unlabelled;
                continue;
            }

            int root = Find(slot);

            if (_label[root] == Unlabelled)
            {
                _label[root] = components++;
                _size[_label[root]] = 0;
            }

            into[slot] = _label[root];
            _size[_label[root]]++;
        }

        largest = 0;

        for (int component = 0; component < components; component++)
        {
            if (_size[component] > largest)
            {
                largest = _size[component];
            }
        }

        usable = 0;

        for (int slot = 0; slot < slots; slot++)
        {
            if (nodes.Rows.IsLive(slot) && _touched[slot])
            {
                usable++;
            }
        }

        return components;
    }

    /// <summary>A node no component contains — a freed slot, and never a live one.</summary>
    public const int Unlabelled = -1;

    private int Find(int node)
    {
        while (_parent[node] != node)
        {
            _parent[node] = _parent[_parent[node]];
            node = _parent[node];
        }

        return node;
    }

    private void Union(int a, int b)
    {
        int rootA = Find(a);
        int rootB = Find(b);

        if (rootA == rootB)
        {
            return;
        }

        // Lower slot wins, rather than by rank. The tree is shallower with rank, and it is not the
        // depth that matters here: a deterministic root makes the labelling reproducible before the
        // renumbering pass rather than only after it, so a test may assert on either.
        if (rootA < rootB)
        {
            _parent[rootB] = rootA;
        }
        else
        {
            _parent[rootA] = rootB;
        }
    }
}
