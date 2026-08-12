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
/// decision that is settled: the <b>commute</b> (<c>CONTEXT.md</c> → Provider List), <b>school</b>
/// (<c>adr/0032</c>), <b>dispatch</b> (<c>adr/0030</c>), <b>immigration</b> (<c>adr/0023</c>),
/// <b>Office export</b>, and <b>freight</b> (<c>03 §6.6</c>). None of them has a mechanism, so under
/// <c>adr/0070</c> each is <em>unbuilt</em> rather than refused — and a value here for a Trip nothing
/// can generate would be a taxonomy invented at the write site, which is not this slice's to invent.
/// <b>Each arrives with the slice that builds its generator.</b>
/// </para>
/// <para>
/// <b>The obvious first purpose is the commute and it is unavailable: there are no jobs.</b> No
/// Office, no wages, no labour market — <c>06</c>'s no-milestone table lists them as
/// settled-and-unplaced. <i>Give every Household a synthetic workplace</i> is precisely the
/// compensating design position <c>adr/0070</c> forbids.
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
    /// <b>It is expected to be deleted</b> — the same sentence <c>CommandKind.Populate</c> carries, for
    /// the same reason. When milestone 5b-bis's commute generator lands (<c>adr/0081</c>), the verb
    /// becomes a test affordance rather than the only door, and a Trip with this purpose in a real run
    /// is a Trip nobody meant to make.
    /// </para>
    /// </remarks>
    Commanded = 2,
}
