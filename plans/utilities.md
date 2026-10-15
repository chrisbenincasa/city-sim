# Power, Water and Sewage delivery

## Outcome

A Building draws Power, Water and Sewage capacity from plants the player sites, and a shortfall
reaches the Citizens and Businesses inside it as a graded consequence they can explain. Siting a
plant is a spatial decision with a cost, an emission and a labour demand. The alternative is to
import at an Outside Connection for a price.

## Current behaviour (checked 10/14/2026 at `9fdb118b`)

| Piece | Built | Where |
|---|---|---|
| `ResourceFamily.Utility` | Declared. Used only by the Water Body Bin | `Ruleset.cs` `ResourceFamily` |
| Water Body Bin | One Utility Resource per body, filled by runoff, drained downstream | `WaterBodyTable`, `World.RunoffIntoWater`, `World.DrainWaterBodies` |
| `storage` on a Resource | Refused at load | `RulesetLoader.RefuseStorage` |
| Networked Service (`serves = "power"`, `"water"` or `"sewage"`) | Refused at load. These are Resources, not Needs | `RulesetLoader`, `adr/0032`, `adr/0103` |
| District | Derived by watershed. Holds one Pool Bin per Good | `DistrictWatershed`, `World.FitDistrictPools` |
| District adjacency graph | No | — |
| Utility Pool Bin | No. `FitDistrictPools` skips non-Goods | `World.FitDistrictPools` |
| Utility import price | No. Import prices are Good-only | `RulesetLoader` import-price check |
| Plants, consumers, outage, billing, overlay | No | Shell tool browser lists "Utilities" as disabled |

- No shipped Ruleset declares `power` or `water`. Only `sewage` exists, and only as the Water Body
  Resource in `coastal`, `flooded`, `traded` and `pictured`.
- A Building can hold a Local Utility Bin, and a Rule can consume from it. Nothing fills that Bin
  from outside the Building.
- A Pool term naming a Utility loads without refusal. At runtime it finds no market row and fires
  at zero applications, so it does nothing without reporting why (`RuleEngine` `Marketless`
  branch). This is from reading, not from a test.

## Decided

- One abstraction for all three, separated by storage (`CONTEXT.md` → Utility, `adr/0031`).
  Power has none and fails within the period. Water drains a reserve first. Sewage fills and
  overflows.
- Delivery is Networked: nobody moves, and there is no Vehicle or congestion (`adr/0032`).
- Utilities ride the Road Graph. The player draws no network. Every Lot is connected by its
  frontage.
- Distribution pools within a District, flows along District adjacency, and imports at an
  Outside Connection for a price (`adr/0013`).
- A plant is a Building with footprint, Materials, emission, Land cost and labour.
- A deficit is a gradient. No Building simply stops.
- A Utility shortfall is a standing state, never a Need (`adr/0103`).
- Upkeep is automatic. There is no maintenance funding lever (`adr/0035`).
- Two Districts are adjacent when at least one road segment has an end in each (D1, 10/14/2026).
  A District across a river shares Utilities only once a bridge exists, and removing the last
  road between two Districts cuts their link. The graph is derived and rebuilds on road edits and
  District re-evaluation.
- A link between adjacent Districts carries a Ruleset amount per Day for each road segment that
  crosses their boundary (D2, 10/14/2026). A plant behind a thin link cannot serve a large
  District, so siting and roads both matter. Per-hop distance loss is rejected as unrealistic.
- Each network loses or gains in proportion to its road length, at a per-Utility Ruleset rate
  (10/14/2026). Power loses a small near-flat fraction, Water leaks, and Sewage gains from inflow
  and infiltration. A sprawling city therefore pays more for the same delivered amount.
- Plants have a rated output per Day, and Buildings draw against it.
- Demand is an input term on an ordinary Rule, and the content author picks its basis per Rule
  (10/14/2026). A home draws per occupant through `apply derived` on Occupancy. A shop or office
  premises Rule draws per floor area, which needs a floor-area Readout. A production Rule draws
  per application. Sewage is an output of the Rule that draws Water, at a fraction of it.
- A short District gives every drawing Rule the same delivered fraction (D3, 10/14/2026). At a
  daily settle this is what a rolling blackout averages to over a Day.
- A short Utility input caps a Rule's application count at the delivered fraction, and the
  firing succeeds at reduced output (D4, 10/14/2026). Goods keep their all-or-band semantics.
  Utility terms differ because nobody holds a stock of electricity. This covers derived counts,
  which otherwise apply in full or fail, so a home degrades smoothly. Each Building records its
  delivered fraction as a standing Readout for the explanation and housing choice. A brownout
  alone is not a Rule failure; whether a sustained one feeds decline is a later decision.
- The treasury owns plants and bills by meter (D5, 10/14/2026). A plant is a staffed Business
  funded by a `[[policy]]` transfer, as a school is. Each drawing Rule pays a per-unit tariff to
  the treasury for what it receives, so a brownout bills less. The payer follows the Rule's
  owner, a Business or a Household. The tariff is a player Policy lever. This is the municipal
  model, and it settles the money circulation row's recipient for utilities.
- The network settles once a Day from the previous Day (D6, 10/14/2026). Each District's supply
  is what its plants produced, and its demand is what its Rules asked for. One flow solve fixes
  every District's delivered fraction for the coming Day. Unused Power is lost at the settle. A
  new plant or a cut road therefore takes effect the next Day, and the readout shows the
  fraction as tomorrow's supply.
  - ⚠ A per-Tick settle, or a cadence between the two, would be more dynamic. Within-Day demand is
    lumpy because only the Rules firing in a Tick ask for anything, so a finer cadence needs
    demand smoothed over a window. Revisit if the one-Day lag reads as unresponsive in play.
- Telecommunications is refused (`0074`). District heating is deferred. Gravity-fed sewage is
  deferred until plant siting proves to be decided by land price alone (`docs/deferred.md`).

## Decisions needed

| # | Question | Candidates | Blocks |
|---|---|---|---|
| D7 | Does an import pay the haulage charge (`adr/0133`) | Yes; no | Import slice |
| D8 | Does treatment discharge into a Water Body | Yes, joining the existing `[water] carries = "sewage"`; or a separate Bin | Sewage slice |
| D9 | Does a sustained brownout feed decline | No; or after a Ruleset duration below a Ruleset fraction | After slice 1 |

## Slices

1. **Power in one test world.**
   - Derive the District adjacency graph from road segments that cross a District boundary.
   - Add a Utility Pool Bin per District, accept `storage = 0`, and add the link capacity and
     network loss keys.
   - Add a plant kind whose Rule makes Power, and a floor-area Readout for premises demand.
   - Settle daily: supply from yesterday's output, demand from yesterday's draws, one flow solve.
   - Cap a Rule's application count at its Utility term's delivered fraction.
   - Add the delivered-fraction Readout, an explanation naming the short Utility, and the
     coverage overlay.
   - Refuse a Pool term whose Resource has no supported delivery, so the silent zero cannot
     happen.
2. **Water.** Storage that carries over between Days. A water tower is a Bin with large storage.
   Leakage scales with road length.
3. **Sewage.** Storage that fills, generated as an output of Water-drawing Rules. Inflow grows
   with road length. Overflow raises the pollution layer, which lowers Fertility. Resolve D8.
4. **Import at an Outside Connection.** Price, gate and payer. Resolve D7.
5. **Billing.** A per-unit tariff charge on delivered Utility terms, paid to the treasury, and
   the tariff Policy lever. Update the [money circulation](money-circulation.md) Utilities row.
6. **Critical loads.** A Ruleset flag on a Building kind serves it in full before the rest share
   the remainder, as real load shedding protects hospitals and pumping. Take this up once a
   critical kind draws a Utility. Water pumping that draws Power makes a power shortage cascade
   into a water shortage.
7. **Base content.** Add the three Utilities and their plants to `rulesets/base/`, and enable the
   shell's Utilities tool category.

## Acceptance for slice 1

- A headless test world has two Districts joined by one road, one plant and consumers in both.
  Power reaches the far District through adjacency.
- Removing the joining road leaves the far District short from the next Day. Its consumers run at
  the delivered fraction, succeed rather than fail, and the explanation names Power.
- A per-occupant home Rule and a greedy production Rule both run at the delivered fraction.
- With link capacity below the far District's demand, adding a second crossing road raises its
  delivered fraction.
- Restoring the road, or placing a second plant there, restores full delivery the next Day.
- Unused Power does not carry over, and network loss grows with road length.
- Replay, save/reload and thread-count equivalence hold, and `DerivedRebuildAuditTests` covers
  the adjacency graph.
- A driven demonstration shows the outage and the recovery in the shell.
