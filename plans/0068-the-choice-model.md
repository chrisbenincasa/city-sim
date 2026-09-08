# 0068 — The choice model, and the Hinterland as a row in it

**Scoping document. Written 2026-09-08, against `choice-model` at `41f87dd`.**
**[`0045`](0045-amnesty.md) queue row 28 — [`06`](../docs/06-roadmap.md) milestone 16, ungated.**

> ***`Transcendental.Exp` was built, tested, documented and has never been called. The thing it was
> built for does not exist.***

---

## Status

✅ **ALL THREE TASKS BUILT 2026-09-08 and none moved a recorded hash**, which was the point of
decision D1: every shipped world states no μ and keeps the argmax it had.

🔴 **THE HEADLINE IS TASK 3's.** `[[building]] arrivals_per_day` has stopped being what decides how
many people want to live here. Measured on the new `welcomed.toml` against `crowded.toml`, same
asking, same doors: **admissions fell from a flat 96 a Day at every gate to between 1 and 11, and
the Unplaced Pool from 839 waiting to 31.** Nothing states a rate anywhere.

✅ **D4 IS CLOSED AND THE ANSWER WAS NEITHER OPTION.** `rent_per_unit` stays required, and task 2
gave it a difference to weigh without any second housing kind: **`moving_costs_rent` is denominated
in the same money and only one candidate ever carries it**, so it does not cancel the way a uniform
rent does. ***The inert key was made load-bearing by the next task rather than by a decision about
the key.***

🔴 **AND TASK 2 FOUND A CEILING NOBODY AUTHORED** — see *What stickiness found*.

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

**D4 — Does `rent_per_unit` stay required, given nothing can exercise it?** ✅ **Closed 2026-09-08:
it stays, and the question dissolved.** Task 2's `moving_costs_rent` is authored in rent and
converts through this scale, and **the incumbent is the only candidate carrying it**, so the term
has a difference whether or not any world stands two housing kinds. ⚠ **The finding below still
stands** — no world can price two homes differently — and it is now a limit on what the model can
express rather than on whether a key is read.

**D5 — What bounds `moving_costs_rent` from above?** Open. `adr/0038`'s horizon puts a hard ceiling
at `11.09 / μ` utility units, past which moving is impossible rather than rare, and **nothing
refuses a file that states one.** ⚠ **The two obvious repairs are both wrong**: a fixed ceiling
cannot be checked, because μ and `rent_per_unit` are both in the same table and the product is what
matters; and clamping would silently give a designer a different city from the one they authored.
***A refusal that reads three keys against each other is the shape, and what it should say is the
open part.***

**D3 — Does `CommandKind.Arrive` survive task 3?** ✅ **Closed 2026-09-08: it survives and changes
meaning.** The command said *admit this many* and now says ***this many present themselves***; how
many cross is the model's answer. ⚠ **A decline is not a refusal**, so it does not break the
admission loop the way a full gate does — the next prospect is a different person facing the same
city. ***Nothing in `Borough.Formats` changed and every committed log still replays***, because the
payload is unaltered.

**D6 — Who knocks, and how often?** Open, and it is the half of `adr/0023` this task did not build.
The comparison decides *whether* somebody crosses; **what decides how many present themselves is
still outside the simulation** — a player, an Input Log, or `ArrivalDump` asking for more than the
door can take. `adr/0023`'s answer is the **Hinterland as a stock the city draws down**, where the
rate falls because the willing are taken first. ⚠ **Until that exists the interest half is real and
the volume half is not**, which is a smaller gap than the one this task closed and is not a rate
hiding in the build.

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

---

## What stickiness found

**Two utility units of stickiness is a city playing musical chairs and twenty is a city nobody can
leave.** Measured on `chosen.toml` over 20,480 Ticks at 12,000 Citizens, against roughly 2,100
housed Households:

| `moving_costs_rent` | Utility units | Preferred moves | What that is |
|---:|---:|---:|---|
| 240 | 2 | **9,388** | every family moving four times in ten Days |
| 720 | 6 | 324 | a move about every 65 Days — **shipped** |
| 960 | 8 | 42 | a move about every 500 Days |
| 2,400 | 20 | **0** | nobody can leave at all |

⚠ **The first row is `adr/0017`'s churn pathology arriving through a channel that ADR does not
mention.** That decision argues stickiness against the *provider list* — which shop, which
workplace — and the housing move is a different recurring choice with the same disease. A two-unit
incumbency bonus at μ = 1 leaves the incumbent about a 70% chance against three alternatives, and
the sweep runs twice a Day.

🔴 **The last row is `adr/0038`'s horizon reaching gameplay for the first time.** Twenty units is
past `11.09 / μ`, so `exp` underflows and every alternative weighs **exactly zero** — moving is
impossible rather than rare. ***The stickiness key therefore has a ceiling nobody authored, it moves
with μ, and a designer turning it up finds the city stop dead rather than slow down.*** That ADR
asked for this consequence to be argued with rather than the resolution; this is the first argument
it has had from a running city. **D5 owns what to do about it.**

## What task 2 did NOT build, and why

**A Household in the Unplaced Pool still takes the best of what it was shown, always.** The stay-put
row landed on the *reassessment* side only. ⚠ **The Pool side needs the Hinterland and cannot be
faked**: the utility of *remaining in the Pool* is the utility of not living in this city, and
[`adr/0023`](../docs/adr/0023-immigration-arrives-through-the-gate.md) rejects a hand-authored
`V_outside` **by name** — *"it has no referent; neither a designer nor a playtester nor a player can
say whether it is too generous."* ***So the second half of task 2 is inside task 3***, and that is
the ordering rather than an omission.

---

## What the gate found

**Turning the choice model on at the doors changed a city more than anything else in row 28.**
`welcomed.toml` is `crowded.toml` with a choice model and four priced Hinterlands, run at 1,000
Citizens for 8,192 Ticks with every gate asked for 100 Households a Day against a ceiling of 96:

| | `crowded.toml` | `welcomed.toml` |
|---|---:|---:|
| west admitted, last Day | 96 | 11 |
| north admitted, last Day | 96 | 9 |
| south admitted, last Day | 96 | 7 |
| east admitted, last Day | 96 | 1 |
| Unplaced Pool | **839 waiting** | **31 waiting** |

⚠ **The four columns order by what the Outside costs, which is the only thing that differs between
them.** The east prices a home at 300 a Day against 400 here and sends almost nobody; the north
prices one at 1,500 and sends nine times as many. ***That ordering is not authored anywhere*** —
`crowded.toml`'s own header says *"nothing in the simulation decides to arrive"*, and that sentence
is now false.

🔴 **A prospect that can see nowhere to live does not come, and it is not a special case.** Every
candidate failing the affordability and vacancy filters leaves one row in the comparison, and a
softmax over one row returns it. ***So "the city is full" falls out of the model rather than being
written into it*** — which is why the Pool fell to 31 rather than growing without bound. ⚠ **That
inverts `crowded.toml`'s premise**, which is why `welcomed.toml` is a second file and not an edit.

⚠ **The first run of this measured almost nothing and the reason is worth keeping.** `crowded.toml`
prices no dwelling at all, so `V_here` was zero for every prospect from every edge and the four
Hinterlands were told apart by the draw alone — 5, 5, 4, 3. ***A comparison needs both sides
priced***, and the city having no price is the same defect as the Outside having none.
