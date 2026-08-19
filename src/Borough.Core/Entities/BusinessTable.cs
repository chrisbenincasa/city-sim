namespace Borough.Core.Entities;

using Borough.Core.Quantities;
using Borough.Core.Tables;

/// <summary>
/// The Businesses, each occupying a Building and holding its own balance.
/// </summary>
/// <remarks>
/// <para>
/// <b>A balance is a column on the actor, because that is already how the one existing actor holds
/// one</b> (<c>adr/0113</c>). <see cref="HouseholdTable.Money"/> is a saved column rather than a Bin,
/// and <c>adr/0024</c>'s <em>"one integer per Household and per Business"</em> is trivial in the way
/// that sentence claims only if both actors spell it the same way.
/// </para>
/// <para>
/// <b>A Building never holds money, and this table is what makes that affordable.</b>
/// <see cref="BuildingTable.OccupantHead"/> is the head of a <em>list</em>, so a money Bin on the
/// Building would be a sum over however many Occupants are in it — an average wearing a total, which
/// is <c>adr/0025</c>'s Cohort prohibition applied to the container. A balance on the actor is right
/// at any cardinality, which is the property the Bin lacks.
/// </para>
/// <para>
/// <b>The occupant list stays homogeneous.</b> This is a <em>second</em> list on the Building rather
/// than one polymorphic list holding two row types — the third axis on the precedent of the second,
/// after <see cref="BuildingTable.OccupantHead"/> into <see cref="HouseholdTable.DwellingNext"/> and
/// <see cref="BuildingTable.WorkerHead"/> into <see cref="CitizenTable.WorkerNext"/>. So
/// <c>CONTEXT.md</c> → Occupant is a concept spanning two lists rather than a discriminated union,
/// which is what lint 7 wants anyway.
/// </para>
/// <para>
/// ⚠ <b>The balance is all there is, and stopping there is the decision.</b> A Business fully
/// modelled <em>"consumes inputs, produces outputs, employs Citizens, and offers Goods or services to
/// the market"</em> — milestones <b>12</b>, <b>13</b> and <b>15</b>. Milestone 10 needs exactly one
/// property: somewhere for conserved money to sit that is not a Building.
/// </para>
/// <para>
/// ⚠ <b>How many Businesses occupy one Building is undesigned</b> (<c>adr/0070</c>) and nothing in the
/// corpus states it. It does not block this table — a balance on the actor is right at any
/// cardinality — but it blocks the first money term a Rule fires on a workplace, because
/// <c>local</c> money must resolve to <em>an</em> actor and a list does not name one.
/// </para>
/// </remarks>
[Table]
public sealed class BusinessTable
{
    private readonly Rows<Business> _rows;

    /// <param name="capacity">Initial slot count.</param>
    /// <param name="buildings">The table this one's premises handles address.</param>
    public BusinessTable(int capacity, BuildingTable buildings)
    {
        ArgumentNullException.ThrowIfNull(buildings);

        _rows = new Rows<Business>("business", capacity, Buffering.OneCopy);

        // Severable, on CitizenTable.Workplace's precedent and for its reason: demolishing the
        // premises invalidates the handle with no hook, and `the premises stopped existing` is a fact
        // worth reading off a dangling handle rather than a state worth writing. The alternative --
        // clearing it in DestroyBuilding -- is a write to a saved column, so a demolition would move
        // the State Hash for a reason that has nothing to do with the demolition.
        Building = _rows.SavedHandle("building", buildings.Rows, reference: Reference.Severable);
        Money = _rows.Saved<Money>("money", Touch.Cold);
        BuildingNext = _rows.Derived<int>("building_next");

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<Business> Rows => _rows;

    /// <summary>The Building this Business occupies.</summary>
    public HandleColumn<Building> Building { get; }

    /// <summary>
    /// This Business's balance.
    /// </summary>
    /// <remarks>
    /// <b>Counted by <see cref="Invariants.Invariant.MoneyIsConserved"/> from the day the column
    /// exists</b>, which is what keeps the conservation walk honest: a place money can sit that the
    /// walk does not visit reads as money destroyed. Nothing funds one yet — a Business is created
    /// with a zero balance and there is no door that pays it, because the counterparty that would is
    /// milestone <b>13</b>'s price surface.
    /// </remarks>
    public Column<Money> Money { get; }

    /// <summary>Link in the premises' Business list.</summary>
    public Column<int> BuildingNext { get; }
}
