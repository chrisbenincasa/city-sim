# 0012 — The corpus audit

**A one-off sweep of every status-bearing document against the state of the code**, run after slice 7
task 8, because the board was found asserting that task 8 was hot reload — which it is not, and never
was. That single error was cheap to fix and expensive to explain: nothing had changed to make the
board wrong, so nothing could have caught it going wrong.

This document is the **record of what the sweep found**, not a plan. Items are struck as they are
discharged. It is a debt ledger with a closing condition: when everything here is struck, delete it.

**The design documents are paid and agree with the code.** `02`, `04`, `05`, `CONTEXT.md` and
`adr/0033` were corrected inline in the sitting that found them, together with the two ADRs the
citation check caught. **What remains open is entirely `plans/`** — `0002`'s live-status half and
`0003`'s three disagreeing tables — which is *Cause 1*, and which the board restructure has already
shown how to fix: give the fact one home and point at it. Nothing left in this ledger describes the
simulation wrongly.

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
- [ ] `0002` **coverage map** — contradicts itself (`§5` listed as both closed and unargued) and lists
      `§8`/`§10` as never grilled when session eight closed them. **This one got more important, not
      less**: the archive banner names the coverage map as the one section still worth reading, because
      it is the only per-document account of what has been examined and `adr/0043` cites its 🟢 rows as
      evidence that a green mark is not evidence a sentence was read. A self-contradicting map cannot
      carry that. **Promote it out of the archive and into the ledger once it is corrected**
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

Neither is a substitute for the restructure. A check over three tables that disagree only tells you
they disagree; the point of thinning is that there is one place to be right.
