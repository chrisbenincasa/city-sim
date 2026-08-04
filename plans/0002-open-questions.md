# Open Questions — resume here

A consolidated ledger of everything still unresolved, ordered so it can be picked up cold. Each entry states the fork, what it blocks, and a recommended answer to argue against.

The per-document `Open questions` sections remain authoritative for their own areas; this file is the index and the ordering. When something here is settled, close it **in the owning document** and strike it here.

Run `/grill-with-docs` to continue. **Session nine opens on `02 §7` + `adr/0006`, then `02 §4` residue + `adr/0015` — the last four items of the Phase 1 gate.**

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
| `06-roadmap.md` | 🔴 | **Never grilled**, and now materially out of date: it sequences work that predates conserved Money, Hinterlands, Office, and the labour system. Milestone ordering should be re-derived rather than patched. |
| `deferred.md` | 🟢 | Maintained rather than grilled, which is correct — it is a record, not a design. |
| `references.md` | 🟢 | §9 added this session. The gap it had (genre prior art) is closed. |
| `adr/0001` | 🟡 | **Grilled session eight, and the finding was mis-scoping rather than under-argument.** Its host argument is sound and untouched; it had also settled the core's **language** by inheritance and its **runtime** not at all. Split — `0001` now decides the host only, and [`adr/0036`](../docs/adr/0036-the-cores-language-is-a-separate-decision-and-it-is-csharp.md) owns the core's language. Its Bevy rejection is corrected: it was filed under *Rust*, and three of its four arguments dissolve against Godot-shell-plus-Rust-core. |
| `adr/0002` | 🟢 | **Rewritten session eight.** It sized its boundary against a *renderer* when the actual consumer is an *inspector* — *"roughly two methods"* against ~20 required entry points, of which the largest family went unmentioned, and its own second revisit trigger had already fired on the day it was written. Rebuilt around **hot/cold query flavours**, with persistence explicitly off the axis. |
| `adr/0004` | 🟢 | **Grilled session eight, with ledger #29.** Its layout claim survives untouched and was never the problem; a **buffering strategy** had ridden into its Consequences and was protected by a *"Not performance"* revisit trigger. Replaced by `adr/0037`. One crack left open as **#29b**: the Chunk-partition claim holds for static entities and is unargued for mobile ones. |
| `adr/0003` | 🟡 | **Opened session eight.** Its *"zero transcendental functions"* claim was false and **hard-blocking** — it bans `Math.*` while `02 §5.4` requires `exp`, so the choice model was unimplementable as written. Fixed-point tabulated `exp`/`log` are now required core components at a **stated resolution**. **Still ungrilled: `02 §8`'s rule list, Q16.16's scope and overflow policy, and `02 §10`'s testing strategy** — which together are milestone 1. |
| `adr/0005`–`0009` | 🔴 | **Written from research, not argued.** All six gate Phase 2: `0005`/`0007` (fidelity tiers), `0006` (the Event Wheel's own rule, and it gates milestone 4), `0008` (walking), `0009` (parking), `0012` (routing intent). |
| `adr/0037` | 🟢 | Session eight. One live world state; hazards classified per table. Deletes ~150 MB of copying per Tick, redefines the Past as a phase-discipline fact, and strengthens crash forensics using machinery that already existed. |
| `adr/0036` | 🟢 | Session eight. The core's language, argued on the convergence finding: all three candidates produce the same code, so the decision falls to the surrounding factors. Adds the seventh CI lint, the intrusive-index-list rule, spike **S4**, and K6 as the revisit trigger. |
| `adr/0010`–`0022` | 🟢 | Sessions one and two. |
| `adr/0023`–`0026` | 🟢 | Session three. `0026` gained a session-five superseding note: the tier wall re-justified as a **category boundary**, within-tier experience, productivity growth, and the finding that **the city has no central bank and correctly so**. |
| `adr/0029`–`0032` | 🟢 | Session five. Transit, crime, the Resource generalisation, and services-by-Trip. `0028` remains **reserved** for the unwritten difficulty-is-exogenous ADR. |
| `adr/0035` | 🟢 | Session seven. Infrastructure priced by what it consumes; Upkeep as consumed design life. Answers `0014`'s corridor question and retires its "probably budgeted". Adds capital expenditure and maintenance to `04 §5`, which had neither. |
| `adr/0034` | 🟢 | Session seven. Fields sorted by source geometry; the Cell split from the Chunk. Carries superseding notes into `0022` (Sealing is per Cell) and `0015` (the spatial world-creation constant is Cell size, not Chunk size), and makes `0014` retroactively load-bearing for the noise model. |
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

- **Is `mean_workforce_experience` a legitimate Building Readout?** `CONTEXT` → Building forbids a field *"that would have to be averaged across its Occupants."* Workers are not Occupants and production is logistics rather than decision, so this reads as permitted — but it is close enough to the invariant to want one sentence stating why, rather than being left to inference.
- **Labour as an input Bin** was reached for as the clean answer to staffing, and it is a real change to how employment works. `adr/0026` has jobs as a Household↔Business relationship; a labour Bin filled by arriving Trips is a *second* representation. They need reconciling, and `04 §7` (Jobs) is already stale twice over.
- **Fallback chain depth.** `on_fail` chains are the whole diagnostic story and are unbounded. Nine Resources and a Policy layer will make them longer; nothing states a limit or a cycle check. Untouched.
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
| **Solution scaffolding** — four projects, CI, both `adr/0002` boundary lints, the `0036` unmanaged-struct analyser | `0001`, `0002`, `0036` |
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

## Resume here — session nine

**Open on `02 §7` + `adr/0006` (the Event Wheel, milestone 4), then `02 §4` residue + `adr/0015` (the Rule engine, milestones 3a/3b). Those four are the last of the Phase 1 gate.**

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
- **Ledger #29b** — `adr/0004`'s Chunk-partition claim for **mobile** entities. `05 §5` role 3 leans on it.
- **Audit the corpus for other unratified numbers**, and — new this session — **for decisions welded inside a single ADR title**, and **for revisit triggers that were already satisfied on the day they were written.** `0002`'s was; it is unlikely to be the only one.
- **`01-player §4` has never been grilled**, and 268 km² of individually-placed service Buildings is a governability problem with no answer.
- ~~**`06-roadmap` must be re-derived**, with S4, S0 and S2 at the front.~~ **PARTIALLY DISCHARGED** — Phase 0 and Phase 1 are re-derived in [`0003-build-plan.md`](0003-build-plan.md), with S4 at the front and S2 flagged as the top risk on its own track. **Phase 2 and Phase 3 remain un-re-derived**, and correctly so: `06` sequences work that predates conserved Money, Hinterlands, Office and the labour system, and re-deriving it before `03 §5` is grilled would be re-writing it twice.

### Unratified numbers and absent classifications, found while decomposing Phase 1

Rule 6 of [`0003`](0003-build-plan.md) applied to the planning itself. **Building will generate these faster than arguing did**, and each is recorded here on the day it was chosen rather than after it has been repeated until it reads as settled.

- ~~**The tabulated `exp`/`log` table resolution has never been stated.**~~ **CLOSED, in [`adr/0038`](../docs/adr/0038-the-transcendental-tables-are-sized-by-the-representation.md): 256 entries per table, rounded linear interpolation, base-2 range reduction.** The number falls out of a stopping rule rather than a preference — **the table must not be the thing limiting the answer**, and at 256 entries it contributes 0.12 ULP of a ~1 ULP total while Q16.16's own rounding supplies the rest. At 128 the table is still a term in the answer (0.72 ULP on `log2`); at 512 it pays a kilobyte to shrink something already an order below the floor. **The precision demand turned out to be five orders weaker than a numerics instinct suggests**, because `02 §5.4` scales utilities so meaningful differences are 1–3 units and only differences matter. Errors also run in the safe direction: quantisation perturbs utilities, which is equivalent to *lowering* `μ`, away from the stampede limit — asserted by a test that the tabulated softmax is never sharper than a double-precision oracle.
- **`adr/0003`'s owed validation is half discharged, and the other half was never blocked on `adr/0005` in the way it read.** The ADR required the resolution be validated against `adr/0005`'s herding behaviour, which is 🔴, and that had been carried as a single blocked debt. It is two: the *does the city feel herdy* half genuinely needs a running choice model and remains owed, but the *does the table change the answer* half needs no city at all — run `02 §5.4`'s softmax through the committed table and through a double-precision oracle over candidate sets including exact ties and near-ties, and compare selection probabilities. Worst divergence is **below 0.001**. **That test was available from the day `adr/0003` was written**, and the debt sat undischarged because it was recorded as one item rather than two. *Worth auditing the other 🔴-blocked debts for the same shape — a validation that is partly runnable now, filed behind a grilling session it does not actually need.*
- **The choice model has a hard horizon at ~11.1 utility units, and `μ` moves it. Nobody had noticed the coupling.** Q16.16's smallest positive value is `1/65536`, so `exp` underflows to *exactly zero* below `-11.09` — a candidate that far below the best is **impossible rather than unlikely**. That is defensible on its own, but the argument to `exp` is `μ·V`, so **doubling `μ` halves the horizon to 5.55 units.** `02 §5.4` calls `μ` *a free design knob* and suggests exposing it as a difficulty or realism setting; it is not only a sharpness knob, it also decides where options stop existing, and the two effects compound in the same direction. Recorded in `adr/0038` as the consequence to argue with. **A finer table does not move it** — it is a property of representing probabilities in fixed point at all, and only a wider representation would.
- **Map Layer diffusion cadence is classified as tuning and probably is not.** `02 §1.2` lists it in the tuning column, but cadence decides when a source becomes visible to a Rule reading the Cell, so two cadences produce two cities — a **design change** under `05 §4`. **Same welding failure `adr/0034` found in Chunk size, one document later**, which is the second demonstration that the hash rule only works if somebody runs it against each number *by name*.
- **The industrial-pollution kernel radius and shape are unstated.** `02 §2.4` grounds the range in reality (1–10 km plumes) and names no kernel. *Author in domain units* applies to the machinery, not only to the balance constants.
- **The CI lint count disagrees across three documents** — `05 §4` says seven, `adr/0036` says six and calls itself the sixth, this file calls it the seventh. One rule, three counts. Cosmetic, and it is how a checklist stops being checkable.
- **Spike results had no home.** `06` says *record them; delete the code* and names no file. Created as [`docs/spike-results.md`](../docs/spike-results.md).
- **Ledger #29b needs a Phase 1 working answer**, recorded rather than assumed: **rows never move, and the Chunk partition is a separate index.** `05 §5` role 3 leans on the other answer and S0 will measure it whether it means to or not.
- **Chunk size is still unmeasured** and still on the *cannot be retrofitted* list. Phase 1 proceeds at **Chunk = Cell**, provisional, pending S2.

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
- **`05 §6` states no GC configuration and now has evidence for one.** K6: **server GC with background
  collection on**, and `<ConcurrentGarbageCollection>false</...>` recorded as a **prohibition** rather
  than a preference. It costs the shell 3–4.6× at the tail and buys the core nothing, and it is precisely
  the knob a latency-conscious developer reaches for on the reasoning that background collection adds
  overhead.
- **`adr/0036`'s revisit trigger has been restated** from a p99.9 to a maximum and an over-budget count,
  because K6 showed the quantile cannot see the event the trigger exists to detect — the run whose worst
  iteration was 100.2 ms read 2.462 ms at p99.9, and in half the GC matrix p99.9 ranked the *rejected*
  design above the chosen one. **Recorded here as a pattern rather than only as an edit:** the standing
  debt above to audit for *revisit triggers already satisfied on the day they were written* now has a
  sibling — **audit for revisit triggers whose statistic cannot detect the thing they name.** A trigger
  that cannot fire is not protecting anything, and `0036`'s is unlikely to be the only one.
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
- **The GC churn rate K6 assumed — 44–52 MB/s, one object in sixteen promoted — is a guess at what the
  shell, the UI and the per-frame snapshot allocate.** Nothing in the corpus states it. Without churn
  there is no collection and no pause whatever is held live, so this one number sets the scale of every
  K6 result **including the one that cleared `adr/0036`'s trigger**.

**Measurement owed, and it dies with the harness.** `spikes/S4.Kernels/` is deliberately **not** deleted
yet — task 11 is held — because the following need it and reconstructing it from git history would cost
more than keeping it:

- ~~**K0, and then K1/K2/K5, on the Apple M4 Pro.**~~ **DONE**, and it earned its cost — see
  [`spike-results.md`](../docs/spike-results.md), where every kernel now carries an *On the second
  machine* subsection. **Three conclusions turned out to be properties of the desktop rather than of the
  design** (the threading payoff, K2's array-of-structs advantage, K3's per-column copy penalty), and one
  methodological defect surfaced only from the disagreement, recorded as a pattern below.
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
- **K6 has never run on the M4 Pro and is the one kernel resting on a single machine.** Every other
  kernel now has two, and K6 is the one carrying the GC verdict that `05 §6` is about to adopt.
- **Re-measure the M4 Pro baseline on a machine confirmed quiet, and re-derive every M4 *vs ideal*
  figure from it.** `results/kernels-apple-m4-pro.md` claims its denominator was *"measured in the same
  sitting"*; the timestamps are **42 h 44 min apart** (2026-08-03 00:42 against 2026-08-04 19:26), on
  the one machine with no governor control, no turbo switch, no thread pinning and an unrecorded
  background load. **This is cheap to fix — a ten-second window, about a minute of wall clock** — and
  until it is fixed every ratio-to-ideal in the M4 subsections is provisional. **The conclusions are
  not**: they are within-process variant ratios and are immune to the denominator, which is why the
  fold-in survives the defect. Do it in the same sitting as K6, on a quiet machine, before the harness
  is deleted.
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
- **Does a Statistical Trip need a concrete path, or only an arrival time?** The corpus has never asked
  this in writing and the two answers give different games. A Traveller on a Statistical Segment is
  defined as *an origin, a destination, and an arrival Tick* — which needs no path — but Stress is
  `volume / capacity`, and volume has to be accumulated from something that knows which Segments a Trip
  traverses. **If volume comes from a periodic assignment over the matrix, per-Trip routing collapses to
  the Microscopic Cap's scale and the Cap is the constraint rather than the router.** If it does not,
  the floor is ~232 searches per Tick before cache hits. `0010` task R2 measures the ratio, and it is
  the number that decides how much of the rest of that spike matters.
- **The route cache has no eviction policy anywhere in the corpus.** `adr/0012` permits caching keyed by
  origin-destination pair, `adr/0006` forbids collections that grow and has reversal criteria of
  *"Nothing."*, and nothing joins the two. `adr/0017` shows the pattern — fixed capacity, least-used
  eviction. Owed to `adr/0012` as an amendment.
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

**From slice 2, building the arithmetic substrate** — tasks 1, 2, 3 and 7 of
[`0005`](0005-arithmetic-substrate.md). The first code in `Borough.Core`, and three findings arrived
within an hour of it existing, which is rule 6's prediction landing rather harder than the planning
did. Tasks 4, 5 and 6 — tabulated `exp`/`log`, `draw()` and `PurposeTag` — are not started, and the
`exp`/`log` resolution remains the decision that slice cannot close without.

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

- **S0, the synthetic-scale spike** — see *work that must be scheduled*. Until it runs, 1M is a hope.
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
