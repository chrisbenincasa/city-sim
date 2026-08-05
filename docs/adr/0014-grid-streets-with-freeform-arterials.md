# Streets snap to the grid and only Arterials are freeform

**Local Streets snap to the Tile grid. A small, bounded number of Arterials — highways, rail, major boulevards — are freeform splines, and they meet each other and the grid only at authored Junction pieces.** The Road Graph is uniform nodes and edges regardless of which of the two a road is. The simulation never sees a spline.

## Why

This is a **scope** decision far more than an aesthetic one, and it should be read as one.

Fully freeform multi-lane roads do not make road *drawing* hard; they make **intersection construction** hard, and intersection construction is a computational-geometry research problem wearing a level-editor costume. It requires boolean operations on curved polygons, lane connectivity resolution across arbitrary junction fan-in, and junction mesh generation that stays watertight for every angle, width, and elevation the player can produce. It is the problem that consumed Citybound: Eickhoff read *"hundreds of papers, master and PhD theses and books"* on it, wrote a bespoke geometry kernel (`descartes`) and a procedural-geometry library (`michelangelo`) to attack it, and his final devblog concedes he had *"been abandoning the simulation aspect for a while"* in favour of exactly this class of work. The geometry is genuinely fascinating and its gameplay payoff is not remotely proportional to its cost.

The hybrid confines the difficulty to a place where it is bounded:

- **Grid-to-grid intersections are trivial**, and the Road Graph falls out of the Tile grid for free — nodes at Tile junctions, edges along Tile edges. It also arrives pre-partitioned, because the Chunk grid is already a regular tiling the pathfinding cluster can align to, which is most of what HPA\* wants handed to it.

  *Amended by [`0040`](0040-the-pathfinding-cluster-is-a-multiple-of-the-chunk-not-the-chunk.md).* This bullet originally read *"the Chunk grid **is** already the pathfinding cluster"*. The useful half — that a regular tiling already exists — stands and is why the alignment costs nothing. The identity was asserted rather than argued, and it is wrong for a reason this ADR had no way to see: **the Chunk is written into the save and the cluster is not**, so unifying them would make a freely-recomputable number as permanent as the save format.
- **Arterial-to-Arterial junctions are rare.** Rarity is what makes authoring viable: a small library of pieces — cloverleaf, diamond, trumpet — that the **player places**, rather than geometry procedurally generated from arbitrary inputs. A finite set of hand-built pieces is content work with a known end; procedural junction generation is not.
- **Arterial-to-grid connections happen only at authored on/off-ramp pieces.** The Arterial never merges into the Street network anywhere else, which is both the geometric constraint and, as it turns out, how real limited-access roads behave.

**The central invariant is that the Road Graph is uniform.** Nodes and edges, identical in kind, regardless of how the road was drawn. Geometry is a rendering concern. This is what makes the decision *scope* rather than *simulation*: nothing in routing, traffic, Trip cost, or Stress accounting knows the difference between a Street and an Arterial except through edge attributes it would carry anyway. If curved local roads are ever wanted, that is a renderer and editor change against an unchanged simulation.

**And the pedestrian layer is where this stops being a compromise and becomes an asset.** Under [`0008`](0008-walking-is-a-simulated-leg.md), Streets carry pedestrian edges — sidewalks alongside the vehicle edges, and crossing edges at intersections. **Arterials deliberately carry none, except at authored Junction pieces.** That single asymmetry is the entire mechanism behind Severance: an Arterial genuinely cuts the pedestrian graph, so a walk route to the shop 80m away across the highway does not exist as a matter of topology, not as a scripted penalty term subtracted from a desirability score. The player can accidentally build a severed neighbourhood, and the game can name it, because the missing edges are real. It also makes the Junction library a live gameplay lever: which piece the player places decides whether people can cross, and that is a land-use decision expressed as a piece of infrastructure.

**Diagonals on the grid are worth allowing** (SimCity 4 and Transport Tycoon both do). They buy a meaningfully more organic street pattern for near-zero simulation cost, because the graph stays discrete — a diagonal edge is still an edge with a length. The cost lands in art (corner and transition pieces) and in Lot subdivision against diagonal frontage, not in routing.

## Rejected

**Fully freeform everything.** The honest maximum, and the direct cause of the largest single time sink in the closest prior art to this project. Rejected on scope grounds alone; nothing about the resulting game is better in proportion.

**Pure grid, no Arterials.** Cheapest, and it deletes the geometry problem entirely. Rejected because a highway then reduces to a fast Street — which removes the mechanism for Severance, removes the distinction between limited-access and local infrastructure, and makes every large city look like graph paper.

## Consequences

- **Arterials must be genuinely rare, and probably budgeted.** If the player can lay them freely, authored-Junction combinatorics return through the back door and the scope saving is spent. Rarity is load-bearing, not flavour.

  > **"Probably budgeted" is retired by [`0035`](0035-infrastructure-is-priced-by-what-it-consumes.md).** A budget is an authored cap justified only by *otherwise players lay too many*, which fails this project's own test for a rule. Three mechanisms restrain Arterials instead: they **Seal** land, they **sterilise** the strip they cannot give frontage to — ruinously in dense areas, cheaply in the periphery, which is self-limiting exactly where the junction combinatorics would arise — and they carry the **largest Upkeep** in the network because Upkeep is wear. Rarity stays load-bearing; nobody has to author it. *Note the mild tension this creates: sterilisation rewards aligning an Arterial to the grid, which pulls against its being freeform. Freeform buys curves and interchanges rather than misalignment, and threading a highway to waste less land is the planning skill — but the pull is real.*
- **Junction pieces are content with three faces each**: a mesh, a Road Graph topology fragment, and a pedestrian-edge policy. The set must ship complete, because a missing piece is a connection the player simply cannot make — and being refused a connection is only acceptable if the game explains which piece is missing.
- **Nothing zones onto an Arterial.** Lots are subdivided against the Street network; Arterials have no frontage and no Access Point. This falls out of the geometry constraint and is also correct urbanism, but it means Arterial corridors need something to be made of.

  > **Answered by [`0035`](0035-infrastructure-is-priced-by-what-it-consumes.md), with no new mechanism.** Corridor land with no frontage is a **dead block interior** — already `02 §2.2`'s stated behaviour, arriving from a different cause. Corridor land reached by a **parallel Street** is ordinary Lots that [`0034`](0034-fields-are-sorted-by-source-geometry.md) makes the noisiest and dirtiest in the city, therefore the cheapest — and **Processing industry is located by reachability**, so it bids high on land whose only defect is the thing it does not care about. The corridor fills with industry and warehousing because three unrelated rules meet, and the premium concentrates **near Junctions** rather than along the corridor, which makes ramp placement a land-use decision. What remains genuinely open is **visual**: what unlotted verge is made of on screen, which is content alongside the Junction library.
- **The grid is visible in the city's silhouette forever.** Diagonals soften it; they do not hide it. This is a permanent aesthetic commitment in the same way low-poly is, and it should be embraced rather than apologised for.
- **Severance becomes a diagnosable state the game must actually diagnose.** [`0008`](0008-walking-is-a-simulated-leg.md) already requires that a *no route found* Trip Fate for a destination 50m away be explicable. This ADR is the decision that makes such Trips common enough to matter. `LEGIBLE CAUSE`

## What would trigger revisiting

- **Playtesting showing the grid reads as rigid or samey.** The escalation order is diagonals first, then Streets that render as splines while snapping their endpoints to grid nodes — the graph is unchanged, so this is a renderer change. Freeform local geometry is the last resort, not the first.
- **The authored Junction set proving insufficient** in normal play, with players routinely unable to make a connection they consider obvious. The answer is more pieces. It is never procedural junction generation from arbitrary geometry — that is the rejected option arriving in instalments.
