# A business is founded by a household or arrives through a gate, and both land in the pool

**A Business enters the city by **two channels**. A **Household founds** one, spending part of its own
balance to capitalise it; or one **arrives through a gate** from a Hinterland, carrying an authored
band. **Both create it UNPREMISED**, into the pool that already exists — neither channel tenants
anything, so [`adr/0069`](0069-placement-is-a-mechanism-of-its-own-and-construction-houses-nobody.md)'s
*construction houses nobody* is preserved rather than strained, and the sink
[`adr/0142`](0142-an-unpremised-business-emigrates-so-the-sink-is-the-one-households-already-use.md)
built already retires whatever nothing tenants. **The founding channel is a TRANSFER and conserves
money; the gate channel is the Outside Connection, which [`adr/0024`](0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md)
already names as money's only source.** ***Neither channel opens a new KIND of door.***
`SOLVE THE ACTUAL PROBLEM` `UNIQUE INDIVIDUALS` `NO VERDICT`

**This answers the question [`adr/0142`](0142-an-unpremised-business-emigrates-so-the-sink-is-the-one-households-already-use.md)
declined and [`plans/0039`](../../plans/0039-session-v-the-business-is-the-actor-and-the-building-is-premises.md)
**V31** reframed away.** `0142` settled the **sink** and says so in its own opening: *"What this ADR
does **not** settle is what **capitalises** a Business."* V31 then concluded that *"question 3 is not
'what creates a Business' — it is 'what CAPITALISES one', and that is a number."* ⚠ **That reframing
was half right and it cost a task.** The number is real and is owed a ratifier
([`plans/0002`](../../plans/0002-open-questions.md) §D2). But a **band with nothing to draw it** is not
a source, and collapsing the shape question into the number question left milestone 27 task 8 with no
first line of code — found on opening it, 2026-08-24
([`plans/0041`](../../plans/0041-the-business-is-a-thing-the-city-contains.md) **G22**–**G24**).
***A question that changes type has not necessarily been answered; it may only have been narrowed.***

## Why

**A Business needs a source, and the build has exactly one shaped hole where it goes.** Milestone 25
shipped the whole exit — `UnpremisedTable`, the give-up clock, `PlacementEngine.Retire` and
`World.Depart(Handle<Business>)` — and `UnpremisedTable`'s own remark says what is missing: *"nothing
**creates** one — `World.CreateBusiness` has no `src/` caller and milestone **27 task 8** is the first
pass that would."* **So the pool has an inflow of nothing and an outflow that works.**

### Why TWO channels rather than one

**Because this corpus already does exactly this, for the same reason, and says so.**
[`docs/04 §5`](../04-economy-and-goods.md) on skilled labour: skilled Citizens *"arrive by **Sorting**
… and are also **produced here**"*, and it draws the conclusion in its own words — ***"Sorting is the
fast channel and schooling is the slow one."*** **A stock the city both imports and grows itself is a
shape this design has already chosen once**, and the argument transfers without modification.

**One channel alone fails in a way a player would experience as a deadlock.**

- **Founding alone**: a city whose Households hold no money founds no shops. On `minimal.toml` every
  Household holds exactly **zero** — the fact `rulesets/taxed.toml` exists to work around — so a
  founding-only city would have no commerce for ever, with no lever the player could pull. ⚠ That is a
  mechanism admitting **one** outcome, which `NO VERDICT` refuses by name.
- **Gate alone**: shops become something the *outside* grants the city. The player's prosperity would
  not produce commerce, and a wealthy isolated city would be as shopless as a poor one. ⚠ It also
  makes every shop an immigrant, which is a strange thing for a city builder to assert.

***Two channels is not a compromise between them; it is the only arrangement in which both a poor
connected city and a rich isolated one still get shops.***

### Why the founding channel matters more than it looks

**It is the only one that makes a shop a thing a person did.** `UNIQUE INDIVIDUALS` is a pillar, and a
Business founded by a named Household has a founder the player can inspect — the money came *from*
somewhere the player can point at. ⚠ **The gate channel cannot offer this** and is not asked to: an
immigrant Business's money comes from outside the map, which is what a Hinterland is.

**And it is conserved by construction.** The founding channel moves money between two Bins inside the
city; nothing is issued. ⚠ **This matters more than it sounds**, because
[`plans/0041`](../../plans/0041-the-business-is-a-thing-the-city-contains.md) **G8** found the map of
money's doors is **already wrong by one**: `SyntheticCity` claims to hold *"THE ONLY PRODUCTION ISSUANCE
OF MONEY IN THE BUILD"* and milestone 11 added a second when an arriving Household draws its Hinterland
band. ***Had both channels issued, task 8 would have taken a miscounted map from two to four.***

## Consequences

- **`UnpremisedTable.Gate` becomes meaningful and should be declared** — and ⚠ **the table's own
  argument is what licenses it**. `Gate` was left out because *"a Business has no arrival door"* and
  *"a column meaningless for every one of its rows is worse than one meaningless for half of them."*
  With two channels it is meaningful for **half** the rows, which is the standard that table already
  holds `UnplacedTable` to. ***The absence was correct when it was written and its stated reason is
  what retires it.***
- **Two numbers, not one, and each is owed its own ratifier** under
  [`adr/0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md): the
  **founding band** (what a Household spends) and the **arrival band** (what an immigrant carries).
  🔴 **A founding RATE is a third**, and it is the one with no precedent to copy.
  `plans/0002` §D2 holds one row today and needs to become three.
- **The founding channel needs a trigger and it is the open work.** Which Household founds, on what
  condition, at what rate. ⚠ **It must not become a demand scalar** — there is no RCI meter and
  `CONTEXT.md` bans one — so the condition has to be a property a Household already has.
- **`adr/0024` is unmoved.** The gate channel is the Outside Connection paying in, which that ADR
  already licenses; the founding channel issues nothing. **No new door, and `MoneyIsConserved` needs
  no new case.**
- **`adr/0069` is preserved.** Neither channel tenants anything into a Building. A Zone Rule still
  raises premises and houses nobody, and placement is still the only thing that puts an occupant into
  standing stock.
- **The pool gains its inflow, so `adr/0006`'s bound is now doing work.** Until today the unpremised
  pool had a sink and no source, which is a bound guarding nothing.

## Rejected

**A Zone Rule creates a Business when it raises a commercial Building.** It keeps `adr/0069` intact on
a technicality — creating *unpremised* is not auto-tenanting — but it **couples shop supply to building
supply**, which is the exact coupling `adr/0069` was written to split apart. ⚠ **And it answers the
wrong question**: it says when a shop appears and stays silent on what funds it, so the band comes back
unanswered and the shop has no founder.

**A gate alone, symmetric with the Household.** The cheapest build by a distance — the arrivals path is
written and tested — and rejected because it makes commerce something the outside grants. See above.

**A founding channel alone.** Rejected for the `minimal.toml` case: every Household holds zero, so the
mechanism would be unreachable in the shipped world, which is
[`plans/0040`](../../plans/0040-the-business-is-the-actor-and-the-building-is-premises.md) **F43**'s
lesson arriving before the code rather than after it.

**Capitalising from the treasury.** A Business funded by the city is a city that owns its commerce, and
`PLAYER GOVERNS` draws the line elsewhere — the player *funds and regulates*, and the city decides what
gets built. It would also make every shop's existence a policy setting.

## What would trigger revisiting

- **A founding trigger that cannot be written without a demand scalar.** If the only honest condition
  for *which Household founds* turns out to need aggregate unmet demand, the founding channel is
  reaching for the RCI meter this design refuses, and it should be withdrawn rather than fudged.
- **Milestone 26's purchase landing and shops proving unable to earn.** Both channels assume a shop
  that can trade. If a Business cannot make money once `Scope.Pool` stops throwing, the bands are
  funding a slow death and the question is what a Business's income is, not what its opening balance
  was.
- **The two channels producing indistinguishable cities.** If a founded shop and an immigrant shop
  behave identically at every point after creation, the second channel is paying for itself in numbers
  and ratifiers and buying nothing, and one of them should go.
