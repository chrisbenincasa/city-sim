# An abandoned shell collapses on a clock, because a bound is not a sink

**A `[[building]]` kind states `collapses_after_days` beside `condemn_after_days`, and an abandoned Building's Lot returns to vacant that many Days after it was abandoned, with no player act involved. The key is required wherever `condemn_after_days` is stated and refused everywhere else.** This **supersedes the sink sentence** in [`0091`](0091-clearing-land-is-bought-rather-than-taken-and-demolish-is-the-sixth-verb.md) — *"Abandoned stock is bounded by the Lot count, not by elapsed time"* — which is **true, and insufficient**, and it is superseded by measurement rather than by argument.

Guiding concepts: `NO VERDICT`, `SOLVE THE ACTUAL PROBLEM`, `HONEST DEGRADATION`.

⚠ **The *shape* is arguable and the thing it turns on was measurable, so it was measured first** ([`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)). The value of the number is neither: it is hash-bearing, unratified, and lives in [`plans/0002`](../../plans/0002-open-questions.md) §D1 with its ratifier named.

---

## Why

### The measurement, because the reading this replaces is refutable and was never run

[`0091`](0091-clearing-land-is-bought-rather-than-taken-and-demolish-is-the-sixth-verb.md) concluded that standing abandoned stock needs no sink of its own: Lots are finite and fixed by the Road Graph, so shells are bounded by the Lot count and cannot grow with *elapsed time*, which is what [`0006`](0006-no-collection-grows-with-elapsed-time.md) asks. [`plans/0046`](../../plans/0046-decline-demolish-and-cleared-land.md) decision **1** typed that claim **measurable** and said the first move was a run rather than a sitting. It is the run.

`rulesets/declining.toml`, 10,000 Citizens, one seed, `--zones` census, `--no-decide-guard`. The city opens at **1,201 built / 0 abandoned / 173 vacant** over **1,374** Lots. The only difference between the columns is `collapses_after_days`: **1** on the left, and on the right a value so large the shell never falls within the run.

| Ticks | collapse after 1 Day | collapse never |
|---|---|---|
| 8,192 | 254 / 717 / 403 | 169 / 1,035 / 170 |
| 32,768 | 594 / 407 / 373 | **0 / 1,204 / 170** |
| 65,536 | 602 / 385 / 387 | **0 / 1,204 / 170** |

*built / abandoned / vacant.*

🔴 **With no collapse the city is dead by Tick 32,768 and stays dead.** Every one of the 1,204 buildable Lots holds a shell nobody lives in, nothing stands that anybody does, and the 170 Lots still vacant were unbuildable from Tick 0 — the count does not move between 32,768 and 65,536 because there is nothing left to move. With collapse the same city on the same seed converges to **602 / 385 / 387** and holds it.

### `0006` was satisfied the whole way down, and that is the finding

The dead column passes [`0006`](0006-no-collection-grows-with-elapsed-time.md). The shell count is **bounded** — 1,204, exactly the Lot count `0091` named — it stops growing, and a long-run assertion watching for an upward trend at steady state would find a flat line and report health. ***The collection is bounded, monotone and terminal.***

⚠ **So `0006`'s test has a blind spot worth naming: a collection that fills a finite space and never empties passes it.** What the design wants from an occupied Lot is not boundedness but **turnover**, and the two come apart exactly when the bound is reached. `0006` is not wrong and is not amended here; it is narrower than it reads, and this ADR is the sighting. ***A bound answers "does it grow for ever"; it does not answer "can the city come back".***

### A player is not a sink, and the acceptance run has no player in it

`0091` names two clearing routes and **both are player acts** — the `Demolish` verb and the `Govern` clearance programme. *The player will clear them* is a hope about a human rather than a property of the simulation, which is the objection `plans/0046` decision **1** opened with.

It is worse than a hope, and the reason is procedural rather than philosophical: **`CLAUDE.md`'s Definition of done requires a 100k-Tick headless balance run**, and a headless run has no player in it at all. On the superseded reading, this milestone's own acceptance test would have been taken on the right-hand column above — a city that is 100% derelict for four fifths of the run, passing every invariant, with `0006` green.

### `NO VERDICT` is the pillar this breaks, and it breaks it completely

*A mechanism must admit more than one outcome. If a system can only ever produce one lesson, it is an argument wearing a simulation's clothes.* A shell that stands until a human removes it makes decline **irreversible by the city**: every Building that fails takes its Lot out of the game permanently, so the only trajectory an unattended city has is downward, and the only question is how fast. That is one lesson, and the measurement above is what it looks like — 1,201 to zero, monotone, with no path back.

⚠ **The contagion term (`02 §5.9`, this milestone's task 7) makes it strictly worse rather than incidentally so**: a permanent shell raises its neighbours' Failure Pressure for ever, so the terminal state is not merely reached, it is *attracting*.

### Why a duration, and what the alternatives were

- **A duration, on the kind.** Chosen. It needs no mechanism that does not exist, it is authored where the failure rate it pairs with is authored, and it makes the shell's extent **observable** — which is the property [`0091`](0091-clearing-land-is-bought-rather-than-taken-and-demolish-is-the-sixth-verb.md) required a shell to have in the first place, since a shell that vanishes on the sweep that finds it has no extent at all.
- **Redevelopment when land value falls far enough that it pencils** (`02 §5.5`, and `0091`'s own preferred sink). 🔴 **Unbuildable here.** It reads the land value Map Layer, whose target is a named hole in `MapLayers`, and under [`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) the answer to an unbuilt mechanism is *build it* — but ***a milestone whose named risk is created by its own first task must ship its sink in the same breath***, and this one cannot ship a Map Layer target. The duration is therefore a **floor** rather than a rival: when redevelopment exists it can pre-empt the clock, and the clock stays as the case where nobody wants the land.
- **Let the Zone Rule overwrite a shell.** Refused for two reasons. It would make the shell's lifetime a function of demand, so a stagnant district keeps its shells for ever and the measurement above returns by a different road; and it deletes the vacancy step, so [`0069`](0069-placement-is-a-mechanism-of-its-own-and-construction-houses-nobody.md)'s *build on a vacant Lot* would need a second path with a second set of conditions.
- **A cap on standing shells, oldest evicted.** Refused: it is a collection with a lid rather than a sink, the eviction order is a hash-bearing rule nobody could see, and it puts a work bound where a design quantity belongs — the shape [`0134`](0134-a-district-is-a-centre-and-its-basin-so-the-count-follows-centres-and-not-a-ceiling.md)'s `migrate_cells` is careful not to be.

### It is designer-facing, which is what `0164` asks before a number becomes a key

*Would a designer ever set this?* Yes, and the pairing is the whole answer: read against `condemn_after_days`'s failure rate, ***what `collapses_after_days` dials is what share of the city is derelict at steady state*** — 39% at 2 Days and 1 Day on `declining.toml`. That is a felt quantity a designer tunes by looking at the city, so it belongs in the Ruleset under [`0164`](0164-a-ruleset-key-is-designer-facing-or-it-belongs-in-the-instrument.md) rather than in the instrument.

⚠ **It is a *duration* and not a count of anything**, for [`0168`](0168-a-decline-threshold-is-a-duration-and-the-premises-and-the-tenant-get-one-each.md)'s reason arriving at the third key in a row after [`0059`](0059-a-zone-rules-sample-is-a-revisit-period-so-the-ruleset-states-a-duration.md) and [`0130`](0130-the-pools-bound-is-a-duration-and-the-unhoused-channel-ships-with-the-gate.md): author the duration, derive the count.

### Required beside `condemn_after_days` and refused elsewhere

[`0130`](0130-the-pools-bound-is-a-duration-and-the-unhoused-channel-ships-with-the-gate.md)'s disposition for `gives_up_after_days`, for the identical reason: **a kind that can be abandoned and cannot collapse is a collection with an inflow and no sink**, which is `0006`, and the measurement above is what such a file produces. Stating it without `condemn_after_days` is refused too — nothing would ever create the shell it bounds, so the key would be inert and would read as though the world had blight in it.

### Zero means never, it is unauthorable, and the division is deliberate

`ZoneRuleEngine` treats `CollapsesAfterDays == 0` as *never collapses*; the loader refuses a non-positive value in any file. That is [`0048`](0048-the-ruleset-is-validated-where-it-is-parsed-and-only-integers-and-strings-cross-into-the-core.md)'s division — the parse site refuses what a designer must not author, and the engine stays defined for everything it can be handed — and it is what lets every test fixture predating this milestone keep the old behaviour without a migration.

🔴 ⚠ **The cost of that convenience is stated here rather than left in a comment: a Ruleset built in C# gets the reading this ADR refuses, silently.** A fixture is short enough that it never shows, and the counterfactual column above is what it would look like if one were not. **Nothing built in code should be run long and read as a city.**

## Consequences

**`0091`'s sink sentence is superseded and its two clearing routes are not.** They stop being *the* sink and become the player's **fast** path over the city's slow one — which is the relationship `01 §6` already describes for clearance as a recovery lever, and it is a better one: a player clearing a shell is choosing to spend to get a Lot back sooner, rather than performing maintenance the simulation cannot perform for itself.

**The number is hash-bearing, unratified, and already ledgered** — [`plans/0002`](../../plans/0002-open-questions.md) §D1, *The collapse duration for abandoned stock*, whose named ratifier is **the first play session in which somebody watches a district decline**, shared with `condemn_after_days` and ratifying **the pair or neither**. ⚠ **Nothing in this ADR ratifies it.** What was settled here is that a shell must fall without a player; how long it stands is a separate question with a separate ratifier, and no headless run can take it.

**`rulesets/declining.toml` is the demonstration**, and `rulesets/diagnosed.toml` must carry the same values because a test compares the two column for column.

**The long acceptance run owes a second assertion, and the measurement above is why.** Counting shells and checking they do not trend upward is exactly what the dead column passes. ***It must also assert that the built count does not trend to zero*** — turnover rather than boundedness — and that is a new obligation on this milestone's task 9 rather than a restatement of `0006`.

**`02 §5.9` gains the sink it never named.** It says a Building is abandoned past a further threshold and says nothing about what removes it, which is the gap `0091` filled with the Lot count and this fills with a clock.

## What would trigger revisiting

- **Redevelopment shipping** (`02 §5.5`), which needs the land value Map Layer's target. The clock would then be the fallback under a land-value condition rather than the only route, and the question becomes which pre-empts which.
- **The contagion term landing** (this milestone's task 7). It makes the shell's standing time felt in a second way — how far the blight spreads before it lifts — so the number acquires a second reading and may want a different value.
- **A world in which decline is not certain.** Every shipped world is at one pole ([`plans/0002`](../../plans/0002-open-questions.md) §A), so today the collapse duration sets a cycle time on a death sentence. When a Building can recover, the steady-state share becomes a property of the design rather than of the stopwatch, and the pairing argued above is worth re-measuring.
- **A `Ruleset` becoming constructible only through the loader.** The zero-means-never escape exists for fixtures built in code; if that route closed, the engine's default should become the refusal rather than the old behaviour.
- **Evidence that the clock is visible as a clock.** The mechanism is defensible because it reads as a building falling down; if shells across a district collapse in lockstep and a player sees a timer rather than a city, the trigger should become a condition with a jittered term and not a shared duration.
