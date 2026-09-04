# 0062 — The urban fabric

Scoped 2026-09-04 against `main` at `8cbb6da`.

**The city called floor area behind an Address density while the picture asked how much of a block
had a Building on it. They are different quantities.**

## Sequence

1. Make `--morphology` report parcel, potential-footprint and standing-footprint coverage per saved
   `BlockPattern`. Reproduce `minimal.toml` and `platted.toml` at fixed populations and camera scales.
2. Separate intensity from coverage in the pattern selection. A tower may house the most people and
   still occupy little ground; neither ordering is allowed to stand in for the other.
3. Make non-Detached forms reachable in the ordinary playable city without making density-band
   admission and built form one indivisible choice.
4. Give the Tower a form of its own: a solid tower over a low podium or perimeter base. Do not let
   `BuildingPlan.Hollow` turn a tall footprint into an extruded courtyard merely because it is wide.
5. Move capacity and drawing through the same plan result. A podium, tower and courtyard must count
   exactly the floor the shell draws; this cannot be a renderer-only repair.
6. Re-run parcel invariants, replay/save equivalence and the fixed visual specimens. Done means dense
   blocks have continuous Street walls where their form promises them, Detached centres remain open,
   and a tall Building is not a hollow square unless its form explicitly says courtyard.

## First finding

Most Rulesets declare no `[[band]]`, so `BlockPatterns.ForBand` selects `Detached` everywhere. The
empty middle is then unlotted ground rather than a vacant Lot, and no placement pass can fill it.
`platted.toml` proves the other patterns can reach the interior, but also proves that floor-area
intensity can rise while Building count and ground coverage fall. The first step therefore measures
all three surfaces separately before changing one.

## Implemented so far

- `--morphology` now reports parcel, potential-footprint and standing-footprint coverage by the
  saved pattern on each Lot. At 10,000 Citizens, the old Tower measured 25.0%, 17.1% and 8.0%.
- A Tower now owns its full block. Its shared `BuildingPlan` is a two-storey, near-full-site podium
  beneath a centred solid shaft half the footprint on each axis. Capacity counts that same vertical
  partition and Godot emits it as two consecutive bodies for one Building.
- The intensity target remains floor area per block: `BlockPatterns.Storeys` solves the Tower's
  height against the shaft after paying for the podium. Giving the block centre back to the Lot
  therefore does not turn the densest form into a broad slab.
- On the same specimen the new Tower measures 100.0% parcel and 88.6% potential coverage; standing
  coverage is 46.1% because two of four Tower Lots are vacant. The driven Tick 1,026 frame shows
  solid shafts over broad bases rather than hollow-square towers.

Steps 1, 4 and 5 are implemented and verified. The Tower now demonstrates the geometric half of
step 2, but its intensity is still inferred from the selected pattern. Step 3—and the saved
intensity/form separation it requires—remains deliberately separate from this geometry change.
