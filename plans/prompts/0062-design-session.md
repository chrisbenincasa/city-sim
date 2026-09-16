# Starter prompt — 0062 design and scoping

Let's design and scope 0062, **Urban fabric: intensity independent of built form**.
An agent is working on 0077 and the first playable founding loop. Preserve that work and the
occupied terrain tree. Row 30's decline/recovery scope is in `plans/decline-recovery.md`.

Follow AGENTS.md and PROCESS.md: check Git status, recent local/remote commits, worktrees and
`plans/0000-board.md` before claiming work. Read `plans/0062-the-urban-fabric.md`, relevant
CONTEXT.md terms, drawing/authoring contracts and relevant decisions. Verify current code and
tests; the Tower podium and shared capacity/drawing geometry have already shipped, so establish
what remains instead of restarting that work.

Treat this as a substantial collaborative design session. Start with a concise account of current
behaviour, the player-facing gap and the most consequential open choice. Ask focused questions,
recommend an answer with its tradeoffs, and resolve one branch at a time. Use concrete blocks and
player actions to explain alternatives. Persist settled decisions in the owning design text.

Work through:

1. What intensity, built form, ground coverage, floor capacity and entrance count each mean.
   Determine which the player governs, which content authors, and which development chooses.
2. How ordinary play reaches Detached, terraces/perimeter forms and towers; how the same form
   supports different intensities and the same intensity supports different forms where feasible.
3. How zoning permissions, intensity caps, saved pattern selection and individual development
   decisions interact. Explain selection/refusal to the player without adding an RCI meter.
4. What happens to existing Lots, occupants, Buildings and access when permission, form or
   intensity changes. Distinguish permission to redevelop from immediate rebuilding; settle the
   treatment of incompatible footprints, parcel changes, insufficient floor area and mixed use.
5. How simulation geometry, floor-derived capacity, Blender assets, drawing and picking agree.
   Preserve continuous Street walls where promised, open Detached centres and solid towers over
   podiums. Coordinate entrance/open-ground work without silently absorbing its whole board row.
6. Interfaces with 0077, runtime profiles and the first playable: specify the contract and owner
   of any needed content/state change. Keep an independent design path while identifying actual
   implementation dependencies. Avoid concurrent edits to their implementation files.

Use the drive skill for any shell launch or visual observation, and Blender first for new Building
models. Reuse morphology reporting and fixed specimens. Read-only probes and demonstrations are
welcome; finish the design before broad implementation.

Deliver an updated short 0062 plan, durable design changes, and one accurately scoped board entry
(split only independently selectable outcomes). Include chosen semantics, remaining blockers,
implementation slices and acceptance checks: reachable forms through ordinary player actions,
matched capacity/drawing, safe occupied-city transitions, deterministic morphology, replay and
save/load. Define a driven demonstration showing trigger, response and consequence. End with the
smallest implementation slice ready to start and any decisions that still require my input.
