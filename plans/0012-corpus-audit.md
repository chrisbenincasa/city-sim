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

## The diagnosis, which is two things and not one — **and a third, a fourth and a fifth were added later**

The sweep expected one failure mode and found two. They want different answers, and conflating them
is why *"tidy the documents"* has never worked as an instruction.

**Cause 3 was added on 2026-08-11 and was not found by the sweep**, which is itself worth noting: it
was found by a spike round tripping over it. The sweep reads documents against each other and catches
facts that disagree; Cause 3's documents **all agree**, and all three are wrong together.

**Cause 4 was added on 2026-08-12 and the sweep could not have found it either**, for a sharper reason:
its disagreement is not between two documents at all. It is between a document and **the code**, and
nothing in this corpus checks that — `CitationTests` checks links resolve, `CoverageMapTests` checks rows
exist, `MarkdownStyleTests` checks markdown renders, and **all three are document-to-document**.

**Cause 5 was added on 2026-08-13 and it is the one the sweep was best placed to find and least likely
to**, because its two copies **agree perfectly**. A figure is quoted correctly and its qualifying clause
is left behind, so the defect is not in either document — it is in the gap between them, and every
occurrence of the digits is identical.

Read the five together and they are ordered by how hard they are to catch, which is roughly the opposite
of the order they were found in:

| | What went wrong | The tell | Repair |
|---|---|---|---|
| **1** | one fact stored twice, drifting | the copies disagree | one copy, or a check |
| **2** | a write that did not land | a decision with no inbound citation | sweep for citations when a decision ships |
| **3** | a read never repeated — true when written, and the world moved | an item that names its gate | check the other way: when a gate clears, sweep for who cites it |
| **4** | **the text was never true** — it describes a mechanism and was wrong on the day | **nothing** | open the mechanism; write names rather than times |
| **5** | a **number** is quoted away from the sentence that qualifies it | **worse than nothing** — repetition makes a bare figure read as *more* settled | name the number after what it measures; quote the sentence, never the digits |

**Causes 4 and 5 are siblings and the difference is worth holding.** Both are about a decision taken from
something that looked like established fact. Cause 4's source sentence is **wrong**; Cause 5's source
sentence is **right and was left behind**. That makes 4 uncheckable by this corpus and 5 checkable by
machinery it already has, since both ends of a travelling number are in documents — which is why 5 sits
at the bottom of the table and has the cheapest available fix.

### ⚠ Cause 2's worst sighting, 2026-08-13 — **the closure named its own consumers by number and still did not land**

Recorded here rather than as a new cause, because it is not one: it is **Cause 2 with the usual excuse
removed**, and it sharpens the repair rather than adding to the list.

[`adr/0047`](../docs/adr/0047-routing-never-keys-on-the-district.md) closed four ledger entries and
**said so in itself, by number** — *"ledger entries 10, 11, 13 and 15 close together, which is what they
were circling"* — and named the one that did **not** close, *"decision 8 does not; it turned out to be a
different question."* [`plans/0010`](0010-s2-routing.md) applied it: decisions 11 and 13 carry
*CLOSED by `adr/0047`* in their own text. **[`0002`](0002-open-questions.md) and
[`0000`](0000-board.md) did not**, and an audit run beside `0003` queue item 6 found **five stale rows
across two sections** — §A's *how coarse a routing destination may be*, three of the five bullets in §C's
*Routing — session M's cluster*, and the board's session **I** row, which was serving as milestone 5c's
gate. **The gate had been discharged for two days.**

Three things make it worth the space.

- **The sibling test failed at one row's distance.** §A's *path source* row was struck by this same ADR,
  and its archived body quotes the ADR back. The row directly beneath it, on the same decision, in the
  same table, was not. *A reader who strikes one row has not thereby swept the table, and nothing marks
  the difference.*
- **The retyping is the dangerous half, not the staleness.** `adr/0047` did not merely close §A's
  question — it **retyped the survivor**, from *arguable* onto a different object that is
  **measurable and unset** (the routing partition's size, machine named). The stale `A` row therefore sat
  open **inviting a session to close by argument the exact thing [`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)
  forbids closing by argument**, and the successor was in no ledger at all. ***A closure that retypes what
  survives it writes two obligations, and only the first one looks like an obligation.***
- **It is what the corpus's own uncatchable-by-machine argument says should be catchable.** Cause 4 is
  uncheckable here because it is document-to-**code**. This is document-to-document, in a corpus with
  three mechanical document-to-document checks, and none of them looks for it.

**The repair is Cause 3's, run in the direction Cause 3 already prescribes** — *when a gate clears, sweep
for who cites it* — with one addition this sighting earns: **an ADR that enumerates the entries it closes
should be the input to that sweep, not a substitute for it.** Filed as a candidate for **mechanical check
9**: an ADR naming a ledger entry by number, against a ledger entry not struck. Both ends are documents,
which is the property that makes it checkable at all.

#### ✅ Check 8 — link resolution. **BUILT 2026-08-13**, `tests/Borough.Tests/Corpus/LinkResolutionTests.cs`

**Green on the day it was written, at 2,292 links across `docs/`, `plans/` and the three root files, and
verified in both directions** — a deliberately dead link makes it red, removing it makes it green again,
which is the corpus's own *write the violation and watch it fire* rule applied to a check rather than to
an analyser. It ships with a companion `[Fact]` that exercises the extractor on synthetic text, because a
file-scanning assertion cannot demonstrate its own failure without committing a broken document; that
companion also pins the three exclusions (an absolute URL, a bare anchor, a link inside a fence) so that a
later widening cannot silently turn them into failures.

**Two design notes worth keeping.** The worktree exclusion is **structural, not a predicate**: the check
enumerates `docs/` and `plans/` from the repository root, so a stale corpus under `.claude/worktrees/` is
*unreachable* rather than *skipped* and cannot be re-admitted by somebody relaxing a filter — which is the
exact failure mode `CitationTests`' two false-green revisions had. And **the anchor is deliberately not
checked**: `#a-heading` is a much weaker claim than a file existing, its slugification is renderer-specific,
and folding them together would make a strong check fail for a weak reason.

*The section below is the original proposal and the measurement behind it, kept because the measurement is
the argument.*

#### ⚠ Check 8 is a different and much cheaper one, and the sitting found it by committing the defect

**Nothing in this corpus checks that a relative link resolves.** The four checks that exist are
`CitationTests` (every ADR is cited by ≥1 document outside `docs/adr`, matched as the **regex `adr/\d{4}`**
— it never opens the target), `CoverageMapTests` (every ADR has a §F row), `MarkdownStyleTests` (tables
render, emphasis uses asterisks) and `DisqualifierTests` (check 6). **A link to a file that does not exist
passes all four.** The sitting discovered this by writing one — `adr/0017` cited as
*households-satisfice-they-do-not-optimise*, which is the ADR's **claim** rather than its **filename**
(*agents-satisfice-they-never-optimise*) — running the suite, and watching it go green. ⚠ **And the same
sitting had already told the user those tests mean *"citations resolve"***, which is `adr/0093` on a
**test name**: the check was described from what it is called rather than from what it opens.

**Measured before proposing, because a check nobody can pass is a cleanup project wearing a ratchet's
clothes.** Across every `.md` in the tree: **4,064 relative links, 5 dead — and 4 of those are in a stale
`.claude/worktrees/5b-trips-and-legs` copy**, not the live corpus. **The live corpus had exactly one**:
`adr/0094:132` pointing at `../plans/0013-tick-budget.md` from inside `docs/adr/`, one `../` short, written
2026-08-13. Fixed in the same sitting.

**That measurement is the argument for building it.** One dead link in 4,064 means the discipline has been
manual and has *worked*, so the check goes green on the day it is written and every future breakage is a
red build rather than an audit — which is the property `adr/0003`'s per-field declaration has and
*"remember to check your links"* does not. **It is also the cheapest check in this file by a wide margin**:
no registry, no ledger parsing, no judgement about what a document means. Resolve the path, or fail.
⚠ **Two scope notes it must ship with**, both learned from `CitationTests`' two false-green revisions:
**exclude `.claude/worktrees/`**, which holds stale corpus copies that are somebody's in-flight work and
not the corpus; and **check the anchor separately or not at all**, since a `#heading` fragment is a
different and much weaker claim than a file existing.

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
>
> ---
>
> **A third form, found 2026-08-11: a fact with exactly one copy, in the wrong artefact.** It behaves
> like no copy at all, because the artefact is invisible to every reader who is not already inside it.
> S2 R2 measured a defect in `IntegerMath.FloorDiv` — *"four 64-bit hardware divisions per node … most
> of the denominator"* — wrote ***"Worth recording beyond this spike"***, worked around it locally, and
> recorded it nowhere but `spikes/S2.Routing/Routing/Heuristic.cs`, **a file whose stated destiny is
> deletion**. S5 met the same defect three rounds later in a kernel that cannot work around it, and
> published a tripwire against `adr/0016` blaming `adr/0003`.
>
> **The mechanism is new and it is not drift, absence or neglect: a local workaround removes the
> finder's own exposure, and with it the only pressure that would have fixed the source.** The defect
> survived *because* it was found by a competent author who fixed his own problem. Now a rule:
> [`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md)
> — **a local workaround is not a discharge**. It is not a fourth inference sibling; it governs what a
> spike must *do* with a finding rather than what a sitting may *conclude*, which puts it next to
> `adr/0042`. The habit it states is the counterpart of `adr/0070`'s: **before working around something
> you do not own, name the document the finding goes to.**

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

### Cause 3 — a blocked item cites a gate and then never re-reads it

**Added 2026-08-11, by S2 R7, which found itself blocked on two gates that had both already cleared.**

`plans/0010`'s R7 said deleting the 33,000-line S2 harness was *"not available"* because R6's
invalidation half was gated on session **M**, and because R6.3 had put a second question in front of
it. Session M ran and shipped that contract as an amendment to `adr/0012`. R6.3's question closed in
session **D** task 1 into `adr/0061`. **Neither clearance reached S2's plan, `spike-results`'s S2
section, or the board** — so three documents went on reporting a block that had stopped existing, and
the last thing standing between a spike and closure was nothing at all.

**This is not Cause 1 and it is not Cause 2.** Cause 1 is one fact stored twice, drifting; the repair
is *one copy, or a check*. Cause 2 is a write that did not land; the tell is a decision with no inbound
citation. **This is a read that was never repeated.** The blocked item was correct when written, cites
its gate honestly, and needs no edit to become wrong — the world moves and the sentence stands still.
It is the cheapest kind to write and the most expensive to catch, because **an item that names its gate
looks rigorous**; the rigour is what stops you checking it.

*A duplicated fact drifts, and a cited gate rots.* The difference matters for the repair: nothing about
the blocked item's own text can fix this, because the text is not what went stale. **The check has to
run the other way** — when a session or a spike round closes, sweep for documents citing it *as a gate*,
which is greppable in the same way `adr/0049`'s orphaning was. R7's earlier repair to its own owed-list
— *each entry carries its state explicitly, so "present" stops meaning "open"* — is Cause 1's medicine
and does nothing here: the entry's state was recorded, and the state was `blocked`, and that was true of
the entry and false of the world.

### Cause 4 — a decision is taken from a description of the code, and the description is wrong about the trigger

**Added 2026-08-12 by session P, with four sightings across three consecutive days.** The rule is
[`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md) —
***a description of the build is where to look, and never what you found*** — and it is the fourth member
of the `adr/0043` / `0052` / `0070` family: those govern what a sitting may conclude from **claims**,
**numbers** and **absences**, and this governs what it may conclude from **what the build does**, which
every sitting reasons from constantly and nothing has ever governed.

| | The description consulted | What it said | What the code said |
|---|---|---|---|
| `adr/0064` | **the test suite** | `RulesetLoader` refuses nothing for a duplicate `(kind, Resource)` Bin | it has refused since slice 7 task 8 — and that refusal was the **one guard in the loader with no test** |
| `adr/0079` | **an ADR's summary sentence** | route a stranded Building through `adr/0053`'s pressure, *"it needs no new mechanism"* | that pressure is a duration of **Rule Instance starvation**, and a bulldozed Street starves nothing |
| `adr/0091` | **a plan's own recommendation** | *"re-zone and wait — the Zone Rule condemns in its own time"* | `ZoneRuleEngine.Condemn` never reads the permission set, so a healthy Building never falls |
| `adr/0090` | **a doc-comment, inside the code** | `RoadGenerator` *"lays a complete Street lattice over the whole map at world creation"* | one production call site, `SyntheticCity`, reached only by `Populate` — a player's world has had no roads since 5a |

**All four are wrong about the *trigger* and none about the behaviour** — the loader's refusal, the
pressure's key, the condemn predicate's terms, the generator's caller. A sentence written to explain a
mechanism explains its **purpose**, and a purpose is not a trigger, so that is the half a description
reliably omits and the half a decision usually turns on.

**This is not Cause 1, 2 or 3, and it is the only one no re-reading finds.** Cause 1's copies disagree;
Cause 2 leaves a decision with no inbound citation; Cause 3's text was true when written. Here **the text
was never true**, there is no second copy to disagree with, no missing citation to spot, and no gate to
re-check. It is also the only one this corpus is structurally unable to check, per the table above.

**The repair is to write a name where a time is written**, and it is why this cause has a cheaper answer
than its position in the table suggests. *"At world creation"* states a moment, and moments are not in the
code; ***"when `SyntheticCity` runs"*** states a symbol, and one grep settles it. Three of the four
sightings would have been self-refuting had the sentence named a caller. Reading side: a description tells
you which symbol to open — open it. Writing side: name a **symbol**, never a time, a phase, or a stage of
the project.

**The corpus coined this twice and left it as commentary both times** — `adr/0044`'s *citing an ADR is not
applying it* and `adr/0079`'s *citing a mechanism is not checking what it is keyed on*, the second offered
explicitly as the first's sibling. Two ADRs reached for the same sentence independently, neither made it
binding, and the failure recurred twice afterwards. ***An aside is not a rule, and the evidence is that
this one was written down twice and did not hold.***

### Cause 5 — a number is quoted away from the sentence that qualifies it

**Added 2026-08-13 by session P, on a sighting the session committed itself**, which is why it is here
rather than in a ledger of other people's mistakes.

*⚠ **Cause 4's fifth sighting, 2026-08-13, and it is the board.** An hour after 5b-bis closed in this
tree, a sitting answered *what is next* by reading `plans/0000-board.md`'s *the code track holds one row:
5b-bis* and reported a task that had been committed ninety minutes earlier. `git log` would have settled
it in one command. **The board is the document in this corpus most likely to be read instead of the
build**, which is exactly what its own opening calls *a view, never a source* — so `adr/0093` binds
hardest on the file whose whole purpose is to describe status. The general form, worth holding beside the
*name a symbol, never a time* rule: ***a status line is a claim about the present tense and nothing in a
document can keep one true.*** Struck rather than deleted on the board, with the correction beside it.

**A caveat attached to a number does not travel with it.*** A figure is written down correctly, with a
clause saying what it measures and what it must not be used for. Somebody later needs a number of roughly
that shape, finds it, and copies **the digits**. The clause stays where it was, still correct, still
findable, and now doing nothing — and because the two documents agree to the last digit, no comparison
between them can see anything wrong.

| | The number | The qualifier it left behind | What was then done with it |
|---|---|---|---|
| **`adr/0094`**, 2026-08-13 | **186,624**, quoted as **~186,600** | `adr/0082`'s *"heavily caveated… it is an **upper bound**"*, and `plans/0013`'s *"S2 R2's fixture"* | used as a bare **denominator** for the Microscopic Cap, and at the **4×** budget rather than the design speed's, producing **27–58×** where the same numbers give **7.3×** |
| **the correction**, hours later | the **caveat**, not the number | `plans/0013`'s three words *"not a stressed count"*, which mean *not a **real city's** stressed count* | read literally, producing the claim that 186,624 *"is a whole fleet"*. It is **2,592 stressed Segments × 72 Vehicles** and was derived carefully in `adr/0082`. Corrected in this file, `adr/0094`, `adr/0096`, `plans/0025` and `CLAUDE.md` |
| **the developed density**, ongoing | **3,700 / km²** | `plans/0002` §D's *"an **output** of the 1M target — §1's column is headed *1M implies*"*, and `docs/spike-results.md` saying it again | cited in four documents as an **independent check** that the map may grow, when it is 1,000,000 ÷ 268 km² and therefore the old map restated. See *The developed density every map decision is priced against is circular* below |
| **`adr/0089`'s amendment**, written 2026-08-13, found 2026-08-14 | the Commute Budget rungs **20 / 40 / 50** minutes | [`adr/0095`](../docs/adr/0095-a-commute-budget-is-three-rungs-and-only-the-last-one-refuses.md)'s own finding, in the commit that shipped it: *"percentiles of a free-flow, **foot-only** distribution"* | substituted into the map table's *Commutes across* column, whose denominator is a **25–36 km/h vehicle** and always was — the table carries *Foot crossing* as a **separate** column. Reports **2.2–3.1** crossings where the same map on foot is **15.7**. Corrected in `adr/0089` |

**~~Three~~ Four sightings, and the middle one is the odd one out.** The first and the third are the **pure
form**, twice: a qualifier that exists, is correct, sits in the right place, and is simply not carried —
in the first it had been read **hours earlier by the same sitting**, and in the third it is written twice
over, in `plans/0002` §D and in `docs/spike-results.md`, while four other documents quote the digits
alone. In the second **the qualifier was itself the thing quoted away from its context** — a three-word
compression that is true where it sits and false when carried, which produced a *wrong correction to the
first*. **The effect on a reader is identical in all three**: a bare figure offered as evidence, with
nothing in the quotation to say whether it can bear the weight.

⚠ **The third sighting was originally written into this table as *no qualifier was ever written*, and
that was false** — the clause was there, in two documents, and the sweep that filed the debt had not
looked. Corrected 2026-08-13. It matters because a missing qualifier is a **writing** failure with no
repair available, and this is a **reading** failure with an obvious one, which is the whole reason check
6 can exist: *there is nothing for an instrument to point at until somebody has written the sentence.*

⚠ **The fourth sighting, 2026-08-14, is the first where the qualifier and the number were written by
the same author on the same day**, and it is the cheapest of the four to have avoided. `adr/0095`'s
commit message states the disqualifier in capitals — the rungs are *percentiles of a free-flow,
**foot-only** distribution* — and the same session then carried 20 and 50 into a column it had to open
`adr/0089` to edit, a column headed by a *Foot crossing* sibling reading **18.5–26.2 h**. **Both halves
were on the screen.** What defeated it is that the qualifier and the destination were phrased in
different vocabularies: one says *foot-only*, the other says *commutes*, and nothing about the word
*commute* announces a mode. ***A unit is not a denomination and a noun is not a unit*** — which is
`adr/0094`'s `revisit_ticks` lesson (*the name of a quantity is not its denomination*) arriving on a
**cross-document** quotation rather than inside one table.

**The general form this adds, and it is the one worth carrying:** *when a number is a duration, the mode
is half of it.* A distance is a time times a speed, so a time quoted without its mode is not a
disqualified number, it is **half a number** — and the missing half was six to ten times, silently. The
registry's writing rule extends: **name a duration after the mode that performs it**, so *"50 minutes"*
is never written where *"a 50-minute walk"* or *"a 50-minute drive"* would do.

***The second sighting is the one to remember, because it happened while writing this section.*** A
caveat is a claim, so it travels exactly as badly as a number does, and compressing one for a table cell
is how it acquires the ambiguity that makes it dangerous elsewhere. **Do not compress a disqualifier to
fit a column** — put it in the prose beneath and let the cell point at it.

**The tell is worse than nothing, and that is what distinguishes this from Cause 4.** Cause 4's tell is
*nothing* — a wrong sentence looks like a right one. Cause 5's tell is **negative**: each time a figure is
repeated it accumulates apparent authority, so the corpus's own habit of quoting numbers across documents
is the mechanism that makes the defect worse. `plans/0002` says this in as many words —
***an unratified number is more dangerous than an open question; it arrives as an illustration and is
repeated until it reads as settled*** — and `plans/0013` says it again in the very paragraph the 186,624
was taken from: *"a number becoming a decision by being the only number in the room is a habit this corpus
has already recorded, and **this table is where it would happen**."*

**So this is Cause 4's ending repeated exactly**: coined twice, left as commentary both times, and it then
happened — in an ADR written by the sitting that had just read the second coining. ***An aside is not a
rule*** is now evidenced twice over, on two different causes, which is the strongest argument this ledger
contains for writing a rule down rather than observing it well.

**The repair has two halves and the writing half is the one that compounds**, on `adr/0093`'s pattern:

- **Reading**: quote the **sentence**, never the digits. If a number arrives in a decision without a
  clause saying what it measures and where it came from, it is not yet evidence — it is a coincidence of
  magnitude.
- **Writing**: **name a number after what it measures, not after where it sits.** *"186,624"* is a bare
  integer and travels freely; *"R2's fixture fleet"* carries its own scope and cannot be silently made a
  denominator. This is `adr/0059`'s move again — state the thing that is checkable and let the rest be
  derived — and `adr/0093`'s *name a symbol, never a time* on the numeric axis.

**A percentage of a budget is the special case, and it has its own rule: *carry the bill, not the
percentage*.** A share is a measurement divided by a **product decision** — which speed rung, which
clock — so it is two facts glued together and only one of them is about the code. `plans/0013`'s whole
last row is the demonstration: ≥229%, ≥114%, ≥57% and ≥29% are **≥17.8 ms** over four candidate budgets,
and reading them as four results is reading one measurement four times. **The failure mode is specific
and it has happened**: `861.87%` survived `adr/0096` repricing its denominator *and* `adr/0094`
multiplying its numerator, to within 0.4%, because the two moved in opposite directions — invisible as a
percentage, obvious as **134.135 ms**. ***A percentage hides which side moved.*** Swept through the
corpus on 2026-08-13; every budget share now states its milliseconds first.

**And a number that is one half of a ratio should say which half.** Every sighting of this cause so far
is a figure being used as the other side of a comparison it was never a side of. The Microscopic Cap has
carried the correct warning about this since `adr/0062` — *it is a ratio and S5 supplies one half of it*
— and the failure still happened, because the warning lives with the **Cap** and the number that got
borrowed lives in a benchmark table.

#### The disqualifier registry

**Built 2026-08-13 as check 6 —** `tests/Borough.Tests/Corpus/DisqualifierTests.cs` **reads the table
below.** Each row names a figure, every rounding it is known to travel as, the **exact phrase** that must
accompany it, and the document that owns the caveat. The test asserts two things: that the **owner**
contains both the figure and the phrase, so this table cannot drift from it in silence; and that **every
other prose document containing the figure contains the phrase too**.

<!-- disqualifier-registry -->

| Figure | Also written as | The phrase that must accompany it | Owner |
|---|---|---|---|
| `186,624` | `186,600` | `upper bound` | `docs/adr/0082-the-behavioural-clock-is-global-and-car-following-sub-steps-inside-it.md` |
| `532,750` | `532750` | `one core` | `plans/0013-tick-budget.md` |
| `3,700` | `3,731` | `of the 1M target` | `plans/0002-open-questions.md` |
| `10.37` | `10370` | `9.4–10.5` `9.37 to 10.51` `9.37–10.51` | `plans/0013-tick-budget.md` |
| `2,592` | `2592` | `upper bound` | `docs/adr/0082-the-behavioural-clock-is-global-and-car-following-sub-steps-inside-it.md` |
| `18.52` | | `uniform` | `docs/spike-results.md` |
| `82.84` | | `synthetic` | `plans/0011-rule-engine-bins-and-rules.md` |
| `861.87` | | `15.6 ms` | `docs/spike-results.md` |
| `90 in-world minutes` | `22.5` | `pre-clock` | `docs/adr/0094-a-day-is-2048-ticks-because-ticks-per-day-is-a-sampling-rate-and-not-a-length-of-life.md` |

**The last four rows were added by a sweep for 186,624's siblings**, on 2026-08-13, and each is the
same shape: a figure that is **one half of a ratio**, quoted into an argument, whose scope clause lives
in one document and not in the others.

**The ninth row is the first one added by catching the failure live rather than by sweeping for it**,
and it is the sharpest evidence this registry has for its own existence: ***it was committed by the
author of this section, in a commit message and five documents, on the same day, in paragraphs that
cite Cause 5 by name.*** The Goods rescale shipped saying *a dwelling now holds 90 in-world minutes of
sundries where it held 22.5, which is a change to the world rather than a neutral rescale*. The digits
are right; the comparison was never stated. It is HEAD against the **pre-clock** world — the larder is
128 Ticks deep and was 128 Ticks deep before `adr/0094` — so the figure restates the clock's already-
recorded cost rather than naming a new one, and a measurement settled it: a **byte-identical 86-line
census** across the rescale. **Knowing the rule, having just written the rule down, and warning about
the rule in the same paragraph, did not prevent it.** That is what a mechanical check is for.

**It is also the first row whose figure is not a cost**, and that was argued rather than assumed. The
other eight are performance or sizing numbers used as one half of a ratio. This one is a fixture's
larder depth in a file whose first line says it models no city, so nothing will be *budgeted* on it —
what it is at risk of is being quoted as evidence that a larder is generous, which is the same failure
with a different consequence. **The row costs one line and disarms a figure that now sits in four
documents.**

- **`2,592`** is 186,624's own **factor** — 2,592 stressed Segments × 72 Vehicles. Registering the product
  and not the factor leaves the ratio rebuildable from its parts, and S5 had already sized its L3 fixture
  from it. It carries the *same* disqualification as the product, deliberately: a row about a factor must
  not say something weaker than the row about what it multiplies into.
- **`18.52`** is the purest example in the corpus. R2's next-hop detour is **18.52% on a uniform
  origin-destination draw and 128.82% on a local one**, which R4 calls *"a different city"* — so the
  number is a property of the draw at least as much as of the path source. `adr/0041` decided volume
  attribution partly on it, with no draw named.
- **`861.87`** is the sharpest of the four and the only one whose disqualifier is a **denominator**
  rather than a scope. It is a bill of **134.135 ms** over the **15.6 ms budget at 4×**, and both terms
  have since moved in opposite directions — `adr/0096` prices at the design speed's 62.5 ms (214.6%),
  `adr/0094` multiplies routing by 4 (back to 858.5%). ***It is now right by cancellation, which is the
  second sighting of a lesson `plans/0013` coined about its own Bin Rule row.*** `adr/0012` and
  `adr/0061` both decided something on it with no denominator named at all.
- **`82.84`** is a **synthetic** unit that met a real world at **552 ns**. `plans/0011`'s own finding is
  ***a unit cost is a hypothesis until a real world has produced one***, coined about this figure, and
  `02 §4.1` was still quoting the laboratory number as a budget.

⚠ **And the check failed on `plans/0025` the same minute, against a paragraph written to explain the
registry** — which had named `3,700` and `10.37` by their digits while arguing that figures must not be
named by their digits. It is the sharpest evidence this file has that Cause 5 is a **reflex** and not a
lapse: a number is shorter than a description of a number, so the wrong form is always the easy one, and
nothing except a machine pushes back at the moment of writing.

**Registering the four cost six sentences**, one or two documents each. That is the argument for doing it early:
the phrase is cheap while the sites are few, and every future document that needs the number inherits the
scope for free.

**The phrase cell may hold several spellings of *one* disqualification and any satisfies** — a caveat is
prose and is legitimately worded two ways, `9.4–10.5` in a summary and `9.37 to 10.51` where the five
captures are listed. **They must name the same disqualification.** Two different caveats in one cell is
the substituted-caveat hole reopened, which is the thing this check exists to close.

⚠ **This table's first row was authored wrong, and the check caught it on its first run — which is the
best evidence for it in this file and is why the mistake is kept rather than tidied.** The phrase
originally required was `plans/0013`'s *"not a stressed count"*. Following the check's own failure report
into `adr/0082` showed that **186,624 is 2,592 Segments over an 80% stress threshold × 72 Vehicles a
Microscopic Segment** — a stressed-Vehicle estimate, arrived at carefully, and **not the whole fleet the
correcting sitting had called it**. `plans/0013`'s three words mean *not a real city's stressed count*,
and compressed that far they say something else when read alone.

***So the caveat itself travelled without its context, and produced a wrong correction to the error it
was correcting.*** Cause 5 twice over on one figure, on consecutive turns: first the number quoted with no
clause at all, then the clause quoted with no number behind it. The registry now names the substantive
disqualification — **it is an upper bound**, because R2's uniform origin-destination draw is the
longest-trip distribution available (R4) — and points at the document that derives it rather than the one
that summarises it.

**Pinning the *particular* phrase is the whole design, and it is what the refuted version lacked.** A
check asking *is this number qualified somewhere nearby* would have **passed** every site, including the
one that dropped the clause that mattered. **Owning documents are preferred to summarising ones for the
same reason**: a summary is where a caveat gets compressed, and a compressed caveat is the thing that
went wrong above.

**The instrument has been defeated twice by typography and never yet by content**, which is worth
recording because both failures produced *false positives* against clean documents and both looked like
findings. The first parser split the alternates cell on commas and turned `186,600` into `186` and `600`;
the second compared phrases against unflattened text, so a caveat hard-wrapped across a line break read
as absent. **Both fixes are in the matcher and neither is in the prose** — the alternative was to forbid
digit grouping and to reflow paragraphs to suit a test, which is a check bending the corpus around
itself. *A guard that makes people rewrite true sentences to keep it quiet is a guard that will be
switched off.*

**The table is small by construction and that is the answer to the obvious objection.** Check 5's design
note warns that a hand-maintained list inside an instrument is *Cause 1 arriving inside the check* — true
of a directory index, which changes weekly, and not of this, which holds only figures somebody has
actually been caught by. An entry is authored by whoever writes the caveat, on the day, which puts the
obligation on the person who already knows the answer; and the owner-side assertion means a reworded
caveat fails loudly rather than orphaning the row.

**The two candidates named here as *not yet registered* were registered the same day, and doing it
found something.** `3,700` was held back on the ground that the corrections owed for it further down this
file should land first; registering it instead showed **the filed debt was incomplete**. The two boxes
name `adr/0089` and `CLAUDE.md`; the check named **four** sites, adding `05 §1`'s budget block — which is
where the circular derivation actually lives — and `adr/0085`, which divides the density back out of the
map it came from. ***A debt filed by reading is a lower bound on the debt.*** All four are paid.

`10.37 ms` cost nothing at all: every site already carried the spread, so the row is a **trap set before
anybody stepped in it**, which is the one entry here not earned by a sighting. That is allowed and should
stay rare — *the registry earns entries from sightings, not from a scan*, because a row costs a phrase in
every future document that legitimately needs the number.

**Unlike Cause 4, this one is mechanically checkable**, and cheaply, because both ends are documents.
See check 6 in *What the mechanical check should be* — **including what was specified there first, and
measured false**.

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

- [x] **`0002`'s *path source* row is closed by [`adr/0047`](../docs/adr/0047-routing-never-keys-on-the-district.md)
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
      ledger, and narrow `0000`'s *R6 gated on session M* to the invalidation half.
      **PAID 2026-08-11.** Clause 1 was struck (`0002` §A, and the body archived). Clause 2 is
      **superseded rather than done**: there is nothing left to narrow the gate *to*, because the whole
      gate has since cleared — session M shipped the contract into `adr/0012`, R6.3's question closed
      into `adr/0061`, and **nobody told S2**. That is **Cause 3**, which this item is the near miss of:
      it caught the gate being *wider than its reason* and did not think to ask whether the reason still
      held

- [ ] **Run check 4 across the corpus once, and again whenever a session or spike round closes — NEW,
      2026-08-11, and it is Cause 3's only repair.** The one-off pass has been run and S2 was its sole
      casualty, but *once* is not the deliverable: the failure is generated by **closing** something, so
      the sweep belongs to the closing act. Cheapest home is the board's *Done* tables, which already
      list every closed session and round — the check is to grep the corpus for each of those names in
      a blocking construction. **It does not need to be automated to be worth writing down**, but it
      does need an owner, and the natural one is whoever strikes a row in *Done*

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

- [x] **PAID 2026-08-12 by session H** ([`adr/0083`](../docs/adr/0083-a-sheds-use-is-the-arrival-query-and-a-stale-shed-is-wrong-by-a-bounded-walk.md)).
      `05 §3:136` now states the rung — **per-Segment witnessed by the walk paths to the Bins the shed
      kept** — carries the *when you pay / what survives* correction and R5.6's four rungs, and adds the
      half this box did not know was owed: **the shed needs no staleness parameter**, because its
      addition error is bounded by the radius and already priced by the Commute Budget. *Original:*
      `05 §3:136` — *"cached Parking Shed membership per Building … **invalidated by the Road Graph
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

**✅ STRUCK 2026-08-11. Both boxes below are done — 5a-bis shipped.** `06`'s Phase 2 table has a
**5a-bis** row naming the mechanism and the risk it retires, and `02 §2.2` says the subdivider is built
rather than that it does not exist. **The entry is kept rather than deleted for one sentence's worth of
history**: it stayed open through milestone **5a**, whose own brief scoped 5a-bis and named it
correctly, and *still* did not put it anywhere a reader looking for work would find it. A mechanism can
be fully specified, correctly scoped, explicitly named by the slice next door, and owned by nobody.

*Original:* **⚠ IT HAS A BUILDER AS OF 2026-08-11 and both boxes below are its to strike:**
[`0022`](0022-the-lot-subdivider-and-build-road.md), **5a-bis**, which pairs the subdivider with
`build_road` because `02 §2.2`'s re-subdivision clause needs an edit signal and the edit signal is
`adr/0012`'s Epoch. **The gap was open for the reason the entry below names**: this is the same shape as
`02 §5.2` step 2 — *settled design, specified in full, owned by nobody* — and that one closed by being
built. Note the entry stayed open through **milestone 5a**, whose brief scoped 5a-bis and named it
correctly and still did not put it anywhere a reader would look for work.

- [x] `docs/06-roadmap.md` — the *Mechanisms with no milestone* table does not list **Lot
      subdivision**, and milestone **5a** is *"Road Graph and Streets"*, whose named risk is geometry
      leaking into the simulation. Neither names the thing that turns zoned land into parcels.
      `02 §2.2` specifies it — subdivision rules, depth and width varying by density band,
      re-subdivision on network change preserving existing Buildings — and `adr/0014` and `adr/0035`
      both reason *from* its frontage rule, so it is settled design with no builder. It belongs either
      in 5a explicitly or in the no-milestone table
- [x] `docs/02-simulation-model.md §2.2` — *"Lots are **generated, not painted**"* is true of the
      design and **false of the build, in every world this project has ever run**. Every Lot in the
      tree is painted: one per `CommandKind.Zone`, or `SyntheticCity`'s grid. Slice 10 made this
      visible and deliberately did not fix it (`0014` → *The second collision*), because the fix is
      5a's. The section wants the same treatment `rulesets/minimal.toml` gave its own emptiness — a
      sentence saying the generator does not exist yet — rather than a silent gap between a design
      document and every number taken under it

**The general shape is the one `0013` published about unit costs.** A precondition stated in a design
document is a hypothesis about the build until something enforces it. Nothing had ever refused a Lot
for want of frontage, so *"every Building is on the Road Graph by construction"* — which `CONTEXT` →
Frontage leans on to delete the utility network entirely — was true by there being no Road Graph rather
than by construction. **As of 5a-bis something refuses**: a block with no Street on any face yields no
Lots, and `SimulationTests.Land_with_no_street_gets_no_lots` is the test. ⚠ **And the enforced sentence
is narrower than the one the corpus was leaning on** — a Building *outlives* its frontage
([`adr/0079`](../docs/adr/0079-a-building-outlives-its-frontage-and-an-address-that-has-none-is-a-hole-the-trip-model-reports.md)),
so what is true by construction is *every Building was on the Road Graph when it was raised*, and the
whole-world invariant is **every *vacant* Lot has frontage**. The stronger reading is false and always
would have been, because a player can bulldoze.

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

**⚠ A second item joined this entry on 2026-08-11, and it shares the cause rather than merely the cost.**
Both shipped Rulesets also say this, in the `[roads]` section:

> *`foot_crossing_every` IS SEVERANCE'S DIAL AND IT IS THE INTERESTING NUMBER IN THIS SECTION. … At 0 there
> are no crossings and the pedestrian network is cut in two while the car network is untouched … It is the
> one number here whose effect a test asserts in both directions.*

**Three clauses, and two are false at the values the same file sets.** At the shipped 32-Tile lattice the
dial strands **0.0%** of walkable nodes at every value in `1..16`, and at *never* it reaches 2.0% — which
is not *cut in two*. `foot_paths_per_thousand_blocks`, four lines above it and described as nothing in
particular, moves the same case from 2.0% to **43.6%** and is therefore the interesting number in the
section. The third clause was true and is now truer: a test does assert it in both directions, over eight
seeds, on `rulesets/severance.toml` — which exists **because** this claim was wrong.

- [ ] Rewrite the `foot_crossing_every` paragraph in both files: the dial is a **ratio** and what
      reconnects a city is an **absolute count of crossings**, so its effect is a property of
      `block_tiles` and `arterial_count` and not of itself. Name `foot_paths_per_thousand_blocks` as the
      co-dial. Point at `rulesets/severance.toml` for the rung where it bites.

**Two items now wait on one re-record, and that is an argument for paying rather than a coincidence.** The
entry above declines to bundle *unrelated causes* behind one hash move. These two are not unrelated: the
cause is identical — **a Ruleset comment is hashed content** — so one commit whose stated purpose is
*"correct two false paragraphs in the shipped Rulesets"* has exactly one cause behind its hash move. The
ledger's rule bites on mixing a correction with a *mechanism* change, not on batching corrections.

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

### Session F's collection — six, one paid in the sitting

[`plans/0021`](0021-trips-legs-and-the-pedestrian-layer.md) sent session **F** past four of these and told it to
route rather than work around them, per [`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md).
It found two more on the way. **None blocks anything.**

**1. `adr/0041` still says the travel-time matrix is District-granular, and it is unbannered.**
*"The matrix remains District-granular; only *attribution* leaves."* [`adr/0047`](../docs/adr/0047-routing-never-keys-on-the-district.md)
reversed exactly that — *"the travel-time matrix's granularity is the routing partition, not the
District"* — and `CONTEXT.md` → District carries the reversal. `adr/0041` **has** an amendment block and
it does not touch the bullet, which is the worst arrangement: a reader who checks for a banner finds one
and concludes the document has been reconciled. It is **5c's foundation**, and it is wrong in the ADR a
reader reaches for first. *Cause 2 — an ADR issued a write to another document and it did not land.*

**2. ~~`CONTEXT.md` has no `Node` entry, and `Node` is load-bearing.~~ PAID by session F**, because
[`adr/0074`](../docs/adr/0074-side-of-street-is-a-property-of-the-access-point-not-of-the-graph.md)
defines an **Address** as *never a Node* and a definition cannot rest on an undefined term. `Node`
appeared in the route cache key, the Rejoin Target, the Sight Horizon's floor, `adr/0040`'s pathfinding
cluster, and in the definitions of **Segment** and **Access Point** — both of which define themselves
*against* it — while having no entry at all. 5a shipped `RoadNodeTable`; the vocabulary never caught up.
**This is Cause 1 with the copy count at zero**: not two copies of a fact drifting apart, but a fact with
no copy, re-derived by each reader from the shape of its absence — the same defect `adr/0064` hit from
the other side, where a loader guard with no test was concluded not to exist.

**3. No design document owns Severance, and it is milestone 5b's stated payoff.** `06` says *"the payoff
is Severance, argued in `adr/0014` and `03 §3.7`"* — but `§3.7` argues *walking's fidelity* and the
one-graph decision, and cites Severance as evidence **for** that decision by quoting `CONTEXT.md` back at
itself. The definition exists only in `CONTEXT.md`. Under [`adr/0042`](../docs/adr/0042-a-planning-document-cites-and-a-design-document-owns.md)
a design document owns and a planning document cites, so **Severance is cited by two and owned by none**.
The natural owner is `03 §3.7`, which would have to acquire a paragraph that argues Severance rather than
using it. *Left for whoever next edits `03`; F did not take it, because adopting a mechanism into a
design document is more than a correction.*

**4. `docs/movement-primer.md` says the Microscopic Cap counts Segments.** [`adr/0062`](../docs/adr/0062-the-microscopic-cap-counts-vehicles-and-nothing-is-ever-evicted.md)
settled that it counts **Vehicles**, and the primer was written in the *same commit* that changed the
unit. Its header disclaims authority, which covers it — but *"the fourth copy drifted inside one commit"*
is precisely the failure the header says the document exists to avoid, and it is **the cheapest sighting
of Cause 1 in the corpus**: same author, same sitting, same hour.

**5. `adr/0025` and `CONTEXT.md` disagree on how many Access Points a Building has.** *(Found by F, not
on its list.)* `adr/0025`: a Building *"may hold Bins, **one Access Point**, one Parking Shed. It may
never hold a Need, money, a Provider List, or a Trip."* `CONTEXT.md` → Access Point: *"every Building has
a **pedestrian** access point and a **vehicle** access point"* — and `adr/0008`'s third consequence is the
decision that made it two. The count is incidental to `adr/0025`'s argument, which is about what a
Building may **never** hold, and that is exactly why it drifted: **a list is checked for its point and
copied for its contents.** It is the sentence a reader asking *what may a Building hold* reaches for
first. [`adr/0074`](../docs/adr/0074-side-of-street-is-a-property-of-the-access-point-not-of-the-graph.md)
states two, so the ADR corpus now disagrees with itself and a banner on `adr/0025` is the fix.

**6. `adr/0007` says walk Legs resolve statistically *"almost always"*, and nothing could ever promote
one.** *(Found by F, and routed rather than fixed.)* Session F settled the reading as **categorical** and
amended `adr/0008` accordingly; `adr/0007` carries the same hedge and **belongs to session E**
(`adr/0005` + `adr/0007`, fidelity). The correction is one word and the argument is written down in
`adr/0008`'s amendment: Stress is `volume / capacity` over **vehicles**, force-promotion fires on a
vehicular downstream, and `CONTEXT.md` → Fidelity bars pedestrians from Stress entirely — so the
expensive regime the hedge gestures at **has no door into it**. F did not take it, per `adr/0073`'s
routing rule. *Note the shape: four copies of one claim, two categorical and two hedged, and the
difference between the readings is a whole mechanism.*

### ~~`02 §1.2`'s derived table sets the calendar and the vehicles to two different clocks, 65× apart~~ — **PAID 2026-08-12**

> **All five rows below are discharged**, by the sitting that closed `0002` §A's *how long is a Tick*
> into [`adr/0082`](../docs/adr/0082-the-behavioural-clock-is-global-and-car-following-sub-steps-inside-it.md).
> `02 §1.2`'s *reads as* column is **deleted** and replaced by the fast-forward the `Day` column
> implies; the `~0.5 Tile/Tick` row is **split**, the ceiling kept and the number struck; `adr/0019` is
> **amended in place**, not bannered; and `Speed.cs`'s comment now states that a Tick's duration in
> seconds is **derived**. **No State Hash moved and no baseline was re-recorded** — the number was right
> and only the reason was wrong.
>
> ⚠ **One row was worse than this ledger recorded, and it was found by checking the arithmetic rather
> than by reading.** The `Cross-town trip` row is not merely *self-consistent with a struck number* — a
> 16.4 km crossing at 50 km/h is **112 Ticks, not 480**, so `adr/0019`'s headline *5.9% of a life spent
> driving* is **1.4%**, and the corpus has been assuming **~4× the standing traffic** the shipped numbers
> produce. **The cause is that `adr/0019` has one exchange-rate row where there are two Ticks→seconds
> rates** — the host's wall-clock one, genuinely free, and the in-world one, derived — which is why it
> believed the ratio was invariant. Re-derivation is filed to `0013` and **5b-bis**, not applied.
>
> **The general shape this entry named was right and is worth keeping**: two *derivations* of one fact
> that never met. What it could not see is **why** neither author was careless — `adr/0019` declares both
> exchange rates free, and the `[roads]` table spent one years of documents later, in another file, with
> no edit making the ADR wrong. ***A degree of freedom is spent by the first document that uses it, and
> nothing announces the spending.***

*(Found while pricing 5b's walk Leg, which needed a Tick to have a duration. Routed under
[`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md);
the question itself is [`0002`](0002-open-questions.md) §A → *how long is a Tick*. **This entry is the
document correction only** — it is owed whichever way that question is answered.)*

**Nothing shipped is wrong. The code is right and the documents are not, which is the reverse of this
ledger's usual direction and is the first thing to establish**, because the entry reads as an outage
otherwise. At `walk_speed_kph = 5` a pedestrian covers **3.66 Tiles/Tick**, and `--trips` over
`minimal.toml` reports a median walk of **1.4 min at 128 m, 10.0 min at 1,024 m and 18.3 min at
2,048 m** — each landing just under its band's ceiling, at exactly 5 km/h. **A person walks at walking
pace in this simulation today.** What is wrong is the table a developer reads to find out what that
number should have been, and the correction is owed to the table.

`02 §1.2`'s *Derived, for orientation only* table states, in one row, that at Normal speed a Day is
**8m32s** of wall clock and that traffic **"reads as ~130 km/h"**. Those are two rates for one world:

- A Day of 86,400 s shown in 512 s is a **168.75×** fast-forward. That is the pacing decision, and it
  is the same 168.75 as `Speed.cs`'s 10.546875 s Tick over a 1/16 s wall Tick — necessarily, because
  it is the same fact.
- A car whose real speed is 50 km/h, shown at 168.75×, **reads as ~8,400 km/h**. The table says 130.

**The two columns are inconsistent by 65×, and the neighbouring `~0.5 Tile/Tick` row is derived from
the wrong one of them** — it is the speed that makes a vehicle *look* right at 1× while the calendar
beside it runs 65× faster still.

**This is the exact failure [`01`](../docs/01-player-experience.md) diagnoses in another game, in our
own table, uncaught.** `01` → *time is an arc, not a clock*: *"Cities: Skylines' calendar runs 112×
faster than its own day/night cycle, which is why its players report cars taking 'weeks' to cross
town."* `02 §1.2` commits the same mismatch at 65×, three documents away from the paragraph that names
it. **`01`'s remedy is intact and this does not threaten it** — a sun arc makes no numeric claim, so
nothing shown to a player is currently lying. What is lying is a table a *developer* reads.

**The cause is a category error worth stating separately, because it is what makes the row look
reasonable.** Appearance was treated as a **constraint on the simulated speed**, when it is a
**consequence of the calendar rate**, and the calendar rate was already chosen by `TICKS_PER_DAY` and
the reference tick rate. Once a Day is 8m32s, *everything* on screen moves at 168.75× and there is no
freedom left to spend on making a car look like a car. The compensation is uniform — a car still reads
as ten times a pedestrian — so the ratios survive and only the absolute claim is false. **A speed
picked to satisfy appearance is a number bought with currency the pacing decision had already
spent.**

| Where | What it says | What is owed |
|---|---|---|
| `02 §1.2` → *Derived, for orientation only* | `Traffic reads as` — ~65 km/h at Study, ~130 at Normal, the first marked *"visually honest"* | The column is 65× low against its own `Day` column. Either restate it at the fast-forward the Day implies, or **delete it** — it is marked *orientation only*, and an orientation figure that disagrees with the row beside it orients nobody |
| `02 §1.2` → *Normative values* | `Vehicle free-flow speed` — **~0.5 Tile/Tick**, *"the car-following ceiling"* | Two claims welded together. The **car-following ceiling is real** and belongs to the Lane kernel; the **~0.5** is derived from the *"reads as"* column above and is 73× off the shipped `[roads]` speeds. Split them: the ceiling survives the correction, the number does not. **And the number cannot be rescued by scaling the pedestrian back up, which is the obvious patch** — at 0.5 Tile/Tick for a car a walker is 0.05 and a 3 km walk is **1.8 Days**, so the temptation is to break the ratio instead. **The walk-to-drive ratio is the quantity [`adr/0008`](../docs/adr/0008-walking-is-a-simulated-leg.md)'s single-currency Commute Budget exists to compare**, so a pedestrian fast enough to be plausible here is a pedestrian nobody would ever drive instead of, and Severance stops costing anything. Under this row you may have **a realistic walk or a realistic mode choice, not both** |
| `02 §1.2` → *Normative values* | `Cross-town trip` — **~480 Ticks**, *"5.9% of a Day"* | Self-consistent with ~0.5 Tile/Tick and travels with it. At that speed *"cross-town"* is **1.08 km** on a 16.4 km map, which is a District rather than a town — worth checking whether the row predates the map size |
| `adr/0019` | *"There are no seconds in the library"*; Ticks → real seconds owned by *the host*, value **"none"** | Not wrong, but **contradicted by shipped code** it does not know about. Needs a banner pointing at `0002` §A either way, and an amendment if the sub-stepping answer is taken — its derivation chain propagates one sub-model's requirement to the global tick rate |
| `src/Borough.Core/Quantities/Speed.cs` | Quotes *"There are no seconds in the library and no metres"* in one paragraph and derives `PerKilometrePerHour = 48_000` from *"a Day is 86,400 s… so a Tick is 10.546875 s"* in the next | **The file cites the rule it breaks, on the same screen.** Its defence — *"the exchange rate lives outside the simulation… nothing in a Tick calls it"* — is true of the **arithmetic** and false of the **assumption**: the conversion runs once at load, and its output then runs in every Tick |

**The shape is Cause 1 with the copies in different units.** Not two statements of one fact drifting
apart, but two *derivations* of one fact — a Tick's duration — that never met, because one lives in a
pacing table and the other in a units comment. Nothing checks that a Day's length and a vehicle's
speed tell the same story, and **the corpus's own worked example of that failure is quoted in `01`**.

### ~~`06` says Phase 2 produces commuting and that nobody is employed, two paragraphs apart~~ — **PAID**

*(Found 2026-08-12 by the sitting on [`0002`](0002-open-questions.md) §A, and **paid in the sitting** —
`06` → *What Phase 2 as written would actually produce* carries the note. Recorded here because the
**shape** is worth more than the correction.)*

`06` → *What Phase 2 as written would actually produce* says the ten milestones build Households that
*"form, look for housing, fail with recorded reasons, move in, **commute** on multi-Leg Trips, park, age
through Life Stages"*. **Two paragraphs later, the same section says the result has *"no money in it,
nobody employed, and no way for anyone to arrive."*** Both are `06`'s own, in one section, and **the
second is the true one** — no milestone in the table above it employed anybody, so the commute in the
first sentence had nothing to commute to.

**This is Cause 1 with both copies in one section, which is the cheapest sighting yet** — cheaper than
session F's *"same author, same sitting, same hour"*, because it is same author, same section, same
screen. The mechanism is the one that explains most of this ledger: **the milestone list was checked
for its ordering and read for its prose.** The first paragraph is a *summary* of the table, so a reader
verifying `06` checks the table; the second is a *judgement about* the table, so a reader checks the
judgement. Nobody checks a summary against a judgement, because neither is the source.

**The consequence was not cosmetic.** `plans/0021` typed §A as *which generator has a destination set
that exists today* and answered *none*, which was correct. It did not go on to check whether any
milestone would ever produce one — and the sentence that would have told it, in the document that owns
sequencing, was sitting two paragraphs from a sentence asserting the opposite. **A milestone was missing
from Phase 2 for as long as Phase 2 has been written down**, and it took a slice being blocked to find
it. *An internal contradiction in a planning document is not a tidiness problem; it is two readers being
told different things about what the plan will produce.*

### `adr/0041` states a mechanism that `adr/0075` has since made unbuildable, and neither ADR says so

*(Found 2026-08-12 by `plans/0021` task 5, which owed the mechanism and could not build it. Owner:
[`adr/0041`](../docs/adr/0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md); an
amendment block, not a rewrite — the decision is untouched and its **schedule** is what is wrong.)*

`adr/0041` requires a Segment's volume to be incremented *"when a Traveller enters it"*, and its S2 R2
amendment says exactly what that needs: *"direct attribution needs a **next Segment** every Tick, and a
path is only one way to supply one."* The rungs it names are a stored path and a **next-hop table**.

**[`adr/0075`](../docs/adr/0075-a-leg-is-a-plan-and-a-traveller-is-a-cursor.md) supplies neither.** A
Leg holds **a cost and no path** — on purpose, and rightly — and nothing in `Borough.Core` holds a
next-hop table. So volume attribution is not merely unimplemented; it is **not expressible** against the
structures 5b shipped, and will stay that way until a path source lands (`plans/0010` decision 11).

**What made this hard to see is that a true reason was already available for the same absence.**
`adr/0041` increments on **vehicular** Legs only and 5b resolves walk Legs only, so `plans/0021` recorded
volume as *"nearly vacuous in this slice by construction"* — correct, and load-bearing on nothing.
**A sufficient reason that happens to be the wrong one is worse than no reason**, because it predicts
that adding a vehicle fixes it, and the next slice will believe that.

**The shape is Cause 2 in reverse.** Cause 2 is *an ADR issues a write to another document and the write
does not land*. Here `adr/0075` issued a write it did not know it was issuing: **a decision that removes
a representation defers every decision that reads it**, and a consumer three ADRs away has no reason to
be re-read. Nothing in the corpus asks *what else was reading the thing I just deleted*.

### A world's seed has two sources, and the doc comment that says this is filed here is what filed it

*(Found 2026-08-12 by `plans/0023` task 5, which added the second source, and by the user asking on the
same day what needed reviewing. Owner: `src/Borough.Core/Entities/World.cs`; a signature sweep, not a
design change.)*

`World` now holds a `Key` — `Randomness.Draw`'s first coordinate — because `CommuteRoster` is
`(derived AND rebuilt)` from it and `RebuildDerived()` takes no arguments and must not start taking
them. **Every other mutator on the class still takes a `WorldKey` as a parameter**: `CreateBuilding`,
`DestroyBuilding`, `Adopt` and the rest, nine call sites in all. So one world has **two sources for one
seed**, and nothing checks that they agree — a caller passing a different key would make the arming
stagger disagree with the commute roster about which world it is in, and both would look correct in
isolation.

**It is redundant rather than wrong today**, because every live call site threads the same key. That is
exactly the state `plans/0012` exists to catch: a fact stored twice, currently consistent, with no
mechanism keeping it so.

⚠ **The reason this entry is worth its own heading is the way it nearly did not get written.** The
`World.Key` doc comment says the sweep is *"filed to `plans/0012` rather than done here, because a
signature change across nine call sites in the middle of a milestone is how an unrelated defect gets
committed under a feature's name."* The reasoning is right and **the filing did not happen** — the
comment shipped, the entry did not, and for two commits the codebase asserted in prose that a ledger
somewhere held this. That is `adr/0073` failing at its own first step: *route the finding on the day,
and before working around it*. **A citation is not a filing, and the document that would have caught it
is the one being cited.** The only thing that found it was somebody asking what was outstanding.

### The developed density every map decision is priced against is circular, and nothing says so

*(Found 2026-08-13 by session P while grilling `01 §4`, and it is a **correction to how a figure is
cited**, not to the figure. Owner: [`adr/0089`](../docs/adr/0089-the-map-is-sized-by-how-many-commutes-fit-across-it.md)
and `CLAUDE.md`'s Map row.)*

**3,700 people per km² is quoted as the design's developed density and it was obtained by dividing the
target population by the old map's area.** 1,000,000 ÷ 268 km² = 3,731. So it is not an independent
figure that the map was checked against — it is **the map**, restated in different units, and every
sentence of the form *the density is unchanged, so the map can grow* is using the old map to justify the
new one.

**The number is not thereby wrong**, which is why this is a citation defect rather than a measurement
one. Two figures derived from the build bracket it: `[lots] lots_per_segment = 5` over 5a's 33,024
Street Segments gives 165,120 Lots on 268 km², and `World`'s independently-chosen 225 Lots per 1,000
Citizens gives 225,000 — **2,738 and 5,136 per km²** at the shipped occupancy, with 3,731 between them.
So the figure survives; what has to stop is quoting it as evidence.

- [x] **`adr/0089`** — say where the density came from wherever it is cited as a check, and cite the two
  bracketing figures instead. The ADR's conclusion does not move. **Done 2026-08-13.**
- [x] **`CLAUDE.md`'s Map row** — *"1M and the 3,700/km² density are unchanged"* reads as two independent
  facts holding and is one fact stated twice. **Done 2026-08-13.**
- [x] **`05 §1`'s budget block and `adr/0085`** — ⚠ **neither was in this list**, and both were found by
  check 6 when the figure was registered. `05` states the derivation `map_area × mature_density ×
  buildable_fraction` with the density as an *input*, which is the circularity in its most load-bearing
  form; `adr/0085` divides 3,700 back out of 268 km² as though that confirmed something. **Done
  2026-08-13**, and the fact that a hand sweep found half the sites is the argument for the check.

⚠ **This is *Cause 5* in its pure form** — the qualifier **is** written, in `plans/0002` §D and in
`docs/spike-results.md`, and four other documents quote the digits without it. *(This paragraph said the
qualifier was never written, until 2026-08-13; the sweep had not looked, which is the same failure one
level up.)* It reads like *Cause 1* and is not: the copies cannot disagree, because one is
arithmetic on the other, so **the tell that usually finds Cause 1 is absent by construction** and it took
a third document (the two build-derived figures) to notice. *A derived copy that can never drift is the
one no drift check will ever surface* — and the reason it did damage is Cause 5's: the figure travelled
into three decisions as **evidence**, with nothing in the quotation to say it could not bear that.

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

4. **Nothing is blocked on a session or a spike round that has closed.** Cause 3's check, and it is
   greppable in the same way check 1 is: the blocking documents write the gate down — *"gated on
   session **M**"*, *"blocked on **S1**"* — and the board's *Done* tables say which sessions and rounds
   have closed. Cross them. Unlike checks 1–3 this one has **no false-negative cost worth managing**:
   the corpus's whole set of gates is small enough to enumerate, and the failure it catches is a piece
   of work sitting available and unnoticed for an unbounded time. **It would have fired on S2 the day
   session M closed.**

5. ~~**`0002` §F2 has a row for every file in `docs/adr/`.**~~ **BUILT the same day** —
   `tests/Borough.Tests/Corpus/CoverageMapTests.cs`. It is the **sibling of
   check 1 pointed the other way**: check 1 asks whether an ADR is cited from outside `docs/adr/`, this
   asks whether the corpus's one **coverage map** knows the ADR exists. Same directory, same shape of
   test. **It asserts only the row, never the mark** — *State* and *Note* are judgements no test can
   make, and the whole value is that an unassessed decision then reads as **unexamined** rather than as
   **absent**. Grouped rows are ranges (`` `0023`–`0027` ``) and are expanded, so `0028` being reserved
   and unwritten is correct rather than a false positive. **Filed because §F2's own drift box wrote the
   trigger and the trigger fired**: the map was rebuilt on 2026-08-10 on the finding that it had
   *stopped at `adr/0043`*, stopped at `0059` by that evening, and the box then said in as many words
   that a third stop meant *"the map wants generating from the directory rather than writing"*. On
   2026-08-12 it was found **stopped at `0070`**, missing `0071`–`0081` — slice 5a, session F, slice
   5a-bis and the §A sitting, four pieces of work, none of which added a row — while the header went on
   claiming *"69 written, numbered to `0070`"*.

   **Only the ADR column is generable, and that is the whole value of the check.** *State* and *Note*
   are judgements and must stay hand-written; what the generator supplies is the **row**, so a decision
   nobody has assessed reads as **unexamined** rather than as **absent**. That distinction is what the
   map exists to make, and a missing row destroys it silently — `adr/0043` cites §F's 🟢 marks as
   evidence, and a map that omits eleven ADRs *reads as complete*. **Three observations of one
   mechanism now agree**: a hand-maintained index of a directory is maintained by whoever remembers,
   and over four sittings nobody did, three times running. **This is Cause 1 with the second copy
   missing rather than wrong** — the failure mode `adr/0064`'s missing loader test had already
   demonstrated, where a fact with no copy at all is re-derived from the shape of its absence.

6. ~~**A markdown table renders, and emphasis is `*asterisk*`.**~~ **BUILT 2026-08-12** —
   `tests/Borough.Tests/Corpus/MarkdownStyleTests.cs`. **The odd one out in this list, because it
   checks the corpus against *itself as rendered* rather than against another document or the source**
   — and it is here because a defect of that class had gone undetected for an unknown time in the most
   read file in the project. **The board's *Do these next* table was split by seven blank lines**, so
   everything from row 2 onward rendered as literal pipe text; `0002` §F2 was in **three** fragments,
   two orphaned by an interleaved blockquote. **Nobody saw it, because in a plain-text read the rows
   look perfect** — the corpus is written and reviewed as text and consumed as HTML, and nothing had
   ever compared the two.

   **The rule is stated as *every run of table rows begins with a header and a separator*, not as *no
   blank line inside a table*.** The second phrasing cannot see the blockquote case, whose fragment is
   not adjacent to a blank line at all — so the invariant is asserted rather than the symptom, which is
   the same move `adr/0063` made on the wake predicate. The emphasis half was **checked against the
   corpus before being imposed on it**, as check 1's own note requires: four exceptions in 118 files,
   all `_Avoid_:` lines in `CONTEXT.md`, normalised rather than exempted.

   **What it is really guarding is not style but *invisible churn*.** A global `formatOnSave` sent the
   board through Prettier on 2026-08-12 and rewrote ~150 lines of emphasis inside the same diff as a
   deliberate 999-line deletion. **Prettier cannot be configured to house style** — emphasis is not an
   option in it — and its table padding takes the board from **82,450 to 182,405 bytes**, because it
   pads every cell in a column to the widest and that table holds a 4,166-character cell. Hence
   `.prettierignore` and `[markdown]` in `.vscode/settings.json`, both of which carry the measurement
   as their comment so the next person does not re-derive it.

7. **Every invariant `02 §10` names has a member of the `Invariant` enum, and every member is either
   registered or explicitly marked unbuilt with the milestone that owes it.** **Filed 2026-08-12 by
   session H** ([`adr/0084`](../docs/adr/0084-parking-occupancy-is-two-checks-and-an-invariant-over-absent-state-cannot-be-written.md)),
   which found *parking occupancy is conserved* specified in **four** documents — `adr/0009`, `02 §10`,
   `05 §60` and `06`'s milestone 8 risk — and built in **none**. **This is check 5's shape pointed at
   the invariant tiers instead of the ADR directory**, and it is the *third* observation of the same
   mechanism: `HouseholdHomeExists` was reported by nothing and found only by an audit; `adr/0033`'s
   satisfiability invariant sat specified across three documents until session N built it and it found
   a live defect in the committed golden baseline **within minutes**; and this one is at four and
   counting. ***An obligation with no member reads as absent rather than as owed***, which is exactly
   check 5's argument about a missing row.

   **Two design notes, because the naive version of this check is a fifth copy of the obligation and
   would therefore be the defect it is meant to catch.** The list must be **read from `02 §10`** or the
   enum must carry the marking — never a hand-written array in the test mirroring the document, which
   is `plans/0012` *Cause 1* arriving inside the instrument. And the *unbuilt* marking is the load-bearing
   half: `HouseholdHomeExists` shows the project already knows how to keep a member visible while it is
   not live (retired, `[Obsolete]`, **id never reused**, because an id travels in a crash artifact), so
   the same technique carries an invariant that does not exist yet. ⚠ **It must not force the member to
   be *written* early**: `adr/0084` finds that an invariant over **absent** state cannot be written at
   all — *zero is a value; undefined is not* — so what the check asserts is that the gap is **declared**,
   not that it is closed.

6. ~~**A distinctive figure appearing in more than one document carries the same qualifying clause in
   each.**~~ **BUILT the same day it was specified, in a different shape, because the specified shape was
   measured and refuted** — `tests/Borough.Tests/Corpus/DisqualifierTests.cs`, reading the registry in
   *Cause 5*. **What ships asserts that a registered trap figure never appears without its exact
   disqualifying phrase.**

   ⚠ **Three measurements killed the original and each is worth keeping**, because this is the first
   check in this list to be specified by argument and refuted before it was written — `adr/0043` reaching
   the instruments rather than the design.

   **① The number did not travel exactly.** `plans/0013` holds **186,624**; what `adr/0094` quoted was
   **~186,600**, rounded once in `plans/0002` and quoted from there. An exact-digit check over
   distinctive figures **would never have fired on its own motivating case**. *The precision fell with
   the caveat, which in hindsight is the sharper tell and is not one a string comparison can see.*

   **② The obvious repair is worse than the disease.** Normalising to three significant figures does
   catch it, and it also collapses `1017`, `1020.92`, `1021` and `1024` into one group — **107 such
   groups across the corpus**, nearly all coincidental collisions between unrelated measurements. A
   check that noisy gets switched off, which is worse than not having one.

   **③ The caveat was not dropped, it was substituted**, and that is what defeats every *generic*
   detector. `plans/0002` does not quote the figure bare: it carries *"R2's uniform draw is the
   longest-trip distribution available"*, a real caveat, correctly stated, and **not the one that
   mattered**. Anything asking *is there a qualification nearby* passes it. So the check has to pin the
   **particular** disqualification, which is what a registry does and what detection cannot.

   **The reporting-only variant was considered and refused.** A file listing all 255 four-plus-digit
   figures that appear in more than one document is where the next registry entry would come from, and
   it is also a 255-line artefact nobody reads, rotting in the repository — which is the failure this
   whole cause is about, committed by the instrument built to catch it.

Neither is a substitute for the restructure. A check over three tables that disagree only tells you
they disagree; the point of thinning is that there is one place to be right. **Checks 4 and 6 are the
exceptions to that sentence**, for opposite reasons — thinning cannot help check 4, because its
documents do not disagree with each other but are all stale against a third thing neither stores; and it
cannot help check 6 either, because there the documents are both *correct* and the defect is in what one
of them declined to copy.
