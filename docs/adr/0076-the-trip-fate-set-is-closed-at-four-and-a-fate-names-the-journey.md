# The Trip Fate set is closed at four and a Fate names the journey

**A Trip Fate is *completed*, *no route found*, *exceeded commute budget* or *stranded*, and there will not be a fifth.** The rule that closes the set has two clauses: **a Fate names how the *journey* ended**, so anything that fails at the far end is another object's outcome and the Trip completed; and **anything that arrives as *time* is scored by the Commute Budget**, which is not a Fate. *Stranded* means the network changed mid-journey and is a Trip outcome; the lost-driver condition that has been wearing the same word is renamed — **a Rejoin is abandoned**.

Guiding concepts: `LEGIBLE CAUSE`, `HONEST DEGRADATION`, `NO VERDICT`, `BOUNDED KNOWLEDGE`.

**Arguable** under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md). No measurement decides how many names an enumeration has; what it decides is whether the resulting reports are diagnosable, and that is a judgement a sitting makes.

## Why

**The corpus has refused a fifth Fate three times, on the same ground, without ever writing the ground down.**

| Refusal | The reason given |
|---|---|
| [`0067`](0067-a-shopping-attempt-is-a-trip-and-a-household-tries-one-provider-per-occasion.md) — no *purchase failed* Fate | *"the Trip **completed**; what failed is the **purchase**"* |
| `CONTEXT.md` → Parking Shed — no *no parking* Fate | scarcity arrives as the walk Leg **growing**, and the Budget scores it |
| `CONTEXT.md` → Diversion — no Fate for a lost driver | *"the cost arrives as **minutes**, which the **Commute Budget** already scores"* |

Three authors reached independently for two arguments, and the arguments are the clauses above. **Three refusals is a pattern, and an unwritten pattern gets re-argued** — which is what happened, three times. Writing it down converts a habit into a rule that a proposal can be tested against, and closing the set is what makes the rule load-bearing rather than advisory: it is the openness that let the same case be made repeatedly.

**Each clause has a home already, which is why nothing is lost by refusing.** A far-end failure is *some other object's* recorded outcome — `adr/0067` puts a failed shopping occasion on the **Household**, as a transaction result with a cursor that advances. A cost in time is the **Commute Budget**'s business, and the Budget already fails a Trip that exceeds it, which is the fourth-and-a-half Fate people keep reaching for without noticing it exists. **So a fifth Fate is always a proposal to record something twice**, in a place less diagnosable than where it already is.

**The rule earns its keep immediately, on a case the corpus has never raised.** `rulesets/minimal.toml` condemns every dwelling 64 Ticks after it is raised — deliberately, and its header says so — so under milestone 5b **a destination being demolished mid-Trip is the common case, not a corner**, and no Fate covers it. Under the rule it needs none, and the split falls out of what an `Address` is:

- the **Segment** is gone → ***stranded***. The network changed mid-journey; that is the Fate's definition.
- the **Building** is gone and the Segment is fine → ***completed***, and the purpose fails at the far end. Exactly `adr/0067`'s shape.

The second works because an [`Address`](0074-side-of-street-is-a-property-of-the-access-point-not-of-the-graph.md) is `(Segment, offset, side)` — **an Address outlives its Building**, so a Traveller can genuinely arrive at a plot of rubble. That is not a technicality dodging the question; it is the honest description of what happens, and it is more diagnosable than a *destination gone* Fate would be, because the Household's own record is where a player looks to find out why nobody is shopping there any more.

**And the word *stranded* is doing two jobs in one file.** `CONTEXT.md` → Trip Fate: *"stranded (the network changed mid-journey)"* — an outcome. `CONTEXT.md` → Diversion: a Traveller *"is **stranded** when no arc out of the node it stands on reduces the straight-line distance to its Target"* — and the **same paragraph** says that is **not** a Fate: *"there is no Trip Fate for a lost driver"*, the Traveller drops the Target, re-aims at its destination and carries on. One word, two conditions, opposite consequences, in the file whose first rule is one meaning per term.

The Fate keeps the word, because a Traveller whose road was bulldozed is stranded in the ordinary English sense and because it is the value three documents already cite. The Rejoin condition is restated as **a Rejoin is abandoned**, which needs no new noun and says what happens rather than naming a state that then invites a Fate.

## Consequences

**`CONTEXT.md` → Trip Fate carries the rule and the closure**, so the next author proposing a fifth reads the argument before making it rather than after.

**`CONTEXT.md` → Diversion loses the word *stranded*** and says the Rejoin is abandoned. `adr/0061`'s mechanism is untouched — a Rejoin is still never a search, and the cost still arrives as minutes.

**Milestone 5b writes the enum with four values and an `O(1)` write-site assertion** that no Trip ends without one, per `02`'s *no Trip without a Fate*. Four values fit two bits, which is not why the set is four.

**A demolition mid-Trip is a live path in milestone 5b, and it must be tested rather than reasoned about.** The shipped Ruleset produces it constantly, so 5b's suite asserts both branches: a Trip whose destination Building was condemned **completes**, and a Trip whose Segment was removed is **stranded**. This is the standing warning from slice 10 task 11 applied ahead of time — *a baseline records what a run did*, so a branch the run stops reaching disappears silently.

**Nothing here requires the demolition to notify the Trip.** Both branches are discovered on arrival or on traversal, which is `BOUNDED KNOWLEDGE` behaving correctly: a Household does not learn its shop was flattened until somebody walks there.

**The Trip Fate is not a verdict.** `NO VERDICT` is why *exceeded commute budget* is a Fate and *this was a bad Trip* is not: the set records what happened to the journey, and every judgement about it belongs to the mechanism that reads the Fate.

## What would trigger revisiting

**A journey ending in a way neither clause covers.** The rule is falsifiable and that is the point: a proposed fifth Fate that is genuinely neither a far-end outcome nor a cost in time refutes the closure, and it should be added rather than argued away. Nothing in the corpus has produced one in three attempts.

**Transit, and the one case that looks closest.** A Traveller waiting at a stop for a vehicle that never comes is the strongest candidate for a fifth — but `CONTEXT.md` → Transit already routes it to clause two: *"waiting is Leg cost, spent against the Commute Budget like any other travel"*, and *"a full vehicle is the next one rather than a refusal"*. If transit is ever built and waiting turns out to need an outcome rather than a cost, this is the ADR that reopens.

**A player unable to diagnose a failure from four values plus the failing Leg's index.** That is the real test, and it is a playtest question rather than an argument. If *completed* is being recorded for journeys players describe as failures, the fault is more likely in what reads the Fate than in the count of Fates — check that before adding one.

**The demolition split producing a misleading report at volume.** *Completed, and the shop was rubble* is correct and may still read badly in aggregate if a large fraction of Trips complete into demolished destinations. That would be a symptom of a Ruleset that demolishes too much — which `rulesets/minimal.toml` does on purpose — and the check is whether the same statistic looks sane under a Ruleset that models a city, which does not yet exist.
