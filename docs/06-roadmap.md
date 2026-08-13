# 06 — Roadmap

> Vocabulary in [`CONTEXT.md`](../CONTEXT.md). Pillars in [`00-vision.md`](00-vision.md). The world model in [`02-simulation-model.md`](02-simulation-model.md), movement in [`03-agent-architecture.md`](03-agent-architecture.md).
>
> **What this document is authoritative over: the phase model, the four rules below, and the risk each milestone retires.** Nothing else. It sequences work; it does not describe the simulation. Every claim here about what the city *does* is a citation, never a restatement — the rule is [`adr/0042`](adr/0042-a-planning-document-cites-and-a-design-document-owns.md), and the reason is that this document had accumulated eleven false claims by copying eleven originals that later moved.
>
> **Phase 0 and Phase 1 order and contents are owned by [`plans/0003-build-plan.md`](../plans/0003-build-plan.md).** Live status is owned by [`plans/0000-board.md`](../plans/0000-board.md). Phase 2's ordering is **provisional** and awaits re-derivation. Phase 3 has no plan and cannot have one yet; see below.

---

## How this roadmap works

Four rules govern everything below. The first three are pacing decisions; the fourth is a review criterion. These four are this document's own, and [`0003`](../plans/0003-build-plan.md) inherits them unchanged.

**1. No dates. Pure dependency ordering.** The developer works somewhere between sustained evenings-and-weekends and casual exploratory, and that rate is not predictable a month out. A dated plan would be wrong immediately and would then be quietly ignored, which is worse than having no plan. What *is* stable is what depends on what, so that is all this document commits to.

**2. Every slice leaves the project in a working, runnable state.** There will be gaps of weeks. The project must be re-enterable cold — check out, build, run, see something happen. A milestone that ends with the build broken, or with a half-migrated data format, has failed regardless of how much of it is written. In practice this means each slice ends with a test that passes and a thing you can watch. What *runnable* means concretely is the definition-of-done list in [`CLAUDE.md`](../CLAUDE.md), refined per slice by [`0003 §Definition of done`](../plans/0003-build-plan.md).

**3. Slices are sized for one or two sittings wherever possible.** A milestone that cannot be finished in a session tends not to get finished — it gets 60% done, then sits for three weeks, and re-entry costs more than the remaining work. Where a milestone genuinely cannot be compressed to that size, [`0003`](../plans/0003-build-plan.md) splits it into numbered slices, each independently completable and each leaving the build green.

**4. Every milestone names the specific risk it retires.** This is a visible field, not prose, and it is a filter: a milestone that cannot name a risk is either not necessary yet or is not understood well enough to start. "Risk retired" means *after this, we know something we did not know, or a class of failure is now structurally impossible.* Its corollary is the stopping rule: **once the risk is retired, the milestone is done, whatever else remains undone in it.**

### What a milestone row is

**A name and a risk.** Nothing else. There is no contents column, because a contents column is a description of the simulation and this document does not own one — the mechanisms live in [`CONTEXT.md`](../CONTEXT.md), `00`–`05`, and the ADRs, and a copy of them here is a copy that goes stale without anybody noticing. That is not a hypothetical: `rate` survived here for months after [`adr/0033`](adr/0033-two-rule-families-scheduled-and-swept.md) replaced polling with subscription, and *"travel time `distance / speed`"* survived alongside a tolerance test against a volume-delay function no milestone built.

---

## Phase 0 — the spikes

Throwaway code answering questions with numeric answers. Results are recorded in [`spike-results.md`](spike-results.md) and the harness is then deleted.

**The framing this section used to carry — *"three spikes, before committing to Godot"* — is spent.** [`adr/0001`](adr/0001-godot-and-csharp.md) was grilled and confirmed, and [`adr/0036`](adr/0036-the-cores-language-is-a-separate-decision-and-it-is-csharp.md) took the core's language out of it, so there is no longer a commitment for S1 and S3 to gate. There are five spikes, not three, and two of them never had anything to do with Godot. Order run and order remaining are both [`0003`](../plans/0003-build-plan.md)'s.

| # | Spike | Risk retired |
|---|---|---|
| **S4** | Kernel benchmark | Three claims the corpus rests on with no arithmetic behind them: `adr/0036`'s GC-pause assertion, `adr/0003`'s *"`checked` is cheap"*, and ledger #29's block-copy cost. Settled **before the first line of core exists** |
| **S2** | Routing | The single most consequential open technical question in the design — and the one that collapsed Cities: Skylines 2. Planned in [`0010`](../plans/0010-s2-routing.md) |
| **S0** | Synthetic 1M-Citizen city | That the 1M target is a hope rather than a spec, and every system sized against it is built on an unvalidated assumption |
| **S1** | Chunked `MultiMeshInstance3D` at city scale | That the per-Chunk MultiMesh split does not deliver acceptable draw counts and culling at city scale |
| **S3** | One data panel with a live multi-series graph | That Godot's `Control` UI cannot carry the Evidence drill-down, which is the load-bearing reason the engine was chosen at all |

**S1 and S3 now feed Phase 3's design rather than gating an engine decision** — see *Phase 3* below. Their specifications in this document were stale by roughly an order of magnitude and have been struck rather than restated; `0003` and [`spike-results.md`](spike-results.md) carry the live figures.

---

## Phase 1 — the foundation

No graphics. Everything is `Borough.Core`, `Borough.Tests`, `Borough.Headless`, `Borough.Formats` and `Borough.Analysers`, and everything is verifiable from a terminal. The boundary in [`adr/0002`](adr/0002-simulation-is-an-engine-agnostic-library.md) is enforced from the first commit by the CI reference check, not retrofitted once it has already been violated.

| # | Milestone | Risk retired |
|---|---|---|
| **1** | Tick and determinism harness | Determinism, which is close to impossible to retrofit. Every later debugging tool is downstream of this one |
| **2** | Typed tables and handles | That an ECS or a naïve object graph gets baked in, that save/load and deterministic iteration order become hard later, and that a full-world double buffer gets baked in and quietly cancels the Event Wheel |
| **3a** | Rule engine — Bins and Rules | That the economy is not conserved and failures are silently partial |
| **3b** | Rule engine — hot reload path | **The Citybound failure.** A simulation that is slow to tune is a simulation nobody tunes, and it then becomes unbalanceable |
| **3c** | Map Layers and Zone Rules | That growth cost scales with Zone size rather than staying constant |
| **4** | Event Wheel | That cost scales with *number of Citizens* rather than *number of Citizens with something happening now* |

~~**One assertion in this section is inherited and has never been argued.** *3b must not slip behind 3c.*~~ **RETIRED by the `adr/0015` session.** It was one unargued claim counted twice — this document gave no reasoning, and [`adr/0015`](adr/0015-all-tuning-data-is-hot-reloadable.md) grounded itself by citing this document straight back. **Slice 6 then falsified it in practice and cost nothing doing so.** Slice 6 is 3c's Layers half, it shipped first, and it introduced real Ruleset content — the diffusion cadence and Layer rates, which [`adr/0044`](adr/0044-the-map-layer-diffusion-cadence-is-the-designers-number-not-the-profilers.md) classifies as hot-reloadable data — with no Ruleset file in existence. It was free because `LayerRuleset` arrives as a **constructor argument rather than a `const`**, which is `adr/0015`'s own no-`const` rule doing the work the ordering was a proxy for.

**What replaces it is checkable where the ordering never was:** `LayerRuleset` is the standing test case, so **slice 8 is not done until the Layer cadence and rates load from a file.** The general form: what protects against the Citybound failure is the no-`const` rule, not the milestone sequence.

---

## Phase 2 — the simulation proper

**This ordering is provisional and awaits re-derivation.** It was written before conserved Money, Hinterlands, Office, the labour system, transit and every Service existed, and the board formally blocks *planning Phase 2 at all* until [`S0`](../plans/0003-build-plan.md) has run and this section is re-derived. The milestone names and risks below are recorded so the risks are not lost; the sequence between them is not load-bearing.

| # | Milestone | Risk retired |
|---|---|---|
| **5a** | Road Graph and Streets | That geometry leaks into the simulation and the routing graph stops being uniform |
| **5a-bis** | The Lot subdivider and the road editor | That a design document's precondition is a hypothesis about the build. *Every Building is on the Road Graph by construction* was true because there **was** no Road Graph; frontage is enforced now, so [`adr/0014`](adr/0014-grid-streets-with-freeform-arterials.md)'s asymmetry and `CONTEXT.md` → Frontage's deletion of the utility network are standing on something. Secondarily [`adr/0012`](adr/0012-routing-intent-lives-in-the-agent.md)'s: an invalidation contract no player has ever driven is a contract nobody has tested |
| **5b** ✅ | Trips, Legs and the pedestrian layer — **DONE 2026-08-12** ([`plans/0021`](../plans/0021-trips-legs-and-the-pedestrian-layer.md)), tasks 1, 2, 3, 5 and 7 | **The irreversible one.** A single-Leg Trip model propagates into Lot valuation, cost functions and every balance constant, and is what Citybound could never undo. **⚠ RETIRED 2026-08-12 by [`adr/0075`](adr/0075-a-leg-is-a-plan-and-a-traveller-is-a-cursor.md) and tasks 1–3**, which built the three-way split with an intrusive Leg list and eager Leg creation — a single-Leg model is not expressible in that structure. What remained of the milestone was **measurement**, which is not what makes a milestone irreversible, and it moved to 5b-bis ([`adr/0080`](adr/0080-phase-4-does-not-wait-on-a-trip-generator-and-a-trip-is-entered-by-command.md)). **⚠ The milestone shipped owing one consequence it could not build**: [`adr/0041`](adr/0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md)'s Segment volume needs *a next Segment every Tick* and `adr/0075` gives a Leg a cost and no path, so **attribution waits on a path source — which is 5c**, not on vehicles. The columns and the conservation invariant exist; nothing increments them |
| **5b-bis** ✅ | Jobs, the commute, and the first Trip generator — **DONE 2026-08-13** ([`plans/0023`](../plans/0023-jobs-and-the-commute.md)), all eight tasks | **That every number milestone 5b was to produce is taken against a fabricated origin-destination draw and lands in [`0013`](../plans/0013-tick-budget.md) and [`0002`](../plans/0002-open-questions.md) as measured fact.** S2 R4 already ran that experiment: a uniform draw put a District-granular route's detour at 18.52% where a local-trip draw puts it at **128.82%**, *"which under `05 §4` is a different city"*. 5b's tasks 4, 6 and 8 each measure a **distribution** — peak pedestrian density, the Commute Budget percentile, the walk-search multiplicand — so each is void without a generator, and **this document had scheduled none**: every generator the corpus names sits in *Mechanisms with no milestone* below. [`adr/0080`](adr/0080-phase-4-does-not-wait-on-a-trip-generator-and-a-trip-is-entered-by-command.md), [`adr/0081`](adr/0081-the-commute-is-the-first-trip-generator-and-a-job-is-taken-by-satisficing-on-distance.md) |
| **5c** | Statistical resolution and the travel-time matrix | That routing intent leaks into the world ([`adr/0012`](adr/0012-routing-intent-lives-in-the-agent.md)) — the GlassBox failure. **⚠ It also inherits 5b's one unpaid consequence**: [`adr/0041`](adr/0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md) needs *a next Segment every Tick* and nothing supplies one, so **Segment volume is not attributed until this milestone's path source exists** ([`plans/0010`](../plans/0010-s2-routing.md) decision 11). Volume is what 7a's Stress reads, so a 5c that ships a matrix and no per-Segment next hop leaves 7a with nothing to threshold |
| **6** | Lane-as-entity traffic | That the vehicle becomes the entity, which costs a spatial index, cache locality, and roughly an order of magnitude ([`adr/0016`](adr/0016-the-lane-is-the-entity-not-the-car.md)) |
| **7a** | Stress-driven Fidelity with hysteresis | That fidelity depends on the camera, which makes observation change outcomes and destroys replay |
| **7b** | The rotating Audit | The known blind spot — junctions that fail at *low* volume through turning conflicts, which V/C structurally cannot see |
| **8** | Parking | That parking is abstracted into a District average, losing the specific diagnosis — and that occupancy leaks, which is an [`adr/0006`](adr/0006-no-collection-grows-with-elapsed-time.md)-class permanent capacity loss. **Two invariants, not one, and neither is per-Tick** ([`adr/0084`](adr/0084-parking-occupancy-is-two-checks-and-an-invariant-over-absent-state-cannot-be-written.md)): a write-site `O(1)` release check and an end-of-run conservation sum. It also owes the **shed radius**, whose ratifier is this milestone's own walk-Leg distribution as occupancy approaches 1 ([`adr/0083`](adr/0083-a-sheds-use-is-the-arrival-query-and-a-stale-shed-is-wrong-by-a-bounded-walk.md)) |
| **9a** | Households, the Unplaced Pool and Departure | That growth is driven by a global demand scalar. The Pool with per-Household reasons *is* the demand signal, and it is a diagnosis rather than a bar chart |
| **9b** | Life Stages and self-generation | That the city has no internal demographic engine, so every dwelling change comes from immigration and the housing market has no churn of its own |
| **10** | Save/load | ~~**Unsaved state**~~ — **the risk moved 2026-08-12, and the milestone got smaller** ([`adr/0086`](adr/0086-a-save-has-no-schema-of-its-own-and-the-field-declaration-is-the-format.md), [`adr/0087`](adr/0087-a-save-is-copied-at-save-cadence-not-read-from-a-past-that-no-longer-exists.md)). The per-field declaration plus `BOR0901` make *state omitted from the file* unrepresentable, so what the save→reload→compare-hash test now finds is **a derived column that does not rebuild to the value it had** — a class 5a-bis has already sighted once. The file needs no authored layout, the snapshot is a copy at the Phase 7 boundary, and the residual risk is the rebuild rather than the write |

**Milestone 5b is the one to protect if the phase runs long**, and it is the one whose value is least visible while it is being built. The payoff is **Severance**, argued in [`adr/0014`](adr/0014-grid-streets-with-freeform-arterials.md) and [`03 §3.7`](03-agent-architecture.md).

### What Phase 2 as written would actually produce

Not *"a city that is alive"* — that claim stood here for a long time and is false. What the ten milestones above build is a **transport and housing simulation**: Households form, look for housing, fail with recorded reasons, move in, commute on multi-Leg Trips, park, age through Life Stages, and generate congestion that promotes specific Segments into microscopic simulation, all of it inspectable only through headless summaries and the hash trace.

It has **no money in it, nobody employed, and no way for anyone to arrive.** That is not a criticism of the ordering; it is the measure of how much the design gained after the ordering was written, and it is the work the re-derivation has to place.

> **⚠ Those two paragraphs contradicted each other for as long as they have both existed, and the
> sitting on `0002` §A found it by walking the milestone list looking for a destination.** The first
> says Phase 2 produces Households that *"**commute** on multi-Leg Trips"*; the second says
> *"**nobody employed**"*. Both are this document's, two paragraphs apart, and **the second is the true
> one** — no milestone in the table above employed anybody, so the commute in the first sentence had
> nothing to commute to. It is now true for 5b-bis onwards and false before it, and the first paragraph
> is left as written with this note rather than silently corrected, because *the milestone list was
> checked for its ordering and read for its prose* is the whole reason it survived.

---

## Mechanisms with no milestone

**Every row below is settled by an ADR and appears in no milestone anywhere in this document.** They are listed rather than placed, because placing them is the re-derivation's job and a guess here would be exactly the kind of unsourced assertion [`adr/0042`](adr/0042-a-planning-document-cites-and-a-design-document-owns.md) forbids.

| Mechanism | Settled by | What this document owes |
|---|---|---|
| Conserved Money, treasury, balance of payments, borrowing | [`adr/0024`](adr/0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md), [`adr/0031`](adr/0031-one-resource-abstraction-and-depth-not-count.md) | a milestone |
| Hinterlands, arrival through an Outside Connection, rejected-arrival reasons | [`adr/0023`](adr/0023-immigration-arrives-through-the-gate.md), [`adr/0020`](adr/0020-one-live-world-and-settlements-are-derived.md) | a milestone. 9a has the Pool and Departure but nothing says where Households come from |
| Settlements — commute-shed components, merge and split | [`adr/0020`](adr/0020-one-live-world-and-settlements-are-derived.md) | a milestone |
| Office, wages, the labour market, Skill Tiers, schooling | [`adr/0026`](adr/0026-wages-are-posted-locally-and-never-cleared.md) | **HALF PLACED 2026-08-12.** ~~a milestone~~ — **5b-bis takes the *assignment* half and not the wage half** ([`adr/0081`](adr/0081-the-commute-is-the-first-trip-generator-and-a-job-is-taken-by-satisficing-on-distance.md)): a kind declares job slots and a Citizen satisfices onto one on distance, with no wage read. **Office, wages, Skill Tiers and schooling still owe a milestone.** The row is struck in half rather than deleted, because a row deleted on the strength of a part shipping is `plans/0012` *Cause 3* |
| Density as a cap; subdivide versus stack | [`adr/0025`](adr/0025-density-is-a-cap-and-it-trades-land-for-materials.md) | a milestone |
| Services delivered by Trips — Attended, Dispatched, Networked | [`adr/0032`](adr/0032-services-are-delivered-by-trips-not-by-coverage.md) | a milestone. Service *coverage* survives here only as an overlay, which is what `0032` demoted it to |
| Crime and Incidents; dispatch response Trips | [`adr/0030`](adr/0030-crime-is-an-incident-with-no-perpetrator.md) | a milestone |
| The nine-Resource abstraction; Utility families; Waste | [`adr/0031`](adr/0031-one-resource-abstraction-and-depth-not-count.md) | a milestone |
| Infrastructure pricing, Upkeep, design life, wear | [`adr/0035`](adr/0035-infrastructure-is-priced-by-what-it-consumes.md) | a milestone. Nothing in this document says a road costs anything |
| Policy as a Sweep Rule, and the Sweep Rule family entire | [`adr/0033`](adr/0033-two-rule-families-scheduled-and-swept.md) | a milestone. 3a/3b are Bin Rules only |
| Transit | [`adr/0029`](adr/0029-transit-is-in-and-right-of-way-is-the-only-axis.md) | a milestone |
| Taste, drawn per Household and persisting for life | [`adr/0027`](adr/0027-preference-is-drawn-per-household-and-persists-for-life.md) | a milestone |
| Terraforming, procedural generation guarantees, ~~the three version numbers in the save header~~ | [`adr/0021`](adr/0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md) | a milestone. **The version numbers are paid, 2026-08-12 by session J** — `05 §7` now names all three and says what each does on a mismatch ([`adr/0086`](adr/0086-a-save-has-no-schema-of-its-own-and-the-field-declaration-is-the-format.md)). Terraforming still owes a milestone |
| Sealing, composed Fertility, Woodland, replanting | [`adr/0022`](adr/0022-land-is-a-stock-the-city-spends.md), [`adr/0034`](adr/0034-fields-are-sorted-by-source-geometry.md) | a milestone |
| Water Bodies as Bins on the water graph | [`adr/0034`](adr/0034-fields-are-sorted-by-source-geometry.md) | a milestone |
| Point-of-use noise and near-road pollution **queries** | [`adr/0034`](adr/0034-fields-are-sorted-by-source-geometry.md) | a milestone. 3c correctly excludes them from the Layers and nothing then builds them |
| Segment volume attribution by the Traveller | [`adr/0041`](adr/0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md) | a milestone, or a named place inside 5c–7 |

### Instructions addressed to this document, unexecuted

These are debts with named creditors rather than a survey, so they are checkable.

- [`adr/0029`](adr/0029-transit-is-in-and-right-of-way-is-the-only-axis.md) — *"**Dwell time is roadmap work**, and it is dwell time specifically — stops themselves are free."*
- [`adr/0029`](adr/0029-transit-is-in-and-right-of-way-is-the-only-axis.md) — *"`06-roadmap.md` sequences none of this and must be re-derived."*
- [`adr/0032`](adr/0032-services-are-delivered-by-trips-not-by-coverage.md) — *"School Trips are roughly **+50% on the commute peak** at the 10k target"*, a sizing fact that lands inside 5b/5c and appears in neither.

---

## Phase 3 — making it visible

**Phase 3 has no plan, and it cannot have one yet, because the design it would sequence does not exist.**

This is not a deferral. Every other phase is backed by a design document — Phase 1 and Phase 2 by [`02`](02-simulation-model.md), [`03`](03-agent-architecture.md), [`04`](04-economy-and-goods.md) and [`05`](05-technical-architecture.md) — and there is no equivalent for presentation. The work so far has been the simulation's functionality, deliberately and correctly, and rendering has never been designed, never been argued, and has no document to argue.

The one thing known about it is a risk rather than a plan, and it is worth stating precisely because it is easy to assume away: **the sim/render boundary has never been grilled, and the interface that would serve it was re-argued to serve something else.** [`0002`](../plans/0002-open-questions.md)'s coverage map puts `05 §2` among the never-argued sections and warns that *"threading policy, save format, and the sim/render boundary are assumed by every decision made so far."* [`adr/0002`](adr/0002-simulation-is-an-engine-agnostic-library.md) was then rewritten around **hot and cold query flavours** on the finding that it had *"assumed a renderer because rendering is what an engine boundary is usually for"* when the actual consumer is an inspector. So the boundary Phase 3 would build on is currently shaped for drill-down, not for drawing, and nobody has checked what that costs.

**What is owed before Phase 3 can be planned:**

1. **S1 and S3 run**, and their numbers recorded. They are the empirical inputs — a rendering ceiling and a UI-cost figure — and this is their real job, now that the engine decision they were framed as gating has been taken elsewhere.
2. **A presentation design written.** It does not exist. It has to cover at minimum what is drawn and what is not, the query surface the shell actually needs, and what `05 §2`'s boundary costs under a renderer.
3. **That design grilled**, as every other design document has been.

Only then is there something to sequence. The board tracks this chain.

Two disciplines are already settled elsewhere and will constrain whatever that design says, so they are cited here rather than argued: **not every Citizen is rendered** ([`01 §7`](01-player-experience.md) — *"every visible agent is a promise you have to keep"*), and **aggressive distance LOD and frustum culling from day one** ([`05 §11`](05-technical-architecture.md), [`00-vision`](00-vision.md) — the Cities: Skylines 2 cautionary tale). **Evidence is built early, not late** is [`00-vision §Evidence`](00-vision.md)'s, and what Evidence must answer is [`02 §9`](02-simulation-model.md)'s.

---

## Where the project stops being an engineering exercise

**Phase 3 is the transition, and getting there matters more than getting any individual earlier step perfect.**

Everything before it is a simulation that only its author can see, verified by hashes and headless assertions. That is the right way to build it and the tests are load-bearing forever, but a headless simulation is not a game and cannot be judged as one. The moment the city is visible and drillable is the moment the design starts producing feedback — patterns nobody designed, failures nobody predicted, and the first honest answer to whether the causal chain in [`00-vision.md`](00-vision.md) is actually interesting to follow.

The practical consequence for sequencing: when a milestone starts consuming sessions on polish rather than on retiring its named risk, that is the signal to stop and move on. The risk field exists partly to make that judgement easy — rule 4's corollary is a stopping rule, and it is meant to be used.

This is the failure mode with the best documented precedent in the reference material, and it is not a lack of skill. [`adr/0018`](adr/0018-prefer-off-the-shelf-infrastructure.md) records what happened to Citybound in full: the thesis survived and the yak-shave did not. Nothing in Phase 1 or Phase 2 is more interesting than the moment the city is visible and drillable, and the ordering above exists to keep that moment from receding.

The smell tests in [`00-vision.md`](00-vision.md) are the honest measure of whether the roadmap worked. Three of the five are testable at the end of Phase 1; the last two need Phase 3.

---

## What this document no longer contains

Removed under [`adr/0042`](adr/0042-a-planning-document-cites-and-a-design-document-owns.md), with where each now lives. Listed so nobody looks for them here and concludes they were dropped.

| Was here | Lives in |
|---|---|
| Milestone contents — mechanisms, algorithms, data structures | [`CONTEXT.md`](../CONTEXT.md), `00`–`05`, and the ADRs |
| Phase 0 and Phase 1 ordering, gates and sittings | [`plans/0003-build-plan.md`](../plans/0003-build-plan.md) |
| Live status — what is done, next, blocked | [`plans/0000-board.md`](../plans/0000-board.md) |
| *Open decisions that block milestones* | [`plans/0000-board.md`](../plans/0000-board.md)'s argument track, which is the live list. This document's table had been struck down to nothing while eleven real gates existed elsewhere |
| The definition of *runnable*, and the invariant list | [`CLAUDE.md`](../CLAUDE.md) and [`02 §10`](02-simulation-model.md). The version here said invariants *"run in debug builds"*, which `02 §10` calls backwards and which the shipped code does not do |
| *What is deliberately not in this roadmap* | [`deferred.md`](deferred.md), which owns deferrals. The version here still listed transit, which [`adr/0029`](adr/0029-transit-is-in-and-right-of-way-is-the-only-axis.md) admitted |
| The Simulator Effect, and the answer to it | [`00-vision.md`](00-vision.md), with the full Hopkins quote |
| What Evidence must be able to answer | [`02 §9`](02-simulation-model.md) |
| Citybound's yak-shave, in detail | [`adr/0018`](adr/0018-prefer-off-the-shelf-infrastructure.md) |
| Severance, and why the pedestrian layer earns its cost | [`adr/0014`](adr/0014-grid-streets-with-freeform-arterials.md), [`03 §3.7`](03-agent-architecture.md) |
| Spike specifications and target figures | [`plans/0003-build-plan.md`](../plans/0003-build-plan.md), [`spike-results.md`](spike-results.md) |
