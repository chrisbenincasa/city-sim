# 07 — The drawing

> Vocabulary used here is defined in [`CONTEXT.md`](../CONTEXT.md). Guiding-concept tags in `SMALL CAPS`
> refer to that file.

**What the city is supposed to look like, and how the picture gets made.** This document owns the
drawing's own pillars, the pipeline argument and the constraints that bind it. It holds **no status
and no queue** — [`plans/0049`](../plans/0049-visuals.md) owns what has shipped, and each plan
document owns its own findings.

---

## Why this document exists

[`plans/0045`](../plans/0045-amnesty.md)'s *Situation* names the hole in five words — ***no renderer,
no plan for one*** — and until 2026-09-01 the answer was a queue with an *Art style* section at the
bottom marked **undecided**. A queue is the wrong home for a design question: it is read when
somebody wants to know what to do next, and the question of what the city should look like is read
when somebody wants to know whether an answer is any good.

The prompt that produced this file was plainer than any of that:

> *I would LOVE for the city to actually LOOK like a city. Even if it's a bit stylized.*
> *This is a dollhouse; a building blocks game; people play because of the management but also to*
> *build something beautiful.*

That sentence is not an art direction. **It is a claim about why anybody plays**, and the four
pillars in [`00 §Pillars`](00-vision.md) do not contain it — see *The fifth pillar* below, which is
an open question this document records and does not settle.

---

## 1. The four pillars of the drawing

Modelled on [`00`](00-vision.md)'s, and subordinate to them. Where one of these collides with one of
`00`'s, `00` wins and the collision is a finding.

### 1.1 A city, not a diagram

The picture reads as a place before it reads as data. `EMERGENCE`

This is the one pillar stated as a **refusal**, and the refusal is what gives it teeth. The
diagrammatic pole — a clean abstract graph, beautifully executed, of the kind *Mini Metro* and *Mini
Motorways* live at — is a legitimate art direction that this project has declined. It is declined
because the thing being simulated is a **place people live in**, and a diagram of a place is a
picture of the model rather than of the city.

⚠ **The cost of this pillar is that it forbids the cheapest way out of every visual problem.** An
overlay, an icon and a coloured band are always available and always legible; the discipline is to
reach for them last.

### 1.2 The player's authorship is the ground and the street

What the player makes is what the drawing must reward. `PLAYER GOVERNS`

[`00 §Pillars`](00-vision.md) pillar 3 is emphatic: *you govern, you don't place*. **So the player
never chooses a building, and a beautiful city cannot be one the player assembled out of buildings.**
What the player does choose is the **street network** and the **zoning pattern** — and that is not a
consolation prize. It is what makes real cities beautiful and it is what people photograph.

🔴 **This inverts the obvious priority order.** Façade detail is on the half of the picture the
player did not author; kerbs, junctions, footways, planting, squares and the shape of a block are on
the half they did. ***The ground and the street are the least developed surfaces in the drawing and
the most directly earned by the player***, and that is the wrong way round.

### 1.3 The picture says what the simulation knows

Every mark either reports a fact the city holds, or is labelled as invention. `LEGIBLE CAUSE`

[`00 §Pillars`](00-vision.md) pillar 4 is *legible failure*, and the Evidence chain is its mechanism.
The drawing is the same pillar applied one level out: a shell is grey because
`BuildingTable.IsAbandoned` says so; a window is shuttered because `Occupants.Length` says so; a door
is on that wall because the Lot's `Side` says so.

⚠ **The converse obligation is the one that keeps this honest.** A Lot has no depth, a Building has
no roof and the city holds no eaves, so `PlotDepthMetres`, `RoofRiseLow` and `EavesMetres` are
**inventions** and each carries a remark saying so. ***A drawing that cannot tell you which of its
marks are load-bearing is a drawing that will be read as evidence by somebody debugging.***

⚠ **It draws on no `purpose_tag` and must not.** The simulation's random stream is for decisions, and
a shape nobody in the city can perceive is not one.

### 1.4 Variety by instancing, never by unique assets

An appearance family costs a mesh; a building costs a transform. `FAST ITERATION`

One MultiMesh per kind of thing, with per-instance transform, colour and custom data. This is why
241 Buildings and 150 gables cost two draw calls, and it is the only reason a million of them is
arguable at all.

⚠ **This is a constraint and not a preference**, and it is the one that decides pipeline questions
before taste does. A hand-authored library is not refused because it looks worse — it looks better —
but because *the number of distinct things you can afford to author is small and the number of
buildings is not*. See §3.

---

## 2. What is settled about the look

Decided by the player on 2026-08-31 and 09-01, and recorded rather than argued.

| | |
|---|---|
| **A high-level default camera** | *You can meet people; that is not the point of the game.* ⚠ **Amended by the screenshot goal** — see §5. The default stays high; what changed is that it may no longer be the *only* camera |
| **Kinds announce themselves heavily** | A school looks like a school. This is the requirement §3 exists to satisfy, and the one procedural generation cannot meet on its own |
| **Vibrant and colourful, not garish** | And the grade does more of that work than the palette does |
| **Light follows the clock** | A lit city at night is wanted, and the windows are lit from **occupancy** |
| **Not diagrammatic** | *This needs to look "city like", not just shallow, drab nothingness* — pillar 1.1 |

---

## 3. How the picture gets made

**Three pipelines, and the project's answer is the third.**

| | What it is | What it costs |
|---|---|---|
| **A — runtime procedural** | Dimensions come from the world, detail is computed in a shader. What ships today | Never wrong-sized, no art labour, scales to a million. **The ceiling on charm is real** |
| **B — offline procedural, hand touched up, shipped as a fixed library** | Generate variants under a grammar, correct them by hand, export a kit. *Cities: Skylines* and *Anno* | Needs the world's dimensions **quantised** to fit the meshes. ***And you can never author enough***, which is what a workshop is for |
| **C — a kit of parts, assembled procedurally** | Author *pieces* at a fixed module — a ground bay, an upper bay, a corner, a roof end, a dormer — and let the code lay them along whatever frontage the Lot has | The middle, and the recommendation |

✅ **C is B's instinct applied at the granularity that survives pillar 1.4.** The pieces are
hand-made and beautiful; the assembly is procedural. Nobody hand-models a 51 m façade and nobody
hand-places a hundred thousand buildings, so the only question is where the seam goes — and it goes
at the **module**, because that is the largest unit that repeats.

⚠ **A module is already ~3.5 m square and nobody chose that.** `StoreyMetres` is 3.5 and the wall
shader's bay is 3.6, so one authored piece is a panel that tiles in both directions.

### 3.1 The four tiers, by what procedural can and cannot say

| Tier | Made how | Covers |
|---|---|---|
| **Massing** | **Procedural, and it already works.** The wall shader is Müller's CGA shape in miniature — `split` on storeys, `repeat` on bays, `comp` where the door picks a face ([`references §6`](references.md)) | Walls, storeys, openings, doors, cornices, shopfronts |
| **Identity** | **A small authored kit.** No shader says *school*. Roughly 10–20 low-poly meshes, each its own MultiMesh, instanced and colour-jittered: spire, clock tower, chimney stack, water tower, sign, portico, car, bus, lamp, a second tree | §2's *kinds announce themselves*, which tier 1 cannot reach |
| **Declaration** | **The Ruleset names an appearance family, never a mesh.** `Core` hands over an id and the shell resolves it, exactly as it does for strings | Stops the next `serves = "health"` being a grey cube for ever, without putting file paths in content |
| **Overlays** | **A second render path, not a material.** The city drops to an unlit base and the layer owns all the colour | Pollution, land value, and every Map Layer after them |

### 3.2 The cost of the kit, and why it threatens nothing

🔴 **Instance count, and it does not scale.** Today 241 Buildings is ~400 instances in two draw
calls. Under a kit it is ~3,600 instances across ~15 meshes — and at a million Buildings that is
15M instances, which is not a thing to do.

✅ **Which is why the kit is a near LOD and today's shader box is the far one, and the far one is
already shipped.** The wall shader fades its openings out past 500 m by distance; the kit stops
before that. ***The kit is an addition at close range rather than a replacement***, so it is
deferrable, it is incremental, and it cannot break a frame that works today.

---

## 4. Reference games, and what each one is cited for

⚠ **Cited for a specific property and not recommended wholesale.** None of these has been re-checked
against a running copy; they are recorded so a later reader knows *which* property was wanted.

| | Cited for |
|---|---|
| **Manor Lords** | The closest shipped analogue: plots subdivided from a boundary, buildings *generated* on them, high camera. How it varies roof and material off plot geometry is the tier-1 question answered by somebody |
| **Anno 1800** | Kinds announcing themselves heavily at a high camera, with a night cycle. The nearest thing to §2's stated preference |
| **Dorfromantik**, **Islanders** | Flat-shaded low-poly done properly — the proof that a tight palette, one strong key and real shadows carry a whole look |
| **Townscaper** | Procedural massing with effectively no unique assets. The existence proof for tier 1 |
| **SimCity 4** | Reading a city at distance from silhouette and colour alone — pillar 1.1's problem, solved in sprites |
| **Cities: Skylines** | ⚠ **The pipeline counter-example and NOT a criticism of how it looks.** A hand-authored library is why it is beautiful *and* why it needs a workshop. **CS2's occupancy-driven window lighting is what §2's night line describes** |
| **Mini Metro**, **Mini Motorways** | The pole pillar 1.1 steers away from, named so the refusal is legible |

---

## 5. The fifth pillar, which is an open question and not a decision

🔴 **[`00 §Pillars`](00-vision.md) holds four and none of them is beauty.** Its *fantasy* section ends
*"That chain is the product. Everything else is in service of it."* The prompt at the top of this
document proposes a second product: **a city the player wants to photograph.**

**That is a vision-level change and not an art-style choice**, and several recorded decisions were
derived from the current four:

- [`plans/0049`](../plans/0049-visuals.md) justifies skipping façade detail with *you can meet people;
  that is not the point of the game*. If a player photographs the city from close and low, that
  premise no longer supports its conclusion.
- ~~The camera's pitch is `atan(1/√2)` and *never moves*~~ 🔴 **UNTRUE AS OF 2026-09-01** — it is
  bounded at 4° and 85° and carries a `tilt` verb ([`plans/0051`](../plans/0051-the-four-pillars-and-a-city-to-photograph.md)
  row 3). The observation that a fixed pitch is *correct framing for reading a city and wrong for
  composing a picture of one* stands; what has changed is that both are now available.
- ✅ **The four cheap pillars were built and the answer came back yes.** A moving sun, a free pitch, a
  lens and lit windows — four evenings, no meshes, no pipeline commitment — and the result is a
  photograph of a coastal town at sunset. ⚠ **The question this section asks is NOT thereby settled**:
  what was tested is whether the cheap half is worth doing, and it is. Whether *beauty* joins
  [`00 §Pillars`](00-vision.md) is still a vision-level decision nobody has taken.
- ⚠ **It does not collide with [`adr/0007`](adr/0007-stress-driven-simulation-detail.md)**, and it is
  worth saying so before somebody assumes it does: a camera is a **reading**, Fidelity is a property
  of place, and a free camera changes neither.

⚠ **This entry is in the wrong document and is here under protest.** *A question is written in one
place* ([`PROCESS.md`](../PROCESS.md) → *Where things live*) and that place is
[`plans/0002`](../plans/0002-open-questions.md). It is here because `0002` stands at **exactly**
its `CorpusBudgetTests` ceiling — 153,786 words against 153,786 — so the file cannot take one more
word while [`0045`](../plans/0045-amnesty.md) runs. ***It moves to `0002` on the day the amnesty
lifts***, and this paragraph is what stops that being forgotten.

---

## 6. What the picture does not say yet

**Grouped by whether the mechanism exists**, because planning art for vocabulary is how a project
ends up with assets for a system nobody built.

### 6.1 Built, and the drawing is silent about it

| | |
|---|---|
| **The overlays** | Pollution and land value produce numbers and **no overlay exists at all**. The instrument the pitch rests on |
| **Zone** | A Lot carries one and only vacant Lots show anything |
| **Kind identity** | `serves` and `[[business]]` both exist; a school, a shop and a house are one box |
| **The street** | One flat slab. No junction, kerb, footway or crossing, and no Arterial/Street distinction — though `foot_crossing_every` and `foot_paths_per_thousand_blocks` are Ruleset keys |
| **Travellers** | One cube for a driver and a walker alike, when Mode and the Fidelity tier are both held |
| **Car Park** | Capacity is declared per kind and nothing is drawn |
| **District** | `DistrictTable` has rows and no boundary is drawn |
| **Sealing** | Per Cell, in the readout, not on the ground |
| **Needs** | Four, saved and hashed, read by a panel |
| **Night** | ✅ **DRAWN as of [`plans/0051`](../plans/0051-the-four-pillars-and-a-city-to-photograph.md)** — the sun is on the clock, and a window's own two hours decide when its lamp is lit. ⚠ **Left in this table rather than struck from it**, because what is drawn is a *dwelling* at night: nothing distinguishes a shop, a school or a street at that hour, and a city whose only night-time mark is a bedroom window is still silent about most of itself |
| **Evidence** | ***The pillar-4 mechanism is a UI element***, and arguably the most important graphical thing in the game |

### 6.2 Vocabulary only — do not buy art for these

Transit, Utility networks, Incident / Crime / Police, lane and junction geometry, terraforming.
Checked against `Borough.Core` on 2026-09-01: every hit for those names is an incidental substring,
not a type.

---

## 7. Terrain has no depth, and that closes the voxel question

🔴 **[`adr/0157`](adr/0157-height-does-not-ship-until-terraforming-does-because-terrain-without-a-price-is-a-wall.md)
— *height does not ship until terraforming does*.** The generator uses a height field while it works
and **stores none of it**: `TerrainCellTable` is one byte a Cell, `FloodCellTable` stores a **depth**
explicitly so that no height column has to ship, and `[water] sea_level_percent` is consumed by the
generator and discarded.

[`plans/0049`](../plans/0049-visuals.md) argued small-cube voxel's payoff is *terrain depth — a cut
shoreline, a cliff, strata, a floodplain with a real edge*. ***There is no depth to cut.***

⚠ **Under [`adr/0070`](adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) this is the
classification that counts.** Height is **refused** rather than unbuilt, and *refused is the one
classification that is evidence*. So voxel would be buying a feature the simulation has declined to
have — and if terraforming is ever argued, that argument reopens the renderer question anyway.

**The ground reads flat because it is flat in the model.** No renderer choice fixes that.

---

## 8. A Lot has no width, and quantising frontage costs nothing

⚠ **This section exists because the opposite was asserted, in this document's own first draft, as
the thing that gated the pipeline decision.** It was wrong.

**The simulation holds no frontage width.** A Lot is seven columns — `East`, `North`, `Zone`,
`BuildingSlot`, `Side`, `FrontageSlot`, `FrontageOffset` — and none of them is an extent.
`Frontage.OffsetOf(index, lotsPerSegment, blockTiles)` is static arithmetic returning a **position**.
The width is computed in `Main.Kerb`, in the shell, as the half-way line to the neighbours on the
same side.

✅ **And it is already quantised.** Five Lots on a 128 m block face sit at 12, 36, 64, 88 and 112 m;
sides alternate, so one kerb takes three and the other two, which yields **five** stretch widths —
34, 36, 50, 58 and 62 m. ⚠ **Computed from `Frontage.OffsetOf` rather than measured**, and the corner
reserve trims the vertical faces further.

***The only continuous quantity in the whole arrangement is `BuildingFillLow`–`High`, which is a
constant in the shell.*** So snapping a Building's width to whole modules is a renderer change:
no simulation edit, no State Hash movement, and
[`adr/0078`](adr/0078-frontage-is-derived-on-the-epoch-and-a-lots-width-is-the-segments-own-building-count.md)
untouched.

---

## 9. What a screenshot actually costs

**Ranked by effect per unit of work**, and the ranking is the point: ***most of it is not the asset
pipeline.***

| | |
|---|---|
| **1. A low sun, and a clock that moves it** | One node. Golden hour does more for a frame than any mesh |
| **2. A camera that can get low** | The pitch is fixed at 35.26° today. The single largest limit on a photograph, and small work |
| **3. Depth of field, or tilt-shift** | A post-process. ***This is where "dollhouse" comes from***, more than geometry is |
| **4. Night, lit from occupancy** | Half-built already: the openings exist and carry the occupancy share |
| **5. The frame with no HUD** | Trivial, and `shoot` exists |
| **6. Then assets** | |

⚠ **Items 1–5 are what [`plans/0051`](../plans/0051-the-four-pillars-and-a-city-to-photograph.md)
takes on**, and none of them commits the project to a pipeline.

🔴 ✅ **ALL FIVE LANDED 2026-09-01, AND THE RANKING WAS WRONG IN ONE WAY WORTH RECORDING: THEY ARE
NOT SEPARABLE.** This table prices each row as an independent win, and each one alone is worth much
less than its line suggests — a low sun over a plan-view camera is a diagram with long shadows, a low
camera at noon is a wall of flat boxes, a lens over a HUD photographs the HUD. ***The effect is in
the combination***, so the honest reading of the table is not *do these in order* but **do all five
before judging any of them**. What survives intact is the headline: it took no meshes, no pipeline
decision and no asset budget, which is what item 6 is still waiting behind.
