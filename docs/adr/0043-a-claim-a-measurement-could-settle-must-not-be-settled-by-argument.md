# A claim a measurement could settle must not be settled by argument

**Every claim a grilling session touches is classified as *arguable* or *measurable*. A session may settle the arguable ones. A measurable one it must leave open and route to a named spike — naming the number that would refute the claim and the machine that would produce it — and until that number exists, no document may cite the claim as settled.** The test for the split is a single question: *can you name the number that would refute this, and the machine that would produce it?* If you can, the claim is measurable and argument is the wrong instrument. If you cannot, it is arguable and no spike will help.

**Extended by S2 R8: name the *shape* you expect, not only the number.** A refuting number is not enough, because a number read off the wrong statistic is not evidence. R8 produced **three instances in one task** — a maximum over 33,018 volume indices, an unconditioned p99, and a monotonicity test — each chosen before anyone knew the distribution it would summarise, and each surviving into a published wire because nothing in the process asked. **A wire must be re-derived once the first measurement shows what the response looks like, and the re-derivation stated and scored separately rather than swapped in.** See [`spike-results`](../spike-results.md) → *S2 R8*.

`SOLVE THE ACTUAL PROBLEM` `HONEST DEGRADATION` `FAST ITERATION`

## Why

**Because five claims in the corpus have now been measured false, and they share a shape.** S2's first four tasks were the project's first sustained contact between the design documents and a machine. Everything they falsified was a claim about *what a structure does once it runs*:

| The claim | Where it lived | What [`0002`](../../plans/0002-open-questions.md) says about that document | What measured it | Result |
|---|---|---|---|---|
| *"the Road Graph arrives pre-partitioned, because the Chunk grid is already the pathfinding cluster"* | [`adr/0014`](0014-grid-streets-with-freeform-arterials.md) | 🟢 **argued**, sessions one and two | S2 R3 | Wrong by **256× in area**. At one Chunk the abstract graph *is* the Road Graph |
| Settlements are identified by union-find over commute range | [`adr/0020`](0020-one-live-world-and-settlements-are-derived.md) | 🟢 **argued**, sessions one and two | S2 R1 | Returns **6** Settlements where Tarjan returns 8 |
| The travel-time matrix rebuilds by dirty region | [`02 §6`](../02-simulation-model.md) | 🟡, and §6 appears on neither the worked list nor the never-grilled one | S2 R1 | Misses **72%** of the entries an edit changes. Unsound |
| The aggregate attribution scheme has a *lag* | [`03 §3.3`](../03-agent-architecture.md) | 🟡, and §3.3 is named nowhere | S2 R2 | Not a lag. **0.00%** deposited on a Segment carrying 108% — the wrong place, not late |
| The path source has *"no correctness content"* | [`adr/0041`](0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md) | Written from research during S2's own planning | S2 R2 | Wrong on two counts |

**Two of the five sat inside documents the ledger records as argued, and that is the finding.** The tempting reading of this record — *grilling produces confident claims that measurement destroys, so grill less and measure more* — does not survive the first column, and neither does its opposite. `adr/0014` and `adr/0020` were both worked in sessions one and two. **The sessions happened and the claims still went through.**

**They went through because a session argues a decision, not every sentence supporting it.** `adr/0014` decides that Streets are a grid and Arterials are freeform. Nothing in that decision depends on the pathfinding aside, which is why nobody stopped on it — and why [`adr/0040`](0040-the-pathfinding-cluster-is-a-multiple-of-the-chunk-not-the-chunk.md) could later correct its *identity* half on structural grounds while leaving the underlying claim unmeasured. The ledger records this as `adr/0010`–`0022` 🟢 *"sessions one and two"*: **thirteen ADRs, one green mark, two sittings.** A supporting sentence inside a document marked argued is indistinguishable from a decision that was argued, and it is cited the same way.

**And the two rows in the middle show the same hole from the other side.** `02 §6` and `03 §3.3` are in documents marked 🟡, and the ledger's per-section notes name neither — not as worked, not as owed. **A section nobody has claimed either way reads as covered by default**, because the document around it is being maintained.

**What none of the five ever received is a *type*.** No session, held or unheld, asked whether the claim was the sort of thing argument could settle at all. That question is cheap, it is answerable in one sentence, and it would have caught four of these five before any code existed. It would not have caught `adr/0020` — see below — and nothing would have.

**The exception is the instructive one, because argument had no purchase on it whatsoever.** Whether union-find or Tarjan identifies the connected components of a commute graph is not a question a session was careless about. Both descriptions are correct sentences and the disagreement is invisible in prose; only running them apart reveals that one merges what the other separates. **Some claims cannot be argued even in principle, and a rule that surfaces them is worth more than a rule that tries to argue them harder.**

**The corpus is reliable exactly where it reasons about what we want, and unreliable where it reasons about what a structure does.** The counter-example proves the boundary rather than weakening it: [`02 §5.8`](../02-simulation-model.md)'s *never resolve a route inside the choice loop* was a claim about **cost**, it was measured, and it held — 1.14 ns at the working District count against a tripwire set at 13.66 ns. Cost claims are arithmetic and survive argument. Behaviour claims are not, and do not.

**The damage compounds through citation, which is why the rule has to bite at the moment of writing.** `adr/0014`'s pre-partition sentence never carried a decision of its own — and [`plans/0010`](../../plans/0010-s2-routing.md) cited it anyway, as a reason HPA\* held the standing, so R3 was scheduled to *confirm* a hierarchy whose foundational claim was wrong by two orders of magnitude in area. **An unmarked measurable claim is three documents deep before anyone thinks to check it**, and by then it is load-bearing. The green mark accelerates this rather than restraining it: a citation into an argued document looks like a citation into a decision.

**The rule makes the failure structurally unavailable rather than merely discouraged**, which is the move this project has made twice before and both times for the same reason. [`adr/0003`](0003-deterministic-integer-simulation.md) does not say *remember to hash every field*; declaring the field is what allocates it, so the coverage hole cannot exist. [`adr/0042`](0042-a-planning-document-cites-and-a-design-document-owns.md) does not say *keep the roadmap current*; a planning row asserts nothing that can be false. **Discipline that depends on remembering fails at the eighth session** — which is precisely when `06-roadmap.md` was found carrying eleven of them.

**And it is nearly free.** Classifying a claim costs a sentence. Discovering it was misclassified cost S2 four tasks and very nearly published a verdict built on `adr/0014`.

## What this does not bind

**A design document may estimate, and should.** An order-of-magnitude sanity check is how you find out a design is absurd before writing it down properly. What the rule forbids is an estimate wearing a decision's clothes — so an estimate is labelled as one, and nothing downstream may cite it as settled.

**A design may proceed on a stated assumption.** Nothing here requires a spike to run before a document can be written; the whole corpus would stop. It requires that the assumption be named as an assumption, with the number that would refute it, so that the spike which eventually runs knows what it is testing and the documents that cite it know what they are standing on.

**It does not extend to questions of intent, because it cannot.** *Should Fidelity be a property of place rather than person?* has no refuting number and no machine. Neither does *should the player govern by policy or by placement?* These are the majority of what remains to be argued, and no amount of measurement will touch them. **This ADR narrows what a grilling session may settle; it does not reduce how many sessions are needed.**

**It is not a claim that measurement is more trustworthy than argument.** It is a claim that they answer different questions, and that the corpus has been using one on the other's territory.

## Consequences

- **A grilling session gains a third output.** It currently produces *settled* (an ADR) or *open* (a ledger entry in [`0002`](../../plans/0002-open-questions.md)). *Open* now splits: **open-and-arguable**, which a later session can close, and **open-and-measurable**, which only a spike can close and which must name its number and its machine to count as routed.
- **A measurable claim with no named number is not routed, it is merely deferred**, and stays on the arguable list as unfinished work. This is the guard against the obvious abuse — a session that classifies every hard question as measurable and goes home.
- **The audit cannot be scoped to the ungrilled documents, and this ADR's own first draft got that wrong.** It asserted that four of the five falsified claims had never been grilled; the ledger says two of them sat in 🟢 rows. **The audit is over every document, argued or not**, because what is being looked for is a claim that was never typed — and a green mark is not evidence it was.
- **`0002`'s blanket rows are a defect in their own right.** `adr/0010`–`0022` 🟢 *"sessions one and two"* covers thirteen ADRs, two of which have since been measured false. A status mark whose granularity is coarser than the claims it covers cannot be checked against anything. Rows of that shape should be split as each is revisited.
- **The known suspects are named by the board already.** [`adr/0016`](0016-the-lane-is-the-entity-not-the-car.md) is *written from research* and carries the order-of-magnitude claim the entire Microscopic tier rests on. [`adr/0009`](0009-parking-is-modelled-supply-never-search.md) and [`adr/0008`](0008-walking-is-a-simulated-leg.md) are the same. Each is a measurable claim currently sitting in a document that reads as decided.
- **Spikes get named earlier and are cheaper for it.** S2 discovered at task 3 of 7 that no cluster size fits the Tick budget — a question nobody had written down, found by accident, which promoted a task scheduled as a tidy-up into a load-bearing one. A spike commissioned against a named number does not have to discover its own subject.
- **Sessions get slower and will feel like they decided less.** Accepted knowingly, on the same judgement `adr/0042` made: a corpus that looks decided in five places it has not measured is worth less than a thinner one that says which five.
- **`0002`'s ledger becomes the register of owed measurements**, not only of open arguments, and the board's *Owed* section reads from it. S2's owed findings are already in this form and are the model.

## What would trigger revisiting

- **If the owed-measurement list grows faster than spikes retire it.** That would mean the classification has become a way of not deciding, which is worse than the confident prose it replaced — an open question at least reads as open, while a routed one reads as handled.
- **If a claim correctly classified as arguable is later falsified by measurement.** The dichotomy would be false, and the correct response would be a third class rather than abandoning the rule. One instance is not enough; a pattern is.
- **If sessions find most claims are mixed rather than cleanly one or the other** — a claim whose intent half is arguable and whose behaviour half is not. If splitting them turns out to be the hard part, the rule is describing the work rather than guiding it.
- **If the audit of the existing ADRs finds few measurable claims**, so that `adr/0014` was an outlier rather than a type. The rule would then be solving a problem the corpus does not have, at a real cost in session throughput.
