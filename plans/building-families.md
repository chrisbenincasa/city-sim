# Building families

## Outcome

Every Building in a shipped Ruleset draws a family body that meets the
[variety bar](appearance-families.md#variety), and no kind or Lot shape falls back to a box.
This file tracks each family's state and the families still missing. The family format and the
picking rule live in [appearance-families.md](appearance-families.md).

## Families

"Meets the bar" means each size band draws at least four forms, a form being a roof shape plus a
plan, and neighbors across runs never share a form and pitch within 5 degrees.

| Group | Families | Lots | Forms | Variety | Where |
|---|---|---|---|---|---|
| Narrow detached houses | `house-detached-*` | 1–3 storeys, 8–11 m | 10 | Meets the bar | main |
| Rowhouses | `rowhouses`, `rowhouses-*` | 2–3 storeys, 4–12 m, up to 16 m deep | 6, 5 at 12 m | Meets the bar | main, #241 |
| Attached apartments | `corridor-`, `two-unit-`, `walkup-apartments*` | 3 storeys, 20–36 m | 8 per band | Meets the bar | #242 |
| Detached apartments | `detached-corridor*`, `detached-walkup*`, `detached-two-unit*` | 4–5 storeys, 16–64 m | 7–8 | Meets the bar | #242 |
| Shop houses | `shop-house-*` | 2–4 storeys, 4–24 m | 9 | Meets the bar; parades stand detached (#243) | #244 |
| Workshops | `workshop`, `workshop-*` | 2 storeys, 28–36 m | 6 | Meets the bar on a fixture preset only | #245 |
| Wide houses | `house-terrace`, `house-front-gables`, `house-villa` | 1–3 storeys, 12–64 m | 2 below 24 m, 3 above | Below the bar | main |
| Mansion blocks | `mansion-block`, `mansion-balconies`, `mansion-shops` | 4–11 storeys | 3 families | Not checked | main |
| Towers | `tower-point`, `tower-stepped-point`, `tower-l`, `tower-h` | any | 4 families | Not checked | main |
| Panel slabs | `panel-slab`, `panel-slab-plain`, `panel-slab-balconies` | 12–24 storeys | 3 families | Not checked | main |
| Corner shop with flats | `corner-shops-with-flats` | 3 storeys, 20–28 m | 1 | Not checked | main |
| Office-warehouse | `office-warehouse` | 2 storeys, 32–40 m | 1 | Not checked | main |
| Centre strip | `centre-strip` | any shopfront | 1 | Not checked | main |

"Families" in the Forms column counts families, not checked forms, so the true form count can be lower.

## Where each family draws

Headless `--appearance appearance/test-street`, seed 1, 10,000 Citizens, 600 Ticks, on
`workshop-variety`. Each Ruleset populates its own lattice.

| Ruleset | Buildings | Box fallbacks | Families that draw |
|---|---|---|---|
| `base/ruleset.toml` | 1,629 | 926 dwellings, 4 ports, 1 shopfront | wide houses 517, office-warehouse 11 |
| `traded.toml` | 1,479 | 266 dwellings, 31 shopfronts | narrow houses, rowhouses, attached apartments, shop houses, mansions, towers, centre strip |
| `gridded.toml` | 840 | 204 dwellings | wide houses 577, office-warehouse 19 |
| `pictured.toml` | 3,712 | 78 shopfronts | dwellings all drawn; centre strip 26 |
| `platted.toml` | 134 | none | detached apartments, wide houses, mansions, panel slabs, office-warehouse |

The base game is founded by the player from empty Ground, so a populated lattice may not show
the Lots a player makes. A founding run's Lots have not been counted.

## Missing families

### Kinds with no family body

| Kind | Declared in | Draws as |
|---|---|---|
| school | base game, 8 fixtures | box |
| port | base game, 11 fixtures | box |
| clinic, hospital | `care`, `pictured`, `profile-services` | box |
| primary, secondary, college, university | `schooling` | box |
| office | `offices`, `pictured` | no family, not even box |
| plant, works, gate | `powered` | no family, not even box |

### Lot shapes with no family

| Lot shape | Box fallbacks | Candidate family |
|---|---|---|
| Detached dwelling, 2–3 storeys, 4–32 m wide, 4–8 m deep | base 891 | Unknown; check whether a player makes such Lots first |
| Detached dwelling, 2–3 storeys, 8–84 m wide, 8–36 m deep, outside the wide houses' 12–64 m by 12–28 m | gridded 204 | Deeper, shallower and wider houses |
| Perimeter dwelling, 3 storeys, 4–12 m wide, 20–24 m deep | traded 170 | Deeper rowhouses or rear wings |
| Perimeter dwelling, 3 storeys, 16–20 m wide | traded 96 | Wide houses at 3 storeys in a Perimeter block |
| Courtyard dwelling, 5–6 storeys, 36–120 m | traded 26 | Courtyard apartment block |
| Pad-site shopfront, 1 storey, 24 × 16 m | pictured 78, traded 24 | Standalone store, diner or filling station |
| Sales-yard shopfront, 1 storey, 24 × 40 m | traded 4 | Car lot or garden center |
| High-street shopfront, 3 storeys, 120 m | traded 2 | Department store |
| Precinct shopfront, 1 storey, 120 × 88 m | traded 1 | Shopping center |

## Next

1. Count the Lots a founding run makes, to decide whether the base game's shallow Lots need a family.
2. Bring the wide houses up to the bar. They draw on the base game and below 24 m have only two forms.
3. Add pad-site stores, the largest shopfront gap.
4. Add school and port bodies, the two service kinds in the base game.
