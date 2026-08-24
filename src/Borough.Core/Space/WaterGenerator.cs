using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// Places the Water Bodies and works out which way each one drains, from the
/// <see cref="WorldKey"/> and one Ruleset key.
/// </summary>
/// <remarks>
/// <para>
/// <c>adr/0034</c>, <c>adr/0156</c>, <c>adr/0159</c>, milestone 24 task 6a. <b>A world-creation pass
/// of its own</b>, called after <see cref="TerrainGenerator"/> and before <c>LayLand</c>.
/// </para>
/// <para>
/// ⚠ <b>It reads the SAME height field <see cref="TerrainGenerator"/> reads, on the same
/// <see cref="PurposeTag"/>, and that is deliberate rather than the correlation the tag rule
/// forbids.</b> <c>adr/0156</c> says the generator uses height <em>"to decide where water sits, which
/// ground floods, and what terrain type a Cell is"</em> — one field, three readings. Drawing water
/// from a second tag would put the sea somewhere unrelated to the low ground of the terrain it sits
/// in, which is not independence but nonsense. ***A distinct tag is owed to a distinct DECISION, and
/// where the ground is low is one decision.***
/// </para>
/// <para>
/// <b>Height is read here and stored nowhere</b> — <c>adr/0156</c>, and <c>adr/0021</c> as amended.
/// What survives is which Cells are wet, which body each belongs to, and which body each body spills
/// into.
/// </para>
/// <para>
/// <b>It authors no number.</b> The one number involved is <c>[water] sea_level_percent</c>, which is
/// Ruleset data and carries its own <c>plans/0002</c> §D1 row. Everything else here is derived: a body
/// is a connected component, and its outflow is found by walking downhill.
/// </para>
/// </remarks>
public static class WaterGenerator
{
    /// <summary>How many neighbours a Cell has for the purpose of being one body.</summary>
    /// <remarks>
    /// <b>Four and not eight, and the choice is visible in the result.</b> Eight-connectivity would
    /// join two ponds that touch only at a corner into one body — which is a body that is not
    /// connected as water, since nothing can flow through a corner. ⚠ It also decides the
    /// <em>shoreline</em> at task 7: a diagonal step is not a shared edge, so it is not shore.
    /// </remarks>
    private const int Neighbours = 4;

    /// <summary>Writes the Water Bodies, their extents and their drainage.</summary>
    /// <param name="bodies">The body rows. Must be empty.</param>
    /// <param name="cells">The wet-Cell rows. Must be empty.</param>
    /// <param name="residency">The index rebuilt as rows are made.</param>
    /// <param name="water">Where the sea stands. Unstated lays nothing at all.</param>
    /// <param name="key">The world key. The same key and Ruleset give the same water.</param>
    public static void LayInto(
        WaterBodyTable bodies,
        WaterCellTable cells,
        WaterResidency residency,
        WaterRuleset water,
        WorldKey key)
    {
        ArgumentNullException.ThrowIfNull(bodies);
        ArgumentNullException.ThrowIfNull(cells);
        ArgumentNullException.ThrowIfNull(residency);

        // A world whose Ruleset states no [water] is an inland world, and laying nothing is the whole
        // of what that means. adr/0159.
        if (!water.Stated)
        {
            return;
        }

        int[] height = ValueNoise.Field(key, PurposeTag.TerrainType);
        int sea = SeaLevel(height, water.SeaLevelPercent);

        // 0 means dry or not yet reached; a wet Cell carries its body number plus one, so that a
        // zeroed array reads as dry without an initialisation pass. IndexList's encoding and its
        // reason.
        var label = new int[CellGrid.WorldCellCount];
        var reaches = new bool[CellGrid.WorldCellCount];
        int found = Label(height, sea, label, reaches);

        var handles = new Handle<WaterBody>[found];

        for (int body = 0; body < found; body++)
        {
            handles[body] = bodies.Create();
        }

        Populate(cells, residency, label, handles);
        Drain(bodies, height, label, reaches, handles);
    }

    /// <summary>
    /// Where the sea stands, in the units of the field. <b>Against the range this key realised.</b>
    /// </summary>
    /// <remarks>
    /// <see cref="TerrainGenerator"/>'s self-normalising reading and its reason: a sum of uniforms is
    /// bell-shaped, so a level fixed against the theoretical ceiling would drown one key and leave the
    /// next dry. Against the realised range, every world has a coast.
    /// </remarks>
    private static int SeaLevel(int[] height, int percent)
    {
        int low = height[0];
        int high = height[0];

        for (int cell = 1; cell < height.Length; cell++)
        {
            if (height[cell] < low) { low = height[cell]; }
            if (height[cell] > high) { high = height[cell]; }
        }

        return low + IntegerMath.RoundDiv((high - low) * percent, 100);
    }

    /// <summary>
    /// Numbers the connected components of wet ground, in Cell index order.
    /// </summary>
    /// <remarks>
    /// <b>Index order is what makes a body's identity a property of the world rather than of the
    /// traversal</b>, so two runs on one key number the bodies identically. An explicit stack rather
    /// than recursion, because a component can be most of the map.
    /// </remarks>
    /// <returns>How many bodies there are.</returns>
    private static int Label(int[] height, int sea, int[] label, bool[] reaches)
    {
        var stack = new int[CellGrid.WorldCellCount];
        int found = 0;

        for (int start = 0; start < CellGrid.WorldCellCount; start++)
        {
            if (height[start] >= sea || label[start] != 0)
            {
                continue;
            }

            found++;
            int depth = 0;
            stack[depth++] = start;
            label[start] = found;

            while (depth > 0)
            {
                int at = stack[--depth];
                int east = EastOf(at);
                int north = NorthOf(at);

                // A body that reaches the map's edge drains out of the world. The edge is asked of
                // the GRID and never of residency, because off-map reads as dry.
                if (east == 0
                    || north == 0
                    || east == CellGrid.WorldCells - 1
                    || north == CellGrid.WorldCells - 1)
                {
                    reaches[found - 1] = true;
                }

                for (int step = 0; step < Neighbours; step++)
                {
                    int next = Step(east, north, step);

                    if (next < 0 || height[next] >= sea || label[next] != 0)
                    {
                        continue;
                    }

                    label[next] = found;
                    stack[depth++] = next;
                }
            }
        }

        return found;
    }

    /// <summary>Makes a row for every wet Cell and indexes it.</summary>
    private static void Populate(
        WaterCellTable cells, WaterResidency residency, int[] label, Handle<WaterBody>[] handles)
    {
        for (int at = 0; at < CellGrid.WorldCellCount; at++)
        {
            if (label[at] == 0)
            {
                continue;
            }

            var east = new Cells(EastOf(at));
            var north = new Cells(NorthOf(at));

            Handle<WaterCell> row = cells.Create(east, north, handles[label[at] - 1]);
            residency.Add(east, north, cells.Rows.Resolve(row));
        }
    }

    /// <summary>
    /// Points every landlocked body at the body it spills into.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The spill point is the body's lowest rim Cell</b> — the lowest dry ground touching it — and
    /// the outflow goes wherever water leaving over that point would go, which is found by walking
    /// downhill. ***That is why there is no outflow-direction key: the field already knows.***
    /// </para>
    /// <para>
    /// ⚠ <b>A body whose downhill walk reaches no other body keeps <c>default</c>, which reads as
    /// off-map</b>, and that is a known coarseness rather than a hidden case. Such a body is
    /// endorheic — it spills into a hollow that holds no water — and modelling where that water then
    /// goes needs a volume, which is a Bin, which is task 6b. **Recorded here so the answer is *it was
    /// noticed*.**
    /// </para>
    /// </remarks>
    private static void Drain(
        WaterBodyTable bodies, int[] height, int[] label, bool[] reaches, Handle<WaterBody>[] handles)
    {
        var rimHeight = new int[handles.Length];
        var rimAt = new int[handles.Length];

        for (int body = 0; body < handles.Length; body++)
        {
            rimHeight[body] = int.MaxValue;
            rimAt[body] = -1;
        }

        for (int at = 0; at < CellGrid.WorldCellCount; at++)
        {
            if (label[at] == 0 || reaches[label[at] - 1])
            {
                continue;
            }

            int east = EastOf(at);
            int north = NorthOf(at);

            for (int step = 0; step < Neighbours; step++)
            {
                int next = Step(east, north, step);
                int body = label[at] - 1;

                if (next < 0 || label[next] != 0 || height[next] >= rimHeight[body])
                {
                    continue;
                }

                rimHeight[body] = height[next];
                rimAt[body] = next;
            }
        }

        for (int body = 0; body < handles.Length; body++)
        {
            if (reaches[body] || rimAt[body] < 0)
            {
                continue;
            }

            int target = Downhill(height, label, rimAt[body], body + 1);

            if (target != 0 && Below(rimHeight, reaches, target - 1, body))
            {
                bodies.DrainsInto(handles[body], handles[target - 1]);
            }
        }
    }

    /// <summary>
    /// Whether one body may drain into another — <b>a strict order, which is what forbids a cycle.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>Without this, the water graph has cycles in it, and that is a measured fact rather than a
    /// worry.</b> A downhill walk leaving body A over its rim can land in B while B's own walk lands
    /// in A — the two basins spill into each other across a ridge, and each walk is individually
    /// correct. <c>WaterTests.Every_outflow_reaches_the_map_edge</c> found exactly that, and it is the
    /// assertion that would not have been written without reading the walk.
    /// </para>
    /// <para>
    /// <b>The order is the SPILL ELEVATION</b> — a basin's rim, the height water has to reach before
    /// it leaves — and a basin drains to one that spills lower. A body reaching the map edge is the
    /// sea and takes anything. Equal rims are broken by body number, which is Cell index order, so the
    /// tie-break is a property of the world rather than of the traversal. ***A strict total order
    /// cannot contain a cycle***, so nothing downstream has to check for one.
    /// </para>
    /// <para>
    /// ⚠ <b>Refusing the edge makes the body ENDORHEIC rather than rerouting it.</b> Where its water
    /// then goes needs a volume — how full the higher basin is before it spills — which is a Bin, and
    /// that is task 6b.
    /// </para>
    /// </remarks>
    private static bool Below(int[] rimHeight, bool[] reaches, int target, int source) =>
        reaches[target]
        || rimHeight[target] < rimHeight[source]
        || (rimHeight[target] == rimHeight[source] && target < source);

    /// <summary>
    /// Walks steepest descent from a rim Cell and returns the label it lands in, or 0.
    /// </summary>
    /// <remarks>
    /// <b>It terminates because every step is strictly lower</b>, and the field is finite. The
    /// neighbour order breaks ties, which is why <see cref="Step"/>'s order is fixed and not an
    /// implementation detail.
    /// </remarks>
    private static int Downhill(int[] height, int[] label, int from, int self)
    {
        int at = from;

        while (true)
        {
            if (label[at] != 0 && label[at] != self)
            {
                return label[at];
            }

            int east = EastOf(at);
            int north = NorthOf(at);
            int best = -1;

            for (int step = 0; step < Neighbours; step++)
            {
                int next = Step(east, north, step);

                // The source body is excluded from the whole walk, and that is the difference
                // between a spill and a puddle. A rim Cell is by construction the lowest DRY ground
                // touching the body, so its lowest neighbour is nearly always the body itself --
                // water would fall straight back in, the walk would terminate at the body's deepest
                // Cell, and every landlocked body would read as draining nowhere. Water leaving over
                // a rim flows away on the OTHER side.
                if (next < 0 || label[next] == self || height[next] >= height[at])
                {
                    continue;
                }

                if (best < 0 || height[next] < height[best])
                {
                    best = next;
                }
            }

            if (best < 0)
            {
                return 0;
            }

            at = best;
        }
    }

    /// <summary>The east coordinate of a Cell index.</summary>
    private static int EastOf(int at) => at % CellGrid.WorldCells;

    /// <summary>
    /// The north coordinate of a Cell index. <b><c>FloorDiv</c> because <c>BOR0203</c> asks for the
    /// rounding to be stated</b>, and flooring is what an index decomposition means.
    /// </summary>
    private static int NorthOf(int at) => IntegerMath.FloorDiv(at, CellGrid.WorldCells);

    /// <summary>
    /// One of the four Cells sharing an edge with this one, or <c>-1</c> if it is off the map.
    /// </summary>
    /// <remarks>
    /// <b>East, north, west, south, in that order, and the order is load-bearing</b> — it breaks
    /// height ties in <see cref="Downhill"/>, so changing it changes which way a lake drains and
    /// therefore the State Hash.
    /// </remarks>
    private static int Step(int east, int north, int step)
    {
        int nextEast = step switch { 0 => east + 1, 2 => east - 1, _ => east };
        int nextNorth = step switch { 1 => north + 1, 3 => north - 1, _ => north };

        if (nextEast < 0
            || nextNorth < 0
            || nextEast >= CellGrid.WorldCells
            || nextNorth >= CellGrid.WorldCells)
        {
            return -1;
        }

        return (nextNorth * CellGrid.WorldCells) + nextEast;
    }
}
