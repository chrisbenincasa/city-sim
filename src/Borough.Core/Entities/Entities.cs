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

/// <summary>
/// One declared <c>[[policy]]</c>, as the city currently governs it. See <see cref="Citizen"/> for why
/// it is empty.
/// </summary>
/// <remarks>
/// <b>The Policy itself lives in the Ruleset and this is what the player has done to it</b> — so a row
/// exists per declared Policy whether or not anybody has governed one, and <see cref="PolicyTable"/>
/// carries what that row means when nobody has.
/// </remarks>
public readonly struct Policy;

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
/// A membership of the unpremised pool: one Business currently looking for premises.
/// </summary>
/// <remarks>
/// <b><see cref="Unplaced"/>'s sibling, and the remark there applies unchanged</b> — a row of this
/// table is a <em>membership</em> rather than a thing in the world, its rows move between slots to
/// keep the pool dense, and nothing ever holds a <c>Handle&lt;Unpremised&gt;</c>.
/// ⚠ <b>It is a SEPARATE table rather than a discriminated column on the Unplaced Pool</b>, which is
/// <see href="../../docs/adr/0143-a-bin-hangs-off-its-owner-and-the-polymorphic-column-stays-unbuilt.md">adr/0143</see>
/// reaching a second relation: a polymorphic member handle would land its cost on
/// <c>Column.Fold</c> and <c>SaveHash.TargetsOf</c>, which are the instrument that proves this
/// simulation deterministic, to save one table that costs two columns.
/// </remarks>
public readonly struct Unpremised;

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

/// <summary>
/// The city's money supply of record, of which there is exactly one.
/// </summary>
/// <remarks>
/// <b>It is the world's own position in <em>money</em>, exactly as <see cref="Clock"/> is its position
/// in time, and it is a row for that struct's reason</b> — a scalar outside the field declaration is a
/// coverage hole in the save and in the State Hash by construction.
/// <para>
/// <b>It is deliberately not a column on <see cref="Treasury"/>.</b> <c>01 §6</c> distinguishes the two
/// by name where it prices a trade deficit — <em>"a different bill — the money supply, not the
/// treasury"</em> — because most of the supply is in Households and never passes through the city's
/// hands at all. See <see cref="MoneySupplyTable"/>.
/// </para>
/// </remarks>
public readonly struct MoneySupply;

/// <summary>
/// The commercial or industrial economic actor occupying a Building.
/// </summary>
/// <remarks>
/// <b>Empty for <see cref="Citizen"/>'s reason</b>, and an Occupant beside <see cref="Household"/>
/// rather than a second kind of <see cref="Building"/> — <c>adr/0113</c>. Milestone 10 gives it a
/// balance and nothing else: inputs, outputs, employment and market behaviour belong to the
/// milestones that already own them. See <see cref="BusinessTable"/>.
/// </remarks>
public readonly struct Business;

/// <summary>
/// A centre of Building density and the basin that drains to it. <b>Empty for <see cref="Citizen"/>'s
/// reason.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>Derived from the city and then saved</b> — <c>adr/0134</c>. A District is not authored, not
/// drawn by the player and not a partition of the map: it is what a watershed over
/// <see cref="Space.BuildingResidency.Density"/> finds, and the count follows the number of centres
/// rather than a ceiling on extent. The row holds the centre; the membership is one row per Cell in
/// <see cref="Space.DistrictCellTable"/>, which is where the extent actually lives.
/// </para>
/// <para>
/// ⚠ <b><c>(saved AND hashed)</c> and deliberately not <c>Derived</c></b>, so
/// <c>DerivedRebuildAuditTests</c> does not reach it and nothing here is owed a rebuild. The reason
/// arrives at milestone 12 task 4 rather than here: hysteresis, damping and persistence all consult
/// the <em>previous</em> extent, so a District that a load recomputed from scratch would be a
/// different District from the one that was saved — and the Pool Bins hanging off it would move
/// between Districts on a reload. A thing whose next value depends on its last one is state.
/// </para>
/// <para>
/// ⚠ <b>It is not <c>RoutingPartition</c> and must never be reused as one</b>
/// (<c>adr/0047</c>): routing never keys on the District.
/// </para>
/// </remarks>
public readonly struct District;
