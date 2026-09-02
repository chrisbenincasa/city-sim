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
    /// ✅ <b>It is a draw over a real choice, and it became one without this remark noticing.</b> The
    /// sentence here read <em>"what it decides today is only WHEN a Business is asked whether it has
    /// given up, since nothing tenants one … it becomes a draw over a real choice the day a Business
    /// placement pass ships"</em>. That day was <c>adr/0147</c>, and ***the prediction was right about
    /// the mechanism and never came back to collect***, which is <c>adr/0093</c>'s trigger failure in
    /// its most forgivable form. <b>Keeping it separate is what that foresight bought</b>: no
    /// correlation had to be untangled when the pass landed, which is the whole argument above
    /// arriving as a fact.
    /// </para>
    /// </remarks>
    UnpremisedDraw = 22,

    /// <summary>
    /// Which of the five terrain types a Cell is, drawn once at world creation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>adr/0158</c>. Keyed on <see cref="Space.CellGrid.Index"/> at
    /// <see cref="Ticks.Zero"/> — a <em>what sort of ground is this</em> draw rather than a
    /// <em>what happens now</em> one, the shape <see cref="CarOwnership"/> and
    /// <see cref="OpeningBalance"/> already use. Terrain is generated once and never changes
    /// (<c>adr/0021</c>), so the Tick coordinate carries nothing and says so.
    /// </para>
    /// <para>
    /// ✅ <b>Woodland got its own tag and did not borrow this one</b> — <see cref="Woodland"/>,
    /// <c>adr/0159</c>, milestone 24 task 8a. Woodland sits <em>on</em> terrain, so a shared tag would
    /// make every wooded Cell the same terrain type as every other wooded Cell — the forest and the
    /// rock as one draw wearing two names, which is a correlation nothing in the city could refute.
    /// ***This paragraph read <em>will need</em> and <em>unbuilt</em> until the tag was added***, which
    /// is this file's own rule working: the tag arrives with the mechanism that draws it.
    /// </para>
    /// <para>
    /// ⚠ <b>This ordinal was 22 until the merge with <c>main</c> on 2026-08-24, and it moved because
    /// milestone 25 had independently taken 22 for <see cref="UnpremisedDraw"/>.</b> A tag is an
    /// argument to <c>Randomness.Draw</c>, so <b>an ordinal IS the decision's identity</b> and moving
    /// one changes every draw made under it — here, the terrain map of every world. ***The branch
    /// yielded rather than the trunk***, which is the rule <c>34d0386</c> applied to ADR numbers on
    /// this same branch. Both baselines were re-recorded in the merge.
    /// </para>
    /// </remarks>
    TerrainType = 23,

    /// <summary>
    /// How much of a Cell is wooded, drawn once at world creation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>adr/0159</c>, milestone 24 task 8a. Keyed on <see cref="Space.CellGrid.Index"/> at
    /// <see cref="Ticks.Zero"/>, the same <em>what sort of ground is this</em> shape
    /// <see cref="TerrainType"/> uses.
    /// </para>
    /// <para>
    /// <b>Its own tag rather than <see cref="TerrainType"/>'s, which this file predicted it would
    /// need.</b> Woodland sits <em>on</em> terrain, so a shared tag would make every wooded Cell the
    /// same terrain type as every other wooded Cell — the forest and the rock as one draw wearing two
    /// names, which is a correlation nothing in the city could refute. ⚠ <b>The two fields are drawn
    /// by the same machinery and calibrated oppositely, and that is deliberate</b>: terrain
    /// self-normalises against the range the key realised, because <em>all five types exist</em> must
    /// not be a property of the seed; Woodland does not, because <c>adr/0022</c> requires that
    /// <em>a heavily forested seed</em> is a thing that can happen.
    /// </para>
    /// </remarks>
    Woodland = 24,

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

    /// <summary>
    /// Which <em>Business</em> a lowered occupancy ceiling evicts from its premises.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="OverflowEviction"/>'s other half, and it is a separate tag because the two draws
    /// range over DIFFERENT ID SPACES</b> (<c>adr/0147</c>). <c>World.Loser</c> draws on an entity's
    /// monotonic id; Household ids and Business ids are independent sequences allocated by different
    /// tables, so <b>Household 5 and Business 5 both exist</b> and under one tag would draw the
    /// <em>identical value</em>. ⚠ ***Two tenants of one Building would be perfectly correlated in a
    /// decision about which of them loses their place*** — which is the invisible correlation the
    /// distinct-tag rule exists to prevent, arriving somewhere it never could before.
    /// </para>
    /// <para>
    /// 🔴 <b>This is the build's first draw over a MIXED population, and that is why the hazard is
    /// new.</b> Every prior draw ranged over one table, so one tag was one id space and the rule had
    /// nothing to bite on. <c>adr/0141</c> made a Building's occupancy list hold tenants of any kind;
    /// this is the first place that costs something.
    /// </para>
    /// </remarks>
    BusinessOverflowEviction = 27,

    /// <summary>Which pooled Business the placement pass tries to give premises this occasion.</summary>
    /// <remarks>
    /// <b>Its own tag rather than <see cref="UnpremisedDraw"/>'s, and the two run on the SAME tick over
    /// the SAME pool.</b> That tag draws who is asked <em>whether they have given up</em>; this one
    /// draws who is <em>tried</em>. ⚠ ***Sharing would make the Business most likely to be offered
    /// premises the same one most likely to be asked to leave***, which is a correlation between two
    /// decisions about one actor and is invisible in every output either produces.
    /// </remarks>
    PremisesDraw = 28,

    /// <summary>Which Lot a pooled Business looks at when it is tried.</summary>
    /// <remarks>
    /// <b><see cref="PlacementCandidate"/>'s other half</b>, separate for
    /// <see cref="BusinessOverflowEviction"/>'s reason exactly: the draw is keyed on the seeker's
    /// monotonic id, and Household ids and Business ids are independent sequences from different
    /// tables. Under one tag <b>Household 5 and Business 5 would look at the identical Lots in the
    /// identical order</b>, so a shop would trail a family around the city.
    /// </remarks>
    PremisesCandidate = 29,

    /// <summary>
    /// Which seller in a District a purchase resolves to (<c>adr/0139</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>02 §8</c> rule 5 arriving in the market.</b> A District's sellers are a list, every one
    /// of them charges the market row's price, and a purchase needs exactly one. Walking the list
    /// from the head costs nothing and gives ***the shop nearest the head every sale for the life of
    /// the city*** — which is the rule's own worked failure, *"the same Building would win every
    /// contested draw"*, with list position standing in for entity id. The draw picks a start offset
    /// and the walk is first-fit from there, so no seller holds a standing advantage and a shop that
    /// restocks is reachable on the next occasion rather than behind a queue that never moves.
    /// </para>
    /// <para>
    /// <b>Keyed on the buying Rule Instance rather than on the market row</b>, so it is a lottery
    /// number the buyer holds. Keyed on the row instead, every buyer in a District would be sent to
    /// the same seller on the same Tick and the dispersion the draw exists to create would be a
    /// rotation the whole city performed in step.
    /// </para>
    /// <para>
    /// ⚠ <b>Distinct from <see cref="PoolDraw"/> despite the name, and they are unrelated
    /// mechanisms.</b> That tag draws out of the <em>Unplaced</em> Pool — who takes a newly built
    /// dwelling — and this one draws a counterparty out of a <em>District</em> Pool. Sharing would
    /// tie *which family gets housed* to *which shop gets the sale*, a correlation nothing in either
    /// readout could show.
    /// </para>
    /// </remarks>
    SellerChoice = 30,

    /// <summary>
    /// Which Day of its pay period a Business's payday falls on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The stagger, and it is what keeps a city's payroll off one Tick.</b> A trade declares
    /// <c>pay_period_days</c> and every Business of it would otherwise pay on the same Days — so a
    /// weekly city would move its entire wage bill on Day 7, then nothing for six Days, and a
    /// profiler would read a spike that no Ruleset asked for. The offset is this draw taken against
    /// the period, and payday is the Day whose remainder matches.
    /// </para>
    /// <para>
    /// <b>Keyed on the Business's monotonic row id and on nothing else</b>, which is what makes it
    /// free: it is derived on demand rather than stored, it survives a save and a reload without a
    /// column, and it does not move when the Business's slot is recycled — because the id is not.
    /// ⚠ <b>Not keyed on the Tick</b>, deliberately. A Tick-keyed draw would give a Business a
    /// different payday every period, which is a lottery rather than a wage.
    /// </para>
    /// <para>
    /// ⚠ <b>Distinct from <see cref="FoundingDraw"/> and <see cref="PremisesDraw"/> though all three
    /// are keyed on a Business.</b> Sharing a tag would tie <i>when a shop pays</i> to <i>whether it
    /// was founded</i> — a correlation invisible in every readout either mechanism has.
    /// </para>
    /// </remarks>
    WagePayday = 31,

    /// <summary>
    /// How many Days a Household spends in the Life Stage it is entering.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>adr/0011</c>'s <c>W</c>, and it is the load-bearing half of the stage table.</b> A stage
    /// declares a floor <c>N</c> and a window <c>W</c>, and the countdown is a uniform draw over
    /// <c>[N, N+W)</c> Days. Without the window every Household created at Tick 0 transitions on the
    /// same Day for ever, and the founding generation's cohort echoes through the whole run — a
    /// demographic wave that reads as a mechanism and is an artefact of world creation.
    /// </para>
    /// <para>
    /// <b>Keyed on the Household's monotonic row id AND on the Tick</b>, which is the opposite of
    /// <see cref="WagePayday"/> and deliberately so. A payday is a property of a Business and must not
    /// move; a stage duration is drawn afresh on every transition, because a Household that drew a
    /// short Young stage has earned no claim on a short Family one. ⚠ <b>Tick-keyed means a Household
    /// re-entering a stage draws again</b>, which is what makes the four draws across a life
    /// independent rather than one number wearing four hats.
    /// </para>
    /// <para>
    /// ⚠ <b>Distinct from every placement and founding tag though all are keyed on a Household or a
    /// Business.</b> Sharing one would tie <i>how long a family lasts</i> to <i>where it was
    /// housed</i> — a correlation no readout in this build could see, which is the whole reason
    /// <c>02 §1.1</c> requires a tag per use.
    /// </para>
    /// </remarks>
    LifeStageDuration = 32,

    /// <summary>How many children a Household bears on leaving its bearing stage.</summary>
    /// <remarks>
    /// <para>
    /// <b><c>adr/0011</c>'s first <em>decision</em>, shipped as a draw.</b> That ADR conditions
    /// fertility on housing cost, dwelling size and job security through the discrete-choice
    /// machinery, and none of it is built — so what runs is a uniform draw over the authored band,
    /// and <c>adr/0070</c> says the absence is a mechanism nobody has built rather than a decision
    /// about what fertility is.
    /// </para>
    /// <para>
    /// ⚠ <b>Distinct from <see cref="LifeStageDuration"/> though both are drawn at the same
    /// transition, on the same Household, on the same Tick.</b> Sharing the tag would tie <i>how many
    /// children</i> to <i>how long the next stage lasts</i> — every large family a long one — and
    /// that is exactly the invisible correlation <c>02 §1.1</c> requires a tag per use to prevent.
    /// ***Two draws at one instant is the case the rule exists for.***
    /// </para>
    /// </remarks>
    LifeStageChildren = 33,

    /// <summary>The static age a Citizen carries from the moment it becomes an adult.</summary>
    /// <remarks>
    /// <b>Keyed on the Citizen's monotonic row id and NOT on the Tick</b>, which is
    /// <see cref="WagePayday"/>'s polarity rather than <see cref="LifeStageDuration"/>'s. An age is
    /// drawn once on formation and never advances (<c>adr/0011</c>), so a Tick-keyed draw would be a
    /// number that could move — and a re-draw on any path that touched it would be an ageing
    /// mechanism this design refuses by name.
    /// </remarks>
    CitizenAge = 34,

    /// <summary>Which Cell of the Hazard Region a Disaster is seeded on.</summary>
    /// <remarks>
    /// <para>
    /// <b>Keyed on the Tick and on entity zero, which is the only draw here that has no entity at
    /// all.</b> <c>01 §5.3</c> makes a Disaster <em>world-scheduled</em> — its timing and place are a
    /// function of seed and Tick with no reference to what is standing there — so there is no subject
    /// whose id could enter the coordinate. ⚠ <b>Passing a Building or a Lot id would be the
    /// city-scheduled version that ADR refuses</b>, and it would be invisible: the draw would still
    /// be deterministic and the overlay would still be drawn, and the only wrong thing would be that
    /// riverside land had become cheap-until-you-use-it.
    /// </para>
    /// <para>
    /// ⚠ <b>One tag for the place because there is no second decision.</b> The <em>when</em> is a
    /// modulus on the Ruleset's interval rather than a draw, and how far the flood reaches is derived
    /// from the seed Cell's own depth — so a flood asks the stream exactly one question.
    /// </para>
    /// </remarks>
    DisasterSeed = 35,

    /// <summary>
    /// Where a Household sits on the space-against-centrality axis — <b>drawn once and kept for
    /// life</b> (<c>adr/0027</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Drawn at <see cref="Quantities.Ticks.Zero"/>, which is <see cref="CarOwnership"/>'s reason
    /// exactly</b>: this asks <em>what sort of Household is this</em> and not <em>what happens
    /// now</em>. A tag keyed on the current Tick would re-answer the question every time it was
    /// asked, and <c>adr/0027</c>'s whole argument is that a Household which chooses differently
    /// each time is <b>noise wearing a name</b> rather than a character.
    /// </para>
    /// <para>
    /// 🔴 <b>THE PERSISTENCE IS FREE BECAUSE NOTHING IS STORED, AND THAT IS THE POINT OF DRAWING IT
    /// HERE.</b> The Household's <em>position</em> within its stage's range is a pure function of
    /// this stream and the Household's own monotonic id, so it cannot drift, cannot be missed by a
    /// save and cannot be reset by a Life Stage transition. The stage's base and width are looked up
    /// live, so a transition moves the range and leaves the position exactly where it was —
    /// <c>adr/0027</c>'s <em>"someone who always valued quiet still values quiet once they have
    /// children"</em>, delivered by the key rather than by a copy-forward nobody could forget.
    /// </para>
    /// <para>
    /// ⚠ <b>Its own tag rather than a share of <see cref="CarOwnership"/>'s</b>, which is this
    /// enum's standing rule and matters more here than usual. Both are standing properties of a
    /// Household drawn at the same instant on the same id, so a shared tag would make <b>every
    /// centrality-loving Household a car owner</b> — a correlation with no cause in the city,
    /// arriving as a demographic pattern somebody would try to explain.
    /// </para>
    /// </remarks>
    CentralityTaste = 36,

    /// <summary>
    /// <b>Where a Building stands on its parcel</b> — the four setbacks, drawn once per patch of
    /// ground.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>DRAWN ON THE GROUND AND NOT ON THE LOT'S ID</b>, which is the one thing about this tag
    /// worth reading. A footprint is a property of the <em>patch</em>: a Lot demolished and re-laid on
    /// the same parcel puts its Building back where the last one stood, and a recycled slot cannot
    /// move a house that nobody touched. ***The entity id is the parcel's own south-west corner***,
    /// which is stable across a save, a re-carve that lands on the same rectangle, and a slot the
    /// allocator hands out again.
    /// </para>
    /// <para>
    /// <b>Tick zero, like every standing property.</b> The setbacks are a fact about how the house was
    /// built and not about the moment it was asked about.
    /// </para>
    /// </remarks>
    BuildingFootprint = 37,
}
