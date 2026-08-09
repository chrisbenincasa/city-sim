# 0000 — The board

**Read this first.** A flat, scannable status of everything planned and everything done, and the one
place that orders the three tracks against each other.

It is a *view*, not a source: [`0003`](0003-build-plan.md) owns the slice order and its gates,
[`0002`](0002-open-questions.md) owns **every open question**, `docs/adr/` owns the decisions. When
they disagree, they win. Update this file whenever a task lands.

**Do not write an open question here.** That is how this file grew to 63 of them while the file named
*open questions* held none — and a board that is also the ledger is not a view of anything.

---

## State of play

Plain summary, kept deliberately short. Everything below this section is dense and argumentative;
this part is not. If the two ever disagree, the detailed sections are right and this one is stale.

**What the project is.** A city-builder whose simulation is an ordinary C# library with no game
engine inside it. Godot will be the display layer and has not been started.

**What you can run today.**

```
dotnet run --project src/Borough.Headless                                   # build a city, print its tables and a State Hash
dotnet run --project src/Borough.Headless -- --seed 1 --ticks 10000         # step a session and print a hash trace
dotnet run --project src/Borough.Headless -- --layer pollution              # print a Map Layer as an ASCII field
dotnet run --project src/Borough.Headless -- --help                         # every flag
```

**What works.** Typed tables where each field is declared once as saved or derived. Integer-only
arithmetic, no floats anywhere. A deterministic eight-phase Tick. An input log that replays to
identical hashes. Map Layers with diffusion. A census of collection sizes. A crash artifact that
replays back into the same crash. Build-time analysers that turn the determinism rules into
compiler errors. Bin Rules: Buildings hold Bins, Rules move Goods between them atomically, a Rule
that cannot run sleeps on the Bin that stopped it instead of retrying, and a failed Rule walks a
fallback chain that ends by recording why. The census also reports what the Rule engine did — how
many Rules came due, how many evaluations that cost, and how deep the chains went.

**What does not exist.** Three of the eight Tick phases are still empty — movement, growth, and most
of commit. There are no jobs, no money, no movement, no traffic, no roads, no buildings growing or
declining, and no renderer. Citizens exist as rows and do nothing at all. Bin Rules now run, but **no
Ruleset file exists anywhere in the repository** for them to run, and **no Ruleset has ever reached a
`World`** — the loader, the engine and the tests build theirs in code, `Replay.Start` hard-codes the
empty one, and the runner hashes `--ruleset` without ever parsing it. Shipping that wiring is slice 7
**task 10a**. What it will *not* ship is a supply chain: a chain between Buildings crosses an
ownership boundary, which is the District Pool, which throws.

**Scale.** A million Citizens fit comfortably: 86 MiB of tables, about 94 MiB resident, and 100,000
Ticks in 11.75 seconds. Whether a Tick is *fast enough* is still unknown, because the Tick is nearly
empty — but four of its consumers now have real prices, summed in
[`0013`](0013-tick-budget.md). **The clearest result is that the answer depends on a decision nobody
has made.** Everything priced so far fits comfortably in a Tick at **1× or 2× speed** and does not fit
at **4×** — and 4× is where the project's stated 15.6 ms budget comes from, on no recorded argument.
Two of the four priced rows also rest on guesses about how big a city's workload is, so the total is a
thing to watch rather than a problem to fix. S0b is what replaces the guesses.
Nothing measured so far suggests C# is the wrong choice; see `docs/adr/0036` and S4 in
`docs/spike-results.md`.

**What is next.** Slice 7, the Rule engine, **is under way — tasks 1–9 of 10 are done**. Bins, the
wait list, the Ruleset loader and its refusals, quoted decimals, Rule evaluation with atomicity,
the apply count, the Readout set, `on_fail` chains, and the counters. **Only task 10 is left, and
planning it split it in two.** As written it asked for a production chain over two or three Goods —
which is `pool`, which the same document had already made a named hole that throws. **10a is the
wiring**, and it is the last thing standing between the engine and a world; **10b, the content, is
re-filed to Phase 2.** Four findings came out of the planning before a line was written, and one of
them was a live defect in slice 6: **a `map` emission accumulated with no sink**, so the corpus's own
worked Rule grew a magnitude for ever — and slice 6's long-run test, the one built to catch exactly
that, had been written to work around it in the fixture. Settled the same day by
**[`adr/0051`](../docs/adr/0051-industrial-pollution-is-a-stock-the-environment-absorbs.md)**, which
turned out to be **less a decision than an excavation**: `02 §2.4`'s field table had said pollution
*"Decays"* since it was written, and slice 6 built the diffusion without the removal. The ceiling is
now emergent rather than clamped, and the two things it left behind are honest ones — **tau is
hash-bearing and unratified**, and **decay fights slice 6's incremental re-diffusion**, which is
measurable and went to S0b rather than to an argument. *(Slice **8** is hot reload — a different thing wearing the same
number as task 8, which this section once confused.)* **The Readout set has exactly one member**
— `occupancy` — because a Readout is admitted when a Rule reads it, and nothing else in the world is
read by a Rule yet; every other scalar `CONTEXT` calls a Readout is refused by name at load. Between
tasks 7 and 8 the **Resource family** was taken out of order, because a Resource declared a name and
neither its family nor either of `CONTEXT`'s two parameters — so `money` was a Good with a warehouse ceiling
and **`02 §4.3`'s own bakery destroyed one money per baking**, in a document six slices old. `family`
is now required with no default, a money Bin is unbounded, a fourth refusal enforces `adr/0024` on
every Rule, and `storage` is a named hole. It reached six slices because **the transcription into the
loader's own test fixture had silently dropped the money line** — a green suite agreeing with the code
instead of with the document.

Task 8 found **the same shape a second time, in a green assertion rather than a fixture**: every chain
link was being armed as its own Rule Instance, so a reporting terminal would have fired on its own
`rate` for ever with no shortage and no head — `adr/0045`'s polling defect arriving through the
instance table instead of through the walk. What hid it was `RulesOf(1) == 4`, a **count**, which
reads as *the bakery has four Rules* and means *the bakery will run four Rules independently*.

Task 9 found **a third instance, and this one was in an instrument rather than in a test**: `02 §4`
asks for a tripwire stated over *Rule evaluations per Tick*, and the counter it was to be stated over
counted **due Rule Instances** — which a chain walk does not move by one. The wire would have read
*fits* at every chain depth including ones that do not. It also produced the first number the project
has for what a Tick actually costs: **82.84 ns an evaluation**, so 15.6 ms holds ~188,000 of them —
and against the corpus's own unratified multipliers, **a 1M city spends ~60% of a Tick on Rule
evaluation alone**, which is a floor because Phase 3 is unmeasured and sorts its intents. The
comfortable half is that **chain depth is the cheap axis after all** — a rung costs 53.6 ns against
the head's 82.84 — which is the first evidence behind a claim `02 §4` had been making unsupported.

**Known problems, none urgent.**

- The synthetic city fixture and the table sizing ratios disagree; Households land at exactly
  capacity, so the first one the simulation creates will grow the table.
- Routing does not fit the Tick budget, and R6.3 found the reason is not the one we had. Starting a
  trip is cheap, because a Citizen reuses the route it worked out once. What is expensive is a driver
  changing its mind mid-journey, which the design made routine and then removed the cheap way of
  serving. That costs about nine Tick budgets at the traffic level we expect a full-size city to
  have, and up to twenty-four at the top of that range. A route cache
  cannot cover it. The likely fix is to let a driver take a different road and then rejoin the route
  it already had, rather than work out a new one — a design decision, not an algorithm.
- The S0a timings were taken with the CPU governor on `powersave`, so the absolute figures are
  upper bounds. Ratios are unaffected.
- Several documents still describe behaviour that later measurement contradicted. They are listed
  under *Owed* at the bottom of this file.

**Where things live.** Five files, five questions, and each answers exactly one.

| | Answers | |
|---|---|---|
| **this file** | *what is next* | Status, and the only place that can order three tracks against each other |
| [`0003`](0003-build-plan.md) | *what is done* | The slice ledger and its gates. This file's *Done* section is a view of it |
| [`0002`](0002-open-questions.md) | *what needs answering* | One ledger, every entry typed *measurable* or *arguable* and grouped by what is blocked. Its session history is an archive below the ledger and is not status |
| [`0012`](0012-corpus-audit.md) | *what a document says wrongly* | Corrections, which are not questions — nobody has to decide anything, somebody has to type. It deletes itself when empty |
| [`0013`](0013-tick-budget.md) | *what a Tick costs* | The Tick budget ledger. A **view** like this file — every figure cites its owner — and what it adds is the property no owner holds: **the sum, and how much of it rests on numbers nobody has measured** |

`docs/adr/` owns settled decisions, `CONTEXT.md` owns the vocabulary of the city, and
[`PROCESS.md`](../PROCESS.md) owns the vocabulary of the project — what a slice is, what a spike is,
what a gate is, and the two words that unfortunately mean two things each.

---

**Where the project is.** Phase 1. Slices 0–6 are closed and the Phase 1 gate with them; **slice 7 is
in flight at 9 of 10 tasks**. The spike track has run S4, S0a and S2 R0–R8. The argument track has
closed sessions A, B, eight and nine; **C is the only session still gating a slice**.

Detail lives where it is owned — slice narrative in the slice plans, spike numbers in
[`spike-results`](../docs/spike-results.md), reasoning in [`0002`](0002-open-questions.md). What
follows is only what a reader needs before choosing what to do next.

**S2 retired the risk it existed for** — *"pathfinding is slow at a load nobody measured, on a map
already committed to"*. The map survives, the budget is known, the path source is chosen, and R8
closed the congestion loop. Three results outrank the rest, because they moved things outside the
spike:

- **The network runs out of routes, not road.** 87.25% of traffic on 1% of the carriageway, 90.87% of
  it empty, at 13% of holding capacity with capacity confirmed realistic — because one free-flow tree
  per District means one route per (node, District) pair in the whole model. That is **decision 11 on
  a different axis**, and it is a design question rather than a spike's.
- **No cluster size fits routing into the Tick budget.** 85 Trip starts per Tick, U-shaped in cluster
  size and pinned at both ends, so it is a floor rather than a rung that was missed. This promoted
  **R6's route cache from a tidy-up to the only exit**, and `adr/0047` has since removed the third
  option nobody had noticed was in reserve.
- **The origin-destination draw had been hiding a conclusion.** A uniform draw is the longest-trip
  distribution available; on a local-trip draw the same table's detour goes from 18.52% to
  **128.82%**, which under `05 §4` is a different city. The draw is now a swept family.

**One caveat travels with every S2 figure and belongs to all of them.** Everything R1–R5 published ran
on a **frozen cost basis** — a route was invalidated because a road was *bulldozed*, never because one
got *busy*. R8 closed that loop for itself; quote nothing from R1–R5 as a statement about a congested
city.

**R5 left four debts, three of which still want a decision.** They are itemised under *Owed*. The one
that grew a second half is **M**: the path source is not one choice, because a maintained table and a
cache are wrong in **different currencies** — structural against temporal. No benchmark ranks those;
`05 §4` does.

**The protocol defect worth remembering.** R5's canonical capture found `routing-run.sh` pinning to one
logical processor, so the JIT's background compilation landed on whatever was timed first — 4.88×
apart within a single capture. Counts and in-process ratios are untouched; **first-timed absolutes in
R0, R1, R3 and R4 are not**, and R7 owns the re-capture.

---

## Do these next

**Build. The argument track is not the constraint any more, and treating it as one is how this
project starts going in circles.**

That is a correction to what this section used to say, and it is written here because the failure was
observed rather than predicted. The board had ordered *argument first* on the reasoning that nothing
is gated on code. True, and it produced a session in which **every decision generated two or three
more** — `adr/0046` alone spawned four unratified numbers, `adr/0047` found defects in two other ADRs
on its way past, and three separate findings arrived labelled *the third instance of a pattern*.
**The design was generating design.** There are 51 ADRs and the game is at slice 7.

**S2's risk is retired** — see *Where the project is*. Everything after that is refinement, and
refinement has no stopping condition, so it needs an external one. This is it.

**The rule from here: an argument session runs when something concrete is blocked on it, and not
because it is available.** The three tracks still do not contend — the code track is somebody at a
keyboard, the argument track is a grilling session, the spike track is a machine running unattended —
but the code track leads.

| | Track | Task | Where | Why this one |
|---|---|---|---|---|
| **1** | **code** | **Slice 7 — the Rule engine**, task **10a** | [`0011`](0011-rule-engine-bins-and-rules.md) | **Gate cleared, 9 of 10 done, and task 10 split while being planned** — it asked for a *production chain over two or three Goods*, and a chain between Buildings is `pool`, which the same document had already settled as a **named hole that throws**. A plan contradicting itself, unnoticed for seven tasks (`0011` finding 34). **10a is the wiring and is the whole of what is left**: no Ruleset has ever reached a `World` — `Replay.Start` hard-codes `Ruleset.Empty`, the runner hashes `--ruleset` and never parses it, and nothing outside a test turns a declaration into a Bin. Four pieces, one of them **hash-bearing** (the arming stagger, which needs a `purpose_tag`). **10b, the proving chain, is re-filed to Phase 2** with decision owed 4, because sustained churn with `local` and `map` alone provably requires a closed loop inside one Building, which is a lie about `04 §1` (finding 35). One thing to know before starting: **scheduled dispatch needs the Event Wheel, and that is slice 9** — most of slice 7 does not, since a subscription is woken by the mutator, but the `rate` re-arm is |
| **2** | spike | **S2 R6 — the two caches** | [`0010`](0010-s2-routing.md) | **Promoted, and its stakes went up.** R3 called a cache *"one of only two exits"*; [`adr/0047`](../docs/adr/0047-routing-never-keys-on-the-district.md) has now removed the third option nobody had noticed was in reserve, so **R6 is the only exit.** It owns the two numbers routing still has open: the cache's **key granularity** — a different number with a different error from the matrix's — and the **eviction policy**, which R5 measured as the bigger lever below the highest edit rates (28–31% of lookups missing on direct-mapped collisions before a road is touched). It inherits R3's stored path arena and a `HpaSearch` pristine-seeding defect R5.5 found. *Runs unattended; does not contend with row 1* |
| **3** | spike | **S2 R5.6 — the Parking Shed**, then **R7 — the report** | [`0010`](0010-s2-routing.md) | The second Epoch consumer, and the last section of R5. It scales with **Buildings** and is a *neighbourhood* rather than a *path*, so per-Segment has no obvious meaning for it. **`CONTEXT.md` → Epoch must not be updated until it runs.** R7 then closes S2 and deletes the harness — and owes a re-capture of R0/R1/R3/R4, all of which carry the one-processor artefact |
| **4** | argument | ~~**`adr/0015` — hot reload**~~ **DONE** | [`adr/0048`](../docs/adr/0048-the-ruleset-is-validated-where-it-is-parsed-and-only-integers-and-strings-cross-into-the-core.md) | Ran, and cleared **two** slice gates rather than one. See *Done*. Nothing in this track is now blocking code, so the next session is chosen by what gets blocked next — not by what is available |

*Why S2 first now:* the argument for delaying it was that the golden baseline should exist before
throwaway spike code starts changing `Core`. It does, and the runner is what a person uses to look at
what moved. Slice 5 is closed and no longer in front of it. **R0, R1 and R2 confirmed the delay cost
nothing** — the spike compiles the arithmetic substrate in by source and can name nothing else of
`Core`, so it has changed no simulation code at all.

*Why the argument sessions dropped down the board:* ~~every remaining Phase 1 slice and every Phase 2
milestone is gated on one, and none of them is gated on code~~ — still true, and it turned out to be
the wrong reason to run them first. **Availability is not priority.** The table below is a menu, not a
queue; take from it when something concrete is waiting, and leave it alone otherwise.

---

## The argument track — what stands between here and Phase 2

**Phase 2 is not blocked by code.** Every milestone in it (`06` 5a–10) waits on a design written from
research and never argued, on one spike, or on a number nobody has chosen. `0002`'s readiness review
states the shape plainly: *Phase 2's wall is one large item, not many small ones.*

**None of these contends with the code track**, unless the last column says otherwise — an argument
session is a sitting, and the code track is somebody at a keyboard. Ordered by what they unblock,
soonest first. Each is a session, not a task.

*The last column's answers were derived against **slice 6**, which has since closed.* They are
retained rather than re-derived, because what they record is whether a session needs a **measurement
or a design that does not exist yet** — which is a property of the session, not of whichever slice
happens to be open. The one row that named a specific dependency, **I**, still names it: S2 R6.

*Slices and milestones share numbers and are not the same thing* — slice 8 is hot reload and
milestone 8 is parking — so the *Unblocks* column always says which.

| | Session | What is actually missing | Unblocks | Runs beside code |
|---|---|---|---|---|
| ~~**A**~~ | ~~**`adr/0015`** — hot reload~~ **CLOSED** — see *Done*. Produced `adr/0048`, named Tomlyn, put the validator in `Borough.Formats`, found `adr/0003`'s exception was never owed, generated a third refusal, retired `06`'s ordering claim and corrected a `CONTEXT.md` sentence that argued against itself | ~~**No longer "never grilled at all."**~~ `adr/0045` hands it **two named refusals** — the `on_fail` cycle check and the `fills` check — both load-time Ruleset validation on the error surface this ADR already specifies. Plus `06`'s *must not slip behind 3c*, which is unargued and circular, and the **TOML dependency exception**, since a parser is what runs the refusals | slices **7** and 8 | **yes** |
| ~~**B**~~ | ~~**`02 §4` residue**~~ | **CLOSED** — see *Done*. Produced `adr/0045`, struck `mean_workforce_experience`, inverted the Readout bound, and settled apply count. Its cycle-checking half moved to **A**, which is what moved A onto slice 7's gate | ~~slices 7, then 10~~ | — |
| **C** | **`02 §7` + `adr/0006`** — Event Wheel | Both never grilled. `02 §7` is partly spoken for by `adr/0033` and must be **read against it rather than fresh** | slice 9 | **yes** |
| **D** | **`03 §5`** — the traffic model | **The wall.** The most detailed unargued design in the project, now carrying transit vehicles. It is one large item and should be booked as more than one sitting | milestones 5b, 5c, 6, 7a | **partly** — the half that wants S2's numbers waits for R1–R3; the rest does not |
| **E** | **`adr/0005` + `adr/0007`** — fidelity | One session, not two: `0007` moved Fidelity from person to **place**, and `0005`'s tiers are what it moved. Written from research, not argued | milestones 7a, 7b | **yes** |
| **F** | **`adr/0008`** — walking is a simulated Leg | Written from research. It is what makes 5b *the irreversible milestone*, so the argument is owed before the Leg model is built rather than after | milestone 5b | **yes** |
| **G** | **`adr/0016`** — the lane is the entity | Written from research. Carries the order-of-magnitude claim the whole microscopic tier rests on | milestone 6 | **yes** |
| **H** | **`adr/0009`** — parking is modelled supply | Written from research. Its `adr/0006`-class occupancy leak is already named and needs the invariant specified with it | milestone 8 | **yes** |
| **I** | **`adr/0012`** — routing intent lives in the agent | Written from research, and already owes an amendment: the route cache's **eviction policy** and its **key** | milestone 5c | **after S2 R6** — the two caches are R6's subject, and R3 promoted R6 to load-bearing |
| **J** | **`05 §7` format half**, plus **map size** and **Outside Connection layout** | The three things `06`'s open-decisions table still has blocking save/load, narrowed from the map question that `adr/0020`–`0022` otherwise closed | milestone 10 | **yes** |
| **K2** | **`06`'s Phase 2 ordering** | The ordering only. **K1 is done** — see *Done* — so what remains is re-deriving the sequence against conserved Money, Hinterlands, Office, the labour system, transit and every Service, and placing the **seventeen mechanisms `06` now lists as having no milestone** | Planning Phase 2 at all | **last** — A–J move what it sequences |
| **M** | **The route cache's invalidation contract** — what a cached route is allowed to be wrong about | **Forced by measurement, and R5.5 has since supplied the numbers it was to argue without.** S2 R5.4 found no Epoch rung both affordable and correct across the core verb. **R5.5.4 measured option C and it works**: a TTL rotation at **0.40 forced refreshes per Tick** takes the wrongly-valid count **38 → 0 within one rotation while retaining 97.08%** of the cache, against a control that plateaus at **23** and never moves again — which is R5.4's *does not heal* measured rather than argued. **That makes option B a design position rather than a defect**: `BOUNDED KNOWLEDGE` permits not knowing about a new road **if the ignorance is modelled with a stated learning rate**, and a rotation period is exactly that. **So M is no longer choosing between five mechanisms — it is answering one question**: is modelled driver ignorance what this city wants, and at what rate? **And R5.5 adds a second half it did not have**: the path source is not one choice, because a maintained table and a cache are wrong in **different currencies** — structural and visible (16.58% uniform, **149.73%** local) against temporal and, unrotated, permanent. No benchmark ranks those; `05 §4` does. **Also still open: whether the contract is one contract**, since `05 §3`'s Parking Shed is a *neighbourhood* rather than a *path*. **And R6.3 has added a question M should answer *before* this one**, because it is the only routing question with a measured overshoot behind it: **what does a diverting Traveller do about its route?** Re-search costs **861.87% of the Tick budget** at R8's own rung and 2,387.73% at the top of S0a's in-flight band; the cache would need an **88.5%** hit rate it has no claim to; and the third option — **rejoin the Habit Route without re-searching** — is free by construction and appears nowhere in the corpus | **R6**, and `adr/0012`'s owed amendment | **yes** |
| **L** | **A presentation design** | **It does not exist.** Every other phase is backed by a design document; rendering has none, and `05 §2`'s sim/render boundary is on the never-argued list while `adr/0002` was re-argued to serve *inspection*. **Write it first, then grill it** — unlike A–K this is not a session against an existing document | Phase 3, and planning it at all | **yes**, but blocked on S1 and S3 |

**Not arguable, and it is worth being explicit about why.** The **Microscopic Cap**'s value needs a
built traffic model; S2 R2 only informs it. **S2** itself is measurement — argument cannot close it,
which is exactly why it sits at the top of the code-adjacent order rather than in this table.

**Cheap, and due before slice 7 rather than during it:** a **TOML parser library is unnamed**, and
`adr/0003` requires any core dependency be argued against it explicitly. A determinism liability
entering the core needs a written exception. `0003` calls this argument cheap and says it should not
happen mid-slice.

### What must *not* be grilled yet

`0002` names these as playtest questions wearing design-question clothing, and the argument track
should not drift into them: health (#26), recreation (#27), Service variants (#28), car ownership
(#3), private capital (#7), and `01-player §1/§3/§4`. The governability problem especially —
*268 km² of individually-placed service Buildings* — **is not answerable by argument.** Somebody has
to try placing them.

### Audit these for the shape `adr/0043` names

**Every ADR and every design section is in scope, not only the ungrilled ones.** `adr/0043` requires a
claim to be typed *arguable* or *measurable*, and nothing in the corpus has ever been typed — including
the documents `0002` marks 🟢. Two of the five claims S2 measured false sat in green rows. The board
already names the likeliest remaining suspects: **`adr/0016`** carries the order-of-magnitude claim the
whole Microscopic tier rests on, and **`adr/0009`** and **`adr/0008`** are the same shape. Each reads
as decided and none of them has a number.

**`0002`'s blanket rows are part of the defect.** `adr/0010`–`0022` is thirteen ADRs under one green
mark from two sittings, two of which are now known false. A status whose granularity is coarser than
the claims it covers cannot be checked; split those rows as each ADR is revisited.

### Audit these for the shape `adr/0003`'s debt had

`0002` recorded a finding worth acting on before booking any of A–K: `adr/0003`'s owed validation sat
undischarged because **two separate debts had been filed as one**, and the runnable half was parked
behind a grilling session it did not actually need. Its own instruction — *worth auditing the other
🔴-blocked debts for the same shape* — has not been carried out. Doing it first is cheap and may
move work out of this table and into the code track.

**There are now two data points, not one.** Session nine found `06` to be the same shape by accident:
K was scheduled last because *"A–J move what it sequences"*, and that argument binds only the
**ordering**. Correcting claims that settled decisions falsify — K1 — depended on nothing, and ran in
one sitting. **The tell in both cases is a gate whose stated reason covers only part of what it
blocks.** The audit is still owed and now has a diagnostic to apply: for each 🔴 row, ask what the
gate's reason *does not* cover, and check whether that remainder is runnable today.

---

## Done

### Phase 0 / Phase 1 slices

Slice status is [`0003`](0003-build-plan.md)'s ledger, which is the source. This list says what each
slice *was for*, and keeps a finding only where it changed something **outside** that slice.

- [x] **0 — scaffolding.** Four projects, build config, the three reflection guards, CI
- [x] **1 — S4, the kernel benchmark.** No tripwire row fired → [`spike-results`](../docs/spike-results.md)
  - [ ] *task 11 — delete `spikes/S4.Kernels/`*, held pending an optional XMP re-sweep
- [x] **2 — the arithmetic substrate** → [`0005`](0005-arithmetic-substrate.md). Produced `adr/0038`
      and an amendment to `adr/0003`
- [x] **3 — the analysers** → [`0006`](0006-analysers-and-lints.md). Twelve diagnostics covering CI lints 2, 3
      and 7. Produced `adr/0036`'s rule-7 exception axis
- [x] **4 — typed tables and the field declaration** → [`0007`](0007-typed-tables.md). Produced
      `BOR0901` and the project's first State Hash
- [x] **5 — the Tick, the Input Log and replay** → [`0008`](0008-tick-and-replay.md). Eight tasks,
      less task 7's trend assertion, which was deliberately not written (*Owed*). Produced
      **`Borough.Formats`, the fifth project** (`adr/0039`). Its costing of the invariant tiers is
      worth three orders of magnitude and is quoted by `adr/0033`
- [x] **6 — Map Layers** → [`0009`](0009-map-layers.md). Ten tasks, and the first thing this project
      has shown that is not a number. Produced **`adr/0044`**, the **sixth claim in the corpus
      measured false and the first outside S2** — which then got its own second half wrong by argument
      and withdrew it, leaving the finding that outlived the slice: **citing an ADR is not applying
      it**, and the difference is whether the test it states was run against the case
- [x] **7 — the Rule engine** → [`0011`](0011-rule-engine-bins-and-rules.md). Tasks 1–9 of 10.
      Produced **`adr/0049`** and **`adr/0050`**. Two findings reached past the slice: `02 §4.3`'s
      worked example **destroyed money** for six slices, because the transcription into the loader's
      own fixture had dropped the money line — and the same shape recurred in task 8 as a green
      **count**, `RulesOf(1) == 4`, hiding every chain link being armed as an independent Rule
      Instance. Both are a green suite agreeing with the code instead of with the document
  - [ ] *task 10a* — **the wiring: the first Ruleset to reach a `World`.** Not a gate, just work, and
        the last task in the slice. Also the first thing slice 8 needs, since hot reload has nothing
        to swap if no Ruleset is ever in force. It carries the **flow** half of slice 5 task 7's
        trend assertion — `evaluations` flat against `due`
  - [ ] ~~*task 10b* — the proving chain~~ **re-filed to Phase 2**, with decision owed 4. Blocked on
        `pool`, which is blocked on roads, Districts and connectivity
- [x] **S0a — the world at target size** → [`spike-results`](../docs/spike-results.md). Closed the
      Phase 1 gate's sizing question at 86 MiB for 1M rows, and found what it was not looking for:
      **run mode had never had a city in it**, so every Tick figure the corpus held had been taken
      over an empty world. It also **split S0 in half** — S0b is not runnable and carries `06`'s risk

### Planning and design

Sessions and the decisions they produced. The reasoning lives in [`0002`](0002-open-questions.md) and
the decision in its ADR; kept here is what each session **changed outside itself**.

- [x] **Session A — `adr/0015`, hot reload.** Produced
      **[`adr/0048`](../docs/adr/0048-the-ruleset-is-validated-where-it-is-parsed-and-only-integers-and-strings-cross-into-the-core.md)**
      and **cleared two slice gates rather than one**, since 7 and 8 were the same ADR. Named
      **Tomlyn**, put the validator in `Borough.Formats`, and found `adr/0003`'s owed exception
      **was never owed** — it governs *core* dependencies and there is no core dependency. Two
      sentences were caught arguing against themselves: `02 §4.3`'s *"requires no parser"*, which hid
      the dependency for six slices, and `CONTEXT` → Input Log's reason for carrying hashes, which
      was the news it said it was not carrying
- [x] **Session B — `02 §4` residue.** Produced **`adr/0045`** — a fallback chain is a source ladder
      over one Bin. Struck `mean_workforce_experience`, **inverted `02 §4.1`'s Readout bound** so the
      set is declared simulation-side, and settled apply count. Its cycle-checking half moved to **A**,
      which is what put A on slice 7's gate. Found the corpus's **own worked example polled for ever**
      — `adr/0033`'s defect reproduced by the subscription model's example. A draft published a depth
      cap of 5 and **withdrew it**: R3's tripwire rule was written down and had not been run
- [x] **Session nine — `06-roadmap.md`, and what a planning document may assert.** Produced
      **`adr/0042`** — a planning document cites, a design document owns. `06` lost its contents
      column; it gained a table of **seventeen mechanisms with no milestone** and a list of
      **instructions ADRs addressed to it that nobody executed**. Taken out of the board's order
      legitimately, which is the diagnostic now in *Audit these*: **a gate whose stated reason covers
      only part of what it blocks**. `06` is since the control case in
      [`0012`](0012-corpus-audit.md) — the only large document that came back clean, because it
      stores no status
- [x] **`adr/0043` — a claim a measurement could settle must not be settled by argument.** The test is
      *can you name the refuting number and the machine?* **Two of the five claims measured false sat
      in 🟢 rows of `0002`**, so the audit it implies is over every document rather than the ungrilled
      ones — its own first draft claimed otherwise and was corrected before registering
- [x] **S2 planned** — [`0010`](0010-s2-routing.md), gate cleared by defining **Segment** in
      `CONTEXT.md`; then **grilled before any code**, thirteen findings
- [x] **`adr/0040`** — the pathfinding cluster is a multiple of the Chunk, not the Chunk
- [x] **`adr/0041`** — volume is attributed by the Traveller, not the District pair
- [x] **`adr/0046`** — Habit, Sight and Temperament: `adr/0017`'s satisficing rule reaching the one
      actor class nobody had applied it to. Sets no parameter; R8 measured all three and they survive
- [x] **`adr/0047`** — routing never keys on the District

## Unblocked, in order

**Slice status is [`0003`](0003-build-plan.md)'s ledger; session status is the table above.** What
this section adds is the order *across* tracks, which neither of those owns. The three do not
contend — the code track is somebody at a keyboard, the argument track is a sitting, the spike track
is a machine running unattended — but **the code track leads**.

- **Code** — slice 7 task **10**, the last in the slice; then slice 8, or slice 9 once session **C**
  has run.
  **S0b** is blocked on slices 7, 9 and 10 and is the half carrying `06`'s stated risk. Do not read
  S0a as having discharged it
- **Argument** — **C** is the only session gating a slice. The rest run when something concrete is
  blocked on them, never because they are available
- **Spike** — **R7**, the report and the verdict. **R5.6 is done and R6's ungated halves are done**,
  so the only measurement left in S2 is R7's owed **re-capture of R0/R1/R3/R4** under root and
  `performance` — every R6 and R5.6 capture is `powersave` too. **S2 cannot fully close**: R6's
  invalidation half is gated on session **M**, which is *arguable* and no measurement can settle

### Parallel track — S2, routing ([`0010`](0010-s2-routing.md))

Numbers and the decision each produced are in [`spike-results`](../docs/spike-results.md) → *S2*. The
three results that moved things outside the spike are in *Where the project is*; this is the run order
and what is left.

- [x] **R0** — the synthetic Road Graph, the density curve and the `(Segment, offset)` denominator.
      **`Chebyshev` is the heuristic**, and **admissibility breaks at the first Arterial**
- [x] **R1** — the travel-time matrix. **It carries the choice loop** at 1.14 ns, so `02 §5.8`'s
      *never resolve a route inside the choice loop* is enforceable. Left `adr/0020` owed an
      amendment, and found **`02 §6`'s dirty-region rebuild unsound**
- [x] **R2** — searched against looked-up path. **Revived the task it was meant to retire**: a
      next-hop table supplies `adr/0041`'s *next Segment* while storing no path
- [x] **R3** — HPA\*, and the cluster it owns. **Weakened its own standing** and measured
      `adr/0014`'s *"the Chunk grid is already the cluster"* **false by 256× in area**
- [x] **R4** — distance-vector. **Out on cost**, 2.13× the rebuild it exists to avoid, beaten by a
      scheme the plan never named. Also found the O-D draw problem
- [x] **R5.1–R5.5** — the edit storm. **A single-counter Epoch *is* a global flush**, no Epoch rung is
      both affordable and correct across the core verb, and a **TTL rotation** removes the choice —
      0.40 forced refreshes per Tick clears the wrongly-valid count 38 → 0 while retaining 97.08%.
      Harness at `spikes/S2.Routing/Storm/`, four independent captures
- [x] **R8** — the congestion loop and `adr/0046`'s three layers, all seven sections. **All three
      layers survive**: `03 §3.4`'s self-correction closes on the local layers alone, the Sight
      Horizon's floor is **1 Segment**, and Temperament damps by 92.28% where a herd exists. **Static
      Habit holds, so there is no refresh cadence to argue about**
- [x] **R5.6 — the Parking Shed. DONE, and it disagrees with routes.** **Per-Segment witnessed by
      *paths* is the only rung that fits** — worst case **26.10% of a Tick** against per-Segment
      (ball) 265.45%, per-cluster **1,351.24%** and global **1,638.20%**. The global tripwire fired
      as written: one deleted Segment invalidates all **159,825** sheds at 255.560 ms, and
      `adr/0009` pays it **on arrival**, so it is a stampede rather than a stall. **`plans/0010`'s
      own prediction is measured false** — it argued per-cluster *"fits it far better than it fits
      routes"* and per-cluster is the **worst survivor**, 127× the paths rung on a one-Segment drag,
      because a cluster is a tile of map holding thousands of sheds and not the shed's
      neighbourhood. **The Arterial gesture makes the error visible**: deleting motorway invalidates
      **zero** sheds correctly and **3,669** under per-cluster, so 100% of that rung's work is
      wrong. **The two consumers do not want the same mechanism** — routes needed a *temporal* answer
      (R5.5.4's TTL rotation) because no structural rung was both affordable and correct; sheds need
      no rotation at all. That is what `adr/0012`'s owed amendment now has to carry.
      **`CONTEXT.md` → Epoch is unblocked**, and `05 §3`'s *invalidated by the Road Graph Epoch* is
      owed the correction `CONTEXT.md` already took
- [ ] **R6 — the two caches. STARTED; R6.0 is done.** Promoted to load-bearing, and now **the only
      exit**: `adr/0047` removed the option nobody had noticed was in reserve. Owns the cache's **key
      granularity** and its **eviction policy**, which R5 measured as the bigger lever below the
      highest edit rates. **The gate on session M is narrower than this list read it as** — `adr/0047`
      closed M's path-source half by name, and the surviving half is the *invalidation contract*,
      which neither the key nor eviction is downstream of. `0000`'s own *gate whose stated reason
      covers only part of what it blocks*, third instance
  - [x] **R6.0 — the pristine-seeding repair.** R5.5's defect, and it was **eight call sites rather
        than the four filed**: the same-Segment and adjacent-Segment bypasses return *routable*
        directly from `Run` without entering a confined search, and R3.8 puts the bypass at **78.28%**
        of Legs inside one block. Repaired, `Unroutable` stops being *"zero by construction"* and
        becomes the section's sharpest staleness instrument — **82.93% → 99.03%** of the control's
        severed lookups, **monotone in the refresh rate**, which **corroborates R5.5.4 through a
        different column** and is evidence session **M** did not have. The re-baseline is bounded by
        checksum: **only R5.3 and R5.5.2 moved**, R5.3's conclusions all survive, and **nothing M
        leans on changed**. Also found *R5.5's 16-against-416 had no denominator* (four rungs against
        one) and *the machine-state block asserted a pinning it never measured*
  - [x] **R6.1a — what a coarser key costs.** The measurement R5.5.2 named and declined to make.
        **The error is bounded by Segment geometry, not by the trip distribution** — `node-a`'s mean is
        **0.86–0.94 Ticks flat across the whole O-D family** while its *percentage* swings 1.84% →
        9.70%, so **the absolute is the number to carry and the percentage should not be quoted**.
        R4.1's finding one layer down, except that here the rung-invariant number exists.
        **`node-a` costs exactly 2× `nearest-node`** on every rung, geometrically, and the fix is one
        comparison at insert with the key space unchanged — `adr/0012`'s amendment can state it in a
        sentence. **A nodes² key is essentially free if the endpoint is chosen well** (`best-endpoint`
        reads 0.00% mean), so the error is an artefact of choosing badly rather than intrinsic. But
        **the greedy choice is not monotone**: it halves the mean and makes the tail worse
        (128.20% against 106.66% on decay L=1024)
  - [x] **R6.1b — the key space.** Two invented axes built and swept, and **the collapse column reads
        1.00× on every row of both**. A node key collapses two Trips only if they share a Segment at
        **both** ends, and 33,018 Segments is **1.09 × 10⁹ ordered pairs** — no pool S2 can draw is
        dense in that. **So `plans/0010`'s five-Buildings argument for the coarse key is unconfirmed,
        and may not be cited**; settling it needs Trip generation (`06` 5b). The asymmetry is the
        useful part: **R6.1a settles the price exactly and R6.1b cannot settle the benefit.** Two
        by-products: R5.3's **28–31% miss floor reproduces from outside R5**, and **`RouteCache.Slot`
        degrades on structured keys** — `access-point` falls 71.9% → 15.9% with distinct keys
        unchanged at 511–512, so it is the hash and not capacity. That is R6.2's, arriving early
  - [x] **R6.2 — the eviction policy.** Reports **blame** rather than a rate — cold / capacity /
        conflict — which is what makes it robust while the absolute hit rate is unmeasurable.
        **R5.3's 28–31% miss floor is 20.0% conflict and 0.0% capacity at its own rung**: not a
        property of cache size, and every one a lookup a perfect cache of the same size would have
        served. **Associativity is the lever** — 20.0% → 10.6% → 3.8% → 1.4% at 1/2/4/8 ways against a
        0.0% bound — and **4-way LRU is the recommendation**, `adr/0017`'s pattern sized for the first
        time. **The index function was predicted to be the lever and is not**, on random keys; it is a
        *robustness* fix that matters only on structured ones (31.2% → 21.7% on eight destination
        sites). **Load is the axis R5.3 never swept and it dominates**: conflict at four ways triples
        from 0.25× to 1.00× load
  - [x] **R6.3 — the two consumers, multiplied.** Not on the plan; run because the two halves of
        routing's bill had never been added up. **`adr/0046` made diversion routine and `adr/0047`
        deleted the one path source that served one cheaply.** At R8's own rung — 40,000 Travellers,
        a 7-Day Habit — Habit formations cost **0.316 ms** and diversions **134.135 ms**: **99.76% of
        routing's bill and 861.87% of the Tick budget.** **Between 32 and 147 diversions per Tick fit
        and R8.3 measured 1,269.51.** **The in-flight rungs are S0a's own band for a 1,000,000
        city**, so this is target scale and not an extrapolation — 795.91% at the floor, 2,387.73% at
        the ceiling. The cache cannot rescue it — it would need **88.5%** at 40,000 and **95.9%** at
        111,000, on R6.1b's worst input. **R3's *85
        Trip starts* is not the binding constraint and never was**, because a Trip start under static
        Habit is a lookup; `plans/0013`'s routing row counts the same wrong event, filed in
        [`0012`](0012-corpus-audit.md). Three levers, one of them unproposed: **let a diversion rejoin
        the Habit Route without re-searching.** Session **M**'s, and now the first thing M answers
- [ ] **R7 — the report, the verdict, and deleting the harness.** Owes a **re-capture of R0, R1, R3
      and R4**, all of which carry the one-processor artefact

### Parallel track — Godot (Track B, no gate)

**These two have a job again.** `06` framed them as gating a commitment to Godot; `adr/0036` took the
core's language out of `adr/0001` and session eight confirmed the host argument, so there is no
decision left for them to gate. They are the **empirical inputs to session L** — a rendering ceiling
and a UI-cost figure — and L is what unblocks Phase 3. Their specifications in `06` were stale by
roughly an order of magnitude and have been struck; size them from `spike-results` and the 1M target.

- [ ] **S1** — chunked `MultiMeshInstance3D` at city scale. *Feeds L*
- [ ] **S3** — one data panel with a live multi-series graph. *Feeds L, and it is **the spike most
      likely to be skipped and most likely to change the decision***

---

## Owed — documentation debt, none of it blocking

Small, and each one is a place the corpus currently says something known to be wrong.

**The corpus-wide sweep's debts are [`0012`](0012-corpus-audit.md), not here.** This section is what
the spike and slice work left behind as it ran; `0012` is the one-off audit of every status-bearing
document against the code. Keep them separate — a debt in two ledgers is the defect `0012` exists to
diagnose.

- [ ] **S0a's capture is `powersave`, not `performance`** — setting the governor needs root and the
      session did not have it. **Every absolute in that section is an upper bound** and every ratio is
      unaffected, since ratios are taken within one machine state. The one verdict leaning on an
      absolute — the State Hash at 2.08 Tick budgets — would need the machine **2.08× faster** to move.
      The re-capture is cheap and should ride along with R7's, which owes the same thing for R0/R1/R3/R4
- [ ] **`05 §9` is owed the State Hash's cost**, on evidence from S0a. Item 1b records the full-world
      double buffer being deleted at *"8–15 ms at 1M — 50–100% of the budget"*; **one State Hash is
      32.47 ms, 2–4× worse**, and the performance strategy does not mention it at all. It is *sampled*
      rather than per-Tick so nothing is broken — what is missing is the statement that **a per-Tick
      hash is not available at the target**, which every golden-baseline and bisection workflow is
      downstream of
- [ ] **The synthetic fixture and `World`'s sizing derivation disagree, and nothing checks that they
      agree.** `World` allocates 225 Lots and 150 Buildings per 1,000 Citizens; `SyntheticCity` builds
      **120** of each, so both are over-provisioned — while **Households land at exactly capacity**,
      leaving zero headroom, so the first Household the simulation itself creates reallocates the
      table. Slice 7's, because the right ratio is a design question and a fixture is not where it
      gets settled
- [ ] **`plans/0002 §1840`'s S0 specification** — it names four clauses as one spike. Now split into
      S0a and S0b across `0003` and this board; the ledger entry itself still reads as one item
- [ ] **`03 §3.3`, `§3.4`, `§3.6` — joint rewrite**, owed by `adr/0041` and now carrying R2's
      evidence. The District-pair counter goes; the circularity argument becomes structural;
      **force-promotion must stand on its own second argument or go** — and R2 removed the last
      support for the first: `§3.3` confessed a *lag* and compensated for it, but the defect is that
      the smear reports the jam **in the wrong place**, which no cadence and no second trigger fixes
- [ ] **`adr/0012` amendment** — the route cache's **eviction policy** *and* its **key** (`adr/0012`'s
      *"keyed by origin-destination pair"* is ambiguous between nodes² and Buildings²)
- [ ] **`adr/0041` amendment** — owed by S2 R2, on evidence. *"Searched per Trip or shared per
      origin-destination pair is a performance axis with **no correctness content**"* is wrong on two
      counts: a shared route costs **36.01%** mean detour and a next-hop table **18.52%**, and *every*
      Trip into a District arrives through its **one representative node**, whose Stress is then an
      artefact of the partition. **The ADR's substantive claim survives untouched** — experience and
      contribution stay the same list of Segments under every rung — so this amends a sentence, not a
      decision. Its **revisit trigger is also discharged**: the crossing rate is 0.79–0.83, not the
      assumed 1.0, and the crossover sits at 105 Ticks rather than ~10
- [ ] **`adr/0020` amendment** — owed by S2 R1, on evidence. *"A connected component of the District
      graph… a union-find"* is not what `CONTEXT.md` → Settlement defines, and the two disagree about
      the city where the city is fragmenting. Tarjan is still cheap; it is simply not the ADR's claim
- [ ] **`02 §6` correction** — owed by S2 R1. *Slow cadence, dirty regions only* is **unsound**: a
      spatial test misses the long routes that cross an edit without ending near it — 309 of 429
      changed entries on a central edit. It is `CONTEXT.md` → Epoch's *when you pay* / *what survives*
      distinction arriving at the matrix instead of at the cache
- [ ] **"Zone" is used for the travel-time matrix's granularity, which is the District.** `CONTEXT.md`
      → Zone is *a permission set over land*; `CONTEXT.md` → District is *"the granularity of the
      travel-time matrix"*. `05 §422` and `references.md §2` both say *"zone-to-zone travel-time
      matrix"*, and `plans/0010` quoted the second verbatim — so this is a corpus-wide sweep and not a
      one-line fix, and a corrected quote is a broken one. Found by S2 R1, which spells it District
- [ ] **`spike-results`** — the 37k–111k in-flight band conflates duration sensitivity with peaking and
      must be re-derived on both axes
- [x] ~~**S2's timing tables are owed a canonical re-capture**~~ **DISCHARGED session eleven.**
      `sudo spikes/S2.Routing/tools/routing-run.sh` took R0 and R1 together under `performance`, turbo
      enabled, pinned to one physical core; `docs/spike-results.md` now quotes that capture throughout
      and the `powersave` run is retained beside it. **Captured twice, fourteen minutes apart, under
      the identical configuration** — so the nanosecond columns now carry a measured error bar rather
      than a disclaimer: drive-search absolutes reproduce within 2%, and the one DRAM-resident read
      within 12% — the exposure S4 already named for this machine — while a bootstrap recovered by
      difference between two loops reaches 29%. **Every count is bit-identical
      across all three captures**, which is the determinism check nobody had run. The tripwire column
      reads **0.36×** against a wire at 1.00×
- [x] ~~**Why plain Dijkstra's absolute moved 1.64× under pinning**~~ **ANSWERED by S2 R5's canonical
      capture, and the hypothesis was right.** Driving `None` went 779,150 ns unpinned → 1,278,071
      pinned, reproducing to 0.2%, while `Chebyshev` moved 0.04% across the same change. The
      hypothesis on file was *`taskset` leaves one visible logical processor and tiered-JIT
      background compilation now shares the measured core, which lands on whatever is timed first*,
      with the check named as *re-run the ladder in reverse, or with tiering disabled*. **Neither was
      needed**: R5 takes its denominator twice, which makes the artefact visible inside a single
      capture — 4.88× apart at one processor, 0.92× at two — and pinning to both threads of the core
      removes it. **The first-timed row of any S2 table taken before this fix is still the least
      trustworthy number in it**, which is now a re-capture task rather than a caveat. It already
      cost one claim — R0's *"`EuclideanFloor` is not faster than Dijkstra at all"* was true of the
      unpinned capture and stays struck
- [ ] **`05`** — strike the ~400k Trips/Day figure, known wrong and still standing in the authoritative
      document
- [ ] **`05 §3`** — Parking Shed invalidation needs the *when you pay / what survives* correction
      `CONTEXT.md` → Epoch has taken
- [x] ~~**`06`** — the S2 specification (*"30k Travellers"*) and S1's (*"20k Buildings"*) are stale~~
      **DISCHARGED session nine** by deletion rather than correction, per `adr/0042`: `06` no longer
      carries spike specifications at all. `0003` and `spike-results` own them
- [ ] **`adr/0012`, and two other filenames, use "Agent"** — banned outright by `CONTEXT.md`. 33
      occurrences across 22 files

## Owed — findings that change a later task

- [x] **R3 must not quote HPA\* in expansions saved.** *Discharged, and the warning was load-bearing:*
      the hierarchy expands **4.7× fewer** nodes and is **1.44×** faster unreduced, because a road
      network is degree-3 and the complete abstraction is degree-40. Quoted in expansions it would
      have read as a large win. **R6 still inherits the instruction.** Original wording: R0 measured a case where the currency does not
      convert: `EuclideanFloor` expands **11% fewer** nodes than `Chebyshev` and takes **1.8× as
      long**, and against plain Dijkstra it cuts expansions by 55% while being no faster at all. The
      cost is its exact integer square root, run twice per node pushed. `plans/0010`'s ladder specified
      nodes expanded, path cost and optimality; **adding a clock is R0's amendment to the plan**, and
      R3 and R6 inherit it — a hierarchy or a cache that saves expansions has not yet saved anything
- [ ] **An artefact that varies with the swept axis is not distinguishable from a result.** R1 needed
      **four** warm-up schemes before its cold-build column stopped falling smoothly with District
      count — which is precisely the shape a reader hopes a sweep will discover, and was the process
      leaving tier 0. Ruling out the per-rung explanations is what identified it as per-process:
      `OneToAll.Run` is called once per District, so the small rungs never call it enough. **R3 and R5
      sweep cluster size and edit rate and are exposed to the same failure**; only a warm pass over the
      whole sweep removes it. This is R0's *"the bootstrap column was mostly the sampler"* in its
      general form, and it is the second time in S2
- [ ] **A sample that shrinks with the swept axis manufactures a trend out of survivorship.** R1's
      entry-error section first drew Access Points uniformly and rejected those outside the named
      District; at 1,024 Districts a hit is one draw in a thousand, so the sample silently collapsed to
      **nine searches** and was printed beside rows built from 2,244. Third instance of the corpus's
      recurring shape, after R0.5's *mean cost when found* and R0's dead Arterials. **Any later section
      that samples inside a swept partition must report its sample size per rung**
- [ ] **An invariant is worth printing on the run where it reads *yes*, because it is worthless on
      the run where nobody printed it.** R2's next-hop rung tested arrival *after* entering the last
      arc, so an arriving Traveller was respawned without decrementing — `adr/0041`'s named
      `adr/0006`-class defect, *"a road that looks busy forever."* It published a peak `v/c` of
      **883×** while the footprint, the crossing rate, the detour and the crossover columns all looked
      healthy. The ADR had already specified the invariant that catches it — *summed Segment volume
      equals in-flight Travellers, every Tick* — and it found the bug on the first run it was printed.
      **Fourth instance in this spike of R0's *"an argument for reporting a quantity you expect to be
      boring"*, and the first where the corpus had written the check down in advance and the harness
      had simply not run it.** R3, R5 and R6 all mutate state a conservation law covers
- [ ] **Two measurements that agree to the last digit are not two measurements.** R2's shared and
      next-hop rungs reported **byte-identical** peaks, because the next-hop fleet was being spawned
      *at* the origin District's representative — which made it walk the shared route. The rung's whole
      claim is that it is followed from wherever the Traveller actually is, and the experiment had
      quietly removed the difference it existed to measure. **Nothing but the identical digits gave it
      away.** R3 compares a hierarchy against a flat search over the same graph and is exposed to the
      same class
- [ ] **An error rate that moves with an unrelated optimisation is not evidence.** R0's heuristic
      multiplies by a floored reciprocal rather than dividing, to remove four hardware divisions per
      node. The reciprocal's ~2-in-10,000 slack **partially cancels an overestimating metric's error**:
      the same change moved walking `Manhattan` from 35 of 300 non-optimal to 4 of 300, worst exactly
      where `adr/0008`'s walk Legs live. **Any later measurement of an error rate — R2b's attribution
      lag, R5's hit rate — should ask what else in the pipeline rounds in the same direction**
- [ ] **A denominator measured once has no error bar, and a denominator measured first has a
      systematic one.** R3's first pinned capture read **1,401,307 ns** for the flat search and
      **477,609 ns** for the same code measured after the sweep — a 193% spread — because the flat
      loop was the first timed thing in the process and the clock had not ramped. **Every ratio R3
      publishes divides by that number**, so the artefact would have decorated the whole task rather
      than one column. The harness now measures it twice and publishes both. **R4, R5 and R6 all
      divide by the same denominator** and must do the same. Fifth instance in S2 of R0's *"an
      argument for reporting a quantity you expect to be boring"*, and the first where the boring
      quantity was the denominator
- [ ] **A correctness column that cannot move is not evidence that the error is absent.** R3's detour
      read 0.00% at every cluster rung, which is the shape R2's byte-identical peaks wore. It is real
      — the abstraction is complete, so it cannot lose a route — but that was established by making
      the instrument move: **sampling transitions drove the same column to 80.49%**. A zero should be
      paired with a rung that is expected to be non-zero, or it is indistinguishable from an
      instrument that is not wired up
- [ ] **Nothing in the corpus invalidates a route when congestion changes, and R3 is the first task
      with a large number attached to that.** The Epoch bumps on an *edit*; the VDF makes travel time
      a function of `volume / capacity`; `adr/0041` moves volume **every Tick**. A flat search reads
      arc costs at query time and is always current — a structural advantage of the denominator R3
      never priced — while **every precomputed structure in S2 is stale the Tick after it is built**:
      HPA\*'s intra-cluster edges, R2's next-hop table, and R1's matrix alike. **Third invalidation
      mechanism in the corpus and the first with none at all**, after R1.7's dirty region and the
      scalar Epoch. **R5 must be given the refresh cadence of the routing cost basis before its edit
      storm means anything**; if that cadence is the time-of-day phase R1.8 found, the exposure
      evaporates, and if it is per-Tick both surviving routers are dead against a 15.6 ms budget
- [ ] **A tripwire should be gathered as direct data, and where it cannot be, the derivation should
      be inverted until what is published is measured.** R3's Tick-budget row was first drafted as
      *routing is 6.4× over budget*, which multiplies a measured per-route cost by **550 Trip starts
      per Tick** — a figure resting on a mean Trip duration the corpus calls provisional, in a spike
      with no Travellers and no Trip generation to improve it. **A wire whose denominator is a guess
      fires on the guess.** Published the other way round — *routing fits while fewer than 85 Trips
      start per Tick* — the quantity is a measured cost over a world constant and survives the arrival
      rate being measured elsewhere. **R4, R5 and R6 each have a row that can be inverted the same
      way**, and `plans/0010`'s tripwire section now carries the rule
- [x] ~~**S2's O-D draw is uniform over the map, and R0 flagged that as a placeholder that was never
      replaced.**~~ **DISCHARGED as a debt and reopened as an axis, by R4.1.** The draw is now a
      **swept family** — uniform, distance-decay at L = 1024/512/256 Tiles, monocentric — with
      uniform as a rung of the same sampler, so a difference between rows is the shape and cannot be
      the machinery. It cost a conclusion to find: R2's **18.52%** detour is 20.14% on uniform and
      **128.82%** on the tightest decay rung, because uniform is the **longest-trip** distribution
      available (8.53 km mean on a 16.4 km map) and a District-representative detour is a fixed error
      against a shrinking journey. **R3's speedups remain an upper bound and are now quantifiably
      so.** What is *not* discharged is the underlying absence: the family is invented, and only Trip
      generation can replace it — filed as `plans/0010` decision 14, and **no document may cite an
      S2 figure derived from it without naming the rung**. Original wording:
      **S2's O-D draw is uniform over the map, and R0 flagged that as a placeholder that was never
      replaced.** R0 said it could not have the distribution it was supposed to use and would take
      R1's; R1 produced none, and R3 inherited the uniform draw unchanged. **A uniform draw over 4,096
      Tiles produces long routes, and long routes are where a hierarchy wins widest** — so R3's
      speedups are an upper bound. It does not move the optimality counts, which are counts, and it
      does not move the ranking of the cluster rungs against each other. **R4 inherits the same draw**
      and a next-hop table's error profile is distance-dependent too, so the comparison is on the same
      footing but both sides are measured on a distribution nobody has confirmed. **R3.8's bypass
      table is the only evidence in the spike about the short end**, and it says the cliff is at one
      block
- [ ] **Two S2 tasks publish different absolutes for the same operation.** R2 reports 474.47 ms to
      build the 121-column next-hop table; R4 reports **234.74 ms** for the identical rebuild. R4
      substantially explains it rather than resolving it: **the same 121 backward Dijkstras read
      423.47 ms measured first in R4's own process and 234.74 ms measured later** — 1.80× apart,
      which is **R3's *a denominator measured first has a systematic error* reproducing a third
      time**, and R2's figure was also first-timed. Every R4 ratio is taken in-process against R4's
      own figure, so no conclusion moves. **R7 owes the reconciliation**
- [x] ~~**The canonical `performance` capture of R4 is owed**~~ **DISCHARGED.** Captured as root
      under `performance`, pinned, turbo enabled; 23.69 s, CPU stall 1.41%, memory stall 0.00%.
      `docs/spike-results.md` §S2 R4 now quotes it throughout, and the `powersave` run is retained
      beside it. **Against that run every non-timing row is bit-identical** — relaxations, rounds,
      wrong-entry counts, stranded counts, footprints, detour percentages and the whole O-D table —
      and only nanosecond columns move, by 2–7%. **The governor moved nothing R4 concludes from.**
      The scheme ranking, the sign of every comparison and the break-even band are all unchanged;
      dynamic repair's margin over a rebuild on one deletion widened from 37.99× to **49.76×**
- [ ] **`plans/0010` R3's *current standing favours HPA\* at 16 Chunks* is withdrawn — NEW, produced
      by S2 R5.2**, and it is a correction R3 asked for in advance. R3 narrowed cluster size to 8 or
      16, put the bias on 16 on a 1.31× faster refined query, and said the axis that separates them
      is an edit rate R5 owns. **8 is 1.9× cheaper on a coalesced 256-Segment drag, 5.8× on the naive
      worst case, and its full rebuild is 43.14 ms against 75.31 ms.** A 1.31× query advantage
      against a ~2× edit penalty picks 8, and **the canonical capture confirms it** — the one run
      that said otherwise is the mis-pinned one. **The conditional on R5.6 is discharged**: sheds do
      not rank it differently — **8 beats 16 on every gesture and every size**, by 2.9× at a
      one-Segment drag — so the sweep may go
- [x] ~~**`HpaSearch` cannot see a Segment deleted under a Trip's own feet — NEW, found by S2 R5.5, and
      it is pre-existing.**~~ **DISCHARGED by S2 R6.0, and the defect was twice the size filed** —
      eight call sites, not the two remainders, the extra four being the same-Segment and
      adjacent-Segment bypasses, which return *routable* without entering a confined search at all.
      Repairing it moved `Unroutable` from ~1% of the control to **82.93–99.03%**, monotone in the
      refresh rate, which turned a dead column into an independent confirmation of R5.5.4. The
      re-baseline it was deferred for is **bounded to R5.3 and R5.5.2** and changes no R5 conclusion.
      Numbers in [`spike-results`](../docs/spike-results.md) → *S2 R6.0*. Original wording:
      **`HpaSearch` cannot see a Segment deleted under a Trip's own feet — NEW, found by S2 R5.5, and
      it is pre-existing.** The forward seed and goal remainders call `CostToEndpoint(graph, null, …)`,
      and a **null** cost array reads `graph.ArcCarTicks` — the pristine array — while the storm
      deletes into a shadow clone. **So the hierarchy returns a route down a road the player has just
      bulldozed.** `flat` found **416** unroutable over R5.5's sweep where four cache rungs found
      **16**. It is **common-mode across all three Epoch rungs**, so no R5.3 or R5.4 conclusion moves
      — but ***Unroutable* on any hierarchical row is evidence of nothing**, and **R6 must fix it
      before it caches anything.** Not repaired, because repairing it re-baselines R5.3
- [ ] **A per-edit repair API invites the loop that destroys it — NEW, produced by S2 R5.2**, and it
      is a shape rather than a number. **R5.5 tested it for generality and it does not have any**:
      looping `RepairSubtree` per Segment over a drag costs **0.91–1.51×**, not 23.26×, because a
      shortest-path subtree is *repaired* from the boundary inward where a cluster's edge set must be
      *decided* whole. **The finding is real and local to `AbstractGraph`**, not a corpus-wide rule. `RebuildFor(segment)` is the natural signature and looping it
      over a drag re-decides the same few clusters dozens of times, for **23.26×** the coalesced cost
      and a worst case of **253.22 ms**. The two spellings are **identical at a gesture of one**,
      which is the only size R3 and R4 ever measured, so no earlier task could have caught it.
      **Anything the player can do to hundreds of objects at once needs its API shaped for the
      gesture**, not for the object — and above ~63 clusters touched the repair loses to a **full
      rebuild** outright, so the path has two thresholds rather than none
- [ ] **No Epoch rung is both affordable and correct across the whole core verb — NEW, MEASURED by
      S2 R5.4**, which was not a task the plan had. Deletion is monotone-**worsening**, so a rung
      watching a route's own Segments misses nothing and per-Segment is exact. Addition is
      monotone-**improving**, and **a route computed before a road existed cannot contain it**, so
      per-Segment declares **100.00%** of the cache valid and structurally cannot notice — and
      **per-cluster fails the same way**, since a new fast link in a cluster the route never enters
      still beats it. **Only global is sound under addition, and R5.3 measured global as unusable.**
      Sized rather than argued: restoring **4 Arterial Segments — ~512 m, the smallest addition worth
      drawing** — leaves per-Segment serving stale routes on **9.22%** of resident entries at a mean
      **16.71%** detour and a worst of **62.65%**, against R2's 18.52% which the corpus treats as a
      serious correctness finding. **It is a floor, and unlike every other error in this spike it
      does not heal**: nothing the rung watches will ever move again, so only **eviction** removes
      it — and `adr/0012` keys by O-D **rather than by agent**, so it is every driver's route and a
      hot pair is the *least* likely to be evicted. **Five ways out are tabulated in
      [`spike-results`](../docs/spike-results.md) → R5.4**, and two of them are corpus decisions
      rather than engineering: **B** (weaken the contract to feasibility — which fits `BOUNDED
      KNOWLEDGE` if the ignorance is *modelled* and is a defect if it is *accidental*), and **E**
      (R1's matrix as an O(1) detector — **the relationship R1 explicitly declined to argue**,
      arriving from the other side). **Addition is measurable after all**, which R3 had thought it
      was not: build the abstract graph on the full graph so every portal slot is reserved, then
      delete a set and restore it
- [ ] **The synthetic grid cannot answer the Street half of that question — NEW, produced by S2 R5.4.**
      Restoring ordinary Street improved **0.00%** of cached routes at every size up to 126 Segments,
      because one Street per Cell boundary at a uniform speed gives very many *equal-cost* shortest
      paths: deleting a line leaves an equal-cost alternative one block over, so the cached cost never
      moved. **The zero is real and does not generalise** — a real network has heterogeneous speeds
      and far fewer ties. It is the same debt `CONTEXT.md` → Segment already carries from R0: **road
      density has a curve and no source**, and now so does road *homogeneity*. **The Arterial side is
      thin for the same reason**: the graph holds **8 Arterials and 104 Arterial Segments**, and an
      Arterial-only drag saturates at **4** — which is why R5.4 publishes one Arterial rung rather
      than a sweep, and why its 16.71% is stated as a **floor** rather than a curve
- [ ] **R5's cache hit rates rest on an invented pool, and no document may quote the level — NEW,
      produced by S2 R5.3.** A route cache works because real Trips **repeat**, and nothing in S2 can
      produce that recurrence because it needs Trip generation. R5 substitutes a fixed pool of 512
      O-D pairs sampled with repetition; drawing fresh pairs every Tick would report ~0% for every
      rung and compare nothing. **So every absolute hit rate in R5.3 is a property of the pool
      size.** What the pool cannot distort is the *ratio between rungs under the same pool*, which is
      what the ladder is for. **Exactly the handling R4.1's O-D family already has** — and that debt
      is discharged as an *axis* rather than closed, so this one should be too: **R6 must sweep pool
      reuse rate the way R4.1 swept trip length**, or its hit-rate curve is one guess wearing a
      measurement's clothes
- [ ] **`adr/0017`'s eviction pattern has a number for the first time — NEW, produced by S2 R5.3 and
      not looked for.** R5's miss column sits at **28–31% and does not move with edit rate at all**,
      which is the tell that it is collisions rather than staleness: **a direct-mapped route cache at
      2× over-provisioning loses about three lookups in ten before a single road is touched.** The
      decision belongs to **R6**, which owns eviction and the key; what R5 supplies is evidence that
      the policy is worth more than the Epoch rung below the highest edit rates
- [ ] **A mean per-route cost times an arrival rate does not bound a Tick — NEW, produced by S2 R5.3.**
      R3 published *routing fits while fewer than 85 Trips start per Tick*, derived from a mean. At
      **16** Trip starts R5 already measures a worst Tick of **10.37 ms** against a 15.6 ms budget.
      **S4's K6 said it first** — a run whose worst iteration was 100.2 ms read 2.462 ms at p99.9 —
      and R6 inherits the instruction along with R3's
- [x] ~~**The canonical `performance` capture of R5 is owed**~~ **DISCHARGED**, and taking it found a
      defect in the capture protocol itself. `spike-results` → *S2 R5* quotes the pinned run
      throughout and the unpinned `powersave` capture is retained beside it. **Every count, share and
      percentage is bit-identical across the two**, including R5.4's whole addition table, so no
      conclusion moved. Three absolutes did: the repair loop reads **23.26× and a 253.22 ms worst
      gesture** rather than 21.25× / 219.50 ms, and the worst Tick reads **10.37 ms** rather than
      13.26 ms
- [ ] **Every earlier `performance` capture in S2 carries a one-processor artefact — NEW, produced by
      R5's canonical capture.** `routing-run.sh` pinned with `taskset -c 2`, one logical processor
      with the SMT sibling idle, which starves the .NET tiered JIT's background compilation of
      anywhere to run but the measured core. Measured **within one capture** by the twice-taken
      denominator: **214.94 ms first against 43.99 ms last, 4.88× apart**, against **0.92×** under
      `-c 2,8`. It inflated R5.2's first-timed half by ~3× and **flipped the 8-versus-16 verdict**.
      **R0, R1, R3 and R4 were all captured this way**, session eleven's canonical re-capture
      included. Counts and in-process ratios are unaffected; **first-timed absolutes are not**, and
      R3's 1,401,307 ns / 477,609 ns denominator spread is very likely this rather than a cold clock.
      The harness is fixed — it reads `thread_siblings_list` and pins to both threads. **R7 owes the
      re-capture**, and it is cheap
- [ ] **A published absolute with no artefact behind it is not a measurement — NEW.** Six figures in
      R5's first write-up existed in no file under `results/`, because the harness wrote every run to
      one filename keyed on machine configuration alone: a `--storm` run displaced a whole-run
      capture, and a later `--storm` displaced that. **Fixed** — every capture is now named by
      section, CPU set and capture time and nothing is ever overwritten, and the retained captures
      have been renamed to the same scheme, which is what makes the artefact above visible in an
      `ls`. **The generalisation is owed a home**: this is *an argument for reporting a quantity you
      expect to be boring* arriving at the **retention** layer rather than the measurement one, and
      S4 and every later spike write to the same kind of directory
- [ ] **The canonical `performance` capture of R3 is owed, and it is now owed twice over.** The
      published figures come from a `powersave` capture, and it was pinned the **wrong** way —
      `taskset -c 2`, one logical processor — so it carries the artefact the entry above describes on
      top of the governor. Every *count* is configuration-independent and every ratio is taken within
      one process, so no R3 decision rests on it; **no absolute nanosecond figure should be quoted
      outside the section until it exists.** The harness pins correctly now:
      `sudo spikes/S2.Routing/tools/routing-run.sh --cluster`
- [ ] **Slice 7 ships two counters, and its tripwire is stated over cost rather than over depth.**
      Owed by `adr/0045`. **Rule evaluations per Tick**, and **walked chain depth** — the second
      already required by `02 §9`'s *"which fallback chain it walked and where it terminated"*.
      Published the R3 way: *chain walking fits while fewer than N evaluations occur per Tick*, never
      as a multiple over a guessed denominator. **The instrument is specified before the number is
      needed**, which is the shape task 7's Census owed and did not have. **Churn is the third
      thing to count**: a chain is walked once on entry into shortage, so supplied/short boundary
      crossings are the cost driver — and **greedy apply maximises them**, which makes burstiness a
      bill a designer chose rather than one the engine imposed
- [ ] **A helper is only as safe as the largest quantity anybody has yet asked it to measure.** R4's
      elapsed-time helper computed `elapsed × 1,000,000,000`, which passes `long.MaxValue` at about
      **9.2 seconds** on a nanosecond clock — and R4 measured a rung that took four minutes, which it
      published as **−8,267.51 ms**. The identical expression is correct in every earlier S2 section
      because every earlier section timed loops far below the threshold. **R5, R6 and R7 use the same
      harness**, and any of them may time a storm rather than a loop
- [ ] **A phase that does nothing does it very quickly, and reports success.** R4's DSDV poison phase
      was seeded with the nodes that *detect* a break rather than the nodes they *advertise to* — and
      since a correctly-implemented node rejects stale claims, nothing changed, nothing propagated,
      and the phase returned **converged: yes** after 2 rounds and 24 relaxations while leaving
      16,680 of 16,697 entries wrong. **A convergence flag is not a correctness check**, and the only
      reason this was caught is that a separate audit column existed to disagree with it
- [ ] **A defect that produces a plausible number is worse than one that produces an absurd one.**
      R4's audit counted the destination itself as unreachable, one phantom per column, which
      presented as a suspiciously round **121** — a number a reader could have rationalised. R2's
      883× `v/c` announced itself; this did not
- [ ] **The long-run trend assertion is owed by slice 7, and the instrument for it now exists.**
      *Decided:* task 7 shipped the Census and `series(metric, window)` and deliberately did not ship
      the assertion. Nothing in the world grows or shrinks yet — no Event Wheel, no Rules, no Trips —
      so *no collection trends upward at steady state* would pass against an empty world and a static
      one equally, and an assertion that cannot fail reads as covered. **Switch it on when slice 7
      gives the world churn**: sample on the trace cadence, take a series per metric over the tail of
      a 100k-Tick run, and fail on a positive trend in `slots` with `live` flat. The `--census` report
      prints the numbers today, and printing them is what makes the vacuity checkable rather than
      argued. **Task 9 narrowed where it lands and added a metric it must cover**: the world still has
      no churn — a Ruleset is task 10's — so this is task 10's, not task 9's. But the census now
      carries three **flow** counters beside the levels, and a flow trends differently: a rising
      `evaluations` sum with a flat `due` sum is chain walking growing without the city growing, which
      is `adr/0006`'s shape arriving through the Rule engine rather than through a collection.
      **Task 10's planning then split the assertion itself, and found it had been filed against the
      wrong slice since slice 5** (`0011` finding 36). Its stated form is a rising `slots` against a
      flat `live` — and the Rule engine **creates no rows at all**: a Rule Instance's life is its
      Building's and subscription allocates nothing, which was recorded as a virtue in finding 2 and
      is also a statement about what the instrument can see. **No Ruleset can make a slot count
      trend.** What churns rows is Buildings arriving and being demolished, so the `slots` half is
      **slice 10's**, with Zone Rules. The flow half stays here and is the only half this slice could
      ever have carried
- [ ] **The end-of-run tier allocates on the Large Object Heap at scale** — ~544 KB at 100k Citizens,
      ~5.4 MB extrapolated at 1M, Gen2 collections at the top of the measured range. Once per run,
      after the trace is written, so it perturbs nothing today. Fix is a scratch buffer on the
      registry; **do it when S0 shows a real 1M city**, not on an extrapolation

## Owed — decisions, and who owns them

**Moved. The open decisions now live in [`0002`](0002-open-questions.md)'s ledger**, typed *measurable*
or *arguable* and grouped by what is blocked on them. This board holds status; it stopped being a view
the moment it became the only place an open question was written down, and that is how it came to hold
63 of them while the file named *open questions* held none.

What stays here is the handful S2 **settled**, because the number is the finding and it belongs beside
the run that produced it.

- [x] ~~**Volume attribution's price** — S2 R2a. Decided by `adr/0041`; the cost is still
      unmeasured~~ **MEASURED by S2 R2a, and it is not a price.** Direct attribution costs
      **139,437 ns/Tick** at the derived 56,000 in flight, against an aggregate smear whose
      **crossover is 105 Ticks** at the anchor — so direct is the *cheaper* scheme at any plausible
      congestion cycle, an order of magnitude past `adr/0041`'s estimate of ~10. The ADR's *"we are
      knowingly paying for correctness"* understates its own case
- [x] ~~**The `adr/0020` exposure** — union-find computes weak connectivity, *"mutually reachable"* is
      strong~~ **SETTLED by S2 R1, against the ADR.** At a tight Commute Budget union-find returns
      **6 Settlements where Tarjan returns 8**, largest component 90 against 70 — a fifth of the map
      assigned to a Settlement it is not mutually reachable within. **`adr/0020` is owed an
      amendment**; see *Owed — documentation debt*. R1 also found the plan asked for the wrong
      instrument: an asymmetry distribution is a claim about travel times, and the test is whether the
      two algorithms disagree about the **city**. And the exposure is a **band, not a threshold** — the
      one-way pair count rises to 264 and falls back to 47 — so no generous Budget closes it

---

## Blocked

**Slice gates are [`0003`](0003-build-plan.md)'s gate board, which is the source.** Only one is still
red — slice 9, on session **C**. Slices 7, 8 and 10 cleared with session A, and slice 6 closing
released S0a, so three rows that sat here through most of Phase 1 have gone.

What this table keeps is everything `0003` does **not** own: Phase 2, Phase 3, and the one row waiting
on code.

| | Blocked on | Which is |
|---|---|---|
| **S0b** — the Tick with work in it | slices 7, 9 and 10 | the only row here blocked on code. **S0a is done** and sized the world; S0b is the half carrying `06`'s stated risk |
| **Phase 2 milestones 5a–10** | 🔴 `03 §5` and six research-written ADRs, plus S2 | sessions **D**–**J**, plus a spike |
| **Planning Phase 2 at all** | S0 must have run, and `06`'s ordering must be re-derived | session **K2** |
| **Phase 3** | 🔴 **a presentation design that does not exist** | session **L**, itself blocked on **S1** and **S3** |

**The Phase 3 row used to read *"unplanned by design, and stays that way"*, and that was wrong** — it
described a choice where the truth is an absence. Phase 3 is unplanned because rendering has never
been designed, never been argued, and has no document to argue: every other phase is backed by `02`,
`03`, `04` or `05`, and there is no equivalent for presentation. Worse, the interface it would build
on was **re-argued to serve something else** — `adr/0002` was rebuilt around hot and cold query
flavours on the finding that it had *"assumed a renderer because rendering is what an engine boundary
is usually for"*, when the actual consumer is an inspector. The chain is written down now, in `06` and
here: **S1 + S3 → L → Phase 3 is plannable.**
