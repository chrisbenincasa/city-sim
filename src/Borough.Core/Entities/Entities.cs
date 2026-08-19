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
/// <para>
/// <b>⚠ That ranking is suspended, and the reason is the list's representation</b> (<c>adr/0066</c>).
/// K0 modelled the Provider List <em>inline</em> — 8 reserved entries on every Household, so it priced
/// what a Household is permitted to know rather than what it knows. It is an <b>intrusive index
/// list</b>, which is what <c>05 §4</c> required of every variable-length collection all along, so the
/// 104 bytes are an upper bound and the ordering against <see cref="Citizen"/> may reverse. The
/// replacement figure needs a capture and is a <c>plans/0002</c> §B row. <b>Be careful about this
/// schema anyway</b> — the caution the paragraph above draws is unaffected by which table is larger.
/// </para>
/// </remarks>
public readonly struct Household;

/// <summary>A structure on a Lot, with Occupants. See <see cref="Citizen"/> for why it is empty.</summary>
public readonly struct Building;

/// <summary>
/// The city's balance sheet — a singleton. See <see cref="Citizen"/> for why it is empty.
/// </summary>
/// <remarks>
/// It exists so that <c>Handle&lt;Treasury&gt;</c> is a different type from
/// <c>Handle&lt;Building&gt;</c>, and so that <see cref="TreasuryTable"/> has a row for the treasury's
/// Bins to hang off. Nothing constructs a handle to it today: the treasury is a singleton, so its Bins
/// are found through <c>BinOwnerKind.Treasury</c> and not through an id.
/// </remarks>
public readonly struct Treasury;

/// <summary>A parcel of land. See <see cref="Citizen"/> for why it is empty.</summary>
public readonly struct Lot;

/// <summary>
/// One Household's place in the Unplaced Pool.
/// </summary>
/// <remarks>
/// <b>The only row type here that is not a thing in the world.</b> A Lot, a Building and a Household
/// are nouns a player would use; a row of this table is a <em>membership</em>, and it exists because
/// the Pool needs to be drawn from in constant time and saved verbatim. Nothing ever holds a
/// <c>Handle&lt;Unplaced&gt;</c> — see <see cref="UnplacedTable"/>, whose rows move between slots.
/// </remarks>
public readonly struct Unplaced;

/// <summary>
/// The world's clock, of which there is exactly one.
/// </summary>
/// <remarks>
/// <b>The second row type here that is not a thing in the world, and the first that is not even a
/// membership.</b> A row of this table is the world's own position in time, and it is a row rather than
/// a field because the field declaration is what generates the save and folds the State Hash — so a
/// scalar outside it is a coverage hole by construction. See <see cref="ClockTable"/>.
/// </remarks>
public readonly struct Clock;
