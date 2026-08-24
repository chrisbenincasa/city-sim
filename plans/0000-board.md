# 0000 — The board

**Read this first.** A flat, scannable status of everything planned and everything done, and the one
place that orders the three tracks against each other.

---

## What is next

✅ **MILESTONE 12 IS CAPPED AT TASK 6 AND CLOSES THERE, 2026-08-22.** Its risk is rewritten to what
tasks 1–6 actually retire — ***that a District is an administrative label rather than a derived thing
with a market in it*** — because `Scope.Pool` still throws and **a milestone must name a risk it
actually retires**. **Its original risk and its tasks 7–10 moved to milestone 26.**

🟢 **The live code row is milestone 25 — the Business is the actor and the Building is premises**,
scoped by session V ([`0039`](0039-session-v-the-business-is-the-actor-and-the-building-is-premises.md)),
**then 27, then 26, the purchase**. ✅ **DECOMPOSED 2026-08-23 — ten tasks in two groups,
[`0040`](0040-the-business-is-the-actor-and-the-building-is-premises.md).**

> ✅ **MILESTONE 25 CLOSED 2026-08-23 — GROUP A TASKS 1, 2, 4, 5 AND THE CLOSING TASK, IN ONE DAY.** ① a Bin hangs off its **owner**
> ([`adr/0143`](../docs/adr/0143-a-bin-hangs-off-its-owner-and-the-polymorphic-column-stays-unbuilt.md)),
> and the polymorphic column `adr/0114` gestured at is **not built**. ② a Household owns Bins, its Rules
> follow them, and the arming stagger mixes the **tenant** — ⚠ **which is ONE task and `adr/0141` had
> already said so in its *Rejected* section** (`0040` **F19**): the decomposition split what the record
> governing it had declined to split. **A Ruleset says `owner = "occupant"`; a Rule's side is DERIVED
> from its own `local` terms and a mixed one is refused at load.** 🔴 **The shipped city now holds three
> times the stock** — the draw is unchanged, the supply is not — and every one of the **twelve** edited
> Rulesets says so in its own header. 🔴 **The State Hash moved; four golden artefacts re-recorded; the
> version byte NOT bumped**, because the fold did not change. ⚠ **`derived = "occupancy"`, the one
> declared Readout in the project, has lost its only caller.** ④ **condemnation ends a TENANCY and
> leaves the premises standing** — one walk, filtered on the subject, premises judged first, and a
> failing tenant evicted through `World.Unplace` while the Building stands. 🔴 **It removes a defect
> TASK 2 SHIPPED** (`0040` **F30**): pressure was taken across the Building's whole Rule list, so once a
> tenant had Rules of its own, ***one starving Household condemned the Building its two neighbours were
> living in*** — live for the length of one commit, and no test failed because nothing in the suite had
> two tenants failing differently. ⚠ **No golden artefact moved**, since a tenancy ending is reachable
> only past `condemn_after` and no golden session gets there. **`RuleEvidence` and `BinEvidence` gained
> `Tenant`**, which closes **F28** and uncovered a second hole in the same panel: the bin table showed
> Rules drawing from Bins it did not display. 🔴 **Nothing records *why* a tenancy ended** (**F35**) —
> the condemnation trail is a **Lot's**, so an entry there would be a demolition record for a Building
> still standing; that channel is `adr/0130`'s and ships with task 5.
> ⑤ **the unpremised pool and the emigration sink** — a Business that loses its premises waits under a
> give-up bound and then leaves the city with its money. **Open decisions 1 and 3 were settled first,
> into [`adr/0144`](../docs/adr/0144-a-tenant-that-loses-its-premises-keeps-only-its-money-and-waits-a-households-wait.md)**:
> a tenant carries across the gap precisely what does not depend on premises for its bounds, and a
> shop's patience is a family's as a declared stand-in. ⚠ **The pool ships with ONE exit and it is the
> sink** — nothing tenants a Business, so the placement half is milestone 27's. 🔴 **The State Hash
> moved; three golden baselines re-recorded.** 🔴 **TWO LATENT DEFECTS FOUND, ONE ALREADY ON `main`**
> (`0040` **F36**, **F38**): a **saved table outside `World._tables` is not hashed** — 2,074 tests
> passed with one — and the census's **hand-maintained per-family slot count** was wrong for
> `ZoneCounters` since task 4, so *tenancies ended* and *placement considered* printed the identical
> four numbers. ⚠ **Same shape both times**: a declaration and a hand-kept count with nothing checking
> they agree (**F39**). Both closed by tests; ***the class is not closed.***
> ⑩ **something to look at, and the long run** — 🔴 **and the thing to look at DID NOT EXIST**
> (`0040` **F43**): on all twelve shipped files the premises fail and the tenants never do, so
> `minimal.toml` reads **2,610 condemned against 0 tenancies ended** and task 4's mechanism was
> invisible in every world the build could generate. ⚠ **Every test of it built its Ruleset by hand**,
> which is why nothing noticed. **`rulesets/evicted.toml`** is the thirteenth file — two Rules deleted,
> because the failure only had to move from the premises to the tenant — and reads **929 tenancies
> ended against 0 condemned**. 🔴 **The long run's stated obligation was the wrong collection**
> (**F44**): the unpremised pool is empty in every world, and the one milestone 25 introduced is the
> **tenant's Rule Instances and Bins**. **131,072 Ticks: both allocators' slot counts FLAT** under
> ~1,900 tenancies ending per window (**F45**).
> **Milestone 25 is closed. Next is 27 — the Business is a thing the city contains.**
🔴 **CAPPED AT GROUP A THE SAME DAY, with the user in the room — 25 is tasks 1–5 plus the closing
task, and ITS RISK IS REWRITTEN** to ***that a Rule Instance names premises rather than an actor, so no
money term can resolve to a payer.*** ⚠ **Group A makes the actor NAMEABLE; it does not make one
EXIST.** 🔴 **Tasks 6–9 became MILESTONE 27 — *the Business is a thing the city contains*** — which is
25's **original** risk, unretired, and it sits **between 25 and 26** by row order
([`adr/0140`](../docs/adr/0140-a-milestone-number-is-an-identity-and-the-roadmaps-order-is-the-sequence.md):
next-free, never renumbered). ⚠ **Second cap in two days, and the difference is the whole point** —
***12 was capped by running out of road; 25 was capped by decomposition before a line of code was
written.*** **Group A is a repair driven end to end by the Household that needs nothing which does not
exist; every entry in group B needs something that does.** 🔴 **Decomposition found what two ADRs and a whole sitting did not** — `0040`
open decision **1**: `adr/0141` keeps a Bin's capacity keyed on the **building kind**, `adr/0142` makes
**unpremised** a legitimate steady state, and `Businesses.Building` is `Reference.Severable`, ***so an
unpremised Business owns Bins whose capacity is declared by premises it does not have.*** ⚠ **It also
made the milestone SMALLER in one place** — the Rule half's blast radius is the derived `BuildingRules`
list and **not** the saved handle, which no test reads at all. **Session V's fifth question left §A by
changing type** and is now [`0002`](0002-open-questions.md) **§D2** — ***what capitalises a Business***,
which blocks 26 exactly as the payer did. ⚠ **25 and 26 sit BETWEEN 12 and 13 in `06`'s table and that is correct**:
[`adr/0140`](../docs/adr/0140-a-milestone-number-is-an-identity-and-the-roadmaps-order-is-the-sequence.md)
makes a milestone number an **identity** and the table's **row order** the sequence. ***Read that table
top to bottom; its first column no longer sorts.***

~~**The next code row is [`06`](../docs/06-roadmap.md) milestone **12** — Goods between Buildings, the
District Pool.**~~ Ungated, scoping under way in
[`0037`](0037-goods-between-buildings-the-district-pool.md), **tasks 1 through 6 shipped 2026-08-22**. Decisions **1, 2, 4, 5, 6,
8 and 9 are settled** ([`adr/0132`](../docs/adr/0132-the-district-is-derived-and-a-ward-is-what-the-player-draws.md)–[`adr/0138`](../docs/adr/0138-freight-is-unbuilt-so-the-min-follows-it-and-neither-is-at-twelve.md));
**open: 3 and 10**. ⚠ **`0037` no longer owns 10** — it was escalated 2026-08-22 to **session U**,
[`0038`](0038-session-u-the-pool-or-the-seller.md), and to [`0002`](0002-open-questions.md) §A.

✅ **DECOMPOSED 2026-08-22 — ten tasks, tasks 1 through 6 shipped.** **3 is an obligation, not a fork** — `adr/0052`
requires a ratifier be *named*, not that the number be settled. **7 is largely pre-answered** by
`adr/0134`. 🔴 **10 is new and decomposition found it**: the Pool is the counterparty on **both** sides
of a trade and the two sides happen at different Ticks, so *where the money sits between a Provider's
deposit and a consumer's draw* is unanswered by `adr/0050`, `adr/0135` and `adr/0114` alike. ***Ordering
the work asked what each task needed and found a question seven decisions had not.***

✅ **AND 10 OUTGREW THE DOCUMENT THAT FOUND IT AND CLOSED THE SAME DAY, VOID AS POSED** —
[`adr/0139`](../docs/adr/0139-a-district-pool-is-a-market-and-not-a-store-so-stock-stays-with-the-seller.md),
***a District Pool is a market and not a store, so stock stays with the seller.***
[`adr/0013`](../docs/adr/0013-goods-are-pooled-within-a-district-and-shipped-between.md) is **amended,
not superseded**: it decided **reach**, and custody was a reading it never argued for. Decision 10 was
never a question about money — **a District owns nothing, so the Pool was never a party to the trade**,
and the seller's money Bin is a Business balance that already exists.

⚠ **What task 7 must build changed, and it did not grow.** `Scope.Pool` resolves to a **seller's** Bin
beside the two searches `RuleEngine.Bin` already runs; the market row is the **wake target**, compared
by `BinRef` with **no resolution**; and a seller's price opens at the **import ceiling**, which costs
**no new number and no new ratifier**. ⚠ **Most of tasks 5 and 6 stands** — 20 of 20 loader tests and
22 of 26 price tests untouched, and `MarketRuleset` survives with its signature unchanged.
⚠ **The seller-lookup cost is *measurable* and UNMEASURED**, and `adr/0139` says so itself.
🔴 **TASK 7 IS BLOCKED AGAIN, and by the correction to `adr/0139` rather than by anything it decided.**
That record put a seller's Goods in *the selling **Building's** own Bin* and a seller's money in *a
**Business** balance* — **one seller, two custodians** — and it wrote *Building* because the code does,
which is [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
**inverted**. ⚠ **A purchase needs a payer**: money lives on a Business, a Rule Instance names a
Building, and a Building holds a **list** of Businesses. **Now session V**,
[`0039`](0039-session-v-the-business-is-the-actor-and-the-building-is-premises.md), and it is
**milestone-sized**. ⚠ **On its main axis it is a CORRECTION** — `adr/0113` decided it and `adr/0114`
wrote *"`World.FindBin` takes an owner rather than a Building slot"* — ***so a revisit trigger that had
already fired has been sitting finished-in-design and unbuilt since milestone 10.***

🔴 **Session U also found [`0003`](0003-build-plan.md) queue item 17** — `World.RetirePool` raises the
heir Bin's level with a raw `Bins.Move` and never drains it — ***found while reading for a design
question and not while looking for a defect.***

⚠ **Two more things decomposition turned up, and both are the reason to do it before starting.**
🔴 **Task 5 found a defect it does not own and routed it rather than fixing it:
[`0003`](0003-build-plan.md) queue item 15** — a Ruleset edit that **inserts** a `[[resource]]` crashes
the swap, on the **treasury**, on `rulesets/minimal.toml`, with no District anywhere. `RulesetMigration`
maps Resources by name and `World.Migrate` applies that map to **Building Bins only**. ***The migration
is right and its reach is short.***

✅ **Task 5's blocker — [`0003`](0003-build-plan.md) queue item 14 — was settled 2026-08-22 and the row
has left the board**: the invariant narrowed to the head of the wait list, on `adr/0063`'s own argument
rather than on the cheaper repair, and the narrowing turned up a second half nobody had reasoned to — a
woken waiter records no claim, so the drain's guarantee is true of an *instant*. **Moves no hash.**
⚠ ***Decomposition found it and decomposition is what unblocked it***, a week before `Scope.Pool` would
have. And a **fourth precondition** still stands: `BinOwnerKind` has four members, none is a District,
and `BinTable.Owner` is a `HandleColumn<Building>`.

✅ **Task 1 was a WORLD and not code, and it shipped 2026-08-22** — `rulesets/twinned.toml` and the
`[[lattice]]` key, two lattices **joined by a Street corridor** so that only the density field can split
them. ***That is milestone 11 task 3's lesson arriving before the milestone instead of during it.***
⚠ **The key is not spelled `settlement`** and `CONTEXT.md` → Lattice says why. ✅ **Task 2 found its field already built** — `BuildingResidency`, 5b-bis — so it shipped a name and a measurement.

⚠ **Two things a reader of this row needs and will not guess.** The survey found **three preconditions
no document had listed as blockers** — the largest being that ***there is no District in the build at
all***. And **Upkeep is no longer part of it**
([`adr/0136`](../docs/adr/0136-upkeep-has-three-blockers-landing-at-three-times-so-it-has-a-queue-and-not-a-milestone.md),
2026-08-22), and **neither is freight nor `adr/0088`'s `min()`**
([`adr/0138`](../docs/adr/0138-freight-is-unbuilt-so-the-min-follows-it-and-neither-is-at-twelve.md),
same day) — 🔴 ***three mechanisms found parked at 12 on an assumption their authors did not check***,
each placed by a document that was not scoping the milestone. **`06`'s rows for all three are now
UNPLACED.** ⚠ **The consequence to carry forward: 12 makes import real as a PRICE and not as traffic**,
so a distant gate costs nothing until freight lands and `adr/0088`'s thesis is deliberately inert. ***A milestone whose named risk is a single `throw` reads as a milestone with a single
obstacle***, and the `throw` is the symptom.

**Status for every other milestone is [`0003`](0003-build-plan.md)'s Phase 2 ledger.** This section
carried it until 2026-08-22 and had grown to **551 lines**; see *How to read this file*.

---

## How to read this file

**This is a view, not a source, and it owns nothing.** Three documents answer three questions:
***why in this order*** is [`06`](../docs/06-roadmap.md)'s; ***what is done and what gates it*** is
[`0003`](0003-build-plan.md)'s — both ledgers and both gate boards, Phase 0 through Phase 2;
***what is next*** is this file's, and it is nothing but an index over the other two.
[`0002`](0002-open-questions.md) owns **every open question** and the **§F coverage map**;
`docs/adr/` the decisions; [`0013`](0013-tick-budget.md) what a Tick costs;
[`0012`](0012-corpus-audit.md) what a document says wrongly. **When they disagree, they win.**

**Three rules keep it a view rather than a second ledger.** ⚠ **All three were broken by 2026-08-22.**

1. **Do not write an open question here** — that is how it once held 63 while the file named *open
   questions* held none.
2. **A cell is at most three sentences.** One had reached 15 sentences and 3,986 characters.
3. **A closed row leaves**, to [`0000a`](0000a-board-archive.md), one line each.

✅ **All three are enforced mechanically as of 2026-08-22** — `BoardShapeTests`, in the assertion tier,
so a breach fails the commit gate rather than waiting for somebody to notice. A fourth check caps the
whole file. ⚠ **They catch the symptom and not the cause**: when one fires, the fix is not to delete
lines but to find ***the document that should have held them***.

⚠ **Cleared three times — 2026-08-12, 2026-08-15 and 2026-08-22 — and the third had a different
cause.** The first two were hand-clearings and both grew back within days. The third found that
`0003` covered only Phase 0 and Phase 1, so per-milestone status for eleven shipped Phase 2 milestones
had nowhere to live and this file grew a **551-line** *What is next* doing a ledger's job.
***A document that declines a layer does not thereby abolish it***, so the repair was to give `0003` a
Phase 2 ledger **first**. [`0000a`](0000a-board-archive.md) holds the recovery pointers.

> ⚠ **This file is the document most likely to be read instead of the build.** On 2026-08-13 a sitting
> read a paragraph here to answer *what is next* and reported work that had shipped an hour earlier in
> the same tree. [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
> applies to the board itself.

---

## State of play

**What the project is.** A city-builder whose simulation is an ordinary C# library with no game engine
inside it. Godot will be the display layer and has not been started.

**Where it is.** **Phase 2, between milestones.** Phase 1 is closed; **12 closed at task 6, 25 closed
at group A**, and the live row is **27**.
⚠ **Which milestones have shipped and which queue items stand open are [`0003`](0003-build-plan.md)'s,
enumerated there and never here.** This sentence carried its own copy of both until 2026-08-23 and both
had drifted: it named **12** as the live row two closures later, and it named queue items **8** and
**10**, which ***have no row in that queue at all***, while **13**, **15**, **17** and **18** stood open.
***A view that keeps its own copy of the source is no longer a view*** — the correction the *Done*
section took on 2026-08-22, arriving one section later and by the same route.

**What works.** Typed tables with every field declared once, integer-only arithmetic, a deterministic
eight-phase Tick, replay and save/load that both recompute their own hash, Map Layers with diffusion,
two Rule families, a Road Graph with Lots and Access Points, and a movement stack through to a
vehicular Leg and a volume-delay function. **Runnable commands are [`CLAUDE.md`](../CLAUDE.md)'s.**

**What does not exist.** Two of the eight Tick phases are empty. There is **no supply chain** — that
crossing is the District Pool, the named hole that throws — and **no land use**, so every Building has
the same occupants and posts.

**Known problems, none urgent, none owned here.** Routing does not fit the Tick budget
([`0013`](0013-tick-budget.md)); the network runs out of routes rather than road
([`0002`](0002-open-questions.md) §C); the job-search box does not filter and cannot in a foot-only
world ([`adr/0089`](../docs/adr/0089-the-map-is-sized-by-how-many-commutes-fit-across-it.md)); the
synthetic fixture and `World`'s table sizing disagree with nothing checking; and **every S2 and S0a
absolute is `powersave`, mis-pinned, or both**
([`adr/0047`](../docs/adr/0047-routing-never-keys-on-the-district.md)).

⚠ **Two things no shipped Ruleset can demonstrate**, both measured: `minimal.toml` severs **0.0%** of
pedestrians at every dial value, and **a generated city cannot congest itself** — `v/c` peaks at 0.44
at every population. **Read a Ruleset's own header before quoting anything out of it.**

### The five numbers to hold in your head

**[`0013`](0013-tick-budget.md) owns the bill and [`spike-results`](../docs/spike-results.md) owns the
captures.** These five are here because no single document holds all five, and a reader needs them
together. ⚠ ***Quote the sentence, never the digits.***

| | Number | What it means |
|---|---|---|
| **The good one** | **8.72 ms a Tick at 1M — 55.9% of the budget at 4×** (S0b) | The **only** Tick figure ever taken from a real running city |
| **The sum** | [`0013`](0013-tick-budget.md) reads **≥44–50 ms a Tick** | ⚠ ***Carry the bill, not the percentage.*** Against a settled 15.6 ms at 4× on one core, the gap is **~3×** |
| **The row that decides it** | **Routing is 37.6–42.0 ms of those 44–50 — 85% of the bill** | Its unit came off a synthetic harness and its multiplicand counts the wrong event |
| **The correction with a known direction** | A diverting Traveller re-searching costs **134.135 ms a Tick** at target scale | Points **up**, and the cache cannot rescue it. **Answered rather than reduced** by [`adr/0061`](../docs/adr/0061-a-diversion-rejoins-by-local-descent-and-a-rejoin-is-never-a-search.md) |
| **Scale** | 1M Citizens in **86 MiB** of tables; 100,000 Ticks in 11.75 s | Sizing risk retired. **One State Hash is 32.47 ms — 2.08 Tick budgets** |

**The meta-figure, and it is the one to be uneasy about.** *Every* time a fixture has been replaced by
a real world, the number came in **worse**. [`0013`](0013-tick-budget.md) states the general form —
***a unit cost is a hypothesis until a real world has produced one*** — and routing's has never met a
world. The enumeration and its one counterexample are
[`0019`](0019-s5-lane-kernel.md):348 and [`0013`](0013-tick-budget.md):594–608.

---

## Do these next

**Build. The argument track is not the constraint**, and treating it as one is how this project starts
going in circles. **The standing rule: an argument session runs when something concrete is blocked on
it, and never because it is available.**

⚠ **The three tracks contend — for conclusions, for cores, and for names.** A capture whose subject is
parallelism names *nothing else running on this machine* as its first control — ⚠ **widened from *in
this repository* on 2026-08-24**, because ***cores are a property of the machine and not of the
repository*** and the narrower wording would have passed a machine carrying an unrelated project's
compiler at 603%. [`0012`](0012-corpus-audit.md) holds all three sightings;
[`spike-results`](../docs/spike-results.md) → *S5 L6* owns the control list.

| | Track | Task | Plan | Why this one |
|---|---|---|---|---|
| **1** | code | 🔴 **Milestone 27 — the Business is a thing the city contains.** Not started; ungated 2026-08-23 and decomposed 2026-08-24 into [`0041`](0041-the-business-is-a-thing-the-city-contains.md), which owns the order, the preconditions and task 8's ratifier obligation under [`0002`](0002-open-questions.md) §D2 — ⚠ **an obligation and not a block** (`0041` **G13**), so nothing stalls tasks 6, 9 or 8. 🔴 **The order is 6 → 9 → 8 → 7 and not the specification's** — run it as written and task 7 takes employment to zero (`0041` **G5**). | [`0041`](0041-the-business-is-a-thing-the-city-contains.md) | **It carries milestone 25's ORIGINAL risk** — *that the economic actor does not exist in the build* — and **26 is blocked on it**: 25 made the payer **nameable**, and 27 is what makes one **exist** |
| **2** | code | **Milestone 24 — terrain and the land rows.** Scoped out of sequence and split in the scoping; **tasks 1, 2, 3, 4, 5, 6a, 8a and 8b are DONE**, all twelve decisions are settled, and 6b, 7, 9 and the long run are left. ⚠ **Tasks 4 and 8b are two halves of one loop and each found a defect in the other's shape** — Sealing's decay had never once run and stalled short of bare ground, and regrowth needed a saved ceiling the scoping had not anticipated (**F12**, **F13**). | [`0042`](0042-terrain-and-the-land-rows.md) | The only other ungated code row with **nothing upstream of it** |
| **3** | spike | ⚠ **Do NOT delete `spikes/S2.Routing/`.** The 5a gate is discharged, but another session is doing research inside it, so it is live work. 51 tracked C# files, 29,719 lines | [`0010`](0010-s2-routing.md) → *R7* | ⚠ ***A deletion held twice for unrelated reasons is the row that gets struck when the wrong one clears*** |
| **4** | spike | **S5 owes two captures** — the 4-thread Lane kernel rung, which is bimodal, and the canonical `performance` re-capture. 2 threads is settled at 1.84–1.93× | [`0019`](0019-s5-lane-kernel.md) | ⚠ **Quote the supply-side multiple as *at least 1.84× and plausibly near 4×*, never as 4× bare** |
| **5** | tidy | ⏸ **HELD — do not delete `spikes/S4.Kernels/` yet.** S4 task 11, open since the spike closed | [`0004`](0004-s4-kernel-benchmark.md) | ⚠ **Held 2026-08-22 on a stated condition, where it previously read *gated on nothing*.** It goes when we are certain we will not revisit what it holds, and nobody is certain yet. ***A deletion that might be revisited is a bet rather than tidying***, and this row was ranked as housekeeping. **The condition is the trigger — do not promote this row because the suite is green** |

**Closed rows are in [`0000a`](0000a-board-archive.md)**, one line each with the document that owns the
record. **The argument track has no promoted row.**

---

## Done

**[`0003`](0003-build-plan.md) owns both ledgers and this file keeps no copy.** Phase 0 and Phase 1 are
its *slice ledger*, slices 0–10; Phase 2 is its *Phase 2 ledger*, keyed by `06`'s milestone number.
Each row there names the gate, links the plan document that owns the tasks and findings, and states
what is done.

⚠ **A copy of that table lived here until 2026-08-22 and had already drifted** — it stopped at
milestone 10 while 11 and 12 stood. ***A view that keeps its own copy of the source is no longer a
view.***

### Spikes

**[`spike-results`](../docs/spike-results.md) owns every number and [`0003`](0003-build-plan.md)'s
spike table owns the gates.** S4, S0a, S0b, S2 and S5 have all run; **S1 and S3 have not** — Track B,
Godot only, ungated, and the empirical inputs to session **L**.

⚠ **One caveat travels with every S2 figure**: R1–R5 ran on a **frozen cost basis**, so quote nothing
from them as a statement about a congested city ([`0010`](0010-s2-routing.md):1016).

---

### Sessions

**An index, not a record** — every finding belongs to the linked document. Kept because sessions are a
board-tracked axis and nothing else lists them in one place.
[`PROCESS.md`](../PROCESS.md) → *Numbering* owns the lettering.

**Closed:** A, B, C, D, E, F, H, J, K, M, P, Q, T, *eight*, *nine* — records in
[`0017`](0017-session-d-the-traffic-model.md), [`0018`](0018-session-n-the-bin-the-pool-and-the-economy.md),
[`0024`](0024-session-j-the-save-the-map-and-the-outside.md), [`0025`](0025-the-player-model.md),
[`0027`](0027-session-t-the-target-speed.md), [`0029`](0029-session-e-fidelity.md) and the ADRs each
produced. **Open:** N, and what is open is task 5's residue. **Never opened:** G, R, L.
✅ **V closed 2026-08-22** — the Business is the actor,
[`0039`](0039-session-v-the-business-is-the-actor-and-the-building-is-premises.md), **opened and closed
the same day**, into [`adr/0141`](../docs/adr/0141-a-tenant-owns-what-leaves-with-it-and-the-premises-own-the-capacity.md)
and [`adr/0142`](../docs/adr/0142-an-unpremised-business-emigrates-so-the-sink-is-the-one-households-already-use.md).
🔴 **Its fifth question did not close and changed type on the way** — *what capitalises a Business* is a
hash-bearing **number** rather than a shape, so it left §A for [`0002`](0002-open-questions.md) **§D2**
and is owned by milestone **27**.
✅ **U closed 2026-08-22** — the Pool or the seller,
[`0038`](0038-session-u-the-pool-or-the-seller.md), into
[`adr/0139`](../docs/adr/0139-a-district-pool-is-a-market-and-not-a-store-so-stock-stays-with-the-seller.md).

---

## Open tracks

### The argument track — a menu, not a queue

~~**Nothing in it gates a slice.**~~ ~~🔴 **V DOES** — it blocks milestone **26**, which is milestone 12's capped-off remainder, and it is milestone-sized.~~ ✅ **NOTHING IN IT GATES A SLICE AGAIN, 2026-08-23** — V opened and closed on 2026-08-22, and milestone **25**, the row it unblocked, closed the next day. ⚠ **U also gated something, for one afternoon on 2026-08-22, and closed the same day**
into [`adr/0139`](../docs/adr/0139-a-district-pool-is-a-market-and-not-a-store-so-stock-stays-with-the-seller.md)
— the standing rule working rather than failing: *an argument session runs when something concrete is
blocked on it.* Take from the three below when something is waiting and leave them alone otherwise. **Closed sessions are in [`0000a`](0000a-board-archive.md).**

| | Session | What is missing | Unblocks |
|---|---|---|---|
| **G** | `adr/0016` — the lane is the entity | Carries the order-of-magnitude claim the whole microscopic tier rests on. ⚠ **Partly discharged by S5** | milestone **21** |
| **R** | `05 §6`'s threading policy | The obligation `06` could not give a milestone | lint 4 |
| **L** | **A presentation design** | **It does not exist.** Every other phase is backed by a design document; rendering has none | **Phase 3** |

### Not arguable, and the audit still owed

The **Microscopic Cap** needs a built traffic model and **S2** is measurement — argument closes
neither. [`0002`](0002-open-questions.md) names a set of **playtest questions wearing design-question
clothing** this track must not drift into; ⚠ **session P grilled all three `01` sections and left that
set intact**, because ***an examined section is not thereby a settled one.***

⚠ **OPEN — type every claim `arguable` or `measurable`**
([`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)),
**across every section rather than only the ungrilled ones**: two claims measured false sat in 🟢 rows
of the coverage map. **Read the suspect marks from [`0002`](0002-open-questions.md) §F**, which owns
them — an enumeration here is how the last one went stale.

---

## Owed — documentation debt, none of it blocking

**Not held here.** [`0002`](0002-open-questions.md) owns every corpus debt as an open item, in fuller
form than a board cell can carry — the stale `05` figures, the `05 §3` and `03 §3.8` corrections, the
`06` spike specifications, and the **33 occurrences across 22 files** of a term `CONTEXT.md` bans
outright. [`0012`](0012-corpus-audit.md) owns what a document says *wrongly*, with its Causes and its
mechanical checks. ⚠ **A debt in two ledgers is the defect `0012` exists to diagnose**, which is why a
copy stopped living here on 2026-08-22.

---

## Blocked

**There is no red gate anywhere in the corpus.** [`0003`](0003-build-plan.md) owns both gate boards —
Phase 1's is its *gate board*, Phase 2's is the *Gate* column of its Phase 2 ledger.

| | Blocked on | Which is |
|---|---|---|
| **Phase 3** | 🔴 **a presentation design that does not exist** | session **L**. ⚠ **S1 and S3 are themselves ungated**, so the head of the chain is runnable; what stops it is that **Track B has never been stood up**. **The chain is S1 + S3 → L → Phase 3 is plannable** |

**Phase 3 is undesigned, not unplanned**, and the distinction describes an absence rather than a
choice.
