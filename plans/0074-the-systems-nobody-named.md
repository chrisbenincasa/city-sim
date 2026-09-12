# 0074 — The systems nobody named

**Definitions for the systems [`06`](../docs/06-roadmap.md)'s *Systems this corpus has never named*
table found absent.** Written 2026-09-11 against `main` at `1d8bded`.

## What this is

[`06`](../docs/06-roadmap.md)'s sweep found a set of standard city-builder systems that the corpus had
never mentioned once. A row in that table names an absence and what it would attach to, and stops
there. **This document gives each one a high-level definition, so the next sitting can classify it
under [`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) rather than
rediscover it.**

⚠ **An entry here is a definition and not a decision.** Nothing below is placed at a milestone,
nothing below is refused, and no entry may be cited as a design constraint. `adr/0070` is explicit
that only *refused* is evidence, and these are not refused.

⚠ **Open questions raised here stay here.** [`0045`](0045-amnesty.md) standing order 2 forbids new
entries in [`0002`](0002-open-questions.md) §A–§F while the amnesty runs, so each entry carries its
own settling question in its last field.

**Twelve further systems went to [`deferred.md`](../docs/deferred.md)** rather than here — prisons,
courts, ferries, cable cars, wildlife and ecology, historic preservation, architectural era, the
achievements and campaign shells, snow clearing, and birth-rate and immigration policy as player
levers. They are parked rather than refused, and that file owns the retrofit cost and the revisit
trigger for each.

## How to read an entry

Four fields, in this order.

| Field | What it holds |
|---|---|
| **What it is** | The system stated in [`CONTEXT.md`](../CONTEXT.md)'s vocabulary. If a term is not in that file, the entry says so |
| **Attaches to** | The shipped symbol or settled decision it would hang from |
| **Class** | *unbuilt* where the definition is complete enough to build from, *undesigned* where it leaves a real open branch |
| **What would settle it** | The one question whose answer moves the class |

## The finding that came out of writing this

**Four of these need no new mechanism at all.** `CONTEXT.md` → Amenity counts distinct `[[building]]`
kinds reachable on foot, and says outright that ***which destinations exist is Ruleset content and not
architecture***. A library, a museum and a hotel are therefore a Ruleset kind and a reason for a Trip,
not a system. What they are missing is the reason, and that is a smaller question than it looked.

---

## People and the life course

### Death care

**What it is.** An Attended Service taking the Trip that a Household's dissolution currently does not
generate, and a land use that the dissolution currently does not consume. A cemetery is a
`[[building]]` kind occupying Lots that never come back; a crematorium is one that does not grow.

**Attaches to.** [`adr/0011`](../docs/adr/0011-household-life-stages-and-self-generating-population.md)'s
dissolving Household, and
[`adr/0006`](../docs/adr/0006-no-collection-grows-with-elapsed-time.md)'s rule that nothing grows without
a sink.

**Class.** *unbuilt*. The mechanism is the ordinary Attended shape and nothing about it is open.

**What would settle it.** Whether a dissolution that consumes no land is a hole or a decision.
A cemetery is the design's only land use whose stock **only ever rises**, which makes it the sharpest
test of `adr/0022`'s *Land is a stock the city spends* — and the one case where a sink would need a
sink of its own.

### Childcare

**What it is.** An Attended Service whose catchment is the Family Life Stage and whose output is
**present adult labour supply**. Every other Service in the design gates a future Skill Tier or meets
a current Need; this is the only one that would decide whether an adult can take a job today.

**Attaches to.** `CONTEXT.md` → Family and Mature Family, `EmploymentEngine.Assign`, and
[`adr/0032`](../docs/adr/0032-services-are-delivered-by-trips-not-by-coverage.md)'s drop-off, which
that ADR already specifies and `deferred.md` already records as unbuilt.

**Class.** *unbuilt*. Its Trip is `adr/0032`'s drop-off, which is specified and waiting for a builder.

**What would settle it.** Whether a Household with children and no reachable childcare loses a wage,
loses a worker, or loses nothing. That choice decides whether this is a constraint on the labour
market or a cost line, and the three answers are different games.

### Elder care

**What it is.** An Attended Service whose catchment is the Empty Nest Life Stage, plus a residential
kind that Households move into rather than out of.

**Attaches to.** `CONTEXT.md` → Empty Nest, `LifeStageEngine.Sweep`, and `CivicEngine`'s bed
occupancy, which already models an inpatient stay.

**Class.** *undesigned*. Whether a Household relocates into care, or receives it where it lives, is a
genuine branch and nothing in the corpus leans either way.

**What would settle it.** Whether ageing should produce a **move**. The design already carries an
ageing population's fiscal weight through Empty Nest supplying no labour, so this asks whether it
should also carry a spatial consequence.

## Reading the city

### Segregation, and a reading for it

**What it is.** Not a mechanism. A **reading** over a sorting the city already performs —
[`adr/0027`](../docs/adr/0027-preference-is-drawn-per-household-and-persists-for-life.md) draws a preference per
Household and keeps it for life, and `02 §5.4`'s utility sorts on income and taste, so neighbourhoods
will stratify whether or not anybody looks.

**Attaches to.** `adr/0027`, `02 §5.4`, `Choice`, and `02 §9`'s Evidence.

**Class.** *undesigned*. The mechanism ships and no term exists for what it produces.

**What would settle it.** What the city is being read **on**. Skill Tier, Household balance and Life
Stage are all present and all say different things, and picking one is a statement about what the game
thinks sorting is.

⚠ **This is the entry most likely to go wrong quietly.** A mechanism that produces an outcome the
game cannot name is the failure `02 §9` exists to prevent, and it is already live.

### Public opinion

**What it is.** A channel through which a Household harmed by a Policy registers that harm **before**
it leaves. `CONTEXT.md` → Policy states that every Policy helping someone is paid for by someone, and
Departure is currently the only response available.

**Attaches to.** `PolicyEngine.Sweep`,
[`adr/0102`](../docs/adr/0102-a-housed-departure-is-a-comparison-the-household-re-runs-not-a-threshold-it-crosses.md)'s
Departure, and `01 §7`'s Evidence panels.

**Class.** *undesigned*. What a Household does short of leaving is stated nowhere.

**What would settle it.** Whether incidence needs a **mechanism** or only a **readout**. A readout
showing who pays for a Policy may be the whole requirement, and it would be far cheaper than
modelling a response.

⚠ **`00-vision` refuses a global happiness number and this must not become one.** The refusal is about
aggregation, not about incidence.

### Homelessness as a place

**What it is.** A state an unhoused Household occupies **on the map** rather than in a queue. Today a
Household enters the Unplaced Pool, waits, and either is housed or gives up and leaves.

**Attaches to.** `UnplacedTable`,
[`adr/0130`](../docs/adr/0130-the-pools-bound-is-a-duration-and-the-unhoused-channel-ships-with-the-gate.md)'s
give-up bound, and `PlacementEngine.Place`.

**Class.** *undesigned*. Whether the unhoused should have a location is the branch, and nothing has
asked.

**What would settle it.** Whether the game wants the player to **see** its most legible failure. The
design's product is clicking on something and finding out why, and this is the one urban failure with
no picture.

## Movement

### Road pricing

**What it is.** A Policy of the Constraint family priced on entry to a Segment, taking Money from a
Traveller's Household to the treasury.

**Attaches to.** [`adr/0072`](../docs/adr/0072-the-mode-mask-is-saved-on-the-arc-and-the-segments-is-derived.md), which already
names a toll as the kind of thing a Segment would carry; `01 §4`'s Govern verb; and the volume-delay
function that prices congestion on entry today.

**Class.** *unbuilt*. The location is settled and the Policy families exist.

**What would settle it.** Whether a Traveller may be charged **mid-Trip**. Every Policy shipped so far
sweeps a population on an interval, and a toll fires on an event, which may be a new Rule family
rather than a new Policy.

⚠ **It is the only Govern lever that would act on the traffic model directly**, and the design has
spent heavily making that model congestible.

### Cycling

**What it is.** A third travel mode between walking and driving, with its own speed, its own
Commute Budget reach, and access to the foot graph.

**Attaches to.** [`adr/0014`](../docs/adr/0014-grid-streets-with-freeform-arterials.md)'s pedestrian
edges and the Severance they produce, and
[`adr/0098`](../docs/adr/0098-a-citizen-travels-in-their-households-mode-and-mode-choice-is-undesigned-rather-than-unbuilt.md),
which makes mode follow car ownership and leaves mode choice undesigned.

**Class.** *undesigned*, and it inherits that from mode choice rather than owning it.

**What would settle it.** Mode choice, which is already the sole row in `06`'s *Mechanisms nothing
designs*. A third mode cannot be chosen by a rule that picks between two.

⚠ **Severance is currently binary because it is a property of the foot graph alone.** A bicycle is what
would make it graded, so the value of this entry is larger than a mode and the price of waiting rises
with every Ruleset that states `[roads]`.

## Land and matter

### Waste disposal on the map

*Covers landfill and incineration, which the corpus names only inside `references.md`'s prior-art
survey.*

**What it is.** A sink for the Waste Good inside the city. Waste is already a Good that is hauled and
congests; its only sink today is an Outside Connection at a price, which makes it a balance-of-payments
line rather than a land use.

**Attaches to.** `CONTEXT.md` → Waste and Dispatched collection,
[`adr/0088`](../docs/adr/0088-the-price-of-a-far-hinterland-is-paid-in-your-own-traffic.md)'s
gate, and `adr/0051`'s pollution stock.

**Class.** *unbuilt*. A landfill is a Building kind with a Bin that fills and never drains; an
incinerator is one that drains and emits.

**What would settle it.** Nothing about the mechanism. ⚠ **What this entry actually records is that
`deferred.md` already names the exact risk on the liquid half** — that the Networked bundle plays as
pure budgeting — **and the solid half carries the same risk with no entry anywhere.**

### District heating

**What it is.** A fourth Utility, and the reason it is here is that it would **break the Utility
abstraction**. `CONTEXT.md` states one abstraction distinguished by a single parameter, storage
capacity, and heat is distance-limited in a way that District pooling cannot express.

**Attaches to.** `CONTEXT.md` → Utility, and `adr/0013`'s District pooling.

**Class.** *undesigned*, and deliberately so. This entry exists to mark the untested edge of a
generalisation the design leans on, not to propose a system.

**What would settle it.** Whether the Utility abstraction is claimed to cover **every** Networked
Service or only the three it names. Those are different claims and `CONTEXT.md` currently reads as the
first.

### Land banking

**What it is.** An owner who declines to build on a Lot that is worth building on, holding it against
a later price.

**Attaches to.** [`adr/0022`](../docs/adr/0022-land-is-a-stock-the-city-spends.md)'s Land as a spent
stock, `02 §5.6`'s development pro-forma, and `ZoneRuleEngine.Sweep`, which raises a Building at the
sample instant.

**Class.** *undesigned*. The design has **no actor who could decline** — `PlacementEngine` withdraws
from nobody and there is no developer or landlord, which [`0070`](0070-the-city-spends.md) records as
the reason it may not price private construction.

**What would settle it.** Whether the design wants a landowner at all. That is a larger question than
this entry, and it is the same missing actor that blocks construction pricing and compulsory purchase.

⚠ **Withholding is what would make a finite stock adversarial rather than merely finite.**

## Destinations

**All four below are `[[building]]` kinds under `CONTEXT.md` → Amenity, and none of them is
architecture.** What each needs is a reason for a Trip.

### Libraries

**What it is.** An Attended destination that meets no Need and confers no Skill Tier, counting once
for Amenity like any other reachable kind.

**Attaches to.** `CONTEXT.md` → Amenity, `adr/0152`, and `ServiceEngine.Attend`.

**Class.** *undesigned*, thinly. The kind is expressible today; what it **does** is not stated.

**What would settle it.** Whether a destination may exist purely as an Amenity entry with no Need
behind it. `adr/0103` says a park refuses nobody who can reach it and is therefore not a Need, which
is the precedent, and a library is the case that tests whether that generalises.

### Museums and culture

**What it is.** The same shape as a library, distinguished only by whether it draws Trips from outside
the city.

**Attaches to.** `CONTEXT.md` → Amenity, and the tourism entry below.

**Class.** *undesigned*. It is either a second library or the first thing tourism needs, and nothing
decides which.

**What would settle it.** Tourism. Without visitors a museum is a library with a different name.

### Hotels

**What it is.** Where a visitor stays. A residential kind whose occupants are not Households of this
city and do not persist.

**Attaches to.** `HouseholdTable`, `adr/0147`'s tenancy ceiling, and tourism.

**Class.** *undesigned*, and **it cannot be settled before tourism is.** A hotel with nobody to put in
it is a Building kind with no mechanism.

**What would settle it.** Whether a visitor is a Citizen. The design's answer to *everybody is a person
you can meet* makes a transient occupant a real question rather than a bookkeeping one.

### Evening destinations

*Covers nightlife, which the corpus has never named.*

**What it is.** A land use that draws Trips outside working hours.
[`adr/0101`](../docs/adr/0101-a-commute-is-two-journeys-and-the-days-shape-is-a-property-of-the-job.md)
derives the Day's shape from job start hours alone and notes that the middle of the Day is a property
of land use, so with only workplaces and shops the network has two peaks and a dead night.

**Attaches to.** `adr/0101`'s Shift model, `ShoppingEngine.Step`, and `CONTEXT.md` → Amenity.

**Class.** *unbuilt*. A Trip generator keyed to an hour band is the ordinary shape and nothing about it
is open.

**What would settle it.** Nothing blocking. ⚠ **This is the cheapest entry in the document and the one
with the most visible effect**, because it populates twelve hours the simulation currently leaves empty
and Amenity's walkable-variety term already rewards it.

## Outside the city

### Tourism

*Named in the corpus only inside [`adr/0012`](../docs/adr/0012-routing-intent-lives-in-the-agent.md),
as GlassBox's tourist packs cited as a failure to avoid.*

**What it is.** Trips originating outside the city that are not immigration. A visitor crosses an
Outside Connection, spends, occupies a bed, and leaves without ever joining the Unplaced Pool.

**Attaches to.** `adr/0023`'s arrival through the gate, `HinterlandTable` and the row 31 work on
`worktree-row-31-attracts-people`, and `adr/0088`'s throughput ceiling.

**Class.** *undesigned*. Whether a visitor is a Citizen, a Household, or neither is the branch, and it
decides everything downstream.

**What would settle it.** Row 31. ⚠ **The gate is being rebuilt right now**, and a visitor is a second
kind of thing crossing it — so the cost of asking this question is at its lowest during that work and
rises afterwards.

⚠ **`adr/0012` cites tourist packs as the GlassBox failure to avoid**, so anything built here must not
be an identical agent on an identical field.

### Telecommunications

**What it is.** A candidate fourth Utility, named here only because `plans/0001` uses the internet as a
routing analogy and nothing has ever proposed it as a system.

**Attaches to.** `CONTEXT.md` → Utility.

**Class.** *undesigned*, and this entry expects it to become **refused**. A Networked Service with no
storage and no scarcity is a Utility whose single distinguishing parameter is zero, which makes it a
line item rather than a decision.

**What would settle it.** Whether it would ever produce a **spatial** choice. If not, it fails
`00-vision`'s own test and should be refused rather than built.

### Modding

**What it is.** Not a simulation system. Whether a player may author Ruleset content and share it.

**Attaches to.** [`adr/0015`](../docs/adr/0015-all-tuning-data-is-hot-reloadable.md)'s hot-reloadable
TOML, `RulesetCatalogue`'s content-hash identity, and the schema at `rulesets/ruleset.schema.json`.

**Class.** *undesigned*. ⚠ **The capability is most of the way built and nobody has said so.** A
Ruleset is already authored TOML, already validated against a published schema, already identified by
content hash and already hot-reloadable, which is the hard half of modding arriving as a side effect of
determinism.

**What would settle it.** Whether a shared Ruleset may move a State Hash, and what a save carrying an
unknown Ruleset hash does. `adr/0112` refuses a mismatch today, and that refusal is the whole question.

---

## What this document does not do

- **It places nothing at a milestone.** [`06`](../docs/06-roadmap.md) sequences work; this defines
  terms.
- **It refuses nothing.** Under `adr/0070` a refusal is the only classification that counts as
  evidence, so a refusal written casually here would be worse than the silence it replaced.
- **It opens no `plans/0002` row and no ADR**, per [`0045`](0045-amnesty.md) standing orders 1 and 2.
- **It states no numbers.** Nothing here is tuning and nothing here is hash-bearing.
