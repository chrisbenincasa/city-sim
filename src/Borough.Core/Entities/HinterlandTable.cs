namespace Borough.Core.Entities;

using Borough.Core.Space;
using Borough.Core.Tables;

/// <summary>
/// One row per map edge: the population standing behind it, as a total and as a set of flows.
/// </summary>
/// <remarks>
/// <para>
/// <b>Four rows for the life of the world, and the slot is the edge.</b>
/// <c>CONTEXT.md</c> → Hinterland makes the edge the identity — <em>the economy behind one map edge,
/// shared by every Outside Connection on that edge</em> — and <see cref="Rules.Ruleset.Hinterlands"/>
/// declares at most one per edge. So there is nothing to allocate at runtime and no handle anybody
/// needs: <see cref="SlotOf"/> is the whole addressing scheme, and <see cref="Edge"/> is saved beside
/// it so a row says which edge it is rather than a reader having to know the convention.
/// </para>
/// <para>
/// <b>What is here is the edge's lifetime account, and it outlives the groups it came from.</b> A
/// composition nobody authored is freed once nothing stands in it
/// (<see cref="HinterlandPopulationTable"/>), so a sum over live groups is a sum over survivors —
/// which is exactly the shape <c>plans/0073</c> D10 refuses for the global account. These columns are
/// written at the same call that moves a group's own counter, so a retirement takes nothing with it.
/// </para>
/// <para>
/// ⚠ <b>It is not a simulated place and holds no money, prices or wages.</b> Those are
/// <see cref="Rules.HinterlandDefinition"/>, which is Ruleset content the designer authors; this is
/// the stock the city spends and the record of what it spent.
/// </para>
/// </remarks>
[Table]
public sealed class HinterlandTable
{
    /// <summary>How many edges a bounded map has, and therefore how many rows this table holds.</summary>
    public const int Edges = 4;

    private readonly Rows<Hinterland> _rows;

    /// <summary>Builds the four rows and stamps each with its edge.</summary>
    public HinterlandTable()
    {
        _rows = new Rows<Hinterland>("hinterland", Edges, Buffering.OneCopy);

        Edge = _rows.Saved<byte>("edge", Touch.Cold);
        GroupHead = _rows.Derived<int>("group_head", Touch.Cold);
        GroupTail = _rows.Derived<int>("group_tail", Touch.Cold);

        ReplenishedHouseholds = _rows.Saved<long>("replenished_households", Touch.Cold);
        ReplenishedPeople = _rows.Saved<long>("replenished_people", Touch.Cold);
        ReturnedHouseholds = _rows.Saved<long>("returned_households", Touch.Cold);
        ReturnedPeople = _rows.Saved<long>("returned_people", Touch.Cold);
        AdmittedHouseholds = _rows.Saved<long>("admitted_households", Touch.Cold);
        AdmittedPeople = _rows.Saved<long>("admitted_people", Touch.Cold);
        TurnoverHouseholds = _rows.Saved<long>("turnover_households", Touch.Cold);
        TurnoverPeople = _rows.Saved<long>("turnover_people", Touch.Cold);

        _rows.Seal();

        // MoneySupplyTable's line and its reason: the row count and the columns have to agree, so the
        // fixed rows go through the allocator rather than around it.
        for (int slot = 0; slot < Edges; slot++)
        {
            _rows.Allocate();
            Edge[slot] = (byte)EdgeAt(slot);
        }
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<Hinterland> Rows => _rows;

    /// <summary>Which edge this row is. Never <see cref="MapEdge.None"/>.</summary>
    public Column<byte> Edge { get; }

    /// <summary>The first composition standing behind this edge, in ascending slot order.</summary>
    /// <remarks>
    /// <b><c>(derived AND rebuilt)</c>, and the ordered insert is what makes that honest.</b> A group
    /// is threaded in ascending slot order however it arrived, so
    /// <see cref="HinterlandPopulationTable.RebuildIndexes"/> walking the live rows reproduces the
    /// order and not merely the membership — <see cref="Parking.CarParkResidency"/>'s test, which
    /// appending would fail the moment the free list recycled a slot.
    /// </remarks>
    public Column<int> GroupHead { get; }

    /// <summary>The last composition standing behind this edge.</summary>
    public Column<int> GroupTail { get; }

    /// <summary>Households the Outside has added here, over the life of the world.</summary>
    public Column<long> ReplenishedHouseholds { get; }

    /// <summary>People those Households hold.</summary>
    public Column<long> ReplenishedPeople { get; }

    /// <summary>Households that have left the city for here, over the life of the world.</summary>
    public Column<long> ReturnedHouseholds { get; }

    /// <summary>People those Households hold.</summary>
    public Column<long> ReturnedPeople { get; }

    /// <summary>Households this edge has sent into the city, over the life of the world.</summary>
    public Column<long> AdmittedHouseholds { get; }

    /// <summary>People those Households hold.</summary>
    public Column<long> AdmittedPeople { get; }

    /// <summary>
    /// Households the Outside has dropped from here because it held more than it rests at.
    /// </summary>
    /// <remarks>
    /// <b>Outside population turnover, and it is not a death</b> (<c>plans/0073</c> D2). Stock above
    /// the resting count leaves the same way it would have arrived — the recovery term working
    /// downwards — and naming it separately is what keeps a returning family from being reported as a
    /// casualty of anything the city did.
    /// </remarks>
    public Column<long> TurnoverHouseholds { get; }

    /// <summary>People those Households hold.</summary>
    public Column<long> TurnoverPeople { get; }

    /// <summary>Which row holds <paramref name="edge"/>'s population.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="MapEdge.None"/>, which has no row.</exception>
    public static int SlotOf(MapEdge edge)
    {
        if (edge == MapEdge.None)
        {
            throw new ArgumentOutOfRangeException(
                nameof(edge), "the interior of the map is not behind an edge and has no Hinterland.");
        }

        return (int)edge - 1;
    }

    /// <summary>Which edge a row holds the population of.</summary>
    public static MapEdge EdgeAt(int slot)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slot);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slot, Edges);

        return (MapEdge)(slot + 1);
    }
}
