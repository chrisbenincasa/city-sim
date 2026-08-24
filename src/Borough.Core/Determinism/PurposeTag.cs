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

    /// <summary>
    /// Which dwellings a Household in the Unplaced Pool looks at this occasion (<c>adr/0069</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Distinct from <see cref="PoolDraw"/> even though one pass uses both</b>, and the pair is the
    /// same shape as <see cref="ZoneRuleSample"/> beside <see cref="PoolDraw"/> already is. They
    /// answer <em>whom do we look at</em> and <em>what do they look at</em>; sharing a tag would
    /// correlate the two, so the families considered earliest would be the ones shown the same
    /// dwellings, and a District would appear to be offered to the same people every pass.
    /// </para>
    /// <para>
    /// <b>Keyed on the Household rather than on the pass</b>, so that who a family looks at does not
    /// change because somebody ahead of them in the draw was housed first. A tag keyed on position
    /// would make one placement re-roll every candidate list behind it.
    /// </para>
    /// </remarks>
    PlacementCandidate = 6,

    /// <summary>
    /// Where a freeform Arterial enters the map — which edge, and how far along it.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="RoadArterialHeading"/> because an Arterial that entered at a corner
    /// would otherwise correlate with the direction it set off in, and the pairing that produces is
    /// the degenerate one: a road entering at a corner pointing back off the map.
    /// </remarks>
    RoadArterialOrigin = 7,

    /// <summary>
    /// Which way a freeform Arterial is pointing when it enters — its inward and cross components.
    /// </summary>
    RoadArterialHeading = 8,

    /// <summary>
    /// A freeform Arterial's gentle bend, redrawn every so many steps along its polyline.
    /// </summary>
    /// <remarks>
    /// <b>This is the tag S2's generator got wrong, and the failure is the argument for the whole
    /// enum.</b> The first capture drew one hash and sliced it for two coordinates by pre-shifting;
    /// the reduction consumed the top bits, so the shifted half read zeroes and returned the same
    /// value every time. Every Arterial took the same heading, left the map within one step, and
    /// <b>the footprint table looked entirely healthy while it happened</b> — a graph with no
    /// Arterials in it is still a graph. Distinct counters under one tag are the idiom; a second tag
    /// is not what fixes it, but a shared tag is what would have hidden it.
    /// </remarks>
    RoadArterialCurvature = 9,

    /// <summary>
    /// Whether a block gets a foot-only cut-through. <c>CONTEXT.md</c> → Segment's <i>few, and they
    /// are the edges Severance turns on</i>.
    /// </summary>
    RoadFootPath = 10,

    /// <summary>
    /// Which workers a lowered <c>jobs</c> ceiling dismisses (milestone 5b-bis task 2).
    /// </summary>
    /// <remarks>
    /// <b>Its own tag rather than <see cref="OverflowEviction"/>'s, and the correlation it avoids is
    /// the one that tag's own note describes arriving a second time.</b> A Ruleset patch may lower
    /// occupancy and employment together; share the tag and the families turned out of their homes
    /// are the same people turned out of their jobs, in every Building, on every patch. Two
    /// misfortunes with one cause is a story the city never told, and no readout could say where it
    /// came from.
    /// </remarks>
    JobEviction = 11,

    /// <summary>
    /// Which Citizens the assignment pass looks at this trigger (<c>adr/0081</c>, 5b-bis task 4).
    /// </summary>
    /// <remarks>
    /// <b><see cref="PoolDraw"/>'s tag on the employment axis, and distinct from it for the reason
    /// that one is distinct from <see cref="ZoneRuleSample"/>.</b> Both sweeps run in Tick phase 6 on
    /// their own cadences, and sharing a tag would make <em>who looks for a job today</em> a function
    /// of <em>who looked for a home today</em> — two decisions correlated invisibly, which is exactly
    /// what a distinct tag per use exists to prevent.
    /// </remarks>
    JobSeeker = 12,

    /// <summary>
    /// Which places a Citizen looks at for work on one occasion (<c>adr/0017</c>, <c>adr/0081</c>).
    /// </summary>
    /// <remarks>
    /// <b><see cref="PlacementCandidate"/>'s pair, and the same argument holds one axis over</b>: the
    /// seeker draw picks who looks and this picks what they see, so a shared tag would tie a
    /// Citizen's candidate set to their position in the sweep. It draws a <em>Cell</em> in the box
    /// the Commute Budget reaches and then a Building standing in it, so one occasion consumes two
    /// draws per look and the look ordinal separates them.
    /// </remarks>
    JobCandidate = 13,

    // 14 was CommuteDeparture — which Tick of the Day a Citizen left for work on, drawn uniformly
    // inside a window. adr/0101 retired it: a departure is now the Workplace's Shift start less the
    // commute the Citizen expected when they took the job, so it is arithmetic over saved state and
    // is not drawn at all. The number is retired rather than reused, on the invariant ids' precedent
    // — a tag is what a reproduced draw is keyed on, so reusing 14 would make two unrelated
    // decisions in two versions of this project share a stream, and nothing would report it.

    /// <summary>
    /// Whether a Household keeps a car (<c>01 §8</c> ledger #3, 5c task 5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A <see cref="Ticks.Zero"/> draw, and the second one, for
    /// <see cref="CommuteDeparture"/>'s reason exactly</b> — it answers <i>what sort of Household is
    /// this</i> rather than <i>what happens now</i>. A car that re-rolled every Tick would be a
    /// household selling and repurchasing a vehicle continuously, which is neither the simple
    /// assumption <c>01 §8</c> names nor the endogenous mechanism it defers.
    /// </para>
    /// <para>
    /// ⚠ <b>What the draw buys is a <em>nested</em> set, and that is the property that makes the rate
    /// hot-reloadable.</b> Ownership is <c>draw % 100 &lt; rate</c>, so lowering the rate takes cars
    /// from the Households at the top of their own fixed ordering and leaves every other one holding
    /// exactly what it held. A saved column re-rolled on reload would churn the whole city for a
    /// one-point change, and a saved column <em>not</em> re-rolled would make a key in a
    /// hot-reloadable file silently world-creation-fixed.
    /// </para>
    /// <para>
    /// <b>Distinct from <see cref="CommuteDeparture"/> because both are properties of the same
    /// commuter.</b> Sharing would make the households that drive exactly the households that leave
    /// earliest — a rush hour composed of drivers followed by a second one composed of walkers, with
    /// no cause in the city and a visible signature in every peak measurement.
    /// </para>
    /// </remarks>
    CarOwnership = 15,

    /// <summary>
    /// Which in-world hour a Building's jobs start at, inside its kind's band (<c>adr/0101</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A <see cref="Ticks.Zero"/> draw, and the first one keyed on a <em>Building</em> rather than
    /// on a person or a Household.</b> Working hours are a property of the job, so a Citizen who
    /// changes employer changes their hours with nothing written on the Citizen — which is the whole
    /// reason this tag is here and not on the commuter.
    /// </para>
    /// <para>
    /// <b>Distinct from every tag drawn on a Citizen, because the two compose into a departure.</b>
    /// A Building's start hour and a Citizen's Shift length are multiplied together across the whole
    /// city, so a shared stream would correlate <em>when a workplace opens</em> with <em>how long its
    /// staff work</em> — which would make every early-opening Building also a short-shift one, a
    /// second peak with no cause in the city, and exactly the signature <see cref="CarOwnership"/>'s
    /// remark warns about one entity over.
    /// </para>
    /// </remarks>
    ShiftStart = 16,

    /// <summary>
    /// How long a Citizen works, inside <c>[jobs]</c>'s band (<c>adr/0101</c>).
    /// </summary>
    /// <remarks>
    /// <b>A <see cref="Ticks.Zero"/> draw on the Citizen, and it is what makes the evening peak
    /// broader than the morning one.</b> A Workplace's staff arrive together, because they share a
    /// start hour, and leave apart, because they do not share a Shift length — which is the asymmetry
    /// real cities show and which no single dial produces. Sharing <see cref="ShiftStart"/> is refused
    /// above; sharing <see cref="CarOwnership"/> would make everybody who drives also work the same
    /// hours, putting the drivers in one peak and the walkers in another.
    /// </remarks>
    ShiftLength = 17,

    /// <summary>
    /// How far ahead of their Shift a Citizen aims to arrive (<c>adr/0101</c>).
    /// </summary>
    /// <remarks>
    /// <b>Added after the first profile measurement, and the reason is worth keeping.</b> The
    /// decision's only continuous term was the commute itself, which is about four minutes in a
    /// 4,000-Citizen city against an hour of 85 Ticks — so the measured morning came out as a
    /// plateau of five near-equal bars rather than a peak. This is the term that spreads people
    /// behind a shared anchor. <b>Distinct from <see cref="ShiftLength"/> because both are drawn on
    /// the same Citizen</b>: sharing would make everybody who works long hours also the punctual
    /// ones, which is a correlation with no cause and a visible signature in both tails at once.
    /// </remarks>
    CommutePunctuality = 18,

    /// <summary>
    /// What a Household is founded with, inside its Ruleset's band (<c>plans/0033</c> task 5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A <see cref="Ticks.Zero"/> draw on the Household's own id, and the third of that shape</b>
    /// — it answers <i>what sort of Household is this</i> rather than <i>what happens now</i>.
    /// </para>
    /// <para>
    /// ⚠ <b>Unlike <see cref="CarOwnership"/> the draw is consumed once rather than re-derived, and
    /// the difference is the mechanism rather than the tag.</b> Ownership is a standing property, so
    /// it is recomputed on every read and a retuned rate moves a nested set. An endowment is
    /// <em>issued</em> — it enters a Bin, is spent, and cannot be recovered by redrawing — so this
    /// tag is read exactly once per Household, at creation. What that costs is that the band is not
    /// retroactive on reload, which is correct: money already issued is money in the world.
    /// </para>
    /// <para>
    /// <b>Distinct from <see cref="CarOwnership"/> because both are drawn on the same Household at
    /// the same instant.</b> Sharing would make the richest Households exactly the ones that own
    /// cars — a wealth-mobility correlation with no cause in the city, which is the one pattern a
    /// player would most readily believe was designed.
    /// </para>
    /// </remarks>
    OpeningBalance = 19,

    /// <summary>
    /// Where a Policy's sweep starts on one trigger (<c>02 §4.2</c>, <c>plans/0033</c> task 5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>02 §4.2</c>'s rotation, drawn rather than advanced.</b> That section asks for the scan
    /// start to move per trigger so that a payer running dry produces <em>a gradient across the
    /// population rather than a permanent boundary</em> between the always-paid and the never-paid. A
    /// fixed stride would meet the letter and add a second hash-bearing number that can resonate with
    /// the population size — a stride sharing a factor with the row count visits a subset for ever.
    /// A draw needs no number and cannot resonate.
    /// </para>
    /// <para>
    /// <b>Keyed on the Policy's index rather than on a Household</b>, which is the one place in this
    /// enum where the entity coordinate is not an entity. It has to be: two Policies triggering on
    /// one Tick and sharing a start would pay and tax the same Households first for ever, so who the
    /// treasury runs out on would correlate with who was taxed hardest — <c>02 §8</c> rule 5's bias,
    /// arriving between two Rules rather than inside one.
    /// </para>
    /// <para>
    /// <b>Distinct from <see cref="ZoneRuleSample"/> even though both are the Sweep family choosing
    /// where to look.</b> Sharing would tie the Lots a Zone Rule samples to the Households a Policy
    /// reaches, so a District would appear to be both developed and paid first, with no cause.
    /// </para>
    /// </remarks>
    PolicyScanStart = 20,

    /// <summary>
    /// What an arriving Household carries across the gate, inside its Hinterland's band
    /// (<c>plans/0035</c> task 5, <c>adr/0131</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="OpeningBalance"/>'s form and not its tag.</b> A <see cref="Ticks.Zero"/> draw on
    /// the Household's own id, consumed exactly once, because an endowment is <em>issued</em> — it
    /// enters a Bin, is spent, and cannot be recovered by redrawing. Retuning a Hinterland's band
    /// therefore moves the Households that arrive after the reload and leaves every standing one
    /// holding what it holds. ***Money already issued is money in the world***, and a hot reload that
    /// redistributed it would be a Ruleset editing history.
    /// </para>
    /// <para>
    /// 🔴 <b>Distinct from <see cref="OpeningBalance"/>, and sharing would have been the subtlest
    /// correlation in this enum.</b> Both are drawn on a Household's id at <see cref="Ticks.Zero"/>,
    /// and the two populations do not overlap — a Household is founded <em>or</em> it arrives — so a
    /// shared tag would produce no visible collision at all. What it would produce is a **rank
    /// correlation between the founding band and every Hinterland's band**: the same id draws the
    /// same fraction of whichever span it is given, so the family that would have been richest at the
    /// founding is the richest emigrant from every edge. ***A correlation between two populations
    /// that never meet is one nothing in the city can refute.***
    /// </para>
    /// <para>
    /// <b>Not per edge, and the id is what separates the edges.</b> Four Hinterlands drawing on one
    /// tag is not four correlated draws — each Household is drawn once, at one gate, against one
    /// band. A tag per edge would be a tag per <em>value of a field</em>, which this enum does not do
    /// and could not: the edges are Ruleset content and the tags are simulation source.
    /// </para>
    /// </remarks>
    EmigrantBalance = 21,

    /// <summary>
    /// Which Business in the unpremised pool is looked at this pass.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="PoolDraw"/>'s sibling, and distinct from it for that tag's own stated reason one
    /// collection across.</b> The two draws run on the same trigger in the same pass, so sharing a tag
    /// would tie them together — ***the Businesses looked at would correlate with the Households
    /// housed***, and a run in which many families were placed would be a run in which many shops
    /// happened to be examined. Two decisions, two tags.
    /// </para>
    /// <para>
    /// ⚠ <b>What it decides today is only WHEN a Business is asked whether it has given up</b>, since
    /// nothing tenants one (<c>adr/0142</c>, milestone 25 task 5). It becomes a draw over a real
    /// choice the day a Business placement pass ships, and it is separate now so that the day it does,
    /// no correlation has to be untangled.
    /// </para>
    /// </remarks>
    UnpremisedDraw = 22,

    /// <summary>
    /// Which housed Household is asked whether it founds a Business this pass.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Milestone 27 task 8, and <c>adr/0145</c>'s amendment is what it draws for</b> — <em>a
    /// Household founds on its own means, and never on the city's need.</em> The pass draws a bounded
    /// sample of housed Households and each drawn one founds if its balance covers the band, so this
    /// tag decides <b>who is asked</b> and the balance decides the answer.
    /// </para>
    /// <para>
    /// ⚠ <b>Distinct from <see cref="PoolDraw"/> and <see cref="UnpremisedDraw"/> for the reason those
    /// two are distinct from each other</b>, and it binds harder here: all three run on the same
    /// <c>[placement]</c> trigger in one pass. Sharing a tag would make ***the families who start
    /// shops correlate with the families who get housed***, which is a relationship no ADR argues for
    /// and which nothing downstream could untangle.
    /// </para>
    /// <para>
    /// <b>Drawn on the sample INDEX rather than on a Household id</b>, as <see cref="UnpremisedDraw"/>
    /// is: the pass is choosing positions in a population, not asking a known Household a question.
    /// The Household's own id enters where a per-Household decision is made, and there is none here —
    /// whether it founds is read off its balance, not drawn.
    /// </para>
    /// </remarks>
    FoundingDraw = 25,

    /// <summary>
    /// Which trade a founding Household opens.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Uniform over the declared <c>[[business]]</c> kinds, and uniform is a decision rather than a
    /// default.</b> <c>adr/0145</c> settles who founds and says nothing about what they found, because
    /// at task 8 a trade declares nothing but its name — there is no margin, no wage and no fill rate
    /// to prefer one over another. ⚠ <b>With no information distinguishing the trades, anything other
    /// than uniform would be a preference nobody argued for.</b> ***The day a trade carries numbers,
    /// this becomes a choice and wants an ADR.***
    /// </para>
    /// <para>
    /// <b>Its own tag rather than <see cref="FoundingDraw"/>'s</b>, for that tag's own reason: sharing
    /// would tie <em>which family founds</em> to <em>what they open</em>, so the bakeries and the
    /// barbers would be founded by systematically different Households.
    /// </para>
    /// </remarks>
    FoundingTrade = 26,
}
