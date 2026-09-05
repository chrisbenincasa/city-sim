# 0045 — The amnesty

**Read this, not the board. The only thing in flight.**

Opened 2026-08-26. **Ends at a ratio, not on a date: 30 words of prose per line of simulation.**
**52 at 2026-08-31.** `CorpusBudgetTests.The_amnesty_has_not_yet_earned_its_end` goes red the day
it is earned, and that red is the report. One page, and it stays one page.



---

## The situation

**1.17M words of prose against 17,872 lines of executable simulation.** 169 ADRs, one per 106 lines,
30 of them in five days. **236 of 524 commits changed no code.**

🔴 **And that count was low by 636,000 words.** Doc-comments under `src/` and `tests/` are **35% of
all the prose here** and no corpus check could see one: `CLAUDE.md` says the checks are all
document-to-document. The three `Borough.Core` files the amnesty bought are **56–66% comment by
line**. Counted since 2026-08-30 — *the ratchet was reporting a win on the half it could measure.*

Nobody ages, is born or dies. Wages are unbuilt, so money flows one way into Businesses. Every
shipped world decays. No renderer, no plan for one. The corpus grows with elapsed time and has no
sink — `adr/0006`, violated by its citers.

---

## Standing orders

Until the ratio is earned:

1. **No new ADRs.** `CorpusBudgetTests` reddens the build if `docs/adr/` passes 169.
2. **No new entries in `plans/0002` §A–§F.** Frozen at its 2026-08-26 size.
3. **Prose grows only as fast as the simulation it describes.** `CorpusBudgetTests` caps the
   **ratio** — all prose, doc-comments included, over non-comment `src/` lines — at **52** as of
   2026-08-31, so a page written beside a mechanism is free and a page written alone is refused.
   ⚠ **It is a RATCHET and the number moves down**: it opened at 57, the flood put simulation in
   the denominator, and the ceiling followed the measurement the same day. ***A ratchet not lowered
   to the reading banks the gain as slack to spend on prose later.*** ⚠ **This is not a
   claim that a remark beside code is waste**: `adr/0093` asks for exactly that prose, and doc-
   comments are on the numerator only so that prose cannot escape `docs/` by relocating.
   🔴 **It replaced two absolute word ceilings on 2026-08-30**, which went red on all four commits
   that *improved* the ratio and were raised by their own author every time. ***A ceiling that
   reddens on the work paying down the debt is measuring the wrong side of the fraction.***
4. **`adr/0043` and `adr/0052` are suspended.** Choose numbers by taste, stamp them `PROVISIONAL`,
   open no §D row, name no ratifier. *Ratification needs a city and the city needs the numbers, so
   they now prevent every commitment, not only premature ones.*
5. **A session ending with no change under `src/` is not committed.**

To break one, delete its test in a commit saying why — visible, not hard.

---

## Owed when the freeze lifts

🔴 **THE AMNESTY COULD REFUSE A QUESTION AND HAD NOWHERE TO RECORD REFUSING IT.** Standing order 2
seals `plans/0002` §A–§F, which is *the* document for **what needs answering** — so a question raised
under the freeze had a correct home it was barred from, and the only place left was a findings
section nobody reads once the board stops pointing here. ***A freeze with no deferred list loses
exactly the questions it was right to postpone.***

**This list is that sink. Each row is filed in [`0002`](0002-open-questions.md) on the day standing
order 2 expires, and struck from here.** ⚠ **It is a queue and not a discussion** — a row names the
question, its type and its trigger, and never argues it.

| Question | Type | Trigger to open it | Raised |
|---|---|---|---|
| **Should two children pair into one Household, or does each form its own?** `World.SpawnChildren` gives every child its own Household and nothing in the build pairs anybody, so a formed Household holds **one adult** and exact replacement is **one child** — where [`adr/0011`](../docs/adr/0011-household-life-stages-and-self-generating-population.md) derives the threshold as **two**, *"two children replacing two adults"*, which is airtight for a Household of two. ⚠ **The ADR is not wrong; it assumes a mechanism nobody built.** What is owed is the decision and not the arithmetic: ***who pairs with whom***, and whether the answer is a marriage market, a draw at formation, or nothing at all. ⚠ **The readout is already honest** — `--stages` measures `adults per Household` and takes the threshold from it — so nothing is blocked, and what is at stake is whether a Household is a person or a family. Goes to §C under `adr/0011` | *arguable* — no measurement says how many adults a Household ought to hold; the census already says how many it does | **A mechanism that treats two adults in one Household differently.** `plans/0046` decision 3 names exactly that as `Citizens.Age`'s revisit trigger, and it is the same trigger: ***until something can tell two adults apart, a pair and a single are the same row twice*** | 2026-08-31, the 1,200-Day run |
| **What is the natural playing speed, and what does a player do at each rung?** Two sittings settled that ***slower rungs are necessary*** and nothing about where the natural one sits. What is owed is four things at once: the **phases** — early, mid, long — and whether they are a design claim or a description; the **cadences that have emerged** and which phase each belongs to; **which rung a player reaches for and when**; and whether *fun* and *legible* want the same rung. ⚠ ***A ladder is a list of speeds; this is a question about what a player is doing at each one.*** Goes to §C under `01-player-experience.md` | *arguable* — no number refutes *this is the speed the game is played at*, and `adr/0094`'s revisit trigger names **a person at the controls**, which is a sitting rather than a machine | **A game with phases in it.** Asking now answers from a city that has one phase, and ***a pacing question answered against a world with nothing to pace is a stopwatch reading rather than a design*** | 2026-08-31, the shell's second sitting |
| **What does the ground SAY, and may it say anything that is not a readout?** The drawing's remaining budget is better spent on the ground than on more massing — a Building is a box with a roof and the shader already varies its face, while the ground is one flat colour under the whole city. ⚠ **The question is not what to draw but what drawing it CLAIMS.** Under `LEGIBLE CAUSE`, ground that varies without meaning is decoration and ground that varies with Sealing, land value or wear is an instrument a player reads from a kilometre up — and `01 §683` already refuses an overlay sharper than the player's ability to act on it. ***Which of the two this project is buying has to be settled before anything is drawn***, because the two are indistinguishable in a screenshot and opposite in what they promise. Goes to §C under [`07-the-drawing.md`](../docs/07-the-drawing.md), **with [`0049`](0049-visuals.md) *What the ground may say* and its `F46`–`F52` attached** — investigated 2026-09-04, still open, and the argument is filed there rather than here because this table is a queue and not a discussion | *arguable* — no measurement says whether a texture is a claim; what settles it is what the player is entitled to infer from one | **Anything drawn on the ground.** The first decal, kerb or wear mark commits the answer whether or not anybody chose it, which is why this is raised before the work rather than during it  | 2026-09-04, the appearance sitting |
| ~~**Should the lattice a player's Streets SNAP to be uniform?**~~ **ANSWERED 2026-09-04 by the player, the same day it was raised: NO.** ⚠ **So it left this table without ever being filed** — a sink for questions the freeze could not answer is not where an answered one belongs, and ***a decision left sitting in a list of open questions is read as still open***. Promoted to queue row **25**, which carries the decision and what it costs. The tombstone stays rather than the row being deleted, for row 17's reason: a struck row costs a line and a renumber costs a citation | — | — | 2026-09-04, and closed the same day |

---

## Definition of done, amended

Hexadecimal was satisfying `CLAUDE.md`'s *"something to look at"*.

> **Done means you watched it and were surprised.**

---

## The queue

Ordered; reordering deletes this.

🔴 **Rows 8–11 were written and replaced within the hour, and the reason is a trap worth keeping.**
The first set was a dwelling sink, a Ruleset window, a tenant's middle and a renderer — chosen
because a failing test or an instrument named each one. ⚠ **But a queue assembled from failing tests
finds only the mechanisms that HAVE tests, and nothing unbuilt has one.** Every row was a repair to
something that already ran. ***This page was opened to build the simulation, not to finish it.***
The rows below were found by asking the code what is missing instead.

🔴 **AND IT HAPPENED A SECOND TIME ON 2026-08-31, WITH THE SAME CAUSE WEARING A DIFFERENT COAT.**
Rows 15–18 were written and re-scoped within the hour. This time they were not repairs — every one
named a mechanism that genuinely does not exist — but every one was ***named by a symbol***: a column
with no reader, a call site that only ever fires one way, a remark quoting its own missing half.
⚠ **A queue assembled from symbols finds only what fits in a `grep`, and a swath does not.** The first
list found things with tests; the second found things with names. ***Neither question asks what the
project is missing at the size of a milestone.*** Asking that one instead produced 15a–15e: the shell
issues **no `Command` at all** — `Command`, `CommandKind` and `TickInput` are zero references in
`Main.cs` — so all six player verbs are applied in `Simulation` and reachable only from an Input Log
or a test.

🔴 **AND THE RE-SCOPE NEVER HAPPENED TO THE OTHER FOUR. THEY WERE RENUMBERED.** The sentence
above says *rows 15–18 were written and re-scoped within the hour*, and the first half is true.
`69e53fe`'s queue read **15** *a Need has no consequence*, **16** *nobody moves house*, **17** *a
dwelling costs nothing*, **18** *the Day is a comb* — and today's rows 16–19 are those four
**word for word**, diffed on 2026-09-01, the only changes being one whitespace in an empty cell and
one internal cross-reference bumped. ***The re-scope consisted of inserting a properly sized row 15
in front and shifting the old list down one place.*** ⚠ **The paragraph explaining that a
symbol-derived queue is too narrow was written in the SAME COMMIT as the rows it describes**, and the
rows outlived it. ✅ **Paid on 2026-09-01**, and the repair is a **grammar** rather than a
rewrite: 15a–15g name *capabilities* — a click becomes a Tile, the session is a log —
and 16–19 named *absences*. ***An absence is one symbol wide by construction and a capability is
not***, which is why the two sets read as different sizes in one document by one author on one day.

⚠ **A NUMBER HERE IS AN IDENTITY AND THE TABLE'S POSITION IS THE ORDER.** The shift above broke
two cross-references — *became row 18* appears twice below and meant the comb, which by then was
19 — and the done rows already sit out of numeric order (`15g` above `15e`). ***So nothing is
renumbered again***: a merged row keeps the lowest number, a folded one stays as a struck tombstone,
and a new row takes the next number wherever it belongs in the order.

**The risk 15 retires:** *that the design's central claim has never been tested by a person.* Pillar 3
is **govern, don't place**; nobody can govern, and nobody can place. Every pillar and every anti-goal
in `00` is asserted in prose and unexercised, and a city nobody can act on is a simulation rather than
a game.

**The risk 16 retires:** *that a Household's circumstances have never changed what it does.* Every
move in this city is something done **to** a Household — evicted, starved out, condemned over — so
`adr/0027`'s preferences, `adr/0011`'s life stages and `adr/0069`'s placement are all wired to a
population that never chooses. ***A simulation of people who only ever react is not the pillar.***

**The risk 20 retires:** *that one Trip generator has been standing in for a city's whole day.* The
Commute Budget's three rungs, the volume-delay function's calibration and every congestion figure in
the corpus were measured on the commute alone, and ***a number measured against one generator is a
property of that generator until a second one exists.*** ⚠ **Three other swaths were sized and passed over**, each verified rather than assumed, and
they are the next candidates: **traffic's second tier** (`Lane`, `Stress`, `Microscopic` are zero
non-comment references in `Borough.Core`, and `RoadSegmentTable.Fidelity` is a `Derived` column
`RoadGraph` sets to 0 and nothing ever raises — row 2's shape again); ~~**the shopping occasion**~~
✅ **PROMOTED TO ROW 20 on 2026-09-01**, which is the point: it was sized and verified here and
then sat in a paragraph for a day, and ***a row in a paragraph is a row nobody picks up***; and
**the eye's other half** (`ChunkAggregates` 0 references, `Notification` 0
non-comment references, **19 headless dumps against a three-line on-screen readout**).

| | Work | State |
|---|---|---|
| 1 | `CorpusBudgetTests`, this page, the `CLAUDE.md` pointer | ✅ 26-08 |
| 2 | Write `Citizens.Activity` — saved, hashed, per-Tick, **no writer** | ✅ 26-08 |
| 3 | `--day` — one Citizen, one Day, off `Evidence.OfCitizen` | ✅ 26-08 |
| 4 | ~~Nobody comes home~~ — misread; real defect fixed | ✅ 27-08 |
| 5 | ~~Held parking drifts upward~~ — a ramp, not a drift; `ParkingLongRunTests` owns it | ✅ 27-08 |
| 6 | ~~Wages~~ — `waged.toml`; arrears got a sink | ✅ 27-08 |
| 7 | Life Stages and self-generation — [`0046`](0046-life-stages-and-a-self-generating-population.md) | ✅ 5/5 |
| 8 | **`Govern` throws.** `PolicyEngine.Sweep` runs and a Ruleset can declare a `[[policy]]` — but the verb letting a **player** set one hits `InvalidOperationException` at `Simulation.cs:440`. ⚠ **Two of the six verbs are declared and unapplied**; this is the one with a whole mechanism already sitting under it | ✅ 30-08 |
| 9 | **Needs, and the preference axes.** `Taste` **0 files**, `Preference` **0 files** in `Borough.Core`. `adr/0027` calls them *"the most load-bearing data in the design"*. ⚠ **This row said placement *satisficed on distance alone* and that was WRONG** — it did not satisfice at all, and `adr/0069` says so itself: *"no acceptance filter, no sampler bias, no scored choice, no `μ`"*. A Household had no preference **and** no mechanism that could have expressed one | ✅ 31-08 |
| 10 | **`Service` throws.** The civic swath — schools, health, safety. `School` is **0 files**. ⚠ **After 9, not before**: a service with no need to satisfy is a Building with a Bin | ✅ 30-08 |
| 11a | **The eye.** Of `05 §2`'s three hot queries only `LayerCells` exists; **`VisibleAgents` and `ChunkAggregates` are nowhere in `src/`** and a **Traveller has no coordinate at all**. ⚠ **Day one of 11b either way** | ✅ 30-08 |
| 11b | **The shell.** `src/Borough.Godot` — Godot 4.7.2, a camera over the city, one MultiMesh per kind of thing, a speed ladder and a readout. ⚠ **Not in `Borough.slnx` and never will be**: that absence is what enforces *the headless runner never requires Godot* | ✅ 30-08 |
| 12 | **Disasters.** `coastal.toml` carries a Hazard Region and **nothing fires on it** | ✅ 31-08 |
| 13 | The `0046` loose ends — the dwelling stock's missing sink, `aged.toml`'s narrow windows. ⚠ **Small on purpose and last on purpose**: `StageDumpTests` pins both with tests that assert the defect, so neither can be lost | ✅ 31-08 |
| 14 | **The audit.** Walk this page for what it recorded as owed and never paid. ⚠ **A findings section is not a ledger**, and a debt written in a narrative paragraph is a debt nobody sums | ✅ 31-08 |
| 15a | 🔴 **Picking.** A click becomes a Tile. The camera is an orbit, and **nothing in the shell converts a screen position to anything at all** — no ray, no ground-plane intersection, no Tile. ⚠ **Every row below is blocked on this one**, and it carries a hover readout of its own: ***a verb you cannot aim is a verb you cannot test***, so what you are pointing at has to be on screen before anything commits | ✅ 31-08 |
| 15b | **`Zone`, `Connect`, `Demolish` — the three that change the ground.** ⚠ **`Zone` SUBDIVIDES and does not paint** — found by building it. ⚠ **`Demolish` addresses the LOT's Tile and never the cursor's**, because `ApplyDemolish` matches exactly and refuses rather than substituting | ✅ 31-08 |
| 15c | **`Govern` and `Service` — the two that change the rules.** A governing panel (`p`) per `[[policy]]`, and `s` places a `serves` kind. ⚠ **The panel is NOT the tuner** — one edits the world's premises, the other plays the game. ⚠ **`RulesetNames` had no Policy accessor**, so a panel could only have offered *policy 0, policy 1* | ✅ 31-08 |
| 15d | **The session is a log.** `Populate` now enters through `Simulation.Apply`, every verb is appended at the Tick it applies, and `w` writes a `.borough`. ⚠ **The shell was putting a whole city in by a door the log does not account for**, so every hand-played session would have replayed against an **empty world** | ✅ 31-08 |
| ~~15d~~ | ~~🔴 **The session is a log, and this is the row that makes the other three worth anything.** The shell writes `.borough` through `Borough.Formats` — ⚠ **using the codec and never implementing one** (`adr/0039`). ***A verb that is not in the log is a state change no replay reproduces and no State Hash divergence explains***, which is the sentence `Populate` and `Arrive` each already carry in their own remark. **The proof is the round trip**: a city played by hand in the shell, replayed in `Borough.Headless`, same State Hash~~ | ✅ |
| 15g | ⚠ **Tool selection needs a UI, and the keyboard-only mode line is already unintuitive.** Raised by the player on 2026-08-31, unprompted, at five verbs — `z x b s p` plus two cycling sub-selections (`z` cycles zone rules, `s` cycles service kinds) reported through **one line of text**. ⚠ **It is a legibility row and not a mechanism row**: nothing new reaches `Simulation.Apply`, so ***the whole of it is shell*** and the standing no-test-host limitation covers all of it. Related to `15e` — a refusal a person can read and a tool they can see are the same complaint | ✅ 31-08 |
| 15e | **A refusal reaches the player as a sentence.** `Simulation.Refuses` answers off the applier's own predicate and returns a **number**; the shell owns every word. ⚠ **The shell had guarded THREE of the ten that belong to the five verbs it issues**, by restating the rule in its own words. ⚠ **The grammar gained `hold` and `click`**, because a refusal nobody can make happen is a refusal nobody has seen | ✅ 31-08 |
| 15f | 🔴 **A CLASS WENT MISSING FROM THE COMMIT GATE AND NOTHING NOTICED — AND BOTH HALVES OF THAT SENTENCE WERE WRONG.** ***Nothing went missing; it had not arrived.*** `c2e9ff3`'s tree holds **zero** `RulesetSchemaTests` — `git ls-tree` says so — so its `Total: 2496` was a complete run of the tests that existed, and the class was authored **19 minutes later** in `4e902cb`. ***And something did notice***: the push lane went red on the pushed tree at 20:39 UTC, `Failed: 1, Passed: 2497, Total: 2498`, naming this class, and `63bb181` fixed it six minutes later. ⚠ **The cause is a REBASE, which is the candidate nobody listed because it is not a property of the run at all**: the work was replayed onto `4e902cb` as `1c24c05` at 16:39, and that tree carries a new loader key **and** the test that checks the schema for it. ***Each parent was green on its own tree; the child was red and no local gate ever ran on it.*** ✅ **What survives is paid in `scripts/test.sh`** — a run now names the **tree** it gated and counts what it **collected**, comparing against the last run of the same lane — ⚠ **outside the suite on purpose, because a test cannot report that it did not run**: the report would not have been collected either | ✅ 01-09 |
| 16 | 🔴 **A HOUSEHOLD LEAVES A HOME IT CAN DO BETTER THAN.** ⚠ **This is rows 16, 17 and 18 as they stood, merged on 2026-09-01, and the merge is the finding rather than a tidy-up.** Each was written as an **absence** — *a Need has no consequence*, *nobody moves house*, *a dwelling costs nothing* — and ***each is inert without the other two***: a reason to leave with no door, a door with no criterion, a price nobody can act on. ⚠ **The page already contained the sentence that joins them and listed three rows anyway** — the *dwelling costs nothing* row's own *`rent` is the thing that would make [the] move a decision instead of a shuffle.* ***Three absences at one symbol each was a milestone cut into three parts too small to start.*** | ✅ Rent, voluntary reassessment and shortage consequences implemented; see *What housing consequences found* |
| 16a | **A dwelling has a price and a Household can be short of it.** `adr/0027`'s **third preference axis**, and the only one of the three that needs no new world: `taxed.toml` already puts money in Households, where `minimal.toml` holds every one at exactly zero. `PlacementEngine`'s own remark names the hole — *"acceptance needs rent, a commute and a tolerance; none exists, so any member would take any dwelling."* ⚠ **One of the three has shipped since that remark was written** — `EmploymentEngine` says *"this is where the commute exists"* — so it is two thirds true rather than wholly. ⚠ **First, because the other two have no criterion without it**, and because it is what makes `choosy.toml`'s centrality numbers ratifiable at all: ***a preference for the centre means nothing until the centre costs more*** | ✅ `70b8ec3`: rent and placement affordability |
| 16b | **A housed Household enters the Unplaced Pool by choosing to.** Four call sites reach `World.Unplace` — over-capacity eviction, the premises emptying, the tenant's decline threshold, and shedding — and ***every one of them is the Household LOSING its home***. ⚠ **`choosy.toml` had to be built on `declining.toml` for exactly this reason**: a preference about where to live is unreachable for anybody already living somewhere, and `adr/0011` calls life stage *"one of the primary drivers of residential mobility"* against a build that has none. ⚠ **A second door into the Pool needs a sink and the loader already knows it** — `[placement] gives_up_after_days`, refused today for any file declaring `children_become` without one (`adr/0006`) | ✅ `4f06dad`: affordability reassessment and Pool sink |
| 16c | **A Household that goes short does something about it.** `Sustenance` and `Satisfaction` are saved, hashed, degraded on a duration and recovered on supply — and **the only thing in `src/` that reads either is `Evidence`**, which is a panel. ***A Household starves to the floor and nothing in the city is different.*** ⚠ **Last of the three, because a consequence needs somewhere to go**: after 16a and 16b, going short is a reason to move and the move has a price to compare against; before them it is a number somebody has to invent a use for. ⚠ **After 9 and 10 and because of them** — those built the reading, and a reading nothing acts on is an instrument rather than a mechanism | ✅ `PlacementEngine.ShortagePromptsMove`; guarded shortage/recovery run observed |
| ~~17~~ | ~~🔴 **Nobody moves house.**~~ **Folded into 16b, 2026-09-01.** ⚠ **The number stays as a tombstone rather than closing the gap**: two cross-references below already broke when this queue last shifted, and a struck row costs a line where a renumber costs a citation | — |
| ~~18~~ | ~~🔴 **A dwelling costs nothing.**~~ **Folded into 16a, 2026-09-01**, for 17's reason | — |
| 20 | 🔴 **A CITIZEN GOES SHOPPING.** `TripPurpose.Shopping` is declared with a full `adr/0067` remark and **`Shopping = 1` is its only non-comment reference in the whole of `src/`** — counted 2026-09-01. Nothing starts one. ⚠ **So every Commute Budget rung, every congestion figure and every Trip cost in this corpus is calibrated against a SINGLE generator**, which is the largest unexamined claim the amnesty has written down. ⚠ **It is also what makes `provisioned.toml`'s seller a shop rather than a Bin**: `adr/0171` gave the market a price that moves and ***no Citizen has ever walked to one***. 🔴 **It was sized, verified and left in a PARAGRAPH** — the preamble's passed-over swaths — and a row in a paragraph is a row nobody picks up, which is the same shape as `plans/0012` **Cause 1** for work rather than for status. **Before 19, because it is a mechanism and 19 is a repair** | |
| 19 | ⚠ **THE CITY HAS SOMEBODY OUTSIDE AT EVERY HOUR IT IS AWAKE.** Today the Day is a **comb** and two mechanisms cut the teeth: `CommuteRoster.ShiftStartOf` sums two draws, halves them and rounds the result to an **hour**; `ServiceEngine.Attend` returns unless `tick.Raw % Ticks.PerDay == 0`, so ***every school Trip in the city starts on one Tick of 2,048***. Measured on `minimal.toml` at 1,000 Citizens: **1,341 of 2,047 Ticks with nobody out at all**, longest empty run 486. ⚠ **Last, because it is the only row here that repairs something that already runs** — and ⚠ **20 lands in front of it for a second reason**: a second Trip generator changes what the comb's teeth are made of, so measuring the Day before shopping exists measures one generator's shape | |
| 21 | 🔴 **A PLAYER-BUILT WORLD HAS NO PEOPLE, AND THE DOOR WAS BUILT FOR IT ON 2026-08-15.** `CommandKind.Ground` landed 2026-09-04 and made [`adr/0090`](../docs/adr/0090-the-generator-makes-land-and-the-player-makes-every-road.md)'s world reachable for the first time — terrain, Woodland, water and hazard, with no lattice, no Lots and nobody. ⚠ **A player can now lay Streets and carve Lots and NOTHING IS EVER BUILT**: measured on `minimal.toml`, 40 Street clicks and 16 zone clicks gave 40 Segments and 128 Lots, and **five in-world Days later the readout said 0 Buildings and 0 Citizens**. The chain is Households → the Unplaced Pool → placement → Buildings, and ***an empty world has an empty Pool, which is the Pool working rather than failing***. `SyntheticCity.PeopleInto` is `PopulateInto`'s other half and its own remark names this exact case — *"a city whose Streets were laid by `CommandKind.Connect` wants the people half without the land half and could not ask for it"* — and it still has no verb. `CommandKind.People` is `Ground`'s shape exactly: no payload, no format version move, one `Simulation` case. ⚠ **Everything that would be learned by playing the empty world is blocked on this row**, including the world `plans/0002` §D2 names as the ratifier for `[traffic]`'s three hash-bearing numbers | ✅ 26-09-04 |
| 22 | ⚠ **THE STREET TOOL CANNOT BE AIMED, AND IT IS THE ONLY VERB THAT CHANGES THE GROUND WITH NO HOVER.** Raised by the player 2026-09-04, unprompted, on the first session with a world worth building in. **Two invisible rules compose.** `Simulation.ApplyConnect` **floors** the Tile to the lattice, so the edit lands on the **south-west corner of the block clicked in** and never on the nearest corner; `Main.Lay` then picks the axis on `east % block >= north % block`, which is a diagonal split of that same block. ***So a click near a block's top-right corner lays a Street at its bottom-left one.*** ⚠ **Face midpoints work perfectly and the interior does not**, which is why every driven run so far missed it — the playtest's 40 clicks were all on midpoints and produced 40 Segments with no surprise. ⚠ **`Main.Pointing` gives `Zone` a `Virgin` line, `Service` a Lot line and `Demolish` a resolved Building; `Street` gets the Tile coordinate and nothing else.** Row 15g one level down: ***a verb you cannot aim is a verb you cannot test***, and this one can be aimed only by arithmetic the player is doing in their head ✅ **FIXED 2026-09-04, IN THE SHELL AND NOT IN THE FLOOR.** `StreetGrid.NearestEdge` takes a Tile to the lattice edge it is *nearest* by perpendicular distance — which answers the axis as a by-product rather than as a second rule — and `StreetGrid.IntersectionTile` addresses it, so `Main.Lay` now sends the intersection's own Tile and ***`ApplyConnect`'s floor is a no-op on what arrives***. ⚠ **The floor is deliberately untouched**: it is `adr/0014`'s snap, and moving it would change what every already-recorded `.borough` replays to — [`adr/0100`](../docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md) prices a hash move at nothing and does not license changing a recorded log's meaning. **Measured by driving `--empty`**: nine clicks in nine blocks — four corners, four face midpoints, the dead centre — laid nine Segments, and the `draw` list put every one on the edge nearest its click. 🔴 **The row's *face midpoints work perfectly* is true only ON the face line**, and five of those nine differ from what the old rule would have laid. **The hover gained a `Connect` arm** naming the Segment, the edge and which of lay/bulldoze a click is; **the ghost draws the edge rather than the block**. `StreetAimTests` — 16 assertions, in `Borough.Core`, because that is where the aim now lives | ✅ 26-09-04 |
| 23 | ⚠ **THERE ARE NO DIAGONAL STREETS AND THE REFUSAL NEVER REACHES THE PLAYER.** `StreetAxis` declares exactly `East` and `North`; `RefuseConnect` rejects any `RoadKind` that is not `Street`; [`adr/0077`](../docs/adr/0077-a-road-edit-is-one-segment-and-the-player-lays-streets-only.md) refuses Arterials by name because *"a spline is many control points and is not one command at all"*. So *how do I build a diagonal road* has the answer **you cannot**, which is a **refusal** and therefore the one classification [`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) counts as evidence. ⚠ **What is owed is a sentence and not a mechanism**: 15e gave the shell a refusal vocabulary and this is not in it, so the tool declines silently. 🔴 **And the generated world DOES contain diagonals** — six occupied compass bins where a pure lattice has four, measured by `--morphology` on 2026-09-04, laid by `[roads] foot_paths_per_thousand_blocks`. ***A player who has seen a diagonal on screen will reasonably ask for the tool that made it***, and the honest answer is that no tool did ✅ **ANSWERED 2026-09-04, AND THE ANSWER IS THREE SENTENCES AND A GESTURE TO TRIGGER THEM.** 🔴 **The tool never DECLINED — it SUBSTITUTED**, which is worse than the row said: a diagonal is not expressible as a `Command` at all, so `Simulation.Refuses` is never asked, there is no `Refusal` to word, and `Sentence` could not have carried this however complete it was. ***A refusal with no gesture that provokes it is a refusal nobody can be shown***, so the gesture came first: **a drag**, which is what every reference game binds to a road and what the player will reach for. `StreetGrid.Between` classifies one — `OneEdge`, `OneLine` or **`TwoAxes`**, the diagonal and the dog-leg together because they differ in shape and not in what is owed — and `Main.Dragged` says so and lays nothing, the press having already acted. **And the hover names the diagonal you are pointing at**: `Main.Crossing` walks the block's own off-lattice bucket and reports *a FOOT PATH cuts this block corner to corner — foot only, laid when the world was made. No tool lays one*. ⚠ **The drawing was checked rather than assumed** — the two foot paths on `minimal.toml` at 1,000 Citizens draw at yaw 2.356 rad and 181 m long against 42 and 42 Streets at 1.571 and 3.142, so ***the player really has seen one***. 🔴 **AND THE SENTENCE COULD NOT BE WATCHED HAPPENING, WHICH IS A SECOND DEFECT AND THE REASON THE FIRST DRAFT LOOKED FINE.** `readout`, `shoot` and the socket reply all carried the readout panel alone; **the hover is a second `Control` and was in none of them**, so every arm of `Pointing` — the Street tool's edge, `Zone`'s free frontage, `Demolish`'s Building — was unassertable from a driven run. `Main.Captioned` puts it in the caption; [`0048`](0048-driving-the-shell.md) **F29** owns it. ⚠ **The first draft of the refusal ran the full width of a 3,024-pixel frame and collided with the hover**, found by shooting it and not by reading it — it is two lines now. `StreetDragTests` (11 assertions, in `Borough.Core` beside the aim) and `StreetDiagonalTests` hold both halves: what a drag is, and that every diagonal the generator lays is a `FootPath` and not one of them is on the Street lattice | ✅ 26-09-04 |
| 24 | **A PLOT WIDTH IS A MULTIPLE OF ITS OWN BLOCK'S UNIT, NOT A FIFTH OF EVERY SEGMENT.** `[lots] lots_per_segment = 5` is uniform across the map, and [`adr/0078`](../docs/adr/0078-frontage-is-derived-on-the-epoch-and-a-lots-width-is-the-segments-own-building-count.md) claims *"there was no number to choose"* because the five is already in `CONTEXT.md` → Address. 🔴 **Read what that sentence is FOR: it is a ROUTING-GRAPH argument** — five Addresses a Segment is what holds the graph at ~30,000 Segments rather than 150,000–300,000 — ***so the five was chosen to size the graph and is being used to size plots***, and it was never a claim about how wide a building is. ⚠ **The routing argument survives untouched**: five *on average* is compatible with widths that vary, and nothing in the ~30,000 bound needs them equal. **Tait's 49 Scottish blocks (2008) supply the STRUCTURE** — widths quantised rather than continuous, the unit **block-specific and varying 2.2× between blocks**, regularity within a block and variation between them, *"a formal system of measurement is not needed"*. 🔴 **THEY DO NOT SUPPLY THE STEP, AND THIS ROW SAID *quarter multiples* ON THEIR AUTHORITY UNTIL 2026-09-04.** ¾/1/1¼/1½/1¾/2 is what fell out of 49 blocks in Scotland; importing it makes a Borough plot width a claim about Scotland, which is `plans/0012` **Cause 5** committed by the author of the row. ⚠ **The grid has an opinion nobody asked for**: a block is 32 Tiles, so quarters land on the grid only where the unit is a multiple of **4 Tiles** — at a 6-Tile unit the six classes floor to 4/6/7/9/10/12 and two of them stop being distinguishable. ✅ **A quarter may still be right for a reason that is Borough's own**: 32 is a power of two, so halves and quarters are exact where thirds are not (32 ÷ 3 = 10.67) — ***the same number on a footing this project can defend***, and it argues the quarter is AVAILABLE rather than correct, since eighths are equally exact and give sixteen classes nobody could tell apart. **What settles it is two questions a survey cannot answer**: *how many distinguishable width classes does a block need* — the step derives from that plus the grid, not the other way round — and *what reads at the camera distance*, since a quarter of a 6-Tile unit is **4 metres of frontage seen from a kilometre up**, which the `draw` list and a screenshot can now measure and nobody has. ⚠ **Hash-bearing; needs a new `purpose_tag`** for the per-block draw. 🔴 **And it takes the ground out from under `BlockPatterns.StripTiles`**, which derives DEPTH from width via this file's only shape claim — *a plot is about as deep as it is wide* — and has no single width to divide by once widths vary. ⚠ **Related to but not the same as 22**: this is what the ground is cut into, that is how the player cuts it. **Twin of 25 at the parcel grain**, and they share `LotSubdivider` and `BlockPattern`, so they sequence rather than parallelise ✅ **DONE 2026-09-04, and BOTH questions the row said nobody had asked were answered by instrument.** [`0060`](0060-the-plot-module.md). 🔴 **The measurement did not exist: the draw list is in metres and a screenshot is in pixels and NOTHING JOINED THEM**, so *what reads at the camera distance* was an eyeball dressed as a question. `draw` gained a **`scale`** row from `Main.Ruler` — three ground points a Tile apart through `Camera3D.UnprojectPosition`, the same projection the frame was drawn with, so tilt, perspective and the viewport are in the answer. **Measured** at 3,024×1,834 and tilt 40°: **54.6 px a Tile at 100 m, 27.0 at 200, 13.4 at 400 — where a player edits — 5.35 at 1,000 with the city in frame, 2.67 at 2,000.** ***So the grid's own step is already at the threshold at the far end and there is no case for anything finer***, which answers the row's own 4-metre worry with **5 pixels, visible, only just**. ✅ **And the class count came out of THIS BUILD rather than out of Scotland**: a face carries **two or three** parcels, the Addresses splitting by parity, so ***a set of six classes is a distribution nobody can see in a row of three*** — **two**. The module is `blockTiles ÷ 8` or `÷ 16` drawn per block (**4 or 2 Tiles**, a **2×** spread, the nearest the grid can express to Tait's 2.2 without borrowing it), the spare modules go to a contiguous run drawn per **face**, and the last parcel absorbs `reach mod unit` so `Exhaustive` still leaves no sliver. ⚠ **Two `purpose_tag`s and not one** — `PlotUnit` on the block, `PlotWidths` on the block AND the face, since four faces keyed alike read as four copies of one terrace. 🔴 **THE ROW PREDICTED `StripTiles` WOULD BREAK WHEN WIDTHS VARIED AND IT WAS ALREADY BROKEN**: its first term was `blockTiles ÷ lotsPerSegment`, which is the **Address spacing** and not a frontage, so the file's one shape claim — *a plot is about as deep as it is wide* — was false in the shipped city by **1.8×** (6 deep behind 10 and 11 wide, 40×24 m and 44×24 m off the draw list). ***A derivation can be wrong about the quantity it names without ever being wrong about the number it returns.*** Corrected to `2 × blockTiles ÷ lotsPerSegment`, where the quarter cap binds: depth **6 → 8**. **Parcels went 40×24, 44×24, 64×24, 24×40, 24×80 m → 32×32, 40×32, 48×32, 64×32, 32×64**, and at 1,000 m a terrace that read as a uniform bar now breaks into unequal pieces. 🔴 **AND IT TIPPED TWO TESTS THAT WERE ALREADY SITTING ON NOTHING.** `CarOwnershipTests` asserted a driver city **employs more people**, sited at 16,000 on a sweep recorded three days earlier reading **−1, +41, +1,070**; re-measured on `2620f50` **before this change** the same runs give **+1, +1, +19**, so it had been passing on a margin of **one job** and this row moved it by four. ***The employment total saturates*** — a walker the ceiling refuses takes a nearer job — so the reading is `EmploymentActivity.Beyond`, and **at 16,000 the Budget refuses NOBODY in either mode**: 0, 0, 36, 1,124, **4,920** for a walker at 16k/24k/32k/48k/64k against **0 at every one** for a driver. ***The class escaped an inert fixture on 09-01 and landed on another one.*** Re-sited at **64,000** on the refusal and the rung split. `TasteTests` failed the same way — moved off 2,000 the same day for the same reason, and **seed 0 is the single negative reading in its 12,000 row** (−6, +3, +11, +3, +8); re-sited at **32,000**, where five seeds give **+13, +14, +16, +15, +12** and every one clears its sham. ⚠ **Both session traces moved and `world-hash.txt` could not**, its fixture hand-placing Lots by its own comment's design | ✅ 26-09-04 |
| 25 | 🔴 **THE LATTICE A PLAYER'S STREETS SNAP TO IS NOT UNIFORM. Decided 2026-09-04 by the player**, on the sitting that raised it. `[roads] block_tiles = 32` is one number for the whole map, and it is what `Simulation.ApplyConnect` floors a click to — so the player's grid is exactly as regular as the generator's was, and 128 m is a good block size that is also the only one. ⚠ **`BlockTiles` is read at 71 sites across 11 files** — `RoadGenerator` 35, `StreetGrid` 6, `LotSubdivider` 6, `RoadGraph` 5, `Simulation` 5, `Ruleset` 4, `SyntheticCity` 4, `Frontage` 2, `World` 2, `TrafficPresence` 1, `LineSourceQueries` 1 — so *uniform* is not a value anywhere; it is the assumption that one integer answers *how big is a block* everywhere at once. ***The work is not changing a number, it is finding out how many places believe there is one.*** ⚠ **Hash-bearing**, and it moves the block sizing every Lot, every frontage and every paved-extent figure derives from. ⚠ **Row 22 first and this is not a preference**: a non-uniform snap makes aiming *harder*, and a player who cannot see where a click will land on a REGULAR grid cannot judge an irregular one at all. ⚠ **`--morphology` is the instrument that will read it** — the shipped lattice measured φ 0.9987 and 166 intersections per sq mile on 2026-09-04, against Manhattan's ~105 and Portland's 400, so the grain was never the problem and ***a change that moves the grain rather than the uniformity would be the wrong change made confidently*** ✅ **DONE 2026-09-04, and the survey's answer was sharper than the row's own question.** [`0061`](0061-the-varying-lattice.md). 🔴 **Every one of the seventy-odd `BlockTiles` sites is one of exactly TWO expressions** — `line * block_tiles` (*where does this line stand*) and `FloorDiv(tile, block_tiles)` (*which block is this Tile in*) — ***so `uniform` is not a value anywhere and could not have been found by searching for one***: it is the shape of the two expressions, and one integer answered both because on an even lattice they are inverses of one multiplication. **`BlockLattice` names them** — `EdgeOf`, `LineAt` and **`WidthOf`, which is the difference the arithmetic could not express at all**, the divisor being the answer. ⚠ **`Even(blockTiles)` is the identity case**, which is what makes routing every site through it PROVABLE: nothing in the routing is a design change under `05 §4`, and the golden baselines are the proof (`adr/0100`). **Four things a mechanical rewrite would have got wrong**: `LineSourceQueries` and `TrafficPresence` size windows on *the* pitch, and on a varying lattice the pitch is a RANGE, so a window sized by the mean silently under-covers — `Narrowest` now; `RoadGenerator.Link` stepped in fixed block-sized jumps and would invent a Node between two intersections; a Street's length was hoisted as one `var length` for the whole lattice, and ***a Segment's length is GROUND rather than a nominal***; and `Nominal` against `WidthOf` is the distinction the type exists to make, so a reach and a carve say which they meant. ✅ **`BlockGround` closed the gap the first commit MARKED rather than half-fixed**: `BlockPatterns.Carve` took one number and produced four faces, ***so a block was square by construction*** and passing the column's width would have silently used it as the depth. 🔴 **`--morphology`, which this row names as *the instrument that will read it*, was BLIND to what the row changes** — orientation entropy, φ, node degree and circuity are all properties of which way an edge runs — so it gained a **`## Blocks`** section, the Street-length distribution, Streets only. **Measured, `minimal` against `gridded` at 10,000 Citizens: 627 Segments, 324 Nodes, 320 intersections and the WHOLE degree histogram including its 72.50% four-way share come out BYTE-IDENTICAL**, which is the row's *do not move the grain* satisfied rather than asserted. ⚠ **Two readings move and neither is the grain**: φ 0.9981 → 0.9969 with the four cardinal bins holding at **306 each**, so all of it is the **foot-path diagonals** ceasing to be at 45° across an oblong block; and paved extent 4.73 → 4.60 km², carrying density 175 → 180 per sq mile ***on an unchanged Node count — a density that moved because the denominator did***. ✅ **`[roads] block_spread_tiles`, and ABSENT MEANS UNIFORM** — a derived spread with no key would replace one absolute with another, which is this row's own complaint one level up — so no shipped Ruleset moved and **not one golden baseline moved**. `gridded.toml` is the demonstration; the step of 8 Tiles is a quarter of `block_tiles` and is **not imported from a survey of anywhere**. ⚠ **The mean is held over a period of FOUR lines and the position of the wide one is DRAWN** (`PurposeTag.BlockSpacing`, once per period): a fixed pattern is wallpaper, an independent draw is noise, ***and what a real gridiron has is a hierarchy***. 🔴 **A refusal became UNSTATEABLE and that is `adr/0070` evidence**: a `[[lattice]]` origin is checked against *a multiple of `block_tiles`*, which is the same sentence as *on a line* only while the lines are even — once a spread is stated the positions are drawn on the WORLD SEED and ***a Ruleset is loaded before any world exists***, so the pair is refused by name. `adr/0048` 229 → **232**. 🔴 **AND THE SHELL WATCH FOUND WHAT NO NUMBER HERE PREDICTED**: the same ground carves into **more Lots** once blocks are unequal — **192 uniform, 200 at spreads 2/4/8/12, 216 at 15**, on 149 Segments and 81 Nodes in every reading. ***A step and not a slope***, so it is a rounding threshold in the carve; three candidates, none separated, **typed *measurable*** under `adr/0043`. 🔴 **AND IT COULD NOT BE FILED IN [`0002`](0002-open-questions.md) §B, WHICH IS A FINDING ABOUT THIS AMNESTY**: `adr/0073` routes a question there, `CorpusBudgetTests.The_open_questions_do_not_grow` holds that file at 153,786 words with **zero headroom**, and ***the amnesty's own rule that prose beside new simulation is free has no expression in that test***. The entry is written and held in `0061` **F8** | ✅ 26-09-04 |
| 26 | **`src/Borough.Godot/Main.cs` IS THE SHELL, AND IT IS ONE FILE.** Raised by the player 2026-09-04. **6,636 lines, 3,010 of them non-comment, 135 members — and the only `.cs` file in the project.** ⚠ **The line count flatters it in one direction and hides it in the other**: more than half the file is doc-comments, which is the house style working as designed (they sit on `CorpusBudgetTests`' numerator precisely so prose cannot escape `docs/` by relocating), so it is 3k of code carrying 3.6k of argument — ***and a split moves both without moving the ratio, because a doc-comment counts wherever it lives***. **The seams already exist and barely cross-talk**: *channels* (`Drive` `Driven` `Apply` `Shoot` `Listen` `Serve` `Answer` `DrawList` `Write`, ~600); *ground and layers* (`Ground` `Hazard` `Flood` `Skin` `Scatter` `Washing` `Rewash` `Legend` `RungOf` `Vintage` `Surface` `Pave` `Cells`, ~900); *panels* (`Panels` `Dial` `Tuner` `Governing` `Palette` `Govern` `ShowTools` `ShowPolicies` `Regenerate`, ~750); *massing* (`Cap` `CapFor` `CapBasis` `Massing` `Massings` `Fill` `Scramble` `Slate`, ~700); *camera* (`Pan` `Edge` `Dolly` `Look` `Lens` `Orbit` `Tipped` `Nearest` `Furthest` `Frame`, ~400); *verbs* (`Ordered` `Record` `Act` `Hold` `Held` `Send` `Sentence` `Lay` `Clear` `Raise`, ~400); *readout and hover* (`Pointing` `Built` `Underfoot` `Virgin` `Cursor` `Readout` `Doing`, ~300); *sky* (`Sun` `Clock` `Daylight` `Weather`, ~300). ⚠ **`Main` is a `Node3D` bound to a scene, so the low-risk form is PARTIAL CLASSES** — one class across eight files, no fields to thread, and Godot's `Main_ScriptMethods.generated.cs` keeps working. **Extracting real types is better design and is a SECOND commit with actual risk in it**, because a camera or a massing builder has to be handed `_world`, `_simulation` and the layer handles. ✅ **The move-only commit changes no behaviour and moves no State Hash**, so it is an *optimisation* under [`adr/0100`](../docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md) and its diff is reviewable as provable text relocation. ⚠ **The honest counter-argument, recorded rather than answered**: a 6,636-line file *hurts*, and that pain is the only thing currently stopping it reaching 9,000 — ***a split removes the pressure along with the problem***, so whatever replaces the pressure is part of this row and not a later thought. 🔴 **TAKEN UP WHEN 21 LANDS AND BEFORE 22–25**, all four of which edit `Main.cs` or `Simulation.cs`. ⚠ **One small patch goes in FIRST and is already written**: the hover panel moves bottom-left → top-right and the type scales with window height (1.0–1.6× of a size stated at 1080), because it is anchored on text this row would move and re-anchoring costs more than applying it  ✅ **DONE 2026-09-04, the move-only commit.** Nine files, one class: `Main.cs` 6,768 → **1,780**, and `Channels` 613, `Verbs` 580, `Readout` 545, `Massing` 800, `Ground` 1,095, `Sky` 443, `Camera` 367, `Panels` 800. ⚠ **The seams were the eight named above with two departures, both recorded in the file that took them**: the typography — `Retype`, `Typed` and six point constants — went to `Readout` although it also sizes the panels' shared `Theme`, and `Weather` went to `Sky` because what it reports is the sky. **What stayed is the node**: every field and constant, `_Ready`, `_Process`, `_UnhandledInput`, `Draw`, `Arguments`, `Layer` and `Quit`. ⚠ **The check that it moved nothing is the readout, BYTE-IDENTICAL before and after** on a driven `minimal.toml --citizens 400` at Tick 60; 🔴 **the screenshot is NOT that check** — two runs of the SAME build differ by 1.3M pixels and a max channel delta of 228, against 877k and a delta of **1** across the split, so the picture is evidence and never proof. `split-baseline.tsv` is the draw list kept for 22–25. 🔴 **And the pressure this removes is not replaced**, which row 26 named as its own counter-argument and this line does not answer | ✅ 26-09-04 |

Items 2 and 3 cost one day and added no Ruleset key, number or ADR. They moved three golden
baselines: a hashed column stopped being zero (`adr/0100`).

## What `--day` found

🔴 ***Nobody comes home* was WRONG** — 468 arrive home a Day; that came off a midnight sample.

**The real defect:** 163 Citizens a Day set off *home* from home, 69 *for work* from work.
`CommuteEngine.Travel` walked both roster lists and never asked where the person was, so **a quarter
of this city's commuting went to where the Citizen already stood.** Invisible until `Activity` had a
writer: home-to-home is a Trip like any other. `CommuteDirectionTests` holds it.

⚠ **It failed three other tests, none a regression**: one horizon too short, two bands calibrated on
phantom traffic. ***Figures off those instruments before 2026-08-27 were partly paid for by a
defect.***

## What `hungry.toml` found

**Sustenance and Satisfaction ship** — two saved columns, one writer (`RuleEngine.MoveNeed`), a
`[[resource]] need` key and a `[needs]` table. Education and Health are refused **by name** and filed
in `docs/deferred.md`. `--day` reports the reading and the city's hunger beside it.

🔴 **And watching it ran the mechanism into a wall: the Need is a TALLY where `04 §6` step 6 asks for a
duration.** It moves on a failed *occasion*, and a blocked Rule has **one** — `RuleEngine.Stop`
subscribes it to the Bin and it sleeps until that Bin changes, which on a world nothing restocks is
never. So *a dry afternoon and a dry month* read **-4** alike, which is the sentence the mechanism was
built from, inverted.

**Measured on `hungry.toml`, 2,000 Citizens over 32,768 Ticks:** deepest **-44**, mean **-28** over
720 Households — **7 degrades each**, where `consume`'s rate of 32 would give 1,024. ⚠ **The 7 is the
rehousing count.** The census reads *tenancies ended* at 13–30 per 64-Tick window over 512 readings,
≈6,600 city-wide, ≈9 a Household; each rehousing builds a fresh Bin and buys exactly one more failure.
***The column is counting tenancies, not hunger.*** 🔴 **The first reading of this said 30 tenancies
total and the attribution was withdrawn for an hour** — the census columns are *first/last/low/high* of
a per-interval **sum**, not a run total, which is `plans/0012` **Cause 5** read off a table header.

***This is `adr/0053` arriving a second time, one subject along*** — the Building's pressure clock was
rebuilt as a duration for this reason and the Household's was not.

✅ **FIXED THE SAME DAY.** `RuleEngine.RefreshNeed` recomputes the depth from `tick − StarvedSince`
on `SweepNeeds`, a daily pass. ⚠ **It RECOMPUTES rather than accumulating**, which is what makes a
staggered pass sound: the depth is a function of the duration, so a Household first visited ten Days
in arrives at the right depth in one step. **The period is derived** — the Ruleset states a degrade
*per Day*, so a Day is where the depth is exact and no cadence number enters the design.
⚠ **Its own pass rather than a ride on the Zone Rule sweep**: `Condemn` reaches a Building only
through a *sample* and only when its kind declares `condemn_after_days` or `tenancy_ends_after_days`,
so hunger would have been a silent function of two keys about Buildings. ***A Household mechanism must
not depend on a Ruleset having opinions about Buildings.***

**Re-measured:** deepest **-31**, mean **-10** — the depth is now days-since-fed, and it is bounded by
`evicted.toml`'s own cycle rather than by the mechanism. `NeedTests.The_depth_is_a_duration_and_not_a_tally`
is the guard; ⚠ **every other test in that class passes against the tally.** 🔴 **A new Tick consumer
with no `plans/0013` row** (`adr/0073`) — the corpus freeze is why.

## What the first frames found

**`--watch` prints the city as ASCII** — Buildings under the Travellers moving over them, scaled to
the Lots that exist. `VisibleAgents(aabb, alpha)` is `05 §2`'s second hot query and the first thing
in the project that answers *where is everybody, right now*.

🔴 **THE DAY IS A COMB AND NOT A CURVE.** Measured on `minimal.toml`, 1,000 Citizens, sampled every
Tick: the morning has **five departure bursts** — peaking at Ticks 486, 570, 652, 733 and 820, one
per in-world hour — with **50–59 consecutive Ticks of a COMPLETELY EMPTY road network** between
them. A Shift starts on an integer hour and the window is 6–10, so there are **five** possible
departure clumps and `arrive_early_max_minutes = 15` is the only thing spreading each one.
***`adr/0101` says the Day's shape is emergent, and what emerges from an hour-granular Shift start
is a comb.*** At a million Citizens that is a fifth of the city departing inside 21 Ticks, five
times a morning.

⚠ **14 of 24 hours have nobody outside at all** — nothing moves before 05:00, after 19:00, or
between 10:00 and 12:00. That is the commute being the only built Trip generator, seen rather than
read.

🔴 **A FRAME EVERY 32 TICKS REPORTS AN EMPTY CITY**, which is the job cadence aliased against itself
and cost the first four readings. ***An instrument sampling a paced mechanism has to pick an
interval coprime with the pacing***, and nothing warned; the dump's header does now.

⚠ **A walker is placed on a straight line it did not walk, and a driver is placed on its Segment.**
A foot Leg is priced once and holds no Segment, so the only thing stored about a walking Traveller
is that it left one Address for another — and **nine shipped worlds have `car_ownership_percent`
absent or 0**, so this is most of the city. The frames show it: walkers cut through blocks, drivers
ride the lattice. `alpha` therefore moves the walkers and cannot move the drivers.

✅ **Placement is total** — `placed` equals `travelling` on every frame of every world tried.

⚠ **A new Tick consumer with no `plans/0013` row** (`adr/0073`) — the corpus freeze is why.

## What the shell found

**`src/Borough.Godot` exists** — Godot 4.7.2, the `.NET` build, on `net10.0` against a `net10.0`
core. A camera framed on the built extent, one `MultiMeshInstance3D` each for the Road Graph,
the Buildings and the Travellers, a speed ladder on `space` and `1`–`4`, and a readout. **It is not
in `Borough.slnx`**, so a root `dotnet build` still neither builds it nor needs Godot: ***the
constraint that the headless runner never requires Godot stopped being vacuous and became a check.***

🔴 **THE FIRST FRAME RENDERED HALF A ROAD NETWORK AND THE ASCII DUMP COULD NOT HAVE CAUGHT IT.**
`Basis.Scaled` scales in the **parent** frame, so every east–west Segment was given 8 m of length
and its own length of width, and the lattice drew as north–south lines with no cross-streets.
⚠ ***The two viewers disagree because they ask different questions***: `--watch` rasterises the line
itself and never asks for a transform, so a bug in the transform is invisible to it. **Two eyes on
one mechanism is worth what it cost.**

🔴 **THE WALKERS WERE FLOATING IN THE MIDDLE OF THE BLOCKS.** At Tick 751 — 08:48, the peak of the
third burst, 63 people out — every Traveller sat off the network. That was `VisibleAgents`'
straight-line placement for a foot Leg, stated in its own remarks and *believed*; seeing it is
different. ***A shell is where an approximation you documented stops being a footnote.***

✅ **FIXED, AND THE FIX IS A MECHANISM RATHER THAN A SNAP.** `TripEngine.Plan` passed
`recordPath: driving`, so a walk computed its path and **threw it away** — the search runs either
way and only the `_via` bookkeeping was conditional. It now records for both, and a walker is placed
at the share of its route's *length* that its elapsed time has bought. ⚠ **`adr/0041` is untouched**:
that ADR decides who is attributed **volume**, and `BeginLeg` tests the mode before it looks at
`RouteHead`, so a walk is still priced once and its hops are never entered or left. **Same Tick,
same 63 people, all of them on a street.** `VisibleAgentsTests.A_traveller_stands_on_a_segment`
holds it, and fails on the old placement at the first Traveller it meets.

⚠ **It moves the State Hash** — a walk's hops are saved state — so all three golden traces were
re-baselined (`adr/0100`: that costs nothing while nobody carries a save). 🔴 **The Tick cost is
BELOW THIS MACHINE'S NOISE FLOOR and is therefore unmeasured rather than small**: 4,000 Citizens
over 4,096 Ticks reads **3.27–5.77 s before against 3.38–4.25 s after**, two runs each, which
separates nothing. ***What it costs at a million Citizens is unknown***, ~~and it owes `plans/0013` a
row~~ ✅ **and `plans/0013` carries the row as of 2026-08-31**, saying exactly that.

⚠ **Confirmed the comb from the other side**: at Tick 402, 04:42, the readout says `travelling 0`
against a fully built city. Nobody is outside before 05:00.

⚠ **Godot loads the `Debug` assembly.** A tree built only in `Release` fails with *Cannot instantiate
C# script*, which reads as a missing class and is a missing configuration.

## What watching it at 1× found

🔴 **THE LADDER IS 675× TOO FAST TO WATCH A PERSON AND THE DOCUMENT SAYS OTHERWISE.** A Tick is
42.19 s of in-world time, so `01 §1`'s **1× runs the world at 675× real time**: a 20-minute commute
is **1.8 real seconds**, a walker crosses a 128 m block in **0.14 s**, and a car crosses one in
**0.014 s** — ***sub-frame at 60 Hz on every rung the document offers.***

🔴 **`01 §1` says *traffic is visually truthful* at 0.5×, and truthfulness is 1× REAL time — 0.0237
Ticks/s, 1/675 of the ladder's 1×.** `§7`'s recorded concession says an untouched speed control
shows traffic *"roughly twice as fast as its apparent size warrants"*; the figure is **675**.
⚠ **Two requirements 675× apart are sharing one ladder** — a Day inside a two-minute observation
window, and traffic that looks like traffic — and the document asserts both. Filed in `plans/0012`
as a Cause 4 sighting; ***not paid here, because choosing what the ladder should be is design.***

✅ **The shell added rungs of its own below 0.5×** and **prints the × real-time figure beside the
rung name**, so the gap is on screen rather than in a table. **1 Tick/s is where a walker becomes
watchable**: a block in 2.2 s, a commute in 28 s, a Day in 34 minutes.

🔴 **THE WHOLE LADDER IS THEN HALVED, AS AN EXPERIMENT AND NOT AS A DECISION.** Every rung is half
`01 §1`'s Ticks/s — 1× is **8** and a Day is **4m16s** — and 1 Tick/s is kept as `1/8×` because it is
the rung a person actually looked at and liked. ⚠ **`01 §1` is still the design and this shell
disagrees with it on purpose**: the tick rate is host-side and runtime-only, so nothing here moves a
hash or settles anything. ***It exists to produce the playtest `adr/0094`'s own revisit trigger asks
for***, which names a person at the controls as the only instrument that settles a pacing number.

⚠ **`adr/0094`'s revisit trigger predicted the wrong direction.** It says *"Lower is expected to be
the direction if it moves"* — a **faster** Day — and names *too fast* as the case where the
`Observe → Diagnose → Intervene → Wait` loop will not close inside one. **The first sitting anybody
ever watched pushed the other way, and for a reason the trigger does not contain**: the loop is
about **events**, and what broke was **motion**. `01 §1` reasons entirely in events per real second
and `§7` knows apparent speed scales with the tick rate. ***Each section holds half the argument and
nobody wrote the sentence that puts them together.***

✅ **The shell takes `--ruleset` and `--citizens`**, so worlds can be compared. 🔴 **The first
comparison is a surprise: `congested.toml` at 4,000 Citizens has 43 Travellers on the network at
Tick 749, against `minimal.toml` at 1,000 with 63.** Four times the population and a third fewer
people out. ⚠ **The cause is that everybody drives there** — a car is ten times a walker's pace, so
each journey occupies a tenth of the time and concurrency falls with it. ***A city where everyone
drives looks emptier than a walking city a quarter its size***, which is a claim about what a
renderer shows rather than about congestion, and no column would have raised it.

⚠ **The camera frames the whole built extent, which is wrong for a two-centre world** —
`twinned.toml`'s lattices each render as a postage stamp. Drag-pan and wheel-zoom were added rather
than a cleverer framing rule, because which centre you want to look at is a question only the
operator can answer.

⚠ **Nothing about the simulation changed.** The tick rate is host-side and runtime-only
(`CLAUDE.md` → Constants), so a rung moves no hash and settles no design question.

## What the school run found

**`Service` is applied**, `Need` is complete at four, and `docs/deferred.md` named the exact trigger
that un-parked Education and Health: *a civic Building a Household draws on*. Nothing was chosen that
the trigger did not supply.

⚠ **`adr/0118` left this verb's payload examined-not-yet and the answer is clean for a reason it did
not anticipate.** It expected *a Building and a catchment* to be the hard part. ***There is no catchment
in the payload at all***: `adr/0032` demoted coverage from mechanism to overlay, so the field that would
not have fitted turned out not to be a field.

✅ **The degradation rule did not have to be CHOSEN, which was the condition of un-parking.** An attended
occasion is a daily **sweep**, not a subscription, so the per-occasion step already *is* the per-Day
rate. ***The asymmetry that forced `RefreshNeed` one item ago was a property of how the occasion
ARRIVES, not of Needs.***

🔴 **THE ENGINE REWARDED A PLAYER FOR NOT USING IT, AND A RULESET HEADER CAUGHT IT.** The first spelling
returned early where no school stood, conflating *this Ruleset has no schools* with *this city has built
none* — so the one city the verb exists to punish was the one where Education stayed pinned at zero.
`schooled.toml`'s header had already stated the opposite in prose. ***A Ruleset gates the pass; the
state of the city never does.***

🔴 **AND `main` WAS RED BEFORE ANY OF THIS.** The decide guard's default went false on 08-30 and
`RuleEvaluationTests.Deciding_writes_nothing_even_on_a_tick_where_rules_fire` asserts it is ON. Eight
classes were found and opted in; that one was not, and no full run happened afterwards.

**Measured on `schooled.toml`: 2,000 Citizens, 146 Days, 4 schools, 12,122 occasions — `unreached`
ZERO, and ZERO again at 20,000.**

🔴 ⚠ **AND ALL FOUR SCHOOLS WERE ONE SCHOOL, WHICH THIS READING COULD NOT SEE.** Found 2026-09-03 by
a per-school panel that did not exist here: satisficing stops at the first candidate inside the Fast
rung, and in a city entirely inside one Fast rung that is *the first school in slot order*, for
everybody, for ever — so **109 families attended one of the four and the other three served nobody**,
while the reach panel above called it 100%. ***A share of occasions delivered cannot see which
Building delivered them.*** `[capacity] floor_tiles_per_place` is the repair and
[`plans/0054`](0054-the-kind.md) holds it.

🔴 **THAT 100% IS A PROPERTY OF THE WORLD.** ***The synthetic city is ~1.4 km across against a Commute
Budget that walks 4.2 km***, so nothing in it is out of reach — `adr/0089` backwards.

✅ **BUILD ONE WIDER THAN A BUDGET AND THE NUMBER APPEARS: 61% at 100,000 Citizens, 50% at 200,000**
— 9,841 occasions with a school in the box that no route delivers in time. ***That is the number a
coverage Map Layer could not have produced.*** ⚠ **It is not `adr/0032`'s Arterial**: `arterial_count
= 0` here, so the detour is the grid's — a straight-line box against a right-angled walk.
***Severance stays unmeasured and the instrument for it now exists.***

✅ **The failure half lives at `--schools 0`**: mean depth **−233 by Day 144**, falling at exactly
`education_degrade = 2` a Day. ⚠ **The knob is the instrument's** — how many schools stand is a fact
about the city, and no Ruleset says it.

🔴 **A new Tick consumer with no `plans/0013` row** (`adr/0073`) — the corpus freeze is why. ⚠ **It starts
every school Trip on ONE Tick**; a school day has no hours key to partition on, and the Event Wheel is
the successor.

## What the second sitting found

🔴 **BUILDINGS WERE DRAWN IN THE MIDDLE OF THE ROAD, AND ONLY A PICTURE COULD SAY SO.** A Lot's
`east`/`north` is its **address point on the Segment**, not a plot of ground beside it — Lots hang on
Segments and have no depth (`adr/0078`) — and which kerb it belongs to lives in a **separate `side`
column** the shell was discarding. Every hash, every census and every ASCII dump was correct
throughout: they all ask *which Lot*, and none of them asks *where does it stand*. ***The one
question a renderer asks is the one nothing else in the build had ever asked.***

🔴 **A DAY TAKING 4m16s READ AS A STUCK `Day` COUNTER, AND THE READOUT'S OWN NUMBER IS WHY.** It
printed `338x real time`, which is true — 8 Ticks/s × 42.19 s — and a person reading *338×* does not
then expect to wait four and a quarter minutes for tomorrow. **The reassuring figure was the one on
screen.** ⚠ ***A speed is a rate and a person waiting is holding a duration***, which is `adr/0059`'s
*state the duration, derive the rate* arriving in a readout rather than in a Ruleset. The rung now
prints **a Day in 4m16s** beside the multiple.

⚠ **`BOROUGH_SHOT` did not work on a machine with no screen**, which is the only thing it is for: the
viewport has no texture under `--headless`, so reaching through it threw before both the print and
the quit. ***A run neither said it had arrived nor stopped***, and the timing attempt against it read
its own `timeout` back as the answer.

🔴 **AND THE SECOND ATTEMPT DID IT AGAIN AFTER THE GUARD LANDED, FOR AN UNRELATED REASON.** The
pipeline was `godot | grep -m1`: **`grep` exits on its match and `godot` does not notice**, so the
run continued to the `timeout` and the elapsed figure was the killer's, not the city's — **400s,
then 600s, neither of them a measurement.** ⚠ **The second reading was nearly reported as evidence
of a slowdown that does not exist.** ***A fire-once-and-quit trigger can only be asked "tell me when
you pass N", and a stopwatch wrapped around it cannot tell arrival from being killed***; an
instrument that prints as it goes distinguishes them by construction. `plans/0032`'s axis, arriving
one level down: this was an **instrument** all along, and it was built in the shape of an assertion.

✅ **MEASURED, ONCE THE INSTRUMENT REPORTED CONTINUOUSLY: 8.0 Ticks/s, FLAT.** 128 Ticks per 16 s
across the run, frame time constant, and `_owed` **draining rather than filling** — the host is far
ahead of the ladder, and a Tick costs ~3 ms in Debug at 4,000 Citizens against a top rung of 32
Ticks/s. **So a Day is 4m16s by measurement and not only by arithmetic, and nothing degrades as the
city fills.** ⚠ **Debug, headless, and not the reference machine** (`adr/0106`) — it is a check that
the loop delivers its rung, never a figure about the city.

🔴 **THE PACING QUESTION IS OPEN AND IS BIGGER THAN THE LADDER.** Watching motion settled that
**slower rungs are necessary** and settled nothing about where the *natural* rung sits. ***That is
design, and it is not paid here.*** **It is queued in *Owed when the freeze lifts* above, which is
where it is stated in full** — ⚠ **restating it here would be two copies of one question in one
document**, which is `plans/0012` **Cause 1** written by hand.

## What the flood found

**A Flood ships.** `DisasterEngine` schedules one from the seed and the Tick over the Hazard Region,
spreads it through the floodplain connected to its seed below a rising surge, recedes it to nothing,
and takes what it finds — `rulesets/flooded.toml`, `--flood`, `DisasterTests`, and water on the map
in the shell.

**THREE DURATIONS AND NO SEVERITY KEY**, which is `01 §5.2`'s own sentence kept: *"No severity
constant is authored anywhere; the only constants are a frequency interval and a spread rate, both
durations, both scale-free."* How bad a flood is comes out of **where the world seeded it** —
measured, and it is monotone:

| seed depth | ruined | swept |
|---|---|---|
| 102 | 0 | 224 |
| 378 | 38 | 185 |
| 1,119 | 128 | 51 |
| 1,800 | 235 | 5 |
| 1,876–1,975 | 0 | 0 |

A Hazard Region row holds *the flood level minus its ground*, so a large depth is **low** ground. The
surge opens with the water at the seed's own ground and rises to the flood level; ground **below**
the origin is swept away and its Lot vacates, ground at or above it is **ruined** and the shell
stands. ***Both are existing verbs and the fork is one depth against another.*** A flood seeded high
on the floodplain destroys the city; one seeded in the deepest hollow never reaches it.

⚠ **5 of 9 floods touched nothing at all**, which is `01 §5.3` working: *"Riverside floodplain
inundated — 0 Buildings affected"* is the game telling a player a siting decision was correct.

🔴 **THE FIRST RUN REPORTED FOUR FLOODS AND ZERO BUILDINGS TOUCHED, AND THE MECHANISM WAS FINE.**
`coastal.toml`'s lattice sits at the map's middle on high ground; **0 of 420 Lots were on floodplain
at all.** The synthetic city is ~1.4 km across on a 65.5 km map, so it covers two ten-thousandths of
the ground and *cannot meet a coast by accident* — `adr/0089` arriving where nobody expected it.
`flooded.toml` therefore states a `[[lattice]]` **on the water's edge** and that siting is the whole
demonstration: **240 of 420 Lots exposed.** ⚠ ***A world where a disaster cannot reach the city is
not a demonstration of disasters***, and the dump now prints where the floodplain actually is so the
next person is told in one line rather than after an afternoon.

🔴 **THE FOOTPRINT LEAKED, AND ONLY ITS DEEP HALF.** The end-of-flood drain freed the Cells *below*
the surge — and ground deeper than the seed is never below it, because the surge only climbs back to
where it started. **5,140 Cells still standing after three floods had ended and a fourth had reached
291.** ⚠ **The comment above the line had reasoned from the right fact to the opposite conclusion**:
the deepest ground *is* the last to dry, which is exactly why the recession cannot be what takes it.
`adr/0006`, caught by the dump's own last line rather than by a test — `DisasterTests` holds it now.

🔴 **TWO PHOTOGRAPHS OF A FLOOD WERE TAKEN WITH NO CAPTION ON THEM.** `--start-at` fast-forwards in
`_Ready`, so the world is already past `BOROUGH_SHOT_AT` when the **first** frame draws — and a
`Control` added to a `CanvasLayer` that frame has not been laid out yet. ***The one thing in the
frame that says which Tick it is, is the thing a first-frame capture drops.*** The trigger now waits
for the third frame. ⚠ **This is the third defect `BOROUGH_SHOT` has had and all three were about
*when* it fires**, never about what it draws.

✅ **THE SHELL SHOWS WATER, AND THE FLOOD ARRIVING IN THE CITY.** One MultiMesh for the sea, laid
once; one for the standing water, refilled every frame. **Buildings are sized from their kind** —
`[[building]] occupants`, jittered per Building off its monotonic row id — and every shipped kind
declares **3**, so *today the derivation buys nothing visible and the variation is all jitter*. It
is there so that a kind holding thirty draws a tower without the renderer being told.

🔴 **A RUIN LOOKED EXACTLY LIKE A HOUSE, AND THE FLOOD IS WHAT MADE THAT UNBEARABLE.** In the first
frame at Tick 6,101 the water was unmistakable and **235 ruined Buildings standing in it were
indistinguishable from the dry ones on the bank**. The readout said the number; the picture did not.
***That is the same shape as the hexadecimal the Definition of done was amended over***, one level
along — a state the city knows and the eye cannot find.

✅ **FIXED, AND IT IS `IsAbandoned` RATHER THAN *FLOODED*.** The Buildings MultiMesh carries a colour
per instance now. ⚠ **The wider predicate is deliberate**: a Building abandoned by `adr/0053`'s
failure pressure and one ruined by a flood are the same state — `02 §4.3`'s derelict — and the
renderer has no business knowing which verb put it there. **The visible consequence is that
`declining.toml` now greys out as it decays**, which nobody asked for and is the point: ***one
colour, every mechanism that reaches the state.*** Checked rather than asserted — at Tick 7,001 that
city is a mix of pale standing Buildings and dark shells with no flood anywhere near it.

🔴 ⚠ **AND THE FIRST SPELLING WASHED IT OUT, IN A WAY THAT LOOKS EXACTLY LIKE A BAD PALETTE.** A
MultiMesh instance colour is multiplied into albedo in **linear** space, so sRGB values written
straight through render far brighter than they read: standing came out near white and the contrast
the change exists to create was most of the way gone. ***The colours were right and the space they
were written in was not.*** `SrgbToLinear` at the write site. ⚠ **It cost a second screenshot to
see, and no test could have caught it** — there is nothing in the build that asserts anything about
a colour.

~~⚠ **The Hazard Region is still not drawn**~~ ✅ **DRAWN 2026-08-31**, by the audit pass below —
`Main.Hazard`, one flat colour laid once under the roads and under the sea. What `01 §5.3` calls the
*posted price* is now on screen from the first frame instead of being a number `--flood` prints.
⚠ **The DEPTH is deliberately not drawn**: a Hazard Region row holds *the flood level minus the
ground*, so a shade ramp on it would read backwards — the polarity that made this file's
worst-looking seed the one that ruined nothing.

~~⚠ **A new Tick consumer with no `plans/0013` row**~~ ✅ **FILED 2026-08-31.** It is `O(footprint)`
a Tick while a flood is live and still unmeasured at a million Citizens, but the ledger now carries
the row and says so — and the row makes a point the engine's own remark could not: ***the footprint
is ground, so this is the only consumer in the ledger whose multiplicand does not fall when the
population does.***

## What the loose ends found

**The dwelling stock has a sink.** `[[building]] abandoned_when_empty_after_days` abandons a Building
nobody has lived in for its kind's duration — `adr/0069`'s build predicate mirrored, and `02 §5.5`'s
redevelopment floor, *the case where nobody wants the land*. ⚠ **It ABANDONS rather than demolishing,
so `adr/0091` is untouched**: the city stops maintaining an empty house and never sends a bulldozer a
player would have had to pay for. **`Days the stock fell` goes 0 → 141 of 400.**

⚠ **It is not `condemn_after_days` and the difference is the whole point.** That key reads Failure
Pressure, so a kind stating it declines whether anybody wants it or not — on `aged.toml`, whose
`upkeep` can never be supplied, it would be a fixed **lifespan** and every dwelling would die on one
clock. This reads occupancy. ***Only surplus stock dies, so the sink is the demand signal read from
the other end.***

🔴 **AND IT DOES NOT RESTORE `jobs = 8`, WHICH IS THE FINDING RATHER THAN A FAILED REPAIR.** Swept at
5, 10, 20, 40 and 80 Days, `posts per Citizen` moves **1.61 → 1.98 across a sixteenfold range**,
against 2.06 with no sink and a derived 0.96. ***A shrinking city does not consolidate***: placement
takes the first Lot with room out of a draw of three and nothing biases it toward a fuller house, so
the families left after a trough are spread **one per dwelling**. **Over a fifth of the housing
capacity stands empty while a thirtieth of the houses do**, and a sink keyed on an empty house can
only collect the tail of that. ⚠ **Neither half is a defect** — a family has no reason to prefer the
house with neighbours in it, and steering the sample would be the optimiser `adr/0017` refuses. ***So
`1000/360 × occupants` assumed every dwelling was FULL, which is a city under housing pressure, and a
demographic city is under it half the time.***

🔴 **THE EMPTY CLOCK'S FIRST SPELLING TOOK THE WRONG NEIGHBOUR'S ENCODING AND A FIXTURE CAUGHT IT.**
`AbandonedSince` uses zero-as-sentinel and this column copied it — but a Building is empty **from the
Tick it is raised**, so zero-as-sentinel loses every Building raised on **Tick 0**. That is not a
corner: it is every fixture in the suite and every Building `SyntheticCity` lays. ⚠ **It was invisible
on the shipped world** because the populator fills what it raises in the same call, and the remark
above the line said the case was *unreachable*. ***A sentinel is a claim about which values cannot
occur, and it was written by looking at the neighbour rather than at the mechanism.***

**The stage windows are as wide as their own floors.** `busiest ÷ mean` **7.0× → 3.3×**, Days with no
transition at all **116 → 19** of 400.

🔴 **THE WINDOW COULD NOT BE WIDENED ALONE, AND A NUMBER IN THREE DOCUMENTS SAID OTHERWISE.** A wake
is drawn uniform on `[N, N+W)`, so a life is `N + W/2` **on average** — and `aged.toml`'s mean was
already **188 Days against `adr/0094`'s ~190**, where `plans/0046`, the file's own header and the
`--stages` panel all called the chain **160**. ***160 is the FLOOR and the ceiling is about the
MEAN***, so there was no room at all. Widening at fixed floors would have put the mean at 236. The
floors are halved to pay for the widths and the mean is now exactly 160. ***A number that is one end
of a distribution says which end*** — `plans/0012` **Cause 5**, on a distribution rather than on a
ratio.

⚠ **IT DAMPS THE ECHO AND DOES NOT REMOVE IT, AND 400 DAYS CANNOT SEE WHICH.** Over **1,200 Days**
the widened city converges — the swing falls **3.67× → 2.55× → 1.19×** across three 400-Day thirds —
where the narrow one is still swinging **2.47×** in its last third. ***`plans/0046`'s definition of
done was answered on a run too short to answer it.***

🔴 **AND THE LONG RUN FOUND THE REPLACEMENT THRESHOLD WRONG BY A FACTOR OF TWO.** `adr/0011` derives
exact replacement as *two children replacing two adults* — airtight for a Household of **two** adults,
and `World.SpawnChildren` gives **every child its own Household**. Nothing pairs anybody, so exact
replacement is **one child**, and ***the census says so rather than the arithmetic***: `working age`
and the Household count come back **exactly equal**. ⚠ **So 1.45 against 2.00 was a city growing 45% a
generation reported as one in decline**, in the panel, in `plans/0046` and in `aged.toml`'s header at
once. **720 → ~1,100 Households over 1,200 Days**, bounded by housing rather than by fertility. The
panel now **measures** the threshold; the design question is queued in *Owed when the freeze lifts*.

⚠ **A new saved column and no `plans/0013` row** (`adr/0073`) — the corpus freeze is why.


## What the preference found

**A Household has a Taste and placement reads it.** `adr/0027` in one expression: a Life Stage
supplies a **base** and a **width**, each Household draws its own **position** inside them, and only
the range moves — so a stage transition slides the band under a fixed position and a family that
always wanted room still wants room once it is an Empty Nest. `Ruleset.CentralityTaste`,
`rulesets/choosy.toml`, `PlacementEngine.TryHouse`.

🔴 **THE BOARD WAS WRONG ABOUT WHAT WAS MISSING, AND THE WRONG HALF WAS THE MECHANISM.** Row 9 said
placement *satisficed on distance alone*. It did not satisfice at all: `TryHouse` drew three Lots and
took the **first with room in it** — three boolean filters over a uniform draw, no score, no
comparison, no second thought. `adr/0069` says so in its own words, *"no acceptance filter, no sampler
bias, no scored choice, no `μ`"*. ***A row describing an absence can be wrong about which absence it
is***, and this one understated the hole by a whole mechanism.

🔴 **THE FIRST DEMONSTRATION RULESET DEMONSTRATED NOTHING, AND EVERY TEST OF IT PASSED.**
`choosy.toml` was built as `aged.toml` plus ten keys, because `aged.toml` is the file with the Life
Stages in it. The Ruleset loaded, `CentralityVaries` was true, the tastes spread correctly across
their bands, and the file produced a State Hash **identical to `aged.toml`'s at every sample** over
20,480 Ticks. The Unplaced Pool was **empty on every one of those Ticks**: `minimal.toml` stopped
condemning anything at milestone 17 (`adr/0164`), so nobody ever lost a home, so `TryHouse` never
ran. ***A preference about where to live is unreachable in a world where nobody is looking for
somewhere to live.*** ⚠ **The three tests that passed were all asking the Ruleset and none was asking
the city** — the one that found it asserts the Pool is non-empty on at least one Tick, which is a
sentence nobody writes until they have been bitten. It is `flooded.toml`'s lattice origin arriving
again, one row along: **the mechanism was right and the world could not exercise it.**

The file is now `declining.toml` plus the stages, because the smallest shipped world that condemns is
the smallest one in which anybody is ever **rehoused** — and rehousing is exactly the moment a
preference is worth having. ⚠ **The slow path would have worked and is not a fix**: `aged.toml`'s
chain reaches `children_become` after 24 + 48 + 48 = 120 Days, which is **245,760 Ticks**, and a
demonstration nobody will sit through is not one.

⚠ **THE LOADER CAUGHT THE `adr/0006` LEAK BEFORE THE RUN DID.** A `[[life_stage]]` stating
`children_become` opens a second door into the Unplaced Pool, and `declining.toml` states no
`[placement] gives_up_after_days` — so the file was **refused at load** with a sentence explaining
that a Pool with a door and no give-up rule grows without bound. ***A bound a Ruleset can violate
belongs in the loader***, and this is what that rule buys: the alternative was a slow leak over a
hundred thousand Ticks that nothing was watching for.

✅ **MEASURED, AND WITH A PLACEBO UNDER IT.** On `choosy.toml` at 2,000 Citizens over 20,480 Ticks,
Households wanting the centre live **161–162 Tiles** from the nearest lattice origin and Households
wanting room live **172–173** — stable across a 6× longer run, so it is a signal rather than noise.
⚠ **Eleven Tiles in a city ~350 Tiles across is small enough to owe the reader a reason to believe
it**, so the same Households are split again on a taste they **do not have**, drawn from an unrelated
stream over the same ids on the same Tick. That sham split moves the mean by **1 Tile against the
real preference's 11**. ***A signal that survives a sham grouping is a property of the map and not of
the preference***, and this one does not survive it.

⚠ **THE EFFECT IS BOUNDED BY THE SAMPLE AND NOT BY THE TASTE, WHICH IS `adr/0069` HOLDING.** A
Household compares the **three** Lots it was shown and nothing biases which three it sees — so
best-of-three is the ceiling on how central anybody can get, however hard they want it. It is also
diluted by the founding population, which `SyntheticCity` places directly without ever calling
`TryHouse`. ***Neither is a defect and both cap the number***: a preference that steered the sample
would be an optimiser, and `adr/0017` refuses one.

⚠ **The mechanism is a comparison and never a threshold.** Nothing refuses a dwelling — a family that
dislikes all three still moves in. A preference that could refuse would fill the Pool for a reason no
Ruleset authored, and on a file with no `gives_up_after_days` it would fill it for ever.

✅ **29 of the 30 shipped worlds produce the same city, State Hash for State Hash.** The gate is
`Ruleset.CentralityVaries` — *does any stage state an opinion* — and not the taste of the Household
in hand, and the reason is the **draw count** rather than the score: a neutral Household would make
the same choice and consume a different number of candidate draws getting there. ⚠ **A neutral taste
weighs exactly zero and is not a special case anybody wrote**: placement scores `distance × (2T − 1)`,
so `centrality_base_percent = 50` ties every candidate and falls through to the first-with-room accept
the build already had. ***The mechanism is continuous with the behaviour it replaces at the midpoint
of the axis.***

🔴 **ONE AXIS OF THREE, AND THE OTHER TWO ARE UNBUILT RATHER THAN REFUSED** (`adr/0070`). Centrality
shipped first because it is the only one the world can already measure — distance to the nearest
`[[lattice]]` origin needs no new state. **Quiet** needs a pollution Layer that is zero on every
shipped file but `fouled.toml`; **rent** needs a price a dwelling does not have. ⚠ **And no
centrality number can be ratified until rent exists**: a preference for the centre is only meaningful
against something that makes the centre cost more, so ***the numbers `choosy.toml` produces ratify
nothing.***

## What the audit found

**Row 14 read this page for its own unpaid debts rather than for a mechanism**, which is the one kind
of sitting the queue's opening warning does not cover: a debt written into a narrative paragraph is a
debt nobody sums. Six were standing. Five are paid below and one became row 19.

✅ **`plans/0013` has the five rows it was owed** — `ServiceEngine.Attend`, `RuleEngine.SweepNeeds`,
`DisasterEngine.Sweep`, a walk's recorded path, and the shell's per-frame world walk — plus a note on
the Zone Rules row saying its unit was measured before the empty-clock verdict existed.
⚠ **Every one says UNMEASURED**, and that is the ledger working rather than failing: `adr/0073`'s rule
is that a cost reaches the document on the day it is found, not on the day somebody has a number.
🔴 **Three source remarks said *the corpus freeze is why it has none*, and all three were wrong by the
time anybody read them** — the freeze caps a **ratio**, and a row filed beside the mechanism it prices
costs the ratio nothing. ***An excuse written into a comment outlives the thing it was excusing.***

✅ **THE HAZARD REGION IS DRAWN.** `Main.Hazard`, `FloodCells` as one flat colour laid once, at 0.02 m
— **under** the roads at 0.1 m and under the sea at 0.6 m, because the risk is a property of the
*ground* and a Street across a floodplain must read as a Street on a floodplain rather than as a
floodplain with a hole in it. ⚠ **The depth is deliberately not shaded**: a row's depth is *the flood
level minus the ground*, so a ramp on it reads backwards — ***a shade ramp on a quantity that reads
backwards teaches the wrong thing faster than no ramp at all.***

🔴 **AND LOOKING AT IT IS WHAT MADE THE NUMBER MEAN ANYTHING.** *240 of 420 Lots exposed* has been in
`flooded.toml`'s header and in `CLAUDE.md` for days. On screen it is **the whole northern half of the
city sitting on rust-coloured ground**, with the boundary running diagonally through blocks so that
one side of a Street is exposed and the other is not. ⚠ **The floodplain is far larger than the
water** — 11,063 Cells at risk, 4% of the map, against a sea that is a strip along one edge of the
frame — because five percentage points between `sea_level_percent` and `flood_level_percent` buy a
great deal of flat coastal ground. ***That is the posted price `01 §5.3` asks for, and it is not a
thing a dump can print.***

🔴 **`BOROUGH_SHOT` HAS A FOURTH DEFECT AND IT IS THE FIRST ONE ABOUT WHETHER IT CAN DRAW AT ALL.**
The three before it were about *when* it fires. This one: the guard was `GetViewport().GetTexture() is
{ } texture`, and under `--headless` that is **not null** — Godot returns a `ViewportTexture` whose
RID the dummy renderer has nothing behind. So `GetImage()` returned null, `SavePng` threw, and the run
neither wrote a picture nor stopped: **41,350 error lines in two minutes, killed by a timeout.**
⚠ **The comment above the guard described that exact symptom, in the past tense, as something already
prevented.** ***A guard checks the handle and the emptiness is in what the handle points at*** — the
same shape as a flood depth reading backwards, the wrong end of an indirection. It now asks
`DisplayServer.GetName()` first, because asking the dummy renderer for a picture is itself two red
lines in a log whose whole job is to be read: **zero errors, exit 0, and a sentence saying a
screenshot needs a real display.**

⚠ **The one debt that could not be paid here became row 19** — it was written as row 18 and the queue shifted under it, which is the sighting the preamble now carries. The comb is two mechanisms — an
hour-granular Shift start and a school pass that fires on one Tick in 2,048 — and both need a Ruleset
key and a loader refusal, which is queue work rather than audit work. Re-measured live on
`minimal.toml` at 1,000 Citizens: **1,341 of 2,047 Ticks with nobody travelling**, longest empty runs
486, 341, 172. ***It is not stale.***

🔴 **AND THE AUDIT'S OWN GATE HAD A HOLE IN IT, FOUND BY WALKING INTO IT.** `RulesetSchemaTests`
went red on the first lane run after picking landed — *`building.abandoned_when_empty_after_days`
read by the loader and absent from the schema*, which is **row 13's debt**: the key shipped and
`rulesets/ruleset.schema.json` was never regenerated. ⚠ **The alarming half is not the stale file.**
The class **fails on `c2e9ff3`'s own tree** — proven by stashing and running it — and the lane that
gated that commit reported **`Failed: 0, Total: 2496`**. The lane now reports **2,498**, and the
delta is exactly this class's two tests. ***It was not collected, and the run said everything
passed.***

⚠ **Three causes were ruled out and none of them was it**: the `tier!=instrument` filter (the class
runs, and fails, under exactly that expression), a skip (`Skipped: 0` in every run today) and a
missing file (the test asserts its existence separately, with its own message). ~~🔴 **The cause is
still unknown, and saying so is the finding**~~ ✅ **KNOWN 2026-09-01, and it was the fourth candidate
nobody listed: a REBASE.** See *What the missing class found* below — ***the class had not gone
missing, it had not yet arrived***, and the paragraph above is wrong in its first sentence.

🔴 **What it exposes is that the suite counts everything about itself except how much of itself
ran.** `TierBudgetTests` times every test and fails a slow one; `TierDeclarationTests` refuses a
third tier and holds the instrument share under a quarter. **Neither would notice a class vanishing
from the run**, because both reason about the tests they were handed. ***A gate that cannot say how
many tests it ran cannot tell a green run from a short one***, which is row 15f. ⚠ **That sentence
survives the correction below and its target moves**: nothing counts the lane, and the counter cannot
be a test.

## What the player's hands found

**15a and 15b, and the shell now issues `Command`s.** `v` looks, `z` subdivides, `x` lays a Street,
`b` demolishes; a left click acts and a left drag still pans, told apart **on release** because any
held button pans and the difference is not knowable until the button comes up.

🔴 **`Zone` IS NOT A BRUSH, AND THE VERB'S NAME SAYS OTHERWISE.** `LotSubdivider.Face` returns zero
on a frontage `World.Frontage` has already claimed, so the verb **creates Lots on virgin faces and
can never repaint a block that has them**. ⚠ **The first test of it zoned a block `SyntheticCity`
had already carved and reported the verb broken** — 102 live Lots, zones `1` and `2`, and the brush's
`0x0008` on none of them. ***The commonest misuse of this verb is silent***: the command is accepted,
creates nothing and reports nothing. The panel now counts the block's unclaimed faces and says
*a click does nothing* before the click, and the mode reads **SUBDIVIDE**.

⚠ **The player's loop is Street THEN zoning, and that is what `PlayerVerbTests` asserts** — lay a
Segment on lattice ground the city never paved, subdivide the block, and Lots appear admitting the
Zone Rule that asked for them. **A Lot's `Zone` is a bitmask** (`ZoneRuleDefinition.Admits`), so a
brush painting the rule's *index* would make Lots that read as zoned and that nothing ever builds on
— ***a city that silently never grows.***

🔴 **`Demolish` is addressed at the LOT's Tile and never the cursor's.** `Simulation.BuildingOn`
matches a Lot's coordinate exactly and `ApplyDemolish` refuses rather than clearing the nearest —
*"a mistyped command must not be indistinguishable from the demolition somebody meant"*. A cursor
lands on a Tile that is almost never a Lot's own, so the shell resolves the click to a Building,
names it in the hover, and sends **that Building's address**. ***The refusal stays exact and the aim
becomes possible.***

⚠ **The shell DECLINES rather than catching, and the reason is not tidiness.** Commands apply at the
top of a Tick, so an exception out of `Apply` aborts `Step` half way and leaves a world no invariant
covers — ***a crash is not the worst outcome of an unguarded click; a half-stepped world is.*** Each
guard reads the **same field** the core reads, so no rule is restated: `Demolish` asks
`IsAbandoned`, `Connect` asks `block_tiles`. `15e` is what turns a disabled click into a player's
sentence.

🔴 **A QUEUED COMMAND BUYS ONE TICK, EVEN PAUSED, and that is a decision.** A verb pressed at rung 0
would otherwise sit until somebody started the clock and look broken. Every reference builder lets
you edit while paused; the cost is that acting is the one input that moves a paused world, by
exactly one Tick.

⚠ **`src/Borough.Godot` is not in `Borough.slnx` and cannot be tested**, so `PlayerVerbTests` pins
the **core's half** of each of the shell's three translations — the brush word, the lattice snap and
the address. ***A shell built on a misreading of the core goes red there rather than being wrong on
screen where nothing watches.*** ⚠ **`Demolish` is deliberately absent from it**: `DemolishVerbTests`
already owns the verb, and `Demolishing_empty_ground_is_refused` is the assertion the Lot-addressing
exists to satisfy.

⚠ **Two of those tests failed first and each failure was the fixture rather than the city.** A world's
constructor lays no roads, so both `Connect` arms bulldozed edges that were not there, two no-ops
hashed the same, and the *two axes differ* assertion **failed by agreeing** — the answer a
difference-assertion gives when neither side did anything. And an A/B on a command that sampled the
hash before and after `Order` was comparing two **different Ticks**, with every clock and cadence in
the world moved; ***an A/B on a command has to hold the Tick count equal on both arms.***

✅ **The hover is a stack, and a line is OMITTED when the thing it would describe has no row.** Nine
of the shipped worlds have no Layer row, no District and no water, so a fixed template would print
`pollution 0 · land value 0 · district 0` over every Tile of every one of them. ***Zero and absent
are different answers***, and a panel rendering them identically teaches that a city has a quantity
where it has no mechanism. ⚠ **Terrain is the one exception and is stated rather than hidden** — the
table is dense, so `ordinary` everywhere is a uniform answer and not a missing one.

⚠ **`TerrainKind.Floodplain` and the Hazard Region share a word and are two mechanisms.** The first
is a quantile of `TerrainGenerator`'s noise field; the second is the water generator's flood level
against the height field. The panel printed both and read as if it were repeating itself, so the
second says **AT FLOOD RISK**.

⚠ **The window opens at 1920×1080, maximised.** Godot's 1152×648 default is a third of a modern
screen against a readout that is one long line and an orbit over a 65.5 km map.

## What `Govern` and `Service` found

✅ **The shell issues four of the five player verbs.** `p` opens a governing panel with a row per
declared `[[policy]]`; `s` holds a `serves` kind and places it on the vacant Lot under the cursor.
**`schooled.toml`'s school is placeable by hand for the first time** — its header's *nothing in the
world places its school* was true of a run with no `--schools`, and the only world with an
`education` Need now has something that answers it.

🔴 **`RulesetNames` HAD NO POLICY ACCESSOR, so the panel could only have offered *policy 0, policy
1, policy 2*.** `Ruleset.PolicyKeys` holds a **hash** — a name is the only thing that survives a
renumbering, which is why saved state keys by it — and a person cannot read one. The loader had the
string in hand and dropped it, which is the same absence `RulesetNames` was written to close for
kinds and Rules. ⚠ **A list and not a map, and it is the one name here that is not inverted from an
id table**: a Policy has no id, `Govern` addresses it by **declaration position**, so the position is
the index.

⚠ **An unnamed `[[policy]]` is SHOWN AND DISABLED rather than omitted, and that is the whole of
`PolicyNameTests`' second assertion.** Omitting the row would shift every position below it, and
`Govern` names a Policy by exactly that position — ***a gap in the middle has to be a gap and not a
shortening***, or every command the panel issues below it addresses the wrong Policy.

🔴 **THE FIELD IS THE TRANSFER AMOUNT AND NOT THE TAX RATE, AND A PANEL THAT DID NOT SAY SO WOULD
MISLEAD.** On `levied.toml` all three rows read **1** — `transfer.amount` — while the levy that bites
is `apply = { derived = "balance", percent = 10 }`. `Govern` writes `PolicyTable.Amount` and nothing
else; `ApplyCount` is Ruleset data and is not governable. ***A person turning this dial expecting a
rate would change the wrong number and watch nothing happen***, so the panel says which number it is
in its own header.

⚠ **The governing panel is deliberately NOT the tuner.** The tuner rewrites Ruleset text and
regenerates a **new city**; this issues a `Govern` `Command` against the city that is running, at a
Tick, through the door a replay reproduces. ***One edits the world's premises and the other plays the
game***, and putting them on one key would blur exactly that line.

⚠ **`Service` needs a vacant Lot and there is no Cell index over Lots.** `BuildingResidency` indexes
Buildings; a Lot is a point on a Segment rather than ground. So the shell walks the Lot table — but
**on a click and on a hover in this mode only**, never in the ordinary per-frame path, which is what
keeps it off `plans/0013`'s `Main.Draw` row. It is still an unpriced walk.

⚠ **Nothing new was added to `PlayerVerbTests`, and that is deliberate.** `ServiceTests` already
covers the raising and all three refusals and `GovernTests` covers the amount, the table, both
reloads and both refusals — ***the shell's contract was already pinned***, and a second copy of it
would be `plans/0012` **Cause 1**. What did need a test is the new accessor, and `PolicyNameTests`
is it.

⚠ **`--govern` opens the panel at start**, on `BOROUGH_SHOT`'s bargain: a machine with no hands
cannot press `p`, and ***a panel nobody can photograph is a panel nobody reviews.***

🔴 **A BINDING THAT DECIDES WHICH OF TWO THINGS YOU MEANT WILL DECIDE WRONG, AND 15a SHIPPED ONE.**
Panning and every verb shared the left button, told apart by a **four-pixel slop test taken on
release**. macOS tap-to-click and force-click both emit motion while the button is down, so the
pointer drifted past four pixels before the release arrived — ***the verb was cancelled and that same
drift panned the camera***. ⚠ **A larger slop radius is the same defect with a longer fuse**, which
is why the test was deleted rather than tuned: a click now acts **on press** and the camera has its
own buttons. Separately, `InputEventPanGesture` was wired to *zoom* while pinch was too, so a
trackpad had two ways to zoom and no way to move. ⚠ **The dead zone over the panels asks each
`Control` for `GetGlobalRect()` rather than guessing a corner** — a guess killed edge-scrolling
north-west outright on the first attempt. Fixed in `4e56a7e`.

## What LOOKING at it found, which is the point of the amended Definition of done

🔴 **TWO OF THE FIVE VERBS WERE INVISIBLE, AND BOTH WERE WORKING PERFECTLY.** `PlayerVerbTests` is
green and proves the Core half of `Connect` and `Zone` in four assertions; the shell drew neither
result. ***A verb whose success and whose failure look identical cannot be learned***, and no test in
this repository can fail on that.

🔴 **`Pave()` RAN AT `_Ready` AND ON A TUNER REBUILD AND NOWHERE ELSE.** A Street laid by `Connect`
entered the world, routed Trips and carried Lots, and **was never drawn**. Its own doc-comment said
so in the first line — *the Road Graph, laid once* — and that sentence was true when it was written
and became a defect the day 15a gave a player the verb. ⚠ **The re-draw is flagged in `Ordered` and
run after the step loop**, because the Command applies at the top of the *next* Tick and `Pave()` is
`O(Segments)` against `bordered.toml`'s **535,817**.

🔴 **A VACANT LOT DREW NOTHING AT ALL**, so `Zone` — which creates Lots and never a Building
(`adr/0069`) — had no visible result on any world. `Buildings()` walks the **Building** table and an
empty Lot is not in it. Now a `_plots` layer draws the vacant ones on the kerb and the readout counts
them.

🔴 ⚠ **AND THE DEFAULT RULESET MAKES THE VERB POINTLESS EVEN ONCE IT IS VISIBLE.** `adr/0069` builds
only while the Unplaced Pool is non-empty. Measured at 2,000 Citizens over 8,192 Ticks:
`minimal.toml` **0 raised, 0 in the Pool**; `declining.toml` **22 raised, 598 in the Pool**;
`crowded.toml` **0 raised, 0 in the Pool** — ⚠ **which contradicts the repository map's claim that it
is the file whose Pool is under pressure**, and is a `plans/0012` **Cause 5** sighting to chase.
***The shell opens on the one world where zoning can never do anything***, which is the same shape as
`choosy.toml`'s finding: a decision about housing is unreachable where nobody needs a house.

⚠ **`godot --path` did not pick up a `-c Release` build**, so the first verification photograph was
of a stale assembly and showed the old readout. ***A screenshot is only evidence of the binary that
took it*** — `dotnet build src/Borough.Godot` (Debug) before any shell capture.

## What the session-as-a-log found

🔴 **THE SHELL PUT A WHOLE CITY INTO THE WORLD BY A DOOR THE LOG DOES NOT ACCOUNT FOR.** It called
`SyntheticCity.PopulateInto` directly at Tick 0 — thousands of rows, before a player had touched
anything — so ***every hand-played session would have replayed against an EMPTY world and diverged at
Tick 0***, with nothing in the file to explain it. `Borough.Headless` has recorded the population as
a `Populate` **Command** since slice 6 (`Session.Load`, and its comment says why); the shell simply
did not. ⚠ **It costs one Tick and that is the runner's behaviour rather than a charge** — a Command
applies at the top of a Tick, so the readout now opens at Tick 1 and `--start-at` counts from 1.

✅ **The round trip holds, measured twice.** `declining.toml` at Tick 400:
**`0xAB863AB17C6FA56C`** from the shell and from `Borough.Headless --log`. `schooled.toml`:
**`0xCFBF926025349931`** both sides.

🔴 ⚠ **A ONE-TICK RECORDING SLIP IS INVISIBLE TO A STATE HASH, AND SO IS A SIXTY-FOUR-TICK ONE.**
The first negative control was written against `declining.toml` with `Connect` and `Zone`, and a
slip of **1, 2, 4, 8, 16, 31, 32, 33 and 64 Ticks was ABSORBED every time** — a Segment and a Lot
***record no creation time***, so the city at Tick 400 is the same city whether the verb landed at
100 or at 164. ***A round trip that only compares hashes cannot see when a verb happened, only
whether it happened*** — which is a far weaker guarantee than the row assumed, and would have been
recorded as a pass. The control now uses `Service`, because a raised Building stamps
`BuildingTable.EmptySince`, and `schooled.toml` is the only shipped world declaring a kind that
`serves`.

⚠ **`SessionRoundTripTests` is in `Borough.Tests` and asserts the shell's ARITHMETIC rather than the
shell.** `src/Borough.Godot` is still not in `Borough.slnx`. What is pinned is the contract
`Ordered()` depends on — *a Command recorded at `world.Tick`, then handed to `Step`, replays to the
same city* — in the one place a test host exists.

⚠ **`BOROUGH_LOG` writes the log on `BOROUGH_SHOT`'s bargain**, and for a stronger reason than the
photograph has: ***a verification nobody can run in a script is one nobody runs twice.***

⚠ **A tuned Ruleset is written out beside the log.** `Regenerate` rebuilds the world from edited TOML
held in memory, so after a tuner pass the log names a content hash **no file has** — and
`Replay.Start` refuses a catalogue whose opening hash differs, which is the refusal working and an
operator with no way to satisfy it.

## What the refusals found

**15e, and the shape of it is one predicate with two consumers.** Each verb's checks moved into a
`Refuse*` method returning a `Refusal` code; `Simulation.Apply` throws on a non-zero one and
`Simulation.Refuses` returns it. `RefusalTests` drives its theory off `Enum.GetValues<Refusal>()`, so
***a member with no case reddens the suite*** — the registry enumerates itself rather than relying on
somebody remembering to add a row.

🔴 **THE SHELL GUARDED THREE OF THE TEN THAT BELONG TO ITS OWN VERBS, AND EACH OF THE THREE WAS A
SECOND COPY OF A RULE.** `plans/0012` **Cause 1** by construction — seventeen exist in all, and the
other seven belong to verbs only an operator sends. The seven it could not see included
`Govern`'s *this world holds no row for that Policy* — reachable through the governing panel, and it
would have arrived as a **half-stepped Tick** rather than as a sentence. ⚠ **The panel restated the
one it did know about** (*states no name*) and had no way to ask about the other two.

🔴 **THE ONE REFUSAL THE SHELL ALREADY HAD THAT COULD ONLY HAPPEN OFF THE MAP WAS THE ONE IT COULD
NOT SHOW.** `REFUSED — …` was composed inside `Pointing()`, which returns *— pointing off the map —*
before it ever reaches the line, so *that is not on the map.* was set on every such click and
displayed on none of them. ⚠ **It moved to the readout for a second and larger reason**: `readout`,
`shoot` and the socket's reply all carry `_readout.Text` and none carries the hover, so ***a refusal
in the hover is a refusal no driven run can observe.***

⚠ **What stays the shell's is AIMING, and it is a different question from a rule.** *No Building
stands in this Cell* is the shell failing to resolve a cursor into an address — there is no command
to ask the core about. ***A rule belongs to the city and an aim belongs to the hand***, and both
reach the same line of the readout.

⚠ **A player's sentence and an exception's message are two registers for two readers.**
`Simulation.Explain` names the ADR, the successor mechanism and the Ruleset key, because its reader
is holding a crash artefact; `Main.Sentence` says *somebody still lives there*. **Every word of the
first was moved rather than written**, and it is now selected by the same code the guard reads.

⚠ **`Borough.Godot` is still not in `Borough.slnx`, so nothing asserts the sentence table is
complete.** The unmapped arm prints the reason **number** rather than falling silent — a missing
sentence has to look wrong on screen, because the screen is the only reviewer.

### What driving the verbs found

**`hold <tool> [which]` and `click <east> <north> [shift]`**, in the drive grammar — `plans/0048`
tier 5, and its **D1** answered by where a click already goes. ***A refusal nobody can make happen is
a refusal nobody has looked at***, and every verb the grammar shipped with moves the clock, the eye
or a file.

🔴 **THE FOUR VERB KEYS WERE NOT RECORDED, AND THAT MADE A RECORDED SESSION UNREPLAYABLE.** They were
excluded on the ground that holding a tool changes nothing in the world, which is true — but a click
means whatever is held, so a recording carrying the click and not the choice replays as a **different
verb**. ***What a recording needs is not everything that changed the city; it is everything a replay
has to know.*** `--record` now spells `hold` and `click` and the file replays through `--drive`.

🔴 **A MISSPELT TOOL LEFT THE PREVIOUS ONE HELD, AND THE NEXT CLICK ACTED WITH IT.** Found by putting
`hold plough` after a `hold service` in a script: the unknown-tool sentence was written, the next
click **cleared it and acted as SERVICE**, and the readout came back reading exactly as the previous
one had. ⚠ **The misspelling was invisible twice over** — once because the sentence was erased, and
once because what replaced it looked like the same complaint. ***A script that misspells its tool
must not be a script that quietly does the last thing again.*** **Two repairs and both are about
erasure**: an unknown tool now disarms to `look`, and `Act` clears the standing refusal **after** the
look check rather than before it — ***looking is the one verb that changes nothing and should erase
nothing.***

⚠ **A driven `hold service 200` can put the shell in a state the keyboard cannot**, because the `s`
key only cycles kinds the Ruleset declares. That is the demonstration rather than a hole: the core
refuses the id and the shell says *this Ruleset declares no such building*, which is the whole of
15e in one line.

✅ **Measured, `minimal.toml` at 2,000 Citizens.** A demolish over an occupied dwelling, a service of
an undeclared kind, a misspelt tool and a Street that was accepted — four readouts, **byte-identical
across two independent runs**, and the picture at Tick 200 carries the sentence at the top of the
screen and the aimed Tile at the bottom.

### What the palette found

**15g, and the palette is a third caller rather than a second path.** Every button — a tool, a Zone
Rule chip, a service kind — calls `Apply(Held(tool, choice))`, which is the command the `z` key
builds and the command `hold` parses. ***One applier and one vocabulary, entered three ways***: the
tool words in `Tools` are the drive grammar's own, so a script, a keypress and a click cannot name a
verb differently.

🔴 **THE GOVERNING PANEL WAS BUILT ONCE AT `_Ready` AND `Regenerate` NEVER REBUILT IT.** Found by
asking whether the palette would acquire the same defect the moment it listed a Zone Rule, and it
would have. A tuner pass replaces the World **and** the Ruleset, so a tune that changed the
`[[policy]]` set left a panel addressing positions the new city has not got. ⚠ **Since 15e that
arrives as a sentence rather than as a half-stepped Tick** — `Simulation.Refuses` catches
`GovernPolicyNotInThisWorld` — ***but a panel showing a Policy the city has not got is still a panel
lying about the city.*** Both Ruleset-shaped panels now rebuild through `Panels()`.

⚠ **A tool this Ruleset cannot supply is DISABLED WITH ITS REASON, and the reason is read rather than
restated.** `Offers` asks `Rules.ZoneRules.Length` and `NextService(0)` — the same two facts `Act`
tests before it sends anything — so `SERVICE — no kind declares 'serves'` is 15e's rule arriving
**before** the click instead of after it. ***The five verbs are not five verbs in every world, and
the mode line could only ever say what was held.***

⚠ **`Pressed` and not `Toggled`.** `ShowTools` writes `ButtonPressed` off the world every time a tool
changes, and `Toggled` fires on a programmatic write as well as on a click — so the refresh would
have issued the command it was describing. ***A control that reports state must not be wired to the
signal that state-writing raises.***

⚠ **The choice row is REBUILT rather than hidden**, because it holds a different list per tool and
the lists are sized by whichever Ruleset is loaded. The three tools with nothing to choose get a
sentence saying what the verb **does**: `SUBDIVIDE` could say the word for ever and never that
clicking a block which already has Lots does nothing at all.

⚠ **`RulesetNames` had no Zone Rule accessor, which is exactly the absence 15c found for Policies.**
The chips would have read *zone rule 0*. ⚠ **A chip names the Zone Rule AND its permission word**
(`housing 0x0001`), because the word is what a Lot stores and what decides whether anything is ever
built there — a panel showing only the name would hide the one number a misconfigured Ruleset goes
wrong in.

⚠ **A click over the palette costs nothing and an edge-pan under it did.** Clicks reach the world
through `_UnhandledInput`, so a `Button` consumes one before the world sees it; the edge-scroll reads
the raw pointer in `_Process` and had to be told about the new rect. ***Two input paths, and only one
of them gets Godot's own guard for free.***

🔴 **`shoot`'S PICTURE WAS ONE WHOLE FRAME BEHIND ITS CAPTION, AND `plans/0048` **F8** RECORDED THAT
FIXED WHEN ONLY THE CAPTION WAS.** The caption at Tick 140 read `SERVICE` while the picture showed
`SUBDIVIDE` — the previous command's world. ⚠ **It has been wrong since `shoot` existed and was
only legible once the palette was on screen**, because the palette is the first thing whose whole
content changes on one command; a map two Ticks apart looks the same. Two `RenderingServer.ForceDraw`
calls made no difference. The capture is now deferred to the top of the next `_Process`, with
`_quitAfterShot` so a `shoot` and a `quit` on one Tick still gets its picture. ***A description of
the build is wrong about the trigger*** ([`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)).

✅ **Watched, driven, on two Rulesets.** `schooled.toml` at Tick 140: SERVICE lit, one `school
education` chip, and the caption agreeing with the picture. `provisioned.toml` under `hold zone 1`:
two chips, `housing 0x0001` unlit and `trade 0x0002` lit, with SERVICE and POLICIES dimmed and
carrying their reasons — ***the same screen tells you what you may do and why you may not do the
rest.***

## What the missing class found

🔴 **NOTHING WENT MISSING. THE CLASS HAD NOT ARRIVED, AND THE ROW SPENT A DAY LOOKING FOR A DEFECT
IN THE ONE PLACE IT COULD NOT BE.** `git ls-tree -r c2e9ff3` holds **zero** `RulesetSchemaTests`;
the class was authored **nineteen minutes later**, in `4e902cb` at 16:31. So `Total: 2496` was a
complete run of every test that existed on that tree, and the gate was telling the truth.
***The three candidates that were ruled out were all properties of the run, and the cause was not a
property of the run at all.***

**The cause is a rebase.** `c2e9ff3` is not an ancestor of `main`: the same work was replayed as
`1c24c05` at 16:39, on top of `4e902cb`, which had brought the class. That tree carries a **new
loader key** — `building.abandoned_when_empty_after_days` — and the **test that checks the schema
against the loader's key surface**, and it does not touch `rulesets/ruleset.schema.json`, which was
regenerated only later in `63bb181`. ⚠ ***Each parent was green on its own tree. The child was red,
and no local gate ever ran on it.*** A rebase is not a merge conflict and git reports nothing,
because neither side edited a line the other touched — the collision is between a key and a test
that had never met.

🔴 **AND SOMETHING DID NOTICE, WHICH IS THE HALF THE ROW GOT MOST WRONG.** The push lane ran on the
pushed tree — `69e53fe`, with `1c24c05` under it — and reported **`Failed: 1, Passed: 2497,
Skipped: 0, Total: 2498`**, naming `RulesetSchemaTests.The_committed_schema_is_the_loaders_own_key_surface`.
⚠ **It was also useless, and the timing says why**: the failing run finished at 20:45:33 UTC and the
fix was pushed at **20:45:39**, six seconds later. ***A lane that takes five and a half minutes
cannot tell you anything you found in four***, which is [`adr/0121`](../docs/adr/0121-the-commit-gate-is-the-assertion-tier-and-a-long-test-runs-post-submit-on-a-machine-that-is-not-yours.md)
working exactly as designed and not a backstop anybody should plan around.

⚠ **`adr/0121` names three lanes and none of them is the tree a rebase produces.** What you run
while working, what gates a commit, and what a runner does afterwards — the first two gate *the tree
you have*, and a rebase substitutes a different one between the second and the push. ***The gate is
a claim about a tree and the claim does not travel with the work.*** That is the same shape as the
rule about quotation this project already has: a caveat does not travel with a number, and a green
does not travel with a rebase.

✅ **PAID IN `scripts/test.sh`, AND OUTSIDE THE SUITE ON PURPOSE.** A run now prints the **tree** it
gated — short hash, branch, and a blunt `-dirty` — and the number of tests it **collected**, compared
against the last run of the *same lane expression* and shouted about when it falls. ⚠ **The ledger
is one line per run in `.git/`**, so it is per-clone, uncommitted, and can never become a corpus
artefact somebody has to keep true.

🔴 ⚠ **AND THE ROW'S OWN PROPOSED REPAIR WOULD NOT HAVE WORKED, WHICH IS WORTH MORE THAN THE FIX.**
*The suite counts everything about itself except how much of itself ran* is true, and the counter
**cannot be a test**: a class that is not collected takes the check that would have counted it with
it. ***A test cannot report that it did not run.*** `TierBudgetTests` and `TierDeclarationTests` are
not merely incomplete here — they are the wrong kind of thing, and so is any third class beside
them. Only something outside the run can count the run.

## What the aim found

🔴 **THE ROW'S OWN EXONERATION OF FACE MIDPOINTS IS TRUE ONLY *ON* THE LINE, AND THE SAFE SET WAS
SMALLER THAN IT THOUGHT.** *Face midpoints work perfectly and the interior does not* reads as
*anywhere near a face is fine*, and it is not: the old rule floored to the block and then split it
on `alongEast >= alongNorth`, so a click **two Tiles inside the north face** — 16 east, 30 north of
its own corner — chose the **west** edge, a quarter-turn and half a block from where the cursor was.
Only a click exactly on the line escaped, because landing on the line is what carries the floor into
the next block. ⚠ **Measured**: of the nine driven clicks, **five** name a different edge under the
new rule than under the old, and two of the five are a *face midpoint offset by two Tiles*.
***The playtest's forty clicks were not on the safe side of a wide margin; they were on a line.***

⚠ **THE DEAD CENTRE OF A BLOCK IS A GENUINE FOUR-WAY TIE AND THERE IS NO RIGHT ANSWER THERE.**
Perpendicular distance is equal to all four faces, so the aim has to *state* an order rather than
find one — south, then west, then north, then east — and `StreetAimTests` pins it. Left implicit it
would be a property of which way the comparison happened to be written, which means ***the same click
could lay different Streets in two builds***, and that is the same surprise this row exists to
remove rather than a smaller one.

🔴 **THE GHOST'S HEIGHT HAD TO BE READ OFF THE ROADS LAYER'S MESH, AND THE OBVIOUS NUMBER WAS
WRONG BY TEN.** `Pave` scales a Segment by **1** in Y, which reads as half a metre either side of
the ground — but `_roads` is built on a `BoxMesh` of size `(1, 0.1, 1)`, so a Street's roof is at
**0.05 m**. A ghost placed at 0.2 m *to sit under the carriageway* sat over it instead and hid the
Street it was aimed at, which is exactly the case a bulldoze needs to see. ⚠ **It looked correct in
the first screenshot** — a yellow bar on the right edge, which is what was being checked — and the
defect was only visible in the second, where a Street was already there. ***A drawing constant
derived from a scale rather than from the mesh it scales is a number about the wrong object.***

⚠ **AND THE HOVER CANNOT BE ASSERTED FROM A DRIVEN RUN, WHICH IS A GAP THIS ROW LEAVES OPEN.**
`Main.Write` puts `_readout.Text` on disk and the hover panel is a second Label that never reaches
the file, so the new `Connect` line is verifiable only by reading a `shoot`. ⚠ **Appending it would
make readout files non-deterministic** rather than merely longer: before any driven `click`, `_aimed`
is null and `Aim()` falls back to the real mouse position, so the hover of a script's first frames is
a property of where somebody left the pointer. ***The fix is a way to aim without acting*** — a
`point` verb setting `_aimed` and nothing else — and it is not built.

## What housing consequences found

Row 16's rent filter and affordability reassessment were already in `70b8ec3` and `4f06dad`;
the queue had not recorded either. `PlacementEngine.ShortagePromptsMove` supplies the remaining
Need consequence through the same reassessment sweep. `[placement] move_at_need` is optional,
hot-reloadable and requires both Needs and reassessment; reassessment already requires a Pool sink.
`TryHouse` retains its affordability filter when the Household searches again.

A residual deficit must not cause immediate repeated moves. The predicate reads the Household's
Sustenance or Satisfaction and the continuing shortage of its own Rule Instances in the current
tenancy. That episode must have lasted long enough to reach the configured depth. Recovery and
rehousing therefore buy time without erasing hunger. Education and Health do not use this predicate.
`shortage moves` counts the shortage-triggered subset of `reassessed out`; rent takes precedence
when both reasons apply.

The first driven headless run crashed when a moving Household owned a long-sleeping producer.
`World.Unlink` only searched the fine Event Wheel, although `EventWheel.Arm` could put that Rule
on the coarse wheel. Cleanup now searches both tiers; `CoarseWheelTests` covers removal before
and after cascade, and `NeedTests` exercises removal through an actual shortage-driven move.

`restless.toml` and `restless-fed.toml` demonstrate shortage and restored supply through a producer
rate reload. Their changed numbers are PROVISIONAL. They exercise relocation, not shopping or a
comparison against a Hinterland. Run with `--census --series` to distinguish moves from departures.

Observed on `restless.toml`, seed 0, 64 Citizens over 16,384 Ticks, with a reload to
`restless-fed.toml` at Tick 8,192 and the Decide guard enabled: 40 shortage moves, 40 placements,
no departures. The last positive 64-Tick sample ended at Tick 14,016. A faster rate does not wake
an already-armed long sleep: the recovery reached these Households through new tenancies using
the new rate, so the reload did not stop moves immediately. This is a finite demonstration, not
a balance or performance claim.

```
dotnet run -c Release --project src/Borough.Headless -- --census --series \
  --ruleset rulesets/restless.toml --reload-at 8192 --ruleset rulesets/restless-fed.toml \
  --citizens 64 --ticks 16384
```
