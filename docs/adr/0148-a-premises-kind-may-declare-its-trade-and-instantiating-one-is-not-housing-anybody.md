# A premises kind may declare its trade, and instantiating one is not housing anybody

**A `[[building]]` kind may name one `[[business]]` trade, through a new `business` key. When a Zone
Rule raises a Building of that kind, construction **instantiates** a Business of that trade, already
premised, occupying one of the kind's `occupants` slots. It is an ordinary Business from that instant:
no founder, no capital and no flag. **When those premises come down it goes with them**, because a
source needs the sink that inverts it and not a timeout. ⚠ **Which Business that is, is recorded on the
Business as `Origin`, a handle naming the premises** — *amended 2026-08-24 by milestone 27 task 10,
because this ADR shipped identifying it by its TRADE and a kind is not an identity. See the banner
below.* And `[[building]] jobs` and its Shift band are
**removed and refused at load** —
[`adr/0141`](0141-a-tenant-owns-what-leaves-with-it-and-the-premises-own-the-capacity.md) gave those to
the trade, and a key nothing reads is worse than a key that is gone.**
`SOLVE THE ACTUAL PROBLEM` `LEGIBLE CAUSE` `EMERGENCE`

⚠ **This exists because task 7 emptied every shipped city and the plan had predicted it.**
[`plans/0041`](../../plans/0041-the-business-is-a-thing-the-city-contains.md) task 7 wrote it out in
advance — *"employment goes to zero, every commute stops, and the traffic, parking and commute suites
go with it"* — and held that task 8's founding pass discharged it. **It did not.** Task 8 put founding
in the *build*; it put a Business in exactly **one** of fifteen shipped worlds. Sixty-six assertions
failed on one sentence: ***nobody is employed anywhere.***

## Why

### `jobs = 8` on `dwelling` was always this decision, taken silently

`rulesets/minimal.toml` states the whole of it in its own comment, and states it as a stand-in:

> *"IT IS ON THE DWELLING RATHER THAN ON A WORKPLACE KIND, AND THAT IS THIS FILE'S FIRST LINE BEING
> OBEYED RATHER THAN A SHORTCUT. … Living above the shop is the smallest arrangement in which the
> assignment pass has somewhere to send anybody."*

***The shop was already in the premises kind.*** It had no name, no row and no balance, so nothing
could point at it — which is why `adr/0141` moved employment to the trade in the first place. This ADR
does not add a shop to the shipped cities. **It gives the one that was already there a row**, and
`LEGIBLE CAUSE` arrives exactly where `minimal.toml` said it was missing: a Citizen's employer stops
being *the building* and becomes *the bakery on its ground floor*.

### `adr/0069` is not violated, and what it protects is the reason

[`adr/0069`](0069-placement-is-a-mechanism-of-its-own-and-construction-houses-nobody.md) says
construction houses **nobody**. Its argument is about a **signal**: *"creation drains the signal that
authorised it, so no Ruleset can build past its demand however wide its sample"* — placement runs
first, so a Household still in the Pool when a Zone Rule samples is one **the standing stock could not
house**, which is the residual `02 §5.2` calls the demand signal.

***An instantiated trade is drawn from no pool.*** It is not a Business that was looking for premises
and found them. It is made at the instant the Building is, from the same `[[building]]` declaration and
by the same call that makes the Building's Bins and its Rule Instances. **Construction has always
instantiated what the kind declares; this adds one more thing a kind may declare.** The Unplaced Pool
is untouched, the unpremised pool is untouched, and no signal is drained by anything.

⚠ **The counter-sentence is real and it is about a different act.** `plans/0041` task 8 says *"A Zone
Rule that auto-tenants a shop is that rule broken on the commercial side."* **Auto-tenanting takes a
shop that already exists out of the unpremised pool at the moment of construction** — which drains
precisely the pool [`adr/0147`](0147-a-business-takes-premises-by-placement-and-one-ceiling-counts-both-kinds-of-tenant.md)'s
placement pass exists to drain, and would let a Ruleset build past the demand for premises. ***This
creates rather than draws, and the distinction is the whole of the argument.*** If it ever stops being
observable — if some pass starts satisfying a kind's declared trade *out of the pool* — this ADR is
wrong and `adr/0069` is right.

### The sink is demolition, and it was found by measurement rather than argued

🔴 **The first version of this ADR said a kind-declared shop lands in the unpremised pool like any
other, and that was an `adr/0006` violation.** Measured on `rulesets/minimal.toml`, which condemns
every dwelling it raises:

| Tick | Buildings | Businesses | Unpremised pool |
|---:|---:|---:|---:|
| 0 | 121 | 121 | 0 |
| 8,192 | 57 | 340 | 221 |
| 16,384 | 61 | 594 | 444 |
| 32,768 | 58 | 1,095 | 907 |

***Construction created one shop per Building and demolition destroyed none***, so the count rose with
elapsed time for ever — and the Unplaced Pool rose with it, because re-premising put those shops into
the housing slots of Buildings that already had their own.

⚠ **`gives_up_after_days` does not close it, and it is worth saying why**, because the bound was the
obvious answer and it is the wrong shape. `rulesets/founded.toml` declares one and reached **1,275**
Businesses on the same run. ***A bound drains a stock at a rate; this is a source with no matching
sink***, and `adr/0006`'s rule is about the pairing rather than about the eventual level.

**So the trade a kind declares dies with the premises**, which is `Fit`'s inverse exactly: construction
instantiates one, demolition destroys one. With the pairing in place the same run holds
Businesses **equal to Buildings** at every reading, an empty unpremised pool, and an Unplaced Pool that
is flat instead of climbing.

> 🔴 **AMENDED 2026-08-24 by milestone 27 task 10, and the paragraph below is the sentence that was
> wrong.** ***A kind is not an identity.*** `[founding]` draws uniformly over **every** declared trade,
> so a Household may found a shop of the very trade a dwelling declares — and on the two shipped files
> that found anything, `founded.toml` and `levied.toml`, the founded shop and the instantiated one sat
> in the same Building's list, indistinguishable, and **demolition razed whichever came first.**
>
> **Two defects from that one line, and they point in opposite directions.** The founded Business's
> capital left the city through `Raze`'s money-supply write — measured at **23,983 of 354,562 per
> 20,480 Ticks**, and `founded.toml`'s header had been reading that drain as its own designed leak.
> And the instantiated Business **outlived its premises** into the unpremised pool, where nothing ever
> collected it: 52 stranded on `levied.toml` at 24,576 Ticks, against **zero** on `minimal.toml` and
> `taxed.toml`, which found nothing. ⚠ **Both are this section's own `adr/0006` argument reopened by
> the mechanism the next ADR added**, which is why the long run found it and no test did.
>
> **The repair is `BusinessTable.Origin`** — a severable `Handle<Building>` naming the premises that
> instantiated this Business — and `Fit` and `DestroyBuilding` now agree on the same row at both ends.
> ⚠ **It is not the flag this paragraph refused, and the difference is not a quibble**: a bit saying
> *I was instantiated* travels with the Business into whatever premises it is later placed in and gets
> it razed there, where a handle naming **one** Building stops meaning anything the moment it leaves.
> ***The refused thing was a property of the Business; what was needed was a property of the edge.***
>
> **Saved and hashed**, so every golden artefact re-recorded — and both session traces moved this time
> where task 6's column moved only `world-hash.txt`, because `Fit` writes it in a session that raises
> Buildings. `adr/0100`: that cost nothing and is not a reason to have deferred it.

⚠ **It is still not a second class, because the choice is made by TRADE and not by a column.**
Demolition destroys at most one Business of the kind's declared trade and pools every other tenant
under `adr/0144`. The flag stays refused: a stored *came with the premises* bit would separate two
Businesses identical in every column, and the only case it decides differently is one where both
answers are the same.

### One class of Business, and the kind decides only how it enters

A kind-declared Business and a founded one differ in **how they arrive** and in nothing afterwards. No
column records which. A shop that walked into these premises through `adr/0147`'s pass and is
still here when they come down goes to the unpremised pool carrying
[`adr/0144`](0144-a-tenant-that-loses-its-premises-keeps-only-its-money-and-waits-a-households-wait.md)'s
balance, waits a Household's wait, and either takes premises again or emigrates through
[`adr/0142`](0142-an-unpremised-business-emigrates-so-the-sink-is-the-one-households-already-use.md)'s
bound. ***The one the kind declared does not, and the section above is why.***

***A second class would need a flag; a flag would need a rule that reads it; and there is no such
rule.*** The one asymmetry — a kind-declared shop has no founder and no capital — is not a class, it
is the same absence a Business has before its first trade.
[`adr/0146`](0146-founding-costs-a-citizen-and-the-households-money-so-the-founder-is-the-first-worker.md)
governs **founding**, and this is not founding: nobody spent anything, so nobody is owed a job by it.

### It takes a slot, and the content pays for it in the open

`adr/0147` decided **one ceiling over both kinds of tenant**. An instantiated trade is a tenant, so it
takes a slot. The alternative — a trade the kind declares being free of the ceiling a trade that walked
in must respect — is two space semantics for one table, which is the split the section above refuses.

That cuts every shipped world's housing by a third, so **`occupants` moves from 3 to 4** in every file
that declares a trade. ⚠ **The derivation is preserved rather than edited.** `minimal.toml` derives
`jobs = 8` from *three Households per Building* — 1,000/360 × 3 = 8.33, floored — and three Households
per Building is exactly what four slots minus one shop is. ***The number moves and what it means does
not***, which is the only kind of content edit that may accompany a mechanism.

### The workplace-only kind becomes expressible, and still does not ship

A kind stating `occupants = 1` and a trade is a Building that houses nobody and employs — the workplace
kind `adr/0147` refused on [`plans/0040`](../../plans/0040-the-business-is-the-actor-and-the-building-is-premises.md)
**F43**: *"no shipped Ruleset declares one … a mechanism reachable only in a Ruleset nobody has written
is a mechanism with no world in it."* **This ships none either**, and F43 stands. What changes is that
writing one becomes a **content** decision — a second `[[zone_rule]]`, a decline Rule and a land-use
split, which is what `minimal.toml` said the barrier was — rather than an engine one.

## Consequences

- 🔴 **Hash-bearing in every shipped world, by three separate routes**: a Business row per dwelling,
  `occupants` 3 → 4, and the Shift-start draw re-keyed onto the Business's monotonic id
  (`plans/0041` **G6**). **Every golden artefact re-records.**
  [`adr/0100`](0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md): that
  costs nothing while nobody is carrying a save, and is not a reason to defer, narrow or split this.
- ⚠ **The re-record must happen AFTER the content lands and not before.** Re-recording while employment
  is zero commits a jobless city as the reference, and a baseline is the one artefact nobody re-checks.
- **`[[building]] jobs`, `shift_start_earliest_hour` and `shift_start_latest_hour` are refused**, not
  ignored. Three refusals; `adr/0048`'s recount is owed on the day.
- **A kind naming an undeclared trade is refused**, and a kind declaring a trade whose `occupants` is
  zero is refused — a premises with no room for the shop it comes with is half a sentence.
- **No new `purpose_tag`.** Instantiation makes no draw: the trade is named, not sampled.
- **`RulesetShape` compares the declared trade**, so a reload that repoints `dwelling` at a different
  trade is refused rather than migrated. It is identity and not tuning, on the same footing as the Bins
  and Rules a kind declares — every standing Building would otherwise hold a shop of the wrong trade.
- **`World.DestroyBusiness` now dismisses the staff** — severing each Citizen's `Workplace` and taking
  their commute off the roster. ⚠ **That was a latent leak the handle move left**, not something this
  ADR introduced: `Depart` freed a Business row while `Businesses.WorkerHead` still threaded live
  Citizens, so a recycled row could inherit somebody else's staff.
- **`World.TryDeclaredHousing` is the distinction the build now carries.** Anything sizing a city must
  ask how many *Households* fit, not how many *tenants* do — `SyntheticCity` asked the wrong one and
  built a quarter too few homes.
- **`jobs` stops being derivable from the building kind**, so `World.TryDeclaredJobs` reads the trade
  and `WorldInvariants.cs:1014` moves with it (`plans/0041` **G7a**).

## Rejected

**Leaving `[[building]] jobs` readable as a second employer path.** It would keep every shipped world
green with no content edit at all, which is exactly why it is worth naming. ⚠ **It makes `adr/0141`
advisory** — the trade owns `jobs` except when it does not — and it requires `Citizen.Workplace` to be
a tagged handle addressing either table, which is a discriminated reference in simulation state to
avoid editing fifteen TOML files. ***A key nothing reads is this corpus's own named failure; a key two
things read is worse.***

**Founding everywhere instead.** Measured rather than argued: `rulesets/founded.toml` stands **38**
Businesses at 2,000 Citizens, so declaring `jobs = 8` on the trade gives ~**0.15** jobs per resident
against today's **0.96** — not a smaller city but a **statistically vacuous** one, in which most of the
commute, traffic and parking suites assert over single-digit samples. ⚠ **And it makes a money economy
a prerequisite of anybody being employed anywhere**, since founding spends a Household balance and
`minimal.toml`'s Households hold zero — which is milestone 15's subject arriving nine milestones early.

**The arrival channel instead.** `adr/0145`'s second door is unbuilt, and only `bordered.toml` and
`crowded.toml` have a gate for it to come through. **Unbuilt is not a design constraint**
([`adr/0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md)), and equally it is not a
mechanism thirteen worlds can use tomorrow.

**The instantiated shop being free of the occupancy ceiling.** Two space semantics for one table; see
above. It is also the shape `adr/0147` already rejected as *a separate `businesses` key*, reached by a
different road: shops and housing not competing for space.

**Several trades per kind.** A list where one name does. Nothing in the design needs a Building that
comes with two trades, and a second one can already arrive by placement into a spare slot — so the list
buys an authoring convenience and costs a shape that has to be migrated.

## What would trigger revisiting

- **A pass that satisfies a kind's declared trade out of the unpremised pool.** That is auto-tenanting,
  it drains a signal, and `adr/0069` reaches it. The argument above depends on *creates rather than
  draws* staying literally true.
- **The shared ceiling deadlocking.** Already `adr/0147`'s trigger, and instantiation makes it likelier
  by construction: every dwelling now starts one slot down. ⚠ **Measure it before believing it** — the
  `occupants` 3 → 4 move exists precisely so that it should not.
- **Founding or arrival becoming the dominant source of shops in a played city.** Then the kind-declared
  trade is scaffolding rather than a mechanism, and it should be argued as content.
- **A workplace kind shipping.** `occupants = 1` plus a trade becomes the obvious spelling, and *which
  kinds may admit a trade* stops being *any kind with room* — which is a permission question, and `Zone`
  is where this corpus puts those.
