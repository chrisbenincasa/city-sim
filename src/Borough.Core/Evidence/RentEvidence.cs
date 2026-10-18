namespace Borough.Core.Evidence;

using Borough.Core.Quantities;

/// <summary>
/// What one tenancy owes for its premises — <c>02 §9</c>'s rent answer
/// (<c>plans/money-circulation.md</c> L8, L16).
/// </summary>
/// <remarks>
/// <para>
/// <b>Numbers and not a sentence</b> (<c>adr/0002</c>). The Day is the Day; <em>behind on rent since
/// Day 14</em> is the host's string, and the Core never spells it.
/// </para>
/// <para>
/// ⚠ <b><see cref="Debt"/> is what says whether the tenant is behind, not <see cref="SinceDay"/>.</b>
/// Day 0 is a legitimate Day a shortfall can begin on, so zero there means nothing on its own — the
/// two columns are written as a pair and <see cref="Behind"/> is the question a reader actually has.
/// </para>
/// <para>
/// ⚠ <b>Absent rather than zeroed where the city charges no rent</b>, which is
/// <c>CitizenEvidence.HouseholdBalance</c>'s rule: a nullable meaning <em>this world has no rent</em>
/// is a state of the world a Ruleset authored, and printing a daily rent of zero over such a city
/// would teach that the tenancy is free rather than that nothing is collected.
/// </para>
/// </remarks>
/// <param name="Daily">What this tenancy is charged a Day, which is zero for a resident Landowner.</param>
/// <param name="Debt">Rent charged and not paid, carried forward.</param>
/// <param name="SinceDay">The Day the run of short Days began, where <paramref name="Debt"/> is positive.</param>
public readonly record struct RentEvidence(Money Daily, Money Debt, int SinceDay)
{
    /// <summary>Whether this tenant owes rent it could not pay.</summary>
    public bool Behind => Debt.Raw > 0;
}
