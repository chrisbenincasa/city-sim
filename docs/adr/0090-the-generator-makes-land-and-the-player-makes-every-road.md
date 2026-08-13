# The generator makes land and the player makes every road

**World generation produces terrain, Woodland, and a small number of Outside Connections each with a short road stub running inward. Nothing else stands anywhere, and the player lays every Segment after that.** The map is **open** from the first second: there is no progressive land unlock, no serviceability gate and no boundary of any kind. `01 §8` question 3 closes as **refused**, and the [`0011`](0011-household-life-stages-and-self-generating-population.md) damper argument that was its strongest ground is **given up rather than answered**.

Guiding concepts: `PLAYER GOVERNS`, `EMERGENCE`, `NO VERDICT`, `SOLVE THE ACTUAL PROBLEM`.

**Arguable** under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md). Whether the player may build anywhere is a permission, and no measurement settles a permission — but the ground it is refused on is partly measured, since [`0089`](0089-the-map-is-sized-by-how-many-commutes-fit-across-it.md) is what makes the refusal survivable.

## Why

### The open map is the decision `adr/0089` was actually about

`adr/0089` sized the map by **how many Commute Budgets fit across it**, on the ground that the ratio decides *"whether the player can build separate towns or only one blob"* — and that this is the **player's** decision. A progressive unlock takes that decision back and gives it to a schedule. The two cannot both stand: an unlock boundary is a rule about where the city may be, and `adr/0089`'s whole argument is that where the city is should be an answer.

`plans/0025` decision 2 states the same thing from the opening's side: **what makes the player start somewhere is a consequence, not a fence.** An Outside Connection is the only object that does anything, so founding fifty kilometres away is permitted and simply does not work, legibly. That is a teaching mechanism. A boundary is not — *a consequence teaches and a fence only stops*, and having chosen the consequence for the opening it would be incoherent to reintroduce the fence for the middle game.

### The damper argument is given up, not answered

`01 §8` Q3 does not rest on pacing. Its real ground is structural: progressive unlock is *"a **physical** damper on the population feedback loop in `adr/0011`, forcing a choice between density and family formation"*. That is a serious argument and it is abandoned deliberately, for three reasons, each of which would be sufficient.

**It is difficulty by fence, which `01 §5.5` forbids by name.** *"The difficulty inside the map is authored by the player, and the simulation only reports it"*, and the dial *"cannot make construction slower, services costlier to run, or decline steeper"* — every one of those being a modifier on the city. Withholding ground is the same class of move: it does not change what the city does, it changes what the player is allowed to do, and it must then be policed by hand.

**The dampers it would supply already exist and are causal.** [`0022`](0022-land-is-a-stock-the-city-spends.md) makes Materials imports *"a growth brake nobody designed"* — building gets steadily dearer as a direct function of how much has already been built — and `adr/0089` restores distance as a real cost after the map spent a milestone at 0.9 Commute Budgets across. Both damp by making growth **expensive**, which is `01 §5.1`'s Bill axis and *"the dead end is expensive, not closed"*. An unlock damps by making growth **impossible**, which is the one shape this design refuses everywhere else.

**The density-versus-family choice it was reaching for is not gone.** It is `adr/0025`'s subdivide-versus-stack decision, which is already a real trade over Access Points, logistics and — since `01 §5.2` — disaster exposure. What unlock added was compulsion, and compulsion is what `NO VERDICT` costs.

### The generator makes land, and this is what the build already does

The player half of this is not new work. **`RoadGenerator` has exactly one production call site — `SyntheticCity`, reached only by `CommandKind.Populate`** — which `Command.cs` describes as *"a verb no player has"* and *"expected to be deleted when the player can grow a city instead of declaring one"*. A world created without that command has **no roads at all**. What this decision adds is the **stub**: a gate with a short length of road running inward, so that an Outside Connection is something to connect *to* rather than a marker to guess at.

The generator's remit is therefore stated positively and closed: **terrain, Woodland, hazard regions, and the Outside Connections with their stubs.** Woodland is `adr/0022`'s one generated resource; hazard is `01 §5.2`'s precomputed, visible-from-Tick-zero terrain. Nothing else. In particular the generator places **no Buildings, no Lots and no Streets**, so `02 §2.2`'s subdivider only ever runs on ground the player has fronted.

### ⚠ `adr/0089`'s stated blocker is false as written, and the two questions were welded

`adr/0089` is gated on `RoadGenerator` *"paving the entire map at world creation"* — 525,312 Segments and 2,626,560 Lots at `WorldCells = 512` — and routes the repair to `plans/0002` ledger #2, *open map or progressive unlock*, with the instruction that it *"must not be answered by capping the generator"*.

**It does not pave at world creation.** It paves on `Populate`. `CellGrid.cs`'s own comment says *"at world creation"*, and that sentence is where the claim came from — the third time in three days a decision has been taken from a sentence about the code rather than from the code, after [`0064`](0064-a-bins-capacity-is-a-property-of-the-ruleset-in-force-and-an-over-full-bin-drains-rather-than-clamps.md)'s loader and [`0079`](0079-a-building-outlives-its-frontage-and-an-address-that-has-none-is-a-hole-the-trip-model-reports.md)'s pressure source. ***Citing a mechanism is not checking what it is keyed on***, and this time the mechanism was keyed on a **command**.

So the two questions were welded and come apart cleanly:

| Question | Kind | Answer |
|---|---|---|
| May the player build anywhere from the first second? | **design**, ledger #2 | **Yes.** This ADR, and nothing about the generator bears on it |
| Should a *synthetic* city pave 4,295 km² to declare a city occupying 270 km²? | **mechanical**, and it was never a design question | **No**, and capping it is correct rather than forbidden — `SyntheticCity` should pave what it populates |

`adr/0089`'s instruction *not* to answer it by capping the generator was right about the design question and wrong about which question the cap belonged to. **The flip to 512 is therefore gated on a bounded piece of work in a spike verb, not on a decision about the player's freedom** — and a player's world at 512 allocates no Segments and no Lots at all, because nothing has been built yet, which is `adr/0021` behaving exactly as written.

## Consequences

**`01 §8` question 3 is struck and answered**, with the `adr/0011` damper argument recorded as given up and the two replacements named, so a later reader does not reopen it on the ground that the strongest argument went unaddressed.

**`plans/0002` ledger #2 closes as *refused*** — the second entry ever to leave that ledger by a decision rather than a measurement — and its recommendation of unlock-by-serviceability is kept beside the refusal, because a recommendation deleted reads as one never made.

**`adr/0089`'s gate is re-described rather than discharged.** The flip still waits; what it waits on is scoping `SyntheticCity`'s paving to the area it populates. `plans/0003`'s hash-moving-queue item 6 already says *scope `RoadGenerator` to developed land* — the instinct was right and the reason attached to it was a design question that turns out not to bear on it.

**`CellGrid.cs`'s comment is wrong and is a defect of the class `adr/0073` routes**: a sentence in code that a design decision was taken from. It is corrected where it lives rather than annotated here.

**The stub is generated state and is therefore hash-bearing**, and its length is a world-creation number this ADR does not choose. It is bounded from both ends by what it is for — long enough that the gate is visibly a road and not a dot, short enough that it decides nothing about where the player builds — so it enters `plans/0002` §D2 unset, with the first play session as its ratifier. **How many Outside Connections** is the same kind of number and the same row; `01 §5.4` already assumes **four edges drifting independently**, which is a floor rather than a value.

**Nothing is unlocked, so nothing needs an unlock indicator, a preview or a boundary renderer.** That is the deletion this decision buys, and it is larger than it looks: an unlock rule would have needed a serviceability predicate, a boundary representation in the save, and a whole class of *why can I not build here* explanation that `LEGIBLE CAUSE` would have obliged.

## What would trigger revisiting

**A 65.5 km map proving unnavigable rather than merely large.** [`0092`](0092-the-region-view-is-the-map-from-far-away-and-a-trajectory-names-the-place-it-is-reported-at.md) is the answer to overview at this size, and it is unbuilt. If the region view ships and players still cannot find their way around an open map, the failure is in the view and the fence would be treating a symptom — but a second failure after that repair is real evidence, and it is the strongest case unlock will ever have.

**The population feedback loop running away without the fence.** `adr/0011`'s loop is the thing unlock was proposed to damp. If a long run shows population compounding past what Materials cost and distance restrain, the damper argument was right and was given up prematurely. **That is measurable, its machine is the first long run with `adr/0011`'s Life Stages built, and no session may close it.**

**Founding capital turning out to underwrite a bad opening.** An open map plus a granted balance means a player can lay road fifty kilometres from a gate and discover the consequence slowly. `plans/0025` decision 3 calls the balance *enough to get started and not enough to win*; if playtest shows the honest failure taking longer to read than a player will sit through, the repair is the **Evidence** that explains it, and only after that the balance.
