# 0000 — The board

**Read this first.** A flat, scannable status of everything planned and everything done, and the one
place that orders the three tracks against each other.

---

## What is next

✅ **MILESTONE 27 CLOSED 2026-08-24 — ALL FIVE TASKS, AND THE BUSINESS IS NOW A THING THE CITY CONTAINS**
([`0041`](0041-the-business-is-a-thing-the-city-contains.md)). The city creates one **two ways** —
[`adr/0148`](../docs/adr/0148-a-premises-kind-may-declare-its-trade-and-instantiating-one-is-not-housing-anybody.md)
instantiates a premises kind's declared trade with the Building, and
[`adr/0145`](../docs/adr/0145-a-business-is-founded-by-a-household-or-arrives-through-a-gate-and-both-land-in-the-pool.md)/[`adr/0146`](../docs/adr/0146-founding-costs-a-citizen-and-the-households-money-so-the-founder-is-the-first-worker.md)
has a **Household found** one, spending a **Citizen** as well as its money, with the founder recorded by
***the job it does and by nothing else.*** `jobs` and the Shift band moved to the **trade**
([`adr/0141`](../docs/adr/0141-a-tenant-owns-what-leaves-with-it-and-the-premises-own-the-capacity.md)),
which **emptied every shipped city** until the content half landed — 66 assertions on one sentence,
*nobody is employed anywhere*. A **Policy now sweeps Businesses**
([`adr/0149`](../docs/adr/0149-a-business-is-a-population-a-policy-sweeps-and-a-readout-names-every-entity-it-reads-against.md)),
and **`levied.toml` and `founded.toml` shipped with it** — ⚠ **count the files at `rulesets/` rather than here; milestone 24's merge landed two more the day after.**
⚠ **Task 9 is NOT the task its plan described**: the Rule-Instance column `RuleInstanceTable`'s own comment
predicted was **built, crashed on the Tick it fired, and was reverted** — ***`adr/0093` working rather than
failing***, because the comment was right about where to look and was never a claim about what the work was.
🔴 **Task 10's long run found a defect the suite could not** — a razing that identified a Building's own
trade **by kind** stranded shops and leaked their capital, **52 stranded and 23,983 of 354,562 gone per
20,480 Ticks** — repaired by a saved `BusinessTable.Origin`, so **identity and not kind** answers *did this
Business arrive with these premises.* ⚠ **What delayed finding it was a HEADER that predicted its own
symptom**, so the drain was explained and nobody looked.

🟢 **THE LIVE CODE ROW IS NOW MILESTONE 26 — the purchase, where `Scope.Pool` stops throwing**, its tasks
kept at [`0037`](0037-goods-between-buildings-the-district-pool.md) 7–10.
🔴 **27 WAS ITS LARGEST GATE AND NOT ITS ONLY ONE — TWO REMAIN AND THEY ARE BOTH CONTENT**: the **Provider
kind's three decisions** (a second `[[zone_rule]]`, a second decline Rule, a land-use split) and **a world
where a Building genuinely runs out of money**. ⚠ ***[`0002`](0002-open-questions.md) §A says outright that
nobody owns either***, which is why they are in §A rather than in a ledger.
⚠ **The bankrupt world was CHECKED at task 10 rather than assumed, and the answer was no**: **7,165
premisings against ZERO give-ups** over 131,072 Ticks, so ***nothing in the build drains a Business's money
and the world is still unwritten.*** **The gate is an argument sitting, and the standing rule is satisfied
by a milestone being blocked on it rather than by its being available.**

✅ **MILESTONE 12 IS CAPPED AT TASK 6 AND CLOSES THERE, 2026-08-22.** Its risk is rewritten to what
tasks 1–6 actually retire — ***that a District is an administrative label rather than a derived thing
with a market in it*** — because `Scope.Pool` still throws and **a milestone must name a risk it
actually retires**. **Its original risk and its tasks 7–10 moved to milestone 26.**

~~🟢 **The live code row is milestone 25 — the Business is the actor and the Building is premises**,
scoped by session V ([`0039`](0039-session-v-the-business-is-the-actor-and-the-building-is-premises.md)),
**then 27, then 26, the purchase**.~~ ✅ **25 CLOSED 2026-08-23 AND 27 CLOSED 2026-08-24; the run of three
is spent and 26 is what it was for** — see the head of this section. ✅ **DECOMPOSED 2026-08-23 — ten tasks
in two groups, [`0040`](0040-the-business-is-the-actor-and-the-building-is-premises.md).**

> ✅ **MILESTONE 25 CLOSED 2026-08-23 — GROUP A, IN ONE DAY**, and ✅ **ARCHIVED 2026-08-25.** A Bin
> hangs off its **owner**; a Household owns Bins and its Rules follow them; **condemnation ends a
> TENANCY and leaves the premises standing**; and an unpremised tenant waits under a give-up bound and
> then emigrates.
> ⚠ **Its forty-line closure narrative lived HERE and nowhere else in this file's job description**, so
> it is cut to this pointer rather than kept — ***the board is a view and a closure record is a
> ledger's.*** **Findings `F19`, `F28`, `F30`, `F35`, `F36`, `F38`, `F39`, `F43`, `F44` and `F45` are
> all in [`0040`](0040-the-business-is-the-actor-and-the-building-is-premises.md)**, which is where they
> were written and where the caveats around them still are; the row's own entry is
> [`0003`](0003-build-plan.md)'s. 🔴 **Two of them are worth knowing you have not read**: **F30**, one
> starving Household condemning the Building its neighbours lived in, and **F36**/**F38**, a saved table
> outside `World._tables` going unhashed while 2,074 tests passed. ***Follow the link rather than
> quoting this summary*** ([`0012`](0012-corpus-audit.md) **Cause 5**).
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
at group A, 27 closed 2026-08-24**, and the live row is **26**.
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
| **1** | code | 🔴 **Milestone 26 — the purchase, and its CONTENT GATE IS DISCHARGED.** Session W settled what raises a Provider, what declines one and the land-use split ([`adr/0163`](../docs/adr/0163-demand-for-a-shop-is-elapsed-unserved-need-in-reach-and-building-claims-it.md)–[`adr/0166`](../docs/adr/0166-a-business-runs-rules-and-its-rules-live-as-long-as-its-tenancy.md)). ⚠ **It did not get smaller: `adr/0166` widens the Rule Instance's subject to a Business and 26's decline half depends on it** | [`0044`](0044-the-purchase-and-the-provider-that-answers-it.md) | **Decomposed 2026-08-25 into ten tasks, the inherited order was backwards, and 🟢 TASKS 1–9 OF TEN HAVE LANDED** — a Business runs Rules, the land-use split, `rulesets/provisioned.toml`, `Scope.Pool` resolving ([`adr/0167`](../docs/adr/0167-a-purchase-picks-its-seller-by-a-draw-and-waits-on-the-market-rather-than-on-a-shop.md)), Evidence telling bankruptcy from starvation, **a shop that can go broke** ([`adr/0169`](../docs/adr/0169-a-standing-cost-needs-a-counterparty-so-a-trade-pays-rates-until-there-is-a-supplier-to-pay.md)) **a shop raised on hunger rather than on homelessness** ([`adr/0170`](../docs/adr/0170-a-shop-is-selected-rather-than-sited-so-the-birth-signal-is-coarse-and-death-does-the-correcting.md)) `--market`, whose *who could not afford it* panel is **141 of 408** starving Rules, and the 524,288-Tick acceptance run, with **task 7 run before task 6 at the user's instruction**. 🟢 **QUEUE ITEMS 21 AND 22 ARE STRUCK, 2026-08-26 by one record** ([`adr/0171`](../docs/adr/0171-a-markets-level-is-what-its-sellers-hold-and-the-price-divides-by-the-sum-while-a-wake-spends-the-maximum.md)) — a market holds nothing, so its level is what its **sellers** hold, the price divides by the **sum** and a wake spends the **maximum**; the price moves for the first time in this project's history, `oversupplied.toml` walking **100 → 58**, and both worlds now run 524,288 Ticks with **no invariant violated**, against **two further call sites reading the same undefined quantity with no symptom**, **neither placement queue item 22 offered being taken**, and **a first repair that relocated the violation onto the world that had been the control**. 🔴 **Six things are live and none is a task**: a Rule Instance can be left asleep on a Bin that covers it because a **readout** shrank its band under it, which `adr/0171` unmasked rather than caused ([`0003`](0003-build-plan.md) queue item 23), the purchase is [`0013`](0013-tick-budget.md)'s only super-linear consumer with `adr/0139`'s fallback already spent ([`0002`](0002-open-questions.md) §B), `adr/0139` contradicts itself about where a price sits ([`0012`](0012-corpus-audit.md)), four Ruleset numbers are unratified and the demand threshold **saturates** ([`0002`](0002-open-questions.md) §D1), the tier-1 scan's own cost is **UNMEASURED** because every arm that removes it removes the market ([`0013`](0013-tick-budget.md)), and `adr/0168` on an unmerged branch **refuses a key this milestone's Ruleset states twice** — **findings, decisions and per-task detail: [`0044`](0044-the-purchase-and-the-provider-that-answers-it.md) F1–F60** |
| **2** | tidy | 🔴 **Sweep every Ruleset key against *would a designer ever set this*** ([`adr/0164`](../docs/adr/0164-a-ruleset-key-is-designer-facing-or-it-belongs-in-the-instrument.md)). ⚠ **Raised by the user, and session W's own near-miss is the worked example** — a key was one exchange from being authored with a ratifier chosen. **Not run** | [`0012`](0012-corpus-audit.md) | ⚠ ***The remedy for a borderline case is a disclaiming comment, never a deletion*** — a sweep that deletes keys does more damage than the thing it fixes |
| **3** | code | **Milestone 24 — terrain and the land rows: every task DONE, all twelve decisions settled.** Runoff and the shoreline term both landed 2026-08-24, so `02 §2.4` composes three of four terms and only amenity is left (**F17**, **F18**). 🔴 **Dumping stays UNBUILT and wants the designer in the room** — it needs a `Scope` that reaches a Water Body, and a Bin can *fail* where a Map Layer cell cannot. | [`0042`](0042-terrain-and-the-land-rows.md) | The only other ungated code row with **nothing upstream of it** |
| **4** | spike | ⚠ **Do NOT delete `spikes/S2.Routing/`.** The 5a gate is discharged, but another session is doing research inside it, so it is live work. 51 tracked C# files, 29,719 lines | [`0010`](0010-s2-routing.md) → *R7* | ⚠ ***A deletion held twice for unrelated reasons is the row that gets struck when the wrong one clears*** |
| **5** | spike | **S5 owes two captures** — the 4-thread Lane kernel rung, which is bimodal, and the canonical `performance` re-capture. 2 threads is settled at 1.84–1.93× | [`0019`](0019-s5-lane-kernel.md) | ⚠ **Quote the supply-side multiple as *at least 1.84× and plausibly near 4×*, never as 4× bare** |
| **6** | tidy | ⏸ **HELD — do not delete `spikes/S4.Kernels/` yet.** S4 task 11, open since the spike closed | [`0004`](0004-s4-kernel-benchmark.md) | ⚠ **Held 2026-08-22 on a stated condition, where it previously read *gated on nothing*.** It goes when we are certain we will not revisit what it holds, and nobody is certain yet. ***A deletion that might be revisited is a bet rather than tidying***, and this row was ranked as housekeeping. **The condition is the trigger — do not promote this row because the suite is green** |

**Closed rows are in [`0000a`](0000a-board-archive.md)**, one line each with the document that owns the
record. ✅ **The argument track HAS a promoted row as of 2026-08-25, and it is rank 1** — session W, [`0043`](0043-session-w-the-provider-kinds-content.md). ⚠ **This sentence read *no promoted row* while §A carried an open blocker on the live code milestone**, which is the board doing the thing it warns about.

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
| **L** | **A presentation design** | **It does not exist**, and every other phase is backed by a design document while rendering has none. ⚠ **One piece came out of it early on 2026-08-24** — [`adr/0150`](../docs/adr/0150-appearance-is-derived-in-the-shell-and-a-kind-is-not-a-mesh.md) settles that appearance is composed in the **shell**, never enters the `World`, and that a `[[building]]` kind is **not** a mesh id. **L's scope is unchanged**, and the geometry fork and the appearance input set are filed to [`0002`](0002-open-questions.md) §B and §C. | **Phase 3** |

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
