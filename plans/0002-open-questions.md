# Open Questions — resume here

A consolidated ledger of everything still unresolved, ordered so it can be picked up cold. Each entry states the fork, what it blocks, and a recommended answer to argue against.

The per-document `Open questions` sections remain authoritative for their own areas; this file is the index and the ordering. When something here is settled, close it **in the owning document** and strike it here.

Run `/grill-with-docs` to continue. **Session ten opens on `02 §7` + `adr/0006`, then `02 §4` residue + `adr/0015` — the last four items of the Phase 1 gate.** Session nine was booked for those and opened on `06-roadmap.md` instead; it produced `adr/0042` and left them untouched.

**Milestone 1 is now buildable**, and so are the scaffolding, S4, the fixed-point library, typed quantities, milestone 2 and milestone 3c. See *Readiness*. **The most useful thing that can happen next is code, not another session.**

**That work is now decomposed.** [`0003-build-plan.md`](0003-build-plan.md) is the ordered slice ledger for Phase 0 and Phase 1, with a plan document per slice ([`0004`](0004-s4-kernel-benchmark.md) S4, [`0005`](0005-arithmetic-substrate.md) substrate, [`0006`](0006-analysers-and-lints.md) analysers, [`0007`](0007-typed-tables.md) tables, [`0008`](0008-tick-and-replay.md) tick and replay, [`0009`](0009-map-layers.md) Map Layers) and a gate board naming exactly what slices 7–10 are waiting on. **Slice 1 — S4 — has run**, and no tripwire row fired; only its final task, deleting the harness, is outstanding and held on purpose. The four items below stay owed; they gate the *back* of Phase 1, not the front.

---

## Where we got to

Settled across the two sessions so far, in the order it happened:

| Decision | Record |
|---|---|
| One clock, no calendar, no aging | `adr/0010`, `adr/0011` |
| `TICKS_PER_DAY = 8192`, 16 Ticks/s reference, pacing lives in the speed ladder | `adr/0019` |
| No hour or minute — a sun arc with named phases, Commute Budget as a wedge on it | `02-sim §1.2`, `01-player §7` |
| One live world; **Settlement** derived from commute range; region-of-tiles rejected | `adr/0020` |
| Bounded procedural rectangle, sparse Chunks, terrain at construction time only, terraforming priced by haul | `adr/0021` |
| Land is a stock the city spends; fertility composed not generated; Woodland the one generated resource | `adr/0022` |

Session three — the density and demographics thread:

| Decision | Record |
|---|---|
| Density is a **cap**, not a designation; capacity not quality; trades Land for Materials | `adr/0025` |
| Two routes to density — **subdivide** vs **stack** — separated by Access Points per capita | `adr/0025` |
| A Building holds many Occupants but **aggregates logistics, never decisions** | `adr/0025`, `CONTEXT` → Building |
| Zoning is **exclusion**, not placement; nothing permitted by default; agriculture is protection | `CONTEXT` → Zone |
| Immigration is **arrivals through the gate** as ordinary Trips — no demand scalar anywhere | `adr/0023` |
| The Outside is **four Hinterlands**, authored in domain units, drained as a stock | `adr/0023` |
| **Money is conserved**, sourced and sunk only at the gate; the city has a balance of payments | `adr/0024` |
| Policy is a lever set, never a prescription | `adr/0024`, `CONTEXT` → Policy |
| **Preference is drawn per Household** from a stage-constrained range, and persists for life | `adr/0027` |
| Failure is **spatial as well as citywide**; notifications fire on named trajectories, never thresholds | `01-player §6` |
| **Office** — one family, all tiers, no freight but business-travel Trips; it is what makes a downtown | `04-economy §1` |
| **Amenity** = walkable variety, which is what rewards mixed use | `CONTEXT` → Amenity |

Session four — pressure and difficulty (`01-player §5`):

| Decision | Record |
|---|---|
| Pressure is **two axes — the Bill and the Clock** — not three layers. Every §6 trajectory maps to one or both | `01-player §5.1`, `CONTEXT` → The Bill / The Clock |
| **Difficulty lives outside the map.** The dial changes the cost of *recovering* from a mistake, never the cost of the mistake | `01-player §5.5`, `CONTEXT` → Intensity Dial |
| The dial's whole surface is **the Hinterland plus one interval** — no new parameters, no branch written on intensity | `01-player §5.4` |
| The dial **never touches an instrument**. Checkable via the Input Log: intensity is an input, notification verbosity is not | `01-player §5.7` |
| A **Shock** is a movement in a Hinterland's authored figures, full stop. Closes ledger #19's "one home for the shock layer" | `01-player §5.2`, `CONTEXT` → Shock |
| A **Disaster** is world-scheduled; the **city sets severity**, because containment is an ordinary Trip that can fail | `01-player §5.2–5.3`, `CONTEXT` → Disaster |
| Catalogue: **Flood, Urban fire, Wildfire** ship. Utility failure blocked; outbreak, earthquake, wind deferred | `01-player §5.2` |
| A **Mode** is a preset plus a lock policy, fixed at world creation. The lock is opted into, never imposed | `01-player §5.6`, `CONTEXT` → Mode |
| Randomisation is **orthogonal to Mode** — the Mode sets a range, the seed picks the point. Same shape as `adr/0027` Taste | `01-player §5.6` |

Session four continued — `03-agent-architecture`:

| Decision | Record |
|---|---|
| **Invariant 6 was inverted.** It required Microscopic segments to agree with the VDF; §3.2's whole justification is that the VDF is wrong there. Rescoped to the **audit** | `03 §4`, `03 §3.5` |
| The **divergence metric** is travel time against the statistical prediction, on audited *unstressed* segments only. It also gives §3.6's blind spot a detector | `03 §3.5` |
| The Microscopic tier gets an **acceptance suite** derived from its own justification — queueing, spillback, hysteresis — with an admission rule and the audit as its discovery route | `03 §5.1` |
| `T_high`/`T_low` are **measured, not chosen** — sweep one Input Log and find where the boundary stops mattering | `03 §3.3` |
| **Walking is always Statistical**, permanently — pedestrian networks do not saturate, so `distance/speed` is exact. Reopened only by transit | `03 §3.7`, `CONTEXT` → Fidelity |
| **One graph with mode masks**, not two networks. It is what makes Severance emergent | `03 §3.7`, `CONTEXT` → Road Graph |
| **Microscopic Cap** (was *Fidelity Budget*) — a world constant, never host-tunable, or one Input Log yields two cities | `03 §3.9`, `CONTEXT` → Microscopic Cap |
| Reaching the Cap is **not a failure mode**. Removed from `01-player §6`; `HONEST DEGRADATION` is met by §7's existing overlay-honesty rule | `03 §3.9`, `01-player §6` |
| **VDF** finally defined — and *why* it is structurally wrong under saturation, which is the two-tier design's whole justification | `CONTEXT` → Volume-Delay Function |
| **Contiguity needs a second trigger.** Stress decides where microscopic simulation *begins*; **downstream blocking** decides where it *extends*, event-driven so it keeps pace with a jam | `03 §3.3` |
| **A segment with a non-empty queue never demotes.** The queue is what demotion would discard, deleting hysteresis at the moment it would be observed | `03 §4`, invariant 3 |

Session five — the services layer, and it went much further than services:

| Decision | Record |
|---|---|
| `Service` was **one verb over four unrelated mechanisms**. The axis that sorts them is **who makes the journey** | `adr/0032`, `CONTEXT` → Delivery Mode |
| Services are delivered by **Trips**; the coverage Map Layer is demoted to an **overlay** | `adr/0032` |
| **School is an Attended Trip**, which finally gives `adr/0010`'s **Sorting** a mechanism | `adr/0032`, `adr/0010` note |
| A drop-off is a **waypoint on the parent's commute**, never a planned itinerary. *A Household chooses providers and modes; it never chooses an itinerary* | `adr/0032` |
| Mode is a **Provider List attribute**; re-evaluation is an Event Wheel countdown **or a failed Trip** | `adr/0032`, `CONTEXT` → Household |
| **TRANSIT SHIPS.** The largest open question in the project, closed | `adr/0029` |
| **Right-of-way is transit's only axis** — shared / reserved / separated. No mode list, no unlock ladder | `adr/0029` |
| Player buys **vehicles**; frequency is emergent from round-trip time | `adr/0029` |
| **Crime is reopened** — thread E's deferral overturned deliberately | `adr/0030` |
| An Incident has a **victim, a place, and a response — and no perpetrator** | `adr/0030` |
| Crime reads **employment, never income** — and unemployment here is a reachability failure the player caused | `adr/0030` |
| Police suppress the **symptom, never the cause**, which is what keeps neglect uncontainable | `adr/0030` |
| Safety presence counts as an **`adr/0027` Taste axis**, so under-provision changes the *population*, not the score | `adr/0032` |
| **Good, Utility and Money are three families of one Resource**, split on transport alone | `adr/0031` |
| **The constraint is chain depth, not list length.** Five Goods → nine Resources, zero added depth | `adr/0031` |
| **Waste is a Good.** Its collection is a Shipment; it can be exported at a price | `adr/0031` |
| Nobody draws a utility network — utilities **ride the Road Graph and pool by District** | `CONTEXT` → Utility |
| **Capacity ≠ Storage.** Money is unbounded capacity; Power is zero storage | `adr/0031` |
| **Schooling is accumulated per completed Trip**, in three levels under three tiers. Primary is a **gate** | `CONTEXT` → Schooling |
| A school sets the tier of **a Household that does not exist yet**; the lag is made legible by showing the **cohort in flight** | `adr/0010` note |
| **In Education** is a state of a Young Household, not a stage. *Life Stage is composition* | `CONTEXT` → In Education |
| The tier wall stands, **re-justified as a category boundary** — learnable skill vs credential | `adr/0026` note |
| **Experience is continuous within a tier**, and it is the design's only **productivity growth** | `adr/0026` note |
| **The city has no central bank, correctly.** Three external anchors do the damping | `adr/0026` note |

Prior art for all of the above is now recorded in [`references.md` §9](../docs/references.md) — SC4's cap, SC2013's road-derived cap, CS's command model, and the Peanut Butter Point as the genre's most instructive failure.

Through-lines worth holding on to, because they decided several arguments each:

- **Ratios are real; units are invented.** The simulation holds only Ticks and Tiles. Anything expressed in seconds or metres is an exchange rate chosen outside the simulation and free to change. The one number that is *not* an exchange rate — `TICKS_PER_DAY` — is a balance constant, because it fixes the ratio of a commute to a life.
- **Scarcity is a gradient, never a wall.** Parking widens the shed (`adr/0009`); Timber lengthens the haul then becomes an import (`adr/0022`); terrain is a price rather than a refusal (`adr/0021`); a drained Hinterland gets expensive, never empty. Anything that fails hard should be suspected.
- **Author in domain units, never in utility units.** The Hinterland argument generalises: `rent §620` is a number a designer can defend and a player can read; `V = 4.7` is neither. Any constant that cannot be stated in something the player already sees is a balance hazard.
- **Prefer a crossover to a threshold.** Two curves meeting is a fact nobody chose; a threshold is always somebody's guess. Regime changes should be detected as crossovers wherever one exists.
- **An authored constant is acceptable when it is the same thing the player is shown.** A magnitude threshold in a config file fails; a named failure mode in a table the player reads passes. Corollary: **duration constants beat magnitude constants**, because durations are scale-free and mean the same thing in a village and a metropolis.
- **An exploit is usually a missing mechanism, not a missing rule.** Five were found and closed this session — density spam, Office monoculture, universal mixed use, public hiring as an unemployment sink, sparse streets for mega-Lots — and *every one* resolved into something the design already implied. Reaching for a rule first is the tell that the mechanism has not been found yet. Where a rule is genuinely needed, the test is: **would you have written it without knowing about the exploit?** If the only justification is "otherwise players do X," it is a patch, and players read patches as arbitrary because they are.
- **Growth changes source; it never stops.** Extraction (spend the Hinterland) gives way to cultivation (house families). There is no population ceiling, only a transition between engines.
- **Every specialisation starves an input only a different specialisation produces.** Five instances have now arrived from unrelated directions: all-high-density starves families and therefore internal generation; all-Office starves the tier-2 pipeline that staffs it; all-wealthy starves the tier-1 labour the skill ladder is built from; all-clean starves Materials and therefore construction; all-urban starves fertility and therefore Food. **This is why balance is the winning play rather than the virtuous one** — nobody wrote a rule, the chains are simply circular. It is also why Policies bite here where SC4's ordinances did not: theirs moved a demand scalar, ours act on the chain that produces the thing.
- **Goods are price-constrained; people are rate-constrained.** Any material shortage can be imported away forever at a cost. A demographic shortage cannot — Hinterlands recover at a rate no amount of money exceeds. **The labour pipeline is therefore the hardest constraint in the design**, harder than Food or Materials, and the one that ultimately forces the balance above.

Added session five:

- **Repeated exceptions on one axis mean the axis *is* the abstraction.** Four things had been excepted from *"it's a Good"* — Money, the utilities, Waste, service capacity — and every exception was on **transport** alone. `adr/0024` spent a whole ADR proving Money is Good-like-but-not-a-Good, and the proof reduced to one boolean. When a rule has accumulated four exceptions, stop writing the fifth.
- **The axis that separates systems is usually *who moves*, not what they provide.** Sorting the Services by delivery rather than by function is what made them tractable, and it immediately exposed that `adr/0026`'s demand-determined staffing rule was never universal.
- **Generalise the mechanism; keep the families named.** `CONTEXT` is domain language, not implementation. *"They all sit in Bins"* is an implementation fact, and flattening nine things into one player-facing list makes the model less legible, not more. `Zone` had already found this shape.
- **The honest model and the cheap one keep coinciding.** The walked school run costs nothing in CPU *and* is what the player should be designing toward. An Incident with no perpetrator avoids an unfounded claim *and* avoids an activity model for offenders. When they diverge, suspect the model before the budget.
- **A category is not a quantity.** *Scarcity is a gradient, never a wall* is the most-repeated rule here, and the tier 2 → 3 boundary is the one place it correctly does not apply — because credentials are not a rung, and no amount of experience makes a surgeon. A wall is defensible exactly when it is a category boundary and indefensible when it is a balance constant wearing one.
- **For a lagged consequence, show the pipeline, not the output.** Schooling's effect arrives a full Life Stage after its cause, which looked like a `LEGIBLE CAUSE` catastrophe. It isn't, because the cohort is in flight and its state is readable *now*. The counter-intuitive part: **accumulation is what makes the lag legible** — a conferred model would have nothing in flight to show.
- **Deferrals must be overturned deliberately, never routed around.** Crime was reopened by naming thread E's decision, arguing it, and recording the reversal. The failure mode is a design that quietly grows the thing it decided against, one defensible exception at a time — which is also why the five-Good *count* was replaced by a depth *rule*: a quota cannot say why Waste is fine and Steel is not.

---

## Coverage map — what has been grilled, and what has not

Three sessions in, coverage is **very uneven**, and the unevenness is not random: everything argued so far is the *simulation and economy* layer. The **agent architecture and the entire technical layer have never been through this process at all**, and neither has the roadmap that sequences them.

Legend: 🔴 never grilled · 🟡 partially · 🟢 substantially settled

| Document | State | Notes |
|---|---|---|
| `00-vision.md` | 🟡 | All four open questions now closed — **Q1 (transit) closed session five, `adr/0029`**, having been the largest single open question in the project. **The pillars themselves have never been challenged directly** — they are inherited from `plans/0001` and assumed by everything, and that is now the document's only remaining exposure. |
| `01-player-experience.md` | 🟡 | §2 verbs rewritten; §5 rebuilt session four (two axes, shocks/disasters, Modes) and ledger #5 closed; §6 failure grilled (three trajectories added, spatial expansion, notification rules, acceptance test made checkable); §7 information design settled session 2. **§1 (core loop), §3 (first ten minutes), and §4 (two hours, and twenty) have never been touched.** An earlier note here assumed the intensity dial scales how readily trajectories trigger — **struck**: §5.7 forbids the dial touching any instrument. |
| `02-simulation-model.md` | 🟡 | **§2.4 rebuilt session seven** — fields sorted by source geometry, two of them removed from the Layer table entirely, and a new **§2.5** giving the classification procedure for any future field. §1 time, §2 space, §5 growth/choice/prices, §5.9 decline all worked. §2.4's service-coverage row is now an **overlay** spec per `adr/0032`. **§4 (the Rule engine) grilled session six** — split into Bin Rules and Sweep Rules, scopes fixed at four, scheduling settled, `adr/0033`. Residual in §4: what a predicate may read, fallback chain depth, Ruleset versioning. **§3 (Resources), §7 (sleeping and the Event Wheel), §8 (determinism rules), §10 (testing strategy) never grilled** — and §7 is now partly spoken for by `adr/0033`, so it should be read against it rather than fresh. §3 is stale: `adr/0031` replaced the Goods taxonomy with Resource families. |
| `03-agent-architecture.md` | 🟡 | Session four: invariant 6 rescoped (it required the Microscopic tier to agree with the VDF, inverting the tier's own justification); the audit's **divergence metric** settled; §5.1 **acceptance suite** added; **walking** given a fidelity answer and one graph with mode masks; the **Microscopic Cap** made a world constant and removed as a failure mode. Session six closed §2.1's **sizing tension** — 1M is the spec, not headroom — but left the record itself unargued and its 40-byte figure stale. Still unargued: §2 (the Citizen model), and §5's traffic model itself — **the most detailed unargued design in the project**, and now under a Microscopic Cap that binds far harder at 1M. |
| `04-economy-and-goods.md` | 🟡 | **§5 gained capital expenditure and Upkeep session seven** — it had neither, and nothing in the corpus said a road costs money. Also a fourth second-order effect: public construction is a stimulus or a leak depending on the Materials chain. §1 goods list closed; §4 prices and §5 budget substantially reworked. **§3 (Movement), §6 (how a shortage becomes an unhappy person), §7 (Jobs) never grilled** — and §7 is now stale by construction, since `adr/0026` rewrote how jobs work without anyone reading it. |
| `05-technical-architecture.md` | 🟡 | **§5 closed session seven** — split into the Cell (design, frozen) and the Chunk (performance, still to be measured), and `§4`'s hash-preserving list corrected where it had wrongly claimed Chunk size. **§1, §2, §6, §7 (format half), §8, §10 remain unargued.** **Opened session six, partially.** It gained the thing it never had — **a stated budget** (10k = the first hour, 1M = the late-game floor, on a 4096² map), which had been silently supplied by an unratified 10k figure. §7's **Ruleset-versioning** half is closed. §4 gained the **State Hash rule** (*a change is an optimisation if the hash is unchanged, a design change otherwise*), §9 gained Rule scheduling and a Sweep Rule slot in the build order, §3 gained the two derived structures. **§1, §2, §5, §6, §7, §8, §10 remain unargued**, and the *unquestioned* parts are still the larger risk — threading policy, save format, and the sim/render boundary are assumed by every decision made so far. Questions 20–24 below are untouched. Session four's two direct dependants also remain: the Microscopic Cap's real value, and force-promotion being event-driven rather than cycle-driven. |
| `06-roadmap.md` | 🟡 | **Grilled session nine, and the finding was that almost none of it was its own.** A sweep found essentially every prose block restating a better original elsewhere, and **eleven false claims, each one a copy whose original had moved** — `rate` after `adr/0033`, `distance / speed` after the VDF, *"invariants run in debug builds"* after `02 §10`, *"transit: not in scope"* after `adr/0029`. Fixed by **deletion, not correction**, per the ADR it produced: **`adr/0042`** — a planning document cites, a design document owns. `06` lost its contents column; rows are now name plus risk. It gained **seventeen mechanisms with no milestone** and the ADR instructions addressed to it and never executed. **Phase 2's ordering (K2) is still owed and still last**; Phase 3 turned out to be blocked on a presentation design that does not exist, not on choice. |
| `deferred.md` | 🟢 | Maintained rather than grilled, which is correct — it is a record, not a design. |
| `references.md` | 🟢 | §9 added this session. The gap it had (genre prior art) is closed. |
| `adr/0001` | 🟡 | **Grilled session eight, and the finding was mis-scoping rather than under-argument.** Its host argument is sound and untouched; it had also settled the core's **language** by inheritance and its **runtime** not at all. Split — `0001` now decides the host only, and [`adr/0036`](../docs/adr/0036-the-cores-language-is-a-separate-decision-and-it-is-csharp.md) owns the core's language. Its Bevy rejection is corrected: it was filed under *Rust*, and three of its four arguments dissolve against Godot-shell-plus-Rust-core. |
| `adr/0002` | 🟢 | **Rewritten session eight.** It sized its boundary against a *renderer* when the actual consumer is an *inspector* — *"roughly two methods"* against ~20 required entry points, of which the largest family went unmentioned, and its own second revisit trigger had already fired on the day it was written. Rebuilt around **hot/cold query flavours**, with persistence explicitly off the axis. |
| `adr/0004` | 🟢 | **Grilled session eight, with ledger #29.** Its layout claim survives untouched and was never the problem; a **buffering strategy** had ridden into its Consequences and was protected by a *"Not performance"* revisit trigger. Replaced by `adr/0037`. One crack left open as **#29b**: the Chunk-partition claim holds for static entities and is unargued for mobile ones. |
| `adr/0003` | 🟡 | **Opened session eight.** Its *"zero transcendental functions"* claim was false and **hard-blocking** — it bans `Math.*` while `02 §5.4` requires `exp`, so the choice model was unimplementable as written. Fixed-point tabulated `exp`/`log` are now required core components at a **stated resolution**. **Still ungrilled: `02 §8`'s rule list, Q16.16's scope and overflow policy, and `02 §10`'s testing strategy** — which together are milestone 1. |
| `adr/0005`–`0009` | 🔴 | **Written from research, not argued.** All six gate Phase 2: `0005`/`0007` (fidelity tiers), `0006` (the Event Wheel's own rule, and it gates milestone 4), `0008` (walking), `0009` (parking), `0012` (routing intent). |
| `adr/0037` | 🟢 | Session eight. One live world state; hazards classified per table. Deletes ~150 MB of copying per Tick, redefines the Past as a phase-discipline fact, and strengthens crash forensics using machinery that already existed. |
| `adr/0036` | 🟢 | Session eight. The core's language, argued on the convergence finding: all three candidates produce the same code, so the decision falls to the surrounding factors. Adds the seventh CI lint, the intrusive-index-list rule, spike **S4**, and K6 as the revisit trigger. |
| `adr/0010`–`0022` | 🟢 | Sessions one and two. **This row is now known to be too coarse to be checkable, and `adr/0043` says so.** Thirteen ADRs, one mark, two sittings — and two of the thirteen have since been measured false: `adr/0014`'s *"the Chunk grid is already the pathfinding cluster"* (wrong by 256× in area, S2 R3) and `adr/0020`'s union-find Settlement identification (6 where Tarjan gives 8, S2 R1). Neither claim carried the decision its ADR was about, which is exactly how a supporting sentence passes through a session unread. **Split this row as each ADR is revisited.** |
| `adr/0023`–`0026` | 🟢 | Session three. `0026` gained a session-five superseding note: the tier wall re-justified as a **category boundary**, within-tier experience, productivity growth, and the finding that **the city has no central bank and correctly so**. |
| `adr/0029`–`0032` | 🟢 | Session five. Transit, crime, the Resource generalisation, and services-by-Trip. `0028` remains **reserved** for the unwritten difficulty-is-exogenous ADR. |
| `adr/0035` | 🟢 | Session seven. Infrastructure priced by what it consumes; Upkeep as consumed design life. Answers `0014`'s corridor question and retires its "probably budgeted". Adds capital expenditure and maintenance to `04 §5`, which had neither. |
| `adr/0034` | 🟢 | Session seven. Fields sorted by source geometry; the Cell split from the Chunk. Carries superseding notes into `0022` (Sealing is per Cell) and `0015` (the spatial world-creation constant is Cell size, not Chunk size), and makes `0014` retroactively load-bearing for the noise model. |
| `adr/0043` | 🟢 | Written after S2 R3, and it is a decision about how the corpus is built rather than about the game — `adr/0042`'s precedent. **A claim a measurement could settle must not be settled by argument**: every claim a session touches is typed *arguable* or *measurable*, and a measurable one is routed to a named spike with the number that would refute it. Evidence is the five claims S2 measured false, **two of which sat in 🟢 rows of this very table**. Its own first draft asserted all four were ungrilled and was corrected against this ledger before it was registered. |
| `adr/0033` | 🟢 | Session six. Two Rule families. Carries superseding notes into `0009` (parking is a Trip transaction, not a Rule) and corrections into `0015` (§4.2 → §4.3). |
| `plans/0001-foundational-design.md` | 🟡 | The origin document. Its research sections still hold; its architecture recommendations feed `05`, which is ungrilled. |

**The honest summary, updated after session five:** we have argued *what the city does* thoroughly and *how it is built* not at all — and the gap **widened**. Session five added transit vehicles to the traffic model, ~50% more Trips to the peak, a new event source, a District-level flow solve, four Resources, and a Rule-engine coupling that `CONTEXT` → Rule does not currently permit. Every one of those assumes a technical layer nobody has stress-tested.

**The deferral of `05` at the top of session five was correct and must not be repeated.** The argument for going to services first was that `05`'s job is to size a budget against the *complete* inventory of systems, and the inventory was missing a tier. That inventory is now materially complete. `05` is owed, and so is `02 §4`.

---

## Corrections owed to existing documents — **CLEARED**

All applied. Recorded here so the changes are findable, and because two of them were larger than "an edit."

| Document | What changed |
|---|---|
| `01-player §2` | Verb table rewritten: **five verbs — Zone, Connect, Service, Govern, Inspect.** `Fund` + `Regulate` merged. Zone families listed. Density stated as a ceiling. `Service` named as the design's one placement exception. |
| `02-sim §2.1` | District open question **closed as both** — automatic by default, player-adjustable. Extent bounded by pooling validity. |
| `02-sim §5.4` | The "stay-put / no-choice alternative" now names the **Hinterland**, and the domain-units-not-utility-units rule is stated where the choice model lives. |
| `02-sim §5.5` | "three separate placement systems" → per Zone family, since there are now five. |
| `02-sim §5.9` | "declines a density level" removed — **Buildings do not shrink.** Renumbered from a duplicate §5.6. **Abandonment contagion added** as a cycle damped by bid price. |
| `04-economy §1` | New: **the employer that produces no Good** (Office, and why not a sixth Good), and **money is conserved and it flows**. |
| `04-economy §5` | Now under `Govern`. **Third second-order effect added: tax rates are a velocity control.** Service funding as a fiscal multiplier; public jobs demand-determined and therefore unable to absorb unemployment. |
| `adr/0010` | Third superseding note: **Sorting is not a schools mechanism**, it is what the choice model does whenever the Outside is an alternative. "Immigration needs a composition model" is answered — there is no composition model. Tier 2 no longer closes by sorting alone. |
| `adr/0025` | Regulate reference updated post-merge. |
| `00-vision` | Open question 1 (transit) gains its **economic** case — labour sheds, wage premiums, and a destitution exit. Open question 4 (goods) **closed at five**. |

## Session six — the Rule engine, entered through `05`

**The ledger said `02 §4` and the user said `05`; both were right, because the two documents' open lists had bled into each other.** Three of the four things `§4` "does not say" were `05` questions wearing `02`'s badge — ordering is `adr/0003` determinism, rate-versus-the-wheel is `05 §9`, and Ruleset versioning for saves *is* `05` open question 6. It was one thread — **the Rule engine's execution model** — and the entry point barely mattered.

Settled, recorded in [`adr/0033`](../docs/adr/0033-two-rule-families-scheduled-and-swept.md):

| Decision | Record |
|---|---|
| **Two Rule families, not one.** Bin Rules are scheduled; Sweep Rules are polled | `adr/0033`, `02 §4`, `CONTEXT` → Rule |
| The discriminator: **subscribe when waiting on a specific named thing; poll when sweeping a population** | `02 §4` |
| `rate` is a **reschedule interval, not a polling period**. Failure **subscribes to the Bin that was short** | `02 §4.1`, `05 §9` |
| Every Bin holds a **wait list, drained round-robin** — the determinism answer and the balance answer are the same answer | `02 §4.1` |
| **The failure mode inverts and becomes silent.** Bins are not public fields; one write function; plus a sweep invariant in CI | `05 §9`, `adr/0033` |
| Wait lists are **rebuilt, never saved** — and dropped on hot reload, so they are never cross-version state | `05 §9`, `02 §4.3` |
| **Zone Rules were the second model all along.** Policy is its second instance | `adr/0033`, `CONTEXT` → Sweep Rule |
| **A Policy sweeps; a Zone Rule samples** — and the difference is semantic. Sampling *is* the behaviour model; a transfer is an entitlement | `02 §4.2` |
| Distributing a Policy onto Households is a **false economy** — same work, plus a spawn-arming invariant, and worse at `Evidence` expansion | `adr/0033` |
| **Proportion is a derived apply count.** No query language, no parser, no floats | `02 §4.1`, `CONTEXT` → Policy |
| **Four scopes, final.** `map` is write-only; there is **no proximity scope** | `02 §4.1`, `adr/0033` |
| **Movers choose; Rules transform.** A Parking Shed is a Trip's query, not a Rule's scope | `adr/0009` note |
| Money moves to **`local` scope** — `02 §4.2`'s example predated `adr/0024`, and left alone it was a thundering herd | `02 §4.3` |
| **The State Hash is the boundary of what optimisation may touch.** A mechanism never changes family for performance reasons | `05 §4` |

Through-lines added:

- **A contradiction between two documents is usually one thread filed twice.** The ledger's own ordering argument dissolved on inspection: three of `02 §4`'s four gaps were `05`'s questions. Before deferring a document, check whether its open list is actually somebody else's.
- **Applying a good rule everywhere is how you get the next bug.** Subscription is right for a Bin Rule and wrong for a Policy, and the reason is not cost — it is that *an entity cannot know whether it qualifies without being evaluated*, so the Event Wheel has nothing to sleep through. The wheel earns its keep on **activity**, never on **membership**.
- **The cheap centralised scan was dismissed without arithmetic.** Eight Policies over ten thousand Households is ~10 comparisons per Tick. *"O(population)"* is a shape, not a number, and at this scale the shape was irrelevant. Do the arithmetic before designing around a complexity class.
- **A repeated exception is the second model.** `adr/0031` found this on the transport axis; Zone Rules found it on the dispatch axis. Both times the fix was to name the family rather than write the fifth exception.
- **An unratified number is more dangerous than an open question.** The 10k figure was never decided by anyone. It arrived as an illustration, was repeated until it read as settled, and silently sized `03 §2.1`, the Microscopic Cap, Chunk size, the snapshot format, and the entire map-size analysis — against a target **100× smaller** than the design's actual ambition. An open question at least announces itself. **Audit the corpus for other figures nobody ratified.**
- **Sizing is a derivation, not a constant.** `target = map_area × mature_density × buildable_fraction` stays correct when the map changes; `1,000,000` does not. Same rule as *author in domain units, never in utility units* — a number you can recompute from things the designer can defend beats a number somebody once typed.
- **A new mechanism must be walked through the phase table before it is believed.** The round-robin wait list was written, recorded in an ADR, and *did nothing* — Phase 3's sorted-key settle order picked the winner regardless, so the head of the queue lost every time. It was only caught by tracing two named bakeries through all eight phases across three Ticks. Prose about a mechanism is not a check on it; `02 §1.1` is the check.
- **An ordering rule inherited from an implementation detail is a bug with a plausible face.** *"Sorted by entity id"* reads as neutral and is neither neutral nor stable — it biases permanently toward one Building and, given recycled row indices, lets an unrelated demolition decide a contested draw across the map. Any tiebreak worth trusting should survive the question *what real fact is this ordering asserting?*
- **The right test discriminates; a wrong one merely sounds strict.** *"A bounded set of Bins with a single mutation site"* passed parking and was useless. *"Failing on it produces a Rule that can wait"* killed it instantly and generalised to *movers choose, Rules transform.* When a test admits the thing you meant to exclude, the test is wrong, not the intuition.

Also settled:

| Decision | Record |
|---|---|
| **The Readout** — a named read-only scalar a Rule *consults*, as against a Bin it *spends*. Never consumed, conserved, or subscribed to | `CONTEXT` → Readout, `02 §4.1` |
| **A Rule may read anything the player can see, and nothing else.** The readable set *is* the `Evidence` surface — a `LEGIBLE CAUSE` guarantee by construction, and a stable Ruleset interface that fails loudly | `CONTEXT` → Readout |
| **Predicates belong to Sweep Rules only.** On a Bin Rule a predicate has nothing to subscribe to (so it polls) and produces no `on_fail` chain (so it is a silent non-event) | `02 §4.1`, `adr/0033` |
| Every apparent Bin Rule predicate dissolves: staffing → a **labour input Bin** filled by commute Trips, density → a different `kind`, time of day → the scheduler | `02 §4.1` |
| **A derived apply count of zero is a success, not a failure** — nothing is missing, so nothing is waited on | `02 §4.1` |
| **A subscription records the shortfall**, and a Bin drains from the head only while the arriving quantity covers it. Without this the queue is decorative — Phase 3's sort would pick the winner and the head would lose forever | `02 §4.1`, `02 §1.1` |
| **Phase 3 settle order is a counter-based random shuffle**, not entity id. Entity id was *biased* and, because `05 §3` recycles row indices, not even *stable* | `02 §8` rule 5, `05 §3` |
| **A handle index is never a sort key.** Recycled indices mean an unrelated demolition could change who wins a contested draw across the map | `05 §3` |
| **`05` had no stated budget.** Now two figures: **10k = the first hour** (responsiveness), **1M = the late-game floor** (does it still run) | `05` — the budget |
| **1M Citizens is the spec, not headroom** — the endgame is sprawling polycentric cities, and nothing smaller exercises `adr/0020` | `03 §2.1`, `05` |
| **Map closed at 4096²** (268 km², ~3,700/km² — LA sprawl), 2048² as the S2 fallback. Ledger #1 closed | ledger #1, `05` |
| **Spike S2 is now the project's top risk**, not an optimisation choice: it decides whether 1M is reachable | `05`, `adr/0020` |

That last group closes capability 2 as well: productivity reads workforce experience as a Readout in a derived apply count, which is a coupling `CONTEXT` → Rule now permits explicitly.

### Still open in `02 §4`

- ~~**Is `mean_workforce_experience` a legitimate Building Readout?**~~ **CLOSED by the `02 §4` residue session, and by deletion rather than by the sentence this entry asked for.** The recommendation here — *workers are not Occupants, so it reads as permitted* — is a **letter defence against a spirit invariant**, and `CONTEXT` → Building says in terms that a Cohort *"would re-enter here if anywhere."* A **sum** is not an average, so the question evaporates instead of being argued past; the mean also costs a division on a hash-bearing path, where `sum / count` and then `× 15 / 100` round twice. Experience folds into the **labour input Bin** as a per-worker deposit multiplier, which is better on its own terms: under a Readout an unstaffed Building has a derived apply count of **zero**, which `02 §4.1` calls a *success*, so it would re-arm for ever and never produce an `on_fail` chain — the *silent non-event* that section bans predicates for. Summed workforce experience stays worth **displaying**; `02 §2.5`'s test separates the two.
- **Labour as an input Bin** was reached for as the clean answer to staffing, and it is a real change to how employment works. `adr/0026` has jobs as a Household↔Business relationship; a labour Bin filled by arriving Trips is a *second* representation. They need reconciling, and `04 §7` (Jobs) is already stale twice over.
- ~~**Fallback chain depth.**~~ **CLOSED by the `02 §4` residue session, recorded in [`adr/0045`](../docs/adr/0045-a-fallback-chain-is-a-source-ladder-over-one-bin.md), and both of this entry's multipliers were wrong.** A **Policy cannot lengthen a chain** — Policies are Sweep Rules and a Sweep Rule has no `on_fail`. **Nine Resources cannot either**, because *no power → run the generator on fuel* is input substitution, which the source-ladder law refuses at load. **Cycle checking is not a `02 §4` question at all**: the `on_fail` graph is static, so it is Ruleset validation on `adr/0015`'s error surface. And depth **costs no subscriptions**, so it is a `LEGIBLE CAUSE` question only — typed **measurable** under `adr/0043` and routed to slice 7, whose instrument `02 §9` already required.
- ~~**Ruleset versioning.**~~ **CLOSED — ledger #24, recorded in `05 §7`.** Two policies, not one: play permits a cross-Ruleset load with degradation and warnings; replay refuses an *unaccounted* mismatch. The discriminator is the State Hash rule again, third application. Three findings came out of it: **a logged transition is replayable**, because degradation is a pure function of `(state, old, new)` — what defeats replay is a changed **binary**, not changed data. There are **two replay bases**, and bug reports use the weaker one (*from save*, which §8 already builds), so a city that crossed several patch boundaries stays diagnosable. And the one class no replay can ever reach — a defect caused by a degradation three patches ago — is answered by making degradation **state rather than a warning**: a **provenance trail** in the save naming what each transition destroyed. `adr/0006`-checked: it grows with patches survived, not elapsed time, and caps with older entries aggregated.

## Session seven — Chunk size, and it was two questions welded together

**The question as posed was malformed, and finding out why took the session somewhere else.** `05 §5` asked whether 32×32 should grow, or whether HPA\* should get a coarse super-Chunk. Neither: the grid had a **design** role and six **performance** roles sharing one number, and the only fork that mattered was between those two kinds of decision.

Settled, recorded in [`adr/0034`](../docs/adr/0034-fields-are-sorted-by-source-geometry.md):

| Decision | Record |
|---|---|
| **Chunk size changed the State Hash and `05 §4` said it didn't.** Map Layers store one value per Chunk, so Chunk size *is* pollution resolution, which feeds Fertility and the choice model | `05 §4`, `adr/0034` |
| Split into the **Cell** — 32×32, design constant, frozen — and the **Chunk**, a strict multiple, hash-preserving | `CONTEXT` → Cell, `05 §5`, `02 §2.1` |
| **The split costs nothing** because a strict divisor means every conversion is a shift and no boundary can disagree. `05 §5`'s unification argument survives at six roles | `05 §5` |
| **Chunk size is now a measurement, not an argument** — and probably wants to be *larger*. Pathfinding has the strongest claim; rendering has a genuine two-sided optimum | `05 §5`, spike S2 |
| **Fields are sorted by source geometry, not by subject.** Wide-range point → diffused Layer; short-range line → point-of-use query; **area reduces to line** along its perimeter; graph transport → network flow | `adr/0034`, `02 §2.4` |
| The old **`Pollution` row was two mechanisms** — industry (point, 1–10 km) and traffic (line, 150 m) — on one grid with one kernel, so one was always wrong | `02 §2.4` |
| **Noise and near-road pollution stop being Map Layers.** A line source is a distance query, exact at Tile resolution, and quantising it to *any* grid is worse than not quantising it | `CONTEXT` → Noise, `02 §2.4` |
| **A Layer is a convolution, never a relaxation.** This is what makes superposition and incremental re-diffusion *exact* | `02 §2.4`, `CONTEXT` → Map Layer |
| **A Water Body is a Bin** with a capacity and an outflow rate. Pond, river and sea are two numbers, not three types | `CONTEXT` → Water Body, `adr/0034` |
| **Nothing is an infinite sink** — the map holds a *section* of ocean, and a section is bounded. Capacity decides whether pollution is a **debt** or a **rent**, which is a gradient | `CONTEXT` → Water Body |
| A Water Body's land effect is a **shoreline line source** whose intensity is the Bin's level — which is why *area* needs no geometry of its own | `adr/0034` |
| **§2.5 — a classification procedure for any new field**, seven questions and five guard rules | `02 §2.5` |

Then the corridor thread, which `adr/0014` had left open since it was written and which nothing tracked. Recorded in [`adr/0035`](../docs/adr/0035-infrastructure-is-priced-by-what-it-consumes.md):

| Decision | Record |
|---|---|
| **The corpus had no cost model for infrastructure at all.** `04 §5` had taxes, service funding and borrowing; *maintenance* and *upkeep* appeared in no document and in none of the 34 ADRs, and nothing said a road costs money | `04 §5`, `adr/0035` |
| **Infrastructure is paid for three times** — Money, Materials, and Land — and none of the three is authored | `adr/0035`, `CONTEXT` → Infrastructure |
| **Money spent on infrastructure is a transfer, not a sink.** Conservation means it becomes somebody's income, and the Materials chain decides whose | `adr/0035`, `04 §5` |
| **A city that builds while importing all its Materials is exporting its own stimulus** — a new and *economic* argument for the domestic chain, where every previous one was logistical | `04 §5` |
| **Upkeep is `construction cost ÷ effective life`**: a base term from design life plus a wear term from Segment volume. The only authored number is a **duration** | `CONTEXT` → Upkeep, `adr/0035` |
| It lands on `adr/0033`'s **Sweep Rule** and `05 §9`'s staggered slot — sweeping a population, no wheel entry per Segment | `adr/0035` |
| **Roads Seal.** Sealing had only ever been discussed through Buildings, leaving the road network invisible to Fertility. The Arterial verge stays *unsealed*, so Woodland regrows on it | `CONTEXT` → Sealing |
| **`adr/0014`'s corridor question answered with no new mechanism** — dead block interior, plus Processing bidding high on land whose only defect is noise it does not care about. The premium concentrates **near Junctions**, so ramp placement is a land-use decision | `adr/0014` note, `adr/0035` |
| **`adr/0014`'s "probably budgeted" retired.** Sealing, sterilisation and wear-based Upkeep restrain Arterials without an authored cap | `adr/0014` note |

Then both of that thread's own debts, settled before moving on:

| Decision | Record |
|---|---|
| **Borrowing is a player action, never an automatic overdraft.** `adr/0024`'s *"a deficit becomes a debt burden"* read as automatic, and an automatic damper deletes a decision | `adr/0024` note, `CONTEXT` → Money |
| **The treasury genuinely empties**, and what happens then is ordinary `adr/0033` — the Rules that could not draw **wait** | `adr/0035` §3a |
| **Unfunded Upkeep is not a decay system.** Unrenewed life lowers capacity and free-flow speed, both Road Graph attributes the VDF already reads. The deferral was wrong, not merely early | `adr/0035` §3a |
| **The Bill converts into the Clock** — the first mechanism coupling `01-player §5`'s two pressure axes, and it makes a fiscal crisis legible on the map | `01-player §5.1`, `CONTEXT` → Upkeep |
| **No maintenance funding lever.** A slider whose only sensible setting is *as high as affordable* is not a decision | `04 §5`, `adr/0035` §3a |
| **Overfunding is the preservation curve, and it is a Policy** — *rebuild below N% of life*, sweeping the remaining-life distribution, with a derived reference point | `adr/0035` §3b |
| **District-overridable, and that is what makes it a choice.** Preservation needs capital sooner and more often, so nobody can commit everywhere — the player chooses *where*, not merely *how much* | `adr/0035` §3b |
| **The deferral bet is settled by the demographic transition.** Deferring works while revenue grows, and growth slows at thread D's crossover | `adr/0035` §3b |
| **The liability is told, not shown.** A decayed road has a visual signature; an accumulated deferred liability does not — thread D's rule, second application | `adr/0035` §3b |
| **Design life is derived, not authored** — from the share of a mature city's budget Upkeep should occupy. Wear weighting is grounded separately: damage scales superlinearly with axle load, so **freight dominates and commuters barely register** | `adr/0035` |
| **Remaining life is stored as accumulated wear, never as a fraction** — or a Ruleset edit rescales it and design life becomes an `adr/0015` world-creation constant | `adr/0035` §3 |
| **Right-sizing needs the bill decomposed by utilisation**, or pruning is a bulldozer and a guess | `adr/0035` |

Through-lines added:

- **A constant welded to two decisions is governed by whichever of them is louder.** Chunk size had a design role and six performance roles; the performance roles had a profiler and the design role had nobody, so a benchmark was entitled to an opinion about farm yields. **The State Hash rule is the test that separates them, and it works only if somebody applies it to each number by name.** `05 §4` did not fail — it was never run against this one.
- **Do the arithmetic before believing a stated tension.** `05 §5` named a four-way pull and three of the four were fictional: save overhead was 64 KB, diffusion was already at the noise floor, the parallel partition was non-binding. This is the second session running that a *shape* (`O(n)`, "seven competing roles") turned out to have no *number* behind it.
- **A one-sided constraint is usually an unexamined one.** Render streaming was recorded as *smaller is better for culling*. It is a bowl: draw calls scale with visible Chunks × archetypes, and MultiMesh exists to collapse draw calls. Any constraint stated as a direction rather than an optimum deserves a second look.
- **Ground the axis in reality before choosing a resolution.** Asking what real falloff distances are — 150 m for near-road pollution, 1–10 km for a stack plume, 50–300 m logarithmic for road noise, 400 m for a walk — sorted the fields in one pass and dissolved the grid question entirely. *Author in domain units* applies to the **machinery**, not only to the balance constants.
- **The sorting axis was geometry, not subject.** Pollution and Noise each had a point-source and a line-source half; sorting by *what emits it* separated them cleanly where sorting by *what it is* never could. Third instance of the same move — `adr/0031` sorted Resources by transport, `adr/0032` sorted Services by who travels.
- **Categories collapse when you find the right generalisation.** The water taxonomy went from five body types to **two numbers on a Bin**, and the debt/rent distinction became emergent from inflow-versus-outflow rather than a category. *Scarcity is a gradient, never a wall*, arriving somewhere nobody was looking for it.
- **"Unbounded" is a modelling convenience that smuggles in a dominant strategy.** Calling the ocean an infinite sink made coastal dumping free and would have beaten every Waste mechanism `adr/0031` built. The fix was not a rule but an observation about the map: *only a section of the ocean is represented, and a section is bounded.* **An exploit is usually a missing mechanism**, and here the missing mechanism was already implied by the map's own edges.
- **The most dangerous gap is the one with no placeholder.** Session six found an *unratified* number silently sizing five decisions. This session found something worse: capital expenditure and maintenance, the two largest items in a real city budget, were **absent** — not wrong, not unratified, simply never written, while every participant assumed them. An unratified number at least appears in a document and can be audited. **Audit for absent categories, not only for unratified figures**, and the way to find them is to ask what a domain expert would expect to see and then grep for it.
- **A budget is what you reach for when you have not found the price.** `adr/0014` wanted Arterials capped because uncapped Arterials break the Junction scope argument. The cap was never needed: they Seal, they sterilise the land they cannot front, and they wear fastest because they carry most. Third instance of *an exploit is usually a missing mechanism* — and the tell each time was a restraint justified only by *otherwise players do X*.
- **Ask what the cost is a function of, not what the cost is.** *§X per Lane-Tile per Day* is a magnitude constant and would have gone stale as the economy grew. *A design life in Days, consumed faster by traffic* is a duration — scale-free, the same in a village and a metropolis — and it arrived with four consequences nobody designed: the Bill responds to **use** rather than size, freight routing gains an economic cost, transit's case strengthens, and an unused road is nearly free. **Deriving the constant produced the mechanism.**
- **A deferral inherited is a deferral untested.** *Unfunded upkeep* was parked as "a whole decay system," and one turn later the design-life formulation reduced it to a Rule that did not run — no new state, no new effect, no new explanation surface. **The reason to re-test is that the deferral's cost is a function of the design around it**, and that design had just changed. Same failure the corpus already names about deferrals being routed around rather than overturned, in the opposite direction.
- **An automatic damper is a deleted decision.** `adr/0024` wrote borrowing as a damper and it read as an overdraft. Making it a player action cost nothing and bought the entire unfunded branch — four exits, all visible, and refusing all four is a supported outcome. **Check every damper in the corpus for whether it fires by itself**; a mechanism that rescues the player without being asked is a mechanism that removed a choice.
- **A lever with one correct setting is a wealth readout — unless it is scoped where scarcity bites.** *Rebuild below N%* is always *as high as affordable*, which failed the `NO VERDICT` test as a global knob and passed instantly as a District-overridable one: the constraint was never the setting, it was that preservation cannot be afforded **everywhere**. **When a knob looks degenerate, check its scope before rejecting the mechanism.**
- **A guard rule needs a subject, not a budget.** *A modelling refinement is admitted when a player decision distinguishes it* killed depth and stratification instantly, admitted downstream flow loudly, and left tidal direction in the honest middle — passing on merit, parked on priority. A cost-based test would have got all three wrong.

### New debts this session created

- ~~**`adr/0014` is retroactively load-bearing for the noise model.**~~ **CLOSED same session.** Checked against `adr/0029` and the flagged band was the wrong one: **Separated** is already an Arterial (`CONTEXT` → Arterial reads *"highway, rail, major boulevard"*) and equally rare, while **Reserved** manufactures the middle case by putting Arterial-scale volume onto a grid Street. Fixed by **enumerating linear sources by loudness rather than by road class** — every source within range whose contribution exceeds the ambient background. A crossover, not a threshold; nobody authors a number, and the set stays small by definition. `0014`'s real contribution is **bimodal traffic volume**, which is robust to new road classes in a way "two tiers" was not. *Trade-off recorded in `02 §2.4` for profiling: loudness-enumeration is a spatial search where class-enumeration was a fixed lookup — and swapping them changes the State Hash, so it is a design change rather than a free optimisation.*
- **A river is an uncosted Waste export**, which `adr/0024` conserves Money specifically to prevent. Parked in `deferred.md` with a trigger, and the honest fix may be a degrading Hinterland rather than a price.
- **Water pollution is the first candidate causal chain for Health** (#26), the one Service with no stated purpose. A lead, not an answer — and it should be argued when #26 is opened rather than assumed now.
- **Lake remediation is an endgame lever** of the same shape as brownfield unsealing. Third instance for ledger #4.
- ~~**Unfunded Upkeep**~~ and ~~**design life values**~~ — **both CLOSED the same session**, see the block below.
- **`04 §7` (Jobs) is now stale three times over** — `adr/0026`, the labour-Bin question, and now public construction as a wage channel.
- **Visual content for Arterial verge** — sound walls, embankments, planting. No mechanics, but it should be scheduled beside the Junction piece library rather than discovered as an empty grey strip.
- **The Chunk's actual size is still unmeasured**, and it is still on the *cannot be retrofitted* list. It now belongs to spike **S2** alongside routing, since the pathfinding role has the strongest claim on it.
- **`CONTEXT` → Chunk was removed.** After the split it carries no player-facing meaning, and `CONTEXT.md` is domain language. It is defined in `05 §5` instead. Reverse this if it turns out to be missed.

---

## Session eight — the stack, and the session was redirected on purpose

**Opened on `05 §6` threading and got two questions in before being stopped, correctly.** The first question found that §6 is titled *"Threading policy"* and never says which thread `step()` runs on, while three places in the corpus already assume a reader of the Past that outlives the Tick that produced it. Before that could be argued the session was redirected to the thing underneath it: **the stack had never been discussed at all.**

Settled, recorded in [`adr/0036`](../docs/adr/0036-the-cores-language-is-a-separate-decision-and-it-is-csharp.md):

| Decision | Record |
|---|---|
| **`adr/0001` argued one decision and made three** — host, core language, and a runtime it never named. Split; `0001` decides the host only | `adr/0001` note, `adr/0036` |
| **`05 §4`'s State Hash rule is what separates them.** The host is hash-preserving by construction; integer semantics, shift behaviour and the RNG's hash are language-defined and are not | `adr/0036` |
| **The core is C#** — argued, not inherited. The convergence finding: all three candidates produce nearly the same code, so the decision falls to shell language, tooling, enforcement, fluency and iteration speed, and C# takes all five | `adr/0036` |
| **The constraints on the core are not C#'s constraints.** Six of seven CI lints are needed identically in Rust; handles-not-references is something Rust *forces* rather than relaxes | `adr/0036`, `05 §4` |
| **C# is the better determinism substrate than C++** — fully specified overflow, shift and division, where C++'s signed overflow is UB. Ties Rust, beats C++, and nobody had written it down | `adr/0036` |
| **`0001` rejected *Bevy* and filed it under *Rust*.** Three of its four arguments dissolve against Godot-shell-plus-Rust-core, which it never considered. Rejected anyway, on its own grounds | `adr/0001` note, `adr/0036` |
| **The GC risk was never the tables** — it is the per-Bin wait lists, the cached Parking Sheds and the wheel buckets, ~10⁶ traced objects if written as `List<T>` | `05 §4`, `adr/0036` |
| **Every variable-length collection is an intrusive index list.** Seventh CI lint: no reference types in simulation state | `05 §4`, `adr/0036` |
| **The escape hatch should be assumed unused.** A backstop is not a plan; invest in the discipline that makes the choice succeed | `adr/0001` note, `adr/0036` |
| **Ledger #29 opened** — the Past/Future double buffer is a full-state copy per Tick, ~8–15 ms at 1M, and it **defeats the Event Wheel** | ledger #29 |
| **Spike S4 created**, six kernels, runs before S0. **K6 is `0036`'s revisit trigger** | *work that must be scheduled* |

Then `adr/0002`, which the language thread had just promoted to the highest-value 🔴. Recorded in the [rewritten `adr/0002`](../docs/adr/0002-simulation-is-an-engine-agnostic-library.md):

| Decision | Record |
|---|---|
| **`0002` sized its boundary against a *renderer*; the actual consumer is an *inspector*.** *"Roughly two methods"* against ~20 entry points the corpus requires, and the largest family — Evidence — went unmentioned | `adr/0002` note |
| **Its own second revisit trigger had already fired when it was written.** *"A feature that cannot be expressed through `step` and `visible_agents`"* — `02 §9` is a whole section of them, and it is explicitly *"a constraint on the simulation"* | `adr/0002` |
| **Two query flavours, split on the cadence of the caller** — hot and cold. Not more methods; the axis the methods sort on | `adr/0002`, `05 §2` |
| **The split is caller cadence, never data location.** A cold Evidence query reads hot fields constantly and is still cold | `adr/0002`, `05 §2` |
| **Third application of an axis already in the corpus** — `05 §3` splits tables, `adr/0036` places the lint, `0002` splits the API | `adr/0002` |
| **It discharges `adr/0036`'s owed exception enumeration.** The hot path allocates nothing; the cold path may, because it runs on a click. Both candidate exceptions were on the same axis | `adr/0036`, `05 §4` |
| **Persistence is not a query** and does not sit on the axis — bytes not answers, whole world not bounded sample, versioned, concurrent | `adr/0002`, `05 §7` |
| **The boundary is a membrane and was guarded in the direction nothing crosses.** `02 §9` deliberately shapes simulation state to a UI requirement; the real leak vector is a method returning a formatted string | `adr/0002` |
| **Second CI check: no human-readable strings returned from `Core`.** Ids and numbers only; the shell resolves display text | `adr/0002`, `05 §2` |
| **Notifications are an outbound queue the host drains**, not a push — the no-callbacks property survives the cold path intact | `adr/0002` |
| **`0002`'s three payoffs are no longer equal.** `0036` says assume the escape hatch is unused, so reversibility weakens; headless testing and GPU-free determinism alone justify the boundary | `adr/0002` |

Then `adr/0004` and ledger #29, taken together because they were one thread. Recorded in [`adr/0037`](../docs/adr/0037-the-world-is-single-buffered-and-hazards-are-per-table.md):

| Decision | Record |
|---|---|
| **#29 was not a defect in `0004`'s layout claim.** A *buffering strategy* had ridden into the design inside a *data-layout* ADR — the welding pattern, third instance | `adr/0004` note, `adr/0037` |
| **`0004`'s revisit trigger says *"Not performance"*** — correct about layout, and it made a performance defect unchallengeable on the only available ground for eight sessions | `adr/0004` note |
| **One live world state.** A table is double-buffered **iff a parallel phase both reads and writes it** — Lane dynamics and Map Layer cells, ~2 MB against ~150 MB | `adr/0037`, `05 §3` |
| **It was a naming artifact.** *"Two world states"* implies a full copy; the real property is *no parallel phase observes a partially-updated peer*, which is per-table | `adr/0037` |
| **Phase 2's read-only-ness became load-bearing.** It is what permits every entity table to be single-buffered | `02 §1.1`, `adr/0037` |
| **The Past is redefined** — a phase-discipline fact, *the state as of the start of this Tick*, not a second copy. `02 §1.1`'s semantics unchanged | `05 §3`, `02 §1.1` |
| **Crash forensics gets *stronger*** — last checkpoint plus Input Log, which replays into the failure under a debugger rather than dumping a corpse. No new machinery | `05 §8`, `adr/0037` |
| **The undo journal was rejected as one mechanism too many** — and rejecting it deleted its write barrier on the hot path | `adr/0037` |
| **Cold queries are serviced at a Tick boundary**, or a mid-Tick drill-down reads a torn world | `adr/0002`, `adr/0037` |
| **`adr/0002`'s free-Past property traded knowingly** for 50–150×, replaced by three specific channels | `adr/0002`, `adr/0037` |
| **`CONTEXT` → Past / Future removed** on the `Chunk` precedent — implementation, not domain language | `CONTEXT`, `05 §3` |
| **Ledger #29b opened** — `0004`'s Chunk-partition claim holds for static entities and is unargued for mobile ones | ledger #29b |

Then `adr/0003`, opened alongside a readiness review of the whole corpus against the roadmap:

| Decision | Record |
|---|---|
| **`0003`'s *"zero transcendental functions"* is false**, and `02 §1` and `05 §4` repeated it. `02 §5.4`'s choice model is a **softmax over `exp`**, and `adr/0032` routes every Provider List choice through it | `adr/0003`, `02 §1`, `05 §4` |
| **The contradiction was hard, not cosmetic.** `0003` bans `Math.*`, so as written **the choice model could not legally be implemented** | `adr/0003` |
| **`exp` and `log` are tabulated fixed-point functions in the core**, from the start — promoted from `0003`'s contingency to a required component | `adr/0003`, `02 §5.4` |
| **The table's resolution is a stated figure, not an implementation detail.** It perturbs the effective `μ`, and `adr/0005`/`0017` make `μ` the thing that prevents stampedes — so a hidden table size would be a hidden global constant tuning a system-wide outcome | `adr/0003`, `02 §5.4` |
| **The *algorithm* is recorded, not frozen.** Softmax stands; **Gumbel-max** (exactly the same distribution, no `exp` on the hot path, cheaper) and coarser/finer tabulation are noted as levers, triggered by **how the housing market feels** rather than by a profile | `adr/0003`, `02 §5.4` |
| **A third case for `05 §4`'s State Hash rule: hash-breaking but distributionally neutral.** Swapping the choice algorithm changes every realisation and no behaviour — safe, but it invalidates stored replays and hash baselines, so it needs a deliberate re-baseline | `adr/0003` |
| **Third consecutive ADR whose revisit trigger was written against its author's mental model** rather than against the corpus that already existed | `adr/0003` note |
| **Overflow is the one class of bug the State Hash oracle cannot see** — both runs wrap identically, so replay, bisection and save/reload are all blind. It needs a policy, not a test | `adr/0003` |
| **The arithmetic killed the uniform-`checked` proposal.** Money in i64 has ~10⁹× headroom and is unreachable; **i32 is already exceeded** at target population. **Width is decisive, the check is worthless** — and width is free at runtime where checks are not | `adr/0003` |
| **The real hazard is Q16.16's *range*, not any width** — ±32,768, blown by a product of two moderate absolutes. And widening is *not* symmetric: fixed×fixed needs a 2N-bit intermediate, so Q32.32 costs an i128 multiply | `adr/0003` |
| **The stated use of Q16.16 never multiplies at all.** `position += velocity × ticks` is fixed × *integer*, bounded by the map at 8× headroom. **Fixed×fixed exists in exactly one place: IDM and the VDF** — the ungrilled traffic model | `adr/0003`, `05 §3` |
| **Every one of those terms is a ratio**, so: **fixed-point multiplication operates on dimensionless ratios, never on absolute quantities.** [0,3] to the fourth power against ±32,768 is three orders of headroom, with no widening and no check | `adr/0003` |
| **Fifth instance of *ratios are real; units are invented*** — and the first where it prevents a defect rather than clarifying a design | `adr/0003` |
| **Typed quantities make the rule structural.** `Money`, `Ticks`, `Tiles`, `Ratio` as `readonly record struct` — `Tiles × Tiles` does not compile. `adr/0004`'s typed-handle argument moved from identities to quantities, at zero runtime cost | `adr/0003`, `05 §3` |
| **Money stays signed.** The design genuinely never holds negative money — four independent decisions confirm it — but **stocks are non-negative while every delta and flow is signed**, so unsigned would arm `balance - cost` while protecting a column with no bugs in it | `adr/0003` |
| **`checked` scoped to the fixed-point library only**, where it guards a *stated rule* rather than an unknown — the CI-lint posture applied to arithmetic. Conservation invariants (`05 §1`) are the money detector and already exist | `adr/0003` |
| **`adr/0006` applied to magnitudes:** no quantity accumulates without bound. `adr/0035`'s wear **caps at a stated multiple of design life** | `adr/0003` |
| **Fixed-point range sizing deferred to `03 §5`**, legitimately — IDM and the VDF are its only sites and the traffic model has never been grilled. Nothing in Phase 1 is blocked | `adr/0003` |

Then `02 §8` and `02 §10`, grilled together — which is what exposed the gap, because **§8 states rules, §10 states tests, and neither noticed that several rules had no test and several tests did not run where the risk was**:

| Decision | Record |
|---|---|
| **The RNG hash function was cited in four documents and defined in none.** Now normative — **SplitMix64's finalizer, constants written out literally** so it is re-implementable from the document alone | `adr/0003`, `02 §8` |
| **The RNG is a *format*, not an implementation detail.** An Input Log reproduces a run only if the hash is bit-identical, so changing it is a **save-format-class change** under `05 §7`, never a free optimisation | `adr/0003`, `05 §4` |
| **`purpose_tag` is a compile-time integer from one central enum**, never a string — a string needs string hashing (banned) and a typo collides silently. **Uniqueness is a build-time check**, which is the only possible detector | `adr/0003`, `02 §8`, `02 §10` |
| **The State Hash's coverage was the oracle's own blind spot.** *A field that is saved but not hashed is invisible to every tool in the project* — runs diverge, hashes agree, replay succeeds, save/reload passes | `adr/0003` |
| **Fixed structurally, not by a test:** every field is declared once as `(saved AND hashed)` or `(derived AND rebuilt)`, and both are generated from that declaration. Save/reload then transitively proves hash completeness, and composition order falls out | `adr/0003`, `02 §8`, `adr/0004` note |
| **§8 rule 1 was worded too narrowly** — *"no floats in simulation **state**"* permits a float temporary cast to an integer, which is exactly as non-deterministic. `05 §4` had the same bug. Both now say **arithmetic** | `02 §8`, `05 §4` |
| **Two constructs specified rather than banned:** integer division truncates toward zero, which is a **directional bias at every zero crossing**; and shift counts are silently masked. Both go through helpers | `adr/0003`, `02 §8`, `05 §4` |
| **§10's invariants ran in *debug builds* and the runs that matter are release** — the same inversion as `checked`, and it would have closed the gate exactly where the exposure is | `02 §10` |
| **Invariants now sort by frequency, not by build configuration** — `O(1)` per Tick, `O(n)` staggered, whole-world walks at end of run. `adr/0033` had already found this shape and §10 had not applied it | `02 §10` |
| **Golden-hash regression restated:** the point is not that the hash never moves, it is that it never moves **without someone saying so** | `02 §10` |

Through-lines added:

- **A rule with no specification is not a rule.** `hash(seed, entity, tick, purpose)` was cited in four documents, treated as settled by all of them, and was **unimplementable** — two people could satisfy every stated constraint and produce different cities. The tell is a rule that names a *function* rather than a *property*: properties are self-describing, functions need constants. **Grep the corpus for anything written as a call and check it has a definition.**
- **Ask what watches the watchman.** The State Hash is the project's primary oracle and nothing tested its **coverage** — a field saved but not hashed defeats replay, bisection and save/reload simultaneously, silently. This is the third defect class found this session that the oracle is structurally blind to, after overflow and the wrapping-identically problem. **A tool that certifies correctness needs its own completeness argument, and it should be structural rather than a test**, because a test for coverage has the same blind spot as the thing it tests.
- **A guard gated on build configuration is inverted, because exposure and configuration are anti-correlated.** §10 ran its invariants in debug; the runs that surface the bugs are millions of Ticks long and run in release. The same reasoning killed *debug-checked, release-unchecked* an hour earlier, from the other direction. **Invariants sort by frequency — `O(1)` per Tick, `O(n)` staggered, whole-world at end of run — never by build.** `adr/0033` had already found this shape and nobody carried it across.
- **Grill paired documents together.** §8 and §10 had each been internally consistent for eight sessions. The rules-without-tests and tests-that-do-not-run gaps existed *only in the space between them*, and neither document could have exposed it alone. **Where one document states rules and another states their enforcement, they are one subject and should be read as one.**
- **Do the arithmetic before choosing a *policy*, not only before believing a tension.** The uniform-`checked` proposal was defensible, principled, and wrong: it charged the hottest code in the project 0–30% to protect the one column with ~10⁹× of headroom, while the actual hazard sat in a **format range** that no amount of checking would have made safe. Sizing it took two minutes and moved the answer from *check everything* to *state the widths, and constrain what may be multiplied*. **A policy is a magnitude claim about a distribution of risk, and this corpus's rule about magnitude claims applies to it.**
- **When widening stops being free, that is the signal to look for a structural constraint instead.** i32→i64 is free; Q16.16→Q32.32 is not, because fixed×fixed needs a 2N-bit intermediate. The asymmetry is what forced the question *what is actually being multiplied*, and the answer — **ratios, always** — was cheaper than either widening or checking, and was already a through-line the project had been using for other reasons since `adr/0019`.
- **A convention needs a type or it needs a lint; it never survives on discipline.** *Multiply only ratios* is unenforceable by any analyser, because the units are not in the type — until they are. **Typed quantities turn the project's oldest through-line into something the compiler checks**, and they are the same move `adr/0004` already made for identities. This is what promoted them from tidy to required.
- **Not every decision needs making upfront, and the test is *reversibility*, not importance.** The choice function is the most consequential algorithm in the game and one of the cheapest to swap, because its interface is *a scored list in, one choice out*. The corpus already maintains a *cannot be retrofitted* list — determinism, the renderer, Chunk size, routing, scale, buffering — and **the complement of that list is the deferral list**, which nothing had ever written down. Grilling a swappable algorithm spends session budget on something a playtest answers better. *What must be settled now is the substrate underneath it, which is not swappable at all.*
- **When a decision looks urgent, separate the algorithm from the substrate.** `exp`-versus-Gumbel is a tuning choice; *the core contains a deterministic fixed-point `exp` at a stated resolution* is a commit-#1 constraint. They arrived as one question and only one of them was actually due.
- **889 KB of design prose and zero lines of code.** The corpus cites Citybound repeatedly as a developer who built infrastructure instead of a game; the mirror failure is arguing design instead of testing it. The design is not under-argued — it is under-**tested**, and past a point more argument is how you avoid finding out. **Six items gate the whole of Phase 1 and every one is engine-level**; most of what remains ungrilled is a playtest question wearing design-question clothing.
- **The welding pattern is not rare, it is the default failure of a well-written ADR.** Three instances in one session, found from three directions: `0001` settled the core's *language* while arguing a *host*; `0002` shipped a *query surface* it never named while specifying a *renderer boundary*; `0004` fixed a *buffering strategy* while deciding a *data layout*. In every case the argued decision was **correct** and the passenger was **unargued** — which is exactly why nobody looked. **The tell is an ADR whose consequences include something its Why never mentions.**
- **A revisit trigger protects the decision its author had in mind.** `0004`'s *"Not performance"* is true of layout and foreclosed the only ground on which its block copy could be attacked; `0002`'s trigger had already fired on the day it was written. **Run every trigger against what the ADR actually decided rather than against what it is titled**, and against the corpus as it stands rather than against the future.
- **The first correct-looking answer can still be one mechanism too many.** The undo journal was right, cheap, and unnecessary — determinism plus the Input Log already did crash forensics better. Rejecting it deleted the write barrier that was the whole proposal's only real cost. **When a fix requires new machinery, check whether an existing guarantee already covers the case it was built for.**
- **"Is this fancy dirty-bit checking?" was the question that found the simplification.** It forced the mechanism to be re-derived in plain terms, and re-deriving it exposed a redundant part. **Explaining a design to someone who will not accept a hand-wave is a debugging technique**, not a courtesy — and it belongs in the loop *before* the ADR, not after.
- **An ADR's revisit triggers are written against the system its author had in mind, so a mis-sized ADR cannot detect its own mis-sizing.** `0002` named the exact condition that would have caught it, and the condition was already true. **The check is to run each trigger against the corpus *as it stands* rather than against the future** — a trigger that is already satisfied on the day of writing is not a trigger, it is an unnoticed defect.
- **Ask what the consumer is before sizing an interface.** `0002` assumed a renderer because rendering is what an engine boundary is usually *for*. `0001` had already established the opposite — ~60% data-dense UI, and Godot selected **for** the drill-down — and the two documents never met. Same shape as `0034`'s finding that the field taxonomy was sorted by subject when the answer was geometry: **the sorting question is usually one level up from where the argument is happening.**
- **A boundary has a direction, and the guarded direction is rarely the busy one.** One CI check enforced engine → simulation, where nothing travels. Simulation state is shaped by UI requirements constantly and deliberately (`02 §9`), and *that* is where a leak would be invisible — not as an import, but as a method returning something already formatted for a panel.
- **A decision can be welded to another decision, not merely a constant to a decision.** `0034` found a *number* governed by whichever of its two roles was louder. `0001` is the same failure at document scale: one title, three decisions, one argument — and the two that rode along were invisible for exactly as long as the loud one kept being right. **The State Hash rule separates decisions as well as constants**, and it should be run over every ADR title that contains an *and*.
- **"Settle it with a number" is only honest when the measurement is cheaper than the decision.** Proposing a language bake-off of S0 meant building the core twice, which costs more than the question is worth — at which point *measure it* becomes deferral wearing a spike's clothing, and the decision gets made by default by whoever writes the first file. The fix was to find the *cheap* measurement: **don't benchmark the simulation, benchmark the shapes the simulation makes.** Six kernels, a few hundred lines, no city in it.
- **A tripwire beats a gate when the expected answer is "fine."** S4 is written once, in the incumbent language, and the second implementation happens only in the branch where it changes the answer. Same move as `0034` refusing to build machinery for tensions that dissolved under arithmetic.
- **When a discipline bans most of a language's idioms, check whether the bans are about the language.** Six of seven were not — they are determinism and cache-layout constraints that hold in every candidate. **The style is what the problem looks like, not what the language costs.** The genuine residual was narrower and worth naming: Rust makes *no GC* structural where C# makes it a maintained lint — against which C# makes the lint itself cheap to write, which is the trade actually being made.
- **The arithmetic that sizes one question routinely finds a bigger one.** Sizing the language found ledger #29 — an 8–15 ms per-Tick memcpy that is language-independent, that **cancels the Event Wheel**, and that is an instance of the exact Factorio failure `05 §6` quotes as a warning. Second session running that the estimate mattered more than the thing it was estimating.
- **Audit the derived structures, not the declared ones.** `05 §3` said the wait list and the Parking Shed *"must be named or they will be discovered."* They were named; their **representation** was not, and the representation is where the entire GC risk lived. **Naming a structure is not specifying it.**

---

## Readiness — what actually gates a first prototype

Established session eight by mapping every ungrilled item onto `06-roadmap`'s milestones. **The result is far better than the coverage map suggests, because most of what is ungrilled does not gate anything you would build first.**

**Phase 1 — headless, no graphics. Six items, all engine-level, no content:**

| Milestone | Blocked by | State |
|---|---|---|
| **1** Tick + determinism harness | ~~`adr/0003`~~, ~~`02 §8`~~, ~~`02 §10`~~ | 🟢 **buildable now** — hash function normative, `purpose_tag` specified, save/hash declaration settled, invariant tiers settled |
| **2** Typed tables and handles | `0004`, `0037`, `05 §3` | 🟢 **buildable now** |
| **3a** Rule engine — Bins and Rules | `02 §4` residue — fallback depth, cycle checking, predicate reads | 🟡 |
| **3b** Rule engine — hot reload | **`adr/0015`** | 🔴 and the roadmap says *"not optional, must not slip"* |
| **3c** Map Layers and Zone Rules | `0034`, `02 §2.4–2.5` | 🟢 **buildable now** |
| **4** Event Wheel | **`02 §7`**, **`adr/0006`** | 🔴 |

**Phase 2's wall is one large item, not many small ones:** `03 §5`, the traffic model — still the most detailed unargued design in the project, now carrying transit vehicles under an unset Microscopic Cap. Plus `adr/0005`, `0007`, `0008`, `0009`, `0012`, `0016`, `05 §7`'s format half, and **S2**, which argument cannot close.

**What must *not* be grilled yet**, because they are playtest questions in design-question clothing: health (#26), recreation (#27), Service variants (#28), car ownership (#3), private capital (#7), and `01-player §1/§3/§4`. `01-player §4`'s governability problem especially — *268 km² of individually-placed service Buildings* — is not answerable by argument. Somebody has to try placing them.

**What can be written today, with zero design risk:**

| | Unblocked by |
|---|---|
| **Solution scaffolding** — four projects, CI, both `adr/0002` boundary lints, the `0036` unmanaged-struct analyser (**built in slice 3**, in a fifth project that is a build-time input rather than part of the runtime four) | `0001`, `0002`, `0036` |
| **S4**, the kernel benchmark | nothing — it never had a blocker |
| **The fixed-point library** — mul/div/lerp, tabulated `exp`/`log`, `checked` internally | `adr/0003`'s transcendental and overflow policies, session eight |
| **Typed quantities** — `Money`, `Ticks`, `Tiles`, `Ratio` | `adr/0003`, `05 §3`. **The item most expensive to retrofit**: it touches every arithmetic site in the core |
| **Milestone 2** — typed tables, generational handles, the single-buffer rule | `0004`, `0037`, `05 §3` |
| **Milestone 3c** — Map Layer Cells, integer convolution, staggered schedule | `0034`, `02 §2.4–2.5` |

Note this reorders the roadmap slightly — tables before the hash. Low risk: tables do not encode determinism rules; the RNG and the State Hash do.

**The sequence:**

1. **Start S4 now.** Zero blockers, a few hundred lines, and it settles `adr/0036`'s K6 trigger before a line of core exists. Its **K1 now carries a `checked`/`unchecked` pair**, which is the last unmeasured claim in `adr/0003`.
2. **Grill `02 §8` + `02 §10` with `adr/0003`'s residue as one thread** — they are one subject, and together they *are* milestone 1.
3. Then `02 §7` + `adr/0006`; then `02 §4` residue + `adr/0015`. **That completes the Phase 1 gate.**
4. **Then stop grilling and build Phase 1.** Do not open Phase 2 content until S0 has run.

---

## Session nine — the roadmap, and what a planning document may assert

**Ran out of order, and the out-of-order run is the finding.** Session nine was booked for `02 §7` + `adr/0006`; it opened on `06-roadmap.md` instead, which the board had scheduled **last** as session K on the reasoning that *"A–J move what it sequences."* That reasoning binds the **ordering** and nothing else. Correcting claims that a settled decision falsifies depends on no other session, and it ran in one sitting.

So **K was two debts filed as one** — the second confirmed instance of the shape `adr/0003`'s owed validation had, and the first was found the same way: by accident, while doing something else. The audit `0002` asked for has not been carried out and now has a diagnostic to apply rather than a suspicion: **for each 🔴 row, ask what the gate's stated reason does not cover, and check whether that remainder is runnable today.** `06`'s gate said *ordering*; more than half of what it blocked was not ordering.

**What the sweep found.** Essentially none of `06`'s prose was original — the Simulator Effect restated `00-vision`, the Evidence triple restated `02 §9`, Citybound's yak-shave restated `adr/0018`, the Cities: Skylines 2 renderer story restated both `05 §11` and `00-vision`. And **eleven false claims, every one a copy whose original had moved**: `rate` after `adr/0033` replaced polling with subscription; *"travel time `distance / speed`"* against a tolerance test demanding a VDF no milestone built; *"invariant assertions run in debug builds"*, which `02 §10` calls backwards and which the shipped code does not do; *"Public transit. Not in scope, and possibly never"* against `adr/0029`'s *"Transit ships"*, citing a `deferred.md` entry that no longer exists; a Phase 1 project list two projects short; an `adr/0001` exit criterion offering to revisit a language decision `adr/0036` had taken away.

**Nobody was careless, and that is the point.** Each original moved in a session that did its job. What no session reliably does is walk every *other* document hunting uncited paraphrases of the claim just changed. The remedy is therefore structural, not diligent — **`adr/0042`**, the ADR this session produced: a planning document asserts only project facts, and every claim about what the simulation does is a citation. A row that reads *"5c — mechanism in `03 §5`"* cannot go stale, because it asserts nothing that can be false. Same move as `adr/0003`'s per-field declaration: not *remember to hash every field*, but *declaring the field allocates it*.

**What `06` is now.** Its warrant narrows to the phase model, its four rules, and the risk field. The contents column is gone — a milestone row is a name and the risk it retires — which removed ten of the eleven false claims by deletion rather than correction. Phase 0/1 order points at `0003`, live status at the board, mechanism at the design documents.

**Three findings beyond the corrections:**

- **Seventeen mechanisms have no milestone anywhere** — conserved Money, Hinterlands and arrival through the Gate, Settlements, Office and the labour system, Density, Services-by-Trips, Crime, the nine Resources, Upkeep and wear, Policy and the entire Sweep Rule family, transit, Taste, terraforming, Sealing, Water Bodies, the point-of-use queries, Traveller volume attribution. `06` now lists them without placing them, since placing them is K2's job. Consequently Phase 2's closing line — *"the city is alive"* — was struck and replaced by what those ten milestones would actually produce: **a transport and housing simulation with no money in it, nobody employed, and no way for anyone to arrive.**
- **Phase 3 is blocked on an absence, not a choice.** The board read *"unplanned by design, and stays that way"*. The truth is that rendering has never been designed and has no document to argue — and `adr/0002`, the interface it would build on, was **re-argued to serve inspection** on the finding that it had *"assumed a renderer because rendering is what an engine boundary is usually for."* New session **L**: write a presentation design, then grill it. It is the first session on the board that is not a grilling of an existing document. This also gives **S1 and S3 their job back** — they are L's empirical inputs, not a referendum on Godot.
- **One assertion was kept and labelled rather than struck.** *3b must not slip behind 3c* is unargued, **circular** — `adr/0015` grounds itself by citing `06` straight back, so the two are one claim counted twice — and **already violated**, since slice 6 is 3c's Layers half. Deleting it silently would repeal a live constraint other documents act on; K1 has no mandate to repeal. It stands with its provenance named, owed to session **A**.

**K2 — Phase 2's ordering — remains, and stays last.** Its acceptance test is now checkable rather than a judgement: it is done when every row is a name, a risk and a citation.

---

## Session B — `02 §4` residue, and the gate it moved rather than cleared

**Run beside slice 6, against the ordering claim that it should run *before* `adr/0015`. The session closed its four items and refuted its own premise:** `adr/0015` now gates slice 7 as well as slice 8, because the half of `02 §4` that dissolved turned out to dissolve *into* it.

Settled, recorded in [`adr/0045`](../docs/adr/0045-a-fallback-chain-is-a-source-ladder-over-one-bin.md), `02 §4.1` and `CONTEXT`:

| Decision | Record |
|---|---|
| **A fallback chain is a source ladder over one Bin.** Every link relieves the Bin the head failed on — refill if short, drain if a full output | `adr/0045`, `02 §4.1` |
| Therefore **a failed chain subscribes once, at its head**, and depth costs no subscriptions | `adr/0045`, `CONTEXT` → Bin Rule |
| The substitution this economy has is **source** substitution. `04 §1`'s Goods table is linear, one input per Good, so **input substitution has no instance** — it was imported from the genre | `adr/0045`, `04 §1` |
| An asynchronous rescue declares `fills = { scope, resource }`. Without it `request_shipment` — the corpus's own third link — fails its own well-formedness rule | `adr/0045`, `02 §4.1` |
| **Refused at load, never warned.** The `on_fail` graph is static, so this and the cycle check are one walk on `adr/0015`'s error surface | `adr/0045`, `adr/0015` |
| **A reporting terminal is not a Rule that succeeds.** `mark_input_starved` succeeding re-arms the head on `rate` — `adr/0033`'s polling defect, reproduced by the subscription model's own worked example, through the link nobody checks | `adr/0045`, `02 §4.1` |
| **The Readout bound is inverted.** `02 §9` is an obligation to *expand* aggregates and contains no enumeration, so *"a Rule may read what the player can see"* pointed at a non-set — and made slice 7 depend on a presentation design that does not exist | `02 §4.1`, `CONTEXT` → Readout |
| **Every Readout is inspectable; the converse does not hold.** Declaring a scalar a Readout is a decision to let Rules *act* on it, not to *show* it. `02 §2.5`'s test — *does a Rule read it, or is it only displayed?* — is the discriminator, third application after `adr/0032` and `adr/0034` | `CONTEXT` → Readout |
| **Apply count is authored per Rule**, greedy or fixed, with `min = max` as the fixed spelling. `adr/0035`'s Upkeep must never draw more because the treasury is full, so greed cannot be a property of the engine | `02 §4.1`, `CONTEXT` → Bin Rule |
| Either `{min, max}` or `derived`, never both: **greed handles what is consumed, derived handles what is consulted.** Fertility is why derived counts survive on Bin Rules | `02 §4.1` |
| **The cost driver under subscription is shortage *churn***, not depth and not brokenness. A chain is walked once on entry into shortage. Sharpens `adr/0033` from *most expensive when most broken* to *most expensive when most unstable* | `adr/0045`, `adr/0033` |

**Two process findings, both recurrences.**

- **A draft of `adr/0045` published a depth cap of 5**, derived from the length of the source ladder and dressed as a refuting number. That is R3's tripwire defect — *a wire whose denominator is a guess fires on the guess* — and the rule that catches it was already written in `plans/0010`. **Citing a rule is not running it**, which is `adr/0044`'s closing finding arriving one ADR later.
- **The session's own premise did not survive it.** The claim was *`02 §4` before `adr/0015`, because `02 §4` unlocks slice 7*. It does — and so does `adr/0015`, which the session discovered only by settling `02 §4`. **A gate's stated reason covering only part of what it blocks** is the diagnostic the board already carries from session nine; this is its third instance, and the first where the *reason moved* rather than the work being split.

**Left open, and routed out:** **labour as an input Bin against `adr/0026`'s Household↔Business jobs**, which this session made heavier by folding experience into that Bin. `04 §7` is stale twice over and owns it. **An economy session, not `02 §4`'s.**

---

## Resume here — `adr/0015`, and the two refusals it inherited

**Open on `adr/0015` (hot reload, milestone 3b), then `02 §7` + `adr/0006` (the Event Wheel, milestone 4).** `02 §4` residue is **closed** — see the session above — and the order between the two reversed as a result: `adr/0015` now carries the **`on_fail` cycle check**, the **`fills` check** and the **TOML dependency exception**, so it gates slice **7** as well as slice 8. It is no longer *"never grilled at all"*; it is a session with a concrete first item and a stated error surface to specify.

`06`'s *3b must not slip behind 3c* is still unargued and still circular, and is still owed an answer here — but it is no longer the reason to hold the session.

~~Session nine opens on `02 §8` and `02 §10` with `adr/0003`'s residue~~ — **all closed session eight. Milestone 1 is buildable**, along with the scaffolding, S4, the fixed-point library, typed quantities, milestone 2 and milestone 3c. **The most useful thing that can happen before session nine is code.**

**Superseded brief, retained for its content:** This reorders session eight's own plan, and deliberately: the readiness review above found that `05 §6` threading gates milestones 10 and 11, not 1–4, so it is **Phase 2 work and no longer the front of the queue.** Phase 1 is entirely single-threaded and headless.

**What `adr/0003` left open when it was interrupted:**

- **`02 §8`'s rule list** — never grilled, and it is the enforceable form of everything `0003` argues.
- ~~**Q16.16's scope and the overflow policy**~~ — **CLOSED same session**, see the table above. Wide integers, Q16.16 for positions only, fixed×fixed on **dimensionless ratios**, `checked` scoped to the fixed-point library, typed quantities making the rule structural, and the **range sizing deferred to `03 §5`** because IDM and the VDF are its only sites.
- **`02 §10`'s testing strategy** — never grilled, and milestone 1's entire deliverable is a test harness. **It now also owns `adr/0003`'s conservation invariants**, which are the money-overflow detector and must run in the headless suite where millions of Ticks elapse.
- **`adr/0031`'s unbounded-Bin comparison.** Flagged there as *"a determinism hazard"* and never resolved. Under the new policy it is an i64 Money Bin, which cannot overflow — but the *comparison* question (what "full" means for a Bin with no capacity) is a Rule-engine question and is still open.

**Then, before anything else: build S4.** It has no blockers and it is the cheapest information the project can buy.

**Deferred to Phase 2, carried from session eight:** `05 §6` threading — which thread runs `step()`; who owns and publishes the **transform history** (`adr/0037` left this downstream of the answer); whether hot-path results need **generation tagging** (`adr/0002`'s rules table leaves that row open); and whether **cold queries** drain from a queue or are called between Ticks. Also in §6: the `adr/0033` wait-list check, the `02 §1.1`-versus-`§6` parallelism contradiction (the phase table states *permission*, §6 states *implementation*, and neither says so), **Phase 4 Move being marked parallel while §6 never mentions it**, and `adr/0034`'s point-of-use noise queries having moved a spatial search into the Decide phase.

The open question as it stood:

**Q1 — does the simulation own a thread, or run inside the host's frame loop?** §6 never says. The recommendation on the table is *the simulation owns a thread, and `threads=1` means it runs on the caller's thread rather than meaning no sim thread* — because `02 §1.2` forbids Tick skipping, so a saturated sim on the main thread takes the camera down with it, which is the worst available presentation of the failure `01-player §6` wants legible.

**What `adr/0037` changed about it.** The two consumers that motivated the question are now served by named channels rather than by a readable Past — the renderer by a transform history, the saver by a copy taken at save time — so the question narrows to **who owns those channels and who publishes them**. Three specific things fall out of the answer:

- whether the **transform history** lives in the library (which `visible_agents`' `alpha` parameter currently implies) or in the shell;
- whether **hot-path results need generation tagging** — `adr/0002`'s rules table leaves that row explicitly open, and it is a threading question, not a hot/cold one;
- whether **cold queries serviced at a Tick boundary** means a queue drained during a serial phase (if the sim owns a thread) or a direct call between Ticks (if it does not).

### Then, in order

1. **The rest of `05 §6`** — the `adr/0033` wait-list check (new mutable state, apparently confined to the serial Settle phase, but subscription-on-failure may occur in parallel Phase 2 and that is unstated); the `02 §1.1`-versus-`§6` parallelism contradiction (the phase table states *permission*, §6 states *implementation*, and neither says so); **Phase 4 Move is marked parallel and §6 never mentions it**, though a Vehicle crossing a Junction writes another Lane's queue, which is a cross-partition write; and `adr/0034`'s point-of-use noise queries, which moved a spatial search into the Decide phase.
2. **`05 §2` sim/render boundary and open question 4, the snapshot format.** Now sharpened: `visible_agents` is a cross-thread query under the Q1 recommendation, so the snapshot format and the buffer-lifetime protocol are one question.
3. **`05 §1`, §7 remainder, §8, §10.** §7's Ruleset-versioning half is closed; the format and migration half is not.
4. **`02 §4` residue** — fallback chain depth and cycle checking, and whether `mean_workforce_experience` is a legitimate Building Readout.
5. **Labour as an input Bin**, which collides with `adr/0026`. `04 §7` (Jobs) is stale twice over.

### Standing debts, carried

- ~~**S4** (new, and it runs first)~~ — **RUN, and no tripwire row fired.** Tasks 1–10 are complete and recorded in [`docs/spike-results.md`](../docs/spike-results.md); task 11 (deleting the harness) is deliberately held pending the XMP re-sweep. What it handed back is below, under *From slice 1, running S4 tasks 3–10*. **`S0` and `S2` remain**, and S2 also owns Chunk size.
- ~~**`adr/0036`'s owed enumeration**~~ — **CLOSED same session by `adr/0002`.** Hot/cold *is* the exception rule: the hot path allocates nothing and holds no references, the cold path may do both because it runs on a click. Both candidates were on one axis, which is `adr/0031`'s test for having found the abstraction.
- ~~**`adr/0002` has never been grilled**~~ and ~~**`adr/0004` + ledger #29**~~ — **both CLOSED same session.** **`adr/0003` (determinism) is now the highest-value 🔴**, and it got more load-bearing rather than less: `0036` rests on its integer-semantics argument, and `0037` made *replay reconstructs any later state* the entire mechanism of crash forensics. It is the one ADR the project cannot route around.
- ~~**Ledger #29b**~~ — **answered for Phase 1 in slice 4: rows never move, and the Chunk partition is a separate index.** Structural rather than intended — `Rows` has no compaction and no relocation. Still open as a *measurement*: S0 will exercise it, and **`05 §5` role 3 still leans on the other answer** and needs a wording pass.
- **Audit the corpus for other unratified numbers**, and — new this session — **for decisions welded inside a single ADR title**, and **for revisit triggers that were already satisfied on the day they were written.** `0002`'s was; it is unlikely to be the only one.
- **`01-player §4` has never been grilled**, and 268 km² of individually-placed service Buildings is a governability problem with no answer.
- ~~**`06-roadmap` must be re-derived**, with S4, S0 and S2 at the front.~~ **PARTIALLY DISCHARGED, in two steps.** Phase 0 and Phase 1 are re-derived in [`0003-build-plan.md`](0003-build-plan.md), with S4 at the front and S2 flagged as the top risk on its own track — and `06` now carries all five spikes rather than three, since the *"before committing to Godot"* framing is spent. **Session nine then discharged the correctness half** (`adr/0042`): every claim a settled decision falsified is struck, and `06` no longer asserts mechanism at all. **What remains is Phase 2's ordering — K2 — and it stays last**, because re-deriving it before `03 §5` is grilled would be re-writing it twice. **Phase 3 is a different problem entirely**: it is not un-re-derived, it is undesigned, and session **L** owes the design before any ordering exists to derive.

### Unratified numbers and absent classifications, found while decomposing Phase 1

Rule 6 of [`0003`](0003-build-plan.md) applied to the planning itself. **Building will generate these faster than arguing did**, and each is recorded here on the day it was chosen rather than after it has been repeated until it reads as settled.

- ~~**The tabulated `exp`/`log` table resolution has never been stated.**~~ **CLOSED, in [`adr/0038`](../docs/adr/0038-the-transcendental-tables-are-sized-by-the-representation.md): 256 entries per table, rounded linear interpolation, base-2 range reduction.** The number falls out of a stopping rule rather than a preference — **the table must not be the thing limiting the answer**, and at 256 entries it contributes 0.12 ULP of a ~1 ULP total while Q16.16's own rounding supplies the rest. At 128 the table is still a term in the answer (0.72 ULP on `log2`); at 512 it pays a kilobyte to shrink something already an order below the floor. **The precision demand turned out to be five orders weaker than a numerics instinct suggests**, because `02 §5.4` scales utilities so meaningful differences are 1–3 units and only differences matter. Errors also run in the safe direction: quantisation perturbs utilities, which is equivalent to *lowering* `μ`, away from the stampede limit — asserted by a test that the tabulated softmax is never sharper than a double-precision oracle.
- **`adr/0003`'s owed validation is half discharged, and the other half was never blocked on `adr/0005` in the way it read.** The ADR required the resolution be validated against `adr/0005`'s herding behaviour, which is 🔴, and that had been carried as a single blocked debt. It is two: the *does the city feel herdy* half genuinely needs a running choice model and remains owed, but the *does the table change the answer* half needs no city at all — run `02 §5.4`'s softmax through the committed table and through a double-precision oracle over candidate sets including exact ties and near-ties, and compare selection probabilities. Worst divergence is **below 0.001**. **That test was available from the day `adr/0003` was written**, and the debt sat undischarged because it was recorded as one item rather than two. *Worth auditing the other 🔴-blocked debts for the same shape — a validation that is partly runnable now, filed behind a grilling session it does not actually need.*
- **The choice model has a hard horizon at ~11.1 utility units, and `μ` moves it. Nobody had noticed the coupling.** Q16.16's smallest positive value is `1/65536`, so `exp` underflows to *exactly zero* below `-11.09` — a candidate that far below the best is **impossible rather than unlikely**. That is defensible on its own, but the argument to `exp` is `μ·V`, so **doubling `μ` halves the horizon to 5.55 units.** `02 §5.4` calls `μ` *a free design knob* and suggests exposing it as a difficulty or realism setting; it is not only a sharpness knob, it also decides where options stop existing, and the two effects compound in the same direction. Recorded in `adr/0038` as the consequence to argue with. **A finer table does not move it** — it is a property of representing probabilities in fixed point at all, and only a wider representation would.
- **Map Layer diffusion cadence is classified as tuning and probably is not.** `02 §1.2` lists it in the tuning column, but cadence decides when a source becomes visible to a Rule reading the Cell, so two cadences produce two cities — a **design change** under `05 §4`. **Same welding failure `adr/0034` found in Chunk size, one document later**, which is the second demonstration that the hash rule only works if somebody runs it against each number *by name*.
- **The industrial-pollution kernel radius and shape are unstated.** `02 §2.4` grounds the range in reality (1–10 km plumes) and names no kernel. *Author in domain units* applies to the machinery, not only to the balance constants.
- **The CI lint count disagrees across three documents** — `05 §4` says seven, `adr/0036` says six and calls itself the sixth, this file calls it the seventh. One rule, three counts. Cosmetic, and it is how a checklist stops being checkable.
- **Spike results had no home.** `06` says *record them; delete the code* and names no file. Created as [`docs/spike-results.md`](../docs/spike-results.md).
- ~~**Ledger #29b needs a Phase 1 working answer**~~ — **taken in slice 4: rows never move, and the Chunk partition is a separate index.** `05 §5` role 3 leans on the other answer and S0 will measure it whether it means to or not.
- **Chunk size is still unmeasured** and still on the *cannot be retrofitted* list. Phase 1 proceeds at **Chunk = Cell**, provisional. ***No longer pending S2*** — `adr/0040` moved the pathfinding role off the Chunk, so what S2 measures is the cluster and Phase 1's provisional pin no longer waits on a routing spike.

**From slice 1, running S4 task 1** — the first entries generated by building rather than by planning,
which is what rule 6 predicted would happen.

- **The S4 measurement configuration is a chosen constant and nobody ratified it.** `performance`
  governor, turbo enabled, one hardware thread pinned with its SMT sibling idle, on DIMMs running at
  **2133 MT/s because XMP is off** though they are rated for 3200 and the memory controller is
  specified to 2666. It was chosen on evidence — the sweep in
  [`spike-results.md`](../docs/spike-results.md) shows `performance` has the tightest tail and turbo
  the highest median — but **every K0–K6 figure divides by it**, so it belongs here rather than in a
  commit message. A re-sweep with XMP enabled is owed; until it lands the DRAM-bound figures describe
  this configuration and not this machine.
- **The threading payoff for memory-bound work is now measured on two machines, and they disagree —
  which is the finding.** `05 §6`'s Factorio rule is **confirmed in its direction and refuted in its
  magnitude.** Read-only streaming gains **1.83×** on a six-core desktop and **3.75×** on an M4 Pro;
  read-write streaming gains **1.0×** on the desktop and **1.87×** on the M4 Pro. The mechanism is
  visible in the single-core share of the memory ceiling — 45% on the desktop against 25% on the
  M4 Pro — so a bandwidth-starved machine has nothing left for a second core and a generous one has
  plenty. **Had only the desktop been measured, the corpus would have recorded "memory-bound work
  does not parallelise" as a fact about the design when it is a fact about a DDR4-2133 desktop.**
  Read this before `05 §6` is opened in Phase 2: it settles that **the parallelisation decision is a
  runtime one made from measurement, not a rule written into source**, which is exactly the
  host-adaptive case the next entry admits. The crossover — **~55 cycles per 64-byte line** on the
  desktop against **~23 per 128-byte line** on the M4 Pro, a factor of five per byte — is two rules
  of thumb, neither ratified. Random access is still unmeasured until K2, and will be worse than
  either curve on both machines.
- **Host-adaptive parameters have no policy, and the first one arrives in slice 4.** Two machines
  differing (64-byte cache lines against 128, 34 GB/s against several times that) makes *let the code
  fit the machine* an obvious instinct, and it is half right in a way that hides the dangerous half.
  `05 §4`'s test already decides it: **hash-preserving adaptation needs no permission, hash-bearing
  adaptation is two different cities from one Input Log** — it breaks replay, breaks save
  portability across machines, and breaks `adr/0037`'s crash forensics, which rest entirely on
  *replay reconstructs any later state*. The corpus has already caught this twice, in Chunk size
  (`adr/0034`) and in Map Layer diffusion cadence, which is **still misfiled as tuning in `02 §1.2`**
  — so the failure mode is demonstrated, not hypothetical. **Recommended policy, owed to `05 §4` and
  `05 §6`:**
  - **Admitted so far: thread count, and whether a phase is parallelised at all.** Both sit inside
    the guarantee invariant 4 already makes. Nothing else has earned it.
  - **Every host-adaptive parameter is admitted through a hash-equivalence test** — one Input Log,
    both extremes of the knob, identical State Hash sequences, in CI. This is invariant 4 generalised
    from threads to any knob, and it makes the unsafe version fail on the day it is written rather
    than a year later when a save will not load.
  - **Cache line size is not adaptive; it is two constants**, and the second machine confirmed the
    64/128 split rather than leaving it assumed. Pack table rows against **64** — a layout dense at
    64 is correct at 128, two rows to a line and never a straddle — and pad anything two threads
    write to at **128** unconditionally, because that is where false sharing actually lives and the
    wasted bytes are a rounding error. Slice 4's row schema is the first consumer.
  - **A layout tuned to "fit in L2" is worth nothing on one of the two machines.** The M4 Pro streams
    at 54–64 GB/s at every size from 512 KiB to 256 MiB — its DRAM is fractionally *faster* than its
    L2, and only L1 residency buys anything — while the desktop falls from 31.8 GB/s to 12.8 across
    the same range. Sizing a structure to a cache level is therefore a machine-specific optimisation
    wearing the clothes of a layout decision, and slice 4 should not take it.
  - **Build none of it yet.** Sequential code already reaches 78% of the measured ceiling, so there
    is no headroom for adaptive machinery to recover, and building it before the first table exists
    is the failure `adr/0018` names by example.

**From slice 1, running S4 task 2** — the row schema and the row counts. Full working in
[`spike-results.md`](../docs/spike-results.md).

- **A field lives at the level at which it can differ — SETTLED, by generalising a rule already in
  the corpus.** `CONTEXT.md` on Buildings — *"If a field would differ between two Occupants, it lives
  on the Occupant"* — decides the Citizen-versus-Household contradiction that `03 §4` invariant 1 and
  `CONTEXT.md`'s Household entry had left open, and it was a 1M-against-400k-row fork. Money, goods,
  Needs, Provider List, Life Stage, Taste, car ownership and Schooling are **Household**; health, age,
  Skill Tier, experience, employment, workplace and `next_event_tick` are **Citizen**. Three edits
  owed and none of them design changes: `03 §4` invariant 1 is mis-grouped rather than wrong (its own
  next clause shows the contrast it draws is against the *embodiment*, not the Household);
  `CONTEXT.md`'s Citizen entry lists `home`, which cannot differ between members and is therefore the
  Household's dwelling; and **`Unemployment` stops being a field**, becoming *"a Household where no
  contained Citizen holds a job"*. A cached bit for it, if a profile ever wants one, is
  `(derived AND rebuilt)` with a debug invariant against the walk — a stale bit is a Household that
  believes it is employed and never seeks work, which is silent and hash-bearing.
- **"40 bytes hot" was never one number, which is worse than being stale.** Recomputed, the Citizen
  row is **13 B touched per Tick** and **51 B in the wake working set** — 4× apart, and the 40-byte
  figure matches neither. The per-Tick figure is what the Event Wheel argument implies; the working
  set is what a woken Citizen costs. **K0 must report both**, because they size different things: the
  Wheel drain and the wake gather against the first, the world footprint and the save copy against
  the second. At 1M that is 13 MB against 51 MB, versus `03 §2.1`'s *"roughly 40 MB"*. Note also that
  structure-of-arrays removes per-row padding from the question entirely — a row costs the sum of its
  column widths.
- **Two chosen numbers with nothing behind them, and they are the largest assumptions in the count.**
  An **employment rate of ~50%** of population — the corpus contains no employment rate anywhere — and
  a **mean Trip duration of 240 Ticks**, half of the only figure the corpus gives, which `02 §1.2`
  defines as *cross-town* rather than typical. Every Trip, Leg and Vehicle count moves linearly with
  the second one; the in-flight figure ranges 37k–111k across a plausible band.
- **Trips per Day is ~1.9M, not ~400k, and `adr/0037`'s ~23,000 travellers is not a second source.**
  It is exactly `400k × 480 ÷ 8192` — the same unratified figure restated. The 400k reads as one Trip
  per Household per Day, i.e. the outbound commute with the journey home never counted. Derived from
  the generators instead — commutes, school, shopping, freight — the figure is ~1.9M/Day and **~56,000
  Trips in flight**, which sits between `adr/0037`'s 23k (too low, omits the return) and `adr/0019`'s
  12% ≈ 120k (too high, prices every journey as cross-town). **S2 must be sized against the derived
  figure**, because a routing design that passes at 400k/Day and fails at 1.9M is the exact failure
  that spike exists to catch. `adr/0037`'s *conclusion* survives — 5× more traveller writes is still
  trivial against the 150 MB it deleted — but its stated number does not.
- **~400k Households is corroborated for the first time, and its provenance is worth recording.** It
  appears in exactly two places — `adr/0003`'s overflow-headroom sum and `adr/0011`'s decision-volume
  sum — and **both are arguments robust to being wrong by 2×**, which is why neither derives it and
  neither states a household size. It implies 2.5, close to real-world average household size, so the
  innocent reading is a borrowed real figure. Equal stage durations against `adr/0011`'s *own*
  compositions give 2.8 and **~360,000 Households**, within 12% — so the ADR asserts one number in its
  Cost section and specifies a model implying another further down, and nobody has run the two
  against each other until now. **Also worth an audit rather than an assumption: 400,000 is already
  *the* number in this corpus** — it is Citybound's individually-simulated-car count in `adr/0007`
  (twice), `adr/0016`, `adr/0018` and `06`, and it is separately the Trips/Day figure. Three unrelated
  quantities, one number, none derived. Contamination is unproven and each has an innocent
  explanation, but this is the exact shape of the 10k incident, and `0002`'s own standing instruction
  is to audit for it.
- **`05`'s population derivation is circular and cannot be used as one.** `mature_density` at
  ~3,700/km² is an *output* of the 1M target — [`0002`](0002-open-questions.md) §1's column is headed
  *"1M implies"* — so feeding it back confirms nothing. `buildable_fraction ≈ 1.0` appears once, is
  never argued, and contradicts `adr/0021`'s mandatory water bodies and maximum buildable grade.
  `05` also folds roads and parks into the density anchor, so either the anchor is gross and the
  fraction must fall well below 1, or it is net and the anchor is wrong. **Only `map_area` is
  ratified.** The formula is a consistency check, not a derivation.
- **Buildings, Businesses, Lots and Segments have no derivable counts** — ~150k, ~50k, ~225k and ~30k
  are placeholders resting on a Households-per-Building figure, a workers-per-Business figure and a
  road-density figure, none of which exist anywhere in the corpus.
- **The Lane and Vehicle tables are sized by the Microscopic Cap, which is unset**, so K0 must take it
  as a parameter and report footprint as a function of it. That also makes K0 the natural place to
  inform what the Cap should be, which is currently a fixed world constant with no value and no
  derivation.

**From slice 1, running S4 tasks 3–10** — the kernels themselves. Full working in
[`docs/spike-results.md`](../docs/spike-results.md), which is corpus and survives the spike's deletion.
**S4 has run and no tripwire row fired**, so `adr/0036`'s language decision now stands on measurement
rather than on argument alone. What follows is what the running produced *beside* the verdict, which is
the part a spike is actually for.

**Corpus edits owed, each with the kernel that produced it:**

- **`adr/0003` calls `checked` inside the fixed-point library cheap, and names it the only claim there
  without arithmetic behind it.** K1 supplied the arithmetic: **27%** on a scan that does nothing but the
  multiply, which is the worst case. Whether 27% counts as "cheap" is a judgement the ADR should now make
  explicitly rather than continue to inherit. **The second machine makes that judgement cheaper to take
  once rather than per target: it is 29% on arm64 against 27% on x86-64**, so the overflow policy's price
  is a property of the arithmetic and not of the ISA.
- **The `checked`-block-scope footgun needs a sentence wherever the overflow policy is stated.** `checked`
  is a *block* in C#, so scoping it to a loop body that indexes a raw pointer silently prices the address
  arithmetic as well — worth **40 points on top of the 27** on x86-64 and **95 points on arm64**, where
  the same footgun costs 2.28× against unchecked rather than 1.67×. The fix is to walk pointers or use
  spans. K1 measured both forms specifically so that the two costs could not be recorded as one number.
- **`unsafe` earns nothing on x86-64 and *costs* 11% on arm64.** Bounds checks elide completely on both,
  so `Borough.Core`'s table access does not need `unsafe` — and on one of the two targets taking it
  anyway is a measurable regression rather than a wash. Slice 4 should write its scans over `Span<T>`.
- **`adr/0037`'s 8–15 ms band for the async save's copy is conditional — and the second machine changed
  what it is conditional *on*.** K3 on the desktop: one contiguous arena copies in **13.9 ms** and is
  inside the band; per-column allocation copies in **17.2 ms** and is outside it. **On the M4 Pro the
  same comparison is 3.077 ms against 3.105 ms — a 1% penalty rather than 24%**, because the non-temporal
  store threshold that causes it sits below 1 MiB there and between 1 and 8 MiB on the desktop, and the
  K0 schema's columns straddle one and clear the other. **So the 24% is a fact about a DDR4-2133 desktop,
  not about the design**, and quoting it as the cost of per-column allocation would be the exact error the
  two-machine rule exists to catch. **The constraint on slice 4 softens from a precondition to a strong
  default:** arena-allocate, because it makes the save's cost independent of where the host's threshold
  falls — *that* is the durable argument, and unlike the 24% it does not expire when the target hardware
  changes. A slice 4 that finds arena allocation genuinely expensive is no longer choosing between
  compliance and non-compliance; it is choosing to make the save cost host-dependent between 1% and 24%.
- **`adr/0037`'s band names no machine, and both M4 Pro figures fall *below* its 8 ms floor.** Same defect
  already recorded for the 15.6 ms Tick budget: a range meant as *acceptable* reads as *expected*, and a
  fast host simply beats it. The ADR should say which machine class 8–15 ms describes.
- **`05 §3` owns layout and should state which `bins[9]` shape it means.** K4: `WorldSchema`'s
  entry-interleaved form is the slowest of the three permitted layouts, and keys-then-values is never
  worse for the same 81 bytes — the same memory in a different order, so it costs nothing to adopt.
  **"Strictly better" was too strong**: the relayout is worth 5% on x86-64 and **0.1% on arm64**, inside
  the error bars. Adopt it because it is free and because the vector probe wants the keys contiguous —
  **the vector probe is the finding, worth 38–42% on both machines; the relayout is what makes it
  expressible.**
- **`05 §6` states no GC configuration and now has evidence for half of one.** ~~K6: server GC with
  background collection on.~~ **The second machine split this recommendation in two, and only one half is
  portable.**
  - **Background collection on is prohibition-grade, and firmer than the desktop alone showed.**
    `<ConcurrentGarbageCollection>false</...>` costs the shell **2.9–4.95×** at the tail across both
    machines, and on the M4 Pro it costs the *core* 1.57–2.36× as well, where the desktop recorded it as
    neutral. **There is no cell on either machine, in either arm, where off beats on.** It is precisely
    the knob a latency-conscious developer reaches for on the reasoning that background collection adds
    overhead, so `05 §6` should state it as a prohibition rather than a preference.
  - **Server versus workstation is a host setting, not a constant.** Its effect on the *unmanaged* arm
    reverses between machines — **2.8× worse** on the desktop, **5.1× better** on the M4 Pro — and the two
    hosts disagree about which cell minimises the managed arm's worst pause. `05 §6` should make it a
    startup configuration read from the host, and should not compile in a winner. See the pattern entry
    below.
- **`adr/0036`'s revisit trigger has been restated** from a p99.9 to a maximum and an over-budget count,
  because K6 showed the quantile cannot see the event the trigger exists to detect — the run whose worst
  iteration was 100.2 ms read 2.462 ms at p99.9, and in half the GC matrix p99.9 ranked the *rejected*
  design above the chosen one. **Recorded here as a pattern rather than only as an edit:** the standing
  debt above to audit for *revisit triggers already satisfied on the day they were written* now has a
  sibling — **audit for revisit triggers whose statistic cannot detect the thing they name.** A trigger
  that cannot fire is not protecting anything, and `0036`'s is unlikely to be the only one.
- **When a number reverses between hosts, the durable decision is the one that removes the host
  dependence — not the one that picks the winner on the machine to hand.** This has now happened twice
  in S4 and the second time is what makes it a rule. K3: the per-column copy penalty is 24% on the
  desktop and 1% on the M4 Pro, so the argument for arena allocation is that it makes the figure stop
  varying, not that it buys 24%. K6: server GC is 2.8× worse for the core on the desktop and 5.1× better
  on the M4 Pro, so `05 §6` gets a host-read startup setting rather than a compiled-in choice. **The
  standing instruction: before recording any S4 number as a decision, ask whether a configuration knob
  or a host could move it, and if so record the knob rather than the number.**
- **A configuration sweep must report the configuration the system actually adopted, not the one it was
  asked for.** K6's first sweep asked for four GC configurations and silently ran two, because
  `DOTNET_gcServer` overrides `runtimeconfig.json` and `DOTNET_gcConcurrent` does not. It was caught only
  because the report printed *effective* settings; had the label echoed the requested value it would have
  been recorded as four configurations agreeing closely — a tidy and completely false result that would
  have hidden the largest number in the matrix. **Same shape as the unratified-number failure this file
  keeps finding: a value never checked against what it claimed to describe.**
- **A capture must state its own provenance truthfully, and the M4 Pro capture did not.**
  `results/kernels-apple-m4-pro.md` asserts its denominator was *"measured in the same sitting"*; the two
  timestamps are 42 h 44 min apart. **Third instance of the pattern above**, and the most instructive,
  because the desktop capture got it right in the same words: it claims *"measured under the same
  **configuration**"* — 59 hours earlier, but pinned, at a set governor and a labelled DIMM rate, all
  re-established for the run. One file claims a controlled *configuration* and can show it; the other
  claims a shared *moment* and cannot, on the machine that has no controls to fall back on. **The lesson
  is not "co-measure" — it is that a provenance line is a claim like any other and gets checked like
  one.** A capture harness should emit the denominator's own timestamp beside the figure it divides by,
  so the gap is visible without anyone thinking to look for it.
- **A ratio against a hand-computed ideal is only a verdict while the ideal binds — and `plans/0004`'s
  tripwire is written entirely in those ratios.** K1 runs at 91% of the desktop's copy ceiling and 50% of
  the M4 Pro's, so the same loop is bandwidth-bound on one machine and compute-bound on the other, and its
  ratio-to-ideal degrades from **1.10× to 1.99× without the code changing**. On a fast enough host the
  footgun variant reaches 4.53× and would fire a wire it does not deserve to fire; on a slow enough host a
  genuinely bad kernel hides inside a bandwidth ceiling it cannot exceed. **Recorded as a pattern, and it
  is the third member of a family this file keeps finding** — beside *revisit triggers already satisfied
  when written* and *revisit triggers whose statistic cannot detect what they name*, now **thresholds
  whose meaning depends on an unstated machine.** A tripwire must name the machine class it applies to,
  as must `adr/0037`'s 8–15 ms band and the 15.6 ms Tick budget, neither of which does.

Carried and still owed, none of them from S4: `03 §4` invariant 1 wording pass; `CONTEXT.md`'s Citizen
entry still lists `home`, which task 2 moved to the Household; `CONTEXT.md` has no **Unemployment**
definition and wants one — *a Household where no Citizen holds a job*; and `CLAUDE.md` still calls Map
"open" though session six closed it at 4096².

**Two more unratified numbers, in the shape this section keeps finding:**

- **The mean wake interval is unratified, has a 32× range, and drives the Event Wheel's entire cost.** K5
  found bucket occupancy to be triangular rather than uniform, so an entity waking every M Ticks is
  drained 1/M of the time and the drain rate is **N/M per Tick, not N/8192** — 381 wakes per Tick at
  M = 4,096 and 6,094 at M = 256. The corpus has never fixed M. The Wheel's cost is linear in wakes and
  the wake rate is the only lever on it, so this single input decides whether the Wheel and its wake
  gather cost **0.11% of a Tick or 1.80%**.
- **The GC churn rate K6 assumed — one object in sixteen promoted — is a guess at what the shell, the UI
  and the per-frame snapshot allocate.** Nothing in the corpus states it. Without churn there is no
  collection and no pause whatever is held live, so this one number sets the scale of every K6 result
  **including the one that cleared `adr/0036`'s trigger**. **The second machine supplies an accidental
  sensitivity check and it is reassuring rather than conclusive**: churn is per *iteration*, so the M4
  Pro's 2.4× faster iteration ran the same matrix at **122–126 MB/s against the desktop's 44–52** —
  2.5× the pressure — and the unmanaged arm still recorded zero over-budget iterations. That is one
  factor of 2.5 in one direction, not a curve, and the number is still owed.

**Measurement owed, and it dies with the harness.** `spikes/S4.Kernels/` is deliberately **not** deleted
yet — task 11 is held — because the following need it and reconstructing it from git history would cost
more than keeping it:

- ~~**K0, and then K1/K2/K5, on the Apple M4 Pro.**~~ **DONE**, and it earned its cost — see
  [`spike-results.md`](../docs/spike-results.md), where every kernel now carries an *On the second
  machine* subsection. **Four conclusions turned out to be properties of the desktop rather than of the
  design** (the threading payoff, K2's array-of-structs advantage, K3's per-column copy penalty, and —
  once K6 ran there too — the sign of server GC's effect on the unmanaged arm), and one methodological
  defect surfaced only from the disagreement, recorded as a pattern below.
- **The XMP re-sweep — downgraded from the reason the deletion is held to a refinement.** These DIMMs are
  rated **3200 MT/s and are running at 2133** with XMP off. The deletion was held because `adr/0037`'s
  save-copy band was being judged against a machine not running to its own specification — but **the
  second machine has since answered the question the re-sweep was being held for**: the per-column
  penalty is a property of this box, and a re-sweep at 3200 would reach the same conclusion by a weaker
  route. What it would still buy is whether *this desktop* meets the band at its own rated speed, which
  has value the M4 Pro cannot supply because the desktop is the more representative target — a player's
  machine is x86 with DIMMs. The arithmetic predicts ~9.5 ms and ~11.7 ms at 1.5× the bandwidth, both
  inside the band; **that is a prediction and not a measurement.** Needs a reboot: `sudo
  tools/baseline-sweep.sh`, then `sudo tools/kernel-run.sh`. Labels carry the configured MT/s
  automatically, so it cannot overwrite the DDR-2133 results.
- ~~**K6 has never run on the M4 Pro and is the one kernel resting on a single machine.**~~ **DONE**,
  2026-08-04, and it was worth the eighty minutes: `adr/0036`'s trigger clears on both machines
  (**zero** over-budget unmanaged iterations in 6,062,762), the arm separation widens to **36.8×**, the
  quantile finding strengthens — but **the server GC recommendation `05 §6` was about to adopt reversed**.
  Two defects recorded from the capture: the invoking command dropped the redirection `tools/k6-run.sh`
  performs, so the reports were recovered from terminal scrollback rather than written to `results/`;
  and the harness's *"plus N MiB of managed objects"* line reports the whole managed heap rather than
  the ~71 MiB live graph, so it tracks allocation rate instead of the thing the arm varies.
- **Re-measure the M4 Pro baseline and re-derive every M4 *vs ideal* figure from it.**
  `results/kernels-apple-m4-pro.md` claims its denominator was *"measured in the same sitting"*; the
  timestamps are **42 h 44 min apart** (2026-08-03 00:42 against 2026-08-04 19:26), on the one machine
  with no governor control, no turbo switch and no thread pinning. **This is cheap to fix — a
  ten-second window, about a minute of wall clock** — and until it is fixed every ratio-to-ideal in the
  M4 subsections is provisional. **The conclusions are not**: they are within-process variant ratios and
  are immune to the denominator, which is why the fold-in survives the defect. Do it before the harness
  is deleted.
- **The two machines carry different defects and neither is the clean one — corrected 2026-08-04, having
  been recorded backwards first.** The **desktop was running other work during its captures**; the **M4
  Pro was quiet during its own**. The M4 Pro's defect is the stale denominator above; the desktop's is
  contention. `kernel-run.sh` pins the desktop to one physical core with the SMT sibling idle, so
  another process must land on that core to steal cycles — **but pinning is no protection against DRAM
  bandwidth contention, and K1, K2 and K3 are bandwidth-bound.** The desktop's absolutes and its
  ratios-to-ideal therefore carry an unquantified error bar; its within-process variant ratios, which is
  where the conclusions are, do not. Argued in full in [`spike-results`](../docs/spike-results.md) under
  *What the desktop's background load can and cannot have moved*. **The lesson is the filing error rather
  than the load:** the capture that got the caveat is the one that did not need it, because the caveat
  was attached from a memory of which concern was raised rather than from a record of which machine was
  measured. Neither harness records host load, and nothing in `results/` could have settled it.
- **K2's sparse-wake premise is confirmed on one machine only, and cannot be confirmed on the other as
  written.** The M4 Pro's 16 MiB cluster-shared L2 holds most of the 20 MB working set, so that run beats
  its own bandwidth ideal by 3× and measures cache rather than DRAM. Reproducing the desktop's finding on
  Apple Silicon needs a working set several times the L2 — a different kernel, and not worth building,
  since the claim is already established on the machine where it is harder to establish.
- **K6 under the canonical governor.** It is now the only kernel never captured under `performance`+turbo,
  and also the one where it matters least, since it measures pauses rather than bandwidth. Eighty minutes
  if it is ever worth closing properly.

**From planning S2** — [`0010-s2-routing.md`](0010-s2-routing.md), written the session slice 1 reported,
per [`0003`](0003-build-plan.md)'s instruction that S2 be planned the moment it does. Rule 6 applied to
the planning rather than to the running, which is where it caught four of these.

- ~~**`Segment` has no `CONTEXT.md` entry, and it gates S2.**~~ **CLOSED same session.** The word
  appeared twenty times in that file — Fidelity, Stress, the Audit, the Microscopic Cap, Promotion and
  Demotion, Traveller, the VDF, Upkeep — and was defined nowhere, while being the unit the Cap counts,
  the unit Fidelity attaches to, the unit the VDF is evaluated on and the unit `adr/0035` prices Upkeep
  against. **Now defined as the Road Graph edge** — one run of road between two adjacent nodes, carrying
  capacity, free-flow speed, mode mask, volume and Fidelity, and owning Lanes when Microscopic. **The
  entry recovers a meaning rather than choosing one**, and the arithmetic is what pins it: a Tile-length
  edge puts millions on a 4096² map and a run between authored Junctions puts almost none on a city that
  is mostly Streets, while K0's ~30,000 Segments at about four Lanes each is only consistent with the
  middle reading. The ~30,000 itself still rests on a road-density figure that exists nowhere, and
  remains S2's to replace.
- **Whether `volume / capacity` is per Segment or per direction is unsettled, and the Segment entry
  deliberately did not settle it.** Lanes are directional queues and a Segment carries about four of
  them, so **a Segment jammed inbound at the morning peak reads roughly half-loaded if the two
  directions are summed** — Stress would understate exactly where it matters and promotion to
  Microscopic would fire late, which is the failure `adr/0007` leans on *errors self-correct toward
  detection* to prevent. The VDF entry says *"one Segment's own `volume / capacity`"*, singular, which
  reads as per-Segment and is probably just imprecision rather than a decision. Real traffic engineering
  is per-direction. `0010` task R0 parameterises it; **it must not be assumed by whoever writes the
  first Segment row.**
- **The travel-time matrix refresh cadence is filed as tuning and is almost certainly hash-bearing.**
  `02 §1.2` lists accessibility refresh among the tunable knobs; but cadence decides when a changed
  travel time becomes visible to the choice loop, so two cadences produce two cities — **a design
  change under `05 §4`.** This is the **fifth instance of the welding failure** `adr/0034` found in
  Chunk size, after Map Layer diffusion cadence, and **the second one found by reading `02 §1.2`'s
  tuning column specifically** — which is now the highest-yield place in the corpus to audit, and
  should be audited in full rather than one entry at a time. Note the near miss: `05 §4` separately
  lists *"rebuilding the travel-time matrix rather than saving it"* as hash-preserving, which is a
  **different and correct** claim — a deterministic rebuild is hash-preserving, a variable cadence is
  not, and the two sit close enough to be mistaken for each other.
- **Routing has no stated share of the Tick budget, and S2 needs one to have a tripwire.** `0010`
  chooses **10% of 15.6 ms on the desktop**, against two anchors: S4 measured the Event Wheel and its
  wake gather at 1.80% at the most pessimistic wake rate, and `adr/0037` names the order of suspicion
  for a slow Tick as *the Microscopic Cap, routing, and the Sweep Rule schedule*. Defensible; **not
  derived**, and recorded here on the day it was chosen.

  **Grilling established that it *cannot* be ratified yet, rather than merely that nobody has** — and
  the figure is deliberately kept as a stated guess rather than replaced with false precision. **The
  denominator does not exist:** 15.6 ms is the whole Tick at 4×, which also contains Rules, Map Layers,
  the Sweep schedule and the Microscopic tier, and **only the Event Wheel has ever been measured** at
  1.80%. A share is a claim about a whole that is ~90% unpriced. *This is the denominator lesson a third
  time — S4 established that a denominator needs its machine and its moment; `0010` R0 adds that it
  needs its quality stated; this adds that it needs to exist.* **And the symptom is one the design
  elsewhere calls harmless:** `CONTEXT.md` → Speed makes tick rate *"purely a host concern"* and
  `03 §3.9` absorbs hardware limits by *a slow machine advancing fewer Ticks per second*, so blowing the
  budget breaks nothing and yields an identical city — the player simply does not get 4×. What the row
  actually protects is **the point at which the top speed setting stops being deliverable**, which bites
  under `01 §7`'s rule that a number must not contradict what the player is watching.

  **The response is under-argued as well as the threshold.** Nothing establishes that **map size** is
  the right lever for a routing cost rather than a coarser matrix cadence or a lower top speed. Falling
  back to 2048² changes the *world* in answer to a *pacing* symptom, in a design with an explicit home
  for pacing that is not the map. **S2 reports the absolute per-Tick figure at peak on the named machine
  regardless**, so the fallback can be decided on evidence once the Tick's other consumers are priced.
- **Does a Statistical Trip need a concrete path, or only an arrival time?** Asked because
  `CONTEXT.md` → Traveller defines a Statistical Traveller as *an origin, a destination, and an arrival
  Tick*, which reads as needing no path, while Stress needs a volume that something must attribute.
  **Grilling `0010` found the corpus has already answered it twice, differently, in the same
  document** — see the two items below, which supersede this one as the live question. `0010` R2 must
  not be written up until they resolve.
- ~~**`03 §3.3` and `03 §3.6` give a Trip two different routes and nothing reconciles them.**~~
  **CLOSED —
  [`adr/0041`](../docs/adr/0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md): the
  Traveller attributes volume, not the District pair.** Decided rather than measured, because the two
  schemes produce **different cities** and `05 §4`'s rule makes that a design decision. Three grounds:
  `03 §3.4`'s self-correcting circularity — *"the load-bearing assumption of the whole scheme"* — only
  closes if the Segments a Traveller uses are the Segments it raises the volume of, which aggregate
  attribution breaks and direct repairs by construction; `03 §3.3` confesses the aggregate scheme lags a
  backward-propagating jam and invented **force-promotion** purely to compensate, so a mechanism exists
  to patch a compression nobody priced; and under `02 §2.1` Districts are **player-adjustable**, so
  District-keyed attribution would let an organisational act change the State Hash. Cost is bounded and
  small — ~80,000 increment/decrement pairs per Tick into a 120 KB L2-resident array, which is S4's K2
  inner loop. **Owed: a joint rewrite of `03 §3.3`, `§3.4` and `§3.6`, and force-promotion must now
  stand on its own second argument or go.** `0010` R2 keeps the crossover as a *pricing* measurement and
  still decides the independent **path-source** axis.
- **The original framing of this, retained because it is how the question was found.** §3.3:
  *"`in_flight[origin_District][dest_District]` is incremented on departure and decremented on arrival,
  then distributed onto segments along **cached District-pair routes** each congestion cycle."* §3.6:
  *"A Traveller on a Statistical segment is animated from its trip — position interpolated **along its
  route**."* So volume is attributed along a route keyed by District pair, while the Traveller travels
  along a route of its own. **A Traveller therefore experiences congestion on the Segments its own
  route uses and contributes congestion to the Segments the District-pair route uses.** If the two
  disagree, `03 §3.4`'s self-correcting circularity — named there as *"the load-bearing assumption of
  the whole scheme"* — is broken at the joint, because the failure no longer feeds the detector: the
  detector is watching different Segments. **Note also that `adr/0007` is ambiguous on this and `03`
  resolved it in prose.** `adr/0007` says only *"incremented on departure, decremented on arrival"*,
  which reads equally well as Segment entry and exit; the District-pair indexing exists nowhere in an
  ADR. That is a decision living in prose, against `CLAUDE.md`'s own rule.
- **If Districts are player-drawn, `03 §3.3` makes a cosmetic act change the State Hash.**
  `CONTEXT.md` → District is *"either player-drawn or automatically derived"* and `06` leaves the
  choice open, blocking milestone 5c. But under §3.3 the District pair is the key of both the volume
  counter and the cached route, so **redrawing a boundary changes volume attribution → Stress →
  Fidelity → travel times → the city.** This is the same defect class as a host-tunable Microscopic
  Cap, which `03 §3.9` rejects in words that transfer unchanged: *"anything the host could vary must not
  be able to change an outcome."* Three resolutions: **(a)** Districts become automatic; **(b)** volume
  attribution keys on a partition the player cannot touch, and the District keeps its `CONTEXT.md` role
  as pooling, reporting and matrix granularity only; **(c)** volume attribution comes from the
  individual Traveller's own Segment sequence, collapsing the two routes into one and repairing §3.4 by
  construction. **(c) is the physically honest model and is the one to price first** — a Tick is ~10.5
  in-world seconds and a Segment is roughly a block, so a vehicle crosses about one Segment per Tick and
  the direct scheme costs on the order of one increment and one decrement per in-flight vehicle Leg per
  Tick into a ~30,000-entry array that fits in L2. **S4's K2 may already price it**, which would settle
  in arithmetic a question `03 §3.3` settled by assumption.
- **The router is handed Legs, not Trips, and every load figure in the corpus counts Trips.** At 2.5
  Legs per Trip the real ask is **~580 Leg routes per Tick — ~232 drive and ~464 walk** — so `0010`'s
  stated floor of *"~232 searches per Tick"* is out by 2.5× and the omitted part is the larger part. A
  walk Leg is not exempt just because `CONTEXT.md` calls it *"always Statistical"*: `distance / speed` is
  exact only over a **network** distance, which is a search, and whether a walk route *exists at all* is
  the whole of **Severance**. **A spike that never routes a walk cannot see the design's flagship
  emergent behaviour.** Corrected in `0010`; the walk and drive classes are now measured separately
  throughout, and walk routing is owed a failure-mode measurement — whether the router can distinguish
  *severed* from *merely far*, which are different Trip Fates and would be collapsed into one by any
  search-radius bound chosen for performance.
- ~~**Is the ~30,000-Segment figure inflated by `adr/0008`'s pedestrian layer?**~~ **CLOSED — no.**
  `spike-results` derives it as *"road density against 268 km²"*, and `03 §3.7` settles the shape:
  mode masking is *"an edge property, not a missing graph"*, so a footway is the same Segment with the
  foot bit set and the pedestrian network is a **subgraph** rather than an addition. Recorded in
  `CONTEXT.md` → Segment so the question is not re-asked. **What remains unsized is the small set of
  foot-only Segments** — crossings at authored Junction pieces, paths, precincts — which are few and are
  exactly the edges Severance turns on, so R0 may not omit them.
- ~~**Is a Building's Access Point a graph node?**~~ **CLOSED — no, it is an offset along a Segment**,
  and the arithmetic is what settles it rather than a preference. `CONTEXT.md` → Segment already says
  *"the nodes are intersections"*, and five Buildings share a Segment at the working figures — so
  promoting Access Points to nodes would split every Segment five ways and put the graph at
  **150,000–300,000 edges instead of ~30,000**, five to ten times everything S2 measures. Now stated in
  `CONTEXT.md` → Access Point. **The consequence is the query shape:** a routing query is
  `(Segment, offset) → (Segment, offset)`, so R0's denominator seeds both endpoints of the origin
  Segment and terminates on either endpoint of the goal, reporting that overhead separately — *a
  node-to-node denominator measures a query the game never issues, and every figure in the spike
  divides by it.*
- ~~**S2 owns Chunk size, which is on the *cannot be retrofitted* list.**~~ **CLOSED —
  [`adr/0040`](../docs/adr/0040-the-pathfinding-cluster-is-a-multiple-of-the-chunk-not-the-chunk.md).
  The pathfinding cluster is a multiple of the Chunk, not the Chunk**, and the asymmetry nobody had
  noticed is that **one of the two is in the save and the other is not.** `05 §5` makes a save *"a
  sequence of Chunk records"*, so the Chunk is pinned from milestone 10; the hierarchical router's
  abstract graph is `(derived AND rebuilt)` like the travel-time matrix, so changing the cluster costs a
  recomputation and nothing else, forever. **Unifying them imported permanence onto a structure that
  never had any** — the *sixth* instance of `05 §5`'s own lesson that *"a constant welded to two
  decisions is governed by whichever of them is louder"*, and a new axis of it: the Cell was split
  because a role was **hash-bearing**, this splits because a role is **permanent**. It meets `05 §5`'s
  burden — four of the five couplings are not couplings at all, and **dirty tracking** survives because
  a cluster constrained to a whole number of Chunks maps by a shift, exactly as the Cell does as a
  strict *divisor*. `adr/0014`'s *"the Chunk grid is already the pathfinding cluster"* keeps its useful
  half (a regular tiling already exists) and loses the asserted half (that it is *the* cluster).
- **There is no save migration path for a Chunk size change, and Chunk size is still on the *cannot be
  retrofitted* list.** `adr/0040` removes a *reason* the Chunk was pinned, not the pinning: rendering,
  saves and work partitioning still fix it from milestone 10, and `05 §11` open question 3 still says
  *"cheap to change now and expensive once save files exist."* **Nothing in the corpus describes what
  happens if a profile later says the Chunk should be a different size.** A save is a sequence of Chunk
  records generated from `adr/0003`'s field declaration, which is the machinery a re-chunking migration
  would be written against — so the ingredients exist and the procedure does not. Owed its own session;
  raised while grilling `0010` and deliberately not answered there.
- **`volume / capacity` per direction makes the Road Graph *directed*, and that is why S2 cares — not
  Stress.** The gate framing is about Stress understating a Segment jammed inbound at the peak, which is
  milestone 6's problem. But the **VDF** is the travel-time function and is *"evaluated on one Segment's
  own `volume / capacity`"*, so the choice sets **the cost function S2 routes on**: `cost(A→B) ≠
  cost(B→A)`, the route cache key becomes an *ordered* node pair (a cache treating `{u,v}` as unordered
  returns the wrong route for half its hits, **silently**), and the travel-time matrix is asymmetric so
  nothing may halve it by symmetry. None of the three is a local retrofit, so R0 must be right about it
  from its first line.
- **`adr/0020`'s Settlement algorithm assumes a symmetric matrix and nothing says so.** It computes a
  Settlement as *"a **connected component** of the District graph… **a union-find** over data already
  being maintained, at effectively no cost"*, while `CONTEXT.md` → Settlement defines one as *"a maximal
  set of Districts **mutually** reachable within the Commute Budget."* **Union-find computes weak
  connectivity and "mutually reachable" is strong connectivity**; they coincide only when the matrix is
  symmetric. The divergence's headline case is precisely the one the Segment gate already cites —
  inbound within Commute Budget at the morning peak, outbound not — so union-find would merge two
  Districts into a Settlement that is not mutually reachable at all, and Settlements would appear and
  merge for a reason the definition excludes. Strongly connected components is Tarjan: still cheap, but
  not `adr/0020`'s stated claim. **`0010` R1 now reports the distribution of
  `|matrix[i][j] − matrix[j][i]|` at peak, which settles it either way** — negligible asymmetry means
  union-find survives on evidence rather than assumption. **Recorded before the numbers arrive.**
  > **SETTLED by R1, against the ADR, and it needed a different instrument than this entry specified.**
  > At a tight Commute Budget union-find returns **6 Settlements where Tarjan returns 8**, and the
  > largest component differs by twenty Districts out of 121. **`adr/0020` is owed an amendment.**
  >
  > Three things R1 found that this entry did not anticipate, and each is the more useful half:
  >
  > 1. **The asymmetry distribution is the wrong test.** A distribution is a claim about travel times;
  >    a Settlement is an object the game is made of, and a Building's Trips fail or do not depending
  >    on which algorithm is right. **Run both algorithms and compare the cities.** A negligible
  >    distribution would not have proved union-find safe, because the question is whether any pair
  >    straddles the Budget — not whether the typical pair does.
  > 2. **The exposure is a band, not a threshold.** A pair is one-way only while the Budget sits
  >    between its two directions' costs, so the one-way count rises to 264 and falls back to 47.
  >    **No generous Commute Budget closes the gap** — a Budget generous enough has stopped bounding
  >    anything, which is the thing `CONTEXT.md` → Commute Budget exists to do.
  > 3. **It is the same question as the volume-scope entry above, which this ledger filed as two.**
  >    Under per-Segment volume the matrix is symmetric **to the bit** and union-find is right by
  >    construction — so `adr/0020` is only exposed if the corpus takes the scope its Lane, Stress and
  >    Settlement definitions all separately require. **The exposure is the price of three other
  >    decisions being coherent**, and it costs 5% of a structure that is 1.2% of the world.
- **The travel-time matrix's time resolution is unstated, and a Day-average matrix cannot represent the
  peak.** Every load figure in `0010` is now measured at the morning peak, but if the matrix is averaged
  over the Day then morning inbound and evening outbound cancel and the asymmetry above vanishes into
  the mean — so the matrix would be answering a question about a different moment than the one being
  asked. A per-phase matrix over the sun arc's five phases multiplies resident size by five and matches
  the moment. `0010` R1 now sweeps it as a second axis alongside District count, because it interacts
  with both the peaking factor and the `adr/0020` exposure.
  > **MEASURED by R1, and the cancellation is near-total rather than partial.** A Day-average matrix
  > reports **1** one-way District pair where the morning peak has **76** — the two peaks are opposite
  > in sign by definition and an unweighted mean of five phases is dominated by the three balanced
  > ones, so a single-resolution matrix hands the choice loop a city with almost no directional
  > structure in it at all.
  >
  > **And that makes resolution a decision rather than an axis.** A Household reading a Day-average
  > matrix chooses differently from one reading a per-phase matrix, measurably — so under `05 §4`'s
  > test they are **two cities**, which is the same argument this ledger already makes about the
  > refresh *cadence*. **Resolution is not in this ledger anywhere and cadence is**, and they are the
  > same class of decision about the same object; filed as `0010` decision 2a and owed alongside it.
  > The price of the honest option is not the deciding factor — five matrices are 286 KiB at the
  > working District count against 57 KiB, and five times a 188 ms build.
  >
  > One methodological note worth keeping. The Day average is taken over the **cost**, not the volume.
  > BPR is convex, so the delay at the mean volume is strictly less than the mean of the delays:
  > averaging volumes first would produce a Day-average matrix describing a city **with no rush hour in
  > it at all** rather than one whose rush hour has been smeared. Neither is right, and the one chosen
  > is at least wrong in the direction that does not flatter the abstraction under test.
- **The Epoch is a single counter on the whole Road Graph, so "never a global flush" is true about
  *when you pay* and false about *what survives*.** A counter carries no location, so a cached route
  cannot tell whether an edit touched it: the only options are *treat it as stale* — a total
  invalidation paid lazily — or *re-walk its Segments*, which is O(path length) on every hit. **Read
  strictly, the corpus's own Epoch fails S2's own tripwire** (*"a candidate that needs a global flush
  is out on a design commitment"*). It bites here for the reason `0010` already gives about DSDV: *in a
  city builder, link deletion is the core verb*, so the Epoch moves every few seconds of play, and R6's
  cache hit rate stops being a property of the O-D distribution and becomes a property of how recently
  the player touched anything. **`CONTEXT.md` → Epoch now carries the distinction**, and `0010` R5
  measures a three-rung ladder — **global, per-cluster (riding `adr/0040`'s partition), per-Segment** —
  reporting hit rate *as a function of edit rate*. Storage decides nothing: a version word per Segment
  is ~120 KB.
  > **R1 found the matrix's half of this, and it is worse than the ambiguity.** The matrix carries
  > **no** Epoch — R1 declined to give it one, because a version counter would imply a relationship to
  > the route cache that nobody has argued — and it invalidates by *dirty region* instead, per
  > `02 §6`. **That mechanism is unsound.** A path from District *i* to District *j* can cross the
  > edited ground without either endpoint being near it, so rebuilding the Districts the region
  > overlaps missed **309 of 429** changed entries on a central edit and 132 of 252 on a corner one —
  > silently, leaving entries stale rather than merely coarse.
  >
  > The sound test — *which routes crossed the region* — identifies the changed set almost exactly, 430
  > entries against 429. But it needs the **route store** to exist (4.06 GiB at 4,096 Districts against
  > a 172.3 MiB world), and it still rebuilds **every row**, because a one-to-all fills a row and every
  > row holds the entry addressed *to* the edited District. **So the matrix's cheap invalidation and
  > its cheap storage are the same trade, taken twice**, and `02 §6` is owed a correction whichever way
  > it is taken.

  **Worth recording as method, not just as a finding.** All three rungs are conservative and
  recomputation is deterministic over the same graph, so **the State Hash is identical across them** —
  which makes the choice an *optimisation* under `05 §4`'s own rule and therefore something to settle
  by measurement rather than argument. The same test says the opposite about `0010` R2b's attribution
  question, where direct and aggregate produce **different cities** and no benchmark can decide it
  alone. *The State Hash rule is what tells you which kind of question you are holding, and it is worth
  running by name each time.*
- **The Parking Shed is a third Epoch consumer and it scales with Buildings.** `05 §3` caches shed
  membership per Building, *"invalidated by the Road Graph Epoch"* — so under one global counter **a
  single road edit invalidates all ~150,000 sheds**, and `adr/0009`'s promise that arrival is *"a
  handful of lookups, never a search"* holds only while the shed is warm. After any edit every arriving
  vehicle pays a rebuild first, **on arrival**, which is when the Trip is trying to finish: a stampede
  triggered by the player's most common action. It also ranks the Epoch ladder differently from routes —
  a shed is a *set of Bins reachable within a walk* rather than a path, so per-Segment versioning does
  not obviously help while **per-cluster fits it better than it fits routes**. `0010` R6 now measures
  both caches against the same edit storm and reports rebuild cost at the moment of edit, not only
  steady-state hit rate; **a ladder chosen on routes alone would be chosen on the cheaper consumer.**
  `05 §3` is owed the same *when you pay / what survives* correction as `CONTEXT.md` → Epoch.
- **Route invalidation and matrix invalidation are two unrelated mechanisms and nothing joins them.**
  Routes invalidate against a **scalar** Epoch; `0010` R1 rebuilds the travel-time matrix against a
  **dirty region**, which is spatial. Nobody has said whether the matrix carries an Epoch at all — so as
  written the matrix and the route cache can disagree about what the network currently is. Owed a
  statement either way.
- **The travel-time matrix's tripwire tested complexity where the risk is cache, and could not fire.**
  *"Not O(1) and cheap"* cannot be true of a lookup into an *n*×*n* array, so the row was unfireable —
  the same effect as a wire reasoned around, arrived at earlier. What binds is where the matrix lives:
  40 KB at 100 zones, 640 KB at 400, **16 MB at 2,000 — past L3, so every read is a DRAM miss**. `0010`
  now trips on *the read costing more than S4's K2 random gather at the zone count the design needs, on
  the desktop*, which is falsifiable and divides by a denominator that already exists. **Zone count has
  a hard ceiling set by L3 rather than by memory**, and `plans/0001`'s 100–400 sits near its edge.
- **The choice loop's access pattern over the matrix is unspecified, and the corpus uses two
  incompatible phrasings for it.** `references.md §2` calls it both *"what is the commute from this
  candidate dwelling to any job?"* — one origin, many destinations, a cache-friendly **row scan** — and
  *"many-to-many, evaluated tens of thousands of times per cycle"*, which reads as scattered. **They
  differ by an order of magnitude at 2,000 zones and are indistinguishable at 100.** `0010` R1 measures
  both; which one the loop performs is then a design decision with a price rather than an accident of
  implementation.
- **District extent needed a number, and the criterion was already in the corpus while the number was
  not.** `02 §2.1` settles the basis — *"District extent is bounded by the pooling abstraction's own
  validity"*, since *"a single-District map would pool Goods across the whole world instantly, deleting
  Shipments and silently collapsing `adr/0022`"* — so **the count is physics rather than a design
  choice**, and the early city has one District *because the city is one neighbourhood*. **Working
  anchor now recorded in `CONTEXT.md` → District: 128 Cells, 2.10 km², ~1.45 km across**, explicitly a
  starting point subject to playtesting rather than a derivation.

  *A grilling correction worth keeping.* The first proposal was **8.4 km²**, derived from real district
  areas — Paris arrondissements, Chicago community areas, London wards. **That grounded the bound in the
  wrong quantity**: how big people *call* a district is not the area within which ignoring transport is
  defensible. At 8.4 km² a District is 2.9 km across and an intra-District delivery is a van trip —
  precisely the Shipment the abstraction is meant to be standing in for. The corpus's own criterion
  gives roughly a quarter of that.

  *And the count cap is the wrong instrument for the exploit.* A cap on **count** constrains *many
  small* Districts and does nothing at all to *one huge* one, which passes any count cap trivially. Max
  **area** is what bounds the free-logistics exploit; a **minimum** area is a performance guard against
  the L3 cliff above, and at a 2.10 km² maximum a fully-built map holds ~128 Districts, so the cliff is
  not reachable in ordinary play and the guard exists only against a pathological redraw.
- **`02 §2.1` makes Districts *"contiguous sets of Chunks"* and `05 §4` makes Chunk size free to tune.
  Both cannot be true.** District extent decides Goods pooling and travel-time matrix granularity, so a
  Chunk-aligned boundary means **a profiler can change the city**. This is the welding failure for the
  **seventh** time and the first that is a flat contradiction between two documents rather than a
  mis-classification in a tuning column — `05 §201`'s own lesson, *"a constant welded to two decisions
  is governed by whichever of them is louder"*, with a profiler as the loud one. **Fixed in
  `CONTEXT.md` → District: a District is a contiguous set of Cells, never of Chunks.** The Cell is
  frozen, is a strict divisor of the Chunk, and is already *"the resolution at which the city's
  environment varies"*, so the alignment costs nothing. **`02 §2.1` is owed the same correction.**
- **`06:42` is stale: it lists *"Are Districts player-drawn or automatic?"* as open and blocking
  milestones 5c and 8.** `02 §2.1` settled it — ***both***, automatic by default and player-adjustable
  as an advanced action, with splitting and redrawing a late-game action that *"arrives exactly when one
  end of the map genuinely differs from the other"*. `0010`'s gate section repeated `06`'s claim and has
  been corrected. `06` is 🔴 and never grilled, which is consistent with it carrying a question two
  documents have since closed.
- **The routing cost function was unstated, and a spike that leaves it unstated measures the SC4
  failure.** `02 §5.9` already settles it — *"the cost function used for routing must be the same
  quantity used to judge trip failure, and the same quantity shown to the player"* — and the **Commute
  Budget** is drawn as a wedge on the sun arc, so the quantity is **time**. `0010` R0 now says so.
  Nothing else in the corpus was wrong; it was silent, which is how a spike picks by default.
- ~~**Does the arithmetic substrate need a `Sqrt` for S2's A\* heuristic?**~~ **CLOSED — no, and the
  question was the wrong one.** `Borough.Core.Arithmetic` has no `Sqrt` and `0010` specified a
  *Euclidean* heuristic, which looked like a blocker. It is not: **admissibility needs only a lower
  bound on the true distance**, so an underestimating integer approximation suffices and can live in
  the spike, which dies. **The real finding is that the tight grid metrics are not safely admissible on
  this graph at all.** Manhattan is exact on `adr/0014`'s grid-snapped Streets, but **Arterials are
  freeform splines at arbitrary angles**, so a diagonal Arterial makes the true distance shorter than
  Manhattan, the heuristic overestimates, and A\* silently returns a **non-optimal path — which is a
  different Trip and therefore a different city.** `CONTEXT.md` calls Arterials *"deliberately rare"*,
  which makes the tight metrics admissible *almost* always, and *almost always* is the trap. `0010` R0
  now sweeps a four-rung ladder — Chebyshev, underestimating Euclidean, octile, Manhattan — and reports
  **how often each returns a path Dijkstra says is not optimal**. **Owed: the Arterial density at which
  admissibility breaks.** None of it enters the substrate.
- **A denominator needs its own quality reported, not just its machine.** Every ratio S2 publishes
  divides by R0's A\*, and a distance heuristic over a time-cost graph must be divided by the map's
  **maximum** free-flow speed to stay admissible — which, since Arterials are the fast edges on a
  network that is mostly slow Streets, makes it nearly uninformative across most of the graph. A weak
  denominator flatters HPA\*'s speedup, the cache's value and R2's crossover alike. `0010` R0 now
  reports nodes expanded against path length and the ratio to plain Dijkstra. **This is S4's
  denominator lesson one level up:** S4 learned a denominator must state its machine and its moment
  truthfully; this adds that it must state its *quality*.
- **Every load figure in the corpus is a Day-average, and the Day has a rush hour. No peaking factor
  exists anywhere.** `02 §1.2` and `01 §7` give the sun arc five named phases — *dawn, morning peak,
  midday, evening peak, night* — with **no durations attached**, so nothing can say how concentrated
  demand is. Meanwhile the generator mix is 79% peak-bound: commutes (1M) and school (500k) against
  410k spread shopping and freight. Outbound commute plus outbound school is ~750,000 Trips in one
  phase — ~458 Trips/Tick at a fifth of a Day, ~732 at an eighth, against a stated mean of 232. **So
  every load figure is low by 2–3× at the moment routing is under most pressure**, and combined with the
  Legs correction above the real ask is on the order of **1,200–2,300 Leg routes per Tick**. Three
  consequences, recorded in `0010`: the **37k–111k in-flight band conflates two axes** — it is presented
  as sensitivity to mean Trip duration, but 56,000 × 2–3× peaking is 110k–170k, so its top is roughly
  the *provisional* duration at peak rather than the pessimistic duration at mean, and **`spike-results`
  is owed the correction**; S2's tripwire said *"sustained"*, which excludes transients but not a peak
  lasting hundreds of Ticks, and now reads *"at the morning peak"*; and **R2a's crossover moves with the
  peak while only one side of it moves** — direct attribution is peak-sensitive, aggregate is not — so
  the crossover is swept and the report names **the peaking factor at which it inverts**. **The phase
  widths are almost certainly hash-bearing**, for the same reason as the matrix refresh cadence: two
  peak widths produce two cities.
- **The route cache has no eviction policy anywhere in the corpus, and now no defined key either.**
  `adr/0012` permits caching *"keyed by origin-destination pair"*, `adr/0006` forbids collections that
  grow and has reversal criteria of *"Nothing."*, and nothing joins the two. `adr/0017` shows the
  eviction pattern — fixed capacity, least-used. **The key is the newer gap:** that phrase was written
  before anyone knew an Access Point is a `(Segment, offset)`, and it is ambiguous between a key space
  of nodes² and one of Buildings² ≈ 2.25 × 10¹⁰, where the hit rate is approximately zero. `0010` R6 now
  keys on the **node pair the search spans**, with offsets applied as an arithmetic correction, and
  measures the error that trade induces against the **Commute Budget** — the only thing that consumes
  it. Both halves owed to `adr/0012` as an amendment.
- **`06`'s S2 specification is stale and should be struck.** *"30k Travellers"* predates the 1M target
  and the 4096² map; the derived load is ~56,000 Trips and ~140,000 Legs in flight, so the spike as
  specified is undersized ~2× on Trips and ~5× on Legs. S1's *"20k Buildings"* is stale for the same
  reason. **And the ~400k Trips/Day figure this ledger already debunked still stands uncorrected in
  `05` and in item #20** — a figure identified as wrong and left in place is worse than one nobody has
  checked, because the next reader finds it in the authoritative document rather than in the ledger.
- **Three filenames use a term `CONTEXT.md` bans outright.** `CONTEXT.md`'s *Terms we deliberately do
  not use* opens with *"**Agent** — too vague. Say Citizen (the record), Traveller (the embodiment), or
  Vehicle."* Yet `adr/0012-routing-intent-lives-in-the-agent.md`,
  `adr/0017-agents-satisfice-they-never-optimise.md` and `docs/03-agent-architecture.md` all carry it,
  and `03`'s title is *"Agents and Movement"*. **For an ADR this is not cosmetic: `CLAUDE.md` states
  that the filename *is* the claim**, so `0012`'s claim is stated in vocabulary the corpus forbids —
  and its actual claim, *routing intent lives in the Traveller*, uses the correct term in its own body.
  **Whether the ban post-dates the filenames cannot be settled from git** — the entire corpus arrived in
  one commit (`84240a1`), so there is no history to read, and this is the first time that has cost
  anything. **The rename is 33 occurrences across 22 files** and is not S2's to do; recorded so it is a
  decision rather than a drift.

**Partially discharging the Microscopic Cap bullet above:** K0 does take the Cap as a parameter and
reports footprint as a curve — **3.0 MiB at 1,000 Segments to 90.3 MiB at 30,000**. The Cap itself
remains unset, and remains the input `adr/0036`'s headroom argument is downstream of.

**From slice 2, building the arithmetic substrate** — [`0005`](0005-arithmetic-substrate.md),
**now complete, all seven tasks.** The first code in `Borough.Core`, and three findings arrived within
an hour of it existing, which is rule 6's prediction landing rather harder than the planning did. The
slice was gated on one decision — the `exp`/`log` resolution, settled in `adr/0038` — and produced a
second nobody had scheduled: **an amendment to `adr/0003`'s normative hash.**

- **The normative `draw()` collapsed two of its four coordinates, and writing its first known-answer
  vectors is what exposed it.** `adr/0003`'s opening round was `mix(seed + GOLDEN + entity)` — the world
  seed and the entity id **added together**, so only their sum reached the hash. `draw(seed=1000,
  entity=1)` and `draw(seed=1001, entity=0)` were bit-identical at every Tick and for every purpose:
  **rerolling the world seed by one produced the same world shifted by one entity rather than a new
  one.** Within a single world it was harmless, the seed being constant; the damage was across worlds,
  and it is the failure `05 §4` names — *correlates two decisions invisibly* — one level up, correlating
  two cities. The ADR is amended to give the seed its own round; the round is loop-invariant, so it is
  paid once at world creation in `WorldKey` and the hot path is the same three mixes it always was.
  **Three things worth keeping from it:**
  - **The cost curve is the whole argument for doing this early.** This is a format-class change by the
    ADR's own terms. Today it cost a table of test vectors. After the first Input Log, State Hash
    baseline or save exists it invalidates all of them. `adr/0003` says integer discipline is *the first
    thing built*; this is the same argument applied to the hash, and it is the second time this corpus
    has been paid by acting before there was anything to break.
  - **The defect was in a document that had been reviewed repeatedly and was invisible in prose.**
    `hash(world_seed, entity_id, tick, purpose_tag)` reads as four coordinates and the pseudocode says
    so too; only evaluating it reveals that the first line takes two of them and adds them. **A
    specification that is only ever read is only ever checked for plausibility.** The generalisation for
    the audit list: **anything normative in this corpus that has never been executed should be executed
    once, cheaply, before it acquires dependents** — the Q16.16 range assertions and `WHEEL_SIZE`'s
    interaction with routine sleep lengths are the nearest candidates.
  - **The fix is honest about what it does not buy.** It does not make the coordinates algebraically
    independent: two worlds still satisfy `draw_A(e) == draw_B(e + d)` for a constant `d`. What changes
    is that `d` is now a pseudorandom 64-bit value rather than the seed difference itself, which takes
    the overlap probability between two given worlds from a certainty to about 2⁻³⁹.
- **The one property task 5 actually names is still not tested, and the test that claims to test it
  cannot.** `0005` asks for *a second, independent implementation written from the ADR text alone*,
  because the stated property is that two people can implement this and get the same city. Both
  implementations in `RandomnessTests` were written by one author in one sitting, so a misreading of the
  ADR appears in both — the test is a check on transcription, not on the document. **It is recorded as
  a limitation in the test file itself rather than left to look like coverage.** What it does close: the
  second implementation reduces mod 2⁶⁴ explicitly in `BigInteger` and so inherits none of C#'s
  `unchecked` semantics, which is the most likely way to get this function wrong. **The real check is a
  reader who has not seen the code, and it stays owed.**
- **`PurposeTag` ships nearly empty, deliberately, and its uniqueness check ~~is a stopgap that must be
  deleted~~ is now the build-time check the corpus asked for.** `adr/0003` and `02 §10` both require
  uniqueness to be a **build-time** check; a unit test is not one, since it catches a duplicate only
  when someone runs the suite. `PurposeTagTests` held the window and **slice 3 deleted it**, replacing
  it with `BOR0801`–`BOR0803`. Tags are still added when a mechanism that draws is built rather than in
  advance, because a tag with no caller cannot be checked against the draw it is meant to name.

  **Writing the analyser sharpened why a test was the wrong instrument, and it is not the usual
  argument about tests running late.** Every other rule in `Borough.Analysers` describes a defect that
  *something* could eventually observe — a float diverges across machines, a walked `Dictionary`
  reorders, a managed field shows up in a GC trace. A reused `purpose_tag` has **no runtime symptom at
  all**: it throws nothing, trips no invariant, and produces a city that is entirely plausible, because
  the two mechanisms sharing the tag simply agree forever and every aggregate over them looks like
  ordinary variance. There is no observation that distinguishes the correlated world from the
  uncorrelated one. A test therefore runs at the wrong *kind* of moment, not merely a late one.

- **A mechanically-enforced rule was incompatible with the type shape the corpus itself prescribes,
  and the first file to land under it tripped it.** Slice 0's `Core_returns_no_human_readable_strings`
  guard rejects any public string-returning method; `0005` prescribes `readonly record struct` for
  typed quantities; **every record struct generates a public `ToString()`.** The guard has been
  narrowed to exempt an override of `object.ToString()` specifically, and the exemption is right
  rather than convenient: **`object.ToString()` is callable on every type whether or not it is
  overridden**, so banning the override closes no leak — it only trades `Money { Raw = 5 }` for
  `Borough.Core.Quantities.Money`. What `adr/0002` names as the leak vector is a *bespoke* member
  written because a panel wanted one, and those are still caught. **The general lesson is the one
  worth keeping: a guard written before any code exists is a hypothesis about what the code will look
  like**, and this corpus has now written seven of them against a codebase of one marker class. The
  other six should be expected to need the same treatment, and needing it is not evidence they were
  wrong.
- **`adr/0003`'s justification for leaving ambient arithmetic unchecked does not cover unsigned
  subtraction, and `Ticks` is unsigned.** The stated reason `checked` is confined to the fixed-point
  library is that *the width already closes the question*. For `u64` subtraction the width closes
  nothing: `earlier - later` wraps to roughly **1.8×10¹⁹**, a Tick count so large that every
  downstream comparison silently succeeds — and unlike a narrowing overflow it needs no large inputs,
  only two Ticks in the wrong order. Handled without widening the `checked` scope, by giving `Ticks` a
  `TrySubtract` in the same shape as `Money.TryDebit` and defining no `operator -` at all; the
  negative-compilation suite asserts `Ticks - Ticks` does not compile. **`adr/0003` should state the
  exception rather than leave the width argument reading as universal.**
- **Floor rounding compounds downward, and the corpus has a conservation obligation it interacts
  with.** Q16.16 has no exact third, so `Ratio.FromFraction(1, 3)` floors and three of them come to
  **one representable step below one**, not to one. That is nothing for a position — a ten-thousandth
  of a Tile — and it is asserted as a test rather than avoided, because the loss is always downward
  and never upward, which is what makes it safe here. **It is not nothing for a Bin.** `CLAUDE.md`'s
  definition of done requires Goods conserved, and a conserved quantity split three ways by scaling
  each share independently loses units on every split, silently and identically in both runs — so the
  State Hash certifies it. **Whatever splits a Bin must reconcile the remainder rather than scale each
  share**, and that belongs to the Rule engine (slice 7) rather than to the substrate. Recorded now
  because the arithmetic that causes it is landing now.

**From [`adr/0046`](../docs/adr/0046-a-driver-routes-on-habit-sight-and-temperament-never-on-current-cost.md), the routing choice model** — written the session S2 R5.5 closed, and recorded on the day the structure was chosen rather than after the numbers arrive. The ADR settles the *structure* by argument and sets **no parameter**; these are the four it creates, and R8 measures three of them.

- **The Sight Horizon has no value and it is the only routing parameter with a derivable floor.** How many Segments ahead a Traveller reads live cost for. Too small and the signal arrives at a node with no alternative — the driver is already committed to the corridor. **R8.1 derives the floor from the Road Graph with no traffic at all**: the distance to the nearest node with a real choice. The value above the floor is a curve R8.3 reports and the corpus chooses. `plans/0010`'s 10% budget row is what it is chosen against, and that row is itself unratified, so **this is a number selected against a threshold nobody has ratified** — stated so it is not later read as derived.
- **The base Temperament threshold has no value.** How much better an alternative must be before a Traveller diverts. `adr/0017` says *"substantially better… by enough to be worth the bother"* and deliberately gives no figure, because it is per-actor and emergent — Temperament is that word finally acquiring a number, and the *base* of that number is still a choice.
- **The base/jitter blend weight has no value and no argument at all.** `adr/0046` argues both endpoints fail — pure jitter gives a population diverse in aggregate and uniform in character, which is `adr/0005`'s failure wearing a distribution; pure character re-synchronises the flow into a smaller permanent herd. **What it does not argue is where between them.** R8.4 sweeps `{0, ½, 1}` and reports; it does not choose. *This is the weakest-supported number the routing model contains and it should be treated as such when it is finally set.*
- ~~**The Habit refresh cadence is unset, and it is hash-bearing if it is ever finite.**~~ **CLOSED by R8.5, and it closed the cheap way.** Static per world was entered as the null hypothesis and it **survives**: under a *sustained* demand asymmetry — 40% of every respawn bound for one District for a whole window, which is R1's monocentric morning peak as it actually behaves — Sight settles **42.62% below** a control with identical physics and no ability to respond, and the control settles *above* its own pre-surge level while Sight settles *below* it. `03 §3.4`'s self-correction therefore closes with only the local layers reading the VDF, **so there is no cadence to set**: no maintenance scheme, no `adr/0015` membership test, and **R4.6's incremental-versus-rebuild break-even does not select an algorithm after all**. The error runs in the safe direction — 4 of 5 control runs peak inside the last quarter, so the control's plateau is a lower bound and a longer window would *widen* the gap. **The first version of this test could not answer it**: `Surge` retargets Travellers who respawn on arrival, making the disturbance a pulse with a half-life of one journey, which any system recovers from. Control and Sight settling identically was not a null result, it was no result.
- **CLOSED by R8.1: the Sight Horizon's floor is 1 Segment**, derived from the Road Graph with no traffic — 98.02% of arrivals are already at a node with a real choice, taken at the p90 of the arrival distribution rather than the median, because a horizon set at the median is structurally useless to half the crossings in the city. **The value above the floor is still unset** and R8 reports a curve rather than choosing, exactly as R1 did for the District count.
- **NEW, and it is a constraint on the base threshold that `adr/0046` does not anticipate.** BPR at `β = 4` with the ratio clamped at 4.00 makes one saturated arc **39.4× its free-flow time** — a Street that runs in 0.87 Ticks runs in 34, against a mean journey of order 80. **So a lookahead of even two arcs can put more live cost in front of a driver than the free-flow remainder behind it**, the detour is charged at free-flow and looks cheap, and the Sight Horizon stops behaving like a monotone knob. That is what a live-versus-lagged comparison does when the live half is bounded only by a clamp and the lagged half is not bounded at all, and it means **the base threshold and the horizon are not independent numbers.**

**One classification, and it is not a number.** `adr/0046` requires that **Sight and Promotion read the same congestion quantity**. Sight reads live `v/c` at a junction to decide a diversion; `CONTEXT.md` → Stress drives Promotion. If they diverge, the city routes around a jam it never promotes to Microscopic — `01 §7`'s rule that a number must not contradict what the player is watching, arriving in the one place where the contradiction is between two parts of the simulation rather than between the simulation and a panel. **Nothing currently guarantees they are the same quantity.** Owed to `03 §3`, not to S2.

### Slice 3 — the analysers

- **An absolute rule was cheaper to obey than to argue with, and obeying it found the bug.** `05 §4`
  bans *"`Math.Exp` / `Math.Log` and every other `Math.*`"*, and the ban reads over-broad on
  `Math.Abs(int)`, which is exact integer arithmetic with no intrinsic to vary. The lint fired on
  `Tiles.Magnitude` anyway. Writing the replacement rather than the exemption surfaced what the call
  was actually doing: **`Math.Abs(int.MinValue)` throws**, and `Tiles.Magnitude` was propagating an
  `OverflowException` nobody had written down. `IntegerMath.Abs` now states the edge case in the same
  shape as `ShiftLeft`. **The general point is worth more than the instance:** the argument for
  narrowing a mechanical rule to the cases that "really" matter is usually available and usually
  costs more than obeying it, because the rule is cheap and the audit of whether this instance is
  safe is not.

- **The analysers found exactly one violation in ~700 lines, which is a weak signal recorded as
  one.** It is not evidence the lints are unnecessary; it is evidence they landed early enough to
  shape the code rather than condemn it, which is the stated reason slice 3 sits before the first
  table. The number to watch is whether the first violation in slice 4's tables is also a real defect
  or the first false positive.

- **A rule-7 marker attribute would have been the wrong shape, and the reason generalises to every
  opt-in guard.** The obvious design — check only types carrying `[SimulationState]` — makes a
  *forgotten* marker a silent exemption that reports nothing anywhere, which is the same failure
  class as the reused `purpose_tag`: no symptom, no observation that distinguishes it. Opt-out puts
  the friction on the exception instead, where `adr/0031`'s finding says it belongs. **Prefer
  opt-out for any guard whose failure mode is silence.**

- **Three of `05 §4`'s seven lints remain unwritten and one of them must stay that way.** Rule 4,
  thread-count equivalence, would today assert a property against no parallelism and **pass
  vacuously forever** — a green test that certifies nothing is worse than a missing one, because it
  is indistinguishable from coverage. Rules 5 and 6 are owed by slice 5 and milestone 10. `05 §4` now
  records the status of each inline, so the list stops reading as if all seven were live.

- **A guard written against the *spelling* of a rule enforces less than it appears to, and the gap
  is invisible from inside.** The floating-point lint originally tested whether a type's
  `SpecialType` was `Single`, `Double` or `Decimal`. Every deliberate-violation test passed, the core
  built clean, and the rule was open at four doors — `List<double>`, `Func<double, double>`,
  `double?`, and `Vector2`, which hides two `float` fields inside an `unmanaged` struct and so slipped
  lint 7 at the same time. **`Vector2` is not a hypothetical**: it is what somebody writing a position
  reaches for, in a Godot project, and nothing would have reported it. The fix was to make the
  predicate structural rather than keyword-shaped. *The general form: when a rule is about a
  behaviour, a guard that matches a name checks the name.* Worth applying to the six other guards
  before trusting any of them.

- **A rule contradicted itself and the contradiction survived a full test suite.** `BOR0301` exists
  because .NET randomises string hashing per process — and `string.GetHashCode()`, the direct
  spelling of that hazard, was unflagged, as was `System.HashCode`, which is seeded from process
  entropy at start-up. Both now report under `BOR0206`.

- **A diagnostic instructed an action its own rule forbade.** `BOR0301` said *"use a sorted array"*
  while reporting every route from a hash map to one. The resolution was not to carve out `OrderBy`
  — sorting only restores determinism when the key is total, which an analyser cannot check and a
  reader will not notice is missing — but to fix the instruction: **order comes from the ordered
  source, never from a rescue at the end.** Recorded because a lint whose remedy is illegal is a lint
  that gets suppressed, which is `adr/0002`'s and `adr/0036`'s shared revisit trigger arriving by
  accident rather than by decision.

- **No unratified numbers.** Rule 6 of `plans/0003` applies and produced nothing this slice: the only
  figures chosen were the diagnostic id scheme, which is derived from `05 §4`'s lint numbering rather
  than picked, and Roslyn's `4.14.0`, which is a floor set by the installed SDK. Recorded explicitly
  because *no numbers* and *nobody wrote the numbers down* look identical in a ledger a year later.

### Slice 4 — typed tables, the field declaration and the State Hash

- **Ledger #29b has a Phase 1 answer: rows never move, and the Chunk partition is a separate
  index.** `adr/0004`'s *tables partition naturally along Chunks* is true for static entities and was
  unargued for mobile ones, and the failure it risks is worse than a stale handle: the generation
  counter detects use-after-free but **cannot detect *this handle now points at someone else's valid
  row***. The answer costs an indirection on spatial queries and nothing on the hot path. It is now
  structural rather than intended — `Rows` has no compaction and no relocation, and `SlotCount` is
  documented as deliberately not the live count so that nobody adds one to close the gap.
  **Revisit trigger: S0's measurement**, which will exercise this whether it means to or not, and
  `05 §5` role 3, which still leans on the other answer and needs a wording pass.
  **S0a has now fired that trigger and the answer holds**: 100,000 Ticks over 1.6M rows at the target,
  no relocation, no compaction, nothing trending. **`05 §5` role 3's wording pass is still owed** —
  the measurement did not touch the document.

- **The Citizen row is 62 bytes, against S4 task 2's recomputed 56 and `05 §3`'s stale 40**, and the
  two extra columns are both the table layer's rather than the design's: the monotonic id is a `u64`
  where task 2 assumed `u32`, and **`free_next` is a column task 2's schema never counted at all**
  because it is allocator bookkeeping rather than a field of a Citizen. Per-Tick is **13 B**, which is
  exactly what task 2 derived and is the figure the Event Wheel's argument rests on. *Unratified: the
  `u64` id.* At 1M it is 8 MB against 4 MB and buys headroom no city will reach; it is recorded rather
  than argued, and narrowing it later is a hash re-baseline rather than a migration.

- **A list whose order carries meaning cannot be `(derived AND rebuilt)`, and this was found by
  building the rebuild.** A derived structure claims to be a pure function of saved state, so a
  rebuild must reproduce it exactly — and a rebuild has only *index* order to work from. Appending in
  *arrival* order diverges from that the first time the free list recycles a slot, and **nothing
  reports it**, because derived fields are outside the State Hash by declaration. The occupant and
  member lists therefore insert in slot order. *The consequence belongs to slice 7:* a Bin's wait list
  is drained round-robin in arrival order, arrival order is recoverable from nothing else, so **the
  wait list is `Saved`** — which `05 §3` currently contradicts by listing it among the two derived
  structures that *"are not saved; both are rebuilt"*. That sentence needs correcting before slice 7.

- **Zeroing a freed row turned the generation counter into a no-op, and only one test could see
  it.** `FreeSlot` cleared every column and then bumped the generation — but the generation *is* a
  column, so the bump was applied to zero and produced 1, this design's encoding for *live*. Every
  freed row read as occupied and every stale handle to it resolved. Recorded because of the shape
  rather than the bug: **the mechanism meant to implement an invariant is a normal place to break
  it**, and the only test that saw it was the one asserting the negative — that a freed slot is *not*
  live. The positive tests all passed.

- **A column's element type must have no padding bytes, and nothing enforces it.** The hash folds a
  column's bytes, and a struct like `{ byte, int }` carries three bytes whose contents the runtime
  does not define. Every width `05 §3` states is a single primitive or a wrapper over one, so this
  holds today and is documented at `Column<T>`. *Owed:* either a build-time check, or the rule stated
  in `05 §3` beside the widths. It is cheap now and it is a two-run divergence with no cause when it
  is not.

- **The State Hash seed carries a version byte** (`0x426F726F75676801`), because `05 §4`'s test is
  that the hash never moves without somebody saying so — and a change to the fold, to the composition
  order or to `Randomness.Mix` moves every hash in the project at once, which is indistinguishable
  from a regression unless the change is signed. *Unratified, and deliberately trivial to bump.*

- **Provisional figures in the headless report, none of them in `Borough.Core`:** three Households per
  Building, and the per-1,000-Citizen ratios the `World` constructor sizes tables from — 360
  Households, ~150 Buildings, ~225 Lots. All are S4 task 2's, all are marked provisional there, and
  all are sizing hints that `Capacity_is_not_a_hash_input` proves cannot reach the hash.

---

## Superseded — session seven's opening brief

> **The Chunk-size fork below is answered by `adr/0034` and the *Then, in order* list has moved into session eight's section above.** Retained because **§*What lands on the rest of `05`*§ is still entirely live** — it is the inventory of what §1, §2, §6, §7, §8 and §10 must absorb, and nothing in session seven discharged any of it.

**Open on `05 §5`, Chunk size.** It is the highest-value unargued question in the corpus right now, it is directly downstream of last session's map decision, and it is on the *cannot be retrofitted* list — `05` open question 3 says plainly that it is *"cheap to change now and expensive once save files exist."*

### The question to open on

**32×32 is described in `§5` as *"the working figure and it is not yet validated against any of these,"*** and the map closing at **4096²** sharpened every tension that section already named:

| | At 32×32 on a 4096² map | Pulls toward |
|---|---|---|
| Chunk count | **16,384** Chunks, each with aggregates and a MultiMesh set | larger |
| Map Layer grid | **128×128 cells** for the whole map, one per Chunk — 128 m resolution for pollution | either; probably fine |
| HPA\* clusters | 128 m across, on a map where a commute crosses tens of km² of graph — **very many inter-cluster edges** | much larger |
| Render culling | 128 m granularity | smaller |
| Save records | per-Chunk overhead × 16,384 | larger |

`§5` argues hard that unifying the seven roles onto one grid is a **major simplification** — *"a proposal to split any one of these onto its own grid should have to argue why the coupling it removes is worth the six it breaks."* At 4096² the pathfinding role may finally be the one with that argument. **That is the fork: does the Chunk stay one grid at a larger size, or does HPA\* get a coarse super-Chunk that is a multiple of the Chunk?**

Note the second is not really a seventh grid if it is a strict multiple — which is probably the cheap answer, and should be argued against rather than assumed.

### Then, in order

1. **`05 §6` threading** — check it against `adr/0033`. §6 claims *"the Past/Future split means shared mutable state is already rare"*; wait lists are new mutable state, though they appear to be confined to the serial Settle phase. Also note `02 §1.1` marks Phase 2 parallel while `§6` defers parallel decision evaluation — that is a build-order-versus-design distinction, and it should be said rather than inferred.
2. **`05 §2` sim/render boundary and open question 4, the snapshot format.** `adr/0002` names marshalling cost as a trigger for revisiting the boundary, and 1M Citizens is the load that would trigger it.
3. **`05 §1`, §7 remainder, §8, §10.** §7's Ruleset-versioning half is closed; the format and migration half is not.
4. **`02 §4` residue** — fallback chain depth and cycle checking, and whether `mean_workforce_experience` is a legitimate Building Readout under `CONTEXT` → Building's no-averaging-across-Occupants invariant.
5. **Labour as an input Bin** — reached for in session six as the clean answer to *"only produces if staffed."* It is a real change to how employment works and it collides with `adr/0026`, which models jobs as a Household↔Business relationship. `04 §7` (Jobs) is stale twice over already.

### Standing debts this session created

- ~~**S0, the synthetic-scale spike**~~ — **S0a done, S0b not runnable.** See *work that must be scheduled*. 1M is a spec for **row counts** and a hope for **the Tick**, and only the second was ever the risk `06` names.
- **Audit the corpus for other unratified numbers.** The 10k figure was never decided by anyone and silently sized five decisions. It will not be the only one.
- **`01-player §4` has never been grilled**, and the 1M target creates a governability problem there: placing individual service Buildings across 268 km² is a player-experience question with no current answer.
- **`06-roadmap` must be re-derived**, with S0 and S2 at the front.

### What lands on the rest of `05`

Sections **§4, §9, §3 and the budget** were worked in session six. **§1, §2, §5, §6, §7 (format half), §8, §10 remain unargued.** What they now have to absorb:

| From | What it demands of `05` |
|---|---|
| `adr/0029` transit | a third mode mask on the Road Graph, transit vehicles in Lane queues, and separated-band edges. **The traffic model just got bigger and the Microscopic Cap was already binding** |
| `adr/0032` school Trips | **+50% on the peak**, mostly walk Legs — which are Statistical, but are still Event Wheel entries |
| **The 1M budget** | every §5, §6, §7 and §10 figure must be restated at target scale. Chunk count, save size, snapshot marshalling, and the Microscopic Cap's binding share of the network all change by an order of magnitude |
| `adr/0031` Resources | nine Resources, one conservation invariant, and a **Bin with unbounded capacity** whose overflow-safe comparison is a determinism hazard |
| `CONTEXT` → Utility | a District-adjacency flow solve on a staggered schedule — small, but new, and it needs a slot |
| `adr/0030` Incidents | a new event source and a new dispatch Trip purpose |
| `adr/0026` note | **productivity growth means a Rule's output depends on employed Citizens' experience** — a coupling `CONTEXT` → Rule does not currently permit. *Half-answered by `adr/0033`'s derived apply count; what it may read is open* |
| `adr/0033` | a **wait list on every Bin** on the hot path of every write, a **cached Parking Shed per Building** invalidated by the Road Graph Epoch, and a **staggered Sweep Rule slot** in the performance build order |

Also outstanding and cheap:

- **`adr/0028` — "Difficulty is exogenous"** was offered at the end of the `01-player §5` thread and never answered; the number is still reserved for it. It passes all three ADR tests: hard to reverse, surprising without context, and a real trade-off with a recorded cost. Disaster world-scheduling folds in as a consequence.
- **`03 §2` (the Citizen model) was never reached.** Its 100× sizing tension is **closed** — 1M is the spec, 10k is the first hour — but the record itself is still unargued, and the 40-byte figure is stale: session five added a schooling accumulator, experience, and car ownership and none are reflected in it. Recompute rather than trust.
- **Documentation debt from session five**, listed so it is not discovered: `02 §2.4`'s service-coverage row is now an overlay spec rather than a mechanism; `04 §1`'s five-Good rule is superseded by the depth rule in `adr/0031`; `04 §7` (Jobs) was already stale from `adr/0026` and is now stale twice over; `06-roadmap` sequences neither transit nor any Service.

### What session five did **not** reach, and owes

The session opened on services and closed most of them, but three bundles were named and never argued in their own right:

- **Health.** Established as **Attended**, alongside schools rather than alongside fire — and nothing further. No mechanism, no failure mode, no link to any existing system. It is the only Service with no stated purpose in the causal chains.
- **Parks and recreation.** Given a home — *a park is an Amenity entry that is not a Business* — and nothing else. Whether recreation is only Amenity, or has its own Need, is open.
- **Variations within each bundle.** Raised explicitly in session five's first exchange and deferred every time: how many kinds of school, clinic, plant, station, and what distinguishes them. This is content that should follow the Ruleset work, but the *axis* on which variants differ should be settled before any are authored.

---

## Next up — new threads opened in session three

**Session order, and why:**

| # | Thread | Why here |
|---|---|---|
| 1 | **A — wages** | The only thing opened this session that can destabilise something already decided. Everything else is additive; this is load-bearing under `adr/0024`. |
| 2 | **B — Policy** | `adr/0024` leaves poverty as an absorbing state whose only counterplay lives in a system that does not exist. Until B lands, that is a mechanic with no exit, which is **not** what was chosen. Coupled to A, since transfers move money. |
| 3 | **C — Office** | Agreed in shape, unbuilt. Needed before the export side of the balance of payments is real. |
| 4 | **Ledger #1 — map size** | Needs *re-arguing*, not answering. Its pacing job moved to Hinterlands; it picked up a new one (how many outside economies exist). |
| 5 | **D — announcing the transition** | Detection is settled (crossover). The artifact is not. |
| 6 | **E — brownfield remediation** | Smallest and most self-contained; a Rule and a land designation. |

### A. Wages, and the second price system — **CLOSED, recorded in `adr/0026`**

**Was ledger #16.** Conserved Money forced it. Note the correction found on opening it: wages are the **third** price system, not the second — housing (`02-sim §5.6`) and Goods (`04-economy §4`) both already exist — and the real risk is not a third market but that **wages are the edge closing three loosely-coupled markets into one circuit**, which is a wage-price spiral.

Settled:

| Question | Answer |
|---|---|
| Wage mechanism | **Posted wages, adjusted locally per Business** by fill rate. No clearing loop. `adr/0017` applied to employers. |
| Stabiliser | **The Hinterland wage anchors it**, as import price already anchors Goods. All three markets anchor to the same authored object. |
| Thin markets | **Shrinkage toward the anchor, weighted by pool size.** True rather than a hack — a forty-worker market genuinely *is* anchored to the outside. Damping handles volatility; shrinkage handles thinness. |
| Wages move down too | Yes — so a well-connected Business pays less. **The wage surface is an accessibility readout**, and transport failure becomes visible in money. |
| Labour-starved Business | **Shrinks** as readily as it raises pay — `02-sim §5.9` decline. Fat margins pay up, thin margins contract. Agglomeration sorting falls out; bankruptcy is the tail, not the headline. |
| Commute Budget | **One shared exponential disutility curve plus a hard filter** — both already implied by the logit in `§5.4`. Cost is universal, compensation is individual, so the SC4 divergence trap is avoided. |
| Unemployment | **A real state**, not an instant Departure. Third Departure channel: **Destitute**. Five exits, one of which is a transfer. |
| Skill | **Three tiers**, jobs specify a *minimum* not a match, so underemployment is measurable. Fourth tier parked in `deferred.md`. |
| Progression | **Experience carries 1 → 2; only schooling reaches 3.** Event Wheel countdown on taking a job — the same machinery `adr/0011` uses for Life Stages. Protects the education → Office → exports chain from a bypass. |
| Employer demand | **A mix, not a tier.** Tier-3 employers need tier-2 support, so the city cannot skip stages and an all-Office city is impossible. |
| Job search | **The Provider List already says "workplaces."** Sticky, satisficing. Load-bearing: churn would reset promotion countdowns and delete tier 2. |
| Public employment | **Demand-determined by catchment**, so it can never absorb unemployment — the number of teachers is set by the number of children. Mild wage lag as a *gradient* in service quality, never a collapse. |
| Taxes | Now a **velocity control** as well as a revenue one, since they are the only private→public conversion. `04-economy §5` needs a third second-order column entry. |

Emergent results worth protecting in balance testing: **brain drain** (underemployed tier-2 workers leave for a Hinterland that pays their tier), **job stability produces skill**, and **transport as anti-poverty policy**.

### B. The Policy system — **structure settled; catalogue outstanding**

Settled, recorded in `CONTEXT` → Policy and `01-player §2`:

| Question | Answer |
|---|---|
| What is a Policy? | **A Rule, never a modifier.** Test: can `Evidence` expand it into named entities? Percentages preferred to flat amounts — a flat amount goes stale as the economy grows. |
| Toggled or parameterised? | **Parameterised**, meeting a real distribution. Thresholds are player-set, with a derived reference point shown beside them. |
| Scope | **Anything place-attached is District-overridable; global is the default level.** Only balance-sheet instruments (borrowing) are irreducibly global. |
| Cost | **Incidence, not affordability.** No setting favours everyone. Teeth come from the chains being circular. |
| Discovery | **Nothing is ever hidden or locked.** `NO VERDICT` applies to the interface. The B2 preview *is* the relevance signal, stating a fact rather than a judgement. |
| Two kinds | **Constraint** (acts on what gets built — preemption is the *normal* case) and **Flow** (acts on money already moving — reactive by nature). |
| Verb | **`Fund` + `Regulate` → `Govern`.** Five verbs: Zone, Connect, Service, Govern, Inspect. |

**Still open:** the actual catalogue — which Policies exist, their parameters, and their Rule bodies. That is content rather than structure, and it should follow the Ruleset work rather than precede it. Also unresolved: whether enacting a Policy is instantaneous, or needs friction/hysteresis to prevent flip-flop exploitation — the design already needs hysteresis for Segment Stress and the forestry frontier, so this would be the third instance and probably shares a mechanism.

### C. Office as a Zone family

Agreed in shape: clean, stacking, education-gated, export-earning — the sink that makes education non-decorative and the earner that pays the endgame's import bill. Open: whether it is one family or two, and whether it has density bands like Residential or is inherently high.

### D. Announcing the demographic transition

The crossover — the Day Households born here outnumber those arriving — is the detection mechanism. The artifact is not designed. Current lean: the instrument itself changes, with Retention promoted to the headline growth figure because it *is* the growth figure after the crossover.

**The rule that decides what gets announced at all, settled in thread A:**

> **Announce what the city cannot show. Never announce what it already shows.**

A boom has a visual signature — scaffolding everywhere, arrivals queuing at the gate, Segments going Microscopic — so labelling it adds nothing and converts an observation into a goal, which `NO VERDICT` forbids. The demographic transition has no visual signature whatsoever: same buildings, same traffic, and only the *source* of new residents has changed. Invisible by construction, therefore told.

### E. Brownfield remediation, and abandonment as the contagion vector

Named in `adr/0022`'s own list of endgame levers and now wanted: paying to unseal. It is the release valve that answers "permanent Sealing is a dead end" **without** touching the decay constant, which `adr/0022` explicitly warns against.

**Thread B added its reason to exist.** Neglect must not be *containable*, or ignoring a poor District is free. The contagion vector is **abandonment**, not crime:

> abandonment → neighbours' desirability falls → their failure pressure rises → more abandonment → land value falls far enough that redevelopment finally pencils → recovery

A **cycle, not a spiral** — the bid-price mechanism in `§5.5` damps it at the bottom, because cheap land eventually attracts something. Needs no new Map Layer and says *"empty buildings degrade a neighbourhood"* rather than *"poor people cause problems"* — which is the `NO VERDICT` distinction.

> **Crime was reopened in session five and the deferral overturned deliberately.** See [`adr/0030`](../docs/adr/0030-crime-is-an-incident-with-no-perpetrator.md). The defect in abandonment-only was practical rather than principled: **it fires exclusively in Districts that are already dead**, so it can be neither playtested nor intervened in. Unemployment gives the same cycle an **early** entry point. The `NO VERDICT` objection dissolves because unemployment here is not a property of people — per `CONTEXT` → Destitution it is *a reachability failure the player's network, zoning and policy produced* — so the story reads *"unreachable jobs cause problems."* Guardrail: **crime reads employment, never income, never tier, never Life Stage.** Police suppress the symptom and never the cause, so this thread's actual purpose — *neglect must not be containable* — survives: policing a dying District buys a quieter death. Crime **types** are deferred in `deferred.md` with a stated trigger.

### 1. How big is the map? — **CLOSED session six at 4096², and it was the target that was missing, not the analysis**

The table below was being answered against a **10k** figure that nobody had ever ratified, and which had drifted in and quietly sized `03 §2.1`, the Microscopic Cap, Chunk size, and the snapshot format. With the late-game floor set at **1M Citizens**, the same table answers itself:

| Map | Area @ ~4 m/Tile | 1M implies | Character | Corner-to-corner |
|---|---|---|---|---|
| 1024² | 16.8 km² | 59,500/km² — denser than Manhattan across *every* Tile including roads and parks | not credible | 35% of a Day |
| 2048² | 67 km² | 15,000/km² — Paris, Tokyo wards | compact megacity, one 8 km blob | 71% of a Day |
| **4096²** | **268 km²** | **3,700/km² — Los Angeles** | **sprawl; several genuinely separate cities** | 141% of a Day |

**4096², with 2048² as the documented fallback if spike S2 comes back badly.** The endgame is sprawling polycentric cities with interdependent Settlements — which is exactly the machinery `adr/0020` built and which no smaller map exercises. The three-quarters-of-a-Day corner-to-corner figure is not a defect; it is what makes far Settlements genuinely separate.

**What makes it survivable was already decided in #2.** Unlock by serviceability plus `adr/0021`'s sparse Chunks decouple map size from early-game sparsity — the first hour is a few hundred Tiles regardless, and unbuilt land costs nothing. Neither decision was taken for this reason, and together they are what make a 268 km² map possible at all.

**What it costs, and both were already the named risks:** route computation becomes the project's top risk rather than an optimisation choice — `adr/0020` calls it *"the binding constraint on world size"*, and at 16× the graph with ~400k Trips/Day, **S2 now decides whether the target is reachable**. And the Microscopic Cap binds far harder, so most of the network is permanently Statistical and `03 §3.5`'s divergence audit becomes load-bearing.

**Struck everywhere: the "10k ship target."** It is the first hour, not the ceiling.

> **Original entry retained below**, since its reasoning about Hinterlands and Settlement counts still holds and now applies to four edges of a much larger map.

**Blocks:** save header, `06-roadmap` milestone 10.

Now that Settlements are derived from commute range, map size is no longer "how big is the city" — it is **how many Settlements can exist**. A vehicle covers ~0.5 Tile/Tick, so the 480-Tick cross-town trip is ~240 Tiles and the commutable radius is roughly 480 Tiles.

| Map | Chunks | Corner-to-corner | Share of a Day | Forces |
|---|---|---|---|---|
| 512² Tiles | 16×16 | 724 Ticks | 17.7% | Monocentric but strained |
| **1024²** | 32×32 | 2900 Ticks | 35% | Far corners not commutable — **polycentric** |
| 2048² | 64×64 | 5793 Ticks | 71% | Several genuinely separate cities |

Density at the 10k ship target: 512² ≈ 2,500/km² (suburban, right); 1024² ≈ 600/km² (rural, sparse and sad).

**Recommendation: 1024², with the *buildable* area starting near 256² and expanding by serviceability.** Density early, range later, and the second downtown appears when the city has earned it. This makes question 2 load-bearing rather than optional.

**Counter to argue:** if the map should be open from Tick zero, 1024² is wrong and 512² is right, because an open 1024² map at 10k citizens is a bad first hour.

**Changed by session three, and this needs re-arguing before it is closed.** Hinterlands now pace the early and mid game, which was the job this question was carrying. Two consequences pull in opposite directions: map size no longer has to do the pacing work, which *frees* it — but the map has **four edges and therefore four Hinterlands**, so map size now also sets how many distinct outside economies exist, and a small map with one useful edge is a materially different game from a large one with four. Density resolves the other half: the citizens-per-km² figures above assumed a density model that now exists, so the table should be recomputed rather than trusted.

### 2. Open map, or progressive land unlock?

**Owned by** `01-player-experience.md §8 Q3`. **Now coupled to** question 1 — `adr/0020` established these are the same question.

If unlock, the existing argument stands that the gate should be **serviceability** — road network reaching the border, utilities with headroom — rather than a population or money threshold, so it stays a condition read off the map rather than a number in a config file. Progressive unlock also damps the population feedback loop in `adr/0011`.

**Recommendation: unlock by serviceability**, which is what makes the 1024² map work.

### 3. Outside Connection layout

**Blocks:** `06-roadmap` milestone 10, and it just became more important — under `adr/0022` a mature city imports permanently, so the Outside Connection is endgame infrastructure rather than a starting convenience.

Unresolved: how many; where on the edge; placed by the generator or built by the player; whether road, rail, and port are mechanically distinct or one abstraction with different capacities and prices.

**Recommendation:** generator places a small number at plausible edge locations (a road, a rail line, a river mouth); the player may build more at cost; they are one abstraction differing in capacity, price, and which Goods they favour. Port and rail being cheaper per unit for bulk Goods gives late-game import dependence a real infrastructure decision rather than a flat bill.

### 4. More endgame levers that restart the core loop

**Open research**, flagged as a real gap. `adr/0022` establishes that the late game must offer ways to reboot the loop rather than only bills, and names **replanting** as the first. Others need finding — likely candidates to research: redevelopment and brownfield remediation (paying to unseal), density transitions that reduce Materials per capita, transit reducing car dependence and therefore parking and Segment stress, and policy levers that change consumption rather than supply.

---

## Design forks, by owner

### `00-vision.md`

1. ~~**Is multi-modal transport in the vision at all?**~~ **CLOSED session five, `adr/0029`. Transit ships.** Not admitted for realism or genre convention — admitted because **five mechanisms already in the design had a cost and no counter-force**, and transit is the counter-force to all five (density's concentrated Access Point, destitution's second exit, parking's missing release valve, staffing a dense Office core, and the only tool that *merges* two Settlements rather than splitting them). The corpus was already written as though it existed: `CONTEXT` → Destitution names a bus line as an exit, and `CONTEXT` → Arterial already read *"highway, **rail**, major boulevard."*

### `01-player-experience.md`

2. ~~**Is transit ever implemented?**~~ **CLOSED with #1.** Right-of-way is the only axis; `Connect` places, `Govern` operates.
3. **Is car ownership a choice?** **Live, and half-answered.** Session five established that ownership is a **persistent Household state**, not a per-Trip fact — a purchase price plus a per-Day running cost, the third standing claim on Household money after rent and Food and the first that is optional — and that **mode is a Provider List attribute** (`adr/0032`). What remains open is the *decision*: whether a Household buys endogenously (bought when habitual Trips are bad without one, sold under pressure), which would make ownership an **outcome of urban form rather than an input** and let parking demand, congestion and Materials all follow one cause. The counter is that endogenous ownership is a feedback loop — bad transit → buy a car → more congestion → worse transit — and this design has already needed hysteresis in three places to stop that shape oscillating. Also unresolved: the poverty interaction, where an unemployed Household sells the car, freeing money and removing reach, so **the absorbing state tightens by the act of surviving.**
4. ~~**What does the education system actually look like?**~~ **CLOSED session five.** Schools are **Attended** Trips (`adr/0032`); schooling is **accumulated per completed Trip** across three levels mapped to `adr/0011`'s stages; primary is a **gate**, not a producer; university is an **In Education** state of a Young Household, not a stage. `adr/0010` and `adr/0026` no longer contradict each other — schools *produce* the city's own tier-3 workers on a Life Stage lag and *attract* educated Households immediately, at two completely different speeds. The *"rate rather than a count"* problem this entry named is answered by showing the **cohort in flight** rather than the output.
5. ~~**How is the intensity dial surfaced?**~~ **Closed session four, `01-player §5.6`.** A **Mode** — a named preset plus a lock policy, fixed at world creation — with three sub-dials underneath: Bill, Clock, Acts of God. Randomisation orthogonal, drawing within the Mode's range.

### `02-simulation-model.md`

6. ~~**Districts: player-drawn or automatic?**~~ **Closed by thread B — both.** Automatic by default, player-adjustable as an advanced action. The key finding: **District extent is bounded by the pooling abstraction's own validity** — a District can only be as large as the area within which "ignore transport" is defensible — so the count is physics rather than a design choice. That is what stops a one-District map from deleting Shipments and silently collapsing `adr/0022`. The early city has one District because the city *is* one neighbourhood; more appear as it outgrows the pooling radius. Splitting and redrawing is therefore a late-game advanced action that arrives exactly when one end of the map genuinely differs from the other.
7. **Where private capital comes from.** A regenerating pool is legible but arbitrary; deriving it from profits and savings is causally honest but adds a loop that could deadlock a struggling city. Probably derived, with a floor.
8. ~~**Does the player place service buildings, or zone for them?**~~ **Closed — a named exception, recorded in `01-player §2`.** The player places service Buildings and only those. What the player does *not* get is staffing: it is demand-determined by catchment, so the number of teachers is set by the number of children (`adr/0026`).
9. **TOML now, DSL later — or DSL now?** Parked in `deferred.md` with a trigger. Not relitigated.
10. **Multiplicative rather than additive utility** in the choice model (§5.4). SILO multiplies components so zero on any one yields zero total, expressing "no amount of cheapness compensates for zero reachable jobs" structurally rather than via a penalty.

### `03-agent-architecture.md`

11. **Audit rate and escalation policy.** ~~Divergence metric~~ **closed session four** — travel time against the statistical prediction, on audited *unstressed* segments only. Still open: how often the audit runs, and what happens on repeated divergence (permanent promotion? a flag for us?).
12. ~~**Do Microscopic segments form contiguous regions?**~~ **Closed session four, `03 §3.3`** — yes, via an event-driven force-promotion on downstream blocking. **Flagged for revisit with a build:** the interaction between force-promotion and the Microscopic Cap is untested, and a single arterial jam could in principle cascade promotions along its whole length and consume the Cap by itself.
13. ~~**What is the Microscopic budget, and is it player-facing?**~~ **Closed session four, `03 §3.9`.** Renamed the **Microscopic Cap**; a world constant, identical on every machine, not player-adjustable — else one Input Log yields different cities on different machines. **Removed from `01-player §6` as a failure mode**: it names a state of the simulator, not of the city, and a failure triggered by a config constant fails the project's own test for authored constants. Gridlock's indicator is now the commute distribution approaching the Commute Budget wedge.

    **Still open, and wanting real investigation:** the actual Cap value in a built system, and the general problem of **keeping gameplay mechanics decoupled from simulation resource constraints**. Gridlock was one instance of that coupling; the rule that fell out — *if an indicator would change when the simulation is optimised, it is not a trajectory* — needs auditing against every figure the game displays.
14. **Freight weighting.** Whether freight vehicles contribute to Segment stress identically to commuters. **Coupled to 14b**, since a weighting only matters if freight is mechanically distinct.

14b. **Is the vehicle fleet speed-heterogeneous, and does that force MOBIL?** **Sharpened and downgraded, session five.** The premise was wrong: a four-lane road is **four Lanes, four queues**, so a stopped vehicle blocks its own Lane and nothing else — and a bus stop therefore costs one Lane's capacity, scaling inversely with road width with no rule written. Discretionary lane changing is a **refinement, not a prerequisite**. The honest residual is narrower: *does traffic distribute across parallel Lanes at all?* If lane choice is made only for the next turn, a car can queue behind a bus with three empty Lanes beside it — not wrong at the road level, possibly wrong at the lane level, and visible. Roadmap work, and it is **dwell time** specifically; stops themselves are free.

14c. **How does a Service Building respond to a Disaster?** **Mostly closed session five**, `adr/0030` and `adr/0032`. Fire and police are **one mechanism** — dispatch reachability, producing a response Trip that can fail and an ambient suppression term — so the Dispatched mode is specified and `01-player §5.2`'s *"containment is an ordinary Trip that can fail"* now has machinery under it. **Still open, and it is the narrow part:** how many units dispatch, from where, and what *contained* means mechanically as a stopping condition. Nobody has argued the arithmetic of a response.

14e. **Does traffic have anything to say about transit vehicle capacity?** Session five settled that transit scarcity is a **gradient** — a full vehicle is the next one, and waiting is Leg cost against the Commute Budget — but nobody checked what happens when the wait exceeds the Budget for an entire corridor. That is the one place transit could fail hard, and hard failure is the thing this design says to suspect.

14d. **The real Microscopic Cap, and decoupling gameplay from simulation resource constraints generally.** Gridlock was one instance of that coupling and has been severed. The rule that fell out — *if an indicator would change when the simulation is optimised, it is not a trajectory* — has **not** been audited against every figure the game displays. Owed a pass.

### `04-economy-and-goods.md`

15. ~~**Do Businesses hold money, or only Bins?**~~ **Closed.** Yes — conserved Money requires every actor to hold a balance, and bankruptcy becomes a distinct diagnosis from input starvation. Businesses hold money; they do not hold Needs.
16. ~~**Is there a labour market price?**~~ **Superseded by thread A** — conserved Money forces a wage, so this is no longer a fork, it is unavoidable work.
17. **How does construction consume Materials?** **Raised in priority by `adr/0022`** — Materials imports are now the game's growth brake, so whether construction draws down over Days or in one transaction decides how sharply that brake bites and how visible it is.
18. **Does industrial pollution use the same Map Layer machinery?** Assumed yes. `adr/0022` now depends on it, since fertility composes from pollution.
19. **Should Outside Connection prices drift?** **Now unified with the Hinterland.** Goods prices, wages, and rents outside are one economic character per edge, not two systems that could contradict each other. If it drifts, it all drifts together, and the shock layer has one home. What remains open is only *whether* and *on what* — drawdown is the one non-arbitrary candidate found so far.

### `05-technical-architecture.md`

20. **Microscopic-Segment routing: HPA\* vs distance-vector.** Deferred to spike **S2**, which is **now the project's top risk rather than an optimisation choice.** `adr/0020` calls route computation *"the binding constraint on world size"*; with the map at 4096² and ~400k Trips/Day, S2 decides whether the 1M target is reachable at all. **2048² is the documented fallback.** Build the travel-time matrix first and measure what routing work is left.
21. ~~**Chunk size.**~~ **CLOSED session seven, `adr/0034` — and the question was malformed.** The seven roles were not in tension; three of the four claimed pulls dissolved under arithmetic, and the one role that genuinely wanted a small grid was **not a performance role at all.** Map Layer storage made Chunk size hash-bearing, which `05 §4` denied. Split into the **Cell** (32×32, design constant, frozen) and the **Chunk** (a multiple of it, hash-preserving, still to be measured and probably larger). **The Chunk's size remains open but is now a measurement rather than an argument** — and it stays on the *cannot be retrofitted* list.
22. **The snapshot interchange format.** Wants measuring early — `adr/0002` names marshalling cost as a trigger for revisiting the sim/render boundary, and 1M Citizens is the load that would trigger it.
23. **Is the travel-time matrix saved or rebuilt on load?** Leaning rebuild, treating it as a cache rather than state. Consistent with session six's ruling that wait lists are rebuilt rather than saved.
24. ~~**Where the Ruleset version boundary sits for saves.**~~ **CLOSED session six, `05 §7`.** Two policies split on the State Hash rule; Input Log gains a Ruleset content hash; saves gain a provenance trail; unaccounted loads mark a save hash-broken for the rest of its descent.

29. ~~**The Past/Future double buffer is a full-state copy per Tick.**~~ **CLOSED session eight, [`adr/0037`](../docs/adr/0037-the-world-is-single-buffered-and-hazards-are-per-table.md) — and the answer was subtraction, not machinery.**

    **One live world state. A table is double-buffered iff a *parallel* phase both reads and writes it** — which sorts to exactly two: **Lane dynamics** (Phase 4 Move is parallel and a Vehicle crossing a Junction reads another Lane's queue) and **Map Layer cells** (Phase 5, already double-buffered per `05 §9`). ~2 MB combined, against ~150 MB. Everything else is written only by the serial phases Settle and Growth, and a serial writer has no peer to race.

    **It was a naming artifact.** `adr/0002` said *"two world states"*, and once the words are *two worlds* a full copy is implied by the vocabulary. The property actually required — *a parallel phase must not observe a partially-updated peer* — is **per-table**, not world-level.

    **The three real consumers each got something cheaper and more specific:** the **saver** takes one real copy at save time (minutes, not Ticks); the **renderer** reads a published transform history one generation deep (<1 MB); a **panic** emits the last checkpoint plus the Input Log, which *replays into the failure under a debugger* rather than dumping a dead world — strictly stronger than `05 §8`'s original guarantee, using machinery that already existed.

    **An intermediate proposal was rejected on the way.** An undo journal (record pre-images, roll back on panic) was the first answer and turned out to be redundant — determinism plus the Input Log already do crash forensics better — which also deleted its one real cost, a **write barrier on the hot path**. *The first correct-looking answer was still one mechanism too many.*

    **What it cost:** `adr/0002`'s *"any thread may read the Past without coordination"*, traded knowingly for 50–150×, and a new correctness rule — **cold queries are serviced at a Tick boundary**, or a drill-down landing mid-Tick reads a torn world and shows a number that never existed.

    **Left open:** whether the transform history belongs to the library or the shell, which is downstream of `05 §6`'s unanswered *which thread runs `step()`*.

29b. **`adr/0004`'s *"tables partition naturally along Chunks"* is true for static entities and unargued for mobile ones.** Buildings, Lots and Lanes have fixed positions. Grouping **Households or Citizens** by home Chunk means a Household moving house **relocates its row** — and a relocated row is worse than a stale one, because `05 §3`'s generation counter detects use-after-free but cannot detect *this handle now points at someone else's valid row*. Either rows never move and the Chunk partition is a separate index, or rows move and handles need indirection. **`05 §5` role 3 (parallel work assignment) leans on the claim**, and `adr/0037` flagged it rather than closing it.

    `adr/0004` states it plainly: *"advancing a Tick is a bulk copy of the hot columns plus writes into the Future."* At 1M Citizens the hot state is on the order of **80–150 MB** (~40 MB of Citizens alone at `05 §3`'s 40-byte figure, which is itself stale and understated). A `memcpy` of that at 10–20 GB/s is **8–15 ms per Tick** — 13–24% of the 62.5 ms budget at the reference rate, and **50–100% of the 15.6 ms budget at 4× speed**, spent copying bytes that did not change.

    **Three things make this worse than a line item:**

    - **It defeats the Event Wheel.** `05 §9`'s premise is *"a Citizen at work for a third of a Day consumes zero CPU for a third of a Day."* A full-state block copy touches every sleeping Citizen every Tick. `adr/0006`'s rule — cost proportional to activity, never to population — is violated by a structure two ADRs over, and neither document cites the other.
    - **It is memory-bandwidth bound**, which is the exact failure `05 §6` quotes Factorio's electric-network attempt for. The document names the hazard and then contains an instance of it.
    - **It is language-independent.** Rust copies the identical 150 MB. This is *not* a C# problem, and any language bake-off that does not isolate the copy will report "the language is too slow" when what it measured was `memcpy`.

    **What it is not:** an argument against the Past/Future split. `adr/0002` buys three things with it — safe parallel reads, asynchronous saves, crash forensics — and all three are worth keeping. The fork is over the *copy*, not the split.

    **Candidate answers, none argued:**

    | | Shape | Cost |
    |---|---|---|
    | **Copy-on-write by Chunk** | Copy only Chunks a Tick actually touched; the Event Wheel already knows which | Dirty tracking already exists (`05 §5` role 1). Needs a stated rule for cross-Chunk writes |
    | **Write-through with an undo log** | One live state; record the pre-image of every write; the Past is reconstructed by replaying the log backwards | Cheapest per Tick, most expensive per read. Breaks *"any thread may read the Past without coordination"* |
    | **Split by mutability** | Only fields a Tick can mutate are double-buffered; static fields are single | Probably a large win for near-zero effort, and it is a question about `05 §3`'s hot/cold split rather than a new mechanism |
    | **Generational ring** | Buffers are published generations with reader leases, not two fixed halves | Fixes buffer *lifetime* (renderer interpolation needs two live states; an async save needs one for its whole duration) but **not** the copy cost. Orthogonal, and also owed |

    **Why it is urgent rather than merely open:** it is a data-layout decision, so it belongs on the *cannot be retrofitted* list beside determinism, the renderer, Chunk size and routing. Every table written before it is settled is a table that has to be revisited. It is also the first thing S0 will measure whether it means to or not.

    **It is entangled with the threading question `05 §6` never answers** — which thread runs `step()`, and what a reader of the Past is permitted to hold across a Commit swap. Session eight opened on that and had not finished it. Both are owed together.

### Unowned — no document covers this

25. ~~**The utilities network.**~~ **DISSOLVED session five — it was mis-scoped in both directions.** Three of the four bundles it implied already had homes and were load-bearing there, and the one genuine novelty turned out not to need a network at all.

    **Utilities ride the Road Graph and pool by District** (`CONTEXT` → Utility). A Lot already requires road frontage, so every Building is connected by construction — no second network to draw, route, save, version, or revalidate, and an entire class of *why isn't this working* deleted with an entire input mode. Distribution reuses `adr/0013` exactly. **Power, Water and Sewage are Resources of the Utility family** (`adr/0031`), distinguished from Goods by needing no Vehicle and from each other by **storage**, which is not the same field as Bin capacity. What remains a decision is plant siting, which is the same argument as siting any dirty industry.

    **Utility failure is unblocked as the fourth Disaster** — destroying a plant drops District supply, which is a gradient the whole city reads, with no new machinery.

    **Sewage's one genuinely novel idea is parked, not lost:** gravity-fed drainage over real terrain is in `deferred.md` with a trigger. It is not forbidden by `adr/0021`, which bars terrain only from a *Tick*.

26. **What is health actually for?** Established as **Attended** and nothing more. Every other Service has a stated place in a causal chain — schools feed the labour pipeline, police enter the decline cycle, utilities gate construction, parks feed Amenity. Health has none, and a Service with no chain is a Service that will end up as a desirability bar. Note the design has **no illness, no mortality, and no aging** (`adr/0010`), so the obvious mechanism is foreclosed and something else has to justify it.

27. **Is recreation only Amenity?** `adr/0032` gives a park a free home by widening `CONTEXT` → Amenity from *"Business types reachable on foot"* to *destinations* reachable on foot. Open: whether that is the whole of it, or whether recreation carries its own Need. If Amenity is the whole answer, parks are the cheapest Service in the game and that should be stated deliberately rather than by omission.

28. **On what axis do Service variants differ?** Raised in session five's opening exchange and deferred every time it came up. How many kinds of school, clinic, plant, station — and, more importantly, what *distinguishes* them. Capacity alone makes variants a ladder the player graduates up, which is exactly the error `adr/0025` refused for Density. The axis should be settled before any variant is authored, even though the catalogue itself is Ruleset content.

---

## Not a fork — work that must be scheduled

- **S0 — the synthetic-scale spike. Highest priority, and it validates the whole of `05`.** Generate a **1M-Citizen city in `Borough.Headless`** and measure the Tick: tables at target size, the Event Wheel, Bin Rules with wait lists, a Sweep Rule pass, and a routing load. No renderer, no gameplay, no content. Until this exists, **1M is a hope rather than a spec**, and every system built on top of it is built on an unvalidated assumption. It is cheap now and it is never cheaper.

    **PARTLY DISCHARGED, and running it found that this entry is four clauses wearing one name.** Only the first — *tables at target size* — was ever reachable without the Rule engine; the Event Wheel is slice 9, Bin Rules with wait lists is slice 7, and a Sweep Rule pass is slice 10. **This is the third instance of the shape the board tells itself to audit for**, after `adr/0003`'s owed validation and `06`'s K1/K2: a single item whose stated blocker covers only part of what it names. Split into **S0a** (done — [`spike-results`](../docs/spike-results.md), [`0003`](0003-build-plan.md)) and **S0b** (not runnable). **S0a closes the sizing half outright**: 86 MiB of tables at 1M, linear to 4M, 100,000 Ticks in 11.75 s with nothing trending — so *1M is a hope* is retired for **row counts** and stands for **the Tick**, which is what `06` actually names as the risk. **Two claims it measured that nobody had typed**: one State Hash costs **2.08 Tick budgets** at the target, and the Decide guard costs **4.9**, against a `05 §9` that mentions neither and that records `adr/0037` deleting the double buffer for costing less than either.

    Its companion is **S2 — routing (HPA\* versus distance-vector, `05` open question 1)**, which stopped being an optimisation choice when the map closed at 4096²: it decides whether the target is reachable, and 2048² is the fallback if it comes back badly.

    **The standing rule this sits under:** *"we'll find optimisations along the way"* is true in general and false for a specific short list the corpus has already identified — determinism (`05 §4`, *"must exist from commit #1"*), the renderer (`05 §10`, CS2's *"has to fight every asset already made"*), Chunk size (*"expensive once save files exist"*), and now routing and scale. Anything on that list is validated early or not at all.

- **S4 — the kernel benchmark. Runs *before* S0, and it is a few hundred lines.** Six microbenchmarks over synthetic arrays at target row counts. **No simulation, no gameplay, no Rules, no routing, no city** — it measures the machine's response to the *shapes* this design makes, not the design.

    It exists because the language question (`adr/0001`'s unargued half) and ledger **#29** both need a number, and the obvious way to get one — build the core twice — costs more than the decision is worth. **A measurement is only an honest deferral when it is cheaper than the thing it defers.** S4 is; a bake-off of S0 is not.

    | | Kernel | What it decides |
    |---|---|---|
    | **K0** | Allocate the whole world at 1M and report the **actual footprint**, per table | Ledger #29's copy cost, and the size of the Past/Future 2× claim. `05 §3`'s 40-byte Citizen is admitted stale — session five added a schooling accumulator, experience and car ownership |
    | **K1** | Linear scan-and-update over three SoA columns, 1M rows, **in a `checked` and an `unchecked` variant** | Throughput ceiling; bounds-check elision; and the cost of `checked`, which is the one claim in `adr/0003`'s overflow policy with no arithmetic behind it |
    | **K2** | Random gather by generational handle — ~2k handles into 1M rows, three columns each | **The Event Wheel wake pattern.** The memory-bound one, and the one §6's Factorio rule is about |
    | **K3** | Bulk copy of the K0 footprint | **Ledger #29 directly.** Language-independent, and it must be isolated or it will be misattributed to the language |
    | **K4** | Many lookups into small sorted arrays (≤ 9 entries) | The `ResourceMap` (`05 §3`). Cache behaviour, not algorithmic |
    | **K5** | Wheel bucket drain and reschedule across 8192 buckets | Random writes across a large structure — the wheel's own cost, which nothing has ever sized |
    | **K6** | Hold the whole K0 heap live and run K1–K5 in a loop for **10 minutes**; histogram the per-iteration time | **The GC tail.** The only kernel that can genuinely surprise, and the only one a median hides |

    **What it reports:** for each kernel, achieved throughput against the machine's measured `memcpy` bandwidth and a hand-computed ideal — *not* against a Tick budget, because these are not Ticks. For K6, the **p99.9**.

    **Written once, in C#. It is a tripwire, not a gate.** The second implementation is written only in the branch where it changes the answer: any kernel worse than ~3–4× off its ideal, or a K6 pause exceeding **15.6 ms** (the 4×-speed Tick). Absent that, the language is settled by argument and S4 has confirmed the argument rather than replaced it.

    **What it must not become:** a benchmark of the simulation. That is S0, it is expensive, and running it against two languages is the cost this spike exists to avoid.

- **`06-roadmap` needs re-deriving, not patching.** It sequences work that predates conserved Money, Hinterlands, Office, the labour system, transit, every Service — and now the 1M target and the 4096² map. S0 and S2 belong at its front.

- **Generation with playability guarantees.** A seed producing no buildable land or no water access is a broken map, not a hard one. Named in `adr/0021` so it gets scheduled rather than discovered.
- **Dial playability floors.** Second instance of the same rule (`01-player §5.6`). A Hinterland at minimum depth and minimum recovery produces no immigration at all, which breaks §3's opening outright. Every Mode is a hand-validated point; the free sliders need floors.
- **Hazard Region derivation.** Floodplain and Woodland hazard maps are computed at world generation and never read during a Tick, so they belong to the generator's output alongside terrain — not to the Ruleset.
- **Hysteresis on the extraction frontier.** Forestry Buildings declining and regrowing will flicker without it, same as Segment Stress.
- **Evidence for composed fertility.** A farm's panel must decompose its own yield by source, or the whole `adr/0022` mechanic is inexplicable.

---

## Standing caveat — **DISCHARGED session eight**

> ~~**`adr/0001` (Godot + C#) has not been through this process.** It was written from research rather than argued, and everything downstream — the snapshot format, the threading policy, allocation discipline in the hot arrays — currently assumes it. Nothing decided so far depends on the *language*, and it should be kept that way until `adr/0001` is either grilled or confirmed. Treat any argument that reaches for a C#-specific fact as suspect until then.~~

**Discharged, and the caveat was pointing at the right thing for the wrong reason.** `adr/0001` was not under-argued — it was **mis-scoped**. Its argument for the *host* is sound and survives untouched; what it never argued was the **core's language**, which it settled by inheritance, and the **runtime**, which it never named. See [`adr/0036`](../docs/adr/0036-the-cores-language-is-a-separate-decision-and-it-is-csharp.md).

The caveat's operative instruction — *treat any argument that reaches for a C#-specific fact as suspect* — no longer applies, and the audit it was protecting against found that **only one of seven CI lints is C#-specific**. The rest of `adr/0001`–`0009` remain 🔴 and the caveat's general form still stands for them: written from research, not argued.
