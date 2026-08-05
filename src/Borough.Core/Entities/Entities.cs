namespace Borough.Core.Entities;

/// <summary>
/// A person. Not a struct anybody constructs — a row spread across the columns of
/// <see cref="CitizenTable"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>These four types are empty on purpose, and the emptiness is the layout being honest.</b> Under
/// structure-of-arrays there is no row struct: a Citizen is a slot index that means something in
/// eleven separate arrays. Declaring <c>struct Citizen { … }</c> with the fields in it would be an
/// array-of-structures declaration that the storage then contradicts, and it is the shape somebody
/// reaches for when adding a field. There is nothing here to reach for.
/// </para>
/// <para>
/// What they exist for is <see cref="Tables.Handle{T}"/>: they are what makes
/// <c>Handle&lt;Citizen&gt;</c> a different type from <c>Handle&lt;Building&gt;</c>, so a Citizen
/// handle cannot index the Building table. adr/0004 types identities for the same reason adr/0003
/// types quantities.
/// </para>
/// <para>
/// <b>A Citizen has no <c>home</c>, and that is settled rather than missing.</b> S4 task 2 resolved
/// the ownership contradiction with a rule the corpus already contained for Buildings — <em>a field
/// lives at the level at which it can differ</em> — and a dwelling cannot differ between members of a
/// group defined as sharing one. Health, age, Skill Tier, experience, employment and current activity
/// can, so they are here; money, Needs, Life Stage and the dwelling are on the Household.
/// </para>
/// </remarks>
public readonly struct Citizen;

/// <summary>
/// People sharing a dwelling and finances. The unit that holds money, Needs and a Life Stage.
/// </summary>
/// <remarks>
/// K0 measured this as the <b>largest table in the world</b> at the 1M target — 75.2 MiB against the
/// Citizens' 53.4 MiB, from 2.8× fewer rows — driven by a Provider List that is 47% of the row. The
/// list is not in this slice; the finding is recorded here because it is the reason the Household
/// schema is the one to be careful about, not the Citizen one.
/// </remarks>
public readonly struct Household;

/// <summary>A structure on a Lot, with Occupants. See <see cref="Citizen"/> for why it is empty.</summary>
public readonly struct Building;

/// <summary>A parcel of land. See <see cref="Citizen"/> for why it is empty.</summary>
public readonly struct Lot;
