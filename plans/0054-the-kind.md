# 0054 — The kind

**A probe of the Building kind, and the instrument it turned out to need.** Opened 2026-09-02.

---

## Why this exists

**A question about the size of the Ruleset key surface**, asked plainly: *why must every kind of
building in the game be stated explicitly, and would that not end up being thousands of buildings?*

The wariness behind it is the one worth recording: ***going far down one road and finding out at the
end, when the main game's Ruleset is written, that the shape was wrong.*** So the answer was not an
argument. It was **a fragment of that Ruleset written now, deliberately, to break things** — twelve
kinds a real city has and the demonstration files do not: house, terrace, corner shop, supermarket,
office, workshop, warehouse, primary school, clinic, church.

⚠ **The probe is not shipped and must not be.** It lives in a scratchpad, it declares content
nothing ratifies, and every number below was read off it once. What ships from this session is the
**instrument**, because the probe could not be read without one.

---

## What the key surface measured, before any of it ran

**172 keys are published in [`docs/ruleset-reference.md`](../docs/ruleset-reference.md); 138 are stated
by at least one shipped file.** Of the 34 remainder, 24 are inline-table keys a line parser misses and
are in use. **Ten are stated by nothing at all**: `[[rule]] fills`, `[needs] health_degrade` and
`health_recover`, and seven `[layers]` keys — the three shoreline terms, `sealing_decay_period` and
`_offset`, `woodland_regrowth_period` and `_offset`.

🔴 **BUT THE SIZE IS NOT THE DEFECT, AND THE MEASUREMENT THAT SAYS SO IS THIS ONE.** Of **3,199**
authored key-lines across the 32 shipped files that are not `minimal.toml`, **2,431 — 76% — are
byte-identical to `minimal.toml`.** `declining.toml` has 81 key-lines of which **79 are copy**; it
exists to demonstrate decline and two of its lines are about decline. **Sixty keys are stated by all
33 files.**

⚠ **The 76% is a FLOOR.** A line counts as copy only where the key sits at the same table occurrence
with the same text, so a file that reorders its tables scores lower than it should.

**The cause is a rule the loader states key by key**: *"Required, like every other key inside a
present table."* A table may be omitted; a key inside a present table may not, and there are no
key-level defaults anywhere. ⚠ **The argument for it is good and is not overturned here** — *a
default could not announce itself*, so a reader cannot tell a chosen number from an inherited one.

✅ **Verified rather than inferred.** Deleting `arterial_capacity_per_hour` from `minimal.toml` gives
`nokey.toml:182: no arterial_capacity_per_hour.` — and that file states `arterial_count = 0`.
***Every Ruleset in the game must state the speed and the capacity of a road type that no shipped
file builds.***

**So the surface is not 172 keys deep. It is ~70 keys wide at every call site, 33 times over.**

---

## What the probe found

### 🔴 F1 — There is no way to say *workplace, and not a home*

**Three of the twelve kinds were refused outright**, in the same words:

> `business is "clerk" and this kind is not tenanted. A Building of this kind comes with a trade and
> has nowhere to hold it — one ceiling counts both kinds of tenant (adr/0147), so write
> tenanted = true, or drop the trade.`

***`tenanted` is one ceiling for Households and Businesses together***
([`adr/0147`](../docs/adr/0147-a-business-takes-premises-by-placement-and-one-ceiling-counts-both-kinds-of-tenant.md)),
so an office, a supermarket and a workshop must each declare themselves housing to hold a trade at
all — and then Households are placed into them. The probe was forced to `tenanted = true` on all
three to get past the load, which is a city where families live in the warehouse district.

⚠ **This is the qualm arriving from the other side.** The difficulty is not that a designer must name
kinds; it is that a kind **cannot say the one thing that separates a workplace from a home**.
Classified *undesigned* rather than unbuilt
([`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md)): nothing has
refused the distinction, nobody has designed it.

### F2 — House, terrace and tenement are not three kinds

Their declarations came out **byte-identical apart from `parked`**. That is
[`plans/0053`](0053-the-block.md) step 3 working exactly as intended — form is the parcel's and the
block pattern's, never the kind's — and it is the strongest evidence that the *thousands* fear is
unfounded: ***the variety a player sees is parcel × pattern × storeys × trade, and none of it is
authored.***

⚠ **What has no expression is the MIX.** Two Zone Rules on one zone bit each draw their own sample
and race; there is no weight, no share and no priority key. `[[band]]` is the mechanism that carries
density, and `banded.toml`'s own header records that ***the generator paints bands as concentric
rings and the Ruleset chooses only how many.***

### ✅ F3 — A service Building has no capacity, and one school covers a million people

`ServiceEngine` holds no capacity of any kind: **it serves every Household that can route to it.**
`--schools 4` changes travel distance and not admission. ***"The school is full" is most of what a
school IS in a city-builder***, and it is `adr/0070` *unbuilt* — a mechanism, not a key.

⚠ **DISCHARGED 2026-09-03** — and the finding was worse than this paragraph said. See
*F3 discharged*, below: it was not that one school could cover a million people in principle, it was
that ***one school was covering the whole of a shipped world already*** while three others stood
empty beside it and the reach panel reported 100%.

### 🔴 F4 — Nothing in this project had ever built a mixed city

`SyntheticCity.DwellingKind` is a hardcoded **1**, and its own remark says *"the kind this populator
raises, and the only one it knows."* ⚠ ***Every headless run, every golden fixture and every
measurement in this corpus was taken on a city made entirely of the first declared kind.***

The probe's first run bore it out: 424 Lots, 376 Buildings, 5 Zone Rules, **8,192 Ticks, 0 Buildings
raised** — the populator had already built on everything. Adding decline so Lots would free up gave
559 raised and 562 condemned over 20,480 Ticks, ***and the mix could not be read, because no
instrument in the repository counted Buildings by kind.*** That is what §*What shipped* closes.

### 🔴 F5 — A Zone Rule on an unpainted bit loads clean and builds nothing for ever

`LotTable.ZoneBits` is **16**. `SyntheticCity` paints **two** — `Housing` (bit 0) and `Trade`
(bit 1). The probe's industrial rule named bit 2, **loaded without complaint, and can never fire.**

⚠ **`RulesetLoader` is not wrong to admit it**: it checks the bit is inside `ZoneBits`, and it cannot
check that anything paints it — a `zone` command carries the full width, so a player could. ***The
gap is that nothing downstream reported the silence***, and every counter in `--census` reads for a
dead rule exactly as it reads for a rule that found no vacant Lot.

---

## What shipped — `--kinds`

**The standing city counted by Building kind**, `src/Borough.Headless/KindDump.cs`, with
`KindDumpTests` beside it. Three panels and two footers: what each kind **declares**, the standing
city **before and after** `--ticks` on `--zones`' shape, the **Zone Rules that can never build**, and
**what stands nowhere and why**.

⚠ **The zero rows are the point rather than the padding**, and the footer separates three unlike
reasons a kind stands nowhere: no Zone Rule names it and it is a service, which is
[`adr/0032`](../docs/adr/0032-services-are-delivered-by-trips-not-by-coverage.md) working; no
Zone Rule names it and it is not, which is content with no way in; or a rule names it and has not yet
won a Lot.

⚠ **The painted set is read off the WORLD and not off the generator** — the union of every live Lot's
permission set — so it stays true when a command paints something `SyntheticCity` does not
([`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)).

### 🔴 Two defects in the instrument, both caught by writing its tests

**`used` counted Households against a ceiling that counts Households AND Businesses.** A row of 24
shops each holding a trade printed `used 0%`. ***A share whose halves are denominated differently is
[`plans/0012`](0012-corpus-audit.md) Cause 5 inside one expression***, and what catches it is
`The_used_share_counts_both_kinds_of_tenant` — an **identity over the printed columns**, which holds
for every row of every world where a spot value would have held for one.

🔴 **AND THE DEAD-RULE CHECK WAS READ OFF THE WRONG QUESTION.** The first cut reported it only for a
kind standing nowhere, which hid **every dead rule naming a kind the populator also builds** — so
moving `minimal.toml`'s only Zone Rule to bit 5 produced a clean report. ***A rule that can never fire
is dead whether or not something else raises its kind.*** The test failed, the instrument was wrong,
and the two questions now have a panel each.

---

## What it found on shipped worlds, the first time it was pointed at them

⚠ **Every figure below is one reading at one population and one horizon, and ratifies nothing.**

🔴 **`banded.toml` has never stood a `shopfront`.** 306 Lots, 272 Buildings, 4,000 Citizens, 20,480
Ticks: **zero**. Nothing in that file declines, so no Lot ever goes vacant for the trade rule to win.
The file's own header says it exists so that `admits` *has something to refuse*, and
`BandAdmissionTests` asserts the refusal at the predicate — ***so this is the file working as written
and not a defect***, but the second kind in the file that demonstrates a second kind has never been
built.

🔴 **`provisioned.toml`'s steady state is HALF DERELICT, and the market readings are taken there.**
At 4,000 Citizens over 24,576 Ticks: 490 dwellings become **378 standing of which 190 are shells**,
Households fall **1,440 → 499**, and **8 shopfronts** stand holding **no Household at all**. At 2,000
Citizens the proportions are the same — 190 standing, **93 shells**, 237 Households — ***so it is a
property of the file and not of the population.***

⚠ **This does not invalidate the market column**, which measures stock against draw and is unaffected
by how many neighbours are derelict. **It is a caveat on the world those numbers were read in**, owed
to whoever next quotes them. Filed rather than worked around
([`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md)).

---

## ✅ F1 discharged — `tenanted` became `houses` and `premises`

**One boolean was doing two jobs.** `adr/0147` gives a Building a single ceiling that a Household and
a Business compete for — a shop on the ground floor costs a family its slot — and that is right and is
**untouched**. What was missing is the **permission** over that ceiling, which was always two questions
wearing one word.

| | `houses` | `premises` |
|---|---|---|
| **A house, a terrace** | yes | no |
| **A corner shop** — flats above the shop | yes | yes |
| **An office, a supermarket, a depot** | no | yes |
| **A warehouse, a monument** | no | no |

⚠ **Neither key adds a tenancy.** A Building's tenancies are its floor over `[capacity]
floor_tiles_per_occupant` whichever is set, and both absent is a Building nobody occupies.
`World.TryDeclaredOccupancy` gates on **either**; `TryDeclaredHousing` returns **zero** without
`houses`, which is the line that stops anything sizing a city from counting an office block's
tenancies as homes.

**One predicate became two, and the two call sites had always been asking different questions in the
same words.** `World.HasRoom` stays as *is there a free tenancy*; `HasRoomForHousehold` and
`HasRoomForPremises` add the permission, and `PlacementEngine`'s housing pass and unpremised pass now
ask one each. ⚠ **They are not exclusive and must not be written as a choice** — mixed use is a
Household and a Business competing for one slot, which is what a second ceiling would have destroyed.

### 🔴 `tenanted` is REFUSED by name rather than read as `houses`

***A key that changed meaning is more dangerous than a key that was deleted.*** Every shipped file
writing `tenanted = true` on a kind carrying a trade meant **both** keys, so quietly keeping the
housing half would have left every shop in the game unable to hold the shop — ***a Ruleset loading
clean and doing less than it says.*** `RefuseRetired` carries it, and the refusal names both halves.

### ✅ It is behaviour-neutral, and that is measured rather than argued

All 39 declarations across the 33 shipped files became `houses = true` **and** `premises = true`,
which is exactly what the old key meant. **2,693 tests passed on the first run with the split in** and
not one placement, occupancy, tenancy, employment, business or parking test moved. ⚠ **The three
Ruleset CONTENT hashes moved and the State Hash traces did not** — the text of the files changed and
the city did not, which is `05 §4`'s distinction arriving at the file level.

### ✅ And the probe now writes what it could not

The same twelve kinds, reloaded: **16 offices stand holding no Household and 16 trades**, 13
supermarkets likewise, 19 corner shops mixed, and **`house` and `terrace` hold zero trades** — the
residential kinds refuse a Business now, which is the half of this nobody asked for and which falls
out of the same key. ⚠ **At 4,000 Citizens over 20,480 Ticks on a probe that ratifies nothing.**

---

## ✅ F3 discharged — a school is full, and being full is a third city

`[capacity] floor_tiles_per_place`. **A fourth rate in a table that already had three**, and
`plans/0053` step 3 arriving at the one capacity that had escaped it: a school's places are its floor
area over the rate, so ***a bigger school teaches more children*** and where the schools stand starts
to matter.

### 🔴 The reading that justified building it, and what it found instead

`--school --ruleset rulesets/schooled.toml --citizens 2000 --ticks 100000 --schools 4` stood four
schools of **64, 54, 48 and 144** Tiles of floor and reached steady state at **109 families with a
child**. With no ceiling:

| | attended | unreached | no school | full | deliverable share |
|---|---|---|---|---|---|
| **before** | 109 | 0 | 0 | — | **100%** |
| **after**, at rate 3 | 103 | 0 | 0 | **6** | 95% |

🔴 ⚠ **ALL 109 FAMILIES ATTENDED THE SAME SCHOOL AND THE OTHER THREE SERVED NOBODY.** The per-school
panel read `109 / 0 / 0 / 0`. `ServiceEngine` satisfices — it stops at the first candidate inside the
Fast rung (`adr/0017`) — and at 2,000 Citizens the whole city is inside one Fast rung, so *the first
school in slot order* wins for everybody for ever. ***And the reach panel called that 100%,*** which
is F3's *one school covers a million people* in the literal, on a shipped file, unreported by any
instrument in the repository until this one printed a per-school row.

With the rate in force the same four hold **21, 18, 16 and 48** places and every one of them fills.
⚠ **The numbers are a property of that population and that siting and ratify nothing** — `--school`
places schools by striding the vacant Lot list, which is deliberately not a siting policy.

### What the ceiling actually buys is DISTRIBUTION, and scarcity is the case beyond it

A full school is **skipped and the family walks on**, which is what a family does — so the mechanism's
first effect is that the load spreads and siting becomes a decision. Only when every reachable school
is full does anybody fail, and that is the **third counter**.

| counter | the city it names | what the player does |
|---|---|---|
| `no school` | none in the box | build one |
| `unreached` | one in the box, no route in the Budget — `adr/0032`'s Severance | mend the network |
| **`full`** | one in the box, reachable, no place left today | build **another** one |

⚠ **`full` is tested AFTER the route and not before it**, which is the expensive order and the only
honest one: fullness is `O(1)` and a route is not, but asking the cheap question first would file a
school behind an Arterial under *full* and ***the player would build a second school to fix a road.***
It costs nothing in the ordinary world, where nothing is full and every candidate in the box is routed
anyway.

### 🔴 Its absence means the OPPOSITE of the other three rates', and that is forced

The other three are **supplies**, so an absent one is a city with none of that thing. This one is a
**ceiling**, so an absent one is a city where no service Building is ever full. ***An unstated bound
is no bound.*** The other reading would have emptied every school in the corpus, silently, on the day
the key was added. ⚠ **The cost is that it is opt-in**, so `RulesetLoader` refuses it in a file that
declares no serving kind — `TryAttendedRates`' refusal from the other side, and this key needs it
more, because its absence is invisible by design.

⚠ **`BuildingTable.AttendedToday` advances in EVERY world and binds only where a rate is stated.**
***The number a designer needs in order to choose the ceiling is the one that column holds***, and a
meter that only started counting once somebody had already chosen could not have supplied the reading
above.

### ✅ F6 discharged — WHO gets the last place is the family living nearest

**The pass walks Households in slot order**, so the same families take the places every Day and the
same six are turned away every Day. ⚠ **Slot order is not noise**: a slot is allocated when a
Household is created, so ***the oldest Households in the city always get the school and the newest
never do.*** It reads as a queue nobody joined.

**It is `adr/0070` *undesigned* rather than unbuilt.** The alternatives are real design choices with
arguments of their own — admit nearest-first, admit by arrival order within the Day, admit by
lottery on the Household's own id — and each says something different about what a school is. ⚠ **It
could not have been seen before the ceiling existed**, because with no ceiling the order of admission
has no consequence at all. ***A mechanism with no scarcity in it cannot be unfair.*** Filed here and
not worked around.

**Discharged 2026-09-03, and the answer taken was nearest-first.** The pass is now two passes:
`ServiceEngine.Ask` asks every Household how far its nearest service Building is, `Order` sorts the
occasions by that distance, and `Serve` serves them in that order. ***The family living nearest a
school is admitted first, and the family turned away is the one living furthest from any of them.***

🔴 **It cost the satisficing break, and that is a design change rather than a tidy-up.** The walk
used to stop at the first `Fast`-rung candidate in slot order ([`adr/0017`](../docs/adr/0017-agents-satisfice-they-never-optimise.md));
it now routes every candidate in the box and takes the cheapest. ***Nothing can admit nearest-first
without knowing who is nearest*** — and a build that has paid for a distance may not then walk a
family past its nearest school to a further one it happened to meet earlier in slot order. ⚠ **What
makes the break affordable to lose is the argument already written down for having no `candidates`
key**: service Buildings are placed by hand, one verb at a time, so the set being ranked is bounded
by the player and not by the city. The blow-up `adr/0017` refuses is a Household ranking the *city*.

⚠ **The order is over OCCASIONS and not over one school's applicants, and the two come apart.** Each
family is keyed by the distance to *its own* nearest school, so a family whose nearest is full can be
admitted at a further one ahead of a family living closer to *that* one. Closing the gap is deferred
acceptance — a rejected applicant re-keyed on its next candidate and re-queued — and ***that costs a
route per displacement where this costs a sort.*** What is left is a mis-ordering between two
families at one school; what it replaces was a mis-ordering across the whole city on a property that
has nothing to do with schools.

### 🔴 AND THE INSTRUMENT THAT FOUND F3 CANNOT SEE F6's REPAIR AT ALL

`--school --ruleset rulesets/schooled.toml --citizens 2000 --ticks 100000 --schools 4` prints
**byte-for-byte the same output** before and after — diffed on 2026-09-03, both engines built from
the same tree. 103 attended, 6 turned away, deliverable share 95%, the four schools at 21/18/16/48
and every one full.

***Every column in that panel is a COUNT, and the change is an IDENTITY.*** A different six families
being turned away is invisible to a tally of how many were, which is
[F3](#-f3-discharged--a-school-is-full-and-being-full-is-a-third-city)'s *the reach panel called that
100%* arriving one repair later and one level down. ⚠ **So the evidence is the tests and there was
never going to be a reading**: `The_place_goes_to_the_nearest_family_and_not_the_oldest_household`
and `Both_schools_teach_somebody_when_neither_is_full` both go **red against the previous engine** —
it admitted a family at a walk of **201,325** where the nearest applicant stood at **85,004**, and it
left the second school teaching nobody. ***A number that only a test can hold is still a number.***


### 🔴 F6a — the gap was MEASURED before it was argued about, and it is not small

The section above deferred the per-school ordering on the grounds that ***nobody knew the size of the
gap.*** That is a claim a measurement could settle, so [`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)
says it may not be settled by argument. **It was measured on 2026-09-03 and the argument loses.**

**An INVERSION is a blocking pair, counted.** A family turned away at a full door, which could have
reached some school inside the Commute Budget, and whose walk to that school is *shorter* than the
longest walk among the families that school admitted. ***That is exactly the pair a stable matching
forbids***: the family would rather have the school, and the school would rather have the family.
`ServiceEngine.LastHouseholds`/`LastProviders` expose the Day's assignment, `ServiceAdmission.Measure`
counts the pairs, and `--school` prints them.

`--school --ruleset rulesets/schooled.toml --citizens 2000 --ticks 100000`, sweeping `--schools`:

| schools | admitted | turned away | **inverted** | worst margin |
|---|---|---|---|---|
| 1 | 21 | 88 | **0** | — |
| 2 | 54 | 55 | **55 — every one** | **12.3 min** |
| 4 | 103 | 6 | **4** | 2.1 min |
| 8 | 109 | 0 | **0** | — |

🔴 **THE GAP IS ZERO AT BOTH ENDS AND TOTAL IN THE MIDDLE, AND ONLY ONE OF THE TWO ZEROES IS A
RESULT.** At **one** school it is zero *by construction* — there is nowhere to leak to, so *nearest to
my own nearest* and *nearest to this school* are the same question, and that row is the instrument's
control rather than a finding. At **eight** it is zero because nobody is turned away at all: ***a
mechanism with no scarcity in it cannot be unfair***, which is the same sentence that explains why
F6 was invisible before the ceiling shipped. **The middle is where a player actually builds.**

⚠ **12.3 minutes is against a Fast rung of 20 and a Commute Budget of 50** ([`adr/0095`](../docs/adr/0095-a-commute-budget-is-three-rungs-and-only-the-last-one-refuses.md)),
so the worst-served family at two schools was walked past a school **62% of a Fast commute** nearer
than the family that took its place. That is not a rounding difference between two orderings.

### 🔴 F6b — the mechanism that produces it, which is why 2 schools is worse than 4

***A family is keyed by the distance to the school it did NOT get.*** The key is the walk to its
nearest service Building; the walk it actually makes is to whatever still had a place when its turn
came. Those are the same journey only while its first choice is free.

So the scarcer the supply, the more families are admitted at a school their key says nothing about —
and a key that describes an unmade journey is a key that sorts nobody correctly. At two schools
**every family whose nearest was full** was placed at the other one on a key drawn from the first,
which is why the inversion count is not merely high but **total**.

⚠ **The count is a LOWER BOUND on what deferred acceptance would move.** A displaced family proposes
onward and can displace a third, so one blocking pair may stand for a chain. What it is exact about
is the direction: ***a world with no inversions has no chain to start.***

### 🔴 F6c — so the recommendation reverses, and the reason it was wrong is on the record

**The judgement written when nearest-first shipped was *not worth building, the gap is probably
theoretical*.** It rested on three arguments and the measurement kills the first outright — the gap
is 100% of the turned-away at the shape of city the mechanism exists for. The other two stand and are
now the whole case for waiting: deferred acceptance is a per-school held-set plus a per-family cursor
plus a convergence loop, and [`adr/0017`](../docs/adr/0017-agents-satisfice-they-never-optimise.md)
refuses an equilibrium computation in a way it does not refuse ranking a handful of schools once.
***Ranking what the player built is not optimisation; re-proposing in response to what other families
got is.*** That is an ADR-sized argument and ADRs are frozen ([`plans/0045`](0045-amnesty.md)
standing order 1).

🔴 **What the amnesty does NOT freeze is the finding**, and this is it: **the shipped ordering is
unstable, the instability is measured, and the design question is now *what is a school for* rather
than *how big is the gap*.** The three answers F6 originally named — nearest-first, arrival order,
lottery — were about who deserves a place. ***The measurement says the current answer is none of the
three***: at two schools it is *whoever lived near a school that was already full*, which is nobody's
idea of a rule.

⚠ **And it cost the panel a column it should always have had.** Every other number in `--school` is a
count, so the day admission stopped being slot order the dump printed byte-for-byte the same output —
see the section above. ***An instrument that can only count cannot report a change of identity***,
which is [F3](#-f3-discharged--a-school-is-full-and-being-full-is-a-third-city)'s *the reach panel
called that 100%* for the third time in this document.

### The rate is chosen, and it is the one number in `[capacity]` with no standing city behind it

The other three are divisions of what 39 `occupants`, 32 `jobs` and 29 `parking` declarations already
said about a detached building. **No kind has ever declared a place count**, so there is nothing to
divide. What is left is the method `minimal.toml` uses for its *cross-check* rather than its anchor: a
school is about 10 m² of gross floor per pupil, a Tile is 16 m² so a real place is 0.6 Tiles, and that
file admits its floor areas are **about 4× a real building's** — so 2.5, and **3** is the integer
beside it. It lands where `floor_tiles_per_job` sits, and a desk scaled the same way lands at 4.
🔴 **No ratifier, no `plans/0002` §D row**, under `plans/0045` standing order 4.

---

## What is owed

| # | Owed | Kind |
|---|---|---|
| 1 | ~~A kind cannot declare itself a workplace~~ ✅ **DONE 2026-09-02** — `houses` and `premises`, above | shipped |
| 2 | ~~A service Building has no capacity~~ ✅ **DONE 2026-09-03** — `[capacity] floor_tiles_per_place`, above | shipped |
| 3 | **Nothing expresses the MIX within a zone** (F2) — no weight, share or priority on a Zone Rule | *undesigned* |
| 4 | **76% of every shipped Ruleset is copy**, because no key inside a present table may be omitted. A base-and-differences mechanism needs an answer to *a default could not announce itself* | *undesigned* |
| 5 | **`banded.toml` has never stood its second kind**; **`provisioned.toml` steadies at half shells** | readings, above |
| 6 | ~~Who gets the last place at a full school is slot order~~ ✅ **NEAREST-FIRST SHIPPED 2026-09-03** — and the residue is **MEASURED**: the ordering is unstable, 55 of 55 turned-away families inverted at two schools by up to 12.3 min (F6a). Deferred acceptance is the answer and it needs an ADR | shipped, residue *undesigned* |

🔴 **NONE OF THESE IS A SHAPE COMMITMENT TO UNWIND.** They are absences. ***The thing that IS a
commitment — a kind carries behaviour and the ground carries form — held up under twelve kinds and
got smaller rather than larger***, which is the one answer the original question actually wanted.
