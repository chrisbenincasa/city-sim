# A Building is drawn realistically at every distance, from its own facts

Status: accepted, 2026-09-23. Builds on [`adr/0150`](0150-appearance-is-derived-in-the-shell-and-a-kind-is-not-a-mesh.md).

Every camera distance draws a lower-detail version of the same Building. No distance substitutes a
code colour or symbol for what the building is. Kind reads through architecture, as a real school
reads as a school. Its landmark feature (spire, chimney, sawtooth roof) survives at far distance.
Information colours belong to overlays only.

A Building's appearance is a function of its own facts, its monotonic id and the labels on its own
edges. Each Building is drawn in one Appearance Family chosen by eligibility. Attached Buildings
match because they share inputs (block face, era of construction), never because one reads another.
The only neighbour fact is whether an edge is a party wall.

## Considered options

- **Kind colour at far distance.** Colour is the strongest signal from high above. It was rejected
  because it makes the city diagrammatic, which `docs/07-the-drawing.md` §2 already refuses.
- **Guaranteed-unique civic Buildings.** This would need a city-wide assignment table, so one
  demolition could change another Building's look. Distinct Appearance Families rely on a large pool
  instead, and a repeat is possible but rare.
- **Neighbour queries or constraint solving (WFC).** Both make one edit change Buildings the player
  never touched, and solving can fail.

## Consequences

- The far level needs a simplified version of each Appearance Family. One generic box is not enough.
- The storey and bay split must match at every level, so windows do not jump on a transition.
- A neighbour's construction or demolition redraws only the shared wall of the adjacent Buildings.
- Session record and evidence are in
  [`research/procedural-buildings/`](../../research/procedural-buildings/SESSION.md).
