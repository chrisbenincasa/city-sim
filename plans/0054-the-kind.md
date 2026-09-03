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

### 🔴 F3 — A service Building has no capacity, and one school covers a million people

`ServiceEngine` holds no capacity of any kind: **it serves every Household that can route to it.**
`--schools 4` changes travel distance and not admission. ***"The school is full" is most of what a
school IS in a city-builder***, and it is `adr/0070` *unbuilt* — a mechanism, not a key.

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

## What is owed

| # | Owed | Kind |
|---|---|---|
| 1 | **A kind cannot declare itself a workplace** (F1). One key, or splitting the tenancy ceiling | *undesigned* |
| 2 | **A service Building has no capacity** (F3). A mechanism rather than a key | *unbuilt* |
| 3 | **Nothing expresses the MIX within a zone** (F2) — no weight, share or priority on a Zone Rule | *undesigned* |
| 4 | **76% of every shipped Ruleset is copy**, because no key inside a present table may be omitted. A base-and-differences mechanism needs an answer to *a default could not announce itself* | *undesigned* |
| 5 | **`banded.toml` has never stood its second kind**; **`provisioned.toml` steadies at half shells** | readings, above |

🔴 **NONE OF THESE IS A SHAPE COMMITMENT TO UNWIND.** They are absences. ***The thing that IS a
commitment — a kind carries behaviour and the ground carries form — held up under twelve kinds and
got smaller rather than larger***, which is the one answer the original question actually wanted.
