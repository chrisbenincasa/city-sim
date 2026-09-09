# 0069 — Residential scale

User-requested scale correction, 2026-09-09. Scope: house geometry, frontage subdivision,
parcel painting and a visible distance ruler. The map boundary stays at its current size.

## Decisions

- `ResidentialPlots` and `LotRuleset.Plots` separate residential frontage and depth from Street
  spacing. Detached and Perimeter forms use this path; the other block forms retain their own
  geometry. Absence retains the demonstration subdivision. Longer Streets admit more parcels.
- The new `neighbourhood.toml` shell default supplies **PROVISIONAL** target parcels 16 m wide
  and 24 m deep, with centred 8 × 12 m, two-storey houses. Integer partitioning absorbs the
  frontage remainder; the block interior bounds depth. `pictured.toml` uses the same dimensions.
  These geometry keys are fixed at world creation. Existing worlds retain their recorded Ruleset.
- Small dwellings are residential-only. A shop sharing their single tenancy would leave no room
  for a Household. The neighbourhood fixture retains minimal's placeholder economy; pictured's
  shopfronts retain their Businesses. This work makes no balance claim.
- `ZoneParcel` paints a Lot selected by ground position; `Zone` still paints a block. A first
  parcel click subdivides frontage with the block's existing permission, then changes the selected
  Lot alone. Saved Lot permissions survive rebuild and reload. Mixed permissions prevent automatic
  re-parcelling until the player repaints the block uniformly.
- `MapRuler` measures projected ground at the view centre. Parcel selection is the shell default;
  the Zoning palette also offers Whole blocks. `Main.Massing` bounds decorative outbuildings to
  the available garden space on the new residential parcels.

## Observation and verification

The controlled baseline was `minimal.toml`, seed 0, 10,000 Citizens, before stepping: 863 Buildings
inside a 1,400 × 1,528 m bounding rectangle; average footprint about 899 m². The new neighbourhood
capture at Tick 256 has 3,608 Buildings with 96 m² house footprints inside a 1,772 × 1,772 m
bounding rectangle. That rectangle occupies about 0.073% of the full map; the map remains a large
city-building area, and local scale is no reason to shrink it automatically. This is a comparison of two
named fixtures, not a density target or a balance measurement.

The revealing interaction was an empty map: one parcel painted, one house standing, 21 neighbouring
Lots still unzoned. The second surprise was decorative sheds extending beyond the smaller gardens;
the shell now bounds them to the parcel. Reproduce with:

```
dotnet build src/Borough.Godot -m:1
godot --path src/Borough.Godot -- --empty --citizens 8 \
  --ruleset rulesets/neighbourhood.toml --drive scripts/ui/residential-scale.drive
```

`ResidentialScaleTests` covers house size across block lengths, unchanged subdivision when the
legacy Lots-per-Segment key changes, non-overlap, individual permissions, Input Log payloads,
replay, derived geometry, save/reload on uniform and varied lattices, and content validation.
The renderer's driven draw list supplies the independent world-to-picture check.

## Unequal-block follow-up

The address probe found that `LotSubdivider` still called the nominal-grid overload of
`Parcel.Address`. A Lot on a varied Street could therefore claim a frontage cache entry while its
saved coordinate resolved to no Street. The regression failed at that actual call path; subdivision
now uses `Parcel.Address(BlockGround)` for both geometry modes. The former nominal overload remains
available for explicit uniform-grid callers.

This also separates [0061 F8](0061-the-varying-lattice.md#f8--watched-in-the-shell-and-the-surprise-is-a-lot-count-nobody-was-measuring)'s
Lot-count step from its suspected per-block rounding. In the current minimal-derived fixtures,
seed 0, 2,000 Citizens at population creation, absent spread produces 216 Lots on 27 carved blocks;
spreads 2, 8 and 15 produce 224 Lots on 28 carved blocks. After the address repair each carved block
has eight Lots in every case. The total counts are unchanged by the address repair. The extra block
comes from `SyntheticCity.Subdivide` reaching its Household-capacity target after a different amount
of floor has been carved; the historical inference that each unequal block gains parcels was wrong
for this reproduction. These are current fixture readings, not replacements for the historical table.
