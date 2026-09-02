# 0050 — The Ruleset sweep

**The board's tidy row 3, run.** [`adr/0164`](../docs/adr/0164-a-ruleset-key-is-designer-facing-or-it-belongs-in-the-instrument.md)
settled the test on 2026-08-25 and did not apply it; its own Consequences say
*"🔴 The existing keys have not been swept and almost certainly contain hits."*
[`0012`](0012-corpus-audit.md) holds the specification and asks for **a count first**.

⚠ **This document owns the count and the verdict per key. It owns no decision** — `adr/0164` is the
decision and this is its execution. Standing order 1 bans a new ADR and none is needed.

---

## The count

**169 leaf keys in `rulesets/ruleset.schema.json`. 150 are set by some shipped file. 19 are set by
none.**

Of the 19, **8 are refused by name** — the loader accepts them only so it can tell an author where
they went — and **11 are accepted with a default no demonstration has needed**.

| | keys | verdict |
|---|---|---|
| **Tombstones** — refused by name, advertised as valid | 8 | 🔴 **HIT.** The schema is the defect, not the key |
| **Accepted, unexercised** — a default nothing overrides | 11 | ✅ no action |
| **Set, and diverged on** — two files disagree | 58 | ✅ somebody has had an opinion |
| **Set at one value everywhere** — never diverged on | 92 | ⚠ the pool the test is applied to below |

🔴 **The 92 is EVIDENCE AND NOT THE TEST**, and the distinction is the one `adr/0164` spends its
*Rejected* section on. *Does it ship in more than one Ruleset* was refused there outright, because
`congested.toml`'s `[traffic]` and `twinned.toml`'s `[market]` are single-file **on purpose**. What
the never-diverged reading buys is the shape of milestone 17's own argument — ***a key no author has
ever diverged on is a constant wearing a key's clothes*** — which is a reason to **look**, and the
looking is below.

---

## The hits

### 🔴 Eight tombstones, and the schema is what makes them a defect

Each is refused at load by name, with a message telling the author where the key went. Each appears
in `rulesets/ruleset.schema.json`, which `.taplo.toml` points every editor at.

| key | refused at | what it became |
|---|---|---|
| `[[building]] condemn_after` | `RulesetLoader.cs:1794` | `condemn_after_days`, **and the units changed** |
| `[[building]] jobs` | `:2063` (`EmploymentKeysThatMoved`) | `[[business]] jobs` (`adr/0148`) |
| `[[building]] shift_start_earliest_hour` | `:2063` | `[[business]]` |
| `[[building]] shift_start_latest_hour` | `:2063` | `[[business]]` |
| `[[business]] wage` | `:2334` | `wage_per_day` + `pay_period_days`, required together |
| `[layers] sealing_decay_tau` | `:4756` | `[[terrain]] sealing_decay_tau`, one per ground type |
| `[[resource]] storage` | `:1335` | nothing — unimplemented, refused rather than accepted silently |
| `[[zone_rule]] sample` | `:2960` | `revisit_ticks` (`adr/0059` — state the duration, derive the sample) |

🔴 ***THE REFUSALS ARE ALL CORRECT AND THE SCHEMA UNDOES EVERY ONE OF THEM.*** A designer opening a
Ruleset gets completion for eight keys whose only behaviour is to refuse the file. That is
`adr/0164`'s Cause — ***a document offers a control nobody would use*** — arriving in the one
artefact a designer actually looks at, and it is worse than the keys the ADR was written about,
because these do not merely fail to be useful: **they are advertised as the way to do a thing the
loader will not let you do.**

⚠ **The cause is mechanical rather than an oversight.** `RulesetSchemaTests.FromLoader` builds the
schema from `RulesetLoader.KeySurface`, whose contract is *"every key any reader asks for"* — and a
reader asks for a tombstone in order to refuse it. ***A key surface that cannot tell asking-to-accept
from asking-to-refuse will publish every refusal as an offer.***

⚠ **The remedy is NOT to stop refusing them.** The refusal is the whole value: an author who writes
`condemn_after` is told the key was renamed *and that its units changed*, which is a sentence no
"unknown key" message could carry. What is wrong is the advertisement.

### The eleven that are accepted and unexercised, and are not hits

`[layers] desirability_shoreline_percent`, `shoreline_intensity_percent`, `shoreline_range_metres`,
`sealing_decay_offset`, `sealing_decay_period`, `woodland_regrowth_offset`, `woodland_regrowth_period`;
`[needs] health_degrade`, `health_recover`; `[[rule]] apply.percent`; `[[rule]] fills`.

Each has a default the loader supplies and no shipped world overrides. ✅ **A key with a default that
nobody has needed to change is the test passing, not failing** — a designer *would* set a shoreline
weight or a Health rate; nothing has yet had a reason to. ⚠ **`fills` is the one worth watching**: it
is required of a multi-link `on_fail` chain and `diagnosed.toml`'s chain is one link, so the only
mechanism that needs it is unexercised rather than the key being unwanted.

---

## The verdict on the 150 keys that are set

Grouped by table. A table gets one verdict where the answer is uniform.

| table | keys | verdict |
|---|---|---|
| `[[resource]]`, `[[building]]`, `[[business]]`, `[[rule]]`, `[[zone_rule]]`, `[[policy]]`, `[[life_stage]]`, `[[terrain]]`, `[[hinterland]]` | declarations | ✅ **Not tuning at all.** A name, a kind, a scope and an amount are the content a Ruleset exists to carry |
| `[trips]`, `[traffic]`, `[market]`, `[districts]`, `[water]`, `[disasters]`, `[needs]`, `[founding]` | 26 | ✅ **Designer-facing.** Each states how hard a mechanism bites. Never diverged on only because each has exactly one demonstration |
| `[roads]` speeds and capacities | 6 | ✅ **Designer-facing.** `CLAUDE.md` records that the speeds have a source outside the corpus and the capacities do not |
| `[lots] lots_per_segment` | 1 | ✅ **Shape 3, and already correct.** Its comment says *"DERIVED, not chosen"* — the disclaimer `adr/0164` asks for, written before the ADR was |
| `[placement] revisit_ticks`, `[[zone_rule]] revisit_ticks` | 2 | ✅ **Shape 3, and already correct.** *"stating it is not choosing it"* is `adr/0164`'s own worked example |
| `[roads] arterial_count` | 1 | ✅ **Shape 1, and already correct.** `adr/0164`'s worked example — *an Arterial is a player tool that does not belong in a generator* — neutered to 0 in 28 of 31 |
| `[placement] interval`, `[[zone_rule]] interval`, `[jobs] interval` | 3 | ⚠ **Shape 3, and the disclaimer was MISSING.** Written 2026-09-01 (below) |
| `[layers]` cadences and weights | 11 | ✅ **Shape 3 by `adr/0044`, and the disclaimer was already there.** See below |
| `[roads] arterial_junction_tiles`, `arterial_speed_kph`, `arterial_capacity_per_hour`, `foot_crossing_every` | 4 | ⚠ **Inert rather than scaffolding.** See below |
| `[[lattice]] origin_east_tiles`, `origin_north_tiles` | 2 | ⚠ **Borderline. Disclaiming comment.** See below |

### ⚠ `interval = 32` stands in three tables, in every file ever shipped, and said nothing about itself

🔴 **THE ROW ABOVE READ *five keys* UNTIL 2026-09-01 AND THE ANSWER WAS THREE.** The count was taken
off the never-diverged list rather than out of the files, and `minimal.toml` already documented two
of the five at length — `candidates` is *"02 §5.3'S N AND IT IS THE ONE FREE NUMBER HERE"*, and
`[jobs] revisit_ticks` gets four paragraphs including why copying `[placement]`'s 1024 is an argument
rather than a convenience. ***A sweep that counts a key's occurrences and not its comments finds
work that is already done***, which is this document doing the thing `plans/0012` Cause 1 is about.

**What was genuinely missing is `interval`, and it is missing in all three tables at once.** It
appears in `minimal.toml` only inside the formula `sample = ceil(Pool × interval ÷ revisit_ticks)` —
never on its own account. Its neighbours both carry the disclaimer: `revisit_ticks` says *"IS A
DURATION AND THE SAMPLE IS DERIVED"*, and `[[zone_rule]]`'s says *"2048 IS TICKS_PER_DAY AND STATING
IT IS NOT CHOOSING IT"*, which is `adr/0164`'s own quoted model.

✅ **Written 2026-09-01, once, in the `[[zone_rule]]` block, with a one-line pointer from the other
two.** What it says: `interval` is the **delivery grain**, it divides out of the formula, and
nothing a player watches moves with it — ⚠ **but it is still hash-bearing, because the sample is a
`ceil`**, so two worlds differing only in `interval` diverge while the *design* does not. It stays a
key because deleting it would put a cadence in the binary that `adr/0015` says belongs in a file.

🔴 **They are shape 3 and NOT shape 2, and getting that backwards would be the damage the spec
warns about**: `adr/0059` decided the Ruleset states a **duration** and the sample derives from it,
so `revisit_ticks` is the authored half and `interval` and `candidates` are the delivery. Deleting
them would move a designer-facing duration into code. ***The remedy is the sentence, not the
deletion.***

### ⚠ Eleven `[layers]` keys, same shape, and one of them is louder than the rest

`noise_range_metres` · `noise_intensity_percent` · `desirability_pollution_percent` ·
`desirability_noise_percent` · `pollution_period` · `pollution_offset` · `land_value_period` ·
`land_value_offset` · `kernel_metres` · `pollution_decay_ticks` · `land_value_tau`.

All eleven at one value in all 31 files. [`adr/0044`](../docs/adr/0044-the-map-layer-diffusion-cadence-is-the-designers-number-not-the-profilers.md)
is a whole decision that the cadence is **the designer's number and not the profiler's**, so the
test's answer is **yes** and these stay.

✅ **Nothing is owed here, and this row said otherwise until 2026-09-01.** `minimal.toml`'s
`[layers]` block opens *"EVERY NUMBER HERE IS `02 §2.4`'S, AND STATING ONE IS NOT CHOOSING IT"* and
goes on to say the cadence is hash-bearing and therefore the designer's rather than the profiler's —
which is the disclaimer and the ADR's sentence, both, written before either was asked for. **Same
correction as the pacing row above and the same cause**: the count was taken off the schema and not
out of the file.

🔴 **`desirability_pollution_percent = 100` and `desirability_noise_percent = 100` are stated in
31 files and are NON-ZERO IN ONE.** `CLAUDE.md` already records that only `fouled.toml` emits, so
***land value is zero everywhere by construction on the other thirty*** — two weights authored 31
times against a quantity that is zero 30 times. That is not a hit under the test (a designer would
absolutely set a desirability weight) and it is worth writing down, because ***a key repeated into
thirty files where it multiplies zero is how a number gets believed without ever being exercised.***

### ⚠ Four `[roads]` keys describe a road type 28 worlds do not have

`arterial_junction_tiles = 512` · `arterial_speed_kph = 90` · `arterial_capacity_per_hour = 12000` ·
`foot_crossing_every = 4`, all in all 31 files, where `arterial_count = 0` in 28 of them.

✅ **NOT a hit, and the reason matters.** These are *inert*, not scaffolding: a designer building a
world with Arterials in it would set every one of them, and three files do exactly that
(`severance.toml`, `bordered.toml`, `crowded.toml`). ⚠ **`CLAUDE.md` already records
`foot_crossing_every` as *inert at the shipped lattice rather than merely unratified***, which is
this observation arriving one key at a time. ***An inert key is a world's property and a scaffolding
key is the corpus's*** — only the second is what `adr/0164` is about.

### ⚠ `[[lattice]]`'s two origins are the borderline case, and the borderline case gets a comment

Six files state an origin. Two readers: `SyntheticCity` decides where to build, and
`PlacementEngine.Distance` measures a Household's taste for the centre against it — so it is **not**
generator-only and shape 2 does not reach it.

🔴 **But both shipped moves were made for the instrument and both headers say so.**
`coastal.toml`'s origin is at the map's middle *"so runoff does not drain off the world"*;
`flooded.toml`'s is on the water's edge because *"the synthetic city cannot meet a coast by
accident"*. ***Neither is a designer deciding where a city stands; both are an author aiming a
demonstration at its own mechanism.***

⚠ **The remedy is the disclaiming comment and not a move**, because the taste reader is real and
`adr/0164`'s third row exists for exactly this: a value with a genuine consumer, stated so an author
can find it. What the comment owes is which of the two readers a given file's number was chosen for.

✅ **Written into both files 2026-09-01**, and writing it found a **third** wrong header. `coastal.toml`
already explained its own 8192 at length and now says what that explanation is *for*. **`flooded.toml`
explained nothing and claimed the diff did not exist**: its header read *"WHAT IT ADDS TO
`coastal.toml`, AND IT IS ONE TABLE … everything else below is `coastal.toml` unchanged"*, and the
non-comment diff is `[disasters]` **plus both origins** — 11520/4800 against 8192/8192. 🔴 ***It
undercounted its own diff by the half that makes the file work***: 0 of 420 Lots exposed at the
inherited origin against 240 of 420 at the moved one. **Three files have now been caught claiming a
one-line diff they did not have, and all three were files whose headers say a copied comment
drifts.**

---

## What this sweep did not find

✅ **No key is read only by `SyntheticCity`.** That was session W's near-miss — a `[lots]` key for
the commercial share of a generated world, one exchange from being authored with a ratifier chosen —
and it is the shape the sweep most expected to find standing. It is not there. `[roads] block_tiles`
is read by `LotSubdivider`, `StreetGrid`, `Frontage`, `Simulation` and the shell; `[[lattice]]`'s
origins are read by placement. ***The ADR was written the day the mistake was nearly made, and it
was made zero times before that.***

🔴 **So the hits are not where the ADR predicted.** It named three shapes and the eight real hits are
a **fourth**: keys the loader already refuses, republished as offers by the artefact generated from
it. ***The sweep found a broken instrument rather than a set of bad decisions***, which is the better
outcome and not the one it was looking for.

---

## The demonstration census

**31 files asked *what do you demonstrate that your parent does not*.** One had no answer.

### 🔴 `monetised.toml` demonstrated nothing, and is retired

Its entire diff against `minimal.toml` was **the removal of `parking = 8` and the `[parking]`
table** — four lines deleted, none added. Its header said it existed as the first file to declare
`family = "money"`, and that declaration is now in the common core of **all** of them,
`minimal.toml` included. ***The demonstration was absorbed into the baseline and nobody went back.***

⚠ **Its one live consumer wanted it for something its header never mentions.**
`ParkingDumpTests.A_ruleset_with_no_parking_table_is_refused` loaded it precisely *because* it was
the file with no `[parking]` — so the only property it still had was the absence a test needed.
***A world whose only distinguishing property is what a test wants from it is a fixture and not a
demonstration.*** Re-pointed at `taxed.toml`, which states no `[parking]` either and has a
demonstration of its own. 765 lines gone.

### The four files nothing in `src/` or `tests/` executed

| file | what covered it | what was done |
|---|---|---|
| **`maintained.toml`** | **nothing at all** | ✅ `MaintainedRulesetTests` — the A/B on one seed |
| **`hungry.toml`** | `NeedTests`, on a hand-built fixture | ✅ `HungryRulesetTests` — the file, against `evicted.toml` |
| `thinned.toml` | `OccupancySheddingTests`, on a hand-built fixture | ✅ `ThinnedRulesetTests` — the file, against `declining.toml` |
| `scarce.toml` | `ParkingScarcityTests`, which **sweeps the parking rung including this file's own value of 1** | ⚠ **deliberately left unrun — see below** |

🔴 **`maintained.toml` was the only one whose CLAIM had no test anywhere, and `CLAUDE.md` quoted a
census out of it.** The sentence at stake — ***one Rule is the whole difference between a city that
loses half its stock and one that loses none*** — was held up by somebody having once run the file by
hand. It is now measured every commit: **0 abandoned against `declining.toml`'s 130** on one seed at
2,000 Citizens over 8,192 Ticks. ⚠ **A second test asserts the dwellings still go short**, because a
city that never starves and a city that heals both abandon nobody and only the second is the point.

⚠ **`thinned.toml` and `scarce.toml` are left, and the reason is stated rather than assumed.** Both
mechanisms are asserted against hand-built fixtures, which is stronger evidence about the *code* than
a shipped file gives. What a shipped file adds is that the world can still **reach** the mechanism —
`choosy.toml` is this project's own example of that failing while every fixture test passed — so
these two are a real gap and a small one. **`thinned.toml`'s mirror is measured and sitting here for
whoever writes it:** 570 Occupants shed / 0 condemned, against `declining.toml`'s 0 shed / 213
condemned, same seed and size.

🔴 **The one-house fixture could not have asked what `thinned.toml` asks.** `OccupancySheddingTests`
runs one Building, four Households and a Zone Rule that *judges and never builds* — deliberately, so
that a replacement raised on every emptied Lot does not confuse its counts. ⚠ **So the thing the file
exists to show — that the loop still closes in a city where placement refills what shedding empties —
was tested nowhere.** `ThinnedRulesetTests` runs it against `declining.toml` on one seed at 1,000
Citizens over 8,192 Ticks: **68 Buildings abandoned on the control arm, 0 on this one**, with the
Unplaced Pool peaking at **97**. ***The Pool is what makes the zero mean something***: this Ruleset
states no gate and no `[[life_stage]]`, so losing a home is the Pool's only door, and a Pool that
fills while nothing is abandoned is a Building that shed and stayed standing.

⚠ **`scarce.toml` was left alone on purpose, and the reason is the opposite one.** Its mechanism is
already swept by `ParkingScarcityTests` **across the whole parking rung, including this file's own
value of 1** — so a single-point run of the file would re-derive one row of a sweep that already
covers it, at the cost of another world-scale test in the commit gate. ***The gap that remains is
that the shipped file is never run, and that is what the folder sweep is for.*** Recorded rather than
closed.

### The two poles that cannot be merged, and why the census did not try

`provisioned.toml` and `oversupplied.toml` differ by two keys and look like one file with a switch.
They are not: [`adr/0170`](../docs/adr/0170-a-shop-is-selected-rather-than-sited-so-the-birth-signal-is-coarse-and-death-does-the-correcting.md)
condition 4 says selection needs **over-supply**, a shop with no competitor pays its levy for ever,
and tier 1's whole job is to stop the city over-supplying — ***so birth and death cannot be shown in
one world***. Same for `declining.toml` and `maintained.toml`. ⚠ **A census that merged files by
similarity would have deleted exactly the pairs whose difference is the finding.**

---

## Collapsing the prose

**13,618 lines and roughly 264,000 words removed, and the loader cannot tell.**

`rulesets/` held **300,167 words** of comment across 20,630 lines, of which only **3,214 lines were
unique** — no corpus check has ever seen one of them, because `CorpusBudgetTests` reads `docs/`,
`plans/` and `//` comments in `src/`+`tests/` and never opens a `.toml`.

🔴 **The convention had already changed and 27 files predated it.** `flooded.toml` is 163 lines —
a file-specific header, then bare TOML. `crowded.toml` 233, `bordered.toml` 304, `twinned.toml` 401.
***Nothing decided; the newer files were simply written differently***, and the older ones went on
carrying ~770 lines of per-key argument each. `minimal.toml` put **100 comment lines between
`[parking]` and `radius_metres`**.

⚠ **The rule applied is mechanical and it is the two lean files' own.** A comment block in any file
but `minimal.toml` is deleted when ≥80% of its lines already appear in `minimal.toml`'s comments;
every unique sentence survives. `bordered.toml` and `twinned.toml` were already saying it in prose —
*"minimal.toml's dwelling carries the argument"* — so each collapsed file now opens its body with one
line pointing there. ***`minimal.toml` is the file that carries an argument and the other 29 comment
only what they change.***

✅ **Proved comment-only twice over.** The schema generated from the collapsed folder is
**byte-identical** to the committed one — no key the loader reads moved — and the golden traces
regenerated with **no diff at all**: all 32 State Hash samples and all 32 driving samples unchanged,
with only the three content-hash header lines moving. ***A content hash is a file fingerprint and not
a State Hash***, which this corpus has had to relearn before.

⚠ **Two headers claimed a diff they did not have, and both said the right thing while doing the
wrong one.** `varied.toml` — *"AND IT IS ONE THING … everything else below is `minimal.toml`
unchanged"* — changes six other lines, three of them about the dwelling itself. `coastal.toml` —
*"AND IT IS ONE TABLE … with one key"* — has six keys in that table and adds a Resource and an
`occupants` change besides. ***Both files carried, verbatim, the sentence about a copied comment being
a second copy that drifts.*** They were the only two that said it, and they were the two that were
wrong. Corrected in place.

### 🔴 A second pass, because the first one stopped at the wrong layer

**Run 2026-09-01 on the user's reading of the result: *"the explanations in rulesets are kind of
ridiculous, i will not lie to you."* They were, and the first pass had not touched why.**

⚠ **What the first pass removed was DUPLICATION ACROSS FILES** — the same paragraph pasted into 24
places. What it left was a different defect and a larger one: **7,329 comment lines against 4,016
lines of TOML**, `minimal.toml` alone at **760 against 95**, and **1,159 of those lines carrying an
`adr/` or `docs/` citation**. ***A paragraph that cites a record is usually re-arguing it***, and a
Ruleset is the last place that argument should live. A good part of the rest was not argument at
all but **changelog** — twenty-five lines in `minimal.toml` on why a quantity was multiplied by four
on 2026-08-13, which is a commit message, and the commit exists.

**The rule, chosen by the user: headers stay, inline commentary goes, one-liners only where the key
is genuinely non-obvious.** All **228** inline comment blocks deleted across 30 files; **six**
one-line notes added back, in `minimal.toml` only, because the other 29 headers already say what
their file changes.

| | before the sweep | after pass 1 | after pass 2 |
|---|---|---|---|
| lines | 25,201 | 11,345 | **8,019** |
| comment lines | 20,630 | 7,329 | **3,922** |
| comment words | 300,167 | 103,283 | **50,456** |

✅ **Proved comment-only by the same two instruments, a second time.** The schema regenerated from
the stripped folder is **byte-identical**; the golden traces regenerate with **all 64 State Hash
samples unchanged** and only the three `ruleset` content-hash header lines moving.

⚠ **What is left is now HEADERS, and the outliers are the same defect one level up.** 3,971 header
lines against 3,473 of TOML, and the distribution is wide: `levied.toml` **367** header lines to
127 of TOML, `founded.toml` **286** to 121 — against `crowded.toml`'s **40** to 117 and
`raised.toml`'s **42** to 150. ***The lean files are lean because they were written after the
convention changed, not because anybody cut them***, which is the first pass's finding arriving
again in the one place it did not reach. **Not acted on**: the user's instruction was *headers
mainly*, and a header is the thing this corpus decided a demonstration Ruleset must carry.

### What the collapse did NOT do

⚠ **`provisioned.toml`, `oversupplied.toml` and `waged.toml` lost 25 lines each and keep ~670.** Their
body prose is about the market and appears nowhere else, so the rule left it. ***That is the check on
the rule*** — a collapse that had flattened them would have been deleting findings rather than copies.

---

## What running the demonstrations found

**The plan required each surviving demonstration to be run and checked against the figure its header
claims.** `declining.toml` still condemns — 440 over 5,000 Ticks — and the collapse left it a
**306-line file with 97 lines of TOML and a two-line diff against `minimal.toml`**, which is what
the amended Definition of done asked somebody to look at.

### 🔴 The `--zones` readout has been printing a false sentence on every run since `adr/0069`

*"A demolition evicts a Building's whole occupancy; a Zone Rule rehouses one Household per Building
it raises, **because a Building has no declared occupancy yet** (`plans/0014` task 10)."*

**Both halves went false, in two different records, and neither took the sentence with it.**
[`adr/0068`](../docs/adr/0068-a-buildings-occupancy-is-declared-by-its-kind-and-an-over-capacity-building-evicts.md)
gave a kind an `occupants` count — it is **4** in 28 of the 34 shipped declarations — and
[`adr/0069`](../docs/adr/0069-placement-is-a-mechanism-of-its-own-and-construction-houses-nobody.md)
took the housing out of construction outright. ⚠ **`ZoneRuleEngine.Create` names the deleted line in
its own remark** — *"construction houses NOBODY. This used to draw a Pool member and place them here
… `World.Place` had exactly one caller and it was this line"* — so the correction was written down,
in the same file, and the readout was not read.

***This is what [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
predicts: the sentence was wrong about the TRIGGER and right about nothing.*** Corrected 2026-09-01.
⚠ **It is worth noticing where it lived.** The corpus checks are document-to-document and a string
literal in a readout is invisible to every one of them — the same blind spot as the 300,167 words of
`.toml` comment this sweep collapsed, one artefact along.

### ⚠ An observation left open, and not a finding

On that run — `--zones --ruleset rulesets/declining.toml --ticks 5000 --no-decide-guard`, `--citizens`
unset and therefore **10,000** — the city **condemned 440 Buildings, held 1,317 Households in the
Unplaced Pool, evaluated 467 vacant Lots, and raised 1**. `Create`'s gate is the zone bit plus a
non-empty Pool, and both look satisfied on the face of it. 🔴 **It is recorded as a thing to check
and NOT as a defect**: this sweep did not read the counters' fold semantics, a 5,000-Tick run is
early in a world that starts full, and `CLAUDE.md`'s own census of this file is taken over 65,536
Ticks. ***Somebody who knows the Zone Rule's flows should look before anybody quotes this paragraph
as a bug.***

---

## Owed

| | Work | State |
|---|---|---|
| 1 | The count — 169 keys, 150 set, 19 unset, 8 hits | ✅ |
| 2 | `RefuseRetired`, the schema regenerated, `RulesetSchemaTests` guarding it | ✅ |
| 3 | The disclaiming comment — `interval` in three tables, both lattice origins. ⚠ **The `[layers]` eleven and two of the five pacing keys already had one** | ✅ |
| 4 | The census — one file retired, two given tests | ✅ |
| 5 | The collapse — 13,618 lines | ✅ |
| 6 | `thinned.toml` exercised as a file. ⚠ **`scarce.toml` deliberately not** | ✅ |
| 7 | Should `rulesets/` join the amnesty numerator? **The real number now exists — see below** | ⚠ **user's call** |

⚠ **Row 3 is deliberately last and is not a deletion.** Every one of those keys passes the test; what
they lack is the sentence saying whether the value was chosen or derived. `adr/0164`'s third row
exists to stop this sweep over-firing, and ***a sweep that deletes keys does more damage than the
thing it fixes***.

### 🔴 Row 7 — the numerator, measured rather than estimated

**The plan reserved this for the day the real number existed. It exists.**

| | words | ratio over 36,766 lines of simulation |
|---|---|---|
| `docs/` + `plans/` + `//` comments in `src/`+`tests/` — what `CorpusBudgetTests` counts today | **1,930,386** | **52**, against a ceiling of **52** |
| `#` comments in `rulesets/`, after the collapse | **102,679** | — |
| both | **2,033,065** | **55** |

⚠ **The estimate in the plan was 40,000 words and it was low by 2.5×**, because it was taken off the
line count rather than off `wc -w`. ***An estimate of a quantity nobody had measured, written into a
plan and then reasoned from, is `plans/0042` F12's shape*** — the same reason this document quotes
the census and not the arithmetic anywhere else.

🔴 **So admitting `rulesets/` today costs a re-seed from 52 to 55, and that is the escape hatch and
not the ratchet.** `CorpusBudgetTests`'s own remarks say a raise made by the author who tripped it
*"is not a check"*, and this would be one: the collapse removed 264,000 words and the reward for it
would be a ceiling three points looser than the one that has been holding. **Left undone
deliberately, and named here rather than acted on** — it is the user's call whether the reservoir
refills or the ceiling moves.

⚠ **Note what the collapse did to the number.** `rulesets/` held **300,167** words before it and
holds **102,679** after; at the old figure the combined ratio was **60**. ***The directory has gone
from a quarter of the corpus to a twentieth of it*** without anything being decided about whether it
counts.
