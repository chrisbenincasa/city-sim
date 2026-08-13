# A description of the build is where to look, and never what you found

**Before deciding anything on the strength of what the build does, read the build.** A sentence describing a mechanism — in an ADR, in a plan, in a doc-comment, or implied by what a test suite covers — tells you *where to look*. It never tells you what is there. And where such a sentence is wrong it is almost always wrong in one specific way: about the **trigger** — what fires the mechanism, what it is keyed on, when it runs. The obligation this creates is small and mechanical: **find the call site and name it**, and when writing a description of code, state a **name** rather than a **time**, because a name is greppable and a time is not.

Guiding concepts: `SOLVE THE ACTUAL PROBLEM`, `LEGIBLE CAUSE`.

**Arguable** under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md), and the four sightings below are evidence rather than a measurement: nobody has counted how often a description of this build is wrong, and the number would not change the rule, because the cost of checking is one grep and the cost of not checking has been a wrong decision every time.

## Why

### It is the fourth thing a sitting reasons from, and it was the one with no rule

Three ADRs govern what a sitting may conclude, and each names a different input: [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md) governs **claims**, [`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md) governs **numbers**, and [`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) governs **absences**. The fourth input is *what the build does*, and every sitting uses it constantly — *the loader already refuses this*, *that pressure source would cover it*, *the generator lays this at world creation*. Nothing has ever governed it.

`adr/0073` sits beside these rather than among them: it governs what a spike must **do** with a finding, not what a sitting may **conclude**. This one is in the family.

### Four sightings, three consecutive days, and each cost a decision

| | The description consulted | What it said | What the code said |
|---|---|---|---|
| [`0064`](0064-a-bins-capacity-is-a-property-of-the-ruleset-in-force-and-an-over-full-bin-drains-rather-than-clamps.md) | **the test suite** | `RulesetLoader` refuses nothing for a duplicate `(kind, Resource)` Bin declaration | it has refused since slice 7 task 8 — and that refusal was the **one guard in the loader with no test** |
| [`0079`](0079-a-building-outlives-its-frontage-and-an-address-that-has-none-is-a-hole-the-trip-model-reports.md) | **an ADR's summary sentence** | route a stranded Building through `adr/0053`'s failure pressure, *"it needs no new mechanism"* | `adr/0053`'s pressure is a duration of **Rule Instance starvation**, and a bulldozed Street starves nothing |
| [`0091`](0091-clearing-land-is-bought-rather-than-taken-and-demolish-is-the-sixth-verb.md) | **a plan's own recommendation** | *"re-zone and wait — the Zone Rule condemns in its own time"* | `ZoneRuleEngine.Condemn` never reads the permission set, so a healthy Building never falls however the paint changes |
| [`0090`](0090-the-generator-makes-land-and-the-player-makes-every-road.md) | **a doc-comment, inside the code** | `RoadGenerator` *"lays a complete Street lattice over the whole map at world creation"* | one production call site, `SyntheticCity`, reached only by `CommandKind.Populate` — a player's world has had no roads since 5a |

**Every one of the four is about the trigger, and none is about the behaviour.** The loader's refusal, the pressure's key, the condemn predicate's terms, the generator's caller. In each case the description named the right subject and described what the mechanism *does*; the decision turned on what makes it *happen*, and that is the half a description reliably omits — because a sentence written to explain a mechanism explains its purpose, and its purpose is not its trigger.

**The fourth was caught by the practice this ADR states, while the practice was being written**, which is the best evidence available that the cost is one grep. `plans/0025`'s recommendation had stood as the pinned answer to an open question; opening `ZoneRuleEngine.Condemn` took a minute and refuted half of it.

### It is not Cause 1, 2 or 3, and it is the one no re-reading finds

`plans/0012` has three causes. Cause 1 is **one fact stored twice, drifting** — the tell is that the copies disagree. Cause 2 is **a write that did not land** — the tell is a decision with no inbound citation. Cause 3 is **a read never repeated** — the text was true when written and the world moved.

This is none of them, and the difference is not academic: **the text was never true.** There is no second copy to disagree with, no missing citation to notice, and no gate to re-check. The disagreement is between a document and **the code**, and it was there on the day the sentence was written.

**The corpus is structurally unable to catch it.** Its three mechanical checks — `CitationTests`, `CoverageMapTests`, `MarkdownStyleTests` — check that links resolve, that rows exist, and that markdown renders. **All three are document-to-document or document-to-directory.** Nothing anywhere checks that a sentence about `RoadGenerator` is true of `RoadGenerator`, and no such check is proposed here, because a general one would be a natural-language proof obligation. What is available instead is a discipline with a checkable output, which is the next section.

### The repair is to write a name where a time is written

Three of the four descriptions above would have been *self-refuting* if they had named a caller.

> *"lays a complete Street lattice over the whole map **at world creation**"*

is unfalsifiable by reading; it states a moment, and moments are not in the code. Written as **"lays a lattice when `SyntheticCity` runs"** the same sentence carries its own check — one grep for `SyntheticCity`, and the reader learns in seconds that it is `CommandKind.Populate`'s and therefore no player's. The rewrite costs nothing and is strictly more informative.

So the rule has two halves and the second is the one that compounds:

- **Reading**: a description tells you which symbol to open. Open it.
- **Writing**: a description of code names a **symbol** — a caller, a predicate, a column — and not a **time**, a **phase**, or a **stage of the project**. *Since slice 7 task 8*, *at world creation*, *on the Epoch* are all sentences a reader cannot check without already knowing the answer.

**This is `adr/0059`'s move in a different medium**: state the thing that is checkable and derive the rest. There, a Ruleset states a duration and the engine derives the sample; here, a description states a name and the reader derives the timing.

### The two asides this makes into a rule

The corpus has coined this twice and left it as commentary both times. [`0044`](0044-the-map-layer-diffusion-cadence-is-the-designers-number-not-the-profilers.md): ***citing an ADR is not applying it***. `adr/0079`: ***citing a mechanism is not checking what it is keyed on***, offered explicitly as `adr/0044`'s sibling. Two ADRs independently reached for the same sentence and neither made it binding, and the failure recurred twice more afterwards. **An aside is not a rule, and the evidence that an aside is not a rule is that this one was written down twice and did not hold.**

## Consequences

**`plans/0012` gains Cause 4** with these four sightings and the repair, so a reader meeting that file's diagnosis meets all four ways this corpus fails rather than three.

**A decision that turns on what the build does cites the symbol it read**, not the document that describes it. `adr/0079` and `adr/0091` already do this — `ZoneRuleEngine.Condemn`, `RulesetLoader`, `StreetGrid`, `RoadGenerator` — and the citation is what makes the next reader's check cheap rather than a repeat of the whole investigation.

**A doc-comment stating when a mechanism runs is a defect of `adr/0073`'s class** and is routed to the code that owns it on the day. `CellGrid.cs`'s is the first, filed into `plans/0003`'s hash-moving queue item 6.

**This does not oblige reading the build before every decision.** Most decisions in this corpus are about what the city *should* do and touch no mechanism. The rule binds exactly when a premise takes the form *the build already does X* or *the build does not do X* — and the second form is where it meets `adr/0070`, which classifies absences: an absence claimed from a description is not yet an absence at all.

**No mechanical check is added**, and that is stated rather than left as an omission. The one that would help is narrower than the rule and worth building when it is cheap: a lint over doc-comments containing *at world creation* and its family would have caught the fourth sighting and none of the other three.

## What would trigger revisiting

**A fifth sighting after this is written.** If the practice is stated, cited, and the failure recurs anyway, then a discipline is the wrong instrument and the answer is the narrow lint above — or a convention that makes the check unnecessary, such as forbidding prose about a mechanism's timing anywhere except at its call site. **The corpus has now demonstrated twice that an aside does not hold; this ADR is the test of whether a rule does.**

**A description turning out to be wrong about something other than the trigger.** The claim that it is *almost always* the trigger rests on four cases. A sighting where a description got the trigger right and the behaviour wrong would mean the repair — name the call site — does not cover the failure mode, and the rule would want restating around whatever the new case has in common with these.

**The corpus acquiring a real document-to-code check.** If one is ever built and holds, this rule becomes advice rather than an obligation, and should be demoted rather than kept out of habit.
