# 0073 — The city attracts people

**[`0045`](0045-amnesty.md) queue row 31. Implementation plan, expanded 2026-09-11 against
`main` at `7ea9ab5`.** Continues [0068 D6](0068-the-choice-model.md#decisions) and the stock half of
[`adr/0023`](../docs/adr/0023-immigration-arrives-through-the-gate.md).

## Status, authority and scope

**BUILT AND DEMONSTRATED.** Tasks 1–8 are built and the observation episodes are taken; the
completion record at the end of this file carries the evidence and the defects the work found. The
scoping text below is kept as written. This replaces the initial scoping draft's three unanswered branches with
explicit implementation choices. **The user confirmed three design decisions during planning:**
Life Stage housing preferences participate, willing prospects queue outside with timed
reconsideration, and Outside population recovers towards its resting stock from either direction,
with additions and losses counted. The remaining concrete choices below are the plan author's
implementation decisions under the request for a comprehensive handoff. No unanswered design
branch is delegated to the implementer.

**The risk is growth supplied by a caller.** A city that can persuade somebody to enter but needs
repeated `Arrive` commands to supply the people has only half an immigration mechanism. This row
connects population stock, composition, reconsideration, choice, gate throughput, admission,
placement, Departure and replenishment, with an account and a picture of the whole circuit.

The relevant roadmap capabilities are milestones 11 and 16. Their built gate and comparison are
inputs to this work. This row does not clear milestone 19's other gates or absorb row 30's
failure-source work. No new ADR, no entry in `0002`, and no implementation merely to make this
planning session committable: amnesty standing order 5 leaves a documentation-only change uncommitted.

The implementation includes:

- Shared population stock per map edge, selective depletion, replenishment and counted losses.
- Actual adult/child composition and adult Skill Tiers; consistent prospect Money and preferences.
- Autonomous occasions inside `Simulation`, an outside queue, existing gate admission and placement.
- Return of every existing Household Departure, including locally formed and subsequently evicted
  Households, without retaining departed identities.
- Population reconciliation, save/reload, deterministic replay, Ruleset transitions, headless
  evidence, shell inspection and a driven demonstration.
- [0068 D5](0068-the-choice-model.md#decisions), the moving-friction arithmetic refusal.

**Explicit boundaries.** No wage, school-quality, service or job-access term is added to housing
utility. No Shock system, Intensity Dial, Outside city simulation, Outside treasury, arrival traffic
before admission, physical Departure Trip, new failure source, or Building asset is required.
Entry remains an event at a gate; placement starts the built move-in Trips. Existing abstract
Departure is retained and gains an accountable destination. These are limits on what the row may
claim, not unassigned work the implementer must discover.

## What inspection established

These are findings from source, not measurements of a demonstration.

| Finding | Symbols inspected | Required response |
|---|---|---|
| Number presenting and composition come from the caller. | `Simulation.ApplyArrive`, `ArrivePayload`, `ArrivalDump.Run` | Generate occasions in the simulation, not by issuing commands from the runner. |
| Hinterlands have configuration but no mutable population. | `HinterlandDefinition`, `Ruleset.TryHinterland` | Declare saved stock tables owned by `World`. |
| A prospect stops at its first feasible sample, sets centrality weight to zero and compares rent. | `PlacementEngine.ProspectCrosses`, `Utility` | Give the prospect its actual preferences and use the same utility kernel as residents. |
| Life Stage centrality preferences already exist for residents. | `Ruleset.CentralityTaste`, `PlacementEngine.TryHouse`, `Reassess` | Reuse them; do not invent another centrality axis. Add only the missing rent weight specified below. |
| The affordability purse and admitted purse use different identities. | `ProspectCrosses`, `World.TryArrive` | Carry the evaluated purse into admission without redrawing it. |
| Gate quota counts Households admitted per Day, lazily reset. | `World.TryArrive`, `BuildingTable.ArrivalDay`, `ArrivalsToday` | Keep quota separate from willingness and stock. Read yesterday's meter as zero today even before the first arrival. |
| Admission creates adults through `CreateCitizen`; stage alone creates no children. | `World.TryArrive`, `AddMember`, `Bear` | Create imported children explicitly, without recording them as births. |
| `AddMember` starts everybody at Skill Tier 1. | `World.AddMember`, `CitizenTable.SkillTier` | Initialise imported adults to the stock's declared tier before returning control to a simulation pass. |
| Children leaving home move existing Citizens. Dissolution destroys them. | `SpawnChildren`, `FormHousehold`, `Dissolve`, `DestroyHousehold` | Household creation is not population creation. Count people at the actual source and sink. |
| Illness already kills individual Citizens through the generic destruction helper. | `CivicEngine` illness-death branch, `World.DestroyCitizen` | Add a classified illness-death operation; those deaths must not appear as scenario removals. |
| Departure removes Money and people, and credits no Outside. | `World.Depart(Handle<Household>)` | Credit population before freeing the members; retain the existing Money boundary. |
| A Pool origin is a gate handle; it may be stale or absent. | `UnplacedPool.GateAt`, `PlacementEngine.TryOutside` | Save source edge separately and never make the give-up sink depend on a live origin Building. |
| Phase 6 runs Life Stages and Policies before placement. | `Simulation` phase 6 body | Insert autonomous arrivals immediately before `_placement.Place`, after `_rules.SweepNeeds`. |
| Declaring columns does not register their tables for persistence. | `World._tables`, `_writableTables`, `RebuildDerived`, `SaveFile` | Wire both registration and rebuilding explicitly and exercise the derived audit. |
| The current save header uses a manual declaration-set version. | `SaveHeader.Current`, `SaveFile.ReadBody` | Bump the save version once for this row's final schema; refuse old saves before reading their body. |
| The player can lay Streets or place a Service, but neither places a gate. Demolish refuses Buildings not marked abandoned. | `RefuseConnect`, `RefuseService`, `RefuseDemolish`, `Main.ToolDefinitions` | Build the gate placement/removal command in D14. Use a Street edit, not an illegal demolition of an occupied dwelling, for the housing intervention. |

## Decisions and implementation contracts

### D1 — Outside population is counted by exact composition

Use aggregate Household counts keyed by **edge, Life Stage, counts of adults at each of Skill Tiers
1–3, child count, and carried-Money band**. A composition is a storage key, not a new domain actor.
Use that ordinary word in the interface; add no named population tier or prohibited domain term.
A group with `H` Households and composition `(a1,a2,a3,c)` contains
`H × (a1+a2+a3+c)` people. `UNIQUE INDIVIDUALS`, `LEGIBLE CAUSE`.

The three carried-Money bands are the low, middle and high thirds of the Hinterland's inclusive
`emigrant_balance_min..max` range. For width `W=max-min+1`, amount `x` maps to
`floor(3 × clamp(x-min,0,W-1) / W)`. Band `b` has offsets
`ceil(b×W/3)..ceil((b+1)×W/3)-1`; reject an authored population in an empty band. Compute widened
products before division. A width of one has only band zero. This partition is a provisional
representation choice, not three independently priced Outside economies.

Opening entries declare exact compositions and counts. Returns join the exact matching composition,
creating an aggregate row if none exists. Do not round a departing family into a preferred template,
split it to fit, discard a child, or replace a Tier 3 adult with Tier 1. There is no fixed maximum
family size introduced by this row; store member counts as non-negative `int` and check their sum.
Normal authored openings use one or two adults. Directly constructed unusual Households and survivors of illness deaths still have
a total return mapping, including a nonempty child-only Household. They need not be eligible to
immigrate again until a meaningful adult-led Household can be offered: such stock remains visible
and subject to Outside turnover. A zero-member Household transfers zero people and contributes no
stock row; record its removal as a Household event only.

Matching uses a flat derived hash index with lookup and insertion only, rebuilt by walking rows in
slot order. Never enumerate a `Dictionary` or `HashSet`. Prefer the repository's existing flat
index style; a new index must delete/rebuild entries on row retirement and cannot retain every key
that ever appeared. Retire empty groups in their existing per-edge order and rebuild the lookup
once after each batch, with no lookup or insertion while the index is stale. Non-authored rows with zero stock and no queue reservations are freed,
including their fractions. An explicit saved `Authored` flag distinguishes an authored zero target
from a return-only group; authored target rows persist when empty. Anonymous returned composition
is retained; departed Household/Citizen ids, names, experience, attendance and Trip history are not.
A new arrival gets new city identities. The Outside is not another simulation of their lives.

### D2 — Return adds people; recovery restores the mix

Each authored composition has a resting Household count equal to its opening count. A composition
created only by returns has target zero. Recovery acts on the difference between **total stock,
including reserved queued Households**, and the target. Below target it adds Households of that
composition; above target it removes unreserved Households of that composition. Report the latter
as **Outside population turnover**, not as a city death, Departure or declined offer.
`HONEST DEGRADATION`.

This is an explicit extension of `adr/0023`'s one-sided recovery sentence. It is the sink for returns
above target and for compositions the city introduced. No cap discards a returned Household. No
code periodically overwrites stock with its target. No claim is made that city plus Outside is a
closed demographic system: replenishment and turnover are named external flows in its account.

Let `R = recovery_days × Ticks.PerDay`. Each Tick, for each live group:

1. Read `d = target - total_stock` before fresh presentations or admission.
2. If `d=0`, clear the recovery fraction and direction. If its sign changed, clear the old fraction
   before accruing in the new direction; fractional births must not become fractional removals.
3. Accumulate `abs(d)` into a non-negative numerator, take `floor(numerator/R)` whole Households,
   and retain `numerator mod R`.
4. Add at most the current deficit, or remove at most the current excess **and unreserved stock**.
   If reservations prevent removal, retain only the proper fractional remainder, not a debt to
   destroy Households later. Recompute the excess next Tick.
5. Transfer the exact composition's people to the appropriate population-flow counters.

Zero `recovery_days` disables both directions and requires zero fraction. This is useful for the
isolated depletion assertion, but the shipped demonstration enables recovery. In a world with a
steady bounded return flow, excess stock tends towards a bounded level rather than growing with
elapsed time. Zero-target groups reach zero through carried fractions; the last Household is not
immortal due to truncation. Queue residence is bounded by D5, so reservations cannot defeat this
sink indefinitely. This is a bound relative to city flows, not a hard cap on a growing city's
population. Test it in a steady-flow fixture; do not promise steady state for a world whose births
or externally injected population grow without bound.

### D3 — Reconsideration counts occasions, never successful immigration

Each group owns a numerator for reconsideration. Each Tick after recovery, add its **unreserved
Household count**; divide by `reconsider_days × Ticks.PerDay` to obtain occasions; carry the
remainder. Process those occasions against the current unreserved count, consuming an occasion
whether it declines or succeeds. A member may reconsider on multiple occasions while remaining
outside; no persistent individual exists until a willing prospect enters the queue.
`EMERGENCE`, `BOUNDED KNOWLEDGE`.

Require `reconsider_days >= 1`. When unreserved stock becomes zero, clear its numerator: time
spent empty is not credit earned by later returnees. A group containing no adults produces no
occasions and is shown as ineligible composition. There is no authored arrivals-per-Day figure
on this engine and no rate derived from a city-wide attractiveness score.

Before queue processing, capture each group's free stock after recovery into a per-pass scratch
allowance. Cancelled/expired reservations become free immediately for the stock account but are
excluded from this Tick's fresh allowance and numerator accrual. Each new reservation/admission
reduces its allowance. This prevents expiry followed by a fresh ask on behalf of the same released
population in the same pass; normal reconsideration includes it next Tick. No saved extra delay
queue is needed. A decline does not reduce the allowance because nobody left the available stock.

Walk edges in `MapEdge` numeric order and groups through a rotating intrusive list per edge. Advance
the saved start cursor after each Tick so fixed declaration order does not give one group every
last vacancy. Capture the iteration's successor before processing a group that may empty. Admit
from the existing queue before considering fresh stock. No return added by placement later this
Tick is considered until the next Tick.

Process all accrued occasions: do not introduce an undocumented CPU quota that silently becomes
a second immigration ceiling. Cost is `O(live compositions + accrued occasions × candidate budget
+ due queue work)`, and an occasion may inspect at most `2 × placement.candidates` Lots. No backlog
of missed occasions is retained. This is a finite population-dependent bound, not a measured Tick
budget; phase 6's cost is an acceptance measurement if the demonstration or sizing exposes one.

### D4 — The prospect is the person the gate actually admits

Introduce an unmanaged `ArrivalProspect` value carrying source group, stage, composition, purse,
choice identity and source edge. These are proposed new symbols. A monotonically increasing saved
sequence per edge supplies the identity; combine it with the edge using `Randomness.Mix` and new
purpose tags. Do not use a recycled group or queue slot, a gate id, or a per-command ordinal alone.
Assert checked sequence increment. It must distinguish two commands and autonomous occasions on
one Tick. `UNIQUE INDIVIDUALS`.

Draw the purse once, uniformly within the composition's current Money band. Draw centrality through
`Ruleset.CentralityTaste` with the prospect's identity and stage. Store that identity on the admitted
Household as `ChoiceIdentity`; resident callers use it when nonzero and otherwise retain the city's
Household id. Use a separate saved presence flag if zero is a possible generated identity; do not
silently reinterpret a valid zero. Life Stage transitions and retuning recompute preferences from
this stable identity and current stage, as they do for residents today.

Add `rent_weight_percent` to `[[life_stage]]`, defaulting to 100 when absent in existing worlds.
Require an explicit value in each stage of a stock-enabled world; accept 0..200 as **PROVISIONAL**.
Zero means that affordability still filters but rent does not influence preference. Define a shared
`HousingUtility` kernel, used by `TryHouse`, `Reassess`, prospect choice and Outside choice:

```
centrality_weight = 2 × CentralityTaste(key, choice_identity, stage) - Fixed.One
centrality_term   = -floor(centrality_tiles × centrality_weight / centrality_tiles_per_unit)
rent_base         = floor(rent × Fixed.One / rent_per_unit)
rent_term         = -floor(rent_base × rent_weight_percent / 100)
utility           = saturate_to_int(centrality_term + rent_term)
```

Use widened intermediates, `IntegerMath` division/shift helpers and a single rounding order.
Stage zero retains rent weight 100 and neutral centrality for legacy worlds. The rent weight is
relative to centrality, not an affordability discount. Do not add wages to the Hinterland schema:
there is still no corresponding city-side wage term.

For a stock prospect, sample up to `placement.candidates` distinct feasible Buildings within at
most twice that many draws; de-duplicate Buildings because two sampled Lots must not double a
home's probability. Reuse `Consider`'s existing feasibility checks and name their actual outcome;
no new Household-size capacity rule is introduced. Put the Outside first and the sampled homes
in accepted draw order; apply `Choice.Draw` once. No feasible sample means no city alternative,
not proof that every dwelling in the whole city is unsuitable. No gate on the source edge means
no connected city alternative and no Lot sampling. D15 adds vacant buildable housing Lots to the
sample, so a feasible sample means a Building with room or a Lot a housing Zone Rule would build on.

The Outside carries the existing moving-friction bonus when an unadmitted prospect compares leaving
its current home. A city Household's incumbent carries that same bonus; a Household already in the
Unplaced Pool carries none. This explicitly changes the old prospect path's frictionless comparison
in stock-enabled worlds. The chosen dwelling is evidence for willingness, not a reservation or a
promise of a particular address, and a chosen Lot promises no construction. Queue members retain purse and identity; they do not retain a
right to that dwelling. Differences in rent weights and centrality now make Life Stage composition
selectively drain, without claiming a predetermined ordering in every city.

### D5 — A willing prospect waits outside, separately from the Unplaced Pool

Use an intrusive FIFO per edge of saved queue rows, plus an intrusive review list ordered by
`LastScheduledReviewTick`. Both are doubly linked so cancelling a scheduled review can unlink a
member from the middle of the admission FIFO in constant time. Enqueuing **reserves one Household inside its
composition's stock**; it does not subtract population from the Outside and creates no Citizen or
Money Bin. The row stores the prospect's immutable composition/purse/identity, group handle and
`SinceTick`, `LastScheduledReviewTick` and `LastComparedTick`. Admission reduces both stock and reservations; cancellation reduces reservations
only. Maintain `0 <= reserved <= total` at each mutation. `LEGIBLE CAUSE`.

On each Tick, expire FIFO heads whose elapsed wait is at least `queue_wait_days × Ticks.PerDay`,
before considering admission. A shortened duration on reload therefore takes effect immediately;
the FIFO remains ordered because the same current duration applies to all rows. Expired prospects
return to unreserved stock and must earn another ordinary reconsideration occasion. Do not retry
one immediately as part of removing its queue row. The queue has no demographic clock, ages no
children and creates no births. Maximum residence is the authored duration, not a count of retries.

After expiry, process every review-list head whose elapsed time since its last scheduled review
is at least `queue_reconsider_days × Ticks.PerDay`, even when all gates are full. Re-run the choice
with retained purse/identity and current conditions. A decline or a sample with no feasible city
alternative cancels its reservation and counts as a changed mind. Neither spends stock or gate quota;
the released stock earns fresh reconsideration credit from the next Tick. Willingness
keeps its place in the admission FIFO and appends it to the review-list tail with the scheduled
review Tick updated. A scheduled review never resets `SinceTick`, so it cannot make the waiting
bound infinite. A fresh enqueue starts both clocks on its enqueue Tick. An admission or any
cancellation unlinks the row from both lists. Because every row uses the same current interval,
changing that interval on reload preserves review-list ordering.

When an edge has capacity, process its admission FIFO in order. Re-evaluate each old queue member against
current housing and Outside conditions, using its retained purse and identity, a new Tick draw and
current preferences. A decline or an empty feasible sample cancels its reservation; only a willing
result tries admission. Stop when
no gate on that edge has quota left. A prospect generated on this Tick already made its comparison:
try it once without a second draw, enqueueing it if the edge's quota is exhausted. Honour
`LastComparedTick` for prospects queued by phase-0 commands too: a gate placed by a later command
may admit them in phase 6, but must not buy them a second choice draw on that Tick. New prospects
join behind all surviving older queue members. Failed admission due to an invalidated gate keeps
the reservation and tries another eligible door; it never debits stock.

If every gate on an edge disappears, cancel its queue immediately on the next engine pass and
report **connection lost**, restoring availability. Do not leave it waiting for a gate handle that
can never resolve. Restoring a gate resumes normal occasions; it creates no population. Apart from
expiry, a queue member is reconsidered when admission capacity exists or its scheduled review is due, at most once on a Tick. Check
`LastComparedTick` in both paths and reuse a willing result already obtained on that Tick. This
avoids re-drawing willingness on every full-gate Tick while still giving timed reconsideration
and a finite waiting bound.

Queue storage is bounded by reserved stock and by the arrivals of willing prospects during the
finite wait window. Rows and intrusive links are reusable; no rejected-prospect history is kept.
Do not impose a second arbitrary capacity or drop the newest queue row to save memory.

### D6 — Gates share a stock and a queue, and only admission spends quota

Add a derived intrusive list of live Outside Connections per edge on `BuildingTable`, with heads
on the edge table. Maintain it on creation/destruction and rebuild after load or migration. A live
Building counts only when its kind is currently an Outside Connection and its Lot resolves to that
edge. A gate at a corner retains the existing refusal; do not assign an arbitrary edge.

Allocate admission opportunities by round-robin over gates with remaining quota, starting after
the edge's saved last-admitted gate id. Compare monotonic ids, not recycled slots; if the old gate
is gone, take its successor or wrap. The list is sorted by id and no hash-map iteration establishes
its order. Each successful admission advances the cursor. Each admission attempt may walk at most
the live gate count; full doors are skipped. A derived daily remaining-capacity summary may avoid
repeated full scans, but must be invalidated on quota use, Day change, gate changes and reload.

A second gate adds its declared quota, never a second round of prospective people. Gate quotas
count Households, stock/readouts also count people. Lowering a gate's quota below today's use gives
zero remaining capacity; it does not undo admissions or reset the meter. A newly placed gate has
its normal current-Day quota, retaining the existing building-lifetime semantics rather than
inventing an edge-wide quota. Gate distance and congestion still affect move-in Trips, not the
Outside comparison in this row.

### D7 — Admission is an atomic, common World operation

Provide `World.TryAdmitProspect` for the new engine and stock-aware explicit commands. Before any
write, validate the live group, enough unreserved stock or the caller's reservation, source edge,
valid composition, gate identity/edge, current quota, and numeric capacities. Treat an old Day's quota as logically zero-used without writing a reset
until admission succeeds. Ordinary rejection
returns an enum and makes no population, quota, Money or queue mutation. Allocation failure is not
an ordinary rejection to catch and continue after partial writes.

On success, one World operation:

1. Allocates a Household, its empty Money balance where Money exists, and the exact adult/child
   members. Initialise adults in tier order, then children, so creation order is deterministic.
2. Sets adult age using the existing age draw and Skill Tier from composition. Imported children
   have age zero and initial Tier 1. Use an internal member-creation primitive, not `Bear`, because
   these children arrived rather than being born here.
3. Starts experience and observed attendance at zero. This row imports adult credentials, not an
   invented childhood transcript. In a schooling world an imported teenager therefore has fewer
   locally observed school Days; show arrival origin and do not report the unobserved Days as
   school failures. University eligibility/state follows the existing formation rules only where
   they explicitly apply; do not auto-enrol arriving Tier 2 adults.
4. Sets Life Stage and arms its full currently authored duration, using the existing duration draw.
   This is a new stage spell at entry; no unmodelled Outside age is backdated.
5. Saves `ArrivalEdge` and choice identity on the Household, endows precisely the evaluated purse
   through `World.Endow`, and joins the Unplaced Pool with the chosen live gate.
6. Decrements stock/reservations, spends exactly one gate quota, removes the queue row if present,
   and records one Household and its exact member count in admissions and the city population
   account. Nothing after successful validation may return an ordinary refusal halfway through.

Extract common allocation plumbing from `TryArrive`; retain a legacy wrapper for stock-disabled
worlds. The stock-enabled path may not bypass its stock debit via a direct `World.TryArrive` call:
route it through the new operation or refuse that overload's insufficient payload explicitly.
An admitted Household can remain unhoused, lose its gate, be evicted after placement, or later give
up. None of those refunds an arrival; only actual Departure returns population to Outside stock.

### D8 — Explicit commands exercise the same stock, without becoming its generator

`ArrivePayload` currently has 8 bits of Household count and 4 each of stage and total Citizens.
Do not reinterpret its wire representation or add a second automatic command stream. In a
stock-enabled world, its stage and member total select eligible unreserved groups on that edge.
For each requested Household select among matching groups weighted by available Household count,
using a dedicated purpose tag and the saved prospect sequence, then evaluate and enqueue/admit
through the same pipeline. The payload does not manufacture a child count or Skill Tier.

A positive request with no matching *declared or live* composition gets a new command refusal
explaining the mismatch; a matching composition that is exhausted returns zero presentations and
an empty-stock activity result. A zero count remains a no-op after the existing gate validation.
Requests cannot draw reserved stock, exceed stock, force willingness or bypass quota. Repeated
requests on one Tick have distinct identities. They are scenario-triggered extra reconsideration
occasions and are reported separately from autonomous occasions. Input commands run before phase 6
and can consume quota first; this is the recorded order, not thread scheduling.

Stock-disabled worlds preserve existing command semantics and the old prospect path for their
fixtures. The new demonstration and its Input Logs contain **zero `Arrive` commands**. `ArrivalDump`
selects autonomous behaviour from the Ruleset's stock declaration; legacy runs retain explicit
asking and label it. Do not put generated arrivals into a replay log: replay regenerates them.

### D9 — Departure chooses an Outside destination even if the old gate is gone

For the existing unhoused Household Departure, compare all stock-enabled Hinterlands through the
same utility kernel using its current stage and choice identity. There is no incumbent bonus: it
already has no home here. Use `Choice.Draw` with a dedicated Departure tag; candidate order is
`MapEdge` numeric order. Destination does not have to equal source, and throughput does not ration
Departures. Save the original `ArrivalEdge` for provenance and Pool comparison, not as a mandatory
return address. `LEGIBLE CAUSE`.

The existing give-up sink must work with no live gate. Destination is therefore an **accounting
edge**, selected from declared stock Hinterlands even then; the interface says where population
went, not that a physical exit Trip was observed. Physical exit routing remains unbuilt, exactly
as the pre-row Departure operation is abstract. Require stock on all four Hinterlands in a
stock-enabled Ruleset so there is always a destination. A hot reload cannot remove those edges.

Immediately before destroying the members, count adults by their actual Skill Tier and children
by age zero, read the current Life Stage and Money, and credit that exact composition at the chosen
edge. Classify the Money into the destination's current band; out-of-band balances map to its
nearest band **without altering the actual Money leaving the city**. No Outside wealth is conserved
or simulated. Invalid tiers or an undeclared stage are invariant violations in this mode, not a
reason to omit somebody from the count. Structural Life Stage reload restrictions prevent the
normal undeclared-stage case.

All callers of the existing Household Departure use this door. `Dissolve`, `DestroyCitizen`,
`DestroyHousehold` in a fixture and Business bankruptcy are not automatic return paths. Dissolution
keeps its estate-to-treasury behaviour; emigration keeps its Money-supply decrement. Refactor raw
cleanup behind reason-bearing population operations rather than teaching row destruction to guess
whether somebody emigrated.

`PlacementEngine.TryOutside` reads saved `ArrivalEdge` for an immigrant in the Pool when the old gate
handle is stale. A locally formed Household keeps its current Pool comparison behaviour; selecting
its eventual Departure destination does not add the new housed-departure or failure mechanisms
owned by row 30. Inspection distinguishes source from destination.

### D10 — The population account is independent of table counts

Add a saved `PopulationLedgerTable` with a founding baseline, a sealed-baseline flag, and counters
for city births, admissions, Departures, illness deaths, dissolution removals, scenario additions
and scenario removals. Record people and Household events separately. Initial synthesis counts as scenario
creation until `Simulation.Step` first seals the founding baseline **before applying inputs**;
sealing folds those setup entries into the opening count and happens once. Loading a save cannot
seal it again. Inputs at Tick zero are then ordinary recorded scenario events.

For `P` live city Citizens, assert:

```
P = opening_city_people + births + admissions + scenario_additions
    - departures - illness_deaths - dissolution_people - scenario_removals
Sg = opening_group_households + replenished + returned - admitted - turnover
0 <= reserved_group_households <= Sg
Qedge = queued_total - queue_admissions - queue_cancellations
P + sum(Sg × group_size)
  = opening_city_people + opening_outside_people + births + scenario_additions
    + replenished_people - illness_deaths - dissolution_people - scenario_removals - turnover_people
```

The last equation cancels migration, including queue reservations. Queued people are already in
`Sg`; never add them a second time. Outside counts and ledger anchors are separate declarations,
not getters summing one another. Edge lifetime flows retain a retired group's history; the global
account uses those edge totals, not sums of counters on only the groups that are still live. Group
recreation starts its own zero-opening account with the new return, without resetting edge history. At a completed Tick no population transfer is half applied.

Writer responsibilities:

| Population operation | Ledger change | Must not also count |
|---|---|---|
| Setup `CreateCitizen`, later explicit creation | Opening setup or scenario +1 | Birth or admission |
| `World.Bear` from Life Stage fertility | Birth +1 | Opening or scenario addition |
| New common stock admission | Admission + exact prospect member count | Each internal member as a birth/scenario addition |
| `SpawnChildren` / `FormHousehold` | Household formation event only | Any people added or removed |
| `World.Depart(Household)` | Departure + actual members, with matching Outside credit | Dissolution or scenario removal |
| `World.Dissolve` | Dissolution people + actual members | Outside return |
| `CivicEngine` illness-death branch via new `World.DieCitizen` | Illness death +1 | Scenario removal or Outside return |
| Explicit `DestroyCitizen` / fixture cleanup | Scenario removal + actual members | A guessed Departure |
| Replenishment / Outside turnover | Group and external-flow counters only | City births/deaths |

Preserve the care engine's trace, roster and civic-row cleanup around its death call; only its
population reason changes. The remaining Household stays live under the current care behaviour,
even when its last adult dies. A child-only or empty eventual Departure follows D1/D9, not an
invented automatic family dissolution.

Use internal unaccounted member/Household cleanup called by the reason-bearing operations; default
public fixture helpers remain explicit scenario operations. Protect those internal operations from
unclassified production callers. Audit usages of `Citizens.Rows.Allocate/Free` and
`Households.Rows.Allocate/Free` while implementing. Tests must create deliberate missing/doubled
ledger writes and see the invariant fire; an invariant that reconstructs its anchor from live
rows would pass both defects.

Use checked `long` for counts/products and `ulong` for Tick/identity where appropriate. Validate
opening products and summed people before constructing state. Check running additions, sequence
increments and products before transfer; an overflow is a clear invariant failure, never wrapped
negative stock or a saturated conservation anchor. Lifetime counters are historical totals, not a
promise that cumulative admissions are steady-state stock. Keep current-Day and previous-Day
flows alongside them for bounded inspection, not an append-only history.

### D11 — Exact state ownership and persistence

Proposed names are implementation destinations, not claims these files already exist.

| Owner | Saved and hashed fields | Derived/rebuilt or scratch |
|---|---|---|
| `HinterlandTable`, four rows | Edge; prospect sequence; composition-start cursor as a saved handle; last-admitted gate id; current flow Day; lifetime/current/previous-Day flows | Gate list head; available quota summary and its validity inputs |
| `HinterlandPopulationTable`, aggregate rows | Edge; stage; Authored flag; adult counts at Tiers 1/2/3; children; Money band; target; total stock; reserved; reconsider numerator; recovery numerator/direction; composition intrusive links; group flow counters | Composition lookup index |
| `HinterlandQueueTable`, one row per willing prospect | Group handle; purse; choice identity; SinceTick; LastScheduledReviewTick; LastComparedTick; admission and review previous/next links | Immutable composition and edge read through the saved group; no duplicate copies that can disagree |
| `HinterlandTable` queue ownership | Admission and review head/tail saved handles | Reservation audit sums from live queue rows |
| `HouseholdTable` additions | ArrivalEdge; ChoiceIdentity and presence flag | Current utility/taste computed from Ruleset and identity |
| `PopulationLedgerTable`, one row | Sealed flag; opening people; classified flow counters and daily snapshots | Independent live-city/Outside walks for validation |
| `BuildingTable` additions | None required beyond its existing gate meters | Intrusive next-gate link |
| `HinterlandEngine` | No durable simulation state in ordinary instance fields | Candidate buffers and per-pass results only |

Implement columns with `Rows.Saved`, `Rows.SavedHandle` and `Rows.Derived`, using declared quantity
types or integer counts, never floating point. Add table marker types and append tables to
`World._tables` in the order above; include them in `_writableTables` so the Decide guard covers
them. Queue/group handles fold target row ids through the existing declaration machinery. Rebuild
gate lists and the group lookup in `World.RebuildDerived` from authoritative rows without altering
saved state. Exercise the populated structures in `DerivedRebuildAuditTests`.

Initialise all four edge rows and authored stock in `World` construction from validated Ruleset
content, not in the shell or on the first headless observation. Create target rows in canonical
edge then composition-key order. Save restore overwrites this constructor state; it must never
perform an additional refill. Reconstructed tables/indexes must match the uninterrupted run even
when a returned group and a gate were freed and their slots reused.

Increment `SaveHeader.Current` for the schema change, not `World.HashSeed`. Extend format/refusal
fixtures and `SaveHash` coverage if its generic walk requires it. Old binary saves are refused;
this row provides no partial migration. Input Log field layout stays unchanged; D14 adds the explicit `gate` verb to the codec vocabulary. Re-record affected
State Hash fixtures through `tests/Borough.Tests/Golden/README.md`; do not paste replacement hashes.
New saved tables can move even stock-disabled world's hashes, so do not promise golden stability
merely because their gameplay path is unchanged.

**Task 2 declared the three tables and a subset of the columns above, and the subset is deliberate.**
A column declared with `Rows.Saved` is allocated, folded into the State Hash and written to every
save whether or not anything assigns it, so a column ahead of its writer is state the city carries and
nobody maintains. What exists after task 2: on `HinterlandTable`, the edge and the lifetime flow pairs
(replenished, returned, admitted, turnover), plus derived gate-list head and tail. On
`HinterlandPopulationTable`, the whole composition key, the Authored flag, target, opening, stock,
reserved, the four group flow counters and the derived per-edge link. On `PopulationLedgerTable`, the
Sealed flag, the four opening figures and the thirteen classified flow counters. What is absent and
which task brings it: the prospect sequence and the composition-start cursor (task 4, which is what
advances them); the last-admitted gate id and the current-Day flow columns (task 4's rollover); the
reconsider and recovery numerators (task 4's engine); the current- and previous-Day snapshots on the
ledger (task 7, which is what reads them); `HouseholdTable.ArrivalEdge` and the choice identity
(task 3). ⚠ **Each of those is a further `SaveHeader.Current` bump**, which costs nothing while nobody
carries a save.

**The city side of the account is closed and the Outside side is not, and task 5 owns the gap.**
`World.Depart` counts its Household and its people as departures, and credits no Hinterland with
receiving them — D9's destination choice needs `HouseholdTable.ArrivalEdge` and the choice model,
neither of which exists yet. `World.ReturnToHinterland` and `World.CompositionOf` are built and tested
and have no caller in `src/`. So `CityPopulationIsAccounted` and `CityHouseholdsAreAccounted` are
whole-world equations over live rows, and there is no two-sided conservation law over city and Outside
together until task 5 wires the credit. ***Half an account is not a conservation law.***

**Task 3 added the three `HouseholdTable` columns, so `SaveHeader.Current` is 3 and all three golden
artefacts were re-recorded.** `Arrived` is the presence flag D4 asks for and both columns beside it
need one: zero is `MapEdge.North` and zero is a choice identity a draw can produce, so neither value
can double as an absence. `HouseholdTable.TasteIdentity` is what both resident choice paths now draw
on — the arrival's identity where there is one and the row's own monotonic id otherwise — which is
identical to what they drew before in every world that has no stock.

**The shared kernel is `Rules/HousingUtility.cs`, and a rent weight of 100 is bit-identical to the
arithmetic it replaced.** `TryHouse`, `Reassess`, the Outside row and the prospect all go through
`Worth`; `Taste` is the `2T − One` expression the first two duplicated. So the State Hash moved for
the three columns and not for the kernel, and `attracted.toml` is the only file whose stages weigh
rent at anything but neutral. ⚠ **The weight scales the rent *term* and never the money** — the
affordability filter in `Consider` does not read it, and a stage at zero still cannot move into what
it cannot afford.

**`World.TryAdmitProspect` and the new `ProspectCrosses` overload have no caller in `src/`.** Task 4's
engine and task 5's commands are what reach them; the legacy `TryArrive` and the anonymous
`ProspectCrosses(int gate, …)` are untouched, which is what D8 requires of a stock-disabled world.
⚠ **Two pieces of D7 are deliberately absent.** Admission requires *unreserved* stock
(`Stock − Reserved ≥ 1`) and has no path for a caller presenting its own reservation, because nothing
reserves yet; task 4 extends it alongside the queue, and admission must then decrement `Reserved` as
well as `Stock`. The prospect *sequence* that makes an identity unique is task 4's too — task 3's
`ArrivalProspect.Of` takes an identity rather than minting one.

**The purse draw is `HinterlandDefinition.BandBalance` on a new `PurposeTag.ProspectPurse`; the
candidate sample and the choice draw reuse `PlacementCandidate` and `ChoiceDraw`.** Reusing those two
is `ChoiceDraw`'s own recorded argument — *one tag serves every consumer of the choice model, because
the entity id already separates them* — and the purse needed its own for `EmigrantBalance`'s reason
one level down: the same id takes the same fraction of whichever span it is given, so a shared tag
would make the richest family in the low band the richest family in the high band.

**Task 4 added `HinterlandQueueTable`, `Rules/HinterlandEngine.cs` and the state the engine advances,
so `SaveHeader.Current` is 4 and all four golden artefacts were re-recorded.** On `HinterlandTable`:
the prospect sequence, the last-admitted gate id, the composition cursor, the two queue-list ends, the
derived gate-list ends and the thirteen current/previous Day flow pairs, rolled by `RollDay` before the
inputs. On `HinterlandPopulationTable`: the reconsider and recovery numerators and the recovery
direction. The queue's admission and review lists are **saved** because join order is recoverable from
nothing else — a rebuild would put the queue in slot order and serve a different family after a reload —
while the gate list is derived from Kind and Lot position. `LinkedIndexList` is the doubly-linked
sibling `IndexList.Remove`'s remark anticipated; a queue row leaves the middle of both lists at once.

**A waiting family is reconsidered only when a door on its edge has room, and that ordering is the
mechanism rather than an optimisation.** Re-asking at a shut door re-draws willingness on every Tick of
the wait, which cancelled almost every queue member within a dozen Ticks and made the authored wait
unreachable — caught by `A_wait_ends_at_exactly_the_authored_duration`. `World.GateHasRoom` exists for
that question. `TryAdmitProspect` gained the `reserved` path D7 left for this task, so a queued family
spends its own reservation and stock together.

**`Invariant.TheQueueMatchesItsReservations` compares the count with the rows**, which nothing else
does: a reservation left behind by a cancelled row takes a Household out of circulation for good, and a
row whose count was never raised lets a fresh occasion draw the family already standing at the door.

⚠ **Three of the acceptance cases named for this task are not written yet.** The reduced-quota case
needs the reload plumbing D12 gives task 5; `StockArrivalCommandTests` is task 5's; and the
selective-depletion ensemble belongs with task 8's observation, where a rate comparison can be read
against the diagnostic counters rather than asserted from one run. `AutonomousArrivalTests` covers the
attribution those tests will rest on — every occasion sums to exactly one of no-connection, no-sample,
stayed-outside and willing.

**Task 5 added one saved pair, so `SaveHeader.Current` is 5 and three artefacts were re-recorded.**
`HinterlandTable` gained `requested_today` and `requested_yesterday`, rolled by `RollDay` beside the
other Day pairs: a commanded `Arrive` is an occasion the player caused, and it increments both that
pair and `occasions_today`, so the attribution identity task 4 rests on still holds. No shipped
Ruleset was edited, so no content hash moved and no `.borough` literal with it.

**The two-sided law this section said did not exist is now `Invariant.TheCityAndItsOutsideBalance`.**
The city plus everybody standing behind its edges equals the two opening figures plus births,
scenario additions, replenishment and returns, less illness deaths, dissolutions, scenario removals,
turnover and departures. ⚠ **An arrival has no term in it, and that absence is the check.** A gate
that produced a Household without spending stock raises the left side and leaves the right side
alone, which is exactly what a stock-aware `ApplyArrive` must not do. Departures and returns each
carry a term because they need not be equal — a family with no edge to go to leaves the world.

**`SealFoundingPopulation` retakes the Outside figures and clears the edge crossing counters.** The
opening stock is filled in at construction from Ruleset content, so a fixture that departed or
returned somebody before the first Tick had them counted twice, once inside the opening figure and
once as the crossing that moved them. It is a no-op in every world nothing touches before its first
Tick, which is every world outside a test. ***An opening figure taken at a different moment from the
one it is compared against is not an opening figure.***

**`World.Adopt` refuses the Outside edits that would invent or destroy people, and rescales the two
fractions the rest carry.** Refused: stating or withdrawing `[immigration]`, changing the money
Resource, adding or removing a `[[hinterland]]`, and any inequality between two
`HinterlandPopulationDefinition`s — which covers composition, resting count, purse band and declaration
order in one comparison. Retuned: rent, centrality, the emigrant purse range, and the reconsider and
recovery periods, whose accrued numerators are rescaled by `IntegerMath.MulDivFloor` into the new
denominator. ⚠ **The refusal is taken before `RulesetShape.Compare`**, so a composition edit sitting
beside a rent edit is refused rather than let through by whichever difference was noticed first, and
`HinterlandReloadTests` asserts the untouched world by State Hash rather than by inspection. Four
further allowed edits are covered there: a gate quota cut below what a door has already spent shuts
it for the rest of the Day; a purse-range retune that empties a band keeps the stock filed under it,
which is the one place authoring is stricter than transition; a longer queue wait leaves the Tick a
waiting family joined at alone; and recovery switched off and back on resumes from zero and still
empties the group.

**Task 6 changed no production code, and the reason it did not is the finding.** `SaveFile` and
`SaveHash` walk `World.Tables` and each table's saved columns, so row 31's four tables entered the
file the moment they were appended to the world; all three derived rebuilds already clear before they
fill. What was missing was evidence. `FactorioTests` compares a saved world against one that never
stopped on `minimal` and `congested`, and neither states `[immigration]` — so no world with anybody
standing behind its edges had ever been resumed, and the machinery being generic is a reason to expect
it works rather than a demonstration that it does.

**`HinterlandPersistenceTests` saves over a deliberately awkward Outside and asserts that it is
awkward before writing the file.** An idle Outside round-trips by holding still: empty queues, whole
fractions and unspent quotas all survive a load that does nothing. So the fixture checks, before every
save, that a queue is non-empty, that both fractions are part-way accrued, that stock is reserved,
that a door is spent and that a group the Ruleset never declared is standing. Nothing is staged by
writing a meter — `recovery_days` is cut to 1 so a returned group finishes draining and frees a row,
and the authored quota of ninety-six families a Day across four gates supplies the pressure on its
own. ⚠ **A freed row is handed straight back to the next return**, so the drained group is identified
by composition through `HinterlandCompositions.TryFind` and never by slot; asserting on the slot reads
the reuse as a group that never left.

⚠ **The queue exists only while the gate quota is the binding constraint, and that window closes.**
Once the city is full enough that housing is the limit, prospects stay outside and nobody queues, so a
later save is a save over an idle Outside. The two save points sit at Tick 4,000 and at Tick 4,090 —
the second six Ticks before a Day rolls, so the resumed world is the one that performs the rollover
and moves the flow counters. Join order is the one piece of Outside state no rebuild could recover: it
lives in the admission list and nowhere else, so a load reconstructing the queue from live rows would
come back in slot order and admit a different family. Worker-count equivalence is worth asking of a
stock world in particular, because an admitted family joins the Unplaced Pool and its move-in Trip is
routed on the parallel path.

**Replay equivalence had never been asked of a world anybody emigrates to.** `ReplayTests` runs under
`Ruleset.Empty`. The new case replays a log whose whole content is `Populate` at Tick 0 — a hand-built
`Arrive` log cannot name a gate Tile, because gate positions are decided by the subdivider — twice
under the stock Ruleset, into two Worlds sharing nothing but the log's seed. Admissions are asserted
nonzero, since two traces over a stock engine that never ran would agree perfectly. `SaveHeaderTests`
gained the refusal in the other direction: the format version has moved five times, four of them for
row 31, so a file one declaration set behind this build is an artefact that exists rather than a
hypothesis. No saved column was added, so the header stayed at 5 and nothing was re-recorded.

**Task 7's gate is charged, which closes the inconsistency D14 opened.** `ApplyGate` pays the kind's
`placement_cost` through `World.SpendOnPlacement` exactly as `ApplyService` does, and
`GateTreasuryCannotPay` is asked **last** for `RefuseService`'s reason: the six checks ahead of it
are shape and this one is a level, so it is the only one that can answer differently for the same
command two Ticks apart. ⚠ **No shipped Ruleset prices a door**, so every shipped world still raises
one for nothing and the refusal is reachable only from a Ruleset that states a cost —
`RefusalTests.PricedPort` is that world, and it declares four Hinterlands because which edge the
generator leaves a vacant Lot on is not something a fixture chooses. Removal refunds nothing, on
`ApplyDemolish`'s terms rather than a rule of this verb's own.

**The ledger's Day figures are snapshots rather than a second set of counters, so a writer cannot
forget one.** `population_ledger` gained `flow_day` and, for each of its thirteen classified flow
counters, what that counter stood at when the current Day opened and when the last complete Day
opened — twenty-seven saved columns, `SaveHeader.Current` `5 → 6`, three golden artefacts
re-recorded. A Day's figure is the **difference** between a counter and its snapshot. The
alternative pairs a lifetime counter with a daily one at each of the sixteen recording sites, and
two counters can disagree: an increment that lands on one and misses the other reports a Day that
does not add up to the history it sits inside. ⚠ **`RollDay` runs after `Seal`** — `Simulation.Step`
seals before it rolls, and the first roll is a Day after the seal — so a snapshot never stands ahead
of a counter the seal has zeroed and a Day's figure is never negative.

**`HinterlandReading` reads and never drains, and `A_reading_moves_no_state` is the assertion that
says so.** `Instruments/HinterlandReading.cs` carries the per-edge reading with its two Days of
flows, plus `HinterlandGateReading` and `HinterlandGroupReading`, which fill a caller's Span so a
panel refreshing every frame allocates nothing. `Instruments/PopulationReading.cs` carries the
account, the Pool and the residual. The Day each reading is denominated in comes from the table's own
`FlowDay` rather than from dividing the Tick out, so the flow counters and the gate meters cannot
disagree about which Day they are counting.

⚠ **The placements and give-ups the inspection contract lists are deliberately NOT in the
reading.** They live in `PlacementActivity`, which a **Census drains** — so a reading that carried
them would take them from whichever of the panel and the dump asked second, and the contract's own
first rule is that inspecting resets no meter. The two readers that want them already own a Census
each. ***A figure that cannot be read twice does not belong in a thing that is read every frame.***

**`--arrivals` became two pictures and the Ruleset picks which, so no flag enables a Ruleset's own
mechanism.** A file stating `[immigration]` gets a run that **issues no commands at all** — every
Tick stepped empty — and four new panels reading the instruments: who stands behind each edge and
who is waiting at a full door, every composition with whether the file authored it or an emigration
created it, each door's quota spent and remaining, the whole circuit today and over the last
complete Day, and the population account with its residual. A file without one keeps the driven run,
now labelled **explicit presentations** in the header and in `Options.Usage`. ⚠ **The Households in
`waiting` are not the Unplaced Pool**: they are still the Outside's stock, held as `reserved`, with
no Citizen row anywhere — the Pool holds people already let in and looking for a home, and the
panels say so where both appear.

⚠ **The empty-Pool claim was false and is now a figure rather than a sentence.** "The Pool is empty,
which in a world with a door in it means construction kept up with the gates" reads identically in a
city nobody was willing to enter and one whose doors had no connection to offer. The panel now
prints lifetime admissions and lets that discriminate, because ***a picture that cannot tell two
cities apart must not name one of them.***

**The shell reads the Outside through the same instruments and computes nothing of its own.**
`Main.Hinterlands.cs` holds the panel: four edge rows that stand whatever the city has done to them,
the city's account beneath, and — for a selected edge — its doors, its compositions and all three
groups of flow counters for today and the last complete Day. It opens from an **Outside** launcher
that appears only where the file states `[immigration]`, and from a section on any inspected Outside
Connection which states that door's own quota and links to the edge whose stock it shares.
`PopulationReading` gained an `Ever` window so the panel can put births beside admissions without
the shell adding up columns. ⚠ **Waiting outside and admitted-and-looking are separate headings with
separate counts**, because they are different states and a reader told only *unplaced* cannot tell a
full door from a city with no dwellings.

**It writes into standing labels and rebuilds only when the shape changes**, on `Main.Budget`'s
split-signature discipline — a panel refreshed on every collected batch that tore thirty labels down
and made thirty more would be one refreshed too rarely to watch. `scripts/ui/check-hinterlands.py`
drives it: four edges, each edge's admissions adding up out of its own doors, every fresh occasion
being exactly one of four outcomes, the account's residual at zero, the panel opening from a door as
well as from the console, and — the one that matters — ***the State Hash unmoved across a full
read.***

**The shell had no sentence for any of the eleven gate refusals, so a refused door fell through to
`refused for reason 34, which this shell has no sentence for`.** `Simulation.Explain` had the
diagnosis all along and the player never saw it. The eleven sentences in `Main.Verbs.cs` are the
shell's own and shorter than Core's: they name the plot, the kind or the money, say what to do next,
and cite no ADR.

**`GateCommandTests` asserts the accepting half, because `RefusalTests` enumerates `Refusal` and
goes red on a member with no case.** ⚠ **Every existing second-gate fixture raises its door with
`World.CreateBuilding`** — `AutonomousArrivalTests`, `HinterlandQueueTests` and
`DerivedRebuildAuditTests` all do, and none of them runs phase 0 — so nothing asserted that the
player's verb reaches the same mechanism. The seven cases are a door landing on the named plot, a
second door leaving its edge's stock and compositions untouched, a door admitting on the Tick it
appears, removal sending the queue home and a replacement resuming it, three refusals leaving the
State Hash where it was, the log round trip, and a two-run replay whose Tile is scouted from a
replay rather than chosen. ⚠ **The same-Tick admission is asserted on the new door's own meter**:
a Tick that rolled the Day would reopen the shut doors and raise the edge's total without this one
admitting anybody.

### D12 — Ruleset surface, validation and reload

Add `HinterlandPopulationRuleset.cs` for the new immutable definitions; thread them through
`Ruleset`, its constructor/build path and **every copying helper, including `WithLayers`**.
Extend `RulesetLoader` table collection/unknown-key handling, readers, `RulesetNames`, key notes,
schema generation and reference generation. Use the existing parser's array-of-table conventions;
the intended authored shape is:

```toml
[immigration]
reconsider_days = 2
recovery_days = 32
queue_wait_days = 2
queue_reconsider_days = 1

# Existing [[hinterland]] edge/economy/price fields remain on that table.
[[hinterland.population]]
stage = "young"
adults_by_tier = [1, 0, 0]
children = 0
money_band = 0
households = 600

[[hinterland.population]]
stage = "family"
adults_by_tier = [1, 1, 0]
children = 2
money_band = 1
households = 200

# On the corresponding existing [[life_stage]] table:
# rent_weight_percent = 150
```

The nested entries attach to their immediately preceding `[[hinterland]]`; the example is a
fragment, not a standalone loading fixture. Accept no orphan population table. Resolve stages by
existing stable name keys, not numeric declaration position. Canonicalise compositions and reject
duplicate keys rather than silently adding them. Author `households=0` to keep a recovery target
of zero for an intentionally empty group. At least one positive opening group is required across
the world, not per edge.

| Validation | Required outcome |
|---|---|
| `[immigration]` absent | Existing mode. Population entries or new stock-only settings are refused. |
| Present | All four durations explicit; choice model, placement with give-up, valid Life Stages and adult-age band, Money, and exactly four Hinterlands with explicit population arrays required. An empty edge array is permitted. |
| Composition | Exactly three non-negative adult counts; children non-negative; nonempty; opening child-only compositions refused; stage declared; legal nonempty Money band; count/product fits checked representation. |
| Stage composition | Child-bearing stock uses a stage with primary/secondary `SchoolLevel`; a stage outside those levels cannot carry children. Returned unusual compositions are counted and marked ineligible, not lost. |
| Durations | Reconsider/wait/queue review at least one Day; queue review strictly shorter than maximum wait; recovery zero or at least one Day; all `Days × Ticks.PerDay` conversions checked. No default zero for an omitted required key. |
| Preferences | Explicit rent weight 0..200 per stage in stock worlds; existing centrality validation retained; same arithmetic validation on direct Core Ruleset construction as on TOML inputs. |
| Stage reload in stock mode | Refuse additions, deletions, renames, reordering and changes to transition/fertility/school-level structure or adult-age bounds. Preference weights and stage durations may change. This preserves keys and meaningful return mapping. |
| Stock reload | Refuse enabling/disabling stock, edge changes, population composition/layout, targets/opening counts and Money-family identity changes. Retune economy, durations and preferences without reinitialisation. |

Run structural checks at the top of `World.Adopt`, before any table mutation, alongside its existing
world-creation refusals. Stock refusal must not depend on `RulesetShape.Compare` returning this
particular change first: check all restricted properties even when another change is also present.
`Changes?.Invalidate` alone may occur before refusal, but State Hash, Ruleset in force, quotas,
fractions, stock and queue must remain unchanged. The format layer supplies readable errors;
Core returns typed reasons or throws the existing reload exception form at its boundary.

Tuning changes take effect at the logged transition's Tick, before inputs. Preserve progress under
a changed denominator by `new_fraction=floor(old_fraction × new_R / old_R)` using overflow-safe
integer ratio arithmetic. Add a positive `IntegerMath.MulDivFloor` helper using widened `Int128`
intermediates inside the arithmetic namespace if no existing helper fits; never multiply these
`long` values unchecked. Test the helper against a `BigInteger` test oracle at boundary values. Do this separately for reconsider/recovery. Zero recovery clears its
fraction and direction; re-enabling starts from zero. Preserve queue SinceTick and LastScheduledReviewTick; evaluate elapsed
wait and review eligibility against the new durations. Expiry precedes review if both are due. Do not redraw queued purses when emigrant balance bands change;
new prospects use the new band. Existing group band indices remain meaningful even if a retune
makes a band empty: its stock is temporarily ineligible for fresh presentations, still counted,
and can recover/turn over; reopening the band makes it eligible again. Validate initial content
more strictly than this honest transition outcome.

The accepted refill semantics are **target immutable, recovery speed tunable**. A recorded Outside
rent edit changes comparisons, not population. Stock depth changes require a new world, not a Shock
implemented under another name.

### D13 — Close the moving-friction arithmetic hole

`PlacementRuleset.StayingPut` and `Mu` already define the rounding, and `Choice.Weight` compares
against `Transcendental.ExpUnderflowsBelow`. Add a shared validator next to the arithmetic rather
than repeating an approximate 11.09 constant in the loader. Compute the incumbent's fixed-point
bonus widened, require it to fit, and compute the negative scaled utility gap using the same
multiply/shift order as `Choice.Weight`. Refuse when `Exp` would return zero for an otherwise equal
alternative. Refusal names `moving_costs_rent`, `rent_per_unit` and `mu_percent`, and advises lowering
friction/μ or increasing the rent scale. Do not clamp.

Widen subtraction **before** `(utility-best)` in `Choice.Weight`; the current parenthesised `int`
subtraction can overflow before its multiplication is widened. Widen incumbent-plus-bonus before
saturation too. Test extreme opposite signed utilities, adjacent accepted/refused friction values,
zero friction, changed scale and changed μ. This guarantees only that friction alone does not make
an otherwise equal alternative impossible. Large real utility differences may still underflow.
Close 0068 D5 with a link to implementation tests when it ships, not at plan-writing time.

### D14 — The player can place and remove an Outside Connection

`Connect` edits Streets, and `Service` requires `Serves != Need.None`; neither can place the shipped
`port`. This missing reader is part of the capability, not a conditional follow-up. Add
`CommandKind.Gate` with a new unused enum value and `Command.Gate(east,north,kind)` factory.
The existing Tile fields name the exact Lot origin. `Zone` holds a declared gate kind for placement;
zero means remove the Outside Connection at that exact Tile. Values above the kind-id width are
refused before narrowing. Keep `Arrive`'s payload unchanged. `PLAYER GOVERNS`.

Place validates, in order: declared kind, `arrivals_per_day > 0`, a vacant live Lot at the exact
Tile, `World.EdgeOf(lot) != None`, a declared Hinterland on that edge, and usable Lot frontage.
Return a specific refusal for each failure, distinguishing a corner from an interior Tile where
the geometry can state that difference. Do not snap to a neighbouring gate, invent a Lot, pave a
Street or clear a Building as part of placement. On success call `World.CreateBuilding` once and
update the gate index. The player supplies ground through the existing Street/zoning tools;
`LotSubdivider.SubdivideAt` and `PaintParcelAt` are the existing ground paths. A gate is charged the
`placement_cost` its kind states, through row 32's mechanism; no price is invented here, and a
Ruleset that states none places a door for nothing.

Remove validates a live Outside Connection at that exact Tile and no Household or Business tenants,
then calls `World.DestroyBuilding`. A gate need not be marked abandoned: that is the ordinary
demolition tool's restriction, not a requirement for removing this infrastructure. An outside
queue is not a Building tenant and is handled through D5 on the next engine pass. Already admitted
Households retain their source edge and the existing move-in/gate-loss behaviour. Refuse occupied
mixed-use gate removal rather than silently adding compulsory eviction to this verb. Do not
change `Demolish`'s general permissions.

Add `gate` to `InputLogCodec`'s read and write vocabulary and test a round trip. The command record's
fields and log framing need no new width; old readers correctly refuse an unknown verb. Add the
Core refusal cases to `Simulation.Refuse`/`Explain` and the shell's refusal text. Extend `Verb`,
`Main.ToolDefinitions` and `Main.Verbs` with an **Outside Connection** tool in **Connections**, a
selector over declared gate kinds, exact-Lot preview, and Shift-click removal. Give it no competing
shortcut by default. The driven interface accepts `hold gate <kind-id>` and the same click/Shift
semantics; it must invoke the real command path. Selecting the Outside inspector remains a view.

For the second-gate demonstration, preserve a vacant edge Lot through existing zoning controls
before the run, and name that Lot in the recorded command. For housing intervention, remove a
frontage Street with `ConnectAction.Bulldoze`; `ApplyConnect` rebuilds frontage and invokes
`LotSubdivider.Resubdivide`, which supplies the existing loss-of-housing path. Record which homes
actually lost usable ground; do not assume every adjacent home did. Restore the Street and zoning
and let the existing construction/placement mechanisms respond. This avoids requiring the general
demolition tool to remove an occupied, non-abandoned home.

Tests cover a valid second gate and same-edge stock identity, unknown/non-gate/high-bit kind,
occupied/no Lot, interior/corner, missing Hinterland, missing frontage, placement followed by
admission on that Tick, empty gate removal with a queue, mixed-use occupied removal refusal,
double removal refusal, log round trip and replay. At least one driven episode places/removes a
gate with these actual commands.

### D15 — A family weighs buildable land as well as standing homes

After sampling standing Buildings, `PlacementEngine.Compare` samples up to `placement.candidates`
further vacant Lots within at most twice that many draws, on its own purpose tag
(`ProspectLandCandidate`), so the Building draws are unchanged. A Lot qualifies when the first
housing Zone Rule that answers the Unplaced Pool admits it (`ConstructionPermission`). Market-reading
Rules do not count, because they build for trade. The Lot is valued as that Rule's kind: the
kind's rent, the Lot's centrality, and the same affordability filter a standing dwelling gets. Lots
are de-duplicated like Buildings.

A family that prefers the land comes through the gate and waits in the Unplaced Pool. Construction
builds because the Pool is not empty, and placement houses the family. `gives_up_after_days` bounds
the wait if nothing is built in time.

**Why:** without land in the sample, a city founded from Ground attracts nobody. Arrivals compared
only standing homes, and housing is built only for Households already waiting, so each waited on the
other. Land counts at every city size, not only while the city is empty, so zoning new land draws
arrivals the way it does in other city-builders. There is no special case at zero homes.

**Measured:** `rulesets/base/` replaying `founding.borough` (seed 20,260,924, 16 zoned blocks, gate
on the south edge) grew from 0 to 366 Households and 582 Citizens in 12 Days, with 120 dwellings
built and at most 6 Households waiting at any reading. Before this rule the same log admitted
nobody.

## Inspection and demonstration contract

Add `HinterlandReading` in Core instruments: typed ids, enums and counts, no human-readable strings.
Use it from `ArrivalDump` and a new shell `Main.Hinterlands.cs` panel. Follow `Main.Threading.RequireWorld` and `AtBoundary`: read only once the simulation batch has
been collected, and queue inspector callbacks through `AtBoundary` while the worker owns the World.
Attach refresh to `Main`'s existing collected-batch readout/budget refresh path. Inspecting must not
consume draws, reset meters or mutate rows. This is an ownership boundary, not an assumption that
an independent immutable World snapshot already exists.

The panel opens from an **Outside** control available whenever stock is enabled, and from inspecting
an Outside Connection. Its four edge rows remain visible even with zero gates. Selecting an edge
shows composition and queue details. A gate selection additionally shows its own admitted/remaining
quota and explains that stock is shared with the other doors on that edge. Do not label all waiting
people “Unplaced”: distinguish **waiting outside** and **admitted, looking for housing**.

Required values, with Households and people labelled separately:

- Current total Outside stock, reserved subset, available stock, resting stock and composition.
- Current Day and last complete Day: autonomous/scenario occasions, no connection, ineligible
  composition/band, no feasible sampled home, Outside chosen, willing, admitted, queued, expired,
  changed-mind cancellation, connection-lost cancellation, returned, replenished and turnover.
- Gate admitted/remaining Households today; oldest outside wait; current Unplaced Pool; placements
  and give-ups over the stated interval. Do not attribute locally born/evicted Pool members to a
  gate merely because their placement occurred in the same Day.
- Current city people, births versus immigrant people, and the reconciliation residual. Residual
  zero is reported as an account check, not as an explanation of why the city is thriving.

Fresh considered occasions have disjoint outcomes: no connection, no feasible sample, Outside
chosen, or willing. Ineligible stock is a stock count, not a fictional series of comparisons.
Willing fresh prospects either admit or reserve. Queue retries have separate counters; they must
not inflate fresh interest. Cancellation restores availability, not replenishment. Day rollover
copies current flows into previous and clears current exactly once from `Simulation` before inputs,
including when no immigration engine work follows. A lazy UI read cannot perform the rollover.

Extend `ArrivalDump` to print this circuit automatically when `[immigration]` is present, and keep
its legacy mode labelled “explicit presentations”. Correct unconditional claims such as “an empty
Pool means construction kept up”: zero willingness or no connection can also empty it. Explain
scope in `Options.Usage`. Keep `--arrivals` and change its observation/driver branch by Ruleset
mode; do not add a second flag to enable a Ruleset's simulation mechanism.

### Authored demonstration and provisional starting numbers

Create `rulesets/attracted.toml` and a rent-transition partner `attracted-outside-cheaper.toml`.
Read `welcomed.toml`'s header as the starting admission example and a current Life Stage Ruleset as
the source of valid stage wiring. The new file must state its own demonstration and limitations;
do not copy the old anonymous-prospect or rent-only claim. Use existing Building kinds/assets.

**PROVISIONAL by taste, not measured:** reconsideration 2 Days, recovery 32 Days, maximum queue wait 2 Days and queue review 1 Day;
one-person Young low-band groups of 600 Households, two-adult/two-child Family middle-band groups
of 200, and one-person Tier 3 Empty Nest high-band groups of 100 per edge. Split Family adults
between Tiers 1 and 2 as in the schema fragment. Use existing valid stage definitions, with rent
weights Young 100, Family 150, Mature Family 150, Childless 100, Empty Nest 75. Start centrality
spreads at zero in the causal comparison fixture; the playable demonstration may retain nonzero
spread once the controlled ordering is proved. Start gate quota at 96 Households/Day per gate from
`welcomed.toml`, with its four Outside rents and city rent as a provisional baseline, **not a
prediction that these combined numbers will produce the required observations**.

These provisional values are starting content, and completing the demonstration includes tuning
and recording final values here. Increase stock or slow reconsideration if the first observation
already missed drawdown; shorten recovery for the demonstration if recovery cannot be watched;
change rent/centrality differences if all groups have numerically zero or certain willingness.
Do not tune an unrelated wage or tax hoping a utility term will read it. Preserve an untuned copy
or exact Ruleset hashes beside each observation so changed parameters cannot masquerade as results.

Use a controlled setup with actual spare dwelling capacity and priced homes, four real edge gates,
and initial Citizen capacity 1,000. Capacity is not a population cap. Headless run starts with
`--arrivals --ruleset rulesets/attracted.toml --citizens 1000 --ticks 131072` (64 Days), extends only
if the observations require it, and sends no `Arrive`. The shell demonstration may step to those
Ticks through the `drive` skill rather than require waiting in real time.

Required observations and their controls:

| Episode | Trigger and comparison | What must be visible |
|---|---|---|
| Autonomous onset | Step an empty-command log with feasible housing. | Positive admissions, Outside drawdown, real Unplaced membership and move-in Trips, no caller supplying prospects. |
| Selective depletion | Hold economy/housing fixed in a small controlled world; recovery disabled in this assertion variant. | Two feasible compositions with different utility have different per-occasion acceptance and remaining shares; no assertion of a universal Family ordering. |
| Scarcity and recovery | Exhaust a small stock, then run the recovery-enabled counterpart. | Admissions stop at empty stock, recover after realised replenishment, and never go negative. |
| Housing intervention | Fork one saved city; in treatment bulldoze a named frontage Street through `Connect` and let resubdivision remove its affected housing; leave control alone. | Fewer feasible offers or more waiting/give-ups attributable to the homes actually lost. Restore the Street/zoning and watch recovery where capacity returns. |
| Outside competition | Fork the same saved city; apply the rent-only partner Ruleset at a named Tick. | Current Outside utility and queue reconsideration change; stock is not reset. Compare admissions and declines against the unchanged continuation. |
| Shared gate constraint | In a saturated-queue controlled setup use D14's gate command to add a second gate on the same edge. | Greater admission capacity, same stock/sequence of fresh opportunities at the branch point, no second Hinterland created. Subsequent stock-dependent occasions may diverge because admissions differ. |
| Return | Admit, then force the existing unhoused give-up path with no feasible home. Include a locally formed Household. | Exact people credited to a named destination, Money leaves the city, and no identity retained. |
| Lost connection | Use D14's removal command on the sole gate while its outside queue is nonempty, then place one again. | Queue cancellation returns reservations, current stock survives, new gate resumes ordinary occasions. |
| Turnover | Return a non-authored mixed-skill composition above target. | Exact initial credit, later counted Outside losses, aggregate row eventually freed. |

Fixture-only direct construction may arrange targeted assertion states, but the shell episode must
use real Input commands for player interventions and a recorded Ruleset transition for Outside
rent. D14 supplies gate placement/removal; no UI-only counter stands in for it. The controlled
assertion tests may call the World creation door. No new Building model is required.

Record Ruleset hashes, seed, setup, Tick intervals, commands, stock/flow differences and actual
surprise here when observed. Use the `drive` skill for launch, readout and screenshot capture; this
planning pass does not launch Godot and does not claim those observations exist.

## Ordered implementation tasks

Each task leaves buildable code with its own meaningful checks. Tasks are dependencies within one
capability, not new amnesty rows. Keep changes under `src/` substantive; do not add placeholder
mechanisms merely to buy prose.

The integration order is fixed, including on the first Tick of a Day:

```
Reload(input)                         // a refused transition changes no population state
SealFoundingPopulationOnce()
RollPopulationAndHinterlandDayFlows()   // after reload validation, before any input transfers
ApplyInput(input)
Wake / Decide / Settle / Move / Layers // illness deaths in Move are classified here
existing phase-6 passes through SweepNeeds
HinterlandEngine:
  validate/rebuild invalid gate summaries
  recover stock; snapshot free allowances for this pass
  expire queues; cancel lost-edge connections
  process due timed reviews
  admit surviving old queue members while quota remains
  accrue and process fresh occasions from the captured allowances
  retire empty return-only groups; advance valid composition cursors
Placement / Employment / Zoning / remaining existing passes
existing end-of-Tick hash/readout boundary
```

Return credit during Placement is visible in this Tick's closing account and next Tick's
autonomous occasions. A phase-0 queued prospect can be reviewed/admitted later that Tick only
under D5's one-comparison rule. Every list removal fixes both neighbours and its owner's head/tail
before freeing the row; a freed group also advances any saved start cursor that names it.

| Task | Files/symbols to change or add | Completion evidence |
|---|---|---|
| **1. Data contract and arithmetic** | New `Rules/HinterlandPopulationRuleset.cs`; `Ruleset`, `RulesetLoader`, `RulesetKeyNotes`, `RulesetNames`; shared utility/choice arithmetic; relevant copying/validation helpers | Schema fragment loads as an actual fixture; invalid/orphan/duplicate entries fail with key/line; utility and horizon boundary tests pass; no generator yet. |
| **2. Stock and population ownership** | New `Entities/HinterlandTable.cs`, `HinterlandPopulationTable.cs`, `PopulationLedgerTable.cs`; `World` constructor/tables; member creation/destruction wrappers and `CivicEngine` death call; invariant enum and `WorldInvariants` | Opening counts and every population writer reconcile; exact return composition matches; deliberate missing/double updates fail. |
| **3. Prospect and common admission** | `ArrivalProspect`, `PlacementEngine`, `World.TryArrive` extraction/new `TryAdmitProspect`; `HouseholdTable`; new purpose tags | Compared purse/preferences equal admitted ones; Family has real children and adult credentials; invalid/full/exhausted operations leave all relevant state unchanged. |
| **4. Queue and autonomous engine** | New `HinterlandQueueTable`, `Rules/HinterlandEngine.cs`; `BuildingTable` gate links; `Simulation` phase 6 and start-of-Tick rollover | Empty-command arrivals, FIFO reservation/review lifecycle, shared-edge quota, depletion/recovery and gate loss work; no unsaved engine cursor or per-Tick allocation. |
| **5. Commands, Departure and transitions** | `ApplyArrive`, `RefuseArrive`/refusal enum and shell text; `World.Depart`, `TryOutside`; `World.Adopt`, reload/copy plumbing | Stock-aware commands cannot mint people; all Household Departures credit exactly once; locally formed and gateless cases work; allowed/refused reload matrix passes. |
| **6. Persistence and determinism** | `SaveHeader`, save tests, `RebuildDerived`, derived audit; golden generation as required | Mid-Day Factorio check, retry identities, reused slots, declarations and reconstructed indexes agree; replay/worker-count checks pass. |
| **7. Inspection and authored world** | New Core `HinterlandReading`; `ArrivalDump`, `Options.Usage`; `Main.Hinterlands.cs` and panel/readout hooks; two Rulesets; D14 gate command/codec/tool path | Typed/read-only inspection with correct units/reasons; headless emits no arrival commands; shell shows the four edges, queue and quota independently. |
| **8. Demonstration and closeout** | Driven script/artifacts and this plan's findings; 0068 D5/D6 links; amnesty status only after evidence exists | All episodes observed, surprise recorded, assertion gate green, relevant long-run/persistence obligations discharged; no claim based solely on a hexadecimal column. |

Task 1's shared utility change can affect existing choice-enabled worlds even without stock; record
that as a design change if it does. Keep a clearly scoped legacy branch only where D8 explicitly
requires it, not a second arithmetic implementation allowed to drift. Task 5 must not remain
unbuilt while task 4 is exposed in a shipped Ruleset: autonomous inflow with an unaccounted return
sink is an incomplete circuit.

## Acceptance tests — conditions and expected outcomes

New names below are proposed. Use existing `ArrivalTests`, `HinterlandChoiceTests`,
`PlacementChoiceTests`, `LifeStageTests`, `RulesetReloadTests`, `FactorioTests` and the corpus's
schema/reference checks as regression coverage. New tests default to assertion; only a measured
instrument opts out under the repository's tier rule.

| Area / proposed test class | Cases that must be written |
|---|---|
| **`HinterlandPopulationLoadTests`** | Absence preserves legacy mode; all-four-edge opt-in; missing durations/model/Money/Life Stages; duplicate composition; empty band; invalid tier-array shape/count/stage/child composition; zero target; product overflow; stable canonical ordering; copies retain settings; nested tables bind to the correct edge. |
| **`HousingUtilityTests` / `ChoiceTests`** | Same inputs give identical utilities for resident/prospect/Outside; stage rent and centrality terms both affect a nontrivial trade-off; affordability remains a filter at zero rent weight; zero spread deterministic identity; extreme signed subtraction; friction exactly at either side of actual underflow boundary; no feasible sample correctly labelled; duplicate sampled Building does not get extra probability; a city with no standing homes and vacant zoned housing land still yields a sample. |
| **`HinterlandStockTests`** | Opening Households/people exact; debit last Household then refuse next; fractional single-Household recovery eventually adds/removes; no overshoot; sign change clears wrong-direction fraction; disabled recovery inert; returns above target retained initially; queue reservations protected; zero-target group retires and its index entry vanishes. |
| **`PopulationLedgerTests`** | Founding seal once, including save before/after first Step; Tick-zero inputs separate; one birth; imported child not birth; child formation adds zero people; dissolution including children counted as removal not return; actual civic illness death counted separately, including the last adult; direct scenario creation/removal classified; Business destruction changes no people; deliberately omitted/doubled transfer trips invariant. |
| **`ProspectAdmissionTests`** | Exact evaluated purse through threshold affordability; mixed adult tiers and children created correctly; full/invalid/wrong-edge gate makes no stock/quota/Money changes; FIFO group reservation honoured; stock-aware legacy overload cannot bypass accounting; source edge survives placement/eviction/gate destruction; preference identity survives new Household id. |
| **`HinterlandQueueTests`** | Queue reserves but transfers no population; capacity serves oldest first; no immediate double draw on fresh willingness; scheduled review at a full gate uses current city/Outside and retained purse; willingness preserves FIFO rank but not an unlimited wait; review/admission on one Tick draw once; expiry at exact duration; no same-pass retry loop after expiry; sole-gate loss cancels; one of two gates lost reroutes; reduced quota below use admits zero; replacement gate's reused slot cannot inherit cursor identity. |
| **`AutonomousArrivalTests`** | No `Arrive` inputs needed; no gates means zero admissions without stock loss; no Building with room and no buildable housing Lot means zero admissions; same-edge gates share stock and occasions; empty stock cannot generate; occasional single Household not permanently rounded away; selective depletion under fixed conditions across a fixed seed ensemble, with no fragile single-draw monotonicity assertion. |
| **`StockArrivalCommandTests`** | Zero request; unsupported composition refusal; supported empty stock no-op; payload larger than remaining stock; reserved stock excluded; two commands on same Tick generate distinct identities; explicit and autonomous shares use the same quota; stock-disabled payload and replay behaviour retained. |
| **`HinterlandDepartureTests`** | Heterogeneous adult tiers/children; local formation; migrated then evicted; no live gate; destination differs from source; child-only unusual return counted/ineligible; empty Household; zero and out-of-band Money; dissolution and fixture destruction do not refill; non-authored composition recovers to zero. |
| **`HinterlandReloadTests`** | Mid-Day rent/taste/reconsider/recovery/wait edits, preserved fractions and SinceTick; lowering quota; queued purse retained when band changes; temporarily empty band; zero recovery off/on; refusal for stock/layout/target/edge/stage structure and Money family even alongside another shape change; refusal leaves State Hash and Ruleset unchanged. |
| **`HinterlandPersistenceTests`** | Save with nonzero two fractions, queue reservation, used gate quota, nonzero sequence, returned group and prior freed slots; N/save/load/M equals N+M per Tick; raw saved-table coverage; clear/rebuild derived indexes; old save-version refusal; new Input Log replay; routing workers 1/8 equal with admitted move-in Trips. |
| **`GateCommandTests`** | Every D14 placement/removal refusal; no mutation on refusal; shared stock, same-Tick quota, codec round trip, real shell command dispatch and replay. |
| **`HinterlandReadoutTests` / `ArrivalDumpTests`** | Observation changes no hash/draw/counter; current versus previous-Day rollover including idle Day; queued people not double-counted; fresh versus retry counts; no sample versus global no-home wording; births/formation distinguished; empty Pool does not imply successful construction; autonomous runner produces no injected commands. |
| **`HinterlandLongRunTests`** | At least 100,000 Ticks in a steady-flow controlled world: exact population/Money balances, non-negative stock, queue age bound, returned-group retirement/reuse and bounded occupied storage. Separate growing-city scenario verifies accounts without asserting its population is flat. Record any performance capture in `0013` under its measurement rules, not as an assertion's incidental wall time. |

For the directed rate comparisons use multiple fixed seeds and a fixture whose capacity is not
binding unless the test is about capacity. A city-side full gate can hide the preference effect;
a depleted stock can hide the rent intervention. Assert the diagnostic counters as well as the
admission count so a pass cannot be caused by the wrong bottleneck.

While implementing, use `scripts/test.sh` with the relevant class/namespace filters. Before a
commit run `scripts/test.sh`. On milestone closure run the repository's unfiltered Release suite
and required long-run gates. Regenerate the TOML schema and key reference through Headless, then
run their consistency tests; do not hand-edit generated files. Use the drive skill when watching
shell changes. A documentation-only review needs link/format and corpus-budget checks, not Godot
or the entire simulation suite.

## Completion record — to fill from the built result

Do not replace this section with predicted outcomes. Record final provisional settings and why
they changed, commands and Ruleset hashes for the observation episodes, actual population-account
results, persistence/determinism evidence, tests run, relevant performance findings, and what
watching surprised the implementer with. Route defects to their owning task immediately. The row
closes only when autonomous people enter, housing and Outside changes alter that flow, returns and
recovery add up, and the player can distinguish the causes on screen.

### Ruleset hashes

Measured through `RulesetFile.HashOf`. Every episode below ran on `--citizens 1000`.

| File | Content hash |
|---|---|
| `rulesets/attracted.toml` | `0xBF8F4407E4C78272` |
| `rulesets/attracted-outside-cheaper.toml` | `0xCC003D5CA871F64D` |
| `rulesets/attracted-declining.toml` | `0xE524A0B61FB74912` |

### Final provisional settings and why they changed

`attracted-declining.toml` was added during task 8 and nothing else was retuned. `attracted.toml`'s
circuit **stops from Day 58**: Pool, placements and give-ups all read zero, the Money supply freezes
at 1,755,996, and 3,519 Households still stand behind the four edges with none willing. Every
long-run assertion would have passed in that world *against an immigration engine that had been
deleted*, which is why the steady long-run fixture uses the declining file instead. It differs from
`attracted.toml` by two keys only — `condemn_after_days = 2` and `collapses_after_days = 1` — and its
header records that its population falls (827 against 1,377) and that the fast decline serves a
test's horizon rather than making a claim about cities.

### The observation episodes

Four were driven through the shell; the rest are headless dumps or assertion tests. That split is
recorded rather than smoothed over, because the plan asked for driven demonstrations and only
episodes 4, 6 and 8 have one.

| Episode | How | Measured result |
|---|---|---|
| Autonomous onset | headless `--arrivals --ticks 4096` | Pool **366** at Tick 4,096; Citizens 1,566, Buildings 111, vacant Lots 25. Stock west 820 / east 900 / south 895 / north 714, north reserving 68. North admitted 96 today with 68 queued, and no `Arrive` command was issued. Also `AutonomousArrivalTests.People_arrive_without_a_single_Arrive_command`. |
| Selective depletion | assertion tests | 30 tests green across `AutonomousArrivalTests`, `HinterlandStockTests`, `HinterlandDepartureTests`. Corroborated over 50 Days: west 900→642, north 900→166, east 900→1,956, south flat ~899. No driven run, no per-episode figures. |
| Scarcity and recovery | assertion tests | Same 30-test lane, plus `HinterlandReloadTests`' recovery-off and recovery-on cases. No driven run. |
| Housing intervention | **driven**, `--start-at 4096` | See below. |
| Outside competition | headless pair, `--reload-at 2048` | Control byte-identical to the stored onset dump, treatment differs. Numeric tables were never printed after the fix, so only that verdict stands. Regression test `ArrivalDumpTests.A_ruleset_transition_reaches_the_circuit_and_leaves_the_day_before_it_alone`. |
| Shared gate constraint | **driven**, south edge | Gates 1→2, quota 96→192, stock unchanged at 893 Households / 1,478 people. Pool 366→469. Plus `GateCommandTests.A_second_door_on_one_edge_shares_the_stock_behind_it`. |
| Return | assertion tests | 11 `HinterlandDepartureTests` cases, plus `HinterlandLongRunTests.A_returned_group_is_opened_and_later_retired`: 34 groups opened beyond the 12 authored, live count falling on Days 24, 37 and 39. |
| Lost connection | **driven**, north edge | Removing the sole gate against a 52-Household queue cancelled exactly **52**, left stock untouched at 714, and a replacement door on the vacated Lot resumed ordinary admissions — 2 willing, 2 admitted by Tick 4,300. The other three edges did not move. |
| Turnover | assertion tests | East sits above its resting count and is trimmed: 900→1,956 over 50 Days, turnover 11 today / 9 yesterday in the 64-Day run. No driven run. |

### Episode 4, in full, because its premise was wrong

**The plan's stated mechanism does not exist in this codebase.** This row asked to "let resubdivision
remove its affected housing", but `LotSubdivider.Resubdivide` frees a Lot only when it is unfronted
**and** vacant. `adr/0079` and `02 §2.2` both state that a Building whose last Street is bulldozed
keeps standing and keeps its Occupants, and `SimulationTests.A_building_survives_losing_its_street_and_a_vacant_lot_beside_it_does_not`
already asserts exactly that, including the vacant-Lot deletion. That test's own remark rejects the
mechanism this plan assumed: `ZoneRuleEngine.Condemn` is keyed on a starving Rule Instance, and a
bulldozed Street starves nothing.

The first attempt demonstrated the rule rather than the plan. Bulldozing Segment 130,304 — frontage
"3 Lots · 0 vacant", Buildings 61, 62 and 63, all dwellings — produced a treatment **identical to
the control** at Tick 6,144 and 8,192. The Street did go: the hover changed to "LAYS a Street on the
edge running EAST from (8,160, 8,128)", and a re-lay stood a new Segment 536,525 there.

Capacity moves only where vacant Lots lose their frontage. Four Segments were cut one per Tick from
Tick 4,096 and re-laid one per Tick from Tick 6,144 — one command a Tick deliberately, because the
backlog records a shell crash when two arrive in one Tick.

| Segment | Aim | Vacant frontage |
|---|---|---|
| 130,303 | (8144, 8128) | 3 of 3 Lots |
| 130,816 | (8176, 8160) | 3 of 5 Lots |
| 131,328 | (8176, 8192) | 2 of 5 Lots |
| 392,959 | (8128, 8144) | 2 of 2 Lots |

| Tick | Control | Treatment |
|---|---|---|
| 4,096 | 1,566 / 111 / **25** | — |
| 4,112 (four cut) | — | 1,669 / 111 / **15** |
| 6,144 | 1,604 / 112 / **24** | 1,604 / 112 / **14** |
| 6,160 (four re-laid) | — | 1,603 / 112 / **24** |
| 8,192 | 1,395 / 112 / **24** | 1,395 / 112 / **24** |

Citizens / Buildings / vacant Lots. The ten vacant Lots lost are exactly the 3 + 3 + 2 + 2 measured
beforehand, and re-laying returned all ten. A two-Segment control on the same world took 25 to **19**,
exactly the six predicted. **Buildings never fell** — 111 to 112 throughout — so not one occupied
dwelling was removed, which is `adr/0079` behaving as specified. By Tick 8,192 treatment and control
agree exactly. ⚠ The Citizens figure at Tick 4,112 is not comparable to the control's 4,096 sample;
vacant Lots is the measured variable here.

**What the shell cannot show.** The Unplaced Pool is unreachable from a driven run. The Outside
panel's text never reaches the `readout` caption, and `--arrivals` refuses `--log`, so the
intervention's own Input Log cannot be replayed for the account. So episode 4's "more waiting or
give-ups" half rests on the assertion tests above and not on this run.

### Population and Money account

From the 64-Day `attracted.toml` dump: people **1,377** with 1,377 rows standing and residual **0**;
Households **470** with 470 rows standing and residual 0; admitted-unhoused 0. Money conserved
exactly — supply 1,755,996, walked 1,755,996, no flow term — with 1,659,090 held by Households and
96,906 by the treasury. 1,955 people were admitted over that run.

### Long run

`HinterlandLongRunTests` exercises two fixtures for 102,400 Ticks each at seed 11 and 1,000
founding Citizens: `attracted-declining.toml` for continuing flow and `attracted.toml` for a city
that fills. The corrected queue comparison changes which families reach the city and return.

The storage check discards one authored recovery period before comparing tail halves, rather
than a fixed eight Days. Recovery takes 32 Days in this fixture; the earlier window measured
initial adjustment as well as later growth. A Release run of the corrected engine measured:

| Reading | Result |
|---|---|
| groups, opening to Day 50 | 12 → 47 |
| old Day-8 tail means | 36 → 43, 19.4% drift |
| Day-32 tail means | 42 → 45, 7.1% drift against the unchanged 12.5% tripwire |

This is a finite-run regression check, not proof of a plateau. The fixture must still exercise
admissions, willing occasions, queueing and retirement, and both worlds must reconcile population
and Money. Fixture construction time is shared by the assertions and is not a per-Tick timing.
The original demonstration measurements remain in Git at `35c20dd`.

### Persistence, determinism and tests run

Full working lane green after review fixes: **3,593 tests, 0 failed, 5m09s**
(`scripts/test.sh -- --no-restore`). This includes persistence, replay, worker equivalence and
both 50-Day fixtures. Regression cases cover queue cancellation with no feasible housing,
composition overflow and batched retirement with slot reuse. Queue fixtures now establish a
currently willing prospect where admission is required. `scripts/format.sh --check` and the Godot
build passed. No instrument tier was run and no performance figure is claimed.

The review smoke used the compatibility renderer, `attracted.toml`, 1,000 founding Citizens and
Tick 128. The four-edge panel opened, seven people had arrived, and both population residuals
were zero. Forward+ hit an instance resource limit; the full socket-driven panel check did not
complete. This smoke verifies the overview only. Shell edits in this review are comments only;
a C# token comparison confirmed that the comment cleanup did not change executable shell code.

### Defects this work found

| Fixed | What |
|---|---|
| `270598e` | **Two of four edges could not be clicked at all.** An edge Lot on the north or east boundary anchors at exactly `CellGrid.WorldTiles`, which converts to a Cell one past the last, so `Aim` refused a cursor there and `GateNear`/`VacantNear` compared that unreachable Cell. The gate tool could not reach the north or east doors by hand or by script, though D14 promises all four. No test could catch it — `GateCommandTests` builds `Command`s directly and `Main.Verbs.cs` does not compile into the test project. The fix clamps the Lot's Cell for comparison only, so Core still receives the true anchor. |
| `ea575d5` | `--arrivals` silently dropped `--reload-at`, so a transition run read and hashed the second Ruleset, never switched to it, and reported it anyway. The header also named only the opening Ruleset. |
| `4afc887` | `attracted.toml`'s dead circuit, above, and the long-run tests that would have passed against it. |

Open, and not this row's: `AllocationProbe`'s failure message directs a reader to `plans/0002` §D and
`plans/0003` item 13, and **both are retired tombstones**. `AllocationAssertionTests` pins the dead
pointer with `Assert.Contains("plans/0002", message)`, so the test changes with the message.

### What watching actually surprised the implementer with

1. **The plan was wrong about the corpus, and the corpus had already written the correction down.**
   Episode 4 was scoped around resubdivision removing housing. It cannot. The test that asserts the
   real behaviour also contains a remark rejecting the exact mechanism this plan assumed, which means
   somebody had made and recorded this error before.
2. **A shipped Ruleset can make a long-run test meaningless without failing it.** `attracted.toml`'s
   circuit stops dead at Day 58 while 3,519 Households wait outside. Every balance assertion still
   passes, because conserved nothing is still conserved.
3. **A visible capability was unreachable on half its surface and no test could have said so.** The
   gate tool never worked on the north or east edges. The defect lived in the one file the test
   project cannot compile, which is precisely why the driven run is the guard.
