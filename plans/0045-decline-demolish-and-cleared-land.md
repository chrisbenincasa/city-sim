# 0045 — Decline, Demolish and cleared land

**Milestone 17.** Scoped 2026-08-25, against `main` at `fabca9f`.

---

## Status

🟡 **SCOPED, NOT STARTED.** ✅ **GATE ASSESSED THE SAME DAY: no document names a gate on milestone 17.**
[`0003`](0003-build-plan.md)'s Phase 2 ledger holds it in the collective row whose Gate cell is `—`,
[`0002`](0002-open-questions.md) §A has no row against it, and [`06`](../docs/06-roadmap.md):101 names
no upstream. ⚠ **That is an absence of a recorded gate and not a survey**; what scoping found instead is
that two of this milestone's own mechanisms are blocked on things outside it, and they are decisions
**3** and **4** below rather than gates on the row.

---

## Why this milestone exists, in one paragraph

⚠ **This is not a feature. It is the build being made to agree with a decision taken 2026-08-16.**
[`adr/0091`](../docs/adr/0091-clearing-land-is-bought-rather-than-taken-and-demolish-is-the-sixth-verb.md)
found that [`02 §5.9`](../docs/02-simulation-model.md) **contradicts itself twelve lines apart** —
*"Past a further threshold, it is abandoned and its Lot returns to vacant"* against *"an abandoned
Building raises its neighbours' failure pressure"* and *"the specific accumulated condition is **retained
on the Building** and shown in the inspector"* — and it decided which reading stands: ***abandonment
empties a Building and leaves it standing on its Lot***, because three other mechanisms depend on the
shell being there. 🔴 **The build implements the reading that was refused.** `ZoneRuleEngine.Condemn`
calls `World.DestroyBuilding`, which frees everything, and it has done so since before the ADR was
written. **So the shell that `adr/0091`, `01 §6` and `02 §5.9` all presume exists, does not.**

---

## The named risk, as `06` states it

**That standing abandoned stock has no sink**, which is
[`adr/0006`](../docs/adr/0006-no-collection-grows-with-elapsed-time.md)-class (`06`:101).

⚠ **The risk is stated against a world this milestone CREATES.** Today nothing stands abandoned, so
nothing accumulates and the risk is unreachable — it arrives the moment task 2 lands and not before.
***A milestone whose named risk is created by its own first half is a milestone that must ship its sink
in the same breath***, which is why decision **1** is the one this turns on and why the sink is not
deferred to a task 9 nobody reaches.

---

## What the build already holds — surveyed 2026-08-25

**Read from the symbols, not from prose about them**
([`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)).

| | What | Where |
|---|---|---|
| ✅ | **Failure Pressure as a duration**, per subject, cross-multiplied against a threshold | `RuleInstanceTable.StarvedSince`, `ZoneRuleEngine.Worst` |
| ✅ | **Two subjects**, premises and tenant, on one `condemn_after` — `adr/0141` | `ZoneRuleEngine.Condemn`, `:319` |
| ✅ | **The condemnation trail** — tick, Lot, kind and the reported condition, written **before** the demolition because the demolition frees what it would copy | `CondemnationTrailTable.Record` |
| ✅ | **Eviction with capital intact** — `adr/0054` | `World.DestroyBuilding` |
| 🔴 | **ONE threshold, where `CONTEXT.md` specifies two** | `KindDefinition.CondemnAfter`, `Ruleset.cs:451` |
| 🔴 | **ONE Failure Pressure source of the three `CONTEXT.md` names** | `RuleEngine.Stop`, `:608`–`:627` |
| 🔴 | **No standing abandoned state at all** — the verdict goes straight to `DestroyBuilding` | `ZoneRuleEngine.Condemn:364` |
| 🔴 | **No `Demolish` verb.** `adr/0091` decided it; `01 §2`'s list is still five | — |
| 🔴 | **No clearance Policy**, the `Govern` wholesale sibling `adr/0091`:49 names | — |

### The two findings that size this milestone

🔴 **F1 — `RuleEngine.Stop` starts the pressure clock on `Blocking.Supply` ALONE, and CLEARS it for
every other reason.** Read it: the `else if` arms only on `Supply`, and the first branch zeroes
`StarvedSince` on anything else. `CONTEXT.md`:296 names **three** sources — *Trips to or from it
failing*, *its Rules repeatedly reaching a reporting terminal*, and *local conditions falling below its
Occupants' tolerance* — and **only the second is built**. ⚠ **This is not a defect**: nothing decided
against the other two, so under
[`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) they are ***unbuilt***
and the answer to *should the one source compensate* is **build the others**. They are decision **3**.

🔴 **F2 — THE DECLINE STAGE BETWEEN THE TWO THRESHOLDS IS ENTIRELY UNBUILT, AND IT IS TWO MECHANISMS
RATHER THAN ONE.** `CONTEXT.md`:296: *"Past a threshold it **loses occupancy and quality**; past a
further one it is **abandoned**."* The build has the second threshold only, and *loses occupancy* and
*loses quality* are separate things — the first has a mechanism to borrow (`adr/0068`'s over-capacity
eviction) and ⚠ **the second has no referent in the build at all**: there is no quality column, no
quality term in desirability, and no document that says what quality is. **Decision 2.**

---

## Open decisions this milestone owes

### 1. Is a player a sink? Typed *arguable* — 🔴 **and it is the decision this milestone turns on**

[`adr/0006`](../docs/adr/0006-no-collection-grows-with-elapsed-time.md) is *no collection grows with
elapsed time*. `adr/0091` gives abandoned stock exactly two sinks and **both are player acts** — the
`Demolish` verb, and the `Govern` clearance programme. ***A player is not a sink***: a city left alone
accumulates abandoned shells for as long as it runs, and *the player will clear them* is a hope about
a human rather than a property of the simulation.

**Two readings, and scoping could not settle between them:**

- **(a) It is already bounded, and the risk is misstated.** Abandoned Buildings stand on **Lots**, Lots
  are finite and fixed by the Road Graph, so the collection is bounded by the Lot count and cannot grow
  with *elapsed time* — only with *city size*. On this reading `adr/0006` is satisfied by construction
  and nothing further is owed. ⚠ **Check it against the actual invariant before relying on it**: the
  standing shells are bounded, but the **`CondemnationTrailTable` rows, the Evidence rows and the
  Unplaced Pool arrivals** each abandonment produces may not be.
- **(b) It needs a decay sink, and that is a design change.** A shell that stands for ever is also a
  shell whose contagion term (decision **4**) suppresses its neighbours for ever, which is a city that
  cannot recover without the player. On this reading something must remove a shell **without** a player
  — collapse after a duration, or a rebuild that overwrites it.

⚠ **Type it before arguing it** ([`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)):
*(a)*'s bound is **measurable** — run a long headless city with abandonment on and count standing
shells, trail rows and Pool arrivals against Ticks. ***So the first move on this decision is a
measurement and not a sitting***, and it cannot be taken until task 2 exists to produce the world.

### 2. What is *quality*, and does it ship here? Typed *arguable* — **recommendation: no**

`CONTEXT.md`:296's first threshold loses *occupancy and quality*. Occupancy has a mechanism to borrow.
**Quality has no column, no term and no definition**, and inventing one here would put a hash-bearing
number under a word no ADR defines. **Recommend shipping the first threshold as occupancy loss only**
and routing *quality* to [`0002`](0002-open-questions.md) §A as *undesigned*, named rather than
silently dropped.

### 3. Which of the two missing Failure Pressure sources ship here? Typed *arguable*

- **Trips failing** — the record exists (`adr/0097` counts a reach failure on the Citizen), so this is
  reachable. ⚠ **Check whether it is a Building-addressable signal**: pressure is the *Building's*, and
  a Citizen's failed reach has to be attributed back to premises.
- **Conditions below tolerance** — 🔴 **tolerance is milestone 16's**, the residential choice model,
  which `06`:100 calls *"the most leaned-on absence in the corpus."* **This one cannot ship here** and
  saying so is the decision.

### 4. Does abandonment contagion ship here? Typed *arguable* — **and it has a named blocker**

`02 §5.9` wants an abandoned Building to raise its neighbours' pressure. `adr/0091`:59 already found
why it cannot: ***"bare ground has no dereliction term in the desirability composition, so the mechanism
the section calls deliberate cannot occur."*** Task 2 supplies the carrier — a shell is no longer bare
ground — **but the desirability term itself is a new weight and therefore a hash-bearing number owed a
named ratifier** under [`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md).

### 5. The clearance price — ⚠ **already decided as *unset*, and it is a gap rather than a debt**

`adr/0091`:69 settles the shape and refuses the number: it is `f(land value at the Lot, what stands on
it)`, hash-bearing, and **the composition is the decision**. It enters `plans/0002` §D **unset**, with
its ratifier named as *the first play session in which a player clears something*. ⚠ **It is the second
thing in the corpus blocked on the land value target**, which is a named hole in `MapLayers`. **Nothing
in this milestone requires the price to exist** — the `Govern` clearance programme needs no compensation
term at all, because abandoned stock has nobody left in it to compensate.

---

## Tasks — ⚠ **PROVISIONAL until decision 1**

1. **The second threshold stops destroying** — `ZoneRuleEngine.Condemn`'s premises verdict empties the
   Building and leaves the row standing on its Lot, instead of calling `DestroyBuilding`. Occupants are
   still evicted into the Pool with capital intact (`adr/0054`), the trail is still written. 🔴 **This
   is the whole risk arriving**: from this task onward the city accumulates shells. **Moves the State
   Hash.**
2. **The abandoned state, read rather than recorded if that is possible.** ⚠ **Try hard to derive it
   before adding a column** — `CONTEXT.md` → Derelict is emphatic that its own state is derived and says
   why (`adr/0057`), and a state that is *empty, standing, and was condemned* may be answerable from the
   Lot, the occupant list and the trail. **If a column is unavoidable it is `(saved AND hashed)`** and
   this task moves the hash a second time.
3. **The first threshold** — occupancy loss, per decision 2, quality routed out rather than invented.
4. **`Demolish`, the sixth verb** — `01 §2`'s list becomes six, compulsory purchase paid from the land
   value layer to whoever is displaced. ⚠ **Blocked on decision 5's composition**, so it may ship as the
   verb over *abandoned* stock only, where no compensation is owed.
5. **The `Govern` clearance programme** — the wholesale sibling, treasury-funded, no compensation term.
   ⚠ **This is the sink `adr/0006` is asking after**, on reading (b), and it is a Policy rather than a
   verb.
6. **Trips-failing as a second Failure Pressure source**, per decision 3.
7. **The contagion term**, per decision 4 — with its `plans/0002` §D row written on the day.
8. **Something to look at** — a runner mode showing a district declining, standing empty, and being
   cleared. ⚠ **The third clause is the one that gets dropped**, and it is the only one that shows the
   sink working.
9. **The long acceptance run** — 100k+ Ticks with abandonment on, and **decision 1's measurement taken
   here**: standing shells, trail rows and Pool arrivals against Ticks, each checked for a trend.

---

## What this milestone must not do

- ⚠ **It must not call any of this *derelict*.** `CONTEXT.md`:313 — dereliction is what a **Ruleset
  edit** does to a Building (`Kind == 0`, derived, `adr/0057`), abandonment is what the **city** does,
  and the entry says outright that they *"share none of its machinery."* ***Scoping made this mistake on
  its first pass and it is recorded here so the next reader does not.***
- **It must not add a demand scalar**, and *quality* is where one would enter.
- **It must not build tolerance** — that is milestone 16's and taking it here would scope-creep the
  most leaned-on absence in the corpus into a decline milestone.
- **It must not price compulsory purchase** by choosing a composition to unblock task 4. `adr/0091`
  refused that number deliberately.

---

## Definition of done

`CLAUDE.md`'s cumulative list, plus:

- **A Building that fails stands empty and is visible as such**, and the condition that killed it is
  still readable off it — which is `02 §5.9`'s *retained on the Building* clause, the one the current
  build cannot satisfy because the row is gone.
- **Decision 1 is settled by the measurement in task 9, not by argument**, and whichever reading wins is
  written into `adr/0006`'s neighbourhood rather than left in this plan.
- **The long run shows no collection and no magnitude trending** — with standing shells, trail rows and
  Pool arrivals each named and counted separately, because they have different bounds.

---

## What scoping found

**F1** and **F2** are above, where they size the milestone.

**F3 — 🔴 THE BUILD CONTRADICTS A SETTLED ADR AND NO LEDGER RECORDS IT.** `adr/0091` chose `02 §5.9`'s
second reading on 2026-08-16 and noted *"the build implemented the first reading."* **That sentence is
the only place the contradiction is written down.** It is not in [`0012`](0012-corpus-audit.md), not in
[`0003`](0003-build-plan.md)'s queue, and not in `02 §5.9` itself, which still carries both readings
twelve lines apart. ⚠ ***A decision recorded only in the ADR that made it is invisible to every document
that would route work at it*** — which is why this milestone reads as a feature and is a repair.
**Owed to [`0012`](0012-corpus-audit.md)** whether or not this milestone runs.

**F4 — ⚠ SCOPING CONFLATED DERELICTION WITH ABANDONMENT ON ITS FIRST PASS**, in exactly the way
`CONTEXT.md`:315 says the two must not be — and it did so while holding the roadmap row, which cites
`CONTEXT.md` → Derelict directly. ***The citation is what caused it***: the row's own reference list
names Derelict beside `adr/0053` and `adr/0091`, and reading the row rather than the entry makes the two
look like one subject. **Recorded because the same reference list will do it to the next reader.**

**F5 — ✅ THE `adr/0166` DECLINE DECISION IS *NOT* THIS MILESTONE'S, AND THE SUBJECT SPLIT IS WHY.**
[`0044`](0044-the-purchase-and-the-provider-that-answers-it.md) **F1** found that a money-consuming
decline Rule needs a counterparty — treasury or market — and that the choice is unmade. It was offered
to this milestone as *"probably yours rather than task 7's."* ⚠ **It is not.** `adr/0141` splits Failure
Pressure into two subjects: a **tenant's** pressure ends the tenancy, a **premises'** condemns the
Building. `adr/0166`'s money Rule is a **Business's** decline and belongs to milestone 26's task 7;
this milestone owns the **premises'**, which is driven by a duration and needs no money term at all.
⚠ **And it must not acquire one**: Upkeep is UNPLACED
([`adr/0136`](../docs/adr/0136-upkeep-has-three-blockers-landing-at-three-times-so-it-has-a-queue-and-not-a-milestone.md)),
so a recurring cost on the premises would be reaching for a mechanism three documents have already
declined to place.

---

## Where this sits

`06` row order puts 17 after 16 and before 18. **It is being taken out of order**, which is what created
decision **3**'s second half — tolerance is 16's, so one of the three Failure Pressure sources is
unreachable from here and the milestone ships two of three by construction. ⚠ **That is a smaller
version of milestone 18's problem and it does not have 18's answer**: 18 taken early had *no* consumer
and collapsed to an argument; 17 taken early has two of its three sources and a complete decline chain
without the third. ***Partial is not the same as empty.***
