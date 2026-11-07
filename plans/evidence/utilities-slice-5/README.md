# Utilities slice 5 — a Power bill, a raised tariff and a brownout

A driven run of the shell over `rulesets/powered.toml`. The player raises the Power tariff from 1 to
4 money per unit, the dwelling's next Day bills four times as much, and a brownout Day bills nothing.

```sh
godot --path src/Borough.Godot -- --load plans/evidence/utilities-slice-5/ridge.borough \
  --drive plans/evidence/utilities-slice-5/billing.drive
```

## The city

`ridge.borough` is slice 1's ridge, rebuilt on the billed `powered.toml` and saved at Tick 4,097:
8 Cells, 44 Buildings, two Districts. The plant stands in the west (Building 1) and the occupied
dwelling in the east (Building 37, two Households). One Street crosses the District boundary.
`ridge.geometry.txt` holds those Tiles.

The dwelling's lighting Rule belongs to its Households, so they pay the bill. They hold no posts,
so the fixture moves 8,192 money from the treasury to each Household before the first Day. The
dwelling asks 512 units of Power a Day.

## Trigger, response, consequence

| Shot | Tick | What it shows |
|---|---|---|
| `01` | 4,097 | Day 1 billed at the Ruleset's tariff of 1: `Paid 512 for power yesterday.` |
| `02` | 4,099 | `ui govern 0 4`. The Government panel's tariff row reads `power tariff · money per unit of power received, paid by the drawing Household or Business` and the status line reads `power_tariff set to 4 on Tick 4,100. Bills from the next firing.` |
| `03` | 4,101 | Shift-click at east 191 removes the crossing Street. Day 2 was settled whole at Tick 4,096, so it runs lit. |
| `04` | 6,150 | Day 2 billed at the raised tariff: `Paid 2,048 for power yesterday.` The settle at Tick 6,144 found no crossing, so Day 3 runs at 0% delivered. |
| `05` | 6,152 | The Budget panel lists `utility tariffs · in`. |
| `06` | 8,200 | Day 3 ran at 0% delivered, so its Rules fired at zero applications and billed nothing: `Paid 0 for power yesterday.` |

Each `.png` has a `.txt` beside it with the whole readout, so the numbers above are checkable
without opening the pictures.

## What observation exposed

- **The ridge's Households earn nothing.** The first run showed `Paid 0 … 512 went unpaid` on every
  Day, which is the waiver working as designed and the wrong picture of billing. The fixture now
  stakes each Household from the treasury, which conserves money.
- **The Building panel's network line and its bill disagree on what "yesterday" means.** At Tick
  6,150 the panel reads `Yesterday the District's network served 0 of the 512 units` above
  `Paid 2,048 for power yesterday`. Day 2 was served whole and billed whole. The network line
  reports the settle that fixed today's fraction, which slice 1 introduced. The bill line is right.
- **The Building panel covers the Budget panel's amounts** in shot `05`, and the shell has no verb
  that closes the Building panel. The readout carries neither panel's budget rows, so the amount in
  the tariff row is visible in neither. `TreasuryFlowsExplainTheBalanceTests` covers the number.
- **The HUD and the readout count Days differently**, as in slice 1. The HUD bar counts from 1 and
  the readout's header and the captions above count from 0.

## Regenerating the save

```sh
BOROUGH_UTILITY_CITY=$PWD/plans/evidence/utilities-slice-5/ridge.borough \
  scripts/test.sh --filter 'FullyQualifiedName~Writes_the_ridge_city'
```
