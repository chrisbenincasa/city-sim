# Clearing land is bought rather than taken, and Demolish is the sixth verb

**`01 §2`'s verb list becomes six. `Demolish` takes over the bulldoze `Connect` has carried unnamed since [`0077`](0077-a-road-edit-is-one-segment-and-the-player-lays-streets-only.md) and extends it from Streets to Buildings.** Every route to clearing occupied ground — the verb, and the compulsory purchase that `Connect` and `Service` perform on what stands in their way — **pays market value, read off the land value Map Layer and paid to whoever is displaced**. Nothing is confiscated and nothing is free. Two further routes need no purchase because nobody is displaced: a **`Govern` clearance programme** over abandoned stock, and **re-zoning**, which withdraws *replacement* and never the Building.

Guiding concepts: `PLAYER GOVERNS`, `LEGIBLE CAUSE`, `NO VERDICT`, `HONEST DEGRADATION`.

**Arguable** under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md): every candidate differs in what the player is permitted to do, and no machine produces a number that settles a permission. The **price** it names is measurable and is deliberately not a number here — see *Consequences*.

## Why

`01 §2` listed five verbs and none of them removed a Building. The Zone Rule demolishes on its own initiative and `Connect` bulldozes Streets, so the city could clear land and the player could not. That was never decided; it is what the list happened to contain.

### The pillar does not forbid a bulldozer, and the corpus already required one

The objection that stops most bulldozer proposals here is pillar 3, govern-don't-place. It does not apply. `01 §2`'s own sentence is *"the player never **places** a Building that Citizens live or work in"*, and removing one is not placing one — the player still cannot author what stands anywhere, only veto what does. A veto is an ordinary governing act, and it is compulsory purchase with no road attached.

**`CONTEXT.md` → Derelict has meanwhile been asserting the verb outright**, for a reason that has nothing to do with this question: a Building whose kind a reloaded Ruleset no longer declares *"has no failure pressure and cannot be condemned by it; **it stands until the player clears it**"* ([`0057`](0057-dereliction-is-a-design-time-state-and-it-is-derived-rather-than-recorded.md)). That sentence has had no verb behind it since it was written. A vocabulary entry stating a player act the verb list does not contain is the same failure as a consequence with no mechanism, seen from the definitions side.

### `Connect` already demolishes, and calling it connecting was the drafting error

`adr/0077` gives `CommandKind.Connect` a lay-or-bulldoze flag, so the player has been demolishing since 5a-bis. Keeping the count at five would therefore have preserved a number rather than a distinction, at the price of `Connect` meaning *lay a road*, *unlay a road* and *remove a house* — a verb defined by subject matter instead of by what the player does, which is the exact split `01 §2` records having already collapsed once when `Fund` and `Regulate` became `Govern`. Six verbs cut at a better joint than five did: `Connect` lays, `Demolish` removes, and neither has a second job.

### Bought rather than taken, because the price is the only thing that makes it a decision

A free bulldozer is a verb with no cost, and a verb with no cost is not governed by anything the city does. Pricing it is what puts it on the same footing as every other act in this design, and the price must be a **mechanism** rather than a fee, because clearing a slum and clearing a street of townhouses are not the same act.

**The mechanism already exists: the land value Map Layer.** It is composed from what the player's own city did to that ground, so the price is high exactly where clearing is most destructive and collapses exactly where the city has already failed — which is `01 §6`'s *"the dead end is expensive, not closed"* arriving without a rule saying so. It introduces **no authored number at all**, which is the property [`0059`](0059-a-zone-rules-sample-is-a-revisit-period-so-the-ruleset-states-a-duration.md) and [`0069`](0069-placement-is-a-mechanism-of-its-own-and-construction-houses-nobody.md) keep finding is available when a value is looked for in the city instead of chosen for it.

**It is paid to the displaced Households, and that is not decoration.** [`0024`](0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md) makes the Outside Connection money's only door, so a payment to nobody would be a sink the ADR forbids. Paying the evicted makes compulsory purchase a **transfer**, conserved by construction, and it gives the mechanism a consequence nothing had to design: a cleared district arrives in the Unplaced Pool with capital, so the displaced bid for somewhere better and the money moves through the city ahead of them. `World.DestroyBuilding` already evicts Occupants into the Pool with their Money and Savings intact ([`0054`](0054-a-demolished-buildings-households-are-evicted-into-the-unplaced-pool.md)); this adds a credit on the way out.

### No act in this design yields empty ground, and that is what answers bulldozer-by-proxy

Compulsory purchase under `Connect` leaves an Arterial; under `Service` it leaves a service Building with a footprint and an upkeep obligation under [`0035`](0035-infrastructure-is-priced-by-what-it-consumes.md). Neither can be used as a cheap way to obtain a cleared Lot, because neither produces one. `Demolish` is the single exception and it is the one that pays full market value for the privilege — so the scalpel exists, it is priced, and the acts that clear land incidentally cannot be repurposed into it.

### Where the geometry actually puts compulsory purchase

**Laying a Street can never require it.** `StreetGrid` puts Segments only on block boundaries and a Lot is a point on a block face, so a lattice line either already carries a Street or carries no Lots — a new Street adds frontage and cannot run through a Building. An **Arterial** is the opposite: `RoadGenerator` walks it as a freeform polyline at one-Tile steps and destroys every Street Segment it crosses, which is that generator's one substantive claim. So compulsory purchase under `Connect` is an **Arterial** mechanism exclusively, and Arterials are refused by name under `adr/0077` today.

That is the honest scope of this decision: driving a motorway through a dense district — the case that makes `03 §3.7`'s **Severance** something the player *paid for* rather than something that happened — is designed here and buildable when Arterials are.

### Re-zone-and-wait is a real lever and the obvious description of it is false

Withdrawing a permission does **not** condemn anything. `ZoneRuleEngine.Condemn` is keyed on Rule Instance starvation and never reads the Lot's permission set, which `02 §5.9` states deliberately via [`0055`](0055-a-zone-rules-permission-set-scopes-what-it-builds-never-which-lots-it-looks-at.md) — scoping the population by permission would be immortality by paintbrush. So a re-zone removes **replacement**, not the Building: a district that is churning empties itself instead of rebuilding, and a district that is healthy is not cleared this way at any speed, because nothing is declining to not-replace.

Stated that way it is exactly `01 §6`'s *"rezoning to a lower band so cheaper uses can bid"*, and it costs no mechanism. Stated as *the Zone Rule condemns in its own time* it would have been a second condemnation trigger with its own threshold and its own ratifier, which is what [`0079`](0079-a-building-outlives-its-frontage-and-an-address-that-has-none-is-a-hole-the-trip-model-reports.md) closes by warning against bolting a second pressure source onto `adr/0053`.

### The clearance programme is a Policy and `01 §6` had already said so

`01 §6` lists *"clearance of abandoned stock"* among the recovery levers and then says of that whole list: *"per §2 they are ordinary **Policies** whose preview simply reads 'applies to 0 Tiles' until there is something to act on."* So the District-scale clearing tool is `Govern`, funded from the treasury, and the player does not click a Building to invoke it. It is not a competitor to `Demolish` but its wholesale sibling — and it needs no compensation term, because abandoned stock has nobody left in it to compensate.

### The shell has to stand, and `02 §5.9` says both

Everything above presumes abandoned stock **exists as standing Buildings**, and `02 §5.9` contradicts itself twelve lines apart. *"Past a further threshold, it is abandoned and its Lot returns to vacant"* against *"an abandoned Building raises its neighbours' failure pressure"* and *"the specific accumulated condition is **retained on the Building** and shown in the inspector"*. A condition cannot be retained on a Building whose Lot has gone back to vacant, and the build implemented the first reading: `World.DestroyBuilding` frees everything.

**The second reading is the one three other things depend on**, so it is the one that stands:

| What depends on it | What breaks under immediate demolition |
|---|---|
| `02 §5.9` abandonment contagion | **no carrier.** Bare ground has no dereliction term in the desirability composition, so the mechanism the section calls *deliberate* cannot occur |
| `01 §6` sustained detection | its duration is *"the time abandonment contagion takes to reach neighbours"* — a constant **derived from a mechanism** rather than picked for the interface, and there is no mechanism to derive it from |
| `01 §6` and `02 §176` clearance | *"rezoning and clearance of abandoned stock as two separate recovery levers"* has one lever with no referent |

So **abandonment empties a Building and leaves it standing on its Lot**, and clearing it is the player's or the clearance Policy's. It must not be called *derelict*: `CONTEXT.md` → Derelict is a Ruleset-edit state that *"shares none of its machinery"*, and the two states differ in the only way that matters here — dereliction is what a file did and abandonment is what the city did, so only the second has a cause worth reporting.

## Consequences

**`01 §2` states six verbs**, and `Demolish` applies to Streets and Buildings alike. `CommandKind` gains `Demolish`; `Connect`'s bulldoze flag is superseded rather than retained, so there is one spelling of *remove a thing* in the Input Log rather than two.

**A price per Building is owed and it is not a number.** It is `f(land value at the Lot, what stands on it)`, hash-bearing, and the *composition* is the decision here — the weights are not chosen in this ADR, and it is the second thing in the corpus to be blocked on the land value target, which is a named hole in `MapLayers`. Under [`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md) it enters `plans/0002` §D **unset**, which is a gap rather than a debt, with its ratifier named as the first play session in which a player clears something.

**Compulsory purchase is an Arterial mechanism and inherits Arterials' schedule.** It is designed and unbuildable, which is [`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) used the way round it is meant to be used: the absence of Arterials is *unbuilt*, so the answer is build them, and this decision does not compensate for their absence.

**`02 §5.9` is corrected rather than amended away.** Abandonment leaves the Building standing; its Lot is occupied; contagion has a carrier. The **sink** that keeps this inside [`0006`](0006-no-collection-grows-with-elapsed-time.md) is the one an occupied Lot already has — redevelopment when land value falls far enough that it pencils (`02 §5.5`), plus the two clearing routes above. Abandoned stock is bounded by the Lot count, not by elapsed time.

> 🔴 **CORRECTED 2026-08-26 by [`0170`](0170-an-abandoned-shell-collapses-on-a-clock-because-a-bound-is-not-a-sink.md), and the sentence above is TRUE AND INSUFFICIENT.** *Abandoned stock is bounded by the Lot count, not by elapsed time* survives as a statement about `0006`; it does not survive as a statement about a sink. Milestone 17 built the shell this ADR asked for and then measured the city it produces with no sink but the two player routes: on `rulesets/declining.toml` at 10,000 Citizens, **1,201 built → 0 built by Tick 32,768**, with all 1,204 buildable Lots holding a permanent shell and the count flat from there to 65,536. ***The collection is bounded, monotone and terminal, and `0006` is green the whole way down*** — a bound answers *does it grow for ever* and not *can the city come back*. The two clearing routes below are unaffected and keep their design; what they lose is the claim to be sufficient on their own. The redevelopment route this paragraph names first remains **unbuilt**, because it reads the land value Map Layer's target.

**`CONTEXT.md` → Derelict's *"it stands until the player clears it"* acquires the verb it has been asserting**, and → Failure Pressure gains the standing-shell fact and the pointer that keeps the two states apart.

**Nothing here observes its own consequence.** No verb reaches the simulation: `Simulation.cs` leaves `Service` and `Govern` unapplied, Arterials are refused, and there is no treasury to pay from. This is a design decision recorded ahead of its milestone, and the slice that builds it is the one that owes the acceptance test.

## What would trigger revisiting

**Market value proving unreadable as a price.** The claim is that a player can look at the land value overlay and predict what clearing will cost. If playtest shows the number arriving as a surprise — most likely because land value composes from terms the player cannot see acting on it — then the price wants a simpler basis, and replacement cost under `adr/0035` is the candidate that was rejected here for being flat across a city.

**`Demolish` becoming the primary verb.** The design's expectation is that clearing is rare and expensive and that the wholesale routes carry the volume. A player who bulldozes constantly is a player for whom the Zone Rules are not producing what they want, and the fault would be in the Rules rather than in the price — but a price that failed to bite would look identical from outside, so the two want separating before either is tuned.

**Abandoned stock accumulating without bound in a long run.** The sink argued above is redevelopment, and redevelopment needs land value to fall far enough to pencil. If a 100,000-Tick run shows standing abandoned Buildings trending upward at steady state, `adr/0006`'s collection half is violated and the fix is the bid-price damper rather than a cap on the stock. **That is measurable, it is the first long run after decline is built, and no session may close it.**

**A second act wanting to take land without paying.** Anything that clears ground for free — a disaster is not one, since `01 §5.2` makes disasters world-scheduled and unaimed — would make the price optional in fact while remaining stated here, which is the shape [`0044`](0044-the-map-layer-diffusion-cadence-is-the-designers-number-not-the-profilers.md) records as *citing an ADR is not applying it*.
