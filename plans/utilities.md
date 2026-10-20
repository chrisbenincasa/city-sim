# Power, Water and Sewage delivery

## Outcome

A Building draws Power, Water and Sewage capacity from plants the player sites, and a shortfall
reaches the Citizens and Businesses inside it as a graded consequence they can explain. Siting a
plant is a spatial decision with a cost, an emission and a labour demand. The alternative is to
import at an Outside Connection for a price.

## Current behaviour (checked 10/20/2026 on `utilities-1`)

| Piece | Built | Where |
|---|---|---|
| Delivered Utility | A Utility Resource stating `link_per_crossing` is delivered. `storage`, `link_per_crossing` and `loss_per_road_tile` load | `RulesetLoader`, `Ruleset.IsDeliveredUtility` |
| District | Derived by watershed. Holds one Pool Bin per Good | `DistrictWatershed`, `World.FitDistrictPools` |
| District adjacency graph | Derived. Crossing counts per District pair, and road Tiles credited per District | `DistrictAdjacency.Compute` |
| Utility network row | One saved row per District per delivered Utility, holding the Pool, today's fraction and yesterday's supplied/asked/received | `UtilityFlowTable`, `World.FitUtilityFlows` |
| Daily settle | Road loss added to local demand, local production first, then integer max-flow across the links. Runs at the head of the Day's first Tick | `World.SettleUtilityNetworks`, `Simulation.Step` |
| Degraded firing | A pool Utility input caps the application count at `floor(count × delivered)`. It never blocks a Rule | `RuleEngine.Check` |
| Floor-area Readout | Built, Building-scoped | `Readouts.FloorArea` |
| Evidence | Each drawn Utility with today's delivered Ratio and yesterday's received/asked. No new saved state | `BuildingEvidence.Utilities`, `Evidence.OfBuilding` |
| Shell | Building panel Utilities section, the brownout clause in the condition line and in attention, and a Utilities Building wash | `Main.Utilities.cs`, `Main.Information.cs`, `Main.Diagnosis.cs` |
| Headless | The evidence dump prints a per-Utility delivered/served/asked table | `EvidenceDump.Utilities` |
| Water Body Bin | One Utility Resource per body, filled by runoff, drained downstream | `WaterBodyTable`, `World.RunoffIntoWater`, `World.DrainWaterBodies` |
| Networked Service (`serves = "power"`, `"water"` or `"sewage"`) | Refused at load. These are Resources, not Needs | `RulesetLoader`, `adr/0032`, `adr/0103` |
| Utility import price | No. Import prices are Good-only | slice 4 |
| Billing and the tariff Policy | No | slice 5 |
| Base content | No. `powered.toml` is the only file declaring a Utility, and the shell's tool browser still lists "Utilities" as disabled | slice 7 |

- A Building can hold a Local Utility Bin, and a Rule can consume from it. Nothing fills that Bin
  from outside the Building.
- A pool term naming a Utility that states no `link_per_crossing` is refused at load, so the
  silent zero `RuleEngine`'s `Marketless` branch used to produce can no longer happen.
- Delivery is structure rather than tuning on a hot reload (`RulesetChange.ResourceDelivery`). A
  Utility that stops being delivered loses its networks and its District Pool rows
  (`World.RetireUndeliveredNetworks`); what a crossing carries and what a Tile loses stay numbers.
- ⚠ `--evidence` refuses a Ruleset with no `[[zone_rule]]`, so `powered.toml` cannot be run through
  the runner. The dump's table is covered by calling `EvidenceDump.Utilities` from a test instead.
  A Ruleset that both declares a Utility and condemns something would close this.

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
- An imported Utility pays its import price only, and no haulage charge (D7, 10/20/2026). The
  price covers delivery to the city's edge. Inside the city, the treasury owns the network and
  already pays for distance through street upkeep and `loss_per_road_tile`. A haulage fee would
  have the treasury pay itself, or need a network owner Borough lacks. Good imports pay no
  haulage either (`adr/0135`). Revisit when the `adr/0133` haulage charge ships for Goods.
- Telecommunications is refused (`0074`). District heating is deferred. Gravity-fed sewage is
  deferred until plant siting proves to be decided by land price alone (`docs/deferred.md`).

## Decisions needed

| # | Question | Candidates | Blocks |
|---|---|---|---|
| D8 | Does treatment discharge into a Water Body | Yes, joining the existing `[water] carries = "sewage"`; or a separate Bin | Sewage slice |
| D9 | Does a sustained brownout feed decline | No; or after a Ruleset duration below a Ruleset fraction | After slice 1 |

## Slices

1. **Power in one test world. Built on `utilities-1`, 10/20/2026.**
   - Ruleset keys: `storage`, `link_per_crossing` and `loss_per_road_tile` on a Utility Resource.
     Stating `link_per_crossing` is what makes the Utility delivered. The fixture is
     `rulesets/powered.toml` — a plant, a per-occupant dwelling draw and a greedy works draw.
   - The settle runs at the head of each Day's first Tick and fixes one fraction per District for
     the whole Day, so a cut road or a new plant reads from the next Day.
   - A short Utility degrades rather than blocks, which is why no field a stopped Rule fills can
     report a brownout. The explanation carries it in its own field.
   - A Utility an `on_fail` link draws counts as drawn. A link holds no Rule Instance of its own
     and runs against the head's, so the readers walk the chain from each Instance's head.
   - A District that has a consumer or a plant owes `loss_per_road_tile × road Tiles` before
     anybody in it is served, and local production covers that total before the links do. So the
     loss falls on an importing District as much as on a producing one.
   - Tests: `DistrictAdjacencyTests`, `UtilityRulesetLoadTests`, `PowerDeliveryTests`,
     `UtilityEvidenceTests`. Driven demonstration in `evidence/utilities-slice-1/`.
   - Gap found: a Lot built in code gets one Tile of ground, so the shell draws no massing for it
     and a wash over a hand-built fixture is invisible. The fixture states its ground.
   - **Known gaps accepted for slice 1.**
     - A District with neither demand nor production pays no road loss, so a pure relay forwards
       what crosses it for nothing.
     - `DistrictAdjacency` gives each pair touching one component of district-less road
       `min(n_A, n_B)` crossings, taken per pair. One District facing a 3-Segment boundary onto a
       component with two neighbors behind it therefore gets 2 + 2 crossings of outbound capacity
       over that 3-Segment boundary, which overstates it.
     - The max-flow picks one optimal split when total supply is short, with no fairness between
       Districts.
     - Dropping `link_per_crossing` on a hot reload closes the Utility's networks and Pool rows;
       restoring it opens new ones at nothing delivered. A Day's production does not survive the
       edit either way.
     - A Day's `produced` and `demanded` saturate at `UtilityFlowTable.Bound` — 10¹⁸, which a
       Ruleset only reaches by stating absurd amounts — rather than wrapping.
2. **Water.** Storage that carries over between Days. A water tower is a Bin with large storage.
   Leakage scales with road length.
3. **Sewage.** Storage that fills, generated as an output of Water-drawing Rules. Inflow grows
   with road length. Overflow raises the pollution layer, which lowers Fertility. Resolve D8.
4. **Import at an Outside Connection.** Price, gate and payer. No haulage charge (D7).
5. **Billing.** A per-unit tariff charge on delivered Utility terms, paid to the treasury, and
   the tariff Policy lever. Update the [money circulation](money-circulation.md) Utilities row.
6. **Critical loads.** A Ruleset flag on a Building kind serves it in full before the rest share
   the remainder, as real load shedding protects hospitals and pumping. Take this up once a
   critical kind draws a Utility. Water pumping that draws Power makes a power shortage cascade
   into a water shortage.
7. **Base content.** Add the three Utilities and their plants to `rulesets/base/`, and enable the
   shell's Utilities tool category.

## Acceptance for slice 1

Met on `utilities-1`. `PowerDeliveryTests` carries the first six lines, `FactorioTests` and the
Golden fixtures carry the equivalences, and `evidence/utilities-slice-1/` carries the last.

- A headless test world has two Districts joined by one road, one plant and consumers in both.
  Power reaches the far District through adjacency.
- Removing the joining road leaves the far District short from the next Day. Its consumers run at
  the delivered fraction, succeed rather than fail, and the explanation names Power.
- A per-occupant home Rule and a greedy production Rule both run at the delivered fraction.
- With link capacity below the far District's demand, adding a second crossing road raises its
  delivered fraction.
- Restoring the road, or placing a second plant there, restores full delivery the next Day.
- Unused Power does not carry over, and network loss grows with road length — in the District that
  generates the Power and in the one that imports it.
- Replay, save/reload and thread-count equivalence hold. `DerivedRebuildAuditTests` covers the
  Pool-to-network index and that a rebuild does not change the next settle's output; the adjacency
  graph's rebuild stability is held by
  `DistrictAdjacencyTests.Results_are_sorted_unique_and_unchanged_by_derived_rebuild`, since
  `Compute` rebuilds the graph whole at every settle and nothing else reads it.
- A driven demonstration shows the outage and the recovery in the shell.
