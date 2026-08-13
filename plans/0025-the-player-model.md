# The player model — grilling `01 §1` and `§3`

⚠ **PINNED MID-SESSION 2026-08-12.** Ten decisions taken and written; one question open and stated
verbatim at the bottom. This document is the resumption point.

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

## Parked from earlier the same day, not abandoned

The road write-up this session interrupted. All three are decided and unwritten:

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

## Small debt

`adr/0082` and `CONTEXT.md` both cite **`05 §26`**; `docs/05-technical-architecture.md` has eleven
sections. Filed in `plans/0002`, not fixed.
