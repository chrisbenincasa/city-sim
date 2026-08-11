# 0012 — The corpus audit

**A one-off sweep of every status-bearing document against the state of the code**, run after slice 7
task 8, because the board was found asserting that task 8 was hot reload — which it is not, and never
was. That single error was cheap to fix and expensive to explain: nothing had changed to make the
board wrong, so nothing could have caught it going wrong.

This document is the **record of what the sweep found**, not a plan. Items are struck as they are
discharged. It is a debt ledger with a closing condition: when everything here is struck, delete it.

**The design documents are paid and agree with the code.** `02`, `04`, `05`, `CONTEXT.md` and
`adr/0033` were corrected inline in the sitting that found them, together with the two ADRs the
citation check caught. **What remains open is almost entirely `plans/`** — `0002`'s live-status half
and `0003`'s three disagreeing tables — which is *Cause 1*, and which the board restructure has
already shown how to fix: give the fact one home and point at it. Nothing left in this ledger
describes the simulation wrongly.

**One item is not like the others and arrived after the sweep closed:** a `const` in `src/` that
`adr/0015` says belongs in the Ruleset. It is a **code** defect rather than a document correction, and
it is here because it exposes a bias in this sweep's method — every other item fixes a document,
because *documents against the code* was only ever run in one direction. See *A constant `adr/0015`
enumerates lives in the binary*.

---

## The diagnosis, which is two things and not one

The sweep expected one failure mode and found two. They want different answers, and conflating them
is why *"tidy the documents"* has never worked as an instruction.

### Cause 1 — status is stored in three places that disagree

**The same six facts live in `0003`'s ledger, `0003`'s gate board, and `0002`'s Readiness table.**
All three are hand-maintained, none derives from the others, and at the time of the sweep all three
disagreed. Any gate movement needs three edits and gets between one and two.

**The predictor is measurable and was measured.** Every document that stores per-slice status had
drifted; every document that stores none had not.

| Document | Stores status? | Sweep result |
|---|---|---|
| [`06-roadmap.md`](../docs/06-roadmap.md) | no — `adr/0042` made it cite | **clean**, one soft item |
| [`PROCESS.md`](../PROCESS.md) | no | **clean** but for two counts |
| [`0000-board.md`](0000-board.md) | yes, heavily | badly drifted |
| [`0002`](0002-open-questions.md) | yes, in half of it | 20+ items |
| [`0003`](0003-build-plan.md) | yes | 10 items |
| `CLAUDE.md` | yes | **self-contradictory twice** |

`06` is the control. It was *deliberately* stripped of everything but names and risks by session nine
and `adr/0042`, and it is the only large document that came back clean. That is not a coincidence and
it is the argument for the restructure.

**`0002` is carrying two incompatible roles.** Its session history is a record and should be
immutable; its live-status half — the header, *Readiness*, *Resume here* — holds every stale item in
it bar two. The costliest is `0002:541`, the file's own **resume-here pointer**, which still directs
a reader to open `adr/0015`. Session A closed `adr/0015` and produced `adr/0048`.

> **Cause 1 has a second form that no restructure reaches, and session N found it three times in one
> day.** The form above is *a fact with two copies, one of which drifted*. The other is **a fact with no
> copy at all, re-derived wrongly from the shape of its absence** — and it is worse, because a missing
> copy does not look like a gap. It looks like the truth.
>
> | Sighting | The fact with no copy | What was re-derived in its place |
> |---|---|---|
> | Tasks 3–4 | `RulesetLoader` has refused duplicate `(kind, Resource)` Bins since slice 7 — **the one guard in that loader with no test** | `adr/0064` recorded it as a live defect. *A guard with no test is invisible to every future reader, including the one about to decide it does not exist* |
> | Task 2 | `adr/0025` says *"Density says how many Occupants a Lot may carry"*, under a heading reading **Capacity, not quality** | `0002` §C and `plans/0018` both disqualified that ADR as *"adjacent and does not cover it"*, on a distinction it does not draw |
> | Task 2 | `02 §5.2` **step 2, Household placement, is implemented nowhere** — `World.Place` has exactly one caller | Two ledger entries concluded a **number** settles the five-sixths-homeless equilibrium. §B said so outright. No number does; the mechanism does |
>
> **The third is the dangerous one and it generalises past documents.** An unbuilt mechanism reads as a
> design constraint, and the constraint then generates positions — the sitting spent its first exchanges
> asking whether construction should fill a Building to capacity, a question that exists **only** because
> placement does not. *The code was allowed to be wrong about the design*, which is the exact mirror of
> the first row, where an ADR was wrong about the code. **Both are this cause; neither is drift.**
>
> **Now a rule rather than a habit:**
> [`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) — *an unbuilt
> mechanism is not a design constraint*. Name the mechanism and classify the absence as **unbuilt**,
> **undesigned** or **refused**; only the third is evidence. It is the third sibling of `adr/0043`
> (claims) and `adr/0052` (numbers), and it governs the thing neither does: **absences**.
>
> The mechanical check in [*What the mechanical check should be*](#what-the-mechanical-check-should-be)
> catches none of these, and should not be extended to try. What catches them is the habit tasks 3–5
> arrived at independently and `adr/0070` now states: **before recording that something does not exist,
> name the file you looked in.**

### Cause 2 — ADRs issue writes to other documents and the writes do not all land

This is the one restructuring cannot fix, and it is the more expensive of the two.

An ADR's *Consequences* routinely contain instructions addressed elsewhere — *"`CONTEXT` → Bin Rule
gains the ladder"*, *"`02 §4.1`'s worked example is corrected on two links"*. **Twelve such
instructions were verified as carried out.** The ones below were not, and the pattern in the misses
is that a correction reaches the document being *argued about* and not the two others describing the
same mechanism.

The failure has a cheap mechanical tell that nobody was looking for: **`adr/0049` is cited by no
document at all.** Not `CONTEXT.md`, not `docs/02`–`06`, not `plans/`, not `src/`. A decision with no
inbound citation is a decision that governs nothing, and that is greppable.

---

## Fixed in the sitting that found them

Unambiguous factual errors, no judgement required.

- [x] `CLAUDE.md` — **"43 ADRs"** against its own line 106's "49 decision records". Self-contradictory
- [x] `CLAUDE.md` — **"Five claims measured false"** against its own line 32's "the **sixth** claim".
      Self-contradictory, and `PROCESS.md` already said six
- [x] `CLAUDE.md` — slice 7 "tasks 1–7 of 10", now 1–8
- [x] `CLAUDE.md` — `spike-results.md` described as *"Empty until S4 runs"*; it is 4,329 lines
- [x] `CLAUDE.md` — `plans/0004`–`0009`, which omits `0010` and `0011`; `0011` owns the live slice
- [x] `CLAUDE.md` — *"Lints 4, 5 and 6 need machinery that does not exist yet"*; lint 5 has
      `ReplayTests` and the golden baseline
- [x] `CLAUDE.md` — *"Slices 7 onward are gated"*; 7, 8 and 10 are cleared and 9 is the only red gate
- [x] `PROCESS.md` — "forty-eight ADRs" and "six plan documents"; 49 and twelve
- [x] `CONTEXT.md` — **`adr/0047` applied to one of three places.** `:43` correctly says the District
      *"is not the granularity of the travel-time matrix"*; `:45` still made matrix granularity the
      reason Districts are Cell-aligned, and `:110` stated the removed role as positive fact citing
      `adr/0020`. The entry contradicted itself
- [x] `adr/0048` and `adr/0015` — both said the loader runs **three** refusals. Five
- [x] `adr/0045` — authored the reporting terminal and did not record that task 8 made it a **refusal**

---

## Filed — needs judgement, or a task that has not run

### ~~`docs/02-simulation-model.md`~~ — **PAID**

The Rule engine's owning document had moved twelve places behind the code. All are corrected inline,
and the documents that repeated the same claims are corrected with them, because a correction that
reaches one of three places is *Cause 2* rather than a fix.

- [x] **`02:271` — "Bins live in a `ResourceMap`".** They are rows in the Bin table, and a Building's
      Bins are an **intrusive index list** walked linearly. The paragraph now says so and keeps the
      hash-map ban that was its real content. **`05 §3` repeated the claim and is corrected too**
- [x] **The pre-`adr/0045` subscription model, in two places outside `§4.1`.** `bake_bread` was said to
      register interest *"in flour arriving in the Pool"*; it subscribes on its own `local` Bin, and
      the Pool rung is a link that refills that Bin
- [x] **`adr/0049` and `adr/0038`**, propagated into the sections that own them
- [x] The Bin shape — **signed level**, and a **money Bin is unbounded** with an authored capacity
      refused
- [x] **Two wait lists per Bin, not one**, with the deadlock argument for why, and a **Withdraw drains**
- [x] The shortfall formula is over the **net Bin delta**, not the term's amount
- [x] The chain law: relieving includes **drawing from** the head's Bin, and **a chain that does not end
      in a terminal is refused** — with the check ordered before the relieving check, and why
- [x] `pool` and `global` are **named holes that throw and are not refused at load**, so the worked
      chain loads clean and throws when reached, and `§1`'s contention example is not yet reachable
- [x] The Readout examples are marked as the intended shape; **the declared set has one member**
- [x] **A link is not a Rule Instance of its own**, and a rescue re-arms on the **link's** rate
- [x] The two counters are task 9 and do not exist; the evaluation counter counts due Instances —
      **and task 9 has since replaced that sentence with the measurement it was waiting for**
- [x] `02:449`'s *"never `global`"* reconciled: no actor's **balance** is `global`, and `global` names
      the **treasury** as the far end of a transfer — which is the only spelling the loader accepts

**Also corrected, being the same claims elsewhere:** `CONTEXT.md`'s Readout enumeration and its
**inverted** Readout↔Evidence bound; `adr/0033`'s stale Readout list and the same inverted bound;
`05 §4`'s rule 5, which said replay equivalence was *owed* by a slice that delivered it; and `04 §2`,
which described integer Bins without saying money's has no ceiling.

### `plans/0002-open-questions.md` — **mostly PAID, by restructure rather than by edit**

**The file was reorganised by state instead of by session.** Every entry below was in its
**live-status half**, and the diagnosis that closed most of them at once is that the live-status half
should not have existed: `0002` was organised by *which session raised a question*, so answering *what
is open* meant reading fifteen session sections and diffing them. Nobody did, and the board — a
self-declared *view* — silently became the ledger, carrying **63** open items while the file named
*open questions* carried **none**. That is Cause 1 with the status column swapped for the questions
column, and it is the largest single instance of it in the corpus.

`0002` now opens with a ledger grouped by what is blocked, every entry typed *measurable* or
*arguable* per `adr/0043`. Everything that was there before is retained verbatim beneath it under a
banner reading **not maintained, not status**. Correcting a stale pointer inside an archive would have
been the wrong repair — the pointer is not wrong about the past, it was only ever wrong as status.

- [x] ~~`0002:541` — the resume-here pointer, still opening `adr/0015`~~ **archived**
- [x] ~~`0002:7`, `:485`, `:577` — "the last four items of the Phase 1 gate"~~ **archived**
- [x] ~~`0002:9`, `:11`, `:432` — "buildable now" for things that are built~~ **archived**
- [x] ~~`0002:602`, `:604`, `:1496`, `:1436` — four ledger entries settled elsewhere and never
      struck~~ **archived**; the live ones were carried into the ledger and the settled ones were not
- [x] ~~`0002:323` vs `:578` — `04 §7` is stale "twice over" and "three times over"~~ **archived**;
      the ledger states it once
- [x] ~~**`0002` has no *Unratified numbers* section for slices 5, 6 or 7**~~ **PAID** — the ledger's
      section D is that section, and it is now cumulative rather than per-slice, which is what stopped
      it being written three times running. All three concrete items named here are in it: the Cell's
      metre reading, the industrial pollution **kernel radius**, and the sizing ratios
- [ ] `0002:457-462` — the **Readiness table**, five of six rows stale. Now archived, but **`0003:5`
      still points readers at it as the readiness derivation**, so the live defect is `0003`'s pointer
      rather than the table
- [x] ~~`0002` **coverage map** — contradicts itself (`§5` listed as both closed and unargued) and lists
      `§8`/`§10` as never grilled when session eight closed them. **Promote it out of the archive and
      into the ledger once it is corrected**~~ **DONE 2026-08-10 — it is now [`0002`](0002-open-questions.md) §F**,
      and correcting it found **a third defect larger than the two filed**: it **stopped at `adr/0043`**,
      so **twenty-two ADRs — `0038` through `0059` — did not appear in it at all.** That is every
      decision the *building* has produced, and it is the half of the corpus this audit's own *Cause 1*
      predicts will drift, because the map stores status. **A coverage map blind to two thirds of a
      year's output is worse than none, because it reads as complete.** The blanket rows are also split
      now, per `adr/0043`. The superseded version is retained in the archive, bannered, because §F
      states its defects against that wording. **The finding that outlives the fix**: the old map's
      closing summary — *we have argued what the city does thoroughly and how it is built not at all* —
      is **no longer true and nothing had noticed**, because thirty-one decisions have since come out of
      building rather than arguing, concentrated in exactly the layer it called untouched
- [ ] **Nine slice plans carry a *Decisions owed by this slice* section and nothing reconciles them
      against the ledger.** `0003:254` already makes it a per-slice definition of done that every
      unratified number goes into `0002` before the slice closes, and **slices 5, 6 and 7 all closed
      tasks without doing it** — which is why the mechanism looked absent when it was merely unexecuted.
      The rule needs no rewriting; it needs a check. Same shape as Cause 2
- [ ] **The design documents' own *Open questions* sections have never been reconciled against the
      ledger.** `0002`'s old preamble said they *"remain authoritative for their own areas"*, which is
      the drift generator stated as policy — five authorities and an index. The rule is now that a
      document may **restate** an entry and may never **hold** one the ledger lacks. Nobody has run the
      pass. `04 §8` is the known instance: it listed a question `adr/0024` closed **in the document the
      ADR is about**

- [ ] **`0002`'s *path source* row is closed by [`adr/0047`](../docs/adr/0047-routing-never-keys-on-the-district.md)
      and still sits in the live ledger as session **M**'s — NEW, found by S2 R6.** The row reads *"a
      maintained table is wrong structurally — 16.58% detour uniform, 149.73% local, and it does not
      move across a storm deleting 1,021 Segments"*, and `adr/0047`'s own table carries **those three
      figures verbatim** and concludes *"the table was never a path source. It was the fallback if the
      cache did not work."* An ADR beats the ledger, so this is a correction and not a question.
      **What survives of M is the invalidation contract**, which is a narrower gate than the board's
      spike order reads it as — R6's two owned questions, the **key** and the **eviction policy**, are
      not gated on it. **This is `0000`'s own diagnostic — *a gate whose stated reason covers only
      part of what it blocks* — landing on a spike row rather than a slice row**, which is the third
      instance after `adr/0003`'s split debt and `06`'s ordering. Fix: strike the row from `0002`'s
      ledger, and narrow `0000`'s *R6 gated on session M* to the invalidation half

- [ ] **`0000` and `docs/spike-results.md` both quote R5.5's *16 against 416* — NEW, found by S2 R6.**
      The 16 sums **four** cache rungs where the 416 is **one** control rung, so the comparison never
      had a denominator. It cost R5.5 no conclusion, both sides being about 1% of the control, and it
      is struck rather than argued: `spike-results` → *S2 R6.0* now publishes the per-rung table

### `plans/0003-build-plan.md`

- [x] `0003:164` — ~~states slice 7's owed counters **twice inside one table cell**, and calls the
      refusals three~~ **PAID by slice 7 task 9.** Both statements of the owed counters are struck in
      place rather than deleted, because each cell records a different session's reasoning and the
      duplication is the record; the refusal count now says three at the gate and **five in the
      build**, naming the two that arrived while it was written
- [ ] `0003:105`, `:107`, `:108` — slice 5 carries no status, and the Phase 1 gate is written as
      future. **The slice 7 half is paid**: its row now carries 9 of 10 and what each task delivered
- [x] `0003:111` — slice 10 marked 🔴 in the *Gate* column when its gate is cleared; it waits on
      slice 7 finishing. `0003:167` states it correctly, so the file disagrees with itself.
      **STRUCK 2026-08-08, and it had just cost something.** Planning the shortest path to a running
      city, the red mark was read as a gate and produced the recommendation *task 10a → slice 9 →
      slice 10*, routing the critical path through **session C** — the one genuinely red gate — when
      slice 9 is not on the path at all. **A stale status mark is not cosmetic**: this one invented a
      design session's worth of work and put it in front of the thing the project most needs
- [ ] `0003:184-192` — decisions-owed item 1 settled by `adr/0038`; the only one of four unstruck
- [ ] `0003:266` — *"S0 is slice 11"*. There is no slice 11, as the next paragraph says

### The ADR corpus

- [x] **`adr/0049` has zero inbound citations.** *Paid*, along with **`adr/0038`**, which the check
      found on its first tightened run and which nobody had noticed at all
- [x] **`adr/0015`'s world-creation constants were missing the kernel radius**, which `adr/0044`
      instructed it to add — in a paragraph declaring the category *"a named category with an
      **enumerated membership**"*. An unenumerated member is that paragraph's own stated failure
- [x] `adr/0050:88` — asked as a revisit trigger a question closed in three other documents. *Struck, and the ADR records that it survived the re-reading it asked for*
- [x] `adr/0033:40` — listed four Readouts, three refused by the loader and one (`experience`)
      **struck** by `02 §4.1`; and still states the Readout↔Evidence bound in the direction
      `02 §4.1` **inverted** and called an error
- [x] `CONTEXT.md:383` — the same inverted bound, and an enumeration four-fifths of which is refused
- [x] `adr/0048` put the Ruleset loader in `Borough.Formats`; **`05 §1`'s project table did not know**
- [x] `adr/0047:84` — *"a failed Trip must demote the option that produced it… owed to `adr/0017`"*.
      **Recorded in `adr/0017`, not invented.** The defect is now stated where the creditor can see
      it; which mechanism answers it — demotion, cooldown, or Habit's weight moving — is a design
      decision and belongs to a session
- [x] `adr/0023:85` — claimed to absorb `04 §8` question 7. **Question 7 now records that the
      *mechanism* is settled and only the tuning is open**, which is a playtest question
- [x] `adr/0045:78` — *"slice 7 is therefore gated on `adr/0015`"*; annotated with the clearance and the corrected refusal count

### `adr/0007` still states two things the document it governs has since corrected

**Found by session D's typing pass (task 0), which is the first time the ADR was read against `03`
rather than cited by it.** Both are *Cause 2* in its purest form — a correction reached the document
being argued about and not the ADR that governs it, which is the inverse of the direction the sweep
expected and therefore the direction it never looked in.

- [ ] **`adr/0007`'s eighth consequence carries invariant 6 inverted.** It reads *"Where a segment is
      microscopically simulated, observed travel times must match VDF predictions within tolerance"* —
      and `03 §4` invariant 6 records that binding exactly this to **Microscopic** segments *"inverted
      it"*, because §3.2's whole justification for simulating them is that the VDF **is wrong there**.
      The document says so in a quoted block; the ADR was never amended. **Divergence on a stressed
      Segment is the product; divergence on an unstressed one is the defect** — and the ADR currently
      asserts the opposite of its own design's success condition
- [ ] **`adr/0007`'s third bullet still describes the counter `adr/0041` deleted.** *"The trigger
      consumes a count, not a model. `in_flight` is exact — incremented on departure, decremented on
      arrival"* is the `in_flight[origin_District][dest_District]` scheme, and `03 §3.3` carries a
      superseding banner over the same sentence while its governing ADR carries none. S2 R2 found the
      aggregate scheme is not late but **wrong about which Segments** — 130.21% `v/c` direct against
      28.09% aggregate on the watched arc — so the banner is not cosmetic

### The routing budget counts an event that stopped being the expensive one

**Raised by S2 R6.3 and owed to two documents at once.** Both size routing by **Trip starts**:

- [ ] `plans/0013:82` — the *Routing* row is denominated in *16 Trip starts — guessed*. Under static
      Habit a Trip start is a **lookup**, and R6.3 prices the whole formation bill at **0.316 ms**, or
      **0.24% of routing's total**, at R8's own rung. The row is measuring the cheap half. It wants a
      **diversions per Tick** denominator instead — R8.3 measured 1,269.51 at 40,000 Travellers.
      **Not edited in place**: `0013` was being written by another session when this was found, so the
      correction is filed here rather than applied underneath it
- [ ] `docs/spike-results.md` R3 — the tripwire *"routing fits while fewer than 85 Trips start per
      Tick"* is not wrong, it is **no longer the binding constraint**, and nothing near it says so.
      R6.3 is now adjacent in the same file; a forward reference from R3 is what closes it

**The general defect is worth more than either fix.** `adr/0047` deleted the next-hop table on the ADR
track, and in doing so **invalidated a denominator on the spike track** — a diversion stopped having a
cheap path source, which is what made the Trip-start count adequate. Nothing in the process noticed,
because the board's model of the three tracks is that they *do not contend*. They do not contend for
**files**. They plainly do contend for **conclusions**.

### `05 §3` describes the shed's invalidation the way `CONTEXT.md` used to

- [ ] `05 §3:136` — *"cached Parking Shed membership per Building … **invalidated by the Road Graph
      Epoch**"* is owed the correction `CONTEXT.md` → Epoch already took. **The phrase says when the
      rebuild is paid, not how much survives**, and S2 R5.6 measured what it costs under one counter:
      all **159,825** sheds at **255.560 ms — 1,638.20% of a Tick** — and `adr/0009` pays it *on
      arrival*, so it is a stampede across every arriving vehicle rather than one stall. The rung is
      **per-Segment, witnessed by the walk paths to the Bins the shed kept**
- [ ] `plans/0010` R5.6 — its own prose predicts *"a per-cluster Epoch fits it far better than it fits
      routes."* **Measured false**: per-cluster is the worst surviving rung. Struck rather than
      deleted, per the corpus's practice, because the prediction is why the section was run

### The Lot subdivider is described in full by `02` and built by no milestone

**Raised by slice 10's first sitting**, which went looking for the shape of its `zone` verb and found
that the verb cannot have the shape `02` gives it until Phase 2.

- [ ] `docs/06-roadmap.md` — the *Mechanisms with no milestone* table does not list **Lot
      subdivision**, and milestone **5a** is *"Road Graph and Streets"*, whose named risk is geometry
      leaking into the simulation. Neither names the thing that turns zoned land into parcels.
      `02 §2.2` specifies it — subdivision rules, depth and width varying by density band,
      re-subdivision on network change preserving existing Buildings — and `adr/0014` and `adr/0035`
      both reason *from* its frontage rule, so it is settled design with no builder. It belongs either
      in 5a explicitly or in the no-milestone table
- [ ] `docs/02-simulation-model.md §2.2` — *"Lots are **generated, not painted**"* is true of the
      design and **false of the build, in every world this project has ever run**. Every Lot in the
      tree is painted: one per `CommandKind.Zone`, or `SyntheticCity`'s grid. Slice 10 made this
      visible and deliberately did not fix it (`0014` → *The second collision*), because the fix is
      5a's. The section wants the same treatment `rulesets/minimal.toml` gave its own emptiness — a
      sentence saying the generator does not exist yet — rather than a silent gap between a design
      document and every number taken under it

**The general shape is the one `0013` published about unit costs.** A precondition stated in a design
document is a hypothesis about the build until something enforces it. Nothing has ever refused a Lot
for want of frontage, so *"every Building is on the Road Graph by construction"* — which `CONTEXT` →
Frontage leans on to delete the utility network entirely — is currently true by there being no Road
Graph rather than by construction.

### `02 §5.2` step 2 is Household placement, and of that section's six steps only step 5 is built

**Raised by session N task 2**, and it is the *Lot subdivider* entry above wearing a different
mechanism — settled design, specified in full, owned by nobody. **Now decided** by
[`adr/0069`](../docs/adr/0069-placement-is-a-mechanism-of-its-own-and-construction-houses-nobody.md),
so what is filed here is the paperwork that decision owes, not the question.

- [x] **PAID in the sitting that found it**, on task 1's precedent for `§1.1` and `§4.1`. `§5.2` gained a
      *which of these six steps exist* note and the omission's cost; `§5.6`'s sentence was amended in
      place with the superseded wording recorded; `§1.1`'s phase table gained placement and the note
      that its **position** is the decision. Left below as written, because what was owed is the
      argument for the edit.
- [x] `docs/02-simulation-model.md §5.6` — the note reading *"creation drains the signal that authorised
      it, so no Ruleset can build past its demand however wide its sample"* is **superseded**. Under
      `adr/0069` construction houses nobody, so the self-limiting property comes from the **ordering** —
      placement runs ahead of the Zone Rules' sample, so a Household still in the Pool is one the
      standing stock could not house. Amend in place with the superseded wording recorded, on task 1's
      precedent for `§1.1` and `§4.1`. **The replacement is stronger than what it replaces** and the note
      should say so: today's predicate reads a Pool that construction drains one Household at a time, so
      a wide sample can build ahead of demand by up to the sample size within a trigger
- [x] `docs/02-simulation-model.md §5.2` — the six-step loop wants the same treatment
      `rulesets/minimal.toml` gave its own emptiness: a sentence saying **which steps exist**. Step 5
      does; steps 1, 3, 4 and 6 do not; step 2 is decided and unbuilt. Without it the section reads as a
      description of the build, and this sweep has now caught that misreading **twice** — once in `§2.2`
      above and once here, where it cost two ledger entries and a wrong subject
- [ ] `docs/06-roadmap.md` — the *Mechanisms with no milestone* table does not list **Household
      placement**. Milestone **9a** is *Households, the Unplaced Pool and Departure* and is the obvious
      home, but 9a as written is about where Households come *from*, and this is about where they go
- [ ] `docs/adr/0025-density-is-a-cap-and-it-trades-land-for-materials.md` — not wrong, and it wants a
      forward pointer to `adr/0068` all the same. *"Density says how many Occupants a Lot may carry"* is
      now discharged **through the permitted kind set** rather than by a mechanism of its own, and this
      entry exists because a reader who could not find that sentence is exactly what happened

### A constant `adr/0015` enumerates lives in the binary — and this sweep could not have found it

**Raised by slice 8's planning** ([`0015`](0015-hot-reload-and-the-ruleset-as-a-thing-that-changes.md)),
which went looking for what a hot reload would have to refuse and found there was nothing there to
refuse.

- [x] **`src/Borough.Core/Space/SeparableKernel.cs:177` — the industrial pollution kernel radius is
      `public const int IndustrialPollutionMetres = 1_024;`**, with the kernel built from it at `:180`.
      It is not in `LayerRuleset`, it is in no file, and no loader has ever seen it. **`adr/0015`'s
      world-creation category freezes a number *per world*; it does not move it into the binary** — its
      own words are that these constants *"live in the Ruleset like everything else and are **read**
      from it, but they are fixed when a world is created and baked into the save"*. `CLAUDE.md` states
      the rule without qualification: **a `const` where a Ruleset value belongs is a defect, not a
      shortcut.** Discharged by `0015` task 3, which needs it discharged *first*: a reload cannot refuse
      a change to a number a designer has no way to change, so the refusal is untestable theatre until
      the value is in a file. Moving it does **not** ratify it — the 1–10 km band is still 10× wide and
      the `0002` §D row stays open. **Discharged by slice 8 task 3**: it is `[layers] kernel_metres`,
      it lives in `LayerConstants`, the loader reads it, `MapLayers` freezes it at world creation, and
      both the loader and the core refuse a reload that moves it. The `0002` §D row did stay open

**The paragraph below asked a question and task 3 answered it, so the answer replaces the guess.**
*"Two of the three other enumerated members have never been checked against the same test either"* —
they have now, and **all three fail it**. `TICKS_PER_DAY` had no symbol at all until task 3 named it
`Ticks.PerDay` (it existed as prose in three documents and as a bare `8192` in one populator);
`WHEEL_SIZE` is `EventWheel.Size`; the **Cell** is `CellGrid.TilesPerCell`. So `adr/0015`'s sentence
that its world-creation members *"live in the Ruleset like everything else and are read from it"* is
false of **three of its four members**, and was false of all four until task 3.

- [ ] **`adr/0015`'s world-creation enumeration is one-quarter implemented, and the remaining three are
      not the same defect as the kernel radius was.** The radius was *tuning frozen per world* that had
      simply never been offered. These three are numbers the corpus argues a designer should **not** be
      handed: `adr/0019` is an entire ADR on `TICKS_PER_DAY` not being a pacing knob, `CLAUDE.md` calls
      the Cell a *design constant, never tuned*, and `WHEEL_SIZE` is set by the longest routine sleep
      rather than by taste. **So the correction owed is to the ADR's sentence, not to the code**: either
      the category admits a member that is Ruleset data *in principle and not in the file*, or those
      three belong to the revisit trigger's *"a parameter that genuinely cannot be data"* exception,
      which the ADR already provides and which requires a written exception each. Nothing is unguarded
      in the meantime — a file that cannot state a number cannot change it — so this is a sentence to
      fix rather than a hole to plug. **Arguable** under `adr/0043`: no measurement separates the two
      readings

**Why this ledger did not already hold it is the more useful half.** The entry above —
*`adr/0015`'s world-creation constants were missing the kernel radius* — is marked paid, and it is:
`adr/0044` instructed the ADR to enumerate the member, and the ADR now does. **The document was
corrected and the code it describes was never looked at**, so the corpus reads as consistent from every
angle this sweep took.

**That is a directional bias in the audit's own method.** The premise is *status-bearing documents
against the state of the code*, and every one of the forty-odd items above corrects a **document**.
The sweep had no verb for the other direction. Where a design document is **right** and the build is
wrong, this ledger is structurally blind — and `adr/0015` is precisely the ADR most exposed to that,
because what it asserts is a property of **where a number lives**, which is checkable in source and
invisible in prose. The other three enumerated members were never checked against the same test either
— **and when task 3 checked them, all three failed**, which is the box above.

*This is a code defect rather than a document correction, so it sits outside this ledger's closing
condition.* It is filed here because the same method found it and because it belongs beside its
sibling entry — but it is discharged by slice 8, not by typing.

### `02 §5.7` claims a Zone Rule's cost is constant regardless of Zone size, and it is 1.56×

**Raised by slice 10 task 9's tripwire** ([`0014`](0014-zone-rules-and-the-sweep-family.md) §9), which
existed to measure this sentence and did.

- [ ] **`02 §5.7` — amend, do not strike.** Measured **1.56×** from 256 Lots to 256,000 with the
      sample fixed, against a deliberate-scan control rung that moved **989×** on the same data. The
      section is right about the algorithm — sampling is `O(sample)` exactly and the number of Lots
      never enters it — and wrong about the consequence: the cost is a function of the **working
      set**, so it climbs as the Lot table leaves each level of cache and then stops. The sentence as
      written is falsifiable by a benchmark, and a reader who ran one would conclude the sampling
      design had failed when what they had measured was DRAM

**Filed here rather than in `0002` because it is not a question.** The number exists, the machine
that produced it is committed, and what is owed is a paragraph in `02`.

### `adr/0055` says a derelict Building dies of its own failures, and it structurally cannot

**Raised by slice 8's merged degradation task**, from the call site `adr/0055` is about. **The
positive claim it collides with is now [`adr/0057`](../docs/adr/0057-dereliction-is-a-design-time-state-and-it-is-derived-rather-than-recorded.md)**,
which strikes this bullet by name; what is left here is the edit to `0055` itself.

- [ ] **`docs/adr/0055` consequence bullet 2** — *"Slice 8's derelict Buildings decline like anything
      else… still sampled and still dies of its own failures, rather than becoming a permanent monument
      to a Ruleset edit."* **The mechanism it names cannot fire.** A derelict Building is one whose kind
      the Ruleset no longer declares, so it has no Rule Instances; `ZoneRuleEngine.Condemn` walks its
      Rules looking for one that has starved past its kind's threshold, finds none, and returns. It is
      a permanent monument, exactly as the bullet says it must not be. **And the obvious repair is
      forbidden**: condemning a derelict Building on sight is silent deletion arriving through the Zone
      Rule instead of through the reload, which is what `adr/0015` forbids and why the state is called
      derelict rather than removed. `adr/0015` wins; the Building stands until a player clears it
      (`PLAYER GOVERNS`). **Amend the bullet, do not strike it** — the ADR's decision is untouched and
      only this consequence is wrong

### `adr/0015`'s world-creation members are not the only Ruleset numbers that were never checked against the code

**Raised by slice 8 task 4 sub-task A**, and it is the same directional blindness this ledger diagnosed
about itself above — a design document that is right about a property of *where a number lives*, and a
build that never implemented it.

- [x] **A Ruleset declaration's id is its position in the file, and `02 §4.3` describes the reload
      degradations as though ids were stable across two files.** Nothing made them so. Deleting one
      `[[resource]]` shifts every id below it, and a live Bin row holds the id — so a reload that
      *removed* a declaration, which is the entire point of the degradations, would have relabelled
      every survivor while it derelicted the casualty. `RulesetShape.Compare` could not see it either:
      a **reordering** of two same-shaped declarations leaves every count, family and shape identical,
      so it read as *numbers only* and was admitted. **Discharged by slice 8 task 4**: `Ruleset` carries
      a key per declaration — the content hash of its name, supplied by the loader — and `Compare`
      checks it before anything else about that id. Rulesets built in code keep positional identity, so
      no fixture moved

### Both shipped Rulesets state a fixed defect as an engine property, and the fix is a baseline re-record

**Found while implementing [`adr/0063`](../docs/adr/0063-a-wait-list-wakes-on-the-bins-state-and-a-shortfall-is-derived-rather-than-stored.md),
2026-08-10.** `rulesets/minimal.toml` and `rulesets/minimal-tuned.toml` carry the same header paragraph:

> *THE SHORTAGE REGIME IS NOT AVAILABLE AND THAT IS AN ENGINE PROPERTY RATHER THAN A TASTE: a recorded
> shortfall is a deficit at the instant of failure and the wait list wakes on the arriving quantity, so a
> consumer short of 3 is never woken by three arrivals of 1. … See finding 41.*

**Every clause of that is now false**, and finding 41 is struck. What survives is the *content* reason the
file runs in surplus — it has no second producer and no sink — which is a different sentence.

- [ ] Rewrite both headers, and add the **mirror the original never stated**: *consuming in a quantum at
      least as large as any producer's headroom deficit*. Its absence is why `minimal-tuned.toml` broke the
      headroom side the day it was written.

**Why it is not struck in the commit that found it, which is the interesting half.** A Ruleset comment is
**hashed content** — `RulesetFile.HashOfContent` normalises line endings *and nothing else*, and says so in
its own remarks: *"whitespace, key order and comments are content."* So editing either header moves both
Rulesets' content hashes, which moves `session.borough`'s recorded `reload` line, `GoldenFixtures`'
`RulesetHash` and `TunedRulesetHash`, and every sample in `session-trace.txt`. Folding that into the
predicate's re-record would put **two unrelated causes behind one hash move**, which is precisely what
[`0003`](0003-build-plan.md) → *The hash-moving queue* refuses on the *right by cancellation* precedent:
*a re-record is a command; a mis-attributed hash move is a bug hunt.*

**This is a third shape of debt and worth naming.** Cause 1 is status stored in several places; Cause 2 is
an ADR's writes not all landing. This is neither: **a correction whose cost is a baseline**, so the honest
move is to file it rather than to bundle it. The ledger is where a correction waits for its own commit.

### ~~`04 §6`'s steps 4 and 5 do not say the attempt is a Trip, and step 5 reads as a sequence~~ — **PAID**

**Paid in the sitting that raised it**, and not by choice: `CitationTests` fails an ADR that no document
outside `docs/adr` cites, and it caught `adr/0066` and `adr/0067` within minutes of their being written.
**The test is the mechanical form of this file's *Cause 2*** — an ADR written, registered and never
propagated — and it turned a debt that would have sat here into an edit. `04 §6` steps 4 and 5 and the
paragraph beneath them now state the Trip, the one-per-occasion rule and the cost that follows;
`CONTEXT.md` → Household carries both decisions. *Original entry follows.*

**Owed by [`adr/0067`](../docs/adr/0067-a-shopping-attempt-is-a-trip-and-a-household-tries-one-provider-per-occasion.md)**
(session N task 5). Two edits to the seven-step chain, and the second is the substantive one.

- **Step 4** — *"A Household visits a shop on its Provider List and finds nothing"* — must say the visit
  is a **Trip**, and that the recorded failure is a **transaction** outcome on the Household, not a
  `Trip Fate`. As written, *visits* is compatible with a Household reaching across the city into a Bin
  without moving, which is the coverage model `adr/0032` demoted.
- **Step 5** — *"The Household consults the rest of its short, sticky Provider List"* — reads as a
  sequence **within one occasion**, which once step 4 is a Trip means a shortage costs `N` Trips per
  Household. It must say *at the next occasion*. This is a **correction**, not a clarification: the two
  readings are different cities, and the one on the page amplifies the failure path by the Provider
  List's length.

`CONTEXT.md` → **Trip Fate** needs no edit and is worth a sentence saying why: the four outcomes are
properties of the journey, and *arrived and could not be served* is not one of them.

### `spike-results`'s Provider List figures priced a representation the project's own rule forbids

**Owed by [`adr/0066`](../docs/adr/0066-the-provider-list-is-an-intrusive-index-list-and-its-ruleset-length-is-a-cap-rather-than-an-allocation.md)**
(session N task 5). S0a's footprint model holds the Provider List **inline** on the Household row —
*"104 bytes, 47% of the Household row and roughly 21% of the entire world"*, with *"every entry is
~4.5 MiB at 1M"* — which prices **declared capacity**, since a column is a flat array over the whole
slot count and an unset entry costs what a set one does. `adr/0066` makes it an **intrusive index
list**, which is what `05 §4` and `CLAUDE.md` already required of every variable-length collection and
what every other collection in the code does, `MemberHead`/`MemberTail` on that very table included.

**Three corrections, and none of them is a retraction** — the numbers are true of the model that was
measured:

| Where | What it says | What is owed |
|---|---|---|
| `docs/spike-results.md` → *Three things the footprint says that the corpus does not* | 104 bytes, 47%, ~21% of the world, *"a tuning knob controls a fifth of the world's footprint"* | A banner that these price the **inline** model, superseded by `adr/0066`; the cap no longer costs memory until knowledge is acquired |
| same, *"Households are the largest table in the world, and it is not close"* | 75.2 MiB against Citizens' 53.4 MiB | **The ordering may reverse** once 104 bytes stop being reserved. Flag rather than restate — the replacement figure is the measurable routed to `0002` §B |
| `src/Borough.Core/Entities/Entities.cs` | the same 47% claim, in a code comment | The figure has propagated into source; it travels with the correction |

**Why it is here rather than in `0002`**: nobody has to decide anything. `adr/0066` decided it; somebody
has to type. The replacement *number* is a different matter and is a `0002` §B row, because it needs a
machine.

> **PAID 2026-08-10, all three.** `spike-results` carries a banner above the two findings — stating that
> they are an upper bound correct for what was measured, and that *~21% of the world* and the
> Household-versus-Citizen ranking are suspended pending §B — and `Entities.cs`'s remark carries the
> same, with the caution it was written to give left standing, since which table is largest does not
> change which schema to be careful about. `CONTEXT.md` → Household states the structure.

### Not a defect — recorded so it is not re-raised

**The reporting terminal is described correctly.** The sweep flagged `adr/0045`, `02 §4.1` and
`CONTEXT.md` for saying a terminal *"records a condition"* when task 8 made the terminal the one Rule
that is **never evaluated**. Recording is not evaluating: the terminal does record its condition, it
is simply not `Check`ed as a Rule, because it has no term that could be short and would otherwise
succeed and re-arm the head for ever. The documents say what happens. No correction is owed.

---

## What the mechanical check should be

Deferred to the third step of this work, recorded here so the sweep's evidence is attached to it.

1. ~~**Every ADR is cited by at least one document outside `docs/adr/`.**~~ **Built** —
   `tests/Borough.Tests/Corpus/CitationTests.cs`. It went through two versions that passed and should
   not have: the first counted `plans/`, so the audit's own debt ledger satisfied it; the second
   counted `src/`, so a comment written the same afternoon did. **Documentation only** leaves exactly
   two of forty-nine failing, which is what makes it the right line rather than a strict one — the
   rule was checked against the corpus before being imposed on it. It found **`adr/0038`**, which this
   sweep had missed.
2. **Slice task status agrees between `0003` and the slice plan.** Needs one machine-readable line
   per slice. Catches the drift that started this sweep.
3. **Every member of `adr/0015`'s world-creation enumeration is read from the Ruleset, not a `const`.**
   Added after slice 8's planning found the kernel radius in the binary. This is the only one of the
   three that checks a **document's claim against the source** rather than one document against
   another, which is the direction the sweep was blind in — and it is the direction `adr/0015` is most
   exposed to, since a tuning number silently becoming a `const` is exactly the failure it exists to
   prevent. The enumeration is four items long, so the check is cheap; `Borough.Analysers` is the
   natural home if it wants teeth.

Neither is a substitute for the restructure. A check over three tables that disagree only tells you
they disagree; the point of thinning is that there is one place to be right.
