namespace Borough.Core.Determinism;

/// <summary>
/// The fourth coordinate of <see cref="Randomness.Draw"/>: what a draw is <em>for</em>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every distinct use gets a distinct tag.</b> Reusing one across two decisions makes those two
/// decisions the same coin flip — a Citizen who chooses a job badly also chooses a shop badly, always,
/// in a way no playtest reads as a bug. adr/0003 and 05 §4 both name this: the correlation is
/// <em>invisible at runtime</em>, so nothing observing a running city can find it.
/// </para>
/// <para>
/// <b>Never a string.</b> A string tag needs string hashing, which 02 §8 rule 2 bans outright, and a
/// mistyped one collides silently instead of failing to compile.
/// </para>
/// <para>
/// <b>Values are explicit and are never renumbered or reused.</b> A tag is an input to the hash, so
/// changing which integer a tag denotes changes every value drawn under it. That is a format change in
/// the sense adr/0003 gives the word — it invalidates stored Input Logs and State Hash baselines — and
/// it is invisible in a diff that only shows the enum. <b>Append; do not slot new tags in beside their
/// siblings.</b>
/// </para>
/// <para>
/// <b>Uniqueness is checked at build time, which is the only time it can be.</b> A duplicate value
/// here is exactly the silent correlation above, wearing valid syntax — <c>BOR0801</c>. Zero is
/// reserved by <c>BOR0802</c> and the backing type by <c>BOR0803</c>. The stopgap unit test slice 2
/// left here is gone: it caught a duplicate when somebody happened to run the suite, which can be
/// after values have been drawn under the wrong tag and after a State Hash baseline was taken over
/// them.
/// </para>
/// <para>
/// This enum is deliberately near-empty. Tags are added when a mechanism that draws is built, not in
/// advance — a tag with no caller cannot be checked against the draw it is supposed to name.
/// </para>
/// </remarks>
public enum PurposeTag : ulong
{
    /// <summary>
    /// Reserved, and never a valid argument to <see cref="Randomness.Draw"/>. It exists so that a
    /// default-initialised <see cref="PurposeTag"/> is recognisable rather than being some real tag's
    /// value — a zeroed struct field must not silently mean "the first purpose anyone declared".
    /// </summary>
    None = 0,

    /// <summary>
    /// Phase 3's settle order over the Rule evaluations due on one Tick.
    /// </summary>
    /// <remarks>
    /// <b>The first real tag in the project, and it is a shuffle rather than a choice.</b> <c>02 §8</c>
    /// rule 5: a contested outcome is settled by a counter-based shuffle, never by arrival and never by
    /// entity id — ordering by id is <em>biased</em>, so the same Building would win every contested
    /// draw for the life of the city, and nothing observing the running city could see why.
    /// </remarks>
    RuleSettleOrder = 1,

    /// <summary>
    /// The offset within its own rate at which a new Rule Instance first comes due.
    /// </summary>
    /// <remarks>
    /// <b>A stagger is not optional and it is not a free parameter.</b> Arming every Building's copy
    /// of a Rule at one delay puts the whole city into one Event Wheel bucket, so the Tick that bucket
    /// falls on pays for all of it and every other Tick pays nothing — <c>02 §2.4</c>'s reason for
    /// staggering the Layer passes, arriving at the Rule engine. What removes the free parameter is
    /// that the window is the Rule's **own <c>rate</c>**: a Rule re-arms at <c>+rate</c> for ever
    /// after, so spreading the first firing uniformly across <c>[1, rate]</c> is the only offset that
    /// stays spread and the only one that privileges no Tick. Nobody chooses a number.
    /// <para>
    /// <b>Distinct from <see cref="RuleSettleOrder"/> because they answer different questions about
    /// the same row</b> — <em>when does it first come due</em> against <em>who wins when two of them
    /// contend on one Tick</em>. Sharing a tag would correlate the two invisibly, which is what the
    /// one-tag-per-use rule exists to prevent: a Building armed early would win its contests.
    /// </para>
    /// </remarks>
    RuleArmingStagger = 2,

    /// <summary>
    /// Which Lots a Zone Rule looks at on one trigger.
    /// </summary>
    /// <remarks>
    /// <b>The first tag belonging to the Sweep family, and it answers a question about a Lot rather
    /// than about a Building.</b> <c>02 §5.3</c>: sampling is a behaviour model and not an
    /// optimisation — a developer does not evaluate every parcel in the city, so the Rule does not
    /// either.
    /// <para>
    /// <b>Distinct from <see cref="RuleSettleOrder"/>, and the reason is sharper here than usual.</b>
    /// Both are *which of these do we act on*, so sharing a tag looks harmless — and would mean the
    /// Lots a Zone Rule sampled correlated with which Bin Rule won a contested draw on the same Tick.
    /// That is invisible in play and would read as a lucky District rather than a defect.
    /// </para>
    /// </remarks>
    ZoneRuleSample = 3,

    /// <summary>
    /// Which Household in the Unplaced Pool takes a newly built dwelling.
    /// </summary>
    /// <remarks>
    /// <b><c>02 §8</c> rule 5 applied where its own wording does not quite reach.</b> The rule is
    /// stated over Phase 3's contested intents; this is a Sweep Rule acting in phase 6 and nothing is
    /// contested, because the drain is blind (<c>adr/0054</c>) and any member would take the house.
    /// The rule's <em>reason</em> reaches it exactly, though: a Pool that does not fully drain — which
    /// is what a housing shortage <em>is</em> — would leave the same Households unhoused for the life
    /// of the city under any fixed order, and the player would see a permanent underclass with no
    /// cause behind it.
    /// <para>
    /// <b>Distinct from <see cref="ZoneRuleSample"/> even though one trigger uses both.</b> They
    /// answer <em>which Lot do we look at</em> and <em>who moves in</em>; sharing a tag would tie the
    /// two together, so the Households housed would correlate with the Lots sampled and a District
    /// would appear to be favoured by the same families every time.
    /// </para>
    /// </remarks>
    PoolDraw = 4,

    /// <summary>
    /// Which Occupants a lowered occupancy ceiling evicts (<c>adr/0068</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Its own tag rather than <see cref="PoolDraw"/>'s, and the correlation it avoids is a
    /// visible one.</b> Sharing would tie *who is evicted* to *who is rehoused*, so the families a
    /// patch turned out of one Building would be the same families the next placement drew — the city
    /// would appear to shuffle a fixed set of households between the same doors, and no readout would
    /// say why. That is the failure the per-use tag rule exists for, arriving somewhere the rule's
    /// usual example (two decisions about one entity) does not obviously reach.
    /// </para>
    /// <para>
    /// <b>Keyed on the Household rather than on the Building</b>, so the draw is a lottery number each
    /// Occupant holds rather than a position in a list. Evicting by list order costs nothing and
    /// removes the same families from every Building on every patch, which is <c>02 §8</c> rule 5.
    /// </para>
    /// </remarks>
    OverflowEviction = 5,
}
