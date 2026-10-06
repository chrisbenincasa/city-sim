# First playable founding loop

Board row: First playable founding loop.
Builds on: [0073 arrivals](0073-the-city-attracts-people.md), [0070 city spending](0070-the-city-spends.md),
[labour-bound production](private-production-and-labour.md) (PR #105), [0075 viability](0075-base-game-ruleset-viability.md).

## Outcome

One Ruleset, the base package in `rulesets/base/`, plays a whole loop from bare Ground:

1. The player lays Streets, zones housing and work, and places a gate and a school.
2. Families arrive, find homes and jobs, and buy what local Businesses make.
3. The city keeps running for months on its own Money circuit.
4. A shortage arises from growth. The player sees its cause, acts, and the city recovers.

Replay and save/reload hold throughout. Completion does not require every planned city system.

## Current behaviour (checked 10/05/2026 at `ef405f1c`)

| Stage | Exists | Missing |
|---|---|---|
| Start | Shell defaults to `rulesets/base/ruleset.toml`. "New empty city" steps `Ground` (`CityPreparation.cs`) | The shell never reads `founding.borough`, which only `BaseFoundingPackageTests` and Headless `--log` use |
| Arrival | A gate plus zoned vacant Lots is enough. `HinterlandEngine` admits families, and the housing Zone Rule builds a free `dwelling` while the Unplaced Pool is non-empty | — |
| Supply | `restock` has `inputs = []`, so every dwelling makes its own `sundries` for free | Production, shop sales and Household purchases. Base declares no `[shopping]`, `pool` term or Hinterland `prices` |
| Business Money | Wages, arrears and bankruptcy (`WageEngine`). `[[business]] opening_grant` pays a zone-raised Business out of the treasury (slice 3a) | Base declares no grant, so its zone-raised Businesses open at zero balance. The `shop` trade posts no wage and buys nothing |
| City Money | Opening treasury 4,194,304. School `placement_cost` 262,144. `school_funding` grant per job. Demolition paid to the displaced | Income. `ruleset.toml` says "the city has no income". `[income_tax]` and `[business_tax]` exist only in `taxed.toml`/`taxing.toml`. Road upkeep code exists (`SpendOnUpkeep`) but no shipped Ruleset sets it |
| Shortage | `StarvedSince` clock, `SupplyEvidence`, the inspector's "Waiting for X" line, the city evidence panel and the trouble layer | Staffing, school places and jobs have counters but no elapsed episode. No notification surface |
| Recovery | Zone, Street, Service, Gate, Govern, Fund, Tax and Demolish all reach the shell. `scripts/ui/check-diagnosis.py` shows a player-led recovery on a `taxed.toml` fixture | No shortage-then-recovery run on base |
| Verification | `BaseFoundingPackageTests` replays `founding.borough` twice to Day 12 and matches daily State Hashes. A Day 6 save reloads and matches the uninterrupted run to Day 12. `TreasuryFromAFileTests` sweeps base with the top-level files | No run longer than 12 Days |

PR #105 ships `milled.toml`. In it, mills turn labour into flour, and grocers bake flour into sundries
for Households. Its trade fades after about two weeks. Businesses open with no Money, and Money then
collects in mill balances with no way out. The founding loop inherits this problem the moment base adopts that chain.

## Boundaries

In scope:

- Base package content and the mechanisms the loop needs to close its Money circuit.
- Persistence and replay coverage for base.
- One shortage with a player-led recovery, demonstrated headless and driven in the shell.

Out of scope:

| Work | Owner |
|---|---|
| Exports and Money entering from outside | [export-base](export-base.md) slice 2 |
| Charging for Streets and zoning | Infrastructure construction, wear and renewal |
| Faster construction gestures and rezoning | Player construction tools |
| Freeform Streets and road-aligned Lots | Freeform local Streets |
| Notifications | Trajectory notifications and persistent Pins |
| Integrated execution replacing the Ruleset lowering | [0077](0077-ruleset-source-loading.md) item 3. It gates the first *release*, not this loop |
| Balance | Playtesting and content calibration. Numbers here are provisional |

## Decisions (10/05/2026)

| # | Question | Options | Decided |
|---|---|---|---|
| D1 | Where do sundries come from? | (a) The labour chain from `milled.toml`: mill → flour → shop bakes → Households buy. (b) Priced imports only | (a), with a priced import at the gate edge as the expensive fallback. Imports alone drain Money every purchase and make the city a pass-through |
| D2 | Where does a zone-raised Business get its opening Money? | (a) The treasury pays a Ruleset-declared opening grant, refused when the treasury is short. (b) A founder Household capitalises it, as `Found` does. (c) New Money is minted at opening | (a). It conserves Money and gives the treasury a growth cost the player feels. (b) needs zoning and founding to be reconciled. (c) adds a Money door outside export-base |
| D3 | How does Business surplus return to circulation? | (a) `[business_tax]` to the treasury. (b) A dividend to an owner Household. (c) Leave it and accept accumulation | (a). It exists, it is daily, and it closes the loop through school funding and wages. Check in the long run whether tills stay bounded. Revisit with private capital |
| D4 | Which shortage does the acceptance run show? | (a) Treasury squeeze. Growth raises school funding past tax income, teachers go unpaid, and the school closes places. The fix is Tax or Fund. (b) Supply break. A cut Street starves shops. The fix is relaying the Street. (c) School places. Growth fills the school. The fix is placing a second one | (a). Growth causes it, so the player did not stage it, and it uses the levers base already has. (b) is a player-caused break. (c) needs a school-place episode record that does not exist |
| D5 | Road upkeep in base? | On / off | On. The code exists. It gives the treasury a cost that scales with the network |

D1 to D3 fix the Money circuit that slices 2–4 build. D4 fixes the shortage that slice 5 demonstrates.

## Slices

Each slice is one PR. [Execution](#execution) gives the order and which slices run in parallel.

1. **Base in the verification lanes.** Built. Add `rulesets/base/` to the all-Ruleset sweeps. Replay
   `founding.borough` twice and compare State Hashes. Save at Day 6, reload and continue to
   Day 12, and match the uninterrupted run. Needs nothing else.
2. **Production chain in base (D1).** After #105 merges. Replace free `restock` with the labour chain.
   Mills and shops post wages. Households buy sundries with Money. Price the fallback import at the gate edge.
3a. **Opening grant (D2). Built.** `[[business]] opening_grant` is the Money the treasury pays a
   Business that a Zone Rule opens, on a raised Building or in a vacant Unit. `World.CreateBusiness`
   moves it from the treasury Bin to the Business's balance. A treasury short of the grant opens no
   Business, and the Building stands without its trade. `Found` and placed Buildings receive none.
   The budget panel and the Census count it as the `opening grant` flow. Absent pays nothing, and no
   golden or shipped Ruleset hash moved. `rulesets/granted.toml` and `OpeningGrantTests` prove it.
3b. **City income in base (D3, D5).** Turn on 3a's grant and add `[business_tax]`, `[income_tax]` and
   road upkeep to base. Run 120 Days headless. Record the treasury, the sum of Business tills and the
   Household balances by Day.
4. **Continued operation.** Tune provisional numbers until the 120-Day run meets the operation
   checks below. Record the run's command, seed and measurements in this plan.
5. **Shortage and recovery (D4).** A headless test pairs an intervention run with a control.
   If D4 needs an elapsed episode that does not exist, coordinate it with row 30 (decline and recovery),
   which owns failure-duration semantics.
6. **Driven demonstration.** From the shell's "New empty city", drive the founding commands, then the
   shortage, the player's action and the recovery. Record what observation exposed. Run this after
   freeform slice 5 lands, or plan to re-record. Freeform replaces the Street command and the Lot layer
   that `founding.borough` and the drive script use.

## Execution

```text
(1 ∥ 3a) → #105 merges → 2 → 3b → 4 → 5 → 6
                                          ↑ freeform slice 5
```

| Slice | Size | Waits on | Lane | Risk |
|---|---|---|---|---|
| 1 | Small | — | Parallel now | May expose base defects nobody has seen |
| 3a | Medium | — | Parallel now | Golden hashes must not move. If they do, the default is wrong |
| 2 | Medium | #105 | Serial chain | Base needs a work zone so Zone Rules raise mills |
| 3b | Small | 2, 3a | Serial chain | — |
| 4 | Large | 3b | Serial chain, fan-out for tuning | Business tax may not bound tills. That would need a new mechanism |
| 5 | Medium | 4 | Serial chain | Unchecked whether a bankrupt school's teaching Business reopens after funding returns |
| 6 | Small–medium | 5, freeform slice 5 | Serial chain | Freeform re-records the log and the drive script |

Rules for parallel sessions:

- **Claim before starting.** Name the slice and its worktree in the board row's owner column, and
  check `git worktree list` for a tree already holding it.
- **One worktree per slice.** Branch from `main`, and name the branch `founding-<slice>`, e.g. `founding-3a`.
- **Only 1 and 3a run concurrently.** Slice 1 touches tests only. Slice 3a touches the Ruleset reader,
  schema, key notes and `World`, plus a fixture. Neither edits `rulesets/base/`.
- **The chain from 2 onward is serial.** Every one of those slices edits `rulesets/base/` and
  re-records `founding.borough`, whose header carries the package hash. Concurrent edits there conflict.

How to run each slice:

| Slice | Approach |
|---|---|
| 1, 2, 3b, 5, 6 | One session works it directly, in its own worktree |
| 3a | One implementer agent in an isolated worktree, or a separate session. `/orchestrate` adds an independent reviewer |
| 4 | One session owns the slice. Optionally fan out 4–6 agents, each running the 120-Day headless run with a different set of provisional numbers and reporting the Money curves |
| Before 5 | A read-only agent can settle the school-reopening risk early, while 2–4 are in progress |

## Acceptance

- **Founding.** From Ground, the committed log houses at least 80% of admitted Households by Day 12.
  Every shop sells sundries made from local flour. No Rule outside labour produces from nothing.
- **Operation.** Across Days 30–120:
  - Population, employment and the Business count stay within a band the plan records.
  - The sum of Business tills and the treasury each stay bounded. Neither grows linearly after Day 60.
  - `MoneyIsConserved` and the end-of-run invariants hold.
- **Shortage.** The D4 shortage arises without a scripted fault. The city evidence panel names its cause.
- **Recovery.** One player command restores the shortage's metric within a stated number of Days.
  The control run without that command does not recover.
- **Persistence.** Double replay matches. Save, reload and continue matches the uninterrupted run at
  Day 30 and Day 120.
- **Shell.** A driven run shows founding, the shortage, the action and the recovery.

## Coordination

- **#105** must merge before slice 2.
- **Freeform Streets** changes the Street command and the Lot layer. Slices 1–5 are content and
  economy, and survive with a re-recorded log. Slice 6 should follow freeform slice 5.
- **Export-base** later adds a Money source. Re-run the operation checks when its slice 2 lands.
- **Row 30** owns failure-duration semantics. Slice 5 reuses them and does not define its own.
