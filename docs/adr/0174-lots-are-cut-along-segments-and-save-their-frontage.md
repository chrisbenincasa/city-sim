# Lots are cut along Segments and save their frontage

Status: accepted, 10/03/2026. Supersedes the frontage half of
[`adr/0078`](0078-frontage-is-derived-on-the-epoch-and-a-lots-width-is-the-segments-own-building-count.md).
Amends [`adr/0014`](0014-grid-streets-with-freeform-arterials.md) for local Streets.

## Decision

- Local Streets are straight or circular arcs between integer-Tile Nodes. A Segment saves one
  signed sagitta in Q16.16 Tiles.
- Every Lot is a plot cut along one side of one Segment. A block is a closed face of the Street
  graph. It sets plot depth per side and owns the leftover interior. It is not saved.
- A Lot saves its frontage: Segment handle, offset and side. It also saves its ground as an
  oriented rectangle.
- A Segment split keeps the original id on the A part and moves later Lots to the new Segment.

## Why

- `adr/0078` derives frontage from a Lot's position on a lattice line. Freeform Streets remove
  the lattice, and a nearest-Segment search is ambiguous at corners and on curves.
- One carving primitive means blocks and roadside strips cannot claim the same ground.
- Arcs keep offset curves exact, which Parallel mode and curved plot strips need.

## Consequences

- Frontage now has two homes (the Lot and the Segment). Every Street edit must migrate the
  Lots it touches. Splits and bulldozes are the two edits that do.
- Saved frontage moves the State Hash. Goldens and committed Input Logs are re-recorded.
- `adr/0078`'s `lots_per_segment` sizing argument stands. Its claim that a Lot has no depth
  no longer holds.

Details and remaining decisions: [`plans/freeform-streets.md`](../../plans/freeform-streets.md).
