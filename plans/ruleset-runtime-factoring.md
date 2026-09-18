# Ruleset runtime factoring: shared definitions and fractional consumption

## Outcome

Shared `[[basket]]` and `[[reserve]]` definitions in the Ruleset source, resolved by Formats into
today's numeric Ruleset, with Core carrying the one thing arithmetic cannot: saved fractional
consumption progress. A designer states daily quantities and Days of cover once and references them;
the loader derives per-firing amounts and Bin capacities.

Scoped 2026-09-17 against `main`. Supersedes the runtime half of the board's
*Ruleset runtime factoring and saved profiles* entry. [0077](0077-ruleset-source-loading.md) owns the
source format; the [authoring contract](../docs/ruleset-authoring.md) is canonical and needs the
terminology correction below.

## Decisions taken

| Decision | Reason |
|---|---|
| The declaration is `[[reserve]]`, never `[[storage]]` | `CONTEXT.md` defines Capacity and Storage as different parameters and says Storage is *not* Bin capacity. `RulesetLoader.RefuseStorage` holds `storage` open as a named hole on `[[resource]]` for that meaning |
| Consumption accrues per firing, not per elapsed Tick | A blocked Rule does not fire, so a starvation gap accrues nothing. Forfeit falls out of the shape rather than needing a special case |
| Core first, then the complete loader | Shipping basket-derived capacity without basket-driven consumption lets the two drift: change the basket and the ceiling moves while the consumption does not |
| Per-instance parameter variation is out of scope | See *Boundaries* |

⚠ `CONTEXT.md` already uses "reserve" for a Household's Life-Stage-sized money buffer. The two are
unrelated and both attach to Households; document the distinction where `[[reserve]]` is defined.

## Core: implemented

`BinTable.Progress` is appended after `owner_next` as `consumption_progress`, and a `Term` carries
`PerDay` as an init property, so an input amount may be a quantity per Day. `RuleEngine.Check`
derives the whole units a firing takes and holds the remainder; `Fire` banks it, so a Rule that is
checked and then blocked accrues nothing. Both Bin constructors zero it, for the reason the level
and the cost are already written on a recycled slot.

Refused in the engine, pending the loader's own refusal with a file and a line: a per-Day quantity
on a pool term, which settles three deltas against a market row and so has no Bin to carry a
remainder; on an output term, because the column is consumption progress; and under an apply count
that is not fixed at one, because Check multiplies every delta by the applications it settled on.

`SaveHeader.Current` is 7. `World.HashSeed`'s version byte is unmoved: the fold did not change, a
column was added to it. The four golden artefacts were re-recorded on 2026-09-17 and nothing else
in the working lane moved — 3,759 passed, 4 golden baselines failed, and the re-record is the only
diff. `ConsumptionProgressTests` covers the daily total, eight Days without drift, the evenly
dividing case, a firing below a whole unit, accrual frozen while blocked, no catch-up on recovery,
save and reload mid-fraction, a recycled Bin, and the apply-count refusal.

Still Core's, still open: nothing. The remaining work is `What Formats needs` below.

## What Core needs: one saved column

`BinTable` is already one row per owner per Resource, which is the grain the contract asks for
("per actor and consumed Good"). `RuleInstanceTable` is per subject per Rule and has no per-Good
grain, so it is the wrong home.

Append one column after `owner_next` in `src/Borough.Core/Rules/BinTable.cs`. Appending rather than
inserting keeps the golden trace diff attributable, because hash order is declaration order.

```
Progress = _rows.Saved<int>("consumption_progress", Touch.PerTick);
```

The arithmetic is `CitizenTable.WageRemainder`'s idiom, applied per firing rather than per Tick:

```
scaled   = Progress[bin] + usePerDay * definition.Rate
whole    = IntegerMath.FloorDiv(scaled, Ticks.PerDay)
Progress[bin] = scaled % Ticks.PerDay
```

`whole` is the term amount for that firing. `Progress` is bounded below `Ticks.PerDay` (2048), and
the product is bounded by `usePerDay × EventWheel.CoarseCeilingTicks`.

Consequences to carry:

- Reset on slot recycle, as `WageRemainder` is at `World.cs:8216` and `:8389`.
- `SaveHeader.FormatVersion` 6 → 7. It versions the declaration set and a mismatched save is refused
  before its body is read, so this invalidates existing saves.
- Golden re-record and a deliberate hash re-baseline, under
  [the established procedure](../tests/Borough.Tests/Golden/README.md).
- Written in phase 3, which is serial. It stays safe under the planned phase-2 parallelism only as a
  per-row write keyed by the row being processed, never a `+=` into a shared aggregate.

### What Core does not need

| Capability | Already provided by |
|---|---|
| Atomic multi-Good consumption | `RuleEngine.Check` writes nothing and tests every term before any Bin moves; an early return leaves nothing applied |
| Shortage and retry | `adr/0063`. A short Rule subscribes to the blocking Bin; `World.Drain` recomputes the requirement live on every deposit and wakes it. No stored deficit, no polling |
| Per-instance capacity plumbing | `adr/0064`. Capacity is `Rows.Derived`, rebuilt from the Ruleset in force at load and at every swap |

## What Formats needs

1. `[[basket]]` — `id`, `label`, `owner` matching `BinTenancy`, `use_per_day` mapping Resource ids to
   positive integers. `[[reserve]]` — `id`, `label`, `days`.
2. A Rule's `basket` reference supplies its input terms. Explicit `inputs`/`outputs` alongside it are
   refused, as are `basket` and `recipe` together. Fixed apply count one.
3. A Bin's `reserve` reference derives `capacity = use_per_day × effective days`, with a local `days`
   override replacing the profile's Days and omission restoring inheritance. Money Bins are excluded;
   they are unbounded and already refuse a declared capacity.
4. Refuse two basket Rules consuming one Resource for one actor. They would share the one progress
   field on that Bin and interleave.
5. Resolved capacities must fit existing Core bounds. Literal capacities remain supported.

## Acceptance

- Three units a Day over eight successful firings consumes three units, not zero and not eight.
- A zero-whole-unit firing is a success that moves no Goods.
- A Bin's capacity derives from its basket and reserve; changing the shared basket moves every
  dependant capacity; a local `days` override preserves Days rather than a frozen number.
- Save and reload mid-fraction continues identically. Replay and thread-count equivalence hold.
- A starved actor accrues nothing while blocked and does not catch up on recovery. Demonstrate a
  shortage and a player-led recovery, not only uninterrupted totals.
- A shipped Ruleset converted to baskets produces the same Ruleset field-for-field as its literal form,
  where the quantities are equivalent.
- Existing single-file content and golden identities remain supported. Re-record once, deliberately.

## Boundaries

**Per-instance parameter variation is deliberately excluded.** Two Buildings of one kind cannot differ
in a declared parameter today, and expressing that is wanted but does not block this work.

The 254-kind ceiling that appeared to force it is not a design bound. `BuildingTable.Kind` is a
`byte` because the Rule engine indexes arrays with it; `adr/0150` names the ceiling an artifact and
carries "a kind namespace widened past a byte" as a revisit trigger. The 83 kinds × 3 variants
measurement in [0076](0076-ruleset-authoring-experiment.md) came from a synthetic catalogue that gave
every kind all three variants by construction. If the kind budget is ever actually exhausted, widening
the column is one mechanical change across 58 sites in Core, not a reason to invent a second dimension
of saved state.

When per-instance variation is taken up, `RebuildCapacities` overwrites every Bin ceiling at load and
at every Ruleset swap, so any per-instance capacity must be derivable there from already-saved state.
Deriving from Lot floor area is the cheapest route and the one three existing capacities already use;
it needs no saved column, no save break and no re-record.

Work-dependent production belongs to the private-production item. A graphical editor, mod
distribution, arbitrary inheritance and general migration mappings are outside this slice.
