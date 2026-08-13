# The player model — grilling `01 §1`, `§3` and `§4`

✅ **DONE 2026-08-13.** Twenty-three decisions, six ADRs
([`0090`](../docs/adr/0090-the-generator-makes-land-and-the-player-makes-every-road.md)–[`0095`](../docs/adr/0095-a-commute-budget-is-three-rungs-and-only-the-last-one-refuses.md)),
and **every section of `01-player-experience.md` has now been examined** — §4 was the last, and it was
never on the session's plan.

*(Pinned mid-session on 2026-08-12 with ten decisions and one open question; the pin was lifted the same
day and the document ran to the end.)*

## Why this session exists

`plans/0002`'s coverage map marks `01 §2`, `§5`, `§6` and `§7` examined and **`§1` and `§3` never
touched**. Everything downstream — `02`–`05`, 89 ADRs, ~19,600 lines of simulation — was built on the two
sections nobody had read. Session J found the first symptom on 2026-08-12: `§3` step 1 says *"a road from
the **Outside Connection** inward"* and `RoadGenerator` had been paving the entire map since milestone 5a,
so the build had contradicted the opening for two milestones and nobody had put the two documents side by
side.

The user's own framing, which is the reason this ran before the road write-up it interrupted:

> it sounds like were designing backwards then — decide how the player plays and then create the game
> around it

## What was decided

Ten, in the order they were taken. Every one of them is written into the corpus already; this table is a
view, not the record.

| # | Decision | Where it lives |
|---|---|---|
| 1 | **The map starts empty.** The generator places a few Outside Connections on the edges, each with a short road stub running inward; nothing else stands anywhere. No hamlet, no pre-laid grid, no unlock boundary. The player builds every metre | `01 §3` |
| 2 | **What makes the player start somewhere is a consequence, not a fence.** An Outside Connection is the only object on the map that does anything — the door Citizens, Goods and Money come through. Founding fifty kilometres away is permitted and simply does not work, legibly. This replaces `adr/0088`'s reason, which rested on an unlock rule the design has since refused | `01 §3` |
| 3 | **Founding capital is granted automatically at world creation.** Money's only door is the Outside Connection, so a balance of zero is a game that cannot start. Gift-versus-loan is the natural **difficulty axis** and is deliberately left open. It must be *enough to get started and not enough to win*, which is hash-bearing and carries a named ratifier | `01 §3` |
| 4 | **Budget pressure is present from the first second; budget *failure* is what is absent.** The earlier draft said pressure was absent and was wrong — the balance is finite and infrastructure is paid for three times (`adr/0035`). You can overspend in the first ten minutes and feel it; you cannot lose | `01 §3` |
| 5 | **1× is the design speed and the game must be enjoyable there.** Pause (planning) / 0.5× (slowdown) / **1×** / 2× / 4× (speedup). No 8×, because at a 64-second Day `Observe` has nothing to observe | `01 §1` |
| 6 | **No feedback-latency rule — response latency *is* the demand signal.** Build where the city needs something and it appears in seconds, which is the reward; build where it does not and the ground stays empty. A stated maximum latency would be a demand meter through the back door. What the cadences bound is **perception, not response** | `01 §1` |
| 7 | **A vacant Lot's reason is recomputed on the click and never recorded.** Cold path, no column, no State Hash coverage, and it cannot drift from the predicate because it *is* the predicate. The single piece of stored state is **when the Lot was last looked at** | `01 §1`, `CONTEXT.md` → Frontage |
| 8 | **The hour markers are expectations, not a script.** This is a sandbox, not a story. §3 and §4's durations are predictions the design must make *plausible*, never a curve it may *enforce*. Any mechanism that would guarantee the curve is out of bounds for the reason a demand meter is | `01 §1`, `§4` banner |
| 9 | **The ground carries resources, and developing over one without extracting it forfeits it.** No clearing verb and no terrain tool: Woodland is ordinary ground with something on it. Sequencing — zone Extraction, harvest, re-zone — is *intended play* and is priced in Days the block houses nobody, because the Unplaced Pool does not wait | `CONTEXT.md` → Zone, `01 §3`, `adr/0022` amendment |
| 10 | **The Unplaced Pool is shown as a count *and* inspectable as a population.** An RCI meter is a synthesised scalar with no constituents; this is a count of real named records, so it is not *added*, it is counted. Two constraints: it must not read as an error state (a Pool of zero means immigration stopped), and it is decomposed by default rather than shown as one figure | `01 §1` |
| 11 | **`Demolish` is the sixth verb**, over Streets and Buildings alike, taking over the bulldoze `Connect` has carried unnamed since `adr/0077`. The pillar never forbade it — `01 §2` says the player never **places** a Building, and removing one is not placing one | `adr/0091`, `01 §2` |
| 12 | **Clearing is bought rather than taken.** Market value, read off the land value Map Layer, paid to the displaced Households — so it is a **transfer** under `adr/0024` rather than a sink, and it adds **no authored number**. The composition is unset with a named ratifier | `adr/0091`, `0002` §D2 |
| 13 | **No act in this design yields empty ground except the one that pays full price.** Compulsory purchase under `Connect` leaves an Arterial, under `Service` a service Building with an upkeep obligation. That is what stops the priced routes being a cheap bulldozer, structurally rather than by pricing | `adr/0091`, `01 §2` |
| 14 | **Re-zoning withdraws *replacement*, never the Building.** `ZoneRuleEngine.Condemn` is keyed on Rule Instance starvation and never reads the permission set (`adr/0055`), so a churning district empties itself and a healthy one is not cleared this way at any speed. Stated as *the Zone Rule condemns in its own time* it would have been a second pressure source, which `adr/0079` closes | `adr/0091`, `01 §2` |
| 15 | **Abandonment leaves the shell standing.** `02 §5.9` said both, twelve lines apart, and the build took the wrong half. The shell is what **carries** contagion, what `01 §6`'s detection duration is *derived from*, and what the clearance lever clears. It is **not** *derelict* — that term is a Ruleset-edit state and shares none of its machinery | `02 §5.9`, `CONTEXT.md` → Failure Pressure, `adr/0091` |
| 16 | **The region view is a zoom level of the one map**, not a second screen — zoom out and the ground stops being drawn and the Settlement graph is drawn at true positions. A toggled view is refused because SC4's region screen was a different *game state*, which `adr/0020` refuses, and because a screen you switch *to* reads as a menu of places when the property that makes the diagram honest is that nobody chose what is on it | `adr/0092`, `01 §1` |
| 17 | **A trajectory is reported first at the place its mechanism makes.** Settlement → District, one hierarchy and not two axes, because a Settlement *is* a set of Districts. Commute-shed failures (Gridlock, Labour mismatch, Retention) at the Settlement; policy and pooling failures at the District | `adr/0092`, `01 §6` |
| 18 | **`§3` step 1 is reading the edges, so the first verb used is `Inspect`.** Every Outside Connection is live from Tick 0 and carries its own Hinterland's figures, so the gates are not interchangeable and the choice is real. `§5.6` already called this *"the opening's real reconnaissance"* and `§3` had no such step | `01 §3` |
| 19 | **`TICKS_PER_DAY = 2048`.** A Tick is 42.1875 s of in-world time, a Day is 2m08s at 1×, and a twenty-hour campaign holds **562 Days** against 140 — which `§4`'s Replacement Rate marker needs and which no Life Stage length could have supplied at 8192. Taken on `adr/0019`'s explicitly forbidden ground, **pacing**, because `adr/0082` deleted the premise that forbade it | `adr/0094`, `01 §1`, `§4` |
| 20 | **What rescales is decided by one question: what is the quantity denominated in?** Ticks are kept at their number; in-world time is unchanged and there is four times more of it per real second; Days are unchanged and four times faster in real time. **Only Goods quantities rescale, ×4.** The alternative — divide every Tick cadence by four — holds the in-world meaning and quadruples the cost, and the authored intent was the **visible pace** | `adr/0094` |
| 21 | **The speed ladder keeps 2× and 4× and gains 3×.** `§1`'s *no 8×* rule reads as a floor on Day length and is keyed on **events the player can perceive per second**, which did not move. What moved is that **Day-scaled phenomena have a lower top watchable speed than everything else** — the commute peak is 11 seconds at 4× — which is `§7`'s Study argument one rung up, not a new problem. 4× is *getting somewhere*, not watching | `01 §1` |
| 22 | **A Commute Budget is three rungs — fast 20, moderate 40, unsavoury 50 — and only the ceiling refuses.** A single threshold makes a cliff out of `adr/0017`, and reports **zero** while every commute in the city creeps from twelve minutes to nineteen. The **unsavoury** rung is where `§4`'s *housed* Departures come from, and no fifth Trip Fate is opened | `adr/0095`, `01 §4` |
| 24 | **The Microscopic Cap derives from the design speed's 62.5 ms, not the top rung's 15.6 ms.** Pricing it at 4× chooses `03 §3.9`'s **simulation** degradation — every player permanently less accurate — to avoid its **hardware** one, which is one machine dilating and saying so. That is the section's own table read backwards, and `01 §1` makes 4× the rung a large city withdraws anyway. **Recorded rather than decided beside it**: a fallback tier below Microscopic is **foreseen and deliberately undesigned**, and a **2- and 4-thread** Lane kernel measurement is owed to S5 | `adr/0096`, `03 §3.9` |
| 23 | **The map stays at `WorldCells = 512`, on a recomputed ratio rather than the one it was granted.** `adr/0089` sized it at 3.7–5.2 crossings for a 30-minute Budget, and 30 minutes is a rung that no longer exists: **5.6–7.8 at the fast rung, 2.2–3.1 at the ceiling**, and the ceiling governs. Two or three separable settlements is not the blob that ADR exists to prevent | `adr/0095`, `adr/0089` |

## What was written

- **`docs/01-player-experience.md`** — `§1` gains the Day-long loop turn, the speed table, *waiting and
  getting nothing is the reading*, *the hour markers are expectations not a script*, the Unplaced Pool
  section, and a second half for `Diagnose`. `§3` is substantially rewritten: the empty map, the stub, the
  consequence-not-a-fence paragraph, the forested-seed fifth step, founding capital, and the corrected
  budget sentence. `§4` gains a banner pointing at decision 8.
- **`CONTEXT.md`** — → Zone gains *the ground carries resources* and the sequencing trade; → Woodland
  gains a pointer saying it is neither a verb nor an obstacle; → Frontage's *reasons a Lot is vacant* goes
  from a four-item sentence to a **six-row table**, and is named the one canonical copy.
- **`docs/adr/0022`** — amended: how the player clears forest (no sixth verb), and that its macro-arc was
  **inert at 16.4 km**.
- **`docs/adr/0089`** — consequence list amended to name `adr/0022`, the largest distance-dependent claim
  it missed.

Then, after the pin was lifted:

- **`docs/adr/0091`** — clearing land is bought rather than taken, and `Demolish` is the sixth verb.
- **`docs/adr/0092`** — the region view is the map from far away, and a trajectory names the place it is
  reported at.
- **`docs/01-player-experience.md`** — `§1`'s `Observe` row names four surfaces and gains *the region view
  is a zoom level*; `§2` becomes **six verbs** with the four clearing routes and the removing-is-not-placing
  paragraph; `§3` step 1 becomes **read the edges** and the list renumbers to six; `§6`'s spatial axis
  becomes the Settlement → District hierarchy with a level column, and its clearance lever gets its
  referent back.
- **`docs/02-simulation-model.md` §5.9** — the abandoned shell **stands**; the *"Lot returns to vacant"*
  half is corrected in place with the three things that depended on the other half.
- **`CONTEXT.md`** — → Failure Pressure gains the standing shell and its `adr/0006` sink; → Derelict's
  *"it stands until the player clears it"* acquires the verb it had been asserting since `adr/0057`.
- **`docs/adr/0022`** — the forest amendment's *"there is no sixth verb"* corrected: one exists and does
  not reach ground.
- **`plans/0002`** — two coverage-map rows, and a §D2 row for the compulsory purchase price.

Then, on 2026-08-13, grilling `§4`:

- **`docs/adr/0094`** — a Day is 2048 Ticks, because Ticks per Day is a sampling rate and not a length of
  life. **`docs/adr/0095`** — a Commute Budget is three rungs and only the last one refuses.
- **`docs/adr/0019`** — amended a **second** time, and this time the **conclusion** falls: *Why shortening
  the Day is not a pacing change* struck in full, the *"free in another currency"* claim struck, the speed
  ladder superseded, and the revisit trigger marked as pointing backwards.
- **`docs/adr/0082`** — its *"`TICKS_PER_DAY = 8192` stands"* clause superseded, and a banner naming the
  two live consequences it left standing on the premise it deleted.
- **`docs/adr/0089`** — the *Buildable for 1M* column renamed to **Occupied by 1M**, and the 30-minute
  ratio restated at two rungs.
- **`docs/01-player-experience.md`** — `§1` gains a five-rung speed ladder with Tick budgets, the restated
  refusal rule and the `plans/0013` ×4; `§4` gains the graded commute, the Bill-axis reconciliation and
  the recomputed twenty-hour marker; `§5.1` gains *two scarcities read as long commutes*; `§5.5`'s *"§3
  removes budget pressure"* is struck.
- **`CLAUDE.md`** — three constants rows rewritten, the Commute Budget row reshaped, the ADR count 92 → 94.
- **`plans/0002`** — two coverage rows, the `01` row closed, three new §D2 rows (`TICKS_PER_DAY`, a
  Household's life in Days, which rung governs separability) and two amended.
- **`plans/0003`** — hash-moving queue **item 7**, ungated and not urgent.
- **`plans/0012`** — the developed density is circular, filed as Cause 1 with the tell absent by
  construction.

Corpus tests green throughout (`CitationTests`, `CoverageMapTests`, `MarkdownStyleTests`), all links
resolve.

## The findings, and they are one finding

**Seven times in one sitting, the answer was already written down in a place nobody had read it.**

| What was about to be decided | Where it already was |
|---|---|
| whether an empty Lot is information | `01 §2`, three sections from `§1`: *"when a zoned Lot stays empty, that is information"* |
| the list of reasons a Lot is vacant | `CONTEXT.md` → Frontage held four, and `§1` was one edit from becoming a second copy that disagreed |
| perception versus response | `PlacementEngine.cs:72`'s own comment, reached from the Census side |
| that the first ten minutes contain a forestry decision | `adr/0022`, which wrote a step into `§3` that `§3` never had |
| that `adr/0022`'s arc needs distance | itself, worded in *retreat* and *reachability* and never in metres — which is why `adr/0089`'s sweep missed it |
| whether the player may clear land at all | `CONTEXT.md` → Derelict, since `adr/0057`: *"it stands until the player clears it"* — a vocabulary entry asserting a player act the verb list did not contain |
| that the District-scale clearing tool is `Govern` | `01 §6`, which lists *clearance of abandoned stock* among the recovery levers and then says of that list *"per §2 they are ordinary **Policies**"* |

**Nine by the end of the session, and three of the last four were not in prose at all** — a doc-comment,
a plan's own recommendation, and an ADR's summary sentence, each describing a mechanism and each wrong
about its **trigger**. That is a different failure from the one above, and it is now
[`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
and `plans/0012` **Cause 4**: the others are a fact with two copies drifting, or a write that did not
land, or a read never repeated, and **this one was never true when it was written**. It is also the only
one the corpus cannot check, because all three of its mechanical checks are document-to-document.

The generalisation is worth keeping: ***a claim about distance is not always a claim that says so***, and
more broadly, **a corpus this size fails by non-reading long before it fails by non-writing**. Session J
found the same thing twice on the same day (`05 §3` contradicting `05 §7` inside one file, and S2 R1.5's
unread column). Three sightings in one day is a pattern, not a coincidence.

## Closed — how the player clears a developed block

**Answered 2026-08-12 into [`adr/0091`](../docs/adr/0091-clearing-land-is-bought-rather-than-taken-and-demolish-is-the-sixth-verb.md), decisions 11–15 above, and the recommendation was half wrong.**

The recommendation's *no sixth verb* was withdrawn **by the user, on the ground the corpus had already
written down**: `CONTEXT.md` → Derelict says a derelict Building *"stands until the player clears it"*,
which has had no verb behind it since `adr/0057`. And the pillar this design keeps invoking does not
forbid a bulldozer — `01 §2` says the player never **places** a Building, and removing one is not placing
one. The player still cannot author what stands anywhere; only veto what does.

Four routes, differing in scale and in who is displaced:

| Route | Verb | Over what | Price |
|---|---|---|---|
| **Demolish** | `Demolish` | anything the player selects | market value, paid to the displaced |
| **Compulsory purchase** | `Connect` (Arterial), `Service` | whatever is in the way of the act | the same price, charged as part of the act |
| **Clearance programme** | `Govern` | abandoned stock only | funded from the treasury |
| **Withdraw replacement** | `Zone` | nothing standing | free, and null in a healthy district |

**Three things the code decided that no argument would have.** Compulsory purchase is an **Arterial**
mechanism exclusively — `StreetGrid` snaps Segments to block boundaries and a Lot is a point on a block
face, so a Street can never run through a Building, while `RoadGenerator` walks an Arterial as a freeform
polyline destroying every Segment it crosses. *Re-zone and wait* does not wait, because `Condemn` never
reads the permission set. And `02 §5.9` **contradicts itself twelve lines apart** about whether an
abandoned Building stands — the build took the half that leaves contagion with no carrier and `01 §6`'s
derived detection duration with nothing to derive from.

## Closed — `Observe`, and `§3` step 1

**Both examined 2026-08-12. Decisions 16–18, and [`adr/0092`](../docs/adr/0092-the-region-view-is-the-map-from-far-away-and-a-trajectory-names-the-place-it-is-reported-at.md) for the first two.**

`Observe`'s row read *"map overlays and the aggregate panels"* and was missing a view **five other places
describe in identical words** — the region view, *a diagram of the commute sheds the city actually has*
(`adr/0020`, `adr/0085`, `02 §2.1`, `CONTEXT.md` → Settlement, and `00-vision.md`, which calls it one of
the two things belonging in a vision). A view named everywhere except in the document that owns what the
player looks at is a view with **no owner**, and the two questions an owner has to answer are the two
nobody describing it ever had to: is it a screen, and what unit does it report at.

`§3` step 1 gained the step `§5.6` had already named. That section says a player *"faces four outside
economies of different character, discovered by reading the world, **which is the opening's real
reconnaissance**"* — and `§3`'s opening had no reading step at all. Same shape as `adr/0022` writing a
forestry step into `§3` that `§3` never had, which was finding 4 of the original five.

**The largest of the three findings is that `01 §6` had made a decision it could not have made.** *Every
trajectory must be expandable by District* was not District chosen over Settlement; at 16.4 km the map is
one Settlement and the alternative had **no instances**, which `adr/0085` found S2 R1.5 had already
measured. ***A decision taken when the alternative had no instances is re-decided rather than defended***,
and nothing in the corpus flags such a decision when the map changes underneath it — `adr/0089` re-opened
four distance-dependent claims by name and this was not among them, which is the second time in two days
that ADR's consequence list has come up one short.

## Unparked and written — the road write-up

**All three done 2026-08-12, and one of them found the reason it was hard was false.**

- **[`adr/0090`](../docs/adr/0090-the-generator-makes-land-and-the-player-makes-every-road.md)** written.
  The map is open; the `adr/0011` damper argument is **given up rather than answered**, with the two
  causal dampers that replace it named.
- **[`adr/0077`](../docs/adr/0077-a-road-edit-is-one-segment-and-the-player-lays-streets-only.md) amended.**
  The edit unit is a **run** of Segments; `ConnectPayload`'s bits 2–7 are free, so **63 Segments — 8.06 km
  in one command** — with the Input Log at version 1. `ConnectAction.Bulldoze` leaves for `Demolish`,
  freeing bit 1 and a ceiling of 127 nobody has spent. ***A revisit trigger names one way a decision can
  fall, and a decision usually falls another way***: this ADR predicted a finer `block_tiles` and what
  actually fired was `adr/0090` plus `adr/0089` — the player now lays every metre of road on a map
  sixteen times the area.
- **`plans/0002` ledger #2 closed as refused**, `01 §8 Q3` struck with its recommendation kept below the
  strike, and `plans/0003` item 6 rewritten.

⚠ **The finding is that `adr/0089`'s stated blocker is false, and two questions had been welded.**
`RoadGenerator` does not pave at world creation — it has one production call site, `SyntheticCity`,
reached only by `CommandKind.Populate`, *a verb no player has*. A player's world has had **no roads since
5a**. `CellGrid.cs`'s comment says *"at world creation"* and that sentence is what both `adr/0089` and
ledger #2 reasoned from, which makes it the **third decision in three days taken from a sentence about
the code rather than from the code** (`adr/0064`'s loader, `adr/0079`'s pressure source). So the flip to
512 waits on a bounded mechanical fix in a spike verb, not on the player's freedom — and `adr/0089`'s
*"must not be answered by capping the generator"* was right about the design question and wrong about
which question the cap belonged to.

*Original parked list, for the record:*

1. **`adr/0090`** — the generator makes land and the player makes every road; the **open map** recorded as
   a stated refusal of `01 §8 Q3`'s unlock-by-serviceability recommendation.
2. **`adr/0077` amendment** — the road edit unit becomes one **run** of Segments (click and drag), which
   fits `ConnectPayload`'s six spare bits up to a 63-Segment run with no Input Log version bump. The
   Streets-only restriction is **discharged** rather than overturned, since `01 §2` already tags it as an
   ⚠ *as built* caveat with its successor written beside the refusal.
3. **`plans/0002` ledger #2** closes as *refused*, with `01 §8 Q3` amended and its `adr/0011` damper
   argument explicitly given up.

Downstream of those, on the code track: `plans/0003` hash-moving-queue **item 6** — scope `RoadGenerator`
to developed land, then flip `WorldCells` to 512 and re-record all three golden baselines. One commit of
its own, and it is `adr/0089`'s named blocker.

## Closed — `§4`, which was never on the plan

**Grilled 2026-08-13. Decisions 19–23, [`adr/0094`](../docs/adr/0094-a-day-is-2048-ticks-because-ticks-per-day-is-a-sampling-rate-and-not-a-length-of-life.md)
and [`adr/0095`](../docs/adr/0095-a-commute-budget-is-three-rungs-and-only-the-last-one-refuses.md).**

`§4` is two sentences long and states two markers. **Both were unsupportable and for opposite reasons.**
The two-hour marker rests on the Commute Budget and the Budget was a **step function**, so the signal it
produces arrives at the moment a spatial fix has stopped being cheap. The twenty-hour marker rests on
**Replacement Rate**, a quantity that takes generations, and a twenty-hour campaign contained **140
Days** — no generation at any plausible Life Stage length. *The deepest skill the game asks for was
scheduled to arrive after the campaign ended*, and the section had said so in plain words since it was
written.

**The finding is that `adr/0082` deleted a premise and left two consequences standing on it.** It
inverted `adr/0019`'s clock — a Tick's duration became **derived from** `TICKS_PER_DAY` — and the section
titled *Why shortening the Day is not a pacing change* needs the opposite. Its argument, *"same city,
same population, same roads, twice the vehicles"*, requires a commute to be a fixed number of Ticks; under
the new chain a commute is **1.39% of a Day at 8192 and 1.39% at 2048**, so the constant cancels and there
is no rebalancing at all. `adr/0082` kept the number and never re-derived what stood on it, which is
`adr/0089`'s ***the obligation a deletion creates is a re-derivation, not a retraction*** for the **third
time in two days**.

**And it left a live wrong instruction in a revisit trigger.** `adr/0019` says that if vehicles cost too
much, *"4096 with doubled vehicle speed is the correct response"*. Under two clocks that makes the problem
worse by exactly the factor it is reached for to improve: the vehicles in flight do not move, and the
**sub-step ratio** does. That is `adr/0093`'s failure on an axis `adr/0093` does not cover — not a
description of the **build** going stale, but a **consequence** whose premise was replaced in another
file.

**Two smaller ones, and both are about a sentence I nearly acted on.** `§1`'s *no 8×* rule is stated as a
threshold on Day length and is **keyed on events per second**, which is a different quantity; reading it
as written would have deleted 2× and 4× from the ladder for no reason. And `§4`'s *"the fix is not more
money — it is geography"* looked like a flat contradiction of `§5.1`'s *"buyable out of? Yes, always"* and
is not one: they are about where money must be **pointed** and about whether a priced route **exists**.
**Neither section could have said so when it was written** — the route that reconciles them is
`Demolish`, which is a day old.

*(One correction landed the other way. `§5.5`'s *"§3 removes budget pressure"* is simply stale: this
session's own decision 4 says §3 removes budget **failure**, so the Bill is expressed from the first
purchase and the opening is **not** nearly identical at every dial setting.)*

## The session's own worst moment, and it is the sharpest finding

**`adr/0094` shipped with a ratio whose denominator does not exist, and the document it was borrowed from
had predicted that failure in writing, in the paragraph directly beneath the number.**

The claim was that the clock change opens a **27–58×** gap between what the Lane kernel can afford and
what a city demands. The demand figure — 186,624 — is S2 R2's synthetic **fleet**.
[`plans/0013`](0013-tick-budget.md) says so **in the table cell itself**:

> | 186,624 — S2 R2's fixture, **not a stressed count** |

and closes that section:

> *"How many Vehicles a real city stresses at once is milestone 5b's and does not exist, which is why **no
> row in this table claims a share for it**. A number becoming a decision by being the only number in the
> room is a habit this corpus has already recorded, and **this table is where it would happen**."*

**The Cap counts Vehicles in *stressed* Segments and 186,624 is a whole fleet**, so the two were never
comparable. And the supply side was quoted at 15.6 ms while the clock moved, which double-counts, because
the Tick budget scales on the same ladder — at the **design speed** the figure is **~25,400 Vehicles on
one core, identical to what the old clock produced at its own top rung**.

***A caveat attached to a number does not travel with it.*** The annotation was correct, in the right
place, and had been read hours earlier; what crossed into the ADR was the digits. This is not
`plans/0012`'s Cause 1 — nothing drifted, the two copies never disagreed — and it is not Cause 4, since
no description of the build was involved. **It is a fifth thing and it is now `plans/0012` Cause 5**: a
number quoted away from the sentence that qualifies it, whose tell is *worse* than nothing, because a
figure repeated verbatim accumulates apparent authority each time it appears.

**Cause 5 is Cause 4's sibling with the polarity reversed** — there the source sentence is wrong, here it
is right and was left behind — and the two have the **same ending**: both were coined in the corpus as
commentary, twice each, and left non-binding, and both then happened to a sitting that had just read the
coining. `plans/0002`'s *an unratified number is more dangerous than an open question* and `plans/0013`'s
*a number becoming a decision by being the only number in the room* are this cause, written down and not
made a rule. ***An aside is not a rule*** is now evidenced on two causes rather than one. Unlike Cause 4
it **is** mechanically checkable, because both ends of a travelling number are documents, and `plans/0012`
gains **check 6** on the back of it.

**Two smaller notes on the same episode.** The user's *"the new number is clearly untenable"* was the
correct reading of what I had written and the wrong reading of the world, which is what an unqualified
number does — it recruits the reader into acting on it. And the repair produced a decision that was worth
taking on its own merits ([`adr/0096`](../docs/adr/0096-the-microscopic-cap-derives-from-the-design-speeds-budget-and-not-from-the-top-rungs.md)'s
budget basis) and **nearly produced a mechanism nobody needs**: designing a fallback tier against an
unmeasured shortfall is `adr/0070`'s void question in its exact stated form, and the session got to the
edge of it before checking the denominator.

## Small debt

`adr/0082` and `CONTEXT.md` both cite **`05 §26`**; `docs/05-technical-architecture.md` has eleven
sections. Filed in `plans/0002`, not fixed.
