# Terrain is five types and Base Fertility varies across them, because a category exclusion is not an overlay

**Terrain ships as five types — `ordinary`, `rock`, `floodplain`, `marsh`, `thin_soil` — stored one per
Cell, `(saved AND hashed)`.** **Base Fertility varies across them**: `1.0`, `0.2`, `1.0`, `0.5`, `0.6`.
🔴 **That amends [`0022`](0022-land-is-a-stock-the-city-spends.md) rather than tuning within it**, and
the amendment is deliberate: that ADR refuses *"fertile valleys here, poor ground there"* on the ground
that it makes farm siting **a lookup**, and five ranked values are a lookup. **The refusal's argument is
kept and its scope is narrowed** — what it protects is that ***the interesting fact is the one the player
makes***, and Sealing plus pollution still supply that, unchanged and unbounded, against a ceiling the
generator sets.

⚠ **The set is five and the enumeration is the decision** — [`0154`](0154-base-fertility-is-ruleset-data-keyed-by-terrain-type-and-the-old-name-invented-a-field.md)
made Base Fertility *"Ruleset data keyed by terrain type"* and presumed *"a small enumeration"* without
naming one member. **Nothing in the corpus enumerated terrain**, which decomposition found on the day
task 2 tried to write the column.

Guiding concepts: `SOLVE THE ACTUAL PROBLEM`, `HONEST DEGRADATION`.

## Why

**`0022`'s refusal is aimed at one thing and it says so in its own sentence:**

> *"The obvious design puts fertility in the generator — **fertile valleys here, poor ground there**. It
> is rejected because it makes the interesting fact a **lookup**. A player reading the fertility overlay
> is **following instructions**, and the map has a correct answer to where farms go before the player has
> done anything."*

**The target is a ranking among farmable land.** ⚠ **It is not a claim that ground has no physical
character**, and that ADR proves it in its own next paragraph — *"rock and clay may never recover,
alluvial floodplain may recover over hundreds of Days"*. **It already varies ground by type.** What it
does is route that character through the **decay rate** rather than through the ceiling, and the choice
between those two routes was never argued; the rate was simply the one the formula had a key for.

🔴 **So the honest reading is that `0022` refused a gradient and never considered an exclusion.** *You
cannot grow crops on rock* is the same class of fact as *you cannot build on water*: it removes a
category rather than ranking the options inside one. **That much is compatible with `0022` as written.**

⚠ **And five ranked values are not that**, which is the half this decision does not get for free. `0.2`,
`0.5`, `0.6` and two at `1.0` **are** an overlay a player can read and follow. ***Calling it realism does
not stop it being a lookup.*** **It is taken anyway, and the reason is stated rather than dodged:**

- **The refusal's own justification is comparative, not absolute.** *"Making the interesting fact a
  lookup"* presumes the lookup is **the** fact. It is not: Fertility is `base − w_s·Sealing −
  w_p·pollution`, and only the first term is dealt. Sealing runs 0–1024 against a ceiling of `1.0`, so
  ***what the player does to a Cell moves its Fertility across the whole range the generator's five values
  sit inside***. The generator sets where you start; play sets where you end.
- **A world with no bad ground has no siting decision at all**, which is the failure `0022` was avoiding
  from the other side. If every Cell farms identically until the player builds, the first farm goes
  anywhere and the *"agriculture and housing repel each other"* dynamic has nothing to push against on
  Tick 0.
- **`0022`'s endgame argument is untouched.** *"A mature city is a net importer of Food and Materials"*
  rests on Sealing's ratchet and Woodland's regrowth, neither of which this touches.

## What this does not decide

⚠ **The five numbers are chosen against no consumer, and that is recorded rather than hidden.**
`MapLayers.Fertility` throws; task 5 builds it and no farm exists in any milestone yet. Under
[`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) the ratios cannot be argued from what
farming *needs*, because farming is **unbuilt**. **They are a stated starting point with a named
ratifier, not a settled balance** — five `plans/0002` §D1 rows, each naming the machine, the world and
the quantity, and **the trigger to reopen every one of them is the first milestone that builds a farm.**

⚠ **It does not put Fertility on the map.** Base Fertility is Ruleset data keyed by the stored type;
there is still **no fertility column, no fertility layer and no baked field**, which is `0154` and is the
part of `0022` that was never under pressure.

⚠ **`rock` is `0.2` and not `0`.** Zero would make the exclusion total and `0022`'s *scarcity is a
gradient, never a wall* is the standing rule against walls. **A rock Cell farms badly rather than
refusing to farm**, and the difference is the one that ADR spent a section on.

## Consequences

- **`0022` is amended, not superseded.** Its refusal stands for a *generated fertility field* and is
  narrowed for a *Ruleset value keyed by a stored type*. The banner goes on that document.
- **`0154` is amended**: its *"a small enumeration"* becomes the five named here, and its own revisit
  trigger — *"terrain type turns out to need more than a small enumeration"* — is what would reopen it.
- **`CONTEXT.md` → Base Fertility loses *"in the shipped Rulesets it is uniform"***, which stops being
  true the day the varied-terrain Ruleset ships.
- **Five hash-bearing numbers**, five §D1 rows, one ratifier named for all five: the first farm.
- **The type column is `(saved AND hashed)` and the ordinals are hash-bearing.** Appending a sixth type
  is free; renumbering the five is a re-baseline.
- ⚠ **The decay rate keyed by the same type is task 4's and is NOT decided here.** `[layers]
  sealing_decay_tau` is a single global today and becomes per-type; that reconciliation is task 4's and
  is filed as such. ✅ **DONE 2026-08-24 by task 4**: the key is a `sealing_decay_tau` on each
  `[[terrain]]` table, `[layers]` refuses the old one by name, and the five values are in
  [`plans/0002`](../../plans/0002-open-questions.md) §D1 under one row. ⚠ **They are a SECOND five
  numbers keyed off this enumeration and they do NOT share the farm ratifier above** — their quantity
  is Days from demolition to bare ground, which is observable today.

## What would trigger revisiting

- **A farm is built and the ratios read wrong against it.** The named ratifier firing, and the expected
  case rather than a failure.
- **A player reads a terrain overlay to site farms and reports it as following instructions.** Then
  `0022`'s refusal was right at this scale too, and the repair is to collapse the five values toward the
  exclusion-only shape — `rock` low, the rest equal — which this decision considered and did not take.
- **Terrain type needs more than five members**, at which point `0154`'s own storage trigger fires and
  the per-Cell type may be the wrong representation.
- **Terraforming ships.** A player who can change terrain type can change Base Fertility, and a ceiling
  the player edits is a different decision from one the generator deals.
