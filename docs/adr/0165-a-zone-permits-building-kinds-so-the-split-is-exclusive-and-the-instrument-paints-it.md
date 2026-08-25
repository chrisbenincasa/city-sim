# A Zone permits Building kinds, so the split is exclusive and the instrument paints it

**Land permitted for a trade's premises is permitted for that and not for dwellings, and the reverse:
the split is exclusive, as `CONTEXT.md` → Zone already says a permission set is. `SyntheticCity` paints
it by deterministic index arithmetic — every *N*th block carries the trade bit — as a property of the
instrument rather than as Ruleset data. **No number is authored, no `purpose_tag` is drawn, and
universal mixed use is not permitted.**** `EMERGENCE` `PLAYER GOVERNS`

**The reason exclusive zoning costs nothing is that a Zone permits *Building kinds*, and a Business is
an Occupant rather than a Building.** *Living above the shop* — a Household and a Business sharing one
Building's occupancy under
[`adr/0147`](0147-a-business-takes-premises-by-placement-and-one-ceiling-counts-both-kinds-of-tenant.md)
— **is invisible to zoning and always was.** ***So the pairing that makes a street feel alive survives
an exclusive split, and the thing that does not survive is the one the corpus already calls an
exploit.***

---

## Why

### Universal mixed use is a closed exploit, and the mechanism that closes it is unbuilt

Session W's own first proposal was to permit both uses on every Lot — one literal, and `CONTEXT.md`
appears to bless it: *"Mixed use needs no machinery: it is a permission set with more than one entry."*
🔴 **It is a named exploit.** [`adr/0011`](0011-household-life-stages-and-self-generating-population.md),
in a table of what fails without each Household preference axis: ***"mixed-use tolerance | universal
mixed use becomes strictly optimal."***
[`adr/0027`](0027-preference-is-drawn-per-household-and-persists-for-life.md) lists it among six
anti-monoculture results, and `plans/0002` records it as one of **five exploits found and closed in one
session**, beside density spam and Office monoculture.

⚠ **What closes it is per-Household mixed-use tolerance, drawn from a Life Stage range and kept for
life — and that is UNBUILT.** `HouseholdTable.LifeStage` is a saved byte; there is no preference column
at all and milestone 20 has not been reached. ***So permitting universal mixed use now would install the
exploit precisely during the window in which nothing can push back on it***, which is
[`adr/0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) inverted: not *reasoning from an
absence*, but **leaning on one**.

### The vocabulary already said exclusive, and the build already contradicted it

`CONTEXT.md` → Zone: ***"A permission set over land: it lists the uses allowed there and forbids every
other."*** And: ***"Nothing is permitted by default. Unzoned land builds nothing."***

🔴 **`SyntheticCity` paints bit 0 on every Lot it carves**, so *nothing is permitted by default* is the
design's rule and *everything is permitted, for houses* is the generator's behaviour. **The exclusive
split is not a new constraint; it is the first time the generator obeys an old one.**

### A Zone permits kinds, so building-level mixed use is out of scope by construction

`ZoneRuleEngine.Create` tests `Lots.Zone[lot] & definition.Admits`, where `definition.Kind` is a
**`[[building]]` kind**. `World.Fit` instantiates a kind's declared trade into the Building **without
consulting the zone at all**. ***So a shop inside a dwelling is not a zoning question, and no permission
set has to mention it.***

⚠ **This is the whole answer to the objection that exclusive zoning makes a boring city**, and it is
worth stating because the objection is correct about every other city-builder. Here the interesting
pairing — residential and commercial in one structure — is **already shipped**, through occupancy rather
than through land. `rulesets/minimal.toml` has called it *living above the shop* since before there was
a Business to put there.

### The split belongs to the instrument, not to a Ruleset

[`adr/0164`](0164-a-ruleset-key-is-designer-facing-or-it-belongs-in-the-instrument.md)'s test —
*would a designer ever set this?* — returns **no**, for the strongest reason available: no designer
touches `SyntheticCity`, which describes itself as ***"an instrument, not a mechanism … when slice 10
lands there is a case for deleting it."***

**And the same comment settles how it is painted**: ***"It draws no randomness, deliberately. Every
value below is index arithmetic, so the city is a pure function of its size and needs no `purpose_tag`
— and therefore cannot correlate itself with a simulation decision that shares a stream. That is a real
hazard here rather than a hypothetical one."*** ⚠ **A drawn share was proposed and is refused by that
sentence**, which the instrument wrote about itself in advance.

**Every *N*th block, at `SyntheticCity.cs:693`'s existing loop**, which already has `column` and `row`
in hand and already passes a zone to `LotSubdivider.SubdivideBlock`. ***The change is what that value
is, not where it comes from.*** **Block granularity rather than Lot** because a block is where
`SubdivideBlock` already applies one zone to a whole frontage, and because scattering permissions
parcel-by-parcel would model nothing any zoning authority has ever done.

## Rejected

**Permit both uses everywhere.** One character, and it is the exploit above. ***Refused.***

**Key the split on distance from the lattice centre.** It would make a generated city *look* right
immediately — shops in the middle, houses out. **Refused because it encodes a location theory into world
creation at the same moment
[`adr/0163`](0163-demand-for-a-shop-is-elapsed-unserved-need-in-reach-and-building-claims-it.md) puts
location in demand's hands.** `CONTEXT.md` → Zone assigns Residential and Commercial their places by
***"the market — bid price on a Lot"***, and a generator that pre-decides the answer would make the
demand mechanism unfalsifiable in the only world that exercises it.

**Author the share as `[lots]` Ruleset data.** Session W's own proposal, carried as far as picking its
ratifier. Refused by `adr/0164`.

**Reopen [`adr/0135`](0135-a-market-needs-two-sides-so-twelve-ships-a-provider-and-the-price-moves.md)
and let a tier-1 shop be a lodger rather than premises.** Visible once building-level mixed use was
understood, and it would dissolve W-Q3 entirely by needing no land. **Not taken**: `adr/0135` settled
that the Provider is a second `[[building]]` kind, and the reason was that a market needs a seller with
its own premises to have somewhere to sell *from*. ***Listed because it is the option a future reader
will think of, and it is a reopening rather than a choice.***

## Consequences

- ✅ **No number, no `purpose_tag`, no §D row.** The land-use split stops being a design object.
  ⚠ **Nothing is owed when zoning becomes a player verb either** — an instrument's pattern is simply
  irrelevant to a played city, so there is no key to retire and no migration to write.
- ⚠ **A generated city will look wrong, and the header must say so.** Commercial blocks appear at a
  fixed stride rather than along a corridor, which is not how any city is zoned. ***A demonstration
  file's job is to exercise a mechanism, not to resemble a city***, and every shipped Ruleset already
  carries a header saying what it must not be read as.
- 🔴 **Two Zone Rules on disjoint bits means a Lot is sampled by both and admitted by one.** The rule
  whose bit is absent returns immediately at the zone test, so contention never arises and
  **declaration order stops mattering** — which is a consequence of *exclusive*, and would not hold
  under the rejected overlapping split.
- ⚠ **Land permitted for a trade and never claimed stays vacant for ever**, and that is the mechanism
  working. Exclusionary zoning has exactly this failure mode in reality; it is
  [`adr/0163`](0163-demand-for-a-shop-is-elapsed-unserved-need-in-reach-and-building-claims-it.md)'s
  demand signal being visibly wrong about where shops were wanted, which is **the best diagnostic this
  demonstration can produce** and should be looked for rather than tuned away.
- ⚠ **`SyntheticCity`'s stride moves the State Hash of every world it generates**, and it is a fixture
  constant rather than Ruleset data, so it moves under a rebuild rather than a reload. **Nobody is
  carrying a save** ([`adr/0100`](0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md)).

## What would trigger revisiting

- **Mixed-use tolerance being built** — milestone 20's preference table. Once a Household can *prefer*
  or *refuse* a mixed street, the exploit closes and overlapping permission becomes a live option
  rather than a refused one. ***That is the trigger this ADR most expects to fire.***
- **Zoning becoming a player verb.** `CommandKind.Zone` already carries a full sixteen-bit mask and has
  no production call site. Wiring it makes the split the player's, and this record governs only what a
  world starts with.
- **A run in which permitted commercial land never fills, or fills instantly.** Either says the stride
  and `adr/0163`'s threshold disagree, and the stride is the cheaper of the two to move because it
  ratifies nothing.
- **`SyntheticCity` being deleted.** Its own comment anticipates it. Everything here goes with it, and
  nothing else does.
