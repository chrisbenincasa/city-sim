using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// Which Buildings stand in a given Cell. The reverse index from a place to the things on it.
/// </summary>
/// <remarks>
/// <para>
/// <b>It did not exist, and every candidate first Trip generator needed it</b>
/// (<c>adr/0081</c>). <see cref="CellResidency"/> indexes Map Layer <em>cells</em> rather than
/// entities and <see cref="Frontage"/> maps a Lot to its Segment in one direction only, so there was
/// no Cell → Buildings and no Segment → Lots index anywhere in the core. It is built as its own thing
/// rather than inside the consumer that first wanted it, because job assignment,
/// <c>adr/0067</c>'s Provider List acquisition, <c>adr/0032</c>'s Attended services and
/// <c>02 §5.3</c>'s candidate sampling all want the same query — and a piece that widely shared,
/// built inside one consumer, acquires that consumer's shape and is then rewritten twice.
/// </para>
/// <para>
/// <b>⚠ This is not a catchment, and the distinction is load-bearing rather than pedantic.</b>
/// <see cref="LineSourceQueries.Amenity"/> records what a catchment is: <i>"a walkable catchment on
/// the Road Graph — a <b>time</b> rather than a distance"</i>, whose range <i>"no geometry on the Cell
/// grid can express"</i>. This answers a strictly geometric question — <em>which Buildings are in this
/// box of Cells</em> — and answers nothing about whether anyone can get to them. That is the two-stage
/// shape <c>adr/0081</c>'s assignment needs and it is why the stages must not be merged: **the box
/// supplies candidates and the walk decides acceptability**, so a Building across an unbridged
/// motorway is a candidate that fails, which is a Severance the model can report. An index that
/// pre-filtered by reachability would delete that reading and would need the Road Graph's Epoch.
/// </para>
/// <para>
/// <b>An intrusive index list whose owner is a coordinate.</b> `CLAUDE.md`'s standing rule is that
/// every variable-length collection in the core is a head index on the owner and a <c>next</c> index
/// on the element, both flat, and never a per-entity collection object. The element side is
/// <see cref="BuildingTable.CellNext"/>, a column, because an element is a row. The head and tail are
/// the two arrays below, because a Cell is not — there are <see cref="CellGrid.WorldCellCount"/> of
/// them whether or not any holds anything, and <c>BOR0901</c> rejects storage in a <c>[Table]</c> type
/// that is not a declared column. That is <see cref="CellResidency"/>'s argument reused: <i>"a column
/// would have to invent 16,384 rows to avoid one array."</i>
/// </para>
/// <para>
/// <b><c>(derived AND rebuilt)</c>, and it passes <c>05 §3</c>'s test for that rather than merely
/// wanting to.</b> The rule is that a structure earns the classification only if its <em>order</em> is
/// recoverable from saved state, not merely its membership. A Building's Cell is a function of its
/// Lot's saved position, and <see cref="IndexList.InsertOrdered"/> puts each list in ascending slot
/// order regardless of the order things were added — so a rebuild reproduces this **exactly rather
/// than plausibly**, which is the property <see cref="Rebuild"/>'s test asserts. Using
/// <see cref="IndexList.Append"/> here would reproduce *creation* order, which diverges from a
/// rebuild the moment the free list recycles a slot, and nothing would report it.
/// </para>
/// <para>
/// <b>256 KB, flat, and not sized from the population.</b> Two <c>int</c> arrays over the whole Cell
/// grid. That is a fixed cost of the map rather than of the city, which is deliberate: the
/// alternative is a structure whose size is a guessed ratio per 1,000 Citizens, and
/// <c>0002</c> §D1's table-sizing row is already the corpus's live example of what those cost.
/// </para>
/// </remarks>
public sealed class BuildingResidency
{
    /// <summary>
    /// Slot plus one, so a zeroed array reads as <em>empty</em> rather than as slot 0.
    /// </summary>
    /// <remarks>
    /// The encoding <see cref="IndexList"/> and <see cref="CellResidency"/> both use, for the reason
    /// both give: the empty state has to be the default value, or the structure needs an
    /// initialisation pass that somebody will eventually forget.
    /// </remarks>
    private readonly int[] _head = new int[CellGrid.WorldCellCount];

    private readonly int[] _tail = new int[CellGrid.WorldCellCount];

    /// <summary>
    /// How many Buildings each Cell holds. <b>A cached list length, and it is one so that a fair draw
    /// over a box costs a read per Cell instead of a walk per Cell.</b>
    /// </summary>
    /// <remarks>
    /// Derived from the lists it counts, maintained alongside them, and rebuilt with them — so it is
    /// exactly as recoverable from saved state as the index is, and no more. A third array is 64 KB on
    /// top of the 128 KB already here, which is the same fixed cost of the map rather than of the
    /// city.
    /// </remarks>
    private readonly int[] _count = new int[CellGrid.WorldCellCount];

    /// <summary>The list threaded through this index. Composed at the call site, never stored.</summary>
    private IndexList List(BuildingTable buildings) =>
        new(_head.AsSpan(), _tail.AsSpan(), buildings.CellNext);

    /// <summary>
    /// Rebuilds the whole index from the Lots the Buildings stand on.
    /// </summary>
    /// <remarks>
    /// <b>Walked in slot order, which is what makes the result exact.</b> A rebuild has no other order
    /// available, so a derived list that claims to be a pure function of saved state has to be one a
    /// slot-order walk produces — see the class remarks. Called from <c>World.RebuildDerived</c> after
    /// the Lot reverse index, because it reads a Building's Lot handle.
    /// </remarks>
    public void Rebuild(BuildingTable buildings, LotTable lots)
    {
        ArgumentNullException.ThrowIfNull(buildings);
        ArgumentNullException.ThrowIfNull(lots);

        Array.Clear(_head);
        Array.Clear(_tail);
        Array.Clear(_count);
        buildings.CellNext.Span.Clear();

        IndexList list = List(buildings);

        for (int slot = 0; slot < buildings.Rows.SlotCount; slot++)
        {
            if (buildings.Rows.IsLive(slot) && Cell(buildings, lots, slot, out int cell))
            {
                list.InsertOrdered(cell, slot);
                _count[cell]++;
            }
        }
    }

    /// <summary>Adds a Building to the Cell its Lot sits in.</summary>
    /// <remarks>
    /// <b>Ordered rather than appended even here, where the list is being maintained rather than
    /// rebuilt.</b> The two inserts have to agree or a saved-and-reloaded city drains this list in a
    /// different order from a continuously-run one, and no hash would report it because the list is
    /// derived.
    /// </remarks>
    public void Add(BuildingTable buildings, LotTable lots, int building)
    {
        ArgumentNullException.ThrowIfNull(buildings);
        ArgumentNullException.ThrowIfNull(lots);

        if (Cell(buildings, lots, building, out int cell))
        {
            List(buildings).InsertOrdered(cell, building);
            _count[cell]++;
        }
    }

    /// <summary>Takes a Building out of its Cell, before its row is freed.</summary>
    /// <remarks>
    /// <b>Before the row goes back, because the Lot handle is read off it</b> — the same ordering
    /// constraint the demolition path already has for vacating the Lot. A Building removed after its
    /// row was freed would leave a dangling entry that the next allocation of that slot would find
    /// itself already in, which is the recycled-slot defect
    /// <c>Invariant.CitizenIsNotAlreadyInThisHousehold</c> exists to catch one table over.
    /// </remarks>
    public void Remove(BuildingTable buildings, LotTable lots, int building)
    {
        ArgumentNullException.ThrowIfNull(buildings);
        ArgumentNullException.ThrowIfNull(lots);

        if (Cell(buildings, lots, building, out int cell))
        {
            List(buildings).Remove(cell, building);
            _count[cell]--;
        }
    }

    /// <summary>
    /// The Buildings standing in a box of Cells. <b>The hot query.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Allocation-free and string-free, and the caller owns the buffer</b> — the pattern
    /// <see cref="MapLayers.LayerCells"/> set as the project's first hot entry point. Slots go in,
    /// slots come out, nothing is cached and there is no collection to keep.
    /// </para>
    /// <para>
    /// <b>Truncates rather than throwing</b>, for <see cref="MapLayers.LayerCells"/>'s reason and for
    /// one of its own: <c>adr/0081</c>'s assignment wants a <em>sample</em> of candidates rather than
    /// all of them (<c>adr/0017</c> — satisficing, never optimising), so a caller that fills its buffer
    /// and stops is the ordinary case here rather than the degraded one.
    /// </para>
    /// <para>
    /// <b>Ascending Cell then ascending slot</b>, because the outer walk is row-major over the box and
    /// each list is in slot order. That is a deterministic order and callers may rely on it; what they
    /// may not do is read it as a *ranking*. Nearest-first belongs to something that moves
    /// (<c>CONTEXT.md</c> → there is no proximity scope), so a consumer that wants the closest
    /// Building sorts what it took, and one that wants a fair sample draws from it.
    /// </para>
    /// </remarks>
    /// <param name="area">The box, in Cells. Clamped to the map.</param>
    /// <param name="buildings">The table whose slots come back.</param>
    /// <param name="into">Where the slots go. Truncated if it is too small; see the return value.</param>
    /// <returns>How many slots were written.</returns>
    public int In(CellRect area, BuildingTable buildings, Span<int> into)
    {
        ArgumentNullException.ThrowIfNull(buildings);

        CellRect box = area.Clamp();
        Span<int> next = buildings.CellNext.Span;
        int written = 0;

        for (int north = box.North.Raw; north < box.NorthEnd.Raw; north++)
        {
            for (int east = box.East.Raw; east < box.EastEnd.Raw; east++)
            {
                int encoded = _head[CellGrid.Index(new Cells(east), new Cells(north))];

                while (encoded != 0)
                {
                    if (written == into.Length)
                    {
                        return written;
                    }

                    into[written++] = encoded - 1;
                    encoded = next[encoded - 1];
                }
            }
        }

        return written;
    }

    /// <summary>
    /// How many Buildings stand in a box of Cells. <b>The denominator of a fair draw over it.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This pair is the <em>sampling</em> entry point, where <see cref="In"/> is the
    /// <em>enumeration</em> one, and they exist because those are different questions.</b>
    /// <see cref="In"/> truncates from the box's north-west corner, which is right for a consumer that
    /// wants some Buildings and wrong for one that wants a fair draw — a Citizen sampling a box the
    /// size of a commute would only ever see its corner. A count and an ordinal give a caller a draw
    /// that is uniform over the Buildings in the box without materialising a list of them.
    /// </para>
    /// <para>
    /// <b>Uniform over Buildings rather than over Cells, and the alternative was tried first.</b>
    /// Drawing a Cell and then a Building inside it is the cheaper query and reads better on paper —
    /// a look that lands on empty ground <em>found nothing</em>, which is what looking for work in a
    /// city with none near you is. It is wrong here for a reason that is a fact about this project
    /// rather than about cities: the shipped Rulesets hold on the order of sixty Buildings across
    /// 16,384 Cells, so a Cell-uniform draw finds a Building roughly one occasion in four hundred and
    /// the mechanism would be unobservable in the only worlds anybody runs. It also makes job density
    /// stop attracting workers, which is a claim about labour nobody argued.
    /// </para>
    /// <para>
    /// <b>Both walk the box's counts rather than its lists, which is what the count array buys.</b>
    /// The cost is one <c>int</c> read per Cell in the box rather than one per Building — a box is
    /// bounded by the Commute Budget and a caller pays it once per look.
    /// </para>
    /// </remarks>
    /// <param name="area">The box, in Cells. Clamped to the map.</param>
    /// <returns>The count, which is zero for an empty or off-map box.</returns>
    public int CountIn(CellRect area)
    {
        CellRect box = area.Clamp();
        int total = 0;

        for (int north = box.North.Raw; north < box.NorthEnd.Raw; north++)
        {
            for (int east = box.East.Raw; east < box.EastEnd.Raw; east++)
            {
                total += _count[CellGrid.Index(new Cells(east), new Cells(north))];
            }
        }

        return total;
    }

    /// <summary>The Building at a given position in a box, counting row-major then by slot.</summary>
    /// <remarks>
    /// <b><see cref="In"/>'s order, addressed rather than enumerated</b> — ascending Cell, then
    /// ascending slot within a Cell. So an ordinal names the same Building on a continuously-run world
    /// and on a reloaded one, which is what <see cref="IndexList.InsertOrdered"/> is used for in the
    /// first place. It is an <em>address</em> and not a <em>ranking</em>: nearest-first belongs to
    /// something that moves (<c>CONTEXT.md</c> → there is no proximity scope).
    /// </remarks>
    /// <param name="area">The box, in Cells. Clamped to the map.</param>
    /// <param name="buildings">The table whose slots come back.</param>
    /// <param name="ordinal">A position from zero, below <see cref="CountIn"/>.</param>
    /// <returns>The Building's slot, or <see cref="Rows.NoSlot"/> when the position is past the end.</returns>
    public int NthIn(CellRect area, BuildingTable buildings, int ordinal)
    {
        ArgumentNullException.ThrowIfNull(buildings);
        ArgumentOutOfRangeException.ThrowIfNegative(ordinal);

        CellRect box = area.Clamp();
        Span<int> next = buildings.CellNext.Span;
        int remaining = ordinal;

        for (int north = box.North.Raw; north < box.NorthEnd.Raw; north++)
        {
            for (int east = box.East.Raw; east < box.EastEnd.Raw; east++)
            {
                int cell = CellGrid.Index(new Cells(east), new Cells(north));
                int here = _count[cell];

                if (remaining >= here)
                {
                    remaining -= here;
                    continue;
                }

                int encoded = _head[cell];

                for (int i = 0; i < remaining; i++)
                {
                    encoded = next[encoded - 1];
                }

                return encoded - 1;
            }
        }

        return Rows.NoSlot;
    }

    /// <summary>
    /// The Cell a Building stands in, or false when it has no Lot to stand on.
    /// </summary>
    /// <remarks>
    /// <b>A Building whose Lot has gone is skipped rather than reported.</b> It is a state the world
    /// passes through — the demolition path frees a Lot with no frontage after vacating it — and
    /// <c>Invariant.LotsAndBuildingsAgreeWhoIsWhere</c> is what says whether it persisted. An index
    /// that threw here would turn a whole-world check into a crash at an arbitrary write site.
    /// </remarks>
    private static bool Cell(BuildingTable buildings, LotTable lots, int building, out int cell)
    {
        cell = 0;

        if (!lots.Rows.TryResolve(buildings.Lot[building], out int lot))
        {
            return false;
        }

        Cells east = CellGrid.ToCells(lots.East[lot]);
        Cells north = CellGrid.ToCells(lots.North[lot]);

        if (!CellGrid.Contains(east, north))
        {
            return false;
        }

        cell = CellGrid.Index(east, north);

        return true;
    }
}
