namespace Borough.Core.Entities;

using Borough.Core.Rules;
using Borough.Core.Tables;

/// <summary>
/// People sharing a dwelling and finances. Holds the money, and the member list.
/// </summary>
/// <remarks>
/// <para>
/// <b>Economics is <see cref="Touch.Cold"/> — entirely, and that is adr/0004's concrete case.</b>
/// Income, expenses, savings, purchases made and purchases missed are touched when a Household
/// transacts or when a panel inspects it, never on an ordinary Tick. It is what makes <em>ship with
/// role and routine, leave room for economics</em> real rather than aspirational: the later layer
/// extends a cold set of columns instead of restructuring a hot loop.
/// </para>
/// <para>
/// <b>Unemployment is not a column here.</b> S4 task 2 made it a derived readout — <em>a Household
/// where no contained Citizen holds a job</em> — walked through the member list. Should a profile
/// ever want it cached, it is declared <see cref="Disposition.Derived"/> with an invariant asserting
/// it matches the walk, because a stale bit is a Household that believes it is employed and never
/// seeks work: silent, and hash-bearing.
/// </para>
/// </remarks>
[Table]
public sealed class HouseholdTable
{
    private readonly Rows<Household> _rows;

    /// <param name="capacity">Initial slot count. 360 Households per 1,000 Citizens, per S4 task 2.</param>
    /// <param name="buildings">The table this one's dwelling handles address.</param>
    /// <param name="bins">The table this one's balance handles address.</param>
    public HouseholdTable(int capacity, BuildingTable buildings, BinTable bins)
    {
        ArgumentNullException.ThrowIfNull(buildings);
        ArgumentNullException.ThrowIfNull(bins);

        _rows = new Rows<Household>("household", capacity, Buffering.OneCopy);

        Dwelling = _rows.SavedHandle("dwelling", buildings.Rows);
        LifeStage = _rows.Saved<byte>("life_stage");
        DwellingNext = _rows.Derived<int>("dwelling_next");
        PoolSlot = _rows.Derived<int>("pool_slot");
        MemberHead = _rows.Derived<int>("member_head");
        MemberTail = _rows.Derived<int>("member_tail");
        Balance = _rows.SavedHandle("balance", bins.Rows, reference: Reference.Required);

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<Household> Rows => _rows;

    /// <summary>The Building this Household lives in.</summary>
    public HandleColumn<Building> Dwelling { get; }

    /// <summary>Which of adr/0011's five Life Stages. Resolved through the Ruleset.</summary>
    public Column<byte> LifeStage { get; }

    /// <summary>Link in the dwelling's occupant list.</summary>
    public Column<int> DwellingNext { get; }

    /// <summary>
    /// This Household's position in the Unplaced Pool, <b>plus one</b>; zero means housed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Derived, and rebuilt from the Pool rather than the other way round</b> — the Pool table is
    /// the saved side, because a member is chosen by position and a position has to survive a reload.
    /// This is the reverse index, and it exists for the same reason <see cref="LotTable.BuildingSlot"/>
    /// does: without it, <em>is this Household in the Pool</em> is a walk, and <c>02 §10</c>'s
    /// staggered tier may only ask <c>O(1)</c> questions.
    /// </para>
    /// <para>
    /// <b>Plus-one encoded because a zeroed row must read as <em>housed</em>.</b> Slots are zero-filled
    /// on growth and on free, so position 0 and <em>no position</em> would otherwise be the same value
    /// — and every Household in a freshly grown table would claim to be at the front of the queue.
    /// </para>
    /// </remarks>
    public Column<int> PoolSlot { get; }

    /// <summary>Head of the member list — see <see cref="CitizenTable.MemberNext"/>.</summary>
    public Column<int> MemberHead { get; }

    /// <summary>Tail of the member list.</summary>
    public Column<int> MemberTail { get; }

    /// <summary>
    /// This Household's money Bin — its balance (<c>adr/0114</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A balance is a Bin because a Bin is the only thing a Rule can blame and wait on.</b> This
    /// was two <c>Saved&lt;Money&gt;</c> columns, <c>money</c> and <c>savings</c>, until milestone 10
    /// task 4c. A Rule short of money held in a column cannot say what stopped it, has no list to
    /// join, is woken by nothing and reports no <c>Blocking</c> — so <c>adr/0050</c>'s bankruptcy
    /// diagnosis, which is the whole payoff, could not be written.
    /// </para>
    /// <para>
    /// <b>The link is here rather than on the Bin, and the asymmetry with a Building has a reason.</b>
    /// A Building holds <em>many</em> Bins because its kind declares many Resources, so it carries a
    /// list and each Bin names it back through <see cref="BinTable.Owner"/>. An actor holds
    /// exactly <em>one</em>, because money is one Resource — so a single saved handle is the whole
    /// relationship, <c>World.FindActorBin</c> is O(1) rather than a list walk, and
    /// <c>World.RebuildDerived</c> has nothing to rebuild. ⚠ <b>A second conserved Resource makes this
    /// wrong and throws by name</b> rather than silently holding the first one; <c>adr/0114</c>'s own
    /// revisit trigger already calls that a decision rather than a detail.
    /// </para>
    /// <para>
    /// ⚠ <b>Unset means the Ruleset in force names no money, not that this Household is broke.</b> A
    /// Bin exists only for a Resource a Ruleset declares, so a balance is conditional on the file —
    /// which a column never was. <see cref="Reference.Required"/> is still right: an unset handle is
    /// not dangling, and a <em>set</em> one whose Bin was freed under a living Household is a defect
    /// <c>Invariant.CrossTableHandleResolves</c> reports for free.
    /// </para>
    /// <para>
    /// ✅ <b><c>savings</c> is deleted rather than moved, and it was never a second account.</b> Every
    /// design sentence about it describes a <em>threshold</em> — <c>adr/0024</c>'s <em>"reserve sized
    /// by its Life Stage"</em>, its revisit trigger's <em>"savings buffer… where velocity is set"</em>
    /// — so a Household has one pool and what varies is how much of it it will spend. The reserve is
    /// derived from Life Stage and the Ruleset in force when something spends money, which is
    /// milestone <b>14</b>. ***A threshold stored as a stock reads as a second account, and every
    /// document that later names the pair inherits it.***
    /// </para>
    /// </remarks>
    public HandleColumn<Bin> Balance { get; }

    /// <summary>Whether this Household is in the Unplaced Pool.</summary>
    public bool IsUnplaced(int slot) => PoolSlot[slot] != 0;

    /// <summary>Where in the Pool it is, or <see cref="Rows.NoSlot"/> if it is housed.</summary>
    public int PoolPosition(int slot) => PoolSlot[slot] - 1;

    /// <summary>Records that this Household is now at <paramref name="position"/> in the Pool.</summary>
    public void EnterPool(int slot, int position) => PoolSlot[slot] = position + 1;

    /// <summary>Records that this Household is no longer in the Pool.</summary>
    public void LeavePool(int slot) => PoolSlot[slot] = 0;
}
