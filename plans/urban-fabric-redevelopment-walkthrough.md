# 0062 — A street changes one Building at a time

Design walkthrough supporting [the urban-fabric plan](0062-the-urban-fabric.md). The Core geometry
and permission branches now run in `LocalLayoutTests`: 12-by-16 parcels, two storeys and 128 floor
Tiles per tenancy give two terrace tenancies or four in a combined 24-by-16 courtyard. These are
fixture values, not balanced Ruleset tuning. Automatic housing choice, evidence accounting and the
visible shell demonstration below remain planned integration work.

## Starting conditions

One Street frontage, schematically:

```text
Street =========================================================
       [occupied W] [vacant A] [vacant B] [vacant C] [occupied E]
```

All three vacant parcels are cleared ground, independently accessible from the Street. W and E
stay occupied throughout. There are no hypothetical acquisitions or demolition permissions.

| Ground | Housing permission | Form permission |
|---|---|---|
| A | Allowed | Terrace or courtyard |
| B | Allowed | Unrestricted |
| C | Allowed | Terrace only |

For this example, intensity permissions are uniform and admit all the candidate floor areas below.
This isolates parcel assembly from the separate implementation of different intensity limits.
Four actual Households remain seeking homes after placement. Assume their current circumstances
and sufficiently observed searches justify four additional tenancies here, with no suitable spare
capacity already covering them. The capacity-shortage integration now verifies this input against actual unplaced Households
and complete bounded standing capacity. All proposed homes use an affordable rent.

## Compare alternatives on the same ground

For A and B, compare two terrace Buildings with two Household tenancies apiece against one
courtyard Building with four. Neither proposal gets more seekers merely by enumerating more
Buildings or parcels. Both provide four useful tenancies; neither relies on surplus.

The fixture must establish that the courtyard fits A+B, that each terrace fits its individual
parcel, and that both arrangements preserve access to C, W and E. Compare their actual shared
floor-area result rather than setting capacity independently of the drawing. Existing Street
alignment can modestly favour a terrace arrangement, but does not mandate the winner.

A courtyard proposal on B+C is refused because C excludes that form. A proposal including W or E
is refused because that ground is occupied. Neither refusal changes ground, permissions or the
housing evidence. Larger candidates still need the agreed need/surplus checks.

## Follow the incremental terrace branch

Assume the selection chooses the two-terrace arrangement and its first Building stands on A.
Construct A only. B does not become a queued Building, and its form permission stays unrestricted.
C, W and E are unchanged. Only A's two tenancies count as new capacity.

Immediately before placement next runs, all four Households may still be in the Unplaced Pool.
The next construction assessment must nevertheless see A's two available tenancies. The remaining
need is two in this controlled example, not four and not zero. Actual placement later remains free
to choose among homes; these estimates do not move or reserve particular Households.

Save here and reload. Preserve A's realised footprint, storeys, form, Lot and access, the unchanged
parcels, and each piece of ground's permissions. No unbuilt terrace on B is restored because none
was committed. An uninterrupted continuation and a reloaded continuation must make the same next
decision under the same inputs.

Exercise two separate continuations:

- **Need disappears:** A houses two seekers and the other two find existing suitable homes or
  depart through their normal mechanisms. B stays vacant; the old comparison is no authority to
  complete the terrace pair.
- **Conditions change:** while B is still vacant, the player changes its permitted forms to
  courtyard only. The next proposal cannot build the previously considered terrace on B. It must
  find a currently justified courtyard that fits B alone or wait. It cannot absorb A, now occupied,
  or C, which excludes courtyards. A's standing Building is unaffected.

## Exercise actual assembly in a separate branch

Return to the starting state and select the valid courtyard on A+B. Commit one Building on the
combined site. Preserve the original geographic permissions beneath it: A remains terrace-or-
courtyard land and B remains unrestricted. Saving only their intersection would silently remove
B's broader future permission. Save/reload and inspect this branch as well as the terrace branch.

After eventual removal of this Building, those original permissions still govern subsequent
development unless the player repainted them. Removal does not have to recreate the old A/B Lot
boundaries, but any new subdivision must respect the surviving land permissions.

## Findings that determine the implementation boundary

- Permission geometry must survive independently of whichever Lots currently occupy it. Storing
  only one combined permission on the new Lot loses the player's intent.
- Realised parcel and Building geometry already has saved columns in `LotTable`. At `16263a0`,
  `SaveFile.Read` restores them and calls `World.RebuildDerived`, which explicitly does not call
  `RebuildParcels`. Normal save/load is not the missing mechanism.
- `LotSubdivider.Preview` still generates a whole-block pattern, and `PaintParcelAt` first checks
  that preview. Both need to understand realised local layouts. `Resubdivide` also returns to
  whole-block carving; road edits must not recreate overlapping old parcels.
- `RebuildParcels` itself still overwrites geometry from a whole-block pattern. Its comment naming
  `RebuildDerived` as a caller is stale. Decide its supported use when changing local layouts.
- One-step commitment must account only for the Building actually created, despite comparing a
  wider arrangement. Candidate evaluation must not publish capacity or consume evidence.

## First implementation slice: local layout and permission preservation

Build the Core foundation for a partially occupied block: assemble whole adjacent vacant rectangles
along one Street frontage, preserve geographic permissions, and commit one legal Building while
keeping neighbouring Lots and Buildings intact. Include both branches above. Start with rectangular
sites of compatible depth; corner assembly and arbitrary polygon cutting remain outside this first
slice, not permanent design refusals.

Before coding, settle the authoritative permission representation, its bounded storage and merge/
split behaviour, plus the local commit contract and frontage/preview readers. Prefer reusing saved
realised geometry over introducing a second competing source. Any new saved permission state needs
an explicit save-format/hash decision coordinated with concurrent Ruleset work.

Use explicit test proposals and known evidence to isolate geometry. This first slice does not claim
automatic form selection or implement the complete local housing signal. Its meaningful checks are
permission preservation after assembly and removal, occupied-neighbour invariance, overlap/access
refusals without mutation, actual floor capacity, derived rebuild equivalence, save/load and road
edit behaviour. Repeated assembly/removal/repainting must have bounded storage with reclaimed state.

The opt-in Core integration adds bounded candidate comparison and actual seeker/capacity accounting
to Zone Rules. Existing shell painting and previews already read the saved realised layout. A driven
demonstration must show the trigger, one-Building response and changed continuation; new or changed
Building assets follow Blender authoring first. Playtesting follows that executable integration,
with attention to visible evolution, preserved Street character and explanations for refused growth.

Permission storage, local commit, reader/road-edit migration and bounded capacity-shortage selection
are implemented in `urban-permissions`. `HousingConstructionTests` exercises both arrangements,
remaining need before placement, competing Zone Rules, repaint and save/load continuation. Persistent
preference mismatch, numeric intensity caps, larger arrangements and new shell controls remain later
work; the [local contract](urban-fabric-local-layout-contract.md#capacity-shortage-integration)
states the current search bounds.
