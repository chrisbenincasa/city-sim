using Borough.Core.Arithmetic;
using Borough.Core.Entities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// Finds the Districts: a persistence-seeded watershed over the Building-density field, clipped to a
/// road component.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is <c>adr/0134</c> as an algorithm.</b> The ADR settles that a District is a centre and the
/// basin that drains to it, so the count follows the number of centres rather than a ceiling on
/// extent; a watershed is the operator that turns a field into exactly that. The field is
/// <see cref="BuildingResidency.Density"/> and nothing else — it is not a Map Layer, it is not
/// smoothed, and the decision not to smooth it was taken against a measurement rather than by argument
/// (<c>plans/0037</c> F8).
/// </para>
/// <para>
/// <b>Two floods, and the second is the one that assigns.</b> The first flood asks which maxima are
/// real: when two basins meet at level <em>h</em>, the lower peak dies, and its <em>prominence</em> is
/// its own height minus <em>h</em>. A peak whose prominence clears
/// <see cref="DistrictRuleset.ProminencePercent"/> is a genuine centre and becomes a seed; one that
/// does not is a bump on the side of a bigger hill. The second flood repeats the descent and refuses
/// exactly one merge — the one that would join two sets that each already hold a seed — which is what
/// draws the boundary. ⚠ <b>One flood cannot do this</b>: whether a merge should be refused depends on
/// a prominence that is not known until the whole descent has run.
/// </para>
/// <para>
/// <b>The threshold is relative — a percentage of the dying peak's own height</b> — and that is a
/// decision rather than a convenience. An absolute count would be silently tied to
/// <c>lots_per_segment = 5</c>, which is what makes a built Cell hold ten Buildings on the shipped
/// lattice: a threshold of, say, three would mean <em>a third of a peak</em> today and something else
/// entirely the day the lattice changes. ⚠ <b>It is hash-bearing and unratified</b>; its
/// <c>plans/0002</c> §D1 row names milestone 15 as the ratifier, because the field is flat on every
/// shipped Ruleset and a threshold over a flat field has nothing to discriminate.
/// </para>
/// <para>
/// <b>The road-component clip is constitutive and not a filter.</b> Two Cells never merge across a
/// road-component boundary however dense they both are, because a Pool that two Buildings cannot reach
/// each other through is not one Pool — <c>adr/0134</c> makes the component part of what a District
/// <em>is</em>. ⚠ <b>The component read is the FOOT subgraph</b>, which is the weaker of the two: a
/// world reachable on foot is reachable by car and not conversely, so the foot component is the one
/// that can actually separate a city that drives everywhere. ⚠ <b>No shipped Ruleset exercises the
/// clip</b> — <c>twinned.toml</c> is deliberately one component — so it is held by an in-code fixture
/// that cuts the corridor, and that absence is the point rather than a gap.
/// </para>
/// <para>
/// ⚠ <b>It runs once, at world creation.</b> No persistence, no hysteresis, no damping and no
/// per-evaluation Cell bound: those are milestone 12 task 4, and they are what earn a District its
/// <c>Saved</c> disposition. Nothing here reads the previous extent, so calling this twice on one
/// world produces the same answer twice — which will stop being true at task 4, on purpose.
/// </para>
/// <para>
/// ⚠ <b>It is not <c>RoutingPartition</c> and must not be reused as one</b>
/// (<c>adr/0047</c>).
/// </para>
/// </remarks>
public static class DistrictWatershed
{
    /// <summary>An ordinal that belongs to no basin yet.</summary>
    private const int NoOrdinal = -1;

    /// <summary>
    /// Replaces the world's Districts with the ones the field currently supports.
    /// </summary>
    /// <remarks>
    /// <b>It clears first and unconditionally</b>, including when <c>[districts]</c> is absent. A
    /// Ruleset that stops stating the table is a city that stops having Districts, and a stale row
    /// surviving that reload would be a District with no rule behind it — which is worse than none,
    /// because <c>Scope.Pool</c> would resolve against it.
    /// </remarks>
    public static void Evaluate(
        DistrictTable districts,
        DistrictCellTable cells,
        DistrictResidency residency,
        BuildingResidency density,
        BuildingTable buildings,
        LotTable lots,
        RoadGraph roads,
        DistrictRuleset rules)
    {
        ArgumentNullException.ThrowIfNull(districts);
        ArgumentNullException.ThrowIfNull(cells);
        ArgumentNullException.ThrowIfNull(residency);
        ArgumentNullException.ThrowIfNull(density);
        ArgumentNullException.ThrowIfNull(buildings);
        ArgumentNullException.ThrowIfNull(lots);
        ArgumentNullException.ThrowIfNull(roads);

        Clear(districts, cells, residency);

        if (!rules.Runs)
        {
            return;
        }

        Basins basins = Collect(density, buildings, lots, roads);

        if (basins.Count == 0)
        {
            return;
        }

        int[] order = ByDescendingDensity(basins);

        bool[] seeds = Seed(basins, order, rules.ProminencePercent);
        int[] owner = Assign(basins, order, seeds);

        Emit(districts, cells, residency, basins, owner);
    }

    /// <summary>Frees every District row and empties the index.</summary>
    private static void Clear(
        DistrictTable districts, DistrictCellTable cells, DistrictResidency residency)
    {
        for (int slot = cells.Rows.SlotCount - 1; slot >= 0; slot--)
        {
            if (cells.Rows.IsLive(slot))
            {
                cells.Rows.Free(cells.Rows.At(slot));
            }
        }

        for (int slot = districts.Rows.SlotCount - 1; slot >= 0; slot--)
        {
            if (districts.Rows.IsLive(slot))
            {
                districts.Rows.Free(districts.Rows.At(slot));
            }
        }

        residency.Rebuild(cells);
    }

    /// <summary>
    /// The built Cells, in Cell-index order, with each one's density and road component.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A full scan of the Cell grid rather than a walk of the Buildings</b>, and the reason is
    /// determinism rather than speed. The ordinals this produces are the identity every array below is
    /// keyed by, and a walk of the Building table would number them in Building-slot order — which
    /// depends on the order rows were allocated and recycled, so a saved-and-reloaded city could
    /// number the same Cells differently and tie-break a merge the other way. Cell-index order is a
    /// property of the map. The scan is 262,144 loads, once, at world creation.
    /// </para>
    /// <para>
    /// <b>A Cell's component is its lowest-slot Building's</b>, which is
    /// <see cref="BuildingResidency.NthIn"/> at ordinal zero because that list is kept in slot order.
    /// A Cell whose Buildings sit in two components is possible — 128 m of ground can straddle a cut —
    /// and it resolves to one of them rather than to both. ⚠ <b>That is a tie-break and not a
    /// finding</b>: nothing measures how often it happens, because no shipped Ruleset has two
    /// components at all.
    /// </para>
    /// </remarks>
    private static Basins Collect(
        BuildingResidency density, BuildingTable buildings, LotTable lots, RoadGraph roads)
    {
        int[] ordinalOf = new int[CellGrid.WorldCellCount];

        List<int> cellIndex = [];
        List<int> heights = [];
        List<int> components = [];

        for (int north = 0; north < CellGrid.WorldCells; north++)
        {
            for (int east = 0; east < CellGrid.WorldCells; east++)
            {
                Cells e = new(east);
                Cells n = new(north);

                int height = density.Density(e, n);

                if (height <= 0)
                {
                    continue;
                }

                ordinalOf[CellGrid.Index(e, n)] = cellIndex.Count + 1;

                cellIndex.Add(CellGrid.Index(e, n));
                heights.Add(height);
                components.Add(ComponentOf(density, buildings, lots, roads, e, n));
            }
        }

        return new Basins(
            [.. cellIndex], [.. heights], [.. components], ordinalOf);
    }

    /// <summary>
    /// The Foot component of the first Building in a Cell that has an Address, or <c>Unlabelled</c>.
    /// </summary>
    /// <remarks>
    /// <b>The first that HAS one, rather than simply the first.</b> A Lot sitting exactly on an
    /// intersection fronts nothing — <c>CONTEXT.md</c> → Address is emphatic that an Address is
    /// <em>never a Node</em>, and <see cref="Frontage.Locate"/> returns nothing for such a position
    /// rather than tie-breaking between two Segments. Taking the lowest-slot Building unconditionally
    /// made every Cell in a hand-built fixture answer <c>Unlabelled</c> while every Cell was in fact on
    /// a Street, which is a Cell reporting the property of one of its Buildings rather than its own.
    /// </remarks>
    private static int ComponentOf(
        BuildingResidency density,
        BuildingTable buildings,
        LotTable lots,
        RoadGraph roads,
        Cells east,
        Cells north)
    {
        CellRect cell = CellRect.At(east, north);
        int here = density.CountIn(cell);

        for (int ordinal = 0; ordinal < here; ordinal++)
        {
            int building = density.NthIn(cell, buildings, ordinal);

            if (building == Rows.NoSlot
                || !lots.Rows.TryResolve(buildings.Lot[building], out int lot))
            {
                continue;
            }

            int frontage = lots.FrontageSlot[lot];

            if (frontage == 0 || !roads.Segments.Rows.IsLive(frontage - 1))
            {
                continue;
            }

            if (roads.Nodes.Rows.TryResolve(roads.Segments.NodeA[frontage - 1], out int node)
                && roads.Nodes.FootComponent[node] != RoadConnectivity.Unlabelled)
            {
                return roads.Nodes.FootComponent[node];
            }
        }

        return RoadConnectivity.Unlabelled;
    }

    /// <summary>
    /// Whether two Cells may drain into one another. <b>An unlabelled Cell may never merge, including
    /// with another unlabelled Cell.</b>
    /// </summary>
    /// <remarks>
    /// <b>Unlabelled is <em>unknown</em>, and unknown must not be an equivalence class.</b> A Cell
    /// answers <c>Unlabelled</c> when nothing in it stands on a Street the connectivity pass reached —
    /// which says that its reachability is unknown, not that it shares a component with every other
    /// such Cell across the map. Comparing the two labels for equality made <c>Unlabelled</c> behave as
    /// the largest component in the world and silently bridged two genuinely separate ones; a fixture
    /// with two islands in it came out as one District with every assertion about the field passing.
    /// ⚠ <b>The consequence is visible rather than silent</b>: an unlabelled Cell becomes a District of
    /// its own, which is a strange answer somebody will notice, and the alternative was a merge nobody
    /// would.
    /// </remarks>
    private static bool Reaches(int a, int b) =>
        a != RoadConnectivity.Unlabelled && a == b;

    /// <summary>
    /// The ordinals sorted by density descending, ties in Cell-index order.
    /// </summary>
    /// <remarks>
    /// <b>A counting sort, because the key is small and the tie-break has to be exact.</b> Density is
    /// a Building count, so the key range is bounded by the Buildings in one Cell; and the ordinals go
    /// in already in Cell-index order, so a stable pass over them keeps that as the tie-break without
    /// a comparator. No general sort is called: <c>05 §4</c>'s determinism requirement is not that the
    /// sort be stable in this run but that it be the same sort in every run of every build.
    /// </remarks>
    private static int[] ByDescendingDensity(Basins basins)
    {
        int tallest = 0;

        foreach (int height in basins.Heights)
        {
            if (height > tallest)
            {
                tallest = height;
            }
        }

        int[] counts = new int[tallest + 2];

        foreach (int height in basins.Heights)
        {
            counts[height]++;
        }

        // Running total from the top down, so that height `tallest` starts at offset zero.
        int[] start = new int[tallest + 2];
        int running = 0;

        for (int height = tallest; height >= 1; height--)
        {
            start[height] = running;
            running += counts[height];
        }

        int[] order = new int[basins.Count];
        int[] cursor = (int[])start.Clone();

        for (int ordinal = 0; ordinal < basins.Count; ordinal++)
        {
            order[cursor[basins.Heights[ordinal]]++] = ordinal;
        }

        return order;
    }

    /// <summary>
    /// Which maxima are real: the first flood, which merges everything and records prominence.
    /// </summary>
    private static bool[] Seed(Basins basins, int[] order, int percent)
    {
        UnionFind sets = new(basins.Count);

        int[] peak = new int[basins.Count];
        bool[] active = new bool[basins.Count];
        bool[] seeds = new bool[basins.Count];

        for (int ordinal = 0; ordinal < basins.Count; ordinal++)
        {
            peak[ordinal] = ordinal;
        }

        foreach (Level level in Levels(basins, order))
        {
            for (int k = level.From; k < level.To; k++)
            {
                active[order[k]] = true;
            }

            for (int k = level.From; k < level.To; k++)
            {
                int here = order[k];

                foreach (int there in basins.Neighbours(here))
                {
                    if (!active[there]
                        || !Reaches(basins.Components[here], basins.Components[there]))
                    {
                        continue;
                    }

                    int a = sets.Find(here);
                    int b = sets.Find(there);

                    if (a == b)
                    {
                        continue;
                    }

                    // The lower peak dies here; the higher one carries on. A tie keeps `a`, which is
                    // the newly activated Cell's set -- arbitrary, and fixed rather than arbitrary at
                    // run time, which is the only property determinism asks of it.
                    bool keepA = basins.Heights[peak[a]] >= basins.Heights[peak[b]];

                    int high = keepA ? a : b;
                    int low = keepA ? b : a;

                    int dying = basins.Heights[peak[low]];
                    int prominence = dying - level.Height;

                    // Relative: prominence as a percentage of the dying peak's own height, compared
                    // without a division so nothing rounds.
                    if (prominence * 100 >= dying * percent)
                    {
                        seeds[peak[low]] = true;
                    }

                    sets.Union(low, high);
                    peak[sets.Find(high)] = peak[high];
                }
            }
        }

        // Every set that survives the whole descent has a maximum nothing ever drowned. Its prominence
        // is its full height -- there is no saddle -- so it is a centre at any threshold, and this is
        // what guarantees the assignment below leaves no Cell ownerless.
        for (int ordinal = 0; ordinal < basins.Count; ordinal++)
        {
            if (sets.Find(ordinal) == ordinal)
            {
                seeds[peak[ordinal]] = true;
            }
        }

        return seeds;
    }

    /// <summary>
    /// Which centre each Cell drains to: the second flood, which refuses seed-to-seed merges.
    /// </summary>
    private static int[] Assign(Basins basins, int[] order, bool[] seeds)
    {
        UnionFind sets = new(basins.Count);

        int[] owner = new int[basins.Count];
        bool[] active = new bool[basins.Count];

        Array.Fill(owner, NoOrdinal);

        foreach (Level level in Levels(basins, order))
        {
            // Activation and seeding both precede every merge at this level, so that a seed is in its
            // set before the set can be joined to anything.
            for (int k = level.From; k < level.To; k++)
            {
                int here = order[k];

                active[here] = true;

                if (seeds[here])
                {
                    owner[here] = here;
                }
            }

            for (int k = level.From; k < level.To; k++)
            {
                int here = order[k];

                foreach (int there in basins.Neighbours(here))
                {
                    if (!active[there]
                        || !Reaches(basins.Components[here], basins.Components[there]))
                    {
                        continue;
                    }

                    int a = sets.Find(here);
                    int b = sets.Find(there);

                    if (a == b)
                    {
                        continue;
                    }

                    // The watershed line, and the only line there is: two basins that each already
                    // drain to a centre stay two basins for ever.
                    if (owner[a] != NoOrdinal && owner[b] != NoOrdinal)
                    {
                        continue;
                    }

                    int carried = owner[a] != NoOrdinal ? owner[a] : owner[b];

                    sets.Union(a, b);
                    owner[sets.Find(a)] = carried;
                }
            }
        }

        int[] centre = new int[basins.Count];

        for (int ordinal = 0; ordinal < basins.Count; ordinal++)
        {
            centre[ordinal] = owner[sets.Find(ordinal)];
        }

        return centre;
    }

    /// <summary>Opens a District per surviving centre and files every Cell under one.</summary>
    /// <remarks>
    /// <b>In ordinal order, which is Cell-index order</b>, so the row a District lands in is a
    /// property of the map rather than of the flood. The State Hash folds a handle as the target row's
    /// monotonic id, so this ordering is the difference between a reproducible hash and a hash that
    /// depends on which basin happened to finish first.
    /// </remarks>
    private static void Emit(
        DistrictTable districts,
        DistrictCellTable cells,
        DistrictResidency residency,
        Basins basins,
        int[] centre)
    {
        Handle<District>[] opened = new Handle<District>[basins.Count];
        bool[] isOpen = new bool[basins.Count];

        for (int ordinal = 0; ordinal < basins.Count; ordinal++)
        {
            int seat = centre[ordinal];

            if (seat == NoOrdinal || isOpen[seat])
            {
                continue;
            }

            opened[seat] = districts.Create(basins.East(seat), basins.North(seat));
            isOpen[seat] = true;
        }

        for (int ordinal = 0; ordinal < basins.Count; ordinal++)
        {
            int seat = centre[ordinal];

            if (seat == NoOrdinal)
            {
                continue;
            }

            Cells east = basins.East(ordinal);
            Cells north = basins.North(ordinal);

            Handle<DistrictCell> row = cells.Create(east, north, opened[seat]);

            residency.Add(east, north, cells.Rows.Resolve(row));
        }
    }

    /// <summary>The runs of equal density in <paramref name="order"/>, tallest first.</summary>
    private static IEnumerable<Level> Levels(Basins basins, int[] order)
    {
        int from = 0;

        while (from < order.Length)
        {
            int height = basins.Heights[order[from]];
            int to = from;

            while (to < order.Length && basins.Heights[order[to]] == height)
            {
                to++;
            }

            yield return new Level(height, from, to);

            from = to;
        }
    }

    /// <summary>One flood step: every Cell at one density.</summary>
    private readonly record struct Level(int Height, int From, int To);

    /// <summary>
    /// The built Cells as a compact array, and the map back from a Cell to its place in it.
    /// </summary>
    private sealed class Basins(int[] cellIndex, int[] heights, int[] components, int[] ordinalOf)
    {
        /// <summary>How many Cells hold at least one Building.</summary>
        public int Count => cellIndex.Length;

        /// <summary>Each Cell's Building count, by ordinal.</summary>
        public int[] Heights => heights;

        /// <summary>Each Cell's road component, by ordinal.</summary>
        public int[] Components => components;

        /// <summary>A Cell's east coordinate, by ordinal.</summary>
        public Cells East(int ordinal) => new(cellIndex[ordinal] % CellGrid.WorldCells);

        /// <summary>A Cell's north coordinate, by ordinal.</summary>
        public Cells North(int ordinal) =>
            new(IntegerMath.FloorDiv(cellIndex[ordinal], CellGrid.WorldCells));

        /// <summary>
        /// The built Cells sharing an edge with this one — four-connected, never eight.
        /// </summary>
        /// <remarks>
        /// <b>Four-connected because eight-connected lets two basins touch at a corner</b>, and a
        /// corner is not a route: two Buildings diagonally across a Cell boundary are 128 m apart on
        /// the map and however far apart the Street lattice makes them. The whole point of the clip
        /// below is that adjacency in this operator has to mean something a Citizen could walk.
        /// </remarks>
        public IEnumerable<int> Neighbours(int ordinal)
        {
            int index = cellIndex[ordinal];
            int east = index % CellGrid.WorldCells;
            int north = IntegerMath.FloorDiv(index, CellGrid.WorldCells);

            if (east > 0 && Built(index - 1))
            {
                yield return Ordinal(index - 1);
            }

            if (east < CellGrid.WorldCells - 1 && Built(index + 1))
            {
                yield return Ordinal(index + 1);
            }

            if (north > 0 && Built(index - CellGrid.WorldCells))
            {
                yield return Ordinal(index - CellGrid.WorldCells);
            }

            if (north < CellGrid.WorldCells - 1 && Built(index + CellGrid.WorldCells))
            {
                yield return Ordinal(index + CellGrid.WorldCells);
            }
        }

        /// <summary>
        /// Whether a Cell holds a Building. <b>An unbuilt neighbour is not a neighbour</b> — it has no
        /// ordinal, so every array in the flood would be indexed at minus one.
        /// </summary>
        private bool Built(int index) => ordinalOf[index] != 0;

        private int Ordinal(int index) => ordinalOf[index] - 1;
    }

    /// <summary>
    /// Union-find over the ordinals, with union by size and path halving.
    /// </summary>
    /// <remarks>
    /// <b>Local to this file rather than shared with <see cref="RoadConnectivity"/></b>, which has its
    /// own. The two are answering different questions — that one labels a graph's components, this one
    /// merges basins during a descent and has to be told which side survives — so the shared thing
    /// would be four lines of <c>Find</c> and a disagreement about the rest.
    /// </remarks>
    private sealed class UnionFind
    {
        private readonly int[] _parent;
        private readonly int[] _size;

        public UnionFind(int count)
        {
            _parent = new int[count];
            _size = new int[count];

            for (int i = 0; i < count; i++)
            {
                _parent[i] = i;
                _size[i] = 1;
            }
        }

        public int Find(int node)
        {
            while (_parent[node] != node)
            {
                _parent[node] = _parent[_parent[node]];
                node = _parent[node];
            }

            return node;
        }

        /// <summary>
        /// Joins two sets. ⚠ <b>Not by size</b> — the caller has already decided which root survives,
        /// and a rank heuristic that overrode it would silently discard the surviving peak.
        /// </summary>
        public void Union(int loser, int winner)
        {
            int a = Find(loser);
            int b = Find(winner);

            if (a == b)
            {
                return;
            }

            _parent[a] = b;
            _size[b] += _size[a];
        }
    }
}
