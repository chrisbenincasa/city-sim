# 0068 — The choice model, and the Hinterland as a row in it

**Scoping document. Written 2026-09-08, against `choice-model` at `41f87dd`.**
**[`0045`](0045-amnesty.md) queue row 28 — [`06`](../docs/06-roadmap.md) milestone 16, ungated.**

> ***`Transcendental.Exp` was built, tested, documented and has never been called. The thing it was
> built for does not exist.***

---

## Status

✅ **TASK 1 BUILT 2026-09-08 and it moved no recorded hash**, which was the point of decision D1: every
shipped world states no μ and keeps the argmax it had. Three failures in the working lane were the
generated schema, the generated key reference and `adr/0048`'s refusal count, all regenerated or
updated. **Tasks 2 and 3 unstarted.**

🔴 **AND THE HALF THAT DID NOT LAND IS THE FINDING** — see *What the second term found*. Rent is in
the utility function, is read every time a candidate is scored, and **has no world in which it can
differ between two candidates.** Decision D4 is what that costs and it is open.

**Scope** set by the user on the opening call:
**full row 28, Hinterland included** — the softmax, the stay-put alternative, and
[`adr/0023`](../docs/adr/0023-immigration-arrives-through-the-gate.md)'s comparison against the
Outside through the identical utility function. ⚠ **The Hinterland's population stock and its
drawdown are NOT in scope** — that is `adr/0023`'s second half and it needs a sink argument of its
own.

---

## The gap, stated

[`02 §5.4`](../docs/02-simulation-model.md) specifies `P(i) = exp(μ·V_i) / Σ exp(μ·V_j)` over a
sampled candidate set. What runs is `PlacementEngine.TryHouse:495-501` — an argmin over
`Distance(lot) × weight`, one term, no distribution, and a first-fit early return above it that is
still the accept in most shipped worlds.

**The build is already the μ→∞ limit of the model it lacks**, which is what makes the key's absence
a coherent world rather than a hole: a Ruleset that states no `μ` gets today's deterministic pick,
and stating one is what buys the distribution. ⚠ **That framing is the whole reason task 1 moves no
recorded hash** — every shipped file keeps the argmax it has, and the model arrives in a new
demonstration file.

**Three things arrive with the softmax, and only the first is arithmetic.**

| | What | Why it cannot be deferred |
|---|---|---|
| μ | A **feel** parameter, not a fitted coefficient | `02 §5.4`: *"when the city feels too herdy or too random, tune `μ`, not the coefficients"*. Its ratifier is a person at the controls |
| The scales | Domain units per utility unit, one per term | Only differences matter, so terms are commensurable or the model is arbitrary. `adr/0023`: **author in domain units, never in utility units** |
| The stay-put row | An explicit alternative to moving | *Everything available is terrible and nobody moves* is **inexpressible** without it — adding a constant to every alternative changes nothing |

## What the arithmetic already settles, so this document does not

[`adr/0038`](../docs/adr/0038-the-transcendental-tables-are-sized-by-the-representation.md) fixed
the table at 256 entries, stated the error budget, and named the consequence that matters here:
**`exp` underflows below −11.09, so a candidate more than about 11 utility units below the best is
impossible rather than unlikely, and doubling μ halves that horizon.** `TranscendentalTests`
already carries `TabulatedSoftmax` — *"computed the way the core will have to compute it"* — and a
differential test against a double-precision oracle showing worst selection-probability divergence
below 0.001. ***The core's implementation is that harness moved, not a new derivation.***

---

## Tasks

**1 — The draw where the argmax is.** `Choice.Draw(utilities, μ, value)` in `Borough.Core/Rules`,
`PurposeTag.ChoiceDraw`, and a utility function over the two terms the build can already state:
centrality and rent. `[placement] mu_percent`, `centrality_tiles_per_unit`, `rent_per_unit`. A new
`chosen.toml`, so nothing recorded moves.

**2 — The stay-put row.** `Reassess` scores the incumbent through the identical function and puts it
in the candidate set; a Household in the Pool scores remaining there. `gives_up_after_days` stops
being the only way to stay put and becomes the bound on how long staying put is available.

**3 — The Hinterland as a row.** `[[hinterland]]` gains `rent`, `wage` and a commute figure in the
same units a District exposes. A prospective Household scores *staying outside* against *moving
here* with the same function, and `arrivals_per_day` goes back to being throughput rather than
interest.

---

## Decisions

**D1 — Absent `mu_percent` means the deterministic pick, and that is the model rather than a
fallback.** Taken. The alternative was gating on `CentralityVaries`, which would have made the
choice model arrive with a taste axis it does not depend on.

**D2 — The scale keys are required when `mu_percent` is stated and refused without it.** Taken, on
the `gives_up_after_days` pattern: a world that opts into a utility comparison owes the units the
comparison is in.

**D4 — Does `rent_per_unit` stay required, given nothing can exercise it?** Open, and it is the
first decision task 2 runs into. Keeping it required is a key every choosing world must state and no
world can use; making it conditional needs a condition, and *a world where two housing kinds stand*
is not a thing the loader can check. ⚠ **The third option is to fix the world rather than the key** —
see the finding below, whose cause is the populator and not the format.

**D3 — Does `CommandKind.Arrive` survive task 3?** Open. Interest becoming emergent does not by
itself remove a door a player or an Input Log can push somebody through, and `ArrivalDump` and every
committed log depend on it.

---

## What the second term found

**A `[[building]] rent` is per KIND, and no world can stand two housing kinds, so the rent term is
identical across every candidate — and a term identical across every candidate CANCELS.** Only
differences matter in a logit. The scale is authored, read, and weighs nothing.

⚠ **It was built the other way first and the world refused it.** `chosen.toml` shipped for about an
hour with `dwelling` at 500 a Day beside `flat` at 120, which is exactly the trade-off `02 §5.4`
describes and exactly what `rent_per_unit` exists to weigh. **Over 20,480 Ticks at 12,000 Citizens,
zero flats stood.** Two arrangements were tried and both failed for different reasons, which is what
makes this structural rather than a mistake:

| Arrangement | What happened |
|---|---|
| The second `[[zone_rule]]` on `zone = 1` | The generator paints two permission bits and **bit 1 is trade land** — `SyntheticCity` zones Housing or Trade and nothing else. A housing kind on the trade bit is a rule that samples for ever |
| Both `[[zone_rule]]` tables on `zone = 0` | **`SyntheticCity` raises the standing city out of one kind**, and the Zone Rule sweep that could raise the other builds about **one Building per 2,048 Ticks**. Reversing the order of the two tables changed nothing |

🔴 **`--kinds` diagnosed it in its own words** — *"1 Zone Rule(s) raise it on a bit this world paints,
and none has yet won a Lot"* — which is the instrument working. ⚠ **The cause is the POPULATOR and
not the format.** The loader accepts two housing kinds; the world has no way to stand them.

⚠ **This is the shape the amnesty was opened to hunt, arriving one level down.** Row 28 was chosen
because `Transcendental.Exp` was built and uncalled. Building its caller produced a Ruleset key that
is read and inert — ***a reader is not the same as a difference*** — and the reason is a mechanism
nobody has written rather than evidence about what the design should be (`adr/0070`: *unbuilt*,
which is not evidence).

⚠ **What it does NOT block is task 3.** A Hinterland states its own rent, so *staying outside* against
*moving here* is a rent difference between two rows of the same utility function without any second
kind standing anywhere. ***The term acquires a difference from the Outside before it acquires one
from the city.***
