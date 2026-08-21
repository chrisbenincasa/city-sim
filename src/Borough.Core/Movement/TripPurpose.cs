namespace Borough.Core.Movement;

/// <summary>
/// Why a Trip is being made. <b>Spelled in full and never abbreviated</b> — <c>PurposeTag</c> is the
/// counter-based RNG tag policed by <c>BOR0801</c>–<c>BOR0803</c>, an unrelated concept one word
/// away, in a corpus whose first rule is exactly one meaning per term.
/// </summary>
/// <remarks>
/// <para>
/// <b>This enumeration is deliberately short, and its shortness is a statement about the build
/// rather than about the design.</b> The corpus names seven Trip generators, each owned by a
/// decision that is settled: the <b>commute</b> (<c>adr/0081</c>), <b>shopping</b>
/// (<c>adr/0067</c>), <b>school</b> (<c>adr/0032</c>), <b>dispatch</b> (<c>adr/0030</c>),
/// <b>immigration</b> (<c>adr/0023</c>), <b>Office export</b>, and <b>freight</b> (<c>03 §6.6</c>).
/// All but one still lack a mechanism, so under <c>adr/0070</c> each is <em>unbuilt</em> rather than
/// refused — and a value here for a Trip nothing can generate would be a taxonomy invented at the
/// write site. <b>Each arrives with the slice that builds its generator.</b>
/// </para>
/// <para>
/// <b><see cref="Commute"/> arrived first, and the paragraph this replaces said it could not.</b> It
/// read <i>"the obvious first purpose is the commute and it is unavailable: there are no jobs"</i> —
/// true when it was written, and an <c>adr/0070</c> absence rather than a refusal, which is exactly
/// the kind that ends by somebody building the missing mechanism. Milestone 5b-bis built it.
/// </para>
/// </remarks>
public enum TripPurpose : byte
{
    /// <summary>
    /// No purpose recorded. <b>The unset value, and a Trip carrying it is a defect.</b>
    /// </summary>
    /// <remarks>
    /// Zero is unset rather than the commonest purpose for the reason <see cref="TripFate.InFlight"/>
    /// is zero: a freshly allocated row is zero-filled, and a default that named a real purpose would
    /// make every unwritten row read as a plausible Trip.
    /// </remarks>
    Unset = 0,

    /// <summary>
    /// A Household travelling to one provider on its Provider List. <b>The one generator whose
    /// mechanism is already specified down to its fields</b> (<c>adr/0067</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <i>"A Household travels to a shop on its Provider List, and finding the shelf empty is a
    /// <b>transaction</b> outcome recorded on the Household… a failed occasion costs one Trip, not
    /// <c>N</c>."</i> The provider is selected by a cursor that advances on failure and resets on
    /// success, and the Household gains three small fields and no collection.
    /// </para>
    /// <para>
    /// <b>Finding the shelf empty is not a Fate.</b> The Trip <see cref="TripFate.Completed"/> and
    /// what failed is the purchase — which is <c>adr/0076</c>'s first clause and the reason
    /// <c>adr/0067</c> is cited by it.
    /// </para>
    /// </remarks>
    Shopping = 1,

    /// <summary>
    /// A Trip somebody asked for through <see cref="Input.CommandKind.Trip"/>. <b>Not a claim about
    /// what any resident does</b>, and the only purpose in this enumeration that never will be.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It exists so that the absence of a generator is legible in the data rather than only in a
    /// plan.</b> <c>adr/0080</c> builds Phase 4 ahead of anything that generates a Trip, because every
    /// generator the corpus names is unmilestoned — so the Trips that exercise the Traveller cursor,
    /// Fate resolution and the Census enter through the Input Log on <c>CommandKind.Populate</c>'s
    /// precedent. A Trip a human asked for is reproduced by replay <em>by construction</em> and makes
    /// no assertion about the city, which is the property a sampled generator could not have had.
    /// </para>
    /// <para>
    /// <b>Nothing downstream may branch on this value, and that is the load-bearing rule.</b> Phase 4,
    /// volume attribution, Fate resolution and the Census must treat a commanded Trip exactly as they
    /// will treat a generated one. Reading it to <em>count</em> is fine; reading it to <em>decide</em>
    /// would leave a second code path waiting for the generator to arrive into, which is the drifted-copy
    /// failure with both copies live in one file.
    /// </para>
    /// <para>
    /// ⚠ <b>It was scheduled for deletion and it is kept, and the reason is that its job changed
    /// rather than ended.</b> <c>plans/0023</c> task 5 says to delete this value when the commute
    /// generator lands; the sentence it rests on — <c>adr/0080</c>'s — says the verb <i>"becomes a
    /// test affordance rather than the only door"</i>, which is a **demotion**. And the value is
    /// worth more after the generator than before it: while every Trip was commanded it distinguished
    /// nothing, and now it is the only thing that tells a fixture's Trips from a city's. The sentence
    /// above — <i>a Trip with this purpose in a real run is a Trip nobody meant to make</i> — became
    /// **checkable** on the day it stopped being vacuous. Deleting it would have left
    /// <see cref="CommandKind.Trip"/> either untagged or lying about its purpose.
    /// </para>
    /// </remarks>
    Commanded = 2,

    /// <summary>
    /// A Citizen travelling from home to their Workplace. <b>The first generated Trip in the
    /// project</b> (<c>adr/0081</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One a Day, home to work, and the return journey is deliberately absent.</b>
    /// <c>plans/0023</c> scopes the milestone to one Workplace and one Trip a Day; a Citizen's day
    /// becomes a <em>schedule</em> rather than a repeated Trip the moment <c>adr/0067</c>'s shopping
    /// or <c>adr/0032</c>'s school exists, and that is the point at which the daily occasion stops
    /// being a sweep. Modelling the evening leg now would be building half of that schedule with no
    /// way to say what the other half is.
    /// </para>
    /// <para>
    /// <b>The occasion is a <em>phase</em> rather than a schedule, and that is what keeps it off the
    /// Event Wheel.</b> A commute recurs every Day and the Wheel is exactly a Day long, so a Citizen
    /// armed once would sit in the same bucket for life — which makes the bucket a partition of the
    /// population by a constant, and a partition on a constant is derivable rather than scheduled.
    /// <c>CommuteRoster</c> is that partition, <c>(derived AND rebuilt)</c>, and the Wheel is left
    /// carrying only the thing whose next firing genuinely varies.
    /// </para>
    /// </remarks>
    Commute = 3,

    /// <summary>
    /// The move-in: a Citizen travelling from the gate their Household arrived at to the dwelling
    /// placement has just given it (<c>adr/0023</c>, <c>adr/0129</c>, milestone 11 task 6).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The Trip <c>adr/0023</c> describes, made in the order the build can make it.</b> That
    /// record reads *arrive as **Trips** … enter the Unplaced Pool, and house themselves* — Trip
    /// first, Pool second — and it cannot be built that way, because <see cref="TripTable.Start"/>
    /// takes an origin <em>and a destination</em> Address and a Household the Pool has not placed has
    /// no destination. ***A journey described in prose can name an endpoint the mechanism has to
    /// compute.*** <c>adr/0129</c> reorders it and keeps every property: the arrival is still
    /// physical, still located at a named gate, still bounded by that gate's throughput, and — here —
    /// still congestion-bearing.
    /// </para>
    /// <para>
    /// <b>Once per journey and never again, which is what separates it from
    /// <see cref="Commute"/>.</b> A commute is a daily occasion and is a <em>phase</em> rather than a
    /// schedule; a move-in happens on the Tick a Household is housed and has no recurrence at all. So
    /// it is started at the placement site rather than armed on the Wheel or partitioned into a
    /// roster.
    /// </para>
    /// <para>
    /// ⚠ <b>Only a Household that came through a gate makes one.</b> Three of the Unplaced Pool's
    /// four entry routes have no gate — a Household the city generated itself, one evicted by a
    /// demolition, one that decided to move — so their membership carries a default handle and there
    /// is no origin to travel from. A re-housed evictee moving across the city is a real journey and
    /// a <em>different</em> one; giving it this purpose would file an internal move as immigration,
    /// and the Departure readouts that read by channel would inherit the error.
    /// </para>
    /// <para>
    /// 🔴 ⚠ <b>It can fail on the Commute Budget, and that is the map working rather than the
    /// mechanism breaking.</b> <c>TripEngine</c> judges the Budget on <em>every</em> Trip and not
    /// only on a commute, and <c>adr/0089</c> sizes the map by how many Commute Budgets fit across
    /// it — so a far gate is outside one by construction: measured on <c>bordered.toml</c>, east
    /// <b>62</b> minutes by car and north <b>73</b> against a ceiling of <b>49</b>. A move-in from a
    /// far gate to a dwelling in the corner city therefore resolves
    /// <see cref="TripFate.ExceededCommuteBudget"/>. ***A far gate is made usable by a dwelling
    /// beside it, not by a faster road.***
    /// </para>
    /// </remarks>
    Immigration = 4,
}
