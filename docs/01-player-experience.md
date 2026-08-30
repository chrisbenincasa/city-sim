# Player experience

What the player actually does, what it feels like over a session and over a campaign, and how the game communicates a simulation this deep without drowning anybody.

This document owns two open decisions that propagate widely: **whether transit is ever implemented**, and **whether car ownership is a choice**. Both are logged in §8.

---

## 1. The core loop

> **Observe → Diagnose → Intervene → Wait → Observe**

The `Wait` step is not filler. It is where the simulation does the work the player is here to watch, and the design's job is to make waiting *interesting* rather than idle. That means the city must be legibly changing at all times: Households arriving and departing, Trips succeeding and failing, Businesses stocking and starving.

Each step has a home in the interface:

| Step | Where it happens |
|---|---|
| **Observe** | The map, the **region view** it becomes when zoomed out, the overlays over both, and the aggregate panels |
| **Diagnose** | **Evidence** — drilling from any aggregate to its named constituents |
| **Intervene** | The six verbs (§2) |
| **Wait** | The city, running |

**One turn of the loop is about a minute or two of real time, and that is what a Day is.** It is not a
target imposed on the simulation; it is what the simulation's own cadences already are. Placement, job
assignment and the Zone Rules look at every candidate once per **1,024 Ticks** — 64 seconds at 1× — and a
commute recurs **daily**, which is **2,048 Ticks**, or 128 seconds. So a change can be made, noticed,
acted on and observed inside about two minutes, and the Day is the outer edge of that window.

The speeds exist to make that interval watchable, and **1× is the speed the game is designed to be played
at**:

| Speed | Ticks/s | One in-world Day | Tick budget | What it is for |
|---|---|---|---|---|
| **Pause** | 0 | — | — | Planning. The verbs work while paused |
| 0.5× | 8 | 4m 16s | 125 ms | Slowing down to watch one thing happen. **Traffic is visually truthful here** (§7) |
| **1×** | 16 | **2m 08s** | **62.5 ms** | **The design speed. The game must be enjoyable here** |
| 2× | 32 | 1m 04s | 31.25 ms | Comfortable once a city is settled |
| 3× | 48 | 42 s | 20.8 ms | Fast-forward that still shows a commute peak |
| 4× | 64 | 32 s | **15.6 ms** | **Getting somewhere, not watching.** ~~The first thing a large city stops offering~~ — **the target ([`adr/0105`](adr/0105-the-target-speed-is-4x-at-a-million-and-a-rung-dilates-rather-than-being-withdrawn.md))** |

> ⚠ **The struck clause was this table's only claim that a rung is conditional, and it is refused
> outright.** [`adr/0105`](adr/0105-the-target-speed-is-4x-at-a-million-and-a-rung-dilates-rather-than-being-withdrawn.md),
> session **T**, 2026-08-16: **every rung is offered at every city size for ever, and 4× at 1,000,000
> Citizens is the budget the simulation is targeted at.** A host that cannot sustain the rung the player
> chose **dilates wall-clock time and reports *simulation running behind***
> ([`adr/0019`](adr/0019-ticks-per-day-is-a-balance-constant-not-a-pacing-knob.md)'s rider,
> [`03 §3.9`](03-agent-architecture.md)'s second row) — it never takes the control away.
>
> **Withdrawal and dilation are different things and only the first is refused.** Nothing here promises
> 64 Ticks/s at 1M on unknown hardware; no game could. What is promised is that the control is present,
> that the simulation is identical whichever rung is chosen, and that falling short is announced.
> ***Options disappearing as a city progresses is a worse experience than a rung that runs slower than
> it says*** — and the loop above is why: `Wait` is hardest to close in a large city, which is exactly
> where the struck clause proposed to remove the fast-forward.
>
> **1× keeps its own job and it is not the target.** It is the speed the game must be *enjoyable* at,
> and it is what a **capability** is priced against — the Microscopic Cap stays on a 62.5 ms basis
> ([`adr/0096`](adr/0096-the-microscopic-cap-derives-from-the-design-speeds-budget-and-not-from-the-top-rungs.md),
> whose revisit trigger fired here and whose conclusion survived). The **bill** targets 4×; a **fidelity
> ceiling** does not.

The Day lengths follow from `TICKS_PER_DAY = 2048`
([`adr/0094`](adr/0094-a-day-is-2048-ticks-because-ticks-per-day-is-a-sampling-rate-and-not-a-length-of-life.md)),
and a Day at 1× is four times shorter than it was, because the twenty-hour marker in §4 asks the player to
read a demographic quantity and the old constant put **140 Days** in a twenty-hour campaign. It now puts
**562**.

**The rule that stops the ladder is about events, not about the calendar**, and it was written the other
way round on the day the ladder was: *at 8× a Day is sixty-four seconds and `Observe` has nothing left to
observe*. That reads as a floor on Day length and it is not one. What the player can observe is governed
by **how many events arrive per real second**, which is set by the tick rate and by cadences authored in
Ticks — and neither of those moved when the Day did. A Rule firing every 64 Ticks still fires every four
seconds at 1×.

**What a shorter Day does change is that Day-scaled phenomena have a lower top watchable speed than
everything else.** The commute peak spreads over a third of a Day, so it is a 43-second event at 1× and an
11-second one at 4×. That is not a defect and it is not new: §7 already says the slowest speed is *"the
speed at which rendered traffic is visually truthful"*, and this is the same observation one rung up.
**Different phenomena stay legible to different speeds, and the ladder's job is to say which** — which is
why 3× exists and why 4× is described as getting somewhere rather than as watching.

~~**A consequence for [`plans/0013`](../plans/0013-tick-budget.md), which had assumed otherwise.** That
ledger… **should be read against the design speed**, where the same work is a fraction of a 62.5 ms
Tick — so an over-budget figure at 4× is a statement about which speedups a city of that size offers,
which is `HONEST DEGRADATION`, and not a statement that the game does not run.~~
⚠ **REVERSED 2026-08-16 by [`adr/0105`](adr/0105-the-target-speed-is-4x-at-a-million-and-a-rung-dilates-rather-than-being-withdrawn.md).**
The escape this paragraph offered — *read the ledger at 1× and the problem goes away* — is exactly the
two-point specification session T refused, and it is refused because the thing it disposes of is a
**rung the player would have lost**. [`plans/0013`](../plans/0013-tick-budget.md) is now denominated in
**15.6 ms** and reads **283–318%**, and that gap is a target with a number rather than a reason to
change denominators. `HONEST DEGRADATION` still applies and it applies to **dilation**, not to
withdrawal.

**What this paragraph got right and keeps.** ⚠ **`adr/0094` moves the routing row ×4** — route searches
fire per Trip, Trips are daily, and Days now arrive four times faster — taking the ledger from ≥17.8 ms
to roughly **47.6 ms a Tick**, which is **one bill read at several rungs**. ⚠ **This document was
carrying that correction for three days while the ledger that owns the sum was not**; session T found
`plans/0013` still summing to ≥17.8 ms, having applied the identical reasoning to its *volume
attribution* row on 2026-08-14 and not to routing. ***A correction attached to a number does not travel
with it any more readily than a caveat does.*** The row's multiplicand still counts the wrong event and
the known-direction correction still points **up**.

### The region view is a zoom level, not a second screen

[`adr/0092`](adr/0092-the-region-view-is-the-map-from-far-away-and-a-trajectory-names-the-place-it-is-reported-at.md).

**This document had never mentioned the region view, and five other places in the corpus had.**
[`adr/0020`](adr/0020-one-live-world-and-settlements-are-derived.md), [`adr/0085`](adr/0085-nothing-on-this-map-is-far-away-so-a-settlement-is-made-by-a-gap.md), `02 §2.1`, `CONTEXT.md` → Settlement
and `00-vision.md` all describe it in the same words — ***a diagram of the commute sheds the city actually
has, not a menu of tiles anyone chose*** — and the vision document lists it among the two outcomes that
belong in a vision rather than a technical file. `Observe`'s row named overlays and panels and stopped
there.

**Zoom out far enough and the ground stops being drawn; the Settlement graph is drawn in its place.** One
camera and one continuous gesture, which is what `adr/0020` priced this at — *"UI over derived state, so
it costs a camera and a stats panel, not a subsystem"*. A Settlement's position on that diagram is its real
position, so nothing has to be laid out: the diagram is what the map looks like from far away.

**A separate toggled view was refused, and the reason is SimCity 4's.** Its region screen was a different
*game state*, which is the thing [`adr/0020`](adr/0020-one-live-world-and-settlements-are-derived.md)
decided against when it made one world live at all times — and a screen the player switches *to* invites
reading the diagram as a menu of places, when the whole property that makes it honest is that nobody chose
what is on it. Under `adr/0089` the map is 65.5 km across and a 1M city occupies 6.3% of it, so this is not
a convenience: **the thing the player navigates by cannot be the ground.**

Overlays follow the camera rather than stopping at it. At map zoom an overlay tints Cells; at region zoom
it tints Settlements, which is the same figure aggregated to the unit being drawn, and it inherits §7's two
rules unchanged — never sharper than the player can act on, never sharper than the simulation underneath.

### Waiting and getting nothing is the reading

**Nothing bounds how fast the city responds to the player, and nothing may.** How quickly a zoned Lot
fills *is* the demand signal: build where the city needs something and it appears within seconds, which is
the reward and the confirmation; build where it does not and the ground stays empty for as long as that
remains true. §2 states the same thing from the verb's side — *when a zoned Lot stays empty, that is
information* — and it is why this design has no RCI meter. **A stated maximum latency between an action
and its consequence would be a demand meter through the back door**, obliging the city to answer on a
clock whether or not anybody wanted the thing.

What the cadences bound is **perception, not response**. `revisit_ticks` is how long before the city has
*looked* at a Lot, and the player must be able to tell *nobody wants this* from *nobody has checked yet*,
or the silence carries no information at all. That distinction is load-bearing and was nearly lost:
[`adr/0069`](adr/0069-placement-is-a-mechanism-of-its-own-and-construction-houses-nobody.md)'s
`revisit_ticks` shipped at one Day — **eight and a half minutes at 1×** — and was lowered to 1,024 Ticks
for an equilibrium reason, that 45% of the housing stock stood empty. The number is right and its recorded
justification is half of why.

### The Unplaced Pool is shown as a count and read as a population

The Pool is this design's demand signal and there is no other one, so what `Observe` does with it decides
whether the game has an RCI meter by another name. **It does not, and the reason is not that the number is
hidden — the number is shown.**

**An RCI meter is a synthesised scalar with no constituents.** Nothing in SC4 *is* the RCI value; it is a
formula's output drawn as a bar, and it cannot be interrogated when it is wrong, which is what makes it
prescriptive. **The Pool count is a count of real records**: 1,240 means 1,240 Households exist and every
one of them is named, with money, a work requirement and a reason it left the last place. `CLAUDE.md`'s
rule is *do not **add** a demand scalar* — this one is not added, it is counted. Withholding a number the
simulation knows would sit badly beside a design whose whole promise is a city that will not lie to you.

Two constraints on how it is shown, and they are where this could still go wrong:

- **It must not read as an error state.** A healthy city always has people arriving, and a Pool of zero
  means immigration has stopped, which is usually the worse condition. Rendered as a warning that clears
  when satisfied, it teaches a false goal — a verdict smuggled in through styling. It is a **level**, like
  a Bin's, and never an outstanding-work queue. `NO VERDICT`
- **It is decomposed by default rather than shown as one figure.** *"1,240 waiting"* reads as *build 1,240
  units*; the truth is usually *these 900 need cheap housing near the industrial district and your new
  suburb is forty minutes away*. Same information, grouped so it can be acted on. The count is the head of
  the list, never a substitute for it, and the drill-through is `§5`'s existing facility pointed at a
  queue. `UNIQUE INDIVIDUALS`

**The build already distinguishes the two readings and this document did not.** `PlacementEngine`'s Census
family reports *considered* against *placed*, and its own comment gives the reason: a Pool being looked at
and not housed is a city out of dwellings, where a Pool not being looked at is a different condition
entirely. That is the perception-versus-response distinction above, reached from the instrumentation side
before it was reached from here.

### Diagnose has two halves, and only one of them is built

**Evidence** explains what happened: it drills from an aggregate to the named constituents behind it (§5).
It cannot explain a **non-event**, and under the paragraph above the non-event is the design's most common
signal.

Hence the second half: **selecting an empty Lot re-runs the Zone Rule's predicate against it and reports
which clause failed.** `CONTEXT.md` → Frontage owns the list and there are six; this section deliberately
does not repeat them. What matters here is that they **present identically as bare ground**, that several
are player mistakes with fixes rather than statements about demand, and that one of them — *not looked at
yet* — is a transient produced by the engine's own cadence. Leaving them indistinguishable would fail
`LEGIBLE CAUSE` at the exact moment the design leans hardest on it.

**The reason is recomputed on the click and never recorded.** It costs no column, no State Hash coverage
and nothing inside `step()` — a cold path in `05 §4`'s sense — and it cannot drift from the behaviour it
describes, because it *is* the predicate. The one thing it cannot answer is why the city declined an hour
ago, which differs from why it would decline now only during a transient; the repair is to report **when
the Lot was last looked at**, and that is the only piece of stored state this whole facility needs.

### The hour markers are expectations, not a script

**This is a simulation sandbox, not a story.** §3 and §4 describe what we *expect* to emerge at roughly
certain points, and the game is tuned to trend that way — but **nothing enforces them and nothing should**.
A player who meets the first spatial constraint at forty minutes, or never, has not found a bug. They have
played differently, which is the proposition. `NO VERDICT`

The distinction changes what a pacing claim licenses. *"The first genuine constraint should be spatial"*
(§4) is a statement about what the simulation makes **likely** — put housing and jobs apart and commutes
lengthen — and it is discharged by the mechanism existing and biting when the conditions occur. It is
**not** a licence to detect the two-hour mark and arrange for something to happen. Any mechanism that would
*guarantee* the curve is out of bounds for exactly the reason a demand meter is: it substitutes the
designer's intention for the city's answer.

So read every duration in §3 and §4 as a prediction the design is accountable for making *plausible*, and
never as a promise it is accountable for *keeping*.

The loop is deliberately slower than a builder's. This is not a game about laying track *efficiently* —
the qualifier is the whole sentence, since the player lays every metre of it (§3) — it is a game about
forming a hypothesis and testing it against a city that will not lie to you.

---

## 2. The player's verbs

There are six, and the list is short on purpose. `PLAYER GOVERNS`

| Verb | What it does | What it does *not* do |
|---|---|---|
| **Zone** | Paint a permission set over land — Residential, Commercial, Office, Industry-Extraction, Industry-Processing — with a density band | Place buildings. Zone Rules decide what actually gets built, and when. The band is a **ceiling**, never a floor |
| **Connect** | Lay Streets on the grid, draw Arterials, place authored Junction pieces | Micromanage lanes, signals, or turn restrictions |
| | ⚠ *As built (5a-bis): **Streets only**, one **Segment** per act — an origin intersection and an axis, lay or bulldoze ([`adr/0077`](../docs/adr/0077-a-road-edit-is-one-segment-and-the-player-lays-streets-only.md)). Arterials and Junction pieces are **refused by name** with their successor written beside the refusal, because a spline is many control points and is not one command, and a Junction piece needs the authored library `adr/0014` calls content.* | |
| **Service** | Place schools, utilities, health, fire, waste | Guarantee they are reached, or staff them |
| | ⚠ *As built (amnesty item 10): **Attended only**. The payload is a Tile and a kind, and ***carries no catchment*** — `adr/0032` demoted coverage to an overlay.* | |
| **Govern** | Set taxes, service funding, transfers, and constraints; borrow. Globally by default, overridden per **Ward** *(was: per District — [`adr/0132`](adr/0132-the-district-is-derived-and-a-ward-is-what-the-player-draws.md), 2026-08-22)* | Directly set outcomes. Every Policy is a Rule with a named payer and named beneficiaries |
| **Demolish** | Remove a Street or a Building, at a price ([`adr/0091`](adr/0091-clearing-land-is-bought-rather-than-taken-and-demolish-is-the-sixth-verb.md)) | Yield anything for free, or decide what replaces it |
| | ⚠ *As built (milestone 17 task 4): **abandoned Buildings only**. A Building somebody still occupies is **refused by name**, with compulsory purchase written beside the refusal — `adr/0091` settles that the price is `f(land value at the Lot, what stands on it)` and deliberately declines to compose it, so the wide half of this verb is blocked on the land value target rather than on the verb. Abandoned stock is the half that needs no compensation term, because there is nobody left in it to compensate.* ⚠ *It is a **shortcut** and not the sink: a shell falls on its own after `[[building]] collapses_after_days` ([`adr/0172`](adr/0172-an-abandoned-shell-collapses-on-a-clock-because-a-bound-is-not-a-sink.md)), because a player is not a sink and a headless run has no player in it. What the verb buys is the Lot back sooner.* 🔴 ***Streets are still removed through `Connect`'s bulldoze flag**, which `adr/0091` says should have been superseded — retiring it re-spells six of the committed golden session's seven `connect` commands, so it is owed as a baseline change rather than done. Two spellings of remove-a-thing stand today and that is a debt, not the design.* | |
| **Inspect** | Overlays, Evidence, Pin | Change anything |

**The unit of a road edit is a Segment, not a Tile, and that is a property of the graph rather than of the interface.** `adr/0014` says Streets snap to the grid, which reads as though a Street were paintable Tile by Tile; the Road Graph puts nodes only at intersections a block apart, so one Street Segment spans an entire block face. A per-Tile command would either split Segments — which `CONTEXT` → Address refuses, because it is what holds the graph at ~30,000 rather than 150,000–300,000 — or accumulate dozens of commands into one edge and leave all but one of them meaning nothing. **What the player drags across the screen is a presentation question; what reaches the Input Log is one edge.**

**`Fund` and `Regulate` were merged into `Govern`.** They were never different acts — both are *"set a parameter on a Rule the city then obeys"* — and the split was drawn on subject matter (money versus law) rather than on what the player does. `adr/0025` then emptied `Regulate` further by moving density caps into zoning, leaving a two-item verb. Constraint-versus-flow is a distinction *inside* Govern, not a division of the verb set.

**One honest cost, recorded so nobody later "fixes" it:** the `PLAYER GOVERNS` pillar's own wording is that *"the player zones, connects, funds, and regulates"* — so in the pillar's sense, **all six verbs are governing**, and naming one of them Govern overlaps. That is accepted. Govern is the best available word for *the things you set*, and the pillar text should not be narrowed to match, because the pillar is about the relationship between player and city, not about one menu.

**`Service` is the design's acknowledged placement exception.** Pillar 3 is govern-don't-place, and a fire station appearing wherever the simulation likes is bad play — so the player places service Buildings, and only those. Note what the player still does *not* control: staffing is demand-determined by catchment, so the number of teachers is set by the number of children (see `adr/0026`).

**The player never places a Building that Citizens live or work in.** That is the line that separates this from a city *builder*, and it is what makes the city's growth an answer rather than an instruction. When a zoned Lot stays empty, that is information.

**Removing one is not placing one, which is why `Demolish` costs the pillar nothing.** The player still cannot author what stands anywhere — only veto what does, which is an ordinary governing act and is compulsory purchase with no road attached. The verb was a **sixth** rather than a mode of `Connect` because `Connect` had been carrying an unnamed demolition since [`adr/0077`](adr/0077-a-road-edit-is-one-segment-and-the-player-lays-streets-only.md) gave it a lay-or-bulldoze flag: keeping the count at five would have preserved a number rather than a distinction, and left one verb meaning *lay a road*, *unlay a road* and *remove a house*. That is a verb drawn on subject matter rather than on what the player does, which is the split this section already records collapsing once when `Fund` and `Regulate` became `Govern`.

**Clearing is bought rather than taken, and there are four routes to it** ([`adr/0091`](adr/0091-clearing-land-is-bought-rather-than-taken-and-demolish-is-the-sixth-verb.md)). They differ in scale and in who is displaced, and only the first two pay anybody:

| Route | Verb | Over what | Price |
|---|---|---|---|
| **Demolish** | `Demolish` | anything the player selects | market value, from the land value Layer, paid to the displaced |
| **Compulsory purchase** | `Connect` (Arterial), `Service` | whatever stands in the way of the act | the same price, charged as part of the act |
| **Clearance programme** | `Govern` | abandoned stock only | funded from the treasury; nobody left to compensate |
| **Withdraw replacement** | `Zone` | nothing standing | free, and it clears nothing in a healthy district |

**No act in this design yields empty ground, and that is what stops the priced routes being a cheap bulldozer.** Compulsory purchase under `Connect` leaves an Arterial; under `Service` it leaves a service Building with a footprint and an upkeep obligation. `Demolish` is the one act that produces a cleared Lot and it is the one that pays full market value for it.

**Withdrawing a permission removes *replacement*, never the Building.** Repainting does nothing immediate to what already stands — [`adr/0055`](adr/0055-a-zone-rules-permission-set-scopes-what-it-builds-never-which-lots-it-looks-at.md), and `02 §5.9` states it deliberately, because scoping a Zone Rule's population by permission would be immortality by paintbrush. So a churning district empties itself instead of rebuilding, and a healthy one is not cleared this way at any speed. It is a real lever and it is the slowest one.

**Inspect is a first-class verb, not a menu.** Roughly half the play time should be spent here, and the interface should be built as though that were true.

---

## 3. The first ten minutes

The opening must teach the causal chain, not the controls.

**The map starts empty.** The generator places a small number of **Outside Connections** on the map edges
([`adr/0088`](adr/0088-the-price-of-a-far-hinterland-is-paid-in-your-own-traffic.md)), each with a short
stub of road running inward so that a gate is something to connect *to* rather than a marker to guess at,
and **nothing else stands anywhere on the map**. No starting hamlet, no pre-laid grid, no unlock boundary:
the player may build anywhere from the first second and is responsible for every metre of it. That is the
genre's own opening — SC4's edge highway, rail and pylons; Cities: Skylines' single motorway ramp — and it
is what `PLAYER GOVERNS` amounts to when the player is also the one building.

**What makes the player start somewhere in particular is a consequence, not a fence.** An Outside
Connection is the only object on the map that does anything: it is where Citizens and Goods enter and the
only door Money comes through
([`adr/0024`](adr/0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md)). A city founded
fifty kilometres from one is a city nothing arrives at, and *why* is legible on inspection. Nothing forbids
it. This replaces the reason `adr/0088` originally gave, which rested on unlock-by-serviceability — a
boundary the design has since refused — and it is the stronger of the two, because a consequence teaches
and a fence only stops.

1. **Read the edges, and pick one.** Every Outside Connection is live from Tick 0 and each carries its own
   Hinterland's authored figures — price level, wage, depth, recovery rate, favoured Goods
   ([`adr/0023`](adr/0023-immigration-arrives-through-the-gate.md),
   [`adr/0026`](adr/0026-wages-are-posted-locally-and-never-cleared.md)) — so the map has several outsides
   and no inside, and they are not interchangeable. The panels are readable before anything is built, which
   makes the first verb used **`Inspect`** rather than `Connect`.
2. **A road inward from the gate you chose.** The map acquires an inside.
3. Zone a few Residential Lots. Nothing happens for a moment — then Households from the **Unplaced Pool** choose them, and buildings appear *because someone chose to live there*.
4. Zone Commercial. A shop opens, and its shelves are visibly empty until Goods arrive.
5. Click a Household. See where they live, where they work, what they need, and where their last Trip went.

Step 5 is the one that has to land. It is the moment the player learns that everything on screen is made
of people, and that the game will always answer *why*.

**On a forested seed there is a sixth step, and it is the first one without an obvious answer.**
[`adr/0022`](adr/0022-land-is-a-stock-the-city-spends.md) puts a decision in the opening that this section
did not have: *clear for lumber now, or keep the forest and import.* **No verb clears it and no terrain-editing tool
exists** — `Demolish` removes Streets and Buildings and never ground — because under `CONTEXT.md` → Zone
the player may zone anything anywhere and Woodland is ordinary ground with something standing on it. Build over it and the Timber is forfeited; zone
**Industry — Extraction** and it is harvested; do both in sequence and you pay for the harvest in Days the
block houses nobody, while the Unplaced Pool goes elsewhere.

It earns its place in the first ten minutes precisely because the other five steps each have one right
answer and this one does not — with the partial exception of step 1, which is a choice between edges
rather than a puzzle with a solution, and which the player makes before they know enough to be wrong. The trade is **speed against value**, and it reads differently to a city
short of Materials than to a city short of housing, with nothing in the game saying which you are.

**The city is founded with money and the player did not ask for it.** Money's only door is the Outside
Connection, so a city that exports nothing earns nothing and a founding balance of zero is a game that
cannot start. The balance is therefore granted automatically at world creation, and the opening says
nothing about where it came from. Whether it is a **gift** or a **loan to be serviced** is the natural axis
for a difficulty setting and is deliberately left open; making the player choose a borrowing figure before
they have laid a road would put a number in front of them before any of the numbers mean anything.

**It must be enough to get started and not enough to win.** Those are the two ends the figure is chosen
between, and both are observable rather than argued: a balance is too small if the player cannot reach a
first housed Household, and too large if a city grows to self-sufficiency without the player having made a
single spatial decision. It is world-creation state that enters the treasury Bin, so it is hash-bearing and
needs a named ratifier under [`adr/0052`](adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md);
the ratifier is the first real play session and the two refuting observations are the ones just named.
**It is not ratified by a player failing to meet §4's two-hour mark**, which under the previous section is
not a promise.

> ⚠ **That sentence was a `plans/0002` §D row for the life of this document and no ledger carried it — filed
> 2026-08-18** ([`adr/0116`](adr/0116-the-treasury-opens-empty-and-a-founding-balance-is-a-ratio-this-milestone-holds-neither-side-of.md)).
> ***A number that states its own ratifier inside a design document is still unratified, because the ledger is
> what schedules the ratification.*** **And it is not milestone 10's to choose**, though that is where the
> treasury Bin first exists: both ends above are denominated in **what things cost**, and there is no
> construction cost, no wage, no price surface and no gate — so a figure picked there would be a numerator with
> no denominator. Milestone 10's treasury opens **empty**, which is a different quantity that happens to share
> this one's range; only the missing consumer tells them apart.

**What is deliberately absent from the first ten minutes:** shocks, and any failure state.

**Budget pressure is not absent, and the earlier draft of this section was wrong to say so.** The founding
balance is finite and infrastructure is paid for three times — in Money, Materials and Land
([`adr/0035`](adr/0035-infrastructure-is-priced-by-what-it-consumes.md)) — so a player laying road on a
blank map is spending from the first act. What is absent is budget *failure*: the opening teaches the loop,
and *failure* arrives once the loop is understood.

> **⚠ AMENDED 2026-08-18 by [`adr/0116`](adr/0116-the-treasury-opens-empty-and-a-founding-balance-is-a-ratio-this-milestone-holds-neither-side-of.md).
> The superseded wording was:** *"…what is absent is budget failure, for the reason `CONTEXT.md` → Resource
> already gives: a deficit becomes a debt burden and never a stop. You can overspend in the first ten minutes
> and you will feel it; you cannot lose."* **Two things are wrong with it and they compound.** The sentence is
> under `CONTEXT.md` → **Money**, not → Resource; and it was quoted at **half its length**. In full it reads
> *"a deficit becomes a debt burden, never a stop — **but it is a player action, never an automatic overdraft,
> so the treasury genuinely empties and the Rules that could not draw simply wait.**"* The dropped clause is the
> one [`adr/0035`](adr/0035-infrastructure-is-priced-by-what-it-consumes.md) §3a wrote **specifically to correct
> that reading**, and [`adr/0024`](adr/0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md) carries
> the amendment: *"reads as an automatic overdraft, and an automatic one deletes a decision the player should be
> making."* So *you cannot lose* was the reading the corpus had already refused, and the correction landed in
> two documents and missed this one — `plans/0012` **Cause 2**. ***A caveat can be a clause of the sentence it
> qualifies, and a half-quote is the one form of miscitation that reads as a faithful one.***
>
> **What replaces it.** Borrowing is a lever the player **reaches for**, so a treasury that empties stays empty
> until they do, and the Rules that could not draw wait — which for infrastructure means capacity and free-flow
> speed fall and *an unpaid bill lengthens every commute* (§5). That is real pressure with no failure state
> behind it, and it is a better opening than the one this paragraph claimed, because the consequence is visible
> on the map rather than only in a number.
>
> ⚠ **And it does not cover the first ten minutes, which are made of *commands*.** `adr/0035` §3a specifies what
> happens when a **Rule** cannot draw; a command cannot wait, so an unaffordable `Connect` must be **refused**,
> and a refusal is the stop this paragraph says is absent. **No document states what an unaffordable player
> action does** — [`adr/0070`](adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md)'s *undesigned* class
> — and it is filed to [`plans/0002`](../plans/0002-open-questions.md) §C rather than answered here, because the
> likely shape is a pre-flight affordability query at the sim/render boundary (`05 §2`) rather than anything in
> the treasury. ***Specifying what happens when a Rule cannot pay is not specifying what happens when the player
> cannot.***

---

## 4. Two hours, and twenty

⚠ **Read this section under §1's *the hour markers are expectations, not a script*.** Every duration below
is a prediction the design must make plausible, not a curve it is entitled to enforce.

**Around two hours** the first genuine constraint bites, and it is a *spatial* one. The classic shape: housing and jobs have grown in different places, commutes lengthen, and Trips slide down the **Commute Budget**'s rungs. Businesses whose customers cannot reach them decline. What the player has to do is read the map.

**The commute degrades before it fails, and that is the whole of what this marker teaches.**
[`adr/0095`](adr/0095-a-commute-budget-is-three-rungs-and-only-the-last-one-refuses.md) grades a commute
**fast** to 20 clock minutes, **moderate** to 40 and **unsavoury** to 50, and only the ceiling refuses.
A single threshold would report **zero** while every commute in the city crept from twelve minutes to
nineteen, and then report a cliff — the signal arriving exactly when the geography has already gone
wrong and a spatial fix has stopped being cheap. The rungs make the intervening period the readable
thing. `LEGIBLE CAUSE`

⚠ **Built 2026-08-13, and the rungs report only one of §5.1's two scarcities.** `adr/0095` argued
entirely from **Separation** and did not notice that this document's own table makes **Congestion**
co-equal — nor that §7's **Gridlock** overlay, *the commute-time distribution's upper tail sliding toward
the Commute Budget wedge*, is the congestion reading. A walk Leg cannot carry a congestion term by
construction (`03 §3.7`: pedestrian networks do not saturate) and a vehicular Leg carries none either,
because [`adr/0075`](adr/0075-a-leg-is-a-plan-and-a-traveller-is-a-cursor.md) gives a Leg a cost and no
path. **So a city grades worse today when it *spreads* and never when it *fills up*, and these three
numbers are percentiles of a free-flow distribution.** `adr/0070` — stated rather than compensated for,
and reopened when 5c pays `adr/0041`'s volume debt. ⚠ **And no world this project can build occupies the
top rung**: the paved extent scales with √population, so a 10,000-Citizen city is 1.9 km across against a
ceiling that reaches 4.2 km, and *unsavoury* first occupies at **40,000**. The ladder is in the ADR.

This is also when the first Departures appear, and the distinction between **unhoused** (a capacity failure — build more) and **housed** (a quality failure — fix what you have) does real teaching work. The **unsavoury** rung is where the second kind comes from: a Citizen who has a home and a fifty-minute walk to work is the cleanest quality failure the design has.

⚠ **This is a Bill-axis scarcity, and §5.1's *buyable out of? Yes, always* is not contradicted by the
paragraph above.** The two sentences read as opposites and are about different things. What money cannot
do here is be *spent at the problem* — there is no import that shortens a distance, which is why the fix
is geography. What money can do is buy the **remedy**, and since
[`adr/0091`](adr/0091-clearing-land-is-bought-rather-than-taken-and-demolish-is-the-sixth-verb.md) there
are three priced ones: `Demolish` and rebuild nearer, an Arterial laid by compulsory purchase, or new
development zoned where the jobs are. So the axis is right and §5.1's leading clause — *"everything
physical is importable or buildable at a price"* — is carried by its **second** half. *(Neither section
could have said this when it was written; the third route did not exist.)*

**Around twenty hours** the city is large enough that the demographic engine becomes the main event. At
1× that is **about 562 Days** ([`adr/0094`](adr/0094-a-day-is-2048-ticks-because-ticks-per-day-is-a-sampling-rate-and-not-a-length-of-life.md)),
where under the old clock it was 140 — and 140 does not contain one generation at any plausible Life
Stage length, so this marker was scheduled to arrive after the campaign ended. ⚠ **It still constrains
[`adr/0011`](adr/0011-household-life-stages-and-self-generating-population.md) rather than being
satisfied by the clock alone**: three generations inside a campaign needs a Household life under about
190 Days, which is shorter than the arc that ADR describes. The number is not chosen here.

Two independent forces are running:

- **Sorting** — who chooses to arrive and leave
- **Life Stages** — the population the city generates for itself

And they pull against each other in the way the design is really about: **affordability drives internal generation, attractiveness drives immigration, and attractiveness raises prices.** A city can be dying of its own desirability, with every attractiveness indicator excellent and the **Replacement Rate** quietly below 2.0. Reading which of the two channels is failing is the deepest skill the game asks for.

By this point the player should be managing Districts rather than Lots, thinking in Arterials rather than Streets, and using overlays as their primary view.

---

## 5. Pressure, and the intensity dial

### 5.1 Two axes, not three layers

Every trajectory in §6 bottoms out in one of exactly two scarcities, and the difference between them is whether money can solve it.

| Axis | Scarce thing | Why it binds | Reads as | Buyable out of? |
|---|---|---|---|---|
| **The Bill** | Goods, Materials, Food, Land, road capacity | Everything physical is importable or buildable at a price. The Outside Connection never refuses. | Money draining — the treasury, the balance of payments, empty shelves, jammed Segments | **Yes, always.** The dead end is expensive, not closed |
| **The Clock** | People, and the skills they carry | A Hinterland recovers at a rate. A Life Stage takes Days. Tier 1 → 2 is an Event Wheel countdown; tier 2 → 3 needs schooling. | Vacancies unfilled, Retention falling, Replacement Rate under 2.0, arrivals skewing cheap | **No.** No amount of money exceeds a recovery rate |

This is the through-line stated generally: **Goods are price-constrained; people are rate-constrained.** An earlier draft split pressure into Economic, Logistics, and Shocks, which failed two ways — logistics failures resolve into money, so those two were one axis wearing two hats, and seven of §6's nine trajectories had no home at all.

**Two different scarcities read as *long commutes*, and both are Bill.** *Congestion* is road capacity and
is bought directly — more road, better road. *Separation* is distance, and no purchase is aimed at it;
what money buys is the **remedy**, which is `Demolish` and rebuild, an Arterial through compulsory
purchase, or development zoned where the jobs are. So *buyable out of? Yes, always* holds for both, and it
holds on this table's **second** clause — *"or buildable at a price"* — rather than its first. §4's *"the
fix is not more money — it is geography"* is about where the money has to be **pointed**, not about
whether it works, and the two sentences have read as a contradiction since both were written.

**The two axes are not independent, and [`adr/0035`](adr/0035-infrastructure-is-priced-by-what-it-consumes.md) is the first mechanism that couples them.** Infrastructure Upkeep is an automatic draw; borrowing is a player action rather than an automatic overdraft; so a treasury that empties leaves the maintenance Rule unable to draw, and unrenewed road life lowers capacity and free-flow speed. **An unpaid bill lengthens every commute.** The Bill becomes the Clock — not by a rule saying so, but because the thing money was buying was travel time all along. It is also what makes a fiscal crisis legible on the map rather than only in a number. `LEGIBLE CAUSE`

| Trajectory (§6) | Axis | |
|---|---|---|
| Insolvency | Bill | |
| Trade deficit | Bill | a *different* bill — the money supply, not the treasury |
| Gridlock | Bill | capacity you underbought; the fix is Materials and Land |
| Capacity failure | Bill | housing is construction; construction is Materials |
| Quality failure | **Both** | services are a bill; the Households leaving are a rate you cannot refill |
| Immiseration | **Both** | four of five exits are spatial; the fifth restores agency so the rest become reachable |
| Demographic stall | Clock | |
| Retention failure | Clock | |
| Labour mismatch | Clock | vacancies and unemployment together is the Clock's signature reading |

The two `Both` rows are the two the design cares most about, which is a sign the axes cut at a joint rather than sorting a list. Off-diagonal dial settings are therefore real games rather than multipliers: slack Bill with tight Clock is a rich city that cannot staff itself; tight Bill with slack Clock is a crowded city it cannot feed.

### 5.2 Shocks and disasters

Neither is a source of pressure. Both are **perturbations applied to the two axes** — a schedule for tensions that already exist. They are separated because they probe different properties.

| | **Shock** | **Disaster** |
|---|---|---|
| Where | the **Hinterland** — outside the map | the **mainland** — a footprint of Tiles |
| What moves | the authored figures: prices, wage, rent, population, composition | the world: Segments out, Buildings destroyed, Bins emptied, a Map Layer spiked |
| Onset | slow — drifts in and out over Days | sudden, with recovery over Days |
| Tests | **exposure** — how much of the economy runs through one edge | **redundancy** — whether an alternative exists |
| Can be *good* | **yes** — a boom edge, a migration wave | no |

**A shock is a movement in a Hinterland's authored figures, and nothing else.** That gives the layer one home, states every shock in domain units a player can read off a panel, and lets it propagate through chains the city already has — import price anchors Goods prices, the Hinterland wage anchors the wage surface, Hinterland attractiveness drives arrivals. Four edges drift independently, so shocks are spatially differentiated with no new machinery. Nothing here subtracts money directly.

**Disasters are not aimed at the player.** They simulate events outside human control, and the city's exposure to them is something the player authored by siting.

> **A disaster is the only instrument that can measure redundancy, because redundancy is invisible while nothing has failed.**

A city with one bridge to its industrial District and a city with three are identical on every overlay — same volumes, same commute times, same land values — until the bridge closes. No amount of `Inspect` finds that, because nothing is wrong yet. Same shape as §6's *notify what the player cannot be looking at*.

Three properties keep this a test rather than a tax:

**The city sets severity; the dial sets only frequency.** A disaster's initial footprint is small and fixed. What varies — by orders of magnitude — is how far it spreads before containment, and containment is an **ordinary Trip that can fail**. `Trip Fate` already enumerates *no route found* and *exceeded commute budget*, so a fire station behind a jammed Arterial loses a District and `Evidence` names the response Trips that overran. This is what §2's *"Service does not guarantee that they are reached"* has been describing with no mechanism behind it. No severity constant is authored anywhere; the only constants are a frequency interval and a spread rate, both **durations**, both scale-free.

**Every effect is an existing verb.** A Segment removed bumps the **Epoch** and cached routes revalidate lazily. Destroyed Buildings vacate **Lots**, which normal redevelopment reoccupies at Materials cost — so recovery time is the Bill axis reading the disaster back to the player. A fire spikes the pollution **Map Layer**. A proposed disaster effect that cannot be written in this vocabulary is a bolt-on.

**Hazard is terrain, precomputed, and visible from Tick zero.** Hazard regions are derived at world generation, never read from terrain during a Tick — so [`adr/0021`](adr/0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md) holds — and shown as an ordinary overlay. Cheap riverside land becomes a **decision with a posted price** rather than an ambush.

The catalogue, kept deliberately short:

| Disaster | Spreads via | Contained by | What it tests |
|---|---|---|---|
| **Flood** | precomputed floodplain, by depth | nothing; it recedes | **where you chose to build**, and whether Arterials cross it |
| **Urban fire** | adjacency; worse in stacked stock | fire service reachability | the road network |
| **Wildfire** | **Woodland — the fuel is the resource** | reachability, and how much Woodland remains | siting against the extraction frontier |

Wildfire needs no rule to become interesting: Woodland regrows on unsealed, unoccupied land, so a mature city **accumulates fuel on its own** as its frontier migrates outward. And a burn is a clearing minus the payout — it takes the Timber and leaves the fertile ground behind, exactly as a harvest would — so it is not purely a loss. `NO VERDICT`

Stacking is riskier than subdividing, also with no rule written: one Building destroyed displaces twenty Households instead of one. That hands [`adr/0025`](adr/0025-density-is-a-cap-and-it-trades-land-for-materials.md)'s subdivide-versus-stack decision a third axis alongside Access Points and logistics. **Severance** gains a sharper meaning too — an Arterial with no crossing can mean the fire crew has no route.

**Utility failure** is the obvious fourth entry and is **blocked** on the utilities network, which is undesigned. **Outbreak** — spreading over the Trip graph, the one mechanism where being well-connected is a liability — **earthquake**, and **wind** are in [`deferred.md`](deferred.md).

### 5.3 Disasters are world-scheduled

The footprint and timing are `f(seed, Tick)` over precomputed hazard regions, with **no reference to what is standing there**.

This is the only version where the hazard overlay tells the truth. A disaster that fired only where there was something to lose would make riverside land cheap-*until-you-use-it*, and the overlay would be describing a trap rather than a price. World-scheduling means a player who sites carefully genuinely never sees a flood do anything, which is the correct reward and which city-scheduling structurally cannot give. It also keeps the dial honest: a disaster that scaled with the player's success would be an internal difficulty modifier, which §5.5 forbids.

Two riders:

- **A grace period in Days**, because §3 requires no shocks in the first ten minutes. A Tick condition, not a state condition, so the schedule stays a pure function.
- **Uninteresting disasters still fire, and are still reported.** *"Riverside floodplain inundated — 0 Buildings affected"* is the game telling a player that a zoning decision made forty Days ago was correct. There is no other way to be told that. `LEGIBLE CAUSE`

### 5.4 What the dial actually scales

| Sub-dial | Scales | Authored in | What it changes about play |
|---|---|---|---|
| **The Bill** | each Hinterland's **price level**, and the rate it lends at | § per unit, % | how expensive it is to import your way out of a physical shortage |
| **The Clock** | each Hinterland's **depth** and **recovery rate** | Households, Households/Day | *when* the Extraction → Cultivation transition arrives — early and forced, or late and optional |
| **Acts of God** | the **frequency interval** for Flood and Fire | Days | how often the city is tested |

Two things follow. **The list introduces no new parameters** — every entry is a figure the Hinterland already carries under [`adr/0023`](adr/0023-immigration-arrives-through-the-gate.md) and [`adr/0026`](adr/0026-wages-are-posted-locally-and-never-cleared.md), plus one interval. Nothing is authored twice and nothing can drift out of sync with the model. And *"it scales parameters, never disables systems"* becomes **structural rather than aspirational**, because the dial has no reachable surface other than config the simulation reads anyway. No branch anywhere is written on intensity; two builds at opposite settings are the same binary reading a different Hinterland table.

An earlier draft named **tax tolerance** as an antagonist. There is no such scalar — tolerance is emergent, a Household comparing the city against a Hinterland with the same utility function everyone uses. It was a knob borrowed from the genre rather than from this design, as *"demand parameters"* was in the same paragraph.

### 5.5 Difficulty lives outside the map

> **The dial sets the terms of trade with the world outside. The difficulty inside the map is authored by the player, and the simulation only reports it.**

A tower behind a cul-de-sac strangles itself through Segment Stress and the Commute Budget at *every* dial setting. Nothing external is involved: the player made the geography, the geography made the failure. `PLAYER GOVERNS` `EMERGENCE`

Which sharpens what the dial is for:

> **The dial does not change the cost of a mistake. It changes the cost of recovering from one.**

The tower still strangles. What the dial decides is whether Materials can be imported through the shortfall while it is fixed (Bill), and whether the Households who left are replaceable (Clock). **Mistakes are made inside; the price of undoing them is quoted outside.**

The bound this accepts, recorded so nobody later relaxes it: the dial **cannot** make construction slower, services costlier to run, or decline steeper. Every one of those is a modifier on the city, and admitting one puts the constraint back to being policed by hand.

It also cannot be escaped. [`adr/0022`](adr/0022-land-is-a-stock-the-city-spends.md) makes a mature city a permanent net importer, [`adr/0024`](adr/0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md) makes the gate money's only source and sink, and `adr/0023` makes it the only way people arrive. There is no border-closing strategy that opts out.

⚠ ~~One consequence worth knowing when tuning: **the opening is nearly identical at every setting**, since §3 removes budget pressure and failure states from the first ten minutes and the Bill and Clock have nothing to act on until a mistake exists to price. Acts of God is therefore the dial's only early expression, and the one that makes a chosen setting feel chosen.~~ **Struck 2026-08-13: §3 removes budget *failure*, never budget *pressure*, and this paragraph was written against the earlier draft that confused them.** The balance is finite from the first second and infrastructure is paid for three times ([`adr/0035`](adr/0035-infrastructure-is-priced-by-what-it-consumes.md)), so the **Bill has something to act on from the first purchase** — a harsher price level is felt while laying the opening road, not two hours later. So the opening is *not* nearly identical at every setting, and Acts of God is not the dial's only early expression; it is only the most **visible** one, which is a different claim and the one worth keeping. Bill bites from the first minute and *binds* on §4's two-hour schedule; Clock on its twenty-hour one.

### 5.6 Modes, and what a lock is for

A **Mode** is a named preset over the dial, plus a lock policy. Not a separate concept.

| Mode | Bill / Clock / Acts | Dial | Why that lock policy |
|---|---|---|---|
| **Relaxed** | generous | freely adjustable | the setting is a comfort control |
| **Balanced** *(default)* | authored midpoint | freely adjustable | |
| **Challenging** | authored harsh point | set at creation, then fixed | commitment is the point |
| **Extreme** | authored near-max | not settable | the terms *are* the scenario |

**The lock is opted into, never imposed.** §2's *nothing is hidden or locked* is about the game withholding levers; a player choosing Extreme is choosing the constraint, and a player who does not want it does not pick that Mode. `NO VERDICT` holds — the game is not deciding anyone should be challenged.

**Mode is chosen at world creation and fixed for the world's life.** It is the design's only irreversible player-facing choice, and it is the price of the lock meaning anything. A downgrade path was considered and rejected: it earns nothing and lets a player quietly convert Extreme into Balanced at the first real setback, which is the exact failure the lock exists to prevent. The dial moves within whatever freedom the Mode grants; the Mode does not.

**Randomisation is orthogonal to Mode.** A separate toggle: the Mode sets a *range*, and randomise decides whether the player picks the point inside it or the seed does. Same mechanism [`adr/0027`](adr/0027-preference-is-drawn-per-household-and-persists-for-life.md) uses for Taste, where a Life Stage supplies a base and a range.

It costs nothing in legibility, because **a randomised dial is still fully readable**: its parameters *are* the figures on the Hinterland panel. A player with randomised edges does not face an unknown difficulty — they face four outside economies of different character, discovered by reading the world, which is the opening's real reconnaissance. Relaxed players should have that too.

**The corners of the cube are not all valid games.** Every Mode is a hand-validated point and the free sliders have floors. A Hinterland at minimum depth and minimum recovery produces no immigration at all, which breaks §3 outright — the opening depends on Households from the Unplaced Pool choosing zoned Lots. This is the second instance of a rule `adr/0021` already established for terrain: **a setting that produces no playable game is broken, not hard.**

### 5.7 Constraints on the dial

- **It scales parameters; it never disables systems.** Structural under §5.4 rather than a promise: there is no code path to disable.
- **It never touches an instrument.** Not detection, not notification thresholds, not `Evidence`. §6 derives its sustained-detection duration *from the mechanism* — the time abandonment contagion takes to reach neighbours — and a dial scaling that would return it to being somebody's guess. The tempting version is *hard mode warns you less*, which is difficulty-by-information-denial; the relaxed version is worse, since it hides problems from the player least equipped to find them. **The dial makes the city harder. It never makes the game less honest.**
  The separation is checkable rather than aspirational, via the **Input Log**: the dial is a simulation input and enters it; notification verbosity is presentation and does not. Two replays at different verbosity must produce identical State Hashes.
- **Shocks and disasters are seeded and deterministic.** Derived from the world seed and Tick, never wall-clock randomness, or replay breaks. See [`adr/0003`](adr/0003-deterministic-integer-simulation.md).
- **Every pressure has a legible cause.** The failure mode is a player who loses and does not know why. Each must be traceable through **Evidence** to the specific Buildings, Goods, or Households involved.
- **Prefer pressure that emerges over pressure that is scripted.** A Hinterland whose prices move and whose consequences propagate through the city's own chains is better than anything that subtracts money.

---

## 6. Failure, and what losing means

There is no game-over screen. There are **trajectories**, and the game's obligation is to make a bad one visible early enough to act on.

| Trajectory | Leading indicator | What it means |
|---|---|---|
| **Insolvency** | Debt service outrunning revenue | The classic. Recoverable, painful. |
| **Capacity failure** | Unplaced Pool growing, unhoused Departures rising | The city generates demand it cannot physically house |
| **Quality failure** | Housed Departures rising | The city houses people and then fails them |
| **Demographic stall** | Replacement Rate below 2.0, Childless share climbing | Too expensive to start a family |
| **Retention failure** | Spawned Households departing | Too expensive to stay in — you raised them and priced them out |
| **Gridlock** | The commute-time distribution's upper tail sliding toward the **Commute Budget wedge** | Trips are approaching failure across the board. Capacity you underbought — a Bill failure |

Three further trajectories arrived with the economy and labour work, and the third is the one with no counterpart above:

| Trajectory | Leading indicator | What it means |
|---|---|---|
| **Immiseration** | Destitute Departures rising, unemployment persisting | Neither capacity nor quality — the city **trapped** them. Five exits exist, only one of which is a transfer ([`adr/0024`](adr/0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md)) |
| **Labour mismatch** | Vacancies and unemployment coexisting; underemployment share climbing | Shows up as Departures, but the remedy is **transport**, not housing ([`adr/0026`](adr/0026-wages-are-posted-locally-and-never-cleared.md)) |
| **Trade deficit** | Imports exceeding exports; the money supply contracting | **Not insolvency.** Raising taxes does not fix it — that moves money inside a shrinking pool. The only remedies are fewer imports or more exports |

Note that four of the original six are demographic rather than financial. That is deliberate — a city sim whose only failure state is bankruptcy is a spreadsheet with roads.

**Every indicator in both tables is a state of the city, never a state of the simulator.** An earlier draft gave Gridlock the indicator *"Microscopic Segment budget exhausted"* — a software ceiling, triggered by a number in a config file, which fails the rule in §5 that an authored constant is acceptable only when it is the same thing the player is shown. It also let a resource limit borrow a diagnosis's authority. Reaching the **Microscopic Cap** means the simulation grows less precise where it was most needed; that is surfaced by `§7`'s existing requirement that an overlay mark a modelled number as modelled, and it is not an event. See [`03 §3.9`](03-agent-architecture.md).

The general rule, worth holding: **a trajectory names something happening to the city.** If an indicator would change when the simulation is optimised, it is not one.

### Failure is spatial as well as citywide

Every indicator above is a single number for the whole city, which was sufficient while the city was economically uniform. It no longer is, and it was made non-uniform deliberately: District-scoped Policy, abandonment contagion, and wage surfaces that differ by location. **A city can therefore score acceptably on every citywide indicator while containing a District in freefall** — aggregates hide exactly that, because it is what aggregates are for.

So **every trajectory must be expandable by place.** This is `Evidence` gaining a spatial axis rather than a new system: asking *"why is my tax base shrinking"* must decompose to a place, not stop at an average.

**The place is a Settlement, and then a District inside it — one hierarchy, not two axes** ([`adr/0092`](adr/0092-the-region-view-is-the-map-from-far-away-and-a-trajectory-names-the-place-it-is-reported-at.md)). The containment is free and structural: a Settlement *is* a maximal set of Districts mutually reachable inside the Commute Budget, so drilling from one to the other costs nothing and invents nothing. `02 §2.1` already designates a Settlement *"a reporting unit only"*, which is exactly what a panel needs and no more than a panel needs.

⚠ **This section originally said *by District* and that was decided when it could not have been decided correctly.** At 16.4 km the whole map is one Settlement — [`adr/0085`](adr/0085-nothing-on-this-map-is-far-away-so-a-settlement-is-made-by-a-gap.md) found S2 R1.5 had already measured it, one Settlement holding all 121 Districts at every Budget rung — so District was not chosen over Settlement, it was the only unit that existed. Under [`adr/0089`](adr/0089-the-map-is-sized-by-how-many-commutes-fit-across-it.md) there can be several, and the two units answer different questions: a District is authored, is where Goods pool without transport, and is a Policy's scope; a Settlement is derived and is a **commute shed**. Decomposing a labour failure by District can therefore read as healthy while a whole Settlement is starved of workers, which is this section's own hiding-in-aggregates failure one level up from where this section caught it.

**Each trajectory names the level it is *first* reported at**, because that level is a property of the mechanism producing it and not a preference:

| Reported first at | Trajectories | Why |
|---|---|---|
| **Settlement** | Gridlock, Labour mismatch, Retention failure | commute-shed facts. Each is produced by travel time, and a commute shed is what travel time makes |
| **District** | Insolvency, Trade deficit, Quality failure, Capacity failure, Demographic stall, Immiseration | policy and pooling facts. Each is produced by something a District bounds — a tax rate, a Goods pool, a service catchment |

Both remain reachable from either end; what the level decides is where the game looks *by default*, which is the only thing that decides whether a failure is found before it is expensive.

**No trajectory is terminal.** A District with no population, no land value, and full Sealing still has a recovery path, assembled from levers that exist for other reasons: remediation (pay to unseal), clearance of abandoned stock, a District tax override to zero, a service funding override upward, running transit in, and rezoning to a lower band so cheaper uses can bid.

⚠ **Two of those levers had no referent until [`adr/0091`](adr/0091-clearing-land-is-bought-rather-than-taken-and-demolish-is-the-sixth-verb.md), and this paragraph is where the gap was found.** *Clearance of abandoned stock* presumes abandoned Buildings **stand**, and `02 §5.9` said in one line that abandonment returns the Lot to vacant and in another that the condition is *retained on the Building* — the build implemented the first, so there was nothing to clear and abandonment contagion had no carrier. The shell stands, and the sustained-detection duration below — derived from *the time contagion takes to reach neighbours* — has a mechanism to be derived from again. **The lever is `Govern`**, exactly as the sentence after this list says: a District-scoped, treasury-funded clearance programme, not a click on a Building. The retail act is the `Demolish` verb (§2), which is a different tool with a different price. **The dead end is expensive, not closed** — consistent with scarcity being a gradient everywhere else in this design. None of these are unlocked or hidden; per §2 they are ordinary Policies whose preview simply reads *"applies to 0 Tiles"* until there is something to act on.

### Notification, and what earns one

**It should be straightforward for a player to diagnose a negative trajectory in their city.** That is the standard the whole information design is held to, and it is why **Evidence** is scheduled early in [`06-roadmap.md`](06-roadmap.md) rather than treated as polish.

*(An earlier draft said "in under a minute." That was dramatic rather than useful — unmeasurable, and it silently assumed the player already knew they were losing.)*

The aspiration is judged by humans in playtest. What is **checkable in a build** is the structural precondition beneath it: **no orphan figures.** Every displayed aggregate has a navigable path to its constituents, and every trajectory above reaches a named root cause through links the panels themselves provide. That is a test over the Evidence graph, it fails loudly when someone adds a figure without wiring it, and it is a constraint on the **simulation** rather than the interface — `CONTEXT.md` already states the principle: *if a figure cannot name its constituents, the simulation is computing it wrong.*

Note deliberately that the invariant checks **connectivity, not brevity**. A depth limit would reward collapsing chains — jumping from symptom to root reads as compliance while explaining less — so how *short* the chain should be is left to judgement, and only whether it *exists* is automated.

**Most causes need no history.** A trade deficit traces back through Materials imports to exhausted Woodland to Sealing — all of which are readable from present state, not from a log. Where history is genuinely needed, the fixed-size time series [`adr/0006`](adr/0006-no-collection-grows-with-elapsed-time.md) permits is sufficient, because Departures are already required to be counted **by reason**: the reasons survive even though the people do not.

But that test quietly assumes the player *knows* they are losing, and localised failure breaks the assumption — a player deep in one District can miss another declining entirely. Hence:

> **Notify what the player cannot be looking at.** Visible from anywhere — a boom, citywide construction — needs no notification. Visible only from *somewhere*, or from *nowhere at all*, earns one.

Notifications state an **event**, never a state, because an event is a fact and a state is a judgement: *"Eastfield: 34 Buildings abandoned this week"*, not *"Eastfield is failing."* Level-headed language, a feed rather than a modal, no recommendation attached, and clickable through to Evidence — **a camera hint, not a verdict.** `NO VERDICT`

**What earns one: a named trajectory becoming detectable in a place.** The table above *is* the trigger definition — every row is already a syndrome, a pattern of several indicators moving together, which is what makes it specific enough to notify on without a magnitude threshold. Three consequences: the feed is bounded by design (only these rows can fire), the trigger is documented in-game rather than buried in config, and there is one definition of "a problem" rather than a failure taxonomy and an alert system drifting apart.

Two detection paths, because they have complementary blind spots:

| Path | Catches | Behaviour |
|---|---|---|
| **Crossover** | healthy → declining | fires once, at the transition. Silent for a District that was never healthy |
| **Sustained correlation** | never healthy, so no transition exists | fires once the syndrome has persisted |
| **Pin**, generalised from Households to places and metrics | anything not named above | the player's own judgement, not ours |

Sustained detection introduces a **duration**, and durations are the acceptable kind of constant: scale-free, meaning the same thing in a village and a metropolis, where a magnitude threshold needs retuning as the city grows. Better, it can be *derived* — long enough that consequences are becoming hard to reverse, which for abandonment is the time contagion takes to reach neighbours. A number read off the mechanism rather than picked for the interface.

> **The general rule this settles: an authored constant is acceptable when it is the same thing the player is shown.** A threshold in a config file fails that test. A named failure mode in a table the player can read passes it.

The third row is the honest limit — the first two can only warn about failures we thought of.

---

## 7. Information design

### Overlays are a primary view, not a debug tool

The later SimCity games got this right and it is worth taking seriously: a map tinted by a single variable is the fastest diagnostic instrument in the genre. Expect overlays for traffic volume, land value, pollution (air, water, noise), utility coverage, service catchment, commute time, affordability, and Household composition.

Two rules:

- **An overlay must never be sharper than the player's ability to act on it.** This is why turning-movement diagnosis is deferred — see [`deferred.md`](deferred.md). Showing a problem with no corresponding verb is an invitation to frustration.
- **An overlay must never be sharper than the simulation underneath it.** Under [`adr/0007`](adr/0007-stress-driven-simulation-detail.md), congestion is exact where it is Microscopic and modelled where it is Statistical. Where the overlay is showing a modelled number, it should not pretend otherwise.

### Evidence is the spine

Every aggregate the game displays can be expanded into the specific entities behind it. Not "residential demand: 62%" but *"412 Households want to move in; 380 can't find anything under §900; 32 can't reach a job inside their Commute Budget"* — and each of those numbers opens into the actual Households.

This is a constraint on the simulation rather than a UI feature. **If a figure cannot name its constituents, the simulation is computing it wrong.**

### Pin, and the one family you follow

Players do not want to browse strangers; they want to follow *someone they were introduced to*. A Household met through Evidence can be **Pinned** and surfaced persistently thereafter, with a fixed-size ring of recent Trips.

Free-roam browsing of the population is explicitly not a mechanic — see [`deferred.md`](deferred.md) for why it is diagnostically worthless and what ships instead.

### Time is an arc, not a clock

There is no hour and no minute — see [`02-simulation-model.md` §1.2](02-simulation-model.md). Time of day is a **sun arc** with named phases: dawn, morning peak, midday, evening peak, night.

This is not decoration. A numeric clock makes a claim that can be checked against what the player is watching, and under any workable set of rates that claim is false — Cities: Skylines' calendar runs 112× faster than its own day/night cycle, which is why its players report cars taking "weeks" to cross town. An arc makes no numeric claim and so cannot be caught lying. Colossal Order reached the same place empirically and shipped a sun/moon arc rather than a clock.

**Commute Budget is drawn as a wedge on that same arc.** The budget and the day become one visual object, so there is no conversion between them to be dishonest about, and a failed Trip is a wedge that overran — shown against the day it overran in. `LEGIBLE CAUSE`

### Speed is where pacing lives, and Study is where truth lives

Four speeds and a pause. The simulation cannot observe which one is selected, so no speed changes any outcome; a longer Day at a slower speed buys the player real seconds to react, not a different game.

The default is **Normal**, not the slowest. The slowest — **Study** — is the speed at which rendered traffic is visually truthful, because apparent vehicle speed scales with the tick rate while the mechanics do not. Traffic looks true at exactly the speed where a player slows down to inspect it, which is the same principle as [`adr/0007`](adr/0007-stress-driven-simulation-detail.md) arriving on a different axis.

The concession, recorded rather than discovered later: a player who never touches the speed control sees traffic running roughly twice as fast as its apparent size warrants, forever.

### The rendering promise

**Every visible agent is a promise you have to keep.** Willmott's observation about GlassBox is the warning: statistical simulations let players rationalise random or buggy behaviour as intelligence, and closing the visualisation gap removes that grace. If a behaviour cannot be afforded at full fidelity, it must not be drawn individually.

---

## 8. Open questions

1. **Is transit ever implemented?** [`adr/0008`](adr/0008-walking-is-a-simulated-leg.md) removed the irreversibility — a bus is a Leg type inserted into machinery that already handles Legs — so this is now a scope question rather than an architectural one. It remains the largest single unbuilt system and it interacts with 2, 3, and 4 below.
2. **Is car ownership a choice?** ⚠ **Half-closed 2026-08-14 by [`adr/0098`](adr/0098-a-citizen-travels-in-their-households-mode-and-mode-choice-is-undesigned-rather-than-unbuilt.md): the exogenous half is *built*, and the endogenous half stays open on this entry's own terms.** A `[households] car_ownership_percent` decides what share of Households keeps a car; a Citizen of one that does drives everywhere and one of a Household that does not walks everywhere. **This entry chose the shape, and the choice was load-bearing rather than incidental** — ownership sits on the *Household* because this entry says so, and a per-Trip mode share would have given each Citizen a different route on alternate mornings, which would have made every route-cache measurement in milestone 5c meaningless. ⚠ **It also exposed something this entry could not have known**: *whether a Citizen weighs a walk against a drive on the day* is a **different question** from this one, it is settled by no ADR anywhere, and it appears in **neither** of `06`'s two inventories — so it is `adr/0070`'s **undesigned** class, invisible to the only instrument that would have caught it. ⚠ **Read again 2026-08-18 by [`adr/0119`](adr/0119-a-parking-space-is-held-by-the-citizen-and-a-household-holds-as-many-cars-as-it-has-drivers.md), which needed this entry to say something it does not say.** Milestone 7 asked what object holds a parking space, and the **Household** is the obvious answer — it owns the car, it outlives both journeys of a commute, and it is what *"a household's car sits at home overnight"* is grammatically about. **It cannot hold one, because this entry never said *one* car.** `adr/0098` implements the simple assumption below as a per-Household **boolean**, and `World.ModeOf` therefore drives **every member** of an owning Household — so a Household of three workers puts three cars at three destinations, and one location column would overwrite two of them. The holder is the **`Citizen`**. ***The simple assumption named below is about who owns, and this entry was quoted for a claim about how many*** — `plans/0012` **Cause 5**, on a sentence rather than on a number. `adr/0098`'s own revisit trigger already names the fleet-size artefact; what nothing named is that a *second* decision rested on the same fact. **This entry's open half is unchanged**: whether ownership responds to walkability is still open, and `adr/0119` is written so that a `VehicleTable` closing it costs **one column** — the holder moves from the Citizen to the Vehicle and the conservation sum's left-hand side never moves.

*Original text follows.* Every Household owning a car is the simple assumption. Making ownership respond to walkability and transit access closes the loop, letting parking pressure feed back into whether people drive at all. Only becomes interesting once transit exists.
3. ~~**Open map or progressive land unlock?**~~ **Closed 2026-08-12: the map is open, and unlock is refused** ([`adr/0090`](adr/0090-the-generator-makes-land-and-the-player-makes-every-road.md)). The generator makes terrain, Woodland, hazard regions and a few Outside Connections with road stubs; the player lays every Segment after that, anywhere, from the first second. **The damper argument was given up rather than answered**, and that is recorded because it was the strongest ground: withholding ground is difficulty by fence, which §5.5 forbids by name — *"the difficulty inside the map is authored by the player"* — and the dampers it would have supplied exist already and are causal, since [`adr/0022`](adr/0022-land-is-a-stock-the-city-spends.md) makes Materials imports a growth brake and [`adr/0089`](adr/0089-the-map-is-sized-by-how-many-commutes-fit-across-it.md) restores distance as a cost. The density-versus-family choice it was reaching for is [`adr/0025`](adr/0025-density-is-a-cap-and-it-trades-land-for-materials.md)'s subdivide-versus-stack trade, which is real without compulsion. *The serviceability recommendation is kept below the strike rather than deleted, because a recommendation deleted reads as one never made:* ~~If unlock, the gate should be **serviceability** — road network reaching the border, utilities with headroom — rather than a population or money threshold, so it stays a condition read off the map rather than a number in a config file.~~
4. **What does the education system actually look like?** Under [`adr/0010`](adr/0010-one-clock-and-demographics-by-sorting.md) schools work by **Sorting** — good schools attract already-educated Households — while under [`adr/0011`](adr/0011-household-life-stages-and-self-generating-population.md) school capacity is a **flow** serving Households passing through Family and Mature Family stages. Both are true simultaneously and the interface has to convey a *rate* rather than a count, which is harder.
5. ~~**How is the intensity dial surfaced?**~~ **Closed in §5.6.** A **Mode** is a named preset plus a lock policy, with the three sub-dials — Bill, Clock, Acts of God — exposed underneath to whatever extent the Mode permits. Randomisation is an orthogonal toggle drawing within the Mode's range.
