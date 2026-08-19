# Frontage is derived on the Epoch and a Lot's width is the Segment's own Building count

**A Lot's frontage and its Access Point are `(derived AND rebuilt)` from the Road Graph, rebuilt on the Epoch, and never saved** — a Lot no more stores its frontage than an Arc stores its cost, because both are functions of the Segments. **The subdivider takes exactly one hash-bearing number, `lots_per_segment`, and that number is already in the corpus**: `CONTEXT.md` → Address's *"five Buildings share a Segment at the working figures"*, which the whole ~30,000-Segment argument rests on. **Lot depth does not exist**, because a Lot has no extent in the schema and inventing one to park a number in would be modelling for a consumer that does not exist.

Guiding concepts: `EMERGENCE`, `LEGIBLE CAUSE`, `SOLVE THE ACTUAL PROBLEM`, `FAST ITERATION`.

The **disposition** half is arguable under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md) — no measurement distinguishes saving a derivable fact from deriving it, only which of them can go stale. The **number** half is governed by [`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md), and this decision's claim is that there was no number to choose.

## Why

### Frontage is derived because it is a function of Segments

`CONTEXT.md` → Frontage is explicit that this is arithmetic and not policy: *"**frontage is arithmetic, not a rule.** Block geometry decides how much land is in a parcel and whether it touches a street at all."* A fact computed from other saved facts, stored beside them, is the shape [`0064`](0064-a-bins-capacity-is-a-property-of-the-ruleset-in-force-and-an-over-full-bin-drains-rather-than-clamps.md) and [`0063`](0063-a-wait-list-wakes-on-the-bins-state-and-a-shortfall-is-derived-rather-than-stored.md) each deleted for the same reason four months apart — *a Ruleset number copied onto a row at creation and never re-derived, so the edit reaches everywhere except where it matters*. Frontage copied onto a Lot at subdivision is that shape with the Road Graph in the Ruleset's place, and the edit that would not reach it is **the player bulldozing the Street**, which is the one edit this slice exists to make possible.

**The rebuild trigger is the Epoch and nothing else**, which is what makes [`0012`](0012-routing-intent-lives-in-the-agent.md)'s contract load-bearing outside routing for the first time. Until now the Epoch had one prospective consumer, the route cache, and no producer in a running world.

**`LotTable.BuildingSlot` is the precedent and it is exact**: derived, outside the State Hash, rebuilt by `World.RebuildDerived`, and read by the Zone Rule's create predicate every Tick. Reading a derived column inside a Rule is already established practice and is sound because the rebuild is deterministic — the hash folds the saved state the rebuild is a function of, so two identical cities derive identically or the analysers have failed.

### The Access Point is derived for the same reason, and its shape is not this slice's to choose

[`plans/0022`](../../plans/0022-the-lot-subdivider-and-build-road.md) decision 3 recommends that *"this slice produces frontage and the Access Point's **location**; session F decides its **shape**"*. **F ran on 2026-08-11, after that brief was written, and decided it.** [`0074`](0074-side-of-street-is-a-property-of-the-access-point-not-of-the-graph.md) and `CONTEXT.md` → Address settle the shape completely: an Access Point **is** an Address, `(Segment, offset, side)`, every Building has a pedestrian one and a vehicle one, and today they hold the same Address. So decision 3 is discharged by a session rather than by this slice, and what is left is a disposition — and the disposition follows frontage, because an Access Point *is* the frontage expressed as a location.

*A brief's open question can be closed between its writing and its execution, and the brief will not say so.* This one names its closer in its own status section — *"it does not contend with session F"* — which is exactly the sentence that stops reading like a dependency once F has run.

### The number was already in the corpus

[`plans/0022`](../../plans/0022-the-lot-subdivider-and-build-road.md) decision 5 predicts *"two or more hash-bearing, world-creation numbers"* for `02 §2.2`'s depth and width targets, each owing a named ratifier under [`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md), and instructs: *"**look for the derivation before reaching for a value** — that is tau's precedent and `adr/0059`'s, and the corpus has twice found there was no number to choose."* It is a third time.

`CONTEXT.md` → Address argues the Segment count from a Building count:

> *"five Buildings share a Segment at the working figures, so promoting Addresses to Nodes would split every Segment five ways and put the Road Graph at 150,000–300,000 Segments instead of ~30,000."*

**That figure is not decorative — it is the premise of the decision that keeps an Address off a Node**, which is what holds the graph at its working size and therefore what every routing cost in [`plans/0013`](../../plans/0013-tick-budget.md) is priced against. It is a *Buildings per Segment* number that the corpus has been leaning on for as long as it has had a Road Graph to lean on.

**And the graph 5a built reproduces its other half by construction.** A 4096² map at `block_tiles = 32` gives a 129×129 node grid and `2 × 129 × 128 = 33,024` Street Segments before severance — against *"~30,000"*. Nobody arranged that: `block_tiles = 32` was chosen in 5a as one Street per Cell boundary, to reproduce S2 R0's road density, and the Segment count fell out. So the derivation has both ends already fixed, and the Lot count follows:

| | Figure | Where it comes from |
|---|---|---|
| Street Segments | **33,024** | 5a's graph at the shipped `[roads]` |
| Buildings per Segment | **5** | `CONTEXT.md` → Address, the premise of the Node refusal |
| Lots at full zoning | **165,120** | the product |

> ⚠ **AMENDED 2026-08-19: the cross-check below is circular as of 2026-08-13, and the Segment count is
> stale.** `SyntheticCity.PavedTiles` now sizes the paved lattice from `world.Lots.Rows.Capacity` **and**
> `LotsPerSegment` together ([`plans/0003`](../../plans/0003-build-plan.md) queue item 6), so the Lot
> count and `World`'s row sizing are **one figure read twice** rather than two figures that never met,
> and the agreement is guaranteed instead of earned. The Segment figure quoted below is the
> fully-paved-map count; [`0089`](0089-the-map-is-sized-by-how-many-commutes-fit-across-it.md) took the
> map to 16384² and the generator stopped paving it, so no shipped world produces it. ***A corroboration
> between two quantities survives only as long as nothing wires them together***, and nothing in this
> corpus re-checks one when a commit made for an unrelated reason closes the loop. What the paragraph
> below claims was true when it was written and is false now.

**The cross-check is the part worth trusting, because neither side was derived from the other.** `World` sizes its Lot table at **225 Lots per 1,000 Citizens** — S4 task 2's ratio, chosen from row-count sizing years of documents before there was a graph — which is 225,000 at the 1M target. The subdivider's 165,120 is 73% of it, from a completely independent route. Two figures that never met agreeing within a quarter is the first spatial corroboration this project has had, and it is why `lots_per_segment` is a *derived* number rather than a plausible one.

**It also survives its own arithmetic.** A Segment shared by two blocks carries Lots on both sides, and five alternating positions give each adjacent block two or three of them. That is odd-and-even house numbering — which is not a coincidence dressed up, because it is the *reason the corpus chose the word Address*: `CONTEXT.md` → Address says the word is used *"because a street address is literally this triple: a distance along a street plus an odd or even side."* The subdivider walking a Segment and alternating sides is that sentence executed.

### Depth does not exist, and refusing to invent it is the point

`02 §2.2` asks for *"Lot depth and width targets vary by zone density; the subdivider fits what it can"*, and gives no values. **Width is answered above. Depth has nothing to be a property of**: `LotTable` holds a position and a permission set and no extent, so a depth number would change no observable in this slice — it would decide how much land a Lot *notionally* covers, and nothing reads land area.

The consumer that would read it is [`0025`](0025-density-is-a-cap-and-it-trades-land-for-materials.md)'s density bands, which [`plans/0022`](../../plans/0022-the-lot-subdivider-and-build-road.md) lists as **Out** of this slice: *"this slice supplies the frontage arithmetic its trade rests on and no bands."* Choosing a depth now would be [`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) inverted — reasoning *toward* an unbuilt mechanism instead of from one — and would put an unratifiable hash-bearing number in the Ruleset on the strength of a consumer nobody has designed. `adr/0059` deleted a number for less.

**The dead block interior survives depth's deletion, and is better for it.** With Lots hung on Segments, everything that is not on a block face is unlotted **structurally** — not because a depth parameter ran out. A large block has a proportionally larger dead interior with no number governing it, which is `02 §2.2`'s *"this is how bad street layouts punish the player **mechanically** rather than through a penalty number"* taken at its word: the mechanism has no number in it at all.

## Consequences

**The Ruleset gains one number, `[lots] lots_per_segment`, and it is hash-bearing, world-creation and derived.** It is entered in [`plans/0002`](../../plans/0002-open-questions.md) §D with `CONTEXT.md` → Address as its **source** rather than as its ratifier — a derived number's ratifier is whatever refutes its derivation, and here that is the Segment-count argument itself. If *five Buildings share a Segment* is ever measured wrong, this number is wrong with it, and the same sentence fixes both.

**Frontage, the Access Point and the Lot's Address are `Rows.Derived`**, rebuilt by `World.RebuildDerived` and on every Epoch bump. They are outside the State Hash and outside the save, which means a save written before this slice and one written after differ in no Lot column that survives a reload.

**`--zones` can show a dead block interior without a parameter to explain it**, which is task 6's picture and `adr/0025`'s rejected road-derived cap shown working the way that ADR said it would.

**Lot depth is a named absence with an owner.** `02 §2.2`'s depth-and-width sentence is annotated rather than amended: width is derived, depth waits on density bands, and the note names [`0025`](0025-density-is-a-cap-and-it-trades-land-for-materials.md) so the next author reads a refusal rather than an oversight.

**A Building's occupancy and a Segment's Lot count are now two independent capacity figures**, and nothing checks that they are consistent with the population. That is not this slice's to fix — it is the same shape as the standing *synthetic fixture and `World`'s sizing disagree* item on [`plans/0000`](../../plans/0000-board.md) — but it is now one row larger, and the 165,120-against-225,000 gap above is where somebody should start.

## What would trigger revisiting

⚠ **AMENDED 2026-08-19: the ratifier named in this section is structurally defeated, not merely unrun.**
`LotSubdivider.Face` creates exactly `LotsPerSegment` Lots on a Segment and `LotTable.BuildingSlot` holds
one Building per Lot, so **Buildings per Segment cannot exceed the number this ADR is ratifying** and no
run can report a distribution the constant did not censor. ***A quantity a mechanism bounds cannot be
evidence about that bound.*** The ratifier is therefore not a milestone but a **world** — one whose
Buildings are placed under density bands, which is the same event this section already names as the
reopen trigger. Recorded rather than quietly re-pointed, because
[`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)'s 2026-08-15
amendment — *a ratifier names a machine, a world **and** a quantity* — is what makes the defect visible,
and it postdates this decision.

**`CONTEXT.md` → Address's five moving.** It is a *working figure* and has never been measured against a real city — no Ruleset that models one exists. The first long run that produces a real Building-per-Segment distribution can refute it, and this number follows it without argument. **That is the honest ratifier and it is milestone 5b's**, because a Trip is what makes Buildings-per-Segment observable rather than assumed.

**Density bands arriving.** [`0025`](0025-density-is-a-cap-and-it-trades-land-for-materials.md)'s trade is *land for materials*, and the moment a band reads land area, depth acquires a consumer and stops being a number with nothing to be a property of. At that point `lots_per_segment` also stops being one number and becomes one per band, which is the *"vary by zone density"* half of `02 §2.2` that this decision leaves unbuilt on purpose.

**Frontage becoming expensive to rebuild.** The rebuild is wholesale, on the same reasoning `RoadGraph.RebuildDerived` gives: nothing has measured the cost of a player editing roads, because until this slice nothing could. If a profile shows the rebuild rather than the edit, the Epoch already carries the per-Segment granularity an incremental rebuild would need — the invalidation contract exists, and only the consumer would be new.

**A Street Segment that is not a block face.** The alternating-sides derivation assumes a Segment spans one block edge, which is what [`0077`](0077-a-road-edit-is-one-segment-and-the-player-lays-streets-only.md) makes the player's unit too. A generator or a command that produced Streets of varying length would make *Lots per Segment* the wrong unit and *Lots per Tile of frontage* the right one — which is a rescaling rather than a re-argument, but it is a hash-moving one.
