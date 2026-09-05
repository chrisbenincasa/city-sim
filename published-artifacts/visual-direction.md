# Borough: a direction for the visuals

**Follow-up:** the player separated era from visual treatment.
[0063](../plans/0063-the-visual-treatment-comparison.md) now owns the concrete comparison plan;
the recommendations below retain their original discussion context.

Discussion proposal, 2026-09-04. Read alongside [the visuals queue](../plans/0049-visuals.md),
[the drawing](../docs/07-the-drawing.md), [Ten Thousand Facades](ten-thousand-facades.html), and
[The Grid Nobody Builds](the-grid-nobody-builds.html). This proposes the next work; it does not
settle the game's art direction.

**Aim for a convincing, colourful city with restrained detail.** The current picture needs stronger
construction, material distinctions and composition. Increasing texture resolution alone will not
make its buildings feel inhabited. A family of terraces needs a recognisable rhythm; a school needs
an entrance and grounds; a street needs convincing contact between its surfaces and the things on it.

## What the research gets right, and where I disagree

The strongest argument in *Ten Thousand Facades* is perceptual variety: different random numbers
can still produce a field of apparently identical buildings. Shape families, relationships between
neighbours, and occasional distinctive structures matter more than independent colour jitter.
The roof-family recommendation has already partly landed; hip and mansard meshes exist in the shell.

Its claim that authored assets are unable to express simulation state is too strong. Authored bays
can repeat to fit storeys and frontage. Their materials can read occupancy and condition. A fixed
building can also carry state-driven materials. **The useful distinction is between fixed geometry
and adaptable assembly, not between art and simulation.** Nor does a school have to be a wholly
handmade mesh: a recognisable architectural grammar can generate it. Authoring gives us control over
that grammar's vocabulary.

Scale also needs a more careful argument. Repeating a shared authored mesh is instancing, just as
repeating a generated mesh is. Geometry, material passes, shadows, visible instances and update cost
are the bills. The repository's population target counts Citizens; the research sometimes substitutes
Buildings, which changes the workload substantially. There is no measured basis here for declaring
all prefab libraries unaffordable, or the proposed modular kit automatically affordable.

The reports are useful leads, not final authorities. Some statements about the build are already
stale. Failure to find a precedent for occupancy-lit windows does not establish originality. I have
not independently re-audited every historical game claim. Anno's developers do explicitly describe
material and colour as part of architectural identity, which is a useful outcome to borrow.
[Primary source](https://www.anno-union.com/devblog-residential-tiers/).

The ground research's useful lesson is that urban form is part of the picture. Unequal plots,
street hierarchy, block edges and distinct neighbourhoods will change the whole image. A road-layout
statistic should describe a fixture, not become an aesthetic target or a reason to silently redraw
the player's streets. The varying lattice and plot-module work already address part of that concern.

## The proposed end state

Use procedural envelopes fitted to the actual Lots, authored architectural parts and a small shared
material library. Keep storeys, occupied capacity, abandonment and other meaningful readings tied to
the city. Give the renderer responsibility for construction detail: reveals, roof seams, trim,
plant forms and surface grain. Cosmetic texture should not masquerade as pollution or road wear.
The ground criterion in 0049 is too restrictive if interpreted as forbidding material finish simply
because the simulation can address that location; ordinary grass texture need not promise a new Rule.

Art-direct coherent families rather than draw every choice independently. A street can share brick,
eaves and window proportions while its buildings differ in width, roof form and entrances. Reserve
stronger differences for a change in use or architectural family. Keep architectural style stable
when simulation District membership changes; a reassigned boundary should not repaint a neighbourhood.

At the city camera, spend geometry on rooflines, setbacks, trees and recognisable civic or commercial
forms. At the street camera, add entrances, shopfronts, parapets and ground transitions. At photograph
distance, add selected depth and material detail. Interior mapping belongs here after silhouettes
and entrances work; it will contribute little to a normal high-camera view.

The renderer should group instances spatially, retain cheap distant representations, and update
static geometry when it changes. Godot documents all-or-none MultiMesh visibility and recommends
splitting the world into multiple MultiMeshes. Its documentation flags the page as not yet updated
for 4.7, so confirm behaviour on the installed engine before fixing budgets.
[Godot documentation](https://docs.godotengine.org/en/stable/tutorials/performance/using_multimesh.html).
Measure CPU updates, GPU rendering, shadow passes, memory and camera transitions together. Separate
render-detail choices from simulation Fidelity. Avoid committing to a particular kit radius or
large asset count before this experiment.

## The next task I recommend

**Finish one representative neighbourhood to the intended quality.** Start with a residential
family, a shopfront family and a school, plus their streets, trees and a small vehicle set. Choose
the architectural setting and reference images first. Build a modest set of reusable parts with
consistent units, pivots, material slots and detail levels, then assemble several deliberately
different blocks from it. This tests both recognisability and variety.

Review the same place at city, street and photograph distances, in noon light, evening light and
night. Include occupancy changes, abandonment and overlays. Reuse those views as the visual review
set. Then repeat the families across a larger city and measure the rendering cost. Expand the kit
only after the small scene looks right and the larger scene remains practical.

The discussion that matters next is architectural character and camera ambition: which place and
era should these buildings suggest, and how close should a satisfying photograph get? My provisional
preference is stylized realism, with believable proportions and materials and a controlled palette.

## This pass

`Main.Assets`, `Main.Travellers`, `Main.Foliage`, `buildings.gdshader`, `surfaces.gdshader` and
`ground.gdshader` contain the first improvements: distinct grounded walkers and cars, clustered tree
crowns and trunks, foliage clearance around rendered buildings, and restrained surface/joinery detail.
Parts are merged into shared meshes at startup. The shader's storey height now comes from the shell's
own value through a uniform.

These are provisional visuals. Drivers still occupy their hop's midpoint and may overlap there;
the new car shape does not create lane positions or continuous motion. Walkers are simple figures,
not animated characters. Water and shoreline geometry, architectural identity and the door/Address
alignment debt remain further work. The new geometry and shaders have not been priced at target
population. The screenshots show that trees and object proportions help immediately, whereas finer
surface detail contributes mainly at closer distances.
