# Amenity counts Building kinds, and the count belongs to the place while the set belongs to the Household

**Amenity is the count of distinct `[[building]] kind`s reachable on foot from a place, under `log(1 + x)` diminishing returns. Every kind counts — a shop, a school, a clinic, a park, a beach — and nothing is weighted by size, stock or quality.** The distinctness key is the kind's **name in the Ruleset**, so *which* destinations exist is content and not architecture.

**And the word names two objects, which is why nothing could be built against it.** The Amenity **count** is a property of a **place** and feeds `02 §2.4`'s desirability composition. The Amenity **set** — which destinations a given Household actually uses — is a property of a **mover**, is the Provider List, and is governed by `CONTEXT.md`'s *no proximity scope* rule. They share a word and share no machinery. `EMERGENCE` `LEGIBLE CAUSE` `FAST ITERATION`

## Why

**The enumeration was never the decision, and treating it as one is why it stayed open.** [`plans/0002`](../../plans/0002-open-questions.md) has carried *"how many kinds of school, clinic, plant, station, and what distinguishes them"* as unowned since session five, with the instruction that *"this is content that should follow the Ruleset work, but the **axis** on which variants differ should be settled before any are authored."* That is the correct division and this ADR settles only the axis. A list of destination names in a decision record would be [`adr/0015`](0015-all-tuning-data-is-hot-reloadable.md)'s tuning data smuggled into the binary's documentation.

### The key is the kind's name, and the alternatives were considered rather than skipped

`CONTEXT.md` → Amenity and `02 §5.5` both say **types**, and a Building already has a `[[building]] kind`. Three other keys were available:

- **An `amenity_class` grouping**, coarser than the kind, so that variety is authored deliberately rather than falling out of how many kinds happen to exist. Rejected **for now** rather than on principle: it is a strictly additive retrofit, and choosing it today would settle by argument a thing a Ruleset can settle by measurement.
- **The Good sold.** Principled and impossible to game, and far too coarse — only two of the five Goods are household-facing, so the ceiling is two and the term stops rewarding mixed use, which is the one job `02 §5.5` gives it.
- **The Need served.** Same objection at a ceiling of about six, and it inherits [`adr/0103`](0103-a-need-is-where-a-frequent-private-failure-accumulates.md)'s four-Need set, which was sized for a different question.

⚠ **The gaming risk is real, is *measurable*, and is therefore not settled here** ([`adr/0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)). A designer who authors thirty kinds of shop raises Amenity everywhere for nothing. What bounds it in practice is that a kind is not free — each needs a `[[zone_rule]]` to raise it, a decline Rule so it churns rather than accumulating, and inputs it can actually get — and **no shipped Ruleset has more than two kinds**, so the machine that would produce the refuting number does not exist yet. Routed, not answered.

### The count is the place's, and the set is the mover's

**This is the contradiction that stopped anything being built, and it reads as a contradiction only because one word was doing two jobs.** `02 §2.4` files Amenity as a cached spatial query at a Tile, sampled by a Cell at its four quadrant centres ([`adr/0126`](0126-a-cell-samples-desirability-at-its-quadrant-centres-and-a-line-sources-area-mean-does-not-converge.md)), with no mover anywhere in it. `CONTEXT.md` → Amenity makes it a term in the **per-Household** residential choice utility, and `02 §5.2` samples that at the Lot. And `CONTEXT.md`'s *no proximity scope* paragraph names *"an Amenity set"* alongside a Parking Shed and a Provider List as things that **"always belong to something that moves, never to a Building's Rule."**

All three are correct about their own object. **A count is not a set.** *How much variety stands within a walk of this address* is a standing property of the address, is what land value should read, and no Citizen is involved in computing it. *Which bakery this Household actually uses* is a choice made by something that moves, is sticky, and is the Provider List — which `CONTEXT.md` → Household has always defined as *"a short, sticky set of known shops, workplaces, and services."* The *no proximity scope* rule governs the second and was never about the first.

***A rule that forbids a Building's Rule from selecting among nearby options does not forbid a place from having a property.*** Conflating them is what made Amenity look like it needed a mover before land value could read it.

### Services count, and `adr/0032` is what proves it rather than what permits it

[`adr/0032`](0032-services-are-delivered-by-trips-not-by-coverage.md) widened *Business* to *destination* to give a park a home. The question it left is whether a **school** is an Amenity entry or whether its reach lives in `adr/0103`'s separate *"reach to any Service"* utility term, and the two ADRs list those as different items without ever assigning one.

**It is settled by a consequence `adr/0032` already committed to.** That ADR's stated payoff is: *"the ugly corollary arrives free: good schools raise land value, which prices out the Families the school was built for."* Land value reads the **place-side composition**. If a school's reach lived only in a Household's utility, **a school would not raise land value at all** and the corollary would be unwritable. So the corollary is evidence, and what it evidences is that a school contributes to the count.

**Both terms then exist and neither is redundant, because they differ in fungibility.** Amenity is variety and is fungible under a logarithm — the fifth kind matters less than the first, and one kind substitutes for another. Reach to a Service is **specific**: a Family with children cannot substitute a bakery for a primary school, and `CONTEXT.md` already gates the three school levels by Life Stage. A new school therefore raises land value for **everyone** near it and raises utility **only for Families**, which is exactly the shape `adr/0032` described and had no mechanism for.

### What this does not decide

**Nothing about quality, stock or size.** A count of distinct kinds is blind to all three, deliberately: `04 §5`'s *"a labour-starved district does not watch its businesses die, it watches them get smaller"* is invisible to this term, and that is correct, because a smaller bakery is still a bakery within a walk. Whether the city has a *separate* term that reads quality is [`adr/0103`](0103-a-need-is-where-a-frequent-private-failure-accumulates.md)'s question and not this one.

## Consequences

- 🔴 **[`adr/0123`](0123-desirability-ships-without-its-only-positive-term-and-a-caveat-that-must-travel-gets-a-test.md) scopes milestone 15's blocker wrongly, and the correction *shrinks* the milestone.** It states — and `LineSourceQueries.Amenity`'s doc comment and throw message repeat — that what is missing is *"one column and a catchment query"*, the column being a `kind` on `BusinessTable`. **Under this decision no column is owed at all**: a Building already carries its kind, and a `BusinessTable` column could never have enumerated a park, which is not a Business. ***The recorded blocker was narrower than the definition `adr/0032` had already widened***, and it has been quietly overstating milestone 15 ever since. **What is owed is the catchment query, and that is the whole of it.** Filed to [`plans/0012`](../../plans/0012-corpus-audit.md).
- **`CONTEXT.md` → Amenity is amended** to carry `adr/0032`'s widening, which it never received, and to say *kinds* rather than *Business types*. The old wording is why three documents disagree about whether a park counts.
- **The Provider List gains a name for what it already was.** *An Amenity set* and *a Provider List* are one object; `CONTEXT.md`'s *no proximity scope* rule keeps its full force over it, and loses its apparent force over the count.
- **`02 §2.4`'s Amenity row stands unchanged** — walkable catchment on the Road Graph, cached, Epoch-invalidated, a *time* rather than a distance. This decision changes what is counted, never how the catchment is found.
- ⚠ **A beach is now both an Amenity entry and the `− w₅·shoreline` term**, since `CONTEXT.md` → Water Body says a fouled beach *"degrades adjacent land value **and** removes a walkable Amenity destination."* That is coherent — presence and quality are different quantities — and it is **two terms over one object**, which somebody will eventually and reasonably call double counting. Recorded here so that the answer is *it was noticed* rather than *nobody looked*.
- 🔴 **The catchment radius is unratified and this ADR does not ratify it.** `~400 m on foot` appears only in prose, in a row that insists the range is a **time** and not a distance, and it carries no `[ ]` Ruleset key and no ratifier under [`adr/0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md). It owes a `plans/0002` §D entry on the day it is authored, and the Parking Shed's radius is the exemplar for what ratifying one costs.
- **Amenity is the smooth term [`adr/0126`](0126-a-cell-samples-desirability-at-its-quadrant-centres-and-a-line-sources-area-mean-does-not-converge.md) is waiting for.** That ADR records that its quadrature justification reopens when a term smooth inside a Cell arrives, and names amenity as the candidate. Building this is the trigger firing, not a surprise.

## What would trigger revisiting

- **A measured count that a designer inflated by authoring kinds.** If a Ruleset with a realistic kind set shows Amenity dominated by how finely commerce was subdivided rather than by how mixed the neighbourhood is, the `amenity_class` key above is the prepared answer and this ADR is amended rather than replaced. **The machine is a Ruleset with more than two `[[building]]` kinds, which milestone 12 is the first to ship.**
- **A destination that is not a Building.** A beach is the standing case and is handled by the shoreline term rather than by a kind; if a second arrives — a viewpoint, a waterfront, anything ground-pinned with no Building on it — then *kind* stops being a total key and the count needs a second source.
- **A consumer that reads an absolute Amenity rather than an ordering.** Everything today reads land value's ordering, and [`adr/0125`](0125-a-ratifier-that-needs-a-consumer-nobody-built-is-not-reachable-so-the-weights-get-a-floor-and-a-debt.md) records that no consumer of an absolute value exists. The first one makes `w₄` refutable and reopens the weight, not this axis.
