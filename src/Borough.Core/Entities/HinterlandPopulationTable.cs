namespace Borough.Core.Entities;

using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

/// <summary>
/// Who one Household behind an edge is made of. <b>A storage key, never a domain actor.</b>
/// </summary>
/// <remarks>
/// Exact adult Skill Tier counts, children, Life Stage and purse band form the key.
/// Returns can include child-only families that are ineligible for admission.
/// </remarks>
public readonly record struct HinterlandComposition(
    byte Stage,
    int AdultsTier1,
    int AdultsTier2,
    int AdultsTier3,
    int Children,
    int MoneyBand)
{
    /// <summary>How many adults one Household of this composition holds.</summary>
    public int Adults => AdultsTier1 + AdultsTier2 + AdultsTier3;

    /// <summary>How many people one Household of this composition holds.</summary>
    public int Members => Adults + Children;

    /// <summary>How many of this Household's adults hold <paramref name="tier"/>.</summary>
    /// <remarks>
    /// Only Skill Tiers 1 through 3 are represented; children are stored separately.
    /// </remarks>
    public int AdultsAt(byte tier) => tier switch
    {
        SchoolingRuleset.FloorTier => AdultsTier1,
        2 => AdultsTier2,
        SchoolingRuleset.TopTier => AdultsTier3,
        _ => 0,
    };

    /// <summary>The composition an authored entry states.</summary>
    public static HinterlandComposition Of(in HinterlandPopulationDefinition declared) =>
        new(
            declared.Stage,
            declared.AdultsTier1,
            declared.AdultsTier2,
            declared.AdultsTier3,
            declared.Children,
            declared.MoneyBand);
}

/// <summary>
/// The Households standing behind each edge, one row per exact composition.
/// </summary>
/// <remarks>
/// Authored rows persist at zero stock. Return-only rows have target zero and retire once
/// stock and reservations are empty. Edge counters retain flows after those rows retire.
/// </remarks>
[Table]
public sealed class HinterlandPopulationTable
{
    private readonly Rows<HinterlandPopulation> _rows;

    /// <param name="capacity">Initial slot count. A hint; returns grow the table.</param>
    public HinterlandPopulationTable(int capacity)
    {
        _rows = new Rows<HinterlandPopulation>("hinterland_population", capacity, Buffering.OneCopy);

        Edge = _rows.Saved<byte>("edge", Touch.Cold);
        Stage = _rows.Saved<byte>("stage", Touch.Cold);
        Authored = _rows.Saved<byte>("authored", Touch.Cold);

        AdultsTier1 = _rows.Saved<int>("adults_tier1", Touch.Cold);
        AdultsTier2 = _rows.Saved<int>("adults_tier2", Touch.Cold);
        AdultsTier3 = _rows.Saved<int>("adults_tier3", Touch.Cold);
        Children = _rows.Saved<int>("children", Touch.Cold);
        MoneyBand = _rows.Saved<int>("money_band", Touch.Cold);

        Target = _rows.Saved<int>("target", Touch.Cold);
        Opening = _rows.Saved<int>("opening", Touch.Cold);
        Stock = _rows.Saved<int>("stock", Touch.Cold);
        Reserved = _rows.Saved<int>("reserved", Touch.Cold);

        Replenished = _rows.Saved<long>("replenished", Touch.Cold);
        Returned = _rows.Saved<long>("returned", Touch.Cold);
        Admitted = _rows.Saved<long>("admitted", Touch.Cold);
        Turnover = _rows.Saved<long>("turnover", Touch.Cold);

        ReconsiderNumerator = _rows.Saved<long>("reconsider_numerator", Touch.Cold);
        RecoveryNumerator = _rows.Saved<long>("recovery_numerator", Touch.Cold);
        RecoveryDirection = _rows.Saved<sbyte>("recovery_direction", Touch.Cold);

        GroupNext = _rows.Derived<int>("group_next", Touch.Cold);

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<HinterlandPopulation> Rows => _rows;

    /// <summary>Which edge these Households stand behind.</summary>
    public Column<byte> Edge { get; }

    /// <summary>The Life Stage every Household in this group is in.</summary>
    public Column<byte> Stage { get; }

    /// <summary>Whether a Ruleset declared this composition, as against returns having created it.</summary>
    public Column<byte> Authored { get; }

    /// <summary>How many of each Household's adults hold Skill Tier 1.</summary>
    public Column<int> AdultsTier1 { get; }

    /// <summary>How many hold Skill Tier 2.</summary>
    public Column<int> AdultsTier2 { get; }

    /// <summary>How many hold Skill Tier 3.</summary>
    public Column<int> AdultsTier3 { get; }

    /// <summary>How many children each Household carries.</summary>
    public Column<int> Children { get; }

    /// <summary>Which third of the Hinterland's purse range each Household arrives holding.</summary>
    public Column<int> MoneyBand { get; }

    /// <summary>
    /// The Household count this group rests at.
    /// </summary>
    /// <remarks>
    /// Recovery approaches this count from either direction; return-only compositions have target zero.
    /// </remarks>
    public Column<int> Target { get; }

    /// <summary>What stood here when the group was created. The account's anchor.</summary>
    /// <remarks>
    /// Immutable baseline for the per-group population account, distinct from the recovery target.
    /// </remarks>
    public Column<int> Opening { get; }

    /// <summary>How many Households of this composition stand behind the edge now.</summary>
    public Column<int> Stock { get; }

    /// <summary>
    /// How many of them are already promised to a gate, waiting to be admitted.
    /// </summary>
    /// <remarks>
    /// Reservations are included in Stock. Always maintain 0 &lt;= Reserved &lt;= Stock.
    /// </remarks>
    public Column<int> Reserved { get; }

    /// <summary>Households the Outside has added to this group over its life.</summary>
    public Column<long> Replenished { get; }

    /// <summary>Households that have returned into this group from the city.</summary>
    public Column<long> Returned { get; }

    /// <summary>Households this group has sent into the city.</summary>
    public Column<long> Admitted { get; }

    /// <summary>Households dropped from this group for standing above its resting count.</summary>
    public Column<long> Turnover { get; }

    /// <summary>
    /// Progress towards this group's next reconsideration occasion, in Household-Ticks.
    /// </summary>
    /// <remarks>
    /// Carries the remainder modulo ReconsiderTicks. Clear it when unreserved stock is empty.
    /// </remarks>
    public Column<long> ReconsiderNumerator { get; }

    /// <summary>Progress towards this group's next replenished or dropped Household.</summary>
    /// <remarks>
    /// Carries the remainder modulo RecoveryTicks. Reset on a direction change or disabled recovery.
    /// </remarks>
    public Column<long> RecoveryNumerator { get; }

    /// <summary>Which way <see cref="RecoveryNumerator"/> is accruing: 1 towards, -1 away, 0 at rest.</summary>
    public Column<sbyte> RecoveryDirection { get; }

    /// <summary>The next composition behind the same edge, encoded. <c>(derived AND rebuilt)</c>.</summary>
    public Column<int> GroupNext { get; }

    /// <summary>How many people one Household in this group holds.</summary>
    public int Members(int slot) =>
        AdultsTier1[slot] + AdultsTier2[slot] + AdultsTier3[slot] + Children[slot];

    /// <summary>How many people this group holds.</summary>
    public long People(int slot) => (long)Stock[slot] * Members(slot);

    /// <summary>The composition this row is keyed by.</summary>
    public HinterlandComposition CompositionAt(int slot) =>
        new(
            Stage[slot],
            AdultsTier1[slot],
            AdultsTier2[slot],
            AdultsTier3[slot],
            Children[slot],
            MoneyBand[slot]);

    /// <summary>Whether this row holds <paramref name="composition"/> behind <paramref name="edge"/>.</summary>
    public bool Holds(int slot, MapEdge edge, in HinterlandComposition composition) =>
        Edge[slot] == (byte)edge && CompositionAt(slot) == composition;

    /// <summary>How many Households of this group nobody has promised to a gate.</summary>
    public int Free(int slot) => Stock[slot] - Reserved[slot];

    /// <summary>
    /// Opens a group behind an edge, threading it into that edge's list in slot order.
    /// </summary>
    /// <remarks>
    /// The caller updates the composition index after opening the row.
    /// </remarks>
    /// <returns>The row's slot.</returns>
    public int Open(
        HinterlandTable hinterlands,
        MapEdge edge,
        in HinterlandComposition composition,
        int stock,
        int target,
        bool authored)
    {
        ArgumentNullException.ThrowIfNull(hinterlands);
        ArgumentOutOfRangeException.ThrowIfNegative(stock);
        ArgumentOutOfRangeException.ThrowIfNegative(target);

        int slot = _rows.Resolve(_rows.Allocate());

        Edge[slot] = (byte)edge;
        Stage[slot] = composition.Stage;
        Authored[slot] = authored ? (byte)1 : (byte)0;

        AdultsTier1[slot] = composition.AdultsTier1;
        AdultsTier2[slot] = composition.AdultsTier2;
        AdultsTier3[slot] = composition.AdultsTier3;
        Children[slot] = composition.Children;
        MoneyBand[slot] = composition.MoneyBand;

        Target[slot] = target;
        Opening[slot] = stock;
        Stock[slot] = stock;

        Groups(hinterlands).InsertOrdered(HinterlandTable.SlotOf(edge), slot);

        return slot;
    }

    /// <summary>
    /// Frees a group nothing stands in, unlinking it from its edge first.
    /// </summary>
    /// <remarks>
    /// Only retire unauthored rows with zero stock and zero reservations.
    /// </remarks>
    public void Retire(HinterlandTable hinterlands, int slot)
    {
        ArgumentNullException.ThrowIfNull(hinterlands);

        Groups(hinterlands).Remove(HinterlandTable.SlotOf((MapEdge)Edge[slot]), slot);

        _rows.Free(_rows.At(slot));
    }

    /// <summary>The compositions behind one edge, in ascending slot order.</summary>
    public IndexList Groups(HinterlandTable hinterlands)
    {
        ArgumentNullException.ThrowIfNull(hinterlands);

        return new IndexList(hinterlands.GroupHead, hinterlands.GroupTail, GroupNext);
    }

    /// <summary>Rebuilds every per-edge list from the live rows.</summary>
    /// <remarks>
    /// Insertion order must match normal maintenance so save/reload preserves traversal order.
    /// </remarks>
    public void RebuildIndexes(HinterlandTable hinterlands)
    {
        ArgumentNullException.ThrowIfNull(hinterlands);

        hinterlands.GroupHead.Span.Clear();
        hinterlands.GroupTail.Span.Clear();
        GroupNext.Span.Clear();

        IndexList groups = Groups(hinterlands);

        for (int slot = 0; slot < _rows.SlotCount; slot++)
        {
            if (_rows.IsLive(slot))
            {
                groups.InsertOrdered(HinterlandTable.SlotOf((MapEdge)Edge[slot]), slot);
            }
        }
    }
}
