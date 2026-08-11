# The mode mask is saved on the Arc and the Segment's is derived

**Each Arc — one permitted direction of travel along a Segment — carries the saved mode mask valid in
that direction, and the Segment's mask is the derived union of its two.** The spike stored the
opposite: one saved mask per Segment, from which both directions inherited. That arrangement cannot
express a one-way street, which is the exact case the spike's own `Modes.cs` spends three paragraphs
arguing the mask exists to serve.

Guiding concepts: `EMERGENCE`, `LEGIBLE CAUSE`, `HONEST DEGRADATION`.

This is an **arguable** claim under [`adr/0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md).
No measurement distinguishes the two: they are the same bits at the same count while every Segment is
bidirectional, which is every Segment this project has ever generated. What differs is which cases
remain expressible, and that is a question for a sitting.

## Why

**The spike's own file states the argument and then implements its converse.**
`spikes/S2.Routing/Graph/Modes.cs` reads: *"The mask is on the arc, not on the Segment, and the
difference is not pedantry. `CONTEXT.md` → Segment lists the mode mask among a Segment's attributes,
which is exactly right while every Segment is bidirectional for every mode it permits. **A one-way
street is not: it carries cars in one direction and pedestrians in both.** Holding the mask on the
Segment forces a choice between a second arc set for foot — which is the two-parallel-networks
structure the corpus rejects by name — and a one-way street nobody may walk down."* Twenty lines
later, `RoadGraph.cs` declares `SegmentModes` as `(saved AND hashed)` and `ArcModes` as
`(derived AND rebuilt)`, and `GraphGenerator.Finish` writes `arcModes[arc] = segModes[segment]`.

**It got away with it because it never generated the case.** The generator's own header says so:
*"There are no one-way streets — every Segment's two arcs carry the same mask, though the structure
stores them separately and would carry a difference."* So the derivation ran the wrong way for the
whole of S2 and no measurement could have noticed, because the two orderings agree on every graph the
spike built. **A docstring that argues for a structure the file below it does not have is worse than
one that argues for nothing**, because the next reader ports the argument and inherits the code.

**The corpus rejects the escape route by name.** With one mask per Segment, a one-way street needs a
second edge set for pedestrians — and `CONTEXT.md` → Road Graph is categorical: *"Pedestrian and
vehicle edges are the same structure, tagged by which modes may traverse them — **not two parallel
networks**."* `03 §3.7` gives the three things one graph buys and all three are lost by splitting it:
one Epoch covering both networks, one revalidation path, and a multi-Leg Trip routed by a single
mode-aware search rather than stitched across two structures. The alternative escape — a one-way
street nobody may walk down — is not a modelling simplification but a wrong city.

**Severance is the mechanism this protects, and it is the design's flagship emergent behaviour.**
`CONTEXT.md` → Severance: *"a city can be perfectly well connected for cars and broken for people, and
the game can say so."* It works because an Arterial's Arcs carry `Car` and not `Foot` — *nobody deletes
a pedestrian route, the mask simply never granted one*. That is already a per-direction statement in
everything but storage; a per-Segment mask expresses it only because an Arterial happens to be
symmetric. A one-way Arterial with a footway on one side is a real thing, and under the old ordering
it is inexpressible.

**The cost is one column and it is paid once.** Two saved `byte` columns instead of one, on the
Segment table — not on the Arc table, which is derived and stores its own copy either way. Against
the count of Segments that is a rounding error in a graph whose footprint is dominated by the
adjacency. The alternative cost is a schema change applied to a table that already has rows, plus a
re-baseline, on the day somebody builds a one-way street.

**Deriving the Segment's mask loses nothing, because the union is what every reader of it wants.**
`CONTEXT.md` → Segment lists the mode mask among a Segment's attributes, and that entry stays true:
*what is this road for* is answered by the union, and it is only *may I go this way* that needs the
direction. The union is a single `OR` recomputed with the rest of the derived columns.

## Consequences

**`CONTEXT.md` gains an `Arc` entry.** The directed half-edge had no name in the vocabulary — Node,
Segment, Street, Arterial, Junction, Epoch and Severance all had entries, and `CONTEXT.md` → Segment
explicitly rules "edge" out of design prose. A decision that turns on the distinction between a
Segment and one direction of it cannot be stated in a vocabulary with only one of the two words.

**`RoadSegmentTable` saves `modes_forward` and `modes_backward` and derives `modes`.** The forward
direction is A→B, fixed by the endpoint columns. The generator writes the two equal everywhere, so
this ADR changes no city today — which is the point: it changes what the *next* city can be.

**A one-way street is now a data question rather than a schema one.** Nothing generates one and no
command can draw one, because `CommandKind.Connect` is 5a-bis. When it arrives it writes two different
masks and every consumer already reads the right one.

**The invariant tier gains the union check.** `modes` being exactly `modes_forward | modes_backward`
is derived state agreeing with saved state, which is the whole-world tier's business — and a derived
column that silently stops agreeing with its source is precisely the defect the `(derived AND
rebuilt)` declaration exists to make impossible to hide.

## What would trigger revisiting

**A mode that is not a property of a direction.** Both current modes are. A mode gated on something
per-Segment rather than per-direction — a weight limit, a toll, a seasonal closure — would sit on the
Segment, and if such attributes outnumbered the directional ones the storage should follow them rather
than the mask.

**Per-Arc storage growing beyond a mask.** This ADR is cheap because a mask is a byte. If a direction
acquires its own capacity, its own free-flow speed and its own volume — which a genuine Lane model
might well want — then the Arc is the row and the Segment becomes a grouping, and that is a bigger
decision than this one, taken on the day the second per-direction column is needed rather than
inferred from this one.

**A measurement showing the union recomputation matters.** It is one `OR` per Segment per rebuild,
against an adjacency rebuild that is `O(Segments)` anyway. If a profile ever attributes a visible cost
to it, the answer is to save the union alongside rather than to move the mask back — the two are
independent choices and only the second one loses expressiveness.
