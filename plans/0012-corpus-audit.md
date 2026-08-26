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

## The diagnosis, which is two things and not one — **and a third, a fourth, a fifth and a sixth were added later**

The sweep expected one failure mode and found two. They want different answers, and conflating them
is why *"tidy the documents"* has never worked as an instruction.

**Cause 3 was added on 2026-08-11 and was not found by the sweep**, which is itself worth noting: it
was found by a spike round tripping over it. The sweep reads documents against each other and catches
facts that disagree; Cause 3's documents **all agree**, and all three are wrong together.

**Cause 4 was added on 2026-08-12 and the sweep could not have found it either**, for a sharper reason:
its disagreement is not between two documents at all. It is between a document and **the code**, and
nothing in this corpus checks that — `CitationTests` checks links resolve, `CoverageMapTests` checks rows
exist, `MarkdownStyleTests` checks markdown renders, and **all three are document-to-document**.

**Cause 7 was added on 2026-08-22 and it is Cause 4 running BACKWARDS.** Cause 4 is *a decision taken
from a description of the code, where the description is wrong*. Cause 7 is ***a description taking its
NOUN from the code, where the code is behind the design*** — so the sentence is an accurate report of
an implementation the corpus has already decided to replace. **It was found the same day it was
written**, in an ADR written that morning, and the tell is that the record contradicts *itself* two
paragraphs apart rather than contradicting anything else.

**Cause 6 was added on 2026-08-21 and no sweep of this corpus could ever have found it**, because it
is not in the corpus: it is in a **doc-comment**, and a doc-comment is the one place a description of
the build lives that no document-to-document check reads. It took a **code-against-code** test, and on
the day that test was written it failed on **forty** sites.

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
| **6** | a **description** is filed under the wrong declaration — two `///` blocks, one member | **nothing**, and no check this corpus had could see it: the defect is in a **doc-comment** and every other check is document-to-document | a code-against-code test — `DocCommentAttachmentTests`. Found **40** sites in 31 files the day it was written |
| **7** | **two documents claim ONE ordinal**, on branches that have not met | **nothing, and git makes it worse** — the filenames differ, so the duplicate merges with no conflict | ✅ a check that compares numbered files *to each other* — `PlanIdentityTests.No_two_numbered_documents_claim_one_ordinal`, 2026-08-24 |

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

#### ✅ Check 9 — ledger citation. **BUILT 2026-08-14**, `tests/Borough.Tests/Corpus/LedgerCitationTests.cs`

**Green on the day it was written, at ten qualified references, and verified in both directions** —
reinstating the defect below makes it red, correcting it makes it green again.

⚠ **It does not check what this section filed, and the change is the point.** The filing says *against a
ledger entry **not struck***, and **strike status is a prose judgement a machine cannot make**: an ADR
that *closes* an entry and one that merely *names* it read identically to a regex, so a check on
strikethrough would be red wherever an ADR cites an entry it did not settle. What is mechanical is the
**inbound citation** — entry `#N` must name the ADR back — which is **Cause 2's actual repair** rather
than a proxy for it, and which catches the filed shape as a special case: an entry that was closed and
never touched cannot cite the ADR that closed it.

**Measured before it was built**, per check 8's precedent. **Twenty-four numbered ledger references across
the ADR corpus, ten of them qualified by a named source, of which eight passed and two were one live
defect** — so it is a ratchet on the day rather than a cleanup project.

⚠ **The defect it found is `adr/0098` naming the wrong ledger, and it had reached six documents inside one
day.** The ADR cites *`01 §8` ledger **#3*** for *is car ownership a choice?*, which is `01 §8`'s
**second** entry; its third is *open map or progressive land unlock*, closed two days earlier by
[`adr/0090`](../docs/adr/0090-the-generator-makes-land-and-the-player-makes-every-road.md). Corrected in
`adr/0098`, `CLAUDE.md`, [`06`](../docs/06-roadmap.md), [`0000`](0000-board.md), [`0002`](0002-open-questions.md)
and [`0026`](0026-statistical-resolution-and-the-travel-time-matrix.md).

⚠ **Why it could not announce itself is worth more than the correction, and it is Cause 5 on an
*identifier*.** [`0002`](0002-open-questions.md) holds **two** numbered ledgers sharing one namespace —
the four-entry map-and-endgame list, and *Design forks, by owner*, which runs to `#29b` — and `01 §8`
holds a third. So **`ledger #3` resolves to a real but different question in each of the three**, and
*Design forks* groups its entries under the **owning document's name**, which is exactly what invites a
reader holding `plans/0002`'s number to write `01 §8` in front of it. ***A bare `#N` travels freely and
lands on something that exists***, which is Cause 5's tell — worse than nothing — arriving on an
identifier rather than on a quantity. **The repair on the writing side is the same as Cause 5's**: name
the ledger, never the number alone.

⚠ **A second defect it does *not* see — ✅ PAID 2026-08-14, in the sitting that found it.** `0002`'s
*Design forks* **#3**, the entry `adr/0098` half-closed on the substance, still read *"Live, and
half-answered"* from session five and named no ADR; it now carries the half-closure, what was built, and
the transit trigger the endogenous half keeps. **The lesson survives the repair and is the reason this
paragraph stays**: check 9 was blind to it because no ADR cites `plans/0002 ledger #3` for car
ownership; the citation that would have made it visible is the one that was wrong. ***A check keyed on a
citation cannot see an entry nobody cited***, and that is the standing limit of every document-to-document
check in this file.

**Two scope decisions, both earned from check 8.** **Only qualified references are checked** —
**fourteen of the twenty-four name no source within reach of the number**, and with three ledgers sharing
a namespace a bare `#N` is *genuinely ambiguous* rather than merely terse, so resolving one would be
guessing, and a strong check must not fail for a weak reason. And **an entry numbered in more than one
list within a source passes if any of them cites the ADR**: the question is whether the write landed, not
which list it landed in.

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

### 🔴 Why a defect SURVIVES is not why it was written, and this ledger had never separated the two

**Every Cause below is a mechanism by which a defect gets WRITTEN.** Status copied to three places,
an ADR's write that does not land, a number quoted away from its sentence — each names how a wrong
sentence came to exist. ⚠ **None of them says why nobody caught it**, and the ledger has been
accumulating that second thing inline, under whichever Cause happened to be next, for as long as it has
existed. ***It is a different axis and it is not a Cause 8***, because a survival property is not a way
of writing something — **it is a coat any Cause can wear.** Two are known:

**1. Overdetermination — a statement wrong in TWO ways reads as a considered alternative.** Raised by
the concurrent milestone 24 session, 2026-08-24, and it is the better half of what it found.
`CONTEXT.md` → Water Body and [`adr/0034`](../docs/adr/0034-fields-are-sorted-by-source-geometry.md) §4
both carried *"a Bin holding the **Waste family**"*. 🔴 **That sentence is wrong twice**:
[`adr/0031`](../docs/adr/0031-one-resource-abstraction-and-depth-not-count.md) leaves **three** families
— Good, Utility, Money — so there is no Waste family at all; **and** Waste is settled as a **Good** in
that ADR's own words, *"Waste answers itself — it needs a Vehicle, so it is a Good"*, so the sentence
also picks the wrong one of the two that could have been meant. ⚠ **Being wrong twice is what saved
it.** A single wrong word reads as a slip and invites a check; ***a phrase that is confidently wrong in
two dimensions at once reads as somebody's deliberate position***, and a reader's next move becomes
*find the argument for this* rather than *check this*. **Three sessions looked straight at it.** The
session that found it had built an entire `references.md` §10 survey before noticing `CONTEXT.md`
already held the correct split two hundred lines away. ⚠ **The underlying Cause was plain Cause 1** —
two copies, one edited, neither aware of the other. ***The concealment is the part that cost three
sessions.***

**2. No instrument — the claim's shape is outside what any check can see.** Filed twice on 2026-08-24
from opposite directions. `adr/0145` argued from a `BusinessTable` column that does not exist, and
`CitizenTable.Employment` is `Saved` — therefore **hashed** — with no writer or reader anywhere in
`src/` ([`plans/0041`](0041-the-business-is-a-thing-the-city-contains.md) **G29**). ⚠ **Every corpus
check is document-to-document**; `RefusalCountTests` is the sole document-to-code exception and it
counts one construct in one file. ***So a prose claim about a table's contents and a saved column with
no writer are the same hole seen from two sides***, and neither has an owner.

⚠ **The practical use of this section is what it changes about a sweep.** Reading for defects means
reading for *sentences that look wrong*. **Both survival properties defeat that** — the first by looking
right, the second by being unreachable — so ***a sweep that only reads prose finds neither.*** The
first is caught by checking a confident claim against its cited ADR **even when it reads as settled**;
the second is caught only by reading the code the prose describes.

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

> ---
>
> **A fourth form, found 2026-08-21 by milestone 11 task 4: the copy that drifted was a TEST.** Every
> sighting above is a document drifting from code, or code from a document. This one is
> `InputLogCodecTests.Every_declared_verb_survives_the_round_trip`, a `[Theory]` over **four**
> hardcoded `[InlineData]` verbs, written when `CommandKind` had four. The enum reached **seven**;
> `Populate` (milestone 5a) and `Trip` (milestone 5b) were declared, applied by `Simulation`, and
> **unknown to `InputLogCodec` in both directions** — `Write` threw *a command with no verb cannot be
> written* and a hand-written `trip` line was refused as *not a verb this format knows*. So a session
> containing the verb the whole of milestone 5b exists to exercise **could not be written to a log**,
> and a log is what a crash artifact is made of.
>
> **A test that enumerates its cases is a second copy of the switch it checks, and it drifts the way
> any second copy drifts.** The test's *name* already claimed the whole set — *every declared verb* —
> so the drift was visible in the file for two milestones and read as covered. ***A test named after a
> universal and written as a list is a list wearing a proof's name.***
>
> **Repaired by reflecting over the enum**, so the next verb declared is in the test before anybody
> writes a line of it. The habit is `adr/0093`'s one level in: **where a test asserts a property of a
> declared set, enumerate the set rather than the cases** — and where it cannot, its name must say
> which cases.

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
| **`plans/0002` §D2**, written 2026-08-13, found 2026-08-14 | S2 R1's entry-error sweep, **24.70%–3.80%** | ⚠ **none — the clause that was written is *wrong*.** §D2 says the sweep *"was measured with the store in the denominator and on the District axis"*; `MatrixReport.MeasureError` divides by the **per-query A\* cost** and the route store is a separate size table, while the harness's partition is a Cell-aligned grid over nodes and is geometrically the routing partition | nothing yet — **5c task 1 caught it before quoting it**, which is the first time the tell fired before the damage. The three real disqualifiers had never been written: a **uniform** O-D draw (R4: *a different city*), **pre-`adr/0094` Ticks** at 8192 a Day, and **car** times against foot rungs. Corrected in §D2 and `RoutingPartition.DesignEdge` |
| ⚠ **`plans/0013`'s routing row**, found 2026-08-16 by session **T** | the **routing bill**, ≥17.8 ms, quoted as the ledger's whole sum | not a caveat — a **correction**. `adr/0094`'s ×4 was stated in `01 §1`, in `CLAUDE.md`, **and twice inside `plans/0013` itself**: the *volume attribution* row was re-derived for it on 2026-08-14 and a sidebar on `adr/0061` stated it on 2026-08-13 | the routing row was never multiplied, so the document that **owns** the sum under-reported it by ~2.6× for three days while two views of it were right. Corrected: **≥44–50 ms** |
| ⚠ **`0d8b114`'s merge subject**, committed and found 2026-08-19 | **the State Hash is unchanged** — a claim rather than a figure | *"the golden world still reproduces its committed hash"*, which is true of the **test** and silent about **`main`**: the baseline file arrived in that same merge, already re-recorded on the parking branch for milestone 7 | It shipped as the message of the merge that moved `main`'s hash from **`0x4D7675CF9217B955`** to **`0x817C9B00CA65113D`**, so the one commit a later reader would search for the move is the commit that denies it. ⚠ **`tests/Borough.Tests/Golden/README.md` → *Re-baselining* asks for the opposite at step 4** — *say why, in the commit message: what moved, whether it was intended, and which document authorises it* — and forbids at step 5 the pairing a merge creates by construction. ***A green baseline test proves the committed number matches the built world; it does not prove that number is the one that was there yesterday.*** Recorded rather than repaired: the merge stands and [`0031`](0031-parking.md) now carries the move |
| 🔴 ⚠ **`plans/0002` §D1's blight census**, written 2026-08-25, found 2026-08-26 | **313 built, 586 abandoned, 475 vacant** on `declining.toml`, quoted as **at 1,000 Citizens** | ⚠ **none — the population beside the number is simply WRONG, and the number is right.** `Options.cs:200` defaults `--citizens` to **`10_000`**; the runner was run bare and the census filed against a world ten times smaller than the one measured | it reached `rulesets/declining.toml`'s header and `CLAUDE.md`'s repository map with the same wrong population, so **three documents agreed to the last digit and none of them named the city.** ***The Lot total is what gives it away and no reader would have looked***: 313+586+475 = **1,374**, and a 1,000-Citizen world has **134**. ⚠ **It is the SECOND Cause 5 error on this one figure** — the row already records the share being derived on paper as *roughly 30%* and refuted at 65% by the first instrument to measure it. ***First the number was reasoned instead of measured; then the measurement was filed against the wrong world.*** ⚠ **The damage is that every before-and-after comparison drawn against it was invalid**, including two taken during milestone 17's own threshold change, which read a 1,000-Citizen *after* against a 10,000-Citizen *before* and reported a smaller gain than the change actually made (39% against 65%, not 44%). **The writing-half repair is this Cause's own rule arriving on a dimension nobody had applied it to**: *name a number after what it measures* has always meant the quantity, and a population is part of what a census measures. Corrected in `plans/0002` §D1; `declining.toml` and `CLAUDE.md` owed |

> ⚠ **That last row is a third form of this Cause and it is the one no check can reach.** The first two
> forms are a **caveat** left behind when digits are copied, and a **compressed caveat** acquiring a new
> meaning when it travels. This is a **correction** left behind when a premise expires:
> ***a correction attached to a number does not travel with it any more readily than a caveat does.***
>
> **Two things make it worse than the others.** The **owner** was the wrong copy — Cause 1 is normally
> *the copy nobody owns is the one that drifts*, and here two views were right while the source was
> wrong. And **both copies live in one file**, so no document-to-document check could have found it,
> which is every mechanical check this corpus has. The only instrument that would have caught it is
> somebody re-summing a table by hand, which is what session T did and what nothing schedules.
>
> It is also 5c task 6's own finding turned on the document that recorded it: ***a premise that expires
> retires every site resting on it, and finding one of them is not finding them.*** Nine days.

⚠ **The fifth sighting, 2026-08-14, is the first where the qualifier that existed was *itself false*, and
it is a Cause 5 error sitting inside a Cause 5 entry.** §D2 was written to stop somebody quoting R1's
curve, and the disqualifier it reached for named the wrong measurement — the route store was never in
that denominator, and the axis was never deleted. **The three that would have bound were absent.** So a
reader doing exactly what this file asks — read the sentence, not the digits — would have declined to use
a curve that is usable, for a reason that is false, while remaining exposed to three that are not
written. ***A caveat that is wrong is worse than no caveat, because it is the reason nobody writes the
right one*** — which is `adr/0093`'s *a false description of a guard* arriving on this cause rather than
on its own, and the two rules are converging: **name what a number measures, never what it is
disqualified by.** *"Uniform-draw, car-time, 8192-Tick"* is three greps against the harness; *"the store
was in the denominator"* took 600 lines of reading to refute. It is also the **first sighting caught
before the damage**, and what caught it was the working rule that a claim about the build is checked
against the build — so the tell can fire early, given somebody who reads the source the caveat is about.

**~~Three~~ ~~Four~~ Five sightings, and the middle one is the odd one out.** The first and the third are the **pure
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

⚠ **The fifth and sixth sightings both landed on 2026-08-14, in one task, and they are the two
directions of the same cause.** 5c task 4's brief in [`0026`](0026-statistical-resolution-and-the-travel-time-matrix.md)
said *"no TTL rotation — R5.5.4's rotation was the shed's answer and `adr/0083` explicitly declines to
carry the parameter across"*. **R5.5.4 rotated the route cache**, resident population 412, and
[`adr/0012`](../docs/adr/0012-routing-intent-lives-in-the-agent.md) says of that number *"0.40 forced
refreshes per Tick is affordable there and **stays**"*; [`adr/0083`](../docs/adr/0083-a-sheds-use-is-the-arrival-query-and-a-stale-shed-is-wrong-by-a-bounded-walk.md)
is the **Parking Shed**'s and declined to *take* it. ***A refusal to import a number is not evidence
the number was wrong where it was measured*** — and the brief inverted the arrow, which is a new
failure mode for this cause: not a number arriving without its clause, but a **clause arriving without
its subject**.

**The sixth is the expensive one and it has no writing rule that would have caught it.** `adr/0047`
moved route storage out of the travel-time matrix because S2 R1 measured a **4.06 GiB** route store,
and sent the routes to *"the route cache"*. Building that cache measured the same cost waiting there:
**~4 GB at 1M**, because a pair-keyed store's hit rate is `store ÷ distinct pairs` and distinct pairs
are **0.30 × population**. Nothing was misquoted; the number was correctly disqualified as *the
matrix's* problem and correctly moved. ***A cost that was moved is not a cost that was removed***, and
this cause's registry is a registry of **figures**, which cannot hold a *destination*. **The rule this
adds is about deletions rather than quotations, and it is [`adr/0089`](../docs/adr/0089-the-map-is-sized-by-how-many-commutes-fit-across-it.md)'s
own — *the obligation a deletion creates is a re-derivation, not a retraction* — arriving on a
**relocation**: when a decision removes a cost from one structure by moving it to another, the
obligation it creates is a measurement **at the destination**, and no document in this corpus records
one.** Filed to [`0002`](0002-open-questions.md) §C.


⚠ **The seventh sighting is the sixth's own repair, written the same day, wrong in four places at
once — and it is a *projection* rather than a quotation.** The amendment above originally carried a
memory figure, *~4 GB at 1M*, built from numbers this session had itself measured an hour earlier.
Every input was real and every one was used outside what it measures:

| Used as | What it actually was |
|---|---|
| route length at 1M | a fitted `√population` curve through **five points** spanning 16× |
| bytes per route | the **maximum** route in the draw, where memory scales on the **median** — 26 against 8 |
| workers at 1M | the employment ratio of `rulesets/minimal.toml`, **a file whose header says it models no city** |
| routes that must exist | **distinct pairs over a Day**, which is a *cache working set* and not a live count |

**The general form, and it is not this cause's reading rule:** ***an extrapolation is a claim about a
mechanism, not about a curve.*** A fitted curve says what a quantity *did* over the range measured; a
projection asserts that nothing outside that range stops it. Here something did — **the Commute Budget
caps a foot route absolutely**, at 50 minutes × 5 km/h = 4.17 km ≈ 32 blocks, however large the city
grows — and the fitted exponent ran straight through the ceiling and out the other side. **The check is
one question asked before the multiplication: *what would stop this growing, and have I looked for
it?*** Nobody had, because a curve through five points looks like evidence and a ceiling looks like
nothing at all.

**Two corollaries worth carrying separately.** ***A maximum and a median are different numbers about
the same distribution, and which one belongs is decided by the consumer*** — memory takes the median, a
fixed stride takes the p90, a correctness bound takes the maximum — and the reading to hand was the
maximum because that is what an earlier test happened to print. And ***a ratio measured off a fixture
that disclaims being a city is a property of the fixture***, which is `plans/0013`'s *a unit cost is a
hypothesis until a real world has produced one* arriving on the **denominator**; filed to
[`0002`](0002-open-questions.md) §C in its own right, because that ratio had by then been used three
times in one afternoon.

⚠ **The tell that would have caught it is cheap and was available throughout: the figure had no
consumer.** Nothing was blocked on route memory at 1M, no decision turned on it, and no document had
asked for it. ***A number produced because it was producible is a number nobody will check.***

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
last row is the demonstration: ~~≥229%, ≥114%, ≥57% and ≥29% are **≥17.8 ms**~~ **≥44–50 ms, re-summed
2026-08-16 by session T** over four candidate budgets, and reading them as four results is reading one
measurement four times. ⚠ **The struck figure is this Cause's own third form caught inside this Cause's
own paragraph**: `adr/0094`'s ×4 reached `plans/0013`'s volume row and never its routing row, the
correction was stated in that file's own sidebar, and *a correction attached to a number does not travel
with it any more readily than a caveat does* — so the sentence teaching the rule went on quoting the
uncorrected sum for three days. ***The instrument that finds this is somebody re-summing the table by
hand, and nothing schedules that.*** **The failure mode is specific
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

⚠ **The registry cannot hold a low-precision figure, and that bound was found by trying.** On
2026-08-19 milestone 7 task 4's two provisional parking costs — **6.40 µs** and **1.58–1.63 ms** — were
registered here and check 6 failed instantly against **nineteen** documents. None of them was quoting a
parking cost. `Mentions` is a bare `text.Contains` with no word boundary, so the spelling `6.4` matched
*"the full **16.4** km map width"* in `02` and *"**6.4×** past budget"* in `adr/0036`, and `1.63` matched
*"moves the pass **1.63–1.75×**"* in `adr/0016`. ***A substring match on a short decimal is a match on
arithmetic, not on a citation.*** Every row this table has ever held is a long distinctive figure —
`186,624`, `532,750`, `861.87` — and **the reason was never written down**, so the first attempt to add
a short one read as a corpus-wide violation rather than as a registry that could not express the claim.

**The rows were withdrawn rather than the check weakened**, and the repair went to the number's own home:
[`0013`](0013-tick-budget.md) marks both cells *PROVISIONAL, do not quote* in the cell itself. That is
weaker — it is prose, and prose is what Cause 5 defeats — so it is recorded here as a **gap in check 6's
reach** rather than as a discharge. The figure a registry can protect is one whose digits are their own
citation, and a three-digit decimal is not. ⚠ **Do not repair this by padding the figure with units or
context** into the Figure column: `Mentions` tests the raw string against prose that is written freely,
so a longer key does not match less, it matches nothing.

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

### Cause 6 — a description is filed under the wrong declaration

**Added 2026-08-21 by `06` milestone 11 task 2**, on a sighting the same milestone's task 1 committed
one day earlier and a human caught by reading the diff.

**Two `///` blocks with no member between them both bind to the one member that follows.** So the
member above the pair silently loses its documentation to its neighbour, and the neighbour starts
carrying a description of something it is not. There is no third outcome — a doc block either
documents the declaration under it or it documents the wrong one.

⚠ **The tell is nothing, and it is nothing in a stronger sense than Cause 4's.** The compiler does not
warn: a duplicate `<summary>` is legal C#, and a doc comment is not even parsed unless documentation is
being generated. Every mechanical check this corpus owns is document-to-document, and this lives in a
**doc-comment**, so none of them has ever been able to see it — `RefusalCountTests` was the first
document-to-*code* check and it reads one number.

🔴 **The scale is the finding.** Task 2 wrote the check and it failed on **forty** sites across
**thirty-one** files: **eight** where a rewritten block had been left stacked on the old one, and
**thirty-two** where a member had genuinely lost its documentation. Two were actively misleading —
`Ruleset.cs` had the `[jobs]` table's documentation attached to `TrafficRuleset`, and
`WorldInvariants.cs` had *"Every Rule Instance is in exactly one queue"* attached to
`NoBuildingRunsRulesItsKindDoesNotDeclare`. ***A defect nothing can see is found once per reviewer, for
ever.***

**Where it sits against the other five.** It is Cause 5's shape with a different unit: Cause 5 is a
**number** quoted away from its qualifying sentence; this is a **whole description** separated from its
symbol. Both leave two artefacts that are individually correct — the prose is true, the member is
real — and the defect is in the binding between them. And it is `adr/0093` inverted: that record says
*a description of the build is where to look*, and here the description points at the wrong place while
reading as though it points at the right one.

**Repair, both halves.** `DocCommentAttachmentTests` holds the tree: no member carries two doc
comments, and every doc comment closes the tags it opens. It is **code against code**, reading `src/`
and `tests/` from disk. ⚠ **The second half was found by the sweep the first half paid for** — six
blocks had a `</remarks>` typed where a `</para>` belonged, closing the comment early and stranding
every paragraph after it outside the remarks. ***A malformation nobody's tooling surfaces is a
malformation nobody fixes***, which is why it wants a test rather than a convention.

⚠ **What the sweep cost, recorded so the next one is cheaper.** Deciding between *this block is
orphaned* and *this block was superseded* is a judgement per site, and it needs the member read. The
one thing that made it tractable was asking, per file, **which members have no doc comment at all** —
the orphan's owner is almost always in that list. **Three deletions dropped a clause worth keeping**
and were reviewed by hand; one was carried back verbatim (`BinTable.Create`'s *linking it in is
`World`'s*), and two were confirmed superseded — an `EventWheel` sentence naming a flat overflow list
`adr/0056` had since **refused**, and a `Readouts` paragraph whose `adr/0006` argument had already
moved to `IndexList.Length`. ***A block left stacked on its replacement is not evidence that its
author meant to keep it.***

### Cause 7 — two documents claim one ordinal

**Added 2026-08-24, on a sighting from the milestone 24 session** (worktree `city-sim-q8`, branch
`milestone-24-terrain-scoping`), which merged `main` into its branch and hit **four** ordinal
collisions at once: ADRs, a `PurposeTag`, an `Invariant` and a **plan number**. ⚠ **This one was found
by a person looking, and that is the entry.**

**Two of the four merged CLEANLY, and they are the two the corpus was supposed to cover.** The
`PurposeTag` collision conflicted loudly and the `Invariant` one did not — it was caught by the
**compiler**, `CA1069`, because an enum with two members at one value does not build. That leaves the
documents: **`plans/0041` existed twice, on two branches, under two different slugs**, and git merged
it without a murmur ***because the filenames differed***. Nothing in the corpus noticed either.

🔴 **`PlanIdentityTests` looks like the check for this and is not.** Its one assertion is that a
numbered document's **heading** agrees with its **own filename** — read the test rather than the name
([`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)).
It loops per file and never compares two files to each other. So two `plans/0041-*.md` with different
slugs each agree with themselves, pass all thirty-two corpus checks, and land. ***A filename is not a
declaration, so nothing declares it twice.*** ⚠ **And the gap covers `docs/adr/` identically** — the
same loop iterates `["plans", "docs/adr"]` in one array, so two `adr/0145-*.md` pass the same way.

🔴 **THE ADR HALF IS THE WORSE ONE, and the reason is not obvious: an ADR is cited BY NUMBER, in
prose.** A plan is usually reached by a link, so a duplicate leaves the link still opening *a* file.
An ADR is written into sentences as bare text — *`adr/0006`* — and **measured 2026-08-24, that is the
dominant form by 3.6 to 1: 6,592 bare-number citations across 162 files, against 1,839 that carry the
slug.** ⚠ **The peer session estimated *nine documents* and the real figure is 162**; it was checked
rather than taken, which is the only reason the row says the right thing. So duplicating an ADR number
makes **thousands of existing sentences ambiguous** while ***every link in the corpus still resolves
and every check stays green***. `LinkResolutionTests` opens the file it was handed; it has no opinion
about whether the number in a sentence names one document.

⚠ **The asymmetry is the whole finding, and it decides where the repair goes.** A `PurposeTag` or an
`Invariant` is a **compiled symbol**, so a collision is a build error and the machinery already exists
— on `main`, not in this corpus. An **ADR number or a plan number is a filename**, so a collision is
two files sitting quietly beside each other. ***The corpus's numbering scheme is the one identity
space in this project with no uniqueness check at all***, and it is the space `PROCESS.md` uses for
every citation.

⚠ **It is Cause 1 with the copies in different documents rather than the same one.** Cause 1's tell is
*the copies disagree*; here there is nothing to disagree, because each document is internally
consistent and the contradiction is only visible from outside both. **That is why it needs a new row
rather than a sighting under an old one.**

✅ **PAID THE SAME DAY** — `PlanIdentityTests.No_two_numbered_documents_claim_one_ordinal`, beside the
test that could not see it. It groups `plans/*.md` and `docs/adr/*.md` by leading ordinal and fails on
any group above one, naming every claimant. ⚠ **It is a corpus check and not a convention, because a
convention is what both sessions were already following** when this happened. **Watched fire on a real
collision before it was committed** — a second `plans/0041-*.md` under a different slug, which is the
sighting reproduced — as this repository requires of any diagnostic. ⚠ **Keyed on directory as well as
number**: `plans/0041` and `adr/0041` are different documents and always were. **`0000` and `0000a` are
different ordinals**, so the suffix is part of the key and the board and its archive both pass.

⚠ **It catches a collision at the MERGE and not at the moment it is created**, because a check running
on one branch cannot see the other branch's file — and the moment of creation is when it is cheap to
fix. ***The test is the backstop; the sessions telling each other is the fix.*** It is the same shape as
`PlanIdentityTests` and belongs beside it. ⚠ **A check that runs on one branch cannot see the other
branch's file**, so this catches a collision at the **merge** and not at the moment it is created —
which is the moment it is cheap to fix, and is why the sessions also told each other. ***The test is
the backstop; the message is the fix.***

**No ordinal is owed by this session.** Milestone 27's four commits touch no ADR, no `PurposeTag` and
no `Invariant`, checked rather than assumed; `plans/0041` is this session's and the other branch
renumbered to `0042`. **Reserved forward for milestone 27's remaining tasks: ADRs 0145–0149,
`PurposeTag` 25+, `Invariant` 56+** — below the other branch's 0150 and above its 24 and 55.

### Session K's collection — three, all paid in the sitting, and one is a new Cause

**All three were found by sequencing rather than by auditing**, which is the useful part: a
re-derivation walks every row of an inventory *for a purpose*, and a purpose is what makes a stale row
visible. None of the three would have been found by reading the documents that contain them.

**1. `Scope.Pool` appears in no inventory, and the reason is a new failure mode.** `RuleEngine.cs:803`
throws with a sentence saying *"the District Pool does not exist"*. It is settled (`02 §4.3` builds its
worked example on it), unbuilt, and has a live consumer — so it satisfies every entry condition of
[`06`](../docs/06-roadmap.md)'s *Mechanisms with no milestone*, and it was in none of its forty rows.
***A partially-shipped milestone reports as shipped, so a branch of it that throws is invisible to an
inventory of unscheduled work.*** Milestone **3a** is marked done in Phase 1's table, `pool` is one
scope inside the Rule engine 3a built, and nobody scanning for *what has no milestone* opens a
milestone marked done. **This is `adr/0093`'s sibling rather than an instance of it**: the description
was not *wrong*, it was **coarser than the thing it described** — and coarseness is invisible to every
check in this corpus, all of which compare a claim against a claim. **PAID**: it is milestone **12** *(9 before the 2026-08-18 economic reorder)*.
**The cheap standing check nobody runs**: every `throw` in `Borough.Core` whose message names a
mechanism is a candidate row for that table.

**2. Milestone 10 was two milestones wearing one number, and the clearance landed on the wrong half.**
[`06`](../docs/06-roadmap.md) said milestone 10 was **Save/load**; [`0000`](0000-board.md)'s *Blocked*
table said *"10 — the Outside"*; [`0002`](0002-open-questions.md) §D2 routed an Outside Connection's
throughput ceiling to *"`06` milestone 10"*. Session **J** closed both halves of `05 §7` — the save
format **and** the Outside layout — and the two collapsed onto one number, after which the board
recorded milestone 10 as **cleared**. **So the Outside was never scheduled at all**, while reading as
shipped, and it had a row in `06`'s own inventory the whole time saying *"a milestone"*. **Cause 1**,
with the twist that the two copies did not drift — *they named different things and agreed on a
number*. **PAID**: Save/load is **8**, the Outside is **14**, and `06` carries a retired-numbering
table so both old citations resolve.

**3. `adr/0041`'s Segment volume attribution had shipped and the row still said *a milestone*.**
`TripEngine.cs:572` increments it; 5c task 6 paid the debt on 2026-08-14. Found by opening the symbol
rather than by reading the cell, which is [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
working as intended. ***A gate is discharged by the work and struck by somebody*** applies to an
inventory row exactly as it applies to a gate board — **and the row was eight hours old when it went
stale**, which is the argument for `06`'s own note that the sweep is a snapshot and not an instrument.
**PAID**: struck, with the reason recorded in the cell.

⚠ **A fourth, filed rather than paid.** Two of the six roots session K sequenced are **half-built in a
way no document says**: `MapLayers.SetLandValueTarget` and `HouseholdTable.Money`/`.Savings` are
declared columns and operators whose **only callers are tests, fixtures and the golden builder**. Six
decisions read land value as extant and `06`'s row calls it *"a named hole"* — both true, and neither
says the transport already ships and only the **producer** is missing. That is a real difference in
cost and it is stated nowhere. `plans/0012` has no Cause for it because it is not a document being
wrong; it is a document being **less precise than a `find_referencing_symbols` call**, which is the
half of `adr/0093` that says *name a symbol, never a time*.

### Session T's collection — four, three paid in the sitting

**Found by re-summing a table rather than by auditing it**, which is the same lesson as session K's
collection one axis over: K found its three *by sequencing for a purpose*, T found these *by needing a
denominator*. A ledger read for information looks fine; a ledger read in order to divide by it does not.

**1. `plans/0013`'s sum was stale by ×4 on its own largest row.** **PAID** — see the new Cause 5 row
above, which is where the general form belongs.

**2. `adr/0019`'s *"64 Ticks/s is Factorio's rate"* is wrong on the digit and backwards on the
mapping.** Factorio runs at **60 UPS**. 64 is `2⁶` off that ADR's own 16 Ticks/s reference rate, and it
landed within 7% of a figure from another game, so it read as corroboration. **PAID**: struck in place,
with the dilation rider — which is correct, is genuinely Factorio's documented behaviour, and is now
load-bearing under [`adr/0105`](../docs/adr/0105-the-target-speed-is-4x-at-a-million-and-a-rung-dilates-rather-than-being-withdrawn.md)
— kept whole.

> ⚠ **This is Cause 5's first sighting against a source *outside* the corpus, and no check we could
> build would reach it.** Every mechanical check here compares a claim to another claim in this
> repository. A number attributed to an external product has no second copy to disagree with, and the
> only defence is the general rule: ***a number arriving with no clause saying what it measures is a
> coincidence of magnitude rather than evidence.*** **The mapping error is larger than the digit** —
> Factorio has no speed multiplier in normal play, so its only rate corresponds to our **1×**, and the
> rider took another game's *design rate*, mapped it onto our *ladder top*, and concluded the ladder top
> was not a design choice. ***Two rates sharing a unit are not thereby comparable.*** The conclusion
> survives on `01 §1`'s grounds, so this is a **citation defect and not a design defect** — worth
> distinguishing, because the repair is different: a citation defect is struck, a design defect is
> re-argued.

**3. `plans/0013`'s Map Layer row re-derived its knee for the new map and left its price behind.** The
2026-08-13 amendment moved residency saturation from 256 to 8,192 sources for a 512-Cell map and left
the whole-map recompute at **1.01 ms**, which was measured at 128 Cells. A recompute is `O(cells)`, so
the ceiling is **~16 ms** — over a whole Tick at the target rung, at any population. **FILED, not
paid**: the repair is a re-measurement, not an edit, and scaling by 16 is exactly the extrapolation that
document refuses everywhere else. `plans/0002` §E. ***One reader re-derived one consequence of a premise
and the other consequence sat two lines away*** — the same shape as item 1, in the same file, found the
same afternoon.

**4. `05`'s budget section says `CellGrid.WorldCells` is still 128 and it is 512.** *"⚠ **The constant
has not moved.** `CellGrid.WorldCells` is still 128, gated on road generation being scoped to developed
land"* — both halves are stale: `plans/0003` queue item 6 shipped 2026-08-13 and the flip landed the
same day. **PAID** below. ⚠ **`adr/0093`'s repair would have caught this and the sentence already
complies with it** — it names a **symbol**, not a time, so one `find_symbol` settles it. *Naming a
symbol makes a claim checkable; it does not make anybody check it*, which is the half of that ADR
nothing enforces.

### Milestone 8's collection — eight; the first is a new surface for Cause 4, items 3 and 4 are a matched pair, and the last four came out of building it — including one against check 6 itself

**1. ⚠ `BOR0901`'s diagnostic message describes a save serialiser that does not exist.** The
message a developer reads when they trip the lint says *"both the save serialiser and the State Hash
are generated from that one declaration"*, and its extended description reasons from a *"save/reload
test [that] passes because the field is saved"* (`src/Borough.Analysers/Diagnostics.cs:181-183`).
**The State Hash half is true; the save half has never existed** — `src/Borough.Formats/` holds eight
files and none touches world state.

⚠ **This is Cause 4 on a surface [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)'s
inventory does not name.** That ADR enumerates an ADR, a plan, a doc-comment, and what a test suite's
coverage implies. **A compiler diagnostic is a fifth, and it is the most persuasive of them**: it is
current by construction, it is emitted by the build itself rather than written beside it, and it is
read at the exact moment the reader is being taught the rule and is therefore least equipped to
audit it. ***A description of the build that arrives with the build's authority is the one nobody
checks.*** **Not paid, and deliberately**: milestone 8 makes the sentence **true** rather than
correcting it, which is available exactly once and is the better repair. Strike this entry when task
5 lands; if milestone 8 slips, correct the message instead.

**2. ⚠ "Milestone 8" names two different milestones inside `src/`, and the retired-numbering table
structurally cannot reach it.** Session K mapped old **8 → 7** (parking) and old **10 → 8**
(Save/load). Both old numbers are live in the build:

| Says | Means | Sites |
|---|---|---|
| *"milestone 10"* | **Save/load** — now 8 | `Formats/CrashArtifact.cs:36`, `:38`, `:82`, `:140`, `:195`; `Core/Entities/LotTable.cs:14` |
| *"milestone 8"* | **Parking** — now 7 | `Core/Movement/TripEngine.cs:198`, `:395`; `Core/Entities/World.cs:1082`, `:1135`; `tests/…/AccessPointTests.cs:53`; `tests/…/StatisticalTravelTimeTests.cs:243`; `tests/…/CarOwnershipTests.cs:151` |
| *"milestone 8"* | **Save/load** — correct | `tests/…/CondemnationTrailTests.cs:264` |

So somebody grepping `milestone 8` for this milestone's obligations finds **parking six times out of
seven**, and the single correct hit was written on 2026-08-17 by the session building milestone 6 —
***the collision is being created faster than it is being repaired, because new writing uses new
numbering while the old sites sit unmigrated beside it.*** [`06`](../docs/06-roadmap.md):390 already
warns that *a retired-numbering table makes an old citation resolve and cannot stop a new one being
translated as though it were old*; the new half is that **this instance is inside the build**, where
`06`'s table cannot reach it and **no document-to-document check can see it** — checks 1–8 are all
prose-to-prose. **Repair is by reading each citation's subject, never its digits**, so it is a sweep
by somebody who opens all thirteen sites, and it is not a line in a save commit.

**3. `adr/0087` says *both double-buffered tables have settled* and there is one.** Exactly one table
declares `Buffering.TwoCopies` — `LayerCellTable` (`src/Borough.Core/Space/LayerCellTable.cs:44`) —
and `Simulation.cs:709` calls it *"the first table in this project to declare"* it. The second the
corpus counts is **Lane dynamics, which does not exist**; `TripTable`, `LegTable` and `TravellerTable`
are all `OneCopy`. **The ADR's conclusion is untouched** — the phase-7 boundary is after the one
table has settled, and a save may ignore `_back` entirely — so this is a count and not an argument.
`CLAUDE.md` and `05 §3` carry the same *"two tables"* phrasing. ***A number stated as a fact about the
build ages against the build, and a conclusion that does not depend on it will not report the drift.***

**4. ⚠ `Reference`'s own remark says *only one column needs it* and seven do.**
`src/Borough.Core/Tables/Declaration.cs:127` states, of the `Reference.Severable` exemption, that
*"Slice 10 is what made this necessary, and only one column needs it"* — the column being
`CitizenTable.Workplace`. It is now **seven columns across five tables**:
`Movement/LegTable.cs:74`, `:79`; `Movement/TripTable.cs:67`, `:72`; `Movement/RouteHopTable.cs:69`;
`Rules/CondemnationTrailTable.cs:120`; `Entities/CitizenTable.cs:63`. Milestones 5b and 5c added six of
them, so the sentence was true when written and was out by 7× within two milestones. **The enum's
conclusion is untouched** — the axis is right and every one of the seven is a correct declaration — so
this is a count and not an argument, exactly as item 3 above is.

⚠ **It is the matched pair to item 3 and the pair is what makes it worth filing.** Item 3 is a count in
an **ADR** that is **too high** — *two double-buffered tables*, and one exists, because it counted a
table that was never built. This is a count in a **doc-comment** that is **too low**, because it counted
at the moment of writing and the build moved. ***Two ways for a stated count to rot, opposite in
direction, identical in that the conclusion resting on it does not depend on it and therefore cannot
report the drift.*** Cause 4, and `adr/0093`'s writing half does not repair either: *name a symbol,
never a time* fixes a description of **where to look** and neither of these is one — a count is a
description of **how many**, which no symbol names. Item 3 of the *milestone 6* collection made the same
point about a measurement written into prose. ***A count is a measurement with no units, and nothing in
this corpus re-runs it.***

⚠ **This one had a live consumer within the hour, which is why it is filed rather than shrugged at.**
Milestone 8's open decision 2 — *is `Derived` one class or two* — turns on whether a declaration axis
added for a single column stays a single column, and `Declaration.cs:127` is the one sentence in the
repository that speaks to it. **Read as current it argues against a third `Disposition`** (one column
needed it, one column still needs it, so the taxonomy grew a member nobody needed); **read against the
build it argues for one** (the last axis added on a single instance reached seven within two
milestones). The decision went the second way ([`0030`](0030-save-load.md) D3) *because somebody
grepped*. ***A stale count in a doc-comment about a declaration axis is read by exactly the person
deciding whether to add another one***, which is Cause 4's blast radius at its widest: not a wrong fact,
but a wrong fact positioned where it is load-bearing on a decision about its own subject.

**Repair is one sentence** — state the count as of a date, or state no count and name the disposition's
rule instead. Prefer the second: it is the form that cannot rot. Unpaid because it is the same sweep as
item 3, and both are one commit by whoever does either.

✅ **A THIRD instance landed 2026-08-24 and is REPAIRED, and it carries a wrinkle the pair above does
not.** `FactorioTests` held three column counts: two comments each said a table's saved columns numbered
*five*, where `business` has **four** and `unpremised` has **two**; and the union total lived in **two
places at two values**, *187* in the `UnreachableColumns` doc-comment against *249* in the remarks
eleven lines below it. ⚠ **The `business` five was NEVER right** — that table had three saved columns
when the comment was written — so this is not a count that rotted but a count that was **wrong on the
day**, which the pair above has no example of. And the `unpremised` five was **copied from the
paragraph above it** in the same commit that wrote it, which is **Cause 5** — a figure taken from a
neighbouring sentence without its subject — arriving inside a single file.

⚠ **The test was green throughout and could not have gone red.** It asserts an empty *residue* and
prints the totals; nothing in it compares a prose count to `SavedColumns`, and nothing could, because
the prose is a comment. ***This is the blind spot named at the head of this document arriving with an
example*** — `tests/Borough.Tests/Corpus/` is document-to-document, so a number that lives only in a
doc-comment is invisible to all thirty-two checks.

**Repaired the way this entry's own second option prescribes, and it is worth recording that the second
option was available here and not above.** The per-table counts are now stated with the column names
beside them and a note saying they were counted from `BusinessTable`'s constructor; the **union totals
were deleted rather than corrected**, because the test prints `250 of 250` on every run and a document
holding a copy of a number the run emits is the drift with extra steps. ***Where a machine already
states a figure, the repair is to delete the copy, not to update it.*** Found while shipping milestone
27 task 6, filed and repaired the same day under [`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md);
[`plans/0041`](0041-the-business-is-a-thing-the-city-contains.md) **G14** owns the full version.

**5. ⚠ `Column.FoldBytes`'s remark claims a byte-order property the fold does not have.** It reads
*"a State Hash whose value depends on the host's byte order is a hash that reports a divergence on a
port"*, and assembles `ulong`s through `BinaryPrimitives.ReadUInt64LittleEndian` on that ground
(`src/Borough.Core/Tables/Column.cs`). **That fixes the combination step and not the layout.** The
bytes it combines come from `MemoryMarshal.AsBytes` over the column's `T[]`, and a multi-byte field
inside an `unmanaged` struct sits in memory in the **machine's** order — so on a big-endian host the
byte sequence itself differs and **the State Hash differs for the same city**, which is precisely what
the remark says it prevents. ***A byte order fixed at the point of combination is not a byte order
fixed at the point of storage.***

⚠ **Cause 4 with the trigger read one level too shallow**, which is that pattern's own signature: the
sentence is right about *what the code does* (it does read little-endian) and wrong about *what that
achieves*, because the input to the step it describes is not the input the claim needs. **Not
repaired, and the reason is that the repair does not exist**: per-field byte swapping over an
arbitrary `unmanaged` struct is not expressible without knowing the struct's layout, which is the same
wall `adr/0086`'s *the field declaration is the format* leans on from the other side. Every platform
.NET supports is little-endian, so nothing is broken today. **The repair is the sentence**: say that
the representation is the host's and that the project is little-endian by platform rather than by
construction. Found 2026-08-17 by milestone 8 task 2, which had to decide whether the save could
inherit the discipline and found there was less of it than advertised.

**6. ⚠ S0a's 85.98 MiB is quoted in five places as the world's table footprint at 1M, and the declared
width is now 170.49 MiB.** Milestone 8 task 2 totalled the columns for the first time: Σ(declared
column bytes × allocated capacity) on a `new World(1_000_000)` is **178,770,767 B**, of which the
**saved** set — what a file holds — is **137,706,463 B, 131.33 MiB**. S0a's figure is a **resident**
measurement from a populated run and this is a **declared-width** computation, so ***the two must not
be subtracted***: they are different instruments and the gap is not a delta anybody measured.

⚠ **What it is consistent with is that the figure is simply old.** Five milestones have added columns
since S0a ran — the three Movement tables, the commute columns on `citizen`, the worker list on
`building`, the trail — and ***nothing re-measures a footprint when a column is added***, which is the
same shape as item 3 and item 4 one level up: a **measurement** stated as a fact about the build, ageing
against the build, with every conclusion resting on it unable to report the drift. **The consequence is
live rather than archival**: `adr/0087` prices the save's copy at ~10 ms citing `adr/0037`'s *8–15 ms
for 80–150 MB*, and 131 MiB is the **top** of that band — so its first revisit trigger, *the copy
becoming unaffordable at a single occurrence*, is closer than the ADR states. **Unpaid**: the repair is
a re-measurement rather than an edit, it belongs to S0a's owner, and milestone 8 task 9 is the run that
would produce it.

**7. ⚠ Check 6 cannot tell a quotation from a coincidence of magnitude, and it failed a document for
the second kind.** The registry row for `adr/0094`'s larder quantity registers **`22.5`** as an
alternate spelling of *90 in-world minutes*, requiring the phrase `pre-clock` wherever it appears.
Milestone 8 task 2 totalled the saved set per table, `bin` came out at **22,500,000 B**, and writing
that as *22.5 MB* failed
`DisqualifierTests.No_registered_figure_is_quoted_without_its_disqualifier` — for a quantity in
**megabytes** that has no relationship to a quantity in **in-world minutes** beyond sharing three
characters.

⚠ **This is the registry failing its own rule.** *Cause 5*'s writing half is ***name a number after
what it measures, not after where it sits*** — and a row whose pin is the bare digits `22.5` has named
the number after neither. **The shorter the figure, the more coincidences there are**: `186,624` and
`532,750` are effectively unique in any corpus, `10.37` and `82.84` are unlikely, and `22.5` and `3,700`
are magnitudes ordinary quantities land on routinely. So the registry's rows are **not equally strong**,
and nothing says which are weak.

⚠ **It is the instrument's second self-inflicted defect and the two are opposite in kind.** The first
is recorded in the test's own remarks: the parser split the alternates cell on commas, turned
`186,600` into `186` and `600`, and reported four clean documents — a **false positive from parsing**.
This is a **false positive from under-specification**, and it will recur, because the corpus keeps
producing new quantities and the registry keeps pinning short decimals. ***A check that fires on the
digits alone gets less specific every time the corpus grows.***

**Not repaired, and the shape of the repair is the open part.** Making the pin `22.5 in-world minutes`
would defeat the row, because every genuine site writes it bare — *"where it held 22.5"*,
*"90-vs-22.5"*, *"Restoring 22.5"*. The candidates are a **negative** pin (fire unless a unit follows),
a **strength column** saying which rows are safe to match bare, or accepting the false positives and
requiring the tripped document to say why — which is what happened here, and what this entry is.
⚠ **Do not repair it by deleting the row**: the row is doing real work, and the failure is that it is
doing more than its share.

⚠ **The document tripped the check a second time on the sentence explaining the first trip**, which is
worth recording because it shows where the escape hatch actually is. The test says *"if the figure is
genuinely being used another way, say so explicitly"* — and the only thing it can detect is **the
phrase**. So *saying so explicitly* and *satisfying the check* are the same act only if the explanation
**names the disqualifier**, which is a better outcome than it sounds: a reader grepping `pre-clock`
now lands on a sentence stating that `plans/0030`'s megabytes are not that quantity. ***The instrument
has one signal and it is the phrase, so the way to disclaim a coincidence is to name what it is not.***
That is the usable reading of the escape hatch and it was not obvious from the message.

**8. ⚠ `Session.cs` prints a per-session count against a per-world total in one sentence, and it reads
correctly only because no session has ever begun from a save.** `src/Borough.Headless/Session.cs:187-198`
reports *"{simulation.Reloads} reload(s), of which {recorded} cost the city something."* The first is
`Simulation.Reloads`, documented in its own remark as *"since this **Simulation** started"*; the second
comes from `RulesetTrailTable`, which is a **saved table** and therefore per-**world**. Load a save into
a fresh `Simulation` and the sentence becomes *"0 reload(s), of which 3 cost the city something"*.

**Found by milestone 8 task 4's walk of the `Simulation`'s private state** ([`0030`](0030-save-load.md)
D5), and filed rather than fixed because the repair belongs with task 8, which is where the runner
learns `--load` and can be tested against it. ***Two quantities agree for as long as the mechanism that
separates them does not exist*** — the divergence is not a defect this milestone introduces, it is one
the milestone makes reachable, and the fields themselves are correctly classified: both `Reloads` and
`LastReload` say in their own remarks that they are about the **run**.

⚠ **The general form is worth more than the site.** A shell that composes one number from `Core`'s
session-scoped state with another from the World's saved state has no way to notice they are denominated
differently, because both are `int`. **Every such pair is invisible until a load exists**, and this
milestone creates the first load. Nothing enumerates them; this is the one that was in the way.

---

### Milestone 7's collection — four; the first is the first defect in this ledger that a **printed number** carried, and the last three came out of task 8

**1. ⚠ A Tick's duration in seconds was stated as `10.546875` in four places in `src/`, and
[`adr/0094`](../docs/adr/0094-a-day-is-2048-ticks-because-ticks-per-day-is-a-sampling-rate-and-not-a-length-of-life.md)
made it `42.1875` — a factor of four, in one string the runner prints to its operator.** Found
2026-08-19 while scoping milestone 7 task 7, which needed the same conversion.

The four sites were `Borough.Headless/TripDump.cs` at the **printed** line and twice in the comments
either side of `Minutes`, `Borough.Core/Quantities/Speed.cs` twice, and
`Borough.Core/Quantities/TravelTime.cs` once. **All five statements derive it correctly and then quote
the old value** — *"a Day is 86,400 s over `Ticks.PerDay` Ticks, so a Tick is 10.546875 s"* — which is
**Cause 5 inverted**: there the digits travel away from the clause that qualifies them, and here the
**clause stayed put and went on producing digits nobody recomputed**. `Ticks.cs` names both values in
one sentence, so the corpus knew the whole time. ***A derivation written out beside its result is not
self-correcting; it is two claims, and only one of them is checked by anything.***

⚠ **The arithmetic was never wrong.** `TripDump.Minutes` divides by `Ticks.PerDay`, so every figure the
instrument printed was right and the sentence explaining it was wrong by 4×. That is the worst
available arrangement — a wrong statement no test can fail and no output disagrees with — and it is
why this was found by somebody about to reuse the method rather than by the suite.

⚠ **`Speed.cs`'s was a stated *consequence* and not just a value**: *"at 10.546875 s a car clears a
128 m Segment in 0.9 Ticks, which is far too coarse for Lane queues to form."* At 42.1875 s it is
**0.22** Ticks, so the correction makes that argument **stronger** and nothing downstream of it moves.
[`03 §3`](../docs/03-agent-architecture.md) already carried the corrected figure from the other end —
the crossing rate is **~4.6** Segments a Tick, not `adr/0041`'s *about one* — so a **document and the
code it describes disagreed about one number for a month**, which no check in this corpus can see:
they are all document-to-document.

**Repaired 2026-08-19** at all four sites, each carrying what it used to say. **The mechanical check
is the open half** and it is not obviously cheap: what would have caught this is a test that a derived
constant quoted in prose agrees with the constant, and the prose is free text in a doc comment. The
narrow form worth having is a **disqualifier registry entry for `10.546875`**, on *Cause 5*'s
precedent — a retired value is exactly the sort of figure that should never appear again without the
clause saying it is retired.

**2. ⚠ [`0031`](0031-parking.md)'s Definition of done named a fixture that could not satisfy it, and
could never have satisfied it.** The item reads *"the walk Leg's cost is non-zero for at least one
Citizen in the committed golden session, so the baseline covers the mechanism."* The committed golden
session runs `rulesets/minimal.toml`, which states no `[households]` table **by design** — so nobody in
it owns a car, no car Trip is ever generated, and **no walk in it can cost anything at any point in the
milestone's future.** Found 2026-08-19, at task 8, by the task that had to meet it.

⚠ **This is a new shape and it is worth naming precisely: it is not Cause 1 and not Cause 5.** The
sentence is not a stale copy of a true one and it carries no travelling digits. It is an obligation
written against a **fixture the author had not opened**, in a document whose own header warns that a
description of the build is where to look and never what you found
([`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)).
***An obligation naming a fixture that cannot satisfy it is not a demanding obligation, it is an unread
one*** — and the failure mode is that it reads as **rigorous** right up to the day somebody tries to
discharge it. ⚠ **It would have been discharged silently by an easier reading.** *At least one Citizen*
over a session with zero drivers is vacuously unsatisfiable, but a reader in a hurry could have written
a test asserting *no walk Leg has a negative cost*, watched it pass, and closed the item.

**Repaired 2026-08-19 by meeting it rather than by editing it**: a second committed session on
`congested.toml`, so nothing the first covers stopped being covered. The item now carries the amendment
above it. ⚠ **The mechanical check is not obviously available** — this is a plan sentence about a test
fixture, and every check in `tests/Borough.Tests/Corpus/` is document-to-document. The cheap partial
form is the one **check 12** already has the shape of: *a document that names a fixture by name must
name one that exists*, which would not have caught this, because the fixture existed and merely could
not do the thing.

**3. ⚠ `[parking] shed_keeps`'s ratifier fired and refuted it, and the finding above it is that the
*radius*'s ratifier named a condition the mechanism makes unreachable.**
[`0002`](0002-open-questions.md) §D1 asked for the walk-Leg distribution *as shed occupancy approaches
1*. Occupancy **saturates at 83.0% and then falls**, because a Trip that cannot park is refused and a
refused Trip is a car that needs no space — ***a shed cannot be filled by shrinking it, because the
refusal that scarcity causes removes the demand that would have filled it.*** Found 2026-08-19 by
milestone 7 task 8's sweep.

⚠ **This is a ratifier defeated by the thing it was written to observe, and that is a harder failure
than the one D1 had already corrected for.** That row was amended once before, in 2026-08-18, because
*a generated city cannot vary parking occupancy* — and **that** was fixed by building a world
([`rulesets/scarce.toml`](../rulesets/scarce.toml)). **No world fixes this one.** The clause is amended
to *as occupancy approaches its ceiling*, and the number stands, because the ceiling is a property of
the mechanism rather than a shortfall in the world.

⚠ **The general form is worth carrying forward past this row**:
[`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md) as
amended twice asks a ratifier to name **a machine, a world and a quantity**, and this one named all
three correctly and was still unreachable. ***A ratifier can name a state the mechanism it ratifies
prevents***, and nothing in `adr/0052`'s checklist asks whether the named state is reachable. Filed
here rather than as an amendment because one sighting is not a rule, and because the repair — *ask
whether the mechanism can produce the state you propose to read* — is a sentence for whoever writes the
next ratifier rather than a fourth clause.

**4. ⚠ `TreasuryFromAFileTests.Shipped` enumerated the shipped Rulesets by hand, so two of the eight
sat outside the only test that surveys every one of them.** It was a six-element string array —
`minimal`, `minimal-tuned`, `severance`, `congested`, `diagnosed`, `taxed` — and the tree held
**seven** before task 8 and eight after: `monetised.toml` had never been added, and `scarce.toml`
would not have been. **Nothing failed on either occasion**, because a hand-written list is complete
with respect to itself. ⚠ **And the doc-comment above it made the stronger claim in the wrong
tense**: it says this fails *in the direction that will actually fail, a sixth file added without
money* — describing a check that a sixth file had already walked past. Found and repaired 2026-08-19
at task 8 — it is a directory glob now, on **check 5**'s own precedent, so a ninth file becomes a case
on the next build with nothing to remember.

⚠ **It is `plans/0012`'s own subject arriving inside `tests/`.** ***A test that enumerates the
repository by hand stops covering the repository the first time somebody adds to it***, which is Cause
1 — *every document that stores per-slice status drifted, and the only large one that did not stores
none* — with `rulesets/` as the thing being stored and a `string[]` as the second copy. Routed on the
day under
[`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md),
since the defect is in a suite task 8 does not own. ⚠ **The mechanical check is a sweep worth running
once**: any other test holding a literal list of files that a directory listing would produce.

### Milestone 10's collection — six; the first is a new form of Cause 1 (two copies that disagree about a *direction*), the second and third are new *surfaces* — a path, and the space across working trees — the fourth is Cause 2 at its widest reach yet and is PAID, the fifth was split out of it, and the sixth is the first defect a document committed against **itself**

**1. ⚠ [`06`](../docs/06-roadmap.md)'s dependency graph makes the District Pool a root, and
[`adr/0050`](../docs/adr/0050-crossing-an-ownership-boundary-is-a-trade-and-payment-is-implicit-in-the-scope.md)
makes it downstream of two milestones.** Found 2026-08-18 while scoping milestone 10
([`0033`](0033-conserved-money-and-the-treasury.md) → **F1**), and it is why milestone **9** was not
the row picked.

`06`'s roots table lists **the District Pool | 9 | Needs road connectivity, which shipped in 5a**,
under a preamble stating that *"nothing in the inventory precedes any of them"*. Its edge table then
carries **District Pool → the price surface**.

`adr/0050` and the build's own throw site say the opposite. `RuleEngine.cs:803-811` refuses
`Scope.Pool` and states, citing that ADR **by name**, that a pool term *"crosses an ownership
boundary, so the Good moves one way and money the other at the prevailing price, settled atomically
with the Rule"*, and that *"implementing this as a Bin lookup ships an unconserved economy, and no
refusal can catch that."* A trade needs **money** to move and a **price** to move at. So the Pool is
downstream of milestone **10** and of milestone **13**, and the graph carries the second of those two
edges **pointing the other way**.

⚠ **This is a new shape for Cause 1.** Every earlier sighting is two copies of a *status* drifting
apart, or one copy silent where another speaks. Here both copies are current, both are load-bearing,
and they disagree about the **direction of an edge** — which is the one kind of disagreement a
document-to-document check could never find, because both documents are internally consistent.

⚠ **Its cause is worth more than the row.** `06`'s graph is derived from what each mechanism needs in
order to **exist**; `adr/0050` states what the Pool needs in order to **function**. ***A dependency
graph built from existence conditions will miss functional ones, and nothing about the graph's form
announces which kind it holds.*** The same document already recorded, on 2026-08-16, that two of its
edges *"[were] stated in a row's prose"* and absent from the graph — this is the harder version of
that, since here the graph is not silent, it is wrong.

✅ **PAID 2026-08-18, the same day, with the user in the room** — and paying it found **two more edges
of the same kind**, which is why it stopped being a filing. The District Pool is **struck as a root**;
the graph gains **Hinterland → price surface**, **Hinterland → District Pool** and **Hinterland →
wages, rents and Goods prices**; and `06`'s economic rows are re-ordered, four of them permuting while
10 and 13 keep their numbers.

⚠ **The general form is what outlives the row: every edge that was missing is an *anchor* edge.**
`06`'s graph is derived from what a mechanism needs in order to **exist** — its edges read *"needs X,
which shipped in 5a"* — and it is therefore blind to what a mechanism needs in order to be
**bounded**. That is one blind spot producing three sightings, not three defects. ⚠ **Its sharpest
instance is that `06` contradicted itself in two adjacent rows**: the Hinterland's own risk cell says
that milestone retires *"that **no price in the design has an anchor**"*, and the price surface was
sequenced before it.

⚠ **A second defect, in the machinery that exists to absorb renumbers.** The retired-numbering table is
two columns, *Was → Is now*, which **assumes exactly one renumber**; there have now been two, so
*"milestone 12"* resolves differently either side of 2026-08-18. ***A retired-numbering table is
generation-scoped and nothing in its two-column form says so.*** Each block now carries its window.
**PAID in the same edit.**

**2. ⚠ A worktree's directory name is a claim about which branch is in it, and nothing re-checks
it.** Found 2026-08-19, by a peer session reasoning from the path and asking a question this branch
could not have answered.

`.claude/worktrees/milestone-8-save-load` has **`milestone-10-conserved-money`** checked out in it. A
second session read the directory name, concluded this branch was building save/load, and asked
whether it carried a saved world pinning a Ruleset content hash under
[`adr/0100`](../docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md)
before re-recording the golden baselines. Milestone 8 has shipped and merged; the
`milestone-8-save-load` branch holds **zero commits against `main`**. The question was well-formed,
urgent, and about a milestone this branch has nothing to do with.

⚠ **This is [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
on a path rather than on a doc-comment**, and it fails in exactly the way that ADR predicts. A
worktree directory is a description of the build whose name is fixed once, at `git worktree add`, and
whose subject — the branch checked out in it — moves freely afterwards with `git checkout -b`. So the
name states what the directory was **created for** and never what is **in** it, which is that ADR's
*a description explains a mechanism's purpose, and a purpose is not a trigger* with the two halves a
filesystem apart. **Cause 4**, on a surface none of this corpus's eight mechanical checks can reach:
every one of them reads a **document**, and this is a path.

⚠ **The cost is the shape of the question that arrives, not the confusion.** Answering it meant
opening `src/Borough.Core/Persistence/SaveHeader.cs` — reading the build, which is the ADR's own
repair — and the answer was reassuring: `SaveHeader.RulesetInForce` does store the content hash of
the Ruleset in force, so the worry had the right shape, but `git ls-files` finds **no save artifact
anywhere in the repository**, every save in the suite is written and read inside one run, and the
persistence tests assert against locally-declared constants rather than real file hashes. `adr/0100`
holds. ***It was cheap here because the answer was reassuring.*** A session that had reasoned from
the same path to a **conclusion** rather than to a question would have had nothing stop it.

✅ **RECORDED 2026-08-19, and there is nothing to pay** — the defect is in a path, and renaming it
would only re-arm the same trap at the next `git checkout`. A worktree outlives the branch it was
made for, which is what makes it worth keeping. **The repair is in how it is cited: `git worktree
list` prints the pairing in one command, and where a document or a message names a worktree it should
name the *branch*.** `adr/0093`'s ***name a symbol, never a time***, with a branch as the symbol.

**3. ⚠ A cross-branch ADR numbering check that compares *content* is twenty-one false positives out
of twenty-three, because an ADR's identity is its filename and its content is the one part this corpus
guarantees will move.** Found 2026-08-19 by the milestone-7-parking session, relayed by a third
session sharing this working tree, and measured here before being written down.

**The occasion is a real collision and it is two, not three.** `0112` is `main`'s *the saved set is
the hashed set* against milestone-7-parking's *a parking space is held by the Citizen*; `0113` is
milestone-10's *a Business is an occupant* against milestone-7-parking's *a car park is not a Bin*.
Both are being renumbered to `0119`/`0120`, which is free — milestone-10's highest is `0118`.

**The instrument is worth more than the collision.** The detection method was `git ls-tree -r` over
every branch compared by **number**, and it reported milestone-10 as colliding with `main` on
`0110`–`0112` when `main` is a strict ancestor of milestone-10 and those three files are
byte-identical — inherited, not duplicated. Measured across `main`, `milestone-7-parking` and
`milestone-10-conserved-money`, **112 ADR numbers appear on two or more branches**:

| Compared by | Reports | Of which false | Why it fails |
|---|---|---|---|
| number | 112 | **110** | every branch collides with everything it descends from |
| content | 23 | **21** | an amended ADR is not a duplicated number |
| filename | 2 | **0** | — |

⚠ **The proposed repair is the half worth correcting, because it is still wrong and it is the version
somebody would build.** *Compare content and report only numbers whose files differ* fails on two of
this corpus's own conventions meeting. An ADR's **filename is the claim** — `CLAUDE.md` states it
outright, *"The filename is the claim, stated as a sentence"* — so the filename is the identity. And
**a superseded document gets a banner, never a deletion**, so an ADR's content is expected to move for
the whole of its life: milestone-10 amends six ADRs that exist on `main` and milestone-7-parking
amends fifteen, and not one of them is a collision. ***An instrument that compares content is
measuring the one thing the corpus guarantees will differ.***

⚠ **The content row moved while this entry was being written, and that is the finding rather than an
erratum.** It was measured at **22/20** and re-measured at **23/21** within the hour, because
`milestone-7-parking` committed an amendment to `adr/0083` — a *third* branch moving a file neither of
the two branches in the comparison had touched. The filename row did not move, and could not have.
***The quantity content-comparison keys on drifts continuously and the quantity filename-comparison
keys on changes only when somebody claims a number***, which is the same fact the table states,
arriving as a measurement instead of an argument. It is also [`plans/0026`](0026-statistical-resolution-and-the-travel-time-matrix.md)'s
***a measurement written into prose does not re-run itself when the mechanism underneath it moves***,
caught this time only because the re-run happened to be cheap and somebody happened to do it.

⚠ **The two rows have different *dynamics*, and the mechanism is measured rather than argued.** The
content row's false positives are **exactly** the ADRs amended on some branch since its merge-base
with the branch it is compared against — 21 against 21, with both differences empty, so this is an
identity and not a correlation. A merge moves the merge-base past the amendment and takes the number
out of the set, so ***the content row's error term rises with every unmerged amendment anybody makes
and falls only on merge***, while the filename row's count moves only when somebody claims a number,
which is the event being detected. Signal, against signal plus a term proportional to how much work is
in flight.

**What follows from that is a judgement rather than a measurement, and it is the half worth keeping**
(raised by the session sharing this working tree, and marked as an argument per
[`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)): a
cross-branch collision check exists *for* a world with many branches open at once, and content-compare
degrades in exact proportion to how true that world is — **least reliable precisely when it is most
needed**. So the twenty-one is not a tolerable constant. ***A reader who decides today that twenty-one
false positives are worth living with is setting that tolerance against a quantity that grows.***

**So the check compares filenames per number**, treating a differing filename as the collision and
differing content as an amendment. It needs **no ancestry test at all**, which is the property that
matters: a merge-base filter also gets to two here, but it can only compare branches that have met,
and the case this exists to catch is two branches that have not.

⚠ **This is the third surface in this section that none of the corpus's eight mechanical checks can
reach, and it is the widest.** Item 2 is a **path**; this is **across working trees**, and every check
in this document is document-to-document **within one tree**. ***What the two share is the shape of
the repair: neither wants a stricter version of an existing check, both want a check with a different
domain*** — and a domain no existing check has is a domain nobody notices is missing.

✅ **RECORDED 2026-08-19 as a constraint on an instrument rather than as a check to build.** Nothing
here is owed an edit: the collision is being paid by the branch that made it, and the check does not
exist. The naive version is the obvious one to write and the corrected-by-content version is the
obvious repair, which is exactly why both are written down before anybody writes either.

---

**4. ~~⚠ `adr/0094` moved the Day from 8192 Ticks to 2048 and four numbered design documents still
state 8192 as settled~~** — ✅ **PAID 2026-08-19, in the sitting after the one that found it**, and
the filing was wrong twice before it was paid. Found while building milestone 10 task 8, by reading
`02 §9` for the clause that task discharges and meeting `02 §11`'s *"`TICKS_PER_DAY = 8192`"* three
sections later. **Cause 2** — an ADR issues writes to other documents and the writes do not all land —
and it is the widest reach recorded: four documents, not one.

⚠ **THE FILING SAID `adr/0019` NEEDED A SUPERSESSION BANNER AND IT HAS HAD ONE SINCE 2026-08-13.**
That banner is thorough: it withdraws the ADR's **title claim**, strikes four things by name, says what
survives, and records that the State Hashes moved. So the ADR discharged its obligation **completely**
and four documents citing it still carried the old figure. ***An ADR that supersedes itself correctly
does not thereby correct the documents that quote it***, which is Cause 2 stated at its purest — and
the first sighting where the ADR half is provably blameless.

⚠ **THE FILING SAID SEVEN SITES AND SAID SIX EDITS AND ONE JUDGEMENT.** It was **eleven**, across three
*independent* stale constants that interact, and the interaction is the whole difficulty:

| Constant | Stated | Actual | Moved by |
|---|---|---|---|
| `TICKS_PER_DAY`, `WHEEL_SIZE` | 8192 | **2048** | [`adr/0094`](../docs/adr/0094-a-day-is-2048-ticks-because-ticks-per-day-is-a-sampling-rate-and-not-a-length-of-life.md), 2026-08-13 |
| a Tick's in-world duration | 10.546875 s | **42.1875 s** | derived from the above |
| the map's width | 16.4 km / 4096² | **65.5 km / 16384²** | [`adr/0089`](../docs/adr/0089-the-map-is-sized-by-how-many-commutes-fit-across-it.md), 2026-08-12 |

⚠ **`02 §1.2`'s HEADLINE RATIO WAS WRONG IN BOTH OPERANDS, AND THE TWO CORRECTIONS THAT EXISTED WERE
FIFTEEN LINES APART IN THE SAME SECTION.** The block read `480 ÷ 8192 = 5.9%`. The numerator was
struck on 2026-08-12 by the *Cross-town trip* row **below it in the same table**, which names the
figure and gives `~112 Ticks`; the denominator was struck the next day by `adr/0094`. ***A correction
that lands in a table does not land in the prose above the table***, and neither correction crossed
fifteen lines.

⚠ **THE SHARPEST FINDING IS A FIGURE THAT WAS RIGHT BY CANCELLATION WHILE BOTH ITS INPUTS WERE
WRONG.** `~112 Ticks` for a map crossing is correct today and was correct on 2026-08-12 — because
`adr/0089` made the map ×4 wider and `adr/0094` made a Tick ×4 longer, and a crossing is denominated
in in-world time. The *Cross-town trip* row states **both** inputs and **both were stale**. The same
holds for the derived table's cross-town-seconds column, which needed no edit at all. ***The one
column nobody had to touch is the one whose inputs were both wrong***, and `plans/0012` already had
the phrase for it — *right by cancellation* — from a different sighting. ⚠ **The share does not
cancel**: a crossing went 1.4% → **5.5%** of a Day, because the map move is a change to the world and
the clock move is not.

⚠ **AND `02 §1.2` SAID *"invariant under both exchange rates"* WHILE READING AS *invariant*.** The two
exchange rates are Ticks→seconds and Tiles→metres. The **map's width** is neither; it is a fact about
the world, and it is what moved the number. The sentence was true and the paragraph around it was not.

⚠ **The stalest sentence claims nothing false.** `02 §1.2`'s *"8192 is not divisible by 24, so an hour
would not land on a Tick boundary"* — 2048 is not divisible by 24 either, so the **conclusion survived
its premise moving**. ***A conclusion that survives its premise moving is the hardest kind of stale
sentence to find, because nothing it claims is false***, and no mechanical check reaches it: check 8
opens links, check 6 refuses a registry figure quoted bare, and this is neither.

**What was paid.** `02 §1.2` — the `adr/0082` amendment's Tick duration, the headline ratio block and
its two surrounding claims, both constants-table rows, the *Cross-town trip* row's map width and
share, the derived orientation table in full, and the divisibility bullet. `02 §11` question 1.
`00`'s open question 2, whose two wall-clock figures are **derived** and were re-derived against
[`01 §1`](../docs/01-player-experience.md) rather than divided. `05`'s open question 2 and its
`WHEEL_SIZE` sentence. Every figure taken from the code — `Ticks.PerDay`, `EventWheel.Size`,
`CellGrid.WorldCells`, `Tiles.Metres` — rather than from another document.

✅ **The derived table now defers to [`01 §1`](../docs/01-player-experience.md) instead of restating
it.** It carried a **four**-rung ladder with names — *Study / Normal / Fast / Very fast* — against
`01 §1`'s five rungs plus pause, none of which are named, and it had drifted in three columns at once.
***A table that restates another document's ladder is a second copy***, which is **Cause 1** on a
surface that ledger had only ever recorded for *status*. `01 §1` itself was **already correct** and had
been all along, which is what makes the copy the defect.

⚠ **NOT PAID, AND DELIBERATELY OUT OF SCOPE — `docs/05` reasons from a 4096² map in five places while
its own line 31 says `adr/0089` replaced it.** Same shape, different constant, and it needs its own
sitting: `05 §33`'s *"3.7–5.2 Commute Budgets across"* is `adr/0089`'s derived figure and **could not
be reproduced from the shipped constants** during this sitting, so it was left rather than guessed at.
***A figure you cannot re-derive is one to file, not one to correct.*** Filed as item **5** below.

**5. ⚠ `docs/05` reasons from a 4096² map in five places, and its own line 31 records that
[`adr/0089`](../docs/adr/0089-the-map-is-sized-by-how-many-commutes-fit-across-it.md) took the map to
16384² on 2026-08-12.** Split out of item 4 on 2026-08-19 rather than paid with it. The sites are
`05:20` (*"a fully-developed 4096² map"*), `:33`, `:37`, `:41` (*"at 4096² the Road Graph is 16× the
1024² case"*) and `:255` (*"three of the four dissolve under arithmetic at 4096²"*). ⚠ **Three of the
five are arguments whose conclusions may not survive**, unlike item 4's divisibility bullet: `:41`'s
route-computation risk is stated as a multiple of a map that is now 16× larger again, and `:255`'s
Chunk-size tension is claimed to dissolve *under arithmetic at 4096²*. ***A conclusion reached by
arithmetic on a constant has to be re-reached when the constant moves, not re-stated.*** Also owed:
`05:33`'s **3.7–5.2 Commute Budgets across** could not be reproduced from the shipped constants — a
50-minute ceiling at 50 km/h is 41.7 km against a 65.5 km map, which is **1.6**, and the fast rung's
20 minutes gives **3.9** — so the figure needs its author's derivation before anybody quotes or
corrects it. **Measurable** under `adr/0043` only once the intended denominator is known; **arguable**
until then.

---

**6. ⚠ `plans/0033` OPENED `# 0031` — WHICH IS `plans/0031-parking.md` — FROM THE DAY IT WAS CREATED.**
✅ **PAID 2026-08-19**, found while closing milestone 10 by reading the file's own first line. Conserved
Money was drafted as `0031`; parking took that number *"first by sixteen hours, so conserved money
renumbered to **`0033`**"* ([`0002`](0002-open-questions.md) records it); and the renumber moved the
**filename** and every citation while leaving the **heading** behind. So a reader who opened the file
was told it was a different, existing plan about a different milestone — and every reader who arrived
by citation was told correctly, which is why it survived a full day of work inside the document.

⚠ **This is a new form of Cause 1 and it is the smallest possible one: the two copies are in the same
file, four characters apart in meaning and one line apart on screen.** ***A document's own title is a
second copy of its number.*** Nothing in this ledger had counted a filename as a *copy* of anything.

⚠ **No existing check could have reached it, and the reason generalises.** Every mechanical check in
`tests/Borough.Tests/Corpus/` is **document-to-document** — citations resolve, links open, tables
render, no registry figure appears bare. A **self**-reference is none of those: the number in a heading
is a claim about the file it is already in, so no second document has to agree with it and no link has
to open. ***The checks are built to catch two documents disagreeing, and this was one document
disagreeing with itself.***

✅ **`PlanIdentityTests` is the discharge**, and it is `adr/0073` in its literal form — the finding
went to the code that can prevent it on the day, not into a note. It fires only where a heading
**states** a number, because a heading with no number claims nothing: `plans/0001` and `plans/0025`
both open with a bare title and neither is a contradiction. ***A check that also enforced a house
style would be choosing one, and this ledger is for contradictions.*** Watched fail on the real
violation before the heading was corrected.

---

## Fixed in the sitting that found them

**⚠ Cause 4 — `RulesetShape.cs:217` names milestone 27 task 7 for three things and one of them is
milestone 15's.** The comment reads *"`adr/0141` gives the trade `jobs`, shift hours and the wage, and
all three arrive with milestone 27 task 7."* **Two of three do.** [`06:99`](../docs/06-roadmap.md)
places wages at **milestone 15** — *"attended services, wages and Skill Tiers"* — citing
[`adr/0026`](../docs/adr/0026-wages-are-posted-locally-and-never-cleared.md) by name, and
`Readouts.cs:69` says the same thing independently: *"income is a **flow** that arrives with wages in
milestone 15."* Found 2026-08-24 while settling
[`plans/0041`](0041-the-business-is-a-thing-the-city-contains.md) decision 2.

⚠ **This is Cause 4's exact shape — wrong about the TRIGGER and right about everything else.** The
comment is correct that `adr/0141` gives the trade all three; it is wrong about **when**, which is the
half [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
says a description of the build is always wrong about. ***Left in place rather than corrected here***,
because the line is a doc comment inside a symbol milestone 27 task 7 is about to rewrite, and the
repair belongs in that change. **Recorded so the task inherits it rather than rediscovers it.**

🔴 **A doc comment naming a milestone is a date in disguise**, which is `adr/0093`'s *name a symbol,
never a time* being broken by a document that otherwise obeys it — and the reason it survived is that
**no corpus check reads doc comments at all**.


**⚠ Cause 5 with no number in it — `adr/0145` argued from a table column that does not exist.** The
ADR's `UNIQUE INDIVIDUALS` paragraph reads *"a Business founded by a named Household has a founder the
player can inspect — the money came from somewhere the player can point at."* `BusinessTable` declares
`building`, `kind`, `bin_head`, `bin_tail`, `balance`, `building_next` and `pool_slot`. **There is no
founder.** `World.Found` moves the band and severs the link in the same statement. Written and
contradicted by its own task's code **on the same day, 2026-08-24**; found the next sitting by reading
the table. Banner and inline mark added to `0145`; repaired by
[`adr/0146`](../docs/adr/0146-founding-costs-a-citizen-and-the-households-money-so-the-founder-is-the-first-worker.md),
which makes the claim true through the employment link rather than the column.

⚠ **It generalises Cause 5 past quantities, which is why it is filed here rather than as a Cause 8.**
This ledger's Cause 5 is *a number quoted away from the sentence that qualifies it*, and every prior
sighting carried digits. ***This one carries none — the thing detached from its support is a
CAPABILITY*** — but the mechanism is identical: an argument reached for a fact of the right *shape*,
found a sentence that supplied it, and never checked that the sentence rested on anything. **The
writing-side repair is also identical**: name the claim after what the build does, not after what the
argument needs. ***A pillar is as quotable as a percentage and detaches the same way.***

🔴 **Nothing mechanical could have caught it, and the reason is structural.** Every check in
`tests/Borough.Tests/Corpus/` is document-to-document; `RefusalCountTests` is the sole document-to-code
exception and it counts `Refuse(` sites in one file. ***A prose claim about a table's contents is
invisible to all thirty-two***, so the only instrument that sees it is a reader who opens the table.
**This is the same blind spot** [`plans/0041`](0041-the-business-is-a-thing-the-city-contains.md) **G29**
found from the other side — `CitizenTable.Employment` is `Saved`, therefore hashed, and nothing in
`src/` writes or reads it. **A saved column with no writer and a prose claim with no column are the
same hole seen from two directions**, and neither has an owner.


**⚠ Cause 2, in the table whose whole job is to be the enumeration.** Milestone 12 task 4 registered
`Invariant.ADistrictCellNamesALiveDistrictAndBuiltGround` in the end-of-run tier and **did not add it to
[`02 §10`](../docs/02-simulation-model.md)'s end-of-run row**, which lists the whole-world walks one by
one and had been kept current through nine milestones. Found and fixed **2026-08-22** by task 5, which
went to add its own and noticed the gap. Both are in it now.

🔴 **The sharper half is that nothing could have noticed.** The corpus checks are document-to-document,
so a member of a C# enum missing from a Markdown table is invisible to every one of them; and the row is
prose rather than a count, so no `RefusalCountTests`-shaped check applies either. ***What caught it was
the next person doing the same job one task later***, which is the mechanism this ledger exists because
it cannot be relied on.

⚠ **It is a genuine Cause 2 and not a near miss**: the write was owed — a whole-world invariant is
exactly what that cell enumerates — and a whole sitting closed with it not made.

**⚠ A doc-comment cited `WaitListWakeTests`, a class that does not exist, and every mechanical check in
the corpus is blind to it.** Found and fixed **2026-08-22**, settling
[`0003`](0003-build-plan.md) hash-moving queue item 14.
`WorldInvariants.HeadThatShouldHaveWoken`'s remarks claimed the predicate was *"shared with
`WaitListWakeTests` rather than restated there, which walked its own copy of this predicate and would have
drifted from it"* — a paragraph whose whole subject is drift, naming a test file that was never written.

🔴 **The reason it is worth a row: `tests/Borough.Tests/Corpus/` is document-to-document by construction.**
Citations resolve, links open, tables render, registry figures carry their clause — and **not one of them
reads a `<see cref=>` or a `<c>` in a doc-comment against the symbols that exist**. The compiler would
have caught `<see cref="WaitListWakeTests"/>`; this was `<c>WaitListWakeTests</c>`, which is prose in
angle brackets. ***A citation the compiler cannot see and the corpus checks do not read is a citation
nothing checks at all***, and this file's own header says the mechanical checks are all
document-to-document — so the surface was known and the sighting is the first one in it.

⚠ **It is Cause 4's surface rather than Cause 5's**: nothing was quoted away from its caveat; a
*description of the build* named a symbol that is not there, which is
[`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)'s
writing half — **name a symbol, never a time** — failing on the *symbol* half. **Not filed as a new
check**, because a `<c>`-tag audit would have to distinguish a symbol from ordinary code voice and would
be mostly false positives; what it argues for is preferring `<see cref=>`, which the compiler already
checks.

**⚠ Two documents say *two separated settlements* where they mean two Lattices, and under `CONTEXT.md`'s
own definition the world they describe is ONE Settlement.** Found and fixed **2026-08-22**, building
[`0037`](0037-goods-between-buildings-the-district-pool.md) task 1.
[`adr/0134`](../docs/adr/0134-a-district-is-a-centre-and-its-basin-so-the-count-follows-centres-and-not-a-ceiling.md)'s
Consequences and `0037` task 1 both ask for *"a Ruleset authoring two separated settlements"*.
`CONTEXT.md` → Settlement is a **derived** commute shed: *"connectivity is transitive, so a
contiguously-developed lattice is one Settlement however large the graph — what fragments it is unbuilt
ground with no road across it … or a gap wider than the Commute Budget."* The world task 1 actually
needs is **joined by road**, because `adr/0134` rejected splitting on road components — so under the
term's own definition it is one Settlement wherever anybody drives across the gap.

🔴 **The sharpest half: whether it is one Settlement or two is decided by a key in a DIFFERENT table.**
Over `rulesets/twinned.toml`'s corridor, 7,680 m is ~9 clock minutes by car and ~92 on foot against a
50-minute ceiling, and that file states no `[households]`, so nobody drives. ***A term that names a
derived thing cannot be borrowed for an authored one, because the derivation may depend on something the
author did not write.*** The key shipped as **`[[lattice]]`** for that reason, and `CONTEXT.md` gains a
**Lattice** entry saying what the three neighbouring terms — Settlement, District, centre — each are and
that all three are derived.

⚠ **This is a near miss rather than a caught defect, and the difference is worth stating.** The wrong
name was in a tool call before `CONTEXT.md` was opened; what caught it was the rule that a concept
needing a name gets a `CONTEXT.md` entry **first**. ***The vocabulary check works by being upstream of
the code, and it only works if it runs before the key is written and not after.*** **The two documents'
prose is loose rather than wrong and is left as it stands** — they were describing an outcome, not
choosing a term — with `CONTEXT.md` → Lattice carrying the correction and pointing at both.

**⚠ `06` placed a mechanism at milestone 12 that milestone 12's own scoping document never scoped, and
neither document contradicted itself.** Found and fixed **2026-08-22**, settling
[`0037`](0037-goods-between-buildings-the-district-pool.md) decision 6 —
[`adr/0138`](../docs/adr/0138-freight-is-unbuilt-so-the-min-follows-it-and-neither-is-at-twelve.md).
`06`'s row read *"Shipments — freight Vehicles between Districts and to the gate — **Placed: 12**"*.
`plans/0037` surveyed the milestone, found three preconditions no document had listed as blockers,
enumerated **nine decisions**, and freight appears in **none** of them.

🔴 **This is a new shape and it is the reason it survived a survey built to catch exactly this.** ***A
survey looks for what its author suspects is missing, and a mechanism placed by a DIFFERENT document is
not a suspicion.*** The scoping author was reading the build and the decisions; the placement lived in a
table three hundred lines away in another file, in a column about what *that* document owes. **Neither
document is internally wrong** — which is why every document-to-document check passes, since nothing
here is a citation that fails to resolve.

⚠ **A candidate check, named and not built**: for each milestone with a scoping plan, the set of `06`
rows saying **Placed: N** against the set of mechanisms that plan's decisions and preconditions mention.
A divergence is not automatically a defect — a mechanism can be genuinely trivial to scope — **so the
check needs a way to say *deliberately***, which is the same hard half `adr/0137`'s proposed
write-without-reader check has. **Two proposals now blocked on the same missing affordance**, which is
worth noticing before either is attempted.

**⚠ Third mechanism found parked at milestone 12 on an assumption its author did not check, in one day.**
Upkeep ([`adr/0136`](../docs/adr/0136-upkeep-has-three-blockers-landing-at-three-times-so-it-has-a-queue-and-not-a-milestone.md)),
freight and `adr/0088`'s `min()` (`adr/0138`). ***Each placement was correct about the blocker its author
happened to be holding and silent about the others***, and each was made by somebody working on a
different problem. 🔴 **The generalisation is about WHEN a placement is made, not about who makes it**: a
mechanism is placed at the moment somebody notices it is blocked, which is the moment they know **least**
about what else blocks it. **The scoping session is the first occasion anybody asks what a milestone
actually ships**, so it should be read as the first real audit of every row pointing at that milestone —
not only of the row that names it.

**⚠ A decision's stated dependency pointed at the wrong upstream, and would have read as answered when
that upstream closed.** `plans/0037` decision 6 said *"whether freight itself is in this milestone is
decision 8, so this one is downstream of it."* Decision 8 asked whether a **Provider** ships — an
intra-District seller — and `adr/0013` defines pooled intra-District movement **in opposition to** a
Shipment, so decision 8 answers nothing about freight. It was settled **yes** on 2026-08-22. ***A
decision routed to the wrong upstream reads as answered the moment the wrong upstream closes***, and a
later reader checking only whether 8 was done would have marked 6 settled without ever asking whether a
Vehicle exists. **Corrected in place**, with the misrouting kept rather than deleted, because the entry
is now the worked example.

**⚠ `plans/0037`'s Status block described the sitting's first hour while six more decisions closed
underneath it.** It read *"one sitting has run and it settled less of decision 1 than it changed about
it"* and *"decisions 2 and 5 to 9 are untouched"*; by the end of the day **seven of nine** were settled.
Its decisions heading had been wrong twice — *NONE SETTLED*, then *ALL NINE STILL OPEN*. This is
**Cause 1** arriving **inside one document over one day**, which is faster than the cause is usually
stated, and the mechanism is the ordinary one: ***a count sits at the top of a list that changes
underneath it.*** **Both rewritten**, and the heading now says to read each entry.


**⚠ `adr/0050` stated as settled a property of the build that the build does not have, and no mechanical check in this repository could reach it.** Found and corrected **2026-08-22**, settling [`0037`](0037-goods-between-buildings-the-district-pool.md) decision 9. The ADR says bankruptcy and starvation are distinguishable because *"the distinction falls out of the wait list rather than needing a mechanism"* — a Pool Bin short is starvation, a money Bin short is bankruptcy. **It is true of the wait list and false of the build.** `RuleInstanceTable.WaitingOn` records the Bin that stopped a Rule and `RuleEngine.Stop` writes it, but **`RuleEvidence` does not carry it** — `RuleId`, `LastRan`, `Succeeded`, `Blocking Blocked`, `ConditionId Reported`, `StarvedSince`, `Rate`, `MissedFirings` — and `Blocking` is only `Nothing / Supply / Space`, so **both cases surface as `Supply`**. ***The wait list is not a reader; `Evidence` is.***

🔴 **This is a new surface, and the reason is that the claim is about CODE.** Every mechanical check here is **document-to-document** — citations resolve, links open, tables render, no registry figure appears bare — so a sentence that agrees with every document it cites and disagrees with a **struct** passes all of them. ***A corpus that checks itself against itself cannot notice the build.*** `plans/0037` caught it only because [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md) made it a habit to read the symbol, and it flagged the entry in advance as **the one most likely to be assumed** — which is the mitigation working, by hand.

⚠ **And it is the second sighting of one shape.** Milestone 11 task 8 found `PlacementCounter.Departed` reaching no instrument — *"a flow that reaches no instrument is a flow nobody can read."* Here a column is written and no `Evidence` surface reads it. ***Writing the column feels like finishing the work***, which is why the consumer gets assumed. **A check is available and not built**: `DerivedRebuildAuditTests` exists because a column can be declared and never rebuilt, and its sibling would name columns no `Evidence` reader touches. **The hard half is saying *deliberately*** — a column nobody reads *yet* is not a defect. Corrected by [`adr/0137`](../docs/adr/0137-the-wait-list-knows-which-bin-and-evidence-does-not-so-bankruptcy-needs-one-field.md); `adr/0050` carries a banner.

**⚠ `02` disagreed with itself about whether Districts are player-drawn, and §11 held the copy that says
*leaning player-drawn*.** Found and fixed **2026-08-22**, in the sitting on `plans/0037` decision 1 —
found because the *reader* remembered the question as settled *player-drawn* and the corpus was checked
rather than argued with. `02 §2.1` carries the close in a blockquote: *"**Settled: both.** Automatic by
default, player-adjustable as an advanced action"*, with the derivation named in its own body text —
*"derived automatically from **road topology and land use**"*. `02 §11` item 3 listed the same question
as an open fork, unstruck, ending *"Leaning player-drawn with an automatic default."* ***A settled
question struck in one section and listed as open in another is a document disagreeing with itself, and
the reader believes whichever section they opened*** — and a list headed *Open questions* is the section
somebody opens **on purpose** when they want to know what is unsettled, which is the worst place for a
stale entry to survive. 🔴 ⚠ **It is also the wrong half left standing**: *leaning player-drawn* names the
arm that is **not** the default, so the stale copy does not merely fail to close the question, it points
at the losing answer. **This is Cause 1 reached from a new direction, and the direction is the finding.**
`plans/0002` had already filed this exact staleness **twice** — against `06:42` and against `plans/0010`'s
gate section — and both were corrected. Nobody checked the settling document's *own* open-questions list.
***A sweep that corrects every document that repeated a claim can leave the document that made it***, and
the settling document is the one the corrections all cite. Struck in place in `02 §11`; the entry now
carries the close, the derivation and the scope of the player arm.

**⚠ And a third copy, in `plans/0002` itself: a bullet whose three stated resolutions had all been
overtaken, unstruck.** Found the same day, by grepping the corpus for the phrase rather than trusting
that two fixes were all of them. The bullet — *"If Districts are player-drawn, `03 §3.3` makes a
cosmetic act change the State Hash"* — offers resolutions (a), (b) and (c) and recommends pricing (c)
first. 🔴 **(c) was chosen, recorded and shipped**:
[`adr/0041`](../docs/adr/0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md) is (c)
stated as its own title, and `06` records it live at `TripEngine.cs:572`. **The premise was separately
removed** by [`adr/0047`](../docs/adr/0047-routing-never-keys-on-the-district.md) — no District-pair
key survives to be cosmetic about. **And the opening conditional is closed** by `02 §2.1`. Its
*"blocking milestone 5c"* is stale twice over: **5c is DONE** (2026-08-16), and this fork was never
what blocked it. ***A bullet whose stated resolutions have all shipped reads as live work until
somebody strikes it***, and nothing in this corpus walks backwards from a shipped ADR to the question
that proposed it. ⚠ **The aggravating detail is that item 818 of the same document struck the one-line
version on 2026-08-13 and left this one standing.** ***The short copy of a finding is the copy that
gets maintained, because it is the copy people read*** — so the long version, which is the one carrying
the reasoning somebody would act on, is the one that rots. Struck in place with the argument retained.

**⚠ And `02 §2.1` still called a District *"the granularity of the travel-time matrix"* — the role
[`adr/0047`](../docs/adr/0047-routing-never-keys-on-the-district.md) exists to remove.** Found the same
day and in the same passage. `CONTEXT.md` → District has carried the corrected wording — *"**It is not**
the granularity of the travel-time matrix, and it is not where routing happens (`adr/0047`)"*, with the
consequence spelled out: *"redrawing a boundary changes what pools, never what a Traveller drives."*
`02 §2.1` was never given the same edit, and it is the longer passage of the two. 🔴 ⚠ **The error
propagated one paragraph down under cover of a correction note**: the blockquote repairing the
Chunks-versus-Cells mistake argues from *"District extent decides Goods pooling **and matrix
granularity**"*, and `CONTEXT.md` had already been amended to say the argument *"stands on pooling
alone"*. ***A correction note is trusted more than the text it corrects, so a stale premise inside one
travels further than the error it was written to fix.*** ⚠ **The live cost is at milestone 12**:
[`plans/0037`](0037-goods-between-buildings-the-district-pool.md) decision 1 warns in as many words
against re-attaching the routing role to the District, and a scoping reader who opened `02 §2.1` — the
document that owns the space hierarchy — would re-attach it **on that document's authority**. Struck in
place, with the ADR named at the strike.

**⚠ `06` milestone 12's row inverted `adr/0117`'s blocker count, and lost a whole ground by re-partitioning it.**
Found and fixed **2026-08-21**, on the re-check that row's own last sentence demands before starting the
milestone. The row read *"**three** of its four blockers arrive here and **one** does not"*; `adr/0117`'s
consequences read *"only ground 1 is discharged by arriving at 12"* and *"whoever picks Upkeep up re-checks
grounds **2, 3 and 4**"*. **One arrives and three do not**, and the row said the reverse. ***A summary that
inverts its source's count fails in the reassuring direction, because a row saying three of four are cleared
is a row nobody re-opens*** — and this one is read at exactly the moment somebody is deciding what to scope.
🔴 **The second half is worse and is a new surface for Cause 1.** `adr/0117` has four *grounds*: the loader
refusal, **both missing terms as one ground**, the transfer-versus-purchase **shape**, and the missing actor.
`06` had four *blockers*: counterparty, cost, life, actor. The counts match and the sets do not — splitting
ground 2 into two freed a slot, and **ground 3 fell out of it silently**. That is the ground deciding whether
Upkeep's authored quantity is **money or Materials**, which `adr/0117` assigns to *"whoever builds Upkeep"*.
***A re-partition that preserves the count reads as a restatement and is a deletion***, and the preserved
count is what makes it invisible: a reader checking four against four finds nothing wrong. ⚠ **No mechanical
check reaches this either** — every citation resolves and every link opens, because the row cites `adr/0117`
correctly while disagreeing with it. **Both halves are struck in place in `06` and the row now says re-check
all four.** The re-check itself is recorded there: ground 1 discharged at 12 (`Scope.Pool` is the market),
grounds 2, 3 and 4 open, with `BinOwnerKind` carrying no Segment and `RuleEngine.Bin` still taking
`int building` as the evidence for 4.

**⚠ The board's headline paragraph and the board's own table disagreed about what was in flight.**
Found and struck 2026-08-19 while closing milestone 10. [`0000`](0000-board.md) → *What is next* opened
*"One milestone is scoped and in flight, and it is 7"* while *Do these next* row **1b** read
*"milestone 10 … 🟡 IN FLIGHT"* — both current, both correct about their own subject, and
contradictory read together. **Cause 1** at the shortest range this ledger has recorded: not two
documents, not two files, but **two sections of the page whose entire job is to be the single view**.
***A view is only one copy if it is written in one place***, and a headline paragraph standing beside a
table is two copies with a sentence between them. ⚠ **It was struck by the milestone closing rather
than by anybody noticing**, which is the wrong repair arriving by luck — the paragraph became true on
the day the row it contradicted went away, and would have stayed false for as long as milestone 10 ran.
**No mechanical check reaches it**: every citation resolves, every link opens, and the two statements
are prose about status, which is exactly the class this ledger exists for because no test can hold it.


Unambiguous factual errors, no judgement required.

- [x] `adr/0048` and `adr/0015` — **the Ruleset loader's refusal count of record, stale by 36 and carried in three
      places.** **Found and fixed 2026-08-18**, milestone 10 task 3. The count read *twenty-two at load and a
      twenty-third on reload*, corrected to that figure on 2026-08-11; a walk of the loader put it at **58** before
      this milestone added one. ⚠ **The shape of the drift outranks the size: 17 of the 36 sit in `[[rule]]`,
      `[[resource]]` and `[[building]]`, every one of which existed on the day of the correction.** So the 2026-08-11
      pass walked the four ADRs it knew had moved the number and never walked the loader — ***a count corrected by
      adding what you remember adding is still a count nobody has taken***, which is `adr/0093` applied to a number
      rather than to a mechanism. **Repaired in two moves.** `adr/0048` keeps the number and the enumeration; `adr/0015`
      and this plan's sibling `0003` now **cite** it and state none, because ***the cheapest way to stop two copies
      drifting is to have one copy***. And because the single copy went stale anyway, **check 11** now holds it to the
      build: `RefusalCountTests` counts `RulesetLoader.cs`'s `Refuse(` call sites and fails when `adr/0048` disagrees.
      ⚠ **It is the corpus's first document-to-*code* check** — checks 1–9 are all document-to-document, and **check
      10**, proposed at *A `Rows.Saved` column whose only writer is a test*, named the direction first and is **still
      unbuilt**; this one arrived from a different task and does not discharge it. What it holds is the **site count**,
      which is a fact; the semantic subset is a judgement under `adr/0048`'s own *loads clean and misbehaves in silence*
      rule, and a judgement cannot be a test. ***The checkable part of a claim about the build is the part that is
      counted.***

- [ ] ⚠ **The corpus's count of its own mechanical checks now reads three different values, which is the failure
      `adr/0093` predicted about itself.** `adr/0093` and `CLAUDE.md` say **six** (counting test *classes*), this
      document's 2026-08-18 entry above says *"all eight compare documents"*, and its check-10 proposal says
      *"checks 1–9"*. All three are defensible readings of different units — classes, checks, checks-plus-proposals —
      and **no document says which unit it is counting**, which is why they were never comparable in the first place.
      ***A count with no stated unit cannot drift, because it never agreed.*** Not fixed here: the repair is a canonical
      numbered list of the checks with one owner, and inventing one inside a task about the Ruleset loader is how the
      thing being counted grows a third copy. ⚠ **Do not increment any of the three.** Owner: whoever writes the list.

- [x] `06` — **ten `Placed:` numbers left behind by its own reorder.** **Found and fixed 2026-08-18**, milestone 10
      decision 5. The economic reorder earlier the same day permuted the milestone table — old 9 → **12**, old 11 → **9**,
      old 12 → **14**, old 14 → **11** — and did not reach the *Mechanisms with no milestone* inventory, so ten rows named a
      milestone that had become a different one: Hinterlands and Settlements read **14** (now the Provider List), the
      nine-Resource abstraction read **9** (now land value), the Provider List itself read **12**, and four Layer rows read
      **11** (now the Hinterland). ⚠ **Committed by the same session that wrote the *Retired numbering* block against exactly
      this failure** — `plans/0012` **Cause 2**, a write that did not land, inside the commit that was guarding against it.
      ***A renumbering is a write to every document that names a number, and the table that lists the numbers is the easiest
      one to forget because it looks like the source.*** ⚠ **No mechanical check can see this**: all eight compare documents
      or resolve links, and a stale integer in a prose cell resolves to a real milestone that happens to be the wrong one

- [x] `01 §3` — **half a sentence, from the wrong entry.** **Found and fixed 2026-08-18**, milestone 10 decision 3
      ([`adr/0116`](../docs/adr/0116-the-treasury-opens-empty-and-a-founding-balance-is-a-ratio-this-milestone-holds-neither-side-of.md)).
      It cited *"`CONTEXT.md` → Resource"* for *"a deficit becomes a debt burden and never a stop"*; the sentence is
      under → **Money**, and it continues *"— **but it is a player action, never an automatic overdraft, so the
      treasury genuinely empties and the Rules that could not draw simply wait**"*. The dropped clause is the one
      `adr/0035` §3a wrote **to correct that exact reading**, so §3's conclusion — *"you can overspend… you cannot
      lose"* — was the automatic-overdraft reading `adr/0024`'s own amendment refuses. **Cause 2**: the correction
      reached `adr/0024` and `CONTEXT` → Money and **missed the third document**, which is the one a player-facing
      reader opens first. ⚠ **It is also Cause 5 on a new object.** The reading rule is *quote the sentence, never
      the digits*; here the **caveat was a clause of the sentence itself**, so quoting the sentence was exactly what
      went wrong — ***a half-quote is the one form of miscitation that reads as a faithful one***, and no mechanical
      check in this corpus can see it, since all eight compare whole documents rather than clauses

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

### ~~`plans/0037` task 4 called the migration bound a *work bound*~~ — **PAID in the sitting that found it**

**NEW 2026-08-22, found by reading `adr/0134` before the plan while implementing milestone 12 task 4, and
corrected the same hour.** It is *Cause 4* — a decision taken from a description rather than from the
thing described — with the unusual feature that **the description and the thing described were both in
this corpus and forty lines apart**.

The task entry read *"⚠ **The Cell bound is the fourth §D number** and it is a work bound, so it is the
one most likely to be mistaken for a profiler's choice."* Both
[`adr/0134`](../docs/adr/0134-a-district-is-a-centre-and-its-basin-so-the-count-follows-centres-and-not-a-ceiling.md)
and [`0002`](0002-open-questions.md) §D2 say the opposite, in the same words: *"a boundary migrates by at
most a bounded number of Cells per evaluation, so it never jumps"*, and §D2 glosses it *"how far a
boundary may move per update."*

🔴 **A work bound and a change bound are different numbers and would have been different code.** A work
bound makes the flood incremental — it looks at *N* Cells and resumes next time — and the answer it
produces depends on where it stopped. A change bound runs the whole flood and applies at most *N* of its
conclusions. ***One of them is a profiler's number and the other is a designer's***, which is the
distinction the entry's very next clause was drawing: *"must not size a District from a profiler."*
**The entry contained its own refutation one clause later** and neither half had been read against the
other.

⚠ **What made it visible was reading the ADR first rather than the plan.** The plan is the working
document and the ADR is the record;
[`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)'s
rule is that a description tells you **where to look** — and a plan describing an ADR is a description
in exactly that sense, however operational it reads. ***A task entry is not a specification; it is a
pointer to one.***

- [x] `plans/0037` task 4 corrected, with the wrong words struck rather than deleted so the correction is
      legible.
- [x] The key ships as `[districts] migrate_cells` and its doc comment states the distinction in the
      terms above, so the next reader meets it at the symbol rather than in a plan.


### `PROCESS.md` → *Numbering* has no row for an ADR, and two branches proved it needs one

**Found 2026-08-23, renumbering `0139`–`0142` and `plans/0038` after they collided with `main`.** The
numbering table covers Phase, Milestone, Sub-milestone, Slice, Task, Session, Spike, Round and Plan
document. **It does not cover an ADR at all**, which is the most-cited unit in this corpus.

**The omission is not the missing row — it is that nothing says who owns the next number.** Two
branches each took `0139` while neither could see the other, and the same happened at `plans/0038`
even though the plan-document axis *is* in the table. So the row would not have prevented it.
***Assigning from next-free is a shared mutable counter, and nothing in the corpus serialises writes
to it.***

⚠ **The mechanical checks cannot find this, and that is not a defect in them.** `CitationTests` and
`LinkResolutionTests` resolve links **inside one working tree**, where each branch's numbering is
internally perfect and every link opens. The collision exists only in the *union of two branches*,
which no test ever sees — the *space across working trees* surface milestone 10's collection already
recorded, arriving on the **document** axis rather than the code one.

**What is owed is a decision and not an edit**, which is why this is filed rather than paid. Adding
the ADR row is trivial; *how a number is claimed* — reserved on the branch, renumbered on merge, or
keyed on something that cannot collide — is a real choice with real costs, and it should be taken
once rather than improvised at the next merge. **The mapping for this instance is already recorded**
in [`06`](../docs/06-roadmap.md) → *Retired numbering*, third block, which is what
[`PROCESS.md`](../PROCESS.md) requires of any renumber.

### `adr/0123` says Amenity needs a `kind` column on `BusinessTable`, and a park is not a Business

**Found 2026-08-23, in the sitting that produced
[`adr/0152`](../docs/adr/0152-amenity-counts-building-kinds-and-the-count-belongs-to-the-place-while-the-set-belongs-to-the-household.md).**
Three places say it, and one of them is an exception message a future reader will trust absolutely:

| Where | What it says |
|---|---|
| [`adr/0123`](../docs/adr/0123-desirability-ships-without-its-only-positive-term-and-a-caveat-that-must-travel-gets-a-test.md) | "`BusinessTable` holds exactly three columns … and **no kind**" |
| `LineSourceQueries.cs` — doc comment | "what is missing is that a Business has no type to be distinct in. **One column and a catchment query**, both milestone 15's" |
| `LineSourceQueries.cs` — the `NotSupportedException` | "what does not is **a kind on a Business**, so there are no distinct types to count" |

**It is wrong in the expensive direction: it makes milestone 15 look bigger than it is.** A `kind`
column on `BusinessTable` cannot enumerate a **park**, and
[`adr/0032`](../docs/adr/0032-services-are-delivered-by-trips-not-by-coverage.md) had already made a
park an Amenity entry — *"widen Business to destination and a park is an Amenity entry"* — before
`0123` was written. Nor could it enumerate a school, a clinic or a beach. Under `adr/0152` the key is
the **`[[building]] kind`**, which every Building already carries, so ***no column is owed at all***
and what milestone 15 owes is the catchment query and nothing else.

⚠ **This is Cause 4 with a new surface, and the surface is the interesting part.**
[`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)'s
rule is that a description of the build tells you *which symbol to read* and never *what is in it*,
and that where such a sentence is wrong it is wrong about the **trigger**. Here the sentence is wrong
about **what would discharge it** — it names the work a hole needs, and it names too much. ***A
description of a blocker is a description of the build, and it decays the same way***: `0123` was
right about `BusinessTable` on the day it was written and was already stale against `adr/0032`, which
predates it.

🔴 **And the third row is the one that matters, because an exception message is not a document.** None
of `tests/Borough.Tests/Corpus/` can see it — the checks are document-to-document by construction — so
the sentence most likely to be believed by somebody picking up milestone 15 cold is the one sentence
nothing in this repository is watching. It is the same blind spot this ledger already records against
doc-comments, arriving in a string literal.

**The repair is three edits and a widening**: correct `adr/0123` with a correction block rather than a
rewrite, correct both sentences in `LineSourceQueries.cs`, and note that `CONTEXT.md` → Amenity has
now been amended to carry `adr/0032`'s widening, which is the root the other three grew from.

### `CLAUDE.md` says `[households]` and `[traffic]` are `congested.toml`'s alone, and six files state one

**Found 2026-08-23, while measuring the land value pass for milestone 24 task 3.** Two cells say it.
The Constants table's *Household car ownership* row reads **100% — `rulesets/congested.toml` only**,
and the repository map's `rulesets/` cell calls `congested.toml` *"the only file that states
`[traffic]` and `[households]`, because a generated city cannot congest itself"*.

**Counted rather than quoted** (`CLAUDE.md`'s own rule for this cell):

| Table | Files that state it |
|---|---|
| `[households]` | **six** — `bordered`, `congested`, `crowded`, `fouled`, `scarce`, `taxed` |
| `[traffic]` | **two** — `congested`, `fouled` |

⚠ **The volume-delay row is wrong the same way and separately**: it carries
*`rulesets/congested.toml` only* against a `[traffic]` that `fouled.toml` also states.

**What it cost, which is why this is filed rather than fixed in passing.** The sentence is not merely
out of date — it is *load-bearing for a reading of a measurement*. `CLAUDE.md` states, correctly and
in the same row, that **absent means nobody drives**. So a reader who finds no Vehicle in motion on
`bordered.toml` and remembers *cars are congested.toml only* has a ready-made explanation that is
false, and it terminates the enquiry. That happened on the day this was filed, and what it nearly
buried is [`0002`](0002-open-questions.md) §B's **five Vehicles against 937** — a live question about
whether the commute generator or the Commute Budget is the cause.

***A staleness claim of the form "X only" decays every time somebody adds a file, and nothing warns
them.*** That is the shape rather than the instance: the count is maintained in one document and the
exclusivity is asserted in another, so the second goes wrong silently the moment the first grows.
`CLAUDE.md` already carries the antidote for exactly this in the same cell — ⚠ ***count them rather
than quoting a total*** — and the antidote is stated about the *number of Rulesets* while the
*membership of a table* two lines away is asserted flat.

**The repair is two halves and only the first is an edit.** Correct both cells; and note that a
mechanical check is available here in a way it is not for most of this ledger, because both sides of
the claim are in the repository — the assertion is in `CLAUDE.md` and the ground truth is a `grep`
over `rulesets/*.toml`. `tests/Borough.Tests/Corpus/` is document-to-document by construction and so
cannot see it today. **Not written here, because whether the corpus tests may read the Rulesets is a
decision and not a chore.**

### `CLAUDE.md`'s assertion tier is **42s at 1,690 tests** and five readings since say **3m02s–8m29s at 1,974–2,311** — 🔴 **and there have been TWO jumps, the second on 2026-08-25**

⚠ **A third reading, 2026-08-22, by milestone 24's scoping: 3 m 42 s over 2,002 tests, on `main`.**
🔴 ***It is an upper bound and not a figure, because its first control was not held*** — two other
worktrees were live on the same six cores, and this document's own rule is that a test-cost capture is a
parallelism measurement. **It is recorded because a spoiled reading still bounds**, and it bounds in the
same direction as the two below: the count is still climbing and the duration with it. **What it adds is
that the drift is not a one-day artefact.** The edit still waits on a reading taken on a quiet machine.

**NEW 2026-08-22, found while gating milestone 12 task 3, and filed rather than fixed because a
replacement figure is a *capture* and this sitting was not set up to take one.**

The table under *Running the tests* states the default lane at **42s, 1,690 tests**, and it names
*nothing else running in this repository* as its first control. Two runs today, back to back, each on
the whole assertion tier, reported **3 m 02 s / 3 m 03 s over 1,974 tests**. ⚠ **The test count grew
17% and the duration grew about fourfold**, so growth alone does not account for it.

🔴 **What this entry is NOT is a claim that task 3 caused it.** That was checked rather than assumed
(`adr/0093`): the first of the two readings was taken while the watershed ran from
`CommandKind.Populate`, so most of the suite never called it at all, and the second was taken after it
moved to `SyntheticCity.PopulateInto`, where every fixture does — **the two readings are within a
second of each other**. World creation was then timed directly at **2.13–2.23 s** on `minimal.toml`
(no `[districts]`, so the derivation returns immediately) and **2.14–3.06 s** on `twinned.toml` (two
Districts derived), three runs each: ***the watershed's cost is below this instrument's noise floor.***

⚠ **A THIRD READING, 2026-08-24: 3 m 10 s over 2,094 tests**, taken while decomposing milestone 27.
**The count has grown 1,690 → 1,974 → 2,094 and the duration has not come back down**, which is what
makes this an entry about a stale figure rather than about one bad afternoon. ⚠ **It is an upper bound
on the same terms as the other two** — chrome, slack and a media player were up — and it is recorded
**because it agrees**, not because it improves on them. ***Three upper bounds that cluster still do not
make a figure***, and the table under *Running the tests* still says 42s.

⚠ **What is owed is a reading, not an edit.** ***A test-cost capture is a parallelism measurement, so
it takes a parallelism measurement's controls*** — the rule this document already carries from the
2026-08-14 threading capture and the 1m52s/50s pair before it. Today's two readings were taken with
file edits happening alongside them, which makes them **upper bounds** and nothing better, and an
upper bound is exactly what must not be pasted into a table that a reader will treat as the working
loop's cost. **The number to replace it with comes from a deliberate act on the reference machine
with the room quiet** ([`adr/0121`](../docs/adr/0121-the-commit-gate-is-the-assertion-tier-and-a-long-test-runs-post-submit-on-a-machine-that-is-not-yours.md),
[`adr/0106`](../docs/adr/0106-a-wall-clock-budget-names-a-machine-class-and-a-thread-count-or-it-is-not-a-budget.md)).

⚠ **It matters beyond tidiness because the figure is load-bearing.** `adr/0121` bands the working loop
at *past five minutes a test stifles iteration and ten is the ceiling*, and 42s reads as a lane with
four times the headroom it may actually have. ***A stale duration does not merely misinform; it hides
how close a gate is to the band that would reopen the ADR.***

⚠ **A third reading, later the same day: 3 m 02 s over 1,974 tests**, taken detached while this
session did nothing but wait on it — so the tightest controls of the three, and it lands on the first
reading to the second. **Three readings now agree and the row still may not be closed by them**, because
agreement is not a control: all three were taken on a machine nobody had quieted deliberately, and
`adr/0106` asks for the machine and the thread count, not for a consensus. ***What repetition buys is
confidence that 42s is wrong; it does not buy the number that replaces it.***

⚠ **A fourth reading, 2026-08-23, milestone 25 task 1: 3 m 08 s over 2,064 tests** — taken with a
second test host running against the same tree, so **the loosest controls of the four** and an upper
bound like the rest. 🔴 **What it adds is not confirmation, it is an ATTRIBUTION**: between the third
reading and this one the test count grew **1,974 → 2,064**, about 4.6%, and the duration grew
**3 m 02 s → 3 m 08 s**, about 3.3%. ***Growth and duration moved together at this end of the range and
did not between 1,690 and 1,974*** — where the count grew 17% and the duration roughly fourfold. **So
whatever produced the fourfold jump is not the suite getting bigger**, and it is still unidentified.
⚠ **This does not close the row and moves it no nearer closing**, for the reason above: four
uncontrolled readings agreeing is four uncontrolled readings. ***It narrows what the missing capture
has to explain.*** ⚠ **The COUNT is a different fact from the duration and it has now drifted twice**
— `CLAUDE.md` says 1,690 — but it is deliberately **not** corrected here, because this row's own rule
is *what is owed is a reading, not an edit*, and editing half a figure would leave a table whose count
was taken on one day and whose duration was taken on another. ***A figure is replaced whole or not at
all.***

🔴 **A FIFTH READING, 2026-08-25, milestone 26 task 1: 7 m 50 s over 2,311 tests** — and it is the
first one that does not agree with the others. Three consecutive runs the same day gave **6 m 40 s,
7 m 28 s and 8 m 29 s**; the `time`-measured wall on the instrumented run was **485 s**. ⚠ **Same loose
controls as readings three and four** — chrome, slack, a media player and a second session up, though
the only other test host on the tree was an *idle* VS Code one at 0% CPU — ***so it is an upper bound
like the rest.*** **The count grew 2,094 → 2,311, about 10%, and the duration grew 3 m 10 s → 7 m 50 s,
about 150%.** So there has been a **SECOND** jump, and this row's *"whatever produced the fourfold jump
is not the suite getting bigger"* now has a second instance of the same shape.

✅ **AND THIS READING CARRIES THE ATTRIBUTION THE OTHER FOUR COULD NOT, because it was taken with
per-test timings rather than as a total.** `--logger trx`, aggregated by class. The sum of per-test
durations is **3,063 s across 2,311 tests** against 485 s of wall, so the harness is getting about
**6.3× parallelism** — and the total is dominated by a handful of classes:

| Class | Per-test seconds | Tests | First committed |
|---|---|---|---|
| `FoundingTests` | 352.7 | 5 | 2026-08-24 |
| `PolicyTests` | 219.5 | 6 | — |
| `LandValueSteadyStateTests` | 211.4 | 2 | 2026-08-20 |
| `SaveLongRunTests` | 208.9 | 2 | 2026-08-18 |
| `MoneyCensusTests` | 201.5 | 9 | 2026-08-19 |
| `CarOwnershipTests` | 159.7 | 8 | 2026-08-14 |
| `BusinessLevyTests` | 136.4 | 2 | 2026-08-24 |
| `GoldenHashTests` | 124.9 | 13 | — |

🔴 **THE HYPOTHESIS THIS SUPPORTS, STATED AS ONE: the suite did not get slower because it got bigger,
it got slower because a small number of LONG-RUN STEADY-STATE TESTS landed in two clusters, and the two
clusters are the two jumps.** `SaveLongRunTests` (2026-08-18), `MoneyCensusTests` (2026-08-19) and
`LandValueSteadyStateTests` (2026-08-20) total **622 s** of per-test duration and all landed between the
1,690 reading and the 1,974 one — ***the window of the unexplained fourfold jump.*** `FoundingTests` and
`BusinessLevyTests` total **489 s** and both landed **2026-08-24**, after the fourth reading was taken
and before this one — ***the window of the second jump.*** ⚠ **It is a hypothesis and not a finding**
([`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)):
what would settle it is checking the four earlier readings out and re-timing them per class, which this
sitting did not do. **What it does establish is where to look**, and no previous reading established
that at all.

⚠ **These are correctly tiered and that is the uncomfortable part.** The axis is *what would you do on
the day it failed* — and a steady-state acceptance run going red means **something broke**, not that a
constant moved. ***So none of them is a mis-tagged instrument, and the tier cannot be made fast by
re-labelling.*** The question `adr/0121`'s band actually raises is whether an assertion that runs
100,000 Ticks belongs in the lane you run *while working*, and that is a design question rather than a
correction.

🔴 **`TierBudgetTests` IS CLOSER TO FIRING THAN ANYBODY HAS NOTICED.** `TierBudget.PerTest` is **4
minutes**, and the slowest single assertion is
`LandValueSteadyStateTests.The_field_oscillates_within_a_bound_and_does_not_trend` at **211.4 s** —
**within 12% of the budget**, on an upper-bound reading. ***The guard that was built so a slow test
goes red rather than quietly becoming the critical path is about to do exactly that***, and when it
does, `CLAUDE.md`'s own warning applies: it is *not a licence to raise the budget.*

⚠ **This still does not close the row, on its own stated terms.** Five uncontrolled readings are five
uncontrolled readings, and `adr/0106` asks for a machine and a thread count rather than a consensus.
***What changed is that the missing capture now has something to explain rather than only a number to
replace.***

- [ ] Re-capture the assertion tier on the reference machine, quiet, Release, and correct
      `CLAUDE.md`'s table **as a pair — count and duration from the same run**. 🔴 **It has now landed
      past five minutes on three consecutive readings, so `adr/0121`'s band is the next question and
      it is no longer a footnote.**
⚠ **Sixth and seventh readings, 2026-08-26, by milestone 26 task 4, and they take the band's top OUT: 8 m 21 s and 9 m 22 s over 2,317 tests**, both Release, both on the reference machine with nothing else in this repository running, the second immediately after the first. ***So the range in this heading is now 3m02s–9m22s and the ceiling moved by a minute in one afternoon.*** ⚠ **The same session read 7 m 47 s at 2,317 the day before**, which is a **20% spread across three readings of one tree at one count** — and that spread is the finding, because ***a measurement that moves 20% between consecutive runs cannot resolve the two jumps this row is trying to attribute.*** 🔴 **What it does settle is `adr/0121`'s band**: *past five minutes a test stifles iteration and ten is the ceiling*, and the assertion tier is now **within 40 seconds of that ceiling on its worst reading** — so the gate the whole tier exists to keep cheap is about to stop being cheap, on a preference no measurement can settle. ⚠ **Task 4 added two tests and 6,144 Ticks of `provisioned.toml`, which is not where a minute came from**; it is recorded here rather than diagnosed, and it does not move `CLAUDE.md`, which stays wrong at 42s until somebody takes a controlled capture.

- [ ] Re-time the four earlier readings **per class** to settle whether the two jumps are the two
      long-run clusters. ***It is the cheapest thing that would turn this row's hypothesis into a
      finding***, and it needs no new instrument — only `--logger trx` at four earlier commits.


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

### `adr/0037`'s band and `plans/0004`'s tripwires name no machine, and now a rule says they must

**Owed by [`adr/0106`](../docs/adr/0106-a-wall-clock-budget-names-a-machine-class-and-a-thread-count-or-it-is-not-a-budget.md),
2026-08-16.** That ADR states the rule — *a wall-clock budget names a machine class and a thread count,
or it is not a budget* — and deliberately does not edit two decisions somebody else argued. Both were
already filed as a pattern in `plans/0002` (*"thresholds whose meaning depends on an unstated machine"*)
and now have a rule to be repaired against.

- [ ] [`adr/0037`](../docs/adr/0037-the-world-is-single-buffered-and-hazards-are-per-table.md)'s
  **8–15 ms** async-copy band. **Both M4 Pro figures fall below its floor** (3.077 ms and 3.105 ms), so a
  fast host does not *pass* the band — it leaves it. A range meant as *acceptable* reads as *expected*.
  Say which machine class 8–15 ms describes.
- [ ] [`plans/0004`](0004-s4-kernel-benchmark.md)'s **ratio tripwires**, written against a hand-computed
  ideal that binds on one machine and not the other. K1's ratio-to-ideal *"degrades from 1.10× to 1.99×
  without the code changing"*, and the footgun variant reaches 4.53% on a fast host, firing a wire it
  does not deserve to fire.
- [x] The **15.6 ms Tick budget** itself — **PAID** by `adr/0106` and carried in `plans/0013`,
  `CLAUDE.md` and `plans/0000-board.md`: *one core of the reference class*.

⚠ **The thread count is the half most likely to be dropped**, because it does not look like part of a
duration — and `plans/0013`'s lever 2 is that everything measured is single-threaded while Tick phase 2
is parallel by construction. That clause is owed to session **R** rather than to this file.

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
      placement**. Milestone **19** is *Households, the Unplaced Pool and Departure* and is the obvious
      home, but 19 as written is about where Households come *from*, and this is about where they go
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

**1. ~~`adr/0041` still says the travel-time matrix is District-granular, and it is unbannered.~~ PAID
2026-08-14**, in a session running beside milestone 5c and *before* 5c read the file — which is the only
reason it is worth recording how narrowly this one was caught. Original entry follows.
*"The matrix remains District-granular; only *attribution* leaves."* [`adr/0047`](../docs/adr/0047-routing-never-keys-on-the-district.md)
reversed exactly that — *"the travel-time matrix's granularity is the routing partition, not the
District"* — and `CONTEXT.md` → District carries the reversal. `adr/0041` **has** an amendment block and
it does not touch the bullet, which is the worst arrangement: a reader who checks for a banner finds one
and concludes the document has been reconciled. It is **5c's foundation**, and it is wrong in the ADR a
reader reaches for first. *Cause 2 — an ADR issued a write to another document and it did not land.*

> **The banner is now on the bullet itself rather than at the top of the file, and that placement is the
> repair.** This debt's own diagnosis — *a reader who checks for a banner finds one and concludes the
> document has been reconciled* — is an argument about **where** an amendment sits, not about whether one
> exists, so a second top-of-file block would have reproduced the defect it names. ***An amendment
> belongs against the sentence it corrects, because that is the only place a reader who is not looking
> for it will pass.***
>
> ⚠ **The polarity is worth carrying: the vocabulary file was right and the decision record was wrong.**
> `CONTEXT.md` → District and → Settlement both had `adr/0047`'s reversal, the second naming the ADR
> outright. Cause 1 is normally read as *the copies drift and the authoritative one is right*; here the
> authoritative one was the drifted copy, and the cheap check — *does the glossary agree?* — would have
> caught it any day in the last three.

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

### A revisit period is described as coverage and delivers a rate, in a doc comment and in a refusal the user reads

*(Found 2026-08-21 by `plans/0035` task 7 — **F24** — when a test failed on it. Owner:
`src/Borough.Core/Rules/Ruleset.cs` and `src/Borough.Formats/RulesetLoader.cs`; a wording defect with
a live consequence, not a design change.)*

`PlacementRuleset.RevisitTicks` is documented as *"how long the placement pass takes to look at
everybody in the Unplaced Pool once"*, and the loader repeats it almost verbatim in the refusal text a
designer reads when the key is wrong. **`PlacementEngine.DrawPool` takes `sample` independent uniform
draws over the Pool and deduplicates nothing** — a draw *with replacement* — so over one revisit period
each member is looked at about **once on average** and about **1/e of them are not looked at at all**.
***The period is a rate. It has never been coverage.***

**The consequence is live rather than cosmetic**, which is why this is not filed as a typo. Milestone
11 task 7 hangs the Unplaced Pool's give-up bound on being *tested* when a member is next drawn, so
the sentence above reads as *a Household leaves at most one revisit period late* — and the first
draft of that mechanism's doc comment asserted exactly that, reasoning from the name. It is wrong:
the lateness is geometric with no upper bound. `adr/0006` is satisfied by a different argument
entirely — the sample scales with the Pool, so the drain rate is proportional to the stock and the
Pool's *size* is bounded even though one Household's *wait* is not.

⚠ **`adr/0059` is not implicated and must not be swept in with this.** Its argument is that an
absolute count makes the fraction of the queue cleared per cycle shrink as the queue grows, and that
argument is about the *rate* and is entirely correct. What drifted is a sentence describing the
mechanism's coverage, which is `adr/0093` from the usual side: ***a description of the build is where
to look and never what you found.***

**Two repairs, and the second is the one that matters.** The doc comment can say *how often a member is
looked at* rather than *how long until everybody has been*. The refusal text is read by a designer who
has no access to `DrawPool`, so it is the copy that cannot be checked against the code by its reader.
⚠ **Whether placement should sweep on a rotating cursor instead — which would make the period mean what
it says — is a hash-bearing change to placement and is a question rather than a correction.** It is
named here and not decided here.

### A world's seed has two sources, and the doc comment that says this is filed here is what filed it

*(Found 2026-08-12 by `plans/0023` task 5, which added the second source, and by the user asking on the
same day what needed reviewing. Owner: `src/Borough.Core/Entities/World.cs`; a signature sweep, not a
design change.)*

`World` now holds a `Key` — `Randomness.Draw`'s first coordinate — because `CommuteRoster` is
`(derived AND rebuilt)` from it and `RebuildDerived()` takes no arguments and must not start taking
them. **Every other mutator on the class still takes a `WorldKey` as a parameter.** So one world has
**two sources for one seed**, and nothing checks that they agree — a caller passing a different key would make the arming
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

⚠ **The membership is named rather than counted, because this entry has now drifted in both
directions** — `plans/0035` **F22**, found 2026-08-21 by milestone 11 task 6. It previously read
*"`CreateBuilding`, `DestroyBuilding`, `Adopt` and the rest, nine call sites in all"*, and
`DestroyBuilding` **no longer takes one**, so the ledger's own worked example had gone stale. In the
other direction, milestone 11 task 5 added `World.TryArrive` to the list without noticing and task 6
removed it again — ***a new instance of a filed pattern is not caught by having filed it.*** As of
2026-08-21 the members are, in declaration order:

`Adopt`, `Migrate`, `CreateBuilding`, `Fit`, `ArmingStagger`, `EvictOverflow`, `Loser`, `LosingWorker`
— **eight**, plus the constructor, which legitimately *receives* the key rather than re-supplying it.

⚠ **A ledger of debts accrues debt, and that is this entry's second lesson.** `plans/0012` stores a
fact — *which symbols carry this defect* — with no mechanism keeping it true, which is **Cause 1** with
the drifted copy inside the audit itself. A mechanical check that a named symbol still has a named
parameter is something `tests/Borough.Tests/Corpus/` could hold; it is **filed rather than built**,
because one instance is not a pattern yet.

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

### Six saved columns have no mechanism, and `adr/0093`'s repair passes on every one of them

*(Found 2026-08-15 by the corpus sweep behind [`06`](../docs/06-roadmap.md)'s inventory rows. Owners:
[`adr/0104`](../docs/adr/0104-a-skill-tier-is-earned-by-attendance-and-the-credential-stays-a-wall.md),
[`adr/0026`](../docs/adr/0026-wages-are-posted-locally-and-never-cleared.md), and `GoldenFixtures.cs`.)*

**`CitizenTable` declares `Activity`, `SkillTier`, `Employment`, `Experience`, `Age` and `Health` as
`(saved AND hashed)`. Nothing in `src/` writes or reads any of them.** Each has exactly one writer in
the whole repository and it is the same block of six consecutive lines in a **test fixture** —
`GoldenFixtures.cs:355`–`:360` — filling them with arithmetic patterns (`i % 4`, `i * 1_009`,
`100 - i`). `HouseholdTable.LifeStage` is the seventh and differs only in that its one writer is in
`src/`: `World.cs:647`, at creation, and nothing ever reads it back.

**The columns are not the defect.** A column declared ahead of its mechanism costs nothing —
[`adr/0086`](../docs/adr/0086-a-save-has-no-schema-of-its-own-and-the-field-declaration-is-the-format.md)
makes the declaration the save format, so declaring early is how the format stays honest, and the
fixture writing non-zero values is what makes the State Hash cover them. Two ADRs describing them as
live is the defect, and the general lesson is worth more than either.

⚠ **A declared column is the strongest description of the build there is, and `adr/0093`'s repair does
not catch it.** That ADR's writing half is ***name a symbol, never a time*** — *"at world creation"*
cannot be checked without already knowing the answer; *"when `SyntheticCity` runs"* is one grep. Here
the grep **succeeds**. `CitizenTable.SkillTier` exists, resolves, is typed, is hash-bearing, and appears
in the save. A reader following the rule exactly finds the symbol and concludes the mechanism is there.
***A symbol is evidence that something was declared and never that anything calls it***, so the check
has to be *a symbol with a caller in `src/`*, and `adr/0093` asks only for the symbol. This is **Cause 4
one level down**: not a description of the build that is wrong about a trigger, but a **declaration in
the build** that describes a mechanism nobody wrote — and unlike a doc-comment it cannot rot, because it
was never a claim about behaviour in the first place.

- [ ] **`adr/0104`** — its Consequences say the schooling tier *"has been stored and read by school
  demand alone"*. **Both halves are false**: `grep -rn schooling src/` returns nothing, and there is no
  school demand. It is the one sentence in that ADR that asserts something about the build rather than
  about the design, and it was written on 2026-08-15 by the sitting that grilled `04 §7`.
- [ ] **`adr/0026`** — *"Jobs specify a **minimum**, not a match"* and the underemployment readout
  *"34% of your advanced-tier workforce is in basic-tier jobs"* both read as live. `EmploymentEngine`
  (`src/Borough.Core/Rules/EmploymentEngine.cs`) reads **no tier at all** — a Citizen takes the first
  free slot inside the Commute Budget — so underemployment is not merely unreported, it is
  **unrepresentable**. Say so where the readout is named.
- [ ] **`GoldenFixtures.cs:356`** — `(byte)(i % 4)` writes **four** values into a **three**-tier space,
  so one Citizen in four in the committed baseline holds tier **0**, which
  [`adr/0104`](../docs/adr/0104-a-skill-tier-is-earned-by-attendance-and-the-credential-stays-a-wall.md)
  refused by name the day before this was found. Harmless today because nothing reads it; it becomes a
  seeded invalid state the moment something does. ⚠ **Fixing it re-records three baselines**, which
  [`adr/0100`](../docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md)
  says is not a reason to defer.
- [ ] **A judgement, not a correction**: whether `Age` and `Health` should be declared at all yet.
  `adr/0010` makes Age *"a static attribute drawn when a Citizen arrives"* and nothing draws it, while
  Health acquired an owner on 2026-08-15 — it is one of
  [`adr/0103`](../docs/adr/0103-a-need-is-where-a-frequent-private-failure-accumulates.md)'s four Needs,
  and a Need is a Household accumulator rather than a Citizen byte, so the **column and the decision
  disagree about which record holds it**. The other five have owners in `06`'s inventory.

**The mechanical form is check 10 and it is cheap**: a `Rows.Saved` column whose only assignment in the
tree is under `tests/` is a mechanism nobody built, announced by the build itself. It would have found
all six on the day each was declared, and — unlike checks 1–9, which are all document-to-document — it
reads the **code** to hold a **document** to account, which is the direction this corpus has never been
able to check in.

### `CONTEXT.md`'s specimen abandonment sentence promises a **window**, and the window is the only part of it the build cannot reach

*(Found 2026-08-16 while grilling [`06`](../docs/06-roadmap.md) milestone 6 — Evidence, the
accumulators — before scoping it. Owners: `CONTEXT.md` → Failure Pressure,
[`adr/0097`](../docs/adr/0097-a-reach-failure-is-counted-on-the-citizen-and-a-stock-failure-is-not-remembered-at-all.md),
and [`02 §9`](../docs/02-simulation-model.md).)*

**`CONTEXT.md:203` states the retained condition a player is shown when a Building is abandoned —
*"abandoned: 74% of work trips exceeded commute budget over 30 days"* — and it is quoted as the model
of what `LEGIBLE CAUSE` means.** It decomposes into five clauses, of which **four are in hand at one
instant inside one method** and the fifth is not reachable at all:

| Clause | Status |
|---|---|
| *exceeded commute budget* | **Exists.** `TripFate.ExceededCommuteBudget`, decided in `TripEngine.Start` |
| *work trips* | **Exists.** `TripPurpose.Commute`, a parameter of that method |
| *to this Building* | ⚠ **In hand and discarded.** `toBuilding` is a live parameter at the refusal; the Trip row stores road Addresses and the Building slot is not persisted |
| *74%* | **Two integers.** A numerator and a denominator on the Building, incremented at a site that already holds every operand |
| *over 30 days* | 🔴 **The gap.** A trailing-window rate needs either per-Building history or a reset cadence, and a reset cadence is a new hash-bearing number |

**The ratio is cheap and the window is the decision.** `TripEngine.Start`'s budget test —
*"a person who can see the journey is too long does not make two thirds of it and stop"* — reaches its
verdict twelve lines below a signature carrying `toBuilding` and `purpose`. So the design can already
produce *this Building has refused 74% of the commute Trips aimed at it, **ever***. What it cannot
produce is the trailing window, and the window is what makes the sentence a **diagnosis** rather than a
lifetime average: a Building that was fine for a year and unreachable for a month reads healthy on a
lifetime ratio, which is the direction that fails silently.

⚠ **The window is the question [`adr/0053`](../docs/adr/0053-failure-pressure-is-a-duration-not-a-tally.md)
already answered once, and its answer was to refuse the obvious shape.** A decaying average is a tally
with a decay rate, and that ADR deleted exactly that — *"a tally needs a decay rate authored, tuned and
ratified. A duration needs none"* — recording it as the second time the cheapest way to satisfy
`adr/0052` was to **find the derivation that removes the choice**. So a decay rate proposed here is
running against a decision taken deliberately, and the first move is to look for the derivation.

⚠ ~~The candidate worth trying: report the rate over **the same window condemnation is judged over**,
which `ZoneRuleEngine.Condemn` already computes as `kind.CondemnAfter × rule.Rate` — derived, already
hash-bearing for another reason, and it makes the reported number and the lethal number the same
number.~~ **WITHDRAWN hours later, on the same day: it is refused by name by
[`adr/0079`](../docs/adr/0079-a-building-outlives-its-frontage-and-an-address-that-has-none-is-a-hole-the-trip-model-reports.md).**
That ADR's revisit triggers require a later mechanism needing a duration to be *"built once, named, and
given a threshold with a ratifier — **not bolted onto `0053`**, whose predicate is about Bins and whose
`CondemnAfter` is denominated in a **Rule's rate**. Two pressure sources sharing one threshold would
make the number mean two things."* A Ruleset halving a Rule's rate would silently halve the Evidence
window. ***A derivation that reuses a constant inherits every decision that constant is already
carrying*** — `adr/0094`'s `Speed.PerKilometrePerHour` literal on a third axis, and the reason
`02 §2.1` splits the Cell from the Chunk. **The search for a derivation is the right first move and it
has not succeeded**, which under `adr/0043` is a result to write down rather than an absence of one.
⚠ **The whole question moved to milestone 17 on 2026-08-16** — decline owns it, because *Evidence
reports pressure and does not produce it*, and `06`'s inventory already parks a sibling window question
there (`01 §6`'s sustained-detection duration).

⚠ **`02 §9` recorded this identical defect once already, on the other axis, and the Building axis is
unrepaired.** *"`jobs beyond budget` counts Citizens the Commute Budget excluded and keeps **no entity
reference**, so a 100,000-Tick run could report *distance rather than supply is what separates them* in
aggregate and name **nobody** it was true of."* `adr/0097` repaired it with a reach-failure count **on
the Citizen**, resetting on success. `TripCounter.ExceededCommuteBudget` is the same aggregate figure
with the same missing reference, on the **Building**, and the repair shape transposes without argument.
***A defect repaired on one axis of a symmetric pair is not thereby repaired***, and nothing in this
corpus walks the other axis when one is fixed.

⚠ **And *`adr/0097` repaired it* was a sentence about a decision doing the work of a sentence about the
build, for four days.** `CitizenTable.ReachFailures` did not exist until milestone 6 task 3 on
2026-08-17; before that, `grep` found no such symbol anywhere in `src/` or `tests/`, and this paragraph
read as though the Citizen axis were closed. It is closed **now**. `adr/0093` governs the form —
***a description of the build is where to look and never what you found*** — and the writing half is
the fix here as everywhere: **name the symbol**, because *"`adr/0097` repaired it"* cannot be checked
without already knowing the answer and *"`CitizenTable.ReachFailures` holds it"* is one grep.

- [ ] **`CONTEXT.md:203`** — the sentence is aspirational and reads as descriptive. It sits in the file
  that governs vocabulary, so it is the copy most likely to be quoted as a specification. Either mark the
  window as owed, or replace the specimen with one the build can produce.
- [ ] **`02 §9`** — add the Building axis beside the Citizen one it already carries, so the pair is
  visible as a pair.
- [ ] **A judgement, not a correction**: how the window is denominated, given that a decay rate is
  refused by `adr/0053` and reusing `CondemnAfter × rate` is refused by `adr/0079`. **Owed by `06`
  milestone 17**, not by milestone 6 — the counter and its window moved there on 2026-08-16, because
  Evidence reports pressure and does not produce it. See [`0028`](0028-evidence-the-accumulators.md)
  → *What this milestone must not do*.

⚠ **How this was nearly filed wrongly, which is Cause 4 in a form that will recur.** The first draft of
this entry said the rate was *"not computable, blocked on an unbuilt link"*. That came from a **subagent's
survey of the code**, which said accurately that *a Trip does not know which Building it went to* and
that *the Building slot is not persisted* — both true **of the Trip row**. The inference drawn from them
— that the Building is therefore unavailable — is false, and it survived because nobody opened
`TripEngine.Start`. ***A generated survey of the build is a description of the build***, so `adr/0093`
governs it exactly as it governs a doc-comment: it tells you which symbol to open and never what is
inside it. This is worth recording on its own, because a survey is *more* persuasive than a doc-comment —
it is current, it cites line numbers, and its facts are individually correct — and this corpus is going to
read a great many of them.

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

7. ~~**Every invariant `02 §10` names has a member of the `Invariant` enum, and every member is either
   registered or explicitly marked unbuilt with the milestone that owes it.**~~ **BUILT 2026-08-19**,
   `tests/Borough.Tests/Corpus/InvariantCoverageTests.cs`, and **it found three gaps rather than the one
   it was filed over — two of which no milestone owns.** See below. **Filed 2026-08-12 by
   session H** ([`adr/0084`](../docs/adr/0084-parking-occupancy-is-two-checks-and-an-invariant-over-absent-state-cannot-be-written.md)),
   which found *parking occupancy is conserved* specified in **four** documents — `adr/0009`, `02 §10`,
   `05 §60` and `06`'s milestone 7 risk — and built in **none**. **This is check 5's shape pointed at
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

   ✅ **Built 2026-08-19, in three assertions, and the design note about the copy decided the shape.**
   Reading arbitrary prose out of `02 §10` is brittle, and a hand-written list in the test is *Cause 1*
   inside the instrument — so **the convention became the check**: the tier table now names each
   invariant's enum member in backticks, and a member that does not exist cannot be named. The three
   assertions are *every name in the table resolves to a member*, *every member is live, retired or
   `[Unbuilt]`*, and *every `[Unbuilt]` member is named in the table*. The third is the one that keeps
   the marking honest — ***a gap declared only in the enum is invisible to every reader of the document
   that owns the tiers***, which is the same failure in the opposite file.

   ⚠ **It found three gaps, and only one of them was the one it was filed over.**
   ~~`ParkingOccupancyIsConserved` is owed by milestone 7 task 6 and was the founding case.~~
   **CLOSED 2026-08-19 by milestone 7 task 6** — the member is live, registered in the end-of-run tier,
   and the `[Unbuilt]` marking is gone. ⚠ **The gap it left behind was in `02 §10` and not in the enum,
   and the check could not see it**: the tier table named the member in its **per-Tick** row, which is
   the tier `adr/0084` had already demoted it out of, so the document was pointing at a live member and
   describing the wrong frequency. ***A check that an obligation has a member does not check that the
   row it sits in is true of it***, and the same hole is open for every other name in that table.
   **`GoodsAreConserved` and `CitizenIsInExactlyOnePlace` are named in `02 §10`'s staggered tier, have
   never had a member, and are owned by nothing** — not deferred, not gated, not refused; no row in
   `06`, `0003` or `0002` claims either. ***An obligation nobody scheduled is indistinguishable from one
   nobody wrote down***, and both had been sitting in the tier table since before the ADRs existed.
   `CitizenIsInExactlyOnePlace` also has a live near-namesake, `CitizenIsInExactlyOneHousehold`, which
   is a claim about **membership** where this one is about **location** — ***a live invariant with a
   similar name is how an unbuilt one stays invisible.***

   ⚠ **`[Unbuilt]`'s argument is a string and the honest value is often *nothing*.** The filing says
   *marked unbuilt with the milestone that owes it*, and two of the three had no milestone to name. The
   attribute takes free text so that **"nothing owns this"** is expressible, because that is the case no
   board or roadmap is showing anybody — a marking that could only name a milestone would have forced a
   lie or an omission on exactly the two entries that most needed recording.

   ⚠ **The enum's ids collided across two sessions on the day this was built.** `ParkingSpaceIsReleasedOnce`
   shipped as **40** from the parking branch and `MoneyIsConserved` shipped as **40** from the money
   branch; the merge resolved it by moving parking's to **41**. Harmless here — neither had reached a
   crash artifact — and it is the plan-number collision again on a third axis. `adr/0084` refuses to
   reserve ids in advance precisely because *an id travels in a crash artifact and a reused id cannot be
   un-reused*, which makes **concurrent** allocation the residual risk that refusal does not cover.

12. **Every invariant `02 §10` names sits in the tier it is registered into — NEW 2026-08-19, from
    milestone 7 task 6, and it is check 7's own blind spot.** Check 7 asks whether every name in the
    tier table resolves to a member, and it passed the whole time `ParkingOccupancyIsConserved` was
    named in the **per-Tick** row — the tier
    [`adr/0084`](../docs/adr/0084-parking-occupancy-is-two-checks-and-an-invariant-over-absent-state-cannot-be-written.md)
    had demoted it out of before the member existed. ***A check that an obligation has a member does not
    check that the row it sits in is true of it***, and the failure is worse than a missing member
    because the document reads as covered by an instrument that is genuinely running.

    **The mechanical form is cheap and the tier is already legible from the code.** A `Walk` registered
    through `Register(InvariantTier.EndOfRun, …)`, a `Sweep` through
    `Register(InvariantTier.Staggered, …)`, and a per-Tick check is a bare `Require` at a write site
    with no registration at all — so the tier a member is *in* is derivable from `src/` by which
    registration mentions it, and the tier a member is *claimed* to be in is the row of the table its
    backticked name appears in. ⚠ **A member may legitimately appear in two tiers** — `02 §10` says so
    itself, *where a corpus invariant splits across two tiers, both halves are here* — so the assertion
    is that every row naming it is one of the tiers it is registered into, never that there is exactly
    one.

    ⚠ **This is the third time a check has been filed off the thing it was already meant to catch**, and
    it is the same shape as check 8's own note that the cheaper check was found by committing the defect.
    ***An instrument's blind spot is discovered by the first task that walks into it***, which is an
    argument for building the founding case rather than admiring the check that declared it.

13. ~~🔴~~ ✅ **CHEAP HALF BUILT 2026-08-22, the same sitting it was specified** —
    `tests/Borough.Tests/Corpus/LedgerAgreementTests.cs`, asserting the **row sets** agree. ⚠ **The
    status half is still owed** and still needs check 2's machine-readable line. ***Building the half
    that works is the whole point of the entry below.***

    🔴 **It failed on its own founding case before it passed, and then failed a SECOND way that
    mattered more.** `06` carries the **identical** table header for Phase 1 and Phase 2, so anchoring
    on the header alone silently compared **Phase 1's table against Phase 2's ledger** — a green-looking
    comparison of two unrelated things. ***A check keyed on a string that is not unique is a check on
    the wrong rows***, and nothing about its output said so; it took reading the names it printed.
    Anchored on the section heading instead. **The violation was then written and watched to fire**:
    deleting milestone 26's ledger row reports *in 06 and not in 0003's Phase 2 ledger: 26*.

    **Milestone status agrees between [`0003`](0003-build-plan.md)'s Phase 2 ledger and
    [`06`](../docs/06-roadmap.md)'s milestone table — NEW 2026-08-22, and it is CHECK 2 on the axis
    check 2 does not cover.** Check 2 is *slice* task status between `0003` and the slice plan, and it
    says what it needs: *"one machine-readable line per slice."* ⚠ **It was specified and never
    built** — and the same defect has now recurred on the **milestone** axis, which is the finding
    rather than the sighting. ***A check specified and left unbuilt does not hold the ground it
    describes; it records that somebody once knew.***

    **The sighting.** Milestone 12 was capped at task 6 in `06`, with its risk rewritten and its tasks
    7–10 moved to milestone 26. `0003`'s Phase 2 ledger — ***the document that answers what is
    done*** — went on reading **`🟢 LIVE. Scoped and decomposed 2026-08-22 — ten tasks`** for a whole
    commit, and `plans/0000` went on promoting milestone 12 as the code row to do next.

    🔴 **All thirty-one corpus checks passed while the two disagreed**, and that is the point. Every
    check this corpus owns compares **links and shapes** — does the target resolve, does the table
    render, has a closed row left, does each ADR have a coverage row. **None compares a CLAIM in one
    document against a claim in another.** ⚠ *Milestone 12 is live* and *milestone 12 is capped* are
    both well-formed, both link correctly, and are flatly contradictory.

    ⚠ **It was found by a person asking whether the boards were updated**, not by an instrument —
    which is the same way `plans/0012` **Cause 1** was found in the first place. ***This is Cause 1
    on the milestone axis***, and Cause 1's own headline is *every document that stores per-slice
    status drifted, and the only large one that did not stores none*. **`0003` is now a large document
    that stores milestone status**, which is exactly the category the Cause names.

    **The mechanical form, and the blocker is the same one check 2 named.** Both tables are keyed by
    the milestone number, so the row **sets** can be compared today and cheaply — *every milestone in
    `06`'s table has a Phase 2 ledger row and the reverse* — and that alone would have caught 25 and
    26 existing in one file and not the other. **What cannot be compared is the STATUS**, because it
    is prose in both. ⚠ **So the cheap half should be built now and the expensive half needs the
    machine-readable line check 2 has been waiting for since the sweep began.** ***Build the half that
    works rather than deferring the whole again***, which is what left check 2 unbuilt.

    🔴 **And the sighting turned up a second, narrower defect the same check would not catch.**
    `06`'s placement rows say **`Placed: 12`** and name a *milestone* rather than a *task* — so when
    12 split, *"the nine-Resource abstraction; Utility families; Waste"* could not be mapped to
    either half and had to be flagged rather than moved. ***A placement row that names a milestone and
    not a task cannot survive that milestone being split***, and until 2026-08-22 no milestone had
    ever been split, so nothing had tested it. **A check that every `Placed: N` names a milestone that
    exists is trivial and would not have helped**; the real repair is that a placement names what it
    is placed *against*.

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

7. ~~**The board keeps its own three rules.**~~ **BUILT 2026-08-22** —
   `tests/Borough.Tests/Corpus/BoardShapeTests.cs`, assertion tier, four checks: no question posed, no
   table cell over three sentences, no closed row still standing, and a ceiling on the whole file in
   **both** lines and bytes.

   **The argument for a test rather than a fourth clearing is the first three.** `plans/0000` was
   founded at 132 lines, reached 1,234 in four days, was cleared by hand, reached 1,504, was cleared by
   hand again, and reached 925. ***Two hand-clearings, both grown back within days***, and nothing in
   this directory looked at the board through any of it.

   ⚠ **Its polarity was checked against the real inflation and not only against a fixture.** Run over
   `ca91e86^`'s 925-line board, all four file-reading checks fire, and check 2 reproduces — line for
   line — the ten over-ceiling cells an independent measurement had found, worst at **15 sentences and
   3,987 characters**. *A check that has only ever passed is a check whose polarity nobody knows.*

   ⚠ **Two limits are stated in the test rather than left to be discovered.** Rule 1 catches the
   **literal** form only: a question mark is detectable and *"the payee is unsolved"* is not, so a green
   result is evidence about punctuation and not a certificate. And **all four catch the symptom, never
   the cause** — the 2026-08-22 inflation happened because `plans/0003` covered Phase 0 and Phase 1
   only, so Phase 2 status had no owner. ***A document that declines a layer does not thereby abolish
   it***, and the ceilings are tripwires rather than budgets: when one fires the question is *which
   document should have held this?*

Neither is a substitute for the restructure. A check over three tables that disagree only tells you
they disagree; the point of thinning is that there is one place to be right. **Checks 4 and 6 are the
exceptions to that sentence**, for opposite reasons — thinning cannot help check 4, because its
documents do not disagree with each other but are all stale against a third thing neither stores; and it
cannot help check 6 either, because there the documents are both *correct* and the defect is in what one
of them declined to copy.

---

## Filed 2026-08-20, by the §B allocation measurement — two findings it did not go looking for

Both surfaced while running six full unfiltered suites for `plans/0002` §B on an M4 Pro. Routed here
on the day under [`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md),
before anything was worked around, because neither is owned by the code that found them.

### The suite's test count is quoted as 1,745 in two documents and read 1,779 six times

`plans/0000-board.md` and `plans/0033-conserved-money-and-the-treasury.md` both state the whole
unfiltered suite is green at **1,745** on 2026-08-20. Six unfiltered Release runs at `243f22b` on that
same date reported **1,779 total, 0 skipped**, every time. **A gap of 34, and it is stable rather than
flapping**, so it is not a conditional-fact or a platform-skip artefact.

⚠ **`243f22b` did not cause it.** That commit is the probe, and `git show --stat` says it touched nine
files and added no `[Fact]` — it instrumented eight assertions that already existed. So the 1,745 was
already wrong when it was written, or was taken against a different tree than the one it names.

**This is *Cause 5* in its plainest form and also its most boring**: a count is a four-digit figure that
travels beautifully and carries no clause, and both sightings are the same digits in two documents, which
is *Cause 1* underneath. ⚠ **No mechanical check in this repository can see it** — every corpus check
compares a document to another document, and here the two documents **agree with each other** and are
both stale against a third thing neither stores. That is the shape `check 4` is already recorded as
being unable to help with.

**Needs judgement**: whether the repair is to correct the digits or to stop quoting a suite size in
prose at all. `CLAUDE.md` already says *count them rather than quoting a total* about the ADRs, for
exactly this reason, and nothing extends that rule to tests.

⚠ **And the assertion tier has drifted the same way, in four places.** `CLAUDE.md`, `plans/0002`,
`plans/0032` (twice) and [`adr/0121`](../docs/adr/0121-the-commit-gate-is-the-assertion-tier-and-a-long-test-runs-post-submit-on-a-machine-that-is-not-yours.md)
all state the commit gate is **1,690 tests**. `--filter "tier!=instrument"` at `243f22b` reports
**1,760**. ✅ **Reproduced on the reference machine 2026-08-20 — 1,760 passed, 0 failed** — so this
half is confirmed on *both* machines rather than argued from one. ***That is the entry's own claim
being tested rather than asserted***: a count carries no machine, so if the two machines had
disagreed the finding would have been something else entirely. **A gap of 70**, and it is the same defect one lane over — so the repair, whatever it is,
has ~~**six sites**~~ **seven sites across six documents** and not two — `CLAUDE.md`, `plans/0032`
**twice**, `adr/0121` and `plans/0002` for the 1,690, and `plans/0000-board.md` and `plans/0033` for
the 1,745. ⚠ **The site count was itself off by one**, corrected 2026-08-20 by opening all seven,
which is this entry's own defect arriving inside the entry that reports it. ⚠ **And all five 1,690
sites state the count and the 42s *in one sentence*** — so a repair that corrects the digits must
leave the duration's machine caveat standing beside them, and cannot be a blind substitution. ⚠ **Do not read the wall clock beside it the same way**: the 42s is
correctly caveated to the reference machine and this run's **27s** is a different machine, so those two
figures do not disagree. ***A count carries no machine and a duration does***, which is why the count
drifted silently and the duration could not.

### `JobSearchBoxTests.The_box_is_not_where_the_pass_spends_its_time` is a wall-clock ratio in the assertion tier, and it failed one run in six

Run 2 of 6 went red on it: **a 5.34× box cost 2.21×** against a bound of **2.2**. The other five runs
passed. It is **untagged**, therefore assertion tier, therefore it gates every commit.

⚠ **The bound is not obviously wrong, and that is the finding.** Its own comment records the derivation
— five runs on 2026-08-19 spanning **1.73–1.84** with the `decide` guard off, and 2.2 chosen
*deliberately wider than the observed band* because 5c task 8 found that a band transplanted from a
quieter quantity fails one run in ten with nothing wrong under it. **It then failed one run in six on a
different machine**, at 2.21 — over by half a percent.

**So the test measures wall clock and the corpus has a rule about that.**
[`adr/0106`](../docs/adr/0106-a-wall-clock-budget-names-a-machine-class-and-a-thread-count-or-it-is-not-a-budget.md)
says a wall-clock budget names a machine class and a thread count or it is not a budget. This assertion
names neither, and its comment's *"the ratio survives a slow CI box where an absolute would not"* is the
reasoning that a **ratio** is machine-portable. ⚠ **A ratio of two timings taken inside a 14-way
parallel suite is not machine-portable, because the two arms contend for cores differently** — the wide
arm is longer and therefore more exposed to whatever else the scheduler is running. ***A ratio removes
the machine's speed and does not remove its scheduler.***

⚠ **It is separately in tension with `adr/0121`**: an assertion is a test that fails when the city
changes, and this one can fail when the city has not changed. By `plans/0032`'s own discriminator —
*what would you do on the day it failed* — the answer here was **re-run it**, which is neither of the
two permitted answers.

**Needs judgement, and three ways out are visible**: widen the bound again (which buys another machine
and loses the guard), tag it `instrument` and move it post-submit (which matches what it actually is and
loses the commit-time guard entirely), or replace the timing with a **counter** — box cells walked per
pass — which is what the test is really asserting and is machine-independent by construction. **The
third is the only one that does not trade the guard away**, and it is more work than the sitting that
found this had. Recorded rather than built.

---

## Filed 2026-08-22, by the board's third clearing — four findings that lived only on the board

All four were written into [`0000`](0000-board.md) as narrative and exist **nowhere else in the
corpus**, which is how a view accumulates the only copy of something. Routed here on the day under
[`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md),
before the board was cut, because a clearing that deletes the last copy of a finding is not a clearing.

### A repair driven by a diagnosis instead of by a diff added rows the diff would have shown

Restoring what the `0d8b114` merge was believed to have dropped, a sitting read it as having *also*
removed milestone 7's cleared row from the board's per-milestone gate table, and restored one. But
`0d8b114` had **replaced** that row with a longer version of itself, so the table came out carrying
**two** milestone 7 rows where it had carried one. The restoration's other half — milestone 8's own
gate row, three lines further down — went untouched.

***A repair that reasons about what a merge must have done adds rows a diff would have shown were
already there.*** The check that costs nothing is `git diff <pre-merge tip> <merge>` on the one file.
Both struck 2026-08-19. ⚠ **This is the same failure mode as Cause 5 one level up**: the sitting
worked from a *description* of the merge rather than from the merge, which is
[`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
reaching version control.

### Check 8 fired on citations that were correct and merely early

`plans/0030` and `adr/0110` existed only on an unmerged branch, so **check 8** — every relative link
resolves — failed on references that were perfectly correct and simply ahead of their targets. Both
resolve as of `0d8b114`, and **a merge discharged the finding rather than an argument**.

***A cross-reference is a claim about the tree and not about the document***, so a link check run
against one branch reads a multi-branch corpus as broken. Recorded rather than repaired: the check is
right and its failure was informative, but a reader meeting the red without this note would go and
"fix" a correct citation.

### Two sessions collided on a plan number and nothing in the scheme could have stopped them

Both scoped into `plans/0030` on 2026-08-17, **two and a half hours apart**; parking moved to `0031`,
because Save/load was first and [`PROCESS.md`](../PROCESS.md) → *Numbering* never re-uses a number.

***The next free plan number is a fact about the tree that two sessions read at the same time and
neither can hold.*** ⚠ **This is a fourth axis of the board's own *the three tracks contend*** — not
files, not conclusions, not cores, but **names**. It is a distinct instance from the `0112`/`0113`
**ADR**-number collision recorded above: same class, different axis, and the filenames-per-number check
derived there does not reach plan numbers. **The board is what would have shown it, and neither
session's board edit was visible to the other.**

⚠ **Recurred 2026-08-22.** A second session was found writing `adr/0136`, `adr/0117` and `06` while
this file's own sitting was cutting the board — detected by file mtimes rather than by anything the
corpus does. ***A scheme that allocates names by reading the tree cannot be made safe by a rule***, and
nothing here yet proposes the machinery that would be.

### `06`'s Milestone column had become a status column, under a header forbidding exactly that

**Found while cutting the board, because the same question — *who owns per-milestone status* — has two
wrong answers and they were both live.** [`06`](../docs/06-roadmap.md)'s header states it is
authoritative over *"the phase model, the four rules below, and the risk each milestone retires.
**Nothing else**"*, and assigned live status to `plans/0000`. Beneath that sentence, the *Milestone*
column carried ✅ marks, completion dates, plan links, task counts, decision counts and **11,138
characters** of findings across eight rows. **Milestone 8's cell ran to 7,496 characters against 22 for
its name.**

***A column that names a thing had become a column that reports on it***, and the rule it broke was
stated four lines above it. This is **Cause 1** on its third document — `plans/0000` and `plans/0003`
being the other two — and the sharper form of it: *a header is not a mechanism, and a document that
declares its own scope does not thereby keep to it.*

⚠ **It is also `adr/0042` — *a planning document cites and a design document owns* — on the surface
that ADR was written for.** `06` had accumulated eleven false claims once before by copying originals
that later moved, which is why the header exists; the repair then did not reach the table.

**Struck 2026-08-22.** The Milestone column is now the milestone's name and nothing else, for all
twenty-four rows, so shipped and unshipped are indistinguishable there — which is correct, because
`06` sequences and [`0003`](0003-build-plan.md)'s Phase 2 ledger says what is done. ⚠ **Every finding
was verified present in its own plan document before removal**, twice and independently, after a
similar check on the board had returned one false *covered*.

### The two scopings' renumbering counts disagree, and neither may be quoted

One says six and six; the other disagrees. **The reconciliation is owed at merge and neither number
should be quoted until then.** ⚠ ***A milestone number is neither a symbol nor a time***, so
`adr/0093`'s *name a symbol, never a time* does not reach it — it is the one citation form that rule
leaves uncovered, and it is exactly the form a renumber invalidates. Related to the retired-numbering
table's two-column defect recorded above, and not the same finding.


---

### 🔴 A GATE RUN IN A SHARED WORKING TREE IS A GATE ON A TREE THAT NO LONGER EXISTS

**NEW 2026-08-22, found by running milestone 12's own gate and watching it come back green on a
world that had changed underneath it.** ⚠ ***This is the finding, and the thing it corrects is a
sentence this corpus added deliberately eight days ago.***

`CLAUDE.md` → *Running the tests* carries an amended reading of
[`adr/0121`](../docs/adr/0121-the-commit-gate-is-the-assertion-tier-and-a-long-test-runs-post-submit-on-a-machine-that-is-not-yours.md):
***a quiet machine is a control on a capture and not on a run***, and therefore *"the 36-minute suite
may be run detached alongside other work, including other tests — the only thing lost is a figure
nobody was going to take."* **That paragraph is correct and it is correct about the wrong hazard.**
It reasons entirely about **noise**, because the misreading it was written to repair was somebody
declining to run a suite in case CPU contention spoiled it.

**What happened.** The unfiltered suite was started at ~21:13 and reported **2,082 passed, 0 failed,
38 m 03 s, exit 0**. A second session was editing `src/` in the same working tree at **21:13–21:16** —
**207 insertions across five files**. Re-running the corpus filter on the settled tree afterwards gave
**two failures**: `RefusalCountTests` (`RulesetLoader.cs` at **139** `Refuse(` sites against
`adr/0048`'s recorded **138**) and `DocCommentAttachmentTests` (two stacked `<summary>` blocks on
`RoadGenerator.cs:658`). ⚠ **Both are document-to-code checks, both read the file from disk at run
time, and both take under a second** — so they executed in the suite's first minute, read the
**pre-edit** files, and passed. ***The green was true of a tree that stopped existing three minutes
into a thirty-eight minute run.***

🔴 **The consequence is not a spoiled figure, it is a spoiled VERDICT.** A capture that runs in a noisy
room returns a number that is too large, and its own caveat says so. **A gate that runs in a moving
tree returns `Failed: 0`, which is the same sentence a real pass returns and carries no caveat at
all.** ***Noise degrades a measurement; a concurrent editor falsifies an assertion.*** Milestone 12's
Definition of done was recorded as met on this run and **was not met**.

⚠ **It is not `bin/` contention, and that was the first diagnosis, stated to the user and wrong.** Two
`dotnet test` invocations sharing an output path is a real hazard and is **not** this one: the failures
reproduced on a settled tree with nothing else building. ***A wrong cause that predicts the symptom is
the most expensive kind*** — `plans/0012` **Cause 4** on a process rather than on a mechanism.

⚠ **What is NOT owed is a retraction of the amendment.** It answers its own question correctly and the
question it answers is real. What is owed is the second half it never had: **a control on the tree, to
sit beside the control on the room.**

- [ ] Amend `CLAUDE.md` → *Running the tests*: a **milestone gate** names an unchanging tree the way a
      **capture** names a quiet machine. ***The room is a control on the number; the tree is a control
      on the verdict.*** A worktree is the mechanism that already exists — three are checked out.
- [ ] 🔴 **Re-run milestone 12's gate on a settled tree.** Its row in
      [`0003`](0003-build-plan.md) reads **CLOSED** and the Definition of done behind that word has
      **not** been satisfied — the only unfiltered run against it is the void one above. ***A closure
      resting on a void gate is Cause 1 with no second copy to disagree with it.***
- [ ] Consider whether a gate should record the tree it ran against, so a green that outlived its
      world can be recognised as one. ⚠ **A commit hash is not sufficient** and that is the whole
      point: every one of the 207 insertions was **uncommitted**, so the `HEAD` at the start and the
      `HEAD` at the end were identical and told nobody anything.

---

### Cause 7 — a description takes its noun from the build, and the build is behind the design

**Sighted once, on the day it was written.**
[`adr/0139`](../docs/adr/0139-a-district-pool-is-a-market-and-not-a-store-so-stock-stays-with-the-seller.md)
says a seller's Goods stay in *"the selling **Building's** own Bin"* and, sixty lines later, that *"a
seller's money Bin is a **Business** balance that already exists."* ***One seller, two custodians, in
one record.***

**Why it happened, and it is not carelessness.** The author checked the build — which is
[`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
working — and the build says `BinTable.Owner` is a `HandleColumn<Building>`. **So the noun was correct
about the code and wrong about the design**, because
[`adr/0113`](../docs/adr/0113-a-business-is-an-occupant-with-its-own-balance-and-a-building-never-holds-money.md)
and [`adr/0114`](../docs/adr/0114-a-balance-a-rule-can-fail-on-is-a-bin-and-a-bins-owner-is-discriminated.md)
had already decided the owner should be the actor and the build had not caught up.

⚠ **This is the inverse of Cause 4 and the two must not be merged.** Cause 4: *the description was
wrong, so the decision was wrong.* Cause 7: ***the description was RIGHT about the code, and the code
was the thing lagging.*** **The corrective is opposite too** — Cause 4 says *go and read the symbol*,
and here reading the symbol is what produced the error.

**The corrective.** ***Before naming an entity in a design record, check whether an ADR has already
moved it.*** A revisit trigger that has already fired is the signal: `adr/0113`'s reads *"`BinTable.Owner`
is a `HandleColumn<Building>`, so no Household, Business or treasury can own a Bin today"* — **a
sentence describing the exact state the new record then wrote down as though it were the design.**

**The tell, and it is cheap.** ***A record that contradicts itself two paragraphs apart is reporting
two different layers*** — one sentence from the design and one from the build. Neither is a typo, and
proofreading does not catch it because both halves are locally true.

🔴 **No mechanical check reaches this**, and it is not obvious one could: every check here is
document-to-document, and both halves of the sentence resolve. **A candidate exists and is not
proposed** — *an entity named in an ADR must not contradict the same entity named in an ADR that
supersedes or amends the one that introduced it* — but it needs an entity vocabulary the corpus does
not have. **Routed to session V** ([`0039`](0039-session-v-the-business-is-the-actor-and-the-building-is-premises.md))
as evidence rather than as a task.

---

## Filed 2026-08-22, by milestone 24's scoping — two defects, an unenforceable rule, and one recurrence of the naming hazard

### `deferred.md` says Sealing's terrain-keyed recovery **already does** something it structurally cannot

[`docs/deferred.md`](../docs/deferred.md):52, arguing for *absorption varies by ground*:

> *"a tree-planting programme raises what the ground absorbs and shows up slowly, over the whole area,
> **exactly as `Sealing`'s terrain-keyed recovery already does**."*

**It does nothing, and it cannot.** `MapLayers.DecaySealing` (`Space/MapLayers.cs:416`) has no caller in
`src/`; `LayerSchedule.For` answers `Never` for `Layer.Sealing`; and — the part no document had —
🔴 **`MapLayers.Seal` (`Space/MapLayers.cs:393`) has no `src` caller either**, so
`LayerCellTable.Sealing` is a **saved, hashed column that is identically zero on every world this build
can generate**. A recovery over a field nothing writes is not slow; it is absent.

⚠ **This is Cause 4 with the polarity reversed and it is worth separating.** Cause 4 is *a decision taken
from a description of the code, where the description is wrong about the **trigger***. Here the
description is wrong about **whether the mechanism runs at all** — and it is load-bearing, because the
sentence's job is to establish that a **precedent exists** for the mechanism being argued for. ***A
citation offered as precedent is the one kind of description that is never checked, because the reader
is checking the argument and not the example.***

⚠ **And `adr/0124` enumerated the blockers and counted two of three** — `sealing_decay_tau = 0` and
`Step` never calling `DecaySealing` — **both of which are downstream of the missing write path.** Fourth
sighting of the enumeration defect, after `adr/0062`'s Cap admission ranks, `03 §4`'s demotion fields and
`adr/0117`'s four grounds. Owned by [`0042`](0042-terrain-and-the-land-rows.md) **F3**, which is where
the repair order is corrected; `adr/0124` needs the amendment.

### `06`'s inventory row said both that terraforming was placed and that it was not

[`06`](../docs/06-roadmap.md):330, in one cell: *"✅ **Placed: 24.** a milestone. **The version numbers
are paid** … **Terraforming still owes a milestone**"*.

**The row carried three mechanisms** — terraforming, procedural generation guarantees, and the three save
header version numbers — and **one status column**. The version numbers were struck as paid; *Placed: 24*
was presumably about the generation guarantees; and terraforming's real status is the last clause, four
sentences after a green tick.

⚠ ***A row that carries three mechanisms carries one status***, which is this document's **granularity**
defect — a status coarser than the claims it covers — arriving in an inventory table rather than in a
`plans/0002` §D row. It is the same shape as the three free-flow speeds sharing one ratifier because they
shared a row.

🔴 **It was load-bearing rather than untidy.** Milestone 24's decision 2 turns on whether terraforming is
available, because
[`adr/0021`](../docs/adr/0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md) makes it
the difference between terrain as a **price** and terrain as a **wall** — and a scoping session reading
this row would have taken terraforming as placed at 24 and shipped the height field on that basis.
✅ **PAID 2026-08-22 by `plans/0042` decision 2**: the row is split in two, terraforming's is **UNPLACED**,
and it now says it owes a **verb** before it owes a milestone
([`adr/0157`](../docs/adr/0157-height-does-not-ship-until-terraforming-does-because-terrain-without-a-price-is-a-wall.md)).

### `adr/0021` called a rule **checkable** for four years, and nothing could have checked it

[`adr/0021`](../docs/adr/0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md):31:

> *"**The checkable rule: if a terrain value is read inside a Tick phase, something has gone wrong.**"*

**Two things are wrong with it, and the second is the interesting one.**

🔴 **It would have gone red on the thing that satisfies it.** World creation in this build is an event in
the Input Log — `SyntheticCity.PopulateInto` is dispatched from `Simulation.cs:391` on
`CommandKind.Populate`, **inside Phase 0** — so the generator reads height *inside a Tick phase* on the
Tick that makes the world (`0041` **F6**).

🔴 **And nothing enforces phase discipline at all.** `TickPhase` is referenced by its own file and by
`Simulation.cs`, and by nothing else in the repository. There is no analyser, no test and no fixture that
knows which phase a call sits in — the same standing as `05 §4`'s **lint 4**.

⚠ **So the word *checkable* was doing the work of a mechanism that was never built**, which is
[`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) arriving from an
unfamiliar direction: not *an absence read as a constraint*, but ***a constraint asserted as though its
enforcer existed.*** A reader auditing terrain reads would have gone looking for the check and found
nothing to run.

✅ **PAID 2026-08-22 by `plans/0042` decision 3**, as an **amendment in place** rather than a new ADR —
the design content belongs to
[`adr/0157`](../docs/adr/0157-height-does-not-ship-until-terraforming-does-because-terrain-without-a-price-is-a-wall.md),
and a second home for one decision is **Cause 1** by construction. The rule is restated against **state**:
***terrain height is not state***, which is checkable by inspection because no height column exists.
⚠ **The mechanical check becomes owed the day terraforming lands**, since *seed + edits* stores heights on
edited Chunks — recorded in the amendment so that milestone finds the obligation.

### The naming hazard recurred a **third** time, and this time it was detected before the collision

Recorded above: two sessions collided on `plans/0030`; the `0112`/`0113` ADR collision; and a recurrence
on 2026-08-22 *"detected by file mtimes rather than by anything the corpus does."*

⚠ **2026-08-22, milestone 24's scoping: three worktrees were live at once** — `city-sim` on milestone 12,
`city-sim-m18` on milestone 18, `city-sim-q8` on `main` — and the scoping session read `git worktree
list` and the branch diffs **before** claiming `plans/0042`. The plan number was free; **the next ADR
number was not safely claimable**, because the milestone-12 branch holds unmerged work with open
decisions 3, 7 and 10 still to settle, any of which lands an ADR.

***So the entry above is right that a rule cannot make the scheme safe, and wrong by implication that
nothing shows the collision*** — `git worktree list` plus a branch diff shows it, and it is what a
session should read before claiming any number. **That is a habit and not machinery**, it is not
mechanically checkable, and it does not close this item. What it does establish is that **the tree
already carries the fact**; what is missing is anything that makes a session look.

### The naming hazard recurred a **fourth** time, and this time it collided

⚠ **2026-08-23.** The entry above records the third recurrence as *"detected before the collision"* and
credits `git worktree list` plus a branch diff with showing it. 🔴 **That reading was right about the
mechanism and wrong about the outcome: the collision happened anyway.** The milestone-24 scoping session
read the tree on 2026-08-22, found `plans/0040` free, claimed it, and took `adr/0143`–`0149`. The
milestone-25 session then committed **a different `plans/0040`** and **a different `adr/0143`** directly
to `main`. Both names existed, on different branches, naming different documents.

🔴 **Git cannot see it and will not say so, and the reason is sharper than *the filenames differ*.**
***`git merge` treats the number as part of a FILENAME; a citation treats it as an IDENTITY.*** The two
documents differ as names — the number is a prefix and the claim is the rest — so a merge takes both,
cleanly, and every `adr/0143` citation in the corpus silently acquires two referents. **The tool that
would catch the collision is looking at a different object from the one the corpus is.** That is why it
merges clean, and it is why no amount of care with `git` finds it: ***the check is not weak, it is
pointed elsewhere.***

⚠ **This is the diagnosis rather than the symptom, and it says where a mechanical check would have to
stand**: on the corpus's object, not on git's — something that reads the *set of numbers in use across
every live branch* and asks whether one names two claims. **Nothing in `tests/Borough.Tests/Corpus/`
can do it.**

🔴 **And the reason is sharper than *the tests are aimed elsewhere*, which is how this entry first put
it and is too broad.** ***The tests' object is a CHECKOUT.*** They are aimed correctly at every fact
that fits inside one working tree, and blind to exactly the class that does not:

| | Where the fact lives | Can a corpus test see it? |
|---|---|---|
| **§F2's ADR count** — *"149 written, numbered to `0158`"* | document-to-**filesystem**, inside **one** checkout | ✅ **Yes, and one does** — `CoverageMapTests.cs:170` asserts both the count and the high-water mark against `docs/adr` on disk, and its message says *"this is the fourth time"* |
| **Two branches naming one `adr/0143`** | only in the **difference between two** checkouts | 🔴 **No, and not by oversight** — ***in each tree separately, nothing is wrong*** |

***Same ledger, opposite outcomes, and the whole of the difference is whether the fact fits inside a
single working tree.*** ⚠ **So the count line is the useful contrast rather than a second example**:
it drifted four times, somebody built a check for it, and the check works — which is what the
collision cannot have, standing in either tree.

⚠ **The count line also shows the honest way to keep a quoted total.** It is in tension with
`CLAUDE.md`'s *count them rather than quoting a total*, and the tension is **deliberate**: somebody
decided a reader skimming §F2 needs to know how far the map reaches, then **paid for the convenience
with a test**. ***A guarded total is a different thing from a total nobody guards***, and only the
second is this document's Cause 1.

***A collision that resolves as a clean merge is worse than a conflict, because a conflict stops
somebody.***

⚠ **None of the above is this session's and it is credited rather than absorbed** — the milestone-25
session offered the filename-versus-identity mechanism on 2026-08-23 in reply to the note telling it
which numbers had been freed, then **corrected this entry twice more**: it found `CoverageMapTests`
after this document had asserted *nothing downstream complains* — ***a claim about the build made
without reading it***, which is [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
committed inside the ledger that exists to catch it — and it narrowed its own *aimed elsewhere* to the
one-tree/two-tree line above. **The correction arrived because the two sessions talked**, which is the only channel that
carried it: no document either of them could read would have produced it, since the fact lives in the
*difference* between two branches.

✅ **Resolved by renumbering the milestone-24 branch**, 2026-08-23: `adr/0143`–`0149` → **`0150`–`0156`**,
`plans/0040` → **`plans/0042`**, `0144`–`0149` left free. **One side moves, not both** — the branches
agreed which before either edited. The 32 corpus tests pass, which is what says the citations resolve.

⚠ **What the third entry got right stands and is now load-bearing**: the tree carries the fact and
nothing makes a session look. ⚠ **What this recurrence adds is that reading the tree ONCE is not enough**
— milestone 24 read it correctly on the day and was overtaken afterwards. ***A number claimed against a
set that is still growing stays unsafe for as long as the branch is unmerged***, which is the original
entry's own sentence arriving with a second half. **This does not close the item.**

🔴 ⚠ **FIFTH RECURRENCE, 2026-08-25, and it is the FIRST that an agreement had already covered.**
`plans/0042` records a split *agreed with the milestone 27 session on 2026-08-24* — this branch
`0150`–`0158`, milestone 27 `0145`–`0149` — and `main` then committed `adr/0150` *appearance is
derived in the shell* on the same day. **This branch moved again: `0150`–`0161` → `0151`–`0162`**,
61 files of citations, `main`'s `0150` untouched.

⚠ **The direction was not the agreement's to decide, and that is the finding.** The agreement said
this branch owned `0150`; the repair still moved this branch, because by the time anybody looked
`main`'s `0150` was **published and cited from three documents**, and moving it would have re-used a
number for different work — which `PROCESS.md` → *Numbering* forbids outright. ***So an allocation
agreement is only binding until one side publishes; after that the published side wins whatever was
agreed.*** That is not a reason to stop agreeing splits — it is the reason a split must be **claimed
in the tree** rather than in a conversation, because only the tree is visible to the branch that
breaks it.

⚠ **Two of the six merge-conflict files were rewritten three times by one pass** over `git ls-files`,
because ***an unmerged path is listed once per stage***, and a script that walks that list touches a
conflicted file three times and every other file once. **The renumber was +3 on exactly the files a
human was already reading closely** and +1 everywhere else. Caught by a dangling-link sweep, not by
review. ***A file list taken during a merge is not a file list***, and `git ls-files` is the specific
trap: it is the obvious way to enumerate the corpus and it is wrong for as long as a conflict stands.

⚠ **The reservation in [`0036`](0036-the-coarse-day-wheel.md) did not prevent it and was not consulted**
— that document reserves **0140–0149** for the coarse-day-wheel track, and both milestone 24 and
milestone 25 took numbers from inside it. ***A reservation nobody reads is a comment.***

## Filed 2026-08-24, by milestone 27's decomposition — two doc comments wrong about the city

**Both were found by [`0041`](0041-the-business-is-a-thing-the-city-contains.md) while sizing tasks 7
and 8, and both are the same shape.** ⚠ **Every mechanical check in `tests/Borough.Tests/Corpus/` is
document-to-document, so a claim living only in a doc comment is invisible to all of them** — which is
the property that let `BusinessTable`'s *"nothing funds one"* sit unread for eight milestones. ***The
class is not new; the two sightings are.***

### `WorldInvariants.cs:1014` says no shipped Ruleset declares a job, and all thirteen do

**`0041` G7a.** The comment above `Invariant.CitizenIsInExactlyOneWorkplace` reads *"Unemployment is the
common case rather than the exception here — no shipped Ruleset declares a job — so the exemption
carries almost every row, and what it is actually checking is the other direction."*

🔴 **Every one of the thirteen shipped Rulesets declares `jobs = 8` on its `dwelling` kind.** So the
exemption carries a *minority* of rows and the invariant is doing the work the comment says it is not.
It was true when written — the comment dates itself to **milestone 5b-bis task 2** — and `jobs` landed
on the shipped files at **5b-bis task 4**, ***two tasks later in the same slice.***

**This is [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
exactly**: the sentence is right about which symbol to read and wrong about the **trigger**. ⚠ **What it
costs is not correctness but attention** — a reader deciding where to look for an employment defect is
told this invariant is nearly vacuous, and it is not.

**The repair is one sentence** and it is owed by milestone 27 task 7, which re-subjects the invariant
anyway.

### `SyntheticCity.cs:257` claims the only production issuance of money, and it names its own expiry

**`0041` G8.** The comment reads **"THE ONLY PRODUCTION ISSUANCE OF MONEY IN THE BUILD"**, and the very
next clause says why it was true: *"`adr/0024` makes the Outside Connection money's only source and that
is milestone 11."*

🔴 **Milestone 11 shipped.** `World.cs:1242` endows an arriving Household from its Hinterland's
`emigrant_balance` band, through the same `World.Endow`. ***There are two production issuances, the
comment says one, and milestone 27 task 8 proposes a third.***

⚠ **This one is worse than stale and the difference is the point.** The comment does not merely age —
***it states the condition under which it stops being true, and that condition is a milestone that has
since closed.*** A sentence carrying its own expiry date is the cheapest possible correction and nobody
made it, because closing a milestone does not walk the comments that named it.

**The candidate check, and it is more tractable than Cause 7's**: ***a doc comment naming a future
milestone as the thing that will falsify it is a citation, and a citation to a closed milestone is
checkable.*** It needs no entity vocabulary — only a grep for a milestone number in `src/` and the
Phase 2 ledger's status for that number. 🔴 **Not proposed as work here**; recorded because it is the
first candidate in this document that a mechanical check could actually reach.

⚠ **Both findings are routed rather than fixed**, on
[`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md):
they were found while sizing a milestone, they live in code that milestone does not own yet, and the
task that will touch each is named above.

### `RuleInstanceTable.cs:92` promises a `Business` column at milestone 27, and milestone 27 did not add one

**Found 2026-08-24 by milestone 27 task 9**
([`0041`](0041-the-business-is-a-thing-the-city-contains.md)). The comment reads ***"A Business gets its
own column when a Business runs a Rule, which is milestone 27"***, and
[`plans/0041`](0041-the-business-is-a-thing-the-city-contains.md) **G10** built the task's plan on it —
*"task 9's real content is a third subject on the Rule Instance, and the build already says so."*

🔴 **The column was implemented and withdrawn the same day.** A `[[rule]] trade = "<name>"` armed on a
Business loaded and then crashed on the Tick it fired: `RuleEngine.Fire` resolves a Building from the
instance, not only `Band` does, and the Building-centricity runs through evidence, `on_fail`, the wake
targets and every local Bin lookup.
[`adr/0149`](../docs/adr/0149-a-business-is-a-population-a-policy-sweeps-and-a-readout-names-every-entity-it-reads-against.md)
records the attempt and takes the other route: a Business is a **population a Policy sweeps**, which
needs no Rule Instance at all.

**This is [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
in its ordinary form and Cause 4's shape** — the sentence is right about which symbol to read and wrong
about the **trigger**, and ⚠ **the trigger it is wrong about is a MILESTONE NUMBER**, which is the
candidate check the entry above this one proposes. *A doc comment naming a future milestone as the thing
that will falsify it is a citation*, and this is one that came due and was not paid.

- [ ] **The repair is the milestone number and not the sentence.** A Business will get its own column
      when a Business runs a **Bin Rule**, and that needs `RuleEngine`'s Building resolve unpicked
      first — milestone **26**, where a Business earns. ⚠ **Do not delete the sentence**: it is the only
      place in the build that names the column's condition, and it was right about everything except
      when.

⚠ **What it cost is worth recording, because it is what Cause 4 is for.** The comment did not mislead a
reader about behaviour; it sized a task. Task 9 was planned as *a column plus a fork* and is a **loop
plus a membership test**, and the half-day between those two estimates was spent building the wrong one.

### 🔴 Two doc comments say *nothing tenants a Business* four hundred lines above the method that does

**Found 2026-08-25, surveying the Zone Rule for
[`0043`](0043-session-w-the-provider-kinds-content.md) — session W's brief — and not while looking for a
defect.** Cause 4, and the **third sighting of one shape in two days**.

| Where | What it says | What is true |
|---|---|---|
| `src/Borough.Core/Entities/UnpremisedTable.cs:19-25` | *"IT SHIPS WITH ONE EXIT AND THAT EXIT IS THE SINK … nothing tenants a Business … `World.CreateBusiness` has no `src/` caller"* | `PlacementEngine.Tenant` (`:563-630`) premises pool members into any standing Building with room, calling `World.Premise` at `:626`. `CreateBusiness` has **two** production callers — `World.cs:1335` (the gate) and `World.cs:2856` (`Fit`) |
| `src/Borough.Core/Rules/PlacementEngine.cs:645-650`, in `Retire`'s remarks | *"nothing tenants a Business"* | ⚠ **The method that tenants one is in the same file, four hundred lines above it** |

**Both were true when written and both were falsified by milestone 27** — task 7's placement pass
([`adr/0147`](../docs/adr/0147-a-business-takes-premises-by-placement-and-one-ceiling-counts-both-kinds-of-tenant.md))
and task 8's founding channel. ***The mechanism moved and the sentences describing it did not.***

⚠ **It is the shape [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
predicts exactly**: both are **right about where to look and wrong about the trigger**. And it is
[`0041`](0041-the-business-is-a-thing-the-city-contains.md) **G1** a third time — a milestone's own risk
cell, then `plans/0003`'s copy of it, now two doc comments — ***every one of them a sentence about a
mechanism that outlived the mechanism.***

🔴 **What makes this one worse than a stale comment: `UnpremisedTable`'s says the pool has ONE EXIT AND
IT IS THE SINK**, and a reader sizing `adr/0006` off it would conclude the pool drains only by
emigration. It does not — **milestone 27 task 10 measured 7,165 premisings against ZERO give-ups over
131,072 Ticks** (`0041` **G44**), so ***in every world that exists the exit the comment denies is the
only one that has ever fired.*** **A comment that is wrong about which sink runs is a comment that
misdirects the next `adr/0006` audit.**

⚠ **Not fixed in the sitting that found it, and that is deliberate**: `plans/0043` is a brief and edits
no code, and these are doc comments on the mechanism session W is about to make decisions against.
**They go with milestone 26's first code task**, whoever takes it.

### 🔴 `CLAUDE.md`'s demand-scalar rule dropped the word that made it narrow, and a session read it as a ban

**Found 2026-08-25, in session W** ([`0043`](0043-session-w-the-provider-kinds-content.md) **W13**).
**Cause 5 on a RULE rather than on a number**, which is a new surface for it.

| Where | What it says |
|---|---|
| `CLAUDE.md` → *Things to be careful about* | *"**Don't add a demand scalar.** There is no RCI meter. The Unplaced Pool *is* the demand signal"* |
| [`docs/01-player-experience.md`](../docs/01-player-experience.md), the RCI discussion | *"`CLAUDE.md`'s rule is do not **add** a demand scalar — **this one is not added, it is counted**"* … *"An RCI meter is a **synthesised scalar with no constituents**. Nothing in SC4 **is** the RCI value; it is a formula's output drawn as a bar, and **it cannot be interrogated when it is wrong**"* |

**The compressed form is not wrong; it is unqualified.** What `01` refuses is a *synthesised* scalar
with no constituents, and the qualifier is what makes the rule narrow enough to be obeyed. ⚠ **The
clause that carried the qualifier stayed where it was, doing nothing** — ***which is Cause 5's exact
sentence, arriving on a design rule instead of on a figure.***

🔴 **What it cost, and it is why this is filed rather than shrugged at.** Session W's brief offered the
user four ways to raise a shop and labelled the fourth ***"NOT a demand slider — the design refuses
those"***, then recommended the impoverished option on the grounds that it was safe. **The user refused
that reading** — *rejecting the demand SLIDER does not mean we cannot model DEMAND at all* — and was
right on the corpus's own text. ⚠ **The session then spent an exchange establishing whether demand could
be modelled at all, while [`02 §5`](../docs/02-simulation-model.md) had specified the mechanism in its
own step 3 the whole time.** ***A rule quoted without its qualifier does not merely mislead; it
forecloses.***

**The repair is the two-half one, and both halves are owed.** **Reading**: quote `01`'s sentence rather
than `CLAUDE.md`'s compression. **Writing**: `CLAUDE.md`'s bullet should carry the qualifier — *a
synthesised scalar with no constituents* — because a rule stated as four words is a rule that will be
read as four words. ⚠ **Not edited in the sitting that found it**: `CLAUDE.md` is the file whose whole
discipline is that it stores no second copy, and adding a clause there is a judgement about that file
rather than a correction to it.

### ⚠ `ZoneRuleEngine.Create`'s summary line states an acceptance test the code does not perform

**Found the same day and in the same survey.** Cause 4 — a description wrong about the trigger.

The doc comment's `<summary>` reads ***"The create predicate: vacant AND permitted AND somebody in the
Pool would take it"***, and calls it ***"Three terms"***. The code is
`(Lots.Zone[lot] & definition.Admits) == 0 || _world.UnplacedPool.Count == 0` — **a zone-bit test and a
non-empty test**, with vacancy decided by the caller. ***There is no acceptance test and no per-record
question of any kind.***

⚠ **The same comment admits it three paragraphs later** — *"The Pool is read as non-empty and drained
blind. **There is no acceptance test**, because acceptance needs rent, a commute and a tolerance"* —
**so the document contradicts itself between its summary and its remarks**, and ***the summary is the
half that shows up in tooling.*** A reader sizing work off it would believe acceptance was built.

🔴 **This one has a fix and it is not a comment edit**:
[`adr/0163`](../docs/adr/0163-demand-for-a-shop-is-elapsed-unserved-need-in-reach-and-building-claims-it.md)
builds the acceptance test the summary describes, for trades, at milestone 26. **The summary becomes
true of the trade rule and stays false of the housing rule**, which is tier 0 by that ADR's own
consequences — so ***the correction is to say which rule it is describing***, and that is owed with 26's
first code task rather than now.

### 🔴 The Ruleset keys have never been asked *would a designer ever set this*, and the answer is not always yes

**Raised by the user on 2026-08-25, in session W, unprompted and against the session's own
proposal** — *"it seems like some keys we're defining for rulesets will only ever be applicable in the
demonstration / test rulesets… I am all but certain we've defined ones that are only ever useful for
demonstrations."* **The test is now
[`adr/0164`](../docs/adr/0164-a-ruleset-key-is-designer-facing-or-it-belongs-in-the-instrument.md)**;
***the sweep is this row and it has not been run.***

⚠ **It is a NEW Cause rather than an instance of one, and the shape is worth naming.** Every Cause in
this ledger so far is *a document says something wrongly*. This one is **a document offers a control
nobody would use** — the corpus is not incorrect anywhere, and a designer opening a Ruleset is still
misled, because ***every key in a file asserts that somebody should have an opinion about it.***

**What the sweep must do, and it is not a grep.** For each key in `rulesets/` and each key
`RulesetLoader` accepts, ask **would a designer ever set this** — *would*, in the course of making a
city behave differently, not *could*. `adr/0164` names three ways the answer is **no**: the real
mechanism is a **player verb** (`[roads] arterial_count = 0` is the worked example, and it is already
correct); the consumer is an **instrument** (`SyntheticCity`, which calls itself *"an instrument, not a
mechanism"*); or the value is **derived** and stating it is not choosing it.

🔴 **Only the first two are defects, and the third must not be swept up with them.** A derived value
written down so a designer can *find* it is serving the designer — `rulesets/minimal.toml`'s
`revisit_ticks = 2048` says ***"stating it is not choosing it"*** in its own comment, and that sentence
is the disclaimer the test asks for. ***So the remedy for a borderline case is the disclaiming comment
and not a deletion***, and a sweep that deletes keys will do more damage than the thing it is fixing.

⚠ **What a hit costs, stated so the sweep is not treated as tidying.** A key that fails the test has
usually acquired a [`0002`](0002-open-questions.md) **§D** row and a ratifier under
[`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md) —
***machinery for deciding whether a number describes the city correctly, pointed at a number that
describes no city at all.*** §D is the ledger `adr/0043` cites as evidence for what has been examined,
so scaffolding in it degrades the one record that is supposed to say what is settled.

**Session W's own near-miss is the worked example and it is the reason to believe there are others.**
The session proposed a `[lots]` key for the commercial share of a generated world and **got as far as
choosing its ratifier** before the question was asked — by the user, not by the session. The answer was
**no**, and for the strongest reason available: no designer touches `SyntheticCity` at all.
⚠ ***A session that had just written an ADR about not authoring content by hand was one exchange from
authoring a key nobody would ever set.***

**Not run here**, because a sweep is work rather than a decision, and because `adr/0164` was written the
same afternoon and nothing has been checked against it yet. **It wants a session or a milestone task of
its own**, and the first thing it should produce is a count: how many keys, how many hits, and how many
of the hits are the third row rather than the first two.

---

## Filed 2026-08-26, by milestone 26 task 4 — one finding, and the document that holds it holds **both halves of it**

### 🔴 `adr/0139` says the price sits on the market row and says it sits on the seller, four paragraphs apart

**Found by writing [`adr/0167`](../docs/adr/0167-a-purchase-picks-its-seller-by-a-draw-and-waits-on-the-market-rather-than-on-a-shop.md),
because *buy from whoever is cheapest* is the obvious seller rule and the first thing it needs is
somewhere to read a price from.** There are two answers in one record and they are not compatible.

| Where | What it says |
|---|---|
| [`0139`](../docs/adr/0139-a-district-pool-is-a-market-and-not-a-store-so-stock-stays-with-the-seller.md)'s **Decision**, line 4 | *"the `(District, Good)` row is the **market** — **the price it clears at**, the thing a blocked buyer waits on, and the set of sellers"* |
| its own **correction banner**, 2026-08-22 | *"the market row is **still the price** and the wake target"* — listed among the things the correction explicitly does **not** move |
| its **Consequences** | *"**The `Price` field moves from the market row to the seller**, so per-seller dispersion is expressible from the first day rather than retrofitted — which is the whole point of deciding it now."* |

**The build took the first reading**, and it was not a slip: `Space.DistrictPoolTable.Price` is keyed by
`(District, Good)` and has been since milestone 12 task 6, [`CONTEXT.md`](../CONTEXT.md) → District Pool
records that reading, and [`CLAUDE.md`](../CLAUDE.md)'s `twinned.toml` cell repeats it. ***Three
documents agree with the half of `0139` that `0139`'s own Consequences overrule.***

⚠ **This is not Cause 2 and it is worth being precise about why.** Cause 2 is *an ADR issues writes to
other documents and the writes do not all land* — a delivery failure, where the source is coherent and
the copies lag. Here **the source is incoherent**, so there was never one write to deliver. It is the
shape milestone 10's collection named — *the first defect a document committed against itself* — on its
second sighting, and the aggravating detail is that **the correction banner passed over it**: a sitting
went through this record clause by clause on 2026-08-22, enumerated what did and did not move, wrote
*"the market row is still the price"*, and did not notice that sixty lines further down the same record
said otherwise. ***A banner that lists what survives is a re-reading, and this one re-read the half it
was already holding.***

🔴 **What it cost is a design question that could not be asked.** With one price per `(District, Good)`
every seller in a District charges the same number, so *cheapest* is a comparison of one value with
itself and there is no discriminator for a purchase to choose on. `adr/0167` reached that by measuring
the build rather than by reading `0139`, and it says so in its own *Why* — but ***a sitting that had
read only the Consequences would have written a tie-break dressed as a price comparison***, which reads
as price competition in a world that has none. That is the failure `LEGIBLE CAUSE` exists to prevent,
reached through a document rather than through code.

⚠ **Neither half is obviously the one to keep, and this ledger must not pick.** The Consequences'
argument is real — per-seller dispersion *is* cheaper to express on the first day than to retrofit — and
the row's argument is real too, because a price with one home per District is the only shape the
tâtonnement in `MarketRuleset.Reprice` currently has a denominator for. ***A ledger entry that resolved
this would be settling a design question in the corpus audit***, which is what [`adr/0042`](../docs/adr/0042-a-planning-document-cites-and-a-design-document-owns.md)
forbids. **It is an ADR, and the natural home is `06` milestone 13, the price surface** — which is
already `0139`'s own revisit trigger and `0167`'s first one.

- [x] **`04 §4`'s *Local* bullet struck 2026-08-26.** It had copied the Consequences —
      *"⚠ **A price now sits on the SELLER**"* — and now states where the price actually is, names both
      halves, points here, and says what `adr/0167` chose because of it.
- [ ] 🔴 **`adr/0139` itself is untouched and that is deliberate**, because a superseded document gets a
      banner and never a deletion ([`PROCESS.md`](../PROCESS.md) → *Conventions*) and the banner has to
      say which half stands. **Owed at milestone 13**, or on the day anybody proposes reading a price
      per seller — whichever is first.
- [ ] Check whether any *other* document quoted the Consequences' half. `04 §4` was found by grep for
      the word *seller*; nothing systematic has been run. ⚠ **The mechanical check that would have
      caught this does not exist and may not be cheap**: it is *a record does not contradict itself*,
      which is not a link, a citation, a count or a registry figure — the four shapes
      `tests/Borough.Tests/Corpus/` can currently express.

---

## Filed 2026-08-26, by milestone 26 task 7 — a mechanical check with a silent exemption, and a quotation attributed to a plausible neighbour

### 🔴 `BoardShapeTests` rule 2 could not see the board's largest cell, because the row omitted a trailing `|`

**Fixed the day it was found; recorded because the shape is the point and the shape is new.**
`BoardShapeTests.Cells` split a row on `|` and iterated `1 .. parts.Length - 2`, which is correct for a
row written `| a | b |` — the last part is the empty string after the trailing pipe — and **drops a real
cell** from a row written `| a | b`. Markdown accepts both.

🔴 **Exactly one row on the board omitted it, it was row 1, and the cell it hid was 1,724 characters and
seven sentences against a ceiling of three.** The check had been reporting green while the cell it
exists for grew unread. ⚠ **It is not a coincidence that it was that row**: the row somebody appends to
every day is the row whose punctuation eventually goes wrong, so ***the cell a length check most needs
to see is the cell most likely to break the parser that feeds it.***

⚠ **This is not `Cause 5` and it is not `Cause 1`.** Nothing drifted and nothing was miscopied — a
check was **narrower than its own claim**, and the gap was invisible from the outside because the
failure mode of a too-narrow check is *silence*. The nearest sibling in this ledger is `adr/0021`
*called a rule **checkable** for four years, and nothing could have checked it*, and the difference is
that this one **half**-worked, which is worse: a check that never runs gets noticed, and a check that
runs on nine rows out of ten reports success.

**Repair, and it is two halves.** `Cells` now normalises the row — strip one leading and one trailing
pipe, then split — so the next omission costs nothing. And the synthetic board in
`The_three_rules_hold_on_a_synthetic_board` gained the row **without** the trailing pipe asserting the
**same** violation as the row with it, under `CLAUDE.md`'s rule that a diagnostic ships with a test that
writes the violation and watches it fire. ⚠ **The board itself was also brought under the ceiling**, but
that is the smaller half: ***fixing the row would have left the parser waiting for the next row.***

**A sibling nobody has looked for.** Every corpus check parses markdown by hand. This one assumed a
dialect; the others may too — `CitationTests` splits on nothing, but the table-rendering checks and
`0000a`'s index checks all read rows. ⚠ **No claim is made here that they are wrong** — this is a place
to look, which is [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
observed rather than a finding.

### ⚠ *"A cost paid to nobody is a leak, not a cost"* is the loader's message, and three documents credited `adr/0024`

**Corrected on the day, in all three, and recorded because the corpus's own checks cannot reach it.**
The sentence is `RulesetLoader`'s **refusal 4**, quoted by
[`adr/0117`](../docs/adr/0117-upkeep-leaves-milestone-10-and-its-blocker-is-a-rule-with-no-actor.md).
`adr/0024` argues that money is conserved and **never puts it that way**. Milestone 26 task 7 attributed
it to `0024` in the new `adr/0169`, in `rulesets/provisioned.toml`'s header and in
`ProvisionedRulesetTests`' doc comment — three places, one act — and it was found only by grepping for
the phrase after two *unrelated* link targets failed to resolve.

🔴 **This is `Cause 5` with the digits replaced by a sentence.** A quotation needed a source, a
plausible neighbour was to hand, and the attribution was supplied rather than checked — the caveat
travelled and the provenance did not. ⚠ **The consequence is worse than a bare number**: the wrong
document now reads as having said something load-bearing, so anyone who follows the citation to check
the claim finds a record that does not contain it and has no way to tell whether the claim or the
citation is the error.

⚠ **`CitationTests` cannot catch this and is not failing.** It asserts that a link **resolves**, never
that a quotation is **where it says it is** — a gap that is real and probably not worth closing
mechanically, since the check would have to distinguish quotation from paraphrase. ***The reading rule
already covers it and was simply not followed***: quote the sentence, and name where the sentence is.

## Filed 2026-08-26, by milestone 17's guard repair and milestone 26 task 7's correction — a **Cause 8 candidate** with four sightings in one day, and a duplicated ordinal in this document

### 🔴 A counter that aggregates over the whole world, read as though it were scoped to the subject the claim names

**The framing is milestone 26's, arrived at from its side; the fourth sighting is milestone 17's, arrived
at from the other.** Two sessions hit it independently on 2026-08-26 and it is not
[**Cause 5**](#cause-5--a-number-is-quoted-away-from-the-sentence-that-qualifies-it), though it is its
sibling. Cause 5 is a number travelling away from the clause that qualifies it. **This is a number whose
*scope* is wider than the claim it is being used for, sitting in the file that makes the claim, with its
qualifying clause still attached and still correct.** ***Nothing has moved and nothing has been
compressed; the number simply answers a broader question than the one being asked.***

| # | Sighting | The counter | The claim it was read as |
|---|---|---|---|
| **1** | `ProvisionedRulesetTests.A_shop_can_go_broke` | `Zoning.Drain().Ended.Sum` — **every** tenancy ended anywhere in the city | *a broke shop was turned out*. It was satisfied by **dwellings evicting Households** on a 32-Tick clock in the window before the market could exist, and a Business tenancy **cannot end at all** — `ZoneRuleEngine.Condemn` walks `World.Occupants` and never `World.BuildingBusinesses`. [`plans/0002`](0002-open-questions.md) §A |
| **2** | milestone 26 task 7's **F37**, first half | a shop too new to have earned anything | *the levy is failing because the shop is broke* |
| **3** | milestone 26 task 7's **F37**, second half | the same, a second time | the same |
| **4** | `PlacementLongRunTests.The_hundred_thousand_Tick_occupancy_run` | `capacity` summed over **every live Building** | *places somebody could move into*. [`adr/0091`](../docs/adr/0091-clearing-land-is-bought-rather-than-taken-and-demolish-is-the-sixth-verb.md) leaves a condemned Building **standing**, so `Rows.IsLive` stopped meaning *habitable* and the denominator became a statement about blight |

⚠ **CARE INSIDE THE TEST BODY CANNOT FIX IT, which is what makes it a Cause rather than four bugs.** The
fault is in the *reading of what the counter counts*, and the counter is usually named correctly —
`Ended` really is the count of endings, `capacity` really is the declared capacity. ***Every one of these
four passed review, and three of them passed a green test.***

⚠ **The tell is a title with a subject in it.** *A shop can go broke* names a shop; *the occupancy run*
names places somebody lives in. **When the assertion's title names a subject and its counter names a
world, the two have to be reconciled explicitly or the test is measuring the world.**

**The repair, in both directions.** ***Writing***: assert on the subject rather than on the aggregate —
milestone 26's correction reads `tick - StarvedSince` on the broke shop's **own** Rule Instance, which is
a property of the subject and cannot be satisfied by anything else in the city. ***Reading***: before
trusting a counter, ask what else could move it. ⚠ **It is not a mechanical check.** A test that names
its subject and folds a world-wide sum is indistinguishable, to a linter, from one that means to.

🔴 ⚠ **A CORRECTION HERE REMOVES A SYMPTOM AND IS NOT A FIX, and sighting 1 is now the example.** The
assertion was corrected the same day and the mechanism gap under it is untouched — a Business's Failure
Pressure still reaches no threshold. ***What was a red test is now a silent one***, which is why
`plans/0002` §A stays open with the correction recorded in it rather than closing on it.

### ⚠ And this document has two sections numbered **Cause 7**, one of which is titled *two documents claim one ordinal*

`### Cause 7 — two documents claim one ordinal` and `### Cause 7 — a description takes its noun from the
build, and the build is behind the design` are both present, at different depths of the file. **They are
two different causes wearing one number**, which is the first one's own subject arriving inside the
document that names it.

⚠ **Not renumbered here, because a Cause number is cited from elsewhere** and a renumber is a sweep
rather than an edit — the same hazard `adr/0119`/`0120`'s renumber recorded. Recorded so the next reader
of *Cause 7* knows to check which. ***It is also why the section above is filed as a Cause 8
CANDIDATE and not minted as Cause 8***: allocating an ordinal in a document with a live ordinal
collision is how the collision doubles.

---

## Filed 2026-08-26, by milestone 17's golden-session repair — two **Cause 9 candidates**, both about a check that keeps passing after its subject moves

⚠ **Filed as candidates for the reason the section above gives**: this document has a live ordinal
collision at *Cause 7*, and minting numbers into that is how a collision doubles.

**Both shapes below are about a test that goes on passing, cheerfully, once the thing it was written to
watch has moved out from under it.** Neither is a wrong sentence in a document, which is why neither
would ever have reached this ledger by the route the other Causes did. They are here because the audit's
subject is *how a record stops being true without anybody noticing*, and a green test is a record.

### Candidate A — a guard written against a fixture pointer stops guarding whatever the pointer moved off

**Sighting 1 — `The_two_golden_rulesets_differ_in_exactly_one_line`, 2026-08-26.** It read
`GoldenFixtures.RulesetPath` and `TunedRulesetPath`, which is correct and is what made it dangerous.
Milestone 17 repointed those at `declining.toml` / `declining-tuned.toml`; the test **followed
automatically and passed**, and `minimal-tuned.toml` — still loaded by `BinWaitListTests` and
`TreasuryFromAFileTests` — was left a hand-maintained copy of `minimal.toml` with **nothing** watching it
drift. Caught during the repair rather than after it, and only because somebody asked what the pointer
had been pointing at. *Repaired by asserting both pairs.*

**Sighting 2 — `LotLongRunTests.The_hundred_thousand_Tick_lot_run`, 2026-08-26, and it is the same shape
with the pointer on the OTHER side.** The test's subject is the **subdivider**, and it built its world
with `GoldenFixtures.Rules()` — which means *whatever the committed baseline opens on*, not *a city that
stands still*. Repointing the baseline at `declining.toml` gave a test about Lots a city in which
Buildings fall down: `adr/0091` leaves a condemned Building standing as a **shell** and `adr/0079` has a
Building outlive its frontage, so the share of Segment faces occupied by something nobody lives in
climbs through the run and a bulldoze frees nothing on them. ✅ **Its vacuity guard caught it and named
the cause correctly** — *carved Lots on only 15 of 97 edits … the second is a statement about the Zone
Rule rather than about the subdivider* — which is a guard written in 2026-08-13 diagnosing a cause that
did not exist yet. *Repaired by giving the test its own named Ruleset, `GoldenFixtures.StaticRules()`.*

⚠ **The tempting reading is that the test should have used a literal, and that is wrong** — a literal is
`plans/0012` **Cause 1** and drifts the other way. ***The property that matters is coverage of a set,
and a pointer names a member.*** A guard over "the two golden Rulesets" is a guard over a set that
changed size when nobody said so.

### Candidate B — a sample rate derived from the run's length cannot see a phenomenon whose period comes from the Day

**Sighting 1 — `The_session_sends_people_to_work_without_a_trip_command`, 2026-08-13.** It read the final
Tick alone. `adr/0094` took the Day to 2,048 Ticks, the departure window fell 2,731 → **683**, and the
session finished a whole Day's commuting with time to spare — so the one instant it read found the quiet
*after* the wave. **Coverage went up and the assertion measuring it went to zero.** Repaired by sampling
eight times across the run.

**Sighting 2 — the same test, 2026-08-26, and the repair is what recurred.** Eight samples across the run
is a *stride equal to one eighth of the session*. Milestone 17 took the session 2,048 → 8,192 Ticks, so
the stride went 256 → **1,024**, and every one of the eight looks landed past 683 and short of the next
Day's window. ***The city was commuting exactly as much as before.*** The fix had inherited the defect
one level along, because it was still denominated in the run rather than in the thing being looked for.

**The shape, which is what to carry**: ***a sampling interval is a bound and not a count.*** Anything
periodic in the Day — departures, a Map Layer cadence, a revisit period, a decline threshold — has a
window that does not move when a fixture's length does, so a sample rate stated as *N looks* aliases the
moment somebody lengthens the run for an unrelated reason. State the interval against the window.
*Repaired by a `Stride` of 256 Ticks, stated against the 683-Tick window and documented against it.*

⚠ **Neither candidate is a mechanical check and both are recorded anyway.** A linter cannot tell a
sampling stride that means to be a fraction of the run from one that has aliased, and cannot tell a
pointer-following guard that still covers its set from one that no longer does. What is available is a
question for whoever moves a fixture: ***what was this pointing at, and what is watching that now?***

---

## Filed 2026-08-26, by `adr/0170` — one document still authoring a unit that moved

**`docs/02-simulation-model.md` §5.9 still says the decline threshold is authored in *missed firings*,
and it has not been since [`adr/0168`](../docs/adr/0168-a-decline-threshold-is-a-duration-and-the-premises-and-the-tenant-get-one-each.md).**
Two sentences carry it: *"The threshold is therefore authored in **missed firings** rather than Ticks,
so that a Ruleset which retunes every `rate` cannot silently retune every Building's lifespan"*, and the
`adr/0141` amendment below it, which lists *same `missed firings`* among the things the split left
untouched.

⚠ **The paragraph's REASONING is not wrong and that is what makes this worth filing rather than
striking.** `adr/0053`'s argument — that a tally inverts severity, because a comprehensively starved
Building emits one failure event and an intermittently supplied one emits many — survives `adr/0168`
completely; the Rule still counts firings internally. What moved is **what the designer writes in the
file**, and `adr/0168`'s whole finding is that those two had come apart without anybody noticing.
***So a reader who obeys this section authors a key the loader now refuses by name.***

**Owed to `02 §5.9`**: an amendment saying the unit is `condemn_after_days` and
`tenancy_ends_after_days`, in Days, converted at the parse site, with `adr/0053`'s severity argument
kept and re-homed as the reason the *derived* count is what the engine compares.

⚠ **It is a sighting of `Cause 4`** — a decision taken from a description of the code, wrong about the
**trigger** — arriving in the design document rather than in a doc comment. It was found by writing
`adr/0170` against the same section, which is the only reason anybody looked.
