# Economy and goods

The supply chain, the money, and the path by which a broken chain becomes a specific unhappy Household.

The governing constraint is scope. Supply chains exist here to create **pressure that propagates into people**, not to be an optimisation surface. If zoning ever becomes an afterthought to logistics, the design has drifted — see the anti-goal in [`00-vision.md`](00-vision.md).

---

## 1. The goods list

**Five Goods, two extraction steps, three processing steps, maximum chain depth of three.**

| Good | Produced by | From | Consumed by |
|---|---|---|---|
| **Produce** | Farms | Fertility | Food processing |
| **Food** | Food processing | Produce | **Households, daily** |
| **Timber** | Forestry | Woodland | Materials |
| **Materials** | Mills, plants | Timber | **Construction**, and Consumer Goods |
| **Consumer Goods** | Factories | Materials | **Households, discretionarily** |

```
Produce ──▶ Food ──────────────▶ Household  (Sustenance)

Timber ──▶ Materials ──┬───────▶ Construction
                       └──▶ Consumer Goods ──▶ Household  (Satisfaction)
```

**This is not a total map of the Needs, and stopped being one on 2026-08-15.** There are **four** — Sustenance, Satisfaction, **Education** and **Health** — and the last two have no Good, no Bin and no price behind them, because a Need exists for each *exhaustible* thing a Household consumes and can be refused on arrival, whether or not a Good carries it ([`adr/0103`](adr/0103-a-need-is-where-a-frequent-private-failure-accumulates.md)). Everything standing — the commute, the rent, the neighbourhood, whether a Service can reach you at all — is a term in `02 §5.4`'s utility and never a Need.

Every Good can also arrive through an **Outside Connection** at a price, so no chain is ever strictly required — importing is always the expensive fallback, which is what makes local production a *choice* rather than a prerequisite.

### The employer that produces no Good

**Office is not in the table above, and that is the point.** It consumes no Good, produces no Good, emits no pollution, and wants no freight access. What it earns is money, from outside.

What it consumes is **labour, in a mix** — an office building holds janitors, administrators, analysts and partners, so Office is not "the tier-3 zone" but the zone with the **highest tier-3 share**. It is therefore the clearest instance of the rule in [`adr/0026`](adr/0026-wages-are-posted-locally-and-never-cleared.md) that employers demand a mix rather than a tier, and it is subject to that rule's consequence: **an office cannot staff itself in a city with no tier-1 or tier-2 employment**, because that is where tier-2 workers come from.

It also consumes **service capacity** heavily — power, water, waste — which is coverage rather than a Good. That requires service demand to scale with what a Building actually *is*, not a binary in-catchment test, and it is a real counterweight: **Office pays in services what it saves in logistics.**

**Its exports need no Shipment, but they do need a gate.** Services are not physical, so no Vehicle carries them and no freight access is required. But Office generates **Trips to Outside Connections in proportion to its export volume** — business travel — which uses existing machinery, contributes real congestion, and means a metropolis cannot export through one lane. So Office wants to be central *and* well-connected outward, which is where real central business districts sit.

**Office is what makes a downtown exist.** It is the only land use with no logistics constraint at all, so it outbids everything for the most *accessible* land — and §5.5 already resolves Lot competition by highest bid. Nothing declares a centre and no rule mentions one. Two forces stop it becoming a monoculture, both already in the design: offices house nobody, so a pure office core makes every worker commute in at the same phase and **strangles itself on Segment Stress and Commute Budgets**; and **Amenity** rewards walkable variety, so a centre of offices, shops and homes outperforms a centre of offices alone.

A sixth Good was the obvious alternative and is rejected twice over: by the resource discipline below, and because its physical movement would be a fiction. Nobody trucks consulting hours to a warehouse, so it would be a lie inside the one system this document insists must be conserved and auditable.

It closes two holes that would otherwise stay open. **Education has no sink without it** — every other employer here is a farm, a mill, a factory, or a shop, so a city full of graduates would staff sawmills and `adr/0010`'s Sorting mechanism would have nowhere to land. And **nothing pays for the endgame**: `adr/0022` commits to a mature city importing Food and Materials forever, and residential taxation scales with the same population generating the Food bill. Office is the export side.

> **The mature city imports Goods and exports services.** Sealed land forces the imports; educated labour earns the currency that covers them.

### Money is conserved, and it flows

Money is not a score. It is a **conserved stock that moves without transport** — never created or destroyed inside the city, held by Households, Businesses, and the treasury, and moved between them by wages, purchases, rents, and taxes. The **Outside Connection is its only source and sink**, exactly as it is for Goods, which is what lets Office earn without anything being unconserved.

So the city has a **balance of payments**: Food and Materials imports flow money out, service exports flow it in, and a city whose land is sealed and whose schools were never built watches its supply drain. Borrowing is the damper. See [`adr/0024`](adr/0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md).

⚠ **Of the four channels named above, two move money today and two name an actor that does not exist**, and the list is written as design rather than as status — read it that way. **Purchases** move it as of milestone 26 task 4: a `pool` term pays its seller, and the price comes off the District market row ([`adr/0167`](adr/0167-a-purchase-picks-its-seller-by-a-draw-and-waits-on-the-market-rather-than-on-a-shop.md)). **Taxes** move it as a Policy, which `taxed.toml` and `levied.toml` demonstrate. **Wages** do not: a `wage` key is *refused at load* until milestone 15 ([`adr/0026`](adr/0026-wages-are-posted-locally-and-never-cleared.md)). **Rent** does not, and cannot be written: a Building never holds money ([`adr/0113`](adr/0113-a-business-is-an-occupant-with-its-own-balance-and-a-building-never-holds-money.md)) and no table names a landlord.

**That absence has a consequence, and it is a shop's only recurring cost.** A trade with no outgoings is one nothing can end — an unsold shop stops on a full Bin, which clears its failure-pressure clock, so it stands solvent and dead for ever. [`adr/0169`](adr/0169-a-standing-cost-needs-a-counterparty-so-a-trade-pays-rates-until-there-is-a-supplier-to-pay.md) gives it a **levy to the treasury**, because conservation demands a counterparty and the treasury is the only money-holding actor a shop can reach. ⚠ **It is a stand-in with a named successor** — cost of goods to a supplier — ***and rates are the least explanatory of the costs a shop actually has***, so a readout naming one is saying what the build can pay rather than what a shopkeeper would recognise.

### Why these five

The three sinks are deliberately asymmetric, and the asymmetry is the whole design:

- **Food fails fast and hard.** A daily Need, and unmet Sustenance degrades a Household quickly. This is the chain that produces crises.
- **Consumer Goods fail slowly and softly.** Unmet Satisfaction erodes over Days and eventually produces a housed **Departure**. This is the chain that produces decline.
- **Materials are not a Need at all — they gate construction.** A Materials shortage means zoned Lots stay empty and the city simply *stops growing*, with no citizen unhappy anywhere.

That third one is the most interesting and it is the reason Materials shares a chain with Consumer Goods. It welds the supply-chain pillar directly to the zoning pillar: the industrial economy is not a side system running in parallel to growth, it is the thing that *permits* growth. A player who ignores industry finds their residential zones stubbornly vacant and no obvious culprit — until they open the overlay.

### Where the two extraction chains get their land

Both extraction steps draw on ground that the player's own growth consumes — see [`adr/0022`](adr/0022-land-is-a-stock-the-city-spends.md).

**Produce comes from Fertility**, which every Cell starts with and which development degrades: `base fertility − Sealing − pollution` ([`adr/0155`](adr/0155-base-fertility-is-ruleset-data-keyed-by-terrain-type-and-the-old-name-invented-a-field.md), ~~`terrain suitability`~~). Farms are therefore pushed outward as the city grows, and far enough out they fall out of the city's commute shed and become their own Settlement.

**Timber comes from Woodland**, the one thing the generator actually places. Clearing it is a one-time harvest that leaves fertile ground behind, and it regrows slowly on unsealed, unoccupied land — so the logging frontier migrates outward on its own, through ordinary Building decline rather than a system built for it.

**Both ratchets converge on the same macro-arc, and it is intended**: a mature city is a **net importer of Food and Materials**, its growth costs rising with its size. The two stay asymmetric, which is what stops it being one pressure wearing two hats — Food import is an *operating* cost scaling with population, Materials import is a *capital* cost scaling with ambition. Materials imports are therefore a soft, emergent brake on growth, where SimCity used an arbitrary cap.

The endgame must offer ways to restart the core loop rather than only bills. **Replanting** — designating land for reforestation and paying for accelerated regrowth — is the first, and finding others is open work.

### Resource discipline

**Resist Good number six.** Eickhoff's shipped Citybound enum was roughly ten entries with more deliberately commented out, and his own conclusion was that absolute-amount resources make balancing very hard and amplify bugs. Each additional Good multiplies the balance surface and adds a chain the player must hold in their head.

The taxonomy is a ceiling, not a starting point. If a sixth Good is ever added it should replace something rather than extend the list.

---

## 2. Goods are absolute; Needs are relative

A split taken deliberately from two references, because they are right about different things.

**Goods live in integer Bins** — a `long` in `[0, capacity]`, no floats, no continuous flows. (**Money is a Resource too, and its Bin is unbounded**: a ceiling on money is a warehouse limit on a balance, and the loader refuses an authored one. `adr/0024`. *Unbounded* is `long.MaxValue` and is named as a ceiling rather than denied — [`adr/0065`](adr/0065-a-bin-holds-a-long-and-unbounded-names-a-ceiling-whose-approach-is-a-defect-rather-than-a-refusal.md).) The range is what every *write* respects; a Bin can sit **above** its ceiling when a reload lowers one, and it drains back rather than being clamped, because clamping would destroy Goods and conservation is the whole point ([`adr/0064`](adr/0064-a-bins-capacity-is-a-property-of-the-ruleset-in-force-and-an-over-full-bin-drains-rather-than-clamps.md)). Necessary for supply chains to be conserved and auditable. If a hundred units of Food entered the District, a hundred units must be accounted for. This is GlassBox's model and it is genuinely excellent.

**Needs are relative scalars where 0 is ideal.** A Household's Sustenance is not a stockpile; it expresses *how well is this Household doing*. This is Citybound's model, and his warning about the alternative is a balancing point rather than a performance one — absolute Need amounts make the system unstable and amplify bugs.

The conversion happens at the shop counter: a Household buys Food (an integer leaving a Bin) and its Sustenance moves toward zero (a scalar).

---

## 3. Movement

Per [`adr/0013`](adr/0013-goods-are-pooled-within-a-district-and-shipped-between.md):

- **Within a District**, Goods flow through an abstract **Pool** subject to connectivity. No vehicle is simulated.
- **Between Districts, and to or from Outside Connections**, real vehicles carry real cargo as **Shipments**, and contribute real congestion.

"Abstract Pool" does not mean connectivity is ignored. A District whose internal Road Graph is broken must still fail to distribute — otherwise the abstraction is a lie rather than a simplification.

The transport layer is **swappable per Good**, so the boundary can move once there is profiling data rather than being frozen by this document.

---

## 4. Prices and market clearing

Prices are **not** set by the player and **not** authored in the Ruleset. They emerge from the ratio of a Good's Pool level to its recent consumption rate, adjusted incrementally each Day — a damped tâtonnement rather than an instantaneous equilibrium solve.

Three properties matter more than realism:

- **Damped.** An undamped price signal produces the same oscillation pathology as undamped congestion feedback: everyone piles into the profitable Good, it crashes, everyone piles out. Price moves by a bounded amount per Day.
- **Local.** ~~Prices are per-District, because Goods are pooled per-District.~~ ⚠ **The premise went 2026-08-22 and the conclusion stands** ([`adr/0139`](adr/0139-a-district-pool-is-a-market-and-not-a-store-so-stock-stays-with-the-seller.md)): Goods are **not** pooled per-District — stock stays with the seller — so **prices are per-District because a buyer may only reach the sellers its District’s connectivity reaches**, which is `adr/0013`’s reach argument doing the work the pooling was credited with. 🔴 ⚠ **A price does NOT sit on the seller and this sentence has said so since 2026-08-22.** `adr/0139`'s *Consequences* say the `Price` field moves to the seller; milestone 12 task 6 put it on the **market row** — `Space.DistrictPoolTable.Price`, keyed by `(District, Good)` — and `CONTEXT.md` records that reading. ***So every seller in a District charges the same number***, and until price formation ships that number is the import ceiling, which is what the shipped files stating no `[market]` already do. ⚠ **The contradiction is filed in [`plans/0012`](../plans/0012-corpus-audit.md) rather than resolved here**, and it is load-bearing: [`adr/0167`](adr/0167-a-purchase-picks-its-seller-by-a-draw-and-waits-on-the-market-rather-than-on-a-shop.md) chose a **draw** over *buy from the cheapest* precisely because there is no dispersion for *cheapest* to read, and that record is retired the day per-seller prices arrive. ***This bullet’s own reasoning was circular and it is the sentence that made it so.*** Two Districts of the same city can have different Food prices, and the gap is what makes inter-District Shipments profitable. That is the mechanism that makes the tiered transport model *economically* motivated rather than merely a performance trick.
- **Bounded by import.** The Outside Connection price is a ceiling. No local shortage can drive a price past the cost of shipping it in, which prevents runaway spirals and gives the player a reliable, expensive escape hatch.
- 🔴 **Carriage is not free — added 2026-08-22 by [`adr/0133`](adr/0133-a-pool-draw-pays-for-its-haulage-so-the-boundary-is-a-gradient-and-not-a-cliff.md).** A Pool draw gains a **haulage charge**, so moving a Good across a District costs something even though no Vehicle is created and no routing query is issued. [`adr/0013`](adr/0013-goods-are-pooled-within-a-district-and-shipped-between.md) is not reopened: its case was a *simulation-budget* argument about query volume, and it never claimed carriage was worth nothing — **free-inside was the default reading of a budget decision, not a finding.** What that reading left is a discontinuity with no physical referent: 100 m across a boundary is a Shipment, 1.45 km inside it is nothing. ⚠ **The charge is a cost INPUT and not the price** — this section's opening sentence stands unchanged, the price is still emergent and still neither the player's nor the Ruleset's. ⚠ **Form and value are UNSET**; the leading candidate scales the charge with the District's own extent, which would make `CONTEXT.md`'s extent bound **self-enforcing** rather than authored. 🔴 **A payee is a blocker on shipping it**: a cost with no counterparty destroys money, which [`adr/0024`](adr/0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md) forbids.

🔴 **Milestone 12 is where all three arrive, and it authors the anchor — added 2026-08-22 by [`adr/0135`](adr/0135-a-market-needs-two-sides-so-twelve-ships-a-provider-and-the-price-moves.md).** `[[hinterland]]` gains its **price per Good** there, because 12 is the milestone that reads it ([`adr/0131`](adr/0131-the-gate-carries-people-and-the-money-they-hold-and-a-hinterland-field-lands-in-the-milestone-that-reads-it.md)), and the recompute runs on a `Ticks.PerDay` boundary — ⚠ **with no dependency on milestone 18's Day wheel**, since a Day boundary is already computed without one. ⚠ **The anchor is authored and dynamic but EXOGENOUS**: Hinterland prices drift (§Open questions 7, closed) and move under shocks, and they are never derived from the city's — ***a ceiling derived from what it bounds bounds nothing***, which is `adr/0050`'s runaway. 🔴 **12 also ships a second `[[building]]` kind, a Provider**, because with one kind nothing sells into the Pool and this section's formula runs on **two zeroes** — *Local* and *Damped* both have nothing to act on, leaving *Bounded by import* alone. ⚠ **The *two Districts can have different prices* clause stays unobservable at 12**: [`adr/0134`](adr/0134-a-district-is-a-centre-and-its-basin-so-the-count-follows-centres-and-not-a-ceiling.md) gives one District per world until a Ruleset authors two centres, and ✅ **`rulesets/twinned.toml` is that Ruleset as of 2026-08-22** (milestone 12 task 1). ⚠ **The clause is therefore observable on ONE shipped world and on no other**, and it is the world's second lattice that makes it so rather than anything about the price. ⚠ **AMENDED 2026-08-22 by milestone 12 task 3: it is one shipped world and ZERO others, not one and nine ones.** The derivation runs only where the Ruleset states `[districts]`, and `twinned.toml` is the only file that does — so the other nine produce **no** District rather than one. `adr/0134`'s *one District per world* was written before the table had a polarity; the reason absence means none is `adr/0052`, since the prominence threshold is hash-bearing and unratified and must not be defaulted into the binary. **`Scope.Pool` still throws**, so nothing reads a District on any world.

✅ **THE ANCHOR AND THE TÂTONNEMENT SHIPPED 2026-08-22, milestone 12 task 6, and two of this section's three properties are now code.** `[[hinterland]]` gains **`prices`**, an inline array of tables giving one price per Good per edge; the ceiling for a Good is the **minimum across every declared Hinterland**, which is derived rather than chosen because `adr/0135` ships **no haulage term at 12** — with carriage free every gate is equidistant and a city buys at the cheapest. A new **`[market]`** table holds the damping, two keys and neither of them a price: `decay_percent` and `move_cap_percent`. **A Pool opens at the ceiling and moves from there**, recomputed on a `Ticks.PerDay` boundary from the Pool's level against a smoothed consumption rate — target `ceiling / cover`, where cover is how many Days the standing level lasts at the standing rate. ⚠ **Omitting `[market]` means every trade clears at the ceiling for ever**, which is the city the other ten shipped files have, so the key moved no State Hash but `twinned.toml`'s.

🔴 ⚠ **AND IT IS INERT ON EVERY WORLD THAT EXISTS, exactly as this section's own *two zeroes* warning said it would be.** Nothing writes the consumption bucket while `Scope.Pool` throws, so every rate is zero, the recompute reads that as *no trades*, and every price sits at the ceiling it opened at from Tick 0 to the end of the run. ***That is the third time this corpus has shipped a producer with no consumer*** — milestone 9's land value and milestone 12's Pool Bins being the other two — and it is accepted for the reason it was the last two times: **task 7 supplies the writer**, and a purchase settling at a price needs the price to already exist. ⚠ **So the *two Districts can have different prices* clause is still unobservable**, and now for a second reason on top of the one above.

⚠ **A file that states `[districts]` and leaves a `good` unpriced is REFUSED AT LOAD**, which is new and is not a range check. A District opens a Pool per Good and the Hinterland's price is the **only** ceiling on it, so an unpriced Good is not merely unanchored — ***it is free everywhere, for ever***, which reads as a balance problem rather than as a missing key. `adr/0048` holds the count and the enumeration.

Businesses decide to produce, expand, or close using the same **satisficing** logic Households use for everything else — see [`adr/0017`](adr/0017-agents-satisfice-they-never-optimise.md). A Business does not solve for optimal output; it notices its margin is bad and considers a small number of known alternatives.

**A Business enters the city by two channels, and leaves by one.** ⚠ **This paragraph describes a mechanism that is UNBUILT at the time of writing** (`adr/0070`): [`adr/0145`](adr/0145-a-business-is-founded-by-a-household-or-arrives-through-a-gate-and-both-land-in-the-pool.md) decides the shape and milestone 27 task 8 builds it. A **Citizen founds** one — spending their own labour, and their **Household's** money, because a Citizen has no Bin and a Household has one ([`adr/0146`](adr/0146-founding-costs-a-citizen-and-the-households-money-so-the-founder-is-the-first-worker.md)). It is a **transfer**, so nothing is issued; or one **arrives through a gate** from a Hinterland carrying an authored band, which is the Outside Connection paying in and is what [`adr/0024`](adr/0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md) already licenses. ***Both create it UNPREMISED***, into the pool, and **neither tenants anything** — placement is what puts an occupant into standing stock, and [`adr/0069`](adr/0069-placement-is-a-mechanism-of-its-own-and-construction-houses-nobody.md)'s *construction houses nobody* holds on the commercial side exactly as it does on the residential one. The exit is [`adr/0142`](adr/0142-an-unpremised-business-emigrates-so-the-sink-is-the-one-households-already-use.md)'s and is **built**: a Business nothing tenants waits out a give-up bound, then leaves and takes its money with it.

⚠ **Two channels rather than one, for the reason §5 already gives about skilled labour** — *Sorting is the fast channel and schooling is the slow one.* Neither works alone here: a city whose Households hold nothing would found no shops and have no lever, and a gate-only city would find commerce something the outside grants rather than something prosperity produces. 🔴 **Three numbers are owed and none is chosen** — a founding band, an arrival band, and a founding **rate** — each hash-bearing and each owed a named ratifier ([`adr/0052`](adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)); [`plans/0002`](../plans/0002-open-questions.md) §D2 holds them. ⚠ **The founding trigger must not become a demand scalar**: there is no RCI meter, and if the only honest condition for *which Household founds* needs aggregate unmet demand, `adr/0145`'s own first revisit trigger says withdraw the channel rather than fudge it.

**A shop takes premises the way a family takes a home, and they take them from the same pool of room.** ⚠ **UNBUILT until milestone 27** (`adr/0070`), then built: [`adr/0147`](adr/0147-a-business-takes-premises-by-placement-and-one-ceiling-counts-both-kinds-of-tenant.md). A Business waiting in the unpremised pool is offered premises by **placement** — the same mechanism, trigger and patience a Household gets, which is [`adr/0069`](adr/0069-placement-is-a-mechanism-of-its-own-and-construction-houses-nobody.md)'s *construction houses nobody* holding on the commercial side. ⚠ **A Building's `occupants` counts tenants of ANY kind** ([`adr/0141`](adr/0141-a-tenant-owns-what-leaves-with-it-and-the-premises-own-the-capacity.md)), so a dwelling that admits three holds three families, or two families and a shop. ***The consequence is the point: a city that fills with shops houses fewer people, from one number, with no rule expressing it.*** 🔴 **And a shop that loses its premises — its Building condemned — goes back to the pool and looks again**, which is [`adr/0144`](adr/0144-a-tenant-that-loses-its-premises-keeps-only-its-money-and-waits-a-households-wait.md) and is measurable: on `rulesets/founded.toml`, **69 shops took premises over a run and 31 of them lost those premises to condemnation.**

**And a premises kind may come with its trade, so a shop need not have been founded to exist.** [`adr/0148`](adr/0148-a-premises-kind-may-declare-its-trade-and-instantiating-one-is-not-housing-anybody.md). A `[[building]]` names one `[[business]]`, and raising a Building of that kind **instantiates** it, already premised, in one of the `occupants` slots above. ⚠ **It is drawn from no pool**, which is why this is not [`adr/0069`](adr/0069-placement-is-a-mechanism-of-its-own-and-construction-houses-nobody.md)'s *construction houses nobody*: that rule protects a **demand signal** from being drained by creation, and nothing here is taken out of either pool. ***It is what every shipped Ruleset was already doing under the name `jobs = 8` on the dwelling*** — `minimal.toml` called it *living above the shop* and said in its own comment that it stood in for a workplace kind nobody had written. ⚠ **The shop it makes carries no founder and no flag**: [`adr/0146`](adr/0146-founding-costs-a-citizen-and-the-households-money-so-the-founder-is-the-first-worker.md) governs founding, nobody founded this, and condemn its premises and it waits in the pool like any other. 🔴 **What it costs is a slot**, so a kind that comes with a trade houses one family fewer than its ceiling — and anything sizing a city must ask **how many Households fit**, not how many tenants do.


**Founding costs a person as well as money, and the founder is the shop's first worker.** ⚠ **UNBUILT** (`adr/0070`): [`adr/0146`](adr/0146-founding-costs-a-citizen-and-the-households-money-so-the-founder-is-the-first-worker.md) decides it and milestone 27 builds the half that is reachable. **A Citizen is the subject** — one Citizen, never a Household and never a group, which is `adr/0005`'s *decisions are never shared* — and the money comes from the Household because the purse is Household-scoped throughout this document. ***The founder becomes the Business's first worker, so nothing records a founder separately***: the founder is the Citizen whose workplace is that Business. ⚠ **No `founder` column is declared, and that is deliberate** — two records of one relationship drift, and a founder handle would collide with the workplace handle the day a job points at a Business.

⚠ **The founder forgoing an income is NOT part of what milestone 27 builds, and the reason is worth stating.** A founder who takes no wage until the shop earns is [`adr/0026`](adr/0026-wages-are-posted-locally-and-never-cleared.md) running on a Business with an empty Bin — **wages are designed and unbuilt**, arriving at milestone 15, and a Bin is already the thing a blocked Rule waits on ([`adr/0114`](adr/0114-a-balance-a-rule-can-fail-on-is-a-bin-and-a-bins-owner-is-discriminated.md)). ***So the honest move is to build wages at 15 rather than approximate them at 27***, which is `adr/0070` exactly. **What 27 ships is the labour cost alone**: the founder is occupied, so the employment pass will not hire them and the city is one worker down. ⚠ **That is also what makes founding a CHOICE** — the founding pass and the employment pass draw from the same unemployed Citizens, and whichever reaches one first takes them. ***Neither pass knows the other exists, and nobody compares the two***, which is [`adr/0017`](adr/0017-agents-satisfice-they-never-optimise.md).


---

## 5. Budget and taxation

All of this lives under one verb, **`Govern`** — `Fund` and `Regulate` were merged, because both are *"set a parameter on a Rule the city then obeys."* The financial levers are deliberately few: **set tax rates**, **set service funding levels**, and **borrow**. Everything place-attached can be overridden per District; only borrowing is irreducibly global, since the city has one balance sheet.

| Lever | Direct effect | Second-order effect |
|---|---|---|
| Residential tax rate | Revenue per Household | Affordability, which feeds the fertility decision in [`adr/0011`](adr/0011-household-life-stages-and-self-generating-population.md) |
| Commercial / industrial tax rate | Revenue per Business | Business viability, and therefore jobs |
| Service funding | Service quality and catchment | Attractiveness, and therefore immigration |
| Debt | Immediate capital | Interest as a standing drain |

**Two entries were missing from this document entirely and are the largest items in a real city budget: capital expenditure and maintenance.** Neither appeared anywhere in the corpus — not here, not in `CONTEXT`, not in any ADR. Everyone assumed them; nobody wrote them, which is the failure mode session six named about the unratified 10k figure, one step worse. Both are settled in [`adr/0035`](adr/0035-infrastructure-is-priced-by-what-it-consumes.md):

| Claim on the treasury | What it is | Scales with |
|---|---|---|
| **Capital** | building Infrastructure — roads, Junction pieces, right-of-way, plants | Lane-Tiles and discrete pieces. Also draws **Materials** and **Land** |
| **Upkeep** | `construction cost ÷ effective life`, drawn per Day | a **base term** from design life plus a **wear term** from Segment volume |

**Upkeep is drawn automatically and there is no maintenance funding lever**, because a slider whose only sensible setting is *as high as affordable* is not a decision. What the player controls is a **rebuild threshold**, which is a Policy rather than a fifth lever: infrastructure decays slowly and then rapidly, so an early cheap treatment beats a late reconstruction, and the threshold is **District-overridable**. That is what makes it a real choice — preservation needs capital sooner and more often, so committing to it everywhere is impossible and the player must decide which corridors to keep. Deferring, and betting on future revenue, is an equally coherent play style; **the bet is settled by the demographic transition**, since revenue growth slows at the crossover.

**Borrowing is a player action, not an automatic overdraft.** The treasury genuinely empties, and the Rules that could not draw wait — see [`adr/0024`](adr/0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md) and [`adr/0035`](adr/0035-infrastructure-is-priced-by-what-it-consumes.md) §3a. For roads that means capacity and free-flow speed fall, so **an unpaid bill lengthens every commute** — the first mechanism coupling `01-player §5`'s Bill and Clock axes.

**And a fourth second-order effect, which conservation supplies for free.** Under [`adr/0024`](adr/0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md) a § spent on a road is not destroyed — it becomes somebody's income. Construction consumes Materials, and where those Materials came from decides where the money lands: **bought from local Processing it becomes local wages; imported, it leaves through the gate.** So public construction is a **stimulus** or a **leak**, and *a city that builds while importing all its Materials is exporting its own stimulus.*

That is a new argument for the domestic Materials chain, and it is an **economic** one where every previous argument for it was logistical. It also gives the endgame in `adr/0022` a second edge: the mature city that has sealed its land imports Materials to maintain the very network whose upkeep is now its largest standing bill.

The second-order column is where the game is. **Raising residential tax is not simply a revenue decision — it is a fertility decision**, because affordability drives internal generation. A player squeezing revenue to fund services makes the city more attractive to immigrants and less able to reproduce itself. Both channels are visible separately (Pool size versus Replacement Rate), so this is a legible trade rather than a hidden one.

**A third second-order effect arrives with conserved Money: tax rates are a velocity control.** Taxes are the *only* mechanism converting private money to public, and the treasury spends immediately where a Household might have saved. So a tax rise is simultaneously **contractionary** (less private consumption) and **expansionary** (more public wages), and which dominates depends entirely on *whom* you taxed. Nobody authored that; it falls out of conservation.

Service funding is likewise not only a coverage lever. Service Buildings employ Citizens and the treasury pays them, so **cutting funding fires people** — less public consumption, thinner business margins, fewer private jobs. Austerity in a struggling city makes it more struggling, by a chain the player can trace.

What service funding **cannot** do is absorb unemployment, and the block is structural rather than punitive: public jobs are **demand-determined**. A school needs teachers in proportion to the children in its catchment. *You cannot fix unemployment by hiring everyone as a teacher, because the number of teachers is set by the number of children.* See [`adr/0026`](adr/0026-wages-are-posted-locally-and-never-cleared.md).

There is **no calendar**, so there is no annual budget cycle. Revenue and expenditure accrue per Day. See [`adr/0010`](adr/0010-one-clock-and-demographics-by-sorting.md).

---

## 6. How a shortage becomes an unhappy person

This is the chain the whole economy exists to produce, and every link must be individually inspectable through **Evidence**.

1. A farm's output falls, or a Shipment cannot get through, or a Materials plant closes.
2. The District's Food **Pool** drains. The price rises until it hits the import ceiling.
3. A grocery's Food **Bin** empties. Its shelves are visibly empty — the player can *see* this before any number changes.
4. A Household **travels** to a shop on its **Provider List** and finds nothing. The visit is a **Trip** ([`adr/0067`](adr/0067-a-shopping-attempt-is-a-trip-and-a-household-tries-one-provider-per-occasion.md)) — the same rule [`adr/0032`](adr/0032-services-are-delivered-by-trips-not-by-coverage.md) already applies to Services, and Goods have the stronger claim, because a grocery's Bin is a physical stock in a place. This is a recorded failure, not a silent one — recorded on the **Household**, as a count of consecutive failed occasions and a refusal reason, and never as a `Trip Fate`, because the journey *completed*; what failed is the transaction.
5. The Household tries a different entry **at its next shopping occasion**, not this one. `BOUNDED KNOWLEDGE` — it does not scan the city, it checks the places it knows, **one per occasion**. Which one is decided by a cursor that advances on failure and resets on success, so a provider that failed is skipped for exactly one occasion and the list stays *sticky* in `adr/0017`'s sense. **A failed occasion therefore costs one Trip, never one per entry**: consulting the whole list in a single occasion would multiply the city's shopping traffic by the list's length at exactly the moment the city is already failing, and the resulting shortage → Trips → congestion → Trip failures → Failure Pressure loop would be an amplifier nobody chose. The cost of bounding it is **lag** — a Household with three known groceries takes three occasions to discover its District is dry — and the lag is realism.
6. If none can supply, the Household's **Sustenance** Need degrades. **The Need is the accumulator** — a relative scalar that falls while unmet and recovers when met — so a dry afternoon and a dry month are one mechanism at two depths, and nothing else in this chain has to remember anything.
7. At its next re-evaluation the Household compares where it lives against a **Hinterland** and leaves if the Hinterland scores better: a housed **Departure**, counted and attributed to the term that dominated ([`adr/0102`](adr/0102-a-housed-departure-is-a-comparison-the-household-re-runs-not-a-threshold-it-crosses.md)).

**Step 7 is `02 §5.4` firing, not a mechanism of its own, and there is no threshold underneath it.** An earlier draft read *"sustained degradation produces a housed Departure"*, which names a duration — and a duration crossing a line is [`adr/0053`](adr/0053-failure-pressure-is-a-duration-not-a-tally.md)'s Failure Pressure, the **Building** mechanism, whose whole argument is that abandonment has no actor. A Household has one. `CONTEXT.md` had already settled this while refusing something else: *"tolerance is emergent — a Household compares the city against a Hinterland using the same utility function everyone uses, and leaves or does not."* Three mechanisms, three jobs: the **Need** accumulates, the **comparison** decides, and a **cadence** — an Event Wheel countdown, or immediately on a failed occasion — decides only when the Household looks. **What a cadence bounds is perception, not response** (`01 §6`), so it must never be read as the duration in disguise.

**Only two things produce a housed Departure**, and the second arrived from another document: this chain, and [`adr/0095`](adr/0095-a-commute-budget-is-three-rungs-and-only-the-last-one-refuses.md)'s **unsavoury** commute rung. A Citizen who cannot reach work *at all* is not a third — that is destitution, which `CONTEXT.md` → Unemployment calls *"a reachability failure wearing a money costume"*, and it is the **Destitute** channel's. The Commute Budget is therefore one ladder feeding two channels by which side of the ceiling a Citizen lands on.

**Every step names its constituents.** The aggregate "Food shortage in Riverside" opens into the specific groceries with empty Bins, which open into the specific Households that walked in and found nothing, which open into the specific Departures that followed. `LEGIBLE CAUSE`

The step that most often gets skipped in other games is 4. A Household must *actually attempt* the purchase and *actually fail*, because that failure is the evidence. A global happiness number computed from a global stock level would produce the same aggregate and answer no questions at all.

**That commitment has a price and it is named rather than hidden.** Since step 4 is a Trip, every Household's shopping is **Trip generation** — so this chain's cost lands in milestone 5b's budget rather than in an economy row of its own, and the multiplicand nobody has is *shopping occasions per Household per Day*. `adr/0067` types the two halves apart deliberately: **that the attempt must be real** is a design commitment this document takes, and **that the city can afford it at a million people** is a measurement that could still force a choice between the Evidence chain and the Tick budget.

---

## 7. Jobs

Employment is the other half of the economy and it runs on the same machinery. A Business posts a number of positions **and a wage**; a **Citizen** takes one of the **known** workplaces it can reach inside the **Commute Budget**; a Business that cannot fill its positions has **two** levers and not one.

**Three corrections to the sentence above, each owed to a decision taken after it was written.** The wage is [`adr/0026`](adr/0026-wages-are-posted-locally-and-never-cleared.md)'s — posted locally, adjusted by the Business's own fill rate, anchored to the **Hinterland** wage and never cleared by a market — and this section had described employment without mentioning money at all. The chooser is a **Citizen** and not a Household: a job has exactly one holder, so employment is a Citizen's and the Household's money is what follows from how many of its members earn (`CONTEXT.md` → Unemployment, milestone 5b-bis). And *produces less* is only the second of `adr/0026`'s two levers — **pay more, or be smaller** — which is why *"a labour-starved district does not watch its businesses die, it watches them get smaller."*

Two consequences worth stating:

- **Jobs are the demand-side link between industry and housing.** Residential Zone Rules test for reachable jobs, so an industrial collapse shows up as residential vacancy a few Days later.
- **Education gates positions, and a city educates its own.** Higher-value Businesses want a **mix** weighted toward the upper tiers (`§1`, *the employer that produces no Good*), and a Citizen reaches the top tier only through schooling. Skilled Citizens arrive by **Sorting** — attracted by school provision, `adr/0010` — and are also **produced here**: when a Household's Life Stage advances and a new Young Household forms, its adults' Skill Tier is read off the **Education** Need that Household accumulated while it had children ([`adr/0104`](adr/0104-a-skill-tier-is-earned-by-attendance-and-the-credential-stays-a-wall.md)). So Sorting is the **fast** channel and schooling is the slow one, and the payoff for a school arrives one Life Stage later — the slowest feedback in the game, and the right one to be slowest.
- **Experience carries 1 → 2 and never reaches 3.** A Citizen who missed schooling still climbs to tier 2 by working and takes longer over it; the credential boundary is a **wall**, because if time alone reached the top tier a patient player would never need to build a school at all. The loop that closes — schools → tier 3 → Office → exports — is damped by `adr/0032`'s own corollary rather than by tuning: *good schools raise land value, which prices out the Families the school was built for*.

---

## 8. Open questions

1. ~~**Do Businesses hold money, or only Bins?**~~ **Closed by [`adr/0024`](adr/0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md), which says so in its own words** — *"Closes the open question of whether they hold only Bins."* Businesses hold a **balance**, one integer per Household and per Business, and hold no Needs; Business as a full economic actor with preferences is explicitly out of scope. Struck rather than corrected, per session nine. **It sat here as open after the ADR that closed it**, in the document the ADR is about, which is the granularity defect `plans/0000` names — a settled decision is not findable if the document it settles still asks the question.
2. ~~**A Bin Rule term is a Ruleset constant, and a price is not.**~~ **Closed by [`adr/0050`](adr/0050-crossing-an-ownership-boundary-is-a-trade-and-payment-is-implicit-in-the-scope.md).** A scope answers *whose is it*, not *where do I look*: a term crossing an **ownership boundary** is a **trade**, and **no payment is ever authored in a Rule** — the price is emergent by `§4`, the quantity is the term's `amount`, the counterparty follows from the scope. So `amount` stays a fixed integer permanently and the District **Pool is a market**, not a wider Bin lookup. The Hinterland gained a **price per Good** to be the anchor `adr/0026` already claimed it was. Opened and closed in one session, before slice 7 task 7.
3. ~~**Can an authored money term and an implicit trade payment both exist?**~~ **Closed while fixing the leak in [`02 §4.3`](02-simulation-model.md#43-format).** They can, because they are different shapes rather than two spellings of one. An explicit money term is a **transfer** — a tax, a wage, a subsidy — which names both ends and therefore balances inside its own atomic Rule; `adr/0050`'s payment is a **purchase**, whose price is emergent and whose counterparty is implied by the scope, so there is nothing to author and nothing to collide with. `adr/0050`'s *no syntax for payment* was always a claim about purchases. The Ruleset loader now enforces the transfer half mechanically: a Rule whose money terms do not sum to zero is refused **in either direction**, since destroying money and creating it are one defect with the sign flipped. That refusal only became writable once a `[[resource]]` declared its **family**, which is `CONTEXT` → Resource's first distinguishing parameter and was missing from the Ruleset entirely.
4. **Is there a labour market price?** Wages responding to scarcity would close the loop between job shortages and household income, but adds a second tâtonnement running against the goods one, and two coupled price systems is where economic models become unstable.
5. **How does construction consume Materials?** Whether a building under construction draws down the Materials Pool over several Days or in a single transaction changes how visible a shortage is and how sharply growth stalls.
6. **Does industrial pollution use the same Map Layer machinery as everything else?** Assumed yes, but the interaction between a Good's production rate and a layer's emission rate has not been specified.
7. ~~**Should Outside Connection prices drift?**~~ **The mechanism is settled; the tuning is not.** [`adr/0023`](adr/0023-immigration-arrives-through-the-gate.md) makes Goods prices, wages and rents outside **one object per edge**, so a drifting price has somewhere to live and cannot contradict a second system — and [`adr/0050`](adr/0050-crossing-an-ownership-boundary-is-a-trade-and-payment-is-implicit-in-the-scope.md) gives the Hinterland a price per Good. What is still open is only **whether the drift is switched on and at what rate**, which is the economic **shock** layer in [`01-player-experience.md`](01-player-experience.md) §5 and is a playtest question rather than a design one.
