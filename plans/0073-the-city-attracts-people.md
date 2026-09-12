# 0073 — The city attracts people

**[`0045`](0045-amnesty.md) queue row 31. Implementation plan, expanded 2026-09-11 against
`main` at `7ea9ab5`.** Continues [0068 D6](0068-the-choice-model.md#decisions) and the stock half of
[`adr/0023`](../docs/adr/0023-immigration-arrives-through-the-gate.md).

## Status, authority and scope

**SCOPED; NOT STARTED.** This replaces the initial scoping draft's three unanswered branches with
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
that ever appeared. Non-authored rows with zero stock and no queue reservations are freed,
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
no connected city alternative and no Lot sampling.

The Outside carries the existing moving-friction bonus when an unadmitted prospect compares leaving
its current home. A city Household's incumbent carries that same bonus; a Household already in the
Unplaced Pool carries none. This explicitly changes the old prospect path's frictionless comparison
in stock-enabled worlds. The chosen dwelling is evidence for willingness, not a reservation or a
promise of a particular address. Queue members retain purse and identity; they do not retain a
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
with retained purse/identity and current conditions. A decline cancels its reservation; willingness
keeps its place in the admission FIFO and appends it to the review-list tail with the scheduled
review Tick updated. A scheduled review never resets `SinceTick`, so it cannot make the waiting
bound infinite. A fresh enqueue starts both clocks on its enqueue Tick. An admission or any
cancellation unlinks the row from both lists. Because every row uses the same current interval,
changing that interval on reload preserves review-list ordering.

When an edge has capacity, process its admission FIFO in order. Re-evaluate each old queue member against
current housing and Outside conditions, using its retained purse and identity, a new Tick draw and
current preferences. A decline cancels its reservation; a willing result tries admission. Stop when
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
`LotSubdivider.SubdivideAt` and `PaintParcelAt` are the existing ground paths. No new construction
price is invented here; capital expenditure remains row 32's scope.

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
| **`HousingUtilityTests` / `ChoiceTests`** | Same inputs give identical utilities for resident/prospect/Outside; stage rent and centrality terms both affect a nontrivial trade-off; affordability remains a filter at zero rent weight; zero spread deterministic identity; extreme signed subtraction; friction exactly at either side of actual underflow boundary; no feasible sample correctly labelled; duplicate sampled Building does not get extra probability. |
| **`HinterlandStockTests`** | Opening Households/people exact; debit last Household then refuse next; fractional single-Household recovery eventually adds/removes; no overshoot; sign change clears wrong-direction fraction; disabled recovery inert; returns above target retained initially; queue reservations protected; zero-target group retires and its index entry vanishes. |
| **`PopulationLedgerTests`** | Founding seal once, including save before/after first Step; Tick-zero inputs separate; one birth; imported child not birth; child formation adds zero people; dissolution including children counted as removal not return; actual civic illness death counted separately, including the last adult; direct scenario creation/removal classified; Business destruction changes no people; deliberately omitted/doubled transfer trips invariant. |
| **`ProspectAdmissionTests`** | Exact evaluated purse through threshold affordability; mixed adult tiers and children created correctly; full/invalid/wrong-edge gate makes no stock/quota/Money changes; FIFO group reservation honoured; stock-aware legacy overload cannot bypass accounting; source edge survives placement/eviction/gate destruction; preference identity survives new Household id. |
| **`HinterlandQueueTests`** | Queue reserves but transfers no population; capacity serves oldest first; no immediate double draw on fresh willingness; scheduled review at a full gate uses current city/Outside and retained purse; willingness preserves FIFO rank but not an unlimited wait; review/admission on one Tick draw once; expiry at exact duration; no same-pass retry loop after expiry; sole-gate loss cancels; one of two gates lost reroutes; reduced quota below use admits zero; replacement gate's reused slot cannot inherit cursor identity. |
| **`AutonomousArrivalTests`** | No `Arrive` inputs needed; no gates means zero admissions without stock loss; no feasible housing means zero admissions; same-edge gates share stock and occasions; empty stock cannot generate; occasional single Household not permanently rounded away; selective depletion under fixed conditions across a fixed seed ensemble, with no fragile single-draw monotonicity assertion. |
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
