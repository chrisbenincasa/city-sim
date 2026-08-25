# The golden-hash baseline, and how to re-baseline it

Slice 5 task 4 of [`plans/0008`](../../../plans/0008-tick-and-replay.md).

**The point is not that the State Hash never moves. It is that it never moves without somebody
saying so.** Every other determinism test in this repository is a closed loop — it runs a thing
twice and checks the two runs agree, which stays true however far the simulation has drifted from
what it used to compute. The files here are the only numbers recorded on a previous day, so they
are the only thing that can notice a change nobody was looking for.

## What is committed

| File | What it is | Recorded from |
|---|---|---|
| `session.borough` | The session itself, as the Input Log the runner replays | hand-written; checked against `GoldenFixtures.Session()` |
| `session-trace.txt` | Thirty-two State Hash samples from that 2,048-Tick session | `Borough.Headless --out` |
| `world-hash.txt` | One State Hash over a hand-built city, with its row counts | `GoldenFixtures.Build()` |
| `driving-session.borough` | **The second session**, on the one shipped Ruleset in which anybody owns a car | hand-written; checked against `GoldenFixtures.DrivingSession()` |
| `driving-session-trace.txt` | Thirty-two samples from that 4,096-Tick session | `Borough.Headless --out` |

**Two further artefacts are not in this directory and are part of the baseline anyway**:
**[`rulesets/minimal.toml`](../../../rulesets/minimal.toml)**, which the session opens on, and
**[`rulesets/minimal-tuned.toml`](../../../rulesets/minimal-tuned.toml)**, which it **reloads into at
Tick 1,024** (slice 8 task 10; it was Tick 128 until slice 10 task 11 lengthened the session and moved
the reload to stay halfway, and this line went on saying 128 for two slices afterwards). Both are named
by content hash, so editing either moves every sample here — and
`The_golden_ruleset_is_the_one_the_session_names` says so for **both**, with the number to paste in.
It covered only the first until 5a-bis, which is how the second's hash came to be wrong in this
directory for a slice with nothing to notice it.

**The second Ruleset is the first with one number changed, and two tests hold it there.**
`The_two_golden_rulesets_differ_in_exactly_one_line` compares them with the comments stripped, so a
copy that drifts fails with the diff in the message. And
`The_committed_reload_moves_the_trace_and_only_after_it` is the one that makes *the golden session
reloads* worth saying: every sample up to Tick 1,024 is identical to the same session with no
transition, and every sample after it differs. Without that line the baseline could be covering a
reload that changed nothing.

**Two artefacts, because one of them cannot see three tables.** The session drives the simulation
through `step()` and is the more valuable of the two — it covers the Input Log, the phase order,
replay and the hash together. But the only verb applied before slice 7 is `Zone`, and a Zone command
creates a Lot; Buildings, Households and Citizens are reachable only through the cold API. Without
`world-hash.txt`, three of the four tables' saved columns would sit under no committed hash at all
and the baseline would be claiming coverage it does not have. When later slices give the player
verbs that build a city, the session absorbs the world fixture's job and this file can go.

**Slice 7 task 10a moved that boundary without dissolving it.** The session now opens with
`populate`, so it raises 121 Buildings, 360 Households and 1,000 Citizens on land the subdivider
carved for it (13 blocks of lattice row 0), gives every Building its
kind's Bins and Rule Instances, and runs the Rule engine for 2,048 Ticks — the first time the golden
trace has covered any of that. What it does **not** cover is what `GoldenFixtures.Build()` was
written for: a destroyed Household and a destroyed Citizen, so the allocator's free head and its
never-reused id counter are off their initial values. Nothing in a session can destroy a row yet.
`world-hash.txt` goes when slice 10's Zone Rules can demolish a Building, and not before.

**Slice 10 task 11 lengthened the session 256 → 2,048 Ticks, and it was to keep coverage rather than to
gain any.**
[`adr/0059`](../../../docs/adr/0059-a-zone-rules-sample-is-a-revisit-period-so-the-ruleset-states-a-duration.md)
derives a Zone Rule's sample from the Lot count, and at that fixture's 132 Lots the shipped one-Day
revisit period gives **one** Lot a trigger where the retired `sample = 4` gave four. Over the old eight
triggers the session condemned and never once landed on a Lot demolition had cleared, so the committed
trace **stopped covering `ZoneRuleEngine`'s create branch entirely** — and said nothing, because every
hash moved anyway. **That is the failure mode a baseline is structurally blind to**: it records what a
run *did*, so a change that narrows what the run *reaches* looks exactly like a change that moved it.
The cadence moved to **64** with the Tick count, holding the trace at thirty-two samples, and the reload
to **1,024** to stay halfway; `The_golden_session_raises_buildings_as_well_as_condemning_them` is the
assertion that makes the new length mean something rather than being a number somebody once chose. If
you shorten this session, that test is what will tell you. **5a-bis widened the same margin without
moving either number**: a real subdivider carves blocks, so the session peaks at **247** Lots against
132 and the derived sample is **2** rather than 1.

**Milestone 5a re-recorded all three artefacts, and the reason is worth separating into its two
halves.** `world-hash.txt` and `session-trace.txt` moved because `road_node` and `road_segment` joined
`World._tables`, so the composition order gained two tables at the end. Both **Ruleset content hashes**
moved independently, because each file gained a `[roads]` table — which is why
`GoldenFixtures.RulesetHash`, `GoldenFixtures.TunedRulesetHash` and the `ruleset` and `reload` lines
*inside* `session.borough` all had to change before the trace could be regenerated at all. **Four
literals in two files for one edit**, and the ordering is not optional: regenerating the trace against
a log that still names the old Ruleset produces a green run of the wrong session.

**5a-bis re-recorded all three again, and its lesson is about the session's *content* rather than its
hashes.** The Ruleset files gained a `[lots]` table and `LotTable` gained a saved `side` column, so
every number in this directory moved for reasons nobody needs telling. What needed telling is that
**a straight re-record would have quietly retired the `zone` verb from the baseline.** Zoning a Tile
now zones the *block* it falls in
([`adr/0077`](../../../docs/adr/0077-a-road-edit-is-one-segment-and-the-player-lays-streets-only.md)),
and eight of the session's eleven Zone commands named Tiles 0–31 — which is **one** block, and one
the populator had already carved. Eight of eleven commands would have become no-ops while the trace
came back full of freshly correct hashes.

**That is slice 10 task 11's finding on its second outing, so it is now a test rather than a
paragraph.** `GoldenSessionCoverageTests` asserts what the session *reaches*: every Zone command
carves a block nothing else has touched, a block stripped of frontage is refused, and a road edit
re-subdivides in both directions. **If you move a command in this session, that file is what will
tell you the move cost coverage** — `GoldenHashTests` structurally cannot.

**The session gained road edits for the same reason.** `connect` is applied as of 5a-bis, so a
committed session that never edits a road leaves the Epoch, re-subdivision and the frontage refusal
outside every hash here. Seven `connect` lines now bulldoze a block face, restore it and strip
another block entirely — four of them in one Tick, which is also the first time a per-Tick slice in
this session has held more than a pair.

**A guard existed for one Ruleset and not for its twin, and adding `[lots]` is what found it.**
`The_golden_ruleset_is_the_one_the_session_names` checked `minimal.toml` alone;
`GoldenFixtures.TunedRulesetHash` was a literal **nothing held to a file**. The catalogue pairs a
*stated* hash with a *loaded* file, so a stale literal resolves to the new content and no test
anywhere notices — the tuned hash had in fact been wrong from the moment the file was edited, and it
was the failure of its twin that prompted anyone to look. It is a `[Theory]` over both files now.
*A guard with no test is invisible to every future reader; a guard that covers one of two identical
files is worse, because the one it covers is evidence that somebody thought about it.*

**5b-bis task 4 re-recorded all three, and its lesson is that a re-record can be complete and still
leave a branch uncovered.** Both Ruleset files gained a `[jobs]` table, a `commute_budget_minutes` and a
`[[building]] jobs`, so both content hashes moved and every sample here moved with them — four literals
in two files again, in the order the 5a note gives. What the fresh numbers do **not** cover is the
Commute Budget refusing anything. The Budget is read off `--trips` over the shipped `[roads]`, which is a
property of the *map*; this session is 1,000 Citizens on ~120 Buildings in one contiguous strip, and every
pair in it is within twenty minutes' walk. At 10,000 Citizens the same Ruleset refuses a steady 48 walks a
census interval. **So the trace is entirely correct and the branch the number exists for is unreached** —
slice 10 task 11's finding arriving from a new direction, and `JobAssignmentTests` asserts the zero as
well as the mechanism so that the day the fixture changes, somebody has to read the note.

**5b-bis task 5 re-recorded all three again, and it closed a hole task 4's re-record could not.** Both
Ruleset files gained a `commute_peak_factor`, so both content hashes moved; the trace moved for a second
and better reason, which is that **the city now generates Trips**. Task 3 shipped the whole Trip model
outside the committed baseline — this session contains no `trip` command and never has — so `TripEngine`,
the Traveller cursor and every Fate were covered by unit tests alone. A *generator* fixes that without a
command, and `GoldenSessionCoverageTests.The_session_sends_people_to_work_without_a_trip_command` is what
says so.

⚠ **What it still does not cover is a second departure.** Everybody with a Workplace leaves once a Day,
spread over a window of `ceil(8192 / commute_peak_factor)` = **2,731** Ticks, and this session is **2,048**.
So the baseline reaches three quarters of the departure phases and **no Citizen in it departs twice**.
Covering that means lengthening the session past a Day — a change to the baseline rather than a line in a
test — and the note is here so that whoever lengthens it knows what they are buying.

**Milestone 12 task 3 re-recorded all three, and it is the 5a note's first half with nothing new in
it.** `district` and `district_cell` joined `World._tables`, appended, so the composition order gained
two tables at the end and every sample here moved. ⚠ **Neither Ruleset content hash moved**, because
neither file was edited — the new `[districts]` table is stated by `rulesets/twinned.toml` alone, which
no session opens on. So this was a two-file re-record and not the four-literals-in-two-files dance the
5a note describes; ***the two halves of that note are separable and this re-record is the proof.***

⚠ **What the fresh numbers do not cover is a District**, and they never will on these sessions. Both
open on Rulesets that state no `[districts]`, so both tables are **empty in every sample here** and
their contribution to the fold is the allocator's four scalars at their initial values. **That is a
real hole and it is covered elsewhere on purpose**: `FactorioTests` needed a **fifth** world —
`twinned.toml`, stepped — before a single one of the eleven new saved columns could be reached by
corruption, and `DistrictWatershedTests` is what asserts the derivation itself. ***A baseline records
what a run did, so a table no session fills is a table it is structurally blind to*** — slice 10 task
11's finding arriving from yet another direction.

**`World.HashSeed`'s version byte did not move, and that is deliberate.** It is for a change to the
*fold* — the composition order's rules, `Randomness.Mix`, what a column contributes — and not for a
world that has more tables in it. Bumping it for an appended table would make the byte a change
counter, at which point it stops distinguishing the one thing it exists to distinguish: a hash that
means something different from a hash of something different.

**Slice 10 task 7 arrived and the trigger named above turns out not to fire.** The session now
demolishes Buildings, so it frees Building, Bin and Rule Instance rows and hands them back out —
which is new coverage and is exactly what `world-hash.txt` was standing in for. It does **not** cover
what that file was written for. Demolition **evicts** a Household into the Unplaced Pool
([`adr/0054`](../../../docs/adr/0054-a-demolished-buildings-households-are-evicted-into-the-unplaced-pool.md));
it destroys neither a Household nor a Citizen, because destroying one would delete its Money. So the
free head and the id counter of those two tables are still at their initial values in every session
this project can record, and `world-hash.txt` stays. **The correct trigger is a session that can
remove a person** — emigration, or a Departure record — which is Phase 2 and not this slice.

**The session exists twice, and that is the codec's test rather than a duplication.** `session.borough`
is the artefact; `GoldenFixtures.Session()` is the same session built in C#. One test asserts they are
the same log and that the *parsed file* reproduces the committed trace — which is a stronger check
than any round trip the codec can run against its own output, because nothing writes the file first.
A codec checked only against what it just wrote agrees with itself by construction. This one is
checked against a file that was on disk before the run started, which is the situation a bug report
actually creates, and it is the property
[`adr/0039`](../../../docs/adr/0039-the-text-formats-are-a-fifth-project-not-a-core-exception.md)
bought a fifth project to guarantee.

If you change one, change the other. The test will tell you which line you missed.

## Re-baselining

A failure here is a question — *did you mean to do that?* — and the answer is often yes.

1. **Establish which kind of change it is.** A **design change** moved the city; an **optimisation**
   should not have. `05 §4`: *a change is an optimisation if the State Hash is unchanged, and a
   design change otherwise, however it was motivated.* **If you believed you were writing an
   optimisation, this test has just told you that you were not.** That is the whole reason it exists;
   stop and find out what moved before regenerating anything.
2. **Regenerate.** For the session, run the runner and commit the diff:

   ```
   dotnet run --project src/Borough.Headless -- \
     --log tests/Borough.Tests/Golden/session.borough \
     --ruleset rulesets/minimal.toml \
     --ruleset rulesets/minimal-tuned.toml \
     --ticks 2048 --hash-every 64 \
     --out tests/Borough.Tests/Golden/session-trace.txt
   ```

   **`--ruleset` is not optional here and the runner will tell you so.** The session records the
   Ruleset's content hash, so a run given none — or given a different one — is refused before it
   starts rather than quietly reproducing a different city. **Both are needed**, because the session
   reloads: a replay resolves each transition's hash out of what it was given, and a transition
   nobody supplied is refused rather than run under the wrong Rules. `--force-ruleset` does not
   waive that one — it waives a mismatch, and Rules nobody has are not a mismatch.

   The runner writes exactly the committed format, so this is a re-record rather than a
   transcription. For `world-hash.txt`, which is a hand-built world rather than a session the runner
   can play, copy the file out of the failure message — the test prints it in full.

   **There is deliberately no `--update-baselines` switch and no environment variable.** A baseline
   that can rewrite itself is one CI misconfiguration away from approving every change it sees, which
   is a baseline that has stopped being one. Regenerating is a command a person runs, and the review
   happens on the diff.
3. **If the *fold itself* changed, bump the version byte** in `World.HashSeed`. A change to the fold,
   to the composition order or to `Randomness.Mix` moves every hash in the project at once, and the
   signed seed is what distinguishes that from a regression. A change to the simulation's *behaviour*
   moves the baselines and leaves the seed alone.
4. **Say why, in the commit message**, in the terms this repository already uses: what moved, whether
   it was intended, and which document or ADR authorises it. A re-baseline whose commit message is
   *update golden hashes* is a re-baseline nobody can audit later.
5. **Never regenerate a baseline in the same commit as an unrelated change.** The diff is the record.

### The standing example

**Swapping softmax for Gumbel-max is hash-breaking and distributionally neutral.** The two draw from
the same distribution; the citizens make the same *kinds* of choices; every recorded hash moves
anyway, because the arithmetic that produced them is different. It is entirely safe to do
deliberately, with a re-baseline and a commit message that says so — and entirely unsafe to do
quietly, because from the far side of the change there is no way to tell it from the bug it hides.

That is the shape of every legitimate re-baseline: *the numbers moved, here is the sentence that
authorises it.*

## Editing the fixtures

`GoldenFixtures.cs` and `session.borough` are data that happen to be written in C# and in the log
format. Every number in either is load-bearing and tidying it is a re-baseline. If you need a wider
session — a new verb, more Ticks, a different cadence — that is a deliberate change under the
procedure above, not a refactor, and the cadence and Tick count are named in three places: the
fixture's constants, the trace header, and the command in step 2.

**Milestone 5c task 6 re-recorded `world-hash.txt` and `session-trace.txt` and moved **neither** Ruleset
content hash, which is the combination worth naming.** `route_hop` joined `World._tables` and `traveller`
gained `current_hop` and `carry`, so the composition changed and every number in those two files moved.
Both `[roads]`-era literals stayed put because **no shipped Ruleset states the new `[traffic]` table** —
so this re-record needed no edit to `session.borough` and none to `GoldenFixtures`, which is the first
time since 5a that a table could join the hash without four literals moving in two files first.

⚠ ~~**And that is also the coverage hole this re-record leaves, stated here rather than discovered later.**~~
**CLOSED BY MILESTONE 7 TASK 8**, by a second session rather than by this one adopting the file — see
*The driving session* below. Neither shipped Ruleset states `[traffic]` *or* `[households]`, so nobody in the committed session drives
and **the whole volume-delay mechanism sits outside every hash in this directory** — the same shape as
5b-bis task 3's *the golden session contains no `trip` command at all*. `SegmentVolumeTests`,
`VolumeDelayReachTests` and `TrafficRulesetLoadTests` are the only things that run it, and they say so in
their own remarks. The place both tables get stated together is **5c task 8's long run**, which is their
named ratifier; if that lands and this directory still covers none of it, the session is what wants
changing.

⚠ ~~**Task 7 shipped `rulesets/congested.toml` and the hole did not close, which is worth being precise
about.**~~ **CLOSED BY MILESTONE 7 TASK 8.** The paragraph's own prediction — *the session would have to
adopt it* — is what happened, with one correction: a **second** session adopted it, so nothing this one
covers stopped being covered.
 That file states both tables, so the mechanism now has a Ruleset — but the golden session loads
`minimal.toml` and reloads into `minimal-tuned.toml`, and neither states either table, so **nothing in
this directory has changed**. A Ruleset existing is not a Ruleset the baseline runs. The session would
have to adopt it, which is a decision about the committed trace rather than about congestion, and it is
5c task 8's to make with the long run in hand. `TrafficDumpTests` joins the three suites above as a
reader; the hash files still cover none of it.

**`adr/0101` re-recorded all four artefacts — including `session.borough` itself, which is the rarer
half.** A commute became two journeys, the departure phase became a function of the Workplace's Shift
band instead of the Citizen's id, and `CitizenTable` gained a saved `planned_commute` column, so
`world-hash.txt` and `session-trace.txt` moved for the ordinary reason: the composition changed and the
city behaves differently. What moved the log is that **both Ruleset files gained keys** — `[jobs]`
lost `commute_peak_factor` and gained `shift_hours_min`/`max` and `arrive_early_max_minutes`, and the
`dwelling` kind gained a Shift-start band — so both content hashes moved and the `ruleset` and `reload`
lines *inside* `session.borough` name numbers that no file produced any more. **Four literals in two
files, exactly the shape 5b-bis task 4's re-record had.**

**Milestone 6 task 1 re-recorded `world-hash.txt` and `session-trace.txt` and moved neither Ruleset
content hash — the same combination 5c task 6 named, and for the same reason.** `condemnation_trail`
joined `World._tables`, appended, so the composition gained a table at the end and every number in
those two files moved; no shipped Ruleset gained a key, so `session.borough` and `GoldenFixtures`
needed no edit and the four-literals ordering did not apply. The version byte in `World.HashSeed` is
deliberately **unmoved**: this is a world with more tables in it, not a change to the fold, which is
the distinction the note above draws.

⚠ ~~**What this re-record covers is the table's *existence*, not its use, and the gap closes in task 2.**~~
**CLOSED BY TASK 2.** For as long as task 1 stood alone the fresh numbers folded **256 + 1 rows of
zeroes** and would have been identical if `Record` were deleted — the shape `plans/0012`'s **check 10**
names, *a `Rows.Saved` column whose only assignment in the tree is under `tests/`* — and it was written
down here rather than left for the check, because ***a baseline that covers a table's declaration reads
exactly like one that covers its behaviour***.

**Milestone 6 task 2 re-recorded `session-trace.txt` alone, and the artefact that did *not* move is the
informative one.** `ZoneRuleEngine.Condemn` now copies the condemning Rule's condition into the trail
before `World.DestroyBuilding` frees it, so the committed session — which demolishes throughout —
writes real rows and every sample from Tick 128 on moved. **`world-hash.txt` did not, and could not**:
it is a hand-built world that never runs a Zone Rule, so it has no condemnation in it and its 256 + 1
rows are still zeroes. The two artefacts have separated for the first time on a *behavioural* change
rather than a compositional one, and that is the division of labour the top of this file describes
working as intended. Neither Ruleset content hash moved — no shipped file gained a key — so
`session.borough` and `GoldenFixtures` needed no edit and the four-literals ordering did not apply.

⚠ **Sample 0 is unchanged and every later sample moved, which is a fact about the fixture worth
keeping.** The first Zone Rule trigger that finds a Building past its threshold lands in Ticks 65..128,
so the trail is empty at Tick 64 and never empty again. **A session shortened below the second sample
would cover the mechanism exactly as poorly as task 1's re-record did**, with a full set of freshly
correct hashes to say so — slice 10 task 11's finding, in the one place it can still bite this table.

**Milestone 7 task 1 re-recorded all four artefacts, and it is the *first* re-record where a shipped
Ruleset gained a key for the baseline's own sake rather than for the mechanism's.** `car_park` joined
`World._tables`, appended, and `CitizenTable` gained a saved `parked_in` handle — so the composition
changed and both hash files moved for the ordinary reason. Both Ruleset content hashes moved because
**every shipped file gained a `[[building]] parking`**, which put this back on the *four literals in
two files* path, in the order the 5a note gives.

⚠ **The key was added to `minimal.toml` deliberately, and the alternative is the thing worth naming.**
`adr/0120` and `CarParkTable` could have shipped with **no Ruleset declaring parking at all** — the
table would have joined the hash, every number here would have moved, and **not one Car Park would
ever have been created.** That is `world-hash.txt`'s own milestone 6 task 1 note repeating one
milestone later: ***a baseline that covers a table's declaration reads exactly like one that covers its
behaviour***, and this time it was foreseen rather than found. With the key stated, the hand-built
world's four Buildings each carry a Car Park, so the fixture folds **four live rows** rather than
four zeroed ones and demolition frees them.

⚠ ~~**What it still does not cover is a Car Park being *occupied*, and the reason is a second file.**~~
**CLOSED BY TASK 8**, and the sentence below called the shape of it exactly — *it closes the same way or
not at all.* It closed the same way.

Occupancy needs somebody to drive, driving needs `[households] car_ownership_percent`, and
`minimal.toml` states none — so nobody in the committed session owns a car and every Car Park here is
permanently empty. That is **exactly 5c task 6's hole on a new mechanism**, and it closes the same way
or not at all: the session would have to adopt `congested.toml`, which is a decision about the
committed trace rather than about parking. The acquire and the release do not exist yet either; they
are task 4's, and task 5 is the one that makes a walk Leg cost something in this directory.

**`World.HashSeed`'s version byte is deliberately unmoved**, for the reason milestone 6 task 1 gives:
a world with more tables in it is not a change to the fold.

**Milestone 7 task 2 moved the two Ruleset content hashes and nothing else, and that is the honest
outcome rather than a near miss.** `[parking] radius_metres = 400` joined all five shipped Rulesets;
no table joined `World._tables`, no column was declared, and no behaviour changed — the shed that reads
this number is task 3. So `world-hash.txt` is untouched, every sample in `session-trace.txt` is
byte-identical, and the only line that moved in the trace is the header naming the Ruleset. ***A number
a Ruleset states and no mechanism reads does not move the world***, which is the clean case of the
distinction task 1's note had to make the hard way.

⚠ **It moved them twice, and the second time was a comment.** The first re-record was taken with a
loader guard that has since been withdrawn, and withdrawing it edited `minimal.toml`'s header — so the
content hash moved again for a change to **prose**. That is `RulesetHash` working exactly as designed:
it is a hash of the *file*, comments included, because a Ruleset's comments are how its numbers are
defended and a file whose defence changed is not the file the session ran against.

⚠ **`[parking]` sits before `[trips]` rather than at the end of the file, and a fixture is why.**
`TripCommandTests.RulesWithTripsTable` truncates the golden Ruleset at `[trips]` and asserts that what
goes with it is **exactly `[jobs]`** — which is a schema constraint rather than an accident, since a
`[jobs]` table is refused without a Commute Budget. Appending `[parking]` after them put a third,
independent table inside that deletion, and the assertion fired. ***A fixture that depends on a file's
section order is depending on something no document promises*** — the guard was written for exactly this
and caught it on its first occasion. `JobAssignmentTests.WithoutJobs` had the same dependency and was
repaired the other way, by excising the section it names instead of truncating the tail.

⚠ **The re-record was blocked before it could start, and the refusal was the right one.** The runner
would not replay a session naming a Ruleset hash nobody supplied — *Rules nobody has are not a
mismatch, and `--force-ruleset` cannot waive it* — so the two literals in `session.borough` and the two
in `GoldenFixtures` had to be corrected **before** the trace could be regenerated at all. That ordering
is not an inconvenience: a runner that had quietly played the session against whichever Rulesets it was
handed would have produced a full set of freshly correct hashes for a session that was no longer the
committed one, which is 5a-bis's finding wearing different clothes.

**Milestone 7 task 3 moved them twice more, and the second time was an ADR renumber.** The first was
ordinary: `[parking] shed_keeps = 24` joined all five shipped Rulesets with a long header defending it,
so both content hashes moved and the trace's `ruleset` line moved with them — a key and its prose, the
same shape as task 2.

⚠ **The second had no key in it at all.** This branch's two ADRs were renumbered `0112` → `0119` and
`0113` → `0120` to clear a collision with `main` and with `milestone-10-conserved-money`, and the sweep
that rewrote every citation rewrote **five ruleset comments** along with the ninety-odd document ones.
Both content hashes moved, the golden suite went red, and the change that did it was a **rename with no
semantic content whatsoever**. ***A renumber is a hash-moving change, and nothing about a renumber looks
like one*** — the diff is filenames and citations, the reviewer's whole attention is on whether the
citations resolve, and `CitationTests` was green throughout because every one of them did.

This is the *comments are content* rule above, arriving from the one direction it had not yet arrived
from. Task 2's second re-record was prose **about a number**, which at least sits next to the thing the
hash is for. This was prose about **where a document lives**. The rule survives intact and the reason it
should is unchanged — a Ruleset's comments are how its numbers are defended — but the working corollary
is worth stating on its own: ***any sweep that rewrites text across the repository must be assumed to
have touched `rulesets/`, and the golden suite is the only thing that will say so.*** Renumber first,
re-record second, and expect the second.

⚠ **And a mechanical trap in the re-record itself, which cost a red run.** `session-trace.txt` is a
content file copied to the build output, so the sequence *build → regenerate trace → `dotnet test
--no-build`* checks the **stale copy in `bin/`** against the fresh literals and fails with a hash
mismatch that looks exactly like a real regression. The regenerated trace has to be followed by a build
before the suite means anything. ***A `--no-build` run is only as fresh as the last build, and that is
true of the fixtures as well as of the code*** — the same defect as the `const` this session hit two
commits earlier, where a stale binary reported the pre-edit `RulesetHash` and the failure message
quoted a number that no longer existed anywhere in the tree.

**Milestone 24 task 4 moved THREE Ruleset content hashes and NOT ONE State Hash sample, and that
combination is the one worth naming here.** Sealing's decay was scheduled for the first time — a third
`LayerCadence`, a `DecaySealing` call in `MapLayers.Step`, and the rate moved out of `[layers]` onto
each `[[terrain]]` table — so twelve shipped Rulesets lost a key and gained a comment, and
`minimal.toml`, `minimal-tuned.toml` and `congested.toml` are three of the twelve. ⚠ **The city did not
move, and it could not have**: `DecaySealing` returns immediately when the Ruleset states no
`[[terrain]]`, and **none of the fifteen shipped files except `rulesets/varied.toml` states one** —
which is not a fixture here. So `session-trace.txt`, `driving-session-trace.txt` and `world-hash.txt`
were **not** regenerated; what was edited is the four literals in two files, plus the `ruleset` header
line each trace carries. ***A comment edit to a shipped Ruleset moves a file fingerprint and never a
State Hash***, which is the distinction `adr/0100` and the repository map both spend a paragraph on,
and this is the cleanest example of it the directory has: a scheduled mechanism landed, and the
committed city is bit-identical.

**Milestone 24 task 8b moved every State Hash sample and NOT ONE Ruleset content hash, which is the
exact complement of the entry above it.** `WoodlandCellTable` gained a second saved column,
`potential` — what the seed laid, which regrowth climbs back toward — so the composition changed and
every number in all three artefacts moved from the first sample on. **No shipped Ruleset was edited
except `rulesets/varied.toml`**, which is not a fixture here, so `session.borough`,
`driving-session.borough` and the four literals in two files needed no touch at all.

⚠ **The two entries together are the cleanest statement of the distinction this directory keeps.** Task
4 changed twelve Rulesets and no city; task 8b changed one column and every city. ***A file fingerprint
and a State Hash answer different questions, and a re-baseline that moves one and not the other is the
normal case rather than the surprising one.*** The version byte in `World.HashSeed` is unmoved in both:
a table gained a column, which is a world with more state in it, not a change to the fold.

## The driving session

**Milestone 7 task 8 added a second session, and the reason it is a second rather than a wider first is
the whole decision.** Two paragraphs above have been standing open — 5c task 6's *the whole volume-delay
mechanism sits outside every hash in this directory*, and milestone 7 task 1's *what it still does not
cover is a Car Park being **occupied*** — and both diagnosed the same cause and named the same repair.
Driving needs `[households] car_ownership_percent`; parking needs somebody driving; **`rulesets/congested.toml`
is the only shipped file stating that key**, and `minimal.toml` states no `[households]` table at all by
design. Both paragraphs said the session would have to adopt the file, and both deferred it, *because it
is a decision about the committed trace rather than about the mechanism*.

**Adopting it in `session.borough` would have paid for the new coverage with the old.** That session
zones eleven blocks, edits seven road faces, reloads halfway and is held to what it reaches by
`GoldenSessionCoverageTests`; moving it onto a different Ruleset re-baselines all of that and puts every
one of those claims back up for re-derivation, in a commit whose subject is parking. **A second session
costs one file, one trace and one fixture method, and costs the first session nothing.**

| | `session.borough` | `driving-session.borough` |
|---|---|---|
| Ruleset | `minimal.toml`, reloading into `minimal-tuned.toml` | `congested.toml`, no reload |
| Length | 2,048 Ticks — one Day | 4,096 Ticks — **two** Days |
| Cadence | every 64 Ticks, 32 samples | every 128 Ticks, 32 samples |
| Commands | `populate`, 12 × `zone`, 7 × `connect` | `populate`, **and nothing else** |
| What it is for | the player's verbs, the Rule engine, the reload | **what a populated city does**: driving, congestion, parking, the walks at each end |

**The emptiness of the second session is the design and not a stub.** Every player verb is already under
a committed hash in the first; copying the commands across would buy no coverage and would create a
second set of block coordinates to hold in agreement with a lattice — `plans/0012` **Cause 1** by
construction, in the one directory whose entire purpose is noticing when two records disagree.

⚠ **Two Days rather than one, and the count is the argument.** A Citizen's first car journey walks to
their own Building's kerb, because they hold no space — so the departure walk is zero by construction
and only the *arrival* walk costs anything until somebody has parked away from home and come back for
it. Measured by `--parking` on this Ruleset at this population: **0 of 975** departure walks leave their
starting Address at half a Day, **2 of 1,883** at one Day, **136 of 3,330** at two. One Day is not
vacuous; it is worse — it clears the assertion on two walks, which is a coverage claim one tuning change
away from being false with nothing in the suite to say the claim had narrowed. That is this file's own
standing finding arriving as a choice of length rather than as a surprise.

**`GoldenSessionCoverageTests.The_driving_session_walks_to_and_from_a_car` is what makes the trace mean
something**, and it is milestone 7's Definition of done item stated as a test: *the walk Leg's cost is
non-zero for at least one Citizen in the committed golden session.* A trace cannot say that — every
sample in `driving-session-trace.txt` would be just as green over a city in which every Car Park sat
empty and every walk cost nothing, which is exactly what the *first* session's numbers are. It observes
every Tick rather than sampling, because `TripEngine.Release` frees a Leg as its Trip resolves and no
instant holds them all.

⚠ **What it still does not cover is a city where parking is scarce.** `congested.toml` declares
`parking = 8` per `dwelling`, which at this population is **1,952 spaces against 4,000 drivers** — and
the run's peak holding is **1,326**, sixty-eight per cent of supply. So the sheds are working well
inside themselves, `adr/0009`'s gradient is being walked at its shallow end, and the refusal at the far
end of it is not something these hashes can be trusted to have reached. ⚠ **Nor can they say it was
never reached**, and that is the sharper half: a refusal presents as `TripFate.ExceededCommuteBudget`
(`adr/0009` refused a *no parking* Fate on purpose), so a locally exhausted shed and a genuinely long
commute arrive down the same channel and nothing in this directory tells them apart. **The Ruleset that reaches them is `rulesets/scarce.toml`**, and the file that measures
them is `ParkingScarcityTests`, which is an instrument. Adopting a third session for it would be a third
baseline to re-record on every change to the fold, for a branch a test already walks deliberately; the
note is here so that whoever wants it knows what it costs and that nobody forgot.

### Re-baselining the driving session

Step 1 and steps 3–5 above are unchanged. Step 2's command is different, and the difference matters:

```
dotnet run --project src/Borough.Headless -- \
  --log tests/Borough.Tests/Golden/driving-session.borough \
  --ruleset rulesets/congested.toml \
  --ticks 4096 --hash-every 128 \
  --out tests/Borough.Tests/Golden/driving-session-trace.txt
```

**One `--ruleset`, not two.** This session does not reload, so there is no transition to resolve and a
second file would be Rules nobody names. And **`congested.toml` is now a baseline artefact**: it was a
demonstration Ruleset that only three suites read, and editing it — including editing its comments, and
including a repository-wide citation sweep touching them — now moves a recorded **Ruleset content hash**.

⚠ **CORRECTED 2026-08-20, milestone 9 task 3: that is a file fingerprint, not a State Hash, and the two
were being said with one word.** Adding four `[layers]` keys to all eight Rulesets moved **eight header
lines** — three constants here, the `ruleset` line in both `.borough` logs, the `ruleset` line in both
traces — and **not one of the thirty-two State Hash samples**, because a key nothing reads cannot change
the city. A *semantic* edit to a key something reads does move them, and that is a different sentence.
***Saying "moves N committed hashes" of a fingerprint change is Cause 5 with the units dropped***, and it
is the sentence that makes somebody defer a Ruleset edit — which `adr/0100` forbids by name.
`The_golden_ruleset_is_the_one_the_session_names` covers all three files and fails with the number to
paste in.

**Milestone 27 task 10 re-recorded ALL THREE, and the contrast with task 6 four paragraphs down is the
note.** `BusinessTable` gained a saved `origin` handle — the premises that instantiated a Business
(`adr/0148`, amended) — and **both session traces moved as well as `world-hash.txt`**, where task 6's
saved `kind` byte moved only the world fixture. ***The difference is not the column, it is who writes
it***: `kind` is written by `CreateBusiness`, which no session reaches, and `origin` is written by
`World.Fit`, which every session that raises a Building runs. **A saved column moves the traces exactly
when a session's own verbs write it**, which is a better rule than counting columns.

⚠ **Neither Ruleset content hash moved**, because no baseline Ruleset was edited. `rulesets/founded.toml`
and `rulesets/levied.toml` were both rewritten in the same commit and neither is a baseline artefact.

**Milestone 27 task 6 re-recorded ONE artefact, and which one is the whole note.** `BusinessTable`
gained a saved `kind` byte — the trade, on `adr/0141`'s second kind namespace — so `world-hash.txt`
moved. ⚠ **Neither session trace moved and neither Ruleset content hash moved**, and the reason is
worth keeping: `GoldenFixtures.Build()` is the **only golden world that contains a Business at all**.
The sessions run `populate` over `minimal.toml`, nothing in the simulation creates a Business
(`World.CreateBusiness` has no production caller until task 8), so both sessions hold an empty
`business` table and a column added to a table with no live rows folds nothing. ***A saved column can
therefore move one baseline and not the others, and that is the tables being reached differently
rather than a hash being missed.***

**The fixture was changed before the number was taken, and that is this file's own lesson applied.**
A straight re-record would have given both Businesses `kind = 0`, which covers the column's
*existence* and never a trade's *value* — the baseline would move on the day the column landed and
stay put for ever afterwards however the trades were shuffled. That is *"a re-record can be complete
and still leave a branch uncovered"* arriving for the fourth time. The two Businesses now carry
**kinds 1 and 2**, on exactly the reasoning that made them two rather than one: a fixture holding the
default leaves the machinery uncovered. The two hashes differ — `0xD92277A7748AC804` at kind 0 against
the committed `0xD32D3335AA6D991E` — which is the proof the value is folded rather than only the
column.

⚠ **Both trades are DERELICT in this fixture and that is deliberate rather than sloppy.**
`minimal.toml` declares no `[[business]]`, so ids 1 and 2 name nothing under the Ruleset this fixture
loads. Dereliction is a legal state for a trade exactly as `02 §4.3` gives one to a Building, and it
is the honest one here, because **nothing creates a Business from a declared kind until milestone 27
task 8**. What this fixture does *not* cover — a trade with a name behind it — is covered by
[`rulesets/tenanted.toml`](../../../rulesets/tenanted.toml) and `BusinessKindLoadTests`, which is
where to look for it rather than here.
