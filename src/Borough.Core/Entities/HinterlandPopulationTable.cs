namespace Borough.Core.Entities;

using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

/// <summary>
/// Who one Household behind an edge is made of. <b>A storage key, never a domain actor.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>The ordinary word for <em>who this family is</em></b> — a Life Stage, so many adults at each
/// Skill Tier, so many children, and which third of the Hinterland's purse range they carry.
/// <c>CONTEXT.md</c> → <i>Terms we deliberately do not use</i> bans Cohort by name and this is not
/// one: nothing keyed by a composition decides anything together, and a group is split the moment one
/// of its Households leaves.
/// </para>
/// <para>
/// <b>It is the runtime key and <see cref="HinterlandPopulationDefinition"/> is the authored
/// entry.</b> The two carry the same six fields and are deliberately different types: a definition
/// also states a count and whether a file declared it, and neither of those belongs in a key that
/// two Households have to compare equal on.
/// </para>
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
    /// <b>So admission can create them in tier order</b> without three near-identical loops, which
    /// is what makes the creation order a stated property rather than an accident of how the three
    /// fields happen to be written out.
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
/// <para>
/// <b>Aggregate rows, because the Outside is a count and not a population of individuals.</b>
/// <c>CONTEXT.md</c> → Hinterland calls it <em>a stock the city spends</em>: it is never Ticked and
/// never rendered, and nobody out there has a name, a Trip or a history. A row says
/// <em>this many Households, each made of exactly this</em>, and <see cref="Stock"/> times
/// <see cref="Members"/> is how many people that is. The first individual in the story is the
/// prospect that presents itself at a gate.
/// </para>
/// <para>
/// <b>A group is never rounded, split or substituted.</b> A Household returning from the city joins
/// the row whose composition it exactly matches, and creates one if there is none — so a Tier 3 adult
/// does not become a Tier 1 adult and a third child is not dropped to fit a template
/// (<c>plans/0073</c> D1). That is why the composition is the key rather than an authored list of
/// permitted family shapes.
/// </para>
/// <para>
/// <b><see cref="Authored"/> is the only thing separating an authored empty group from a returned
/// one</b>, and both exist. An authored count of zero is a decision — a composition the Outside keeps
/// none of but will hold returns in — so its row persists; a row returns created is freed once
/// nothing stands in it, or the table would grow with elapsed time in exactly the way
/// <c>adr/0006</c> forbids.
/// </para>
/// <para>
/// ⚠ <b>The per-edge list is <c>(derived AND rebuilt)</c> and the per-edge lifetime flows are
/// not.</b> A retired group takes its own counters with it, which is why
/// <see cref="HinterlandTable"/> accumulates the same flows beside it: the account has to survive the
/// rows it was made of.
/// </para>
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
    /// <b>The opening count doing a second job, which is <c>plans/0073</c> D2's decision.</b>
    /// Recovery works on the difference between what stands here and this, in both directions — so a
    /// composition returns created rests at zero and drains away again, and no code ever overwrites
    /// the stock with its target.
    /// </remarks>
    public Column<int> Target { get; }

    /// <summary>What stood here when the group was created. The account's anchor.</summary>
    /// <remarks>
    /// <b>Separate from <see cref="Target"/> because a returned group's two differ</b>, and separate
    /// from <see cref="Stock"/> for <see cref="MoneySupplyTable.Issued"/>'s reason: an invariant that
    /// recomputed the anchor from the live count would check that a write happened and never what was
    /// written.
    /// </remarks>
    public Column<int> Opening { get; }

    /// <summary>How many Households of this composition stand behind the edge now.</summary>
    public Column<int> Stock { get; }

    /// <summary>
    /// How many of them are already promised to a gate, waiting to be admitted.
    /// </summary>
    /// <remarks>
    /// <b>Counted in <see cref="Stock"/> and not beside it.</b> A family queueing outside a full gate
    /// has not left the Outside — it is still standing there, and it is still one of the people the
    /// edge holds — so double-counting it is what the account would do if this were a second total.
    /// What it cannot be is drawn twice, which is the whole of what this column bounds.
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
    /// <b>Occasions are accrued and never scheduled</b> (<c>plans/0073</c> D3). Each Tick the free
    /// Household count is added here and whole occasions are taken out at
    /// <c>reconsider_days × Ticks.PerDay</c> apiece, so a group of one presents roughly once every
    /// authored interval and a group of six hundred presents six hundred times as often. The
    /// remainder is carried, which is what keeps a small group from being rounded out of existence.
    /// ⚠ <b>It is cleared when the free stock reaches zero</b>: time spent empty is not credit earned
    /// by whoever returns later.
    /// </remarks>
    public Column<long> ReconsiderNumerator { get; }

    /// <summary>Progress towards this group's next replenished or dropped Household.</summary>
    /// <remarks>
    /// <b>One numerator for both directions, with <see cref="RecoveryDirection"/> saying which</b>
    /// (D2). A group that crosses its resting count clears the numerator before accruing the other
    /// way, because a fraction of a Household on its way in must not become a fraction of one on its
    /// way out.
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
    /// ⚠ <b>The caller indexes it.</b> The lookup index is a structure outside this table
    /// (<c>BOR0901</c>), so the two are kept in step by <c>World</c> rather than by this method — the
    /// same division <see cref="CarParkTable"/> and <see cref="Parking.CarParkResidency"/> have.
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
    /// ⚠ <b>The caller re-indexes.</b> <see cref="Open"/>'s division, for its reason.
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
    /// <b>Wholesale, and the ordered insert is what makes the claim checkable</b> —
    /// <see cref="Parking.CarParkResidency.Rebuild"/>'s reasoning: a list accumulated across a run and
    /// one rebuilt from the same rows have to agree, and the cheap guarantee is for the rebuild to be
    /// the definition.
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
